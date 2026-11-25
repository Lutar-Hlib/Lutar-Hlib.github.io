using System;
using System.Collections.Generic;
using UnityEngine;

public enum OrderT { None, Move, Follow, Hold, Return, Board, Attack, Land, Paradrop, Assault, Unload }

public static class Util
{
    static Material addMat;
    public static Material Additive
    {
        get
        {
            if (addMat == null)
            {
                Shader s = Resources.Load<Shader>("RTSAdditive");
                if (s == null) s = Shader.Find("RTS/SpriteAdditive");
                if (s == null) s = Shader.Find("Sprites/Default");
                addMat = new Material(s);
            }
            return addMat;
        }
    }
    public static SpriteRenderer SR(Transform parent, string name, Sprite s, int order, Vector2 lp, float lrot = 0f, bool additive = false)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = new Vector3(lp.x, lp.y, 0);
        g.transform.localRotation = Quaternion.Euler(0, 0, lrot);
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s; sr.sortingOrder = order;
        if (additive) sr.sharedMaterial = Additive;
        return sr;
    }
}

public class WS
{
    public W d; public float cd; public int ammo; public int burst; public float burstT; public Ent tgt; public Vector2 aim; public int mount;
    public WS(W d) { this.d = d; ammo = d.ammo; cd = G.Rnd(0f, d.cd * 0.6f); }
}

public abstract class Ent
{
    public static int nextId = 1;
    public int id; public UnitDef def; public Kind kind; public Faction fac; public Brigade brig;
    public GameObject go; public Transform tr;
    public Vector2 pos; public float rot; public float hp, maxHp; public bool dead, removed, hidden, selected, visible = true;
    public int group = -1; public float radius = 1f; public Vector2 vel; public float alt;
    public float[] spot = { -99f, -99f, -99f };
    public Ent lastAttacker; public float lastHit = -99f; public int kills;
    public Vector2 home; public float homeRot;
    SpriteRenderer selSR;

    public bool Airborne { get { return alt > 0.35f; } }
    public bool IsAirKind { get { return kind == Kind.Plane || kind == Kind.Heli || kind == Kind.Drone; } }
    public virtual bool Targetable { get { return !dead && !removed && !hidden; } }
    public abstract void Tick(float dt);
    public virtual void Visual(float dt) { }
    public abstract void Hit(float dmg, float pen, Vector2 from, Ent src, bool expl, bool fire = false);
    public virtual string Status() { return dead ? "Destroyed" : "Operational"; }
    public virtual void OnRemove() { }
    public virtual float Mass { get { return 5f; } }
    public virtual int Stage { get { if (dead) return 5; float f = hp / maxHp; return f > 0.75f ? 1 : f > 0.5f ? 2 : f > 0.25f ? 3 : 4; } }

    public void Destroy()
    {
        if (removed) return;
        removed = true; selected = false;
        OnRemove();
        if (go != null) UnityEngine.Object.Destroy(go);
        if (Game.I != null) Game.I.sel.Remove(this);
    }

    public void SetSelected(bool s)
    {
        selected = s;
        if (s && selSR == null && go != null)
        {
            selSR = Util.SR(tr, "sel", Art.Spr.ring, 60, Vector2.zero);
            selSR.color = Fac.Color(fac);
            float sc = Mathf.Max(0.6f, radius * 1.25f + 0.35f);
            selSR.transform.localScale = new Vector3(sc, sc, 1);
        }
        if (selSR != null) { selSR.gameObject.SetActive(s); selSR.transform.rotation = Quaternion.identity; }
    }
    public void SpotBy(Faction f) { if ((int)f < 3) spot[(int)f] = World.I.simTime; }
    public bool SpottedBy(Faction f) { return (int)f < 3 && World.I.simTime - spot[(int)f] < 4f; }
    public bool IsEnemyOf(Ent o) { return o != null && fac != Faction.Neutral && o.fac != Faction.Neutral && fac != o.fac; }
    public virtual void SetPos(Vector2 p) { pos = p; if (tr != null) tr.position = new Vector3(p.x, p.y, 0); }
}

