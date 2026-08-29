using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The match-end board, converted from `scripts/ui/match_result.gd`.
    ///
    /// Self-sufficient like the HUD: it reads the match directly and needs no wiring beyond
    /// being present.
    ///
    /// ⚠️ COLOUR TRACKS ROLE AND PLACEMENT, NEVER TEAM IDENTITY (§4.2's hard rule). There are
    /// no teams — four players, one taya per round — so the board ranks four seats, not two
    /// sides. The two pip rows the original had were deleted with the teams they counted.
    /// </summary>
    public sealed class MatchResult : MonoBehaviour
    {
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        private Canvas _canvas;
        private Text _message;
        private Text _broadcastLine;

        /// <summary>
        /// "2 / 3 WANT A REMATCH", under the button.
        ///
        /// ⚠️⚠️ IT IS ITS OWN LABEL AND NOT THE BROADCAST LINE. The first version of the tally
        /// wrote into `_broadcastLine`, which already carries
        /// "HERO STRIKE · FINAL STANDINGS · 8 ROUNDS", so the moment anybody pressed rematch the
        /// mode and the round count were deleted from a screen whose whole job is the final
        /// standings. Two facts, two labels.
        /// </summary>
        private Text _rematchTally;

        /// <summary>
        /// What this match added to your career, under the standings.
        ///
        /// ⚠️⚠️ IT IS ITS OWN LABEL FOR THE SAME REASON `_rematchTally` IS. The broadcast line
        /// already carries the mode and the round count and the tally already carries the vote;
        /// writing a third fact into either one deletes the first two the moment it appears.
        ///
        /// ⚠️ IT ARRIVES LATE ON A CLIENT AND THAT IS NORMAL. The record is counted by the host
        /// and broadcast, so on a peer this label is empty for the frame or two before
        /// `MatchRecord` lands. `MatchStatsCollector.RecordReady` fills it in when it does,
        /// rather than the board waiting on a network message before it can be drawn at all.
        /// </summary>
        private Text _yourMatchLine;
        private readonly List<Text[]> _rows = new List<Text[]>();
        private Button _rematch;
        private Button _menu;

        /// <summary>Peers that have voted for a rematch. ⚠️ The rules live in
        /// `Core.RematchVote`, engine-free, because every bug this has ever had was a counting
        /// bug and counting can be asserted in a millisecond.</summary>
        private readonly Core.RematchVote _rematchVotes = new Core.RematchVote();

        /// <summary>
        /// ⚠️⚠️ EVERY PLAYING PEER VOTES ON A REMATCH, NOT ONLY THE HOST. 🧑 2026-08-01:
        /// *"in multiplayer only host has rematch and this doesnt disappear... can we make it
        /// so that they all can click rematch button (only the humans playing) and if they
        /// all check the rematch goes on"*, and separately *"spectator shouldnt see rematch
        /// button js scoreboard."*
        ///
        /// ✅ THE WIRE HALF LANDED 2026-08-26. `MatchRpc` carries a three-message vote
        /// (`VoteRematch` peer to host, `RematchTally` host to all, `BeginRematch` host to all)
        /// and the button is still hidden for a spectator here, which is the half that never
        /// needed the wire.
        ///
        /// ⚠️⚠️ THE TALLY IS BROADCAST, NOT INFERRED. Without it a peer who has voted sees a
        /// pressed button and nothing else, cannot tell whether anybody agreed, and reads the
        /// wait as the button being broken. `match_result.gd` draws the count for exactly that
        /// reason.
        ///
        /// ⚠️ IT COUNTS PEERS, NEVER CHARACTERS, which is `ReadyGate.ExpectedReadyCount`'s rule
        /// and the same one for the same reason: a bot-filled seat cannot press a button, so a
        /// gate that waits for four SEATS in a two-human match never opens.
        /// </summary>
        public bool IsSpectator { get; set; }

        private void Awake()
        {
            Build();

            // ⚠️ THE CANVAS HIDES, NOT THIS OBJECT. Deactivating the GameObject stops OnEnable
            // firing, so the component would never subscribe to MatchEnded and the board would
            // never appear — a screen that is permanently invisible reads exactly like a
            // screen that was never converted.
            _canvas.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameServices.Match != null) GameServices.Match.MatchEnded += OnMatchWon;
            if (GameServices.Stats != null) GameServices.Stats.RecordReady += OnRecordReady;
        }

        private void OnDisable()
        {
            if (GameServices.Match != null) GameServices.Match.MatchEnded -= OnMatchWon;
            if (GameServices.Stats != null) GameServices.Stats.RecordReady -= OnRecordReady;

            // ⚠⚠ WHOEVER STOPPED TIME RESTORES IT, ON EVERY PATH INCLUDING DEATH. This
            // board was the second class in the project to stop the clock from an instance and
            // restore it only from a button, which is the exact lifetime fault `Hitstop`'s own
            // header documents at length. Destroy this object while the board is up, which a
            // scene unload, a host tearing the match down or a probe ending a run all do, and
            // `Time.timeScale` stayed 0 for the rest of the process, so the MENUS the player
            // returned to were frozen and nothing said why.
            RestoreTime();
        }

        private void OnDestroy() => RestoreTime();

        /// <summary>Undoes this board's own pause, and only its own.</summary>
        private void RestoreTime()
        {
            if (!_stoppedTime) return;

            _stoppedTime = false;
            Time.timeScale = 1.0f;
        }

        /// <summary>True while THIS board is the reason the match clock is stopped.</summary>
        private bool _stoppedTime;

        /// <summary>Shown when the match ends. -1 is a genuine draw, not an error.</summary>
        public void OnMatchWon(int winningSlot)
        {
            _canvas.gameObject.SetActive(true);

            if (winningSlot < 0)
            {
                _message.text = $"DRAW  —  {TiedNames()}";
                _message.color = UiTheme.Highlight;
            }
            else
            {
                _message.text =
                    $"{NameFor(winningSlot)} WINS THE MATCH!  {GameServices.Match.ScoreFor(winningSlot)} PTS";
                _message.color = UiTheme.Cream;
            }

            string mode = SceneFlow.SelectedMode == Core.GameMode.HeroStrike
                ? "HERO STRIKE"
                : "CLASSIC";
            int rounds = Core.MatchRules.RoundCountFor(SceneFlow.SelectedMode);
            _broadcastLine.text = $"{mode}  ·  FINAL STANDINGS  ·  {rounds} ROUNDS";
            _broadcastLine.color = SceneFlow.SelectedMode == Core.GameMode.HeroStrike
                ? UiTheme.Highlight
                : UiTheme.Amber;

            RenderStandings(winningSlot);

            // ⚠️ READ BACK RATHER THAN WAITED FOR. On the host the record is already written by
            // the time this runs; on a client it is not, and `RecordReady` fills the line in
            // when the broadcast lands. Clearing it first stops the PREVIOUS match's summary
            // sitting under a rematch's standings.
            if (_yourMatchLine != null) _yourMatchLine.text = "";
            if (GameServices.Stats?.Last != null) OnRecordReady(GameServices.Stats.Last);

            _rematchVotes.Clear();
            _rematch.gameObject.SetActive(!IsSpectator);
            _rematch.interactable = true;

            // ⚠️ THE TALLY STARTS EMPTY RATHER THAN AT "0 / n". A count nobody has contributed
            // to yet is not information, and the line is shared with the broadcast message.
            ShowTally(0, 1);

            // The cursor has been locked for the whole match; the board is the first thing
            // since the menu that wants a pointer.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // ⚠️ SINGLE PLAYER PAUSES, NETWORKED DOES NOT. A networked peer that froze its own
            // time would stop answering the host.
            if (!NetAuthority.IsNetworked)
            {
                Time.timeScale = 0.0f;
                _stoppedTime = true;
            }

            PlayTheWin();
        }

        /// <summary>
        /// The sound of winning: the sting, and the bed handing back to the menu.
        ///
        /// ⚠⚠ `match_win` IS IN THE LIVE CUE CATALOGUE AND HAD NO CALLER ANYWHERE. 🧑
        /// 2026-08-30: *"No audio cue on victory, like wala ung jingle unlke last time"*. It is
        /// listed in `AudioCues`, it is one of the six names in `DuckTriggers` so the bed was
        /// already written to get out of its way, and `grep` across `Assets` finds the string in
        /// the catalogue, in that duck list and in a test — and in no code that plays it. The
        /// same dead-feature shape `MatchInstaller` records for `DebugPlayerSwitcher` and
        /// `SpectatorCamera`: the work was done and nothing selected it.
        ///
        /// ⚠⚠ AND THE MATCH BED KEPT PLAYING OVER THE BOARD. *"Round Music still plays after
        /// winning instead of Main Menu"*. `Hud` starts `"match"` at the countdown and nothing
        /// ever ended it, so the standings went up over the round loop. Handing back to
        /// `"menu"` here rather than at the scene change means the change lands on the frame the
        /// result appears, which is the moment it means something.
        ///
        /// ⚠ IT IS `Play`, NOT `StopNow`. Silence over a result board reads as the audio having
        /// crashed, which is the same misreading `DownedVignette`'s header records about a held
        /// red tint. `MusicDirector.Play` crossfades, and it is idempotent on the name, so the
        /// lobby this screen leads back to does not restart the track.
        ///
        /// ⚠ THE STING IS PLAYED AT THE CAMERA, like every other UI cue in the project
        /// (`Hud`'s `sfx_super_ready` is the pattern). The audio rig is 3D, so a cue played at
        /// the origin of a match whose camera is thirty metres away arrives quiet and panned.
        /// </summary>
        private void PlayTheWin()
        {
            var camera = UnityEngine.Camera.main;
            GameServices.Audio?.PlayAt(
                "match_win", camera != null ? camera.transform.position : Vector3.zero);

            GameServices.Music?.Play("menu", GameServices.MenuTrack);
        }

        /// <summary>
        /// The board. Every seat is listed, in ranking order.
        ///
        /// ⚠️ A DRAW MARKS EVERY TIED LEADER WITH "=" RATHER THAN "1". Printing 1 and 2 for
        /// two players on identical scores states a winner the rules did not pick.
        /// </summary>
        private void RenderStandings(int winningSlot)
        {
            var m = GameServices.Match;
            int[] order = m.Ranking();
            int topScore = order.Length > 0 ? m.ScoreFor(order[0]) : 0;
            bool drawn = winningSlot < 0;

            for (int i = 0; i < _rows.Count; i++)
            {
                var cells = _rows[i];

                if (i >= order.Length)
                {
                    foreach (var c in cells) c.enabled = false;
                    continue;
                }

                foreach (var c in cells) c.enabled = true;

                int slot = order[i];
                int points = m.ScoreFor(slot);
                bool tiedAtTop = points == topScore;

                Color colour = tiedAtTop ? UiTheme.Highlight : UiTheme.Cream;

                cells[0].text = drawn && tiedAtTop ? "=" : $"{i + 1}";
                cells[1].text = NameFor(slot);
                cells[2].text = $"{points} PTS";

                foreach (var c in cells) c.color = colour;
            }
        }

        /// <summary>
        /// `FUTURE.md` § 19.2 step 5: the end-of-match summary, showing what this match added.
        ///
        /// ⚠️ IT IS THIS MATCH'S LINE, NOT THE CAREER TOTAL. A career total on the results
        /// board answers a question nobody is asking at that moment, and the career page exists
        /// for it. What a player wants here is what they just did.
        ///
        /// ⚠️ THE UPLOAD STATE IS PART OF THE SENTENCE. A match played with the internet
        /// unplugged is kept and sent on the next sign-in, and saying so is the difference
        /// between a game that looks like it lost your match and one that tells you it did not.
        /// </summary>
        private void OnRecordReady(Core.MatchRecord record)
        {
            if (_yourMatchLine == null || record == null) return;

            string me = GameServices.Account?.ConnectionToken ?? Net.NetIdentity.Token;
            var line = Core.MatchRecordRules.LineFor(record, me);
            if (line == null)
            {
                // A spectator has no line, and a spectated match adds nothing to a career.
                _yourMatchLine.text = "";
                return;
            }

            var career = GameServices.Career;
            string queued = career != null && career.QueuedCount > 0
                ? "  ·  SAVED ON THIS MACHINE, WILL UPLOAD"
                : "";

            string clutch = Core.MatchRecordRules.IsClutch(record, line.Slot) ? "  ·  CLUTCH" : "";

            _yourMatchLine.text =
                $"YOUR MATCH   {line.Knockdowns} KNOCKDOWNS  ·  {line.Retrievals} RETRIEVALS  ·  " +
                $"{line.Tags} TAGS  ·  {Core.MatchRecordRules.PassiveDefenceSeconds(line):0} s DEFENDING" +
                clutch + queued;
        }

        private static string NameFor(int slot)
        {
            var who = GameServices.Round?.PlayerAt(slot);
            return who != null ? who.DisplayName() : $"P{slot + 1}";
        }

        /// <summary>Everyone level at the top of a draw, joined for the headline.</summary>
        private static string TiedNames()
        {
            var m = GameServices.Match;
            int[] order = m.Ranking();
            if (order.Length == 0) return "";

            int top = m.ScoreFor(order[0]);
            var names = new List<string>();

            foreach (int slot in order)
                if (m.ScoreFor(slot) == top) names.Add(NameFor(slot));

            return string.Join(" · ", names);
        }

        private void Build()
        {
            var canvasGo = new GameObject("ResultCanvas");

            // ⚠️ UNDER THE HUD, SO THE CLEAN FEED TAKES IT WITH THEM. Same parenting and the
            // same reason as `RoleSwapCard.Build`: see `Hud.CleanFeedRoot`.
            var hud = UnityEngine.Object.FindFirstObjectByType<Hud>();
            canvasGo.transform.SetParent(hud != null ? hud.CleanFeedRoot : transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;   // over the HUD

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            AspectSafeCanvas.Apply(scaler);
            canvasGo.AddComponent<GraphicRaycaster>();

            // ⚠️ THE BACKDROP IS THE INK NAVY AT 0.72, NOT BLACK AT 0.55. `MatchResult.tscn`
            // authors `Color(0.015686, 0.031373, 0.219608, 0.72)`, the same colour the
            // intermission card dims with. Black at half strength leaves the lit arena reading
            // through the standings, which is a large part of why this screen photographed as
            // muddy rather than as a board.
            MenuKit.Backdrop(canvasGo.transform, new Color(0.015686f, 0.031373f, 0.219608f, 0.72f));

            // ⚠️⚠️ 600 x 340 IS THE .tscn's CARD AND THE PORT DREW 860 x 660. Nearly double the
            // area, with everything inside it placed by hand at a size chosen to fill that area
            // rather than at the size it is authored — the message at 42 against a Display
            // variation, the standings at 30 against 24. Everything read oversized and loose,
            // which is 🧑's *"the end win screen UI ... looks ugly comapred to godot"*.
            //
            // ⚠️ AND IT IS A FLOOR, NOT A FIXED SIZE. Godot's `PanelContainer` clamps UP to its
            // content, so a long name grows the card instead of clipping. A layout group plus a
            // minimum is the same rule here. Nothing on this card gets a hard maximum.
            var card = BuildCard(canvasGo.transform);

            _message = CardLabel(card, "MessageLabel", 34, UiTheme.Cream, 76,
                                 TextAnchor.MiddleCenter);

            _broadcastLine = CardLabel(card, "BroadcastLine", 18, UiTheme.Amber, 30,
                                       TextAnchor.MiddleCenter);
            _broadcastLine.text = "FINAL STANDINGS";

            _yourMatchLine = CardLabel(card, "YourMatchLine", 17, UiTheme.CreamMuted, 28,
                                       TextAnchor.MiddleCenter);

            Spacer(card, 10.0f);

            var standings = SubStack(card, "Standings", 10.0f);

            for (int i = 0; i < Core.Balance.PlayerCount; i++) _rows.Add(BuildPlaceRow(standings));

            Spacer(card, 16.0f);

            // ⚠️ STACKED, NOT SIDE BY SIDE, AND THE SECOND ONE SAYS "MAIN MENU". Both come
            // straight off the .tscn, which puts `RematchButton` above `MenuButton` in the same
            // VBox. Two 280-wide buttons in a row do not fit a 600-wide card at all, which is
            // the kind of thing an oversized card hides.
            _rematch = StackedButton(card, "REMATCH", OnRematchPressed);

            // ⚠️ UNDER THE BUTTON IT DESCRIBES, at the broadcast line's size rather than the
            // button's. It is a status, not a second call to action, and 🧑's note on the
            // Ilalim ng Tulay objective card applies here too: `HudLabel` does not wrap, so this
            // sits on a card the standings have already made wide enough for it.
            _rematchTally = CardLabel(card, "RematchTally", 18, UiTheme.Highlight, 24,
                                      TextAnchor.MiddleCenter);
            _rematchTally.text = "";

            _menu = StackedButton(card, "MAIN MENU", OnMenuPressed);
        }

        /// <summary>The wood card: a centred column that grows to fit what is put in it.</summary>
        private static VerticalLayoutGroup BuildCard(Transform parent)
        {
            var go = new GameObject("Card");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Centre;
            rt.anchorMax = Centre;
            rt.pivot = Centre;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600.0f, 0.0f);

            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            img.type = Image.Type.Sliced;

            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = 8.0f;
            column.padding = new RectOffset(28, 28, 22, 22);
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.childControlHeight = true;
            column.childControlWidth = true;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            BuildFold(go.transform);

            return column;
        }

        /// <summary>
        /// The turned-up corner, from `card_fold.gdshader` and the 16x16 node
        /// `MatchResult.tscn` anchors it to.
        ///
        /// ⚠️ A GENERATED SPRITE RATHER THAN A SHADER, and that is the cheaper answer to the
        /// same picture. Godot draws it in a fragment shader because a `ColorRect` has no other
        /// way to be a triangle; Unity can just hand an Image a 16x16 texture with the triangle
        /// already in it. No shader to compile, no material to own, and it cannot fail to load.
        ///
        /// ⚠️ IT IS PARENTED OUTSIDE THE LAYOUT'S FLOW. `Card` carries a VerticalLayoutGroup,
        /// which positions every child it controls; an Image added as a plain child would be
        /// laid out as a row of the card and push the buttons down by 16 px. Setting the
        /// RectTransform's anchors AFTER parenting is not enough on its own, so it is also
        /// excluded from the layout by having no LayoutElement and being ignored: see the
        /// `ignoreLayout` flag below.
        /// </summary>
        private static void BuildFold(Transform card)
        {
            var go = new GameObject("Fold");
            go.transform.SetParent(card, false);

            var rt = go.AddComponent<RectTransform>();

            // Bottom-right corner of the card, 16x16, exactly as the .tscn anchors it.
            rt.anchorMin = new Vector2(1.0f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 0.0f);
            rt.pivot = new Vector2(1.0f, 0.0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(FoldSize, FoldSize);

            var ignore = go.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = FoldSprite();
            img.color = Color.white;
        }

        private const int FoldSize = 16;
        private static Sprite _fold;

        /// <summary>
        /// The triangle itself: opaque below the anti-diagonal, transparent above it, in the
        /// same near-black navy the ink outline uses.
        ///
        /// ⚠️ CACHED, because every result screen builds a card and the texture is identical
        /// on all of them.
        /// </summary>
        private static Sprite FoldSprite()
        {
            if (_fold != null) return _fold;

            var tex = new Texture2D(FoldSize, FoldSize, TextureFormat.RGBA32, mipChain: false)
            {
                name = "CardFold",

                // ⚠️ CLAMP, NOT REPEAT. A bilinear tap at the edge of a repeating texture wraps
                // to the opposite side and draws a stray line of navy along the top of the fold.
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var fill = new Color(0.016f, 0.031f, 0.220f, 1.0f);
            var clear = new Color(0.016f, 0.031f, 0.220f, 0.0f);

            for (int y = 0; y < FoldSize; y++)
            {
                for (int x = 0; x < FoldSize; x++)
                {
                    // ⚠️ THE ROW INDEX IS FLIPPED AGAINST THE SHADER'S UV. Godot's UV origin is
                    // the TOP left and `SetPixel` counts from the BOTTOM, so the .gdshader's
                    // "lower-right triangle" is `u + v > 1` there and `x > y` here. Getting
                    // this backwards mirrors the fold onto the wrong corner, which looks
                    // deliberate and is the kind of thing nobody reports.
                    float u = (x + 0.5f) / FoldSize;
                    float v = (y + 0.5f) / FoldSize;

                    tex.SetPixel(x, y, u > v ? fill : clear);
                }
            }

            tex.Apply();

            _fold = Sprite.Create(tex, new Rect(0, 0, FoldSize, FoldSize),
                                  new Vector2(0.5f, 0.5f), pixelsPerUnit: FoldSize);
            _fold.name = "CardFold";

            return _fold;
        }

        private static Text CardLabel(VerticalLayoutGroup card, string name, int size,
                                      Color colour, float height, TextAnchor align)
        {
            var label = MenuKit.Label(card.transform, "", size, colour,
                Centre, Vector2.zero, new Vector2(540.0f, height), align);

            label.gameObject.name = name;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            return label;
        }

        private static void Spacer(VerticalLayoutGroup card, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(card.transform, false);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private static VerticalLayoutGroup SubStack(VerticalLayoutGroup card, string name,
                                                    float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(card.transform, false);

            var stack = go.AddComponent<VerticalLayoutGroup>();
            stack.spacing = spacing;
            stack.childForceExpandHeight = false;
            stack.childForceExpandWidth = true;
            stack.childControlHeight = true;
            stack.childControlWidth = true;

            return stack;
        }

        /// <summary>Place, name, points. The .tscn's own 48 / 260 / 120 columns at 24, with 18
        /// between them, so the three columns line up down the table.</summary>
        private static Text[] BuildPlaceRow(VerticalLayoutGroup standings)
        {
            var rowGo = new GameObject("Place", typeof(RectTransform));
            rowGo.transform.SetParent(standings.transform, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18.0f;
            row.childForceExpandWidth = false;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childAlignment = TextAnchor.MiddleLeft;

            var place = PlaceCell(rowGo.transform, "Place", TextAnchor.MiddleLeft, 48.0f, 0.0f);
            var name = PlaceCell(rowGo.transform, "Name", TextAnchor.MiddleLeft, 260.0f, 1.0f);
            var points = PlaceCell(rowGo.transform, "Points", TextAnchor.MiddleRight, 120.0f, 0.0f);

            return new[] { place, name, points };
        }

        private static Text PlaceCell(Transform parent, string name, TextAnchor align,
                                      float width, float flexible)
        {
            var label = MenuKit.Label(parent, "", 24, UiTheme.Cream,
                Centre, Vector2.zero, new Vector2(width, 34.0f), align);

            label.gameObject.name = name;

            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = 34.0f;
            element.flexibleWidth = flexible;

            return label;
        }

        private static Button StackedButton(VerticalLayoutGroup card, string text,
                                            System.Action onPressed)
        {
            var button = MenuKit.WoodButton(card.transform, text, Centre,
                Vector2.zero, new Vector2(360.0f, 60.0f), onPressed);

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 60.0f;
            element.preferredHeight = 60.0f;

            return button;
        }

        private void OnRematchPressed()
        {
            // ⚠️ THE SCOREBOARD MUST NOT DISAPPEAR ON THE PRESS. 🧑: *"when rematch happens the
            // UI for the scoreboard doesnt dissappear"* — it stays up until the rematch is
            // actually agreed and the next round starts.
            RestoreTime();

            // Single player is a vote of one, so it starts immediately.
            if (!NetAuthority.IsNetworked)
            {
                _rematchVotes.Add(0);
                BeginRematchNow();
                return;
            }

            // ⚠️⚠️ THE LOCAL PRESS IS NOT COUNTED LOCALLY. It goes to the host like every other
            // peer's and comes back in the broadcast tally. Counting it here as well would give
            // this screen a number the host does not have, and the first thing a player would
            // see is their own count disagreeing with everybody else's.
            // ⚠️⚠️ THE BUTTON IS ONLY DEADENED ONCE THE VOTE HAS ACTUALLY LEFT. It was disabled
            // first and the send attempted afterwards, so a vote that could not be delivered
            // (the host gone, or the transport still finishing its handshake) left the player
            // staring at a dead REMATCH button with no way to try again and no tally to explain
            // it. `DeclareReadyServerRpc` reports delivery for the same reason and
            // `ReadyGate.Update` resends off it.
            if (Net.MatchRpc.Instance == null || !Net.MatchRpc.Instance.VoteRematchServerRpc())
            {
                ShowTally(_rematchVotes.Count, ExpectedVotes());
                return;
            }

            _rematch.interactable = false;
            ShowTally(_rematchVotes.Count, ExpectedVotes());
        }

        /// <summary>
        /// HOST ONLY. A peer voted.
        ///
        /// ⚠️⚠️ THE ID IS A TRANSPORT PEER ID, NEVER A SEAT. NGO client id 0 is the host's
        /// identity and must remain distinct from client 1 even when the host occupies seat 1.
        /// </summary>
        public void HostReceiveVote(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            if (!_rematchVotes.Add(peerId)) return;   // idempotent, like the ready set

            int expected = ExpectedVotes();

            Net.MatchRpc.Instance?.RematchTallyClientRpc(_rematchVotes.Count, expected);
            ShowTally(_rematchVotes.Count, expected);

            if (_rematchVotes.Satisfied(expected))
            {
                Net.MatchRpc.Instance?.BeginRematchClientRpc();
                BeginRematchLocally();
            }
        }

        /// <summary>
        /// HOST ONLY. A peer disconnected while the vote was open.
        ///
        /// ⚠️ RE-CHECKED ON A LEAVE, NOT ONLY ON A PRESS, and this is `ReadyGate.OnPeerLeft`'s
        /// note verbatim because it is the same failure: the last player anybody was waiting for
        /// closing the game leaves the rest sitting on a gate that is already satisfied.
        /// </summary>
        public void OnPeerLeft(int peerId)
        {
            if (!NetAuthority.IsHost) return;
            if (!_canvas.gameObject.activeSelf) return;

            _rematchVotes.Remove(peerId);

            int expected = ExpectedVotes();
            Net.MatchRpc.Instance?.RematchTallyClientRpc(_rematchVotes.Count, expected);
            ShowTally(_rematchVotes.Count, expected);

            if (_rematchVotes.Satisfied(expected))
            {
                Net.MatchRpc.Instance?.BeginRematchClientRpc();
                BeginRematchLocally();
            }
        }

        /// <summary>Votes counted so far. Host-side; a test's window into the tally.</summary>
        public int VoteCount => _rematchVotes.Count;

        /// <summary>Whether the real result board is currently accepting a rematch vote.</summary>
        public bool IsVisible => _canvas != null && _canvas.gameObject.activeSelf;

        /// <summary>The same action as the REMATCH button, exposed for the two-process driver.</summary>
        public void RequestRematch() => OnRematchPressed();

        /// <summary>What the tally line currently reads, or "" when it is silent.</summary>
        public string TallyText => _rematchTally != null ? _rematchTally.text : "";

        /// <summary>
        /// How many presses this rematch is waiting for.
        ///
        /// ⚠️ A PEER ID, NOT A SEAT, for the reason `ReadyGate.ExpectedReadyCount` spells out:
        /// the argument is matched against `PeerRecord.PeerId` and a seat number that happens to
        /// equal somebody else's client id forgives the wrong peer.
        /// </summary>
        public int ExpectedVotes()
        {
            var lobby = Net.NetSession.Instance?.Lobby;
            return lobby?.PlayingPeerCount() ?? 1;
        }

        /// <summary>
        /// What the tally reads.
        ///
        /// ⚠️ PURE AND STATIC SO IT CAN BE ASSERTED WITHOUT BUILDING THE SCREEN. `Awake` builds
        /// a Canvas, a theme and eight labels, none of which a test of a sentence needs, and a
        /// test that has to stand a whole UI up to check a string is a test nobody runs.
        ///
        /// ⚠️ BLANK IN SINGLE PLAYER. "1 / 1 WANT A REMATCH" is a sentence about nobody, printed
        /// under a button that has already started the match.
        /// </summary>
        public static string TallyLine(int votes, int expected)
            => expected <= 1 ? "" : $"{votes} / {expected} WANT A REMATCH";

        /// <summary>Draw "2 / 3 WANT A REMATCH" on whichever peer this is.</summary>
        public void ShowTally(int votes, int expected)
        {
            if (_rematchTally == null) return;
            _rematchTally.text = TallyLine(votes, expected);
        }

        /// <summary>Every playing peer agreed, or this is single player. Start.</summary>
        public void BeginRematchLocally() => BeginRematchNow();

        private void BeginRematchNow()
        {
            _canvas.gameObject.SetActive(false);
            _rematch.interactable = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // ⚠️ THE HOST STOPS ASSERTING "NO MATCH RUNNING" ACROSS ITS OWN RELOAD. See
            // `Net.MatchRpc.HostBeginningArenaLoad` and `docs/TODO.md` § 82.3: the rematch
            // reloads on every peer and carries the identical race `HostStartMatch` does, so a
            // latch set only there would be correct for the first match of a session and for no
            // other. It clears itself on the first packet after this host's own match is live.
            Net.MatchRpc.HostBeginningArenaLoad();

            // ⚠️ ONLY THE HOST STARTS THE MATCH. Every peer hides its own board and unlocks its
            // own cursor, which is local presentation, but `StartMatch` writes match state and
            // `CLAUDE.md` § 4 keeps that on the host: four peers each starting a match is four
            // matches. Clients arrive through the host's own round start.
            if (!NetAuthority.IsNetworked || NetAuthority.IsHost)
                GameServices.Match?.StartMatch();
        }

        /// <summary>
        /// ⚠️⚠️ IT ENDS THE SESSION NOW, WHICH THESE FOUR LINES NEVER DID. See
        /// `SceneFlow.LeaveMatchToMainMenu`: `NetworkManager` is `DontDestroyOnLoad`, so a host
        /// pressing MAIN MENU walked to the title screen and went on hosting, and the other three
        /// stayed in a match with no referee until a thirty-second silence timer noticed.
        ///
        /// ⚠️ `RestoreTime` STAYS AND IS NOT THE SAME AS THE CLOCK RESET IN THERE. This board
        /// slows time on the way IN and owns the value it slowed from; the exit only guarantees
        /// the scale is 1 for whatever comes next.
        /// </summary>
        private void OnMenuPressed()
        {
            RestoreTime();
            SceneFlow.LeaveMatchToMainMenu();
        }
    }
}
