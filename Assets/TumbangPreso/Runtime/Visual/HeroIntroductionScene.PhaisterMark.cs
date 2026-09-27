using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PHAISTER, OMEN: THE THROW AND THE MARK (HERO-10 v8, 2026-09-27; `docs/HERO_KIT_METHOD.md` section 6, "the ending must
        // show what the ultimate DOES"; direction.md section 3).
        //
        // THE THROW (2.40 to 3.20): one camera from her RIGHT and a little behind, far enough out to hold her and the spot she aimed
        // at in one frame, so the eye crosses it LEFT TO RIGHT (the direction every shot of hers flows), easing toward the landing.
        //
        // THE MARK (3.20 to 5.00): what OMEN does to people is take them, and its rules give them 2.2 s of cast to run first, so the
        // cutscene cannot show the pull without play showing it again. What it CAN show is the omen itself: the black butterfly
        // (*paru-parong itim*) is the Visayan sign that someone will be taken. One peels off the maelstrom to every REAL player in
        // its reach and settles over their head; each flinches and turns to the eye and leans away from its draw. They are the real
        // ones: copies of exactly the players the eye will pull, by the live rule on the accepted aim (everyone but her within
        // `VoodooRules.HigopRadius` of the spot), their own rigs, skins and outfits (`MatchPoseHistory.Track.Clone`, render data
        // only), animated with clips their rigs already carry (`RootedMotion`: the break-out's flung-open frame for the flinch, the
        // heave for leaning against the draw). They stay where they really stand, so play picks up with them there, the marks
        // over their heads (`PhaisterOmen` starts its marks perched).
        // One crane: from low beside the eye, rising and turning CLOCKWISE round it, ending high with every one of them, the eye and
        // her in frame (pushed back until they fit), never nearer than 2.3 m to a body (Paete's films r20 to r22).
        // Nobody in reach: the maelstrom turns round an empty ring and the crane ends on her and the eye. Nobody is invented.
        // ⚠️ Posed from the scene clock only (the world is paused). No `Random`: every offset is typed.
        // =========================================================================================

        private sealed class PmTarget
        {
            public MatchPoseHistory.Copy Body;
            public GameObject Holder;
            public Transform Model, AnimRoot, Mark;
            public Transform[] MarkWings;
            public Vector3 ModelOffset, Feet;
            public Quaternion ModelTilt;
            public float Yaw0, Top, LandAt;
            public Transform[] Bones;
            public Quaternion[] RestRot, TmpRot;
            public Vector3[] RestPos, TmpPos;
            public AnimationClip Shock, Heave;
        }

        private readonly List<PmTarget> _pmTargets = new List<PmTarget>(3);
        // The marks' flights, typed: (delay after the first, arc height m, circle radius over the head m, wing Hz).
        private static readonly Vector4[] PmMarkRows =
        {
            new Vector4(0f, .9f, .26f, 3.6f), new Vector4(.10f, 1.1f, .22f, 4.2f), new Vector4(.18f, .8f, .30f, 3.2f),
        };
        private const float PmFlight = .38f, PmCameraGap = 2.3f;

        private void BuildPhaisterMark()
        {
            var round = GameServices.Round;
            if (round == null || _source == null) return;
            var landWorld = _root.transform.TransformPoint(_phGround);
            foreach (var p in round.Players)
            {
                if (p == null || p == _source || p.PlayerSlot == _source.PlayerSlot || _pmTargets.Count >= PmMarkRows.Length) continue;
                var d = p.transform.position - landWorld; d.y = 0f;
                if (d.magnitude > Core.VoodooRules.HigopRadius) continue;
                var target = PmCopy(p);
                if (target == null) continue;
                target.LandAt = PhMarkAt + .15f + PmMarkRows[_pmTargets.Count].x + PmFlight;
                _pmTargets.Add(target);
            }
        }

        /// <summary>A render copy of a player it will take, standing where they stand, with its rig's clips and its mark.</summary>
        private PmTarget PmCopy(CharacterMotor p)
        {
            var visual = p.GetComponent<CharacterVisual>();
            if (visual == null || visual.Model == null) return null;
            var track = new MatchPoseHistory.Track(p, visual.Model);
            track.Record(Time.time); track.Record(Time.time + .05f);
            var holder = new GameObject("OmenMarked-P" + (p.PlayerSlot + 1));
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
            t.RestRot = new Quaternion[n]; t.TmpRot = new Quaternion[n]; t.RestPos = new Vector3[n]; t.TmpPos = new Vector3[n];
            for (int i = 0; i < n; i++) { t.RestRot[i] = t.Bones[i].localRotation; t.RestPos[i] = t.Bones[i].localPosition; }
            // The mark: one of her black butterflies.
            var b = PhaisterProp.Spawn("butterfly", _root.transform, null, PhaisterProp.InsectOutlineWidth);
            if (b != null)
            {
                SetLayer(b, _root.layer);
                t.Mark = b.transform;
                t.MarkWings = new[] { PhaisterProp.Find(b, "wing-l"), PhaisterProp.Find(b, "wing-r") };
                b.SetActive(false);
            }
            // ⚠️ Hidden by switching the holder off, not by `forceRenderingOff` (the scene turns every renderer under it on for each
            // capture: Paete's trap). They stand in THE MARK only; the earlier shots are about her.
            holder.SetActive(false);
            return t;
        }

        private void SamplePhaisterMark(float t, float leave)
        {
            bool on = t >= PhMarkAt - .005f;
            Vector3 eye = OmenEyeAt(t);
            for (int i = 0; i < _pmTargets.Count; i++)
            {
                var c = _pmTargets[i];
                if (c.Holder.activeSelf != on) c.Holder.SetActive(on);
                if (c.Mark != null && c.Mark.gameObject.activeSelf != on) c.Mark.gameObject.SetActive(on);
                if (!on) continue;
                PmPose(c, t, eye);
                PmMark(c, i, t, eye, leave);
            }
        }

        private void PmPose(PmTarget c, float t, Vector3 eye)
        {
            // Back to the pose they were in, then the flinch and the lean laid over it by weight.
            for (int i = 1; i < c.Bones.Length; i++) { c.Bones[i].localRotation = c.RestRot[i]; c.Bones[i].localPosition = c.RestPos[i]; }
            float flinch = Ease(c.LandAt - .03f, c.LandAt + .08f, t) * (1f - .6f * Ease(c.LandAt + .25f, c.LandAt + .5f, t));
            float lean = Ease(c.LandAt + .2f, c.LandAt + .5f, t);
            if (c.Shock != null && flinch > 0f) PmBlend(c, c.Shock, .12f + .012f * Mathf.Sin(t * 41f), flinch);
            // Leaning back against the draw, the heave held near its hardest frame, trembling.
            if (c.Heave != null && lean > 0f) PmBlend(c, c.Heave, Mathf.Lerp(.6f, .9f, Ease(c.LandAt + .2f, 5f, t)) + .03f * Mathf.Sin(t * 17f + c.LandAt * 9f), lean * .85f);
            // They turn to face the eye as it marks them; the draw drags their feet a hand's width toward it.
            var toEye = eye - c.Feet; toEye.y = 0f;
            float faceEye = toEye.sqrMagnitude > .01f ? Mathf.Atan2(toEye.x, toEye.z) * Mathf.Rad2Deg : c.Yaw0;
            float yaw = Mathf.LerpAngle(c.Yaw0, faceEye, Ease(c.LandAt - .02f, c.LandAt + .18f, t)) + 3f * Mathf.Sin(t * 13f + c.LandAt) * lean;
            var feet = c.Feet + (toEye.sqrMagnitude > .01f ? toEye.normalized : Vector3.zero) * .16f * Ease(c.LandAt + .2f, 5f, t);
            feet.y += .06f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - c.LandAt) / .2f));
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

        /// <summary>The mark: out of the eye on an arc, over their head, then circling slowly clockwise just above it.</summary>
        private void PmMark(PmTarget c, int i, float t, Vector3 eye, float leave)
        {
            if (c.Mark == null) return;
            var row = PmMarkRows[i];
            float leaveAt = c.LandAt - PmFlight;
            float u = Mathf.Clamp01((t - leaveAt) / PmFlight);
            Vector3 head = c.Feet + Vector3.up * (c.Top + .25f);
            float a = (40f * i - 110f * Mathf.Max(0f, t - c.LandAt)) * Mathf.Deg2Rad;
            Vector3 perch = head + new Vector3(Mathf.Sin(a), .04f * Mathf.Sin(t * 4f + i), Mathf.Cos(a)) * row.z;
            Vector3 p; Vector3 heading;
            if (t < leaveAt) { p = eye; heading = Vector3.forward; }
            else if (u < 1f)
            {
                p = Vector3.Lerp(eye, perch, u * (2f - u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * row.y;
                heading = perch - eye;
            }
            else { p = perch; heading = new Vector3(-Mathf.Cos(a), 0f, Mathf.Sin(a)); }
            // v2 (film v10: at 2.3 across the wide crane they were specks): the mark is the point of the shot.
            float size = (t < leaveAt ? 0f : Mathf.Clamp01(u * 4f)) * 2.1f * 1.6f * leave;
            c.Mark.localPosition = p;
            if (heading.sqrMagnitude > .0001f) c.Mark.localRotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
            c.Mark.localScale = Vector3.one * Mathf.Max(.0001f, size);
            c.Mark.gameObject.SetActive(size > .01f);
            float open = 10f + 60f * (.5f + .5f * Mathf.Sin(t * row.w * Mathf.PI * 2f + i));
            if (c.MarkWings[0] != null) c.MarkWings[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
            if (c.MarkWings[1] != null) c.MarkWings[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
        }

        // ------------------------------------------------------------------ the cameras

        /// <summary>THE THROW and THE MARK are computed from where she aimed and where they stand (scene space). The storyboard rows
        /// in `phaister()` are the fallback; `ShotAt` hands their eye, look and lens to this.</summary>
        private void PhaisterFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (_performance == null || index < 0) return;
            float start = _performance.Shots[index].Start;
            if (start >= PhMarkAt - .01f) PmMarkCamera(t, ref eye, ref look, ref fov);
            else if (start >= PhThrowAt - .1f) PmThrowCamera(t, ref eye, ref look, ref fov);
        }

        private void PmThrowCamera(float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            Vector3 from = OmenPalms, to = _phLand;
            Vector3 path = to - from; path.y = 0f;
            float len = Mathf.Max(2f, path.magnitude);
            Vector3 along = path.sqrMagnitude > .01f ? path.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, along);
            Vector3 mid = (from + to) * .5f;
            fov = 54f;
            float halfW = Mathf.Atan(Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * 16f / 9f);
            float dist = (len * .5f + 1.6f) / Mathf.Tan(halfW);
            // From her right and a little behind her, the eye crossing left to right; easing a metre toward the landing.
            float push = Ease(PhThrowAt - .1f, PhMarkAt, t);
            eye = mid + right * dist - along * (len * .22f) + Vector3.up * .5f + along * push * 1.0f;
            look = Vector3.Lerp(mid, to, push * .35f) + Vector3.up * .15f;
        }

        private void PmMarkCamera(float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            Vector3 g = _phGround;
            Vector3 toHer = -g; toHer.y = 0f;
            float herAngle = Mathf.Atan2(toHer.x, toHer.z) * Mathf.Rad2Deg;
            float u = Ease(PhMarkAt, 4.85f, t);
            // Clockwise seen from above: the angle falls. It starts with her off to one side of the eye and ends high above the ring.
            float a = (herAngle + 62f - 78f * u) * Mathf.Deg2Rad;
            float r = Mathf.Lerp(5.2f, 7.8f, u), h = Mathf.Lerp(1.2f, 5.4f, u * u * (3f - 2f * u));
            eye = g + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r);
            look = Vector3.Lerp(_phLand, g + Vector3.up * 1.1f, u);
            fov = Mathf.Lerp(52f, 58f, u);
            // Everyone it marks, the eye and her, in frame: back the lens out along its own line until they fit.
            var points = new List<Vector3>(_pmTargets.Count * 2 + 3) { _phLand, Vector3.up * (1.2f + LiftAt(t)) };
            foreach (var c in _pmTargets) { points.Add(c.Feet); points.Add(c.Feet + Vector3.up * (c.Top + .45f)); }
            Vector3 back = (eye - look).normalized;
            var inverse = Quaternion.Inverse(Quaternion.LookRotation(-back, Vector3.up));
            float vertical = Mathf.Tan(fov * Mathf.Deg2Rad * .5f) * .82f, horizontal = vertical * 16f / 9f;
            float distance = Vector3.Distance(eye, look);
            foreach (var pt in points)
            {
                Vector3 view = inverse * (pt - look);
                distance = Mathf.Max(distance, Mathf.Abs(view.x) / horizontal - view.z + .2f, Mathf.Abs(view.y) / vertical - view.z + .2f);
            }
            eye = look + back * Mathf.Min(distance, 16f);
            // Never within 2.3 m of a body.
            foreach (var c in _pmTargets)
            {
                var chest = c.Feet + Vector3.up * 1.0f;
                var away = eye - chest;
                if (away.sqrMagnitude < PmCameraGap * PmCameraGap) eye = chest + (away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.up) * PmCameraGap;
            }
        }

        /// <summary>The lens at <paramref name="t"/> in the scene's space (the authored or computed shot, no shake).</summary>
        private bool PhLens(float t, out Vector3 eye)
        {
            eye = Vector3.zero;
            if (_performance == null) return false;
            int shot = _performance.ShotIndexAt(t);
            if (shot < 0) return false;
            _performance.Shot(shot, t, out eye, out var look, out float fov);
            PhaisterFrame(shot, t, ref eye, ref look, ref fov);
            return true;
        }

        // ------------------------------------------------------------------ the impact frame and the grade

        private Material _phImpact;

        /// <summary>
        /// ⚠️ THE IMPACT FRAME (research: Seele 10.6 s), the two frames as the eye lands: the whole finished picture turned inside
        /// out into two tones with her magenta ink splashing out of the eye (`Shaders/PhaisterImpact`). v7 put a dark disc behind the
        /// eye, which is a thing in the world; this is the camera flinching. Reduced effects keeps the landing and drops the frames.
        /// </summary>
        private void PhaisterPostProcess(RenderTexture frame, Camera camera, float t)
        {
            if (_reducedEffects || t < PhLandAt || t > PhLandAt + .085f) return;
            if (_phImpact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/PhaisterImpact");
                if (shader == null) return;
                _phImpact = new Material(shader) { name = "PhaisterImpact" };
            }
            var vp = camera.WorldToViewportPoint(_root.transform.TransformPoint(_phLand));
            _phImpact.SetVector("_Focus", new Vector4(vp.x, vp.y, 0f, 0f));
            _phImpact.SetFloat("_Amount", t < PhLandAt + .045f ? 1f : .75f);
            _phImpact.SetFloat("_Seed", t < PhLandAt + .045f ? 0f : 1f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _phImpact);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>Her night steps the world back (with the night walls): deepest through THE EYE, eased for THE MARK so the
        /// players read, released at the hand-back.</summary>
        private void PhaisterGrade(float t, out float brightness, out float saturation)
        {
            float away = Ease(0f, .3f, t) * (1f - .35f * Ease(PhMarkAt, PhMarkAt + .4f, t)) * (1f - Ease(Seconds - .16f, Seconds, t));
            brightness = 1f - .30f * away;
            saturation = 1f - .20f * away;
        }
    }
}
