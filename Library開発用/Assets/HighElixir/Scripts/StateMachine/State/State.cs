using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine
{
    /// <summary>
    /// ステートマシン内の単一ステートを表す抽象クラス
    /// <br/>ライフサイクル・イベント購読・タグ・遷移制御などを統合
    /// </summary>
    public abstract class State<TCont> : IDisposable
    {
        private List<string> _tags = new();

        /// <summary>所属するステートマシン</summary>
        public IStateMachine<TCont> Parent { get; internal set; }

        /// <summary>ステートに付与されたタグ一覧</summary>
        public List<string> Tags => _tags ??= new();

        /// <summary>ステートが属するコンテキスト（MonoBehaviourなど）</summary>
        protected TCont Cont => Parent.Context;

        #region ライフサイクル
        /// <summary>
        /// ステートに入る時に呼ばれる
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// ステートがアクティブな間、毎フレーム呼ばれる
        /// </summary>
        public virtual void Update(float deltaTime) { }

        /// <summary>
        /// ステートを抜ける時に呼ばれる
        /// </summary>
        public virtual void Exit() { }
        #endregion


        #region 遷移許可

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
        }
    }

    // 空クラス
    public class Idle<TCont> : State<TCont> { }
}
