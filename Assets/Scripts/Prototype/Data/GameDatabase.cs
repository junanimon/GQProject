using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 게임 데이터 모음 (Assets/Data/GameDatabase). GuildGame이 참조한다.
    [CreateAssetMenu(menuName = "GQ/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [Tooltip("몬스터 전체 (세이브 복원 · 해당 없음 찾기용). 도감 카테고리는 regions가 정한다")]
        public List<MonsterData> monsters = new();
        [Tooltip("지역 (관할 순서대로). 길드 등급이 오르면 하나씩 관할에 들어온다")]
        public List<RegionData> regions = new();
        [Tooltip("게임 시작 때 게시판에 붙어 있는 의뢰 (선배가 정답대로 작성)")]
        public List<QuestData> seniorQuests = new();

        [Header("주차 (n번째 = n주차)")]
        public List<WeekData> weeks = new();
        public QuestGeneratorData generator;

        [Header("모험가")]
        public List<NamedCharacterData> namedCharacters = new();
        [Tooltip("이름 없는 모험가 외형")]
        public List<CharacterLook> looks = new();
        [Range(0, 1)] [Tooltip("수주 신청자가 네임드일 확률")]
        public float namedVisitChance = 0.4f;

        [Header("선배 접수원 · NPC")]
        public string seniorName = "마르타";
        public CharacterLook seniorLook;
        [Tooltip("스토리에 나오는 그 밖의 인물. label = 대사의 화자 이름")]
        public List<CharacterLook> npcLooks = new();
        [Tooltip("긴급 의뢰를 들고 오는 사람 (npcLooks의 label과 같으면 그 외형)")]
        public string urgentMessenger = "전령";

        [Header("스토리")]
        public List<StoryLine> intro = new();
        [Tooltip("네임드가 사망했을 때 추모 장면. {name} {epithet}")]
        public List<StoryLine> memorial = new();
        [Tooltip("엔딩 (마지막 주 습격 결과별)")]
        public List<StoryLine> endingGood = new();
        public List<StoryLine> endingBitter = new();
        [Tooltip("엔딩 도감에 올라가는 엔딩 전체 (굿 · 씁쓸 · 주차별 배드)")]
        public List<EndingData> endings = new();
        public List<RandomEvent> randomEvents = new();

        [Header("밤 상점")]
        public List<Facility> facilities = new();

        [Header("승급 심사")]
        [Tooltip("동→은 / 은→금 에 필요한 레벨")]
        public int[] promotionLevel = { 6, 12 };
        [Tooltip("동→은 / 은→금 에 필요한 성공 실적")]
        public int[] promotionRecord = { 3, 6 };

        [Tooltip("몬스터 없는 의뢰의 증거물 아이콘")]
        public Sprite proofIcon;
    }
}
