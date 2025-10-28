using HighElixir.UI.Countable;
using UniRx;
using UnityEngine;

namespace HighElixir.Samples.UI
{
    public class CountableSample : MonoBehaviour
    {
        [SerializeField] private CountableSwitch _cSwitch;
        [SerializeField] private Camera _mainCamera;

        private void Awake()
        {
            _cSwitch.Min = 0;
            _cSwitch.Max = 24;
            _cSwitch.OnValueChanged.AsObservable().Subscribe(newValue =>
            {
                Debug.Log($"CountableSwitch value changed to: {newValue}");
            }).AddTo(this);
        }
    }
}