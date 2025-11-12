using TMPro;
using UnityEngine;
using System;
using DG.Tweening;

namespace HighElixir.Unity.UI
{
    public class PopupText_3D : MonoBehaviour
    {
        [SerializeField] private Collider _head;
        [SerializeField] private Vector3 _delta;

        [Header("画面設定")]
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Camera _camera;
        [SerializeField] private Canvas _canvas;

        [Header("Jump")]
        [SerializeField] private Vector2 _jumpDelta = new Vector2(0, 20);
        [SerializeField] private float _jumpPower = 5f;
        [SerializeField] private int _jumpCount = 5;
        [SerializeField] private float _jumpDuration = 1f;
        private Sequence _sequence;
        private Vector3 _lastPos;

        private void Awake()
        {
            if (_head == null)
            {
                if (!TryGetComponent<Collider>(out _head))
                {
                    var c = gameObject.AddComponent<SphereCollider>();
                    _head = c;
                    c.radius = 2f;
                }
            }
            if (_head == null)
                throw new InvalidOperationException();

            _head.isTrigger = true;
            _text.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            var pos = ScreenHelpers.WorldToUILocalPos(transform.position + _delta, _camera, _canvas);
            _text.rectTransform.anchoredPosition = pos;
            _text.gameObject.SetActive(true);
            _sequence = _text.rectTransform.DOJumpAnchorPos(pos + _jumpDelta, _jumpPower, _jumpCount, _jumpDuration).SetLoops(-1).Play();
        }

        private void OnTriggerExit(Collider other)
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }

        private void OnDestroy()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}
