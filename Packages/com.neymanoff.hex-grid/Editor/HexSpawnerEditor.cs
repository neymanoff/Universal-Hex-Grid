using UnityEditor;
using UnityEngine;
using Neymanoff.HexGrid.Unity;

namespace Neymanoff.HexGrid.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="HexSpawnerBase"/>, <see cref="HexSpawnPoint"/> and <see cref="HexFormationAnchor"/>.
    /// Provides one-click snapping to cell centers and slot inspection.
    /// </summary>
    [CustomEditor(typeof(HexSpawnerBase), true)]
    [CanEditMultipleObjects]
    public class HexSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var spawner = (HexSpawnerBase)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Placement Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Snap to Nearest Cell Center", GUILayout.Height(26)))
            {
                var bridge = spawner.ResolveBridge();
                if (bridge != null)
                {
                    Undo.RecordObject(spawner.transform, "Snap Spawner to Cell");
                    var hex = bridge.WorldToHex(spawner.transform.position);
                    spawner.transform.position = bridge.HexToWorld(hex);
                }
                else
                {
                    Debug.LogWarning("[HexSpawnerEditor] No HexTilemapBridge found in scene to snap against.");
                }
            }

            if (spawner is HexFormationAnchor anchor)
            {
                var bridge = anchor.ResolveBridge();
                if (bridge != null)
                {
                    var slots = anchor.CalculateSlots(bridge);
                    EditorGUILayout.HelpBox($"Calculated Formation Slots: {slots.Count} units. Assigned Prefabs: {anchor.UnitPrefabs.Count}.", MessageType.Info);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                if (GUILayout.Button("Spawn Now (PlayMode)", GUILayout.Height(28)))
                {
                    spawner.Spawn(spawner.ResolveBridge());
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
