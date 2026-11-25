using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum Screen_ { Menu, Settings, Controls, Sandbox }
public enum Tool { Select, Drag, Delete }

[Serializable]
public class Cfg
{
    public float masterVol = 0.8f;
    public int healthBars = 1;       // 0 off, 1 damaged/selected, 2 always
    public bool trackMarks = true;
    public int particles = 1;        // 0 low, 1 medium, 2 high
    public bool edgeScroll = false;
    public bool shake = true;
    public bool minimap = true;
    public bool showFps = false;
    public void Save() { PlayerPrefs.SetString("rts_cfg", JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
    public static Cfg Load()
    {
        try { if (PlayerPrefs.HasKey("rts_cfg")) { var c = JsonUtility.FromJson<Cfg>(PlayerPrefs.GetString("rts_cfg")); if (c != null) return c; } } catch { }
        return new Cfg();
    }
}

[Serializable]
public class AIOpt
{
    public bool autoEngage = true, retreat = true, reactToDamage = true, chase = true, wander = false, ceasefire = false, nightVision = false;
}

[Serializable] public class SaveEnt { public string id; public int fac, brig; public float x, y, rot, hp; public bool dead; }
[Serializable] public class SaveData { public int size, preset, seed, weather; public float tod; public List<SaveEnt> ents = new List<SaveEnt>(); public List<int> deadProps = new List<int>(); }

public class Game : MonoBehaviour
{
    public static Game I;
    public Screen_ screen = Screen_.Menu;
    public Cfg cfg; public AIOpt aiOpt = new AIOpt();
    public World world;
    public Camera cam; Camera droneCam; public RenderTexture droneRT;
    public List<Ent> sel = new List<Ent>();
    public List<Ent>[] groups = new List<Ent>[10];
    public Faction faction = Faction.Ukraine; public int brigIdx = 0;
    public UnitDef spawnDef; public int spawnCount = 1; public bool spawnLoaded = true; public float spawnFacing = 0f; public bool facingManual;
    public Tool tool = Tool.Select;
    public bool paused; public float simSpeed = 1f;
    public bool uiHidden; public bool manualFire;
    public int mapSizeIdx = 1, mapPreset = 0, mapSeed = 12345;
    public Cat cat = Cat.Infantry;
    public Vector2 camPos; public float camSize = 28f, camTarget = 28f;
    public Vector2 mouseWorld; public Vector2 rawMouse;
    string toastText = ""; float toastT;
    List<Soldier> corpses = new List<Soldier>();
    List<Prop> activeProps = new List<Prop>();
    float shakeAmt;
    SpriteRenderer tintSR, wxSR;
    List<SpriteRenderer> rain = new List<SpriteRenderer>(), snow = new List<SpriteRenderer>(), clouds = new List<SpriteRenderer>();
    float[] rainX, rainY, snowX, snowY, snowPh, cloudX, cloudY; float wxT;
    HashSet<KeyCode> held = new HashSet<KeyCode>();
    bool boxing; Vector2 boxStart; bool dragging; Ent dragEnt; Vector2 dragOffset; List<Ent> dragSet = new List<Ent>(); List<Vector2> dragOffsets = new List<Vector2>();
    bool mmbPan; Vector2 mmbLast; public bool followMode;
    public float fps; float fpsT; int fpsN;
    public int entCount;
    SpriteRenderer viewRing;
    public bool menuOpen;       // in-game pause menu
    public int saveSlot = 0;
    Ent lastClick; float lastClickT;
    SpriteRenderer ghost; SpriteRenderer[] ghostMulti = new SpriteRenderer[0];
    public Brigade Brig { get { return Db.Brigades[(int)faction][Mathf.Clamp(brigIdx, 0, Db.Brigades[(int)faction].Length - 1)]; } }
    public Brigade BrigOf(Faction f) { return Db.Brigades[(int)f][Mathf.Clamp(brigIdx, 0, Db.Brigades[(int)f].Length - 1)]; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindObjectOfType<Game>() != null) return;
        var go = new GameObject("RTS Game");
        go.AddComponent<Game>();
    }

    void Awake()
    {
        I = this;
        Application.runInBackground = true;
        Application.targetFrameRate = 120;
        cfg = Cfg.Load();
        Db.Build();
        Art.Spr.Init();
        Fx.Init(); Sfx.Init();
        for (int i = 0; i < 10; i++) groups[i] = new List<Ent>();
        SetupCamera();
        ApplyCfg();
        Atlas.Flush();
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera"); g.tag = "MainCamera"; cam = g.AddComponent<Camera>();
        }
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        cam.orthographic = true; cam.orthographicSize = camSize; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = G.Hex(0x0e151c);
        cam.transform.position = new Vector3(0, 0, -50); cam.transform.rotation = Quaternion.identity; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
        cam.allowMSAA = false;
        var dg = new GameObject("DroneCam"); dg.transform.SetParent(transform);
        droneCam = dg.AddComponent<Camera>(); droneCam.orthographic = true; droneCam.orthographicSize = 13f; droneCam.clearFlags = CameraClearFlags.SolidColor; droneCam.backgroundColor = Color.black;
        droneRT = new RenderTexture(320, 240, 16); droneCam.targetTexture = droneRT; droneCam.cullingMask = ~(1 << 30); droneCam.enabled = false; droneCam.nearClipPlane = 0.1f; droneCam.farClipPlane = 200f;
        droneCam.transform.rotation = Quaternion.identity;
        // overlays
        tintSR = MakeOverlay("TimeTint", 70); wxSR = MakeOverlay("WeatherTint", 71);
    }
    SpriteRenderer MakeOverlay(string n, int order)
    {
        var g = new GameObject(n); g.layer = 30; g.transform.SetParent(transform);
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = Art.Spr.white; sr.sortingOrder = order; sr.color = new Color(0, 0, 0, 0);
        return sr;
    }

    public void ApplyCfg()
    {
        Sfx.master = cfg.masterVol;
        Fx.quality = cfg.particles == 0 ? 0.45f : (cfg.particles == 1 ? 1f : 1.6f);
        Fx.maxParts = cfg.particles == 0 ? 1200 : (cfg.particles == 1 ? 3000 : 5000);
    }

    // ------------------------------------------------------------------ world management
    public void NewWorld(bool keepUnits = false)
    {
        sel.Clear(); foreach (var g in groups) g.Clear();
        corpses.Clear(); activeProps.Clear();
        Proj.Clear(); Fx.Clear();
        float tod = 11f; Weather w = Weather.Sunny; bool auto = false;
        if (world != null) { tod = world.tod; w = world.weather; auto = world.autoCycle; world.Destroy(); }
        world = new World();
        world.tod = tod; world.weather = w; world.autoCycle = auto;
        world.Generate(mapSizeIdx, mapPreset, mapSeed);
        if (mapPreset == 3 && w != Weather.Snow) world.weather = Weather.Snow;
        foreach (var ps in world.pending)
        {
            UnitDef d = Db.Get(ps.id); if (d == null) continue;
            Brigade b = ps.fac == Faction.Neutral ? null : Db.Brigades[(int)ps.fac][0];
            Prop.Spawn(d, b, ps.fac, new Vector2(ps.x, ps.y), ps.rot, ps.v);
        }
        world.pending.Clear();
        camPos = new Vector2(world.w * 0.5f, world.h * 0.5f);
        camTarget = camSize = Mathf.Clamp(world.h * 0.3f, 22f, 60f);
        SetupWeatherFx();
    }

