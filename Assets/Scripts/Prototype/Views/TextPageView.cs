using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 인트로 / 밤 결과 / 게시판 정리 / 결산에 쓰는 종이 한 장 화면
    public class TextPageView : MonoBehaviour
    {
        public Text headingText;
        public Text bodyText;
        public ScrollRect scroll;
        public Button button;
        public Text buttonLabel;

        Action onClick;

        void Awake() => button.onClick.AddListener(() => onClick?.Invoke());

        public void Show(string heading, string body, string label, Action action)
        {
            gameObject.SetActive(true);
            headingText.text = heading;
            bodyText.text = body;
            buttonLabel.text = label;
            onClick = action;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
        }
    }
}
