using System;

namespace HighElixir.Timers
{
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// Reset時に戻る時間
        /// </summary>
        float InitialTime { get; set; }
        float Current { get; set; }
        bool IsRunning { get; }
        bool IsFinished { get; }

        // 汎用的なオプション
        float ArgTime { get; }
        IObservable<TimeData> TimeReactive { get; }
        IObservable<int> ReactiveTimerEvent { get; }
        void Start();
        float Stop();

        // OnFinishedが呼ばれる可能性がある
        // また、自動的にStopが呼ばれる
        void Reset();

        // OnFinishedなどのイベントが呼ばれない
        // また、自動的にStopが呼ばれる
        void Initialize();
        void Update(float dt);
    }

    // Tick数を使うことを示す
    public interface ITick
    {

    }

    public interface INormalizeable
    {
        float NormalizedElapsed { get; }
    }
}