using Cysharp.Threading.Tasks;
using HighElixir.Async.Timers;
using HighElixir.Implements.Observables;
using HighElixir.Unity.Timers;
using HighElixir.Timers.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using HighElixir.Timers;

namespace HighElixir.Samples
{
    public class TimerSample : MonoBehaviour
    {
        [Header("Imgs")]
        [SerializeField] private Image _img1;
        [SerializeField] private Image _img2;

        [Header("Text")]
        [SerializeField] private TMP_Text _text;

        [Header("Bar")]
        [SerializeField] private Slider _bar;

        // 各タイマーの初期値
        private float _upAndDown = 5f;
        private float _countDown2 = 5f;
        private float _countDown3 = 8f;
        private float _countDown4 = 2f;
        private float _pulse = 12f;

        // 上限なし CountUp の識別用ID
        private string _id = "TestTimer";

        private float _countUpInterval = 100f;

        // 生成した TimerTicket を保持する
        private Dictionary<string, TimerTicket> _ticketHolder = new();

        // キャンセル用 CT
        private CancellationTokenSource _token;

        // UniRx の購読破棄用
        private IDisposable _disposable = Disposable.Empty;

        /// <summary>
        /// CountDown2 の終了を待つサンプル処理
        /// </summary>
        private async UniTask Wait()
        {
            // CountDown2 を開始（遅延可能）
            GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown2)], isLazy: true);

            _token = new();

            // 終了まで待機（キャンセルやリセットも検出）
            var res = await GlobalTimer.Update.WaitUntilFinishedAsync(_ticketHolder[nameof(_countDown2)], false, ct: _token.Token);

            if (res == TimerAsyncResult.Completed)
            {
                Debug.Log("CountDown 2 finished");

                // CountDown4 を開始
                GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown4)], isLazy: true);
                _img2.color = Color.black;
            }
            else if (res == TimerAsyncResult.Canceled)
            {
                Debug.Log("CountDown 2 canceled");
                _img2.color = Color.red;
            }
        }

        /// <summary>
        /// CountDown2 を強制キャンセル（Reset）
        /// </summary>
        public void Cancel()
        {
            // Reset は TimerExt により await がキャンセル扱いになる
            GlobalTimer.Update.Reset(_ticketHolder[nameof(_countDown2)], isLazy: false);
        }

        /// <summary>
        /// CountDown2 が走ってなければ Wait() を呼ぶ
        /// </summary>
        public void Invoke()
        {
            if (!GlobalTimer.Update.IsRunning(_ticketHolder[nameof(_countDown2)]))
                Wait().Forget();
        }

        /// <summary>
        /// Up&Down の方向を反転して開始
        /// </summary>
        public void Reverse()
        {
            GlobalTimer.Update.ReverseAndStart(_ticketHolder[nameof(_upAndDown)], onlyNotRun: true);
        }


        private void Awake()
        {
            var audio = GetComponent<AudioSource>();

            // ===============================
            // パルスタイマー登録
            // ===============================
            _disposable.Join(
                GlobalTimer.Update
                .PulseRegister(0f, _pulse, out var ticket, "パルス", andStart: true)
                // イベントストリームを購読 → Finished のみ受け取る
                .Skip()
                .Where(id => TimerEventRegistry.Equals(id, TimeEventType.Finished))
                .Subscribe(_ =>
                {
                    audio.Play();
                })
            );
            _ticketHolder["パルス"] = ticket;

            // ===============================
            // Up&Down タイマー
            // ===============================
            _disposable.Join(
                GlobalTimer.Update.UpDownRegister(_upAndDown, out ticket, "アップ＆ダウン")
                .Where(id => TimerEventRegistry.Equals(id, TimeEventType.Finished))
                .Subscribe(_ =>
                {
                    Debug.Log("アップ＆ダウン finished");
                })
            );
            _ticketHolder[nameof(_upAndDown)] = ticket;

            // ===============================
            // CountDown2（await 用）
            // ===============================
            GlobalTimer.Update.CountDownRegister(_countDown2, out ticket, nameof(_countDown2));
            _ticketHolder[nameof(_countDown2)] = ticket;

            // ===============================
            // CountDown3（FixedUpdate）
            // ===============================
            _disposable.Join(
                GlobalTimer.FixedUpdate.CountDownRegister(_countDown3, out ticket, nameof(_countDown3))
                .Where(id => TimerEventRegistry.Equals(id, TimeEventType.Finished))
                .Subscribe(_ =>
                {
                    Debug.Log("CountDown 3 finished");

                    // 再スタート
                    GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countDown3)], isLazy: true);

                    // 20% の確率で別チケットを Restart
                    if (RandomExtensions.Chance(0.2f))
                    {
                        GlobalTimer.FixedUpdate.Restart(_ticketHolder[_id], isLazy: true);
                        _img2.color = Color.blue;
                    }
                    else
                    {
                        _img2.color = Color.yellow;
                    }
                })
            );
            _ticketHolder[nameof(_countDown3)] = ticket;

            // ===============================
            // CountDown4（CountDown2 の後に開始）
            // ===============================
            _disposable.Join(
                GlobalTimer.Update.CountDownRegister(_countDown4, out ticket, nameof(_countDown4))
                .Where(id => TimerEventRegistry.Equals(id, TimeEventType.Finished))
                .Subscribe(_ =>
                {
                    Debug.Log("CountDown 4 finished");
                    Wait().Forget();   // 再度 CountDown2 の完了待ちへ
                    _img1.color = Color.green;
                })
            );
            _ticketHolder[nameof(_countDown4)] = ticket;

            // ===============================
            // CountUpInterval
            // ===============================
            GlobalTimer.FixedUpdate.CountDownRegister(_countUpInterval, out ticket, nameof(_countUpInterval), true);
            _ticketHolder[nameof(_countUpInterval)] = ticket;

            // ===============================
            // CountUp(0) → リセット時に CountUpInterval 開始
            // ===============================
            _disposable.Join(
                GlobalTimer.FixedUpdate.CountUpRegister(0f, out ticket, _id)
                .Where(id => TimerEventRegistry.Equals(id, TimeEventType.Finished))
                .Subscribe(_ =>
                {
                    Debug.Log("CountUp reseted");

                    // CountUpInterval 開始
                    GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countUpInterval)], isLazy: false);
                })
            );
            _ticketHolder[_id] = ticket;

            // 購読の破棄をこのオブジェクトに紐付ける
            _disposable.AddTo(this);

            // ===============================
            // ReactiveProperty 更新表示（現在の経過時間）
            // ===============================
            GlobalTimer.FixedUpdate.GetReactiveProperty(_ticketHolder[_id])?.Subscribe(time =>
            {
                _text.SetText($"Time : {time.Current:0.00} s");
            });


            // ===============================
            // タイマー起動
            // ===============================
            GlobalTimer.Update.Start(_ticketHolder[nameof(_upAndDown)]);
            GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countDown3)]);
            GlobalTimer.FixedUpdate.Start(_ticketHolder[_id]);
            GlobalTimer.Update.Start(ticket);   // 最後に登録した ticket = CountUpInterval

            // CountDown2 の待機開始
            Wait().Forget();
        }

        private void Update()
        {
            // Up&Down の経過正規化をバーに反映
            if (GlobalTimer.Update.TryGetNormalizedElapsed(_ticketHolder[nameof(_upAndDown)], out var elapsed))
            {
                _bar.value = elapsed;
            }
        }
    }
}
