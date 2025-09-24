using System;

namespace HighElixir.Timers.Internal
{
    internal interface ITimer
    {
        /// <summary>
        /// Reset時に戻る時間
        /// </summary>
        float InitialTime { get; set; }
        float Current { get; set; }
        bool IsRunning { get; }
        float NormalizedElapsed { get; }

        /// <summary>
        /// タイマー完了時のイベント。何をもって完了とするかは実装次第。
        /// </summary>
        event Action OnFinished;
        void Initialize(); // 初期化
        void Start();
        void Stop();
        void Reset();
        void Update(float dt);
    }
}