using System;
using System.Collections.Generic;
using UnityEngine;

public enum Weather { Sunny, Clouds, Rain, Snow, Fog }

public class PropSpawn { public string id; public float x, y, rot; public int v; public Faction fac; }

public class World
{
    public static World I;
    public const byte FIELD = 0, FOREST = 1, ROAD = 2, URBAN = 3, VILLAGE = 4, LAKE = 5, RIVER = 6, BRIDGE = 7, INDUSTRIAL = 8, BASE = 9, PORT = 10, AIRFIELD = 11, BEACH = 12, SEA = 13, RUNWAY = 14;
    public static readonly string[] TerNames = { "Fields", "Forest", "Road", "Urban area", "Village", "Lake", "River", "Bridge", "Industrial area", "Military base", "Port", "Airfield", "Beach", "Sea", "Runway" };
    public static readonly string[] SizeNames = { "Small", "Medium", "Large", "Huge" };
    public static readonly string[] PresetNames = { "Steppe & River", "Urban Front", "Coastal Port", "Winter Forest" };
    static readonly int[] SizeW = { 240, 400, 640, 960 };
    static readonly int[] SizeH = { 150, 250, 400, 600 };
    static readonly int[] SizePPC = { 4, 4, 3, 2 };

    public int w, h, ppc, sizeIdx, preset, seed;
    public byte[] ter, fld;
    public System.Random rng;
    public List<PropSpawn> pending = new List<PropSpawn>();
    public List<Vector2> airfields = new List<Vector2>();
    public List<float> airfieldAng = new List<float>();
    public List<Vector2> ports = new List<Vector2>();
    public List<Vector2> baseSites = new List<Vector2>();
    public bool winter;

    // time & weather
    public float tod = 11f;          // time of day in hours
    public bool autoCycle = false;
    public float dayLength = 300f;   // seconds per 24h
    public Weather weather = Weather.Sunny;
    public float simTime;
    public float wetness;            // 0..1 mud
    public float Daylight { get { return Mathf.Clamp01(Mathf.Cos((tod - 13f) / 12f * Mathf.PI) * 1.6f + 0.35f); } }
    public float Visibility
    {
        get
        {
            float v = Mathf.Lerp(0.55f, 1f, Daylight);
            if (weather == Weather.Fog) v *= 0.45f; else if (weather == Weather.Rain) v *= 0.75f; else if (weather == Weather.Snow) v *= 0.7f; else if (weather == Weather.Clouds) v *= 0.92f;
            return v;
        }
    }

    // textures
    GameObject root;
    SpriteRenderer terSR, decSR;
    Texture2D terTex, decTex;
    Color32[] decPix;
    public bool decDirty;
    float decTimer;
    Texture2D terrainTexRef;

    // navigation
    public const int NC = 4;
    public int nw, nh;
    byte[] nTer, nWater, nBridge, nRoad, nSolid;
    int[] aStamp, aParent; float[] aG; int stampId;

    // spatial grids
    const float CS = 12f;
    int gw, gh;
    List<Ent>[] cells; List<int> usedCells = new List<int>();
    List<Ent>[] pcells;
    public List<Ent> ents = new List<Ent>();
    public List<Ent> props = new List<Ent>();
    public List<Flare> flares = new List<Flare>();
    static List<Ent> tmpQ = new List<Ent>();

    public World() { I = this; }
    public Texture2D TerTex { get { return terTex; } }
    public Vector2 WindVec()
    {
        float k = weather == Weather.Rain ? 1.6f : (weather == Weather.Snow ? 1.0f : (weather == Weather.Fog ? 0.15f : 0.45f));
        return new Vector2(1f + 0.5f * Mathf.Sin(simTime * 0.07f), 0.2f * Mathf.Sin(simTime * 0.11f)) * k;
    }

    // ---------------------------------------------------------------- queries
    public byte At(Vector2 p)
    {
        int x = (int)p.x, y = (int)p.y;
        if (x < 0 || y < 0 || x >= w || y >= h) return SEA;
        return ter[y * w + x];
    }
    public byte AtCell(int x, int y) { if (x < 0 || y < 0 || x >= w || y >= h) return SEA; return ter[y * w + x]; }
    public static bool IsWater(byte t) { return t == LAKE || t == RIVER || t == SEA; }
    public bool Water(Vector2 p) { return IsWater(At(p)); }
    public bool InBounds(Vector2 p) { return p.x >= 1 && p.y >= 1 && p.x <= w - 1 && p.y <= h - 1; }
    public Vector2 Clamp(Vector2 p, float m = 1f) { return new Vector2(Mathf.Clamp(p.x, m, w - m), Mathf.Clamp(p.y, m, h - m)); }

    public float SpeedMul(Move m, byte t)
    {
        float s = 0f;
        switch (m)
        {
            case Move.Foot:
                switch (t) { case FIELD: s = 1f; break; case FOREST: s = 0.75f; break; case ROAD: s = 1.15f; break; case BRIDGE: s = 1.1f; break; case BEACH: s = 0.85f; break; case RUNWAY: s = 1.1f; break; case LAKE: case RIVER: case SEA: s = 0f; break; default: s = 1f; break; }
                break;
            case Move.Wheel:
                switch (t) { case FIELD: s = 0.7f; break; case FOREST: s = 0.35f; break; case ROAD: s = 1.2f; break; case URBAN: s = 0.9f; break; case VILLAGE: s = 0.8f; break; case BRIDGE: s = 1.1f; break; case BEACH: s = 0.5f; break; case RUNWAY: s = 1.2f; break; case LAKE: case RIVER: case SEA: s = 0f; break; default: s = 0.95f; break; }
                break;
            case Move.Track:
                switch (t) { case FIELD: s = 0.95f; break; case FOREST: s = 0.6f; break; case ROAD: s = 1.1f; break; case URBAN: s = 0.9f; break; case VILLAGE: s = 0.9f; break; case BEACH: s = 0.75f; break; case RUNWAY: s = 1.1f; break; case LAKE: case RIVER: case SEA: s = 0f; break; default: s = 1f; break; }
                break;
            case Move.Naval: s = (t == LAKE || t == SEA) ? 1f : (t == RIVER ? 0.8f : 0f); return s;
            case Move.Air: return 1f;
            default: return 0f;
        }
        if (s > 0f && (t == FIELD || t == FOREST || t == VILLAGE || t == BEACH))
        {
            float mud = wetness;
            if (weather == Weather.Snow || winter) mud = Mathf.Max(mud, 0.7f);
            float k = m == Move.Wheel ? 0.4f : (m == Move.Track ? 0.18f : 0.2f);
            s *= 1f - mud * k;
        }
        return s;
    }
    public float SpeedMul(Move m, Vector2 p) { return SpeedMul(m, At(p)); }

