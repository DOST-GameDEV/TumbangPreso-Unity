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
        private float _refreshLeft;

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
            _class.text = isDefense ? "TAYA (DEFENDER)" : "ATTACKER";

            // ⚠️ THE SCORE WAS REMOVED FROM THIS ROW. 🧑 2026-07-31: *"why are there points
            // here, it's already up top it feels redundant"*. The row says who you are instead,
            // which is the only place the player's own chosen name appears to them.
            _detail.text = _character.DisplayName();

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
            if (_isDefenderPerson) _resetKey.text = "RIGHTING LATA [E]";

            _carrier = _character.GetComponent<Carrier>();

            // ⚠️ ONLY THE ROW THAT APPLIES TO THIS ROLE IS SHOWN. An attacker has no righting
            // channel and a taya has no throw charge; drawing an empty meter for the verb you do
            // not have is the "too much stuff happening" the box was cut down for.
            _chargeRow.SetActive(_isAttackerPerson);
            _resetRow.SetActive(_isDefenderPerson);

            // ⚠️ EVERY UNIT, ALWAYS. The row used to be a Guard/Dash meter that only a Prop had;
            // it is STAMINA now and stamina is universal.
            _staminaRow.SetActive(true);
        }

        private void UpdateMeters()
        {
            if (_character == null) return;

            UpdateStamina();

            if (_isAttackerPerson && _chargeFill != null)
                SetFill(_chargeFill, _carrier != null ? _carrier.ChargeRatio : 0.0f);

            if (_isDefenderPerson && _resetFill != null)
            {
                // The righting channel lives on the Carrier alongside the throw charge — both
                // are "hold this key and a bar fills", and they are mutually exclusive because
                // no unit is attacker and taya at once.
                SetFill(_resetFill, _carrier != null ? _carrier.ChannelRatio : 0.0f);
            }
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

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f;

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

            var identity = Row(cardGo.transform, "IdentityRow", 34.0f);

            _class = Label(identity.transform, "ClassLabel", 24, UiTheme.Cream,
                           TextAnchor.MiddleLeft);
            _class.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

            _detail = Label(identity.transform, "DetailLabel", 24, UiTheme.Offense,
                            TextAnchor.MiddleRight);
            _detail.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

            (_staminaRow, _staminaKey, _staminaFill) =
                BuildMeter(cardGo.transform, "GuardDashRow", UiTheme.Highlight);

            (_chargeRow, _chargeKey, _chargeFill) =
                BuildMeter(cardGo.transform, "ChargeRow", UiTheme.Highlight);

            (_resetRow, _resetKey, _resetFill) =
                BuildMeter(cardGo.transform, "ResetChannelRow", UiTheme.Defense);

            _staminaKey.gameObject.SetActive(false);
        }

        private static GameObject Row(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 8.0f;

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

            var element = trackGo.AddComponent<LayoutElement>();
            element.flexibleWidth = 1.0f;
            element.preferredHeight = 18.0f;

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
