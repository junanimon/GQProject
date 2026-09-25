using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 비주얼노벨식 장면: 큰 캐릭터 + 이름표 + 대화창. 화면을 클릭하면 다음 대사.
    // 인트로 · 개인 이벤트 · 주말 습격에 쓴다.
    public class StoryView : MonoBehaviour
    {
        public Image background;
        public CharacterPresenter character;
        public GameObject nameTab;
        public Text nameText;
        public TypewriterText text;
        public Text titleText;
        [Tooltip("화면 전체를 덮는 투명 버튼 (클릭 = 다음)")]
        public Button advanceButton;
        public Button skipButton;

        List<StoryLine> lines;
        Func<string, CharacterLook> lookOf;
        Action onDone;
        int index;

        void Awake()
        {
            advanceButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Finish);
        }

        // lookOf: 화자 이름 → 외형 (null이면 캐릭터 숨김 = 지문)
        public void Play(string title, List<StoryLine> script, Func<string, CharacterLook> lookOf, Action done)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            titleText.text = title;
            lines = script;
            this.lookOf = lookOf;
            onDone = done;
            index = -1;
            character.Hide();
            Advance();
        }

        void Advance()
        {
            if (text.IsPlaying)
            {
                text.Complete();
                return;
            }
            index++;
            if (lines == null || index >= lines.Count)
            {
                Finish();
                return;
            }
            var line = lines[index];
            bool hasSpeaker = !string.IsNullOrEmpty(line.speaker);
            nameTab.SetActive(hasSpeaker);
            nameText.text = line.speaker;
            var look = hasSpeaker ? lookOf?.Invoke(line.speaker) : null;
            if (look != null) character.Show(look);
            text.Play(line.text);
        }

        void Finish()
        {
            var done = onDone;
            onDone = null;
            lines = null;
            gameObject.SetActive(false);
            done?.Invoke();
        }
    }
}
