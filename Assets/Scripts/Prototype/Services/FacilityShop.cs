using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 밤 상점에서 산 편의 시설. 효과는 다른 곳에서 Has / Count 로 물어본다.
    public class FacilityShop
    {
        readonly IReadOnlyList<Facility> catalog;
        readonly Dictionary<FacilityKind, int> owned = new();

        public FacilityShop(IReadOnlyList<Facility> catalog) => this.catalog = catalog;

        public IReadOnlyList<Facility> Catalog => catalog;
        public int Count(FacilityKind kind) => owned.TryGetValue(kind, out int n) ? n : 0;
        public bool Has(FacilityKind kind) => Count(kind) > 0;

        public bool IsOnSale(Facility f, int week) => week >= f.unlockWeek;
        public bool SoldOut(Facility f) => Count(f.kind) >= f.maxCount;
        public bool CanBuy(Facility f, int week, Guild guild) => IsOnSale(f, week) && !SoldOut(f) && guild.Funds >= f.price;

        public void Buy(Facility f, Guild guild)
        {
            guild.Earn(-f.price);
            owned[f.kind] = Count(f.kind) + 1;
        }

        // 소모품(매주 다시 사는 것)은 주가 바뀌면 사라진다
        public void NewWeek()
        {
            foreach (var f in catalog.Where(f => f.weekly)) owned.Remove(f.kind);
        }

        public List<FacilityCount> Save() => owned.Select(p => new FacilityCount { kind = p.Key, count = p.Value }).ToList();

        public void Restore(IEnumerable<FacilityCount> saved)
        {
            owned.Clear();
            foreach (var s in saved) owned[s.kind] = s.count;
        }
    }
}
