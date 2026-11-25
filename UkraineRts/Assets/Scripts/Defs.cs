using System;
using System.Collections.Generic;
using UnityEngine;

public enum Faction { Ukraine = 0, Russia = 1, Neutral = 2 }
public enum Cat
{
    Infantry, Tanks, IFV, APC, Armored, Trucks, Cars, Artillery, MLRS, AirDefense,
    Aircraft, Helicopters, UAV, Ships, Airborne, Engineering, Infrastructure, Other
}
public enum Kind { Soldier, Ground, Plane, Heli, Drone, Ship, Prop }
public enum Move { Foot, Wheel, Track, Air, Naval, None }
public enum Role { Rifle, MG, AT, Sniper, Mech, Moto, Para, Naval, Recon, Engineer, Medic, Operator, Crew, Pilot, AA }
public enum Aura { None, Ammo, Repair, Heal, Fuel, Comms, Dozer, Bridge }
public enum WT { Rifle, MG, Sniper, Cannon, Auto, Arty, Mortar, Rocket, Salvo, Missile, AAGun, SAM, Bomb, Flame, Gun }
public enum Stance { Aggressive, Defensive, Hold, Passive }

public class W
{
    public WT t;
    public float dmg = 10, pen = 1, range = 30, cd = 1, spd = 80, aoe = 0.5f, spread = 3, gap = 0.1f, minR = 0;
    public int burst = 1, ammo = -1, salvo = 1, mount = 0; // mount 0 turret, 1 hull-fixed, 2 free
    public bool air = false, ground = true, homing = false;
    public float muzzle = 1.5f;      // muzzle distance from mount centre
    public string name = "";
}

public class VStyle
{
    public float L = 4.4f, W = 2f, trackW = 0.45f, tx = 0, tl = 1.8f, tw = 1.4f, gl = 2.5f, gt = 0.15f, cabL = 1.3f;
    public bool tracked, era, skirts, cage, bustle, ramp, dozer;
    public int rw = 6, hull, tur, gun, cab, bed, ax;
}

public class UnitDef
{
    public string id, name, desc = "";
    public Cat cat;
    public Kind kind = Kind.Ground;
    public Move move = Move.Wheel;
    public int mask = 3;                // 1 = Ukraine, 2 = Russia
    public string fam = "wheeled";      // tracked wheeled soldier plane heli drone ship prop
    public VStyle vs;
    public int style;                   // style id for air / ship / prop / soldier
    public float hp = 100, armor = 1, speed = 8, turn = 90, radius = 1, detect = 60;
    public int crew, cap;
    public float len = 4, wid = 2;
    public W[] w = new W[0];
    public Role role = Role.Rifle;
    public Aura aura = Aura.None;
    public int alt = 1;                 // default altitude layer (0 ground, 1 low, 2 high)
    public bool flares, eject, solid, flammable, explosive, crush, towed, turretless;
    public float sizeScale = 1f;
    public string special = "";
    public string kit = "";             // operators: "quad:2,fpv:3"
    public string loadId = ""; public int loadN;
    public float signal = 0, battery = 0, hardness = 1;
    public Sprite iconSpr;
}

public class Brigade
{
    public int idx; public Faction fac; public string name, nick, note;
    public Color main, sec, accent, helmet;
    public int emblem, camo;
    public Cat[] cats;
    public bool Has(Cat c) { return cats == null || Array.IndexOf(cats, c) >= 0; }
}

public static class Fac
{
    public static Color Color(Faction f)
    {
        if (f == Faction.Ukraine) return G.Hex(0x2f86e0);
        if (f == Faction.Russia) return G.Hex(0xe0463a);
        return G.Hex(0xb0b0b0);
    }
    public static string Name(Faction f) { return f == Faction.Ukraine ? "Ukraine" : f == Faction.Russia ? "Russia" : "Neutral"; }
    public static Faction Enemy(Faction f) { return f == Faction.Ukraine ? Faction.Russia : f == Faction.Russia ? Faction.Ukraine : Faction.Neutral; }
}

public static class Wp
{
    public static W Rifle() { return new W { t = WT.Rifle, name = "Assault rifle", dmg = 9, pen = 0.6f, range = 26, cd = 1.4f, burst = 3, gap = 0.1f, spd = 95, aoe = 0.5f, spread = 3.5f, mount = 2, muzzle = 0.7f }; }
    public static W LMG() { return new W { t = WT.MG, name = "Machine gun", dmg = 8, pen = 0.7f, range = 32, cd = 1.6f, burst = 10, gap = 0.07f, spd = 100, aoe = 0.5f, spread = 4.5f, mount = 2, muzzle = 0.8f }; }
    public static W Sniper() { return new W { t = WT.Sniper, name = "Marksman rifle", dmg = 60, pen = 1.6f, range = 55, cd = 3.2f, spd = 160, aoe = 0.4f, spread = 0.6f, mount = 2, muzzle = 0.9f }; }
    public static W Pistol() { return new W { t = WT.Rifle, name = "Sidearm", dmg = 6, pen = 0.4f, range = 14, cd = 1.2f, burst = 2, gap = 0.2f, spd = 70, aoe = 0.4f, spread = 6, mount = 2, muzzle = 0.5f }; }
    public static W RPG() { return new W { t = WT.Rocket, name = "AT rocket", dmg = 130, pen = 8, range = 38, cd = 6, spd = 32, aoe = 2f, spread = 1.5f, ammo = 3, mount = 2, muzzle = 0.8f }; }
    public static W Manpads() { return new W { t = WT.SAM, name = "MANPADS", dmg = 100, pen = 5, range = 75, cd = 8, spd = 42, aoe = 3.5f, spread = 0, ammo = 3, mount = 2, air = true, ground = false, homing = true, muzzle = 0.8f }; }
    public static W Coax() { return new W { t = WT.MG, name = "Coax MG", dmg = 7, pen = 0.7f, range = 30, cd = 1.2f, burst = 8, gap = 0.06f, spd = 100, aoe = 0.5f, spread = 3, mount = 0, muzzle = 1.4f }; }
    public static W HMG() { return new W { t = WT.MG, name = "Heavy MG", dmg = 14, pen = 1.6f, range = 40, cd = 1.2f, burst = 7, gap = 0.09f, spd = 105, aoe = 0.6f, spread = 3, mount = 0, muzzle = 1.2f, air = true }; }
    public static W Auto(float dmg = 18, float range = 42) { return new W { t = WT.Auto, name = "Autocannon", dmg = dmg, pen = 3.2f, range = range, cd = 1.3f, burst = 6, gap = 0.1f, spd = 90, aoe = 0.8f, spread = 2.2f, mount = 0, muzzle = 1.8f }; }
    public static W Cannon(float dmg = 110, float range = 55, string name = "Tank gun") { return new W { t = WT.Cannon, name = name, dmg = dmg, pen = 9, range = range, cd = 4.5f, spd = 75, aoe = 2.2f, spread = 1.1f, ammo = 40, mount = 0, muzzle = 2.8f }; }
    public static W ATGM(float range = 60) { return new W { t = WT.Missile, name = "ATGM", dmg = 210, pen = 12, range = range, cd = 9, spd = 34, aoe = 2.4f, spread = 0, ammo = 6, mount = 0, homing = true, muzzle = 1f }; }
    public static W Howitzer(float dmg = 170, float range = 190, float cd = 5f) { return new W { t = WT.Arty, name = "Howitzer", dmg = dmg, pen = 6, range = range, cd = cd, spd = 48, aoe = 7f, spread = 6, ammo = 30, minR = 30, mount = 0, muzzle = 3.2f }; }
    public static W Mortar(float dmg = 90, float range = 75) { return new W { t = WT.Mortar, name = "Mortar", dmg = dmg, pen = 3, range = range, cd = 3f, spd = 30, aoe = 4.5f, spread = 5, ammo = 40, minR = 12, mount = 0, muzzle = 1f }; }
    public static W Rockets(int n = 12, float range = 180, float dmg = 65) { return new W { t = WT.Salvo, name = "Rocket salvo", dmg = dmg, pen = 4, range = range, cd = 18, gap = 0.12f, spd = 55, aoe = 5f, spread = 10, ammo = n * 3, salvo = n, burst = n, minR = 30, mount = 0, muzzle = 1.5f }; }
    public static W Thermo() { return new W { t = WT.Flame, name = "Thermobaric rockets", dmg = 230, pen = 4, range = 62, cd = 14, gap = 0.2f, spd = 30, aoe = 8.5f, spread = 5, ammo = 24, salvo = 4, burst = 4, minR = 8, mount = 0, muzzle = 1.5f }; }
    public static W SAM(float range = 110, float dmg = 160) { return new W { t = WT.SAM, name = "SAM", dmg = dmg, pen = 6, range = range, cd = 4.5f, spd = 48, aoe = 6f, spread = 0, ammo = 8, mount = 0, air = true, ground = false, homing = true, muzzle = 1.5f }; }
    public static W AAGun(float range = 46) { return new W { t = WT.AAGun, name = "AA gun", dmg = 14, pen = 2f, range = range, cd = 0.9f, burst = 9, gap = 0.05f, spd = 110, aoe = 1.6f, spread = 2.6f, mount = 0, muzzle = 1.6f, air = true, ground = true }; }
    // aircraft
    public static W AirGun() { return new W { t = WT.Auto, name = "Cannon", dmg = 14, pen = 3, range = 40, cd = 1.6f, burst = 14, gap = 0.04f, spd = 110, aoe = 0.8f, spread = 2.8f, mount = 1, muzzle = 2.2f, air = true, ground = true }; }
    public static W AAM(float range = 105) { return new W { t = WT.Missile, name = "Air-to-air missile", dmg = 140, pen = 6, range = range, cd = 6, spd = 60, aoe = 5f, ammo = 4, mount = 1, air = true, ground = false, homing = true, muzzle = 2f }; }
    public static W AGRockets(int n = 12) { return new W { t = WT.Salvo, name = "Rocket pods", dmg = 45, pen = 4, range = 55, cd = 7, gap = 0.07f, spd = 70, aoe = 3f, spread = 5, ammo = 36, salvo = n, burst = n, mount = 1, muzzle = 2f }; }
    public static W Bombs(int n = 6, float dmg = 260) { return new W { t = WT.Bomb, name = "Bombs", dmg = dmg, pen = 7, range = 6, cd = 0.5f, spd = 0, aoe = 9f, spread = 3, ammo = n, mount = 1, muzzle = 0 }; }
    public static W AGMissile() { return new W { t = WT.Missile, name = "Air-to-ground missile", dmg = 210, pen = 12, range = 85, cd = 7, spd = 50, aoe = 3f, ammo = 4, mount = 1, homing = true, muzzle = 2f }; }
    // ships
    public static W NavalGun(float dmg = 105, float range = 120) { return new W { t = WT.Arty, name = "Naval gun", dmg = dmg, pen = 6, range = range, cd = 2.6f, spd = 70, aoe = 4f, spread = 3, mount = 0, muzzle = 2.6f, minR = 10, ammo = -1, air = false }; }
    public static W ShipMissile(float range = 230) { return new W { t = WT.Missile, name = "Cruise missile", dmg = 340, pen = 10, range = range, cd = 14, spd = 40, aoe = 7f, ammo = 8, mount = 0, homing = true, muzzle = 2f, minR = 25 }; }
    public static W CIWS() { return new W { t = WT.AAGun, name = "CIWS", dmg = 12, pen = 2, range = 40, cd = 0.7f, burst = 18, gap = 0.035f, spd = 120, aoe = 1.5f, spread = 2.2f, mount = 0, muzzle = 1.5f, air = true, ground = true }; }
    public static W ShipMG() { return new W { t = WT.MG, name = "Deck MG", dmg = 12, pen = 1.4f, range = 36, cd = 1.2f, burst = 8, gap = 0.07f, spd = 100, aoe = 0.6f, spread = 3.5f, mount = 0, muzzle = 1.2f, air = true }; }
}

