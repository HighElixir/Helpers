using System;

namespace HighElixir.Timers.Internal
{
    internal abstract class InternalTimerBase : ITimer
    {
        public float InitialTime { get; set; }
        public float Current { get; set; }
        public bool IsRunning { get; protected set; }
        public abstract float NormalizedElapsed { get; }

        public event Action OnFinished; // null 許容


        public InternalTimerBase(Action onFinished)
        {
            if (onFinished != null) OnFinished += onFinished;
        }

        public virtual void Initialize()
        {
            Stop();
            Current = InitialTime;
        }
        public virtual void Reset()
        {
            Stop();
            Current = InitialTime;
        }

        public virtual void Start()
        {
            IsRunning = true;
        }

        public virtual void Stop()
        {
            IsRunning = false;
        }

        public abstract void Update(float dt);

        protected void EventInvokeSafely()
        {
            try
            {
                OnFinished?.Invoke();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[HighElixir.Timers] Timer OnFinished callback threw an exception: {ex}");
            }
        }
    }
}