using HighElixir.Implements.Observables;
using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine
{
    public sealed partial class StateMachine<TCont, TEvt, TState>
    {
        // 親→子へのイベント転送や出口バブルを設定するオプション
        public sealed class SubMachineOptions<TSubState>
        {
            /// <summary>親のSendを子に「先に」試すか（true）/「最後に」試すか（false）</summary>
            public bool ForwardEventsFirst { get; set; } = true;

            public bool OnExitResetState { get; set; } = true;
            /// <summary>子がこのサブステートにEnterしたら、対応する親イベントをLazySendする</summary>
            public Dictionary<TSubState, TEvt> ExitMap { get; } = new();
        }

        // StateInfoにぶら下げる内部IF
        internal interface ISubHost : IDisposable
        {
            void OnParentEnter();
            void OnParentExit();
            void Update(float dt);
            bool TrySend(TEvt evt);
            bool ForwardFirst { get; }
        }

        // 実体：任意の TSubState を持つ子FSMをホスト
        internal sealed class SubMachineHost<TSubState> : ISubHost
        {
            private readonly StateMachine<TCont, TEvt, TState> _parent;
            private readonly StateMachine<TCont, TEvt, TSubState> _sub;
            private readonly TSubState _initial;
            private readonly Dictionary<TSubState, TEvt> _exitMap;
            private IDisposable _disposable;
            public bool ForwardFirst { get; }
            public bool OnExitResetState { get; set; }
            public SubMachineHost(
                StateMachine<TCont, TEvt, TState> parent,
                StateMachine<TCont, TEvt, TSubState> sub,
                TSubState initial,
                SubMachineOptions<TSubState> options = null)
            {
                _parent = parent;
                _sub = sub;
                _sub.Parent = _parent;
                _initial = initial;

                _exitMap = options == null ? new() : options.ExitMap ?? new();
                ForwardFirst = options == null || options.ForwardEventsFirst;
                OnExitResetState = options == null || options.OnExitResetState;
            }

            public void OnParentEnter()
            {
                if (!_sub.Awaked)
                    _sub.Awake(_initial);
                else _sub.Start();

                var disList = new List<IDisposable>();
                // 子の特定ステートへのEnterを親イベントにバブル
                foreach (var kv in _exitMap)
                {
                    var disp = _sub.OnEnterEvent(kv.Key)?.Subscribe(_ =>
                    {
                        try { _parent.LazySend(kv.Value); }
                        catch (Exception ex) { _parent.OnError(ex); }
                    });
                    if (disp != null) disList.Add(disp);
                }
                _disposable = ObservableExt.Join(disList.ToArray());
            }

            public void OnParentExit()
            {
                _sub.Pause(OnExitResetState);
            }

            public void Update(float dt) => _sub.Update(dt);

            public bool TrySend(TEvt evt) => _sub.Send(evt);

            public void Dispose()
            {
                _disposable?.Dispose();
                _sub.Dispose();
            }
        }

        /// <summary>
        /// 親ステートにサブマシンをアタッチ
        /// </summary>
        public void AttachSubMachine<TSubState>(
            TState parentStateId,
            StateMachine<TCont, TEvt, TSubState> subMachine,
            TSubState initialSubState,
            SubMachineOptions<TSubState> options = null)
        {
            if (_disposed) return;
            if (!_states.TryGetValue(parentStateId, out var s))
                s = CreateInfo(parentStateId);

            options ??= new SubMachineOptions<TSubState>();
            s.SubHost = new SubMachineHost<TSubState>(
                this, subMachine, initialSubState, options);
        }
    }
}
