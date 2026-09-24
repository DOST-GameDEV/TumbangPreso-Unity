using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ EVERY HERO CAST MOVES YOUR HANDS THROUGH THE SCREEN.
    ///
    /// 🧑 2026-09-24: *"and the animation of all skill casting"*, in both views. Filmed from the owner's eyes
    /// (`CastAndMotionReel`), all 21 casts barely moved the hands: the bespoke first-person clips only turn
    /// the forearms a little, the carry pose holds the slipper hand where it was, and the off hand stays
    /// half a screen below the bottom edge. Casting an ultimate looked like standing still.
    ///
    /// Each cast now has a gesture that says what the skill does, laid over its clip on the clip's own clock:
    /// a THRUST (force sent out), a RAISE (something called up), a SLAM (driven into the ground), a SWEEP
    /// (drawn across), a PULL (brought back), a SPREAD (burst outward) or a GLIDE (the body going somewhere).
    /// Both hands take part, so the off hand comes into view. Rise to the contact, HOLD, recover: the same
    /// shape as the body's casts (`HeroAbilityClips.ClipBuilder.HoldAt`) and the throw. Positions only; the
    /// ability's timing is untouched, and reduced motion turns it off. Keyed by the FIRST-PERSON action each
    /// ability sends (`HeroAbility.ViewmodelAction`), not the body clip's name.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private enum Gesture { Thrust, Raise, Slam, Sweep, Pull, Spread, Glide, Coven }

        private readonly struct CastShape
        {
            public readonly Gesture Kind; public readonly float Contact, Hold, End;
            public CastShape(Gesture kind, float contact, float hold, float end) { Kind = kind; Contact = contact; Hold = hold; End = end; }
        }

        private static readonly Dictionary<string, CastShape> CastShapes = new Dictionary<string, CastShape>
        {
            { "thrust-fire", new CastShape(Gesture.Thrust, .22f, .15f, .7f) },
            { "ignite", new CastShape(Gesture.Raise, .28f, .22f, .82f) },
            { "supernova-slam", new CastShape(Gesture.Slam, .65f, .2f, 1.2f) },
            { "sprint-electric", new CastShape(Gesture.Glide, .20f, .2f, .7f) },
            { "overcharge", new CastShape(Gesture.Pull, .30f, .18f, .73f) },
            { "summon-lightning", new CastShape(Gesture.Raise, .45f, .22f, .97f) },
            { "stomp-heavy", new CastShape(Gesture.Slam, .30f, .2f, .75f) },
            { "carapace-guard", new CastShape(Gesture.Spread, .32f, .28f, 1.08f) },
            { "fissure-slam", new CastShape(Gesture.Slam, .40f, .22f, .95f) },
            { "frost-sweep", new CastShape(Gesture.Sweep, .22f, .2f, .8f) },
            { "raise-barricade", new CastShape(Gesture.Raise, .30f, .22f, .82f) },
            { "nova-burst", new CastShape(Gesture.Spread, .32f, .25f, 1.1f) },
            { "ghost-step", new CastShape(Gesture.Glide, .22f, .16f, .71f) },
            { "project-spirit", new CastShape(Gesture.Thrust, .25f, .2f, .75f) },
            { "seance-channel", new CastShape(Gesture.Spread, .40f, .25f, 1.0f) },
            { "cast-hex", new CastShape(Gesture.Slam, .34f, .16f, .71f) },
            { "blink", new CastShape(Gesture.Pull, .12f, .12f, .54f) },
            // SKILL-FX-1: her own path, not the shared Raise (see `CovenPath`).
            { "coven-eclipse", new CastShape(Gesture.Coven, 1.55f, .21f, 2.12f) },
            { "current-cut", new CastShape(Gesture.Sweep, .18f, .18f, .76f) },
            { "mirror-feint", new CastShape(Gesture.Sweep, .14f, .12f, .78f) },
            { "breakwater-release", new CastShape(Gesture.Thrust, .55f, .2f, 1.25f) },
        };

        private bool _castApplied;
        private Vector3 _castRight, _castLeft;

        private void RestoreCastGesture()
        {
            if (!_castApplied) return;
            if (_rightPivot != null) _rightPivot.localPosition -= _castRight;
            if (_leftPivot != null) _leftPivot.localPosition -= _castLeft;
            _castRight = _castLeft = Vector3.zero; _castApplied = false;
        }

        private void ApplyCastGesture()
        {
            if (_rightPivot == null || _leftPivot == null || _clip == null || _actionName == null) return;
            if (!CastShapes.TryGetValue(_actionName, out var shape)) return;
            if (Settings.SettingsStore.Current.ReducedUiMotion) return;
            float t = _clipTime;
            if (t >= shape.End) return;
            // Rise to the contact (ease out), hold, recover.
            float rise = t < shape.Contact ? 1 - Mathf.Pow(1 - t / shape.Contact, 3) : 1;
            float fall = t > shape.Contact + shape.Hold ? Mathf.SmoothStep(1, 0, Mathf.InverseLerp(shape.Contact + shape.Hold, shape.End, t)) : 1;
            float w = rise * fall;
            if (shape.Kind == Gesture.Coven)
            {
                CovenPath(t, out var coven, out var covenLeft);
                _castRight = coven; _castLeft = covenLeft;
                _rightPivot.localPosition += coven; _leftPivot.localPosition += covenLeft;
                _castApplied = true;
                return;
            }
            // The first half of the rise, for gestures with an anticipation before their contact.
            float early = t < shape.Contact ? Mathf.Sin(Mathf.Clamp01(t / shape.Contact) * Mathf.PI) : 0;
            Vector3 r, l;
            switch (shape.Kind)
            {
                case Gesture.Thrust: r = new Vector3(-.10f, .06f, .22f) * w; l = new Vector3(.14f, .30f, .18f) * w; break;
                case Gesture.Raise: r = new Vector3(-.04f, .30f, .05f) * w; l = new Vector3(.10f, .52f, .05f) * w; break;
                case Gesture.Slam:
                    r = new Vector3(-.08f, -.10f, .16f) * w + new Vector3(0, .28f, -.04f) * early;
                    l = new Vector3(.12f, .20f, .16f) * w + new Vector3(0, .30f, -.04f) * early; break;
                case Gesture.Sweep:
                    r = new Vector3(-.30f, -.05f, .12f) * w + new Vector3(.12f, .10f, 0) * early;
                    l = new Vector3(.10f, .30f, .05f) * w; break;
                case Gesture.Pull:
                    r = new Vector3(.02f, 0, -.12f) * w + new Vector3(-.05f, .08f, .25f) * early;
                    l = new Vector3(.12f, .28f, .10f) * w; break;
                case Gesture.Spread: r = new Vector3(.14f, .18f, .08f) * w; l = new Vector3(-.02f, .46f, .08f) * w; break;
                default: r = new Vector3(.08f, -.05f, -.08f) * w; l = new Vector3(0, .20f, -.08f) * w; break;
            }
            _castRight = r; _castLeft = l;
            _rightPivot.localPosition += r; _leftPivot.localPosition += l;
            _castApplied = true;
        }

        // ⚠️⚠️ GRAND COVEN IN FIRST PERSON (SKILL-FX-1, 2026-09-24). 🧑: *"its cast/animation is awkward
        // and ugly"*. The shared Raise put both hands up and back, which says "summon" and not "draw a
        // circle". This is the body cast seen from her eyes, on the same clock: the right hand drops
        // to point low, SWEEPS across the bottom of the screen from right to left (drawing the ring
        // on the road), both hands lift into view palms up, then clench out and down on the close
        // (1.55 s, `RitualBuildSeconds`), hold, and recover by 2.12 s.
        private static readonly (float t, Vector3 r, Vector3 l)[] CovenKeys =
        {
            (0.00f, Vector3.zero, Vector3.zero),
            (0.36f, new Vector3(.14f, -.06f, .12f), new Vector3(.10f, .34f, .05f)),
            (0.80f, new Vector3(-.06f, -.07f, .14f), new Vector3(.10f, .34f, .05f)),
            (1.04f, new Vector3(-.28f, -.06f, .12f), new Vector3(.10f, .34f, .05f)),
            (1.26f, new Vector3(-.06f, .34f, .08f), new Vector3(.08f, .40f, .08f)),
            (1.55f, new Vector3(.10f, -.16f, .10f), new Vector3(-.10f, -.10f, .10f)),
            (1.76f, new Vector3(.09f, -.14f, .10f), new Vector3(-.09f, -.09f, .10f)),
            (2.12f, Vector3.zero, Vector3.zero),
        };

        private static void CovenPath(float t, out Vector3 right, out Vector3 left)
        {
            right = left = Vector3.zero;
            for (int i = 1; i < CovenKeys.Length; i++)
            {
                if (t > CovenKeys[i].t) continue;
                var a = CovenKeys[i - 1]; var b = CovenKeys[i];
                float u = Mathf.InverseLerp(a.t, b.t, t);
                // The close arrives fast and stops (the body's punch); everything else eases.
                u = Mathf.Approximately(b.t, 1.55f) ? u * u * u : u * u * (3 - 2 * u);
                right = Vector3.Lerp(a.r, b.r, u); left = Vector3.Lerp(a.l, b.l, u);
                return;
            }
        }
    }
}
