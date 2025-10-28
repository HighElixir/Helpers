using System;

namespace HighElixir.StateMachine.Extention
{
    public static class TagExt
    {
        public static bool HasAny<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, params string[] tags)
            where TState : IEquatable<TState>
        {
            return s.Has_Internal(tags);
        }

        public static bool HasAll<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, params string[] tags)
            where TState : IEquatable<TState>
        {
            foreach (var tag in tags)
                if (!s.Current.state.Tags.Contains(tag)) return false;
            return true;
        }

        private static bool Has_Internal<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, params string[] tags)
            where TState : IEquatable<TState>
        {
            foreach (var tag in tags)
                if (s.Current.state.Tags.Contains(tag)) return true;
            return false;
        }
    }
}