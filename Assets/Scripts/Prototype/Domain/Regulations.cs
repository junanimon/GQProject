using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 길드 규정집: 수주 신청 검사
    public static class Regulations
    {
        public static readonly string[] BaseLines =
        {
            "<b>규정 1.</b> 개인은 자기 등급 이하의 의뢰만 수주할 수 있다.",
            "<b>규정 2.</b> 파티는 가장 낮은 파티원 등급보다 한 단계 위 의뢰까지 수주할 수 있다. 단, 한 단계 위를 받으려면 3인 이상이어야 한다.",
            "<b>규정 3.</b> 필수 역할이 지정된 의뢰는 해당 직업이 1명 이상 있어야 한다.",
        };

        public const string CompletionLine =
            "<b>완료 심사.</b> 증거물은 게시된 대상 몬스터의 증거 부위여야 하고, 개수가 기입된 수량 범위 안이어야 한다. 몬스터 없는 의뢰는 해당 물품을 확인한다.";

        // 창구에서 보이는 카드와 게시된 기입란 기준 검사. 문제 없으면 null
        public static string CheckApplication(IReadOnlyList<GuildCard> cards, Quest q)
        {
            var forged = cards.FirstOrDefault(c => c.IsForged);
            if (forged != null) return $"위조 카드 ({forged.Forgery.Name}) — {forged.Name}";

            var entry = q.Entry;
            Rank min = cards.Min(c => c.ShownRank);
            int gap = (int)entry.Rank - (int)min;
            if (cards.Count == 1)
            {
                if (gap > 0) return "규정 1 위반 — 자기 등급보다 높은 의뢰";
            }
            else
            {
                if (gap >= 2) return "규정 2 위반 — 파티 최저 등급보다 두 단계 위 의뢰";
                if (gap == 1 && cards.Count < 3) return "규정 2 위반 — 한 단계 위 의뢰를 3인 미만 파티가 수주";
            }
            if (entry.Role != Job.None && !cards.Any(c => c.Job == entry.Role))
                return $"규정 3 위반 — 필수 역할({Txt.J(entry.Role)}) 없음";
            return null;
        }
    }
}
