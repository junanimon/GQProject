using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 게임 데이터 모음 (Assets/Data/GameDatabase). GuildGame이 참조한다.
    [CreateAssetMenu(menuName = "GQ/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public List<MonsterData> monsters = new();
        [Tooltip("게임 시작 때 게시판에 붙어 있는 의뢰 (선배가 정답대로 작성)")]
        public List<QuestData> seniorQuests = new();
        [Tooltip("n번째 = n일차 밤에 작성할 의뢰서")]
        public List<NightSheets> nights = new();

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

        [Header("스토리")]
        public List<StoryLine> intro = new();
        [Tooltip("주말 습격 도입")]
        public List<StoryLine> raidIntro = new();
        [Tooltip("{defenders} = 지원 온 네임드 이름들, {hero} = 가장 호감도 높은 네임드")]
        public List<StoryLine> raidPerfect = new();
        public List<StoryLine> raidClose = new();
        public List<StoryLine> raidFail = new();

        [Tooltip("몬스터 없는 의뢰의 증거물 아이콘")]
        public Sprite proofIcon;
    }
}
