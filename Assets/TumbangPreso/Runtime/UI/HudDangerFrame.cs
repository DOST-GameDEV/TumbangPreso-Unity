using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The screen-edge danger frame (TODO VISUAL-1.1, the HUD half).
    ///
    /// ⚠️⚠️ THE RULE THAT DEFINES THE GAME HAD NO PICTURE. An attacker is taggable while inside
    /// the box, holding their own slipper, with the lata upright (`CharacterMotor.IsTaggable`
    /// plus `Lata.IsUpright`). That was one orange sentence at the bottom prompt ("You can be
    /// tagged") and a 0.36 s edge flash at onset, so for the rest of a retrieval the player had
    /// to READ to know they were in danger. Knockout City answers the same question with a thin
    /// frame round the whole screen while you are targeted: no words, no darkening, and it is
    /// in the corner of the eye wherever the eye is.
    ///
    /// ⚠️ DEFENSE BLUE BECAUSE IT MEANS THE TAYA (`UiTheme.Defense`, `CLAUDE.md` § 6.4's one
    /// exemption). The frame says "the taya can catch you", in the taya's colour. It is thin (7
    /// canvas units), 70 percent opaque, and STILL: it fades in over 0.12 s and never pulses,
    /// so Reduce UI motion changes nothing about it and it cannot become a strobe during a long
    /// retrieval. It is a state, not a pulse, which is `Hud.PopHitmarker`'s lesson the other way
    /// round: this one really is on for as long as the state lasts.
    ///
    /// ⚠️ THE CORNERS ARE ROUNDED so the frame reads as a border of the view rather than as a
    /// rectangle drawn on it, and a black keel inside keeps it visible over blue sky.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudDangerFrame : MaskableGraphic
    {
        public float Width = 7;
        public float Radius = 22;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var keel = new Color(0, 0, 0, color.a * .55f);
            HudDraw.RoundedFrame(vh, HudDraw.Inset(r, Width - 1), Radius - Width + 1, 3, keel, 8);
            HudDraw.RoundedFrame(vh, r, Radius, Width, color, 8);
        }
    }
}
