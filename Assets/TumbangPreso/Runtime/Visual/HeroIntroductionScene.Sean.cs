using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // RAGO, MAYON AWAKENS v2: THE MOUNTAIN RISES, 5.0 s.
        //
        // Owner 2026-10-24, after v1 (a mountain backdrop, a blast round him, 8 s of a buff): *"his ult sucks as shit especially
        // cutscene i cant even tell wtf his ult does"*, then *"the ult of sean doesnt earn its place"* beside Paete's and Phaister's;
        // he chose "The mountain rises" and pointed at Dante's: *"dante has a really good ult"*. So this is Dante's shape (the
        // charge-up, where the power comes from, cut as it launches at the real players) on Paete's payoff (a thing in the court,
        // placed where he chose, that everyone plays round). Play is `SeanVolcano`.
        //
        // ONE SENTENCE: the lava in his fists goes into the court, races to the spot he chose, and the mountain bursts up there.
        // ONE TRAVELLING THING: the lava, from his fists, along the court, up as the mountain.
        //   A. THE FISTS (0 to 1.5): both fists into the court (0.35); lava wells up out of it round them and climbs his forearms
        //      into the Molten fists (`SeanProps/molten_fist`, what `SeanMoltenLook` puts on him in play); his eyes ignite (1.1).
        //   B. THE RUN (1.5 to 3.0): a river of lava races from his fists to the spot (1.55 to 2.45); the lens races beside it,
        //      low; the court there bulges and vents (2.4 to 3.0).
        //   C. THE RISE (3.0 to 5.0): wide, low, him and the spot and the real players: he hauls his fists up and the mountain
        //      (`SeanProps/mayon`, 3 m, the same model play stands up) bursts out of the court (3.1); its wave races out to 9 m.
        // ⚠️ IT ENDS AS THE WAVE REACHES THE PLAYERS, NOT AFTER IT THROWS THEM. The live volcano is spawned standing
        // (`SeanVolcano.Spawn(handBack)`) and throws them the moment the cutscene hands back: the throw is seen once.
        // ⚠️ THE SPOT IS PLAY'S SPOT (`SeanVolcano.SpotFor` on the commit's aim), so the staged mountain stands where the live one will.
        // ⚠️ NO STAGE WALL. The spot can be 10 m off, past any wall's 8 m; the court and the real players ARE the stage here, so
        // only the grade steps back.
        // =========================================================================================
        private const float SnSlam = .35f, SnFistsFrom = .42f, SnFistsTo = 1.05f, SnIgnite = 1.1f, SnRunFrom = 1.55f, SnRunTo = 2.45f,
                            SnRise = 3.1f, SnRiseTo = 3.45f, SnHome = 4.55f;
        private static readonly Color SnCore = new Color(1f, .78f, .23f, 1), SnEdge = new Color(.85f, .15f, .11f, 1),
                                      SnEmber = new Color(1f, .45f, .12f, 1), SnAsh = new Color(.45f, .42f, .42f, 1),
                                      SnSteam = new Color(.9f, .88f, .86f, 1);

        private int _snWell, _snRiverHead, _snBulge, _snColumn, _snColumnCore, _snRing, _snRingThin, _snSummit, _snCraterBomb;
        private Transform _snMayon;
        private Vector3 _snMayonScale = Vector3.one, _snMayonOffset;
        private Vector3 _snSpot;
        private LineRenderer _snRiver, _snRiverCore;
        private readonly List<int> _snEmbers = new List<int>(18), _snAsh = new List<int>(10), _snFistGlows = new List<int>(2),
                                   _snDrips = new List<int>(6), _snEyes = new List<int>(2), _snVents = new List<int>(4),
                                   _snDebris = new List<int>(14), _snBeads = new List<int>(8);
        private readonly List<LineRenderer> _snCracks = new List<LineRenderer>(6), _snBurstCracks = new List<LineRenderer>(8);
        private readonly List<Quaternion> _snOtherRest = new List<Quaternion>(4);

        /// <summary>A Molten fist on one of the copy's hands: the model, the bone it rides, the palm in that bone's space.</summary>
        private sealed class SnFist { public Transform Model, Knuckles, Bone; public Vector3 Palm; public float Scale; public bool Carrying; }
        private readonly List<SnFist> _snFists = new List<SnFist>(2);

        private void BuildSean()
        {
            // Play's spot, in the scene's space: where the live mountain will stand.
            var lata = GameServices.Round?.Lata;
            var spot = Abilities.SeanVolcano.SpotFor(_source.transform.position, _source.transform.forward, _aim, lata);
            if (lata == null && FilmCan.HasValue)
            {
                var can = FilmCan.Value;
                SeanRules.VolcanoSpotClearOfCan(spot.x, spot.z, can.x, can.z, _source.transform.position.x, _source.transform.position.z,
                    out float x, out float z);
                spot.x = x; spot.z = z;
            }
            _snSpot = _root.transform.InverseTransformPoint(spot);
            _snSpot.y = Mathf.Clamp(_snSpot.y, -1.5f, 1.5f);

            // Shot A: the court round his fists, lava welling up out of it; the cracks it sends out.
            _snWell = AddGlow("SeanWell", SnEdge, billboard: false, falloff: 1.8f, core: .5f);
            for (int i = 0; i < 6; i++) _snCracks.Add(Line("SeanCrack" + i, 7, .07f, SnCore));
            BuildSeanFists();
            for (int i = 0; i < 2; i++) _snFistGlows.Add(AddGlow("SeanFistGlow" + i, SnCore, falloff: 2.2f, core: .55f));
            for (int i = 0; i < 6; i++) _snDrips.Add(AddGlow("SeanDrip" + i, i % 2 == 0 ? SnCore : SnEdge, falloff: 2.4f, core: .8f));
            var glint = VfxShapes.Star(4, .22f);
            for (int i = 0; i < 2; i++) _snEyes.Add(AddGlow("SeanEyeGlint" + i, SnCore, glint, falloff: 1.4f, core: .8f));
            for (int i = 0; i < 18; i++) _snEmbers.Add(AddGlow("SeanEmber" + i, i % 3 == 0 ? SnCore : SnEmber, falloff: 2.4f, core: .7f));
            for (int i = 0; i < 10; i++) _snAsh.Add(AddGlow("SeanAsh" + i, SnAsh, falloff: 1.6f, core: .4f));

            // Shot B: the river to the spot, its bright head and the beads it throws, the bulge and the vents at the spot.
            _snRiver = Line("SeanRiver", 14, .32f, SnEdge);
            _snRiverCore = Line("SeanRiverCore", 14, .14f, SnCore);
            _snRiverHead = AddGlow("SeanRiverHead", SnCore, falloff: 1.8f, core: .7f);
            for (int i = 0; i < 8; i++) _snBeads.Add(AddGlow("SeanRiverBead" + i, i % 2 == 0 ? SnCore : SnEmber, falloff: 2.4f, core: .8f));
            _snBulge = AddGlow("SeanBulge", SnEdge, billboard: false, falloff: 1.6f, core: .5f);
            for (int i = 0; i < 4; i++) _snVents.Add(AddGlow("SeanVent" + i, SnSteam, falloff: 1.5f, core: .3f));

            // Shot C: the mountain itself, its fountain, the summit, the debris it throws, its wave and cracks, the first bomb.
            var mayon = SeanProp.Spawn("mayon", _root.transform, SeanProp.CourtMountainPalette);
            if (mayon != null) { _snMayon = mayon.transform; FitSeanMountain(); mayon.SetActive(false); }
            _snColumn = AddGlow("MayonColumn", SnEdge, SnConeMesh(), billboard: false, falloff: 1.2f, core: .35f);
            _snColumnCore = AddGlow("MayonColumnCore", SnCore, SnConeMesh(), billboard: false, falloff: 1.6f, core: .7f);
            _snSummit = AddGlow("MayonSummit", SnEmber, falloff: 2.4f, core: .5f);
            _snCraterBomb = AddGlow("MayonFirstBomb", SnCore, falloff: 2f, core: .7f);
            for (int i = 0; i < 14; i++) _snDebris.Add(AddGlow("MayonDebris" + i, i % 2 == 0 ? SnCore : SnEdge, falloff: 2.2f, core: .7f));
            _snRing = AddGlow("SeanShockwave", SnEmber, SnRingMesh(.88f), billboard: false, falloff: 1.4f, core: .6f);
            _snRingThin = AddGlow("SeanShockwaveThin", SnCore, SnRingMesh(.96f), billboard: false, falloff: 1.4f, core: .8f);
            for (int i = 0; i < 8; i++) _snBurstCracks.Add(Line("SeanBurstCrack" + i, 6, .1f, SnEdge));

            // The real players, where they stand (never pulled in: who is thrown is play's rule, not the stage's).
            StageOthers(1.0f, 40f);
            foreach (var other in _others) _snOtherRest.Add(other.Holder.transform.localRotation);
        }

        /// <summary>
        /// One Molten fist per hand of the copy, on the hand bones the game itself uses (`CharacterVisual.HandBone`): on this rig
        /// that is the FOREARM, so the fist sits on the hand and not the elbow, and the sleeve lies along the real forearm.
        /// </summary>
        private void BuildSeanFists()
        {
            if (_bodyRoot == null) return;
            SkinnedMeshRenderer skinned = null;
            foreach (var candidate in _bodyRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (CharacterVisual.HandBone(candidate, "right", out _, out _)) { skinned = candidate; break; }
            if (skinned == null) return;
            foreach (string side in new[] { "left", "right" })
            {
                if (!CharacterVisual.HandBone(skinned, side, out int index, out var palm)) continue;
                var model = SeanProp.Spawn("molten_fist", _root.transform);
                if (model == null) return;
                model.SetActive(false);
                var bone = skinned.bones[index];
                float forearm = (bone.TransformPoint(palm) - bone.position).magnitude;
                // The slipper rides the right hand (`CharacterVisual.HandAnchor`): that hand stays a hand.
                _snFists.Add(new SnFist
                {
                    Model = model.transform, Knuckles = SeanProp.Find(model, "knuckles"), Bone = bone, Palm = palm,
                    Scale = Mathf.Clamp(forearm * .95f / .36f, .6f, 1.6f), Carrying = side == "right" && _heldItem != null,
                });
            }
        }

        /// <summary>
        /// Measures the mountain as drawn at scale 1 and fits it to play's: its summit at `SeanRules.VolcanoHeight`, its foot on
        /// the spot. ⚠️ FROM ITS DRAWN BOUNDS, NOT THE BUILDER'S NUMBERS (film s14: the builder's "1.0 tall" stood no taller than him).
        /// </summary>
        private void FitSeanMountain()
        {
            _snMayon.localPosition = Vector3.zero; _snMayon.localRotation = Quaternion.identity; _snMayon.localScale = Vector3.one;
            var renderers = _snMayon.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            float low = float.MaxValue, high = float.MinValue, left = float.MaxValue, right = float.MinValue, near = float.MaxValue, far = float.MinValue;
            foreach (var r in renderers)
            {
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var local = _root.transform.InverseTransformPoint(corner);
                    low = Mathf.Min(low, local.y); high = Mathf.Max(high, local.y);
                    left = Mathf.Min(left, local.x); right = Mathf.Max(right, local.x);
                    near = Mathf.Min(near, local.z); far = Mathf.Max(far, local.z);
                }
            }
            float tall = high - low;
            if (tall < .001f) return;
            float k = SeanRules.VolcanoHeight / (tall * .97f / 1.225f);
            _snMayonScale = Vector3.one * k;
            _snMayonOffset = new Vector3(-(left + right) * .5f * k, -low * k - .05f, -(near + far) * .5f * k);
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>Mayon's cone as a silhouette in XY: concave sides and a small flat summit, unit high, unit wide at the base.</summary>
        private static Mesh SnConeMesh()
        {
            const int rows = 14;
            var vertices = new List<Vector3>(rows * 2 + 2); var triangles = new List<int>(rows * 6);
            for (int i = 0; i <= rows; i++)
            {
                float h = i / (float)rows;
                float half = .5f * Mathf.Pow(1 - h, 1.7f) + .025f;
                vertices.Add(new Vector3(-half, h, 0)); vertices.Add(new Vector3(half, h, 0));
                if (i == rows) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n, n + 1, n + 2, n + 2, n + 1, n + 3 });
            }
            var mesh = new Mesh { name = "MayonCone" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            var uv = new List<Vector2>(vertices.Count);
            foreach (var v in vertices) uv.Add(new Vector2(v.x + .5f, v.y));
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            return VfxShapes.TwoSided(mesh);
        }

        /// <summary>A flat ring in XZ of unit outer radius; its band runs from <paramref name="inner"/> to 1, v across it.</summary>
        private static Mesh SnRingMesh(float inner)
        {
            const int sides = 64;
            var vertices = new List<Vector3>((sides + 1) * 2); var uv = new List<Vector2>((sides + 1) * 2);
            var triangles = new List<int>(sides * 6);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                var way = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                vertices.Add(way * inner); vertices.Add(way);
                uv.Add(new Vector2(i / (float)sides, 0)); uv.Add(new Vector2(i / (float)sides, 1));
                if (i == sides) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n, n + 1, n + 2, n + 2, n + 1, n + 3 });
            }
            var mesh = new Mesh { name = "SeanShockwaveRing" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2, .2f, 2));
            return VfxShapes.TwoSided(mesh);
        }

        /// <summary>A fixed scatter in 0 to 1 for row <paramref name="i"/>: the same every frame and on every peer.</summary>
        private static float SnScatter(int i, int salt)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(salt * 19349663);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h & 0xffff) / 65535f;
        }

        /// <summary>Where his fists meet the court (the river's source).</summary>
        private static readonly Vector3 SnFistsOnCourt = new Vector3(0, .03f, .55f);

        /// <summary>The river's path from his fists to the spot, <paramref name="u"/> 0 to 1, a gentle S so it reads as poured.</summary>
        private Vector3 SnRiverAt(float u)
        {
            var from = SnFistsOnCourt; var to = new Vector3(_snSpot.x, _snSpot.y + .03f, _snSpot.z);
            var way = to - from; way.y = 0;
            var side = way.sqrMagnitude > .01f ? new Vector3(way.z, 0, -way.x).normalized : Vector3.right;
            float bend = Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Min(.6f, way.magnitude * .06f);
            var p = Vector3.Lerp(from, to, u) + side * bend;
            p.y = Mathf.Lerp(from.y, to.y, u) + .02f;
            return p;
        }

        /// <summary>The river's head along its path: off his fists at 1.55, at the spot by 2.45, fast at first.</summary>
        private float SnRiverHead(float t)
        {
            float u = Mathf.Clamp01(Mathf.InverseLerp(SnRunFrom, SnRunTo, t));
            return 1 - (1 - u) * (1 - u) * (1 - .3f * u);
        }

        /// <summary>The wave's reach from the spot: out fast off the rise, easing as it reaches the 9 m ring at the last frame.</summary>
        private float SnWaveReach(float t)
        {
            float u = Mathf.Clamp01(Mathf.InverseLerp(SnRise, Seconds, t));
            return SeanRules.VolcanoBlastRadius * (1 - (1 - u) * (1 - u));
        }

        // ------------------------------------------------------------------ the clock

        private void SampleSean(float t)
        {
            float present = StagePresence(t, .25f);
            SampleSeanFists(t, present);
            SampleSeanAir(t, present);
            SampleSeanRiver(t, present);
            SampleSeanMountain(t, present);
            SampleSeanOthers(t);
        }

        private void SampleSeanFists(float t, float present)
        {
            // The court round his fists glows as they go in, and the lava wells up out of it.
            float well = Ease(SnSlam - .02f, SnSlam + .15f, t) * (1 - .6f * Ease(SnRunTo, SnRise + .5f, t));
            PlaceGlow(_snWell, SnFistsOnCourt + Vector3.up * .01f, new Vector3(1.3f, 1.3f, 1) * (1 + .3f * Flash(t, SnSlam, .2f)),
                Quaternion.Euler(90, 0, 0), well * 1.4f * present);
            // Cracks spider out round his fists from the slam.
            for (int i = 0; i < _snCracks.Count; i++)
            {
                var line = _snCracks[i];
                float grow = Ease(SnSlam + .02f * i, SnSlam + .35f, t);
                float angle = (-150f + 60f * i + 14f * (SnScatter(i, 1) - .5f)) * Mathf.Deg2Rad;
                var to = SnFistsOnCourt + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * (1.6f + .6f * SnScatter(i, 2));
                for (int k = 0; k < line.positionCount; k++)
                {
                    float s = k / (float)(line.positionCount - 1);
                    var side = new Vector3(to.z, 0, -to.x).normalized * (.14f * (SnScatter(i * 7 + k, 3) - .5f));
                    line.SetPosition(k, Vector3.Lerp(SnFistsOnCourt, to, s * grow) + side * s + Vector3.up * .02f);
                }
                line.widthMultiplier = .07f * grow * present * (1 - .5f * Ease(SnRise, Seconds, t));
                line.enabled = grow > .01f && present > .01f;
            }

            // The Molten fists: lava climbs out of the court up both forearms (left, then right) and stays; he is Molten from here.
            for (int i = 0; i < _snFists.Count; i++)
            {
                var f = _snFists[i];
                float from = SnFistsFrom + .1f * i;
                float grow = Mathf.Clamp01(Mathf.InverseLerp(from, from + (SnFistsTo - SnFistsFrom), t));
                float swell = grow <= 0 ? 0 : 1f - Mathf.Pow(1f - grow, 3f) + .2f * Mathf.Sin(grow * Mathf.PI);
                swell *= 1 + .12f * Flash(t, SnRise, .12f);
                if (f.Carrying) swell = 0;
                bool shown = swell > .01f;
                if (f.Model.gameObject.activeSelf != shown) f.Model.gameObject.SetActive(shown);
                Vector3 palmWorld = f.Bone.TransformPoint(f.Palm);
                Vector3 palm = _root.transform.InverseTransformPoint(palmWorld);
                Vector3 dir = _root.transform.InverseTransformDirection(palmWorld - f.Bone.position);
                if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
                dir.Normalize();
                // The world's up, so its drips hang down (film s15: the bone's up stood them out sideways like rods).
                var up = Mathf.Abs(dir.y) < .95f ? Vector3.up : Vector3.forward;
                float s = f.Scale * Mathf.Max(.0001f, swell);
                f.Model.localPosition = palm - dir * .1f * s;
                f.Model.localRotation = Quaternion.LookRotation(dir, up);
                f.Model.localScale = Vector3.one * s;
                if (f.Knuckles != null) f.Knuckles.localScale = Vector3.one * (1f + .08f * Mathf.Sin(t * 9f + i * 1.7f));
                if (i < _snFistGlows.Count)
                    PlaceGlow(_snFistGlows[i], palm, Vector3.one * .6f * Mathf.Min(1, swell), Quaternion.identity, shown ? 1.2f + .8f * Flash(t, SnRise, .2f) : 0);
                for (int d = i * 3; d < i * 3 + 3 && d < _snDrips.Count; d++)
                {
                    float cycle = Mathf.Repeat(t * 1.4f + d * .29f, 1f);
                    var at = palm + new Vector3(.06f * (d % 3 - 1), -cycle * Mathf.Max(.1f, palm.y), .02f);
                    PlaceGlow(_snDrips[d], at, Vector3.one * .06f, Quaternion.identity, shown && cycle < .95f ? 1.3f : 0);
                }
            }

            // His eyes ignite as the lava reaches his elbows: a four-pointed glint on each.
            var head = HeadPoint + new Vector3(0, -.32f, .17f);
            float eyes = Ease(SnIgnite - .05f, SnIgnite + .08f, t) * (1 - Ease(SnRunFrom + .3f, SnRunFrom + .7f, t)) * (1 + 1.5f * Flash(t, SnIgnite + .05f, .12f));
            for (int i = 0; i < _snEyes.Count; i++)
                PlaceGlow(_snEyes[i], head + new Vector3(i == 0 ? -.075f : .075f, 0, 0), Vector3.one * (.16f + .2f * Flash(t, SnIgnite + .05f, .15f)),
                    Quaternion.Euler(0, 0, 45f * t), eyes * 1.4f);
        }

        private void SampleSeanAir(float t, float present)
        {
            // Embers and ash in the air the whole time, two of them near the lens in shot A; they ride up past him.
            for (int i = 0; i < _snEmbers.Count; i++)
            {
                float speed = .35f + .4f * SnScatter(i, 4);
                float rise = Mathf.Repeat(SnScatter(i, 5) * 3.2f + t * speed, 3.2f);
                var at = new Vector3((SnScatter(i, 6) - .5f) * 3.4f, rise, .4f + SnScatter(i, 7) * 2.6f);
                if (i < 2) at = new Vector3(i == 0 ? -.35f : .3f, .35f + rise * .3f, 1.4f);
                // Half of them ride with the river and gather round the mountain once it stands.
                if (i % 2 == 1) at = Vector3.Lerp(at, _snSpot + new Vector3(at.x, at.y * 1.4f, at.z - 1.5f), Ease(SnRunFrom, SnRise, t));
                float size = i < 2 ? .07f : .03f + .03f * SnScatter(i, 8);
                PlaceGlow(_snEmbers[i], at, Vector3.one * size * (1 + Flash(t, SnRise, .3f)), Quaternion.identity, present * 1.4f);
            }
            for (int i = 0; i < _snAsh.Count; i++)
            {
                float fall = Mathf.Repeat(SnScatter(i, 9) * 4f - t * .25f, 4f);
                var at = _snSpot * Ease(SnRunFrom, SnRise, t) + new Vector3((SnScatter(i, 10) - .5f) * 5f, fall, (SnScatter(i, 11) - .5f) * 4f);
                PlaceGlow(_snAsh[i], at, Vector3.one * (.06f + .03f * SnScatter(i, 12)), Quaternion.identity, present * .5f);
            }
        }

        private void SampleSeanRiver(float t, float present)
        {
            // The river: drawn from his fists to its head; bright core over a cherry body; it cools behind once the mountain is up.
            float head = SnRiverHead(t);
            bool running = t > SnRunFrom && present > .01f;
            float cool = 1 - .7f * Ease(SnRise + .2f, Seconds, t);
            for (int k = 0; k < _snRiver.positionCount; k++)
            {
                float u = head * k / (_snRiver.positionCount - 1);
                var p = SnRiverAt(u);
                _snRiver.SetPosition(k, p); _snRiverCore.SetPosition(k, p + Vector3.up * .01f);
            }
            _snRiver.widthMultiplier = .32f * cool; _snRiverCore.widthMultiplier = .14f * cool;
            _snRiver.enabled = _snRiverCore.enabled = running;
            var at = SnRiverAt(head);
            bool racing = t > SnRunFrom && t < SnRunTo + .1f;
            PlaceGlow(_snRiverHead, at + Vector3.up * .25f, Vector3.one * (.9f + .2f * Mathf.Sin(t * 30f)), Quaternion.identity, racing ? 2f * present : 0);
            // Beads of lava flicked up off the head as it runs.
            for (int i = 0; i < _snBeads.Count; i++)
            {
                float phase = Mathf.Repeat(t * 3f + i / (float)_snBeads.Count, 1f);
                float back = Mathf.Max(0, head - .08f * phase);
                var b = SnRiverAt(back) + new Vector3((SnScatter(i, 41) - .5f) * .6f * phase, 1.2f * phase - 1.6f * phase * phase, 0);
                PlaceGlow(_snBeads[i], b, Vector3.one * .08f, Quaternion.identity, racing && b.y > 0 ? 1.4f : 0);
            }

            // The bulge where it arrives: the court swells and glows, and steam vents round it, until the mountain breaks it.
            float bulge = Ease(SnRunTo - .1f, SnRise, t) * (1 - Ease(SnRise, SnRise + .3f, t));
            PlaceGlow(_snBulge, _snSpot + Vector3.up * .02f, new Vector3(2.4f, 2.4f, 1) * (.6f + .5f * bulge + .08f * Mathf.Sin(t * 22f)),
                Quaternion.Euler(90, 0, 0), bulge * 1.6f * present);
            for (int i = 0; i < _snVents.Count; i++)
            {
                float a = (i * 90f + 30f * SnScatter(i, 42)) * Mathf.Deg2Rad;
                float k = Mathf.Repeat(t * 1.6f + i * .25f, 1f);
                var v = _snSpot + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 1.1f + Vector3.up * (.2f + 1.2f * k);
                PlaceGlow(_snVents[i], v, Vector3.one * (.25f + .5f * k), Quaternion.identity, bulge * .55f * (1 - k));
            }
        }

        private void SampleSeanMountain(float t, float present)
        {
            // It bursts up out of the court at 3.1 with one overshoot and stands; it is play's mountain from the hand-back.
            float riseU = Mathf.Clamp01(Mathf.InverseLerp(SnRise, SnRiseTo, t));
            float up = riseU <= 0 ? 0 : 1f - Mathf.Pow(1f - riseU, 3f) + .14f * Mathf.Sin(riseU * Mathf.PI);
            if (_snMayon != null)
            {
                bool shown = t >= SnRise && present > .01f;
                if (_snMayon.gameObject.activeSelf != shown) _snMayon.gameObject.SetActive(shown);
                float shake = .05f * Flash(t, SnRise + .1f, .4f) * Mathf.Sin(t * 70f);
                _snMayon.localPosition = _snSpot + _snMayonOffset + new Vector3(shake, -SeanRules.VolcanoHeight * (1 - up), 0);
                _snMayon.localRotation = Quaternion.identity;
                _snMayon.localScale = _snMayonScale;
            }
            float summitY = _snSpot.y + SeanRules.VolcanoHeight * up;
            var summit = new Vector3(_snSpot.x, summitY, _snSpot.z);
            // The fountain out of the crater in the cone's shape, flaring on the rise and settling to a glow.
            float column = Ease(SnRise, SnRise + .1f, t) * (1 - .6f * Ease(SnRise + .5f, Seconds, t));
            float rise = Ease(SnRise, SnRise + .35f, t);
            var toward = new Vector3(-_snSpot.x, 0, -_snSpot.z);
            var face = Quaternion.LookRotation(toward.sqrMagnitude > .01f ? toward : Vector3.back);
            // (film s16: at 2.2 m it read as a needle over the crater.)
            PlaceGlow(_snColumn, summit, new Vector3(3.6f, 6.0f * rise, 1), face, column * 1.2f * present);
            PlaceGlow(_snColumnCore, summit, new Vector3(1.8f, 4.8f * rise, 1), face, column * (1.3f + 1.5f * Flash(t, SnRise + .05f, .2f)) * present);
            PlaceGlow(_snSummit, summit + Vector3.up * .2f, Vector3.one * 1.6f, Quaternion.identity, Ease(SnRise, SnRise + .2f, t) * 1.3f * present);
            // The first bomb wells up in the crater as the cut comes: what it will do next.
            float bomb = Ease(SnHome - .35f, SnHome, t);
            PlaceGlow(_snCraterBomb, summit + Vector3.up * (.2f + .4f * bomb), Vector3.one * (.4f + .5f * bomb), Quaternion.identity, bomb * 1.8f * present);
            // Debris thrown up and out of the crater on fixed arcs.
            float after = Mathf.Max(0, t - SnRise);
            for (int i = 0; i < _snDebris.Count; i++)
            {
                float angle = (i * (360f / 14f) + 12f * SnScatter(i, 43)) * Mathf.Deg2Rad;
                var way = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                float k = after * (.9f + .3f * SnScatter(i, 44));
                var at = _snSpot + Vector3.up * SeanRules.VolcanoHeight * .8f + way * (2f + 2.5f * SnScatter(i, 45)) * k
                         + Vector3.up * ((5f + 3f * SnScatter(i, 46)) * k - 6f * k * k);
                float on = t > SnRise && at.y > _snSpot.y ? present : 0;
                PlaceGlow(_snDebris[i], at, Vector3.one * (.14f + .08f * SnScatter(i, 47)), Quaternion.identity, on * 1.3f);
            }
            // The wave: from its foot out to play's 9 m ring at the last frame, with cracks racing ahead of it.
            float reach = SnWaveReach(t);
            float wave = t > SnRise ? present : 0;
            PlaceGlow(_snRing, _snSpot + new Vector3(0, .04f, 0), new Vector3(reach, 1, reach), Quaternion.identity, wave * 1.5f);
            PlaceGlow(_snRingThin, _snSpot + new Vector3(0, .05f, 0), new Vector3(reach * 1.03f, 1, reach * 1.03f), Quaternion.identity, wave * 1.8f);
            for (int i = 0; i < _snBurstCracks.Count; i++)
            {
                var line = _snBurstCracks[i];
                float angle = (i * 45f + 20f * (SnScatter(i, 13) - .5f)) * Mathf.Deg2Rad;
                var way = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                for (int k = 0; k < line.positionCount; k++)
                {
                    float u = k / (float)(line.positionCount - 1);
                    var side = new Vector3(way.z, 0, -way.x) * (.25f * (SnScatter(i * 9 + k, 14) - .5f));
                    line.SetPosition(k, _snSpot + way * (SeanRules.VolcanoSolidRadius + reach * .95f * u) + side * u + Vector3.up * .03f);
                }
                line.widthMultiplier = .1f * wave;
                line.enabled = wave > .01f;
            }
        }

        private void SampleSeanOthers(float t)
        {
            // Each staged player braces and leans away from the mountain as its wave reaches them (9 m at the last frame).
            for (int i = 0; i < _others.Count; i++)
            {
                var other = _others[i];
                if (other.Holder == null) continue;
                if (!other.Holder.activeSelf) other.Holder.SetActive(true);
                var away = other.Home - _snSpot; away.y = 0;
                float reach = away.magnitude;
                float share = Mathf.Clamp01(reach / SeanRules.VolcanoBlastRadius);
                float reachedAt = SnRise + (Seconds - SnRise) * (1 - Mathf.Sqrt(1 - share));
                float lean = reach <= SeanRules.VolcanoBlastRadius ? Ease(reachedAt - .12f, reachedAt + .05f, t) : 0;
                var axis = Vector3.Cross(Vector3.up, away.sqrMagnitude > .001f ? away.normalized : Vector3.forward);
                other.Holder.transform.localRotation = Quaternion.AngleAxis(16f * lean, axis) * _snOtherRest[i];
            }
        }

        // ------------------------------------------------------------------ the lens, the grade, the hand-back

        /// <summary>
        /// Shots B and C are placed from the real spot (the table's are fallbacks for a stage with no spot). B races low beside the
        /// river, a step behind its head; C stands off to one side of the line from him to the mountain, far enough to hold both
        /// and the ring, and eases back as the wave goes out. Scene space.
        /// </summary>
        private void SeanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            var line = _snSpot - SnFistsOnCourt; line.y = 0;
            float length = line.magnitude;
            var dir = length > .01f ? line / length : Vector3.forward;
            var side = new Vector3(dir.z, 0, -dir.x);
            // The lens goes to the side away from the slipper hand, as the table's shots do.
            if (_heldItem != null) side = -side;
            if (index == 1)
            {
                float head = SnRiverHead(Mathf.Max(t, SnRunFrom));
                var at = SnRiverAt(head);
                eye = at - dir * 2.4f + side * 1.3f + Vector3.up * .9f;
                look = at + dir * 1.8f + Vector3.up * .35f;
                fov = 52f;
                // (film s16: the race ran the lens into a standing player.)
                eye = KeepLensOffOthers(eye, 1.6f);
            }
            else if (index == 2)
            {
                // Over his shoulder at the mountain (film s16: from the side at 12 m he, it and the players were specks): he is
                // in the near frame, the mountain he called up is the middle of it, the players round it are between. It eases
                // back and up as the wave goes out, so the ring and whoever it reaches stay in.
                float open = Ease(SnRise, Seconds, t);
                eye = -dir * (2.2f + 1.2f * open) + side * (1.5f + .5f * open) + Vector3.up * (1.6f + .8f * open);
                look = Vector3.Lerp(_snSpot, SnFistsOnCourt, .25f) + Vector3.up * (1.3f + .3f * open);
                fov = 55f + 5f * open;
                eye = KeepLensOffOthers(eye, 1.3f);
            }
            // The slam and the rise knock the lens.
            float knock = Flash(t, SnSlam, .12f) * .04f + Flash(t, SnRise, .18f) * .09f;
            if (knock > 0) eye += new Vector3(Mathf.Sin(t * 83f), Mathf.Sin(t * 71f), 0) * knock;
        }

        /// <summary>The court steps back while the lava runs, flares on the slam and the rise, and comes back for play.</summary>
        private void SeanGrade(float t, out float brightness, out float saturation)
        {
            float on = Ease(0, .25f, t) * (1 - Ease(Seconds - .4f, Seconds, t));
            brightness = Mathf.Lerp(1f, .8f, on) * (1 + .2f * Flash(t, SnSlam, .1f) + .3f * Flash(t, SnRise + .03f, .12f));
            saturation = Mathf.Lerp(1f, .9f, on);
        }

        /// <summary>
        /// ⚠️ HIS OWN SCREEN COMES HOME TO HIS OWN EYES (kept from the parol's cutscene, owner 2026-10-09). From `SnHome` the lens
        /// leaves the last shot for where his first-person camera will be the moment play resumes: the live camera's own place
        /// and aim (the world is paused). His copy is not drawn once the lens is nearly in it. World positions.
        /// </summary>
        private void SeanHandBack(float t, Camera live, ref Vector3 eye, ref Vector3 target, ref float fov)
        {
            float home = Ease(SnHome, Seconds - .06f, t);
            // ⚠️ SET BOTH WAYS, EVERY CALL. The phase samples each shot at its end to judge it (`UltimatePhaseView.ChooseShot`):
            // the last one's end is past `SnHome`, and a one-way "off" hid his copy for the whole cutscene in a match (film v5:
            // shot A framed an empty court). Drawn until the lens is nearly in his eyes, at any time asked.
            bool drawn = home <= .7f;
            foreach (var surface in _bodyRenderers) if (surface != null) surface.enabled = drawn;
            if (!drawn) foreach (var f in _snFists) if (f.Model != null) f.Model.gameObject.SetActive(false);
            if (home <= 0) return;
            var eyes = live.transform.position;
            eye = Vector3.Lerp(eye, eyes, home);
            target = Vector3.Lerp(target, eyes + live.transform.forward * 8f, home);
            fov = Mathf.Lerp(fov, live.fieldOfView, home);
        }

        /// <summary>An unlit backdrop piece, so the court's light never greys it. Cheska's stage uses it.</summary>
        private void RagoUnlitBackdrop(int index)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            var piece = _pieces[index];
            var material = new Material(shader) { name = "Rago dusk backdrop", color = piece.Color };
            piece.Renderer.sharedMaterial = material;
            VfxRenderTag.Own(piece.Renderer.gameObject, material);
        }
    }
}
