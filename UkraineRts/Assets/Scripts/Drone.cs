using System;
using System.Collections.Generic;
using UnityEngine;

public enum DMission { Recon, Attack, Return, Hover }

// Unmanned aircraft. Always launched by a Soldier (operator) which stays an independent unit.
public class Drone : Aircraft
{
    public Soldier owner;
    public DMission mission = DMission.Recon;
    public Ent atk;
    public float battery, maxBattery, sig = 1f, lossT, viewR = 40f;
    public bool noSignal;
    Vector2 launchPos, patrolC;
    float patrolT, scanT, commsT, commsMul = 1f, loiterT;
    public float SignalRange { get { return def.signal * commsMul; } }

    public static Drone SpawnFrom(UnitDef d, Soldier op, Vector2 target, Ent tgt)
    {
        var dr = (Drone)Aircraft.SpawnAir(d, op.brig, op.fac, op.pos + G.Dir(op.rot) * 1.0f, op.rot, true);
        dr.owner = op; dr.launchPos = op.pos; dr.home = op.pos;
        dr.alt = 0.15f; dr.targetAlt = d.alt;
        dr.battery = dr.maxBattery = d.battery; dr.patrolC = target; dr.anchor = target;
        dr.viewR = d.special == "ucav" ? 70f : (d.special == "recon" ? 55f : (d.special == "fpv" ? 22f : 34f));
        dr.rot = G.Ang(target - op.pos);
        dr.speed = d.speed * 0.5f;
        switch (d.special)
        {
            case "fpv": case "loiter":
                dr.mission = DMission.Attack; dr.atk = tgt; break;
            case "bomber": dr.mission = DMission.Attack; dr.atk = tgt; break;
            default: dr.mission = DMission.Recon; break;
        }
        if (tgt == null && (d.special == "fpv" || d.special == "loiter")) dr.mission = DMission.Attack;
        return dr;
    }

    public void OwnerLost() { owner = null; }
    public void Recall() { if (!dead) mission = DMission.Return; }
    public void Retarget(Ent e) { if (e != null && !dead) { atk = e; mission = DMission.Attack; } }
    public void SendTo(Vector2 p) { patrolC = p; anchor = p; if (mission == DMission.Return) mission = DMission.Recon; if (def.special == "fpv" || def.special == "loiter") atk = null; }

    public override string Status()
    {
        if (dead) return "Destroyed";
        if (state == AState.Crashing) return "Crashing";
        if (noSignal) return "NO SIGNAL";
        switch (mission)
        {
            case DMission.Return: return "Returning to operator";
            case DMission.Attack: return atk != null && !atk.dead ? "Attack run" : "Seeking target";
            case DMission.Hover: return "Holding position";
        }
        return "Reconnaissance";
    }

