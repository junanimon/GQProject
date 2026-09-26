using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuildProto
{
    // 한 주 (Assets/Data/Weeks): 분위기 · 임시 규칙 · 해금 · 고정 의뢰 · 스토리 · 주말 습격
    [CreateAssetMenu(menuName = "GQ/Week", fileName = "Week")]
    public class WeekData : ScriptableObject
    {
        public string title;
        [Tooltip("주차 전환 화면의 부제")]
        public string subtitle;

        [Header("규칙")]
        public TempRuleKind tempRule;
        public List<DayUnlock> unlocks = new();

        [Header("의뢰")]
        [Tooltip("밤마다 작성할 의뢰서 수 (고정 의뢰가 모자라면 자동 생성으로 채움)")]
        public int sheetsPerNight = 3;
        [Tooltip("이 주에 몰려 내려오는 몬스터 (관할 지역 몬스터에 더해 자동 생성 의뢰로 나옴)")]
        public List<MonsterData> monsterPool = new();
        [Range(0, 1)] [Tooltip("자동 생성 의뢰 중 몬스터 없는 잡무 비율")]
        public float errandChance = 0.2f;
        public List<DayQuests> fixedQuests = new();

        [Header("스토리")]
        [Tooltip("주 첫날 밤에 재생")]
        public List<StoryLine> intro = new();
        public List<DayStory> dayStories = new();

        [Header("수배 주간")]
        public List<WantedPoster> wanted = new();

        [Header("축제 주")]
        [Tooltip("축제 기간에 카드를 인정해 주는 관할 밖 협력 지역 (지역 이름)")]
        [FormerlySerializedAs("partnerGuilds")] public List<string> partnerRegions = new();
        [Tooltip("있지도 않은 가짜 지역 이름")]
        [FormerlySerializedAs("fakeGuilds")] public List<string> fakeRegions = new();

        [Header("주말")]
        public RaidScript raid = new();
    }
}
