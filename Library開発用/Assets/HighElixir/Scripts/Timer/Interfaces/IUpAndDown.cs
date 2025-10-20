namespace HighElixir.Timers
{
    public interface IUpAndDown : ITimer
    {
        bool IsReversing { get; }
        void ReverseDirection();
        void SetDirection(bool isUp);
    }
}