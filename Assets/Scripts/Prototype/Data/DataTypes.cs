using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 데이터 에셋 안에 들어가는 작은 묶음들
    // (ScriptableObject는 파일 이름과 클래스 이름이 같아야 해서 각자 파일로 뺐고, 여기엔 일반 클래스만 둔다)

    // 주차별 새 규칙 해금
    public enum Unlock { Border, Seal, Portrait, Number, Death }

    // 주차 전체에 걸리는 임시 규칙
    public enum TempRuleKind { None, Festival, Wanted, Plague }

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

    // 네임드 개인 이벤트: 호감도가 minAffinity 이상이고 minWeek주차 이상인 날 밤에 한 번 재생
    [Serializable]
    public class PersonalEvent
    {
        public string title;
        public int minAffinity = 55;
        [Tooltip("이 주차부터 나올 수 있다")]
        public int minWeek = 1;
        [Tooltip("이 이벤트를 본 뒤로 위조 카드를 쓰지 않는다 (레온)")]
        public bool stopsForgery;
        public List<StoryLine> lines = new();
    }

    // 랜덤 이벤트 (주당 최대 2개)
    public enum RandomEffect { None, Funds, Danger, Reputation, AllAffinity }

    [Serializable]
    public class RandomEvent
    {
        public string title;
        public int minWeek = 1;
        public int maxWeek = 5;
        [Range(0, 1)] public float chance = 0.25f;
        public RandomEffect effect;
        public int amount;
        public List<StoryLine> lines = new();
    }

    // 편의 시설 (밤 상점)
    // BoardExpansion은 게시판 칸 제한을 없애면서 쓰지 않음 (저장된 값 번호가 밀리지 않게 남겨 둠)
    public enum FacilityKind { Magnifier, Scale, BoardExpansion, Lounge, Flyer }

    [Serializable]
    public class Facility
    {
        public FacilityKind kind;
        public string name;
        [TextArea(2, 4)] public string description;
        public int price = 100;
        [Tooltip("이 주차부터 상점에 나온다")]
        public int unlockWeek = 1;
        [Tooltip("여러 번 살 수 있는 최대 횟수 (1 = 한 번만)")]
        public int maxCount = 1;
        [Tooltip("매주 다시 살 수 있다 (소모품)")]
        public bool weekly;
    }

    // 엔딩
    public enum EndingKind { Good, Bitter, Bad }

    [Serializable]
    public class EndingData
    {
        public string id;
        public string title;
        public EndingKind kind;
        [Tooltip("배드 엔딩: 몇 주차 습격에서 무너졌을 때")]
        public int week;
        public List<StoryLine> lines = new();
    }

    // n일째에 해금되는 규칙
    [Serializable]
    public class DayUnlock
    {
        [Tooltip("그 주의 몇째 날부터 (1부터)")]
        public int day = 1;
        public Unlock unlock;
    }

    // n일째 밤에 반드시 나오는 의뢰서 (나머지는 자동 생성)
    [Serializable]
    public class DayQuests
    {
        public int day = 1;
        public List<QuestData> quests = new();
    }

    // n일째 밤에 재생되는 스토리. id가 있으면 코드가 분기에 쓴다 (예: 원정)
    [Serializable]
    public class DayStory
    {
        public int day = 1;
        public string id;
        public string title;
        public List<StoryLine> lines = new();
    }

    // 주말 습격
    [Serializable]
    public class RaidScript
    {
        public string title;
        [Tooltip("습격 규모 = 마을 위험도 + 이 값")]
        public int baseAttack = 20;
        public List<StoryLine> intro = new();
        [Tooltip("{defenders} = 달려와 준 네임드, {hero} = 가장 호감도 높은 네임드")]
        public List<StoryLine> perfect = new();
        public List<StoryLine> close = new();
        public List<StoryLine> fail = new();
    }

    // 수배서 한 장 (수배 주간)
    [Serializable]
    public class WantedPoster
    {
        public string name;
        [Tooltip("다른 이름. 카드에 이 이름을 쓰고 오기도 한다")]
        public string alias;
        [TextArea(2, 4)] public string features;
        public int bounty = 60;
        public Rank fakeRank = Rank.Silver;
        public CharacterLook look;
    }

    // 자동 생성 의뢰의 의뢰인 성격
    public enum ClientTone { Accurate, Exaggerate, Understate }

    [Serializable]
    public class ClientPersona
    {
        [Tooltip("예: 마사 부인 (호들갑)")]
        public string label;
        public ClientTone tone;
    }

    // 몬스터 없는 잡무 의뢰 틀
    [Serializable]
    public class ErrandTemplate
    {
        public string title;
        [TextArea(2, 4)] public string description;
        public string proof;
        public int reward = 50;
    }
}
