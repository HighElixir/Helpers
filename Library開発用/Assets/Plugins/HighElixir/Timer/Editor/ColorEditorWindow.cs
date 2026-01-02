using UnityEditor;
using UnityEngine;

namespace HighElixir.Editors.Timers
{
    public class ColorEditorWindow : EditorWindow
    {
        public TimeEditorOption Option { get; set; }

        private Editor _parentColorEditor;

        private void OnGUI()
        {
            if (Option == null)
            {
                EditorGUILayout.HelpBox("TimeEditorOption が割り当てられていません。", MessageType.Warning);
                return;
            }

            // トグルを取得してから Option に代入する（代入を if の中で行わない）
            var enabled = GUILayout.Toggle(
                Option.EnableCustomColor,
                "EnableCustomColor",
                EditorStyles.toolbarButton,
                GUILayout.Width(120));
            Option.EnableCustomColor = enabled;

            if (Option.EnableCustomColor)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Color Settings", EditorStyles.boldLabel);

                // ParentColor アセットを選択させる
                Option.ParentColor = (ColorData)EditorGUILayout.ObjectField("Parent Color", Option.ParentColor, typeof(ColorData), false);

                // 任意で保存パスなどを編集できるようにする
                Option.ColorDataPath = EditorGUILayout.TextField("Color Data Path", Option.ColorDataPath);

                // ParentColor のインスペクタを埋め込んでカラーリスト等を直接編集できるようにする
                if (Option.ParentColor != null)
                {
                    Editor.CreateCachedEditor(Option.ParentColor, null, ref _parentColorEditor);
                    if (_parentColorEditor != null)
                    {
                        EditorGUILayout.Space();
                        _parentColorEditor.OnInspectorGUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("ParentColor アセットを割り当ててください。", MessageType.Info);
                }
            }
        }

        public static void ShowWindow(TimeEditorOption op)
        {
            GetWindow<ColorEditorWindow>("Color Editor").Option = op;
        }
    }
}