using UnityEditor;
using UnityEngine;

namespace HighElixir.Editor
{
    public class TimerWatcher : EditorWindow
    {
        private Vector2 _scroll;
        private Color _currentColor = Color.clear;
        private System.Type _lastParentType = null;

        [MenuItem("HighElixir/Timer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TimerWatcher));
        }

        private void Print(string one, string two, string three, float four, string five, string six, bool seven, bool isUp, Color color = default)
        {
            Rect rect = EditorGUILayout.BeginHorizontal();

            // 背景を塗る
            if (color != default)
                EditorGUI.DrawRect(rect, color);

            // 行の内容
            EditorGUILayout.LabelField(one, GUILayout.Width(120));
            EditorGUILayout.LabelField(two, GUILayout.Width(100));
            EditorGUILayout.LabelField(three, GUILayout.Width(120));
            var text = $"{five:0.00}/{six:0.00}";
            if (isUp)
            {
                four = 1f;
                text = $"{five:0.00}";
            }
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Width(200)),
                four,
                text
            );
            EditorGUILayout.LabelField(seven ? "▶" : "■", GUILayout.Width(30));

            EditorGUILayout.EndHorizontal();

        }
        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                Print(
                    "ParentType",
                    "ID",
                    "Timer",
                    1f,
                   "Current",
                    "",
                    true,
                    true);
                foreach (var timer in HighElixir.Timers.Timer.AllTimers)
                {
                    // ParentType が変わったときだけ色を切り替え
                    if (_lastParentType != timer.ParentType)
                    {
                        Color newColor;
                        do
                        {
                            newColor = Random.ColorHSV(0f, 1f, 0.4f, 0.8f, 0.7f, 1f);
                        } while (newColor == _currentColor);

                        _currentColor = newColor;
                        _lastParentType = timer.ParentType;
                    }

                    foreach (var snap in timer.GetSnapshot())
                    {
                        bool isUp = snap.TimerClass.Contains("CountUp");
                        Print(
                            timer.ParentType.Name,
                            snap.Id,
                            snap.TimerClass,
                            snap.NormalizedElapsed,
                            $"{snap.Current:0.00}",
                            $"{snap.Initialize:0.00}",
                            snap.IsRunning,
                            isUp,
                            _currentColor);
                    }
                    Print("LastCommandCount:", $"{timer.CommandCount}", "", 1f, "", "", true, true, _currentColor);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Enter Play Mode to view timers.", EditorStyles.boldLabel);
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
