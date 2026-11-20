#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using tmkoc.butterfly;
using AssetKits.ParticleImage;
using AssetKits.ParticleImage.Enumerations;

/// <summary>
/// One-shot builder for the Sonu's Little Butterfly scene (portrait, 1080x1920 reference).
/// Menu: Tools/Sonu Butterfly/Build Scene. Safe to re-run: it rebuilds MainCanvas content and managers.
/// Art is placeholder only (existing project sprites, tinted); every sprite slot is serialized for later swaps.
/// </summary>
public static class ButterflySceneBuilder
{
    const string ScenePath = "Assets/Scenes/SonusLittleButterfly.unity";
    const string SoFolder = "Assets/ScriptableObjects";
    const string DbPath = SoFolder + "/LevelDatabase.asset";
    const string StoryFolder = SoFolder + "/Stories";
    const string FontPath = "Assets/ImportedAssets/Poppins-SemiBold SDF.asset";
    const string CirclePath = "Assets/ImportedAssets/NewUI/Circle.png";
    const string RoundedPath = "Assets/ImportedAssets/NewUI/CommonButton.png";
    const string CloudPath = "Assets/ImportedAssets/NewUI/Cloud 1 New.png";
    const string ResetPath = "Assets/ImportedAssets/NewUI/resetreplay.png";
    const string StarPath = "Assets/ImportedAssets/EndPanelAsstes/Star 1.png";
    const string StoryBgPath = "Assets/ImportedAssets/StoryMode/UI_BGs_0005.png";
    const string SonuPath = "Assets/ImportedAssets/0GameAssets/EndPanelAssets/Sonu Happy.png";
    const string SonuSmallPath = "Assets/ImportedAssets/NewUI/sonu.png";
    const string WinLosePrefabPath = "Assets/Prefabs/WinLose.prefab";
    const string WinLoseCanvasPrefabPath = "Assets/Prefabs/WinLosePanelCanvas.prefab";
    const string StoryPrefabPath = "Assets/Scripts/StorySystemPackage/Prefabs/StoryCanvas_Root.prefab";
    const string BackButtonPath = "Assets/ImportedAssets/NewUI/BackButton.png";
    const string RestartButtonPath = "Assets/ImportedAssets/NewUI/Ellipse 948.png";
    const string TutorialHandPath = "Assets/ImportedAssets/NewUI/TutorialHand.png";
    // House 9-slice frames for text backgrounds, cards and board cells (instead of CommonButton.png)
    const string FrameRoundPath = "Assets/ImportedAssets/NewUI/Frame 1171276659.png"; // 475x285 rounded rect, ~125px corners
    const string FramePillPath = "Assets/ImportedAssets/NewUI/Frame 1171276660.png";  // 1000x230 pill, 115px caps
    static readonly Vector4 FrameRoundBorder = new Vector4(130, 130, 130, 130);
    static readonly Vector4 FramePillBorder = new Vector4(115, 114, 115, 114);

    // Sonu's Little Butterfly art (Assets/ImportedAssets/SonuButterflyAsset)
    const string ArtRoot = "Assets/ImportedAssets/SonuButterflyAsset/";
    const string BgArtPath = ArtRoot + "BG_3.png";
    const string TreeArtPath = ArtRoot + "FullTreeNew.png";      // 1024x1578: same canopy as FullTree, 44 px lower, longer trunk
    const string TreeShadowArtPath = ArtRoot + "tree shadow.png"; // 604x122 black ellipse
    const string ButterflyArtPath = ArtRoot + "Assets/Butterfly.png";
    const string CaterpillarArtPath = ArtRoot + "Assets/Caterpillar.png";
    const string ChrysalisArtPath = ArtRoot + "Assets/Chrysalis.png";
    const string EggOnLeafArtPath = ArtRoot + "Assets/egg.png";
    const string EggArtPath = ArtRoot + "Assets/Egg_2.png";
    const string WaterDropArtPath = ArtRoot + "Assets/water droplet.png";
    const string BushesArtPath = ArtRoot + "Assets/Bushes.png";
    const string Bushes2ArtPath = ArtRoot + "Assets/Bushes_2.png";
    const string BranchArtPath = ArtRoot + "Branch/Branch.png";          // merge-board twig icon
    const string GardenBranchArtPath = ArtRoot + "Branch/Branch_2.png";  // long garden branch (1624x680), layout per Branch/Ref_4.png
    const string BranchLeafArtPath = ArtRoot + "Branch/Leaf.png";
    const string BranchDryLeafArtPath = ArtRoot + "Branch/Dry leaf_1.png";
    const string PlantArtPath = ArtRoot + "Plant/Plant_01.png";
    const string HalfPlantArtPath = ArtRoot + "Plant/Half Plant.png";
    const string FreshLeafArtPath = ArtRoot + "Plant/Fresh leaf.png";
    const string FreshHalfLeafArtPath = ArtRoot + "Plant/Fresh half leaf.png";
    const string DryLeafArtPath = ArtRoot + "Plant/Dry leaf.png";
    const string DryHalfLeafArtPath = ArtRoot + "Plant/Dry half leaf.png";


    static readonly Color Cream = Hex("#FFF9EB");
    static readonly Color HeadingGreen = Hex("#588367");
    static readonly Color SoftGreen = Hex("#8AA78D");
    static readonly Color HintGreen = Hex("#96AE91");
    static readonly Color BoardGreen = Hex("#A6D397");
    static readonly Color CellGreen = Hex("#D5EBBE");
    static readonly Color GoalYellow = Hex("#FFEDC9");
    static readonly Color Orange = Hex("#F5A36E");

    static TMP_FontAsset font;
    static Sprite circle, rounded, cloud, resetIcon, star, storyBg, sonu, sonuSmall;
    static Sprite backBtn, restartBtn, handSprite, frameRound, framePill;
    static Sprite treeArt, treeShadowArt, bgArt, butterflyArt, caterpillarArt, chrysalisArt, eggOnLeafArt, eggArt, waterDropArt, bushesArt, bushes2Art;
    static Sprite branchArt, gardenBranchArt, branchLeafArt, branchDryLeafArt, plantArt, halfPlantArt, freshLeafArt, freshHalfLeafArt, dryLeafArt, dryHalfLeafArt;

