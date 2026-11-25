using System;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Pooled particle system built from sprite renderers
// ---------------------------------------------------------------------------
public class Part
{
    public GameObject go; public Transform tr; public SpriteRenderer sr;
    public Vector2 pos, vel; public float life, max, s0, s1, rot, rotV, drag; public Color c0, c1; public bool add;
}

public static class Fx
{
    static List<Part> active = new List<Part>(2048), pool = new List<Part>(2048);
    static Transform root; static Material defMat;
    public static float quality = 1f; public static int maxParts = 3000;
    public static Rect view = new Rect(0, 0, 100, 100);
    static List<FireSpot> spots = new List<FireSpot>();
    class FireSpot { public Vector2 pos; public float r, t, e; }

    public static void Init()
    {
        if (root != null) return;
        root = new GameObject("FX").transform;
    }
    public static void Clear()
    {
        for (int i = active.Count - 1; i >= 0; i--) Release(active[i], i);
        spots.Clear();
    }
    static void Release(Part p, int idx)
    {
        p.go.SetActive(false); pool.Add(p);
        int last = active.Count - 1; active[idx] = active[last]; active.RemoveAt(last);
    }
    public static bool InView(Vector2 p, float margin = 18f)
    {
        return p.x > view.xMin - margin && p.x < view.xMax + margin && p.y > view.yMin - margin && p.y < view.yMax + margin;
    }
    public static Part Spawn(Sprite s, Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c0, Color c1, float drag = 0f, float rotV = 0f, int order = 33, bool add = false, float rot = 0f)
    {
        if (active.Count >= maxParts) return null;
        Part p;
        if (pool.Count > 0) { p = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); p.go.SetActive(true); }
        else
        {
            p = new Part(); p.go = new GameObject("p"); p.tr = p.go.transform; p.tr.SetParent(root, false);
            p.sr = p.go.AddComponent<SpriteRenderer>();
            if (defMat == null) defMat = p.sr.sharedMaterial;
        }
        p.sr.sprite = s; p.sr.sortingOrder = order;
        p.sr.sharedMaterial = add ? Util.Additive : defMat;
        p.add = add; p.pos = pos; p.vel = vel; p.life = p.max = Mathf.Max(0.02f, life); p.s0 = s0; p.s1 = s1; p.c0 = c0; p.c1 = c1; p.drag = drag; p.rot = rot; p.rotV = rotV;
        p.tr.position = new Vector3(pos.x, pos.y, 0); p.tr.localScale = Vector3.one * s0; p.tr.rotation = Quaternion.Euler(0, 0, rot); p.sr.color = c0;
        active.Add(p);
        return p;
    }
    public static void Update(float dt)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Part p = active[i];
            p.life -= dt;
            if (p.life <= 0f) { Release(p, i); continue; }
            float t = 1f - p.life / p.max;
            p.pos += p.vel * dt;
            if (p.drag > 0f) p.vel *= Mathf.Exp(-p.drag * dt);
            p.rot += p.rotV * dt;
            float sc = Mathf.Lerp(p.s0, p.s1, t);
            p.tr.position = new Vector3(p.pos.x, p.pos.y, 0);
            p.tr.localScale = new Vector3(sc, sc, 1);
            p.tr.rotation = Quaternion.Euler(0, 0, p.rot);
            p.sr.color = Color.Lerp(p.c0, p.c1, t);
        }
        for (int i = spots.Count - 1; i >= 0; i--)
        {
            var s = spots[i]; s.t -= dt; s.e -= dt;
            if (s.t <= 0f) { spots.RemoveAt(i); continue; }
            if (s.e <= 0f) { s.e = 0.06f; Vector2 q = s.pos + UnityEngine.Random.insideUnitCircle * s.r; Fire(q, 0.8f + s.r * 0.12f); if (G.R01() < 0.35f) Smoke(q, 1.2f, true, Vector2.up); }
        }
    }
    public static void FirePatch(Vector2 pos, float r, float dur) { spots.Add(new FireSpot { pos = pos, r = r, t = dur }); }

    static float Q(float n) { return Mathf.Max(1f, n * quality); }

    public static void Smoke(Vector2 pos, float size, bool dark, Vector2 vel)
    {
        if (!InView(pos)) return;
        Color c0 = dark ? new Color(0.1f, 0.095f, 0.09f, 0.8f) : new Color(0.75f, 0.75f, 0.75f, 0.5f);
        Color c1 = dark ? new Color(0.22f, 0.22f, 0.22f, 0f) : new Color(0.85f, 0.85f, 0.85f, 0f);
        Vector2 w = World.I != null ? World.I.WindVec() : Vector2.zero;
        Spawn(Art.Spr.smoke[UnityEngine.Random.Range(0, 3)], pos, vel * 0.6f + w * 0.8f + UnityEngine.Random.insideUnitCircle * 0.3f, G.Rnd(2.2f, 3.8f), size * 0.4f, size * 1.7f, c0, c1, 0.3f, G.Rnd(-25, 25), 34, false, G.Rnd(0, 360));
    }
    public static void Puff(Vector2 pos, Color c, float size, float life, Vector2 vel)
    {
        if (!InView(pos)) return;
        Color c1 = c; c1.a = 0;
        Spawn(Art.Spr.smoke[UnityEngine.Random.Range(0, 3)], pos, vel, life, size * 0.5f, size * 1.5f, c, c1, 0.5f, G.Rnd(-30, 30), 33, false, G.Rnd(0, 360));
    }
    public static void Dust(Vector2 pos, float size, Color c)
    {
        if (!InView(pos, 8f)) return;
        Color c1 = c; c1.a = 0;
        Spawn(Art.Spr.smoke[UnityEngine.Random.Range(0, 3)], pos, UnityEngine.Random.insideUnitCircle * 0.7f, G.Rnd(0.7f, 1.2f), size * 0.4f, size * 1.2f, c, c1, 1f, G.Rnd(-20, 20), 31, false, G.Rnd(0, 360));
    }
    public static void Fire(Vector2 pos, float size)
    {
        if (!InView(pos)) return;
        Spawn(Art.Spr.flame[UnityEngine.Random.Range(0, 3)], pos, new Vector2(G.Rnd(-0.5f, 0.5f), G.Rnd(0.6f, 1.8f)), G.Rnd(0.35f, 0.7f), size * 0.9f, size * 0.25f, new Color(1f, 0.85f, 0.4f, 0.95f), new Color(1f, 0.2f, 0.03f, 0f), 0.5f, G.Rnd(-60, 60), 73, true, G.Rnd(0, 360));
    }
    public static void Glow(Vector2 pos, float size, Color c, float life)
    {
        if (!InView(pos)) return;
        float night = World.I != null ? 1f - World.I.Daylight * 0.75f : 1f;
        c.a *= night;
        Color c1 = c; c1.a = 0;
        Spawn(Art.Spr.glow, pos, Vector2.zero, life, size, size * 0.9f, c, c1, 0, 0, 74, true);
    }
    public static void Spark(Vector2 pos, Vector2 dir, int n)
    {
        if (!InView(pos)) return;
        n = (int)Q(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 v = (dir.normalized * 0.4f + UnityEngine.Random.insideUnitCircle).normalized * G.Rnd(4f, 10f);
            Spawn(Art.Spr.spark, pos, v, G.Rnd(0.15f, 0.4f), 0.7f, 0.15f, new Color(1f, 0.85f, 0.5f, 1f), new Color(1f, 0.4f, 0.1f, 0f), 2.5f, 0, 73, true, G.Ang(v));
        }
    }
    public static void Sparkle(Vector2 pos)
    {
        for (int i = 0; i < 6; i++) Spawn(Art.Spr.sparkle, pos, UnityEngine.Random.insideUnitCircle * 2f, 0.6f, 1.5f, 0.3f, new Color(0.7f, 1f, 0.8f, 1f), new Color(0.7f, 1f, 0.8f, 0f), 1f, 0, 73, true);
    }
    public static void Heal(Vector2 pos) { Spawn(Art.Spr.dot, pos + UnityEngine.Random.insideUnitCircle * 0.3f, Vector2.up * 0.8f, 0.8f, 0.6f, 0.9f, new Color(0.3f, 1f, 0.4f, 0.9f), new Color(0.3f, 1f, 0.4f, 0f), 0.2f, 0, 60, false); }
    public static void Debris(Vector2 pos, int n, float speed)
    {
        if (!InView(pos)) return;
        n = (int)Q(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 v = UnityEngine.Random.insideUnitCircle.normalized * speed * G.Rnd(0.3f, 1.2f);
            Spawn(Art.Spr.debris[UnityEngine.Random.Range(0, 4)], pos, v, G.Rnd(0.7f, 1.6f), G.Rnd(0.8f, 1.5f), 0.7f, new Color(0.7f, 0.7f, 0.7f, 1f), new Color(0.3f, 0.3f, 0.3f, 0f), 2.2f, G.Rnd(-500, 500), 32, false, G.Rnd(0, 360));
        }
    }
    public static void Leaves(Vector2 pos)
    {
        for (int i = 0; i < 6; i++) Spawn(Art.Spr.dot, pos, UnityEngine.Random.insideUnitCircle * 2f, 0.9f, 0.5f, 0.2f, new Color(0.3f, 0.55f, 0.2f, 0.9f), new Color(0.3f, 0.45f, 0.15f, 0f), 2f, 0, 35, false);
    }
    public static void Blood(Vector2 pos)
    {
        if (!InView(pos, 4f)) return;
        for (int i = 0; i < 3; i++) Spawn(Art.Spr.dot, pos, UnityEngine.Random.insideUnitCircle * 2f, 0.35f, 0.5f, 0.2f, new Color(0.55f, 0.05f, 0.05f, 0.9f), new Color(0.4f, 0.02f, 0.02f, 0f), 3f, 0, 22, false);
    }
    public static void Splash(Vector2 pos, float size)
    {
        if (!InView(pos)) return;
        Spawn(Art.Spr.shock, pos, Vector2.zero, 0.7f, size * 0.4f, size * 2.2f, new Color(0.9f, 0.97f, 1f, 0.7f), new Color(0.9f, 0.97f, 1f, 0f), 0, 0, 3, false);
        int n = (int)Q(5 + size * 3);
        for (int i = 0; i < n; i++) Spawn(Art.Spr.dot, pos, UnityEngine.Random.insideUnitCircle.normalized * G.Rnd(1f, 4f) * size, G.Rnd(0.4f, 0.8f), 0.6f * size, 0.2f, new Color(0.9f, 0.97f, 1f, 0.9f), new Color(0.9f, 0.97f, 1f, 0f), 1.5f, 0, 35, false);
        Sfx.Play(Sfx.Id.Splash, pos, 0.4f * size);
    }
    public static void Wake(Vector2 pos, float size, Vector2 vel)
    {
        if (!InView(pos)) return;
        Spawn(Art.Spr.wakeFoam, pos, vel, G.Rnd(2.2f, 3.2f), size * 0.6f, size * 2.3f, new Color(1, 1, 1, 0.5f), new Color(1, 1, 1, 0f), 0.6f, 0, 2, false, G.Rnd(0, 360));
    }
    public static void CanopyCollapse(Vector2 pos, float rot)
    {
        Spawn(Art.Spr.canopy, pos, Vector2.zero, 2f, 0.9f, 0.6f, new Color(1, 1, 1, 0.95f), new Color(1, 1, 1, 0f), 0, 0, 5, false, rot);
    }
    public static void Muzzle(Vector2 pos, float ang, float size)
    {
        if (!InView(pos, 6f)) return;
        Sprite s = size < 0.9f ? Art.Spr.muzzle0 : (size < 2f ? Art.Spr.muzzle1 : Art.Spr.muzzle2);
        Spawn(s, pos, Vector2.zero, 0.07f + size * 0.015f, size, size * 0.8f, new Color(1f, 0.95f, 0.7f, 1f), new Color(1f, 0.6f, 0.2f, 0f), 0, 0, 73, true, ang);
        Glow(pos, size * 3.2f, new Color(1f, 0.75f, 0.35f, 0.4f), 0.08f);
    }
    public static void Casing(Vector2 pos, float ang)
    {
        if (!InView(pos, 6f)) return;
        Vector2 v = G.Dir(ang + 90f + G.Rnd(-30, 30)) * G.Rnd(2f, 4f);
        Spawn(Art.Spr.casing, pos, v, 0.45f, 1f, 1f, new Color(1, 1, 1, 1f), new Color(1, 1, 1, 0f), 4f, G.Rnd(-900, 900), 24, false, G.Rnd(0, 360));
    }
    public static void Shock(Vector2 pos, float size)
    {
        Spawn(Art.Spr.shock, pos, Vector2.zero, 0.45f, size * 0.3f, size * 2.6f, new Color(1f, 0.95f, 0.85f, 0.55f), new Color(1f, 0.9f, 0.7f, 0f), 0, 0, 36, true);
    }

    // big composite effect
    public static void Explosion(Vector2 pos, float size, bool fire)
    {
        bool water = World.I != null && World.I.Water(pos);
        if (!InView(pos, 40f)) { Sfx.Play(size > 2f ? Sfx.Id.ExplosionBig : Sfx.Id.Explosion, pos, Mathf.Clamp(size * 0.4f, 0.3f, 1f)); return; }
        if (water) { Splash(pos, size * 1.5f); Sfx.Play(Sfx.Id.Explosion, pos, 0.7f); return; }
        Glow(pos, size * 6f, new Color(1f, 0.85f, 0.5f, 0.8f), 0.18f);
        Glow(pos, size * 3f, new Color(1f, 1f, 0.9f, 0.9f), 0.1f);
        Shock(pos, size * 2.2f);
        int nf = (int)Q(5 + size * 5);
        for (int i = 0; i < nf; i++)
        {
            Vector2 v = UnityEngine.Random.insideUnitCircle * size * 4.5f;
            Spawn(Art.Spr.flame[UnityEngine.Random.Range(0, 3)], pos + UnityEngine.Random.insideUnitCircle * size * 0.4f, v, G.Rnd(0.4f, 0.9f), size * G.Rnd(0.8f, 1.5f), size * 0.3f, new Color(1f, 0.92f, 0.55f, 1f), new Color(1f, 0.25f, 0.03f, 0f), 2.5f, G.Rnd(-100, 100), 73, true, G.Rnd(0, 360));
        }
        int ns = (int)Q(4 + size * 4);
        for (int i = 0; i < ns; i++)
        {
            bool dark = i % 3 != 0;
            Vector2 v = UnityEngine.Random.insideUnitCircle * size * 2.2f + Vector2.up * 0.8f;
            Color c0 = dark ? new Color(0.08f, 0.075f, 0.07f, 0.85f) : new Color(0.5f, 0.5f, 0.5f, 0.7f);
            Spawn(Art.Spr.smoke[UnityEngine.Random.Range(0, 3)], pos + UnityEngine.Random.insideUnitCircle * size * 0.5f, v, G.Rnd(2.5f, 4.5f), size * 0.9f, size * G.Rnd(2.2f, 3.4f), c0, new Color(0.3f, 0.3f, 0.3f, 0f), 1.1f, G.Rnd(-20, 20), 35, false, G.Rnd(0, 360));
        }
        int nd = (int)Q(size * 3 + 3);
        Debris(pos, nd, size * 4.5f + 3f);
        Spark(pos, Vector2.up, (int)(6 + size * 3));
        // dust ring
        for (int i = 0; i < 8; i++)
        {
            Vector2 d = G.Dir(i * 45f + G.Rnd(0, 20)); Color dc = new Color(0.6f, 0.52f, 0.4f, 0.5f);
            Spawn(Art.Spr.smoke[UnityEngine.Random.Range(0, 3)], pos + d * size * 0.6f, d * size * 3.2f, 1.1f, size * 0.7f, size * 1.9f, dc, new Color(dc.r, dc.g, dc.b, 0), 2.0f, 0, 30, false, G.Rnd(0, 360));
        }
        if (size >= 0.7f) World.I.Scorch(pos, size * 1.1f);
        Sfx.Play(size > 2f ? Sfx.Id.ExplosionBig : Sfx.Id.Explosion, pos, Mathf.Clamp(0.4f + size * 0.25f, 0.4f, 1f));
        Game.I.Shake(Mathf.Clamp(size * 0.12f, 0.02f, 0.5f), pos);
        if (fire) FirePatch(pos, size * 1.2f, G.Rnd(5f, 9f));
    }
}

