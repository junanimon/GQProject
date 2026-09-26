using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    public class LedgerEntry
    {
        public string Text { get; }
        public int Gold { get; }
        public int Reputation { get; }
        public bool Paid { get; }       // 낮에 이미 자금에서 나간 돈 (지명 수당 · 성공 보너스) — 밤 정산에선 기록만
        public bool IsLoss => Gold < 0 || Reputation < 0;

        public LedgerEntry(string text, int gold, int reputation, bool paid)
        {
            Text = text;
            Gold = gold;
            Reputation = reputation;
            Paid = paid;
        }
    }

    // 하루 동안 쌓인 자금·평판 변화. 밤 결과 화면에서 한꺼번에 정산한다.
    public class Ledger
    {
        readonly List<LedgerEntry> entries = new();

        public IReadOnlyList<LedgerEntry> Entries => entries;
        public bool IsClean => entries.All(e => !e.IsLoss || e.Paid);
        public int PenaltyCount => entries.Count(e => e.Gold < 0 && !e.Paid);

        public void Add(string text, int gold = 0, int reputation = 0) => entries.Add(new LedgerEntry(text, gold, reputation, false));

        // 이미 낸 돈: 기록만 남긴다
        public void AddPaid(string text, int gold) => entries.Add(new LedgerEntry(text, gold, 0, true));
    }
}
