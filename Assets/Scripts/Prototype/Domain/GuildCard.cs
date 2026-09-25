using UnityEngine;

namespace GuildProto
{
    // 창구에 제출된 길드 카드 한 장. 위조(IForgery)가 내용을 바꿔 놓을 수 있다.
    public class GuildCard
    {
        public Adventurer Holder { get; }           // 실제로 카드를 낸 사람
        public string Name { get; internal set; }
        public Rank ShownRank { get; internal set; }
        public Job Job { get; }
        public Color Border { get; internal set; }
        public CharacterLook Portrait { get; internal set; }
        public bool FakeSeal { get; internal set; }
        public string Number { get; internal set; }
        public IForgery Forgery { get; internal set; }

        public bool IsForged => Forgery != null;

        GuildCard(Adventurer holder)
        {
            Holder = holder;
            Name = holder.Name;
            ShownRank = holder.Rank;
            Job = holder.Job;
            Border = Txt.RankColor(holder.Rank);
            Portrait = holder.Look;
            Number = holder.CardNumber;
        }

        public static GuildCard Of(Adventurer a) => new(a);
    }
}
