using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Tells the player which of the four units they are actually driving, and what that unit
    /// can do right now. Converted from `scripts/ui/you_card.gd` and `YouCard.tscn`.
    ///
    /// Q-5, from a "role unclear on rejoin" gap: nothing else on the HUD says which unit is
    /// YOURS — the scores row shows every seat, not your seat.
    ///
    /// ⚠️⚠️ THE STAMINA BAR LIVES HERE AND NOWHERE ELSE, AND THAT IS A DECISION WITH TWO HALVES.
    /// `hud.gd::_refresh_stamina` is deliberately an empty function: a centre-bottom bar was
    /// added to the HUD while this card was already drawing one from the same number, and two
    /// bars showing one value forty pixels apart is worse than either. This card keeps it,
    /// because it is where a player already looks for their own state.
    ///
    /// ⚠️ IT WAS MISSING FROM THE CONVERSION ENTIRELY. The card came across with the charge and
    /// righting meters and no stamina row at all, so the one number a player watches while
    /// running out of the box was not on screen anywhere.
    ///
    /// ⚠️ "YOU" AND THE HOLD ROW WERE DELETED FROM THE ORIGINAL. 🧑 2026-08-01: *"too ugly, too
    /// much stuff happening in that box. confusing"*. Do not add a header back.
    /// </summary>
    public sealed class YouCard : MonoBehaviour
    {
        public const float RefreshInterval = 0.15f;

        /// <summary>How long the "just became ready again" flash lasts. Deliberately NOT shared
        /// with the 3D hit flash — that one is about a hit landing on a mesh, this is a 2D meter
        /// filling back up, and there is no reason the two move in lockstep.</summary>
        public const float ReadyFlashDuration = 0.2f;

        /// <summary>`YouCard.tscn` anchors bottom-left at 16, -196 to 396, -64.</summary>
        private static readonly Vector2 CardSize = new Vector2(380.0f, 132.0f);
        private static readonly Vector2 CardOffset = new Vector2(16.0f, 64.0f);

        private CharacterMotor _character;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private float _refreshLeft;

        /// <summary>True while the taya is winding a lunge. Named after the row's own history;
        /// see <see cref="UpdateLungeMeter"/>.</summary>
        private bool _bumpCharging;

        private Image _card;
        private Text _class;
        private Text _detail;

        private Text _staminaKey;
        private Image _staminaFill;

        private Text _chargeKey;
        private Image _chargeFill;
        private Text _resetKey;
        private Image _resetFill;

        private GameObject _staminaRow;
        private GameObject _chargeRow;
        private GameObject _resetRow;

        private bool _isAttackerPerson;
        private bool _isDefenderPerson;
        private bool _wasFatigued;

        /// <summary>Has the stamina row had its one-time state written? See UpdateStamina.
        /// </summary>
        private bool _staminaPrimed;

        /// <summary>
        /// ⚠️⚠️ IT STARTS TRUE, AND FALSE MADE THE BAR SPAWN WHITE. The ready flash fires on the
        /// TRANSITION from not-full to full, so that "you can sprint again" is readable without
        /// watching the bar. Initialised to false, a character who spawns with full stamina
        /// counts as having just transitioned, so every round opened by lerping the bar to
        /// `Card` (#f5f7fa) for the flash duration.
        ///
        /// Measured against the Godot build's own capture of the same moment: its role panel is
        /// dominated by HIGHLIGHT (248,208,40) where this build's was a flat near-white, which
        /// is what *"some of ur ui isnt the right color"* was looking at. Nobody ever sees the
        /// flash it was meant to be, because a full bar at spawn has nothing to announce.
        /// </summary>
        private bool _wasReady = true;
        private float _readyFlashLeft;
        private bool _accentKnown;
        private bool _accentDefense;

        public void Bind(CharacterMotor local) => _character = local;

        private void Awake() => Build();

        private void Update()
        {
            _refreshLeft -= Time.deltaTime;
            if (_refreshLeft <= 0.0f)
            {
                _refreshLeft = RefreshInterval;
                Refresh();
            }

            // ⚠️ EVERY FRAME, NOT EVERY REFRESH INTERVAL. A meter that moves nine times a second
            // visibly stutters, and this one is watched while running away.
            UpdateMeters();
        }

        public void Refresh()
        {
            if (_character == null)
            {
                _card.transform.parent.gameObject.SetActive(false);
                return;
            }

            _card.transform.parent.gameObject.SetActive(true);

            bool isDefense = _character.IsDefender;

            // ⚠️ THE CLASS ROW IS THE ROLE ROW. It used to name which of three unit KINDS you
            // were driving; there is only one kind, and the thing a player needs telling is
            // which of the two JOBS they have this round — they are different games.
            //
            // ⚠️⚠️ "TAYA", NOT "TAYA (DEFENDER)", AND THE GLOSS WAS COLLIDING WITH THE NAME.
            // 🧑 2026-08-27 sent a screenshot reading `TAYA (DEFENDEDANTE`. The row is a
            // `HorizontalLayoutGroup` with two `flexibleWidth: 1` children both set to
            // `HorizontalWrapMode.Overflow`, so when the two strings do not fit the 336 px of
            // content box they do not shrink, they draw straight over each other. "TAYA
            // (DEFENDER)" at 32 pt is 15 characters before the name has had a single pixel.
            //
            // ⚠️ AND THE GLOSS IS TAUGHT ELSEWHERE, WHICH IS WHY IT IS THE HALF THAT GOES.
            // `TutorialContent`'s premise strip puts TAYA over "guards it, alone" in the one
            // place a player meets the word for the first time; every other HUD readout in the
            // match already says the bare word (the round line, the scoreboard marker). This card
            // was the only surface still carrying the translation, six minutes into a match, in
            // the busiest corner of the screen.
            _class.text = isDefense ? "TAYA" : "ATTACKER";

            // ⚠️ THE SCORE WAS REMOVED FROM THIS ROW. 🧑 2026-07-31: *"why are there points
            // here, it's already up top it feels redundant"*. The row says who you are instead.
            //
            // ⚠️⚠️ AND IT IS THE CHARACTER'S NAME, NOT THE ACCOUNT HANDLE, WHICH IS THE THIRD
            // TIME THIS ROW HAS OVERFLOWED AND THE FIRST TIME THE CAUSE WAS UNBOUNDED. The two
            // notes above record `TAYA (DEFENDEDANTE` and `ATTACKERROCKAFORT`, and both were
            // fixed by making the strings SHORTER. 🧑 2026-09-03, off the phone render:
            // **"dont show #8826 or the player tag of ppl here bcz it makes it too long"**, and
            // *"and it overflows"*, over a card reading `ATTACKI` with `PLAYER#8226` drawn
            // straight through it.
            //
            // ⚠️⚠️ THE DIFFERENCE THAT MATTERS: A CHARACTER NAME IS BOUNDED AND AN ACCOUNT HANDLE
            // IS NOT. `DisplayName()` answers `CharacterName()` for a BOT and `_playerName` for a
            // human, so every previous fit was measured against the roster (`PHAISTER` is the
            // longest at eight characters) while a real player arrives as `PLAYER#8226` and a
            // custom one can be longer still. **Shortening the other half again would only move
            // the number at which it breaks.** Naming the character removes the class of fault.
            //
            // ⚠️ AND IT LOSES NOTHING THE PLAYER NEEDED. This is the one card that is about YOU,
            // and a player does not need to be told their own handle six minutes into a match;
            // what they can genuinely forget is which of the eighteen fighters they picked. The
            // handle is still on the scoreboard, where it is the thing that distinguishes four
            // seats from each other.
            _detail.text = _character.CharacterName();

            // ⚠️⚠️ MEASURED HERE, EVERY REFRESH, RATHER THAN ONCE AT BUILD. `docs/TODO.md`
            // § 129.3. Both strings above can change mid-match (the role swaps every round, and
            // the name arrives after the pick does), and the row's own WIDTH changes with the
            // canvas: `AspectSafeCanvas` scales on the short axis, so the same card is a different
            // number of units wide on a 4:3 panel and on his short wide window. A fit computed
            // once at build is a fit for one of those.
            //
            // ⚠️ IT IS CHEAP BECAUSE IT SHORT-CIRCUITS. `FitIdentityRow` compares the two strings
            // and the width against what it last fitted and returns immediately when nothing has
            // moved, which is the common case: this runs about seven times a second and the role
            // changes about once every ninety.
            FitIdentityRow();

            Color accent = isDefense ? UiTheme.Defense : UiTheme.Offense;

            // ⚠️ WOOD WITH A ROLE-COLOURED BORDER, matching the menu and the rest of the HUD.
            // This was a navy translucent card, which is the treatment the whole HUD used before
            // the wood restyle, and this is the card the player's eye returns to most.
            if (!_accentKnown || _accentDefense != isDefense)
            {
                _accentKnown = true;
                _accentDefense = isDefense;

                _card.sprite = GodotTheme.Box(UiTheme.WoodDeep, accent,
                                              GodotTheme.WoodBorderWidth,
                                              GodotTheme.WoodCornerRadius);
                _card.type = Image.Type.Sliced;
                _card.color = Color.white;
            }

            _class.color = UiTheme.Cream;
            _detail.color = accent;

            _isAttackerPerson = _character.IsPerson && !isDefense;
            _isDefenderPerson = _character.IsPerson && isDefense;

            if (_isAttackerPerson) _chargeKey.text = "[LMB]";
            // ⚠️⚠️ `RESET [E]`, NOT `RIGHTING LATA [E]`, AND THE OLD ONE WAS DRAWN CLIPPED.
            // 🧑 2026-08-29, off the taya card in the built player, where it read `RIGHTING LA`:
            // *"'righting lata' text overflow, maybe shorten that and check for other text
            // overflows here"*.
            //
            // ⚠️ THE ROW IS A METER, SO THE LABEL COLUMN IS NARROW BY DESIGN. This is the same
            // `BuildMeter` row the other two use, and the key sits beside a fill bar rather than
            // across the card. Its siblings are `[LMB]` at 5 characters and `LUNGE [RMB]` at 11;
            // this was **17** and had nowhere to go.
            //
            // ⚠️ AND `RESET` IS THE GAME'S OWN WORD FOR IT RATHER THAN A SHORTER SYNONYM.
            // `docs/Design.md` describes the verb as the taya RESETTING the lata, and the lata
            // card on the same screen already reads `RESETTING 53%` while it is happening, so the
            // two surfaces now say the same thing. `RIGHTING` was the only place that word
            // appeared anywhere in the game.
            if (_isDefenderPerson) _resetKey.text = "RESET [E]";

            _carrier = _character.GetComponent<Carrier>();
            _verbs = _character.GetComponent<CombatVerbs>();

            // ⚠️ EVERY UNIT, ALWAYS. The row used to be a Guard/Dash meter that only a Prop had;
            // it is STAMINA now and stamina is universal.
            _staminaRow.SetActive(true);

            RefreshRowVisibility();
        }

        /// <summary>
        /// ⚠️⚠️ THE ROLE GATE IS ONLY HALF OF IT, AND SHIPPING JUST THE ROLE GATE PUT A SECOND,
        /// PERMANENTLY EMPTY BAR IN THE CORNER OF THE SCREEN FOR THE WHOLE MATCH. This read
        /// `_chargeRow.SetActive(_isAttackerPerson)`, so every attacker carried an empty `[LMB]`
        /// meter from spawn to final whistle and every taya carried an empty righting bar.
        /// Measured against `Logs/shots-godot/g04-ready.png`, whose YOU card has exactly ONE bar.
        /// `you_card.gd::_update_row_visibility` is two conditions:
        ///
        ///     charge_row.visible = (_is_attacker_person and _charging) or _bump_charging
        ///     reset_channel_row.visible = _is_defender_person and _channeling
        ///
        /// ⚠️ AND THE CHARGE ROW IS SHARED WITH THE TAYA'S LUNGE METER, which this port did not
        /// have at all. Sharing is safe precisely because the two belong to DIFFERENT ROLES, so
        /// no unit can ever be charging both — it used to be shared by the throw and the shove,
        /// which were the same role and genuinely could collide. The taya's card showed a
        /// righting channel and nothing else; the verb that scores their points now has a
        /// readout, which is the whole of the .gd's own argument for the row.
        ///
        /// ⚠️ POLLED FOR THE LUNGE, SIGNALLED FOR THE THROW, exactly as the .gd splits it: the
        /// throw lives on `Carrier`, the lunge lives on the combat step, and a poll is
        /// self-healing across the role swap that re-resolves the local unit every round.
        /// </summary>
        private void RefreshRowVisibility()
        {
            bool charging = _isAttackerPerson && _carrier != null && _carrier.IsCharging;
            bool channeling = _isDefenderPerson && _carrier != null && _carrier.ChannelRatio > 0.0f;

            _chargeRow.SetActive(charging || _bumpCharging);
            _resetRow.SetActive(channeling);
        }

        private void UpdateMeters()
        {
            if (_character == null) return;

            UpdateStamina();
            UpdateLungeMeter();

            if (_isAttackerPerson && _chargeFill != null && !_bumpCharging)
            {
                _chargeKey.text = "[LMB]";
                SetFill(_chargeFill, _carrier != null ? _carrier.ChargeRatio : 0.0f);
            }

            if (_isDefenderPerson && _resetFill != null)
            {
                // The righting channel lives on the Carrier alongside the throw charge — both
                // are "hold this key and a bar fills", and they are mutually exclusive because
                // no unit is attacker and taya at once.
                SetFill(_resetFill, _carrier != null ? _carrier.ChannelRatio : 0.0f);
            }

            // The rows appear and disappear with the verb, so this has to be asked every frame
            // rather than once per `RefreshInterval` — a meter that shows up a sixth of a second
            // after the key goes down misses most of a 0.5 s lunge wind-up entirely.
            RefreshRowVisibility();
        }

        /// <summary>
        /// § THE TAYA'S LUNGE METER, on the same bar as the attacker's throw charge.
        ///
        /// ⚠️ THIS ROW WAS THE BUMP METER, THEN THE SHOVE METER, AND SINCE 2026-08-01 IT IS THE
        /// LUNGE. Worth stating because the row keeps outliving the mechanic it was built for:
        /// bump died with the 2v2 pivot, the shove inherited it, and the shove then became a
        /// single tap with no charge at all, which leaves nothing to draw. The lunge took its
        /// place because it is the only charged commitment left in the game — hold, 0.5 s to
        /// full power, release to dash and tag — and it belongs to the one role that had no
        /// meter at all.
        /// </summary>
        private void UpdateLungeMeter()
        {
            if (!_isDefenderPerson || _verbs == null)
            {
                _bumpCharging = false;
                return;
            }

            float ratio = _verbs.ObservedLungeCharge;
            _bumpCharging = ratio >= 0.0f;

            if (!_bumpCharging) return;

            _chargeKey.text = "LUNGE [RMB]";
            SetFill(_chargeFill, ratio);
        }

        /// <summary>
        /// ⚠️⚠️ FATIGUE IS SHOWN ON THIS BAR BECAUSE IT IS THE SAME BAR. Emptying the meter costs
        /// a lockout, and a bar that simply sits at zero does not say that — it reads as "wait
        /// for it to refill", which is the wrong instruction. Red plus the word is the difference
        /// between empty and punished.
        ///
        /// ⚠️ AND THE ROW IS SILENT AT REST. It used to read `SPRINT [SHIFT]` every frame of
        /// every match, a binding the tutorial and the settings screen both already teach,
        /// printed permanently in the busiest corner of the HUD. FATIGUED is the only text this
        /// row ever shows, so it means something when it appears.
        ///
        /// ⚠️ HIDDEN WHEN EMPTY, NOT JUST BLANKED. A zero-width label is still a child of the
        /// row, so the layout still spends its spacing on it and the bar starts 8 px right of
        /// the content box while ending flush with the margin. Eight pixels of one-sided padding
        /// is not enough to name and exactly enough to look wrong. 🧑 2026-08-02: *"center the
        /// thing i underlined"*.
        /// </summary>
        private void UpdateStamina()
        {
            float ratio = Mathf.Clamp01(_character.Stamina.Ratio);
            SetFill(_staminaFill, ratio);

            bool fatigued = _character.Stamina.IsFatigued;

            // ⚠️⚠️ THE EDGE CHECK BELOW CANNOT FIRE AT SETUP, AND THAT IS WHY THE BAR SAT OFF
            // CENTRE. Both sides start false, so the very first pass through this function does
            // nothing at all, and the key label stays ACTIVE and EMPTY — spending its 110 px of
            // preferred width on nothing while the bar ends flush with the right margin. The
            // .gd carries the identical warning against its own `_was_fatigued` check and the
            // identical fix: hide the label rather than blank it, because a zero-width child is
            // still a child and the row still pays for it.
            if (!_staminaPrimed)
            {
                _staminaPrimed = true;
                _staminaKey.gameObject.SetActive(fatigued);
            }

            if (fatigued != _wasFatigued)
            {
                _wasFatigued = fatigued;

                _staminaKey.text = fatigued ? "FATIGUED" : "";
                _staminaKey.color = fatigued ? UiTheme.Danger : UiTheme.CreamMuted;
                _staminaKey.gameObject.SetActive(fatigued);
                _staminaFill.color = fatigued ? UiTheme.Danger : UiTheme.Highlight;
            }

            // Flash to near-white for a moment when the bar returns to full, so "ready again" is
            // readable without watching the bar.
            bool isReady = ratio >= 1.0f;
            if (isReady && !_wasReady && !fatigued) _readyFlashLeft = ReadyFlashDuration;
            _wasReady = isReady;

            if (_readyFlashLeft <= 0.0f) return;

            _readyFlashLeft = Mathf.Max(0.0f, _readyFlashLeft - Time.deltaTime);

            float k = _readyFlashLeft / ReadyFlashDuration;
            _staminaFill.color = Color.Lerp(UiTheme.Highlight, UiTheme.Card, k);
        }

        private static void SetFill(Image fill, float ratio)
        {
            var rect = fill.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1.0f);
        }

        private void Build()
        {
            var canvasGo = new GameObject("YouCardCanvas");
            canvasGo.transform.SetParent(transform, false);

            // ⚠️⚠️ THIS IS A ROOT CANVAS AND IT STAYS ONE. Parenting it under `Hud.CleanFeedRoot`
            // the way `RoleSwapCard` does was tried on 2026-08-27 and reverted the same hour:
            // a NESTED Canvas ignores its own `CanvasScaler`, so the card lost `AspectSafeCanvas`
            // and its 380 x 132 rect stopped being anchored to a screen-sized parent.
            // `HudOverflowProbe` caught it immediately, at all nine resolutions, with the
            // identity row hanging 274 units off the RIGHT edge. That is `docs/TODO.md` § 18.1b's
            // "converted between two different canvases" hazard arriving from the other side.
            //
            // ⚠️⚠️ SO THE HIDING IS EXPLICIT INSTEAD, AND IT HAS TO BE. 🧑 2026-08-27, watching a
            // match: *"fix all these spectator hud problems wtf some shit dont hide"*, with this
            // card and its stamina bar left in the corner of a spectator's screen. A root canvas
            // is invisible to all three of the HUD's hiding paths, so all three now sweep for
            // this component BY TYPE: `Hud.EnterSpectatorMode`, `Hud.SetCleanFeed`, and
            // `MatchInstaller`, which does not build it for a watcher at all.
            //
            // **If you add another way to hide the HUD, add this card to it.**
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f;
            AspectSafeCanvas.Apply(scaler);

            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(canvasGo.transform, false);

            _card = cardGo.GetComponent<Image>();
            _card.sprite = GodotTheme.Box(UiTheme.WoodDeep, UiTheme.Offense,
                                          GodotTheme.WoodBorderWidth, GodotTheme.WoodCornerRadius);
            _card.type = Image.Type.Sliced;
            _card.raycastTarget = false;

            var rect = _card.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.sizeDelta = CardSize;
            rect.anchoredPosition = CardOffset;

            var column = cardGo.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.spacing = 4.0f;

            // ⚠️ PADDING, NOT A TALLER RECT, IS HOW THIS CARD IS MADE BIGGER. 🧑 2026-08-02:
            // *"GOOD TEXT NOW ... js make box bigger"*. An earlier pass grew the anchored rect
            // and produced a third-empty panel, because an ATTACKER shows two of the four rows
            // and a pinned height is sized for a row set that seat never has.
            column.padding = new RectOffset(22, 22, 16, 16);

            var fitter = cardGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ⚠️⚠️ 132 IS A FLOOR, NOT A CEILING, AND WITHOUT IT THE CARD CAME OUT 96. Measured
            // off `Logs/shots-godot/g04-ready.png` at 1920x1080: the Godot card's role-coloured
            // border runs y882 to y1015, which is the .tscn's own `-196 .. -64` to the pixel.
            // The port had the fitter alone, so an ATTACKER — who shows two rows of the four —
            // got a card sized to those two rows and it sat 36 px lower than the original with
            // its bar crowding the bottom edge. The .tscn authors a FIXED 132 and grows UP from
            // the pinned bottom edge; a floor plus the fitter is the same behaviour, and it
            // keeps the growth the fitter was added for.
            cardGo.AddComponent<LayoutElement>().minHeight = CardSize.y;

            // ⚠️ 44, BECAUSE THE ROW HOLDS 32/34pt TEXT. `Row`'s height is the box, not the
            // glyphs; 34 here clipped the taller of the two labels against the row above it.
            var identity = Row(cardGo.transform, "IdentityRow", 44.0f, separation: 10.0f);

            // ⚠️⚠️ 32 AND 34, THE `HudCaption` AND `HudBody` VARIATIONS `YouCard.tscn` ASSIGNS
            // THESE TWO NODES, AND THE 24 HERE WAS INVENTED. Same fault the lata card had and
            // for the same reason: `ui_theme.gd`'s HUD_SIZES dict is the seam that lets the HUD
            // grow without dragging the menus with it, and reading a plausible number instead of
            // that dict is how the whole card ended up reading a size small beside the Godot
            // build. Its own note is worth reading before anyone trims them again — 16/13 to
            // 22/19 to 30/28, with a screenshot answered *"text still small"* each time.
            _class = Label(identity.transform, "ClassLabel", 32, UiTheme.Cream,
                           TextAnchor.MiddleLeft);

            // ⚠️⚠️ THE ROLE TAKES ITS OWN WIDTH AND THE NAME TAKES THE REST. IT WAS A 50/50
            // SPLIT AND THAT IS WHY THE ROW COLLIDED A SECOND TIME. 🧑 2026-08-29, with a
            // screenshot reading `ATTACKERROCKAFORT`: *"this has overflow as well"*.
            //
            // Both children had `flexibleWidth: 1`, so each got half of the 336 px content box —
            // about 163 px. `ATTACKER` at 32 pt does not fit 163 px in Darumadrop, and `Label`
            // sets `HorizontalWrapMode.Overflow`, so the role simply drew PAST its half and
            // straight into a name that is anchored `MiddleRight`. The best-fit added to
            // `_detail` shrinks the NAME and can do nothing about the label overrunning from the
            // left, which is why `TAYA (DEFENDEDANTE` came back as `ATTACKERROCKAFORT` the moment
            // the strings on the two sides were long enough again.
            //
            // ⚠️ `flexibleWidth: 0` PLUS `childControlWidth` IS THE FIX: the group asks `Text`
            // for its own `preferredWidth` and gives it exactly that, so the role is never
            // clipped and never overruns, whichever of `TAYA` and `ATTACKER` it is holding.
            var classLayout = _class.gameObject.AddComponent<LayoutElement>();
            classLayout.flexibleWidth = 0.0f;

            // ⚠⚠ AND THE ROLE IS BOUNDED, BECAUSE `flexibleWidth: 0` ALONE LET IT ASK FOR MORE
            // THAN THE ROW HAS. 🧑 2026-08-30, a THIRD report on this one row: *"Overflowing text
            // eg. Attacker Rockafort in the bottom left"*.
            //
            // `childControlWidth` gives an unbounded child its `preferredWidth`, and a
            // `HorizontalLayoutGroup` will not shrink a child below its `minWidth` — it overflows
            // the container instead. So the row asked for `preferredWidth(ATTACKER at 32 pt)` plus
            // 10 px of spacing plus the name's 140 px floor, and when that sum passes the 336 px
            // content box the surplus goes out of the wood. **That is exactly the mechanism
            // `docs/TODO.md` § 79.6 measured on the hero picker** (`rowLe.minHeight =
            // rowLe.preferredHeight` overflowing a `VerticalLayoutGroup`), arriving on the other
            // axis: the container CAN be squeezed and the children CANNOT.
            //
            // 170 + 10 + 140 = **320 against 336**, so the row now fits by construction whatever
            // the two strings are, rather than by the arithmetic happening to work out.
            //
            // ⚠️⚠️ SINCE 2026-09-03 THESE TWO NUMBERS ARE THE STARTING VALUES ONLY, AND
            // `FitIdentityRow` OVERWRITES BOTH ON THE FIRST REFRESH THAT HAS A LAID-OUT ROW TO
            // MEASURE. They are kept because they are what the row uses for the one frame before
            // any layout has run, and because the arithmetic above is the reasoning the measured
            // version replaced rather than a number it disagrees with. **The 336 is the part that
            // was never true everywhere**: `AspectSafeCanvas` scales on the short axis, so this
            // content box is about 336 units at 4:3 and wider on his window, and a split correct
            // at one width is `CLAUDE.md` § 6.2c's first row. `docs/TODO.md` § 129.3.
            classLayout.preferredWidth = 170.0f;

            // ⚠⚠ BEST-FIT IS GONE FROM BOTH LABELS AND `MenuKit.Fit` DOES THE WORK, WHICH IS THE
            // FOURTH TIME THIS ROW HAS BEEN TOUCHED AND THE FIRST TIME THE MECHANISM CHANGED.
            // `docs/TODO.md` § 129.3.
            //
            // `resizeTextForBestFit` looks like a measurement and is not one a caller can read.
            // When the string still does not fit at `resizeTextMinSize` it does not report that:
            // it clamps at the floor and then `horizontalOverflow = Overflow` draws the surplus
            // PAST the box, into the neighbour. **Two `Overflow` labels in one row is the whole
            // fault**, and every previous fix moved the number at which it triggers rather than
            // removing it: `TAYA (DEFENDEDANTE` (2026-08-27), `ATTACKERROCKAFORT` (2026-08-29),
            // *"Overflowing text eg. Attacker Rockafort"* (2026-08-30), `ATTACKI` over
            // `PLAYER#8226` (2026-09-03).
            //
            // ⚠️ `MenuKit.Fit` MEASURES THE SAME THING AND ANSWERS. It shrinks through
            // `preferredWidth` against a real width and RETURNS whether it succeeded, so
            // `FitIdentityRow` can act on a failure instead of drawing one. Its own header:
            // *"a caller that ignores the answer has an overflow it has been told about."*
            _class.resizeTextForBestFit = false;

            _detail = Label(identity.transform, "DetailLabel", 34, UiTheme.Offense,
                            TextAnchor.MiddleRight);

            var detailLayout = _detail.gameObject.AddComponent<LayoutElement>();
            detailLayout.flexibleWidth = 1.0f;

            // ⚠️ AND THE NAME KEEPS A FLOOR, so `ATTACKER` cannot eat the whole row on a wider
            // font or a smaller card. Below this the best-fit below has nothing to work with and
            // the name is clipped instead of shrunk.
            //
            // ⚠️⚠️ `FitIdentityRow` SETS THIS TO ZERO, AND THAT IS A REVERSAL WORTH READING
            // BEFORE PUTTING IT BACK. A `HorizontalLayoutGroup` will not shrink a child below its
            // `minWidth` and **overflows the container instead**, so a 140-unit floor beside a
            // 170-unit role is a row that can exceed its own box by construction on a narrow
            // screen: the floor that was protecting the name was also one of the two things
            // making the row overrun. The name is the child that gives now, and the role is
            // capped at half the row so it can never be the one that eats it.
            detailLayout.minWidth = 140.0f;

            // ⚠️⚠️ THE NAME SHRINKS RATHER THAN OVERLAPPING THE ROLE, AND SHORTENING THE ROLE
            // STRING ALONE WOULD NOT HAVE BEEN ENOUGH. Two `Overflow` labels in one layout row
            // draw over each other whenever the pair is too wide, and the right-hand one is a
            // PLAYER-TYPED name: `Balance.PlayerNameMax` allows more characters than 336 px of
            // content box can hold at 34 pt however short the role word is. Best-fit makes the
            // collision structurally impossible instead of arithmetically unlikely.
            //
            // ⚠️ THE FLOOR IS `MenuKit.MinReadableUnits`. Below that a label is a smudge on a 4:3
            // panel (see that constant), so a name long enough to need smaller than 18 is left
            // clipped by the row rather than shrunk into illegibility.
            _detail.resizeTextForBestFit = false;

            // `GuardDashSpacer`, 6 px, straight out of the .tscn. It separates the identity line
            // from the meters by more than the column's own spacing, so the card reads as a name
            // over a set of gauges rather than as three evenly-spaced rows.
            var spacer = new GameObject("GuardDashSpacer", typeof(RectTransform));
            spacer.transform.SetParent(cardGo.transform, false);
            spacer.AddComponent<LayoutElement>().preferredHeight = 6.0f;

            (_staminaRow, _staminaKey, _staminaFill) =
                BuildMeter(cardGo.transform, "GuardDashRow", UiTheme.Highlight);

            (_chargeRow, _chargeKey, _chargeFill) =
                BuildMeter(cardGo.transform, "ChargeRow", UiTheme.Highlight);

            (_resetRow, _resetKey, _resetFill) =
                BuildMeter(cardGo.transform, "ResetChannelRow", UiTheme.Defense);

            _staminaKey.gameObject.SetActive(false);
        }

        /// <summary>
        /// The authored sizes the row starts from every time it is measured.
        ///
        /// ⚠️ RE-APPLIED BEFORE EACH FIT RATHER THAN LEFT WHERE THE LAST FIT PUT THEM.
        /// `MenuKit.Fit` ONLY EVER SHRINKS, by construction (*"so a short string cannot inflate
        /// and change a row's height from screen to screen"*). Measuring `PHAISTER` after
        /// `SOMEBODYVERYLONG` had driven the label to 18 would keep the 18 for ever, so the row
        /// would ratchet smaller over a match and never come back.
        /// </summary>
        private const int ClassAuthoredSize = 32;
        private const int DetailAuthoredSize = 34;

        /// <summary>Set when the strings or the box changed, so the fit is not recomputed on
        /// every one of the four refreshes a second this card does.</summary>
        private string _fittedClass;
        private string _fittedDetail;
        private float _fittedWidth;

        /// <summary>
        /// Measure the identity row and shrink into it, rather than trusting that it fits.
        ///
        /// ⚠️⚠️ THIS IS `docs/TODO.md` § 129.3, AND THE ENTRY EXISTS BECAUSE THE THREE FIXES
        /// BEFORE IT ALL CHANGED A STRING AND NONE OF THEM CHANGED THE ARRANGEMENT. The row is a
        /// `HorizontalLayoutGroup` holding two `HorizontalWrapMode.Overflow` labels, so a pair
        /// that does not fit does not shrink, **it draws over itself**. § 129.2 removed the
        /// unbounded input (the account handle) and that was the right fix for the report; it did
        /// nothing about the mechanism, and the entry's own warning is *"treat any new string on
        /// this card as a fourth report waiting to happen."*
        ///
        /// ⚠️⚠️ THE 170-UNIT ROLE COLUMN WAS THE TRUSTED NUMBER AND IT IS NOW A MEASURED ONE.
        /// `170 + 10 + 140 = 320 against 336` was correct arithmetic about `ATTACKER` at 32 pt in
        /// Darumadrop at ONE canvas width, written as a constant. `AspectSafeCanvas` scales on the
        /// SHORT axis, so this card's content box is not 336 units on every screen —
        /// `CLAUDE.md` § 6.2c's first row is exactly this: *"a percentage of the window is not a
        /// size ... size a panel against its CONTENT and state the arithmetic."* The role now asks
        /// the font what it needs and the name gets everything left over, so the split is right at
        /// every width instead of at the one it was computed for.
        ///
        /// ⚠️ AND THE FLOOR IS STILL `MenuKit.MinReadableUnits`, SO A STRING THAT CANNOT FIT AT 18
        /// IS CLIPPED RATHER THAN SHRUNK INTO A SMUDGE. That is the same trade `Fit` documents and
        /// the same one `_detail`'s old note chose. What changes is WHICH failure happens: a
        /// clipped name loses its own tail, where an overflowing one destroys the label beside it.
        /// **Half a string is a bad readout; two strings on top of each other is neither.**
        /// </summary>
        private void FitIdentityRow()
        {
            if (_class == null || _detail == null) return;

            var rowRect = _class.transform.parent as RectTransform;
            if (rowRect == null) return;

            float room = rowRect.rect.width;

            // ⚠️ A ROW THAT HAS NOT BEEN LAID OUT YET REPORTS 0, and fitting against 0 would drive
            // both labels to the floor for no reason. Same guard `MenuKit.Fit` carries and
            // `MenuKit.FitBox`'s header warns about: measure after a layout pass, never in the
            // frame the row was built.
            if (room <= 1.0f) return;

            if (_fittedClass == _class.text && _fittedDetail == _detail.text &&
                Mathf.Approximately(_fittedWidth, room))
                return;

            _fittedClass = _class.text;
            _fittedDetail = _detail.text;
            _fittedWidth = room;

            var classLayout = _class.GetComponent<LayoutElement>();
            var detailLayout = _detail.GetComponent<LayoutElement>();
            if (classLayout == null || detailLayout == null) return;

            float spacing = 10.0f;
            var group = rowRect.GetComponent<HorizontalLayoutGroup>();
            if (group != null) spacing = group.spacing;

            // ⚠️ THE ROLE IS FITTED FIRST AND AGAINST A CAP, because it is the left-hand label
            // and the one that overran INTO its neighbour in all four reports. The cap is half the
            // row: the role is one of two words and the name is the thing a player actually reads
            // off this card, so the role may never take more than its half however wide the font
            // renders it.
            _class.fontSize = ClassAuthoredSize;
            MenuKit.Fit(_class, (room - spacing) * 0.5f);

            // What the role ACTUALLY needs at the size it settled on, rather than 170.
            float roleWidth = Mathf.Min(_class.preferredWidth, (room - spacing) * 0.5f);
            classLayout.preferredWidth = roleWidth;
            classLayout.minWidth = roleWidth;

            // ⚠️ AND THE NAME GETS EVERY UNIT THE ROLE DID NOT, WITH NO FLOOR OF ITS OWN. The old
            // `minWidth = 140` was the second half of the overflow: a `HorizontalLayoutGroup`
            // will not shrink a child below its `minWidth` and **overflows the container
            // instead**, so a 140-unit floor beside a 170-unit role was a row that could exceed
            // its own box by construction on a narrow screen. The name has no floor because it is
            // the child that gives.
            float nameRoom = Mathf.Max(0.0f, room - roleWidth - spacing);
            detailLayout.minWidth = 0.0f;
            detailLayout.preferredWidth = nameRoom;

            _detail.fontSize = DetailAuthoredSize;

            if (!MenuKit.Fit(_detail, nameRoom))
            {
                // ⚠️⚠️ THE ONE CASE THAT USED TO OVERPRINT, AND IT NOW CLIPS INSTEAD. `Fit`
                // returns false when the string will not fit at `MinReadableUnits`, and the
                // honest options at that point are "draw over the role" or "lose the tail".
                // `Wrap` breaks the line at the box edge and `Truncate` throws away every line
                // after the first, which on a one-line rect is exactly a clip. **`Overflow` is
                // what drew `ATTACKI` through `PLAYER#8226`**, so it is the one thing this row may
                // not do.
                _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
                _detail.verticalOverflow = VerticalWrapMode.Truncate;
            }
            else
            {
                _detail.horizontalOverflow = HorizontalWrapMode.Overflow;
                _detail.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        /// <summary>
        /// One row of the card. `separation` is the .tscn's own
        /// `theme_override_constants/separation`: 10 on `IdentityRow`, 8 on all three meters.
        /// </summary>
        private static GameObject Row(Transform parent, string name, float height,
                                      float separation = 8.0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = separation;

            go.AddComponent<LayoutElement>().preferredHeight = height;
            return go;
        }

        private static (GameObject, Text, Image) BuildMeter(Transform parent, string name,
                                                            Color fillColour)
        {
            var row = Row(parent, name, 26.0f);

            var key = Label(row.transform, $"{name}KeyLabel", 20, UiTheme.CreamMuted,
                            TextAnchor.MiddleLeft);
            key.text = "";
            key.gameObject.AddComponent<LayoutElement>().preferredWidth = 110.0f;

            var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(row.transform, false);

            var track = trackGo.GetComponent<Image>();
            track.sprite = GodotTheme.Plain(GodotTheme.CornerRadius);
            track.type = Image.Type.Sliced;
            track.color = UiTheme.WoodDark;
            track.raycastTarget = false;

            // ⚠️ (160, 26), THE .tscn's `custom_minimum_size` ON ALL THREE BARS. The 18 here was
            // invented and it is most of why the port's meters read as pinstripes beside the
            // Godot build's. A MINIMUM width rather than a fixed one, so the bar still takes the
            // rest of the row once the key label has had its 110.
            var element = trackGo.AddComponent<LayoutElement>();
            element.flexibleWidth = 1.0f;
            element.minWidth = 160.0f;
            element.preferredHeight = 26.0f;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);

            var fill = fillGo.GetComponent<Image>();
            fill.sprite = GodotTheme.Plain(GodotTheme.CornerRadius);
            fill.type = Image.Type.Sliced;
            fill.color = fillColour;
            fill.raycastTarget = false;

            var fr = fill.rectTransform;
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = new Vector2(0.0f, 1.0f);
            fr.pivot = new Vector2(0.0f, 0.5f);
            fr.offsetMin = new Vector2(2.0f, 2.0f);
            fr.offsetMax = new Vector2(-2.0f, -2.0f);

            return (row, key, fill);
        }

        /// <summary>The game's own face with the HUD's INK outline: this card sits over a live
        /// arena like everything else on the screen.</summary>
        private static Text Label(Transform parent, string name, int size, Color colour,
                                  TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<Text>();
            t.font = MenuKit.Font;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.alignByGeometry = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var ring = go.AddComponent<GodotOutline>();
            ring.OutlineColour = UiTheme.Ink;
            ring.Radius = 3.0f;

            return t;
        }
    }
}
