using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.Editors.Timers
{
    public sealed class ColorData : ScriptableObject
    {
        public List<ColorPair> ColorPairs = new List<ColorPair>();

        public bool TryGet(string name, out ColorPair colorPair)
        {
            foreach (var pair in ColorPairs)
            {
                if (pair.ParentName == name)
                {
                    colorPair = pair;
                    return true;
                }
            }
            colorPair = null;
            return false;
        }
    }

    [Serializable]
    public sealed class ColorPair
    {
        public string ParentName;
        public Color Color;
        public Color FontColor = Color.white;
    }
}