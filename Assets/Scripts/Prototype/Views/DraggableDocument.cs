using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuildProto
{
    // 책상 위 서류: 누르면 맨 위로 올라오고, 끌어서 옮길 수 있다. 새로 놓일 때는 미끄러져 들어온다.
    public class DraggableDocument : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        [Tooltip("서류가 부모 영역 밖으로 나갈 때 남겨둘 최소 크기(px)")]
        public float keepVisible = 80f;
        [Tooltip("새로 놓일 때 이 만큼 떨어진 곳에서 미끄러져 들어온다")]
        public Vector2 slideFrom = new(-320, 0);
        public float slideTime = 0.28f;

        RectTransform rt;
        Vector2 grabOffset;

        void Awake() => rt = (RectTransform)transform;

        // 펼치거나 새로 놓이면 맨 위로
        void OnEnable() => transform.SetAsLastSibling();

        public void OnPointerDown(PointerEventData e) => rt.SetAsLastSibling();

        public void OnBeginDrag(PointerEventData e)
        {
            StopAllCoroutines();
            if (LocalPoint(e, out var p)) grabOffset = rt.anchoredPosition - p;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!LocalPoint(e, out var p)) return;
            rt.anchoredPosition = Clamp(p + grabOffset);
        }

        // spawn 위치로 미끄러져 들어오며 놓인다
        public void Present(RectTransform spawn)
        {
            if (rt == null) Awake();
            gameObject.SetActive(true);
            rt.SetAsLastSibling();
            rt.position = spawn.position;
            var target = rt.anchoredPosition;
            StopAllCoroutines();
            StartCoroutine(Slide(target + slideFrom, target));
        }

        IEnumerator Slide(Vector2 from, Vector2 to)
        {
            for (float t = 0; t < slideTime; t += Time.deltaTime)
            {
                float k = 1 - Mathf.Pow(1 - t / slideTime, 3);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        bool LocalPoint(PointerEventData e, out Vector2 p) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out p);

        Vector2 Clamp(Vector2 pos)
        {
            var parent = ((RectTransform)rt.parent).rect;
            var size = rt.rect.size;
            float maxX = parent.width / 2 + size.x / 2 - keepVisible;
            float maxY = parent.height / 2 + size.y / 2 - keepVisible;
            return new Vector2(Mathf.Clamp(pos.x, -maxX, maxX), Mathf.Clamp(pos.y, -maxY, maxY));
        }
    }
}
