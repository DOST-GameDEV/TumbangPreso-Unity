using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CONTINENTAL DRIFT in live play, after the hand-back (`docs/reports/dante-presentation-2026-10-16/plan.md` section 6). This is
    /// what most players actually see, from their own first person, so the scale must read map-wide. It adds to the fault ink
    /// (`DanteDriftFault`), posed from the wave's accepted age like it (`StepTo`), so a rejoiner and a replay draw the same frame:
    ///  * THE FUSE: a molten seam races forward from him along the cascade's line, far faster than the blasts, and leaves a
    ///    glowing crack-star at the middle of every band before that band goes, so a victim can read it and step out.
    ///  * FIVE BLASTS, one per band every 0.30 s: the cutscene's row of six basalt slabs heaves up across the band, molten light under it,
    ///    five big chunks thrown (typed per band, no two alike), dust clumps at its front, and molten cracks that open exactly
    ///    along the band's real hit rectangle (full width x band depth): the picture never lies about the hit area. The rubble
    ///    holds, then sinks back into the court in two crumbling steps before the wave's tail ends.
    ///  * MAP-WIDE: every viewer's own camera shakes by its distance to the blast, the ground rumbles, and a few grains fall
    ///    through their own view from above.
    ///  * THE SCAR: the fault line stays after the wave, cools orange, dull red, grey, then crumbles flat (`DanteDriftScar`).
    /// </summary>
    public sealed class DanteDriftLive : MonoBehaviour, IVfxTimeline
    {
        public float LifeSeconds => GeoRules.DriftSeconds;
        private const float FuseSpeed = 34f;          // m/s along the line: it reaches the far band long before its blast

        private Vector3 _origin, _forward, _right;
        private float _halfWidth, _depth;
        private readonly List<Band> _bands = new List<Band>(GeoRules.DriftBlasts);
        private Transform _fuse;
        private Material _fuseMaterial;
        private int _shookUpTo = -1;
        private float _lastAge = -1f;

        private sealed class Band
        {
            public float Start;            // metres along the line
            public readonly List<Transform> Slabs = new List<Transform>(6);
            public readonly List<Transform> Dust = new List<Transform>(4);
            public readonly List<Material> DustMaterials = new List<Material>(4);
            public Transform Star, Glow;
            public Material StarMaterial, GlowMaterial;
            public readonly List<Chunk> Chunks = new List<Chunk>(5);
            public Transform[] Edge = new Transform[4];
        }
        private struct Chunk { public Transform Node; public Vector3 Start, Velocity, Spin; public float Size; }

        // Five chunks per band, typed per band: (across as a share of the half width, up speed, out speed along, size, seed).
        private static readonly (float across, float up, float along, float size, int seed)[][] ChunkRows =
        {
            new[] { (-.18f, 5.6f, 1.2f, .34f, 3), (.09f, 6.4f, .4f, .26f, 8), (.31f, 4.9f, 1.8f, .30f, 12), (-.42f, 4.2f, .9f, .22f, 17), (.52f, 5.1f, -.3f, .25f, 21) },
            new[] { (.14f, 6.1f, .8f, .36f, 4), (-.27f, 5.2f, 1.5f, .28f, 9), (.38f, 4.4f, .2f, .24f, 14), (-.05f, 6.9f, -.4f, .21f, 18), (-.55f, 4.6f, 1.1f, .27f, 23) },
            new[] { (-.08f, 6.6f, .5f, .38f, 5), (.24f, 5.5f, 1.4f, .29f, 10), (-.33f, 4.7f, .7f, .26f, 15), (.47f, 5.9f, -.2f, .23f, 19), (-.60f, 4.0f, 1.6f, .24f, 24) },
            new[] { (.20f, 5.8f, 1.0f, .35f, 6), (-.15f, 6.7f, .3f, .27f, 11), (-.40f, 4.5f, 1.7f, .31f, 16), (.36f, 4.9f, .6f, .22f, 20), (.62f, 5.3f, -.5f, .26f, 25) },
            new[] { (-.22f, 7.0f, .9f, .40f, 7), (.11f, 6.0f, 1.6f, .33f, 13), (.43f, 5.2f, .1f, .27f, 22), (-.48f, 4.8f, 1.3f, .29f, 26), (.02f, 7.6f, -.6f, .24f, 28) },
        };
        // Each band's heaved slab: (across centre as a share of the half width, width share, depth share, tilt about the line, about across).
        private static readonly (float across, float width, float depth, float roll, float pitch)[] SlabRows =
        {
            (-.04f, .34f, .46f, 6f, -7f), (.07f, .30f, .52f, -8f, -5f), (-.10f, .36f, .44f, 5f, -9f), (.05f, .32f, .50f, -6f, -6f), (0f, .40f, .48f, 7f, -10f),
        };

        // THE CUTSCENE'S BANDS, IN PLAY (owner, 2026-10-17, on the after-ult film and the cascade rebuild): each band is the same
        // row of six basalt slabs the cutscene heaved, across the band's full width, lower so players stay readable over them.
        // (across share -1 to 1, width share of a sixth, depth share, height m, tilt x, tilt z, delay s, seed). Typed per band.
        private static readonly (float across, float width, float depth, float height, float tiltX, float tiltZ, float delay, int seed)[][] LiveSlabRows =
        {
            new[] { (-.86f, .95f, .80f, .42f, -12f,  7f, .03f,  3), (-.52f, 1.05f, .88f, .52f, -16f, -5f, .00f,  5), (-.17f, .98f, .84f, .48f, -11f,  4f, .02f,  7),
                    ( .18f, 1.02f, .90f, .56f, -18f, -6f, .01f, 11), ( .53f, .96f, .82f, .44f, -13f,  8f, .04f, 13), ( .87f, 1.00f, .86f, .40f, -10f, -7f, .05f, 17) },
            new[] { (-.85f, 1.00f, .84f, .48f, -14f, -6f, .04f, 19), (-.50f, .97f, .90f, .60f, -19f,  5f, .01f, 23), (-.16f, 1.04f, .82f, .54f, -12f, -4f, .00f, 29),
                    ( .19f, .95f, .88f, .62f, -20f,  7f, .02f, 31), ( .54f, 1.03f, .84f, .50f, -15f, -8f, .03f, 37), ( .88f, .98f, .80f, .44f, -11f,  6f, .05f, 41) },
            new[] { (-.87f, .97f, .86f, .52f, -16f,  6f, .02f, 43), (-.51f, 1.02f, .82f, .64f, -21f, -7f, .03f, 47), (-.15f, .99f, .90f, .68f, -15f,  5f, .00f, 53),
                    ( .20f, 1.04f, .84f, .60f, -19f, -5f, .01f, 59), ( .55f, .96f, .88f, .56f, -16f,  8f, .04f, 61), ( .86f, 1.01f, .82f, .46f, -12f, -6f, .05f, 67) },
            new[] { (-.86f, 1.03f, .82f, .56f, -17f, -7f, .03f, 71), (-.52f, .96f, .88f, .70f, -22f,  6f, .02f, 73), (-.17f, 1.02f, .84f, .74f, -16f, -4f, .00f, 79),
                    ( .18f, .98f, .90f, .66f, -20f,  7f, .01f, 83), ( .53f, 1.04f, .82f, .60f, -17f, -8f, .04f, 89), ( .87f, .97f, .86f, .50f, -13f,  5f, .05f, 97) },
            new[] { (-.85f, .99f, .88f, .62f, -20f,  8f, .02f, 101), (-.51f, 1.04f, .84f, .78f, -24f, -6f, .01f, 103), (-.16f, .97f, .90f, .84f, -19f,  5f, .00f, 107),
                    ( .19f, 1.03f, .82f, .80f, -23f, -7f, .02f, 109), ( .54f, .98f, .88f, .70f, -20f,  7f, .03f, 113), ( .88f, 1.02f, .84f, .58f, -15f, -5f, .05f, 127) },
        };

        private static readonly Color Basalt = new Color(.31f, .27f, .24f), Dust = new Color(.42f, .32f, .22f);
        private static readonly Color MoltenHot = new Color(1f, .62f, .18f), MoltenCore = new Color(1f, .42f, .08f);

        public static DanteDriftLive Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftLive");
            root.transform.SetParent(parent, false);
            // On the court's own surface (the cutscene's trap, film r1: Bayan Plaza's court stands above the floor `GroundPoint`
            // finds, so every flat piece here, the molten light, the fuse marks and the hit edges, lay buried under it).
            var ground = VfxShapes.GroundPoint(origin);
            float court = Slipper.GroundY(origin + Vector3.up * .3f);
            if (!float.IsInfinity(court) && !float.IsNaN(court) && court > ground.y && court - ground.y < .6f) ground.y = court;
            root.transform.SetPositionAndRotation(ground, Quaternion.LookRotation(forward));
            var live = root.AddComponent<DanteDriftLive>();
            live._origin = origin; live._forward = forward; live._right = Vector3.Cross(Vector3.up, forward);
            live._halfWidth = halfWidth; live._depth = reach / GeoRules.DriftBlasts;
            live.BuildFuse();
            for (int i = 0; i < GeoRules.DriftBlasts; i++) live.BuildBand(i);
            // No beast goes under any more: the cutscene was rebuilt without it (owner, 2026-10-17, "lock inr efine it go").
            DanteDriftScar.Spawn(origin, forward, reach);
            live.StepTo(0f);
            return live;
        }

        private void BuildFuse()
        {
            var seam = VfxShapes.Lay(transform, "DriftFuse", RaggedLine(), 1, 0);
            _fuse = seam.transform;
            VfxMaterial.Ghost(seam.GetComponent<Renderer>(), new Color(MoltenHot.r, MoltenHot.g, MoltenHot.b, .95f), .7f);
            _fuseMaterial = seam.GetComponent<Renderer>().sharedMaterial;
        }

        private void BuildBand(int i)
        {
            var b = new Band { Start = i * _depth };
            var s = SlabRows[i];
            // The crack-star the fuse leaves at the band's middle: where this blast will land.
            var star = VfxShapes.Lay(transform, "DriftMark" + i, VfxShapes.Star(6, .28f, 40 + i), 1, 0);
            star.transform.localPosition = new Vector3(s.across * _halfWidth, .02f, b.Start + _depth * .5f);
            star.transform.localScale = Vector3.one * Mathf.Min(1.4f, _depth * .45f);
            VfxMaterial.Ghost(star.GetComponent<Renderer>(), new Color(MoltenCore.r, MoltenCore.g, MoltenCore.b, .9f), .6f);
            b.Star = star.transform; b.StarMaterial = star.GetComponent<Renderer>().sharedMaterial;
            // The molten light under the whole band, seen in the gaps as its slabs heave (the cutscene's band glow).
            var glow = VfxShapes.Lay(transform, "DriftUnderGlow" + i, VfxShapes.Splat(9, .18f, 60 + i), 1, 0);
            glow.transform.localPosition = new Vector3(0f, .015f, b.Start + _depth * .5f);
            glow.transform.localScale = new Vector3(_halfWidth * .98f, 1f, _depth * .34f);
            VfxMaterial.Ghost(glow.GetComponent<Renderer>(), new Color(MoltenCore.r, MoltenCore.g, MoltenCore.b, .85f), .8f);
            b.Glow = glow.transform; b.GlowMaterial = glow.GetComponent<Renderer>().sharedMaterial;
            // The band's row of six basalt slabs, as in the cutscene (film after-ult v1: one wide flat slab read as a brown mat).
            float sixth = _halfWidth * 2f / 6f;
            foreach (var r in LiveSlabRows[i])
            {
                var slab = VfxShapes.Stand(transform, "DriftHeave" + i + "_" + r.seed, VfxShapes.Prism(4, 1f, .84f, .16f, .785f, r.seed), 1);
                slab.transform.localScale = Vector3.zero;
                // Lit, faceted stone like the cutscene's (live film r21: the flat toon dress read as brown tarps).
                VfxMaterial.Solid(slab.GetComponent<Renderer>(), Basalt * (.8f + .12f * (r.seed % 3)), .2f);
                VfxRenderTag.Attach(slab);
                b.Slabs.Add(slab.transform);
            }
            // Dust standing on the band's front edge, billowing as it goes: ragged clumps, never a wall.
            for (int d = 0; d < 4; d++)
            {
                var puff = VfxShapes.Stand(transform, "DriftDust" + i + "_" + d, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 201 + i * 17 + d * 5)), 1);
                puff.transform.localScale = Vector3.zero;
                VfxMaterial.Ghost(puff.GetComponent<Renderer>(), new Color(Dust.r, Dust.g, Dust.b, .7f), .25f);
                b.Dust.Add(puff.transform); b.DustMaterials.Add(puff.GetComponent<Renderer>().sharedMaterial);
            }
            // Five chunks, typed.
            foreach (var r in ChunkRows[i])
            {
                var part = GrowthVfx.Part(transform, "DriftChunk", VfxShapes.Prism(5, 1f, .66f, .3f, 15f, r.seed), r.seed % 3 == 0 ? Dust : Basalt);
                var start = new Vector3(r.across * _halfWidth, .1f, b.Start + _depth * (.35f + .3f * ((r.seed % 5) / 4f)));
                part.transform.localPosition = start;
                part.transform.localScale = Vector3.zero;
                b.Chunks.Add(new Chunk { Node = part.transform, Start = start, Velocity = new Vector3(r.across * 1.6f, r.up, r.along), Spin = new Vector3(r.seed * 31f, 160f + r.seed * 7f, r.seed * 11f), Size = r.size });
            }
            // The hit edge: four molten cracks on the court, exactly on the band's real hit rectangle (after-ult v1: as low brown
            // walls they read as planks lying on the court; the picture still never lies about the hit area).
            for (int e = 0; e < 4; e++)
            {
                var wall = VfxShapes.Stand(transform, "DriftDustEdge" + i + "_" + e, VfxShapes.Prism(4, 1f, .9f, .12f, 0f, 90 + e), 1);
                VfxMaterial.Ghost(wall.GetComponent<Renderer>(), new Color(MoltenCore.r, MoltenCore.g, MoltenCore.b, .9f), .7f); VfxRenderTag.Attach(wall);
                bool along = e < 2;
                float x = along ? (e == 0 ? -_halfWidth : _halfWidth) : 0f;
                float z = along ? b.Start + _depth * .5f : (e == 2 ? b.Start : b.Start + _depth);
                wall.transform.localPosition = new Vector3(x, 0f, z);
                wall.transform.localRotation = Quaternion.Euler(0f, along ? 0f : 90f, 0f);
                wall.transform.localScale = new Vector3(.18f, 0f, along ? _depth : _halfWidth * 2f);
                b.Edge[e] = wall.transform;
            }
            _bands.Add(b);
        }

        public void StepTo(float age)
        {
            float bandTime = GeoRules.DriftInterval;
            // The fuse runs out ahead of everything, then cools behind the last mark.
            float fuseAt = Mathf.Min(age * FuseSpeed, _depth * GeoRules.DriftBlasts);
            float fuseCool = Mathf.Clamp01((age - .6f) / .4f);
            _fuse.localPosition = new Vector3(0f, .018f, fuseAt * .5f);
            _fuse.localScale = new Vector3(.32f, 1f, Mathf.Max(.01f, fuseAt));
            _fuse.gameObject.SetActive(fuseCool < 1f);
            _fuseMaterial.SetColor("_EmissionColor", Color.Lerp(MoltenHot, MoltenCore * .3f, fuseCool) * .9f);

            for (int i = 0; i < _bands.Count; i++)
            {
                var b = _bands[i];
                float marked = fuseAt >= b.Start + _depth * .5f ? 1f : 0f;
                float t = age - i * bandTime;                    // the band's own blast clock
                // The mark: lit as the fuse passes, pulsing, and spent when its blast goes.
                b.Star.gameObject.SetActive(marked > 0f && t < .05f);
                b.StarMaterial.SetColor("_EmissionColor", MoltenCore * (.6f + .4f * Mathf.Abs(Mathf.Sin(age * 14f))));
                // The band's slabs: each jolts up (a small overshoot), settles to rubble, holds, then sinks back into the court in
                // two crumbling steps before the wave's tail ends (stone never fades), as the cutscene's bands did.
                float sixth = _halfWidth * 2f / 6f, mid = b.Start + _depth * .5f;
                float tail = GeoRules.DriftSeconds - i * bandTime;            // this band's own time left
                for (int k = 0; k < b.Slabs.Count; k++)
                {
                    var r = LiveSlabRows[i][k];
                    float sa = t - r.delay;
                    float u = Mathf.Clamp01(sa / .08f);
                    float up = sa < 0f ? 0f : u < 1f ? u * u : 1f + .12f * Mathf.Max(0f, 1f - (sa - .08f) / .1f);
                    float settle = 1f - .45f * Mathf.Clamp01((sa - .2f) / .4f);
                    // Sinks over the last .5 s of its band's time, in two steps, each a little smaller (cracking down).
                    float sink = Mathf.Clamp01((t - (tail - .55f)) / .5f);
                    float stepDown = sink < .5f ? sink * 2f * .5f : .5f + (sink - .5f) * 2f * .5f;
                    float crack = sink < .5f ? 1f : .85f;
                    // The band at his feet stays low, so his own first-person view keeps the lane ahead (live film r23).
                    float h = r.height * (i == 0 ? .5f : 1f);
                    var slab = b.Slabs[k];
                    slab.gameObject.SetActive(sa >= 0f && sink < 1f);
                    slab.localPosition = new Vector3(r.across * (_halfWidth - sixth * .5f), -h * .5f + h * .8f * up * settle - h * stepDown,
                                                     mid + ((r.seed % 5) - 2) * .1f * _depth);
                    slab.localRotation = Quaternion.Euler(r.tiltX * Mathf.Min(1f, up), (r.seed % 7 - 3) * 5f, r.tiltZ * Mathf.Min(1f, up));
                    // Lumps with gaps between them (r21: full-size they closed into one mat), so the molten light shows.
                    slab.localScale = new Vector3(sixth * r.width * .55f, h, _depth * r.depth * .5f) * crack;
                }
                // Dust on the band's front edge: billowing up and out, thinning (dust may thin; stone may not).
                for (int d = 0; d < b.Dust.Count; d++)
                {
                    float du = Mathf.Clamp01(t / .9f);
                    float across = (d / 3f * 2f - 1f) * (_halfWidth - 1f);
                    // Low and thin (r21: tall clumps a few metres ahead painted a dark band over his first-person view).
                    float puff = Mathf.Lerp(.6f, 1.4f, du) * (.8f + .1f * d);
                    var dust = b.Dust[d];
                    dust.gameObject.SetActive(t >= 0f && du < 1f);
                    dust.localPosition = new Vector3(across, puff * .5f + du * .4f, b.Start + _depth + .1f);
                    dust.localRotation = Quaternion.Euler(90f, 0f, (d - 1.5f) * 9f);
                    dust.localScale = new Vector3(puff * 1.3f, 1f, puff);
                    var c = new Color(Dust.r, Dust.g, Dust.b, .45f * (1f - du * du));
                    b.DustMaterials[d].color = c; if (b.DustMaterials[d].HasProperty("_BaseColor")) b.DustMaterials[d].SetColor("_BaseColor", c);
                }
                b.Glow.gameObject.SetActive(t >= 0f && t < tail - .1f);
                b.GlowMaterial.SetColor("_EmissionColor", Color.Lerp(MoltenHot, MoltenCore * .35f, Mathf.Clamp01((t - .2f) / .9f)));
                for (int c = 0; c < b.Chunks.Count; c++)
                {
                    var ch = b.Chunks[c];
                    float u = Mathf.Max(0f, t);
                    var p = ch.Start + ch.Velocity * u + Vector3.down * (.5f * 9.81f * 1.3f * u * u);
                    bool landed = p.y < 0f;
                    if (landed) p.y = 0f;
                    ch.Node.gameObject.SetActive(t >= 0f && t < 1.2f);
                    ch.Node.localPosition = p;
                    ch.Node.localRotation = Quaternion.Euler(ch.Spin * Mathf.Min(u, .9f));
                    float broken = !landed ? 1f : t < .85f ? .7f : Mathf.Max(0f, .7f - (t - .85f) * 2f);
                    ch.Node.localScale = Vector3.one * ch.Size * broken;
                }
                // The hit edge: molten cracks opening on the court along the hit rectangle as the band goes, then closing.
                float edge = t < 0f ? 0f : Mathf.Clamp01(t / .1f) * (1f - Mathf.Clamp01((t - .7f) / .4f));
                foreach (var e in b.Edge)
                {
                    var sc = e.localScale; sc.x = .14f; sc.y = .035f * edge; e.localScale = sc;
                    e.gameObject.SetActive(edge > .01f);
                }
            }
            // Map-wide, on this viewer's own camera: once per blast as its clock passes zero.
            if (age >= _lastAge)
                for (int i = _shookUpTo + 1; i < _bands.Count && age >= i * bandTime; i++) { Quake(i); _shookUpTo = i; }
            _lastAge = age;
        }

        private void Quake(int band)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rig = cam.GetComponent<CameraRig>();
            Vector3 centre = transform.TransformPoint(new Vector3(0f, 0f, _bands[band].Start + _depth * .5f));
            float distance = Vector3.Distance(new Vector3(cam.transform.position.x, 0f, cam.transform.position.z), new Vector3(centre.x, 0f, centre.z));
            float falloff = Mathf.Lerp(1f, .35f, Mathf.Clamp01(distance / 18f)) * (band == GeoRules.DriftBlasts - 1 ? 1.25f : 1f);
            if (rig != null) { rig.Shake(.32f * falloff, .28f); rig.BeginGroundRumble(.55f * falloff); }
            if (Settings.SettingsStore.Current.ReducedEffects) return;
            // A few grains through this viewer's own view, from above.
            var ahead = cam.transform.position + cam.transform.forward * 1.6f + Vector3.up * 1.1f;
            DanteBoulderGrit.Drop(ahead + cam.transform.right * (-.5f + .3f * band % 3), false);
            DanteBoulderGrit.Drop(ahead + cam.transform.right * (.45f - .2f * (band % 2)) + cam.transform.forward * .4f, false);
        }

        // A flat crack one unit long (z 0 to 1, centred on z .5 so its scale grows from the middle), its own typed wander
        // and width per point: one dominant line, never a straight bar.
        private static readonly (float x, float z, float w)[] Ragged =
        {
            (0f, -.5f, .10f), (.30f, -.41f, .55f), (-.22f, -.30f, .80f), (.18f, -.17f, 1f), (-.35f, -.05f, .85f), (.12f, .08f, .95f),
            (.40f, .19f, .70f), (-.15f, .31f, .90f), (.22f, .42f, .60f), (0f, .5f, .12f),
        };
        internal static Mesh RaggedLine()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < Ragged.Length; i++)
            {
                var r = Ragged[i];
                v.Add(new Vector3(r.x - .5f * r.w, 0f, r.z)); v.Add(new Vector3(r.x + .5f * r.w, 0f, r.z));
                if (i == 0) continue;
                int n = v.Count;
                t.AddRange(new[] { n - 4, n - 2, n - 3, n - 3, n - 2, n - 1 });
            }
            var m = new Mesh { name = "Drift ragged line" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        public int MarkCount { get { int n = 0; foreach (var b in _bands) if (b.Star.gameObject.activeSelf) n++; return n; } }
        public Bounds DustEdgeBounds(int band)
        {
            var b = _bands[band];
            var bounds = new Bounds(b.Edge[0].position, Vector3.zero);
            foreach (var e in b.Edge) bounds.Encapsulate(e.position);
            return bounds;
        }
    }

    /// <summary>
    /// The fault line Continental Drift leaves: it outlives the wave by 3 s, cools from molten through dull red to grey, then
    /// crumbles flat from his end outward. Its own clock, as nothing gameplay reads it.
    /// </summary>
    public sealed class DanteDriftScar : MonoBehaviour
    {
        private const float Life = GeoRules.DriftSeconds + 3f;
        private Material _material;
        private float _age, _length;
        private Transform _line;

        public static void Spawn(Vector3 origin, Vector3 forward, float reach)
        {
            var go = new GameObject("~ContinentalDriftScar");
            go.transform.SetPositionAndRotation(VfxShapes.GroundPoint(origin) + Vector3.up * .021f, Quaternion.LookRotation(forward));
            var scar = go.AddComponent<DanteDriftScar>();
            scar._length = reach;
            var line = VfxShapes.Lay(go.transform, "ScarLine", DanteDriftLive.RaggedLine(), 1, 0);
            scar._line = line.transform;
            VfxMaterial.Ghost(line.GetComponent<Renderer>(), new Color(1f, .42f, .08f, .9f), .5f);
            scar._material = line.GetComponent<Renderer>().sharedMaterial;
            scar.Step();
        }

        private void Update() { _age += Time.deltaTime; Step(); if (_age > Life) Destroy(gameObject); }

        private void Step()
        {
            float grown = Mathf.Clamp01(_age / ((GeoRules.DriftBlasts - 1) * GeoRules.DriftInterval + .1f));
            float cool = Mathf.Clamp01((_age - GeoRules.DriftSeconds) / 2f);
            float crumble = Mathf.Clamp01((_age - (Life - 1f)) / 1f);
            Color c = cool < .5f ? Color.Lerp(new Color(1f, .42f, .08f), new Color(.55f, .16f, .06f), cool * 2f)
                                 : Color.Lerp(new Color(.55f, .16f, .06f), new Color(.22f, .21f, .20f), cool * 2f - 1f);
            _material.SetColor("_EmissionColor", c * (1f - .7f * cool));
            _material.color = new Color(c.r, c.g, c.b, .9f);
            // Crumbles flat: narrows to nothing, his end first (the line's start shrinks toward its far end).
            float len = _length * grown * (1f - crumble);
            _line.localPosition = new Vector3(0f, 0f, _length * grown - len * .5f);
            _line.localScale = new Vector3(.55f * (1f - crumble * .6f), 1f, Mathf.Max(.01f, len));
        }
    }
}
