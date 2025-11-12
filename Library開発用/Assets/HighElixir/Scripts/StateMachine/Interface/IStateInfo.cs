using System;

namespace HighElixir.StateMachine
{
    // TState TEvtに依存しないステート情報
    public interface IStateInfo<TCont>
    {
        State<TCont> State { get; }
        bool Binded { get; } // ステートが紐づけ済みかどうか
        #region イベント
        /// <summary>Enter発火時の通知（Reactive）</summary>
        public IObservable<IStateInfo<TCont>> OnEnter { get; }

        /// <summary>Exit発火時の通知（Reactive）</summary>
        public IObservable<IStateInfo<TCont>> OnExit { get; }

        #endregion

        #region 遷移許可
        /// <summary>
        /// ステート自身の<see cref="AllowEnter"/>よりも先に呼ばれる
        /// <br/>TState => Preview State
        /// </summary>
        public event Func<IStateInfo<TCont>, bool> AllowEnterFunc;
        /// <summary>
        /// ステート自身の<see cref="AllowExit"/>よりも先に呼ばれる
        /// <br/>TState => Next State
        /// </summary>
        public event Func<IStateInfo<TCont>, bool> AllowExitFunc;

        #endregion
    }
}