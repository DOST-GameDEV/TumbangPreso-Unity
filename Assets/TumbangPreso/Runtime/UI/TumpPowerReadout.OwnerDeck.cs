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
        private readonly HudCard[] _chargePips = new HudCard[3];
        private readonly Text[] _chargeCounts = new Text[3];
        private static bool _referenceOpened;
        private int _deckPlacement = -1;

        // ⚠️⚠️ IN-GAME UI OVERHAUL 2026-10-20: THE DECK IS SPLIT THE WAY OVERWATCH SPLITS IT. The two
        // skills are leaning tiles at the bottom right with their key under each; the ultimate is
        // its own group, `UltimateSeal`, a ring with the percentage inside at the bottom CENTRE,
        // where Overwatch has always put it (`docs/reports/visual-research-2026-09-23/findings.md`
        // recorded the same frame). Charge-to-ultimate is the one number every player in a Hero
        // Strike match is watching, and the bottom centre is the place the eye returns to between
        // aims without leaving the reticle's column.
        //
        // ⚠️ ON TOUCH IT IS ONE CENTRED ROW, [SKILL][ULT][SKILL]: the right thumb's buttons own the
        // lower right of a phone (the reason this deck was centred on touch to begin with), so the
        // skills close in around the ultimate instead. Both layouts come from these numbers.
        //
        // ⚠️ THE 25 PER CENT ENLARGEMENT IS KEPT ON BOTH GROUPS (Feedback 2026-09-30, "the live
        // power deck grows 25 percent around its existing corner/bottom anchor").
        // ⚠️ THE KEY IS 32, NOT THE CORNER BADGE'S 26: it sits under its power now rather than over
        // the art, and at 26 the first 960 x 540 render drew E and Q about ten pixels tall.
        private const float SkillSize = 88, SkillGap = 14, KeySize = 32, KeyRow = KeySize + 4, UltSize = 92;
        private const float OwnerDeckHeight = SkillSize + 6 + KeyRow + 4;
        /// <summary>Keyboard and pad: two skills side by side.</summary>
        public const float SkillDeckWidth = SkillSize * 2 + SkillGap;
        /// <summary>Touch: the skills either side of the ultimate's slot.</summary>
        public const float TouchDeckWidth = SkillSize * 2 + UltWidth + SkillGap * 2;
        public const float UltWidth = UltSize + 12;
        private RectTransform _ultimate;

        // ⚠️⚠️ THE ROLE ABILITY'S BADGE AND SWAP (ability overhaul, 2026-09-25). On a role kit the
        // middle power is one ability attacking and another defending, and it changes at every
        // round boundary. The badge says which role it belongs to in the game's own role
        // vocabulary (the thrower's slipper on the attackers' orange, the taya's tag on the
        // defenders' blue, `UiTheme.Offense`/`Defense`, the one blue the palette rule exempts),
        // and the power pops when it changes so the swap is seen, not discovered. Keyboard, pad
        // and touch all read this same tile; touch's thumb button reads `kit.Skill2` too.
        private HudBadge _roleBadge;
        private HudCard _roleChip;
        private HeroAbilitySystem _system;
        private HeroAbility _shownRoleSkill;
        private float _roleSwappedAt = -10;
        public const float RoleSwapSeconds = 0.45f;

        public void Build(Transform root)
        {
            _deck = OwnerUiLayout.Rect(root, "PowerSeals");
            // ⚠️ BUILT AFTER THE SKILLS' GROUP ON PURPOSE: hierarchy order is power order, so every
            // `GetComponentsInChildren<OwnerAbilitySeal>` reader still finds Skill1, Skill2, Ultimate.
            _ultimate = OwnerUiLayout.Rect(root, "UltimateSeal");
            // Enlarge the live controls around their existing corner/bottom anchor.
            // HudReadingLayout retains this base scale for accessibility settings.
            _deck.localScale = Vector3.one * 1.25f; _ultimate.localScale = Vector3.one * 1.25f;
            for (int i = 0; i < 3; i++)
            {
                var group = i == 2 ? _ultimate : _deck;
                float size = i == 2 ? UltSize : SkillSize;
                _ownerDials[i] = OwnerUiLayout.Rect(group, "Power" + i).gameObject.AddComponent<OwnerAbilitySeal>();
                _ownerDials[i].raycastTarget = false; OwnerUiLayout.Place(_ownerDials[i].rectTransform, 0, 0, size, size);
                _symbols[i] = OwnerUiLayout.Rect(_ownerDials[i].transform, "PowerIcon").gameObject.AddComponent<TumpAbilitySymbol>();
                OwnerUiLayout.Fill(_symbols[i].rectTransform);
                // VISUAL-1.18: the match weight (see `TumpAbilitySymbol.HudStyle`).
                _symbols[i].HudStyle = true;
                // ⚠️ A DRAWN ICON FILLS MOST OF ITS FRAME (2026-09-23): the illustrations carry their own
                // ink keyline. The ring leaves more room than the tile because its notches are inside it.
                float inset = size * (i == 2 ? .2f : .17f);
                _symbols[i].rectTransform.offsetMin = new Vector2(inset, inset); _symbols[i].rectTransform.offsetMax = new Vector2(-inset, -inset);
                _symbols[i].raycastTarget = false;
                // Overwatch draws the seconds and the charge percentage as the biggest thing on the
                // power, italic, straight over the dimmed icon.
                _states[i] = OwnerUiLayout.Text(_ownerDials[i].transform, "PowerState", "", i == 2 ? 32 : 34, OwnerUiLayout.TypeRole.Display);
                _states[i].color = HudDraw.Ivory; _states[i].alignment = TextAnchor.MiddleCenter; _states[i].fontStyle = FontStyle.Italic;
                var stateEdge = _states[i].gameObject.AddComponent<Outline>(); stateEdge.effectColor = UiTheme.InGameOutline; stateEdge.effectDistance = new Vector2(2, -2);
                _states[i].verticalOverflow = VerticalWrapMode.Overflow; OwnerUiLayout.Fill(_states[i].rectTransform);

                // The key sits UNDER its power on a small dark glass cap, Overwatch's arrangement.
                _ownerKeycaps[i] = OwnerUiLayout.Rect(group, "KeyboardCap" + i).gameObject.AddComponent<HudCard>();
                _ownerKeycaps[i].Glass(HudDraw.GlassDeep, HudDraw.Lean, 3, .3f); _ownerKeycaps[i].BorderWidth = 1;
                _ownerKeycaps[i].raycastTarget = false; OwnerUiLayout.Place(_ownerKeycaps[i].rectTransform, 0, 0, KeySize + 6, KeySize);
                // ⚠️⚠️ CHARGES ARE A PIP, NOT A NUMBER OVER THE ICON (2026-09-23 UI review). "2" was drawn
                // across the middle of the power with the icon faded to 22 per cent, so a player saw a
                // number and could not tell WHICH power it counted. Valorant and Overwatch show the
                // icon whole and count charges at its edge; the pip sits at the top left corner in the
                // ready honey.
                _chargePips[i] = OwnerUiLayout.Rect(group, "ChargePip" + i).gameObject.AddComponent<HudCard>();
                _chargePips[i].Glass(HudDraw.Honey, HudDraw.Lean, 3, .3f); _chargePips[i].BorderWidth = 0; _chargePips[i].FollowContrast = false;
                _chargePips[i].raycastTarget = false; OwnerUiLayout.Place(_chargePips[i].rectTransform, 0, 0, 32, 30);
                _chargeCounts[i] = OwnerUiLayout.Text(_chargePips[i].transform, "ChargeCount", "", 28, OwnerUiLayout.TypeRole.Display);
                _chargeCounts[i].color = HudDraw.Ink; _chargeCounts[i].alignment = TextAnchor.MiddleCenter;
                _chargeCounts[i].horizontalOverflow = HorizontalWrapMode.Overflow; _chargeCounts[i].verticalOverflow = VerticalWrapMode.Overflow;
                OwnerUiLayout.Fill(_chargeCounts[i].rectTransform);
                _chargePips[i].gameObject.SetActive(false);

                _keys[i] = OwnerUiLayout.Text(group, "LiveBinding" + i, "", 28, OwnerUiLayout.TypeRole.Display);
                _keys[i].alignment = TextAnchor.MiddleCenter; _keys[i].verticalOverflow = VerticalWrapMode.Overflow;
                _keys[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                OwnerUiLayout.Place(_keys[i].rectTransform, 0, 0, KeySize, KeySize);
                var edge = _keys[i].gameObject.AddComponent<Outline>(); edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(1, -1);
                edge.enabled = false;
                _keyGlyphs[i] = OwnerUiLayout.Rect(_keys[i].transform, "BindingGlyph").gameObject.AddComponent<Image>();
                _keyGlyphs[i].preserveAspect = true; _keyGlyphs[i].raycastTarget = false; OwnerUiLayout.Fill(_keyGlyphs[i].rectTransform);
                _keyGlyphs[i].rectTransform.offsetMin = Vector2.zero; _keyGlyphs[i].rectTransform.offsetMax = Vector2.zero;
            }
            // The role is a small chip in your seat colour on the swapping power's top corner.
            _roleChip = OwnerUiLayout.Rect(_deck, "RoleChip").gameObject.AddComponent<HudCard>();
            _roleChip.color = HudDraw.Honey; _roleChip.Radius = 17; _roleChip.ShadowOffset = new Vector2(0, -2);
            _roleChip.raycastTarget = false; _roleChip.FollowContrast = false;
            _roleChip.gameObject.SetActive(false);
            _roleBadge = OwnerUiLayout.Rect(_deck, "RoleBadge").gameObject.AddComponent<HudBadge>();
            _roleBadge.raycastTarget = false; _roleBadge.RimWidth = 2;
            _roleBadge.gameObject.SetActive(false);
            _hint = OwnerUiLayout.Text(_deck, "PowerInfoBinding", "", 28); _hint.color = HudDraw.Ivory;
            _hint.alignment = TextAnchor.MiddleCenter;
            var outline = _hint.gameObject.AddComponent<Outline>(); outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(1, -1);
            PlaceDeck(Hud.OnTouch);
            BuildDetails(root);
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            _inspect = asset?.FindActionMap("Player", false)?.FindAction("AbilityInfo", false); _inspect?.Enable();
        }

        private void PlaceDeck(bool touch)
        {
            int placement = touch ? 1 : 0;
            if (placement == _deckPlacement) return;
            _deckPlacement = placement;
            float width = touch ? TouchDeckWidth : SkillDeckWidth;
            if (touch) { _deck.anchorMin = _deck.anchorMax = _deck.pivot = new Vector2(.5f, 0); _deck.anchoredPosition = new Vector2(0, 34); }
            else { _deck.anchorMin = _deck.anchorMax = _deck.pivot = new Vector2(1, 0); _deck.anchoredPosition = new Vector2(-40, 30); }
            _deck.sizeDelta = new Vector2(width, OwnerDeckHeight);
            // The ultimate is bottom centre on every device: alone on keyboard and pad, between the
            // two skills on touch (it lands in the gap the touch layout leaves for it).
            _ultimate.anchorMin = _ultimate.anchorMax = _ultimate.pivot = new Vector2(.5f, 0);
            _ultimate.anchoredPosition = new Vector2(0, touch ? 34 : 26);
            _ultimate.sizeDelta = new Vector2(UltWidth, OwnerDeckHeight);

            float second = width - SkillSize;
            PlacePower(0, 0, 0, SkillSize);
            PlacePower(1, second, 0, SkillSize);
            PlacePower(2, (UltWidth - UltSize) * .5f, 0, UltSize);
            OwnerUiLayout.Place(_roleChip.rectTransform, second + SkillSize - 26, -10, 34, 34);
            OwnerUiLayout.Place(_roleBadge.rectTransform, second + SkillSize - 24, -8, 30, 30);
            OwnerUiLayout.Place(_hint.rectTransform, 0, -40, width, 36);
            var layout = _deck.GetComponentInParent<HudReadingLayout>();
            layout?.RebasePlacement(_deck); layout?.RebasePlacement(_ultimate);
        }

        /// <summary>One power's frame, its key cap centred under it and its charge pip on its top corner.</summary>
        private void PlacePower(int i, float x, float y, float size)
        {
            OwnerUiLayout.Place(_ownerDials[i].rectTransform, x, y, size, size);
            float keyY = i == 2 ? size - 8 : size + 6;
            OwnerUiLayout.Place(_ownerKeycaps[i].rectTransform, x + (size - KeySize - 6) * .5f, keyY, KeySize + 6, KeySize);
            OwnerUiLayout.Place(_keys[i].rectTransform, x + (size - KeySize) * .5f, keyY, KeySize, KeySize);
            OwnerUiLayout.Place(_chargePips[i].rectTransform, x - 6, y - 6, 32, 30);
        }

        public void Tick(HeroAbilitySystem system, bool visible)
        {
            _system = system;
            var kit = system != null ? system.Kit : null; visible &= kit != null; _deck.gameObject.SetActive(visible);
            _ultimate.gameObject.SetActive(visible);
            if (!visible) { _detail.gameObject.SetActive(false); return; }
            PlaceDeck(Hud.OnTouch);
            _skills[0] = kit.Skill1; _skills[1] = kit.Skill2; _skills[2] = kit.Ultimate;
            PaintRole(kit);
            for (int i = 0; i < 3; i++)
            {
                var skill = _skills[i]; if (skill == null) continue;
                _symbols[i].SetCastLocked(system.GetComponent<CharacterMotor>()?.IsZapped == true);
                if (_symbols[i].Glyph != skill.Glyph) { _symbols[i].Glyph = skill.Glyph; _symbols[i].SetVerticesDirty(); }
                // Active reactivation follows its own window, not the original cast cooldown.
                bool actionReady = skill.IsActive && skill.CanReactivate ? skill.ReactivateReady : skill.IsReady;
                bool ready = !kit.PracticeMode && !system.GetComponent<CharacterMotor>().IsZapped && (i == 2 ? kit.IsUltimateReady : actionReady);
                // ⚠️ AN ULTIMATE STILL RUNNING AFTER ITS CAST (MOUNTAIN FORM) is drawn as an active power: the dial is its time left
                // and the tile reads the seconds, so the player who cast it knows how long the suit lasts (it is hidden from their view).
                float afterLeft = 0f, afterTotal = 1f;
                bool aftermath = i == 2 && kit.UltimateAftermath(out afterLeft, out afterTotal);
                float ratio = aftermath ? afterLeft / Mathf.Max(.01f, afterTotal) : skill.IsActive ? skill.DurationRatio : i == 2 ? kit.UltimateRatio : 1 - skill.CooldownRatio;
                var mine = system.GetComponent<CharacterMotor>();
                _ownerDials[i].Accent = mine != null ? PlayerIdentity.Colour(mine.PlayerSlot) : HudDraw.Honey;
                _ownerDials[i].State(ratio, ready, skill.IsActive || aftermath, i == 2);
                // Ivory when it can be pressed, honey for a charged ultimate, dimmed while it cannot.
                _symbols[i].color = ready ? (i == 2 ? HudDraw.Honey : HudDraw.Ivory) : HudDraw.Paper;
                if (_symbols[i].Muted == ready) { _symbols[i].Muted = !ready; _symbols[i].SetVerticesDirty(); }
                string state = aftermath ? afterLeft.ToString("0.0") + "s" : kit.PracticeMode ? "Wait" : skill.IsPersistentActive ? "Active" : skill.IsActive ? skill.CanReactivate
                    ? (skill.ReactivateReady ? "Again" : AbilityDeckHud.CooldownLabel(skill.ReactivateReadyIn)) + "\n" + skill.DurationRemaining.ToString("0.0") + "s"
                    : skill.DurationRemaining.ToString("0.0") + "s" :
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
                bool pip = i != 2 && !kit.PracticeMode && !skill.IsActive && skill.UsesCharges
                           && state == skill.ChargesRemaining.ToString();
                _chargePips[i].gameObject.SetActive(pip);
                if (pip) { _chargeCounts[i].text = state; state = ""; }
                _states[i].text = state;
                bool drawn = AbilityIcons.Illustration(skill.Glyph) != null;
                _symbols[i].canvasRenderer.SetAlpha(string.IsNullOrEmpty(state) ? 1 : drawn ? .5f : .22f);
                string binding = Hud.KeyLabelFor(Actions[i]); _keys[i].text = Hud.OnTouch ? "" : binding;
                bool pad = LastInputDevice.Current == InputDeviceKind.Gamepad;
                _keyGlyphs[i].sprite = !Hud.OnTouch ? InputGlyphs.For(binding.ToUpperInvariant(), true) : null; _keyGlyphs[i].enabled = _keyGlyphs[i].sprite != null;
                float extraWidth = (InputGlyphs.PromptWidth(_keyGlyphs[i].sprite, KeySize, KeySize) - KeySize) * .5f;
                _keyGlyphs[i].rectTransform.offsetMin = new Vector2(-extraWidth, 0);
                _keyGlyphs[i].rectTransform.offsetMax = new Vector2(extraWidth, 0);
                bool cap = !Hud.OnTouch && !pad && !_keyGlyphs[i].enabled && binding.Length <= 3;
                _ownerKeycaps[i].gameObject.SetActive(cap);
                _keys[i].color = _keyGlyphs[i].enabled ? Color.clear : HudDraw.Ivory;
                var edge = _keys[i].GetComponent<Outline>(); if (edge != null) edge.enabled = !cap && !_keyGlyphs[i].enabled;
            }
            if (kit.IsUltimateReady && !kit.PracticeMode && !_ultimateReady) GameServices.Audio?.PlayUi("sfx_super_ready");
            _ultimateReady = kit.IsUltimateReady && !kit.PracticeMode;
            bool held = _captureReference || (_inspect != null && _inspect.IsPressed());
            if (held && !_captureReference) _referenceOpened = true;
            var match = GameServices.Match;
            _hint.text = Hud.OnTouch ? "Hold info for skills" : "Hold " + Hud.KeyLabelFor("AbilityInfo") + " for skills";
            _hint.enabled = false; // Current Feedback removes the extra instruction above the deck.
            _detail.gameObject.SetActive(held); if (held) Describe(kit, _skills);
        }

        private void PaintRole(HeroKit kit)
        {
            bool role = kit.HasRoleAbilities;
            if (_roleBadge.gameObject.activeSelf != role) { _roleBadge.gameObject.SetActive(role); _roleChip.gameObject.SetActive(role); }
            if (!role) { _shownRoleSkill = null; return; }
            if (_shownRoleSkill != null && _shownRoleSkill != kit.Skill2) _roleSwappedAt = Time.unscaledTime;
            _shownRoleSkill = kit.Skill2;
            bool defending = kit.IsDefending;
            _roleBadge.Show(defending ? HudBadge.Glyph.Tag : HudBadge.Glyph.Slipper, CourtPresentationPalette.Paper, Color.clear);
            var owner = kit != null && _system != null ? _system.GetComponent<CharacterMotor>() : null;
            var chip = owner != null ? PlayerIdentity.Colour(owner.PlayerSlot) : HudDraw.Honey;
            if (_roleChip.color != chip) { _roleChip.color = chip; _roleChip.SetVerticesDirty(); }
            float t = Mathf.Clamp01((Time.unscaledTime - _roleSwappedAt) / RoleSwapSeconds);
            // A flip: the power squashes to its edge and turns over into the new one, then settles
            // with a small overshoot. Reduced motion keeps only the badge.
            float flip = Settings.SettingsStore.Current.ReducedUiMotion || t >= 1 ? 1
                : t < .45f ? Mathf.Cos(t / .45f * Mathf.PI * .5f) : Mathf.Sin((t - .45f) / .55f * Mathf.PI * .5f) * (1 + .12f * Mathf.Sin((t - .45f) / .55f * Mathf.PI));
            _ownerDials[1].rectTransform.localScale = new Vector3(Mathf.Max(.02f, flip), 1, 1);
            float pulse = t >= 1 ? 1 : 1 + .35f * Mathf.Sin(t * Mathf.PI);
            _roleBadge.rectTransform.localScale = Vector3.one * pulse;
        }

        /// <summary>The deck's drawn rectangle in its canvas's centred units, for anything that
        /// must stay clear of it. Empty while the deck is hidden.</summary>
        public Rect DeckRect() => DeckVisible ? CanvasRect(_deck) : default;

        /// <summary>The ultimate's drawn rectangle, in the same units as <see cref="DeckRect"/>. Since
        /// the 2026-10-20 split it is its own group at the bottom centre, so anything that dodges
        /// the deck (the recall mark) has to dodge this one too.</summary>
        public Rect UltimateRect() => DeckVisible && _ultimate != null && _ultimate.gameObject.activeInHierarchy ? CanvasRect(_ultimate) : default;

        private static Rect CanvasRect(RectTransform target)
        {
            var canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null) return default;
            var root = (RectTransform)canvas.rootCanvas.transform;
            var corners = new Vector3[4]; target.GetWorldCorners(corners);
            Vector2 min = root.InverseTransformPoint(corners[0]), max = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }
    }
}
