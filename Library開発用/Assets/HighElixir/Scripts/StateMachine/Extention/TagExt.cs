namespace HighElixir.StateMachine.Extention
{
    public static class TagExt
    {
        public static bool HasAny<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, params string[] tags)
            
        {
            foreach (var tag in tags)
                if (s.Current.info.State.Tags.Contains(tag)) return true;
            return false;
        }

        public static bool HasAny<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, TState target, params string[] tags)
            
        {
            if (s.TryGetStateInfo(target, out var info))
            {
                foreach (var tag in tags)
                {
                    if (info.State.Tags.Contains(tag))
                        return true;
                }
            }
            return false;
        }

        public static bool HasAll<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, params string[] tags)
            
        {
            foreach (var tag in tags)
                if (!s.Current.info.State.Tags.Contains(tag)) return false;
            return true;
        }
        public static bool HasAll<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, TState target, params string[] tags)
            
        {
            if (s.TryGetStateInfo(target, out var info))
            {
                foreach (var tag in tags)
                    if (!info.State.Tags.Contains(tag)) return false;
                return true;
            }
            return false;
        }
    }
}