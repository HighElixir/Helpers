using HighElixir.StateMachines;
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
            _ = _stateMachine.Send(evt, destroyCancellationToken);
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
            _ = _stateMachine.Awake(state, destroyCancellationToken);
        }

        private void Update()
        {
            if (!Interval.Check(_rate)) return;
            if (_needToRegenerate)
            {
                _source.Dispose();
                _source = new CancellationTokenSource();
                _needToRegenerate = false;
            }
        }

        private void OnDestroy()
        {
            _stateMachine?.Dispose();
        }
    }
}