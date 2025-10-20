using System;

namespace HighElixir.Timers
{
    [Flags]
    public enum CountType
    {
        Invalid = 0,
        Tick = 1 << 1, // 更新回数でカウント
        CountDown = 1 << 2, // カウントダウン式
        CountUp = 1 << 3, // カウントアップ式
        Pulse = 1 << 4, // パルス
        UpAndDown = 1 << 5, // カウントアップ＆ダウン式
    }

    public static class CountExtention
    {
        public static bool Has(this CountType value, CountType type)
        {
            return (value & type) != 0;
        }
    }
}