using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Neymanoff.HexGrid.Unity;

namespace Neymanoff.HexGrid.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="FormationPatternSO"/> enabling one-click Tilemap baking
    /// and quick standard preset application.
    /// </summary>
    [CustomEditor(typeof(FormationPatternSO))]
    public class FormationPatternSOEditor : UnityEditor.Editor
    {
        private Tilemap _bakingSourceTilemap;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var pattern = (FormationPatternSO)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Tilemap Authoring (WYSIWYG)", EditorStyles.boldLabel);

            _bakingSourceTilemap = (Tilemap)EditorGUILayout.ObjectField("Source Tilemap", _bakingSourceTilemap, typeof(Tilemap), true);

            GUI.enabled = _bakingSourceTilemap != null;
            if (GUILayout.Button("Bake Slots from Tilemap", GUILayout.Height(28)))
            {
                Undo.RecordObject(pattern, "Bake Formation from Tilemap");
                pattern.BakeFromTilemap(_bakingSourceTilemap);
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Standard Tactical Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("2-3 Standard"))
            {
                Undo.RecordObject(pattern, "Apply 2-3 Preset");
                var preset = FormationPatternSO.CreatePreset2_3();
                pattern.SetSlots(preset.GetRelativeSlots(), preset.AlignmentMode);
                DestroyImmediate(preset);
            }
            if (GUILayout.Button("3-2 Vanguard"))
            {
                Undo.RecordObject(pattern, "Apply 3-2 Preset");
                var preset = FormationPatternSO.CreatePreset3_2();
                pattern.SetSlots(preset.GetRelativeSlots(), preset.AlignmentMode);
                DestroyImmediate(preset);
            }
            if (GUILayout.Button("1-2-1 Diamond"))
            {
                Undo.RecordObject(pattern, "Apply 1-2-1 Preset");
                var preset = FormationPatternSO.CreatePreset1_2_1();
                pattern.SetSlots(preset.GetRelativeSlots(), preset.AlignmentMode);
                DestroyImmediate(preset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Line (4) Defense"))
            {
                Undo.RecordObject(pattern, "Apply Line Preset");
                var preset = FormationPatternSO.CreatePresetLine(4);
                pattern.SetSlots(preset.GetRelativeSlots(), preset.AlignmentMode);
                DestroyImmediate(preset);
            }
            if (GUILayout.Button("Wedge Spearhead"))
            {
                Undo.RecordObject(pattern, "Apply Wedge Preset");
                var preset = FormationPatternSO.CreatePresetWedge();
                pattern.SetSlots(preset.GetRelativeSlots(), preset.AlignmentMode);
                DestroyImmediate(preset);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
