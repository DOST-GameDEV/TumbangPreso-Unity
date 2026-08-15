using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Screen-edge arrows for your own slipper and the tracked can, converted from
    /// `scripts/ui/offscreen_indicators.gd`.
    ///
    /// `Dev_Plan.md` §3.3 calls these **mandatory** for FPP: a Person's camera has a much
    /// narrower awareness cone than a Prop's TPP, and this is the mitigation.
    ///
    /// ⚠️ THE FIRST ARROW POINTS AT **YOUR OWN SLIPPER**. 🧑 2026-08-01: *"A dynamic UI arrow
    /// floats around the Attacker's feet pointing directly toward their uncollected slipper."*
    /// It used to point at a teammate; the pivot deleted teams. It is only well-defined
    /// because slippers have owners now — under the old any-attacker-may-take-any rule there
    /// was no such thing as "your" slipper.
    ///
    /// Nothing is drawn while you are holding it: an arrow pointing at your own hand is noise.
    ///
    /// Driven from the HUD, which already resolves the local character once a frame. It takes
    /// that as a parameter rather than scanning for it a second time.
    /// </summary>
    public sealed class OffscreenIndicators : MonoBehaviour
    {
        /// <summary>Keeps the arrow's own size inside the true screen edge rather than
        /// clipping half of it off-frame.</summary>
        public const float EdgeMargin = 40.0f;

        /// <summary>Roughly chest height, so the arrow points at "the unit" rather than at
        /// whatever happens to be at its feet.</summary>
        public static readonly Vector3 TargetHeightOffset = new Vector3(0.0f, 0.5f, 0.0f);

        /// <summary>INK outline on the glyphs. These arrows live on the screen EDGE, which is
        /// where this game's backgrounds are least predictable: sky one frame, asphalt the
        /// next, a lit facade after that. A flat glyph is legible against roughly half of
        /// that; an outline makes it legible against all of it.</summary>
        public const int GlyphOutline = 6;

        private RectTransform _canvasRect;
        private RectTransform _slipperArrow;
        private RectTransform _canArrow;

        private void Awake() => Build();

        private void Build()
        {
            var canvasGo = new GameObject("OffscreenCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _canvasRect = canvasGo.GetComponent<RectTransform>();

            _slipperArrow = MakeArrow(canvasGo.transform, "SlipperArrow", UiTheme.Ink);
            _canArrow = MakeArrow(canvasGo.transform, "CanArrow", UiTheme.Highlight);
        }

        private static RectTransform MakeArrow(Transform parent, string name, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(64.0f, 64.0f);

            // Anchored to the centre so the placement maths below is in one frame of
            // reference: an offset from screen centre, exactly as the .gd computes it.
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

            var label = go.GetComponent<Text>();
            label.text = "▲";                     // the glyph points UP at rotation 0
            label.font = MenuKit.Font;
            label.fontSize = 48;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = tint;
            label.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = UiTheme.Ink;
            outline.effectDistance = new Vector2(GlyphOutline * 0.5f, -GlyphOutline * 0.5f);

            go.SetActive(false);
            return rect;
        }

        /// <summary>Called once a frame by the HUD with the already-resolved local unit.</summary>
        public void UpdateArrows(CharacterMotor local, Carrier carrier, Transform ownSlipper,
            Transform can)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || local == null)
            {
                _slipperArrow.gameObject.SetActive(false);
                _canArrow.gameObject.SetActive(false);
                return;
            }

            // An arrow pointing at your own hand is noise.
            bool holding = carrier != null && carrier.Held != null;
            UpdateOne(_slipperArrow, cam, holding ? null : ownSlipper);
            UpdateOne(_canArrow, cam, can);
        }

        private void UpdateOne(RectTransform arrow, UnityEngine.Camera cam, Transform target)
        {
            // ⚠️ THE NULL TEST IS A LIVENESS TEST, NOT JUST A REFERENCE TEST. Godot needed
            // `is_inside_tree()` because a unit mid-free stayed a valid object for a frame.
            // Unity's fake-null covers a destroyed object, but an INACTIVE one still has a
            // readable transform and must not draw an arrow either.
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            Vector3 worldPos = target.position + TargetHeightOffset;
            Vector3 toTarget = worldPos - cam.transform.position;

            // The projection divides by a plane distance that hits zero when a target sits
            // exactly perpendicular to the camera's forward axis, or sits on the lens itself.
            // Both are edge cases with nothing meaningful to point at anyway.
            float forward = Vector3.Dot(cam.transform.forward, toTarget);
            if (toTarget.magnitude < 0.1f || Mathf.Abs(forward) < 0.05f)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            bool isBehind = forward < 0.0f;
            Vector3 screen = cam.WorldToScreenPoint(worldPos);

            var viewport = new Vector2(Screen.width, Screen.height);
            var screenPos = new Vector2(screen.x, screen.y);

            // ⚠️ A BEHIND-CAMERA POINT COMES BACK MIRRORED THROUGH THE FRAME CENTRE rather
            // than flagged. Undo that, or the arrow points the long way around to the target.
            if (isBehind) screenPos = viewport - screenPos;

            Vector2 centre = viewport * 0.5f;

            bool onScreen = !isBehind
                            && screenPos.x >= 0.0f && screenPos.x <= viewport.x
                            && screenPos.y >= 0.0f && screenPos.y <= viewport.y;

            if (onScreen)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            arrow.gameObject.SetActive(true);

            Vector2 dir = screenPos - centre;
            if (dir.magnitude < 0.01f) dir = Vector2.up;   // dead centre behind: pick an edge
            dir.Normalize();

            // Push out to whichever screen edge is hit first, inside the margin.
            Vector2 half = centre - Vector2.one * EdgeMargin;
            float scaleX = Mathf.Abs(dir.x) > 0.0001f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
            float scaleY = Mathf.Abs(dir.y) > 0.0001f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
            float t = Mathf.Min(scaleX, scaleY);

            arrow.anchoredPosition = dir * t;

            // The glyph points up at rotation 0, and atan2 is measured from +X, so it needs
            // the quarter turn to line up with `dir`.
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90.0f;
            arrow.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
        }
    }
}
