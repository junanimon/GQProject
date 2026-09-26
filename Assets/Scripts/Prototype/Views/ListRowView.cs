using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 목록 한 줄 (프리팹: Assets/Prefabs/Prototype/ListRow)
    // 길드 홀 · 게시판 구성 · 상점 · 엔딩 도감에서 함께 쓴다.
    public class ListRowView : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Text titleText;
        public Text detailText;
        [Tooltip("오른쪽 끝 작은 글 (가격 · 상태)")]
        public Text sideText;
        [Tooltip("초상화 (없으면 숨김)")]
        public Image portrait;
        public Color normal = new(0.95f, 0.92f, 0.82f);
        public Color selected = new(0.98f, 0.84f, 0.55f);
        public Color disabled = new(0.72f, 0.68f, 0.60f);

        public void Bind(string title, string detail, string side, Action onClick, CharacterLook look = null)
        {
            titleText.text = title;
            detailText.text = detail;
            if (sideText != null) sideText.text = side ?? "";
            if (portrait != null)
            {
                bool has = look != null && look.portrait != null;
                portrait.gameObject.SetActive(look != null);
                if (look != null)
                {
                    if (has) portrait.sprite = look.portrait;
                    portrait.color = look.tint;
                }
            }
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
            SetState(false, onClick != null);
        }

        public void SetState(bool isSelected, bool enabled = true)
        {
            button.interactable = enabled;
            background.color = !enabled ? disabled : isSelected ? selected : normal;
        }
    }
}