    [MenuItem("Tools/Sonu Butterfly/Build Scene")]
    public static void Build()
    {
        LoadAssets();
        ApplyPlayerSettings();
        var db = BuildLevelDatabase();
        BuildStories(db);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ---------------------------------------------------------------- roots
        var mainCanvasGo = GameObject.Find("MainCanvas");
        var scriptsRoot = GameObject.Find("Scripts");
        var othersRoot = GameObject.Find("Others");
        var cam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (cam != null) { cam.orthographic = true; cam.backgroundColor = Cream; cam.clearFlags = CameraClearFlags.SolidColor; }

        var canvas = mainCanvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;
        var scaler = mainCanvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // keep the back button, clear everything else under MainCanvas (re-runnable)
        var backButton = mainCanvasGo.transform.Find("PlaySchoolBackButton")?.gameObject
                         ?? mainCanvasGo.transform.Find("PlayschoolBackButton")?.gameObject
                         ?? mainCanvasGo.transform.Find("BGPanel/PlaySchoolBackButton")?.gameObject;
        var toDelete = new List<GameObject>();
        foreach (Transform c in mainCanvasGo.transform) if (c.gameObject != backButton && c.name != "BGPanel") toDelete.Add(c.gameObject);
        foreach (var g in toDelete) Object.DestroyImmediate(g);
        var oldBg = mainCanvasGo.transform.Find("BGPanel");
        if (oldBg != null) { if (backButton != null && backButton.transform.parent == oldBg) backButton.transform.SetParent(mainCanvasGo.transform, false); Object.DestroyImmediate(oldBg.gameObject); }

        var mc = (RectTransform)mainCanvasGo.transform;

        // ---------------------------------------------------------------- BGPanel (full-screen garden art) + back button
        var bgPanel = Img(mc, "BGPanel", null, Cream);
        Stretch(bgPanel.rectTransform);
        var bgArtImg = Img(bgPanel.rectTransform, "BGArt", bgArt, Color.white); bgArtImg.raycastTarget = false;
        Place(bgArtImg.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1080, 1920)); bgArtImg.rectTransform.pivot = new Vector2(0.5f, 1f);
        var bgAligner = bgArtImg.gameObject.AddComponent<BackgroundAligner>(); // covers the screen and keeps the lawn line under the garden on every aspect
        if (backButton == null) { backButton = Img(bgPanel.rectTransform, "PlaySchoolBackButton", backBtn, Color.white).gameObject; backButton.AddComponent<Button>(); }
        // above the gameplay (the tree canopy reaches the top-left corner), not inside BGPanel
        backButton.transform.SetParent(mc, false);
        backButton.name = "PlaySchoolBackButton";
        // same placement as BuildABot / TugOfWar: top-left, pivot (0,1), 50px inset, 125px
        var backImg = backButton.GetComponent<Image>(); if (backImg != null) { backImg.sprite = backBtn; backImg.color = Color.white; backImg.preserveAspect = true; }
        var backRt = (RectTransform)backButton.transform;
        backRt.anchorMin = backRt.anchorMax = new Vector2(0, 1); backRt.pivot = new Vector2(0, 1);
        backRt.anchoredPosition = new Vector2(50, -50); backRt.sizeDelta = new Vector2(125, 125);
        AddSafeArea(backButton, SafeAreaFitter.Mode.PushIn); // background stays full-screen, the button avoids the notch

        // ---------------------------------------------------------------- LevelMain / Level1
        var levelMain = Empty(mc, "LevelMain"); Stretch(levelMain);
        AddSafeArea(levelMain.gameObject, SafeAreaFitter.Mode.FitRect); // all gameplay UI inside the safe area
        var level1 = Empty(levelMain, "Level1"); Stretch(level1);
        var levelController = level1.gameObject.AddComponent<ButterflyLevelController>();

        // Garden (top 40%): sky and hedge come from BG_3 behind it. Everything happens on one branch growing out of the tree;
        // GardenView zooms the tree between "whole tree" and "branch" (TreeImage is bottom-left anchored and pivoted, art px units).
        var gardenRt = Empty(level1, "GardenView");
        SetField(bgAligner, "gardenView", gardenRt);
        gardenRt.anchorMin = new Vector2(0, 0.6f); gardenRt.anchorMax = Vector2.one; gardenRt.offsetMin = gardenRt.offsetMax = Vector2.zero;
        var garden = gardenRt.gameObject.AddComponent<GardenView>();
        // TreeRoot = shadow + FullTreeNew (+ branch), at the tree's native size (1024x1578). Pivot bottom-left (tree px coordinates),
        // anchored to the garden's TOP-left so the canopy stays under the top bar on every phone. Its scene layout is the level-start
        // framing: a big tree whose roots stand at about the middle of the grid (~806 under the garden for the 1080x768 reference
        // garden), trunk filling the left edge (its right edge, tree x 398, at screen x ~110), the branch centred in the garden
        // (its middle ~330 above the garden bottom). Scale = (roots-to-branch distance) / (knot height 728 tree px) = 1.35.
        // GardenView zooms in from here (~12%: trunk slides out left, branch centred) and never zooms out.
        const float TreeScale = 1.35f;
        var treeRoot = Empty(gardenRt, "TreeRoot");
        treeRoot.anchorMin = treeRoot.anchorMax = new Vector2(0, 1); treeRoot.pivot = Vector2.zero;
        treeRoot.sizeDelta = new Vector2(1024, 1578);
        treeRoot.anchoredPosition = new Vector2(110f - 398f * TreeScale, -806f - 768f); // measured from the garden's top-left
        treeRoot.localScale = new Vector3(TreeScale, TreeScale, 1f);
        // soft ground shadow under the roots (base spans tree px x 67..560 at y ~1540), drawn behind the trunk
        var treeShadow = Img(treeRoot, "TreeShadow", treeShadowArt, new Color(0f, 0f, 0f, 0.25f)); treeShadow.raycastTarget = false; treeShadow.preserveAspect = true;
        Place(treeShadow.rectTransform, Vector2.zero, new Vector2(310f, 34f), new Vector2(660f, 133f));
        var tree = Prop(treeRoot, "TreeImage", treeArt, Vector2.zero, Vector2.zero, new Vector2(1024, 1578), Vector2.zero);

