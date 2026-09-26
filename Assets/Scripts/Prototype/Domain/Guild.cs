using UnityEngine;

namespace GuildProto
{
    // 길드 운영 상태: 자금 · 마을 위험도 · 평판 · 길드 등급 (등급이 오르면 관할 지역이 늘어난다)
    public class Guild
    {
        public const string HomeName = "로웰";

        public int Funds { get; private set; }
        public int Danger { get; private set; }
        public int Reputation { get; private set; }
        public int Rank { get; private set; } = 1;     // 1=E … 5=A (RegionMap.RankName)

        public Guild(int funds, int danger, int reputation, int rank = 1)
        {
            Funds = funds;
            Danger = danger;
            Reputation = reputation;
            Rank = Mathf.Max(1, rank);
        }

        public void RankUp() => Rank++;

        public void Earn(int gold) => Funds += gold;
        public void ChangeDanger(int delta) => Danger = Mathf.Max(0, Danger + delta);
        public void ChangeReputation(int delta) => Reputation = Mathf.Clamp(Reputation + delta, 0, 100);

        // 밤 결과 화면에서 하루치 정산을 반영
        public void Settle(Ledger ledger)
        {
            foreach (var e in ledger.Entries)
            {
                if (!e.Paid) Funds += e.Gold;
                Reputation = Mathf.Clamp(Reputation + e.Reputation, 0, 100);
            }
        }
    }
}
