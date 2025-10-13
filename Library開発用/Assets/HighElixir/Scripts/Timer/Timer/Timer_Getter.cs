namespace HighElixir.Timers
{
    // 取得関連
    /// <summary>
    /// KEY 付きクールダウン/タイマー管理。
    /// </summary>
    public sealed partial class Timer
    {

        /// <summary>
        /// タイマーが存在するか。
        /// </summary>
        public bool Contains(TimerTicket ticket) => _timers.ContainsKey(ticket);

        /// <summary>
        /// 終了済みか（登録が無ければ false）。
        /// </summary>
        public bool IsFinished(TimerTicket ticket)
        {
            return _timers.TryGetValue(ticket, out var t) && t.IsFinished;
        }

        /// <summary>
        /// 現在の時間を取得。
        /// </summary>
        public bool TryGetRemaining(TimerTicket ticket, out float remaining)
        {
            if (_timers.TryGetValue(ticket, out var t))
            {
                remaining = t.Current;
                return true;
            }
            remaining = 0f;
            return false;
        }

        /// <summary>
        /// 経過正規化 [0..1] を取得（未登録及びカウントアップなど正規化不可能なタイマーは 1 として返す）。
        /// </summary>
        public bool TryGetNormalizedElapsed(TimerTicket ticket, out float elapsed)
        {
            bool res = _timers.TryGetValue(ticket, out var t);
            elapsed = res ? t.NormalizedElapsed : 1f;
            return res;
        }
    }
}
