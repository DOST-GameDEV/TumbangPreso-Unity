using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CONTINENTAL DRIFT's cascade in play (`docs/reports/dante-presentation-2026-10-16/direction.md` section 3). The cutscene is
    /// his charge-up and ends as he slams down (owner, 2026-10-17: *"after nna malaunch ung ult, end nna dapat cutscene tapos ppl
    /// see the rock going towards them and maknoknnock away sila and concuss"*); this is what everyone then sees, from their own
    /// first person, posed from the wave's accepted age (`StepTo`) so a rejoiner and a replay draw the same frame:
    ///  * THE FUSE: a molten crack races out from his fists down the middle of the lane, just ahead of the bands.
    ///  * FIVE BANDS, one every `GeoRules.DriftInterval`: a row of basalt slabs heaves up across the band's full width (the hit
    ///    area), molten light in the gaps, dust at its front, rocks thrown; whoever stands on it is knocked away (`DanteDriftWave`).
    ///  * THE RECEDE (owner on v8: *"they all js recede in unison and disappear"*, *"how its done rnn looks so bad"*): band by band
    ///    behind the wave, each slab on its own beat. It cracks with a shudder, tips toward the lane's middle, breaks into three
    ///    pieces that slump into the court, and dust settles where it stood; the band's seams flare once as it breaks. Stone
    ///    never fades: it goes into the court.
    ///  * Every viewer's own camera shakes by its distance to each band, and a few grains fall through their view.
    /// The look is the one the owner approved in the cutscene (*"i like how ult cutscene looks liek rn"*, *"i js want actual game
    /// to match it"*): its slab table, lit basalt with a fill, the soft molten glow. Nothing here runs on `Update`; it never
    /// consumes gameplay random state; the hottest colour is (1, .62, .18).
    /// </summary>
    public sealed class DanteDriftLive : MonoBehaviour, IVfxTimeline
    {
        public float LifeSeconds => GeoRules.DriftSeconds;

        public static float BandAt(int k) => k * GeoRules.DriftInterval;
        private static float LastBand => BandAt(GeoRules.DriftBlasts - 1);
        // THE RECEDE: each band starts breaking on its own beat, the first while the last still heaves (a ripple behind the wave);
        // each slab waits up to `SlabWait` more (the middle first), then takes `SlabSink` to go into the court. The last band is
        // gone at 2.8 s, before the wave's own end (3.0).
        private static float SinkAt(int k) => BandAt(k) * .6f + 1.3f;
        private const float SlabWait = .18f, SlabSink = .6f;
        private static float BandGoneAt(int k) => SinkAt(k) + SlabWait + SlabSink;
        private const float FuseLead = .12f;          // the fuse reaches each band this long before it heaves

        // Each band's slabs across the lane: (across share -1 to 1, width share of a column, depth share, height m, tilt x, tilt z,
        // delay s, seed). Typed per band, no two bands alike. The cutscene's table, the only one. A lane wider than the cutscene's
        // first one (9 m) gets more columns of the same size, the table repeated with new seeds (film r30: six columns across the
        // court's 17 m made 2.7 m plates that read as walls).
        private const float ColumnWidth = 1.5f;
        private static readonly (float across, float width, float depth, float height, float tiltX, float tiltZ, float delay, int seed)[][] SlabRows =
        {
            new[] { (-.86f, .95f, .82f, .62f, -9f,  6f, .03f,  3), (-.52f, 1.05f, .90f, .78f, -12f, -4f, .00f,  5), (-.17f, .98f, .86f, .70f, -8f,  3f, .02f,  7),
                    ( .18f, 1.02f, .92f, .84f, -14f, -5f, .01f, 11), ( .53f, .96f, .84f, .66f, -10f,  7f, .04f, 13), ( .87f, 1.00f, .88f, .58f, -7f, -6f, .05f, 17) },
            new[] { (-.85f, 1.00f, .86f, .72f, -11f, -5f, .04f, 19), (-.50f, .97f, .92f, .90f, -15f,  4f, .01f, 23), (-.16f, 1.04f, .84f, .80f, -9f, -3f, .00f, 29),
                    ( .19f, .95f, .90f, .96f, -16f,  6f, .02f, 31), ( .54f, 1.03f, .86f, .74f, -12f, -7f, .03f, 37), ( .88f, .98f, .82f, .64f, -8f,  5f, .05f, 41) },
            new[] { (-.87f, .97f, .88f, .80f, -13f,  5f, .02f, 43), (-.51f, 1.02f, .84f, .98f, -17f, -6f, .03f, 47), (-.15f, .99f, .92f, 1.06f, -12f,  4f, .00f, 53),
                    ( .20f, 1.04f, .86f, .92f, -15f, -4f, .01f, 59), ( .55f, .96f, .90f, .84f, -13f,  7f, .04f, 61), ( .86f, 1.01f, .84f, .70f, -9f, -5f, .05f, 67) },
            new[] { (-.86f, 1.03f, .84f, .88f, -14f, -6f, .03f, 71), (-.52f, .96f, .90f, 1.10f, -18f,  5f, .02f, 73), (-.17f, 1.02f, .86f, 1.18f, -13f, -3f, .00f, 79),
                    ( .18f, .98f, .92f, 1.04f, -16f,  6f, .01f, 83), ( .53f, 1.04f, .84f, .94f, -14f, -7f, .04f, 89), ( .87f, .97f, .88f, .78f, -10f,  4f, .05f, 97) },
            new[] { (-.85f, .99f, .90f, 1.00f, -16f,  7f, .02f, 101), (-.51f, 1.04f, .86f, 1.26f, -20f, -5f, .01f, 103), (-.16f, .97f, .92f, 1.36f, -15f,  4f, .00f, 107),
                    ( .19f, 1.03f, .84f, 1.30f, -19f, -6f, .02f, 109), ( .54f, .98f, .90f, 1.12f, -16f,  6f, .03f, 113), ( .88f, 1.02f, .86f, .92f, -12f, -4f, .05f, 127) },
        };
        // Rocks each band throws: (across share, up speed, forward speed, size, seed). Five per band.
        private static readonly (float across, float up, float along, float size, int seed)[] RockRows =
        {
            (-.62f, 5.2f, 1.6f, .26f, 2), (-.20f, 6.4f, 2.2f, .32f, 4), (.14f, 5.8f, 1.2f, .22f, 6), (.48f, 6.8f, 2.6f, .28f, 8), (.80f, 4.9f, 1.8f, .24f, 10),
        };

        private static readonly Color[] Basalt = { new Color(.29f, .24f, .2f, 1f), new Color(.34f, .29f, .25f, 1f), new Color(.24f, .2f, .17f, 1f) };
        private static readonly Color DustColour = new Color(.5f, .37f, .24f, .72f);
        private static readonly Color MoltenCore = new Color(1f, .42f, .08f), MoltenFuse = new Color(1f, .5f, .1f);
        private static readonly int ColourId = Shader.PropertyToID("_Color"), FacingId = Shader.PropertyToID("_Facing");

        private float _halfWidth, _reach, _depth;
        private int _shookUpTo = -1;
        private float _lastAge = -1f;
        private Glow _fuse;
        private readonly List<Band> _bands = new List<Band>(GeoRules.DriftBlasts);

        private sealed class Band
        {
            public Transform Root;
            public float Start, End;                     // metres along the lane, as drawn (the first starts ahead of his fists)
            public readonly List<int> Rows = new List<int>(12);   // each slab's row in its band's table
            public readonly List<Transform> Slabs = new List<Transform>(12), Rocks = new List<Transform>(5), Dust = new List<Transform>(4);
            public readonly List<Transform[]> Pieces = new List<Transform[]>(12);   // what each slab breaks into
            public readonly List<Material> DustMaterials = new List<Material>(4);
            public Glow Light;
        }
        internal sealed class Glow
        {
            public Transform Node; public Renderer Renderer; public MaterialPropertyBlock Block; public Color Colour;
        }

        /// <summary>Play: the real lane, on the court's own surface, continuing the cutscene's clock.</summary>
        public static DanteDriftLive Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftLive");
            root.transform.SetParent(parent, false);
            // On the court's own surface (film r1: Bayan Plaza's court stands above the floor `GroundPoint` finds).
            var ground = VfxShapes.GroundPoint(origin);
            float court = Slipper.GroundY(origin + Vector3.up * .3f);
            if (!float.IsInfinity(court) && !float.IsNaN(court) && court > ground.y && court - ground.y < .6f) ground.y = court;
            root.transform.SetPositionAndRotation(ground, Quaternion.LookRotation(forward));
            var live = root.AddComponent<DanteDriftLive>();
            live.Make(halfWidth, reach);
            live.StepTo(0f);
            return live;
        }

        /// <summary>The cutscene: the same build inside its stage (his feet at the stage's origin, forward along +z), posed with
        /// the same `StepTo` on the cutscene's clock; play's wave picks up where its last frame left off.</summary>
        public static DanteDriftLive BuildStage(Transform stage, float courtY, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftStage");
            root.transform.SetParent(stage, false);
            root.transform.localPosition = new Vector3(0f, courtY, 0f);
            var live = root.AddComponent<DanteDriftLive>();
            live._stage = true;
            live.Make(halfWidth, reach);
            live.StepTo(0f);
            return live;
        }

        private bool _stage;

        // THE STRIKE: a basalt column punching up out of the court under a player the band reaches, twice the band's height so it
        // stands out of the slabs (owner on v8: *"It doesnt feel like it hits that hard for anyone"*). Three stacked chunks with
        // molten light in the seams between them, a molten star and a ring racing out under it, shards and dust bursting off it.
        // It holds, then comes apart from the top: the two upper chunks topple off and the base sinks into the court.
        private sealed class Strike
        {
            public float At, Yaw;
            public Vector3 Local;
            public readonly Transform[] Chunks = new Transform[3];
            public readonly Glow[] Core = new Glow[2];
            public Glow Star, Ring;
            public readonly List<Transform> Shards = new List<Transform>(10);
            public readonly List<Transform> Dust = new List<Transform>(5);
            public readonly List<Material> DustMaterials = new List<Material>(5);
        }
        private readonly List<Strike> _strikes = new List<Strike>(4);
        private const float PillarHeight = 2.3f, StrikeLife = 1.35f, ChunkGap = .07f;
        // The column, base up: (share of its height, footprint radius m, the way it falls in degrees round it, when it goes s).
        private static readonly (float height, float width, float fall, float goes)[] ChunkRows =
        {
            (.46f, .78f, 0f, .9f), (.32f, .66f, 140f, .74f), (.22f, .54f, -65f, .58f),
        };
        private static readonly (float angle, float up, float out_, float size)[] ShardRows =
        {
            (20f, 7.5f, 2.9f, .26f), (60f, 8.6f, 2.2f, .18f), (95f, 8.2f, 2.4f, .2f), (130f, 6.6f, 3.4f, .24f), (160f, 6.8f, 3.1f, .28f),
            (200f, 9.0f, 1.9f, .16f), (225f, 7.9f, 2.7f, .22f), (265f, 7.0f, 3.2f, .2f), (300f, 7.1f, 3.4f, .25f), (350f, 8.8f, 2.0f, .18f),
        };

        /// <summary>
        /// The ground strikes the player at <paramref name="world"/>: posed from the wave's clock like everything else. The one
        /// struck feels it on their own camera: a hard punch and a shake (owner, 2026-10-17: *"TRY TO make it more felt"*).
        /// </summary>
        public void Struck(Vector3 world, float at, bool viewed)
        {
            int n = _strikes.Count, seed = 600 + n * 13;
            var s = new Strike { At = at, Local = transform.InverseTransformPoint(world), Yaw = (seed % 7) * 23f };
            s.Local.y = 0f;
            for (int c = 0; c < ChunkRows.Length; c++)
                s.Chunks[c] = Stone(transform, "DriftStrike" + n + "_" + c, VfxShapes.Prism(5, 1f, .8f, .22f, 0f, seed + 20 + c), Basalt[(n + c) % 3]);
            // The molten core: two crossed sheets of light up the column, just inside its stone, so it shows only in the seams.
            for (int i = 0; i < 2; i++) s.Core[i] = AddGlow(transform, "DriftStrikeCore" + n + "_" + i, MoltenCore, 1.3f, .75f);
            s.Star = AddGlow(transform, "DriftStrikeStar" + n, MoltenCore, 1.4f, .6f);
            s.Star.Renderer.sharedMaterial.SetFloat("_Band", 0f);
            s.Ring = AddGlow(transform, "DriftStrikeRing" + n, MoltenFuse, 1.2f, .4f, RingMesh());
            for (int i = 0; i < ShardRows.Length; i++)
                s.Shards.Add(Stone(transform, "DriftStrikeShard" + i, VfxShapes.Prism(5, 1f, .7f, .3f, 0f, seed + i + 1), Basalt[(i + 1) % 3]));
            for (int i = 0; i < 5; i++)
            {
                var puff = VfxShapes.Stand(transform, "DriftStrikeDust" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, seed + 40 + i)), 1);
                var r = puff.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                VfxMaterial.Ghost(r, DustColour, .3f);
                puff.transform.localScale = Vector3.zero;
                s.Dust.Add(puff.transform); s.DustMaterials.Add(r.sharedMaterial);
            }
            _strikes.Add(s);
            if (viewed && !_stage)
            {
                var cam = Camera.main;
                var rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                // Their own screen takes the hit (`DanteHitLens`), and broken stone bursts up past their eyes.
                DanteHitLens.Begin(cam, n);
                if (cam != null && !Settings.SettingsStore.Current.ReducedEffects)
                    for (int i = 0; i < 6; i++)
                        DanteBoulderGrit.Drop(cam.transform.position + cam.transform.forward * (.7f + .1f * i) + cam.transform.right * ((i % 3) - 1) * .35f
                                              + Vector3.down * .2f, true);
                // Up and away with them, hard (owner on v7: *"TRY TO make it more felt"*; on v8: *"It doesnt feel like it hits that
                // hard"*). Play is never paused for a hit-stop: the round is shared, so the lens takes the stop.
                if (rig != null) { rig.ImpactPunch(transform.forward + Vector3.up * .7f, 2.6f); rig.Shake(.85f, .5f); rig.BeginGroundRumble(1f); }
            }
            PoseStrike(s, _lastAge);
        }

        private void PoseStrike(Strike s, float t)
        {
            float a = t - s.At;
            bool on = a >= 0f && a < StrikeLife;
            // Out of the court in a punch (0.06 s) with an overshoot, held, then apart from the top.
            float up = a < 0f ? 0f : a < .06f ? a / .06f : 1f + .18f * Mathf.Max(0f, 1f - (a - .06f) / .1f);
            float bottom = -.25f - PillarHeight * (1f - Mathf.Min(1f, up)) + PillarHeight * .1f * (up - 1f);
            var spin = Quaternion.Euler(0f, s.Yaw, 0f);
            for (int c = 0; c < ChunkRows.Length; c++)
            {
                var row = ChunkRows[c];
                float h = row.height * PillarHeight, fa = a - row.goes;
                var pos = s.Local + Vector3.up * bottom;
                var turn = spin * Quaternion.Euler(-5f + 4f * c, 25f * c, 4f - 3f * c);
                if (c == 0)
                {
                    // The base shudders and sinks back into the court.
                    pos += Vector3.down * (h + .35f) * Ease(0f, .4f, fa);
                    turn = Quaternion.Euler(fa > 0f && fa < .15f ? Mathf.Sin(fa * 120f) * 3f : 0f, 0f, 0f) * turn;
                }
                else
                {
                    // The upper chunks topple off their own way, then slump into the court.
                    float topple = Ease(0f, .3f, fa), slump = Ease(.15f, .45f, fa);
                    float way = (row.fall + s.Yaw) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(way), 0f, Mathf.Cos(way));
                    pos += dir * (.5f * h + .3f * c) * topple;
                    turn = Quaternion.AngleAxis(75f * topple, Vector3.Cross(Vector3.up, dir)) * turn;
                    pos += Vector3.down * (bottom + h * (1.2f + c) + .4f) * slump;
                }
                bottom += h + ChunkGap;
                var chunk = s.Chunks[c];
                chunk.gameObject.SetActive(on && up > 0f && fa < .45f);
                chunk.localPosition = pos;
                chunk.localRotation = turn;
                chunk.localScale = new Vector3(row.width, h, row.width);
            }
            // The core burns orange through the seams at the punch and is out as the top chunk goes (film r47: left burning, it
            // stood on as a column of light with no stone round it).
            float core = !on ? 0f : (1.4f + 1.4f * (1f - Ease(.06f, .35f, a))) * (1f - Ease(ChunkRows[2].goes - .15f, ChunkRows[2].goes + .02f, a));
            float coreBase = -.25f - PillarHeight * (1f - Mathf.Min(1f, up));
            for (int i = 0; i < 2; i++)
                Light(s.Core[i], s.Local + Vector3.up * (coreBase + PillarHeight * .55f), new Vector3(PillarHeight * 1.1f, 1.05f, 1f),
                      Quaternion.Euler(0f, s.Yaw + 45f + 90f * i, 90f), core);
            Light(s.Star, s.Local + Vector3.up * .02f, Vector3.one * Mathf.Lerp(.8f, 3.4f, Mathf.Clamp01(a / .1f)), Quaternion.Euler(90f, 0f, 0f),
                  !on ? 0f : 2.6f * (1f - Ease(.1f, 1f, a)));
            float ring = Mathf.Lerp(.4f, 3.6f, Ease(0f, .32f, a));
            Light(s.Ring, s.Local + Vector3.up * .03f, new Vector3(ring, 1f, ring), Quaternion.identity, !on ? 0f : 1.8f * (1f - Mathf.Clamp01(a / .36f)));
            for (int i = 0; i < s.Shards.Count; i++)
            {
                var r = ShardRows[i];
                float ang = r.angle * Mathf.Deg2Rad, land = 2f * r.up / 9.8f, ft = Mathf.Clamp(a, 0f, land);
                var pos = s.Local + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * (.3f + r.out_ * ft) + Vector3.up * (.6f + r.up * ft - 4.9f * ft * ft);
                pos.y = Mathf.Max(0f, pos.y);
                float broken = a < land ? 1f : a < land + .2f ? .7f : Mathf.Max(0f, .7f - (a - land - .2f) * 3f);
                s.Shards[i].gameObject.SetActive(on && a >= 0f && broken > 0f);
                s.Shards[i].localPosition = pos;
                s.Shards[i].localRotation = Quaternion.Euler(ft * 500f + i * 40f, i * 60f, ft * 300f);
                s.Shards[i].localScale = Vector3.one * r.size * broken;
            }
            for (int i = 0; i < s.Dust.Count; i++)
            {
                float du = Mathf.Clamp01(a / .9f), ang = i * 1.257f + .4f;
                float puff = Mathf.Lerp(.7f, 2.3f, du);
                var d = s.Dust[i];
                d.gameObject.SetActive(on && a >= 0f && du < 1f);
                d.localPosition = s.Local + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * (.5f + 1.6f * du) + Vector3.up * (puff * .45f);
                d.localRotation = Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f) * Quaternion.Euler(90f, 0f, 0f);
                d.localScale = new Vector3(puff * 1.2f, 1f, puff);
                var c = DustColour; c.a = .8f * (1f - du * du);
                s.DustMaterials[i].color = c; if (s.DustMaterials[i].HasProperty("_BaseColor")) s.DustMaterials[i].SetColor("_BaseColor", c);
            }
        }
        private int Columns => Mathf.Max(6, Mathf.RoundToInt(_halfWidth * 2f / ColumnWidth));
        public float HalfWidth => _halfWidth;
        public float Reach => _reach;
        public float BandDepth => _depth;
        /// <summary>Where the molten crack has reached down the lane at this age: each band's middle just before it heaves.</summary>
        private float Front(float t)
        {
            float from = .4f;
            for (int k = 0; k < _bands.Count; k++)
            {
                float reach = (_bands[k].Start + _bands[k].End) * .5f, at = Mathf.Max(0f, BandAt(k) - FuseLead);
                float prevAt = k == 0 ? 0f : Mathf.Max(0f, BandAt(k - 1) - FuseLead), prev = k == 0 ? from : (_bands[k - 1].Start + _bands[k - 1].End) * .5f;
                if (t <= at) return k == 0 ? Mathf.Lerp(from, reach, t / Mathf.Max(.01f, at)) : Mathf.Lerp(prev, reach, (t - prevAt) / Mathf.Max(.01f, at - prevAt));
            }
            var last = _bands[_bands.Count - 1];
            return Mathf.Lerp((last.Start + last.End) * .5f, last.End, Mathf.Clamp01((t - LastBand + FuseLead) / .25f));
        }

        private void Make(float halfWidth, float reach)
        {
            _halfWidth = Mathf.Max(.5f, halfWidth); _reach = Mathf.Max(1f, reach); _depth = _reach / GeoRules.DriftBlasts;
            _fuse = AddGlow(transform, "DriftFuse", MoltenFuse, 1.6f, .7f);
            for (int k = 0; k < GeoRules.DriftBlasts; k++)
            {
                var b = new Band { Start = k * _depth, End = (k + 1) * _depth };
                // The first band's slabs start ahead of his fists, never under his own body.
                if (k == 0) b.Start = Mathf.Min(1.4f, _depth * .45f);
                b.Root = new GameObject("DriftBand" + k).transform;
                b.Root.SetParent(transform, false);
                // The molten light under the band, seen in the gaps as its slabs heave.
                b.Light = AddGlow(b.Root, "DriftBandGlow" + k, MoltenCore, .8f, .7f);
                for (int c = 0; c < Columns; c++)
                {
                    var s = SlabRows[k][c % 6];
                    int seed = s.seed + 131 * (c / 6);
                    b.Rows.Add(c % 6);
                    b.Slabs.Add(Stone(b.Root, "DriftSlab" + k + "_" + seed, VfxShapes.Prism(4, 1f, .84f, .16f, .785f, seed), Basalt[(k + seed) % 3]));
                    var pieces = new Transform[3];
                    for (int j = 0; j < 3; j++)
                        pieces[j] = Stone(b.Root, "DriftSlab" + k + "_" + seed + "_piece" + j, VfxShapes.Prism(4, 1f, .8f, .3f, .785f, seed + 500 + j), Basalt[(k + seed + j) % 3]);
                    b.Pieces.Add(pieces);
                }
                foreach (var r in RockRows)
                    b.Rocks.Add(Stone(b.Root, "DriftRock" + k + "_" + r.seed, VfxShapes.Prism(5, 1f, .7f, .3f, 0f, r.seed + k * 31), Basalt[(k + r.seed) % 3]));
                for (int i = 0; i < 4; i++)
                {
                    var puff = VfxShapes.Stand(b.Root, "DriftDust" + k + "_" + i, VfxShapes.TwoSided(VfxShapes.Splat(9, .48f, 201 + k * 17 + i * 5)), 1);
                    var r = puff.GetComponent<Renderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    VfxMaterial.Ghost(r, DustColour * (i % 2 == 0 ? .9f : 1f), .3f);
                    puff.transform.localScale = Vector3.zero;
                    b.Dust.Add(puff.transform); b.DustMaterials.Add(r.sharedMaterial);
                }
                _bands.Add(b);
            }
        }

        /// <summary>An opaque lit stone with a small local fill, so the dusk court cannot turn it into a black cutout.</summary>
        private static Transform Stone(Transform parent, string name, Mesh mesh, Color colour)
        {
            var go = VfxShapes.Stand(parent, name, mesh, 1);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            VfxMaterial.Solid(r, colour, .45f);
            go.transform.localScale = Vector3.zero;
            return go.transform;
        }

        /// <summary>A flat piece of light (`Resources/Shaders/SpiritGlow`): unlit, additive, soft across its width.</summary>
        internal static Glow AddGlow(Transform parent, string name, Color colour, float falloff, float core, Mesh mesh = null)
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "DriftGlowQuad" };
                mesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
                mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.RecalculateNormals();
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            }
            var go = VfxShapes.Stand(parent, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (shader != null)
            {
                var m = new Material(shader) { name = name };
                m.SetFloat("_Billboard", 0f); m.SetFloat("_Band", 1f); m.SetFloat("_Falloff", falloff); m.SetFloat("_Core", core); m.SetFloat("_Lift", 0f);
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else VfxMaterial.Ghost(renderer, colour, 1f);
            return new Glow { Node = go.transform, Renderer = renderer, Block = new MaterialPropertyBlock(), Colour = colour };
        }

        /// <summary>A flat ring in XZ, unit outer radius, its band thin, v across it. One per glow: `VfxShapes.Stand` retires the mesh
        /// with its piece.</summary>
        private static Mesh RingMesh()
        {
            const int Sides = 48;
            var v = new Vector3[(Sides + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[Sides * 12];
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = d * .86f; v[i * 2 + 1] = d;
                uv[i * 2] = new Vector2(i / (float)Sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Sides, 1f);
                if (i == Sides) continue;
                int b = i * 12, k = i * 2;
                // Both faces, so it reads from above and from a lens on the court.
                tris[b] = k; tris[b + 1] = k + 2; tris[b + 2] = k + 1; tris[b + 3] = k + 2; tris[b + 4] = k + 3; tris[b + 5] = k + 1;
                tris[b + 6] = k; tris[b + 7] = k + 1; tris[b + 8] = k + 2; tris[b + 9] = k + 2; tris[b + 10] = k + 1; tris[b + 11] = k + 3;
            }
            var mesh = new Mesh { name = "DriftStrikeRing", vertices = v, uv = uv, triangles = tris };
            mesh.RecalculateNormals();
            return mesh;
        }

        internal static void Light(Glow g, Vector3 position, Vector3 scale, Quaternion rotation, float strength)
        {
            g.Node.localPosition = position; g.Node.localScale = scale; g.Node.localRotation = rotation;
            var c = g.Colour; c.a = Mathf.Max(0f, strength);
            g.Block.SetColor(ColourId, c); g.Block.SetVector(FacingId, Vector4.zero);
            g.Renderer.SetPropertyBlock(g.Block);
            g.Renderer.enabled = strength > .002f;
        }

        private static float Ease(float from, float to, float time)
        { float u = Mathf.InverseLerp(from, to, time); return u * u * (3 - 2 * u); }

        /// <summary>Pose every piece at the wave's accepted age; each band's quake once, as its clock passes zero.</summary>
        public void StepTo(float age)
        {
            Pose(Mathf.Max(0f, age));
            if (age >= _lastAge)
                for (int i = _shookUpTo + 1; i < _bands.Count && age >= BandAt(i); i++)
                {
                    // Never on a rejoin far into the wave.
                    if (!_stage && age - BandAt(i) < .25f) Quake(i);
                    _shookUpTo = i;
                }
            _lastAge = age;
        }

        private void Pose(float t)
        {
            foreach (var strike in _strikes) PoseStrike(strike, t);
            int columns = Columns;
            float sixth = _halfWidth * 2f / columns;
            float cool = Ease(LastBand + .4f, SinkAt(_bands.Count - 1) + .3f, t);

            // THE FUSE: the molten crack racing down the middle of the lane, just ahead of each band, cooling in play.
            {
                float front = Front(t), from = .4f, len = Mathf.Max(.01f, front - from);
                float heat = 1.4f * (1f - .55f * Ease(LastBand + .1f, LastBand + .6f, t)) * (1f - cool);
                Light(_fuse, new Vector3(0f, .015f, from + len * .5f), new Vector3(len, .32f, 1f), Quaternion.Euler(90f, 90f, 0f), heat);
            }

            for (int k = 0; k < _bands.Count; k++)
            {
                var b = _bands[k];
                float at = BandAt(k), age = t - at, depth = b.End - b.Start, mid = (b.Start + b.End) * .5f;
                float breaks = SinkAt(k);
                bool gone = t >= BandGoneAt(k);
                b.Root.gameObject.SetActive(age >= 0f && !gone);
                if (age < 0f || gone) continue;
                // The molten light: a flare as the band goes, the glow in its cracks cooling, then one last flare through the seams
                // as it breaks.
                float glow = 2.3f * (1f - .4f * Mathf.Clamp01(age / .8f)) * (1f - .85f * Ease(at + .4f, breaks + .3f, t));
                // (Short and low, gone before the stone is: on a bare court it read as an orange stripe, film r47.)
                float flare = (t - breaks) / .3f;
                if (flare > 0f && flare < 1f) glow += .55f * Mathf.Sin(flare * Mathf.PI);
                glow *= 1f - Ease(breaks + .2f, breaks + .45f, t);
                Light(b.Light, new Vector3(0f, .01f, mid), new Vector3(_halfWidth * 1.9f, depth, 1f), Quaternion.Euler(90f, 0f, 0f), glow);
                float bandSink = Ease(breaks + .1f, breaks + SlabWait + SlabSink, t);

                // A wide lane makes deep slabs: their tilt is held to the cutscene's edge rise, so none stands up as a wall.
                float tiltHold = Mathf.Min(1f, 1.4f / Mathf.Max(.1f, depth * .88f));
                var rows = SlabRows[k];
                for (int i = 0; i < b.Slabs.Count; i++)
                {
                    var s = rows[b.Rows[i]];
                    // Evenly across the lane, each column nudged by its row's own offset within a sixth.
                    float across = ((i + .5f) / columns * 2f - 1f) + (s.across - ((b.Rows[i] + .5f) / 6f * 2f - 1f)) / columns;
                    float sa = age - s.delay, u = Mathf.Clamp01(sa / .1f);
                    // A jolt, a small overshoot, a hold, then it settles back to rubble so the ridge travels down the lane.
                    float up = sa < 0f ? 0f : u < 1f ? u * u : 1f + .12f * Mathf.Max(0f, 1f - (sa - .1f) / .12f);
                    float settle = 1f - .5f * Ease(.25f, .7f, sa);
                    float lift = up * settle;
                    float h = s.height * .8f, w = sixth * s.width, d = depth * s.depth;
                    float jitter = ((s.seed % 5) - 2) * .12f * Mathf.Min(depth, 1.6f);
                    var slab = b.Slabs[i];
                    // Inside its own band: the stone never draws past the hit area.
                    // (The last band's slabs tip toward the far edge: they keep their height's lean clear of it.)
                    float far = b.End - d * .5f - (k == _bands.Count - 1 ? h : 0f);
                    float z = Mathf.Clamp(mid + jitter, b.Start + d * .5f, Mathf.Max(b.Start + d * .5f, far));
                    // The edge columns keep their width and roll inside the lane's sides.
                    float x = Mathf.Clamp(across * _halfWidth, -_halfWidth + w * .5f + h * .3f, _halfWidth - w * .5f - h * .3f);

                    // THE RECEDE, this slab's own beat: the middle of the lane first, the edges after, each a little apart.
                    float su = t - (breaks + .1f * Mathf.Abs(across) + .04f * (s.seed / 3 % 3));
                    // It cracks (a shudder), then tips toward the lane's middle (so the leaning stone never leaves the lane) and
                    // slumps; then it breaks in three and the pieces slump on into the court.
                    float roll = x > 0f ? 1f : -1f;
                    float shudder = su >= 0f && su < .12f ? Mathf.Sin(su * 110f) * 3f * (1f - su / .12f) : 0f;
                    float tipOver = Ease(.1f, .3f, su), slump = .25f * tipOver;
                    bool broken = su >= .3f;
                    slab.gameObject.SetActive(sa >= 0f && !broken);
                    var slabAt = new Vector3(x, -h * .5f + h * .8f * lift - h * slump, z);
                    float tip = Mathf.Min(1f, up) * tiltHold;
                    var slabTurn = Quaternion.Euler(s.tiltX * 1.6f * tip + shudder, (s.seed % 7 - 3) * 5f, s.tiltZ * 1.8f * tip + shudder * .6f + roll * 20f * tipOver);
                    slab.localPosition = slabAt;
                    slab.localRotation = slabTurn;
                    slab.localScale = new Vector3(w, h, d);

                    var pieces = b.Pieces[i];
                    float fall = Ease(.3f, .3f + SlabSink - .3f, su);
                    for (int j = 0; j < 3; j++)
                    {
                        float side = j - 1;
                        var piece = pieces[j];
                        piece.gameObject.SetActive(broken);
                        if (!broken) continue;
                        // Each third of the slab drifts a little off its neighbour, leans its own way and goes down.
                        piece.localPosition = slabAt + slabTurn * new Vector3(side * w * .34f * (1f + .25f * fall), 0f, ((s.seed + j) % 3 - 1) * d * .08f)
                                              + Vector3.down * h * (.1f + 1.3f * fall);
                        piece.localRotation = slabTurn * Quaternion.Euler(((s.seed + j) % 3 - 1) * 14f * fall, j * 30f, (roll * 15f - side * 18f) * fall);
                        piece.localScale = new Vector3(w * .38f, h * .85f, d * .8f);
                    }
                }
                // Dust on the band's front edge, billowing up and out, thinning (dust may thin; stone may not). Lower than the
                // cutscene's, so it never paints a wall across a first-person view (live film r21).
                // The same puffs again as it breaks: low and wide over the band, settling where the stone went down.
                for (int i = 0; i < b.Dust.Count; i++)
                {
                    float du = Mathf.Clamp01(age / .9f), settle = (t - breaks - .2f) / .8f;
                    var dust = b.Dust[i];
                    var c = DustColour * (i % 2 == 0 ? .9f : 1f);
                    if (du < 1f)
                    {
                        float across = (i / 3f * 2f - 1f) * (_halfWidth - 1.2f);
                        float puff = Mathf.Lerp(.8f, 1.9f, du) * (.8f + .1f * i);
                        float z = Mathf.Min(b.End + .1f, _reach - .2f);
                        dust.localPosition = new Vector3(across, puff * .5f + du * .4f, z);
                        dust.localRotation = Quaternion.Euler(90f, 0f, (i - 1.5f) * 9f);
                        dust.localScale = new Vector3(puff * 1.3f, 1f, puff);
                        c.a = .7f * (1f - du * du);
                    }
                    else if (settle >= 0f && settle < 1f)
                    {
                        float across = ((i + .5f) / b.Dust.Count * 2f - 1f) * (_halfWidth - 1f);
                        float puff = Mathf.Lerp(.7f, 2.1f, settle);
                        dust.localPosition = new Vector3(across, puff * .3f + settle * .2f, mid + (i % 2 - .5f) * depth * .3f);
                        dust.localRotation = Quaternion.Euler(90f, 0f, (i - 1.5f) * 13f);
                        dust.localScale = new Vector3(puff * 1.6f, 1f, puff);
                        c.a = .55f * (1f - settle * settle);
                    }
                    dust.gameObject.SetActive(du < 1f || (settle >= 0f && settle < 1f));
                    b.DustMaterials[i].color = c; if (b.DustMaterials[i].HasProperty("_BaseColor")) b.DustMaterials[i].SetColor("_BaseColor", c);
                }
                // Rocks flung up and forward, landing where they land and staying; they go down with the slabs.
                for (int i = 0; i < RockRows.Length; i++)
                {
                    var r = RockRows[i];
                    var start = new Vector3(r.across * (_halfWidth - .4f), .2f, mid);
                    float land = 2f * r.up / 9.8f, ft = Mathf.Min(age, land);
                    var pos = start + Vector3.forward * (r.along * ft) + Vector3.up * (r.up * ft - 4.9f * ft * ft);
                    pos.z = Mathf.Min(pos.z, _reach - .2f);
                    pos.y -= r.size * 1.6f * bandSink;
                    float spin = ft * (240f + r.seed * 9f);
                    b.Rocks[i].localPosition = pos;
                    b.Rocks[i].localRotation = Quaternion.Euler(spin, r.seed * 23f, spin * .6f);
                    b.Rocks[i].localScale = Vector3.one * r.size;
                }
            }
        }

        private void Quake(int band)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rig = cam.GetComponent<CameraRig>();
            var b = _bands[band];
            Vector3 centre = transform.TransformPoint(new Vector3(0f, 0f, (b.Start + b.End) * .5f));
            float distance = Vector3.Distance(new Vector3(cam.transform.position.x, 0f, cam.transform.position.z), new Vector3(centre.x, 0f, centre.z));
            float falloff = Mathf.Lerp(1f, .35f, Mathf.Clamp01(distance / 18f)) * (band == GeoRules.DriftBlasts - 1 ? 1.25f : 1f);
            if (rig != null) { rig.Shake(.32f * falloff, .28f); rig.BeginGroundRumble(.55f * falloff); }
            if (Settings.SettingsStore.Current.ReducedEffects) return;
            // A few grains through this viewer's own view, from above.
            var ahead = cam.transform.position + cam.transform.forward * 1.6f + Vector3.up * 1.1f;
            DanteBoulderGrit.Drop(ahead + cam.transform.right * (-.5f + .3f * (band % 3)), false);
            DanteBoulderGrit.Drop(ahead + cam.transform.right * (.45f - .2f * (band % 2)) + cam.transform.forward * .4f, false);
        }

        /// <summary>The lane's stone, as drawn, in world space (tests: it covers the hit area and never strays out of it).</summary>
        public Bounds StoneBounds()
        {
            bool any = false; var bounds = new Bounds();
            foreach (var b in _bands)
                foreach (var s in b.Slabs)
                {
                    if (!s.gameObject.activeInHierarchy) continue;
                    var r = s.GetComponent<Renderer>().bounds;
                    if (!any) { bounds = r; any = true; } else bounds.Encapsulate(r);
                }
            return bounds;
        }
    }
}
