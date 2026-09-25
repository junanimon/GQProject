namespace GuildProto
{
    // 밤에 접수원이 채우는 길드 기입란
    public class QuestEntry
    {
        public MonsterData Monster { get; }
        public CountBand Count { get; }
        public Rank Rank { get; }
        public Job Role { get; }

        public QuestEntry(MonsterData monster, CountBand count, Rank rank, Job role)
        {
            Monster = monster;
            Count = count;
            Rank = rank;
            Role = role;
        }

        // 정답대로 채운 기입란 (선배가 써 둔 의뢰)
        public static QuestEntry Correct(QuestData d) =>
            new(d.truthMonster, d.truthCount, d.truthMonster.RankFor(d.truthCount), d.truthMonster.role);
    }

    // 게임 중의 의뢰 한 건: 의뢰서(QuestData) + 기입란 + 수주 상태
    public class Quest
    {
        public QuestData Data { get; }
        public bool BySenior { get; }
        public int Deadline { get; private set; }
        public QuestEntry Entry { get; private set; }
        public Assignment TakenBy { get; private set; }

        public bool IsTaken => TakenBy != null;
        public string Title => Data.title;
        public int Reward => Data.reward;
        public string Proof => Data.proof;
        public MonsterData Truth => Data.truthMonster;
        public CountBand TruthCount => Data.truthCount;
        public int TruthNumber => Data.truthNumber;
        public Rank TruthRank => Truth.RankFor(TruthCount);

        public Quest(QuestData data, bool bySenior)
        {
            Data = data;
            BySenior = bySenior;
            Deadline = data.deadline;
            if (bySenior) Entry = QuestEntry.Correct(data);
        }

        public void Post(QuestEntry entry) => Entry = entry;
        public void AssignTo(Assignment a) => TakenBy = a;

        // 하루 지남. 기한이 끝나면 true
        public bool PassDay() => --Deadline <= 0;

        public bool EntryWrong =>
            Entry.Monster != Truth || (Truth.isMonster && Entry.Count != TruthCount) ||
            Entry.Rank != TruthRank || Entry.Role != Truth.role;
    }
}
