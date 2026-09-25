using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 도감 목차 한 줄 (프리팹: Assets/Prefabs/Prototype/BestiaryIndexEntry)
    public class BestiaryIndexEntry : MonoBehaviour
    {
        public Button button;
        public Text label;
        public Image background;
        public Color normal = new(0.87f, 0.82f, 0.70f);
        public Color current = new(0.72f, 0.18f, 0.16f);

        public void Bind(string name, Action onClick)
        {
            label.text = name;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        public void SetCurrent(bool on)
        {
            background.color = on ? current : normal;
            label.color = on ? Color.white : new Color(0.18f, 0.14f, 0.10f);
        }
    }
}
