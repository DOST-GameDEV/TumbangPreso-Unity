using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ THE FIRST-PERSON THROW MOVES THROUGH THE SCREEN.
    ///
    /// 🧑 2026-09-24: *"refine other gameplay animations too for both FPP and TPP view ... throwing of
    /// slippers or pektus"*. Filmed before this pass (`GameplayActionShots`), the charge only tilted the
    /// slipper hand a few degrees in place and the off hand never moved, so a full wind-up, a left pektus and
    /// a right pektus looked the same from your own eyes. First person has only the hands to say it
    /// (`docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md` § 1.5), so:
    ///
    ///   * CHARGE: the slipper hand draws BACK, up and toward the right edge as the charge builds; for pektus
    ///     it drops LOW and WIDE (the sidearm plane) and the wrist ROLLS, palm up for right spin and palm down
    ///     for left, so the slipper itself shows the curve you are about to throw. The off hand rises into
    ///     view pointing at the crosshair: the aiming hand.
    ///   * RELEASE (`ApplyReleaseSweep`): the sweep's finish follows the throw. Straight exits low left of
    ///     centre, right pektus finishes OPEN low right, left pektus sweeps CLOSED across to the low left.
    ///
    /// Positions only move the pivots, so the carried slipper stays in the grip; the charge's aim offset and
    /// timing are untouched. Reduced motion turns all of it off, like the release sweep.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private bool _reachApplied;
        private Vector3 _reachRight, _reachLeft;
        private Quaternion _reachRightRotBase, _reachLeftRotBase;

        private void RestoreThrowReach()
        {
            if (!_reachApplied) return;
            if (_rightPivot != null) { _rightPivot.localPosition -= _reachRight; _rightPivot.localRotation = _reachRightRotBase; }
            if (_leftPivot != null) { _leftPivot.localPosition -= _reachLeft; _leftPivot.localRotation = _reachLeftRotBase; }
            _reachRight = _reachLeft = Vector3.zero; _reachApplied = false;
        }

        private void ApplyThrowReach()
        {
            if (!_carrying || _charge < 0 || _rightPivot == null || _leftPivot == null) return;
            if (Settings.SettingsStore.Current.ReducedUiMotion) return;
            float weight = Mathf.Clamp01(WorldCueProfile.Current.ViewmodelFraming);
            float p = (1 - Mathf.Exp(-4.5f * Mathf.Clamp01(_charge))) / (1 - Mathf.Exp(-4.5f)) * weight;
            float spin = _chargeSpin, side = Mathf.Abs(spin);
            // Overhand: back, up, to the right edge. Sidearm: low and wide.
            // ⚠️ MEASURED ON SCREEN, NOT GUESSED (`GameplayActionShots` logs hands.csv). The existing charge
            // cocks the forearm back and pushes the elbow AWAY to keep the hand still, so at full charge the
            // slipper shrank and went end-on. The draw-back therefore pulls the whole arm clearly CLOSER and up.
            // The off hand started 0.5 of a screen below the bottom edge; it needs this much to be seen.
            var over = new Vector3(.08f, .12f, -.26f);
            // The left roll turns the hand down out of frame, so the left sidearm sits higher (the fifth film).
            var sidearm = spin < 0 ? new Vector3(.10f, .06f, -.16f) : new Vector3(.14f, -.05f, -.14f);
            _reachRight = Vector3.Lerp(over, sidearm, side) * p;
            // Held at full, the hand trembles by a few millimetres: a loaded throw you can feel (🧑 2026-09-24,
            // *"make ALL animations and actions ... look satisfying"*).
            float full = Mathf.InverseLerp(.93f, 1f, Mathf.Clamp01(_charge)) * weight;
            if (full > 0) _reachRight += new Vector3(Mathf.Sin(Time.time * 83f), Mathf.Sin(Time.time * 71f + 1.3f), 0) * .004f * full;
            _reachLeft = new Vector3(.14f, .24f, -.04f) * p;
            _reachRightRotBase = _rightPivot.localRotation; _reachLeftRotBase = _leftPivot.localRotation;
            _rightPivot.localPosition += _reachRight;
            _leftPivot.localPosition += _reachLeft;
            // The pektus roll about the arm, and the off hand pitched up to point where you are aiming.
            // 35 degrees, not 50: at 50 the left roll turned the hand out of the bottom of the frame (the fourth film).
            _rightPivot.localRotation = Quaternion.AngleAxis(-35f * spin * p, Vector3.forward) * _rightPivot.localRotation;
            _leftPivot.localRotation = Quaternion.AngleAxis(-28f * p, Vector3.right) * _leftPivot.localRotation;
            // Counter most of the old forearm cock on the straight throw, on the RAW charge because the cock grows
            // linearly with it: at full charge it turned the slipper
            // end-on to the lens and it shrank to a sliver (the second film). Pektus keeps its roll instead.
            _rightPivot.localRotation = Quaternion.AngleAxis(-30f * Mathf.Clamp01(_charge) * weight * (1 - side), Vector3.right) * _rightPivot.localRotation;
            _reachApplied = true;
        }

        /// <summary>The release sweep eases out PAST its finish by about 12 per cent and settles: the whip.</summary>
        private static float SweepWhip(float u) { u = Mathf.Clamp01(u); const float c = 1.4f; return 1 + (c + 1) * Mathf.Pow(u - 1, 3) + c * Mathf.Pow(u - 1, 2); }

        /// <summary>Where the release sweep finishes, by the throw's spin: straight low left, right pektus open low right, left pektus closed low left.</summary>
        private static Vector3 ReleaseFollow(float spin)
        {
            var straight = new Vector3(-.46f, -.16f, .07f);
            if (spin > .05f) return Vector3.Lerp(straight, new Vector3(.18f, -.22f, .10f), spin);
            if (spin < -.05f) return Vector3.Lerp(straight, new Vector3(-.60f, -.20f, .05f), -spin);
            return straight;
        }
    }
}
