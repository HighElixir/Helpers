using UnityEngine;

namespace HighElixir.Test
{
    [CreateAssetMenu(menuName ="HighElixir/Test/Named", fileName ="file")]
    public class NamedObject : ScriptableObject
    {
        [LinkedFileName]
        public string Name;
    }
}