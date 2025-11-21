using HighElixir.Timers;
using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HighElixir.Editors.Timers
{
    /// <summary>
    /// Timer.AllTimers からスナップショットを取得して
    /// エディタ上で一覧表示するウォッチャーウィンドウ。
    /// </summary>
    public class TimeInspector : EditorWindow
    {
        /// <summary>
        /// 1行分の表示に必要な情報をまとめたラッパー。
        /// ParentName + TimerSnapshot
        /// </summary>
        private class Wrapper
        {
            public string Parent;
            public TimerTicket Ticket;
            public ITimer Timer;

            public Wrapper(string parent, (TimerTicket ticket, ITimer timer) timer)
            {
                Parent = parent;
                Ticket = timer.ticket;
                Timer = timer.timer;
            }

            public static List<Wrapper> FromSnapshots(string parent, IEnumerable<(TimerTicket ticket, ITimer)> timer)
            {
                var list = new List<Wrapper>();
                foreach (var s in timer)
                    list.Add(new Wrapper(parent, s));
                return list;
            }
        }

        /// <summary>
        /// ソートモード。親名・名前・型・時間などで並び替え。
        /// </summary>
        public enum SortMode
        {
            ParentType,
            Name,
            CountType,
            Current,
            Initialize,
            IsRunning,
            IsFinished
        }

        // エディタ設定 SO
        private TimeEditorOption _cache;

        // スクロール位置
        private Vector2 _scroll;

        // Parent ごとの色（行背景に使用）
        private readonly Dictionary<string, Color> _typeColorMap = new();

        // 検索文字列（Parent / Name / CountType に部分一致）
        private string _searchText = "";

        [MenuItem("Window/HighElixir/TimeInspector")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TimeInspector), false, "TimeInspector");
        }

        /// <summary>
        /// 1行分の表示。
        /// </summary>
        private void Print(
            string parent,
            string name,
            string countType,
            float normalized,
            string current,
            string init,
            string running,
            string finished,
            bool isUp,
            string option = "",
            int optionWidth = 100,
            Color color = default,
            ITimer timer = null)
        {
            // 1行分の Rect（背景塗り用）
            using (var scope = new EditorGUILayout.HorizontalScope())
            {
                // 親ごとの色で背景を塗る（default(Color) は全要素 0）
                if (color != default)
                    EditorGUI.DrawRect(scope.rect, color);

                // Parent
                EditorGUILayout.LabelField(parent, GUILayout.Width(120));

                // Name
                EditorGUILayout.LabelField(name, GUILayout.Width(120));

                // CountType
                EditorGUILayout.LabelField(countType, GUILayout.Width(140));

                // 進捗バー
                var text = isUp ? $"{current}" : $"{current} / {init}";
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Width(220)),
                    Mathf.Clamp01(normalized),
                    text);

                // 実行中フラグ（▶ / ■）
                var size = GUILayout.Width(75);
                if (timer != null)
                {
                    if (EditorGUILayout.LinkButton(running, size))
                    {
                        if (timer.IsRunning)
                            timer.Stop();
                        else
                            timer.Start();
                    }
                }
                else
                    EditorGUILayout.LabelField(running, size);

                // 完了フラグ（✔）
                EditorGUILayout.LabelField(finished, GUILayout.Width(50));

                // option が空じゃなければ表示
                if (!string.IsNullOrEmpty(option))
                    EditorGUILayout.LabelField(option, GUILayout.Width(optionWidth));
            }
        }

        private void OnGUI()
        {
            if (_cache == null)
                LoadOrCreate();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            #region ソート＋検索バー
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // SortMode 切り替え（Enum をドロップダウン風に）
                _cache.SortMode = (SortMode)EditorGUILayout.EnumPopup(
                    _cache.SortMode,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(140)
                );

                // 昇順／降順ボタン
                var ascIcon = _cache.SortAscending
                    ? EditorGUIUtility.IconContent("AlphabeticalSorting")
                    : EditorGUIUtility.IconContent("AlphabeticalSorting");

                ascIcon.text = _cache.SortAscending ? " Asc" : " Desc";

                _cache.SortAscending = GUILayout.Toggle(
                    _cache.SortAscending,
                    ascIcon,
                    EditorStyles.toolbarButton,
                    GUILayout.Width(80));

                GUILayout.FlexibleSpace();

                if (_cache.EnableCustomColor =
                    GUILayout.Toggle(
                        _cache.EnableCustomColor,
                        "Custom Color",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(120)))
                {
                    ColorEditor.Utility(_cache);
                }

                var searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField")
                                  ?? GUI.skin.FindStyle("ToolbarSearchTextField")
                                  ?? EditorStyles.toolbarTextField;

                var cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton")
                                  ?? GUI.skin.FindStyle("ToolbarSearchCancelButton")
                                  ?? EditorStyles.toolbarButton;

                _searchText = GUILayout.TextField(
                    _searchText ?? "",
                    searchStyle,
                    GUILayout.Width(220));

                if (GUILayout.Button(GUIContent.none, cancelStyle))
                {
                    _searchText = string.Empty;
                    GUI.FocusControl(null); // フォーカス外して見た目もクリア
                }
            }
            #endregion

            // ヘッダー行
            Print(
                "Parent",
                "Name",
                "CountType",
                1f,
                "Current",
                "Init",
                "State",
                "Complete",
                true,
                "Option",
                100);

            // プレイモード限定
            if (Application.isPlaying)
            {

                int totalCommands = 0;
                var timers = new List<Wrapper>();

                // すべての Timer から Snapshot を収集
                foreach (var rTimer in TimerManager.Timers)
                {
                    totalCommands += rTimer.CommandCount;
                    timers.AddRange(Wrapper.FromSnapshots(rTimer.ParentName, rTimer.GetTimers()));
                }

                // 検索フィルタ（Parent / Name / CountType 部分一致）
                if (!string.IsNullOrEmpty(_searchText))
                {
                    var q = _searchText.ToLowerInvariant();

                    timers = timers.FindAll(t =>
                    {
                        var parent = (t.Parent ?? "").ToLowerInvariant();
                        var name = (t.Ticket.Name ?? "").ToLowerInvariant();
                        var countType = (t.Timer.GetType().Name ?? "").ToLowerInvariant();

                        return parent.Contains(q)
                               || name.Contains(q)
                               || countType.Contains(q);
                    });
                }

                // ソート
                timers.Sort((a, b) =>
                {
                    int cmp = 0;
                    switch (_cache.SortMode)
                    {
                        case SortMode.ParentType:
                            cmp = string.Compare(a.Parent, b.Parent, StringComparison.Ordinal);
                            break;
                        case SortMode.Name:
                            cmp = string.Compare(a.Ticket.Name, b.Ticket.Name, StringComparison.Ordinal);
                            break;
                        case SortMode.CountType:
                            cmp = a.Ticket.GetType().Name.CompareTo(b.Timer.GetType().Name);
                            break;
                        case SortMode.Current:
                            cmp = a.Timer.Current.CompareTo(b.Timer.Current);
                            break;
                        case SortMode.Initialize:
                            cmp = a.Timer.InitialTime.CompareTo(b.Timer.InitialTime);
                            break;
                        case SortMode.IsRunning:
                            cmp = a.Timer.IsRunning.CompareTo(b.Timer.IsRunning);
                            break;
                        case SortMode.IsFinished:
                            cmp = a.Timer.IsFinished.CompareTo(b.Timer.IsFinished);
                            break;
                    }
                    return _cache.SortAscending ? cmp : -cmp;
                });

                // 各タイマー行の描画
                foreach (var timer in timers)
                {
                    // 親ごとに色を割り当て
                    Color parentColor;

                    if (_cache.EnableCustomColor && _cache.ParentColor != null)
                    {
                        // カスタムカラーON ＋ データあり
                        ColorEditor.Utility(_cache);

                        if (_cache.ParentColor.TryGet(timer.Parent, out var col))
                        {
                            // 既に登録されてる色を使う
                            parentColor = col.Color;
                        }
                        else
                        {
                            // なければ新規作成して登録
                            parentColor = UnityEngine.Random.ColorHSV(0.6f, 1f, 0.6f, 1f, 0.6f, 1f);
                            _cache.ParentColor.ColorPairs.Add(new ColorPair
                            {
                                ParentName = timer.Parent,
                                Color = parentColor,
                            });
                            // 必要ならここで SetDirty とか
                            // EditorUtility.SetDirty(_cache.ParentColor);
                        }
                    }
                    else
                    {
                        // カスタムカラー無効時は、その場で Dictionary 管理
                        if (!_typeColorMap.TryGetValue(timer.Parent, out parentColor))
                        {
                            parentColor = UnityEngine.Random.ColorHSV(0.6f, 1f, 0.6f, 1f, 0.6f, 1f);
                            _typeColorMap[timer.Parent] = parentColor;
                        }
                    }


                    var tp = timer.Timer.GetType();

                    // Normalize できない（INormalizeable でない）ものはカウントアップ扱い
                    bool isUp = tp is not INormalizeable;

                    string tmp = tp is ITick ? " (Tick)" : "";
                    string postfix1 = isUp ? tmp : "";
                    string postfix2 = isUp ? "" : tmp;

                    // TimeEditorOption からオプション文字列を取得
                    _cache.TryGetText(tp, timer.Timer.ArgTime, out var option);

                    Print(
                        timer.Parent,
                        timer.Ticket.Name,
                        timer.Timer.GetType().Name,
                        timer.Timer is INormalizeable normalizeable ? normalizeable.NormalizedElapsed : 1f,
                        $"{timer.Timer.Current:0.00}" + postfix1,
                        $"{timer.Timer.InitialTime:0.00}" + postfix2,
                        timer.Timer.IsRunning ? "▶" : "■",
                        timer.Timer.IsFinished ? "✔" : "",
                        isUp,
                        option,
                        160,
                        parentColor,
                        timer.Timer
                    );
                }

                EditorGUILayout.LabelField($"LazyCommands : {totalCommands}");
            }
            else
            {
                GUILayout.Label("Enter Play Mode to view timers.", EditorStyles.boldLabel);
            }

            EditorGUILayout.EndScrollView(); //ここ
        }

        private void OnInspectorUpdate()
        {
            // 定期的に Repaint して表示更新
            Repaint();
        }

        /// <summary>
        /// エディタ用設定 SO をロードまたは新規作成。
        /// </summary>
        private void LoadOrCreate()
        {
            _cache = AssetDatabase.LoadAssetAtPath<TimeEditorOption>(TimeEditorOption.CreatePath);

            if (_cache == null)
            {
                _cache = ScriptableObject.CreateInstance<TimeEditorOption>();
                AssetDatabase.CreateAsset(_cache, TimeEditorOption.CreatePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
