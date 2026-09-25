using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 몬스터 한 종 (Assets/Data/Monsters). 그림은 illustration / evidenceIcon에 스프라이트를 넣으면 된다.
    [CreateAssetMenu(menuName = "GQ/Monster", fileName = "Monster")]
    public class MonsterData : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("대사에 쓰는 이름 (예: 몬스터 없는 일)")]
        public string talkName;
        [TextArea(3, 8)] public string features;

        [Header("이미지")]
        public Sprite illustration;
        [Tooltip("임시 그림 색. 실제 그림을 넣으면 흰색으로")]
        public Color illustrationTint = Color.white;
        public string evidenceName;
        public Sprite evidenceIcon;
        public Color evidenceTint = Color.white;

        [Header("판정")]
        [Tooltip("수량 1~3 / 4~7 / 8 이상일 때 권장 등급")]
        public Rank[] rankByBand = new Rank[3];
        public Job role;
        public bool isMonster = true;
        [Tooltip("허위 보고 때 대신 내미는 비슷한 몬스터")]
        public MonsterData lookAlike;

        public Rank RankFor(CountBand b) => rankByBand[(int)b];
    }

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

    [Serializable]
    public class NightSheets
    {
        public string label;
        public List<QuestData> sheets = new();
    }

    // 모험가 외형. body = 창구에 선 모습(전신/반신), portrait = 길드 카드 초상화
    [Serializable]
    public class CharacterLook
    {
        public string label;
        public Sprite body;
        public Sprite portrait;
        [Tooltip("임시 그림 색. 실제 그림을 넣으면 흰색으로")]
        public Color tint = Color.white;
    }

    // 대화 한 줄. speaker가 비어 있으면 지문(나레이션)
    [Serializable]
    public class StoryLine
    {
        public string speaker;
        [TextArea(2, 6)] public string text;
    }

    // 네임드 개인 이벤트: 호감도가 minAffinity 이상이 된 날 밤에 한 번 재생
    [Serializable]
    public class PersonalEvent
    {
        public string title;
        public int minAffinity = 55;
        public List<StoryLine> lines = new();
    }

    // 네임드 모험가 (Assets/Data/Characters). 서사 · 성격 · 대사 · 개인 이벤트
    [CreateAssetMenu(menuName = "GQ/Named Character", fileName = "Character")]
    public class NamedCharacterData : ScriptableObject
    {
        public string displayName;
        [Tooltip("칭호 (예: 자칭 대마법사)")]
        public string epithet;
        public Rank rank;
        public Job job;
        public CharacterLook look;
        [Range(0, 100)] public int startAffinity = 50;

        [Header("서사")]
        [TextArea(2, 5)] public string profile;
        [TextArea(3, 8)] [Tooltip("기획용 숨은 사연 (게임엔 개인 이벤트로만 드러남)")]
        public string secret;

        [Header("성격 (방문 성향)")]
        [Range(0, 1)] [Tooltip("자기 등급보다 높은 의뢰를 노리는 정도")]
        public float greed;
        [Range(0, 1)] [Tooltip("위조 카드를 쓰는 정도 (위조 검사가 해금된 뒤)")]
        public float forgeryTendency;

        [Header("대사")]
        public string[] greetings;
        public string[] thanks;
        public string[] rejected;
        public string[] caught;
        public string[] returnSuccess;
        public string[] returnFail;
        public string[] chats;

        [Header("개인 이벤트 (호감도 순)")]
        public List<PersonalEvent> events = new();
    }

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
