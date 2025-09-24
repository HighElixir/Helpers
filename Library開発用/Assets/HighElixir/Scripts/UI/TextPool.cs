using TMPro;
using UnityEngine;

namespace HighElixir.Pool
{
    public class TextPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private TMP_Text _prefab;
        [SerializeField] private int _size;
        private RectTransform _container;
        private Pool<TMP_Text> _textPool;
        public Pool<TMP_Text> Pool => _textPool;
        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            _textPool = new Pool<TMP_Text>(_prefab, _size, _container);
        }
    }
}