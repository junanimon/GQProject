using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuildProto
{
    // 종이 화면(게시판 정리 · 밤 결과 · 주간 정산 · 결산)에 들어갈 글
    public static class ReportWriter
    {
        public static string UnlockLine(Unlock u) => u == Unlock.Death
            ? "<b>공문.</b> 이번 주부터 무리한 의뢰에 나간 모험가는 <b>목숨을 잃을 수 있다</b>. 기입란과 배정에 더욱 신중할 것."
            : ForgeryCatalog.For(u)?.RuleLine ?? u.ToString();

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

        public static string NightReport(DayRecord r, Guild guild, GameConfig c, IReadOnlyList<Unlock> newTomorrow, ITempRule ruleTomorrow)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"처리: 승인 {r.Approved}건 / 반려·거절 {r.Rejected}건 · 받은 돈 {r.Fees}G");
            sb.AppendLine($"오늘 보낸 모험가 {r.Sent}팀 — 내일 창구로 돌아와 보고합니다.");
            var extra = new List<string>();
            if (r.Nominations > 0) extra.Add($"길드 홀 지명 {r.Nominations}건");
            if (r.Bonuses > 0) extra.Add($"성공 보너스 {r.Bonuses}건");
            if (r.UrgentHandled > 0) extra.Add($"긴급 의뢰 처리 {r.UrgentHandled}건");
            if (r.UrgentMissed > 0) extra.Add($"<color=#b02c28>긴급 의뢰 놓침 {r.UrgentMissed}건</color>");
            if (extra.Count > 0) sb.AppendLine(string.Join(" · ", extra));
            sb.AppendLine();

            if (r.Deaths.Count > 0)
            {
                sb.AppendLine("<b><color=#8a1a1a>■ 부고</color></b>");
                foreach (var d in r.Deaths) sb.AppendLine($"  · {d.Name} ({Txt.R(d.Rank)}급 {Txt.J(d.Job)}){(d.IsNamed ? $" 「{d.Epithet}」" : "")}");
                sb.AppendLine();
            }

            sb.AppendLine("<b>■ 정산 (자금 · 평판)</b>");
            if (r.Ledger.Entries.Count == 0) sb.AppendLine("변동 없음.");
            foreach (var e in r.Ledger.Entries)
                sb.AppendLine($"  · {e.Text}  <color={(e.IsLoss ? "#b02c28" : "#2f7a3e")}><b>{Delta(e)}</b></color>");
            if (r.WrongRejects > 0) sb.AppendLine($"  · 규정상 문제없는 모험가를 돌려보냄 {r.WrongRejects}건 (호감도 하락)");
            sb.AppendLine();

            sb.AppendLine($"<b>■ 마을 위험도</b> {r.DangerBefore} → {r.DangerAfter}  (매일 +{c.dailyDangerRise})");
            sb.AppendLine($"<b>■ 길드 평판</b> {guild.Reputation}  <color=#7a6a5a>(평판이 높을수록 실력 좋은 모험가가 찾아오고, 습격 때 더 많이 모인다)</color>\n");

            if (newTomorrow.Count > 0 || ruleTomorrow != null)
            {
                sb.AppendLine("<b>■ 공문 — 내일부터 시행</b>");
                foreach (var u in newTomorrow) sb.AppendLine($"<color=#b02c28>{UnlockLine(u)}</color>");
                sb.AppendLine();
            }

            bool clean = r.Ledger.IsClean && r.WrongRejects == 0;
            sb.AppendLine($"<b>선배:</b> {(r.Deaths.Count > 0 ? "\"……오늘은 일찍 들어가. 서류는 내가 정리할게.\"" : clean ? "\"깔끔하네. 이 정도면 믿고 맡겨도 되겠어.\"" : "\"항의가 들어오면 우리 길드 얼굴에 먹칠이야. 기입란부터 꼼꼼히.\"")}\n");
            sb.AppendLine("오늘 밤 의뢰서가 책상에 놓여 있다.");
            return sb.ToString();
        }

        public static string WeekSummary(int week, RaidResult raid, bool rescued, Guild guild, IReadOnlyList<DayRecord> weekDays, IEnumerable<Adventurer> recruits)
        {
            var sb = new StringBuilder();
            string grade = raid.Grade switch { RaidGrade.Perfect => "완벽한 방어", RaidGrade.Close => "아슬아슬한 방어", _ => rescued ? "방어 실패 → 길드장 그라함 출동" : "방어 실패" };
            sb.AppendLine($"<b>■ {raid.Title}: {grade}</b>");
            sb.AppendLine($"  습격 규모 {raid.Attack} / 마을 방어력 {raid.Defense}  (모험가 참가율 {raid.Participation:P0} — 평판 {guild.Reputation})");
            if (raid.Helpers.Count > 0) sb.AppendLine($"  달려와 준 네임드: {string.Join(", ", raid.Helpers.Select(h => h.Name))}");
            if (raid.SealKey) sb.AppendLine("  <color=#2f7a3e>니아가 봉인의 열쇠로 나섰다</color>");
            sb.AppendLine();

            sb.AppendLine($"<b>■ {week}주차 평일</b>");
            sb.AppendLine($"  승인 {weekDays.Sum(d => d.Approved)} · 반려 {weekDays.Sum(d => d.Rejected)} · 받은 돈 {weekDays.Sum(d => d.Fees)}G");
            sb.AppendLine($"  규정 위반 승인 {weekDays.Sum(d => d.Violations.Count)}건 · 패널티 {weekDays.Sum(d => d.Ledger.PenaltyCount)}건 · 사망 {weekDays.Sum(d => d.Deaths.Count)}명\n");

            sb.AppendLine($"자금 {guild.Funds}G · 평판 {guild.Reputation} · 위험도 {guild.Danger} (습격 뒤 절반으로)\n");
            var joined = recruits.ToList();
            if (joined.Count > 0)
                sb.AppendLine($"<b>■ 새로 등록한 모험가 {joined.Count}명</b>  {string.Join(", ", joined.Select(a => $"{a.Name}({Txt.R(a.Rank)})"))}\n  <color=#7a6a5a>평판이 높을수록 더 많이, 더 높은 등급이 온다</color>");
            return sb.ToString();
        }

        public static string Ending(Guild guild, RunStats s, AdventurerRoster roster, Expedition expedition, int endingCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"길드 등급 {RegionMap.RankName(guild.Rank)}급 · 자금 {guild.Funds}G · 길드 평판 {guild.Reputation} · 마을 위험도 {guild.Danger}\n");

            sb.AppendLine("<b>■ 주말 습격</b>");
            for (int i = 0; i < s.raids.Count; i++)
            {
                var r = s.raids[i];
                sb.AppendLine($"  {i + 1}주차 {r.title}: {r.grade switch { RaidGrade.Perfect => "완벽", RaidGrade.Close => "아슬아슬", _ => "실패" }} ({r.defense}/{r.attack})");
            }
            sb.AppendLine();

            sb.AppendLine("<b>■ 네임드</b>");
            foreach (var n in roster.Named.OrderByDescending(a => a.IsDead ? -1 : a.Affinity))
                sb.AppendLine(n.IsDead
                    ? $"  · <color=#8a1a1a>{n.Name} 「{n.Epithet}」 — 사망</color>"
                    : $"  · {n.Name} 「{n.Epithet}」  {new string('♥', n.Hearts)}{new string('♡', 5 - n.Hearts)}  ({n.Affinity})");
            if (expedition != null && expedition.Resolved)
                sb.AppendLine($"  3주차 원정: 동행 {(expedition.Companion?.Name ?? "없음")} · 누적 피로 {expedition.Fatigue} → {(expedition.Survived ? "바르톨 생환" : "바르톨 전사")}");
            sb.AppendLine();

            sb.AppendLine($"<b>■ 밤 기입 정확도</b> {s.writtenRight}/{s.written} ({(s.written == 0 ? 0 : s.writtenRight * 100 / s.written)}%)");
            sb.AppendLine($"<b>■ 5주간</b> 승인 {s.approved} · 반려 {s.rejected} · 규정 위반 {s.violations} · 사망 {s.deaths}명");
            sb.AppendLine($"  승급 {s.promotions}명 (그중 기준 미달 {s.overpromotions}명) · 지명 {s.nominations}건 · 긴급 의뢰 처리 {s.urgentHandled} / 놓침 {s.urgentMissed}");
            sb.AppendLine($"\n<color=#7a6a5a>엔딩 도감은 타이틀 화면에서 볼 수 있습니다. (전체 {endingCount}종)</color>");
            return sb.ToString();
        }

        static string Delta(LedgerEntry e) =>
            (e.Gold != 0 ? $"{e.Gold:+#;-#}G " : "") + (e.Reputation != 0 ? $"평판 {e.Reputation:+#;-#}" : "") + (e.Paid ? " (낮에 지급)" : "");
    }
}
