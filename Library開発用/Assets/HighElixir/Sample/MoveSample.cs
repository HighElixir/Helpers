using HighElixir.StateMachines;
using HighElixir.Unity.Loggings;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HighElixir.Sample
{
    [RequireComponent(typeof(PlayerInput), typeof(Rigidbody))]
    public class MoveSample : MonoBehaviour
    {
        public enum Event
        {
            Moved = 0,
            Stop = 1,
        }

        public enum State
        {
            Idle,
            Moved,
        }
        private StateMachine<GameObject, Event, State> _fms;
        private ReactiveProperty<Vector2> _move = new(Vector2.zero);

        private void Awake()
        {
            _fms = new(gameObject, logger: new UnityLogger());
            _fms.RegisterState(State.Idle, new Idle<GameObject>());
            _fms.RegisterState(State.Moved, new Move(_move, GetComponent<Rigidbody>()));
            _fms.RegisterTransition(State.Idle, Event.Moved, State.Moved);
            _fms.RegisterTransition(State.Moved, Event.Stop, State.Idle);
            _fms.Awake(State.Idle);
        }

        private void Update()
        {
            _fms?.Update(Time.deltaTime);
        }
        private void OnMove(InputValue inputValue)
        {
            _move.Value = inputValue.Get<Vector2>();
            if (_move.Value != Vector2.zero)
            {
                _fms.Send(Event.Moved);
            }
            else
            {
                _fms.Send(Event.Stop);
            }
        }
    }

    public sealed class Move : State<GameObject>
    {
        private IDisposable _dis;
        private Vector2 _moveDir;
        private Rigidbody _rigidbody;
        public Move(IObservable<Vector2> observable, Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
            _dis = observable.Subscribe(x =>
            {
                //Debug.Log(x.ToString());
                _moveDir = x;
            });
        }

        public override void Update(float deltaTime)
        {
            if (_moveDir != Vector2.zero)
            {
                var convertion = new Vector3(_moveDir.x, 0, _moveDir.y);
                _rigidbody.AddForce(convertion * 5);
            }
        }

        public override void Dispose()
        {
            _dis?.Dispose();
        }
    }
}