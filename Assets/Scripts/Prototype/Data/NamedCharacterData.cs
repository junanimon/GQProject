using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
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
}
