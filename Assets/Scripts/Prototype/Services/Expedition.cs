using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 3주차 바르톨의 북쪽 숲 원정. 동행자 · 호감도 · 누적 피로로 생사가 갈린다.
    public class Expedition
    {
        public const string DepartId = "expedition_depart";
        public const string ReturnAliveId = "expedition_alive";
        public const string ReturnDeadId = "expedition_dead";

        public Adventurer Leader { get; }
        public Adventurer Companion { get; private set; }
        public bool Started { get; private set; }
        public bool Resolved { get; private set; }
        public bool Survived { get; private set; }
        public int Fatigue { get; }

        public Expedition(Adventurer leader, int fatigue)
        {
            Leader = leader;
            Fatigue = fatigue;
        }

        public SavedExpedition Save() => new()
        {
            companion = Companion?.Name ?? "", started = Started, resolved = Resolved, survived = Survived, fatigue = Fatigue,
        };

        public static Expedition Restore(SavedExpedition s, AdventurerRoster roster)
        {
            var leader = roster.Named.FirstOrDefault(a => a.Named.displayName == "바르톨");
            if (leader == null) return null;
            return new Expedition(leader, s.fatigue)
            {
                Companion = roster.All.FirstOrDefault(a => a.Name == s.companion),
                Started = s.started,
                Resolved = s.resolved,
                Survived = s.survived,
            };
        }

        // 동행 후보: 살아 있는 다른 네임드
        public static IEnumerable<Adventurer> Candidates(AdventurerRoster roster, Adventurer leader) =>
            roster.Named.Where(a => a != leader && !a.IsDead);

        public void Depart(Adventurer companion)
        {
            Companion = companion;
            Started = true;
            Leader.Depart();
            companion?.Depart();
        }

        // 생존 조건 (기획서: 06_바르톨.md)
        //  ① 세실리아 동행 + 호감도 70 이상 + 피로 한도 이내
        //  ② 누구든 동행 + 호감도 85 이상 + 피로가 한도보다 1 이상 낮음
        public void Resolve(int fatigueLimit)
        {
            bool cecilia = Companion?.Named != null && Companion.Named.job == Job.Priest;
            Survived = (cecilia && Leader.Affinity >= 70 && Fatigue <= fatigueLimit)
                       || (Companion != null && Leader.Affinity >= 85 && Fatigue < fatigueLimit);
            Resolved = true;
            if (Survived)
            {
                Leader.Injure(2);
                Leader.Return();
            }
            else Leader.Die();
            if (Companion != null)
            {
                Companion.Injure(2);
                Companion.Return();
            }
        }
    }
}
