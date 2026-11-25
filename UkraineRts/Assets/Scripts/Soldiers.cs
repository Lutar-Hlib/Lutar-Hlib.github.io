using System;
using System.Collections.Generic;
using UnityEngine;

public class KitItem { public UnitDef d; public int n; }

public class Soldier : Unit
{
    public Role role;
    Model mdl; SpriteRenderer sr, chuteSR, shadowSR;
    public int mag = 30, magSize = 30; float reloadT, hitT, shootT, dieT, opT, landT;
    public bool crouching, wading, chuting, running, corpseGone;
    public Unit inVeh;
    float chuteZ, chuteAge; Vector2 drift;
    public List<KitItem> kit = new List<KitItem>();
    public List<Drone> drones = new List<Drone>();
    float droneCd = 3f;
    float healT;
    public Vector2 wadeTarget; public bool hasWadeTarget;
    float swayPhase;

    public override float Mass { get { return 1f; } }
    public override bool Targetable { get { return !dead && !removed && !hidden; } }

    public static Soldier Spawn(UnitDef d, Brigade b, Faction f, Vector2 p, float rot)
    {
        var s = new Soldier();
        s.InitUnit(d, b, f, p, rot);
        s.role = d.role;
        s.mdl = Art.Get(d, b, f);
        s.sr = Util.SR(s.tr, "body", s.mdl.fr[0], 20, Vector2.zero);
        s.shadowSR = Util.SR(s.tr, "shadow", s.mdl.fr[0], 19, new Vector2(0.06f, -0.06f));
        s.shadowSR.color = new Color(0, 0, 0, 0.3f);
        s.maxSpeed = d.speed;
        s.magSize = d.role == Role.MG ? 60 : (d.role == Role.Sniper ? 5 : 30); s.mag = s.magSize;
        s.radius = 0.4f;
        s.swayPhase = G.Rnd(0, 6f);
        if (d.kit != "") s.ParseKit(d.kit);
        s.tr.rotation = Quaternion.Euler(0, 0, rot);
        return s;
    }

    public void ParseKit(string k)
    {
        kit.Clear();
        foreach (var part in k.Split(','))
        {
            var kv = part.Split(':'); if (kv.Length != 2) continue;
            UnitDef d = Db.Get(kv[0]); if (d == null) continue;
            kit.Add(new KitItem { d = d, n = int.Parse(kv[1]) });
        }
    }

    public override string Status()
    {
        if (dead) return "Killed in action";
        if (hidden && inVeh != null) return "Inside " + inVeh.def.name;
        if (chuting) return "Parachuting";
        if (reloadT > 0) return "Reloading";
        if (opT > 0 || drones.Count > 0) return "Operating drone";
        if (retreating) return "Retreating";
        if (hp < maxHp * 0.35f) return "Wounded";
        if (tgtG != null || tgtA != null) return "Engaging";
        if (speed > 0.2f) return running ? "Running" : "Moving";
        return crouching ? "Crouching" : "Idle";
    }

    public void StartChute(Vector2 p, float z)
    {
        chuting = true; chuteZ = z; chuteAge = 0f; hidden = false; if (go) go.SetActive(true);
        SetPos(p); path = null; ord = OrderT.None;
        if (chuteSR == null) { chuteSR = Util.SR(tr, "chute", Art.Spr.canopy, 46, Vector2.zero); }
        chuteSR.gameObject.SetActive(true);
        chuteSR.transform.localScale = Vector3.one * 0.2f;
        drift = new Vector2(G.Rnd(-0.6f, 0.6f), G.Rnd(-0.4f, 0.4f)) + World.I.WindVec();
        sr.sortingOrder = 45;
    }

    public void EnterVehicle(Unit v)
    {
        inVeh = v; hidden = true; SetSelected(false); ord = OrderT.None; path = null; speed = 0;
        if (go) go.SetActive(false);
        Game.I.sel.Remove(this);
    }
    public void ExitVehicle(Unit v, Vector2 p)
    {
        inVeh = null; hidden = false; if (go) go.SetActive(true);
        SetPos(p); rot = v != null ? v.rot + 90f : rot; tr.rotation = Quaternion.Euler(0, 0, rot);
        ord = OrderT.None; path = null; speed = 0;
        if (v is Ship && v.cargo != null) { wading = true; Vector2 shore = World.I.NearestOfType(p, t => !World.IsWater(t) && t != World.BEACH + 100, 40f); wadeTarget = shore; hasWadeTarget = true; }
    }

    public void OrderBoard(Unit v)
    {
        if (v == null || v.dead) return;
        ord = OrderT.Board; ordTarget = v; SetPath(v.pos); retreating = false;
    }

