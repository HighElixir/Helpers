using Cysharp.Threading.Tasks;
using HighElixir.Timers.Extensions;
using HighElixir.Implements.Observables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HighElixir.Timers.Async
{
    public enum TimerAsyncResult
    {
        Completed, // 正常に完了した
        Canceled,  // キャンセルされた
        Faulted,   // エラーが発生した
        TimerAlreadyFinished, // タイマーは既に完了していた
        TimerNotFound // タイマーが見つからなかった (削除済みなど)
    }

    /// <summary>
    /// 非同期待機サポート
    /// </summary>
    /// <remarks>
    /// Initialize / Reset でキャンセルされる
    /// </remarks>
    public static class TaskWaiter
    {
        // チケットごとの待機状態
        private sealed class AwaitState
        {
            public int Version; // Initialize/Reset で++する
            public readonly List<UniTaskCompletionSource<bool>> FinishWaiters = new();
        }

        // 全体管理
        private static readonly ConcurrentDictionary<TimerTicket, AwaitState> _awaits = new();

        /// <summary>
        /// タイマーが完了するまで待機する
        /// </summary>
        public static UniTask<TimerAsyncResult> WaitUntilFinishedAsync(this HighElixir.Timers.Timer timer, TimerTicket ticket, bool autoStart, bool isLazy = true, CancellationToken ct = default)
        {
            if (autoStart)
                timer.Start(ticket, isLazy);

            return UniTask.Create<TimerAsyncResult>(async () =>
            {
                // 遅延開始対応（Start されるまで待機）
                if (isLazy)
                {
                    await UniTask.WaitUntil(
                        () => timer.IsRunning(ticket) || !timer.Contains(ticket),
                        PlayerLoopTiming.PreUpdate,
                        ct);
                }

                if (!timer.Contains(ticket))
                    return TimerAsyncResult.TimerNotFound;

                if (timer.IsFinished(ticket))
                    return TimerAsyncResult.TimerAlreadyFinished;

                var st = _awaits.GetOrAdd(ticket, _ => new AwaitState());

                // イベント取得
                var tevt = timer.GetTimerEvtType(ticket);
                var dispose = tevt.Subscribe(evt =>
                {
                    if (evt == TimeEventType.Initialize || evt == TimeEventType.Reset)
                        BumpGenerationAndCancelWaiters(ticket);
                    if (evt == TimeEventType.Finished)
                        NotifyFinished(ticket);
                    if (evt == TimeEventType.OnRemoved)
                        OnRemoved(ticket);
                });

                // 待機用TCS作成
                var tcs = new UniTaskCompletionSource<bool>();
                lock (st.FinishWaiters)
                {
                    st.FinishWaiters.Add(tcs);
                }

                CancellationTokenRegistration reg = default;
                if (ct.CanBeCanceled)
                {
                    if (ct.IsCancellationRequested)
                    {
                        if (tcs.TrySetCanceled(ct))
                        {
                            lock (st.FinishWaiters)
                                st.FinishWaiters.Remove(tcs);
                        }

                        dispose.Dispose();
                        return TimerAsyncResult.Canceled;
                    }

                    reg = ct.Register(() =>
                    {
                        lock (st.FinishWaiters)
                        {
                            if (tcs.Task.Status == UniTaskStatus.Pending)
                            {
                                tcs.TrySetCanceled(ct);
                                st.FinishWaiters.Remove(tcs);
                            }
                        }
                    });
                }

                TimerAsyncResult result = TimerAsyncResult.Faulted;
                try
                {
                    await tcs.Task;
                    result = TimerAsyncResult.Completed;
                }
                catch (OperationCanceledException)
                {
#if DEBUG
                    Debug.Log($"[TimerExt] canceled: {ticket.Name}");
#endif
                    result = TimerAsyncResult.Canceled;
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.LogError($"[TimerExt] wait error: {ex}");
#endif
                    result = TimerAsyncResult.Faulted;
                }
                finally
                {
#if DEBUG
                    Debug.Log($"[TimerExt] finally: {ticket.Name}");
#endif
                    reg.Dispose();
                    dispose.Dispose();
                }

                return result;
            });
        }

        #region --- 内部イベントメソッド群 ---
        /// <summary>完了トリガ</summary>
        internal static void NotifyFinished(TimerTicket ticket)
        {
#if DEBUG
            Debug.Log($"[TimerExt] NotifyFinished: {ticket.Name}");
#endif
            if (TryClearWaiters(ticket, out var waiters))
            {
                foreach (var tcs in waiters)
                    tcs.TrySetResult(true);

                _awaits.TryRemove(ticket, out _);
            }
        }

        /// <summary>Restart/Reset 時は世代更新＆未完了待機者をキャンセル扱いに</summary>
        public static void BumpGenerationAndCancelWaiters(TimerTicket ticket)
        {
#if DEBUG
            Debug.Log($"[TimerExt] BumpGenerationAndCancelWaiters: {ticket.Name}");
#endif
            if (TryClearWaiters(ticket, out var waiters, bumpVersion: true))
            {
                foreach (var tcs in waiters)
                    tcs.TrySetCanceled();

                _awaits.TryRemove(ticket, out _);
            }
        }

        /// <summary>Timer削除時（Unregisterなど）</summary>
        private static void OnRemoved(TimerTicket ticket)
        {
            if (TryClearWaiters(ticket, out var waiters))
            {
                foreach (var tcs in waiters)
                    tcs.TrySetCanceled();

                _awaits.TryRemove(ticket, out _);
            }
        }

        /// <summary>Waiterリストを安全にクリアして取得</summary>
        private static bool TryClearWaiters(TimerTicket ticket, out List<UniTaskCompletionSource<bool>> waiters, bool bumpVersion = false)
        {
            waiters = null;
            if (!_awaits.TryGetValue(ticket, out var st))
                return false;

            lock (st.FinishWaiters)
            {
                if (st.FinishWaiters.Count == 0)
                    return false;

                waiters = new List<UniTaskCompletionSource<bool>>(st.FinishWaiters);
                st.FinishWaiters.Clear();

                if (bumpVersion)
                    st.Version++;
            }
            return true;
        }
        #endregion
    }
}
