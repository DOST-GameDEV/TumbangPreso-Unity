using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ EVERY HERO CAST MOVES YOUR HANDS THROUGH THE SCREEN, AND EACH CAST MOVES THEM ITS OWN WAY.
    ///
    /// 🧑 2026-09-24: *"and the animation of all skill casting"*, in both views. Filmed from the owner's eyes
    /// (`CastAndMotionReel`), all 21 casts barely moved the hands, so a gesture was laid over each clip. The
    /// first version drew those from SEVEN shared shapes (thrust, raise, slam, sweep, pull, spread, glide), so
    /// Ignition Cannon, Ice Barricade and Thunderstrike's call were one gesture, and so were Flame Rush, Astral
    /// Hijack and Breakwater. 🧑 the same day: *"do each animation one by one and dont js spam copy paste
    /// stuff"*, and SKILL-FX-1: *"refine all their skills VFX SFX and animation"*. **Each cast now has its own
    /// keyed path** (`CastPaths`), written from what its body clip does (`tools/author_hero_action.py`) and
    /// what the skill says (`docs/reports/skill-performances-2026-09-24/plan.md` section 3), with the hero's
    /// motion language on top:
    ///
    ///   Sean      forward, one axis, symmetric, a dead stop          Zack   sideways, bladed, snap then chatter
    ///   Dante     down and wide, heavy, slow recovery                Cheska one hand, one exact line, then HOLD
    ///   Nemu      floats, limbs lead, never snaps                    Phaister a flourish on the way, front-on hold
    ///   Rafi      the off hand, a sell and a cut back, a cupped release
    ///
    /// Offsets are added to the two arm pivots in the viewmodel's frame: +x right, +y up, +z away from the
    /// eye. The right hand holds the slipper; the left rests below the frame, so a left offset with y around
    /// +0.3 is what brings the off hand into view. Positions only; the clip's timing is untouched, the key at
    /// `Contact` is the cast's impact (reached with a hang-and-snap unless the path is `Floats`), and reduced
    /// motion turns every path off. Keyed by the FIRST-PERSON action each ability sends
    /// (`HeroAbility.ViewmodelAction`), not the body clip's name.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private readonly struct CastKey
        {
            public readonly float T; public readonly Vector3 R, L;
            public CastKey(float t, Vector3 r, Vector3 l) { T = t; R = r; L = l; }
        }

        private sealed class CastPath
        {
            public readonly float Contact; public readonly bool Floats; public readonly CastKey[] Keys;
            public CastPath(float contact, bool floats, params CastKey[] keys) { Contact = contact; Floats = floats; Keys = keys; }
            public float End => Keys[Keys.Length - 1].T;
        }

        private static CastKey K(float t, float rx, float ry, float rz, float lx, float ly, float lz)
            => new CastKey(t, new Vector3(rx, ry, rz), new Vector3(lx, ly, lz));

        private static CastKey Rest(float t) => new CastKey(t, Vector3.zero, Vector3.zero);

        private static readonly Dictionary<string, CastPath> CastPaths = new Dictionary<string, CastPath>
        {
            // ---------------------------------------------------------------- SEAN: forward, one axis
            // FLAME RUSH. A committed straight line: both hands rake back low (the body's sprinter set),
            // then the right drives straight down the middle and stops; the left stays pumped back.
            { "thrust-fire", new CastPath(.22f, false,
                Rest(0), K(.12f, .03f, -.10f, -.14f, .06f, .12f, -.10f),
                K(.22f, -.05f, .02f, .30f, .06f, .16f, -.12f), K(.37f, -.05f, .02f, .28f, .06f, .15f, -.10f), Rest(.70f)) },
            // IGNITION CANNON. Loading the throw: the slipper comes back to the shoulder and the free hand
            // comes across and cups over it, his lantern maker's care; held while it catches, then released.
            { "ignite", new CastPath(.28f, false,
                Rest(0), K(.14f, .04f, .08f, -.04f, .14f, .24f, .00f),
                K(.28f, .02f, .16f, -.10f, .30f, .40f, .02f), K(.50f, .02f, .15f, -.09f, .29f, .39f, .02f), Rest(.82f)) },
            // SUPERNOVA. Both hands go up out of the frame with the leap, hang, and come down together to
            // the bottom centre: the one symmetric slam in the kit, stopped dead.
            { "supernova-slam", new CastPath(.65f, false,
                Rest(0), K(.30f, -.04f, .40f, .02f, .08f, .62f, .02f), K(.50f, -.06f, .46f, .00f, .10f, .66f, .00f),
                K(.65f, -.14f, -.18f, .20f, .20f, .08f, .20f), K(.85f, -.13f, -.16f, .19f, .19f, .09f, .19f), Rest(1.2f)) },

            // ---------------------------------------------------------------- ZACK: sideways, bladed
            // BOLT SPRINT. A skater's arms: the right swings across and back, the left counters, twice,
            // quick and lateral. Locomotion, so no impact frame; `Contact` is the first push.
            { "sprint-electric", new CastPath(.12f, true,
                Rest(0), K(.12f, -.18f, .06f, .06f, -.06f, .16f, -.06f), K(.26f, .10f, -.04f, -.04f, .14f, .30f, .08f),
                K(.40f, -.16f, .05f, .05f, -.04f, .18f, -.04f), K(.54f, .06f, -.02f, -.02f, .08f, .22f, .04f), Rest(.70f)) },
            // MAGNET. The off hand aims out at the slipper, then the right snaps in to the chest as it
            // arrives, with one electrical chatter before it lets go.
            { "overcharge", new CastPath(.30f, false,
                Rest(0), K(.14f, .02f, .02f, .04f, -.12f, .42f, .22f), K(.22f, .03f, .02f, .06f, -.14f, .44f, .24f),
                K(.30f, -.08f, .10f, -.14f, -.06f, .36f, .10f), K(.36f, -.07f, .11f, -.13f, -.06f, .36f, .10f),
                K(.42f, -.09f, .10f, -.14f, -.06f, .35f, .10f), Rest(.73f)) },
            // THUNDERSTRIKE. One arm calls the sky and HOLDS ("hold to pick a spot"), then snaps down level
            // to point at it while the left is thrown back as the counterweight; chatter after.
            { "summon-lightning", new CastPath(.45f, false,
                Rest(0), K(.22f, .02f, .44f, .04f, -.04f, .20f, .00f), K(.36f, .02f, .50f, .04f, -.05f, .22f, .00f),
                K(.45f, -.08f, .10f, .30f, -.12f, .12f, -.14f), K(.52f, -.07f, .11f, .29f, -.12f, .12f, -.14f),
                K(.60f, -.08f, .10f, .30f, -.12f, .12f, -.14f), Rest(.97f)) },

            // ---------------------------------------------------------------- DANTE: down and wide
            // SEISMIC STOMP. Fists come up short, never overhead, then hammer down and OUT to the sides
            // with the stomp; a heavy hold and a slow recovery.
            { "stomp-heavy", new CastPath(.30f, false,
                Rest(0), K(.18f, .06f, .22f, .02f, -.02f, .42f, .02f),
                K(.30f, .12f, -.22f, .10f, -.14f, .02f, .10f), K(.50f, .11f, -.20f, .09f, -.13f, .03f, .09f), Rest(.75f)) },
            // DEMONIC CARAPACE. The flex: both fists draw in low, then out wide and up at the shoulders and
            // stay there, trembling once with the effort. He is bigger now.
            { "carapace-guard", new CastPath(.32f, false,
                Rest(0), K(.15f, -.08f, -.04f, .04f, .10f, .20f, .04f),
                K(.32f, .16f, .10f, .04f, -.16f, .32f, .04f), K(.44f, .17f, .10f, .04f, -.17f, .32f, .04f),
                K(.56f, .16f, .11f, .04f, -.16f, .33f, .04f), K(.70f, .16f, .10f, .04f, -.16f, .32f, .04f), Rest(1.08f)) },
            // TITAN FISSURE. Arms rise short of vertical and the weight hangs, then both drive down and
            // FORWARD into the road ahead, where the split will open.
            { "fissure-slam", new CastPath(.40f, false,
                Rest(0), K(.22f, .02f, .34f, .00f, .02f, .54f, .00f), K(.30f, .02f, .35f, .00f, .02f, .55f, .00f),
                K(.40f, -.06f, -.20f, .26f, .12f, .04f, .26f), K(.62f, -.05f, -.18f, .24f, .11f, .05f, .24f), Rest(.95f)) },

            // ---------------------------------------------------------------- CHESKA: one hand, exact
            // PERMAFROST SHEET. One flat pass of the right hand, low, right to left at one height (the
            // sheet's edge), and she holds it there. The left never moves: she spends one hand.
            { "frost-sweep", new CastPath(.22f, false,
                Rest(0), K(.10f, .12f, -.06f, .12f, 0, 0, 0),
                K(.22f, -.24f, -.06f, .14f, 0, 0, 0), K(.42f, -.24f, -.06f, .14f, 0, 0, 0), Rest(.80f)) },
            // ICE BARRICADE. The palm rises in one straight vertical line, drawing the wall up, and stops.
            { "raise-barricade", new CastPath(.30f, false,
                Rest(0), K(.08f, -.06f, -.12f, .14f, 0, 0, 0),
                K(.30f, -.06f, .22f, .14f, 0, 0, 0), K(.52f, -.06f, .22f, .14f, 0, 0, 0), Rest(.82f)) },
            // GLACIAL NOVA. The only time she uses both: they draw in to the centre (compression), open
            // flat to the sides in one exact beat, and hold the longest of anyone.
            { "nova-burst", new CastPath(.32f, false,
                Rest(0), K(.20f, -.14f, -.06f, .06f, .16f, .26f, .06f),
                K(.32f, .18f, .06f, .08f, -.18f, .34f, .08f), K(.57f, .18f, .06f, .08f, -.18f, .34f, .08f), Rest(1.1f)) },

            // ---------------------------------------------------------------- NEMU: floats, limbs lead
            // PHANTOM VEIL. The hands lift and trail back as if the body stopped being a body; no frame
            // stops, so the whole path eases.
            { "ghost-step", new CastPath(.22f, true,
                Rest(0), K(.10f, .04f, .08f, -.04f, -.04f, .26f, -.02f),
                K(.22f, .08f, .14f, -.10f, -.08f, .34f, -.08f), K(.40f, .06f, .12f, -.08f, -.06f, .30f, -.06f), Rest(.71f)) },
            // ASTRAL HIJACK. The spirit leaves: the right hand is flung out after it, forward and right,
            // and the left comes to her chest.
            { "project-spirit", new CastPath(.25f, true,
                Rest(0), K(.12f, .02f, .02f, .06f, .10f, .24f, .00f),
                K(.25f, .14f, .12f, .26f, .20f, .34f, -.02f), K(.45f, .12f, .10f, .22f, .20f, .33f, -.02f), Rest(.75f)) },
            // DEVOURING SEANCE. Arms wide as she rises, then dragged IN to the centre and down: the only
            // ultimate that collapses rather than strikes.
            { "seance-channel", new CastPath(.40f, true,
                Rest(0), K(.20f, .20f, .18f, .04f, -.20f, .40f, .04f), K(.30f, .21f, .19f, .04f, -.21f, .41f, .04f),
                K(.40f, -.12f, -.06f, .10f, .14f, .20f, .10f), K(.65f, -.11f, -.05f, .09f, .13f, .21f, .09f), Rest(1.0f)) },

            // ---------------------------------------------------------------- PHAISTER: the flourish
            // HEX. A small circle drawn with the fingertip, then a stamp down and forward at the chalk,
            // the left hand opened out for the audience.
            { "cast-hex", new CastPath(.34f, false,
                Rest(0), K(.08f, .06f, .06f, .12f, -.04f, .16f, .00f), K(.16f, -.04f, .12f, .12f, -.06f, .22f, .00f),
                K(.24f, -.08f, .02f, .12f, -.08f, .26f, .02f), K(.34f, -.02f, -.16f, .20f, -.08f, .26f, .02f),
                K(.50f, -.02f, -.15f, .19f, -.08f, .26f, .02f), Rest(.71f)) },
            // SHADOW BLINK. No flourish: a blink has no time for one. Collapse in, then thrown wide open.
            { "blink", new CastPath(.12f, false,
                Rest(0), K(.06f, -.10f, -.02f, -.06f, .12f, .20f, -.06f),
                K(.12f, .18f, .08f, .06f, -.18f, .30f, .06f), K(.24f, .17f, .08f, .06f, -.17f, .30f, .06f), Rest(.54f)) },
            // GRAND COVEN (SKILL-FX-1, 🧑: *"its cast/animation is awkward and ugly"*). The body cast seen
            // from her eyes: the right hand drops to point low and sweeps across the bottom of the screen
            // (drawing the ring on the road), both hands lift into view, then clench out and down on the
            // close (1.55 s, `RitualBuildSeconds`), hold, and recover by 2.12 s.
            { "coven-eclipse", new CastPath(1.55f, false,
                Rest(0), K(.36f, .14f, -.06f, .12f, .10f, .34f, .05f), K(.80f, -.06f, -.07f, .14f, .10f, .34f, .05f),
                K(1.04f, -.28f, -.06f, .12f, .10f, .34f, .05f), K(1.26f, -.06f, .34f, .08f, .08f, .40f, .08f),
                K(1.55f, .10f, -.16f, .10f, -.10f, -.10f, .10f), K(1.76f, .09f, -.14f, .10f, -.09f, -.09f, .10f), Rest(2.12f)) },

            // ---------------------------------------------------------------- RAFI: the tease
            // CROSSCURRENT. The off-hand cut: the LEFT slices across left to right and a little down,
            // the line the slipper will bend along, while the right curls the slipper in.
            { "current-cut", new CastPath(.18f, false,
                Rest(0), K(.08f, .04f, .02f, -.04f, -.08f, .32f, .08f),
                K(.18f, .02f, .04f, -.06f, .24f, .22f, .14f), K(.36f, .02f, .04f, -.06f, .23f, .22f, .13f), Rest(.76f)) },
            // MIRRORWAKE. A real feint: a big sell to the right, held a beat, then the cut back left.
            { "mirror-feint", new CastPath(.14f, false,
                Rest(0), K(.14f, .16f, .02f, .06f, .10f, .24f, .04f), K(.20f, .16f, .02f, .06f, .10f, .24f, .04f),
                K(.32f, -.12f, .02f, .04f, -.14f, .26f, .04f), K(.47f, -.04f, .00f, .02f, -.04f, .14f, .02f), Rest(.78f)) },
            // BREAKWATER. The cupped release: both hands gather low at the right hip, then sweep up and out
            // forward to the left in one arc, and hold while the wave goes.
            { "breakwater-release", new CastPath(.55f, false,
                Rest(0), K(.26f, .04f, -.12f, .02f, .34f, .14f, .02f), K(.40f, .03f, -.10f, .04f, .32f, .16f, .04f),
                K(.55f, -.18f, .14f, .26f, .02f, .40f, .26f), K(.76f, -.17f, .13f, .25f, .02f, .39f, .25f), Rest(1.25f)) },

            // ---------------------------------------------------------------- AMIHAN: the spiral
            // QUICK DASH. Both hands snap back low, then the OFF hand swings out and forward across
            // the frame from the left, leading her line; the slipper hand stays pulled back.
            { "gust-dash", new CastPath(.13f, false,
                Rest(0), K(.08f, .04f, -.08f, -.10f, .02f, .06f, -.10f),
                K(.13f, .02f, .00f, -.06f, -.12f, .36f, .26f), K(.30f, .02f, .00f, -.05f, -.11f, .34f, .25f), Rest(.62f)) },
            // UPDRAFT. Palms press down at the bottom of the frame, then both hands fly up out of it
            // with the lift, and come back to hang either side as she settles into the air.
            { "updraft-lift", new CastPath(.24f, false,
                Rest(0), K(.16f, .02f, -.20f, .04f, -.02f, .10f, .04f),
                K(.24f, .06f, .44f, .02f, -.06f, .62f, .02f), K(.46f, .08f, .32f, .02f, -.08f, .50f, .02f), Rest(.80f)) },
            // WHIRLWIND. Both hands wound up to the right, then one sweep across the front to the
            // left at shoulder height: the gale leaves them.
            { "gale-sweep", new CastPath(.32f, false,
                Rest(0), K(.20f, .18f, .02f, .02f, .26f, .22f, .02f),
                K(.32f, -.24f, .06f, .18f, -.20f, .34f, .18f), K(.52f, -.26f, .05f, .17f, -.22f, .33f, .17f), Rest(.90f)) },
            // STORM SURGE. Both palms forward and braced through the 2.5 s gather, pressing harder as
            // it builds, then one shove forward on the release (the key at 2.5 s is the contact).
            { "storm-call", new CastPath(2.5f, false,
                Rest(0), K(.25f, -.02f, .06f, .16f, .10f, .32f, .16f), K(1.2f, -.02f, .07f, .19f, .10f, .33f, .19f),
                K(2.36f, -.01f, .08f, .12f, .09f, .34f, .12f), K(2.5f, -.02f, .10f, .36f, .10f, .36f, .36f),
                K(2.66f, -.02f, .10f, .35f, .10f, .36f, .35f), Rest(2.85f)) },
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
            if (!CastPaths.TryGetValue(_actionName, out var path)) return;
            if (Settings.SettingsStore.Current.ReducedUiMotion) return;
            if (_clipTime >= path.End) return;
            Sample(path, _clipTime, out var r, out var l);
            _castRight = r; _castLeft = l;
            _rightPivot.localPosition += r; _leftPivot.localPosition += l;
            _castApplied = true;
        }

        /// <summary>
        /// The path at t. ⚠️ THE SEGMENT INTO THE CONTACT KEY HANGS AND SNAPS (cubic ease-in, the body clips'
        /// `SNAP` idea), and every other segment is smoothstep, so the impact is the fastest moment and the
        /// hand stops on it. A path that `Floats` (Nemu, Zack's skating) never snaps.
        /// </summary>
        private static void Sample(CastPath path, float t, out Vector3 right, out Vector3 left)
        {
            var keys = path.Keys;
            right = left = Vector3.zero;
            for (int i = 1; i < keys.Length; i++)
            {
                if (t > keys[i].T) continue;
                var a = keys[i - 1]; var b = keys[i];
                float u = Mathf.InverseLerp(a.T, b.T, t);
                u = !path.Floats && Mathf.Approximately(b.T, path.Contact) ? u * u * u : u * u * (3 - 2 * u);
                right = Vector3.Lerp(a.R, b.R, u); left = Vector3.Lerp(a.L, b.L, u);
                return;
            }
        }
    }
}
