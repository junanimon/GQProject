using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 하루치 낮 창구 영업. 방문객을 차례로 받고, 마감 때 결과 판정과 정산을 한다.
    public class Counter
    {
        public int Day { get; }
        public DayView View { get; }
        public Guild Guild { get; }
        public GameConfig Config { get; }
        public DayRecord Record { get; }
        public Sprite ProofIcon { get; }
        public Visit Current { get; private set; }
        public bool IsOpen { get; private set; }

        // 모험가가 반응한 뒤 다음 방문객까지 기다릴 시간 (GuildGame이 받아서 Next를 예약)
        public event Action<float> Reacted;

        readonly GameDatabase database;
        readonly List<Quest> board;
        readonly List<Assignment> assignments;
        readonly AdventurerRoster roster;
        readonly VisitorFactory visitors;
        readonly QuestResolver resolver;
        readonly IReadOnlyList<IForgery> forgeries;
        readonly Queue<Assignment> returns = new();
        bool busy;

        public Counter(int day, DayView view, Guild guild, GameConfig config, GameDatabase database,
            List<Quest> board, List<Assignment> assignments, AdventurerRoster roster, VisitorFactory visitors, QuestResolver resolver)
        {
            Day = day;
            View = view;
            Guild = guild;
            Config = config;
            this.database = database;
            this.board = board;
            this.assignments = assignments;
            this.roster = roster;
            this.visitors = visitors;
            this.resolver = resolver;
            ProofIcon = database.proofIcon;
            forgeries = ForgeryCatalog.UnlockedOn(day);
            Record = new DayRecord(day);
        }

        public void Open()
        {
            roster.NewDay();
            foreach (var a in assignments.Where(a => a.IsResolved && !a.IsReported)) returns.Enqueue(a);
            View.SetRules(forgeries, ForgeryCatalog.NewOn(Day), database.monsters);
            View.RefreshBoard(board);
            IsOpen = true;
            Next();
        }

        public void Next()
        {
            if (!IsOpen) return;
            busy = false;
            var open = board.Where(q => !q.IsTaken).ToList();
            if (returns.Count > 0) Current = new ReturnVisit(returns.Dequeue());
            else if (open.Count > 0) Current = visitors.Create(open[UnityEngine.Random.Range(0, open.Count)], forgeries);
            else Current = new ChatVisit(roster.AnyIdle(preferNamed: true));

            View.SetQueue(returns.Count + board.Count(q => !q.IsTaken && q != Current.Quest));
            View.SetCards(Current.Cards);
            Current.Present(this);
            RefreshStats();
        }

        public void Approve() => Act(v => v.Approve(this));
        public void Reject() => Act(v => v.Reject(this));
        public void Confirm() => Act(v => v.Confirm(this));

        void Act(Func<Visit, string> action)
        {
            if (!IsOpen || busy || Current == null) return;
            string line = action(Current);
            if (line == null) return;               // 이 방문에선 쓸 수 없는 동작
            busy = true;
            View.SetMode(DayView.Mode.Waiting);
            if (line.Length > 0) View.Say(line);
            RefreshStats();
            Reacted?.Invoke(Config.reactionDelay);
        }

        // 수주 승인 (ApplicationVisit이 호출)
        public void Accept(Assignment a)
        {
            assignments.Add(a);
            Record.Approved++;
            Record.Sent++;
            View.RefreshBoard(board);
        }

        void RefreshStats() => View.SetStats(Record.Approved, Record.Rejected, returns.Count);

        // 영업 마감: 못 받은 귀환 보고 처리 → 오늘 보낸 의뢰 판정 → 위험도·기한 → 정산
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
            foreach (var a in assignments.Where(a => a.DayAccepted == Day && !a.IsResolved)) resolver.Resolve(a);

            Guild.ChangeDanger(Config.dailyDangerRise);
            foreach (var q in board.Where(q => !q.IsTaken))
            {
                if (!q.PassDay()) continue;
                Record.Expired.Add(q);
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
