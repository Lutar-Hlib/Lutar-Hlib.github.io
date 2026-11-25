using System;
using System.Collections.Generic;
using UnityEngine;

// Palette derived from a brigade (uniform colours, camo, emblem).
public class Pal
{
    public Color main, sec, dark, light, accent, helmet;
    public Color metal = G.Hex(0x2a2d2a), rubber = G.Hex(0x181818), glass = G.Hex(0x1d2d3a), skin = G.Hex(0xd2a37e);
    public int emblem, camo, bars, seed;
    public Faction fac;

    public static Pal Make(Brigade b, Faction f)
    {
        var p = new Pal();
        p.fac = f;
        if (b == null)
        {
            p.main = G.Hex(0x7a7a72); p.sec = G.Hex(0x5a5a54); p.accent = G.Hex(0xb0b0a8); p.helmet = p.sec; p.emblem = 14; p.camo = 0; p.bars = 0; p.seed = 7;
        }
        else
        {
            p.main = b.main; p.sec = b.sec; p.accent = b.accent; p.helmet = b.helmet; p.emblem = b.emblem; p.camo = b.camo; p.bars = b.idx; p.seed = b.idx * 31 + (int)f * 7 + 3;
        }
        p.dark = G.Mul(p.main, 0.62f);
        p.light = G.Mul(p.main, 1.22f);
        p.dark.a = p.light.a = 1;
        return p;
    }
    public string Key { get { return ((int)fac) + "_" + seed; } }
}

public class Model
{
    public Sprite hull, turret, gun, dmg, icon, shadow, body;
    public Sprite[] trk = new Sprite[4], whl = new Sprite[4];
    public Vector2[] wheelPos = new Vector2[0];
    public float wheelR = 0.4f, trackY, trackLen, gunPivotX, gunLen, turX;
    public bool tracked;
    public Sprite[] fr;                       // soldier frames
    public Vector2[] eng = new Vector2[0];    // engine nozzles / props
    public float engSize = 0.5f;
    public Sprite rotor, rotorBlur, tailRotor, prop, propBlur;
    public Vector2[] rotorPos = new Vector2[0];
    public float rotorR = 3f, tailRotR = 0.8f;
    public Vector2 tailRotPos;
    public Sprite[] mounts = new Sprite[2];
    public Vector2[] mountPos = new Vector2[2];
    public float[] mountLen = new float[2];
    public int propBlades;
}

public static partial class Art
{
    static Dictionary<string, Model> cache = new Dictionary<string, Model>();
    static Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    public static int NextSeed(int s) { return s * 1103515245 + 12345; }

    public static Model Get(UnitDef d, Brigade b, Faction f)
    {
        Pal pal = Pal.Make(b, f);
        string key = d.id + "|" + pal.Key;
        Model m;
        if (cache.TryGetValue(key, out m)) return m;
        m = Build(d, pal, false);
        cache[key] = m;
        return m;
    }

    public static Sprite Icon(UnitDef d, Brigade b, Faction f)
    {
        Pal pal = Pal.Make(b, f);
        string key = d.id + "|" + pal.Key;
        Sprite s;
        if (iconCache.TryGetValue(key, out s)) return s;
        Model m = Build(d, pal, true);
        s = m.icon;
        iconCache[key] = s;
        return s;
    }

    static Model Build(UnitDef d, Pal pal, bool icon)
    {
        switch (d.fam)
        {
            case "tracked":
            case "wheeled": return BuildGround(d, pal, icon);
            case "soldier": return BuildSoldier(d, pal, icon);
            case "plane": return BuildPlane(d, pal, icon);
            case "heli": return BuildHeli(d, pal, icon);
            case "drone": return BuildDrone(d, pal, icon);
            case "ship": return BuildShip(d, pal, icon);
            case "prop": return BuildProp(d, pal, icon, 0, 0);
            default: return BuildSpecialIcon(d, pal);
        }
    }

    static Model BuildSpecialIcon(UnitDef d, Pal pal)
    {
        var m = new Model();
        var p = new Pix(2.4f, 2.4f, 16);
        p.Circle(G.Hex(0xe8e8e0), 0, 0.2f, 0.9f);
        p.Circle(pal.main, 0, 0.2f, 0.75f);
        p.Line(G.Hex(0xe8e8e0), -0.7f, 0.2f, 0.7f, 0.2f, 0.1f);
        p.Line(G.Hex(0xe8e8e0), -0.5f, -0.1f, -0.1f, -0.9f, 0.08f);
        p.Line(G.Hex(0xe8e8e0), 0.5f, -0.1f, 0.1f, -0.9f, 0.08f);
        p.Line(G.Hex(0xe8e8e0), 0, 0.2f, 0, -0.95f, 0.08f);
        p.Poly(G.Hex(0xe8e8e0), -0.9f, 0.4f, 0, 1.1f, 0.9f, 0.4f);
        m.icon = p.Make();
        return m;
    }

    // ------------------------------------------------------------------ markings
    public static void FacMark(Pix p, Pal pal, float x, float y, float w, float h)
    {
        // horizontal stripes of the national colours
        if (pal.fac == Faction.Ukraine)
        {
            p.Rect(G.Hex(0x1f6fd6), x, y + h * 0.25f, w, h * 0.5f);
            p.Rect(G.Hex(0xffd500), x, y - h * 0.25f, w, h * 0.5f);
        }
        else if (pal.fac == Faction.Russia)
        {
            p.Rect(G.Hex(0xf2f2f2), x, y + h / 3f, w, h / 3f);
            p.Rect(G.Hex(0x2a4fc8), x, y, w, h / 3f);
            p.Rect(G.Hex(0xd02a2a), x, y - h / 3f, w, h / 3f);
        }
    }

    public static void Camo(Pix p, Pal pal, int seed, float scale = 1f)
    {
        if (pal.camo == 0) return;
        var r = new System.Random(seed + pal.seed * 13);
        float hw = p.HalfW, hh = p.HalfH;
        Func<float, float, float> R = (a, b) => a + (float)r.NextDouble() * (b - a);
        switch (pal.camo)
        {
            case 1:
                for (int i = 0; i < 16; i++) p.Ell(pal.sec, R(-hw, hw), R(-hh, hh), R(0.25f, 0.8f) * scale, R(0.18f, 0.5f) * scale, R(0, 180), true, 14);
                break;
            case 2:
                for (int i = 0; i < 90; i++) { float s = R(0.12f, 0.3f) * scale; p.Rect(pal.sec, R(-hw, hw), R(-hh, hh), s, s, 0, true); }
                break;
            case 3:
                for (int i = 0; i < 10; i++) { float x = R(-hw, hw); p.Line(pal.sec, x - 0.5f, -hh, x + 0.5f, hh, R(0.1f, 0.22f) * scale, true); }
                break;
            default:
                for (int i = 0; i < 70; i++) p.Circle(i % 2 == 0 ? pal.sec : pal.light, R(-hw, hw), R(-hh, hh), R(0.05f, 0.16f) * scale, true);
                break;
        }
    }

    public static float[] Tf(float[] a, float x, float y, float s, float rot)
    {
        var r = new float[a.Length];
        float cr = Mathf.Cos(rot * Mathf.Deg2Rad), sr = Mathf.Sin(rot * Mathf.Deg2Rad);
        for (int i = 0; i < a.Length; i += 2)
        {
            float lx = a[i] * s, ly = a[i + 1] * s;
            r[i] = x + lx * cr - ly * sr; r[i + 1] = y + lx * sr + ly * cr;
        }
        return r;
    }

