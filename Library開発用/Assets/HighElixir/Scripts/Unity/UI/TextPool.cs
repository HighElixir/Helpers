using TMPro;
using UnityEngine;

namespace HighElixir.Unity.Pools.UI
{
    public class TextPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private TMP_Text _prefab;
        [SerializeField] private int _size;
        private RectTransform _container;
        private ObjectPool<TMP_Text> _textPool;
        public ObjectPool<TMP_Text> Pool => _textPool;
        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            _textPool = new ObjectPool<TMP_Text>(_prefab, _size, _container);
        }
    }
}