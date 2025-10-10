using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using DG.Tweening;
namespace HighElixir.Tweenworks.Internal
{
    [Serializable]
    internal class TweenProfiler : IDisposable
    {
        [SerializeField, Tooltip("このプロファイルの名前")]
        private string _name;
        [SerializeField, Tooltip("このプロファイルの再生種別")]
        private ProfilerType _type;
        [SerializeField, Min(-1), Tooltip("ループ回数（無限ループ(-1)の場合、1回ループで完了扱いになる。(再生自体は続行)）")]
        private int _loop;
        [SerializeField]
        private LoopType _loopType;
        [SerializeReference]
        private List<ITweenUser> _users = new();

        private Sequence _sequence;
        private bool _initflg = false;
        private readonly SemaphoreSlim _gate = new(1, 1);
        // 全ての再生が完了した後のコールバック
        public event Action<CompletionType> OnComplete;

        public string Name { get => _name; set => _name = value; }
        public ProfilerType Type => _type;
        public int Loop
        {
            get
            {
                return _loop;
            }
            set
            {
                if (value < -1) _loop = -1;
                else _loop = value;
            }
        }
        public bool IsInfinityLoop => _loop == -1;
        public bool IsPlaying => _sequence.IsActive() && _sequence.IsPlaying();

        public float Progress
        {
            get
            {
                if (!IsPlaying) return -1;
                return _sequence.ElapsedPercentage(IsInfinityLoop);
            }
        }
        /// <summary>
        /// このプロファイラーを起動する
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async UniTask Invoke(Action<CompletionType> action, CancellationToken token = default)
        {
            await _gate.WaitAsync(token);
            var restart = true;
            if (_sequence.IsActive()) _sequence.Rewind();
            else if (_sequence == null)
            {
                _sequence = DOTween.Sequence();
                _sequence.SetAutoKill(false);
                _sequence.OnKill(() => _initflg = false);
                _initflg = false;
                restart = false;
            }
            OnComplete += action;
            if (!_initflg)
            {
                foreach (var u in _users)
                {
                    var tween = u?.Invoke();
                    if (tween == null) continue;
                    if (_type == ProfilerType.Parallel) _sequence.Join(tween);
                    else _sequence.Append(tween);
                }
                _sequence.SetLoops(_loop, _loopType);
                _initflg = true;
            }
            if (restart) _sequence.Restart();
            else _sequence.Play();

            // 非同期連携
            UniTask task;
            if (IsInfinityLoop)
                task = _sequence.AsyncWaitForElapsedLoops(1).AsUniTask();
            else
                task = _sequence.AsyncWaitForCompletion().AsUniTask();
            try
            {
                await task.AttachExternalCancellation(token);
                token.ThrowIfCancellationRequested();
                OnComplete?.Invoke(IsInfinityLoop ? CompletionType.Looping : CompletionType.Done);

            }
            catch (OperationCanceledException)
            {
                Stop();
                OnComplete?.Invoke(CompletionType.Interrupted);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;
                Debug.LogException(ex);
                _sequence.Kill();
                _initflg = false;
                OnComplete?.Invoke(CompletionType.Errored);
            }
            finally
            {
                OnComplete -= action;
                _gate.Release();
            }
        }

        public void Pause(bool pause)
        {
            if (_sequence.IsActive())
            {
                if (pause)
                    _sequence.Pause();
                else
                    _sequence.Play();
            }
        }

        public void Stop()
        {
            if (_sequence.IsActive())
            {
                _sequence.Pause();
                _sequence.Rewind();
            }
        }
        public void Bind(GameObject go)
        {
            foreach (var user in _users)
            {
                if (user != null)
                    user.Bind(go);
            }
        }

        public void Dispose()
        {
            if (_sequence.IsActive()) _sequence.Kill();
            foreach (var user in _users)
            {
                user.Dispose();
            }
            _users = null;
            OnComplete = null;
        }
    }
}