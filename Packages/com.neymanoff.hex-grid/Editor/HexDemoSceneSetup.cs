using System.Collections.Generic;
using System.IO;
using Neymanoff.HexGrid.Core;
using Neymanoff.HexGrid.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Neymanoff.HexGrid.Editor
{
    /// <summary>
    /// Editor utility that configures the complete demonstration environment in SampleScene.
    /// Sets up formation ScriptableObjects, unit greybox prefabs, tactical squad spawners,
    /// and pre-battle formation preview and selection UI.
    /// </summary>
    public static class HexDemoSceneSetup
    {
        private const string FormationsPath = "Assets/Settings/Formations";
        private const string PrefabsPath = "Assets/Prefabs/Demo";
        private const string MaterialsPath = "Assets/Materials/Demo";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Hex Grid/Setup Demo Scene Full", priority = 50)]
        public static void SetupDemoScene()
        {
            Debug.Log("[HexDemoSceneSetup] Starting full demo scene configuration...");

            EnsureFoldersExist();

            // 1. Create Materials
            var playerMat = GetOrCreateMaterial("M_Unit_Player", new Color(0.2f, 0.6f, 1f));
            var enemyMat = GetOrCreateMaterial("M_Unit_Enemy", new Color(1f, 0.28f, 0.28f));
            var neutralMat = GetOrCreateMaterial("M_Unit_Neutral", new Color(0.95f, 0.85f, 0.2f));

            // 2. Create Formation Patterns
            var f2_3 = GetOrCreateFormation("Formation_2_3", FormationPatternSO.CreatePreset2_3);
            var f3_2 = GetOrCreateFormation("Formation_3_2", FormationPatternSO.CreatePreset3_2);
            var f1_2_1 = GetOrCreateFormation("Formation_1_2_1", FormationPatternSO.CreatePreset1_2_1);
            var fLine = GetOrCreateFormation("Formation_Line", () => FormationPatternSO.CreatePresetLine(4));
            var fWedge = GetOrCreateFormation("Formation_Wedge", FormationPatternSO.CreatePresetWedge);

            // 3. Create Unit Prefabs
            var playerPrefab = GetOrCreateUnitPrefab("PlayerUnitPrefab", playerMat);
            var enemyPrefab = GetOrCreateUnitPrefab("EnemyUnitPrefab", enemyMat);
            var neutralPrefab = GetOrCreateUnitPrefab("NeutralUnitPrefab", neutralMat);

            // 4. Create UI Prefabs
            var slotPrefab = GetOrCreateSlotPrefab("FormationSlotPrefab");
            var buttonPrefab = GetOrCreateButtonPrefab("FormationButtonPrefab");

            AssetDatabase.SaveAssets();

            // 5. Open & Configure Scene
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[HexDemoSceneSetup] Could not open scene at {ScenePath}");
                return;
            }

            var bridge = Object.FindAnyObjectByType<HexTilemapBridge>();
            if (bridge == null)
            {
                Debug.LogError("[HexDemoSceneSetup] HexTilemapBridge not found in scene!");
                return;
            }

            // Setup Tactical Spawners
            SetupSpawners(bridge, f2_3, f3_2, playerPrefab, enemyPrefab, neutralPrefab);

            // Setup UI Canvas
            SetupUI(new[] { f2_3, f3_2, f1_2_1, fLine, fWedge }, slotPrefab, buttonPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HexDemoSceneSetup] Demo scene configured and saved successfully!");
        }

        private static void EnsureFoldersExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(FormationsPath)) AssetDatabase.CreateFolder("Assets/Settings", "Formations");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabsPath)) AssetDatabase.CreateFolder("Assets/Prefabs", "Demo");

            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsPath)) AssetDatabase.CreateFolder("Assets/Materials", "Demo");
        }

        private static Material GetOrCreateMaterial(string matName, Color color)
        {
            string path = $"{MaterialsPath}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");

            mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static FormationPatternSO GetOrCreateFormation(string assetName, System.Func<FormationPatternSO> factory)
        {
            string path = $"{FormationsPath}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FormationPatternSO>(path);
            if (existing != null) return existing;

            var created = factory();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static GameObject GetOrCreateUnitPrefab(string prefabName, Material mat)
        {
            string path = $"{PrefabsPath}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = prefabName;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            go.AddComponent<GridOccupant>();
            go.AddComponent<GridMover>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject GetOrCreateSlotPrefab(string prefabName)
        {
            string path = $"{PrefabsPath}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(prefabName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64f, 64f);

            var img = go.GetComponent<Image>();
            var outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.neymanoff.hex-grid/Runtime/Tiles/HexagonPointTop_Outline.png");
            if (outlineSprite != null)
            {
                img.sprite = outlineSprite;
            }
            img.color = new Color(0.2f, 0.75f, 1f, 0.9f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject GetOrCreateButtonPrefab(string prefabName)
        {
            string path = $"{PrefabsPath}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(prefabName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 36f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.45f, 0.75f, 1f);
            colors.pressedColor = new Color(0.15f, 0.35f, 0.6f, 1f);
            btn.colors = colors;

            // Child Text
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Formation";
            tmp.fontSize = 13f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void SetupSpawners(
            HexTilemapBridge bridge,
            FormationPatternSO playerFormation,
            FormationPatternSO enemyFormation,
            GameObject playerPrefab,
            GameObject enemyPrefab,
            GameObject neutralPrefab)
        {
            var spawnersRoot = GameObject.Find("Tactical Spawners");
            if (spawnersRoot == null)
            {
                spawnersRoot = new GameObject("Tactical Spawners");
            }

            // 1. Player Squad Anchor at (-2, 0)
            var playerAnchorGo = GameObject.Find("Player Squad Anchor");
            if (playerAnchorGo == null)
            {
                playerAnchorGo = new GameObject("Player Squad Anchor");
                playerAnchorGo.transform.SetParent(spawnersRoot.transform);
            }
            playerAnchorGo.transform.position = bridge.HexToWorld(new HexCoord(-2, 0));

            var playerAnchor = playerAnchorGo.GetComponent<HexFormationAnchor>() ?? playerAnchorGo.AddComponent<HexFormationAnchor>();
            playerAnchor.Faction = CellOwner.Player;
            playerAnchor.FormationPattern = playerFormation;
            playerAnchor.FacingDirectionStep = 0; // East
            playerAnchor.FaceTargetTransform = true;
            playerAnchor.UnitPrefabs.Clear();
            for (int i = 0; i < 5; i++) playerAnchor.UnitPrefabs.Add(playerPrefab);

            // 2. Enemy Squad Anchor at (2, 0)
            var enemyAnchorGo = GameObject.Find("Enemy Squad Anchor");
            if (enemyAnchorGo == null)
            {
                enemyAnchorGo = new GameObject("Enemy Squad Anchor");
                enemyAnchorGo.transform.SetParent(spawnersRoot.transform);
            }
            enemyAnchorGo.transform.position = bridge.HexToWorld(new HexCoord(2, 0));

            var enemyAnchor = enemyAnchorGo.GetComponent<HexFormationAnchor>() ?? enemyAnchorGo.AddComponent<HexFormationAnchor>();
            enemyAnchor.Faction = CellOwner.Enemy;
            enemyAnchor.FormationPattern = enemyFormation;
            enemyAnchor.FacingDirectionStep = 3; // West
            enemyAnchor.FaceTargetTransform = true;
            enemyAnchor.UnitPrefabs.Clear();
            for (int i = 0; i < 5; i++) enemyAnchor.UnitPrefabs.Add(enemyPrefab);

            // Cross-target transforms for automatic facing
            playerAnchor.TargetTransform = enemyAnchorGo.transform;
            enemyAnchor.TargetTransform = playerAnchorGo.transform;

            // 3. Neutral Patrol Point at (0, 2)
            var neutralPointGo = GameObject.Find("Neutral Spawn Point");
            if (neutralPointGo == null)
            {
                neutralPointGo = new GameObject("Neutral Spawn Point");
                neutralPointGo.transform.SetParent(spawnersRoot.transform);
            }
            neutralPointGo.transform.position = bridge.HexToWorld(new HexCoord(0, 2));

            var neutralPoint = neutralPointGo.GetComponent<HexSpawnPoint>() ?? neutralPointGo.AddComponent<HexSpawnPoint>();
            neutralPoint.Faction = CellOwner.Neutral;
            neutralPoint.UnitPrefab = neutralPrefab;
            neutralPoint.FacingDirectionStep = 1; // South-East
        }

        private static void SetupUI(
            IReadOnlyList<FormationPatternSO> formations,
            GameObject slotPrefab,
            GameObject buttonPrefab)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Clean up existing Formation Preview Panel if present
            var existingPreviewPanel = canvas.transform.Find("Formation Preview Panel");
            if (existingPreviewPanel != null)
            {
                Object.DestroyImmediate(existingPreviewPanel.gameObject);
            }

            var uiRoot = canvas.transform.Find("Tactical UI Root");
            if (uiRoot == null)
            {
                var go = new GameObject("Tactical UI Root", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                uiRoot = go.transform;
            }

            // 1. Selector Panel (Top-Left)
            var selectorPanelGo = uiRoot.Find("Selector Panel")?.gameObject;
            if (selectorPanelGo == null)
            {
                selectorPanelGo = new GameObject("Selector Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                selectorPanelGo.transform.SetParent(uiRoot, false);
            }
            var selectorRt = selectorPanelGo.GetComponent<RectTransform>();
            selectorRt.anchorMin = new Vector2(0f, 1f);
            selectorRt.anchorMax = new Vector2(0f, 1f);
            selectorRt.pivot = new Vector2(0f, 1f);
            selectorRt.anchoredPosition = new Vector2(24f, -24f);
            selectorRt.sizeDelta = new Vector2(210f, 260f);

            var selectorImg = selectorPanelGo.GetComponent<Image>();
            selectorImg.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

            // Selector Title
            var titleGo = selectorPanelGo.transform.Find("Title")?.gameObject;
            if (titleGo == null)
            {
                titleGo = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(selectorPanelGo.transform, false);
            }
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            titleRt.sizeDelta = new Vector2(0f, 28f);

            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "FORMATIONS";
            titleTmp.fontSize = 13f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.9f, 0.95f, 1f);

            // Buttons Container
            var btnContainerGo = selectorPanelGo.transform.Find("Buttons Container")?.gameObject;
            if (btnContainerGo == null)
            {
                btnContainerGo = new GameObject("Buttons Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
                btnContainerGo.transform.SetParent(selectorPanelGo.transform, false);
            }
            var btnContainerRt = btnContainerGo.GetComponent<RectTransform>();
            btnContainerRt.anchorMin = new Vector2(0f, 0f);
            btnContainerRt.anchorMax = new Vector2(1f, 1f);
            btnContainerRt.anchoredPosition = new Vector2(0f, -18f);
            btnContainerRt.sizeDelta = new Vector2(-16f, -44f);

            var vlg = btnContainerGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 2. Preview Panel (Bottom-Left)
            var previewPanelGo = uiRoot.Find("Preview Panel")?.gameObject;
            if (previewPanelGo == null)
            {
                previewPanelGo = new GameObject("Preview Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                previewPanelGo.transform.SetParent(uiRoot, false);
            }
            var previewRt = previewPanelGo.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0f, 0f);
            previewRt.anchorMax = new Vector2(0f, 0f);
            previewRt.pivot = new Vector2(0f, 0f);
            previewRt.anchoredPosition = new Vector2(24f, 24f);
            previewRt.sizeDelta = new Vector2(260f, 220f);

            var previewImg = previewPanelGo.GetComponent<Image>();
            previewImg.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

            // Preview Title
            var pTitleGo = previewPanelGo.transform.Find("Title")?.gameObject;
            if (pTitleGo == null)
            {
                pTitleGo = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                pTitleGo.transform.SetParent(previewPanelGo.transform, false);
            }
            var pTitleRt = pTitleGo.GetComponent<RectTransform>();
            pTitleRt.anchorMin = new Vector2(0f, 1f);
            pTitleRt.anchorMax = new Vector2(1f, 1f);
            pTitleRt.pivot = new Vector2(0.5f, 1f);
            pTitleRt.anchoredPosition = new Vector2(0f, -8f);
            pTitleRt.sizeDelta = new Vector2(0f, 28f);

            var pTitleTmp = pTitleGo.GetComponent<TextMeshProUGUI>();
            pTitleTmp.text = "SQUAD PREVIEW";
            pTitleTmp.fontSize = 13f;
            pTitleTmp.fontStyle = FontStyles.Bold;
            pTitleTmp.alignment = TextAlignmentOptions.Center;
            pTitleTmp.color = new Color(0.9f, 0.95f, 1f);

            // Preview Slots Container
            var slotsContainerGo = previewPanelGo.transform.Find("Slots Container")?.gameObject;
            if (slotsContainerGo == null)
            {
                slotsContainerGo = new GameObject("Slots Container", typeof(RectTransform));
                slotsContainerGo.transform.SetParent(previewPanelGo.transform, false);
            }
            var slotsContainerRt = slotsContainerGo.GetComponent<RectTransform>();
            slotsContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            slotsContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotsContainerRt.pivot = new Vector2(0.5f, 0.5f);
            slotsContainerRt.anchoredPosition = new Vector2(0f, -10f);
            slotsContainerRt.sizeDelta = new Vector2(220f, 160f);

            // Wire Components
            var previewUI = previewPanelGo.GetComponent<HexFormationPreviewUI>() ?? previewPanelGo.AddComponent<HexFormationPreviewUI>();
            previewUI.Container = slotsContainerRt;
            previewUI.SlotPrefab = slotPrefab;
            previewUI.CellHeightPx = 52f;
            previewUI.CellGapPx = 6f;
            previewUI.InitialPattern = formations[0];

            var selectorUI = selectorPanelGo.GetComponent<HexFormationSelectorUI>() ?? selectorPanelGo.AddComponent<HexFormationSelectorUI>();
            selectorUI.ButtonContainer = btnContainerGo.transform;
            selectorUI.ButtonPrefab = buttonPrefab;
            selectorUI.LinkedPreview = previewUI;
            selectorUI.SetFormations(formations);
        }
    }
}