    public void StartSandbox(bool demo)
    {
        screen = Screen_.Sandbox; menuOpen = false;
        NewWorld();
        if (demo) SpawnDemo();
        Sfx.Ui(true);
    }

    public void RestartSandbox() { world.ClearUnitsKeepProps(); sel.Clear(); corpses.Clear(); Proj.Clear(); Fx.Clear(); foreach (var g in groups) g.Clear(); Toast("Sandbox restarted"); }
    public void ClearMap()
    {
        world.ClearUnitsKeepProps(); sel.Clear(); corpses.Clear(); Proj.Clear(); Fx.Clear(); foreach (var g in groups) g.Clear();
        // remove destroyed props too: leave terrain decals reset
        Toast("Map cleared");
    }
    public void RegenerateMap(int size, int preset, bool newSeed)
    {
        mapSizeIdx = size; mapPreset = preset; if (newSeed) mapSeed = UnityEngine.Random.Range(1, 999999);
        NewWorld(); Toast("New map: " + World.SizeNames[size] + " / " + World.PresetNames[preset]);
    }

    // ------------------------------------------------------------------ spawning
    public bool ValidSpawn(UnitDef d, Vector2 p)
    {
        if (!world.InBounds(p)) return false;
        bool water = world.Water(p);
        switch (d.kind)
        {
            case Kind.Ship: return water && world.At(p) != World.RIVER || (water && d.len < 8f);
            case Kind.Plane: case Kind.Heli: case Kind.Drone: return true;
            case Kind.Prop: return !water || d.style == 31;
            default: return !water || world.At(p) == World.BRIDGE;
        }
    }

    public static float Spacing(UnitDef d)
    {
        switch (d.kind)
        {
            case Kind.Soldier: return 1.2f;
            case Kind.Plane: return Mathf.Max(8f, d.len * 1.3f);
            case Kind.Heli: return Mathf.Max(7f, d.len * 1.3f);
            case Kind.Drone: return 3f;
            case Kind.Ship: return d.len + 3f;
            case Kind.Prop: return Mathf.Max(d.len, d.wid) + 0.6f;
            default: return Mathf.Max(d.len, d.wid) + 1.2f;
        }
    }

    public void SpawnAt(UnitDef d, Vector2 p, int count)
    {
        if (d == null) return;
        float rot = facingManual ? spawnFacing : (faction == Faction.Ukraine ? 0f : 180f);
        if (d.mask == 2 && faction == Faction.Ukraine || d.mask == 1 && faction == Faction.Russia) { }
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        float sp = Spacing(d);
        int placed = 0, rejected = 0;
        Vector2 fwd = G.Dir(rot), side = G.Dir(rot + 90f);
        for (int i = 0; i < count; i++)
        {
            int cx = i % cols, cy = i / cols;
            Vector2 off = side * ((cx - (cols - 1) * 0.5f) * sp) + fwd * (-cy * sp);
            Vector2 q = p + off;
            if (count > 1) q += UnityEngine.Random.insideUnitCircle * sp * 0.08f;
            if (!ValidSpawn(d, q)) { rejected++; continue; }
            Ent e = SpawnDef(d, q, rot, faction, Brig, spawnLoaded);
            if (e != null) placed++;
        }
        if (placed == 0 && rejected > 0) Toast(d.kind == Kind.Ship ? "Ships can only be placed on open water" : "Cannot place " + d.name + " on water");
        if (placed > 0) Sfx.Play(Sfx.Id.UiClick, p, 0.8f);
    }

    public Ent SpawnDef(UnitDef d, Vector2 p, float rot, Faction f, Brigade b, bool loaded)
    {
        if (world.ents.Count > 4000) { Toast("Entity limit reached"); return null; }
        switch (d.fam)
        {
            case "soldier":
                { var s = Soldier.Spawn(d, b, f, p, rot); return s; }
            case "tracked": case "wheeled":
                {
                    var v = Vehicle.Spawn(d, b, f, p, rot);
                    if (loaded && d.cap > 0 && d.cat != Cat.Artillery && d.cat != Cat.AirDefense && d.cat != Cat.MLRS && d.cat != Cat.Engineering)
                    {
                        string sid = d.cat == Cat.Trucks ? "inf_moto" : (d.cat == Cat.Cars ? "inf_rifle" : (b != null && b.nick == "Black Tridents" || b != null && b.nick == "Black Sea" ? "inf_naval" : "inf_mech"));
                        int n = d.cat == Cat.Trucks ? Mathf.Min(d.cap, 8) : (d.cat == Cat.Cars ? Mathf.Min(d.cap, 3) : Mathf.Min(d.cap, 6));
                        if (d.aura == Aura.Heal) n = 0;
                        for (int i = 0; i < n; i++) { var s = Soldier.Spawn(Db.Get(sid), b, f, p, rot); v.Load(s); }
                    }
                    return v;
                }
            case "plane": case "heli":
                {
                    var a = Aircraft.SpawnAir(d, b, f, p, rot, true);
                    if (loaded && d.cap > 0 && d.special == "transport") { a.inFlightLoad = true; a.Preload(Db.Get(d.kind == Kind.Plane ? "inf_para" : "inf_mech"), d.kind == Kind.Plane ? Mathf.Min(d.cap, 20) : Mathf.Min(d.cap, 8), b); a.inFlightLoad = false; }
                    return a;
                }
            case "drone":
                {
                    var op = Soldier.Spawn(Db.Get("inf_uav"), b, f, p, rot);
                    int n = (d.special == "fpv" || d.special == "loiter" || d.special == "bomber") ? 4 : 2;
                    if (d.special == "ucav") n = 1;
                    op.ParseKit(d.id + ":" + n);
                    return op;
                }
            case "ship":
                {
                    var sh = Ship.Spawn(d, b, f, p, rot);
                    if (loaded && d.cap >= 20 && d.cat == Cat.Ships) { for (int i = 0; i < Mathf.Min(d.cap, 24); i++) { var s = Soldier.Spawn(Db.Get("inf_naval"), b, f, p, rot); sh.Load(s); } }
                    return sh;
                }
            case "prop":
                {
                    if (d.w.Length > 0) return Emplacement.Spawn(d, b, f, p, rot);
                    var pr = Prop.Spawn(d, b, d.special == "tree" ? Faction.Neutral : f, p, rot, UnityEngine.Random.Range(0, 10));
                    pr.fac = d.cat == Cat.Other && (d.style == 31 || d.style == 26 || d.style == 27 || d.style == 32 || d.style == 13) ? f : Faction.Neutral;
                    return pr;
                }
            case "special": return SpawnSpecial(d, p, rot, f, b);
        }
        return null;
    }

