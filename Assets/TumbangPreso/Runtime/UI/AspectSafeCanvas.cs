using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// One rule for every canvas in the game: an aspect ratio the layout was not authored for
    /// may add empty room, but it may never cut the layout off.
    ///
    /// ⚠️⚠️ MATCH-ON-HEIGHT CROPS ANYTHING NARROWER THAN 16:9, AND THAT IS THE HALF NOBODY
    /// LOOKS AT. Every canvas here is authored against 1920x1080 and every one of them was
    /// built with `matchWidthOrHeight = 1.0`, which is match-on-HEIGHT. On a 1024x768 monitor
    /// that gives a scale of 768/1080 = 0.711, so the canvas is only 1024/0.711 = 1440
    /// reference units wide against a layout that is 1920 wide: 480 units, a quarter of the
    /// screen, is off the edge. The banner, the arrows and the READY button all live out
    /// there. 1920x1200 fails the same way for 192 units.
    ///
    /// ⚠️ `Expand` IS EXACTLY MATCH-ON-HEIGHT AT EVERY 16:9 AND WIDER RESOLUTION, so nothing
    /// that already read correctly moves. It takes the SMALLER of the two axis scales rather
    /// than the height one:
    ///
    ///     1280x720   min(0.667, 0.667) = 0.667   identical to before
    ///     2560x1440  min(1.333, 1.333) = 1.333   identical to before
    ///     2560x1080  min(1.333, 1.000) = 1.000   identical to before, 21:9 gains side room
    ///     1920x1200  min(1.000, 1.111) = 1.000   was 1.111 and cropped 192 units of width
    ///     1024x768   min(0.533, 0.711) = 0.533   was 0.711 and cropped 480 units of width
    ///
    /// So this is not a retune of the 16:9 look. It is the guarantee that the 16:9 look is
    /// the whole of what a 4:3 or 16:10 player sees, scaled down rather than clipped.
    ///
    /// ⚠️ IT IS APPLIED AT RUNTIME AS WELL AS BY THE IMPORTER. The converted screens are
    /// committed `.unity` assets whose scaler was serialised by an earlier importer run, so
    /// fixing `TscnUiImporter` alone fixes nothing that already shipped. `ConvertedScreen`
    /// calls this on the canvas above it, which reaches every imported screen without
    /// regenerating twenty-one scenes.
    /// </summary>
    public static class AspectSafeCanvas
    {
        /// <summary>The one resolution every screen in this game is authored against.</summary>
        public static readonly Vector2 Reference = new Vector2(1920.0f, 1080.0f);

        public static void Apply(CanvasScaler scaler)
        {
            if (scaler == null) return;
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return;

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        /// <summary>Finds the scaler above (or on) a component and applies the rule.</summary>
        public static void ApplyToParentOf(Component anything)
        {
            if (anything == null) return;
            Apply(anything.GetComponentInParent<CanvasScaler>());
        }

        /// <summary>
        /// The reference-space size a canvas ends up with at a given pixel resolution. Used by
        /// the aspect probes so the claim being asserted is arithmetic rather than a picture.
        /// </summary>
        public static Vector2 ReferenceSizeAt(int pixelWidth, int pixelHeight)
        {
            float scale = Mathf.Min(pixelWidth / Reference.x, pixelHeight / Reference.y);
            if (scale <= 0.0f) return Vector2.zero;

            return new Vector2(pixelWidth / scale, pixelHeight / scale);
        }
    }
}
