using HighElixir.Timers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
namespace HighElixir.Test
{
    public class TimerMono : MonoBehaviour
    {
        [Header("Imgs")]
        [SerializeField] private Image _img1;
        [SerializeField] private Image _img2;
        [Header("Text")]
        [SerializeField] private TMP_Text _text;

        private float _countDown = 5f;
        private float _countDown2 = 5f;
        private float _countDown3 = 8f;
        private float _countDown4 = 2f;
        private float _pulse = 12f;
        private string _id = "TestTimer";
        private float _countUpInterval = 100f;
        private Dictionary<string, TimerTicket> _ticketHolder = new();
        private void Awake()
        {
            var audio = GetComponent<AudioSource>();
            var pu = GlobalTimer.Update.PulseRegister(_pulse, "パルス", () =>
            {
                audio.Play();
            });
            _ticketHolder[nameof(_countDown)] = GlobalTimer.Update.CountDownRegister(_countDown, nameof(_countDown), () => Debug.Log("CountDown 1 finished"));
            _ticketHolder[nameof(_countDown2)] = GlobalTimer.Update.CountDownRegister(_countDown2, nameof(_countDown2), () =>
            {
                Debug.Log("CountDown 2 finished");
                GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown4)], isLazy: true);
                _img1.color = Color.black;
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
                GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown2)], isLazy: true);
                _img1.color = Color.green;
            });
            _ticketHolder[nameof(_countUpInterval)] = GlobalTimer.FixedUpdate.CountDownRegister(_countUpInterval, nameof(_countUpInterval), null, true);
            _ticketHolder[_id] = GlobalTimer.FixedUpdate.CountUpRegister(_id, () =>
            {
                Debug.Log("CountUp reseted");
                GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countUpInterval)], isLazy: false);
            });
            GlobalTimer.FixedUpdate.GetReactiveProperty(_ticketHolder[_id])?.Subscribe(time =>
                {
                    _text.SetText($"Time : {time:0.00} s");
                });
            //// タイマー開始
            GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown)]);
            GlobalTimer.Update.Start(_ticketHolder[nameof(_countDown2)]);
            GlobalTimer.FixedUpdate.Start(_ticketHolder[nameof(_countDown3)]);
            GlobalTimer.FixedUpdate.Start(_ticketHolder[_id]);
            GlobalTimer.Update.Start(pu);
        }

        public void Restart()
        {
            GlobalTimer.FixedUpdate.Restart(_ticketHolder[_id], true);
        }
        public void StartC()
        {
            GlobalTimer.FixedUpdate.Start(_ticketHolder[_id], false, true);
        }
        public void Pause()
        {
            GlobalTimer.FixedUpdate.Stop(_ticketHolder[_id], false, true);
        }
    }
}