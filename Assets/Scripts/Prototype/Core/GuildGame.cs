using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuildProto
{
    // 게임 조립 + 진행 흐름
    //  인트로(스토리) → [밤: (개인 이벤트) → 결과 정리 → 의뢰서 작성 → 게시판 정리 → 낮: 창구] × N일 → 주말 습격 → 결산
    // 규칙과 상태는 도메인 객체와 서비스가 맡고, 여기서는 어느 화면을 언제 띄울지만 정한다.
    public class GuildGame : MonoBehaviour
    {
        [Header("데이터 (몬스터 · 의뢰 · 모험가 · 스토리)")]
        public GameDatabase database;

        [Header("화면 (Core 씬)")]
        public TopBarView topBar;
        public TextPageView textPage;
        public StoryView storyView;

        [Header("화면 씬 (시작할 때 함께 불러온다)")]
        public string nightScene = "Night";
        public string dayScene = "Day";

        // 다른 씬의 화면은 인스펙터로 연결할 수 없어서, 씬을 불러온 뒤 찾아서 연결한다
        [NonSerialized] public NightView nightView;
        [NonSerialized] public DayView dayView;

        [Header("밸런스")]
        public GameConfig config = new();

        Guild guild;
        AdventurerRoster roster;
        VisitorFactory visitors;
        QuestResolver resolver;
        RaidResolver raids;

        readonly List<Quest> board = new();
        readonly List<Quest> written = new();
        readonly List<Assignment> assignments = new();
        readonly List<DayRecord> history = new();

        int day;
        NightShift night;
        Counter counter;
        RaidResult raid;
        float dayTimer;
        bool posting;

        IEnumerator Start()
        {
            yield return LoadView<NightView>(nightScene, v => nightView = v);
            yield return LoadView<DayView>(dayScene, v => dayView = v);

            nightView.Posted += OnSheetPosted;
            dayView.approveButton.onClick.AddListener(() => counter?.Approve());
            dayView.rejectButton.onClick.AddListener(() => counter?.Reject());
            dayView.confirmButton.onClick.AddListener(() => counter?.Confirm());
            dayView.closeDayButton.onClick.AddListener(EndDay);
            NewGame();
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
            float minutes = Mathf.Lerp(9 * 60, 18 * 60, dayTimer / config.dayLength);
            topBar.SetClock($"{(int)minutes / 60:00}:{(int)minutes % 60:00}", 1 - dayTimer / config.dayLength);
            topBar.SetStats(guild.Funds, guild.Danger, guild.Reputation);
            if (dayTimer >= config.dayLength) EndDay();
        }

        // ───────── 흐름 ─────────

        void NewGame()
        {
            StopAllCoroutines();
            counter = null;
            raid = null;
            guild = new Guild(config.startFunds, config.startDanger, config.startReputation);
            roster = new AdventurerRoster(database);
            visitors = new VisitorFactory(roster, config, database.namedVisitChance);
            resolver = new QuestResolver(config);
            raids = new RaidResolver(config);
            board.Clear();
            board.AddRange(database.seniorQuests.Select(d => new Quest(d, true)));
            written.Clear();
            assignments.Clear();
            history.Clear();
            day = 1;

            ShowOnly(null);
            Top("프롤로그");
            storyView.Play("프롤로그 — 로웰 모험가 길드", database.intro, LookOf, StartNight);
        }

        void StartNight()
        {
            var sets = database.nights;
            night = new NightShift(day, sets[Mathf.Clamp(day - 1, 0, sets.Count - 1)].sheets);
            if (history.Count == 0) ShowSheet();
            else PlayPersonalEvent(ShowNightReport);
        }

        // 호감도가 닿은 네임드 개인 이벤트 (하룻밤에 하나)
        void PlayPersonalEvent(Action next)
        {
            var who = roster.Named.Where(a => a.PendingEvent() != null).OrderByDescending(a => a.Affinity).FirstOrDefault();
            if (who == null)
            {
                next();
                return;
            }
            var ev = who.PendingEvent();
            who.MarkSeen(ev);
            ShowOnly(null);
            Top($"{day}일차 밤 — 개인 이벤트");
            storyView.Play($"{who.Name} — {ev.title}", ev.lines, LookOf, next);
        }

        void ShowNightReport() =>
            ShowPage($"{day}일차 밤 — 오늘 결과 정리", $"{day - 1}일차 영업 결과",
                ReportWriter.NightReport(history[history.Count - 1], guild, config, ForgeryCatalog.NewOn(day)), "의뢰서 작성하기", ShowSheet);

        void ShowSheet()
        {
            posting = false;
            ShowOnly(nightView);
            Top($"{day}일차 밤 — 의뢰서 작성");
            nightView.ShowSheet(night.Current, night.Index, night.Count, database.monsters);
        }

        void OnSheetPosted()
        {
            if (posting) return;
            posting = true;
            var q = night.Post(nightView.ReadEntry());
            board.Add(q);
            written.Add(q);
            StartCoroutine(After(0.6f, () =>
            {
                if (!night.IsDone) ShowSheet();
                else ShowPage($"{day}일차 밤 — 게시판 정리", "내일 게시판", ReportWriter.BoardSummary(board), "아침이 밝았다 — 창구 열기", StartDay);
            }));
        }

        void StartDay()
        {
            ShowOnly(dayView);
            Top($"{day}일차 낮 — 모험가 창구");
            var c = new Counter(day, dayView, guild, config, database, board, assignments, roster, visitors, resolver);
            c.Reacted += delay => StartCoroutine(After(delay, c.Next));
            counter = c;
            dayTimer = 0;
            c.Open();
        }

        void EndDay()
        {
            if (counter == null || !counter.IsOpen) return;
            StopAllCoroutines();
            history.Add(counter.Close());
            counter = null;

            if (day < config.lastDay)
            {
                day++;
                StartNight();
            }
            else PlayRaid();
        }

        // 주말 습격 → 결산
        void PlayRaid()
        {
            foreach (var a in assignments.Where(a => a.IsResolved && !a.IsReported)) a.CompleteReturn(guild);   // 주말엔 전원 귀환
            raid = raids.Resolve(guild, roster);
            var outcome = raid.Grade switch
            {
                RaidGrade.Perfect => database.raidPerfect,
                RaidGrade.Close => database.raidClose,
                _ => database.raidFail,
            };
            var script = RaidResolver.Fill(database.raidIntro.Concat(outcome), raid);
            ShowOnly(null);
            Top("주말 — 습격");
            storyView.Play("주말 습격 — 늑대 무리의 남하", script, LookOf, ShowEnding);
        }

        void ShowEnding() =>
            ShowPage("결산 — 1주차 종료", $"1주차 기록 ({config.lastDay}일)",
                ReportWriter.Ending(guild, config, history, written, assignments.Where(a => a.DayAccepted == day), raid, roster),
                "처음부터 다시", NewGame);

        // ───────── 화면 전환 ─────────

        CharacterLook LookOf(string speaker)
        {
            if (speaker == database.seniorName) return database.seniorLook;
            return roster.All.FirstOrDefault(a => a.Name == speaker)?.Look
                   ?? database.npcLooks.FirstOrDefault(l => l.label == speaker);
        }

        void ShowOnly(Component view)
        {
            nightView.gameObject.SetActive(view == nightView);
            dayView.gameObject.SetActive(view == dayView);
            textPage.gameObject.SetActive(view == textPage);
            if (view != null) storyView.gameObject.SetActive(false);
        }

        void Top(string title)
        {
            topBar.Set(title, guild.Funds, guild.Danger, guild.Reputation);
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
