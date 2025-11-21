using System;

namespace HighElixir.StateMachine
{
    /// <summary>
    /// ステートマシンのエラーハンドリングを差し替えるためのインターフェース
    /// </summary>
    public interface IStateMachineErrorHandler
    {
        void Handle(Exception ex);
    }
}
