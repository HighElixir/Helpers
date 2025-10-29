using HighElixir.Implements.Observables;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        public class AnimationBinding : IDisposable
        {
            private string _name;
            private Animator _animator;
            private bool _isValid;
            internal IDisposable disposable;

            public int hash { get; internal set; }
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

            public Animator animator
            {
                get => _animator;
                set
                {
                    _animator = value;
                    Checked = false;
                }
            }

            public bool Disposed { get; private set; }
            public bool Checked { get; internal set; } = false;

            public bool IsValid
            {
                get
                {
                    if (!Checked) _isValid = Check(this);
                    return _isValid;
                }
            }

            public void Invalidate() => Checked = false;

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
        public static AnimationBinding Subscribe<TCont, TEvt, TState>(
            this IObservable<StateMachine<TCont, TEvt, TState>.TransitionResult> ontrans,
            TCont cont,
            string animTrigger)
            where TCont : Component

        {
            var wr = cont.GetWrapper(animTrigger);
            wr.disposable = ontrans.Subscribe(_ =>
            {
                if (wr == null || !wr.IsValid) return;
                wr.animator.ResetTrigger(wr.name);
                wr.animator.SetTrigger(wr.name);
            });
            return wr;
        }

        #region ▼ 単体登録 ▼

        public static AnimationBinding RegisterTransition<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState>.StateInfo info, TEvt evt, TState toState, string animTrigger, Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate = null)
            where TCont : Component
        {
            info.RegisterTransition(evt, toState);
            return info.Parent.OnTransWhere(info.ID, evt, toState)
                               .Where(predicate)
                               .Subscribe(info.Parent.Context, animTrigger);
        }

        /// <summary>
        /// 通常の遷移にAnimator Trigger発火を紐づける（条件付き）
        /// </summary>
        public static AnimationBinding RegisterTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TState fromState,
            TEvt evt,
            TState toState,
            string animTrigger,
            Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)
            where TCont : Component

        {
            stateMachine.RegisterTransition(fromState, evt, toState);
            return stateMachine.OnTransWhere(fromState, evt, toState)
                               .Where(predicate)
                               .Subscribe(stateMachine.Context, animTrigger);
        }

        /// <summary>
        /// 通常の遷移にAnimator Trigger発火を紐づける（条件なし）
        /// </summary>
        public static AnimationBinding RegisterTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TState fromState,
            TEvt evt,
            TState toState,
            string animTrigger)
            where TCont : Component

            => stateMachine.RegisterTransition(fromState, evt, toState, animTrigger, null);


        /// <summary>
        /// 任意遷移にAnimator Trigger発火を紐づける（条件付き）
        /// </summary>
        public static AnimationBinding RegisterAnyTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TEvt evt,
            TState toState,
            string animTrigger,
            Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)
            where TCont : Component

        {
            stateMachine.RegisterAnyTransition(evt, toState);
            return stateMachine.OnTransWhere(evt, toState)
                               .Where(predicate)
                               .Subscribe(stateMachine.Context, animTrigger);
        }

        /// <summary>
        /// 任意遷移にAnimator Trigger発火を紐づける（条件なし）
        /// </summary>
        public static AnimationBinding RegisterAnyTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TEvt evt,
            TState toState,
            string animTrigger)
            where TCont : Component

            => stateMachine.RegisterAnyTransition(evt, toState, animTrigger, null);

        #endregion


        #region ▼ 一括登録 ▼

        /// <summary>
        /// 任意遷移のTrigger発火を複数登録する（条件付き）
        /// </summary>
        public static Dictionary<(TEvt evt, TState toState), AnimationBinding>
            RegisterAnyTransitions<TCont, TEvt, TState>(
                this StateMachine<TCont, TEvt, TState> stateMachine,
                params (TEvt evt, TState toState, string animTrigger,
                        Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)[] transes)
            where TCont : Component

        {
            var res = new Dictionary<(TEvt evt, TState toState), AnimationBinding>();
            foreach (var trans in transes)
                res.Add((trans.evt, trans.toState),
                    stateMachine.RegisterAnyTransition(trans.evt, trans.toState, trans.animTrigger, trans.predicate));
            return res;
        }

        /// <summary>
        /// 任意遷移のTrigger発火を複数登録する（条件なし）
        /// </summary>
        public static Dictionary<(TEvt evt, TState toState), AnimationBinding>
            RegisterAnyTransitions<TCont, TEvt, TState>(
                this StateMachine<TCont, TEvt, TState> stateMachine,
                params (TEvt evt, TState toState, string animTrigger)[] transes)
            where TCont : Component

        {
            var res = new Dictionary<(TEvt evt, TState toState), AnimationBinding>();
            foreach (var trans in transes)
                res.Add((trans.evt, trans.toState),
                    stateMachine.RegisterAnyTransition(trans.evt, trans.toState, trans.animTrigger));
            return res;
        }

        /// <summary>
        /// 通常遷移のTrigger発火を複数登録する（条件付き）
        /// </summary>
        public static Dictionary<(TState fromState, TEvt evt, TState toState), AnimationBinding>
            RegisterTransitions<TCont, TEvt, TState>(
                this StateMachine<TCont, TEvt, TState> stateMachine,
                TState fromState,
                params (TEvt evt, TState toState, string animTrigger,
                        Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)[] transes)
            where TCont : Component

        {
            var res = new Dictionary<(TState fromState, TEvt evt, TState toState), AnimationBinding>();
            foreach (var trans in transes)
                res.Add((fromState, trans.evt, trans.toState),
                    stateMachine.RegisterTransition(fromState, trans.evt, trans.toState, trans.animTrigger, trans.predicate));
            return res;
        }

        /// <summary>
        /// 通常遷移のTrigger発火を複数登録する（条件なし）
        /// </summary>
        public static Dictionary<(TState fromState, TEvt evt, TState toState), AnimationBinding>
            RegisterTransitions<TCont, TEvt, TState>(
                this StateMachine<TCont, TEvt, TState> stateMachine,
                TState fromState,
                params (TEvt evt, TState toState, string animTrigger)[] transes)
            where TCont : Component

        {
            var res = new Dictionary<(TState fromState, TEvt evt, TState toState), AnimationBinding>();
            foreach (var trans in transes)
                res.Add((fromState, trans.evt, trans.toState),
                    stateMachine.RegisterTransition(fromState, trans.evt, trans.toState, trans.animTrigger));
            return res;
        }

        #endregion


        /// <summary>
        /// すべてのアニメ購読を破棄する
        /// </summary>
        public static void DisposeAll(this IEnumerable<AnimationBinding> wrappers)
        {
            foreach (var w in wrappers)
                w?.Dispose();
        }

        #region private

        private static AnimationBinding GetWrapper<TCont>(this TCont cont, string trigger)
            where TCont : Component
        {
            if (!cont.TryGetComponent<Animator>(out var anim))
                throw new MissingComponentException($"[StateMachine_Anim]{cont.name}にAnimatorがアタッチされていません");

            return new AnimationBinding()
            {
                name = trigger,
                animator = anim
            };
        }

        private static bool HasTrigger(this Animator animator, int hash)
            => animator.parameters.Any(p =>
                p.type == AnimatorControllerParameterType.Trigger &&
                p.nameHash == hash);

        private static bool Check(AnimationBinding wrapper)
        {
            if (wrapper.Disposed) return false;
            if (wrapper.animator == null) return false;
            return wrapper.animator.HasTrigger(wrapper.hash);
        }

        #endregion
    }
}
