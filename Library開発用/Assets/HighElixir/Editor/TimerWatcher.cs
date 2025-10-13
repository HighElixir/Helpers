using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using HighElixir.Timers;

namespace HighElixir.Editors
{
    public class TimerWatcher : EditorWindow
    {
        private class Wrapper
        {
            public string Parent;
            public TimerSnapshot Snapshot;

            public Wrapper(string parent, TimerSnapshot snapshot)
            {
                Parent = parent;
                Snapshot = snapshot;
            }

            public static List<Wrapper> FromSnapshots(string parent, IEnumerable<TimerSnapshot> snapshots)
            {
                var list = new List<Wrapper>();
                foreach (var s in snapshots)
                    list.Add(new Wrapper(parent, s));
                return list;
            }
        }

        private enum SortMode
        {
            ParentType,
            Name,
            CountType,
            Current,
            Initialize,
            IsRunning,
            IsFinished
        }

        private SortMode _sortMode = SortMode.ParentType;
        private bool _sortAscending = true;
        private Vector2 _scroll;
        private Color _currentColor = Color.clear;
        private string _lastParentType = null;

        private readonly Dictionary<string, Color> _typeColorMap = new();
        private string _editingColorParent = null; // ★ 編集中の親タイプ
        private Color _editingColor;               // ★ カラーピッカーで選択中の色

        [MenuItem("HighElixir/Timer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TimerWatcher), false, "Timer Watcher");
        }

        private void Print(string parent, string name, string countType, float normalized, string current, string init,
                           bool running, bool finished, bool isUp, string option = "", int optionWidth = 100, Color color = default)
        {
            Rect rect = EditorGUILayout.BeginHorizontal();

            if (color != default)
                EditorGUI.DrawRect(rect, color);

            // ★ Parentリンクボタン
            if (EditorGUILayout.LinkButton(parent, GUILayout.Width(120)))
            {
                _editingColorParent = parent;
                _editingColor = _typeColorMap.ContainsKey(parent) ? _typeColorMap[parent] : Color.white;
            }

            EditorGUILayout.LabelField(name, GUILayout.Width(100));
            EditorGUILayout.LabelField(countType, GUILayout.Width(120));

            var text = isUp ? $"{current}" : $"{current} / {init}";
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Width(200)), normalized, text);

            EditorGUILayout.LabelField(running ? "▶" : "■", GUILayout.Width(30));
            EditorGUILayout.LabelField(finished ? "✔" : "", GUILayout.Width(30));

            if (!string.IsNullOrEmpty(current))
                EditorGUILayout.LabelField(option, GUILayout.Width(optionWidth));

            EditorGUILayout.EndHorizontal();
        }

        private void OnGUI()
        {
            // ★ カラーモーダル（最上部に表示）
            if (!string.IsNullOrEmpty(_editingColorParent))
            {
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label($"Change color for: {_editingColorParent}", EditorStyles.boldLabel);

                var newColor = EditorGUILayout.ColorField("Color", _editingColor);
                if (newColor != _editingColor)
                {
                    _editingColor = newColor;
                    _typeColorMap[_editingColorParent] = newColor;
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply"))
                {
                    _typeColorMap[_editingColorParent] = _editingColor;
                    _editingColorParent = null;
                }
                if (GUILayout.Button("Cancel"))
                {
                    _editingColorParent = null;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (Application.isPlaying)
            {
                // ソートモード選択
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SortMode:", GUILayout.Width(70));
                if (GUILayout.Button(_sortMode.ToString(), GUILayout.Width(120)))
                {
                    _sortMode++;
                    if (!Enum.IsDefined(typeof(SortMode), _sortMode))
                        _sortMode = SortMode.ParentType;
                }

                var ascText = _sortAscending ? "Ascending" : "Descending";
                if (GUILayout.Button(ascText, GUILayout.Width(120)))
                    _sortAscending = !_sortAscending;

                EditorGUILayout.EndHorizontal();

                // ヘッダー
                Print("Parent", "Name", "CountType", 1f, "Current", "Init", true, false, true);

                int totalCommands = 0;
                var timers = new List<Wrapper>();
                foreach (var rTimer in Timer.AllTimers)
                {
                    totalCommands += rTimer.CommandCount;
                    timers.AddRange(Wrapper.FromSnapshots(rTimer.ParentName, rTimer.GetSnapshot()));
                }

                // ソート
                timers.Sort((a, b) =>
                {
                    int cmp = 0;
                    switch (_sortMode)
                    {
                        case SortMode.ParentType: cmp = string.Compare(a.Parent, b.Parent, StringComparison.Ordinal); break;
                        case SortMode.Name: cmp = string.Compare(a.Snapshot.Name, b.Snapshot.Name, StringComparison.Ordinal); break;
                        case SortMode.CountType: cmp = a.Snapshot.CountType.CompareTo(b.Snapshot.CountType); break;
                        case SortMode.Current: cmp = a.Snapshot.Current.CompareTo(b.Snapshot.Current); break;
                        case SortMode.Initialize: cmp = a.Snapshot.Initialize.CompareTo(b.Snapshot.Initialize); break;
                        case SortMode.IsRunning: cmp = a.Snapshot.IsRunning.CompareTo(b.Snapshot.IsRunning); break;
                        case SortMode.IsFinished: cmp = a.Snapshot.IsFinished.CompareTo(b.Snapshot.IsFinished); break;
                    }
                    return _sortAscending ? cmp : -cmp;
                });

                foreach (var timer in timers)
                {
                    if (!_typeColorMap.TryGetValue(timer.Parent, out var parentColor))
                    {
                        parentColor = UnityEngine.Random.ColorHSV(0.6f, 1f, 0.6f, 1f, 0.6f, 1f);
                        _typeColorMap[timer.Parent] = parentColor;
                    }

                    var tp = timer.Snapshot.CountType;
                    bool isUp = tp.Has(CountType.CountUp);
                    string tmp = tp.Has(CountType.Tick) ? " (Tick)" : "";
                    string postfix1 = isUp ? tmp : "";
                    string postfix2 = isUp ? "" : tmp;
                    string option = tp.Has(CountType.Pulse) ? $"PulseCount : {timer.Snapshot.Optional}" : "";

                    Print(
                        timer.Snapshot.ParentName ?? timer.Parent,
                        timer.Snapshot.Name,
                        timer.Snapshot.CountType.ToString(),
                        timer.Snapshot.NormalizedElapsed,
                        $"{timer.Snapshot.Current:0.00}" + postfix1,
                        $"{timer.Snapshot.Initialize:0.00}" + postfix2,
                        timer.Snapshot.IsRunning,
                        timer.Snapshot.IsFinished,
                        isUp,
                        option,
                        100,
                        parentColor
                    );
                }

                EditorGUILayout.LabelField($"LazeCommands : {totalCommands}");
            }
            else
            {
                GUILayout.Label("Enter Play Mode to view timers.", EditorStyles.boldLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
