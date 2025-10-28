using System;

namespace HighElixir.Timers
{
    public interface IUpAndDown : ITimer
    {
        bool IsReversing { get; }
        event Action<bool> OnReversed;
        void ReverseDirection();
        void SetDirection(bool isUp);
    }
}