    Ent SpawnSpecial(UnitDef d, Vector2 p, float rot, Faction f, Brigade b)
    {
        Vector2 back = G.Dir(rot + 180f);
        switch (d.special)
        {
            case "para":
                {
                    var pd = Db.Get("inf_para"); Ent last = null;
                    for (int i = 0; i < d.loadN; i++)
                    {
                        Vector2 q = p + UnityEngine.Random.insideUnitCircle * (3f + d.loadN * 0.45f);
                        if (!world.InBounds(q)) continue;
                        var s = Soldier.Spawn(pd, b, f, q, rot); s.StartChute(q, 14f + UnityEngine.Random.value * 14f); s.home = q; last = s;
                    }
                    return last;
                }
            case "pilot":
                { var s = Soldier.Spawn(Db.Get("inf_pilot"), b, f, p, rot); s.StartChute(p, 16f); return s; }
            case "il76": case "c130":
                {
                    UnitDef pd = Db.Get(d.special == "il76" ? "av_il76" : "av_c130");
                    Vector2 st = world.Clamp(p + back * 110f, 6f);
                    var a = (Aircraft)Aircraft.SpawnAir(pd, b, f, st, G.Ang(p - st), true);
                    a.inFlightLoad = true; a.Preload(Db.Get("inf_para"), d.loadN, b); a.inFlightLoad = false;
                    a.OrderParadrop(p); return a;
                }
            case "mi8": case "uh60":
                {
                    UnitDef hd = Db.Get(d.special == "mi8" ? "he_mi8" : "he_uh60");
                    Vector2 st = world.Clamp(p + back * 90f, 6f);
                    var a = Aircraft.SpawnAir(hd, b, f, st, G.Ang(p - st), true);
                    a.inFlightLoad = true; a.Preload(Db.Get("inf_mech"), d.loadN, b); a.inFlightLoad = false;
                    a.OrderAssault(p); return a;
                }
            case "landing":
                {
                    // sail in from open water below the clicked shore point
                    Vector2 w = Vector2.zero; bool found = false;
                    for (float dy = 0; dy < 160 && !found; dy += 2f) { Vector2 q = p + new Vector2(0, -dy); if (world.InBounds(q) && world.At(q) == World.SEA && dy > 20f) { w = q + new Vector2(0, -12f); found = true; } }
                    if (!found) { w = world.NearestOfType(p, t => t == World.SEA || t == World.LAKE, 120f); found = world.Water(w); }
                    if (!found) { Toast("No water near the target for a landing craft"); return null; }
                    var sh = Ship.Spawn(Db.Get("sh_lcu"), b, f, w, G.Ang(p - w));
                    for (int i = 0; i < d.loadN; i++) { var s = Soldier.Spawn(Db.Get("inf_naval"), b, f, w, 0); sh.Load(s); }
                    sh.OrderLandTroops(p); return sh;
                }
        }
        return null;
    }

    void SpawnDemo()
    {
        Vector2 ua = world.baseSites.Count > 0 ? world.baseSites[0] : new Vector2(world.w * 0.25f, world.h * 0.6f);
        Vector2 ru = world.baseSites.Count > 1 ? world.baseSites[1] : new Vector2(world.w * 0.75f, world.h * 0.6f);
        if ((ua.x > ru.x)) { var t = ua; ua = ru; ru = t; }
        Vector2 mid = (ua + ru) * 0.5f;
        SpawnGroup("inf_rifle", 8, mid + new Vector2(-30, 0), Faction.Ukraine, 1, 0); SpawnGroup("inf_mg", 2, mid + new Vector2(-34, 3), Faction.Ukraine, 1, 0);
        SpawnGroup("t_leo2", 3, mid + new Vector2(-45, 4), Faction.Ukraine, 1, 0); SpawnGroup("ifv_cv90", 2, mid + new Vector2(-55, -6), Faction.Ukraine, 4, 0);
        SpawnGroup("inf_uav", 1, mid + new Vector2(-50, 12), Faction.Ukraine, 5, 0); SpawnGroup("art_paladin", 1, mid + new Vector2(-70, 6), Faction.Ukraine, 4, 0);
        SpawnGroup("inf_medic", 1, mid + new Vector2(-52, 3), Faction.Ukraine, 1, 0); SpawnGroup("ad_gepard", 1, mid + new Vector2(-62, 10), Faction.Ukraine, 4, 0);
        SpawnGroup("he_apache", 1, mid + new Vector2(-60, 22), Faction.Ukraine, 6, 0); SpawnGroup("av_f16", 1, mid + new Vector2(-70, 30), Faction.Ukraine, 2, 0);
        SpawnGroup("inf_rifle", 8, mid + new Vector2(30, 0), Faction.Russia, 1, 180); SpawnGroup("inf_mg", 2, mid + new Vector2(34, -3), Faction.Russia, 1, 180);
        SpawnGroup("t_t90m", 3, mid + new Vector2(45, -4), Faction.Russia, 1, 180); SpawnGroup("ifv_bmp3", 2, mid + new Vector2(55, 6), Faction.Russia, 4, 180);
        SpawnGroup("inf_uav", 1, mid + new Vector2(50, -12), Faction.Russia, 5, 180); SpawnGroup("art_msta", 1, mid + new Vector2(70, -6), Faction.Russia, 6, 180);
        SpawnGroup("ad_tor", 1, mid + new Vector2(62, -10), Faction.Russia, 1, 180); SpawnGroup("he_ka52", 1, mid + new Vector2(60, -22), Faction.Russia, 2, 180);
        SpawnGroup("av_su34", 1, mid + new Vector2(70, -30), Faction.Russia, 6, 180); SpawnGroup("ml_grad", 1, mid + new Vector2(75, 8), Faction.Russia, 6, 180);
        camPos = mid;
    }
    void SpawnGroup(string id, int n, Vector2 c, Faction f, int bi, float rot)
    {
        UnitDef d = Db.Get(id); if (d == null) return;
        Brigade b = Db.Brigades[(int)f][Mathf.Clamp(bi, 0, Db.Brigades[(int)f].Length - 1)];
        int cols = Mathf.CeilToInt(Mathf.Sqrt(n)); float sp = Spacing(d);
        for (int i = 0; i < n; i++)
        {
            Vector2 q = c + new Vector2((i / cols) * sp * (rot == 0 ? -1 : 1), ((i % cols) - (cols - 1) * 0.5f) * sp);
            if (!ValidSpawn(d, q)) q = world.NearestOfType(q, t => !World.IsWater(t), 30f);
            SpawnDef(d, q, rot, f, b, true);
        }
    }

    public Vector2 EnemySide(Faction f)
    {
        Vector2 best = new Vector2(world.w * (f == Faction.Ukraine ? 0.75f : 0.25f), world.h * 0.6f);
        Vector2 sum = Vector2.zero; int n = 0;
        foreach (var e in world.ents) if (!e.removed && !e.dead && e.fac != f && e.fac != Faction.Neutral && e.kind != Kind.Drone) { sum += e.pos; n++; }
        return n > 0 ? sum / n : best;
    }

