using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 호감도 하트 5칸 (20당 1칸)
    public class AffinityHearts : MonoBehaviour
    {
        public List<Image> hearts = new();
        public Color filled = new(0.90f, 0.35f, 0.45f);
        public Color empty = new(0.35f, 0.30f, 0.32f, 0.8f);

        public void Set(int affinity)
        {
            int n = Mathf.Clamp(affinity / 20, 0, hearts.Count);
            for (int i = 0; i < hearts.Count; i++) hearts[i].color = i < n ? filled : empty;
        }
    }
}
