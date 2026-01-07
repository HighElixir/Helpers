using System;
using System.Threading;
using UnityEngine;

namespace HighElixir.Core.UnityExtensions.Thread
{
    internal static class UnityThread
    {
        private static SynchronizationContext context;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            context = SynchronizationContext.Current;
        }

        public static void Post(Action action)
        {
            context?.Post(_ => action(), null);
        }
    }
}