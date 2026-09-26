using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuildProto
{
    // 누르면 쿵 하고 눌리는 느낌 (도장 · 버튼)
    public class ButtonPunch : MonoBehaviour, IPointerClickHandler
    {
        public float squash = 0.85f;
        public float duration = 0.18f;

        public void OnPointerClick(PointerEventData e)
        {
            if (!isActiveAndEnabled) return;
            AudioHub.Play(Sfx.Click);
            StartCoroutine(Punch());
        }

        IEnumerator Punch()
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI);
                transform.localScale = Vector3.one * Mathf.Lerp(1, squash, k);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
