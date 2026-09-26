using System.Collections.Generic;

namespace GuildProto
{
    // 긴급 의뢰: 영업 중에 전령이 뛰어 들어온다. 선배가 기입을 마친 의뢰서를 바로 게시판에 붙인다.
    // 오늘 안에 아무도 받지 않으면 마감 때 위험도가 크게 오르고 평판이 떨어진다.
    public class UrgentVisit : Visit
    {
        readonly Quest quest;
        readonly string messenger;
        readonly CharacterLook look;
        public override Quest Quest => quest;

        public UrgentVisit(Quest quest, string messenger, CharacterLook look)
            : base(new List<GuildCard>(), "헉, 헉… 기, 긴급 의뢰예요! 오늘 안에 누가 가 주지 않으면 큰일 나요!")
        {
            this.quest = quest;
            this.messenger = messenger;
            this.look = look;
        }

        public override void Present(Counter c)
        {
            c.View.ShowMessenger(messenger, look, Line, "긴급 의뢰 — 선배가 기입을 마쳤다. 게시하고, 오늘 안에 누군가에게 맡길 것");
            c.View.ShowQuestDoc(quest, 0);
            c.View.SetMode(DayView.Mode.Confirm, "긴급 게시");
            c.View.Alarm();
        }

        public override string Confirm(Counter c)
        {
            c.PostUrgent(quest);
            c.View.Float("긴급 의뢰 게시!", false);
            return "부탁드려요! 길드 홀에서 직접 지명하셔도 돼요!";
        }
    }
}
