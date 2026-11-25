using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class Art
{
    // ================================================================== PROPS
    static Dictionary<string, Sprite> propCache = new Dictionary<string, Sprite>();
    static Dictionary<string, Model> propModels = new Dictionary<string, Model>();

    public static Sprite PropSprite(UnitDef d, Brigade b, Faction f, int variant, int state)
    {
        Pal pal = Pal.Make(d.cat == Cat.Infrastructure && f == Faction.Neutral ? null : b, f);
        string key = d.id + "|" + pal.Key + "|" + variant + "|" + state;
        Sprite s;
        if (propCache.TryGetValue(key, out s)) return s;
        Model m = BuildProp(d, pal, false, variant, state);
        s = m.body;
        propCache[key] = s;
        propModels[key] = m;
        return s;
    }
    public static Model PropModel(UnitDef d, Brigade b, Faction f, int variant, int state)
    {
        PropSprite(d, b, f, variant, state);
        Pal pal = Pal.Make(d.cat == Cat.Infrastructure && f == Faction.Neutral ? null : b, f);
        return propModels[d.id + "|" + pal.Key + "|" + variant + "|" + state];
    }

    static Model BuildProp(UnitDef d, Pal pal, bool icon, int v, int state)
    {
        var m = new Model();
        bool tree = d.special == "tree";
        float pad = tree ? 1.4f : 0.9f;
        float ppu = icon ? 9f : (tree ? 14f : 20f);
        var p = new Pix(d.len + pad, d.wid + pad, ppu);
        if (state == 2 && !tree) DrawRuin(p, d, pal, v);
        else
        {
            DrawProp(p, d, pal, v, state, m);
            if (state == 1 && !tree) DrawPropDamage(p, d, v);
        }
        if (icon) { m.icon = p.Make(); return m; }
        m.body = p.Make();
        return m;
    }

    static void DrawRuin(Pix p, UnitDef d, Pal pal, int v)
    {
        float hw = d.len * 0.5f, hh = d.wid * 0.5f;
        p.Ell(new Color(0.06f, 0.05f, 0.04f, 0.7f), 0, 0, hw * 1.05f, hh * 1.05f, 0, false, 18);
        int n = 14 + (int)(d.len);
        for (int i = 0; i < n; i++)
        {
            float x = (G.Hash(i, v, 11) - 0.5f) * hw * 1.7f, y = (G.Hash(i, v, 12) - 0.5f) * hh * 1.7f;
            float s = 0.25f + G.Hash(i, v, 13) * 0.55f;
            float g = 0.22f + G.Hash(i, v, 14) * 0.3f;
            Color c = i % 4 == 0 ? new Color(0.4f, 0.22f, 0.15f) : new Color(g, g * 0.97f, g * 0.92f);
            p.Rect(c, x, y, s, s * (0.5f + G.Hash(i, v, 15)), G.Hash(i, v, 16) * 180f);
        }
        for (int i = 0; i < 4; i++) { float x = (G.Hash(i, v, 21) - 0.5f) * hw, y = (G.Hash(i, v, 22) - 0.5f) * hh; p.Line(G.Hex(0x4a3a28), x, y, x + (G.Hash(i, v, 23) - 0.5f) * 1.6f, y + (G.Hash(i, v, 24) - 0.5f) * 1.2f, 0.08f); }
        p.Ell(new Color(0.02f, 0.02f, 0.02f, 0.5f), 0, 0, hw * 0.5f, hh * 0.5f, 0, false, 12);
    }

    static void DrawPropDamage(Pix p, UnitDef d, int v)
    {
        var o = new Pix(p.w, p.h, p.ppu, true);
        float hw = d.len * 0.5f, hh = d.wid * 0.5f;
        for (int i = 0; i < 6; i++) o.Ell(new Color(0.05f, 0.04f, 0.04f, 0.55f), (G.Hash(i, v, 31) - 0.5f) * hw * 1.4f, (G.Hash(i, v, 32) - 0.5f) * hh * 1.4f, 0.3f + G.Hash(i, v, 33) * 0.7f, 0.25f + G.Hash(i, v, 34) * 0.5f, G.Hash(i, v, 35) * 180f, false, 12);
        for (int i = 0; i < 4; i++) { float x = (G.Hash(i, v, 36) - 0.5f) * hw * 1.3f, y = (G.Hash(i, v, 37) - 0.5f) * hh * 1.3f; o.Poly(new Color(0.03f, 0.03f, 0.03f, 0.92f), new float[] { x, y, x + 0.5f, y + 0.15f, x + 0.3f, y + 0.55f, x - 0.1f, y + 0.4f }); }
        for (int i = 0; i < 5; i++) { float x = (G.Hash(i, v, 38) - 0.5f) * hw * 1.3f, y = (G.Hash(i, v, 39) - 0.5f) * hh * 1.3f; o.Line(new Color(0.25f, 0.2f, 0.16f, 0.9f), x, y, x + 0.7f, y - 0.3f, 0.04f); }
        o.Mask(p);
        p.Blit(o, 0, 0, 0);
    }

    static Color[] RoofCols = { G.Hex(0x9a4a36), G.Hex(0x6a6e72), G.Hex(0x4e6a4c), G.Hex(0x8a6a3e), G.Hex(0x5a4e66) };
    static Color[] WallCols = { G.Hex(0xd8d0bc), G.Hex(0xc9c2ae), G.Hex(0xcdb99a), G.Hex(0xb6b9ae) };

    static void DrawProp(Pix p, UnitDef d, Pal pal, int v, int state, Model m)
    {
        float L = d.len, W = d.wid, hl = L / 2f, hw = W / 2f;
        Color roof = RoofCols[v % RoofCols.Length], wall = WallCols[(v / 2) % WallCols.Length];
        Color dk = G.Mul(roof, 0.65f), conc = G.Hex(0x8c8e88);
        Color shade = new Color(0, 0, 0, 0.28f);
        // soft building shadow
        if (d.solid) p.Rect(shade, 0.18f, -0.18f, L + 0.1f, W + 0.1f);
        switch (d.style)
        {
            case 0: case 1: // houses
                p.Rect(wall, 0, 0, L, W);
                p.Poly(roof, new float[] { -hl - 0.15f, -hw - 0.15f, hl + 0.15f, -hw - 0.15f, hl + 0.15f, 0, -hl - 0.15f, 0 });
                p.Poly(G.Mul(roof, 0.8f), new float[] { -hl - 0.15f, 0, hl + 0.15f, 0, hl + 0.15f, hw + 0.15f, -hl - 0.15f, hw + 0.15f });
                p.Line(G.Mul(roof, 0.55f), -hl - 0.15f, 0, hl + 0.15f, 0, 0.07f);
                for (int i = 0; i < 6; i++) p.Line(G.Mul(roof, 0.7f), -hl + i * L / 5f, -hw, -hl + i * L / 5f, hw, 0.02f);
                p.Rect(G.Hex(0x6a5648), hl * 0.4f, -hw * 0.4f, 0.4f, 0.4f);
                if (d.style == 1) { p.Rect(G.Mul(roof, 0.9f), hl * 0.5f, hw * 0.7f, L * 0.4f, W * 0.55f); p.Line(G.Mul(roof, 0.55f), hl * 0.3f, hw * 0.45f, hl * 0.3f, hw * 1.1f, 0.06f); }
                break;
            case 2: // apartment
                p.Rect(G.Hex(0x9a9a92), 0, 0, L, W); p.Rect(G.Hex(0xb4b4ac), 0, 0, L - 0.5f, W - 0.5f);
                for (int i = 0; i < 4; i++) p.Rect(G.Hex(0x82847e), -L * 0.35f + i * L * 0.23f, 0.1f * (i % 2 == 0 ? 1 : -1), 0.9f, 0.6f);
                p.Rect(G.Hex(0x6a6c68), L * 0.3f, W * 0.28f, 1.4f, 1.0f); p.Rect(G.Hex(0x4a4c48), L * 0.3f, -W * 0.25f, 0.9f, 0.9f);
                p.Outline(G.Hex(0x76766e), new float[] { -hl, -hw, hl, -hw, hl, hw, -hl, hw }, 0.12f);
                for (int i = 0; i < 7; i++) p.Line(G.Hex(0xa0a09a, 0.5f), -hl + 0.2f + i * (L - 0.4f) / 6f, -hw + 0.1f, -hl + 0.2f + i * (L - 0.4f) / 6f, -hw + 0.4f, 0.05f);
                break;
            case 3: // high-rise
                p.Rect(G.Hex(0x6c7076), 0, 0, L, W); p.Rect(G.Hex(0x878c92), 0, 0, L - 0.5f, W - 0.5f); p.Circle(G.Hex(0x5a5e64), 0, 0, W * 0.3f); p.Circle(G.Hex(0xe0e0d8), 0, 0, W * 0.2f);
                p.Rect(G.Hex(0xcc2222), 0, 0, 0.1f, 0.5f); p.Rect(G.Hex(0x555a60), -hl * 0.6f, hw * 0.6f, 1.0f, 1.0f);
                p.Line(G.Hex(0x202020), 0, 0, hl, hw, 0.04f);
                break;
            case 4: // warehouse
                p.Rect(G.Hex(0x5f7280), 0, 0, L, W);
                p.Poly(G.Hex(0x7a8a98), new float[] { -hl, -hw, hl, -hw, hl, 0, -hl, 0 });
                p.Poly(G.Hex(0x68798a), new float[] { -hl, 0, hl, 0, hl, hw, -hl, hw });
                p.Line(G.Hex(0x4a5560), -hl, 0, hl, 0, 0.08f);
                for (int i = 1; i < 10; i++) p.Line(G.Hex(0x4a5a68, 0.7f), -hl + i * L / 10f, -hw, -hl + i * L / 10f, hw, 0.025f);
                p.Rect(G.Hex(0xa0c0d0, 0.8f), -hl * 0.4f, -hw * 0.45f, 1.5f, 0.6f); p.Rect(G.Hex(0xa0c0d0, 0.8f), hl * 0.4f, hw * 0.45f, 1.5f, 0.6f);
                break;
            case 5: // factory
                p.Rect(G.Hex(0x7c7e7a), 0, 0, L, W);
                for (int i = 0; i < 6; i++) { p.Poly(G.Hex(0x909490), new float[] { -hl + i * L / 6f, -hw + 0.2f, -hl + (i + 0.85f) * L / 6f, -hw + 0.2f, -hl + (i + 0.85f) * L / 6f, hw - 0.2f, -hl + i * L / 6f, hw - 0.2f }); p.Rect(G.Hex(0xa8c4d0), -hl + (i + 0.5f) * L / 6f, 0, L / 6f * 0.35f, W * 0.75f); }
                p.Circle(G.Hex(0x5a2a22), hl * 0.7f, hw * 0.55f, 0.65f); p.Circle(G.Hex(0x1a1a1a), hl * 0.7f, hw * 0.55f, 0.4f);
                p.Circle(G.Hex(0x5a2a22), hl * 0.4f, hw * 0.55f, 0.55f); p.Circle(G.Hex(0x1a1a1a), hl * 0.4f, hw * 0.55f, 0.32f);
                p.Rect(G.Hex(0x6a6e70), -hl * 0.3f, -hw - 0.1f, L * 0.4f, 0.3f);
                break;
            case 6: // hangar
                p.Rect(G.Hex(0x6c7466), 0, 0, L, W);
                for (int i = 0; i < 12; i++) { float t = i / 11f; p.Rect(Color.Lerp(G.Hex(0x8a947e), G.Hex(0x5e6658), Mathf.Abs(t - 0.5f) * 2f), 0, -hw + (i + 0.5f) * W / 12f, L, W / 12f * 0.95f); }
                p.Rect(G.Hex(0x3e443a), hl - 0.1f, 0, 0.3f, W * 0.8f); Camo(p, pal, 3, 1.2f);
                p.Line(G.Hex(0x3a4036), -hl, 0, hl, 0, 0.05f);
                break;
            case 7: // barracks
                p.Rect(G.Mul(pal.main, 0.9f), 0, 0, L, W); p.Poly(G.Mul(pal.main, 1.1f), new float[] { -hl, -hw, hl, -hw, hl, 0, -hl, 0 });
                Camo(p, pal, 9, 1.3f); p.Line(G.Mul(pal.dark, 0.7f), -hl, 0, hl, 0, 0.06f);
                for (int i = 0; i < 5; i++) p.Rect(G.Mul(pal.dark, 0.8f), -hl + 1 + i * (L - 2) / 4f, hw * 0.5f, 0.5f, 0.4f);
                Emblem(p, pal.emblem, 0, -hw * 0.45f, 0.9f, pal.accent, pal.dark, 0); FacMark(p, pal, hl - 0.8f, hw * 0.55f, 0.8f, 0.4f);
                break;
            case 8: // command post
                p.Rect(G.Mul(pal.main, 0.85f), 0, 0, L, W); Camo(p, pal, 12, 1.1f); p.Outline(pal.dark, new float[] { -hl, -hw, hl, -hw, hl, hw, -hl, hw }, 0.08f, true);
                p.Circle(G.Hex(0xc0c4c0), -hl * 0.4f, 0, 0.8f); p.Circle(G.Hex(0x909490), -hl * 0.4f, 0, 0.45f); p.Line(G.Hex(0x202020), -hl * 0.4f, 0, hl * 0.0f, 0.7f, 0.05f);
                p.Rect(G.Hex(0x303030), hl * 0.4f, hw * 0.3f, 1.2f, 0.9f); p.Line(G.Hex(0x222222), hl * 0.7f, -hw * 0.6f, hl * 0.7f, hw * 0.9f, 0.04f);
                Emblem(p, pal.emblem, hl * 0.3f, -hw * 0.35f, 0.9f, pal.accent, pal.dark, 0); FacMark(p, pal, hl - 0.6f, -hw + 0.5f, 0.7f, 0.35f);
                break;
            case 9: // tower
                p.Rect(G.Hex(0xb8b8b0), 0, 0, L, W); p.Circle(G.Hex(0x8f9498), 0, 0, W * 0.38f); p.Circle(G.Hex(0x7ac0d8, 0.85f), 0, 0, W * 0.28f); p.Line(G.Hex(0x202020), 0, 0, W * 0.5f, 0.2f, 0.04f);
                p.Rect(G.Hex(0xe04020), hl - 0.3f, hw - 0.3f, 0.3f, 0.3f);
                break;
            case 10: // radar
                p.Rect(G.Hex(0x9a9e98), 0, 0, L * 0.7f, W * 0.7f); p.Circle(G.Hex(0xd0d4d0), 0, 0, W * 0.45f); p.Circle(G.Hex(0xa8acaa), 0, 0, W * 0.3f); p.Line(G.Hex(0x303030), -W * 0.45f, 0, W * 0.45f, 0, 0.05f); p.Line(G.Hex(0x303030), 0, -W * 0.45f, 0, W * 0.45f, 0.05f);
                p.Circle(G.Hex(0x202020), 0, 0, 0.12f);
                break;
            case 11: // fuel tank
                p.Circle(shade, 0.15f, -0.15f, hl);
                p.Circle(G.Hex(0xa8a8a0), 0, 0, hl); p.Circle(G.Hex(0xbcbcb4), 0, 0, hl * 0.82f); p.Circle(G.Hex(0x9a9a92), 0, 0, hl * 0.55f); p.Circle(G.Hex(0x70726c), 0, 0, hl * 0.2f);
                p.Line(G.Hex(0x6a6c66), 0, 0, hl * 0.95f, 0, 0.1f); p.Rect(G.Hex(0xcc2222), hl * 0.5f, 0, 0.3f, 0.3f);
                break;
            case 12: // ammo depot
                p.Ell(G.Hex(0x5e5a3e), 0, 0, hl, hw); p.Ell(G.Hex(0x70704a), 0, 0.1f, hl * 0.82f, hw * 0.78f); p.Rect(G.Hex(0x404038), hl * 0.8f, 0, 1.0f, 1.8f);
                for (int i = 0; i < 3; i++) p.Rect(G.Hex(0x3a3a2c), -hl * 0.4f + i * hl * 0.4f, 0, 0.5f, W * 0.55f);
                p.Rect(G.Hex(0xd0a020), 0, 0, 0.5f, 0.5f); Camo(p, pal, 15, 1.2f);
                break;
            case 13: // bunker
                { float[] pts = { -hl, -hw * 0.8f, -hl * 0.6f, -hw, hl * 0.6f, -hw, hl, -hw * 0.8f, hl, hw * 0.8f, hl * 0.6f, hw, -hl * 0.6f, hw, -hl, hw * 0.8f };
                  p.Poly(conc, pts); p.Poly(G.Hex(0x9c9e98), Tf(pts, 0, 0, 0.8f, 0)); p.Outline(G.Hex(0x5a5c58), pts, 0.08f); Camo(p, pal, 16, 1.2f);
                  p.Rect(G.Hex(0x0c0c0c), hl * 0.85f, 0, 0.12f, hw * 0.7f); p.Rect(G.Hex(0x0c0c0c), 0, -hw * 0.9f, hl * 0.6f, 0.12f); p.Rect(G.Hex(0x0c0c0c), 0, hw * 0.9f, hl * 0.6f, 0.12f);
                  FacMark(p, pal, -hl * 0.5f, 0, 0.8f, 0.4f); break; }
            case 14: p.Circle(conc, 0, 0, hl); p.Circle(G.Hex(0x9c9e98), 0, 0, hl * 0.75f); p.Rect(G.Hex(0x0c0c0c), hl * 0.7f, 0, 0.1f, 0.6f); break;
            case 15: case 37: // watchtower / observation post
                p.Rect(G.Hex(0x5a4a34), 0, 0, L * 0.9f, W * 0.9f); p.Rect(G.Mul(pal.main, 0.9f), 0, 0, L * 0.75f, W * 0.75f); Camo(p, pal, 18, 0.9f);
                p.Line(G.Hex(0x3a3024), -hl, -hw, hl, hw, 0.08f); p.Line(G.Hex(0x3a3024), -hl, hw, hl, -hw, 0.08f);
                if (d.style == 37) { p.Circle(G.Hex(0x303030), 0, 0, 0.3f); p.Line(G.Hex(0x303030), 0, 0, 0.6f, 0.1f, 0.06f); p.Line(G.Hex(0x303030), 0, 0, 0.6f, -0.1f, 0.06f); }
                break;
            case 16: // church
                p.Rect(G.Hex(0xe8e4d8), 0, 0, L, W * 0.55f); p.Rect(G.Hex(0xe8e4d8), -hl * 0.1f, 0, L * 0.35f, W);
                p.Rect(G.Hex(0x5a5e6a), 0, 0, L, W * 0.3f); p.Rect(G.Hex(0x4a4e5a), -hl * 0.1f, 0, L * 0.18f, W * 0.95f);
                p.Circle(G.Hex(0xe0b020), -hl * 0.1f, 0, 0.55f); p.Circle(G.Hex(0xf0d050), -hl * 0.1f, 0, 0.3f);
                break;
            case 17: // garage / shop
                p.Rect(G.Hex(0xb8b4a6), 0, 0, L, W); p.Rect(G.Hex(0x7a8a96), 0, 0, L - 0.4f, W - 0.4f);
                for (int i = 0; i < 8; i++) p.Line(G.Hex(0x66767f), -hl + 0.2f + i * (L - 0.4f) / 7f, -hw, -hl + 0.2f + i * (L - 0.4f) / 7f, hw, 0.04f);
                break;
            case 18: // port crane
                p.Rect(G.Hex(0xc8791c), 0, 0, 1.6f, W); p.Rect(G.Hex(0xe09a30), 0, 0, 1.2f, W - 0.4f);
                p.Rect(G.Hex(0xc8791c), 0, 0, L, 0.5f); p.Line(G.Hex(0x303030), -hl, 0, hl, 0, 0.05f);
                p.Rect(G.Hex(0x303a44), hl * 0.7f, 0, 0.8f, 0.9f); p.Line(G.Hex(0x555555), 0, -hw, 0, hw, 0.05f);
                break;
            case 19: // containers
                { Color[] cc = { G.Hex(0xb04a30), G.Hex(0x30608a), G.Hex(0x7a8a3a), G.Hex(0xc0a030), G.Hex(0x808890), G.Hex(0x30804a) };
                  for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) { p.Rect(cc[(i * 3 + j * 5 + v) % cc.Length], -hl * 0.65f + i * hl * 0.65f, -hw * 0.5f + j * hw * 1.0f, L * 0.3f, W * 0.42f); for (int k = 1; k < 5; k++) p.Line(G.Hex(0x000000, 0.25f), -hl * 0.65f + i * hl * 0.65f - L * 0.15f + k * L * 0.06f, -hw * 0.5f + j * hw - W * 0.2f, -hl * 0.65f + i * hl * 0.65f - L * 0.15f + k * L * 0.06f, -hw * 0.5f + j * hw + W * 0.2f, 0.02f); } }
                break;
            case 21: // sandbags
                for (int i = 0; i < 9; i++) for (int r = 0; r < 2; r++) p.Ell(r == 0 ? G.Hex(0xb0a070) : G.Hex(0xa09060), -hl + 0.3f + i * (L - 0.6f) / 8f + r * 0.2f, (r - 0.5f) * 0.45f, 0.32f, 0.2f, 0, false, 10);
                break;
            case 22: // tank traps
                for (int i = 0; i < 3; i++) { float x = -hl * 0.6f + i * hl * 0.6f; for (int a = 0; a < 3; a++) { var dir = G.Dir(a * 60f + 20f) * 0.5f; p.Line(G.Hex(0x3a3d3a), x - dir.x, -dir.y * 0.5f, x + dir.x, dir.y * 0.5f, 0.1f); } }
                break;
            case 23: // fence
                p.Line(G.Hex(0x70747a), -hl, 0, hl, 0, 0.05f); for (int i = 0; i < 6; i++) p.Rect(G.Hex(0x4a4e52), -hl + i * L / 5f, 0, 0.1f, 0.18f);
                p.Line(G.Hex(0x70747a, 0.5f), -hl, 0.1f, hl, 0.1f, 0.03f);
                break;
            case 24: // concrete barrier
                for (int i = 0; i < 3; i++) { p.Rect(G.Hex(0x9a9c96), -hl + 0.7f + i * 1.3f, 0, 1.2f, 0.8f); p.Rect(G.Hex(0x84867f), -hl + 0.7f + i * 1.3f, 0.2f, 1.2f, 0.3f); }
                break;
            case 25: // trench
                { float[] pts = { -hl, -0.3f, -hl * 0.5f, 0.3f, 0, -0.3f, hl * 0.5f, 0.3f, hl, -0.3f, hl, -0.7f, hl * 0.5f, -0.1f, 0, -0.7f, -hl * 0.5f, -0.1f, -hl, -0.7f };
                  p.Poly(G.Hex(0x3b2f22), pts); p.Poly(G.Hex(0x261d14), Tf(pts, 0, 0.0f, 0.8f, 0)); p.Outline(G.Hex(0x6a5a40), pts, 0.15f); }
                break;
            case 26: // tent
                p.Poly(G.Mul(pal.main, 0.95f), new float[] { -hl, -hw, hl, -hw, hl, 0, -hl, 0 }); p.Poly(G.Mul(pal.main, 0.75f), new float[] { -hl, 0, hl, 0, hl, hw, -hl, hw });
                p.Line(G.Mul(pal.dark, 0.8f), -hl, 0, hl, 0, 0.07f); Camo(p, pal, 19, 1.1f);
                break;
            case 27: // field hospital
                p.Poly(G.Hex(0xf0f0ea), new float[] { -hl, -hw, hl, -hw, hl, 0, -hl, 0 }); p.Poly(G.Hex(0xd6d6cf), new float[] { -hl, 0, hl, 0, hl, hw, -hl, hw });
                p.Line(G.Hex(0xaaaaa0), -hl, 0, hl, 0, 0.06f); p.Rect(G.Hex(0xcc2222), 0, 0, 1.4f, 0.38f); p.Rect(G.Hex(0xcc2222), 0, 0, 0.38f, 1.4f);
                break;
            case 28: // ammo crates
                for (int i = 0; i < 4; i++) { p.Rect(G.Hex(0x4a5a34), -0.6f + (i % 2) * 1.0f, (i / 2 - 0.5f) * 0.75f, 0.9f, 0.6f, (i * 17) % 11 - 5); p.Rect(G.Hex(0xc8b040), -0.6f + (i % 2) * 1.0f, (i / 2 - 0.5f) * 0.75f, 0.2f, 0.1f); }
                break;
            case 29: // supplies
                for (int i = 0; i < 5; i++) p.Rect(i % 2 == 0 ? G.Hex(0x8a6a3a) : G.Hex(0x5a6a44), -0.8f + (i % 3) * 0.8f, (i / 3 - 0.5f) * 0.9f, 0.7f, 0.65f, (i * 13) % 9 - 4);
                p.Line(G.Hex(0xc0b080), -1.1f, 0.0f, 1.1f, 0.0f, 0.03f);
                break;
            case 30: // barrels
                for (int i = 0; i < 3; i++) { float x = -0.4f + i * 0.45f, y = (i % 2 - 0.5f) * 0.5f; p.Circle(i % 2 == 0 ? G.Hex(0xb02820) : G.Hex(0xc8a020), x, y, 0.3f); p.Circle(G.Hex(0x000000, 0.25f), x, y, 0.2f); p.Circle(G.Hex(0x303030), x, y, 0.07f); }
                break;
            case 31: // flag
                p.Circle(G.Hex(0x303030), -0.4f, 0, 0.1f); p.Rect(G.Hex(0x303030), -0.4f, 0, 0.06f, 0.06f);
                FacMark(p, pal.fac == Faction.Neutral ? Pal.Make(null, Faction.Neutral) : pal, 0.1f, 0.0f, 0.9f, 0.55f);
                p.Rect(G.Hex(0xb0b0b0), -0.4f, 0, 0.06f, 0.6f);
                break;
            case 32: // camo net
                p.Rect(G.Mul(pal.main, 0.8f), 0, 0, L, W); Camo(p, pal, 21, 1.6f);
                for (int i = 0; i < 8; i++) p.Line(G.Hex(0x1a1a14, 0.5f), -hl + i * L / 7f, -hw, -hl + i * L / 7f, hw, 0.025f);
                p.Rect(G.Hex(0x000000, 0.2f), 0, 0, L, W);
                break;
            case 33: // helipad
                p.Circle(conc, 0, 0, hl); p.Circle(G.Hex(0xe0e0d8), 0, 0, hl * 0.82f); p.Circle(G.Hex(0x8a8c86), 0, 0, hl * 0.78f);
                p.Rect(G.Hex(0xe0e0d8), -0.6f, 0, 0.3f, 2.0f); p.Rect(G.Hex(0xe0e0d8), 0.6f, 0, 0.3f, 2.0f); p.Rect(G.Hex(0xe0e0d8), 0, 0, 1.2f, 0.3f);
                break;
            case 34: // pylon
                p.Poly(G.Hex(0x6a6e72), new float[] { 0, hl, hl * 0.9f, -hw * 0.8f, -hl * 0.9f, -hw * 0.8f }); p.Poly(G.Hex(0x8a8e92), new float[] { 0, hl * 0.5f, hl * 0.45f, -hw * 0.4f, -hl * 0.45f, -hw * 0.4f });
                p.Circle(G.Hex(0xcc2222), 0, 0, 0.15f); p.Line(G.Hex(0x404448, 0.6f), 0, 0, 0, hl, 0.04f); p.Line(G.Hex(0x404448, 0.6f), 0, 0, hl * 0.9f, -hw * 0.8f, 0.04f); p.Line(G.Hex(0x404448, 0.6f), 0, 0, -hl * 0.9f, -hw * 0.8f, 0.04f);
                break;
            case 35: // MG nest
                for (int i = 0; i < 12; i++) { var dir = G.Dir(i * 30f + 15f); if (i >= 5 && i <= 6) continue; p.Ell(G.Hex(0xb0a070), dir.x * 1.2f, dir.y * 1.2f, 0.3f, 0.22f, i * 30f + 15f + 90f, false, 10); p.Ell(G.Hex(0xa09060), dir.x * 1.05f, dir.y * 1.05f, 0.25f, 0.2f, i * 30f + 15f + 90f, false, 10); }
                p.Circle(G.Hex(0x5a4a34), 0, 0, 0.95f);
                if (m != null) { var tp = new Pix(2.4f, 1.0f, 24); tp.Rect(G.Hex(0x2a2c28), 0.5f, 0, 1.2f, 0.12f); tp.Rect(G.Hex(0x15150f), 1.12f, 0, 0.1f, 0.18f); tp.Rect(G.Hex(0x303a34), 0.0f, 0, 0.35f, 0.35f); tp.Rect(pal.dark, 0.1f, 0, 0.12f, 0.8f); m.turret = tp.MakeAt(-0.4f, 0); m.gunLen = 1.1f; }
                break;
            case 36: // AT gun nest
                for (int i = 0; i < 12; i++) { var dir = G.Dir(i * 30f + 15f); if (i >= 5 && i <= 6) continue; p.Ell(G.Hex(0xb0a070), dir.x * 1.5f, dir.y * 1.5f, 0.35f, 0.25f, i * 30f + 15f + 90f, false, 10); }
                p.Circle(G.Hex(0x5a4a34), 0, 0, 1.2f);
                if (m != null) { var tp = new Pix(3.4f, 1.6f, 24); tp.Rect(G.Hex(0x2a2c28), 0.9f, 0, 1.9f, 0.16f); tp.Rect(G.Hex(0x15150f), 1.85f, 0, 0.16f, 0.24f); tp.Rect(pal.main, 0.1f, 0, 0.18f, 1.3f); tp.Rect(pal.dark, 0.0f, 0, 0.4f, 0.5f); tp.Circle(G.Hex(0x333333), -0.4f, 0.75f, 0.2f); tp.Circle(G.Hex(0x333333), -0.4f, -0.75f, 0.2f); m.turret = tp.MakeAt(-0.9f, 0); m.gunLen = 1.9f; }
                break;
            case 39: // oak
                { p.Circle(new Color(0, 0, 0, 0.25f), 0.25f, -0.25f, hl * 0.95f);
                  Color g1 = G.Hex(0x3b6a2a), g2 = G.Hex(0x4f8238), g3 = G.Hex(0x2d5420);
                  float jt = (v % 3) * 0.1f;
                  p.Circle(g3, 0, 0, hl * 0.95f); p.Circle(g1, -0.1f, 0.1f, hl * 0.78f); p.Circle(g2, -0.25f + jt, 0.25f, hl * 0.5f); p.Circle(G.Hex(0x68a04a, 0.8f), -0.4f, 0.4f, hl * 0.22f);
                  for (int i = 0; i < 8; i++) p.Circle(i % 2 == 0 ? g3 : g2, (G.Hash(i, v, 41) - 0.5f) * hl * 1.2f, (G.Hash(i, v, 42) - 0.5f) * hl * 1.2f, 0.25f + G.Hash(i, v, 43) * 0.3f, true);
                  if (state == 1) DrawFallen(p, d); break; }
            case 40: // pine
                { p.Circle(new Color(0, 0, 0, 0.25f), 0.2f, -0.2f, hl * 0.9f);
                  var pts = new float[24];
                  for (int i = 0; i < 12; i++) { float a = i * 30f * Mathf.Deg2Rad; float rr = hl * (i % 2 == 0 ? 0.98f : 0.62f); pts[i * 2] = Mathf.Cos(a) * rr; pts[i * 2 + 1] = Mathf.Sin(a) * rr; }
                  p.Poly(G.Hex(0x1f4a2a), pts); p.Poly(G.Hex(0x2a6034), Tf(pts, 0, 0, 0.66f, 15f)); p.Poly(G.Hex(0x3a7a42), Tf(pts, 0, 0, 0.32f, 0f)); p.Circle(G.Hex(0x1a3a20), 0, 0, 0.1f);
                  if (state == 1) DrawFallen(p, d); break; }
            case 41: // birch
                { p.Circle(new Color(0, 0, 0, 0.22f), 0.2f, -0.2f, hl * 0.9f);
                  p.Circle(G.Hex(0x6a9a40), 0, 0, hl * 0.9f); p.Circle(G.Hex(0x80b050), -0.15f, 0.1f, hl * 0.62f); p.Circle(G.Hex(0xa0c868), -0.3f, 0.25f, hl * 0.3f);
                  for (int i = 0; i < 6; i++) p.Circle(G.Hex(0x5a8a38), (G.Hash(i, v, 44) - 0.5f) * hl * 1.3f, (G.Hash(i, v, 45) - 0.5f) * hl * 1.3f, 0.2f, true);
                  p.Circle(G.Hex(0xe8e8e0), 0, 0, 0.12f);
                  if (state == 1) DrawFallen(p, d); break; }
            case 44: // haystack
                p.Circle(new Color(0, 0, 0, 0.2f), 0.15f, -0.15f, hl); p.Circle(G.Hex(0xc8a848), 0, 0, hl); p.Circle(G.Hex(0xdcc060), -0.1f, 0.1f, hl * 0.72f); for (int i = 0; i < 4; i++) p.Circle(G.Hex(0xb09038, 0.6f), 0, 0, hl * (0.3f + i * 0.15f), true);
                break;
            case 45: // burning barrel
                p.Circle(G.Hex(0x4a4a46), 0, 0, 0.42f); p.Circle(G.Hex(0x15110c), 0, 0, 0.34f); p.Circle(G.Hex(0xe86a10), 0, 0, 0.22f); p.Circle(G.Hex(0xffc030), 0, 0, 0.1f);
                break;
            default: p.Rect(G.Hex(0x8a8a82), 0, 0, L, W); break;
        }
        if (state == 1 && d.special == "tree") { }
    }

    static void DrawFallen(Pix p, UnitDef d)
    {
        // charred/broken look for damaged trees
        p.Circle(new Color(0.05f, 0.04f, 0.03f, 0.55f), 0, 0, d.len * 0.35f, true);
        p.Line(G.Hex(0x4a3828), -d.len * 0.4f, -0.2f, d.len * 0.5f, 0.3f, 0.2f);
    }

    // ================================================================== VFX sprites
    public static class Spr
    {
        public static Sprite glow, white, ring, spark, bullet, shell, rocket, missile, bomb, flare, canopy, wakeFoam, cloud, drop, flake, muzzle0, muzzle1, muzzle2, selRing, smokeRing, shock, dot, shadowBlob, sparkle, casing;
        public static Sprite[] smoke = new Sprite[3], flame = new Sprite[3], debris = new Sprite[4];
        static bool ready;

        public static void Init()
        {
            if (ready) return; ready = true;
            Pix p;
            p = new Pix(2f, 2f, 32); p.Radial(new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), 1f, 1.6f); glow = p.Make();
            p = new Pix(0.25f, 0.25f, 16); p.RectA(Color.white, -1, -1, 1, 1); white = p.Make();
            p = new Pix(2f, 2f, 32); for (int y = 0; y < p.h; y++) for (int x = 0; x < p.w; x++) { float dx = (x + 0.5f - p.w / 2f) / (p.w / 2f), dy = (y + 0.5f - p.h / 2f) / (p.h / 2f); float r = Mathf.Sqrt(dx * dx + dy * dy); float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) / 0.1f); if (r < 1f) p.b[y * p.w + x] = new Color32(255, 255, 255, (byte)(a * 255)); }
            ring = p.Make();
            p = new Pix(2f, 2f, 32); for (int y = 0; y < p.h; y++) for (int x = 0; x < p.w; x++) { float dx = (x + 0.5f - p.w / 2f) / (p.w / 2f), dy = (y + 0.5f - p.h / 2f) / (p.h / 2f); float r = Mathf.Sqrt(dx * dx + dy * dy); float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.8f) / 0.2f) * 0.9f; if (r < 1f) p.b[y * p.w + x] = new Color32(255, 255, 255, (byte)(a * 255)); }
            shock = p.Make();
            selRing = ring;
            for (int i = 0; i < 3; i++) { p = new Pix(2f, 2f, 24); p.Radial(new Color(0.85f, 0.85f, 0.85f, 0.95f), new Color(0.6f, 0.6f, 0.6f, 0f), 1f, 1.2f, 0.55f, i * 7 + 1); smoke[i] = p.Make(); }
            for (int i = 0; i < 3; i++) { p = new Pix(2f, 2f, 24); p.Radial(new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.35f, 0.05f, 0f), 1f, 1.1f, 0.45f, i * 5 + 3); flame[i] = p.Make(); }
            p = new Pix(0.7f, 0.12f, 48); p.Poly(new Color(1f, 0.9f, 0.5f, 1f), -0.3f, -0.03f, 0.3f, -0.015f, 0.3f, 0.015f, -0.3f, 0.03f); spark = p.Make();
            p = new Pix(1.1f, 0.12f, 40); for (int x = 0; x < p.w; x++) { float t = x / (float)p.w; for (int y = 0; y < p.h; y++) { float dy = Mathf.Abs((y + 0.5f) / p.h - 0.5f) * 2f; float a = t * Mathf.Clamp01(1f - dy); p.b[y * p.w + x] = new Color32(255, (byte)(200 + 55 * t), (byte)(120 + 100 * t), (byte)(a * 255)); } } bullet = p.Make(0.9f, 0.5f);
            p = new Pix(0.6f, 0.26f, 40); p.Rect(G.Hex(0x3a3a34), 0, 0, 0.4f, 0.14f); p.Poly(G.Hex(0xe8c860), 0.2f, -0.07f, 0.3f, 0, 0.2f, 0.07f); p.Rect(G.Hex(0xe8e0b0), 0.0f, 0, 0.1f, 0.15f); shell = p.Make();
            p = new Pix(1.3f, 0.34f, 36); p.Rect(G.Hex(0x707468), 0.0f, 0, 0.8f, 0.14f); p.Poly(G.Hex(0xcc4020), 0.4f, -0.07f, 0.6f, 0, 0.4f, 0.07f); p.Poly(G.Hex(0x40443c), -0.4f, 0.05f, -0.55f, 0.16f, -0.3f, 0.05f); p.Poly(G.Hex(0x40443c), -0.4f, -0.05f, -0.55f, -0.16f, -0.3f, -0.05f); rocket = p.Make();
            p = new Pix(1.5f, 0.5f, 36); p.Rect(G.Hex(0xe8e8e0), 0.0f, 0, 1.0f, 0.18f); p.Poly(G.Hex(0xe8e8e0), 0.5f, -0.09f, 0.72f, 0, 0.5f, 0.09f); p.Poly(G.Hex(0x707468), -0.4f, 0.08f, -0.6f, 0.24f, -0.2f, 0.08f); p.Poly(G.Hex(0x707468), -0.4f, -0.08f, -0.6f, -0.24f, -0.2f, -0.08f); p.Rect(G.Hex(0xcc2222), 0.2f, 0, 0.1f, 0.19f); missile = p.Make();
            p = new Pix(0.9f, 0.5f, 40); p.Ell(G.Hex(0x4a5238), 0, 0, 0.32f, 0.14f); p.Poly(G.Hex(0x303428), -0.28f, 0.08f, -0.42f, 0.2f, -0.22f, 0.08f); p.Poly(G.Hex(0x303428), -0.28f, -0.08f, -0.42f, -0.2f, -0.22f, -0.08f); p.Rect(G.Hex(0xd8c060), 0.05f, 0, 0.06f, 0.28f); bomb = p.Make();
            p = new Pix(0.7f, 0.7f, 40); p.Radial(new Color(1, 1, 0.9f, 1), new Color(1, 0.55f, 0.15f, 0), 0.35f, 0.8f); p.Circle(Color.white, 0, 0, 0.05f); flare = p.Make();
            // parachute canopy: striped disc with panels
            p = new Pix(3.2f, 3.2f, 20);
            for (int i = 0; i < 12; i++)
            {
                float a0 = i * 30f, a1 = a0 + 30f; var v0 = G.Dir(a0) * 1.5f; var v1 = G.Dir(a1) * 1.5f;
                p.Poly(i % 2 == 0 ? G.Hex(0xe8e4d0) : G.Hex(0xc8c8b4), 0, 0, v0.x, v0.y, G.Dir((a0 + a1) / 2f).x * 1.55f, G.Dir((a0 + a1) / 2f).y * 1.55f, v1.x, v1.y);
            }
            p.Circle(G.Hex(0x888878), 0, 0, 0.16f); canopy = p.Make();
            p = new Pix(1.6f, 1.0f, 24); p.Radial(new Color(1, 1, 1, 0.85f), new Color(1, 1, 1, 0), 0.5f, 1f, 0.6f, 5); wakeFoam = p.Make();
            p = new Pix(8f, 8f, 16); p.Radial(new Color(0.05f, 0.07f, 0.1f, 0.5f), new Color(0.05f, 0.07f, 0.1f, 0f), 4f, 1.2f, 0.4f, 9); cloud = p.Make();
            p = new Pix(0.1f, 0.7f, 40); p.Poly(new Color(0.8f, 0.88f, 1f, 0.6f), -0.015f, -0.3f, 0.015f, -0.3f, 0.015f, 0.3f, -0.015f, 0.3f); drop = p.Make();
            p = new Pix(0.2f, 0.2f, 40); p.Circle(new Color(1, 1, 1, 0.9f), 0, 0, 0.07f); flake = p.Make();
            p = new Pix(0.3f, 0.3f, 32); p.Circle(Color.white, 0, 0, 0.12f); dot = p.Make();
            p = new Pix(3f, 3f, 16); p.Radial(new Color(0, 0, 0, 0.5f), new Color(0, 0, 0, 0), 1.5f, 1.2f, 0.2f, 3); shadowBlob = p.Make();
            p = new Pix(0.15f, 0.15f, 40); p.Circle(Color.white, 0, 0, 0.05f); sparkle = p.Make();
            p = new Pix(0.18f, 0.08f, 60); p.Rect(G.Hex(0xd8b040), 0, 0, 0.14f, 0.06f); casing = p.Make();
            for (int i = 0; i < 4; i++)
            {
                p = new Pix(0.6f, 0.6f, 40);
                float s0 = 0.2f + i * 0.03f;
                p.Poly(i % 2 == 0 ? G.Hex(0x3a3a36) : G.Hex(0x585046), -s0, -s0 * 0.6f, s0 * 0.8f, -s0, s0, s0 * 0.4f, -s0 * 0.3f, s0);
                debris[i] = p.Make();
            }
            // muzzle flashes (point +x)
            muzzle0 = MakeFlash(0.9f, 0.3f, 5, 1); muzzle1 = MakeFlash(1.6f, 0.5f, 7, 2); muzzle2 = MakeFlash(2.6f, 0.9f, 9, 3);
        }

        static Sprite MakeFlash(float len, float wid, int rays, int seed)
        {
            var p = new Pix(len + 0.4f, wid * 2.4f, 36);
            Color y = new Color(1f, 0.85f, 0.4f, 0.95f), w = new Color(1f, 1f, 0.9f, 1f);
            for (int i = 0; i < rays; i++)
            {
                float a = (i - (rays - 1) / 2f) * (i == (rays - 1) / 2 ? 0 : 1) * 22f * (seed == 3 ? 1.2f : 1f);
                float l = len * (0.5f + 0.5f * G.Hash(i, seed, 8)) * (i == rays / 2 ? 1f : 0.6f);
                var d = G.Dir(a);
                p.Poly(y, 0.0f, -wid * 0.35f, d.x * l, d.y * l - 0.02f, d.x * l, d.y * l + 0.02f, 0, wid * 0.35f);
            }
            p.Poly(w, 0, -wid * 0.25f, len * 0.5f, 0, 0, wid * 0.25f);
            p.Circle(w, 0, 0, wid * 0.3f);
            return p.MakeAt(-p.HalfW + 0.2f, 0);
        }
    }
}
