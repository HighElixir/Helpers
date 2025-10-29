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

    /// <summary>
    /// ステート登録時の加工ロジックを提供するインターフェース
    /// </summary>
    public interface IStateRegisterProcessor<TCont, TEvt, TState>

    {
        void OnRegisterState(TState id, StateMachine<TCont, TEvt, TState>.StateInfo state);
    }
}
