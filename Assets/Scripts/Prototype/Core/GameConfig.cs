using System;
using UnityEngine;

namespace GuildProto
{
    // 밸런스 값 (GuildGame 인스펙터에서 편집)
    [Serializable]
    public class GameConfig
    {
        [Header("진행")]
        [Tooltip("한 주의 평일 수 (평일이 끝나면 주말 습격). 주 수는 GameDatabase.weeks 개수")]
        public int daysPerWeek = 4;
        [Tooltip("낮 영업 시간 (실제 초)")]
        public float dayLength = 150f;
        [Tooltip("모험가 반응 후 다음 방문객까지 대기 (초)")]
        public float reactionDelay = 1.2f;

        [Header("경제 / 평판")]
        public int startFunds = 100;
        public int startDanger = 20;
        public int startReputation = 50;
        [Tooltip("완료 1건당 길드 수수료 (고정)")]
        public int feePerQuest = 15;
        public int dailyDangerRise = 5;
        public int expiredDangerRise = 5;

        [Header("평판 변화")]
        public int repSuccess = 1;
        public int repGreat = 2;
        public int repFail = -2;
        public int repWrongReject = -2;
        public int repExpired = -3;
        public int repViolation = -3;
        public int repPenalty = -5;
        public int repMissedReturn = -1;

        [Header("사망 판정 (해금 후)")]
        [Range(0, 1)] [Tooltip("실패 · 미스매치 2개일 때 사망 확률")]
        public float deathChance = 0.2f;
        [Range(0, 1)] [Tooltip("실패 · 미스매치 3개 이상일 때 사망 확률")]
        public float deathChanceSevere = 0.6f;
        public int repDeath = -5;

        [Header("수배 · 전염병")]
        public int repWantedApproved = -8;
        public int repWrongReport = -3;
        public int repSickApproved = -3;

        [Header("주말 습격 (습격 규모 = 위험도 + 주차별 기본값, WeekData.raid)")]
        [Tooltip("이 호감도 이상인 네임드는 습격 때 달려와 준다")]
        public int raidHelperAffinity = 70;
        public int raidHelperBonus = 2;
        [Range(0, 1)] [Tooltip("방어력이 습격 규모의 이 비율 이상이면 '아슬아슬'")]
        public float raidCloseRatio = 0.7f;
        public int raidPerfectReputation = 5;
        public int raidFailReputation = -10;
        [Tooltip("실패 시 길드장이 나서며 드는 비용")]
        public int raidFailCost = 50;
        [Tooltip("이름 없는 모험가의 습격 참가율 = 기본 + 평판/200")]
        [Range(0, 1)] public float raidBaseParticipation = 0.5f;
        [Tooltip("바르톨이 죽은 뒤 각성한 네임드의 습격 보너스")]
        public int awakenedBonus = 3;
        [Tooltip("마지막 주: 니아가 봉인의 열쇠로 나설 때 방어력 보너스")]
        public int sealKeyBonus = 25;

        [Header("3주차 원정")]
        [Tooltip("바르톨이 3주차 전에 이 횟수보다 많이 수주하면 지쳐서 위험")]
        public int expeditionFatigueLimit = 3;

        [Header("밤 업무")]
        [Tooltip("밤마다 선배가 검토해서 게시판에 붙이는 의뢰 수 (n번째 = n주차). 기입은 선배가 정답대로. 모자라면 마지막 값")]
        public int[] seniorQuestsByWeek = { 1, 2, 3, 3, 4 };
        [Tooltip("하룻밤 승급 심사 신청자 최대 수")]
        public int promotionApplicants = 2;
        [Tooltip("한 주에 일어날 수 있는 랜덤 이벤트 최대 수")]
        public int randomEventsPerWeek = 2;

        [Header("창구 확장")]
        [Tooltip("완료 승인 때 '보너스 지급'으로 길드가 얹어 주는 돈")]
        public int successBonusGold = 10;
        public int successBonusAffinity = 6;
        [Tooltip("길드 홀에서 지명 한 번에 쓰는 영업 시간 (초)")]
        public float nominationSeconds = 12f;
        [Tooltip("하루에 지명할 수 있는 횟수")]
        public int nominationsPerDay = 2;
        [Range(0, 1)] [Tooltip("네임드 한 명이 그날 길드 홀에 나와 있을 확률")]
        public float hallNamedChance = 0.35f;
        [Tooltip("그날 길드 홀에 나와 있는 이름 없는 모험가 수")]
        public int hallNamelessCount = 4;
        [Range(0, 1)] [Tooltip("2주차부터 하루에 긴급 의뢰가 들어올 확률")]
        public float urgentChance = 0.4f;
        [Range(0, 1)] [Tooltip("긴급 의뢰가 들어오는 시각 (영업 시간 비율)")]
        public float urgentAt = 0.35f;
        public float urgentRewardMultiplier = 1.5f;
        [Tooltip("긴급 의뢰를 아무에게도 못 맡겼을 때")]
        public int urgentMissedDanger = 10;
        public int urgentMissedReputation = -3;

        [Header("편의 시설 효과")]
        public float magnifierSeconds = 20f;
        public int flyerRecruits = 2;
        public int flyerReputation = 2;

        [Header("길드 등급 · 관할 지역")]
        [Tooltip("주간 정산 때 평판이 이 값 이상이면 길드 등급 상승 (E→D, D→C, C→B, B→A). 한 주에 한 단계")]
        public int[] rankUpReputation = { 55, 62, 70, 78 };
        [Range(0, 1)] [Tooltip("관할 밖 지역 모험가가 잘못 찾아올 확률 (수주 신청마다)")]
        public float outsideRegionChance = 0.08f;

        [Header("방문객")]
        [Range(0, 1)] public float violationChance = 0.35f;
        [Range(0, 1)] public float forgeryChance = 0.3f;
        [Range(0, 1)] public float lieChanceOnFail = 0.4f;
    }
}
