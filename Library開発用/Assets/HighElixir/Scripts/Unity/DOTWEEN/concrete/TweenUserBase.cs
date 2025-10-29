using DG.Tweening;
using System;
using UnityEngine;

namespace HighElixir.Tweenworks
{
    [Serializable]
    public abstract class TweenUserBase : ITweenUser
    {
        [Header("Ease")]
        [SerializeField] protected Ease _ease = Ease.Linear;


        protected Tween _tween;
        private GameObject _target;

        public Tween Invoke()
        {
            if (_target == null)
            {
                Debug.Log($"[{GetType().Name}] : Target is null!");
                return null;
            }

            if (_tween == null)
                _tween = GetTween(_target);
            else
            {
                _tween.Restart();
            }
            return _tween;
        }

        public void Bind(GameObject target)
        {
            _target = target;
            if (_tween.IsPlaying()) _tween.Kill();
            _tween = GetTween(_target);
        }

        public void Dispose()
        {
            _tween.Kill();
            _tween = null;
        }
        protected abstract Tween GetTween(GameObject go);
    }
}