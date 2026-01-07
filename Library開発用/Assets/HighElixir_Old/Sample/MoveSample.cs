using HighElixir.StateMachines;
using HighElixir.StateMachines.Extension;
using HighElixir.Unity;
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
            ToMove = 0,
            ToIdle = 1,
        }

        public enum State
        {
            Idle,
            Moved,
        }

        [SerializeField] private string _walk;
        private StateMachine<GameObject, Event, State> _fms;
        private Animator _animator;
        private Rigidbody _rb;

        // InputManage
        private ReactiveProperty<Vector2> _inputMove = new(Vector2.zero);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _fms = new(gameObject, logger: new UnityLogger());
            _fms.RegisterState(State.Idle, new Idle<GameObject>());
            _fms.RegisterState(State.Moved, new Move(_inputMove, _rb));
            _fms.RegisterTransition(State.Idle, Event.ToMove, State.Moved);
            _fms.RegisterTransition(State.Moved, Event.ToIdle, State.Idle);
            _ = _fms.Awake(State.Idle);
        }

        private void Update()
        {
            _fms?.Update(Time.deltaTime);
            if (Interval.Check(25))
            {
                var speed = Mathf.Abs(_rb.linearVelocity.x) + Mathf.Abs(_rb.linearVelocity.z);
                _animator.SetFloat(_walk, speed);
                if (_inputMove.Value != Vector2.zero)
                {
                    _fms.SendToForget(Event.ToMove);
                }
                else
                {
                    _fms.SendToForget(Event.ToIdle);
                }
            }
        }
        private void OnMove(InputValue inputValue)
        {
            Debug.Log("OnMove");
            _inputMove.Value = inputValue.Get<Vector2>();
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
                _rigidbody.AddForce(convertion * 30);
            }
        }

        public override void Dispose()
        {
            _dis?.Dispose();
        }
    }
}