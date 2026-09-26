using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 지역 목록 + 길드 등급으로 관할 지역 · 출몰 몬스터를 계산한다
    public class RegionMap
    {
        public static readonly string[] RankNames = { "E", "D", "C", "B", "A" };

        readonly IReadOnlyList<RegionData> regions;

        public RegionMap(IReadOnlyList<RegionData> regions) => this.regions = regions.Where(r => r != null).OrderBy(r => r.unlockRank).ToList();

        public IReadOnlyList<RegionData> All => regions;
        public RegionData Home => regions.FirstOrDefault();
        public int MaxRank => regions.Count == 0 ? 1 : regions.Max(r => r.unlockRank);

        public static string RankName(int rank) => RankNames[UnityEngine.Mathf.Clamp(rank, 1, RankNames.Length) - 1];

        public IEnumerable<RegionData> Managed(int rank) => regions.Where(r => r.unlockRank <= rank);
        public IEnumerable<RegionData> Unmanaged(int rank) => regions.Where(r => r.unlockRank > rank);
        public IEnumerable<RegionData> OpenedAt(int rank) => regions.Where(r => r.unlockRank == rank);
        public bool IsManaged(string regionName, int rank) => Managed(rank).Any(r => r.displayName == regionName);
        public RegionData Named(string regionName) => regions.FirstOrDefault(r => r.displayName == regionName);

        // 자동 생성 의뢰에 나올 수 있는 몬스터
        public IEnumerable<MonsterData> MonstersOf(int rank) => Managed(rank).SelectMany(r => r.monsters).Where(m => m != null && m.isMonster);

        public RegionData RegionOf(MonsterData m) => regions.FirstOrDefault(r => r.monsters.Contains(m));
    }
}