        // Branch_2 (1624x680 art) grows out of the trunk just under the canopy and reaches right across the garden
        // (reference mock-up shared in chat - about 0.64 tree px per art px, holder top-left at tree px (255, 390 from top)).
        // Inside the holder everything keeps the old layout units (art px * 0.65). HolderScale = 0.85 screen px per holder unit at
        // level start: the branch takes ~85% of the width, so the zoom-in has room to grow it until it fills the width.
        const float BranchScale = 0.65f, HolderScale = 0.85f / TreeScale;
        var branchHolder = Empty(tree, "BranchHolder");
        branchHolder.anchorMin = branchHolder.anchorMax = Vector2.zero; branchHolder.pivot = new Vector2(0, 1);
        branchHolder.sizeDelta = new Vector2(1624 * BranchScale, 680 * BranchScale);
        branchHolder.localScale = new Vector3(HolderScale, HolderScale, 1);
        // branch base (art 30,615 = holder 19.5,-399.75) set just inside the trunk at the knot: tree px (330, 850 from the top)
        branchHolder.anchoredPosition = new Vector2(330f, 1578f - 850f) - new Vector2(30f * BranchScale, -615f * BranchScale) * HolderScale;
        var branch = Prop(branchHolder, "Branch", gardenBranchArt, new Vector2(0, 1), Vector2.zero, new Vector2(1624 * BranchScale, 680 * BranchScale), new Vector2(0, 1));
        var branchImg = branch.GetComponent<Image>();
        branchImg.type = Image.Type.Filled; branchImg.fillMethod = Image.FillMethod.Horizontal; branchImg.fillOrigin = (int)Image.OriginHorizontal.Left; branchImg.fillAmount = 1f;
        // (anchor in art px, angle, length, mirrored, when it appears); mirrored leaves point left (angle = direction - 180).
        // Anchors are measured from Branch_2's alpha: the centre of each twig end, moved ~14 px back along the twig so the twig
        // runs under the leaf's stem end. Angles follow each twig's direction (curl 102, twig1 63, twig2 -10, twig3 70, twig4 -7).
        // Leaf1 (the egg / caterpillar leaf) leans a bit flatter than its twig so they can sit on it.
        // Negative "at" = on the branch from stage 1 (watered from dry to green).
        var leafDefs = new (Vector2 tip, float angle, float len, bool mirror, float at)[]
        {
            (new Vector2(706, 272), -62f, 150f, true, -1f),
            (new Vector2(881, 221), 45f, 210f, false, -1f),
            (new Vector2(1241, 445), -12f, 150f, false, 0.3f),
            (new Vector2(1449, 17), 60f, 140f, false, 0.55f),
            (new Vector2(1597, 255), -3f, 140f, false, 0.8f),
        };
        var branchLeaves = new Object[leafDefs.Length]; var leafAt = new float[leafDefs.Length];
        RectTransform bigLeaf = null;
        for (int i = 0; i < leafDefs.Length; i++)
        {
            var d = leafDefs[i];
            var leaf = Prop(branch, "Leaf" + i, branchLeafArt, new Vector2(0, 1), new Vector2(d.tip.x, -d.tip.y) * BranchScale, new Vector2(d.len, d.len * 290f / 425f), new Vector2(0.06f, 1f - 178f / 290f)); // pivot = stem end
            leaf.localScale = new Vector3(d.mirror ? -1 : 1, 1, 1);
            leaf.localRotation = Quaternion.Euler(0, 0, d.angle);
            branchLeaves[i] = leaf; leafAt[i] = d.at;
            if (i == 1) bigLeaf = leaf;
        }
        // Egg and caterpillar sit on the top centre of the big leaf (Leaf.png 425x290: top edge peaks at x 220, y ~1).
        // Pivots are at the bottom of the visible art so they rest on the leaf; they follow the leaf's tilt and scale.
        // On the 45-degree leaf they rest on its upper face (between the midrib and the top edge), not on the rim.
        var leafSeat = new Vector2(0.55f, 0.62f);
        var egg = Prop(bigLeaf, "Egg", eggArt, leafSeat, Vector2.zero, new Vector2(230, 230), new Vector2(0.5f, 1f - 693f / 1024f));          // Egg_2 art bottom at y 693
        var cat = Prop(bigLeaf, "Caterpillar", caterpillarArt, leafSeat, Vector2.zero, new Vector2(190, 190), new Vector2(0.5f, 1f - 810f / 1024f)); // feet at y ~810
        // the leaf leans 45 degrees: turn them back so they sit nearly upright on it (10 degrees in world space)
        egg.localRotation = Quaternion.Euler(0, 0, -35f); cat.localRotation = Quaternion.Euler(0, 0, -35f);
        // Level 3: the fed caterpillar lies along the top of the branch (top edge ~art y 320 at x 800, 350 at x 1000), inside
        // the stub (fill 0.65 = art x 1056). The seat is a little below the edge so its feet sink into the bark, tilted to the slope.
        var catSeat = Empty(branchHolder, "CaterpillarSeat");
        catSeat.anchorMin = catSeat.anchorMax = new Vector2(0, 1); catSeat.pivot = new Vector2(0.5f, 0.5f);
        catSeat.anchoredPosition = new Vector2(880 * BranchScale, -352 * BranchScale); catSeat.sizeDelta = new Vector2(10, 10);
        catSeat.localRotation = Quaternion.Euler(0, 0, -8f);
        // hangs under the branch at art (1000, 405); pivot at the top of the visible chrysalis (art y 46)
        var chry = Prop(branchHolder, "Chrysalis", chrysalisArt, new Vector2(0, 1), new Vector2(1000 * BranchScale, -405 * BranchScale), new Vector2(190, 190), new Vector2(0.5f, 1f - 46f / 1024f));

