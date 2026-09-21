using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The screen-edge arrow for the tracked can, converted from
    /// `scripts/ui/offscreen_indicators.gd`.
    ///
    /// `Dev_Plan.md` §3.3 calls these **mandatory** for FPP: a Person's camera has a much
    /// narrower awareness cone than a Prop's TPP, and this is the mitigation.
    ///
    /// ⚠️⚠️ THE SLIPPER ARROW WAS DELETED ON 2026-09-19 AND IT WAS REPLACED RATHER THAN DROPPED.
    /// 🧑 2026-08-01 asked for it in these words: *"A dynamic UI arrow floats around the
    /// Attacker's feet pointing directly toward their uncollected slipper."* That is still the
    /// feature and it is `SlipperRecall` now, which does strictly more: it sits ON the tsinelas
    /// while the tsinelas is on screen, clamps to this same edge with a chevron when it is not,
    /// and carries the control that fetches it. **The two could not both draw.** This arrow hid
    /// itself the moment the shoe came into frame and the mark appears there, so keeping both
    /// would have been two markers for one target with a seam in the middle, which is
    /// `CLAUDE.md` § 6.3's *"NEVER ADD A SECOND DOOR TO FIX A FINDABILITY PROBLEM"* built in.
    ///
    /// ⚠️ NOTHING ABOUT THE PROJECTION WAS DELETED WITH IT. `ScreenTrack` is the maths that used
    /// to live in `UpdateOne`, lifted out whole so the can arrow and the recall mark cannot drift
    /// apart, and its three measured corrections are recorded there.
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
        /// whatever happens to be at its feet. ⚠️ IT IS `ScreenTrack`'S NOW, not a second copy
        /// of the same number: two markers read it and `CLAUDE.md` § 5's rule about a number
        /// living in two places applies to a constant as much as to a balance value.</summary>
        public static Vector3 TargetHeightOffset => ScreenTrack.ChestHeight;

        /// <summary>INK outline on the glyphs. These arrows live on the screen EDGE, which is
        /// where this game's backgrounds are least predictable: sky one frame, asphalt the
        /// next, a lit facade after that. A flat glyph is legible against roughly half of
        /// that; an outline makes it legible against all of it.</summary>
        public const int GlyphOutline = 6;

        private RectTransform _canvasRect;
        private RectTransform _canArrow, _direction;
        private Text _markerState;
        private TumpSymbol _canSymbol;
        public bool CanMarkerVisible => _canArrow != null && _canArrow.gameObject.activeInHierarchy;
        public string CanMarkerState => _markerState != null ? _markerState.text : "";

        private void Awake() => BuildNative();

        private void BuildNative()
        {
            var canvas = TumpUiFactory.Canvas(transform, "TumpOffscreenCanvas", 110);
            _canvasRect = (RectTransform)canvas.transform;
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            _canArrow = NativeArrow(_canvasRect, "CanArrow", TumpUiTheme.Current.Yellow, "UI/portraits/" + Core.Roster.Cans[0].Id);
        }
        private RectTransform NativeArrow(Transform parent, string name, Color tint, string portrait)
        {
            var rect = TumpUiFactory.Rect(parent, name);
            TumpUiFactory.Anchor(rect, new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 52));
            _direction = TumpUiFactory.Rect(rect, "Direction");
            TumpUiFactory.Anchor(_direction, new Vector2(.5f, .5f), new Vector2(0, -16), new Vector2(22, 24));
            var pointer = _direction.gameObject.AddComponent<TumpTargetPointer>(); pointer.color = tint; pointer.raycastTarget = false;
            _canSymbol = TumpUiFactory.Rect(rect, "CanSymbol").gameObject.AddComponent<TumpSymbol>();
            _canSymbol.Kind = TumpSymbol.Icon.Can; _canSymbol.raycastTarget = false;
            TumpUiFactory.Anchor(_canSymbol.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 12), new Vector2(28, 32));
            var edge = _canSymbol.gameObject.AddComponent<Outline>();
            edge.effectColor = OwnerUiTheme.Current.DeepInk; edge.effectDistance = new Vector2(2, -2);
            _markerState = OwnerUiLayout.Text(rect, "ObjectiveState", "", 20, OwnerUiLayout.TypeRole.Reading);
            _markerState.alignment = TextAnchor.MiddleCenter; _markerState.raycastTarget = false;
            TumpUiFactory.Anchor(_markerState.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 43), new Vector2(144, 28));
            var textEdge = _markerState.gameObject.AddComponent<Outline>();
            textEdge.effectColor = OwnerUiTheme.Current.DeepInk; textEdge.effectDistance = new Vector2(1.5f, -1.5f);
            rect.gameObject.SetActive(false); return rect;
        }
        private void OnDisable() { if (_canvasRect != null) _canvasRect.gameObject.SetActive(false); }
        private void OnEnable() { if (_canvasRect != null) _canvasRect.gameObject.SetActive(true); }

        private void Build()
        {
            var canvasGo = new GameObject("OffscreenCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            AspectSafeCanvas.Apply(scaler);

            _canvasRect = canvasGo.GetComponent<RectTransform>();

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

        /// <summary>
        /// R-28 — the lata arrow takes the LOCAL player's ROLE colour.
        ///
        /// ⚠️ DRIVEN OFF THE LOCAL UNIT, NOT OFF WHICHEVER SIDE DEFENDS. Only the local
        /// character knows which side the person holding this keyboard is on, and this arrow and
        /// the crosshair are the two in-world markers that answer "what am I doing".
        /// </summary>
        public void SetCanArrowColour(Color colour)
        {
            if (_direction == null) return;
            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            var taya = GameServices.Match != null && GameServices.Round != null
                ? GameServices.Round.PlayerAt(GameServices.Match.DefenderSlot) : null;
            bool restoring = lata != null && !lata.IsUpright && taya != null &&
                             taya.GetComponent<Carrier>()?.ChannelRatio > 0;
            string state = lata == null ? "" : !lata.IsUpright ? (restoring ? "RESETTING" : "DOWN") :
                lata.IsProtected ? "CAN PROTECTED" : "";
            colour = lata != null && !lata.IsUpright ? OwnerUiTheme.Current.Lime : OwnerUiTheme.Current.Pale;
            _direction.GetComponent<TumpTargetPointer>().color = colour;
            if (_markerState != null) { _markerState.text = state; _markerState.color = colour; }
            if (_canSymbol != null)
            {
                _canSymbol.color = colour;
                _canSymbol.rectTransform.localRotation = Quaternion.Euler(0, 0, lata != null && !lata.IsUpright ? -65 : 0);
            }
        }

        /// <summary>
        /// Called once a frame by the HUD with the already-resolved local unit.
        ///
        /// ⚠️ THE TSINELAS IS NOT A PARAMETER ANY MORE. `SlipperRecall` owns that target now, on
        /// screen and off it, and the class note above records why it could not be both.
        /// </summary>
        public void UpdateArrows(CharacterMotor local, Transform can)
        {
            var cam = UnityEngine.Camera.main;
            if (PresentationClock.Held || cam == null || local == null)
            {
                _canArrow.gameObject.SetActive(false);
                return;
            }

            UpdateOne(_canArrow, cam, can);
        }

        /// <summary>
        /// ⚠️⚠️ THE PROJECTION IS `ScreenTrack`'S AND IS NO LONGER WRITTEN HERE. It carried three
        /// corrections that were each measured once (the mirrored behind-camera point, the push
        /// that has to be in canvas units rather than pixels, and the direction that must not be
        /// converted at all), and a second marker had to ask the same question. Copying them
        /// would have been `docs/TODO.md` § 94.1 again: two rules that happen to agree today.
        /// The reasoning moved with the code and is recorded in that file.
        ///
        /// ⚠️ THE MARGIN IS ZERO ON THE EDGE TEST AND `EdgeMargin` ON THE PUSH, WHICH IS WHAT
        /// THIS ARROW ALWAYS DID. It hides on any target inside the frame at all and only keeps
        /// its own body clear once it is clamped. The recall mark passes its own radius instead,
        /// because a ring straddling the edge is drawn half cut rather than simply hidden.
        /// </summary>
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

            // ⚠️ THE CAMERA'S OWN PIXELS, NOT `Screen`. They agree in play and disagree under a
            // render probe, and `ScreenTrack.Project` carries the reasoning.
            var viewport = new Vector2(cam.pixelWidth, cam.pixelHeight);
            Vector2 canvas = _canvasRect != null ? _canvasRect.rect.size : viewport;

            var track = ScreenTrack.Project(cam, target.position + Vector3.up * .85f,
                                            canvas, viewport, EdgeMargin + 26);
            if (!track.Visible) { arrow.gameObject.SetActive(false); return; }
            arrow.gameObject.SetActive(true);
            arrow.anchoredPosition = track.Anchored;
            // Only the arrow turns. Can silhouette and state remain readable at
            // every screen edge, and the on-screen marker sits above the aim point.
            _direction.localRotation = Quaternion.Euler(0, 0,
                track.Clamped ? track.Bearing * Mathf.Rad2Deg - 90 : 180);
            SetCanArrowColour(Color.white);
        }
    }
}
