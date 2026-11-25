using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class Art
{
    // ================================================================== SOLDIERS
    public const int FR_IDLE = 0, FR_WALK1 = 1, FR_WALK2 = 2, FR_RUN1 = 3, FR_RUN2 = 4, FR_CROUCH = 5, FR_SHOOT = 6, FR_RELOAD = 7, FR_HIT = 8, FR_OPER = 9, FR_DIE1 = 10, FR_DIE2 = 11, FR_DEAD = 12, FR_COUNT = 13;

    static Model BuildSoldier(UnitDef d, Pal pal, bool icon)
    {
        var m = new Model();
        m.fr = new Sprite[FR_COUNT];
        int n = icon ? 1 : 10;
        Role role = d.role;
        for (int i = 0; i < n; i++)
        {
            var p = new Pix(1.3f, 1.3f, icon ? 36f : 44f);
            DrawStand(p, pal, role, i);
            m.fr[i] = p.Make();
        }
        if (icon) { m.icon = m.fr[0]; return m; }
        var st = new Pix(1.3f, 1.3f, 44f);
        DrawStand(st, pal, role, 5);
        for (int k = 0; k < 3; k++)
        {
            var p = new Pix(1.3f, 1.3f, 44f);
            if (k == 2) { p.Ell(new Color(0.35f, 0.02f, 0.02f, 0.75f), 0.0f, 0.0f, 0.36f, 0.3f); p.Ell(new Color(0.3f, 0.02f, 0.02f, 0.6f), 0.15f, 0.12f, 0.2f, 0.12f); }
            float[] rot = { 35f, 75f, 88f };
            p.Blit(st, k == 0 ? -0.05f : 0f, 0, rot[k]);
            if (k == 2) { var c = new Color(0.55f, 0.5f, 0.45f, 0.18f); p.Ell(c, 0, 0, 0.5f, 0.4f, 0, true); }
            m.fr[FR_DIE1 + k] = p.Make();
        }
        m.icon = m.fr[0];
        return m;
    }

    static void DrawStand(Pix p, Pal pal, Role role, int pose)
    {
        Color boot = G.Hex(0x23201c), pants = G.Mul(pal.sec, 0.95f), skin = pal.skin;
        bool crouch = pose == FR_CROUCH;
        float lg = pose == 1 ? 0.16f : pose == 2 ? -0.16f : pose == 3 ? 0.3f : pose == 4 ? -0.3f : crouch ? 0.04f : 0f;
        float bx = pose == FR_HIT ? -0.08f : crouch ? 0.03f : 0f;
        float k = crouch ? 0.9f : 1f;
        // legs
        p.Ell(pants, lg, 0.12f, 0.2f * k, 0.085f); p.Ell(pants, -lg, -0.12f, 0.2f * k, 0.085f);
        p.Ell(boot, lg + 0.16f * k, 0.12f, 0.085f, 0.075f); p.Ell(boot, -lg + 0.16f * k, -0.12f, 0.085f, 0.075f);
        // pack / role gear
        Color pack = G.Mul(pal.dark, 0.9f);
        switch (role)
        {
            case Role.Medic: p.Ell(G.Hex(0xe8e8e0), bx - 0.2f, 0, 0.12f, 0.22f); p.Rect(G.Hex(0xcc2222), bx - 0.2f, 0, 0.18f, 0.06f); p.Rect(G.Hex(0xcc2222), bx - 0.2f, 0, 0.06f, 0.18f); break;
            case Role.Para: p.Ell(G.Hex(0x4a5a3a), bx - 0.22f, 0, 0.15f, 0.25f); p.Line(G.Hex(0x303a28), bx - 0.3f, -0.2f, bx - 0.1f, 0.2f, 0.03f); break;
            case Role.Operator: p.Ell(pack, bx - 0.2f, 0, 0.12f, 0.2f); p.Line(G.Hex(0x202020), bx - 0.25f, 0.1f, bx - 0.6f, 0.2f, 0.025f); p.Circle(G.Hex(0xc03030), bx - 0.6f, 0.2f, 0.025f); break;
            case Role.Engineer: p.Ell(pack, bx - 0.2f, 0, 0.12f, 0.21f); p.Line(G.Hex(0x888880), bx - 0.15f, -0.18f, bx - 0.5f, -0.28f, 0.045f); p.Rect(G.Hex(0x6a5a3a), bx - 0.52f, -0.29f, 0.14f, 0.1f); break;
            case Role.Pilot: p.Ell(G.Hex(0x3a4a2a), bx - 0.2f, 0, 0.12f, 0.2f); break;
            case Role.AT: p.Ell(pack, bx - 0.18f, 0, 0.11f, 0.2f); p.Line(G.Hex(0x4a5238), bx - 0.5f, 0.2f, bx + 0.3f, 0.2f, 0.09f); break;
            case Role.Naval: p.Ell(G.Hex(0x1c1f24), bx - 0.2f, 0, 0.12f, 0.21f); break;
            default: p.Ell(pack, bx - 0.2f, 0, 0.12f, 0.2f); break;
        }
        // torso
        Color body = role == Role.Pilot ? G.Hex(0xd86a18) : pal.main;
        p.Ell(body, bx, 0, 0.15f * k, 0.27f);
        if (role != Role.Pilot) Camo(p, pal, 51, 0.5f);
        Color vest = role == Role.Recon || role == Role.Sniper ? G.Mul(pal.sec, 0.8f) : G.Mul(pal.dark, 0.85f);
        if (role != Role.Pilot) p.Ell(vest, bx + 0.02f, 0, 0.1f, 0.2f);
        if (role == Role.Mech || role == Role.Crew) p.Ell(G.Mul(pal.dark, 0.7f), bx + 0.02f, 0, 0.1f, 0.2f);
        if (role == Role.Recon || role == Role.Sniper) for (int i = 0; i < 10; i++) p.Circle(i % 2 == 0 ? G.Hex(0x4a5a30) : G.Hex(0x6a7a3a), bx + (G.Hash(i, 1, 5) - 0.5f) * 0.3f, (G.Hash(i, 2, 5) - 0.5f) * 0.5f, 0.05f);
        for (int i = 0; i < 3; i++) p.Rect(G.Mul(pal.dark, 0.5f), bx + 0.1f, -0.1f + i * 0.1f, 0.05f, 0.07f);
        // faction ID armband + patch
        FacMark(p, pal, bx + 0.0f, 0.255f, 0.1f, 0.09f);
        p.Rect(pal.accent, bx + 0.0f, -0.255f, 0.07f, 0.07f);
        // weapon & arms
        float wl = 0.62f; Color wc = G.Hex(0x1e1f1b);
        Vector2 h1, h2; // right hand (rear grip), left hand (fore grip)
        Vector2 muz;
        switch (pose)
        {
            case FR_SHOOT: h1 = new Vector2(bx + 0.28f, -0.05f); h2 = new Vector2(bx + 0.5f, 0.0f); break;
            case FR_RELOAD: h1 = new Vector2(bx + 0.22f, -0.12f); h2 = new Vector2(bx + 0.26f, 0.1f); break;
            case FR_HIT: h1 = new Vector2(bx + 0.1f, -0.42f); h2 = new Vector2(bx + 0.05f, 0.42f); break;
            case FR_OPER: h1 = new Vector2(bx + 0.32f, -0.1f); h2 = new Vector2(bx + 0.32f, 0.1f); break;
            default: h1 = new Vector2(bx + 0.26f, -0.1f); h2 = new Vector2(bx + 0.44f, 0.08f); break;
        }
        Color arm = G.Mul(body, 0.95f);
        p.Line(arm, bx + 0.02f, -0.24f, h1.x, h1.y, 0.1f); p.Line(arm, bx + 0.02f, 0.24f, h2.x, h2.y, 0.1f);
        if (pose == FR_OPER)
        {
            p.Rect(G.Hex(0x2a2c30), bx + 0.38f, 0, 0.2f, 0.3f); p.Rect(G.Hex(0x3a8ad0), bx + 0.38f, 0, 0.15f, 0.23f); p.Rect(G.Hex(0x9ad0ff, 0.6f), bx + 0.4f, 0.04f, 0.09f, 0.06f);
            p.Line(G.Hex(0x303030), bx + 0.3f, 0.12f, bx + 0.24f, 0.28f, 0.03f);
        }
        else if (role == Role.Medic && pose != FR_SHOOT) { p.Rect(G.Hex(0xe8e8e0), bx + 0.34f, 0, 0.12f, 0.14f); p.Rect(G.Hex(0xcc2222), bx + 0.34f, 0, 0.09f, 0.03f); p.Rect(G.Hex(0xcc2222), bx + 0.34f, 0, 0.03f, 0.09f); }
        else if (pose != FR_HIT)
        {
            switch (role)
            {
                case Role.MG: wl = 0.78f; p.Line(wc, h1.x - 0.1f, -0.03f, h1.x - 0.1f + wl, 0, 0.08f); p.Line(wc, h1.x + 0.5f, 0.0f, h1.x + 0.64f, 0.14f, 0.025f); p.Line(wc, h1.x + 0.5f, 0.0f, h1.x + 0.64f, -0.14f, 0.025f); p.Rect(G.Hex(0x4a4d3c), h1.x + 0.05f, -0.08f, 0.14f, 0.1f); break;
                case Role.Sniper: wl = 0.96f; p.Line(wc, h1.x - 0.12f, -0.03f, h1.x - 0.12f + wl, 0, 0.055f); p.Rect(G.Hex(0x15150f), h1.x + 0.15f, 0.0f, 0.2f, 0.06f); break;
                case Role.AT: p.Line(G.Hex(0x4a5238), h1.x - 0.25f, 0.0f, h1.x + 0.55f, 0.0f, 0.12f); p.Circle(G.Hex(0x303428), h1.x + 0.55f, 0.0f, 0.075f); break;
                case Role.AA: p.Line(G.Hex(0x7a7e6a), h1.x - 0.2f, 0.0f, h1.x + 0.55f, 0.0f, 0.13f); p.Circle(G.Hex(0xe0e0d8), h1.x + 0.55f, 0.0f, 0.08f); break;
                case Role.Operator: case Role.Crew: case Role.Medic: case Role.Pilot: p.Line(wc, h1.x, -0.03f, h1.x + 0.2f, 0, 0.05f); break;
                default: p.Line(wc, h1.x - 0.1f, -0.03f, h1.x - 0.1f + wl, 0, 0.06f); p.Rect(G.Hex(0x4a4d3c), h1.x + 0.08f, -0.06f, 0.1f, 0.07f); break;
            }
        }
        p.Circle(skin, h1.x, h1.y, 0.05f); p.Circle(skin, h2.x, h2.y, 0.05f);
        // head & headgear
        float hx = bx + 0.04f;
        switch (role)
        {
            case Role.Para: p.Circle(pal.helmet, hx, 0, 0.16f); p.Circle(G.Mul(pal.helmet, 1.15f), hx + 0.02f, 0, 0.11f); p.Line(G.Hex(0x202020), hx + 0.1f, -0.1f, hx + 0.1f, 0.1f, 0.025f); break;
            case Role.Naval: p.Ell(G.Hex(0x14171b), hx, 0, 0.17f, 0.16f); p.Circle(pal.accent, hx + 0.08f, 0.07f, 0.03f); break;
            case Role.Recon: case Role.Sniper: p.Circle(G.Hex(0x6a7248), hx, 0, 0.19f); p.Circle(G.Hex(0x585f3a), hx, 0, 0.12f); for (int i = 0; i < 6; i++) p.Circle(G.Hex(0x8a9258), hx + (G.Hash(i, 3, 7) - 0.5f) * 0.3f, (G.Hash(i, 4, 7) - 0.5f) * 0.3f, 0.025f); break;
            case Role.Crew: p.Circle(G.Hex(0x1f2630), hx, 0, 0.16f); p.Line(G.Hex(0x303a48), hx - 0.1f, 0, hx + 0.1f, 0, 0.06f); p.Rect(G.Hex(0x9ac0d0), hx + 0.12f, 0, 0.04f, 0.14f); break;
            case Role.Pilot: p.Circle(G.Hex(0xe8e8e0), hx, 0, 0.17f); p.Rect(G.Hex(0x22303a), hx + 0.1f, 0, 0.06f, 0.14f); break;
            case Role.Operator: p.Circle(pal.helmet, hx, 0, 0.15f); p.Ell(G.Hex(0x202020), hx, 0.14f, 0.05f, 0.05f); p.Ell(G.Hex(0x202020), hx, -0.14f, 0.05f, 0.05f); p.Line(G.Hex(0x202020), hx, 0.14f, hx, -0.14f, 0.025f); break;
            case Role.Medic: p.Circle(pal.helmet, hx, 0, 0.16f); p.Rect(G.Hex(0xeeeeea), hx, 0, 0.12f, 0.12f); p.Rect(G.Hex(0xcc2222), hx, 0, 0.09f, 0.03f); p.Rect(G.Hex(0xcc2222), hx, 0, 0.03f, 0.09f); break;
            default: p.Circle(pal.helmet, hx, 0, 0.16f); Camo(p, pal, 55, 0.3f); p.Ell(G.Mul(pal.helmet, 0.7f), hx, 0, 0.16f, 0.16f, 0, true, 12); p.Circle(pal.helmet, hx, 0, 0.14f); p.Rect(pal.accent, hx + 0.09f, 0, 0.05f, 0.16f, 0, true); break;
        }
        if (role == Role.Engineer) p.Rect(G.Hex(0xe0b020), hx - 0.02f, 0, 0.05f, 0.2f, 0, true);
        // unit emblem on the pack (identifies brigade)
        if (role != Role.Medic && role != Role.Pilot) Emblem(p, pal.emblem, bx - 0.2f, 0, 0.19f, pal.accent, pal.dark, 90);
    }

    // ================================================================== AIRCRAFT
    class PS
    {
        public float L, fw, wxr, wcr, wct, wh, wsw, txr, tcr, tct, th, tsw, canopy;
        public int fins, engs, props, canard; public float finY, engY, engX, noseK = 0.7f, tailK = 0.3f;
        public bool pods;
    }
    static PS[] planeTbl;
    static void InitPlane()
    {
        if (planeTbl != null) return;
        planeTbl = new PS[] {
            /*0 MiG-29  */ new PS{ L=6.8f, fw=0.9f, wxr=0.8f, wcr=2.4f, wct=0.7f, wh=2.7f, wsw=1.4f, txr=-2.4f, tcr=1.3f, tct=0.5f, th=1.7f, tsw=0.6f, canopy=1.4f, fins=2, finY=0.65f, engs=2, engY=0.38f, engX=-3.4f },
            /*1 Su-27   */ new PS{ L=7.6f, fw=0.85f, wxr=1.0f, wcr=3.0f, wct=0.6f, wh=3.1f, wsw=1.6f, txr=-3.1f, tcr=1.3f, tct=0.5f, th=1.8f, tsw=0.7f, canopy=1.8f, fins=2, finY=1.0f, engs=2, engY=0.8f, engX=-3.8f, tailK=0.2f },
            /*2 F-16    */ new PS{ L=6.6f, fw=0.8f, wxr=0.5f, wcr=2.5f, wct=0.5f, wh=2.4f, wsw=1.9f, txr=-2.6f, tcr=1.4f, tct=0.5f, th=1.5f, tsw=0.8f, canopy=1.1f, fins=1, engs=1, engY=0, engX=-3.3f, noseK=0.85f },
            /*3 Su-35   */ new PS{ L=7.8f, fw=0.9f, wxr=1.0f, wcr=3.1f, wct=0.7f, wh=3.3f, wsw=1.3f, txr=-3.2f, tcr=1.4f, tct=0.55f, th=1.9f, tsw=0.6f, canopy=1.9f, fins=2, finY=1.05f, engs=2, engY=0.8f, engX=-3.9f, canard=1 },
            /*4 Su-57   */ new PS{ L=8.0f, fw=1.1f, wxr=0.9f, wcr=3.4f, wct=0.8f, wh=2.9f, wsw=1.6f, txr=-3.0f, tcr=1.8f, tct=0.7f, th=1.8f, tsw=1.1f, canopy=1.7f, fins=2, finY=0.9f, engs=2, engY=0.7f, engX=-4f, noseK=0.9f },
            /*5 MiG-31  */ new PS{ L=9.0f, fw=1.0f, wxr=0.7f, wcr=3.4f, wct=1.0f, wh=3.7f, wsw=1.8f, txr=-3.8f, tcr=1.8f, tct=0.7f, th=2.1f, tsw=0.6f, canopy=2.0f, fins=2, finY=1.1f, engs=2, engY=1.0f, engX=-4.5f, tailK=0.15f },
            /*6 Su-25   */ new PS{ L=7.0f, fw=0.8f, wxr=0.4f, wcr=1.8f, wct=1.2f, wh=3.7f, wsw=0.4f, txr=-3.0f, tcr=1.0f, tct=0.8f, th=1.8f, tsw=0.2f, canopy=1.6f, fins=2, finY=0.8f, engs=2, engY=0.7f, engX=-1.6f, noseK=0.5f, pods=true },
            /*7 Su-34   */ new PS{ L=8.4f, fw=1.3f, wxr=0.3f, wcr=3.2f, wct=0.7f, wh=3.7f, wsw=1.1f, txr=-3.3f, tcr=1.6f, tct=0.6f, th=2.1f, tsw=0.5f, canopy=2.4f, fins=1, engs=2, engY=0.6f, engX=-4.2f, canard=1, noseK=0.45f },
            /*8 Su-24   */ new PS{ L=8.2f, fw=1.1f, wxr=0.6f, wcr=3.0f, wct=2.0f, wh=3.8f, wsw=0.6f, txr=-3.4f, tcr=1.7f, tct=0.6f, th=2.0f, tsw=0.7f, canopy=2.0f, fins=1, engs=2, engY=0.55f, engX=-4.0f },
            /*9 Tu-22   */ new PS{ L=11f, fw=1.5f, wxr=0.5f, wcr=4.6f, wct=1.4f, wh=5.4f, wsw=3.0f, txr=-4.8f, tcr=2.0f, tct=0.8f, th=2.8f, tsw=1.2f, canopy=3.6f, fins=1, engs=2, engY=0.8f, engX=-5.2f, noseK=0.5f },
            /*10 Tu-95  */ new PS{ L=13f, fw=1.5f, wxr=1.3f, wcr=3.2f, wct=1.2f, wh=6.8f, wsw=2.6f, txr=-5.2f, tcr=2.0f, tct=1.0f, th=3.2f, tsw=0.9f, canopy=4.6f, fins=1, engs=4, props=4, engY=1.9f, engX=0.9f, noseK=0.4f },
            /*11 Tu-160 */ new PS{ L=14f, fw=1.8f, wxr=1.0f, wcr=5.4f, wct=1.2f, wh=6.4f, wsw=4.0f, txr=-6.3f, tcr=2.6f, tct=1.0f, th=3.4f, tsw=1.4f, canopy=5.2f, fins=1, engs=4, engY=1.2f, engX=-5.2f, noseK=0.45f },
            /*12 Il-76  */ new PS{ L=12f, fw=1.9f, wxr=1.2f, wcr=3.1f, wct=1.3f, wh=6.0f, wsw=1.5f, txr=-5.0f, tcr=2.0f, tct=1.2f, th=3.0f, tsw=0.8f, canopy=4.4f, fins=1, engs=4, engY=2.4f, engX=0.4f, noseK=0.35f },
            /*13 An-26  */ new PS{ L=8.5f, fw=1.3f, wxr=1.0f, wcr=2.2f, wct=1.0f, wh=5.5f, wsw=0.2f, txr=-3.6f, tcr=1.4f, tct=0.9f, th=2.3f, tsw=0.3f, canopy=3.2f, fins=1, engs=2, props=2, engY=1.9f, engX=1.2f, noseK=0.4f },
            /*14 C-130  */ new PS{ L=11f, fw=1.7f, wxr=1.5f, wcr=2.7f, wct=2.1f, wh=6.5f, wsw=0.3f, txr=-4.7f, tcr=1.9f, tct=1.3f, th=2.9f, tsw=0.2f, canopy=4.3f, fins=1, engs=4, props=4, engY=1.9f, engX=1.2f, noseK=0.4f },
            /*15 An-124 */ new PS{ L=16f, fw=2.4f, wxr=1.5f, wcr=4.6f, wct=1.6f, wh=8.0f, wsw=2.4f, txr=-7.0f, tcr=2.6f, tct=1.4f, th=3.8f, tsw=1.4f, canopy=6.4f, fins=2, finY=1.3f, engs=4, engY=2.8f, engX=0.6f, noseK=0.35f },
            /*16 An-30  */ new PS{ L=9.0f, fw=1.3f, wxr=1.0f, wcr=2.0f, wct=1.2f, wh=5.5f, wsw=0.1f, txr=-3.8f, tcr=1.5f, tct=1.0f, th=2.4f, tsw=0.2f, canopy=3.4f, fins=1, engs=2, props=2, engY=1.9f, engX=1.3f, noseK=0.4f },
            /*17 Il-20  */ new PS{ L=11.5f, fw=1.7f, wxr=1.3f, wcr=3.0f, wct=1.6f, wh=6.2f, wsw=1.0f, txr=-4.8f, tcr=1.8f, tct=1.0f, th=2.8f, tsw=0.6f, canopy=4.5f, fins=1, engs=4, props=4, engY=2.0f, engX=1.2f, noseK=0.35f },
            /*18 Su-24MR*/ new PS{ L=8.2f, fw=1.1f, wxr=0.6f, wcr=2.8f, wct=1.6f, wh=3.6f, wsw=0.5f, txr=-3.4f, tcr=1.7f, tct=0.6f, th=2.0f, tsw=0.7f, canopy=2.0f, fins=1, engs=2, engY=0.55f, engX=-4.0f, pods=true },
        };
    }

    static Model BuildPlane(UnitDef d, Pal pal, bool icon)
    {
        InitPlane();
        var s = planeTbl[Mathf.Clamp(d.style, 0, planeTbl.Length - 1)];
        var m = new Model();
        float ppu = icon ? 6f : 20f;
        if (d.len < 8 && !icon) ppu = 24;
        float span = Mathf.Max(d.wid, s.wh * 2f) + 1f;
        var p = new Pix(s.L + 1f, span, ppu);
        Color body = pal.main, wing = G.Mul(pal.main, 0.93f), dk = G.Mul(pal.main, 0.62f), metal = G.Hex(0x2d2f2c);
        Color grey = G.Mul(pal.main, 1.0f);
        if (d.special == "transport" || d.style >= 12) { body = G.Mul(Color.Lerp(pal.main, G.Hex(0x9aa0a0), 0.45f), 1f); wing = G.Mul(body, 0.95f); }
        float hl = s.L / 2f;
        // wings
        float wx = s.wxr - 0.0f;
        bool high = d.style >= 12;
        Func<float, float, float, float, float, float, float[]> WingPoly = (xr, cr, ct, h, sw, sign) =>
            new float[] { xr, 0, xr - sw, sign * h, xr - sw - ct, sign * h, xr - cr, 0 };
        float[] wp = { s.wxr, -0.2f, s.wxr - s.wsw, -s.wh, s.wxr - s.wsw - s.wct, -s.wh, s.wxr - s.wcr, -0.2f, s.wxr - s.wcr, 0.2f, s.wxr - s.wsw - s.wct, s.wh, s.wxr - s.wsw, s.wh, s.wxr, 0.2f };
        p.Poly(wing, wp);
        float[] tp = { s.txr, -0.15f, s.txr - s.tsw, -s.th, s.txr - s.tsw - s.tct, -s.th, s.txr - s.tcr, -0.15f, s.txr - s.tcr, 0.15f, s.txr - s.tsw - s.tct, s.th, s.txr - s.tsw, s.th, s.txr, 0.15f };
        p.Poly(G.Mul(wing, 0.95f), tp);
        if (s.canard > 0) { float[] cp = { hl * 0.45f, 0, hl * 0.3f, 1.2f, hl * 0.22f, 1.2f, hl * 0.28f, 0, hl * 0.3f, -1.2f, hl * 0.22f, -1.2f }; p.Poly(wing, new float[] { hl * 0.45f, 0.2f, hl * 0.32f, 1.2f, hl * 0.22f, 1.2f, hl * 0.25f, 0.2f }); p.Poly(wing, new float[] { hl * 0.45f, -0.2f, hl * 0.32f, -1.2f, hl * 0.22f, -1.2f, hl * 0.25f, -0.2f }); }
        // fuselage
        float fw = s.fw, nose = hl, tail = -hl;
        float[] fu = { nose, 0, nose - s.L * (1f - s.noseK) * 0.5f, fw * 0.28f, nose - s.L * 0.35f, fw * 0.5f, tail + s.L * 0.2f, fw * 0.5f, tail, fw * s.tailK + 0.1f, tail, -fw * s.tailK - 0.1f, tail + s.L * 0.2f, -fw * 0.5f, nose - s.L * 0.35f, -fw * 0.5f, nose - s.L * (1f - s.noseK) * 0.5f, -fw * 0.28f };
        // engines housed (nacelles) for multi-engine
        if (s.engs >= 2 && s.engY > 0.9f)
        {
            for (int i = 0; i < s.engs; i++)
            {
                float y = (i % 2 == 0 ? 1 : -1) * s.engY * (i < 2 ? 1f : 1.9f / 1f * 0.0f + 1.0f);
                if (s.engs == 4) y = (i % 2 == 0 ? 1 : -1) * s.engY * (i < 2 ? 1f : 1.75f);
                p.Rect(G.Mul(body, 0.88f), s.engX, y, 2.2f, 0.55f);
                p.Rect(metal, s.engX - 1.0f, y, 0.2f, 0.45f);
                p.Rect(G.Mul(body, 1.1f), s.engX + 1.15f, y, 0.18f, 0.42f);
            }
        }
        p.Poly(body, fu);
        Camo(p, pal, 61, 1.1f);
        p.Outline(dk, fu, 0.04f, true);
        // intakes / cockpit
        if (s.engs >= 2 && s.engY <= 0.9f)
        {
            p.Rect(G.Mul(dk, 0.7f), s.canopy * 0.0f + hl * 0.1f, s.engY, 1.4f, 0.3f); p.Rect(G.Mul(dk, 0.7f), hl * 0.1f, -s.engY, 1.4f, 0.3f);
        }
        if (s.engs == 1) p.Ell(G.Mul(dk, 0.6f), hl * 0.05f, 0, 0.9f, 0.3f);
        float cx = hl - s.canopy * 0.5f - 0.7f; if (s.L > 11f) cx = hl - 1.3f;
        p.Ell(pal.glass, cx, 0, Mathf.Min(1.0f, 0.5f + s.L * 0.04f), fw * 0.22f);
        p.Ell(new Color(0.7f, 0.85f, 0.95f, 0.35f), cx + 0.15f, 0.05f, 0.35f, fw * 0.1f);
        p.Line(G.Mul(dk, 0.5f), -hl + 1.0f, 0, hl * 0.2f, 0, 0.03f);
        // fins
        if (s.fins == 1) { p.Rect(G.Mul(body, 0.8f), -hl + 1.2f, 0, 1.9f, 0.14f); p.Rect(pal.accent, -hl + 0.9f, 0, 0.5f, 0.15f); Emblem(p, pal.emblem, -hl + 2.3f, 0, 0.55f, pal.accent, dk, 0); }
        else if (s.fins == 2) { for (int sg = -1; sg <= 1; sg += 2) { p.Rect(G.Mul(body, 0.8f), -hl + 1.3f, sg * s.finY, 2.0f, 0.13f); p.Rect(pal.accent, -hl + 0.8f, sg * s.finY, 0.4f, 0.14f); } Emblem(p, pal.emblem, -hl * 0.55f, 0, 0.6f, pal.accent, dk, 0); }
        // pylons / pods
        if (s.pods) for (int sg = -1; sg <= 1; sg += 2) for (int i = 0; i < 3; i++) p.Rect(G.Hex(0x3a3d34), s.wxr - 1.2f, sg * (1.0f + i * 0.9f), 0.7f, 0.22f);
        // roundels (national markings) on wings
        for (int sg = -1; sg <= 1; sg += 2)
        {
            float rx = s.wxr - s.wcr * 0.55f, ry = sg * s.wh * 0.62f;
            if (pal.fac == Faction.Ukraine) { p.Circle(G.Hex(0xffd500), rx, ry, 0.5f); p.Circle(G.Hex(0x1f6fd6), rx, ry, 0.34f); }
            else { p.Circle(G.Hex(0xe8e8e8), rx, ry, 0.5f); p.Circle(G.Hex(0x2a4fc8), rx, ry, 0.38f); p.Circle(G.Hex(0xd02a2a), rx, ry, 0.24f); }
            if (s.wh * 0.62f < 0.8f) break;
        }
        for (int i = 0; i < pal.bars; i++) p.Rect(pal.accent, hl * 0.35f - i * 0.2f, 0, 0.1f, fw * 0.55f);
        // engine nozzles
        var eng = new List<Vector2>();
        if (s.props > 0)
        {
            for (int i = 0; i < s.props; i++) { float y = (i % 2 == 0 ? 1 : -1) * s.engY * (i < 2 ? 1f : 1.85f); if (s.props == 2) y = (i % 2 == 0 ? 1 : -1) * s.engY; eng.Add(new Vector2(s.engX + 1.2f, y)); p.Rect(G.Mul(body, 0.85f), s.engX, y, 2.3f, 0.5f); p.Circle(metal, s.engX + 1.2f, y, 0.12f); }
        }
        else if (s.engs == 1) { eng.Add(new Vector2(-hl + 0.05f, 0)); p.Circle(metal, -hl + 0.12f, 0, 0.32f); p.Circle(G.Hex(0x5a3a22), -hl + 0.12f, 0, 0.2f); }
        else
        {
            for (int i = 0; i < s.engs; i++)
            {
                float y = (i % 2 == 0 ? 1 : -1) * s.engY * (s.engs == 4 && i >= 2 ? 1.75f : 1f);
                if (s.engY > 0.9f) { eng.Add(new Vector2(s.engX - 1.1f, y)); p.Circle(metal, s.engX - 1.05f, y, 0.26f); }
                else { eng.Add(new Vector2(-hl + 0.05f, y)); p.Circle(metal, -hl + 0.12f, y, 0.3f); p.Circle(G.Hex(0x5a3a22), -hl + 0.12f, y, 0.18f); }
            }
        }
        if (icon) { m.icon = p.Make(); return m; }
        m.body = p.Make();
        m.shadow = p.Silhouette(new Color(0, 0, 0, 0.32f)).Make();
        m.eng = eng.ToArray();
        m.engSize = s.props > 0 ? 0.0f : 0.5f + s.L * 0.04f;
        if (s.props > 0)
        {
            // propeller sprites
            var pp = new Pix(2.0f, 2.0f, 22);
            for (int b2 = 0; b2 < 4; b2++) { float a = b2 * 90f; var v = G.Dir(a); pp.Line(G.Hex(0x1c1c1a), 0, 0, v.x * 0.95f, v.y * 0.95f, 0.1f); pp.Circle(G.Hex(0x8a8a80), v.x * 0.9f, v.y * 0.9f, 0.04f); }
            pp.Circle(G.Hex(0x707068), 0, 0, 0.12f);
            m.prop = pp.Make();
            var pb = new Pix(2.0f, 2.0f, 22); pb.Circle(new Color(0.75f, 0.78f, 0.8f, 0.2f), 0, 0, 0.95f); m.propBlur = pb.Make();
            m.propBlades = 4;
        }
        return m;
    }

    // ================================================================== HELICOPTERS
    static Model BuildHeli(UnitDef d, Pal pal, bool icon)
    {
        var m = new Model();
        float ppu = icon ? 7f : 22f;
        int st = d.style;
        // style table: length, cabin width, tail length, nose type, tandem, wings, rotors, rotor radius
        float L = d.len, bw = 1.2f, tail = L * 0.45f, rr = d.wid * 0.5f;
        bool tandem = false, attack = st <= 3, coax = false, twin = false, medic = d.special == "medic";
        float wing = 0;
        switch (st)
        {
            case 0: bw = 1.5f; wing = 1.9f; tail = 3.0f; break;               // Mi-24
            case 1: bw = 1.15f; wing = 1.6f; tail = 2.9f; tandem = true; break; // Mi-28
            case 2: bw = 1.5f; wing = 1.7f; tail = 1.6f; coax = true; break;    // Ka-52
            case 3: bw = 1.1f; wing = 1.8f; tail = 3.0f; tandem = true; break;  // Apache
            case 4: bw = 1.9f; tail = 3.4f; break;                              // Mi-8
            case 5: bw = 2.6f; tail = 4.6f; break;                              // Mi-26
            case 6: bw = 1.8f; tail = 0f; twin = true; break;                   // Chinook
            case 7: bw = 1.5f; tail = 2.6f; break;                              // Black Hawk
            case 8: bw = 1.7f; tail = 1.5f; coax = true; break;                 // Ka-29
            case 9: bw = 1.9f; tail = 3.3f; wing = 1.0f; break;                 // Mi-17 multirole
            case 10: bw = 1.9f; tail = 3.4f; break;                             // Mi-8 medevac
            default: bw = 1.5f; tail = 2.6f; break;                             // UH-60 medevac
        }
        Color body = medic ? G.Mul(Color.Lerp(pal.main, Color.white, 0.8f), 1f) : pal.main;
        if (st >= 4 && !medic) body = G.Mul(Color.Lerp(pal.main, G.Hex(0x7a8070), 0.2f), 1f);
        Color dk = G.Mul(body, 0.6f), glass = pal.glass;
        float cl = L - tail * 0.8f; // cabin length
        var p = new Pix(L + 1.5f, Mathf.Max(bw, wing * 2.2f) + 1.2f, ppu);
        float noseX = L / 2f - 0.5f;
        // tail boom
        if (!twin)
        {
            float tx0 = -noseX + cl * 0.2f;
            p.Poly(G.Mul(body, 0.92f), new float[] { tx0 + 0.5f, -bw * 0.28f, -noseX - tail + 0.5f, -0.12f, -noseX - tail + 0.5f, 0.12f, tx0 + 0.5f, bw * 0.28f });
            p.Rect(G.Mul(body, 0.85f), -noseX - tail + 0.7f, 0.0f, 0.5f, 0.9f);   // horizontal stabiliser
            p.Rect(dk, -noseX - tail + 0.55f, 0.0f, 0.65f, 0.14f);
            m.tailRotPos = new Vector2(-noseX - tail + 0.5f, 0.28f); m.tailRotR = 0.7f;
        }
        // stub wings
        if (wing > 0)
        {
            p.Rect(G.Mul(body, 0.9f), 0.2f, 0, 0.9f, wing * 2f);
            for (int sg = -1; sg <= 1; sg += 2) { p.Rect(G.Hex(0x30332d), 0.2f, sg * wing * 0.75f, 0.9f, 0.45f); p.Rect(G.Hex(0x3a3d34), 0.7f, sg * wing * 0.75f, 0.4f, 0.12f); p.Circle(G.Hex(0x15150f), 0.7f + 0.2f, sg * wing * 0.75f, 0.06f); }
        }
        // fuselage
        if (twin)
        {
            p.Poly(body, new float[] { noseX, -0.5f, noseX - 1.3f, -bw * 0.5f, -noseX + 1.2f, -bw * 0.5f, -noseX, -bw * 0.35f, -noseX, bw * 0.35f, -noseX + 1.2f, bw * 0.5f, noseX - 1.3f, bw * 0.5f, noseX, 0.5f });
            p.Rect(dk, -noseX + 0.4f, 0, 1.0f, bw * 0.6f);   // rear ramp
            p.Poly(glass, new float[] { noseX - 0.1f, -0.35f, noseX - 0.8f, -0.45f, noseX - 0.8f, 0.45f, noseX - 0.1f, 0.35f });
        }
        else if (attack)
        {
            p.Poly(body, new float[] { noseX, 0, noseX - 0.5f, -bw * 0.4f, noseX - 1.2f, -bw * 0.5f, -noseX + 1f, -bw * 0.4f, -noseX, -bw * 0.15f, -noseX, bw * 0.15f, -noseX + 1f, bw * 0.4f, noseX - 1.2f, bw * 0.5f, noseX - 0.5f, bw * 0.4f });
            if (tandem) { p.Ell(glass, noseX - 0.8f, 0, 0.4f, bw * 0.28f); p.Ell(glass, noseX - 1.9f, 0, 0.4f, bw * 0.3f); }
            else { p.Ell(glass, noseX - 0.8f, -bw * 0.18f, 0.38f, bw * 0.2f); p.Ell(glass, noseX - 0.8f, bw * 0.18f, 0.38f, bw * 0.2f); }
            p.Rect(G.Hex(0x1c1c1a), noseX - 0.2f, 0, 0.4f, 0.16f);   // chin gun
            for (int sg = -1; sg <= 1; sg += 2) p.Rect(dk, -0.8f, sg * bw * 0.35f, 1.1f, 0.3f);   // intakes
        }
        else
        {
            p.Poly(body, new float[] { noseX, 0, noseX - 0.4f, -bw * 0.38f, noseX - 1.2f, -bw * 0.5f, -noseX + 0.6f, -bw * 0.45f, -noseX, -bw * 0.18f, -noseX, bw * 0.18f, -noseX + 0.6f, bw * 0.45f, noseX - 1.2f, bw * 0.5f, noseX - 0.4f, bw * 0.38f });
            p.Poly(glass, new float[] { noseX - 0.05f, 0, noseX - 0.35f, -bw * 0.3f, noseX - 1.0f, -bw * 0.34f, noseX - 1.0f, bw * 0.34f, noseX - 0.35f, bw * 0.3f });
            for (int i = 0; i < 4; i++) p.Rect(pal.glass, -0.4f + i * 0.5f, bw * 0.46f, 0.3f, 0.06f);
            p.Rect(dk, -noseX + 0.55f, 0, 0.5f, bw * 0.7f);
            if (st == 4 || st == 9 || st == 10) for (int sg = -1; sg <= 1; sg += 2) p.Rect(G.Mul(body, 0.8f), -0.3f, sg * (bw * 0.5f + 0.15f), 1.0f, 0.3f);   // engines/tanks
        }
        Camo(p, pal, 71, 1f);
        p.Line(dk, -noseX + 1.5f, 0, noseX - 1.5f, 0, 0.04f);
        if (medic) { p.Rect(G.Hex(0xcc2222), -0.3f, 0, 1.1f, 0.3f); p.Rect(G.Hex(0xcc2222), -0.3f, 0, 0.3f, 1.1f); }
        Emblem(p, pal.emblem, -noseX * 0.3f - 0.4f, 0, 0.55f, pal.accent, dk, 0);
        FacMark(p, pal, -noseX - tail * 0.5f, -0.0f, 0.5f, 0.25f);
        for (int i = 0; i < pal.bars; i++) p.Rect(pal.accent, noseX - 2.3f - i * 0.12f, 0, 0.06f, bw * 0.5f);
        // engine exhaust positions
        m.eng = new Vector2[] { new Vector2(-1.0f, bw * 0.35f), new Vector2(-1.0f, -bw * 0.35f) };
        if (icon)
        {
            DrawRotor(p, rr * 0.9f, 0, 0, st >= 5 && st != 7, true);
            m.icon = p.Make(); return m;
        }
        m.body = p.Make();
        m.shadow = p.Silhouette(new Color(0, 0, 0, 0.3f)).Make();
        // rotors
        var rp = new Pix(rr * 2f + 0.4f, rr * 2f + 0.4f, 16);
        DrawRotor(rp, rr, 0, 0, false, false);
        m.rotor = rp.Make();
        var rb = new Pix(rr * 2f + 0.4f, rr * 2f + 0.4f, 12);
        rb.Circle(new Color(0.7f, 0.74f, 0.76f, 0.16f), 0, 0, rr);
        rb.Circle(new Color(0.7f, 0.74f, 0.76f, 0.1f), 0, 0, rr * 0.55f);
        m.rotorBlur = rb.Make();
        m.rotorR = rr;
        if (twin) m.rotorPos = new Vector2[] { new Vector2(L * 0.28f, 0), new Vector2(-L * 0.28f, 0) };
        else m.rotorPos = new Vector2[] { new Vector2(attack && !coax ? 0.2f : 0f, 0) };
        var tr = new Pix(1.6f, 1.6f, 22);
        for (int i = 0; i < 4; i++) { var v = G.Dir(i * 90f); tr.Line(G.Hex(0x1e1e1c), 0, 0, v.x * 0.7f, v.y * 0.7f, 0.07f); }
        m.tailRotor = tr.Make();
        return m;
    }

    static void DrawRotor(Pix p, float rr, float cx, float cy, bool four, bool icon)
    {
        int blades = four ? 5 : 4;
        for (int i = 0; i < blades; i++)
        {
            float a = i * (360f / blades) + 18f; var v = G.Dir(a);
            p.Poly(new Color(0.12f, 0.12f, 0.11f, icon ? 0.5f : 1f), Tf(new float[] { 0.1f, -0.1f, rr, -0.07f, rr, 0.07f, 0.1f, 0.1f }, cx, cy, 1f, a));
        }
        p.Circle(G.Hex(0x303330), cx, cy, 0.25f); p.Circle(G.Hex(0x70746c), cx, cy, 0.12f);
    }

    // ================================================================== DRONES
    static Model BuildDrone(UnitDef d, Pal pal, bool icon)
    {
        var m = new Model();
        float ppu = icon ? 14f : (d.len < 2f ? 56f : 30f);
        var p = new Pix(Mathf.Max(d.len, d.wid) + 0.6f, Mathf.Max(d.len, d.wid) + 0.6f, ppu);
        Color bd = G.Mul(pal.main, 1.1f), dk = G.Mul(pal.main, 0.55f);
        var props = new List<Vector2>();
        float rotR = 0;
        switch (d.style)
        {
            case 0: // recon fixed-wing, high wing + twin boom
                p.Rect(G.Hex(0xe8e8e0), 0, 0, 2.2f, 0.22f); p.Ell(G.Hex(0xe8e8e0), 0.3f, 0, 0.6f, 0.17f);
                p.Rect(G.Hex(0xe8e8e0), 0.1f, 0, 0.5f, 3.2f); p.Rect(G.Hex(0xcfd0c8), -0.95f, 0, 0.4f, 1.1f); p.Rect(G.Hex(0xe8e8e0), -1.0f, 0, 0.1f, 0.5f);
                p.Rect(pal.accent, 0.1f, 0.9f, 0.12f, 0.8f); p.Rect(pal.accent, 0.1f, -0.9f, 0.12f, 0.8f);
                p.Circle(G.Hex(0x303030), -1.1f, 0, 0.16f); m.prop = null; props.Add(new Vector2(-1.15f, 0)); rotR = 0.38f;
                break;
            case 1: // mavic-style quad
                p.Rect(bd, 0, 0, 0.5f, 0.28f); p.Ell(G.Hex(0x303030), 0.25f, 0, 0.09f, 0.1f);
                for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2) { p.Line(dk, 0, 0, sx * 0.42f, sy * 0.42f, 0.07f); props.Add(new Vector2(sx * 0.42f, sy * 0.42f)); }
                rotR = 0.26f; break;
            case 2: // heavy quad
                p.Rect(bd, 0, 0, 0.9f, 0.5f); p.Rect(dk, -0.1f, 0, 0.5f, 0.35f); p.Rect(G.Hex(0x303030), 0.5f, 0, 0.18f, 0.22f);
                for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2) { p.Line(dk, 0, 0, sx * 0.75f, sy * 0.75f, 0.1f); props.Add(new Vector2(sx * 0.75f, sy * 0.75f)); }
                rotR = 0.42f; p.Circle(pal.accent, -0.1f, 0, 0.1f); break;
            case 3: case 4: // FPV
                {
                    float k = d.style == 4 ? 1.2f : 1f;
                    p.Rect(G.Hex(0x1a1a1a), 0, 0, 0.4f * k, 0.22f * k); p.Rect(G.Hex(0xc02020), -0.04f, 0, 0.26f * k, 0.12f * k); p.Rect(G.Hex(0x303030), 0.24f * k, 0, 0.1f, 0.12f);
                    for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2) { p.Line(G.Hex(0x222222), sx * 0.04f, sy * 0.04f, sx * 0.3f * k, sy * 0.3f * k, 0.05f); p.Circle(G.Hex(0x888888), sx * 0.3f * k, sy * 0.3f * k, 0.04f); props.Add(new Vector2(sx * 0.3f * k, sy * 0.3f * k)); }
                    p.Rect(pal.accent, -0.14f * k, 0, 0.05f, 0.2f * k);
                    rotR = 0.2f * k; break;
                }
            case 5: case 6: // MALE UCAV
                {
                    float L = d.len, sp = d.wid;
                    p.Ell(bd, 0, 0, L * 0.5f, 0.32f); p.Poly(bd, new float[] { 0.3f, -0.1f, -0.3f, -sp * 0.5f, -0.8f, -sp * 0.5f, -0.6f, -0.1f, -0.6f, 0.1f, -0.8f, sp * 0.5f, -0.3f, sp * 0.5f, 0.3f, 0.1f });
                    p.Line(dk, -L * 0.5f + 0.2f, 0, -L * 0.5f - 0.1f, 0.8f, 0.12f); p.Line(dk, -L * 0.5f + 0.2f, 0, -L * 0.5f - 0.1f, -0.8f, 0.12f);
                    p.Ell(G.Hex(0x303a40), L * 0.3f, 0, 0.35f, 0.14f);
                    for (int sg = -1; sg <= 1; sg += 2) p.Rect(G.Hex(0x30332d), -0.2f, sg * sp * 0.25f, 0.8f, 0.14f);
                    Camo(p, pal, 81, 1f); p.Circle(G.Hex(0x303030), -L * 0.5f, 0, 0.18f); props.Add(new Vector2(-L * 0.5f - 0.1f, 0)); rotR = 0.5f;
                    Emblem(p, pal.emblem, 0.0f, 0, 0.45f, pal.accent, dk, 0);
                    break;
                }
            case 7: // delta loiter drone
                p.Poly(bd, new float[] { 1.7f, 0, -1.5f, -1.3f, -1.7f, -1.3f, -1.5f, 0, -1.7f, 1.3f, -1.5f, 1.3f });
                p.Rect(dk, 0, 0, 1.0f, 0.3f); p.Rect(G.Hex(0xe8e8e0), -1.2f, 0, 0.7f, 0.25f); p.Rect(G.Hex(0xcc2222), -1.55f, -1.2f, 0.12f, 0.2f); p.Rect(G.Hex(0xcc2222), -1.55f, 1.2f, 0.12f, 0.2f);
                p.Circle(G.Hex(0x303030), -1.55f, 0, 0.14f); props.Add(new Vector2(-1.6f, 0)); rotR = 0.35f; break;
            case 8: // lancet-style X wing
                p.Ell(G.Hex(0xe8e8e0), 0, 0, 0.8f, 0.14f);
                for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2) { p.Poly(G.Hex(0xd8d8d0), new float[] { 0.1f * sx, 0.04f * sy, 0.35f * sx, 0.04f * sy, 0.25f * sx, 0.9f * sy, 0.05f * sx, 0.9f * sy }); p.Poly(G.Hex(0xcfcfc6), new float[] { -0.6f, 0.04f * sy, -0.8f, 0.04f * sy, -0.75f, 0.55f * sy, -0.62f, 0.55f * sy }); }
                p.Circle(pal.accent, 0.5f, 0, 0.08f); props.Add(new Vector2(-0.85f, 0)); rotR = 0.2f; break;
            default: // switchblade-like tube
                p.Ell(G.Hex(0xd0d0c8), 0, 0, 0.6f, 0.1f); for (int sg = -1; sg <= 1; sg += 2) p.Poly(G.Hex(0xc0c0b8), new float[] { 0.1f, 0.05f * sg, 0.3f, 0.05f * sg, 0.15f, 0.55f * sg, -0.05f, 0.55f * sg });
                p.Circle(pal.accent, 0.4f, 0, 0.06f); props.Add(new Vector2(-0.65f, 0)); rotR = 0.16f; break;
        }
        if (icon) { m.icon = p.Make(); return m; }
        m.body = p.Make();
        m.shadow = p.Silhouette(new Color(0, 0, 0, 0.3f)).Make();
        m.eng = props.ToArray();
        m.engSize = rotR;
        // rotor/propeller sprites
        var rp = new Pix(rotR * 2f + 0.1f, rotR * 2f + 0.1f, Mathf.Max(30f, 40f / Mathf.Max(0.2f, rotR * 2f)));
        bool multi = d.style == 1 || d.style == 2 || d.style == 3 || d.style == 4;
        for (int i = 0; i < 2; i++) { var v = G.Dir(i * 180f + 20f); rp.Line(G.Hex(0x181818), 0, 0, v.x * rotR, v.y * rotR, rotR * 0.14f); }
        rp.Circle(G.Hex(0x707068), 0, 0, rotR * 0.12f);
        m.prop = rp.Make();
        var rb = new Pix(rotR * 2f + 0.1f, rotR * 2f + 0.1f, Mathf.Max(30f, 40f / Mathf.Max(0.2f, rotR * 2f)));
        rb.Circle(new Color(0.75f, 0.78f, 0.8f, multi ? 0.24f : 0.2f), 0, 0, rotR);
        m.propBlur = rb.Make();
        m.propBlades = multi ? 4 : 1;
        return m;
    }

    // ================================================================== SHIPS
    static Model BuildShip(UnitDef d, Pal pal, bool icon)
    {
        var m = new Model();
        float ppu = icon ? 5f : 16f;
        float L = d.len, B = d.wid;
        var p = new Pix(L + 1.2f, B + 1.0f, ppu);
        Color hullc = G.Mul(Color.Lerp(pal.main, G.Hex(0x7c848a), 0.55f), 1f);
        if (pal.fac == Faction.Neutral || d.mask == 3 && d.style == 4) hullc = G.Hex(0x5a6068);
        Color deck = G.Mul(hullc, 1.12f), dk = G.Mul(hullc, 0.6f), metal = G.Hex(0x2c2f32), super = G.Mul(hullc, 1.3f);
        float hl = L / 2f, hb = B / 2f;
        // hull outline (bow to the right)
        float[] hp;
        switch (d.style)
        {
            case 2: hp = new float[] { hl, 0, hl - L * 0.28f, hb * 0.9f, hl - L * 0.5f, hb, -hl + 0.2f, hb, -hl, hb * 0.8f, -hl, -hb * 0.8f, -hl + 0.2f, -hb, hl - L * 0.5f, -hb, hl - L * 0.28f, -hb * 0.9f }; break;
            case 3: case 7: hp = new float[] { hl, hb * 0.5f, hl - 0.8f, hb, -hl, hb, -hl, -hb, hl - 0.8f, -hb, hl, -hb * 0.5f }; break;
            case 4: case 6: case 8: hp = new float[] { hl, 0, hl - L * 0.14f, hb * 0.8f, hl - L * 0.3f, hb, -hl + 0.3f, hb, -hl, hb * 0.85f, -hl, -hb * 0.85f, -hl + 0.3f, -hb, hl - L * 0.3f, -hb, hl - L * 0.14f, -hb * 0.8f }; break;
            default: hp = new float[] { hl + 0.2f, 0, hl - L * 0.2f, hb * 0.78f, hl - L * 0.4f, hb, -hl + 0.5f, hb, -hl, hb * 0.82f, -hl, -hb * 0.82f, -hl + 0.5f, -hb, hl - L * 0.4f, -hb, hl - L * 0.2f, -hb * 0.78f }; break;
        }
        p.Poly(dk, hp);
        var inner = new float[hp.Length];
        for (int i = 0; i < hp.Length; i += 2) { inner[i] = hp[i] * 0.94f; inner[i + 1] = hp[i + 1] * 0.86f; }
        p.Poly(hullc, inner);
        Camo(p, pal, 91, 1.4f);
        p.Poly(deck, Tf(inner, 0, 0, 0.93f, 0));
        p.Outline(G.Mul(hullc, 0.45f), hp, 0.07f, true);
        p.Line(G.Hex(0xe8e8e0, 0.6f), -hl + 0.3f, -hb * 0.0f, hl - L * 0.2f, 0, 0.03f);
        // deck features by style
        var mounts = new List<Vector3>();
        switch (d.style)
        {
            case 0: // frigate
                p.Rect(super, 0.1f, 0, L * 0.16f, B * 0.6f); p.Rect(pal.glass, 0.1f + L * 0.07f, 0, 0.12f, B * 0.5f);
                p.Rect(dk, -L * 0.2f, 0, L * 0.12f, B * 0.45f); p.Circle(G.Hex(0xc8c0a0), -L * 0.36f, 0, B * 0.3f); p.Rect(G.Hex(0x303030), -L * 0.36f, 0, B * 0.45f, 0.05f); p.Rect(G.Hex(0x303030), -L * 0.36f, 0, 0.05f, B * 0.45f);
                p.Rect(metal, L * 0.2f, 0, L * 0.1f, B * 0.35f); for (int i = 0; i < 6; i++) p.Circle(G.Hex(0x45494c), L * 0.17f + (i % 3) * 0.28f, (i / 3 - 0.5f) * 0.4f, 0.1f);
                p.Rect(G.Hex(0x5a5e60), -L * 0.08f, 0, 0.5f, 0.8f); p.Line(G.Hex(0x202020), 0.1f, 0, 0.1f, 0, 0.04f); p.Circle(G.Hex(0xa0a4a0), 0.1f, 0, 0.3f);
                mounts.Add(new Vector3(L * 0.34f, 0, 0)); mounts.Add(new Vector3(-L * 0.12f, 0, 1)); break;
            case 1: // corvette
                p.Poly(super, new float[] { -L * 0.15f, -B * 0.32f, L * 0.1f, -B * 0.22f, L * 0.15f, 0, L * 0.1f, B * 0.22f, -L * 0.15f, B * 0.32f });
                p.Rect(pal.glass, L * 0.07f, 0, 0.1f, B * 0.35f);
                for (int i = 0; i < 4; i++) { p.Line(G.Hex(0x808488), -L * 0.38f, -B * 0.28f + i * B * 0.19f, -L * 0.18f, -B * 0.28f + i * B * 0.19f, 0.16f); p.Poly(G.Hex(0xe0e0d8), new float[] { -L * 0.18f, -B * 0.28f + i * B * 0.19f - 0.07f, -L * 0.14f, -B * 0.28f + i * B * 0.19f, -L * 0.18f, -B * 0.28f + i * B * 0.19f + 0.07f }); }
                p.Circle(G.Hex(0xa0a4a0), -L * 0.02f, 0, 0.2f);
                mounts.Add(new Vector3(L * 0.3f, 0, 0)); mounts.Add(new Vector3(L * 0.0f, 0, 1)); break;
            case 2: // patrol boat
                p.Rect(super, -0.1f, 0, L * 0.3f, B * 0.55f); p.Rect(pal.glass, 0.5f * L * 0.15f, 0, 0.1f, B * 0.45f);
                p.Rect(G.Hex(0x303030), -L * 0.32f, 0, L * 0.12f, B * 0.5f); p.Rect(pal.accent, -0.1f, 0, 0.12f, B * 0.5f);
                mounts.Add(new Vector3(L * 0.32f, 0, 2)); mounts.Add(new Vector3(-L * 0.38f, 0, 2)); break;
            case 3: // amphibious assault
                p.Rect(G.Mul(deck, 1.02f), -L * 0.05f, 0, L * 0.6f, B * 0.72f);
                for (float x = -L * 0.32f; x < L * 0.22f; x += 0.9f) p.Line(G.Hex(0xe0e0d8, 0.5f), x, -B * 0.3f, x + 0.35f, -B * 0.3f, 0.04f);
                p.Rect(super, L * 0.2f, -B * 0.38f, L * 0.2f, B * 0.25f); p.Rect(pal.glass, L * 0.3f, -B * 0.38f, 0.1f, B * 0.2f); p.Circle(G.Hex(0xd8d8d0), -L * 0.15f, 0.0f, B * 0.22f); p.Rect(G.Hex(0x303030), -L * 0.15f, 0, B * 0.3f, 0.05f); p.Rect(G.Hex(0x303030), -L * 0.15f, 0, 0.05f, B * 0.3f);
                p.Line(dk, hl - 0.5f, -hb * 0.6f, hl - 0.5f, hb * 0.6f, 0.08f); p.Rect(G.Hex(0x5a5a4a), -L * 0.38f, 0.0f, 0.9f, 0.9f);
                for (int i = 0; i < 4; i++) p.Rect(i % 2 == 0 ? G.Hex(0x4a5238) : G.Hex(0x5a5a42), -L * 0.05f + i * 0.9f - 1.4f, 0.4f, 0.7f, 0.5f);
                mounts.Add(new Vector3(L * 0.38f, hb * 0.4f, 1)); mounts.Add(new Vector3(-L * 0.42f, -hb * 0.4f, 1)); break;
            case 4: // cargo
                p.Rect(super, -L * 0.34f, 0, L * 0.14f, B * 0.7f); p.Rect(pal.glass, -L * 0.34f + L * 0.07f, 0, 0.1f, B * 0.6f); p.Rect(G.Hex(0x303030), -L * 0.42f, 0, 0.5f, 0.5f);
                { Color[] cc = { G.Hex(0xb04a30), G.Hex(0x30608a), G.Hex(0x7a8a3a), G.Hex(0xc0a030), G.Hex(0x808080), G.Hex(0x30804a) };
                  for (int i = 0; i < 5; i++) for (int j = 0; j < 3; j++) { p.Rect(cc[(i * 2 + j * 3) % cc.Length], -L * 0.16f + i * 1.3f, (j - 1) * 0.75f, 1.15f, 0.65f); p.Line(G.Hex(0x000000, 0.35f), -L * 0.16f + i * 1.3f - 0.5f, (j - 1) * 0.75f, -L * 0.16f + i * 1.3f + 0.5f, (j - 1) * 0.75f, 0.03f); } }
                p.Line(G.Hex(0xc8a72c), L * 0.28f, -hb * 0.7f, L * 0.28f, hb * 0.7f, 0.1f); break;
            case 5: // missile cruiser / boat
                p.Rect(super, 0.0f, 0, L * 0.18f, B * 0.5f); p.Rect(pal.glass, L * 0.09f, 0, 0.1f, B * 0.4f); p.Circle(G.Hex(0xc8c8c0), -L * 0.08f, 0, B * 0.18f);
                { int tubes = L > 12 ? 8 : 4; for (int i = 0; i < tubes; i++) for (int sg = -1; sg <= 1; sg += 2) { float x = L * 0.12f + (i % (tubes / 2)) * 0.9f * 0.0f + (i / 2) * 0.0f; } }
                for (int i = 0; i < (L > 12 ? 8 : 4); i++) for (int sg = -1; sg <= 1; sg += 2) { float x = L * 0.28f - i * L * 0.075f; p.Line(G.Hex(0x70767a), x, sg * B * 0.32f, x - L * 0.06f, sg * B * 0.4f, 0.22f); p.Poly(G.Hex(0xd8d8d0), new float[] { x, sg * B * 0.32f - 0.08f, x + 0.12f, sg * B * 0.31f, x, sg * B * 0.32f + 0.08f }); }
                p.Rect(G.Hex(0x45494c), -L * 0.25f, 0, 0.9f, B * 0.4f); p.Circle(G.Hex(0xa0a4a0), -L * 0.36f, 0, 0.25f);
                mounts.Add(new Vector3(L * 0.38f, 0, 0)); mounts.Add(new Vector3(-L * 0.3f, 0, 1)); break;
            case 6: case 8: // auxiliary tanker / tug
                p.Rect(super, -L * 0.3f, 0, L * 0.14f, B * 0.6f); p.Rect(pal.glass, -L * 0.3f + L * 0.07f, 0, 0.1f, B * 0.5f);
                if (d.style == 6) { for (int i = 0; i < 4; i++) p.Circle(G.Hex(0x7a7e80), -L * 0.05f + i * 1.5f, 0, 0.35f); p.Line(G.Hex(0x303030), -L * 0.1f, 0, L * 0.4f, 0, 0.1f); }
                else { p.Line(G.Hex(0xc8a72c), L * 0.2f, 0, L * 0.4f, 0.5f, 0.15f); p.Circle(G.Hex(0x303030), L * 0.2f, 0, 0.2f); p.Circle(G.Hex(0xe8e0c0), -L * 0.05f, 0, 0.3f); }
                p.Rect(pal.accent, -L * 0.22f, 0, 0.12f, B * 0.5f); break;
            default: // landing craft
                p.Rect(G.Mul(deck, 0.9f), -0.2f, 0, L * 0.6f, B * 0.7f); p.Rect(super, -L * 0.4f, 0, L * 0.12f, B * 0.45f); p.Rect(G.Hex(0x4a5238), 0, 0.3f, 1.4f, 0.5f); p.Rect(G.Hex(0x5a5a42), 0, -0.3f, 1.4f, 0.5f);
                p.Rect(dk, hl - 0.2f, 0, 0.4f, B * 0.8f);
                mounts.Add(new Vector3(-L * 0.3f, 0, 2)); break;
        }
        FacMark(p, pal, -hl + 0.8f, hb - 0.3f, 0.7f, 0.3f);
        Emblem(p, pal.emblem, hl * 0.55f, 0, Mathf.Min(0.8f, B * 0.28f), pal.accent, dk, 90);
        p.Rect(G.Hex(0xe8e8e0, 0.7f), hl * 0.5f, hb - 0.12f, 0.04f * L, 0.05f);
        // naval ensign (stern)
        p.Rect(pal.fac == Faction.Ukraine ? G.Hex(0x1f6fd6) : G.Hex(0xf0f0f0), -hl + 0.15f, 0.0f, 0.3f, 0.2f); p.Rect(pal.fac == Faction.Ukraine ? G.Hex(0xffd500) : G.Hex(0x2a4fc8), -hl + 0.15f, -0.1f, 0.3f, 0.1f);
        if (icon) { m.icon = p.Make(); return m; }
        m.body = p.Make();
        m.shadow = p.Silhouette(new Color(0, 0, 0, 0.25f)).Make();
        // mounts
        for (int i = 0; i < mounts.Count && i < 2; i++)
        {
            int t = (int)mounts[i].z;
            var mp = new Pix(2.8f, 1.2f, 22);
            if (t == 0) { mp.Circle(G.Mul(hullc, 1.2f), 0, 0, 0.5f); mp.Rect(G.Hex(0x303336), 0.9f, 0, 1.4f, 0.14f); mp.Rect(G.Hex(0x15171a), 1.55f, 0, 0.12f, 0.2f); mp.Circle(G.Mul(hullc, 0.9f), -0.05f, 0, 0.3f); m.mountLen[i] = 1.6f; }
            else if (t == 1) { mp.Circle(G.Hex(0xb4b8b0), 0, 0, 0.42f); mp.Rect(G.Hex(0x303336), 0.5f, 0.12f, 0.9f, 0.08f); mp.Rect(G.Hex(0x303336), 0.5f, -0.12f, 0.9f, 0.08f); mp.Circle(G.Hex(0x70747a), 0, 0, 0.2f); m.mountLen[i] = 1.0f; }
            else { mp.Circle(G.Mul(hullc, 1.1f), 0, 0, 0.3f); mp.Rect(G.Hex(0x202224), 0.5f, 0, 0.8f, 0.07f); m.mountLen[i] = 0.9f; }
            m.mounts[i] = mp.Make();
            m.mountPos[i] = new Vector2(mounts[i].x, mounts[i].y);
        }
        return m;
    }
}
