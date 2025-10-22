using System;

namespace HighElixir.Timers
{
    /// <summary>
    /// タイマーのイベントを表すインターフェース
    /// </summary>
    public interface ITimerEvt
    {
        /// <summary>
        /// タイマー完了時のイベント。何をもって完了とするかは実装次第。
        /// </summary>
        event Action OnFinished;
        event Action OnStarted;
        event Action OnReset;
        event Action OnStopped;
        event Action OnInitialized;
        event Action OnRemoved; // タイマーが削除されたときのイベント
    }
}