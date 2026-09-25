using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildProto
{
    // 데이터 에셋 안에 들어가는 작은 묶음들
    // (ScriptableObject는 파일 이름과 클래스 이름이 같아야 해서 각자 파일로 뺐고, 여기엔 일반 클래스만 둔다)

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
}
