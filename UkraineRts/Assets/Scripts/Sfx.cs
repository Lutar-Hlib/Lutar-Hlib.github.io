using System;
using System.Collections.Generic;
using UnityEngine;

// Procedurally synthesised sound effects with distance attenuation relative to the camera.
public static class Sfx
{
    public enum Id { Rifle, MG, Auto, Cannon, Arty, Mortar, Rocket, Missile, Explosion, ExplosionBig, Hit, Splash, UiClick, UiOpen, Flare, Death, Whoosh, COUNT }
    public enum Loop { Tank, Car, Heli, Jet, Prop, Track, Water, Drone, COUNT }

    const int SR = 22050;
    static AudioClip[] clips = new AudioClip[(int)Id.COUNT];
    static AudioClip[] loopClips = new AudioClip[(int)Loop.COUNT];
    static AudioSource[] voices; static GameObject root;
    static AudioSource[] loops;
    static float[] lvl = new float[(int)Loop.COUNT], panAcc = new float[(int)Loop.COUNT], wAcc = new float[(int)Loop.COUNT], pitchK = new float[(int)Loop.COUNT];
    static float[] loopBase = { 0.55f, 0.35f, 0.7f, 0.55f, 0.5f, 0.3f, 0.35f, 0.3f };
    static int[] perFrame = new int[(int)Id.COUNT]; static int frame;
    public static float master = 0.8f;
    public static Vector2 camPos; public static float camSize = 25f;
    static System.Random rnd = new System.Random(5);

    static float Noise() { return (float)(rnd.NextDouble() * 2.0 - 1.0); }

    public static void Init()
    {
        if (root != null) return;
        root = new GameObject("Audio");
        voices = new AudioSource[28];
        for (int i = 0; i < voices.Length; i++) { var a = root.AddComponent<AudioSource>(); a.playOnAwake = false; a.spatialBlend = 0f; voices[i] = a; }
        BuildClips();
        loops = new AudioSource[(int)Loop.COUNT];
        for (int i = 0; i < loops.Length; i++)
        {
            var a = root.AddComponent<AudioSource>(); a.clip = loopClips[i]; a.loop = true; a.volume = 0f; a.playOnAwake = false; a.spatialBlend = 0f; a.Play();
            loops[i] = a;
        }
    }

    static AudioClip Make(string name, float dur, Func<float, int, float> f, bool loop = false)
    {
        int n = (int)(dur * SR); var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = f(i / (float)SR, i);
        if (loop) { int fade = SR / 20; for (int i = 0; i < fade; i++) { float k = i / (float)fade; d[n - fade + i] = d[n - fade + i] * (1f - k) + d[i] * k * 0f + d[i] * k; } }
        float mx = 0.0001f; for (int i = 0; i < n; i++) mx = Mathf.Max(mx, Mathf.Abs(d[i]));
        if (mx > 1f) for (int i = 0; i < n; i++) d[i] /= mx;
        var c = AudioClip.Create(name, n, 1, SR, false); c.SetData(d, 0);
        return c;
    }

