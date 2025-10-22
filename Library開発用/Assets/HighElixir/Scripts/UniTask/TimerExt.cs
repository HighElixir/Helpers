using Cysharp.Threading.Tasks;
using HighElixir.Timers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HighElixir.Async.Timers
{
    /// <summary>
    /// Timer 拡張: UniTask による非同期待機サポート
    /// </summary>
    public static class TimerExt
    {
        // チケットごとの待機状態
        private sealed class AwaitState
        {
            public int Version; // Initialize/Resetで++する
            public readonly List<UniTaskCompletionSource<bool>> FinishWaiters = new();
        }

        // 全体管理
        private static readonly ConcurrentDictionary<TimerTicket, AwaitState> _awaits = new();

        /// <summary>
        /// タイマーが完了するまで待機する (UniTask.Create 版)
        /// </summary>
        public static UniTask<TimerAsyncResult> WaitUntilFinishedAsync(this HighElixir.Timers.Timer timer, TimerTicket ticket, bool isLazy = true, CancellationToken ct = default)
        {
            return UniTask.Create<TimerAsyncResult>(async () =>
            {
                // 遅延開始対応（Startされるまで待機）
                if (isLazy)
                    await UniTask.WaitUntil(
                        () => timer.IsRunning(ticket),
                        PlayerLoopTiming.PreUpdate,
                        ct);

                if (!timer.Contains(ticket))
                    throw new InvalidOperationException("Timer not found.");

                if (timer.IsFinished(ticket))
                    return TimerAsyncResult.TimerAlreadyFinished;

                var st = _awaits.GetOrAdd(ticket, _ => new AwaitState());

                // イベント取得
                var tevt = timer.GetTimerEvt(ticket);
                Action onFinished = () => NotifyFinished(ticket);
                Action onRemoved = () => OnRemoved(ticket);
                Action onReset = () => BumpGenerationAndCancelWaiters(ticket);
                Action onInit = () => BumpGenerationAndCancelWaiters(ticket);

                tevt.OnFinished += onFinished;
                tevt.OnRemoved += onRemoved;
                tevt.OnReset += onReset;
                tevt.OnInitialized += onInit;

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
                        // 既にキャンセル済みなら即キャンセル扱い
                        if (tcs.TrySetCanceled(ct))
                        {
                            lock (st.FinishWaiters)
                                st.FinishWaiters.Remove(tcs);
                        }
                        DetachAll(tevt, onFinished, onRemoved, onReset, onInit);
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
                    // 想定内：CT/Reset/Restart/Remove 等でキャンセル
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
                    DetachAll(tevt, onFinished, onRemoved, onReset, onInit);
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

        /// <summary>Restart/Reset時は世代更新＆未完了待機者をキャンセル扱いに</summary>
        public static void BumpGenerationAndCancelWaiters(TimerTicket ticket)
        {
            Debug.Log($"[TimerExt] BumpGenerationAndCancelWaiters: {ticket.Name}");
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

        /// <summary>登録解除共通化</summary>
        private static void DetachAll(ITimerEvt tevt, Action onFinished, Action onRemoved, Action onReset, Action onInit)
        {
            tevt.OnFinished -= onFinished;
            tevt.OnRemoved -= onRemoved;
            tevt.OnReset -= onReset;
            tevt.OnInitialized -= onInit;
        }

        /// <summary>Waiterリストを安全にクリアして取得</summary>
        private static bool TryClearWaiters(TimerTicket ticket, out List<UniTaskCompletionSource<bool>> waiters, bool bumpVersion = false)
        {
            waiters = null;
            if (!_awaits.TryGetValue(ticket, out var st)) return false;

            lock (st.FinishWaiters)
            {
                if (st.FinishWaiters.Count == 0)
                    return false;

                waiters = new List<UniTaskCompletionSource<bool>>(st.FinishWaiters);
                st.FinishWaiters.Clear();

                if (bumpVersion) st.Version++;
            }
            return true;
        }
        #endregion
    }
}