    // ---------------------------------------------------------------- spatial grid
    void InitGrids()
    {
        gw = Mathf.CeilToInt(w / CS) + 1; gh = Mathf.CeilToInt(h / CS) + 1;
        cells = new List<Ent>[gw * gh]; pcells = new List<Ent>[gw * gh];
        for (int i = 0; i < cells.Length; i++) { cells[i] = new List<Ent>(4); pcells[i] = new List<Ent>(2); }
        usedCells.Clear();
    }
    int CellOf(Vector2 p) { int x = Mathf.Clamp((int)(p.x / CS), 0, gw - 1), y = Mathf.Clamp((int)(p.y / CS), 0, gh - 1); return y * gw + x; }
    public void RebuildGrid()
    {
        for (int i = 0; i < usedCells.Count; i++) cells[usedCells[i]].Clear();
        usedCells.Clear();
        for (int i = 0; i < ents.Count; i++)
        {
            Ent e = ents[i];
            if (e.removed || e.hidden) continue;
            int c = CellOf(e.pos);
            if (cells[c].Count == 0) usedCells.Add(c);
            cells[c].Add(e);
        }
    }
    public void AddProp(Ent e) { props.Add(e); pcells[CellOf(e.pos)].Add(e); if (e.def.solid) ModSolid(e.pos, 1); }
    public void RemoveProp(Ent e) { props.Remove(e); pcells[CellOf(e.pos)].Remove(e); }
    public void PropFell(Ent e) { if (e.def.solid) ModSolid(e.pos, -1); }

    // gather mobile entities within radius (centre distance <= r + e.radius)
    public void Query(Vector2 p, float r, List<Ent> res, bool withProps = false)
    {
        int x0 = Mathf.Clamp((int)((p.x - r) / CS), 0, gw - 1), x1 = Mathf.Clamp((int)((p.x + r) / CS), 0, gw - 1);
        int y0 = Mathf.Clamp((int)((p.y - r) / CS), 0, gh - 1), y1 = Mathf.Clamp((int)((p.y + r) / CS), 0, gh - 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var l = cells[y * gw + x];
                for (int i = 0; i < l.Count; i++) { Ent e = l[i]; if (!e.removed && (e.pos - p).sqrMagnitude <= (r + e.radius) * (r + e.radius)) res.Add(e); }
                if (withProps)
                {
                    var pl = pcells[y * gw + x];
                    for (int i = 0; i < pl.Count; i++) { Ent e = pl[i]; if (!e.removed && (e.pos - p).sqrMagnitude <= (r + e.radius) * (r + e.radius)) res.Add(e); }
                }
            }
    }
    public void QueryProps(Vector2 p, float r, List<Ent> res)
    {
        int x0 = Mathf.Clamp((int)((p.x - r) / CS), 0, gw - 1), x1 = Mathf.Clamp((int)((p.x + r) / CS), 0, gw - 1);
        int y0 = Mathf.Clamp((int)((p.y - r) / CS), 0, gh - 1), y1 = Mathf.Clamp((int)((p.y + r) / CS), 0, gh - 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var pl = pcells[y * gw + x];
                for (int i = 0; i < pl.Count; i++) { Ent e = pl[i]; if (!e.removed && (e.pos - p).sqrMagnitude <= (r + e.radius) * (r + e.radius)) res.Add(e); }
            }
    }
    public Ent Pick(Vector2 p, bool includeProps = true)
    {
        // topmost: air units first, then soldiers, vehicles, props
        Ent best = null; float bestScore = 1e9f;
        tmpQ.Clear(); Query(p, 1.5f, tmpQ, includeProps);
        for (int i = 0; i < tmpQ.Count; i++)
        {
            Ent e = tmpQ[i];
            if (e.hidden) continue;
            float d = (e.pos - p).magnitude - e.radius;
            float pri = e.kind == Kind.Plane || e.kind == Kind.Heli || e.kind == Kind.Drone ? -3f : (e.kind == Kind.Soldier ? -2f : (e.kind == Kind.Prop ? 0f : -1f));
            if (d > 0.6f) continue;
            float sc = d + pri * 0.6f;
            if (e.kind == Kind.Prop && e.def.special == "tree") sc += 1.5f;
            if (sc < bestScore) { bestScore = sc; best = e; }
        }
        return best;
    }

    // ---------------------------------------------------------------- navigation
    void ModSolid(Vector2 p, int d)
    {
        int nx = Mathf.Clamp((int)(p.x / NC), 0, nw - 1), ny = Mathf.Clamp((int)(p.y / NC), 0, nh - 1);
        int v = nSolid[ny * nw + nx] + d; nSolid[ny * nw + nx] = (byte)Mathf.Clamp(v, 0, 250);
    }
    void BuildNav()
    {
        nw = Mathf.CeilToInt(w / (float)NC); nh = Mathf.CeilToInt(h / (float)NC);
        int n = nw * nh;
        nTer = new byte[n]; nWater = new byte[n]; nBridge = new byte[n]; nRoad = new byte[n]; nSolid = new byte[n];
        aStamp = new int[n]; aParent = new int[n]; aG = new float[n]; stampId = 0;
        for (int ny = 0; ny < nh; ny++) for (int nx = 0; nx < nw; nx++) RecalcNav(nx, ny);
    }
    void RecalcNav(int nx, int ny)
    {
        int wc = 0, bc = 0, rc = 0; int[] cnt = new int[16];
        for (int y = 0; y < NC; y++) for (int x = 0; x < NC; x++)
            {
                byte t = AtCell(nx * NC + x, ny * NC + y);
                cnt[t]++;
                if (IsWater(t)) wc++; if (t == BRIDGE) bc++; if (t == ROAD || t == BRIDGE || t == RUNWAY) rc++;
            }
        int best = 0, bi = 0; for (int i = 0; i < 16; i++) if (i != LAKE && i != RIVER && i != SEA && cnt[i] > best) { best = cnt[i]; bi = i; }
        int idx = ny * nw + nx;
        nTer[idx] = (byte)bi; nWater[idx] = (byte)wc; nBridge[idx] = (byte)bc; nRoad[idx] = (byte)rc;
    }
    float NavCost(Move m, int idx)
    {
        if (m == Move.Naval) return nWater[idx] >= 12 ? 1f : -1f;
        if (m == Move.Air) return 1f;
        bool bridge = nBridge[idx] >= 3;
        if (nWater[idx] >= 9 && !bridge) return -1f;
        if (nWater[idx] >= 4 && !bridge && m != Move.Foot) return -1f;
        if (m != Move.Foot && nSolid[idx] > 0 && nRoad[idx] < 6) return -1f;
        float sm = nRoad[idx] >= 6 ? SpeedMul(m, ROAD) : SpeedMul(m, nTer[idx]);
        if (bridge) sm = Mathf.Max(sm, 1f);
        if (sm <= 0.05f) return -1f;
        float c = 1f / sm;
        if (m == Move.Foot && nSolid[idx] > 0) c *= 1.3f;
        return c;
    }
    public bool NavPassable(Move m, Vector2 p)
    {
        int nx = Mathf.Clamp((int)(p.x / NC), 0, nw - 1), ny = Mathf.Clamp((int)(p.y / NC), 0, nh - 1);
        return NavCost(m, ny * nw + nx) > 0f;
    }

