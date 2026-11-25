using System;
using System.Collections.Generic;
using UnityEngine;

public enum AState { Parked, TakeOff, Flying, Landing, Crashing, Wreck }

// Fixed-wing aircraft and helicopters (Drone derives from this)
public class Aircraft : Unit
{
    protected Model m;
    protected SpriteRenderer bodySR, shadowSR;
    protected List<SpriteRenderer> flames = new List<SpriteRenderer>(), props = new List<SpriteRenderer>(), propBlur = new List<SpriteRenderer>(), rotors = new List<SpriteRenderer>(), rotorBlurs = new List<SpriteRenderer>();
    protected SpriteRenderer tailRotSR;
    public AState state = AState.Flying;
    public float targetAlt = 1f, engine = 100f;
    public int flaresLeft = 30; float flareCd;
    public bool isHeli, isDrone, isPlane;
    protected Vector2 anchor; protected float phi, orbitDir = 1f, orbitR;
    protected Vector2 wp; protected bool hasWp;
    float breakT; bool breakaway;
    float rotorRatio = 1f, rotorAng, propAng;
    float crashT, crashDur, spin, alt0; bool ejected; public bool crewSurvived;
    float smokeT, fireT, sparkT, burnT, trailT;
    // landing / assault
    public bool landingRunway; Vector2 rwStart, rwDir; int landPhase;
    public Vector2 lzTarget; public bool assaultActive; float assaultWait;
    public bool dropActive; Vector2 dropStart, dropEnd, dropTarget; float dropT; int dropPhase;
    public float flap;

    public override float Mass { get { return 20f; } }
    public override bool Targetable { get { return !dead && !removed && !hidden && state != AState.Crashing; } }
    public override string Status()
    {
        switch (state)
        {
            case AState.Parked: return dead ? "Wreck" : "Parked";
            case AState.TakeOff: return "Taking off";
            case AState.Landing: return "Landing";
            case AState.Crashing: return ejected ? "Crashing - pilot ejected" : "Crashing!";
            case AState.Wreck: return "Destroyed";
        }
        if (dropActive) return "Paradrop run";
        if (assaultActive) return "Air assault";
        int st = Stage; if (st >= 4) return "On fire";
        if (st == 3) return "Heavily damaged";
        if (st == 2) return "Damaged";
        if (tgtG != null || tgtA != null) return "Engaging";
        return alt > 1.5f ? "Flying (high)" : "Flying (low)";
    }
    public string LayerName { get { return state == AState.Parked || alt < 0.2f ? "GROUND" : (alt > 1.5f ? "HIGH" : "LOW"); } }

    public static Aircraft SpawnAir(UnitDef d, Brigade b, Faction f, Vector2 p, float rot, bool airborne = true)
    {
        Aircraft a = d.kind == Kind.Drone ? new Drone() : new Aircraft();
        a.Setup(d, b, f, p, rot, airborne);
        return a;
    }

    protected void Setup(UnitDef d, Brigade b, Faction f, Vector2 p, float r, bool airborne)
    {
        InitUnit(d, b, f, p, r);
        isHeli = d.kind == Kind.Heli; isDrone = d.kind == Kind.Drone; isPlane = d.kind == Kind.Plane;
        m = Art.Get(d, b, f);
        targetAlt = d.alt;
        alt = airborne ? targetAlt : 0f; state = airborne ? AState.Flying : AState.Parked;
        speed = airborne && isPlane ? d.speed * 0.8f : 0f;
        anchor = p; phi = r; orbitDir = G.R01() < 0.5f ? 1f : -1f; orbitR = Mathf.Max(30f, d.speed * 0.7f);
        radius = d.radius;
        bodySR = Util.SR(tr, "body", m.body, 40, Vector2.zero);
        shadowSR = Util.SR(tr, "shadow", m.shadow, 36, Vector2.zero);
        if (isPlane || isDrone)
        {
            if (m.prop != null)
            {
                foreach (var e in m.eng)
                {
                    props.Add(Util.SR(tr, "prop", m.prop, 44, e));
                    propBlur.Add(Util.SR(tr, "pblur", m.propBlur, 43, e));
                }
            }
            if (m.prop == null || def.style < 12 && isPlane)
                foreach (var e in m.eng)
                {
                    var fl = Util.SR(tr, "flame", Art.Spr.flame[1], 39, e + new Vector2(-0.5f, 0), 0, true);
                    flames.Add(fl);
                }
        }
        if (isHeli)
        {
            for (int i = 0; i < m.rotorPos.Length; i++)
            {
                rotors.Add(Util.SR(tr, "rotor", m.rotor, 46, m.rotorPos[i]));
                rotorBlurs.Add(Util.SR(tr, "rblur", m.rotorBlur, 45, m.rotorPos[i]));
            }
            if (m.tailRotor != null) tailRotSR = Util.SR(tr, "trotor", m.tailRotor, 46, m.tailRotPos);
        }
        rotorRatio = airborne ? 1f : 0f;
        if (isDrone && m.prop != null) { for (int i = 0; i < props.Count; i++) props[i].transform.localScale = Vector3.one; }
        engine = 100f;
        flaresLeft = def.flares ? 30 : 0;
        maxSpeed = d.speed;
        UpdateTransforms();
    }

