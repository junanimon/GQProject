using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 길드가 관리하는 지역 한 곳 (Assets/Data/Regions). 길드 등급이 unlockRank에 닿으면 관할에 들어온다.
    // 관할 지역의 몬스터가 자동 생성 의뢰에 나오고, 그 지역 모험가가 길드에 온다. 도감은 지역별 카테고리로 묶인다.
    [CreateAssetMenu(menuName = "GQ/Region", fileName = "Region")]
    public class RegionData : ScriptableObject
    {
        public string displayName;
        [TextArea(2, 5)] public string description;

        [Header("지역 마크 (길드 카드에 찍힌다)")]
        public Sprite mark;
        [Tooltip("임시 그림 색. 실제 마크를 넣으면 흰색으로")]
        public Color markTint = Color.white;

        [Header("관할")]
        [Tooltip("이 길드 등급(1=E … 5=A)이 되면 관할에 들어온다")]
        [Range(1, 5)] public int unlockRank = 1;
        [Tooltip("관할에 들어올 때 새로 등록하는 모험가 수")]
        public int newcomers = 3;
        [TextArea(2, 5)] [Tooltip("관할에 들어올 때 주간 정산에 나오는 한 줄")]
        public string unlockNote;

        [Header("몬스터 (도감 카테고리)")]
        public List<MonsterData> monsters = new();
    }
}
