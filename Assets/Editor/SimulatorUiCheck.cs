#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dev tool: drives the Device Simulator and measures the HUD for overlaps, off-screen / notch intrusions
/// and text that had to shrink to its minimum size. Use in play mode:
///   SimulatorUiCheck.SetDevice(index)  -> switch simulated device (index into the Simulator device list)
///   SimulatorUiCheck.Report()          -> text report for the current device
/// Menu: Tools/Sonu Butterfly/Simulator UI Check (current device).
/// </summary>
public static class SimulatorUiCheck
{
    const BindingFlags BF = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    // HUD elements that must not overlap each other (paths under MainCanvas).
    static readonly string[] HudPaths =
    {
        "BGPanel/PlaySchoolBackButton",
        "LevelMain/Level1/GardenView/RestartButton",
        "LevelMain/Level1/GardenView/LevelPill",
        "LevelMain/Level1/GardenView/RoundDots",
        "LevelMain/Level1/GardenView/CaptionPill",
        "LevelMain/Level1/GamePanel/Objective",
        "LevelMain/Level1/GamePanel/Instruction",
        "LevelMain/Level1/GamePanel/Goals",
        "LevelMain/Level1/GamePanel/BoardArea/Board",
        "LevelMain/Level1/GamePanel/Hint",
    };

    static object Main()
    {
        var wt = Type.GetType("UnityEditor.DeviceSimulation.SimulatorWindow, UnityEditor.DeviceSimulatorModule");
        if (wt == null) return null;
        var w = EditorWindow.GetWindow(wt);
        return wt.GetProperty("main", BF)?.GetValue(w);
    }

    public static string SetDevice(int index)
    {
        var main = Main();
        if (main == null) return "Device Simulator not available";
        main.GetType().GetProperty("deviceIndex", BF).SetValue(main, index);
        return DeviceName(main);
    }

    static string DeviceName(object main)
    {
        var dev = main.GetType().GetProperty("currentDevice", BF)?.GetValue(main);
        var info = dev?.GetType().GetField("deviceInfo", BF)?.GetValue(dev);
        return info?.GetType().GetField("friendlyName", BF)?.GetValue(info) as string ?? "?";
    }

    // phone 16:9, notch, small, narrow, punch hole, foldable, tablets
    public static readonly int[] DefaultSweep = { 18, 25, 14, 29, 49, 54, 72, 73, 0, 10 };

