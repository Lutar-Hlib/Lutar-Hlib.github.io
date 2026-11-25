using System;
using System.Collections.Generic;
using UnityEngine;

// Static, destructible world objects: buildings, trees, barriers, supplies ...
public class Prop : Ent
{
    public int variant, pstate;
    protected SpriteRenderer sr;
    public bool burning; protected float burnT, burnTick, cookT, smokeT, fireT;
    public bool active;
    float halfL, halfW, rotRad;
    public bool IsTree { get { return def.special == "tree"; } }
    public bool Solid { get { return def.solid && pstate < 2 && !dead; } }
    protected float scaleK = 1f;
    public override bool Targetable { get { return false; } }

    public static Prop Spawn(UnitDef d, Brigade b, Faction f, Vector2 p, float rot, int variant)
    {
        Prop pr = new Prop();
        pr.Setup(d, b, f, p, rot, variant);
        return pr;
    }

    protected virtual void Setup(UnitDef d, Brigade b, Faction f, Vector2 p, float r, int v)
    {
        id = Ent.nextId++; def = d; kind = Kind.Prop; fac = f; brig = b; pos = p; rot = r; variant = v;
        hp = maxHp = d.hp; radius = d.radius;
        go = new GameObject(d.name); tr = go.transform; tr.position = new Vector3(p.x, p.y, 0);
        if (IsTree) { scaleK = 0.85f + G.Hash(v, (int)p.x, 5) * 0.45f; radius = d.len * 0.35f * scaleK; hp = maxHp = d.hp * scaleK; }
        tr.rotation = Quaternion.Euler(0, 0, IsTree ? rot : rot);
        tr.localScale = Vector3.one * scaleK;
        sr = Util.SR(tr, "s", Art.PropSprite(d, b, f, v % 6, 0), SortOrder(0), Vector2.zero);
        halfL = d.len * 0.5f * 0.92f; halfW = d.wid * 0.5f * 0.92f; rotRad = rot * Mathf.Deg2Rad;
        if (d.special == "burning") { Ignite(999f); }
        World.I.AddProp(this);
    }
    protected int SortOrder(int st)
    {
        if (IsTree) return 9;
        if (st == 2) return 5;
        switch (def.style)
        {
            case 21: case 22: case 24: case 28: case 29: case 30: case 31: case 45: return 8;
            case 23: case 25: case 26: case 27: case 32: case 33: return 6;
            default: return 7;
        }
    }

    public bool PushOut(Vector2 p, float r, out Vector2 res)
    {
        res = p;
        if (def.style == 11 || def.style == 14 || def.style == 33)
        {
            Vector2 dv = p - pos; float d = dv.magnitude, min = def.len * 0.5f * 0.9f + r;
            if (d < min) { res = pos + (d < 0.001f ? Vector2.right : dv / d) * min; return true; }
            return false;
        }
        // oriented box
        float c = Mathf.Cos(-rotRad), s = Mathf.Sin(-rotRad);
        Vector2 q = p - pos; Vector2 l = new Vector2(q.x * c - q.y * s, q.x * s + q.y * c);
        float cx = Mathf.Clamp(l.x, -halfL, halfL), cy = Mathf.Clamp(l.y, -halfW, halfW);
        Vector2 diff = l - new Vector2(cx, cy); float dist = diff.magnitude;
        if (dist >= r) return false;
        Vector2 push;
        if (dist < 0.001f)
        {
            float dx = halfL - Mathf.Abs(l.x), dy = halfW - Mathf.Abs(l.y);
            push = dx < dy ? new Vector2(Mathf.Sign(l.x) * (dx + r), 0) : new Vector2(0, Mathf.Sign(l.y) * (dy + r));
            push = new Vector2(Mathf.Sign(l.x) * (halfL + r), l.y);
            if (dx >= dy) push = new Vector2(l.x, Mathf.Sign(l.y) * (halfW + r));
            Vector2 w0 = push; float cr = Mathf.Cos(rotRad), sr2 = Mathf.Sin(rotRad);
            res = pos + new Vector2(w0.x * cr - w0.y * sr2, w0.x * sr2 + w0.y * cr); return true;
        }
        Vector2 nl = new Vector2(cx, cy) + diff / dist * r;
        float cr2 = Mathf.Cos(rotRad), sr3 = Mathf.Sin(rotRad);
        res = pos + new Vector2(nl.x * cr2 - nl.y * sr3, nl.x * sr3 + nl.y * cr2);
        return true;
    }