public static class Db
{
    public static List<UnitDef> All = new List<UnitDef>();
    public static Dictionary<string, UnitDef> ById = new Dictionary<string, UnitDef>();
    public static Brigade[][] Brigades = new Brigade[2][];

    public static string CatName(Cat c)
    {
        switch (c)
        {
            case Cat.Infantry: return "Infantry";
            case Cat.Tanks: return "Tanks";
            case Cat.IFV: return "IFVs";
            case Cat.APC: return "APCs";
            case Cat.Armored: return "Armored Vehicles";
            case Cat.Trucks: return "Military Trucks";
            case Cat.Cars: return "Military Cars";
            case Cat.Artillery: return "Artillery";
            case Cat.MLRS: return "Rocket Launchers";
            case Cat.AirDefense: return "Air Defense";
            case Cat.Aircraft: return "Aircraft";
            case Cat.Helicopters: return "Helicopters";
            case Cat.UAV: return "UAVs / Drones";
            case Cat.Ships: return "Military Ships";
            case Cat.Airborne: return "Airborne Units";
            case Cat.Engineering: return "Engineering";
            case Cat.Infrastructure: return "Infrastructure";
            default: return "Other Objects";
        }
    }

    public static List<UnitDef> For(Cat c, Faction f)
    {
        var l = new List<UnitDef>();
        int m = f == Faction.Ukraine ? 1 : 2;
        foreach (var d in All) if (d.cat == c && (d.mask & m) != 0) l.Add(d);
        return l;
    }

    static VStyle S(float L = 4.4f, float W = 2f, bool tracked = false, int rw = 6, int hull = 0, int tur = 0, float tx = 0, float tl = 1.8f, float tw = 1.4f,
        int gun = 0, float gl = 2.5f, float gt = 0.15f, bool era = false, bool skirts = false, bool cage = false, bool bustle = false, bool ramp = false,
        bool dozer = false, int cab = 0, int bed = 0, float cabL = 1.3f, int ax = 0, float trackW = 0.45f)
    {
        return new VStyle { L = L, W = W, tracked = tracked, rw = rw, hull = hull, tur = tur, tx = tx, tl = tl, tw = tw, gun = gun, gl = gl, gt = gt, era = era, skirts = skirts, cage = cage, bustle = bustle, ramp = ramp, dozer = dozer, cab = cab, bed = bed, cabL = cabL, ax = ax, trackW = trackW };
    }

    static UnitDef Add(UnitDef d)
    {
        All.Add(d); ById[d.id] = d;
        if (d.vs != null) { d.len = d.vs.L; d.wid = d.vs.W; }
        if (d.radius <= 1f) d.radius = Mathf.Max(0.35f, Mathf.Min(d.len, Mathf.Max(d.wid, d.len * 0.5f)) * 0.5f);
        return d;
    }

    static UnitDef Veh(string id, string name, Cat cat, int mask, VStyle vs, float hp, float armor, float speed, int crew, int cap, params W[] w)
    {
        var d = new UnitDef { id = id, name = name, cat = cat, mask = mask, vs = vs, hp = hp, armor = armor, speed = speed, crew = crew, cap = cap, w = w };
        d.kind = Kind.Ground;
        d.fam = vs.tracked ? "tracked" : "wheeled";
        d.move = vs.tracked ? Move.Track : Move.Wheel;
        d.turn = vs.tracked ? 75 : 85;
        d.detect = 65;
        d.crush = vs.tracked && hp > 250;
        return Add(d);
    }

    static UnitDef Soldier(string id, string name, Role role, int mask, float hp, params W[] w)
    {
        var d = new UnitDef { id = id, name = name, cat = Cat.Infantry, mask = mask, kind = Kind.Soldier, move = Move.Foot, fam = "soldier", role = role, hp = hp, armor = 0.5f, speed = 2.4f, turn = 360, radius = 0.4f, w = w, detect = 60, len = 0.9f, wid = 0.9f };
        return Add(d);
    }

    static UnitDef Air(string id, string name, Cat cat, int mask, Kind kind, int style, float len, float span, float hp, float armor, float speed, float turn, int alt, params W[] w)
    {
        var d = new UnitDef { id = id, name = name, cat = cat, mask = mask, kind = kind, style = style, len = len, wid = span, hp = hp, armor = armor, speed = speed, turn = turn, alt = alt, w = w };
        d.move = Move.Air; d.fam = kind == Kind.Plane ? "plane" : kind == Kind.Heli ? "heli" : "drone";
        d.radius = Mathf.Max(len, span) * 0.35f; d.detect = 80; d.flares = kind != Kind.Drone; d.eject = kind == Kind.Plane;
        return Add(d);
    }

    static UnitDef Ship(string id, string name, int mask, int style, float len, float wid, float hp, float armor, float speed, int cap, params W[] w)
    {
        var d = new UnitDef { id = id, name = name, cat = Cat.Ships, mask = mask, kind = Kind.Ship, move = Move.Naval, fam = "ship", style = style, len = len, wid = wid, hp = hp, armor = armor, speed = speed, turn = 22 + 120f / len, cap = cap, w = w, radius = wid * 0.5f + 0.5f, detect = 90, crew = 8 + (int)(len) };
        return Add(d);
    }

    static UnitDef Prop(string id, string name, Cat cat, int style, float len, float wid, float hp, bool solid, params W[] w)
    {
        var d = new UnitDef { id = id, name = name, cat = cat, mask = 3, kind = Kind.Prop, move = Move.None, fam = "prop", style = style, len = len, wid = wid, hp = hp, armor = 2, solid = solid, w = w };
        d.radius = Mathf.Max(len, wid) * 0.5f; d.flammable = true;
        return Add(d);
    }

    static bool built;
    public static void Build()
    {
        if (built) return; built = true;
        BuildBrigades();
        BuildInfantry(); BuildTanks(); BuildIFVAPC(); BuildWheeled(); BuildArtillery(); BuildAirDefense();
        BuildAir(); BuildUAV(); BuildShips(); BuildAirborne(); BuildEngineering(); BuildProps();
    }

