using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v3: THE LANE, 3.70 to 5.60 s (`docs/reports/amihan-presentation-2026-10-02/airburst-v3.md`). The 3.6 s
        // version's AIM shot looked down an EMPTY lane: the shared phase hides every live body while it draws, so the one thing
        // the cast is about (who is standing in it) was not on screen.
        //
        // ⚠️⚠️ THE PLAYERS ARE THE REAL ONES, copied the way Paete's TAKE copies his prisoners (`HeroIntroductionScene.PaeteTake.cs`):
        // `MatchPoseHistory.Track.Clone` (render data only), standing exactly where they stand, by the live rule on the accepted
        // cast (`AmihanStorm.InsideFan` from her feet along her facing, the same test the release uses). Nobody is moved or
        // invented: where they are is the information.
        //
        //   3.70        CUT to the WARP: everyone is there, as they were.
        //   4.10        the first breath of wind passes them. Those INSIDE the fan turn to her and brace: leaning in, arms up
        //               over the face (the break-out's flung-open arms laid at half weight over a forward lean), trembling, and a
        //               breath of cotton streams past each of them. Those OUTSIDE stand still: that is the read.
        //   5.05        v3.2 THE HIT (owner: *"show the ult actually hitting and knocking abck ppl already in the cutscene"*): those
        //               inside are thrown by the live rule's numbers, Whirled round, arms flung; those outside stand untouched.
        //   5.60        hand-back ON the hit: the host throws them for real from where they stand (no live delay).
        //
        // ⚠️ The hit it shows is the rule's own: who (`InsideFan`), which way (`BlowDirection`), how fast and how high, and play
        // resumes on it with no dodge window, so it is never a promise the rules break. ⚠️ Nothing here changes a rule, a number or a byte on the wire. Posed from the scene clock only; no `Random`.
        // =========================================================================================

        private const int AlMax = 9;
        // The stagger of the brace, so a lane of players is a ripple outward from her and not one switch.
        private const float AlStaggerPerMetre = .018f;

        private sealed class AlBody
        {
            public MatchPoseHistory.Copy Body;
            public GameObject Holder;
            public Transform Model, AnimRoot;
            public Vector3 Feet, ModelOffset;
            public Quaternion ModelTilt;
            public float Yaw, ToHer, BraceAt, HitAt;
            public Vector3 Blow;
            public bool Inside;
            public Transform[] Bones;
            public Quaternion[] RestRot, TmpRot;
            public Vector3[] RestPos, TmpPos;
            public AnimationClip Brace;
            public Transform ArmLeft, ArmRight, Head;
            public WindVfx.Motif Breath;
        }

        private readonly List<AlBody> _alBodies = new List<AlBody>(AlMax);

        private void BuildAmihanLane()
        {
            var round = GameServices.Round;
            if (round == null || _source == null) return;
            var origin = _source.transform.position;
            var forward = _source.transform.forward; forward.y = 0f;
            foreach (var p in round.Players)
            {
                if (_alBodies.Count >= AlMax) break;
                if (p == null || p == _source || p.PlayerSlot == _source.PlayerSlot || !p.gameObject.activeInHierarchy) continue;
                var body = AlCopy(p, Abilities.AmihanStorm.InsideFan(origin, forward, p.transform.position));
                if (body != null) _alBodies.Add(body);
            }
        }

        /// <summary>A render copy of a player where they really stand, with the clip their rig carries for the brace.</summary>
        private AlBody AlCopy(CharacterMotor p, bool inside)
        {
            var visual = p.GetComponent<CharacterVisual>();
            if (visual == null || visual.Model == null) return null;
            var track = new MatchPoseHistory.Track(p, visual.Model);
            track.Record(Time.time); track.Record(Time.time + .05f);
            var holder = new GameObject("AmihanLane-P" + (p.PlayerSlot + 1));
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
            var feetWorld = new Vector3(motor.position.x, Slipper.GroundY(motor.position + Vector3.up * .3f), motor.position.z);
            var feet = _root.transform.InverseTransformPoint(feetWorld);
            var flat = new Vector3(feet.x, 0f, feet.z);
            var c = new AlBody
            {
                Body = copy, Holder = holder, Model = copy.Root.transform, Feet = feet, Inside = inside,
                ModelOffset = Quaternion.Inverse(yaw) * (model.position - feetWorld),
                ModelTilt = Quaternion.Inverse(yaw) * model.rotation,
                Yaw = motor.eulerAngles.y - _facing.eulerAngles.y,
                // Facing her: the way back along the lane to her feet.
                ToHer = flat.sqrMagnitude > 1e-4f ? Mathf.Atan2(-flat.x, -flat.z) * Mathf.Rad2Deg : 180f,
                BraceAt = AmBraceAt + flat.magnitude * AlStaggerPerMetre,
                // The front crosses the court in about four frames: each is hit a hundredth of a second per metre out.
                HitAt = AmReleaseAt + flat.magnitude * .01f,
                // The live rule's direction (`AmihanStorm.BlowDirection`), in the scene's frame: out along the fan, leaning on it.
                Blow = Abilities.AmihanStorm.BlowDirection(Vector3.zero, Vector3.forward, flat),
            };
            c.Model.localScale = model.lossyScale;
            var animator = visual.Model.GetComponentInChildren<Animator>();
            c.AnimRoot = animator != null ? track.CopiedBone(copy, animator.transform) : c.Model;
            if (c.AnimRoot == null) c.AnimRoot = c.Model;
            string rig = animator != null ? DanceClip.ResourceName(animator.transform) : null;
            var set = GeneratedMotionAssets.For(RootedMotion.Folder, rig);
            if (set != null && set.Clips != null)
                foreach (var clip in set.Clips)
                    if (clip != null && clip.name == RootedMotion.Breakout) c.Brace = clip;
            c.Bones = copy.Bones;
            int n = c.Bones.Length;
            c.RestRot = new Quaternion[n]; c.TmpRot = new Quaternion[n]; c.RestPos = new Vector3[n]; c.TmpPos = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                c.RestRot[i] = c.Bones[i].localRotation; c.RestPos[i] = c.Bones[i].localPosition;
                if (c.Bones[i].name == "arm-left") c.ArmLeft = c.Bones[i];
                else if (c.Bones[i].name == "arm-right") c.ArmRight = c.Bones[i];
                else if (c.Bones[i].name == "head") c.Head = c.Bones[i];
            }
            if (inside)
            {
                // Its own host: a Motif hands its shared tuft mesh to its parent's single GeneratedMeshOwner.
                var host = new GameObject("AmihanLaneBreath-P" + (p.PlayerSlot + 1)).transform;
                host.SetParent(_root.transform, false);
                // 14 pieces (film r4: 7 small ones vanished at the REVEAL's distance).
                c.Breath = new WindVfx.Motif(host, 14, 31.7f + p.PlayerSlot * 5.3f, .45f);
            }
            // ⚠️ HIDDEN BY SWITCHING THE HOLDER OFF, NOT BY `forceRenderingOff` (`SetVisibleForCapture` turns every renderer under
            // the root on for each capture, which would stand them in her CALL and WEAVE). They step in on the cut to the WARP.
            holder.SetActive(false);
            return c;
        }

        private void SampleAmihanLane(float t)
        {
            bool on = t >= AmWarpAt - .005f;
            float light = _reducedEffects ? .5f : 1f;
            float leave = 1 - Ease(Seconds - .30f, Seconds, t);
            for (int i = 0; i < _alBodies.Count; i++)
            {
                var c = _alBodies[i];
                if (c.Holder != null && c.Holder.activeSelf != on) c.Holder.SetActive(on);
                if (!on) { c.Breath?.Step(0f, 0f, (_, __, ___) => Vector3.zero); continue; }
                AlPose(c, t);
                if (c.Breath == null) continue;
                // A breath of cotton streaming past them, down the lane away from her, from the moment the wind reaches them.
                float s = t - c.BraceAt;
                var flat = new Vector3(c.Feet.x, 0f, c.Feet.z);
                var away = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward;
                var side = Vector3.Cross(Vector3.up, away);
                var feet = c.Feet;
                c.Breath.Step(s <= 0f ? 0f : Mathf.Repeat(s / 1.1f, 1f), (_reducedEffects ? .45f : .85f) * Ease(0f, .15f, s) * leave,
                    (start, drift, u) => feet - away * 1.1f + away * (2.4f * u) + side * (start.x * 1.3f) + Vector3.up * (.35f + start.y * 1.5f + drift.y * .3f * u),
                    .11f * light + .05f);
            }
        }

        private void AlPose(AlBody c, float t)
        {
            for (int i = 1; i < c.Bones.Length; i++) { c.Bones[i].localRotation = c.RestRot[i]; c.Bones[i].localPosition = c.RestPos[i]; }
            float yaw = c.Yaw, lean = 0f;
            if (c.Inside)
            {
                float brace = Ease(c.BraceAt - .04f, c.BraceAt + .16f, t);
                yaw = Mathf.LerpAngle(c.Yaw, c.ToHer, Ease(c.BraceAt - .06f, c.BraceAt + .22f, t));
                // Leaning into the wind, a little more on each gust, trembling as it presses on them.
                float gust = .5f + .5f * Mathf.Sin((t - c.BraceAt) * 7.0f);
                lean = brace * (15f + 4f * gust) + (_reducedEffects ? 0f : brace * .8f * Mathf.Sin(t * 37f + c.Feet.x * 3f));
                // ARMS UP OVER THE FACE: the break-out's flung-open moment at most of its weight (r1's half weight barely read), the
                // forward lean holding the chest in against it.
                if (c.Brace != null && brace > 0f) AlBlend(c, c.Brace, .12f + .012f * Mathf.Sin(t * 41f), .45f * brace);
                // ⚠️ FOREARMS UP ACROSS THE FACE (film r2: they face her, so the lean points straight down the lens and the brace
                // never read; the arms must). The authoring tool's convention (`arm_raw`): pitch -raise, roll (80 - spread) on the
                // left and its negative on the right. Raised 140 degrees (film r3: at 112 their short arms stayed at the chest),
                // spread 36, turned in 26, so on these big-headed bodies the forearms come up in front of the face.
                float shiver = _reducedEffects ? 0f : 3f * Mathf.Sin(t * 33f + c.Feet.z);
                if (c.ArmLeft != null) c.ArmLeft.localRotation = Quaternion.Slerp(c.ArmLeft.localRotation, Quaternion.Euler(-142f + shiver, 26f, 44f), brace);
                if (c.ArmRight != null) c.ArmRight.localRotation = Quaternion.Slerp(c.ArmRight.localRotation, Quaternion.Euler(-136f - shiver, -26f, -44f), brace);
                // The head ducks behind them, chin in.
                if (c.Head != null) c.Head.localRotation = Quaternion.Slerp(c.Head.localRotation, c.Head.localRotation * Quaternion.Euler(14f, 0f, 0f), brace);
            }
            // ⚠️ THE HIT (v3.2): thrown by the live rule's numbers (`AmihanRules.StormSurgeSpeed` out along the blow, `StormSurgeLift`
            // up, the court's 20 m/s2 down), Whirled round as they go, arms flung, falling back. Play takes over 0.55 s in; the live
            // throw is the host's, from where they really stand.
            var feet = c.Feet;
            float flown = c.Inside ? t - c.HitAt : -1f;
            if (flown > 0f)
            {
                float speed = Core.AmihanRules.StormSurgeSpeed, lift = Core.AmihanRules.StormSurgeLift;
                feet += c.Blow * speed * flown + Vector3.up * Mathf.Max(0f, lift * flown - 10f * flown * flown);
                yaw = Mathf.Atan2(-c.Blow.x, -c.Blow.z) * Mathf.Rad2Deg + 620f * flown;
                lean = -Mathf.Lerp(0f, 38f, Ease(0f, .18f, flown));
                if (c.Brace != null) AlBlend(c, c.Brace, .12f, 1f);
            }
            var turn = Quaternion.Euler(0f, yaw, 0f);
            // The lean pivots at the feet, toward the way they face (into the wind).
            // Film r3: they face her, so the forward lean points down the lens; a sideways buffet, gust by gust, reads from the front.
            float buffet = c.Inside ? Ease(c.BraceAt - .04f, c.BraceAt + .16f, t) * (5f + 3f * Mathf.Sin((t - c.BraceAt) * 9f + c.Feet.x)) * (c.Feet.x >= 0f ? 1f : -1f) : 0f;
            var tilt = turn * Quaternion.Euler(lean, 0f, buffet);
            c.Model.localPosition = feet + tilt * c.ModelOffset;
            c.Model.localRotation = tilt * c.ModelTilt;
        }

        /// <summary>Lay <paramref name="clip"/> at <paramref name="time"/> over the current pose by <paramref name="weight"/>.</summary>
        private static void AlBlend(AlBody c, AnimationClip clip, float time, float weight)
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

        /// <summary>
        /// Who the RIDE goes to (`AmihanFrame`): the nearest player standing in the fan at least 2.5 m out (nearer, the ride has
        /// no lane to travel), at chest height; nobody in it, the middle of the lane 8 m out.
        /// </summary>
        private Vector3 AlLead()
        {
            AlBody best = null; float bestDistance = float.MaxValue;
            foreach (var c in _alBodies)
            {
                if (!c.Inside) continue;
                float d = new Vector2(c.Feet.x, c.Feet.z).magnitude;
                if (d >= 2.5f && d < bestDistance) { best = c; bestDistance = d; }
            }
            return best != null ? best.Feet + Vector3.up * 1.1f : new Vector3(0f, _amCourt + 1.1f, 8f);
        }

        /// <summary>For probes: how many real players the lane staged, and how many of them stand inside her fan.</summary>
        public int AmihanLaneCount => _alBodies.Count;
        public int AmihanLaneInside { get { int n = 0; foreach (var c in _alBodies) if (c.Inside) n++; return n; } }
    }
}
