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
    public interface IEventQueue<TCont, TEvt, TState> : IDisposable
    {
        QueueMode Mode { get; set; }
        void Enqueue(TEvt item);
        void Process();
    }

    public class DefaultEventQueue<TCont, TEvt, TState> : IEventQueue<TCont, TEvt, TState>
    {
        private readonly Queue<TEvt> _queue = new();
        private readonly StateMachine<TCont, TEvt, TState> _stateMachine;
        public QueueMode Mode { get; set; }

        public DefaultEventQueue(StateMachine<TCont, TEvt, TState> sm, QueueMode mode)
        {
            _stateMachine = sm;
            Mode = mode;
        }

        public void Enqueue(TEvt item) => _queue.Enqueue(item);

        public void Process()
        {
            while (_queue.Count > 0)
            {
                var evt = _queue.Dequeue();
                bool result = _stateMachine.Send(evt);
                if (Mode == QueueMode.UntilSuccesses && result) return;
                if (Mode == QueueMode.UntilFailures && !result) return;
            }
        }

        public void Dispose() => _queue.Clear();
    }

}