    // ------------------------------------------------------------------ brigades
    static Brigade B(int idx, Faction f, string name, string nick, int main, int sec, int accent, int helmet, int emblem, int camo, string note, params Cat[] cats)
    {
        return new Brigade { idx = idx, fac = f, name = name, nick = nick, main = G.Hex(main), sec = G.Hex(sec), accent = G.Hex(accent), helmet = G.Hex(helmet), emblem = emblem, camo = camo, note = note, cats = cats.Length == 0 ? null : cats };
    }
    static void BuildBrigades()
    {
        Brigades[0] = new Brigade[] {
            B(0, Faction.Ukraine, "Unassigned Forces", "General", 0x5d6e3e, 0x46532e, 0xffd500, 0x4b5a35, 0, 0, "All equipment"),
            B(1, Faction.Ukraine, "1st Armored Brigade", "Steel Wolves", 0x4d5b34, 0x2f3a24, 0xe8c200, 0x3b4a2a, 2, 1, "Tanks & IFV spearhead", Cat.Infantry, Cat.Tanks, Cat.IFV, Cat.APC, Cat.Armored, Cat.Trucks, Cat.Cars, Cat.Engineering, Cat.Infrastructure, Cat.Other, Cat.UAV, Cat.Artillery),
            B(1, Faction.Ukraine, "25th Airborne Brigade", "Sky Hawks", 0x76734a, 0x544f32, 0x8c2433, 0x6a2330, 3, 3, "Paratroopers & air assault", Cat.Infantry, Cat.Airborne, Cat.Aircraft, Cat.Helicopters, Cat.APC, Cat.Armored, Cat.Cars, Cat.Trucks, Cat.UAV, Cat.AirDefense, Cat.Infrastructure, Cat.Other),
            B(2, Faction.Ukraine, "36th Marine Brigade", "Black Tridents", 0x2b3341, 0x181d27, 0x2a8bd8, 0x1f2631, 4, 2, "Naval infantry & coastal forces", Cat.Infantry, Cat.APC, Cat.Armored, Cat.Ships, Cat.Airborne, Cat.Trucks, Cat.Cars, Cat.Artillery, Cat.AirDefense, Cat.UAV, Cat.Infrastructure, Cat.Other),
            B(3, Faction.Ukraine, "47th Mechanized Brigade", "Iron Bison", 0x56633a, 0x3a4429, 0xdadada, 0x3d4a2c, 5, 3, "Mechanized combined arms", Cat.Infantry, Cat.Tanks, Cat.IFV, Cat.APC, Cat.Artillery, Cat.MLRS, Cat.AirDefense, Cat.Trucks, Cat.Armored, Cat.Engineering, Cat.Cars, Cat.Infrastructure, Cat.Other),
            B(4, Faction.Ukraine, "92nd Assault Brigade", "Thunder", 0x8c7b52, 0x5e5236, 0xf2c200, 0x6d5f3d, 6, 1, "Assault infantry & fire support", Cat.Infantry, Cat.Tanks, Cat.IFV, Cat.APC, Cat.Armored, Cat.UAV, Cat.Cars, Cat.Trucks, Cat.Artillery, Cat.Infrastructure, Cat.Other),
            B(5, Faction.Ukraine, "3rd Reconnaissance Regiment", "Night Owls", 0x3a4538, 0x232a22, 0x9fb4c8, 0x2a3328, 10, 4, "Recon, drones & helicopters", Cat.Infantry, Cat.UAV, Cat.Cars, Cat.Armored, Cat.Helicopters, Cat.Other, Cat.Infrastructure),
        };
        Brigades[1] = new Brigade[] {
            B(0, Faction.Russia, "Unassigned Forces", "General", 0x6a7048, 0x4d5334, 0xd02a2a, 0x555b3b, 1, 0, "All equipment"),
            B(1, Faction.Russia, "4th Guards Tank Division", "Bear", 0x5b6440, 0x3d4529, 0xc0282d, 0x474f31, 7, 1, "Tank heavy formation", Cat.Infantry, Cat.Tanks, Cat.IFV, Cat.APC, Cat.Armored, Cat.Trucks, Cat.Cars, Cat.Engineering, Cat.MLRS, Cat.Artillery, Cat.AirDefense, Cat.Infrastructure, Cat.Other, Cat.UAV),
            B(2, Faction.Russia, "76th Air Assault Division", "Falcon", 0x5f6c4a, 0x3d4a30, 0x4fa3e0, 0x4fa3e0, 8, 3, "VDV paratroopers", Cat.Infantry, Cat.Airborne, Cat.Aircraft, Cat.Helicopters, Cat.IFV, Cat.APC, Cat.Armored, Cat.Cars, Cat.Trucks, Cat.UAV, Cat.AirDefense, Cat.Infrastructure, Cat.Other),
            B(3, Faction.Russia, "155th Naval Infantry Brigade", "Black Sea", 0x232a33, 0x12161c, 0xe8e8e8, 0x1d232b, 13, 2, "Marines & Black Sea fleet", Cat.Infantry, Cat.APC, Cat.Armored, Cat.Ships, Cat.Airborne, Cat.Trucks, Cat.Cars, Cat.Artillery, Cat.AirDefense, Cat.UAV, Cat.Infrastructure, Cat.Other, Cat.Tanks),
            B(4, Faction.Russia, "1st Motor Rifle Brigade", "Taiga", 0x7c6c47, 0x54492e, 0xe0a000, 0x5d5033, 12, 3, "Motorized infantry", Cat.Infantry, Cat.IFV, Cat.APC, Cat.Trucks, Cat.Cars, Cat.Tanks, Cat.Artillery, Cat.MLRS, Cat.AirDefense, Cat.Engineering, Cat.Armored, Cat.Infrastructure, Cat.Other),
            B(5, Faction.Russia, "45th Reconnaissance Brigade", "Shadow", 0x4a4f43, 0x2b2f28, 0xa8a8a8, 0x363a30, 9, 4, "Spec-recon & drones", Cat.Infantry, Cat.UAV, Cat.Armored, Cat.Cars, Cat.Helicopters, Cat.Other, Cat.Infrastructure),
            B(6, Faction.Russia, "6th Combined Arms Army", "Red Banner", 0x4f5c36, 0x323d22, 0xc8161d, 0x3a4528, 11, 1, "Fire support & air power", Cat.Infantry, Cat.Artillery, Cat.MLRS, Cat.AirDefense, Cat.Tanks, Cat.Trucks, Cat.Engineering, Cat.Aircraft, Cat.Helicopters, Cat.Cars, Cat.Infrastructure, Cat.Other),
        };
        for (int f = 0; f < 2; f++) for (int i = 0; i < Brigades[f].Length; i++) Brigades[f][i].idx = i;
    }

    // ------------------------------------------------------------------ infantry
    static void BuildInfantry()
    {
        Soldier("inf_rifle", "Rifleman", Role.Rifle, 3, 100, Wp.Rifle());
        Soldier("inf_mg", "Machine Gunner", Role.MG, 3, 110, Wp.LMG());
        Soldier("inf_at", "Anti-Tank Soldier", Role.AT, 3, 100, Wp.Rifle(), Wp.RPG());
        Soldier("inf_sniper", "Marksman", Role.Sniper, 3, 90, Wp.Sniper());
        Soldier("inf_mech", "Mechanized Infantry", Role.Mech, 3, 115, Wp.Rifle());
        Soldier("inf_moto", "Motorized Infantry", Role.Moto, 3, 105, Wp.Rifle());
        Soldier("inf_para", "Paratrooper", Role.Para, 3, 110, Wp.Rifle());
        Soldier("inf_naval", "Naval Infantry", Role.Naval, 3, 115, Wp.Rifle());
        Soldier("inf_recon", "Reconnaissance Soldier", Role.Recon, 3, 100, Wp.Rifle()).detect = 95;
        Soldier("inf_eng", "Combat Engineer", Role.Engineer, 3, 100, Wp.Rifle()).aura = Aura.Repair;
        Soldier("inf_medic", "Combat Medic", Role.Medic, 3, 95, Wp.Pistol()).aura = Aura.Heal;
        var op = Soldier("inf_uav", "UAV Operator", Role.Operator, 3, 90, Wp.Pistol()); op.kit = "quad:2,fpv:3"; op.detect = 70;
        Soldier("inf_crew", "Vehicle Crewman", Role.Crew, 3, 90, Wp.Pistol());
        Soldier("inf_pilot", "Pilot", Role.Pilot, 3, 80, Wp.Pistol()).special = "hidden";
    }

    // ------------------------------------------------------------------ tanks
    static void BuildTanks()
    {
        Cat c = Cat.Tanks;
        var t = Veh("t_t64", "T-64BV Bulat", c, 1, S(4.5f, 2.1f, true, 6, 1, 1, 0.15f, 1.9f, 1.6f, 1, 2.8f, 0.15f, era: true), 480, 8.5f, 6.3f, 3, 0, Wp.Cannon(105, 55), Wp.Coax()); t.desc = "Reliable Soviet-era MBT, reactive armor blocks.";
        Veh("t_t72av", "T-72AV", c, 1, S(4.6f, 2.15f, true, 6, 1, 1, 0.1f, 1.8f, 1.5f, 1, 2.85f, 0.15f, era: true, cage: false), 470, 8.5f, 6.2f, 3, 0, Wp.Cannon(105), Wp.Coax());
        Veh("t_oplot", "Oplot-M", c, 1, S(4.7f, 2.2f, true, 6, 1, 2, 0.1f, 2.2f, 1.8f, 1, 2.9f, 0.16f, era: true, bustle: true), 540, 10, 6.4f, 3, 0, Wp.Cannon(115, 58), Wp.Coax());
        Veh("t_leo2", "Leopard 2A6", c, 1, S(5.0f, 2.3f, true, 7, 0, 2, 0.1f, 2.7f, 2.0f, 1, 3.4f, 0.17f, skirts: true, bustle: true), 600, 11, 6.8f, 4, 0, Wp.Cannon(125, 62), Wp.Coax(), Wp.HMG());
        Veh("t_abrams", "M1A1 Abrams", c, 1, S(4.9f, 2.35f, true, 7, 0, 4, 0.05f, 2.6f, 2.1f, 1, 3.1f, 0.17f, skirts: true, bustle: true), 590, 11.5f, 7.0f, 4, 0, Wp.Cannon(125, 60), Wp.Coax(), Wp.HMG());
        Veh("t_chall2", "Challenger 2", c, 1, S(4.8f, 2.4f, true, 7, 0, 4, 0.15f, 2.3f, 2.0f, 1, 3.2f, 0.17f, skirts: true, cage: true), 620, 12, 6.2f, 4, 0, Wp.Cannon(120, 58), Wp.Coax());
        Veh("t_t72b3", "T-72B3", c, 2, S(4.6f, 2.15f, true, 6, 1, 1, 0.1f, 1.9f, 1.55f, 1, 2.85f, 0.15f, era: true, cage: true), 500, 9.5f, 6.3f, 3, 0, Wp.Cannon(110, 56), Wp.Coax(), Wp.HMG());
        Veh("t_t80bvm", "T-80BVM", c, 2, S(4.8f, 2.2f, true, 6, 1, 1, 0.1f, 2.0f, 1.65f, 1, 2.9f, 0.16f, era: true, skirts: true), 520, 10, 7.0f, 3, 0, Wp.Cannon(115, 57), Wp.Coax(), Wp.HMG());
        Veh("t_t90m", "T-90M Proryv", c, 2, S(4.8f, 2.2f, true, 6, 1, 2, 0.1f, 2.2f, 1.75f, 1, 2.95f, 0.16f, era: true, cage: true, bustle: true), 560, 11, 6.5f, 3, 0, Wp.Cannon(120, 60), Wp.Coax(), Wp.HMG());
        var ar = Veh("t_armata", "T-14 Armata", c, 2, S(5.2f, 2.3f, true, 7, 0, 3, 0.2f, 2.6f, 2.0f, 1, 3.3f, 0.17f, skirts: true), 640, 13, 6.8f, 3, 0, Wp.Cannon(135, 65), Wp.Coax()); ar.desc = "Unmanned turret next-generation tank.";
        Veh("t_t62", "T-62M", c, 3, S(4.4f, 2.1f, true, 5, 1, 1, 0.1f, 1.7f, 1.45f, 1, 2.7f, 0.15f), 380, 6.5f, 6.0f, 4, 0, Wp.Cannon(90, 50), Wp.Coax());
    }

