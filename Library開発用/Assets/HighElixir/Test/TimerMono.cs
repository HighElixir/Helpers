using HighElixir.Timers;
using UnityEngine;

namespace HighElixir.Test
{
    public class TimerMono : MonoBehaviour
    {

        private Timer _timer;
        private float _countDown = 5f;
        private float _countDown2 = 5f;
        private float _countDown3 = 8f;
        private float _countDown4 = 2f;
        private string _id = "TestTimer";
        private float _countUpInterval = 1f;
        private void Awake()
        {
            _timer = new Timer(GetType());
            _timer.CountDownRegister(nameof(_countDown), _countDown, () => Debug.Log("CountDown 1 finished"));
            _timer.CountDownRegister(nameof(_countDown2), _countDown2, () =>
            {
                Debug.Log("CountDown 2 finished");
                _timer.Start(nameof(_countDown4), isLazy: true);
            });
            _timer.CountDownRegister(nameof(_countDown3), _countDown3, () =>
            {
                Debug.Log("CountDown 3 finished");
                _timer.Start(nameof(_countDown3), isLazy: true);
                if (RandomExtensions.Chance(0.2f))
                {
                    _timer.Reset(_id, isLazy: true);
                }
            });
            _timer.CountDownRegister(nameof(_countDown4), _countDown4, () =>
            {
                Debug.Log("CountDown 4 finished");
                _timer.Start(nameof(_countDown2), isLazy: true);
            });
            _timer.CountDownRegister(nameof(_countUpInterval), _countUpInterval, () =>
            {
                _timer.Start(_id, init: false, isLazy: true);
            });
            _timer.CountUpRegister(_id, () =>
            {
                Debug.Log("CountUp reseted");
                _timer.Start(nameof(_countUpInterval), isLazy:true);
            });

            // タイマー開始
            _timer.Start(nameof(_countDown));
            _timer.Start(nameof(_countDown2));
            _timer.Start(nameof(_countDown3));
            _timer.Start(_id);
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            //Debug.Log($"[TimerMono] {_timer.Contains(nameof(_countDown))} {_timer.Contains(nameof(_countDown2))} {_timer.Contains(nameof(_countDown3))} {_timer.Contains(nameof(_countDown4))}");
            _timer.Update(Time.fixedDeltaTime);
        }
    }
}