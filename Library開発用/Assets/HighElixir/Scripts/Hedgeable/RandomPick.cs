using System.Collections.Generic;
using UnityEngine;

namespace HighElixir
{
    public static class RandomPick
    {
        public static T PickFromList<T>(List<T> from)
        {
            return from[Random.Range(0, from.Count - 1)];
        }
    }
}