using System;

namespace HighElixir.Timers
{
    public interface ITimer : ITimerEvt, IDisposable
    {
        /// <summary>
        /// Reset時に戻る時間
        /// </summary>
        float InitialTime { get; set; }
        float Current { get; set; }
        bool IsRunning { get; }
        bool IsFinished { get; }
        float NormalizedElapsed { get; }

        IObservable<TimeData> TimeReactive { get; }
        // クラスごとに固定
        CountType CountType { get; }

        void Start();
        float Stop();

        // OnFinishedが呼ばれる可能性がある
        // また、自動的にStopが呼ばれる
        void Reset();

        // OnFinishedなどのイベントが呼ばれない
        // また、自動的にStopが呼ばれる
        void Initialize();

        // リセット=>スタートの順に実行
        void Restart();
        void Update(float dt);
    }
}