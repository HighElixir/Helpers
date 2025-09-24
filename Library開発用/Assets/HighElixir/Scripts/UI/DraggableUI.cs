using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace HighElixir.UI
{
    public class DraggableUI : MonoBehaviour,
        IDragHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        [SerializeField] private float _snapSize = 20f;
        [SerializeField] private bool _clampedInScreen = true;
        private RectTransform _rt;
        private RectTransform _parentRt; // 親のRectTransformを保持する
        private Canvas _canvas;

        // UnityEvents
        public UnityEvent OnBeginDragEvent = new();
        public UnityEvent OnEndDragEvent = new();

        public void OnBeginDrag(PointerEventData e)
            => OnBeginDragEvent.Invoke();

        public void OnDrag(PointerEventData e)
        {
            UpdatePosition(e.position, false);
        }

        public void OnEndDrag(PointerEventData e)
        {
            UpdatePosition(e.position, true);
            OnEndDragEvent.Invoke();
        }

        private void UpdatePosition(Vector2 screenPos, bool snap)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt.parent as RectTransform,
                screenPos,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out localPoint
            );

            if (_clampedInScreen)
            {
                // 親のRectTransformのサイズと、自身のRectTransformのサイズを考慮してクランプ範囲を計算
                Vector2 parentSize = _parentRt.rect.size;
                Vector2 selfSize = _rt.rect.size;

                // アンカーポイントが中央(0.5, 0.5)の場合を想定して計算
                float minX = -parentSize.x / 2f + selfSize.x / 2f;
                float maxX = parentSize.x / 2f - selfSize.x / 2f;
                float minY = -parentSize.y / 2f + selfSize.y / 2f;
                float maxY = parentSize.y / 2f - selfSize.y / 2f;

                localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
                localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
            }

            if (snap)
                localPoint = new Vector2(RoundSnap(localPoint.x), RoundSnap(localPoint.y));
            _rt.anchoredPosition = localPoint;
        }

        private float RoundSnap(float value)
        {
            float half = _snapSize * 0.5f;
            float mod = value % _snapSize;
            float baseVal = value - mod;
            return (mod < half) ? baseVal : baseVal + _snapSize;
        }

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _parentRt = _rt.parent.GetComponent<RectTransform>(); // 親のRectTransformを取得
            _canvas = GetComponentInParent<Canvas>();
        }
    }
}
