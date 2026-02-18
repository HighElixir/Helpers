namespace HighElixir.Timers
{
    /// <summary>
    /// タイマー登録時オプション。
    /// </summary>
    public struct TimerRegisterOptions
    {
        public string Name;
        public string Group;
        public string Tag;
        public bool IsTick;
        public bool InitZero;
        public bool AndStart;
        public bool Reversing;

        public static TimerRegisterOptions Default => new();
    }
}
