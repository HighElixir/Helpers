
using System;
using UnityEngine.Events;

namespace HighElixir.UI.Countable
{
    public interface ICountable
    {
        int Max { get; set; }
        int Min { get; set; }

        UnityEvent<int> OnChanged { get; }
        Func<int, int, bool> AllowChange { get; set; }
        int Value { get; set; }

        bool TrySetValue(int newValue);
    }
}