using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 길드 홀 지명: 접수원이 쉬고 있는 모험가를 직접 골라 의뢰를 맡긴다.
    // 모험가는 보수에 얹어 줄 '지명 수당'을 요구하고, 흥정할 수 있다 (호감도가 높을수록 잘 먹힌다).
    public class Nomination
    {
        public Adventurer Who { get; }
        public Quest Quest { get; }
        public int Ask { get; private set; }
        public bool Haggled { get; private set; }
        public bool WalkedOff { get; private set; }

        public Nomination(Adventurer who, Quest quest)
        {
            Who = who;
            Quest = quest;
            int gap = Mathf.Max(0, (int)quest.Entry.Rank - (int)who.Rank + 1);
            float greed = who.Named != null ? who.Named.greed : 0.3f;
            float ask = 10 + gap * 10 + greed * 20 + quest.Reward * 0.05f - (who.Affinity - 50) * 0.2f;
            Ask = Mathf.Max(5, Mathf.RoundToInt(ask / 5f) * 5);
        }

        public string Offer =>
            $"{Quest.Title}? 흠… 보수 {Quest.Reward}G에 <b>지명 수당 {Ask}G</b>를 얹어 주시면 갈게요.";

        // 흥정: 성공하면 수당이 절반, 실패하면 기분 상해서 돌아간다
        public string Haggle()
        {
            Haggled = true;
            if (Random.value < 0.25f + Who.Affinity / 150f)
            {
                Ask = Mathf.Max(0, Mathf.RoundToInt(Ask / 2f / 5f) * 5);
                Who.ChangeAffinity(-1);
                return Ask == 0 ? "…알았어요, 당신 부탁이니까 수당은 됐어요." : $"에이… 좋아요, {Ask}G로 하죠.";
            }
            WalkedOff = true;
            Who.ChangeAffinity(-4);
            return "그 돈으론 안 가요. 다른 사람 알아보세요.";
        }

        // 오늘 길드 홀에 나와 있는 사람: 네임드는 각자 확률로, 이름 없는 모험가는 몇 명만 (매일 달라진다)
        public static List<Adventurer> PresentToday(AdventurerRoster roster, float namedChance, int namelessCount)
        {
            var idle = roster.All.Where(a => a.IsAvailable && !a.IsGuest && a.Criminal == null).ToList();
            var named = idle.Where(a => a.IsNamed && Random.value < namedChance);
            var nameless = idle.Where(a => !a.IsNamed).OrderBy(_ => Random.value).Take(namelessCount);
            return named.Concat(nameless).ToList();
        }
    }
}
