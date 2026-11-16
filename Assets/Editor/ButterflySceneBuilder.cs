#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using tmkoc.butterfly;

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
        var backButton = mainCanvasGo.transform.Find("PlayschoolBackButton")?.gameObject
                         ?? mainCanvasGo.transform.Find("BGPanel/PlaySchoolBackButton")?.gameObject;
        var toDelete = new List<GameObject>();
        foreach (Transform c in mainCanvasGo.transform) if (c.gameObject != backButton && c.name != "BGPanel") toDelete.Add(c.gameObject);
        foreach (var g in toDelete) Object.DestroyImmediate(g);
        var oldBg = mainCanvasGo.transform.Find("BGPanel");
        if (oldBg != null) { if (backButton != null && backButton.transform.parent == oldBg) backButton.transform.SetParent(mainCanvasGo.transform, false); Object.DestroyImmediate(oldBg.gameObject); }

        var mc = (RectTransform)mainCanvasGo.transform;

        // ---------------------------------------------------------------- BGPanel + back button
        var bgPanel = Img(mc, "BGPanel", null, Cream);
        Stretch(bgPanel.rectTransform);
        if (backButton == null) { backButton = Img(bgPanel.rectTransform, "PlaySchoolBackButton", circle, Color.white).gameObject; backButton.AddComponent<Button>(); }
        else backButton.transform.SetParent(bgPanel.transform, false);
        backButton.name = "PlaySchoolBackButton";
        var backRt = (RectTransform)backButton.transform;
        backRt.anchorMin = backRt.anchorMax = new Vector2(0, 1); backRt.pivot = new Vector2(0.5f, 0.5f);
        backRt.anchoredPosition = new Vector2(80, -80); backRt.sizeDelta = new Vector2(110, 110);

        // ---------------------------------------------------------------- LevelMain / Level1
        var levelMain = Empty(mc, "LevelMain"); Stretch(levelMain);
        var level1 = Empty(levelMain, "Level1"); Stretch(level1);
        var levelController = level1.gameObject.AddComponent<ButterflyLevelController>();

        // Garden (top 40%)
        var gardenRt = Empty(level1, "GardenView");
        gardenRt.anchorMin = new Vector2(0, 0.6f); gardenRt.anchorMax = Vector2.one; gardenRt.offsetMin = gardenRt.offsetMax = Vector2.zero;
        var garden = gardenRt.gameObject.AddComponent<GardenView>();
        Stretch(Img(gardenRt, "Sky", null, Hex("#BFECEA")).rectTransform);
        var hill = Img(gardenRt, "Hill", circle, Hex("#A5D9AC")); hill.rectTransform.anchorMin = new Vector2(-0.3f, 0); hill.rectTransform.anchorMax = new Vector2(1.3f, 0.55f); hill.rectTransform.offsetMin = hill.rectTransform.offsetMax = Vector2.zero;
        var ground = Img(gardenRt, "Ground", null, Hex("#88C995")); ground.rectTransform.anchorMin = Vector2.zero; ground.rectTransform.anchorMax = new Vector2(1, 0.22f); ground.rectTransform.offsetMin = ground.rectTransform.offsetMax = Vector2.zero;
        var sunGlow = Img(gardenRt, "SunGlow", circle, new Color(1f, 0.95f, 0.74f, 0.85f)); Place(sunGlow.rectTransform, new Vector2(1, 1), new Vector2(-150, -130), new Vector2(200, 200));
        var sun = Img(gardenRt, "Sun", circle, Hex("#FFE59A")); Place(sun.rectTransform, new Vector2(1, 1), new Vector2(-150, -130), new Vector2(110, 110));
        var c1 = Img(gardenRt, "Cloud1", cloud, Color.white); Place(c1.rectTransform, new Vector2(0, 1), new Vector2(230, -150), new Vector2(260, 130));
        var c2 = Img(gardenRt, "Cloud2", cloud, new Color(1, 1, 1, 0.8f)); Place(c2.rectTransform, new Vector2(0.5f, 1), new Vector2(120, -90), new Vector2(200, 100));
        var branch = Img(gardenRt, "Branch", rounded, Hex("#A87957")); branch.type = Image.Type.Sliced; Place(branch.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(760, 40));
        var plant = Prop(gardenRt, "Plant", rounded, Hex("#78BF70"), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(230, 320), new Vector2(0.5f, 0)); plant.GetComponent<Image>().type = Image.Type.Sliced;
        var egg = Prop(gardenRt, "Egg", circle, Hex("#FFF7DF"), new Vector2(0.5f, 0), new Vector2(75, 400), new Vector2(54, 66), new Vector2(0.5f, 0.5f));
        var cat = Prop(gardenRt, "Caterpillar", circle, Hex("#77BD67"), new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(150, 80), new Vector2(0.5f, 0.5f));
        var chry = Prop(gardenRt, "Chrysalis", circle, Hex("#B2D88A"), new Vector2(0.5f, 1), new Vector2(0, -320), new Vector2(70, 130), new Vector2(0.5f, 0.5f));
        var bfly = Prop(gardenRt, "Butterfly", circle, Hex("#F6A35F"), new Vector2(0.5f, 0.5f), new Vector2(150, 60), new Vector2(130, 110), new Vector2(0.5f, 0.5f));
        var flowers = new RectTransform[4];
        for (int i = 0; i < 4; i++) flowers[i] = Prop(gardenRt, "Flower" + i, circle, Hex("#F49FA5"), new Vector2(0, 0), new Vector2(170 + i * 250, 150 + (i % 2) * 25), new Vector2(80, 80), new Vector2(0.5f, 0.5f));
        var night = Img(gardenRt, "NightOverlay", null, new Color(0.1f, 0.12f, 0.3f, 0f)); Stretch(night.rectTransform); night.raycastTarget = false;
        var deliveryTarget = Empty(gardenRt, "DeliveryTarget"); Place(deliveryTarget, new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(10, 10));
        var captionPill = Img(gardenRt, "CaptionPill", rounded, Hex("#FFFDF1")); captionPill.type = Image.Type.Sliced; StretchX(captionPill.rectTransform, 40, 70, 100); captionPill.rectTransform.anchorMin = new Vector2(0, 0); captionPill.rectTransform.anchorMax = new Vector2(1, 0);
        var captionText = Text(captionPill.rectTransform, "Caption", "", 30, Hex("#648261"), TextAlignmentOptions.Center); Stretch(captionText.rectTransform, 20, 5);
        var levelPill = Img(gardenRt, "LevelPill", rounded, Hex("#FFFDF1")); levelPill.type = Image.Type.Sliced; Place(levelPill.rectTransform, new Vector2(0, 1), new Vector2(300, -80), new Vector2(280, 70));
        var levelText = Text(levelPill.rectTransform, "LevelText", "LEVEL 1 / 20", 30, Hex("#658267"), TextAlignmentOptions.Center); Stretch(levelText.rectTransform, 10, 5);
        var restartImg = Img(gardenRt, "RestartButton", circle, Hex("#FFFDF1")); Place(restartImg.rectTransform, new Vector2(1, 1), new Vector2(-80, -80), new Vector2(100, 100));
        var restartIcon = Img(restartImg.rectTransform, "Icon", resetIcon, Hex("#739078")); Place(restartIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60)); restartIcon.raycastTarget = false;
        var restartButton = restartImg.gameObject.AddComponent<Button>();

        SetField(garden, "plant", plant); SetField(garden, "plantImage", plant.GetComponent<Image>());
        SetField(garden, "branch", branch.rectTransform); SetField(garden, "egg", egg); SetField(garden, "caterpillar", cat);
        SetField(garden, "chrysalis", chry); SetField(garden, "butterfly", bfly); SetField(garden, "flowers", flowers);
        SetField(garden, "sunGlow", sunGlow.rectTransform); SetField(garden, "nightOverlay", night); SetField(garden, "deliveryTarget", deliveryTarget); SetField(garden, "caption", captionText);

        // Game panel (bottom 60%)
        var gamePanel = Empty(level1, "GamePanel");
        gamePanel.anchorMin = Vector2.zero; gamePanel.anchorMax = new Vector2(1, 0.6f); gamePanel.offsetMin = gamePanel.offsetMax = Vector2.zero;
        var objective = Text(gamePanel, "Objective", "Water the small plant.", 46, HeadingGreen, TextAlignmentOptions.Center); objective.fontStyle = FontStyles.Bold; StretchX(objective.rectTransform, -50, 80, 40); objective.enableAutoSizing = true; objective.fontSizeMin = 28; objective.fontSizeMax = 46;
        var instruction = Text(gamePanel, "Instruction", "Tap or drag two matching items to merge them!", 28, SoftGreen, TextAlignmentOptions.Center); StretchX(instruction.rectTransform, -115, 40, 40);

        var goalsRt = Empty(gamePanel, "Goals"); StretchX(goalsRt, -215, 120, 20);
        var hlg = goalsRt.gameObject.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleCenter; hlg.childControlWidth = false; hlg.childControlHeight = false; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        var goalsPanel = goalsRt.gameObject.AddComponent<GoalsPanel>();
        var chip = Img(goalsRt, "GoalChip", rounded, GoalYellow); chip.type = Image.Type.Sliced; chip.rectTransform.sizeDelta = new Vector2(360, 110);
        var chipIcon = BuildItemView(chip.rectTransform, "ItemIcon", 86, false); Place(chipIcon.Rect, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(86, 86));
        var chipName = Text(chip.rectTransform, "Name", "Water Drop", 26, Hex("#A47A4B"), TextAlignmentOptions.Left); chipName.fontStyle = FontStyles.Bold; Place(chipName.rectTransform, new Vector2(0, 1), new Vector2(230, -32), new Vector2(240, 40));
        var chipCount = Text(chip.rectTransform, "Count", "0 / 1 collected", 20, Hex("#A47A4B"), TextAlignmentOptions.Left); Place(chipCount.rectTransform, new Vector2(0, 0), new Vector2(230, 28), new Vector2(240, 34));
        SetField(goalsPanel, "container", goalsRt); SetField(goalsPanel, "chipTemplate", chip.gameObject);

        var boardArea = Empty(gamePanel, "BoardArea"); boardArea.anchorMin = Vector2.zero; boardArea.anchorMax = Vector2.one; boardArea.offsetMin = new Vector2(50, 75); boardArea.offsetMax = new Vector2(-50, -290);
        var boardImg = Img(boardArea, "Board", rounded, BoardGreen); boardImg.type = Image.Type.Sliced; Place(boardImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
        var grid = boardImg.gameObject.AddComponent<GridLayoutGroup>();
        var board = boardImg.gameObject.AddComponent<MergeBoard>();
        var cellImg = Img(boardImg.rectTransform, "CellTemplate", rounded, CellGreen); cellImg.type = Image.Type.Sliced;
        var outline = cellImg.gameObject.AddComponent<Outline>(); outline.effectColor = Hex("#FFB56C"); outline.effectDistance = new Vector2(5, -5); outline.enabled = false;
        cellImg.gameObject.AddComponent<MergeCell>();
        var itemTemplate = BuildItemView(boardArea, "ItemTemplate", 200, true);
        var hint = Text(gamePanel, "Hint", "Match two items of the same kind to grow them.", 26, HintGreen, TextAlignmentOptions.Center); StretchX(hint.rectTransform, 38, 40, 40); hint.rectTransform.anchorMin = new Vector2(0, 0); hint.rectTransform.anchorMax = new Vector2(1, 0);

        var dragLayer = Empty(level1, "DragLayer"); Stretch(dragLayer);
        var flightLayer = Empty(level1, "FlightLayer"); Stretch(flightLayer);
        var hand = Img(level1, "TutorialHand", circle, new Color(1f, 1f, 1f, 0.9f)); Place(hand.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90)); hand.raycastTarget = false;
        var handCg = hand.gameObject.AddComponent<CanvasGroup>(); handCg.blocksRaycasts = false;
        var handLabel = Text(hand.rectTransform, "Label", "TAP", 22, Hex("#E8795A"), TextAlignmentOptions.Center); Stretch(handLabel.rectTransform); handLabel.fontStyle = FontStyles.Bold;
        var handRing = hand.gameObject.AddComponent<Outline>(); handRing.effectColor = Hex("#E8795A"); handRing.effectDistance = new Vector2(4, -4);

        SetField(board, "boardRect", boardImg.rectTransform); SetField(board, "grid", grid); SetField(board, "cellTemplate", cellImg.GetComponent<MergeCell>());
        SetField(board, "itemTemplate", itemTemplate); SetField(board, "dragLayer", dragLayer); SetField(board, "fallbackItemSprite", circle);

        SetField(levelController, "garden", garden); SetField(levelController, "board", board); SetField(levelController, "goalsPanel", goalsPanel); SetField(levelController, "flightLayer", flightLayer);
        SetField(levelController, "levelLabel", levelText); SetField(levelController, "objectiveText", objective); SetField(levelController, "instructionText", instruction); SetField(levelController, "hintText", hint); SetField(levelController, "restartButton", restartButton);

        // ---------------------------------------------------------------- final celebration panel
        var finalImg = Img(mc, "FinalCelebrationPanel", null, new Color(0.26f, 0.4f, 0.32f, 0.7f)); Stretch(finalImg.rectTransform);
        var finalCg = finalImg.gameObject.AddComponent<CanvasGroup>();
        var finalPanel = finalImg.gameObject.AddComponent<FinalCelebrationPanel>();
        var modal = Img(finalImg.rectTransform, "Modal", rounded, Hex("#FFF9E8")); modal.type = Image.Type.Sliced; Place(modal.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 1150));
        var finalTitle = Text(modal.rectTransform, "Title", "Butterfly Garden Helper!", 52, Hex("#EA936D"), TextAlignmentOptions.Center); finalTitle.fontStyle = FontStyles.Bold; Place(finalTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(860, 80));
        var cycleRow = Empty(modal.rectTransform, "LifeCycle"); Place(cycleRow, new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(880, 220));
        string[] stepNames = { "Egg", "Caterpillar", "Chrysalis", "Butterfly" };
        Color[] stepColors = { Hex("#FFF7DF"), Hex("#77BD67"), Hex("#B2D88A"), Hex("#F6A35F") };
        var steps = new RectTransform[4];
        for (int i = 0; i < 4; i++)
        {
            steps[i] = Prop(cycleRow, "Step" + i, circle, stepColors[i], new Vector2(0, 0.5f), new Vector2(110 + i * 220, 0), new Vector2(150, 150), new Vector2(0.5f, 0.5f));
            steps[i].Find("Label").GetComponent<TextMeshProUGUI>().text = stepNames[i];
            if (i < 3) { var arrow = Text(cycleRow, "Arrow" + i, ">", 48, Hex("#EA936D"), TextAlignmentOptions.Center); Place(arrow.rectTransform, new Vector2(0, 0.5f), new Vector2(220 + i * 220, 0), new Vector2(60, 60)); }
        }
        var badge = Img(modal.rectTransform, "Badge", star, Color.white); Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(340, 340));
        var badgeLabel = Text(badge.rectTransform, "Label", "GARDEN\nHELPER", 30, Hex("#8D724D"), TextAlignmentOptions.Center); badgeLabel.fontStyle = FontStyles.Bold; Place(badgeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(240, 120));
        var reward = Text(modal.rectTransform, "Reward", "You earned a badge and 10 stars!", 34, Hex("#78967C"), TextAlignmentOptions.Center); Place(reward.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(860, 100));
        var contImg = Img(modal.rectTransform, "ContinueButton", rounded, Orange); contImg.type = Image.Type.Sliced; Place(contImg.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(380, 110));
        var contBtn = contImg.gameObject.AddComponent<Button>();
        var contText = Text(contImg.rectTransform, "Text", "Continue", 38, Color.white, TextAlignmentOptions.Center); contText.fontStyle = FontStyles.Bold; Stretch(contText.rectTransform);
        SetField(finalPanel, "canvasGroup", finalCg); SetField(finalPanel, "lifeCycleSteps", steps); SetField(finalPanel, "badge", badge.rectTransform);
        SetField(finalPanel, "titleText", finalTitle); SetField(finalPanel, "rewardText", reward); SetField(finalPanel, "continueButton", contBtn);
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
        var capBg = storyGo.transform.Find("Panel_Slide/Caption_BG") as RectTransform; if (capBg != null) { capBg.sizeDelta = new Vector2(1000, 260); capBg.anchoredPosition = new Vector2(0, 160); }
        var capTxt = storyGo.transform.Find("Panel_Slide/Caption_BG/Text_Caption")?.GetComponent<TextMeshProUGUI>(); if (capTxt != null) { capTxt.fontSize = 38; capTxt.enableAutoSizing = true; capTxt.fontSizeMin = 24; capTxt.fontSizeMax = 40; }
        var skip = storyGo.transform.Find("Button_Skip") as RectTransform; if (skip != null) { skip.anchoredPosition = new Vector2(-90, -90); skip.sizeDelta = new Vector2(120, 120); }
        var storyController = storyGo.GetComponent<StoryController>();
        storyGo.SetActive(false);

        // ---------------------------------------------------------------- Scripts
        var gm = scriptsRoot.transform.Find("GameManager").GetComponent<GameManager>();
        var lm = scriptsRoot.transform.Find("LevelManager").GetComponent<LevelManager>();
        var smT = scriptsRoot.transform.Find("SoundManager"); var smGo = smT != null ? smT.gameObject : new GameObject("SoundManager");
        smGo.transform.SetParent(scriptsRoot.transform, false);
        var sm = GetOrAdd<SoundManager>(smGo);
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

        // sibling order: BGPanel, LevelMain, FinalCelebrationPanel, WinLose
        bgPanel.transform.SetSiblingIndex(0); levelMain.SetSiblingIndex(1); finalImg.transform.SetSiblingIndex(2); winLose.transform.SetSiblingIndex(3);
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
            Theme(MergeTheme.Water, LifeStage.Egg, "Grow a healthy leaf!", "Keep the egg safe on a fresh leaf.", Hex("#76CCE8"), "Tiny Water Drop", "Water Drop", "Large Water Drop", "Water Bundle"),
            Theme(MergeTheme.Leaf, LifeStage.Caterpillar, "Feed the hungry caterpillar!", "Help the caterpillar grow big and strong.", Hex("#80C978"), "Tiny Leaf", "Small Leaf", "Healthy Leaf", "Leaf Bundle"),
            Theme(MergeTheme.Twig, LifeStage.Chrysalis, "Build a strong branch!", "Watch carefully as the chrysalis changes.", Hex("#B98760"), "Tiny Twig", "Small Twig", "Strong Branch", "Leafy Branch"),
            Theme(MergeTheme.Flower, LifeStage.Butterfly, "Grow nectar-rich flowers!", "Help the butterfly stay healthy.", Hex("#F49FA5"), "Tiny Flower Bud", "Large Flower Bud", "Blooming Flower", "Nectar-Rich Flower"),
        };
        db.levels = new[]
        {
            // Stage 1 – Egg: water (levels 1-4)
            L("Water the small plant.", MergeTheme.Water, 3, 3, G(2, 1)) ,
            L("Give the plant more water.", MergeTheme.Water, 3, 3, G(2, 2)),
            L("Help the plant grow healthy leaves.", MergeTheme.Water, 3, 3, G(3, 1)),
            L("Finish preparing the butterfly's leaf home.", MergeTheme.Water, 3, 4, G(4, 1)),
            // Stage 2 – Caterpillar: leaves (levels 5-10)
            L("Feed the baby caterpillar.", MergeTheme.Leaf, 3, 3, G(2, 1)),
            L("Give the caterpillar another meal.", MergeTheme.Leaf, 3, 4, G(2, 2)),
            L("Help the caterpillar grow bigger.", MergeTheme.Leaf, 3, 4, G(3, 1)),
            L("Feed the growing caterpillar.", MergeTheme.Leaf, 4, 4, G(3, 2)),
            L("Prepare a bigger meal.", MergeTheme.Leaf, 4, 4, G(4, 1)),
            L("Complete the caterpillar's final meal.", MergeTheme.Leaf, 4, 4, G(4, 1), G(2, 1)),
            // Stage 3 – Chrysalis: twigs (levels 11-15)
            L("Prepare a small twig.", MergeTheme.Twig, 3, 4, G(2, 2)),
            L("Create a strong branch.", MergeTheme.Twig, 4, 4, G(3, 1)),
            L("Prepare more support in the garden.", MergeTheme.Twig, 4, 4, G(3, 2)),
            L("Create a leafy hanging place.", MergeTheme.Twig, 4, 4, G(4, 1)),
            L("Finish preparing a suitable transformation area.", MergeTheme.Twig, 4, 5, G(4, 1), G(2, 1)),
            // Stage 4 – Butterfly: flowers (levels 16-20)
            L("Grow the first flower bud.", MergeTheme.Flower, 3, 4, G(2, 2)),
            L("Help the first flower bloom.", MergeTheme.Flower, 4, 4, G(3, 1)),
            L("Grow more flowers for the butterfly.", MergeTheme.Flower, 4, 4, G(3, 2)),
            L("Create a nectar-rich flower.", MergeTheme.Flower, 4, 5, G(4, 1)),
            L("Complete Sonu's butterfly garden.", MergeTheme.Flower, 4, 5, G(4, 1), G(3, 1)),
        };
        db.levels[0].isTutorial = true;
        EditorUtility.SetDirty(db);
        return db;
    }

    static void BuildStories(LevelDatabase db)
    {
        if (!AssetDatabase.IsValidFolder(StoryFolder)) AssetDatabase.CreateFolder(SoFolder, "Stories");
        Story("Story_Intro",
            S("One morning, Sonu spots a beautiful butterfly in Gokuldham garden, looking for a safe leaf to lay her egg.", sonu),
            S("Sonu: \"Mummy, look! I want to make a safe home for its egg and watch how it grows!\"", sonu),
            S("Madhavi: \"Wonderful idea, Sonu! The egg needs a healthy leaf, the caterpillar needs food, and the butterfly needs nectar.\"", sonuSmall),
            S("Help Sonu care for the garden and watch the butterfly grow, from a tiny egg to a beautiful butterfly!", sonu));
        db.GetTheme(MergeTheme.Water).stageEndStory = Story("Story_Stage1_End",
            S("The mother butterfly lands on the healthy leaf and lays an egg. After a few days, a tiny caterpillar hatches!", sonu),
            S("Sonu: \"Yay! The butterfly found a home for her egg!\" Now the hungry caterpillar needs lots of leaves to eat.", sonuSmall));
        db.GetTheme(MergeTheme.Leaf).stageEndStory = Story("Story_Stage2_End",
            S("The caterpillar ate well and grew big. Now it is ready to transform, but it needs a safe place to hang.", sonu),
            S("Let us prepare strong twigs and branches so the caterpillar can form its chrysalis.", sonuSmall));
        db.GetTheme(MergeTheme.Twig).stageEndStory = Story("Story_Stage3_End",
            S("The caterpillar hangs beneath the strong branch and forms a chrysalis. Days pass as it changes inside.", sonu),
            S("Madhavi: \"Amazing, Sonu! The caterpillar has transformed into a butterfly!\" Now it needs nectar-rich flowers.", sonuSmall));
        db.GetTheme(MergeTheme.Flower).stageEndStory = Story("Story_Stage4_End",
            S("The butterfly drinks nectar from the flowers you grew and flies happily around Gokuldham garden.", sonu),
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
    static ThemeData Theme(MergeTheme t, LifeStage s, string title, string caption, Color tint, params string[] names) => new ThemeData { theme = t, stage = s, stageTitle = title, gardenCaption = caption, tint = tint, tierNames = names, tierSprites = new Sprite[4] };
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
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.overflowMode = TextOverflowModes.Overflow;
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
    static MergeItemView BuildItemView(Transform parent, string name, float size, bool withName)
    {
        var bg = Img(parent, name, rounded, Color.white); bg.type = Image.Type.Sliced;
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
        var tier = Text(tierBg.rectTransform, "Tier", "1", size * 0.16f, Hex("#8D724D"), TextAlignmentOptions.Center); tier.fontStyle = FontStyles.Bold; Stretch(tier.rectTransform);
        var view = bg.gameObject.AddComponent<MergeItemView>();
        SetField(view, "background", bg); SetField(view, "icon", icon); SetField(view, "nameLabel", nameLabel); SetField(view, "tierLabel", tier); SetField(view, "canvasGroup", cg);
        bg.gameObject.SetActive(false);
        return view;
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
    static T GetOrAdd<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
    static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
}
#endif