// ---------------------------------------------------------------------------
// Unit = mobile entity with orders, AI and weapons
// ---------------------------------------------------------------------------
public abstract class Unit : Ent
{
    public Stance stance = Stance.Aggressive;
    public OrderT ord = OrderT.None; public Vector2 dest; public Ent ordTarget;
    public List<Vector2> path; public int pi; protected float repathT, blockT;
    public List<WS> weps = new List<WS>();
    public Ent tgtG, tgtA; protected float thinkT;
    public float speed, maxSpeed, turnRate;
    public Move move;
    public bool retreating;
    public List<Soldier> cargo = new List<Soldier>(); public int cap;
    public float recoil;
    public float distTravel;
    public float fuel = 100f;
    public Sprite[] frames;
    protected static List<Ent> qbuf = new List<Ent>();
    protected float auraT;
    public float vis = 1f;

    public override float Mass { get { return 5f; } }

    protected void InitUnit(UnitDef d, Brigade b, Faction f, Vector2 p, float r)
    {
        id = Ent.nextId++; def = d; kind = d.kind; fac = f; brig = b; pos = p; rot = r; home = p; homeRot = r;
        hp = maxHp = d.hp; radius = d.radius; move = d.move; maxSpeed = d.speed; turnRate = d.turn;
        foreach (var w in d.w) weps.Add(new WS(w));
        cap = d.cap; thinkT = G.Rnd(0f, 0.5f);
        go = new GameObject(d.name + "#" + id); tr = go.transform; tr.position = new Vector3(p.x, p.y, 0);
        World.I.ents.Add(this);
    }

    // ------------------------------------------------------------ orders
    public virtual void OrderMove(Vector2 p, bool keepStance = false)
    {
        p = World.I.Clamp(p, 2f);
        ord = OrderT.Move; dest = p; ordTarget = null; retreating = false; blockT = 0f;
        SetPath(p);
    }
    protected virtual void SetPath(Vector2 p)
    {
        pi = 0;
        if (move == Move.Air || move == Move.None) { path = new List<Vector2> { p }; return; }
        if ((p - pos).magnitude < 6f) { path = new List<Vector2> { p }; return; }
        path = World.I.FindPath(move, pos, p);
        if (path == null) path = new List<Vector2> { p };
        repathT = 3f;
    }
    public virtual void OrderStop() { ord = OrderT.Hold; path = null; ordTarget = null; retreating = false; }
    public virtual void OrderReturn() { OrderMove(home); ord = OrderT.Return; }
    public virtual void OrderFollow(Ent e) { if (e == null || e == this) return; ord = OrderT.Follow; ordTarget = e; repathT = 0; path = null; }
    public virtual void OrderAttack(Ent e) { if (e == null) return; ord = OrderT.Attack; ordTarget = e; repathT = 0; path = null; }
    public bool Busy { get { return ord == OrderT.Move || ord == OrderT.Return || ord == OrderT.Follow || ord == OrderT.Board || ord == OrderT.Land || ord == OrderT.Paradrop; } }

    public virtual float SpeedFactor() { return 1f; }
    protected virtual bool Passable(Vector2 p)
    {
        if (!World.I.InBounds(p)) return false;
        return World.I.SpeedMul(move, p) > 0f;
    }

    // advance along path; returns true while still moving
    protected bool FollowPath(float dt, bool pivotTurn)
    {
        if (path == null || pi >= path.Count) return false;
        Vector2 wp = path[pi];
        Vector2 d = wp - pos; float dist = d.magnitude;
        bool last = pi == path.Count - 1;
        float arrive = last ? Mathf.Max(0.6f, radius * 0.6f) : 2.2f;
        if (dist < arrive)
        {
            pi++;
            if (pi >= path.Count) { path = null; return false; }
            wp = path[pi]; d = wp - pos; dist = d.magnitude; last = pi == path.Count - 1;
        }
        float want = G.Ang(d);
        float diff = Mathf.DeltaAngle(rot, want);
        float turn = turnRate * SpeedFactorTurn();
        rot += Mathf.Clamp(diff, -turn * dt, turn * dt);
        float align = Mathf.Clamp01(1f - Mathf.Abs(diff) / (pivotTurn ? 110f : 60f));
        float terr = World.I.SpeedMul(move, pos);
        float tgt = maxSpeed * SpeedFactor() * terr * align;
        if (last) tgt = Mathf.Min(tgt, Mathf.Max(1.2f, dist * 1.5f));
        speed = G.Approach(speed, tgt, maxSpeed * 1.2f * dt + 0.5f * dt);
        return true;
    }
    protected virtual float SpeedFactorTurn() { return 1f; }

