using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// CAN DOWN, stamped in the thrower's seat colour, with the screen's edges pulsing the same colour.
    ///
    /// Owner, 2026-10-22: the knockdown is the attackers' payoff and it has to read on every screen
    /// ("everything has to be animated", anticipation, impact, aftermath). Driven by the replicated
    /// `MatchFlair.Kind.LataDown`, so every peer and spectator gets the same beat; it decides nothing.
    ///   * anticipation: the plate drops in oversized and tilted;
    ///   * impact: it slams to size, the edges flash and the can glyph kicks;
    ///   * aftermath: it holds, then lifts away and fades while the edges breathe out.
    /// One at a time: a second knockdown restarts it in the new thrower's colour.
    /// </summary>
    public sealed class CanDownStamp : MonoBehaviour
    {
        private const float SlamIn = .11f, Settle = .18f, Hold = .75f, Exit = .24f;
        private const float Life = SlamIn + Settle + Hold + Exit;
        private const float EdgeThickness = 30f;
        private static CanDownStamp _live;

        private RectTransform _plate, _glyph;
        private CanvasGroup _plateGroup;
        private readonly Image[] _edges = new Image[4];
        private Color _seat;
        private float _born;

        public static void Play(int throwerSlot)
        {
            var round = GameServices.Round;
            var owner = round != null && round.Lata != null ? round.Lata.transform : null;
            if (owner == null || !round.RoundActive) return;
            if (_live != null) Destroy(_live.gameObject);
            var canvas = OwnerUiLayout.Canvas(owner, "~CanDownStamp", 170);
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
            _live = canvas.gameObject.AddComponent<CanDownStamp>();
            _live.Build((RectTransform)canvas.transform, PlayerIdentity.Colour(throwerSlot));
        }

        private void Build(RectTransform root, Color seat)
        {
            _seat = seat; _born = Time.unscaledTime;
            for (int i = 0; i < 4; i++)
            {
                var edge = OwnerUiLayout.Rect(root, "CanDownEdge" + i).gameObject.AddComponent<Image>();
                edge.raycastTarget = false;
                var r = edge.rectTransform;
                bool vertical = i >= 2;
                r.anchorMin = new Vector2(i == 3 ? 1 : 0, i == 1 ? 1 : 0);
                r.anchorMax = new Vector2(i == 2 ? 0 : 1, i == 0 ? 0 : 1);
                r.pivot = new Vector2(i == 3 ? 1 : 0, i == 1 ? 1 : 0);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = vertical ? new Vector2(EdgeThickness, 0) : new Vector2(0, EdgeThickness);
                _edges[i] = edge;
            }

            _plate = OwnerUiLayout.Rect(root, "CanDownPlate");
            _plate.anchorMin = _plate.anchorMax = _plate.pivot = new Vector2(.5f, .5f);
            _plate.anchoredPosition = new Vector2(0, 250);
            var card = _plate.gameObject.AddComponent<HudCard>();
            card.Glass(seat, HudDraw.Lean, 3, .45f).raycastTarget = false; card.FollowContrast = false;
            var title = OwnerUiLayout.Text(_plate, "CanDownTitle", "CAN DOWN", 64, OwnerUiLayout.TypeRole.Display);
            title.alignment = TextAnchor.MiddleCenter; title.raycastTarget = false; title.supportRichText = false;
            title.color = HudDraw.Ivory; title.fontStyle = FontStyle.Italic;
            title.horizontalOverflow = HorizontalWrapMode.Overflow; title.verticalOverflow = VerticalWrapMode.Overflow;
            var ink = title.gameObject.AddComponent<Outline>(); ink.effectColor = UiTheme.InGameOutline; ink.effectDistance = new Vector2(3, -3);
            float width = Mathf.Ceil(title.preferredWidth);
            _plate.sizeDelta = new Vector2(width + 190, 104);
            title.rectTransform.anchorMin = new Vector2(0, 0); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(110, 0); title.rectTransform.offsetMax = new Vector2(-40, 0);

            var glyph = OwnerUiLayout.Rect(_plate, "CanDownGlyph").gameObject.AddComponent<HudBadge>();
            glyph.raycastTarget = false; glyph.Show(HudBadge.Glyph.CanDown, HudDraw.Ivory, Color.clear);
            _glyph = glyph.rectTransform;
            _glyph.anchorMin = _glyph.anchorMax = new Vector2(0, .5f); _glyph.pivot = new Vector2(.5f, .5f);
            _glyph.sizeDelta = new Vector2(70, 70); _glyph.anchoredPosition = new Vector2(70, 0);

            _plateGroup = _plate.gameObject.AddComponent<CanvasGroup>();
            _plateGroup.blocksRaycasts = false; _plateGroup.interactable = false;
            Animate();
        }

        private void Update()
        {
            var round = GameServices.Round;
            if (Time.unscaledTime - _born >= Life || round == null || !round.RoundActive) { Destroy(gameObject); return; }
            Animate();
        }

        private void OnDestroy() { if (_live == this) _live = null; }

        private void Animate()
        {
            float t = Time.unscaledTime - _born;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            float flash = Settings.SettingsStore.Current.EffectiveFlashIntensity;

            // The edges: a hard flash on the slam, two quick breaths, gone with the plate.
            float edge = t < SlamIn ? 0 : Mathf.Exp(-(t - SlamIn) * 4.5f) * (.75f + .25f * Mathf.Cos((t - SlamIn) * 18f));
            edge *= .55f * flash;
            foreach (var e in _edges) { var c = _seat; c.a = edge; e.color = c; }

            float scale = 1, tilt = 0, rise = 0, alpha = 1, kick = 0;
            float exitAt = SlamIn + Settle + Hold;
            if (still) alpha = t < .12f ? t / .12f : t > exitAt ? 1 - (t - exitAt) / Exit : 1;
            else if (t < SlamIn) { float u = t / SlamIn; scale = Mathf.Lerp(1.8f, .88f, u * u); tilt = Mathf.Lerp(-8, 2, u); alpha = Mathf.Clamp01(t / .05f); }
            else if (t < SlamIn + Settle)
            {
                float u = (t - SlamIn) / Settle, k = Mathf.SmoothStep(0, 1, u);
                scale = Mathf.Lerp(.88f, 1, k) + .05f * Mathf.Sin(u * Mathf.PI); tilt = Mathf.Lerp(2, 0, k);
                kick = Mathf.Sin(u * Mathf.PI);
            }
            else if (t > exitAt)
            {
                float u = Mathf.Clamp01((t - exitAt) / Exit);
                rise = 70 * u * u; alpha = 1 - u; scale = 1 - .08f * u;
            }
            _plateGroup.alpha = Mathf.Clamp01(alpha);
            _plate.localScale = Vector3.one * scale;
            _plate.localRotation = Quaternion.Euler(0, 0, tilt);
            _plate.anchoredPosition = new Vector2(0, 250 + rise);
            // The can glyph tips over on the impact, as the can did.
            _glyph.localRotation = Quaternion.Euler(0, 0, -22 * kick);
            _glyph.localScale = Vector3.one * (1 + .25f * kick);
        }
    }
}
