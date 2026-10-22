using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The camcorder frame every in-match replay wears (owner, 2026-10-22, with a mock-up): four
    /// white corner brackets, one red announcement plate at the top in the earned-moment banner's
    /// own surface (`MatchMomentBanner`), a blinking REC light at the bottom right and, on a replay
    /// the viewer may leave, one action prompt. It replaces every caption replays carried before.
    ///
    /// ⚠️ THE ANNOUNCEMENT IS ONE WORD, NOT THE EVENT: TAGGED on the victim's catch replay,
    /// HIGHLIGHT on halftime's. The halftime replay is a shared, host-timed package, so it has
    /// no prompt at all; only the tag replay, which is the victim's alone, can be skipped.
    /// </summary>
    public sealed class ReplayOverlay : MonoBehaviour
    {
        private CanvasGroup _group;
        private HudRing _light;
        private Text _prompt;
        private Image _promptGlyph;
        private RectTransform _promptRoot;
        private HudCard _promptPlate;
        private string _promptKey;
        private const float Inset = 108, Thick = 12, ArmWide = 289, ArmTall = 247;
        // ⚠️ IT MOVES (owner, October 22: "everything has to be animated"). The layout is the mock-up's;
        // only its arrival animates: the brackets close in from beyond the corners, and the plate slams
        // down oversized and tilted, then settles. A replay that keeps its plate for a beat (the tag
        // replay, until the touch) calls `HoldAnnouncement` and then `Slam`.
        private readonly System.Collections.Generic.List<(RectTransform rect, Vector2 home, Vector2 outward)> _bars =
            new System.Collections.Generic.List<(RectTransform, Vector2, Vector2)>();
        private RectTransform _plate;
        private CanvasGroup _plateGroup;
        private float _born, _slamAt;
        private bool _held;
        private const float BracketIn = .28f, SlamIn = .1f, SlamSettle = .16f;
        private static readonly Color RecordRed = new Color32(250, 42, 42, 255);

        public float Alpha { set { if (_group != null) _group.alpha = value; } }

        public static ReplayOverlay Build(Transform parent, string announcement, bool skippable)
        {
            var root = OwnerUiLayout.Rect(parent, "ReplayOverlay"); OwnerUiLayout.Fill(root);
            var overlay = root.gameObject.AddComponent<ReplayOverlay>();
            overlay._group = root.gameObject.AddComponent<CanvasGroup>();
            overlay._group.blocksRaycasts = false; overlay._group.interactable = false;
            for (int corner = 0; corner < 4; corner++) overlay.Bracket(root, corner);
            overlay.Announce(root, announcement);
            overlay._born = overlay._slamAt = Time.unscaledTime;
            overlay.Animate();
            overlay.Record(root);
            if (skippable) overlay.Prompt(root);
            return overlay;
        }

        /// <summary>One corner's two white bars, anchored to that corner of the screen.</summary>
        private void Bracket(RectTransform root, int corner)
        {
            var anchor = new Vector2(corner % 2, corner / 2);
            var inward = new Vector2(anchor.x == 0 ? 1 : -1, anchor.y == 0 ? 1 : -1);
            Vector2 origin = new Vector2(Inset * inward.x, Inset * inward.y);
            _bars.Add((Bar(root, "ReplayCorner" + corner + "Wide", anchor, origin, new Vector2(ArmWide * inward.x, Thick * inward.y)), origin, -inward));
            _bars.Add((Bar(root, "ReplayCorner" + corner + "Tall", anchor, origin, new Vector2(Thick * inward.x, ArmTall * inward.y)), origin, -inward));
        }
        private static RectTransform Bar(RectTransform root, string name, Vector2 anchor, Vector2 origin, Vector2 extent)
        {
            var bar = OwnerUiLayout.Rect(root, name).gameObject.AddComponent<Image>();
            bar.color = Color.white; bar.raycastTarget = false;
            var rect = bar.rectTransform; rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(extent.x > 0 ? 0 : 1, extent.y > 0 ? 0 : 1);
            rect.anchoredPosition = origin; rect.sizeDelta = new Vector2(Mathf.Abs(extent.x), Mathf.Abs(extent.y));
            return rect;
        }

        /// <summary>The red plate at the top: the earned-moment banner's glass surface and title face.</summary>
        private void Announce(RectTransform root, string words)
        {
            var plate = OwnerUiLayout.Rect(root, "ReplayAnnouncement");
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(.5f, 1);
            var card = plate.gameObject.AddComponent<HudCard>();
            card.Glass(HudDraw.Alarm, HudDraw.Lean, 3, .45f).raycastTarget = false; card.FollowContrast = false;
            var title = OwnerUiLayout.Text(plate, "ReplayAnnouncementTitle", words, 66, OwnerUiLayout.TypeRole.Display);
            title.alignment = TextAnchor.MiddleCenter; title.raycastTarget = false; title.supportRichText = false;
            title.color = HudDraw.Ivory; title.fontStyle = FontStyle.Italic;
            title.horizontalOverflow = HorizontalWrapMode.Overflow; title.verticalOverflow = VerticalWrapMode.Overflow;
            OwnerUiLayout.Fill(title.rectTransform);
            plate.anchoredPosition = new Vector2(0, -138);
            plate.sizeDelta = new Vector2(Mathf.Max(420, Mathf.Ceil(title.preferredWidth) + 120), 110);
            _plate = plate;
            _plateGroup = plate.gameObject.AddComponent<CanvasGroup>();
            _plateGroup.blocksRaycasts = false; _plateGroup.interactable = false;
        }

        /// <summary>Keep the announcement off screen until <see cref="Slam"/>.</summary>
        public void HoldAnnouncement() { _held = true; Animate(); }

        /// <summary>The announcement lands now: oversized and tilted, then settled in a fifth of a second.</summary>
        public void Slam() { _held = false; _slamAt = Time.unscaledTime; Animate(); }

        private void Animate()
        {
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            float now = Time.unscaledTime;
            float b = still ? 1 : Mathf.Clamp01((now - _born) / BracketIn);
            float ease = 1 - (1 - b) * (1 - b) * (1 - b);
            foreach (var bar in _bars)
                if (bar.rect != null) bar.rect.anchoredPosition = bar.home + bar.outward * (90 * (1 - ease));
            if (_plate == null) return;
            if (_held) { _plateGroup.alpha = 0; return; }
            float s = now - _slamAt;
            if (still) { _plateGroup.alpha = 1; _plate.localScale = Vector3.one; _plate.localRotation = Quaternion.identity; return; }
            float scale, tilt;
            if (s < SlamIn) { float u = s / SlamIn; scale = Mathf.Lerp(1.75f, .9f, u * u); tilt = Mathf.Lerp(-7, 1.5f, u); }
            else { float u = Mathf.Clamp01((s - SlamIn) / SlamSettle); float k = Mathf.SmoothStep(0, 1, u); scale = Mathf.Lerp(.9f, 1, k); tilt = Mathf.Lerp(1.5f, 0, k); }
            _plateGroup.alpha = Mathf.Clamp01(s / .05f);
            _plate.localScale = Vector3.one * scale;
            _plate.localRotation = Quaternion.Euler(0, 0, tilt);
        }

        /// <summary>REC and its light, at the bottom right inside the bracket.</summary>
        private void Record(RectTransform root)
        {
            var words = OwnerUiLayout.Text(root, "ReplayRecordLabel", "REC", 84, OwnerUiLayout.TypeRole.Reading);
            words.color = Color.white; words.alignment = TextAnchor.MiddleRight; words.raycastTarget = false;
            words.horizontalOverflow = HorizontalWrapMode.Overflow; words.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = words.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(1, .5f);
            rect.anchoredPosition = new Vector2(-248, 191); rect.sizeDelta = new Vector2(240, 96);
            // A ring as thick as its radius is a filled disc.
            _light = OwnerUiLayout.Rect(root, "ReplayRecordLight").gameObject.AddComponent<HudRing>();
            _light.raycastTarget = false; _light.Thickness = 38; _light.Track = Color.clear; _light.Paint(RecordRed, Color.clear);
            var dot = _light.rectTransform; dot.anchorMin = dot.anchorMax = new Vector2(1, 0); dot.pivot = new Vector2(.5f, .5f);
            dot.anchoredPosition = new Vector2(-193, 191); dot.sizeDelta = new Vector2(76, 76);
        }

        /// <summary>
        /// The match's own action prompt (`TumpMatchReadout.BuildCourtPrompts`, the one that says
        /// Retrieve Slipper and Reset Can): one strip of smoked glass, the live Interact glyph,
        /// then the verb in ivory. Its sizes are `SizePromptPlate`'s, so the two read as one family.
        /// </summary>
        private void Prompt(RectTransform root)
        {
            _promptRoot = OwnerUiLayout.Rect(root, "ReplayActionPrompt");
            _promptRoot.anchorMin = _promptRoot.anchorMax = _promptRoot.pivot = new Vector2(.5f, 0);
            _promptRoot.anchoredPosition = new Vector2(0, 282);
            _promptPlate = OwnerUiLayout.Rect(_promptRoot, "PromptPlate").gameObject.AddComponent<HudCard>();
            _promptPlate.Glass(HudDraw.GlassDeep).raycastTarget = false;
            OwnerUiLayout.Fill(_promptPlate.rectTransform);
            _promptGlyph = OwnerUiLayout.Rect(_promptRoot, "ActionBindingGlyph").gameObject.AddComponent<Image>();
            _promptGlyph.preserveAspect = true; _promptGlyph.raycastTarget = false;
            _prompt = OwnerUiLayout.Text(_promptRoot, "ActionPrompt", "", 38, OwnerUiLayout.TypeRole.Reading);
            _prompt.alignment = TextAnchor.MiddleLeft; _prompt.color = HudDraw.Ivory; _prompt.raycastTarget = false;
            _prompt.horizontalOverflow = HorizontalWrapMode.Overflow; _prompt.verticalOverflow = VerticalWrapMode.Overflow;
            var edge = _prompt.gameObject.AddComponent<Outline>(); edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(1.5f, -1.5f);
            Refresh();
        }
        /// <summary>Re-reads the binding when the device in hand changes, then fits the strip to it.</summary>
        private void Refresh()
        {
            if (_prompt == null) return;
            string key = Hud.OnTouch ? "" : Hud.KeyLabelFor("Interact");
            if (key == _promptKey) return;
            _promptKey = key;
            _promptGlyph.sprite = key.Length > 0 ? InputGlyphs.For(key, true) : null;
            _promptGlyph.enabled = _promptGlyph.sprite != null;
            bool glyph = _promptGlyph.enabled;
            _prompt.text = !Hud.OnTouch && !glyph ? Hud.PressCue("Interact") + "Skip Replay" : "Skip Replay";
            const float band = 70, size = 58, slack = 12;
            float glyphWidth = glyph ? InputGlyphs.PromptWidth(_promptGlyph.sprite, size) : 0;
            float lead = glyph ? 14 + glyphWidth + 14 : 28;
            float width = Mathf.Max(220, lead + Mathf.Ceil(_prompt.preferredWidth) + slack + 24);
            _promptRoot.sizeDelta = new Vector2(width, band);
            if (glyph) OwnerUiLayout.Place(_promptGlyph.rectTransform, 14, (band - size) * .5f, glyphWidth, size);
            OwnerUiLayout.Place(_prompt.rectTransform, lead, 0, width - lead - 20, band);
        }

        private void Update()
        {
            // A recording light: lit for most of each second, dark for the rest.
            if (_light != null) _light.enabled = Mathf.Repeat(Time.unscaledTime, 1) < .6f;
            Animate();
            Refresh();
            if (_prompt != null && Hud.OnTouch) InputLayer.TouchHud.Emphasise(Verb.Interact);
        }
    }
}