    protected void ApplyMove(float dt)
    {
        if (speed < 0.01f) return;
        Vector2 dir = G.Dir(rot);
        Vector2 np = pos + dir * speed * dt;
        if (Passable(np)) { distTravel += speed * dt; pos = np; }
        else if (Passable(new Vector2(np.x, pos.y))) { pos = new Vector2(np.x, pos.y); distTravel += speed * dt * 0.5f; speed *= 0.9f; }
        else if (Passable(new Vector2(pos.x, np.y))) { pos = new Vector2(pos.x, np.y); distTravel += speed * dt * 0.5f; speed *= 0.9f; }
        else { speed *= 0.5f; blockT += dt; }
        vel = dir * speed;
    }

    // soft collision with other ground units and solid props
    protected void Separate(float dt, bool crush)
    {
        qbuf.Clear();
        World.I.Query(pos, radius + 3f, qbuf, false);
        for (int i = 0; i < qbuf.Count; i++)
        {
            Ent o = qbuf[i];
            if (o == this || o.hidden || o.removed || o.Airborne || o.IsAirKind && o.alt > 0.1f) continue;
            if (o.kind == Kind.Soldier && kind != Kind.Soldier && o.dead) continue;
            if ((kind == Kind.Ship) != (o.kind == Kind.Ship)) continue;
            if (o.dead && o.kind == Kind.Soldier) continue;
            float min = radius + o.radius;
            Vector2 dv = pos - o.pos; float dd = dv.magnitude;
            if (dd >= min) continue;
            if (dd < 0.001f) { dv = UnityEngine.Random.insideUnitCircle; dd = 0.1f; }
            float wa = Mass, wb = o.Mass;
            float share = wb / (wa + wb);
            Vector2 np = pos + dv / dd * (min - dd) * share * Mathf.Min(1f, dt * 12f + 0.15f);
            if (Passable(np)) pos = np;
        }
        if (move == Move.Wheel || move == Move.Track || move == Move.Foot)
        {
            qbuf.Clear();
            World.I.QueryProps(pos, radius + 6f, qbuf);
            for (int i = 0; i < qbuf.Count; i++)
            {
                Prop p = qbuf[i] as Prop;
                if (p == null || !p.Solid) continue;
                if (crush && p.def.special == "tree" && speed > 0.8f && Mass > 8f) { p.Crush(this); continue; }
                if (p.def.special == "tree" && move != Move.Foot && !crush) { continue; }
                if (p.def.special == "tree" && move == Move.Foot) continue;
                Vector2 pushed;
                if (p.PushOut(pos, radius, out pushed)) { if (Passable(pushed)) pos = pushed; speed *= 0.85f; blockT += dt * 0.5f; }
            }
        }
    }

    // ------------------------------------------------------------ AI
    public bool IsEnemyValid(Ent e)
    {
        return e != null && !e.removed && !e.dead && !e.hidden && IsEnemyOf(e) && e.Targetable;
    }
    protected float DetectR() { return def.detect * World.I.Visibility * (Game.I.aiOpt.nightVision ? 1f : 1f); }
    protected float MaxRange()
    {
        float m = 0; foreach (var w in weps) m = Mathf.Max(m, w.d.range);
        return m;
    }

