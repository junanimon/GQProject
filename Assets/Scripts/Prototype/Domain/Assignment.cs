using System.Collections.Generic;

namespace GuildProto
{
    // 수주 한 건: 누가 어떤 의뢰를 받아 나갔고, 어떻게 돌아왔는가
    public class Assignment
    {
        public Quest Quest { get; }
        public IReadOnlyList<Adventurer> Members { get; }
        public string Violation { get; }            // 수주 당시 규정 위반 (없으면 null)
        public int DayAccepted { get; }

        public bool IsResolved { get; private set; }
        public bool IsReported { get; private set; }
        public Result Result { get; private set; }
        public string Report { get; private set; }
        public int DangerDelta { get; private set; }
        public bool ClaimsSuccess { get; private set; }
        public bool IsLying { get; private set; }
        public Evidence Evidence { get; private set; }  // 없으면 null

        public Assignment(Quest quest, IReadOnlyList<Adventurer> members, string violation, int day)
        {
            Quest = quest;
            Members = members;
            Violation = violation;
            DayAccepted = day;
            quest.AssignTo(this);
            foreach (var m in members) m.Depart();
        }

        public string MemberNames => Members.Count == 1 ? Members[0].Name : $"{Members[0].Name} 외 {Members.Count - 1}명";

        public void SetOutcome(Result result, string report, int dangerDelta, bool claimsSuccess, bool lying, Evidence evidence)
        {
            Result = result;
            Report = report;
            DangerDelta = dangerDelta;
            ClaimsSuccess = claimsSuccess;
            IsLying = lying;
            Evidence = evidence;
            IsResolved = true;
        }

        // 창구에서 귀환 보고를 마침 (수수료 여부와 관계없이)
        public void CompleteReturn(Guild guild)
        {
            guild.ChangeDanger(DangerDelta);
            IsReported = true;
            foreach (var m in Members) m.Return();
        }
    }
}
