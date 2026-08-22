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
        private readonly List<Text[]> _rows = new List<Text[]>();
        private Button _rematch;
        private Button _menu;

        /// <summary>Seats that have voted for a rematch.</summary>
        private readonly HashSet<int> _rematchVotes = new HashSet<int>();

        /// <summary>
        /// ⚠️⚠️ EVERY PLAYING PEER VOTES ON A REMATCH, NOT ONLY THE HOST. 🧑 2026-08-01:
        /// *"in multiplayer only host has rematch and this doesnt disappear... can we make it
        /// so that they all can click rematch button (only the humans playing) and if they
        /// all check the rematch goes on"*, and separately *"spectator shouldnt see rematch
        /// button js scoreboard."*
        ///
        /// The vote collection itself needs peer identity and is pending netcode; the button
        /// is hidden for a spectator here, which is the half that does not need the wire.
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
        }

        private void OnDisable()
        {
            if (GameServices.Match != null) GameServices.Match.MatchEnded -= OnMatchWon;

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
            _broadcastLine.text = $"{mode}  ·  FINAL STANDINGS  ·  {Core.Balance.Rounds} ROUNDS";
            _broadcastLine.color = SceneFlow.SelectedMode == Core.GameMode.HeroStrike
                ? UiTheme.Highlight
                : UiTheme.Amber;

            RenderStandings(winningSlot);

            _rematchVotes.Clear();
            _rematch.gameObject.SetActive(!IsSpectator);

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

            Spacer(card, 10.0f);

            var standings = SubStack(card, "Standings", 10.0f);

            for (int i = 0; i < Core.Balance.PlayerCount; i++) _rows.Add(BuildPlaceRow(standings));

            Spacer(card, 16.0f);

            // ⚠️ STACKED, NOT SIDE BY SIDE, AND THE SECOND ONE SAYS "MAIN MENU". Both come
            // straight off the .tscn, which puts `RematchButton` above `MenuButton` in the same
            // VBox. Two 280-wide buttons in a row do not fit a 600-wide card at all, which is
            // the kind of thing an oversized card hides.
            _rematch = StackedButton(card, "REMATCH", OnRematchPressed);
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
            _rematchVotes.Add(0);
            RestoreTime();

            // Single player is a vote of one, so it starts immediately. The networked path
            // waits for every playing peer and is pending netcode.
            if (!NetAuthority.IsNetworked) BeginRematchNow();
        }

        private void BeginRematchNow()
        {
            _canvas.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            GameServices.Match?.StartMatch();
        }

        private void OnMenuPressed()
        {
            RestoreTime();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneFlow.Go(SceneFlow.MainMenu);
        }
    }
}