    // ------------------------------------------------------------------ misc helpers
    public void Toast(string t) { toastText = t; toastT = 3f; }
    public string ToastText { get { return toastT > 0 ? toastText : ""; } }
    public void Shake(float a, Vector2 at)
    {
        if (!cfg.shake) return;
        float d = (at - camPos).magnitude;
        float k = Mathf.Clamp01(1f - d / (camSize * 2.5f + 10f));
        shakeAmt = Mathf.Min(1.2f, shakeAmt + a * k);
    }
    public void RegisterCorpse(Soldier s)
    {
        corpses.Add(s);
        if (corpses.Count > 450)
        {
            var o = corpses[0]; corpses.RemoveAt(0);
            if (o != null && !o.removed) { world.StampCircle(o.pos, 0.5f, new Color(0.25f, 0.05f, 0.04f, 0.6f), 0.3f); o.Destroy(); }
        }
    }
    public void AddActive(Prop p) { if (!activeProps.Contains(p)) activeProps.Add(p); }
    public void OnPropDestroyed(Prop p) { }

    // ------------------------------------------------------------------ selection & commands
    public void ClearSel() { foreach (var e in sel) if (e != null) e.SetSelected(false); sel.Clear(); }
    public void Select(Ent e, bool add)
    {
        if (!add) ClearSel();
        if (e == null || e.removed) return;
        if (sel.Contains(e)) { if (add) { e.SetSelected(false); sel.Remove(e); } return; }
        sel.Add(e); e.SetSelected(true);
    }
    public void BoxSelect(Vector2 a, Vector2 b, bool add)
    {
        if (!add) ClearSel();
        Rect r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        foreach (var e in world.ents)
        {
            if (e.removed || e.hidden || e.dead || !(e is Unit) || e.kind == Kind.Prop) continue;
            if (r.Contains(e.pos) && !sel.Contains(e)) { sel.Add(e); e.SetSelected(true); }
        }
    }
    public List<Unit> SelUnits() { var l = new List<Unit>(); foreach (var e in sel) { var u = e as Unit; if (u != null && !u.removed && !u.dead) l.Add(u); } return l; }

    public void CmdMove(Vector2 p, Ent target)
    {
        var us = SelUnits(); if (us.Count == 0) return;
        if (followMode && target != null) { foreach (var u in us) if (u != target) u.OrderFollow(target); followMode = false; Toast("Following " + target.def.name); return; }
        followMode = false;
        // operators and drones have special right-click behaviour
        bool enemy = target != null && !target.dead && us[0].IsEnemyOf(target);
        // soldiers boarding a friendly transport
        if (target is Unit && target.fac == us[0].fac && ((Unit)target).cap > 0 && !target.dead)
        {
            bool any = false;
            foreach (var u in us) { var s = u as Soldier; if (s != null && target != s) { s.OrderBoard((Unit)target); any = true; } }
            if (any) { Sfx.Ui(); return; }
        }
        int n = us.Count; int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
        float sp = 0; foreach (var u in us) sp = Mathf.Max(sp, Spacing(u.def));
        sp = Mathf.Min(sp, 10f);
        int idx = 0;
        foreach (var u in us)
        {
            if (u is Drone) { var dr = (Drone)u; if (enemy) dr.Retarget(target); else dr.SendTo(p); continue; }
            var op = u as Soldier;
            if (op != null && op.role == Role.Operator && enemy && op.kit.Count > 0)
            {
                KitItem k = null; foreach (var ki in op.kit) if (ki.n > 0 && (ki.d.special == "fpv" || ki.d.special == "loiter" || ki.d.special == "bomber" || ki.d.special == "ucav")) { k = ki; break; }
                if (k != null) { op.LaunchDrone(k, target.pos, target); continue; }
            }
            if (enemy && u.weps.Count > 0 && !(u is Soldier && ((Soldier)u).role == Role.Medic)) { u.OrderAttack(target); continue; }
            if (u is Ship && ((Ship)u).cargo.Count > 0 && !world.Water(p) && u.cargo.Count > 0) { ((Ship)u).OrderLandTroops(p); continue; }
            int cx = idx % cols, cy = idx / cols; idx++;
            Vector2 off = n > 1 ? new Vector2((cx - (cols - 1) * 0.5f) * sp, (cy - (n / cols) * 0.5f) * sp) : Vector2.zero;
            Vector2 dst = p + off;
            if (u is Aircraft && ((Aircraft)u).isHeli && target == null && false) { }
            u.OrderMove(dst);
        }
        Sfx.Ui();
    }
    public void CmdStop() { foreach (var u in SelUnits()) u.OrderStop(); }
    public void CmdReturn() { foreach (var u in SelUnits()) u.OrderReturn(); }
    public void CmdFollow()
    {
        var us = SelUnits();
        if (us.Count >= 2) { for (int i = 1; i < us.Count; i++) us[i].OrderFollow(us[0]); Toast("Group follows the first selected unit"); }
        else if (us.Count == 1) { followMode = true; Toast("Right-click the unit to follow"); }
    }
    public void CmdUnload() { foreach (var u in SelUnits()) u.CmdUnload(); }
    public void CmdLoadNearby()
    {
        foreach (var u in SelUnits())
        {
            if (u.cap <= 0 || u.cargo.Count >= u.cap) continue;
            var l = new List<Ent>(); world.Query(u.pos, 22f, l, false);
            int n = 0;
            foreach (var e in l) { var s = e as Soldier; if (s != null && !s.dead && !s.hidden && s.fac == u.fac && !s.chuting && u.cargo.Count + n < u.cap) { s.OrderBoard(u); n++; } }
            if (n == 0) Toast("No friendly infantry nearby");
        }
    }
    public void CmdFlares() { foreach (var u in SelUnits()) u.LaunchFlares(); }
    public void CmdEvacuate() { foreach (var u in SelUnits()) { var v = u as Vehicle; if (v != null) v.Evacuate(); else u.CmdUnload(); } }
    public void CmdStance(Stance s) { foreach (var u in SelUnits()) u.stance = s; }
    public void CmdTakeoff() { foreach (var u in SelUnits()) { var a = u as Aircraft; if (a != null) a.OrderTakeoff(); } }
    public void CmdLand() { foreach (var u in SelUnits()) { var a = u as Aircraft; if (a != null) a.OrderLand(a.pos + G.Dir(a.rot) * 4f); } }
    public void CmdLayer(int dl) { foreach (var u in SelUnits()) { var a = u as Aircraft; if (a != null) a.SetLayer(Mathf.RoundToInt(a.targetAlt) + dl); } }
    public void CmdBridge() { foreach (var u in SelUnits()) { var v = u as Vehicle; if (v != null) v.DeployBridge(); } }
    public void CmdRecall() { foreach (var u in SelUnits()) { var d = u as Drone; if (d != null) d.Recall(); var s = u as Soldier; if (s != null) foreach (var dd in s.drones) if (dd != null) dd.Recall(); } }
    public void CmdLaunch(int kitIdx)
    {
        foreach (var u in SelUnits())
        {
            var s = u as Soldier; if (s == null || s.role != Role.Operator || kitIdx >= s.kit.Count) continue;
            var k = s.kit[kitIdx]; if (k.n <= 0) continue;
            Ent tgt = s.tgtG; Vector2 p;
            if (k.d.special == "recon" || k.d.special == "ucav") { Vector2 toward = (EnemySide(s.fac) - s.pos).normalized; p = s.pos + toward * Mathf.Min(70f, k.d.signal * 0.35f); tgt = null; }
            else p = tgt != null ? tgt.pos : s.pos + G.Dir(s.rot) * 30f;
            s.LaunchDrone(k, p, k.d.special == "recon" ? null : tgt);
        }
    }
    public void CmdDelete()
    {
        foreach (var e in new List<Ent>(sel)) { e.Destroy(); }
        sel.Clear();
    }
    public void AssignGroup(int g) { groups[g].Clear(); foreach (var e in sel) groups[g].Add(e); Toast("Group " + g + " assigned (" + sel.Count + ")"); }
    public void RecallGroup(int g)
    {
        groups[g].RemoveAll(e => e == null || e.removed || e.dead);
        if (groups[g].Count == 0) return;
        ClearSel(); foreach (var e in groups[g]) { sel.Add(e); e.SetSelected(true); }
        if (Time.unscaledTime - lastGroupT < 0.35f && lastGroup == g) FocusSel();
        lastGroupT = Time.unscaledTime; lastGroup = g;
    }
    float lastGroupT; int lastGroup = -1;
    public void FocusSel() { if (sel.Count == 0) return; Vector2 c = Vector2.zero; foreach (var e in sel) c += e.pos; camPos = c / sel.Count; }
    public void SelectAll(Faction f) { ClearSel(); foreach (var e in world.ents) if (!e.removed && !e.dead && !e.hidden && e.fac == f && e is Unit && e.kind != Kind.Prop) { sel.Add(e); e.SetSelected(true); } }