    protected override void AutoAir(float dt)
    {
        if (dead || state == AState.Crashing) return;
        // energy
        float drain = 1f + (speed > 0 ? 0.3f * speed / Mathf.Max(1f, maxSpeed) : 0f);
        battery -= dt * drain;
        if (battery <= 0f) { hp = 0; BeginCrash(); return; }
        if (battery < maxBattery * 0.2f && mission != DMission.Return) { mission = DMission.Return; }
        // signal
        commsT -= dt;
        if (commsT <= 0f)
        {
            commsT = 1f; commsMul = 1f;
            if (owner != null && !owner.dead)
            {
                qbuf.Clear(); World.I.Query(owner.pos, 40f, qbuf, false);
                foreach (var e in qbuf) if (e.fac == fac && !e.dead && e.def.aura == Aura.Comms) { commsMul = 1.45f; break; }
            }
        }
        if (owner == null || owner.removed || owner.dead) sig = 0f;
        else sig = Mathf.Clamp01(1f - (owner.pos - pos).magnitude / SignalRange);
        noSignal = sig < 0.08f;
        if (noSignal)
        {
            lossT += dt;
            if (lossT > 2.5f && mission != DMission.Return) mission = DMission.Return;   // fail-safe return
            if (lossT > 14f) battery -= dt * 6f;                                         // drifts and runs out of power
        }
        else lossT = Mathf.Max(0f, lossT - dt);
        // observation camera: reveal enemy units below
        scanT -= dt;
        if (scanT <= 0f && !noSignal)
        {
            scanT = 0.4f;
            qbuf.Clear(); World.I.Query(pos, viewR, qbuf, false);
            foreach (var e in qbuf) if (IsEnemyOf(e) && !e.dead && !e.hidden) e.SpotBy(fac);
        }
        // mission logic
        Vector2 ownerPos = (owner != null && !owner.dead) ? owner.pos : launchPos;
        bool fixedWing = IsFixedWing();
        switch (mission)
        {
            case DMission.Return:
                ord = OrderT.Move; wp = ownerPos; dest = ownerPos; hasWp = true;
                if ((pos - ownerPos).magnitude < (fixedWing ? 7f : 2.5f)) Recovered();
                break;
            case DMission.Attack:
                if (def.special == "bomber") { DoBomber(dt, ownerPos); break; }
                if (atk == null || atk.dead || atk.removed || atk.hidden)
                {
                    atk = null;
                    scanT -= 0f;
                    // look for new target near the drone
                    loiterT -= dt;
                    if (loiterT <= 0f)
                    {
                        loiterT = 0.5f;
                        qbuf.Clear(); World.I.Query(pos, def.special == "loiter" ? 110f : 70f, qbuf, false);
                        float bd = 1e9f;
                        foreach (var e in qbuf) if (IsEnemyOf(e) && !e.dead && !e.hidden && !e.Airborne) { float d = (e.pos - pos).magnitude; if (d < bd) { bd = d; atk = e; } }
                    }
                    if (atk == null)
                    {
                        // loiter around the designated area while waiting
                        if (fixedWing) { ord = OrderT.None; anchor = patrolC; hasWp = false; }
                        else { patrolT -= dt; if (patrolT <= 0f || !hasWp) { patrolT = 3f; wp = patrolC + UnityEngine.Random.insideUnitCircle * 8f; } ord = OrderT.Move; hasWp = true; dest = wp; }
                        break;
                    }
                }
                {
                    Vector2 aim = atk.pos + atk.vel * 0.25f;
                    wp = aim; dest = aim; hasWp = true; ord = OrderT.Move;
                    float d = (atk.pos - pos).magnitude;
                    if (d < atk.radius + 1.0f) Detonate();
                }
                break;
            case DMission.Hover:
                ord = OrderT.Hold; hasWp = false; break;
            default: // Recon
                if (fixedWing) { ord = OrderT.None; anchor = patrolC; hasWp = false; orbitR = Mathf.Max(18f, def.speed * 1.3f); }
                else
                {
                    patrolT -= dt;
                    if (patrolT <= 0f || !hasWp) { patrolT = G.Rnd(3f, 6f); wp = patrolC + UnityEngine.Random.insideUnitCircle * 22f; }
                    ord = OrderT.Move; dest = wp; hasWp = true;
                }
                break;
        }
        base.AutoAir(dt);
    }

    void DoBomber(float dt, Vector2 ownerPos)
    {
        if (atk == null || atk.dead || atk.removed) { atk = null; patrolT -= dt; if (patrolT <= 0f || !hasWp) { patrolT = 4f; wp = patrolC + UnityEngine.Random.insideUnitCircle * 12f; } ord = OrderT.Move; hasWp = true; dest = wp; return; }
        wp = atk.pos; dest = atk.pos; hasWp = true; ord = OrderT.Move;
        bool out_ = true; foreach (var w in weps) if (w.ammo > 0) out_ = false;
        if (out_) mission = DMission.Return;
    }

    void Recovered()
    {
        if (owner != null && !owner.dead) { owner.Recover(this); Fx.Sparkle(pos); }
        dead = false; removed = false;
        Destroy();
    }

    void Detonate()
    {
        if (dead) return;
        float aoe = 3.2f, dmg = 95f, pen = 5f, size = 0.8f;
        if (def.special == "loiter") { if (def.id == "uav_geran") { aoe = 6.5f; dmg = 280f; pen = 8f; size = 1.7f; } else { aoe = 3.8f; dmg = 140f; pen = 9f; size = 1.0f; } }
        else if (def.id == "uav_fpv2") { aoe = 4f; dmg = 125f; pen = 6f; size = 1.0f; }
        bool air = atk != null && atk.Airborne;
        Fx.Explosion(pos, size, false);
        Combat.Explode(pos, aoe, dmg, pen, this, fac, air, !air, false, true);
        Game.I.Shake(0.15f, pos);
        dead = true; hp = 0;
        SetSelected(false); Game.I.sel.Remove(this);
        if (owner != null) owner.drones.Remove(this);
        Destroy();
    }

    protected override void BeginCrash()
    {
        if (owner != null) owner.drones.Remove(this);
        base.BeginCrash();
    }
}
