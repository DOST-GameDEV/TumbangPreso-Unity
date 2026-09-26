using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE: THE TAKE (v7, 2026-09-27), 3.8 to 5.0 s. The owner on the v6 film, of its last shot (the tree
        // waking alone on an empty court): *"this felt liek a weak ending to his ult haha maybe change angle or smth and show everyone
        // getting pulled? and animate too that theyre all shocked or trying to get out"*, *"try to follwo vines going to ppl with
        // camera as well idk ahah figure it out"*, *"weak ending to his ult cutscene is what i meant"*.
        //
        // What was weak: the cutscene ended on the guardian's eyes, and the thing the ultimate DOES (it takes everyone in 9 m) happened
        // only after the hand-back, in play, from wherever the player's camera happened to be. The payoff was off screen. So the
        // sentence gets its last clause on screen: her light, his ground, the mountain's answer, AND WHAT IT ANSWERS WITH.
        //
        //   3.71        its eyes open (the RISE's last beat); every player it will catch flinches and turns to it
        //   3.80        CUT. ONE CONTINUOUS MOVE to the end. Its limbs lash out of the trunk at everyone at once; the camera rides the
        //               longest one out from the bark to the player on its end (the owner's "follow vines going to ppl")
        //   ~4.06       it reaches them: shocked, arms flung up, it wraps their waist twice and cinches
        //   4.18        THE YANK, everyone together, on his own haul (he drives his shoulders up and back, pulling with the tree): they
        //               are dragged in feet first, leaning back against it, and spun round by the wrap so they land facing out
        //   4.18 to 4.62 the camera rises and swings wide round the tree as they fly in
        //   4.50        THE THUD: all of them hit the trunk; bands whip up their shins; a ring races out over the court, the crown
        //               bursts, light runs down every limb to every waist
        //   4.62 to 5.0 the settle: a low wide on the guardian's face with every prisoner bound round it, all of them STRUGGLING
        //               (the struggle loop at its hard speed), and the hand-back
        //
        // ⚠️⚠️ THE PLAYERS ARE THE REAL ONES, NOT EXTRAS. The phase hides every live body while it draws (`UltimatePhaseView.Draw`),
        // so these are copies of exactly the players `PaeteSentry` will catch: the same rule on the same accepted cast
        // (`PaeteVine.SentryTarget`, `PaeteSentry.InReach`), their own rigs, skins and outfits (`MatchPoseHistory.Track.Clone`, which
        // copies render data only), animated with the clips their own rigs carry for his kit (`RootedMotion`: the breakout's flung
        // arms for the shock, the heave for the drag, the struggle for the hold). They stand where they stand relative to the spot,
        // brought inside 2.6 to 8.8 m so the pull reads, and are held at `PaeteRules.SentryHoldDistance`, the same 1.4 m as in play.
        // Nobody in reach: the limbs whip into the court at three typed spots and the camera rides the first; nobody is invented.
        // ⚠️ Nothing here changes a rule, a number or a byte on the wire: in play the catch is still `PaeteSentry`'s, from the same spot.
        // ⚠️ Posed from the scene clock only (the world is paused). No `Random`: every offset is typed.
        // =========================================================================================

        private const float PtLashAt = 3.80f, PtYankAt = 4.18f, PtArriveAt = 4.50f;
        // How far the limbs reach per second as they lash out (a 6 m reach takes 0.23 s), and the bounds on it.
        private const float PtLashSpeed = 26f, PtReachMin = .16f, PtReachMax = .30f;
        // The stagger between prisoners, so seven limbs are seven lashes and not one.
        private static readonly float[] PtStagger = { 0f, .035f, .015f, .05f, .025f, .06f, .045f };
        // With nobody in reach the limbs strike the court here instead: angle off the guardian's facing, distance out.
        private static readonly Vector2[] PtStrikeRows = { new Vector2(-38f, 4.6f), new Vector2(64f, 3.9f), new Vector2(172f, 5.2f) };
        // The leaves torn off as a limb cinches a waist: direction (degrees round them), out speed, up speed, size.
        private static readonly Vector4[] PtWrapLeafRows =
        {
            new Vector4(20f, 1.8f, 1.6f, .15f), new Vector4(95f, 2.3f, 1.1f, .13f), new Vector4(170f, 1.6f, 2.0f, .16f),
            new Vector4(245f, 2.1f, 1.3f, .12f), new Vector4(310f, 1.9f, 1.8f, .14f),
        };
        // Speed lines past the lens during the yank: screen x, screen y (-1 to 1), length (share of the frame), delay.
        private static readonly Vector4[] PtSpeedRows =
        {
            new Vector4(-.82f, .55f, .34f, .00f), new Vector4(.78f, .62f, .28f, .02f), new Vector4(-.90f, -.20f, .30f, .04f),
            new Vector4(.88f, -.35f, .36f, .01f), new Vector4(-.60f, -.72f, .24f, .05f), new Vector4(.64f, -.80f, .26f, .03f),
            new Vector4(-.35f, .85f, .22f, .06f), new Vector4(.30f, .88f, .20f, .02f),
        };
        // A band whipping up a prisoner's shins as they hit the trunk: (angle round the legs, height, distance out), typed.
        private static readonly Vector3[] PtBandPath =
        {
            new Vector3(-20f, -.04f, .36f), new Vector3(40f, .08f, .27f), new Vector3(125f, .20f, .25f), new Vector3(215f, .33f, .24f),
            new Vector3(300f, .47f, .25f), new Vector3(385f, .61f, .26f), new Vector3(455f, .76f, .27f), new Vector3(510f, .88f, .28f),
        };

        private sealed class PtCaught
        {
            public MatchPoseHistory.Copy Body;
            public GameObject Holder;
            public Transform Model, AnimRoot;
            public Vector3 ModelOffset;      // the model's root from the feet, in the body's own yaw frame
            public Quaternion ModelTilt;     // the model's root rotation relative to that yaw
            public Vector3 Start, Hold, Dir; // feet at the start and in the hold (scene space), flat direction out from the spot
            public float StartYaw, Lash, WrapAt, Spin;
            public float StartAngle, StartDist, HoldAngle; // round the spot, degrees; and how far out they start
            public Transform[] Bones;
            public Quaternion[] RestRot, TmpRot;
            public Vector3[] RestPos, TmpPos;
            public AnimationClip Shock, Drag, Struggle;
            public PaeteRope Limb, Band;
            public int Head, KnotGlow, Pulse, Ring, Dust, Crack;
            public readonly int[] Trail = new int[3];
            public readonly int[] Leaves = new int[5];
            public bool Ghost;
        }

        private readonly List<PtCaught> _ptCaught = new List<PtCaught>(8);
        private readonly List<int> _ptSpeed = new List<int>(8);
        private readonly List<Vector3> _ptLimb = new List<Vector3>(40);
        private int _ptLead = -1;
        // Where the take settles, round the spot (degrees, scene space), and which side of the lead's limb the ride ends on (`PtStage`).
        private float _ptSettle = PaeteTreeYaw, _ptRideSide = 1f;

        private void BuildPaeteTake()
        {
            var round = GameServices.Round;
            var spot = _pvLanding + Vector3.up * _paeteCourt;
            if (round != null && _source != null)
            {
                // Where the live tree will stand, by the live rule, and who it will catch from there.
                var feet = _source.transform.position;
                var forward = _source.transform.forward; forward.y = 0f;
                var target = Abilities.PaeteVine.SentryTarget(feet, forward, _aim, round.Lata);
                var targetLocal = _root.transform.InverseTransformPoint(target); targetLocal.y = 0f;
                foreach (var p in round.Players)
                {
                    if (p == null || p == _source || p.PlayerSlot == _source.PlayerSlot || !Abilities.PaeteSentry.InReach(target, p)) continue;
                    if (_ptCaught.Count >= PtStagger.Length) break;
                    var local = _root.transform.InverseTransformPoint(p.transform.position); local.y = 0f;
                    var caught = PtCopy(p, spot, local - targetLocal);
                    if (caught != null) _ptCaught.Add(caught);
                }
            }
            if (_ptCaught.Count == 0)
            {
                // Nobody in reach: the limbs whip into the court, typed, round the way it faces.
                foreach (var row in PtStrikeRows)
                {
                    float a = (PaeteTreeYaw + row.x) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    _ptCaught.Add(new PtCaught { Ghost = true, Dir = dir, Start = spot + dir * row.y, Hold = spot + dir * Core.PaeteRules.SentryHoldDistance });
                }
            }
            // Timing per prisoner, and the one the camera rides to: the longest limb (the most court to follow).
            float longest = -1f;
            for (int i = 0; i < _ptCaught.Count; i++)
            {
                var c = _ptCaught[i];
                float d = Vector3.Distance(new Vector3(c.Start.x, 0f, c.Start.z), new Vector3(spot.x, 0f, spot.z));
                c.Lash = PtLashAt + PtStagger[i];
                c.WrapAt = c.Lash + Mathf.Clamp(d / PtLashSpeed, PtReachMin, PtReachMax);
                c.Spin = (i % 2 == 0 ? 1f : -1f) * 180f;
                if (d > longest) { longest = d; _ptLead = i; }
                // The limb (three dark cords, the embrace limb's own), the band up the shins, and their light.
                var bark = new[] { PaeteSentryBody.Bark, PaeteSentryBody.BarkDark, PaeteSentryBody.BarkDark };
                c.Limb = new PaeteRope(_root.transform, "PaeteTakeLimb" + i, bark, new[] { .075f, .066f, .058f }, .064f, 7.5f, 1.7f * i);
                c.Band = new PaeteRope(_root.transform, "PaeteTakeBand" + i, new[] { PaeteSentryBody.BarkDark, PaeteSentryBody.Bark }, new[] { .07f, .06f }, .045f, 9f, .9f * i);
                c.Head = AddGlow("PaeteTakeHead" + i, PbHot, falloff: 2.0f, core: 1.1f, lift: .1f);
                for (int k = 0; k < c.Trail.Length; k++) c.Trail[k] = AddGlow("PaeteTakeTrail" + i + "_" + k, PaeteLight, falloff: 2.2f, core: .6f);
                c.KnotGlow = AddGlow("PaeteTakeKnot" + i, PaeteLight, falloff: 1.8f, core: .5f, lift: .15f);
                c.Pulse = AddGlow("PaeteTakePulse" + i, PbHot, falloff: 2.0f, core: 1.0f, lift: .1f);
                c.Ring = AddGlow("PaeteTakeRing" + i, PaeteLight, PbRing, billboard: false, band: true, falloff: 1.3f, core: .5f);
                c.Dust = Add("PaeteTakeDust" + i, VfxShapes.Collar(16, .30f, .72f, .14f, 0f, 21 + i), new Color(0.86f, 0.80f, 0.68f, 0.5f), 0.1f, plain: true);
                c.Crack = c.Ghost ? AddSolid("PaeteTakeCrack" + i, VfxShapes.Fracture(5, 2, .05f, 71 + i), new Color(0.20f, 0.14f, 0.09f, 1f)) : -1;
                for (int k = 0; k < c.Leaves.Length; k++)
                    c.Leaves[k] = AddSolid("PaeteTakeLeaf" + i + "_" + k, PvLeafMesh, k % 2 == 0 ? PvLeaf : PvLeafDark);
            }
            PtStage(spot);
            for (int i = 0; i < PtSpeedRows.Length; i++)
                _ptSpeed.Add(PvTips(AddGlow("PaeteTakeSpeed" + i, i % 3 == 1 ? MakilingJade : PaeteLight, PvStreak, billboard: false, band: true, falloff: 1.6f, core: .4f), 1.4f));
        }

        /// <summary>A render copy of a player it will catch, standing <paramref name="offset"/> from the staged spot, with its rig's clips.</summary>
        private PtCaught PtCopy(CharacterMotor p, Vector3 spot, Vector3 offset)
        {
            var visual = p.GetComponent<CharacterVisual>();
            if (visual == null || visual.Model == null) return null;
            var track = new MatchPoseHistory.Track(p, visual.Model);
            track.Record(Time.time); track.Record(Time.time + .05f);
            var holder = new GameObject("PaeteTakeCaught-P" + (p.PlayerSlot + 1));
            holder.transform.SetParent(_root.transform, false);
            holder.SetActive(false);
            var copy = track.Clone(holder.transform);
            if (copy == null) { ObjectDestroy(holder); return null; }
            track.Apply(copy, track.Newest);
            holder.SetActive(true);
            foreach (var surface in copy.Renderers) surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            var model = visual.Model.transform;
            var motor = p.transform;
            var yaw = Quaternion.Euler(0f, motor.eulerAngles.y, 0f);
            var feet = new Vector3(motor.position.x, Slipper.GroundY(motor.position + Vector3.up * .3f), motor.position.z);
            var c = new PtCaught
            {
                Body = copy, Holder = holder, Model = copy.Root.transform,
                ModelOffset = Quaternion.Inverse(yaw) * (model.position - feet),
                ModelTilt = Quaternion.Inverse(yaw) * model.rotation,
                StartYaw = motor.eulerAngles.y - _facing.eulerAngles.y,
            };
            c.Model.localScale = model.lossyScale;
            // Brought inside 2.6 to 8.8 m of the spot, so the pull reads and nobody starts inside the roots.
            offset.y = 0f;
            float d = Mathf.Clamp(offset.magnitude, 2.6f, 8.8f);
            c.Dir = offset.sqrMagnitude > 1e-4f ? offset.normalized : Quaternion.Euler(0f, PaeteTreeYaw, 0f) * Vector3.forward;
            c.Start = spot + c.Dir * d;
            // Never on top of him: a start within 1.6 m of where he kneels is slid round the spot, away from him.
            var fromHim = new Vector3(c.Start.x, 0f, c.Start.z);
            if (fromHim.magnitude < 1.6f)
            {
                c.Dir = Quaternion.Euler(0f, 60f, 0f) * c.Dir;
                c.Start = spot + c.Dir * d;
            }
            c.Hold = spot + c.Dir * Core.PaeteRules.SentryHoldDistance;
            // The animated root: where the rig's clips are bound (the live `Animator`'s transform), and the clips his kit gave this rig.
            var animator = visual.Model.GetComponentInChildren<Animator>();
            c.AnimRoot = animator != null ? track.CopiedBone(copy, animator.transform) : c.Model;
            if (c.AnimRoot == null) c.AnimRoot = c.Model;
            string rig = animator != null ? DanceClip.ResourceName(animator.transform) : null;
            var set = string.IsNullOrEmpty(rig) ? null : Resources.Load<GeneratedAnimationSet>(RootedMotion.Folder + "/" + rig);
            if (set != null && set.Clips != null)
                foreach (var clip in set.Clips)
                {
                    if (clip == null) continue;
                    if (clip.name == RootedMotion.Breakout) c.Shock = clip;
                    else if (clip.name == RootedMotion.Heave) c.Drag = clip;
                    else if (clip.name == RootedMotion.Struggle) c.Struggle = clip;
                }
            c.Bones = copy.Bones;
            int n = c.Bones.Length;
            c.RestRot = new Quaternion[n]; c.TmpRot = new Quaternion[n]; c.RestPos = new Vector3[n]; c.TmpPos = new Vector3[n];
            for (int i = 0; i < n; i++) { c.RestRot[i] = c.Bones[i].localRotation; c.RestPos[i] = c.Bones[i].localPosition; }
            // ⚠️ HIDDEN BY SWITCHING THE HOLDER OFF, NOT BY `forceRenderingOff`: the scene turns every renderer under it ON for each
            // capture (`SetVisibleForCapture`), which would show them standing in the RISE. They step in on the cut to the TAKE.
            holder.SetActive(false);
            return c;
        }

        /// <summary>
        /// ⚠️⚠️ WHERE THE TAKE ENDS, AND WHERE THEY ARE HELD IN IT (films r20 and r21). Held on their own side of the trunk, as in play,
        /// the settle could never show them all: from the guardian's face one of the film's three hung behind the trunk and two
        /// overlapped in front of it. And a ride that ended on the far side of the lead's limb from the settle made the camera swing
        /// ACROSS the lead's line while they were flying down it, so they flew through the lens at a metre.
        ///  * The settle is the guardian's face turned half-way toward the lead (at most 45 degrees), so its eyes stay in the shot.
        ///  * The ride ends on the settle's side of the lead's limb (`_ptRideSide`), so the swing never crosses their path.
        ///  * The holds are FANNED across the settle's side of the trunk, in the order they stand round it, up to 52 degrees apart
        ///    (150 across for seven), and each is reeled round the trunk to theirs (`PtAngle`): the take shows who it caught and that
        ///    it caught them all. Where they end is staging; who, and that they are held at 1.4 m, is the rule.
        /// </summary>
        private void PtStage(Vector3 spot)
        {
            var real = new List<PtCaught>(_ptCaught.Count);
            foreach (var c in _ptCaught)
            {
                var flat = c.Start - spot; flat.y = 0f;
                c.StartAngle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                c.StartDist = flat.magnitude;
                c.HoldAngle = c.StartAngle;
                if (!c.Ghost) real.Add(c);
            }
            if (_ptLead < 0) return;
            float lead = _ptCaught[_ptLead].StartAngle;
            _ptSettle = PaeteTreeYaw + Mathf.Clamp(Mathf.DeltaAngle(PaeteTreeYaw, lead) * .5f, -45f, 45f);
            float toSettle = Mathf.DeltaAngle(lead, _ptSettle);
            _ptRideSide = Mathf.Abs(toSettle) < 8f ? 1f : Mathf.Sign(toSettle);
            real.Sort((x, y) => Mathf.DeltaAngle(_ptSettle, x.StartAngle).CompareTo(Mathf.DeltaAngle(_ptSettle, y.StartAngle)));
            float spacing = real.Count > 1 ? Mathf.Min(52f, 150f / (real.Count - 1)) : 0f;
            for (int k = 0; k < real.Count; k++)
            {
                var c = real[k];
                c.HoldAngle = _ptSettle + (k - (real.Count - 1) * .5f) * spacing;
                float a = c.HoldAngle * Mathf.Deg2Rad;
                c.Hold = spot + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * Core.PaeteRules.SentryHoldDistance;
            }
        }

        /// <summary>Where the ride ends: 2.4 m from them, 50 degrees round from the limb's line on the settle's side, so the limb and
        /// the yank pass beside the lens and the swing never crosses their path.</summary>
        private Vector3 PtRideEnd(PtCaught c)
        {
            var side = Vector3.Cross(Vector3.up, c.Dir).normalized * _ptRideSide;
            var face = c.Ghost ? c.Start + Vector3.up * .35f : new Vector3(c.Start.x, _paeteCourt + 1.3f, c.Start.z);
            return face + (-c.Dir * Mathf.Cos(50f * Mathf.Deg2Rad) + side * Mathf.Sin(50f * Mathf.Deg2Rad)) * 2.4f + Vector3.up * .3f;
        }

        /// <summary>How far through the yank a prisoner is (0 to 1), each a hundredth later than the one before.</summary>
        private float PtYank(PtCaught c, float t) => Mathf.Clamp01((t - PtYankAt - .01f * _ptCaught.IndexOf(c)) / (PtArriveAt - PtYankAt));

        /// <summary>Where round the spot a prisoner is (degrees): where they stood, reeled round the trunk to their hold as they are dragged.</summary>
        private float PtAngle(PtCaught c, float t) => Mathf.LerpAngle(c.StartAngle, c.HoldAngle, Ease(0f, 1f, PtYank(c, t)));

        // ------------------------------------------------------------------ where each one is, and how they stand

        /// <summary>Feet (scene space), facing (degrees), and how far through the drag a prisoner is, at <paramref name="t"/>.</summary>
        private Vector3 PtFeet(PtCaught c, float t, out float yaw, out float drag)
        {
            var spot = _pvLanding + Vector3.up * _paeteCourt;
            // They flinch as the cut lands and turn to the tree as the limb leaves the trunk.
            float turn = Ease(c.Lash - .02f, c.Lash + .12f, t);
            yaw = Mathf.LerpAngle(c.StartYaw, c.StartAngle + 180f, turn);
            float yank = PtYank(c, t);
            drag = yank;
            // Reeled in faster and faster, round the trunk to their hold, feet off the court, spun by the wrap to land facing out.
            float angle = PtAngle(c, t), a = angle * Mathf.Deg2Rad;
            float d = Mathf.Lerp(c.StartDist, Core.PaeteRules.SentryHoldDistance, Mathf.Pow(yank, 1.7f));
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            var feet = spot + dir * d;
            float hop = .12f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - c.Lash) / .22f));
            feet.y += hop + .32f * Mathf.Sin(Mathf.PI * yank);
            if (yank > 0f) yaw = angle + 180f + c.Spin * Ease(.15f, .95f, yank);
            // The thud: pressed 8 cm into the bark and back.
            feet -= dir * .08f * Decay(t - PtArriveAt, .18f);
            if (t > PtArriveAt) yaw = c.HoldAngle + 180f + c.Spin + 6f * Mathf.Sin((t - PtArriveAt) * 11f + c.Spin);
            return feet;
        }

        private void PtPose(PtCaught c, float t)
        {
            var feet = PtFeet(c, t, out float yaw, out float drag);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            // Back to the pose they were in, then each act laid over it by weight.
            for (int i = 1; i < c.Bones.Length; i++) { c.Bones[i].localRotation = c.RestRot[i]; c.Bones[i].localPosition = c.RestPos[i]; }
            float shock = Ease(PtLashAt - .03f, PtLashAt + .08f, t);
            float pulled = Ease(PtYankAt - .02f, PtYankAt + .06f, t);
            float held = Ease(PtArriveAt - .02f, PtArriveAt + .08f, t);
            // SHOCKED: the break-out's flung-open moment (arms up and out, chest thrown back), trembling.
            if (c.Shock != null && shock > 0f) PtBlend(c, c.Shock, .12f + .015f * Mathf.Sin(t * 41f), shock);
            // DRAGGED: the heave, leaning back against the pull, hands on the limb.
            if (c.Drag != null && pulled > 0f) PtBlend(c, c.Drag, Mathf.Lerp(.95f, RootedMotion.HeaveScrubSeconds, drag), pulled);
            // HELD, TRYING TO GET OUT: the struggle loop at its hard speed (a player holding Interact), as in play.
            if (c.Struggle != null && held > 0f) PtBlend(c, c.Struggle, Mathf.Repeat((t - PtArriveAt) * 1.25f + .2f * _ptCaught.IndexOf(c), c.Struggle.length), held);
            c.Model.localPosition = feet + turn * c.ModelOffset;
            c.Model.localRotation = turn * c.ModelTilt;
        }

        /// <summary>Lay <paramref name="clip"/> at <paramref name="time"/> over the current pose by <paramref name="weight"/>.</summary>
        private static void PtBlend(PtCaught c, AnimationClip clip, float time, float weight)
        {
            int n = c.Bones.Length;
            for (int i = 1; i < n; i++) { c.TmpRot[i] = c.Bones[i].localRotation; c.TmpPos[i] = c.Bones[i].localPosition; }
            var modelPos = c.Model.localPosition; var modelRot = c.Model.localRotation;
            clip.SampleAnimation(c.AnimRoot.gameObject, time);
            c.Model.localPosition = modelPos; c.Model.localRotation = modelRot;
            if (weight >= .999f) return;
            for (int i = 1; i < n; i++)
            {
                c.Bones[i].localRotation = Quaternion.Slerp(c.TmpRot[i], c.Bones[i].localRotation, weight);
                c.Bones[i].localPosition = Vector3.Lerp(c.TmpPos[i], c.Bones[i].localPosition, weight);
            }
        }

        /// <summary>Where a prisoner's limb leaves the trunk: the bark 0.52 m out at 2.0 m, on their side as it is now (the embrace limb's own place).</summary>
        private Vector3 PtAnchor(PtCaught c, float t)
        {
            float a = PtAngle(c, t) * Mathf.Deg2Rad;
            return _pvLanding + Vector3.up * (_paeteCourt + 2.0f) + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * .52f;
        }

        /// <summary>The limb's curve from the trunk to <paramref name="to"/>: out, up and over, down onto them.</summary>
        private static Vector3 PtCurve(Vector3 from, Vector3 to, float u)
        {
            var flat = to - from; flat.y = 0f;
            float d = flat.magnitude;
            var n = d > 1e-3f ? flat / d : Vector3.forward;
            var c1 = from + n * (d * .33f) + Vector3.up * (.5f + .12f * d);
            var c2 = to - n * (d * .2f) + Vector3.up * (.35f + .05f * d);
            float v = 1f - u;
            return v * v * v * from + 3f * v * v * u * c1 + 3f * v * u * u * c2 + u * u * u * to;
        }

        // ------------------------------------------------------------------ the sample

        private void SamplePaeteTake(float t, float leave)
        {
            bool on = t >= PtLashAt - .005f;
            float court = _paeteCourt;
            var spot = _pvLanding + Vector3.up * court;
            float light = _reducedEffects ? .5f : 1f;
            for (int i = 0; i < _ptCaught.Count; i++)
            {
                var c = _ptCaught[i];
                if (c.Holder != null)
                {
                    if (c.Holder.activeSelf != on) c.Holder.SetActive(on);
                    if (on) PtPose(c, t);
                }
                if (!on) { PtHideOne(c); continue; }
                var anchor = PtAnchor(c, t);
                Vector3 waist = c.Ghost ? c.Start : PtFeet(c, t, out _, out _) + Vector3.up * .92f;
                var toTree = anchor - waist; toTree.y = 0f;
                float a0 = Mathf.Atan2(toTree.x, toTree.z);
                const float Loop = .28f;
                var wrapStart = c.Ghost ? c.Start + Vector3.down * .3f : waist + new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * Loop + Vector3.up * .12f;
                float reach = Ease(c.Lash, c.WrapAt, t);
                // A ghost limb (nobody in reach) dives into the court, holds, and reels back into the trunk on the yank.
                if (c.Ghost) reach *= 1f - Ease(PtYankAt, PtArriveAt, t);
                float wrap = c.Ghost ? 0f : Ease(c.WrapAt, c.WrapAt + .12f, t);

                _ptLimb.Clear();
                int samples = Mathf.Max(2, Mathf.RoundToInt(16 * reach));
                for (int k = 0; k < samples; k++) _ptLimb.Add(PtCurve(anchor, wrapStart, reach * k / (samples - 1f)));
                if (wrap > 0f)
                {
                    int turns = Mathf.RoundToInt(22 * wrap);
                    float cinch = 1f - .10f * Ease(c.WrapAt + .1f, c.WrapAt + .25f, t) - .03f * Mathf.Sin(t * 9f + i);
                    for (int k = 1; k <= turns; k++)
                    {
                        float f = k / 22f, ang = a0 + f * Mathf.PI * 4f;
                        _ptLimb.Add(waist + new Vector3(Mathf.Sin(ang) * Loop * cinch, .16f - .30f * f, Mathf.Cos(ang) * Loop * cinch));
                    }
                }
                if (reach > .01f) c.Limb.Draw(_ptLimb, .45f); else c.Limb.Clear();

                // Its head as it lashes out: a hot point of his light, three glints trailing it.
                var head = PtCurve(anchor, wrapStart, reach);
                float lashing = reach > .01f && reach < .999f && !(c.Ghost && t > PtYankAt) ? 1f : 0f;
                PlaceGlow(c.Head, head, Vector3.one * .42f, Quaternion.identity, 2.0f * lashing * light * leave);
                for (int k = 0; k < c.Trail.Length; k++)
                {
                    float back = Mathf.Max(0f, reach - .09f * (k + 1));
                    PlaceGlow(c.Trail[k], PtCurve(anchor, wrapStart, back), Vector3.one * (.26f - .05f * k), Quaternion.identity, (1.3f - .3f * k) * lashing * light * leave);
                }

                // The cinch: a ring of light snaps round the waist, leaves are torn off it.
                {
                    float s = t - c.WrapAt;
                    if (c.Ghost || s < 0f || s > .3f) PvHide(c.Ring);
                    else
                    {
                        float r = .45f + .75f * (1f - (1f - s / .3f) * (1f - s / .3f));
                        PlaceGlow(c.Ring, waist + Vector3.up * .05f, new Vector3(r, r, 1f), Quaternion.Euler(90f, 0f, 0f), 1.8f * (1f - s / .3f) * light * leave);
                    }
                    for (int k = 0; k < c.Leaves.Length; k++)
                    {
                        var row = PtWrapLeafRows[k];
                        float ls = t - c.WrapAt - .02f * k;
                        var from = c.Ghost ? c.Start + Vector3.up * .1f : waist;
                        if (ls < 0f || ls > .7f) { PvHide(c.Leaves[k]); continue; }
                        float ang = (row.x + 37f * i) * Mathf.Deg2Rad;
                        var dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                        var p = from + dir * row.y * (1f - Mathf.Exp(-3f * ls)) / 3f + Vector3.up * (row.z * ls - 2.2f * ls * ls);
                        Place(c.Leaves[k], p, Vector3.one * row.w * (1f - Ease(.5f, .7f, ls)),
                              Quaternion.Euler(ls * 600f + 40f * k, ls * 380f + row.x, 30f * k), leave);
                    }
                }

                // The drag: dust scraped up at their feet; a ghost limb cracks the court where it strikes.
                {
                    float s = t - PtYankAt;
                    var feetNow = c.Ghost ? c.Start : PtFeet(c, t, out _, out _);
                    feetNow.y = court;
                    float d = Ease(0f, .5f, s);
                    if (c.Ghost || s < 0f || s > .5f) Place(c.Dust, Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);
                    else Place(c.Dust, feetNow + Vector3.up * (.04f + .2f * d), new Vector3(.5f + 1.4f * d, .8f - .4f * d, .5f + 1.4f * d), Quaternion.identity, (1f - d) * .8f * leave);
                    if (c.Crack >= 0)
                    {
                        float k = Ease(c.WrapAt - .02f, c.WrapAt + .06f, t) * (1f - Ease(PtArriveAt, 4.9f, t));
                        Place(c.Crack, new Vector3(c.Start.x, court + .015f, c.Start.z), new Vector3(1f, 1f, 1f) * Mathf.Max(.001f, k), Quaternion.Euler(0f, 23f * i, 0f), k > .001f ? leave : 0f);
                    }
                }

                // Held: his light runs down each limb to the waist on the thud, and the knot glows, breathing with the tree.
                {
                    float s = (t - PtArriveAt) / .28f;
                    if (c.Ghost || s < 0f || s > 1f || _ptLimb.Count < 2) PvHide(c.Pulse);
                    else PlaceGlow(c.Pulse, PtCurve(anchor, wrapStart, Mathf.Clamp01(s)), Vector3.one * .5f, Quaternion.identity, 2.0f * Mathf.Sin(Mathf.PI * s) * light * leave);
                    float knot = c.Ghost ? 0f : Ease(PtArriveAt, PtArriveAt + .15f, t) * (.8f + .2f * Mathf.Sin(t * 6f + i));
                    PlaceGlow(c.KnotGlow, waist, Vector3.one * .55f, Quaternion.identity, knot * light * leave);
                }

                // A band whips up their shins as they hit the trunk.
                if (!c.Ghost)
                {
                    float grow = Ease(PtArriveAt - .02f, PtArriveAt + .16f, t);
                    if (grow <= .01f) c.Band.Clear();
                    else
                    {
                        var legs = PtFeet(c, t, out float yaw, out _);
                        _ptLimb.Clear();
                        int count = Mathf.Max(2, Mathf.RoundToInt(PtBandPath.Length * grow));
                        for (int k = 0; k < count; k++)
                        {
                            var row = PtBandPath[k];
                            float ang = (row.x + yaw) * Mathf.Deg2Rad;
                            _ptLimb.Add(new Vector3(legs.x, court, legs.z) + new Vector3(Mathf.Sin(ang) * row.z, row.y, Mathf.Cos(ang) * row.z));
                        }
                        c.Band.Draw(_ptLimb, .5f);
                    }
                }
            }

            // Speed lines past the lens during the yank, streaking the way the camera swings.
            Vector3 eye = default, look = default; float fov = 54f;
            bool speeding = t > PtYankAt - .01f && t < PtArriveAt + .1f && PtCamera(t, out eye, out look, out fov);
            for (int i = 0; i < _ptSpeed.Count; i++)
            {
                var row = PtSpeedRows[i];
                float s = (t - PtYankAt - row.w) / .32f;
                if (!speeding || s < 0f || s > 1f || _reducedEffects) { PvHide(_ptSpeed[i]); continue; }
                var fwd = (look - eye).normalized;
                var right = Vector3.Cross(Vector3.up, fwd).normalized;
                var lensUp = Vector3.Cross(fwd, right);
                const float D = 1.2f;
                float halfH = D * Mathf.Tan(fov * .5f * Mathf.Deg2Rad), halfW = halfH * 16f / 9f;
                var at = eye + fwd * D + right * row.x * halfW + lensUp * row.y * halfH;
                // Streaks run toward the middle of the frame, sliding outward as the camera swings.
                var toward = -(right * row.x + lensUp * row.y).normalized;
                var face = Quaternion.LookRotation(-fwd, toward);
                PlaceGlow(_ptSpeed[i], at - toward * s * .25f * halfH, new Vector3(.012f, row.z * halfH, 1f), face, 1.1f * Mathf.Sin(Mathf.PI * s) * leave);
            }
        }

        private void PtHideOne(PtCaught c)
        {
            c.Limb?.Clear(); c.Band?.Clear();
            PvHide(c.Head); foreach (int k in c.Trail) PvHide(k);
            PvHide(c.KnotGlow); PvHide(c.Pulse); PvHide(c.Ring);
            Place(c.Dust, Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);
            if (c.Crack >= 0) Place(c.Crack, Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f);
            foreach (int k in c.Leaves) PvHide(k);
        }

        // ------------------------------------------------------------------ the camera

        /// <summary>
        /// ⚠️⚠️ THE TAKE'S ONE CONTINUOUS MOVE, computed from where they really stand (scene space; `PaeteFrame` hands it to the
        /// shot). It rides the longest limb out from the bark to the player on its end, holds on their face as it wraps them,
        /// rises and swings round the tree as everyone is yanked in (orbiting in polar coordinates round the spot, so it never
        /// passes through the trunk), and settles on a low wide of the guardian's face with them all bound round it.
        /// False (the authored row is used) before the take or with no lead.
        /// </summary>
        private bool PtCamera(float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            eye = look = Vector3.zero; fov = 54f;
            if (t < PtLashAt - .005f || _ptLead < 0 || _ptLead >= _ptCaught.Count) return false;
            var c = _ptCaught[_ptLead];
            var up = Vector3.up;
            var spot = _pvLanding + up * _paeteCourt;
            var dir = c.Dir;
            var side = Vector3.Cross(up, dir).normalized * _ptRideSide;
            var anchor = PtAnchor(c, PtLashAt);
            var face = c.Ghost ? c.Start + up * .35f : new Vector3(c.Start.x, _paeteCourt + 1.3f, c.Start.z);

            // 1 THE RIDE: from beside the limb's root, chasing its head out to them. ⚠️ It ends BESIDE them (`PtRideEnd`), not on the
            // limb's line: film r20 ended 1.75 m in front of them on that line, so the limb crossed the lens and their face, and on
            // the yank they flew straight into the camera.
            var c0 = anchor + dir * .4f + side * .95f + up * .4f;
            var c1 = PtRideEnd(c);
            float chase = Ease(PtLashAt, c.WrapAt + .05f, t);
            var ride = Vector3.Lerp(c0, c1, chase) + up * .3f * Mathf.Sin(Mathf.PI * chase);
            var rideLook = Vector3.Lerp(anchor + dir * 2.4f + Vector3.down * .6f, face, Mathf.Pow(chase, .7f));
            // 2 THE HOLD: on their face as it wraps them, easing in 20 cm.
            float hold = Ease(c.WrapAt, PtYankAt, t);
            ride += (face - c1).normalized * .2f * hold;

            // 3 THE SWING and 4 THE SETTLE: round the spot to a low wide on its face.
            var c1Flat = ride - spot; c1Flat.y = 0f;
            float aFrom = Mathf.Atan2(c1Flat.x, c1Flat.z) * Mathf.Rad2Deg;
            float aTo = _ptSettle;
            const float Radius = 7.4f, Height = 1.2f;
            float swing = Ease(PtYankAt - .02f, PtArriveAt + .12f, t);
            if (swing <= 0f)
            {
                eye = ride; look = rideLook; fov = Mathf.Lerp(54f, 48f, hold);
                return true;
            }
            float rFrom = c1Flat.magnitude, hFrom = ride.y - _paeteCourt;
            // Out fast (it is already on the far side from their path), up over the court, and round to the settle.
            float a = Mathf.LerpAngle(aFrom, aTo, swing) * Mathf.Deg2Rad;
            float r = Mathf.Lerp(rFrom, Radius, 1f - (1f - swing) * (1f - swing));
            float h = Mathf.Lerp(hFrom, Height, swing) + 1.5f * Mathf.Sin(Mathf.PI * swing);
            // The settle drifts in 0.5 m and a little lower, so the last frame still moves.
            float settle = Ease(PtArriveAt + .12f, PaeteEnd, t);
            r -= .5f * settle; h -= .1f * settle;
            eye = spot + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r);
            // ⚠️ AND NEVER WITHIN 2.3 M OF A FLYING BODY (film r22: the lead, reeled round the trunk toward the settle's side, still
            // passed about a metre from the lens for a fifth of a second). Every staged body pushes the lens straight away from its
            // chest; the move itself is unchanged wherever nobody is near it.
            foreach (var other in _ptCaught)
            {
                if (other.Ghost) continue;
                var chest = PtFeet(other, t, out _, out _) + up * 1.0f;
                var away = eye - chest;
                if (away.sqrMagnitude < 2.3f * 2.3f) eye = chest + (away.sqrMagnitude > 1e-4f ? away.normalized : up) * 2.3f;
            }
            var waist = c.Ghost ? c.Start : PtFeet(c, t, out _, out _) + up * .9f;
            look = Vector3.Lerp(waist, spot + up * 2.75f, Ease(0f, .75f, swing));
            fov = Mathf.Lerp(48f, 58f, swing);
            return true;
        }
    }
}