// ---------------------------------------------------------------------------
// Flares (abstract countermeasure decoys)
// ---------------------------------------------------------------------------
public class Flare
{
    public Vector2 pos, vel; public float life = 3f; public Faction fac;
    GameObject go; SpriteRenderer sr, glow; float trailT;
    public static Flare Spawn(Vector2 p, Vector2 v, Faction f)
    {
        var fl = new Flare { pos = p, vel = v, fac = f, life = G.Rnd(2.4f, 3.2f) };
        fl.go = new GameObject("flare");
        fl.sr = fl.go.AddComponent<SpriteRenderer>(); fl.sr.sprite = Art.Spr.flare; fl.sr.sortingOrder = 75; fl.sr.sharedMaterial = Util.Additive;
        fl.go.transform.position = new Vector3(p.x, p.y, 0); fl.go.transform.localScale = Vector3.one * 1.3f;
        return fl;
    }
    public void Tick(float dt)
    {
        life -= dt;
        pos += vel * dt; vel *= Mathf.Exp(-1.3f * dt);
        trailT -= dt;
        if (trailT <= 0f && life > 0f)
        {
            trailT = 0.035f;
            Fx.Spawn(Art.Spr.glow, pos, Vector2.zero, 0.45f, 0.9f, 0.2f, new Color(1f, 0.85f, 0.5f, 0.7f), new Color(1f, 0.4f, 0.1f, 0f), 0, 0, 74, true);
            if (G.R01() < 0.3f) Fx.Smoke(pos, 0.6f, false, Vector2.zero);
        }
        if (go != null)
        {
            float fl = 0.8f + 0.4f * UnityEngine.Random.value;
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = Vector3.one * (1.2f * fl) * Mathf.Clamp01(life * 2f);
        }
        if (life <= 0f && go != null) { UnityEngine.Object.Destroy(go); go = null; }
    }
    public void Kill() { life = 0; if (go != null) { UnityEngine.Object.Destroy(go); go = null; } }
}