    protected virtual void Think()
    {
        tgtG = null; tgtA = null;
        float maxR = Mathf.Min(MaxRange(), 280f);
        float dr = DetectR() * (Spotter() ? 1.25f : 1f);
        float qr = Mathf.Max(dr, Mathf.Min(maxR, 250f));
        if (qr < 5f) qr = dr;
        qbuf.Clear();
        World.I.Query(pos, qr, qbuf, false);
        float bestG = 1e9f, bestA = 1e9f;
        Faction mine = fac;
        for (int i = 0; i < qbuf.Count; i++)
        {
            Ent e = qbuf[i];
            if (!IsEnemyValid(e)) continue;
            float d = (e.pos - pos).magnitude;
            bool seen = d <= dr * (e.kind == Kind.Soldier ? 0.85f : 1f) * (e.IsAirKind ? 1.1f : 1f);
            if (seen) e.SpotBy(mine);
            else if (!e.SpottedBy(mine)) continue;
            if (e.Airborne) { if (d < bestA) { bestA = d; tgtA = e; } }
            else
            {
                float score = d - (e is Unit && ((Unit)e).weps.Count > 0 ? 3f : 0f);
                if (score < bestG) { bestG = score; tgtG = e; }
            }
        }
        // forced attack target
        if (ord == OrderT.Attack && IsEnemyValid(ordTarget)) { if (ordTarget.Airborne) tgtA = ordTarget; else tgtG = ordTarget; }
        // assign to weapons
        for (int i = 0; i < weps.Count; i++)
        {
            WS s = weps[i]; s.tgt = null;
            if (Game.I.aiOpt.ceasefire || stance == Stance.Passive) continue;
            if (!Game.I.aiOpt.autoEngage && !Game.I.manualFire && ord != OrderT.Attack) continue;
            Ent t = null;
            if (s.d.air && tgtA != null && InRange(s, tgtA) && WeaponCanTarget(s, tgtA)) t = tgtA;
            else if (s.d.ground && tgtG != null && InRange(s, tgtG) && WeaponCanTarget(s, tgtG)) t = tgtG;
            s.tgt = t;
        }
    }
    protected virtual bool Spotter() { return false; }
    protected virtual bool WeaponCanTarget(WS s, Ent t) { return true; }
    protected bool InRange(WS s, Ent t)
    {
        float d = (t.pos - pos).magnitude;
        return d <= s.d.range && d >= s.d.minR;
    }

    protected virtual void AutoBehavior(float dt)
    {
        if (dead) return;
        var ai = Game.I.aiOpt;
        // retreat when critically damaged
        if (ai.retreat && !retreating && hp < maxHp * 0.25f && ord != OrderT.Hold && move != Move.None && Stage < 5 && CanMove())
        {
            retreating = true; OrderMove(home); ord = OrderT.Return;
        }
        if (retreating && ord == OrderT.None) retreating = false;
        // following
        if (ord == OrderT.Follow)
        {
            if (ordTarget == null || ordTarget.removed || ordTarget.dead) { ord = OrderT.None; }
            else
            {
                float d = (ordTarget.pos - pos).magnitude;
                repathT -= dt;
                if (d > 7f && (repathT <= 0f || path == null)) { SetPath(ordTarget.pos + (pos - ordTarget.pos).normalized * 4f); repathT = 1.5f; }
                else if (d < 5f) { path = null; }
            }
        }
        else if (ord == OrderT.Attack)
        {
            if (!IsEnemyValid(ordTarget)) { ord = OrderT.None; ordTarget = null; }
            else
            {
                float d = (ordTarget.pos - pos).magnitude;
                float rng = Mathf.Max(5f, MaxRange() * 0.8f);
                repathT -= dt;
                if (d > rng && move != Move.None && (repathT <= 0f || path == null)) { SetPath(ordTarget.pos); repathT = 2f; }
                else if (d <= rng * 0.9f) path = null;
            }
        }
        else if ((ord == OrderT.None || ord == OrderT.Hold) && stance == Stance.Aggressive && ai.chase && move != Move.None && ord != OrderT.Hold && CanMove())
        {
            // advance toward nearest known enemy outside weapon range
            Ent adv = tgtG != null ? tgtG : tgtA;
            if (adv != null && weps.Count > 0)
            {
                float d = (adv.pos - pos).magnitude;
                float want = Mathf.Max(6f, MaxRange() * 0.7f);
                if (d > want && !(MaxRange() > 120f)) { repathT -= dt; if (repathT <= 0f) { SetPath(adv.pos - (adv.pos - pos).normalized * want * 0.8f); repathT = 2.5f; autoAdvance = true; } }
            }
            else if (ai.wander && (path == null || pi >= path.Count) && G.R01() < dt * 0.1f && move != Move.Air)
            {
                Vector2 q = home + UnityEngine.Random.insideUnitCircle * 25f; if (Passable(q)) { SetPath(q); autoAdvance = true; }
            }
        }
        if (ord == OrderT.Hold) path = null;
    }
    protected bool autoAdvance;
    protected virtual bool CanMove() { return true; }

