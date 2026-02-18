using HighElixir.Math.Hedgeable;
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
        private readonly ReactiveProperty<Hedgeable<int>> _value = new();
        private int _min = int.MinValue;
        private int _max = int.MaxValue;

        public int Min
        {
            get => _value.Value.MinValue;
            set
            {
                _value.Value.SetMin(value);
                if (_value.Value.MinValue != _min)
                    _min = value;
            }
        }
        public int Max
        {
            get => _value.Value.MaxValue;
            set
            {
                _value.Value.SetMax(value);
                if (_value.Value.MaxValue != _max)
                    _max = value;
            }
        }
        public Func<int, int, bool> AllowChange { get; set; }
        public UnityEvent<ChangeResult<int>> OnValueChanged => _onValueChanged;
        public int Value
        {
            get => _value.Value;
            set
            {
                bool isValid = AllowChange == null || AllowChange.Invoke(_value.Value, value - _value.Value);
                if (_value.Value.CanSetValue(value) && isValid)
                {
                    _value.Value.SetValue(value);
                    _text.text = _value.ToString();
                }
            }
        }

        public bool TrySetValue(int newValue)
        {
            int old = _value.Value;
            Value = newValue;
            return _value.Value != old && _value.Value == newValue;
        }

        private void Awake()
        {
            // 初期値セット
            _value.Value = new Hedgeable<int>(_defaultAmount, _min, _max);
            var d = _value.Subscribe(x =>
            {
                _text.text = x.LastChangeResult.NewValue.ToString();
                _onValueChanged?.Invoke(x.LastChangeResult);
            }).AddTo(this);
            _text.text = _value.ToString();
            this.OnDestroyAsObservable().Subscribe(_ =>
            {
                d.Dispose();
            }).AddTo(this);
            // Minus ボタン
            _minus.onClick.AddListener(() =>
            {
                TrySetValue(_value.Value - _step);
            });

            // Plus ボタン
            _plus.onClick.AddListener(() =>
            {
                TrySetValue(_value.Value + _step);
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