    protected override bool Passable(Vector2 p)
    {
        if (!World.I.InBounds(p)) return false;
        if (wading) return true;
        return World.I.SpeedMul(Move.Foot, p) > 0f;
    }
    protected override bool CanFire() { return !dead && reloadT <= 0f && !chuting && !hidden && landT <= 0f; }
    protected override Vector2 MuzzlePos(WS s) { return pos + G.Dir(rot) * 0.65f; }
    protected override float AimAngle(WS s) { return rot; }
    protected override bool WeaponCanTarget(WS s, Ent t)
    {
        if (s.d.t == WT.Rocket && t.kind == Kind.Soldier) return false;
        if (s.d.t == WT.Sniper && t.IsAirKind) return false;
        return true;
    }
    protected override float SpreadMul() { return (crouching ? 0.7f : 1f) * (speed > 0.5f ? 1.5f : 1f) * (hp < maxHp * 0.4f ? 1.3f : 1f); }
    protected override void OnFired(WS s)
    {
        shootT = 0.18f;
        if (s.d.t == WT.Rocket) { mag = Mathf.Max(0, mag); return; }
        if (s.d.t == WT.Rifle && def.role == Role.Operator) return;
        mag--;
        if (mag <= 0 && def.role != Role.Operator && def.role != Role.Medic && def.role != Role.Crew && def.role != Role.Pilot)
        { reloadT = def.role == Role.MG ? 4f : 2.4f; mag = magSize; foreach (var w in weps) w.burst = 0; }
    }
    protected override bool Aligned(WS s, Ent t)
    {
        float want = G.Ang(t.pos - pos);
        return Mathf.Abs(Mathf.DeltaAngle(rot, want)) < 14f;
    }
    protected override bool Spotter() { return role == Role.Recon || role == Role.Operator; }

