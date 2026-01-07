using HighElixir.StateMachines;
using HighElixir.Core.UnityExtensions;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HighElixir.Unity.StateMachine
{
    public abstract class SerializableMachineBase<TCont> : MonoBehaviour
    {
        [SerializeField] private StateMachineOption<TCont, int, int> _options;
        [SerializeField] private List<SerializableMapping> _stateMapping;
        [SerializeField] private int _rate; // 60fps基準
        private StateMachine<TCont, int, int> _stateMachine;
        private Dictionary<string, int> _hashedEvent = new();
        private CancellationTokenSource _source = new();
        private bool _needToRegenerate = false;

        public StateMachine<TCont, int, int> StateMachine => _stateMachine;

        public void ReceiveEvent(int evt)
        {
            _ = _stateMachine.Send(evt, _source.Token);
        }
        public void ReceiveEvent(string evt)
        {
            if (!_hashedEvent.TryGetValue(evt, out var hashed))
            {
                _hashedEvent[evt] = hashed = Animator.StringToHash(evt);
            }
            _ = _stateMachine.Send(hashed, destroyCancellationToken);
        }

        public void Cancel()
        {
            _source.Cancel();
            _needToRegenerate = true;
        }
        public void AwakeMachine(TCont cont, int state)
        {
            _stateMachine = new StateMachine<TCont, int, int>(cont, _options);
            _ = _stateMachine.Awake(state, _source.Token);
        }

        private void Update()
        {
            if (_stateMachine.IsDisposed || _stateMachine.IsRunning) return;
            if (!Interval.Check(_rate)) return;
            if (_needToRegenerate)
            {
                _source.Dispose();
                _source = new CancellationTokenSource();
                _needToRegenerate = false;
            }
            _ = _stateMachine.Update(Time.deltaTime, _source.Token);
        }

        private void OnDestroy()
        {
            _source.Cancel();
            _source.Dispose();
            _stateMachine?.Dispose();
        }
    }
}