// ---------------------------------------------------------------------------
// Projectiles (abstract gameplay ballistics)
// ---------------------------------------------------------------------------
public enum PT { Bullet, Shell, Arc, Rocket, Missile, Bomb }

public class Proj
{
    public GameObject go; public Transform tr; public SpriteRenderer sr, shadow;
    public PT type; public Vector2 start, end, pos, dir; public float t, dur, arcH, speed, life, dmg, pen, aoe, turn;
    public Ent owner; public Faction fac; public Ent homing; public Flare decoy; public bool hitAir, hitGround, fire, expl = true, active, aaDetonate;
    public WT wt; float trailT; Vector2 vel; float ang; int sprayId;
    public static List<Proj> live = new List<Proj>(), pool = new List<Proj>();
    static Transform root;

    public static Proj Get(Sprite s, int order, bool add)
    {
        if (root == null) root = new GameObject("Projectiles").transform;
        Proj p;
        if (pool.Count > 0) { p = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); p.go.SetActive(true); }
        else
        {
            p = new Proj(); p.go = new GameObject("proj"); p.tr = p.go.transform; p.tr.SetParent(root, false); p.sr = p.go.AddComponent<SpriteRenderer>();
            var sg = new GameObject("sh"); sg.transform.SetParent(p.tr, false); p.shadow = sg.AddComponent<SpriteRenderer>();
        }
        p.sr.sprite = s; p.sr.sortingOrder = order; p.sr.sharedMaterial = add ? Util.Additive : p.shadow.sharedMaterial; p.sr.color = Color.white;
        p.shadow.gameObject.SetActive(false);
        p.decoy = null; p.homing = null; p.fire = false; p.expl = true; p.aaDetonate = false; p.trailT = 0; p.active = true;
        live.Add(p);
        return p;
    }
    public static void Clear() { for (int i = live.Count - 1; i >= 0; i--) { live[i].go.SetActive(false); pool.Add(live[i]); } live.Clear(); }

    public static void UpdateAll(float dt)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Proj p = live[i];
            bool done = p.Step(dt);
            if (done) { p.go.SetActive(false); p.active = false; pool.Add(p); int last = live.Count - 1; live[i] = live[last]; live.RemoveAt(last); }
        }
    }

    bool Step(float dt)
    {
        switch (type)
        {
            case PT.Bullet:
            case PT.Shell:
                t += dt;
                pos = Vector2.Lerp(start, end, Mathf.Clamp01(t / dur));
                tr.position = new Vector3(pos.x, pos.y, 0);
                if (t >= dur) { Combat.Impact(this, end); return true; }
                return false;
            case PT.Arc:
                {
                    t += dt; float k = Mathf.Clamp01(t / dur);
                    pos = Vector2.Lerp(start, end, k);
                    float h = 4f * k * (1f - k) * arcH;
                    tr.position = new Vector3(pos.x, pos.y + h * 0.6f, 0);
                    float sc = 1f + h * 0.05f; tr.localScale = new Vector3(sc, sc, 1);
                    Vector2 d = (end - start).normalized; float slope = (4f * (1f - 2f * k) * arcH * 0.6f) / Mathf.Max(1f, (end - start).magnitude / dur) * 0.2f;
                    tr.rotation = Quaternion.Euler(0, 0, G.Ang(d) + Mathf.Clamp(slope * 40f, -80f, 80f));
                    shadow.gameObject.SetActive(true); shadow.sprite = sr.sprite; shadow.color = new Color(0, 0, 0, 0.4f); shadow.sortingOrder = 29;
                    shadow.transform.position = new Vector3(pos.x, pos.y, 0); shadow.transform.localScale = Vector3.one * 0.8f / sc;
                    if (k > 0.05f && k < 0.9f) { trailT -= dt; if (trailT <= 0f) { trailT = 0.06f; if (Fx.InView(pos)) Fx.Spawn(Art.Spr.smoke[0], new Vector2(pos.x, pos.y + h * 0.6f), Vector2.zero, 0.7f, 0.25f, 0.8f, new Color(0.9f, 0.9f, 0.9f, 0.28f), new Color(0.9f, 0.9f, 0.9f, 0f), 0, 0, 33); } }
                    if (t >= dur) { Combat.Impact(this, end); return true; }
                    return false;
                }
            case PT.Bomb:
                {
                    t += dt; float k = Mathf.Clamp01(t / dur);
                    pos = Vector2.Lerp(start, end, k);
                    float sc = Mathf.Lerp(1.5f, 0.8f, k); tr.localScale = new Vector3(sc, sc, 1);
                    tr.position = new Vector3(pos.x, pos.y + (1f - k) * 3f, 0);
                    tr.rotation = Quaternion.Euler(0, 0, ang + t * 200f);
                    shadow.gameObject.SetActive(true); shadow.sprite = sr.sprite; shadow.color = new Color(0, 0, 0, 0.35f); shadow.sortingOrder = 29; shadow.transform.position = new Vector3(pos.x + 0.3f, pos.y - 0.3f, 0); shadow.transform.localScale = Vector3.one * 0.8f / sc;
                    if (t >= dur) { Combat.Impact(this, end); return true; }
                    return false;
                }
            default: // Rocket, Missile
                {
                    life -= dt; t += dt;
                    Vector2 aimP = end;
                    if (type == PT.Missile && homing != null && !homing.removed && !homing.dead) aimP = homing.pos + homing.vel * 0.15f;
                    if (type == PT.Missile && decoy != null)
                    {
                        if (decoy.life > 0f) aimP = decoy.pos; else decoy = null;
                    }
                    else if (type == PT.Missile && homing != null && homing.Airborne && World.I.flares.Count > 0)
                    {
                        foreach (var fl in World.I.flares)
                            if (fl.life > 0.4f && fl.fac == homing.fac && (fl.pos - pos).magnitude < 20f && G.R01() < dt * 5f) { decoy = fl; break; }
                    }
                    Vector2 toT = aimP - pos; float dist = toT.magnitude;
                    if (type == PT.Missile)
                    {
                        float want = G.Ang(toT);
                        ang += Mathf.Clamp(Mathf.DeltaAngle(ang, want), -turn * dt, turn * dt);
                    }
                    else ang = G.Ang(toT);
                    float sp = speed * Mathf.Clamp01(0.35f + t * 1.4f);
                    dir = G.Dir(ang);
                    float step = sp * dt;
                    pos += dir * step;
                    tr.position = new Vector3(pos.x, pos.y, 0); tr.rotation = Quaternion.Euler(0, 0, ang);
                    trailT -= dt;
                    if (trailT <= 0f && Fx.InView(pos))
                    {
                        trailT = 0.035f;
                        Fx.Spawn(Art.Spr.smoke[1], pos - dir * 0.6f, Vector2.zero, type == PT.Missile ? 1.5f : 1.0f, 0.3f, 1.0f, new Color(0.92f, 0.92f, 0.92f, 0.5f), new Color(0.9f, 0.9f, 0.9f, 0f), 0, 0, 33, false, G.Rnd(0, 360));
                        Fx.Spawn(Art.Spr.glow, pos - dir * 0.5f, Vector2.zero, 0.12f, 0.9f, 0.3f, new Color(1f, 0.7f, 0.3f, 0.8f), new Color(1f, 0.4f, 0.1f, 0f), 0, 0, 74, true);
                    }
                    if (decoy != null && (decoy.pos - pos).magnitude < 2.2f)
                    {
                        Fx.Explosion(pos, 0.5f, false); return true;      // fooled by a flare
                    }
                    if (dist < Mathf.Max(1.2f, step * 1.3f) || (type == PT.Rocket && (pos - start).magnitude >= (end - start).magnitude))
                    { Combat.Impact(this, pos); return true; }
                    if (type == PT.Missile && homing != null && (homing.removed || homing.dead) && life > 0.5f) { homing = null; end = pos + dir * 30f; }
                    if (life <= 0f) { Combat.Impact(this, pos); return true; }
                    return false;
                }
        }
    }

    public void SetAng(float a) { ang = a; }
    public float Ang { get { return ang; } }
}

