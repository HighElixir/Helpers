using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HighElixir.Core.UnityExtensions
{
    public static class Interval
    {
        private static readonly Dictionary<int, int> _lastTick = new();
        public static bool Check(int interval,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (interval <= 0) return true;

            var hash = HashCode.Combine(memberName, sourceFilePath, sourceLineNumber);
            var tick = GetOrCreateTick(hash);

            int current = Time.frameCount;

            // 初回呼び出し
            if (tick < 0 || current - tick >= interval)
            {
                _lastTick[hash] = current;
                return true;
            }
            return false;
        }

        private static int GetOrCreateTick(int hash)
        {
            if (_lastTick.TryGetValue(hash, out var tick))
            {
                return tick;
            }
            _lastTick.TryAdd(hash, -1);
            return -1;
        }
    }
}