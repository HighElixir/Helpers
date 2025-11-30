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

        // Poller token -> Poller
        private readonly Dictionary<int, Poller> _pollers = new();
        private int _nextToken = 1;

        // reusable snapshot to avoid allocations
        private Poller[] _snapshot = Array.Empty<Poller>();

        private Coroutine _pollCoroutine;

        private struct Poller
        {
            public int Token;
            public int ParamHash;
            public Func<int> Condition;
            public int Last;
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
                var p = new Poller
                {
                    Token = token,
                    ParamHash = hash,
                    Condition = condition,
                    HasLast = false,
                    Last = default
                };
                _pollers[token] = p;

                return Disposable.Create(() => { _pollers.Remove(token); });
            }
            else
            {
                Debug.LogError($"[AnimatorPoller] IntPollingTo: Invalid parameter name '{paramaterName}' for Animator on GameObject '{gameObject.name}'.");

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
            _pollers.Clear();
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
            if (_animator == null || _pollers.Count == 0) return;

            // ensure snapshot capacity
            if (_snapshot.Length < _pollers.Count)
            {
                _snapshot = new Poller[_pollers.Count];
            }

            _pollers.Values.CopyTo(_snapshot, 0);

            for (int i = 0; i < _pollers.Count; i++)
            {
                var p = _snapshot[i];
                // condition might throw; keep behavior same as before
                var val = p.Condition();
                if (!p.HasLast || p.Last != val)
                {
                    // Only call SetInteger when value changes
                    if (_animator != null)
                    {
                        _animator.SetInteger(p.ParamHash, val);
                    }
                    // Update stored poller if it still exists
                    if (_pollers.ContainsKey(p.Token))
                    {
                        var updated = p;
                        updated.Last = val;
                        updated.HasLast = true;
                        _pollers[p.Token] = updated;
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