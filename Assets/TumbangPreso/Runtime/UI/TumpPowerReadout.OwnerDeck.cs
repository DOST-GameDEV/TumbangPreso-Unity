using TumbangPreso.Abilities;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The match deck of three powers (TODO VISUAL-1.4, 1.18).
    ///
    /// ⚠️⚠️ BOTTOM RIGHT ON KEYBOARD AND PAD, BOTTOM CENTRE ON TOUCH. The first-person arms and
    /// the held slipper own the lower centre of the frame, and the deck sat on top of them, so
    /// neither read cleanly. Overwatch and Rocket League put the kit in the lower right for the
    /// same reason. A phone keeps it centred because the right thumb's buttons live in the
    /// lower right there. `SlipperRecall` dodges the deck through <see cref="DeckRect"/>, the
    /// rectangle actually drawn, rather than a remembered position.
    ///
    /// ⚠️ THE KEYCAP IS A CORNER BADGE ON THE POWER, NOT A WHITE BOX UNDER IT, and the "Hold TAB
    /// for skills" line shows only until the reference has been opened once or the first
    /// round is over. VISION § 3: the match HUD carries no sentences.
    /// </summary>
    public sealed partial class TumpPowerReadout
    {
        private readonly OwnerAbilitySeal[] _ownerDials = new OwnerAbilitySeal[3];
        private readonly HudCard[] _ownerKeycaps = new HudCard[3];
        private static bool _referenceOpened;
        private int _deckPlacement = -1;
        private const float OwnerDeckWidth = 316, OwnerDeckHeight = 124;

        public void Build(Transform root)
        {
            _deck = OwnerUiLayout.Rect(root, "PowerSeals");
            PlaceDeck(Hud.OnTouch);
            for (int i = 0; i < 3; i++)
            {
                float size = i == 2 ? 108 : 90, x = i == 0 ? 0 : i == 1 ? 102 : 206, y = OwnerDeckHeight - size - 4;
                _ownerDials[i] = OwnerUiLayout.Rect(_deck, "Power" + i).gameObject.AddComponent<OwnerAbilitySeal>();
                _ownerDials[i].raycastTarget = false; OwnerUiLayout.Place(_ownerDials[i].rectTransform, x, y, size, size);
                _symbols[i] = OwnerUiLayout.Rect(_ownerDials[i].transform, "PowerIcon").gameObject.AddComponent<TumpAbilitySymbol>();
                OwnerUiLayout.Fill(_symbols[i].rectTransform);
                // VISUAL-1.18: the match weight (see `TumpAbilitySymbol.HudStyle`), a little
                // larger inside the disc because the keel now carries the edge.
                _symbols[i].HudStyle = true;
                float inset = size * .24f;
                _symbols[i].rectTransform.offsetMin = new Vector2(inset, inset); _symbols[i].rectTransform.offsetMax = new Vector2(-inset, -inset);
                _symbols[i].raycastTarget = false;
                _states[i] = OwnerUiLayout.Text(_ownerDials[i].transform, "PowerState", "", 28, OwnerUiLayout.TypeRole.Display);
                _states[i].color = CourtPresentationPalette.Paper; _states[i].alignment = TextAnchor.MiddleCenter;
                _states[i].verticalOverflow = VerticalWrapMode.Overflow; OwnerUiLayout.Fill(_states[i].rectTransform);

                _ownerKeycaps[i] = OwnerUiLayout.Rect(_deck, "KeyboardCap" + i).gameObject.AddComponent<HudCard>();
                _ownerKeycaps[i].color = CourtPresentationPalette.Paper; _ownerKeycaps[i].Radius = 7; _ownerKeycaps[i].ShadowOffset = new Vector2(0, -2);
                _ownerKeycaps[i].raycastTarget = false; OwnerUiLayout.Place(_ownerKeycaps[i].rectTransform, x + size - 34, y + size - 30, 40, 34);
                _keys[i] = OwnerUiLayout.Text(_deck, "LiveBinding" + i, "", 28, OwnerUiLayout.TypeRole.Display);
                _keys[i].alignment = TextAnchor.MiddleCenter; _keys[i].verticalOverflow = VerticalWrapMode.Overflow;
                _keys[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                OwnerUiLayout.Place(_keys[i].rectTransform, x + size - 34, y + size - 30, 40, 34);
                var edge = _keys[i].gameObject.AddComponent<Outline>(); edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(1, -1);
                edge.enabled = false;
                _keyGlyphs[i] = OwnerUiLayout.Rect(_keys[i].transform, "BindingGlyph").gameObject.AddComponent<Image>();
                _keyGlyphs[i].preserveAspect = true; _keyGlyphs[i].raycastTarget = false; OwnerUiLayout.Fill(_keyGlyphs[i].rectTransform);
                _keyGlyphs[i].rectTransform.offsetMin = new Vector2(-3, -3); _keyGlyphs[i].rectTransform.offsetMax = new Vector2(3, 3);
            }
            _hint = OwnerUiLayout.Text(_deck, "PowerInfoBinding", "", 28); _hint.color = CourtPresentationPalette.Paper;
            _hint.alignment = TextAnchor.MiddleCenter; OwnerUiLayout.Place(_hint.rectTransform, -90, -40, OwnerDeckWidth + 150, 36);
            var outline = _hint.gameObject.AddComponent<Outline>(); outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(1, -1);
            BuildDetails(root);
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            _inspect = asset?.FindActionMap("Player", false)?.FindAction("AbilityInfo", false); _inspect?.Enable();
        }

        private void PlaceDeck(bool touch)
        {
            int placement = touch ? 1 : 0;
            if (placement == _deckPlacement) return;
            _deckPlacement = placement;
            if (touch) { _deck.anchorMin = _deck.anchorMax = _deck.pivot = new Vector2(.5f, 0); _deck.anchoredPosition = new Vector2(0, 34); }
            else { _deck.anchorMin = _deck.anchorMax = _deck.pivot = new Vector2(1, 0); _deck.anchoredPosition = new Vector2(-40, 30); }
            _deck.sizeDelta = new Vector2(OwnerDeckWidth, OwnerDeckHeight);
            GetComponent<HudReadingLayout>()?.RebasePlacement();
        }

        public void Tick(HeroAbilitySystem system, bool visible)
        {
            var kit = system != null ? system.Kit : null; visible &= kit != null; _deck.gameObject.SetActive(visible);
            if (!visible) { _detail.gameObject.SetActive(false); return; }
            PlaceDeck(Hud.OnTouch);
            _skills[0] = kit.Skill1; _skills[1] = kit.Skill2; _skills[2] = kit.Ultimate;
            for (int i = 0; i < 3; i++)
            {
                var skill = _skills[i]; if (skill == null) continue;
                if (_symbols[i].Glyph != skill.Glyph) { _symbols[i].Glyph = skill.Glyph; _symbols[i].SetVerticesDirty(); }
                bool ready = !kit.PracticeMode && (i == 2 ? kit.IsUltimateReady : skill.IsReady);
                float ratio = i == 2 ? kit.UltimateRatio : skill.IsActive ? skill.DurationRatio : 1 - skill.CooldownRatio;
                _ownerDials[i].State(ratio, ready, skill.IsActive, i == 2);
                _symbols[i].color = ready ? CourtPresentationPalette.Gold : CourtPresentationPalette.Paper;
                if (_symbols[i].Muted == ready) { _symbols[i].Muted = !ready; _symbols[i].SetVerticesDirty(); }
                string state = kit.PracticeMode ? "Wait" : skill.IsActive ? skill.CanReactivate ? "Again" : skill.DurationRemaining.ToString("0.0") :
                    i == 2 ? ready ? "" : Mathf.FloorToInt(kit.UltimateRatio * 100) + "%" :
                    skill.UsesCharges ? skill.ChargesRemaining.ToString() : skill.CooldownRemaining > 0 ? AbilityDeckHud.CooldownLabel(skill.CooldownRemaining) : "";
                var slot = i == 0 ? HeroAbilitySystem.Slot.Skill1 : i == 1 ? HeroAbilitySystem.Slot.Skill2 : HeroAbilitySystem.Slot.Ultimate;
                if (system.SecondsSinceAnswer(slot) < .8f)
                {
                    var answer = system.LastAnswer(slot);
                    if (answer == HeroKit.CastOutcome.CannotAct) state = "Wait";
                    else if (answer == HeroKit.CastOutcome.NoCharge) state = "Empty";
                    else if (answer == HeroKit.CastOutcome.NotYet) state = "Not yet";
                }
                _states[i].text = state; _symbols[i].canvasRenderer.SetAlpha(string.IsNullOrEmpty(state) ? 1 : .22f);
                string binding = Hud.KeyLabelFor(Actions[i]); _keys[i].text = Hud.OnTouch ? "" : binding;
                bool pad = LastInputDevice.Current == InputDeviceKind.Gamepad;
                _keyGlyphs[i].sprite = pad ? InputGlyphs.For(binding.ToUpperInvariant(), true) : null; _keyGlyphs[i].enabled = _keyGlyphs[i].sprite != null;
                bool cap = !Hud.OnTouch && !pad && binding.Length <= 3;
                _ownerKeycaps[i].gameObject.SetActive(cap);
                _keys[i].color = _keyGlyphs[i].enabled ? Color.clear : cap ? HudDraw.CardInk : CourtPresentationPalette.Paper;
                var edge = _keys[i].GetComponent<Outline>(); if (edge != null) edge.enabled = !cap && !_keyGlyphs[i].enabled;
            }
            if (kit.IsUltimateReady && !kit.PracticeMode && !_ultimateReady) GameServices.Audio?.PlayUi("sfx_super_ready");
            _ultimateReady = kit.IsUltimateReady && !kit.PracticeMode;
            bool held = _captureReference || (_inspect != null && _inspect.IsPressed());
            if (held && !_captureReference) _referenceOpened = true;
            var match = GameServices.Match;
            _hint.text = Hud.OnTouch ? "Hold info for skills" : "Hold " + Hud.KeyLabelFor("AbilityInfo") + " for skills";
            _hint.enabled = !held && !_referenceOpened && (match == null || match.RoundNumber <= 1);
            _detail.gameObject.SetActive(held); if (held) Describe(kit, _skills);
        }

        /// <summary>The deck's drawn rectangle in its canvas's centred units, for anything that
        /// must stay clear of it. Empty while the deck is hidden.</summary>
        public Rect DeckRect()
        {
            if (!DeckVisible) return default;
            var canvas = _deck.GetComponentInParent<Canvas>();
            if (canvas == null) return default;
            var root = (RectTransform)canvas.rootCanvas.transform;
            var corners = new Vector3[4]; _deck.GetWorldCorners(corners);
            Vector2 min = root.InverseTransformPoint(corners[0]), max = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }
    }
}