    public void Crush(Ent by)
    {
        if (dead || removed) return;
        hp = 0; Destroyed(false); Fx.Leaves(pos);
    }

    public override void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false)
    {
        if (dead || removed) return;
        if (def.id == "pr_helipad" || def.id == "ot_trench") return;
        if (fire)
        {
            if (def.flammable && !burning && G.R01() < (IsTree ? 0.35f : 0.18f)) Ignite(IsTree ? G.Rnd(10f, 22f) : G.Rnd(18f, 40f));
            return;
        }
        float eff = dmg * Mathf.Clamp(pen / Mathf.Max(0.5f, def.armor), 0.1f, 1.5f) * (expl ? 1.5f : 1f);
        if (IsTree) eff *= 1.4f;
        hp -= eff;
        if (eff > 3f && !IsTree) { Fx.Dust(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, 0.8f, new Color(0.6f, 0.58f, 0.52f, 0.5f)); Fx.Debris(pos, 2, 2f); }
        if (hp <= 0f) { hp = 0; Destroyed(expl); return; }
        if (pstate == 0 && hp < maxHp * 0.6f && !IsTree) { SetState(1); }
        else if (IsTree && pstate == 0 && hp < maxHp * 0.5f) SetState(1);
        if (expl && def.flammable && !burning && G.R01() < 0.25f) Ignite(G.Rnd(8f, 20f));
    }

    protected void SetState(int st)
    {
        pstate = st;
        int s2 = IsTree ? Mathf.Min(st, 1) : st;
        sr.sprite = Art.PropSprite(def, brig, fac, variant % 6, s2);
        sr.sortingOrder = SortOrder(st);
    }

    protected virtual void Destroyed(bool expl)
    {
        dead = true; SetState(2);
        if (IsTree) { sr.color = new Color(0.45f, 0.4f, 0.35f, 1f); }
        World.I.PropFell(this);
        float size = Mathf.Clamp(def.len / 6f, 0.5f, 2.2f);
        if (def.explosive)
        {
            float big = def.style == 11 ? 3.2f : (def.style == 12 ? 2.4f : 1.0f);
            Fx.Explosion(pos, big, true);
            Combat.Explode(pos, 6f * big, 120f * big, 5f, this, Faction.Neutral, false, true, def.style == 11 || def.style == 30, true);
            Game.I.Shake(0.3f * big, pos);
            if (def.style == 12) { cookT = 6f; active = true; Game.I.AddActive(this); }
            burning = def.style == 11; burnT = 60f; active = true; Game.I.AddActive(this);
        }
        else if (!IsTree)
        {
            Fx.Explosion(pos, size * 0.8f, false);
            Fx.Dust(pos, size * 2.2f, new Color(0.62f, 0.6f, 0.55f, 0.6f));
            Fx.Debris(pos, 6 + (int)size * 3, 4f);
            Sfx.Play(Sfx.Id.Explosion, pos, 0.5f);
            World.I.Scorch(pos, Mathf.Max(1f, def.len * 0.3f));
            if (def.flammable) { burning = true; burnT = G.Rnd(25f, 70f); active = true; Game.I.AddActive(this); }
        }
        Game.I.OnPropDestroyed(this);
        // buildings spill their garrison nothing; kill nearby soldiers hiding? (handled by explosion)
    }

    public void ForceRuin()
    {
        if (dead) return;
        dead = true; hp = 0; SetState(2);
        if (IsTree) sr.color = new Color(0.45f, 0.4f, 0.35f, 1f);
        World.I.PropFell(this);
    }

    public void Ignite(float t)
    {
        if (burning || dead && IsTree) return;
        burning = true; burnT = t; active = true; Game.I.AddActive(this);
    }

    // only called for active (burning / cooking) props
    public override void Tick(float dt)
    {
        if (removed) return;
        if (cookT > 0f)
        {
            cookT -= dt;
            if (G.R01() < dt * 3.5f) { var q = pos + UnityEngine.Random.insideUnitCircle * radius; Fx.Explosion(q, 0.6f, false); Combat.Explode(q, 3.5f, 40f, 4f, this, Faction.Neutral, false, true, true, true); }
            if (cookT <= 0f) { Fx.Explosion(pos, 2.2f, true); Combat.Explode(pos, 9f, 120f, 5f, this, Faction.Neutral, false, true, true, true); }
        }
        if (!burning) { if (cookT <= 0f) active = false; return; }
        burnT -= dt;
        if (!dead) { hp -= dt * (IsTree ? 3f : maxHp * 0.01f); if (hp <= 0f) { hp = 0; Destroyed(false); } }
        if (burnT <= 0f) { burning = false; if (cookT <= 0f) active = false; return; }
        // spread
        burnTick -= dt;
        if (burnTick <= 0f)
        {
            burnTick = 2f;
            if (!dead || IsTree == false)
            {
                qb.Clear(); World.I.QueryProps(pos, IsTree ? 3.2f : 5f, qb);
                foreach (var e in qb) { Prop p = e as Prop; if (p != null && p != this && !p.burning && p.def.flammable && !p.dead && G.R01() < (IsTree ? 0.08f : 0.1f)) p.Ignite(IsTree ? G.Rnd(8f, 18f) : G.Rnd(15f, 40f)); }
            }
        }
    }
    static List<Ent> qb = new List<Ent>();

    public override void Visual(float dt)
    {
        if (removed || !visible) return;
        if (burning)
        {
            fireT -= dt; smokeT -= dt;
            float sz = IsTree ? 0.8f : Mathf.Clamp(def.len / 5f, 0.8f, 2f);
            if (fireT <= 0f) { fireT = 0.09f; Fx.Fire(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, sz); }
            if (smokeT <= 0f) { smokeT = 0.2f; Fx.Smoke(pos + UnityEngine.Random.insideUnitCircle * radius * 0.4f, sz * 1.4f, G.R01() < 0.5f, Vector2.up); }
            Fx.Glow(pos, sz * 2.5f, new Color(1, 0.5f, 0.15f, 0.18f), 0.1f);
        }
        else if (!dead && pstate == 1 && !IsTree)
        {
            smokeT -= dt; if (smokeT <= 0f) { smokeT = 0.9f; Fx.Smoke(pos + UnityEngine.Random.insideUnitCircle * radius * 0.3f, 1f, false, Vector2.up * 0.6f); }
        }
        else if (dead && !IsTree) { smokeT -= dt; if (smokeT <= 0f) { smokeT = 1.4f; Fx.Smoke(pos + UnityEngine.Random.insideUnitCircle * radius * 0.4f, 1f, false, Vector2.up * 0.5f); } }
    }

    public override void OnRemove() { World.I.RemoveProp(this); if (!dead && def.solid) World.I.PropFell(this); }
    public override string Status() { return dead ? (IsTree ? "Felled" : "Ruins") : (pstate == 1 ? "Damaged" : "Intact") + (burning ? " - burning" : ""); }
}

