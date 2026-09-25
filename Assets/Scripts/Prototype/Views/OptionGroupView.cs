using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 버튼 여러 개 중 하나를 고르는 도장 그룹
    public class OptionGroupView : MonoBehaviour
    {
        public List<Button> buttons = new();
        public Color normalColor = new(0.87f, 0.82f, 0.70f);
        public Color selectedColor = new(0.72f, 0.18f, 0.16f);
        public Color normalTextColor = new(0.18f, 0.14f, 0.10f);
        public Color selectedTextColor = Color.white;

        public int Selected { get; private set; } = -1;
        public event Action<int> Changed;

        void Awake()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                int idx = i;
                buttons[i].onClick.AddListener(() => Select(idx));
            }
            Refresh();
        }

        public void Clear()
        {
            Selected = -1;
            Refresh();
        }

        public void Select(int i)
        {
            Selected = i;
            Refresh();
            Changed?.Invoke(i);
        }

        void Refresh()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                bool on = i == Selected;
                buttons[i].image.color = on ? selectedColor : normalColor;
                var t = buttons[i].GetComponentInChildren<Text>();
                if (t != null) t.color = on ? selectedTextColor : normalTextColor;
            }
        }
    }
}
