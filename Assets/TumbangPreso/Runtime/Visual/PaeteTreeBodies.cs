using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ YAKAP NG MAKILING'S SENTRY, v5: THE MODELLED TREE (direction.md section 5.2 and 5.8).
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
        private bool _podsDropped;

        // The eight ground branches racing out: yaw, length, wave phase. Each typed, none the same.
        private static readonly float[] VineYaw = { 8f, 52f, 93f, 141f, 183f, 226f, 268f, 317f };
        private static readonly float[] VineLength = { 3.0f, 2.5f, 3.3f, 2.7f, 3.1f, 2.3f, 3.4f, 2.8f };
        private static readonly float[] VinePhase = { 0.2f, 1.9f, 3.1f, 0.8f, 2.5f, 4.0f, 1.3f, 3.6f };
        private const int VineSamples = 16;
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
                var plate = GrowthVfx.Block(pivot, "plate", new Vector3(0.10f, 0.02f, crackLen[i]), GrowthVfx.BarkDark).transform;
                plate.localPosition = new Vector3(0f, 0.01f, 0.9f + crackLen[i] * 0.5f);
                b._cracks.Add(pivot);
            }
            GrowthVfx.Block(root, "soil-ring", new Vector3(2.8f, 0.04f, 2.8f), GrowthVfx.Seed).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            b._model = PaeteProp.Spawn("sentry", root);
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

            // The core: the seed of light held in the crown, the only light besides the eyes. Two nested
            // glowing blocks turned against each other, and four spores circling it.
            b._core = new GameObject("sentry-core").transform;
            b._core.SetParent(b._crown, false);
            GrowthVfx.Block(b._core, "core-shell", Vector3.one, GrowthVfx.LeafGreen, 0.9f).transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            GrowthVfx.Block(b._core, "core-heart", Vector3.one * 0.80f, GrowthVfx.Glow, 1.5f).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            for (int i = 0; i < 4; i++)
                b._spores.Add(GrowthVfx.Block(b._crown, "spore-" + i, Vector3.one * 0.09f, GrowthVfx.Glow, 1.2f).transform);

            // The eight ground branches: a mesh each, rebuilt while they grow, three thorns and a tip leaf.
            for (int i = 0; i < VineYaw.Length; i++)
            {
                var mesh = new Mesh { name = "PaeteSentryGroundBranch" };
                mesh.MarkDynamic();
                PaeteInk.Part(root, "ground-branch-" + i, mesh, i % 3 == 1 ? GrowthVfx.BarkDark : i % 3 == 2 ? GrowthVfx.BarkLit : GrowthVfx.Bark);
                b._vineMeshes.Add(mesh);
                b._branchTwigs.Add(new GrowthTwigs(root, new[] { 0.28f + 0.03f * (i % 3), 0.52f, 0.74f - 0.04f * (i % 2) },
                                                   new[] { 40f + 25f * i, 200f - 15f * i, 310f + 10f * i }, new[] { 1.4f, 1.2f, 1.0f }, GrowthVfx.BarkLit));
                for (int k = 0; k < 3; k++)
                {
                    var thornMesh = new Mesh { name = "PaeteSentryThorn" };
                    PaeteInk.Tube(thornMesh, new List<Vector3> { Vector3.zero, new Vector3(0, 0.12f, 0.03f), new Vector3(0, 0.24f, 0.08f) },
                                  new List<float> { 0.05f, 0.028f, 0.003f }, 4);
                    b._thorns.Add(PaeteInk.Part(root, "thorn", thornMesh, GrowthVfx.BarkDark).transform);
                }
                b._vineTips.Add(PaeteInk.Part(root, "ground-branch-leaf", PaeteInk.Leaf(0.24f, 0.14f, 0.02f), GrowthVfx.LeafGreen).transform);
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
            var bark = new[] { GrowthVfx.BarkLit, GrowthVfx.Bark, GrowthVfx.BarkDark };
            foreach (var t in targets)
            {
                _targets.Add(t);
                _limbs.Add(new PaeteRope(transform, "embrace-limb", bark, new[] { 0.042f, 0.038f, 0.034f }, 0.040f, 7.5f, _targets.Count * 1.7f));
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
                float lift = 0.05f + Mathf.Pow(u, 6f) * 0.55f * grow;
                _points.Add(dir * d + side * wave + Vector3.up * lift);
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
            float lean = (age < 0.55f ? 0f : 6f * GrowthVfx.Pop((age - 0.55f) / 0.3f) - 3f * Mathf.Clamp01((age - 1.2f) / 1.0f)) * alive;
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
                if (_lastAge < at && age >= at && age - _lastAge < 0.5f)
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
                float open = age < 0.55f ? 0f : GrowthVfx.Pop((age - 0.55f) / 0.25f);
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
                    int at = Mathf.Clamp(Mathf.RoundToInt((0.3f + 0.22f * k) * VineSamples), 1, VineSamples - 1);
                    var thorn = _thorns[i * 3 + k];
                    Vector3 along = (_points[at + 1] - _points[at - 1]).normalized;
                    thorn.localPosition = _points[at] + Vector3.up * 0.13f;
                    thorn.localRotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(-10f, 0f, 0f);
                    thorn.localScale = Vector3.one * 1.5f * Mathf.Clamp01((grow - (0.3f + 0.22f * k)) * 5f);
                }
                var tip = _vineTips[i];
                Vector3 last = _points[_points.Count - 1], before = _points[_points.Count - 2];
                tip.localPosition = last;
                tip.localRotation = Quaternion.LookRotation((last - before).normalized, Vector3.up);
                tip.localScale = Vector3.one * 1.6f * grow;
            }

            // THE EMBRACE: a woven limb to each prisoner (direction.md section 5.8).
            for (int i = 0; i < _targets.Count; i++) PoseLimb(i, age, wither);

            // The crown tops out (0.56 s, the squash): leaves blown off it in a burst.
            if (_lastAge < 0.56f && age >= 0.56f && age - _lastAge < 0.5f)
                PaeteLeafBurst.Spawn(transform.position + Vector3.up * 4.4f * Scale, 16, 2.6f);
            // A leaf falling from the crown every 0.8 s through the watch.
            if (age > 1.2f && age < life - 0.5f && Mathf.FloorToInt(age / 0.8f) != Mathf.FloorToInt(_lastAge / 0.8f) && age - _lastAge < 0.5f)
            {
                int k = Mathf.FloorToInt(age / 0.8f) % LeafFrom.Length;
                PaeteLeafBurst.Spawn(transform.TransformPoint(LeafFrom[k] * Scale), 1, 0.35f);
            }

            // The pods: once, as it goes to sleep, the tree lets go of its seeds.
            if (wither > 0.05f && !_podsDropped)
            {
                _podsDropped = true;
                PaeteLeafBurst.Spawn(transform.position + Vector3.up * 3.6f * Scale, 12, 1.8f);
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
            const float Loop = 0.33f;
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
                int wrapSamples = Mathf.RoundToInt(14 * wrap);
                for (int k = 1; k <= wrapSamples; k++)
                {
                    float f = k / 14f;
                    float ang = a0 + f * Mathf.PI * 3f;
                    float r = Loop + 0.02f * Mathf.Sin(k * 1.7f);
                    _limb.Add(waist + new Vector3(Mathf.Sin(ang) * r, 0.12f - 0.22f * f, Mathf.Cos(ang) * r));
                }
            }
            rope.Draw(_limb, 0.45f);
        }
    }

    /// <summary>
    /// ⚠️⚠️ PUNLANG TSINELAS'S SEEDLING, v3: THE MODELLED SPROUT (direction.md section 5.3). A three-cord
    /// stem, two big ARM leaves, four roots, and a POD for a head: a bud of five petals round the wooden
    /// slipper that grows in it. Posed from its age by `PaetePlant`:
    ///  * pop (0 to 0.45 s): pushes up out of the soil, overshoot, squash; the arm leaves flick open last;
    ///  * alive: a slow sway, the pod bobbing a beat behind the stem, the arm leaves breathing;
    ///  * ripening: the petals part as the slipper grows; ready, the pod gives a proud little bob;
    ///  * fire: the arm leaves flare, the pod pulls back 0.12 s and SNAPS forward past rest, the petals
    ///    flare, then a wobble to rest;
    ///  * loosening (15 s on): the stem sags, the pod droops, the leaves dry toward straw (his palette
    ///    re-dressed in four steps), the roots lift, loose soil breathes round it;
    ///  * pulled: up, roots tearing free, flung toward the puller, gone.
    /// </summary>
    public sealed class PaetePlantBody : MonoBehaviour
    {
        private GameObject _model;
        private Transform _root, _stem, _pod, _shoe, _soil;
        private readonly List<Transform> _petals = new List<Transform>(), _roots = new List<Transform>(), _arms = new List<Transform>();
        private readonly List<Quaternion> _petalRest = new List<Quaternion>(), _rootRest = new List<Quaternion>(), _armRest = new List<Quaternion>();
        private Quaternion _podRest;
        private Vector3 _podAt, _shoeScale;
        private int _dryStep = -1;
        private static Color[][] _dryPalettes;
        private static readonly float[] PetalPhase = { 0.0f, 1.3f, 2.9f, 4.1f, 5.4f };

        public static PaetePlantBody Build(Transform parent)
        {
            var go = new GameObject("PaetePlantBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaetePlantBody>();
            b._root = go.transform;
            b._soil = GrowthVfx.Block(b._root, "loose-soil", new Vector3(0.9f, 0.03f, 0.9f), GrowthVfx.Seed).transform;
            b._soil.gameObject.SetActive(false);
            b._model = PaeteProp.Spawn("seedling", b._root);
            b._stem = PaeteProp.Find(b._model, "stem") ?? new GameObject("stem").transform;
            if (b._stem.parent == null) b._stem.SetParent(b._root, false);
            b._pod = PaeteProp.Find(b._model, "pod") ?? b._stem;
            b._podRest = b._pod.localRotation; b._podAt = b._pod.localPosition;
            b._shoe = PaeteProp.Find(b._model, "slipper");
            if (b._shoe != null) b._shoeScale = b._shoe.localScale;
            for (int i = 0; i < 5; i++) { var t = PaeteProp.Find(b._model, "petal-" + i); if (t != null) { b._petals.Add(t); b._petalRest.Add(t.localRotation); } }
            for (int i = 0; i < 4; i++) { var t = PaeteProp.Find(b._model, "root-" + i); if (t != null) { b._roots.Add(t); b._rootRest.Add(t.localRotation); } }
            for (int i = 0; i < 2; i++) { var t = PaeteProp.Find(b._model, "arm-" + i); if (t != null) { b._arms.Add(t); b._armRest.Add(t.localRotation); } }
            return b;
        }

        /// <summary>His palette with the leaf, moss and vine slots dried toward straw, in four steps (cached).</summary>
        private static Color[] DryPalette(int step)
        {
            var baseline = PaeteProp.Palette;
            if (baseline == null) return null;
            if (_dryPalettes == null) _dryPalettes = new Color[4][];
            if (_dryPalettes[step] == null)
            {
                var p = (Color[])baseline.Clone();
                // ⚠️ STRAW-BROWN, NOT `GrowthVfx.Dry`'S YELLOW: at full strength the loosened plant filmed
                // (v14) as a bright yellow flower rather than a drying one.
                float k = step / 3f * 0.75f;
                var straw = new Color(0.55f, 0.47f, 0.27f);
                foreach (int slot in new[] { 0, 1, 2, 3, 4, 7 }) p[slot] = Color.Lerp(baseline[slot], straw, k);
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
            // The fire's anticipation: the stem compresses before the snap.
            float coil = sinceShot < 0.12f ? sinceShot / 0.12f : sinceShot < 0.3f ? 1f - (sinceShot - 0.12f) / 0.18f : 0f;
            squash *= 1f + 0.10f * coil;
            _stem.localScale = new Vector3(squash, 1f / squash, squash);
            _stem.localPosition = Vector3.down * (0.95f * (1f - rise));
            float sway = Mathf.Sin(age * 1.2f) * 2.5f, swayZ = Mathf.Sin(age * 0.85f + 1f) * 2.5f;
            _stem.localRotation = Quaternion.Euler(sway + 14f * loosen, 0f, swayZ + 6f * loosen);
            float pop = Mathf.Clamp01((age - 0.15f) / 0.4f);

            // Loosening: roots lift out, the soil ring shows and breathes.
            for (int i = 0; i < _roots.Count; i++)
            {
                _roots[i].localRotation = _rootRest[i] * Quaternion.Euler(-28f * loosen, 0f, 0f);
                _roots[i].localPosition = new Vector3(0f, 0.12f * loosen, 0f);
                _roots[i].localScale = Vector3.one * Mathf.Max(0.001f, pop);
            }
            _soil.gameObject.SetActive(pullable);
            if (pullable)
            {
                float pulse = 1f + 0.06f * Mathf.Sin(age * 5.0f);
                _soil.localScale = new Vector3(0.9f * pulse, 0.03f, 0.9f * pulse);
            }

            // The pod: a beat behind the stem (follow-through), the recoil, the proud bob when ready, the droop.
            float recoil = sinceShot < 0.12f ? -28f * (sinceShot / 0.12f)
                         : sinceShot < 0.22f ? Mathf.Lerp(-28f, 22f, (sinceShot - 0.12f) / 0.10f)
                         : sinceShot < 0.5f ? Mathf.Lerp(22f, 0f, (sinceShot - 0.22f) / 0.28f) * Mathf.Cos((sinceShot - 0.22f) * 30f) : 0f;
            float grown = Mathf.Clamp01(shotGrowth);
            float proud = grown >= 1f && sinceShot > 0.6f ? Mathf.Abs(Mathf.Sin(age * 3.2f)) * 5f : 0f;
            float follow = Mathf.Sin(age * 1.2f - 0.7f) * 4f;
            _pod.localRotation = _podRest * Quaternion.Euler(recoil + 30f * loosen + follow - proud, 0f, 12f * loosen);
            _pod.localPosition = _podAt + Vector3.up * (0.03f * proud / 5f);

            // The petals part as the slipper ripens; they flare on the shot; they droop as it loosens.
            float flare = GrowthVfx.Envelope(sinceShot, 0.10f, 0.05f, 0.5f, 0.3f);
            for (int i = 0; i < _petals.Count; i++)
            {
                float open = 40f * grown + 22f * flare + 25f * loosen + 3f * Mathf.Sin(age * 1.8f + PetalPhase[i]);
                _petals[i].localRotation = _petalRest[i] * Quaternion.Euler(open, 0f, 0f);
            }
            if (_shoe != null) _shoe.localScale = _shoeScale * Mathf.Max(0.001f, grown);

            // The arm leaves: flick open after the pop, breathe, flare back at the command, droop dry.
            for (int i = 0; i < _arms.Count; i++)
            {
                float flick = GrowthVfx.Pop((age - 0.30f - 0.06f * i) / 0.30f);
                float breathe = Mathf.Sin(age * 1.6f + i * 1.9f) * 4f;
                float armFlare = GrowthVfx.Envelope(sinceShot, 0f, 0.08f, 0.45f, 0.3f);
                float pitch = -70f * (1f - flick) + breathe - 30f * armFlare + 30f * loosen;
                _arms[i].localRotation = _armRest[i] * Quaternion.Euler(pitch, 0f, 0f);
            }

            // The leaves dry in four steps (a palette re-dress, cached per step by `ToonSkin`).
            int step = Mathf.Clamp(Mathf.FloorToInt(loosen * 4f), 0, 3);
            if (step != _dryStep && _model != null)
            {
                _dryStep = step;
                var palette = DryPalette(step);
                if (palette != null) PaeteProp.Redress(_model, palette);
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
    /// ⚠️⚠️ BAWI'S THORN CONSTRUCT, v2: THE MODELLED FIST (direction.md section 5.4). A knot of three
    /// roots and seven square thorns; a woven lash out to each caught slipper that goes taut, holds a
    /// beat and hauls it home. Thorns punch up in a ripple round the ring, QUIVER through the hold, WHIP
    /// back on the yank, then CLENCH inward like a fist closing on what it took, and sink one by one.
    /// </summary>
    public sealed class PaeteThornBody : MonoBehaviour
    {
        private GameObject _model;
        private Transform _knot;
        private Vector3 _knotAt;
        private readonly List<Transform> _spikes = new List<Transform>();
        private readonly List<Vector3> _spikeRest = new List<Vector3>();
        private readonly List<Quaternion> _spikeTurn = new List<Quaternion>();
        private readonly List<Slipper> _targets = new List<Slipper>();
        private readonly List<PaeteRope> _lashes = new List<PaeteRope>();
        private readonly List<Vector3> _points = new List<Vector3>();
        // Each thorn's own beats: when it punches up, and when it sinks at the end.
        private static readonly float[] RiseAt = { 0.00f, 0.035f, 0.07f, 0.02f, 0.10f, 0.055f, 0.085f };
        private static readonly float[] SinkAt = { 2.45f, 2.62f, 2.52f, 2.70f, 2.40f, 2.58f, 2.66f };
        private static readonly float[] Quiver = { 5f, 4f, 6f, 3.5f, 5.5f, 4.5f, 3f };

        public static PaeteThornBody Build(Transform parent, List<Slipper> targets)
        {
            var go = new GameObject("PaeteThornBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaeteThornBody>();
            b._model = PaeteProp.Spawn("thorns", go.transform);
            b._knot = PaeteProp.Find(b._model, "knot") ?? new GameObject("knot").transform;
            if (b._knot.parent == null) b._knot.SetParent(go.transform, false);
            b._knotAt = b._knot.localPosition;
            for (int i = 0; i < 7; i++)
            {
                var t = PaeteProp.Find(b._model, "thorn-" + i);
                if (t == null) continue;
                b._spikes.Add(t); b._spikeRest.Add(t.localPosition); b._spikeTurn.Add(t.localRotation);
            }
            var bark = new[] { GrowthVfx.BarkDark, GrowthVfx.Bark };
            foreach (var shoe in targets)
            {
                b._targets.Add(shoe);
                b._lashes.Add(new PaeteRope(go.transform, "thorn-lash", bark, new[] { 0.042f, 0.036f }, 0.034f, 9f, b._targets.Count * 1.3f));
            }
            return b;
        }

        public void Pose(float age, Vector3 origin)
        {
            float hold = PaeteRules.ThornHoldSeconds, yank = PaeteRules.ThornYankSeconds;
            float quiver = GrowthVfx.Envelope(age, 0.18f, 0.04f, hold + 0.05f, 0.06f);
            float whip = GrowthVfx.Envelope(age, hold, 0.08f, hold + yank, 0.2f);
            float clench = GrowthVfx.Envelope(age, hold + yank - 0.05f, 0.25f, 2.4f, 0.3f);
            for (int i = 0; i < _spikes.Count; i++)
            {
                float up = GrowthVfx.Pop((age - RiseAt[i]) / 0.2f);
                float sink = Mathf.Clamp01((age - SinkAt[i]) / 0.35f);
                _spikes[i].localPosition = _spikeRest[i] + Vector3.down * (0.9f * (1f - up) + 0.9f * sink * sink);
                _spikes[i].localScale = Vector3.one * (up > 0.001f && sink < 1f ? 1f : 0.001f);
                float shiver = Mathf.Sin(age * 70f + i * 2.1f) * Quiver[i] * quiver;
                float pitch = shiver - 20f * whip - 32f * clench;
                _spikes[i].localRotation = _spikeTurn[i] * Quaternion.Euler(pitch, 0f, 0f);
            }
            float grow = GrowthVfx.Pop(age / 0.2f);
            float gone = Mathf.Clamp01((age - 2.55f) / 0.4f);
            _knot.localPosition = _knotAt + Vector3.down * (0.45f * (1f - Mathf.Clamp01(grow)) + 0.5f * gone);
            _knot.localScale = Vector3.one * Mathf.Max(0.001f, Mathf.Clamp01(grow) * (1f - gone * 0.6f));

            // The lashes: out to each slipper, taut through the hold, hauling it home through the yank.
            for (int i = 0; i < _targets.Count; i++)
            {
                var shoe = _targets[i];
                if (shoe == null || age > hold + yank + 0.25f) { _lashes[i].Clear(); continue; }
                Vector3 from = transform.InverseTransformPoint(origin) + Vector3.up * 0.3f;
                Vector3 to = transform.InverseTransformPoint(shoe.transform.position);
                Vector3 tip = Vector3.Lerp(from, to, GrowthVfx.Pop(Mathf.Clamp01(age / 0.18f)));
                float slack = age < hold ? 0.02f : 0f;
                GrowthVfx.Curve(_points, from, tip, 12, slack * Vector3.Distance(from, tip), 0.04f, i * 1.3f + age * 8f);
                _lashes[i].Draw(_points, 0.4f);
            }
        }
    }
}
