using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ MAKILING'S EMBRACE'S SENTRY, v5: THE MODELLED TREE (direction.md section 5.2 and 5.8).
    ///
    /// The owner's notes that shaped it, in order: *"i want u to make a very nice plant sentry"* (Groot's
    /// Strangling Prison), *"it doesnt look that imposing"*, *"its js blocks"*, *"make it look likle this"*
    /// (a crop of Groot's woven limb), the cartoon tree (*"use this for inspiration"*), *"add woven vines
    /// and branches"*, *"Js make 2 fucking holles"* for the eyes, and for the prisoners, *"characters
    /// should look tied to the tree"* with *"woven tree branches"*. The tree is
    /// `Resources/Models/PaeteProps/sentry.glb` (`tools/build_paete_props.py`); this poses its nodes.
    ///
    /// Beats, all from age (so pause, replay and a probe agree). ⚠️⚠️ v6, IT CRAWLS OUT (owner, 2026-09-26 night: *"i also dotn want
    /// the tree to jsut spawn in or teleport in i want there to be an animation of how it grows or smth like make it crawl out from the
    /// ground? u figure it out"*, then *"and then the tree slowly show up"*; direction.md 5.14). Age 0 is the roots arriving under the
    /// spot. It used to screw up to full height in 0.5 s; it takes 1.75 s to its eyes now, the same in play and in the cutscene:
    ///  * BULGE 0 to 0.15: the court heaves up in a mound and cracks radiate from it.
    ///  * CLAWS 0.02 to 0.45: the six claw roots punch up out of the court one by one, in their own order, and slam down GRIPPING
    ///    it, like hands taking hold of a ledge (they belong to the model's root, not the trunk, so they come out first).
    ///  * HAUL 0.40 to 1.50: the trunk hauls itself out in three heaves (`Heaves`), a strain before each (it sinks a hair and
    ///    trembles) and a pause after; the last overshoots and SQUASHES on the stop. The quarter turn unwinds with the hauls.
    ///  * CROWN 1.35 to 1.72: the crown, folded tight while it was under the court, opens as it tops out.
    ///  * WAKE 1.75 (`WakeAt`): the light opens in the two hollows; the tree leans at its first prisoner.
    ///  * EMBRACE: from the catch (0.3, the rule's clock, unchanged) a woven limb reaches to each prisoner, drags them in and wraps
    ///    their waist; while the trunk is still under the court the limb leaves the GROUND beside it and is carried up with it.
    ///    The crown clenches just after the wake.
    ///  * WATCH to 9.5 s: it breathes, sways, blinks at its own times and looks from one prisoner to the next; a leaf falls now and
    ///    then.
    ///  * SLEEP from 9.6 s: the light shuts, the crown droops, the limbs let go and pull back, the trunk unscrews back into the road,
    ///    narra pods drop.
    /// </summary>
    public sealed class PaeteSentryBody : MonoBehaviour
    {
        private GameObject _model;
        private Transform _trunk, _crown, _eyes, _core;
        // ⚠️⚠️ THE LIGHT INSIDE THE CROWN (v10; owner, 2026-09-27: *"pointy on the top with a glow coming from within"*). A soft glow and a
        // hot heart among the branches, drawn with `SpiritGlow` (additive), so the branches in front of it cut it into shafts: it is
        // seen BETWEEN them, from within, which is what he asked for. This is not direction.md 5.12's gem (two green cubes that read as
        // a crystal stuck in a bare tree): there is no object, only light, and it is the same light as its eyes (slot 10).
        private PaeteLight _crownLight, _crownHeart;

        // ⚠️⚠️ ITS VINES CRAWL (owner, 2026-09-27: *"maybe if ur gonan add movement to it make its vines like move or crawl"*). The
        // tree itself holds still (it does not look round), so its life is small motions ON it: three vines wind up the rope and keep
        // climbing through the watch, a ripple running UP each one the way a creeper inches along, the whole vine slowly winding
        // round the trunk, its tip lifting off the bark and feeling the air. Built here, not in the glb, because a baked vine cannot
        // move. Each typed: start angle round the trunk (degrees), turns to the top, top height (authored metres), ripple phase,
        // creep (degrees a second round the trunk), thickness, and where along it its two leaves ride.
        private static readonly float[,] TrunkVineRows =
        {
            { 300f, 1.25f, 3.25f, 0.0f, 7f, 0.038f, 0.42f, 0.78f },
            { 130f, 1.05f, 2.70f, 1.7f, -6f, 0.034f, 0.36f, 0.70f },
            { 210f, 0.80f, 1.95f, 3.1f, 8f, 0.030f, 0.50f, 0.86f },
        };
        private const int TrunkVineSamples = 30;
        // The rope's silhouette (`tools/build_paete_props.py` `SILHOUETTE`, same keys): how far out the cords sit at each height, as
        // a multiplier on their 0.30 distance; the vines lie on the cords' outer face (0.30 x silhouette + their 0.115 girth).
        private static readonly Vector2[] Silhouette =
        {
            // ⚠️ REFITTED 2026-10-07 TO THE REMODELLED TRUNK (`tools/build_paete_sentry.py` `TRUNK`, each radius there plus 5 cm for
            // its cords, written as this table's multiplier): a wide foot, a waist, a heavy head. The first tree's were 0.9 to 1.5.
            new Vector2(-0.10f, 2.65f), new Vector2(0.08f, 2.38f), new Vector2(0.42f, 1.72f), new Vector2(0.95f, 1.38f), new Vector2(1.50f, 1.27f),
            new Vector2(2.00f, 1.28f), new Vector2(2.40f, 1.37f), new Vector2(2.70f, 1.37f), new Vector2(3.00f, 1.28f), new Vector2(3.28f, 1.15f),
            new Vector2(3.50f, 0.98f),
        };

        /// <summary>The remodelled trunk leans (`tools/build_paete_sentry.py` `bend`, the same numbers): what rides it leans with it.</summary>
        private static Vector3 TrunkBend(float y)
        {
            float f = Mathf.Clamp01(y / 3.44f);
            return new Vector3(0.16f * Mathf.Sin(f * 2f * Mathf.PI), 0f, -0.10f * Mathf.Sin(f * Mathf.PI));
        }
        private readonly List<Mesh> _trunkVines = new List<Mesh>();
        private readonly List<Transform> _trunkVineLeaves = new List<Transform>();
        private readonly List<Vector3> _vinePoints = new List<Vector3>();
        private readonly List<float> _vineRadii = new List<float>();

        /// <summary>
        /// How far into the face's burl a point on the rope is (1 inside it, 0 clear): within about 38 degrees of the front and
        /// between 2.12 and 2.68 up, fading over 17 degrees and 0.1 m. The burl is typed in `tools/build_paete_props.py` `sentry`.
        /// </summary>
        private static float FaceBurl(float angleRadians, float y)
        {
            float off = Mathf.Abs(Mathf.DeltaAngle(angleRadians * Mathf.Rad2Deg, 0f));
            // The remodelled face is wider and taller than the first burl: 62 degrees to each side, the mouth to the brow.
            float across = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(64f, 80f, off));
            float up = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.78f, 1.92f, y)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.72f, 2.86f, y)));
            return across * up;
        }

        private static float RopeRadius(float y)
        {
            for (int i = 0; i + 1 < Silhouette.Length; i++)
            {
                if (y > Silhouette[i + 1].x) continue;
                float u = Mathf.Clamp01((y - Silhouette[i].x) / (Silhouette[i + 1].x - Silhouette[i].x));
                return 0.30f * Mathf.Lerp(Silhouette[i].y, Silhouette[i + 1].y, u * u * (3f - 2f * u)) + 0.115f;
            }
            return 0.30f * Silhouette[Silhouette.Length - 1].y + 0.115f;
        }
        private readonly List<Transform> _roots = new List<Transform>();
        private readonly List<Quaternion> _rootRest = new List<Quaternion>();
        private readonly List<Vector3> _rootScale = new List<Vector3>();
        private readonly List<Transform> _claws = new List<Transform>();
        private readonly List<Quaternion> _clawRest = new List<Quaternion>();
        private PaeteEmbraceGround _ground;
        private readonly List<Transform> _spores = new List<Transform>();
        private readonly List<Mesh> _vineMeshes = new List<Mesh>();
        private readonly List<Transform> _vineTips = new List<Transform>();
        private readonly List<GrowthTwigs> _branchTwigs = new List<GrowthTwigs>();
        private readonly List<Transform> _thorns = new List<Transform>();
        private readonly List<CharacterMotor> _targets = new List<CharacterMotor>();
        private readonly List<PaeteEmbraceLimb> _limbs = new List<PaeteEmbraceLimb>();
        // Which of a limb's two beats has been drawn (0 none, 1 it has lashed out, 2 it has landed): each is drawn once.
        private readonly List<int> _limbBeat = new List<int>();
        private readonly List<Vector3> _wrap = new List<Vector3>();
        private bool _grasped;
        private readonly List<float> _arrive = new List<float>();
        private readonly List<float> _released = new List<float>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<Vector3> _limb = new List<Vector3>();
        private float _facing, _lastAge = -99f;

        /// <summary>
        /// ⚠️⚠️ THE MODEL STANDS 1.3 TIMES ITS AUTHORED SIZE, 7.0 m to the tip of its spire (v9 and v10, owner, 2026-09-27, on the v5
        /// film: *"Make the tre a bit smaller and a lot more sleek so that it isnt too distracting"*, then of v9's leaf clouds *"it makes
        /// it look goofy"*, *"js pointy on the top with a glow coming from within"*). It was 1.75, about 9 m, after *"make tree bigger"*
        /// and *"REALLY big and imposing"*: at that size and with v8's clutter it took over the court and covered the can. 7.0 m to a
        /// thin point is still the tallest thing in the box, which is the "imposing" kept; the sleekness is `tools/build_paete_props.py`
        /// `sentry` v10. Only the model is scaled, never this body:
        /// the limbs, shin branches and cracks are sized against the PLAYERS and must not grow with it. The prisoners, held 1.4 m
        /// out (`PaeteRules.SentryHoldDistance`), have their backs against its root knuckles (1.17 m out at shin height).
        /// </summary>
        public const float Scale = 1.3f;

        /// <summary>
        /// ⚠️⚠️ THE GUARDIAN'S OWN PALETTE: OLD WOOD, NOT HIS BODY'S (owner, 2026-09-26, of the tree seen from his screen
        /// in a match: *"it sucks"*, *"REFINE THIS TREE MORE"*). It wore Paete's roster palette, whose bark (8C6440,
        /// B08450, heartwood C29563) the plaza's sun lit to saturated orange: it read as plastic noodles. This is an
        /// ancient tree, older and darker than the carved guardian who calls it: deep browns a step apart so the weave
        /// still reads, darker mosses and leaves, and the same eye light as his (slot 10 and 11), so the two are
        /// visibly kin. Same sixteen slots as `tools/build_paete_voxel.py` (13 bark, 14 bark dark, 15 bark lit,
        /// 5 heartwood, 0/1/7 moss, 2/3 leaf, 4 vine, 6 root, 8 ink, 9 socket, 10/11 eye), direction.md 5.12.
        /// </summary>
        // ⚠️ v9 (2026-09-27): THE CROWN'S GREENS ARE MUTED (owner: *"so that it isnt too distracting"*). Measured off film r16: slot 2
        // (was 6FA532) rendered (158, 228, 44) in the plaza sun against the plaza trees' (97, 124, 71), twice as bright and far more
        // saturated, so the crown was the loudest thing on the court. Slots 2, 3 and 7 are a step darker and greyer now; the eyes (10,
        // 11) keep their light, so the one bright thing on the tree is its gaze.
        public static readonly Color[] Palette =
        {
            Hex(0x4E6E1E), Hex(0x34501A), Hex(0x557A2E), Hex(0x3A5E26), Hex(0x4C6E20), Hex(0x6E4A2C), Hex(0x4A3320), Hex(0x5C7A2E),
            Hex(0x1E140C), Hex(0x140C06), Hex(0xD8FF6A), Hex(0x86C83A), Hex(0xE8C24A), Hex(0x5F4128), Hex(0x3A2616), Hex(0x7C5836),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        /// <summary>The guardian's bark, for every part built at runtime (ground branches, limbs, the prisoners' bands).</summary>
        public static Color Bark => Palette[13];
        public static Color BarkDark => Palette[14];
        public static Color BarkLit => Palette[15];
        public static Color Moss => Palette[0];
        public static Color Leaf => Palette[2];

        /// <summary>The authored model's height to the tip of its spire (the leaf on its point), measured off the v10 glb (metres).</summary>
        public const float ModelHeight = 5.39f;
        private float _scale = Scale;

        /// <summary>
        /// ⚠️ FIT UNDER A ROOF. Ilalim ng Tulay is played under an elevated road, and at full size the crown
        /// went into the deck's underside (`PaeteReviewProbe` v17). The hazard measures the clearance above
        /// where the seed lands and the tree stands as tall as fits, never taller than `Scale`, never under
        /// 1.2 (it still has to tower over the prisoners).
        /// </summary>
        public void FitUnder(float clearance)
        {
            _scale = Mathf.Clamp((clearance - 0.3f) / ModelHeight, 1.0f, Scale);
            if (_model != null) _model.transform.localScale = Vector3.one * _scale;
        }
        private bool _podsDropped;

        /// <summary>
        /// ⚠️⚠️ THE CUTSCENE'S GUARDIAN IS THIS BODY, STAGED (direction.md 5.13, 2026-09-26 night). The introduction used
        /// to raise a separate, simpler copy of the model (`PaeteForestTree`), so the tree that came up in the cutscene
        /// was not the tree that came up in play. Now `HeroIntroductionScene.Paete` builds this very body under its own
        /// root and poses it from the cutscene's clock. `Staged` turns off the only things here that reach outside the
        /// body: `PaeteGroundBreak` and `PaeteLeafBurst` spawn into the WORLD and run on `Update`, and during the shared
        /// phase the world is paused (`Time.timeScale` 0) and is not what the cutscene camera is showing; the cutscene
        /// draws its own bursts on its own clock instead.
        /// </summary>
        public bool Staged { get; set; }

        /// <summary>
        /// ⚠️ THE STAGE'S ROOT, FOR A STAGED TREE THAT SHOULD STILL MAKE ITS EFFECTS (2026-10-08, the owner: "fix the rise").
        /// In a match the tree is handed over already standing (`PaeteHeroKit`, `handBack`), so its rise is only ever
        /// seen in the cutscene, and a staged tree made no effects at all: the breach, the claws taking hold, each haul,
        /// the crown shaken and the wake were in the film and nowhere in the game. With this set the staged tree makes
        /// them itself, on the stage (`PaeteFx.BeginStage`), and whoever stages it steps them (`PaeteFx.StepStage`).
        /// Null (a replay's tree, `RecordedFieldView`): it makes none, as before.
        /// </summary>
        public Transform StageFx { get; set; }
        private bool Emits => !Staged || StageFx != null;

        /// <summary>The node that holds the light in its hollows (null before the model loads): the cutscene lights its eyes from here.</summary>
        public Transform EyesNode => _eyes;

        /// <summary>The crown node its inner light hangs from (v7: the cutscene's TAKE bursts light out of it as its eyes open).</summary>
        public Transform CrownNode => _crown;

        /// <summary>
        /// When (seconds of its age) the light opens in its hollows. 0.55 in play, right as it tops out; the cutscene
        /// holds it back so the guardian's eyes are the last beat, the one its camera ends on.
        /// </summary>
        public float WakeAt { get; set; } = 1.75f;

        /// <summary>How long it lives on the age it is posed at (the live one is posed `PaeteSentry.BodyLead` ahead of the rules).</summary>
        public float LifeSeconds { get; set; } = PaeteRules.SentryLifeSeconds;

        /// <summary>How far the posed age runs ahead of the rules' clock, so the embrace limbs reach on the rules' catch.</summary>
        public float CatchLead { get; set; }

        /// <summary>
        /// ⚠️ THE THREE HAULS (v6): (start, end, the share of its height out of the court when it ends). Typed, each its own reach; the
        /// pauses between them are where it strains. `PaeteSentry` plays a groan and shakes the cameras at each start.
        /// </summary>
        public static readonly Vector3[] Heaves = { new Vector3(0.40f, 0.70f, 0.36f), new Vector3(0.80f, 1.10f, 0.70f), new Vector3(1.20f, 1.50f, 1.00f) };
        /// <summary>The crown opens between these ages, as the trunk tops out.</summary>
        public const float CrownFrom = 1.35f, CrownTo = 1.72f;
        // When each claw root punches up out of the court (its own order round the tree, never a sweep).
        private static readonly float[] ClawOut = { 0.02f, 0.14f, 0.07f, 0.20f, 0.10f, 0.26f };

        /// <summary>
        /// How much of the tree is out of the court at <paramref name="age"/>: 0 under it, 1 fully out (a touch over on the last haul's
        /// overshoot). Between hauls it holds, and in the 0.12 s before each it sinks a hair: the strain before the pull.
        /// </summary>
        public static float Risen(float age)
        {
            float reached = 0f;
            foreach (var h in Heaves)
            {
                if (age < h.x) return reached - 0.02f * Mathf.Clamp01((age - (h.x - 0.12f)) / 0.12f);
                if (age < h.y)
                {
                    float u = (age - h.x) / (h.y - h.x);
                    float e = h.z >= 0.999f ? GrowthVfx.Pop(u) : 1f - (1f - u) * (1f - u) * (1f - u);
                    return Mathf.LerpUnclamped(reached - 0.02f, h.z, e);
                }
                reached = h.z;
            }
            return reached;
        }

        // ⚠️⚠️ FOUR GROUND BRANCHES, NOT EIGHT (v9, owner: *"a lot more sleek so that it isnt too distracting"*). Eight thorned
        // branches racing 2.2 to 5.6 m out painted a star across a third of the box round a 9 m tree; four, from 1.5 m to at most
        // 3.6 m, say "its roots run under the court" without covering it. Yaw, length, wave phase: each typed, none the same.
        private static readonly float[] VineYaw = { 20f, 108f, 196f, 290f };
        private static readonly float[] VineLength = { 1.9f, 1.6f, 2.1f, 1.7f };
        private static readonly float[] VinePhase = { 0.2f, 1.9f, 3.1f, 0.8f };
        private const float VineFrom = 1.5f;
        // The ground branches' turn, so they part round the can (`AvoidPoint`); 0 until told.
        private float _branchTurn;
        private const int VineSamples = 24;
        // Where the thorns sit along a ground branch (hump tops) and where it crosses the court's surface.
        private static readonly float[] HumpThorn = { 0.20f, 0.60f, 0.92f };
        private static readonly float[] EntryAt = { 0.02f, 0.40f, 0.80f };
        private static readonly Vector3[] EntrySize = { new Vector3(0.22f, 0.10f, 0.16f), new Vector3(0.16f, 0.08f, 0.20f), new Vector3(0.19f, 0.09f, 0.14f),
                                                        new Vector3(0.14f, 0.07f, 0.17f), new Vector3(0.20f, 0.09f, 0.15f), new Vector3(0.15f, 0.08f, 0.13f) };
        private readonly List<Transform> _entries = new List<Transform>();
        private Vector3 dir(int i) { float y = (VineYaw[i] + _branchTurn) * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(y), 0f, Mathf.Cos(y)); }
        private Vector3 SideOf(int i) { var d = dir(i); return new Vector3(d.z, 0f, -d.x); }

        /// <summary>
        /// ⚠️ THE GROUND BRANCHES PART ROUND THE CAN (owner, 2026-09-27: *"make it so that it cant block the can too"*). The tree
        /// itself is kept 2.4 m off the can (`PaeteRules.SentryCanClearance`), but a branch racing along the court can still run
        /// through it; this turns the four of them together so the can's bearing sits in the middle of the gap between two.
        /// </summary>
        public void AvoidPoint(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world); local.y = 0f;
            if (local.sqrMagnitude < 1e-4f || local.magnitude > VineFrom + 3.0f) return;
            float bearing = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float best = 999f;
            for (int i = 0; i < VineYaw.Length; i++)
            {
                float a = VineYaw[i], b = VineYaw[(i + 1) % VineYaw.Length] + (i + 1 == VineYaw.Length ? 360f : 0f);
                float off = Mathf.DeltaAngle((a + b) * 0.5f, bearing);
                if (Mathf.Abs(off) < Mathf.Abs(best)) best = off;
            }
            _branchTurn = best;
        }
        // When each claw root slams down, in its own order round the tree (not a sweep).
        private static readonly float[] RootSlam = { 0.42f, 0.55f, 0.47f, 0.62f, 0.50f, 0.66f };
        // When each crown branch finishes unfurling, and how far it sways.
        private static readonly float[] ClawDelay = { 0.00f, 0.06f, 0.03f, 0.09f, 0.02f, 0.07f, 0.05f };
        private static readonly float[] ClawSway = { 3.0f, 2.2f, 3.6f, 2.6f, 3.2f, 2.0f, 2.8f };
        // Blinks, at the tree's own irregular times (one a double), and the order it looks round.
        private static readonly float[] Blinks = { 2.05f, 4.70f, 4.95f, 7.35f, 8.9f };
        private const float LookEvery = 2.6f;
        // Leaves falling from the crown during the watch: where each drops from (typed, cycled). v10: the few leaves are at the tips of
        // the spire's twigs, so they fall from there, rarely.
        private static readonly Vector3[] LeafFrom = { new Vector3(0.45f, 4.7f, 0.3f), new Vector3(-0.35f, 4.9f, -0.4f), new Vector3(0.1f, 4.6f, -0.55f),
                                                       new Vector3(-0.55f, 4.6f, 0.3f), new Vector3(0.3f, 5.0f, 0.45f) };

        public static PaeteSentryBody Build(Transform parent)
        {
            var go = new GameObject("PaeteSentryBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaeteSentryBody>();
            var root = go.transform;

            // ⚠️ THE GROUND BREAKING IS `PaeteEmbraceGround` NOW (2026-10-07, the ability rework). It was eight flat plates for
            // cracks, a patch of three crossed squares and ten cubes for clods. Now: jagged inked cracks that run further on each
            // haul, an uneven patch of turned earth whose rise is the bulge, and slabs of the court lifted round the rim that
            // jump on each haul. Still posed from the age alone, so the introduction's staged tree breaks the court the same way.
            b._ground = new PaeteEmbraceGround(root);

            b._model = PaeteProp.Spawn("sentry", root, Palette, ToonSkin.PersonOutlineWidth);
            if (b._model != null)
            {
                b._model.transform.localScale = Vector3.one * Scale;
                b._trunk = PaeteProp.Find(b._model, "trunk");
                b._crown = PaeteProp.Find(b._model, "crown");
                b._eyes = PaeteProp.Find(b._model, "eyes");
                for (int i = 0; i < 6; i++)
                {
                    var r = PaeteProp.Find(b._model, "buttress-" + i);
                    if (r == null) continue;
                    b._roots.Add(r); b._rootRest.Add(r.localRotation); b._rootScale.Add(r.localScale);
                }
                for (int i = 0; i < 7; i++)
                {
                    var c = PaeteProp.Find(b._model, "claw-" + i);
                    if (c == null) continue;
                    b._claws.Add(c); b._clawRest.Add(c.localRotation);
                }
            }
            if (b._trunk == null) b._trunk = new GameObject("trunk").transform;
            if (b._trunk.parent == null) b._trunk.SetParent(root, false);
            if (b._crown == null) { b._crown = new GameObject("crown").transform; b._crown.SetParent(b._trunk, false); b._crown.localPosition = Vector3.up * 3.44f; }

            // ⚠️ THE CROWN'S GLOWING GEM IS GONE (direction.md 5.12): two nested green cubes turned against each
            // other sat in the branches and read, from his screen, as a crystal stuck in a bare tree. The eyes in the
            // hollows are its only light. The empty node stays so the pose code keeps one shape.
            b._core = new GameObject("sentry-core").transform;
            b._core.SetParent(b._crown, false);
            for (int i = 0; i < TrunkVineRows.GetLength(0); i++)
            {
                var mesh = new Mesh { name = "PaeteTrunkVine" };
                mesh.MarkDynamic();
                PaeteInk.Part(b._trunk, "trunk-vine-" + i, mesh, Palette[4]);
                b._trunkVines.Add(mesh);
                for (int k = 0; k < 2; k++)
                    b._trunkVineLeaves.Add(PaeteInk.Part(b._trunk, "trunk-vine-leaf", PaeteInk.Leaf(0.20f, 0.12f, 0.018f), k == 0 ? Palette[2] : Palette[3]).transform);
            }
            b._crownLight = PaeteLight.Create(b._crown, "crown-light", Palette[10], billboard: true, falloff: 1.7f, core: .5f);
            b._crownHeart = PaeteLight.Create(b._crown, "crown-heart-light", Color.Lerp(Palette[10], Color.white, .35f), billboard: true, falloff: 2.2f, core: 1.2f);

            // The soil heaved where each ground branch crosses the court: two clods per crossing, three crossings.
            // Lumps with the cast's outline, each built at its own size (they were cubes scaled to `EntrySize`).
            for (int i = 0; i < VineYaw.Length * 3 * 2; i++)
                b._entries.Add(PaeteInk.Part(root, "entry-clod", PaeteBloomFx.Clod(EntrySize[i % EntrySize.Length].x * 0.55f), i % 3 == 0 ? Palette[6] : GrowthVfx.Seed).transform);
            // The eight ground branches: a mesh each, rebuilt while they grow, three thorns and a tip leaf.
            for (int i = 0; i < VineYaw.Length; i++)
            {
                var mesh = new Mesh { name = "PaeteSentryGroundBranch" };
                mesh.MarkDynamic();
                // The trunk's own mid tone and its bark by turns (one of the four was its darkest, a step under the tree it runs from).
                PaeteInk.Part(root, "ground-branch-" + i, mesh, i % 2 == 0 ? PaeteEmbraceFx.Wood : Bark);
                b._vineMeshes.Add(mesh);
                b._branchTwigs.Add(new GrowthTwigs(root, new[] { 0.28f + 0.03f * (i % 3), 0.52f, 0.74f - 0.04f * (i % 2) },
                                                   new[] { 40f + 25f * i, 200f - 15f * i, 310f + 10f * i }, new[] { 1.4f, 1.2f, 1.0f }, BarkLit));
                for (int k = 0; k < 3; k++)
                {
                    var thornMesh = new Mesh { name = "PaeteSentryThorn" };
                    PaeteInk.Tube(thornMesh, new List<Vector3> { Vector3.zero, new Vector3(0, 0.12f, 0.03f), new Vector3(0, 0.24f, 0.08f) },
                                  new List<float> { 0.05f, 0.028f, 0.003f }, 4);
                    b._thorns.Add(PaeteInk.Part(root, "thorn", thornMesh, BarkDark).transform);
                }
                b._vineTips.Add(PaeteInk.Part(root, "ground-branch-leaf", PaeteInk.Leaf(0.24f, 0.14f, 0.02f), Leaf).transform);
            }
            return b;
        }

        /// <summary>Which way the tree faces before it has anyone to look at: along the seed's flight.</summary>
        public void SetFacing(Vector3 worldDirection)
        {
            var d = transform.InverseTransformDirection(worldDirection);
            d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) _facing = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        public void SetTargets(List<CharacterMotor> targets)
        {
            // ⚠️ 2026-10-07: A LIMB IS `PaeteEmbraceLimb`, AN ARM OF THE TREE. It was a `PaeteRope` of three dark cords (v9 chose the
            // dark end of the bark so the binding was a silhouette on warm skin); beside the repainted trunk that read as a brown
            // cord from some other tree. The bough is the trunk's own mid tone, and the ink round it is what keeps it off the skin.
            foreach (var t in targets)
            {
                _targets.Add(t);
                _limbs.Add(new PaeteEmbraceLimb(transform, _targets.Count));
                _limbBeat.Add(0);
                _arrive.Add(-1f);
                _released.Add(-1f);
            }
        }

        /// <summary>A ground branch's centreline at growth <paramref name="grow"/> (0 to 1), in local space.</summary>
        private void VinePath(int i, float grow, float age)
        {
            _points.Clear();
            float yaw = (VineYaw[i] + _branchTurn) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var side = new Vector3(dir.z, 0f, -dir.x);
            float len = VineLength[i] * grow;
            for (int k = 0; k <= VineSamples; k++)
            {
                float u = k / (float)VineSamples;
                float d = VineFrom + len * u;
                float wave = Mathf.Sin(u * 7.0f + VinePhase[i] + age * 1.6f) * 0.12f * u;
                // ⚠️⚠️ IT GOES INTO THE GROUND, NOT OFF IT (owner, 2026-09-26: *"make it look like the roots GO INT
                // he ground not float off of it"*). It used to lie along the court and curl its tip 0.55 m up into
                // the air. Now it WEAVES: two humps up out of the road and back under it (the parts below the court
                // are hidden by the court itself), then it dives in for good. It crosses the surface at u = 0, 0.4
                // and 0.8, which is where the soil heaves (`_entries`), and it strains a little as the prisoners fight.
                float hump = Mathf.Sin(u * Mathf.PI * 2.5f) * (0.20f - 0.05f * u) * grow;
                // ⚠️⚠️ FIXED 2026-10-08: THE WHOLE BRANCH HAD BEEN UNDER THE COURT SINCE THIS LINE WAS WRITTEN. It read
                // `Mathf.SmoothStep(0.80f, 1.0f, u)`, meant as the shader's smoothstep (0 until u is 0.8, then up to 1); Unity's
                // eases FROM 0.8 TO 1.0, so the dive was 0.36 to 0.45 m at every point and no part of any ground branch came
                // above the road (measured in the film: the mesh's bounds ran from -0.66 to -0.08 m). Only its thorns showed,
                // standing on the court by themselves. Now only the last fifth dives, as the note above always said.
                float dive = -0.45f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.80f, 1.0f, u));
                _points.Add(dir * d + side * wave + Vector3.up * (hump + dive));
            }
        }

        /// <summary>
        /// ⚠️⚠️ IT DOES NOT LOOK ROUND (owner, 2026-09-27: *"also tree doesnt need to look left and right"*, *"lokks very weird"*). It used
        /// to turn its whole trunk to one prisoner, then the next, every 2.6 s: a 7 m tree swivelling on its roots read as a turret, not a
        /// guardian. It holds the way it came up, facing along its roots' travel, and its life is in its breath, its blinks and the light
        /// inside its crown. `WatchPrisoners` keeps the old behaviour one switch away.
        /// </summary>
        private static readonly bool WatchPrisoners = false;

        private float LookYaw(float age)
        {
            // Before it wakes it faces along the throw; awake, it looks at one prisoner, then the next,
            // every 2.6 s, easing across in 0.7 s (only with `WatchPrisoners`).
            float watchFrom = WakeAt + 0.15f;
            if (!WatchPrisoners || _targets.Count == 0 || age < watchFrom) return _facing;
            float TargetYaw(int k)
            {
                var p = _targets[((k % _targets.Count) + _targets.Count) % _targets.Count];
                if (p == null) return _facing;
                var d = transform.InverseTransformPoint(p.transform.position); d.y = 0f;
                return d.sqrMagnitude > 1e-4f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : _facing;
            }
            float since = age - watchFrom;
            int seg = Mathf.FloorToInt(since / LookEvery);
            float into = since - seg * LookEvery;
            float from = seg == 0 ? _facing : TargetYaw(seg - 1);
            float to = TargetYaw(seg);
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(into / 0.7f));
            return Mathf.LerpAngle(from, to, ease);
        }

        public void Pose(float age, Vector3 centre)
        {
            bool stage = Staged && StageFx != null;
            if (stage) PaeteFx.BeginStage(StageFx);
            try
            {
                float before = _lastAge;
                PoseBody(age, centre);
                // What `Abilities.PaeteSentry` fires for a tree in the world, the staged tree fires for itself.
                if (stage && age >= 0f && age - before < 0.5f)
                {
                    if (before < 0f) PaeteEmbraceFx.Breach(transform.position);
                    for (int k = 0; k < Heaves.Length; k++)
                        if (before < Heaves[k].x && age >= Heaves[k].x) PaeteEmbraceFx.Haul(transform.position, k);
                    if (before < WakeAt && age >= WakeAt) PaeteEmbraceFx.Wake(_crown, _eyes, transform.position, _scale);
                }
            }
            finally { if (stage) PaeteFx.EndStage(); }
        }

        private void PoseBody(float age, Vector3 centre)
        {
            bool landed = age >= 0f;
            for (int c = 0; c < transform.childCount; c++) transform.GetChild(c).gameObject.SetActive(landed);
            if (!landed) { foreach (var l in _limbs) l.Clear(); _lastAge = age; return; }

            float life = LifeSeconds;
            float wither = Mathf.Clamp01((age - (life - 0.4f)) / 0.9f);
            float alive = 1f - wither;

            // BULGE: the court heaves up in a mound over it and the cracks run out from it, further on each haul.
            _ground?.Pose(age, wither);

            // HAUL: out of the court in three heaves, trembling in the pauses; the quarter turn unwinds as it comes; SQUASH on the
            // last stop; breathe; SLEEP unscrews it back into the road.
            float risen = Risen(age);
            float topOut = Heaves[Heaves.Length - 1].y;
            bool hauling = false;
            foreach (var h in Heaves) if (age >= h.x && age < h.y) hauling = true;
            float tremble = age < topOut && !hauling ? 0.012f * Mathf.Sin(age * 47f) : 0f;
            float land = age - topOut;
            float squash = land > 0f && land < 0.32f ? Mathf.Sin(land / 0.32f * Mathf.PI) : 0f;
            float breathe = age > WakeAt ? Mathf.Sin((age - WakeAt) * 2.2f) : 0f;
            float tall = 1f - 0.08f * squash + 0.015f * breathe * alive;
            float wide = 1f + 0.05f * squash + 0.008f * breathe * alive;
            _trunk.localScale = new Vector3(wide, tall, wide);
            // 5.6 model metres under: the whole v9 model (5.1 to its crown top) is below the court before the first haul.
            // ⚠️ IT GOES ALL THE WAY UNDER (2026-10-08). It sank 1.8 model metres of its 5.4 and was then removed where it stood:
            // a third of the way into the court, it vanished (the owner, told so: "fix the ... exit"). Now the whole of it
            // goes back into the road it came out of, slowly at first and then drawn down, in the same 0.9 s.
            float under = wither * wither * (3f - 2f * wither);
            _trunk.localPosition = Vector3.down * (5.6f * (1f - Mathf.Clamp(risen, -0.05f, 1.06f)) + 5.7f * under) + new Vector3(tremble, 0f, 0f);
            // The lean: at the wake it tips 6 degrees forward, the way it faces, settling to 3. Hauling, it pitches into each pull.
            float haulPitch = 0f;
            foreach (var h in Heaves) haulPitch = Mathf.Max(haulPitch, GrowthVfx.Envelope(age, h.x, 0.08f, h.y + 0.1f, 0.2f));
            float lean = (age < WakeAt ? 3f * haulPitch : 6f * GrowthVfx.Pop((age - WakeAt) / 0.3f) - 3f * Mathf.Clamp01((age - WakeAt - 0.65f) / 1.0f)) * alive;
            float yaw = LookYaw(age) - 95f * (1f - Mathf.Clamp01(risen)) + 50f * wither;
            _trunk.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(lean, 0f, 0f);

            // CLAWS: each claw root punches up out of the court (raised high) and slams down gripping it, before the trunk comes;
            // on each haul they bear down a little harder (the pull).
            for (int i = 0; i < _roots.Count; i++)
            {
                float outAt = ClawOut[i % ClawOut.Length];
                float show = Mathf.Clamp01((age - outAt) / 0.08f);
                float slam = GrowthVfx.Pop((age - outAt - 0.10f) / 0.16f);
                float raised = -62f * (1f - slam) + 5f * haulPitch - 25f * wither;
                _roots[i].localRotation = _rootRest[i] * Quaternion.Euler(raised, 0f, 0f);
                _roots[i].localScale = _rootScale[i] * Mathf.Max(0.001f, show * (1f - Mathf.SmoothStep(0f, 1f, wither * 1.4f)));
                float gripAt = outAt + 0.19f;
                if (Emits && _lastAge < gripAt && age >= gripAt && age - _lastAge < 0.5f)
                    PaeteEmbraceFx.ClawGrip(_roots[i].TransformPoint(new Vector3(0f, 0f, 1.0f)), transform.position);
            }

            // CROWN: folded tight while it was under the court, it opens as the trunk tops out; the EMBRACE clench just after the
            // wake; a slow sway; the droop as it sleeps.
            float clench = GrowthVfx.Envelope(age, WakeAt + 0.1f, 0.12f, WakeAt + 1.3f, 0.45f);
            for (int i = 0; i < _claws.Count; i++)
            {
                float unfurl = GrowthVfx.Pop((age - CrownFrom - ClawDelay[i % ClawDelay.Length]) / (CrownTo - CrownFrom));
                float sway = Mathf.Sin(age * 1.35f + i * 1.1f) * ClawSway[i % ClawSway.Length] * alive * Mathf.Clamp01(unfurl);
                // v10: the spire's branches rise steeply, so folded under the court they only draw in 20 degrees (a 62 degree fold
                // crossed them through the leader); the clench on the wake is a tightening, and as it sleeps they droop open.
                // ⚠️ 2026-10-07, THE REMODELLED CROWN: its boughs spread like antlers, 2 m out, so drawn in only 20 degrees they came
                // up THROUGH the court round the trunk. Under the court they fold right up against the leader (52 degrees), and
                // open as it tops out, which is the crown's whole arrival now. The droop as it sleeps is gentler for the same reason.
                // As it sleeps the boughs droop open a moment, then fold up against the leader as they did coming out, so
                // they follow the trunk down through the hole it made and do not sweep the court on the way.
                float droop = 24f * Mathf.Clamp01(wither / 0.25f) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.6f, wither)));
                float foldIn = -52f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.6f, wither));
                float pitch = -52f * (1f - unfurl) - 8f * clench + sway + droop + foldIn;
                _claws[i].localRotation = _clawRest[i] * Quaternion.Euler(pitch, 0f, sway * 0.5f);
            }

            PoseTrunkVines(age, risen, wither);

            // THE LIGHT WITHIN (v10): a faint glow already while it hauls itself out (the light its roots carried is inside it), full
            // when its eyes open, breathing with it through the watch, gone as it sleeps.
            if (_crownLight != null)
            {
                float carried = 0.3f * Mathf.Clamp01((age - Heaves[0].x) / 0.8f);
                float woke = age < WakeAt ? 0f : Mathf.Clamp01(GrowthVfx.Pop((age - WakeAt) / 0.3f));
                float asleep = 1f - Mathf.Clamp01((age - (life - 0.45f)) / 0.4f);
                float breath = 1f + 0.14f * Mathf.Sin((age - WakeAt) * 2.2f) * woke;
                float lit = Mathf.Max(carried, woke) * asleep;
                // Widened 2026-10-07: the remodelled crown is boughs spread like antlers, twice as wide as the first spray of twigs.
                _crownLight.Set(new Vector3(0f, 0.86f, 0f), Vector3.one * 2.0f * breath, Quaternion.identity, 1.15f * lit * breath);
                _crownHeart.Set(new Vector3(0f, 0.80f, 0f), Vector3.one * 0.62f, Quaternion.identity, 1.5f * lit);
            }

            // The core node stays (the gem is gone, direction.md 5.12); the spores drift once it is awake.
            float coreIn = GrowthVfx.Pop((age - WakeAt) / 0.3f);
            _core.localPosition = new Vector3(0f, 0.72f, 0f);
            _core.localRotation = Quaternion.Euler(0f, age * 35f, 0f);
            _core.localScale = Vector3.one * 0.001f;
            for (int i = 0; i < _spores.Count; i++)
            {
                float a = age * (1.4f + 0.2f * i) + i * Mathf.PI * 0.5f;
                _spores[i].localPosition = new Vector3(Mathf.Cos(a) * 0.62f, 0.72f + Mathf.Sin(a * 1.7f) * 0.18f, Mathf.Sin(a) * 0.62f);
                _spores[i].localScale = Vector3.one * 0.07f * Mathf.Clamp01(coreIn) * alive;
            }

            // WAKE, BLINK, SLEEP: the light in the hollows, opened and shut on its own node. The blinks keep their spacing from the wake.
            if (_eyes != null)
            {
                float open = age < WakeAt ? 0f : GrowthVfx.Pop((age - WakeAt) / 0.25f);
                float shut = 0f;
                foreach (float bl in Blinks)
                {
                    float x = (age - (bl + WakeAt - 0.55f)) / 0.09f;
                    if (x > 0f && x < 2f) shut = Mathf.Max(shut, 1f - Mathf.Abs(x - 1f));
                }
                float sleep = 1f - Mathf.Clamp01((age - (life - 0.45f)) / 0.4f);
                float lid = Mathf.Max(0.001f, open * (1f - shut) * sleep);
                _eyes.localScale = new Vector3(Mathf.Lerp(1f, 1.18f, Mathf.Clamp01(open - 1f) * 4f), lid, 1f);
            }

            // The ground branches race out, then writhe; they pull back into the road as it sleeps.
            for (int i = 0; i < _vineMeshes.Count; i++)
            {
                float grow = Mathf.Clamp01((age - 0.06f - 0.025f * i) / 0.42f);
                grow = (1f - (1f - grow) * (1f - grow)) * alive;
                if (grow <= 0.01f)
                {
                    _vineMeshes[i].Clear();
                    for (int k = 0; k < 3; k++) _thorns[i * 3 + k].localScale = Vector3.zero;
                    _vineTips[i].localScale = Vector3.zero;
                    for (int c = 0; c < 6; c++) _entries[i * 6 + c].localScale = Vector3.zero;
                    _branchTwigs[i].Place(_points, 0f, 1f, false);
                    continue;
                }
                VinePath(i, grow, age);
                _radii.Clear();
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.14f, 0.035f, k / (float)VineSamples));
                PaeteInk.Tube(_vineMeshes[i], _points, _radii, 6);
                _branchTwigs[i].Place(_points, grow, 1f, false);
                for (int k = 0; k < 3; k++)
                {
                    // On the tops of the humps (u 0.2 and 0.6) and one on the rise of the last, where they show.
                    int at = Mathf.Clamp(Mathf.RoundToInt(HumpThorn[k] * VineSamples), 1, VineSamples - 1);
                    var thorn = _thorns[i * 3 + k];
                    Vector3 along = (_points[at + 1] - _points[at - 1]).normalized;
                    thorn.localPosition = _points[at] + Vector3.up * 0.13f;
                    thorn.localRotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(-10f, 0f, 0f);
                    thorn.localScale = Vector3.one * 1.5f * Mathf.Clamp01((grow - HumpThorn[k]) * 5f);
                }
                // The leaf rides the top of the second hump; the tip itself is under the road.
                var tip = _vineTips[i];
                int crest = Mathf.RoundToInt(0.6f * VineSamples);
                tip.localPosition = _points[Mathf.Min(crest, _points.Count - 1)] + Vector3.up * 0.16f;
                tip.localRotation = Quaternion.LookRotation(dir(i), Vector3.up) * Quaternion.Euler(-30f, 25f, 0f);
                tip.localScale = Vector3.one * 1.6f * Mathf.Clamp01((grow - 0.6f) * 3f);
                // The soil heaved up where it goes in and out: a clod either side of each crossing.
                for (int e = 0; e < 3; e++)
                {
                    float at = EntryAt[e];
                    float show = Mathf.Clamp01((grow - at) * 6f) * alive;
                    int idx = Mathf.Clamp(Mathf.RoundToInt(at * VineSamples), 0, _points.Count - 1);
                    var p0 = _points[idx]; p0.y = 0f;
                    for (int c = 0; c < 2; c++)
                    {
                        var clod = _entries[(i * 3 + e) * 2 + c];
                        float sideOff = (c == 0 ? 1f : -1f) * (0.17f + 0.03f * e);
                        clod.localPosition = p0 + SideOf(i) * sideOff + Vector3.up * 0.03f;
                        clod.localRotation = Quaternion.Euler(10f * (c == 0 ? 1f : -1f), VineYaw[i] + _branchTurn + 30f * e, 14f);
                        clod.localScale = Vector3.one * show;
                    }
                }
            }

            // THE EMBRACE: a limb to each prisoner (direction.md section 5.8). THE GRASP is the catch as the tree's own event,
            // once, when it has someone to reach for; a rejoiner's tree, posed long after its catch, draws none.
            float graspAt = PaeteRules.SentryCatchSeconds + CatchLead;
            if (!Staged && !_grasped && _targets.Count > 0 && age >= graspAt)
            {
                _grasped = true;
                if (age < graspAt + 0.6f) PaeteEmbraceFx.Grasp(transform.position, _crown, _eyes, _scale);
            }
            for (int i = 0; i < _targets.Count; i++) PoseLimb(i, age, wither);

            // The crown tops out (the last haul's stop, the squash): a few leaves shaken off its tips. ⚠️ v9 and v10 (owner: *"so that it
            // isnt too distracting"*, and its crown now carries only a few leaves): 5 over 1.2 m, was 16 over 2.6 m, and one leaf every
            // 2.4 s through the watch, was every 0.8 s.
            // 2026-10-07: every haul's STOP shakes a few out, not only the last (three, five, seven: `PaeteEmbraceFx.CrownShake`),
            // and they come down as leaves do, slowly, so some are still in the air when its eyes open.
            if (Emits && age - _lastAge < 0.5f)
                for (int k = 0; k < Heaves.Length; k++)
                    if (_lastAge < Heaves[k].y && age >= Heaves[k].y)
                        PaeteEmbraceFx.CrownShake(_crown.position, transform.position, k, _scale);
            if (!Staged && age > WakeAt + 0.6f && age < life - 0.5f && Mathf.FloorToInt(age / 2.4f) != Mathf.FloorToInt(_lastAge / 2.4f) && age - _lastAge < 0.5f)
            {
                int k = Mathf.FloorToInt(age / 2.4f) % LeafFrom.Length;
                PaeteLeafBurst.Spawn(transform.TransformPoint(LeafFrom[k] * _scale), 1, 0.35f);
            }

            // The pods: once, as it goes to sleep, the tree lets go of its seeds.
            if (!Staged && wither > 0.05f && !_podsDropped)
            {
                _podsDropped = true;
                // 2026-10-07: what the crown carried, let go one after another and left coming down after the tree is gone.
                PaeteEmbraceFx.Sleep(_crown.position, transform.position, _scale);
            }
            _lastAge = age;
        }

        /// <summary>
        /// The crawling vines, in the trunk's own (authored) space so they come up with it: each climbs further as it tops out and
        /// keeps climbing through the watch, winds slowly round the rope, carries a ripple travelling up it, and lifts its tip off the
        /// bark to feel the air. As it sleeps they slide back down.
        /// </summary>
        private void PoseTrunkVines(float age, float risen, float wither)
        {
            float climb = 0.30f + 0.45f * Mathf.Clamp01(risen) + 0.25f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - WakeAt) / 4f));
            climb *= 1f - 0.75f * wither;
            float since = Mathf.Max(0f, age);
            for (int v = 0; v < _trunkVines.Count; v++)
            {
                float start = TrunkVineRows[v, 0], turns = TrunkVineRows[v, 1], top = TrunkVineRows[v, 2], phase = TrunkVineRows[v, 3];
                float creep = TrunkVineRows[v, 4] * since, thick = TrunkVineRows[v, 5];
                _vinePoints.Clear(); _vineRadii.Clear();
                for (int j = 0; j <= TrunkVineSamples; j++)
                {
                    float u = j / (float)TrunkVineSamples;
                    float y = 0.02f + u * top * climb;
                    float a = (start + 360f * turns * u * climb + creep) * Mathf.Deg2Rad;
                    // The crawl: a ripple running UP the vine (its phase falls with time), and the tip lifting off the bark.
                    float ripple = 0.018f * Mathf.Sin(u * 16f - since * 2.6f + phase);
                    float feel = 0.09f * Mathf.SmoothStep(0.82f, 1f, u) * (0.55f + 0.45f * Mathf.Sin(since * 1.4f + phase));
                    // ⚠️ v11: the face is a burl grown out of the rope (`tools/build_paete_props.py` `sentry`); a vine that winds
                    // across it dives UNDER it, so nothing crawls over the eyes (the owner on film r20: *"eyes look really weird"*).
                    float r = RopeRadius(y) + 0.026f + ripple + feel - 0.16f * FaceBurl(a, y);
                    _vinePoints.Add(new Vector3(Mathf.Sin(a) * r, y + 0.04f * feel, Mathf.Cos(a) * r) + TrunkBend(y));
                    _vineRadii.Add(thick * Mathf.Lerp(1f, 0.35f, u));
                }
                PaeteInk.Tube(_trunkVines[v], _vinePoints, _vineRadii, 5);
                for (int k = 0; k < 2; k++)
                {
                    var leaf = _trunkVineLeaves[v * 2 + k];
                    int at = Mathf.Clamp(Mathf.RoundToInt(TrunkVineRows[v, 6 + k] * TrunkVineSamples), 1, TrunkVineSamples - 1);
                    var p = _vinePoints[at];
                    // A leaf never rides across the face: under the burl it is folded away.
                    bool onFace = FaceBurl(Mathf.Atan2(p.x, p.z), p.y) > 0.3f;
                    var along = (_vinePoints[at + 1] - _vinePoints[at - 1]).normalized;
                    var outward = new Vector3(p.x, 0f, p.z).normalized;
                    leaf.localPosition = p + outward * 0.03f;
                    // Each leaf rides the vine and flutters a little as the ripple passes it.
                    leaf.localRotation = Quaternion.LookRotation(Vector3.Lerp(along, outward, 0.55f), Vector3.up)
                                         * Quaternion.Euler(-20f + 10f * Mathf.Sin(since * 3.1f + v + k), 0f, 12f * k);
                    leaf.localScale = Vector3.one * (onFace ? 0f : Mathf.Clamp01(climb * 1.4f));
                }
            }
        }

        /// <summary>
        /// One prisoner's embrace: the limb leaves the trunk at 2 m on their side, comes down onto their waist and goes
        /// once and a half round it. It reaches out on the catch (following the body through the drag, which is what makes the drag
        /// read as the tree PULLING), holds while they are rooted, and on a break-out whips back into the trunk; on sleep it
        /// just draws back.
        ///
        /// ⚠️ 2026-10-07, WEIGHT AND TIMING. The times are the first limb's (out in 0.22 s from the catch, wound in 0.35 s from
        /// 0.18 s). What changed is how it moves inside them: it LASHES (most of the way in the first third of the reach) and
        /// REARS (its far end comes over high and down onto them); it is TAUT while it drags them in and goes slack, with one
        /// small bounce, when they stand against the trunk; the band comes round loose and CINCHES past tight; and it squeezes
        /// now and then, as the roots on their legs do. Let go, the band unwinds first and the limb draws back after it.
        /// ⚠️ THE BAND IS LAID ROUND THE BODY'S BOX, UNDER THE ARMS (`PaeteEmbraceFx.WaistHeight`). The first was a 0.28 m circle
        /// at 0.92 m: inside the redesigned bodies at the sides, and across their arms, which must stay free to throw.
        /// </summary>
        private void PoseLimb(int i, float age, float wither)
        {
            var p = _targets[i];
            var limb = _limbs[i];
            float catchAt = PaeteRules.SentryCatchSeconds + CatchLead;
            if (p == null || age < catchAt) { limb.Clear(); return; }

            Vector3 body = transform.InverseTransformPoint(p.transform.position);
            Vector3 flat = new Vector3(body.x, 0f, body.z);
            if (_arrive[i] < 0f) _arrive[i] = PaeteRules.SentryPullArriveSeconds(flat.magnitude);
            bool held = wither < 0.5f && (age <= catchAt + _arrive[i] + 0.3f || p.IsRooted);
            if (!held && _released[i] < 0f && age > catchAt + 0.1f) _released[i] = age;
            if (held) _released[i] = -1f;

            float since = age - catchAt;
            float lash = 1f - Mathf.Clamp01(since / 0.22f);
            float reach = 1f - lash * lash * lash;
            float wrap = Mathf.Clamp01((since - 0.18f) / 0.35f);
            bool landed = lash <= 0f;
            if (_released[i] >= 0f)
            {
                float back = Mathf.Clamp01((age - _released[i]) / 0.28f);
                wrap *= 1f - Mathf.Clamp01(back * 2f);
                reach *= 1f - Mathf.Clamp01(back * 2f - 1f);
                if (back >= 1f) { limb.Clear(); return; }
            }

            Vector3 dir = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward;
            // The trunk's own space is inside the scaled model: where the limb leaves the weave, in this body's space.
            // v9: the slimmer rope is 0.40 out at 1.55 authored (0.52 m and 2.0 m as it stands), so the limb leaves the bark, not the air.
            Vector3 anchor = transform.InverseTransformPoint(_trunk.TransformPoint(Quaternion.Inverse(_trunk.localRotation) * (dir * 0.40f) + Vector3.up * 1.55f));
            // ⚠️ v6: WHILE THE TRUNK IS STILL UNDER THE COURT (it crawls out now, and the catch keeps its old clock), the limb comes
            // out of the GROUND beside where it will stand, and the trunk carries it up as it hauls itself out.
            const float GroundOut = 0.12f;
            if (anchor.y < GroundOut)
            {
                var flatAnchor = new Vector3(anchor.x, 0f, anchor.z);
                if (flatAnchor.sqrMagnitude < 0.64f) flatAnchor = dir * 0.8f;
                anchor = new Vector3(flatAnchor.x, GroundOut, flatAnchor.z);
            }
            // The limb's two beats, each drawn once and only near its own moment (a rejoiner's limb is posed long after both).
            if (!Staged && _limbBeat[i] < 1)
            {
                _limbBeat[i] = 1;
                if (since < 0.6f) PaeteEmbraceFx.LimbLash(transform.TransformPoint(anchor), transform.TransformDirection(dir));
            }

            // Their own turn, in this body's space: the band lies on the box of THEIR body, whichever way they face.
            Quaternion facing = Quaternion.Inverse(transform.rotation) * p.transform.rotation;
            Vector3 waist = body + Vector3.up * PaeteEmbraceFx.WaistHeight;
            if (p.IsStruggling) waist += new Vector3(Mathf.Sin(age * 36f) * 0.03f, 0f, Mathf.Cos(age * 29f) * 0.02f);
            if (!Staged && _limbBeat[i] < 2 && held && landed)
            {
                _limbBeat[i] = 2;
                if (since < 0.9f) PaeteEmbraceFx.Catch(transform.TransformPoint(waist), transform.TransformDirection(-dir));
            }
            Vector3 toTree = anchor - waist; toTree.y = 0f;
            Vector3 theirs = Quaternion.Inverse(facing) * toTree;
            float a0 = Mathf.Atan2(theirs.x, theirs.z);
            // The band comes round loose, is yanked in past tight and let out to it; then it squeezes with the roots.
            float cinch = 1f + 0.26f * (1f - GrowthVfx.Pop((since - 0.50f) / 0.22f)) - 0.04f * PaeteEmbraceFx.Squeeze(since + 0.9f * i);
            Vector3 Hug(float angle, float lift)
                => waist + facing * (PaeteEmbraceFx.Hug(angle, PaeteEmbraceFx.WaistHalfWidth, PaeteEmbraceFx.WaistHalfDepth) * cinch) + Vector3.up * lift;
            Vector3 wrapStart = Hug(a0, PaeteEmbraceFx.WaistBandDrop * 0.5f);

            // The reach: out of the trunk, over, and down onto the waist.
            float span = Vector3.Distance(anchor, wrapStart);
            float arrived = since - _arrive[i];
            float taut = 1f - Mathf.SmoothStep(0f, 1f, arrived / 0.35f);
            float settle = arrived > 0f ? Mathf.Exp(-arrived * 3.5f) * Mathf.Sin(arrived * 15f) * 0.07f : 0f;
            float rear = lash * lash * Mathf.Min(1.0f, 0.25f * span);
            Vector3 home = anchor - wrapStart; home.y = 0f;
            home = home.sqrMagnitude > 1e-4f ? home.normalized : -dir;
            Vector3 c1 = anchor + dir * Mathf.Min(0.55f, span * 0.33f) + Vector3.up * Mathf.Lerp(0.32f, 0.10f, taut);
            Vector3 c2 = wrapStart + home * Mathf.Min(0.45f, span * 0.33f) + Vector3.up * (Mathf.Lerp(0.30f, 0.08f, taut) + rear);
            float sag = (1f - taut) * Mathf.Min(0.22f, span * 0.07f) + settle;
            // Sampled by its length (a 9 m limb through eleven points was a polygon), so its windings and knuckles hold their shape.
            int samples = Mathf.Clamp(Mathf.CeilToInt(span / 0.11f), 10, 64);
            _limb.Clear();
            for (int k = 0; k <= samples; k++)
            {
                float t = k / (float)samples, u = 1f - t;
                _limb.Add(u * u * u * anchor + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * wrapStart + Vector3.down * (sag * 4f * t * u));
            }
            // The band: round the waist (`PaeteEmbraceFx.WaistTurns`), climbing down it.
            _wrap.Clear();
            if (landed && wrap > 0f)
            {
                int whole = Mathf.RoundToInt(16f * PaeteEmbraceFx.WaistTurns);
                float shown = wrap * whole;
                for (int k = 1; k <= Mathf.CeilToInt(shown); k++)
                {
                    float f = Mathf.Min(k, shown) / whole;
                    _wrap.Add(Hug(a0 + f * Mathf.PI * 2f * PaeteEmbraceFx.WaistTurns, PaeteEmbraceFx.WaistBandDrop * (0.5f - f)));
                }
            }
            limb.Draw(_limb, reach, _wrap, PaeteEmbraceFx.WaistBandLength);
        }
    }

    /// <summary>
    /// ⚠️⚠️ BAKYA BLOOM IS A MAKILING PITCHER PLANT (owner, 2026-09-26: *"i wanted all his sentries (ult and
    /// attacker skill and defender skill TO ALL look diff and distinct and have their own style)"*, and of
    /// the concept sheet *"thats pretty fucking good"*). direction.md section 5.11. The v3 sprout wore the
    /// ultimate's woven bark and read as a small copy of the same tree; this is its own species: a leaf
    /// rosette, a thick S-neck, and a lime pitcher with a wine lip and a lid, in which the bakya grows.
    /// Posed from its age by `PaetePlant`:
    ///  * pop (0 to 0.45 s): pushes up out of the soil, overshoot, squash; the arm leaves flick open last;
    ///  * growing: the lid sits ajar and lifts as the clog grows; the clog rises out of the mouth;
    ///  * ready: the lid is open, the clog's toe over the lip, a proud bob;
    ///  * spit: the mouth rears back 0.12 s and SNAPS forward past rest, the lid slaps shut, a wobble;
    ///  * loosening (15 s on): the neck sags, the jug droops, the palette dries toward straw, roots lift;
    ///  * pulled: up, roots tearing free, flung toward the puller, gone.
    /// </summary>
    public sealed class PaetePlantBody : MonoBehaviour
    {
        /// <summary>
        /// The pitcher's own sixteen slots (`tools/build_paete_props.py` `PITCHER_PALETTE`, same hex): 0 body,
        /// 1 shade, 2 leaf, 3 leaf dark, 4 tendril, 5 bakya, 6 root, 7 moss, 8 ink, 9 inside, 10 lip,
        /// 11 unused, 12 lid underside, 13 bakya dark, 14 strap, 15 moss dark.
        /// </summary>
        public static readonly Color[] Palette =
        {
            Hex(0x9CCB3B), Hex(0x5F8F2A), Hex(0x4F9A2F), Hex(0x37752A), Hex(0x79AE38), Hex(0xC29563), Hex(0xB59A6C), Hex(0x5E7F24),
            Hex(0x1E140C), Hex(0x3A1420), Hex(0xB02A45), Hex(0xD6EE86), Hex(0xD9596B), Hex(0x8A6240), Hex(0xF29AA8), Hex(0xFFFDF6),
        };
        // ⚠️ REMODELLED 2026-10-07 AS A LITTLE CHARACTER (owner: "by 'rework' im thinking of remodelling, textures, and vfx..
        // not just vfx"; "try the cutesy character for the plants"). `tools/build_paete_bloom.py` now builds it, with the
        // hand companions' kit, in the redesigned cast's dress: a fat round jug with eyes and cheeks under its lip, a
        // leaf cap for a lid with a curl of sprout, plump leaves, root toes. The palette above is that script's, same
        // order: 11 is now the pale belly, 14 the blush, 15 white (the catchlights). The first model is kept at
        // `ArtSource/paete/props-pre-rework/seedling.glb`. What the character adds here: it blinks, squeezes its eyes
        // shut to spit, goes wide-eyed when loaded, and its sprout wags (`PoseFace`).
        private Transform _rosette, _sprout;
        private MaterialPropertyBlock _tint;
        private static readonly int TintId = Shader.PropertyToID("_Color");
        private readonly Transform[] _eyes = new Transform[2];
        private Quaternion _sproutRest;

        /// <summary>The face, from the same clocks as the rest of the pose: a pure function of them, so every peer agrees.</summary>
        private void PoseFace(float age, float pop, float charge, float sinceShot, bool ready, float loosen)
        {
            if (_rosette != null) _rosette.localScale = Vector3.one * Mathf.Max(0.001f, GrowthVfx.Pop((age - 0.10f) / 0.35f));
            // A blink every few seconds (two close together now and then), shut tight for the spit, wide when loaded,
            // heavy-lidded as it loosens.
            float beat = Mathf.Repeat(age + 0.6f, 3.4f);
            float blink = Mathf.Max(GrowthVfx.Envelope(beat, 0f, 0.05f, 0.07f, 0.07f), GrowthVfx.Envelope(beat, 0.26f, 0.05f, 0.33f, 0.07f) * (Mathf.Repeat(age, 6.8f) < 3.4f ? 1f : 0f));
            // ⚠️ `charge` stays at 1 for as long as a clog is loaded, so squeezing by it shut the eyes for the whole wait
            // (the first in-game film). Loaded is wide-eyed; the squeeze is the spit's own.
            float shut = Mathf.Max(blink, GrowthVfx.Envelope(sinceShot, 0f, 0.03f, 0.22f, 0.12f));
            float tall = Mathf.Lerp(1f + (ready ? 0.16f : 0f), 0.10f, Mathf.Clamp01(shut)) * (1f - 0.35f * loosen);
            float wide = 1f + 0.18f * Mathf.Clamp01(shut);
            foreach (var eye in _eyes) if (eye != null) eye.localScale = new Vector3(wide, Mathf.Max(0.05f, tall) * Mathf.Max(0.001f, pop), 1f);
            if (_sprout != null)
            {
                float wag = Mathf.Sin(age * 4.2f) * 9f + Mathf.Sin(age * 2.7f + 1f) * 5f;
                float flick = GrowthVfx.Envelope(sinceShot, 0.02f, 0.05f, 0.4f, 0.3f) * Mathf.Sin(sinceShot * 26f) * 24f;
                _sprout.localRotation = _sproutRest * Quaternion.Euler(-18f * charge + flick + 30f * loosen, 0f, wag * (1f - loosen));
            }
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private GameObject _model;
        private Transform _root, _stem, _pod, _lid, _shoe, _soil;
        private readonly List<Transform> _roots = new List<Transform>(), _arms = new List<Transform>();
        private readonly List<Quaternion> _rootRest = new List<Quaternion>(), _armRest = new List<Quaternion>();
        private Quaternion _podRest, _lidRest, _shoeRest, _stemRest;
        private Vector3 _podAt, _shoeAt, _shoeScale;
        private int _dryStep = -1;
        private static Color[][] _dryPalettes;

        public static PaetePlantBody Build(Transform parent)
        {
            var go = new GameObject("PaetePlantBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaetePlantBody>();
            b._root = go.transform;
            // ⚠️ A ROUND HEAVED MOUND, NOT A SQUARE (the v19 film: a flat brown 0.9 m tile under a loosened
            // plant read as a doormat). Three thin squares crossed at 0, 30 and 60 degrees make a twelve-sided
            // patch, and six clods sit on its rim where the roots have pushed the soil up, each placed by hand.
            b._soil = new GameObject("loose-soil").transform;
            b._soil.SetParent(b._root, false);
            var dark = Color.Lerp(GrowthVfx.Seed, Color.black, 0.25f);
            foreach (float yaw in new[] { 0f, 30f, 60f })
                GrowthVfx.Block(b._soil, "patch", new Vector3(0.78f, 0.03f, 0.78f), dark).transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            (Vector3 at, Vector3 size, float yaw)[] clods =
            {
                (new Vector3(0.34f, 0.04f, 0.10f), new Vector3(0.16f, 0.08f, 0.12f), 18f),
                (new Vector3(0.12f, 0.035f, 0.35f), new Vector3(0.13f, 0.07f, 0.11f), -30f),
                (new Vector3(-0.24f, 0.045f, 0.27f), new Vector3(0.15f, 0.09f, 0.13f), 41f),
                (new Vector3(-0.36f, 0.035f, -0.05f), new Vector3(0.12f, 0.07f, 0.12f), 7f),
                (new Vector3(-0.14f, 0.04f, -0.33f), new Vector3(0.16f, 0.08f, 0.11f), -52f),
                (new Vector3(0.25f, 0.03f, -0.26f), new Vector3(0.11f, 0.06f, 0.10f), 63f),
            };
            foreach (var clod in clods)
            {
                var piece = GrowthVfx.Block(b._soil, "clod", clod.size, GrowthVfx.Seed).transform;
                piece.localPosition = clod.at; piece.localRotation = Quaternion.Euler(8f, clod.yaw, -6f);
            }
            b._soil.gameObject.SetActive(false);
            b._model = PaeteProp.Spawn("seedling", b._root, Palette, ToonSkin.PersonOutlineWidth);
            b._stem = PaeteProp.Find(b._model, "stem") ?? new GameObject("stem").transform;
            if (b._stem.parent == null) b._stem.SetParent(b._root, false);
            b._stemRest = b._stem.localRotation;
            b._pod = PaeteProp.Find(b._model, "pod") ?? b._stem;
            b._podRest = b._pod.localRotation; b._podAt = b._pod.localPosition;
            b._lid = PaeteProp.Find(b._model, "lid");
            if (b._lid != null) b._lidRest = b._lid.localRotation;
            b._shoe = PaeteProp.Find(b._model, "slipper");
            if (b._shoe != null) { b._shoeScale = b._shoe.localScale; b._shoeAt = b._shoe.localPosition; b._shoeRest = b._shoe.localRotation; }
            b.CarveShoe();
            b._rosette = PaeteProp.Find(b._model, "rosette");
            b._eyes[0] = PaeteProp.Find(b._model, "eye-l"); b._eyes[1] = PaeteProp.Find(b._model, "eye-r");
            b._sprout = PaeteProp.Find(b._model, "sprout");
            if (b._sprout != null) b._sproutRest = b._sprout.localRotation;
            for (int i = 0; i < 4; i++) { var t = PaeteProp.Find(b._model, "root-" + i); if (t != null) { b._roots.Add(t); b._rootRest.Add(t.localRotation); } }
            for (int i = 0; i < 2; i++) { var t = PaeteProp.Find(b._model, "arm-" + i); if (t != null) { b._arms.Add(t); b._armRest.Add(t.localRotation); } }
            return b;
        }

        /// <summary>
        /// ⚠️ THE CLOG IN THE MOUTH IS THE CLOG THAT FLIES (owner, 2026-10-07, of a screenshot of the plant loaded: "did you
        /// fix the model for the bakya bloom"). The rework carved the clog it spits (`PaeteBloomFx.BuildBakya`) and left
        /// the one growing in its mouth as the model's own plain block, so a block went in and a carved bakya came out.
        /// The model's block is hidden and the carved one is hung in its place: same middle, same length, lying the way
        /// the block lay (its longest side is the clog's length, its thinnest the clog's height).
        /// </summary>
        private void CarveShoe()
        {
            if (_shoe == null) return;
            foreach (var r in _shoe.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            // ⚠️ THE PITCHER KEPT A PITCHER (2026-10-07, `tools/build_paete_bloom.py` variant d): the clog grows down in
            // the belly, toe UP the jug (which leans `JugLean` forward), its strap toward the front, and is raised up
            // the neck to the mouth. The stand-in in the model is only there for the Blender review.
            var holder = new GameObject("carved-bakya").transform;
            holder.SetParent(_shoe, false);
            holder.localRotation = Quaternion.LookRotation(JugUp, Quaternion.Euler(JugLean, 0f, 0f) * Vector3.forward);
            // 0.8 of the clog that flies: the neck is 0.24 m across inside and the clog 0.13 wide.
            holder.localScale = Vector3.one * 0.8f;
            PaeteBloomFx.BuildBakya(holder).localPosition = new Vector3(0f, -0.045f, 0f);
        }

        /// <summary>The jug leans this far forward, degrees (`TILT` in the build script), and so this is "up the jug" in the pod's space.</summary>
        private const float JugLean = 24f;
        private static readonly Vector3 JugUp = Quaternion.Euler(JugLean, 0f, 0f) * Vector3.up;
        /// <summary>How far up the jug the clog is raised from the belly to stand in the mouth, toe over the lip, and how far it tips forward there.</summary>
        private const float ShoeRise = 0.41f, ShoeTip = 22f;
        /// <summary>The lid rests half open (28 degrees, in the model). Loaded it swings this much further up; on the spit it slaps this far down; loosened it droops.</summary>
        private const float LidOpen = 46f, LidSlap = 24f, LidDroop = 14f;

        /// <summary>The pitcher's palette with the living greens dried toward straw, in four steps (cached).</summary>
        private static Color[] DryPalette(int step)
        {
            if (_dryPalettes == null) _dryPalettes = new Color[4][];
            if (_dryPalettes[step] == null)
            {
                var p = (Color[])Palette.Clone();
                // ⚠️ STRAW-BROWN, NOT `GrowthVfx.Dry`'S YELLOW (the v14 film: a bright yellow flower, not a dying one).
                float k = step / 3f * 0.75f;
                var straw = new Color(0.55f, 0.47f, 0.27f);
                foreach (int slot in new[] { 0, 1, 2, 3, 4, 7, 11 }) p[slot] = Color.Lerp(Palette[slot], straw, k);
                _dryPalettes[step] = p;
            }
            return _dryPalettes[step];
        }

        // ⚠️ IT TURNS TO ITS SHOT (2026-10-07). The pitcher never turned: it reared and snapped along its own front
        // whichever way the clog went, so a shot to its side left through the wall of the jug. `PaetePlant.Fire` now
        // gives it the way of the shot, and over the first tenth of a second after it the whole plant whips round to
        // face it (and stays: the can it shoots at seldom moves). Look only, set from the same call on every peer.
        private float _yawFrom, _yawTo, _yawNow, _readyAt = -99f;
        private bool _wasReady;

        /// <summary>The way the shot just fired goes (world, flat). The plant faces it from the next pose on.</summary>
        public void AimAt(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) return;
            _yawFrom = _yawNow;
            _yawTo = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        public void Pose(float age, float loosen, bool pullable, float shotGrowth, float sinceShot)
        {
            bool landed = age >= 0f;
            _root.gameObject.SetActive(landed);
            if (!landed) return;
            _yawNow = Mathf.LerpAngle(_yawFrom, _yawTo, Mathf.SmoothStep(0f, 1f, sinceShot / 0.10f));
            _root.localRotation = Quaternion.Euler(0f, _yawNow, 0f);
            // ⚠️ IT RISES OUT OF THE SOIL, IT DOES NOT SCALE IN (owner, 2026-09-26).
            float rise = GrowthVfx.Pop(age / 0.45f);
            float squash = age > 0.35f && age < 0.6f ? 1f + 0.18f * Mathf.Sin((age - 0.35f) / 0.25f * Mathf.PI) : 1f;
            // Store the wind-up as the bakya finishes growing, BEFORE the command
            // launches it. The old recoil curve wound up after the projectile left.
            float grown = Mathf.Clamp01(shotGrowth);
            float chargeStart = Mathf.Max(0f, 1f - .6f / PaeteRules.PlantReloadSeconds);
            float charge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(chargeStart, 1f, grown));
            float release = sinceShot < .38f ? 1f - Mathf.SmoothStep(0f, 1f, sinceShot / .38f) : 0f;
            squash *= 1f + .10f * charge - .05f * release;
            _stem.localScale = new Vector3(squash, 1f / squash, squash);
            _stem.localPosition = Vector3.down * (1.0f * (1f - rise));
            float sway = Mathf.Sin(age * 1.2f) * 2.5f, swayZ = Mathf.Sin(age * 0.85f + 1f) * 2.5f;
            // ⚠️ THE RECOIL IT CAN BE SEEN TO TAKE (2026-10-07): the mouth snaps forward with the spit, and a beat later the
            // whole neck is thrown BACK by it and rocks to rest, forward and back, over the next second.
            float kick = sinceShot > 0.04f && sinceShot < 1.3f ? -15f * Mathf.Sin((sinceShot - 0.04f) * 15f) * Mathf.Exp(-(sinceShot - 0.04f) * 5.0f) : 0f;
            _stem.localRotation = _stemRest * Quaternion.Euler(sway + 16f * loosen + kick, 0f, swayZ + 6f * loosen);
            float pop = Mathf.Clamp01((age - 0.15f) / 0.4f);

            // Loosening: roots lift out, the soil ring shows and breathes.
            for (int i = 0; i < _roots.Count; i++)
            {
                _roots[i].localRotation = _rootRest[i] * Quaternion.Euler(-25f * loosen, 0f, 0f);
                _roots[i].localPosition = new Vector3(0f, 0.08f * loosen, 0f);
                _roots[i].localScale = Vector3.one * Mathf.Max(0.001f, pop);
            }
            _soil.gameObject.SetActive(pullable);
            if (pullable)
            {
                float pulse = 1f + 0.06f * Mathf.Sin(age * 5.0f);
                _soil.localScale = new Vector3(pulse, 1f + 0.5f * loosen, pulse);
            }

            // The pitcher: a beat behind the neck (follow-through); the spit (rear back, snap past rest,
            // wobble); the proud bob when ready; the droop as it loosens. Negative pitch rears the mouth back.
            float settle = sinceShot >= .12f && sinceShot < .65f
                ? Mathf.Sin((sinceShot - .12f) * 19f) * Mathf.Exp(-(sinceShot - .12f) * 6f) * 7f : 0f;
            float spit = -24f * charge + 28f * release + settle;
            bool ready = grown >= 1f && sinceShot > 0.6f;
            float proud = ready ? Mathf.Abs(Mathf.Sin(age * 3.2f)) * 2f : 0f;
            float follow = Mathf.Sin(age * 1.2f - 0.7f) * 4f;
            // LOADED (2026-10-07): the moment the clog is grown the jug hops and shakes its head once, side to side, and is
            // still again. Once per clog, half a second: it happens every five seconds for forty.
            if (ready && !_wasReady) _readyAt = age;
            _wasReady = ready;
            float sinceReady = age - _readyAt;
            float shake = sinceReady >= 0f && sinceReady < 0.6f ? Mathf.Sin(sinceReady * 36f) * Mathf.Exp(-sinceReady * 7f) * 13f : 0f;
            float hop = sinceReady >= 0f && sinceReady < 0.22f ? Mathf.Sin(sinceReady / 0.22f * Mathf.PI) * 0.06f : 0f;
            _pod.localRotation = _podRest * Quaternion.Euler(spit + 38f * loosen + follow - proud, 0f, 12f * loosen + shake);
            _pod.localPosition = _podAt + Vector3.up * (0.03f * proud / 5f + hop);

            // The lid: ajar while the clog grows, lifting with it, open when it is ready; it SLAPS shut on the
            // spit and then lifts again as the next one grows; limp half-shut when the plant loosens. Its rest
            // lies over the mouth, and a NEGATIVE pitch lifts it (its +Z runs forward from the hinge).
            if (_lid != null)
            {
                // The hood: up as the clog comes up, SLAPPED down on the spit, drooping as the plant loosens.
                float open = -LidOpen * Mathf.Max(grown * grown, sinceShot < .12f ? 1f : 0f);
                float slap = GrowthVfx.Envelope(sinceShot, 0.12f, 0.04f, 0.5f, 0.4f);
                open = Mathf.Lerp(open, LidSlap, slap);
                open = Mathf.Lerp(open, LidDroop, loosen);
                _lid.localRotation = _lidRest * Quaternion.Euler(open, 0f, 0f);
            }

            // The bakya grows inside and rises until its toe hangs over the lip.
            if (_shoe != null)
            {
                float up = GrowthVfx.Pop(Mathf.Clamp01((grown - 0.35f) / 0.65f));
                _shoe.localScale = _shoeScale * Mathf.Max(0.001f, Mathf.Clamp01(grown * 1.4f));
                // Up the neck from the belly until it stands in the mouth, toe over the lip, tipping forward.
                _shoe.localPosition = _shoeAt + JugUp * (ShoeRise * up);
                _shoe.localRotation = _shoeRest * Quaternion.Euler(ShoeTip * up, 0f, 0f);
                _shoe.gameObject.SetActive(grown > 0.02f && sinceShot > 0.13f);
            }

            // The arm leaves: flick open after the pop, breathe, flare back at the command, droop dry.
            for (int i = 0; i < _arms.Count; i++)
            {
                float flick = GrowthVfx.Pop((age - 0.30f - 0.06f * i) / 0.30f);
                float breathe = Mathf.Sin(age * 1.6f + i * 1.9f) * 4f;
                float armFlare = GrowthVfx.Envelope(sinceShot, 0f, 0.08f, 0.45f, 0.3f);
                float pitch = -60f * (1f - flick) + breathe - 10f * charge - 24f * armFlare + 26f * loosen;
                _arms[i].localRotation = _armRest[i] * Quaternion.Euler(pitch, 0f, 0f);
            }

            PoseFace(age, pop, charge, sinceShot, ready, loosen);

            // The greens dry in four steps (a palette re-dress, cached per step by `ToonSkin`).
            int step = Mathf.Clamp(Mathf.FloorToInt(loosen * 4f), 0, 3);
            if (step != _dryStep && _model != null)
            {
                _dryStep = step;
                PaeteProp.Redress(_model, DryPalette(step));
                // ⚠️ THE PAINTED PARTS DO NOT TAKE A PALETTE (their colour is the atlas's), so they are tinted toward
                // straw by the same step, or a loosened plant would dry only in its eyes and cheeks.
                _tint ??= new MaterialPropertyBlock();
                var dry = Color.Lerp(Color.white, new Color(0.86f, 0.74f, 0.52f), step / 3f * 0.8f);
                foreach (var r in _model.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.GetComponent<GrowthMeshOwner>() != null) continue;
                    r.GetPropertyBlock(_tint);
                    _tint.SetColor(TintId, dry);
                    r.SetPropertyBlock(_tint);
                }
            }
        }

        /// <summary>The pull-out: up, roots tearing free, tipped toward the puller, then gone.</summary>
        public void PosePulled(float t, Vector3 towardPuller)
        {
            towardPuller.y = 0f;
            Vector3 dir = towardPuller.sqrMagnitude > 0.01f ? towardPuller.normalized : Vector3.back;
            float heave = Mathf.Clamp01(t / 0.35f);
            float fling = Mathf.Clamp01((t - 0.35f) / 0.6f);
            _root.localPosition = Vector3.up * (0.5f * GrowthVfx.Pop(heave)) + dir * 0.8f * fling + Vector3.down * 1.2f * fling * fling;
            _root.localRotation = Quaternion.AngleAxis(-70f * fling, Vector3.Cross(Vector3.up, dir)) * Quaternion.Euler(0f, _yawNow, 0f);
            for (int i = 0; i < _roots.Count; i++) _roots[i].localRotation = _rootRest[i] * Quaternion.Euler(-60f * heave, 0f, 0f);
            _soil.gameObject.SetActive(false);
            _root.localScale = Vector3.one * Mathf.Max(0.001f, 1f - Mathf.Clamp01((t - 0.7f) / 0.25f));
        }
    }

    /// <summary>
    /// ⚠️⚠️ THORN HARVEST IS AN ARMED RATTAN (owner, 2026-09-26: the attacker and defender plants must be
    /// their own species; of the first rattan, *"it looks like  a flimsy plant and not a dangerous cool
    /// plant"*). direction.md section 5.11. A clump of uway round the spot he stamped, the centre clear for
    /// his feet: a ring of black spines, five sheath stems with spine collars, and on each a cane frond that
    /// rears and hooks over like a talon, with a barbed straw whip (the cirrus, the arnis stick's cane)
    /// coiled under its arch. Beats: BURST (stems punch up in a ripple, fronds folded), REAR UP (talons
    /// open, spines bristle), REACH and HOLD (the whip facing each slipper uncoils along the ground and its
    /// grapnel bites, taut and quivering through the hold), YANK (reeled home), CLENCH (the talons close
    /// over the centre like a fist), SINK (blades brown, stems sink one by one).
    /// </summary>
    public sealed class PaeteThornBody : MonoBehaviour
    {
        /// <summary>
        /// The rattan's own sixteen slots (`tools/build_paete_props.py` `RATTAN_PALETTE`, same hex): 0 blade,
        /// 1 blade dark, 2 blade lit, 3 frond cane, 4 sheath, 5 sheath dark, 6 straw cane, 7 node band, 8 ink,
        /// 9 spine, 10 bone tip, 11 soil, 12 cane lit, 13 to 15 his bark.
        /// </summary>
        public static readonly Color[] Palette =
        {
            Hex(0x2F4219), Hex(0x1F2D10), Hex(0x4A5F26), Hex(0x6E6A34), Hex(0x34301A), Hex(0x221F10), Hex(0xD8B86E), Hex(0x7E5E2E),
            Hex(0x1E140C), Hex(0x130E09), Hex(0xC9BC98), Hex(0x6B4A2E), Hex(0xEAD49C), Hex(0xF08A8A), Hex(0xFFFDF6), Hex(0xB9D36A),
        };
        // ⚠️ REMODELLED 2026-10-07 AS A LITTLE CHARACTER (owner: "by 'rework' im thinking of remodelling, textures, and vfx..
        // not just vfx"; "try the cutesy character for the plants"; of its face, "im thinking of cute angry"; of a first
        // bud covered in spines, "its just too much", "the vines were okay, its more the bud itself").
        // `tools/build_paete_rattan.py` builds it with the hand companions' kit and a painted atlas: the same clump of
        // canes, fronds and whips, rounder, and in the middle of them a plain smooth BUD with a cute angry face, whose
        // arms the canes are. Slots 13 to 15 were spare and are now its blush, white and pale belly. The first model
        // is kept at `ArtSource/paete/props-pre-rework/thorns.glb`. What the bud does here: `PoseHeart`.
        private Transform _heart;
        private readonly Transform[] _eyes = new Transform[2];
        private Vector3 _heartAt;
        private MaterialPropertyBlock _tint;
        private static readonly int TintId = Shader.PropertyToID("_Color");

        /// <summary>
        /// The bud, from the age alone (so every peer and a rejoiner agree): it pops up a beat after the canes, glares
        /// through the hold (eyes narrowed), goes wide and hops as the slippers come home, ducks as the fist of fronds
        /// closes over it, blinks now and then, and sinks with the clump.
        /// </summary>
        private void PoseHeart(float age, float hold, float yank)
        {
            if (_heart == null) return;
            float up = GrowthVfx.Pop((age - 0.06f) / 0.24f);
            float gone = Mathf.Clamp01((age - 2.40f) / 0.32f);
            float hop = GrowthVfx.Envelope(age, hold + yank * 0.55f, 0.08f, hold + yank + 0.05f, 0.14f);
            float duck = GrowthVfx.Envelope(age, hold + yank + 0.10f, 0.08f, 2.30f, 0.25f);
            _heart.localPosition = _heartAt + Vector3.down * (0.62f * (1f - up) + 0.80f * gone * gone) + Vector3.up * (0.07f * hop - 0.06f * duck);
            // It breathes hard: a quick puff in and out while it holds on.
            float puff = 1f + 0.035f * Mathf.Sin(age * 11f) * GrowthVfx.Envelope(age, 0.3f, 0.1f, hold + yank, 0.2f);
            _heart.localScale = (up > 0.001f && gone < 1f) ? new Vector3(puff, 1f / puff, puff) : Vector3.one * 0.001f;
            _heart.localRotation = Quaternion.Euler(-10f * hop + 8f * duck, Mathf.Sin(age * 3.1f) * 5f * (1f - duck), 0f);

            float beat = Mathf.Repeat(age + 0.2f, 1.9f);
            float blink = GrowthVfx.Envelope(beat, 0f, 0.04f, 0.06f, 0.06f);
            float glare = GrowthVfx.Envelope(age, 0.30f, 0.08f, hold, 0.08f);
            float tall = Mathf.Lerp(1f - 0.30f * glare + 0.18f * hop, 0.10f, Mathf.Max(blink, duck * 0.8f));
            foreach (var eye in _eyes) if (eye != null) eye.localScale = new Vector3(1f + 0.10f * glare, Mathf.Max(0.06f, tall), 1f);
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private const int Stems = 5;
        private GameObject _model;
        private Transform _clump;
        private readonly Transform[] _sheath = new Transform[Stems], _frond = new Transform[Stems], _whip = new Transform[Stems];
        private readonly Vector3[] _sheathAt = new Vector3[Stems], _frondAt = new Vector3[Stems];
        private readonly Quaternion[] _frondRest = new Quaternion[Stems];
        private readonly List<Slipper> _targets = new List<Slipper>();
        private readonly List<int> _stemFor = new List<int>();
        private readonly List<PaeteRope> _lashes = new List<PaeteRope>();
        private readonly List<Vector3> _points = new List<Vector3>();
        // ⚠️ 2026-10-07, the ability rework: each whip carries a grapnel head and a row of barbs that bristle when it
        // goes taut (`PaeteThornFx.cs`). All posed from the age, like the rest of the body.
        private readonly List<PaeteThornGrapnel> _heads = new List<PaeteThornGrapnel>();
        private readonly List<Mesh> _barbs = new List<Mesh>();
        private static readonly float[] BarbAt = { 0.20f, 0.36f, 0.52f, 0.68f, 0.83f };
        private Vector3 _modelSize = Vector3.one;
        private Color[] _dry;
        private bool _dressedDry;
        // Each stem's own beats: when it punches up, and when it sinks at the end.
        private static readonly float[] RiseAt = { 0.00f, 0.05f, 0.02f, 0.09f, 0.035f };
        private static readonly float[] SinkAt = { 2.45f, 2.62f, 2.52f, 2.70f, 2.40f };
        private static readonly float[] Quiver = { 4f, 5.5f, 3.5f, 5f, 4.5f };

        public static PaeteThornBody Build(Transform parent, List<Slipper> targets)
        {
            var go = new GameObject("PaeteThornBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaeteThornBody>();
            b._model = PaeteProp.Spawn("thorns", go.transform, Palette, ToonSkin.PersonOutlineWidth);
            b._clump = PaeteProp.Find(b._model, "clump");
            b._heart = PaeteProp.Find(b._model, "heart");
            if (b._heart != null) b._heartAt = b._heart.localPosition;
            b._eyes[0] = PaeteProp.Find(b._model, "eye-l"); b._eyes[1] = PaeteProp.Find(b._model, "eye-r");
            for (int i = 0; i < Stems; i++)
            {
                b._sheath[i] = PaeteProp.Find(b._model, "sheath-" + i);
                b._frond[i] = PaeteProp.Find(b._model, "frond-" + i);
                b._whip[i] = PaeteProp.Find(b._model, "whip-" + i);
                if (b._sheath[i] != null) b._sheathAt[i] = b._sheath[i].localPosition;
                if (b._frond[i] != null) { b._frondRest[i] = b._frond[i].localRotation; b._frondAt[i] = b._frond[i].localPosition; }
            }
            // ⚠️ STRAW CANE WITH DARK NODE BANDS: the rattan's own whip, not the ultimate's woven bark rope.
            var cane = new[] { Palette[6], Palette[7] };
            foreach (var shoe in targets)
            {
                b._targets.Add(shoe);
                b._stemFor.Add(b.StemFacing(shoe != null ? shoe.transform.position : go.transform.position + go.transform.forward));
                b._lashes.Add(new PaeteRope(go.transform, "rattan-whip", cane, new[] { 0.026f, 0.012f }, 0.012f, 3f, b._targets.Count * 1.3f));
                b._heads.Add(new PaeteThornGrapnel(go.transform));
                var barbs = PaeteThornShapes.Dynamic("PaeteWhipBarbs");
                GrowthVfx.Part(go.transform, "rattan-whip-barbs", barbs, Palette[9]);
                b._barbs.Add(barbs);
            }
            if (b._model != null) b._modelSize = b._model.transform.localScale;
            return b;
        }

        /// <summary>The stem whose frond faces <paramref name="world"/> best: the lash leaves THAT frond.</summary>
        private int StemFacing(Vector3 world)
        {
            Vector3 d = world - transform.position; d.y = 0f;
            int best = 0; float bestDot = -2f;
            for (int i = 0; i < Stems; i++)
            {
                if (_frond[i] == null) continue;
                Vector3 f = _frond[i].position - transform.position; f.y = 0f;
                float dot = Vector3.Dot(f.normalized, d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward);
                if (dot > bestDot) { bestDot = dot; best = i; }
            }
            return best;
        }

        public void Pose(float age, Vector3 origin)
        {
            float hold = PaeteRules.ThornHoldSeconds, yank = PaeteRules.ThornYankSeconds;
            float quiver = GrowthVfx.Envelope(age, 0.18f, 0.04f, hold + 0.05f, 0.06f);
            float whipBack = GrowthVfx.Envelope(age, hold, 0.08f, hold + yank, 0.2f);
            float clench = GrowthVfx.Envelope(age, hold + yank - 0.05f, 0.25f, 2.35f, 0.3f);
            // ⚠️ THE PUNCH-UP IS EXAGGERATED (2026-10-07): the whole rattan comes up stretched tall and thin, lands squashed
            // and wide, and springs back to itself; and it ducks once more as the fist snaps shut. On the model's root, so
            // sheaths, fronds and whips squash together about the ground and nothing parts from what it grows on.
            if (_model != null)
            {
                float tall = age < 0.12f ? 1.34f
                           : age < 0.22f ? Mathf.Lerp(1.34f, 0.76f, (age - 0.12f) / 0.10f)
                           : age < 0.34f ? Mathf.Lerp(0.76f, 1.08f, (age - 0.22f) / 0.12f)
                           : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((age - 0.34f) / 0.12f));
                tall *= 1f - 0.13f * GrowthVfx.Envelope(age, hold + yank + 0.13f, 0.05f, hold + yank + 0.33f, 0.12f);
                float wide = 1f / Mathf.Sqrt(tall);
                _model.transform.localScale = Vector3.Scale(_modelSize, new Vector3(wide, tall, wide));
            }
            PoseHeart(age, hold, yank);
            if (_clump != null)
            {
                float c = GrowthVfx.Pop(age / 0.18f), gone = Mathf.Clamp01((age - 2.55f) / 0.4f);
                _clump.localPosition = Vector3.down * (0.3f * (1f - Mathf.Clamp01(c)) + 0.4f * gone);
            }
            for (int i = 0; i < Stems; i++)
            {
                float up = GrowthVfx.Pop((age - RiseAt[i]) / 0.22f);
                float sink = Mathf.Clamp01((age - SinkAt[i]) / 0.35f);
                float drop = 0.6f * (1f - up) + 0.7f * sink * sink;
                if (_sheath[i] != null)
                {
                    _sheath[i].localPosition = _sheathAt[i] + Vector3.down * drop;
                    _sheath[i].localScale = Vector3.one * (up > 0.001f && sink < 1f ? 1f : 0.001f);
                }
                if (_frond[i] == null) continue;
                // Negative pitch rears the frond up and in; positive flops it out. Folded on the burst,
                // opening with an overshoot, quivering through the hold, jolting up on the yank, closing
                // over the centre on the clench, flopping out as it sinks.
                float open = Mathf.Clamp01((age - RiseAt[i] - 0.08f) / 0.22f);
                float fold = -38f * (1f - GrowthVfx.Pop(open));
                float shiver = Mathf.Sin(age * 18f + i * 2.3f) * Quiver[i] * quiver;
                float pitch = fold + shiver - 18f * whipBack - 58f * clench + 48f * sink;
                // A last dry flick: each frond jerks up once just before its stem goes under.
                pitch -= 30f * GrowthVfx.Envelope(age, SinkAt[i] - 0.13f, 0.05f, SinkAt[i] + 0.05f, 0.10f);
                // The frond is the sheath's sibling in the file (both under the root), so it drops with it.
                _frond[i].localPosition = _frondAt[i] + Vector3.down * drop;
                _frond[i].localRotation = _frondRest[i] * Quaternion.Euler(pitch, 0f, 0f);
                _frond[i].localScale = Vector3.one * (up > 0.001f && sink < 1f ? 1f : 0.001f);
            }
            // The whips: the coiled one under a reaching frond hides while its lash is out.
            for (int i = 0; i < Stems; i++) if (_whip[i] != null) _whip[i].gameObject.SetActive(true);
            for (int k = 0; k < _targets.Count; k++)
            {
                var shoe = _targets[k];
                int stem = _stemFor[k];
                if (shoe == null || age > hold + yank + 0.25f || _whip[stem] == null) { _lashes[k].Clear(); _heads[k].Hide(); _barbs[k].Clear(); continue; }
                _whip[stem].gameObject.SetActive(false);
                Vector3 from = transform.InverseTransformPoint(_whip[stem].position);
                Vector3 to = transform.InverseTransformPoint(shoe.transform.position);
                // ⚠️⚠️ A WHIP, NOT A LINE THAT GETS LONGER (2026-10-07, the ability rework). It was one eased straight line with
                // a faint wave. Now, all from the age:
                // - THE REACH: the tip ACCELERATES out (slow off the frond, fastest as it arrives, and stops dead on the
                //   slipper), the line arched up and over with a wave running out along it that is widest near the tip;
                // - THE BITE (`ThornReachSeconds`): the line snaps dead straight and twangs like a plucked string, dying in a
                //   fifth of a second, and its barbs flare;
                // - THE HOLD: straight, with a fine tremble; the barbs quiver;
                // - THE YANK: humps of slack run HOME along it from the slipper to the frond, three of them, each smaller;
                // - after the slipper lands: the head lets go and the whip is drawn back into its frond.
                float reach = PaeteRules.ThornReachSeconds;
                float outT = Mathf.Clamp01(age / reach), since = age - reach;
                float let = Mathf.Clamp01((age - hold - yank) / 0.25f);
                Vector3 tip = Vector3.Lerp(from, to, Mathf.Pow(outT, 1.6f));
                if (let > 0f) tip = Vector3.Lerp(to, from, let * let * (3f - 2f * let));
                Vector3 line = tip - from;
                float length = line.magnitude;
                Vector3 along = length > 1e-4f ? line / length : Vector3.forward;
                Vector3 side = Vector3.Cross(along, Vector3.up);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
                side.Normalize();
                Vector3 over = Vector3.Cross(side, along);
                float swing = Mathf.Min(length, 3f), fly = 1f - outT;
                float twang = since > 0f ? Mathf.Exp(-since * 15f) * Mathf.Cos(since * 72f) : 0f;
                float tremble = GrowthVfx.Envelope(age, reach, 0.03f, hold + 0.04f, 0.06f);
                float reel = Mathf.Clamp01((age - hold) / yank);
                float hump = age > hold && let <= 0f ? 1f - Mathf.Repeat(reel * 3f, 1f) : -1f;
                const int Samples = 18;
                _points.Clear();
                for (int n = 0; n <= Samples; n++)
                {
                    float u = n / (float)Samples, bell = Mathf.Sin(u * Mathf.PI);
                    Vector3 p = from + line * u;
                    if (age < reach)
                    {
                        float wave = u * 8.5f - age * 36f + k * 1.3f;
                        p += over * (swing * 0.15f * bell * fly)
                           + (side * Mathf.Sin(wave) + over * 0.5f * Mathf.Cos(wave)) * (swing * 0.10f * fly * bell * (0.3f + 0.7f * u));
                    }
                    p += (over * 0.75f + side * 0.45f) * (0.075f * twang * bell);
                    p += side * (0.006f * Mathf.Sin(age * 70f + u * 26f) * tremble * bell);
                    if (hump >= 0f)
                    {
                        float d = (u - hump) / 0.12f;
                        p += (over * 0.8f + side * (k % 2 == 0 ? 0.45f : -0.45f)) * (0.26f * (1f - 0.6f * reel) * Mathf.Exp(-d * d) * Mathf.Sqrt(Mathf.Max(0f, bell)));
                    }
                    // Never under the court: the reach's wave may not dip through it.
                    if (n > 0 && p.y < 0.02f) p.y = 0.02f;
                    _points.Add(p);
                }
                _lashes[k].Draw(_points, 1.0f);

                // The barbs along it: laid nearly flat while it flies, flaring past full on the bite, quivering in the hold,
                // laid back down as it lets go. Each on its own side of the cane, raked back toward the plant.
                float bristle = (since < 0f ? 0.3f * outT : 0.3f + 0.7f * GrowthVfx.Pop(since / 0.09f)) * (1f - let);
                PaeteThornShapes.Begin();
                for (int j = 0; j < BarbAt.Length; j++)
                {
                    int at = Mathf.Clamp(Mathf.RoundToInt(BarbAt[j] * Samples), 1, Samples - 1);
                    Vector3 run = (_points[at + 1] - _points[at - 1]).normalized;
                    Vector3 outward = Quaternion.AngleAxis(j * 137f + k * 50f, run) * over;
                    float shake = 1f + 0.22f * Mathf.Sin(age * 46f + j * 2.1f) * tremble;
                    PaeteThornShapes.Spike(_points[at], _points[at] + (outward * 0.85f - run * 0.5f).normalized * (0.17f * bristle * shake), 0.02f);
                }
                PaeteThornShapes.Apply(_barbs[k]);

                // The grapnel: flying open and spinning, shut past closed on the bite and settling, open again as it lets go.
                Vector3 end = (_points[Samples] - _points[Samples - 1]);
                float grip = (since < 0f ? 0f : GrowthVfx.Pop(since / 0.09f)) * (1f - let);
                float size = 1.3f * GrowthVfx.Pop(age / 0.10f) * (1f + (since > 0f ? 0.45f * Mathf.Exp(-since * 11f) : 0f)) * (1f - 0.9f * Mathf.Clamp01((let - 0.6f) / 0.4f));
                _heads[k].Pose(_points[Samples], end.sqrMagnitude > 1e-8f ? end : along, grip, size, since, let, age);
            }

            // The blades brown as it sinks (one re-dress, cached by `ToonSkin`).
            bool dry = age > 2.35f;
            if (dry != _dressedDry && _model != null)
            {
                _dressedDry = dry;
                if (_dry == null)
                {
                    _dry = (Color[])Palette.Clone();
                    var straw = new Color(0.45f, 0.38f, 0.20f);
                    foreach (int slot in new[] { 0, 1, 2, 3 }) _dry[slot] = Color.Lerp(Palette[slot], straw, 0.6f);
                }
                PaeteProp.Redress(_model, dry ? _dry : Palette);
                // The painted parts take no palette (their colour is the atlas's): they are tinted toward straw instead.
                _tint ??= new MaterialPropertyBlock();
                var dried = dry ? new Color(0.80f, 0.70f, 0.48f) : Color.white;
                foreach (var r in _model.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.GetComponent<GrowthMeshOwner>() != null) continue;
                    r.GetPropertyBlock(_tint);
                    _tint.SetColor(TintId, dried);
                    r.SetPropertyBlock(_tint);
                }
            }
        }
    }
}
