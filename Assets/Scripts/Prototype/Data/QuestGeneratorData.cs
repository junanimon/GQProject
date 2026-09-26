using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 자동 생성 의뢰의 재료 (Assets/Data/QuestGenerator)
    [CreateAssetMenu(menuName = "GQ/Quest Generator", fileName = "QuestGenerator")]
    public class QuestGeneratorData : ScriptableObject
    {
        public List<ClientPersona> clients = new();
        public List<string> regions = new();
        public List<ErrandTemplate> errands = new();

        [Header("의뢰인 말투 (과장 · 축소 · 정확)")]
        [Tooltip("{n} = 의뢰인이 말하는 수")]
        public List<string> exaggerateLines = new();
        public List<string> understateLines = new();
        public List<string> accurateLines = new();
        [Tooltip("과장·축소한 의뢰인 뒤에 붙는 진짜 단서. {trace} = 몬스터의 흔적 문장")]
        public List<string> truthHints = new();
        [Tooltip("다른 몬스터로 착각한 의뢰인. {other} = 착각한 몬스터")]
        public List<string> misnameLines = new();
    }
}
