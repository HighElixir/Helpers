using HighElixir.Implements;
using HighElixir.Unity.Animations;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.Unity.AnimatorExtension
{
    /// <summary>
    /// Animatorのパラメータをポーリングで更新するクラス
    /// </summary>
    public class AnimatorPoller : MonoBehaviour, IDisposable
    {

        [SerializeField] private Animator _animator;
        [SerializeField] private int _pollingIntervalMs = 100;

        // Poller token -> Poller for each type
        private readonly Dictionary<int, IntPoller> _intPollers = new();
        private readonly Dictionary<int, FloatPoller> _floatPollers = new();
        private readonly Dictionary<int, BoolPoller> _boolPollers = new();
        private int _nextToken = 1;

        // reusable snapshots to avoid allocations
        private IntPoller[] _intSnapshot = Array.Empty<IntPoller>();
        private FloatPoller[] _floatSnapshot = Array.Empty<FloatPoller>();
        private BoolPoller[] _boolSnapshot = Array.Empty<BoolPoller>();

        private Coroutine _pollCoroutine;

        private struct IntPoller
        {
            public int Token;
            public int ParamHash;
            public Func<int> Condition;
            public int Last;
            public bool HasLast;
        }
        private struct FloatPoller
        {
            public int Token;
            public int ParamHash;
            public Func<float> Condition;
            public float Last;
            public bool HasLast;
        }
        private struct BoolPoller
        {
            public int Token;
            public int ParamHash;
            public Func<bool> Condition;
            public bool Last;
            public bool HasLast;
        }

        public IDisposable IntPollingTo(string paramaterName, Func<int> condition)
        {
            if (_animator == null)
            {
                Debug.LogError($"[AnimatorPoller] IntPollingTo: Animator is null on GameObject '{gameObject.name}'.");

                return Disposable.Empty;
            }

            var hash = Animator.StringToHash(paramaterName);
            if (AnimationValidator.IsValid(_animator, AnimatorControllerParameterType.Int, hash))
            {
                var token = _nextToken++;
                var p = new IntPoller
                {
                    Token = token,
                    ParamHash = hash,
                    Condition = condition,
                    HasLast = false,
                    Last = default
                };
                _intPollers[token] = p;

                return Disposable.Create(() => { _intPollers.Remove(token); });
            }
            else
            {
                Debug.LogError($"[AnimatorPoller] IntPollingTo: Invalid parameter name '{paramaterName}' for Animator on GameObject '{gameObject.name}'.");

                return Disposable.Empty;
            }
        }

        public IDisposable FloatPollingTo(string paramaterName, Func<float> condition)
        {
            if (_animator == null)
            {
                Debug.LogError($"[AnimatorPoller] FloatPollingTo: Animator is null on GameObject '{gameObject.name}'.");
                return Disposable.Empty;
            }

            var hash = Animator.StringToHash(paramaterName);
            if (AnimationValidator.IsValid(_animator, AnimatorControllerParameterType.Float, hash))
            {
                var token = _nextToken++;
                var p = new FloatPoller
                {
                    Token = token,
                    ParamHash = hash,
                    Condition = condition,
                    HasLast = false,
                    Last = default
                };
                _floatPollers[token] = p;

                return Disposable.Create(() => { _floatPollers.Remove(token); });
            }
            else
            {
                Debug.LogError($"[AnimatorPoller] FloatPollingTo: Invalid parameter name '{paramaterName}' for Animator on GameObject '{gameObject.name}'.");
                return Disposable.Empty;
            }
        }
        public IDisposable BoolPollingTo(string paramaterName, Func<bool> condition)
        {
            if (_animator == null)
            {
                Debug.LogError($"[AnimatorPoller] BoolPollingTo: Animator is null on GameObject '{gameObject.name}'.");
                return Disposable.Empty;
            }

            var hash = Animator.StringToHash(paramaterName);
            if (AnimationValidator.IsValid(_animator, AnimatorControllerParameterType.Bool, hash))
            {
                var token = _nextToken++;
                var p = new BoolPoller
                {
                    Token = token,
                    ParamHash = hash,
                    Condition = condition,
                    HasLast = false,
                    Last = default
                };
                _boolPollers[token] = p;

                return Disposable.Create(() => { _boolPollers.Remove(token); });
            }
            else
            {
                Debug.LogError($"[AnimatorPoller] BoolPollingTo: Invalid parameter name '{paramaterName}' for Animator on GameObject '{gameObject.name}'.");
                return Disposable.Empty;
            }
        }
        public void SetPollingRate(int milliseconds)
        {
            _pollingIntervalMs = milliseconds;
            RestartCoroutine();
        }

        public void Dispose()
        {
            _intPollers.Clear();
            _floatPollers.Clear();
            _boolPollers.Clear();
            StopPolling();
        }

        internal void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        private void RestartCoroutine()
        {
            if (!isActiveAndEnabled) return;
            StopPolling();
            _pollCoroutine = StartCoroutine(PollLoop());
        }

        private void StopPolling()
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }
        }

        private System.Collections.IEnumerator PollLoop()
        {
            var wait = new WaitForSeconds(_pollingIntervalMs / 1000f);
            while (true)
            {
                PollAll();
                yield return wait;
            }
        }

        private void PollAll()
        {
            if (_animator == null) return;

            // Int pollers
            if (_intPollers.Count > 0)
            {
                if (_intSnapshot.Length < _intPollers.Count) _intSnapshot = new IntPoller[_intPollers.Count];
                _intPollers.Values.CopyTo(_intSnapshot, 0);
                for (int i = 0; i < _intPollers.Count; i++)
                {
                    var p = _intSnapshot[i];
                    var val = p.Condition();
                    if (!p.HasLast || p.Last != val)
                    {
                        _animator.SetInteger(p.ParamHash, val);
                        if (_intPollers.ContainsKey(p.Token))
                        {
                            var updated = p;
                            updated.Last = val;
                            updated.HasLast = true;
                            _intPollers[p.Token] = updated;
                        }
                    }
                }
            }

            // Float pollers
            if (_floatPollers.Count > 0)
            {
                if (_floatSnapshot.Length < _floatPollers.Count) _floatSnapshot = new FloatPoller[_floatPollers.Count];
                _floatPollers.Values.CopyTo(_floatSnapshot, 0);
                for (int i = 0; i < _floatPollers.Count; i++)
                {
                    var p = _floatSnapshot[i];
                    var val = p.Condition();
                    if (!p.HasLast || !Mathf.Approximately(p.Last, val))
                    {
                        _animator.SetFloat(p.ParamHash, val);
                        if (_floatPollers.ContainsKey(p.Token))
                        {
                            var updated = p;
                            updated.Last = val;
                            updated.HasLast = true;
                            _floatPollers[p.Token] = updated;
                        }
                    }
                }
            }

            // Bool pollers
            if (_boolPollers.Count > 0)
            {
                if (_boolSnapshot.Length < _boolPollers.Count) _boolSnapshot = new BoolPoller[_boolPollers.Count];
                _boolPollers.Values.CopyTo(_boolSnapshot, 0);
                for (int i = 0; i < _boolPollers.Count; i++)
                {
                    var p = _boolSnapshot[i];
                    var val = p.Condition();
                    if (!p.HasLast || p.Last != val)
                    {
                        _animator.SetBool(p.ParamHash, val);
                        if (_boolPollers.ContainsKey(p.Token))
                        {
                            var updated = p;
                            updated.Last = val;
                            updated.HasLast = true;
                            _boolPollers[p.Token] = updated;
                        }
                    }
                }
            }
        }

        private void Start()
        {
            if (isActiveAndEnabled)
            {
                _pollCoroutine = StartCoroutine(PollLoop());
            }
        }

        private void OnEnable()
        {
            RestartCoroutine();
        }

        private void OnDisable()
        {
            StopPolling();
        }

    }

    public static class AnimatorPollerExt
    {
        public static IDisposable IntPollingTo(this Animator animator, string paramaterName, Func<int> condition)
        {
            if (!animator.TryGetComponent<AnimatorPoller>(out var anim))
            {
                anim = animator.gameObject.AddComponent<AnimatorPoller>();
                anim.SetAnimator(animator);
            }
            return anim.IntPollingTo(paramaterName, condition);
        }
    }
}