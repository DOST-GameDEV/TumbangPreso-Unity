using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// UNSTOPPABLE's refusal, reworked: when a status bounces off him, the status bursts against his plate as a small
    /// painted puff in that status's own colour (the approved cloud puffs, three tones: lit, shade, line), and a few
    /// basalt chips knock off the plate and crumble on the court (Boulder's own rubble, reused as it is).
    ///
    /// ⚠️ WHY. The older splash threw flat shards of the status colour, which read as paper. A puff is a substance hitting
    /// stone and failing, and the chips say the stone held. Both pieces are GPU-driven already: the puff field is one
    /// shared draw posed in its shader, and the chips are posed from their age with nothing allocated per frame.
    ///
    /// ⚠️ OFF UNTIL THE OWNER TURNS IT ON. `Enabled` defaults to false, so `DanteStatusShatter.Refusal` is unchanged.
    /// The shields, the fitted armour, the cooling warning and the crumble are not touched by this file.
    /// </summary>
    public static class DanteRefusalPuff
    {
        /// <summary>Set true to draw the reworked refusal instead of the flat shards.</summary>
        public static bool Enabled = false;

        public static void Spawn(Vector3 at, Vector3 outward, StatusKind kind)
        {
            outward.y = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward;
            var colour = DanteStatusShatter.ColourOf(kind);
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            // ⚠️ VISIBILITY PASS (2026-10-18). The first film showed a pale wisp for 0.3 s that vanished at 4 m and from
            // the side: one puff at 0.55 scale, its lit tone washed nearly to white, which on a sunlit pale court is
            // the court's own colour. Now three puffs burst off the plate (one in front, two thrown to his sides so
            // the side view sees one), 0.6 s long, and the three values are pulled apart: the lit tone keeps most of
            // the colour, the shade is the colour at full saturation, the line is dark. Still under his shoulders, so
            // it never hides him.
            s = Mathf.Max(s, .45f);
            var lit = Color.HSVToRGB(h, s * .7f, Mathf.Min(1f, v * 1.12f + .08f));
            var shade = Color.HSVToRGB(h, Mathf.Min(1f, s * 1.15f), v * .78f);
            var line = Color.HSVToRGB(h, Mathf.Min(1f, s * 1.2f), v * .32f);
            var side = Vector3.Cross(Vector3.up, outward);
            float turn = (int)kind * 1.3f;
            CloudPuffs.Spawn(new CloudPuffField.Puff
            {
                At = at + outward * .14f, Scale = 1.05f, Life = .6f, Reach = .38f, Lift = .22f,
                Turn = turn, Share = 1f, Lit = lit, Shade = shade, Line = line,
            });
            for (int k = -1; k <= 1; k += 2)
                CloudPuffs.Spawn(new CloudPuffField.Puff
                {
                    At = at + outward * .06f + side * (k * .2f) + Vector3.up * (k * .06f), Scale = .75f, Life = .52f,
                    Reach = .32f, Lift = .18f, Turn = turn + k * 2.1f, Share = .85f, Lit = lit, Shade = shade, Line = line,
                });
            // Five dry chips knocked off the plate, larger, outward, falling and crumbling where they land.
            DanteStatusShatter.Rubble(at, outward, 5, 1.3f, true);
        }
    }
}
