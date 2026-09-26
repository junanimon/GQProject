using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 길드 모험가 명단: 네임드 + 이름 없는 모험가 (+ 한 번 들르는 손님). 필요한 조건의 모험가를 찾거나 새로 등록한다.
    // 새로 등록하는 모험가는 관할 지역 중 한 곳 소속이다.
    public class AdventurerRoster
    {
        readonly List<Adventurer> all = new();
        readonly IReadOnlyList<CharacterLook> looks;
        readonly NameGenerator names;
        readonly RegionMap map;
        readonly List<RegionData> managed = new();

        public IReadOnlyList<Adventurer> All => all;
        public IEnumerable<Adventurer> Named => all.Where(a => a.IsNamed);
        IEnumerable<Adventurer> Members => all.Where(a => !a.IsGuest);

        public AdventurerRoster(GameDatabase db, RegionMap map, int guildRank)
        {
            looks = db.looks;
            this.map = map;
            SetGuildRank(guildRank);
            names = new NameGenerator(db.namedCharacters.Select(n => n.displayName));
            foreach (var n in db.namedCharacters) all.Add(new Adventurer(n, Random.Range(1000, 10000)).InRegion(map.Home));
            // 이름 없는 모험가: 동 8 · 은 4 · 금 1 (마법사·궁수·성직자가 한 명도 없는 상황은 피한다)
            Seed(Rank.Bronze, 8, Job.Mage, Job.Archer, Job.Priest);
            Seed(Rank.Silver, 4, Job.Mage, Job.Archer, Job.Priest);
            Seed(Rank.Gold, 1);
        }

        // 세이브에서 되살리기
        public AdventurerRoster(GameDatabase db, RegionMap map, int guildRank, IEnumerable<SavedAdventurer> saved)
        {
            looks = db.looks;
            this.map = map;
            SetGuildRank(guildRank);
            var list = saved.ToList();
            names = new NameGenerator(db.namedCharacters.Select(n => n.displayName).Concat(list.Select(s => s.name)));
            foreach (var s in list)
            {
                var named = string.IsNullOrEmpty(s.named) ? null : db.namedCharacters.FirstOrDefault(n => n.name == s.named);
                var a = named != null
                    ? new Adventurer(named, s.number)
                    : new Adventurer(s.name, s.rank, s.job, looks[Mathf.Clamp(s.look, 0, looks.Count - 1)], s.number, s.affinity, s.level);
                a.Restore(s);
                var region = string.IsNullOrEmpty(s.region) ? map.Home : map.Named(s.region);
                if (s.guest) a.AsGuestFrom(s.region, region);
                else a.InRegion(region ?? map.Home);
                all.Add(a);
            }
        }

        // 길드 등급이 바뀌면 관할 지역도 바뀐다
        public void SetGuildRank(int guildRank)
        {
            managed.Clear();
            managed.AddRange(map.Managed(guildRank));
        }

        public List<SavedAdventurer> Save() => all.Select(a => a.Save(a.IsNamed ? -1 : IndexOfLook(a.Look))).ToList();

        int IndexOfLook(CharacterLook look)
        {
            for (int i = 0; i < looks.Count; i++) if (looks[i] == look) return i;
            return 0;
        }

        void Seed(Rank rank, int count, params Job[] guaranteed)
        {
            for (int i = 0; i < count; i++) Register(rank, i < guaranteed.Length ? guaranteed[i] : RandomJob(), map.Home);
        }

        // region이 없으면 관할 지역 중 한 곳
        public Adventurer Register(Rank rank, Job job, RegionData region = null)
        {
            region ??= managed.Count > 0 ? managed[Random.Range(0, managed.Count)] : map.Home;
            var a = new Adventurer(names.Next(), rank, job, looks[Random.Range(0, looks.Count)],
                Random.Range(1000, 10000), Random.Range(40, 61)).InRegion(region);
            all.Add(a);
            return a;
        }

        // 조건에 맞는 한가한 이름 없는 모험가 (없으면 새로 등록). used에 이미 뽑힌 사람은 제외
        public Adventurer Find(Rank rank, ISet<Adventurer> used, Job mustBe = Job.None, Job avoid = Job.None)
        {
            var found = Members.Where(a => !a.IsNamed && a.IsAvailable && !used.Contains(a) && a.Rank == rank &&
                                           (mustBe == Job.None ? avoid == Job.None || a.Job != avoid : a.Job == mustBe))
                               .OrderBy(_ => Random.value).FirstOrDefault()
                        ?? Register(rank, mustBe != Job.None ? mustBe : RandomJob(avoid));
            used.Add(found);
            return found;
        }

        public RegionData HomeRegion => map.Home;
        public IEnumerable<Adventurer> AvailableNamed => all.Where(a => a.IsNamed && a.IsAvailable);
        // 길드 소속으로 살아 있는 사람 (손님 제외 — 습격 방어 · 승급 심사)
        public IEnumerable<Adventurer> Alive => Members.Where(a => !a.IsDead);

        // 한 번 들르는 손님 (축제 · 관할 밖 지역 · 수배서와 이름이 같은 사람). 지역 이름이 데이터에 없으면 가짜 지역
        public Adventurer RegisterGuest(Rank rank, Job job, string regionName = null)
        {
            var a = Register(rank, job);
            if (regionName != null) a.AsGuestFrom(regionName, map.Named(regionName));
            else a.AsGuestFrom(a.RegionName, a.Home);
            return a;
        }

        // 주 시작 때 새 모험가 등록. 길드 평판이 높을수록 많이, 높은 등급이 온다 (관할 지역 중 무작위 소속)
        public List<Adventurer> Recruit(int reputation, int extra = 0)
        {
            int count = 1 + reputation / 34 + extra;
            float gold = reputation / 400f, silver = 0.2f + reputation / 250f;
            var joined = new List<Adventurer>();
            for (int i = 0; i < count; i++)
            {
                float r = Random.value;
                var rank = r < gold ? Rank.Gold : r < gold + silver ? Rank.Silver : Rank.Bronze;
                joined.Add(Register(rank, RandomJob()));
            }
            return joined;
        }

        // 새 관할 지역의 모험가들이 길드에 등록한다 (지역이 깊을수록 높은 등급이 섞인다)
        public List<Adventurer> RecruitFrom(RegionData region, int count)
        {
            var joined = new List<Adventurer>();
            for (int i = 0; i < count; i++)
            {
                float r = Random.value, depth = region.unlockRank / 5f;
                var rank = r < 0.1f + depth * 0.25f ? Rank.Gold : r < 0.4f + depth * 0.3f ? Rank.Silver : Rank.Bronze;
                joined.Add(Register(rank, RandomJob(), region));
            }
            return joined;
        }

        public Adventurer AnyIdle(bool preferNamed)
        {
            var idle = Members.Where(a => a.IsAvailable).ToList();
            var named = idle.Where(a => a.IsNamed).ToList();
            var pool = preferNamed && named.Count > 0 ? named : idle;
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : all[0];
        }

        public Adventurer SomeoneElse(Adventurer except, Rank rank) =>
            Members.Where(a => a != except && !a.IsNamed && a.Rank == rank).OrderBy(_ => Random.value).FirstOrDefault()
            ?? Register(rank, except.Job);

        public CharacterLook OtherLook(CharacterLook except) =>
            looks.Where(l => l != except).OrderBy(_ => Random.value).FirstOrDefault() ?? except;

        public void NewDay(int heal = 1)
        {
            foreach (var a in all)
            {
                a.RestOneDay(heal);
                a.SetSick(false);   // 증상은 하루 단위로 바뀐다 (전염병 주간에만 다시 걸림)
            }
        }

        static Job RandomJob(Job avoid = Job.None)
        {
            Job j;
            do j = (Job)Random.Range(1, 5); while (j == avoid);
            return j;
        }
    }

    public class NameGenerator
    {
        static readonly string[] pool =
        {
            "알렌", "베르타", "카일", "도라", "에릭", "피오나", "그렉", "하나", "이반", "줄리아", "콜린", "라나",
            "모건", "오스카", "페트라", "루크", "토마스", "엘사", "빅터", "릴리", "한스", "메이", "휴고", "엠마",
        };
        readonly Queue<string> unused;

        public NameGenerator(IEnumerable<string> reserved)
        {
            var taken = new HashSet<string>(reserved);
            unused = new Queue<string>(pool.Where(n => !taken.Contains(n)).OrderBy(_ => Random.value));
        }

        public string Next() => unused.Count > 0
            ? unused.Dequeue()
            : pool[Random.Range(0, pool.Length)] + " " + (char)('A' + Random.Range(0, 26));
    }
}
