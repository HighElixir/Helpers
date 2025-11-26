using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HighElixir.StateMachines
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
        bool Enqueue(TEvt item, bool skipIfExisting);
        Task Process();
    }

    public class DefaultEventQueue<TCont, TEvt, TState> : IEventQueue<TCont, TEvt, TState>
    {
        private readonly Queue<TEvt> _queue = new();
        private readonly Queue<TEvt> _next = new();
        private readonly StateMachine<TCont, TEvt, TState> _stateMachine;
        public QueueMode Mode { get; set; }

        public DefaultEventQueue(StateMachine<TCont, TEvt, TState> sm, QueueMode mode)
        {
            _stateMachine = sm;
            Mode = mode;
        }

        public bool Enqueue(TEvt item, bool skipIfExisting)
        {
            if (!skipIfExisting || (!_next.Contains(item) && !_queue.Contains(item)))
            {
                _next.Enqueue(item);
                return true;
            }
            return false;
        }

        public async Task Process()
        {
            if (_queue.Count > 0)
            {
                int success = 0;
                int failed = 0;
                while (_queue.Count > 0)
                {
                    var evt = _queue.Dequeue();
                    bool result = await _stateMachine.Send(evt);
                    if (result) success++;
                    else failed++;
                    if (Mode == QueueMode.UntilSuccesses && result) break;
                    if (Mode == QueueMode.UntilFailures && !result) break;
                }
                _stateMachine.Logger?.Info($"[{_stateMachine.ToString()}] Execute:{success + failed}, Success:{success}, Fail:{failed}");
            }
            while (_next.Count > 0)
            {
                _queue.Enqueue(_next.Dequeue());
            }
        }

        public void Dispose() => _queue.Clear();
    }

}