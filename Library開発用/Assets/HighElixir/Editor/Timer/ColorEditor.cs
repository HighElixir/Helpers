using UnityEditor;
using UnityEngine;

namespace HighElixir.Editors.Timers
{
    public static class ColorEditor
    {
        public static void Utility(TimeEditorOption option)
        {
            if (option.ParentColor != null) return;
            if (string.IsNullOrEmpty(option.ColorDataPath))
            {
                var filePath = EditorUtility.SaveFilePanelInProject(
                                "Save",
                                "ColorData",
                                "asset",
                                 "Select save location for ColorData"
                                 );
                if (!string.IsNullOrEmpty(filePath))
                {
                    option.ColorDataPath = filePath;
                    LoadOrCreate(option);
                }
            }
            else
            {
                LoadOrCreate(option);
            }
        }

        private static void LoadOrCreate(TimeEditorOption option)
        {
            option.ParentColor = AssetDatabase.LoadAssetAtPath<ParentColor>(option.ColorDataPath);

            if (option.ParentColor == null)
            {
                option.ParentColor = ScriptableObject.CreateInstance<ParentColor>();
                AssetDatabase.CreateAsset(option.ParentColor, option.ColorDataPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}