using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// TAYA, revealed as each round goes live (owner, 2026-10-22). One big word on a Defense plate and, under it, who:
    /// the seat's colour chip and name, or YOU when it is this screen's player. Played by `Visual.TayaIdentity` from
    /// state every peer has, so every screen reveals the same player.
    ///   * anticipation: the plate drops in from above, oversized and tilted;
    ///   * impact: it slams to size with a kick, the name strip slides out from under it;
    ///   * aftermath: a beat to read, then it shrinks up toward the match bar and fades.
    /// </summary>
    public sealed class TayaReveal : MonoBehaviour
    {
        private const float DropIn = .16f, Settle = .2f, Hold = .95f, Exit = .3f;
        private const float Life = DropIn + Settle + Hold + Exit;
        private static TayaReveal _live;
        /// <summary>How many reveals have played this session. For tests.</summary>
        public static int Played { get; private set; }

        private RectTransform _group, _strip;
        private CanvasGroup _fade, _stripFade;
        private float _born;

        public static void Play(int slot)
        {
            var round = GameServices.Round;
            var owner = round != null && round.Lata != null ? round.Lata.transform : null;
            if (owner == null) return;
            if (_live != null) Destroy(_live.gameObject);
            var canvas = OwnerUiLayout.Canvas(owner, "~TayaReveal", 165);
            var focus = canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>(); if (raycaster != null) raycaster.enabled = false;
            _live = canvas.gameObject.AddComponent<TayaReveal>(); Played++;
            _live.Build((RectTransform)canvas.transform, slot);
        }

        private void Build(RectTransform root, int slot)
        {
            _born = Time.unscaledTime;
            _group = OwnerUiLayout.Rect(root, "TayaRevealGroup");
            _group.anchorMin = _group.anchorMax = _group.pivot = new Vector2(.5f, .5f);
            _group.sizeDelta = new Vector2(640, 240); _group.anchoredPosition = new Vector2(0, 170);
            _fade = _group.gameObject.AddComponent<CanvasGroup>(); _fade.blocksRaycasts = false; _fade.interactable = false;

            // The name strip sits under the plate and slides out from behind it.
            _strip = OwnerUiLayout.Rect(_group, "TayaRevealWho");
            _strip.anchorMin = _strip.anchorMax = _strip.pivot = new Vector2(.5f, .5f);
            _stripFade = _strip.gameObject.AddComponent<CanvasGroup>(); _stripFade.blocksRaycasts = false;
            var stripCard = _strip.gameObject.AddComponent<HudCard>();
            stripCard.Glass(UiTheme.Ink, HudDraw.Lean, 3, .35f).raycastTarget = false; stripCard.FollowContrast = false;
            var chip = OwnerUiLayout.Rect(_strip, "TayaRevealSeat").gameObject.AddComponent<Image>();
            chip.color = PlayerIdentity.Colour(slot); chip.raycastTarget = false;
            chip.rectTransform.anchorMin = chip.rectTransform.anchorMax = new Vector2(0, .5f); chip.rectTransform.pivot = new Vector2(0, .5f);
            chip.rectTransform.sizeDelta = new Vector2(16, 40); chip.rectTransform.anchoredPosition = new Vector2(22, 0);
            bool mine = slot == NetAuthority.LocalSlot && !GameLaunch.Spectator;
            var who = OwnerUiLayout.Text(_strip, "TayaRevealName", mine ? "YOU" : SeatLabel.Raw(slot), 36, OwnerUiLayout.TypeRole.Display);
            who.alignment = TextAnchor.MiddleLeft; who.raycastTarget = false; who.supportRichText = false;
            who.color = mine ? HudDraw.Honey : HudDraw.Ivory;
            who.horizontalOverflow = HorizontalWrapMode.Overflow; who.verticalOverflow = VerticalWrapMode.Overflow;
            var whoInk = who.gameObject.AddComponent<Outline>(); whoInk.effectColor = UiTheme.InGameOutline; whoInk.effectDistance = new Vector2(2, -2);
            float whoWidth = Mathf.Min(420, Mathf.Ceil(who.preferredWidth));
            who.rectTransform.anchorMin = new Vector2(0, 0); who.rectTransform.anchorMax = new Vector2(1, 1);
            who.rectTransform.offsetMin = new Vector2(52, 0); who.rectTransform.offsetMax = new Vector2(-18, 0);
            _strip.sizeDelta = new Vector2(whoWidth + 80, 60);

            var plate = OwnerUiLayout.Rect(_group, "TayaRevealPlate");
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(.5f, .5f);
            var card = plate.gameObject.AddComponent<HudCard>();
            card.Glass(UiTheme.Taya, HudDraw.Lean, 3, .45f).raycastTarget = false; card.FollowContrast = false;
            var word = OwnerUiLayout.Text(plate, "TayaRevealWord", "TAYA", 96, OwnerUiLayout.TypeRole.Display);
            word.alignment = TextAnchor.MiddleCenter; word.raycastTarget = false; word.supportRichText = false;
            word.color = HudDraw.Ivory; word.fontStyle = FontStyle.Italic;
            word.horizontalOverflow = HorizontalWrapMode.Overflow; word.verticalOverflow = VerticalWrapMode.Overflow;
            var ink = word.gameObject.AddComponent<Outline>(); ink.effectColor = UiTheme.InGameOutline; ink.effectDistance = new Vector2(3, -3);
            OwnerUiLayout.Fill(word.rectTransform);
            plate.sizeDelta = new Vector2(Mathf.Ceil(word.preferredWidth) + 140, 132);
            plate.anchoredPosition = new Vector2(0, 30);
            Animate();
        }

        private void Update()
        {
            if (Time.unscaledTime - _born >= Life || GameServices.Round == null) { Destroy(gameObject); return; }
            Animate();
        }

        private void OnDestroy() { if (_live == this) _live = null; }

        private void Animate()
        {
            float t = Time.unscaledTime - _born;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            float exitAt = DropIn + Settle + Hold;
            float scale = 1, tilt = 0, y = 170, alpha = 1;
            if (still) alpha = t < .15f ? t / .15f : t > exitAt ? 1 - (t - exitAt) / Exit : 1;
            else if (t < DropIn)
            {
                float u = t / DropIn;
                y = Mathf.Lerp(420, 170, u * u); scale = Mathf.Lerp(1.6f, .9f, u * u); tilt = Mathf.Lerp(-9, 3, u);
                alpha = Mathf.Clamp01(t / .06f);
            }
            else if (t < DropIn + Settle)
            {
                float u = (t - DropIn) / Settle, k = Mathf.SmoothStep(0, 1, u);
                scale = Mathf.Lerp(.9f, 1, k) + .06f * Mathf.Sin(u * Mathf.PI); tilt = Mathf.Lerp(3, 0, k);
            }
            else if (t > exitAt)
            {
                float u = Mathf.Clamp01((t - exitAt) / Exit), e = u * u;
                y = Mathf.Lerp(170, 360, e); scale = Mathf.Lerp(1, .45f, e); alpha = 1 - u;
            }
            _fade.alpha = Mathf.Clamp01(alpha);
            _group.anchoredPosition = new Vector2(0, y);
            _group.localScale = Vector3.one * scale;
            _group.localRotation = Quaternion.Euler(0, 0, tilt);
            // The strip slides out from under the plate once it has landed.
            float s = still ? 1 : Mathf.Clamp01((t - DropIn - .05f) / .22f);
            float se = 1 - (1 - s) * (1 - s) * (1 - s);
            _strip.anchoredPosition = new Vector2(0, Mathf.Lerp(30, -62, se));
            _stripFade.alpha = se;
        }
    }
}
