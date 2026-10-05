using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Neymanoff.HexGrid.Unity.Tests
{
    [TestFixture]
    public class HexSpawnAndFormationTests
    {
        private GameObject _testRoot;
        private Grid _grid;
        private Tilemap _walkableTilemap;
        private Tilemap _obstacleTilemap;
        private HexTilemapBridge _bridge;
        private GameObject _unitPrefab;

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

            _unitPrefab = new GameObject("TestUnitPrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null) Object.DestroyImmediate(_testRoot);
            if (_unitPrefab != null) Object.DestroyImmediate(_unitPrefab);
        }

        [Test]
        public void FormationPatternSO_Presets_CreateValidSlotCounts()
        {
            var p2_3 = FormationPatternSO.CreatePreset2_3();
            Assert.AreEqual(5, p2_3.SlotCount);
            Assert.AreEqual(5, p2_3.GetRelativeSlots().Count);

            var p3_2 = FormationPatternSO.CreatePreset3_2();
            Assert.AreEqual(5, p3_2.SlotCount);

            var p1_2_1 = FormationPatternSO.CreatePreset1_2_1();
            Assert.AreEqual(4, p1_2_1.SlotCount);

            var pLine = FormationPatternSO.CreatePresetLine(6);
            Assert.AreEqual(6, pLine.SlotCount);

            var pWedge = FormationPatternSO.CreatePresetWedge();
            Assert.AreEqual(5, pWedge.SlotCount);

            Object.DestroyImmediate(p2_3);
            Object.DestroyImmediate(p3_2);
            Object.DestroyImmediate(p1_2_1);
            Object.DestroyImmediate(pLine);
            Object.DestroyImmediate(pWedge);
        }

        [Test]
        public void GridOccupant_BindsAndMovesInOccupancyMap()
        {
            var occMap = new HexOccupancyMap<GridOccupant>();
            var unitGo = new GameObject("UnitWithOccupant");
            var occupant = unitGo.AddComponent<GridOccupant>();

            var startCoord = new HexCoord(2, 3);
            bool bound = occupant.Bind(startCoord, CellOwner.Player, occMap);

            Assert.IsTrue(bound);
            Assert.AreEqual(startCoord, occupant.CurrentCoord);
            Assert.IsTrue(occMap.IsOccupied(startCoord));
            Assert.IsTrue(occMap.TryGetOccupant(startCoord, out var found));
            Assert.AreSame(occupant, found);

            // Move to new cell
            var newCoord = new HexCoord(2, 4);
            bool moved = occupant.MoveTo(newCoord);

            Assert.IsTrue(moved);
            Assert.AreEqual(newCoord, occupant.CurrentCoord);
            Assert.IsFalse(occMap.IsOccupied(startCoord));
            Assert.IsTrue(occMap.IsOccupied(newCoord));

            // Vacate
            occupant.Vacate();
            Assert.IsFalse(occMap.IsOccupied(newCoord));

            Object.DestroyImmediate(unitGo);
        }

        [Test]
        public void HexSpawnPoint_CalculatesSingleSlotAndSpawns()
        {
            var spawnerGo = new GameObject("HeroSpawnPoint");
            spawnerGo.transform.SetParent(_testRoot.transform);
            var spawnPoint = spawnerGo.AddComponent<HexSpawnPoint>();
            spawnPoint.Faction = CellOwner.Player;
            spawnPoint.UnitPrefab = _unitPrefab;
            spawnerGo.transform.position = _bridge.HexToWorld(new HexCoord(3, 1));

            var slots = spawnPoint.CalculateSlots(_bridge);
            Assert.AreEqual(1, slots.Count);
            Assert.AreEqual(new HexCoord(3, 1), slots[0].Coord);
            Assert.AreEqual(CellOwner.Player, slots[0].Faction);

            var occMap = new HexOccupancyMap<GridOccupant>();
            var spawned = spawnPoint.Spawn(_bridge, occMap);

            Assert.AreEqual(1, spawned.Count);
            Assert.IsTrue(occMap.IsOccupied(new HexCoord(3, 1)));

            foreach (var go in spawned) Object.DestroyImmediate(go);
            Object.DestroyImmediate(spawnerGo);
        }

        [Test]
        public void HexFormationAnchor_CalculatesMultiUnitFormationSlots()
        {
            var anchorGo = new GameObject("SquadAnchor");
            anchorGo.transform.SetParent(_testRoot.transform);
            var anchor = anchorGo.AddComponent<HexFormationAnchor>();
            anchor.Faction = CellOwner.Ally;
            anchor.WaveGroup = "DefenseWave1";
            anchor.FormationPattern = FormationPatternSO.CreatePreset2_3();
            anchorGo.transform.position = _bridge.HexToWorld(new HexCoord(0, 0));

            var slots = anchor.CalculateSlots(_bridge);
            Assert.AreEqual(5, slots.Count);
            foreach (var slot in slots)
            {
                Assert.AreEqual(CellOwner.Ally, slot.Faction);
            }

            // Assign prefabs and spawn
            anchor.UnitPrefabs.Add(_unitPrefab);
            anchor.UnitPrefabs.Add(_unitPrefab);
            anchor.UnitPrefabs.Add(_unitPrefab);

            var occMap = new HexOccupancyMap<GridOccupant>();
            var spawned = anchor.Spawn(_bridge, occMap);

            Assert.AreEqual(3, spawned.Count);
            Assert.AreEqual(3, occMap.Count);

            foreach (var go in spawned) Object.DestroyImmediate(go);
            Object.DestroyImmediate(anchor.FormationPattern);
            Object.DestroyImmediate(anchorGo);
        }

        [Test]
        public void MultiTeam_AlliedAndEnemyAnchors_CalculateDistinctFormations()
        {
            var allyGo = new GameObject("AllyAnchor");
            allyGo.transform.position = _bridge.HexToWorld(new HexCoord(0, 0));
            var allyAnchor = allyGo.AddComponent<HexFormationAnchor>();
            allyAnchor.Faction = CellOwner.Ally;
            allyAnchor.FormationPattern = FormationPatternSO.CreatePreset1_2_1();

            var enemyGo = new GameObject("EnemyAnchor");
            enemyGo.transform.position = _bridge.HexToWorld(new HexCoord(10, 10));
            var enemyAnchor = enemyGo.AddComponent<HexFormationAnchor>();
            enemyAnchor.Faction = CellOwner.Enemy;
            enemyAnchor.FormationPattern = FormationPatternSO.CreatePreset1_2_1();
            enemyAnchor.FacingDirectionStep = 3; // Facing West towards ally

            var allySlots = allyAnchor.CalculateSlots(_bridge);
            var enemySlots = enemyAnchor.CalculateSlots(_bridge);

            Assert.AreEqual(4, allySlots.Count);
            Assert.AreEqual(4, enemySlots.Count);

            var allyCoords = new HashSet<HexCoord>();
            foreach (var s in allySlots) allyCoords.Add(s.Coord);

            foreach (var s in enemySlots)
            {
                Assert.IsFalse(allyCoords.Contains(s.Coord), "Enemy slot must not overlap with ally slot");
            }

            Object.DestroyImmediate(allyAnchor.FormationPattern);
            Object.DestroyImmediate(enemyAnchor.FormationPattern);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(enemyGo);
        }
    }
}
