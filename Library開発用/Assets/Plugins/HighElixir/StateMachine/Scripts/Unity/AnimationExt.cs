using HighElixir.Implements.Observables;
using HighElixir.StateMachines;
using HighElixir.StateMachines.Extension;
using HighElixir.Core.UnityExtensions.AnimatorExtensions;
using System;
using UnityEngine;

namespace HighElixir.Unity.StateMachine.AnimatorExtension
{
    public static class AnimationPunblisher
    {
        #region === Subscriber ===
        public static IDisposable SetTriggerOnStateTrans<TCont,TEvt,TState>(this StateMachine<TCont, TEvt, TState> machine, Animator animator, string animTrigger)
        {
            return machine.OnTransition.Subscribe(_ =>
            {
               animator.SetTrigger(animTrigger);
            });
        }
        public static IDisposable ResetTriggerOnStateTrans<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> machine, Animator animator, string animTrigger)
        {
            return machine.OnTransition.Subscribe(_ =>
            {
                animator.ResetTrigger(animTrigger);
            });
        }
        public static IDisposable SetFloatOnStateTrans<TCont,TEvt,TState>(this StateMachine<TCont, TEvt, TState> machine, Animator animator, string animTrigger, float param)
        {
            return machine.OnTransition.Subscribe(_ =>
            {
               animator.SetFloat(animTrigger, param);
            });
        }
        public static IDisposable SetIntegerOnStateTrans<TCont,TEvt,TState>(this StateMachine<TCont, TEvt, TState> machine, Animator animator, string animTrigger, int param)
        {
            return machine.OnTransition.Subscribe(_ =>
            {
               animator.SetInteger(animTrigger, param);
            });
        }
        public static IDisposable SetBoolOnStateTrans<TCont,TEvt,TState>(this StateMachine<TCont, TEvt, TState> machine, Animator animator, string animTrigger, bool param)
        {
            return machine.OnTransition.Subscribe(_ =>
            {
               animator.SetBool(animTrigger, param);
            });
        }
        #endregion

        #region === BindTransition ===
        public static IDisposable RegisterTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> machine,
            TState fromState,
            TEvt evt,
            TState toState,
            Animator animator,
            string animTrigger)
        {
            machine.RegisterTransition(fromState, evt, toState);
            return machine.OnTransWhere(fromState, evt, toState)
                .Subscribe(_ =>
                {
                    if (AnimationValidator.IsValid(animator, AnimatorControllerParameterType.Trigger, animTrigger))
                    {
                        animator.SetTrigger(animTrigger);
                    }
                    else
                        Debug.LogWarning($"[StateMachine_Anim]AnimatorにTriggerパラメータ'{animTrigger}'が存在しないか、型が異なります");
                });
        }

        #endregion
    }
}