public static class Combat
{
    static List<Ent> buf = new List<Ent>();

    public static bool Threatened(Ent e, out float dist)
    {
        dist = 1e9f; bool th = false;
        foreach (var p in Proj.live)
            if (p.type == PT.Missile && p.homing == e && p.decoy == null) { float d = (p.pos - e.pos).magnitude; if (d < dist) dist = d; th = true; }
        return th;
    }

    public static void DeployFlares(Aircraft a)
    {
        for (int i = 0; i < 8; i++)
        {
            float ang = a.rot + 180f + (i - 3.5f) * 26f + G.Rnd(-6, 6);
            Vector2 v = G.Dir(ang) * G.Rnd(10f, 20f) + a.vel * 0.5f;
            World.I.flares.Add(Flare.Spawn(a.pos + G.Dir(a.rot + 180f) * 1.5f, v, a.fac));
        }
        Fx.Glow(a.pos, 3f, new Color(1f, 0.9f, 0.6f, 0.7f), 0.2f);
        Sfx.Play(Sfx.Id.Flare, a.pos, 0.6f);
    }

    // area damage
    public static void Explode(Vector2 pos, float aoe, float dmg, float pen, Ent src, Faction f, bool hitAir, bool hitGround, bool fire, bool expl)
    {
        var l = new List<Ent>();
        World.I.Query(pos, aoe + 3f, l, true);
        Faction sf = src != null ? src.fac : f;
        for (int i = 0; i < l.Count; i++)
        {
            Ent e = l[i];
            if (e == src || e.removed || e.dead && e.kind == Kind.Soldier) continue;
            if (e.hidden) continue;
            bool airborne = e.Airborne;
            if (airborne && !hitAir) continue;
            if (!airborne && !hitGround) continue;
            if (sf != Faction.Neutral && e.fac == sf) continue;
            float d = (e.pos - pos).magnitude - e.radius * 0.7f;
            if (d > aoe) continue;
            float fall = d <= 0f ? 1f : 1f - Mathf.Clamp01(d / aoe) * 0.7f;
            if (e.kind == Kind.Prop)
            {
                Prop p = e as Prop; if (p != null)
                {
                    if (p.dead && !p.IsTree) continue;
                    p.Hit(dmg * fall, pen, pos, src, expl, false); if (fire) p.Hit(0, 0, pos, src, false, true);
                }
                continue;
            }
            e.Hit(dmg * fall, pen, pos, src, expl, fire);
        }
        if (fire && aoe > 2.5f) Fx.FirePatch(pos, aoe * 0.55f, G.Rnd(6f, 12f));
    }

