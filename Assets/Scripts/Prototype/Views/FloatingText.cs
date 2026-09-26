using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildProto
{
    // 떠오르며 사라지는 글자 (+15G · 현상금 · 평판)
    public class FloatingText : MonoBehaviour
    {
        public Text text;
        public float rise = 90f;
        public float duration = 1.1f;
        public Color good = new(1f, 0.85f, 0.35f);
        public Color bad = new(1f, 0.45f, 0.40f);

        public void Play(string message, bool positive)
        {
            text.text = message;
            text.color = positive ? good : bad;
            AudioHub.Play(message.Contains("G") ? Sfx.Coin : message.Contains("♥") ? Sfx.Heart : positive ? Sfx.Click : Sfx.Fail);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var rt = (RectTransform)transform;
            var start = rt.anchoredPosition;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                rt.anchoredPosition = start + new Vector2(0, rise * (1 - (1 - k) * (1 - k)));
                var c = text.color;
                c.a = k < 0.6f ? 1 : 1 - (k - 0.6f) / 0.4f;
                text.color = c;
                transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : 1);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
