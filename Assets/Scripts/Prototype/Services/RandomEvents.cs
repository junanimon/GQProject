using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 밤에 가끔 일어나는 작은 사건 (GameDatabase.randomEvents). 한 판에 한 번씩, 한 주에 최대 몇 개까지.
    public class RandomEvents
    {
        readonly IReadOnlyList<RandomEvent> pool;
        readonly HashSet<string> seen = new();
        int thisWeek;

        public RandomEvents(IReadOnlyList<RandomEvent> pool, IEnumerable<string> alreadySeen = null)
        {
            this.pool = pool;
            if (alreadySeen != null) foreach (var s in alreadySeen) seen.Add(s);
        }

        public IEnumerable<string> Seen => seen;

        public void NewWeek() => thisWeek = 0;

        // 오늘 밤 일어날 사건 (없으면 null)
        public RandomEvent Roll(int week, int maxPerWeek)
        {
            if (thisWeek >= maxPerWeek) return null;
            var ev = pool.Where(e => week >= e.minWeek && week <= e.maxWeek && !seen.Contains(e.title))
                         .OrderBy(_ => Random.value).FirstOrDefault(e => Random.value < e.chance);
            if (ev == null) return null;
            seen.Add(ev.title);
            thisWeek++;
            return ev;
        }

        // 효과를 적용하고 결과 한 줄을 돌려준다
        public static string Apply(RandomEvent e, Guild guild, AdventurerRoster roster)
        {
            switch (e.effect)
            {
                case RandomEffect.Funds:
                    guild.Earn(e.amount);
                    return $"길드 자금 {e.amount:+#;-#}G";
                case RandomEffect.Danger:
                    guild.ChangeDanger(e.amount);
                    return $"마을 위험도 {e.amount:+#;-#}";
                case RandomEffect.Reputation:
                    guild.ChangeReputation(e.amount);
                    return $"길드 평판 {e.amount:+#;-#}";
                case RandomEffect.AllAffinity:
                    foreach (var a in roster.Named.Where(a => !a.IsDead)) a.ChangeAffinity(e.amount);
                    return $"네임드 모두 호감도 {e.amount:+#;-#}";
                default:
                    return null;
            }
        }
    }
}