    public static void Impact(Proj p, Vector2 at)
    {
        bool water = World.I.Water(at);
        float aoe = p.aoe;
        switch (p.type)
        {
            case PT.Bullet:
                if (water) { Fx.Splash(at, 0.4f); }
                else { Fx.Dust(at, 0.5f, new Color(0.6f, 0.55f, 0.45f, 0.4f)); if (G.R01() < 0.4f) Fx.Spark(at, Vector2.up, 2); }
                Explode(at, aoe, p.dmg, p.pen, p.owner, p.fac, p.hitAir, p.hitGround, false, false);
                break;
            case PT.Shell:
                if (water) Fx.Splash(at, 1.2f); else { Fx.Explosion(at, 0.7f + aoe * 0.22f, false); }
                Explode(at, aoe, p.dmg, p.pen, p.owner, p.fac, p.hitAir, p.hitGround, false, true);
                break;
            default:
                {
                    float sz = Mathf.Clamp(0.5f + aoe * 0.3f, 0.5f, 3.2f);
                    if (water) Fx.Splash(at, sz * 1.5f); else Fx.Explosion(at, sz, p.fire);
                    if (p.aaDetonate) { }
                    Explode(at, aoe, p.dmg, p.pen, p.owner, p.fac, p.hitAir, p.hitGround, p.fire, true);
                    if (p.fire && !water) { Fx.FirePatch(at, aoe * 0.7f, G.Rnd(7f, 14f)); }
                    break;
                }
        }
    }