    // Brigade emblem (14 distinct designs)
    public static void Emblem(Pix p, int id, float x, float y, float s, Color c, Color c2, float rot = 0f)
    {
        Action<Color, float, float, float, float, float> L = (col, x0, y0, x1, y1, th) =>
        {
            var a = Tf(new float[] { x0, y0, x1, y1 }, x, y, s, rot);
            p.Line(col, a[0], a[1], a[2], a[3], th * s);
        };
        Action<Color, float[]> P = (col, pts) => p.Poly(col, Tf(pts, x, y, s, rot), false);
        Action<Color, float, float, float, float> E = (col, cx, cy, rx, ry) =>
        {
            var a = Tf(new float[] { cx, cy }, x, y, s, rot);
            p.Ell(col, a[0], a[1], rx * s, ry * s, rot, false, 16);
        };
        switch (id)
        {
            case 0: // trident
                L(c, 0, -0.5f, 0, 0.45f, 0.12f); L(c, -0.3f, 0.05f, -0.3f, 0.45f, 0.1f); L(c, 0.3f, 0.05f, 0.3f, 0.45f, 0.1f); L(c, -0.3f, 0.05f, 0.3f, 0.05f, 0.1f);
                P(c, new float[] { -0.3f, 0.55f, -0.38f, 0.4f, -0.22f, 0.4f }); P(c, new float[] { 0.3f, 0.55f, 0.22f, 0.4f, 0.38f, 0.4f }); P(c, new float[] { 0, 0.6f, -0.08f, 0.42f, 0.08f, 0.42f });
                break;
            case 1: // star
                {
                    var pts = new float[20];
                    for (int i = 0; i < 10; i++) { float a = (90 + i * 36) * Mathf.Deg2Rad; float rr = i % 2 == 0 ? 0.5f : 0.22f; pts[i * 2] = Mathf.Cos(a) * rr; pts[i * 2 + 1] = Mathf.Sin(a) * rr; }
                    P(c, pts);
                    break;
                }
            case 2: // wolf
                P(c, new float[] { -0.5f, -0.05f, -0.38f, 0.5f, -0.12f, 0.18f, 0.12f, 0.18f, 0.38f, 0.5f, 0.5f, -0.05f, 0.2f, -0.5f, -0.2f, -0.5f });
                P(c2, new float[] { -0.28f, 0.0f, -0.08f, 0.04f, -0.26f, 0.12f }); P(c2, new float[] { 0.28f, 0.0f, 0.08f, 0.04f, 0.26f, 0.12f });
                break;
            case 3: // wings
                P(c, new float[] { -0.04f, -0.3f, -0.5f, 0.15f, -0.5f, 0.42f, -0.04f, 0.12f });
                P(c, new float[] { 0.04f, -0.3f, 0.5f, 0.15f, 0.5f, 0.42f, 0.04f, 0.12f });
                P(c2, new float[] { 0, -0.45f, 0.1f, -0.05f, 0, 0.3f, -0.1f, -0.05f });
                break;
            case 4: // anchor
                E(c, 0, 0.36f, 0.14f, 0.14f); E(c2, 0, 0.36f, 0.06f, 0.06f);
                L(c, 0, 0.25f, 0, -0.42f, 0.1f); L(c, -0.28f, 0.08f, 0.28f, 0.08f, 0.1f);
                L(c, -0.36f, -0.12f, -0.02f, -0.46f, 0.1f); L(c, 0.36f, -0.12f, 0.02f, -0.46f, 0.1f);
                break;
            case 5: // horns
                L(c, -0.42f, 0.42f, -0.46f, -0.08f, 0.12f); L(c, -0.46f, -0.08f, -0.2f, -0.42f, 0.12f); L(c, -0.2f, -0.42f, 0.2f, -0.42f, 0.12f);
                L(c, 0.2f, -0.42f, 0.46f, -0.08f, 0.12f); L(c, 0.46f, -0.08f, 0.42f, 0.42f, 0.12f); E(c2, 0, 0.05f, 0.12f, 0.12f);
                break;
            case 6: // lightning
                P(c, new float[] { 0.12f, 0.52f, -0.32f, -0.05f, -0.02f, -0.05f, -0.14f, -0.52f, 0.34f, 0.1f, 0.04f, 0.1f });
                break;
            case 7: // paw
                E(c, 0, -0.12f, 0.3f, 0.24f); E(c, -0.32f, 0.24f, 0.1f, 0.12f); E(c, -0.11f, 0.38f, 0.1f, 0.12f); E(c, 0.11f, 0.38f, 0.1f, 0.12f); E(c, 0.32f, 0.24f, 0.1f, 0.12f);
                break;
            case 8: // shield
                P(c, new float[] { -0.42f, 0.46f, 0.42f, 0.46f, 0.42f, 0.0f, 0, -0.52f, -0.42f, 0.0f });
                P(c2, new float[] { -0.2f, 0.3f, 0.2f, 0.3f, 0.2f, 0.0f, 0, -0.3f, -0.2f, 0.0f });
                break;
            case 9: // sword
                L(c, 0, -0.5f, 0, 0.36f, 0.14f); P(c, new float[] { 0, 0.55f, -0.09f, 0.34f, 0.09f, 0.34f }); L(c, -0.32f, -0.2f, 0.32f, -0.2f, 0.1f); E(c2, 0, -0.5f, 0.07f, 0.07f);
                break;
            case 10: // owl
                P(c, new float[] { -0.34f, 0.5f, -0.2f, 0.25f, 0.2f, 0.25f, 0.34f, 0.5f, 0.42f, 0.0f, 0.2f, -0.5f, -0.2f, -0.5f, -0.42f, 0.0f });
                E(c2, -0.15f, 0.08f, 0.13f, 0.13f); E(c2, 0.15f, 0.08f, 0.13f, 0.13f); E(G.Hex(0x101010), -0.15f, 0.08f, 0.06f, 0.06f); E(G.Hex(0x101010), 0.15f, 0.08f, 0.06f, 0.06f);
                P(G.Hex(0xe8a020), new float[] { 0, 0.02f, -0.06f, -0.14f, 0.06f, -0.14f });
                break;
            case 11: // flame
                P(c, new float[] { 0, 0.52f, 0.32f, 0.0f, 0.26f, -0.3f, 0, -0.5f, -0.26f, -0.3f, -0.32f, 0.0f });
                P(c2, new float[] { 0, 0.1f, 0.13f, -0.1f, 0, -0.36f, -0.13f, -0.1f });
                break;
            case 12: // gear
                E(c, 0, 0, 0.32f, 0.32f);
                for (int i = 0; i < 8; i++) { float a = i * 45f; var d = G.Dir(a) * 0.38f; var q = Tf(new float[] { d.x, d.y }, x, y, s, rot); p.Rect(c, q[0], q[1], 0.18f * s, 0.18f * s, a + rot); }
                E(c2, 0, 0, 0.14f, 0.14f);
                break;
            case 13: // wave
                for (int k = 0; k < 3; k++)
                    for (int i = 0; i < 8; i++)
                    {
                        float x0 = -0.45f + i * 0.12f, x1 = x0 + 0.12f;
                        L(c, x0, -0.3f + k * 0.28f + Mathf.Sin(x0 * 12f) * 0.09f, x1, -0.3f + k * 0.28f + Mathf.Sin(x1 * 12f) * 0.09f, 0.1f);
                    }
                break;
            default:
                E(c, 0, 0, 0.3f, 0.3f); E(c2, 0, 0, 0.14f, 0.14f);
                break;
        }
    }

    // ================================================================== GROUND VEHICLES
    static float[] HullPoly(int type, float L, float bw, float noseLen)
    {
        float hl = L / 2f;
        switch (type)
        {
            case 1: return new float[] { -hl, -bw, hl - noseLen, -bw, hl, -bw * 0.55f, hl, bw * 0.55f, hl - noseLen, bw, -hl, bw };
            case 2: return new float[] { -hl, -bw, hl - noseLen * 1.3f, -bw, hl - noseLen * 0.6f, -bw * 0.85f, hl, -bw * 0.4f, hl, bw * 0.4f, hl - noseLen * 0.6f, bw * 0.85f, hl - noseLen * 1.3f, bw, -hl, bw };
            case 3: return new float[] { -hl, -bw * 0.92f, -hl + L * 0.2f, -bw, hl - noseLen * 1.2f, -bw, hl, -bw * 0.5f, hl, bw * 0.5f, hl - noseLen * 1.2f, bw, -hl + L * 0.2f, bw, -hl, bw * 0.92f };
            default: return new float[] { -hl, -bw, hl - 0.25f, -bw, hl, -bw + 0.2f, hl, bw - 0.2f, hl - 0.25f, bw, -hl, bw };
        }
    }

