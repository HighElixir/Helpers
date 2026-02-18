using System.Collections.Generic;

namespace HighElixir.Timers
{
    public enum TimeEventType : int
    {
        None = 0,
        Stop = 1,
        Start = 2,
        Reset = 3,
        Initialize = 4,
        Finished = 5,
        OnRemoved = 6,
    }

    /// <summary>
    /// イベントIDをハッシュ化し管理。ランタイムごとにハッシュ値は変動する
    /// </summary>
    public static class TimerEventRegistry
    {
        private static readonly Dictionary<TimeEventType, int> _permanentEvent = EnumWrapper.GetValueConvertedMap<TimeEventType, int>(v => (int)v);
        private static readonly Dictionary<string, int> _hashed = new();

        public static int TakeEvt(TimeEventType eventType)
        {
            return _permanentEvent[eventType];
        }

        public static int TakeEvt(string eventName)
        {
            // 登録されてない場合、新たに発行
            if (!_hashed.TryGetValue(eventName, out int value))
                value = _hashed[eventName] = eventName.GetHashCode();
            return value;
        }

        public static bool Equals(int id, TimeEventType eventType)
        {
            return _permanentEvent[eventType] == id;
        }

        public static bool Equals(int id, string eventName)
        {
            if (_hashed.TryGetValue(eventName, out var hashed))
            {
                return hashed.Equals(id);
            }
            return false;
        }

        public static bool HasAny(int id, params TimeEventType[] types)
        {
            foreach(var type in types)
            {
                if (Equals(id, type)) return true;
            }
            return false;
        }

        public static bool HasAny(int id, params string[] types)
        {
            foreach (var type in types)
            {
                if (Equals(id, type)) return true;
            }
            return false;
        }

        public static bool TryGetType(int id, out TimeEventType type)
        {
            foreach (var pair in _permanentEvent)
            {
                if (pair.Value == id)
                {
                    type = pair.Key;
                    return true;
                }
            }

            type = TimeEventType.None;
            return false;
        }

        public static TimeEventType ToType(int id)
        {
            return TryGetType(id, out var type)
                ? type
                : TimeEventType.None;
        }
    }
}
