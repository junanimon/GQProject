using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 자동 생성 의뢰: 몬스터 단서 + 의뢰인 말투(과장 · 축소 · 정확 · 착각) 조합. 몬스터는 관할 지역 + 이 주의 출몰 몬스터
    public class QuestGenerator
    {
        readonly QuestGeneratorData data;

        public QuestGenerator(QuestGeneratorData data) => this.data = data;

        // regional: 관할 지역 몬스터. 이 주에 몰려오는 몬스터(week.monsterPool)와 합쳐서 고른다
        public QuestData Create(WeekData week, IEnumerable<MonsterData> regional)
        {
            var pool = week.monsterPool.Concat(regional ?? Enumerable.Empty<MonsterData>()).Where(m => m != null && m.isMonster).Distinct().ToList();
            if (pool.Count == 0 || Random.value < week.errandChance) return Errand();
            return Hunt(pool[Random.Range(0, pool.Count)]);
        }

        QuestData Hunt(MonsterData m)
        {
            var band = (CountBand)Random.Range(0, 3);
            int n = Random.Range(Txt.BandMin(band), Txt.BandMax(band) + 1);
            var client = data.clients[Random.Range(0, data.clients.Count)];
            var clues = (m.clues ?? new string[0]).OrderBy(_ => Random.value).Take(2).ToList();

            var desc = new List<string>();
            desc.AddRange(clues);
            string trace = m.countTrace.Replace("{n}", n.ToString());
            switch (client.tone)
            {
                case ClientTone.Exaggerate:
                    desc.Add(Pick(data.exaggerateLines).Replace("{n}", (n * Random.Range(3, 8) + 5).ToString()));
                    desc.Add(Pick(data.truthHints).Replace("{trace}", trace));
                    break;
                case ClientTone.Understate:
                    desc.Add(Pick(data.understateLines).Replace("{n}", Mathf.Max(1, n / 3).ToString()));
                    desc.Add(Pick(data.truthHints).Replace("{trace}", trace));
                    break;
                default:
                    desc.Add(Pick(data.accurateLines).Replace("{n}", n.ToString()));
                    break;
            }
            // 가끔 비슷한 몬스터로 착각한다 (단서는 진짜)
            if (m.lookAlike != null && m.lookAlike.isMonster && Random.value < 0.35f)
                desc.Add(Pick(data.misnameLines).Replace("{other}", m.lookAlike.talkName));

            var q = ScriptableObject.CreateInstance<QuestData>();
            q.name = "(생성) " + m.displayName;
            q.title = m.questTitles != null && m.questTitles.Length > 0 ? Pick(m.questTitles) : $"{m.displayName} 퇴치";
            q.client = client.label;
            q.region = Pick(data.regions);
            q.description = string.Join(" ", desc);
            q.truthMonster = m;
            q.truthCount = band;
            q.truthNumber = n;
            q.deadline = Random.Range(2, 4);
            var rank = m.RankFor(band);
            int baseReward = rank switch { Rank.Bronze => 80, Rank.Silver => 180, _ => 320 };
            q.reward = Mathf.RoundToInt(baseReward * (client.tone == ClientTone.Understate ? 0.6f : 1f) * Random.Range(0.85f, 1.2f) / 10f) * 10;
            return q;
        }

        QuestData Errand()
        {
            var e = data.errands[Random.Range(0, data.errands.Count)];
            var none = ScriptableObject.CreateInstance<QuestData>();
            none.name = "(생성) 잡무";
            none.title = e.title;
            none.client = data.clients[Random.Range(0, data.clients.Count)].label;
            none.region = Pick(data.regions);
            none.description = e.description;
            none.proof = e.proof;
            none.reward = e.reward;
            none.deadline = Random.Range(2, 4);
            none.truthMonster = NoneMonster;
            none.truthCount = CountBand.Few;
            return none;
        }

        // '해당 없음' 몬스터 (GuildGame이 데이터에서 찾아 넣어 준다)
        public MonsterData NoneMonster { get; set; }

        static string Pick(IReadOnlyList<string> list) => list.Count == 0 ? "" : list[Random.Range(0, list.Count)];
    }
}
