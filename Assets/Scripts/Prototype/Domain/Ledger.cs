using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    public class LedgerEntry
    {
        public string Text { get; }
        public int Gold { get; }
        public int Reputation { get; }
        public bool IsLoss => Gold < 0 || Reputation < 0;

        public LedgerEntry(string text, int gold, int reputation)
        {
            Text = text;
            Gold = gold;
            Reputation = reputation;
        }
    }

    // 하루 동안 쌓인 자금·평판 변화. 밤 결과 화면에서 한꺼번에 정산한다.
    public class Ledger
    {
        readonly List<LedgerEntry> entries = new();

        public IReadOnlyList<LedgerEntry> Entries => entries;
        public bool IsClean => entries.All(e => !e.IsLoss);
        public int PenaltyCount => entries.Count(e => e.Gold < 0);

        public void Add(string text, int gold = 0, int reputation = 0) => entries.Add(new LedgerEntry(text, gold, reputation));
    }
}