    // ------------------------------------------------------------------ IFV / APC
    static void BuildIFVAPC()
    {
        Cat c = Cat.IFV;
        Veh("ifv_bmp1", "BMP-1", c, 3, S(3.7f, 2.1f, true, 5, 3, 1, 0.1f, 1.2f, 1.1f, 6, 1.5f, 0.16f, ramp: true, trackW: 0.4f), 240, 4, 8.0f, 3, 8, Wp.Cannon(60, 42, "73mm gun"), Wp.Coax()).crush = false;
        Veh("ifv_bmp2", "BMP-2", c, 3, S(3.8f, 2.1f, true, 5, 3, 1, 0.1f, 1.3f, 1.2f, 2, 1.9f, 0.1f, ramp: true, trackW: 0.4f), 260, 4.5f, 8.2f, 3, 7, Wp.Auto(20, 45), Wp.Coax(), Wp.ATGM(55)).crush = false;
        Veh("ifv_bmp3", "BMP-3", c, 2, S(3.9f, 2.2f, true, 6, 2, 1, -0.1f, 1.5f, 1.4f, 1, 1.7f, 0.2f, ramp: false, trackW: 0.4f), 290, 5, 8.5f, 3, 7, Wp.Cannon(75, 48, "100mm gun"), Wp.Auto(15, 42), Wp.Coax()).crush = false;
        Veh("ifv_bmd4", "BMD-4M", c, 2, S(3.4f, 2.1f, true, 5, 2, 1, 0f, 1.5f, 1.4f, 1, 1.6f, 0.18f, trackW: 0.38f), 220, 3.5f, 8.8f, 3, 5, Wp.Cannon(70, 45, "100mm gun"), Wp.Auto(15, 42), Wp.Coax()).crush = false;
        Veh("ifv_t15", "T-15 Armata IFV", c, 2, S(5.0f, 2.3f, true, 7, 0, 5, 0.3f, 1.4f, 1.3f, 2, 2.1f, 0.12f, skirts: true, ramp: true), 380, 8, 7.6f, 3, 8, Wp.Auto(22, 48), Wp.Coax(), Wp.ATGM(60)).crush = false;
        Veh("ifv_bradley", "M2A2 Bradley", c, 1, S(4.5f, 2.3f, true, 6, 0, 4, 0.1f, 1.6f, 1.5f, 2, 2.0f, 0.12f, skirts: true, ramp: true), 300, 5.5f, 8.0f, 3, 6, Wp.Auto(20, 46), Wp.Coax(), Wp.ATGM(60)).crush = false;
        Veh("ifv_cv90", "CV90", c, 1, S(4.4f, 2.2f, true, 6, 1, 2, 0.15f, 1.7f, 1.5f, 2, 2.1f, 0.13f, ramp: true, skirts: true), 300, 5.5f, 8.8f, 3, 8, Wp.Auto(24, 48), Wp.Coax()).crush = false;
        Veh("ifv_marder", "Marder 1A3", c, 1, S(4.2f, 2.1f, true, 6, 0, 1, 0.3f, 1.3f, 1.2f, 2, 1.8f, 0.1f, ramp: true), 270, 5, 8.0f, 3, 6, Wp.Auto(20, 44), Wp.Coax()).crush = false;

        c = Cat.APC;
        Veh("apc_btr80", "BTR-80", c, 3, S(4.9f, 2.35f, false, 8, 2, 5, 0.9f, 0.9f, 0.9f, 4, 1.2f, 0.1f, ax: 4), 190, 3.2f, 11.0f, 2, 9, Wp.HMG());
        Veh("apc_btr82", "BTR-82A", c, 2, S(5.0f, 2.35f, false, 8, 2, 1, 0.9f, 1.1f, 1.0f, 2, 1.5f, 0.1f, ax: 4, skirts: true), 210, 3.8f, 11.0f, 2, 8, Wp.Auto(16, 42), Wp.Coax());
        Veh("apc_btr4", "BTR-4E Bucephalus", c, 1, S(5.3f, 2.5f, false, 8, 1, 1, 0.5f, 1.3f, 1.2f, 2, 1.8f, 0.1f, ax: 4), 230, 4f, 11.5f, 3, 8, Wp.Auto(18, 44), Wp.Coax(), Wp.ATGM(55));
        Veh("apc_m113", "M113", c, 1, S(4.2f, 2.3f, true, 5, 0, 5, 0.3f, 0.9f, 0.9f, 4, 1.1f, 0.1f, trackW: 0.42f), 170, 2.8f, 8.0f, 2, 11, Wp.HMG()).crush = false;
        Veh("apc_mtlb", "MT-LB", c, 3, S(4.1f, 2.4f, true, 6, 3, 5, 0.9f, 0.9f, 0.9f, 4, 1.1f, 0.1f, trackW: 0.48f), 180, 3f, 8.5f, 2, 10, Wp.LMG()).crush = false;
        Veh("apc_stryker", "Stryker", c, 1, S(5.2f, 2.7f, false, 8, 0, 5, 0.6f, 1.0f, 1.0f, 4, 1.3f, 0.12f, ax: 4, skirts: true), 220, 4f, 11.5f, 2, 9, Wp.HMG());
        Veh("apc_btrd", "BTR-D", c, 2, S(4.0f, 2.2f, true, 5, 3, 5, 0.5f, 0.9f, 0.9f, 4, 1.1f, 0.1f, trackW: 0.4f), 160, 2.6f, 9f, 2, 8, Wp.HMG()).crush = false;
        Veh("apc_btr3", "BTR-3DA", c, 1, S(5.0f, 2.4f, false, 8, 3, 1, 0.7f, 1.1f, 1.0f, 2, 1.5f, 0.1f, ax: 4), 200, 3.4f, 11f, 2, 8, Wp.Auto(15, 42), Wp.Coax());

        c = Cat.Armored;
        Veh("arm_kozak", "Kozak-2", c, 1, S(4.7f, 2.2f, false, 4, 3, 5, 0.3f, 0.8f, 0.8f, 4, 1.1f, 0.1f, ax: 2), 130, 2.6f, 14f, 2, 5, Wp.HMG());
        Veh("arm_novator", "Novator", c, 1, S(4.5f, 2.1f, false, 4, 3, 0, 0f, 1f, 1f, 0, 1f, 0.1f, ax: 2), 120, 2.4f, 15f, 2, 5, Wp.LMG());
        Veh("arm_tigr", "Tigr-M", c, 2, S(4.6f, 2.1f, false, 4, 3, 5, 0.2f, 0.8f, 0.8f, 4, 1.1f, 0.1f, ax: 2), 130, 2.5f, 15f, 2, 5, Wp.HMG());
        Veh("arm_typhoon", "Typhoon-K", c, 2, S(6.6f, 2.6f, false, 6, 3, 5, 0.4f, 0.9f, 0.9f, 4, 1.2f, 0.1f, ax: 3, cab: 1), 210, 3.5f, 11f, 2, 14, Wp.HMG());
        Veh("arm_spartak", "Spartak", c, 2, S(5.3f, 2.3f, false, 4, 3, 5, 0.4f, 0.8f, 0.8f, 4, 1.1f, 0.1f, ax: 2), 150, 2.7f, 14f, 2, 6, Wp.HMG());
        Veh("arm_m1117", "M1117 Guardian", c, 1, S(5.0f, 2.45f, false, 4, 0, 5, 0.1f, 0.9f, 0.9f, 4, 1.1f, 0.1f, ax: 2), 150, 3f, 13f, 3, 3, Wp.HMG(), Wp.Coax());
        Veh("arm_maxxpro", "MaxxPro MRAP", c, 1, S(5.8f, 2.55f, false, 4, 0, 5, 0.2f, 0.9f, 0.9f, 4, 1.1f, 0.1f, ax: 2), 190, 3.6f, 11f, 2, 8, Wp.HMG());
        Veh("arm_brdm2", "BRDM-2", c, 3, S(5.0f, 2.35f, false, 8, 2, 5, 0.1f, 1.0f, 1.0f, 4, 1.4f, 0.1f, ax: 2), 120, 2.4f, 13f, 3, 0, Wp.HMG(), Wp.Coax());
        Veh("arm_brdm2at", "BRDM-2 ATGM", c, 3, S(5.0f, 2.35f, false, 8, 2, 8, 0.1f, 1.2f, 1.0f, 0, 1.0f, 0.1f, ax: 2), 120, 2.4f, 13f, 3, 0, Wp.ATGM(65), Wp.ATGM(65));
    }

