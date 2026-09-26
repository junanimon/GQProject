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

        // 정답대로 채운 기입란 (선배가 써 둔 의뢰 · 긴급 의뢰)
        public static QuestEntry Correct(QuestData d) =>
            new(d.truthMonster, d.truthCount, d.truthMonster.RankFor(d.truthCount), d.truthMonster.role);
    }

    // 게임 중의 의뢰 한 건: 의뢰서(QuestData) + 기입란 + 게시·수주 상태
    public class Quest
    {
        public QuestData Data { get; }
        public bool BySenior { get; }
        public bool IsUrgent { get; }
        public bool IsGenerated { get; }
        public int Deadline { get; private set; }
        public QuestEntry Entry { get; private set; }
        public Assignment TakenBy { get; private set; }

        public bool IsTaken => TakenBy != null;
        public bool IsOpen => !IsTaken;
        public string Title => Data.title;
        public int Reward => Data.reward;
        public string Proof => Data.proof;
        public MonsterData Truth => Data.truthMonster;
        public CountBand TruthCount => Data.truthCount;
        public int TruthNumber => Data.truthNumber;
        public Rank TruthRank => Truth.RankFor(TruthCount);

        public Quest(QuestData data, bool bySenior, bool urgent = false, bool generated = false)
        {
            Data = data;
            BySenior = bySenior || urgent;
            IsUrgent = urgent;
            IsGenerated = generated;
            Deadline = urgent ? 1 : data.deadline;
            if (BySenior) Entry = QuestEntry.Correct(data);
        }

        public void Post(QuestEntry entry) => Entry = entry;
        public void AssignTo(Assignment a) => TakenBy = a;

        // 하루 지남. 기한이 끝나면 true
        public bool PassDay() => --Deadline <= 0;

        public SavedQuest Save() => new()
        {
            title = Data.title, client = Data.client, region = Data.region, description = Data.description, proof = Data.proof,
            reward = Data.reward, deadline = Data.deadline, truthNumber = Data.truthNumber,
            truthMonster = Data.truthMonster.name, truthCount = Data.truthCount,
            bySenior = BySenior, urgent = IsUrgent, deadlineLeft = Deadline,
            entryMonster = Entry.Monster.name, entryCount = Entry.Count, entryRank = Entry.Rank, entryRole = Entry.Role,
        };

        // 세이브에서 되살리기. 의뢰서는 실행 중에 새로 만든다 (자동 생성 의뢰는 에셋이 없으므로)
        public static Quest Restore(SavedQuest s, System.Func<string, MonsterData> monster)
        {
            var d = UnityEngine.ScriptableObject.CreateInstance<QuestData>();
            d.name = "(불러옴) " + s.title;
            d.title = s.title;
            d.client = s.client;
            d.region = s.region;
            d.description = s.description;
            d.proof = s.proof;
            d.reward = s.reward;
            d.deadline = s.deadline;
            d.truthMonster = monster(s.truthMonster);
            d.truthCount = s.truthCount;
            d.truthNumber = s.truthNumber;
            var q = new Quest(d, s.bySenior, s.urgent, true);
            q.Post(new QuestEntry(monster(s.entryMonster), s.entryCount, s.entryRank, s.entryRole));
            q.Deadline = s.deadlineLeft;
            return q;
        }

        public bool EntryWrong =>
            Entry.Monster != Truth || (Truth.isMonster && Entry.Count != TruthCount) ||
            Entry.Rank != TruthRank || Entry.Role != Truth.role;
    }
}
