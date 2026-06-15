using UnityEditor;
using UnityEngine;

namespace TaikoAssist
{
    [CustomEditor(typeof(ChartLoader))]
    public class ChartLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ChartLoader loader = (ChartLoader)target;

            EditorGUILayout.Space(8);

            if (GUILayout.Button("加载测试谱面", GUILayout.Height(30)))
            {
                loader.LoadAll();
            }
        }
    }
}
