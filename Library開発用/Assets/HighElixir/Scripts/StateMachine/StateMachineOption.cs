using HighElixir.Loggings;
using System;

namespace HighElixir.StateMachines
{
    [Serializable]
    public sealed class StateMachineOption<TCont, TEvt, TState>
    {
        // イベントキュー
        public QueueMode QueueMode = QueueMode.UntilFailures;
        public IEventQueue<TCont, TEvt, TState> Queue { get; set; }
        // ステート上書きのルール
        // falseの場合、上書きしようとすると例外をスローする
        public bool EnableOverriding  = false;

        /// <summary>
        /// 自己遷移の可否
        /// false の場合、自己遷移は行えない
        /// </summary>
        public bool EnableSelfTransition = false;

        // ロガー(null以外の場合、ログ出力が有効化される)
        public ILogger Logger  = null;
        public LogLevel RequiredLoggerLevel  = LogLevel.Fatal;

    }
}