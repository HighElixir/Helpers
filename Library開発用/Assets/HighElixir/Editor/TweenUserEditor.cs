using UnityEditor;

namespace HighElixir.Editors
{
    public class TweenUserEditor : Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
        {
            var element = base.CreateInspectorGUI();

            return element;
        }
    }
}