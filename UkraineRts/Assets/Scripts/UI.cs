using System;
using System.Collections.Generic;
using UnityEngine;

// All interface is drawn with IMGUI so the project needs no UI assets or packages.
public static class UI
{
    static float S = 1f; static float VW, VH;
    static List<Rect> rects = new List<Rect>();
    static GUIStyle sPanel, sBtn, sBtnOn, sLabel, sSmall, sTitle, sHead, sCenter, sBox, sToast, sBig, sRight;
    static Dictionary<int, Texture2D> texCache = new Dictionary<int, Texture2D>();
    static Vector2 scrollList, scrollSel;
    static bool mapOpen, aiOpen, helpOpen;
    static int menuTab;   // 0 main, 1 settings
    static Texture2D white;
    static Vector2 scrollCtl;
    static Dictionary<string, Sprite> brigIcons = new Dictionary<string, Sprite>();
    static readonly string[] CatShort = { "Infantry", "Tanks", "IFVs", "APCs", "Armored", "Trucks", "Cars", "Artillery", "Rockets", "Air Def.", "Aircraft", "Helis", "UAVs", "Ships", "Airborne", "Engineers", "Infrastr.", "Other" };

    static Texture2D Tex(Color c)
    {
        int k = ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8) | (int)(c.a * 255);
        Texture2D t;
        if (texCache.TryGetValue(k, out t) && t != null) return t;
        t = new Texture2D(2, 2, TextureFormat.RGBA32, false); t.SetPixels(new[] { c, c, c, c }); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave;
        texCache[k] = t; return t;
    }

    static void InitStyles()
    {
        if (sPanel != null) return;
        white = Texture2D.whiteTexture;
        sPanel = new GUIStyle(); sPanel.normal.background = Tex(new Color(0.07f, 0.09f, 0.11f, 0.93f)); sPanel.border = new RectOffset(2, 2, 2, 2);
        sBox = new GUIStyle(); sBox.normal.background = Tex(new Color(0.13f, 0.16f, 0.19f, 0.95f));
        sBtn = new GUIStyle(); sBtn.normal.background = Tex(new Color(0.17f, 0.21f, 0.25f, 1f)); sBtn.hover.background = Tex(new Color(0.24f, 0.31f, 0.37f, 1f)); sBtn.active.background = Tex(new Color(0.32f, 0.45f, 0.58f, 1f));
        sBtn.normal.textColor = new Color(0.9f, 0.93f, 0.95f); sBtn.hover.textColor = Color.white; sBtn.active.textColor = Color.white; sBtn.alignment = TextAnchor.MiddleCenter; sBtn.fontSize = 12; sBtn.clipping = TextClipping.Clip;
        sBtnOn = new GUIStyle(sBtn); sBtnOn.normal.background = Tex(new Color(0.18f, 0.42f, 0.62f, 1f)); sBtnOn.hover.background = Tex(new Color(0.22f, 0.5f, 0.72f, 1f)); sBtnOn.normal.textColor = Color.white;
        sLabel = new GUIStyle(); sLabel.normal.textColor = new Color(0.88f, 0.9f, 0.92f); sLabel.fontSize = 12; sLabel.alignment = TextAnchor.MiddleLeft; sLabel.clipping = TextClipping.Clip;
        sSmall = new GUIStyle(sLabel); sSmall.fontSize = 10; sSmall.normal.textColor = new Color(0.62f, 0.68f, 0.72f);
        sTitle = new GUIStyle(sLabel); sTitle.fontSize = 15; sTitle.fontStyle = FontStyle.Bold; sTitle.normal.textColor = Color.white;
        sHead = new GUIStyle(sLabel); sHead.fontSize = 11; sHead.fontStyle = FontStyle.Bold; sHead.normal.textColor = new Color(0.55f, 0.75f, 0.95f);
        sCenter = new GUIStyle(sLabel); sCenter.alignment = TextAnchor.MiddleCenter;
        sRight = new GUIStyle(sLabel); sRight.alignment = TextAnchor.MiddleRight;
        sToast = new GUIStyle(sCenter); sToast.fontSize = 14; sToast.normal.textColor = Color.white; sToast.normal.background = Tex(new Color(0, 0, 0, 0.7f));
        sBig = new GUIStyle(sBtn); sBig.fontSize = 22; sBig.fontStyle = FontStyle.Bold;
    }

    public static bool MouseOverUI(Vector2 raw)
    {
        Vector2 v = raw / S;
        for (int i = 0; i < rects.Count; i++) if (rects[i].Contains(v)) return true;
        return false;
    }
    static void Reg(Rect r) { rects.Add(r); }

    static bool Btn(Rect r, string t, bool on = false)
    {
        if (GUI.Button(r, t, on ? sBtnOn : sBtn)) { Sfx.Ui(); return true; }
        return false;
    }
    static bool Btn(Rect r, string t, GUIStyle st)
    {
        if (GUI.Button(r, t, st)) { Sfx.Ui(); return true; }
        return false;
    }
    static void Panel(Rect r) { GUI.Box(r, GUIContent.none, sPanel); Reg(r); }
    static void Tint(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old; }

    static void DrawSprite(Rect box, Sprite s, Color? tint = null)
    {
        if (s == null) return;
        Texture2D t = s.texture; Rect sr = s.rect;
        float aspect = sr.width / sr.height; float bw = box.width, bh = box.height;
        float w = bw, h = bw / aspect; if (h > bh) { h = bh; w = bh * aspect; }
        Rect dst = new Rect(box.x + (bw - w) * 0.5f, box.y + (bh - h) * 0.5f, w, h);
        var old = GUI.color; GUI.color = tint ?? Color.white;
        GUI.DrawTextureWithTexCoords(dst, t, new Rect(sr.x / t.width, sr.y / t.height, sr.width / t.width, sr.height / t.height));
        GUI.color = old;
    }

    static Sprite BrigIcon(Brigade b)
    {
        string k = b.fac + "_" + b.idx; Sprite s;
        if (brigIcons.TryGetValue(k, out s)) return s;
        var p = new Pix(1.7f, 1.7f, 28);
        p.Circle(Fac.Color(b.fac), 0, 0, 0.82f); p.Circle(b.main, 0, 0, 0.72f);
        Art.Camo(p, Pal.Make(b, b.fac), 3, 1.6f);
        Art.Emblem(p, b.emblem, 0, 0, 1.0f, b.accent, G.Mul(b.sec, 0.8f), 0);
        s = p.Make(); brigIcons[k] = s; Atlas.Flush(); return s;
    }

    static void Bar(Rect r, float v, Color c, string label = null)
    {
        Tint(r, new Color(0.02f, 0.03f, 0.04f, 0.9f));
        Rect f = new Rect(r.x + 1, r.y + 1, Mathf.Max(0, (r.width - 2) * Mathf.Clamp01(v)), r.height - 2);
        Tint(f, c);
        if (label != null) GUI.Label(new Rect(r.x + 4, r.y - 1, r.width, r.height + 2), label, sSmall);
    }
    static Color HpColor(float f) { return f > 0.6f ? new Color(0.3f, 0.78f, 0.35f) : (f > 0.3f ? new Color(0.9f, 0.75f, 0.2f) : new Color(0.9f, 0.25f, 0.2f)); }

    // ------------------------------------------------------------------ entry point
    public static void Draw(Game g)
    {
        InitStyles();
        S = Mathf.Clamp(UnityEngine.Screen.height / 820f, 0.85f, 2.4f);
        VW = UnityEngine.Screen.width / S; VH = UnityEngine.Screen.height / S;
        rects.Clear();
        var oldM = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(S, S, 1));
        switch (g.screen)
        {
            case Screen_.Menu: DrawMainMenu(g); break;
            case Screen_.Settings: DrawSettings(g, false); break;
            case Screen_.Controls: DrawControls(g); break;
            case Screen_.Sandbox: DrawSandbox(g); break;
        }
        GUI.matrix = oldM;
    }

    // ------------------------------------------------------------------ main menu
    static void DrawMainMenu(Game g)
    {
        Tint(new Rect(0, 0, VW, VH), new Color(0.05f, 0.07f, 0.1f, 1f));
        // decorative flag bands
        Tint(new Rect(0, 0, VW, 10), G.Hex(0x2f7fe0)); Tint(new Rect(0, 10, VW, 10), G.Hex(0xffd500));
        Tint(new Rect(0, VH - 30, VW, 10), G.Hex(0xf2f2f2)); Tint(new Rect(0, VH - 20, VW, 10), G.Hex(0x2a4fc8)); Tint(new Rect(0, VH - 10, VW, 10), G.Hex(0xd02a2a));
        for (int i = 0; i < 18; i++) Tint(new Rect((i * 97 + Time.time * 12f) % (VW + 100) - 50, 40 + (i * 53) % (VH - 120), 2, 2), new Color(1, 1, 1, 0.08f));
        float cx = VW / 2f;
        var big = new GUIStyle(sTitle); big.fontSize = 46; big.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(cx - 400, VH * 0.12f, 800, 70), "FRONTLINE SANDBOX", big);
        var sub = new GUIStyle(sCenter); sub.fontSize = 16; sub.normal.textColor = new Color(0.65f, 0.75f, 0.85f);
        GUI.Label(new Rect(cx - 400, VH * 0.12f + 64, 800, 26), "2D real-time strategy  -  Ukraine vs Russia  -  military sandbox", sub);
        float bw = 300, bh = 52, y = VH * 0.36f;
        string[] names = { "PLAY", "SANDBOX", "SETTINGS", "CONTROLS", "EXIT" };
        for (int i = 0; i < names.Length; i++)
        {
            Rect r = new Rect(cx - bw / 2, y + i * (bh + 12), bw, bh);
            if (Btn(r, names[i], sBig))
            {
                switch (i)
                {
                    case 0: g.StartSandbox(true); break;
                    case 1: g.StartSandbox(false); break;
                    case 2: g.screen = Screen_.Settings; break;
                    case 3: g.screen = Screen_.Controls; break;
                    case 4: Quit(); break;
                }
            }
            Reg(r);
        }
        var foot = new GUIStyle(sSmall); foot.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(0, VH - 62, VW, 20), "PLAY starts a sandbox with a ready-made skirmish.  SANDBOX opens an empty battlefield.  All data and characteristics are fictional gameplay values.", foot);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ settings & controls
    static void DrawSettings(Game g, bool overlay)
    {
        float w = 520, h = 470; float x = (VW - w) / 2f, y = (VH - h) / 2f;
        if (!overlay) Tint(new Rect(0, 0, VW, VH), new Color(0.05f, 0.07f, 0.1f, 1f));
        Panel(new Rect(x, y, w, h));
        GUI.Label(new Rect(x + 20, y + 12, w, 30), "SETTINGS", sTitle);
        float yy = y + 56; var c = g.cfg;
        GUI.Label(new Rect(x + 24, yy, 200, 24), "Master volume  " + Mathf.RoundToInt(c.masterVol * 100) + "%", sLabel);
        float v = GUI.HorizontalSlider(new Rect(x + 230, yy + 6, 260, 20), c.masterVol, 0f, 1f);
        if (Mathf.Abs(v - c.masterVol) > 0.001f) { c.masterVol = v; g.ApplyCfg(); }
        yy += 40;
        GUI.Label(new Rect(x + 24, yy, 200, 26), "Health bars", sLabel);
        string[] hb = { "Off", "Damaged", "Always" };
        for (int i = 0; i < 3; i++) if (Btn(new Rect(x + 230 + i * 88, yy, 84, 26), hb[i], c.healthBars == i)) c.healthBars = i;
        yy += 40;
        GUI.Label(new Rect(x + 24, yy, 200, 26), "Effects quality", sLabel);
        string[] pq = { "Low", "Medium", "High" };
        for (int i = 0; i < 3; i++) if (Btn(new Rect(x + 230 + i * 88, yy, 84, 26), pq[i], c.particles == i)) { c.particles = i; g.ApplyCfg(); }
        yy += 40;
        c.trackMarks = Toggle(new Rect(x + 24, yy, 230, 24), "Tire & track marks", c.trackMarks);
        c.shake = Toggle(new Rect(x + 270, yy, 230, 24), "Camera shake", c.shake); yy += 32;
        c.edgeScroll = Toggle(new Rect(x + 24, yy, 230, 24), "Edge scrolling", c.edgeScroll);
        c.minimap = Toggle(new Rect(x + 270, yy, 230, 24), "Mini-map", c.minimap); yy += 32;
        c.showFps = Toggle(new Rect(x + 24, yy, 230, 24), "Show FPS", c.showFps); yy += 44;
        GUI.Label(new Rect(x + 24, yy, w - 48, 60), "Settings are stored automatically. Press Esc in the sandbox to open the pause menu.", sSmall);
        if (Btn(new Rect(x + w - 150, y + h - 52, 126, 34), "BACK"))
        {
            c.Save();
            if (overlay) menuTab = 0; else g.screen = Screen_.Menu;
        }
    }

    static bool Toggle(Rect r, string label, bool v)
    {
        bool n = GUI.Toggle(r, v, label, GetToggleStyle());
        if (n != v) Sfx.Ui();
        return n;
    }
    static GUIStyle togSt;
    static GUIStyle GetToggleStyle()
    {
        if (togSt != null) return togSt;
        togSt = new GUIStyle(GUI.skin.toggle); togSt.normal.textColor = new Color(0.88f, 0.9f, 0.92f); togSt.onNormal.textColor = Color.white; togSt.hover.textColor = Color.white; togSt.onHover.textColor = Color.white; togSt.fontSize = 12;
        return togSt;
    }

    static void DrawControls(Game g)
    {
        Tint(new Rect(0, 0, VW, VH), new Color(0.05f, 0.07f, 0.1f, 1f));
        float w = 880, h = 560; float x = (VW - w) / 2f, y = (VH - h) / 2f;
        Panel(new Rect(x, y, w, h));
        GUI.Label(new Rect(x + 20, y + 12, w, 30), "CONTROLS", sTitle);
        string[] left = {
            "CAMERA", "W A S D / Arrows  -  move camera (Shift = faster)", "Mouse wheel  -  zoom in / out", "Middle mouse drag  -  pan", "F  -  focus camera on selection", "H  -  hide / show interface", "",
            "SIMULATION", "Space  -  pause / resume", "[ and ]  (or - and =)  -  slower / faster", "Esc  -  cancel / pause menu", "F5 / F9  -  quick save / load", "",
            "SPAWNING", "Pick a unit in the right panel, then Left click to deploy", "Right click or Esc  -  cancel placement", "Q / E  -  rotate spawn facing", "Count buttons  -  spawn many identical units" };
        string[] right = {
            "SELECTION & ORDERS", "Left click  -  select  (Shift = add, double click = same type)", "Left drag on empty ground  -  box select", "Right click ground  -  move   |   enemy  -  attack", "Right click friendly vehicle with soldiers  -  board", "Ctrl + 1..9  -  create group,   1..9  -  recall group", "Ctrl + C  -  select all of current faction", "",
            "COMMANDS", "X stop   R return home   G follow   Del delete", "U unload   L load nearby infantry   B deploy bridge", "Z flares   T take off   N land   PgUp / PgDn altitude layer", "Tools bar: Select / Drag / Delete objects", "",
            "OPERATORS & DRONES", "Select an operator, press a drone button to launch it", "Right click an enemy with an operator  -  attack drone", "Drone selected  -  live observation camera" };
        float ly = y + 54;
        foreach (var s in left) { bool head = s.Length > 0 && s.ToUpper() == s; GUI.Label(new Rect(x + 24, ly, 420, 22), s, head ? sHead : sLabel); ly += head ? 24 : 21; }
        ly = y + 54;
        foreach (var s in right) { bool head = s.Length > 0 && s.ToUpper() == s; GUI.Label(new Rect(x + 450, ly, 420, 22), s, head ? sHead : sLabel); ly += head ? 24 : 21; }
        if (Btn(new Rect(x + w - 150, y + h - 48, 126, 32), "BACK")) g.screen = Screen_.Menu;
    }

    // ------------------------------------------------------------------ sandbox
    static void DrawSandbox(Game g)
    {
        if (g.menuOpen) { DrawPauseMenu(g); DrawFloating(g); return; }
        DrawFloating(g);
        if (g.uiHidden)
        {
            Rect r = new Rect(8, 8, 74, 26); Reg(r);
            if (Btn(r, "SHOW UI")) g.uiHidden = false;
            DrawToast(g);
            return;
        }
        DrawTopBar(g);
        DrawRightPanel(g);
        DrawBottom(g);
        DrawMinimap(g);
        if (mapOpen) DrawMapPanel(g);
        if (aiOpen) DrawAIPanel(g);
        DrawDroneCam(g);
        DrawToast(g);
        if (g.cfg.showFps) GUI.Label(new Rect(VW - 460, 48, 120, 18), "FPS " + Mathf.RoundToInt(g.fps), sSmall);
    }

    static void DrawToast(Game g)
    {
        string t = g.ToastText;
        if (!string.IsNullOrEmpty(t)) { var r = new Rect(VW / 2 - 200, 56, 400, 28); GUI.Label(r, t, sToast); }
        if (g.spawnDef != null && !g.uiHidden)
        {
            var r = new Rect(VW / 2 - 250, VH - (g.sel.Count > 0 ? 210 : 40), 500, 26);
            var st = new GUIStyle(sToast); st.fontSize = 12;
            GUI.Label(r, "Placing: " + g.spawnDef.name + "  x" + g.spawnCount + "  -  Left click to deploy, Right click / Esc to cancel", st);
        }
        if (g.followMode) GUI.Label(new Rect(VW / 2 - 200, 84, 400, 24), "FOLLOW: right-click a unit to follow", sToast);
    }

    static void DrawTopBar(Game g)
    {
        Rect bar = new Rect(0, 0, VW - 340, 44); Panel(bar);
        float x = 8, y = 8;
        if (Btn(new Rect(x, y, 70, 28), "MENU")) g.menuOpen = true; x += 76;
        if (Btn(new Rect(x, y, 78, 28), g.paused ? "RESUME" : "PAUSE", g.paused)) g.paused = !g.paused; x += 84;
        if (Btn(new Rect(x, y, 28, 28), "-")) g.simSpeed = Mathf.Max(0.25f, g.simSpeed * 0.5f); x += 30;
        GUI.Label(new Rect(x, y, 50, 28), "x" + (g.simSpeed < 1 ? g.simSpeed.ToString("0.00") : g.simSpeed.ToString("0.#")), sCenter); x += 50;
        if (Btn(new Rect(x, y, 28, 28), "+")) g.simSpeed = Mathf.Min(8f, g.simSpeed * 2f); x += 40;
        float t = g.world.tod; int hh = (int)t, mm = (int)((t - hh) * 60);
        GUI.Label(new Rect(x, y, 120, 28), string.Format("{0:00}:{1:00}  {2}", hh, mm, g.world.weather), sLabel); x += 116;
        if (Btn(new Rect(x, y, 92, 28), "MAP / TIME", mapOpen)) { mapOpen = !mapOpen; aiOpen = false; } x += 98;
        if (Btn(new Rect(x, y, 78, 28), "AI", aiOpen)) { aiOpen = !aiOpen; mapOpen = false; } x += 90;
        string[] tn = { "SELECT", "MOVE OBJ", "DELETE" };
        for (int i = 0; i < 3; i++) { if (Btn(new Rect(x, y, 78, 28), tn[i], (int)g.tool == i)) { g.tool = (Tool)i; g.spawnDef = null; } x += 80; }
        x += 8;
        if (Btn(new Rect(x, y, 70, 28), "HIDE UI")) g.uiHidden = true; x += 76;
        GUI.Label(new Rect(x, y, 220, 28), "Objects: " + g.entCount + (g.cfg.showFps ? "  FPS " + Mathf.RoundToInt(g.fps) : ""), sSmall);
    }

    static void DrawRightPanel(Game g)
    {
        float pw = 340; Rect r = new Rect(VW - pw, 0, pw, VH); Panel(r);
        float x = r.x + 8, y = 8, w = pw - 16;
        // faction
        GUI.Label(new Rect(x, y, w, 18), "FACTION", sHead); y += 18;
        Rect ua = new Rect(x, y, w / 2 - 3, 34), ru = new Rect(x + w / 2 + 3, y, w / 2 - 3, 34);
        bool isUA = g.faction == Faction.Ukraine;
        Tint(ua, isUA ? G.Hex(0x2f7fe0) : new Color(0.12f, 0.2f, 0.32f)); Tint(new Rect(ua.x, ua.y + ua.height - 6, ua.width, 3), G.Hex(0x2f7fe0)); Tint(new Rect(ua.x, ua.y + ua.height - 3, ua.width, 3), G.Hex(0xffd500));
        Tint(ru, !isUA ? G.Hex(0xd04030) : new Color(0.3f, 0.14f, 0.12f)); Tint(new Rect(ru.x, ru.y + ru.height - 9, ru.width, 3), Color.white); Tint(new Rect(ru.x, ru.y + ru.height - 6, ru.width, 3), G.Hex(0x2a4fc8)); Tint(new Rect(ru.x, ru.y + ru.height - 3, ru.width, 3), G.Hex(0xd02a2a));
        var fs = new GUIStyle(sBtn); fs.normal.background = null; fs.hover.background = Tex(new Color(1, 1, 1, 0.12f)); fs.fontStyle = FontStyle.Bold; fs.fontSize = 13;
        if (Btn(ua, "UKRAINE", fs)) { g.faction = Faction.Ukraine; g.brigIdx = 0; g.spawnDef = null; g.facingManual = false; }
        if (Btn(ru, "RUSSIA", fs)) { g.faction = Faction.Russia; g.brigIdx = 0; g.spawnDef = null; g.facingManual = false; }
        y += 40;
        // brigade
        var brs = Db.Brigades[(int)g.faction];
        Brigade b = g.Brig;
        GUI.Label(new Rect(x, y, w, 18), "BRIGADE", sHead); y += 18;
        Panel2(new Rect(x, y, w, 48));
        DrawSprite(new Rect(x + 4, y + 4, 40, 40), BrigIcon(b));
        GUI.Label(new Rect(x + 50, y + 3, w - 110, 20), b.name, sLabel);
        GUI.Label(new Rect(x + 50, y + 22, w - 110, 20), "\"" + b.nick + "\"  -  " + b.note, sSmall);
        if (Btn(new Rect(x + w - 54, y + 10, 24, 28), "<")) g.brigIdx = (g.brigIdx + brs.Length - 1) % brs.Length;
        if (Btn(new Rect(x + w - 28, y + 10, 24, 28), ">")) g.brigIdx = (g.brigIdx + 1) % brs.Length;
        // swatches
        Tint(new Rect(x + 4, y + 45, 12, 3), b.main); Tint(new Rect(x + 16, y + 45, 12, 3), b.sec); Tint(new Rect(x + 28, y + 45, 12, 3), b.accent);
        y += 54;
        // spawn options
        GUI.Label(new Rect(x, y, 120, 18), "SPAWN COUNT", sHead);
        GUI.Label(new Rect(x + w - 150, y, 150, 18), "Facing: " + (g.facingManual ? Mathf.RoundToInt(Mathf.Repeat(g.spawnFacing, 360)) + "°" : "auto"), sRight); y += 18;
        int[] counts = { 1, 3, 5, 10, 25, 50 };
        float bw = (w - 5 * 3) / 6f;
        for (int i = 0; i < counts.Length; i++) if (Btn(new Rect(x + i * (bw + 3), y, bw, 24), counts[i].ToString(), g.spawnCount == counts[i])) g.spawnCount = counts[i];
        y += 28;
        float sv = GUI.HorizontalSlider(new Rect(x, y + 4, w - 140, 18), g.spawnCount, 1, 100);
        int ni = Mathf.RoundToInt(sv); if (ni != g.spawnCount) g.spawnCount = ni;
        GUI.Label(new Rect(x + w - 132, y - 1, 40, 24), "x" + g.spawnCount, sLabel);
        g.spawnLoaded = Toggle(new Rect(x + w - 90, y - 1, 92, 24), "Loaded", g.spawnLoaded);
        y += 26;
        // categories
        GUI.Label(new Rect(x, y, w, 18), "CATEGORY", sHead); y += 18;
        int cols = 3; float cw = (w - (cols - 1) * 3) / cols;
        for (int i = 0; i < 18; i++)
        {
            Rect cr = new Rect(x + (i % cols) * (cw + 3), y + (i / cols) * 25, cw, 23);
            bool allowed = b.Has((Cat)i);
            var st = (int)g.cat == i ? sBtnOn : sBtn;
            if (!allowed && (int)g.cat != i) { var old = GUI.contentColor; GUI.contentColor = new Color(1, 1, 1, 0.55f); if (Btn(cr, CatShort[i], st)) g.cat = (Cat)i; GUI.contentColor = old; }
            else if (Btn(cr, CatShort[i], st)) g.cat = (Cat)i;
        }
        y += 6 * 25 + 4;
        // unit list
        GUI.Label(new Rect(x, y, w, 18), Db.CatName(g.cat).ToUpper(), sHead); y += 18;
        float listH = VH - y - 52;
        var list = Db.For(g.cat, g.faction);
        list.RemoveAll(d => d.special == "hidden");
        float rowH = 46;
        Rect view = new Rect(x, y, w, listH);
        Rect content = new Rect(0, 0, w - 18, list.Count * (rowH + 2));
        scrollList = GUI.BeginScrollView(view, scrollList, content);
        for (int i = 0; i < list.Count; i++)
        {
            var d = list[i];
            Rect rr = new Rect(0, i * (rowH + 2), w - 18, rowH);
            bool on = g.spawnDef == d;
            Tint(rr, on ? new Color(0.17f, 0.4f, 0.6f, 1f) : new Color(0.13f, 0.16f, 0.19f, 1f));
            if (rr.Contains(Event.current.mousePosition) && !on) Tint(rr, new Color(1, 1, 1, 0.07f));
            DrawSprite(new Rect(rr.x + 3, rr.y + 3, 52, rowH - 6), Art.Icon(d, b, g.faction));
            var nm = new GUIStyle(sLabel); nm.fontStyle = FontStyle.Bold;
            bool allowed = b.Has(d.cat) || d.cat == Cat.Infrastructure;
            GUI.Label(new Rect(rr.x + 60, rr.y + 3, rr.width - 64, 18), d.name + (allowed ? "" : "  *"), nm);
            GUI.Label(new Rect(rr.x + 60, rr.y + 22, rr.width - 64, 18), Stats(d), sSmall);
            if (GUI.Button(rr, GUIContent.none, GUIStyle.none))
            {
                Sfx.Ui(true); g.spawnDef = on ? null : d; g.tool = Tool.Select;
                if (g.spawnDef != null) g.ClearSel();
            }
        }
        GUI.EndScrollView();
        y += listH + 4;
        var hint = new GUIStyle(sSmall); hint.wordWrap = true;
        GUI.Label(new Rect(x, y, w, 46), g.spawnDef != null ? "Selected: " + g.spawnDef.name + "\n" + g.spawnDef.desc : "Select a unit, then click on the map.   * = not standard for this brigade", hint);
    }
    static void Panel2(Rect r) { GUI.Box(r, GUIContent.none, sBox); }

    static string Stats(UnitDef d)
    {
        switch (d.kind)
        {
            case Kind.Soldier: return "HP " + d.hp + "   " + (d.w.Length > 0 ? d.w[d.w.Length - 1].name : "unarmed") + (d.kit != "" ? "   drones" : "");
            case Kind.Prop: return "HP " + d.hp + (d.w.Length > 0 ? "   armed" : "") + (d.solid ? "   solid" : "");
            case Kind.Plane: case Kind.Heli: return "HP " + d.hp + "   SPD " + d.speed + (d.cap > 0 ? "   troops " + d.cap : "") + (d.w.Length > 0 ? "   armed" : "");
            case Kind.Drone: return "Operator + " + (d.special == "fpv" || d.special == "loiter" ? "strike" : (d.special == "ucav" ? "combat" : "recon")) + " drone   range " + d.signal;
            case Kind.Ship: return "HP " + d.hp + "   SPD " + d.speed + (d.cap > 0 ? "   troops " + d.cap : "");
            default:
                if (d.fam == "special") return d.loadN > 0 ? "x" + d.loadN + " troops" : "";
                return "HP " + d.hp + "  ARM " + d.armor.ToString("0.#") + "  SPD " + d.speed + (d.cap > 0 ? "  cap " + d.cap : "") + (d.w.Length > 0 ? "  rng " + MaxRange(d) : "");
        }
    }
    static int MaxRange(UnitDef d) { float m = 0; foreach (var w in d.w) m = Mathf.Max(m, w.range); return (int)m; }

    // ------------------------------------------------------------------ bottom info panel
    static void DrawBottom(Game g)
    {
        var us = g.sel; us.RemoveAll(e => e == null || e.removed);
        if (us.Count == 0) { DrawHover(g); return; }
        float x0 = g.cfg.minimap ? 196 : 8; float w = VW - 340 - x0 - 8; float h = 154; float y0 = VH - h - 8;
        Rect r = new Rect(x0, y0, w, h); Panel(r);
        Ent e0 = us[0];
        // portrait
        Rect pr = new Rect(r.x + 8, r.y + 8, 96, 70); Panel2(pr);
        DrawSprite(new Rect(pr.x + 4, pr.y + 4, pr.width - 8, pr.height - 8), Art.Icon(e0.def, e0.brig, e0.fac));
        float x = r.x + 112;
        if (us.Count == 1)
        {
            GUI.Label(new Rect(x, r.y + 6, 300, 20), e0.def.name, sTitle);
            Tint(new Rect(x - 6, r.y + 8, 4, 16), Fac.Color(e0.fac));
            string brn = e0.brig != null ? e0.brig.name + " \"" + e0.brig.nick + "\"" : "-";
            GUI.Label(new Rect(x, r.y + 26, 360, 16), Fac.Name(e0.fac) + "  -  " + brn, sSmall);
            GUI.Label(new Rect(x, r.y + 42, 360, 16), "Type: " + Db.CatName(e0.def.cat) + "    Status: " + e0.Status(), sLabel);
            DrawConditions(g, e0, new Rect(x, r.y + 62, 330, 90));
            DrawStatsPanel(g, e0, new Rect(r.x + 8, y0 - 128, 340, 120));
        }
        else
        {
            GUI.Label(new Rect(x, r.y + 6, 300, 20), us.Count + " units selected", sTitle);
            var counts = new Dictionary<string, int>();
            foreach (var e in us) { string k = e.def.name; counts[k] = counts.ContainsKey(k) ? counts[k] + 1 : 1; }
            string txt = ""; int c = 0; foreach (var kv in counts) { txt += kv.Value + "x " + kv.Key + "   "; if (++c >= 6) { txt += "..."; break; } }
            var wl = new GUIStyle(sSmall); wl.wordWrap = true; GUI.Label(new Rect(x, r.y + 28, 330, 40), txt, wl);
            float hpSum = 0, hpMax = 0; foreach (var e in us) { hpSum += e.hp; hpMax += e.maxHp; }
            Bar(new Rect(x, r.y + 72, 300, 14), hpSum / Mathf.Max(1, hpMax), HpColor(hpSum / Mathf.Max(1, hpMax)), "Group health");
        }
        DrawCommands(g, new Rect(x + 340, r.y + 6, r.xMax - (x + 340) - 8, h - 12));
    }

    static void DrawHover(Game g)
    {
        Ent e = g.world.Pick(g.mouseWorld, true);
        if (e == null || g.UIOver) return;
        float x0 = g.cfg.minimap ? 196 : 8; Rect r = new Rect(x0, VH - 52, 420, 44); Panel(r);
        GUI.Label(new Rect(r.x + 8, r.y + 4, 400, 18), e.def.name + "  -  " + Fac.Name(e.fac), sLabel);
        GUI.Label(new Rect(r.x + 8, r.y + 22, 400, 18), "HP " + Mathf.RoundToInt(e.hp) + "/" + Mathf.RoundToInt(e.maxHp) + "   " + e.Status() + "   [" + World.TerNames[g.world.At(e.pos)] + "]", sSmall);
    }

    static void DrawConditions(Game g, Ent e, Rect r)
    {
        float x = r.x, y = r.y, bw = 160; int col = 0;
        Action<string, float, Color> add = (label, v, c) =>
        {
            float bx = x + (col % 2) * (bw + 10), by = y + (col / 2) * 18;
            Bar(new Rect(bx, by, bw, 14), v, c, label); col++;
        };
        add("Health " + Mathf.RoundToInt(e.hp) + "/" + Mathf.RoundToInt(e.maxHp), e.hp / Mathf.Max(1, e.maxHp), HpColor(e.hp / Mathf.Max(1, e.maxHp)));
        var vh = e as Vehicle; var a = e as Aircraft; var s = e as Ship; var so = e as Soldier; var d = e as Drone; var u = e as Unit;
        if (vh != null)
        {
            add("Engine", vh.engine / 100f, HpColor(vh.engine / 100f)); add(vh.def.vs.tracked ? "Tracks" : "Wheels", vh.tracks / 100f, HpColor(vh.tracks / 100f));
            if (vh.def.vs.tur > 0) add("Turret", vh.turret / 100f, HpColor(vh.turret / 100f));
            int am = 0, amMax = 0; foreach (var w in vh.weps) if (w.d.ammo > 0) { am += w.ammo; amMax += w.d.ammo; }
            if (amMax > 0) add("Ammo " + am, am / (float)amMax, new Color(0.85f, 0.7f, 0.3f));
            add("Fuel", vh.fuel / 100f, new Color(0.45f, 0.65f, 0.9f));
            if (vh.cap > 0) add("Passengers " + vh.cargo.Count + "/" + vh.cap, vh.cargo.Count / (float)vh.cap, new Color(0.5f, 0.8f, 0.6f));
            if (vh.def.crew > 0) add("Crew " + vh.crewN + "/" + vh.def.crew, vh.crewN / (float)vh.def.crew, new Color(0.6f, 0.75f, 0.85f));
        }
        else if (d != null)
        {
            add("Battery", d.battery / Mathf.Max(1, d.maxBattery), HpColor(d.battery / Mathf.Max(1, d.maxBattery)));
            add("Signal" + (d.noSignal ? " LOST" : ""), d.sig, d.noSignal ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.4f, 0.8f, 1f));
            add("Link range " + Mathf.RoundToInt(d.SignalRange), 1f, new Color(0.3f, 0.45f, 0.6f));
        }
        else if (a != null)
        {
            add("Engine", a.engine / 100f, HpColor(a.engine / 100f)); add("Layer: " + a.LayerName, a.alt / 2f, new Color(0.5f, 0.7f, 0.95f));
            if (a.def.flares) add("Flares " + a.flaresLeft, a.flaresLeft / 30f, new Color(1f, 0.7f, 0.3f));
            int am = 0, amMax = 0; foreach (var w in a.weps) if (w.d.ammo > 0) { am += w.ammo; amMax += w.d.ammo; }
            if (amMax > 0) add("Ordnance " + am, am / (float)amMax, new Color(0.85f, 0.7f, 0.3f));
            if (a.cap > 0) add("Troops " + a.cargo.Count + "/" + a.cap, a.cargo.Count / (float)a.cap, new Color(0.5f, 0.8f, 0.6f));
        }
        else if (s != null)
        {
            add("Flooding", s.flood, s.flood > 0.5f ? new Color(0.9f, 0.3f, 0.2f) : new Color(0.4f, 0.65f, 0.95f));
            if (s.cap > 0) add("Troops " + s.cargo.Count + "/" + s.cap, s.cargo.Count / (float)s.cap, new Color(0.5f, 0.8f, 0.6f));
            int am = 0, amMax = 0; foreach (var w in s.weps) if (w.d.ammo > 0) { am += w.ammo; amMax += w.d.ammo; }
            if (amMax > 0) add("Missiles/ammo " + am, am / (float)amMax, new Color(0.85f, 0.7f, 0.3f));
        }
        else if (so != null)
        {
            add("Magazine " + so.mag, so.mag / (float)Mathf.Max(1, so.magSize), new Color(0.85f, 0.7f, 0.3f));
            if (so.role == Role.Operator) { int n = 0; foreach (var k in so.kit) n += k.n; add("Drones " + n + " (+" + so.drones.Count + " up)", Mathf.Min(1f, n / 5f), new Color(0.5f, 0.8f, 1f)); }
        }
    }

    static void DrawStatsPanel(Game g, Ent e, Rect r)
    {
        Panel(r);
        var u = e as Unit;
        string t = "STATISTICS\n";
        t += "Armor " + e.def.armor.ToString("0.#") + "   Speed " + (u != null ? u.maxSpeed.ToString("0.#") : "0") + "   Kills " + e.kills + "\n";
        t += "Position " + Mathf.RoundToInt(e.pos.x) + "," + Mathf.RoundToInt(e.pos.y) + "   Terrain: " + World.TerNames[g.world.At(e.pos)] + "\n";
        if (u != null)
        {
            t += "Stance: " + u.stance + (u.retreating ? "  (retreating)" : "") + "   Order: " + u.ord + "\n";
            foreach (var w in u.weps) t += " - " + w.d.name + "  rng " + Mathf.RoundToInt(w.d.range) + "  dmg " + Mathf.RoundToInt(w.d.dmg) + (w.d.ammo > 0 ? "  ammo " + w.ammo : "") + "\n";
        }
        if (e.def.desc != "") t += e.def.desc;
        var st = new GUIStyle(sSmall); st.wordWrap = true; st.alignment = TextAnchor.UpperLeft; st.normal.textColor = new Color(0.82f, 0.87f, 0.9f);
        GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 12, r.height - 6), t, st);
    }

    static void DrawCommands(Game g, Rect r)
    {
        var us = g.SelUnits();
        float x = r.x, y = r.y, bw = 76, bh = 22, gap = 3; float cx = x, cy = y;
        Action<string, Action, bool> B = (label, act, on) =>
        {
            if (cx + bw > r.xMax) { cx = x; cy += bh + gap; }
            if (Btn(new Rect(cx, cy, bw, bh), label, on)) act();
            cx += bw + gap;
        };
        bool anyMove = false, anyCargo = false, anyAir = false, anyFlare = false, anyVeh = false, anyOp = false, anyBridge = false, anyDrone = false, anyHeavy = false;
        foreach (var u in us)
        {
            if (u.move != Move.None) anyMove = true; if (u.cap > 0) anyCargo = true; if (u is Aircraft && !(u is Drone)) anyAir = true; if (u.def.flares) anyFlare = true;
            if (u is Vehicle) anyVeh = true; if (u is Soldier && ((Soldier)u).role == Role.Operator) anyOp = true; if (u.def.aura == Aura.Bridge) anyBridge = true; if (u is Drone) anyDrone = true; if (u.weps.Count > 0) anyHeavy = true;
        }
        if (us.Count > 0)
        {
            B("Stop [X]", g.CmdStop, false);
            if (anyMove) { B("Return [R]", g.CmdReturn, false); B("Follow [G]", g.CmdFollow, g.followMode); }
            if (anyHeavy) { B("Stance: " + us[0].stance, () => { var s = (Stance)(((int)us[0].stance + 1) % 4); g.CmdStance(s); }, false); }
            if (anyCargo) { B("Unload [U]", g.CmdUnload, false); B("Load [L]", g.CmdLoadNearby, false); }
            if (anyVeh || anyAir) B("Evacuate", g.CmdEvacuate, false);
            if (anyAir) { B("Take off [T]", g.CmdTakeoff, false); B("Land [N]", g.CmdLand, false); B("Alt + [PgUp]", () => g.CmdLayer(1), false); B("Alt - [PgDn]", () => g.CmdLayer(-1), false); }
            if (anyFlare) B("Flares [Z]", g.CmdFlares, false);
            if (anyBridge) B("Bridge [B]", g.CmdBridge, false);
            if (anyDrone || anyOp) B("Recall drones", g.CmdRecall, false);
            if (anyOp)
            {
                Soldier op = null; foreach (var u in us) if (u is Soldier && ((Soldier)u).role == Role.Operator) { op = (Soldier)u; break; }
                if (op != null) for (int i = 0; i < op.kit.Count; i++) { int ki = i; var k = op.kit[i]; if (k.n > 0 || true) B("Launch " + ShortDrone(k.d) + " x" + k.n, () => g.CmdLaunch(ki), false); }
            }
        }
        B("Delete [Del]", g.CmdDelete, false);
        B("Deselect", g.ClearSel, false);
    }
    static string ShortDrone(UnitDef d)
    {
        if (d.special == "fpv") return "FPV"; if (d.special == "recon") return d.kind == Kind.Drone && d.style == 1 ? "Quad" : "UAV"; if (d.special == "ucav") return "UCAV"; if (d.special == "loiter") return "Loiter"; if (d.special == "bomber") return "Bomber"; return d.name;
    }

    // ------------------------------------------------------------------ drone camera PIP
    static void DrawDroneCam(Game g)
    {
        Drone d = g.sel.Count == 1 ? g.sel[0] as Drone : null;
        if (d == null || d.removed) return;
        Rect r = new Rect(8, 52, 262, 214); Panel(r);
        GUI.Label(new Rect(r.x + 8, r.y + 3, 240, 18), "DRONE CAMERA - " + d.def.name, sHead);
        Rect pic = new Rect(r.x + 8, r.y + 22, 246, 164);
        if (d.noSignal) { Tint(pic, new Color(0.05f, 0.05f, 0.06f, 1f)); var ns = new GUIStyle(sCenter); ns.fontSize = 20; ns.normal.textColor = Time.time % 1f < 0.6f ? new Color(1f, 0.3f, 0.2f) : Color.clear; GUI.Label(pic, "NO SIGNAL", ns); }
        else
        {
            GUI.DrawTexture(pic, g.droneRT, ScaleMode.StretchToFill, false);
            Tint(new Rect(pic.x + pic.width / 2 - 8, pic.y + pic.height / 2, 16, 1), new Color(0.4f, 1f, 0.5f, 0.6f)); Tint(new Rect(pic.x + pic.width / 2, pic.y + pic.height / 2 - 8, 1, 16), new Color(0.4f, 1f, 0.5f, 0.6f));
        }
        GUI.Label(new Rect(r.x + 8, r.y + 188, 250, 18), "BAT " + Mathf.RoundToInt(d.battery / Mathf.Max(1, d.maxBattery) * 100) + "%   SIG " + Mathf.RoundToInt(d.sig * 100) + "%   " + d.Status(), sSmall);
    }

    // ------------------------------------------------------------------ minimap
    static Rect miniRect;
    static void DrawMinimap(Game g)
    {
        if (!g.cfg.minimap) return;
        float mw = 180, mh = mw * g.world.h / g.world.w; if (mh > 130) { mh = 130; mw = mh * g.world.w / g.world.h; }
        miniRect = new Rect(8, VH - mh - 8, mw, mh); Panel(new Rect(miniRect.x - 3, miniRect.y - 3, mw + 6, mh + 6));
        if (g.world.TerTex != null) GUI.DrawTexture(miniRect, g.world.TerTex, ScaleMode.StretchToFill, false);
        float sx = mw / g.world.w, sy = mh / g.world.h;
        int step = Mathf.Max(1, g.world.ents.Count / 500);
        for (int i = 0; i < g.world.ents.Count; i += step)
        {
            var e = g.world.ents[i]; if (e.removed || e.hidden || e.kind == Kind.Prop) continue;
            Color c = e.dead ? new Color(0.15f, 0.15f, 0.15f) : (e.fac == Faction.Ukraine ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.35f, 0.3f));
            Tint(new Rect(miniRect.x + e.pos.x * sx - 1, miniRect.y + (g.world.h - e.pos.y) * sy - 1, 2.4f, 2.4f), c);
        }
        // camera rect
        float cw = g.camSize * 2f * g.cam.aspect * sx, ch = g.camSize * 2f * sy;
        Rect cr = new Rect(miniRect.x + g.camPos.x * sx - cw / 2, miniRect.y + (g.world.h - g.camPos.y) * sy - ch / 2, cw, ch);
        Tint(new Rect(cr.x, cr.y, cr.width, 1), Color.white); Tint(new Rect(cr.x, cr.yMax, cr.width, 1), Color.white); Tint(new Rect(cr.x, cr.y, 1, cr.height), Color.white); Tint(new Rect(cr.xMax, cr.y, 1, cr.height), Color.white);
        Event ev = Event.current;
        if ((ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag) && ev.button == 0)
        {
            Vector2 m = g.rawMouse / S;
            if (miniRect.Contains(m)) { g.camPos = new Vector2((m.x - miniRect.x) / sx, g.world.h - (m.y - miniRect.y) / sy); ev.Use(); }
        }
    }

    // ------------------------------------------------------------------ popups
    static void DrawMapPanel(Game g)
    {
        float w = 330, h = 470; Rect r = new Rect(8, 52, w, h);
        if (g.sel.Count == 1 && g.sel[0] is Drone) r.y = 274;
        Panel(r);
        float x = r.x + 10, y = r.y + 8, iw = w - 20; var wd = g.world;
        GUI.Label(new Rect(x, y, iw, 18), "WEATHER", sHead); y += 20;
        Weather[] ws = { Weather.Sunny, Weather.Clouds, Weather.Rain, Weather.Snow, Weather.Fog };
        string[] wn = { "Sunny", "Clouds", "Rain", "Snow", "Fog" };
        for (int i = 0; i < 5; i++) if (Btn(new Rect(x + i * (iw / 5f), y, iw / 5f - 3, 26), wn[i], wd.weather == ws[i])) wd.weather = ws[i];
        y += 34;
        GUI.Label(new Rect(x, y, iw, 18), "TIME OF DAY   " + string.Format("{0:00}:{1:00}", (int)wd.tod, (int)((wd.tod - (int)wd.tod) * 60)), sHead); y += 20;
        float nt = GUI.HorizontalSlider(new Rect(x, y + 3, iw, 18), wd.tod, 0f, 23.99f); if (Mathf.Abs(nt - wd.tod) > 0.001f) wd.tod = nt; y += 24;
        string[] tp = { "Dawn 5", "Noon 12", "Dusk 19", "Night 0" }; float[] tv = { 5.5f, 12f, 19f, 0.5f };
        for (int i = 0; i < 4; i++) if (Btn(new Rect(x + i * (iw / 4f), y, iw / 4f - 3, 24), tp[i])) wd.tod = tv[i];
        y += 30;
        wd.autoCycle = Toggle(new Rect(x, y, 200, 22), "Day / night cycle", wd.autoCycle);
        GUI.Label(new Rect(x + 180, y, 150, 22), "day = " + Mathf.RoundToInt(wd.dayLength) + " s", sSmall); y += 22;
        wd.dayLength = GUI.HorizontalSlider(new Rect(x, y + 3, iw, 18), wd.dayLength, 60f, 900f); y += 28;
        GUI.Label(new Rect(x, y, iw, 18), "MAP SIZE", sHead); y += 20;
        for (int i = 0; i < 4; i++) if (Btn(new Rect(x + i * (iw / 4f), y, iw / 4f - 3, 26), World.SizeNames[i], g.mapSizeIdx == i)) g.RegenerateMap(i, g.mapPreset, false);
        y += 32;
        GUI.Label(new Rect(x, y, iw, 18), "MAP TYPE   (" + wd.w + " x " + wd.h + ")", sHead); y += 20;
        for (int i = 0; i < 4; i++) if (Btn(new Rect(x + (i % 2) * (iw / 2f), y + (i / 2) * 28, iw / 2f - 3, 26), World.PresetNames[i], g.mapPreset == i)) g.RegenerateMap(g.mapSizeIdx, i, false);
        y += 60;
        if (Btn(new Rect(x, y, iw / 2 - 3, 26), "New random map")) g.RegenerateMap(g.mapSizeIdx, g.mapPreset, true);
        if (Btn(new Rect(x + iw / 2, y, iw / 2 - 3, 26), "Restart sandbox")) g.RestartSandbox();
        y += 32;
        if (Btn(new Rect(x, y, iw / 2 - 3, 26), "CLEAR ENTIRE MAP")) g.ClearMap();
        if (Btn(new Rect(x + iw / 2, y, iw / 2 - 3, 26), "Clear selection")) g.ClearSel();
        y += 36;
        GUI.Label(new Rect(x, y, iw, 18), "SAVE / LOAD SANDBOX", sHead); y += 20;
        for (int i = 0; i < 3; i++)
        {
            float bx = x + i * (iw / 3f);
            GUI.Label(new Rect(bx, y, iw / 3f, 16), "Slot " + (i + 1) + (g.HasSave(i) ? " *" : ""), sSmall);
            if (Btn(new Rect(bx, y + 16, iw / 6f - 2, 24), "Save")) g.SaveSandbox(i);
            if (Btn(new Rect(bx + iw / 6f, y + 16, iw / 6f - 2, 24), "Load")) g.LoadSandbox(i);
        }
    }

    static void DrawAIPanel(Game g)
    {
        float w = 330, h = 330; Rect r = new Rect(8, 52, w, h); Panel(r);
        float x = r.x + 10, y = r.y + 8, iw = w - 20; var ai = g.aiOpt;
        GUI.Label(new Rect(x, y, iw, 18), "AI BEHAVIOUR (all units)", sHead); y += 22;
        ai.autoEngage = Toggle(new Rect(x, y, iw, 22), "Detect and engage enemies automatically", ai.autoEngage); y += 24;
        ai.chase = Toggle(new Rect(x, y, iw, 22), "Aggressive units advance on spotted enemies", ai.chase); y += 24;
        ai.retreat = Toggle(new Rect(x, y, iw, 22), "Retreat when critically damaged", ai.retreat); y += 24;
        ai.reactToDamage = Toggle(new Rect(x, y, iw, 22), "React to damage (take cover, return fire)", ai.reactToDamage); y += 24;
        ai.wander = Toggle(new Rect(x, y, iw, 22), "Idle units patrol around their post", ai.wander); y += 24;
        ai.ceasefire = Toggle(new Rect(x, y, iw, 22), "CEASEFIRE - everyone holds fire", ai.ceasefire); y += 30;
        GUI.Label(new Rect(x, y, iw, 18), "STANCE OF SELECTED UNITS", sHead); y += 22;
        string[] sn = { "Aggressive", "Defensive", "Hold fire pos.", "Passive" };
        for (int i = 0; i < 4; i++) if (Btn(new Rect(x + (i % 2) * (iw / 2f), y + (i / 2) * 28, iw / 2f - 3, 26), sn[i])) g.CmdStance((Stance)i);
        y += 62;
        if (Btn(new Rect(x, y, iw / 2 - 3, 26), "Stop all selected")) g.CmdStop();
        if (Btn(new Rect(x + iw / 2, y, iw / 2 - 3, 26), "Return to origin")) g.CmdReturn();
        y += 32;
        if (Btn(new Rect(x, y, iw / 2 - 3, 26), "Select all " + Fac.Name(g.faction))) g.SelectAll(g.faction);
        if (Btn(new Rect(x + iw / 2, y, iw / 2 - 3, 26), "Follow leader")) g.CmdFollow();
    }

    static void DrawPauseMenu(Game g)
    {
        Tint(new Rect(0, 0, VW, VH), new Color(0, 0, 0, 0.55f));
        Reg(new Rect(0, 0, VW, VH));
        if (menuTab == 1) { DrawSettings(g, true); return; }
        float w = 340, h = 400; float x = (VW - w) / 2, y = (VH - h) / 2;
        Panel(new Rect(x, y, w, h));
        GUI.Label(new Rect(x + 20, y + 12, w, 30), "PAUSED", sTitle);
        float by = y + 56;
        if (Btn(new Rect(x + 30, by, w - 60, 38), "RESUME")) g.menuOpen = false; by += 46;
        if (Btn(new Rect(x + 30, by, w - 60, 38), "SETTINGS")) menuTab = 1; by += 46;
        if (Btn(new Rect(x + 30, by, w - 60, 38), "SAVE SANDBOX (slot " + (g.saveSlot + 1) + ")")) g.SaveSandbox(g.saveSlot); by += 46;
        if (Btn(new Rect(x + 30, by, w - 60, 38), "LOAD SANDBOX (slot " + (g.saveSlot + 1) + ")")) { g.LoadSandbox(g.saveSlot); g.menuOpen = false; } by += 46;
        for (int i = 0; i < 3; i++) if (Btn(new Rect(x + 30 + i * ((w - 60) / 3f), by, (w - 60) / 3f - 3, 26), "Slot " + (i + 1), g.saveSlot == i)) g.saveSlot = i;
        by += 40;
        if (Btn(new Rect(x + 30, by, w - 60, 38), "MAIN MENU")) { g.menuOpen = false; g.screen = Screen_.Menu; menuTab = 0; } by += 46;
        if (Btn(new Rect(x + 30, by, w - 60, 38), "EXIT GAME")) Quit();
        var cs = new GUIStyle(sSmall); cs.alignment = TextAnchor.MiddleCenter;
    }

    // ------------------------------------------------------------------ in-world overlays (health bars, status)
    static void DrawFloating(Game g)
    {
        if (Event.current.type != EventType.Repaint) return;
        var w = g.world; var cam = g.cam;
        float pxPerUnit = UnityEngine.Screen.height / (g.camSize * 2f) / S;
        bool bars = g.cfg.healthBars > 0 && !g.uiHidden;
        float hw = g.camSize * cam.aspect, hh = g.camSize;
        Rect vr = new Rect(g.camPos.x - hw - 3, g.camPos.y - hh - 3, hw * 2 + 6, hh * 2 + 6);
        // box selection
        if (g.IsBoxing)
        {
            Vector2 a = g.WorldToGUI(g.BoxA) / S, b = g.rawMouse / S;
            Rect br = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            Tint(br, new Color(0.3f, 0.8f, 0.4f, 0.12f));
            Tint(new Rect(br.x, br.y, br.width, 1), new Color(0.4f, 1f, 0.5f, 0.9f)); Tint(new Rect(br.x, br.yMax, br.width, 1), new Color(0.4f, 1f, 0.5f, 0.9f));
            Tint(new Rect(br.x, br.y, 1, br.height), new Color(0.4f, 1f, 0.5f, 0.9f)); Tint(new Rect(br.xMax, br.y, 1, br.height), new Color(0.4f, 1f, 0.5f, 0.9f));
        }
        if (!bars && g.sel.Count == 0) return;
        var ents = w.ents; int drawn = 0;
        for (int i = 0; i < ents.Count && drawn < 700; i++)
        {
            Ent e = ents[i];
            if (e.removed || e.hidden || !vr.Contains(e.pos) || e.kind == Kind.Prop && !(e is Unit)) continue;
            bool dmg = e.hp < e.maxHp - 0.5f && !e.dead;
            bool show = e.selected || (g.cfg.healthBars == 2 && !e.dead) || (g.cfg.healthBars == 1 && dmg);
            bool air = e.IsAirKind && !e.dead;
            if (!show && !(air && bars && g.camSize < 45f)) continue;
            Vector2 sp = g.WorldToGUI(e.pos) / S;
            float bw = Mathf.Clamp(e.radius * 2.2f * pxPerUnit, 24f, 90f);
            float by = sp.y - e.radius * pxPerUnit - 10 - (air ? e.alt * 6 : 0);
            if (show)
            {
                float f = Mathf.Clamp01(e.hp / Mathf.Max(1, e.maxHp));
                Tint(new Rect(sp.x - bw / 2 - 1, by - 1, bw + 2, 6), new Color(0, 0, 0, 0.75f));
                Tint(new Rect(sp.x - bw / 2, by, bw * f, 4), HpColor(f));
                Tint(new Rect(sp.x - bw / 2 - 5, by - 1, 3, 6), Fac.Color(e.fac));
            }
            if (air && (show || g.camSize < 45f))
            {
                // altitude layer pips + status indicator
                var a = e as Aircraft;
                if (a != null)
                {
                    int layer = a.state == AState.Parked ? 0 : (a.alt > 1.5f ? 2 : 1);
                    for (int k = 0; k < 3; k++) Tint(new Rect(sp.x - 7 + k * 5, by - 7, 4, 4), k < layer + (layer == 0 ? 0 : 0) && layer > 0 || (layer == 0 && k == 0) ? new Color(0.5f, 0.85f, 1f, 0.95f) : new Color(0.2f, 0.25f, 0.3f, 0.8f));
                    var d = e as Drone;
                    if (d != null && d.noSignal) { var ns = new GUIStyle(sCenter); ns.fontSize = 10; ns.normal.textColor = Time.time % 0.8f < 0.5f ? new Color(1f, 0.3f, 0.2f) : Color.clear; GUI.Label(new Rect(sp.x - 40, by - 20, 80, 12), "NO SIGNAL", ns); }
                    else if (d != null && e.selected) { Tint(new Rect(sp.x - bw / 2, by + 6, bw * Mathf.Clamp01(d.battery / Mathf.Max(1, d.maxBattery)), 2), new Color(0.4f, 0.8f, 1f)); }
                    if (a.state == AState.Crashing) { var cs = new GUIStyle(sCenter); cs.fontSize = 10; cs.normal.textColor = new Color(1f, 0.4f, 0.2f); GUI.Label(new Rect(sp.x - 40, by - 20, 80, 12), "MAYDAY", cs); }
                }
            }
            if (e.selected && e is Unit)
            {
                var u = (Unit)e;
                if (u.path != null && u.pi < u.path.Count && !(e is Aircraft)) { Vector2 dp = g.WorldToGUI(u.path[u.path.Count - 1]) / S; Tint(new Rect(dp.x - 2, dp.y - 2, 4, 4), new Color(0.4f, 1f, 0.5f, 0.8f)); }
                if (e is Aircraft && ((Aircraft)e).dest != Vector2.zero && u.ord == OrderT.Move) { Vector2 dp = g.WorldToGUI(u.dest) / S; Tint(new Rect(dp.x - 3, dp.y - 3, 6, 6), new Color(0.4f, 1f, 0.5f, 0.8f)); }
            }
            if (e.group >= 0) { }
            drawn++;
        }
    }
}
