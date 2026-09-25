using UnityEngine;

namespace GuildProto
{
    // 의뢰서 한 장 (Assets/Data/Quests)
    [CreateAssetMenu(menuName = "GQ/Quest", fileName = "Quest")]
    public class QuestData : ScriptableObject
    {
        public string title;
        public string client;
        public string region;
        [TextArea(4, 12)] public string description;
        public int deadline = 2;
        public int reward = 100;
        [Tooltip("몬스터 없는 의뢰의 완료 확인 물품")]
        public string proof;

        [Header("숨겨진 정답")]
        public MonsterData truthMonster;
        public CountBand truthCount;
        public int truthNumber;
    }
}
