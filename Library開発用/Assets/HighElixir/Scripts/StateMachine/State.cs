using HighElixir.Implements.Observables;
using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine
{
    public sealed partial class StateMachine<TCont, TEvt, TState>
        where TState : IEquatable<TState>
    {
        /// <summary>
        /// ステートマシン内の単一ステートを表す抽象クラス
        /// <br/>ライフサイクル・イベント購読・タグ・遷移制御などを統合
        /// </summary>
        public abstract class State : IDisposable
        {
            /// <summary>遷移イベントマップ</summary>
            internal Dictionary<TEvt, TState> _transitionMap = new();

            private readonly List<string> _tags = new();
            private ReactiveProperty<TState> _onEnter = new();
            private ReactiveProperty<TState> _onExit = new();

            /// <summary>所属するステートマシン</summary>
            public StateMachine<TCont, TEvt, TState> Parent { get; internal set; }

            /// <summary>ステートに付与されたタグ一覧</summary>
            public List<string> Tags => _tags;

            /// <summary>ステートが属するコンテキスト（MonoBehaviourなど）</summary>
            protected TCont Cont => Parent._cont;

            #region ライフサイクル
            /// <summary>
            /// ステートに入る時に呼ばれる
            /// </summary>
            public virtual void Enter(TState prev) { }

            /// <summary>
            /// ステートがアクティブな間、毎フレーム呼ばれる
            /// </summary>
            public virtual void Update(float deltaTime) { }

            /// <summary>
            /// ステートを抜ける時に呼ばれる
            /// </summary>
            public virtual void Exit(TState next) { }
            #endregion

            #region イベント
            /// <summary>Enter発火時の通知（Reactive）</summary>
            public IObservable<TState> OnEnter => _onEnter;

            /// <summary>Exit発火時の通知（Reactive）</summary>
            public IObservable<TState> OnExit => _onExit;

            /// <summary>
            /// Enterの前に呼ばれる
            /// </summary>
            /// <param name="prev">前のステート</param>
            internal void InvokeEnterAction(TState prev)
            {
                try
                {
                    _onEnter.Value = prev;
                }
                catch (Exception ex)
                {
                    Parent.OnError(ex);
                }
            }

            /// <summary>
            /// Exitの後に呼ばれる
            /// </summary>
            /// <param name="next">次のステート</param>
            internal void InvokeExitAction(TState next)
            {
                try
                {
                    _onExit.Value = next;
                }
                catch (Exception ex)
                {
                    Parent.OnError(ex);
                }
            }
            #endregion

            #region 遷移許可

            internal Func<TState, bool> allowEnterFunc;
            internal Func<TState, bool> allowExitFunc;
            internal Func<bool> blockCommandDequeueFunc;

            /// <summary>
            /// ステート自身の<see cref="AllowEnter"/>よりも先に呼ばれる
            /// <br/>TState => Preview State
            /// </summary>
            public event Func<TState, bool> AllowEnterFunc
            {
                add => allowEnterFunc += value;
                remove => allowEnterFunc -= value;
            }

            /// <summary>
            /// ステート自身の<see cref="AllowExit"/>よりも先に呼ばれる
            /// <br/>TState => Next State
            /// </summary>
            public event Func<TState, bool> AllowExitFunc
            {
                add => allowExitFunc += value;
                remove => allowExitFunc -= value;
            }

            /// <summary>
            /// EventQueueによる遅延遷移処理の実行をブロックするかどうか。
            /// <br/><see cref="BlockCommandDequeue"/>よりも先に呼ばれる
            /// </summary>
            public event Func<bool> BlockCommandDequeueFunc
            {
                add => blockCommandDequeueFunc += value;
                remove => blockCommandDequeueFunc -= value;
            }

            /// <summary>このステートへの遷移を許可するかどうか</summary>
            public virtual bool AllowEnter() { return true; }

            /// <summary>このステートからの遷移を許可するかどうか</summary>
            public virtual bool AllowExit() { return true; }

            /// <summary>EventQueueからのコマンド処理をブロックするかどうか</summary>
            public virtual bool BlockCommandDequeue() { return false; }

            #endregion

            #region タグ操作
            /// <summary>ステートにタグを追加</summary>
            public void AddTag(string tag) => _tags.Add(tag);

            /// <summary>指定したタグを削除</summary>
            public void RemoveTag(string tag) => _tags.Remove(tag);

            /// <summary>指定したタグを保持しているか</summary>
            public bool HasTag(string tag) => _tags.Contains(tag);
            #endregion

            /// <summary>
            /// リソース解放処理
            /// </summary>
            public virtual void Dispose()
            {
                _onEnter?.Dispose();
                _onExit?.Dispose();
                allowEnterFunc = null;
                allowExitFunc = null;
                blockCommandDequeueFunc = null;
            }
        }
    }
}
