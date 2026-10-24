using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // RAGO, MAYON AWAKENS, 7.0 s (owner 2026-10-24: Sean remade as the lava brawler from Mayon; "wag na ung parol";
        // "his ult transforms him"; "7.0 s"). The direction is `docs/reports/sean-kit-2026-10-24/plan.md` section 3, from the
        // footage research beside it. The parol cutscene it replaces is kept whole in
        // `tools/cutscene_rework/old/HeroIntroductionScene.Sean.parol.cs.txt`.
        //
        // ONE SENTENCE: the mountain's heat comes up out of the ground into him until he cannot hold it, and he lets it out.
        // ONE TRAVELLING THING: the heat, always rising up the frame: cracks to his feet, up his body, up his forearm into his
        // fist and his eyes, then out of the court as the eruption.
        // ONE EMBLEM: Mayon's cone, three times: the mountain waking behind him, the glint in his eyes, the shape of the
        // fountain he slams out of the court.
        //   A. THE MOUNTAIN WAKES (0 to 2.1): from his feet; cracks of lava race to his tsinelas and the heat climbs him while
        //      the lens cranes up after it; the summit lights as it reaches his chest (1.4); a held breath.
        //   B. THE FIST (2.1 to 4.3): fist in front, face behind (Wriothesley, Lighter); a clench (2.25); lava wells out of his
        //      knuckles and runs UP his forearm, the fist swelling into a dripping lava fist; his eyes ignite (3.45); steam; held.
        //   C. ERUPTION (4.3 to 7.0): low, looking up at him (Zangief): the coil, both fists into the court at 5.6, the fountain
        //      in the cone's shape, the shockwave racing out. The lens rises and backs off to hold the ring and the real players.
        // ⚠️ IT ENDS AS THE WAVE REACHES THEM, NOT AFTER IT THROWS THEM. Play's awakening blast (`SeanHeroKit`, MAYON AWAKENS)
        // throws them the moment the cutscene hands back, so the throw is seen once (`docs/HERO_KIT_METHOD.md` section 6, "never
        // show it twice"). The staged players brace and lean as the wave reaches each of them; who is hit is play's rule.
        // ⚠️ HIS LAVA IS LIQUID AND HIS OWN (the look family, plan section 0): round drips, beads and splashes, gold to cherry
        // red, cooling to black glass; never Dante's stone slabs with orange seams.
        // ⚠️ BLOCKING. Every shape is a first pass built from typed meshes and the shared glow; nothing here has been filmed or
        // judged. Every time is on the table's clock (`tools/author_ultimate_intros.py`, `_sean`).
        // =========================================================================================
        private const float SnCracksIn = .45f, SnSummit = 1.4f, SnClench = 2.25f, SnRunFrom = 2.4f, SnRunTo = 3.2f,
                            SnIgnite = 3.45f, SnDraw = 4.1f, SnSlam = 5.6f, SnHome = 6.5f;
        private static readonly Color SnCore = new Color(1f, .78f, .23f, 1), SnEdge = new Color(.85f, .15f, .11f, 1),
                                      SnEmber = new Color(1f, .45f, .12f, 1), SnGlass = new Color(.09f, .07f, .1f, 1),
                                      SnSky = new Color(.23f, .08f, .12f, 1), SnMountain = new Color(.12f, .06f, .09f, 1),
                                      SnAsh = new Color(.45f, .42f, .42f, 1), SnSteam = new Color(.9f, .88f, .86f, 1);

        private int _snSky, _snMountain, _snSummit, _snColumn, _snColumnCore, _snFist, _snFistGlow, _snSlamPool, _snRing, _snRingThin;
        private readonly List<int> _snEmbers = new List<int>(18), _snAsh = new List<int>(10), _snVeins = new List<int>(6),
                                   _snRivulets = new List<int>(5), _snDrips = new List<int>(4), _snEyes = new List<int>(2),
                                   _snSteam = new List<int>(4), _snDrops = new List<int>(16), _snMoltenFists = new List<int>(2);
        private readonly List<LineRenderer> _snCracks = new List<LineRenderer>(6), _snBurstCracks = new List<LineRenderer>(8);
        private readonly List<Quaternion> _snOtherRest = new List<Quaternion>(4);

        private void BuildSean()
        {
            // The dusk, and the mountain standing up out of it behind him. Unlit, so the court's light never greys them.
            _snSky = Wall("SeanDusk", 0, 9f, SnSky, 8f, 28, .2f, cap: true); RagoUnlitBackdrop(_snSky);
            _snMountain = Add("Mayon", SnConeMesh(), SnMountain, .05f, plain: true); RagoUnlitBackdrop(_snMountain);
            _snSummit = AddGlow("MayonSummit", SnEmber, falloff: 2.4f, core: .5f);
            // The fountain he slams out of the court, in the cone's own shape (the emblem, third time).
            _snColumn = AddGlow("MayonColumn", SnEdge, SnConeMesh(), billboard: false, falloff: 1.2f, core: .35f);
            _snColumnCore = AddGlow("MayonColumnCore", SnCore, SnConeMesh(), billboard: false, falloff: 1.6f, core: .7f);

            // Shot A: the cracks that come across the court to his feet.
            for (int i = 0; i < 6; i++) _snCracks.Add(Line("SeanCrack" + i, 7, .07f, SnCore));
            for (int i = 0; i < 6; i++) _snVeins.Add(AddGlow("SeanVein" + i, SnEmber, falloff: 2.2f, core: .6f));
            for (int i = 0; i < 18; i++) _snEmbers.Add(AddGlow("SeanEmber" + i, i % 3 == 0 ? SnCore : SnEmber, falloff: 2.4f, core: .7f));
            for (int i = 0; i < 10; i++) _snAsh.Add(AddGlow("SeanAsh" + i, SnAsh, falloff: 1.6f, core: .4f));

            // Shot B: the lava running up his forearm into a dripping fist, his eyes, his breath.
            for (int i = 0; i < 5; i++) _snRivulets.Add(AddGlow("SeanRivulet" + i, i % 2 == 0 ? SnCore : SnEmber, falloff: 2f, core: .6f));
            _snFist = Add("SeanLavaFist", SnSphereMesh(), SnEdge, 1.1f);
            _snFistGlow = AddGlow("SeanFistGlow", SnCore, falloff: 2.2f, core: .55f);
            for (int i = 0; i < 4; i++) _snDrips.Add(AddGlow("SeanDrip" + i, SnCore, falloff: 2.4f, core: .8f));
            var glint = VfxShapes.Star(4, .22f);
            for (int i = 0; i < 2; i++) _snEyes.Add(AddGlow("SeanEyeGlint" + i, SnCore, glint, falloff: 1.4f, core: .8f));
            for (int i = 0; i < 4; i++) _snSteam.Add(AddGlow("SeanSteam" + i, SnSteam, falloff: 1.5f, core: .3f));

            // Shot C: the slam, the fountain's drops, the ring and the cracks it throws across the court, his fists Molten.
            _snSlamPool = AddGlow("SeanSlamPool", SnEdge, billboard: false, falloff: 1.8f, core: .5f);
            _snRing = AddGlow("SeanShockwave", SnEmber, SnRingMesh(.84f), billboard: false, falloff: 1.4f, core: .6f);
            _snRingThin = AddGlow("SeanShockwaveThin", SnCore, SnRingMesh(.94f), billboard: false, falloff: 1.4f, core: .8f);
            for (int i = 0; i < 16; i++) _snDrops.Add(AddGlow("SeanFountainDrop" + i, i % 2 == 0 ? SnCore : SnEdge, falloff: 2.2f, core: .7f));
            for (int i = 0; i < 8; i++) _snBurstCracks.Add(Line("SeanBurstCrack" + i, 6, .09f, SnEdge));
            for (int i = 0; i < 2; i++) _snMoltenFists.Add(AddGlow("SeanMoltenFist" + i, SnCore, falloff: 2.2f, core: .6f));

            // The real players, where they stand (never pulled in: who is caught is play's rule, not the stage's).
            StageOthers(1.0f, 40f);
            foreach (var other in _others) _snOtherRest.Add(other.Holder.transform.localRotation);
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
            const int sides = 56;
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

        private static Mesh _snSphere;
        private static Mesh SnSphereMesh()
        {
            if (_snSphere != null) return _snSphere;
            var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _snSphere = probe.GetComponent<MeshFilter>().sharedMesh;
            ObjectDestroy(probe);
            return _snSphere;
        }

        /// <summary>A fixed scatter in 0 to 1 for row <paramref name="i"/>: the same every frame and on every peer.</summary>
        private static float SnScatter(int i, int salt)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(salt * 19349663);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h & 0xffff) / 65535f;
        }

        /// <summary>The fist's palm: the free hand when he carries a slipper, his right otherwise.</summary>
        private Vector3 SnFistPalm => _heldItem != null ? FreePalm : RightPalm;

        // ------------------------------------------------------------------ the clock

        private void SampleSean(float t)
        {
            float present = StagePresence(t, .45f);
            float slam = Ease(SnSlam - .02f, SnSlam + .08f, t);
            var face = Quaternion.identity;

            // The world behind him: the dusk and the mountain, its summit lighting as the heat reaches his chest.
            Place(_snSky, new Vector3(0, 0, 0), new Vector3(8f, 9f, 8f), face, present * .95f);
            Place(_snMountain, new Vector3(0, -.2f, -7.3f), new Vector3(14f, 8.6f, 1f), face, present);
            float summit = Ease(SnSummit - .2f, SnSummit + .3f, t) * (1 + .6f * Flash(t, SnSlam, .25f));
            PlaceGlow(_snSummit, new Vector3(0, 8.3f, -7.2f), Vector3.one * (2.2f + 1.4f * summit), face, summit * 1.6f * present);

            SampleSeanCracks(t, present);
            SampleSeanBody(t, present);
            SampleSeanFist(t);
            SampleSeanEruption(t, slam);
            SampleSeanOthers(t);
        }

        private void SampleSeanCracks(float t, float present)
        {
            // Six cracks run in over the court from in front of him and meet at his feet by 0.45.
            for (int i = 0; i < _snCracks.Count; i++)
            {
                var line = _snCracks[i];
                float start = .05f * i, grow = Ease(start, SnCracksIn, t);
                float angle = (-60f + 24f * i + 10f * (SnScatter(i, 1) - .5f)) * Mathf.Deg2Rad;
                var from = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * (3.2f + .6f * SnScatter(i, 2));
                for (int k = 0; k < line.positionCount; k++)
                {
                    float u = k / (float)(line.positionCount - 1);
                    var along = Vector3.Lerp(from, new Vector3(0, 0, .25f), u * grow);
                    var side = new Vector3(from.z, 0, -from.x).normalized * (.18f * (SnScatter(i * 7 + k, 3) - .5f));
                    line.SetPosition(k, along + side * (1 - u) + Vector3.up * .02f);
                }
                float cool = 1 - Ease(SnSlam + .2f, Seconds, t) * .5f;
                line.widthMultiplier = .07f * grow * cool * present;
                line.enabled = grow > .01f && present > .01f;
            }
        }

        private void SampleSeanBody(float t, float present)
        {
            // The heat climbs him from his feet to his chest, 0.45 to 1.6, and stays in him.
            float climb = Ease(SnCracksIn, 1.6f, t);
            for (int i = 0; i < _snVeins.Count; i++)
            {
                float h = .15f + .25f * i;
                float lit = Mathf.Clamp01(climb * 6.5f - i) * present;
                var at = new Vector3(.08f * Mathf.Sin(i * 2.1f), h, .18f);
                PlaceGlow(_snVeins[i], at, Vector3.one * (.32f + .05f * Mathf.Sin(t * 9f + i)), Quaternion.identity, lit * .9f);
            }
            // Embers and ash in the air the whole time, two of them near the lens; they ride up past him.
            for (int i = 0; i < _snEmbers.Count; i++)
            {
                float speed = .35f + .4f * SnScatter(i, 4);
                float rise = Mathf.Repeat(SnScatter(i, 5) * 3.2f + t * speed, 3.2f);
                var at = new Vector3((SnScatter(i, 6) - .5f) * 3.4f, rise, .4f + SnScatter(i, 7) * 2.6f);
                if (i < 2) at = new Vector3(i == 0 ? -.35f : .3f, .35f + rise * .3f, 1.0f);
                float size = i < 2 ? .07f : .03f + .03f * SnScatter(i, 8);
                PlaceGlow(_snEmbers[i], at, Vector3.one * size * (1 + Flash(t, SnSlam, .3f)), Quaternion.identity, present * 1.4f);
            }
            for (int i = 0; i < _snAsh.Count; i++)
            {
                float fall = Mathf.Repeat(SnScatter(i, 9) * 4f - t * .25f, 4f);
                var at = new Vector3((SnScatter(i, 10) - .5f) * 4f, fall, -.5f + SnScatter(i, 11) * 3f);
                PlaceGlow(_snAsh[i], at, Vector3.one * (.05f + .03f * SnScatter(i, 12)), Quaternion.identity, present * .5f);
            }
        }

        private void SampleSeanFist(float t)
        {
            var palm = SnFistPalm;
            // Up the forearm: from the palm toward the shoulder on the same side.
            var shoulder = new Vector3(palm.x > 0 ? .24f : -.24f, 1.36f, 0);
            float run = Ease(SnRunFrom, SnRunTo, t), live = 1 - Ease(SnDraw, SnDraw + .25f, t);
            for (int i = 0; i < _snRivulets.Count; i++)
            {
                float u = Mathf.Clamp01(run * 1.25f - i * .06f) * (.2f + .16f * i);
                var at = Vector3.Lerp(palm, shoulder, u) + new Vector3(0, .03f * Mathf.Sin(t * 11f + i), 0);
                PlaceGlow(_snRivulets[i], at, Vector3.one * .1f, Quaternion.identity, run * live * 1.2f);
            }
            // The fist swells into a dripping lava fist, then stays one: Molten from the slam on.
            float swell = Ease(SnRunFrom, SnRunTo, t) * live + Ease(SnSlam - .1f, SnSlam + .1f, t);
            swell = Mathf.Clamp01(swell);
            float wobble = 1 + .05f * Mathf.Sin(t * 17f);
            Place(_snFist, palm, Vector3.one * (.17f + .1f * swell) * wobble, Quaternion.identity, swell);
            PlaceGlow(_snFistGlow, palm, Vector3.one * (.5f + .3f * swell), Quaternion.identity, swell * 1.1f);
            for (int i = 0; i < _snDrips.Count; i++)
            {
                float cycle = Mathf.Repeat(t * 1.6f + i * .27f, 1f);
                var at = palm + new Vector3(.05f * (i - 1.5f), -cycle * palm.y, .02f);
                float on = swell * (cycle < .95f ? 1 : 0) * (t > SnRunFrom ? 1 : 0);
                PlaceGlow(_snDrips[i], at, Vector3.one * .05f, Quaternion.identity, on * 1.3f);
            }
            // His eyes ignite at 3.45 (the emblem's second time): a four-pointed glint on each.
            var head = HeadPoint + new Vector3(0, -.32f, .17f);
            float eyes = Ease(SnIgnite - .05f, SnIgnite + .08f, t) * (1 - Ease(SnDraw + .4f, SnDraw + .9f, t)) * (1 + 1.5f * Flash(t, SnIgnite + .05f, .12f));
            for (int i = 0; i < _snEyes.Count; i++)
                PlaceGlow(_snEyes[i], head + new Vector3(i == 0 ? -.075f : .075f, 0, 0), Vector3.one * (.16f + .2f * Flash(t, SnIgnite + .05f, .15f)),
                    Quaternion.Euler(0, 0, 45f * t), eyes * 1.4f);
            // The breath out, 3.6 to 4.1.
            float steam = Ease(3.55f, 3.7f, t) * (1 - Ease(3.9f, 4.3f, t));
            for (int i = 0; i < _snSteam.Count; i++)
            {
                float k = Mathf.Clamp01((t - 3.6f) * 2f - i * .15f);
                PlaceGlow(_snSteam[i], head + new Vector3(.05f * i - .08f, -.05f + .25f * k, .15f + .45f * k), Vector3.one * (.1f + .25f * k), Quaternion.identity, steam * .45f * (1 - k * .6f));
            }
        }

        private void SampleSeanEruption(float t, float slam)
        {
            float after = Mathf.Max(0, t - SnSlam);
            var fists = new Vector3(0, .05f, .55f);
            // The fountain in the cone's shape behind him: up fast, then it stands; its core flares on the slam.
            float column = slam * (1 - .35f * Ease(SnSlam + .6f, Seconds, t));
            float rise = Ease(SnSlam, SnSlam + .3f, t);
            PlaceGlow(_snColumn, new Vector3(0, 0, -.7f), new Vector3(3.4f, 6.4f * rise, 1), Quaternion.identity, column * 1.1f);
            PlaceGlow(_snColumnCore, new Vector3(0, 0, -.68f), new Vector3(1.7f, 5.2f * rise, 1), Quaternion.identity, column * (1.2f + 1.5f * Flash(t, SnSlam + .05f, .2f)));
            // The pool where both fists hit the court.
            PlaceGlow(_snSlamPool, fists + Vector3.up * .01f, new Vector3(1.6f + .6f * slam, 1.6f + .6f * slam, 1), Quaternion.Euler(90, 0, 0), slam * 1.2f * (1 - Ease(SnSlam + 1f, Seconds, t) * .4f));
            // The shockwave: its leading edge reaches play's awakening radius exactly as the cutscene ends.
            float reach = SeanRules.AwakeningRadius * Ease(SnSlam, Seconds, t);
            PlaceGlow(_snRing, new Vector3(0, .04f, 0), new Vector3(reach, 1, reach), Quaternion.identity, slam * 1.3f);
            PlaceGlow(_snRingThin, new Vector3(0, .05f, 0), new Vector3(reach * 1.04f, 1, reach * 1.04f), Quaternion.identity, slam * 1.6f);
            for (int i = 0; i < _snBurstCracks.Count; i++)
            {
                var line = _snBurstCracks[i];
                float angle = (i * 45f + 20f * (SnScatter(i, 13) - .5f)) * Mathf.Deg2Rad;
                var way = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                for (int k = 0; k < line.positionCount; k++)
                {
                    float u = k / (float)(line.positionCount - 1);
                    var side = new Vector3(way.z, 0, -way.x) * (.22f * (SnScatter(i * 9 + k, 14) - .5f));
                    line.SetPosition(k, way * (.3f + reach * .95f * u) + side * u + Vector3.up * .02f);
                }
                line.widthMultiplier = .09f * slam;
                line.enabled = slam > .01f;
            }
            // The fountain's drops: thrown up out of the cone and falling back, on fixed arcs.
            for (int i = 0; i < _snDrops.Count; i++)
            {
                float out_ = .8f + 2.2f * SnScatter(i, 15), up = 5f + 4f * SnScatter(i, 16);
                float angle = (i * 22.5f + 10f * SnScatter(i, 17)) * Mathf.Deg2Rad;
                var way = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle) * .6f - .4f);
                float k = after * (.9f + .3f * SnScatter(i, 18));
                var at = new Vector3(0, .3f, -.6f) + way * out_ * k + Vector3.up * (up * k - 5f * k * k);
                float on = slam * (at.y > 0 ? 1 : 0);
                PlaceGlow(_snDrops[i], at, Vector3.one * (.09f + .06f * SnScatter(i, 19)), Quaternion.identity, on * 1.3f);
            }
            // Molten: both his fists are lava from the slam on (one, when the other carries a slipper).
            var palms = new[] { _heldItem != null ? FreePalm : RightPalm, FreePalm };
            for (int i = 0; i < _snMoltenFists.Count; i++)
            {
                bool shown = i == 0 || _heldItem == null;
                PlaceGlow(_snMoltenFists[i], palms[i], Vector3.one * (.45f + .08f * Mathf.Sin(t * 13f + i)), Quaternion.identity, shown ? slam * 1.3f : 0);
            }
        }

        private void SampleSeanOthers(float t)
        {
            // Each staged player braces and leans away as the wave reaches them; it reaches 4 m at the last frame.
            for (int i = 0; i < _others.Count; i++)
            {
                var other = _others[i];
                if (other.Holder == null) continue;
                if (!other.Holder.activeSelf) other.Holder.SetActive(true);
                float reachedAt = SnSlam + (Seconds - SnSlam) * Mathf.Clamp01(other.Reach / SeanRules.AwakeningRadius);
                float lean = other.Reach <= SeanRules.AwakeningRadius ? Ease(reachedAt - .12f, reachedAt + .05f, t) : 0;
                var away = other.Home; away.y = 0;
                var axis = Vector3.Cross(Vector3.up, away.sqrMagnitude > .001f ? away.normalized : Vector3.forward);
                other.Holder.transform.localRotation = Quaternion.AngleAxis(14f * lean, axis) * _snOtherRest[i];
            }
        }

        // ------------------------------------------------------------------ the lens, the grade, the hand-back

        private void SeanFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            // The rising, wide last shot never stands in a staged body.
            if (index >= 2) eye = KeepLensOffOthers(eye, 1.3f);
        }

        /// <summary>The court steps back to his dusk while the heat is in him, flares on the slam, and comes back for play.</summary>
        private void SeanGrade(float t, out float brightness, out float saturation)
        {
            float on = Ease(0, .45f, t) * (1 - Ease(Seconds - .4f, Seconds, t));
            brightness = Mathf.Lerp(1f, .72f, on) * (1 + .3f * Flash(t, SnSlam + .03f, .12f));
            saturation = Mathf.Lerp(1f, .85f, on);
        }

        /// <summary>
        /// ⚠️ HIS OWN SCREEN COMES HOME TO HIS OWN EYES (kept from the parol's cutscene, owner 2026-10-09). From `SnHome` the lens
        /// leaves the last shot for where his first-person camera will be the moment play resumes: the live camera's own place
        /// and aim (the world is paused). MAYON AWAKENS has no leap, so there is no rise. His copy is not drawn once the lens is
        /// nearly in it. World positions.
        /// </summary>
        private void SeanHandBack(float t, Camera live, ref Vector3 eye, ref Vector3 target, ref float fov)
        {
            float home = Ease(SnHome, Seconds - .06f, t);
            if (home <= 0) return;
            var eyes = live.transform.position;
            eye = Vector3.Lerp(eye, eyes, home);
            target = Vector3.Lerp(target, eyes + live.transform.forward * 8f, home);
            fov = Mathf.Lerp(fov, live.fieldOfView, home);
            if (home > .7f) foreach (var surface in _bodyRenderers) if (surface != null) surface.enabled = false;
        }

        /// <summary>An unlit backdrop piece (the dusk, the mountain), so the court's light never greys it. Cheska's stage uses it too.</summary>
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
