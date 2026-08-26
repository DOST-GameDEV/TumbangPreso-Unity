using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The in-match HUD, ported from `hud.gd` (1,588 lines) onto `HUD.tscn`'s arrangement.
    ///
    /// ⚠️⚠️ IT ASKS THE RULES, IT DOES NOT MIRROR THEM. The VULNERABLE warning comes from
    /// `IsTaggable`, which is the same call the tag itself makes, and the crosshair comes from
    /// `CanThrow`, which is the same call the throw makes. That is a rule and not a
    /// convenience: a HUD with its own opinion about legality will eventually promise safety
    /// the tag ignores, or grey out a throw the rules would have allowed, and the player sees
    /// no reason for either.
    ///
    /// ⚠️⚠️ IT IS WOOD AND AMBER, NOT PLAIN CARDS, AND THAT WAS A REPORTED BUG BEFORE. 🧑
    /// 2026-07-30, on the Godot build: the mid-game HUD *"kinda doesnt look like our theme (menu
    /// and lobby), it looks ugly and plain and confusing"*, and then on the first Unity capture,
    /// *"ugly ui btw, not even same theme what is that white box"*. The front end is wood and
    /// amber; a HUD in a different design language is the one the player spends the whole match
    /// looking at. Every surface here is built from the same `GodotTheme` the menus use, so it
    /// cannot drift away from them again.
    ///
    /// ⚠️ THE THREE FLOATING LINES ARE STYLISED TEXT, NOT PANELS. The objective, the ready
    /// prompt and the toast started as bare text, illegible over the orange viewmodel arms; the
    /// first fix gave all three a wood plate and the plates were rejected on sight: *"it kinda
    /// looks ugly with that hud/ui, can u js do text there but stylized, right color and font"*.
    /// Correct call. Legibility comes from a heavy INK outline instead, the same trick the
    /// screen-edge arrows use, because an outlined glyph survives sky, asphalt and a lit orange
    /// arm without adding another rectangle to a screen that has enough of them.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        /// <summary>INK outline on the free-floating lines. Heavy, because it has to carry
        /// role-orange text over a role-orange viewmodel arm.</summary>
        public const int TextOutline = 8;

        /// <summary>The crosshair sits at dead centre of the busiest part of an FPP frame, so
        /// its outline is heavier still relative to its size.</summary>
        public const int CrosshairOutline = 5;

        /// <summary>
        /// ⚠️ THE HELD DANGER TINT IS 0.16, NOT THE FLASH'S 0.45, and the distinction is the
        /// whole reconciliation. Two states last tens of seconds — the taya's can being down,
        /// and an attacker being catchable — and a full-screen red rect held at flash strength
        /// *"reads as the renderer being broken: measured on the first captured frame of a live
        /// match, the entire arena was washed red"*. 0.16 tints the frame enough to be noticed
        /// in peripheral vision and stays readable through for a whole round. The knockdown
        /// PULSE is kept on top of it, so the moment still punches.
        /// </summary>
        public const float DangerHoldAlpha = 0.16f;

        public const float DownedFlashTime = 0.45f;
        public const float DownedFlashPeak = 0.45f;

        public const int StatusRowLimit = 4;
        public const int StatusFontSize = 20;
        public const int TayaBadgeFontSize = 15;
        public const string TayaBadge = "TAYA";

        /// <summary>
        /// ⚠️ 5, NOT `TextOutline`'s 8. The .tscn sets `outline_size = 5` on every scoreboard
        /// cell and `hud.gd::_build_role_cell` sets the same 5 on the badge it inserts. The heavy
        /// 8 belongs to the free-floating lines, which are drawn straight over a live 3D scene
        /// with no panel behind them; these cells sit on wood and a ring that thick closes up the
        /// counters of the glyphs at font size 20.
        /// </summary>
        public const int ScoreOutline = 5;

        /// <summary>
        /// ⚠️ A FIXED WIDTH, ALWAYS PRESENT, NEVER HIDDEN. The badge is empty on three rows out
        /// of four; hiding it would let those three scores slide left and the column of numbers,
        /// which is the entire point of the board, would stop being a column. `_build_role_cell`
        /// measures "TAYA" at font 15 and falls back to 54 when it has no font, which is what
        /// this is.
        /// </summary>
        public const float TayaBadgeWidth = 132.0f;

        /// <summary>
        /// The .tscn's authored floor for the name cell, from when every row read "P1".."P4".
        /// <see cref="WorstCaseNameWidth"/> only ever widens past it.
        /// </summary>
        public const float ScoreNameFloor = 132.0f;

        private static readonly Vector2 StatusBarSize = new Vector2(190, 8);
        private static readonly Vector2 StatusMargin = new Vector2(38, 150);
        public const float StatusUnderBoardGap = 18.0f;

        [SerializeField] private CharacterMotor _local;

        private Canvas _canvas;
        private RectTransform _root;

        private Text _timer;
        private Image _timerCard;
        private RectTransform _timerCardRt;
        private Text _round;
        private Text _timerPressure;

        private Image _scoreboard;
        private Text _scoreTitle;
        private readonly Text[] _scoreNames = new Text[Balance.PlayerCount];
        private readonly Text[] _scoreMarks = new Text[Balance.PlayerCount];
        private readonly Text[] _scoreValues = new Text[Balance.PlayerCount];
        private readonly RectTransform[] _scoreRows = new RectTransform[Balance.PlayerCount];
        private readonly Image[] _scoreRowPlates = new Image[Balance.PlayerCount];
        private readonly Image[] _scoreRoleRails = new Image[Balance.PlayerCount];
        private readonly float[] _scorePulses = new float[Balance.PlayerCount];
        private readonly int[] _lastScoreBySlot = new int[Balance.PlayerCount];
        private bool _scoresInitialised;
        private RectTransform _scoreboardRt;

        private Image _lataCard;
        private Text _lataLabel;
        private Text _lataHint;
        private Text _lataAlert;

        private Text _toast;
        private float _toastLeft;

        private Image _getUpCard;
        private Text _getUpLabel;
        private Image _getUpFill;
        private Image _getUpMashFill;
        private RectTransform _getUpBarRt;

        /// <summary>How long the get-up bar stays popped after an accepted press.
        ///
        /// ⚠️ SHORTER THAN `Balance.MashCooldown` WOULD LEAVE A GAP BETWEEN TWO GOOD PRESSES and
        /// make a clean 10 Hz burst flicker. 0.14 s against the 0.10 s cap means a player at the
        /// cap holds the bar popped continuously, which is what "you are doing this right" wants
        /// to look like.</summary>
        private const float MashPopSeconds = 0.14f;
        private string _getUpShown = "";

        private Text _countdown;
        private float _countdownPop;
        private RectTransform _countdownRt;

        private Text _readyPrompt;
        private Text _readyObjective;
        private Image _readyPromptPlate;
        private Image _readyObjectivePlate;

        private Image _dangerFlash;
        private bool _dangerHeld;
        private float _flashLeft;

        /// <summary>§ THE STUN FROST — the screen half. See <see cref="UpdateFrost"/>.</summary>
        private Image _frostVignette;
        private Material _frostMaterial;
        private float _frostCoverage;

        public static Hud Instance { get; private set; }

        private Text _vulnerable;
        private Text _crosshair;
        private Text _hitmarker;
        private float _hitmarkerTimer;
        private OffscreenIndicators _indicators;

        private GameObject _heroDeck;
        private AbilityCard _skill1Card, _skill2Card, _ultCard;
        private bool _lastUltReady;
        private AbilityInspectPanel _inspect;
        private Text _inspectHint;
        private string _inspectHintText;

        /// <summary>
        /// How many notches the ultimate meter is cut into.
        ///
        /// ⚠⚠ THE SEGMENTS ARE THE WHOLE POINT OF THE ULT CARD, NOT DECORATION. A cooldown
        /// and an ultimate charge are different quantities and used to be drawn with the same
        /// smooth bar and the same text slot, so a card reading "READY!" and a card reading
        /// "6%" looked like the same widget in two states. The eye cannot tell a ready skill
        /// from a charging ultimate at a glance, which is the only way a HUD card is ever read.
        /// A notched meter cannot be mistaken for a draining one even in peripheral vision.
        /// </summary>
        private const int UltSegments = 10;

        /// <summary>
        /// One tile in the hero deck, and everything it needs to draw its own state.
        ///
        /// ⚠️⚠️ THE RIM IS A SEPARATE IMAGE FROM THE PLATE, AND IT HAS TO BE. `Image.color`
        /// multiplies the WHOLE nine-slice, border included, so a single sprite carrying both
        /// cannot light its edge without also lifting its fill: the old deck tinted the entire
        /// tile hero-orange to say "ready", which is a colour wash where the design wanted an
        /// outline. A transparent-fill box stacked on a static dark plate gives the rim its own
        /// colour for the cost of one extra draw per tile.
        ///
        /// ⚠️ THE ANIMATION STATE LIVES ON THE CARD, NOT IN THREE SETS OF FIELDS ON THE HUD.
        /// `_lastUltReady` is the last survivor of the old shape and it only exists because the
        /// ultimate's ready sting is a sound rather than a scale.
        /// </summary>
        private sealed class AbilityCard
        {
            public RectTransform Rt;
            public Image Plate;
            public Image Rim;
            public Image Glyph;
            public Image CooldownSweep;
            public Text Key;
            public Text State;
            public Image Fill;
            public Image[] Segments;

            /// <summary>
            /// The charge dots, for a skill that is on charges rather than on a cooldown.
            ///
            /// ⚠️⚠️ A THIRD SHAPE FOR A THIRD QUANTITY, AND THAT IS A SETTLED RULE RATHER THAN
            /// A STYLE CHOICE. `docs/VISION.md` § 3: *"Cooldown and ultimate charge must not
            /// look alike. A cooldown drains a smooth bar; the ultimate fills a notched one.
            /// They are different quantities and used to share a widget."* Charges are a third
            /// quantity, so reusing either of the first two would recreate exactly the fault
            /// that rule was written to close.
            ///
            /// Discrete dots are the right shape because the quantity IS discrete: "one left"
            /// is a fact a player acts on, and a bar at 50 per cent of two charges is the same
            /// picture as a bar at 50 per cent of a cooldown while meaning something completely
            /// different. Valorant draws them the same way for the same reason.
            ///
            /// Null on every cooldown ability, which is what `PaintCharges` keys off.
            /// </summary>
            public Image[] Pips;

            /// <summary>How many charges the pips were built for, so a kit swap rebuilds.</summary>
            public int PipCount;

            /// <summary>Seconds left of the flash that fires when a charge is handed back.</summary>
            public float PipGrantLeft;

            /// <summary>Charges seen last frame, so a grant can be told from a spend.</summary>
            public int WasCharges = -1;

            /// <summary>Seconds left of the 0.18 s pop that fires when a power comes back up.</summary>
            public float PopLeft;

            /// <summary>Whether it was available on the previous frame, so the edge can be seen.</summary>
            public bool WasReady;
        }

        // -------------------------------------------------------------------
        // § THE DECK'S GEOMETRY, AS NAMED NUMBERS RATHER THAN LITERALS
        //
        // ⚠️⚠️ THEY ARE PUBLIC SO A TEST CAN DO THE ARITHMETIC. A `HorizontalLayoutGroup` will
        // lay three cards out past the edge of a rect that no longer fits them, silently, and
        // the overflow lands under the first-person hands where it is least visible and most
        // annoying. `TheHeroDeckWidthMatchesItsChildren` asserts the identity below, so the next
        // person to widen a card gets a red test rather than a HUD that looks almost right.
        //
        //     DeckWidth = pad*2 + spacing*(cards-1) + skill + skill + ultimate
        //     240       = 6*2   + 6*2               + 70    + 70    + 76
        // -------------------------------------------------------------------

        public const float DeckWidth = 214.0f;
        public const float DeckHeight = 78.0f;
        public const float DeckBottomMargin = 14.0f;
        public const float DeckPadding = 0.0f;
        public const float DeckSpacing = 11.0f;
        public const float SkillCardWidth = 64.0f;
        public const float UltimateCardWidth = 64.0f;
        public const int DeckCardCount = 3;

        /// <summary>The square icon itself. The rest of a card's height is the key under it.</summary>
        public const float TileSize = 60.0f;

        /// <summary>Gap between the tile and the key label under it.</summary>
        public const float KeyGap = 3.0f;

        /// <summary>How long the "your power is back" pop runs. Seconds.</summary>
        private const float ReadyPopSeconds = AbilityDeckHud.ReadyPopSeconds;

        /// <summary>The deck tile's countdown size, from `BuildAbilityCard`. Sized for "9.9".</summary>
        private const int StateFontSize = 22;

        /// <summary>
        /// The size the word RECAST is drawn at instead.
        ///
        /// ⚠️ SIX BOLD CAPITALS DO NOT FIT A 60 px TILE AT 22 pt, and `HudLabel` sets
        /// `horizontalOverflow = Overflow`, so an oversized string hangs out of the tile rather
        /// than wrapping or shrinking. See `PaintSkillCard`.
        /// </summary>
        private const int RecastFontSize = 14;

        /// <summary>How long a successful cast lights its own tile. Seconds.</summary>
        private const float CastFlashSeconds = AbilityDeckHud.CastFlashSeconds;

        /// <summary>How long a refused press ticks its tile red. Seconds.</summary>
        private const float RefusalFlashSeconds = AbilityDeckHud.RefusalFlashSeconds;

        private GameObject _classicDeck;
        private Text _classicTitle;
        private Text _classicEvent;
        private Image _classicFill;
        private RectTransform _classicDeckRt;
        private float _streetHype;
        private float _streetHypeGrace;
        private float _streetHypePunch;
        private bool _streetHypeMaxCelebrated;
        private int _streetHypeRound = -1;

        private readonly List<StatusRow> _rows = new List<StatusRow>();
        private readonly List<StatusRow> _states = new List<StatusRow>();
        private readonly List<StatusRow> _cooldowns = new List<StatusRow>();

        private RectTransform _stackLeft;
        private RectTransform _stackRight;
        private readonly List<StatusWidget> _rowsLeft = new List<StatusWidget>();
        private readonly List<StatusWidget> _rowsRight = new List<StatusWidget>();

        /// <summary>One drawn status row: its label and its bar. A plain struct rather than a
        /// second MonoBehaviour, because a file may only hold one.</summary>
        private struct StatusWidget
        {
            public GameObject Root;
            public Text Label;
            public Image Fill;
            public Image Back;
        }

        // -------------------------------------------------------------------

        public void Bind(CharacterMotor local) => _local = local;

        /// <summary>
        /// "Walk around freely. Press [R] when you're ready to start the round." — the pre-round
        /// free-roam prompt. Driven by <see cref="ReadyGate"/>; the HUD does not decide when the
        /// window is open.
        ///
        /// ⚠️ IT ALSO RAISES THE ROLE OBJECTIVE. The ready phase is the last screen before the
        /// round and it used to say only how to START one, never what the player was about to be
        /// doing in it — dead air at exactly the moment somebody who has just read the tutorial
        /// needs it confirmed.
        /// </summary>
        public void ShowReadyPrompt(bool show)
        {
            if (_readyPrompt != null)
            {
                // ⚠️ THE KEY IS READ, NOT SPELLED. A rebound ready key used to leave this
                // line telling the player to press R, which is the one instruction on screen
                // they cannot ignore and cannot follow.
                string ready = "[" + KeyLabel("ReadyUp") + "]";

                // ⚠️⚠️ SENTENCE CASE AND ONE CLAUSE OF CONTEXT, NOT FOUR IN CAPITALS. The old
                // line was "PRACTICE TIME  ·  SCORES PAUSED  ·  TEST YOUR POWERS  ·  Press [R]
                // when ready." across 800 px of screen, which is four separate assertions the
                // player has to parse to find the one instruction in it. All-caps also removes
                // the word shapes that make a glance enough.
                _readyPrompt.text = SceneFlow.SelectedMode == GameMode.HeroStrike
                    ? $"Practice freely, scores are paused. Press {ready} when ready."
                    : $"Warm up freely, scores are paused. Press {ready} when ready.";
                _readyPrompt.enabled = show;
                if (_readyPromptPlate != null) _readyPromptPlate.enabled = show;
            }
            RefreshObjective(show);
        }

        /// <summary>
        /// ⚠️ THE OBJECTIVE IS DERIVED HERE, NOT PASSED IN, so it cannot drift out of step with
        /// the role the scoreboard is drawing. Blank when the role cannot be established: NO
        /// objective is a better failure than confidently telling somebody to guard the lata
        /// they are about to throw a slipper at.
        /// </summary>
        private void RefreshObjective(bool active)
        {
            if (_readyObjective == null) return;

            if (!active || _local == null)
            {
                _readyObjective.enabled = false;
                if (_readyObjectivePlate != null) _readyObjectivePlate.enabled = false;
                return;
            }

            bool defending = _local.IsDefender;

            // Two sentences for the taya because it IS two jobs, and a player who only hears
            // "guard the lata" stands on the base and never tags anybody. Two for the attacker
            // for the same reason: the retrieval run is the half people miss.
            if (SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                _readyObjective.text = defending
                    ? "Hold the box with your powers, then tag the retriever."
                    : "Open a gap with your powers, then make the retrieval run.";
            }
            else
            {
                _readyObjective.text = defending
                    ? "Guard the lata, block the shots, tag the retriever."
                    : "Curve or bank the throw, then risk the retrieval.";
            }

            // This is neutral coaching, not a role badge. Keep its rim in the HUD's quiet blue
            // so the orange accent remains reserved for live attacker and impact feedback.
            _readyObjective.color = UiTheme.Cream;
            _readyObjective.enabled = true;

            if (_readyObjectivePlate != null)
            {
                _readyObjectivePlate.sprite = GodotTheme.Box(
                    UiTheme.HeroPlate, UiTheme.HeroRimLit, 2, 6);
                _readyObjectivePlate.enabled = true;
            }
        }

        /// <summary>
        /// One tick of the 3 · 2 · 1 · GO!. Each tick pops in oversize and settles, which reads
        /// far more like a countdown than a static label swap would — the punch is the whole
        /// effect at this size.
        /// </summary>
        public void ShowCountdownTick(string tick)
        {
            if (_countdown == null) return;

            // ⚠️ THE SOUND IS PLAYED FROM HERE, NOT FROM THE GATE, so every caller gets it for
            // free and the pop animation and its sound can never drift apart by a frame. The
            // announcer's "Tatlo! Dalawa! Isa! Simula!" is wired separately, off the same event.
            GameServices.Audio?.PlayAt(tick == "GO!" ? "countdown_go" : "countdown_tick",
                                       UnityEngine.Camera.main != null
                                           ? UnityEngine.Camera.main.transform.position
                                           : Vector3.zero);

            // ⚠️⚠️ THE MATCH BED STARTS AT THE FIRST COUNTDOWN TICK, NOT WHEN THE ARENA LOADS.
            // `audio_manager.gd` hooks it on exactly this cue and explains why: the round does
            // not begin until AFTER the countdown finishes, so starting the bed on round-start
            // left the menu bed playing under the entire 3 · 2 · 1 and crossfading on "GO!",
            // late by the length of the countdown. 🧑 2026-08-01: *"Remove the audio latency
            // during round initialization. RoundMusic should begin playing immediately."*
            if (tick != "GO!" && GameServices.Music != null
                && GameServices.Music.Current != "match")
            {
                GameServices.Music.Play("match", GameServices.MatchTrack);
            }

            _countdown.enabled = true;
            _countdown.text = tick;

            // HIGHLIGHT matches the same urgency tint the round timer uses under 15 s, so it
            // reads as the same game system rather than as a one-off UI element.
            _countdown.color = UiTheme.Highlight;
            _countdownPop = 0.35f;
        }

        public void HideCountdown()
        {
            if (_countdown != null) _countdown.enabled = false;
            _countdownPop = 0.0f;
        }

        /// <summary>Brief on-screen call-out for a locally-relevant event that is not otherwise
        /// visible on the HUD.</summary>
        public void ShowToast(string text, float duration = 1.5f)
        {
            if (_toast == null) return;

            _toast.text = text;
            _toast.enabled = true;
            _toastLeft = duration;
        }

        /// <summary>
        /// ⚠️⚠️ A PULSE, NOT A STATE, AND THAT DISTINCTION IS THE WHOLE BUG. This was
        /// `visible = active` driven by whether the LATA was down, which in a real round is most
        /// of the time. A full-screen red rect left on for forty seconds does not read as
        /// feedback, it reads as the renderer being broken.
        /// </summary>
        public void SetDownedFlash(bool active)
        {
            if (_dangerFlash == null) return;

            if (!active)
            {
                _flashLeft = 0.0f;
                ApplyDangerHold();
                return;
            }

            _flashLeft = DownedFlashTime;
        }

        // -------------------------------------------------------------------

        private void Awake()
        {
            Instance = this;
            Build();
        }

        /// <summary>
        /// ⚠️⚠️ THE HUD SUBSCRIBED TO EXACTLY ONE EVENT AND `hud.gd` SUBSCRIBES TO THREE.
        /// `ShowToast` was built, styled, placed at the .tscn's own offset and then called from
        /// a single site — the local player's own score floater — so three of the four things
        /// the original announces were never announced at all. 🧑 on this build: *"theres also
        /// text for lata is back up"*, which is `hud.gd:1634`:
        ///
        ///     func _on_lata_restored() -> void:
        ///         show_toast("LATA IS BACK UP", 1.2)
        ///
        /// The two tag lines beside it (`hud.gd:1637`) were missing for the same reason and are
        /// wired here too, because they are the same fault and splitting them would leave the
        /// port announcing a reset it never announced the cause of.
        /// </summary>
        private void OnEnable()
        {
            if (GameServices.Match != null) GameServices.Match.Scored += OnScored;
            TrySubscribeRound();
        }

        private void OnDisable()
        {
            if (GameServices.Match != null) GameServices.Match.Scored -= OnScored;

            if (_roundHooked && GameServices.Round != null)
            {
                GameServices.Round.LataRestored -= OnLataRestored;
                GameServices.Round.Tagged -= OnTagged;
            }

            _roundHooked = false;
        }

        private bool _roundHooked;

        /// <summary>
        /// ⚠️ RETRIED UNTIL IT TAKES, NOT ATTEMPTED ONCE AT OnEnable. `MatchInstaller` builds this
        /// HUD as part of the same pass that stands the directors up, so `GameServices.Round` is
        /// legitimately null on the frame this component wakes. Subscribing once and giving up
        /// would leave the toasts silently unwired for the whole match, which is the same class
        /// of failure as the camera the input reader had to re-resolve.
        /// </summary>
        private void TrySubscribeRound()
        {
            if (_roundHooked || GameServices.Round == null) return;

            GameServices.Round.LataRestored += OnLataRestored;
            GameServices.Round.Tagged += OnTagged;
            _roundHooked = true;
        }

        private void OnLataRestored() => ShowToast("LATA IS BACK UP", 1.2f);

        /// <summary>
        /// ⚠️ IT SAYS SOMETHING DIFFERENT TO EACH OF THE TWO PEOPLE INVOLVED AND NOTHING TO THE
        /// OTHER TWO, exactly as `hud.gd::_on_attacker_tagged` does. A tag is the taya's one
        /// scoring verb and the attacker's worst moment; a line that read the same on all four
        /// screens would be telling two bystanders about something that did not happen to them.
        /// </summary>
        private void OnTagged(int defenderSlot, int victimSlot)
        {
            if (_local == null) return;

            if (_local.PlayerSlot == victimSlot)
                ShowToast("TAGGED  ·  BACK TO THE SAFE ZONE", 2.0f);
            else if (_local.PlayerSlot == defenderSlot)
                ShowToast($"TAG  ·  {SeatName(victimSlot)}", 1.4f);
        }

        /// <summary>
        /// ⚠️ ONLY THE LOCAL PLAYER'S OWN AWARDS POP A FLOATER, AND THE PASSIVE TICK NEVER DOES.
        /// Passive defence fires every single second of every round; toasting it would be a
        /// message that never leaves the screen and says nothing while it is there. The
        /// scoreboard already carries that number.
        /// </summary>
        private void OnScored(int slot, ScoreEvent e)
        {
            if (e == ScoreEvent.DefenseTick) return;

            // ⚠️⚠️ THE AWARD STING, WHICH SHIPPED AS A LIVE CUE WITH NO CALLER ANYWHERE. It is
            // `audio_manager.gd::_on_score_changed_audio`, and the DefenseTick guard above is
            // the same one it opens with — for the same reason, spelled out there: defence
            // fires every single second of every round, so a sound on it would be a buzzsaw and
            // would duck the music once a second on top.
            //
            // ⚠️ IT IS ABOVE THE `_local` CHECK ON PURPOSE. The TOAST is only for the local
            // player's own award, because a floater about somebody else's points is noise on
            // your screen. The SOUND is the match reacting, and the original plays it off
            // `score_changed` without asking whose slot it was.
            int points = MatchRules.PointsFor(e);
            GameServices.Audio?.PlayAtVaried("score_award", Vector3.zero,
                                             points < 0 ? 0.78f : 0.96f,
                                             points < 0 ? 0.86f : 1.04f,
                                             points < 0 ? 0.65f : 0.9f);

            if (_local == null || _local.PlayerSlot != slot) return;

            ShowToast($"{(points > 0 ? "+" : "")}{points}  {LabelOf(e)}", 1.2f);
        }

        private static string LabelOf(ScoreEvent e)
        {
            switch (e)
            {
                case ScoreEvent.LataKnocked: return "LATA DOWN";
                case ScoreEvent.Sabotage: return "SABOTAGE";
                case ScoreEvent.Tag: return "TAG";
                case ScoreEvent.TayaCampPenalty: return "CAMPING";
                case ScoreEvent.UnretrievedSlipperPenalty: return "SLIPPER IDLE";
                default: return "DEFENSE";
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE CLEAN FEED IS SPECTATOR ONLY, AND THAT IS ENFORCED RATHER THAN DOCUMENTED.
        /// 🧑 2026-07-31: *"allow option to remove everything in screen to js do record the game
        /// bcz we only added spectator for the video record"*, then explicitly: *"the remove hud
        /// is only for spectator okay, no one else."* A player in a live match must not be able
        /// to hide their own timer, status stack and charge meters by leaning on a key, which
        /// would be a competitive advantage handed out by a typo.
        ///
        /// ⚠️ IT HIDES THE ROOT, NOT EACH CHILD, AND THE PER-CHILD VERSION WAS A BUG. 🧑
        /// 2026-08-02: *"clicking H doesnt hide all huds for spectator ... theres popup huds
        /// midgame and shi"*. Walking the children once at the moment H is pressed is a
        /// SNAPSHOT, not a state: every transient here shows ITSELF later and unconditionally —
        /// the toast, the countdown, the flash, the lata card — so anything that fired after H
        /// went straight back on screen over a "clean" plate. A hidden parent cannot be
        /// out-voted by a child setting its own visibility, and it deletes the restore
        /// bookkeeping outright.
        /// </summary>
        public void EnterSpectatorMode()
        {
            _spectating = true;

            // Every gameplay element on this HUD describes a character and a spectator has
            // none, so they would draw nothing and read as a broken HUD rather than a
            // deliberate one. The timer and the scoreboard stay: those are facts about the
            // MATCH, and they are exactly what somebody watching wants.
            //
            // ⚠️⚠️ THE LATA CARD IS A MATCH FACT AND USED TO BE HIDDEN HERE, WHICH IS WRONG
            // AGAINST THE REFERENCE. `hud.gd` keeps it up for a watcher and the Godot spectator
            // screenshots show `LATA · UPRIGHT` in the corner throughout. Whether the can is
            // standing is the single thing that explains what all four players are doing at any
            // moment — no throw is legal while it is down — so it is the LAST readout to take
            // away from somebody whose whole job is watching. It describes the arena, not a
            // character, which is the line this method is drawn along.
            _crosshair.enabled = false;
            _vulnerable.enabled = false;
            _readyPrompt.enabled = false;
            _readyObjective.enabled = false;
            if (_readyPromptPlate != null) _readyPromptPlate.enabled = false;
            if (_readyObjectivePlate != null) _readyObjectivePlate.enabled = false;
            _dangerFlash.enabled = false;

            // § THE STUN FROST rides along: it is a transient like the flash above, and a
            // spectator has no stun of their own to be told about. A clean feed needs no
            // equivalent, because that path disables the whole canvas.
            ClearFrost();

            if (_stackLeft != null) _stackLeft.gameObject.SetActive(false);
            if (_stackRight != null) _stackRight.gameObject.SetActive(false);
            if (_indicators != null) _indicators.gameObject.SetActive(false);
            if (_heroDeck != null) _heroDeck.SetActive(false);
            if (_classicDeck != null) _classicDeck.SetActive(false);

            BuildSpectatorReadout();
        }

        private Text _spectatorLegend;
        private Text _spectatorStatus;
        private Text _spectatorHint;
        private Text _spectatorLiveBug;
        private CameraSystem.SpectatorCamera _spectatorCamera;
        private string _spectatorStatusShown = "";
        private bool _spectatorControlsVisible = true;

        /// <summary>
        /// The two lines along the bottom of a spectator's screen.
        /// Can be toggled on/off with [C] to avoid hogging the screen.
        /// </summary>
        private void BuildSpectatorReadout()
        {
            if (_spectatorLegend != null) return;

            _spectatorStatus = HudLabel(_root, "SpectatorStatus", 22, UiTheme.Cream,
                                        TextAnchor.MiddleCenter);
            Place(_spectatorStatus.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 108),
                  new Vector2(900, 32));

            _spectatorLegend = HudLabel(_root, "SpectatorLegend", 18, UiTheme.Cream,
                                        TextAnchor.MiddleCenter);
            Place(_spectatorLegend.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 48),
                  new Vector2(1700, 54));

            _spectatorLegend.text = CameraSystem.SpectatorCamera.ControlsText();

            _spectatorHint = HudLabel(_root, "SpectatorHint", MenuKit.MinReadableUnits, UiTheme.CreamMuted,
                                      TextAnchor.LowerRight);
            Place(_spectatorHint.rectTransform, new Vector2(1.0f, 0.0f), new Vector2(-24, 20),
                  new Vector2(310, 28));
            _spectatorHint.text = "[C] CONTROLS OVERLAY";
            _spectatorHint.enabled = true;

            _spectatorLiveBug = HudLabel(_root, "BroadcastBug", 20, UiTheme.Danger,
                                          TextAnchor.MiddleRight, 4);
            Place(_spectatorLiveBug.rectTransform, new Vector2(1.0f, 1.0f),
                  new Vector2(-24, -24), new Vector2(260, 34));
            _spectatorLiveBug.text = "● LIVE";
        }

        public void SetSpectatorControlsVisible(bool visible)
        {
            _spectatorControlsVisible = visible;
            if (_spectatorLegend != null) _spectatorLegend.gameObject.SetActive(visible);
            if (_spectatorStatus != null) _spectatorStatus.gameObject.SetActive(visible);
            if (_spectatorHint != null)
            {
                _spectatorHint.text = visible ? "[C] HIDE CONTROLS" : "[C] SHOW CONTROLS";
            }
        }

        /// <summary>
        /// ⚠️ THE CAMERA IS FOUND LAZILY. `MatchInstaller` creates the spectator and the HUD in
        /// the same Start, in an order this component must not depend on, and a watcher can also
        /// be created later by <see cref="MatchHost.EnterSpectatorMode"/>.
        /// </summary>
        private void UpdateSpectatorReadout()
        {
            if (_spectatorStatus == null || !_spectatorControlsVisible) return;

            if (_spectatorCamera == null)
                _spectatorCamera = FindFirstObjectByType<CameraSystem.SpectatorCamera>();

            string text = _spectatorCamera != null ? _spectatorCamera.StatusText() : "";

            // Same rule the clock follows: assigning Text.text reshapes the mesh whether or not
            // the characters changed, and this one is polled every frame.
            if (text == _spectatorStatusShown) return;

            _spectatorStatusShown = text;
            _spectatorStatus.text = text;

            if (_spectatorLiveBug != null)
            {
                if (text.Contains("REPLAY"))
                {
                    _spectatorLiveBug.text = "⏪ INSTANT REPLAY";
                    _spectatorLiveBug.color = UiTheme.Highlight;
                }
                else if (text.Contains("PAUSE"))
                {
                    _spectatorLiveBug.text = "⏸ TACTICAL PAUSE";
                    _spectatorLiveBug.color = UiTheme.Amber;
                }
                else
                {
                    _spectatorLiveBug.text = "● LIVE";
                    _spectatorLiveBug.color = UiTheme.Danger;
                }
            }
        }

        public bool IsCleanFeed => _cleanFeed;

        /// <summary>
        /// § WHAT THE CLEAN FEED HIDES. Anything parented here disappears with H.
        ///
        /// ⚠️⚠️ THE INTERMISSION CARD AND THE RESULT BOARD BELONG UNDER IT AND WERE SIBLINGS OF
        /// IT INSTEAD. `RoleSwapCard.tscn`'s own header records the reason in the Godot build:
        /// *"IT LIVES INSIDE `HUD.tscn` AND THAT IS WHAT MAKES THE CLEAN FEED COVER IT"*, added
        /// for 🧑 *"again make sure spectator wont see this shit if they click h (turn off
        /// huds)"*. Here both were separate GameObjects with their own canvases created by
        /// `MatchInstaller`, so `SetCleanFeed` hid the HUD and left the round-end card and the
        /// win board sitting over a cinematic feed — the exact thing the key exists to prevent,
        /// on the two screens a spectator is most likely to be filming.
        ///
        /// ⚠️ IT IS PARENTING, NOT A SECOND HIDE CALL, AND THAT DISTINCTION IS THE .gd's OWN.
        /// Both cards show THEMSELVES later and unconditionally, off an event: `SetActive(true)`
        /// from an intermission would punch straight through a flag this class had set on them.
        /// A hidden PARENT cannot be punched through, which is why Godot solved it in the scene
        /// tree rather than in the toggle.
        /// </summary>
        public Transform CleanFeedRoot => _canvas != null ? _canvas.transform : transform;

        private bool _spectating;
        private bool _cleanFeed;

        /// <summary>
        /// ⚠️ THE NAMEPLATES AND GROUND RINGS ARE NOT PART OF THIS CANVAS, AND A CLEAN FEED THAT
        /// LEAVES THEM ON IS NOT CLEAN. 🧑 2026-07-31, with a screenshot: *"turn off character
        /// labels as well as circles around player when click H, we want H turn off hud to make
        /// it cinematic so turn off that stuff."* They are world objects parented to each unit,
        /// which is exactly why the first version of the toggle missed them.
        ///
        /// ⚠️ RE-SWEPT ON EVERY TOGGLE rather than remembered, because the roster changes: a
        /// unit that respawns gets a fresh nameplate this has never seen.
        /// </summary>
        public void SetCleanFeed(bool on)
        {
            if (on == _cleanFeed) return;
            _cleanFeed = on;

            foreach (var plate in FindObjectsByType<Visual.CharacterNameplate>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                plate.gameObject.SetActive(!on);
            }

            if (_canvas != null) _canvas.gameObject.SetActive(!on);
        }

        private void Update()
        {
            // ⚠️ READ BEFORE THE LOCAL-UNIT GUARD. A spectator has no character, so anything
            // below the guard never runs for them — and they are the only person this key is
            // for. The toggle also has to keep working while the canvas is hidden, which it
            // does because this is a component callback and not a UI event.
            if (_spectating && Input.GetKeyDown(KeyCode.H)) SetCleanFeed(!_cleanFeed);
            if (_spectating && Input.GetKeyDown(KeyCode.C)) SetSpectatorControlsVisible(!_spectatorControlsVisible);

            if (GameServices.Match == null || GameServices.Round == null) return;

            float dt = Time.unscaledDeltaTime;

            // See TrySubscribeRound: the director may not exist yet on the frame this HUD wakes.
            TrySubscribeRound();

            // ⚠️⚠️ A SPECTATOR'S CLOCK AND SCOREBOARD USED TO BE FROZEN, WHICH IS THE OPPOSITE OF
            // WHAT THIS MODE IS FOR. `EnterSpectatorMode` deliberately KEEPS the timer, the
            // round line, the scoreboard and the lata card, and says why: they are facts about
            // the MATCH and they are exactly what somebody watching wants. Then `Update`
            // returned on the line below before drawing a single one of them, so all four were
            // stuck on whatever they read at install: 00:00, ROUND 1 / 4, four zeroes.
            //
            // Everything past this block reads `_local` — the watcher has no character, so those
            // rows genuinely have nothing to say and are the ones that stay off.
            if (_spectating)
            {
                UpdateTimer(dt);
                UpdateScores();
                UpdateScorePulses(dt);
                UpdateLataCard();
                UpdateToast(dt);
                UpdateCountdown(dt);
                UpdateSpectatorReadout();
                return;
            }

            if (_local == null) return;

            UpdateTimer(dt);
            UpdateScores();
            UpdateScorePulses(dt);
            UpdateLataCard();
            UpdateStatus();
            UpdateHeroDeck();
            UpdateClassicDeck(dt);
            UpdateDanger();
            UpdateToast(dt);
            UpdateCountdown(dt);
            UpdateFrost(dt);
            UpdateIndicators();
            UpdateGetUpPrompt();

            var carrier = _local.GetComponent<Carrier>();
            bool live = GameServices.Round.CanThrow(_local);
            _crosshair.enabled = live || (carrier != null && carrier.IsCharging);

            // R-28 — the two in-world markers that answer "what am I doing" take the LOCAL
            // player's role colour: the crosshair they aim with, and the edge arrow pointing at
            // the lata. Driven off the local unit, not off whichever side defends.
            Color role = _local.IsDefender ? UiTheme.Defense : UiTheme.Offense;
            _crosshair.color = role;
            _indicators?.SetCanArrowColour(role);

            if (carrier != null && carrier.IsCharging)
            {
                _crosshair.fontSize = 22;
                var lata = GameServices.Round.Lata;

                if (lata != null && !lata.IsUpright)
                {
                    _crosshair.text = "LATA DOWN\nHOLDING CHARGE";
                    _crosshair.color = UiTheme.Offense;
                }
                else if (lata != null && lata.IsProtected)
                {
                    _crosshair.text = $"LATA PROTECTED\n{lata.ProtectionLeft:0.0}s";
                    _crosshair.color = UiTheme.Defense;
                }
                else
                {
                    float spin = carrier.CurrentPektusSpin;
                    float magnitude = Mathf.Abs(spin);
                    if (magnitude > 0.08f)
                    {
                        string arrow = magnitude >= 0.85f ? "◀◀◀" : magnitude >= 0.45f ? "◀◀" : "◀";
                        if (spin > 0.0f) arrow = arrow.Replace('◀', '▶');
                        string bank = magnitude >= Balance.PektusBankSpinThreshold ? "\nBANK READY" : "";
                        _crosshair.text = $"{arrow}  PEKTUS {Mathf.RoundToInt(magnitude * 100.0f)}%{bank}";
                        _crosshair.color = UiTheme.Highlight;
                    }
                    else
                    {
                        _crosshair.text = "+\nPEKTUS 0%";
                    }
                }
            }
            else
            {
                _crosshair.fontSize = 34;
                _crosshair.text = "+";
            }

            _vulnerable.enabled = _local.IsTaggable();

            if (_hitmarkerTimer > 0.0f)
            {
                _hitmarkerTimer -= dt;
                float t = Mathf.Clamp01(_hitmarkerTimer / 0.28f);
                _hitmarker.rectTransform.localScale = Vector3.one * (1.0f + 0.6f * t);
                Color c = _hitmarker.color;
                c.a = t;
                _hitmarker.color = c;
                if (_hitmarkerTimer <= 0.0f) _hitmarker.enabled = false;
            }
        }

        // -------------------------------------------------------------------

        private int _secondsShown = -1;
        private int _urgent = -1;

        private void UpdateTimer(float dt)
        {
            float left = Mathf.Max(0.0f, GameServices.Round.TimeLeft);

            // The announcer's clock warnings ride the same value the clock draws, so "thirty
            // seconds" is spoken on the frame the HUD first shows 30.
            GameServices.Voice?.TickClock(left);

            // ⚠️ ONLY ON THE SECOND, NOT EVERY FRAME. The clock has one-second resolution, and
            // assigning `Text.text` invalidates the mesh and queues a reshape whether or not the
            // characters changed. Measured in the Godot build: the HUD was 0.20 ms of a 0.47 ms
            // whole-frame script budget, mostly from three unconditional writes.
            int t = Mathf.CeilToInt(left);
            if (t != _secondsShown)
            {
                _secondsShown = t;
                _timer.text = $"{t / 60:00}:{t % 60:00}";
            }

            // ⚠️ THE CLOCK GOES AMBER UNDER PRESSURE AND PULSES UNDER TEN. Red means destructive
            // or out of bounds everywhere else in this palette, and a timer running out is
            // neither: it is the round ending normally, for everybody, on schedule.
            int want = left <= 10.0f ? 3
                     : left <= 15.0f ? 2
                     : left <= 30.0f ? 1
                     : 0;

            if (want != _urgent)
            {
                _urgent = want;

                // ⚠️ BACK TO AMBER, NOT TO THE VARIATION'S OWN COLOUR. Falling through would
                // give the near-white the wood restyle replaced, so the timer would go white
                // the moment it climbed back over 15 s.
                _timer.color = want >= 2 ? UiTheme.Highlight : UiTheme.Amber;
                _timer.fontSize = want >= 3 ? 52 : 44;

                if (_timerCard != null)
                {
                    Color rim = want >= 2 ? UiTheme.Highlight
                              : want == 1 ? UiTheme.Amber
                              : UiTheme.WoodEdge;
                    _timerCard.sprite = GodotTheme.Box(UiTheme.WoodDark, rim,
                                                       GodotTheme.WoodBorderWidth,
                                                       GodotTheme.WoodCornerRadius);
                }
            }

            // Pressure grows in three readable bands. The clock gets physically harder to
            // ignore without flashing role colours across the playfield.
            float scale = 1.0f;
            if (want == 1)
                scale += (Mathf.Sin(Time.unscaledTime * 3.0f) * 0.5f + 0.5f) * 0.025f;
            else if (want == 2)
                scale += (Mathf.Sin(Time.unscaledTime * 5.0f) * 0.5f + 0.5f) * 0.065f;
            else if (want == 3)
                scale += (Mathf.Sin(Time.unscaledTime * 8.0f) * 0.5f + 0.5f) * 0.12f;

            if (_timerCardRt != null) _timerCardRt.localScale = Vector3.one * scale;

            // ⚠️ maxi(round, 1). `MatchDirector.RoundNumber` is 0 until the match starts, and
            // the ready-up window happens BEFORE that: the HUD read "ROUND 0 / 4" over the first
            // thing a player ever sees. The .gd has clamped this since the format was written.
            int round = Mathf.Max(1, GameServices.Match.RoundNumber);

            if (GameServices.Match.IsWarmupBuffer)
            {
                _round.text = "WARMUP / PRACTICE BUFFER   ·   SCORES PAUSED";
                _timer.color = UiTheme.Highlight;
                _urgent = -1;
                if (_timerPressure != null) _timerPressure.enabled = false;
            }
            else
            {
                _round.text = $"ROUND {round} / {GameServices.Match.TotalRounds}   ·   DEFENDER: {SeatName(GameServices.Match.DefenderSlot)}";

                if (_timerPressure != null)
                {
                    bool livePressure = GameServices.Round.RoundActive && want > 0;
                    _timerPressure.enabled = livePressure;

                    if (livePressure)
                    {
                        string action = _local == null
                            ? "EVERY SECOND COUNTS"
                            : _local.IsDefender ? "DEFEND THE LATA" : "ATTACK NOW";

                        _timerPressure.text = want == 1
                            ? "PRESSURE BUILDING"
                            : want == 2
                                ? $"FINAL PUSH  ·  {action}"
                                : $"LAST 10  ·  {action}";
                        _timerPressure.color = want >= 3 ? UiTheme.Cream : UiTheme.Highlight;
                        _timerPressure.rectTransform.localScale = Vector3.one
                            * (1.0f + (want >= 2
                                ? (Mathf.Sin(Time.unscaledTime * 6.0f) * 0.5f + 0.5f) * 0.055f
                                : 0.0f));
                    }
                }
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE NAME FOR A SEAT WHEN ONLY A SLOT NUMBER IS IN HAND. 🧑 2026-08-02: *"make
        /// sure the bot names show up everywhere they have to / Not p1 p2 p3 p4"*. Three rows
        /// built their own "P%d" out of a slot and never asked the character at all, so they
        /// kept printing P2 after bots learned their names. One function now, for the same
        /// reason `DisplayName()` is one function: a seat cannot be called two different things
        /// on two rows of one screen.
        /// </summary>
        private static string SeatName(int slot)
        {
            var who = GameServices.Round?.PlayerAt(slot);
            return who != null ? who.DisplayName() : $"P{slot + 1}";
        }

        private string _scoreStamp = "";

        /// <summary>
        /// ⚠️ SORTED BY SCORE, NOT BY SEAT, AND THE BADGE SAYS WHO IS DEFENDING. Both halves
        /// matter: the ranking is the story of the match, and the taya marker is the only thing
        /// on screen that explains why one player is behaving completely differently from the
        /// other three.
        /// </summary>
        private void UpdateScores()
        {
            var m = GameServices.Match;

            // ⚠️⚠️ THE NAMES ARE PART OF THE STAMP, AND LEAVING THEM OUT WAS A REAL BUG. A seat
            // changing hands renames a row without touching a score, so a board keyed on scores
            // alone went on showing the name of the human who left until somebody scored.
            var sb = new System.Text.StringBuilder();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                sb.Append(m.ScoreFor(slot)).Append(':').Append(SeatName(slot)).Append('|');

            sb.Append(m.DefenderSlot);

            string stamp = sb.ToString();
            if (stamp == _scoreStamp) return;
            _scoreStamp = stamp;

            int[] order = m.Ranking();
            int mine = _local != null ? _local.PlayerSlot : -1;

            for (int i = 0; i < _scoreNames.Length; i++)
            {
                if (_scoreNames[i] == null) continue;

                int slot = order[i];
                bool isTaya = slot == m.DefenderSlot;
                int scoreNow = m.ScoreFor(slot);

                if (_scoresInitialised)
                {
                    int delta = scoreNow - _lastScoreBySlot[slot];
                    if (Mathf.Abs(delta) >= 20) _scorePulses[i] = 0.34f;
                }
                _lastScoreBySlot[slot] = scoreNow;

                _scoreNames[i].text = SeatName(slot);
                bool isMine = slot == mine;
                _scoreMarks[i].text = (isTaya ? "DEFENDER" : "ATTACKER")
                    + (isMine ? "  ·  YOU" : "");
                _scoreValues[i].text = scoreNow.ToString();

                // ⚠️ NO LEADING BULLET — THE COLOUR IS THE MARK. 🧑 2026-08-02: *"the arrow
                // makes the names of the characters not aligned"*. The prefix was one character
                // on your own row against two spaces on every other, so all four names started
                // at a different x and the column read as ragged. Highlighting the row says the
                // same thing and costs no width.
                Color colour = isTaya ? UiTheme.Defense : UiTheme.Offense;

                _scoreNames[i].color = colour;
                _scoreMarks[i].color = colour;
                _scoreValues[i].color = colour;

                if (_scoreRoleRails[i] != null) _scoreRoleRails[i].color = colour;
                if (_scoreRowPlates[i] != null)
                {
                    float alpha = isMine ? 0.22f : 0.10f;
                    _scoreRowPlates[i].color = new Color(colour.r, colour.g, colour.b, alpha);
                }
            }

            _scoresInitialised = true;
        }

        private void UpdateScorePulses(float dt)
        {
            for (int i = 0; i < _scoreRows.Length; i++)
            {
                var row = _scoreRows[i];
                if (row == null) continue;

                _scorePulses[i] = Mathf.Max(0.0f, _scorePulses[i] - dt);
                float ratio = Mathf.Clamp01(_scorePulses[i] / 0.34f);
                float punch = Mathf.Sin(ratio * Mathf.PI) * 0.11f;
                row.localScale = Vector3.one * (1.0f + punch);
            }
        }

        private int _lataUprightShown = -1;
        private string _lataHintShown = "￿";

        /// <summary>
        /// `LataCard` is the one readout every player needs whatever their role: the throw is
        /// illegal while the lata is down, and the passive score only ticks while it is up.
        /// </summary>
        private void UpdateLataCard()
        {
            var lata = GameServices.Round.Lata;

            if (lata == null || !GameServices.Round.RoundActive)
            {
                _lataCard.gameObject.SetActive(false);
                if (_lataAlert != null) _lataAlert.enabled = false;
                return;
            }

            _lataCard.gameObject.SetActive(true);

            if (_lataUprightShown != (lata.IsUpright ? 1 : 0))
            {
                _lataUprightShown = lata.IsUpright ? 1 : 0;
                _lataLabel.text = lata.IsUpright ? "LATA  ·  UPRIGHT" : "⚠  LATA DOWN  ⚠";
                _lataLabel.color = lata.IsUpright ? UiTheme.Defense : UiTheme.Offense;
            }

            bool down = !lata.IsUpright;
            float canPulse = down
                ? 1.0f + (Mathf.Sin(Time.unscaledTime * 7.0f) * 0.5f + 0.5f) * 0.10f
                : 1.0f;
            _lataCard.rectTransform.localScale = Vector3.one * canPulse;

            if (_lataAlert != null)
            {
                _lataAlert.enabled = down;
                if (down)
                {
                    _lataAlert.text = _local == null
                        ? "LATA DOWN"
                        : _local.IsDefender
                            ? "LATA DOWN  ·  RESET IT NOW"
                            : "LATA DOWN  ·  RETRIEVE NOW";
                    _lataAlert.rectTransform.localScale = Vector3.one
                        * (1.0f + (Mathf.Sin(Time.unscaledTime * 8.0f) * 0.5f + 0.5f) * 0.09f);
                }
            }

            // The second line is what THIS player can do about it, which differs by role and is
            // the whole reason the card is not just a coloured light.
            string line = "";

            if (lata.IsProtected)
            {
                line = $"PROTECTED  {lata.ProtectionLeft:0.0}s";
            }
            else if (_local == null)
            {
                line = lata.IsUpright ? "TAYA MAY TAG" : "ATTACKERS MAY RETRIEVE";
            }
            else if (_local.IsDefender)
            {
                if (!lata.IsUpright)
                {
                    var carrier = _local.GetComponent<Carrier>();
                    float progress = carrier != null ? carrier.ChannelRatio : 0.0f;

                    line = progress > 0.0f
                        ? $"RESETTING  {Mathf.RoundToInt(progress * 100.0f)}%"
                        : "HOLD E IN THE RING";
                }
                else if (GameServices.Round.IsTayaCampWarningActive)
                {
                    float left = Mathf.Max(0.0f, Balance.TayaCampGracePeriod
                        - GameServices.Round.TayaCampSeconds);
                    line = left > 0.0f
                        ? $"LEAVE CAN RING  {left:0.0}s"
                        : "CAMPING  ·  DEFENSE SCORE PAUSED";
                }
            }
            else if (!_local.HoldingSlipper)
            {
                float idle = GameServices.Round.AttackerIdleSeconds(_local.PlayerSlot);
                if (TournamentRules.IsSlipperWarning(idle))
                {
                    float left = Mathf.Max(0.0f, Balance.SlipperUnretrievedGracePeriod - idle);
                    line = left > 0.0f
                        ? $"FETCH SLIPPER  {left:0.0}s"
                        : "FETCH SLIPPER  ·  -5 / SECOND";
                }
                else
                {
                    line = "RETRIEVE A SLIPPER";
                }
            }
            else if (_local.IsInsideBox())
            {
                line = "GET OUT OF THE BOX TO THROW";
            }

            if (line == _lataHintShown) return;

            _lataHintShown = line;
            _lataHint.text = line;
            _lataHint.enabled = line != "";
        }

        /// <summary>
        /// ⚠️⚠️ TWO STACKS, NOT ONE. Status effects on the LEFT, ability cooldowns on the RIGHT.
        /// Both used to come out of one list into one centred stack, which meant "I am STUNNED"
        /// and "my shove is recharging" competed for the same four rows — and the two are read
        /// at different moments and mean different things. One is what is being done TO you, the
        /// other is what you may do next.
        ///
        /// ⚠️ THE SUFFIX IS THE ROUTING KEY, deliberately the LABEL rather than a new field.
        /// "SHOVE CD", "LUNGE CD" and "THROW CD" all end the same way, so a cooldown added later
        /// routes correctly the day it is added.
        /// </summary>
        private void UpdateStatus()
        {
            StatusStack.Collect(_local, _local.GetComponent<Carrier>(),
                                _local.GetComponent<CombatVerbs>(), _rows);

            _states.Clear();
            _cooldowns.Clear();

            foreach (var row in _rows)
            {
                // ⚠️ VULNERABLE IS FILTERED OUT HERE, NOT REMOVED FROM THE COLLECTOR. The
                // crosshair says it in words now; the RULE stays where it was, and the list is
                // still the honest account of what is live on this body.
                if (row.Label == "VULNERABLE") continue;

                if (row.Label.EndsWith(" CD")) _cooldowns.Add(row);
                else _states.Add(row);
            }

            FollowScoreboard();
            Fill(_stackLeft, _rowsLeft, _states, false);
            Fill(_stackRight, _rowsRight, _cooldowns, true);
        }

        /// <summary>
        /// ⚠️⚠️ THE LEFT STACK SAT ON TOP OF THE SCOREBOARD. Reported with a screenshot of
        /// "STUNNED 0.2s" drawn straight through the P2 and P4 score rows. Both are anchored
        /// top-left and the stack's margin was written when it was centred under the timer.
        /// Derived from the board's real height rather than nudged to a bigger number, because
        /// a second literal would be wrong again the next time a row is added or a name wraps.
        /// </summary>
        private void FollowScoreboard()
        {
            if (_stackLeft == null) return;

            float top = StatusMargin.y;

            if (_scoreboardRt != null)
                top = Mathf.Max(top, 28.0f + _scoreboardRt.rect.height + StatusUnderBoardGap);

            _stackLeft.anchoredPosition = new Vector2(StatusMargin.x, -top);
        }

        private void Fill(RectTransform root, List<StatusWidget> widgets, List<StatusRow> effects,
                          bool rightSide)
        {
            int wanted = Mathf.Min(effects.Count, StatusRowLimit);

            while (widgets.Count < wanted) widgets.Add(BuildStatusRow(root, rightSide));

            for (int i = 0; i < widgets.Count; i++)
            {
                var w = widgets[i];

                if (i >= wanted)
                {
                    w.Root.transform.localScale = Vector3.one;
                    w.Root.SetActive(false);
                    continue;
                }

                var e = effects[i];
                Color colour = ColourFor(e.Label);

                w.Root.SetActive(true);

                // ⚠️ A ROW WITH NO COUNTDOWN IS A REAL CASE, NOT A BUG. An effect that lasts as
                // long as the player chooses reports no time and draws as a solid bar; printing
                // "0.0s" would read as already expired.
                w.Label.text = e.Timed && e.Remaining > 0.0f
                    ? $"{e.Label}  {e.Remaining:0.0}s"
                    : e.Label;

                w.Label.color = colour;
                w.Fill.color = colour;
                w.Fill.fillAmount = e.Timed && e.Remaining > 0.0f
                    ? Mathf.Clamp01(e.Remaining / Mathf.Max(0.01f, e.Total))
                    : 1.0f;

                // The final quarter gives one restrained pulse. This is especially important
                // for stun and vulnerability, where the end of the effect changes what the
                // player can safely do before they have time to read the number again.
                float ratio = e.Timed && e.Total > 0.0f
                    ? Mathf.Clamp01(e.Remaining / e.Total)
                    : 1.0f;
                float pulse = ratio < 0.25f
                    ? 1.0f + (Mathf.Sin(Time.unscaledTime * 10.0f) * 0.5f + 0.5f) * 0.035f
                    : 1.0f;
                w.Root.transform.localScale = Vector3.one * pulse;
            }
        }

        /// <summary>
        /// ⚠️ THE COLOUR IS THE MESSAGE. Three bands, and they mean three different things to
        /// somebody glancing at them mid-fight: DANGER is "you cannot act at all", AMBER is
        /// "something is spent", HIGHLIGHT is "a countdown you are waiting on".
        /// </summary>
        private static Color ColourFor(string label)
        {
            switch (label)
            {
                case "STUNNED":
                case "DOWNED":
                case "VULNERABLE":
                    return UiTheme.Danger;

                case "FATIGUED":
                    return UiTheme.Amber;

                default:
                    return UiTheme.Highlight;
            }
        }

        private void UpdateDanger()
        {
            // ⚠️ PER-SCREEN, WHICH IS THE HALF THAT MAKES IT INFORMATION. A defender sees their
            // can is down, an attacker sees they are catchable, and neither sees the other's
            // warning. A vignette everybody gets at the same time tells nobody anything.
            bool want;

            if (_local.IsDefender)
            {
                var can = GameServices.Round.Lata;
                want = can != null && !can.IsUpright && GameServices.Round.RoundActive;
            }
            else
            {
                // ⚠️ `IsTaggable()` IS THE WHOLE CONDITION and is deliberately the same function
                // the tag asks. "Until they cross back out or get tagged" is not two extra
                // checks: it is exactly what that function stops returning true for.
                want = _local.IsTaggable();
            }

            if (want != _dangerHeld)
            {
                _dangerHeld = want;
                ApplyDangerHold();
            }

            if (_flashLeft <= 0.0f) return;

            _flashLeft = Mathf.Max(0.0f, _flashLeft - Time.unscaledDeltaTime);

            float a = Mathf.Lerp(0.0f, DownedFlashPeak, _flashLeft / DownedFlashTime);

            _dangerFlash.enabled = true;
            _dangerFlash.color = new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b,
                                           Mathf.Max(a, _dangerHeld ? DangerHoldAlpha : 0.0f));

            // ⚠️ HANDS BACK TO THE HELD STATE rather than hiding outright — the knockdown pulse
            // and the defender's "your can is down" hold fire on the same frame, and clearing
            // here would cancel the hold the pulse announced.
            if (_flashLeft <= 0.0f) ApplyDangerHold();
        }

        // -------------------------------------------------------------------
        // ⚠️⚠️ § THE STUN FROST — THE SCREEN HALF. 🧑 2026-08-06, on the Godot build: *"can we
        // have like a frost effect to indicate that an attacker is stunned after getting
        // tagged?"*, with a reference image of an icy frame around a clear centre.
        //
        // ⚠️ IT IS THE VICTIM'S SCREEN ONLY, and the body half in `CharacterVisual` is what
        // everybody else sees. That split is `UpdateDanger`'s own rule applied again: a vignette
        // everybody gets at the same time tells nobody anything. The taya spent their one
        // scoring verb on that tag and needs to watch the attacker freeze; the attacker needs to
        // know why their controls stopped answering. Two messages, two places.
        //
        // ⚠️⚠️ AND IT IS WHY THE SHAPE MATTERS RATHER THAN THE ALPHA. `SetDownedFlash` records
        // the measurement that governs this whole file: a held full-screen tint reads as the
        // renderer being broken. That is why held states sit at `DangerHoldAlpha` 0.16 while
        // only a 0.45 s pulse goes to 0.45. A tag stun is FIVE SECONDS: too long for a flash,
        // far too punchy to hold at flash strength, and too important to drop to 0.16, because
        // it is the single biggest moment in the defender's game. The reference resolves it by
        // putting the opacity where the player is not looking, so there is no alpha ceiling here
        // at all. The shader's own coverage is the dial and the centre stays readable at 1.0.
        //
        // ⚠️ IT RECEDES, so the ice visibly retreats toward the frame as the five seconds run
        // out. That is the accessible-status requirement to signal when an effect is ending, in
        // a channel the player's eyes are already on, unlike the STUNNED countdown bar in the
        // status stack which is correct and easy to miss.
        // -------------------------------------------------------------------

        public const float FrostRampIn = 0.14f;
        public const float FrostRampOut = 0.5f;

        /// <summary>Coverage holds at full until the stun has this long left, then thaws.</summary>
        public const float FrostThawTime = 1.6f;

        private static readonly int FrostCoverageId = Shader.PropertyToID("_Coverage");
        private static readonly int FrostAspectId = Shader.PropertyToID("_Aspect");

        private void UpdateFrost(float dt)
        {
            if (_frostVignette == null) return;

            float target = 0.0f;

            // ⚠️ THE TRIP IS EXCLUDED HERE FOR THE SAME REASON AS THE BODY HALF, and both halves
            // have to agree or a fall would frost the screen while the character on it did not.
            // `CharacterMotor.ApplyTrip` staggers as well as tripping, so `IsStunned` alone
            // caught every stumble. See `CharacterVisual.ProcessFrost`.
            if (_local != null && _local.IsStunned && !_local.IsTripped)
            {
                // ⚠️ THE LOCAL CHARACTER IS ALWAYS THE ONE THIS PEER SIMULATES, so unlike the
                // body half this side can always trust the countdown. `StunLeft` reads 0 only
                // for a body somebody else is running, and that is never this one.
                float left = _local.StunLeft;
                target = left < FrostThawTime ? Mathf.Clamp01(left / FrostThawTime) : 1.0f;
            }

            float rate = target > _frostCoverage ? FrostRampIn : FrostRampOut;
            _frostCoverage = Mathf.MoveTowards(_frostCoverage, target, dt / Mathf.Max(rate, 0.001f));

            // Disabled outright at zero rather than left drawing a fully transparent full-screen
            // quad every frame for the whole match.
            _frostVignette.enabled = _frostCoverage > 0.001f;
            if (!_frostVignette.enabled) return;

            _frostMaterial.SetFloat(FrostCoverageId, _frostCoverage);

            // ⚠️ THE SHADER CANNOT WORK THIS OUT FOR ITSELF. UV is 0..1 on both axes whatever
            // the window's shape, so without the real ratio the frost band is ~1.8x thicker in
            // pixels down the sides than across the top on 16:9. Pushed every frame rather than
            // on a resize event, because the window can also change shape via the fullscreen
            // toggle, which fires no resize on this rect.
            var size = _frostVignette.rectTransform.rect.size;
            if (size.y > 0.0f) _frostMaterial.SetFloat(FrostAspectId, size.x / size.y);
        }

        /// <summary>Clears the frost outright. The spectator has no stun of their own to be told
        /// about, and a clean feed shows no HUD at all.</summary>
        private void ClearFrost()
        {
            _frostCoverage = 0.0f;
            if (_frostVignette != null) _frostVignette.enabled = false;
        }

        private void ApplyDangerHold()
        {
            if (_dangerFlash == null) return;

            _dangerFlash.enabled = _dangerHeld;
            _dangerFlash.color = new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b,
                                           _dangerHeld ? DangerHoldAlpha : 0.0f);
        }

        /// <summary>
        /// "Hammer this key to get up", while a trip is still answerable.
        ///
        /// ⚠⚠ A MECHANIC NOBODY IS TOLD ABOUT IS NOT A MECHANIC. The mash exists because a
        /// trip used to be the one piece of dead time in the game a player could not answer, and
        /// a player who does not know they can answer it is in exactly the position the change
        /// was made to fix. This is the cheapest possible teach: it appears only while it is
        /// true, and it disappears the instant pressing stops buying anything.
        ///
        /// ⚠️ THE KEY COMES FROM THE LIVE BINDING, NEVER FROM A LITERAL. `docs/VISION.md`
        /// § 3: a screen that teaches the wrong key is worse than one that teaches none, and
        /// this key is rebindable in the settings panel like every other.
        ///
        /// ⚠️ IT IS PUSHED THROUGH THE TOAST EVERY FRAME RATHER THAN SET ONCE. `ShowToast`
        /// counts down and switches itself off, so a one-shot call would vanish half a second
        /// into a two and a half second fall. Refreshing it holds it up for exactly as long as
        /// the state lasts, and costs one string comparison when nothing has changed.
        /// </summary>
        private void UpdateGetUpPrompt()
        {
            if (_getUpCard == null) return;

            // ⚠️ THE CARD FOLLOWS `IsTripped`, NOT `CanMashUp`. The old prompt returned early
            // once the mash hit `Balance.MinTripDown`, so the feedback vanished for the last
            // 0.9 s of every fall: the player was still on the floor, still unable to act, and
            // the screen had gone quiet again. That gap is most of what "nothing happened" was.
            if (_local == null || !_local.IsTripped)
            {
                if (_getUpCard.gameObject.activeSelf)
                {
                    _getUpCard.gameObject.SetActive(false);
                    _getUpShown = "";
                    if (_getUpBarRt != null) _getUpBarRt.localScale = Vector3.one;
                }
                return;
            }

            if (!_getUpCard.gameObject.activeSelf) _getUpCard.gameObject.SetActive(true);

            // Two phases, because the fall genuinely has two. While there is slack above the
            // floor, pressing buys time and the prompt asks for presses. Below it nothing more
            // can be bought and the prompt stops asking, which is the rule `CanMashUp` already
            // states: a prompt that keeps demanding presses it will not honour teaches the
            // player that mashing does not work.
            bool buying = _local.CanMashUp;

            string text = buying
                ? "MASH [" + KeyLabel("Jump") + "] TO GET UP"
                : "GETTING UP";

            // ⚠️ THE STRING IS ONLY REBUILT WHEN IT CHANGES. A HUD string rebuilt every frame
            // once cost the 6x behaviour probe an eighth of its frames and most of its physics
            // steps; that finding is recorded in `CLAUDE.md` § 7.1 and this is the same shape of
            // code in the same file.
            if (text != _getUpShown)
            {
                _getUpShown = text;
                _getUpLabel.text = text;
                _getUpLabel.color = buying ? UiTheme.Cream : UiTheme.Amber;
            }

            // ⚠️⚠️ THE BAR SPANS THE WHOLE FALL, NOT THE MASHABLE PART OF IT. Filling only
            // across `TripTotal` down to `MinTripDown` would slam to 100 per cent and then sit
            // there for the 0.9 s floor, which reads as a bar that has finished while the player
            // is demonstrably still down. Measuring against the full trip means the last stretch
            // keeps moving on its own, so "about to be done" stays true right to the end.
            float total = Mathf.Max(0.01f, _local.TripTotal);
            _getUpFill.fillAmount = Mathf.Clamp01(1.0f - _local.TripLeft / total);

            // Amber once the presses have done all they can, so the colour change and the
            // wording agree that the player has stopped being able to help.
            _getUpFill.color = buying ? UiTheme.Offense : UiTheme.Amber;

            // ⚠️ THE GOLD SEGMENT IS WHAT THE PRESSES BOUGHT, MEASURED AGAINST THE SAME
            // DENOMINATOR, so the two read as one bar with the player's share at the front
            // rather than as two competing bars.
            if (_getUpMashFill != null)
                _getUpMashFill.fillAmount = Mathf.Clamp01(_local.MashRemoved / total);

            // ⚠️⚠️ THE POP IS THE ONLY THING THAT SEPARATES A DEAD PRESS FROM A REAL ONE.
            // `Combat.MashRecover` refuses a press inside `Balance.MashCooldown` and changes
            // nothing, so without this a player mashing above 10 Hz watched most of their
            // presses vanish and read the rate cap as a punishment for mashing. A press that
            // counted moves the bar; the pop makes that visible even when the movement is
            // 0.20 s out of 2.50.
            if (_getUpBarRt != null)
            {
                float sincePress = Time.time - _local.LastMashAcceptedTime;
                float pop = Mathf.Clamp01(1.0f - sincePress / MashPopSeconds);
                _getUpBarRt.localScale = new Vector3(1.0f, 1.0f + 0.35f * pop * pop, 1.0f);
            }
        }

        private void UpdateToast(float dt)
        {
            if (_toastLeft <= 0.0f) return;

            _toastLeft -= dt;
            if (_toastLeft <= 0.0f) _toast.enabled = false;
        }

        private void UpdateCountdown(float dt)
        {
            if (_countdownPop <= 0.0f || _countdownRt == null) return;

            _countdownPop = Mathf.Max(0.0f, _countdownPop - dt);

            // Pops in oversize and settles back, which is the whole effect at this size.
            float k = _countdownPop / 0.35f;
            _countdownRt.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.8f, k * k);
        }

        /// <summary>
        /// The screen-edge arrows. Resolved here rather than inside the indicator, because the
        /// HUD already works out the local unit once a frame and a second scan would be a second
        /// answer to the same question.
        /// </summary>
        private void UpdateIndicators()
        {
            if (_indicators == null) return;

            var carrier = _local.GetComponent<Carrier>();

            // Your own slipper is the one that answers to your seat. `OwnerSlot` is what makes
            // "yours" well-defined at all.
            Transform mine = null;
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude,
                                                         FindObjectsSortMode.None))
            {
                if (s.OwnerSlot != _local.PlayerSlot) continue;
                mine = s.transform;
                break;
            }

            var lata = GameServices.Round?.Lata;
            _indicators.UpdateArrows(_local, carrier, mine, lata != null ? lata.transform : null);
        }

        // -------------------------------------------------------------------
        // BUILD. The arrangement and every offset are `HUD.tscn`'s.
        // -------------------------------------------------------------------

        private void Build()
        {
            var canvasGo = new GameObject("HudCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // ⚠️ MATCH ON HEIGHT, like every other screen, or the HUD drifts against the menus
            // it hands over from on anything that is not 16:9.
            scaler.matchWidthOrHeight = 1.0f;
            AspectSafeCanvas.Apply(scaler);

            canvasGo.AddComponent<GraphicRaycaster>();

            _root = canvasGo.GetComponent<RectTransform>();

            // ⚠️ THE BUILD STAMP IS IN-MATCH TOO, not only on the menus. `hud.gd::_ready` calls
            // `GameVersion.attach_to(self, true)` for the same reason the menus carry it: a
            // screenshot or a clip of a bug is only actionable if it says which build it came
            // from, and the frames people send are gameplay frames.
            GameVersion.AttachTo(_root, over3d: true);

            BuildDangerFlash();
            BuildFrostVignette();
            BuildScoreboard();
            BuildClock();
            BuildLataCard();
            BuildGetUpCard();
            BuildStatusStacks();
            BuildHeroDeck();
            _inspect = AbilityInspectPanel.Create(_root);
            BuildClassicDeck();
            BuildFloatingText();
            BuildCrosshair();

            // Its own object, deliberately: the arrows are positioned from screen centre in raw
            // pixels, and putting them under the scaled HUD canvas would move them.
            var indicatorGo = new GameObject("OffscreenIndicators");
            indicatorGo.transform.SetParent(transform, false);
            _indicators = indicatorGo.AddComponent<OffscreenIndicators>();
        }

        /// <summary>
        /// Full-screen, behind everything, and never a raycast target.
        ///
        /// ⚠️⚠️ IT CARRIES A VIGNETTE MATERIAL, AND SHIPPING IT WITHOUT ONE IS WHY THE TAYA'S
        /// SCREEN WAS *"just red"*. `HUD.tscn` puts `downed_vignette.gdshader` on this rect and
        /// this was a bare `Image` with a flat colour, so a ramp that is meant to be clear
        /// through the middle of the frame was a uniform wash over all of it — held, for a
        /// defender, through most of a round.
        ///
        /// See `DownedVignette.shader`: it is the same falloff, and the level still comes off
        /// this Image's own colour so `ApplyDangerHold` and the knockdown pulse are unchanged.
        /// </summary>
        private void BuildDangerFlash()
        {
            var go = new GameObject("DownedFlash");
            go.transform.SetParent(_root, false);

            _dangerFlash = go.AddComponent<Image>();
            _dangerFlash.color = new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b, 0.0f);
            _dangerFlash.raycastTarget = false;
            _dangerFlash.enabled = false;

            // ⚠️ AN OWNED INSTANCE, for the same reason the frost keeps one: a second HUD in a
            // PlayMode test running beside this one must not share a material with it.
            var shader = Shader.Find("TumbangPreso/DownedVignette");

            if (shader != null)
            {
                _dangerMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                _dangerFlash.material = _dangerMaterial;
            }
            else
            {
                Debug.LogWarning("[HUD] TumbangPreso/DownedVignette is missing, so the danger " +
                                 "tint will draw as a flat full-screen rect.");
            }

            MenuKit.Stretch(_dangerFlash.rectTransform);
        }

        private Material _dangerMaterial;

        /// <summary>
        /// § THE STUN FROST — the screen half, for the player who is stunned.
        ///
        /// ⚠️ A SEPARATE IMAGE FROM `DownedFlash`, NOT A SECOND MODE OF IT. The two can be live
        /// on the same frame and mean different things: an attacker inside the box is VULNERABLE
        /// (red hold), and the moment they are tagged they become STUNNED (frost). Sharing one
        /// rect would make the tag cancel its own warning colour, and `ApplyDangerHold` would
        /// fight the frost for the same alpha.
        ///
        /// ⚠️ BUILT AFTER `DownedFlash`, so the ice sits over the red rather than under it. A
        /// tagged attacker stops being taggable, so the red is on its way out on the same frame;
        /// the frost being on top is what makes that handover read as one event.
        /// </summary>
        private void BuildFrostVignette()
        {
            var shader = Shader.Find("TumbangPreso/FrostVignette");
            if (shader == null) return;

            var go = new GameObject("FrostVignette");
            go.transform.SetParent(_root, false);

            // ⚠️ `hideFlags` AND AN OWNED INSTANCE. `Shader.Find` plus `new Material` gives this
            // HUD its own copy, so the coverage this canvas writes cannot leak into a second HUD
            // in a PlayMode test running beside it.
            _frostMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };

            _frostVignette = go.AddComponent<Image>();
            _frostVignette.material = _frostMaterial;
            _frostVignette.raycastTarget = false;
            _frostVignette.enabled = false;

            MenuKit.Stretch(_frostVignette.rectTransform);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Dispose(_frostMaterial);
            Dispose(_dangerMaterial);
        }

        private static void Dispose(Material m)
        {
            if (m == null) return;

            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }

        /// <summary>
        /// Top-left, at the .tscn's 16,28: SCORES over one row per seat, on a WOOD panel.
        ///
        /// ⚠️ ONE ROW PER SEAT, NOT ONE BLOCK OF TEXT. A single label with spaces in it cannot
        /// right-align the numbers, so the scores wander with the length of the name above them
        /// and the column stops reading as a column.
        ///
        /// ⚠️ AND "TAYA" IS ITS OWN CELL. 🧑 2026-08-02: *"inday taya makes it look like inday
        /// taya is her name"*. Two spaces are not a grammar: every roster name is uppercase,
        /// several are two words, and one character is called INDAY, so at the level of pixels
        /// "INDAY TAYA" IS a two-word name. A separate label can differ in the three ways that
        /// carry the meaning — smaller, muted, and in its own column.
        /// </summary>
        private void BuildScoreboard()
        {
            // Amber, per `hud.gd::_build_scoreboard`. See WoodCard.
            var card = WoodCard("Scoreboard", new Vector2(0.0f, 1.0f), new Vector2(16, -28),
                                520.0f, out _scoreboard, sink: false, border: UiTheme.Amber);

            // ⚠️ 4, THE .tscn's `separation` ON `Scoreboard/Column`. `WoodCard`'s own 2 is the
            // right default for a two-line card like the lata readout, but this column is a
            // title over four rows and the tighter value stacks them.
            card.spacing = 4.0f;

            _scoreboardRt = card.GetComponent<RectTransform>();

            _scoreTitle = HudLabel(card.transform, "ScoreTitle", 22, UiTheme.Amber,
                                   TextAnchor.MiddleLeft);
            _scoreTitle.text = "SCORES";
            _scoreTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 30.0f;

            for (int i = 0; i < Balance.PlayerCount; i++)
            {
                var rowGo = new GameObject($"ScoreRow{i}");
                rowGo.transform.SetParent(card.transform, false);

                var rowPlate = rowGo.AddComponent<Image>();
                rowPlate.raycastTarget = false;
                _scoreRowPlates[i] = rowPlate;

                var row = rowGo.AddComponent<HorizontalLayoutGroup>();
                row.childControlHeight = true;
                row.childControlWidth = true;
                row.childForceExpandHeight = false;
                row.childForceExpandWidth = false;
                row.childAlignment = TextAnchor.MiddleLeft;

                // ⚠️ 14, THE .tscn's `theme_override_constants/separation` ON EVERY SCORE ROW.
                // The 8 here was invented and it is most of why the port's board reads tighter
                // than the Godot build's at the same font size.
                row.spacing = 14.0f;

                rowGo.AddComponent<LayoutElement>().preferredHeight = 30.0f;
                _scoreRows[i] = rowGo.GetComponent<RectTransform>();

                var railGo = new GameObject("RoleRail");
                railGo.transform.SetParent(rowGo.transform, false);
                var rail = railGo.AddComponent<Image>();
                rail.raycastTarget = false;
                railGo.AddComponent<LayoutElement>().preferredWidth = 8.0f;
                _scoreRoleRails[i] = rail;

                var name = HudLabel(rowGo.transform, "Name", 20, UiTheme.Cream,
                                    TextAnchor.MiddleLeft, ScoreOutline);

                // ⚠️⚠️ A FIXED WIDTH SIZED FROM THE CAP AND THE FONT, NOT `flexibleWidth`, AND
                // THE FLEXIBLE VERSION FAILED BOTH WAYS AT ONCE. 🧑 2026-08-02 on the original:
                // *"make sure the 14 character names fit in the hud and if the name is too short
                // like CP it doesnt look ugly"*. A flexible cell overruns on a long name and
                // pushes the right-aligned score out; it collapses on a short one and lets that
                // row's score slide left, so the column of numbers stops being a column. A fixed
                // width serves both: nothing moves, whatever the name.
                //
                // ⚠️ MEASURED, NOT TYPED. `HUD.tscn` authors 132, which was chosen when every
                // row read "P1".."P4"; `hud.gd::_widen_name_cell` then widens it at runtime to
                // the real width of `PlayerNameMax` "W"s in the real theme font, and keeps 132
                // as the floor. Same derivation here, so this cannot drift when the font size in
                // this file changes.
                name.gameObject.AddComponent<LayoutElement>().preferredWidth =
                    WorstCaseNameWidth(name);

                // ⚠️ A FIXED WIDTH, ALWAYS PRESENT, NEVER HIDDEN. The badge is empty on three
                // rows out of four; hiding it would let those three scores slide left and the
                // column of numbers — the entire point of the board — would stop being a column.
                var mark = HudLabel(rowGo.transform, "Role", TayaBadgeFontSize,
                                    UiTheme.CreamMuted, TextAnchor.MiddleRight, ScoreOutline);
                mark.gameObject.AddComponent<LayoutElement>().preferredWidth = TayaBadgeWidth;

                // ⚠️ 64, THE .tscn's `custom_minimum_size` ON THE SCORE CELL. 72 was invented.
                var score = HudLabel(rowGo.transform, "Score", 20, UiTheme.Cream,
                                     TextAnchor.MiddleRight, ScoreOutline);
                score.gameObject.AddComponent<LayoutElement>().preferredWidth = 64.0f;

                _scoreNames[i] = name;
                _scoreMarks[i] = mark;
                _scoreValues[i] = score;
            }
        }

        /// <summary>
        /// Top-centre: the clock on a RECESSED wood slot with the round line under it.
        ///
        /// ⚠️ THE RECESSED FACE IS DELIBERATE. It is the same one the map and mode readouts use
        /// on the setup screen, which is the front end's existing idiom for "a value being
        /// displayed to you", and the timer is the single most-read element on the screen.
        /// </summary>
        private void BuildClock()
        {
            var column = new GameObject("TopCentre");
            column.transform.SetParent(_root, false);

            var group = column.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
            group.childAlignment = TextAnchor.UpperCenter;
            group.spacing = 4.0f;

            var rt = column.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1.0f);
            rt.anchorMax = new Vector2(0.5f, 1.0f);
            rt.pivot = new Vector2(0.5f, 1.0f);
            rt.anchoredPosition = new Vector2(0, -28);
            rt.sizeDelta = new Vector2(240, 0);

            var cardGo = new GameObject("TimerCard");
            cardGo.transform.SetParent(column.transform, false);

            _timerCard = cardGo.AddComponent<Image>();
            _timerCard.sprite = GodotTheme.Box(UiTheme.WoodDark, UiTheme.WoodEdge,
                                               GodotTheme.WoodBorderWidth,
                                               GodotTheme.WoodCornerRadius);
            _timerCard.type = Image.Type.Sliced;
            _timerCard.raycastTarget = false;

            _timerCardRt = cardGo.GetComponent<RectTransform>();

            var cardGroup = cardGo.AddComponent<VerticalLayoutGroup>();
            cardGroup.childControlHeight = true;
            cardGroup.childControlWidth = true;
            cardGroup.childForceExpandHeight = false;
            cardGroup.childForceExpandWidth = true;

            // ⚠️ 22/16 PADDING, NOT 14/8. These margins were chosen against 13/16pt lettering;
            // at the 2026-08-02 HUD font sizes the same numbers read as text jammed against a
            // border. 🧑: *"GOOD TEXT NOW ... js make box bigger"*.
            cardGroup.padding = new RectOffset(22, 22, 16, 16);

            cardGo.AddComponent<LayoutElement>().preferredWidth = 240.0f;

            // ⚠️ 44, NOT 48. `HUD.tscn` gives TimerLabel the `HudTimer` variation and
            // `ui_theme.gd` binds that to `FONT_SIZE_TIMER`, which is 44. `hud.gd` overrides the
            // COLOUR to amber and nothing else, so 44 is what ships. The 48 here was invented.
            _timer = HudLabel(cardGo.transform, "TimerLabel", 44, UiTheme.Amber,
                              TextAnchor.MiddleCenter);
            _timer.text = "01:30";

            // ⚠️⚠️ AN EXPLICIT HEIGHT, BECAUSE UGUI MEASURED THIS LABEL AT **ZERO** AND THE CARD
            // COLLAPSED ONTO IT. Found by `HudLayoutProbe`, not by looking: `TimerLabel` laid out
            // 196 wide and 0 tall, so the card came out 240 x 32 — the two 16 px margins and
            // nothing between them — against Godot's 240 x 97. The clock, the single most-read
            // element on the screen, was drawing as a thin pill with its digits jammed under the
            // top border. Every neighbouring label already carries a `LayoutElement` for this
            // reason (`ScoreTitle` 30, `RoundLabel` 34); this one did not and inherited whatever
            // the text generator felt like reporting.
            //
            // ⚠️ 64 IS MEASURED, NOT CHOSEN. `Logs/shots-godot/g04-ready.png` at 1920x1080: the
            // card's wood edge runs y28 to y124, and `hud.gd::_hud_wood_style` sets a 16 px top
            // and bottom content margin, which leaves 64 for the label. That reproduces the
            // Godot card to within a pixel and, unlike a font-metric guess, cannot drift when the
            // font asset is replaced.
            //
            // ⚠️⚠️ `minHeight` AS WELL AS `preferredHeight`, AND `preferredHeight` ALONE DID
            // NOTHING — THE FIRST ATTEMPT AT THIS FIX CHANGED THE CARD BY ZERO PIXELS. `TopCentre`
            // is a `VerticalLayoutGroup` whose own rect is 240 x **0**: it is anchored to the top
            // edge with a top pivot and nothing sizes it, which is correct and is how it lays its
            // children downward from y28. But a layout group with less space than its children's
            // total MINIMUM hands every child its `minHeight` and never reaches `preferredHeight`
            // — and a zero-tall rect is always less. `RoundLabel` below survived this by using
            // `minHeight`, which is why it alone came out at its authored size while the clock
            // collapsed. Setting both is what makes the number mean the same thing whether the
            // column is measured or not.
            var timerBox = _timer.gameObject.AddComponent<LayoutElement>();
            timerBox.minHeight = 64.0f;
            timerBox.preferredHeight = 64.0f;

            // ⚠️ WAS `CREAM_MUTED`. This line carries the round number and who is playing taya —
            // the two facts that change everything about how the next 90 s goes — and it was
            // styled as a caption under the clock.
            _round = HudLabel(column.transform, "RoundLabel", 20, UiTheme.Cream,
                              TextAnchor.MiddleCenter, 0);
            _round.gameObject.AddComponent<LayoutElement>().minHeight = 34.0f;

            _timerPressure = HudLabel(column.transform, "TimerPressure", 20, UiTheme.Highlight,
                                      TextAnchor.MiddleCenter, 4);
            _timerPressure.gameObject.AddComponent<LayoutElement>().minHeight = 32.0f;
            _timerPressure.enabled = false;
        }

        /// <summary>Bottom-right, at the .tscn's -396,-172 to -16,-64.</summary>
        private void BuildLataCard()
        {
            var card = WoodCard("LataCard", new Vector2(1.0f, 0.0f), new Vector2(-16, 64),
                                380.0f, out _lataCard, sink: false);

            // ⚠️ 32 AND 34, FROM THE `HudCaption` AND `HudBody` VARIATIONS THE .tscn ASSIGNS
            // THESE TWO NODES. `ui_theme.gd`'s own note on those numbers is worth reading before
            // anyone trims them again: they went 16/13 to 22/19 to 30/28 and 🧑 answered a
            // screenshot of each with *"text still small"*, because the theme dict was never
            // being regenerated. The 26/20 here was the same mistake arrived at independently,
            // and it is why the lata card reads a size smaller than the Godot build's.
            _lataLabel = HudLabel(card.transform, "LataLabel", 32, UiTheme.Amber,
                                  TextAnchor.MiddleLeft);
            _lataLabel.text = "LATA";

            _lataHint = HudLabel(card.transform, "LataHintLabel", 34, UiTheme.Cream,
                                 TextAnchor.MiddleLeft);
            _lataHint.enabled = false;

            _lataCard.gameObject.SetActive(false);
        }

        /// <summary>
        /// Every line `UpdateLataCard` can put in the hint row.
        ///
        /// ⚠️ THE TIMER LINES CARRY THEIR WIDEST VALUE, not a live one. `{left:0.0}s` renders at
        /// most as four characters plus the suffix, so "9.9s" is the true ceiling and measuring
        /// against an empty format string would size the card for a case that never ships.
        /// ⚠️ Keep this in step with `UpdateLataCard`. A line added there and not here is a line
        /// that overflows again, which is exactly how this bug arrived.
        /// </summary>
        private static readonly string[] LataHintLines =
        {
            "TAYA MAY TAG",
            "ATTACKERS MAY RETRIEVE",
            "RESETTING  100%",
            "HOLD E IN THE RING",
            "LEAVE CAN RING  9.9s",
            "CAMPING  ·  DEFENSE SCORE PAUSED",
            "FETCH SLIPPER  9.9s",
            "FETCH SLIPPER  ·  -5 / SECOND",
            "RETRIEVE A SLIPPER",
            "GET OUT OF THE BOX TO THROW",
            "PROTECTED  9.9s",
        };

        /// <summary>
        /// The width the card needs for the longest string it can ever show, measured through the
        /// label that will draw it and padded by the `WoodCard` inset.
        /// </summary>
        private static float WidestLineWidth(Text probe, string[] lines)
        {
            string keep = probe.text;
            float widest = 0.0f;

            foreach (string line in lines)
            {
                probe.text = line;
                widest = Mathf.Max(widest, probe.preferredWidth);
            }

            probe.text = keep;

            // 22 left and 22 right, from `WoodCard`'s padding, plus a pixel so a rounded
            // `preferredWidth` never lands exactly on the border.
            return Mathf.Ceil(widest) + 45.0f;
        }

        /// <summary>
        /// The get-up prompt: which key, and how close the mashing has got.
        ///
        /// ⚠️⚠️ THE MECHANIC SHIPPED WITHOUT ITS FEEDBACK AND THAT IS WHY THE FALL FELT LIKE
        /// NOTHING. `CharacterMotor.MashRecover`, `CanMashUp`, `TripLeft`, `TripTotal` and
        /// `MashPresses` all exist, and `MashPresses` even carries the note *"so the HUD can
        /// show it filling"*. Nothing ever filled. All the player got was a text toast reading
        /// MASH [SPACE] TO GET UP, with no way to tell whether mashing was doing anything or how
        /// much was left. 🧑: *"i dont feel like i fell down"*.
        ///
        /// ⚠️ A BAR RATHER THAN A COUNTER, because the question a player on the floor is asking
        /// is "how much longer", not "how many presses". The bar answers it at a glance while
        /// they are mashing and cannot read.
        ///
        /// ⚠️ CENTRE SCREEN, LOW. It has to be findable without looking, by somebody who has
        /// just been knocked over and whose camera is on the ground. The corners are where the
        /// standing HUD lives and are the first thing a fall makes irrelevant.
        /// </summary>
        private void BuildGetUpCard()
        {
            var group = WoodCard("GetUpCard", new Vector2(0.5f, 0.0f), new Vector2(0.0f, 150.0f),
                                 460.0f, out _getUpCard, sink: false, border: UiTheme.Offense);

            group.childAlignment = TextAnchor.MiddleCenter;

            _getUpLabel = HudLabel(group.transform, "GetUpLabel", 30, UiTheme.Cream,
                                   TextAnchor.MiddleCenter);
            _getUpLabel.text = "MASH TO GET UP";
            _getUpLabel.gameObject.AddComponent<LayoutElement>().minHeight = 40.0f;

            // The bar. Same two-part build as the status rows: a sunk plate, and a horizontal
            // fill stretched across it.
            var backGo = new GameObject("GetUpBarBack");
            backGo.transform.SetParent(group.transform, false);

            var back = backGo.AddComponent<Image>();
            back.sprite = GodotTheme.Plain(3);
            back.type = Image.Type.Sliced;
            back.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
            back.raycastTarget = false;

            var box = backGo.AddComponent<LayoutElement>();
            box.minHeight = 22.0f;
            box.preferredHeight = 22.0f;

            var fillGo = new GameObject("GetUpBarFill");
            fillGo.transform.SetParent(backGo.transform, false);

            _getUpFill = fillGo.AddComponent<Image>();
            _getUpFill.sprite = GodotTheme.Plain(3);
            _getUpFill.type = Image.Type.Filled;
            _getUpFill.fillMethod = Image.FillMethod.Horizontal;
            _getUpFill.color = UiTheme.Offense;
            _getUpFill.raycastTarget = false;
            _getUpFill.fillAmount = 0.0f;

            MenuKit.Stretch(_getUpFill.rectTransform);

            // ⚠️⚠️ A SECOND FILL, DRAWN OVER THE FIRST, AND IT IS THE PLAYER'S OWN SHARE.
            // 🧑, 2026-08-26: the get-up *"automatically resolves without doing anything"*. One
            // bar could not answer that, because it drew the passive bleed and the presses in
            // the same colour: mashing well and doing nothing looked identical for the first
            // second, and by the time they diverged the fall was over. This one measures
            // `MashRemoved` only, so every accepted press visibly extends it and nothing else
            // ever does.
            var mashGo = new GameObject("GetUpBarMashFill");
            mashGo.transform.SetParent(backGo.transform, false);

            _getUpMashFill = mashGo.AddComponent<Image>();
            _getUpMashFill.sprite = GodotTheme.Plain(3);
            _getUpMashFill.type = Image.Type.Filled;
            _getUpMashFill.fillMethod = Image.FillMethod.Horizontal;
            _getUpMashFill.color = UiTheme.Highlight;
            _getUpMashFill.raycastTarget = false;
            _getUpMashFill.fillAmount = 0.0f;

            MenuKit.Stretch(_getUpMashFill.rectTransform);

            _getUpBarRt = backGo.GetComponent<RectTransform>();

            _getUpCard.gameObject.SetActive(false);
        }

        private void BuildStatusStacks()
        {
            _stackLeft = BuildStack("StatusStackLeft", false);
            _stackRight = BuildStack("StatusStackRight", true);
        }

        /// <summary>
        /// ⚠️ ANCHORED TO ITS OWN CORNER, NOT PARENTED TO A CARD. A growing stack must never
        /// push another element around.
        /// </summary>
        private RectTransform BuildStack(string name, bool rightSide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);

            var group = go.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.spacing = 6.0f;
            group.childAlignment = rightSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(rightSide ? 1.0f : 0.0f, 1.0f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(rightSide ? 1.0f : 0.0f, 1.0f);
            rt.anchoredPosition = new Vector2(rightSide ? -StatusMargin.x : StatusMargin.x,
                                              -StatusMargin.y);
            rt.sizeDelta = new Vector2(StatusBarSize.x, 0.0f);

            // ⚠️⚠️ WITHOUT THIS THE ROWS DREW ON TOP OF EACH OTHER, AND IT IS THE SAME ZERO-HEIGHT
            // TRAP THE CLOCK CARD FELL INTO. This rect is anchored to a corner with a top pivot
            // and a `sizeDelta.y` of 0, which is right for growing downward — but a layout group
            // with less room than its children's total MINIMUM gives every child its `minHeight`
            // and never reaches `preferredHeight`, and zero room is always less. Every status row
            // therefore laid out 0 px tall at the same y: photographed in
            // `Logs/shots-runtime/StunFrost.png` as "THROW CD" and "SHOVE CD 1.0s" superimposed
            // in the top-right corner, one legible line made of two.
            //
            // The fitter makes the stack as tall as its rows, which is what a Godot `VBoxContainer`
            // does by default and what the .tscn assumes. The rows also carry their own minimums
            // now, so neither mechanism is load-bearing alone.
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

        private StatusWidget BuildStatusRow(RectTransform parent, bool rightSide)
        {
            var rowGo = new GameObject("StatusRow");
            rowGo.transform.SetParent(parent, false);

            var group = rowGo.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.spacing = 2.0f;

            var label = HudLabel(rowGo.transform, "Label", StatusFontSize, UiTheme.Cream,
                                 rightSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);

            // ⚠️ MIN AND PREFERRED, for the reason `BuildStack` records: a group with no room
            // hands out minimums, and a `preferredHeight` on its own is then simply ignored.
            var labelBox = label.gameObject.AddComponent<LayoutElement>();
            labelBox.minHeight = 26.0f;
            labelBox.preferredHeight = 26.0f;

            var backGo = new GameObject("Bar");
            backGo.transform.SetParent(rowGo.transform, false);

            var back = backGo.AddComponent<Image>();
            back.sprite = GodotTheme.Plain(3);
            back.type = Image.Type.Sliced;
            back.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
            back.raycastTarget = false;

            var barBox = backGo.AddComponent<LayoutElement>();
            barBox.minHeight = StatusBarSize.y;
            barBox.preferredHeight = StatusBarSize.y;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(backGo.transform, false);

            var fill = fillGo.AddComponent<Image>();
            fill.sprite = GodotTheme.Plain(3);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.raycastTarget = false;

            MenuKit.Stretch(fill.rectTransform);

            return new StatusWidget { Root = rowGo, Label = label, Fill = fill, Back = back };
        }

        /// <summary>
        /// A dark plate behind one line of intermission text.
        ///
        /// ⚠️ IT IS BUILT BEFORE ITS LABEL, ON PURPOSE. Sibling order is draw order in a canvas,
        /// so a plate created after its text would sit on top of it and hide the very thing it
        /// exists to make readable.
        /// </summary>
        private Image BannerPlate(string name, Vector2 offset, Vector2 size, Color rim,
                                  bool fromTop = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);

            var plate = go.AddComponent<Image>();
            plate.sprite = GodotTheme.Box(UiTheme.HeroPlate, rim, 2, 6);
            plate.type = Image.Type.Sliced;
            plate.raycastTarget = false;

            Place(plate.rectTransform, new Vector2(0.5f, fromTop ? 1.0f : 0.0f), offset, size);
            plate.enabled = false;
            return plate;
        }

        private void BuildFloatingText()
        {
            // Toast, top-centre under the clock, at the .tscn's +160.
            _toast = HudLabel(_root, "ToastLabel", 28, UiTheme.Amber, TextAnchor.MiddleCenter);
            Place(_toast.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, -160),
                  new Vector2(600, 44));
            _toast.enabled = false;

            _lataAlert = HudLabel(_root, "LataDownAlert", 42, UiTheme.Danger,
                                  TextAnchor.MiddleCenter, 10);
            Place(_lataAlert.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, -228),
                  new Vector2(980, 70));
            _lataAlert.enabled = false;

            // The countdown owns the middle of the screen for its three seconds, because
            // nothing is in play behind it yet.
            //
            // ⚠️ 40, THE `HudBanner` VARIATION THE .tscn ASSIGNS, AND THE 120 HERE WAS THREE
            // TIMES THE ORIGINAL. `hud.gd::show_countdown_tick` overrides the colour and drives
            // the scale pop, never the size, so 40 is what the Godot build renders. The pop
            // below is already the original's 1.8 -> 1.0 over 0.35 s, which means the effective
            // peak is 72 px rather than 216. Verified against `Logs/shots-godot/g05-countdown.png`,
            // where the "2" is a small glyph beside the character rather than a banner.
            _countdown = HudLabel(_root, "CountdownLabel", 40, UiTheme.Highlight,
                                  TextAnchor.MiddleCenter);
            Place(_countdown.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                  new Vector2(400, 160));
            _countdown.enabled = false;
            _countdownRt = _countdown.rectTransform;

            // ⚠️ BOTH INTERMISSION LINES SIT ON A PLATE NOW. They are drawn over a sunlit
            // asphalt court with a white centre circle on it, and cream text with nothing behind
            // it is legible over exactly none of that. See the screenshot the redesign came
            // from: the objective line was unreadable at 32 pt in the brightest colour available.
            _readyPromptPlate = BannerPlate("ReadyPromptPlate", new Vector2(0, 92),
                                            new Vector2(520, 34), UiTheme.HeroRim);

            _readyPrompt = HudLabel(_root, "ReadyPrompt", 17, UiTheme.Cream,
                                    TextAnchor.MiddleCenter);
            Place(_readyPrompt.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 92),
                  new Vector2(520, 30));
            _readyPrompt.text = "Walk around freely. Press [R] when you're ready to start the round.";
            _readyPrompt.enabled = false;

            // ⚠️ A HOLD KEY NOBODY IS TOLD ABOUT IS A KEY NOBODY PRESSES. One quiet line
            // above the deck, in the muted cream the rest of the HUD uses for asides, naming
            // whatever key is actually bound.
            _inspectHint = HudLabel(_root, "InspectHint", 14,
                                    UiTheme.CreamMuted, TextAnchor.MiddleCenter, 2);
            Place(_inspectHint.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 78),
                  new Vector2(400, 18));
            _inspectHint.enabled = false;

            _readyObjectivePlate = BannerPlate("ReadyObjectivePlate", new Vector2(0, -206),
                                               new Vector2(620, 38), UiTheme.HeroRimLit,
                                               fromTop: true);

            _readyObjective = HudLabel(_root, "ReadyObjective", 20, UiTheme.Cream,
                                       TextAnchor.MiddleCenter);
            Place(_readyObjective.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, -206),
                  new Vector2(900, 44));
            _readyObjective.enabled = false;

            // ⚠️⚠️ THE VULNERABLE LINE IS OFF DEAD CENTRE, ON A PLAYTEST REPORT. 🧑 2026-08-02,
            // with a first-person screenshot: *"You are vulnerable not in middle"*. In first
            // person the thing you are looking at while this warning is live is the slipper you
            // are bending down to pick up, which is the bottom-middle of the screen — so the one
            // line that means "you are about to lose 5 seconds" was drawn over the exact object
            // it is about. Bottom-centre, above the ready prompt's band, inside the 64 px safe
            // band.
            _vulnerable = HudLabel(_root, "VulnerableWarning", 22, UiTheme.Offense,
                                   TextAnchor.MiddleCenter);
            Place(_vulnerable.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 84),
                  new Vector2(400, 40));
            _vulnerable.text = "YOU ARE VULNERABLE";
            _vulnerable.enabled = false;
        }

        /// <summary>
        /// The crosshair is a thin `+` at dead centre of an FPP camera, which is the busiest
        /// part of the frame; role colour alone would lose it against a wall in the same hue, so
        /// it takes the INK outline too.
        /// </summary>
        private void BuildCrosshair()
        {
            _crosshair = HudLabel(_root, "CrosshairLabel", 34, UiTheme.Offense,
                                  TextAnchor.MiddleCenter, CrosshairOutline);

            Place(_crosshair.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                  new Vector2(520, 72));

            _crosshair.text = "+";
            _crosshair.enabled = false;

            _hitmarker = HudLabel(_root, "HitmarkerLabel", 42, UiTheme.Highlight,
                                  TextAnchor.MiddleCenter, 5);
            Place(_hitmarker.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                  new Vector2(64, 64));
            _hitmarker.text = "💥";
            _hitmarker.enabled = false;
        }

        public void PopHitmarker(Color color, string symbol = "💥")
        {
            if (_hitmarker == null) return;
            _hitmarkerTimer = 0.28f;
            _hitmarker.text = symbol;
            _hitmarker.color = color;
            _hitmarker.rectTransform.localScale = Vector3.one * 1.6f;
            _hitmarker.enabled = true;
            GameServices.Audio?.PlayAt("sfx_hitmarker", UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : Vector3.zero);
        }

        public static void TriggerHitmarker(Color color, string symbol = "💥")
        {
            Instance?.PopHitmarker(color, symbol);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// A wood card, built from the same `wood_style()` the menu's own WoodSlot uses, so the
        /// HUD cannot drift away from the front end again.
        /// </summary>
        /// <summary>
        /// ⚠️ THE BORDER IS A PARAMETER BECAUSE THE SCOREBOARD'S IS AMBER, NOT WOOD_EDGE.
        /// `hud.gd::_build_scoreboard` re-skins that panel with
        /// `_style_team_card(scoreboard_panel, score_title, UiTheme.AMBER)`, and its comment
        /// records why: the panel used to render on the stock theme and 🧑 called it *"ugly ui
        /// btw, not even same theme what is that white box"*. Every HUD card here took WOOD_EDGE,
        /// so the scoreboard was brown where the Godot build is amber. Measured off the two
        /// captures of the same moment: Godot (248,184,0), this build (136,80,32).
        /// </summary>
        private VerticalLayoutGroup WoodCard(string name, Vector2 anchor, Vector2 offset,
                                             float width, out Image face, bool sink,
                                             Color? border = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);

            face = go.AddComponent<Image>();
            face.sprite = GodotTheme.Box(sink ? UiTheme.WoodDark : UiTheme.WoodDeep,
                                         border ?? UiTheme.WoodEdge,
                                         GodotTheme.WoodBorderWidth,
                                         GodotTheme.WoodCornerRadius);
            face.type = Image.Type.Sliced;
            face.raycastTarget = false;

            var group = go.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.spacing = 2.0f;
            group.padding = new RectOffset(22, 22, 16, 16);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(width, 0.0f);

            return group;
        }

        /// <summary>
        /// How wide the name cell has to be for the longest name this game can produce, measured
        /// off the real font at the real size rather than typed in.
        ///
        /// ⚠️ "W" IS THE WORST CASE, not an average-case guess a name like MMMMMM would beat, and
        /// `PlayerNameMax` of them is the true ceiling because that is the same constant the name
        /// field itself clamps to. Typing the answer here would be correct until somebody changes
        /// the font size a few lines up, and then silently wrong.
        ///
        /// ⚠️ IT MEASURES THROUGH THE LABEL, NOT THROUGH A SPARE `Font` CALL, because
        /// `preferredWidth` is the number this exact component will lay out to: same font, same
        /// size, same generator settings. Reading a raw font metric instead is how a cell ends up
        /// a few pixels short of the string it was sized for.
        /// </summary>
        private static float WorstCaseNameWidth(Text probe)
        {
            string keep = probe.text;
            probe.text = new string('W', Balance.PlayerNameMax);

            float needed = Mathf.Ceil(probe.preferredWidth);

            probe.text = keep;
            return Mathf.Max(ScoreNameFloor, needed);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        /// <summary>A HUD label in the game's own face, with the heavy INK outline everything
        /// drawn over a live 3D scene needs.</summary>
        private static Text HudLabel(Transform parent, string name, int size, Color colour,
                                     TextAnchor align, int outline = TextOutline)
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

            if (outline > 0)
            {
                var ring = go.AddComponent<GodotOutline>();
                ring.OutlineColour = UiTheme.Ink;
                ring.Radius = Mathf.Max(1.0f, outline * 0.5f);
            }

            return t;
        }

        /// <summary>
        /// Bottom centre: the three hero powers, and nothing else.
        ///
        /// ⚠️⚠️ THE ARITHMETIC HAS TO HOLD OR THE CARDS RUN OFF THE PLATE:
        ///
        ///     width = pad_left + pad_right + (cards - 1) * spacing + sum(card_widths)
        ///     240   = 6        + 6         + 2 * 6                 + (70 + 70 + 76)
        ///
        /// A `HorizontalLayoutGroup` lays children out past the edge of a rect that no longer
        /// fits them without complaining, and the overflow lands under the first-person hands
        /// where it is least visible and most annoying. Change a width, redo the line.
        ///
        /// ⚠️ 240 x 68 AT `y = 10`, AND IT IS A BADGE RATHER THAN A BAR. It was 592 x 122 at
        /// `y = 24`, which is a quarter of a 1080p screen's width of chrome sitting on top of
        /// the viewmodel.
        ///
        /// ⚠️ THE PALETTE IS THE WOOD SET, NOT A SLATE-BLUE GLASS OF ITS OWN. See
        /// `UiTheme.HeroPlate` for what naming seventeen colours inline cost.
        /// </summary>
        private void BuildHeroDeck()
        {
            // ⚠️⚠️ `typeof(RectTransform)` IS LOAD-BEARING AND ITS ABSENCE TOOK THE WHOLE HUD
            // DOWN. `new GameObject(name)` gives a plain `Transform`; the deck used to get a
            // `RectTransform` for free because the very next line added an `Image`, and every
            // uGUI graphic requires one. Dropping the background plate in the Overwatch redesign
            // silently dropped that too, so `GetComponent<RectTransform>()` returned null and
            // `Hud.Build` threw a NullReferenceException out of `Awake`.
            //
            // ⚠️ THE FAILURE DOES NOT LOOK LIKE A MISSING DECK, WHICH IS WHY IT IS WORTH THIS
            // NOTE. An exception in `Awake` abandons the rest of `Build`, so the scoreboard came
            // up as an empty box, the ability deck was absent and the crosshair never appeared:
            // three unrelated-looking faults from one missing type argument. EditMode and
            // PlayMode were both green through it, because neither constructs the in-match HUD.
            // `tools/shoot_player.ps1` is the only check that sees what the .exe does.
            var deckGo = new GameObject("HeroDeck", typeof(RectTransform));
            deckGo.transform.SetParent(_root, false);
            _heroDeck = deckGo;

            // ⚠️⚠️ NO PLATE BEHIND THE ROW, AND THAT IS THE WHOLE REDESIGN. 🧑, looking at the
            // wooden version beside an Overwatch frame: *"the brown shit looks ugly. kinda
            // wanted just the icons like in overwatchh or something"*. He is right, and the
            // reason is structural rather than a matter of taste: a container says "these three
            // things are a group", which the player already knew, and it costs a slab of opaque
            // furniture across the bottom of the frame to say it. Three floating squares say the
            // same thing for free and let the court show through between them.
            //
            // ⚠️ THE GameObject STILL CARRIES A RectTransform AND THE LAYOUT GROUP. Only the
            // Image is gone; the arithmetic below is unchanged and still asserted.
            var rt = deckGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.0f);
            rt.anchorMax = new Vector2(0.5f, 0.0f);
            rt.pivot = new Vector2(0.5f, 0.0f);
            rt.anchoredPosition = new Vector2(0, DeckBottomMargin);
            rt.sizeDelta = new Vector2(DeckWidth, DeckHeight);

            var hgroup = deckGo.AddComponent<HorizontalLayoutGroup>();
            hgroup.childControlHeight = true;
            hgroup.childControlWidth = true;
            hgroup.childForceExpandHeight = true;
            hgroup.childForceExpandWidth = false;
            hgroup.childAlignment = TextAnchor.LowerCenter;
            hgroup.spacing = DeckSpacing;
            hgroup.padding = new RectOffset((int)DeckPadding, (int)DeckPadding, 0, 0);

            _skill1Card = BuildAbilityCard(deckGo.transform, "Skill1", "Skill1", SkillCardWidth, false);
            _skill2Card = BuildAbilityCard(deckGo.transform, "Skill2", "Skill2", SkillCardWidth, false);
            _ultCard = BuildAbilityCard(deckGo.transform, "Ultimate", "Ultimate", UltimateCardWidth, true);

            _heroDeck.SetActive(false);
        }

        /// <summary>
        /// Classic deliberately has no powers, but an empty bottom HUD made its mastery loop
        /// feel less authored than Hero Strike. Street Hype is cosmetic: it names skilled
        /// curves, banks, close calls, blocks and retrievals without changing a single point
        /// or rule. It gives Classic its own identity instead of pretending it is Hero Strike
        /// with three cards removed.
        /// </summary>
        private void BuildClassicDeck()
        {
            var go = new GameObject("ClassicStreetHype");
            go.transform.SetParent(_root, false);
            _classicDeck = go;

            var bg = go.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(UiTheme.WoodDark, UiTheme.Amber,
                                       GodotTheme.WoodBorderWidth, GodotTheme.WoodCornerRadius);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            _classicDeckRt = go.GetComponent<RectTransform>();
            Place(_classicDeckRt, new Vector2(0.5f, 0.0f), new Vector2(0, 24),
                  new Vector2(520, 100));

            var group = go.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.padding = new RectOffset(18, 18, 10, 10);
            group.spacing = 4.0f;

            _classicTitle = HudLabel(go.transform, "HypeTitle", 20, UiTheme.Highlight,
                                     TextAnchor.MiddleCenter, 4);
            _classicTitle.fontStyle = FontStyle.Bold;
            _classicTitle.gameObject.AddComponent<LayoutElement>().minHeight = 24.0f;

            _classicEvent = HudLabel(go.transform, "HypeEvent", MenuKit.MinReadableUnits,
                                     UiTheme.Cream, TextAnchor.MiddleCenter, 3);
            _classicEvent.gameObject.AddComponent<LayoutElement>().minHeight = 24.0f;

            var bar = new GameObject("HypeBar");
            bar.transform.SetParent(go.transform, false);
            var barBg = bar.AddComponent<Image>();
            barBg.sprite = GodotTheme.Plain(3);
            barBg.type = Image.Type.Sliced;
            barBg.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.8f);
            var barLayout = bar.AddComponent<LayoutElement>();
            barLayout.minHeight = 9.0f;
            barLayout.preferredHeight = 9.0f;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bar.transform, false);
            _classicFill = fillGo.AddComponent<Image>();
            _classicFill.sprite = GodotTheme.Plain(3);
            _classicFill.type = Image.Type.Filled;
            _classicFill.fillMethod = Image.FillMethod.Horizontal;
            _classicFill.color = UiTheme.Highlight;
            _classicFill.raycastTarget = false;
            MenuKit.Stretch(_classicFill.rectTransform);

            _classicDeck.SetActive(false);
        }

        /// <summary>Reports a local cosmetic style event; never awards score.</summary>
        public static void ReportStyle(int slot, float amount, string callout)
        {
            if (Instance == null || SceneFlow.SelectedMode != GameMode.Classic
                || Instance._local == null || Instance._local.PlayerSlot != slot)
                return;

            Instance.AddStreetHype(amount, callout);
        }

        private void AddStreetHype(float amount, string callout)
        {
            int before = StreetTier(_streetHype);
            _streetHype = Mathf.Clamp(_streetHype + Mathf.Max(0.0f, amount), 0.0f, 100.0f);
            _streetHypeGrace = 3.0f;
            _streetHypePunch = 0.38f;
            _classicEvent.text = $"{callout}  ·  +{Mathf.RoundToInt(amount)} HYPE";

            int after = StreetTier(_streetHype);
            if (after > before && _local != null)
            {
                string tier = StreetTierName(after);
                Visual.ComicPopup.Spawn(_local.transform.position + Vector3.up * 1.8f,
                                        tier, UiTheme.Highlight, 1.05f);
            }

            if (_streetHype >= 100.0f && !_streetHypeMaxCelebrated)
            {
                _streetHypeMaxCelebrated = true;
                GameServices.Audio?.PlayAtVaried("sfx_super_ready", _local.transform.position,
                                                 1.02f, 1.10f, 0.9f);
                ShowToast("HALIMAW HYPE  ·  KEEP THE RALLY ALIVE", 1.8f);
            }
        }

        private void UpdateClassicDeck(float dt)
        {
            if (_classicDeck == null) return;

            bool show = !_spectating && _local != null
                        && SceneFlow.SelectedMode == GameMode.Classic;
            if (_classicDeck.activeSelf != show) _classicDeck.SetActive(show);
            if (!show) return;

            int round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
            if (_streetHypeRound != round)
            {
                _streetHypeRound = round;
                _streetHype = 0.0f;
                _streetHypeGrace = 0.0f;
                _streetHypeMaxCelebrated = false;
                _classicEvent.text = "CURVE · BANK · BLOCK · SNATCH  (STYLE ONLY)";
            }

            if (GameServices.Round != null && GameServices.Round.RoundActive)
            {
                if (_streetHypeGrace > 0.0f) _streetHypeGrace -= dt;
                else _streetHype = Mathf.Max(0.0f, _streetHype - 4.5f * dt);
            }

            if (_streetHype < 92.0f) _streetHypeMaxCelebrated = false;

            int tier = StreetTier(_streetHype);
            _classicTitle.text = $"STREET HYPE  ·  {StreetTierName(tier)}  ·  {Mathf.RoundToInt(_streetHype)}%";
            _classicTitle.color = tier >= 3 ? UiTheme.Highlight : UiTheme.Amber;
            _classicFill.fillAmount = _streetHype / 100.0f;
            _classicFill.color = Color.Lerp(UiTheme.Offense, UiTheme.Highlight,
                                             _streetHype / 100.0f);

            _streetHypePunch = Mathf.Max(0.0f, _streetHypePunch - dt);
            float ratio = Mathf.Clamp01(_streetHypePunch / 0.38f);
            float scale = 1.0f + Mathf.Sin(ratio * Mathf.PI) * 0.07f;
            _classicDeckRt.localScale = Vector3.one * scale;
        }

        private static int StreetTier(float hype)
            => hype >= 100.0f ? 4 : hype >= 72.0f ? 3 : hype >= 44.0f ? 2 : hype >= 20.0f ? 1 : 0;

        private static string StreetTierName(int tier)
        {
            switch (tier)
            {
                case 4: return "HALIMAW!";
                case 3: return "ASTIG!";
                case 2: return "MAINIT!";
                case 1: return "GISING!";
                default: return "SIMULA";
            }
        }

        /// <summary>
        /// One tile: a glyph, the key it is on, one number when there is one, and a meter.
        ///
        /// ⚠️⚠️ THE THREE STATES ARE RIM, GLYPH AND ONE NUMBER, AND NONE OF THEM IS A WORD.
        /// Ready is a lit glyph inside an accent rim and prints NOTHING in the middle, because
        /// the state a player is in most of the time has to be the quietest thing on screen; a
        /// tile that says "READY" is shouting at somebody who already knew. Cooling dims the
        /// glyph to 20% and puts the seconds in the middle. Active keeps the glyph lit and
        /// breathes the rim. `docs/Hero_Strike_UI.md` section 4 carries the table.
        ///
        /// ⚠️ THE NUMBER AND THE METER SAY THE SAME THING ON PURPOSE. They are read at
        /// different distances: the meter is peripheral vision ("nearly back"), the number is a
        /// glance ("1.8, I can wait"). Dropping either was tried and the tile got worse.
        ///
        /// ⚠️ `actionName` IS AN INPUT ACTION, NOT A STRING TO PRINT. The chip's letter comes
        /// from the live binding via `KeyLabel`, so a rebind in the settings panel is on the HUD
        /// the same frame and the deck cannot go stale the way hard-coded labels did.
        ///
        /// ⚠️ `segmented` PICKS THE METER, AND THE TWO ARE NOT INTERCHANGEABLE. A skill
        /// DRAINS a smooth bar; the ultimate FILLS a notched one. See `UltSegments`.
        /// </summary>
        private AbilityCard BuildAbilityCard(Transform parent, string name, string actionName,
                                             float width, bool segmented)
        {
            var card = new AbilityCard();

            // The card is the tile PLUS the key label under it, so the layout group can align
            // three of them on one baseline.
            var cardGo = new GameObject(name, typeof(RectTransform));
            cardGo.transform.SetParent(parent, false);
            card.Rt = cardGo.GetComponent<RectTransform>();

            var le = cardGo.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            // ---- the square tile -------------------------------------------------
            var tileGo = new GameObject("Tile", typeof(RectTransform));
            tileGo.transform.SetParent(cardGo.transform, false);

            card.Plate = tileGo.AddComponent<Image>();
            card.Plate.sprite = GodotTheme.Box(UiTheme.HeroPlateRaised, new Color(0, 0, 0, 0), 0, 8);
            card.Plate.type = Image.Type.Sliced;
            card.Plate.raycastTarget = false;

            Place(card.Plate.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, 0),
                  new Vector2(TileSize, TileSize));

            // ⚠️⚠️ THE RIM IS ITS OWN IMAGE. `Image.color` multiplies the WHOLE nine-slice,
            // border included, so one sprite carrying both a fill and a border cannot light its
            // edge without also lifting its fill: the old deck tinted the entire tile
            // hero-orange to say "ready", which is a colour wash where the design wants an
            // outline. A transparent-fill box stacked on the plate gives the rim its own colour
            // for one extra draw.
            var rimGo = new GameObject("Rim", typeof(RectTransform));
            rimGo.transform.SetParent(tileGo.transform, false);
            card.Rim = rimGo.AddComponent<Image>();
            card.Rim.sprite = GodotTheme.Box(new Color(0, 0, 0, 0), Color.white, 2, 8);
            card.Rim.type = Image.Type.Sliced;
            card.Rim.raycastTarget = false;
            MenuKit.Stretch(card.Rim.rectTransform);

            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(tileGo.transform, false);
            card.Glyph = glyphGo.AddComponent<Image>();
            card.Glyph.sprite = AbilityIcons.For(AbilityGlyph.Burst);
            card.Glyph.color = UiTheme.HeroGlyphOn;
            card.Glyph.preserveAspect = true;
            card.Glyph.raycastTarget = false;
            MenuKit.Stretch(card.Glyph.rectTransform);
            card.Glyph.rectTransform.offsetMin = new Vector2(11, 13);
            card.Glyph.rectTransform.offsetMax = new Vector2(-11, -11);

            // A radial veil makes the direction of a cooldown readable without looking at the
            // number. It sits above the glyph and below the countdown, so the remaining wedge
            // can never hide the exact final seconds.
            var sweepGo = new GameObject("CooldownSweep", typeof(RectTransform));
            sweepGo.transform.SetParent(tileGo.transform, false);
            card.CooldownSweep = sweepGo.AddComponent<Image>();
            card.CooldownSweep.sprite = AbilityIcons.CooldownDisc();
            card.CooldownSweep.type = Image.Type.Filled;
            card.CooldownSweep.fillMethod = Image.FillMethod.Radial360;
            card.CooldownSweep.fillOrigin = (int)Image.Origin360.Top;
            card.CooldownSweep.fillClockwise = false;
            card.CooldownSweep.fillAmount = 0.0f;
            card.CooldownSweep.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.76f);
            card.CooldownSweep.raycastTarget = false;
            MenuKit.Stretch(card.CooldownSweep.rectTransform);
            card.CooldownSweep.rectTransform.offsetMin = new Vector2(7, 7);
            card.CooldownSweep.rectTransform.offsetMax = new Vector2(-7, -7);

            // ⚠️ THE COUNTDOWN SITS OVER THE GLYPH RATHER THAN BESIDE IT. A 60 px tile has no
            // room for two columns and the two are never both interesting: while a number is up
            // the glyph is at 20% and is only there to say WHICH power is coming back.
            card.State = HudLabel(tileGo.transform, "State", 22, UiTheme.HeroNumber,
                                  TextAnchor.MiddleCenter, 2);
            card.State.fontStyle = FontStyle.Bold;
            card.State.text = "";
            MenuKit.Stretch(card.State.rectTransform);

            // ---- the meter, inside the bottom edge of the tile --------------------
            var groove = new GameObject("Groove", typeof(RectTransform));
            groove.transform.SetParent(tileGo.transform, false);
            var grooveImg = groove.AddComponent<Image>();
            grooveImg.sprite = GodotTheme.Plain(2);
            grooveImg.type = Image.Type.Sliced;
            grooveImg.color = UiTheme.HeroPlateSunk;
            grooveImg.raycastTarget = false;
            var grooveRt = (RectTransform)groove.transform;
            grooveRt.anchorMin = new Vector2(0.0f, 0.0f);
            grooveRt.anchorMax = new Vector2(1.0f, 0.0f);
            grooveRt.pivot = new Vector2(0.5f, 0.0f);
            grooveRt.anchoredPosition = new Vector2(0, 6);
            grooveRt.sizeDelta = new Vector2(-16, 3);

            // ---- the key, BELOW the tile and outside it ---------------------------
            //
            // ⚠️⚠️ OUTSIDE, NOT IN A CHIP IN THE CORNER. 🧑: *"i want the keybind for the icons
            // to show too"*, sent with a crop of the deck in which the corner chips are three
            // illegible smudges. They were 22 x 15 with 13 pt type inside a tile that is itself
            // only 60 px, competing with the glyph for the same square. Overwatch, Valorant and
            // Apex all put the key on its own line under the icon for the same reason: it is
            // read ONCE, while learning, and then never again, so it must be legible and must
            // not cost the icon any room.
            card.Key = HudLabel(cardGo.transform, "Key", 15,
                                new Color(UiTheme.Cream.r, UiTheme.Cream.g, UiTheme.Cream.b, 0.90f),
                                TextAnchor.MiddleCenter, 2);
            card.Key.fontStyle = FontStyle.Bold;
            card.Key.text = KeyLabel(actionName);
            Place(card.Key.rectTransform, new Vector2(0.5f, 1.0f),
                  new Vector2(0, -(TileSize + KeyGap)), new Vector2(width, 15));

            if (segmented)
            {
                var segRow = new GameObject("Segments", typeof(RectTransform));
                segRow.transform.SetParent(groove.transform, false);
                MenuKit.Stretch((RectTransform)segRow.transform);

                var segGroup = segRow.AddComponent<HorizontalLayoutGroup>();
                segGroup.childControlHeight = true;
                segGroup.childControlWidth = true;
                segGroup.childForceExpandHeight = true;
                segGroup.childForceExpandWidth = true;
                segGroup.spacing = 1.0f;
                segGroup.padding = new RectOffset(0, 0, 0, 0);

                card.Segments = new Image[UltSegments];
                for (int i = 0; i < UltSegments; i++)
                {
                    var segGo = new GameObject("Seg" + i);
                    segGo.transform.SetParent(segRow.transform, false);
                    var seg = segGo.AddComponent<Image>();
                    seg.sprite = GodotTheme.Plain(1);
                    seg.type = Image.Type.Sliced;
                    seg.color = UiTheme.HeroRim;
                    seg.raycastTarget = false;
                    card.Segments[i] = seg;
                }

                return card;
            }

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(groove.transform, false);
            card.Fill = fillGo.AddComponent<Image>();
            card.Fill.sprite = GodotTheme.Plain(1);
            card.Fill.type = Image.Type.Filled;
            card.Fill.fillMethod = Image.FillMethod.Horizontal;
            card.Fill.color = UiTheme.HeroNumber;
            card.Fill.raycastTarget = false;
            MenuKit.Stretch(card.Fill.rectTransform);

            return card;
        }

        /// <summary>The key bound to an action, for anything outside this class that draws one.</summary>
        public static string KeyLabelFor(string action) => KeyLabel(action);

        private static string KeyLabel(string action)
        {
            if (_bindingAsset == null)
            {
                _bindingAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("TumbangPreso");
                if (_bindingAsset != null) Settings.Rebinding.Load(_bindingAsset);
            }

            if (_bindingAsset == null) return action == "Ultimate" ? "F" : action == "Skill2" ? "E" : "Q";

            var map = _bindingAsset.FindActionMap("Player");
            var act = map?.FindAction(action);

            if (act == null || act.bindings.Count == 0)
                return action == "Ultimate" ? "F" : action == "Skill2" ? "E" : "Q";

            // ⚠️ THE STATIC FORM, NOT THE EXTENSION. `InputBinding.ToHumanReadableString` is an
            // extension method that only resolves with `using UnityEngine.InputSystem;` in
            // scope, and this file deliberately does not carry one: it fully qualifies the two
            // input types it touches so nothing else in a 2,500 line HUD picks up that
            // namespace by accident. `InputControlPath.ToHumanReadableString` is the same
            // implementation reached as a plain static.
            string key = UnityEngine.InputSystem.InputControlPath.ToHumanReadableString(
                act.bindings[0].effectivePath,
                UnityEngine.InputSystem.InputControlPath.HumanReadableStringOptions.OmitDevice);

            if (string.IsNullOrEmpty(key)) return "";

            // Mouse button abbreviations
            if (key == "Left Button" || key == "LeftButton") return "LMB";
            if (key == "Right Button" || key == "RightButton") return "RMB";
            if (key == "Middle Button" || key == "MiddleButton") return "MMB";

            return key.ToUpperInvariant();
        }

        private static UnityEngine.InputSystem.InputActionAsset _bindingAsset;

        private void UpdateHeroDeck()
        {
            if (_heroDeck == null) return;

            bool isHeroMode = SceneFlow.SelectedMode == GameMode.HeroStrike;
            bool show = !_spectating && _local != null && isHeroMode;

            if (_heroDeck.activeSelf != show) _heroDeck.SetActive(show);
            if (!show) return;

            var abilitySystem = _local.GetComponent<Abilities.HeroAbilitySystem>();
            if (abilitySystem == null || abilitySystem.Kit == null)
            {
                _heroDeck.SetActive(false);
                return;
            }

            if (_inspectHint != null)
            {
                if (_inspectHintText == null)
                    _inspectHintText = "HOLD [" + KeyLabel("AbilityInfo") + "] FOR POWER DETAILS";

                if (_inspectHint.text != _inspectHintText) _inspectHint.text = _inspectHintText;
            }

            if (!_heroDeck.activeSelf) _heroDeck.SetActive(true);

            _inspect?.Tick(abilitySystem.Kit, Time.unscaledDeltaTime);

            var kit = abilitySystem.Kit;
            Color heroColor = UiTheme.ColorForHero(kit.HeroId);
            float dt = Time.unscaledDeltaTime;

            PaintSkillCard(_skill1Card, kit.Skill1, heroColor, abilitySystem,
                           Abilities.HeroAbilitySystem.Slot.Skill1, dt);
            PaintSkillCard(_skill2Card, kit.Skill2, heroColor, abilitySystem,
                           Abilities.HeroAbilitySystem.Slot.Skill2, dt);
            PaintUltimateCard(kit, heroColor, abilitySystem, dt);
        }

        /// <summary>
        /// One skill tile, in whichever of the three states it is in.
        ///
        /// ⚠️⚠️ THE READY POP IS THE FEEDBACK THAT DID NOT EXIST AT ALL. Nothing anywhere told
        /// a player their skill had come back: the number simply stopped being drawn, on a tile
        /// they were not looking at, in the middle of a fight. A single 0.18 s scale to 1.12 on
        /// the frame the cooldown clears is enough to catch in peripheral vision and short
        /// enough that it cannot be confused with the ultimate's slow breath.
        ///
        /// ⚠️ AND THE POP FIRES ON THE EDGE, NOT ON THE STATE. `WasReady` is what makes it
        /// once rather than every frame the skill happens to be up, which would be a tile that
        /// never stops moving and therefore a tile nobody reads.
        /// </summary>
        private static void PaintSkillCard(AbilityCard card, Abilities.HeroAbility skill,
                                           Color heroColor, Abilities.HeroAbilitySystem system,
                                           Abilities.HeroAbilitySystem.Slot slot, float dt)
        {
            if (card == null) return;

            if (skill == null)
            {
                // A hero without this power draws an empty plate rather than a stale one.
                card.Rim.color = UiTheme.HeroRim;
                card.Glyph.color = UiTheme.HeroGlyphOff;
                card.Key.color = UiTheme.HeroGlyphOff;
                card.State.text = "";
                if (card.Fill != null) card.Fill.fillAmount = 0.0f;
                if (card.CooldownSweep != null) card.CooldownSweep.fillAmount = 0.0f;
                return;
            }

            PaintGlyph(card.Glyph, skill);
            card.Plate.color = Color.white;

            // ⚠️ THE SIZE IS RESET EVERY FRAME, BEFORE ANY BRANCH CHOOSES A STRING. Only the
            // recast arm changes it, and a tile that showed RECAST and then went on cooldown
            // would otherwise draw its countdown at 14 pt for the rest of the round: the other
            // arms set `text` and never touch `fontSize`. Defaulting here means one place owns
            // it and no branch has to remember to put it back.
            card.State.fontSize = StateFontSize;

            bool ready = skill.IsReady;
            if (ready && !card.WasReady) card.PopLeft = ReadyPopSeconds;
            card.WasReady = ready;

            PaintCharges(card, skill, heroColor, dt);

            // ⚠️⚠️ A CHARGE SKILL AT ZERO IS NOT "COOLING", AND THAT DISTINCTION IS THE WHOLE
            // REASON THIS BRANCH EXISTS. Its `Cooldown` is 0 so it would fall through to the
            // Ready arm below and draw a lit rim over a power that cannot be cast, which is the
            // exact failure `docs/Hero_Strike_UI.md` § 6 calls the anti-clunk fix: a press that
            // is refused must not look like a press that worked.
            //
            // It gets its own look rather than borrowing the Cooling one, because the two are
            // different facts. Cooling means WAIT. Empty means NOT THIS ROUND, unless this is
            // one of the skills that recharges off play. A countdown would be a lie either way,
            // so there is no number: the pips above the tile already say how many are left, and
            // `docs/VISION.md` § 3 forbids putting a sentence here to explain it.
            if (skill.UsesCharges && !skill.IsActive && skill.ChargesRemaining <= 0)
            {
                card.Rim.color = UiTheme.HeroRim;
                card.Glyph.color = UiTheme.HeroGlyphOff;
                card.Key.color = UiTheme.CreamMuted;
                card.State.text = "";

                if (card.Fill != null) card.Fill.fillAmount = 0.0f;
                if (card.CooldownSweep != null) card.CooldownSweep.fillAmount = 0.0f;

                ApplyAnswer(card, system, slot, heroColor);
                ApplyPop(card, dt);
                return;
            }

            if (skill.IsActive)
            {
                float breath = Mathf.Sin(Time.time * 7.0f) * 0.5f + 0.5f;
                // ⚠️⚠️ ACTIVE IS THE ONE STATE THAT GETS THE HERO ACCENT, AND THAT IS WHY THE
                // ACCENT MEANS ANYTHING. Ready is a white rim, cooling is a dim one; colour is
                // reserved for "this power is RUNNING RIGHT NOW", which is the only state with
                // a clock on it that the player is inside rather than waiting on.
                card.Rim.color = Color.Lerp(heroColor, Color.white, breath * 0.35f);
                card.Glyph.color = UiTheme.HeroGlyphOn;
                card.Key.color = UiTheme.Cream;

                // ⚠️⚠️ A RECASTABLE POWER SAYS SO, AND UNTIL NOW NOTHING IN THE GAME DID.
                // 🧑, off the build: *"i dont feel or know that some abilities are recast too"*.
                // He is right and it was invisible by construction: a running ability drew a
                // countdown, and a running ability you can press AGAIN drew the same countdown.
                // Nemu's Astral Projection is one press out and one press back, so the entire
                // second half of the ability was an affordance the deck never mentioned.
                //
                // ⚠️ THE WORD REPLACES THE NUMBER RATHER THAN CROWDING IT, because the bar
                // underneath is already the timer: `card.Fill` carries `DurationRatio` in the
                // same tile, so nothing is lost by spending the text slot on the thing the
                // player cannot otherwise know. `docs/VISION.md` § 3 forbids a SENTENCE here to
                // explain a state; one word naming the action available is what the key cap and
                // the glyph already are.
                //
                // ⚠️ AND IT IS GATED ON `CanReactivate`, not on a hero id. `HeroAbility` already
                // owns that fact and `HeroKit.Fire` already routes a press by it, so the deck
                // reads the same property the input path does and a recast added to any future
                // ability lights up here the day it is added.
                //
                // ⚠️⚠️ THE WORD IS SET SMALLER THAN THE NUMBER AND THAT IS NOT A STYLE CHOICE.
                // The tile is 60 px and `State` is 22 pt bold, sized for "9.9". Six bold
                // capitals at 22 pt run about 78 px, and `HudLabel` sets
                // `horizontalOverflow = Overflow`, so it would not wrap or shrink: it would
                // simply hang out of both sides of the tile. That is the identical fault just
                // fixed on the objective card, where "-5 / SECOND" ran off the screen edge, so
                // shipping it again one commit later would be careless. 14 pt puts the word at
                // roughly 51 px, inside the tile with a margin.
                bool recastable = skill.CanReactivate;

                card.State.fontSize = recastable ? RecastFontSize : StateFontSize;
                card.State.text = recastable ? "RECAST" : $"{skill.DurationRemaining:0.0}";
                card.State.color = recastable ? Color.Lerp(heroColor, Color.white, 0.35f) : heroColor;

                if (card.Fill != null)
                {
                    card.Fill.fillAmount = skill.DurationRatio;
                    card.Fill.color = heroColor;
                }
                if (card.CooldownSweep != null) card.CooldownSweep.fillAmount = 0.0f;
            }
            else if (skill.CooldownRemaining > 0.0f)
            {
                card.Rim.color = UiTheme.HeroRim;
                card.Glyph.color = UiTheme.HeroGlyphOff;
                card.Key.color = UiTheme.CreamMuted;

                // ⚠️ ONE DECIMAL UNDER THREE SECONDS, WHOLE SECONDS ABOVE IT. A countdown that
                // ticks tenths for nine seconds is a number nobody can read and a canvas rebuild
                // every frame; one that only shows whole seconds is useless in the last moment,
                // which is the only moment a player is actually waiting on it.
                card.State.text = AbilityDeckHud.CooldownLabel(skill.CooldownRemaining);
                card.State.color = UiTheme.HeroNumber;

                if (card.Fill != null)
                {
                    card.Fill.fillAmount = 1.0f - skill.CooldownRatio;
                    card.Fill.color = UiTheme.HeroNumber;
                }
                if (card.CooldownSweep != null)
                    card.CooldownSweep.fillAmount = AbilityDeckHud.CooldownSweep(
                        skill.CooldownRemaining, skill.Cooldown);
            }
            else
            {
                // ⚠️ READY IS A WHITE RIM, NOT AN ACCENT ONE. Three coloured outlines sitting
                // there permanently is a deck that is always shouting, and it leaves nothing
                // louder to say when a power actually fires.
                card.Rim.color = UiTheme.HeroRimLit;
                card.Glyph.color = UiTheme.HeroGlyphOn;
                card.Key.color = UiTheme.Cream;

                // ⚠️⚠️ READY PRINTS NOTHING. See `docs/Hero_Strike_UI.md` section 4.
                card.State.text = "";

                if (card.Fill != null)
                {
                    card.Fill.fillAmount = 0.0f;
                }
                if (card.CooldownSweep != null) card.CooldownSweep.fillAmount = 0.0f;
            }

            ApplyAnswer(card, system, slot, heroColor);
            ApplyPop(card, dt);
        }

        /// <summary>
        /// Draws the charge dots above a tile, building them on first sight of a charge skill.
        ///
        /// ⚠️⚠️ BUILT LAZILY RATHER THAN AT CARD CONSTRUCTION, BECAUSE THE CARD IS BUILT BEFORE
        /// THE KIT IS KNOWN. `BuildAbilityCard` runs once for the deck and the same three cards
        /// are then repainted for whichever hero the seat is playing, so "how many charges" is
        /// not a fact that exists yet at build time. Rebuilding on a count change is also what
        /// makes a mid-match character swap draw the right number of dots instead of the
        /// previous hero's.
        ///
        /// ⚠️ THE PIPS SIT ABOVE THE TILE, NOT INSIDE IT. Inside would put them in the same
        /// square as the glyph, which is the mistake the key labels made and which
        /// `docs/Hero_Strike_UI.md` records being reported as *"three illegible smudges"*. The
        /// groove under the tile is already spoken for by the cooldown bar and the ultimate
        /// segments, so above is the only clear edge left.
        /// </summary>
        private static void PaintCharges(AbilityCard card, Abilities.HeroAbility skill,
                                         Color heroColor, float dt)
        {
            if (card == null || skill == null) return;

            if (!skill.UsesCharges)
            {
                // A cooldown ability on a card that previously held a charge one. Hide rather
                // than destroy: a seat can swap back, and rebuilding is the expensive half.
                if (card.Pips != null)
                    foreach (var p in card.Pips) if (p != null) p.enabled = false;

                card.WasCharges = -1;
                return;
            }

            if (card.Pips == null || card.PipCount != skill.MaxCharges)
                BuildPips(card, skill.MaxCharges);

            if (card.Pips == null) return;

            // ⚠️ THE FLASH FIRES ON A GAIN AND NEVER ON A SPEND. A player who casts something
            // already knows they cast it; a charge handed back by `Recharge.LataKnocked` or
            // `Recharge.OwnSlipperRetrieved` arrives while they are looking at the lata or at
            // their own feet, several metres from the deck, and is the one charge event they
            // will otherwise miss entirely.
            if (card.WasCharges >= 0 && skill.ChargesRemaining > card.WasCharges)
                card.PipGrantLeft = 0.45f;

            card.WasCharges = skill.ChargesRemaining;

            if (card.PipGrantLeft > 0.0f) card.PipGrantLeft = Mathf.Max(0.0f, card.PipGrantLeft - dt);

            float flash = card.PipGrantLeft > 0.0f
                ? Mathf.Sin(card.PipGrantLeft * 28.0f) * 0.5f + 0.5f
                : 0.0f;

            for (int i = 0; i < card.Pips.Length; i++)
            {
                var pip = card.Pips[i];
                if (pip == null) continue;

                pip.enabled = true;

                bool held = i < skill.ChargesRemaining;

                // ⚠️ A SPENT PIP IS DIMMED, NOT REMOVED. Keeping the empty socket on screen is
                // what tells the player how many they STARTED with, which is the number they
                // need to plan a round around. Dots that vanish leave "one left" and "one, and
                // that is all I ever had" looking identical.
                pip.color = held
                    ? Color.Lerp(heroColor, Color.white, flash * 0.75f)
                    : UiTheme.HeroRim;
            }
        }

        private static void BuildPips(AbilityCard card, int count)
        {
            if (card == null || card.Rt == null || count <= 0) return;

            if (card.Pips != null)
                foreach (var p in card.Pips)
                    if (p != null) UnityEngine.Object.Destroy(p.gameObject);

            var row = new GameObject("Pips", typeof(RectTransform));
            row.transform.SetParent(card.Rt, false);

            var rowRt = (RectTransform)row.transform;
            rowRt.anchorMin = new Vector2(0.5f, 1.0f);
            rowRt.anchorMax = new Vector2(0.5f, 1.0f);
            rowRt.pivot = new Vector2(0.5f, 0.0f);
            rowRt.anchoredPosition = new Vector2(0.0f, 3.0f);
            rowRt.sizeDelta = new Vector2(count * 9.0f + (count - 1) * 3.0f, 5.0f);

            var group = row.AddComponent<HorizontalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = true;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.spacing = 3.0f;
            group.padding = new RectOffset(0, 0, 0, 0);

            card.Pips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Pip" + i);
                go.transform.SetParent(row.transform, false);

                var img = go.AddComponent<Image>();
                img.sprite = GodotTheme.Plain(1);
                img.type = Image.Type.Sliced;
                img.color = UiTheme.HeroRim;
                img.raycastTarget = false;

                card.Pips[i] = img;
            }

            card.PipCount = count;
        }

        /// <summary>
        /// The 0.14 s confirm and the 0.12 s refusal tick.
        ///
        /// ⚠️⚠️ THIS IS THE WHOLE ANTI-CLUNK FIX AND IT IS FOUR LINES. A press refused because
        /// the power was down used to look EXACTLY like a press that worked: no flash, no tick,
        /// no movement anywhere on screen. The only reading available to the player was that the
        /// game had dropped their input, and they were wrong, and nothing could have told them.
        /// The rim answers every press within a frame. `HeroAbilitySystem` section on the cast
        /// answer has the other half.
        ///
        /// ⚠️ IT PAINTS OVER THE STATE COLOUR RATHER THAN INSTEAD OF IT, so a tile that is
        /// mid-cooldown still shows its countdown while it ticks red. Replacing the state would
        /// hide the very number that explains the refusal.
        /// </summary>
        private static void ApplyAnswer(AbilityCard card, Abilities.HeroAbilitySystem system,
                                        Abilities.HeroAbilitySystem.Slot slot, Color heroColor)
        {
            if (system == null) return;

            float since = system.SecondsSinceAnswer(slot);
            var answer = system.LastAnswer(slot);

            if (answer == Abilities.HeroKit.CastOutcome.Cast)
            {
                if (since > CastFlashSeconds) return;
                float t = 1.0f - since / CastFlashSeconds;
                card.Rim.color = Color.Lerp(card.Rim.color, UiTheme.Cream, t);
                card.Plate.color = Color.Lerp(Color.white, heroColor, t * 0.55f);
                return;
            }

            if (answer == Abilities.HeroKit.CastOutcome.Cooling ||
                answer == Abilities.HeroKit.CastOutcome.NoCharge ||
                answer == Abilities.HeroKit.CastOutcome.CannotAct)
            {
                if (since > RefusalFlashSeconds)
                {
                    card.Plate.color = Color.white;
                    return;
                }

                float t = 1.0f - since / RefusalFlashSeconds;
                card.Rim.color = Color.Lerp(card.Rim.color, UiTheme.Danger, t);
                card.Plate.color = Color.Lerp(Color.white, UiTheme.Danger, t * 0.45f);
                return;
            }

            card.Plate.color = Color.white;
        }

        private static void ApplyPop(AbilityCard card, float dt)
        {
            if (card.Rt == null) return;

            if (card.PopLeft <= 0.0f)
            {
                if (card.Rt.localScale != Vector3.one) card.Rt.localScale = Vector3.one;
                return;
            }

            card.PopLeft = Mathf.Max(0.0f, card.PopLeft - dt);

            // A half sine: out to 1.12 and back, with no discontinuity at either end.
            float t = 1.0f - card.PopLeft / ReadyPopSeconds;
            card.Rt.localScale = Vector3.one * (1.0f + Mathf.Sin(t * Mathf.PI) * 0.12f);
        }

        /// <summary>
        /// The ultimate tile. Three states again, but the middle one is CHARGING rather than
        /// cooling, and the meter is notched rather than smooth.
        ///
        /// ⚠️ THE PERCENTAGE MOVED OUT OF THE CENTRE. A big "64%" over the glyph made the
        /// charging state the loudest tile in the deck, which is exactly backwards: charge is
        /// the one quantity a player can do nothing about in the moment. The notched meter
        /// carries it, and the number only appears once it is worth acting on.
        /// </summary>
        private void PaintUltimateCard(Abilities.HeroKit kit, Color heroColor,
                                       Abilities.HeroAbilitySystem system, float dt)
        {
            var card = _ultCard;
            if (card == null || kit.Ultimate == null) return;

            PaintGlyph(card.Glyph, kit.Ultimate);
            card.Plate.color = Color.white;

            float ratio = kit.UltimateRatio;

            if (kit.PracticeMode)
            {
                // ⚠️ THE PRACTICE TILE IS LIT WITHOUT PRETENDING TO BE CHARGED. The meter still
                // draws the banked charge, because that IS what the player will start the round
                // with; the rim says the cast is free right now. Showing 100% here would tell
                // them they had an ultimate they have not earned.
                card.Rim.color = heroColor;
                card.Glyph.color = UiTheme.HeroGlyphOn;
                card.Key.color = UiTheme.Cream;
                card.State.text = "";

                PaintUltSegments(card, ratio, heroColor, UiTheme.HeroRim);

                _lastUltReady = false;
                ApplyAnswer(card, system, Abilities.HeroAbilitySystem.Slot.Ultimate, heroColor);
                ApplyPop(card, dt);
                return;
            }

            bool ready = kit.IsUltimateReady;
            if (ready && !card.WasReady) card.PopLeft = ReadyPopSeconds;
            card.WasReady = ready;

            if (ready)
            {
                // ⚠️ A SLOW BREATH, NOT A FAST PULSE. 1.4 s is the only continuous motion the
                // deck is allowed, and it has to be distinguishable from a skill's 0.18 s pop at
                // a glance or neither of them means anything.
                float breath = Mathf.Sin(Time.time * (Mathf.PI * 2.0f / 1.4f)) * 0.5f + 0.5f;
                card.Rim.color = Color.Lerp(heroColor, Color.white, breath * 0.55f);
                card.Glyph.color = UiTheme.HeroGlyphOn;
                card.Key.color = UiTheme.Cream;
                card.State.text = "";

                PaintUltSegments(card, 1.0f, card.Rim.color, card.Rim.color);

                if (!_lastUltReady)
                {
                    _lastUltReady = true;
                    GameServices.Audio?.PlayAt("sfx_super_ready",
                        UnityEngine.Camera.main != null
                            ? UnityEngine.Camera.main.transform.position
                            : Vector3.zero);
                    if (_local != null) Visual.ComicPopup.Super(_local.transform.position);
                }

                ApplyAnswer(card, system, Abilities.HeroAbilitySystem.Slot.Ultimate, heroColor);
                ApplyPop(card, dt);
                return;
            }

            _lastUltReady = false;

            card.Rim.color = UiTheme.HeroRim;
            card.Glyph.color = UiTheme.HeroGlyphOff;
            card.Key.color = UiTheme.CreamMuted;

            // Only worth reading once it is nearly there. Below that the notches say enough.
            card.State.text = ratio >= 0.75f ? $"{Mathf.FloorToInt(ratio * 100f)}%" : "";
            card.State.color = UiTheme.HeroNumber;

            PaintUltSegments(card, ratio, heroColor, UiTheme.HeroRim);

            ApplyAnswer(card, system, Abilities.HeroAbilitySystem.Slot.Ultimate, heroColor);
            ApplyPop(card, dt);
        }

        /// <summary>
        /// ⚠️ THE SPRITE IS SET ONLY WHEN IT CHANGES. `AbilityIcons.For` is cached, so this
        /// is cheap either way, but assigning `Image.sprite` every frame dirties the canvas
        /// batch for three cards on every HUD tick and this runs during a live match.
        /// </summary>
        private static void PaintGlyph(Image target, Abilities.HeroAbility ability)
        {
            if (target == null || ability == null) return;

            var want = AbilityIcons.For(ability.Glyph);
            if (target.sprite != want) target.sprite = want;
        }

        /// <summary>
        /// ⚠️ THE PARTIAL NOTCH IS DIMMED, NOT HALF-DRAWN. A segment either belongs to the
        /// charge or it does not; fading the one currently filling is what stops ten notches
        /// from reading as a coarse, jumpy bar.
        /// </summary>
        private static void PaintUltSegments(AbilityCard card, float ratio, Color on, Color off)
        {
            if (card == null || card.Segments == null) return;

            float exact = Mathf.Clamp01(ratio) * UltSegments;
            int whole = Mathf.FloorToInt(exact);
            float partial = exact - whole;

            for (int i = 0; i < card.Segments.Length; i++)
            {
                if (card.Segments[i] == null) continue;

                if (i < whole) card.Segments[i].color = on;
                else if (i == whole && partial > 0.05f) card.Segments[i].color = Color.Lerp(off, on, partial);
                else card.Segments[i].color = off;
            }
        }

    }
}
