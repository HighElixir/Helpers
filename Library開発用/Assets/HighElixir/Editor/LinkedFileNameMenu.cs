using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.IO;

namespace HighElixir.Editors
{
    public static class LinkedFileNameMenu
    {
        [MenuItem("HighElixir/RenameAssets")]
        public static void Rename()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                var type = obj.GetType();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (System.Attribute.IsDefined(field, typeof(LinkedFileNameAttribute)))
                    {
                        var value = field.GetValue(obj) as string;
                        if (string.IsNullOrEmpty(value))
                        {
                            Debug.LogWarning($"⚠ {obj.name} の {field.Name} が空です。スキップします。");
                            continue;
                        }

                        string currentName = Path.GetFileNameWithoutExtension(path);
                        string set = char.ToUpper(value[0]) + value.Substring(1);
                        if (currentName != value)
                        {
                            string error = AssetDatabase.RenameAsset(path, set);
                            if (string.IsNullOrEmpty(error))
                                Debug.Log($"✅ {currentName} → {set} にリネームしました！");
                            else
                                Debug.LogError($"❌ {obj.name} のリネームに失敗: {error}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
