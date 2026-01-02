//using System;
//using UnityEngine;

//namespace HighElixir.Unity.StateMachine.Extension
//{
//    public static partial class AnimationExt
//    {
//        /// <summary>
//        /// AnimatorのTrigger操作をラップする購読制御クラス
//        /// </summary>
//        public class AnimationBinding : IDisposable
//        {
//            private string _name;
//            private Animator _animator;
//            private bool _isValid;
//            internal IDisposable disposable;

//            public int hash { get; internal set; }
//            public string name
//            {
//                get => _name;
//                set
//                {
//                    _name = value;
//                    hash = Animator.StringToHash(_name);
//                    Checked = false;
//                }
//            }

//            public Animator animator
//            {
//                get => _animator;
//                set
//                {
//                    _animator = value;
//                    Checked = false;
//                }
//            }

//            public bool Disposed { get; private set; }
//            public bool Checked { get; internal set; } = false;

//            public bool IsValid
//            {
//                get
//                {
//                    if (!Checked)
//                    {
//                        _isValid = Ch(this);
//                        Checked = true;
//                    }
//                    return _isValid;
//                }
//            }

//            public void Invalidate() => Checked = false;

//            public void Dispose()
//            {
//                if (Disposed) return;
//                Disposed = true;
//                try
//                {
//                    disposable?.Dispose();
//                }
//                finally
//                {
//                    disposable = null;
//                    _animator = null;
//                    _name = null;
//                    Checked = false;
//                }
//            }
//        }
//    }
//}