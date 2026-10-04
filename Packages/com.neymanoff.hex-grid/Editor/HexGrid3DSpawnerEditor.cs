using UnityEditor;
using UnityEngine;
using Neymanoff.HexGrid.Unity;

namespace Neymanoff.HexGrid.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="HexGrid3DSpawner"/> providing convenient spawn/clear buttons
    /// and contextual status feedback.
    /// </summary>
    [CustomEditor(typeof(HexGrid3DSpawner))]
    public class HexGrid3DSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var spawner = (HexGrid3DSpawner)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3D Spawner Status & Actions", EditorStyles.boldLabel);

            if (spawner.SourceTilemaps == null || spawner.SourceTilemaps.Count == 0)
            {
                EditorGUILayout.HelpBox("Source Tilemaps list is empty. Clicking 'Spawn 3D Grid' will automatically wire Walkable and Obstacle Tilemaps from the Bridge.", MessageType.Info);
            }

            if (spawner.DefaultHexPrefab == null)
            {
                if (spawner.GenerateProceduralHexForUnmapped)
                {
                    EditorGUILayout.HelpBox("Default Hex Prefab is not assigned: unmapped tiles will generate procedural 3D Pointy-Top Greybox hex prisms.", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Unmapped tiles without prefabs will be skipped (2D artwork & contour lines remain visible). Only mapped tiles will spawn 3D objects.", MessageType.None);
                }
            }

            if (spawner.SpawnedInstances != null && spawner.SpawnedInstances.Count > 0)
            {
                string visibilityNote = spawner.HideTilemapsOnSpawn ? " (2D Tilemaps are currently hidden)." : " (2D Tilemaps remain visible).";
                EditorGUILayout.HelpBox($"Active 3D Instances: {spawner.SpawnedInstances.Count}{visibilityNote}", MessageType.Info);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Spawn 3D Grid", GUILayout.Height(32)))
            {
                Undo.RecordObject(spawner, "Spawn 3D Grid");
                spawner.SpawnGrid();
                EditorUtility.SetDirty(spawner);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear 3D Grid", GUILayout.Height(32)))
            {
                Undo.RecordObject(spawner, "Clear 3D Grid");
                spawner.ClearGrid();
                EditorUtility.SetDirty(spawner);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    }
}
