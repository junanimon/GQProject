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

        [Header("자동 생성 의뢰 재료")]
        [Tooltip("의뢰인이 목격한 단서 (의뢰서 상황 설명에 2개쯤 섞임)")]
        [TextArea(1, 3)] public string[] clues;
        [Tooltip("의뢰서 제목 후보")]
        public string[] questTitles;
        [Tooltip("수를 짐작할 흔적 문장. {n} = 실제 수 (예: 발자국이 {n}쌍 찍혀 있었다)")]
        public string countTrace = "흔적을 보니 {n}마리쯤이었다";

        [Header("판정")]
        [Tooltip("수량 1~3 / 4~7 / 8 이상일 때 권장 등급")]
        public Rank[] rankByBand = new Rank[3];
        public Job role;
        public bool isMonster = true;
        [Tooltip("허위 보고 때 대신 내미는 비슷한 몬스터")]
        public MonsterData lookAlike;

        public Rank RankFor(CountBand b) => rankByBand[(int)b];
    }
}