    // simple binary heap
    struct HN { public int i; public float f; }
    List<HN> heap = new List<HN>();
    void HPush(int i, float f)
    {
        heap.Add(new HN { i = i, f = f }); int c = heap.Count - 1;
        while (c > 0) { int p = (c - 1) / 2; if (heap[p].f <= heap[c].f) break; var t = heap[p]; heap[p] = heap[c]; heap[c] = t; c = p; }
    }
    HN HPop()
    {
        var top = heap[0]; int last = heap.Count - 1; heap[0] = heap[last]; heap.RemoveAt(last); int c = 0;
        while (true) { int l = c * 2 + 1, r = l + 1, m = c; if (l < heap.Count && heap[l].f < heap[m].f) m = l; if (r < heap.Count && heap[r].f < heap[m].f) m = r; if (m == c) break; var t = heap[m]; heap[m] = heap[c]; heap[c] = t; c = m; }
        return top;
    }

    // A* path in world coordinates. Returns null when no path was found.
    public List<Vector2> FindPath(Move m, Vector2 a, Vector2 b, int maxNodes = 30000)
    {
        int sx = Mathf.Clamp((int)(a.x / NC), 0, nw - 1), sy = Mathf.Clamp((int)(a.y / NC), 0, nh - 1);
        int gx = Mathf.Clamp((int)(b.x / NC), 0, nw - 1), gy = Mathf.Clamp((int)(b.y / NC), 0, nh - 1);
        int start = sy * nw + sx, goal = gy * nw + gx;
        if (start == goal) return new List<Vector2> { b };
        // if goal is blocked search for nearest passable cell around it
        if (NavCost(m, goal) < 0f)
        {
            bool found = false;
            for (int r = 1; r < 8 && !found; r++)
                for (int dy = -r; dy <= r && !found; dy++)
                    for (int dx = -r; dx <= r && !found; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        int x = gx + dx, y = gy + dy; if (x < 0 || y < 0 || x >= nw || y >= nh) continue;
                        if (NavCost(m, y * nw + x) > 0f) { goal = y * nw + x; gx = x; gy = y; found = true; }
                    }
            if (!found) return null;
        }
        stampId++;
        heap.Clear();
        aG[start] = 0; aStamp[start] = stampId; aParent[start] = -1;
        HPush(start, 0);
        int[] closed = null; // closed state tracked via negative stamp trick: use aStamp = stampId and parent set; we use aG compare
        int expanded = 0; bool ok = false;
        while (heap.Count > 0 && expanded < maxNodes)
        {
            var cur = HPop(); int ci = cur.i;
            int cx = ci % nw, cy = ci / nw;
            float hc = Heur(cx, cy, gx, gy);
            if (cur.f - aG[ci] - hc > 0.001f && cur.f - hc > aG[ci] + 0.001f) continue; // stale entry
            expanded++;
            if (ci == goal) { ok = true; break; }
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= nw || ny >= nh) continue;
                    int ni = ny * nw + nx;
                    float c = NavCost(m, ni);
                    if (c < 0f) continue;
                    if (dx != 0 && dy != 0) { if (NavCost(m, cy * nw + nx) < 0f || NavCost(m, ny * nw + cx) < 0f) continue; }
                    float step = (dx != 0 && dy != 0 ? 1.414f : 1f) * c;
                    float ng = aG[ci] + step;
                    if (aStamp[ni] != stampId || ng < aG[ni] - 0.0001f)
                    {
                        aStamp[ni] = stampId; aG[ni] = ng; aParent[ni] = ci;
                        HPush(ni, ng + Heur(nx, ny, gx, gy));
                    }
                }
        }
        if (!ok) return null;
        var path = new List<Vector2>();
        int k = goal;
        while (k != -1 && k != start) { path.Add(new Vector2((k % nw + 0.5f) * NC, (k / nw + 0.5f) * NC)); k = aParent[k]; }
        path.Reverse();
        if (path.Count > 0) path[path.Count - 1] = (NavPassable(m, b) ? b : path[path.Count - 1]);
        // string pulling
        var res = new List<Vector2>();
        Vector2 from = a; int i = 0;
        while (i < path.Count)
        {
            int j = path.Count - 1;
            while (j > i && !LineClear(m, from, path[j])) j--;
            res.Add(path[j]); from = path[j]; i = j + 1;
        }
        return res;
    }
    float Heur(int x, int y, int gx, int gy) { float dx = Mathf.Abs(x - gx), dy = Mathf.Abs(y - gy); return (dx + dy) + (1.414f - 2f) * Mathf.Min(dx, dy); }
    public bool LineClear(Move m, Vector2 a, Vector2 b)
    {
        float d = (b - a).magnitude; int n = Mathf.CeilToInt(d / (NC * 0.6f));
        for (int i = 1; i < n; i++) { Vector2 p = Vector2.Lerp(a, b, i / (float)n); if (!NavPassable(m, p)) return false; }
        return true;
    }

    public Vector2 NearestOfType(Vector2 p, Func<byte, bool> pred, float maxR = 60f)
    {
        for (float r = 2f; r <= maxR; r += 2f)
            for (int i = 0; i < 16; i++)
            {
                Vector2 q = p + G.Dir(i * 22.5f) * r;
                if (InBounds(q) && pred(At(q))) return q;
            }
        return p;
    }

    // ---------------------------------------------------------------- generation
    public void Generate(int sizeIdx, int preset, int seed)
    {
        this.sizeIdx = sizeIdx; this.preset = preset; this.seed = seed;
        w = SizeW[sizeIdx]; h = SizeH[sizeIdx]; ppc = SizePPC[sizeIdx];
        rng = new System.Random(seed);
        winter = preset == 3;
        ter = new byte[w * h]; fld = new byte[w * h];
        pending.Clear(); airfields.Clear(); airfieldAng.Clear(); ports.Clear(); baseSites.Clear();
        float ox = (float)rng.NextDouble() * 500f, oy = (float)rng.NextDouble() * 500f;
        float seaFrac = preset == 2 ? 0.30f : (preset == 1 ? 0.12f : 0.16f);
        float[] coast = new float[w];
        for (int x = 0; x < w; x++) coast[x] = h * seaFrac + (Mathf.PerlinNoise(x * 0.018f + ox, 3.3f) - 0.5f) * h * 0.14f + (Mathf.PerlinNoise(x * 0.07f + oy, 1.1f) - 0.5f) * 5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte t = FIELD;
                if (y < coast[x]) t = SEA;
                else if (y < coast[x] + 3.5f) t = BEACH;
                ter[y * w + x] = t;
            }
        // lakes
        int lakes = preset == 3 ? 4 : (preset == 1 ? 1 : 3);
        for (int i = 0; i < lakes; i++)
        {
            float lx = Rn(0.1f, 0.9f) * w, ly = Rn(0.35f, 0.9f) * h, lr = Rn(8f, 18f) * (1f + sizeIdx * 0.25f);
            for (int y = (int)(ly - lr * 1.6f); y <= ly + lr * 1.6f; y++)
                for (int x = (int)(lx - lr * 1.6f); x <= lx + lr * 1.6f; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float ang = Mathf.Atan2(y - ly, x - lx);
                    float rr = lr * (1f + 0.35f * Mathf.Sin(ang * 3f + i) + 0.2f * Mathf.Sin(ang * 5f + i * 2f));
                    float dd = Mathf.Sqrt((x - lx) * (x - lx) + (y - ly) * (y - ly));
                    if (dd < rr && ter[y * w + x] != SEA) ter[y * w + x] = LAKE;
                    else if (dd < rr + 2f && ter[y * w + x] == FIELD) ter[y * w + x] = BEACH;
                }
        }
        // river from the top to the sea
        int rivers = sizeIdx >= 2 ? 2 : 1;
        for (int r = 0; r < rivers; r++)
        {
            float rx = Rn(0.25f, 0.75f) * w, ry = h - 1; float freq = Rn(0.02f, 0.05f), amp = Rn(10f, 24f), ph = Rn(0, 6f);
            float baseX = rx;
            for (int step = 0; step < h * 2 && ry > 0; step++)
            {
                ry -= 0.5f; rx = baseX + Mathf.Sin(ry * freq + ph) * amp + Mathf.Sin(ry * freq * 2.7f + ph * 2f) * amp * 0.3f;
                int cx = (int)rx, cy = (int)ry; if (cy < 0 || cx < 0 || cx >= w) break;
                if (ter[Mathf.Clamp(cy, 0, h - 1) * w + Mathf.Clamp(cx, 0, w - 1)] == SEA) break;
                float rw2 = 2.4f + (r == 0 ? 0.6f : 0f);
                for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++)
                    {
                        int x = cx + dx, y = cy + dy; if (x < 0 || y < 0 || x >= w || y >= h) continue;
                        if (dx * dx + dy * dy <= rw2 * rw2 && ter[y * w + x] != SEA) ter[y * w + x] = RIVER;
                        else if (dx * dx + dy * dy <= (rw2 + 1.5f) * (rw2 + 1.5f) && ter[y * w + x] == FIELD) ter[y * w + x] = BEACH;
                    }
            }
        }
        // forests
        float ft = preset == 3 ? 0.50f : (preset == 1 ? 0.64f : 0.58f);
        float fx = (float)rng.NextDouble() * 300f, fy = (float)rng.NextDouble() * 300f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (ter[y * w + x] != FIELD) continue;
                float n = Mathf.PerlinNoise(x * 0.03f + fx, y * 0.03f + fy) * 0.75f + Mathf.PerlinNoise(x * 0.11f + fy, y * 0.11f + fx) * 0.25f;
                if (n > ft) ter[y * w + x] = FOREST;
                // field patterns
                float fn = Mathf.PerlinNoise(x * 0.02f + fy, y * 0.02f + 77f);
                fld[y * w + x] = (byte)(fn > 0.62f ? 1 + (int)(fn * 20f) % 3 : 0);
            }
        // sites
        var sites = new List<Vector3>();   // x,y,radius
        int cities = preset == 1 ? 3 : (sizeIdx == 0 ? 1 : 2);
        int villages = 3 + sizeIdx * 3;
        PlacePort(sites);
        PlaceAirfield(sites);
        PlaceBase(sites, Faction.Ukraine); PlaceBase(sites, Faction.Russia);
        PlaceIndustrial(sites);
        if (preset == 1 || sizeIdx >= 2) PlaceIndustrial(sites);
        for (int i = 0; i < cities; i++) PlaceCity(sites, preset == 1 ? Rn(24f, 34f) : Rn(18f, 26f));
        for (int i = 0; i < villages; i++) PlaceVillage(sites);
        // connect sites by roads
        sites.Sort((a, b) => a.x.CompareTo(b.x));
        BuildNav();
        for (int i = 0; i + 1 < sites.Count; i++) Road(new Vector2(sites[i].x, sites[i].y), new Vector2(sites[i + 1].x, sites[i + 1].y), 3);
        for (int i = 0; i + 2 < sites.Count; i += 2) Road(new Vector2(sites[i].x, sites[i].y), new Vector2(sites[i + 2].x, sites[i + 2].y), 2);
        // trees
        PlaceTrees();
        BuildNav();
        InitGrids();
        BuildTextures();
    }

    float Rn(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    bool SiteFree(List<Vector3> sites, float x, float y, float r)
    {
        if (x - r < 8 || y - r < 8 || x + r > w - 8 || y + r > h - 8) return false;
        foreach (var s in sites) if ((new Vector2(s.x, s.y) - new Vector2(x, y)).magnitude < s.z + r + 8f) return false;
        for (int i = 0; i < 16; i++) { var q = new Vector2(x, y) + G.Dir(i * 22.5f) * r; byte t = At(q); if (IsWater(t) || t == BEACH) return false; }
        byte c = At(new Vector2(x, y)); if (IsWater(c) || c == BEACH) return false;
        return true;
    }
    bool FindSite(List<Vector3> sites, float r, out Vector2 pos)
    {
        for (int i = 0; i < 300; i++)
        {
            float x = Rn(r + 8, w - r - 8), y = Rn(r + 8, h - r - 8);
            if (SiteFree(sites, x, y, r)) { pos = new Vector2(x, y); return true; }
        }
        pos = Vector2.zero; return false;
    }
    void Fill(float x0, float y0, float x1, float y1, byte t, bool onlyLand = true)
    {
        for (int y = Mathf.Max(0, (int)y0); y <= Mathf.Min(h - 1, (int)y1); y++)
            for (int x = Mathf.Max(0, (int)x0); x <= Mathf.Min(w - 1, (int)x1); x++)
            { if (onlyLand && IsWater(ter[y * w + x])) continue; ter[y * w + x] = t; }
    }
    void P(string id, float x, float y, float rot = 0, int v = -1, Faction f = Faction.Neutral)
    {
        pending.Add(new PropSpawn { id = id, x = x, y = y, rot = rot, v = v < 0 ? rng.Next(0, 10) : v, fac = f });
    }

    void PlacePort(List<Vector3> sites)
    {
        // find coast segment
        for (int tries = 0; tries < 200; tries++)
        {
            int x = rng.Next(40, w - 40);
            int y = 0; while (y < h && IsWater(ter[y * w + x])) y++;
            if (y <= 4 || y > h * 0.5f) continue;
            var c = new Vector3(x, y + 9, 24f);
            bool ok = true; foreach (var s in sites) if ((new Vector2(s.x, s.y) - new Vector2(c.x, c.y)).magnitude < s.z + 40) ok = false;
            if (!ok) continue;
            // apron and piers
            Fill(x - 28, y - 2, x + 28, y + 16, PORT, false);
            for (int px = -2; px <= 2; px++) { }
            for (int pier = 0; pier < 3; pier++) { float px = x - 18 + pier * 18; Fill(px - 2.5f, y - 16, px + 2.5f, y, PORT, false); }
            // make sure apron is land-like at the top
            for (int yy = y + 17; yy < y + 22 && yy < h; yy++) for (int xx = x - 28; xx <= x + 28; xx++) if (IsWater(ter[yy * w + xx])) ter[yy * w + xx] = BEACH;
            sites.Add(c); ports.Add(new Vector2(x, y - 8));
            P("pr_crane", x - 8, y + 4, 0); P("pr_crane", x + 10, y + 4, 0);
            P("pr_warehouse", x - 20, y + 11, 0); P("pr_warehouse", x + 20, y + 11, 0); P("pr_warehouse", x, y + 13, 0);
            P("pr_containers", x - 14, y + 5, 0); P("pr_containers", x + 2, y + 6, 0); P("pr_containers", x + 17, y + 6, 0); P("pr_containers", x - 3, y + 6, 0);
            P("pr_fueltank", x + 25, y + 4); P("pr_ctower", x - 26, y + 6);
            return;
        }
    }
    void PlaceAirfield(List<Vector3> sites)
    {
        Vector2 p;
        float L = 80f + sizeIdx * 10f;
        if (!FindSite(sites, L * 0.5f + 6, out p)) return;
        float rw = 8f;
        Fill(p.x - L / 2, p.y - rw / 2, p.x + L / 2, p.y + rw / 2, RUNWAY);
        Fill(p.x - L * 0.3f, p.y + rw / 2, p.x + L * 0.1f, p.y + rw / 2 + 22f, AIRFIELD);
        for (int x = (int)(p.x - L * 0.3f); x < p.x + L * 0.1f; x += 14) { }
        sites.Add(new Vector3(p.x, p.y + 6, L * 0.5f));
        airfields.Add(p); airfieldAng.Add(0f);
        P("pr_hangar", p.x - L * 0.22f, p.y + 18, 0); P("pr_hangar", p.x - L * 0.05f, p.y + 18, 0); P("pr_ctower", p.x + L * 0.14f, p.y + 15); P("pr_fueltank", p.x + L * 0.2f, p.y + 18); P("pr_radar", p.x + L * 0.22f, p.y + 12);
        P("pr_helipad", p.x - L * 0.28f, p.y + 9);
    }
    void PlaceBase(List<Vector3> sites, Faction f)
    {
        Vector2 p;
        bool left = f == Faction.Ukraine;
        for (int tries = 0; tries < 400; tries++)
        {
            float x = left ? Rn(30f, w * 0.38f) : Rn(w * 0.62f, w - 30f), y = Rn(h * 0.35f, h - 30f);
            if (SiteFree(sites, x, y, 24f)) { p = new Vector2(x, y); goto found; }
        }
        if (!FindSite(sites, 24f, out p)) return;
    found:
        Fill(p.x - 22, p.y - 15, p.x + 22, p.y + 15, BASE);
        sites.Add(new Vector3(p.x, p.y, 24f)); baseSites.Add(p);
        // fence perimeter
        for (int i = -4; i <= 4; i++) { P("ot_fence", p.x + i * 5f, p.y - 15.5f); P("ot_fence", p.x + i * 5f, p.y + 15.5f); }
        for (int i = -2; i <= 2; i++) { P("ot_fence", p.x - 22.5f, p.y + i * 5f, 90); P("ot_fence", p.x + 22.5f, p.y + i * 5f, 90); }
        P("pr_watch", p.x - 22, p.y - 15); P("pr_watch", p.x + 22, p.y + 15); P("pr_watch", p.x - 22, p.y + 15); P("pr_watch", p.x + 22, p.y - 15);
        for (int i = 0; i < 3; i++) P("pr_barracks", p.x - 14 + i * 0.0f, p.y - 9 + i * 7.5f, 0, -1, f);
        P("pr_cmd", p.x + 6, p.y, 0, -1, f); P("pr_bunker", p.x + 16, p.y + 8, 0, -1, f); P("pr_bunker", p.x + 16, p.y - 8, 0, -1, f);
        P("pr_radar", p.x + 6, p.y + 10); P("pr_ammo", p.x + 4, p.y - 10); P("ot_flag", p.x - 4, p.y + 11, 0, -1, f); P("pr_helipad", p.x + 14, p.y + 0.5f);
        P("ot_tent", p.x - 4, p.y - 11, 0, -1, f); P("ot_hospital", p.x - 4, p.y - 6, 0, -1, f);
        P("ot_mgnest", p.x + 21, p.y + 3, 0, -1, f); P("ot_mgnest", p.x + 21, p.y - 3, 0, -1, f);
        if (left) { }
    }
    void PlaceIndustrial(List<Vector3> sites)
    {
        Vector2 p; if (!FindSite(sites, 28f, out p)) return;
        Fill(p.x - 25, p.y - 15, p.x + 25, p.y + 15, INDUSTRIAL);
        sites.Add(new Vector3(p.x, p.y, 28f));
        P("pr_factory", p.x - 12, p.y + 6); P("pr_factory", p.x + 8, p.y + 6); P("pr_warehouse", p.x - 15, p.y - 8); P("pr_warehouse", p.x, p.y - 8); P("pr_warehouse", p.x + 15, p.y - 8);
        for (int i = 0; i < 4; i++) P("pr_fueltank", p.x + 20, p.y - 11 + i * 6f);
        P("pr_pylon", p.x - 22, p.y); P("pr_containers", p.x + 24, p.y + 8); P("pr_garage", p.x - 24, p.y + 10);
    }
    void PlaceCity(List<Vector3> sites, float R)
    {
        Vector2 p; if (!FindSite(sites, R + 4, out p)) return;
        sites.Add(new Vector3(p.x, p.y, R + 4));
        float sp = 14f;
        for (int y = (int)(p.y - R); y <= p.y + R; y++)
            for (int x = (int)(p.x - R); x <= p.x + R; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float dx = x - p.x, dy = y - p.y;
                float rr = R * (1f + 0.12f * Mathf.Sin(Mathf.Atan2(dy, dx) * 4f));
                if (dx * dx + dy * dy > rr * rr) continue;
                if (IsWater(ter[y * w + x])) continue;
                ter[y * w + x] = URBAN;
            }
        // streets
        int n = (int)(R / sp);
        for (int i = -n; i <= n; i++)
        {
            float sx = p.x + i * sp, sy = p.y + i * sp;
            for (int k = (int)-R; k <= R; k++)
            {
                for (int wd = 0; wd < 4; wd++)
                {
                    int x = (int)(sx + wd - 1), y = (int)(p.y + k);
                    if (x >= 0 && y >= 0 && x < w && y < h && ter[y * w + x] == URBAN) ter[y * w + x] = ROAD;
                    x = (int)(p.x + k); y = (int)(sy + wd - 1);
                    if (x >= 0 && y >= 0 && x < w && y < h && ter[y * w + x] == URBAN) ter[y * w + x] = ROAD;
                }
            }
        }
        // buildings per block
        for (int bx = -n; bx < n; bx++)
            for (int by = -n; by < n; by++)
            {
                float cx = p.x + bx * sp + sp / 2f + 0.5f, cy = p.y + by * sp + sp / 2f + 0.5f;
                if ((new Vector2(cx - p.x, cy - p.y)).magnitude > R - 4f) continue;
                if (ter[Mathf.Clamp((int)cy, 0, h - 1) * w + Mathf.Clamp((int)cx, 0, w - 1)] != URBAN) continue;
                float centre = (new Vector2(cx - p.x, cy - p.y)).magnitude / R;
                int kind = rng.Next(0, 10);
                if (centre < 0.35f && kind < 5) P("pr_tower", cx, cy);
                else if (kind < 5) { P("pr_apt", cx - 3.2f, cy + 2.4f, 0); P("pr_apt", cx + 0.5f, cy - 2.6f, 0); }
                else if (kind < 7) { P("pr_house", cx - 3, cy - 2.5f); P("pr_cottage", cx + 2.6f, cy + 2.4f); P("pr_garage", cx - 2, cy + 3.5f); }
                else if (kind < 8) P("pr_church", cx, cy);
                else if (kind < 9) P("pr_warehouse", cx, cy);
                else { P("pr_apt", cx, cy, 0); }
            }
    }
    void PlaceVillage(List<Vector3> sites)
    {
        Vector2 p; if (!FindSite(sites, 12f, out p)) return;
        sites.Add(new Vector3(p.x, p.y, 12f));
        for (int y = (int)(p.y - 9); y <= p.y + 9; y++) for (int x = (int)(p.x - 12); x <= p.x + 12; x++)
            { float dx = (x - p.x) / 12f, dy = (y - p.y) / 9f; if (dx * dx + dy * dy < 1f && (ter[y * w + x] == FIELD || ter[y * w + x] == FOREST)) ter[y * w + x] = VILLAGE; }
        int n = rng.Next(5, 10);
        for (int i = 0; i < n; i++)
        {
            float a = Rn(0, 360), r = Rn(2f, 9f);
            Vector2 q = p + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad) * r * 1.2f, Mathf.Sin(a * Mathf.Deg2Rad) * r * 0.9f);
            P(i % 3 == 0 ? "pr_cottage" : "pr_house", q.x, q.y, 0);
        }
        if (rng.Next(0, 3) == 0) P("pr_church", p.x + 1, p.y - 1);
        for (int i = 0; i < 2; i++) P("ot_haystack", p.x + Rn(-10, 10), p.y + Rn(-8, 8));
    }

    void Road(Vector2 a, Vector2 b, int width)
    {
        // A* over the coarse grid; bridge where the path crosses a river
        int sx = Mathf.Clamp((int)(a.x / NC), 0, nw - 1), sy = Mathf.Clamp((int)(a.y / NC), 0, nh - 1);
        int gx = Mathf.Clamp((int)(b.x / NC), 0, nw - 1), gy = Mathf.Clamp((int)(b.y / NC), 0, nh - 1);
        int start = sy * nw + sx, goal = gy * nw + gx;
        stampId++; heap.Clear(); aG[start] = 0; aStamp[start] = stampId; aParent[start] = -1; HPush(start, 0);
        bool ok = false; int expanded = 0;
        while (heap.Count > 0 && expanded < 60000)
        {
            var cur = HPop(); int ci = cur.i; int cx = ci % nw, cy = ci / nw;
            if (cur.f - Heur(cx, cy, gx, gy) > aG[ci] + 0.001f) continue;
            expanded++;
            if (ci == goal) { ok = true; break; }
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy; if (nx < 0 || ny < 0 || nx >= nw || ny >= nh) continue;
                    int ni = ny * nw + nx;
                    float c = 1f;
                    int wc = nWater[ni];
                    if (wc >= 3) { c = nTer[ni] == SEA ? 200f : (wc > 12 ? 90f : 8f); }
                    if (nTer[ni] == FOREST) c = Mathf.Max(c, 2.5f);
                    if (nRoad[ni] >= 6) c = 0.5f;
                    if (nTer[ni] == BASE || nTer[ni] == INDUSTRIAL || nTer[ni] == AIRFIELD) c = Mathf.Max(c, 1.5f);
                    float ng = aG[ci] + c * (dx != 0 && dy != 0 ? 1.414f : 1f);
                    if (aStamp[ni] != stampId || ng < aG[ni] - 0.0001f) { aStamp[ni] = stampId; aG[ni] = ng; aParent[ni] = ci; HPush(ni, ng + Heur(nx, ny, gx, gy)); }
                }
        }
        if (!ok) return;
        var pts = new List<Vector2> { a };
        int k = goal; var rev = new List<Vector2>();
        while (k != -1 && k != start) { rev.Add(new Vector2((k % nw + 0.5f) * NC, (k / nw + 0.5f) * NC)); k = aParent[k]; }
        rev.Reverse(); pts.AddRange(rev); pts.Add(b);
        // smooth with catmull-ish sampling and rasterise
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector2 p0 = pts[i], p1 = pts[i + 1]; float d = (p1 - p0).magnitude; int steps = Mathf.CeilToInt(d * 1.5f);
            for (int s = 0; s <= steps; s++)
            {
                Vector2 q = Vector2.Lerp(p0, p1, s / (float)Mathf.Max(1, steps));
                for (int dy = -width / 2; dy <= width / 2; dy++) for (int dx = -width / 2; dx <= width / 2; dx++)
                    {
                        int x = (int)q.x + dx, y = (int)q.y + dy; if (x < 0 || y < 0 || x >= w || y >= h) continue;
                        byte t = ter[y * w + x];
                        if (t == RIVER || t == LAKE) ter[y * w + x] = BRIDGE;
                        else if (t == FIELD || t == FOREST || t == BEACH || t == VILLAGE) ter[y * w + x] = ROAD;
                    }
            }
        }
        // widen bridges perpendicular so they connect properly (done by raster width)
        for (int ny = 0; ny < nh; ny++) for (int nx = 0; nx < nw; nx++) RecalcNav(nx, ny);
    }

    void PlaceTrees()
    {
        int forestCells = 0; for (int i = 0; i < ter.Length; i++) if (ter[i] == FOREST) forestCells++;
        float cap = 5200f + sizeIdx * 700f;
        float dens = Mathf.Min(0.2f, cap / Mathf.Max(1, forestCells));
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                byte t = ter[y * w + x];
                float pr = 0f;
                if (t == FOREST) pr = dens; else if (t == FIELD) pr = 0.002f; else if (t == VILLAGE) pr = 0.03f; else if (t == BEACH) pr = 0.002f;
                if (pr <= 0f || rng.NextDouble() > pr) continue;
                if (pending.Count > 20000) return;
                float px = x + (float)rng.NextDouble(), py = y + (float)rng.NextDouble();
                int kind = rng.Next(0, 10);
                string id = winter || kind < 5 ? "ot_pine" : (kind < 8 ? "ot_oak" : "ot_birch");
                if (winter && kind > 7) id = "ot_birch";
                P(id, px, py, rng.Next(0, 360));
            }
    }

    // ---------------------------------------------------------------- terrain texture
    static readonly Color[] FieldCols = { G.Hex(0x729d4c), G.Hex(0xc7b35c), G.Hex(0x8a6c3e), G.Hex(0x86ad4a) };
    void BuildTextures()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
        root = new GameObject("World");
        int tw = w * ppc, th = h * ppc;
        var pix = new Color32[tw * th];
        float[] jit = new float[1];
        for (int py = 0; py < th; py++)
        {
            int cyBase = py / ppc;
            for (int px = 0; px < tw; px++)
            {
                float ux = (px + 0.5f) / ppc, uy = (py + 0.5f) / ppc;
                float jx = (G.Hash(px / 2, py / 2, 3) - 0.5f) * 0.9f, jy = (G.Hash(px / 2, py / 2, 4) - 0.5f) * 0.9f;
                int cx = Mathf.Clamp((int)(ux + jx), 0, w - 1), cy = Mathf.Clamp((int)(uy + jy), 0, h - 1);
                byte t = ter[cy * w + cx];
                Color c = TerColor(t, cx, cy, ux, uy, px, py);
                float nse = (G.Hash(px, py, 9) - 0.5f) * 0.07f;
                pix[py * tw + px] = new Color32((byte)Mathf.Clamp((c.r + nse) * 255f, 0, 255), (byte)Mathf.Clamp((c.g + nse) * 255f, 0, 255), (byte)Mathf.Clamp((c.b + nse) * 255f, 0, 255), 255);
            }
        }
        // runway markings
        for (int i = 0; i < airfields.Count; i++)
        {
            float L = 80f + sizeIdx * 10f; Vector2 a = airfields[i];
            StampOn(pix, tw, a.x - L / 2 + 3, a.y, L - 6, 0.25f, new Color(0.9f, 0.9f, 0.85f, 0.9f), true);
            for (int k = 0; k < 9; k++) { StampOn(pix, tw, a.x - L / 2 + 1.5f, a.y - 3.2f + k * 0.8f, 3.5f, 0.35f, new Color(0.9f, 0.9f, 0.85f, 0.9f), false); StampOn(pix, tw, a.x + L / 2 - 5f, a.y - 3.2f + k * 0.8f, 3.5f, 0.35f, new Color(0.9f, 0.9f, 0.85f, 0.9f), false); }
        }
        terTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        terTex.filterMode = FilterMode.Bilinear; terTex.wrapMode = TextureWrapMode.Clamp;
        terTex.SetPixels32(pix); terTex.Apply(false);
        var go = new GameObject("Terrain"); go.transform.SetParent(root.transform, false);
        terSR = go.AddComponent<SpriteRenderer>();
        terSR.sprite = Sprite.Create(terTex, new Rect(0, 0, tw, th), Vector2.zero, ppc, 0, SpriteMeshType.FullRect);
        terSR.sortingOrder = -100;
        decPix = new Color32[tw * th];
        decTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        decTex.filterMode = FilterMode.Bilinear; decTex.wrapMode = TextureWrapMode.Clamp;
        decTex.SetPixels32(decPix); decTex.Apply(false);
        var go2 = new GameObject("Decals"); go2.transform.SetParent(root.transform, false);
        decSR = go2.AddComponent<SpriteRenderer>();
        decSR.sprite = Sprite.Create(decTex, new Rect(0, 0, tw, th), Vector2.zero, ppc, 0, SpriteMeshType.FullRect);
        decSR.sortingOrder = -90;
        // sea backdrop beyond the map edge
        var bg = new GameObject("Backdrop"); bg.transform.SetParent(root.transform, false);
        var bsr = bg.AddComponent<SpriteRenderer>(); bsr.sprite = Art.Spr.white; bsr.color = G.Hex(0x14202a); bsr.sortingOrder = -110;
        bg.transform.position = new Vector3(w / 2f, h / 2f, 0); bg.transform.localScale = new Vector3((w + 600f) * 4f, (h + 600f) * 4f, 1);
        terrainTexRef = terTex;
        wetness = 0f;
    }

    void StampOn(Color32[] pix, int tw, float x, float y, float len, float thick, Color c, bool dashed)
    {
        int x0 = (int)(x * ppc), x1 = (int)((x + len) * ppc), y0 = (int)((y - thick / 2f) * ppc), y1 = (int)((y + thick / 2f) * ppc);
        for (int py = y0; py <= y1; py++) for (int px = x0; px <= x1; px++)
            {
                if (px < 0 || py < 0 || px >= tw || py >= h * ppc) continue;
                if (dashed && ((px - x0) / (ppc * 2)) % 2 == 1) continue;
                pix[py * tw + px] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
            }
    }

    Color TerColor(byte t, int cx, int cy, float ux, float uy, int px, int py)
    {
        float n1 = Mathf.PerlinNoise(ux * 0.05f + 11f, uy * 0.05f + 3f);
        float n2 = Mathf.PerlinNoise(ux * 0.4f + 5f, uy * 0.4f + 9f);
        float var = 0.9f + n1 * 0.18f + (n2 - 0.5f) * 0.08f;
        Color c;
        switch (t)
        {
            case FIELD:
                {
                    int f = fld[cy * w + cx];
                    c = f == 0 ? G.Hex(0x6f9a4c) : FieldCols[f];
                    if (f != 0) { float stripe = ((int)(ux * 0.6f + uy * 0.2f)) % 2 == 0 ? 1f : 0.93f; var *= stripe; }
                    if (winter) c = Color.Lerp(c, G.Hex(0xe4eaee), 0.82f);
                    break;
                }
            case FOREST: c = winter ? G.Hex(0xc9d4d0) : G.Hex(0x4a7a3c); var *= 0.95f; break;
            case ROAD: c = G.Hex(0x4c4f54); var = 0.94f + n2 * 0.1f; break;
            case URBAN: c = G.Hex(0x8e8c84); var = 0.92f + n2 * 0.14f; break;
            case VILLAGE: c = winter ? G.Hex(0xd8dcd8) : G.Hex(0x80884f); break;
            case LAKE: { c = G.Hex(0x2f6c98); var = 0.92f + n1 * 0.2f; c = Shore(c, cx, cy, ux, uy); break; }
            case RIVER: { c = G.Hex(0x347aa5); var = 0.92f + n1 * 0.2f; c = Shore(c, cx, cy, ux, uy); break; }
            case SEA: { c = G.Hex(0x1b5a8c); var = 0.85f + n1 * 0.25f; c = Shore(c, cx, cy, ux, uy); break; }
            case BRIDGE: c = G.Hex(0x57585a); var = 0.95f; if (cx < w - 1 && cy < h - 1 && (IsWater(AtCell(cx, cy + 1)) || IsWater(AtCell(cx, cy - 1)) || IsWater(AtCell(cx + 1, cy)) || IsWater(AtCell(cx - 1, cy)))) c = G.Hex(0x6e6e68); break;
            case INDUSTRIAL: c = G.Hex(0x7c7e78); var = 0.94f + n2 * 0.1f; break;
            case BASE: c = G.Hex(0x6d6a50); var = 0.9f + n2 * 0.2f; break;
            case PORT: c = G.Hex(0x8c9096); var = 0.94f + n2 * 0.08f; if (IsWater(AtCell(cx, cy - 1)) || IsWater(AtCell(cx + 1, cy)) || IsWater(AtCell(cx - 1, cy))) c = G.Hex(0x6b6e74); break;
            case AIRFIELD: c = G.Hex(0x7f8284); var = 0.95f + n2 * 0.08f; break;
            case RUNWAY: c = G.Hex(0x3b3d41); var = 0.95f + n2 * 0.1f; break;
            case BEACH: c = winter ? G.Hex(0xd8dcdc) : G.Hex(0xd3c490); break;
            default: c = Color.magenta; break;
        }
        return new Color(c.r * var, c.g * var, c.b * var, 1f);
    }
    Color Shore(Color c, int cx, int cy, float ux, float uy)
    {
        int land = 0;
        for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++) { byte t = AtCell(cx + dx, cy + dy); if (!IsWater(t)) land++; }
        float sh = Mathf.Clamp01(land / 14f);
        Color shallow = G.Hex(0x58a6b8);
        float wave = Mathf.PerlinNoise(ux * 0.25f + 40f, uy * 0.9f + 12f);
        c = Color.Lerp(c, new Color(c.r * 1.12f, c.g * 1.12f, c.b * 1.1f), wave * 0.5f);
        return Color.Lerp(c, shallow, sh * 0.85f);
    }

    // ---------------------------------------------------------------- decals
    public void StampCircle(Vector2 pos, float radius, Color c, float hardness = 0.3f)
    {
        if (decPix == null) return;
        int tw = w * ppc, th = h * ppc;
        int x0 = Mathf.Max(0, (int)((pos.x - radius) * ppc)), x1 = Mathf.Min(tw - 1, (int)((pos.x + radius) * ppc));
        int y0 = Mathf.Max(0, (int)((pos.y - radius) * ppc)), y1 = Mathf.Min(th - 1, (int)((pos.y + radius) * ppc));
        float rp = radius * ppc;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - pos.x * ppc, dy = y + 0.5f - pos.y * ppc;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / rp;
                if (d >= 1f) continue;
                float a = c.a * Mathf.Clamp01((1f - d) / Mathf.Max(0.01f, 1f - hardness));
                a *= 0.8f + 0.2f * G.Hash(x, y, 77);
                int i = y * tw + x; Color32 dst = decPix[i];
                float da = dst.a / 255f; float oa = a + da * (1f - a);
                if (oa <= 0) continue;
                float kd = da * (1f - a);
                decPix[i] = new Color32((byte)((c.r * 255f * a + dst.r * kd) / oa), (byte)((c.g * 255f * a + dst.g * kd) / oa), (byte)((c.b * 255f * a + dst.b * kd) / oa), (byte)(oa * 255f));
            }
        decDirty = true;
    }
    public void Scorch(Vector2 pos, float radius)
    {
        StampCircle(pos, radius * 1.6f, new Color(0.08f, 0.07f, 0.06f, 0.35f), 0.1f);
        StampCircle(pos, radius * 0.9f, new Color(0.05f, 0.04f, 0.035f, 0.7f), 0.5f);
        for (int i = 0; i < 4; i++) StampCircle(pos + UnityEngine.Random.insideUnitCircle * radius, radius * 0.4f, new Color(0.06f, 0.05f, 0.04f, 0.4f), 0.2f);
    }
    public void Blood(Vector2 pos) { StampCircle(pos, 0.45f, new Color(0.3f, 0.02f, 0.02f, 0.5f), 0.2f); }
    public void TrackMark(Vector2 pos, float r)
    {
        byte t = At(pos);
        if (t == ROAD || t == URBAN || t == BRIDGE || t == RUNWAY || t == AIRFIELD || t == PORT || t == INDUSTRIAL || IsWater(t)) return;
        Color c = t == BEACH ? new Color(0.5f, 0.42f, 0.28f, 0.35f) : (winter || weather == Weather.Snow ? new Color(0.45f, 0.5f, 0.55f, 0.45f) : new Color(0.18f, 0.13f, 0.08f, 0.3f + wetness * 0.2f));
        StampCircle(pos, r, c, 0.5f);
    }
    public void FlushDecals(float dt)
    {
        decTimer += dt;
        if (decDirty && decTimer > 0.2f)
        {
            decTimer = 0; decDirty = false;
            decTex.SetPixels32(decPix); decTex.Apply(false);
        }
    }

    // ---------------------------------------------------------------- terrain modification (bridge deployment)
    public int DeployBridge(Vector2 from, Vector2 dir, float maxLen = 18f)
    {
        int n = 0; dir.Normalize(); Vector2 side = new Vector2(-dir.y, dir.x);
        // only bridge when water is ahead
        bool sawWater = false;
        for (float s = 0; s < maxLen; s += 0.5f)
        {
            Vector2 q = from + dir * s;
            if (IsWater(At(q))) sawWater = true;
            else if (sawWater && s > 3f && !IsWater(At(q + dir * 1.5f))) { }
            for (int k = -2; k <= 2; k++)
            {
                Vector2 qq = q + side * k * 0.6f; int x = (int)qq.x, y = (int)qq.y;
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                byte t = ter[y * w + x];
                if (t == RIVER || t == LAKE) { ter[y * w + x] = BRIDGE; n++; PaintCell(x, y, G.Hex(0x65605a)); }
            }
        }
        if (n > 0) { for (int ny = 0; ny < nh; ny++) for (int nx = 0; nx < nw; nx++) RecalcNav(nx, ny); }
        return n;
    }
    void PaintCell(int cx, int cy, Color c)
    {
        int tw = w * ppc; var px = new Color32[ppc * ppc];
        for (int i = 0; i < px.Length; i++) { float nse = (G.Hash(cx * 7 + i, cy, 5) - 0.5f) * 0.07f; px[i] = new Color32((byte)Mathf.Clamp((c.r + nse) * 255, 0, 255), (byte)Mathf.Clamp((c.g + nse) * 255, 0, 255), (byte)Mathf.Clamp((c.b + nse) * 255, 0, 255), 255); }
        terTex.SetPixels32(cx * ppc, cy * ppc, ppc, ppc, px);
        terDirty = true;
    }
    bool terDirty;
    public void FlushTerrain() { if (terDirty) { terTex.Apply(false); terDirty = false; } }

    // ---------------------------------------------------------------- per-frame
    public void Tick(float dt)
    {
        simTime += dt;
        if (autoCycle) { tod += dt * 24f / Mathf.Max(30f, dayLength); if (tod >= 24f) tod -= 24f; }
        float target = (weather == Weather.Rain ? 1f : (weather == Weather.Snow ? 0.5f : 0f));
        wetness = Mathf.MoveTowards(wetness, target, dt * (target > wetness ? 0.04f : 0.01f));
        for (int i = flares.Count - 1; i >= 0; i--) { flares[i].Tick(dt); if (flares[i].life <= 0) flares.RemoveAt(i); }
        FlushDecals(dt); FlushTerrain();
    }

    public void ClearAll()
    {
        for (int i = ents.Count - 1; i >= 0; i--) ents[i].Destroy();
        ents.Clear();
        for (int i = props.Count - 1; i >= 0; i--) props[i].Destroy();
        props.Clear();
        flares.Clear();
    }
    public void ClearUnitsKeepProps()
    {
        for (int i = ents.Count - 1; i >= 0; i--) ents[i].Destroy();
        ents.Clear();
        flares.Clear();
        if (decPix != null) { Array.Clear(decPix, 0, decPix.Length); decDirty = true; }
    }
    public void Destroy() { ClearAll(); if (root != null) UnityEngine.Object.Destroy(root); if (terTex) UnityEngine.Object.Destroy(terTex); if (decTex) UnityEngine.Object.Destroy(decTex); }
}
