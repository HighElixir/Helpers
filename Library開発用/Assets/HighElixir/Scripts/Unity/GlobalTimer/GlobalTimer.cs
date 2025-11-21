using HighElixir.Timers;
using HighElixir.Timers.Internal;
using System;
using UnityEngine;

namespace HighElixir.Unity.Timers
{
    public static class GlobalTimer
    {
        internal class Wrapper
        {
            public readonly Lazy<Timer> Timer;
            internal TimerTicket _stream;
            public bool IsCreated => Timer.IsValueCreated;
            internal Timer Instance => Timer.Value;
            public Wrapper(string name)
            {
                Timer = new Lazy<Timer>(() => new Timer(name));
            }
        }

        internal static readonly Wrapper update = new Wrapper("GlobalTimer");
        internal static readonly Wrapper fixedUpdate = new Wrapper("GlobalFixedTimer");

        public static Timer Update => update.Instance;
        public static Timer FixedUpdate => fixedUpdate.Instance;

        private static void CreateObj()
        {
            if (GameObject.FindAnyObjectByType<GlobalTimerDriver>() != null)
                return;
            GameObject go = new GameObject("GlobalTimerDriver");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<GlobalTimerDriver>();
        }
        static GlobalTimer()
        {
#if UNITY_EDITOR
            Application.quitting += () =>
            {
                TimerManager.Clear();
            };
#endif
            CreateObj();
        }
    }
}