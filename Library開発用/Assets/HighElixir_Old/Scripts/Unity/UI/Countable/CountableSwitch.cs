using HighElixir.Hedgeable;
using System;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HighElixir.Unity.UI.Countable
{
    public partial class CountableSwitch : MonoBehaviour, ICountable
    {
        public enum ClickSoundOp
        {
            One, // 常に _clickSound を再生
            Two, // Minus / Plus で別々の音を再生
        }

        [Header("Reference")]
        [SerializeField] private Button _minus;
        [SerializeField] private Button _plus;
        [SerializeField] private TMP_InputField _text;

        [Header("Action")]
        [SerializeField] private UnityEvent<ChangeResult<int>> _onValueChanged = new();

        [Header("Data")]
        [SerializeField] private int _defaultAmount = 0;
        [SerializeField] private int _step = 1;
        private Hedgeable<int> _value;
        private int min = int.MinValue;
        private int max = int.MaxValue;

        public int Min
        {
            get => _value.MinValue;
            set
            {
                _value.SetMin(value);
            }
        }
        public int Max
        {
            get => _value.MaxValue;
            set
            {
                _value.SetMax(value);
            }
        }
        public Func<int, int, bool> AllowChange { get; set; }
        public UnityEvent<ChangeResult<int>> OnValueChanged => _onValueChanged;
        public int Value
        {
            get => _value;
            set
            {
                bool isValid = AllowChange == null || AllowChange.Invoke(_value, value - _value);
                if (_value.CanSetValue(value) && isValid)
                {
                    _value.Value = value;
                    _text.text = _value.ToString();
                }
            }
        }

        public bool TrySetValue(int newValue)
        {
            int old = _value;
            Value = newValue;
            return _value != old && _value == newValue;
        }

        private void Awake()
        {
            // 初期値セット
            _value = new Hedgeable<int>(_defaultAmount, min, max);
            var d = _value.Subscribe(x =>
            {
                _text.text = x.NewValue.ToString();
                _onValueChanged?.Invoke(x);
            }).AddTo(this);
            _text.text = _value.ToString();
            this.OnDestroyAsObservable().Subscribe(_ =>
            {
                d.Dispose();
            }).AddTo(this);
            // Minus ボタン
            _minus.onClick.AddListener(() =>
            {
                TrySetValue(_value - _step);
            });

            // Plus ボタン
            _plus.onClick.AddListener(() =>
            {
                TrySetValue(_value + _step);
            });

            // テキスト入力確定時
            _text.onEndEdit.AddListener(s =>
            {
                if (!int.TryParse(s, out var temp))
                {
                    _text.text = _value.ToString();
                    return;
                }
                bool ok = TrySetValue(temp);
                if (!ok)
                {
                    _text.text = _value.ToString(); // 失敗時は表示戻し
                }
            });
        }
        public static implicit operator int(CountableSwitch countable) => countable.Value;
    }
}
