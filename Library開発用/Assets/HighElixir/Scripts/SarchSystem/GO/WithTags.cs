using HighElixir.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.SearchSystems.Comps
{
    [Serializable]
    public sealed class WithTag : SearchSystem<GameObject, GameObject>.ISearchComponent
    {
        [SerializeField] private HashSet<string> _tags = new();
        [SerializeField] private bool _reversing;
        public WithTag(params string[] tags)
        {
            _tags.AddRange(tags);
        }
        public WithTag(bool reversing, params string[] tags)
            : this(tags)
        {
            _reversing = reversing;
        }
        public void Dispose()
        {
            _tags.Clear();
            _tags = null;
        }
        public void Initialize() { }

        public void ExecuteSearch(GameObject cont, ref List<GameObject> targets)
        {
            targets.RemoveAll(t =>
                !_reversing && !_tags.Contains(t.tag) ||
                 _reversing && _tags.Contains(t.tag));
        }

    }
}