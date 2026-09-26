using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 비주얼노벨식 장면: 큰 캐릭터 + 이름표 + 대화창. 화면을 클릭하면 다음 대사. 선택지도 띄울 수 있다.
    // 인트로 · 주차 시작 · 개인 이벤트 · 원정 · 주말 습격 · 엔딩에 쓴다.
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

        [Header("선택지 (최대 버튼 수만큼)")]
        public GameObject choiceBox;
        public List<Button> choiceButtons = new();

        [Header("연출")]
        public UIShake shake;

        List<StoryLine> lines;
        Func<string, CharacterLook> lookOf;
        Action onDone;
        Action<int> onChoice;
        int index;

        void Awake()
        {
            advanceButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Finish);
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                int idx = i;
                choiceButtons[i].onClick.AddListener(() => Choose(idx));
            }
        }

        // lookOf: 화자 이름 → 외형 (null이면 캐릭터 그대로 = 지문이나 '나')
        public void Play(string title, List<StoryLine> script, Func<string, CharacterLook> lookOf, Action done)
        {
            Open(title);
            lines = script;
            this.lookOf = lookOf;
            onDone = done;
            index = -1;
            character.Hide();
            Advance();
        }

        // 선택지: prompt를 대화창에 띄우고 버튼으로 고르게 한다
        public void Ask(string title, string speaker, CharacterLook look, string prompt, IList<string> options, Action<int> chosen)
        {
            Open(title);
            lines = null;
            onChoice = chosen;
            if (look != null) character.Show(look);
            nameTab.SetActive(!string.IsNullOrEmpty(speaker));
            nameText.text = speaker;
            text.Play(prompt);
            text.Complete();
            advanceButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            choiceBox.SetActive(true);
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                bool on = i < options.Count;
                choiceButtons[i].gameObject.SetActive(on);
                if (on) choiceButtons[i].GetComponentInChildren<Text>().text = options[i];
            }
        }

        public void Shake()
        {
            shake?.Shake(14f, 0.35f);
            AudioHub.Play(Sfx.Thud);
        }

        void Open(string title)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            titleText.text = title;
            choiceBox.SetActive(false);
            advanceButton.gameObject.SetActive(true);
            skipButton.gameObject.SetActive(true);
        }

        void Choose(int i)
        {
            var cb = onChoice;
            onChoice = null;
            choiceBox.SetActive(false);
            gameObject.SetActive(false);
            cb?.Invoke(i);
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
            if (line.text.Contains("!!") || line.text.Contains("무너졌다")) Shake();
            text.Play(line.text);
        }

        void Finish()
        {
            if (onChoice != null) return;
            var done = onDone;
            onDone = null;
            lines = null;
            gameObject.SetActive(false);
            done?.Invoke();
        }
    }
}
