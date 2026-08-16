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
        public const float TayaBadgeWidth = 54.0f;

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

        private Image _scoreboard;
        private Text _scoreTitle;
        private readonly Text[] _scoreNames = new Text[Balance.PlayerCount];
        private readonly Text[] _scoreMarks = new Text[Balance.PlayerCount];
        private readonly Text[] _scoreValues = new Text[Balance.PlayerCount];
        private RectTransform _scoreboardRt;

        private Image _lataCard;
        private Text _lataLabel;
        private Text _lataHint;

        private Text _toast;
        private float _toastLeft;

        private Text _countdown;
        private float _countdownPop;
        private RectTransform _countdownRt;

        private Text _readyPrompt;
        private Text _readyObjective;

        private Image _dangerFlash;
        private bool _dangerHeld;
        private float _flashLeft;

        /// <summary>§ THE STUN FROST — the screen half. See <see cref="UpdateFrost"/>.</summary>
        private Image _frostVignette;
        private Material _frostMaterial;
        private float _frostCoverage;

        private Text _vulnerable;
        private Text _crosshair;

        private OffscreenIndicators _indicators;

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
            if (_readyPrompt != null) _readyPrompt.enabled = show;
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
                return;
            }

            bool defending = _local.IsDefender;

            // Two sentences for the taya because it IS two jobs, and a player who only hears
            // "guard the lata" stands on the base and never tags anybody. Two for the attacker
            // for the same reason: the retrieval run is the half people miss.
            _readyObjective.text = defending
                ? "GUARD THE LATA.  TAG ANYONE HOLDING A SLIPPER."
                : "KNOCK THE LATA DOWN.  RETRIEVE FROM THE BOX.";

            _readyObjective.color = defending ? UiTheme.Defense : UiTheme.Offense;
            _readyObjective.enabled = true;
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

        private void Awake() => Build();

        private void OnEnable()
        {
            if (GameServices.Match != null) GameServices.Match.Scored += OnScored;
        }

        private void OnDisable()
        {
            if (GameServices.Match != null) GameServices.Match.Scored -= OnScored;
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
            if (_local == null || _local.PlayerSlot != slot) return;

            ShowToast($"+{MatchRules.PointsFor(e)}  {LabelOf(e)}", 1.2f);
        }

        private static string LabelOf(ScoreEvent e)
        {
            switch (e)
            {
                case ScoreEvent.LataKnocked: return "LATA DOWN";
                case ScoreEvent.Sabotage: return "SABOTAGE";
                case ScoreEvent.Tag: return "TAG";
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
            _lataCard.gameObject.SetActive(false);
            _crosshair.enabled = false;
            _vulnerable.enabled = false;
            _readyPrompt.enabled = false;
            _readyObjective.enabled = false;
            _dangerFlash.enabled = false;

            // § THE STUN FROST rides along: it is a transient like the flash above, and a
            // spectator has no stun of their own to be told about. A clean feed needs no
            // equivalent, because that path disables the whole canvas.
            ClearFrost();

            if (_stackLeft != null) _stackLeft.gameObject.SetActive(false);
            if (_stackRight != null) _stackRight.gameObject.SetActive(false);
            if (_indicators != null) _indicators.gameObject.SetActive(false);
        }

        public bool IsCleanFeed => _cleanFeed;

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

            if (_local == null || GameServices.Match == null || GameServices.Round == null) return;
            if (_spectating) return;

            float dt = Time.unscaledDeltaTime;

            UpdateTimer(dt);
            UpdateScores();
            UpdateLataCard();
            UpdateStatus();
            UpdateDanger();
            UpdateToast(dt);
            UpdateCountdown(dt);
            UpdateFrost(dt);
            UpdateIndicators();

            bool live = GameServices.Round.CanThrow(_local);
            _crosshair.enabled = live;

            // R-28 — the two in-world markers that answer "what am I doing" take the LOCAL
            // player's role colour: the crosshair they aim with, and the edge arrow pointing at
            // the lata. Driven off the local unit, not off whichever side defends.
            Color role = _local.IsDefender ? UiTheme.Defense : UiTheme.Offense;
            _crosshair.color = role;
            _indicators?.SetCanArrowColour(role);

            _vulnerable.enabled = _local.IsTaggable();
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
            bool urgent = left < 15.0f;
            int want = urgent ? 1 : 0;

            if (want != _urgent)
            {
                _urgent = want;

                // ⚠️ BACK TO AMBER, NOT TO THE VARIATION'S OWN COLOUR. Falling through would
                // give the near-white the wood restyle replaced, so the timer would go white
                // the moment it climbed back over 15 s.
                _timer.color = urgent ? UiTheme.Highlight : UiTheme.Amber;
            }

            // A scale pulse rather than a colour flash, to avoid colliding with the danger
            // vignette that may be running at the same time.
            float scale = 1.0f;
            if (left < 10.0f) scale = 1.0f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI)) * 0.05f;

            if (_timerCardRt != null) _timerCardRt.localScale = Vector3.one * scale;

            // ⚠️ maxi(round, 1). `MatchDirector.RoundNumber` is 0 until the match starts, and
            // the ready-up window happens BEFORE that: the HUD read "ROUND 0 / 4" over the first
            // thing a player ever sees. The .gd has clamped this since the format was written.
            int round = Mathf.Max(1, GameServices.Match.RoundNumber);

            // ⚠️ NAME, NOT SEAT. This used to print "P%d" off the raw slot, so a taya who set a
            // name in Settings still read as "P3" on the one line that most needs to say who is
            // playing.
            // ⚠️ THREE SPACES EACH SIDE OF THE DOT, matching `hud.gd`'s own format string. Two
            // reads as a tighter line than the original at the same font size.
            _round.text = $"ROUND {round} / {Balance.Rounds}   ·   TAYA: {SeatName(GameServices.Match.DefenderSlot)}";
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

                _scoreNames[i].text = SeatName(slot);
                _scoreMarks[i].text = isTaya ? TayaBadge : "";
                _scoreValues[i].text = m.ScoreFor(slot).ToString();

                // ⚠️ NO LEADING BULLET — THE COLOUR IS THE MARK. 🧑 2026-08-02: *"the arrow
                // makes the names of the characters not aligned"*. The prefix was one character
                // on your own row against two spaces on every other, so all four names started
                // at a different x and the column read as ragged. Highlighting the row says the
                // same thing and costs no width.
                Color colour = isTaya ? UiTheme.Defense : UiTheme.Offense;
                if (slot == mine) colour = UiTheme.Highlight;

                _scoreNames[i].color = colour;
                _scoreValues[i].color = colour;
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
                return;
            }

            _lataCard.gameObject.SetActive(true);

            if (_lataUprightShown != (lata.IsUpright ? 1 : 0))
            {
                _lataUprightShown = lata.IsUpright ? 1 : 0;
                _lataLabel.text = lata.IsUpright ? "LATA  ·  UPRIGHT" : "LATA  ·  DOWN";
                _lataLabel.color = lata.IsUpright ? UiTheme.Defense : UiTheme.Offense;
            }

            // The second line is what THIS player can do about it, which differs by role and is
            // the whole reason the card is not just a coloured light.
            string line = "";

            if (_local.IsDefender)
            {
                if (!lata.IsUpright)
                {
                    var carrier = _local.GetComponent<Carrier>();
                    float progress = carrier != null ? carrier.ChannelRatio : 0.0f;

                    line = progress > 0.0f
                        ? $"RESETTING  {Mathf.RoundToInt(progress * 100.0f)}%"
                        : "HOLD E IN THE RING";
                }
            }
            else if (!_local.HoldingSlipper)
            {
                line = "RETRIEVE A SLIPPER";
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

            if (_local != null && _local.IsStunned)
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
            BuildStatusStacks();
            BuildFloatingText();
            BuildCrosshair();

            // Its own object, deliberately: the arrows are positioned from screen centre in raw
            // pixels, and putting them under the scaled HUD canvas would move them.
            var indicatorGo = new GameObject("OffscreenIndicators");
            indicatorGo.transform.SetParent(transform, false);
            _indicators = indicatorGo.AddComponent<OffscreenIndicators>();
        }

        /// <summary>Full-screen, behind everything, and never a raycast target.</summary>
        private void BuildDangerFlash()
        {
            var go = new GameObject("DownedFlash");
            go.transform.SetParent(_root, false);

            _dangerFlash = go.AddComponent<Image>();
            _dangerFlash.color = new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b, 0.0f);
            _dangerFlash.raycastTarget = false;
            _dangerFlash.enabled = false;

            MenuKit.Stretch(_dangerFlash.rectTransform);
        }

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
            if (_frostMaterial == null) return;

            if (Application.isPlaying) Destroy(_frostMaterial);
            else DestroyImmediate(_frostMaterial);
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
                                440.0f, out _scoreboard, sink: false, border: UiTheme.Amber);

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

            // ⚠️ WAS `CREAM_MUTED`. This line carries the round number and who is playing taya —
            // the two facts that change everything about how the next 90 s goes — and it was
            // styled as a caption under the clock.
            _round = HudLabel(column.transform, "RoundLabel", 20, UiTheme.Cream,
                              TextAnchor.MiddleCenter);
            _round.gameObject.AddComponent<LayoutElement>().minHeight = 34.0f;
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
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 26.0f;

            var backGo = new GameObject("Bar");
            backGo.transform.SetParent(rowGo.transform, false);

            var back = backGo.AddComponent<Image>();
            back.sprite = GodotTheme.Plain(3);
            back.type = Image.Type.Sliced;
            back.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
            back.raycastTarget = false;

            backGo.AddComponent<LayoutElement>().preferredHeight = StatusBarSize.y;

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

        private void BuildFloatingText()
        {
            // Toast, top-centre under the clock, at the .tscn's +160.
            _toast = HudLabel(_root, "ToastLabel", 28, UiTheme.Amber, TextAnchor.MiddleCenter);
            Place(_toast.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, -160),
                  new Vector2(600, 44));
            _toast.enabled = false;

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

            _readyPrompt = HudLabel(_root, "ReadyPrompt", 28, UiTheme.Cream,
                                    TextAnchor.MiddleCenter);
            Place(_readyPrompt.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 138),
                  new Vector2(1040, 40));
            _readyPrompt.text = "Walk around freely. Press [R] when you're ready to start the round.";
            _readyPrompt.enabled = false;

            _readyObjective = HudLabel(_root, "ReadyObjective", 44, UiTheme.Offense,
                                       TextAnchor.MiddleCenter);
            Place(_readyObjective.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 200),
                  new Vector2(1120, 64));
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
                  new Vector2(48, 48));

            _crosshair.text = "+";
            _crosshair.enabled = false;
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

            var ring = go.AddComponent<GodotOutline>();
            ring.OutlineColour = UiTheme.Ink;
            ring.Radius = Mathf.Max(1.0f, outline * 0.5f);

            return t;
        }
    }
}
