using System;
using System.Collections.Generic;
using UnityEngine;

public class Ship : Unit
{
    Model m;
    SpriteRenderer bodySR, shadowSR;
    SpriteRenderer[] mountSR = new SpriteRenderer[2];
    float[] mAng = new float[2];
    int[] mKind = new int[2] { -1, -1 };
    public float flood;
    float sinkT; bool sinking;
    float wakeT, smokeT, fireT, bubbleT, rollPh;
    Vector2 landTarget; bool landing; bool reachedLand;
    public override float Mass { get { return 30f; } }

    public static Ship Spawn(UnitDef d, Brigade b, Faction f, Vector2 p, float rot)
    {
        var s = new Ship();
        s.InitUnit(d, b, f, p, rot);
        s.m = Art.Get(d, b, f);
        s.bodySR = Util.SR(s.tr, "body", s.m.body, 10, Vector2.zero);
        s.shadowSR = Util.SR(s.tr, "shadow", s.m.shadow, 8, new Vector2(0.15f, -0.15f));
        s.shadowSR.transform.localRotation = Quaternion.identity;
        for (int i = 0; i < 2; i++)
            if (s.m.mounts[i] != null)
            {
                s.mountSR[i] = Util.SR(s.tr, "mount" + i, s.m.mounts[i], 11, s.m.mountPos[i]);
                s.mAng[i] = rot;
                s.mKind[i] = s.m.mountLen[i] > 1.4f ? 0 : (s.m.mountLen[i] > 0.95f ? 1 : 2);
            }
        s.tr.rotation = Quaternion.Euler(0, 0, rot);
        s.rollPh = G.Rnd(0, 6f);
        return s;
    }

    public override float SpeedFactor()
    {
        float f = Mathf.Lerp(1f, 0.35f, flood) * (Stage >= 3 ? 0.8f : 1f);
        if (sinking || dead) return 0f;
        return f;
    }
    protected override bool Passable(Vector2 p) { return World.I.InBounds(p) && World.I.SpeedMul(Move.Naval, p) > 0f; }
    protected override bool CanMove() { return !dead && !sinking; }
    protected override bool CanFire() { return !dead && !sinking; }
    protected override bool CanUnloadNow() { return speed < 1.8f; }

    public override string Status()
    {
        if (dead) return "Sunk";
        if (sinking) return "Sinking!";
        if (flood > 0.15f) return "Flooding " + Mathf.RoundToInt(flood * 100) + "%";
        if (Stage >= 4) return "On fire";
        if (Stage == 3) return "Heavily damaged";
        if (Stage == 2) return "Damaged";
        if (landing) return "Moving to shore";
        if (unloading) return "Disembarking troops";
        if (tgtG != null || tgtA != null) return "Engaging";
        if (speed > 0.3f) return "Under way";
        return "Operational";
    }

    // map weapon -> turret mount (-1 = fixed launcher)
    int MountFor(WS s)
    {
        switch (s.d.t)
        {
            case WT.Arty: case WT.Cannon: for (int i = 0; i < 2; i++) if (mKind[i] == 0) return i; return -1;
            case WT.AAGun: case WT.MG: case WT.Auto: case WT.Rifle:
                for (int i = 0; i < 2; i++) if (mKind[i] == 1 || mKind[i] == 2) return i;
                for (int i = 0; i < 2; i++) if (mKind[i] >= 0) return i;
                return -1;
        }
        return -1;
    }
    protected override float AimAngle(WS s) { int i = MountFor(s); return i >= 0 ? mAng[i] : rot; }
    protected override Vector2 MuzzlePos(WS s)
    {
        int i = MountFor(s);
        if (i >= 0) return pos + G.Rot(m.mountPos[i], rot) + G.Dir(mAng[i]) * m.mountLen[i];
        return pos + G.Rot(new Vector2(def.len * 0.15f, (G.R01() - 0.5f) * def.wid * 0.5f), rot);
    }
    protected override bool Aligned(WS s, Ent t)
    {
        int i = MountFor(s);
        if (i < 0) return true;
        return Mathf.Abs(Mathf.DeltaAngle(mAng[i], G.Ang(t.pos - pos))) < 7f;
    }
    protected override bool Spotter() { return true; }

    public void OrderLandTroops(Vector2 shoreTarget)
    {
        Vector2 w = World.I.NearestOfType(shoreTarget, t => World.IsWater(t) && t != World.RIVER, 70f);
        // keep a couple of units off the beach
        Vector2 off = (w - shoreTarget).normalized * 3f;
        OrderMove(w + off);
        landing = true; landTarget = shoreTarget; reachedLand = false;
    }
    public override void OrderMove(Vector2 p, bool keepStance = false) { landing = false; base.OrderMove(p, keepStance); }

