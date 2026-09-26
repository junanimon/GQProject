using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildProto
{
    // 마우스를 올리면 살짝 커짐
    public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scale = 1.05f;
        Vector3 baseScale = Vector3.one;

        void Awake() => baseScale = transform.localScale;
        void OnDisable() => transform.localScale = baseScale;
        public void OnPointerEnter(PointerEventData e) => transform.localScale = baseScale * scale;
        public void OnPointerExit(PointerEventData e) => transform.localScale = baseScale;
    }
}
