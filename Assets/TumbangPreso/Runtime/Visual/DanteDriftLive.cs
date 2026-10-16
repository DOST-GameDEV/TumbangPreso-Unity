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
    ///  * THE GOING UNDER (0 to .4 s): the mountain beast that carried him in the cutscene sinks into the court behind and under
    ///    him, rock sliding off its back (the owner, 2026-10-08: it disappears into the ground in live play, not in the cutscene).
    ///  * THE FUSE: a molten seam races forward from him along the cascade's line, far faster than the blasts, and leaves a
    ///    glowing crack-star at the middle of every band before that band goes, so a victim can read it and step out.
    ///  * FIVE BLASTS, one per band every 0.30 s: a slab of court heaves 0.35 m in one jolt and tilts, molten light under it,
    ///    five big chunks thrown (typed per band, no two alike), and a DUST EDGE that rises exactly along the band's real hit
    ///    rectangle (full width x band depth): the picture never lies about the hit area.
    ///  * MAP-WIDE: every viewer's own camera shakes by its distance to the blast, the ground rumbles, and a few grains fall
    ///    through their own view from above.
    ///  * THE SCAR: the fault line stays after the wave, cools orange, dull red, grey, then crumbles flat (`DanteDriftScar`).
    /// </summary>
    public sealed class DanteDriftLive : MonoBehaviour, IVfxTimeline
    {
        public float LifeSeconds => GeoRules.DriftSeconds;
        private const float FuseSpeed = 34f;          // m/s along the line: it reaches the far band long before its blast
        private const float HeaveLift = .35f;

        private Vector3 _origin, _forward, _right;
        private float _halfWidth, _depth;
        private readonly List<Band> _bands = new List<Band>(GeoRules.DriftBlasts);
        private Transform _fuse;
        private Material _fuseMaterial;
        private GameObject _beast;
        private Transform _beastRoot;
        private int _shookUpTo = -1;
        private float _lastAge = -1f;

        private sealed class Band
        {
            public float Start;            // metres along the line
            public Transform Slab, Star, Glow;
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

        private static readonly Color Basalt = new Color(.31f, .27f, .24f), Dust = new Color(.42f, .32f, .22f);
        private static readonly Color MoltenHot = new Color(1f, .62f, .18f), MoltenCore = new Color(1f, .42f, .08f);

        public static DanteDriftLive Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftLive");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(VfxShapes.GroundPoint(origin), Quaternion.LookRotation(forward));
            var live = root.AddComponent<DanteDriftLive>();
            live._origin = origin; live._forward = forward; live._right = Vector3.Cross(Vector3.up, forward);
            live._halfWidth = halfWidth; live._depth = reach / GeoRules.DriftBlasts;
            live.BuildFuse();
            for (int i = 0; i < GeoRules.DriftBlasts; i++) live.BuildBand(i);
            live.BuildBeast();
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
            // The heaved slab: a piece of court that jolts up and tilts; the light under it is a flat molten pool beneath.
            var glow = VfxShapes.Lay(transform, "DriftUnderGlow" + i, VfxShapes.Splat(9, .18f, 60 + i), 1, 0);
            glow.transform.localPosition = new Vector3(s.across * _halfWidth, .015f, b.Start + _depth * .5f);
            glow.transform.localScale = new Vector3(s.width * _halfWidth * 2.3f, 1f, s.depth * _depth * 1.3f);
            VfxMaterial.Ghost(glow.GetComponent<Renderer>(), new Color(MoltenCore.r, MoltenCore.g, MoltenCore.b, .85f), .8f);
            b.Glow = glow.transform; b.GlowMaterial = glow.GetComponent<Renderer>().sharedMaterial;
            var slab = VfxShapes.Stand(transform, "DriftHeave" + i, VfxShapes.Prism(6, .3f, .94f, .14f, 0f, 80 + i), 1);
            slab.transform.localPosition = glow.transform.localPosition;
            slab.transform.localScale = new Vector3(s.width * _halfWidth * 2f, 1.1f, s.depth * _depth);
            MaterialKit.Dress(slab.GetComponent<Renderer>(), Basalt);
            ToonSkin.Apply(slab.GetComponent<Renderer>(), .008f); VfxRenderTag.Attach(slab);
            b.Slab = slab.transform;
            // Five chunks, typed.
            foreach (var r in ChunkRows[i])
            {
                var part = GrowthVfx.Part(transform, "DriftChunk", VfxShapes.Prism(5, 1f, .66f, .3f, 15f, r.seed), r.seed % 3 == 0 ? Dust : Basalt);
                var start = new Vector3(r.across * _halfWidth, .1f, b.Start + _depth * (.35f + .3f * ((r.seed % 5) / 4f)));
                part.transform.localPosition = start;
                part.transform.localScale = Vector3.zero;
                b.Chunks.Add(new Chunk { Node = part.transform, Start = start, Velocity = new Vector3(r.across * 1.6f, r.up, r.along), Spin = new Vector3(r.seed * 31f, 160f + r.seed * 7f, r.seed * 11f), Size = r.size });
            }
            // The dust edge: four low walls standing exactly on the band's real hit rectangle.
            for (int e = 0; e < 4; e++)
            {
                var wall = VfxShapes.Stand(transform, "DriftDustEdge" + i + "_" + e, VfxShapes.Prism(4, 1f, .55f, .12f, 0f, 90 + e), 1);
                MaterialKit.Dress(wall.GetComponent<Renderer>(), Dust); VfxRenderTag.Attach(wall);
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

        private void BuildBeast()
        {
            _beastRoot = new GameObject("DriftBeastGoingUnder").transform;
            _beastRoot.SetParent(transform, false);
            _beastRoot.localPosition = new Vector3(0f, 0f, -1.2f);
            _beast = DanteProp.Spawn("beast", _beastRoot);
            if (_beast != null) { DanteProp.SetGlow(_beast, "glow-eyes", 1f); DanteProp.SetGlow(_beast, "glow-seams", .8f); }
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
                // The heave: a jolt up in .06 s, a held tilt, then it drops back over the tail.
                float jolt = t < 0f ? 0f : Mathf.Clamp01(t / .06f);
                float drop = Mathf.Clamp01((t - .55f) / .35f);
                float lift = HeaveLift * jolt * (1f - drop * drop);
                var s = SlabRows[i];
                b.Slab.gameObject.SetActive(t >= 0f && t < 1.2f);
                b.Slab.localPosition = new Vector3(s.across * _halfWidth, lift - .3f * 1.1f * (1f - jolt), b.Start + _depth * .5f);
                b.Slab.localRotation = Quaternion.Euler(s.pitch * jolt * (1f - drop), 0f, s.roll * jolt * (1f - drop));
                b.Glow.gameObject.SetActive(t >= 0f && t < 1.0f);
                b.GlowMaterial.SetColor("_EmissionColor", Color.Lerp(MoltenHot, MoltenCore * .25f, Mathf.Clamp01((t - .2f) / .7f)));
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
                // The dust edge rises along the hit rectangle and sinks back.
                float dust = t < 0f ? 0f : Mathf.Clamp01(t / .12f) * (1f - Mathf.Clamp01((t - .5f) / .5f));
                foreach (var e in b.Edge)
                {
                    var sc = e.localScale; sc.y = .55f * dust; e.localScale = sc;
                    e.gameObject.SetActive(dust > .01f);
                }
            }
            // The beast goes under: down through the court in .4 s, jolting, rock sliding off its back.
            if (_beastRoot != null)
            {
                float under = Mathf.Clamp01(age / .4f);
                float jolts = under < .33f ? under * 1.2f : under < .66f ? .4f + (under - .33f) * 1.4f : .86f + (under - .66f) * .42f;
                _beastRoot.localPosition = new Vector3(0f, -4.6f * jolts, -1.2f);
                _beastRoot.gameObject.SetActive(age < .45f);
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