    // ------------------------------------------------------------------ save / load
    string SlotPath(int slot) { return Path.Combine(Application.persistentDataPath, "sandbox_slot" + slot + ".json"); }
    public bool HasSave(int slot) { try { return File.Exists(SlotPath(slot)); } catch { return false; } }
    public void SaveSandbox(int slot)
    {
        var sd = new SaveData { size = mapSizeIdx, preset = mapPreset, seed = mapSeed, tod = world.tod, weather = (int)world.weather };
        foreach (var e in world.ents)
        {
            if (e.removed || e.hidden) continue;
            if (e is Soldier && ((Soldier)e).chuting) { }
            sd.ents.Add(new SaveEnt { id = e.def.id, fac = (int)e.fac, brig = e.brig != null ? e.brig.idx : 0, x = e.pos.x, y = e.pos.y, rot = e.rot, hp = e.hp, dead = e.dead });
        }
        for (int i = 0; i < world.props.Count; i++) if (world.props[i].dead) sd.deadProps.Add(i);
        try { File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(sd)); Toast("Sandbox saved to slot " + (slot + 1) + " (" + sd.ents.Count + " objects)"); }
        catch (Exception ex) { Toast("Save failed: " + ex.Message); }
    }
    public void LoadSandbox(int slot)
    {
        try
        {
            if (!HasSave(slot)) { Toast("Slot " + (slot + 1) + " is empty"); return; }
            var sd = JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
            mapSizeIdx = sd.size; mapPreset = sd.preset; mapSeed = sd.seed;
            NewWorld();
            world.tod = sd.tod; world.weather = (Weather)sd.weather;
            foreach (int i in sd.deadProps) if (i >= 0 && i < world.props.Count) { var p = world.props[i] as Prop; if (p != null) p.ForceRuin(); }
            var killList = new List<Ent>();
            foreach (var se in sd.ents)
            {
                UnitDef d = Db.Get(se.id); if (d == null) continue;
                Faction f = (Faction)se.fac; Brigade b = f == Faction.Neutral ? null : Db.Brigades[(int)f][Mathf.Clamp(se.brig, 0, Db.Brigades[(int)f].Length - 1)];
                Ent e = SpawnDef(d, new Vector2(se.x, se.y), se.rot, f, b, false);
                if (e == null) continue;
                if (se.dead) killList.Add(e); else if (se.hp > 0 && e.maxHp > 0) e.hp = Mathf.Min(e.maxHp, se.hp);
            }
            foreach (var e in killList) e.Hit(99999f, 99f, e.pos, null, true);
            Toast("Sandbox loaded from slot " + (slot + 1));
        }
        catch (Exception ex) { Toast("Load failed: " + ex.Message); }
    }

    // ------------------------------------------------------------------ weather effects
    void SetupWeatherFx()
    {
        foreach (var l in new[] { rain, snow, clouds }) { foreach (var sr in l) if (sr != null) Destroy(sr.gameObject); l.Clear(); }
        rainX = new float[260]; rainY = new float[260];
        for (int i = 0; i < 260; i++) { rainX[i] = UnityEngine.Random.value; rainY[i] = UnityEngine.Random.value; var sr = MakeWx("rain", Art.Spr.drop, 68); rain.Add(sr); }
        snowX = new float[180]; snowY = new float[180]; snowPh = new float[180];
        for (int i = 0; i < 180; i++) { snowX[i] = UnityEngine.Random.value; snowY[i] = UnityEngine.Random.value; snowPh[i] = UnityEngine.Random.value * 6f; snow.Add(MakeWx("snow", Art.Spr.flake, 68)); }
        cloudX = new float[16]; cloudY = new float[16];
        for (int i = 0; i < 16; i++) { cloudX[i] = UnityEngine.Random.value; cloudY[i] = UnityEngine.Random.value; var sr = MakeWx("cloud", Art.Spr.cloud, 66); clouds.Add(sr); }
    }
    SpriteRenderer MakeWx(string n, Sprite s, int order)
    {
        var g = new GameObject(n); g.layer = 30; g.transform.SetParent(transform);
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = order; g.SetActive(false);
        return sr;
    }

    void UpdateWeather(float dt)
    {
        var w = world.weather; wxT += dt;
        float vw = camSize * 2f * cam.aspect + 6f, vh = camSize * 2f + 6f;
        Vector2 c = camPos;
        bool r = w == Weather.Rain, s = w == Weather.Snow;
        float sc = Mathf.Clamp(camSize / 22f, 0.6f, 3f);
        for (int i = 0; i < rain.Count; i++)
        {
            if (!r) { if (rain[i].gameObject.activeSelf) rain[i].gameObject.SetActive(false); continue; }
            rain[i].gameObject.SetActive(true);
            rainY[i] -= dt * (1.6f + (i % 5) * 0.12f); rainX[i] -= dt * 0.25f;
            if (rainY[i] < 0f) { rainY[i] += 1f; rainX[i] = UnityEngine.Random.value; }
            if (rainX[i] < 0f) rainX[i] += 1f;
            rain[i].transform.position = new Vector3(c.x + (rainX[i] - 0.5f) * vw, c.y + (rainY[i] - 0.5f) * vh, 0);
            rain[i].transform.localScale = new Vector3(sc, sc, 1); rain[i].transform.rotation = Quaternion.Euler(0, 0, 14f);
        }
        for (int i = 0; i < snow.Count; i++)
        {
            if (!s) { if (snow[i].gameObject.activeSelf) snow[i].gameObject.SetActive(false); continue; }
            snow[i].gameObject.SetActive(true);
            snowY[i] -= dt * (0.12f + (i % 4) * 0.03f); snowX[i] += Mathf.Sin(wxT * 0.9f + snowPh[i]) * dt * 0.03f - dt * 0.02f;
            if (snowY[i] < 0f) { snowY[i] += 1f; snowX[i] = UnityEngine.Random.value; }
            if (snowX[i] < 0f) snowX[i] += 1f; if (snowX[i] > 1f) snowX[i] -= 1f;
            snow[i].transform.position = new Vector3(c.x + (snowX[i] - 0.5f) * vw, c.y + (snowY[i] - 0.5f) * vh, 0);
            snow[i].transform.localScale = Vector3.one * sc * (1f + (i % 3) * 0.4f);
        }
        bool cl = w == Weather.Clouds || w == Weather.Rain || w == Weather.Snow || w == Weather.Fog;
        int ncl = w == Weather.Clouds ? 16 : (w == Weather.Sunny ? 0 : 9);
        for (int i = 0; i < clouds.Count; i++)
        {
            if (i >= ncl) { if (clouds[i].gameObject.activeSelf) clouds[i].gameObject.SetActive(false); continue; }
            clouds[i].gameObject.SetActive(true);
            cloudX[i] += dt * 0.008f * (1f + (i % 3) * 0.3f); if (cloudX[i] > 1f) cloudX[i] -= 1f;
            clouds[i].transform.position = new Vector3(c.x + (cloudX[i] - 0.5f) * vw * 1.4f, c.y + (cloudY[i] - 0.5f) * vh * 1.4f, 0);
            float k = (1.2f + (i % 4) * 0.5f) * Mathf.Max(1f, camSize / 24f);
            clouds[i].transform.localScale = new Vector3(k * 1.5f, k, 1);
            clouds[i].color = new Color(1, 1, 1, w == Weather.Clouds ? 0.7f : 0.45f);
        }
    }

    void UpdateOverlays()
    {
        float day = world.Daylight;
        float nightA = Mathf.Clamp01(1f - day * 1.35f) * 0.62f;
        float warm = Mathf.Exp(-Mathf.Pow((day - 0.38f) / 0.2f, 2f)) * 0.22f;
        Color nc = new Color(0.04f, 0.07f, 0.22f), wc = new Color(0.95f, 0.5f, 0.2f);
        float sum = nightA + warm;
        Color tc = sum > 0.001f ? (nc * nightA + wc * warm) / sum : nc; tc.a = Mathf.Clamp01(sum);
        Color wx = Color.clear;
        switch (world.weather)
        {
            case Weather.Fog: wx = new Color(0.8f, 0.84f, 0.87f, 0.5f); break;
            case Weather.Rain: wx = new Color(0.2f, 0.25f, 0.3f, 0.28f); break;
            case Weather.Clouds: wx = new Color(0.25f, 0.28f, 0.33f, 0.16f); break;
            case Weather.Snow: wx = new Color(0.9f, 0.94f, 1f, 0.2f); break;
        }
        if (world.weather == Weather.Fog && day < 0.5f) wx = Color.Lerp(wx, new Color(0.25f, 0.28f, 0.33f, 0.5f), 1f - day * 2f);
        tintSR.color = tc; wxSR.color = Color.Lerp(wxSR.color, wx, 0.05f + (Application.isPlaying ? 0.1f : 1f));
        float sw = (camSize * 2f * cam.aspect + 12f) / 0.25f, sh = (camSize * 2f + 12f) / 0.25f;
        tintSR.transform.position = new Vector3(camPos.x, camPos.y, 0); wxSR.transform.position = new Vector3(camPos.x, camPos.y, 0);
        tintSR.transform.localScale = new Vector3(sw, sh, 1); wxSR.transform.localScale = new Vector3(sw, sh, 1);
    }

    // ------------------------------------------------------------------ frame update
    void Update()
    {
        fpsT += Time.unscaledDeltaTime; fpsN++;
        if (fpsT > 0.5f) { fps = fpsN / fpsT; fpsT = 0; fpsN = 0; }
        if (toastT > 0f) toastT -= Time.unscaledDeltaTime;
        if (screen != Screen_.Sandbox || world == null)
        {
            // menu backdrop: no simulation
            tintSR.color = Color.clear; wxSR.color = Color.clear;
            Sfx.camPos = Vector2.zero; Sfx.Update(Time.unscaledDeltaTime, true);
            return;
        }
        float real = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        CameraUpdate(real);
        float ds = paused || menuOpen ? 0f : real * simSpeed;
        if (ds > 0f)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(ds / 0.04f));
            float h = ds / steps;
            for (int s = 0; s < steps; s++) Step(h);
            Fx.Update(ds);
            UpdateWeather(ds);
        }
        else UpdateWeather(0f);
        Visuals(ds);
        UpdateOverlays();
        Atlas.Flush();
        Sfx.camPos = camPos; Sfx.camSize = camSize;
        Sfx.Update(real, paused || menuOpen);
        UpdateDroneCam();
        UpdateGhost();
    }

    void Step(float h)
    {
        world.Tick(h);
        world.RebuildGrid();
        var ents = world.ents;
        for (int i = 0; i < ents.Count; i++) { var e = ents[i]; if (!e.removed) e.Tick(h); }
        for (int i = activeProps.Count - 1; i >= 0; i--) { var p = activeProps[i]; if (p.removed || !p.active) { activeProps.RemoveAt(i); continue; } p.Tick(h); }
        Proj.UpdateAll(h);
        // cleanup
        if (ents.RemoveAll(e => e.removed) > 0) { }
        entCount = ents.Count;
    }

    void Visuals(float ds)
    {
        float hw = camSize * cam.aspect, hh = camSize;
        Fx.view = new Rect(camPos.x - hw, camPos.y - hh, hw * 2f, hh * 2f);
        Rect vr = new Rect(camPos.x - hw - 8f, camPos.y - hh - 8f, hw * 2f + 16f, hh * 2f + 16f);
        var ents = world.ents;
        for (int i = 0; i < ents.Count; i++)
        {
            var e = ents[i]; if (e.removed) continue;
            e.visible = vr.Contains(e.pos) || e.selected || e is Aircraft;
            if (e.hidden) continue;
            if (e.visible) e.Visual(ds);
        }
        // props: only burning or recently active need visuals
        for (int i = 0; i < activeProps.Count; i++) { var p = activeProps[i]; p.visible = vr.Contains(p.pos); p.Visual(ds); }
        // mid-flight props that are damaged but not active (smoke) - sample a few
        int pn = world.props.Count; if (pn > 0) { for (int k = 0; k < 40; k++) { var p = world.props[(propCursor++) % pn] as Prop; if (p != null && !p.removed && (p.pstate == 1 || p.dead && !p.IsTree) && !p.active) { p.visible = vr.Contains(p.pos); p.Visual(ds * (pn / 40f)); } } }
        // flares of world
        // selection ring for drone observation
        UpdateViewRing();
    }
    int propCursor;

    void UpdateViewRing()
    {
        Drone d = sel.Count == 1 ? sel[0] as Drone : null;
        if (d == null || d.removed || d.dead) { if (viewRing != null) viewRing.gameObject.SetActive(false); return; }
        if (viewRing == null) { var g = new GameObject("ViewRing"); viewRing = g.AddComponent<SpriteRenderer>(); viewRing.sprite = Art.Spr.ring; viewRing.sortingOrder = 61; viewRing.color = new Color(0.4f, 1f, 0.6f, 0.45f); }
        viewRing.gameObject.SetActive(true); viewRing.transform.position = new Vector3(d.pos.x, d.pos.y, 0); float s = d.viewR; viewRing.transform.localScale = new Vector3(s, s, 1);
    }

    void UpdateDroneCam()
    {
        Drone d = sel.Count == 1 && !uiHidden ? sel[0] as Drone : null;
        if (d != null && !d.removed && !d.dead && !d.noSignal)
        {
            droneCam.enabled = true; droneCam.transform.position = new Vector3(d.pos.x, d.pos.y, -50f); droneCam.orthographicSize = Mathf.Clamp(d.viewR * 0.4f, 8f, 28f);
        }
        else droneCam.enabled = false;
    }

    // --------------------------------------------------------------- camera
    void CameraUpdate(float dt)
    {
        Vector2 mv = Vector2.zero;
        if (held.Contains(KeyCode.W) || held.Contains(KeyCode.UpArrow)) mv.y += 1;
        if (held.Contains(KeyCode.S) || held.Contains(KeyCode.DownArrow)) mv.y -= 1;
        if (held.Contains(KeyCode.A) || held.Contains(KeyCode.LeftArrow)) mv.x -= 1;
        if (held.Contains(KeyCode.D) || held.Contains(KeyCode.RightArrow)) mv.x += 1;
        if (cfg.edgeScroll && !uiHidden && Application.isFocused)
        {
            Vector2 m = rawMouse; float sw = Screen.width, sh = Screen.height;
            if (m.x >= 0 && m.x < 6) mv.x -= 1; if (m.x > sw - 6 && m.x <= sw) mv.x += 1; if (m.y >= 0 && m.y < 6) mv.y += 1; if (m.y > sh - 6 && m.y <= sh) mv.y -= 1;
        }
        float spd = camSize * 1.9f * (held.Contains(KeyCode.LeftShift) ? 2.2f : 1f);
        camPos += mv.normalized * spd * dt;
        camSize = Mathf.Lerp(camSize, camTarget, 1f - Mathf.Exp(-dt * 12f));
        float hw = camSize * cam.aspect;
        camPos.x = Mathf.Clamp(camPos.x, -10f, world.w + 10f); camPos.y = Mathf.Clamp(camPos.y, -10f, world.h + 10f);
        shakeAmt = Mathf.Max(0f, shakeAmt - dt * 2.2f);
        Vector2 sh2 = shakeAmt > 0.01f ? UnityEngine.Random.insideUnitCircle * shakeAmt * 0.6f : Vector2.zero;
        cam.orthographicSize = camSize;
        cam.transform.position = new Vector3(camPos.x + sh2.x, camPos.y + sh2.y, -50f);
    }

    public Vector2 ScreenToWorld(Vector2 raw)
    {
        Vector3 p = cam.ScreenToWorldPoint(new Vector3(raw.x, UnityEngine.Screen.height - raw.y, 50f));
        return new Vector2(p.x, p.y);
    }
    public Vector2 WorldToGUI(Vector2 w)
    {
        Vector3 p = cam.WorldToScreenPoint(new Vector3(w.x, w.y, 0));
        return new Vector2(p.x, UnityEngine.Screen.height - p.y);
    }
    public void Zoom(float delta, Vector2 raw)
    {
        Vector2 before = ScreenToWorld(raw);
        camTarget = Mathf.Clamp(camTarget * (1f - delta * 0.12f), 6f, Mathf.Max(60f, world.h * 0.75f));
        // keep the point under the cursor fixed
        float old = cam.orthographicSize; cam.orthographicSize = camTarget; camSize = camTarget;
        Vector2 after = ScreenToWorld(raw);
        camPos += before - after;
    }

    // --------------------------------------------------------------- spawn ghost (preview)
    void UpdateGhost()
    {
        if (spawnDef == null || screen != Screen_.Sandbox || menuOpen) { if (ghost != null) ghost.gameObject.SetActive(false); return; }
        if (ghost == null) { var g = new GameObject("Ghost"); ghost = g.AddComponent<SpriteRenderer>(); ghost.sortingOrder = 80; }
        Sprite s = Art.Icon(spawnDef, Brig, faction);
        ghost.sprite = s;
        bool ok = ValidSpawn(spawnDef, mouseWorld);
        ghost.color = ok ? new Color(1, 1, 1, 0.7f) : new Color(1, 0.3f, 0.3f, 0.7f);
        ghost.gameObject.SetActive(!UIOver);
        ghost.transform.position = new Vector3(mouseWorld.x, mouseWorld.y, 0);
        float rot = facingManual ? spawnFacing : (faction == Faction.Ukraine ? 0f : 180f);
        ghost.transform.rotation = Quaternion.Euler(0, 0, spawnDef.fam == "soldier" || spawnDef.fam == "prop" ? 0f : rot);
        float k = spawnDef.fam == "special" ? 1.2f : 1f; ghost.transform.localScale = Vector3.one * k;
    }
    public bool UIOver;

    // --------------------------------------------------------------- input (called from OnGUI)
    void OnApplicationFocus(bool f) { if (!f) held.Clear(); }

    void OnGUI()
    {
        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None) held.Add(e.keyCode);
        if (e.type == EventType.KeyUp) held.Remove(e.keyCode);
        if (e.type != EventType.Layout) rawMouse = e.mousePosition;
        UI.Draw(this);
        if (screen != Screen_.Sandbox || world == null) return;
        mouseWorld = ScreenToWorld(rawMouse);
        HandleInput(e);
    }

    void HandleInput(Event e)
    {
        if (e.type == EventType.Used) return;
        bool over = UI.MouseOverUI(rawMouse);
        UIOver = over;
        bool shift = e.shift, ctrl = e.control || e.command;
        switch (e.type)
        {
            case EventType.ScrollWheel:
                if (!over && !menuOpen) { Zoom(-e.delta.y * 0.35f, rawMouse); e.Use(); }
                break;
            case EventType.MouseDown:
                if (over || menuOpen) break;
                if (e.button == 2) { mmbPan = true; mmbLast = rawMouse; e.Use(); break; }
                if (e.button == 1) { RightClick(); e.Use(); break; }
                if (e.button == 0) { LeftDown(shift, ctrl); e.Use(); }
                break;
            case EventType.MouseDrag:
                if (mmbPan) { Vector2 d = rawMouse - mmbLast; float k = camSize * 2f / UnityEngine.Screen.height; camPos += new Vector2(-d.x * k, d.y * k); mmbLast = rawMouse; e.Use(); }
                else if (dragging) { DragUpdate(); e.Use(); }
                break;
            case EventType.MouseUp:
                if (e.button == 2) { mmbPan = false; }
                if (e.button == 0) { LeftUp(shift); }
                break;
            case EventType.KeyDown:
                HandleKey(e, shift, ctrl);
                break;
        }
    }

    void LeftDown(bool shift, bool ctrl)
    {
        if (spawnDef != null)
        {
            SpawnAt(spawnDef, mouseWorld, spawnCount);
            if (!shift) { /* keep selection active so the player can keep placing */ }
            return;
        }
        if (tool == Tool.Delete)
        {
            Ent t = world.Pick(mouseWorld, true);
            if (t != null) { if (sel.Contains(t)) sel.Remove(t); t.Destroy(); Sfx.Ui(); }
            return;
        }
        if (tool == Tool.Drag)
        {
            Ent t = world.Pick(mouseWorld, true);
            if (t != null)
            {
                dragging = true; dragEnt = t; dragSet.Clear(); dragOffsets.Clear();
                if (sel.Contains(t) && sel.Count > 1) foreach (var s in sel) { dragSet.Add(s); dragOffsets.Add(s.pos - t.pos); }
                else { dragSet.Add(t); dragOffsets.Add(Vector2.zero); }
            }
            return;
        }
        // select tool
        Ent hit = world.Pick(mouseWorld, true);
        if (hit != null && !(hit is Unit) && hit.kind == Kind.Prop && hit.def.special == "tree" && false) hit = null;
        if (hit != null && (hit.kind != Kind.Prop || !hit.dead || true))
        {
            if (hit == lastClick && Time.unscaledTime - lastClickT < 0.35f && hit is Unit)
            {
                // double click: select all of the same type on screen
                ClearSel();
                foreach (var o in world.ents) if (!o.removed && !o.dead && !o.hidden && o.def == hit.def && o.fac == hit.fac && Fx.InView(o.pos, 0)) { sel.Add(o); o.SetSelected(true); }
            }
            else Select(hit, shift);
            lastClick = hit; lastClickT = Time.unscaledTime;
            return;
        }
        boxing = true; boxStart = mouseWorld; boxShift = shift;
        if (!shift) ClearSel();
    }
    bool boxShift;
    public bool IsBoxing { get { return boxing; } }
    public Vector2 BoxA { get { return boxStart; } }

    void DragUpdate()
    {
        if (dragEnt == null) return;
        for (int i = 0; i < dragSet.Count; i++)
        {
            var e = dragSet[i]; if (e.removed) continue;
            Vector2 np = mouseWorld + dragOffsets[i];
            np = world.Clamp(np, 1f);
            e.SetPos(np); e.vel = Vector2.zero;
            if (e is Unit) { var u = (Unit)e; u.path = null; u.speed = 0; u.home = np; if (!(e is Aircraft)) { if (u.ord == OrderT.Move || u.ord == OrderT.Return) u.ord = OrderT.None; } if (e is Aircraft) { ((Aircraft)e).SetPos(np); } }
        }
    }

    void LeftUp(bool shift)
    {
        if (dragging)
        {
            dragging = false;
            // drop validation: ground units may not end up in water
            foreach (var e in dragSet)
            {
                if (e.removed) continue;
                if (!e.IsAirKind && !ValidSpawn(e.def, e.pos)) { Vector2 n = world.NearestOfType(e.pos, t => e.kind == Kind.Ship ? World.IsWater(t) : !World.IsWater(t), 40f); e.SetPos(n); }
                if (e is Unit) ((Unit)e).home = e.pos;
                if (e is Aircraft) ((Aircraft)e).SetPos(e.pos);
            }
            dragSet.Clear(); dragEnt = null;
            return;
        }
        if (boxing)
        {
            boxing = false;
            if ((mouseWorld - boxStart).magnitude > camSize * 0.02f) BoxSelect(boxStart, mouseWorld, boxShift);
        }
    }

    void RightClick()
    {
        if (spawnDef != null) { spawnDef = null; Sfx.Ui(); return; }
        if (tool != Tool.Select) { tool = Tool.Select; return; }
        if (sel.Count == 0) return;
        Ent t = world.Pick(mouseWorld, false);
        CmdMove(mouseWorld, t);
        Fx.Spawn(Art.Spr.ring, mouseWorld, Vector2.zero, 0.5f, 0.3f, 1.6f, new Color(0.4f, 1f, 0.5f, 0.9f), new Color(0.4f, 1f, 0.5f, 0f), 0, 0, 62, false);
    }

    void HandleKey(Event e, bool shift, bool ctrl)
    {
        KeyCode k = e.keyCode;
        if (k >= KeyCode.Alpha1 && k <= KeyCode.Alpha9 && screen == Screen_.Sandbox)
        {
            int g = k - KeyCode.Alpha0;
            if (ctrl) AssignGroup(g); else RecallGroup(g);
            e.Use(); return;
        }
        switch (k)
        {
            case KeyCode.Escape:
                if (spawnDef != null) spawnDef = null; else if (followMode) followMode = false; else if (tool != Tool.Select) tool = Tool.Select; else if (sel.Count > 0) ClearSel(); else menuOpen = !menuOpen;
                e.Use(); break;
            case KeyCode.Space: paused = !paused; Sfx.Ui(); e.Use(); break;
            case KeyCode.LeftBracket: case KeyCode.Minus: simSpeed = Mathf.Max(0.25f, simSpeed * 0.5f); e.Use(); break;
            case KeyCode.RightBracket: case KeyCode.Equals: simSpeed = Mathf.Min(8f, simSpeed * 2f); e.Use(); break;
            case KeyCode.Delete: case KeyCode.Backspace: CmdDelete(); e.Use(); break;
            case KeyCode.H: uiHidden = !uiHidden; e.Use(); break;
            case KeyCode.Q: facingManual = true; spawnFacing -= 45f; Toast("Facing " + Mathf.RoundToInt(Mathf.Repeat(spawnFacing, 360f)) + "°"); e.Use(); break;
            case KeyCode.E: facingManual = true; spawnFacing += 45f; Toast("Facing " + Mathf.RoundToInt(Mathf.Repeat(spawnFacing, 360f)) + "°"); e.Use(); break;
            case KeyCode.F: FocusSel(); e.Use(); break;
            case KeyCode.X: CmdStop(); e.Use(); break;
            case KeyCode.R: CmdReturn(); e.Use(); break;
            case KeyCode.G: CmdFollow(); e.Use(); break;
            case KeyCode.U: CmdUnload(); e.Use(); break;
            case KeyCode.L: CmdLoadNearby(); e.Use(); break;
            case KeyCode.Z: CmdFlares(); e.Use(); break;
            case KeyCode.T: CmdTakeoff(); e.Use(); break;
            case KeyCode.N: CmdLand(); e.Use(); break;
            case KeyCode.PageUp: CmdLayer(1); e.Use(); break;
            case KeyCode.PageDown: CmdLayer(-1); e.Use(); break;
            case KeyCode.B: CmdBridge(); e.Use(); break;
            case KeyCode.V: tool = Tool.Select; e.Use(); break;
            case KeyCode.C: if (ctrl) { SelectAll(faction); e.Use(); } break;
            case KeyCode.M: cfg.minimap = !cfg.minimap; e.Use(); break;
            case KeyCode.F5: SaveSandbox(saveSlot); e.Use(); break;
            case KeyCode.F9: LoadSandbox(saveSlot); e.Use(); break;
        }
    }
}