    /// <summary>Play mode: runs Report() on each device (waits a few frames after each switch) and logs one summary.</summary>
    [MenuItem("Tools/Sonu Butterfly/Simulator UI Sweep (all test devices)")]
    public static void Sweep()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[SimulatorUiCheck] Enter play mode first."); return; }
        var sb = new StringBuilder("[SimulatorUiCheck] sweep\n");
        int i = 0, wait = 0;
        int startDevice = -1;
        var main = Main();
        if (main != null) startDevice = (int)main.GetType().GetProperty("deviceIndex", BF).GetValue(main);
        EditorApplication.CallbackFunction step = null;
        step = () =>
        {
            if (!Application.isPlaying) { EditorApplication.update -= step; return; }
            if (wait == 0) { SetDevice(DefaultSweep[i]); wait = 1; return; }
            if (wait < 6) { wait++; EditorApplication.QueuePlayerLoopUpdate(); return; }
            sb.Append(Report());
            wait = 0; i++;
            if (i >= DefaultSweep.Length)
            {
                EditorApplication.update -= step;
                if (startDevice >= 0) SetDevice(startDevice);
                Debug.Log(sb.ToString());
            }
        };
        EditorApplication.update += step;
    }

    [MenuItem("Tools/Sonu Butterfly/Simulator UI Check (current device)")]
    static void LogReport() => Debug.Log(Report());

    public static string Report()
    {
        var sb = new StringBuilder();
        var main = Main();
        sb.AppendLine($"{(main != null ? DeviceName(main) : "Game view")}  {Screen.width}x{Screen.height}  safe={Screen.safeArea}");
        var canvasGo = GameObject.Find("MainCanvas");
        if (canvasGo == null) return sb.Append("MainCanvas not found").ToString();
        var canvas = canvasGo.GetComponent<Canvas>();
        Canvas.ForceUpdateCanvases();
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screen = new Rect(0, 0, Screen.width, Screen.height);
        var safe = Screen.safeArea;
        int issues = 0;

        var rects = new List<(string name, Rect r)>();
        foreach (var path in HudPaths)
        {
            var t = canvasGo.transform.Find(path) as RectTransform;
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            var r = ScreenRect(t, cam);
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.ForceMeshUpdate(); if (tmp.textInfo.characterCount > 0) r = TextRect(tmp, cam); }
            if (t.name == "Goals") r = ChildrenRect(t, cam);
            rects.Add((t.name, r));
            if (!Contains(screen, r)) { sb.AppendLine($"  OFFSCREEN {t.name} {r}"); issues++; }
            else if (!Contains(safe, r)) { sb.AppendLine($"  OUTSIDE SAFE AREA {t.name} {r}"); issues++; }
        }
        for (int i = 0; i < rects.Count; i++)
            for (int j = i + 1; j < rects.Count; j++)
            {
                var a = rects[i]; var b = rects[j];
                var o = Intersect(a.r, b.r);
                if (o.width > 2 && o.height > 2) { sb.AppendLine($"  OVERLAP {a.name} x {b.name} ({o.width:0}x{o.height:0}px)"); issues++; }
            }

        foreach (var tmp in canvasGo.GetComponentsInChildren<TextMeshProUGUI>(false))
        {
            if (string.IsNullOrEmpty(tmp.text) || !tmp.enableAutoSizing) continue;
            tmp.ForceMeshUpdate();
            if (tmp.fontSize <= tmp.fontSizeMin + 0.5f) { sb.AppendLine($"  TEXT AT MIN SIZE {tmp.transform.parent.name}/{tmp.name} '{tmp.text}' {tmp.fontSize}"); issues++; }
            if (tmp.isTextOverflowing) { sb.AppendLine($"  TEXT OVERFLOW {tmp.transform.parent.name}/{tmp.name}"); issues++; }
        }

        // board cells must be on screen and the board must fit its area
        var board = canvasGo.transform.Find("LevelMain/Level1/GamePanel/BoardArea/Board") as RectTransform;
        var area = canvasGo.transform.Find("LevelMain/Level1/GamePanel/BoardArea") as RectTransform;
        if (board != null && area != null && !Contains(Grow(ScreenRect(area, cam), 3), ScreenRect(board, cam))) { sb.AppendLine("  BOARD LARGER THAN ITS AREA"); issues++; }

        var cell = board != null && board.childCount > 1 ? board.GetChild(1) as RectTransform : null;
        sb.AppendLine($"  sizes: back={Size(canvasGo, HudPaths[0], cam)} restart={Size(canvasGo, HudPaths[1], cam)} cell={(cell != null ? ScreenRect(cell, cam).size.ToString("0") : "-")}");
        sb.AppendLine(issues == 0 ? "  OK - no issues" : $"  {issues} issue(s)");
        return sb.ToString();
    }

    /// <summary>Renders Main Camera at the simulated screen size (scaled to maxHeight) and saves a PNG.</summary>
    public static string Capture(string path, int maxHeight = 1000)
    {
        var cam = Camera.main;
        if (cam == null) return "no camera";
        float k = Mathf.Min(1f, (float)maxHeight / Screen.height);
        int w = Mathf.RoundToInt(Screen.width * k), h = Mathf.RoundToInt(Screen.height * k);
        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = prev; RenderTexture.ReleaseTemporary(rt);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        return path;
    }

    static string Size(GameObject root, string path, Camera cam) { var t = root.transform.Find(path) as RectTransform; return t != null ? ScreenRect(t, cam).size.ToString("0") : "-"; }

    static Rect ScreenRect(RectTransform t, Camera cam)
    {
        var c = new Vector3[4]; t.GetWorldCorners(c);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (var w in c) { var p = RectTransformUtility.WorldToScreenPoint(cam, w); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static Rect TextRect(TextMeshProUGUI tmp, Camera cam)
    {
        var b = tmp.textBounds;
        var a = RectTransformUtility.WorldToScreenPoint(cam, tmp.transform.TransformPoint(b.min));
        var z = RectTransformUtility.WorldToScreenPoint(cam, tmp.transform.TransformPoint(b.max));
        return Rect.MinMaxRect(Mathf.Min(a.x, z.x), Mathf.Min(a.y, z.y), Mathf.Max(a.x, z.x), Mathf.Max(a.y, z.y));
    }
    static Rect ChildrenRect(RectTransform t, Camera cam)
    {
        bool any = false; Rect r = default;
        foreach (Transform ch in t) { if (!ch.gameObject.activeSelf) continue; var cr = ScreenRect((RectTransform)ch, cam); r = any ? Rect.MinMaxRect(Mathf.Min(r.xMin, cr.xMin), Mathf.Min(r.yMin, cr.yMin), Mathf.Max(r.xMax, cr.xMax), Mathf.Max(r.yMax, cr.yMax)) : cr; any = true; }
        return any ? r : ScreenRect(t, cam);
    }
    static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.yMin >= outer.yMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMax <= outer.yMax + 1;
    static Rect Grow(Rect r, float d) => Rect.MinMaxRect(r.xMin - d, r.yMin - d, r.xMax + d, r.yMax + d);
    static Rect Intersect(Rect a, Rect b) { float x0 = Mathf.Max(a.xMin, b.xMin), y0 = Mathf.Max(a.yMin, b.yMin), x1 = Mathf.Min(a.xMax, b.xMax), y1 = Mathf.Min(a.yMax, b.yMax); return x1 > x0 && y1 > y0 ? Rect.MinMaxRect(x0, y0, x1, y1) : Rect.zero; }
}
#endif
