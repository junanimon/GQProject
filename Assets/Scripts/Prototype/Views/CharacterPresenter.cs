using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 크게 서 있는 캐릭터 연출: 등장(옆에서 미끄러져 들어옴) · 숨쉬기 · 기쁨(깡총) · 불만(흔들림)
    // 구조: 이 오브젝트(위치 연출) → body Image(숨쉬기 스케일)
    public class CharacterPresenter : MonoBehaviour
    {
        public Image body;

        [Header("등장")]
        public float enterOffset = -260f;
        public float enterTime = 0.35f;

        [Header("숨쉬기")]
        public float breathScale = 0.012f;
        public float breathSpeed = 1.6f;

        [Header("반응")]
        public float hopHeight = 26f;
        public float shakeWidth = 14f;

        RectTransform rt;
        Vector2 home;
        CharacterLook current;
        Coroutine anim;

        void Awake()
        {
            rt = (RectTransform)transform;
            home = rt.anchoredPosition;
        }

        void Update()
        {
            float s = 1 + Mathf.Sin(Time.time * breathSpeed * Mathf.PI) * breathScale;
            body.rectTransform.localScale = new Vector3(1, s, 1);
        }

        // 다른 사람이 오면 등장 연출
        public void Show(CharacterLook look)
        {
            if (rt == null) Awake();
            gameObject.SetActive(true);
            bool changed = look != current;
            current = look;
            if (look.body != null) body.sprite = look.body;
            body.color = look.tint;
            if (changed) Play(Enter());
        }

        public void Hide()
        {
            current = null;
            gameObject.SetActive(false);
        }

        public void Hop() => Play(Hopping());
        public void Shake() => Play(Shaking());

        void Play(IEnumerator routine)
        {
            if (!isActiveAndEnabled) return;
            if (anim != null) StopCoroutine(anim);
            rt.anchoredPosition = home;
            anim = StartCoroutine(routine);
        }

        IEnumerator Enter()
        {
            var group = GetComponent<CanvasGroup>();
            for (float t = 0; t < enterTime; t += Time.deltaTime)
            {
                float k = 1 - Mathf.Pow(1 - t / enterTime, 3);
                rt.anchoredPosition = home + new Vector2(enterOffset * (1 - k), 0);
                if (group) group.alpha = k;
                yield return null;
            }
            rt.anchoredPosition = home;
            if (group) group.alpha = 1;
        }

        IEnumerator Hopping()
        {
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                rt.anchoredPosition = home + new Vector2(0, Mathf.Sin(t / 0.35f * Mathf.PI) * hopHeight);
                yield return null;
            }
            rt.anchoredPosition = home;
        }

        IEnumerator Shaking()
        {
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                rt.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * shakeWidth * (1 - t / 0.35f), 0);
                yield return null;
            }
            rt.anchoredPosition = home;
        }
    }
}
