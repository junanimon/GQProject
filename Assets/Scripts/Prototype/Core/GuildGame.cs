using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuildProto
{
    // 게임 조립 + 진행 흐름
    //  타이틀 → 프롤로그 → [주 시작(자동 저장) → (밤: 개인 이벤트 · 사건 · 원정 · 승급 심사 · 의뢰서 · 게시판 구성/상점
    //  → 낮: 창구 → 결과) × 평일 → 주말 습격 → 주간 정산] × 주 → 엔딩 (또는 배드 엔딩 → 재도전)
    // 규칙과 상태는 도메인 객체와 서비스가 맡고, 여기서는 어떤 장면을 어떤 순서로 띄울지만 정한다.
    public class GuildGame : MonoBehaviour
    {
        [Header("데이터 (몬스터 · 의뢰 · 모험가 · 주차 · 스토리)")]
        public GameDatabase database;

        [Header("화면 (Core 씬)")]
        public TitleView titleView;
        public TopBarView topBar;
        public TextPageView textPage;
        public StoryView storyView;
        public ScreenFader fader;

        [Header("화면 씬 (시작할 때 함께 불러온다)")]
        public string nightScene = "Night";
        public string dayScene = "Day";

        // 다른 씬의 화면은 인스펙터로 연결할 수 없어서, 씬을 불러온 뒤 찾아서 연결한다
        [NonSerialized] public NightView nightView;
        [NonSerialized] public BoardSetupView boardSetup;
        [NonSerialized] public PromotionView promotionView;
        [NonSerialized] public DayView dayView;

        [Header("밸런스")]
        public GameConfig config = new();

        Calendar calendar;
        QuestGenerator generator;
        Guild guild;
        AdventurerRoster roster;
        VisitorFactory visitors;
        QuestResolver resolver;
        RaidResolver raids;
        RegionMap regions;
        PromotionBoard promotions;
        FacilityShop shop;
        RandomEvents randomEvents;
        RunStats stats;

        readonly List<Quest> board = new();
        readonly List<Assignment> assignments = new();
        readonly List<DayRecord> history = new();
        readonly HashSet<Adventurer> awakened = new();

        int day;
        ITempRule tempRule;
        NightShift night;
        Counter counter;
        Expedition expedition;
        bool grahamUsed;
        float dayTimer, dayLength, urgentTime;
        bool posting;

        IEnumerator Start()
        {
            yield return LoadView<NightView>(nightScene, v => nightView = v);
            yield return LoadView<BoardSetupView>(nightScene, v => boardSetup = v);
            yield return LoadView<PromotionView>(nightScene, v => promotionView = v);
            yield return LoadView<DayView>(dayScene, v => dayView = v);

            nightView.Posted += OnSheetPosted;
            dayView.approveButton.onClick.AddListener(() => counter?.Approve());
            dayView.rejectButton.onClick.AddListener(() => counter?.Reject());
            dayView.confirmButton.onClick.AddListener(() => counter?.Confirm());
            dayView.reportButton.onClick.AddListener(() => counter?.Report());
            dayView.bonusButton.onClick.AddListener(() => counter?.Bonus());
            dayView.hallButton.onClick.AddListener(() => dayView.nomination.Open(counter));
            dayView.closeDayButton.onClick.AddListener(EndDay);
            ShowTitle();
        }

        // 화면 씬을 덧붙여 불러오고(이미 열려 있으면 그대로) 그 안의 View를 찾는다
        static IEnumerator LoadView<T>(string sceneName, Action<T> found) where T : Component
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded) yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByName(sceneName);
            var view = scene.GetRootGameObjects().Select(go => go.GetComponentInChildren<T>(true)).FirstOrDefault(v => v != null);
            if (view == null) Debug.LogError($"[GuildGame] '{sceneName}' 씬에서 {typeof(T).Name}을(를) 찾지 못했습니다.");
            view.gameObject.SetActive(false);
            found(view);
        }

        void Update()
        {
            if (counter == null || !counter.IsOpen) return;
            dayTimer += Time.deltaTime;
            if (urgentTime > 0 && dayTimer >= urgentTime) SendUrgent();
            float minutes = Mathf.Lerp(9 * 60, 18 * 60, dayTimer / dayLength);
            topBar.SetClock($"{(int)minutes / 60:00}:{(int)minutes % 60:00}", 1 - dayTimer / dayLength);
            topBar.SetStats(guild.Funds, guild.Danger, guild.Reputation);
            if (dayTimer >= dayLength) EndDay();
        }

        // ───────── 타이틀 · 새 게임 · 이어하기 ─────────

        void ShowTitle()
        {
            StopAllCoroutines();
            counter = null;
            ShowOnly(titleView);
            topBar.gameObject.SetActive(false);
            AudioHub.Music(Bgm.Title);
            titleView.Show(SaveSystem.Load(), database.endings, Continue, NewGame);
        }

        void NewGame()
        {
            Setup(null);
            ShowOnly(null);
            Top("프롤로그");
            AudioHub.Music(Bgm.Night);
            storyView.Play("프롤로그 — 로웰 모험가 길드", database.intro, LookOf, BeginNight);
        }

        void Continue()
        {
            var save = SaveSystem.Load();
            if (save == null)
            {
                NewGame();
                return;
            }
            Setup(save);
            BeginNight();
        }

        // 한 판의 상태를 새로 만들거나 세이브에서 되살린다
        void Setup(SaveData s)
        {
            StopAllCoroutines();
            counter = null;
            calendar = new Calendar(database.weeks, config.daysPerWeek);
            generator = new QuestGenerator(database.generator) { NoneMonster = database.monsters.First(m => !m.isMonster) };
            regions = new RegionMap(database.regions);
            guild = s == null ? new Guild(config.startFunds, config.startDanger, config.startReputation) : new Guild(s.funds, s.danger, s.reputation, s.guildRank);
            roster = s == null ? new AdventurerRoster(database, regions, guild.Rank) : new AdventurerRoster(database, regions, guild.Rank, s.adventurers);
            visitors = new VisitorFactory(roster, config, database.namedVisitChance);
            resolver = new QuestResolver(config);
            raids = new RaidResolver(config);
            promotions = new PromotionBoard(database);
            shop = new FacilityShop(database.facilities);
            randomEvents = new RandomEvents(database.randomEvents, s?.randomSeen);
            stats = s?.stats ?? new RunStats();
            board.Clear();
            assignments.Clear();
            history.Clear();
            awakened.Clear();
            if (s == null)
            {
                board.AddRange(database.seniorQuests.Select(d => new Quest(d, true)));
                expedition = null;
                grahamUsed = false;
                day = 1;
                return;
            }
            board.AddRange(s.board.Select(q => Quest.Restore(q, MonsterNamed)));
            foreach (var name in s.awakened)
            {
                var a = roster.All.FirstOrDefault(x => x.Name == name);
                if (a != null) awakened.Add(a);
            }
            shop.Restore(s.facilities);
            expedition = s.expedition != null && s.expedition.started ? Expedition.Restore(s.expedition, roster) : null;
            grahamUsed = s.grahamUsed;
            day = s.day;
        }

        MonsterData MonsterNamed(string assetName) =>
            database.monsters.FirstOrDefault(m => m.name == assetName) ?? generator.NoneMonster;

        void Save() => SaveSystem.Save(new SaveData
        {
            day = day,
            funds = guild.Funds,
            danger = guild.Danger,
            reputation = guild.Reputation,
            guildRank = guild.Rank,
            grahamUsed = grahamUsed,
            adventurers = roster.Save(),
            board = board.Where(q => !q.IsTaken).Select(q => q.Save()).ToList(),
            awakened = awakened.Select(a => a.Name).ToList(),
            facilities = shop.Save(),
            randomSeen = randomEvents.Seen.ToList(),
            expedition = expedition?.Save(),
            stats = stats,
        });

        // ───────── 밤 ─────────

        void BeginNight()
        {
            var week = calendar.Week(day);
            var steps = new List<Action<Action>>();
            topBar.gameObject.SetActive(true);
            AudioHub.Music(Bgm.Night);
            if (calendar.IsFirstDayOfWeek(day))
            {
                tempRule = TempRuleFactory.Create(week);
                foreach (var a in roster.All) a.StartWeek();
                randomEvents.NewWeek();
                shop.NewWeek();
                Save();     // 주 시작 자동 저장 (배드 엔딩 뒤 '이번 주 처음부터'로 돌아오는 곳)
                steps.Add(next => fader.Play($"{calendar.WeekOf(day)}주차 — {week.title}", week.subtitle, () => { ShowOnly(null); next(); }, longHold: true));
                if (week.intro.Count > 0) steps.Add(next => storyView.Play($"{calendar.WeekOf(day)}주차 — {week.title}", week.intro, LookOf, next));
            }
            else steps.Add(next => fader.Play($"{calendar.Label(day)} · 밤", "", () => { ShowOnly(null); next(); }));

            steps.Add(PersonalEvent);
            steps.Add(RandomEvent);
            steps.Add(ExpeditionEvents);
            foreach (var s in calendar.StoriesFor(day).Where(s => string.IsNullOrEmpty(s.id)))
                steps.Add(next => storyView.Play(s.title, s.lines, LookOf, next));
            steps.Add(PromotionReview);
            steps.Add(_ => StartSheets());
            Run(steps);
        }

        void StartSheets()
        {
            var week = calendar.Week(day);
            var sheets = week.fixedQuests.Where(f => f.day == calendar.DayOfWeek(day)).SelectMany(f => f.quests).ToList();
            while (sheets.Count < week.sheetsPerNight) sheets.Add(generator.Create(week, regions.MonstersOf(guild.Rank)));
            night = new NightShift(day, sheets);
            ShowSheet();
        }

        // 호감도 · 주차가 닿은 네임드 개인 이벤트 (하룻밤에 하나)
        void PersonalEvent(Action next)
        {
            int week = calendar.WeekOf(day);
            var who = roster.Named.Where(a => !a.IsDead && a.PendingEvent(week) != null).OrderByDescending(a => a.Affinity).FirstOrDefault();
            if (who == null)
            {
                next();
                return;
            }
            var ev = who.PendingEvent(week);
            who.MarkSeen(ev);
            Top($"{calendar.Label(day)} 밤 — 개인 이벤트");
            storyView.Play($"{who.Name} — {ev.title}", ev.lines, LookOf, next);
        }

        // 작은 사건 (한 주에 최대 몇 개)
        void RandomEvent(Action next)
        {
            var ev = randomEvents.Roll(calendar.WeekOf(day), config.randomEventsPerWeek);
            if (ev == null)
            {
                next();
                return;
            }
            string effect = RandomEvents.Apply(ev, guild, roster);
            var lines = ev.lines.ToList();
            if (effect != null) lines.Add(new StoryLine { text = $"<color=#d9b36a>({effect})</color>" });
            Top($"{calendar.Label(day)} 밤 — 사건");
            storyView.Play(ev.title, lines, LookOf, next);
        }

        // 3주차 바르톨 원정: 출발(동행자 선택) · 귀환(생사)
        void ExpeditionEvents(Action next)
        {
            var depart = calendar.StoryFor(day, Expedition.DepartId);
            var bartol = roster.Named.FirstOrDefault(a => a.Named.displayName == "바르톨");
            if (depart != null && expedition == null && bartol != null && !bartol.IsDead)
            {
                expedition = new Expedition(bartol, bartol.SortiesBeforeWeek);
                storyView.Play(depart.title, depart.lines, LookOf, () =>
                {
                    var candidates = Expedition.Candidates(roster, bartol).ToList();
                    var options = candidates.Select(c => $"{c.Name} ({Txt.J(c.Job)}) 를 붙인다").Append("아무도 보내지 않는다").ToList();
                    storyView.Ask(depart.title, database.seniorName, database.seniorLook,
                        "바르톨이 내일 새벽 북쪽 숲으로 떠난다. 누구를 함께 보낼까?", options, i =>
                        {
                            var companion = i < candidates.Count ? candidates[i] : null;
                            if (companion != null && companion.OnQuest) companion = null;
                            expedition.Depart(companion);
                            next();
                        });
                });
                return;
            }

            bool returnDay = calendar.StoryFor(day, Expedition.ReturnAliveId) != null || calendar.StoryFor(day, Expedition.ReturnDeadId) != null;
            if (returnDay && expedition != null && expedition.Started && !expedition.Resolved)
            {
                expedition.Resolve(config.expeditionFatigueLimit);
                var story = calendar.StoryFor(day, expedition.Survived ? Expedition.ReturnAliveId : Expedition.ReturnDeadId);
                if (!expedition.Survived)
                    foreach (var a in roster.Named.Where(a => !a.IsDead && a != bartol && a.Named.job != Job.Priest && a.Named.job != Job.Archer))
                    {
                        awakened.Add(a);
                        a.ChangeAffinity(10);
                    }
                var lines = story.lines.Select(l => new StoryLine
                {
                    speaker = l.speaker,
                    text = l.text.Replace("{companion}", expedition.Companion?.Name ?? "아무도"),
                }).ToList();
                storyView.Play(story.title, lines, LookOf, next);
                return;
            }
            next();
        }

        // 승급 심사 (첫날 밤은 건너뜀)
        void PromotionReview(Action next)
        {
            var applicants = day == 1 ? new List<Adventurer>() : promotions.Applicants(roster, config.promotionApplicants);
            if (applicants.Count == 0)
            {
                next();
                return;
            }
            ShowOnly(promotionView);
            Top($"{calendar.Label(day)} 밤 — 승급 심사");
            ShowApplicant(applicants, 0, next);
        }

        void ShowApplicant(List<Adventurer> applicants, int i, Action done)
        {
            if (i >= applicants.Count)
            {
                ShowOnly(null);
                done();
                return;
            }
            var a = applicants[i];
            promotionView.Show(a, promotions, i, applicants.Count,
                approve => promotions.Decide(a, approve, stats),
                () => ShowApplicant(applicants, i + 1, done));
        }

        void ShowSheet()
        {
            posting = false;
            ShowOnly(nightView);
            Top($"{calendar.Label(day)} 밤 — 의뢰서 작성");
            nightView.ShowSheet(night.Current, night.Index, night.Count, regions.All, guild.Rank, database.monsters);
        }

        void OnSheetPosted()
        {
            if (posting) return;
            posting = true;
            var q = night.Post(nightView.ReadEntry());
            board.Add(q);
            stats.AddWritten(q);
            StartCoroutine(After(0.6f, () =>
            {
                if (!night.IsDone) ShowSheet();
                else BoardSetup();
            }));
        }

        // 선배가 따로 검토해서 붙인 의뢰 (기입은 정답대로). 주차가 갈수록 늘어난다
        void AddSeniorQuests()
        {
            var week = calendar.Week(day);
            var counts = config.seniorQuestsByWeek;
            int n = counts.Length == 0 ? 0 : counts[Mathf.Min(calendar.WeekOf(day), counts.Length) - 1];
            for (int i = 0; i < n; i++)
                board.Add(new Quest(generator.Create(week, regions.MonstersOf(guild.Rank)), bySenior: true, generated: true));
        }

        // 밤 마무리: 오늘 게시판 확인 (전부 붙는다) + 상점
        void BoardSetup()
        {
            AddSeniorQuests();
            ShowOnly(boardSetup);
            Top($"{calendar.Label(day)} 밤 — 내일 게시판");
            boardSetup.Show(board, OpenShop, StartDay);
        }

        void OpenShop() => boardSetup.shop.Open(shop, guild, calendar.WeekOf(day), () =>
            topBar.SetStats(guild.Funds, guild.Danger, guild.Reputation));

        // ───────── 낮 ─────────

        void StartDay()
        {
            var rules = new DayRules
            {
                Forgeries = ForgeryCatalog.From(calendar.UnlockedOn(day)),
                NewUnlocks = calendar.NewOn(day),
                TempRule = tempRule,
                DeathUnlocked = calendar.IsUnlocked(day, Unlock.Death),
                ManagedRegions = regions.Managed(guild.Rank).ToList(),
                OutsideRegions = regions.Unmanaged(guild.Rank).ToList(),
                EvidenceMonsters = regions.MonstersOf(guild.Rank).Concat(calendar.Week(day).monsterPool).Where(m => m != null).Distinct().ToList(),
            };
            fader.Play($"{calendar.Label(day)} · 낮", tempRule != null ? tempRule.Name : "", () =>
            {
                ShowOnly(dayView);
                AudioHub.Music(Bgm.Day);
                Top($"{calendar.Label(day)} 낮 — 모험가 창구");
                var c = new Counter(day, rules, dayView, guild, config, database, board, assignments, roster, visitors, resolver, shop);
                c.Reacted += delay => StartCoroutine(After(delay, c.Next));
                c.TimeSpent += seconds => dayTimer += seconds;
                counter = c;
                dayTimer = 0;
                dayLength = config.dayLength + (shop.Has(FacilityKind.Magnifier) ? config.magnifierSeconds : 0);
                bool urgent = calendar.WeekOf(day) >= 2 && UnityEngine.Random.value < config.urgentChance;
                urgentTime = urgent ? dayLength * config.urgentAt : 0;
                dayView.nomination.Close();
                c.Open();
            });
        }

        // 긴급 의뢰: 선배가 바로 기입한 의뢰서를 전령이 들고 온다
        void SendUrgent()
        {
            urgentTime = 0;
            var data = generator.Create(calendar.Week(day), regions.MonstersOf(guild.Rank));
            data.reward = Mathf.RoundToInt(data.reward * config.urgentRewardMultiplier / 10f) * 10;
            data.title = "[긴급] " + data.title;
            counter.QueueUrgent(new Quest(data, false, urgent: true, generated: true));
        }

        void EndDay()
        {
            if (counter == null || !counter.IsOpen) return;
            StopAllCoroutines();
            dayView.nomination.Close();
            var record = counter.Close();
            history.Add(record);
            stats.AddDay(record);
            stats.nominations += record.Nominations;
            stats.urgentHandled += record.UrgentHandled;
            stats.urgentMissed += record.UrgentMissed;
            counter = null;

            var steps = new List<Action<Action>>();
            steps.Add(next => fader.Play("영업 종료", calendar.Label(day), () => { ShowOnly(null); next(); }));
            foreach (var dead in record.Deaths.Where(d => d.IsNamed))
                steps.Add(next => storyView.Play($"추모 — {dead.Name}", Memorial(dead), LookOf, next));
            bool weekEnd = calendar.IsLastDayOfWeek(day);
            var tomorrowUnlocks = weekEnd ? new List<Unlock>() : calendar.NewOn(day + 1);
            steps.Add(next => ShowPage($"{calendar.Label(day)} 밤 — 오늘 결과 정리", $"{calendar.Label(day)} 영업 결과",
                ReportWriter.NightReport(record, guild, config, tomorrowUnlocks, null), weekEnd ? "주말 — 습격에 대비한다" : "밤 업무로", next));
            steps.Add(_ =>
            {
                if (weekEnd) Raid();
                else
                {
                    day++;
                    BeginNight();
                }
            });
            Run(steps);
        }

        List<StoryLine> Memorial(Adventurer dead) => database.memorial.Select(l => new StoryLine
        {
            speaker = l.speaker,
            text = l.text.Replace("{name}", dead.Name).Replace("{epithet}", dead.Epithet),
        }).ToList();

        // ───────── 주말 ─────────

        void Raid()
        {
            foreach (var a in assignments.Where(a => a.IsResolved && !a.IsReported)) a.CompleteReturn(guild);   // 주말엔 전원 귀환
            var week = calendar.Week(day);
            bool final = calendar.IsLastWeek(day);
            var result = raids.Resolve(week.raid, guild, roster, final, awakened);
            stats.AddRaid(result);

            bool rescued = false, lost = false;
            List<StoryLine> outcome;
            if (result.Grade == RaidGrade.Perfect) outcome = week.raid.perfect;
            else if (result.Grade == RaidGrade.Close) outcome = week.raid.close;
            else if (!grahamUsed)
            {
                grahamUsed = rescued = true;
                raids.ApplyFailure(guild);
                outcome = week.raid.fail;
            }
            else
            {
                lost = true;
                outcome = new List<StoryLine> { new() { text = "방책이 무너졌다. 이번에는 아무도 나서 주지 않았다." } };
            }

            var script = RaidResolver.Fill(week.raid.intro.Concat(outcome), result);
            ShowOnly(null);
            Top($"{calendar.WeekOf(day)}주차 주말 — 습격");
            AudioHub.Music(Bgm.Raid);
            fader.Play("주말", week.raid.title, () =>
            {
                storyView.Play(week.raid.title, script, LookOf, () =>
                {
                    if (lost) BadEnding(result);
                    else if (final) Ending(result);
                    else WeekSummary(result, rescued);
                });
                storyView.Shake();
            });
        }

        void WeekSummary(RaidResult result, bool rescued)
        {
            int week = calendar.WeekOf(day);
            var days = history.Skip(history.Count - calendar.DaysPerWeek).ToList();
            var recruits = roster.Recruit(guild.Reputation, shop.Has(FacilityKind.Flyer) ? config.flyerRecruits : 0);
            if (shop.Has(FacilityKind.Flyer)) guild.ChangeReputation(config.flyerReputation);
            string rankNote = TryRankUp();
            AudioHub.Music(Bgm.Night);
            ShowPage($"{week}주차 — 주간 정산", $"{week}주차를 넘겼다", rankNote + ReportWriter.WeekSummary(week, result, rescued, guild, days, recruits),
                $"{week + 1}주차로", () =>
                {
                    day++;
                    BeginNight();
                });
        }

        // 길드 등급 심사 (주간 정산): 평판이 기준에 닿으면 한 단계 상승 → 새 지역이 관할에 들어오고 그 지역 모험가가 등록한다
        string TryRankUp()
        {
            int i = guild.Rank - 1;
            if (guild.Rank >= regions.MaxRank || i >= config.rankUpReputation.Length) return "";
            int need = config.rankUpReputation[i];
            if (guild.Reputation < need)
                return $"<b>■ 길드 등급 심사</b>  {RegionMap.RankName(guild.Rank)}급 유지 <color=#7a6a5a>(다음 등급은 평판 {need} 이상)</color>\n\n";
            guild.RankUp();
            roster.SetGuildRank(guild.Rank);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b><color=#2f7a3e>■ 길드 등급 상승 — {RegionMap.RankName(guild.Rank)}급 길드</color></b>");
            foreach (var r in regions.OpenedAt(guild.Rank))
            {
                var joined = roster.RecruitFrom(r, r.newcomers);
                sb.AppendLine($"  새 관할 지역 <b>{r.displayName}</b> — {r.unlockNote}");
                sb.AppendLine($"  {r.displayName}의 모험가 {joined.Count}명 등록: {string.Join(", ", joined.Select(a => $"{a.Name}({Txt.R(a.Rank)})"))}");
                sb.AppendLine($"  <color=#7a6a5a>이 지역 몬스터 {r.monsters.Count}종이 의뢰에 나오기 시작한다. 도감 '{r.displayName}' 탭을 확인할 것.</color>");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        // ───────── 엔딩 ─────────

        EndingData FindEnding(EndingKind kind, int week = 0) =>
            database.endings.FirstOrDefault(e => e.kind == kind && (kind != EndingKind.Bad || e.week == week))
            ?? database.endings.FirstOrDefault(e => e.kind == kind);

        void Ending(RaidResult result)
        {
            var ending = FindEnding(result.Grade == RaidGrade.Perfect ? EndingKind.Good : EndingKind.Bitter);
            var lines = ending != null ? ending.lines : result.Grade == RaidGrade.Perfect ? database.endingGood : database.endingBitter;
            if (ending != null) SaveSystem.UnlockEnding(ending.id);
            SaveSystem.Delete();    // 한 판이 끝났다
            AudioHub.Music(Bgm.Ending);
            storyView.Play(ending != null ? ending.title : "엔딩", RaidResolver.Fill(lines, result), LookOf, () =>
                ShowPage("결산 — 5주의 기록", "로웰 모험가 길드, 다섯 번의 주말",
                    ReportWriter.Ending(guild, stats, roster, expedition, database.endings.Count), "타이틀로", ShowTitle));
        }

        // 두 번째 습격 실패: 주차별 배드 엔딩 → 재도전
        void BadEnding(RaidResult result)
        {
            int week = calendar.WeekOf(day);
            var ending = FindEnding(EndingKind.Bad, week);
            if (ending != null) SaveSystem.UnlockEnding(ending.id);
            AudioHub.Music(Bgm.Ending);
            var lines = ending != null ? RaidResolver.Fill(ending.lines, result) : new List<StoryLine> { new() { text = "로웰이 무너졌다." } };
            storyView.Play(ending != null ? ending.title : "배드 엔딩", lines, LookOf, () =>
            {
                bool canRetry = SaveSystem.HasSave;
                var options = new List<string>();
                if (canRetry) options.Add($"{week}주차 처음부터 다시");
                options.Add("처음부터 새로");
                options.Add("타이틀로");
                storyView.Ask("배드 엔딩", "", null, "……여기서 끝낼 수는 없다.", options, i =>
                {
                    if (!canRetry) i++;
                    if (i == 0) Continue();
                    else if (i == 1) NewGame();
                    else ShowTitle();
                });
            });
        }

        // ───────── 도우미 ─────────

        // 단계들을 차례로 실행 (각 단계는 끝나면 next를 부른다)
        static void Run(List<Action<Action>> steps, int i = 0)
        {
            if (i < steps.Count) steps[i](() => Run(steps, i + 1));
        }

        CharacterLook LookOf(string speaker)
        {
            if (speaker == database.seniorName) return database.seniorLook;
            return roster.All.FirstOrDefault(a => a.Name == speaker)?.Look
                   ?? database.npcLooks.FirstOrDefault(l => l.label == speaker);
        }

        void ShowOnly(Component view)
        {
            titleView.gameObject.SetActive(view == titleView);
            nightView.gameObject.SetActive(view == nightView);
            boardSetup.gameObject.SetActive(view == boardSetup);
            promotionView.gameObject.SetActive(view == promotionView);
            dayView.gameObject.SetActive(view == dayView);
            textPage.gameObject.SetActive(view == textPage);
            if (view != null) storyView.gameObject.SetActive(false);
        }

        void Top(string title)
        {
            topBar.gameObject.SetActive(true);
            topBar.Set($"<color=#d9b36a>[{RegionMap.RankName(guild.Rank)}급 길드]</color> {title}", guild.Funds, guild.Danger, guild.Reputation);
            topBar.SetClock("", -1);
        }

        void ShowPage(string barTitle, string heading, string body, string button, Action next)
        {
            ShowOnly(textPage);
            Top(barTitle);
            textPage.Show(heading, body, button, next);
        }

        IEnumerator After(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }
    }
}
