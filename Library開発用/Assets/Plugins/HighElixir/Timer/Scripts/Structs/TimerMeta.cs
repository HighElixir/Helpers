namespace HighElixir.Timers
{
    internal readonly struct TimerMeta
    {
        public readonly string Group;
        public readonly string Tag;

        public TimerMeta(string group, string tag)
        {
            Group = group;
            Tag = tag;
        }

        public bool HasAny => !string.IsNullOrWhiteSpace(Group) || !string.IsNullOrWhiteSpace(Tag);
    }
}
