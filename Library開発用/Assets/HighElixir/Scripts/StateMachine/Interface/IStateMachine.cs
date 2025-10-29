using System;
namespace HighElixir.StateMachine
{
    // Stateから参照するための切り出し
    public interface IStateMachine<TCont>
    {
        TCont Context { get; }

        void OnError(Exception exception);
    }
}