    // ------------------------------------------------------------------ trucks and cars
    static void BuildWheeled()
    {
        Cat c = Cat.Trucks;
        var t = Veh("trk_ural", "Ural-4320 Cargo", c, 3, S(6.8f, 2.5f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 0, ax: 3, cabL: 1.8f), 140, 1f, 9f, 1, 16);
        t.aura = Aura.Ammo; t.desc = "Cargo truck. Resupplies ammunition of nearby friendly units.";
        Veh("trk_kamaz", "KamAZ-5350 Troop Truck", c, 2, S(7.0f, 2.55f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 7, ax: 3, cabL: 1.9f), 150, 1f, 9.5f, 1, 20);
        Veh("trk_kraz", "KrAZ-6322 Truck", c, 1, S(7.2f, 2.6f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 1, ax: 3, cabL: 2.0f), 160, 1.2f, 9f, 1, 14).aura = Aura.Ammo;
        Veh("trk_man", "MAN HX Truck", c, 1, S(7.0f, 2.5f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 0, ax: 3, cabL: 1.8f, cab: 1), 150, 1f, 10f, 1, 16);
        var f = Veh("trk_fuel", "Fuel Tanker ATZ", c, 3, S(7.0f, 2.5f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 2, ax: 3, cabL: 1.8f), 150, 1f, 8.5f, 1, 0); f.aura = Aura.Fuel; f.explosive = true; f.desc = "Refuels nearby vehicles. Explodes violently.";
        var m = Veh("trk_med", "Medical Truck", c, 3, S(6.6f, 2.5f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, bed: 4, ax: 2, cabL: 1.7f), 130, 1f, 10f, 1, 4); m.aura = Aura.Heal;
        var r = Veh("trk_repair", "Repair Truck", c, 3, S(7.0f, 2.5f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 5, ax: 3, cabL: 1.8f), 160, 1.2f, 8.5f, 2, 2); r.aura = Aura.Repair;
        var cm = Veh("trk_comms", "Communications Truck", c, 3, S(6.8f, 2.5f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 3, ax: 3, cabL: 1.8f), 130, 1f, 9f, 2, 2); cm.aura = Aura.Comms; cm.desc = "Extends spotting and drone signal range around it.";
        var s = Veh("trk_supply", "Supply Truck", c, 3, S(7.0f, 2.5f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 6, ax: 3, cabL: 1.8f), 150, 1f, 9f, 1, 6); s.aura = Aura.Ammo;
        Veh("trk_cargo2", "Transport Vehicle", c, 3, S(6.2f, 2.4f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, bed: 8, ax: 2, cabL: 1.6f), 120, 1f, 10f, 1, 12);

        c = Cat.Cars;
        Veh("car_uaz", "UAZ-3151 SUV", c, 3, S(3.2f, 1.6f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, cab: 0, bed: 10, ax: 2), 70, 0.5f, 14f, 1, 4);
        Veh("car_patriot", "UAZ Patriot", c, 2, S(3.5f, 1.7f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, cab: 1, bed: 10, ax: 2), 75, 0.5f, 15f, 1, 4);
        Veh("car_humvee", "HMMWV", c, 1, S(3.9f, 1.9f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, cab: 2, bed: 10, ax: 2), 85, 0.8f, 14f, 1, 4);
        Veh("car_pickup", "Pickup Truck", c, 3, S(4.1f, 1.8f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, cab: 3, bed: 11, ax: 2), 65, 0.4f, 16f, 1, 5);
        Veh("car_tech", "Technical (DShK)", c, 3, S(4.1f, 1.8f, false, 4, 0, 5, -0.7f, 0.9f, 0.9f, 4, 1.2f, 0.1f, cab: 3, bed: 11, ax: 2), 65, 0.4f, 16f, 2, 3, Wp.HMG());
        var amb = Veh("car_ambulance", "Medical SUV", c, 3, S(4.2f, 1.9f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, cab: 4, bed: 12, ax: 2), 70, 0.5f, 14f, 1, 2); amb.aura = Aura.Heal;
        Veh("car_command", "Command Car", c, 3, S(3.9f, 1.8f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, cab: 5, bed: 13, ax: 2), 75, 0.7f, 15f, 1, 3).aura = Aura.Comms;
    }

    // ------------------------------------------------------------------ artillery & MLRS
    static void BuildArtillery()
    {
        Cat c = Cat.Artillery;
        Veh("art_msta", "2S19 Msta-S", c, 2, S(5.6f, 2.5f, true, 6, 0, 6, -0.2f, 2.5f, 2.3f, 3, 3.8f, 0.2f, bustle: true), 340, 4.5f, 6.0f, 5, 0, Wp.Howitzer(180, 200, 5.5f), Wp.HMG());
        Veh("art_gvozd", "2S1 Gvozdika", c, 3, S(4.9f, 2.5f, true, 7, 3, 6, 0f, 2.0f, 2.0f, 3, 2.8f, 0.17f, trackW: 0.45f), 250, 3.5f, 7.0f, 4, 0, Wp.Howitzer(140, 150, 4.5f));
        Veh("art_akatsia", "2S3 Akatsiya", c, 3, S(5.2f, 2.8f, true, 6, 0, 6, -0.3f, 2.3f, 2.3f, 3, 3.3f, 0.18f), 280, 4f, 6.5f, 4, 0, Wp.Howitzer(160, 170, 5f));
        Veh("art_pzh", "PzH 2000", c, 1, S(7.0f, 3.0f, true, 7, 0, 6, 0f, 3.0f, 2.5f, 3, 4.6f, 0.2f, skirts: true, bustle: true), 360, 5f, 6.5f, 5, 0, Wp.Howitzer(190, 220, 4f), Wp.HMG());
        Veh("art_paladin", "M109A6 Paladin", c, 1, S(6.1f, 3.0f, true, 7, 0, 6, 0.1f, 2.8f, 2.5f, 3, 3.9f, 0.19f, bustle: true), 320, 4.5f, 6.5f, 4, 0, Wp.Howitzer(175, 190, 5f), Wp.HMG());
        Veh("art_caesar", "CAESAR 155", c, 1, S(8.2f, 2.5f, false, 6, 0, 6, -1.5f, 1.9f, 1.8f, 3, 4.0f, 0.18f, cab: 1, bed: 14, ax: 3, cabL: 1.9f), 190, 1.5f, 10f, 4, 0, Wp.Howitzer(170, 200, 4.5f));
        Veh("art_krab", "AHS Krab", c, 1, S(6.3f, 2.9f, true, 7, 0, 6, 0f, 2.8f, 2.4f, 3, 4.2f, 0.19f, bustle: true), 340, 4.5f, 6.0f, 5, 0, Wp.Howitzer(175, 200, 5f));
        var d30 = Veh("art_d30", "D-30 Howitzer (towed)", c, 3, S(5.4f, 2.0f, false, 2, 0, 6, 0.3f, 1.4f, 1.8f, 3, 3.0f, 0.15f, bed: 20, ax: 0), 90, 1.5f, 1.2f, 3, 0, Wp.Howitzer(150, 150, 6f)); d30.towed = true;
        var m777 = Veh("art_m777", "M777 Howitzer (towed)", c, 1, S(6.2f, 2.2f, false, 2, 0, 6, 0.3f, 1.4f, 1.8f, 3, 3.6f, 0.15f, bed: 20, ax: 0), 90, 1.5f, 1.2f, 3, 0, Wp.Howitzer(180, 210, 5.5f)); m777.towed = true;
        var msb = Veh("art_mstab", "2A65 Msta-B (towed)", c, 2, S(6.8f, 2.3f, false, 2, 0, 6, 0.3f, 1.4f, 1.9f, 3, 3.7f, 0.16f, bed: 20, ax: 0), 95, 1.5f, 1.2f, 3, 0, Wp.Howitzer(175, 200, 5.5f)); msb.towed = true;
        var mo = Veh("art_mortar120", "120mm Mortar", c, 3, S(2.0f, 1.6f, false, 2, 0, 6, 0f, 0.8f, 0.9f, 6, 1.1f, 0.2f, bed: 21, ax: 0), 60, 0.5f, 1.4f, 3, 0, Wp.Mortar(95, 80)); mo.towed = true;
        var mo2 = Veh("art_mortar82", "82mm Mortar", c, 3, S(1.4f, 1.2f, false, 2, 0, 6, 0f, 0.6f, 0.7f, 6, 0.9f, 0.16f, bed: 21, ax: 0), 45, 0.3f, 1.8f, 2, 0, Wp.Mortar(60, 60)); mo2.towed = true;

        c = Cat.MLRS;
        Veh("ml_grad", "BM-21 Grad", c, 3, S(7.0f, 2.6f, false, 6, 0, 7, -0.9f, 2.5f, 2.0f, 0, 1f, 0.1f, cab: 0, bed: 15, ax: 3, cabL: 1.9f), 160, 1.2f, 9.5f, 3, 0, Wp.Rockets(20, 170, 55));
        Veh("ml_uragan", "BM-27 Uragan", c, 3, S(9.0f, 2.8f, false, 8, 0, 7, -1.5f, 3.0f, 2.3f, 0, 1f, 0.1f, cab: 0, bed: 15, ax: 4, cabL: 2.1f), 190, 1.5f, 9f, 4, 0, Wp.Rockets(16, 200, 80));
        Veh("ml_smerch", "BM-30 Smerch", c, 3, S(9.4f, 2.9f, false, 8, 0, 7, -1.6f, 3.2f, 2.5f, 0, 1f, 0.1f, cab: 0, bed: 15, ax: 4, cabL: 2.1f), 200, 1.5f, 9f, 4, 0, Wp.Rockets(12, 260, 110));
        var tos = Veh("ml_tos1", "TOS-1A Solntsepyok", c, 2, S(6.4f, 3.0f, true, 6, 1, 7, -0.2f, 3.0f, 2.6f, 0, 1f, 0.1f, skirts: true), 420, 8f, 6f, 3, 0, Wp.Thermo(), Wp.Coax()); tos.desc = "Heavy flame-rocket launcher (abstract thermobaric fire effect).";
        Veh("ml_tornado", "Tornado-S", c, 2, S(9.6f, 2.9f, false, 8, 0, 7, -1.6f, 3.2f, 2.6f, 0, 1f, 0.1f, cab: 0, bed: 15, ax: 4, cabL: 2.1f), 210, 1.8f, 9f, 3, 0, Wp.Rockets(12, 280, 120));
        Veh("ml_himars", "M142 HIMARS", c, 1, S(7.0f, 2.5f, false, 6, 0, 7, -1.2f, 2.0f, 1.8f, 0, 1f, 0.1f, cab: 1, bed: 15, ax: 3, cabL: 2.0f), 180, 1.5f, 11f, 3, 0, Wp.Rockets(6, 270, 150));
        Veh("ml_m270", "M270 MLRS", c, 1, S(6.8f, 3.0f, true, 7, 0, 7, -0.6f, 3.0f, 2.6f, 0, 1f, 0.1f, trackW: 0.5f), 260, 3f, 7f, 3, 0, Wp.Rockets(12, 250, 120));
        Veh("ml_vampire", "RM-70 Vampire", c, 1, S(7.6f, 2.6f, false, 6, 0, 7, -1.0f, 2.4f, 2.0f, 0, 1f, 0.1f, cab: 0, bed: 15, ax: 3, cabL: 1.9f), 170, 1.2f, 9.5f, 3, 0, Wp.Rockets(40, 190, 45));
    }

