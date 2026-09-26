using System;
using System.Collections.Generic;

namespace GuildProto
{
    // 한 판 전체의 누적 기록 (결산 화면 · 세이브)
    [Serializable]
    public class RunStats
    {
        public int approved, rejected, violations, deaths;
        public int written, writtenRight;
        public int promotions, overpromotions;
        public int nominations, urgentHandled, urgentMissed;
        public List<RaidSummary> raids = new();

        public void AddDay(DayRecord r)
        {
            approved += r.Approved;
            rejected += r.Rejected;
            violations += r.Violations.Count;
            deaths += r.Deaths.Count;
        }

        public void AddWritten(Quest q)
        {
            written++;
            if (!q.EntryWrong) writtenRight++;
        }

        public void AddRaid(RaidResult r) => raids.Add(new RaidSummary { title = r.Title, grade = r.Grade, attack = r.Attack, defense = r.Defense });
    }

    [Serializable]
    public class RaidSummary
    {
        public string title;
        public RaidGrade grade;
        public int attack, defense;
    }
}