    public void Preload(UnitDef sd, int n, Brigade b)
    {
        for (int i = 0; i < n && cargo.Count < cap; i++)
        {
            var s = Soldier.Spawn(sd, b, fac, pos, 0);
            Load(s);
        }
    }

    public override bool CanCarry(Soldier s) { return base.CanCarry(s) && (alt < 0.15f || state == AState.Parked || inFlightLoad); }
    public bool inFlightLoad;
    protected override bool CanUnloadNow() { return state == AState.Parked && alt < 0.1f; }
    public override void CmdUnload()
    {
        if (cargo.Count == 0) return;
        if (state == AState.Parked) unloading = true;
        else if (isHeli) { OrderLand(pos + G.Dir(rot) * 6f); assaultActive = true; lzTarget = pos; }
        else if (isPlane && def.special == "transport") OrderParadrop(pos + G.Dir(rot) * 60f);
    }

    // ------------------------------------------------------------ orders
    public override void OrderMove(Vector2 p, bool keepStance = false)
    {
        if (state == AState.Crashing || dead) return;
        if (state == AState.Parked) OrderTakeoff();
        p = World.I.Clamp(p, 3f);
        ord = OrderT.Move; dest = p; wp = p; hasWp = true; anchor = p; retreating = false;
        assaultActive = false; dropActive = false; landingRunway = false; landPhase = 0;
    }
    public override void OrderStop() { ord = OrderT.Hold; hasWp = false; anchor = pos; assaultActive = false; dropActive = false; }
    public override void OrderReturn() { OrderMove(home); ord = OrderT.Return; }
    public override void OrderFollow(Ent e) { base.OrderFollow(e); }

    public void OrderTakeoff()
    {
        if (state != AState.Parked || dead) return;
        state = AState.TakeOff; landingRunway = false; targetAlt = def.alt;
        Sfx.Play(Sfx.Id.UiOpen, pos, 0.3f);
    }
    public void SetLayer(int layer)
    {
        if (state == AState.Flying || state == AState.TakeOff) targetAlt = Mathf.Clamp(layer, 1, 2);
    }
    public void OrderLand(Vector2 p)
    {
        if (dead) return;
        if (isHeli) { ord = OrderT.Land; dest = World.I.Clamp(p, 3f); wp = dest; hasWp = true; landPhase = 1; assaultActive = false; return; }
        // aeroplane: nearest airfield
        int best = -1; float bd = 1e9f;
        for (int i = 0; i < World.I.airfields.Count; i++) { float d = (World.I.airfields[i] - pos).magnitude; if (d < bd) { bd = d; best = i; } }
        if (best < 0) { Game.I.Toast("No airfield on this map to land at"); return; }
        Vector2 a = World.I.airfields[best]; float L = 80f + World.I.sizeIdx * 10f;
        bool fromWest = pos.x < a.x;
        rwDir = new Vector2(fromWest ? 1f : -1f, 0f);
        rwStart = new Vector2(a.x - rwDir.x * L * 0.45f, a.y);
        ord = OrderT.Land; landingRunway = true; landPhase = 0; hasWp = true; wp = rwStart - rwDir * 70f; dest = a;
    }
    public void OrderParadrop(Vector2 target)
    {
        if (def.special != "transport" || !isPlane) return;
        OrderMove(pos);
        Vector2 dir = (target - pos); if (dir.magnitude < 5f) dir = G.Dir(rot); dir.Normalize();
        dropTarget = target; dropStart = World.I.Clamp(target - dir * 90f, 3f); dropEnd = World.I.Clamp(target + dir * 90f, 3f);
        dropActive = true; dropPhase = 0; ord = OrderT.Paradrop; hasWp = true; wp = dropStart; dest = target; targetAlt = def.alt;
    }
    public void OrderAssault(Vector2 target)
    {
        if (!isHeli) return;
        OrderMove(target); lzTarget = target; assaultActive = true; assaultWait = 0f; ord = OrderT.Assault;
        landPhase = 0;
    }

