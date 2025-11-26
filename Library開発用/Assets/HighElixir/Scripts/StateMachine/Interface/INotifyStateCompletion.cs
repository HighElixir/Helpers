using System;

namespace HighElixir.StateMachines
{
    /// <summary>
    /// これを実装すると、StateInfoはStateの責務が完了したことをFMSに通知できるようになる。
    /// </summary>
    public interface INotifyStateCompletion
    {
        IObservable<byte> Completion { get; }
    }
}