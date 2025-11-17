#if UNITY_EDITOR
using HighElixir.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HighElixir.Editors
{
    [CustomPropertyDrawer(typeof(SerializeReferenceSelectorAttribute))]
    public class SerializeReferenceSelectorDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);

            // 宣言側の型（List<…> の要素型＝ISearchComponent<closed>）を解決
            var declaredType = GetDeclaredManagedRefType(property);
            if (declaredType == null)
            {
                EditorGUI.HelpBox(position, "SerializeReference の宣言型を特定できなかったよ…", MessageType.Warning);
                return;
            }

            // インターフェイス（構築済み？）かどうか
            var targetInterface = declaredType;

            // 候補列挙：internal/nested も含め、[Serializable] かつクラスで、UnityEngine.Object でない、引数なしCtor OK
            var candidates = GetAssignableConcreteTypes(targetInterface)
                .Where(t =>
                    t.IsClass && !t.IsAbstract && !t.IsGenericType &&
                    (t.IsPublic || t.IsNestedPublic) &&
                    t.GetCustomAttribute<SerializableAttribute>() != null &&
                    !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
                    HasDefaultCtor(t))
                .OrderBy(t => t.FullName)
                .ToList();

            // 表示名
            var display = new List<string> { "None" };
            display.AddRange(candidates.Select(PrettyName));

            // 現在値 → インデックス
            var currentType = property.managedReferenceValue?.GetType();
            var currentIndex = 0;
            if (currentType != null)
            {
                var idx = candidates.FindIndex(t => t == currentType);
                if (idx >= 0) currentIndex = idx + 1;
            }

            // 左：型選択、右：プロパティ本体
            var left = new Rect(position.x, position.y, 220, EditorGUIUtility.singleLineHeight);
            var right = new Rect(position.x + 225, position.y, position.width - 225, position.height);

            var nextIndex = EditorGUI.Popup(left, currentIndex, display.ToArray());
            if (nextIndex != currentIndex)
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Change ManagedReference Type");
                if (nextIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    var newType = candidates[nextIndex - 1];
                    property.managedReferenceValue = Activator.CreateInstance(newType);
                }
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.PropertyField(right, property, GUIContent.none, true);
        }

        // ==== Helpers ====

        // List<T> / T[] / そのまま のいずれでも、ManagedReference の「要素型/宣言型」を返す
        static Type GetDeclaredManagedRefType(SerializedProperty prop)
        {
            // 例: "AssemblyName Namespace.OuterType`2+InnerInterface[[Cont],[Target]]"
            var fieldType = GetFieldType(prop.managedReferenceFieldTypename);
            if (fieldType == null) return null;

            // List<Declared> の場合は要素型を取り直す
            if (fieldType.IsGenericType &&
                fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                fieldType = fieldType.GetGenericArguments()[0];
            }
            return fieldType;
        }

        static Type GetFieldType(string managedReferenceFieldTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFieldTypename)) return null;
            var parts = managedReferenceFieldTypename.Split(' ');
            if (parts.Length != 2) return null;
            // "AsmName Full.Type+Nested" → Type.GetType("Full.Type+Nested, AsmName")
            return Type.GetType($"{parts[1]}, {parts[0]}");
        }

        static bool HasDefaultCtor(Type t)
            => t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;

        // targetInterface が「構築済みジェネリックの内部インターフェイス」の場合でも合致判定できるようにする
        static IEnumerable<Type> GetAssignableConcreteTypes(Type targetInterface)
        {
            // すべての型から探す（TypeCache だと open generic で止まるケースがあるため）
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(x => x != null).ToArray(); }

                foreach (var t in types)
                {
                    if (t == null) continue;
                    if (!t.IsClass) continue;

                    // 直接/間接の実装インターフェイスに targetInterface と等価なものがあるか
                    if (ImplementsClosedInterface(t, targetInterface))
                        yield return t;
                }
            }
        }

        static bool ImplementsClosedInterface(Type type, Type targetInterface)
        {
            if (!targetInterface.IsInterface) return false;

            foreach (var it in type.GetInterfaces())
            {
                // 完全一致
                if (it == targetInterface) return true;

                // ジェネリック定義が同じ & 型引数まで一致（SearchSystem<,>.ISearchComponent など）
                if (it.IsGenericType && targetInterface.IsGenericType)
                {
                    var a = it.GetGenericTypeDefinition();
                    var b = targetInterface.GetGenericTypeDefinition();
                    if (a == b)
                    {
                        var aa = it.GenericTypeArguments;
                        var bb = targetInterface.GenericTypeArguments;
                        if (aa.Length == bb.Length && aa.SequenceEqual(bb))
                            return true;
                    }
                }
            }
            return false;
        }

        static string PrettyName(Type t)
            => (t.FullName ?? t.Name).Replace('+', '.');
    }
}
#endif
