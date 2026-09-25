using System;
using UnityEngine;

namespace GuildProto
{
    // 밸런스 값 (GuildGame 인스펙터에서 편집)
    [Serializable]
    public class GameConfig
    {
        [Header("진행")]
        [Tooltip("위조 검사가 하루에 하나씩 해금되므로 5일이면 전부 열린다")]
        public int lastDay = 5;
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

        [Header("주말 습격")]
        [Tooltip("습격 규모 = 마을 위험도 + 이 값")]
        public int raidBaseAttack = 20;
        [Tooltip("이 호감도 이상인 네임드는 습격 때 달려와 준다")]
        public int raidHelperAffinity = 70;
        public int raidHelperBonus = 2;
        [Range(0, 1)] [Tooltip("방어력이 습격 규모의 이 비율 이상이면 '아슬아슬'")]
        public float raidCloseRatio = 0.7f;
        public int raidPerfectReputation = 5;
        public int raidFailReputation = -10;
        [Tooltip("실패 시 길드장이 나서며 드는 비용")]
        public int raidFailCost = 50;

        [Header("방문객")]
        [Range(0, 1)] public float violationChance = 0.35f;
        [Range(0, 1)] public float forgeryChance = 0.3f;
        [Range(0, 1)] public float lieChanceOnFail = 0.4f;
    }
}