    public override void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false)
    {
        if (dead || chuting && chuteZ > 6f && !expl) return;
        float eff = dmg * Mathf.Clamp(pen / 0.5f, 0.2f, 1.3f);
        if (expl) eff *= 1.25f;
        if (crouching) eff *= 0.8f;
        hp -= eff; hitT = 0.28f; lastHit = World.I.simTime; if (src != null) lastAttacker = src;
        if (eff > 4f && G.R01() < 0.4f) World.I.Blood(pos + UnityEngine.Random.insideUnitCircle * 0.3f);
        if (eff > 3f) Fx.Blood(pos);
        if (hp <= 0f) Die(src);
        else if (Game.I.aiOpt.reactToDamage && !retreating) { crouching = true; if (src != null && IsEnemyOf(src) && ord == OrderT.None && (tgtG == null)) { thinkT = 0f; } }
    }

    void Die(Ent killer)
    {
        dead = true; dieT = 0f; hp = 0; speed = 0; path = null; ord = OrderT.None; crouching = false;
        if (killer != null) killer.kills++;
        SetSelected(false); Game.I.sel.Remove(this);
        if (chuting) { chuting = false; if (chuteSR != null) chuteSR.gameObject.SetActive(false); sr.sortingOrder = 4; }
        sr.sortingOrder = 3; shadowSR.gameObject.SetActive(false);
        World.I.Blood(pos); Sfx.Play(Sfx.Id.Death, pos, 0.5f);
        foreach (var d in drones) if (d != null) d.OwnerLost();
        Game.I.RegisterCorpse(this);
    }

    public override void Tick(float dt)
    {
        if (removed) return;
        if (dead) { dieT += dt; return; }
        if (hidden)
        {
            if (inVeh != null) { pos = inVeh.pos; }
            return;
        }
        hitT -= dt; shootT -= dt; reloadT -= dt; opT -= dt; landT -= dt; droneCd -= dt;
        if (chuting) { TickChute(dt); return; }
        // think
        thinkT -= dt;
        if (thinkT <= 0f)
        {
            thinkT = 0.35f + G.Rnd(0f, 0.15f);
            Think();
            bool hasT = false; foreach (var w in weps) if (w.tgt != null) hasT = true;
            crouching = hasT && path == null && (role != Role.Medic);
            if (role == Role.Operator) OperatorAI();
            if (role == Role.Medic || role == Role.Engineer) SupportAI();
            // hiding from air? keep simple
        }
        AutoBehavior(dt);
        ProcessAura(dt);
        // movement
        bool moving = false;
        if (ord == OrderT.Board)
        {
            Unit v = ordTarget as Unit;
            if (v == null || v.removed || v.dead) { ord = OrderT.None; }
            else
            {
                float d = (v.pos - pos).magnitude;
                if (d < v.radius + 1.6f) { if (!v.Load(this)) ord = OrderT.None; return; }
                repathT -= dt; if (repathT <= 0f || path == null) { SetPath(v.pos); repathT = 1f; }
            }
        }
        if (hasWadeTarget && wading)
        {
            Vector2 d = wadeTarget - pos;
            if (d.magnitude < 1f || !World.I.Water(pos) && World.I.At(pos) != World.BEACH + 200 && d.magnitude < 3f) { wading = false; hasWadeTarget = false; }
            else { path = new List<Vector2> { wadeTarget }; pi = 0; }
        }
        if (path != null && pi < path.Count)
        {
            Vector2 wp = path[pi]; Vector2 d = wp - pos; float dist = d.magnitude;
            bool last = pi == path.Count - 1;
            if (dist < (last ? 0.5f : 1.6f)) { pi++; if (pi >= path.Count) { path = null; if (ord == OrderT.Move || ord == OrderT.Return) ord = OrderT.None; autoAdvance = false; } }
            else
            {
                float remain = dist; for (int i = pi + 1; i < path.Count; i++) remain += (path[i] - path[i - 1]).magnitude;
                running = retreating || remain > 14f || (Time.time - lastHit < 3f && World.I.simTime - lastHit < 3f);
                float terr = Mathf.Max(0.15f, wading ? 0.4f : World.I.SpeedMul(Move.Foot, pos));
                float sp = maxSpeed * (running ? 1.9f : 1f) * terr * (hp < maxHp * 0.3f ? 0.7f : 1f);
                if (reloadT > 0f || hitT > 0.1f) sp *= 0.4f;
                speed = G.Approach(speed, sp, 12f * dt);
                Vector2 dir = d / dist;
                bool hasT = false; Ent tt = null; foreach (var w in weps) if (w.tgt != null) { hasT = true; tt = w.tgt; break; }
                float face = hasT && !retreating ? G.Ang(tt.pos - pos) : G.Ang(d);
                rot += Mathf.Clamp(Mathf.DeltaAngle(rot, face), -540f * dt, 540f * dt);
                Vector2 np = pos + dir * speed * dt;
                if (Passable(np)) { pos = np; distTravel += speed * dt; }
                else if (Passable(new Vector2(np.x, pos.y))) pos = new Vector2(np.x, pos.y);
                else if (Passable(new Vector2(pos.x, np.y))) pos = new Vector2(pos.x, np.y);
                else { blockT += dt; speed = 0; }
                vel = dir * speed; moving = true; crouching = false;
            }
        }
        if (!moving)
        {
            speed = G.Approach(speed, 0, 14f * dt); vel = Vector2.zero;
            Ent tt = null; foreach (var w in weps) if (w.tgt != null) { tt = w.tgt; break; }
            if (tt != null) rot += Mathf.Clamp(Mathf.DeltaAngle(rot, G.Ang(tt.pos - pos)), -540f * dt, 540f * dt);
        }
        if (blockT > 1.2f && path != null && (ord == OrderT.Move || ord == OrderT.Return)) { blockT = 0; SetPath(dest); }
        if (blockT > 3f) blockT = 0f;
        Separate(dt, false);
        pos = World.I.Clamp(pos, 1f);
        UpdateWeapons(dt);
        if (World.I.Water(pos) && !wading && !chuting) { hp -= 40f * dt; if (hp <= 0) Die(null); }   // drowned
    }

    void TickChute(float dt)
    {
        chuteAge += dt;
        float fall = chuteAge < 0.9f ? 9f : 3.4f;
        chuteZ -= fall * dt;
        pos += (drift + new Vector2(Mathf.Sin(chuteAge * 1.3f + swayPhase) * 0.4f, 0f)) * dt;
        pos = World.I.Clamp(pos, 1f);
        SetPos(pos);
        if (chuteZ <= 0f)
        {
            chuting = false; chuteZ = 0; landT = 0.9f; crouching = true;
            sr.sortingOrder = 20;
            if (chuteSR != null) { Fx.CanopyCollapse(pos, rot); chuteSR.gameObject.SetActive(false); }
            if (World.I.Water(pos) && !(wading)) { Fx.Splash(pos, 1.2f); hp = 0; Die(null); return; }
            Fx.Dust(pos, 1.0f, Color.gray);
            thinkT = 0.3f;
            // paratroopers re-form at their landing point
            home = pos;
        }
    }

    // ---- support & operator behaviour
    void SupportAI()
    {
        if (ord != OrderT.None || path != null || (tgtG != null || tgtA != null) || stance == Stance.Hold) return;
        qbuf.Clear();
        World.I.Query(pos, 38f, qbuf, false);
        Ent best = null; float bd = 1e9f;
        for (int i = 0; i < qbuf.Count; i++)
        {
            Ent e = qbuf[i];
            if (e == this || e.dead || e.hidden || e.fac != fac) continue;
            bool need = role == Role.Medic ? (e.kind == Kind.Soldier && e.hp < e.maxHp * 0.85f) : (e.kind == Kind.Ground && e.hp < e.maxHp * 0.9f && e.Stage < 5);
            if (!need) continue;
            float d = (e.pos - pos).magnitude; if (d < bd) { bd = d; best = e; }
        }
        if (best != null && bd > 3f) { SetPath(best.pos); ord = OrderT.Move; dest = best.pos; }
    }

    public int DroneCount(string special) { int n = 0; foreach (var d in drones) if (d != null && !d.removed && !d.dead && d.def.special == special) n++; return n; }

    void OperatorAI()
    {
        drones.RemoveAll(d => d == null || d.removed || d.dead);
        if (kit.Count == 0 || droneCd > 0f || Game.I.aiOpt.ceasefire || stance == Stance.Passive) return;
        // attack drones at spotted enemies
        Ent tgt = tgtG;
        if (tgt != null)
        {
            foreach (var k in kit)
                if (k.n > 0 && (k.d.special == "fpv" || k.d.special == "loiter" || k.d.special == "bomber" || k.d.special == "ucav") && (tgt.pos - pos).magnitude < k.d.signal * 0.8f)
                { if (DroneCount(k.d.special) < 2) { LaunchDrone(k, tgt.pos, tgt); droneCd = 7f; return; } }
        }
        foreach (var k in kit)
            if (k.n > 0 && k.d.special == "recon" && DroneCount("recon") == 0 && tgt == null)
            {
                Vector2 toward = (Game.I.EnemySide(fac) - pos).normalized;
                LaunchDrone(k, pos + toward * Mathf.Min(70f, k.d.signal * 0.35f) + UnityEngine.Random.insideUnitCircle * 15f, null); droneCd = 10f; return;
            }
    }

    public bool LaunchDrone(KitItem k, Vector2 target, Ent targetEnt)
    {
        if (k == null || k.n <= 0 || dead || hidden) return false;
        k.n--; opT = 1.5f;
        var d = Drone.SpawnFrom(k.d, this, target, targetEnt);
        drones.Add(d);
        Sfx.Play(Sfx.Id.UiOpen, pos, 0.4f);
        return true;
    }
    public void Recover(Drone d)
    {
        foreach (var k in kit) if (k.d == d.def) { k.n++; return; }
        kit.Add(new KitItem { d = d.def, n = 1 });
    }

    public override void Visual(float dt)
    {
        if (removed) return;
        int f = Art.FR_IDLE;
        if (dead) f = dieT < 0.22f ? Art.FR_DIE1 : (dieT < 0.6f ? Art.FR_DIE2 : Art.FR_DEAD);
        else if (hitT > 0f) f = Art.FR_HIT;
        else if (chuting) f = Art.FR_IDLE;
        else if (reloadT > 0f) f = Art.FR_RELOAD;
        else if (opT > 0f || (role == Role.Operator && drones.Count > 0 && speed < 0.3f)) f = Art.FR_OPER;
        else if (shootT > 0f) f = Art.FR_SHOOT;
        else if (speed > 0.4f) { int ph = ((int)(distTravel * (running ? 2.6f : 3.4f))) & 1; f = running ? (ph == 0 ? Art.FR_RUN1 : Art.FR_RUN2) : (ph == 0 ? Art.FR_WALK1 : Art.FR_WALK2); }
        else if (crouching || landT > 0f) f = Art.FR_CROUCH;
        sr.sprite = mdl.fr[f];
        shadowSR.sprite = mdl.fr[f];
        tr.position = new Vector3(pos.x, pos.y, 0);
        float z = chuting ? chuteZ : 0f;
        sr.transform.localScale = Vector3.one * (1f + z * 0.012f);
        tr.rotation = Quaternion.Euler(0, 0, dead ? rot : rot);
        shadowSR.transform.position = new Vector3(pos.x + 0.06f + z * 0.5f, pos.y - 0.06f - z * 0.7f, 0);
        if (chuting && chuteSR != null)
        {
            float open = Mathf.Clamp01(chuteAge / 0.9f);
            float sc = Mathf.Lerp(0.15f, 0.9f, open) * (1f + z * 0.008f);
            chuteSR.transform.localScale = Vector3.one * sc;
            chuteSR.transform.position = new Vector3(pos.x, pos.y + 0.4f + z * 0.08f, 0);
            chuteSR.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(chuteAge * 1.6f + swayPhase) * 8f);
            chuteSR.color = new Color(1, 1, 1, Mathf.Lerp(0.4f, 1f, open));
        }
    }

    public override void OnRemove()
    {
        base.OnRemove();
    }
}
