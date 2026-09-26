using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildProto
{
    // 흔들기 (도장 쿵 · 습격)
    public class UIShake : MonoBehaviour
    {
        Vector3 home;
        Coroutine running;

        void Awake() => home = transform.localPosition;

        public void Shake(float amount = 8f, float duration = 0.15f)
        {
            if (!isActiveAndEnabled) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Play(amount, duration));
        }

        IEnumerator Play(float amount, float duration)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                transform.localPosition = home + (Vector3)(Random.insideUnitCircle * amount * (1 - t / duration));
                yield return null;
            }
            transform.localPosition = home;
        }
    }
}
