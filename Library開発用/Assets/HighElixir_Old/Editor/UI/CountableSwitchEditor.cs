using UnityEditor;
using UnityEngine;

namespace HighElixir.Unity.UI.Countable.Editor
{
    [CustomEditor(typeof(CountableSwitch))]
    public class CountableSwitchEditor : UnityEditor.Editor
    {
        // SerializedProperty のキャッシュ
        private SerializedProperty _minusProp;
        private SerializedProperty _plusProp;
        private SerializedProperty _textProp;

        private SerializedProperty _defaultAmountProp;
        private SerializedProperty _stepProp;
        private SerializedProperty _minProp;
        private SerializedProperty _maxProp;

        private SerializedProperty _onValueChangedProp;

        private void OnEnable()
        {
            var so = serializedObject;
            _minusProp = so.FindProperty("_minus");
            _plusProp = so.FindProperty("_plus");
            _textProp = so.FindProperty("_text");

            _defaultAmountProp = so.FindProperty("_defaultAmount");
            _stepProp = so.FindProperty("_step");
            _minProp = so.FindProperty("min");
            _maxProp = so.FindProperty("max");

            _onValueChangedProp = so.FindProperty("_onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_minusProp);
            EditorGUILayout.PropertyField(_plusProp);
            EditorGUILayout.PropertyField(_textProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_defaultAmountProp, new GUIContent("Default Amount"));
            EditorGUILayout.PropertyField(_stepProp, new GUIContent("Step"));
            EditorGUILayout.PropertyField(_minProp, new GUIContent("Min Value"));
            EditorGUILayout.PropertyField(_maxProp, new GUIContent("Max Value"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_onValueChangedProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