    static void BuildAirDefense()
    {
        Cat c = Cat.AirDefense;
        var zu = Veh("ad_zu23", "ZU-23-2 Towed AA", c, 3, S(3.6f, 1.9f, false, 2, 0, 9, 0.1f, 1.2f, 1.3f, 5, 1.8f, 0.2f, bed: 22), 70, 1f, 1.5f, 3, 0, Wp.AAGun(46)); zu.towed = true;
        Veh("ad_shilka", "ZSU-23-4 Shilka", c, 3, S(6.3f, 2.9f, true, 6, 3, 9, -0.1f, 2.4f, 2.2f, 5, 2.4f, 0.22f), 230, 3f, 8f, 4, 0, Wp.AAGun(52), Wp.AAGun(52));
        Veh("ad_gepard", "Gepard SPAAG", c, 1, S(7.0f, 3.1f, true, 7, 0, 9, 0.2f, 2.5f, 2.6f, 5, 2.6f, 0.2f, skirts: true), 280, 4f, 8f, 3, 0, Wp.AAGun(60), Wp.AAGun(60));
        Veh("ad_tunguska", "2S6 Tunguska", c, 2, S(7.0f, 3.2f, true, 6, 3, 9, 0f, 2.6f, 2.5f, 5, 2.6f, 0.2f), 270, 4f, 8f, 4, 0, Wp.AAGun(56), Wp.SAM(70, 90));
        Veh("ad_strela10", "9K35 Strela-10", c, 3, S(6.0f, 2.8f, true, 6, 3, 8, 0f, 1.5f, 1.6f, 0, 1f, 0.1f), 190, 3f, 9f, 3, 0, Wp.SAM(70, 90));
        Veh("ad_tor", "9K330 Tor-M2", c, 2, S(7.0f, 3.0f, true, 6, 3, 8, 0f, 2.1f, 2.0f, 0, 1f, 0.1f), 230, 3.5f, 8.5f, 3, 0, Wp.SAM(85, 110), Wp.SAM(85, 110));
        Veh("ad_pantsir", "Pantsir-S1", c, 2, S(9.0f, 2.8f, false, 8, 0, 8, -1.8f, 2.6f, 2.4f, 5, 2.0f, 0.2f, cab: 0, bed: 16, ax: 4, cabL: 2.1f), 220, 2f, 10f, 3, 0, Wp.SAM(100, 120), Wp.AAGun(48));
        Veh("ad_buk", "9K37 Buk-M2", c, 3, S(7.4f, 3.2f, true, 7, 3, 8, 0f, 2.6f, 1.9f, 0, 1f, 0.1f), 250, 3f, 7.5f, 3, 0, Wp.SAM(140, 170));
        Veh("ad_s300", "S-300 Launcher", c, 3, S(10.0f, 2.9f, false, 8, 0, 8, -1.8f, 4.2f, 2.2f, 0, 1f, 0.1f, cab: 0, bed: 16, ax: 4, cabL: 2.1f), 240, 2f, 8f, 3, 0, Wp.SAM(200, 220));
        Veh("ad_nasams", "NASAMS Launcher", c, 1, S(8.0f, 2.6f, false, 6, 0, 8, -1.5f, 3.0f, 2.0f, 0, 1f, 0.1f, cab: 1, bed: 16, ax: 3, cabL: 2.0f), 200, 1.8f, 10f, 2, 0, Wp.SAM(150, 150));
        Veh("ad_iris", "IRIS-T SLM", c, 1, S(8.4f, 2.6f, false, 6, 0, 8, -1.6f, 3.2f, 2.0f, 0, 1f, 0.1f, cab: 1, bed: 16, ax: 3, cabL: 2.0f), 210, 1.8f, 10f, 2, 0, Wp.SAM(130, 140));
        Veh("ad_skynex", "Skynex AA Gun", c, 1, S(7.0f, 2.5f, false, 6, 0, 9, -1.2f, 1.7f, 1.7f, 5, 2.2f, 0.18f, cab: 1, bed: 17, ax: 3, cabL: 2.0f), 170, 1.5f, 9f, 3, 0, Wp.AAGun(58));
        var ma = Soldier("ad_manpads", "MANPADS Gunner", Role.AA, 3, 100, Wp.Rifle(), Wp.Manpads()); ma.cat = Cat.AirDefense;
    }

    // ------------------------------------------------------------------ aircraft
    static void BuildAir()
    {
        Cat c = Cat.Aircraft;
        W[] fighter = { Wp.AirGun(), Wp.AAM(110) };
        Air("av_mig29", "MiG-29", c, 3, Kind.Plane, 0, 6.8f, 5.6f, 120, 2f, 62, 75, 2, Wp.AirGun(), Wp.AAM(110), Wp.AAM(110));
        Air("av_su27", "Su-27 Flanker", c, 3, Kind.Plane, 1, 7.6f, 6.4f, 135, 2.2f, 64, 70, 2, Wp.AirGun(), Wp.AAM(125), Wp.AAM(125));
        Air("av_f16", "F-16C Fighting Falcon", c, 1, Kind.Plane, 2, 6.6f, 4.8f, 115, 2f, 66, 80, 2, Wp.AirGun(), Wp.AAM(120), Wp.AGMissile());
        Air("av_su35", "Su-35S", c, 2, Kind.Plane, 3, 7.8f, 6.6f, 145, 2.4f, 68, 80, 2, Wp.AirGun(), Wp.AAM(130), Wp.AAM(130), Wp.AGMissile());
        Air("av_su57", "Su-57", c, 2, Kind.Plane, 4, 8.0f, 6.2f, 150, 2.6f, 72, 85, 2, Wp.AirGun(), Wp.AAM(140), Wp.AGMissile());
        Air("av_mig31", "MiG-31 Interceptor", c, 2, Kind.Plane, 5, 9.0f, 7.4f, 150, 2.4f, 76, 55, 2, Wp.AAM(180), Wp.AAM(180));
        Air("av_su25", "Su-25 Frogfoot", c, 3, Kind.Plane, 6, 7.0f, 7.4f, 190, 4f, 44, 55, 1, Wp.AirGun(), Wp.AGRockets(16), Wp.Bombs(4, 220));
        Air("av_su34", "Su-34 Fullback", c, 2, Kind.Plane, 7, 8.4f, 7.4f, 200, 3.5f, 52, 50, 1, Wp.AirGun(), Wp.AGRockets(20), Wp.Bombs(8, 300), Wp.AAM(80));
        Air("av_su24", "Su-24M Fencer", c, 3, Kind.Plane, 8, 8.2f, 7.6f, 190, 3.2f, 50, 45, 1, Wp.AGRockets(16), Wp.Bombs(8, 280));
        Air("av_tu22", "Tu-22M3 Backfire", c, 2, Kind.Plane, 9, 11.0f, 11.0f, 260, 3f, 56, 28, 2, Wp.Bombs(14, 320));
        Air("av_tu95", "Tu-95MS Bear", c, 2, Kind.Plane, 10, 13.0f, 14.0f, 330, 3f, 36, 18, 2, Wp.Bombs(18, 330));
        Air("av_tu160", "Tu-160 Blackjack", c, 2, Kind.Plane, 11, 14.0f, 13.0f, 340, 3f, 60, 22, 2, Wp.Bombs(20, 340));
        Air("av_il76", "Il-76MD Transport", c, 3, Kind.Plane, 12, 12.0f, 12.0f, 280, 2f, 34, 18, 1).cap = 40;
        Air("av_an26", "An-26 Transport", c, 3, Kind.Plane, 13, 8.5f, 11.0f, 180, 1.5f, 30, 22, 1).cap = 24;
        Air("av_c130", "C-130 Hercules", c, 1, Kind.Plane, 14, 11.0f, 13.0f, 260, 2f, 33, 18, 1).cap = 40;
        Air("av_an124", "An-124 Ruslan", c, 3, Kind.Plane, 15, 16.0f, 16.0f, 380, 2f, 30, 14, 1).cap = 60;
        Air("av_an30", "An-30 Recon", c, 1, Kind.Plane, 16, 9.0f, 11.0f, 160, 1.5f, 32, 25, 1);
        Air("av_il20", "Il-20 Coot", c, 2, Kind.Plane, 17, 11.5f, 12.5f, 220, 2f, 31, 20, 1);
        Air("av_su24mr", "Su-24MR Recon", c, 2, Kind.Plane, 18, 8.2f, 7.4f, 170, 3f, 55, 48, 2);
        foreach (var d in All) if (d.id == "av_il76" || d.id == "av_an26" || d.id == "av_c130" || d.id == "av_an124") { d.role = Role.Pilot; d.special = "transport"; }

        c = Cat.Helicopters;
        Air("he_mi24", "Mi-24P Hind", c, 3, Kind.Heli, 0, 6.6f, 6.8f, 220, 4f, 25, 120, 1, Wp.AirGun(), Wp.AGRockets(16), Wp.ATGM(70)).cap = 6;
        Air("he_mi28", "Mi-28N Havoc", c, 2, Kind.Heli, 1, 6.2f, 6.4f, 230, 4.5f, 27, 130, 1, Wp.AirGun(), Wp.AGRockets(20), Wp.ATGM(75));
        Air("he_ka52", "Ka-52 Alligator", c, 2, Kind.Heli, 2, 5.4f, 6.0f, 240, 4.5f, 28, 140, 1, Wp.AirGun(), Wp.AGRockets(20), Wp.ATGM(80), Wp.AAM(60));
        Air("he_apache", "AH-64D Apache", c, 1, Kind.Heli, 3, 6.4f, 6.2f, 230, 4.5f, 28, 130, 1, Wp.AirGun(), Wp.AGRockets(19), Wp.ATGM(85));
        Air("he_mi8", "Mi-8MT Transport", c, 3, Kind.Heli, 4, 7.2f, 7.6f, 190, 2f, 22, 100, 1, Wp.LMG()).cap = 24;
        Air("he_mi26", "Mi-26 Heavy Transport", c, 3, Kind.Heli, 5, 9.4f, 8.8f, 300, 2.5f, 20, 70, 1).cap = 60;
        Air("he_ch47", "CH-47 Chinook", c, 1, Kind.Heli, 6, 8.6f, 9.0f, 260, 2.2f, 22, 80, 1, Wp.LMG()).cap = 33;
        Air("he_uh60", "UH-60 Black Hawk", c, 1, Kind.Heli, 7, 6.0f, 6.4f, 170, 2f, 26, 120, 1, Wp.HMG()).cap = 11;
        Air("he_ka29", "Ka-29 Assault", c, 2, Kind.Heli, 8, 5.6f, 6.2f, 190, 2.5f, 24, 120, 1, Wp.AGRockets(10), Wp.LMG()).cap = 16;
        Air("he_mi17", "Mi-17 Multirole", c, 3, Kind.Heli, 9, 7.2f, 7.6f, 190, 2f, 23, 110, 1, Wp.AGRockets(12), Wp.LMG()).cap = 20;
        var me = Air("he_medevac", "Mi-8 Medevac", c, 3, Kind.Heli, 10, 7.2f, 7.6f, 180, 2f, 22, 100, 1); me.cap = 12; me.aura = Aura.Heal; me.special = "medic";
        var me2 = Air("he_uh60med", "UH-60 Medevac", c, 1, Kind.Heli, 11, 6.0f, 6.4f, 170, 2f, 26, 120, 1); me2.cap = 6; me2.aura = Aura.Heal; me2.special = "medic";
        foreach (var d in All) if (d.kind == Kind.Heli && d.cap > 0) d.special = d.special == "" ? "transport" : d.special;
    }

