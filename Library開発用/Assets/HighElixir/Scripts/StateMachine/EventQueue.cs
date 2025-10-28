using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine
{
    public enum QueueMode
    {
        UntilSuccesses, // 成功するまでキューを回す
        UntilFailures, // 失敗するまでキューを回す
        DoEverything // すべて処理する
    }
    internal sealed class EventQueue<TCont, TEvt, TState> : IDisposable
        where TState : IEquatable<TState>
    {
        private readonly Queue<TEvt> _queue = new();
        public QueueMode Mode { get; set; }
        public StateMachine<TCont, TEvt, TState> StateMachine { get; private set; }

        public EventQueue(StateMachine<TCont, TEvt, TState> stateMachine, QueueMode mode)
        {
            StateMachine = stateMachine;
            Mode = mode;
        }

        public void Enqueue(TEvt item)
        {
            _queue.Enqueue(item);
        }

        public void Update()
        {
            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                if (item != null)
                {
                    bool result = StateMachine.Send(item);
                    if (Mode == QueueMode.UntilSuccesses && result)
                        return; // 成功したら終了
                    if (Mode == QueueMode.UntilFailures && !result)
                        return; // 失敗したら終了

                }
            }
        }

        public void Dispose()
        {
            _queue.Clear();
        }
    }
}