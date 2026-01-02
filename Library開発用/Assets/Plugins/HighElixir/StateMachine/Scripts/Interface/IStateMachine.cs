using HighElixir.Loggings;
using System;
namespace HighElixir.StateMachines
{

    public interface IStateMachine<TCont> : ILoggable
    {
        /// <summary> コンテキスト </summary>
        TCont Context { get; }
        IStateMachine<TCont> Parent { get; }

        #region === ロギング・エラーハンドリング ===
        /// <summary> エラーハンドラ。外部で例外処理をカスタマイズできる。 </summary>
        IStateMachineErrorHandler ErrorHandler { get; set; }
        #endregion
        bool Awaked { get; }
        bool IsRunning { get; }


        void OnError(Exception exception);
    }
}