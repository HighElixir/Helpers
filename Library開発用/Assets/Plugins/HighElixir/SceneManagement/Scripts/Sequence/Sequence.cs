using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace HighElixir.HESceneManager.DirectionSequence
{
    public sealed class Sequence
    {
        private readonly SceneService _service;
        public SceneService Service => _service;

        public SceneStore Store { get; } = new();

        private readonly List<Entry> _entries = new();

        private float _insertionPoint = 0f;
        private int _currentGroupId = 0;
        private float _currentGroupStart = 0f;
        private int _groupCounter = 0;

        public float Duration { get; private set; } = 0f;
        public int Count => _entries.Count;

        public Sequence(SceneService service)
        {
            _service = service;
        }

        // -------------------- Reset / Clear --------------------

        /// <summary>
        /// アクションだけ全消し（Storeは残す）
        /// </summary>
        public Sequence ClearActions()
        {
            _entries.Clear();
            ResetTimelineState();
            return this;
        }

        /// <summary>
        /// Storeだけ全消し（アクションは残す）
        /// </summary>
        public Sequence ClearStore()
        {
            Store.Clear();
            return this;
        }

        /// <summary>
        /// アクションもStoreも全消し（完全クリア）
        /// </summary>
        public Sequence ClearAll()
        {
            _entries.Clear();
            Store.Clear();
            ResetTimelineState();
            return this;
        }

        /// <summary>
        /// タイムライン状態を初期化（内部状態のみ）
        /// アクションは残したいが、次の追加を「最初から」扱いたいときに使う。
        /// </summary>
        public Sequence ResetTimeline()
        {
            ResetTimelineState();
            return this;
        }

        /// <summary>
        /// 既存のエントリを保持したまま、Duration等を再計算して整合性を取り直す。
        /// 例：外部で action.Duration を変えた/Time をいじった後など。
        /// </summary>
        public Sequence RebuildTimeline()
        {
            // いったん内部状態だけ初期化
            ResetTimelineState();

            if (_entries.Count == 0) return this;

            // 既存エントリから group 情報を復元して Duration を再計算
            Duration = 0f;

            // groupStart で並べ、同startは groupId順（安定）
            var ordered = _entries
                .OrderBy(e => e.GroupStart)
                .ThenBy(e => e.GroupId)
                .ToArray();

            // 最後のグループ情報を current として復元
            var last = ordered[^1];
            _currentGroupId = last.GroupId;
            _currentGroupStart = last.GroupStart;
            _groupCounter = ordered.Max(e => e.GroupId);

            // insertionPoint は「Append で進む想定のポイント」なので
            // 最も後ろの “endTime” を採用しておく（安全側）
            float maxEnd = 0f;
            foreach (var e in ordered)
            {
                var end = e.Action.Time + Math.Max(0f, e.Action.Duration);
                if (end > maxEnd) maxEnd = end;
                if (end > Duration) Duration = end;
            }
            _insertionPoint = maxEnd;

            return this;
        }

        private void ResetTimelineState()
        {
            _insertionPoint = 0f;
            _currentGroupId = 0;
            _currentGroupStart = 0f;
            _groupCounter = 0;
            Duration = 0f;
        }

        // -------------------- Build API --------------------

        public Sequence Append(IAction action)
        {
            if (action == null) return this;

            _currentGroupId = ++_groupCounter;
            _currentGroupStart = _insertionPoint;

            action.Time = _currentGroupStart;
            _entries.Add(new Entry(action, _currentGroupId, _currentGroupStart));

            _insertionPoint = Math.Max(_insertionPoint, action.Time + Math.Max(0f, action.Duration));
            RecalcDurationBy(action);

            return this;
        }

        public Sequence Join(IAction action, float offset = 0f)
        {
            if (action == null) return this;

            if (_currentGroupId == 0)
                return Append(action);

            action.Time = Math.Max(0f, _currentGroupStart + offset);
            _entries.Add(new Entry(action, _currentGroupId, _currentGroupStart));
            RecalcDurationBy(action);

            return this;
        }

        public Sequence Insert(float atTime, IAction action)
        {
            if (action == null) return this;

            var t = Math.Max(0f, atTime);

            _currentGroupId = ++_groupCounter;
            _currentGroupStart = t;

            action.Time = t;
            _entries.Add(new Entry(action, _currentGroupId, _currentGroupStart));
            RecalcDurationBy(action);

            return this;
        }

        public Sequence AppendInterval(float seconds)
            => Append(new IntervalAction(seconds));

        public Sequence AppendCallback(Action callback)
            => Append(new CallbackAction(callback));

        // -------------------- Play --------------------

        public async UniTask PlayAsync(
            CancellationToken token = default,
            bool ignoreTimeScale = false,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (_entries.Count == 0) return;

            var groups = _entries
                .GroupBy(e => e.GroupId)
                .Select(g => new Group(
                    g.Key,
                    g.First().GroupStart,
                    g.Select(x => x.Action).ToArray()))
                .OrderBy(g => g.Start)
                .ThenBy(g => g.Id)
                .ToArray();

            foreach (var group in groups)
            {
                token.ThrowIfCancellationRequested();

                var tasks = new UniTask[group.Actions.Length];
                for (int i = 0; i < group.Actions.Length; i++)
                {
                    var a = group.Actions[i];
                    tasks[i] = RunActionInGroupAsync(a, group.Start, token, ignoreTimeScale, timing);
                }

                await UniTask.WhenAll(tasks);
            }
        }

        private async UniTask RunActionInGroupAsync(
            IAction action,
            float groupStart,
            CancellationToken token,
            bool ignoreTimeScale,
            PlayerLoopTiming timing)
        {
            if (action == null) return;

            var delaySec = Math.Max(0f, action.Time - groupStart);
            if (delaySec > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delaySec),
                    ignoreTimeScale: ignoreTimeScale,
                    delayTiming: timing,
                    cancellationToken: token);
            }

            await action.DO(token);
        }

        private void RecalcDurationBy(IAction action)
        {
            var end = action.Time + Math.Max(0f, action.Duration);
            if (end > Duration) Duration = end;
        }

        // -------------------- Internals --------------------

        private readonly struct Entry
        {
            public readonly IAction Action;
            public readonly int GroupId;
            public readonly float GroupStart;

            public Entry(IAction action, int groupId, float groupStart)
            {
                Action = action;
                GroupId = groupId;
                GroupStart = groupStart;
            }
        }

        private readonly struct Group
        {
            public readonly int Id;
            public readonly float Start;
            public readonly IAction[] Actions;

            public Group(int id, float start, IAction[] actions)
            {
                Id = id;
                Start = start;
                Actions = actions;
            }
        }

        private sealed class IntervalAction : IAction
        {
            public float Time { get; set; }
            public float Duration { get; set; }

            public IntervalAction(float seconds)
            {
                Duration = Math.Max(0f, seconds);
            }

            public async UniTask DO(CancellationToken token)
            {
                if (Duration <= 0f) return;

                await UniTask.Delay(
                    TimeSpan.FromSeconds(Duration),
                    cancellationToken: token);
            }
        }

        private sealed class CallbackAction : IAction
        {
            public float Time { get; set; }
            public float Duration { get; set; } = 0f;

            private readonly Action _callback;

            public CallbackAction(Action callback)
            {
                _callback = callback;
            }

            public UniTask DO(CancellationToken token)
            {
                _callback?.Invoke();
                return UniTask.CompletedTask;
            }
        }
    }
}
