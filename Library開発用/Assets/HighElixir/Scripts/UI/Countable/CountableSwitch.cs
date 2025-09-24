using System;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HighElixir.UI.Countable
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

        [Header("Audio")]
        [SerializeField] private ClickSoundOp _soundOption = ClickSoundOp.One;
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _disallowedSound;
        [SerializeField] private AudioClip _minusSound;
        [SerializeField] private AudioClip _plusSound;
        [SerializeField] private UnityEvent<int> _onChanged = new();
        private AudioSource _audioSource;

        [Header("Data")]
        [SerializeField] private int _defaultAmount = 0;
        [SerializeField] private int _oneClickChange = 1;
        private HedgeableInt _value;
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
        public UnityEvent<int> OnChanged => _onChanged;
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
                    _onChanged?.Invoke(this);
                }
            }
        }

        public bool TrySetValue(int newValue)
        {
            int old = _value;
            Value = newValue;
            return _value != old && _value == newValue;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
                _audioSource.PlayOneShot(clip);
        }

        private void Awake()
        {
            // AudioSource 準備
            _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            // 初期値セット
            _value = new HedgeableInt(_defaultAmount)
                .SetMin(min)
                .SetMax(max);
            var d = _value.Subscribe((x, _) => _text.text = x.ToString()).AddTo(this);
            _text.text = _value.ToString();
            this.OnDestroyAsObservable().Subscribe(_ =>
            {
                d.Dispose();
            }).AddTo(this);
            // Minus ボタン
            _minus.onClick.AddListener(() =>
            {
                bool ok = TrySetValue(_value - _oneClickChange);
                if (!ok)
                {
                    Play(_disallowedSound);
                }
                else
                {
                    if (_soundOption == ClickSoundOp.Two)
                        Play(_minusSound);
                    else
                        Play(_clickSound);
                }
            });

            // Plus ボタン
            _plus.onClick.AddListener(() =>
            {
                bool ok = TrySetValue(_value + _oneClickChange);
                if (!ok)
                {
                    Play(_disallowedSound);
                }
                else
                {
                    if (_soundOption == ClickSoundOp.Two)
                        Play(_plusSound);
                    else
                        Play(_clickSound);
                }
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
