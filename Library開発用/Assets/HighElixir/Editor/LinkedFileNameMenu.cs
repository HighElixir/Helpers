using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HighElixir.Editors
{
    public static class LinkedFileNameMenu
    {
        [MenuItem("HighElixir/RenameAssets")]
        public static void Rename()
        {
            int succesed = 0, failed = 0;
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
                            failed++;
                            continue;
                        }

                        string currentName = Path.GetFileNameWithoutExtension(path);
                        string set = char.ToUpper(value[0]) + value.Substring(1);
                        if (currentName != value)
                        {
                            string error = AssetDatabase.RenameAsset(path, set);
                            if (string.IsNullOrEmpty(error))
                            {
                                Debug.Log($"✅ {currentName} → {set} にリネームしました！");
                                succesed++;
                            }
                            else
                            {
                                Debug.LogError($"❌ {obj.name} のリネームに失敗: {error}");
                                failed++;
                            }
                        }
                    }
                }
                Debug.Log($"[FileRename] 成功:{succesed} 失敗:{failed}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
