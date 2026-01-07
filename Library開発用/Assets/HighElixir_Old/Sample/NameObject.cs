using UnityEngine;

namespace HighElixir.Samples
{
    [CreateAssetMenu(menuName = "HighElixir/Test/Named", fileName = "file")]
    public class NamedObject : ScriptableObject
    {
        [LinkedFileName]
        public string Name;
    }
}