using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    internal sealed class CommandQueue : IDisposable
    {

        private readonly int _capacity;     // キュー最大保持数
        private readonly int _drainLimit;   // 1フレームで処理する最大コマンド数
        private readonly Queue<(TimerTicket key, TimeOperation command)> _commands = new();
        private readonly Timer _parent;
        private readonly object _lock = new();

        public int CommandCount { get; private set; } // 今フレームで処理した数
        public int PendingCount
        {
            get { lock (_lock) return _commands.Count; }
        }

        public CommandQueue(int capacity, Timer parent, int? drainLimit = null)
        {
            _capacity = capacity;
            _drainLimit = drainLimit ?? capacity; // 既存互換
            _parent = parent;
        }

        internal bool Enqueue(TimerTicket target, TimeOperation command)
        {
            lock (_lock)
            {
                if (_commands.Count >= _capacity)
                    return false; // 満杯 → 失敗

                _commands.Enqueue((target, command));
                return true; // 成功
            }
        }

        internal void Update()
        {
            // ロック内で今フレーム分だけ取り出してから、ロック外で実行
            var local = new List<(TimerTicket key, TimeOperation command)>(_drainLimit);

            lock (_lock)
            {
                int n = Math.Min(_drainLimit, _commands.Count);
                for (int i = 0; i < n; i++)
                {
                    local.Add(_commands.Dequeue());
                }
            }

            int processed = 0;
            foreach (var (ticket, command) in local)
            {
                _parent.Send(ticket, command);

                processed++;
                if (processed >= _drainLimit) break;
            }

            CommandCount = processed;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _commands.Clear();
            }
        }
    }
}
