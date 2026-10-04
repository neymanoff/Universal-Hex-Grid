using UnityEditor;
using UnityEngine;
using Neymanoff.HexGrid.Unity;

namespace Neymanoff.HexGrid.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="HexGridGenerator"/> providing one-click generation and clearing buttons.
    /// </summary>
    [CustomEditor(typeof(HexGridGenerator))]
    public class HexGridGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Actions", EditorStyles.boldLabel);

            var generator = (HexGridGenerator)target;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Generate Grid", GUILayout.Height(32)))
            {
                generator.GenerateGrid();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear Grid", GUILayout.Height(32)))
            {
                generator.ClearGrid();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    }
}
