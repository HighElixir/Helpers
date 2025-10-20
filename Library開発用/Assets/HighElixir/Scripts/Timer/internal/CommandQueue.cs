using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    internal sealed class CommandQueue : IDisposable
    {
        [Flags]
        internal enum LazyCommand
        {
            None = 0,
            Start = 1 << 0,
            Stop = 1 << 1,
            Reset = 1 << 2,
            Init = 1 << 3,
            Restart = Reset | Start,
        }

        private readonly int _capacity;     // キュー最大保持数
        private readonly int _drainLimit;   // 1フレームで処理する最大コマンド数
        private readonly Queue<(TimerTicket key, LazyCommand command)> _commands = new();
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

        internal bool Enqueue(TimerTicket target, LazyCommand command)
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
            var local = new List<(TimerTicket key, LazyCommand command)>(_drainLimit);

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
                // 実行順序: Init → Reset(=完了扱い) → Start → Stop
                if (command.Has(LazyCommand.Init))
                    _parent.Initialize(ticket, false);
                if (command.Has(LazyCommand.Reset))
                    _parent.Reset(ticket, false);
                if (command.Has(LazyCommand.Start))
                    _parent.Start(ticket, false, false);
                if (command.Has(LazyCommand.Stop))
                    _parent.Stop(ticket, false, false);

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

    internal static class CommandExt
    {
        internal static bool Has(this CommandQueue.LazyCommand value, CommandQueue.LazyCommand type)
            => (value & type) != 0;
    }
}