    static void BuildClips()
    {
        float lp;
        lp = 0; clips[(int)Id.Rifle] = Make("rifle", 0.16f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.6f; return (lp * 0.9f * Mathf.Exp(-t * 38f) + Mathf.Sin(2 * Mathf.PI * 140f * t) * Mathf.Exp(-t * 35f) * 0.6f) * 0.9f; });
        lp = 0; clips[(int)Id.MG] = Make("mg", 0.1f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.5f; return (lp * Mathf.Exp(-t * 50f) + Mathf.Sin(2 * Mathf.PI * 190f * t) * Mathf.Exp(-t * 45f) * 0.5f) * 0.8f; });
        lp = 0; clips[(int)Id.Auto] = Make("auto", 0.14f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.35f; return (lp * Mathf.Exp(-t * 28f) * 1.4f + Mathf.Sin(2 * Mathf.PI * 95f * t) * Mathf.Exp(-t * 25f) * 0.7f) * 0.85f; });
        lp = 0; clips[(int)Id.Cannon] = Make("cannon", 0.9f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.18f; float sweep = Mathf.Sin(2 * Mathf.PI * (110f * t - 60f * t * t)); return lp * 2.2f * Mathf.Exp(-t * 7f) + sweep * Mathf.Exp(-t * 6f) * 0.9f + Noise() * Mathf.Exp(-t * 60f) * 0.8f; });
        lp = 0; clips[(int)Id.Arty] = Make("arty", 1.3f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.1f; float sweep = Mathf.Sin(2 * Mathf.PI * (70f * t - 25f * t * t)); return lp * 3f * Mathf.Exp(-t * 4.5f) + sweep * Mathf.Exp(-t * 4f) * 1.1f + Noise() * Mathf.Exp(-t * 70f) * 0.7f; });
        lp = 0; clips[(int)Id.Mortar] = Make("mortar", 0.6f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.2f; return lp * 2f * Mathf.Exp(-t * 9f) + Mathf.Sin(2 * Mathf.PI * 120f * t) * Mathf.Exp(-t * 12f) * 0.8f; });
        lp = 0; clips[(int)Id.Rocket] = Make("rocket", 1.2f, (t, i) => { float n = Noise(); lp += (n - lp) * (0.15f + t * 0.1f); float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.2f)); return lp * 2.4f * env * env + Noise() * Mathf.Exp(-t * 50f) * 0.5f; });
        lp = 0; clips[(int)Id.Missile] = Make("missile", 1.4f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.22f; float env = Mathf.Clamp01(t * 8f) * Mathf.Exp(-t * 1.3f); return lp * 2.2f * env + Mathf.Sin(2 * Mathf.PI * (300f + 80f * t) * t) * 0.06f * env; });
        lp = 0; clips[(int)Id.Explosion] = Make("expl", 1.7f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.12f; return lp * 3f * Mathf.Exp(-t * 2.8f) + Mathf.Sin(2 * Mathf.PI * (55f - 12f * t) * t) * Mathf.Exp(-t * 2.4f) * 1.0f + Noise() * Mathf.Exp(-t * 40f) * 0.6f; });
        lp = 0; clips[(int)Id.ExplosionBig] = Make("explbig", 2.6f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.07f; return lp * 4f * Mathf.Exp(-t * 1.8f) + Mathf.Sin(2 * Mathf.PI * (40f - 6f * t) * t) * Mathf.Exp(-t * 1.5f) * 1.2f + Noise() * Mathf.Exp(-t * 30f) * 0.6f; });
        clips[(int)Id.Hit] = Make("hit", 0.25f, (t, i) => (Mathf.Sin(2 * Mathf.PI * 880f * t) * 0.4f + Mathf.Sin(2 * Mathf.PI * 1320f * t) * 0.3f + Noise() * 0.4f) * Mathf.Exp(-t * 28f));
        lp = 0; clips[(int)Id.Splash] = Make("splash", 0.6f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.3f; return lp * 1.6f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.6f)) * Mathf.Exp(-t * 4f); });
        clips[(int)Id.UiClick] = Make("click", 0.07f, (t, i) => Mathf.Sin(2 * Mathf.PI * 1500f * t) * Mathf.Exp(-t * 60f) * 0.6f);
        clips[(int)Id.UiOpen] = Make("open", 0.16f, (t, i) => (Mathf.Sin(2 * Mathf.PI * 700f * t) * (t < 0.07f ? 1f : 0f) + Mathf.Sin(2 * Mathf.PI * 1000f * t) * (t >= 0.07f ? 1f : 0f)) * Mathf.Exp(-t * 14f) * 0.5f);
        lp = 0; clips[(int)Id.Flare] = Make("flare", 0.7f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.7f; return lp * 0.9f * Mathf.Exp(-t * 4f) * Mathf.Clamp01(t * 30f); });
        lp = 0; clips[(int)Id.Death] = Make("death", 0.3f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.08f; return lp * 3f * Mathf.Exp(-t * 9f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.3f)); });
        lp = 0; clips[(int)Id.Whoosh] = Make("whoosh", 0.9f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.1f; return lp * 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.9f)); });

        // seamless engine loops (integer-frequency sinusoids so that they tile)
        lp = 0; loopClips[(int)Loop.Tank] = Make("tank", 1f, (t, i) =>
        {
            float am = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 9f * t);
            float s = Mathf.Sin(2 * Mathf.PI * 38f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 57f * t) * 0.35f + Mathf.Sin(2 * Mathf.PI * 76f * t) * 0.3f + Mathf.Sin(2 * Mathf.PI * 114f * t) * 0.12f;
            float n = Noise(); lp += (n - lp) * 0.05f; return (s * am + lp * 2f) * 0.5f;
        }, true);
        lp = 0; loopClips[(int)Loop.Car] = Make("car", 1f, (t, i) =>
        {
            float am = 0.75f + 0.25f * Mathf.Sin(2 * Mathf.PI * 15f * t);
            float s = Mathf.Sin(2 * Mathf.PI * 70f * t) * 0.4f + Mathf.Sin(2 * Mathf.PI * 140f * t) * 0.3f + Mathf.Sin(2 * Mathf.PI * 210f * t) * 0.18f + Mathf.Sin(2 * Mathf.PI * 280f * t) * 0.1f;
            float n = Noise(); lp += (n - lp) * 0.08f; return (s * am + lp) * 0.55f;
        }, true);
        lp = 0; loopClips[(int)Loop.Heli] = Make("heli", 1f, (t, i) =>
        {
            float pulse = Mathf.Pow(Mathf.Abs(Mathf.Sin(2 * Mathf.PI * 6f * t)), 5f);
            float n = Noise(); lp += (n - lp) * 0.07f;
            return (lp * 3.2f * (0.15f + pulse) + Mathf.Sin(2 * Mathf.PI * 54f * t) * 0.22f * (0.4f + pulse) + Mathf.Sin(2 * Mathf.PI * 18f * t) * 0.2f) * 0.8f;
        }, true);
        lp = 0; loopClips[(int)Loop.Jet] = Make("jet", 1f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.28f; return (lp * 1.4f + Mathf.Sin(2 * Mathf.PI * 310f * t) * 0.12f + Mathf.Sin(2 * Mathf.PI * 620f * t) * 0.06f) * 0.7f; }, true);
        lp = 0; loopClips[(int)Loop.Prop] = Make("prop", 1f, (t, i) => { float saw = ((t * 110f) % 1f) * 2f - 1f; float n = Noise(); lp += (n - lp) * 0.1f; return (saw * 0.35f * (0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * 22f * t)) + lp * 0.6f) * 0.6f; }, true);
        lp = 0; loopClips[(int)Loop.Track] = Make("track", 1f, (t, i) =>
        {
            float ph = (t * 11f) % 1f; float click = Mathf.Exp(-ph * 30f) * Noise();
            float n = Noise(); lp += (n - lp) * 0.2f; return (click * 0.9f + lp * 0.25f) * 0.7f;
        }, true);
        lp = 0; loopClips[(int)Loop.Water] = Make("water", 1f, (t, i) => { float n = Noise(); lp += (n - lp) * 0.12f; return lp * 2f * (0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 3f * t)); }, true);
        loopClips[(int)Loop.Drone] = Make("drone", 1f, (t, i) => { float am = 0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * 19f * t); return (Mathf.Sin(2 * Mathf.PI * 360f * t) * 0.3f + Mathf.Sin(2 * Mathf.PI * 720f * t) * 0.14f + Noise() * 0.06f) * am * 0.8f; }, true);
    }

    static float Atten(Vector2 p)
    {
        float d = (p - camPos).magnitude;
        float ref_ = camSize * 1.3f + 8f;
        float k = 1f / (1f + (d / ref_) * (d / ref_) * 1.5f);
        return k;
    }
    static float Pan(Vector2 p) { return Mathf.Clamp((p.x - camPos.x) / (camSize * 1.8f + 1f), -1f, 1f) * 0.7f; }

    public static void Play(Id id, Vector2 pos, float vol = 1f)
    {
        if (voices == null) return;
        if (perFrame[(int)id] > 4) return;
        float a = Atten(pos) * vol * master;
        if (a < 0.015f) return;
        perFrame[(int)id]++;
        AudioSource src = null;
        for (int i = 0; i < voices.Length; i++) if (!voices[i].isPlaying) { src = voices[i]; break; }
        if (src == null) { float mv = 1f; for (int i = 0; i < voices.Length; i++) if (voices[i].volume < mv) { mv = voices[i].volume; src = voices[i]; } if (src == null || mv > a) return; }
        src.clip = clips[(int)id]; src.volume = Mathf.Clamp01(a); src.pitch = G.Rnd(0.92f, 1.08f); src.panStereo = Pan(pos); src.Play();
    }
    public static void Ui(bool open = false) { Play(open ? Id.UiOpen : Id.UiClick, camPos, 0.6f / Mathf.Max(0.2f, Atten(camPos))); }

    public static void Emit(Loop l, Vector2 pos, float loud)
    {
        int k = (int)l; float a = Atten(pos) * loud;
        if (a < 0.01f) return;
        lvl[k] += a; panAcc[k] += Pan(pos) * a; wAcc[k] += a;
    }

    public static void Update(float dt, bool paused)
    {
        frame++; for (int i = 0; i < perFrame.Length; i++) perFrame[i] = 0;
        if (loops == null) return;
        AudioListener.volume = 1f;
        for (int i = 0; i < loops.Length; i++)
        {
            float target = paused ? 0f : (1f - Mathf.Exp(-lvl[i] * 0.9f)) * loopBase[i] * master;
            loops[i].volume = Mathf.Lerp(loops[i].volume, target, 1f - Mathf.Exp(-dt * 8f));
            loops[i].panStereo = wAcc[i] > 0.001f ? panAcc[i] / wAcc[i] : 0f;
            loops[i].pitch = Mathf.Lerp(loops[i].pitch, 1f + Mathf.Min(0.15f, lvl[i] * 0.03f), dt * 3f);
            lvl[i] = 0f; panAcc[i] = 0f; wAcc[i] = 0f;
        }
    }
}
