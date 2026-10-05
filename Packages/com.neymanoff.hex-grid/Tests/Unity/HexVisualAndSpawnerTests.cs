using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity.Tests
{
    [TestFixture]
    public class HexVisualAndSpawnerTests
    {
        private GameObject _testRoot;
        private Grid _grid;
        private Tilemap _walkableTilemap;
        private Tilemap _obstacleTilemap;
        private Tilemap _overlayTilemap;
        private HexTilemapBridge _bridge;
        private TilemapHighlightOverlay _overlay;

        private Tile _solidTile;
        private Tile _outlineTile;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestHexVisualRoot");
            _grid = _testRoot.AddComponent<Grid>();
            _grid.cellLayout = GridLayout.CellLayout.Hexagon;
            _grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            _grid.cellSize = new Vector3(0.8659766f, 1f, 1f);

            var walkableGo = new GameObject("WalkableTilemap");
            walkableGo.transform.SetParent(_testRoot.transform);
            _walkableTilemap = walkableGo.AddComponent<Tilemap>();

            var obstacleGo = new GameObject("ObstacleTilemap");
            obstacleGo.transform.SetParent(_testRoot.transform);
            _obstacleTilemap = obstacleGo.AddComponent<Tilemap>();

            var overlayGo = new GameObject("OverlayTilemap");
            overlayGo.transform.SetParent(_testRoot.transform);
            _overlayTilemap = overlayGo.AddComponent<Tilemap>();
            _overlay = overlayGo.AddComponent<TilemapHighlightOverlay>();

            _bridge = _testRoot.AddComponent<HexTilemapBridge>();
            _bridge.Configure(_grid, _walkableTilemap, _obstacleTilemap);

            _solidTile = ScriptableObject.CreateInstance<Tile>();
            _solidTile.flags = TileFlags.LockColor; // Default Unity Tile lock flag

            _outlineTile = ScriptableObject.CreateInstance<Tile>();
            _outlineTile.flags = TileFlags.LockColor;

            _overlay.Configure(_overlayTilemap, _solidTile, _outlineTile);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.DestroyImmediate(_testRoot);
            }
            if (_solidTile != null) Object.DestroyImmediate(_solidTile);
            if (_outlineTile != null) Object.DestroyImmediate(_outlineTile);
        }

        [Test]
        public void HexTilemapBridge_CellScale_UpdatesGridTransformScale()
        {
            _bridge.CellScale = 1.5f;

            Assert.AreEqual(1.5f, _grid.transform.localScale.x, 0.0001f);
            Assert.AreEqual(1.5f, _grid.transform.localScale.y, 0.0001f);
        }

        [Test]
        public void HexTilemapBridge_CellSpacing_UpdatesGridCellSizePreservingPointyTopRatio()
        {
            _bridge.CellSpacing = 1.25f;

            float expectedX = HexTilemapBridge.PointyTopAspectRatio * 1.25f;
            float expectedY = 1.25f;

            Assert.AreEqual(expectedX, _grid.cellSize.x, 0.0001f);
            Assert.AreEqual(expectedY, _grid.cellSize.y, 0.0001f);
        }

        [Test]
        public void TilemapHighlightOverlay_SetsColorAndClearsLockColorFlag()
        {
            var hex = new HexCoord(1, 2);
            var cell = HexTilemapBridge.HexToTilemapCell(hex);

            Color testHoverColor = new Color(0.1f, 0.9f, 0.2f, 0.75f);
            _overlay.HoverColor = testHoverColor;

            _overlay.ShowHover(hex);

            // Verify tile is placed
            Assert.IsTrue(_overlayTilemap.HasTile(cell));

            // Verify TileFlags.LockColor was cleared
            var flags = _overlayTilemap.GetTileFlags(cell);
            Assert.AreEqual(TileFlags.None, flags & TileFlags.LockColor, "LockColor flag must be removed to allow tinting.");

            // Verify color was applied
            var appliedColor = _overlayTilemap.GetColor(cell);
            Assert.AreEqual(testHoverColor.r, appliedColor.r, 0.01f);
            Assert.AreEqual(testHoverColor.g, appliedColor.g, 0.01f);
            Assert.AreEqual(testHoverColor.b, appliedColor.b, 0.01f);
            Assert.AreEqual(testHoverColor.a, appliedColor.a, 0.01f);
        }

        [Test]
        public void TilemapHighlightOverlay_SwitchesBetweenSolidAndOutline()
        {
            _overlay.RenderMode = HighlightRenderMode.Solid;
            Assert.AreSame(_solidTile, _overlay.ReachableTile);

            _overlay.RenderMode = HighlightRenderMode.Outline;
            Assert.AreSame(_outlineTile, _overlay.ReachableTile);
        }

        [Test]
        public void HexGrid3DSpawner_SpawnsAndClearsInstances()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            // Paint 2 walkable cells
            _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);
            _walkableTilemap.SetTile(new Vector3Int(1, 0, 0), _solidTile);

            // Create a dummy prefab
            var dummyPrefab = new GameObject("DummyHexPrefab");

            try
            {
                // Reflection/configure spawner
                var defaultField = typeof(HexGrid3DSpawner).GetField("_defaultHexPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                defaultField.SetValue(spawner, dummyPrefab);

                var bridgeField = typeof(HexGrid3DSpawner).GetField("_bridge",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bridgeField.SetValue(spawner, _bridge);

                spawner.SpawnGrid();

                Assert.AreEqual(2, spawner.SpawnedInstances.Count, "Spawner should instantiate one 3D hex per painted cell.");

                spawner.ClearGrid();
                Assert.AreEqual(0, spawner.SpawnedInstances.Count, "ClearGrid should remove all spawned instances.");
            }
            finally
            {
                Object.DestroyImmediate(dummyPrefab);
            }
        }

        [Test]
        public void HexGrid3DSpawner_ScansMultipleTilemaps_WalkableAndObstacle()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            // Paint 2 walkable cells and 1 obstacle cell
            _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);
            _walkableTilemap.SetTile(new Vector3Int(1, 0, 0), _solidTile);
            _obstacleTilemap.SetTile(new Vector3Int(2, 0, 0), _solidTile);

            var dummyPrefab = new GameObject("DummyHexPrefab");
            try
            {
                spawner.Configure(_bridge, new[] { _walkableTilemap, _obstacleTilemap }, dummyPrefab);
                spawner.SpawnGrid();

                Assert.AreEqual(3, spawner.SpawnedInstances.Count, "Spawner should scan both Walkable and Obstacle tilemaps.");

                spawner.ClearGrid();
                Assert.AreEqual(0, spawner.SpawnedInstances.Count);
            }
            finally
            {
                Object.DestroyImmediate(dummyPrefab);
            }
        }

        [Test]
        public void HexGrid3DSpawner_AlignsBottomToSurface_CorrectsPivotOffset()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);

            // Create a standard primitive capsule (pivot at center, height = 2, so min.y would be -1)
            var capsulePrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                spawner.Configure(_bridge, new[] { _walkableTilemap }, capsulePrefab);
                spawner.AlignBottomToSurface = true;
                spawner.SpawnGrid();

                Assert.AreEqual(1, spawner.SpawnedInstances.Count);
                var spawnedCapsule = spawner.SpawnedInstances[0];
                var collider = spawnedCapsule.GetComponent<Collider>();

                Assert.IsNotNull(collider);
                Assert.AreEqual(1f, spawnedCapsule.transform.position.y, 0.01f, "Capsule transform should be shifted up by 1 unit.");
                // Bounds min.y should be ~0f (flush on ground), NOT -1f
                Assert.AreEqual(0f, collider.bounds.min.y, 0.01f, "Capsule bottom should rest flush on Y = 0 surface.");
            }
            finally
            {
                Object.DestroyImmediate(capsulePrefab);
            }
        }

        [Test]
        public void HexGrid3DSpawner_GeneratesProceduralGreyboxWhenEnabledAndPrefabNull()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);

            // DefaultHexPrefab is null, but GenerateProceduralHexForUnmapped is true
            spawner.Configure(_bridge, new[] { _walkableTilemap }, null);
            spawner.GenerateProceduralHexForUnmapped = true;
            spawner.SpawnGrid();

            Assert.AreEqual(1, spawner.SpawnedInstances.Count);
            var spawnedInstance = spawner.SpawnedInstances[0];
            var meshFilter = spawnedInstance.GetComponent<MeshFilter>();

            Assert.IsNotNull(meshFilter, "Procedural hex should have a MeshFilter attached.");
            Assert.IsNotNull(meshFilter.sharedMesh, "Procedural hex should have a generated Pointy-Top mesh.");
            Assert.IsTrue(meshFilter.sharedMesh.name.Contains("PointyTop"), "Generated mesh should be Pointy-Top.");
        }

        [Test]
        public void HexGrid3DSpawner_SkipsUnmappedTilesByDefaultWhenPrefabNull()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);

            // DefaultHexPrefab is null and GenerateProceduralHexForUnmapped is false by default
            spawner.Configure(_bridge, new[] { _walkableTilemap }, null);
            spawner.GenerateProceduralHexForUnmapped = false;
            spawner.SpawnGrid();

            Assert.AreEqual(0, spawner.SpawnedInstances.Count, "Unmapped tiles should be skipped by default when prefab is null.");
        }

        [Test]
        public void HexGrid3DSpawner_PerTileMapping_AppliesScaleAndRotationOffset()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);

            var dummyPrefab = new GameObject("ScaledRotatedPrefab");
            dummyPrefab.transform.localScale = new Vector3(2f, 2f, 2f);

            try
            {
                spawner.Configure(_bridge, new[] { _walkableTilemap }, null);
                spawner.TileMappings.Add(new TilePrefabMapping
                {
                    Tile = _solidTile,
                    Prefab = dummyPrefab,
                    Scale = new Vector3(0.5f, 0.25f, 0.5f),
                    RotationOffset = new Vector3(0f, 90f, 0f)
                });

                spawner.SpawnGrid();

                Assert.AreEqual(1, spawner.SpawnedInstances.Count);
                var instance = spawner.SpawnedInstances[0];

                // Expected scale: prefab.localScale (2,2,2) * globalScale (1,1,1) * perTileScale (0.5, 0.25, 0.5) = (1.0, 0.5, 1.0)
                Assert.AreEqual(1.0f, instance.transform.localScale.x, 0.001f);
                Assert.AreEqual(0.5f, instance.transform.localScale.y, 0.001f);
                Assert.AreEqual(1.0f, instance.transform.localScale.z, 0.001f);

                // Expected rotation: global rotation (0,0,0) + perTile (0,90,0) = (0, 90, 0)
                Assert.AreEqual(90f, instance.transform.rotation.eulerAngles.y, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(dummyPrefab);
            }
        }

        [Test]
        public void HexGrid3DSpawner_HideTilemapsOnSpawn_Preserves2DArtWhenFalse()
        {
            var spawnerGo = new GameObject("Spawner");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawner = spawnerGo.AddComponent<HexGrid3DSpawner>();

            var renderer = _walkableTilemap.gameObject.AddComponent<TilemapRenderer>();
            renderer.enabled = true;

            var dummyPrefab = new GameObject("DummyHexPrefab");
            try
            {
                _walkableTilemap.SetTile(new Vector3Int(0, 0, 0), _solidTile);
                spawner.Configure(_bridge, new[] { _walkableTilemap }, dummyPrefab);
                spawner.HideTilemapsOnSpawn = false;
                spawner.SpawnGrid();

                Assert.IsTrue(renderer.enabled, "TilemapRenderer should remain enabled when HideTilemapsOnSpawn is false.");

                spawner.ClearGrid();
                spawner.HideTilemapsOnSpawn = true;
                spawner.SpawnGrid();

                Assert.IsFalse(renderer.enabled, "TilemapRenderer should be disabled when HideTilemapsOnSpawn is true.");
            }
            finally
            {
                Object.DestroyImmediate(dummyPrefab);
            }
        }
    }
}
