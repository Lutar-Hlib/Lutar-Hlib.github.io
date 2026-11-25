using System;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Small helpers + runtime sprite atlas + software "pixel canvas" used to draw
// every model of the game procedurally (no external art assets required).
// ---------------------------------------------------------------------------
public static class G
{
    public static Color Hex(int rgb, float a = 1f)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
    }
    public static Color Mul(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }
    public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
    public static float Hash(int x, int y, int s)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + s * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777215f;
        }
    }
    public static Vector2 Dir(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }
    public static float Ang(Vector2 v) { return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; }
    public static Vector2 Rot(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
    public static float Approach(float cur, float target, float step)
    {
        if (cur < target) return Mathf.Min(cur + step, target);
        return Mathf.Max(cur - step, target);
    }
    public static float TurnToward(float cur, float target, float step)
    {
        float d = Mathf.DeltaAngle(cur, target);
        if (Mathf.Abs(d) <= step) return target;
        return cur + Mathf.Sign(d) * step;
    }
    public static float Rnd(float a, float b) { return UnityEngine.Random.Range(a, b); }
    public static float R01() { return UnityEngine.Random.value; }
}

public static class Atlas
{
    const int PS = 2048;
    class Page { public Texture2D tex; public int x, y, rowH; public bool dirty; }
    static readonly List<Page> pages = new List<Page>();

    static Page NewPage()
    {
        var p = new Page();
        p.tex = new Texture2D(PS, PS, TextureFormat.RGBA32, false);
        p.tex.filterMode = FilterMode.Bilinear;
        p.tex.wrapMode = TextureWrapMode.Clamp;
        p.tex.SetPixels32(new Color32[PS * PS]);
        p.tex.Apply(false);
        pages.Add(p);
        return p;
    }

