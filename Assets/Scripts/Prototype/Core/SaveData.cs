using System;
using System.Collections.Generic;

namespace GuildProto
{
    // 주 시작 때 자동 저장되는 한 판의 상태 (JsonUtility로 저장)
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int day;
        public int funds, danger, reputation;
        public int guildRank = 1;
        public bool grahamUsed;
        public List<SavedAdventurer> adventurers = new();
        public List<SavedQuest> board = new();
        public List<string> awakened = new();
        public List<FacilityCount> facilities = new();
        public List<string> randomSeen = new();
        public SavedExpedition expedition;
        public RunStats stats = new();
        public string savedAt;
    }

    [Serializable]
    public class SavedAdventurer
    {
        public string name;
        public string named;        // 네임드 데이터 이름 (이름 없는 모험가는 빈 값)
        public int look = -1;       // 이름 없는 모험가 외형 번호 (GameDatabase.looks)
        public Rank rank;
        public Job job;
        public int number;
        public int affinity;
        public int level;
        public int successes;
        public int appliedAt;
        public int sorties;
        public bool overpromoted;
        public int injured;
        public bool dead;
        public bool stoppedForging;
        public string region;
        public bool guest;
        public List<string> seen = new();
    }

    [Serializable]
    public class SavedQuest
    {
        public string title, client, region, description, proof;
        public int reward, deadline, truthNumber;
        public string truthMonster;
        public CountBand truthCount;
        public bool bySenior, urgent;
        public int deadlineLeft;
        public string entryMonster;
        public CountBand entryCount;
        public Rank entryRank;
        public Job entryRole;
    }

    [Serializable]
    public class SavedExpedition
    {
        public string companion;
        public bool started, resolved, survived;
        public int fatigue;
    }

    [Serializable]
    public class FacilityCount
    {
        public FacilityKind kind;
        public int count;
    }
}
