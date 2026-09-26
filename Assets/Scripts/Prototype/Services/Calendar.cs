using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 날짜 계산: 전체 n일째 → 몇 주차 몇째 날, 그날까지 열린 규칙
    public class Calendar
    {
        readonly IReadOnlyList<WeekData> weeks;
        public int DaysPerWeek { get; }

        public Calendar(IReadOnlyList<WeekData> weeks, int daysPerWeek)
        {
            this.weeks = weeks;
            DaysPerWeek = daysPerWeek;
        }

        public int TotalDays => weeks.Count * DaysPerWeek;
        public int WeekOf(int day) => (day - 1) / DaysPerWeek + 1;
        public int DayOfWeek(int day) => (day - 1) % DaysPerWeek + 1;
        public bool IsFirstDayOfWeek(int day) => DayOfWeek(day) == 1;
        public bool IsLastDayOfWeek(int day) => DayOfWeek(day) == DaysPerWeek;
        public bool IsLastWeek(int day) => WeekOf(day) == weeks.Count;
        public WeekData Week(int day) => weeks[System.Math.Min(WeekOf(day), weeks.Count) - 1];

        public string Label(int day) => $"{WeekOf(day)}주차 {DayOfWeek(day)}일";

        // 그날 기준으로 열려 있는 해금 (이전 주 전부 + 이번 주의 오늘까지)
        public IReadOnlyList<Unlock> UnlockedOn(int day)
        {
            int w = WeekOf(day), d = DayOfWeek(day);
            var list = new List<Unlock>();
            for (int i = 0; i < w && i < weeks.Count; i++)
                list.AddRange(weeks[i].unlocks.Where(u => i < w - 1 || u.day <= d).Select(u => u.unlock));
            return list;
        }

        public IReadOnlyList<Unlock> NewOn(int day) =>
            UnlockedOn(day).Except(day > 1 ? UnlockedOn(day - 1) : new List<Unlock>()).ToList();

        public bool IsUnlocked(int day, Unlock u) => UnlockedOn(day).Contains(u);

        public DayStory StoryFor(int day, string id = null) =>
            Week(day).dayStories.FirstOrDefault(s => s.day == DayOfWeek(day) && (id == null ? string.IsNullOrEmpty(s.id) : s.id == id));

        public IEnumerable<DayStory> StoriesFor(int day) => Week(day).dayStories.Where(s => s.day == DayOfWeek(day));
    }
}