    static Model BuildGround(UnitDef d, Pal pal, bool icon)
    {
        VStyle s = d.vs;
        var m = new Model();
        float ppu = icon ? 13f : 26f;
        float margin = 0.4f + (s.dozer ? 1.3f : 0f);
        Pix hull = new Pix(s.L + margin * 2f, s.W + 0.9f, ppu);
        if (s.tracked) DrawTrackedHull(hull, s, pal, d);
        else DrawWheeledHull(hull, s, pal, d, m);
        m.tracked = s.tracked;
        Pix dm = null;
        if (!icon) { dm = MakeDamageOverlay(hull, (int)(s.L * 10f) + 7); }

        Pix tur = null, gun = null;
        if (s.tur > 0)
        {
            tur = new Pix(Mathf.Max(s.tl + 1.4f, 1.8f), Mathf.Max(s.tw + 1.0f, 1.6f), ppu);
            DrawTurret(tur, s, pal, d);
        }
        if (s.gun > 0)
        {
            float gh = s.gun == 5 ? 1.3f : 0.8f;
            gun = new Pix(s.gl + 0.7f, gh, ppu);
            DrawGun(gun, s, pal);
        }
        m.turX = s.tx;
        m.gunLen = s.gun > 0 ? s.gl : 0;
        m.gunPivotX = s.tur == 7 || s.tur == 8 ? 0 : s.tl * 0.12f;
        if (icon)
        {
            float tw = s.L + 1.6f + (s.gun > 0 ? Mathf.Max(0, s.gl + s.tx + m.gunPivotX - s.L / 2f) : 0), th = s.W + 0.9f;
            float shift = (s.gun > 0 ? Mathf.Max(0, s.gl + s.tx + m.gunPivotX - s.L / 2f) : 0) / 2f;
            var ic = new Pix(tw, th, ppu);
            ic.Blit(hull, -shift, 0);
            if (tur != null) ic.Blit(tur, s.tx - shift, 0);
            if (gun != null) ic.Blit(gun, s.tx + m.gunPivotX + gun.HalfW - 0.2f - shift, 0);
            m.icon = ic.Make();
            return m;
        }
        m.hull = hull.Make();
        m.dmg = dm.Make();
        m.icon = null;
        if (tur != null) m.turret = tur.Make();
        if (gun != null) m.gun = gun.MakeAt(-gun.HalfW + 0.2f, 0);
        if (s.tracked)
        {
            float tw = s.trackW;
            m.trackY = s.W / 2f - tw / 2f; m.trackLen = s.L - 0.1f;
            for (int f = 0; f < 4; f++)
            {
                var tp = new Pix(s.L, tw, ppu);
                float sp = 0.24f, off = f * sp / 4f;
                for (float x = -s.L / 2f - sp + off; x < s.L / 2f + sp; x += sp)
                    tp.Rect(new Color(0.42f, 0.42f, 0.38f, 0.8f), x, 0, sp * 0.38f, tw * 0.92f);
                m.trk[f] = tp.Make();
            }
        }
        else
        {
            for (int f = 0; f < 4; f++)
            {
                float wr = m.wheelR;
                var wp = new Pix(wr * 2f, wr * 1.05f, ppu * 1.4f);
                wp.Rect(pal.rubber, 0, 0, wr * 2f, wr * 1.05f);
                wp.Rect(G.Hex(0x303030), 0, 0, wr * 1.9f, wr * 0.9f);
                float sp = wr * 0.5f, off = f * sp / 4f;
                for (float x = -wr - sp + off; x < wr + sp; x += sp)
                    wp.Rect(G.Hex(0x4a4a46), x, 0, sp * 0.4f, wr * 0.95f, 0, true);
                m.whl[f] = wp.Make();
            }
        }
        return m;
    }

    static Pix MakeDamageOverlay(Pix hull, int seed)
    {
        var o = new Pix(hull.w, hull.h, hull.ppu, true);
        var r = new System.Random(seed);
        Func<float, float, float> R = (a, b) => a + (float)r.NextDouble() * (b - a);
        float hw = o.HalfW, hh = o.HalfH;
        for (int i = 0; i < 9; i++) o.Ell(new Color(0.05f, 0.04f, 0.04f, 0.6f), R(-hw * 0.8f, hw * 0.8f), R(-hh * 0.7f, hh * 0.7f), R(0.2f, 0.7f), R(0.15f, 0.45f), R(0, 180), false, 14);
        for (int i = 0; i < 7; i++) o.Circle(new Color(0.01f, 0.01f, 0.01f, 0.9f), R(-hw * 0.8f, hw * 0.8f), R(-hh * 0.7f, hh * 0.7f), R(0.07f, 0.16f));
        for (int i = 0; i < 6; i++) { float x = R(-hw * 0.8f, hw * 0.8f), y = R(-hh * 0.7f, hh * 0.7f); o.Line(new Color(0.9f, 0.45f, 0.1f, 0.5f), x, y, x + R(-0.4f, 0.4f), y + R(-0.3f, 0.3f), 0.03f); }
        o.Mask(hull);
        return o;
    }

