using System.Collections.Generic;
using Neymanoff.HexGrid.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Neymanoff.HexGrid.Unity.Tests
{
    [TestFixture]
    public class HexUiLayoutTests
    {
        private GameObject _canvasGo;
        private Canvas _canvas;
        private GameObject _slotPrefab;
        private GameObject _buttonPrefab;

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas");
            _canvas = _canvasGo.AddComponent<Canvas>();
            _canvasGo.AddComponent<CanvasScaler>();
            _canvasGo.AddComponent<GraphicRaycaster>();

            _slotPrefab = new GameObject("TestSlotPrefab");
            _slotPrefab.AddComponent<RectTransform>();
            _slotPrefab.AddComponent<Image>();

            _buttonPrefab = new GameObject("TestButtonPrefab");
            _buttonPrefab.AddComponent<RectTransform>();
            _buttonPrefab.AddComponent<Button>();
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(_buttonPrefab.transform);
            textGo.AddComponent<TextMeshProUGUI>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGo != null) Object.DestroyImmediate(_canvasGo);
            if (_slotPrefab != null) Object.DestroyImmediate(_slotPrefab);
            if (_buttonPrefab != null) Object.DestroyImmediate(_buttonPrefab);
        }

        [Test]
        public void HexUiLayoutConverter_Build_Empty_ReturnsEmptySlots()
        {
            var layout = HexUiLayoutConverter.Build(System.Array.Empty<HexCoord>(), cellHeightPx: 100f);
            Assert.AreEqual(0, layout.Slots.Count);
            Assert.AreEqual(100f, layout.CellHeightPx);
            Assert.AreEqual(100f * HexTilemapBridge.PointyTopAspectRatio, layout.CellWidthPx, 0.001f);
        }

        [Test]
        public void HexUiLayoutConverter_Build_CalculatesCorrectProportionsAndCentering()
        {
            var coords = new List<HexCoord>
            {
                new(0, 0),
                new(1, 0),
                new(0, 1)
            };

            float h = 100f;
            float expectedW = h * HexTilemapBridge.PointyTopAspectRatio;

            var layout = HexUiLayoutConverter.Build(coords, cellHeightPx: h, cellGapPx: 10f, center: true);

            Assert.AreEqual(3, layout.Slots.Count);
            Assert.AreEqual(h, layout.CellHeightPx);
            Assert.AreEqual(expectedW, layout.CellWidthPx, 0.001f);

            // Verify centered bounding box (sum of min and max should be near zero)
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var slot in layout.Slots)
            {
                var p = slot.AnchoredPositionPx;
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            Assert.AreEqual(0f, minX + maxX, 0.01f, "Centered layout must have horizontal bounds centered around 0");
            Assert.AreEqual(0f, minY + maxY, 0.01f, "Centered layout must have vertical bounds centered around 0");
        }

        [Test]
        public void HexUiLayoutConverter_MirrorX_InvertsHorizontalCoordinates()
        {
            var coords = new List<HexCoord>
            {
                new(1, 0),
                new(-1, 0)
            };

            var normalLayout = HexUiLayoutConverter.Build(coords, cellHeightPx: 80f, center: false, mirrorX: false);
            var mirroredLayout = HexUiLayoutConverter.Build(coords, cellHeightPx: 80f, center: false, mirrorX: true);

            Assert.AreEqual(normalLayout.Slots[0].AnchoredPositionPx.x, -mirroredLayout.Slots[0].AnchoredPositionPx.x, 0.01f);
        }

        [Test]
        public void HexUiLayoutConverter_Build_FromFormationPatternSO()
        {
            var pattern = FormationPatternSO.CreatePreset2_3();
            var layout = HexUiLayoutConverter.Build(pattern, cellHeightPx: 96f);

            Assert.AreEqual(5, layout.Slots.Count);
            Object.DestroyImmediate(pattern);
        }

        [Test]
        public void HexTilemapSampler_SamplesNonEmptyTiles()
        {
            var gridGo = new GameObject("TestGridRoot");
            gridGo.AddComponent<Grid>();
            var tmGo = new GameObject("TestTilemap");
            tmGo.transform.SetParent(gridGo.transform);
            var tm = tmGo.AddComponent<Tilemap>();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tm.SetTile(new Vector3Int(0, 0, 0), tile);
            tm.SetTile(new Vector3Int(1, 2, 0), tile);

            var sampled = HexTilemapSampler.Sample(tm);
            Assert.AreEqual(2, sampled.Count);

            var canonical = HexTilemapSampler.SampleCanonical(tm);
            Assert.AreEqual(2, canonical.Count);
            // Canonical sorts top-to-bottom: row 2 comes before row 0
            Assert.AreEqual(2, canonical[0].GridPos.y);
            Assert.AreEqual(0, canonical[1].GridPos.y);

            Object.DestroyImmediate(tile);
            Object.DestroyImmediate(gridGo);
        }

        [Test]
        public void HexFormationPreviewUI_InstantiatesAndPositionsSlots()
        {
            var previewGo = new GameObject("PreviewPanel", typeof(RectTransform));
            previewGo.transform.SetParent(_canvasGo.transform);
            var preview = previewGo.AddComponent<HexFormationPreviewUI>();

            preview.CellHeightPx = 100f;
            preview.CellGapPx = 5f;
            preview.SlotPrefab = _slotPrefab;

            var pattern = FormationPatternSO.CreatePreset1_2_1(); // 4 slots
            preview.SetFormation(pattern);

            Assert.AreEqual(4, preview.Count);
            for (int i = 0; i < 4; i++)
            {
                var slotRt = preview.GetSlot(i);
                Assert.IsNotNull(slotRt);
                Assert.AreEqual(previewGo.transform, slotRt.parent);
            }

            // Switch formation to 2-3 (5 slots)
            var pattern2 = FormationPatternSO.CreatePreset2_3();
            preview.SetFormation(pattern2);
            Assert.AreEqual(5, preview.Count);

            preview.Clear();
            Assert.AreEqual(0, preview.Count);

            Object.DestroyImmediate(pattern);
            Object.DestroyImmediate(pattern2);
            Object.DestroyImmediate(previewGo);
        }

        [Test]
        public void HexFormationSelectorUI_SelectsFormationAndUpdatesLinkedPreview()
        {
            var previewGo = new GameObject("PreviewPanel", typeof(RectTransform));
            previewGo.transform.SetParent(_canvasGo.transform);
            var preview = previewGo.AddComponent<HexFormationPreviewUI>();
            preview.CellHeightPx = 80f;
            preview.SlotPrefab = _slotPrefab;

            var selectorGo = new GameObject("SelectorUI", typeof(RectTransform));
            selectorGo.transform.SetParent(_canvasGo.transform);
            var selector = selectorGo.AddComponent<HexFormationSelectorUI>();
            selector.LinkedPreview = preview;
            selector.ButtonContainer = selectorGo.transform;
            selector.ButtonPrefab = _buttonPrefab;

            var p1 = FormationPatternSO.CreatePreset2_3();
            var p2 = FormationPatternSO.CreatePreset1_2_1();

            FormationPatternSO selected = null;
            selector.OnFormationSelected += (f) => selected = f;

            selector.SetFormations(new[] { p1, p2 }, selectFirst: false);

            Assert.AreEqual(2, selector.Formations.Count);

            // Select second formation
            selector.SelectFormation(1);
            Assert.AreSame(p2, selector.SelectedFormation);
            Assert.AreSame(p2, selected);
            Assert.AreEqual(4, preview.Count, "Linked preview must show 4 slots of 1-2-1 formation");

            Object.DestroyImmediate(p1);
            Object.DestroyImmediate(p2);
            Object.DestroyImmediate(previewGo);
            Object.DestroyImmediate(selectorGo);
        }
    }
}
