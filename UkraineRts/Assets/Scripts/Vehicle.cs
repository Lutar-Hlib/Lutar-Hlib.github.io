using System;
using System.Collections.Generic;
using UnityEngine;

// Ground vehicles: tanks, IFV, APC, armoured cars, trucks, cars, artillery, MLRS, air defence, engineering
public class Vehicle : Unit
{
    Model m; VStyle vs;
    SpriteRenderer hullSR, turSR, gunSR, dmgSR, trkL, trkR;
    Transform turT, gunT;
    List<SpriteRenderer> wheels = new List<SpriteRenderer>();
    public float turAng, elev;
    public float engine = 100f, tracks = 100f, turret = 100f;
    public int crewN;
    bool bailed, hasTurret, hasGun, towed, wreckInit, cookoff;
    float bailT = -1f, burnT, wreckFireT, smokeT, sparkT, dustT, fireT, exhaustT, trackPhase, markDist, idleTurretT;
    float lastRot, steer;
    Vector2 lastMarkA, lastMarkB;
    public bool Immobilized { get { return tracks <= 0f || engine <= 0f || (Stage >= 4) || fuel <= 0f && false; } }
    public override float Mass { get { return def.hp > 400 ? 14f : (def.hp > 200 ? 9f : (towed ? 3f : 6f)); } }
    int stage = 1;
    float turretTrav;

    public static Vehicle Spawn(UnitDef d, Brigade b, Faction f, Vector2 p, float rot)
    {
        var v = new Vehicle();
        v.InitUnit(d, b, f, p, rot);
        v.vs = d.vs; v.m = Art.Get(d, b, f);
        v.hasTurret = v.vs.tur > 0; v.hasGun = v.vs.gun > 0; v.towed = d.towed;
        v.turAng = rot; v.crewN = d.crew;
        v.hullSR = Util.SR(v.tr, "hull", v.m.hull, 12, Vector2.zero);
        v.dmgSR = Util.SR(v.tr, "dmg", v.m.dmg, 14, Vector2.zero); v.dmgSR.color = new Color(1, 1, 1, 0);
        if (d.vs.tracked)
        {
            v.trkL = Util.SR(v.tr, "trkL", v.m.trk[0], 13, new Vector2(0, v.m.trackY));
            v.trkR = Util.SR(v.tr, "trkR", v.m.trk[0], 13, new Vector2(0, -v.m.trackY));
        }
        else
        {
            for (int i = 0; i < v.m.wheelPos.Length; i++)
            {
                var sr = Util.SR(v.tr, "wheel" + i, v.m.whl[0], 11, v.m.wheelPos[i]);
                v.wheels.Add(sr);
            }
        }
        if (v.hasTurret)
        {
            var tg = new GameObject("turret"); tg.transform.SetParent(v.tr, false);
            v.turT = tg.transform; v.turT.localPosition = new Vector3(d.vs.tx, 0, 0);
            v.turSR = Util.SR(v.turT, "t", v.m.turret, 16, Vector2.zero);
            if (v.hasGun)
            {
                v.gunSR = Util.SR(v.turT, "gun", v.m.gun, 15, new Vector2(v.m.gunPivotX, 0));
                v.gunT = v.gunSR.transform;
            }
        }
        v.maxSpeed = d.speed;
        v.fuel = 100f;
        v.tr.rotation = Quaternion.Euler(0, 0, rot);
        v.lastRot = rot;
        return v;
    }

