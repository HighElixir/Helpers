using System;
using System.Collections.Generic;

namespace HighElixir.StateMachines
{
    /// <summary>
    /// ステートマシン内の単一ステートを表す抽象クラス
    /// <br/>ライフサイクル・イベント購読・タグ・遷移制御などを統合
    /// </summary>
    public interface IState<TCont> : IDisposable
    {
        /// <summary>所属するステートマシン</summary>
        IStateMachine<TCont> Parent { get;  }

        /// <summary>ステートに付与されたタグ一覧</summary>
        List<string> Tags { get; }

        /// <summary>ステートが属するコンテキスト（MonoBehaviourなど）</summary>
        protected TCont Cont => Parent.Context;

        void SetParent(IStateMachine<TCont> parent);

        #region 遷移許可

        /// <summary>このステートへの遷移を許可するかどうか</summary>
        bool AllowTrans(EventState state);

        /// <summary>EventQueueからのコマンド処理をブロックするかどうか</summary>
        bool BlockCommandDequeue();

        #endregion

        #region タグ操作
        /// <summary>ステートにタグを追加</summary>
        void AddTag(string tag);

        /// <summary>指定したタグを削除</summary>
        void RemoveTag(string tag);

        /// <summary>指定したタグを保持しているか</summary>
        bool HasTag(string tag);
        #endregion
    }
}
