using System;

namespace HighElixir.StateMachine
{
    // TState TEvtに依存しないステート情報
    public interface IStateInfo<TCont> : IObservable<EventState>
    {
        State<TCont> State { get; }
        bool Binded { get; } // ステートクラスが紐づけ済みかどうか

        #region 遷移許可
        /// <summary>
        /// ステート自身の<see cref="State{TCont}.AllowTrans(EventState)"/>よりも先に呼ばれる
        /// </summary>
        public event Func<EventState, bool> AllowTrans;

        /// <summary>
        /// ステート自身の<see cref="State{TCont}.BlockCommandDequeue(EventState)"/>よりも先に呼ばれる
        /// <br/> コマンドキューによる遅延遷移処理の実行をブロックするかどうか
        /// </summary>
        event Func<bool> BlockCommandDequeueFunc;
        #endregion
    }
}