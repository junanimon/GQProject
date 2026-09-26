using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 밤 승급 심사: 레벨이나 실적 중 하나라도 기준에 닿은 모험가가 승급을 신청한다.
    // 둘 다 채웠으면 '적격', 하나만 채웠으면 '미달'. 미달인데 승인하면 실제 실력은 그대로라 의뢰에서 무너진다.
    public class PromotionBoard
    {
        readonly GameDatabase db;

        public PromotionBoard(GameDatabase db) => this.db = db;

        public int NeedLevel(Adventurer a) => db.promotionLevel[Mathf.Clamp((int)a.Rank, 0, db.promotionLevel.Length - 1)];
        public int NeedRecord(Adventurer a) => db.promotionRecord[Mathf.Clamp((int)a.Rank, 0, db.promotionRecord.Length - 1)];

        public bool Deserves(Adventurer a) => a.Level >= NeedLevel(a) && a.Successes >= NeedRecord(a);

        // 오늘 밤 신청자 (최대 max명). 같은 실적으로는 두 번 신청하지 않는다
        public List<Adventurer> Applicants(AdventurerRoster roster, int max) =>
            roster.Alive.Where(a => a.Rank < Rank.Gold && a.AppliedAt != a.Successes && a.Criminal == null &&
                                    (a.Level >= NeedLevel(a) || a.Successes >= NeedRecord(a)))
                .OrderByDescending(a => a.IsNamed).ThenBy(_ => Random.value).Take(max).ToList();

        // 심사 결과를 반영하고 모험가의 한마디를 돌려준다
        public string Decide(Adventurer a, bool approve, RunStats stats)
        {
            bool deserved = Deserves(a);
            a.ApplyForPromotion();
            if (approve)
            {
                a.Promote(deserved);
                a.ChangeAffinity(deserved ? 6 : 10);
                stats.promotions++;
                if (!deserved) stats.overpromotions++;
                return deserved ? "감사합니다! 새 카드, 소중히 쓸게요." : "와, 정말요?! …저, 사실 조금 불안했는데. 열심히 할게요!";
            }
            a.ChangeAffinity(deserved ? -8 : -2);
            return deserved ? "…기준은 다 채웠는데요. 이유라도 알려 주시면 좋겠네요." : "역시 아직인가요. 조금 더 실적을 쌓고 올게요.";
        }
    }
}
