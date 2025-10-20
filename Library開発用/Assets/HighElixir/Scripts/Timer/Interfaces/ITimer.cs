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
        float NormalizedElapsed { get; }

        IObservable<TimeData> TimeReactive { get; }
        // クラスごとに固定
        CountType CountType { get; }
        /// <summary>
        /// タイマー完了時のイベント。何をもって完了とするかは実装次第。
        /// </summary>
        event Action OnFinished;

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

        void SetTicket(TimerTicket ticket);
    }

    public interface ICountUp : ITimer { }

    public interface ICountDown : ITimer { }

    public interface IPulse : ITimer { }
}