        // Nectar level: the tree is hidden; two big flowering bushes in a holder anchored + pivoted bottom-centre.
        // The holder's scene scale is the zoomed-in view; GardenView scales it down to zoom out. Each bush also grows with progress.
        var bushHolder = Empty(gardenRt, "BushHolder");
        bushHolder.anchorMin = bushHolder.anchorMax = bushHolder.pivot = new Vector2(0.5f, 0); bushHolder.anchoredPosition = Vector2.zero; bushHolder.sizeDelta = new Vector2(1080, 600);
        var flowers = new RectTransform[2];
        float[] flowerX = { -270, 270 };
        for (int i = 0; i < flowers.Length; i++) flowers[i] = Prop(bushHolder, "Flower" + i, i % 2 == 0 ? bushesArt : bushes2Art, new Vector2(0.5f, 0), new Vector2(flowerX[i], 20), new Vector2(720, 720), new Vector2(0.5f, 0.2f));
        // Sorting: the creatures draw in front of the game panel's text and board (own canvases, override sorting);
        // the night tint, caption, flying items, FX, tutorial hand and final panel sit above them.
        foreach (var creature in new[] { egg, cat, chry }) SortAbove(creature, SortCreatures);
        var bfly = Prop(gardenRt, "Butterfly", butterflyArt, new Vector2(0.5f, 0.5f), new Vector2(230, 80), new Vector2(200, 200), new Vector2(0.5f, 0.5f));
        SortAbove(bfly, SortCreatures);
        var night = Img(gardenRt, "NightOverlay", null, new Color(0.1f, 0.12f, 0.3f, 0f)); night.raycastTarget = false;
        SortAbove(night, SortNight);
        night.rectTransform.anchorMin = new Vector2(0, -2f); night.rectTransform.anchorMax = new Vector2(1, 1.5f); night.rectTransform.offsetMin = night.rectTransform.offsetMax = Vector2.zero; // garden is 40% of the screen: -1.5 reaches the bottom
        var deliveryTarget = Empty(gardenRt, "DeliveryTarget"); Place(deliveryTarget, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
        var levelPill = Img(gardenRt, "LevelPill", null, Hex("#FFFDF1")); Pill(levelPill, 90); Place(levelPill.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(260, 90));
        var dotsRt = Empty(gardenRt, "RoundDots"); Place(dotsRt, new Vector2(0.5f, 1), new Vector2(0, -178), new Vector2(200, 30));
        var dotsLayout = dotsRt.gameObject.AddComponent<HorizontalLayoutGroup>(); dotsLayout.spacing = 16; dotsLayout.childAlignment = TextAnchor.MiddleCenter;
        dotsLayout.childControlWidth = dotsLayout.childControlHeight = false; dotsLayout.childForceExpandWidth = dotsLayout.childForceExpandHeight = false;
        var roundDots = new Object[4];
        for (int i = 0; i < 4; i++)
        {
            var dot = Img(dotsRt, "Dot" + i, circle, Color.white); dot.raycastTarget = false; dot.rectTransform.sizeDelta = new Vector2(22, 22);
            var dotShadow = dot.gameObject.AddComponent<Shadow>(); dotShadow.effectColor = new Color(0.25f, 0.4f, 0.28f, 0.35f); dotShadow.effectDistance = new Vector2(0, -3);
            roundDots[i] = dot;
        }
        var levelText = Text(levelPill.rectTransform, "LevelText", "LEVEL 1", 36, Hex("#658267"), TextAlignmentOptions.Center); Stretch(levelText.rectTransform, 30, 12);
        levelText.color = Hex("#657867"); // label colour picked in the scene
        var captionPill = Img(gardenRt, "CaptionPill", null, new Color(1f, 0.99f, 0.945f, 0.92f)); Pill(captionPill, 90);
        captionPill.rectTransform.anchorMin = new Vector2(0, 1); captionPill.rectTransform.anchorMax = new Vector2(1, 1); captionPill.rectTransform.offsetMin = new Vector2(200, -285); captionPill.rectTransform.offsetMax = new Vector2(-200, -195);
        var captionText = Text(captionPill.rectTransform, "Caption", "", 28, Hex("#648261"), TextAlignmentOptions.Center); Stretch(captionText.rectTransform, 45, 10);
        captionText.enableAutoSizing = true; captionText.fontSizeMin = 18; captionText.fontSizeMax = 28;
        SortAbove(captionPill, SortCaption); // before deactivating: override sorting doesn't stick on an inactive canvas
        captionPill.gameObject.SetActive(false); // only shown while a stage transformation plays
        // restart: same 125px top-corner button as the back button (mirrored to the right), BuildABot/TugOfWar style
        var restartImg = Img(gardenRt, "RestartButton", restartBtn, Color.white); restartImg.preserveAspect = true;
        restartImg.rectTransform.anchorMin = restartImg.rectTransform.anchorMax = new Vector2(1, 1); restartImg.rectTransform.pivot = new Vector2(1, 1);
        restartImg.rectTransform.anchoredPosition = new Vector2(-50, -50); restartImg.rectTransform.sizeDelta = new Vector2(125, 125);
        var restartButton = restartImg.gameObject.AddComponent<Button>();

        SetField(garden, "tree", treeRoot); SetField(garden, "bushHolder", bushHolder);
        SetField(garden, "branch", branch); SetField(garden, "egg", egg); SetField(garden, "caterpillar", cat);
        SetField(garden, "chrysalis", chry); SetField(garden, "butterfly", bfly); SetField(garden, "flowers", flowers);
        SetField(garden, "nightOverlay", night); SetField(garden, "deliveryTarget", deliveryTarget); SetField(garden, "caption", captionText);
        SetField(garden, "branchImage", branchImg); SetField(garden, "branchHolder", branchHolder); SetField(garden, "caterpillarBranchSeat", catSeat); SetField(garden, "branchLeaves", branchLeaves);
        SetFloats(garden, "branchLeafThresholds", leafAt);
        { var gso = new SerializedObject(garden); gso.FindProperty("branchStubFill").floatValue = 1f; gso.ApplyModifiedPropertiesWithoutUndo(); } // full branch from level 1
        SetField(garden, "dryLeafSprite", branchDryLeafArt); SetField(garden, "freshLeafSprite", branchLeafArt);

        // Game panel (bottom 60%)
        var gamePanel = Empty(level1, "GamePanel");
        gamePanel.anchorMin = Vector2.zero; gamePanel.anchorMax = new Vector2(1, 0.6f); gamePanel.offsetMin = gamePanel.offsetMax = Vector2.zero;
        var objective = Text(gamePanel, "Objective", "Water the dry leaves.", 46, Hex("#2F5E36"), TextAlignmentOptions.Center); StretchX(objective.rectTransform, -60, 80, 40); objective.enableAutoSizing = true; objective.fontSizeMin = 28; objective.fontSizeMax = 46;

        var goalsRt = Empty(gamePanel, "Goals"); StretchX(goalsRt, -170, 120, 20);
        var hlg = goalsRt.gameObject.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleCenter; hlg.childControlWidth = false; hlg.childControlHeight = false; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        var goalsPanel = goalsRt.gameObject.AddComponent<GoalsPanel>();
        var chip = Img(goalsRt, "GoalChip", null, GoalYellow); Round(chip, 30); chip.rectTransform.sizeDelta = new Vector2(250, 110);
        var chipIcon = BuildItemView(chip.rectTransform, "ItemIcon", 86, false); Place(chipIcon.Rect, new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(86, 86));
        chipIcon.gameObject.SetActive(true); chipIcon.transform.Find("TierBadge")?.gameObject.SetActive(false);
        var chipCount = Text(chip.rectTransform, "Count", "0/1", 44, Hex("#A47A4B"), TextAlignmentOptions.Center); Place(chipCount.rectTransform, new Vector2(0, 0.5f), new Vector2(178, 0), new Vector2(130, 70));
        SetField(goalsPanel, "container", goalsRt); SetField(goalsPanel, "chipTemplate", chip.gameObject);

        var boardArea = Empty(gamePanel, "BoardArea"); boardArea.anchorMin = Vector2.zero; boardArea.anchorMax = Vector2.one; boardArea.offsetMin = new Vector2(50, 45); boardArea.offsetMax = new Vector2(-50, -245);
        var boardImg = Img(boardArea, "Board", null, new Color(1f, 1f, 1f, 0.45f)); Round(boardImg, 40); Place(boardImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
        var grid = boardImg.gameObject.AddComponent<GridLayoutGroup>();
        var board = boardImg.gameObject.AddComponent<MergeBoard>();
        var cellImg = Img(boardImg.rectTransform, "CellTemplate", null, new Color(1f, 0.976f, 0.92f, 0.9f)); Round(cellImg, 26);
        var outline = cellImg.gameObject.AddComponent<Outline>(); outline.effectColor = Hex("#FFB56C"); outline.effectDistance = new Vector2(5, -5); outline.enabled = false;
        cellImg.gameObject.AddComponent<MergeCell>();
        var itemTemplate = BuildItemView(boardArea, "ItemTemplate", 200, false);
        itemTemplate.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the cell is the tile: no second box, still raycastable

        var dragLayer = Empty(level1, "DragLayer"); Stretch(dragLayer);
        var flightLayer = Empty(level1, "FlightLayer"); Stretch(flightLayer);
        SortAbove(flightLayer, SortFlight); // goal items fly over the garden creatures
        var hand = Img(level1, "TutorialHand", handSprite, Color.white); hand.preserveAspect = true; hand.raycastTarget = false;
        SortAbove(hand, SortTop);
        Place(hand.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 170)); hand.rectTransform.pivot = new Vector2(0.42f, 0.9f); // fingertip on the target
        var handCg = hand.gameObject.AddComponent<CanvasGroup>(); handCg.blocksRaycasts = false;
        BuildFxLayer(level1);

        SetField(board, "boardRect", boardImg.rectTransform); SetField(board, "grid", grid); SetField(board, "cellTemplate", cellImg.GetComponent<MergeCell>());
        SetField(board, "itemTemplate", itemTemplate); SetField(board, "dragLayer", dragLayer); SetField(board, "fallbackItemSprite", circle);

        SetField(levelController, "garden", garden); SetField(levelController, "board", board); SetField(levelController, "goalsPanel", goalsPanel); SetField(levelController, "flightLayer", flightLayer);
        SetField(levelController, "levelLabel", levelText); SetField(levelController, "objectiveText", objective); SetField(levelController, "instructionText", null); SetField(levelController, "hintText", null); SetField(levelController, "restartButton", restartButton);
        SetField(levelController, "subDots", roundDots);
        SetField(levelController, "levelPill", levelPill.rectTransform); SetField(levelController, "goalsRect", goalsRt); SetField(levelController, "boardRect", boardImg.rectTransform);

        // ---------------------------------------------------------------- final celebration panel
        var finalImg = Img(mc, "FinalCelebrationPanel", null, new Color(0.26f, 0.4f, 0.32f, 0.7f)); Stretch(finalImg.rectTransform);
        var finalCg = finalImg.gameObject.AddComponent<CanvasGroup>();
        var finalPanel = finalImg.gameObject.AddComponent<FinalCelebrationPanel>();
        SortAbove(finalImg, SortTop, true); // above the creatures; still blocks taps on the board
        var modal = Img(finalImg.rectTransform, "Modal", null, Hex("#FFF9E8")); Round(modal, 60); modal.gameObject.AddComponent<FitInsideParent>(); /* shrinks on narrow screens (foldables) */ Place(modal.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 1000));
        var finalTitle = Text(modal.rectTransform, "Title", "Butterfly Garden Helper!", 52, Hex("#EA936D"), TextAlignmentOptions.Center); Place(finalTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(860, 80));
        var cycleRow = Empty(modal.rectTransform, "LifeCycle"); Place(cycleRow, new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(880, 220));
        string[] stepNames = { "Egg", "Caterpillar", "Chrysalis", "Butterfly" };
        Sprite[] stepArt = { eggOnLeafArt, caterpillarArt, chrysalisArt, butterflyArt };
        var steps = new RectTransform[4];
        for (int i = 0; i < 4; i++)
        {
            steps[i] = Prop(cycleRow, "Step" + i, stepArt[i], new Vector2(0, 0.5f), new Vector2(110 + i * 220, 15), new Vector2(190, 190), new Vector2(0.5f, 0.5f));
            var stepLabel = Text(steps[i], "Label", stepNames[i], 26, Hex("#78967C"), TextAlignmentOptions.Center); Place(stepLabel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(200, 40));
            if (i < 3) { var arrow = Text(cycleRow, "Arrow" + i, ">", 48, Hex("#EA936D"), TextAlignmentOptions.Center); Place(arrow.rectTransform, new Vector2(0, 0.5f), new Vector2(220 + i * 220, 15), new Vector2(60, 60)); }
        }
        var badge = Img(modal.rectTransform, "Badge", star, Color.white); Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(320, 320));
        var badgeLabel = Text(badge.rectTransform, "Label", "GARDEN\nHELPER", 30, Hex("#8D724D"), TextAlignmentOptions.Center); Place(badgeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(240, 120));
        var reward = Text(modal.rectTransform, "Reward", "You earned a badge and 10 stars!", 34, Hex("#78967C"), TextAlignmentOptions.Center); Place(reward.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(860, 100));
        SetField(finalPanel, "canvasGroup", finalCg); SetField(finalPanel, "lifeCycleSteps", steps); SetField(finalPanel, "badge", badge.rectTransform);
        SetField(finalPanel, "titleText", finalTitle); SetField(finalPanel, "rewardText", reward);
        finalImg.gameObject.SetActive(false);

        // ---------------------------------------------------------------- WinLose prefab (EndPanelScript)
        var winLosePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WinLosePrefabPath);
        var winLose = (GameObject)PrefabUtility.InstantiatePrefab(winLosePrefab, mc);
        var endPanel = GetOrAdd<EndPanelScript>(winLose);
        SetField(endPanel, "winLosePanelFallbackPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(WinLoseCanvasPrefabPath));
        foreach (Transform c in winLose.transform) c.gameObject.SetActive(false);

        // ---------------------------------------------------------------- Story canvas (prefab instance, inactive)
        // GameObject.Find skips inactive objects, so look through the scene roots (the story canvas is saved inactive).
        GameObject storyGo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "StoryCanvas_Root") continue;
            if (storyGo == null) storyGo = root; else Object.DestroyImmediate(root);
        }
        if (storyGo == null)
        {
            var storyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryPrefabPath);
            storyGo = (GameObject)PrefabUtility.InstantiatePrefab(storyPrefab);
            storyGo.transform.SetAsFirstSibling();
        }
        var storyScaler = storyGo.GetComponent<CanvasScaler>();
        storyScaler.referenceResolution = new Vector2(1080, 1920); storyScaler.matchWidthOrHeight = 0.5f;
        var fg = storyGo.transform.Find("Panel_Slide/Image_Foreground") as RectTransform; if (fg != null) { fg.sizeDelta = new Vector2(900, 900); fg.anchoredPosition = new Vector2(0, 150); fg.GetComponent<Image>().preserveAspect = true; }
        var capBg = storyGo.transform.Find("Panel_Slide/Caption_BG") as RectTransform; if (capBg != null) { capBg.sizeDelta = new Vector2(1000, 260); capBg.anchoredPosition = new Vector2(0, 160); var capImg = capBg.GetComponent<Image>(); if (capImg != null) Round(capImg, 50); }
        var capTxt = storyGo.transform.Find("Panel_Slide/Caption_BG/Text_Caption")?.GetComponent<TextMeshProUGUI>(); if (capTxt != null) { capTxt.fontSize = 38; capTxt.enableAutoSizing = true; capTxt.fontSizeMin = 24; capTxt.fontSizeMax = 40; capTxt.alignment = TextAlignmentOptions.Center; Stretch(capTxt.rectTransform, 50, 25); }
        var skip = storyGo.transform.Find("Button_Skip") as RectTransform; if (skip != null) { skip.anchoredPosition = new Vector2(-90, -90); skip.sizeDelta = new Vector2(120, 120); AddSafeArea(skip.gameObject, SafeAreaFitter.Mode.PushIn); }
        if (font != null) foreach (var t in storyGo.GetComponentsInChildren<TextMeshProUGUI>(true)) { t.font = font; t.fontStyle = FontStyles.Normal; }
        var storyController = storyGo.GetComponent<StoryController>();
        storyGo.SetActive(false);

        // ---------------------------------------------------------------- Scripts
        var gm = scriptsRoot.transform.Find("GameManager").GetComponent<GameManager>();
        var lm = scriptsRoot.transform.Find("LevelManager").GetComponent<LevelManager>();
        var smT = scriptsRoot.transform.Find("SoundManager"); var smGo = smT != null ? smT.gameObject : new GameObject("SoundManager");
        smGo.transform.SetParent(scriptsRoot.transform, false);
        var sm = GetOrAdd<SoundManager>(smGo);
        // voice keys come from the code defaults (they mirror AudioXL/Sonu's Little Butterfly - Audio Script.xlsx)
        typeof(SoundManager).GetField("audioMapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(sm, new ButterflyAudioMapper());
        EditorUtility.SetDirty(sm);
        var bgm = GetOrAdd<AudioSource>(Child(smGo.transform, "BGMSource").gameObject); bgm.loop = true; bgm.playOnAwake = false;
        var sfx = GetOrAdd<AudioSource>(Child(smGo.transform, "SFXSource").gameObject); sfx.playOnAwake = false;
        SetField(sm, "bgmSource", bgm); SetField(sm, "sfxSource", sfx);
        var tutT = scriptsRoot.transform.Find("TutorialController"); var tutGo = tutT != null ? tutT.gameObject : new GameObject("TutorialController");
        tutGo.transform.SetParent(scriptsRoot.transform, false);
        var tut = GetOrAdd<TutorialController>(tutGo);
        SetField(tut, "hand", hand.rectTransform); SetField(tut, "handCanvasGroup", handCg); SetField(tut, "levelController", levelController);

        SetField(gm, "levelManager", lm); SetField(gm, "soundManager", sm); SetField(gm, "endPanelScript", endPanel); SetField(gm, "tutorialController", tut);
        SetField(lm, "playSchoolBackButton", backButton.GetComponent<Button>()); SetField(lm, "database", db); SetField(lm, "levelController", levelController);
        SetField(lm, "storyController", storyController); SetField(lm, "introStory", AssetDatabase.LoadAssetAtPath<StoryData>(StoryFolder + "/Story_Intro.asset")); SetField(lm, "finalPanel", finalPanel);

        // sibling order: BGPanel, LevelMain, PlaySchoolBackButton (over the garden), FinalCelebrationPanel, WinLose
        bgPanel.transform.SetSiblingIndex(0); levelMain.SetSiblingIndex(1); backButton.transform.SetSiblingIndex(2); finalImg.transform.SetSiblingIndex(3); winLose.transform.SetSiblingIndex(4);
        hand.transform.SetAsLastSibling();
        hand.gameObject.SetActive(false);

        // build settings: enable gameplay scene first
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in scenes) if (s.path == ScenePath) { s.enabled = true; found = true; }
        if (!found) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ButterflySceneBuilder] Scene built and saved.");
    }

    // ================================================================== data
    static LevelDatabase BuildLevelDatabase()
    {
        if (!AssetDatabase.IsValidFolder(SoFolder)) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DbPath);
        if (db == null) { db = ScriptableObject.CreateInstance<LevelDatabase>(); AssetDatabase.CreateAsset(db, DbPath); }
        db.themes = new[]
        {
            Theme(MergeTheme.Water, LifeStage.Egg, "Grow a healthy leaf!", "Keep the egg safe on a fresh leaf.", Hex("#76CCE8"), new[] { waterDropArt, waterDropArt, waterDropArt, waterDropArt }, "Tiny Water Drop", "Water Drop", "Large Water Drop", "Water Bundle"),
            Theme(MergeTheme.Leaf, LifeStage.Caterpillar, "Feed the hungry caterpillar!", "Help the caterpillar grow big and strong.", Hex("#80C978"), new[] { freshHalfLeafArt, freshLeafArt, branchLeafArt, halfPlantArt }, "Tiny Leaf", "Small Leaf", "Healthy Leaf", "Leaf Bundle"),
            Theme(MergeTheme.Twig, LifeStage.Chrysalis, "Build a strong branch!", "Help the caterpillar find a safe place.", Hex("#B98760"), new[] { branchArt, branchArt, branchArt, branchArt }, "Tiny Twig", "Small Twig", "Strong Branch", "Leafy Branch"),
            // level 4 feeds water to the flowers (the merge items are water drops; the garden's flowering bushes grow)
            Theme(MergeTheme.Flower, LifeStage.Butterfly, "Grow nectar-rich flowers!", "Help the butterfly stay healthy.", Hex("#76CCE8"), new[] { waterDropArt, waterDropArt, waterDropArt, waterDropArt }, "Tiny Water Drop", "Water Drop", "Large Water Drop", "Water Bundle"),
        };
        // 4 main levels like the HTML prototype (one per life-cycle stage). Each has 3-4 short rounds;
        // the garden keeps growing across the rounds and the win panel only shows after the last one.
        db.mainLevels = new[]
        {
            M("Grow a healthy leaf!", MergeTheme.Water,
                L("Water the dry leaves.", MergeTheme.Water, 3, 3, G(2, 1)),
                L("Give the leaves more water.", MergeTheme.Water, 3, 3, G(2, 2)),
                L("Help the leaves grow big and green.", MergeTheme.Water, 3, 3, G(3, 1)),
                L("Finish the butterfly's leaf home.", MergeTheme.Water, 3, 4, G(3, 2))),
            M("Feed the hungry caterpillar!", MergeTheme.Leaf,
                L("Feed the baby caterpillar.", MergeTheme.Leaf, 3, 3, G(2, 2)),
                L("Help the caterpillar grow bigger.", MergeTheme.Leaf, 3, 4, G(3, 1)),
                L("Give it a big final meal.", MergeTheme.Leaf, 4, 4, G(4, 1))),
            M("Build a strong branch!", MergeTheme.Twig,
                L("Prepare small twigs.", MergeTheme.Twig, 3, 4, G(2, 2)),
                L("Make a strong branch.", MergeTheme.Twig, 4, 4, G(3, 1)),
                L("Build a leafy hanging place.", MergeTheme.Twig, 4, 4, G(4, 1))),
            M("Grow nectar-rich flowers!", MergeTheme.Flower,
                L("Water the flower buds.", MergeTheme.Flower, 3, 4, G(2, 2)),
                L("Help the flowers bloom.", MergeTheme.Flower, 4, 4, G(3, 1)),
                L("Make the flowers nectar-rich.", MergeTheme.Flower, 4, 4, G(4, 1))),
        };
        db.mainLevels[0].subLevels[0].isTutorial = true;
        EditorUtility.SetDirty(db);
        return db;
    }

    static void BuildStories(LevelDatabase db)
    {
        if (!AssetDatabase.IsValidFolder(StoryFolder)) AssetDatabase.CreateFolder(SoFolder, "Stories");
        // First-time intro: 3 slides = 3 voice lines (story1-story3 in AudioXL). Sprites are placeholders from the game art for now.
        Story("Story_Intro",
            S("One morning, Sonu spots a beautiful butterfly in Gokuldham garden, looking for a safe leaf to lay her egg.", butterflyArt),
            S("Sonu: \"Mummy, can we help?\" Madhavi: \"Yes! The egg needs a healthy leaf, the caterpillar needs food, and the butterfly needs nectar.\"", sonu),
            S("Help Sonu care for the tree and watch the butterfly grow, from a tiny egg to a beautiful butterfly!", treeArt));
        db.GetTheme(MergeTheme.Water).stageEndStory = Story("Story_Stage1_End",
            S("The mother butterfly lands on the healthy leaf and lays an egg. After a few days, a tiny caterpillar hatches!", eggOnLeafArt),
            S("Sonu: \"Yay! The butterfly found a home for her egg!\" Now the hungry caterpillar needs lots of leaves to eat.", caterpillarArt));
        db.GetTheme(MergeTheme.Leaf).stageEndStory = Story("Story_Stage2_End",
            S("The caterpillar ate well and grew big. Now it is ready to transform, but it needs a safe place to hang.", caterpillarArt),
            S("Let us prepare strong twigs and branches so the caterpillar can form its chrysalis.", branchArt));
        db.GetTheme(MergeTheme.Twig).stageEndStory = Story("Story_Stage3_End",
            S("The caterpillar hangs beneath the strong branch and forms a chrysalis. Days pass as it changes inside.", chrysalisArt),
            S("Madhavi: \"Amazing, Sonu! The caterpillar has transformed into a butterfly!\" Now it needs nectar-rich flowers.", butterflyArt));
        db.GetTheme(MergeTheme.Flower).stageEndStory = Story("Story_Stage4_End",
            S("The butterfly drinks nectar from the flowers you grew and flies happily around Gokuldham garden.", butterflyArt),
            S("Sonu: \"We did it! Our tiny egg has become a beautiful butterfly!\" Madhavi: \"Now you know how a butterfly grows!\"", sonuSmall));
        EditorUtility.SetDirty(db);
    }

    static StoryData Story(string name, params StorySlide[] slides)
    {
        string path = StoryFolder + "/" + name + ".asset";
        var so = AssetDatabase.LoadAssetAtPath<StoryData>(path);
        if (so == null) { so = ScriptableObject.CreateInstance<StoryData>(); AssetDatabase.CreateAsset(so, path); }
        so.slides = slides; so.isSkippable = true;
        EditorUtility.SetDirty(so);
        return so;
    }
    static StorySlide S(string caption, Sprite fg) => new StorySlide { backgroundSprite = storyBg, foregroundSprite = fg, captionText = caption, displayDuration = 4f, transitionIn = SlideTransitionType.FadeIn, transitionOut = SlideTransitionType.FadeOut };
    static ThemeData Theme(MergeTheme t, LifeStage s, string title, string caption, Color tint, Sprite[] sprites, params string[] names) => new ThemeData { theme = t, stage = s, stageTitle = title, gardenCaption = caption, tint = tint, tierNames = names, tierSprites = sprites };
    static MainLevel M(string title, MergeTheme t, params LevelDefinition[] subs) => new MainLevel { title = title, theme = t, subLevels = subs };
    static LevelDefinition L(string obj, MergeTheme t, int cols, int rows, params GoalRequirement[] goals) => new LevelDefinition { objective = obj, theme = t, columns = cols, rows = rows, goals = goals };
    static GoalRequirement G(int tier, int count) => new GoalRequirement { tier = tier, count = count };

    // ================================================================== ui helpers
    static void LoadAssets()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        EnsureSprite(CirclePath, Vector4.zero); EnsureSprite(RoundedPath, new Vector4(100, 120, 100, 100)); EnsureSprite(CloudPath, Vector4.zero); EnsureSprite(ResetPath, Vector4.zero);
        EnsureSprite(StarPath, Vector4.zero); EnsureSprite(StoryBgPath, Vector4.zero); EnsureSprite(SonuPath, Vector4.zero); EnsureSprite(SonuSmallPath, Vector4.zero);
        circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath); rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath); cloud = AssetDatabase.LoadAssetAtPath<Sprite>(CloudPath);
        resetIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ResetPath); star = AssetDatabase.LoadAssetAtPath<Sprite>(StarPath); storyBg = AssetDatabase.LoadAssetAtPath<Sprite>(StoryBgPath);
        sonu = AssetDatabase.LoadAssetAtPath<Sprite>(SonuPath); sonuSmall = AssetDatabase.LoadAssetAtPath<Sprite>(SonuSmallPath) ?? sonu;

        EnsureSprite(FrameRoundPath, FrameRoundBorder); EnsureSprite(FramePillPath, FramePillBorder);
        frameRound = AssetDatabase.LoadAssetAtPath<Sprite>(FrameRoundPath); framePill = AssetDatabase.LoadAssetAtPath<Sprite>(FramePillPath);
        backBtn = LoadSprite(BackButtonPath); restartBtn = LoadSprite(RestartButtonPath); handSprite = LoadSprite(TutorialHandPath);
        bgArt = LoadSprite(BgArtPath); treeArt = LoadSprite(TreeArtPath); treeShadowArt = LoadSprite(TreeShadowArtPath); butterflyArt = LoadSprite(ButterflyArtPath); caterpillarArt = LoadSprite(CaterpillarArtPath);
        chrysalisArt = LoadSprite(ChrysalisArtPath); eggOnLeafArt = LoadSprite(EggOnLeafArtPath); eggArt = LoadSprite(EggArtPath);
        waterDropArt = LoadSprite(WaterDropArtPath); bushesArt = LoadSprite(BushesArtPath); bushes2Art = LoadSprite(Bushes2ArtPath);
        branchArt = LoadSprite(BranchArtPath); gardenBranchArt = LoadSprite(GardenBranchArtPath); branchLeafArt = LoadSprite(BranchLeafArtPath); branchDryLeafArt = LoadSprite(BranchDryLeafArtPath);
        plantArt = LoadSprite(PlantArtPath); halfPlantArt = LoadSprite(HalfPlantArtPath);
        freshLeafArt = LoadSprite(FreshLeafArtPath); freshHalfLeafArt = LoadSprite(FreshHalfLeafArtPath);
        dryLeafArt = LoadSprite(DryLeafArtPath); dryHalfLeafArt = LoadSprite(DryHalfLeafArtPath);
        storyBg = bgArt != null ? bgArt : storyBg;
    }
    static Sprite LoadSprite(string path)
    {
        EnsureSprite(path, Vector4.zero);
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) Debug.LogWarning("[ButterflySceneBuilder] sprite missing: " + path);
        return sp;
    }
    static void EnsureSprite(string path, Vector4 border)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        bool dirty = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (border != Vector4.zero && imp.spriteBorder != border) { imp.spriteBorder = border; dirty = true; }
        if (dirty) imp.SaveAndReimport();
    }
    static void ApplyPlayerSettings()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
    }

    static RectTransform Empty(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static Transform Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null) { var go = new GameObject(name); go.transform.SetParent(parent, false); t = go.transform; }
        return t;
    }
    static Image Img(Transform parent, string name, Sprite sprite, Color color)
    {
        var rt = Empty(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite; img.color = color;
        if (sprite == circle) img.preserveAspect = false;
        return img;
    }
    static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
    {
        var rt = Empty(parent, name);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align; tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Normal; // Poppins SemiBold only, no faux bold
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.overflowMode = TextOverflowModes.Overflow;
        // shrink to fit the box, never grow past the design size
        tmp.enableAutoSizing = true; tmp.fontSizeMax = size; tmp.fontSizeMin = Mathf.Max(12f, Mathf.Round(size * 0.55f));
        tmp.margin = Vector4.zero;
        return tmp;
    }
    static RectTransform Prop(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var img = Img(parent, name, sprite, color);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var label = Text(rt, "Label", name, 22, new Color(0.2f, 0.25f, 0.2f, 0.85f), TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return rt;
    }
    /// <summary>Art prop: white, aspect-preserved, no placeholder label.</summary>
    static RectTransform Prop(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var img = Img(parent, name, sprite, Color.white);
        img.raycastTarget = false; img.preserveAspect = true;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }
    // ---------------------------------------------------------------- pooled UI effects (ParticleImage)
    const string FxSprites = "Assets/AssetKits/ParticleImage/Demo/Sprites/";
    static void BuildFxLayer(Transform parent)
    {
        var layer = Empty(parent, "FxLayer"); Stretch(layer);
        SortAbove(layer, SortFx);
        var fx = layer.gameObject.AddComponent<UiFx>();
        var star = LoadSprite(FxSprites + "4Star.png");
        var puff = LoadSprite(FxSprites + "PuffCloud.png");
        var confetti = LoadSprite(FxSprites + "Confetti.png");
        var glow = LoadSprite(FxSprites + "CircleGlow.png");

        // merge: golden/white stars bursting out of the cell
        var sparkle = Emitter(layer, "Sparkle", star, 14, 2.2f, 5f, 0.45f, 0.8f, 140f, 220f, Hex("#FFC23D"), Hex("#FFF4B0"), 0f, 30f, 360f);
        // wrong move: small soft grey puff
        var puffFx = Emitter(layer, "Puff", puff, 6, 0.7f, 1.8f, 0.3f, 0.5f, 110f, 170f, new Color(0.8f, 0.77f, 0.73f, 0.75f), new Color(0.95f, 0.94f, 0.92f, 0.7f), 0f, 25f, 60f);
        // goal delivered: colourful confetti with gravity
        var conf = Emitter(layer, "Confetti", confetti, 30, 3.5f, 8f, 1f, 1.5f, 90f, 130f, Color.white, Color.white, -10f, 15f, 540f);
        conf.textureSheetEnabled = true; conf.textureTile = new Vector2Int(5, 5); conf.textureSheetType = SheetType.FPS; conf.textureSheetFPS = 1; conf.textureSheetCycles = 1;
        conf.textureSheetStartFrame = new ParticleSystem.MinMaxCurve(0f, 25f); conf.textureSheetFrameOverTime = new ParticleSystem.MinMaxCurve(0f);
        conf.startColor = new ParticleSystem.MinMaxGradient(RainbowGradient()) { mode = ParticleSystemGradientMode.RandomColor };

        var ring = Img(layer, "Ring", glow, Color.white); ring.raycastTarget = false; ring.gameObject.SetActive(false);

        var so = new SerializedObject(fx);
        var arr = so.FindProperty("effects");
        var types = new[] { FxType.Sparkle, FxType.Puff, FxType.Confetti };
        var emitters = new[] { sparkle, puffFx, conf };
        var pools = new[] { 4, 3, 2 };
        arr.arraySize = types.Length;
        for (int i = 0; i < types.Length; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("type").enumValueIndex = (int)types[i];
            el.FindPropertyRelative("template").objectReferenceValue = emitters[i];
            el.FindPropertyRelative("poolSize").intValue = pools[i];
        }
        so.FindProperty("ringTemplate").objectReferenceValue = ring;
        so.FindProperty("ringPoolSize").intValue = 4;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static ParticleImage Emitter(Transform parent, string name, Sprite sprite, int count, float speedMin, float speedMax, float lifeMin, float lifeMax,
        float sizeMin, float sizeMax, Color colA, Color colB, float gravity, float radius, float spin)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(10, 10);
        var pi = go.AddComponent<ParticleImage>();
        pi.raycastTarget = false;
        pi.sprite = sprite;
        pi.PlayMode = AssetKits.ParticleImage.Enumerations.PlayMode.None;
        pi.loop = false;
        pi.duration = 0.1f;
        pi.rateOverTime = 0f;
        pi.AddBurst(0f, count);
        pi.space = Simulation.Local;
        pi.shape = EmitterShape.Circle;
        pi.circleRadius = radius;
        pi.emitOnSurface = false;
        pi.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        pi.speedOverLifetime = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.15f)));
        pi.lifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        var size = new SeparatedMinMaxCurve(1f); size.mainCurve = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax); pi.startSize = size;
        pi.sizeOverLifetime = new SeparatedMinMaxCurve(new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.15f, 1f), new Keyframe(1, 0f)));
        var rot = new SeparatedMinMaxCurve(0f); rot.mainCurve = new ParticleSystem.MinMaxCurve(0f, 360f); pi.startRotation = rot;
        var spinCurve = new SeparatedMinMaxCurve(0f); spinCurve.mainCurve = new ParticleSystem.MinMaxCurve(-spin, spin); pi.rotationOverLifetime = spinCurve;
        pi.startColor = new ParticleSystem.MinMaxGradient(colA, colB);
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                     new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
        pi.colorOverLifetime = new ParticleSystem.MinMaxGradient(fade);
        pi.gravityEnabled = gravity != 0f;
        pi.gravity = new ParticleSystem.MinMaxCurve(gravity);
        return pi;
    }

    static Gradient RainbowGradient()
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Hex("#FF8A80"), 0f), new GradientColorKey(Hex("#FFD54F"), 0.25f), new GradientColorKey(Hex("#81C784"), 0.5f),
                          new GradientColorKey(Hex("#64B5F6"), 0.75f), new GradientColorKey(Hex("#BA68C8"), 1f) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return g;
    }

    static MergeItemView BuildItemView(Transform parent, string name, float size, bool withName)
    {
        var bg = Img(parent, name, null, Color.white); Round(bg, 24);
        Place(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        var cg = bg.gameObject.AddComponent<CanvasGroup>();
        var icon = Img(bg.rectTransform, "Icon", circle, Color.white); icon.raycastTarget = false; icon.preserveAspect = true;
        icon.rectTransform.anchorMin = new Vector2(0.08f, withName ? 0.22f : 0.08f); icon.rectTransform.anchorMax = new Vector2(0.92f, 0.92f); icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
        var nameLabel = Text(bg.rectTransform, "Name", "Item", 18, Hex("#6A7A62"), TextAlignmentOptions.Center);
        nameLabel.rectTransform.anchorMin = new Vector2(0.02f, 0.02f); nameLabel.rectTransform.anchorMax = new Vector2(0.98f, 0.24f); nameLabel.rectTransform.offsetMin = nameLabel.rectTransform.offsetMax = Vector2.zero;
        nameLabel.enableAutoSizing = true; nameLabel.fontSizeMin = 10; nameLabel.fontSizeMax = 20;
        nameLabel.gameObject.SetActive(withName);
        var tierBg = Img(bg.rectTransform, "TierBadge", circle, Hex("#F6CB7D")); tierBg.raycastTarget = false;
        Place(tierBg.rectTransform, new Vector2(1, 1), new Vector2(-6, -6), new Vector2(size * 0.26f, size * 0.26f)); tierBg.rectTransform.pivot = new Vector2(1, 1);
        var tier = Text(tierBg.rectTransform, "Tier", "1", size * 0.16f, Hex("#8D724D"), TextAlignmentOptions.Center); Stretch(tier.rectTransform);
        var view = bg.gameObject.AddComponent<MergeItemView>();
        SetField(view, "background", bg); SetField(view, "icon", icon); SetField(view, "nameLabel", nameLabel); SetField(view, "tierLabel", tier); SetField(view, "canvasGroup", cg);
        bg.gameObject.SetActive(false);
        return view;
    }
    /// <summary>9-sliced rounded-rect frame; radius is the on-screen corner radius in canvas units.</summary>
    static void Round(Image img, float radius) => Slice(img, frameRound, FrameRoundBorder.x, radius);
    /// <summary>9-sliced pill frame with fully round caps for a box of the given height.</summary>
    static void Pill(Image img, float height) => Slice(img, framePill, FramePillBorder.x, height * 0.5f);
    static void Slice(Image img, Sprite sprite, float borderPx, float radius)
    {
        img.sprite = sprite != null ? sprite : rounded;
        img.type = Image.Type.Sliced; img.fillCenter = true; img.preserveAspect = false;
        img.pixelsPerUnitMultiplier = sprite != null ? borderPx / Mathf.Max(1f, radius) : 1f;
    }
    static void Stretch(RectTransform rt, float padX = 0, float padY = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(padX, padY); rt.offsetMax = new Vector2(-padX, -padY);
    }
    /// <summary>Stretch horizontally with side margins, anchored to the top edge at y (use after to re-anchor bottom).</summary>
    static void StretchX(RectTransform rt, float y, float height, float margin)
    {
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margin, y - height / 2f); rt.offsetMax = new Vector2(-margin, y + height / 2f);
    }
    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
    }
    static void SetField(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Builder] field '{field}' not found on {target.GetType().Name}"); return; }
        if (value is Object[] arr)
        {
            p.arraySize = arr.Length;
            for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
        }
        else p.objectReferenceValue = value as Object;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetFloats(Object target, string field, float[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Builder] field '{field}' not found on {target.GetType().Name}"); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).floatValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void AddSafeArea(GameObject go, SafeAreaFitter.Mode mode)
    {
        var fitter = GetOrAdd<SafeAreaFitter>(go);
        var so = new SerializedObject(fitter);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("basePosition").vector2Value = ((RectTransform)go.transform).anchoredPosition;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    // Override-sorting orders inside MainCanvas (everything else is order 0, in hierarchy order)
    const int SortCreatures = 5, SortNight = 6, SortFlight = 8, SortCaption = 10, SortFx = 15, SortTop = 20;
    /// <summary>Gives the object its own nested canvas drawn at this order (WinLose and the story are overlay canvases, always on top).</summary>
    static void SortAbove(Component c, int order, bool raycasts = false)
    {
        var cv = GetOrAdd<Canvas>(c.gameObject);
        cv.overrideSorting = true; cv.sortingOrder = order;
        if (raycasts) GetOrAdd<GraphicRaycaster>(c.gameObject);
    }
    static T GetOrAdd<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
    static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
}
#endif