    public override void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false)
    {
        if (dead || sinking) return;
        float eff = dmg * Mathf.Clamp(pen / Mathf.Max(0.5f, def.armor), 0.05f, 1.3f);
        if (fire) eff *= 0.4f;
        hp -= eff; lastHit = World.I.simTime; if (src != null) lastAttacker = src;
        if (eff > 4f) { Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius * 0.6f, Vector2.up, 4); Sfx.Play(Sfx.Id.Hit, pos, 0.5f); }
        if (eff > 15f && G.R01() < 0.5f) flood = Mathf.Min(1f, flood + eff / maxHp * 0.7f);
        if (hp <= 0f) { hp = 0f; StartSinking(); }
    }

    void StartSinking()
    {
        if (sinking) return;
        sinking = true; sinkT = 0f; path = null; ord = OrderT.None;
        SetSelected(false); Game.I.sel.Remove(this);
        foreach (var w in weps) { w.tgt = null; w.burst = 0; }
        EvacuateAll(true);
        Fx.Explosion(pos, Mathf.Clamp(def.len * 0.2f, 1.5f, 4f), true);
        Game.I.Shake(0.5f, pos);
        Sfx.Play(Sfx.Id.Explosion, pos, 1f);
    }

    public override void Tick(float dt)
    {
        if (removed) return;
        if (sinking)
        {
            sinkT += dt; speed = G.Approach(speed, 0, dt * 2f);
            if (sinkT > 12f) { dead = true; sinking = false; sinkT = 12f; speed = 0; }
            return;
        }
        if (dead) { fireT -= dt; return; }
        // flooding grows when badly damaged
        float f = hp / maxHp;
        if (f < 0.6f) flood = Mathf.Min(1f, flood + (0.6f - f) * 0.012f * dt);
        if (Stage >= 4) hp -= maxHp * 0.004f * dt;
        if (flood >= 1f && !sinking) { hp = 0; StartSinking(); return; }
        if (hp <= 0f) { StartSinking(); return; }
        thinkT -= dt;
        if (thinkT <= 0f) { thinkT = 0.4f + G.Rnd(0, 0.2f); if (weps.Count > 0) Think(); }
        AutoBehavior(dt);
        bool moving = path != null && FollowPath(dt, false);
        if (!moving) speed = G.Approach(speed, 0f, maxSpeed * 0.4f * dt);
        else if (path == null && (ord == OrderT.Move || ord == OrderT.Return)) ord = OrderT.None;
        if (path == null && (ord == OrderT.Move || ord == OrderT.Return)) ord = OrderT.None;
        ApplyMove(dt);
        Separate(dt, false);
        pos = World.I.Clamp(pos, 1f);
        if (blockT > 1.5f && path != null) { blockT = 0f; if (ord == OrderT.Move) SetPath(dest); }
        if (blockT > 4f) blockT = 0f;
        // arrival at the landing zone
        if (landing && !moving && path == null && !reachedLand) { reachedLand = true; landing = false; unloading = true; }
        UpdateMounts(dt);
        UpdateWeapons(dt);
        ProcessAura(dt);
        UpdateUnload(dt);
    }

    void UpdateMounts(float dt)
    {
        for (int i = 0; i < 2; i++)
        {
            if (mountSR[i] == null) continue;
            Ent tgt = null;
            foreach (var w in weps) if (w.tgt != null && !w.tgt.dead && MountFor(w) == i) { tgt = w.tgt; break; }
            float want = tgt != null ? G.Ang(tgt.pos - pos) : rot;
            float trav = mKind[i] == 0 ? 55f : 150f;
            mAng[i] += Mathf.Clamp(Mathf.DeltaAngle(mAng[i], want), -trav * dt, trav * dt);
        }
    }

    protected override Vector2 ExitPoint(int idx)
    {
        float side = ((idx % 2) * 2 - 1) * (def.wid * 0.25f) + ((idx / 2) % 3 - 1) * 0.3f;
        return pos + G.Rot(new Vector2(def.len * 0.38f - (idx / 2 % 4) * 0.5f, side), rot);
    }

    public override void Visual(float dt)
    {
        if (removed) return;
        rollPh += dt * (0.6f + speed * 0.05f);
        float roll = Mathf.Sin(rollPh) * (0.6f + flood * 4f) * (sinking ? 3f : 1f);
        tr.position = new Vector3(pos.x, pos.y, 0);
        tr.rotation = Quaternion.Euler(0, 0, rot + (sinking || dead ? Mathf.Min(sinkT, 12f) * 0.9f : 0f));
        float sk = sinking || dead ? Mathf.Clamp01(sinkT / 12f) : 0f;
        float scl = 1f + Mathf.Sin(rollPh * 1.3f) * 0.006f - sk * 0.07f;
        tr.localScale = new Vector3(scl, scl * (1f + roll * 0.004f), 1f);
        if (sinking || dead)
        {
            Color c = Color.Lerp(Color.white, new Color(0.18f, 0.28f, 0.33f, 0.6f), sk);
            bodySR.color = c; bodySR.sortingOrder = dead ? 3 : 9;
            for (int i = 0; i < 2; i++) if (mountSR[i] != null) { mountSR[i].color = c; mountSR[i].sortingOrder = dead ? 4 : 10; }
            shadowSR.gameObject.SetActive(false);
        }
        else if (flood > 0.1f) bodySR.color = Color.Lerp(Color.white, new Color(0.7f, 0.85f, 0.95f, 1f), flood * 0.6f);
        for (int i = 0; i < 2; i++) if (mountSR[i] != null && !dead) mountSR[i].transform.rotation = Quaternion.Euler(0, 0, mAng[i]);
        if (!dead && !sinking && speed > 0.3f) Sfx.Emit(Sfx.Loop.Water, pos, Mathf.Clamp01(speed / maxSpeed) * Mathf.Min(1f, def.len / 10f));
        if (!visible) return;
        EmitEffects(dt);
    }

    void EmitEffects(float dt)
    {
        wakeT -= dt; smokeT -= dt; fireT -= dt; bubbleT -= dt;
        Vector2 fwd = G.Dir(rot);
        if (!dead && !sinking && speed > 0.4f && wakeT <= 0f)
        {
            wakeT = Mathf.Lerp(0.2f, 0.07f, speed / maxSpeed);
            float sf = Mathf.Clamp(def.wid * 0.5f, 0.6f, 1.6f);
            Vector2 stern = pos - fwd * def.len * 0.45f;
            Fx.Wake(stern + G.Dir(rot + 90f) * def.wid * 0.15f, sf * 1.2f, -fwd * 0.6f + G.Dir(rot + 90f) * 0.5f);
            Fx.Wake(stern - G.Dir(rot + 90f) * def.wid * 0.15f, sf * 1.2f, -fwd * 0.6f - G.Dir(rot + 90f) * 0.5f);
            Fx.Wake(stern, sf * 1.6f, -fwd * 0.3f);
            Vector2 bow = pos + fwd * def.len * 0.42f;
            Fx.Wake(bow + G.Dir(rot + 90f) * def.wid * 0.25f, sf * 0.8f, G.Dir(rot + 70f) * 1.2f);
            Fx.Wake(bow - G.Dir(rot + 90f) * def.wid * 0.25f, sf * 0.8f, G.Dir(rot - 70f) * 1.2f);
        }
        if (dead) { if (bubbleT <= 0f) { bubbleT = 0.8f; Fx.Wake(pos + UnityEngine.Random.insideUnitCircle * radius, 0.8f, Vector2.zero); } if (sinkT >= 12f && fireT <= 0f && G.R01() < 0.04f) { Fx.Smoke(pos, 1.2f, true, Vector2.up); fireT = 0.4f; } return; }
        int st = sinking ? 4 : Stage;
        Vector2 funnel = pos - fwd * def.len * 0.1f;
        if (st >= 2 && smokeT <= 0f) { smokeT = st >= 3 ? 0.1f : 0.35f; Fx.Smoke(funnel + UnityEngine.Random.insideUnitCircle * def.wid * 0.3f, st >= 3 ? 1.7f : 1f, st >= 3, Vector2.up * 0.8f); }
        if (st >= 4 && fireT <= 0f) { fireT = 0.06f; Fx.Fire(pos + UnityEngine.Random.insideUnitCircle * def.len * 0.25f, 1.1f); Fx.Glow(pos, 4f, new Color(1, 0.5f, 0.15f, 0.2f), 0.1f); }
        if ((sinking || flood > 0.3f) && bubbleT <= 0f) { bubbleT = 0.15f; Fx.Wake(pos + UnityEngine.Random.insideUnitCircle * def.len * 0.4f, 0.9f, Vector2.zero); }
    }
}
