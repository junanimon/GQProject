using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    public enum RaidGrade { Perfect, Close, Fail }

    public class RaidResult
    {
        public RaidGrade Grade;
        public int Attack;
        public int Defense;
        public List<Adventurer> Helpers = new();   // 호감도가 높아 달려와 준 네임드
        public Adventurer Hero;                     // 가장 호감도가 높은 네임드
    }

    // 주말 습격: 습격 규모(위험도 + 주차 기본값)와 마을 방어력(쉬고 있는 모험가 전원)을 비교한다
    public class RaidResolver
    {
        readonly GameConfig config;

        public RaidResolver(GameConfig config) => this.config = config;

        public RaidResult Resolve(Guild guild, AdventurerRoster roster)
        {
            var r = new RaidResult { Attack = guild.Danger + config.raidBaseAttack };
            float defense = 0;
            foreach (var a in roster.All.Where(a => a.InjuredDays == 0))
            {
                int power = a.Rank switch { Rank.Bronze => 1, Rank.Silver => 2, _ => 4 };
                defense += power * (0.5f + a.Affinity / 100f);
                if (a.IsNamed && a.Affinity >= config.raidHelperAffinity)
                {
                    defense += config.raidHelperBonus;
                    r.Helpers.Add(a);
                }
            }
            r.Defense = Mathf.RoundToInt(defense);
            r.Hero = roster.Named.OrderByDescending(a => a.Affinity).FirstOrDefault();
            r.Grade = r.Defense >= r.Attack ? RaidGrade.Perfect
                : r.Defense >= r.Attack * config.raidCloseRatio ? RaidGrade.Close
                : RaidGrade.Fail;

            switch (r.Grade)
            {
                case RaidGrade.Perfect:
                    guild.ChangeReputation(config.raidPerfectReputation);
                    foreach (var h in r.Helpers) h.ChangeAffinity(5);
                    break;
                case RaidGrade.Fail:
                    guild.ChangeReputation(config.raidFailReputation);
                    guild.Earn(-config.raidFailCost);
                    break;
            }
            return r;
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
