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
    /// back for balance. The body does the same (`CharacterAnimator.TagBody`). Positions only, on the clips'
    /// own clock; the tag itself is `CombatVerbs`'.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private bool _tagApplied;
        private Vector3 _tagRight, _tagLeft;

        private void RestoreTagReach()
        {
            if (!_tagApplied) return;
            if (_rightPivot != null) _rightPivot.localPosition -= _tagRight;
            if (_leftPivot != null) _leftPivot.localPosition -= _tagLeft;
            _tagRight = _tagLeft = Vector3.zero; _tagApplied = false;
        }

        private void ApplyTagReach()
        {
            if (_rightPivot == null || _leftPivot == null || _clip == null) return;
            bool punch = ReferenceEquals(_clip, PunchClip), lunge = ReferenceEquals(_clip, LungeClip);
            if (!punch && !lunge) return;
            if (Settings.SettingsStore.Current.ReducedUiMotion) return;
            float t = _clipTime;
            float w = lunge ? Reach(t, .10f, .38f, .62f) : Reach(t, .12f, .16f, .34f);
            if (w <= .001f) return;
            _tagRight = (lunge ? new Vector3(-.13f, -.02f, .27f) : new Vector3(-.11f, .05f, .21f)) * w;
            _tagLeft = new Vector3(.03f, -.08f, -.07f) * w;
            _rightPivot.localPosition += _tagRight;
            _leftPivot.localPosition += _tagLeft;
            _tagApplied = true;
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
