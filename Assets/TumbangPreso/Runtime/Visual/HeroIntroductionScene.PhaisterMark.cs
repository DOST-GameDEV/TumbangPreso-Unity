using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, VOODOO DOLL: THE REAL OPPONENTS AND THE CAMERAS (HERO-10 v3, plan 9.8b; `docs/HERO_KIT_METHOD.md` section 6, "the
        // ending must show what the ultimate DOES").
        //
        // What the doll does is fight them for the rest of the round, so the ending puts it in front of THEM: copies of the real
        // opponents nearest where it lands (up to three, everyone but her within 14 m), their own rigs, skins and outfits
        // (`MatchPoseHistory.Track.Clone`, render data only), standing where they really stand so play picks up with them there. They
        // FLINCH when it lands (the rig's own break-out frame, `RootedMotion.Breakout`) and turn to it, and LEAN AWAY as it lurches at
        // them (`RootedMotion.Heave`). Nobody near: the monster lurches at the court. Nobody is invented.
        //
        // The cameras of THE CIRCLE, THE DESCENT and THE PUPPET are computed from where the circle, the monster and they are; the
        // rows in `phaister()` are their storyboard and fallback. Never nearer than 2.3 m to a body. Posed from the scene clock only.
        // =========================================================================================

        private sealed class PmTarget
        {
            public MatchPoseHistory.Copy Body;
            public GameObject Holder;
            public Transform Model, AnimRoot;
            public Vector3 ModelOffset, Feet;
            public Quaternion ModelTilt;
            public float Yaw0, Top;
            public Transform[] Bones;
            public Quaternion[] RestRot, TmpRot;
            public Vector3[] RestPos, TmpPos;
            public AnimationClip Shock, Heave;
        }

        private readonly List<PmTarget> _pmTargets = new List<PmTarget>(3);
        private const float PmCameraGap = 2.3f, PmReach = 14f;

        private void BuildPhaisterStage()
        {
            var round = GameServices.Round;
            if (round == null || _source == null) return;
            var landWorld = _root.transform.TransformPoint(PhDollSpot);
            var near = new List<(CharacterMotor p, float d)>();
            foreach (var p in round.Players)
            {
                if (p == null || p == _source || p.PlayerSlot == _source.PlayerSlot) continue;
                var d = p.transform.position - landWorld; d.y = 0f;
                if (d.magnitude <= PmReach) near.Add((p, d.magnitude));
            }
            near.Sort((a, b) => a.d.CompareTo(b.d));
            foreach (var (p, _) in near)
            {
                if (_pmTargets.Count >= 3) break;
                var target = PmCopy(p);
                if (target != null) _pmTargets.Add(target);
            }
        }

        /// <summary>A render copy of a real opponent, standing where they stand, with its rig's clips.</summary>
        private PmTarget PmCopy(CharacterMotor p)
        {
            var visual = p.GetComponent<CharacterVisual>();
            if (visual == null || visual.Model == null) return null;
            var track = new MatchPoseHistory.Track(p, visual.Model);
            track.Record(Time.time); track.Record(Time.time + .05f);
            var holder = new GameObject("DollOpponent-P" + (p.PlayerSlot + 1));
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
            float top = 2.0f;
            foreach (var r in copy.Renderers) if (r != null) top = Mathf.Max(top, r.bounds.max.y - feetWorld.y);
            var t = new PmTarget
            {
                Body = copy, Holder = holder, Model = copy.Root.transform,
                ModelOffset = Quaternion.Inverse(yaw) * (model.position - feetWorld),
                ModelTilt = Quaternion.Inverse(yaw) * model.rotation,
                Feet = _root.transform.InverseTransformPoint(feetWorld),
                Yaw0 = motor.eulerAngles.y - _facing.eulerAngles.y,
                Top = Mathf.Min(top, 3.2f),
            };
            t.Model.localScale = model.lossyScale;
            var animator = visual.Model.GetComponentInChildren<Animator>();
            t.AnimRoot = animator != null ? track.CopiedBone(copy, animator.transform) : t.Model;
            if (t.AnimRoot == null) t.AnimRoot = t.Model;
            string rig = animator != null ? DanceClip.ResourceName(animator.transform) : null;
            var set = GeneratedMotionAssets.For(RootedMotion.Folder, rig);
            if (set != null && set.Clips != null)
                foreach (var clip in set.Clips)
                {
                    if (clip == null) continue;
                    if (clip.name == RootedMotion.Breakout) t.Shock = clip;
                    else if (clip.name == RootedMotion.Heave) t.Heave = clip;
                }
            t.Bones = copy.Bones;
            int n = t.Bones.Length;
            t.RestRot = new Quaternion[n]; t.TmpRot = new Quaternion[n];
            t.RestPos = new Vector3[n]; t.TmpPos = new Vector3[n];
            for (int i = 0; i < n; i++) { t.RestRot[i] = t.Bones[i].localRotation; t.RestPos[i] = t.Bones[i].localPosition; }
            SetLayer(holder, _root.layer);
            // ⚠️ Hidden by switching the holder off, not by `forceRenderingOff` (the scene turns every renderer under it on for each
            // capture: Paete's trap). They stand in THE DROP and THE PUPPET only; the earlier shots are about her and the circle.
            holder.SetActive(false);
            return t;
        }

        private void SamplePhaisterStage(float t, float leave)
        {
            bool on = t >= PhLandAt - .3f;
            Vector3 monster = PhMonsterFeet(t);
            foreach (var c in _pmTargets)
            {
                if (c.Holder.activeSelf != on) c.Holder.SetActive(on);
                if (on) PmPose(c, t, monster);
            }
        }

        private void PmPose(PmTarget c, float t, Vector3 monster)
        {
            for (int i = 1; i < c.Bones.Length; i++) { c.Bones[i].localRotation = c.RestRot[i]; c.Bones[i].localPosition = c.RestPos[i]; }
            // The flinch as it lands, and again as it lurches; then leaning away from it, trembling.
            float flinch = Mathf.Max(Ease(PhLandAt - .02f, PhLandAt + .08f, t) * (1f - .6f * Ease(PhLandAt + .2f, PhLandAt + .45f, t)),
                                     Ease(PhPuppetAt + .28f, PhPuppetAt + .36f, t) * (1f - .7f * Ease(PhPuppetAt + .5f, PhPuppetAt + .8f, t)));
            float lean = Ease(PhPuppetAt + .3f, PhPuppetAt + .6f, t);
            if (c.Shock != null && flinch > 0f) PmBlend(c, c.Shock, .12f + .012f * Mathf.Sin(t * 41f), flinch);
            if (c.Heave != null && lean > 0f) PmBlend(c, c.Heave, .75f + .03f * Mathf.Sin(t * 17f), lean * .7f);
            // They turn to face it as it lands, and step back a hand's width from the lurch.
            var toIt = monster - c.Feet; toIt.y = 0f;
            float faceIt = toIt.sqrMagnitude > .01f ? Mathf.Atan2(toIt.x, toIt.z) * Mathf.Rad2Deg : c.Yaw0;
            float yaw = Mathf.LerpAngle(c.Yaw0, faceIt, Ease(PhLandAt - .02f, PhLandAt + .2f, t)) + 3f * Mathf.Sin(t * 13f) * lean;
            var feet = c.Feet - (toIt.sqrMagnitude > .01f ? toIt.normalized : Vector3.zero) * .2f * lean;
            var turn = Quaternion.Euler(0f, yaw, 0f);
            c.Model.localPosition = feet + turn * c.ModelOffset;
            c.Model.localRotation = turn * c.ModelTilt;
        }

        private static void PmBlend(PmTarget c, AnimationClip clip, float time, float weight)
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

        // ------------------------------------------------------------------ the cameras

        /// <summary>THE CIRCLE, THE DESCENT and THE PUPPET are computed from where things are (scene space); `ShotAt` hands their
        /// storyboard eye, look and lens to this.</summary>
        private void PhaisterFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (_performance == null || index < 0) return;
            float start = _performance.Shots[index].Start;
            if (start >= PhPuppetAt - .01f) PmPuppetCamera(t, ref eye, ref look, ref fov);
            else if (start >= PhDescentAt - .01f && start < PhLandAt - .01f)
            {
                // Its FACE first, coming out of the eye in close-up from just under the circle (Castorice's dragon filling the frame),
                // then the lens drops with it and backs off until the whole hanging body and its strings are in, looking up.
                Vector3 feetNow = PhMonsterFeet(t);
                Vector3 face = feetNow + Vector3.up * (_phMonsterHeight * .8f);
                float u = Ease(PhDescentAt + .35f, PhLandAt - .1f, t);
                Vector3 closeEye = face + new Vector3(1.6f, -.9f, 2.4f);
                Vector3 wideEye = PhDollSpot + new Vector3(3.9f, .6f, 3.4f);
                eye = Vector3.Lerp(closeEye, wideEye, u);
                look = Vector3.Lerp(face, feetNow + Vector3.up * (_phMonsterHeight * .6f), u);
                fov = Mathf.Lerp(46f, 62f, u);
            }
            else if (start >= PhCircleAt - .01f && start < PhDescentAt - .01f)
            {
                // Tilting up from the doll in her hands to the circle, and holding the eye as the doll is drawn up into it.
                float u = Ease(PhCircleAt, 2.0f, t);
                look = Vector3.Lerp(PhOffered, PhCircleCentre, u);
            }
        }

        /// <summary>Where the staged opponents are from the doll, flat (its forward when nobody is staged).</summary>
        private Vector3 PmToward()
        {
            if (_pmTargets.Count == 0) return Vector3.forward;
            Vector3 sum = Vector3.zero;
            foreach (var c in _pmTargets) sum += c.Feet;
            var d = sum / _pmTargets.Count - PhDollEnd; d.y = 0f;
            return d.sqrMagnitude > .25f ? d.normalized : Vector3.forward;
        }

        /// <summary>How far its head turns to them: the signed angle from its forward (her forward) to them, as far as a neck goes
        /// (+ is to its right, the raw head convention).</summary>
        private float PmLookYaw() => Mathf.Clamp(Vector3.SignedAngle(Vector3.forward, PmToward(), Vector3.up), -60f, 60f);

        /// <summary>
        /// THE PUPPET: in on its face as it snaps up (from the opponents' side, so it lurches at the lens), then back until it, her and
        /// every opponent staged are in frame.
        /// </summary>
        private void PmPuppetCamera(float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            // THE PUPPET: in on its face as its head snaps up and it lurches at the lens (from the opponents' side), then down and back
            // to the ending the references share (Castorice's last frames): the caster small in front, smirking, the summon LOOMING
            // over her, eyes burning, the circle above, the real opponents' shoulders in the foreground.
            Vector3 doll = PhMonsterFeet(t);
            Vector3 toward = PmToward();
            Vector3 side = Vector3.Cross(Vector3.up, toward);
            Vector3 face = doll + Vector3.up * (_phMonsterHeight * .8f);
            float u = Ease(PhPuppetAt + .5f, Seconds - .25f, t);
            // ⚠️ In on its FACE. It stands facing her way (play picks up there) and only its head turns to them, so the lens sits between
            // its front and its look; v5 put the lens on the opponents' side, which was behind its ear whenever they stood off its shoulder.
            Vector3 faceDir = Quaternion.Euler(0f, PmLookYaw() * .6f, 0f) * Vector3.forward;
            Vector3 closeEye = face + faceDir * 2.3f + Vector3.Cross(Vector3.up, faceDir) * .4f;
            // Low (her eye line), in front of her and a little to her left, so she is nearer the lens than the doll beside her.
            Vector3 her = Vector3.up * 1.2f;
            Vector3 lowEye = her + toward * 3.6f - side * 1.5f + Vector3.down * .5f;
            eye = Vector3.Lerp(closeEye, lowEye, u);
            look = Vector3.Lerp(face, (face + her) * .5f + Vector3.up * .8f, u);
            fov = Mathf.Lerp(40f, 60f, u);
            foreach (var c in _pmTargets)
            {
                var chest = c.Feet + Vector3.up * 1.0f;
                var away = eye - chest;
                if (away.sqrMagnitude < PmCameraGap * PmCameraGap) eye = chest + (away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.up) * PmCameraGap;
            }
        }
    }
}
