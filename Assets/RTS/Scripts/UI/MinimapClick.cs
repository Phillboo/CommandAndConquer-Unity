using UnityEngine;
using UnityEngine.EventSystems;

namespace RTS
{
    public class MinimapClick : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public float worldHalf = 102f;
        RectTransform rect;

        void Awake() { rect = GetComponent<RectTransform>(); }

        public void OnPointerDown(PointerEventData e) { Jump(e); }
        public void OnDrag(PointerEventData e) { Jump(e); }

        void Jump(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var lp))
                return;
            var r = rect.rect;
            float ux = Mathf.Clamp01((lp.x - r.xMin) / r.width);
            float uy = Mathf.Clamp01((lp.y - r.yMin) / r.height);
            Vector3 world = new Vector3((ux - 0.5f) * 2f * worldHalf, 0f, (uy - 0.5f) * 2f * worldHalf);
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 f = cam.transform.forward;
            float dist = cam.transform.position.y / Mathf.Max(0.1f, -f.y);
            cam.transform.position = world - f * dist;
        }
    }
}
