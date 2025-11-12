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

        // 子要素の現在のステートも含め、特定のタグを含むか確認
        public static bool HasTagOnChild<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState>.StateInfo info, string tag)
        {
            var sub = info.SubHost;
            while (sub != null)
            {
                if (sub.CurrentStateTag.Contains(tag)) return true;
                if (!sub.TryGetCurrentSubHost(out sub)) break;
            }
            return info.State.Tags.Contains(tag);
        }
        public static bool HasTagOnChild<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, string tag)
        {
            var sub = s.Current.info.SubHost;
            while (sub != null)
            {
                if (sub.CurrentStateTag.Contains(tag)) return true;
                if (!sub.TryGetCurrentSubHost(out sub)) break;
            }
            return s.Current.info.State.Tags.Contains(tag);
        }
    }
}