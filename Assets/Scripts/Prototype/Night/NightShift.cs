using System.Collections.Generic;
using System.Linq;

namespace GuildProto
{
    // 하룻밤 업무: 의뢰서를 한 장씩 기입해 게시한다
    public class NightShift
    {
        readonly List<Quest> sheets;

        public int Day { get; }
        public int Index { get; private set; }
        public int Count => sheets.Count;
        public bool IsDone => Index >= Count;
        public Quest Current => sheets[Index];

        public NightShift(int day, IEnumerable<QuestData> data)
        {
            Day = day;
            sheets = data.Select(d => new Quest(d, false)).ToList();
        }

        // 지금 의뢰서에 기입란을 채워 게시하고 다음 장으로
        public Quest Post(QuestEntry entry)
        {
            var q = Current;
            q.Post(entry);
            Index++;
            return q;
        }
    }
}
