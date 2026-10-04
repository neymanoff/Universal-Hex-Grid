using Neymanoff.HexGrid.Core;
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
            // 1. Root Grid GameObject (Laid horizontally on XZ Ground plane at Y = 0.01)
            var gridGo = new GameObject("Hex Grid");
            Undo.RegisterCreatedObjectUndo(gridGo, "Create Hex Grid Setup");

            if (menuCommand.context is GameObject parent)
            {
                GameObjectUtility.SetParentAndAlign(gridGo, parent);
            }

            gridGo.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            gridGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var grid = gridGo.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Hexagon;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            // Pointy-Top exact aspect ratio (sqrt(3)/2 ≈ 0.8659766) matches Legends: Legacy of the Lost
            grid.cellSize = new Vector3(0.8659766f, 1f, 1f);

            // 2. Walkable Tilemap
            var walkableGo = new GameObject("Walkable Tilemap");
            GameObjectUtility.SetParentAndAlign(walkableGo, gridGo);
            var walkableTilemap = walkableGo.AddComponent<Tilemap>();
            walkableTilemap.tileAnchor = Vector3.zero;
            var walkableRenderer = walkableGo.AddComponent<TilemapRenderer>();
            walkableRenderer.sortingOrder = 0;

            // 3. Obstacle Tilemap
            var obstacleGo = new GameObject("Obstacle Tilemap");
            GameObjectUtility.SetParentAndAlign(obstacleGo, gridGo);
            var obstacleTilemap = obstacleGo.AddComponent<Tilemap>();
            obstacleTilemap.tileAnchor = Vector3.zero;
            var obstacleRenderer = obstacleGo.AddComponent<TilemapRenderer>();
            obstacleRenderer.sortingOrder = 5;

            // 4. Highlight Overlay Tilemap
            var overlayGo = new GameObject("Highlight Overlay");
            GameObjectUtility.SetParentAndAlign(overlayGo, gridGo);
            var overlayTilemap = overlayGo.AddComponent<Tilemap>();
            overlayTilemap.tileAnchor = Vector3.zero;
            var overlayRenderer = overlayGo.AddComponent<TilemapRenderer>();
            overlayRenderer.sortingOrder = 10;

            var overlay = overlayGo.AddComponent<TilemapHighlightOverlay>();

            // Load default tile assets from package
            var groundTile = AssetDatabase.LoadAssetAtPath<TileBase>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexTile_Ground.asset");
            var obstacleTile = AssetDatabase.LoadAssetAtPath<TileBase>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexTile_Obstacle.asset");
            var highlightTile = AssetDatabase.LoadAssetAtPath<TileBase>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexTile_Highlight.asset");
            var highlightOutlineTile = AssetDatabase.LoadAssetAtPath<TileBase>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexTile_Highlight_Outline.asset");

            overlay.Configure(overlayTilemap, highlightTile, highlightOutlineTile);
            overlay.RenderMode = HighlightRenderMode.Outline;

            // Paint initial demo island (radius 3 = 37 cells)
            if (groundTile != null)
            {
                for (int q = -3; q <= 3; q++)
                {
                    int rMin = Mathf.Max(-3, -q - 3);
                    int rMax = Mathf.Min(3, -q + 3);
                    for (int r = rMin; r <= rMax; r++)
                    {
                        walkableTilemap.SetTile(HexTilemapBridge.HexToTilemapCell(new HexCoord(q, r)), groundTile);
                    }
                }
            }

            // Paint 3 demo obstacles
            if (obstacleTile != null)
            {
                obstacleTilemap.SetTile(HexTilemapBridge.HexToTilemapCell(new HexCoord(1, 0)), obstacleTile);
                obstacleTilemap.SetTile(HexTilemapBridge.HexToTilemapCell(new HexCoord(0, 2)), obstacleTile);
                obstacleTilemap.SetTile(HexTilemapBridge.HexToTilemapCell(new HexCoord(-1, -1)), obstacleTile);
            }

            // 5. Attach Bridge with 0.3 unit height offset and Inspector geometry controls
            var bridge = gridGo.AddComponent<HexTilemapBridge>();
            bridge.Configure(grid, walkableTilemap, obstacleTilemap);
            bridge.UnitHeightOffset = 0.3f;
            bridge.ApplyGridDimensions();

            // 6. Attach 3D Spawner foundation
            var spawner = gridGo.AddComponent<HexGrid3DSpawner>();

            // 7. Attach Pointer Picker configured for 3D ground plane (XZ at Y = 0.01)
            var picker = gridGo.AddComponent<TilemapPointerPicker>();
            picker.Configure(bridge, Camera.main, GridPlaneOrientation.XZ, 0.01f);

            // 8. Create Greybox Unit (Cube standing on cell surface)
            var unitGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unitGo.name = "Greybox Hero";
            unitGo.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            unitGo.transform.position = bridge.HexToUnitWorld(HexCoord.Zero);
            Undo.RegisterCreatedObjectUndo(unitGo, "Create Greybox Hero");

            var mover = unitGo.AddComponent<GridMover>();
            mover.Configure(bridge);

            // 9. Create Demo Controller
            var controllerGo = new GameObject("Demo Controller");
            Undo.RegisterCreatedObjectUndo(controllerGo, "Create Demo Controller");

            var controller = controllerGo.AddComponent<HexDemoController>();
            controller.Configure(bridge, overlay, picker, mover);

            // 10. Configure Main Camera for 3D tactical perspective view (angled top-down)
            var cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam.transform, "Align Camera to Hex Grid");
                Undo.RecordObject(cam, "Set Tactical Camera");
                cam.transform.position = new Vector3(0f, 7.5f, -6.3f);
                cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
                cam.orthographic = false;
                cam.fieldOfView = 45f;
            }

            // Mark all created scene objects dirty for persistent serialization
            EditorUtility.SetDirty(gridGo);
            EditorUtility.SetDirty(walkableGo);
            EditorUtility.SetDirty(obstacleGo);
            EditorUtility.SetDirty(overlayGo);
            EditorUtility.SetDirty(bridge);
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(picker);
            EditorUtility.SetDirty(unitGo);
            EditorUtility.SetDirty(mover);
            EditorUtility.SetDirty(controllerGo);
            EditorUtility.SetDirty(controller);

            Selection.activeGameObject = gridGo;
        }
    }
}
