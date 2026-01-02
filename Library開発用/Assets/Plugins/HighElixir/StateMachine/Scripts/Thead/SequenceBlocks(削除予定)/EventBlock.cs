using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachines.Thead.Blocks
{
    // やりたいこと：複数のオペレーターを結んでカスタマイズ可能なイベント送信を実現する
    // Awaiterパターンを利用して、await eventSender; のように使えるようにする
    /// <summary>
    /// </summary>
    public sealed class EventBlock<TCont, TEvt, TState>
    {
        // 所属する StateMachine インスタンス
        private StateMachine<TCont, TEvt, TState> _stateMachine;
        // 連結リストの次/前要素
        private EventBlock<TCont, TEvt, TState> _next;
        private EventBlock<TCont, TEvt, TState> _previous;
        private EventBlock<TCont, TEvt, TState> _root;

        // 実際に実行される非同期オペレーション
        private readonly Func<CancellationToken, Task> _operation;
        // 並列フラグ：true の場合はこのブロックと以前のブロックを並列実行する
        private bool _isParallel = false;

        internal EventBlock<TCont, TEvt, TState> Root
        {
            get
            {
                if (_previous != null)
                {
                    return _previous.Root;
                }
                return _root;
            }
        }
        internal EventBlock<TCont, TEvt, TState> Next => _next;
        internal EventBlock<TCont, TEvt, TState> Previous => _previous;

        public StateMachine<TCont, TEvt, TState> StateMachine
        {
            get
            {
                return _root.StateMachine ?? _previous.StateMachine ?? _stateMachine;
            }
            set
            {
                if (_root != null)
                {
                    _root.StateMachine = value;
                }
                else if (_previous != null)
                {
                    _previous.StateMachine = value;
                }
                else
                {
                    _stateMachine = value;
                }
            }
        }

        public bool IsParallel => _isParallel;

        /// <summary>
        /// このブロックを実行します。
        /// token がキャンセル済みなら早期リターンします。
        /// 並列モードでは、自己とそれ以前のブロックを同時に実行して完了を待ちます。
        /// 直列モードでは先行ブロックを順次実行してから自身を実行します。
        /// 明示的に呼び出すことで2回目以降の実行を待機できます。
        /// </summary>
        public async Task Operate(CancellationToken token = default)
        {
            if (_operateTask != null)
            {
                await _operateTask.ConfigureAwait(false);
                return;
            }
            Result = false;
            if (_isParallel)
            {
                // 並列実行：現在のブロックと以前のブロック群を同時に実行
                var tasks = new System.Collections.Generic.List<Task> { _operation(token) };
                var current = _previous;
                while (current != null)
                {
                    // 前方の操作も追加
                    tasks.Add(current._operation(token));
                    current = current._previous;
                    if (token.IsCancellationRequested)
                        return; // キャンセルが要求されたら終了
                }
                if (token.IsCancellationRequested)
                    return;
                // 全てのタスクが完了するのを待つ
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                // 直列実行：前ブロックを順に実行してから自身を実行
                if (token.IsCancellationRequested)
                    return;
                if (_previous != null)
                    await _previous.Operate(token).ConfigureAwait(false);
                if (token.IsCancellationRequested)
                    return;
                await _operation(token).ConfigureAwait(false);
            }
            // 実行成功フラグ
            Result = true;
            _operateTask = null;
        }

        #region === 検索 ===
        public EventBlock<TCont, TEvt, TState> GetBottom()
            => _next?.GetBottom() ?? this;

        public EventBlock<TCont, TEvt, TState> GetTop()
            => _previous?.GetTop() ?? this;

        #endregion

        #region === 並列実行設定 ===
        /// <summary>
        /// このブロックとそれ以前のブロックを並列で実行するように設定します。
        /// 注意: 呼び出しはチェーン全体（以前のブロック）に伝播します。
        /// </summary>
        public EventBlock<TCont, TEvt, TState> AsParallel()
        {
            _isParallel = true;
            _previous?.AsParallel();
            return this;
        }

        /// <summary>
        /// このブロックとそれ以前のブロックを直列で実行するように設定します。
        /// </summary>
        public EventBlock<TCont, TEvt, TState> AsSeries()
        {
            _isParallel = false;
            _previous?.AsSeries();
            return this;
        }
        #endregion

        #region Attach
        public EventBlock<TCont, TEvt, TState> Attach(EventBlock<TCont, TEvt, TState> root)
        {
            if (_previous != null)
            {
                return _previous.Attach(this);
            }
            else
            {
                root.SetNext(this);
                _root = root;
                return this;
            }
        }

        public EventBlock<TCont, TEvt, TState> Detach()
        {
            if (_previous != null)
            {
                return _previous.Detach();
            }
            else if (_root != null)
            {
                _root.SetNext(null);
                var res = _root;
                StateMachine = _root.StateMachine;
                _root = null;
                return res;
            }
            return this;
        }
        #endregion

        #region === Awaiterパターンの実装 ===
        // 実行結果を保持するフラグ（外部で参照可能）
        public bool Result { get; set; }
        private Task _operateTask;
        private readonly object _sync = new();

        public TaskAwaiter GetAwaiter()
        {
            // 既に実行済み・成功なら即時完了 awaiter を返す
            if (Result) return Task.CompletedTask.GetAwaiter();

            // 実行タスクがあればその awaiter を返す
            var t = _operateTask;
            if (t != null) return t.GetAwaiter();

            // 未実行なら開始してキャッシュする（排他）
            lock (_sync)
            {
                _operateTask ??= Operate();
                t = _operateTask;
            }
            return t.GetAwaiter();
        }
        #endregion

        #region === 内部リンク設定用メソッド ===

        /// <summary>
        /// 後ろのブロックを設定します（双方向リンク）。
        /// </summary>
        /// <param name="next"></param>
        internal void SetNext(EventBlock<TCont, TEvt, TState> next)
        {
            _next = next;
            next._previous = this;
        }

        /// <summary>
        /// 前ブロックを設定します（双方向リンク）。
        /// </summary>
        /// <param name="previous"></param>
        internal void SetPrevious(EventBlock<TCont, TEvt, TState> previous)
        {
            _previous = previous;
            previous._next = this;
        }

        internal EventBlock<TCont, TEvt, TState> AttachBottom(EventBlock<TCont, TEvt, TState> block)
        {
            if (_next != null)
            {
                return _next.AttachBottom(block);
            }
            else
            {
                _next = block;
                return this;
            }
        }

        internal EventBlock<TCont, TEvt, TState> AttachTop(EventBlock<TCont, TEvt, TState> block)
        {
            if (_previous != null)
            {
                return _previous.AttachTop(block);
            }
            else
            {
                _previous = block;
                return this;
            }
        }
        #endregion

        #region === コンストラクタ群 ===
        // ルート作成用コンストラクタ（StateMachine に紐づく最初のブロック）
        internal EventBlock(StateMachine<TCont, TEvt, TState> state, Func<CancellationToken, Task> operation)
        {
            _stateMachine = state;
            _operation = operation;
            Result = false;
        }
        // 子ブロック用コンストラクタ：previous に連結される
        internal EventBlock(EventBlock<TCont, TEvt, TState> block, Func<CancellationToken, Task> operation, bool attachAsRoot = false)
           : this(block.StateMachine, operation)
        {
            if (attachAsRoot)
            {
                Attach(block);
            }
            else
            {
                block.SetNext(this);
                _previous = block;
            }
        }
        #endregion
    }
}