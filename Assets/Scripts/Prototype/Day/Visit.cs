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
        public virtual string Report(Counter c) => null;      // 신고 종 (수배 주간)
        public virtual string Bonus(Counter c) => null;       // 완료 승인 + 성공 보너스
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
            string why = c.Check(Cards, quest);
            var a = new Assignment(quest, Cards.Select(x => x.Holder).ToList(), why, c.Day);
            c.Accept(a);
            if (why != null) c.Record.Violations.Add($"{a.MemberNames} → {quest.Title}: {why}");
            else
            {
                foreach (var m in a.Members) m.ChangeAffinity(2);     // 적절한 배정
                c.View.RefreshHearts(Speaker);
            }
            var crook = Cards.FirstOrDefault(x => x.Holder.Criminal != null);
            if (crook != null)
                c.Record.Ledger.Add($"수배자 {crook.Holder.Criminal.name}에게 의뢰를 넘김 — 의뢰인 물건이 사라졌다", reputation: c.Config.repWantedApproved);
            if (Cards.Any(x => x.Holder.IsSick))
            {
                c.Record.Ledger.Add($"증상 있는 모험가를 내보냄 — 마을에 병이 번졌다", reputation: c.Config.repSickApproved);
                c.Guild.ChangeDanger(2);
            }
            c.View.StampQuest(true);
            c.View.CharacterHappy();
            return Speaker.Line(LineKind.Thanks, Lines.Thanks);
        }

        public override string Report(Counter c)
        {
            if (c.TempRule == null || !c.TempRule.AllowsReport) return null;
            c.Record.Rejected++;
            c.View.StampQuest(false);
            var crook = Cards.FirstOrDefault(x => x.Holder.Criminal != null);
            if (crook != null)
            {
                int bounty = crook.Holder.Criminal.bounty;
                c.Guild.Earn(bounty);
                c.Record.Fees += bounty;
                c.Record.Ledger.Add($"수배자 {crook.Holder.Criminal.name} 신고 — 현상금 +{bounty}G", reputation: 2);
                c.View.Float($"현상금 +{bounty}G", true);
                c.View.CharacterUpset();
                return "쳇, 경비대?! 이, 이거 놔!";
            }
            foreach (var card in Cards) card.Holder.ChangeAffinity(-10);
            c.Record.Ledger.Add($"죄 없는 {Leader.Name}을(를) 신고함", reputation: c.Config.repWrongReport);
            c.View.CharacterUpset();
            return "네?! 저 아니라니까요! 얼굴 좀 보시라고요!";
        }

        public override string Reject(Counter c)
        {
            string why = c.Check(Cards, quest);
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

        public ReturnVisit(Assignment a) : base(a.Survivors.Select(GuildCard.Of).ToList(), a.Report) => assignment = a;

        public override void Present(Counter c)
        {
            c.View.ShowVisitor(Leader, Line, "귀환 보고 — 완료 심사");
            c.View.ShowReport(assignment, c.ProofIcon, c.Facilities.Has(FacilityKind.Scale));
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
            c.View.Float($"+{c.Config.feePerQuest}G", true);

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

        // 완료 승인 + 길드가 얹어 주는 성공 보너스 (호감도가 크게 오른다. 허위 보고에 주면 돈만 날린다)
        public override string Bonus(Counter c)
        {
            int gold = c.Config.successBonusGold;
            if (!assignment.ClaimsSuccess) return null;
            if (c.Guild.Funds < gold)
            {
                c.View.Float("자금 부족", false);
                return null;
            }
            Approve(c);
            c.Guild.Earn(-gold);
            c.Record.Bonuses++;
            c.Record.Ledger.AddPaid($"성공 보너스 — {assignment.MemberNames}", -gold);
            foreach (var m in assignment.Survivors) m.ChangeAffinity(c.Config.successBonusAffinity);
            c.View.RefreshHearts(Speaker);
            c.View.Float($"보너스 -{gold}G", false);
            return assignment.IsLying ? "헤헤… 보, 보너스까지요? (눈을 피한다)" : "보너스까지?! 역시 여기 접수원이 최고예요!";
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
            return assignment.Casualties.Count > 0 ? "……." : "…다음엔 꼭 해낼게요.";
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
            Speaker.ChangeAffinity(c.Facilities.Has(FacilityKind.Lounge) ? 6 : 3);   // 휴게실이 있으면 더 오래 머문다
            c.View.CharacterHappy();
            return "또 올게요~";
        }
    }
}
