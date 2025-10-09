using System;

namespace HighElixir.Timers.Internal
{
    internal interface ITimer : IDisposable
    {
        /// <summary>
        /// Reset時に戻る時間
        /// </summary>
        float InitialTime { get; set; }
        float Current { get; set; }
        bool IsRunning { get; }
        bool IsFinished { get; }
        float NormalizedElapsed { get; }

        IObservable<float> ElapsedReactiveProperty { get; }
        // クラスごとに固定
        CountType CountType { get; }
        /// <summary>
        /// タイマー完了時のイベント。何をもって完了とするかは実装次第。
        /// </summary>
        event Action OnFinished;
        void Initialize(); // 初期化
        void Start();
        void Stop();
        void Reset();

        // リセット=>スタートの順に実行
        void Restart();
        void Update(float dt);
    }
}