    public static Sprite Add(Color32[] px, int w, int h, float ppu, Vector2 pivot)
    {
        if (w < 1) w = 1; if (h < 1) h = 1;
        Page p = pages.Count > 0 ? pages[pages.Count - 1] : NewPage();
        if (p.x + w + 2 > PS) { p.x = 0; p.y += p.rowH; p.rowH = 0; }
        if (p.y + h + 2 > PS) { p = NewPage(); }
        int ox = p.x + 1, oy = p.y + 1;
        p.tex.SetPixels32(ox, oy, w, h, px);
        p.x += w + 2;
        p.rowH = Mathf.Max(p.rowH, h + 2);
        p.dirty = true;
        return Sprite.Create(p.tex, new Rect(ox, oy, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
    }

    public static void Flush()
    {
        for (int i = 0; i < pages.Count; i++)
            if (pages[i].dirty) { pages[i].tex.Apply(false); pages[i].dirty = false; }
    }
}

// Software canvas. Coordinates are in "world units", origin at the centre,
// x to the right, y up. Shapes are anti-aliased scanline polygons.
public class Pix
{
    public int w, h;
    public float ppu;
    public Color32[] b;
    float hw, hh;
    float[] acc;
    List<float> xs = new List<float>(16);
    float[] qb = new float[128];

    public Pix(float wu, float hu, float ppu)
    {
        this.ppu = ppu;
        w = Mathf.Max(2, Mathf.CeilToInt(wu * ppu));
        h = Mathf.Max(2, Mathf.CeilToInt(hu * ppu));
        b = new Color32[w * h];
        hw = w / (2f * ppu);
        hh = h / (2f * ppu);
        acc = new float[w + 2];
    }

    public Pix(int pw, int ph, float ppu, bool raw)
    {
        this.ppu = ppu; w = pw; h = ph; b = new Color32[w * h];
        hw = w / (2f * ppu); hh = h / (2f * ppu); acc = new float[w + 2];
    }

    public float HalfW { get { return hw; } }
    public float HalfH { get { return hh; } }

    void Blend(int idx, Color c, float cov, bool clip)
    {
        float sa = c.a * cov;
        if (sa <= 0.002f) return;
        Color32 d = b[idx];
        float da = d.a / 255f;
        if (clip)
        {
            if (da <= 0.003f) return;
            sa *= da;
            float k = sa;
            b[idx] = new Color32(
                (byte)Mathf.Clamp(d.r + (c.r * 255f - d.r) * k, 0, 255),
                (byte)Mathf.Clamp(d.g + (c.g * 255f - d.g) * k, 0, 255),
                (byte)Mathf.Clamp(d.b + (c.b * 255f - d.b) * k, 0, 255), d.a);
            return;
        }
        float oa = sa + da * (1f - sa);
        if (oa <= 0f) return;
        float kd = da * (1f - sa);
        b[idx] = new Color32(
            (byte)Mathf.Clamp((c.r * 255f * sa + d.r * kd) / oa, 0, 255),
            (byte)Mathf.Clamp((c.g * 255f * sa + d.g * kd) / oa, 0, 255),
            (byte)Mathf.Clamp((c.b * 255f * sa + d.b * kd) / oa, 0, 255),
            (byte)Mathf.Clamp(oa * 255f, 0, 255));
    }

    // fill polygon given as x,y pairs in unit coordinates
    public void Poly(Color c, float[] p, bool clip = false)
    {
        int n = p.Length / 2;
        if (n < 3) return;
        if (qb.Length < p.Length) qb = new float[p.Length];
        float minx = 1e9f, maxx = -1e9f, miny = 1e9f, maxy = -1e9f;
        for (int i = 0; i < n; i++)
        {
            float x = (p[i * 2] + hw) * ppu, y = (p[i * 2 + 1] + hh) * ppu;
            qb[i * 2] = x; qb[i * 2 + 1] = y;
            if (x < minx) minx = x; if (x > maxx) maxx = x;
            if (y < miny) miny = y; if (y > maxy) maxy = y;
        }
        int x0 = Mathf.Max(0, (int)Mathf.Floor(minx)), x1 = Mathf.Min(w - 1, (int)Mathf.Floor(maxx));
        int y0 = Mathf.Max(0, (int)Mathf.Floor(miny)), y1 = Mathf.Min(h - 1, (int)Mathf.Floor(maxy));
        if (x1 < x0 || y1 < y0) return;
        const int SS = 3;
        for (int py = y0; py <= y1; py++)
        {
            for (int i = x0; i <= x1 + 1 && i < acc.Length; i++) acc[i] = 0;
            for (int s = 0; s < SS; s++)
            {
                float yy = py + (s + 0.5f) / SS;
                xs.Clear();
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    float ay = qb[i * 2 + 1], by = qb[j * 2 + 1];
                    if ((ay <= yy && by > yy) || (by <= yy && ay > yy))
                    {
                        float ax = qb[i * 2], bx = qb[j * 2];
                        xs.Add(ax + (yy - ay) / (by - ay) * (bx - ax));
                    }
                }
                if (xs.Count < 2) continue;
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    float xa = Mathf.Clamp(xs[k], x0, x1 + 1), xb = Mathf.Clamp(xs[k + 1], x0, x1 + 1);
                    if (xb <= xa) continue;
                    int ia = (int)xa, ib = (int)xb;
                    if (ia == ib) acc[ia] += xb - xa;
                    else
                    {
                        acc[ia] += ia + 1 - xa;
                        for (int q = ia + 1; q < ib; q++) acc[q] += 1f;
                        if (ib < acc.Length) acc[ib] += xb - ib;
                    }
                }
            }
            int row = py * w;
            for (int px = x0; px <= x1; px++)
            {
                float cov = acc[px] / SS;
                if (cov > 0.002f) Blend(row + px, c, Mathf.Min(1f, cov), clip);
            }
        }
    }

    public void Poly(Color c, params float[] p) { Poly(c, p, false); }

