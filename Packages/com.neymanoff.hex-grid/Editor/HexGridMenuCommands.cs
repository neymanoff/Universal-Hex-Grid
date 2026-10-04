using Neymanoff.HexGrid.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Editor
{
    /// <summary>
    /// Editor utilities and menu items for creating and configuring hex grid setups in the scene.
    /// Empowers level designers to author grids visually in the Inspector and Tile Palette.
    /// </summary>
    public static class HexGridMenuCommands
    {
        [MenuItem("GameObject/Hex Grid/Create Tactical Grid Setup", false, 10)]
        public static void CreateTacticalGridSetup(MenuCommand menuCommand)
        {
            // 1. Root Grid GameObject
            var gridGo = new GameObject("Hex Grid");
            Undo.RegisterCreatedObjectUndo(gridGo, "Create Hex Grid Setup");

            if (menuCommand.context is GameObject parent)
            {
                GameObjectUtility.SetParentAndAlign(gridGo, parent);
            }

            var grid = gridGo.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Hexagon;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            grid.cellSize = new Vector3(1f, 1f, 1f);

            // 2. Walkable Tilemap
            var walkableGo = new GameObject("Walkable Tilemap");
            GameObjectUtility.SetParentAndAlign(walkableGo, gridGo);
            var walkableTilemap = walkableGo.AddComponent<Tilemap>();
            walkableGo.AddComponent<TilemapRenderer>();

            // 3. Obstacle Tilemap
            var obstacleGo = new GameObject("Obstacle Tilemap");
            GameObjectUtility.SetParentAndAlign(obstacleGo, gridGo);
            var obstacleTilemap = obstacleGo.AddComponent<Tilemap>();
            obstacleGo.AddComponent<TilemapRenderer>();

            // 4. Highlight Overlay Tilemap
            var overlayGo = new GameObject("Highlight Overlay");
            GameObjectUtility.SetParentAndAlign(overlayGo, gridGo);
            var overlayTilemap = overlayGo.AddComponent<Tilemap>();
            var overlayRenderer = overlayGo.AddComponent<TilemapRenderer>();
            overlayRenderer.sortingOrder = 10; // Render on top of terrain

            var overlay = overlayGo.AddComponent<TilemapHighlightOverlay>();
            var overlayField = typeof(TilemapHighlightOverlay).GetField("_overlayTilemap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            overlayField?.SetValue(overlay, overlayTilemap);

            // 5. Attach Bridge
            var bridge = gridGo.AddComponent<HexTilemapBridge>();
            var bridgeGridField = typeof(HexTilemapBridge).GetField("_grid",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bridgeGridField?.SetValue(bridge, grid);

            var bridgeWalkableField = typeof(HexTilemapBridge).GetField("_walkableTilemap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bridgeWalkableField?.SetValue(bridge, walkableTilemap);

            var bridgeObstacleField = typeof(HexTilemapBridge).GetField("_obstacleTilemap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bridgeObstacleField?.SetValue(bridge, obstacleTilemap);

            // 6. Attach Pointer Picker
            var picker = gridGo.AddComponent<TilemapPointerPicker>();
            var pickerBridgeField = typeof(TilemapPointerPicker).GetField("_bridge",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            pickerBridgeField?.SetValue(picker, bridge);

            // 7. Create Greybox Unit (Cube)
            var unitGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unitGo.name = "Greybox Hero";
            unitGo.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Undo.RegisterCreatedObjectUndo(unitGo, "Create Greybox Hero");

            var mover = unitGo.AddComponent<GridMover>();
            var moverBridgeField = typeof(GridMover).GetField("_bridge",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            moverBridgeField?.SetValue(mover, bridge);

            // 8. Create Demo Controller
            var controllerGo = new GameObject("Demo Controller");
            Undo.RegisterCreatedObjectUndo(controllerGo, "Create Demo Controller");

            var controller = controllerGo.AddComponent<HexDemoController>();
            var ctrlBridgeField = typeof(HexDemoController).GetField("_bridge",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ctrlBridgeField?.SetValue(controller, bridge);

            var ctrlOverlayField = typeof(HexDemoController).GetField("_overlay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ctrlOverlayField?.SetValue(controller, overlay);

            var ctrlPickerField = typeof(HexDemoController).GetField("_pointerPicker",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ctrlPickerField?.SetValue(controller, picker);

            var ctrlUnitField = typeof(HexDemoController).GetField("_unit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ctrlUnitField?.SetValue(controller, mover);

            Selection.activeGameObject = gridGo;
            EditorUtility.SetDirty(gridGo);
        }
    }
}
