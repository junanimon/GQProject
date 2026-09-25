using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 창구에 온 방문객. 종류마다 보여주는 서류와 승인/거절/확인 때 하는 일이 다르다.
    // 각 동작은 모험가의 반응 대사를 돌려준다 (null = 이 방문에선 쓸 수 없는 동작, "" = 대사 없음).
    public abstract class Visit
    {
        public IReadOnlyList<GuildCard> Cards { get; }
        public string Line { get; }
        public GuildCard Leader => Cards[0];
        protected Adventurer Speaker => Leader.Holder;

        protected Visit(IReadOnlyList<GuildCard> cards, string line)
        {
            Cards = cards;
            Line = line;
        }

        public virtual Quest Quest => null;
        public abstract void Present(Counter c);
        public virtual string Approve(Counter c) => null;
        public virtual string Reject(Counter c) => null;
        public virtual string Confirm(Counter c) => null;
    }

    // 수주 신청: 길드 카드 + 게시본 의뢰서
    public class ApplicationVisit : Visit
    {
        readonly Quest quest;
        public override Quest Quest => quest;

        public ApplicationVisit(Quest quest, IReadOnlyList<GuildCard> cards, string line) : base(cards, line) => this.quest = quest;

        public override void Present(Counter c)
        {
            c.View.ShowVisitor(Leader, Line, Cards.Count > 1 ? $"수주 신청 — 파티 {Cards.Count}명" : "수주 신청");
            c.View.ShowQuestDoc(quest, Cards.Count);
            c.View.SetMode(DayView.Mode.QuestReview);
        }

        public override string Approve(Counter c)
        {
            string why = Regulations.CheckApplication(Cards, quest);
            var a = new Assignment(quest, Cards.Select(x => x.Holder).ToList(), why, c.Day);
            c.Accept(a);
            if (why != null) c.Record.Violations.Add($"{a.MemberNames} → {quest.Title}: {why}");
            else foreach (var m in a.Members) m.ChangeAffinity(2);     // 적절한 배정
            c.View.StampQuest(true);
            c.View.CharacterHappy();
            return Speaker.Line(LineKind.Thanks, Lines.Thanks);
        }

        public override string Reject(Counter c)
        {
            string why = Regulations.CheckApplication(Cards, quest);
            c.Record.Rejected++;
            c.View.StampQuest(false);
            c.View.CharacterUpset();
            if (why == null)
            {
                c.Record.WrongRejects++;
                foreach (var card in Cards) card.Holder.ChangeAffinity(-6);
                return Speaker.Line(LineKind.Rejected, Lines.WrongReject);
            }
            foreach (var card in Cards) card.Holder.ChangeAffinity(-1);
            return Speaker.Line(LineKind.Caught, Lines.Caught);
        }
    }

    // 귀환 보고: 완료 보고서 + 증거물. 기입이 틀렸던 의뢰나 허위 보고는 밤 정산에서 패널티
    public class ReturnVisit : Visit
    {
        readonly Assignment assignment;
        public override Quest Quest => assignment.Quest;

        public ReturnVisit(Assignment a) : base(a.Members.Select(GuildCard.Of).ToList(), a.Report) => assignment = a;

        public override void Present(Counter c)
        {
            c.View.ShowVisitor(Leader, Line, "귀환 보고 — 완료 심사");
            c.View.ShowReport(assignment, c.ProofIcon);
            if (assignment.ClaimsSuccess) c.View.SetMode(DayView.Mode.ReportReview);
            else c.View.SetMode(DayView.Mode.Confirm, "실패 확인");
        }

        public override string Approve(Counter c)
        {
            if (!assignment.ClaimsSuccess) return null;
            var q = assignment.Quest;
            c.Record.Approved++;
            c.Record.Fees += c.Config.feePerQuest;
            c.Guild.Earn(c.Config.feePerQuest);
            assignment.CompleteReturn(c.Guild);
            c.View.StampReport(true);
            c.View.CharacterHappy();

            if (assignment.IsLying)
                c.Record.Ledger.Add($"허위 보고 승인 — '{q.Title}': {assignment.MemberNames}{Txt.Josa(assignment.MemberNames, "은", "는")} 실제로 의뢰에 실패했다. 의뢰인이 몬스터가 아직 있다고 항의",
                    -c.Config.feePerQuest, c.Config.repPenalty);
            else if (q.EntryWrong)
                c.Record.Ledger.Add($"잘못된 의뢰 성공 패널티 — '{q.Title}': 실제로는 {Txt.Describe(q.Truth, q.TruthCount)}" +
                                    (q.Truth.isMonster ? $" {q.TruthNumber}마리" : "") +
                                    $" (권장 {Txt.R(q.TruthRank)} / 역할 {Txt.J(q.Truth.role)}). 잘못 책정된 의뢰로 의뢰인 항의",
                    -c.Config.feePerQuest, c.Config.repPenalty);
            else
                c.Record.Ledger.Add($"'{q.Title}' 완료", reputation: assignment.Result == Result.Great ? c.Config.repGreat : c.Config.repSuccess);

            return assignment.IsLying ? "헤헤, 감사합니다~" : Speaker.Line(LineKind.Thanks, Lines.Thanks);
        }

        public override string Reject(Counter c)
        {
            if (!assignment.ClaimsSuccess) return null;
            c.Record.Rejected++;
            assignment.CompleteReturn(c.Guild);
            c.View.StampReport(false);
            c.View.CharacterUpset();
            if (assignment.IsLying)
            {
                foreach (var m in assignment.Members) m.ChangeAffinity(-3);
                return Speaker.Line(LineKind.Caught, Lines.Caught);
            }
            c.Record.WrongRejects++;
            foreach (var m in assignment.Members) m.ChangeAffinity(-8);
            c.Record.Ledger.Add($"정상 완료 보고를 반려함 — '{assignment.Quest.Title}'", reputation: c.Config.repWrongReject);
            return "네?! 증거물 다 가져왔잖아요!";
        }

        public override string Confirm(Counter c)
        {
            if (assignment.ClaimsSuccess) return null;
            assignment.CompleteReturn(c.Guild);
            c.Record.Ledger.Add($"'{assignment.Quest.Title}' 의뢰 실패", reputation: c.Config.repFail);
            c.View.CharacterUpset();
            return "…다음엔 꼭 해낼게요.";
        }
    }

    // 게시판이 비었을 때 들르는 잡담 (네임드가 먼저 온다)
    public class ChatVisit : Visit
    {
        public ChatVisit(Adventurer a) : base(new List<GuildCard> { GuildCard.Of(a) }, a.Line(LineKind.Chat, Lines.Chat)) { }

        public override void Present(Counter c)
        {
            c.View.ShowVisitor(Leader, Line, "잡담 — 게시판이 비었어요. '영업 마감'으로 하루를 끝낼 수 있어요.");
            c.View.HideDocuments();
            c.View.SetMode(DayView.Mode.Confirm, "잡담 마치기");
        }

        public override string Confirm(Counter c)
        {
            Speaker.ChangeAffinity(3);
            c.View.CharacterHappy();
            return "또 올게요~";
        }
    }
}
