using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    public enum LineKind { Greeting, Thanks, Rejected, Caught, ReturnSuccess, ReturnFail, Chat }

    // 모험가 한 명. 호감도·부상·출발/귀환은 스스로 관리한다.
    // 네임드(Named != null)는 자기 대사와 개인 이벤트를 가진다.
    public class Adventurer
    {
        public string Name { get; }
        public Rank Rank { get; }
        public Job Job { get; }
        public CharacterLook Look { get; }
        public int Number { get; }
        public NamedCharacterData Named { get; }
        public int Affinity { get; private set; }
        public int InjuredDays { get; private set; }
        public bool OnQuest { get; private set; }

        readonly HashSet<PersonalEvent> seenEvents = new();

        public bool IsNamed => Named != null;
        public bool IsAvailable => !OnQuest && InjuredDays == 0;
        public string CardNumber => $"{Txt.RankCode(Rank)}-{Number}";
        public string Epithet => Named != null ? Named.epithet : "";
        public int Hearts => Mathf.Clamp(Affinity / 20, 0, 5);

        public Adventurer(string name, Rank rank, Job job, CharacterLook look, int number, int affinity)
        {
            Name = name;
            Rank = rank;
            Job = job;
            Look = look;
            Number = number;
            Affinity = affinity;
        }

        public Adventurer(NamedCharacterData named, int number)
            : this(named.displayName, named.rank, named.job, named.look, number, named.startAffinity) => Named = named;

        public void ChangeAffinity(int delta) => Affinity = Mathf.Clamp(Affinity + delta, 0, 100);
        public void Injure(int days) => InjuredDays = Mathf.Max(InjuredDays, days);
        public void RestOneDay() { if (InjuredDays > 0) InjuredDays--; }
        public void Depart() => OnQuest = true;
        public void Return() => OnQuest = false;

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

        // 호감도가 닿았지만 아직 안 본 개인 이벤트 (가장 앞의 것)
        public PersonalEvent PendingEvent() =>
            Named?.events.FirstOrDefault(e => Affinity >= e.minAffinity && !seenEvents.Contains(e));

        public void MarkSeen(PersonalEvent e) => seenEvents.Add(e);
    }
}
