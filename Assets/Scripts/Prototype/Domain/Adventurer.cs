using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    public enum LineKind { Greeting, Thanks, Rejected, Caught, ReturnSuccess, ReturnFail, Chat }

    // 모험가 한 명. 호감도·부상·레벨·출발/귀환은 스스로 관리한다.
    // 네임드(Named != null)는 자기 대사와 개인 이벤트를 가진다.
    public class Adventurer
    {
        public string Name { get; }
        public Rank Rank { get; private set; }
        public Job Job { get; }
        public CharacterLook Look { get; }
        public int Number { get; }
        public NamedCharacterData Named { get; }
        public int Affinity { get; private set; }
        public int Level { get; private set; }
        public int Successes { get; private set; }
        public bool Overpromoted { get; private set; }     // 기준 미달인데 승급됨 → 실제 실력은 한 단계 아래
        public int InjuredDays { get; private set; }
        public bool OnQuest { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsSick { get; private set; }
        public bool StoppedForging { get; private set; }
        public int Sorties { get; private set; }            // 지금까지 나간 의뢰 수
        public int SortiesBeforeWeek { get; private set; }  // 이번 주가 시작될 때의 Sorties (원정 피로 계산)
        public int AppliedAt { get; private set; } = -1;    // 마지막으로 승급 심사를 신청했을 때의 실적
        // 소속 지역 (길드 카드의 지역 마크). Home은 데이터가 있는 지역, 없는 이름(가짜 지역)이면 null
        public string RegionName { get; private set; } = "";
        public RegionData Home { get; private set; }
        public bool IsGuest { get; private set; }            // 우리 길드 소속이 아닌 손님 (축제 · 관할 밖 지역)
        public WantedPoster Criminal { get; private set; }     // 수배자가 모험가 행세를 하는 경우

        readonly HashSet<string> seenEvents = new();

        public bool IsNamed => Named != null;
        public bool IsAvailable => !OnQuest && InjuredDays == 0 && !IsDead;
        public string CardNumber => $"{Txt.RankCode(Rank)}-{Number}";
        public string Epithet => Named != null ? Named.epithet : "";
        public int Hearts => Mathf.Clamp(Affinity / 20, 0, 5);
        public IEnumerable<string> SeenEvents => seenEvents;
        // 판정에 쓰는 실제 실력 (억지로 승급된 사람은 한 단계 아래)
        public Rank TrueRank => Overpromoted && Rank > Rank.Bronze ? Rank - 1 : Rank;

        public Adventurer(string name, Rank rank, Job job, CharacterLook look, int number, int affinity, int level = 0)
        {
            Name = name;
            Rank = rank;
            Job = job;
            Look = look;
            Number = number;
            Affinity = affinity;
            Level = level > 0 ? level : DefaultLevel(rank);
        }

        public Adventurer(NamedCharacterData named, int number)
            : this(named.displayName, named.rank, named.job, named.look, number, named.startAffinity, named.level) => Named = named;

        static int DefaultLevel(Rank r) => r switch
        {
            Rank.Bronze => Random.Range(1, 6),
            Rank.Silver => Random.Range(6, 12),
            _ => Random.Range(12, 17),
        };

        public void ChangeAffinity(int delta) => Affinity = Mathf.Clamp(Affinity + delta, 0, 100);
        public void Injure(int days) => InjuredDays = Mathf.Max(InjuredDays, days);
        public void RestOneDay(int days = 1) => InjuredDays = Mathf.Max(0, InjuredDays - days);
        public void Depart() => OnQuest = true;
        public void Sortie() => Sorties++;
        public void StartWeek() => SortiesBeforeWeek = Sorties;
        public void ApplyForPromotion() => AppliedAt = Successes;
        public void Return() => OnQuest = false;
        public void Die() { IsDead = true; OnQuest = false; }
        public void SetSick(bool sick) => IsSick = sick;
        public Adventurer InRegion(RegionData region)
        {
            Home = region;
            RegionName = region != null ? region.displayName : "";
            return this;
        }

        // 손님: 데이터에 없는 지역 이름(가짜)도 받는다
        public Adventurer AsGuestFrom(string regionName, RegionData region)
        {
            Home = region;
            RegionName = regionName;
            IsGuest = true;
            return this;
        }
        public Adventurer AsCriminal(WantedPoster poster) { Criminal = poster; return this; }

        // 의뢰 성공 경험
        public void GainExperience(bool great)
        {
            Successes++;
            Level += great ? 2 : 1;
        }

        public void Promote(bool deserved)
        {
            if (Rank == Rank.Gold) return;
            Rank++;
            if (!deserved) Overpromoted = true;
        }

        public SavedAdventurer Save(int lookIndex) => new()
        {
            name = Name, named = Named != null ? Named.name : "", look = lookIndex, rank = Rank, job = Job, number = Number,
            affinity = Affinity, level = Level, successes = Successes, appliedAt = AppliedAt, sorties = Sorties,
            overpromoted = Overpromoted, injured = InjuredDays, dead = IsDead, stoppedForging = StoppedForging,
            seen = new List<string>(seenEvents), region = RegionName, guest = IsGuest,
        };

        // 세이브에서 되살리기 (생성자로 이름 · 직업 · 외형을 만든 뒤 나머지 상태를 채운다)
        public void Restore(SavedAdventurer s)
        {
            Rank = s.rank;
            Affinity = s.affinity;
            Level = s.level;
            Successes = s.successes;
            AppliedAt = s.appliedAt;
            Sorties = SortiesBeforeWeek = s.sorties;
            Overpromoted = s.overpromoted;
            InjuredDays = s.injured;
            IsDead = s.dead;
            StoppedForging = s.stoppedForging;
            foreach (var e in s.seen) seenEvents.Add(e);
        }

        // 네임드는 자기 대사, 없으면 fallback 풀에서
        public string Line(LineKind kind, string[] fallback)
        {
            var own = Named == null ? null : kind switch
            {
                LineKind.Greeting => Named.greetings,
                LineKind.Thanks => Named.thanks,
                LineKind.Rejected => Named.rejected,
                LineKind.Caught => Named.caught,
                LineKind.ReturnSuccess => Named.returnSuccess,
                LineKind.ReturnFail => Named.returnFail,
                _ => Named.chats,
            };
            return Lines.Pick(own != null && own.Length > 0 ? own : fallback);
        }

        // 호감도·주차 조건이 닿았지만 아직 안 본 개인 이벤트 (가장 앞의 것)
        public PersonalEvent PendingEvent(int week) =>
            Named?.events.FirstOrDefault(e => Affinity >= e.minAffinity && week >= e.minWeek && !seenEvents.Contains(e.title));

        public void MarkSeen(PersonalEvent e)
        {
            seenEvents.Add(e.title);
            if (e.stopsForgery) StoppedForging = true;
        }
    }
}
