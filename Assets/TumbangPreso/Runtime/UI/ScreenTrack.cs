using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// THE one answer to *"where on this canvas does a marker for that world point go"*.
    ///
    /// ⚠️⚠️ IT IS A SHARED STATIC BECAUSE THERE ARE TWO MARKERS AND THERE MUST NOT BE TWO
    /// PROJECTIONS. `OffscreenIndicators` drew the lata arrow and `SlipperRecall` draws the
    /// recall mark, and the maths below carries three separate corrections that were each paid
    /// for once and would have had to be paid for again in a copy. `docs/TODO.md` § 94.1 is what
    /// a second answer to one question costs this project, and `CombatVerbs.SlideMayStartFrom`
    /// makes the same argument one system over: *"it does not restate the pickup rule and must
    /// not start."*
    ///
    /// The three corrections, all of them measured rather than reasoned:
    ///
    /// ⚠️⚠️ **A BEHIND-CAMERA POINT COMES BACK MIRRORED THROUGH THE FRAME CENTRE** rather than
    /// flagged, so it has to be undone or the marker points the long way round to the target.
    ///
    /// ⚠️⚠️ **THE EDGE PUSH IS IN CANVAS UNITS AND NOT IN PIXELS**, and mixing them put the
    /// markers in the wrong place at every resolution but one. Everything that comes out of
    /// `WorldToScreenPoint` is real screen pixels; `anchoredPosition` is not, because this canvas
    /// runs a `CanvasScaler` against a 1920 x 1080 reference. In a 1280 x 720 window the old
    /// maths pushed to 600 units where the canvas edge is 920, so the markers hovered two thirds
    /// of the way out and read as floating decoration; wider than 1920 it overshot and pushed
    /// them off frame entirely, which loses the marker exactly when the target is furthest away.
    /// ⚠️ `AspectSafeCanvas` makes it worse rather than better: `ScreenMatchMode.Expand`
    /// deliberately lets the canvas grow WIDER than 1920 on anything narrower than 16:9, so the
    /// gap is not even a fixed ratio.
    ///
    /// ⚠️ **A NORMALISED DIRECTION NEEDS NO CONVERSION.** A `CanvasScaler` applies ONE uniform
    /// scale to both axes, so a unit vector is the same vector in pixels and in canvas units.
    /// Only magnitudes change frame of reference.
    /// </summary>
    public static class ScreenTrack
    {
        /// <summary>
        /// Roughly chest height, so a marker points at "the unit" rather than at whatever
        /// happens to be at its feet. It is the offset `OffscreenIndicators` shipped with and it
        /// is stated once here now that two markers use it.
        /// </summary>
        public static readonly Vector3 ChestHeight = new Vector3(0.0f, 0.5f, 0.0f);

        public readonly struct Result
        {
            /// <summary>False when there is nothing meaningful to point at at all.</summary>
            public readonly bool Visible;

            /// <summary>True when the target is off frame and the marker sits on the edge.</summary>
            public readonly bool Clamped;

            /// <summary>Centre-anchored canvas position for the marker.</summary>
            public readonly Vector2 Anchored;

            /// <summary>
            /// Radians from +X toward the target, canvas space. Only meaningful while
            /// <see cref="Clamped"/>: on screen the marker IS the target and there is nothing
            /// for a chevron to point at.
            /// </summary>
            public readonly float Bearing;

            public Result(bool visible, bool clamped, Vector2 anchored, float bearing)
            {
                Visible = visible;
                Clamped = clamped;
                Anchored = anchored;
                Bearing = bearing;
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE VIEWPORT IS THE CAMERA'S OWN PIXEL RECT AND NOT `Screen`, WHICH IS WHAT
        /// EVERY CALLER NOW PASSES. They are the same number while the camera draws to the back
        /// buffer, which is the whole of normal play, and they are NOT the same the moment a
        /// camera is given a target texture: `WorldToScreenPoint` answers in the target's pixels
        /// while `Screen` still reports the window. Every render probe in this repository works
        /// exactly that way, so a marker measured against `Screen` is measured against a frame
        /// that is not the one being photographed.
        ///
        /// ⚠️ IT IS A PARAMETER RATHER THAN READ HERE so a probe can ask this question about a
        /// shape the machine running it is not displaying. `AspectRatioProbes` drives nine of
        /// them and `InputSurfaceProbe` twelve.
        /// </summary>
        public static Result Project(Camera cam, Vector3 world, Vector2 canvas, Vector2 viewport,
                                     float margin)
        {
            if (cam == null || viewport.x <= 0.0f || viewport.y <= 0.0f
                || canvas.x <= 0.0f || canvas.y <= 0.0f)
                return new Result(false, false, Vector2.zero, 0.0f);

            Vector3 toTarget = world - cam.transform.position;

            // The projection divides by a plane distance that hits zero when a target sits
            // exactly perpendicular to the camera's forward axis, or sits on the lens itself.
            // Both are edge cases with nothing meaningful to point at anyway.
            float forward = Vector3.Dot(cam.transform.forward, toTarget);
            if (toTarget.magnitude < 0.1f || Mathf.Abs(forward) < 0.05f)
                return new Result(false, false, Vector2.zero, 0.0f);

            bool isBehind = forward < 0.0f;

            Vector3 screen = cam.WorldToScreenPoint(world);
            var screenPos = new Vector2(screen.x, screen.y);
            if (isBehind) screenPos = viewport - screenPos;

            // ⚠️⚠️ EVERYTHING BELOW THIS LINE IS IN CANVAS UNITS, AND THE VERSION THAT WAS NOT
            // PUT THE MARK ON THE WRONG EDGE OF A FRAME THE TARGET WAS SITTING IN THE MIDDLE OF.
            // `WorldToScreenPoint` answers in the camera's own pixels and `anchoredPosition` is in
            // canvas units, and the old shape of this function tested the EDGE in pixels while
            // pushing in canvas units, so the margin meant two different distances in one
            // expression. `SlipperRecallShots`' second frame is the receipt: the tsinelas was
            // dead centre and the ring was clamped to the top right corner.
            //
            // ⚠️ THE CONVERSION IS ONE SCALE PER AXIS AND THE TWO AGREE. A `CanvasScaler` applies
            // a single uniform factor, so `canvas / viewport` is the same number twice; doing it
            // component-wise costs nothing and stays correct if that ever stops being true.
            var perPixel = new Vector2(canvas.x / viewport.x, canvas.y / viewport.y);
            Vector2 at = Vector2.Scale(screenPos, perPixel);

            Vector2 centre = canvas * 0.5f;
            Vector2 dir = at - centre;

            bool onScreen = !isBehind
                            && at.x >= margin && at.x <= canvas.x - margin
                            && at.y >= margin && at.y <= canvas.y - margin;

            if (onScreen) return new Result(true, false, dir, 0.0f);

            if (dir.magnitude < 0.01f) dir = Vector2.up;   // dead centre behind: pick an edge
            dir.Normalize();

            Vector2 half = canvas * 0.5f - Vector2.one * margin;
            float scaleX = Mathf.Abs(dir.x) > 0.0001f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
            float scaleY = Mathf.Abs(dir.y) > 0.0001f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
            float t = Mathf.Min(scaleX, scaleY);

            return new Result(true, true, dir * t, Mathf.Atan2(dir.y, dir.x));
        }
    }
}
