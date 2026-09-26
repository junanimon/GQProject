using System.Collections.Generic;

namespace GuildProto
{
    // 하루 영업 기록 (밤 결과 화면과 결산에 쓴다)
    public class DayRecord
    {
        public int Day { get; }
        public int Approved;
        public int Rejected;
        public int WrongRejects;
        public int Fees;
        public int Sent;
        public int Nominations;
        public int UrgentHandled;
        public int UrgentMissed;
        public int Bonuses;
        public int DangerBefore;
        public int DangerAfter;
        public readonly List<string> Violations = new();
        public readonly List<Quest> Expired = new();
        public readonly List<Adventurer> Deaths = new();
        public readonly Ledger Ledger = new();

        public DayRecord(int day) => Day = day;
    }
}
