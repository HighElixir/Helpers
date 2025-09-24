using DG.Tweening;
using UnityEngine;

namespace HighElixir.UI
{
    public class MoveBounce : MonoBehaviour
    {
        [SerializeField] private bool _awakeOnStart = true;

        [Header("Mode")]
        [SerializeField] private bool _useRectTransform = true;  // ← 追加

        [Header("Positions")]
        [SerializeField] private Vector2 _pos1 = Vector2.zero;
        [SerializeField] private Vector2 _pos2 = Vector2.zero;

        [Header("Timing")]
        [SerializeField] private float _duration = 1f;

        private Tween _tween;
        private RectTransform _rectTransform;

        private void Awake()
        {
            // コンポーネント取得
            _rectTransform = GetComponent<RectTransform>();

            if (_useRectTransform && _rectTransform == null)
                Debug.LogError("MoveBounce: RectTransform がアタッチされていません！");
            if (_awakeOnStart)
                Invoke();
        }

        public void Invoke()
        {
            if (_tween != null && _tween.IsPlaying())
            {
                Debug.LogError("'MoveBounce' is already running!");
                return;
            }

            if (_useRectTransform)
            {
                // UI 用のアンカーポジションでバウンス
                _rectTransform.anchoredPosition = _pos1;
                _tween = _rectTransform
                    .DOAnchorPos(_pos2, _duration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                // 通常の Transform.localPosition でバウンス
                // Z は維持する
                var z = transform.localPosition.z;
                transform.localPosition = new Vector3(_pos1.x, _pos1.y, z);
                _tween = transform
                    .DOLocalMove(new Vector3(_pos2.x, _pos2.y, z), _duration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnDestroy()
        {
            if (_tween != null)
                _tween.Kill();
        }
    }
}
