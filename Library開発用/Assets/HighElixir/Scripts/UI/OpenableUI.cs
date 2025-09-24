using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HighElixir.UI
{
    public class OpenableUI : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private RectTransform _open;
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private bool _hideInStart = true;

        // 開閉時の位置を決定するための変数
        [SerializeField] private Vector3 _hiddenPos = Vector3.positiveInfinity;
        [SerializeField] private Vector3 _targetPos = Vector3.zero;

        // 開閉状態を管理する変数
        private bool _opening = false;
        private Tween _tween;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Switch();
        }



        public void Open()
        {
            _opening = true;
            Move();
        }

        public void Close()
        {
            _opening = false;
            Move();
        }

        public void Switch()
        {
            _opening = !_opening;
            Move();
        }
        private void Move()
        {
            if (_tween != null && _tween.IsActive())
                _tween.Kill();

            // 開閉対象の位置を決定：
            // _opening => true で _hiddenPosに移動
            // false で _targetPosへ
            Vector3 target = _opening ? _targetPos : _hiddenPos;

            _tween = _open.DOLocalMove(target, _duration)
                         .SetEase(Ease.OutCubic)
                         .OnComplete(() =>
                         {
                             if (_opening)
                                 ExecuteEvents.Execute<IOpendEvent>(gameObject, null, (h, d) => h.Opened());
                             else
                                 ExecuteEvents.Execute<IOpendEvent>(gameObject, null, (h, d) => h.Closed());
                         });
        }

        private void Awake()
        {

            if (_hideInStart)
            {
                if (_hiddenPos == Vector3.positiveInfinity)
                    _hiddenPos = _open.localPosition; // 初期位置を隠す位置に設定
                else _open.localPosition = _hiddenPos;// 起動時に隠す
                _opening = false;
            }
        }
    }

    // 開閉完了時に呼び出すイベント用インターフェイス
    public interface IOpendEvent : IEventSystemHandler
    {
        void Opened();
        void Closed();
    }
}
