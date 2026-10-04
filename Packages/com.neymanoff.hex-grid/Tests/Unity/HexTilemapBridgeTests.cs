using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity.Tests
{
    [TestFixture]
    public class HexTilemapBridgeTests
    {
        private GameObject _testRoot;
        private Grid _grid;
        private Tilemap _walkableTilemap;
        private Tilemap _obstacleTilemap;
        private HexTilemapBridge _bridge;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestHexGridRoot");
            _grid = _testRoot.AddComponent<Grid>();
            _grid.cellLayout = GridLayout.CellLayout.Hexagon;
            _grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            _grid.cellSize = new Vector3(0.8659766f, 1f, 1f);

            var walkableGo = new GameObject("WalkableTilemap");
            walkableGo.transform.SetParent(_testRoot.transform);
            _walkableTilemap = walkableGo.AddComponent<Tilemap>();
            walkableGo.AddComponent<TilemapRenderer>();

            var obstacleGo = new GameObject("ObstacleTilemap");
            obstacleGo.transform.SetParent(_testRoot.transform);
            _obstacleTilemap = obstacleGo.AddComponent<Tilemap>();
            obstacleGo.AddComponent<TilemapRenderer>();

            _bridge = _testRoot.AddComponent<HexTilemapBridge>();
            _bridge.Configure(_grid, _walkableTilemap, _obstacleTilemap);
            _bridge.UnitHeightOffset = 0.3f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.DestroyImmediate(_testRoot);
            }
        }

        [Test]
        public void CoordinateConversion_MatchesStaticOddRMath()
        {
            var hex = new HexCoord(3, 4);
            var cell = HexTilemapBridge.HexToTilemapCell(hex);

            var expectedOddR = hex.ToOddR();
            Assert.AreEqual(expectedOddR.col, cell.x);
            Assert.AreEqual(expectedOddR.row, cell.y);

            var backToHex = HexTilemapBridge.TilemapCellToHex(cell);
            Assert.AreEqual(hex, backToHex);
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(-3, 4)]
        [TestCase(5, -6)]
        [TestCase(-10, -10)]
        public void Roundtrip_AllCoordinates_PreserveIdentity(int q, int r)
        {
            var originHex = new HexCoord(q, r);
            var cell = HexTilemapBridge.HexToTilemapCell(originHex);
            var resultHex = HexTilemapBridge.TilemapCellToHex(cell);

            Assert.AreEqual(originHex, resultHex);
        }

        [Test]
        public void CanEnter_WhenCellHasNoWalkableTile_ReturnsFalse()
        {
            var hex = new HexCoord(1, 1);
            Assert.IsFalse(_bridge.CanEnter(hex), "Empty tilemap cell should not be enterable when walkable tile is required.");
        }

        [Test]
        public void CanEnter_WhenObstaclePresent_ReturnsFalse()
        {
            var hex = new HexCoord(0, 0);
            var cell = HexTilemapBridge.HexToTilemapCell(hex);

            // Create a dummy tile
            var dummyTile = ScriptableObject.CreateInstance<Tile>();

            // Put tile on walkable tilemap
            _walkableTilemap.SetTile(cell, dummyTile);
            Assert.IsTrue(_bridge.CanEnter(hex), "Cell with walkable tile should be enterable.");

            // Put tile on obstacle tilemap
            _obstacleTilemap.SetTile(cell, dummyTile);
            Assert.IsFalse(_bridge.CanEnter(hex), "Cell with obstacle tile should be impassable.");
        }

        [Test]
        public void WorldPositionConversions_AreConsistentWithGrid()
        {
            var hex = new HexCoord(2, 3);
            var worldPos = _bridge.HexToWorld(hex);

            var recoveredHex = _bridge.WorldToHex(worldPos);
            Assert.AreEqual(hex, recoveredHex, "Converting Hex -> World -> Hex should preserve coordinate.");
        }

        [Test]
        public void HexToUnitWorld_ElevatesPositionByHeightOffset()
        {
            var hex = new HexCoord(0, 0);
            Vector3 groundPos = _bridge.HexToWorld(hex);
            Vector3 unitPos = _bridge.HexToUnitWorld(hex);

            Assert.AreEqual(groundPos.x, unitPos.x, 0.001f);
            Assert.AreEqual(groundPos.z, unitPos.z, 0.001f);
            Assert.AreEqual(groundPos.y + 0.3f, unitPos.y, 0.001f, "Unit position should be elevated by height offset along Y axis.");
        }

        [Test]
        public void WorldPositionConversions_WorkWith3DGroundPlaneRotation()
        {
            // Rotate grid 90 degrees around X to lay flat on XZ ground plane
            _testRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var hex = new HexCoord(1, -2);
            var worldPos = _bridge.HexToWorld(hex);

            // In 3D ground plane, Y should be flat (0) and Z should be depth
            Assert.AreEqual(0f, worldPos.y, 0.001f, "Ground plane cell center Y should be zero.");

            var recoveredHex = _bridge.WorldToHex(worldPos);
            Assert.AreEqual(hex, recoveredHex, "Converting Hex -> 3D World (XZ) -> Hex should preserve coordinate.");
        }
    }
}