// Armed static emplacement (MG nest, AT gun ...) - behaves like a stationary unit
public class Emplacement : Unit
{
    SpriteRenderer bodySR, turSR;
    float turAng, gunLen, burnT, fireT, smokeT; int pstate; bool burning;
    public override float Mass { get { return 50f; } }

    public static Emplacement Spawn(UnitDef d, Brigade b, Faction f, Vector2 p, float rot)
    {
        var e = new Emplacement();
        e.InitUnit(d, b, f, p, rot);
        e.kind = Kind.Ground; e.move = Move.None; e.maxSpeed = 0; e.radius = d.radius * 0.7f;
        e.bodySR = Util.SR(e.tr, "b", Art.PropSprite(d, b, f, 0, 0), 7, Vector2.zero);
        var pm = Art.PropModel(d, b, f, 0, 0);
        if (pm.turret != null) { e.turSR = Util.SR(e.tr, "t", pm.turret, 12, Vector2.zero); e.gunLen = pm.gunLen; }
        e.turAng = rot;
        return e;
    }
    protected override float AimAngle(WS s) { return turAng; }
    protected override Vector2 MuzzlePos(WS s) { return pos + G.Dir(turAng) * Mathf.Max(0.8f, gunLen); }
    protected override bool Aligned(WS s, Ent t) { return Mathf.Abs(Mathf.DeltaAngle(turAng, G.Ang(t.pos - pos))) < 6f; }
    public override string Status() { return dead ? "Destroyed" : (tgtG != null || tgtA != null ? "Engaging" : (pstate == 1 ? "Damaged" : "Manned")); }

