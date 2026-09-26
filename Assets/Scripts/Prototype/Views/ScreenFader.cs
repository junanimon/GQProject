using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 화면 전환: 어두워지며 제목 카드("2주차 · 축제 주간" 등) → 화면 교체 → 밝아짐
    public class ScreenFader : MonoBehaviour
    {
        public CanvasGroup group;
        public Text title;
        public Text subtitle;
        public float fadeTime = 0.35f;
        public float holdTime = 0.9f;

        public bool IsPlaying { get; private set; }

        public void Play(string head, string sub, Action swap, bool longHold = false)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Run(head, sub, swap, longHold ? holdTime * 1.8f : holdTime));
        }

        IEnumerator Run(string head, string sub, Action swap, float hold)
        {
            IsPlaying = true;
            AudioHub.Play(Sfx.Whoosh);
            transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            title.text = head;
            subtitle.text = sub;
            yield return Fade(0, 1);
            swap?.Invoke();
            yield return new WaitForSecondsRealtime(string.IsNullOrEmpty(head) ? 0.1f : hold);
            yield return Fade(1, 0);
            group.blocksRaycasts = false;
            IsPlaying = false;
            gameObject.SetActive(false);
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
