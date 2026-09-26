using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildProto
{
    // 도장 자국: 크게 찍혔다가 제자리로 (켜질 때)
    public class StampIn : MonoBehaviour
    {
        public float duration = 0.16f;
        public float fromScale = 1.8f;

        void OnEnable()
        {
            AudioHub.Play(Sfx.Stamp);
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            var baseRot = transform.localEulerAngles;
            transform.localEulerAngles = baseRot + new Vector3(0, 0, Random.Range(-6f, 6f));
            var group = GetComponent<CanvasGroup>();
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                transform.localScale = Vector3.one * Mathf.Lerp(fromScale, 1, k * k);
                if (group) group.alpha = k;
                yield return null;
            }
            transform.localScale = Vector3.one;
            if (group) group.alpha = 1;
        }
    }
}
