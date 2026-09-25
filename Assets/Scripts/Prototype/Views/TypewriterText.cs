using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildProto
{
    // 대사를 한 글자씩 보여준다. 대화창을 클릭하면 바로 전부 표시.
    public class TypewriterText : MonoBehaviour, IPointerClickHandler
    {
        public Text target;
        public float charsPerSecond = 40f;
        [Tooltip("다 나오면 켜지는 ▼ 표시 (없어도 됨)")]
        public GameObject continueMark;

        string full = "";
        float shown;

        public bool IsPlaying => shown < full.Length;

        public void Play(string text)
        {
            full = text ?? "";
            shown = 0;
            target.text = "";
            if (continueMark) continueMark.SetActive(false);
        }

        public void Complete()
        {
            shown = full.Length;
            target.text = full;
            if (continueMark) continueMark.SetActive(true);
        }

        void Update()
        {
            if (!IsPlaying) return;
            shown += Time.deltaTime * charsPerSecond;
            if (shown >= full.Length) Complete();
            else target.text = full.Substring(0, (int)shown);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (IsPlaying) Complete();
        }
    }
}