    static void BuildUAV()
    {
        Cat c = Cat.UAV;
                        var d1 = Air("uav_orlan", "Orlan-10 Recon UAV", c, 2, Kind.Drone, 0, 2.4f, 3.2f, 40, 0.5f, 17, 120, 1); d1.signal = 230; d1.battery = 260; d1.special = "recon"; d1.flares = false;
        var d2 = Air("uav_leleka", "Leleka-100 Recon UAV", c, 1, Kind.Drone, 0, 2.2f, 3.0f, 38, 0.5f, 16, 120, 1); d2.signal = 220; d2.battery = 240; d2.special = "recon"; d2.sizeScale = 0.95f;
        var d3 = Air("uav_mavic", "Mavic Quadcopter", c, 3, Kind.Drone, 1, 1.0f, 1.0f, 18, 0.2f, 12, 240, 1); d3.signal = 110; d3.battery = 150; d3.special = "recon";
        var d4 = Air("uav_babayaga", "Heavy Quadcopter 'Baba Yaga'", c, 1, Kind.Drone, 2, 1.8f, 1.8f, 30, 0.3f, 9, 200, 1, Wp.Bombs(3, 55)); d4.signal = 100; d4.battery = 120; d4.special = "bomber"; d4.w[0].range = 4;
        var d5 = Air("uav_fpv", "FPV Strike Drone", c, 3, Kind.Drone, 3, 0.9f, 0.9f, 10, 0.1f, 30, 300, 1); d5.signal = 95; d5.battery = 90; d5.special = "fpv";
        var d6 = Air("uav_fpv2", "FPV Heavy 10-inch", c, 3, Kind.Drone, 4, 1.2f, 1.2f, 14, 0.1f, 27, 280, 1); d6.signal = 100; d6.battery = 100; d6.special = "fpv";
        var d7 = Air("uav_tb2", "Bayraktar TB2 UCAV", c, 1, Kind.Drone, 5, 6.5f, 12.0f, 120, 1.5f, 20, 60, 2, Wp.AGMissile(), Wp.AGMissile()); d7.signal = 420; d7.battery = 600; d7.special = "ucav";
        var d8 = Air("uav_orion", "Orion MALE UCAV", c, 2, Kind.Drone, 6, 8.0f, 16.0f, 130, 1.5f, 19, 55, 2, Wp.AGMissile(), Wp.AGMissile()); d8.signal = 440; d8.battery = 620; d8.special = "ucav";
        var d9 = Air("uav_geran", "Geran Loitering Drone", c, 2, Kind.Drone, 7, 3.5f, 2.6f, 35, 0.8f, 26, 60, 2); d9.signal = 600; d9.battery = 400; d9.special = "loiter";
        var d10 = Air("uav_lancet", "Lancet Loitering Munition", c, 2, Kind.Drone, 8, 1.6f, 1.8f, 20, 0.4f, 24, 90, 1); d10.signal = 150; d10.battery = 140; d10.special = "loiter";
        var d11 = Air("uav_switchblade", "Switchblade Loitering Drone", c, 1, Kind.Drone, 9, 1.3f, 1.2f, 15, 0.3f, 25, 100, 1); d11.signal = 140; d11.battery = 130; d11.special = "loiter";
        foreach (var d in All) if (d.cat == Cat.UAV) { d.radius = Mathf.Max(0.4f, Mathf.Min(d.len, d.wid) * 0.4f); d.flares = false; }
    }

    // ------------------------------------------------------------------ ships
    static void BuildShips()
    {
        Ship("sh_hetman", "Hetman Sahaidachny Frigate", 1, 0, 14f, 2.6f, 700, 6, 6.5f, 20, Wp.NavalGun(105, 125), Wp.CIWS(), Wp.ShipMissile(200));
        Ship("sh_grigor", "Admiral Grigorovich Frigate", 2, 0, 15f, 2.8f, 760, 6, 7f, 24, Wp.NavalGun(110, 130), Wp.CIWS(), Wp.ShipMissile(240), Wp.SAM(110, 150));
        Ship("sh_buyan", "Buyan-M Corvette", 2, 1, 9.5f, 2.0f, 380, 4, 9f, 10, Wp.NavalGun(80, 100), Wp.CIWS(), Wp.ShipMissile(230));
        Ship("sh_lutsk", "Vinnytsia Corvette", 1, 1, 10f, 2.1f, 400, 4, 8.5f, 10, Wp.NavalGun(80, 100), Wp.ShipMG(), Wp.ShipMissile(160));
        Ship("sh_raptor", "Raptor Patrol Boat", 1, 2, 5.4f, 1.5f, 120, 1.5f, 15f, 8, Wp.ShipMG(), Wp.LMG().Also(x => { x.mount = 0; }));
        Ship("sh_grach", "Grachonok Patrol Boat", 2, 2, 6.2f, 1.7f, 140, 2f, 13f, 8, Wp.ShipMG(), Wp.ShipMG());
        Ship("sh_mk6", "Mark VI Patrol Boat", 1, 2, 7.0f, 1.9f, 160, 2f, 14f, 8, Wp.ShipMG(), Wp.ShipMG());
        Ship("sh_ropucha", "Ropucha Landing Ship", 2, 3, 12.5f, 2.9f, 540, 3.5f, 5.5f, 60, Wp.ShipMG(), Wp.AGRockets(16).Also(x => { x.mount = 0; x.range = 70; }));
        Ship("sh_olef", "Yuri Olefirenko Landing Ship", 1, 3, 12f, 2.8f, 520, 3.5f, 5.5f, 60, Wp.ShipMG(), Wp.CIWS());
        Ship("sh_lcu", "Landing Craft", 3, 7, 6.0f, 2.0f, 220, 2f, 7f, 24, Wp.ShipMG());
        Ship("sh_transport", "Cargo Transport Vessel", 3, 4, 15f, 3.0f, 520, 2f, 5f, 80);
        Ship("sh_slava", "Slava Missile Cruiser", 2, 5, 17f, 3.2f, 1000, 7f, 6.5f, 30, Wp.NavalGun(120, 140), Wp.ShipMissile(300), Wp.ShipMissile(300), Wp.SAM(140, 170), Wp.CIWS());
        Ship("sh_karakurt", "Karakurt Missile Corvette", 2, 5, 9f, 2.1f, 360, 3.5f, 9.5f, 8, Wp.NavalGun(80, 100), Wp.ShipMissile(260), Wp.CIWS());
        Ship("sh_neptune", "Neptune Missile Boat", 1, 5, 8.5f, 2.1f, 340, 3f, 9f, 8, Wp.ShipMG(), Wp.ShipMissile(240));
        Ship("sh_tanker", "Auxiliary Tanker", 3, 6, 13f, 2.9f, 420, 2f, 5.5f, 12).aura = Aura.Fuel;
        var tug = Ship("sh_tug", "Rescue Tug", 3, 6, 7.5f, 2.2f, 240, 2f, 7f, 14); tug.aura = Aura.Repair; tug.style = 8;
    }

