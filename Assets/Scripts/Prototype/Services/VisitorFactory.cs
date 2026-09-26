using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 게시판 의뢰를 보고 찾아오는 수주 신청자를 만든다. 일부는 규정을 어기거나 위조 카드를 들고 온다.
    // 네임드는 자기 성격(greed · forgeryTendency)대로 의뢰를 고른다.
    public class VisitorFactory
    {
        readonly AdventurerRoster roster;
        readonly GameConfig config;
        readonly float namedChance;

        public VisitorFactory(AdventurerRoster roster, GameConfig config, float namedChance)
        {
            this.roster = roster;
            this.config = config;
            this.namedChance = namedChance;
        }

        // reputation: 길드 평판이 높을수록 실력 좋은(한 등급 위) 모험가가 찾아온다
        // outside: 관할 밖 지역 (그 지역 모험가가 가끔 잘못 찾아온다 → 관할 규정 위반)
        public ApplicationVisit Create(Quest q, IReadOnlyList<IForgery> forgeries, ITempRule rule, int reputation,
            IReadOnlyList<RegionData> outside = null)
        {
            upgradeChance = 0.1f + reputation / 250f;
            var visit = rule?.TrySpecialVisit(q, roster);
            if (visit == null && outside != null && outside.Count > 0 && Random.value < config.outsideRegionChance)
                visit = FromOutside(q, outside[Random.Range(0, outside.Count)]);
            if (visit == null && Random.value < namedChance) visit = TryNamed(q, forgeries);
            visit ??= CreateNameless(q, forgeries);
            if (rule is PlagueRule plague) plague.Infect(visit);
            return visit;
        }

        // 관할 밖 지역 모험가: 등급 · 역할은 맞지만 카드의 지역 마크가 관할이 아니다
        ApplicationVisit FromOutside(Quest q, RegionData region)
        {
            var job = q.Entry.Role != Job.None ? q.Entry.Role : (Job)Random.Range(1, 5);
            var who = roster.RegisterGuest(q.Entry.Rank, job, region.displayName);
            return new ApplicationVisit(q, new List<GuildCard> { GuildCard.Of(who) },
                Lines.Pick(new[] { $"{region.displayName}에서 왔어요. 거기엔 아직 길드가 없어서… 여기서 받아 주시면 안 될까요?",
                                   "소문 듣고 멀리서 왔어요! 이 의뢰, 제 등급이면 딱이죠?" }));
        }

        float upgradeChance = 0.2f;

        // 네임드 한 명이 혼자 신청하러 온다
        ApplicationVisit TryNamed(Quest q, IReadOnlyList<IForgery> forgeries)
        {
            Rank R = q.Entry.Rank;
            Job role = q.Entry.Role;
            var picks = new List<(Adventurer who, bool forge)>();
            foreach (var a in roster.AvailableNamed)
            {
                bool roleOk = role == Job.None || a.Job == role;
                if (a.Rank >= R && roleOk) picks.Add((a, false));                                   // 규정대로
                else if (a.Rank == R - 1 && roleOk)
                {
                    if (forgeries.Count > 0 && !a.StoppedForging && Random.value < a.Named.forgeryTendency) picks.Add((a, true));  // 위조 카드 (레온은 70 이벤트 뒤로 안 씀)
                    else if (Random.value < a.Named.greed) picks.Add((a, false));                            // 욕심
                }
                else if (a.Rank >= R && Random.value < a.Named.greed * 0.5f) picks.Add((a, false));         // 역할 무시
            }
            if (picks.Count == 0) return null;

            var (who, forge) = picks[Random.Range(0, picks.Count)];
            var card = GuildCard.Of(who);
            if (forge) forgeries[Random.Range(0, forgeries.Count)].Apply(card, roster);
            string line = forge ? who.Line(LineKind.Greeting, Lines.Forger) : who.Line(LineKind.Greeting, Lines.Accept);
            return new ApplicationVisit(q, new List<GuildCard> { card }, line);
        }

        ApplicationVisit CreateNameless(Quest q, IReadOnlyList<IForgery> forgeries)
        {
            var used = new HashSet<Adventurer>();
            Rank R = q.Entry.Rank;
            Job role = q.Entry.Role;
            bool hasRole = role != Job.None;
            bool canLow = R > Rank.Bronze;
            Rank low = canLow ? R - 1 : R;

            if (forgeries.Count > 0 && Random.value < config.forgeryChance)
            {
                var who = roster.Find(low == Rank.Gold ? Rank.Silver : low, used, role);
                var card = GuildCard.Of(who);
                forgeries[Random.Range(0, forgeries.Count)].Apply(card, roster);
                return new ApplicationVisit(q, new List<GuildCard> { card }, Lines.Pick(Lines.Forger));
            }

            var members = new List<Adventurer>();
            string line;
            bool violate = Random.value < config.violationChance && (canLow || hasRole);
            if (!violate)
            {
                float t = Random.value;
                if (t < 0.55f)
                {
                    Rank r = R < Rank.Gold && Random.value < upgradeChance ? R + 1 : R;
                    members.Add(roster.Find(r, used, role));
                }
                else if (t < 0.8f || !canLow)
                {
                    members.Add(roster.Find(R, used, role));
                    members.Add(roster.Find(R, used));
                }
                else
                {
                    members.Add(roster.Find(low, used, role));
                    members.Add(roster.Find(low, used));
                    members.Add(roster.Find(R, used));
                }
                line = members.Count == 1 ? Lines.Pick(Lines.Accept) : Lines.Pick(Lines.Party);
            }
            else
            {
                var kinds = new List<int>();
                if (canLow) { kinds.Add(0); kinds.Add(2); }
                if (hasRole) kinds.Add(1);
                switch (kinds[Random.Range(0, kinds.Count)])
                {
                    case 0: // 욕심 많은 신입
                        members.Add(roster.Find(low, used, role));
                        line = Random.value < 0.5f ? Lines.Pick(Lines.Greedy) : Lines.Pick(Lines.Accept);
                        break;
                    case 1: // 필수 역할 없음
                        members.Add(roster.Find(R, used, avoid: role));
                        line = Lines.Pick(Lines.Accept);
                        break;
                    default: // 인원 부족 파티
                        members.Add(roster.Find(low, used, role));
                        members.Add(roster.Find(low, used));
                        line = Lines.Pick(Lines.Party);
                        break;
                }
            }
            return new ApplicationVisit(q, members.Select(GuildCard.Of).ToList(), line);
        }
    }
}
