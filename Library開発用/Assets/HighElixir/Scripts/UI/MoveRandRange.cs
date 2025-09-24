using DG.Tweening;
using UnityEngine;

namespace HighElixir.UI
{
    public class MoveRandRange : MonoBehaviour
    {
        [Header("Play Settings")]
        [SerializeField] private bool _playOnAwake = true;
        [SerializeField] private bool _isRect = true;       // true=RectTransform, false=普通のTransform

        [Header("Motion Settings")]
        [SerializeField] private float _range = 1f;         // 開始点からランダムにこの距離内
        [SerializeField] private float _speed = 1f;         // 単位距離あたりの秒数（移動速度）

        private Tween _tween;
        private RectTransform _rectTransform;
        private Transform _plainTransform;
        private Vector3 _startPos;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _plainTransform = transform;

            // 開始位置キャッシュ
            if (_isRect && _rectTransform != null)
                _startPos = _rectTransform.anchoredPosition;
            else
                _startPos = _plainTransform.localPosition;

            if (_playOnAwake)
                Invoke();
        }

        public void Invoke()
        {
            // 既存Tween kill
            _tween?.Kill();
            TweenToRandom();
        }

        private void TweenToRandom()
        {
            // 次のランダム位置決定
            var target = GetRandomPos();

            // duration = 距離 / 速度
            float dist = Vector3.Distance(CurrentPos(), target);
            float duration = dist / Mathf.Max(_speed, 0.0001f);

            if (_isRect && _rectTransform != null)
            {
                _tween = _rectTransform
                    .DOAnchorPos((Vector2)target, duration)
                    .SetEase(Ease.Linear)
                    .OnComplete(TweenToRandom);
            }
            else
            {
                _tween = _plainTransform
                    .DOLocalMove(target, duration)
                    .SetEase(Ease.Linear)
                    .OnComplete(TweenToRandom);
            }
        }

        // 開始点からランダムに点を返す
        private Vector3 GetRandomPos()
        {
            var angle = Random.value * Mathf.PI * 2f;
            var radius = Random.Range(0f, _range);
            var x = _startPos.x + Mathf.Cos(angle) * radius;
            var y = _startPos.y + Mathf.Sin(angle) * radius;
            return new Vector3(x, y, _startPos.z);
        }

        // 現在のローカル座標を返す
        private Vector3 CurrentPos()
        {
            if (_isRect && _rectTransform != null)
                return _rectTransform.anchoredPosition;
            return _plainTransform.localPosition;
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}
