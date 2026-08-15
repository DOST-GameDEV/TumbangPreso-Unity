using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// THE INTERMISSION CARD — the four seconds between one round and the next. Converted
    /// from `scripts/ui/role_swap_card.gd`.
    ///
    /// Timeline (Dev_Plan.md §4.6):
    ///   0.0s  intermission starts → headline, swap and standings show
    ///   1.2s  the two panels rise and fade in
    ///   3.5s  "ROUND N — FIGHT!" briefly shown
    ///   ~4.0s round starts → card hides, panels reset for next time
    ///
    /// ⚠️⚠️ WHAT THIS CARD IS FOR, WRITTEN DOWN BECAUSE A PREVIOUS VERSION LOST IT. 🧑
    /// 2026-08-02, with a screenshot: *"i dont get this shit at all, like what is it supposed
    /// to tell me? pls revamp the boxes here and whats supposed to go to them"*.
    ///
    /// It had two boxes saying the same swap twice, forwards and backwards. Every word was
    /// true and none of it useful. A player has exactly two questions at a round boundary and
    /// this card answers both, in the order they are asked:
    ///
    ///   1. **WHO DEFENDS NEXT** — the one fact that changes how the next 90 s is played, and
    ///      the one thing a player cannot work out alone (the rotation is clockwise by slot,
    ///      which nothing on the HUD spells out).
    ///   2. **WHERE DO I STAND** — the match is cumulative with no per-round winner
    ///      (`Design.md` §1), so the standings ARE the story. Naming one leader and a number
    ///      tells the leader something and everybody else nothing.
    ///
    /// ⚠️ DO NOT "SIMPLIFY" THIS BACK INTO A SINGLE SWAP LINE. That is the version he
    /// rejected by screenshot.
    /// </summary>
    public sealed class RoleSwapCard : MonoBehaviour
    {
        public const float RevealDelay = 1.2f;
        public const float FightDelay = 2.3f;
        public const float RevealFade = 0.35f;
        public const float FightHold = 0.4f;

        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        private Canvas _canvas;
        private Text _title, _headline, _tayaName, _attackerNames, _fight;
        private CanvasGroup _swapPanel, _standingsPanel;
        private readonly List<Text[]> _rows = new List<Text[]>();

        private void Awake()
        {
            Build();
            _canvas.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameServices.Match == null) return;
            GameServices.Match.IntermissionStarted += OnIntermissionStarted;
            GameServices.Match.RoundStarted += OnRoundStarted;
        }

        private void OnDisable()
        {
            if (GameServices.Match == null) return;
            GameServices.Match.IntermissionStarted -= OnIntermissionStarted;
            GameServices.Match.RoundStarted -= OnRoundStarted;
        }

        private void OnIntermissionStarted(int nextRound, int nextDefenderSlot)
        {
            _title.text = $"END OF ROUND {Mathf.Max(1, nextRound - 1)}";
            _headline.text = HeadlineText();

            _tayaName.text = NameOf(nextDefenderSlot);

            var others = new List<string>();
            for (int slot = 0; slot < Core.Balance.PlayerCount; slot++)
                if (slot != nextDefenderSlot) others.Add(NameOf(slot));
            _attackerNames.text = string.Join(" · ", others);

            FillStandings(nextDefenderSlot);

            _fight.enabled = false;
            _canvas.gameObject.SetActive(true);
            _swapPanel.alpha = 0.0f;
            _standingsPanel.alpha = 0.0f;

            StopAllCoroutines();
            StartCoroutine(RunTimeline(nextRound));
        }

        private IEnumerator RunTimeline(int nextRound)
        {
            yield return new WaitForSeconds(RevealDelay);
            yield return StartCoroutine(RevealPanels());

            yield return new WaitForSeconds(FightDelay);

            _fight.text = $"ROUND {nextRound} — FIGHT!";
            _fight.enabled = true;

            yield return new WaitForSeconds(FightHold);
            _fight.enabled = false;
        }

        /// <summary>The two panels rise and fade together.</summary>
        private IEnumerator RevealPanels()
        {
            float t = 0.0f;
            while (t < RevealFade)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / RevealFade);

                _swapPanel.alpha = k;
                _standingsPanel.alpha = k;

                // A slight overshoot on the way in, matching the original's BACK/EASE_OUT.
                float scale = Mathf.LerpUnclamped(0.94f, 1.0f, 1.0f - Mathf.Pow(1.0f - k, 3.0f));
                _swapPanel.transform.localScale = Vector3.one * scale;
                _standingsPanel.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            _swapPanel.alpha = 1.0f;
            _standingsPanel.alpha = 1.0f;
        }

        private void OnRoundStarted(int roundNumber, int defenderSlot)
        {
            StopAllCoroutines();
            _canvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// What the round just produced. It stays because it is the only place the
        /// passive-defence payout is ever visible, and that number being enormous is a known
        /// balance risk somebody has to be able to SEE.
        /// </summary>
        private static string HeadlineText()
        {
            var m = GameServices.Match;
            if (m == null) return "";

            int[] order = m.Ranking();
            return order.Length == 0
                ? ""
                : $"{NameOf(order[0])} leads on {m.ScoreFor(order[0])} PTS";
        }

        private void FillStandings(int nextDefenderSlot)
        {
            var m = GameServices.Match;
            int[] order = m.Ranking();

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

                // ⚠️ THE NEXT TAYA IS MARKED IN THE DEFENCE COLOUR, and that is the whole
                // point of showing standings here rather than on the HUD: it answers "who
                // defends next" and "where do I stand" in one glance.
                Color colour = slot == nextDefenderSlot ? UiTheme.Defense : UiTheme.Cream;

                cells[0].text = $"{i + 1}";
                cells[1].text = NameOf(slot);
                cells[2].text = m.ScoreFor(slot).ToString();

                foreach (var c in cells) c.color = colour;
            }
        }

        private static string NameOf(int slot)
        {
            var who = GameServices.Round?.PlayerAt(slot);
            return who != null ? who.DisplayName() : $"P{slot + 1}";
        }

        private void Build()
        {
            var canvasGo = new GameObject("RoleSwapCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            MenuKit.Backdrop(canvasGo.transform, new Color(0.0f, 0.0f, 0.0f, 0.45f));

            _title = MenuKit.Label(canvasGo.transform, "", 40, UiTheme.Cream,
                Centre, new Vector2(0, 300), new Vector2(900, 60));

            _headline = MenuKit.Label(canvasGo.transform, "", 26, UiTheme.Highlight,
                Centre, new Vector2(0, 244), new Vector2(900, 40));

            _swapPanel = BuildPanel(canvasGo.transform, "SwapPanel",
                new Vector2(0, 110), new Vector2(760, 150));

            MenuKit.Label(_swapPanel.transform, "TAYA", 22, UiTheme.Defense,
                Centre, new Vector2(-250, 40), new Vector2(200, 34));
            _tayaName = MenuKit.Label(_swapPanel.transform, "", 30, UiTheme.Cream,
                Centre, new Vector2(-250, 0), new Vector2(300, 40));

            MenuKit.Label(_swapPanel.transform, "ATTACKERS", 22, UiTheme.Offense,
                Centre, new Vector2(180, 40), new Vector2(240, 34));
            _attackerNames = MenuKit.Label(_swapPanel.transform, "", 26, UiTheme.Cream,
                Centre, new Vector2(180, 0), new Vector2(420, 40));

            _standingsPanel = BuildPanel(canvasGo.transform, "StandingsPanel",
                new Vector2(0, -120), new Vector2(760, 260));

            for (int i = 0; i < Core.Balance.PlayerCount; i++)
            {
                float y = 80 - i * 52;

                var rank = MenuKit.Label(_standingsPanel.transform, "", 26, UiTheme.Cream,
                    Centre, new Vector2(-300, y), new Vector2(60, 36), TextAnchor.MiddleLeft);
                var who = MenuKit.Label(_standingsPanel.transform, "", 26, UiTheme.Cream,
                    Centre, new Vector2(-40, y), new Vector2(420, 36), TextAnchor.MiddleLeft);
                var score = MenuKit.Label(_standingsPanel.transform, "", 26, UiTheme.Cream,
                    Centre, new Vector2(300, y), new Vector2(120, 36), TextAnchor.MiddleRight);

                _rows.Add(new[] { rank, who, score });
            }

            _fight = MenuKit.Label(canvasGo.transform, "", 64, UiTheme.Highlight,
                Centre, Vector2.zero, new Vector2(1200, 100));
            _fight.enabled = false;
        }

        private static CanvasGroup BuildPanel(Transform parent, string name,
            Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = UiTheme.WoodDeep;
            MenuKit.Place(img.rectTransform, Centre, offset, size);

            var edge = new GameObject("Edge");
            edge.transform.SetParent(go.transform, false);
            var edgeImg = edge.AddComponent<Image>();
            edgeImg.color = UiTheme.WoodEdge;
            edgeImg.raycastTarget = false;
            MenuKit.Stretch(edgeImg.rectTransform, 3.0f);
            edge.transform.SetAsFirstSibling();

            return go.AddComponent<CanvasGroup>();
        }
    }
}
