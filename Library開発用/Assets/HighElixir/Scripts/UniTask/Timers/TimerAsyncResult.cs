namespace HighElixir.Async.Timers
{
    public enum TimerAsyncResult
    {
        Completed, // 正常に完了した
        Canceled,  // キャンセルされた
        Faulted,    // エラーが発生した
        TimerAlreadyFinished, // タイマーは既に完了していた
        TimerNotFound // タイマーが見つからなかった (削除済みなど)
    }
}