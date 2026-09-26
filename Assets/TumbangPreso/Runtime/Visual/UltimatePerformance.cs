using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// One hero's authored ultimate introduction: its length, body keys, lift, shots and voice.
    ///
    /// ⚠️⚠️ THE TABLES ARE AUTHORED IN `tools/author_ultimate_intros.py`, NOT HERE. REFINE-2.11
    /// (owner 2026-09-24): every hero gets a moment in which the stage briefly feels like theirs,
    /// each authored for that character (*"dont js spam copy paste stuff bcz it will be boring"*).
    /// The script writes `Resources/UltimateIntros/&lt;hero&gt;.txt` and renders pose sheets of the
    /// real glb from the real shots, so each performance was looked at before the next began.
    /// Research, per-hero plan and evidence: `docs/reports/ultimate-performances-2026-09-24/`.
    ///
    /// ⚠️⚠️ <see cref="Seconds"/> IS NETWORK TIMING, NOT ONLY PRESENTATION. The shared phase's
    /// boundary is the longest accepted hero's length, derived on every peer from the same
    /// commits (`SharedUltimatePhase.CohortSeconds`). Changing a length is a protocol change;
    /// `NetSession.ProtocolVersion` 52 records the move from the fixed 2.8 s.
    /// </summary>
    public sealed class UltimatePerformance
    {
        public const float DefaultSeconds = 2.8f;
        /// <summary>The live body starts blending in this long before the boundary.</summary>
        public const float HandoffLead = .4f;
        /// <summary>The overlay dissolves back to the court over the last this-many seconds.</summary>
        public const float ReturnSeconds = .12f;
        // ⚠️ 6.5 s (was 5, 2026-09-27): the owner on Paete's v7, *"lowk slow down ult a bit i cant comprehend wtf is happening"*, chose
        // 6.5 s over his own earlier *"dont go past 5 seconds"*. The cap is the longest any introduction may run; a longer table is clamped.
        public const float MinSeconds = 2.4f, MaxSeconds = 6.5f;

        public readonly struct Key
        {
            public readonly float Time;
            public readonly Vector3 Torso, Head, ArmLeft, ArmRight, LegLeft, LegRight;
            public Key(float time, Vector3 torso, Vector3 head, Vector3 armLeft, Vector3 armRight, Vector3 legLeft, Vector3 legRight)
            { Time = time; Torso = torso; Head = head; ArmLeft = armLeft; ArmRight = armRight; LegLeft = legLeft; LegRight = legRight; }
        }

        public readonly struct ShotKey
        {
            public readonly float Start, End, FovFrom, FovTo;
            public readonly Vector3 EyeFrom, EyeTo, LookFrom, LookTo;
            /// <summary>A close shot crops the body on purpose (the face, the hands). Framing
            /// checks and the wall-clearance fallback treat it differently from a full-body shot.</summary>
            public readonly bool Close;
            /// <summary>Fit every character body (Nemu and the growing Kuro) instead of a fixed eye.</summary>
            public readonly bool Fit;
            public ShotKey(float start, float end, Vector3 eyeFrom, Vector3 eyeTo, Vector3 lookFrom, Vector3 lookTo, float fovFrom, float fovTo, bool close, bool fit)
            { Start = start; End = end; EyeFrom = eyeFrom; EyeTo = eyeTo; LookFrom = lookFrom; LookTo = lookTo; FovFrom = fovFrom; FovTo = fovTo; Close = close; Fit = fit; }
        }

        public string Hero { get; private set; }
        public float Seconds { get; private set; } = DefaultSeconds;
        public readonly List<Key> Keys = new List<Key>();
        public readonly List<float> Punches = new List<float>();
        public readonly List<Vector2> Lift = new List<Vector2>();
        public readonly List<ShotKey> Shots = new List<ShotKey>();
        public bool HasStill { get; private set; }
        public Vector3 StillEye { get; private set; }
        public Vector3 StillLook { get; private set; }
        public float StillFov { get; private set; } = 46;
        public float VoiceAt { get; private set; } = -1;
        public string VoiceCue { get; private set; }
        public float HandoffAt => Mathf.Max(0, Seconds - HandoffLead);

        private static readonly Dictionary<string, UltimatePerformance> Cache = new Dictionary<string, UltimatePerformance>(8);

        /// <summary>
        /// The authored performance for a hero, or null when none exists. `cheska` holding a slipper
        /// asks for `cheska-held` first: her gather moves to the free palm.
        /// </summary>
        public static UltimatePerformance For(string hero, bool holdingSlipper = false)
        {
            if (string.IsNullOrEmpty(hero)) return null;
            if (holdingSlipper)
            {
                var held = Load(hero + "-held");
                if (held != null) return held;
            }
            return Load(hero);
        }

        /// <summary>
        /// ⚠️ A HERO WITH NO TABLE KEEPS THE OLD SHARED LENGTH rather than failing the phase, so a
        /// new hero added without an introduction still pauses and releases exactly as before.
        /// </summary>
        public static float SecondsFor(string hero) => For(hero)?.Seconds ?? DefaultSeconds;

        private static UltimatePerformance Load(string name)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var asset = Resources.Load<TextAsset>("UltimateIntros/" + name);
            UltimatePerformance parsed = null;
            if (asset != null)
            {
                try { parsed = Parse(name, asset.text); }
                catch (Exception error) { Debug.LogException(error); parsed = null; }
                Resources.UnloadAsset(asset);
            }
            Cache[name] = parsed;
            return parsed;
        }

        public static UltimatePerformance Parse(string name, string text)
        {
            var p = new UltimatePerformance { Hero = name.EndsWith("-held") ? name.Substring(0, name.Length - 5) : name };
            float F(string[] parts, int i) => float.Parse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture);
            Vector3 V(string[] parts, int i) => new Vector3(F(parts, i), F(parts, i + 1), F(parts, i + 2));
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "seconds": p.Seconds = Mathf.Clamp(F(parts, 1), MinSeconds, MaxSeconds); break;
                    case "voice": p.VoiceAt = F(parts, 1); p.VoiceCue = parts[2]; break;
                    case "key":
                        p.Keys.Add(new Key(F(parts, 1), V(parts, 2), V(parts, 5), V(parts, 8), V(parts, 11), V(parts, 14), V(parts, 17)));
                        break;
                    case "punch": p.Punches.Add(F(parts, 1)); break;
                    case "lift": p.Lift.Add(new Vector2(F(parts, 1), F(parts, 2))); break;
                    case "shot":
                        p.Shots.Add(new ShotKey(F(parts, 1), F(parts, 2), V(parts, 3), V(parts, 6), V(parts, 9), V(parts, 12), F(parts, 15), F(parts, 16),
                            Array.IndexOf(parts, "close") > 16, Array.IndexOf(parts, "fit") > 16));
                        break;
                    case "still":
                        p.HasStill = true; p.StillEye = V(parts, 1); p.StillLook = V(parts, 4); p.StillFov = F(parts, 7);
                        break;
                }
            }
            p.Keys.Sort((a, b) => a.Time.CompareTo(b.Time));
            p.Lift.Sort((a, b) => a.x.CompareTo(b.x));
            p.Shots.Sort((a, b) => a.Start.CompareTo(b.Start));
            return p;
        }

        /// <summary>Metres off the floor at this moment, eased between the authored points.</summary>
        public float LiftAt(float time)
        {
            if (Lift.Count == 0) return 0;
            if (time <= Lift[0].x) return Lift[0].y;
            for (int i = 0; i + 1 < Lift.Count; i++)
                if (time <= Lift[i + 1].x)
                {
                    float u = Mathf.InverseLerp(Lift[i].x, Lift[i + 1].x, time);
                    return Mathf.Lerp(Lift[i].y, Lift[i + 1].y, u * u * (3 - 2 * u));
                }
            return Lift[Lift.Count - 1].y;
        }

        /// <summary>The authored shot covering this moment: its index and eased progress.</summary>
        public int ShotIndexAt(float time)
        {
            for (int i = 0; i < Shots.Count; i++) if (time < Shots[i].End) return i;
            return Shots.Count - 1;
        }

        public void Shot(int index, float time, out Vector3 eye, out Vector3 look, out float fov)
        {
            var s = Shots[index];
            float u = Mathf.InverseLerp(s.Start, s.End, time); u = u * u * (3 - 2 * u);
            eye = Vector3.Lerp(s.EyeFrom, s.EyeTo, u); look = Vector3.Lerp(s.LookFrom, s.LookTo, u);
            fov = Mathf.Lerp(s.FovFrom, s.FovTo, u);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Cache.Clear();
    }
}