    static void DrawTrackedHull(Pix p, VStyle s, Pal pal, UnitDef d)
    {
        float L = s.L, W = s.W, tw = s.trackW, yT = W / 2f - tw / 2f, hl = L / 2f;
        float bw = W / 2f - tw * 0.55f;
        // tracks
        for (int sg = -1; sg <= 1; sg += 2)
        {
            p.Rect(pal.rubber, 0, sg * yT, L, tw);
            p.Circle(pal.rubber, -hl + 0.02f, sg * yT, tw * 0.5f); p.Circle(pal.rubber, hl - 0.02f, sg * yT, tw * 0.5f);
            for (int i = 0; i < s.rw; i++)
            {
                float x = -hl + tw * 0.6f + i * (L - tw * 1.2f) / Mathf.Max(1, s.rw - 1);
                p.Circle(G.Hex(0x3a3b36), x, sg * yT, tw * 0.34f);
                p.Circle(G.Hex(0x1e1f1c), x, sg * yT, tw * 0.14f);
            }
        }
        // hull body
        float[] pts = HullPoly(s.hull, L, bw, Mathf.Min(0.9f, L * 0.2f));
        p.Poly(pal.main, pts);
        Camo(p, pal, 11);
        p.Outline(pal.dark, pts, 0.05f, true);
        // fenders / skirts
        for (int sg = -1; sg <= 1; sg += 2)
        {
            if (s.skirts)
            {
                p.Rect(G.Mul(pal.main, 0.92f), 0, sg * (yT + tw * 0.08f), L - 0.45f, tw * 1.08f);
                p.Rect(pal.dark, 0, sg * (yT + tw * 0.08f), L - 0.45f, tw * 1.08f, 0, true);
                p.Rect(G.Mul(pal.main, 0.95f), 0, sg * (yT + tw * 0.08f), L - 0.5f, tw * 0.95f);
                for (float x = -hl + 0.5f; x < hl - 0.5f; x += 0.55f) p.Line(pal.dark, x, sg * (yT - tw * 0.4f), x, sg * (yT + tw * 0.5f), 0.025f);
            }
            else p.Rect(G.Mul(pal.main, 0.88f), 0, sg * (yT - tw * 0.18f), L - 0.35f, tw * 0.5f);
        }
        // engine deck
        float ex = -hl + 0.15f;
        p.Rect(G.Mul(pal.dark, 0.9f), ex + 0.5f, 0, 1.0f, bw * 1.5f);
        for (int i = 0; i < 5; i++) p.Line(G.Mul(pal.dark, 0.5f), ex + 0.12f + i * 0.2f, -bw * 0.7f, ex + 0.12f + i * 0.2f, bw * 0.7f, 0.03f);
        // front details
        p.Circle(G.Mul(pal.dark, 0.7f), hl - 0.7f, 0, 0.17f);
        p.Circle(pal.metal, hl - 0.7f, 0, 0.08f);
        p.Rect(pal.metal, hl - 0.15f, bw * 0.55f, 0.15f, 0.12f); p.Rect(pal.metal, hl - 0.15f, -bw * 0.55f, 0.15f, 0.12f);
        p.Circle(G.Hex(0xe0dca0), hl - 0.1f, bw * 0.7f, 0.05f); p.Circle(G.Hex(0xe0dca0), hl - 0.1f, -bw * 0.7f, 0.05f);
        if (s.era)
            for (int i = 0; i < 6; i++)
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    p.Rect(G.Mul(pal.sec, 1.15f), hl - 0.9f + (i % 3) * 0.28f, sg * (0.25f + (i / 3) * 0.3f), 0.24f, 0.26f);
                    p.Outline(pal.dark, new float[] { hl - 1.02f + (i % 3) * 0.28f, sg * (0.12f + (i / 3) * 0.3f), hl - 0.78f + (i % 3) * 0.28f, sg * (0.12f + (i / 3) * 0.3f), hl - 0.78f + (i % 3) * 0.28f, sg * (0.38f + (i / 3) * 0.3f), hl - 1.02f + (i % 3) * 0.28f, sg * (0.38f + (i / 3) * 0.3f) }, 0.02f);
                }
        if (s.ramp) { p.Line(pal.dark, -hl + 0.45f, -bw, -hl + 0.45f, bw, 0.05f); p.Rect(G.Mul(pal.dark, 0.85f), -hl + 0.2f, 0, 0.35f, bw * 1.3f); }
        // fuel drums / stowage
        if (!s.ramp && s.hull != 0 && s.tur > 0)
        {
            p.Rect(pal.metal, -hl + 0.3f, bw - 0.28f, 0.6f, 0.26f); p.Rect(pal.metal, -hl + 0.3f, -bw + 0.28f, 0.6f, 0.26f);
            p.Line(G.Hex(0x555a50), -hl + 0.02f, bw - 0.28f, -hl + 0.58f, bw - 0.28f, 0.03f);
        }
        if (s.cage) { for (int i = 0; i < 6; i++) p.Line(G.Hex(0x303330), -hl + 0.1f + i * 0.2f, -bw, -hl + 0.1f + i * 0.2f, bw, 0.03f); p.Outline(G.Hex(0x303330), new float[] { -hl + 0.1f, -bw, -hl + 1.3f, -bw, -hl + 1.3f, bw, -hl + 0.1f, bw }, 0.04f); }
        if (s.dozer)
        {
            p.Line(G.Hex(0x7a6a2a), hl - 0.2f, bw * 0.8f, hl + 0.35f, W / 2f + 0.15f, 0.12f); p.Line(G.Hex(0x7a6a2a), hl - 0.2f, -bw * 0.8f, hl + 0.35f, -W / 2f - 0.15f, 0.12f);
            float[] bl = { hl + 0.2f, -W / 2f - 0.4f, hl + 0.95f, -W / 2f - 0.4f, hl + 0.95f, W / 2f + 0.4f, hl + 0.2f, W / 2f + 0.4f };
            p.Poly(G.Hex(0xc8a72c), bl); p.Outline(G.Hex(0x6a5a1a), bl, 0.05f);
            p.Line(G.Hex(0xe8c84c), hl + 0.9f, -W / 2f - 0.35f, hl + 0.9f, W / 2f + 0.35f, 0.04f);
        }
        // utility bed (crane / equipment) for engineering hulls
        if (s.bed == 5)
        {
            p.Rect(G.Hex(0x8a7a3a), -0.3f, 0, 2.2f, 0.5f); p.Line(G.Hex(0xc8a72c), -0.3f, 0, 1.6f, 0.9f, 0.18f); p.Line(G.Hex(0x40403a), 1.6f, 0.9f, 1.9f, 0.9f, 0.1f);
            p.Rect(pal.dark, -1.2f, 0.7f, 0.9f, 0.7f); p.Rect(pal.dark, -1.2f, -0.7f, 0.9f, 0.7f);
        }
        if (s.bed == 9)
        {
            p.Rect(G.Hex(0x666a60), 0.3f, 0, 5f, 1.8f);
            for (float x = -2f; x < 2.8f; x += 0.35f) p.Line(G.Hex(0x40443c), x, -0.8f, x, 0.8f, 0.04f);
            p.Line(G.Hex(0xc8a72c), -2.1f, -0.85f, 2.9f, -0.85f, 0.08f); p.Line(G.Hex(0xc8a72c), -2.1f, 0.85f, 2.9f, 0.85f, 0.08f);
        }
        // faction + brigade marks on rear deck
        FacMark(p, pal, -hl + 0.62f, bw - 0.4f, 0.55f, 0.28f);
        for (int i = 0; i < pal.bars; i++) p.Rect(pal.accent, hl - 0.4f - i * 0.12f, -bw + 0.28f, 0.06f, 0.26f);
        if (s.tur == 0) Emblem(p, pal.emblem, 0.2f, 0, 0.8f, pal.accent, pal.dark, 0);
        else Emblem(p, pal.emblem, -hl + 1.5f, 0, 0.45f, pal.accent, pal.dark, 0);
        p.Rect(new Color(1, 1, 1, 0.12f), 0, bw * 0.6f, L - 0.3f, 0.05f, 0, true);
    }

    static void DrawWheeledHull(Pix p, VStyle s, Pal pal, UnitDef d, Model m)
    {
        float L = s.L, W = s.W, hl = L / 2f;
        bool arm = d.cat == Cat.APC || d.cat == Cat.Armored;
        bool towed = s.bed >= 20;
        // wheels layout
        int ax = s.ax > 0 ? s.ax : (towed ? 1 : Mathf.Max(2, s.rw / 2));
        float wr = arm ? 0.52f : (d.cat == Cat.Cars ? 0.36f : (L > 7f ? 0.5f : 0.44f));
        if (towed) wr = 0.42f;
        m.wheelR = wr;
        var wp = new List<Vector2>();
        float front = hl - Mathf.Max(0.9f, L * 0.14f), rear = -hl + Mathf.Max(0.9f, L * 0.13f);
        if (s.cab >= 1 && !arm && d.cat == Cat.Trucks) front = hl - s.cabL * 0.55f;
        if (towed) { front = rear = (s.bed == 21 ? 0f : 0.6f); }
        for (int i = 0; i < ax; i++)
        {
            float x = ax == 1 ? front : Mathf.Lerp(front, rear, i / (float)(ax - 1));
            if (towed && s.bed == 21) continue;
            wp.Add(new Vector2(x, W / 2f - wr * 0.28f)); wp.Add(new Vector2(x, -W / 2f + wr * 0.28f));
        }
        m.wheelPos = wp.ToArray();
        Color chas = G.Mul(pal.dark, 0.7f);
        float bodyW = W - wr * 0.7f;
        if (towed)
        {
            // carriage: split trails + shield mount
            if (s.bed == 20)
            {
                p.Line(pal.metal, 0.2f, 0.25f, -hl + 0.1f, 0.95f, 0.14f); p.Line(pal.metal, 0.2f, -0.25f, -hl + 0.1f, -0.95f, 0.14f);
                p.Rect(pal.main, 0.3f, 0, 0.9f, 1.1f); p.Rect(pal.dark, -hl + 0.15f, 0.95f, 0.3f, 0.14f); p.Rect(pal.dark, -hl + 0.15f, -0.95f, 0.3f, 0.14f);
                p.Line(pal.metal, 0.6f, -0.85f, 0.6f, 0.85f, 0.12f);
                Camo(p, pal, 5);
            }
            else if (s.bed == 21)
            {
                p.Circle(pal.metal, 0, 0, 0.55f); p.Circle(G.Mul(pal.metal, 1.4f), 0, 0, 0.4f);
                p.Line(pal.metal, 0.1f, 0, -0.7f, 0.65f, 0.08f); p.Line(pal.metal, 0.1f, 0, -0.7f, -0.65f, 0.08f);
                p.Rect(pal.dark, -0.55f, 0.1f, 0.35f, 0.25f); p.Rect(pal.accent, -0.55f, -0.2f, 0.25f, 0.18f);
            }
            else // 22 AA twin gun carriage
            {
                p.Line(pal.metal, 0, 0, hl, 0.9f, 0.12f); p.Line(pal.metal, 0, 0, hl, -0.9f, 0.12f); p.Line(pal.metal, 0, 0, -hl, 0, 0.14f);
                p.Circle(pal.main, 0, 0, 0.55f); p.Circle(pal.dark, 0, 0, 0.35f);
                p.Rect(G.Hex(0x3a3d38), 0.2f, 0, 0.4f, 1.0f);
            }
            FacMark(p, pal, 0.2f, 0.0f, 0.4f, 0.25f);
            return;
        }
        if (arm)
        {
            float nose = Mathf.Min(1.1f, L * 0.22f);
            float[] pts = HullPoly(s.hull == 3 ? 3 : (s.hull == 1 ? 1 : 2), L - 0.1f, bodyW / 2f, nose);
            // chassis + body
            p.Poly(G.Hex(0x1a1a18), HullPoly(0, L - 0.3f, bodyW / 2f - 0.15f, 0.2f));
            p.Poly(pal.main, pts);
            Camo(p, pal, 21);
            p.Outline(pal.dark, pts, 0.05f, true);
            // windshield + side windows
            p.Poly(pal.glass, new float[] { hl - nose * 0.9f, -bodyW * 0.36f, hl - nose * 0.45f, -bodyW * 0.28f, hl - nose * 0.45f, bodyW * 0.28f, hl - nose * 0.9f, bodyW * 0.36f });
            p.Rect(G.Mul(pal.dark, 0.9f), -hl + 0.8f, 0, 1.2f, bodyW * 0.7f);
            for (int i = 0; i < 4; i++) p.Line(G.Mul(pal.dark, 0.5f), -hl + 0.3f + i * 0.18f, -bodyW * 0.3f, -hl + 0.3f + i * 0.18f, bodyW * 0.3f, 0.025f);
            // roof hatches
            p.Circle(G.Mul(pal.dark, 0.75f), hl * 0.2f - 0.1f, bodyW * 0.22f, 0.2f); p.Circle(G.Mul(pal.dark, 0.75f), -hl * 0.3f, -bodyW * 0.22f, 0.2f);
            p.Circle(G.Mul(pal.dark, 0.75f), -hl * 0.3f, bodyW * 0.22f, 0.2f);
            if (s.skirts) { p.Rect(G.Mul(pal.main, 0.85f), 0.1f, bodyW / 2f - 0.05f, L - 1.7f, 0.18f); p.Rect(G.Mul(pal.main, 0.85f), 0.1f, -bodyW / 2f + 0.05f, L - 1.7f, 0.18f); }
            p.Rect(pal.metal, -hl + 0.12f, 0, 0.12f, 0.5f);
            if (d.cat == Cat.APC && s.tur == 0) { p.Rect(G.Hex(0x303030), 0, 0, 1.4f, 0.14f); }
            FacMark(p, pal, -hl + 0.55f, bodyW / 2f - 0.35f, 0.5f, 0.25f);
            for (int i = 0; i < pal.bars; i++) p.Rect(pal.accent, hl - 0.5f - i * 0.12f, bodyW / 2f - 0.3f, 0.06f, 0.24f);
            Emblem(p, pal.emblem, -0.1f, 0, 0.55f, pal.accent, pal.dark, 0);
            return;
        }
        // ----- cars and trucks: cab + chassis + bed
        p.Rect(chas, 0, 0, L - 0.2f, bodyW * 0.62f);
        if (d.cat == Cat.Cars)
        {
            float cl = s.cab == 3 ? 1.9f : L - 0.4f;
            float[] body = HullPoly(2, L, bodyW / 2f, 0.9f);
            if (s.cab == 3) body = HullPoly(2, 2.2f, bodyW / 2f, 0.7f);
            float ox = s.cab == 3 ? hl - 1.1f : 0;
            if (s.cab == 3) { for (int i = 0; i < body.Length; i += 2) body[i] += ox; }
            p.Poly(pal.main, body); Camo(p, pal, 3); p.Outline(pal.dark, body, 0.04f, true);
            // glass
            float gx = s.cab == 3 ? ox + 0.1f : hl * 0.1f;
            p.Poly(pal.glass, new float[] { gx + 0.5f, -bodyW * 0.35f, gx + 0.15f, -bodyW * 0.42f, gx - 0.55f, -bodyW * 0.42f, gx - 0.55f, bodyW * 0.42f, gx + 0.15f, bodyW * 0.42f, gx + 0.5f, bodyW * 0.35f });
            p.Rect(pal.main, gx - 0.15f, 0, 0.7f, bodyW * 0.7f);
            p.Line(G.Mul(pal.dark, 0.6f), gx - 0.5f, 0, gx + 0.1f, 0, 0.03f);
            if (s.bed == 11) { p.Rect(G.Mul(pal.dark, 0.8f), -hl + 1.0f, 0, 1.9f, bodyW * 0.78f); p.Rect(G.Hex(0x6a5a3a), -hl + 0.8f, 0.2f, 0.6f, 0.45f); p.Rect(G.Hex(0x4a5a3a), -hl + 1.4f, -0.2f, 0.5f, 0.4f); }
            if (s.bed == 12) { p.Rect(G.Hex(0xeeeeea), -hl + 1.1f, 0, 2.1f, bodyW * 0.84f); p.Rect(G.Hex(0xcc2222), -hl + 1.1f, 0, 0.8f, 0.2f); p.Rect(G.Hex(0xcc2222), -hl + 1.1f, 0, 0.2f, 0.8f); p.Rect(G.Hex(0xff8800), hl * 0.1f, 0.32f, 0.12f, 0.1f); }
            if (s.bed == 13) { p.Line(G.Hex(0x222222), -hl + 0.3f, 0.3f, -hl - 0.0f, 1.0f, 0.03f); p.Circle(G.Hex(0x333333), -hl + 0.7f, 0, 0.25f); }
            if (s.bed == 10) { p.Rect(G.Mul(pal.main, 0.9f), -hl + 0.9f, 0, 1.5f, bodyW * 0.8f); p.Line(G.Mul(pal.dark, 0.5f), -hl + 0.2f, 0, -hl + 1.6f, 0, 0.03f); p.Rect(G.Hex(0x303030), -hl + 0.9f, 0.45f, 1.2f, 0.05f); }
            FacMark(p, pal, hl * 0.1f - 0.1f, bodyW * 0.28f, 0.45f, 0.22f);
            Emblem(p, pal.emblem, -hl + 1.0f, 0, 0.45f, pal.accent, pal.dark, 0);
            return;
        }
        // trucks / launchers
        float cabL = s.cabL;
        float bedL = L - cabL - 0.15f;
        float bedX = -hl + 0.1f + bedL / 2f;
        float cabX = hl - cabL / 2f;
        // bed
        switch (s.bed)
        {
            case 0: // canvas cargo
                p.Rect(G.Mul(pal.main, 0.95f), bedX, 0, bedL, bodyW * 0.95f); CamoDefault(p, pal);
                for (float x = -hl + 0.3f; x < -hl + bedL; x += 0.55f) p.Line(pal.dark, x, -bodyW * 0.45f, x, bodyW * 0.45f, 0.03f);
                p.Line(pal.dark, bedX - bedL / 2f, 0, bedX + bedL / 2f, 0, 0.04f); break;
            case 1: // flatbed with crates
                p.Rect(G.Hex(0x4a4c42), bedX, 0, bedL, bodyW * 0.95f);
                p.Rect(G.Hex(0x7a6a44), bedX - bedL * 0.25f, 0.4f, bedL * 0.4f, 0.8f); p.Rect(G.Hex(0x5a6a44), bedX + bedL * 0.2f, -0.4f, bedL * 0.4f, 0.8f); p.Rect(G.Hex(0x6a5a3a), bedX + bedL * 0.25f, 0.5f, bedL * 0.2f, 0.6f); break;
            case 2: // tanker
                p.Rect(G.Hex(0x2f3028), bedX, 0, bedL, bodyW * 0.5f);
                p.Ell(G.Mul(pal.main, 1.0f), bedX, 0, bedL * 0.5f, bodyW * 0.48f); p.Ell(G.Hex(0xffffff, 0.15f), bedX, 0.35f, bedL * 0.42f, 0.15f);
                p.Circle(pal.metal, bedX, 0, 0.2f); p.Rect(G.Hex(0xcc2020), bedX - bedL * 0.3f, 0, 0.35f, 0.35f);
                p.Rect(G.Hex(0xe8c020), bedX + bedL * 0.3f, 0, 0.1f, bodyW * 0.8f); break;
            case 3: // comms shelter
                p.Rect(G.Mul(pal.main, 1.1f), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Mul(pal.dark, 1.0f), bedX, 0, bedL - 0.2f, bodyW * 0.8f, 0, true);
                p.Circle(G.Hex(0xc0c4c0), bedX - 0.6f, 0, 0.6f); p.Circle(G.Hex(0x909490), bedX - 0.6f, 0, 0.35f); p.Line(G.Hex(0x202020), bedX - 0.6f, 0, bedX - 0.1f, 0.5f, 0.05f);
                p.Line(G.Hex(0x222222), bedX + 1.2f, 0.8f, bedX + 1.2f, -0.8f, 0.04f); p.Circle(G.Hex(0x333333), bedX + 1.2f, 0.8f, 0.07f); p.Circle(G.Hex(0x333333), bedX + 1.2f, -0.8f, 0.07f); break;
            case 4: // ambulance
                p.Rect(G.Hex(0xeeeeea), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Hex(0xcc2222), bedX, 0, 1.2f, 0.28f); p.Rect(G.Hex(0xcc2222), bedX, 0, 0.28f, 1.2f);
                p.Rect(G.Hex(0xff9900), bedX + bedL * 0.42f, 0.5f, 0.12f, 0.12f); p.Rect(G.Hex(0x2060ff), bedX + bedL * 0.42f, -0.5f, 0.12f, 0.12f); break;
            case 5: // repair crane
                p.Rect(G.Mul(pal.main, 0.9f), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Hex(0x8a7a3a), bedX + 0.2f, 0.6f, bedL * 0.5f, 0.5f);
                p.Line(G.Hex(0xc8a72c), bedX + bedL * 0.35f, 0, bedX - bedL * 0.4f, 0.0f, 0.2f); p.Circle(G.Hex(0x303030), bedX + bedL * 0.35f, 0, 0.3f);
                p.Rect(G.Hex(0x444444), bedX - bedL * 0.35f, -0.65f, 0.9f, 0.7f); break;
            case 6: // supply with pallets
                p.Rect(G.Hex(0x45473e), bedX, 0, bedL, bodyW * 0.95f);
                for (int i = 0; i < 4; i++) for (int j = 0; j < 2; j++) p.Rect(i % 2 == 0 ? G.Hex(0x6c7a48) : G.Hex(0x8a7a50), bedX - bedL * 0.38f + i * bedL * 0.25f, (j - 0.5f) * 0.9f, bedL * 0.22f, 0.8f);
                p.Rect(pal.accent, bedX, 0, 0.12f, 0.12f); break;
            case 7: // troop bench
                p.Rect(G.Hex(0x3e4036), bedX, 0, bedL, bodyW * 0.95f);
                p.Line(G.Hex(0x5a4a30), bedX - bedL * 0.45f, 0.55f, bedX + bedL * 0.45f, 0.55f, 0.18f); p.Line(G.Hex(0x5a4a30), bedX - bedL * 0.45f, -0.55f, bedX + bedL * 0.45f, -0.55f, 0.18f);
                for (float x = bedX - bedL * 0.4f; x < bedX + bedL * 0.45f; x += 0.7f) { p.Circle(pal.helmet, x, 0.35f, 0.14f); p.Circle(pal.helmet, x, -0.35f, 0.14f); }
                break;
            case 8: // box transport
                p.Rect(G.Mul(pal.main, 1.05f), bedX, 0, bedL, bodyW * 0.95f); p.Line(pal.dark, bedX, -bodyW * 0.45f, bedX, bodyW * 0.45f, 0.05f); p.Rect(pal.accent, bedX, 0, 0.5f, 0.12f); break;
            case 14: // CAESAR platform
                p.Rect(G.Hex(0x3a3c34), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Mul(pal.main, 0.9f), bedX - 0.9f, 0, 1.2f, bodyW * 0.7f);
                p.Rect(pal.metal, -hl + 0.15f, 0.9f, 0.3f, 0.3f); p.Rect(pal.metal, -hl + 0.15f, -0.9f, 0.3f, 0.3f); break;
            case 15: // MLRS chassis
                p.Rect(G.Hex(0x30322c), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Mul(pal.dark, 1.1f), bedX, 0, bedL - 0.2f, bodyW * 0.45f);
                p.Rect(pal.metal, -hl + 0.2f, 0.95f, 0.35f, 0.35f); p.Rect(pal.metal, -hl + 0.2f, -0.95f, 0.35f, 0.35f); break;
            case 16: // SAM chassis
                p.Rect(G.Hex(0x30322c), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Mul(pal.main, 0.85f), bedX, 0, bedL - 0.2f, bodyW * 0.5f);
                p.Rect(pal.metal, -hl + 0.2f, 0.95f, 0.35f, 0.35f); p.Rect(pal.metal, -hl + 0.2f, -0.95f, 0.35f, 0.35f);
                p.Circle(G.Hex(0x9aa09a), bedX - bedL * 0.3f, 0, 0.25f); break;
            default:
                p.Rect(G.Hex(0x30322c), bedX, 0, bedL, bodyW * 0.95f); p.Rect(G.Mul(pal.main, 0.9f), bedX, 0, bedL - 0.2f, bodyW * 0.6f); break;
        }
        // cab
        float[] cab = HullPoly(s.cab == 1 ? 2 : 0, cabL, bodyW / 2f - 0.05f, 0.5f);
        for (int i = 0; i < cab.Length; i += 2) cab[i] += cabX;
        p.Poly(pal.main, cab); p.Outline(pal.dark, cab, 0.04f, true);
        p.Poly(pal.glass, new float[] { cabX + cabL * 0.36f, -bodyW * 0.38f, cabX + cabL * 0.16f, -bodyW * 0.38f, cabX + cabL * 0.16f, bodyW * 0.38f, cabX + cabL * 0.36f, bodyW * 0.38f });
        p.Rect(G.Mul(pal.dark, 0.8f), cabX - cabL * 0.18f, 0, cabL * 0.38f, bodyW * 0.7f);
        p.Rect(pal.metal, hl - 0.06f, 0, 0.1f, bodyW * 0.55f);
        p.Circle(G.Hex(0xe0dca0), hl - 0.1f, bodyW * 0.4f, 0.06f); p.Circle(G.Hex(0xe0dca0), hl - 0.1f, -bodyW * 0.4f, 0.06f);
        FacMark(p, pal, cabX - 0.05f, bodyW / 2f - 0.2f, 0.4f, 0.2f);
        for (int i = 0; i < pal.bars; i++) p.Rect(pal.accent, hl - 0.3f - i * 0.1f, -bodyW / 2f + 0.25f, 0.05f, 0.2f);
        Emblem(p, pal.emblem, cabX - 0.35f, 0, 0.5f, pal.accent, pal.dark, 0);
    }

    static void CamoDefault(Pix p, Pal pal) { Camo(p, pal, 17); }

    static void DrawTurret(Pix p, VStyle s, Pal pal, UnitDef d)
    {
        float tl = s.tl, tw = s.tw;
        Color mc = pal.main, dk = pal.dark, md = pal.metal;
        switch (s.tur)
        {
            case 1: // cast round turret
                {
                    if (s.bustle) p.Rect(G.Mul(mc, 0.92f), -tl * 0.5f, 0, 0.5f, tw * 0.7f);
                    p.Ell(mc, 0.05f, 0, tl * 0.5f, tw * 0.5f);
                    p.Poly(mc, new float[] { tl * 0.2f, tw * 0.4f, tl * 0.5f, tw * 0.14f, tl * 0.5f, -tw * 0.14f, tl * 0.2f, -tw * 0.4f });
                    Camo(p, pal, 31, 0.8f);
                    p.Ell(dk, 0.05f, 0, tl * 0.5f, tw * 0.5f, 0, true, 26);
                    p.Ell(mc, 0.05f, 0, tl * 0.5f - 0.04f, tw * 0.5f - 0.04f);
                    Camo(p, pal, 31, 0.8f);
                    p.Rect(dk, tl * 0.45f, 0, 0.22f, tw * 0.42f);
                    p.Circle(G.Mul(dk, 0.8f), -tl * 0.12f, tw * 0.28f, 0.2f); p.Circle(md, -tl * 0.12f, tw * 0.28f, 0.1f);
                    p.Circle(G.Mul(dk, 0.8f), -tl * 0.1f, -tw * 0.26f, 0.18f);
                    p.Rect(G.Hex(0x303a34), tl * 0.22f, -tw * 0.28f, 0.28f, 0.2f);
                    for (int i = 0; i < 3; i++) { p.Rect(md, tl * 0.3f - i * 0.1f, tw * 0.47f, 0.07f, 0.12f); p.Rect(md, tl * 0.3f - i * 0.1f, -tw * 0.47f, 0.07f, 0.12f); }
                    if (s.era) for (int i = 0; i < 5; i++) for (int sg = -1; sg <= 1; sg += 2) p.Rect(G.Mul(pal.sec, 1.2f), tl * 0.38f - i * 0.2f, sg * (tw * 0.34f + i * 0.03f), 0.17f, 0.18f, sg * 18f);
                    if (s.cage) { p.Outline(G.Hex(0x2c302c), new float[] { -tl * 0.52f, -tw * 0.45f, -tl * 0.1f, -tw * 0.5f, -tl * 0.1f, tw * 0.5f, -tl * 0.52f, tw * 0.45f }, 0.04f); for (int i = 0; i < 5; i++) p.Line(G.Hex(0x2c302c), -tl * 0.5f + i * 0.17f, -tw * 0.47f, -tl * 0.5f + i * 0.17f, tw * 0.47f, 0.025f); }
                    p.Line(G.Hex(0x15150f), tl * 0.1f, -tw * 0.5f, tl * 0.1f, -tw * 0.5f - 0.0f, 0.03f);
                    break;
                }
            case 2: // angular welded turret
                {
                    float[] pts = { -tl * 0.5f, -tw * 0.45f, tl * 0.2f, -tw * 0.5f, tl * 0.5f, -tw * 0.26f, tl * 0.5f, tw * 0.26f, tl * 0.2f, tw * 0.5f, -tl * 0.5f, tw * 0.45f };
                    p.Poly(mc, pts); Camo(p, pal, 33, 0.8f); p.Outline(dk, pts, 0.05f, true);
                    p.Poly(G.Mul(mc, 0.82f), new float[] { tl * 0.2f, -tw * 0.5f, tl * 0.5f, -tw * 0.26f, tl * 0.5f, -tw * 0.14f, tl * 0.2f, -tw * 0.36f });
                    p.Poly(G.Mul(mc, 0.82f), new float[] { tl * 0.2f, tw * 0.5f, tl * 0.5f, tw * 0.26f, tl * 0.5f, tw * 0.14f, tl * 0.2f, tw * 0.36f });
                    if (s.bustle) { p.Rect(G.Mul(mc, 0.9f), -tl * 0.55f, 0, 0.5f, tw * 0.75f); p.Outline(dk, new float[] { -tl * 0.55f - 0.25f, -tw * 0.375f, -tl * 0.55f + 0.25f, -tw * 0.375f, -tl * 0.55f + 0.25f, tw * 0.375f, -tl * 0.55f - 0.25f, tw * 0.375f }, 0.04f); }
                    p.Circle(G.Mul(dk, 0.75f), -tl * 0.1f, tw * 0.22f, 0.19f); p.Circle(md, -tl * 0.1f, tw * 0.22f, 0.09f);
                    p.Rect(G.Mul(dk, 0.75f), -tl * 0.05f, -tw * 0.2f, 0.4f, 0.32f);
                    p.Rect(G.Hex(0x303a34), tl * 0.35f, tw * 0.3f, 0.2f, 0.16f);
                    if (s.era) for (int i = 0; i < 4; i++) for (int sg = -1; sg <= 1; sg += 2) p.Rect(G.Mul(pal.sec, 1.2f), tl * 0.2f + i * 0.0f, sg * (tw * 0.4f - i * 0.0f) , 0.01f, 0.01f);
                    if (s.era) for (int i = 0; i < 4; i++) for (int sg = -1; sg <= 1; sg += 2) p.Rect(G.Mul(pal.sec, 1.2f), tl * 0.38f - i * 0.0f, sg * (0.18f + i * 0.14f), 0.2f, 0.12f);
                    p.Rect(dk, tl * 0.5f - 0.1f, 0, 0.2f, tw * 0.3f);
                    break;
                }
            case 3: // flat unmanned slab
                {
                    float[] pts = { -tl * 0.5f, -tw * 0.4f, -tl * 0.3f, -tw * 0.5f, tl * 0.3f, -tw * 0.5f, tl * 0.5f, -tw * 0.3f, tl * 0.5f, tw * 0.3f, tl * 0.3f, tw * 0.5f, -tl * 0.3f, tw * 0.5f, -tl * 0.5f, tw * 0.4f };
                    p.Poly(mc, pts); Camo(p, pal, 35, 0.8f); p.Outline(dk, pts, 0.05f, true);
                    p.Rect(G.Mul(dk, 0.7f), tl * 0.1f, 0, tl * 0.5f, 0.14f);
                    p.Circle(md, -tl * 0.2f, tw * 0.3f, 0.2f); p.Circle(G.Hex(0x3a4a44), -tl * 0.2f, tw * 0.3f, 0.1f);
                    p.Rect(G.Hex(0x2c3a36), tl * 0.35f, -tw * 0.3f, 0.3f, 0.18f); p.Rect(G.Hex(0x2c3a36), tl * 0.35f, tw * 0.3f, 0.3f, 0.18f);
                    for (int i = 0; i < 4; i++) p.Line(G.Mul(dk, 0.8f), -tl * 0.4f + i * 0.4f, -tw * 0.4f, -tl * 0.4f + i * 0.4f, tw * 0.4f, 0.02f);
                    p.Line(G.Hex(0x222222), -tl * 0.45f, -tw * 0.35f, -tl * 0.45f - 0.5f, -tw * 0.7f, 0.025f);
                    break;
                }
            case 4: // boxy turret with large bustle
                {
                    float[] pts = { -tl * 0.5f, -tw * 0.48f, tl * 0.15f, -tw * 0.5f, tl * 0.5f, -tw * 0.36f, tl * 0.5f, tw * 0.36f, tl * 0.15f, tw * 0.5f, -tl * 0.5f, tw * 0.48f };
                    p.Poly(mc, pts); Camo(p, pal, 37, 0.8f);
                    p.Rect(G.Mul(mc, 0.93f), -tl * 0.38f, 0, tl * 0.3f, tw * 0.96f);
                    p.Outline(dk, pts, 0.05f, true);
                    p.Poly(G.Mul(mc, 0.8f), new float[] { tl * 0.15f, -tw * 0.5f, tl * 0.5f, -tw * 0.36f, tl * 0.5f, -tw * 0.2f, tl * 0.15f, -tw * 0.34f });
                    p.Poly(G.Mul(mc, 0.8f), new float[] { tl * 0.15f, tw * 0.5f, tl * 0.5f, tw * 0.36f, tl * 0.5f, tw * 0.2f, tl * 0.15f, tw * 0.34f });
                    p.Rect(dk, -tl * 0.5f + 0.08f, 0, 0.1f, tw * 0.8f);
                    p.Circle(G.Mul(dk, 0.75f), tl * 0.0f, tw * 0.28f, 0.2f); p.Circle(md, 0, tw * 0.28f, 0.1f); p.Rect(md, 0.1f, tw * 0.28f, 0.3f, 0.07f);
                    p.Rect(G.Mul(dk, 0.8f), -tl * 0.05f, -tw * 0.22f, 0.45f, 0.34f); p.Rect(G.Hex(0x303a34), tl * 0.1f, -tw * 0.22f, 0.2f, 0.15f);
                    if (s.cage) for (int i = 0; i < 6; i++) p.Line(G.Hex(0x2c302c), -tl * 0.5f + i * 0.2f, -tw * 0.5f, -tl * 0.5f + i * 0.2f, tw * 0.5f, 0.02f);
                    for (int i = 0; i < 3; i++) { p.Rect(md, tl * 0.3f - i * 0.08f, tw * 0.52f, 0.07f, 0.12f); p.Rect(md, tl * 0.3f - i * 0.08f, -tw * 0.52f, 0.07f, 0.12f); }
                    break;
                }
            case 5: // small remote weapon station
                {
                    p.Ell(mc, 0, 0, tl * 0.5f, tw * 0.5f); Camo(p, pal, 39, 0.5f);
                    p.Ell(dk, 0, 0, tl * 0.5f, tw * 0.5f, 0, true); p.Ell(mc, 0, 0, tl * 0.5f - 0.04f, tw * 0.5f - 0.04f);
                    p.Rect(G.Hex(0x2a3a36), tl * 0.25f, 0, 0.18f, tw * 0.5f); p.Circle(md, -tl * 0.1f, 0, 0.1f);
                    if (d.cat == Cat.IFV || d.cat == Cat.APC) p.Rect(G.Mul(pal.main, 0.85f), -tl * 0.2f, 0, tl * 0.3f, tw * 0.9f);
                    break;
                }
            case 6: // open shield turret (SPG / AA)
                {
                    float[] pts = { -tl * 0.5f, -tw * 0.5f, tl * 0.12f, -tw * 0.5f, tl * 0.5f, -tw * 0.28f, tl * 0.5f, tw * 0.28f, tl * 0.12f, tw * 0.5f, -tl * 0.5f, tw * 0.5f };
                    p.Poly(G.Hex(0x30332d), pts); // floor
                    p.Poly(mc, new float[] { tl * 0.0f, -tw * 0.5f, tl * 0.5f, -tw * 0.28f, tl * 0.5f, tw * 0.28f, tl * 0.0f, tw * 0.5f, tl * 0.1f, tw * 0.3f, tl * 0.32f, tw * 0.18f, tl * 0.32f, -tw * 0.18f, tl * 0.1f, -tw * 0.3f });
                    p.Outline(mc, pts, 0.16f); p.Outline(dk, pts, 0.04f, true);
                    Camo(p, pal, 41, 0.8f);
                    p.Rect(G.Mul(dk, 0.6f), -tl * 0.25f, 0, 0.5f, tw * 0.5f); p.Rect(G.Hex(0x5a5a48), -tl * 0.28f, tw * 0.22f, 0.3f, 0.22f); p.Rect(G.Hex(0x5a5a48), -tl * 0.28f, -tw * 0.22f, 0.3f, 0.22f);
                    p.Circle(md, -tl * 0.05f, -tw * 0.3f, 0.12f);
                    if (s.bustle) p.Rect(G.Mul(mc, 0.9f), -tl * 0.5f - 0.1f, 0, 0.3f, tw * 0.8f);
                    break;
                }
            case 7: // rocket rack
                {
                    float hw2 = tw * 0.5f;
                    p.Rect(G.Hex(0x2d2f29), 0, 0, tl * 0.96f, tw * 1.02f);
                    int rows = Mathf.Max(3, Mathf.RoundToInt(tw / 0.3f));
                    for (int r = 0; r < rows; r++)
                    {
                        float y = -hw2 + 0.15f + r * (tw - 0.3f) / Mathf.Max(1, rows - 1);
                        p.Line(G.Hex(0x4a4e44), -tl * 0.47f, y, tl * 0.47f, y, 0.26f);
                        p.Line(G.Hex(0x6a6e62), -tl * 0.47f, y + 0.05f, tl * 0.47f, y + 0.05f, 0.05f);
                        p.Circle(G.Hex(0x0e0e0c), tl * 0.47f, y, 0.1f);
                    }
                    p.Rect(pal.dark, -tl * 0.3f, 0, 0.15f, tw);
                    p.Rect(pal.main, -tl * 0.05f, 0, 0.12f, tw); p.Rect(pal.main, tl * 0.25f, 0, 0.12f, tw);
                    if (d.id == "ml_tos1") { p.Rect(pal.accent, -tl * 0.4f, 0, 0.08f, tw * 0.9f); }
                    break;
                }
            case 8: // SAM launcher / radar
                {
                    int tubes = d.id == "ad_s300" || d.id == "ad_nasams" || d.id == "ad_iris" || d.id == "ad_buk" ? 4 : (d.id == "ad_pantsir" ? 6 : 4);
                    p.Rect(G.Hex(0x30332d), 0, 0, tl * 0.9f, tw * 0.95f);
                    if (d.id == "ad_buk" || d.id == "ad_tor" || d.id == "ad_s300" || d.id == "ad_nasams" || d.id == "ad_iris")
                    {
                        // canister launchers
                        int n = d.id == "ad_tor" ? 4 : (d.id == "ad_buk" ? 4 : 4);
                        for (int i = 0; i < n; i++)
                        {
                            float y = -tw * 0.38f + i * (tw * 0.76f / (n - 1));
                            p.Line(G.Hex(0xb5b9ad), -tl * 0.4f, y, tl * 0.4f, y, 0.32f);
                            p.Line(G.Hex(0xe0e4da), -tl * 0.4f, y + 0.07f, tl * 0.4f, y + 0.07f, 0.06f);
                            p.Poly(G.Hex(0xe8e8e0), new float[] { tl * 0.4f, y - 0.14f, tl * 0.52f, y, tl * 0.4f, y + 0.14f });
                            p.Rect(pal.dark, -tl * 0.1f, y, 0.1f, 0.34f);
                        }
                        if (d.id == "ad_tor" || d.id == "ad_buk") { p.Circle(G.Hex(0x80868a), -tl * 0.3f, 0, 0.28f); p.Circle(G.Hex(0x50565a), -tl * 0.3f, 0, 0.14f); }
                    }
                    else // short rails
                    {
                        for (int i = 0; i < 2; i++) for (int sg = -1; sg <= 1; sg += 2)
                        {
                            float y = sg * (tw * 0.28f + i * 0.0f);
                            p.Line(G.Hex(0xb5b9ad), -tl * 0.25f, y, tl * 0.4f, y, 0.28f);
                            p.Poly(G.Hex(0xe8e8e0), new float[] { tl * 0.4f, y - 0.12f, tl * 0.5f, y, tl * 0.4f, y + 0.12f });
                        }
                        p.Circle(G.Hex(0x80868a), -tl * 0.35f, 0, 0.32f); p.Circle(G.Hex(0x50565a), -tl * 0.35f, 0, 0.14f);
                    }
                    p.Rect(pal.main, -tl * 0.05f, 0, 0.1f, tw);
                    break;
                }
            case 9: // AA gun turret with radar
                {
                    float[] pts = { -tl * 0.5f, -tw * 0.45f, tl * 0.2f, -tw * 0.5f, tl * 0.5f, -tw * 0.4f, tl * 0.5f, tw * 0.4f, tl * 0.2f, tw * 0.5f, -tl * 0.5f, tw * 0.45f };
                    p.Poly(mc, pts); Camo(p, pal, 43, 0.8f); p.Outline(dk, pts, 0.05f, true);
                    p.Circle(G.Hex(0x9aa09a), -tl * 0.28f, 0, 0.5f); p.Circle(G.Hex(0x6a706a), -tl * 0.28f, 0, 0.3f); p.Circle(G.Hex(0x303430), -tl * 0.28f, 0, 0.08f);
                    p.Line(G.Hex(0x303430), -tl * 0.28f, 0, -tl * 0.05f, 0, 0.05f);
                    p.Rect(G.Hex(0x30382f), tl * 0.38f, 0, 0.2f, tw * 0.5f);
                    p.Rect(G.Hex(0x2d3a40), tl * 0.1f, tw * 0.3f, 0.3f, 0.22f); p.Rect(G.Hex(0x2d3a40), tl * 0.1f, -tw * 0.3f, 0.3f, 0.22f);
                    break;
                }
            default:
                p.Circle(mc, 0, 0, 0.4f);
                break;
        }
        // faction + emblem on turret top for tank-like turrets
        if (s.tur == 1 || s.tur == 2 || s.tur == 3 || s.tur == 4)
        {
            Emblem(p, pal.emblem, -tl * 0.28f, 0, Mathf.Min(0.55f, tw * 0.35f), pal.accent, pal.dark, 0);
            FacMark(p, pal, tl * 0.05f, -tw * 0.1f, 0.5f, 0.2f);
        }
        else if (s.tur == 6 || s.tur == 9) Emblem(p, pal.emblem, tl * 0.1f, 0, 0.45f, pal.accent, pal.dark, 0);
    }

    static void DrawGun(Pix p, VStyle s, Pal pal)
    {
        float x0 = -p.HalfW + 0.2f;   // breech position
        float gl = s.gl, gt = s.gt;
        Color bc = G.Hex(0x30322d), hi = G.Hex(0x585c52), dk = G.Hex(0x1c1d19);
        switch (s.gun)
        {
            case 1: // tank gun
                p.Rect(bc, x0 + gl * 0.5f, 0, gl, gt);
                p.Rect(G.Mul(bc, 1.25f), x0 + gl * 0.2f, 0, gl * 0.4f, gt * 1.5f);
                p.Rect(hi, x0 + gl * 0.5f, gt * 0.25f, gl, gt * 0.18f);
                p.Rect(bc, x0 + gl * 0.62f, 0, gl * 0.14f, gt * 1.45f);   // fume extractor
                p.Rect(dk, x0 + gl - 0.02f, 0, 0.1f, gt * 1.25f);
                break;
            case 2: // autocannon
                p.Rect(bc, x0 + gl * 0.5f, 0, gl, gt * 0.75f);
                p.Rect(G.Mul(bc, 1.2f), x0 + gl * 0.28f, 0, gl * 0.5f, gt * 1.5f);
                p.Rect(dk, x0 + 0.25f, gt * 1.2f, 0.4f, 0.22f);
                p.Rect(dk, x0 + gl - 0.04f, 0, 0.09f, gt * 1.1f);
                break;
            case 3: // howitzer
                p.Rect(bc, x0 + gl * 0.5f, 0, gl, gt);
                p.Rect(G.Mul(bc, 1.25f), x0 + gl * 0.28f, 0, gl * 0.34f, gt * 1.7f);
                p.Rect(hi, x0 + gl * 0.5f, gt * 0.25f, gl, gt * 0.16f);
                p.Rect(G.Mul(bc, 1.15f), x0 + gl * 0.58f, 0, gl * 0.1f, gt * 1.5f);
                p.Rect(dk, x0 + gl - 0.12f, 0, 0.26f, gt * 2.3f);
                p.Rect(G.Hex(0x45473e), x0 + gl - 0.12f, 0, 0.08f, gt * 2.3f);
                break;
            case 4: // machine gun
                p.Rect(bc, x0 + gl * 0.5f, 0, gl, gt * 0.55f);
                p.Rect(dk, x0 + 0.2f, 0, 0.4f, gt * 1.3f);
                p.Rect(G.Hex(0x4a4c40), x0 + 0.15f, -gt * 1.1f, 0.22f, 0.2f);
                break;
            case 5: // twin AA guns
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    p.Rect(bc, x0 + gl * 0.5f, sg * 0.3f, gl, gt * 0.6f);
                    p.Rect(G.Mul(bc, 1.3f), x0 + gl * 0.26f, sg * 0.3f, gl * 0.4f, gt * 1.4f);
                    p.Rect(dk, x0 + gl - 0.05f, sg * 0.3f, 0.12f, gt * 1.2f);
                    p.Rect(dk, x0 + 0.3f, sg * 0.55f, 0.5f, 0.24f);
                }
                p.Rect(G.Mul(bc, 1.1f), x0 + 0.2f, 0, 0.35f, 0.75f);
                break;
            default: // short gun
                p.Rect(bc, x0 + gl * 0.5f, 0, gl, gt * 1.1f);
                p.Rect(G.Mul(bc, 1.2f), x0 + gl * 0.35f, 0, gl * 0.3f, gt * 1.6f);
                p.Circle(dk, x0 + gl - 0.05f, 0, gt * 0.85f);
                break;
        }
    }
}
