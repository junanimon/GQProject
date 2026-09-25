using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 마을의 모험가 명단: 네임드 + 이름 없는 모험가. 필요한 조건의 모험가를 찾거나 새로 등록한다.
    public class AdventurerRoster
    {
        readonly List<Adventurer> all = new();
        readonly IReadOnlyList<CharacterLook> looks;
        readonly NameGenerator names;

        public IReadOnlyList<Adventurer> All => all;
        public IEnumerable<Adventurer> Named => all.Where(a => a.IsNamed);

        public AdventurerRoster(GameDatabase db)
        {
            looks = db.looks;
            names = new NameGenerator(db.namedCharacters.Select(n => n.displayName));
            foreach (var n in db.namedCharacters) all.Add(new Adventurer(n, Random.Range(1000, 10000)));
            // 이름 없는 모험가: 동 8 · 은 4 · 금 1 (마법사·궁수·성직자가 한 명도 없는 상황은 피한다)
            Seed(Rank.Bronze, 8, Job.Mage, Job.Archer, Job.Priest);
            Seed(Rank.Silver, 4, Job.Mage, Job.Archer, Job.Priest);
            Seed(Rank.Gold, 1);
        }

        void Seed(Rank rank, int count, params Job[] guaranteed)
        {
            for (int i = 0; i < count; i++) Register(rank, i < guaranteed.Length ? guaranteed[i] : RandomJob());
        }

        public Adventurer Register(Rank rank, Job job)
        {
            var a = new Adventurer(names.Next(), rank, job, looks[Random.Range(0, looks.Count)],
                Random.Range(1000, 10000), Random.Range(40, 61));
            all.Add(a);
            return a;
        }

        // 조건에 맞는 한가한 이름 없는 모험가 (없으면 새로 등록). used에 이미 뽑힌 사람은 제외
        public Adventurer Find(Rank rank, ISet<Adventurer> used, Job mustBe = Job.None, Job avoid = Job.None)
        {
            var found = all.Where(a => !a.IsNamed && a.IsAvailable && !used.Contains(a) && a.Rank == rank &&
                                       (mustBe == Job.None ? avoid == Job.None || a.Job != avoid : a.Job == mustBe))
                           .OrderBy(_ => Random.value).FirstOrDefault()
                        ?? Register(rank, mustBe != Job.None ? mustBe : RandomJob(avoid));
            used.Add(found);
            return found;
        }

        public IEnumerable<Adventurer> AvailableNamed => all.Where(a => a.IsNamed && a.IsAvailable);

        public Adventurer AnyIdle(bool preferNamed)
        {
            var idle = all.Where(a => a.IsAvailable).ToList();
            var named = idle.Where(a => a.IsNamed).ToList();
            var pool = preferNamed && named.Count > 0 ? named : idle;
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : all[0];
        }

        public Adventurer SomeoneElse(Adventurer except, Rank rank) =>
            all.Where(a => a != except && !a.IsNamed && a.Rank == rank).OrderBy(_ => Random.value).FirstOrDefault()
            ?? Register(rank, except.Job);

        public CharacterLook OtherLook(CharacterLook except) =>
            looks.Where(l => l != except).OrderBy(_ => Random.value).FirstOrDefault() ?? except;

        public void NewDay()
        {
            foreach (var a in all) a.RestOneDay();
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
