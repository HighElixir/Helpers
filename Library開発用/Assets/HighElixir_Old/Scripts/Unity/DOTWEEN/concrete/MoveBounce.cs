using DG.Tweening;
using System;
using UnityEngine;

namespace HighElixir.Tweenworks
{
    [Serializable]
    public sealed class MoveBounce : TweenUserBase
    {
        [Header("Positions")]
        [SerializeField] private Vector3 _pos1 = Vector3.zero;
        [SerializeField] private Vector3 _pos2 = Vector3.zero;

        [Header("Options")]
        [SerializeField] private float _duration = 1f;
        [SerializeField] private int _time = 0;
        [SerializeField] private bool _isLocal = false;

        protected override Tween GetTween(GameObject go)
        {
            Tween tween;
            if (_isLocal)
            {
                go.transform.localPosition = _pos1;
                tween = go.transform
                    .DOLocalMove(_pos2, _duration)
                    .SetEase(_ease)
                    .SetLoops(_time);
            }
            else
            {
                go.transform.position = _pos1;
                tween = go.transform
                    .DOMove(_pos2, _duration)
                    .SetEase(_ease)
                    .SetLoops(_time);
            }
            return tween;
        }
    }
}