    public void Rect(Color c, float cx, float cy, float rw, float rh, float rot = 0f, bool clip = false)
    {
        float hx = rw * 0.5f, hy = rh * 0.5f;
        float[] pts = { -hx, -hy, hx, -hy, hx, hy, -hx, hy };
        Xform(pts, cx, cy, rot);
        Poly(c, pts, clip);
    }
    public void RectA(Color c, float x0, float y0, float x1, float y1, bool clip = false)
    {
        Poly(c, new float[] { x0, y0, x1, y0, x1, y1, x0, y1 }, clip);
    }
    public void Ell(Color c, float cx, float cy, float rx, float ry, float rot = 0f, bool clip = false, int segs = 26)
    {
        float[] pts = new float[segs * 2];
        for (int i = 0; i < segs; i++)
        {
            float a = i * Mathf.PI * 2f / segs;
            pts[i * 2] = Mathf.Cos(a) * rx; pts[i * 2 + 1] = Mathf.Sin(a) * ry;
        }
        Xform(pts, cx, cy, rot);
        Poly(c, pts, clip);
    }
    public void Circle(Color c, float cx, float cy, float r, bool clip = false) { Ell(c, cx, cy, r, r, 0, clip, r * ppu > 8 ? 30 : 16); }
    public void Line(Color c, float x0, float y0, float x1, float y1, float th, bool clip = false)
    {
        float dx = x1 - x0, dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 1e-5f) { Circle(c, x0, y0, th * 0.5f, clip); return; }
        float nx = -dy / len * th * 0.5f, ny = dx / len * th * 0.5f;
        Poly(c, new float[] { x0 + nx, y0 + ny, x1 + nx, y1 + ny, x1 - nx, y1 - ny, x0 - nx, y0 - ny }, clip);
    }
    public void Outline(Color c, float[] p, float th, bool clip = false)
    {
        int n = p.Length / 2;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Line(c, p[i * 2], p[i * 2 + 1], p[j * 2], p[j * 2 + 1], th, clip);
        }
    }
    static void Xform(float[] pts, float cx, float cy, float rot)
    {
        float r = rot * Mathf.Deg2Rad, co = Mathf.Cos(r), si = Mathf.Sin(r);
        for (int i = 0; i < pts.Length; i += 2)
        {
            float x = pts[i], y = pts[i + 1];
            pts[i] = x * co - y * si + cx;
            pts[i + 1] = x * si + y * co + cy;
        }
    }

    // radial gradient disc (soft glow, smoke puffs ...)
    public void Radial(Color inner, Color outer, float radius, float power = 1f, float noise = 0f, int seed = 0)
    {
        float cx = w * 0.5f, cy = h * 0.5f, rr = radius * ppu;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / rr;
                if (noise > 0f) d += (G.Hash(x / 2, y / 2, seed) - 0.5f) * noise;
                if (d >= 1f) continue;
                float t = Mathf.Pow(Mathf.Clamp01(d), power);
                Color c = Color.Lerp(inner, outer, t);
                b[y * w + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));
            }
    }

    // composite another canvas into this one (for icons)
    public void Blit(Pix src, float dx, float dy, float rotDeg = 0f)
    {
        float r = -rotDeg * Mathf.Deg2Rad, co = Mathf.Cos(r), si = Mathf.Sin(r);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float ux = (x + 0.5f) / ppu - hw - dx, uy = (y + 0.5f) / ppu - hh - dy;
                float sx = ux * co - uy * si, sy = ux * si + uy * co;
                int ix = (int)((sx + src.hw) * src.ppu), iy = (int)((sy + src.hh) * src.ppu);
                if (ix < 0 || iy < 0 || ix >= src.w || iy >= src.h) continue;
                Color32 s = src.b[iy * src.w + ix];
                if (s.a < 4) continue;
                Blend(y * w + x, new Color(s.r / 255f, s.g / 255f, s.b / 255f, s.a / 255f), 1f, false);
            }
    }

    // multiply this canvas' alpha by another canvas' alpha (same size)
    public void Mask(Pix o)
    {
        int n = Mathf.Min(b.Length, o.b.Length);
        for (int i = 0; i < n; i++) { Color32 c = b[i]; c.a = (byte)(c.a * o.b[i].a / 255); b[i] = c; }
    }
    public Pix Silhouette(Color col)
    {
        var s = new Pix(w, h, ppu, true);
        for (int i = 0; i < b.Length && i < s.b.Length; i++)
            s.b[i] = new Color32((byte)(col.r * 255), (byte)(col.g * 255), (byte)(col.b * 255), (byte)(b[i].a * col.a));
        return s;
    }
    public Pix CloneCanvas()
    {
        var s = new Pix(w, h, ppu, true);
        Array.Copy(b, s.b, Mathf.Min(b.Length, s.b.Length));
        return s;
    }

    public Sprite Make(float pivotX = 0.5f, float pivotY = 0.5f)
    {
        return Atlas.Add(b, w, h, ppu, new Vector2(pivotX, pivotY));
    }
    // pivot given in unit coordinates relative to the canvas centre
    public Sprite MakeAt(float ux, float uy)
    {
        return Atlas.Add(b, w, h, ppu, new Vector2((ux + hw) / (2f * hw), (uy + hh) / (2f * hh)));
    }
}
