using HighElixir.UI;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(OpenableUI))]
public class OpenableUI_Editor : Editor
{
    private SerializedProperty _hiddenPos;
    private SerializedProperty _targetPos;
    private SerializedProperty _openUI;
    private void OnEnable()
    {
        _hiddenPos = serializedObject.FindProperty("_hiddenPos");
        _targetPos = serializedObject.FindProperty("_targetPos");
        _openUI = serializedObject.FindProperty("_open");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (GUILayout.Button("現在の位置を_hiddenPosに設定"))
        {
            _hiddenPos.vector3Value = ((RectTransform)_openUI.exposedReferenceValue).transform.localPosition;
            serializedObject.ApplyModifiedProperties();
        }
        if (GUILayout.Button("現在の位置を_targetPosに設定"))
        {
            _targetPos.vector3Value = ((RectTransform)_openUI.exposedReferenceValue).transform.localPosition;
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.Space(2);
        if (GUILayout.Button("開閉する"))
        {
            ((OpenableUI)target).Switch();
        }
    }
}
