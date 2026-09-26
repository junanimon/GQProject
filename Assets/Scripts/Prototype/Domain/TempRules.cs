using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 주차 전체에 걸리는 임시 규칙 (축제 · 수배 · 전염병).
    // 규정 문구, 창구 검사, 이 규칙 때문에 찾아오는 특별한 방문객을 스스로 정한다.
    public interface ITempRule
    {
        string Name { get; }
        string RuleLine { get; }
        // 수주 신청 검사. 문제 없으면 null
        string Check(IReadOnlyList<GuildCard> cards);
        // 이 규칙 때문에 오는 특별한 수주 신청자 (없으면 null)
        ApplicationVisit TrySpecialVisit(Quest q, AdventurerRoster roster);
        // 신고 종을 쓸 수 있는가
        bool AllowsReport { get; }
        // 관할 밖 지역 카드라도 이 규칙 기간엔 받아 주는가 (축제의 협력 지역)
        bool AcceptsRegion(string region);
    }

    public static class TempRuleFactory
    {
        public static ITempRule Create(WeekData week) => week.tempRule switch
        {
            TempRuleKind.Festival => new FestivalRule(week.partnerRegions, week.fakeRegions),
            TempRuleKind.Wanted => new WantedRule(week.wanted),
            TempRuleKind.Plague => new PlagueRule(),
            _ => null,
        };
    }

    // 축제 주: 관할 밖 지역 모험가가 몰려온다. 관할 지역 + 협력 지역 카드만 인정 (가짜 지역 이름을 대는 사람도 섞임)
    public class FestivalRule : ITempRule
    {
        readonly List<string> partners;
        readonly List<string> fakes;
        const float ForeignChance = 0.35f, FakeChance = 0.3f;

        public FestivalRule(List<string> partners, List<string> fakes)
        {
            this.partners = partners;
            this.fakes = fakes;
        }

        public string Name => "축제 주간";
        public string RuleLine => "<b>축제 임시 규정.</b> 축제 기간엔 관할 지역에 더해 <b>협력 지역</b> 마크 카드도 인정한다.\n  협력 지역: " + string.Join(" · ", partners);
        public bool AllowsReport => false;
        public bool AcceptsRegion(string region) => partners.Contains(region);

        // 지역 검사는 Regulations의 관할 검사가 맡는다 (AcceptsRegion으로 협력 지역을 허용)
        public string Check(IReadOnlyList<GuildCard> cards) => null;

        public ApplicationVisit TrySpecialVisit(Quest q, AdventurerRoster roster)
        {
            if (Random.value > ForeignChance) return null;
            bool fake = fakes.Count > 0 && Random.value < FakeChance;
            string region = fake ? fakes[Random.Range(0, fakes.Count)] : partners[Random.Range(0, partners.Count)];
            var job = q.Entry.Role != Job.None ? q.Entry.Role : (Job)Random.Range(1, 5);
            var who = roster.RegisterGuest(q.Entry.Rank, job, region);
            string line = fake
                ? Lines.Pick(new[] { $"{region}에서 왔소. 축제 구경 겸 일도 좀 하려고.", "먼 데서 왔어요. 카드요? 여기요, 여기." })
                : Lines.Pick(new[] { $"{region}에서 왔습니다. 축제 기간 동안 신세 좀 질게요!", $"안녕하세요, {region} 쪽 모험가예요. 이 의뢰 되나요?" });
            return new ApplicationVisit(q, new List<GuildCard> { GuildCard.Of(who) }, line);
        }
    }

    // 수배 주간: 수배자가 모험가 행세를 하고 온다. 신고하면 현상금
    public class WantedRule : ITempRule
    {
        readonly List<WantedPoster> posters;
        const float CriminalChance = 0.18f, LookalikeChance = 0.1f;

        public WantedRule(List<WantedPoster> posters) => this.posters = posters;

        public IReadOnlyList<WantedPoster> Posters => posters;
        public string Name => "수배 주간";
        public string RuleLine => "<b>수배 임시 규정.</b> 수배서의 인물(얼굴 · 이름 · 가명)이 창구에 오면 수주를 받지 말고 <b>신고 종</b>을 울린다.";
        public bool AllowsReport => true;
        public bool AcceptsRegion(string region) => false;

        public string Check(IReadOnlyList<GuildCard> cards)
        {
            var c = cards.FirstOrDefault(x => x.Holder.Criminal != null);
            return c == null ? null : $"수배자 — {c.Holder.Criminal.name} (가명 '{c.Name}')";
        }

        public ApplicationVisit TrySpecialVisit(Quest q, AdventurerRoster roster)
        {
            if (posters.Count == 0) return null;
            var poster = posters[Random.Range(0, posters.Count)];
            float roll = Random.value;
            if (roll < CriminalChance)
            {
                var job = q.Entry.Role != Job.None ? q.Entry.Role : Job.Warrior;
                var crook = new Adventurer(Random.value < 0.5f ? poster.name : poster.alias, poster.fakeRank, job, poster.look,
                    Random.Range(1000, 10000), 30).AsGuestFrom(roster.HomeRegion.displayName, roster.HomeRegion).AsCriminal(poster);
                return new ApplicationVisit(q, new List<GuildCard> { GuildCard.Of(crook) },
                    Lines.Pick(new[] { "…빨리 처리해 주쇼.", "(모자를 깊게 눌러쓴다) 이 의뢰.", "하하, 별일 없죠? 접수나 해 줘요." }));
            }
            if (roll < CriminalChance + LookalikeChance)
            {
                // 이름만 같은 선량한 모험가 (얼굴이 다르다) — 신고하면 안 된다
                var who = roster.RegisterGuest(q.Entry.Rank, q.Entry.Role != Job.None ? q.Entry.Role : Job.Archer);
                var card = GuildCard.Of(who);
                card.Name = poster.name;
                return new ApplicationVisit(q, new List<GuildCard> { card },
                    "저기요… 수배서에 제 이름이랑 똑같은 사람이 있던데, 저 아니에요! 얼굴 보세요, 얼굴!");
            }
            return null;
        }
    }

    // 전염병 주간: 기침·창백한 안색의 모험가는 수주 금지
    public class PlagueRule : ITempRule
    {
        const float SickChance = 0.22f;

        public string Name => "전염병 주간";
        public string RuleLine => "<b>전염병 임시 규정.</b> 기침을 하거나 안색이 창백한 모험가는 의뢰를 받을 수 없다. (치료 후 재방문)";
        public bool AllowsReport => false;
        public bool AcceptsRegion(string region) => false;

        public string Check(IReadOnlyList<GuildCard> cards)
        {
            var c = cards.FirstOrDefault(x => x.Holder.IsSick);
            return c == null ? null : $"전염병 규정 위반 — {c.Name}에게 증상";
        }

        // 전염병은 방문객을 새로 만들지 않고 기존 신청자를 병들게 한다 (VisitorFactory가 호출)
        public ApplicationVisit TrySpecialVisit(Quest q, AdventurerRoster roster) => null;

        public void Infect(ApplicationVisit v)
        {
            if (Random.value > SickChance) return;
            var target = v.Cards[Random.Range(0, v.Cards.Count)].Holder;
            if (target.Named != null && target.Job == Job.Priest) return;     // 성직자는 스스로 치료
            target.SetSick(true);
        }
    }
}
