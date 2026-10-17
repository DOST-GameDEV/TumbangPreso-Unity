using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, THE PAYOFF: THE SHOCKWAVE (owner on cutscene v2, 2026-10-17: "ult cutscene sucks bcz the entire ult builds up
        // to this shockwave but IT NEVER HAPPENS"). This supersedes the 2026-10-08 rule that the scene ends on the fist with no
        // blast shown. Both fists land at the stomp and the ground answers, all inside the cutscene:
        //  * a molten flash erupts where the fists land, bursting up and out in a quarter second;
        //  * THE EARTH WAVE: a raised ring of rock rolls out across the plates from where its fist landed, 17 m in 1.0 s,
        //    tall at first and flattening as it goes, molten light on its inner edge and a second ring echoing behind it
        //    (film r12: a thin glow ring alone read as nothing, the payoff has to be the ground itself);
        //  * a surge of ragged dust clumps rides on it, billowing as it spreads; it crosses the lens (the lens kicks);
        //  * each plate heaves up and tips as the front passes under it, then drops back (`DnHeave`, used by the plates);
        //  * the players on them are thrown back and up with their plate (`DnShockPass`, used by `DanteTake`);
        //  * twenty typed rocks are flung up off the court where the front passes and come down where they land, and stay.
        // The stage still hands back whole: the wave passes, the plates settle, nothing goes under (that stays live play's).
        // Posed from the scene clock only. Never white; stone never fades.
        // =========================================================================================
        private static readonly Vector2 DnShockCentre = new Vector2(2.75f, 3.4f);
        private const float DnShockSeconds = 1.0f, DnShockReach = 17f;

        // The rocks the front flings up: (bearing from the centre in degrees, distance, size, launch speed up, outward speed, seed).
        private static readonly (float bearing, float distance, float size, float up, float outward, int seed)[] DnShockRocks =
        {
            (  6f, 2.6f, .34f, 5.6f, 2.2f,  3), ( 31f, 3.4f, .26f, 6.4f, 2.8f,  7), ( 58f, 2.9f, .30f, 5.0f, 2.0f, 11), ( 84f, 4.2f, .22f, 6.8f, 3.1f, 13),
            (112f, 3.1f, .38f, 4.6f, 1.8f, 17), (139f, 3.8f, .24f, 6.0f, 2.6f, 19), (167f, 2.7f, .28f, 5.4f, 2.3f, 23), (196f, 4.5f, .20f, 7.0f, 3.3f, 29),
            (223f, 3.2f, .32f, 5.2f, 2.1f, 31), (251f, 3.9f, .25f, 6.2f, 2.7f, 37), (278f, 2.8f, .36f, 4.8f, 1.9f, 41), (305f, 4.0f, .23f, 6.6f, 3.0f, 43),
            (333f, 3.5f, .29f, 5.8f, 2.5f, 47), ( 18f, 6.2f, .30f, 5.0f, 2.4f, 53), ( 47f, 7.1f, .24f, 5.8f, 2.9f, 59), ( 74f, 6.6f, .27f, 4.6f, 2.2f, 61),
            (352f, 6.9f, .26f, 5.4f, 2.6f, 67), (326f, 7.6f, .22f, 6.0f, 3.0f, 71), (100f, 7.8f, .31f, 4.4f, 2.0f, 73), (  0f, 9.0f, .28f, 5.2f, 2.8f, 79),
        };

        private int _dnShockRing, _dnShockRingHot, _dnShockEarth, _dnShockEcho, _dnShockFlash;
        private const int DustClumps = 36;
        private readonly System.Collections.Generic.List<int> _dnShockDust = new System.Collections.Generic.List<int>(DustClumps);
        private readonly System.Collections.Generic.List<int> _dnShockRockPieces = new System.Collections.Generic.List<int>(20);

        private void BuildDanteShock()
        {
            _dnShockRing = AddGlow("DanteShockRing", new Color(1f, .42f, .08f), DnRingMesh, billboard: false, band: true, falloff: 1.1f, core: .5f);
            _dnShockRingHot = AddGlow("DanteShockRingHot", new Color(1f, .62f, .18f), DnRingMesh, billboard: false, band: true, falloff: 2.2f, core: .8f);
            // The dust surge: ragged clumps standing on the front, never one cylinder (film r13: a wall mesh read as a brown
            // tube with a hard rim). Each its own jagged silhouette.
            for (int i = 0; i < DustClumps; i++)
                _dnShockDust.Add(Add("DanteShockDust" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 101 + i * 13)),
                                     new Color(.5f, .37f, .24f, .7f) * (i % 3 == 0 ? .85f : 1f), .3f, plain: true));
            _dnShockEarth = BasilioStone("DanteShockEarth", VfxShapes.TwoSided(VfxShapes.Shockfront(360f, 56, 1f, .8f, .22f, 5)), new Color(.29f, .24f, .2f, 1f));
            _dnShockEcho = AddGlow("DanteShockEcho", new Color(1f, .42f, .08f), DnRingMesh, billboard: false, band: true, falloff: 1.3f, core: .4f);
            _dnShockFlash = AddGlow("DanteShockFlash", new Color(1f, .5f, .12f), falloff: 1.6f, core: .7f);
            var basalt = new[] { new Color(.24f, .2f, .17f, 1f), new Color(.31f, .27f, .24f, 1f), new Color(.2f, .17f, .15f, 1f) };
            for (int i = 0; i < DnShockRocks.Length; i++)
                _dnShockRockPieces.Add(BasilioStone("DanteShockRock" + i, VfxShapes.Prism(5, 1f, .7f, .3f, 0f, DnShockRocks[i].seed), basalt[i % 3]));
            Place(_dnShockEarth, Vector3.zero, Vector3.zero, Quaternion.identity, 1f);
        }

        /// <summary>The front's distance from where its fist landed at t (0 before the stomp).</summary>
        private static float DnShockRadius(float t)
        {
            float u = Mathf.Clamp01((t - DnStomp) / DnShockSeconds);
            return t < DnStomp ? 0f : .3f + DnShockReach * (1f - Mathf.Pow(1f - u, 2.2f));
        }

        /// <summary>How far up the front lifts the ground at a court point (x, z) at t: a bump riding the front, 0 to 1.</summary>
        private static float DnShockPass(float t, Vector2 at)
        {
            if (t < DnStomp) return 0f;
            // Rising as the front arrives, full under it, settled again once it has gone by.
            float gap = (DnShockRadius(t) - Vector2.Distance(at, DnShockCentre)) / 1.7f;
            return Mathf.Exp(-gap * gap * 3f);
        }

        /// <summary>How far the front has gone past a court point (x, z): 0 before it arrives, 1 once it is well past. Kept, not settled.</summary>
        private static float DnShockPassed(float t, Vector2 at)
            => t < DnStomp ? 0f : Mathf.Clamp01((DnShockRadius(t) - Vector2.Distance(at, DnShockCentre) + 1.7f) / 3.4f);

        /// <summary>A plate's heave as the front passes: how high (m) and its tip outward (degrees) about its own middle.</summary>
        private void DnHeave(float t, Vector2 home, out float up, out Quaternion tip)
        {
            float lift = DnShockPass(t, home);
            up = .55f * lift;
            var away = (home - DnShockCentre).normalized;
            tip = Quaternion.AngleAxis(-9f * lift, new Vector3(-away.y, 0f, away.x));
        }

        private void SampleDanteShock(float t)
        {
            float age = t - DnStomp, u = Mathf.Clamp01(age / DnShockSeconds);
            float r = DnShockRadius(t);
            var centre = new Vector3(DnShockCentre.x, DnFloat + .06f, DnShockCentre.y);
            bool on = age >= 0f;
            // The molten front: a wide ring and a thin hot edge on it, both fading as the wave spends itself.
            PlaceGlow(_dnShockRing, centre, new Vector3(r, 1f, r), Quaternion.identity, on ? (1.5f * (1f - u * .75f)) : 0f);
            PlaceGlow(_dnShockRingHot, centre + Vector3.up * .01f, new Vector3(r * 1.01f, 1f, r * 1.01f), Quaternion.identity, on ? (1.2f * (1f - u)) : 0f);
            // The dust surge riding the front: clumps standing on it facing out, billowing taller and wider as they go, then
            // thinning out once past the lens. Each clump its own size, lean and lag behind the front.
            for (int i = 0; i < _dnShockDust.Count; i++)
            {
                float k = i / (float)DustClumps, jit = Mathf.Repeat(i * .618f, 1f);
                float bearing = (k + jit * .02f) * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(bearing), 0f, Mathf.Cos(bearing));
                float lag = .35f + .9f * jit, cr = Mathf.Max(0f, r - lag);
                float puff = Mathf.Lerp(1.2f, 3.4f, u) * (.7f + .6f * Mathf.Repeat(i * .37f, 1f));
                float high = puff * (.8f + .5f * jit);
                var turn = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, (jit - .5f) * 30f);
                Place(_dnShockDust[i], centre + dir * cr + Vector3.up * (high * .55f), on && cr > .2f ? new Vector3(puff, 1f, high) : Vector3.zero, turn,
                      on ? .9f * (1f - u * u) : 0f);
            }
            // THE EARTH WAVE: the ground itself rolls out in a raised ring of rock, flattening as it spreads, and is gone
            // back into the court (sunk, never faded) by the time it has passed the plates.
            float crest = on ? Mathf.Lerp(1.5f, 0f, Ease(.0f, 1f, u)) : 0f;
            Place(_dnShockEarth, centre - Vector3.up * (.08f + .5f * u * u), crest > .01f ? new Vector3(r, crest, r) : Vector3.zero, Quaternion.identity, 1f);
            // The echo: a second molten ring 0.12 s behind the first.
            float er = DnShockRadius(t - .12f);
            PlaceGlow(_dnShockEcho, centre + Vector3.up * .02f, new Vector3(er, 1f, er), Quaternion.identity, age >= .12f ? 1.1f * (1f - Mathf.Clamp01((age - .12f) / DnShockSeconds)) : 0f);
            // The eruption where the fists land: a flash of heat bursting up and out, a quarter second.
            float burst = on ? Mathf.Clamp01(age / .25f) : 0f;
            PlaceGlow(_dnShockFlash, centre + Vector3.up * 1.6f, Vector3.one * Mathf.Lerp(4f, 9f, burst), Quaternion.identity, on ? 1.6f * (1f - burst) : 0f);

            // The rocks: each flung up when the front reaches it, falling under gravity, landing where it lands, staying there.
            for (int i = 0; i < DnShockRocks.Length; i++)
            {
                var row = DnShockRocks[i];
                float a = row.bearing * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float launch = DnStomp + Mathf.Clamp01(row.distance / DnShockReach) * DnShockSeconds * .6f;
                float flight = t - launch;
                var start = centre + dir * row.distance;
                if (flight < 0f) { Place(_dnShockRockPieces[i], start, Vector3.zero, Quaternion.identity, 1f); continue; }
                float land = 2f * row.up / 9.8f;
                float ft = Mathf.Min(flight, land);
                var at = start + dir * (row.outward * ft) + Vector3.up * (row.up * ft - 4.9f * ft * ft);
                float spin = (flight < land ? flight : land) * (200f + row.seed * 7f);
                Place(_dnShockRockPieces[i], at, Vector3.one * row.size * 1.8f, Quaternion.Euler(spin, row.seed * 23f, spin * .6f), 1f);
            }
        }
    }
}
