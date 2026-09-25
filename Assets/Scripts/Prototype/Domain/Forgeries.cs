using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 카드 위조 한 종류. 새 위조는 이 인터페이스를 구현한 클래스를 만들어 ForgeryCatalog에 추가하면 된다.
    public interface IForgery
    {
        string Name { get; }
        string RuleLine { get; }
        void Apply(GuildCard card, AdventurerRoster roster);
    }

    // 공통: 실제보다 한 등급 높게 적어 오고, 종류마다 단서 하나가 어긋난다
    public abstract class ForgeryBase : IForgery
    {
        public abstract string Name { get; }
        public abstract string RuleLine { get; }

        public void Apply(GuildCard card, AdventurerRoster roster)
        {
            Rank shown = card.Holder.Rank + 1;
            card.ShownRank = shown;
            card.Border = Txt.RankColor(shown);
            card.Number = $"{Txt.RankCode(shown)}-{card.Holder.Number}";
            card.Forgery = this;
            Tamper(card, roster);
        }

        protected abstract void Tamper(GuildCard card, AdventurerRoster roster);
    }

    // 등급 글자만 고쳤다 → 테두리 색이 원래 등급
    public class BorderForgery : ForgeryBase
    {
        public override string Name => "테두리 색";
        public override string RuleLine => "<b>규정 4.</b> 카드 테두리 색은 적힌 등급과 같아야 한다. (동=갈색 · 은=은색 · 금=금색)";
        protected override void Tamper(GuildCard card, AdventurerRoster roster) => card.Border = Txt.RankColor(card.Holder.Rank);
    }

    // 카드를 통째로 만들었다 → 인장 모양이 다름
    public class SealForgery : ForgeryBase
    {
        public override string Name => "길드 인장";
        public override string RuleLine => "<b>규정 5.</b> 카드의 길드 인장은 아래 정식 인장과 모양이 같아야 한다.";
        protected override void Tamper(GuildCard card, AdventurerRoster roster) => card.FakeSeal = true;
    }

    // 남의 카드를 빌려 왔다 → 초상화가 창구의 본인과 다름
    public class PortraitForgery : ForgeryBase
    {
        public override string Name => "초상화";
        public override string RuleLine => "<b>규정 6.</b> 카드 초상화는 창구에 선 신청자 본인이어야 한다. (대리 수주 금지)";

        protected override void Tamper(GuildCard card, AdventurerRoster roster)
        {
            var owner = roster.SomeoneElse(card.Holder, card.ShownRank);
            card.Name = owner.Name;
            card.Number = $"{Txt.RankCode(card.ShownRank)}-{owner.Number}";
            card.Portrait = roster.OtherLook(card.Holder.Look);
        }
    }

    // 번호는 원래 등급 그대로 → 번호 코드가 적힌 등급과 다름
    public class NumberForgery : ForgeryBase
    {
        public override string Name => "카드 번호";
        public override string RuleLine => "<b>규정 7.</b> 카드 번호는 적힌 등급의 코드로 시작해야 한다. (동=B · 은=S · 금=G)";
        protected override void Tamper(GuildCard card, AdventurerRoster roster) => card.Number = card.Holder.CardNumber;
    }

    // 하루가 지날 때마다 하나씩 해금 (2일차부터)
    public static class ForgeryCatalog
    {
        static readonly IForgery[] unlockOrder = { new BorderForgery(), new SealForgery(), new PortraitForgery(), new NumberForgery() };

        public static IReadOnlyList<IForgery> UnlockedOn(int day) =>
            unlockOrder.Take(Mathf.Clamp(day - 1, 0, unlockOrder.Length)).ToList();

        // 그날 새로 열린 위조 (없으면 null)
        public static IForgery NewOn(int day)
        {
            var now = UnlockedOn(day);
            return now.Count > UnlockedOn(day - 1).Count ? now[now.Count - 1] : null;
        }
    }
}
