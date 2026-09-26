using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    public enum RaidGrade { Perfect, Close, Fail }

    public class RaidResult
    {
        public string Title;
        public RaidGrade Grade;
        public int Attack;
        public int Defense;
        public float Participation;                 // 이름 없는 모험가 참가율 (평판)
        public List<Adventurer> Helpers = new();   // 호감도가 높아 달려와 준 네임드
        public Adventurer Hero;                     // 가장 호감도가 높은 네임드
        public bool SealKey;                        // 마지막 주: 니아가 봉인의 열쇠로 나섬
    }

    // 주말 습격: 습격 규모(위험도 + 주차 기본값)와 마을 방어력(쉬고 있는 모험가)을 비교한다
    public class RaidResolver
    {
        readonly GameConfig config;

        public RaidResolver(GameConfig config) => this.config = config;

        public RaidResult Resolve(RaidScript script, Guild guild, AdventurerRoster roster, bool finalWeek, ICollection<Adventurer> awakened)
        {
            var r = new RaidResult
            {
                Title = script.title,
                Attack = guild.Danger + script.baseAttack,
                Participation = Mathf.Clamp01(config.raidBaseParticipation + guild.Reputation / 200f),
            };
            float defense = 0;
            foreach (var a in roster.Alive.Where(a => a.InjuredDays == 0))
            {
                int power = a.Rank switch { Rank.Bronze => 1, Rank.Silver => 2, _ => 4 };
                bool helper = a.IsNamed && a.Affinity >= config.raidHelperAffinity;
                float joinRate = a.IsNamed ? 1f : r.Participation;
                defense += power * (0.5f + a.Affinity / 100f) * joinRate;
                if (helper)
                {
                    defense += config.raidHelperBonus;
                    r.Helpers.Add(a);
                }
                if (awakened.Contains(a)) defense += config.awakenedBonus;
            }
            if (finalWeek)
            {
                var nia = roster.Named.FirstOrDefault(a => a.Named.displayName == "니아" && !a.IsDead);
                r.SealKey = nia != null && nia.Affinity >= 85;
                if (r.SealKey) defense += config.sealKeyBonus;
            }
            r.Defense = Mathf.RoundToInt(defense);
            r.Hero = roster.Named.Where(a => !a.IsDead).OrderByDescending(a => a.Affinity).FirstOrDefault();
            r.Grade = r.Defense >= r.Attack ? RaidGrade.Perfect
                : r.Defense >= r.Attack * config.raidCloseRatio ? RaidGrade.Close
                : RaidGrade.Fail;

            if (r.Grade == RaidGrade.Perfect)
            {
                guild.ChangeReputation(config.raidPerfectReputation);
                foreach (var h in r.Helpers) h.ChangeAffinity(5);
            }
            guild.ChangeDanger(-guild.Danger / 2);      // 습격이 지나가면 위험도 절반으로
            return r;
        }

        public void ApplyFailure(Guild guild)
        {
            guild.ChangeReputation(config.raidFailReputation);
            guild.Earn(-config.raidFailCost);
        }

        // 대사의 {defenders} {hero} 자리 채우기
        public static List<StoryLine> Fill(IEnumerable<StoryLine> lines, RaidResult r)
        {
            string helpers = r.Helpers.Count > 0 ? string.Join(", ", r.Helpers.Select(h => h.Name)) : "몇몇 모험가";
            string hero = r.Hero != null ? r.Hero.Name : "모험가";
            return lines.Select(l => new StoryLine
            {
                speaker = l.speaker?.Replace("{hero}", hero),
                text = l.text.Replace("{defenders}", helpers).Replace("{hero}", hero),
            }).ToList();
        }
    }
}