    // ------------------------------------------------------------ weapons
    protected virtual Vector2 MuzzlePos(WS s) { return pos + G.Dir(AimAngle(s)) * s.d.muzzle; }
    protected virtual float AimAngle(WS s) { return rot; }
    protected virtual bool CanFire() { return !dead; }
    protected virtual float SpreadMul() { return 1f; }

    protected void UpdateWeapons(float dt)
    {
        recoil = Mathf.Max(0f, recoil - dt * 4f);
        for (int i = 0; i < weps.Count; i++)
        {
            WS s = weps[i];
            s.cd -= dt;
            if (s.burst > 0)
            {
                s.burstT -= dt;
                if (s.burstT <= 0f && CanFire())
                {
                    s.burst--; s.burstT = s.d.gap;
                    Ent t = s.tgt;
                    if (s.d.t == WT.Salvo || s.d.t == WT.Flame || s.d.t == WT.Bomb) FireOne(s, t, true);
                    else if (t != null && !t.removed && !t.dead) FireOne(s, t, true);
                    else s.burst = 0;
                }
                continue;
            }
            if (s.tgt == null || s.cd > 0f || !CanFire()) continue;
            if (s.tgt.removed || s.tgt.dead) { s.tgt = null; continue; }
            if (s.ammo == 0) continue;
            if (!Aligned(s, s.tgt)) continue;
            // start a burst
            s.cd = s.d.cd * G.Rnd(0.9f, 1.15f);
            s.burst = Mathf.Max(0, s.d.burst - 1); s.burstT = s.d.gap;
            FireOne(s, s.tgt, false);
        }
    }
    protected virtual bool Aligned(WS s, Ent t)
    {
        float want = G.Ang(t.pos - pos);
        return Mathf.Abs(Mathf.DeltaAngle(AimAngle(s), want)) < 9f;
    }

    protected virtual void FireOne(WS s, Ent t, bool cont)
    {
        if (s.ammo == 0) { s.burst = 0; return; }
        Vector2 m = MuzzlePos(s);
        Vector2 aim;
        float dist;
        if (t != null)
        {
            Vector2 tp = t.pos;
            float ps = Mathf.Max(20f, s.d.spd);
            dist = (tp - m).magnitude;
            if (s.d.t != WT.Salvo || true) tp += t.vel * (dist / ps) * (s.d.t == WT.Bomb ? 0f : 1f);
            aim = tp;
        }
        else { aim = m + G.Dir(AimAngle(s)) * s.d.range * 0.7f; dist = (aim - m).magnitude; }
        Combat.FireWeapon(this, s, m, aim, t);
        if (s.ammo > 0) s.ammo--;
        recoil = 1f;
        OnFired(s);
    }
    protected virtual void OnFired(WS s) { }

    protected float SpreadFor(WS s) { return s.d.spread * SpreadMul(); }

