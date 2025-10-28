using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HighElixir.Implements.Observables;

namespace HighElixir.StateMachine.Extention
{
    /// <summary>
    /// ステートマシンとUnity Animatorの連携拡張
    /// <br/>StateMachineの遷移イベントをAnimator Triggerに変換する
    /// </summary>
    public static class AnimationExt
    {
        /// <summary>
        /// AnimatorのTrigger操作をラップする購読制御クラス
        /// </summary>
        public class AnimeWrapper : IDisposable
        {
            // 後から差し替え可能なラッパー
            private string _name;
            private Animator _animator;
            private bool _isValid;
            internal IDisposable disposable;

            /// <summary>Trigger名のハッシュ</summary>
            public int hash { get; internal set; }

            /// <summary>Animator Trigger名</summary>
            public string name
            {
                get => _name;
                set
                {
                    _name = value;
                    hash = Animator.StringToHash(_name);
                    Checked = false;
                }
            }

            /// <summary>制御対象のAnimator</summary>
            public Animator animator
            {
                get => _animator;
                set
                {
                    _animator = value;
                    Checked = false;
                }
            }

            /// <summary>Dispose済みかどうか</summary>
            public bool Disposed { get; private set; }

            /// <summary>有効性チェック済みかどうか</summary>
            public bool Checked { get; internal set; } = false;

            /// <summary>AnimatorおよびTriggerの存在確認結果</summary>
            public bool IsValid
            {
                get
                {
                    if (!Checked) _isValid = Check(this);
                    return _isValid;
                }
            }

            /// <summary>再チェックを要求</summary>
            public void Invalidate() => Checked = false;

            /// <summary>購読解除およびリソース解放</summary>
            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                disposable?.Dispose();
            }
        }

        /// <summary>
        /// 遷移イベントを購読し、Animator Triggerを発火する
        /// </summary>
        public static AnimeWrapper Subscribe<TCont, TEvt, TState>(
            this IObservable<StateMachine<TCont, TEvt, TState>.TransitionResult> ontrans,
            TCont cont,
            string animTrigger)
            where TCont : Component
            where TState : IEquatable<TState>
        {
            var wr = cont.GetWrapper(animTrigger);
            wr.disposable = ontrans.Subscribe(x =>
            {
                var wc = wr;
                if (wc == null) return;
                if (wc.IsValid)
                {
                    wc.animator.ResetTrigger(wc.name);
                    wc.animator.SetTrigger(wc.name);
                }
            });
            return wr;
        }

        /// <summary>
        /// 通常の遷移にAnimator Trigger発火を紐づける
        /// </summary>
        public static AnimeWrapper RegisterTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TState fromState,
            TEvt evt,
            TState toState,
            string animTrigger,
            Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate = null)
            where TCont : Component
            where TState : IEquatable<TState>
        {
            stateMachine.RegisterTransition(fromState, evt, toState);
            return stateMachine.OnTransWhere(fromState, evt, toState)
                               .Where(predicate)
                               .Subscribe(stateMachine.Context, animTrigger);
        }

        /// <summary>
        /// 任意遷移にAnimator Trigger発火を紐づける
        /// </summary>
        public static AnimeWrapper RegisterAnyTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TEvt evt,
            TState toState,
            string animTrigger,
            Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate = null)
            where TCont : Component
            where TState : IEquatable<TState>
        {
            stateMachine.RegisterAnyTransition(evt, toState);
            return stateMachine.OnTransWhere(evt, toState)
                               .Where(predicate)
                               .Subscribe(stateMachine.Context, animTrigger);
        }

        #region 一括登録用の糖衣構文

        /// <summary>
        /// 任意遷移のTrigger発火を複数登録する
        /// </summary>
        public static Dictionary<(TEvt evt, TState toState), AnimeWrapper>
            RegisterAnyTransitions<TCont, TEvt, TState>(
                this StateMachine<TCont, TEvt, TState> stateMachine,
                params (TEvt evt, TState toState, string animTrigger,
                        Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)[] transes)
            where TCont : Component
            where TState : IEquatable<TState>
        {
            var res = new Dictionary<(TEvt evt, TState toState), AnimeWrapper>();
            foreach (var trans in transes)
                res.Add((trans.evt, trans.toState),
                    stateMachine.RegisterAnyTransition(trans.evt, trans.toState, trans.animTrigger, trans.predicate));
            return res;
        }

        /// <summary>
        /// 通常遷移のTrigger発火を複数登録する
        /// </summary>
        public static Dictionary<(TState fromState, TEvt evt, TState toState), AnimeWrapper>
            RegisterTransitions<TCont, TEvt, TState>(
                this StateMachine<TCont, TEvt, TState> stateMachine,
                TState fromState,
                params (TEvt evt, TState toState, string animTrigger,
                        Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)[] transes)
            where TCont : Component
            where TState : IEquatable<TState>
        {
            var res = new Dictionary<(TState fromState, TEvt evt, TState toState), AnimeWrapper>();
            foreach (var trans in transes)
                res.Add((fromState, trans.evt, trans.toState),
                    stateMachine.RegisterTransition(fromState, trans.evt, trans.toState, trans.animTrigger, trans.predicate));
            return res;
        }

        #endregion

        /// <summary>
        /// すべてのアニメ購読を破棄する
        /// </summary>
        public static void DisposeAll(this IEnumerable<AnimeWrapper> wrappers)
        {
            foreach (var w in wrappers)
                w?.Dispose();
        }

        #region private

        /// <summary>
        /// AnimatorとTrigger名から新しいAnimeWrapperを生成する
        /// </summary>
        private static AnimeWrapper GetWrapper<TCont>(this TCont cont, string trigger)
            where TCont : Component
        {
            if (!cont.TryGetComponent<Animator>(out var anim))
                throw new MissingComponentException($"[StateMachine_Anim]{cont.name}にAnimatorがアタッチされていません");

            var wr = new AnimeWrapper()
            {
                name = trigger,
                animator = anim
            };
            return wr;
        }

        /// <summary>
        /// Animatorが指定したTriggerを持っているか判定する
        /// </summary>
        private static bool HasTrigger(this Animator animator, int hash)
        {
            return animator.parameters.Any(p =>
                p.type == AnimatorControllerParameterType.Trigger &&
                p.nameHash == hash);
        }

        /// <summary>
        /// Wrapperの有効性をチェックする
        /// </summary>
        private static bool Check(AnimeWrapper wrapper)
        {
            if (wrapper.Disposed)
            {
                wrapper.Checked = true;
                return false;
            }

            if (wrapper.animator == null)
            {
                wrapper.Checked = true;
                return false;
            }

            else if (wrapper.animator.HasTrigger(wrapper.hash))
            {
                wrapper.Checked = true;
                return true;
            }

            wrapper.Checked = true;
            return false;
        }

        #endregion
    }
}