    public override void LaunchFlares()
    {
        if (!def.flares || flaresLeft <= 0 || flareCd > 0f || dead || alt < 0.3f) return;
        flaresLeft--; flareCd = 3.2f;
        Combat.DeployFlares(this);
    }

    // ------------------------------------------------------------ simulation
    public override void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false)
    {
        if (dead || state == AState.Crashing || state == AState.Wreck) return;
        float arm = def.armor;
        float eff = dmg * Mathf.Clamp(pen / Mathf.Max(0.4f, arm), 0.06f, 1.3f);
        if (state == AState.Parked) eff *= 1.0f;
        hp -= eff; lastHit = World.I.simTime; if (src != null) lastAttacker = src;
        Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, (pos - from).normalized, 3);
        if (eff > 6f && G.R01() < 0.3f) engine = Mathf.Max(0f, engine - eff);
        if (hp <= 0f || engine <= 0f) { hp = 0f; BeginCrash(); }
    }

    protected virtual void BeginCrash()
    {
        if (state == AState.Crashing || state == AState.Wreck) return;
        SetSelected(false); Game.I.sel.Remove(this);
        foreach (var w in weps) { w.tgt = null; w.burst = 0; }
        path = null; ord = OrderT.None; hasWp = false; assaultActive = false; dropActive = false;
        if (state == AState.Parked || alt < 0.25f) { Impact(); return; }
        state = AState.Crashing; crashT = 0f; alt0 = alt;
        crashDur = Mathf.Lerp(1.8f, 4.2f, Mathf.Clamp01(alt0 / 2f)) * (isHeli ? 0.8f : 1f);
        spin = (G.R01() < 0.5f ? -1f : 1f) * (isHeli ? 420f : 150f);
        ejected = false;
        crewSurvived = false;
        if (isPlane && def.eject) willEject = G.R01() < 0.7f;
        else willEject = false;
        if (isHeli) heliCrewSurvives = G.R01() < 0.3f;
        Sfx.Play(Sfx.Id.Explosion, pos, 0.5f);
        Fx.Explosion(pos, 0.9f, false);
    }
    bool willEject, heliCrewSurvives;

    void DoEject()
    {
        ejected = true;
        // pilot (and troops aboard a transport) parachute out
        var pd = Db.Get("inf_pilot");
        var s = Soldier.Spawn(pd, brig, fac, pos, rot);
        s.StartChute(pos + G.Dir(rot + 90f) * 1.0f, 10f + alt0 * 8f);
        s.home = home;
        Fx.Explosion(pos + G.Dir(rot + 90f), 0.35f, false);
        Sfx.Play(Sfx.Id.Rocket, pos, 0.5f);
        if (def.special == "transport" && cargo.Count > 0)
        {
            foreach (var c in cargo.ToArray()) { c.hidden = false; c.ExitVehicle(this, pos); c.StartChute(pos + UnityEngine.Random.insideUnitCircle * 2f, 9f + alt0 * 7f); }
            cargo.Clear();
        }
        if (crewN2 > 1) { var p2 = Soldier.Spawn(pd, brig, fac, pos, rot); p2.StartChute(pos + G.Dir(rot - 90f) * 1.0f, 10f + alt0 * 8f); p2.home = home; }
    }
    int crewN2 { get { return def.id == "av_tu22" || def.id == "av_tu95" || def.id == "av_tu160" || def.id == "av_su34" || def.id == "av_su24" || def.id == "av_il76" ? 2 : 1; } }

    void Impact()
    {
        if (dead) return;
        dead = true; state = AState.Wreck; alt = 0f; speed = 0f; hp = 0f;
        float size = Mathf.Clamp(def.len * 0.28f, 1.2f, 4.5f);
        Fx.Explosion(pos, size, true);
        Combat.Explode(pos, 4f + def.len * 0.25f, 110f, 6f, this, Faction.Neutral, false, true, true, true);
        Game.I.Shake(0.6f, pos);
        burnT = G.Rnd(45f, 90f);
        World.I.Scorch(pos, 2.5f + def.len * 0.2f);
        // crew die unless a helicopter hard-landing was survived
        if (isHeli && heliCrewSurvives && !isDrone)
        {
            var cd = Db.Get("inf_crew");
            for (int i = 0; i < 2; i++)
            {
                var s = Soldier.Spawn(cd, brig, fac, pos + UnityEngine.Random.insideUnitCircle * 2.5f, G.Rnd(0, 360)); s.hp = s.maxHp * 0.3f;
            }
            crewSurvived = true;
        }
        foreach (var c in cargo.ToArray()) { c.ExitVehicle(this, pos + UnityEngine.Random.insideUnitCircle * 3f); if (crewSurvived && G.R01() < 0.4f) c.hp = c.maxHp * 0.25f; else c.Hit(500f, 9f, pos, null, true); }
        cargo.Clear();
        Fx.Debris(pos, 14, 9f);
        bodySR.color = new Color(0.2f, 0.18f, 0.16f, 1f); bodySR.sortingOrder = 5; shadowSR.gameObject.SetActive(false);
        foreach (var f in flames) f.gameObject.SetActive(false);
        foreach (var p in props) p.gameObject.SetActive(false); foreach (var p in propBlur) p.gameObject.SetActive(false);
        foreach (var r in rotorBlurs) r.gameObject.SetActive(false);
        foreach (var r in rotors) { r.sortingOrder = 6; r.color = new Color(0.2f, 0.2f, 0.2f, 1f); r.transform.localRotation = Quaternion.Euler(0, 0, G.Rnd(0, 360)); }
        if (tailRotSR != null) tailRotSR.sortingOrder = 6;
        tr.localScale = Vector3.one;
        rot += G.Rnd(-25f, 25f);
        UpdateTransforms();
    }

    protected float CruiseSpeed()
    {
        float f = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(engine / 60f)) * (Stage >= 3 ? 0.88f : 1f);
        return maxSpeed * f * (retreating ? 1.1f : 1f);
    }

    public override void Tick(float dt)
    {
        if (removed) return;
        flareCd -= dt;
        if (dead || state == AState.Wreck) { burnT -= dt; return; }
        if (state == AState.Crashing) { TickCrash(dt); return; }
        thinkT -= dt;
        if (thinkT <= 0f)
        {
            thinkT = 0.3f + G.Rnd(0f, 0.15f);
            if (weps.Count > 0 || def.aura != Aura.None || isDrone) Think();
            ThreatCheck();
        }
        AutoAir(dt);
        if (state == AState.Parked) { speed = G.Approach(speed, 0f, 8f * dt); rotorRatio = G.Approach(rotorRatio, assaultActive ? 0.4f : (cargo.Count > 0 && unloading ? 0.4f : 0f), dt * 0.25f); UpdateUnload(dt); ProcessAura(dt); if (unloading && isHeli) rotorRatio = 0.45f; return; }
        FlyControl(dt);
        UpdateWeapons(dt);
        ProcessAura(dt);
        // mission-specific
        TickMissions(dt);
        pos += vel * dt;
        pos = World.I.Clamp(pos, 1f);
    }

    void ThreatCheck()
    {
        if (!def.flares || flaresLeft <= 0 || flareCd > 0f) return;
        float d;
        if (Combat.Threatened(this, out d) && d < 42f) LaunchFlares();
    }

    // altitude, orbit & order bookkeeping
    protected virtual void AutoAir(float dt)
    {
        // altitude
        float tgtAlt = state == AState.Parked ? 0f : targetAlt;
        if (state == AState.TakeOff)
        {
            rotorRatio = G.Approach(rotorRatio, 1f, dt * 0.35f);
            if (isHeli)
            {
                if (rotorRatio > 0.9f) alt = G.Approach(alt, targetAlt, dt * 0.6f);
                if (alt >= targetAlt - 0.02f) { state = AState.Flying; }
            }
            else
            {
                speed = G.Approach(speed, CruiseSpeed(), dt * maxSpeed * 0.28f);
                Vector2 d = G.Dir(rot); vel = d * speed; pos += vel * dt;
                if (speed > CruiseSpeed() * 0.62f) alt = G.Approach(alt, targetAlt, dt * 0.45f);
                if (alt >= targetAlt - 0.02f) state = AState.Flying;
                if (!World.I.InBounds(pos)) { pos = World.I.Clamp(pos, 2f); rot += 180f; }
            }
            return;
        }
        if (state == AState.Landing) return;
        alt = G.Approach(alt, targetAlt, dt * 0.55f);
        rotorRatio = G.Approach(rotorRatio, 1f, dt * 0.5f);
    }

    protected virtual void FlyControl(float dt)
    {
        float cruise = CruiseSpeed();
        // determine desired waypoint
        Vector2 target = wp; bool hasT = hasWp;
        Ent combatT = null;
        if (weps.Count > 0 && !retreating && stance != Stance.Passive && !dropActive && !assaultActive && !landingRunway)
        {
            foreach (var w in weps) if (w.tgt != null && !w.tgt.removed && !w.tgt.dead) { combatT = w.tgt; break; }
            if (combatT == null && (ord == OrderT.None || ord == OrderT.Hold) && stance == Stance.Aggressive && Game.I.aiOpt.chase) combatT = tgtG != null ? tgtG : tgtA;
            if (ord == OrderT.Attack && IsEnemyValid(ordTarget)) combatT = ordTarget;
        }
        if (landingRunway) { target = LandingTarget(dt, out hasT); }
        else if (dropActive) { target = DropTarget(dt, out hasT); }
        else if (combatT != null && (ord == OrderT.None || ord == OrderT.Attack || ord == OrderT.Hold && false))
        {
            if (isPlane || isDrone && def.special == "ucav")
            {
                float d = (combatT.pos - pos).magnitude;
                if (breakaway) { breakT -= dt; target = pos + G.Dir(rot) * 80f; if (breakT <= 0f) breakaway = false; }
                else { target = combatT.pos + combatT.vel * 0.5f; if (d < 12f) { breakaway = true; breakT = G.Rnd(2.2f, 3.4f); } }
                hasT = true;
            }
            else { target = combatT.pos; hasT = true; }
        }
        else if (ord == OrderT.Follow && ordTarget != null && !ordTarget.removed) { target = ordTarget.pos; hasT = true; }
        else if (!hasWp || ord == OrderT.None)
        {
            if (isPlane || isDrone && IsFixedWing())
            {
                // orbit around the anchor
                float R = orbitR; phi += orbitDir * (cruise / R) * Mathf.Rad2Deg * 0.9f * dt;
                target = anchor + G.Dir(phi) * R; hasT = true;
            }
            else hasT = false;
        }
        if (isPlane || (isDrone && IsFixedWing()))
        {
            float spd = cruise * (landingRunway && landPhase >= 1 ? 0.55f : 1f);
            if (hasT)
            {
                Vector2 dv = target - pos; float want = G.Ang(dv);
                float tn = def.turn * Mathf.Clamp(speed / Mathf.Max(1f, cruise), 0.4f, 1.1f);
                if (Stage >= 4 && state == AState.Flying) tn *= 0.8f;
                rot += Mathf.Clamp(Mathf.DeltaAngle(rot, want), -tn * dt, tn * dt);
                if (ord == OrderT.Move && !landingRunway && dv.magnitude < Mathf.Max(8f, orbitR * 0.3f)) { ord = OrderT.None; anchor = dest; hasWp = false; }
            }
            speed = G.Approach(speed, spd, maxSpeed * 0.35f * dt);
            // keep inside the map
            if (!World.I.InBounds(pos + G.Dir(rot) * 20f)) { Vector2 c = new Vector2(World.I.w / 2f, World.I.h / 2f); rot += Mathf.Clamp(Mathf.DeltaAngle(rot, G.Ang(c - pos)), -def.turn * dt * 1.4f, def.turn * dt * 1.4f); }
            vel = G.Dir(rot) * speed;
        }
        else
        {
            // rotary wing / quad: free hover
            Vector2 toT = hasT ? target - pos : Vector2.zero; float dist = toT.magnitude;
            float want = hasT ? Mathf.Min(cruise, dist * 1.1f) : 0f;
            if (dist < 1.2f) want = 0f;
            if (ord == OrderT.Move && hasT && dist < 2.5f && !assaultActive) { ord = OrderT.None; hasWp = false; }
            Vector2 desired = dist > 0.01f ? toT / dist * want : Vector2.zero;
            if (isDrone && combatT != null && def.special == "bomber") { desired = (combatT.pos - pos).normalized * Mathf.Min(cruise, (combatT.pos - pos).magnitude); }
            vel = Vector2.Lerp(vel, desired, 1f - Mathf.Exp(-dt * (isDrone ? 3.5f : 1.6f)));
            speed = vel.magnitude;
            float face = rot;
            if (combatT != null && weps.Count > 0 && !isDrone) face = G.Ang(combatT.pos - pos);
            else if (speed > 2f) face = G.Ang(vel);
            else if (combatT != null) face = G.Ang(combatT.pos - pos);
            else face = rot;
            rot += Mathf.Clamp(Mathf.DeltaAngle(rot, face), -def.turn * dt, def.turn * dt);
            // helicopter landing / assault logic
            if (isHeli) HeliMissions(dt, dist);
        }
    }
    protected bool IsFixedWing() { return def.style == 0 || def.style >= 5 && def.style != 1 && def.style != 2 && def.style != 3 && def.style != 4 && !(this is Drone && (def.style == 1 || def.style == 2 || def.style == 3 || def.style == 4)); }

    void HeliMissions(float dt, float dist)
    {
        if (ord == OrderT.Land && hasWp && dist < 2f)
        {
            state = AState.Landing; targetAlt = 0f; ord = OrderT.None; hasWp = false;
        }
        if (assaultActive && hasWp && dist < 3f && state == AState.Flying) { state = AState.Landing; targetAlt = 0f; hasWp = false; }
        if (state == AState.Landing)
        {
            vel = Vector2.Lerp(vel, Vector2.zero, dt * 3f);
            alt = G.Approach(alt, 0f, dt * 0.6f);
            if (alt <= 0.02f) { alt = 0f; state = AState.Parked; vel = Vector2.zero; speed = 0f; if (assaultActive && cargo.Count > 0) { unloading = true; } }
        }
    }

    Vector2 LandingTarget(float dt, out bool has)
    {
        has = true;
        if (landPhase == 0)
        {
            if ((pos - wp).magnitude < 12f) landPhase = 1;
            return wp;
        }
        // final approach: descend toward the threshold
        Vector2 end = rwStart + rwDir * 30f;
        float dist = (rwStart - pos).magnitude * Mathf.Sign(Vector2.Dot(rwStart - pos, rwDir) + 0.001f);
        float distThr = Vector2.Dot(rwStart - pos, rwDir);
        if (landPhase == 1)
        {
            targetAlt = Mathf.Max(0f, Mathf.Clamp01(distThr / 60f) * def.alt);
            alt = G.Approach(alt, targetAlt, dt * 0.6f);
            if (distThr <= 1f && alt < 0.35f) { landPhase = 2; state = AState.Landing; alt = 0f; }
            return rwStart + rwDir * 10f;
        }
        // rolling out
        alt = 0f; targetAlt = 0f;
        speed = G.Approach(speed, 0f, 9f * dt);
        if (speed < 1f) { state = AState.Parked; landingRunway = false; ord = OrderT.None; speed = 0; vel = Vector2.zero; rotorRatio = 0; targetAlt = 0; }
        return rwStart + rwDir * 200f;
    }

    Vector2 DropTarget(float dt, out bool has)
    {
        has = true;
        if (dropPhase == 0)
        {
            if ((pos - dropStart).magnitude < 14f) { dropPhase = 1; }
            return dropStart;
        }
        // fly through the drop zone releasing a trooper every ~0.3 s
        float along = Vector2.Dot(pos - dropTarget, (dropEnd - dropStart).normalized);
        dropT -= dt;
        if (Mathf.Abs(along) < 22f && cargo.Count > 0 && dropT <= 0f)
        {
            dropT = 0.28f;
            var s = cargo[cargo.Count - 1]; cargo.RemoveAt(cargo.Count - 1);
            s.ExitVehicle(this, pos);
            s.StartChute(pos + UnityEngine.Random.insideUnitCircle * 1.2f, 14f + alt * 8f);
            s.OrderMove(dropTarget + UnityEngine.Random.insideUnitCircle * 12f);
        }
        if (along > 25f || (cargo.Count == 0 && along > 0f)) { dropActive = false; ord = OrderT.None; anchor = dropEnd; hasWp = false; }
        return dropEnd;
    }

    protected virtual void TickMissions(float dt) { }

    void TickCrash(float dt)
    {
        crashT += dt;
        float k = crashT / crashDur;
        spin *= 1f + dt * 0.5f;
        rot += spin * dt;
        alt = Mathf.Max(0f, alt0 * (1f - k * k));
        speed = Mathf.Max(speed * (1f - dt * 0.35f), 3f);
        if (isHeli) vel = Vector2.Lerp(vel, G.Dir(rot) * 3f, dt); else vel = G.Dir(rot) * speed;
        pos += vel * dt;
        if (willEject && !ejected && crashT > Mathf.Min(1.1f, crashDur * 0.4f)) DoEject();
        if (alt <= 0.05f || crashT >= crashDur) { alt = 0; Impact(); }
        pos = World.I.Clamp(pos, 1f);
    }

    // ------------------------------------------------------------ visuals
    protected void UpdateTransforms()
    {
        float sc = 1f + alt * 0.11f;
        tr.position = new Vector3(pos.x, pos.y, 0);
        tr.rotation = Quaternion.Euler(0, 0, rot);
        tr.localScale = Vector3.one * sc;
        int bo = 40 + (int)(alt * 8f);
        if (!dead) { bodySR.sortingOrder = bo; foreach (var f in flames) f.sortingOrder = bo - 1; foreach (var p in props) p.sortingOrder = bo + 4; foreach (var p in propBlur) p.sortingOrder = bo + 3; foreach (var r in rotors) r.sortingOrder = bo + 6; foreach (var r in rotorBlurs) r.sortingOrder = bo + 5; if (tailRotSR != null) tailRotSR.sortingOrder = bo + 6; }
        if (shadowSR != null && !dead)
        {
            Vector2 off = new Vector2(0.3f + alt * 1.1f, -0.3f - alt * 1.5f);
            shadowSR.transform.position = new Vector3(pos.x + off.x, pos.y + off.y, 0);
            shadowSR.transform.localScale = Vector3.one * (1f / sc) * (1f - Mathf.Clamp(alt, 0, 2) * 0.08f);
            shadowSR.transform.rotation = Quaternion.Euler(0, 0, rot);
            shadowSR.color = new Color(1, 1, 1, state == AState.Parked ? 0.7f : 0.85f);
        }
    }

    public override void Visual(float dt)
    {
        if (removed) return;
        UpdateTransforms();
        float flick = 0.85f + 0.3f * UnityEngine.Random.value;
        bool running = !dead && state != AState.Parked && state != AState.Crashing;
        float spf = Mathf.Clamp01(speed / Mathf.Max(1f, maxSpeed));
        for (int i = 0; i < flames.Count; i++)
        {
            bool on = (running || state == AState.Crashing && !dead) && (isPlane || isDrone) && m.prop == null;
            flames[i].gameObject.SetActive(on && (state == AState.TakeOff || spf > 0.5f));
            if (on) { float s = m.engSize * (0.8f + spf * 0.9f) * flick; flames[i].transform.localScale = new Vector3(s * 1.6f, s * 0.6f, 1f); flames[i].transform.localPosition = (Vector3)m.eng[i] + new Vector3(-0.45f * s, 0, 0); flames[i].color = new Color(1f, 0.6f + 0.2f * flick, 0.3f, 0.9f); }
        }
        if (!dead)
        {
            propAng += dt * 1800f;
            bool propOn = state != AState.Parked || rotorRatio > 0.05f;
            for (int i = 0; i < props.Count; i++)
            {
                props[i].gameObject.SetActive(true); propBlur[i].gameObject.SetActive(true);
                float on2 = (state == AState.Crashing ? 0.6f : (propOn ? 1f : 0.05f));
                if (isDrone) on2 = engine > 0 ? 1f : 0f;
                props[i].transform.localRotation = Quaternion.Euler(0, 0, propAng * (i % 2 == 0 ? 1 : -1) * on2);
                propBlur[i].color = new Color(1, 1, 1, 0.9f * on2);
                props[i].color = new Color(1, 1, 1, Mathf.Lerp(1f, 0.25f, on2));
            }
            if (isHeli)
            {
                float rr = rotorRatio; rotorAng += dt * 900f * rr * (state == AState.Crashing ? 0.7f : 1f);
                for (int i = 0; i < rotors.Count; i++)
                {
                    rotors[i].transform.localRotation = Quaternion.Euler(0, 0, rotorAng * (i % 2 == 0 ? 1 : -1));
                    rotors[i].color = new Color(1, 1, 1, Mathf.Lerp(1f, 0.55f, rr));
                    rotorBlurs[i].color = new Color(1, 1, 1, Mathf.Clamp01(rr * 1.2f));
                }
                if (tailRotSR != null) tailRotSR.transform.localRotation = Quaternion.Euler(0, 0, rotorAng * 1.8f);
                if (rr > 0.2f) Sfx.Emit(Sfx.Loop.Heli, pos, rr);
            }
            else if (isPlane && state != AState.Parked) Sfx.Emit(m.prop != null ? Sfx.Loop.Prop : Sfx.Loop.Jet, pos, 0.4f + spf * 0.6f);
            else if (isDrone && state != AState.Parked) Sfx.Emit(Sfx.Loop.Drone, pos, 0.35f + 0.4f * spf);
        }
        if (visible) EmitTrails(dt);
    }

    void EmitTrails(float dt)
    {
        smokeT -= dt; fireT -= dt; sparkT -= dt; trailT -= dt;
        Vector2 rear = pos + G.Dir(rot + 180f) * def.len * 0.4f;
        if (dead)
        {
            if (burnT > 0f)
            {
                if (fireT <= 0f) { fireT = 0.08f; Fx.Fire(pos + UnityEngine.Random.insideUnitCircle * radius * 0.6f, 1f); }
                if (smokeT <= 0f) { smokeT = 0.16f; Fx.Smoke(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, 1.8f, true, Vector2.up); }
                Fx.Glow(pos, 3.5f, new Color(1f, 0.5f, 0.15f, 0.2f), 0.1f);
            }
            else if (smokeT <= 0f) { smokeT = 0.7f; Fx.Smoke(pos, 1f, false, Vector2.up * 0.5f); }
            return;
        }
        int st = state == AState.Crashing ? 4 : Stage;
        if (state == AState.Crashing)
        {
            if (smokeT <= 0f) { smokeT = 0.05f; Fx.Smoke(rear, 2.2f, true, -G.Dir(rot) * 2f); }
            if (fireT <= 0f) { fireT = 0.04f; Fx.Fire(rear, 1.2f); }
            if (sparkT <= 0f) { sparkT = 0.12f; Fx.Debris(pos, 1, 3f); }
            return;
        }
        if (st == 2 && smokeT <= 0f) { smokeT = 0.2f; Fx.Smoke(rear, 0.9f, false, Vector2.zero); }
        if (st == 3 && smokeT <= 0f) { smokeT = 0.08f; Fx.Smoke(rear, 1.4f, true, Vector2.zero); }
        if (st >= 4) { if (smokeT <= 0f) { smokeT = 0.05f; Fx.Smoke(rear, 1.7f, true, Vector2.zero); } if (fireT <= 0f) { fireT = 0.05f; Fx.Fire(rear, 0.9f); } }
        if (st >= 3 && sparkT <= 0f) { sparkT = G.Rnd(0.4f, 1f); Fx.Spark(pos, Vector2.up, 3); }
        if (!isDrone && alt > 1.5f && isPlane && trailT <= 0f && def.len > 6f) { trailT = 0.12f; Fx.Puff(rear, new Color(1, 1, 1, 0.07f), 0.7f, 2.2f, Vector2.zero); }
        if (isHeli && alt < 0.25f && rotorRatio > 0.6f && trailT <= 0f) { trailT = 0.1f; byte t = World.I.At(pos); Fx.Dust(pos + UnityEngine.Random.insideUnitCircle * def.wid * 0.35f, 1.4f, t == World.BEACH ? new Color(0.9f, 0.85f, 0.6f, 0.35f) : new Color(0.6f, 0.55f, 0.4f, 0.3f)); }
        if (isHeli && alt < 0.6f && state != AState.Parked && trailT <= 0f) { trailT = 0.12f; Fx.Dust(pos + UnityEngine.Random.insideUnitCircle * def.wid * 0.3f, 1.0f, new Color(0.6f, 0.55f, 0.4f, 0.2f)); }
    }

    public override void OnRemove() { base.OnRemove(); }
}
