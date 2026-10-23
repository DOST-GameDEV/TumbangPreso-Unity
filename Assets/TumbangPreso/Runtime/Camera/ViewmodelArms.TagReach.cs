using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ THE FIRST-PERSON TAG REACHES INTO THE SCREEN.
    ///
    /// 🧑 2026-09-24: *"make tagging better too"*. `PunchClip` and `LungeClip` only PITCH the arm in place, so
    /// from your own eyes a tag was the hand tipping forward a little. A tag is a reach for somebody: the hand
    /// now travels toward the crosshair and back (the jab), or out and HELD while the dash can still land
    /// (the lunge, whose clip note says the held arm is the truth about a live tag), with the off hand pulled
    /// back for balance. The body does the same (`CharacterAnimator.TagBody`). The tag itself is `CombatVerbs`'.
    ///
    /// 🧑 2026-10-23, v2: *"taya animation in FPP is weird (it feels like im js wobbing my arm, no reaching)"*. v1 slid the
    /// pivot 28 cm on top of the clip's pitch, and the frames (Logs/fpp-1023/before) showed why it read as a wobble: the
    /// elbow went forward with the hand, so the forearm kept its slant and only tipped. Now the forearm is AIMED: the fist
    /// is put on a point just under the crosshair, the forearm laid along the line from the shoulder to it, the elbow
    /// behind, so the arm straightens out of the bottom corner toward whoever is being tagged. The clip's own pitch is
    /// faded out under it, so the two never fight.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private bool _tagApplied;
        private Vector3 _tagRightPos, _tagLeftPos;
        private Quaternion _tagRightRot, _tagRightArm;

        /// <summary>Where the jab's fist lands, in the view model's space: just right of and under the crosshair.</summary>
        public static readonly Vector3 JabFist = new Vector3(.05f, -.03f, 1.2f);
        /// <summary>The dash tag's fist: a little lower and further, held while the sweep is live.</summary>
        public static readonly Vector3 LungeFist = new Vector3(.04f, -.08f, 1.26f);
        /// <summary>Where the right forearm comes from (off the bottom right of the screen), for the line it is laid along.</summary>
        private static readonly Vector3 RightShoulder = new Vector3(.46f, -.86f, -.05f);

        private void RestoreTagReach()
        {
            if (!_tagApplied) return;
            if (_rightPivot != null) { _rightPivot.localPosition = _tagRightPos; _rightPivot.localRotation = _tagRightRot; }
            if (_rightArm != null) _rightArm.localRotation = _tagRightArm;
            if (_leftPivot != null) _leftPivot.localPosition = _tagLeftPos;
            _tagApplied = false;
        }

        private void ApplyTagReach()
        {
            if (_rightPivot == null || _leftPivot == null || _rightArm == null || _clip == null) return;
            bool punch = ReferenceEquals(_clip, PunchClip), lunge = ReferenceEquals(_clip, LungeClip);
            if (!punch && !lunge) return;
            if (Settings.SettingsStore.Current.ReducedUiMotion) return;
            float t = _clipTime;
            // Jab: out in 0.09 s, a beat at full stretch, home by 0.34 s. Dash: out in 0.10 s, held while it can land.
            float w = lunge ? Reach(t, .10f, Core.Balance.LungeActiveTime, .62f) : Reach(t, .09f, .17f, .34f);
            if (w <= .001f) return;
            _tagRightPos = _rightPivot.localPosition; _tagRightRot = _rightPivot.localRotation; _tagRightArm = _rightArm.localRotation;
            _tagLeftPos = _leftPivot.localPosition;
            _tagApplied = true;

            AimForearm(_rightPivot, _rightArm, _rightRest, lunge ? LungeFist : JabFist, RightShoulder, w);
            // The off hand drops back and out of the way, as a body does to throw its weight behind the reach.
            _leftPivot.localPosition += new Vector3(.04f, -.12f, -.10f) * w;
        }

        /// <summary>
        /// Lays a forearm (its pivot is the elbow, the mesh runs <see cref="ArmLength"/> up its +Y) so the fist sits on
        /// <paramref name="fist"/>, along the line from <paramref name="shoulder"/>, blended in by <paramref name="w"/>. The turn
        /// is the smallest one from the arm's rest, so the knuckles keep facing the way they do at rest.
        /// </summary>
        private static void AimForearm(Transform pivot, Transform arm, Quaternion rest, Vector3 fist, Vector3 shoulder, float w, float roll = 0)
        {
            Vector3 along = (fist - shoulder).normalized;
            float length = ArmLength * pivot.localScale.y;
            var aimed = Quaternion.AngleAxis(roll, along) * Quaternion.FromToRotation(rest * Vector3.up, along) * rest;
            pivot.localPosition = Vector3.Lerp(pivot.localPosition, fist - along * length, w);
            pivot.localRotation = Quaternion.Slerp(pivot.localRotation, aimed, w);
            arm.localRotation = Quaternion.Slerp(arm.localRotation, Quaternion.identity, w);
        }

        /// <summary>0 to 1 to 0: a fast ease-out reach, a hold, an eased return.</summary>
        private static float Reach(float t, float reach, float hold, float end)
        {
            if (t < reach) { float u = t / reach; return 1 - (1 - u) * (1 - u) * (1 - u); }
            if (t < hold) return 1;
            return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(hold, end, t));
        }
    }
}
