using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// RAGO'S STOKE STEP (a first rough sample, 2026-10-17). He braces for 0.18 s and bursts 2 m along his aim, thrown at
    /// the root of 2 x friction 30 x 2 and stopped by that friction (`SeanHeroKit.StokeSpeed`, `StokeTravelSeconds`), then
    /// stands planted for 0.25 s. It hits nobody and leaves nothing that burns ("No knockdown or burning trail").
    /// So the picture is: a stamp of heat where he pushes off, a wake of flame that is only ever as long as he has gone,
    /// and nothing left on the road a second later.
    ///
    /// Built from the bench, as the method asks (`docs/VFX_BENCH_METHOD.md` section 4):
    ///   * THE WAKE is the approved trail ribbons' own flame picture (`trail_smoke`, the pleated ribbon, which is a row of
    ///     tongues) on an upright curtain laid down his path, coloured fire, front at his back. Two curtains crossed a
    ///     little, so it has a body from the side and from in front. Its tail is let go as he goes and what is left is
    ///     eaten from the tail (the ribbon shader's erosion, `erode_cloud`) when he stops: it never fades.
    ///   * THE STAMP is the scorch study's char and fissure pictures (`scorch_char`, `scorch_fissure`), small, under his
    ///     push-off, the cracks cooling by a rising cut from marigold to red as the scorch's do.
    ///   * THE ASH is the approved cloud, pooled (`CloudPuffs`): a kick at the push-off and a skid where he plants. This
    ///     class says when (`PushOffAt`, `PlantPuff`); whoever plays it spawns them.
    ///   * THE EMBERS ARE NEW: a dozen painted blocks (`SeanEmbers.shader`), the one thing the bench has no piece for. Its
    ///     motes are glow sprites, which are light and go pale on a sunlit road.
    /// Nothing is in front of him: the wake is behind his back, the stamp under his heel, the embers thrown backward.
    ///
    /// ⚠️ A PURE FUNCTION OF ITS CLOCK, MESHES BUILT ONCE. The two curtains and the embers are one mesh each for the whole
    /// process, shared by every Rago; a frame is a handful of floats handed to five materials. Nothing is built, uploaded
    /// or allocated per frame.
    /// </summary>
    /// <summary>What a film needs of any kept look of STOKE STEP: pose it, count it, free it.</summary>
    public interface ISeanStokeLook
    {
        void Pose(float t, Vector3 eye, float front, float stopped);
        void Cost(out int calls, out int triangles);
        void Release();
    }

    /// <remarks>
    /// THE MIX (2026-10-17). The owner, of s3, s4 and s6: "do the mix, i think the sean s3 fire trail was slightly better,
    /// just needs more refinement. it doesnt have the same quality as zack circuit though.." So the wake is s3's soft
    /// flame curtains, with what makes Zack's approved Quick Circuit read finished laid in: a crisp near-white core in
    /// thin lines inside the soft body (a second copy of each curtain cut to only the brightest of the same picture, so
    /// the lines sit exactly in the flames), a bright ground punctuation at BOTH ends (the cracks where he leaves and a
    /// smaller set where he plants), a clean birth (the brace) and a death that is eaten. From s6: the ring, the needles
    /// and a cel burst at the push-off, which now peaks AT the launch (it is set going in the brace, so its swell is the
    /// gather) and is one merged lumpy mass with red creases and thick dark seams (the owner's fire reference 1).
    /// The earlier states are kept by name: `FxStudySeanStokeS3` and `FxStudySeanStokeS4` (s4, and s6 as its `Cel` look).
    /// </remarks>
    public sealed class FxStudySeanStoke : FxStudyEffect, ISeanStokeLook
    {
        /// <summary>The rule's numbers, read from the kit so the picture cannot drift from it.</summary>
        public static float Brace => SeanHeroKit.StokeAnticipation;
        public static float Launch => SeanHeroKit.StokeSpeed;
        public static float Travel => SeanHeroKit.StokeTravelSeconds;
        public static float Friction => Core.Balance.Friction;
        /// <summary>The length the curtains are built to (the rule's 2 m). A shorter dash draws them shorter, not squashed in height.</summary>
        public const float Nominal = SeanHeroKit.StokeDistance;
        /// <summary>When it is all gone, counted from the press, for a dash that went its whole way.</summary>
        public const float Life = 2.10f;
        private const float Lead = 0.20f;

        // Fire between two named extremes: ember red at the dark of the picture, marigold at its light, a thin pale yellow
        // only on the brightest tongue edges (white in thin lines, never a broad area).
        // PAINTED, NOT GLASS (the owner's notes on s3, 2026-10-17: "broad painted flame shapes with a hard-stepped two or
        // three tone ramp (dark red rim, orange body, yellow core), not soft glassy gradients", and "no pink or magenta").
        // Read off a stylised 2D fire sheet seen as an image-search thumbnail: each flame is one broad teardrop in three flat
        // tones, a thin dark red rim, an orange body, a yellow core low in it, with tips that break into separate licks.
        // So the ramp's steps are a few hundredths of the mask wide, the piece covers what is behind it (almost no light
        // laid on), there is no halo, and the hue drift is small: it only wanders, it never turns red toward magenta.
        private static readonly Color Deep = new Color(0.62f, 0.07f, 0.02f), Body = new Color(1.00f, 0.40f, 0.04f),
                                      Hot = new Color(1.00f, 0.86f, 0.42f), Halo = new Color(1.00f, 0.32f, 0.03f);
        // The core lines: orange at their edge, yellow, and near-white only in the thinnest middle of each.
        private static readonly Color CoreDeep = new Color(1.00f, 0.52f, 0.06f), CoreBody = new Color(1.00f, 0.82f, 0.30f),
                                      CoreHot = new Color(1.00f, 0.97f, 0.84f);


        private static Mesh _curtainNear, _curtainFar, _embers, _needles, _ringMesh, _hornMesh;
        // The horn's group: stood at his back, its length drawn out to where he is, its tail curled up as it dies.
        private readonly Transform _horn;
        private const float HornHigh = 0.95f, HornBack = 0.15f;
        private static Mesh _spineMesh;
        private readonly FxPiece _spine;

        /// <summary>
        /// THE WAKE'S TWO KEPT FORMS. On (m5 on), the wake is many separate flame tongues, each living its own life
        /// (`SeanTongues.shader`), after the owner's verdict on the horn: "all of the flame moves as one. it doesnt look like
        /// flame at all". Off, it is the horn of m4, kept so it can be filmed beside them.
        /// </summary>
        public static bool Tongues = true;
        private readonly bool _tongues;

        /// <summary>
        /// THE FIREBALL WAKE (c1 on), after the owner's "it just loooks like stickers and flat" of the tongues and his fire
        /// breakdown: self-multiplied panning fire trails of two lengths and a head of rim cards wrapping his back
        /// (`SeanFireComet.shader`). Off, `Tongues` says which of the two kept wakes is drawn (m7's tongues, or m4's horn).
        /// </summary>
        public static bool Comet = true;
        private readonly bool _comet;
        /// <summary>c2 (on) over c1: the fireball grown, dark holes in its head, a smoke wisp off its tail, a soft flare at
        /// his heels in place of the cartoon push-off blobs, and curl on the embers. Off is c1 exactly, kept to film.</summary>
        public static bool Soft = true;
        private readonly bool _soft;
        /// <summary>c3 (on) over c2: the heel flare a crescent along his path, and the smoke rebuilt (`SmokeLook`). Off is c2.</summary>
        public static bool C3 = true;
        private readonly bool _c3;
        /// <summary>The smoke, in c3: 1 the approved cloud puffs off the fireball's tail (look A, chosen: it reads on the pale court and has volume), 2 the strands (look B, kept to film).</summary>
        public static int SmokeLook = 1;
        private readonly int _smoke;
        private static Mesh _cometMesh;
        private readonly FxPiece _fireball;
        private readonly Material _cometLook;
        private static Mesh _tongueMesh;
        private readonly FxPiece _flameTongues;
        private readonly Material _tongueLook;

        private readonly FxPiece _near, _far, _nearCore, _farCore, _char, _cracks, _plantCracks, _pool, _sparks, _streaks, _ring;
        private readonly CloudPuffField _flames;
        private float _scheduledFor = -1f;
        private readonly List<(CloudPuffField.Puff puff, float at)> _batch = new List<(CloudPuffField.Puff, float)>(8);
        private readonly Material _sparkLook;
        private readonly float _light;

        public override float Seconds => Lead + Life;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = new Vector3(0f, 0.7f, 1.0f); eye = focus + new Vector3(5.2f, 2.4f, -2.6f); fov = 38f; }

        public FxStudySeanStoke(Transform parent) : this(parent, AmihanWhirlwind.DayLight) { }

        /// <summary>`light` is how much of each flame is light laid on (the rest covers), as for the approved whirlwind in
        /// daylight: added light alone goes pale on a sunlit road.</summary>
        public FxStudySeanStoke(Transform parent, float light) : base("seanstoke", parent)
        {
            _light = light;
            _tongues = Tongues;
            _comet = Comet;
            _soft = Soft && Comet;
            _c3 = C3 && _soft;
            _smoke = _c3 ? SmokeLook : 0;
            if (_embers == null) _embers = EmberMesh(Embers, "SeanEmbers");

            // ⚠️ THE WAKE IS ONE BODY-SIZED HORN OF FLAME (m2). Measured against Quick Circuit z4, whose main form is one
            // body-sized layered sweep: in m1 the biggest thing on screen was the grey smoke and the fire was a lick at his
            // fist. So the wake is s3's painted flame picture (`trail_smoke`, the row of tongues) laid round a horn that
            // lies along his path at his waist: as wide as his body at his back, tapering to a point where he left, its
            // tongues pointing back down it. Two skins at two sizes turning opposite ways, and the crisp core lines (the
            // same picture cut to its brightest) on the outer one. Behind his back only: its mouth stands 0.15 m behind
            // the middle of his body, so nothing of it is before his eyes.
            if (_hornMesh == null)
            {
                _hornMesh = FxStudyKit.Shell(new[] { new Vector2(0.62f, 0f), new Vector2(0.60f, 0.25f), new Vector2(0.48f, 0.75f),
                    new Vector2(0.31f, 1.25f), new Vector2(0.14f, 1.70f), new Vector2(0.03f, Nominal) }, 0f, 360f, 0f, 30, 24);
                // ⚠️ NOT A CLEAN CONE. Straight and round, it read as exhaust out of his back (m3). So, once, in the mesh: three
                // slow lobes round it that twist down its length (its outline is lumpy, not a circle), and a bend that lifts
                // its point (flame rises; the shell's local z is up once it is laid down). Licks breaking off its edge and
                // point are the picture's own, cut harder (`Dress`).
                var points = _hornMesh.vertices; var along = _hornMesh.uv;
                for (int k = 0; k < points.Length; k++)
                {
                    float u = along[k].x, turn = Mathf.Atan2(points[k].z, points[k].x);
                    float lobe = 1f + 0.16f * Mathf.Sin(3f * turn + 4.0f * u) * Mathf.Lerp(0.4f, 1f, u);
                    points[k] = new Vector3(points[k].x * lobe, points[k].y, points[k].z * lobe + 0.28f * u * u);
                }
                _hornMesh.vertices = points; _hornMesh.RecalculateNormals(); _hornMesh.RecalculateBounds();
                _hornMesh.UploadMeshData(true);
            }
            _horn = Group("horn");
            _near = new FxPiece("horn-outer", _horn, _hornMesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 12);
            _far = new FxPiece("horn-inner", _horn, _hornMesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 11);
            _nearCore = new FxPiece("horn-outer-core", _horn, _hornMesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 14);
            _farCore = new FxPiece("horn-inner-core", _horn, _hornMesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 13);
            // The outer skin turns one way and streams back; the inner turns the other way and streams back faster.
            Dress(_near, 0.0f, 0.30f, 1f, 2f, 1.6f, 6f); Dress(_far, 0.37f, -0.70f, 0.85f, 1f, 2.6f, 8.5f);
            Dress(_nearCore, 0.0f, 0.30f, 1f, 2f, 1.6f, 6f); Dress(_farCore, 0.37f, -0.70f, 0.85f, 1f, 2.6f, 8.5f);
            foreach (var core in new[] { _nearCore, _farCore })
                core.Colours(CoreDeep, CoreBody, CoreHot, Halo).Set("_Cut", 0.56f, 0.60f).Set("_Curve", 1f).Set("_Stops", 0.62f, 0.70f, 0.76f)
                    .Set("_Halo", 0f).Set("_Add", 0.65f).Set("_Gain", 1.15f).Set("_Hue", 0f, 0f, 0f, 1f).Set("_Soft", 0.10f);
            // The shell stands up its own axis: laid down it runs back from his back (its local up becomes -z).
            foreach (var piece in new[] { _near, _far, _nearCore, _farCore }) piece.At.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Own(_near.Material); Own(_far.Material); Own(_nearCore.Material); Own(_farCore.Material);

            // THE SPINE: one crisp bright line running back along the horn's crown, from his back to its point, with a
            // little wave (Quick Circuit's sweep has its bolt; fire has a hot ridge). A strip standing upright, so it is
            // seen from the side; its picture the approved erosion streaks cut hard, so it is a few torn bright threads.
            if (_spineMesh == null)
            {
                var knots = new FxStudyKit.Knot[9];
                for (int k = 0; k < knots.Length; k++)
                {
                    float u = k / (float)(knots.Length - 1), r = Mathf.Lerp(0.56f, 0.04f, Mathf.Pow(u, 0.9f));
                    knots[k] = new FxStudyKit.Knot(new Vector3(0.04f * Mathf.Sin(u * 9f), r * 0.80f + 0.28f * u * u, -u * Nominal), Mathf.Lerp(0.16f, 0.03f, u));
                }
                _spineMesh = FxStudyKit.Sweep(knots, Vector3.right, perKnot: 6, across: 4);
                _spineMesh.UploadMeshData(true);
            }
            _spine = new FxPiece("horn-spine", _horn, _spineMesh, FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 15);
            _spine.Colours(CoreDeep, CoreBody, CoreHot, Halo).Set("_Tile", 2f, 1f, 0f, 0f).Set("_Scroll", 3.0f, 0f)
                  .Set("_Cut", 0.50f, 0.54f).Set("_Curve", 1f).Set("_Stops", 0.56f, 0.64f, 0.70f)
                  .Set("_Ends", 0.04f, 0.20f, 0.20f).Set("_ErodeTile", 1f, 1f, 0f, 0f).Set("_ErodeScroll", 1.0f, 0f).Set("_Sweep", -1.1f).Set("_Soft", 0.08f)
                  .Set("_Halo", 0f).Set("_Add", 0.7f).Set("_Gain", 1.2f).WindowSoft(0.06f);
            Own(_spine.Material);

            // THE TONGUES (m5): one mesh of every tongue and its pinched-off blob, one material, one draw.
            if (_tongueMesh == null) _tongueMesh = TongueMesh();
            var tongueShader = Shader.Find("TumbangPreso/SeanTongues");
            _tongueLook = tongueShader != null ? new Material(tongueShader) { name = "SeanTongues", hideFlags = HideFlags.HideAndDontSave } : null;
            _flameTongues = new FxPiece("tongues", Root, _tongueMesh, _tongueLook, 0);
            if (_tongueLook != null)
            {
                _tongueLook.renderQueue = 2503;
                var eats = FxStudyKit.Mask("erode_cloud");
                if (eats != null) _tongueLook.SetTexture("_ErodeTex", eats);
                _tongueLook.SetFloat("_Launch", Launch); _tongueLook.SetFloat("_Friction", Friction); _tongueLook.SetFloat("_Brace", Brace);
                Own(_tongueLook);
            }
            _flameTongues.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(5f, 5f, 7f));

            // THE FIREBALL (c1): two trails and four head cards in one mesh, one material, one draw.
            if (_cometMesh == null) _cometMesh = CometMesh();
            var cometShader = Shader.Find("TumbangPreso/SeanFireComet");
            _cometLook = cometShader != null ? new Material(cometShader) { name = "SeanFireComet", hideFlags = HideFlags.HideAndDontSave } : null;
            _fireball = new FxPiece("fireball", Root, _cometMesh, _cometLook, 16);
            if (_cometLook != null)
            {
                var a = FxStudyKit.Mask("erode_streak"); var b = FxStudyKit.Mask("erode_cloud");
                if (a != null) _cometLook.SetTexture("_NoiseA", a);
                if (b != null) _cometLook.SetTexture("_NoiseB", b);
                _cometLook.SetFloat("_Launch", Launch); _cometLook.SetFloat("_Friction", Friction); _cometLook.SetFloat("_Brace", Brace);
                _cometLook.SetFloat("_Add", _light);
                _cometLook.SetFloat("_Soft", _soft ? 1f : 0f);
                _cometLook.SetFloat("_C3", _c3 ? 1f : 0f);
                _cometLook.SetFloat("_SmokeMode", _smoke);
                Own(_cometLook);
            }
            _fireball.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(6f, 5f, 8f));

            // The stamp: the scorch's own two pictures, small, lying along his path under his push-off.
            var lie = Quaternion.Euler(0f, 90f, 0f);
            _char = AddFlat("stamp-char", 1, "scorch_char", lying: true, erode: "erode_cloud");
            var soot = new Color(0.03f, 0.022f, 0.018f);
            _char.Colours(soot, soot, new Color(0.08f, 0.05f, 0.035f), soot).Set("_Cut", 0.12f, 0.40f).Set("_Add", 0f).Set("_Soft", 0.10f).Set("_Emit", 0f);
            _char.At.localPosition = new Vector3(0f, 0.010f, -0.12f); _char.At.localRotation = lie; _char.At.localScale = new Vector3(1.90f, 1f, 0.95f);
            _cracks = AddFlat("stamp-cracks", 3, "scorch_fissure", lying: true, erode: "erode_cloud");
            // The strike where he leaves has a white core in its thinnest lines, as Quick Circuit's does.
            _cracks.Colours(new Color(0.55f, 0.04f, 0.0f), new Color(1f, 0.42f, 0.03f), new Color(1f, 0.96f, 0.82f), new Color(1f, 0.4f, 0.05f))
                   .Set("_Halo", 0.5f).Set("_Soft", 0.10f).Set("_Hue", 8f, 0f, 0.4f, 1f);
            _cracks.At.localPosition = new Vector3(0f, 0.016f, -0.12f); _cracks.At.localRotation = lie; _cracks.At.localScale = new Vector3(1.90f, 1f, 0.95f);
            // Where he plants: a smaller set of the same cracks, hot as he lands and cooling: Quick Circuit marks both ends.
            _plantCracks = AddFlat("plant-cracks", 3, "scorch_fissure", lying: true, erode: "erode_cloud");
            _plantCracks.Colours(new Color(0.55f, 0.04f, 0.0f), new Color(1f, 0.36f, 0.02f), new Color(1f, 0.92f, 0.60f), new Color(1f, 0.4f, 0.05f))
                        .Set("_Halo", 0.5f).Set("_Soft", 0.10f).Set("_Add", 0.25f).Set("_Hue", 8f, 0f, 0.9f, 1f).Set("_Tile", 1f, 1f, 0.5f, 0f);
            _plantCracks.At.localRotation = Quaternion.Euler(0f, 90f, 0f); _plantCracks.At.localScale = new Vector3(1.10f, 1f, 0.60f);
            // The cracks are painted heat, not light: they cover the char in flat yellow and orange.
            _cracks.Set("_Add", 0.25f);
            // The ground under his rear foot glows through the brace (the one soft thing in it, and only on the road).
            _pool = AddGlow("stamp-glow", 2, new Color(1f, 0.82f, 0.40f), new Color(1f, 0.40f, 0.05f), lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.013f, -0.12f); _pool.At.localScale = new Vector3(1.6f, 1f, 1.9f);

            var shader = Shader.Find("TumbangPreso/SeanEmbers");
            _sparkLook = shader != null ? new Material(shader) { name = "SeanEmbers", hideFlags = HideFlags.HideAndDontSave } : null;
            _sparks = new FxPiece("embers", Root, _embers, _sparkLook, 0);
            if (_sparkLook != null)
            {
                _sparkLook.renderQueue = 2502;
                _sparkLook.SetFloat("_Curl", _soft ? 0.9f : 0f);
                _sparkLook.SetFloat("_Length", Nominal); _sparkLook.SetFloat("_Launch", Launch); _sparkLook.SetFloat("_Friction", Friction); _sparkLook.SetFloat("_Brace", Brace);
                Own(_sparkLook);
            }
            _sparks.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 1f), new Vector3(4f, 4f, 6f));

            // ---- the pieces drawn to the owner's two fire references.
            // THE NEEDLES: reference 1's long thin pale streaks radiating from the burst. The same material and the same
            // clock as the embers, in a mesh of their own so the curtain look stays exactly as it was filmed.
            if (_needles == null) _needles = EmberMesh(Needles, "SeanNeedles");
            _streaks = new FxPiece("needles", Root, _needles, _sparkLook, 0);
            _streaks.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 1f), new Vector3(8f, 5f, 9f));
            // THE RING: reference 2's thin bright twisted rim round the feet. The approved trail's `trail_bands` picture (two
            // thin bands that cross and cross again) laid round a flat ring, in hard-stepped gold.
            if (_ringMesh == null)
            {
                _ringMesh = FxStudyKit.Revolve(new[] { new Vector2(0.86f, 0.02f), new Vector2(0.93f, 0.02f), new Vector2(1.0f, 0.02f) }, 64);
                _ringMesh.UploadMeshData(true);
            }
            _ring = new FxPiece("ring", Root, _ringMesh, FxStudyKit.Ribbon("trail_bands", "erode_cloud"), 4);
            _ring.Colours(new Color(0.80f, 0.22f, 0.03f), new Color(1.00f, 0.55f, 0.08f), new Color(1.00f, 0.88f, 0.42f), Halo)
                 .Set("_Tile", 3f, 1f, 0f, 0f).Set("_Scroll", 0.25f, 0f).Set("_Cut", 0.34f, 0.37f).Set("_Curve", 1f).Set("_Stops", 0.40f, 0.62f, 0.65f)
                 .Set("_Ends", 0f, 0f, 0f).Set("_ErodeTile", 3f, 1f, 0f, 0f).Set("_ErodeScroll", 0.2f, 0f).Set("_Sweep", 0f).Set("_Soft", 0.04f)
                 .Set("_Halo", 0f).Set("_Emit", 0.3f).Set("_Add", 0.2f).Set("_Hue", 5f, 0f, 0.3f, 1f);
            Own(_ring.Material);
            // THE FIRE BLOBS AND THE SMOKE: the approved cloud, pooled, in a field of its own (one material, the shared
            // mesh). Reference 1's fire is lumpy blobs in flat tones with dark ember seams between them: the cloud's balls
            // are the lumps, its two tones the orange body and the red creases, and its line the dark seam.
            {
                _flames = new CloudPuffField(Root, "Sean fire blobs");
                _flames.Show(false);
                // This field's material is its own (not the match's landing dust): its line is drawn thick, to be the dark
                // ember seams of reference 1, and the light is decided by the whole puff, so its lower side is one red crease.
                foreach (var renderer in Root.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.name == CloudPuffField.ShaderName)
                    { renderer.sharedMaterial.SetFloat("_Line", 0.030f); renderer.sharedMaterial.SetFloat("_Mass", 0.85f); }
            }
        }

        // A fire blob of reference 1: orange body, red creases, a dark ember seam round each lump.
        private static CloudPuffField.Puff Blob(Vector3 at, float scale, float life, float lift, float turn)
        {
            var p = CloudPuffField.Puff.Landing(at);
            p.Scale = scale; p.Life = life; p.Reach = 0.0f; p.Lift = lift; p.Turn = turn; p.Share = 1f;
            p.Lit = new Color(1.00f, 0.52f, 0.06f); p.Shade = new Color(0.80f, 0.12f, 0.03f); p.Line = new Color(0.16f, 0.04f, 0.02f);
            return p;
        }
        // Its yellow heart: smaller, inside it, gone first.
        private static CloudPuffField.Puff Heart(Vector3 at, float scale, float life, float lift, float turn)
        {
            var p = Blob(at, scale, life, lift, turn);
            p.Reach = 0.03f; p.Share = 0.8f;
            p.Lit = new Color(1.00f, 0.84f, 0.32f); p.Shade = new Color(1.00f, 0.58f, 0.10f); p.Line = new Color(0.86f, 0.30f, 0.04f);
            return p;
        }
        // Reference 1's smoke: dark grey, rising out of the fire after it, higher and slower.
        private static CloudPuffField.Puff Smoke(Vector3 at, float scale, float life, float lift, float turn)
        {
            var p = Blob(at, scale, life, lift, turn);
            p.Reach = 0.10f; p.Share = 0.55f;
            p.Lit = new Color(0.33f, 0.30f, 0.29f); p.Shade = new Color(0.25f, 0.23f, 0.22f); p.Line = new Color(0.19f, 0.17f, 0.16f);
            return p;
        }

        /// <summary>The time the rule has him `d` metres from where he left, counted from the press.</summary>
        public static float PassAt(float d)
        {
            d = Mathf.Clamp(d, 0f, Nominal);
            return Brace + (Launch - Mathf.Sqrt(Mathf.Max(0f, Launch * Launch - 2f * Friction * d))) / Friction;
        }

        // Writes every puff of the cast into the field once, each at the moment the rule has him there (the field runs on
        // this effect's own clock, so nothing more is written while it plays). Written again only if he stops short of
        // the full 2 m: then nothing is drawn past where he stopped, and his plant is where he really stopped.
        private void Schedule(float reach)
        {
            if (_flames == null || !_flames.Valid) return;
            _scheduledFor = reach;
            _flames.Clear();
            Vector3 W(float x, float z) => Root.TransformPoint(new Vector3(x, 0f, z));
            // The push-off. ⚠️ SET GOING IN THE BRACE: a ball of the cloud swells for 60 to 75 percent of its life, so a blob
            // started at the launch is biggest after he has gone (s6). Started 0.2 s early it swells under his heel as he
            // braces and is at its biggest as he leaves. One mass: no outward kick, so the lumps sit on one another.
            // Two masses at his hips, one either side and a little behind, and their hearts: from in front they show past
            // both sides of him as he goes.
            _batch.Clear();
            if (!_soft) {
            _batch.Add((Blob(W(0.36f, -0.12f), 3.0f, 0.34f, 0.32f, 0.0f), Brace - 0.20f));
            _batch.Add((Blob(W(-0.36f, -0.12f), 3.0f, 0.34f, 0.32f, 2.4f), Brace - 0.19f));
            _batch.Add((Heart(W(0.30f, -0.10f), 2.0f, 0.28f, 0.30f, 0.7f), Brace - 0.16f));
            _batch.Add((Heart(W(-0.30f, -0.10f), 2.0f, 0.28f, 0.30f, 3.1f), Brace - 0.15f));
            // ⚠️ THE SMOKE IS NEVER THE BIGGEST THING. In m1 it was. One small quiet puff, low in contrast, gone in under half a second.
            // It rises while it lives, small: never the biggest thing.
            _batch.Add((Smoke(W(0f, -0.40f), 1.2f, 0.45f, 1.30f, 1.9f), Brace + 0.10f));
            }
            // c2: no cartoon blobs at his heels (the soft flare in the fireball's own shader is the push-off now), and the
            // smoke is the wisp off the fireball's tail. Only the plant keeps its small dust.
            // The plant: a small blob and a skid of dark ash where he stops.
            float stop = Brace + Travel;
            if (!_soft) _batch.Add((Heart(W(0f, reach - 0.10f), 1.5f, 0.30f, 0.30f, 2.6f), stop - 0.06f));
            if (_smoke == 1)
            {
                // Look A: small soft grey puffs of the approved cloud, born off the fireball's tail (a metre behind him) as
                // he passes, rising as they grow and shrink away.
                for (int i = 0; i < 4; i++)
                {
                    float d = 0.25f + 0.45f * i;
                    if (d > reach) break;
                    var puff = Smoke(W(i % 2 == 0 ? 0.05f : -0.05f, d - 1.15f), 1.15f - 0.08f * i, 0.55f, 1.0f, i * 1.7f);
                    puff.At.y += 0.55f; puff.Reach = 0.10f; puff.Share = 0.75f;
                    puff.Lit = new Color(0.40f, 0.37f, 0.35f); puff.Shade = new Color(0.27f, 0.25f, 0.24f); puff.Line = new Color(0.18f, 0.16f, 0.15f);
                    _batch.Add((puff, PassAt(d) + 0.04f));
                }
            }
            _batch.Add((PlantPuff(W(0f, reach)), stop + 0.02f));
            // Every row written, the material handed them once (`CloudPuffField.SpawnAll`): one upload for the cast.
            _flames.SpawnAll(_batch);
        }

        // The ribbon shader's dress for a fire curtain. u runs along his path (0 where he left, 1 at his back), v up it.
        // The ribbon shader's dress for a horn of flame. u runs down the horn from his back (0) to where he left (1), v round
        // it. The picture is laid crosswise, so its row of tongues runs round the horn (`round` times, a whole number, or
        // the seam shows) and each tongue points down it, its foot at his back. It turns slowly round (`turn`); the
        // erosion streams back down it, so the tongues lick and tear toward the tail.
        // ⚠️ IT MUST CHANGE AS IT MOVES (m5). Filmed every second frame, m4's horn kept one silhouette from launch to plant and
        // only its texture shimmered: "the trail just looks the same as it moves" was the owner's verdict on a sibling
        // effect, and `docs/VFX_BENCH_METHOD.md` section 1a says why (layers at different speeds and directions, edges eaten
        // by a moving second map, a body that licks). So: the picture is laid in two ROWS down the horn and streams back
        // along it (`stream`, pictures a second), so tongues are born at his back and travel to the point; the erosion map
        // streams back faster than the picture and runs a little open all the time, so licks tear off the edges and point
        // and the outline changes every frame; and the warp bends the whole outline, most at the point.
        private void Dress(FxPiece piece, float seed, float turn, float gain, float round, float stream = 1.8f, float warp = 6f)
        {
            piece.Colours(Deep, Body, Hot, Halo)
                 .Set("_Swap", 1f).Set("_Tile", round, 2f, seed, 0.15f).Set("_Scroll", turn, stream)
                 .Set("_ErodeTile", 0.8f, 1f, seed, 0f).Set("_ErodeScroll", stream * 1.6f, turn * 0.5f)
                 // Cut harder than s3's curtains (0.07): the gaps between the tongues open, so licks break off the horn's
                 // edges and point instead of a closed skin.
                 .Set("_Cut", 0.13f, 0.38f).Set("_Curve", 1.2f).Set("_Stops", 0.32f, 0.66f, 0.95f)
                 // A short fade at his back, a long one at the point, nothing round it (it closes on itself).
                 .Set("_Ends", 0.06f, 0.15f, 0f).Set("_Sweep", -1.1f).Set("_Soft", 0.22f)
                 // s3's drift turned its red edges toward pink by brightness ("no pink or magenta"): now it only wanders.
                 .Set("_Hue", 8f, 0f, seed + 0.2f, 1f).Set("_Warp", 0.08f, 2.2f, seed * 5f, warp)
                 .Set("_Halo", 0.25f).Set("_Emit", 0.6f).Set("_Add", _light).Set("_Gain", gain).WindowSoft(0.06f);
        }

        private readonly List<Object> _mine = new List<Object>();
        private void Own(Object thing) { if (thing != null) _mine.Add(thing); }

        /// <summary>For a match that reuses one effect per caster: the next cast schedules its own puffs where it is.</summary>
        public void Restart() { _scheduledFor = -1f; _flames?.Clear(); }

        /// <summary>Frees this cast's materials as well as everything the bench owns. The meshes are the process's.</summary>
        public void Release()
        {
            foreach (var thing in _mine) { if (thing == null) continue; if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing); }
            _mine.Clear();
            _flames?.Dispose();
            Dispose();
        }

        /// <summary>When the push-off puff goes, counted from the press, and where (in this effect's own space).</summary>
        public static float PushOffAt => Brace;
        public static readonly Vector3 PushOffPoint = new Vector3(0f, 0f, -0.15f);

        /// <summary>The ash kicked up where he pushes off, in the approved cloud: warm grey, low and thrown back.</summary>
        public static CloudPuffField.Puff KickPuff(Vector3 at)
        {
            var p = CloudPuffField.Puff.Landing(at);
            p.Scale = 1.05f; p.Life = 0.46f; p.Reach = 0.50f; p.Lift = 0.30f; p.Share = 0.85f;
            p.Lit = new Color(0.44f, 0.40f, 0.37f); p.Shade = new Color(0.28f, 0.25f, 0.23f); p.Line = new Color(0.16f, 0.14f, 0.13f);
            return p;
        }

        /// <summary>The skid where he plants: the same ash, smaller and quicker.</summary>
        public static CloudPuffField.Puff PlantPuff(Vector3 at)
        {
            var p = KickPuff(at);
            p.Scale = 0.80f; p.Life = 0.34f; p.Reach = 0.34f; p.Lift = 0.14f; p.Share = 0.7f; p.Turn = 1.3f;
            return p;
        }

        // On the bench: a beat of nothing, then the brace, the dash its full 2 m by the rule's own sum, and the plant.
        public override void Sample(float age, Vector3 eye)
        {
            float t = age - Lead, s = Mathf.Clamp(t - Brace, 0f, Travel);
            Pose(t, eye, Launch * s - 0.5f * Friction * s * s, t - Brace - Travel);
        }

        /// <summary>The distance the rule has him from where he left, `s` seconds after the launch.</summary>
        public static float Gone(float s) { s = Mathf.Clamp(s, 0f, Travel); return Launch * s - 0.5f * Friction * s * s; }

        /// <summary>
        /// Poses it `t` seconds after the press. `front` is how far he is from where he left (m), read from his body in a
        /// match, so the wake is only ever as long as he has gone and never ahead of him. `stopped` is how long ago he
        /// stopped (below nothing while he is still going).
        /// </summary>
        public void Pose(float t, Vector3 eye, float front, float stopped)
        {
            float since = t - Brace;

            // ---- the stamp: heat gathers under his heel through the brace, cracks at their hottest as he leaves, cooling
            // by a rising cut (the outer ones first), then the char is eaten.
            float shown = t < 0f ? 0f : 1f;
            _char.Frame(t, shown, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.0f, Brace)), FxStudyKit.Run(t, 1.25f, 1.80f)));
            float cooled = FxStudyKit.Run(t, Brace + 0.20f, 1.40f);
            _pool.Frame(t, shown * FxStudyKit.Envelope(t, 0.0f, Brace * 0.8f, Brace + 0.06f, 0.40f));
            float cut = Mathf.Lerp(0.04f, 0.96f, Mathf.Pow(cooled, 0.8f));
            _cracks.Set("_Cut", cut, cut + 0.10f).Set("_Stops", cut + 0.12f, Mathf.Lerp(0.30f, 1.2f, cooled), Mathf.Lerp(0.70f, 1.6f, cooled))
                   .Set("_Emit", 2.0f * (1f - cooled)).Set("_Gain", Mathf.Lerp(2.2f, 0.9f, cooled) * (1f + 0.6f * FxStudyKit.Envelope(t, Brace - 0.04f, 0.04f, Brace + 0.04f, 0.20f)));
            _cracks.Frame(t, shown, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.0f, Brace * 0.7f)), Mathf.Max(0.2f * cooled, FxStudyKit.Run(t, 1.15f, 1.50f))));


            // ---- the wake: a horn at his back, drawn out to exactly where he is; it springs open as he leaves, its point is
            // let go behind him as he runs, and when he has planted it lingers a beat, then curls its tail up and is eaten
            // from the point toward him.
            float reach = Mathf.Max(0.04f, front);
            float open = FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.08f), 0.18f);
            // ⚠️ OVER BY ABOUT 0.8 s FROM THE PRESS (he plants at 0.545 s), as Quick Circuit is: the roll-up starts as he plants.
            float curl = FxStudyKit.Ease(FxStudyKit.Run(stopped, 0.0f, 0.24f));
            float letGo = 0.18f * FxStudyKit.Ease(FxStudyKit.Run(since, 0.12f, Travel + 0.05f)) + 0.55f * FxStudyKit.Ease(FxStudyKit.Run(stopped, 0.04f, 0.26f));
            // It lingers a beat after he plants (about 0.3 s, at the owner's note on s3) before it is eaten.
            float eaten = Mathf.Pow(FxStudyKit.Run(stopped, 0.06f, 0.27f), 0.8f);
            // The snap: at the launch the horn shoots out a third longer than he has gone (behind him, never ahead) and
            // settles back over a fifth of a second.
            float snap = 1f + 0.35f * FxStudyKit.Envelope(since, 0f, 0.05f, 0.07f, 0.16f);
            float length = Mathf.Max(0.05f, reach - HornBack) * snap / Nominal * Mathf.Lerp(1f, 0.55f, curl);
            // The roll-up: the point swings up over his back (70 degrees) as it shortens and is eaten from the point.
            _horn.localPosition = new Vector3(0f, HornHigh + 0.15f * curl, Mathf.Max(0f, reach - HornBack));
            _horn.localRotation = Quaternion.Euler(70f * curl, 0f, 0f);
            for (int i = 0; i < 2; i++)
            {
                var piece = i == 0 ? _near : _far;
                var core = i == 0 ? _nearCore : _farCore;
                // The inner skin is narrower and a little shorter; both breathe a little out of step.
                // It breathes: two slow beats out of step between the skins, plus a quick flutter.
                float wide = Mathf.Max(0.01f, open) * (i == 0 ? 1f : 0.70f)
                             * (1f + 0.09f * Mathf.Sin(t * 11f + i * 2.4f) + 0.05f * Mathf.Sin(t * 27f + i * 1.1f));
                piece.At.localScale = core.At.localScale = new Vector3(wide, length * (i == 0 ? 1f : 0.85f), wide);
                piece.Open(-0.2f, 1.2f - letGo); core.Open(-0.2f, 1.2f - letGo);
                // A little open all its life, so the streaming erosion is always tearing licks off it; then the death.
                float body = (0.26f + 0.06f * Mathf.Sin(t * 19f + i)) * FxStudyKit.Run(since, 0.04f, 0.12f);
                piece.Frame(t, _comet || _tongues || since < 0f ? 0f : 1f, Mathf.Max(body, eaten));
                core.Frame(t, _comet || _tongues || since < 0f ? 0f : 1f, Mathf.Max(Mathf.Max(body, eaten), FxStudyKit.Run(stopped, 0.02f, 0.18f)));
            }
            _spine.At.localScale = new Vector3(1f, Mathf.Max(0.01f, open), length);
            _spine.Open(-0.2f, 1.2f - letGo);
            // The spine flickers: its brightness and how much of it is torn jump frame to frame (from the clock, not at random).
            float flick = 0.5f + 0.5f * Mathf.Sin(t * 47f) * Mathf.Sin(t * 23f + 1.3f);
            _spine.Set("_Gain", 0.9f + 0.6f * flick);
            if (_tongueLook != null) { _tongueLook.SetFloat("_Reach", reach); _tongueLook.SetFloat("_Stopped", stopped >= 0f ? t - stopped : 10000f); }
            if (_cometLook != null) { _cometLook.SetFloat("_Reach", reach); _cometLook.SetFloat("_Stopped", stopped >= 0f ? t - stopped : 10000f); }
            _fireball.Frame(t, !_comet || t < Brace - 0.06f || t > Brace + Travel + 0.4f ? 0f : 1f);
            _flameTongues.Frame(t, _comet || !_tongues || t < Brace - 0.02f || t > Brace + Travel + 0.6f ? 0f : 1f);
            _spine.Frame(t, _comet || _tongues || since < 0.03f ? 0f : 1f, Mathf.Max(0.25f * (1f - flick), Mathf.Max(eaten, FxStudyKit.Run(stopped, 0.0f, 0.16f))));

            // ---- the embers: the shader does all of it from the clock and the reach.
            if (_sparkLook != null) { _sparkLook.SetFloat("_Reach", reach); _sparkLook.SetFloat("_Stopped", stopped >= 0f ? t - stopped : 10000f); }
            _sparks.Frame(t, t < 0f || t > Life ? 0f : 1f);
            _streaks.Frame(t, t < Brace - 0.02f || t > Brace + Travel + 0.8f ? 0f : 1f);

            // ---- the plant's cracks: hot as he lands, cooling by the same rising cut, gone in about a second.
            float landedAt = stopped >= 0f ? t - stopped : Brace + Travel;
            float plantCool = FxStudyKit.Run(t, landedAt + 0.10f, landedAt + 0.95f), plantCut = Mathf.Lerp(0.04f, 0.96f, Mathf.Pow(plantCool, 0.8f));
            _plantCracks.At.localPosition = new Vector3(0f, 0.017f, reach - 0.05f);
            _plantCracks.Set("_Cut", plantCut, plantCut + 0.10f).Set("_Stops", plantCut + 0.12f, Mathf.Lerp(0.30f, 1.2f, plantCool), Mathf.Lerp(0.70f, 1.6f, plantCool))
                        .Set("_Gain", Mathf.Lerp(2.2f, 0.9f, plantCool));
            _plantCracks.Frame(t, stopped < 0f ? 0f : 1f, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(stopped, 0f, 0.08f)), Mathf.Max(0.2f * plantCool, FxStudyKit.Run(t, landedAt + 0.80f, landedAt + 1.10f))));

            // ---- the ring: drawn tight round his rear foot through the brace, thrown out as he leaves, then eaten.
            float gather = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, Brace)), thrown = FxStudyKit.EaseOut(FxStudyKit.Run(since, 0f, 0.35f));
            _ring.At.localPosition = new Vector3(0f, 0.02f, -0.15f);
            _ring.At.localScale = Vector3.one * (Mathf.Lerp(0.55f, 0.40f, gather) + 0.45f * thrown);
            _ring.Frame(t, t < 0f ? 0f : gather, Mathf.Pow(FxStudyKit.Run(since, 0.05f, 0.28f), 0.8f));
            // ---- the fire and smoke: the field plays on this clock; it is drawn only while any of it can be alive.
            if (_flames != null)
            {
                float landedReach = stopped >= 0f ? reach : Nominal;
                if (Mathf.Abs(landedReach - _scheduledFor) > 0.01f) Schedule(landedReach);
                bool live = t >= Brace - 0.22f && t <= Brace + Travel + 1.0f;
                _flames.Show(live);
                if (live) _flames.Hold(t);
            }
        }

        // ---------------------------------------------------------------------------------------------------- meshes, once

        // An upright curtain down his path, 2 m long: low at the tail and tallest just behind him, its top laid back the
        // way the flame is dragged, rolled `roll` degrees off upright, `across` metres off his line.
        private static Mesh Curtain(float tall, float lean, float roll, float across)
        {
            const int along = 24, up = 6;
            int columns = up + 1;
            var vertices = new Vector3[(along + 1) * columns];
            var uvs = new Vector2[vertices.Length];
            var colours = new Color[vertices.Length];
            var turn = Quaternion.AngleAxis(roll, Vector3.forward);
            for (int r = 0; r <= along; r++)
            {
                float u = r / (float)along;
                // Rises from a low lick at the tail to its full height a hand behind him, rounding off at his back.
                float h = tall * Mathf.Lerp(0.28f, 1f, FxStudyKit.Ease(Mathf.Pow(u, 0.8f))) * Mathf.Lerp(0.82f, 1f, FxStudyKit.Ease(FxStudyKit.Run(1f - u, 0f, 0.10f)));
                for (int c = 0; c <= up; c++)
                {
                    float v = c / (float)up, y = -0.06f + v * h;
                    var p = turn * new Vector3(0f, y, 0f);
                    vertices[r * columns + c] = new Vector3(across + p.x, p.y, u * Nominal - lean * v * h);
                    uvs[r * columns + c] = new Vector2(u, v);
                    colours[r * columns + c] = Color.white;
                }
            }
            var triangles = new int[along * up * 6];
            int n = 0;
            for (int r = 0; r < along; r++)
                for (int c = 0; c < up; c++)
                {
                    int i = r * columns + c;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            var mesh = new Mesh { name = "SeanStokeCurtain", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colours; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        // One ember: where along the dash it leaves (below 0, off his heel in the brace), a delay, its life, its size,
        // where from off that point, how thrown, how fast it tumbles. A dozen and a few, placed, not sprayed: most leave
        // in the first metre where the push is, and they are thrown back and up, away from his eyes.
        private readonly struct Ember
        {
            public readonly Vector4 A, B, C, D;
            public Ember(float share, float delay, float life, float size, float x, float y, float z, float vx, float vy, float vz, float spin, float seed, float needle = 0f, float thick = 0f)
            { A = new Vector4(share, delay, life, size); B = new Vector4(x, y, z, spin); C = new Vector4(vx, vy, vz, seed); D = new Vector4(needle, thick, 0f, 0f); }
        }

        // The needles of reference 1: thrown from the push-off outward to the back and both sides and up, never forward
        // of him; a few more shed back off him on the way. Long while fast, shortening as they slow, then gone.
        private static readonly Ember[] Needles =
        {
            new Ember(0f, 0.00f, 0.42f, 1f,  0.10f, 0.40f, -0.20f,  5.5f, 2.2f, -3.0f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.01f, 0.38f, 1f, -0.10f, 0.50f, -0.20f, -6.0f, 1.8f, -2.4f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.00f, 0.45f, 1f,  0.05f, 0.60f, -0.25f,  2.0f, 4.8f, -5.0f, 0f, 0f, 0.070f, 0.020f),
            new Ember(0f, 0.02f, 0.40f, 1f, -0.05f, 0.55f, -0.25f, -2.6f, 5.2f, -4.2f, 0f, 0f, 0.070f, 0.020f),
            new Ember(0f, 0.01f, 0.36f, 1f,  0.12f, 0.30f, -0.25f,  6.8f, 0.8f, -1.2f, 0f, 0f, 0.080f, 0.022f),
            new Ember(0f, 0.00f, 0.36f, 1f, -0.12f, 0.30f, -0.25f, -6.6f, 1.0f, -1.6f, 0f, 0f, 0.080f, 0.022f),
            new Ember(0f, 0.03f, 0.44f, 1f,  0.00f, 0.70f, -0.30f,  0.4f, 6.4f, -2.8f, 0f, 0f, 0.065f, 0.020f),
            new Ember(0f, 0.02f, 0.40f, 1f,  0.08f, 0.35f, -0.30f,  3.8f, 1.4f, -5.6f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.01f, 0.40f, 1f, -0.08f, 0.35f, -0.30f, -3.4f, 1.6f, -5.8f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.04f, 0.34f, 1f,  0.00f, 0.25f, -0.30f,  0.6f, 2.6f, -6.6f, 0f, 0f, 0.070f, 0.020f),
            new Ember(0.35f, 0.00f, 0.32f, 1f,  0.15f, 0.70f, 0f,  2.4f, 2.6f, -4.0f, 0f, 0f, 0.060f, 0.018f),
            new Ember(0.60f, 0.00f, 0.30f, 1f, -0.15f, 0.80f, 0f, -2.6f, 2.8f, -3.6f, 0f, 0f, 0.060f, 0.018f),
            new Ember(0.85f, 0.00f, 0.28f, 1f,  0.05f, 0.90f, 0f,  0.8f, 3.4f, -3.2f, 0f, 0f, 0.060f, 0.018f),
        };
        private static readonly Ember[] Embers =
        {
            // Off his fists in the brace, two a side, lifting up and back: the stoke. Beside him, never before his eyes.
            new Ember(-1f, 0.02f, 0.50f, 0.060f,  0.44f, 1.00f, -0.05f,  0.3f, 1.6f, -0.7f,  9f, 0.11f),
            new Ember(-1f, 0.07f, 0.46f, 0.050f,  0.40f, 1.05f, -0.10f,  0.5f, 1.9f, -0.5f, -7f, 0.23f),
            new Ember(-1f, 0.04f, 0.50f, 0.060f, -0.44f, 1.00f, -0.05f, -0.3f, 1.7f, -0.7f, 11f, 0.37f),
            new Ember(-1f, 0.10f, 0.44f, 0.050f, -0.40f, 1.05f, -0.10f, -0.5f, 1.8f, -0.5f, -8f, 0.49f),
            new Ember(-1f, 0.05f, 0.42f, 0.050f,  0.08f, 0.06f, -0.20f,  0.3f, 2.0f, -0.6f, 10f, 0.31f),
            // Shed along the dash as he passes, thrown back and up.
            new Ember(0.05f, 0.00f, 0.62f, 0.098f,  0.18f, 0.45f, 0f,  0.7f, 2.4f, -2.2f,  8f, 0.41f),
            new Ember(0.12f, 0.01f, 0.55f, 0.077f, -0.20f, 0.70f, 0f, -0.8f, 2.0f, -1.8f, -9f, 0.53f),
            new Ember(0.20f, 0.00f, 0.70f, 0.112f,  0.05f, 0.95f, 0f,  0.2f, 2.8f, -2.6f,  6f, 0.67f),
            new Ember(0.30f, 0.02f, 0.50f, 0.070f, -0.12f, 0.35f, 0f, -0.5f, 1.6f, -1.4f, 12f, 0.71f),
            new Ember(0.40f, 0.00f, 0.66f, 0.091f,  0.22f, 0.80f, 0f,  0.9f, 2.2f, -2.0f, -8f, 0.83f),
            new Ember(0.52f, 0.01f, 0.58f, 0.084f, -0.15f, 1.05f, 0f, -0.6f, 2.5f, -1.6f,  7f, 0.97f),
            new Ember(0.64f, 0.00f, 0.52f, 0.063f,  0.10f, 0.55f, 0f,  0.4f, 1.9f, -1.2f, 10f, 0.13f),
            new Ember(0.78f, 0.01f, 0.60f, 0.098f, -0.05f, 0.90f, 0f, -0.2f, 2.6f, -1.0f, -6f, 0.29f),
            new Ember(0.90f, 0.00f, 0.48f, 0.070f,  0.16f, 0.60f, 0f,  0.6f, 2.0f, -0.8f,  9f, 0.47f),
            // Two thrown off as he plants, low and short.
            new Ember(1.00f, 0.03f, 0.40f, 0.063f,  0.20f, 0.10f, -0.1f,  1.2f, 1.4f, -0.4f, 13f, 0.59f),
            new Ember(1.00f, 0.05f, 0.36f, 0.056f, -0.22f, 0.10f, -0.1f, -1.1f, 1.2f, -0.5f, -12f, 0.61f),
        };

        // One tongue: where off his back it is born (m across, m up), how often (s), its offset in that cycle (s), its full
        // length and width (m), how far and fast it sways, how much it rises to its tip, how fast its length flickers, and
        // when its tip pinches off. A dozen, from his hips to his shoulders and a little past both sides, every one on its
        // own cycle so no two are ever at the same age. ⚠️ THE SHAPES ARE READ OFF THE OWNER'S REFERENCES (reference 2's
        // column breaking into licks, reference 1's lumps); THE MOTION IS OUR OWN DESIGN: no reference shows it moving.
        private static readonly float[,] TongueTable =
        {
            //  x      y     cycle  offset  long   wide   sway  rate   rise  flick pinch
            { 0.00f, 0.95f, 0.31f, 0.00f, 1.62f, 0.72f, 0.08f, 11.00f, 0.25f, 23.00f, 0.58f },
            { 0.22f, 1.20f, 0.27f, 0.11f, 1.30f, 0.54f, 0.10f, 14.00f, 0.35f, 29.00f, 0.55f },
            { -0.24f, 1.15f, 0.29f, 0.19f, 1.37f, 0.58f, 0.11f, 13.00f, 0.30f, 19.00f, 0.60f },
            { 0.30f, 0.70f, 0.34f, 0.05f, 1.23f, 0.54f, 0.12f, 10.00f, 0.15f, 27.00f, 0.62f },
            { -0.31f, 0.65f, 0.25f, 0.16f, 1.17f, 0.50f, 0.13f, 16.00f, 0.18f, 31.00f, 0.54f },
            { 0.10f, 1.38f, 0.23f, 0.08f, 1.04f, 0.43f, 0.09f, 17.00f, 0.40f, 25.00f, 0.52f },
            { -0.10f, 0.45f, 0.33f, 0.22f, 1.10f, 0.54f, 0.07f, 9.00f, 0.10f, 21.00f, 0.63f },
            { 0.38f, 1.00f, 0.28f, 0.27f, 0.98f, 0.43f, 0.14f, 15.00f, 0.28f, 33.00f, 0.56f },
            { -0.38f, 0.95f, 0.30f, 0.03f, 0.98f, 0.43f, 0.14f, 12.00f, 0.26f, 26.00f, 0.57f },
            { 0.05f, 0.75f, 0.26f, 0.14f, 1.43f, 0.65f, 0.10f, 12.00f, 0.20f, 18.00f, 0.60f },
            { -0.18f, 1.40f, 0.24f, 0.20f, 0.91f, 0.40f, 0.10f, 18.00f, 0.45f, 28.00f, 0.53f },
            { 0.20f, 0.40f, 0.32f, 0.24f, 1.04f, 0.47f, 0.09f, 11.00f, 0.12f, 22.00f, 0.61f },
        };

        // The fireball's mesh: the long trail, the short trail, and four head cards. Every point starts at the origin; the
        // shader places it. The cards are born every 0.10 to 0.13 s, out of step, so a new one is always starting.
        private static Mesh CometMesh()
        {
            const int along = 16;
            var points = new List<Vector3>(); var faces = new List<int>(); var a = new List<Vector4>(); var b = new List<Vector4>();
            for (int kind = 0; kind < 2; kind++)
            {
                int first = points.Count;
                var size = kind == 0 ? new Vector4(2.6f, 0.95f, 0f, 0f) : new Vector4(1.3f, 1.20f, 0f, 0f);
                for (int r = 0; r <= along; r++)
                    for (int side = 0; side < 2; side++)
                    { points.Add(Vector3.zero); a.Add(new Vector4(r / (float)along, side == 0 ? -1f : 1f, kind, kind)); b.Add(size); }
                for (int r = 0; r < along; r++)
                {
                    int i = first + r * 2;
                    faces.Add(i); faces.Add(i + 2); faces.Add(i + 1); faces.Add(i + 1); faces.Add(i + 2); faces.Add(i + 3);
                }
            }
            // The smoke wisp (piece 3) and the heel flare (piece 4): drawn only by c2, but always in the mesh.
            {
                int first = points.Count;
                for (int r = 0; r <= along; r++)
                    for (int side = 0; side < 2; side++)
                    { points.Add(Vector3.zero); a.Add(new Vector4(r / (float)along, side == 0 ? -1f : 1f, 3f, 3f)); b.Add(new Vector4(2.6f, 0.9f, 0f, 0f)); }
                for (int r = 0; r < along; r++)
                {
                    int i = first + r * 2;
                    faces.Add(i); faces.Add(i + 2); faces.Add(i + 1); faces.Add(i + 1); faces.Add(i + 2); faces.Add(i + 3);
                }
                first = points.Count;
                foreach (var q in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                { points.Add(Vector3.zero); a.Add(new Vector4(q.x, q.y, 4f, 4f)); b.Add(Vector4.zero); }
                faces.Add(first); faces.Add(first + 2); faces.Add(first + 1); faces.Add(first + 1); faces.Add(first + 2); faces.Add(first + 3);
            }
            // The c3 heel flare's second card (crossed on the path's line), and the three smoke strands (piece 5).
            {
                int first = points.Count;
                foreach (var q in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                { points.Add(Vector3.zero); a.Add(new Vector4(q.x, q.y, 4f, 0f)); b.Add(Vector4.zero); }
                faces.Add(first); faces.Add(first + 2); faces.Add(first + 1); faces.Add(first + 1); faces.Add(first + 2); faces.Add(first + 3);
                float[] every = { 0.26f, 0.31f, 0.36f }, late = { 0.00f, 0.11f, 0.20f }, thick = { 0.26f, 0.20f, 0.23f };
                for (int k = 0; k < 3; k++)
                {
                    first = points.Count;
                    for (int r = 0; r <= along; r++)
                        for (int side = 0; side < 2; side++)
                        { points.Add(Vector3.zero); a.Add(new Vector4(r / (float)along, side == 0 ? -1f : 1f, 5f, k)); b.Add(new Vector4(every[k], late[k], thick[k], k)); }
                    for (int r = 0; r < along; r++)
                    {
                        int i = first + r * 2;
                        faces.Add(i); faces.Add(i + 2); faces.Add(i + 1); faces.Add(i + 1); faces.Add(i + 2); faces.Add(i + 3);
                    }
                }
            }
            float[] cycle = { 0.10f, 0.12f, 0.11f, 0.13f }, offset = { 0.00f, 0.05f, 0.08f, 0.03f }, wide = { 1.15f, 1.0f, 1.25f, 0.95f };
            for (int k = 0; k < 4; k++)
            {
                int first = points.Count;
                foreach (var q in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                { points.Add(Vector3.zero); a.Add(new Vector4(q.x, q.y, 2f, k)); b.Add(new Vector4(0f, wide[k], cycle[k], offset[k])); }
                faces.Add(first); faces.Add(first + 2); faces.Add(first + 1); faces.Add(first + 1); faces.Add(first + 2); faces.Add(first + 3);
            }
            var mesh = new Mesh { name = "SeanFireComet", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetTriangles(faces, 0, false);
            mesh.bounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(6f, 5f, 8f));
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh TongueMesh()
        {
            const int along = 12, blobAlong = 4;
            var points = new List<Vector3>(); var faces = new List<int>();
            var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>(); var d = new List<Vector4>();
            for (int k = 0; k < TongueTable.GetLength(0); k++)
            {
                var born = new Vector4(TongueTable[k, 0], TongueTable[k, 1], TongueTable[k, 2], TongueTable[k, 3]);
                var size = new Vector4(TongueTable[k, 4], TongueTable[k, 5], TongueTable[k, 6], TongueTable[k, 7]);
                var more = new Vector4(TongueTable[k, 8], TongueTable[k, 9], TongueTable[k, 10], 0f);
                float seed = FxStudyKit.Hash(k, 41);
                for (int piece = 0; piece < 2; piece++)
                {
                    int rows = piece == 0 ? along : blobAlong, first = points.Count;
                    for (int r = 0; r <= rows; r++)
                        for (int side = 0; side < 2; side++)
                        {
                            // Every point starts at the origin: the shader places it.
                            points.Add(Vector3.zero);
                            a.Add(new Vector4(r / (float)rows, side == 0 ? -1f : 1f, piece, seed)); b.Add(born); c.Add(size); d.Add(more);
                        }
                    for (int r = 0; r < rows; r++)
                    {
                        int i = first + r * 2;
                        faces.Add(i); faces.Add(i + 2); faces.Add(i + 1);
                        faces.Add(i + 1); faces.Add(i + 2); faces.Add(i + 3);
                    }
                }
            }
            var mesh = new Mesh { name = "SeanTongues", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, c); mesh.SetUVs(3, d);
            mesh.SetTriangles(faces, 0, false);
            mesh.bounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(5f, 5f, 7f));
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Mesh EmberMesh(Ember[] embers, string name)
        {
            var points = new List<Vector3>(); var normals = new List<Vector3>(); var faces = new List<int>();
            var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>(); var dd = new List<Vector4>();
            Vector3[] sides = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var ember in embers)
                foreach (var n in sides)
                {
                    // A unit cube, half a unit each way: four corners a face, so each face is one flat painted tone.
                    Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up, w = Vector3.Cross(n, u);
                    int first = points.Count;
                    foreach (var corner in new[] { -u - w, u - w, u + w, -u + w })
                    { points.Add((n + corner) * 0.5f); normals.Add(n); a.Add(ember.A); b.Add(ember.B); c.Add(ember.C); dd.Add(ember.D); }
                    faces.Add(first); faces.Add(first + 1); faces.Add(first + 2);
                    faces.Add(first); faces.Add(first + 2); faces.Add(first + 3);
                }
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetNormals(normals); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, c); mesh.SetUVs(3, dd);
            mesh.SetTriangles(faces, 0, false);
            // The shader moves every corner: the bounds are the whole of where an ember can be.
            mesh.bounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(5f, 4f, 7f));
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>What it costs to draw: one draw call a piece that is on, and the triangles they submit.</summary>
        public void Cost(out int calls, out int triangles)
        {
            calls = 0; triangles = 0;
            foreach (var renderer in Root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!renderer.enabled || renderer.forceRenderingOff) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                // A material with two passes (the cloud's balls and their line) is two draws of its whole mesh.
                int passes = renderer.sharedMaterial != null ? Mathf.Max(1, renderer.sharedMaterial.passCount) : 1;
                calls += passes; triangles += passes * (int)(filter.sharedMesh.GetIndexCount(0) / 3);
            }
        }
    }
}
