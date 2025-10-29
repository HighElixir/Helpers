using HighElixir.Implements;
using HighElixir.Implements.Observables;
using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine.Extention
{
    /// <summary>
    /// ステートマシンのイベント購読・フィルタリング拡張
    /// </summary>
    public static class StateMachineExtension
    {
        /// <summary>
        /// 指定された from / event / to に一致する遷移イベントを購読する
        /// </summary>
        public static IObservable<StateMachine<TCont, TEvt, TState>.TransitionResult>
            OnTransWhere<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, TState from, TEvt evt, TState to)
            
        {
            return s.OnTransition.Where(x =>
                    x.FromState.Equals(from) &&
                    x.Event.Equals(evt) &&
                    x.ToState.Equals(to));
        }

        /// <summary>
        /// 任意の from から指定された event / to に一致する遷移イベントを購読する
        /// </summary>
        public static IObservable<StateMachine<TCont, TEvt, TState>.TransitionResult>
            OnTransWhere<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, TEvt evt, TState to)
            
        {
            return s.OnTransition.Where(x =>
                    x.Event.Equals(evt) &&
                    x.ToState.Equals(to));
        }
    }

    /// <summary>
    /// ステートマシンの登録系ユーティリティ拡張
    /// </summary>
    public static class RegisterExt
    {
        /// <summary>
        /// 同一ステートから複数の遷移をまとめて登録する
        /// </summary>
        public static IDisposable RegisterTransitions<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> s,
            TState from,
            params (TEvt evt, TState to,
                    Action<StateMachine<TCont, TEvt, TState>.TransitionResult> action,
                    Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)[] transes)
            
        {
            var dis = new List<IDisposable>();
            foreach (var t in transes)
                dis.Add(s.RegisterTransition(from, t.evt, t.to, t.action, t.predicate));

            if (dis.Count == 0) return Disposable.Empty;

            var d = dis[0];
            dis.Remove(d);
            return d.Join(dis.ToArray());
        }

        /// <summary>
        /// コールバックを伴わないシンプルな複数遷移登録
        /// </summary>
        public static void RegisterTransitions<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> s,
            TState from,
            params (TEvt evt, TState to)[] transes)
            
        {
            foreach (var t in transes)
                s.RegisterTransition(from, t.evt, t.to);
        }

        /// <summary>
        /// どのステートからでも遷移可能な任意遷移をまとめて登録
        /// </summary>
        public static void RegisterAnyTransitions<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> s,
            params (TEvt evt, TState toState)[] transes)
            
        {
            foreach (var t in transes)
                s.RegisterAnyTransition(t.evt, t.toState);
        }

        /// <summary>
        /// 任意遷移にコールバックと条件を設定して複数登録する
        /// </summary>
        public static IDisposable RegisterAnyTransition<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> s,
            params (TEvt evt, TState to,
                    Action<StateMachine<TCont, TEvt, TState>.TransitionResult> action,
                    Func<StateMachine<TCont, TEvt, TState>.TransitionResult, bool> predicate)[] transes)
            
        {
            var dis = new List<IDisposable>();
            foreach (var t in transes)
                dis.Add(s.RegisterAnyTransition(t.evt, t.to, t.action, t.predicate));

            if (dis.Count == 0) return Disposable.Empty;

            var d = dis[0];
            dis.Remove(d);
            return d.Join(dis.ToArray());
        }
    }
}