    static bool IsArcType(WT t) { return t == WT.Arty || t == WT.Mortar; }

    // The unified firing routine: visuals, sound, projectile creation
    public static void FireWeapon(Unit u, WS s, Vector2 m, Vector2 aim, Ent target)
    {
        W d = s.d;
        float dist = (aim - m).magnitude;
        float baseAng = G.Ang(aim - m);
        float ang = baseAng;
        bool lobbed = d.t == WT.Arty || d.t == WT.Mortar || d.t == WT.Salvo || d.t == WT.Flame || d.t == WT.Bomb;
        Vector2 imp = aim;
        float sprd = s.d.spread * (u is Soldier ? 1f : 1f);
        if (lobbed)
        {
            imp = aim + UnityEngine.Random.insideUnitCircle * sprd * (0.4f + dist / 160f) * (u is Soldier ? 1f : 1f);
        }
        else
        {
            float jitter = G.Rnd(-1f, 1f) * sprd * (u != null ? 1f : 1f);
            jitter *= (u is Vehicle ? 1f : 1f);
            ang = baseAng + jitter * (u is Soldier ? 1f : 1f);
            float shoot = dist * (d.t == WT.Missile || d.t == WT.SAM ? 1f : 1f + G.Rnd(-0.05f, 0.06f));
            imp = m + G.Dir(ang) * shoot;
            // on target hit chance improved by crouching handled via SpreadMul in Unit (applied through s.d.spread scale)
        }
        float sp = Mathf.Max(20f, d.spd);
        var owner = u;
        switch (d.t)
        {
            case WT.Rifle: case WT.MG: case WT.Gun: case WT.AAGun: case WT.Auto: case WT.Sniper:
                {
                    var p = Proj.Get(Art.Spr.bullet, 36, true);
                    p.type = PT.Bullet; p.start = m; p.end = imp; p.pos = m; p.t = 0; p.dur = Mathf.Max(0.04f, (imp - m).magnitude / sp);
                    p.dmg = d.dmg; p.pen = d.pen; p.aoe = d.aoe; p.owner = owner; p.fac = u.fac; p.hitAir = target != null ? target.Airborne : d.air; p.hitGround = target != null ? !target.Airborne : d.ground; p.wt = d.t;
                    p.tr.position = new Vector3(m.x, m.y, 0); p.tr.localScale = new Vector3(d.t == WT.Auto || d.t == WT.AAGun ? 1.3f : 1f, 1f, 1f); p.tr.rotation = Quaternion.Euler(0, 0, ang);
                    p.sr.color = d.t == WT.AAGun || d.t == WT.Auto ? new Color(1f, 0.55f, 0.35f, 1f) : new Color(1f, 0.9f, 0.55f, 1f);
                    float ms = d.t == WT.Rifle || d.t == WT.MG ? 0.55f : (d.t == WT.Sniper ? 0.7f : 0.9f);
                    Fx.Muzzle(m, ang, ms);
                    if (d.t != WT.Auto && d.t != WT.AAGun) Fx.Casing(m - G.Dir(ang) * 0.2f, ang);
                    if (G.R01() < 0.3f) Fx.Puff(m + G.Dir(ang) * 0.3f, new Color(0.85f, 0.85f, 0.85f, 0.25f), 0.45f, 0.7f, G.Dir(ang) * 0.5f);
                    Sfx.Play(d.t == WT.Rifle || d.t == WT.Sniper ? Sfx.Id.Rifle : (d.t == WT.MG ? Sfx.Id.MG : Sfx.Id.Auto), m, d.t == WT.Sniper ? 1f : 0.55f);
                    break;
                }
            case WT.Cannon:
                {
                    var p = Proj.Get(Art.Spr.shell, 36, false);
                    p.type = PT.Shell; p.start = m; p.end = imp; p.pos = m; p.t = 0; p.dur = Mathf.Max(0.05f, (imp - m).magnitude / sp);
                    p.dmg = d.dmg; p.pen = d.pen; p.aoe = d.aoe; p.owner = owner; p.fac = u.fac; p.hitAir = false; p.hitGround = true; p.wt = d.t;
                    p.tr.position = new Vector3(m.x, m.y, 0); p.tr.localScale = Vector3.one * 1.1f; p.tr.rotation = Quaternion.Euler(0, 0, ang); p.sr.color = Color.white;
                    Fx.Muzzle(m, ang, 2.3f);
                    for (int i = 0; i < 3; i++) Fx.Puff(m + G.Dir(ang) * (0.5f + i * 0.8f), new Color(0.8f, 0.8f, 0.78f, 0.55f), 1.2f + i * 0.3f, 1.3f, G.Dir(ang) * (2f + i));
                    Fx.Dust(m - G.Dir(ang) * 0.5f, 1.4f, new Color(0.6f, 0.55f, 0.45f, 0.35f));
                    Sfx.Play(Sfx.Id.Cannon, m, 1f);
                    Game.I.Shake(0.05f, m);
                    break;
                }
            case WT.Arty: case WT.Mortar:
                {
                    var p = Proj.Get(Art.Spr.shell, 37, false);
                    float dd = (imp - m).magnitude;
                    p.type = PT.Arc; p.start = m; p.end = imp; p.pos = m; p.t = 0; p.dur = Mathf.Clamp(dd / sp, 1.0f, 9f); p.arcH = Mathf.Clamp(dd * 0.18f, 3f, 28f);
                    p.dmg = d.dmg; p.pen = d.pen; p.aoe = d.aoe; p.owner = owner; p.fac = u.fac; p.hitAir = false; p.hitGround = true; p.wt = d.t;
                    p.tr.position = new Vector3(m.x, m.y, 0); p.tr.localScale = Vector3.one * (d.t == WT.Mortar ? 0.8f : 1.2f); p.sr.color = Color.white;
                    Fx.Muzzle(m, ang, d.t == WT.Mortar ? 1.2f : 3.0f);
                    for (int i = 0; i < 5; i++) Fx.Puff(m + G.Dir(ang) * (0.6f + i * 0.7f) + UnityEngine.Random.insideUnitCircle * 0.5f, new Color(0.82f, 0.82f, 0.8f, 0.6f), 1.8f + i * 0.25f, 2.2f, G.Dir(ang) * (2.5f + i * 0.5f));
                    Fx.Dust(m, 2.2f, new Color(0.62f, 0.55f, 0.45f, 0.45f));
                    Sfx.Play(d.t == WT.Mortar ? Sfx.Id.Mortar : Sfx.Id.Arty, m, 1f);
                    Game.I.Shake(0.08f, m);
                    break;
                }
            case WT.Rocket: case WT.Salvo: case WT.Flame:
                {
                    var p = Proj.Get(Art.Spr.rocket, 37, false);
                    p.type = PT.Rocket; p.start = m; p.end = imp; p.pos = m; p.t = 0; p.life = 12f; p.speed = Mathf.Max(30f, sp); p.SetAng(G.Ang(imp - m));
                    p.dmg = d.dmg; p.pen = d.pen; p.aoe = d.aoe; p.owner = owner; p.fac = u.fac; p.hitAir = false; p.hitGround = true; p.wt = d.t; p.fire = d.t == WT.Flame; p.turn = 0f;
                    p.tr.position = new Vector3(m.x, m.y, 0); p.tr.localScale = Vector3.one * 1.0f; p.sr.color = Color.white;
                    Fx.Muzzle(m, ang, 1.4f);
                    for (int i = 0; i < 3; i++) Fx.Puff(m - G.Dir(ang) * (0.5f + i * 0.5f) + UnityEngine.Random.insideUnitCircle * 0.4f, new Color(0.9f, 0.9f, 0.88f, 0.55f), 1.4f, 1.5f, -G.Dir(ang) * 1.5f);
                    Sfx.Play(Sfx.Id.Rocket, m, 0.8f);
                    break;
                }
            case WT.Missile: case WT.SAM:
                {
                    var p = Proj.Get(Art.Spr.missile, 38, false);
                    p.type = PT.Missile; p.start = m; p.pos = m; p.t = 0; p.life = 14f; p.speed = Mathf.Max(30f, sp); p.SetAng(baseAng); p.turn = 130f;
                    p.homing = target; p.end = target != null ? target.pos : imp;
                    p.dmg = d.dmg; p.pen = d.pen; p.aoe = d.aoe; p.owner = owner; p.fac = u.fac; p.hitAir = target != null ? target.Airborne : d.air; p.hitGround = target != null ? !target.Airborne : d.ground; p.wt = d.t;
                    p.tr.position = new Vector3(m.x, m.y, 0); p.tr.localScale = Vector3.one * 1.0f; p.sr.color = Color.white;
                    Fx.Muzzle(m, ang, 1.2f);
                    for (int i = 0; i < 4; i++) Fx.Puff(m - G.Dir(ang) * (0.4f + i * 0.5f), new Color(0.95f, 0.95f, 0.95f, 0.6f), 1.5f, 1.6f, -G.Dir(ang) * 1.2f);
                    Sfx.Play(Sfx.Id.Missile, m, 0.8f);
                    break;
                }
            case WT.Bomb:
                {
                    var p = Proj.Get(Art.Spr.bomb, 39, false);
                    Vector2 fwd = u.vel;
                    Vector2 e = m + fwd * 1.1f + UnityEngine.Random.insideUnitCircle * d.spread;
                    p.type = PT.Bomb; p.start = m; p.end = e; p.pos = m; p.t = 0; p.dur = 1.1f; p.SetAng(G.Ang(fwd));
                    p.dmg = d.dmg; p.pen = d.pen; p.aoe = d.aoe; p.owner = owner; p.fac = u.fac; p.hitAir = false; p.hitGround = true; p.wt = d.t;
                    p.tr.position = new Vector3(m.x, m.y, 0); p.sr.color = Color.white;
                    Sfx.Play(Sfx.Id.Whoosh, m, 0.6f);
                    break;
                }
        }
    }
}