    // ------------------------------------------------------------ loading
    public virtual bool CanCarry(Soldier s) { return cap > 0 && cargo.Count < cap && !dead && s.fac == fac; }
    public virtual bool Load(Soldier s)
    {
        if (!CanCarry(s)) return false;
        cargo.Add(s); s.EnterVehicle(this);
        return true;
    }
    protected float unloadTimer; public bool unloading;
    public virtual void UnloadAll() { if (cargo.Count > 0) unloading = true; }
    protected virtual Vector2 ExitPoint(int idx)
    {
        float a = rot + 90f + (idx % 2 == 0 ? 1 : -1) * (idx / 2) * 25f;
        return pos + G.Dir(a + (idx % 2 == 0 ? 0 : 180f)) * (radius + 0.8f + (idx / 4) * 0.7f);
    }
    protected void UpdateUnload(float dt)
    {
        if (!unloading) return;
        if (cargo.Count == 0) { unloading = false; return; }
        if (!CanUnloadNow()) return;
        unloadTimer -= dt;
        if (unloadTimer > 0f) return;
        unloadTimer = 0.28f;
        Soldier s = cargo[cargo.Count - 1]; cargo.RemoveAt(cargo.Count - 1);
        Vector2 ex = ExitPoint(cargo.Count);
        if (!World.I.InBounds(ex) || (World.I.Water(ex) && kind != Kind.Ship && !IsAirKind)) ex = pos + UnityEngine.Random.insideUnitCircle.normalized * (radius + 1f);
        s.ExitVehicle(this, ex);
    }
    protected virtual bool CanUnloadNow() { return speed < 1.5f; }
    public void EvacuateAll(bool wounded)
    {
        for (int i = cargo.Count - 1; i >= 0; i--)
        {
            Soldier s = cargo[i]; cargo.RemoveAt(i);
            Vector2 ex = pos + UnityEngine.Random.insideUnitCircle.normalized * (radius + 0.8f);
            if (!World.I.InBounds(ex) || (World.I.Water(ex) && kind != Kind.Ship)) ex = pos;
            s.ExitVehicle(this, ex);
            if (wounded) s.hp = Mathf.Max(5f, s.hp * G.Rnd(0.3f, 0.8f));
        }
        unloading = false;
    }
    public override void OnRemove()
    {
        for (int i = 0; i < cargo.Count; i++) { cargo[i].removed = true; if (cargo[i].go) UnityEngine.Object.Destroy(cargo[i].go); }
        cargo.Clear();
    }

    // supply/repair/heal aura processing
    protected void ProcessAura(float dt)
    {
        if (def.aura == Aura.None || dead) return;
        auraT -= dt; if (auraT > 0f) return; auraT = 1f;
        qbuf.Clear();
        float r = def.aura == Aura.Comms ? 0f : 11f;
        if (r <= 0f) return;
        World.I.Query(pos, r, qbuf, false);
        for (int i = 0; i < qbuf.Count; i++)
        {
            Ent e = qbuf[i]; Unit u = e as Unit;
            if (u == null || e == this || e.dead || e.fac != fac || e.hidden) continue;
            switch (def.aura)
            {
                case Aura.Heal: if (e.kind == Kind.Soldier && e.hp < e.maxHp) { e.hp = Mathf.Min(e.maxHp, e.hp + 7f); Fx.Heal(e.pos); } break;
                case Aura.Repair: if (e.kind != Kind.Soldier && e.kind != Kind.Prop && e.hp < e.maxHp && e.Stage < 5) { u.Repair(def.id == "eng_brem" ? 14f : 8f); } break;
                case Aura.Ammo: foreach (var w in u.weps) if (w.d.ammo > 0 && w.ammo < w.d.ammo) { w.ammo = Mathf.Min(w.d.ammo, w.ammo + Mathf.Max(1, w.d.ammo / 8)); } break;
                case Aura.Fuel: u.fuel = Mathf.Min(100f, u.fuel + 18f); break;
            }
        }
    }
    public virtual void Repair(float amt) { if (dead) return; hp = Mathf.Min(maxHp, hp + amt); Fx.Spark(pos + UnityEngine.Random.insideUnitCircle * radius * 0.5f, Vector2.up, 3); }
    public virtual void LaunchFlares() { }
    public virtual void CmdUnload() { UnloadAll(); }
}
