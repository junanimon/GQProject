namespace GuildProto
{
    // 귀환한 모험가가 내미는 증거물
    public class Evidence
    {
        public string Item { get; }
        public int Count { get; }
        public MonsterData Source { get; }      // 몬스터 부위면 그 몬스터, 의뢰 물품이면 null

        Evidence(string item, int count, MonsterData source)
        {
            Item = item;
            Count = count;
            Source = source;
        }

        public static Evidence PartsOf(MonsterData m, int count) => new(m.evidenceName, count, m);
        public static Evidence ProofItem(string item) => new(item, 1, null);

        // 게시본 기입란과 맞는가
        public bool Matches(Quest q)
        {
            var m = q.Entry.Monster;
            if (!m.isMonster) return Item == q.Proof;
            return Item == m.evidenceName && Count >= Txt.BandMin(q.Entry.Count) && Count <= Txt.BandMax(q.Entry.Count);
        }

        public override string ToString() => $"{Item} ×{Count}";
    }
}
