using System;

namespace HighElixir.StateMachines
{
    /// <summary>
    /// ステートマシンのエラーハンドリングを差し替えるためのインターフェース
    /// </summary>
    public interface IStateMachineErrorHandler
    {
        void Handle(Exception ex);
    }
}
