using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity.Tests
{
    [TestFixture]
    public class HexGridGeneratorTests
    {
        private GameObject _testRoot;
        private Grid _grid;
        private Tilemap _walkableTilemap;
        private Tilemap _obstacleTilemap;
        private HexTilemapBridge _bridge;
        private HexGridGenerator _generator;

        private Tile _groundTile;
        private Tile _obstacleTile;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestGeneratorRoot");
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

            _bridge = _testRoot.AddComponent<HexTilemapBridge>();
            _bridge.Configure(_grid, _walkableTilemap, _obstacleTilemap);

            _groundTile = ScriptableObject.CreateInstance<Tile>();
            _obstacleTile = ScriptableObject.CreateInstance<Tile>();

            _generator = _testRoot.AddComponent<HexGridGenerator>();
            _generator.Configure(_walkableTilemap, _obstacleTilemap, _groundTile, _obstacleTile, _bridge);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.DestroyImmediate(_testRoot);
            }
            if (_groundTile != null) Object.DestroyImmediate(_groundTile);
            if (_obstacleTile != null) Object.DestroyImmediate(_obstacleTile);
        }

        [TestCase(1, 7)]
        [TestCase(2, 19)]
        [TestCase(3, 37)]
        [TestCase(4, 61)]
        [TestCase(5, 91)]
        public void HexagonIsland_CalculatesExactRadialCellCount(int radius, int expectedCount)
        {
            _generator.Shape = HexGridShape.HexagonIsland;
            _generator.Radius = radius;

            var coords = _generator.GetCoordinatesForShape();
            Assert.AreEqual(expectedCount, coords.Count, $"Radius {radius} hex island should produce {expectedCount} cells.");
        }

        [TestCase(5, 4, 20)]
        [TestCase(10, 8, 80)]
        [TestCase(12, 10, 120)]
        public void Rectangle_CalculatesExactRectangularCellCount(int width, int height, int expectedCount)
        {
            _generator.Shape = HexGridShape.Rectangle;
            _generator.Width = width;
            _generator.Height = height;

            var coords = _generator.GetCoordinatesForShape();
            Assert.AreEqual(expectedCount, coords.Count, $"Rectangle {width}x{height} should produce {expectedCount} cells.");
        }

        [Test]
        public void GenerateGrid_PaintsWalkableTilesOnTilemap()
        {
            _generator.Shape = HexGridShape.HexagonIsland;
            _generator.Radius = 2; // 19 cells
            _generator.ObstacleDensity = 0f;

            _generator.GenerateGrid();

            var bounds = _walkableTilemap.cellBounds;
            int placedCount = 0;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    if (_walkableTilemap.HasTile(new Vector3Int(x, y, 0)))
                        placedCount++;
                }
            }

            Assert.AreEqual(19, placedCount, "Walkable tilemap should have exactly 19 tiles painted.");
        }

        [Test]
        public void ClearGrid_RemovesAllTilesFromTilemaps()
        {
            _generator.Shape = HexGridShape.HexagonIsland;
            _generator.Radius = 2;
            _generator.GenerateGrid();

            _generator.ClearGrid();

            Assert.IsFalse(_walkableTilemap.HasTile(Vector3Int.zero), "Tiles should be cleared.");
        }
    }
}
