using HighElixir.Timers;
using HighElixir.Timers.Extensions;
using HighElixir.Async.Timers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

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

        private float _upAndDown = 5f;
        private float _countDown2 = 5f;
        private float _countDown3 = 8f;
        private float _countDown4 = 2f;
        private float _pulse = 12f;
        private string _id = "TestTimer";
        private float _countUpInterval = 100f;
        private Dictionary<string, TimerTicket> _ticketHolder = new();
        private CancellationToken _token;

        private async UniTask Wait()
        {
            GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown2)], isLazy: true);
            _token = new();
            var res = await GlobalTimer.Update.WaitUntilFinishedAsync(_ticketHolder[nameof(_countDown2)], ct: _token);
            if (res == TimerAsyncResult.Completed)
            {
                Debug.Log("CountDown 2 finished");
                GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown4)], isLazy: true);
                _img2.color = Color.black;
            }
            else if (res == TimerAsyncResult.Canceled)
            {
                Debug.Log("CountDown 2 canceled");
                _img2.color = Color.red;
            }
        }

        public void Cancel()
        {
            GlobalTimer.Update.Reset(_ticketHolder[nameof(_countDown2)], isLazy: false);
        }

        public void Invoke()
        {
            if (!GlobalTimer.Update.IsRunning(_ticketHolder[nameof(_countDown2)]))
                Wait().Forget();
        }

        public void Reverse()
        {
            GlobalTimer.Update.ReverseAndStart(_ticketHolder[nameof(_upAndDown)], onlyNotRun: true);
        }


        private void Awake()
        {
            var audio = GetComponent<AudioSource>();

            // タイマー登録
            var pu = GlobalTimer.Update.PulseRegister(_pulse, "パルス", () =>
            {
                audio.Play();
            });
            _ticketHolder[nameof(_upAndDown)] = GlobalTimer.Update.UpDownRegister(_upAndDown, "アップ＆ダウン", () => Debug.Log("アップ＆ダウン finished"));

            _ticketHolder[nameof(_countDown2)] = GlobalTimer.Update.CountDownRegister(_countDown2, nameof(_countDown2), () =>
            {
            });
            _ticketHolder[nameof(_countDown3)] = GlobalTimer.FixedUpdate.CountDownRegister(_countDown3, nameof(_countDown3), () =>
            {
                Debug.Log("CountDown 3 finished");
                GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countDown3)], isLazy: true);
                if (RandomExtensions.Chance(0.2f))
                {
                    GlobalTimer.FixedUpdate.Restart(_ticketHolder[_id], isLazy: true);
                    _img2.color = Color.blue;
                }
                else
                {
                    _img2.color = Color.yellow;
                }
            });
            _ticketHolder[nameof(_countDown4)] = GlobalTimer.Update.CountDownRegister(_countDown4, nameof(_countDown4), () =>
            {
                Debug.Log("CountDown 4 finished");
                Wait().Forget();
                _img1.color = Color.green;
            });
            _ticketHolder[nameof(_countUpInterval)] = GlobalTimer.FixedUpdate.CountDownRegister(_countUpInterval, nameof(_countUpInterval), null, true);
            _ticketHolder[_id] = GlobalTimer.FixedUpdate.CountUpRegister(_id, () =>
            {
                Debug.Log("CountUp reseted");
                GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countUpInterval)], isLazy: false);
            });

            // リアクティブ購読
            GlobalTimer.FixedUpdate.GetReactiveProperty(_ticketHolder[_id])?.Subscribe(time =>
                {
                    _text.SetText($"Time : {time.Current:0.00} s");
                });
            //// タイマー開始
            GlobalTimer.Update.Start(_ticketHolder[nameof(_upAndDown)]);
            GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countDown3)]);
            GlobalTimer.FixedUpdate.Start(_ticketHolder[_id]);
            GlobalTimer.Update.Start(pu);

            Wait().Forget();
        }

        private void Update()
        {
            if (GlobalTimer.Update.TryGetNormalizedElapsed(_ticketHolder[nameof(_upAndDown)], out var elapsed)){
                _bar.value = elapsed;
            }
        }
    }
}