    public override void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false)
    {
        if (dead) return;
        float eff = dmg * Mathf.Clamp(pen / Mathf.Max(0.5f, def.armor), 0.08f, 1.3f) * (fire ? 0.3f : 1f);
        hp -= eff; lastHit = World.I.simTime; if (src != null) lastAttacker = src;
        if (eff > 3f) Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius, Vector2.up, 3);
        if (hp <= 0f)
        {
            hp = 0; dead = true; SetSelected(false); Game.I.sel.Remove(this);
            bodySR.sprite = Art.PropSprite(def, brig, fac, 0, 2); bodySR.sortingOrder = 5;
            if (turSR != null) turSR.gameObject.SetActive(false);
            Fx.Explosion(pos, 0.8f, false); World.I.Scorch(pos, 1.5f);
            burning = true; burnT = G.Rnd(15f, 30f);
            foreach (var w in weps) { w.tgt = null; w.burst = 0; }
        }
        else if (hp < maxHp * 0.5f && pstate == 0) { pstate = 1; bodySR.sprite = Art.PropSprite(def, brig, fac, 0, 1); }
    }

    public override void Tick(float dt)
    {
        if (removed) return;
        if (dead) { burnT -= dt; return; }
        thinkT -= dt;
        if (thinkT <= 0f) { thinkT = 0.4f + G.Rnd(0, 0.15f); Think(); }
        Ent aim = null; foreach (var w in weps) if (w.tgt != null && !w.tgt.dead) { aim = w.tgt; break; }
        if (aim != null) turAng += Mathf.Clamp(Mathf.DeltaAngle(turAng, G.Ang(aim.pos - pos)), -150f * dt, 150f * dt);
        UpdateWeapons(dt);
    }
    public override void Visual(float dt)
    {
        if (removed) return;
        if (turSR != null && !dead) { turSR.transform.rotation = Quaternion.Euler(0, 0, turAng); turSR.transform.localPosition = -G.Dir(turAng) * recoil * 0.12f; }
        if (!visible) return;
        if (dead && burnT > 0f) { fireT -= dt; smokeT -= dt; if (fireT <= 0f) { fireT = 0.1f; Fx.Fire(pos + UnityEngine.Random.insideUnitCircle * 0.8f, 0.8f); } if (smokeT <= 0f) { smokeT = 0.25f; Fx.Smoke(pos, 1.3f, true, Vector2.up); } }
        else if (pstate == 1 && !dead) { smokeT -= dt; if (smokeT <= 0f) { smokeT = 0.6f; Fx.Smoke(pos, 0.9f, false, Vector2.up * 0.5f); } }
    }
}
