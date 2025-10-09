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
        [SerializeField]
        private string _name;
        [SerializeField]
        private ProfilerType _type;
        [SerializeField, Min(-1)]
        private int _loop;
        [SerializeReference]
        private List<ITweenUser> _users = new();

        private Sequence _sequence;

        // 全ての再生が完了した後のコールバック
        public event Action OnComplete;

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
        public async UniTask Invoke(Action action, CancellationToken token = default)
        {
            if (_sequence.IsActive()) _sequence.Kill();
            OnComplete += action;
            var e = _users.GetEnumerator();
            while (e.MoveNext())
            {
                var tween = e.Current.Invoke(); // TweenUserBase.Invoke()でTweenを取得
                if (tween == null) continue;

                if (_type == ProfilerType.Parallel)
                    _sequence.Join(tween);
                else
                    _sequence.Append(tween);
            }
            _sequence.SetLoops(_loop, LoopType.Restart);

            UniTask task;
            if (IsInfinityLoop)
                task = _sequence.AsyncWaitForElapsedLoops(1).AsUniTask();
            else
                task = _sequence.AsyncWaitForCompletion().AsUniTask();
            await task;
            OnComplete?.Invoke();
            OnComplete = null;
        }

        public void Pause(bool pause)
        {
            if (pause)
                _sequence.Pause();
            else
                _sequence.Play();
        }

        public void Stop()
        {
            _sequence.Kill();
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
            foreach (var user in _users)
            {
                user.Dispose();
            }
            _users = null;
            OnComplete = null;
        }
    }
}