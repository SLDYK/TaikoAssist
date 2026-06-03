using UnityEditor;
using UnityEngine;

namespace TaikoAssist
{
    [CustomEditor(typeof(Separator))]
    public class SeparatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Separator Separator = (Separator)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Blend To State 1"))
            {
                Separator.BlendToWithSlider(0f);
            }

            if (GUILayout.Button("Blend To State 2"))
            {
                Separator.BlendToWithSlider(1f);
            }
        }
    }
}
