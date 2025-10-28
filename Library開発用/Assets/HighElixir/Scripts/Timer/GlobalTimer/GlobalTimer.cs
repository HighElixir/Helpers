using System;
#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif

namespace HighElixir.Timers
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

#if UNITY_2017_1_OR_NEWER
        private static void CreateObj()
        {
            if (GameObject.FindAnyObjectByType<Timers.Internal.GlobalTimerDriver>() != null)
                return;
            GameObject go = new GameObject("GlobalTimerDriver");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<Timers.Internal.GlobalTimerDriver>();
        }
        static GlobalTimer()
        {
            Application.quitting += () =>
            {
                Timer.DisposeAll();
            };
            CreateObj();
        }
#else
        static GlobalTimer()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => 
            {
                if (update.IsCreated)
                    update.Timer.Value.Dispose();
                if (fixedUpdate.IsCreated)
                    fixedUpdate.Timer.Value.Dispose();
            };
        }
#endif
    }
}