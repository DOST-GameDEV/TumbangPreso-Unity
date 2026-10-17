using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO, CONTINENTAL DRIFT, 7.0 s. REBUILT 2026-10-16 (`docs/reports/dante-presentation-2026-10-16/plan.md` section 5, owner
        // approved: *"looks good pede na go"*; the bar, his words: *"it has to be as good as paete, nemu, phaister and amihans ult"*).
        //
        // ONE SENTENCE: he knocks on the court like a door, then strikes it; his fire runs through the cracks and splits it into
        // continents, wakes the horned mountain beast beneath, and it lifts him to deliver the blow.
        //
        // What the four approved ultimates all do, and his answer to each (plan 5.0):
        //  * a PERSONALITY beat: his is PATIENCE. Everyone else's starts fast; his starts STILL, on one knee, palm on the court,
        //    listening, and knocks twice, calm, like on a door. Only the third blow is violent. "Refuses to be rushed."
        //  * the WORLD changes: the court breaks into continents on a molten sea (the ultimate's own name, literal).
        //  * a GIANT from his myth: the horned basalt beast under Montalban, Bernardo Carpio's mountain (`DanteProp` "beast", a
        //    typed glb in his own family: his stepped horn, the left one chipped like his scar side).
        //  * a CLOSE-UP of its eyes as the climax: its sockets, cut into the rock, ignite at the lens (3.75).
        //  * the real VICTIMS on screen: the players the cascade will reach, stumbling on their own drifting plates, then small in
        //    the far court bracing as the fists come down (`HeroIntroductionScene.DanteTake.cs`).
        //  * a finish with WEIGHT: both fists land as one, his on its back, its on the court; the cut is on the contact.
        //
        // THE BEATS (the body's keys and the shots are the table's: `tools/author_ultimate_intros.py`, `_dante_drift`):
        //   0.00 THE KNOCK      still, kneeling, listening; dust already hangs in the air
        //   0.45, 0.80          two calm knocks: a thin molten ring runs out under the court from each; the second lights his hand's cracks
        //   0.95 to 1.15        an insert: his gold eye opens (the crest's motif sounds)
        //   1.15 THE PUNCH      the hanging grit is pulled into his cocked fist; 1.30 to 1.50 held; 1.55 it lands: an impact frame,
        //                       a shockwave, the crest cracked into the court for a quarter second
        //   1.70 THE CONTINENTS six molten seams race out from his fist, the plates jolt apart in two steps (1.95, 2.30), the sea's
        //                       glow comes up; the victims stumble on their plates; 3.10 the light dives into the widest seam
        //   3.30 THE RISE       face first: its horn tips break the surface, then its brow; 3.75 its eyes ignite at the lens (an
        //                       impact frame), the crest lit on its brow; three heaves (4.0, 4.5, 4.95) carry his plate up on its back
        //   5.30 THE STOMP      it raises its right fist; he loads both of his; the light runs up its arm and down his;
        //                       6.60 to 6.85 THE HANG (the story clock at a fifth); 6.88 both fists land as one; the cut, on contact
        // ⚠️ NO BLAST IS SHOWN AND NOTHING GOES BACK UNDER (owner, 2026-10-08): it ends on the fist with him still on its back; the
        // going under and the five blasts are live play's (`DanteDriftLive`). So the stage is whole on the last frame.
        //
        // THE TRAVELLING THING is the molten light: his fist, the seams, the dive, its eyes, up its arm and down his, always
        // travelling left to right across the screen; every shot is on his right side of the line (+x).
        // THE CREST (horns over a split stone, left horn chipped) is drawn three times only: in the court at the punch, on its brow
        // as it wakes, huge behind them at the stomp, each with the motif.
        //
        // RULES (the method's traps): nothing here runs on `Update`, everything is posed from the scene clock; flat effects sit on
        // the court (`Slipper.GroundY` is under `_ground`); light is cut INTO stone (the beast's sockets and seams are SoulGlow
        // holes), never a raised ridge; the hottest colour anywhere is molten hot (1, .62, .18), never white; stone ends by cooling
        // and crumbling, never an alpha fade. The density pass (grit, rings, motes, curtain, rays, near layer) is `DanteBurst.cs`.
        // =========================================================================================
        private const float DnKnock1 = .45f, DnKnock2 = .80f, DnEyeOpen = .95f, DnWind = 1.15f, DnPunch = 1.55f, DnBreak = 1.57f,
                            DnSplit1 = 1.95f, DnSplit2 = 2.30f, DnDive = 3.10f, DnHorns = 3.30f, DnIgnite = 3.75f, DnRisen = 5.20f,
                            DnLoad = 5.30f, DnHangFrom = 6.60f, DnHangTo = 6.85f, DnStomp = 6.88f, DnTitanTop = 3.4f;
        private static readonly float[] DnHeaves = { 4.0f, 4.5f, 4.95f };
        // The court's own surface over the stage root (`Slipper.GroundY`, the method's section 8 trap): Bayan Plaza's court stands
        // above the ground the root is found on, and a fixed 0.15 m left every plate under it (film r1). Measured once at the build.
        private float _dnCourt;
        private float DnFloat => _dnCourt + .03f;
        // The story clock in the hang runs at a fifth (Amihan's hang, his version): VFX motion reads it, the body's keys do not.
        private const float DnHangSpeed = .2f;

        // The centre plate's corners (x, z), then one kink and one far corner per crack running out from each of them.
        private static readonly Vector2[] DnCore =
        { new Vector2(1.55f, .45f), new Vector2(.70f, 1.75f), new Vector2(-.95f, 1.50f), new Vector2(-1.70f, .05f), new Vector2(-.80f, -1.55f), new Vector2(.95f, -1.40f) };
        private static readonly Vector2[] DnKink =
        { new Vector2(5.3f, .6f), new Vector2(1.2f, 5.6f), new Vector2(-3.9f, 4.1f), new Vector2(-5.6f, -.9f), new Vector2(-2.0f, -5.2f), new Vector2(3.7f, -4.3f) };
        private static readonly Vector2[] DnFar =
        { new Vector2(10.6f, 2.6f), new Vector2(3.9f, 10.1f), new Vector2(-7.0f, 8.2f), new Vector2(-10.7f, -.4f), new Vector2(-5.4f, -9.3f), new Vector2(6.6f, -8.5f) };
        // The far edge of each outer plate bulges through one point between its two cracks.
        private static readonly Vector2[] DnRim =
        { new Vector2(8.2f, 7.4f), new Vector2(-1.6f, 11.0f), new Vector2(-10.2f, 4.4f), new Vector2(-9.0f, -6.2f), new Vector2(.8f, -10.9f), new Vector2(10.4f, -3.6f) };
        // Each outer plate's own share of the drift, its tilt once adrift (degrees about x and z) and its turn.
        private static readonly float[] DnShare = { 1.00f, .82f, 1.18f, .90f, 1.10f, .95f };
        private static readonly Vector3[] DnTilt =
        { new Vector3(3.5f, 5f, -2.5f), new Vector3(-4f, -3f, 1.5f), new Vector3(2f, 6f, 4f), new Vector3(-2.5f, -4f, -3.5f), new Vector3(4.5f, 2f, 2f), new Vector3(-3f, -6f, 3f) };
        // The order the six seams race out from his fist (plate cracks), and each one's delay: the +x side first (left to right
        // on his right-side shots), then round.
        private static readonly float[] DnSeamDelay = { 0f, .05f, .14f, .20f, .11f, .03f };

        // THE BEAST'S RISE, typed keys (time, root height, nose-up pitch in degrees). Face first: it comes up pitched nose-up so the
        // horns and then the eyes break the surface before its back; it levels as the three heaves carry the saddle (3.4 m on the
        // model) up under his plate. From 3.95 its saddle and his lift agree (`DnBeastPose`).
        private static readonly Vector3[] DnRiseKeys =
        {
            new Vector3(3.20f, -6.0f, -36f), new Vector3(3.30f, -5.3f, -38f), new Vector3(3.55f, -3.5f, -41f), new Vector3(3.75f, -2.7f, -42f),
            new Vector3(3.95f, -2.8f, -38f), new Vector3(4.00f, -2.95f, -27f), new Vector3(4.20f, -2.86f, -24f), new Vector3(4.50f, -1.75f, -15f), new Vector3(4.70f, -1.62f, -12f),
            new Vector3(4.95f, -.45f, -5f), new Vector3(5.20f, 0f, 0f), new Vector3(7.0f, 0f, 0f),
        };

        private int _dnDustLow, _dnDustHigh, _dnSea, _dnBlastGlow, _dnEyeGold, _dnHandCrack, _dnHandCrackGlow;
        private readonly List<Transform> _dnPlates = new List<Transform>(7);
        private readonly List<Vector2> _dnPlateHome = new List<Vector2>(7);
        private readonly List<int> _dnSeams = new List<int>(6), _dnKnockRings = new List<int>(2);
        private int _dnCrestCourt, _dnCrestBrow, _dnCrestSky;
        private Transform _dnTitan, _dnTitanArm, _dnTitanJaw, _dnTitanHead;
        private Quaternion _dnArmRest, _dnJawRest, _dnHeadRest;
        private GameObject _dnBeast;
        private Material _dnImpact;

        private void BuildDante()
        {
            float court = Slipper.GroundY(_root.transform.position + Vector3.up * .3f) - _root.transform.position.y;
            _dnCourt = float.IsInfinity(court) || float.IsNaN(court) ? 0f : Mathf.Clamp(court, 0f, .6f);
            _dnDustLow = Wall("DustGround", 0, 1.4f, new Color(.2f, .14f, .09f, .8f), radius: 15f);
            _dnDustHigh = Wall("DustSky", 1.4f, 30, new Color(.42f, .32f, .22f, .72f), radius: 15f, emission: .08f, cap: true);
            // A deep molten red-orange, not molten hot: the sea is the ground the light lives in, so the seams stay the brightest
            // thing on the court (film r3: a flat (1, .36, .05) sea outshone every seam).
            _dnSea = Add("MoltenSea", VfxShapes.Prism(28, .02f, 1f), new Color(.72f, .2f, .04f, 1f), .7f, plain: true);

            // The plates: the centre one he kneels on, then six round it, each between two cracks.
            var stone = new Color(.31f, .27f, .24f, 1);
            _dnPlates.Add(DantePlate("PlateCentre", DnCore, stone, out var home)); _dnPlateHome.Add(home);
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                var ring = new[] { DnCore[i], DnKink[i], DnFar[i], DnRim[i], DnFar[j], DnKink[j], DnCore[j] };
                var tone = stone * (i % 2 == 0 ? 1f : .9f); tone.a = 1;
                _dnPlates.Add(DantePlate("Plate" + i, ring, tone, out home)); _dnPlateHome.Add(home);
            }
            // Six main seams along the plate cracks: the light running out from his fist (its mesh is the crack's own path).
            for (int i = 0; i < 6; i++)
                _dnSeams.Add(AddGlow("DanteSeam" + i, new Color(1f, .5f, .1f), DnSeamMesh(i), billboard: false, band: true, falloff: 1.4f, core: .55f));
            // The knocks' thin molten rings under the court surface, and his hand's hairline cracks.
            for (int i = 0; i < 2; i++)
                _dnKnockRings.Add(AddGlow("DanteKnockRing" + i, new Color(1f, .45f, .09f), DnRingMesh, billboard: false, band: true, falloff: 1.2f, core: .4f));
            _dnHandCrackGlow = Add("DanteHandCrackGlow", VfxShapes.Fracture(5, 2, .07f, 31), new Color(1f, .42f, .08f, .5f), .8f);
            _dnHandCrack = Add("DanteHandCrack", VfxShapes.Fracture(5, 2, .03f, 31), new Color(1f, .62f, .18f, .95f), 1f);
            // His gold eye catching the seam light from below.
            _dnEyeGold = AddGlow("DanteGoldEye", new Color(1f, .62f, .18f), falloff: 2.4f, core: .9f, lift: .04f);
            // The crest, three times only.
            _dnCrestCourt = AddGlow("DanteCrestCourt", new Color(1f, .45f, .09f), DnCrestMesh, billboard: false, band: false, falloff: 1.1f, core: .35f);
            _dnCrestBrow = AddGlow("DanteCrestBrow", new Color(1f, .5f, .1f), DnCrestMesh, billboard: false, falloff: 1.1f, core: .35f);
            _dnCrestSky = AddGlow("DanteCrestSky", new Color(1f, .42f, .08f), DnCrestMesh, billboard: false, falloff: 1.1f, core: .3f);

            BuildDanteBeast();
            BuildDanteBurst();
            BuildDanteTake();

            var strip = new[] { new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(.5f, .5f), new Vector2(-.5f, .5f) };
            _dnBlastGlow = Add("BlastSeam", DantePlateMesh(strip, Vector2.zero, .02f), new Color(1, .5f, .1f, .9f), 1f);
        }

        /// <summary>The beast under the court: the typed glb, its named nodes found for the pose.</summary>
        private void BuildDanteBeast()
        {
            _dnTitan = new GameObject("MountainBeast").transform; _dnTitan.SetParent(_root.transform, false);
            _dnBeast = DanteProp.Spawn("beast", _dnTitan);
            if (_dnBeast == null) return;
            _dnTitanArm = DanteProp.Find(_dnBeast, "arm-right");
            _dnTitanJaw = DanteProp.Find(_dnBeast, "jaw");
            _dnTitanHead = DanteProp.Find(_dnBeast, "head");
            if (_dnTitanArm != null) _dnArmRest = _dnTitanArm.localRotation;
            if (_dnTitanJaw != null) _dnJawRest = _dnTitanJaw.localRotation;
            if (_dnTitanHead != null) _dnHeadRest = _dnTitanHead.localRotation;
            foreach (var r in _dnBeast.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            DanteProp.SetGlow(_dnBeast, "glow-eyes", 0f);
            DanteProp.SetGlow(_dnBeast, "glow-seams", 0f);
        }

        /// <summary>One plate of the court: a slab under its own holder, which sits on the plate's middle so it tilts about itself.</summary>
        private Transform DantePlate(string name, Vector2[] ring, Color colour, out Vector2 home)
        {
            home = Vector2.zero;
            foreach (var p in ring) home += p;
            home /= ring.Length;
            var holder = new GameObject(name).transform; holder.SetParent(_root.transform, false);
            int index = BasilioStone(name + "Slab", DantePlateMesh(ring, home, .55f), colour);
            _pieces[index].Transform.SetParent(holder, false);
            Place(index, Vector3.zero, Vector3.one, Quaternion.identity, 1);
            return holder;
        }

        /// <summary>A flat-topped slab from an outline (x, z) round its middle: a top fan and one wall per edge, every face its own.</summary>
        private static Mesh DantePlateMesh(Vector2[] ring, Vector2 middle, float thick)
        {
            var vertices = new List<Vector3>(ring.Length * 9); var triangles = new List<int>(ring.Length * 9);
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { var keep = b; b = c; c = keep; }
                int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 p = ring[i] - middle, q = ring[(i + 1) % ring.Length] - middle;
                Vector3 a = new Vector3(p.x, 0, p.y), b = new Vector3(q.x, 0, q.y), down = Vector3.down * thick;
                Face(Vector3.zero, a, b, Vector3.up);
                var outward = (a + b) * .5f;
                Face(a, b, b + down, outward); Face(a, b + down, a + down, outward);
            }
            var mesh = new Mesh { name = "Basilio court plate" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private int BasilioStone(string name, Mesh mesh, Color colour)
        {
            int index = AddSolid(name, mesh, colour);
            // Opaque lit facets with a small local fill, so the dusk stage cannot turn the moving slabs into black cutouts.
            var material = _pieces[index].Renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * .45f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return index;
        }

        /// <summary>
        /// One seam as a flat band along crack i (from the centre plate's corner, through its kink, to the far corner), v across it.
        /// It is drawn unit length along its own path and revealed by scaling nothing: `DnSeamReveal` clips it by the uv.u written here.
        /// </summary>
        private static Mesh DnSeamMesh(int i)
        {
            var path = new[] { DnCore[i], DnKink[i], DnFar[i] };
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float total = Vector2.Distance(path[0], path[1]) + Vector2.Distance(path[1], path[2]), run = 0f;
            for (int k = 0; k < path.Length; k++)
            {
                var dir = k < path.Length - 1 ? (path[k + 1] - path[k]).normalized : (path[k] - path[k - 1]).normalized;
                var side = new Vector2(-dir.y, dir.x) * (k == 0 ? .13f : k == 1 ? .11f : .05f);
                if (k > 0) run += Vector2.Distance(path[k - 1], path[k]);
                v.Add(new Vector3(path[k].x - side.x, 0f, path[k].y - side.y)); uv.Add(new Vector2(run / total, 0f));
                v.Add(new Vector3(path[k].x + side.x, 0f, path[k].y + side.y)); uv.Add(new Vector2(run / total, 1f));
                if (k == 0) continue;
                int n = v.Count;
                t.AddRange(new[] { n - 4, n - 2, n - 3, n - 3, n - 2, n - 1 });
            }
            var m = new Mesh { name = "Basilio seam " + i };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return VfxShapes.TwoSided(m);
        }

        /// <summary>A flat ring in XZ, unit outer radius, its band thin, v across it: the knocks' rings under the court.</summary>
        private static Mesh DnRingMesh
        {
            get
            {
                if (_dnRingMesh != null) return _dnRingMesh;
                const int Sides = 64;
                var v = new Vector3[(Sides + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[Sides * 6];
                for (int i = 0; i <= Sides; i++)
                {
                    float a = i * Mathf.PI * 2f / Sides;
                    var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    v[i * 2] = d * .9f; v[i * 2 + 1] = d;
                    uv[i * 2] = new Vector2(i / (float)Sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Sides, 1f);
                    if (i == Sides) continue;
                    int b = i * 6, k = i * 2;
                    tris[b] = k; tris[b + 1] = k + 2; tris[b + 2] = k + 1; tris[b + 3] = k + 2; tris[b + 4] = k + 3; tris[b + 5] = k + 1;
                }
                _dnRingMesh = new Mesh { name = "BasilioKnockRing", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tris };
                _dnRingMesh = VfxShapes.TwoSided(_dnRingMesh);
                return _dnRingMesh;
            }
        }
        private static Mesh _dnRingMesh, _dnCrestMeshCache;

        /// <summary>
        /// THE CREST: two hooked horns over a split stone, the LEFT horn chipped (his scar side), flat in XY, about a unit tall.
        /// Every outline typed: the stone is two halves with a gap down its middle; each horn sweeps out and hooks in, the left
        /// one stopping short at a slanted break.
        /// </summary>
        private static Mesh DnCrestMesh
        {
            get
            {
                if (_dnCrestMeshCache != null) return _dnCrestMeshCache;
                // SpiritGlow fades by the uv's distance from its middle, so each piece's centre is uv (.5, .5), hottest, and its
                // outline sits part way out (film r8: with no uv every crest read uv 0, past the falloff, and never drew at all).
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
                void Poly(params float[] xy)
                {
                    int start = v.Count, n = xy.Length / 2;
                    float cx = 0f, cy = 0f; for (int i = 0; i < n; i++) { cx += xy[i * 2]; cy += xy[i * 2 + 1]; }
                    v.Add(new Vector3(cx / n, cy / n, 0f)); uv.Add(new Vector2(.5f, .5f));
                    for (int i = 0; i < n; i++) { v.Add(new Vector3(xy[i * 2], xy[i * 2 + 1], 0f)); uv.Add(new Vector2(.72f, .5f)); }
                    for (int i = 0; i < n; i++) { t.Add(start); t.Add(start + 1 + i); t.Add(start + 1 + (i + 1) % n); }
                }
                // The split stone: two halves, a gap between, each chipped differently.
                Poly(-.42f, -.50f, -.04f, -.50f, -.07f, -.18f, -.02f, .02f, -.06f, .12f, -.38f, .14f, -.48f, -.06f);
                Poly(.04f, -.50f, .44f, -.50f, .50f, -.10f, .40f, .14f, .07f, .12f, .03f, -.04f, .08f, -.22f);
                // The right horn (viewer's left when it faces you is its right; drawn as worn: right on +x): out, up, hooked in.
                Poly(.10f, .18f, .30f, .16f, .52f, .26f, .66f, .46f, .70f, .70f, .60f, .88f, .46f, .96f, .52f, .80f, .54f, .62f, .44f, .44f, .24f, .30f);
                // The left horn, chipped: the same sweep, stopping at a slanted break below its hook.
                Poly(-.10f, .18f, -.30f, .16f, -.52f, .26f, -.66f, .46f, -.71f, .64f, -.60f, .70f, -.54f, .58f, -.44f, .44f, -.24f, .30f);
                var m = new Mesh { name = "Basilio crest", hideFlags = HideFlags.DontSave };
                m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
                _dnCrestMeshCache = VfxShapes.TwoSided(m);
                return _dnCrestMeshCache;
            }
        }

        private static float DanteKick(float t, float at, float size, float fade)
            => t < at ? 0 : size * Mathf.Clamp01(1 - (t - at) / fade);

        /// <summary>The lens feels it: the punch (four hard frames), each heave, and the stomp it ends on.</summary>
        private Vector3 DanteShake(float t)
        {
            float size = DanteKick(t, DnPunch, .07f, .14f) + DanteKick(t, DnStomp, .13f, .12f)
                       + DanteKick(t, DnHeaves[0], .03f, .3f) + DanteKick(t, DnHeaves[1], .035f, .3f) + DanteKick(t, DnHeaves[2], .04f, .3f)
                       + DanteKick(t, DnSplit1, .02f, .2f) + DanteKick(t, DnSplit2, .025f, .2f);
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 93f + 1.3f), 0) * size;
        }

        /// <summary>The story clock: the scene clock everywhere but THE HANG, where it runs at a fifth, eased at its edges.</summary>
        private static float DanteStory(float t)
        {
            if (t <= DnHangFrom) return t;
            float inside = Mathf.Min(t, DnHangTo) - DnHangFrom;
            float after = Mathf.Max(0f, t - DnHangTo);
            return DnHangFrom + inside * DnHangSpeed + after;
        }

        /// <summary>The backdrop steps back from the seams on (0.7), and lifts toward 0.9 for the stomp.</summary>
        private void DanteGrade(float t, out float brightness, out float saturation)
        {
            float back = Ease(DnBreak, DnBreak + .3f, t) * (1f - .65f * Ease(DnLoad, DnStomp, t));
            brightness = Mathf.Lerp(1f, .7f, back) * (1f - .1f * Ease(0f, .3f, t) * (1f - Ease(DnBreak, DnBreak + .2f, t)));
            saturation = Mathf.Lerp(1f, .82f, back);
        }

        /// <summary>The three impact frames: two frames each, ink on molten orange, lines in to the hit.</summary>
        private void DantePostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects) return;
            float impact = 0f; Vector3 at = Vector3.zero;
            if (t >= DnPunch && t < DnPunch + .067f) { impact = 1f; at = DnFist; }
            else if (t >= DnIgnite && t < DnIgnite + .067f) { impact = .9f; at = DnEyesAt(t); }
            else if (t >= DnStomp && t < DnStomp + .067f) { impact = 1f; at = new Vector3(2.75f, DnFloat + .1f, 3.4f); }
            if (impact <= 0f || camera == null) return;
            if (_dnImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/DanteImpact");
                if (shader == null) return;
                _dnImpact = new Material(shader) { name = "DanteImpact" };
            }
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(at));
            _dnImpact.SetFloat("_Impact", impact);
            _dnImpact.SetVector("_Focus", new Vector4(Mathf.Clamp(vp.x, .1f, .9f), Mathf.Clamp(vp.y, .1f, .9f), 0f, 0f));
            _dnImpact.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            _dnImpact.SetFloat("_Seed", t < 2f ? 1f : t < 5f ? 2f : 3f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _dnImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        // Where his fist lands, just ahead of his feet on his knock side (the table's punch: the right fist, the left when held).
        // Caught from his real knocking hand at the punch (`DanteFrame.cs`), so the impact, the crest and the seams start under the fist.
        private Vector3 DnFist => _dnFistAt ?? new Vector3(_heldItem != null ? -.15f : .15f, DnFloat + .02f, .85f);

        /// <summary>The beast's root pose at t: height and nose-up pitch from the typed keys.</summary>
        private void DnBeastPose(float t, out float height, out float pitch)
        {
            if (t <= DnRiseKeys[0].x) { height = DnRiseKeys[0].y; pitch = DnRiseKeys[0].z; return; }
            for (int i = 1; i < DnRiseKeys.Length; i++)
            {
                if (t > DnRiseKeys[i].x) continue;
                var a = DnRiseKeys[i - 1]; var b = DnRiseKeys[i];
                // Heavy things move in jolts and holds: a quick smoothstep between keys, never a glide.
                float u = Mathf.InverseLerp(a.x, b.x, t); u = u * u * (3f - 2f * u);
                height = Mathf.Lerp(a.y, b.y, u); pitch = Mathf.Lerp(a.z, b.z, u); return;
            }
            height = DnRiseKeys[DnRiseKeys.Length - 1].y; pitch = DnRiseKeys[DnRiseKeys.Length - 1].z;
        }

        // It comes up a body length AHEAD of him, out of the widest seam between him and the targets (plan 5.3, 3.30: the light
        // dove there), so its face never rises through his own plate; the three heaves slide it back under him as they lift him.
        private const float DnSurgeAhead = 1.1f;
        private static float DnSurge(float t) => DnSurgeAhead * (1f - Ease(DnHeaves[0], DnRisen, t));

        // Its eyes in the beast's own space (the glb's head node at (0, 1.62, 1.95), the sockets 1.03 in front, 0.17 up).
        private static readonly Vector3 DnEyesLocal = new Vector3(0f, 1.79f, 2.98f);
        private Vector3 DnEyesAt(float t) => _dnTitan != null ? _root.transform.InverseTransformPoint(_dnTitan.TransformPoint(DnEyesLocal)) : new Vector3(0, 1.2f, 3.3f);

        private void SampleDante(float t)
        {
            float story = DanteStory(t);
            DnCatchContacts(t);
            float haze = Ease(.0f, .6f, t);
            Tint(_dnDustLow, haze); Tint(_dnDustHigh, haze);

            float lift = LiftAt(t);
            float plateLift = Mathf.Max(0f, lift);
            bool broken = t >= DnBreak;
            // The plates jolt apart in two steps (1.95, 2.30), each a jolt then a hold; then drift slowly on; shoved aside by what rises.
            float jolt1 = Ease(DnSplit1, DnSplit1 + .07f, t), jolt2 = Ease(DnSplit2, DnSplit2 + .07f, t);
            float adrift = Ease(DnBreak, DnBreak + .12f, t) * .12f + jolt1 * .38f + jolt2 * .45f + Ease(DnSplit2 + .1f, DnHorns, story) * .35f;
            float shoved = Ease(DnHeaves[0], DnRisen, t) * 2.1f;
            float open = adrift + shoved;

            // The sea under everything, breathing; its glow comes up as the gaps open.
            float glow = _reducedEffects ? .85f : .82f + .18f * Mathf.Sin(story * 5.2f);
            Place(_dnSea, new Vector3(0, _dnCourt + .008f, 0), new Vector3(15f, 1, 15f), Quaternion.identity, broken ? glow * Ease(DnBreak, DnSplit1, t) : 0);

            for (int i = 0; i < _dnPlates.Count; i++)
            {
                var plate = _dnPlates[i]; var home = _dnPlateHome[i];
                // Whole before the punch: one court; after it, the plates (`_dnSea` shows in the gaps).
                plate.gameObject.SetActive(true);
                if (i == 0)
                {
                    plate.localPosition = new Vector3(home.x, DnFloat + plateLift, home.y);
                    float ride = Mathf.Clamp01(plateLift);
                    plate.localRotation = Quaternion.Euler(Mathf.Sin(story * 2.3f) * 1.2f * ride, 0, Mathf.Sin(story * 1.9f + 1) * 1.2f * ride);
                    continue;
                }
                int k = i - 1;
                Vector2 away = home.normalized * open * DnShare[k];
                float loose = Mathf.Clamp01(open / .9f), heel = Mathf.Clamp01(shoved / 2.1f);
                float bob = Mathf.Sin(story * 1.4f + k * 1.7f) * .035f * loose;
                // Once loose they ride up on the welling sea (film r9: at the court's own height a one degree rock sank each plate's
                // far half under the sea's surface, so the players stood in molten rock on slivers of stone). Rock and bob together
                // now dip a plate's far edge at most 0.11 m, under the 0.12 m it rides.
                float afloat = Ease(DnBreak, DnSplit1, t) * .12f;
                plate.localPosition = new Vector3(home.x + away.x, DnFloat + afloat + bob + heel * .42f, home.y + away.y);
                var rock = DnTilt[k] * .12f * loose * (.6f + .4f * Mathf.Sin(story * 1.1f + k));
                var heeled = Quaternion.AngleAxis(-4f * heel, new Vector3(-home.y, 0, home.x).normalized);
                plate.localRotation = heeled * Quaternion.Euler(rock.x, rock.y * .4f, rock.z);
            }

            // THE KNOCKS: a thin molten ring runs out under the court from his knuckles each time; the second leaves his hand's cracks lit.
            var palm = DnKnockSpot;
            for (int i = 0; i < _dnKnockRings.Count; i++)
            {
                float at = i == 0 ? DnKnock1 : DnKnock2, age = t - at;
                float r = Mathf.Lerp(.1f, i == 0 ? 3.2f : 4.6f, Mathf.Clamp01(age / .55f));
                PlaceGlow(_dnKnockRings[i], palm + Vector3.up * .004f, new Vector3(r, 1f, r), Quaternion.identity,
                          age < 0f ? 0f : (1f - Mathf.Clamp01(age / .6f)) * .9f);
            }
            float hand = Ease(DnKnock2, DnKnock2 + .08f, t) * (1f - Ease(DnPunch, DnPunch + .1f, t));
            Place(_dnHandCrackGlow, palm, Vector3.one * .55f, Quaternion.Euler(0, 20, 0), hand * .8f);
            Place(_dnHandCrack, palm + Vector3.up * .003f, Vector3.one * .5f, Quaternion.Euler(0, 20, 0), hand);

            // HIS GOLD EYE (his left, the scarred side) opens and catches the seam light from below.
            var eye = HeadPoint + new Vector3(-.085f, -.33f, .14f);
            float opened = Ease(DnEyeOpen + .04f, DnEyeOpen + .14f, t) * (1f - Ease(DnPunch + .2f, DnPunch + .5f, t));
            PlaceGlow(_dnEyeGold, eye, Vector3.one * .16f, Quaternion.identity, opened * (.9f + .2f * Mathf.Sin(t * 30f)));

            // THE SEAMS: six race out from the fist at the punch, the +x side first; lit through the continents, cooling at the stomp.
            for (int i = 0; i < _dnSeams.Count; i++)
            {
                float at = DnBreak + DnSeamDelay[i], run = Mathf.Clamp01((t - at) / .32f);
                float heat = run <= 0f ? 0f : (1f - .35f * Ease(DnStomp - .2f, DnStomp, t));
                // Drawn as the crack itself, revealed from the fist outward: scaled about the fist, not the stage's origin (film r1),
                // so it grows out of where the punch landed and lies exactly on its crack once run.
                float grown = Mathf.Max(.001f, run);
                var fist = new Vector3(DnFist.x, 0f, DnFist.z);
                PlaceGlow(_dnSeams[i], fist * (1f - grown) + Vector3.up * (DnFloat + .02f), new Vector3(grown, 1f, grown), Quaternion.identity,
                          heat * (1.2f + .25f * Mathf.Sin(story * 7f + i)));
            }

            // THE CREST, 1 of 3: cracked into the court round the fist for a quarter second.
            {
                float age = t - DnPunch;
                float s = age < 0f ? 0f : (1f - Mathf.Clamp01((age - .2f) / .1f));
                PlaceGlow(_dnCrestCourt, DnFist + Vector3.up * .025f, Vector3.one * 1.7f * Mathf.Lerp(.6f, 1f, Mathf.Clamp01(age / .06f)),
                          Quaternion.Euler(90f, 0f, 0f), s * 1.3f);
            }

            // THE BEAST: face first out of the widest seam, then the heaves; its eyes ignite at 3.75; the crest lights on its brow.
            bool risen = t >= DnRiseKeys[0].x && _dnTitan != null;
            if (_dnTitan != null) _dnTitan.gameObject.SetActive(risen);
            if (risen)
            {
                DnBeastPose(t, out float height, out float pitch);
                float grind = _reducedEffects ? 0 : Mathf.Sin(t * 41f) * .025f * (1 - Ease(DnRisen - .1f, DnRisen + .2f, t));
                _dnTitan.localPosition = new Vector3(grind, height + _dnCourt, DnSurge(t));
                _dnTitan.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                if (_dnBeast != null)
                {
                    float eyes = Ease(DnIgnite - .03f, DnIgnite + .05f, t);
                    DanteProp.SetGlow(_dnBeast, "glow-eyes", eyes * (.85f + .15f * Mathf.Sin(story * 3f)));
                    DanteProp.SetGlow(_dnBeast, "glow-seams", Ease(DnIgnite, DnHeaves[1], t) * (.75f + .25f * Ease(DnLoad, DnStomp, t)));
                }
                // Big things move little: the jaw drops a hand as the eyes light; the head lifts a little on each heave.
                if (_dnTitanJaw != null) _dnTitanJaw.localRotation = _dnJawRest * Quaternion.Euler(10f * Ease(DnIgnite, DnIgnite + .15f, t) * (1f - Ease(DnHeaves[0], DnHeaves[0] + .3f, t)), 0f, 0f);
                float nod = 0f; foreach (var h in DnHeaves) nod += Mathf.Sin(Mathf.Clamp01((t - h) / .35f) * Mathf.PI) * -4f;
                if (_dnTitanHead != null) _dnTitanHead.localRotation = _dnHeadRest * Quaternion.Euler(nod, 0f, 0f);
                // Its right arm: planted, raised overhead through his load, held trembling in the hang, brought down on the stomp.
                float raised = Ease(DnLoad, DnLoad + .8f, t) * (1 - Ease(DnStomp - .06f, DnStomp, t));
                float tremble = _reducedEffects ? 0 : Mathf.Sin(t * 33f) * 1.6f * raised * Ease(DnHangFrom - .3f, DnHangFrom, t);
                if (_dnTitanArm != null) _dnTitanArm.localRotation = _dnArmRest * Quaternion.Euler(-150f * raised + tremble, 0f, -12f * raised);
                // THE CREST, 2 of 3: lit on its brow as it wakes.
                var brow = _root.transform.InverseTransformPoint(_dnTitan.TransformPoint(new Vector3(0f, 2.28f, 2.75f)));
                float crest = Ease(DnIgnite, DnIgnite + .1f, t) * (1f - Ease(DnIgnite + .45f, DnIgnite + .7f, t));
                PlaceGlow(_dnCrestBrow, brow, Vector3.one * .62f, _dnTitan.localRotation * Quaternion.Euler(-12f, 0f, 0f), crest * 1.2f);
            }
            else PlaceGlow(_dnCrestBrow, Vector3.zero, Vector3.one, Quaternion.identity, 0f);

            // THE CREST, 3 of 3: huge behind them both at the stomp, flaring and cracking as the fists land.
            {
                float c = Ease(DnLoad + .5f, DnHangFrom, t);
                float flare = Flash(t, DnStomp, .1f);
                // Film r9: at 9.5 m its stone hid behind the beast and its horns ran off the top, washed pink on the sky; smaller and
                // higher, the whole emblem stands over the beast, and hotter.
                PlaceGlow(_dnCrestSky, new Vector3(-.4f, 6.9f, -5.5f), Vector3.one * 6.5f, Quaternion.identity, c * (.95f + .6f * flare));
            }

            // Where its fist lands: a lit seam shooting forward toward them on the last frames.
            {
                float age = t - DnStomp;
                Place(_dnBlastGlow, new Vector3(2.75f, DnFloat + .1f, 3.4f + 2.2f * Mathf.Clamp01(age / .1f)),
                      new Vector3(.5f, 1, 3.2f * (1 + 4f * Mathf.Clamp01(age / .1f))), Quaternion.identity, age >= 0 ? 1 : 0);
            }

            SampleDanteBurst(t, story);
            SampleDanteTake(t, story);
        }
    }
}
