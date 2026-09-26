using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 그날 창구에 적용되는 규칙 묶음
    public class DayRules
    {
        public IReadOnlyList<IForgery> Forgeries;
        public IReadOnlyList<Unlock> NewUnlocks;
        public ITempRule TempRule;
        public bool DeathUnlocked;
        public IReadOnlyList<RegionData> ManagedRegions;   // 관할 지역 (관할 검사 · 규정집의 지역 마크)
        public IReadOnlyList<MonsterData> EvidenceMonsters; // 규정집 증거 부위표에 싣는 몬스터
        public IReadOnlyList<RegionData> OutsideRegions;    // 관할 밖 지역 (그 지역 모험가가 가끔 찾아온다)
    }

    // 하루치 낮 창구 영업. 방문객을 차례로 받고, 마감 때 결과 판정과 정산을 한다.
    public class Counter
    {
        public int Day { get; }
        public DayView View { get; }
        public Guild Guild { get; }
        public GameConfig Config { get; }
        public DayRecord Record { get; }
        public Sprite ProofIcon { get; }
        public FacilityShop Facilities { get; }
        public ITempRule TempRule => rules.TempRule;
        public Visit Current { get; private set; }
        public bool IsOpen { get; private set; }

        // 모험가가 반응한 뒤 다음 방문객까지 기다릴 시간 (GuildGame이 받아서 Next를 예약)
        public event Action<float> Reacted;
        // 지명 등으로 영업 시간을 썼다 (초)
        public event Action<float> TimeSpent;

        readonly DayRules rules;
        readonly GameDatabase database;
        readonly List<Quest> board;
        readonly List<Assignment> assignments;
        readonly AdventurerRoster roster;
        readonly VisitorFactory visitors;
        readonly QuestResolver resolver;
        readonly Queue<Assignment> returns = new();
        readonly HashSet<Adventurer> refusedNomination = new();
        readonly List<Adventurer> hallToday = new();
        Quest pendingUrgent;
        bool busy;

        public Counter(int day, DayRules rules, DayView view, Guild guild, GameConfig config, GameDatabase database,
            List<Quest> board, List<Assignment> assignments, AdventurerRoster roster, VisitorFactory visitors, QuestResolver resolver,
            FacilityShop facilities)
        {
            Day = day;
            this.rules = rules;
            View = view;
            Guild = guild;
            Config = config;
            this.database = database;
            this.board = board;
            this.assignments = assignments;
            this.roster = roster;
            this.visitors = visitors;
            this.resolver = resolver;
            Facilities = facilities;
            ProofIcon = database.proofIcon;
            Record = new DayRecord(day);
        }

        List<Quest> Posted => board.ToList();   // 게시판엔 모든 의뢰가 붙는다

        // 수주 신청 규정 검사 (임시 규칙 · 위조 · 관할 · 등급 · 역할). 문제 없으면 null
        public string Check(IReadOnlyList<GuildCard> cards, Quest q) =>
            Regulations.CheckApplication(cards, q, TempRule, rules.ManagedRegions?.Select(r => r.displayName).ToList());

        public void Open()
        {
            roster.NewDay(Facilities.Has(FacilityKind.Lounge) ? 2 : 1);
            hallToday.AddRange(Nomination.PresentToday(roster, Config.hallNamedChance, Config.hallNamelessCount));
            foreach (var a in assignments.Where(a => a.IsResolved && !a.IsReported)) returns.Enqueue(a);
            View.SetRules(rules.Forgeries, rules.NewUnlocks, rules.TempRule, rules.EvidenceMonsters, rules.ManagedRegions);
            View.RefreshBoard(Posted);
            IsOpen = true;
            Next();
        }

        public void Next()
        {
            if (!IsOpen) return;
            busy = false;
            var open = board.Where(q => q.IsOpen).ToList();
            var urgent = open.Where(q => q.IsUrgent).ToList();
            if (pendingUrgent != null)
            {
                Current = new UrgentVisit(pendingUrgent, database.urgentMessenger, MessengerLook);
                pendingUrgent = null;
            }
            else if (returns.Count > 0) Current = new ReturnVisit(returns.Dequeue());
            else if (open.Count > 0)
            {
                var q = urgent.Count > 0 && UnityEngine.Random.value < 0.5f ? urgent[0] : open[UnityEngine.Random.Range(0, open.Count)];
                Current = visitors.Create(q, rules.Forgeries, rules.TempRule, Guild.Reputation, rules.OutsideRegions);
            }
            else Current = new ChatVisit(roster.AnyIdle(preferNamed: true));

            View.SetQueue(returns.Count + open.Count(q => q != Current.Quest));
            View.SetCards(Current.Cards);
            Current.Present(this);
            RefreshStats();
        }

        CharacterLook MessengerLook =>
            database.npcLooks.FirstOrDefault(l => l.label == database.urgentMessenger) ?? database.seniorLook;

        public void Approve() => Act(v => v.Approve(this));
        public void Reject() => Act(v => v.Reject(this));
        public void Confirm() => Act(v => v.Confirm(this));
        public void Report() => Act(v => v.Report(this));
        public void Bonus() => Act(v => v.Bonus(this));

        void Act(Func<Visit, string> action)
        {
            if (!IsOpen || busy || Current == null) return;
            string line = action(Current);
            if (line == null) return;               // 이 방문에선 쓸 수 없는 동작
            busy = true;
            View.SetMode(DayView.Mode.Waiting);
            View.DismissDocuments();
            if (line.Length > 0) View.Say(line);
            RefreshStats();
            Reacted?.Invoke(Config.reactionDelay);
        }

        // 수주 승인 (ApplicationVisit · 지명이 호출)
        public void Accept(Assignment a)
        {
            assignments.Add(a);
            Record.Approved++;
            Record.Sent++;
            View.RefreshBoard(Posted);
        }

        // ───── 긴급 의뢰 ─────

        public bool HasUrgentToday { get; private set; }

        // 전령이 도착했다 (다음 방문객으로 들어온다)
        public void QueueUrgent(Quest q)
        {
            HasUrgentToday = true;
            pendingUrgent = q;
        }

        public void PostUrgent(Quest q)
        {
            board.Add(q);
            View.RefreshBoard(Posted);
        }

        // ───── 길드 홀 지명 ─────

        public List<Quest> NominableQuests => board.Where(q => q.IsOpen && q != Current?.Quest).ToList();

        public int NominationsLeft => Config.nominationsPerDay - Record.Nominations;

        // 오늘 길드 홀에 나와 있는 사람 중 지금 지명할 수 있는 사람
        public List<Adventurer> NominationCandidates(int max)
        {
            var atDesk = new HashSet<Adventurer>(Current != null ? Current.Cards.Select(c => c.Holder) : Enumerable.Empty<Adventurer>());
            return hallToday.Where(a => a.IsAvailable && !refusedNomination.Contains(a) && !atDesk.Contains(a))
                .OrderByDescending(a => a.IsNamed).ThenByDescending(a => a.Rank).ThenBy(a => a.Name).Take(max).ToList();
        }

        public string Haggle(Nomination n)
        {
            string line = n.Haggle();
            if (n.WalkedOff) refusedNomination.Add(n.Who);
            TimeSpent?.Invoke(Config.nominationSeconds * 0.5f);
            return line;
        }

        public bool CanAfford(Nomination n) => Guild.Funds >= n.Ask;

        // 지명 확정: 수당은 그 자리에서 길드 자금으로 낸다. 규정 위반이면 창구 승인과 똑같이 기록된다
        public string AcceptNomination(Nomination n)
        {
            if (NominationsLeft <= 0 || !CanAfford(n)) return null;
            var cards = new List<GuildCard> { GuildCard.Of(n.Who) };
            string why = Check(cards, n.Quest);
            var a = new Assignment(n.Quest, new List<Adventurer> { n.Who }, why, Day);
            Accept(a);
            Record.Nominations++;
            if (n.Ask > 0)
            {
                Guild.Earn(-n.Ask);
                Record.Ledger.AddPaid($"지명 수당 — {n.Who.Name} → '{n.Quest.Title}'", -n.Ask);
                View.Float($"수당 -{n.Ask}G", false);
            }
            if (why != null) Record.Violations.Add($"(지명) {n.Who.Name} → {n.Quest.Title}: {why}");
            else n.Who.ChangeAffinity(3);
            TimeSpent?.Invoke(Config.nominationSeconds);
            View.SetQueue(returns.Count + board.Count(q => q.IsOpen && q != Current?.Quest));
            return n.Who.Line(LineKind.Thanks, Lines.Thanks);
        }

        void RefreshStats() => View.SetStats(Record.Approved, Record.Rejected, returns.Count);

        // 영업 마감: 못 받은 귀환 보고 처리 → 오늘 보낸 의뢰 판정(사망 포함) → 위험도·기한 → 정산
        public DayRecord Close()
        {
            IsOpen = false;
            while (returns.Count > 0)
            {
                var a = returns.Dequeue();
                a.CompleteReturn(Guild);
                Record.Ledger.Add($"'{a.Quest.Title}' 귀환 보고를 마감까지 받지 못함", reputation: Config.repMissedReturn);
            }

            Record.DangerBefore = Guild.Danger;
            foreach (var a in assignments.Where(a => a.DayAccepted == Day && !a.IsResolved))
            {
                resolver.Resolve(a, rules.DeathUnlocked);
                if (a.Quest.IsUrgent) Record.UrgentHandled++;
                if (a.Casualties.Count == 0) continue;
                Record.Deaths.AddRange(a.Casualties);
                foreach (var d in a.Casualties)
                    Record.Ledger.Add($"'{a.Quest.Title}' — {d.Name}({Txt.R(d.Rank)} {Txt.J(d.Job)}) 사망", reputation: Config.repDeath);
                if (a.AllDead) a.CompleteReturn(Guild);     // 돌아와 보고할 사람이 없다
            }

            Guild.ChangeDanger(Config.dailyDangerRise);
            foreach (var q in board.Where(q => !q.IsTaken))
            {
                if (!q.PassDay()) continue;
                Record.Expired.Add(q);
                if (q.IsUrgent)
                {
                    Record.UrgentMissed++;
                    Guild.ChangeDanger(Config.urgentMissedDanger);
                    Record.Ledger.Add($"긴급 의뢰 '{q.Title}'를 아무에게도 맡기지 못함 — 피해가 번졌다", reputation: Config.urgentMissedReputation);
                    continue;
                }
                Guild.ChangeDanger(Config.expiredDangerRise);
                Record.Ledger.Add($"'{q.Title}' 기한 초과", reputation: Config.repExpired);
            }
            board.RemoveAll(q => q.IsTaken || q.Deadline <= 0);

            foreach (var v in Record.Violations) Record.Ledger.Add($"규정 위반 승인: {v}", reputation: Config.repViolation);

            Guild.Settle(Record.Ledger);
            Record.DangerAfter = Guild.Danger;
            return Record;
        }
    }
}
