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
    /// ⚠️ IN THE TAGGABLE COLOUR, NOT THE TAYA'S (2026-10-23): the frame says "you can be caught",
    /// so `TumpHudEffects` paints it in the player's Taggable colour (`Settings.TagColours`, blue by
    /// default), the same as the fill on their body; crimson `UiTheme.Defense` is the taya. It is thin (7
    /// canvas units), 70 percent opaque, and STILL: it fades in over 0.12 s and never pulses,
    /// so Reduce UI motion changes nothing about it and it cannot become a strobe during a long
    /// retrieval. It is a state, not a pulse, which is `Hud.PopHitmarker`'s lesson the other way
    /// round: this one really is on for as long as the state lasts.
    ///
    /// The taya gets four red corner strikes while the can is down. Their shape distinguishes
    /// the can cue from the attacker's continuous taggable frame, with the court clear in between.
    ///
    /// ⚠️ THE CORNERS ARE ROUNDED so the frame reads as a border of the view rather than as a
    /// rectangle drawn on it, and a black keel inside keeps it visible over blue sky.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudDangerFrame : MaskableGraphic
    {
        public float Width = 7;
        public float Radius = 22;
        private bool _canDown;
        private float _impact;

        internal void SetCanDownPose(bool canDown, float impact)
        {
            if (_canDown == canDown && _impact == impact) return;
            _canDown = canDown;
            _impact = impact;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (_canDown)
            {
                DrawCanDownCorners(vh, r);
                return;
            }
            var keel = new Color(0, 0, 0, color.a * .55f);
            HudDraw.RoundedFrame(vh, HudDraw.Inset(r, Width - 1), Radius - Width + 1, 3, keel, 8);
            HudDraw.RoundedFrame(vh, r, Radius, Width, color, 8);
        }

        private void DrawCanDownCorners(VertexHelper vh, Rect r)
        {
            float scale = Mathf.Min(r.width, r.height) / 720f;
            float inset = (9 + 14 * _impact) * scale;
            float reach = (118 + 38 * _impact) * scale;
            float height = reach * .62f;
            float weight = (8 + 5 * _impact) * scale;
            var keel = new Color(.09f, .02f, .025f, color.a * .92f);
            var glint = new Color(1, .55f, .43f, color.a * .85f);

            for (int i = 0; i < 4; i++)
            {
                var inward = new Vector2(i % 2 == 0 ? 1 : -1, i < 2 ? 1 : -1);
                var edge = new Vector2(inward.x > 0 ? r.xMin : r.xMax, inward.y > 0 ? r.yMin : r.yMax);
                var corner = edge + inward * inset;
                float outline = 3 * scale;
                Strike(vh, corner - inward * outline, inward, reach + outline * 2,
                    height + outline * 2, weight + outline * 2, keel);
                Strike(vh, corner, inward, reach, height, weight, color);
                Strike(vh, corner + inward * (2 * scale), inward, reach * .4f,
                    height * .4f, 2 * scale, glint);
            }
        }

        private static void Strike(VertexHelper vh, Vector2 origin, Vector2 inward,
            float reach, float height, float weight, Color tint)
        {
            // Both arms share a miter. Each slanted tip tapers toward the outside edge.
            int first = vh.currentVertCount;
            vh.AddVert(origin, tint, Vector2.zero);
            vh.AddVert(origin + new Vector2(inward.x * reach, 0), tint, Vector2.zero);
            vh.AddVert(origin + Vector2.Scale(inward, new Vector2(reach - weight, weight * .45f)), tint, Vector2.zero);
            vh.AddVert(origin + inward * weight, tint, Vector2.zero);
            vh.AddVert(origin + Vector2.Scale(inward, new Vector2(weight * .45f, height - weight)), tint, Vector2.zero);
            vh.AddVert(origin + new Vector2(0, inward.y * height), tint, Vector2.zero);
            for (int i = 1; i < 5; i++) vh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
