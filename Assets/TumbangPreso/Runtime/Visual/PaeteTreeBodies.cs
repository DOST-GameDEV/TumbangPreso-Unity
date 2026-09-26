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
    /// Beats, all from age (so pause, replay and a probe agree):
    ///  * CRACK 0 to 0.12: crack plates radiate from the seed (and `PaeteGroundBreak`, from the hazard).
    ///  * ERUPT 0.06 to 0.56: the woven trunk screws up out of the road a quarter turn, overshoots, and
    ///    SQUASHES on the stop before springing back.
    ///  * GRIP 0.40 to 0.70: the six claw roots slam down one after another, soil kicked at each.
    ///  * UNFURL 0.24 to 0.62: the crown opens like a hand; the core swells in among it.
    ///  * WAKE 0.55 to 0.85: the light in the two hollows opens; the tree leans at its first prisoner.
    ///  * EMBRACE from the catch: the crown clenches, and a woven limb reaches out of the trunk to each
    ///    prisoner, drags them in and wraps their waist (arms and head free: they can still throw).
    ///  * WATCH to 9.5 s: it breathes, sways, blinks at its own times and looks from one prisoner to the
    ///    next; a leaf falls now and then.
    ///  * SLEEP from 9.6 s: the light shuts, the crown droops, the limbs let go and pull back, the trunk
    ///    unscrews back into the road, narra pods drop.
    /// </summary>
    public sealed class PaeteSentryBody : MonoBehaviour
    {
        private GameObject _model;
        private Transform _trunk, _crown, _eyes, _core;
        private readonly List<Transform> _roots = new List<Transform>();
        private readonly List<Quaternion> _rootRest = new List<Quaternion>();
        private readonly List<Vector3> _rootScale = new List<Vector3>();
        private readonly List<Transform> _claws = new List<Transform>();
        private readonly List<Quaternion> _clawRest = new List<Quaternion>();
        private readonly List<Transform> _cracks = new List<Transform>();
        private readonly List<Transform> _spores = new List<Transform>();
        private readonly List<Mesh> _vineMeshes = new List<Mesh>();
        private readonly List<Transform> _vineTips = new List<Transform>();
        private readonly List<GrowthTwigs> _branchTwigs = new List<GrowthTwigs>();
        private readonly List<Transform> _thorns = new List<Transform>();
        private readonly List<CharacterMotor> _targets = new List<CharacterMotor>();
        private readonly List<PaeteRope> _limbs = new List<PaeteRope>();
        private readonly List<float> _arrive = new List<float>();
        private readonly List<float> _released = new List<float>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<Vector3> _limb = new List<Vector3>();
        private float _facing, _lastAge = -99f;

        /// <summary>
        /// ⚠️ THE MODEL STANDS 1.75 TIMES ITS AUTHORED SIZE, about 9 m to the branch tips (owner,
        /// 2026-09-26: *"make tree bigger ITS weiurd that a tree that small is pulling everyone to it"*, then
        /// *"i want it to be REALLY big and imposing and really feel like an ult"*). 1.75 is the most the
        /// hold allows: at 1.9 m out a prisoner's shins meet the flared foot (about 1.7 m at shin height),
        /// so they are pressed into the roots; any bigger and the foot swallows them. Only the model is scaled, never this body: the limbs, shin branches and cracks are sized
        /// against the PLAYERS and must not grow with it. The prisoners, held 1.9 m out
        /// (`PaeteRules.SentryHoldDistance`), end up among its roots, pressed to the tree.
        /// </summary>
        public const float Scale = 1.75f;

        /// <summary>
        /// ⚠️⚠️ THE GUARDIAN'S OWN PALETTE: OLD WOOD, NOT HIS BODY'S (owner, 2026-09-26, of the tree seen from his screen
        /// in a match: *"it sucks"*, *"REFINE THIS TREE MORE"*). It wore Paete's roster palette, whose bark (8C6440,
        /// B08450, heartwood C29563) the plaza's sun lit to saturated orange: it read as plastic noodles. This is an
        /// ancient tree, older and darker than the carved guardian who calls it: deep browns a step apart so the weave
        /// still reads, darker mosses and leaves, and the same eye light as his (slot 10 and 11), so the two are
        /// visibly kin. Same sixteen slots as `tools/build_paete_voxel.py` (13 bark, 14 bark dark, 15 bark lit,
        /// 5 heartwood, 0/1/7 moss, 2/3 leaf, 4 vine, 6 root, 8 ink, 9 socket, 10/11 eye), direction.md 5.12.
        /// </summary>
        public static readonly Color[] Palette =
        {
            Hex(0x4E6E1E), Hex(0x34501A), Hex(0x6FA532), Hex(0x3F7424), Hex(0x4C6E20), Hex(0x6E4A2C), Hex(0x4A3320), Hex(0x6A8C2A),
            Hex(0x1E140C), Hex(0x140C06), Hex(0xD8FF6A), Hex(0x86C83A), Hex(0xE8C24A), Hex(0x5F4128), Hex(0x3A2616), Hex(0x7C5836),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        /// <summary>The guardian's bark, for every part built at runtime (ground branches, limbs, the prisoners' bands).</summary>
        public static Color Bark => Palette[13];
        public static Color BarkDark => Palette[14];
        public static Color BarkLit => Palette[15];
        public static Color Moss => Palette[0];
        public static Color Leaf => Palette[2];

        /// <summary>The authored model's height to its highest branch tip, measured off the glb (metres).</summary>
        public const float ModelHeight = 5.2f;
        private float _scale = Scale;

        /// <summary>
        /// ⚠️ FIT UNDER A ROOF. Ilalim ng Tulay is played under an elevated road, and at full size the crown
        /// went into the deck's underside (`PaeteReviewProbe` v17). The hazard measures the clearance above
        /// where the seed lands and the tree stands as tall as fits, never taller than `Scale`, never under
        /// 1.2 (it still has to tower over the prisoners).
        /// </summary>
        public void FitUnder(float clearance)
        {
            _scale = Mathf.Clamp((clearance - 0.3f) / ModelHeight, 1.2f, Scale);
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
        /// When (seconds of its age) the light opens in its hollows. 0.55 in play, right as it tops out; the cutscene
        /// holds it back so the guardian's eyes are the last beat, the one its camera ends on.
        /// </summary>
        public float WakeAt { get; set; } = 0.55f;

        // The eight ground branches racing out: yaw, length, wave phase. Each typed, none the same.
        private static readonly float[] VineYaw = { 8f, 52f, 93f, 141f, 183f, 226f, 268f, 317f };
        private static readonly float[] VineLength = { 3.0f, 2.5f, 3.3f, 2.7f, 3.1f, 2.3f, 3.4f, 2.8f };
        private static readonly float[] VinePhase = { 0.2f, 1.9f, 3.1f, 0.8f, 2.5f, 4.0f, 1.3f, 3.6f };
        private const int VineSamples = 24;
        // Where the thorns sit along a ground branch (hump tops) and where it crosses the court's surface.
        private static readonly float[] HumpThorn = { 0.20f, 0.60f, 0.92f };
        private static readonly float[] EntryAt = { 0.02f, 0.40f, 0.80f };
        private static readonly Vector3[] EntrySize = { new Vector3(0.22f, 0.10f, 0.16f), new Vector3(0.16f, 0.08f, 0.20f), new Vector3(0.19f, 0.09f, 0.14f),
                                                        new Vector3(0.14f, 0.07f, 0.17f), new Vector3(0.20f, 0.09f, 0.15f), new Vector3(0.15f, 0.08f, 0.13f) };
        private readonly List<Transform> _entries = new List<Transform>();
        private static Vector3 dir(int i) { float y = VineYaw[i] * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(y), 0f, Mathf.Cos(y)); }
        private static Vector3 SideOf(int i) { var d = dir(i); return new Vector3(d.z, 0f, -d.x); }
        // When each claw root slams down, in its own order round the tree (not a sweep).
        private static readonly float[] RootSlam = { 0.42f, 0.55f, 0.47f, 0.62f, 0.50f, 0.66f };
        // When each crown branch finishes unfurling, and how far it sways.
        private static readonly float[] ClawDelay = { 0.00f, 0.06f, 0.03f, 0.09f, 0.02f, 0.07f, 0.05f };
        private static readonly float[] ClawSway = { 3.0f, 2.2f, 3.6f, 2.6f, 3.2f, 2.0f, 2.8f };
        // Blinks, at the tree's own irregular times (one a double), and the order it looks round.
        private static readonly float[] Blinks = { 2.05f, 4.70f, 4.95f, 7.35f, 8.9f };
        private const float LookEvery = 2.6f;
        // Leaves falling from the crown during the watch: where each drops from (typed, cycled).
        private static readonly Vector3[] LeafFrom = { new Vector3(0.9f, 4.3f, 0.3f), new Vector3(-0.7f, 4.6f, -0.5f), new Vector3(0.2f, 4.1f, -1.0f),
                                                       new Vector3(-1.1f, 4.2f, 0.6f), new Vector3(0.6f, 4.8f, 0.8f) };

        public static PaeteSentryBody Build(Transform parent)
        {
            var go = new GameObject("PaeteSentryBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaeteSentryBody>();
            var root = go.transform;

            // The ground breaking: eight crack plates and the soil ring (the model rises through them).
            float[] crackLen = { 0.9f, 0.7f, 1.05f, 0.75f, 0.95f, 0.65f, 1.1f, 0.8f };
            for (int i = 0; i < crackLen.Length; i++)
            {
                var pivot = new GameObject("crack-" + i).transform;
                pivot.SetParent(root, false);
                pivot.localRotation = Quaternion.Euler(0f, i * 45f + 20f, 0f);
                var plate = GrowthVfx.Block(pivot, "plate", new Vector3(0.10f, 0.02f, crackLen[i]), BarkDark).transform;
                plate.localPosition = new Vector3(0f, 0.01f, 0.9f + crackLen[i] * 0.5f);
                b._cracks.Add(pivot);
            }
            // ⚠️ THE HEAVED SOIL IS ROUND (the 2026-09-26 ultimate film: a 2.8 m square of flat brown under the tree
            // read as a rug laid on the plaza). Three thin squares crossed at 0, 30 and 60 degrees make a
            // twelve-sided patch of dark turned earth, and a rim of clods, each placed by hand, sits where the
            // roots pushed the road up, so it reads as ground that broke rather than a tile.
            var soil = new GameObject("soil-ring").transform;
            soil.SetParent(root, false);
            var turned = Color.Lerp(GrowthVfx.Seed, Color.black, 0.3f);
            foreach (float yaw in new[] { 0f, 30f, 60f })
                GrowthVfx.Block(soil, "patch", new Vector3(2.5f, 0.04f, 2.5f), turned).transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            (Vector3 at, Vector3 size, float yaw, float tilt)[] clods =
            {
                (new Vector3(1.28f, 0.07f, 0.22f), new Vector3(0.42f, 0.16f, 0.30f), 12f, 14f),
                (new Vector3(0.78f, 0.06f, 1.02f), new Vector3(0.30f, 0.13f, 0.26f), -34f, 10f),
                (new Vector3(-0.18f, 0.08f, 1.30f), new Vector3(0.44f, 0.18f, 0.28f), 71f, 16f),
                (new Vector3(-1.02f, 0.06f, 0.78f), new Vector3(0.28f, 0.12f, 0.30f), 25f, 9f),
                (new Vector3(-1.34f, 0.07f, -0.12f), new Vector3(0.38f, 0.15f, 0.34f), -58f, 13f),
                (new Vector3(-0.82f, 0.05f, -1.06f), new Vector3(0.26f, 0.11f, 0.24f), 40f, 8f),
                (new Vector3(0.10f, 0.08f, -1.33f), new Vector3(0.46f, 0.17f, 0.30f), -15f, 15f),
                (new Vector3(0.96f, 0.06f, -0.92f), new Vector3(0.32f, 0.13f, 0.28f), 63f, 11f),
                (new Vector3(1.10f, 0.04f, -0.34f), new Vector3(0.18f, 0.08f, 0.16f), -80f, 6f),
                (new Vector3(-0.52f, 0.04f, 1.20f), new Vector3(0.17f, 0.08f, 0.15f), 5f, 7f),
            };
            foreach (var clod in clods)
            {
                var piece = GrowthVfx.Block(soil, "clod", clod.size, GrowthVfx.Seed).transform;
                piece.localPosition = clod.at;
                // Tilted outward, the way a slab of road lifts when something shoulders up under it.
                var outward = new Vector3(clod.at.x, 0f, clod.at.z).normalized;
                piece.localRotation = Quaternion.AngleAxis(clod.tilt, Vector3.Cross(Vector3.up, outward)) * Quaternion.Euler(0f, clod.yaw, 0f);
            }

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
            if (b._crown == null) { b._crown = new GameObject("crown").transform; b._crown.SetParent(b._trunk, false); b._crown.localPosition = Vector3.up * 3.22f; }

            // ⚠️ THE CROWN'S GLOWING GEM IS GONE (direction.md 5.12): two nested green cubes turned against each
            // other sat in the branches and read, from his screen, as a crystal stuck in a bare tree. The eyes in the
            // hollows are its only light. The empty node stays so the pose code keeps one shape.
            b._core = new GameObject("sentry-core").transform;
            b._core.SetParent(b._crown, false);

            // The soil heaved where each ground branch crosses the court: two clods per crossing, three crossings.
            for (int i = 0; i < VineYaw.Length * 3 * 2; i++)
                b._entries.Add(GrowthVfx.Block(root, "entry-clod", Vector3.one, i % 3 == 0 ? Palette[6] : GrowthVfx.Seed).transform);
            // The eight ground branches: a mesh each, rebuilt while they grow, three thorns and a tip leaf.
            for (int i = 0; i < VineYaw.Length; i++)
            {
                var mesh = new Mesh { name = "PaeteSentryGroundBranch" };
                mesh.MarkDynamic();
                PaeteInk.Part(root, "ground-branch-" + i, mesh, i % 3 == 1 ? BarkDark : i % 3 == 2 ? BarkLit : Bark);
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
            var bark = new[] { BarkLit, Bark, BarkDark };
            foreach (var t in targets)
            {
                _targets.Add(t);
                // ⚠️ Thicker than v1 (4 cm cords read as string at 10 m): a limb of three 6 to 7 cm cords.
                _limbs.Add(new PaeteRope(transform, "embrace-limb", bark, new[] { 0.072f, 0.064f, 0.056f }, 0.062f, 7.5f, _targets.Count * 1.7f));
                _arrive.Add(-1f);
                _released.Add(-1f);
            }
        }

        /// <summary>A ground branch's centreline at growth <paramref name="grow"/> (0 to 1), in local space.</summary>
        private void VinePath(int i, float grow, float age)
        {
            _points.Clear();
            float yaw = VineYaw[i] * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var side = new Vector3(dir.z, 0f, -dir.x);
            float len = VineLength[i] * grow;
            for (int k = 0; k <= VineSamples; k++)
            {
                float u = k / (float)VineSamples;
                float d = 2.2f + len * u;
                float wave = Mathf.Sin(u * 7.0f + VinePhase[i] + age * 1.6f) * 0.16f * u;
                // ⚠️⚠️ IT GOES INTO THE GROUND, NOT OFF IT (owner, 2026-09-26: *"make it look like the roots GO INT
                // he ground not float off of it"*). It used to lie along the court and curl its tip 0.55 m up into
                // the air. Now it WEAVES: two humps up out of the road and back under it (the parts below the court
                // are hidden by the court itself), then it dives in for good. It crosses the surface at u = 0, 0.4
                // and 0.8, which is where the soil heaves (`_entries`), and it strains a little as the prisoners fight.
                float hump = Mathf.Sin(u * Mathf.PI * 2.5f) * (0.26f - 0.06f * u) * grow;
                float dive = -0.45f * Mathf.SmoothStep(0.80f, 1.0f, u);
                _points.Add(dir * d + side * wave + Vector3.up * (hump + dive));
            }
        }

        private float LookYaw(float age)
        {
            // Before it wakes it faces along the throw; awake, it looks at one prisoner, then the next,
            // every 2.6 s, easing across in 0.7 s.
            if (_targets.Count == 0 || age < 0.7f) return _facing;
            float TargetYaw(int k)
            {
                var p = _targets[((k % _targets.Count) + _targets.Count) % _targets.Count];
                if (p == null) return _facing;
                var d = transform.InverseTransformPoint(p.transform.position); d.y = 0f;
                return d.sqrMagnitude > 1e-4f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : _facing;
            }
            float since = age - 0.7f;
            int seg = Mathf.FloorToInt(since / LookEvery);
            float into = since - seg * LookEvery;
            float from = seg == 0 ? _facing : TargetYaw(seg - 1);
            float to = TargetYaw(seg);
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(into / 0.7f));
            return Mathf.LerpAngle(from, to, ease);
        }

        public void Pose(float age, Vector3 centre)
        {
            bool landed = age >= 0f;
            for (int c = 0; c < transform.childCount; c++) transform.GetChild(c).gameObject.SetActive(landed);
            if (!landed) { foreach (var l in _limbs) l.Clear(); _lastAge = age; return; }

            float life = PaeteRules.SentryLifeSeconds;
            float wither = Mathf.Clamp01((age - (life - 0.4f)) / 0.9f);
            float alive = 1f - wither;

            // CRACK.
            for (int i = 0; i < _cracks.Count; i++)
                _cracks[i].localScale = new Vector3(1f, 1f, Mathf.Max(0.001f, Mathf.Clamp01((age - 0.01f * i) / 0.12f)));

            // ERUPT: up out of the road a quarter turn with overshoot; SQUASH on the stop; breathe; SLEEP
            // unscrews it back into the road.
            float erupt = GrowthVfx.Pop((age - 0.06f) / 0.50f);
            float land = age - 0.56f;
            float squash = land > 0f && land < 0.32f ? Mathf.Sin(land / 0.32f * Mathf.PI) : 0f;
            float breathe = age > 1f ? Mathf.Sin((age - 1f) * 2.2f) : 0f;
            float tall = 1f - 0.08f * squash + 0.015f * breathe * alive;
            float wide = 1f + 0.05f * squash + 0.008f * breathe * alive;
            _trunk.localScale = new Vector3(wide, tall, wide);
            _trunk.localPosition = Vector3.down * (5.4f * (1f - Mathf.Clamp(erupt, 0f, 1.08f)) + 1.8f * wither * wither);
            // The lean: at the wake it tips 6 degrees at whoever it looks at, settling to 3.
            float lean = (age < WakeAt ? 0f : 6f * GrowthVfx.Pop((age - WakeAt) / 0.3f) - 3f * Mathf.Clamp01((age - WakeAt - 0.65f) / 1.0f)) * alive;
            float yaw = LookYaw(age) - 95f * (1f - Mathf.Clamp01(erupt)) + 50f * wither;
            _trunk.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(lean, 0f, 0f);

            // GRIP: each claw root raised as it comes up, then slammed down past level and back.
            for (int i = 0; i < _roots.Count; i++)
            {
                float at = RootSlam[i % RootSlam.Length];
                float show = Mathf.Clamp01((age - (at - 0.18f)) / 0.14f);
                float slam = GrowthVfx.Pop((age - at) / 0.16f);
                float raised = -38f * (1f - slam) - 25f * wither;
                _roots[i].localRotation = _rootRest[i] * Quaternion.Euler(raised, 0f, 0f);
                _roots[i].localScale = _rootScale[i] * Mathf.Max(0.001f, show * (1f - Mathf.SmoothStep(0f, 1f, wither * 1.4f)));
                if (!Staged && _lastAge < at && age >= at && age - _lastAge < 0.5f)
                    PaeteGroundBreak.Spawn(_roots[i].TransformPoint(new Vector3(0f, 0f, 1.3f)), 0.32f);
            }

            // UNFURL, then the EMBRACE clench at the catch, a slow sway, and the droop as it sleeps.
            float catchAt = PaeteRules.SentryCatchSeconds;
            float clench = GrowthVfx.Envelope(age, catchAt, 0.12f, 1.2f, 0.45f);
            for (int i = 0; i < _claws.Count; i++)
            {
                float unfurl = GrowthVfx.Pop((age - 0.24f - ClawDelay[i % ClawDelay.Length]) / 0.36f);
                float sway = Mathf.Sin(age * 1.35f + i * 1.1f) * ClawSway[i % ClawSway.Length] * alive;
                float pitch = -48f * (1f - unfurl) - 14f * clench + sway + 38f * wither;
                _claws[i].localRotation = _clawRest[i] * Quaternion.Euler(pitch, 0f, sway * 0.5f);
            }

            // The core: swells in with the crown, flares at the catch, breathes, dims as it sleeps.
            float coreIn = GrowthVfx.Pop((age - 0.3f) / 0.3f);
            float coreBreath = 1f + 0.09f * Mathf.Sin(age * 3.8f) + 0.04f * Mathf.Sin(age * 9.1f) + 0.25f * GrowthVfx.Envelope(age, catchAt, 0.06f, catchAt + 0.4f, 0.3f);
            _core.localPosition = new Vector3(0f, 0.72f, 0f);
            _core.localRotation = Quaternion.Euler(0f, age * 35f, 0f);
            _core.localScale = Vector3.one * Mathf.Max(0.001f, 0.62f * coreIn * coreBreath * (1f - 0.8f * wither));
            for (int i = 0; i < _spores.Count; i++)
            {
                float a = age * (1.4f + 0.2f * i) + i * Mathf.PI * 0.5f;
                _spores[i].localPosition = new Vector3(Mathf.Cos(a) * 0.62f, 0.72f + Mathf.Sin(a * 1.7f) * 0.18f, Mathf.Sin(a) * 0.62f);
                _spores[i].localScale = Vector3.one * 0.07f * Mathf.Clamp01(coreIn) * alive;
            }

            // WAKE, BLINK, SLEEP: the light in the hollows, opened and shut on its own node.
            if (_eyes != null)
            {
                float open = age < WakeAt ? 0f : GrowthVfx.Pop((age - WakeAt) / 0.25f);
                float shut = 0f;
                foreach (float b in Blinks)
                {
                    float x = (age - b) / 0.09f;
                    if (x > 0f && x < 2f) shut = Mathf.Max(shut, 1f - Mathf.Abs(x - 1f));
                }
                float sleep = 1f - Mathf.Clamp01((age - (life - 0.45f)) / 0.4f);
                float lid = Mathf.Max(0.001f, open * (1f - shut) * sleep);
                _eyes.localScale = new Vector3(Mathf.Lerp(1f, 1.18f, Mathf.Clamp01(open - 1f) * 4f), lid, 1f);
            }

            // The ground branches race out, then writhe; they pull back into the road as it sleeps.
            for (int i = 0; i < _vineMeshes.Count; i++)
            {
                float grow = Mathf.Clamp01((age - 0.2f - 0.025f * i) / 0.42f);
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
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.20f, 0.04f, k / (float)VineSamples));
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
                        float sideOff = (c == 0 ? 1f : -1f) * (0.22f + 0.04f * e);
                        clod.localPosition = p0 + SideOf(i) * sideOff + Vector3.up * 0.03f;
                        clod.localRotation = Quaternion.Euler(10f * (c == 0 ? 1f : -1f), VineYaw[i] + 30f * e, 14f);
                        clod.localScale = EntrySize[(e * 2 + c) % EntrySize.Length] * show;
                    }
                }
            }

            // THE EMBRACE: a woven limb to each prisoner (direction.md section 5.8).
            for (int i = 0; i < _targets.Count; i++) PoseLimb(i, age, wither);

            // The crown tops out (0.56 s, the squash): leaves blown off it in a burst.
            if (!Staged && _lastAge < 0.56f && age >= 0.56f && age - _lastAge < 0.5f)
                PaeteLeafBurst.Spawn(transform.position + Vector3.up * 4.4f * _scale, 16, 2.6f);
            // A leaf falling from the crown every 0.8 s through the watch.
            if (!Staged && age > 1.2f && age < life - 0.5f && Mathf.FloorToInt(age / 0.8f) != Mathf.FloorToInt(_lastAge / 0.8f) && age - _lastAge < 0.5f)
            {
                int k = Mathf.FloorToInt(age / 0.8f) % LeafFrom.Length;
                PaeteLeafBurst.Spawn(transform.TransformPoint(LeafFrom[k] * _scale), 1, 0.35f);
            }

            // The pods: once, as it goes to sleep, the tree lets go of its seeds.
            if (!Staged && wither > 0.05f && !_podsDropped)
            {
                _podsDropped = true;
                PaeteLeafBurst.Spawn(transform.position + Vector3.up * 3.6f * _scale, 12, 1.8f);
            }
            _lastAge = age;
        }

        /// <summary>
        /// One prisoner's embrace: the limb leaves the trunk at 1.7 m on their side, arcs out and down to
        /// their waist and wraps it one and a half turns. It reaches out on the catch (following the body
        /// through the drag, which is what makes the drag read as the tree PULLING), holds while they are
        /// rooted, and on a break-out whips back into the trunk; on sleep it just draws back.
        /// </summary>
        private void PoseLimb(int i, float age, float wither)
        {
            var p = _targets[i];
            var rope = _limbs[i];
            float catchAt = PaeteRules.SentryCatchSeconds;
            if (p == null || age < catchAt) { rope.Clear(); return; }

            Vector3 body = transform.InverseTransformPoint(p.transform.position);
            Vector3 flat = new Vector3(body.x, 0f, body.z);
            if (_arrive[i] < 0f) _arrive[i] = PaeteRules.SentryPullArriveSeconds(flat.magnitude);
            bool held = wither < 0.5f && (age <= catchAt + _arrive[i] + 0.3f || p.IsRooted);
            if (!held && _released[i] < 0f && age > catchAt + 0.1f) _released[i] = age;
            if (held) _released[i] = -1f;

            float reach = Mathf.Clamp01((age - catchAt) / 0.22f);
            float wrap = Mathf.Clamp01((age - catchAt - 0.18f) / 0.35f);
            if (_released[i] >= 0f)
            {
                float back = Mathf.Clamp01((age - _released[i]) / 0.28f);
                reach *= 1f - back; wrap *= 1f - Mathf.Clamp01(back * 2f);
                if (back >= 1f) { rope.Clear(); return; }
            }

            Vector3 dir = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward;
            // The trunk's own space is inside the scaled model: where the limb leaves the weave, in this body's space.
            Vector3 anchor = transform.InverseTransformPoint(_trunk.TransformPoint(Quaternion.Inverse(_trunk.localRotation) * (dir * 0.50f) + Vector3.up * 1.70f));
            Vector3 waist = body + Vector3.up * 0.92f;
            if (p.IsStruggling) waist += new Vector3(Mathf.Sin(age * 36f) * 0.03f, 0f, Mathf.Cos(age * 29f) * 0.02f);
            Vector3 toTree = anchor - waist; toTree.y = 0f;
            float a0 = Mathf.Atan2(toTree.x, toTree.z);
            // Tight on the body (v1 hung a 0.33 m hoop round a 0.25 m waist): it grips.
            const float Loop = 0.28f;
            Vector3 wrapStart = waist + new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * Loop + Vector3.up * 0.12f;

            _limb.Clear();
            // The limb: out of the trunk, up and over, down onto the waist (a cubic, eleven samples).
            Vector3 c1 = anchor + dir * 0.45f + Vector3.up * 0.30f, c2 = wrapStart + Vector3.up * 0.35f - dir * 0.10f;
            int limbSamples = Mathf.Max(2, Mathf.RoundToInt(11 * reach));
            for (int k = 0; k < limbSamples; k++)
            {
                float t = k / 10f;
                float u = 1f - t;
                _limb.Add(u * u * u * anchor + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * wrapStart);
            }
            // The wrap: one and a half turns round the waist, spiralling down a little.
            if (reach >= 1f)
            {
                // Two full turns, cinching: the loop draws in 3 cm after it closes and breathes with the tree.
                int wrapSamples = Mathf.RoundToInt(20 * wrap);
                float cinch = 1f - 0.10f * Mathf.Clamp01((age - catchAt - 0.55f) / 0.25f) - 0.03f * Mathf.Sin(age * 2.4f);
                for (int k = 1; k <= wrapSamples; k++)
                {
                    float f = k / 20f;
                    float ang = a0 + f * Mathf.PI * 4f;
                    float r = (Loop + 0.015f * Mathf.Sin(k * 1.7f)) * cinch;
                    _limb.Add(waist + new Vector3(Mathf.Sin(ang) * r, 0.16f - 0.30f * f, Mathf.Cos(ang) * r));
                }
            }
            rope.Draw(_limb, 0.45f);
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
            Hex(0x93B540), Hex(0x5B7F2C), Hex(0x4F8B2F), Hex(0x3A6B24), Hex(0x6F9B35), Hex(0xC29563), Hex(0xA8946A), Hex(0x5E7F24),
            Hex(0x1E140C), Hex(0x2B1512), Hex(0x8E2435), Hex(0x9A3243), Hex(0xB04A55), Hex(0x8A6240), Hex(0x3F5A1A), Hex(0x3F5A1A),
        };

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
            for (int i = 0; i < 4; i++) { var t = PaeteProp.Find(b._model, "root-" + i); if (t != null) { b._roots.Add(t); b._rootRest.Add(t.localRotation); } }
            for (int i = 0; i < 2; i++) { var t = PaeteProp.Find(b._model, "arm-" + i); if (t != null) { b._arms.Add(t); b._armRest.Add(t.localRotation); } }
            return b;
        }

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
                foreach (int slot in new[] { 0, 1, 2, 3, 4, 7 }) p[slot] = Color.Lerp(Palette[slot], straw, k);
                _dryPalettes[step] = p;
            }
            return _dryPalettes[step];
        }

        public void Pose(float age, float loosen, bool pullable, float shotGrowth, float sinceShot)
        {
            bool landed = age >= 0f;
            _root.gameObject.SetActive(landed);
            if (!landed) return;
            // ⚠️ IT RISES OUT OF THE SOIL, IT DOES NOT SCALE IN (owner, 2026-09-26).
            float rise = GrowthVfx.Pop(age / 0.45f);
            float squash = age > 0.35f && age < 0.6f ? 1f + 0.18f * Mathf.Sin((age - 0.35f) / 0.25f * Mathf.PI) : 1f;
            // The spit's anticipation: the neck compresses before the snap.
            float coil = sinceShot < 0.12f ? sinceShot / 0.12f : sinceShot < 0.3f ? 1f - (sinceShot - 0.12f) / 0.18f : 0f;
            squash *= 1f + 0.10f * coil;
            _stem.localScale = new Vector3(squash, 1f / squash, squash);
            _stem.localPosition = Vector3.down * (1.0f * (1f - rise));
            float sway = Mathf.Sin(age * 1.2f) * 2.5f, swayZ = Mathf.Sin(age * 0.85f + 1f) * 2.5f;
            _stem.localRotation = _stemRest * Quaternion.Euler(sway + 16f * loosen, 0f, swayZ + 6f * loosen);
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
            float spit = sinceShot < 0.12f ? -34f * (sinceShot / 0.12f)
                       : sinceShot < 0.22f ? Mathf.Lerp(-34f, 28f, (sinceShot - 0.12f) / 0.10f)
                       : sinceShot < 0.55f ? Mathf.Lerp(28f, 0f, (sinceShot - 0.22f) / 0.33f) * Mathf.Cos((sinceShot - 0.22f) * 28f) : 0f;
            float grown = Mathf.Clamp01(shotGrowth);
            bool ready = grown >= 1f && sinceShot > 0.6f;
            float proud = ready ? Mathf.Abs(Mathf.Sin(age * 3.2f)) * 5f : 0f;
            float follow = Mathf.Sin(age * 1.2f - 0.7f) * 4f;
            _pod.localRotation = _podRest * Quaternion.Euler(spit + 38f * loosen + follow - proud, 0f, 12f * loosen);
            _pod.localPosition = _podAt + Vector3.up * (0.03f * proud / 5f);

            // The lid: ajar while the clog grows, lifting with it, open when it is ready; it SLAPS shut on the
            // spit and then lifts again as the next one grows; limp half-shut when the plant loosens. Its rest
            // lies over the mouth, and a NEGATIVE pitch lifts it (its +Z runs forward from the hinge).
            if (_lid != null)
            {
                // ⚠️ Opens to 84 degrees at READY, not 58: the v22 film had the rising bakya cutting through the lid.
                float open = -12f - 72f * grown * grown;
                float slap = GrowthVfx.Envelope(sinceShot, 0.12f, 0.04f, 0.5f, 0.4f);
                open = Mathf.Lerp(open, -4f, slap);
                open = Mathf.Lerp(open, -30f, loosen);
                _lid.localRotation = _lidRest * Quaternion.Euler(open, 0f, 0f);
            }

            // The bakya grows inside and rises until its toe hangs over the lip.
            if (_shoe != null)
            {
                float up = GrowthVfx.Pop(Mathf.Clamp01((grown - 0.35f) / 0.65f));
                _shoe.localScale = _shoeScale * Mathf.Max(0.001f, Mathf.Clamp01(grown * 1.4f));
                // ⚠️ Rises further than the first pass (0.075 m): at READY its toe must clear the thicker lip.
                _shoe.localPosition = _shoeAt + new Vector3(0f, 0.11f, 0.10f) * up;
                _shoe.localRotation = _shoeRest * Quaternion.Euler(-28f * up, 0f, 0f);
                _shoe.gameObject.SetActive(grown > 0.02f && sinceShot > 0.13f);
            }

            // The arm leaves: flick open after the pop, breathe, flare back at the command, droop dry.
            for (int i = 0; i < _arms.Count; i++)
            {
                float flick = GrowthVfx.Pop((age - 0.30f - 0.06f * i) / 0.30f);
                float breathe = Mathf.Sin(age * 1.6f + i * 1.9f) * 4f;
                float armFlare = GrowthVfx.Envelope(sinceShot, 0f, 0.08f, 0.45f, 0.3f);
                float pitch = -60f * (1f - flick) + breathe - 24f * armFlare + 26f * loosen;
                _arms[i].localRotation = _armRest[i] * Quaternion.Euler(pitch, 0f, 0f);
            }

            // The greens dry in four steps (a palette re-dress, cached per step by `ToonSkin`).
            int step = Mathf.Clamp(Mathf.FloorToInt(loosen * 4f), 0, 3);
            if (step != _dryStep && _model != null)
            {
                _dryStep = step;
                PaeteProp.Redress(_model, DryPalette(step));
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
            _root.localRotation = Quaternion.AngleAxis(-70f * fling, Vector3.Cross(Vector3.up, dir));
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
            Hex(0x1E140C), Hex(0x130E09), Hex(0xC9BC98), Hex(0x6B4A2E), Hex(0xEAD49C), Hex(0x8C6440), Hex(0x553A22), Hex(0xB08450),
        };

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
            }
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
                float shiver = Mathf.Sin(age * 60f + i * 2.3f) * Quiver[i] * quiver;
                float pitch = fold + shiver - 18f * whipBack - 58f * clench + 48f * sink;
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
                if (shoe == null || age > hold + yank + 0.25f || _whip[stem] == null) { _lashes[k].Clear(); continue; }
                _whip[stem].gameObject.SetActive(false);
                Vector3 from = transform.InverseTransformPoint(_whip[stem].position);
                Vector3 to = transform.InverseTransformPoint(shoe.transform.position);
                Vector3 tip = Vector3.Lerp(from, to, GrowthVfx.Pop(Mathf.Clamp01(age / 0.18f)));
                float slack = age < hold ? 0.03f : 0f;
                GrowthVfx.Curve(_points, from, tip, 12, slack * Vector3.Distance(from, tip), 0.05f, k * 1.3f + age * 6f);
                _lashes[k].Draw(_points, 0.55f);
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
            }
        }
    }
}
