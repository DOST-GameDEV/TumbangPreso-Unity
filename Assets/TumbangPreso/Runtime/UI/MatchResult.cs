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
        }

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

            RenderStandings(winningSlot);

            _rematchVotes.Clear();
            _rematch.gameObject.SetActive(!IsSpectator);

            // The cursor has been locked for the whole match; the board is the first thing
            // since the menu that wants a pointer.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // ⚠️ SINGLE PLAYER PAUSES, NETWORKED DOES NOT. A networked peer that froze its own
            // time would stop answering the host.
            if (!NetAuthority.IsNetworked) Time.timeScale = 0.0f;
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
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;   // over the HUD

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // A dimmed backdrop so the board reads over whatever the arena was showing.
            MenuKit.Backdrop(canvasGo.transform, new Color(0.0f, 0.0f, 0.0f, 0.55f));

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(canvasGo.transform, false);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = UiTheme.WoodDeep;
            MenuKit.Place(cardImg.rectTransform, Centre, Vector2.zero, new Vector2(860, 660));

            var edge = new GameObject("Edge");
            edge.transform.SetParent(cardGo.transform, false);
            var edgeImg = edge.AddComponent<Image>();
            edgeImg.color = UiTheme.WoodEdge;
            edgeImg.raycastTarget = false;
            MenuKit.Stretch(edgeImg.rectTransform, 4.0f);
            edge.transform.SetAsFirstSibling();

            var card = cardGo.transform;

            _message = MenuKit.Label(card, "", 42, UiTheme.Cream,
                Centre, new Vector2(0, 235), new Vector2(800, 70));

            for (int i = 0; i < Core.Balance.PlayerCount; i++)
            {
                float y = 100 - i * 64;

                var place = MenuKit.Label(card, "", 30, UiTheme.Cream,
                    Centre, new Vector2(-330, y), new Vector2(60, 40), TextAnchor.MiddleLeft);
                var name = MenuKit.Label(card, "", 30, UiTheme.Cream,
                    Centre, new Vector2(-60, y), new Vector2(420, 40), TextAnchor.MiddleLeft);
                var pts = MenuKit.Label(card, "", 30, UiTheme.Cream,
                    Centre, new Vector2(300, y), new Vector2(160, 40), TextAnchor.MiddleRight);

                _rows.Add(new[] { place, name, pts });
            }

            _rematch = MenuKit.WoodButton(card, "REMATCH", Centre,
                new Vector2(-160, -240), new Vector2(280, 72), OnRematchPressed);

            _menu = MenuKit.WoodButton(card, "MENU", Centre,
                new Vector2(160, -240), new Vector2(280, 72), OnMenuPressed);
        }

        private void OnRematchPressed()
        {
            // ⚠️ THE SCOREBOARD MUST NOT DISAPPEAR ON THE PRESS. 🧑: *"when rematch happens the
            // UI for the scoreboard doesnt dissappear"* — it stays up until the rematch is
            // actually agreed and the next round starts.
            _rematchVotes.Add(0);
            Time.timeScale = 1.0f;

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
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneFlow.Go(SceneFlow.MainMenu);
        }
    }
}