    public override float SpeedFactor()
    {
        if (Immobilized) return 0f;
        float f = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(engine / 60f)) * (stage >= 3 ? 0.7f : 1f) * (tracks < 40f ? 0.6f : 1f);
        if (fuel <= 0f) f *= 0.2f;
        if (retreating) f *= 1.15f;
        return f;
    }
    protected override float SpeedFactorTurn() { return Immobilized ? 0f : Mathf.Lerp(0.5f, 1f, tracks / 100f); }
    protected override bool CanMove() { return !Immobilized && !dead; }
    protected override bool CanFire()
    {
        if (dead || bailed && crewN <= 0) return false;
        return true;
    }
    public override string Status()
    {
        if (dead) return "Destroyed";
        if (stage >= 4) return bailed ? "Burning - abandoned" : "Burning / Immobilized";
        if (Immobilized) return "Immobilized";
        if (stage == 3) return "Heavily damaged";
        if (stage == 2) return "Damaged";
        if (ord == OrderT.Attack) return "Attacking";
        if (tgtG != null || tgtA != null) return "Engaging";
        if (retreating) return "Retreating";
        if (speed > 0.3f) return "Moving";
        return "Operational";
    }
    public override int Stage { get { return dead ? 5 : stage; } }

    protected override float AimAngle(WS s)
    {
        if (hasTurret && s.d.mount == 0) return turAng;
        return rot;
    }
    protected override Vector2 MuzzlePos(WS s)
    {
        if (hasTurret && s.d.mount == 0)
        {
            Vector2 tc = pos + G.Rot(new Vector2(vs.tx, 0), rot);
            float md = s.d.muzzle;
            if (hasGun && (s.d.t == WT.Cannon || s.d.t == WT.Arty || s.d.t == WT.Auto || s.d.t == WT.AAGun || s.d.t == WT.Mortar || s.d.t == WT.Gun)) md = m.gunPivotX + vs.gl * (1f - 0.18f * elev);
            else if (vs.tur == 7 || vs.tur == 8) md = vs.tl * 0.5f;
            else if (hasGun) md = Mathf.Min(md, m.gunPivotX + vs.gl);
            return tc + G.Dir(turAng) * md;
        }
        return pos + G.Dir(rot) * (vs.L * 0.5f);
    }
    protected override bool Aligned(WS s, Ent t)
    {
        float want = G.Ang(t.pos - pos);
        if (hasTurret && s.d.mount == 0)
        {
            if (turret <= 0f) return false;
            float tol = (s.d.t == WT.Cannon || s.d.t == WT.Missile) ? 4f : (s.d.t == WT.Arty || s.d.t == WT.Salvo ? 6f : 9f);
            return Mathf.Abs(Mathf.DeltaAngle(turAng, want)) < tol;
        }
        return Mathf.Abs(Mathf.DeltaAngle(rot, want)) < 10f;
    }
    protected override float SpreadMul() { return (speed > 1f ? 1.4f : 1f) * (stage >= 3 ? 1.4f : 1f); }
    protected override bool Spotter() { return def.id == "uav" || def.aura == Aura.Comms; }
    protected override void OnFired(WS s)
    {
        if (s.d.t == WT.Cannon || s.d.t == WT.Arty || s.d.t == WT.Mortar) { recoil = 1f; }
    }

    public override void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false)
    {
        if (dead) return;
        float arm = def.armor;
        if (arm > 3.5f && !expl)
        {
            float ang = Mathf.Abs(Mathf.DeltaAngle(rot, G.Ang(from - pos)));
            arm *= ang < 55f ? 1f : (ang > 125f ? 0.5f : 0.72f);
        }
        float eff = dmg * Mathf.Clamp(pen / Mathf.Max(0.5f, arm), 0.04f, 1.3f);
        if (expl && arm > 4f && pen < 7f) eff *= 0.55f;
        if (fire) eff *= 0.5f;
        hp -= eff; lastHit = World.I.simTime; if (src != null) lastAttacker = src;
        if (eff > 3f)
        {
            Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius * 0.6f, (pos - from).normalized, eff > 20 ? 6 : 3);
            Sfx.Play(Sfx.Id.Hit, pos, 0.5f);
        }
        if (eff > 5f)
        {
            float r = G.R01();
            if (r < 0.22f) engine = Mathf.Max(0f, engine - eff * 0.9f);
            else if (r < 0.46f) tracks = Mathf.Max(0f, tracks - eff * 1.1f);
            else if (r < 0.66f && hasTurret) turret = Mathf.Max(0f, turret - eff * 1.1f);
        }
        if (hp <= 0f) { hp = 0f; Explode(expl && eff > 80f); return; }
        // cook-off chance when badly damaged and ammunition aboard
        if (stage >= 3 && eff > 25f && G.R01() < 0.08f && TotalAmmo() > 0) { Explode(true); return; }
        UpdateStage();
    }

    int TotalAmmo() { int n = 0; foreach (var w in weps) if (w.ammo > 0) n += w.ammo; return n; }

    void UpdateStage()
    {
        float f = hp / maxHp;
        int ns = f > 0.75f ? 1 : f > 0.5f ? 2 : f > 0.25f ? 3 : 4;
        if (ns != stage)
        {
            int old = stage; stage = ns;
            if (stage >= 4 && old < 4) { bailT = 0.8f; }
            if (stage >= 3) Fx.Smoke(pos, 1.6f, true, Vector2.up);
        }
    }

    void BailOut()
    {
        if (bailed) return; bailed = true;
        // crew leave a burning vehicle
        var cd = Db.Get("inf_crew");
        int n = Mathf.Min(crewN, 4);
        for (int i = 0; i < n; i++)
        {
            if (G.R01() < 0.2f) continue;
            Vector2 p = pos + G.Dir(rot + 90f + i * 60f) * (radius + 0.9f);
            if (!World.I.InBounds(p) || World.I.Water(p)) p = pos + UnityEngine.Random.insideUnitCircle.normalized * (radius + 0.8f);
            var s = Soldier.Spawn(cd, brig, fac, p, rot + G.Rnd(-60, 60));
            s.hp = s.maxHp * G.Rnd(0.35f, 0.9f);
            s.home = home;
            s.OrderMove(pos + (home - pos).normalized * 10f + UnityEngine.Random.insideUnitCircle * 4f);
            s.retreating = true; s.ord = OrderT.Return;
        }
        crewN = 0;
        EvacuateAll(true);
        Fx.Smoke(pos, 1.5f, true, Vector2.up);
    }

    void Explode(bool big)
    {
        if (dead) return;
        dead = true; hp = 0; speed = 0; path = null; ord = OrderT.None; stage = 5;
        SetSelected(false); Game.I.sel.Remove(this);
        if (!bailed) { bool surv = G.R01() < 0.3f; if (surv) BailOut(); else { crewN = 0; EvacuateAllDead(); bailed = true; } }
        cookoff = hasTurret && TotalAmmo() > 3 && (big || G.R01() < 0.55f) && def.hp > 150;
        float size = Mathf.Clamp(def.hp / 140f, 1.2f, 3.8f) * (def.explosive ? 1.8f : 1f);
        Fx.Explosion(pos, size, true);
        Combat.Explode(pos, radius * 1.4f + (def.explosive ? 7f : 2f), def.explosive ? 220f : 30f, 4f, this, Faction.Neutral, false, true, def.explosive, true);
        Game.I.Shake(Mathf.Min(1f, size * 0.25f), pos);
        burnT = def.explosive ? 90f : G.Rnd(35f, 70f);
        InitWreck();
        World.I.Scorch(pos, radius * 1.2f);
        foreach (var w in weps) { w.tgt = null; w.burst = 0; }
    }
    void EvacuateAllDead()
    {
        for (int i = cargo.Count - 1; i >= 0; i--)
        {
            Soldier s = cargo[i]; cargo.RemoveAt(i);
            if (G.R01() < 0.35f) { s.ExitVehicle(this, pos + UnityEngine.Random.insideUnitCircle * (radius + 0.5f)); s.hp = Mathf.Max(5f, s.maxHp * 0.3f); }
            else { s.ExitVehicle(this, pos); s.hp = 0.1f; s.Hit(50f, 5f, pos, null, true); }
        }
        unloading = false;
    }

    void InitWreck()
    {
        wreckInit = true;
        hullSR.color = new Color(0.22f, 0.2f, 0.18f, 1f);
        dmgSR.color = Color.white;
        hullSR.sortingOrder = 5; dmgSR.sortingOrder = 6;
        foreach (var w in wheels) { w.color = new Color(0.18f, 0.18f, 0.18f, 1f); w.sortingOrder = 4; w.transform.localScale = Vector3.one * 0.88f; }
        if (trkL != null) { trkL.color = new Color(0.3f, 0.3f, 0.3f, 0.6f); trkR.color = trkL.color; trkL.sortingOrder = 5; trkR.sortingOrder = 5; }
        if (turSR != null)
        {
            turSR.color = new Color(0.22f, 0.2f, 0.18f, 1f); turSR.sortingOrder = 8;
            if (gunSR != null) { gunSR.color = new Color(0.2f, 0.19f, 0.17f, 1f); gunSR.sortingOrder = 7; }
            if (cookoff)
            {
                // turret blown off the hull
                Vector2 off = UnityEngine.Random.insideUnitCircle.normalized * G.Rnd(1.2f, 2.6f);
                turT.position = new Vector3(pos.x + off.x, pos.y + off.y, 0);
                turT.rotation = Quaternion.Euler(0, 0, G.Rnd(0, 360f));
                turAng = turT.eulerAngles.z;
                Fx.Debris(pos, 8, 5f);
            }
            else if (gunT != null) { gunT.localRotation = Quaternion.Euler(0, 0, G.Rnd(-12, 12)); }
        }
    }

    public override void Repair(float amt)
    {
        base.Repair(amt);
        engine = Mathf.Min(100, engine + amt * 0.7f); tracks = Mathf.Min(100, tracks + amt * 0.9f); turret = Mathf.Min(100, turret + amt * 0.7f);
        int old = stage; UpdateStage(); if (stage < old) stage = Mathf.Max(1, stage);
        float f = hp / maxHp; stage = f > 0.75f ? 1 : f > 0.5f ? 2 : f > 0.25f ? 3 : 4;
    }

    public void Evacuate() { if (!dead) { BailOut(); } }
    public void DeployBridge()
    {
        if (def.aura != Aura.Bridge || dead) return;
        int n = World.I.DeployBridge(pos + G.Dir(rot) * (radius + 0.5f), G.Dir(rot), 20f);
        if (n > 0) { Sfx.Play(Sfx.Id.Hit, pos, 0.8f); Fx.Dust(pos + G.Dir(rot) * 4f, 2f, Color.gray); Game.I.Toast("Bridge deployed (" + n + " segments)"); }
        else Game.I.Toast("No water ahead to bridge");
    }

    public override void Tick(float dt)
    {
        if (removed) return;
        if (dead) { TickWreck(dt); return; }
        // fire damage / bail out
        if (stage >= 4)
        {
            hp -= maxHp * 0.011f * dt;
            if (bailT > 0f) { bailT -= dt; if (bailT <= 0f) BailOut(); }
            if (hp <= 0f) { Explode(false); return; }
        }
        thinkT -= dt;
        if (thinkT <= 0f)
        {
            thinkT = 0.4f + G.Rnd(0f, 0.2f);
            if (weps.Count > 0 && !bailed) Think();
            else if (def.aura == Aura.None) { tgtG = tgtA = null; }
        }
        if (bailed && crewN <= 0 && def.crew > 0) { foreach (var w in weps) w.tgt = null; }
        AutoBehavior(dt);
        // movement
        bool moving = false;
        if (!Immobilized && !bailed && path != null) moving = FollowPath(dt, def.vs.tracked);
        if (!moving) { speed = G.Approach(speed, 0f, maxSpeed * 1.5f * dt + 0.6f * dt); if (ord == OrderT.Move || ord == OrderT.Return) { if (path == null) ord = OrderT.None; } }
        else if (path == null && (ord == OrderT.Move || ord == OrderT.Return)) ord = OrderT.None;
        if (path == null && (ord == OrderT.Move || ord == OrderT.Return)) ord = OrderT.None;
        float d0 = distTravel;
        ApplyMove(dt);
        if (def.vs.tracked || true) { Separate(dt, def.crush); }
        pos = World.I.Clamp(pos, 1f);
        if (blockT > 1.5f && path != null) { blockT = 0f; if (ord == OrderT.Move || ord == OrderT.Return) SetPath(dest); }
        if (blockT > 4f) { blockT = 0f; }
        float dr = Mathf.Abs(Mathf.DeltaAngle(rot, lastRot)); steer = Mathf.Lerp(steer, Mathf.Clamp(Mathf.DeltaAngle(lastRot, rot) * 3f, -30f, 30f), dt * 6f); lastRot = rot;
        trackPhase += (distTravel - d0) + dr * 0.025f * (vs.tracked ? 1f : 0f);
        if (speed > 0.5f) fuel = Mathf.Max(0f, fuel - dt * 0.12f * (speed / maxSpeed));
        // turret
        UpdateTurret(dt);
        UpdateWeapons(dt);
        // supply / repair aura
        ProcessAura(dt);
        UpdateUnload(dt);
        // retreat arrival
        if (retreating && ord == OrderT.None) retreating = false;
        // marks
        if (distTravel - markDist > 0.9f && World.I != null && Game.I.cfg.trackMarks && visible)
        {
            markDist = distTravel;
            if (vs.tracked) { World.I.TrackMark(pos + G.Rot(new Vector2(-vs.L * 0.15f, m.trackY), rot), 0.28f); World.I.TrackMark(pos + G.Rot(new Vector2(-vs.L * 0.15f, -m.trackY), rot), 0.28f); }
            else foreach (var wp in m.wheelPos) World.I.TrackMark(pos + G.Rot(wp, rot), 0.16f);
        }
    }

    void UpdateTurret(float dt)
    {
        if (!hasTurret) return;
        Ent aimT = null; WS main = null;
        foreach (var w in weps) if (w.d.mount == 0 && w.tgt != null && !w.tgt.removed && !w.tgt.dead) { aimT = w.tgt; main = w; break; }
        float trav = def.vs.tur == 9 ? 140f : (def.vs.tur == 6 ? (def.cat == Cat.Artillery ? 32f : 90f) : (def.vs.tur == 7 ? 32f : (def.vs.tur == 8 ? 80f : 62f)));
        trav *= Mathf.Lerp(0.3f, 1f, turret / 100f);
        if (turret <= 0f || bailed) trav = 0f;
        float want;
        if (aimT != null) { want = G.Ang(aimT.pos - pos); idleTurretT = 0f; }
        else { idleTurretT += dt; want = idleTurretT > 3f ? rot : turAng; }
        // lead the turret relative to hull rotation (turret keeps world angle unless idle)
        turAng += Mathf.Clamp(Mathf.DeltaAngle(turAng, want), -trav * dt, trav * dt);
        // gun elevation for indirect fire
        float ev = 0f;
        if (main != null) { float dist = (main.tgt.pos - pos).magnitude; ev = (main.d.t == WT.Arty || main.d.t == WT.Mortar || main.d.t == WT.Salvo || main.d.t == WT.SAM) ? Mathf.Clamp01(dist / Mathf.Max(30f, main.d.range)) * 0.6f + 0.3f : 0.15f; }
        elev = Mathf.MoveTowards(elev, ev, dt * 1.2f);
    }

    void TickWreck(float dt)
    {
        wreckFireT -= dt;
        if (burnT > 0f) burnT -= dt;
        if (cargo.Count > 0) EvacuateAllDead();
    }

    public override void Visual(float dt)
    {
        if (removed) return;
        tr.position = new Vector3(pos.x, pos.y, 0);
        tr.rotation = Quaternion.Euler(0, 0, rot);
        if (dead && !wreckInit) InitWreck();
        // damage overlay
        if (!dead) dmgSR.color = new Color(1, 1, 1, stage <= 1 ? 0f : (stage == 2 ? 0.35f : (stage == 3 ? 0.65f : 1f)));
        if (!dead || !cookoff)
        {
            if (hasTurret && turT != null) turT.localRotation = Quaternion.Euler(0, 0, turAng - rot + (dead ? 15f : 0f));
        }
        if (hasTurret && !dead)
        {
            float recoilK = Mathf.Max(0f, recoil);
            if (gunT != null)
            {
                gunT.localPosition = new Vector3(m.gunPivotX - recoilK * 0.4f, 0, 0);
                gunT.localScale = new Vector3(1f - 0.2f * elev, 1f, 1f);
            }
            else if (vs.tur == 7 || vs.tur == 8)
            {
                turT.localScale = new Vector3(1f - 0.2f * elev, 1f, 1f);
            }
            else if (turSR != null) { turSR.transform.localPosition = new Vector3(-recoilK * 0.08f, 0, 0); }
        }
        if (!dead)
        {
            int f = ((int)(trackPhase * 4.5f)) & 3; if (trackPhase < 0) f = 0;
            if (trkL != null) { trkL.sprite = m.trk[f]; trkR.sprite = m.trk[f]; }
            else
            {
                for (int i = 0; i < wheels.Count; i++)
                {
                    wheels[i].sprite = m.whl[f];
                    if (i < 2) wheels[i].transform.localRotation = Quaternion.Euler(0, 0, steer);
                }
            }
            if (!Immobilized && speed > 0.4f) Sfx.Emit(def.vs.tracked ? Sfx.Loop.Tank : Sfx.Loop.Car, pos, Mathf.Clamp01(speed / maxSpeed) * 0.8f + 0.2f);
            else if (!Immobilized && !towed) Sfx.Emit(def.vs.tracked ? Sfx.Loop.Tank : Sfx.Loop.Car, pos, 0.12f);
            if (def.vs.tracked && speed > 1f) Sfx.Emit(Sfx.Loop.Track, pos, Mathf.Clamp01(speed / maxSpeed) * 0.6f);
        }
        if (!visible) return;
        EmitEffects(dt);
    }

    void EmitEffects(float dt)
    {
        if (dead)
        {
            if (burnT > 0f)
            {
                fireT -= dt; smokeT -= dt;
                float k = Mathf.Clamp01(burnT / 30f);
                if (fireT <= 0f) { fireT = 0.07f / Mathf.Max(0.3f, k); Fx.Fire(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, 0.8f + k * 0.5f); }
                if (smokeT <= 0f) { smokeT = 0.16f; Fx.Smoke(pos + UnityEngine.Random.insideUnitCircle * radius * 0.4f, 1.6f, true, Vector2.up * 0.8f); }
                Fx.Glow(pos, 3f * k + 1f, new Color(1f, 0.5f, 0.15f, 0.25f * k), 0.1f);
            }
            else { smokeT -= dt; if (smokeT <= 0f) { smokeT = 0.6f; Fx.Smoke(pos + UnityEngine.Random.insideUnitCircle * radius * 0.4f, 1.0f, false, Vector2.up * 0.5f); } }
            return;
        }
        smokeT -= dt; sparkT -= dt; fireT -= dt; dustT -= dt; exhaustT -= dt;
        Vector2 rear = pos + G.Rot(new Vector2(-vs.L * 0.35f, 0), rot);
        if (stage == 2 && smokeT <= 0f) { smokeT = 0.5f; Fx.Smoke(rear, 0.9f, false, Vector2.up * 0.5f); }
        if (stage == 2 && sparkT <= 0f) { sparkT = G.Rnd(1.2f, 3f); Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, Vector2.up, 4); }
        if (stage == 3 && smokeT <= 0f) { smokeT = 0.18f; Fx.Smoke(rear, 1.4f, G.R01() < 0.6f, Vector2.up * 0.7f); }
        if (stage == 3 && sparkT <= 0f) { sparkT = G.Rnd(0.5f, 1.4f); Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, Vector2.up, 5); }
        if (stage >= 4)
        {
            if (fireT <= 0f) { fireT = 0.07f; Fx.Fire(pos + G.Rot(new Vector2(UnityEngine.Random.Range(-vs.L * 0.35f, vs.L * 0.1f), UnityEngine.Random.Range(-0.4f, 0.4f)), rot), 0.9f); }
            if (smokeT <= 0f) { smokeT = 0.1f; Fx.Smoke(rear, 1.8f, true, Vector2.up); }
            Fx.Glow(pos, 3f, new Color(1f, 0.5f, 0.15f, 0.22f), 0.1f);
        }
        // exhaust & dust from movement
        if (speed > 0.8f)
        {
            if (exhaustT <= 0f && def.vs.tracked) { exhaustT = 0.35f; Fx.Puff(rear, new Color(0.5f, 0.5f, 0.5f, 0.16f), 0.5f, 0.9f, Vector2.up * 0.4f - G.Dir(rot) * 0.5f); }
            if (dustT <= 0f)
            {
                dustT = Mathf.Lerp(0.22f, 0.08f, speed / maxSpeed);
                byte t = World.I.At(pos);
                Color dc = World.I.winter || World.I.weather == Weather.Snow ? new Color(0.92f, 0.95f, 1f, 0.45f) : (World.I.wetness > 0.45f && t != World.ROAD ? new Color(0.25f, 0.18f, 0.12f, 0.5f) : (t == World.ROAD || t == World.URBAN || t == World.RUNWAY ? new Color(0.65f, 0.65f, 0.65f, 0.22f) : (t == World.BEACH ? new Color(0.85f, 0.78f, 0.55f, 0.45f) : new Color(0.55f, 0.45f, 0.3f, 0.4f))));
                if (t != World.BRIDGE) Fx.Dust(rear + UnityEngine.Random.insideUnitCircle * 0.5f, 0.6f + radius * 0.2f, dc);
            }
        }
    }

    protected override Vector2 ExitPoint(int idx)
    {
        // soldiers leave through the rear ramp / sides
        Vector2 back = G.Dir(rot + 180f) * (vs.L * 0.5f + 0.7f + (idx / 3) * 0.7f);
        Vector2 side = G.Dir(rot + 90f) * ((idx % 3) - 1) * 0.8f;
        return pos + back + side;
    }
}
