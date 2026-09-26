using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildProto
{
    // 켜질 때 살짝 커지며 나타남 (팝업 · 책 · 선택지)
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPopIn : MonoBehaviour
    {
        public float duration = 0.18f;
        public float fromScale = 0.9f;

        void OnEnable() => StartCoroutine(Play());

        IEnumerator Play()
        {
            var group = GetComponent<CanvasGroup>();
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1 - Mathf.Pow(1 - t / duration, 3);
                transform.localScale = Vector3.one * Mathf.Lerp(fromScale, 1, k);
                group.alpha = k;
                yield return null;
            }
            transform.localScale = Vector3.one;
            group.alpha = 1;
        }
    }
}
