using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuildProto
{
    // 종이 화면(인트로 · 게시판 정리 · 밤 결과 · 결산)에 들어갈 글
    public static class ReportWriter
    {
        public static string BoardSummary(IEnumerable<Quest> board)
        {
            var sb = new StringBuilder("밤 업무를 마쳤습니다. 내일 창구에서는 이 기입란을 기준으로 수주와 완료를 심사합니다.\n\n");
            foreach (var q in board)
            {
                sb.AppendLine($"<b>{q.Title}</b>{(q.BySenior ? "  <color=#7a6a5a>(선배 작성)</color>" : "")}");
                sb.AppendLine($"    {Txt.Describe(q.Entry.Monster, q.Entry.Count)} · 권장 {Txt.R(q.Entry.Rank)} · 역할 {Txt.J(q.Entry.Role)} · 보상 {q.Reward}G · 기한 {q.Deadline}일\n");
            }
            return sb.ToString();
        }

        public static string NightReport(DayRecord r, Guild guild, GameConfig c, IForgery newForgery)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"처리: 승인 {r.Approved}건 / 반려·거절 {r.Rejected}건 · 받은 수수료 {r.Fees}G");
            sb.AppendLine($"오늘 보낸 모험가 {r.Sent}팀 — 내일 창구로 돌아와 보고합니다.\n");

            sb.AppendLine("<b>■ 정산 (자금 · 평판)</b>");
            if (r.Ledger.Entries.Count == 0) sb.AppendLine("변동 없음.");
            foreach (var e in r.Ledger.Entries)
                sb.AppendLine($"  · {e.Text}  <color={(e.IsLoss ? "#b02c28" : "#2f7a3e")}><b>{Delta(e)}</b></color>");
            if (r.WrongRejects > 0) sb.AppendLine($"  · 규정상 문제없는 모험가를 돌려보냄 {r.WrongRejects}건 (호감도 하락)");
            sb.AppendLine();

            sb.AppendLine($"<b>■ 마을 위험도</b> {r.DangerBefore} → {r.DangerAfter}  (매일 +{c.dailyDangerRise})");
            sb.AppendLine($"<b>■ 길드 평판</b> {guild.Reputation}\n");

            if (newForgery != null)
            {
                sb.AppendLine("<b>■ 공문 — 내일부터 시행</b>");
                sb.AppendLine($"<color=#b02c28>{newForgery.RuleLine}</color>");
                sb.AppendLine("최근 위조 카드로 등급을 속이는 사례가 늘고 있다. 창구에서 반드시 확인할 것.\n");
            }

            bool clean = r.Ledger.IsClean && r.WrongRejects == 0;
            sb.AppendLine($"<b>선배:</b> {(clean ? "\"깔끔하네. 이 정도면 믿고 맡겨도 되겠어.\"" : "\"항의가 들어오면 우리 길드 얼굴에 먹칠이야. 기입란부터 꼼꼼히.\"")}\n");
            sb.AppendLine("오늘 밤 의뢰서 3장이 책상에 놓여 있다.");
            return sb.ToString();
        }

        public static string Ending(Guild guild, GameConfig c, IReadOnlyList<DayRecord> history,
            IEnumerable<Quest> written, IEnumerable<Assignment> lastDaySent, RaidResult raid, AdventurerRoster roster)
        {
            var last = history[history.Count - 1];
            var sb = new StringBuilder();
            string grade = raid.Grade switch { RaidGrade.Perfect => "완벽한 방어", RaidGrade.Close => "아슬아슬한 방어", _ => "방어 실패 (길드장 출동)" };
            sb.AppendLine($"<b>■ 주말 습격: {grade}</b>  (습격 규모 {raid.Attack} / 마을 방어력 {raid.Defense})");
            if (raid.Helpers.Count > 0) sb.AppendLine($"  달려와 준 모험가: {string.Join(", ", raid.Helpers.Select(h => h.Name))}");
            sb.AppendLine();

            sb.AppendLine("<b>■ 네임드 호감도</b>");
            foreach (var n in roster.Named.OrderByDescending(a => a.Affinity))
                sb.AppendLine($"  · {n.Name} 「{n.Epithet}」  {new string('♥', n.Hearts)}{new string('♡', 5 - n.Hearts)}  ({n.Affinity})");
            sb.AppendLine();
            sb.AppendLine($"자금: {guild.Funds}G  (시작 {c.startFunds}G)");
            sb.AppendLine($"길드 평판: {guild.Reputation}  (시작 {c.startReputation})");
            sb.AppendLine($"마을 위험도: {guild.Danger}");
            sb.AppendLine($"규정 위반 승인 {history.Sum(h => h.Violations.Count)}건 · 부당 반려 {history.Sum(h => h.WrongRejects)}건 · 수수료 회수 패널티 {history.Sum(h => h.Ledger.PenaltyCount)}건\n");

            if (last.Ledger.Entries.Count > 0)
            {
                sb.AppendLine($"<b>■ {last.Day}일차 정산</b>");
                foreach (var e in last.Ledger.Entries) sb.AppendLine($"  · {e.Text}  {Delta(e)}");
                sb.AppendLine();
            }

            sb.AppendLine("<b>■ 밤에 쓴 의뢰서 채점 (정답 공개)</b>");
            foreach (var q in written)
            {
                sb.AppendLine($"{(!q.EntryWrong ? "<color=#2f7a3e>○</color>" : "<color=#b02c28>×</color>")} <b>{q.Title}</b>");
                sb.AppendLine($"     기입: {Txt.Describe(q.Entry.Monster, q.Entry.Count)} → {Txt.R(q.Entry.Rank)} / {Txt.J(q.Entry.Role)}");
                sb.AppendLine($"     정답: {Txt.Describe(q.Truth, q.TruthCount)} → {Txt.R(q.TruthRank)} / {Txt.J(q.Truth.role)}" +
                              (q.Truth.isMonster ? $"  (실제 {q.TruthNumber}마리)" : ""));
            }
            sb.AppendLine();

            var sent = lastDaySent.ToList();
            sb.AppendLine($"<b>■ {last.Day}일차에 보낸 모험가들 (다음 날 귀환 결과 미리보기)</b>");
            if (sent.Count == 0) sb.AppendLine("없음");
            foreach (var a in sent)
                sb.AppendLine($"· {a.MemberNames} → {a.Quest.Title}: <b>{Txt.Res(a.Result)}</b>{(a.IsLying ? " (허위 보고 예정)" : "")}");
            sb.AppendLine("\n<color=#7a6a5a>프로토타입은 여기까지입니다. 주말 습격, 사망 판정, 승급 심사, 지명 수주, 개인 이벤트 등은 아직 포함되지 않았습니다.</color>");
            return sb.ToString();
        }

        static string Delta(LedgerEntry e) =>
            (e.Gold != 0 ? $"{e.Gold:+#;-#}G " : "") + (e.Reputation != 0 ? $"평판 {e.Reputation:+#;-#}" : "");
    }
}
