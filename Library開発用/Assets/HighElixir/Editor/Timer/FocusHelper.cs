using UnityEditor;
using UnityEngine;

public static class FocusHelper
{
    [MenuItem("Window/HighElixir/TimeEditorOption")]
    public static void FocusTimeEditorOption()
    {
        const string path = "Assets/HighElixir/Editor/Timer/EditorData.asset";

        // 1. アセットをロード
        var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (obj == null)
        {
            Debug.LogWarning($"Asset not found: {path}");
            return;
        }

        // 2. Project ウィンドウを手前に
        EditorUtility.FocusProjectWindow();

        // 3. 選択状態にする
        Selection.activeObject = obj;

        // 4. 黄色いハイライト＆スクロールして見える位置に
        EditorGUIUtility.PingObject(obj);
    }
}