    // ------------------------------------------------------------------ airborne scenarios
    static void BuildAirborne()
    {
        Cat c = Cat.Airborne;
        var d = new UnitDef { id = "ab_para6", name = "Paratroopers x6 (drop)", cat = c, mask = 3, kind = Kind.Soldier, fam = "special", special = "para", loadId = "inf_para", loadN = 6, len = 3, wid = 3, hp = 1, desc = "Six paratroopers drop from altitude and descend by parachute." }; Add(d);
        d = new UnitDef { id = "ab_para16", name = "Paratrooper Platoon x16 (drop)", cat = c, mask = 3, kind = Kind.Soldier, fam = "special", special = "para", loadId = "inf_para", loadN = 16, len = 3, wid = 3, hp = 1, desc = "A full platoon dropped over the target." }; Add(d);
        d = new UnitDef { id = "ab_il76", name = "Il-76 Paradrop Mission", cat = c, mask = 3, kind = Kind.Plane, fam = "special", special = "il76", loadId = "inf_para", loadN = 24, len = 12, wid = 12, hp = 1, desc = "Transport aircraft flies in and drops 24 paratroopers over the clicked point." }; Add(d);
        d = new UnitDef { id = "ab_c130", name = "C-130 Paradrop Mission", cat = c, mask = 1, kind = Kind.Plane, fam = "special", special = "c130", loadId = "inf_para", loadN = 24, len = 11, wid = 13, hp = 1, desc = "Transport aircraft flies in and drops 24 paratroopers." }; Add(d);
        d = new UnitDef { id = "ab_mi8", name = "Mi-8 Air Assault (landing)", cat = c, mask = 3, kind = Kind.Heli, fam = "special", special = "mi8", loadId = "inf_mech", loadN = 12, len = 7, wid = 7, hp = 1, desc = "Helicopter lands at the clicked point and unloads 12 soldiers." }; Add(d);
        d = new UnitDef { id = "ab_uh60", name = "UH-60 Air Assault (landing)", cat = c, mask = 1, kind = Kind.Heli, fam = "special", special = "uh60", loadId = "inf_mech", loadN = 10, len = 7, wid = 7, hp = 1, desc = "Helicopter lands at the clicked point and unloads 10 soldiers." }; Add(d);
        d = new UnitDef { id = "ab_landing", name = "Naval Landing Group", cat = c, mask = 3, kind = Kind.Ship, fam = "special", special = "landing", loadId = "inf_naval", loadN = 20, len = 6, wid = 2, hp = 1, desc = "Landing craft with marines sails to shore and unloads troops." }; Add(d);
        d = new UnitDef { id = "ab_pilot", name = "Downed Pilot (parachute)", cat = c, mask = 3, kind = Kind.Soldier, fam = "special", special = "pilot", loadId = "inf_crew", loadN = 1, len = 3, wid = 3, hp = 1, desc = "A pilot parachutes down and becomes infantry." }; Add(d);
    }

    // ------------------------------------------------------------------ engineering
    static void BuildEngineering()
    {
        Cat c = Cat.Engineering;
        var bat = Veh("eng_bat2", "BAT-2 Bulldozer", c, 2, S(6.6f, 3.3f, true, 6, 0, 0, 0, 0, 0, 0, 0, 0, dozer: true), 360, 4f, 6f, 2, 0); bat.aura = Aura.Dozer; bat.cat = c;
        var d6 = Veh("eng_d6", "Armored Dozer D6", c, 1, S(6.0f, 3.2f, true, 6, 0, 0, 0, 0, 0, 0, 0, 0, dozer: true), 320, 3.5f, 6f, 2, 0); d6.aura = Aura.Dozer;
        Veh("eng_imr", "IMR-2M Engineering Vehicle", c, 3, S(6.4f, 3.3f, true, 6, 1, 0, 0, 0, 0, 0, 0, 0, dozer: true, cage: false, bed: 5), 420, 8f, 6f, 2, 0).aura = Aura.Dozer;
        var brem = Veh("eng_brem", "BREM-1 Recovery Vehicle", c, 3, S(6.6f, 3.2f, true, 6, 1, 0, 0, 0, 0, 0, 0, 0, bed: 5), 380, 6f, 6.5f, 3, 0); brem.aura = Aura.Repair; brem.desc = "Repairs damaged friendly vehicles nearby.";
        var mtu = Veh("eng_mtu", "MTU-72 Bridge Layer", c, 3, S(7.2f, 3.1f, true, 6, 1, 0, 0, 0, 0, 0, 0, 0, bed: 9), 330, 5f, 6.2f, 2, 0); mtu.aura = Aura.Bridge; mtu.desc = "Deploy Bridge: spans narrow water ahead.";
        var ur = Veh("eng_ur77", "UR-77 Mine Clearer", c, 2, S(6.5f, 3.2f, true, 6, 1, 0, 0, 0, 0, 0, 0, 0, bed: 5), 360, 5f, 6f, 2, 0);
        var jcb = Veh("eng_excav", "Excavator", c, 3, S(6.0f, 2.6f, false, 4, 0, 0, 0, 0, 0, 0, 0, 0, bed: 5, ax: 2, cabL: 1.6f), 150, 1f, 5f, 1, 0); jcb.aura = Aura.Dozer;
        var cr = Veh("eng_crane", "Mobile Crane", c, 3, S(8.0f, 2.8f, false, 6, 0, 0, 0, 0, 0, 0, 0, 0, bed: 5, ax: 3, cabL: 2.0f), 200, 1.5f, 7f, 2, 0); cr.aura = Aura.Repair;
        foreach (var d in All) if (d.cat == c) d.crush = d.aura == Aura.Dozer;
    }

    // ------------------------------------------------------------------ props: infrastructure & other
    static void BuildProps()
    {
        Cat c = Cat.Infrastructure;
        Prop("pr_house", "Village House", c, 0, 5f, 4.2f, 160, true);
        Prop("pr_cottage", "Cottage", c, 1, 5.6f, 4.6f, 180, true);
        Prop("pr_apt", "Apartment Block", c, 2, 12f, 7f, 700, true);
        Prop("pr_tower", "High-rise Tower", c, 3, 8f, 8f, 900, true);
        Prop("pr_warehouse", "Warehouse", c, 4, 12f, 6.5f, 420, true);
        var fa = Prop("pr_factory", "Factory", c, 5, 14f, 9f, 650, true);
        Prop("pr_hangar", "Aircraft Hangar", c, 6, 14f, 10f, 520, true);
        Prop("pr_barracks", "Barracks", c, 7, 11f, 4.6f, 360, true);
        Prop("pr_cmd", "Command Center", c, 8, 6f, 5f, 400, true);
        Prop("pr_ctower", "Control Tower", c, 9, 3.4f, 3.4f, 260, true);
        Prop("pr_radar", "Radar Station", c, 10, 5f, 5f, 200, true);
        Prop("pr_fueltank", "Fuel Storage Tank", c, 11, 5f, 5f, 260, true).explosive = true;
        var am = Prop("pr_ammo", "Ammunition Depot", c, 12, 8f, 6f, 300, true); am.explosive = true;
        Prop("pr_bunker", "Concrete Bunker", c, 13, 5.5f, 4.2f, 800, true).flammable = false;
        Prop("pr_watch", "Watchtower", c, 15, 2.6f, 2.6f, 140, true);
        Prop("pr_church", "Church", c, 16, 7f, 5f, 380, true);
        Prop("pr_garage", "Garage / Shop", c, 17, 6.5f, 5f, 200, true);
        Prop("pr_crane", "Port Crane", c, 18, 9f, 4f, 420, true);
        Prop("pr_containers", "Container Stack", c, 19, 6f, 5f, 260, true).flammable = false;
        Prop("pr_pylon", "Antenna Tower", c, 34, 2.6f, 2.6f, 150, true);
        Prop("pr_helipad", "Helipad", c, 33, 7f, 7f, 9999, false).flammable = false;

        c = Cat.Other;
        Prop("ot_sandbag", "Sandbag Wall", c, 21, 4.5f, 1.2f, 80, true).flammable = false;
        Prop("ot_trap", "Tank Traps", c, 22, 4.5f, 1.8f, 160, true).flammable = false;
        Prop("ot_fence", "Fence", c, 23, 5f, 0.5f, 40, false);
        Prop("ot_barrier", "Concrete Barrier", c, 24, 4f, 1.2f, 220, true).flammable = false;
        Prop("ot_trench", "Trench", c, 25, 6f, 1.6f, 9999, false).flammable = false;
        Prop("ot_tent", "Field Tent", c, 26, 5f, 4f, 70, false);
        var fh = Prop("ot_hospital", "Field Hospital", c, 27, 6f, 5f, 90, false); fh.aura = Aura.Heal;
        Prop("ot_ammocrate", "Ammo Crates", c, 28, 2.2f, 1.6f, 60, false).explosive = true;
        var sc = Prop("ot_supply", "Supply Crates", c, 29, 2.8f, 2f, 60, false); sc.aura = Aura.Ammo;
        Prop("ot_barrels", "Explosive Barrels", c, 30, 1.6f, 1.6f, 40, false).explosive = true;
        Prop("ot_flag", "Faction Flag", c, 31, 1.2f, 1.2f, 30, false);
        Prop("ot_net", "Camouflage Net", c, 32, 6f, 5f, 40, false);
        var mg = Prop("ot_mgnest", "MG Nest", c, 35, 3.2f, 3f, 180, false, Wp.HMG().Also(x => { x.mount = 0; x.muzzle = 1.3f; x.range = 44; }));
        mg.turretless = false; mg.hardness = 1;
        var at = Prop("ot_atnest", "AT Gun Nest", c, 36, 3.6f, 3.4f, 200, false, Wp.Cannon(90, 50, "AT gun").Also(x => { x.muzzle = 1.8f; }));
        var op = Prop("ot_obs", "Observation Post", c, 37, 3f, 3f, 140, false); op.detect = 110;
        Prop("ot_pine", "Pine Tree", c, 40, 3f, 3f, 40, false);
        Prop("ot_oak", "Oak Tree", c, 39, 4f, 4f, 50, false);
        Prop("ot_birch", "Birch Tree", c, 41, 3f, 3f, 35, false);
        Prop("ot_haystack", "Haystack", c, 44, 2.6f, 2.6f, 40, false);
        var brl = Prop("ot_fire", "Burning Barrel", c, 45, 1.2f, 1.2f, 30, false); brl.special = "burning";
        foreach (var d in All) if (d.kind == Kind.Prop) { if (d.id == "ot_pine" || d.id == "ot_oak" || d.id == "ot_birch") d.special = "tree"; d.mask = 3; }
        // static weapon props act as armed emplacements
        ById["ot_mgnest"].detect = 55; ById["ot_atnest"].detect = 60;
    }

    // seeded lookup of the helper-only ids used by map generator
    public static UnitDef Get(string id) { UnitDef d; return ById.TryGetValue(id, out d) ? d : null; }
}

public static class WExt
{
    public static W Also(this W w, Action<W> a) { a(w); return w; }
}
