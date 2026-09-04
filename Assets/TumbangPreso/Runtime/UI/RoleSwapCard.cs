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
        private Text _title, _headline, _tayaName, _attackerNames, _fight, _bufferPrompt;
        private CanvasGroup _swapPanel, _standingsPanel;
        private readonly List<Text[]> _rows = new List<Text[]>();
        private float _bufferRemaining;
        private bool _isBufferActive;

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

        private void Update()
        {
            if (_isBufferActive && _canvas != null && _canvas.gameObject.activeSelf)
            {
                _bufferRemaining = Mathf.Max(0.0f, _bufferRemaining - Time.deltaTime);
                if (_bufferPrompt != null)
                {
                    _bufferPrompt.text = $"WARMUP / PRACTICE BUFFER: {Mathf.CeilToInt(_bufferRemaining)}s\n[SPACE] / [CLICK] TO DISMISS SCORES & PRACTICE NOW";
                }

                // ⚠️ THE PAD IS THE FOURTH WAY IN AND IT WAS MISSING. This card is shown
                // during a match, so it is not focusable and has no button to move to: a pad
                // player could read `[SPACE] / [CLICK] TO DISMISS` while holding a device with
                // neither. See `InputLayer.MenuNav.PadSubmitPressed`.
                // ⚠️ ESCAPE IS INSIDE `MenuNav.CancelPressed` RATHER THAN BESIDE IT, so this file
                // holds no keyboard literal of its own. That is what lets
                // `ControllerSupportTests` guard the whole runtime by reading it as text: a
                // twelfth screen added next month with its own `GetKeyDown(KeyCode.Escape)` is
                // exactly as silent as the eleven were, so the check has to be on the shape.
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)
                    || InputLayer.MenuNav.PadSubmitPressed
                    || InputLayer.MenuNav.CancelPressed)
                {
                    DismissAndPractice();
                }
            }
        }

        public void DismissAndPractice()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
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

            _bufferRemaining = Core.Balance.WarmupBufferDuration;
            _isBufferActive = true;

            GameServices.Audio?.PlayAt("round_end", Vector3.zero);

            StopAllCoroutines();
            StartCoroutine(RunTimeline(nextRound));
        }

        /// <summary>
        /// Raise the card exactly as an intermission would, without the event.
        ///
        /// ⚠️ TWO CALLERS AND NEITHER IS THE HOST'S NORMAL PATH. It was written for the capture
        /// pass, and `MatchRpc.SyncWorldSnapshotClientRpc` now uses it for a CLIENT, which never
        /// receives `IntermissionStarted` at all: that event is raised only by
        /// `MatchDirector.BeginIntermission`, which is host-only, and it cannot simply be raised
        /// on a client because `SliceRunner` is wired to it and would teleport every body and
        /// schedule its own `AdvanceRound`. `docs/TODO.md` § 57.2 has the reasoning.
        /// </summary>
        public void ShowForShot(int nextRound, int nextDefenderSlot)
            => OnIntermissionStarted(nextRound, nextDefenderSlot);

        private IEnumerator RunTimeline(int nextRound)
        {
            yield return new WaitForSeconds(RevealDelay);
            yield return StartCoroutine(RevealPanels());

            // Hold warmup buffer for remaining duration minus the final fight cue (1.5s)
            float waitTime = Mathf.Max(1.0f, Core.Balance.WarmupBufferDuration - RevealDelay - RevealFade - 1.5f);
            yield return new WaitForSeconds(waitTime);

            _isBufferActive = false;
            _fight.text = $"ROUND {nextRound} — FIGHT!";
            _fight.enabled = true;

            yield return new WaitForSeconds(1.5f);
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
        /// <remarks>
        /// ⚠️⚠️ IT IS THE TAYA'S PAYOUT, NOT THE LEADER'S TOTAL, AND THE PORT HAD THE WRONG
        /// SENTENCE. `role_swap_card.gd::_show_headline()` is
        /// `"%s HELD THE LATA FOR %d PTS"` against `MatchManager.defender_slot` — the round
        /// that just ended, and the one number on this card that is about the round rather than
        /// about the match. The port printed *"X leads on N PTS"* instead, which repeats what
        /// the standings table directly underneath already says, in a different order, and
        /// deletes the only readout the passive-defence payout has anywhere in the game.
        /// </remarks>
        private static string HeadlineText()
        {
            var m = GameServices.Match;
            if (m == null) return "";

            int taya = m.DefenderSlot;
            return $"{NameOf(taya)} HELD THE LATA FOR {m.ScoreFor(taya)} PTS";
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

            // ⚠️ UNDER THE HUD, SO THE CLEAN FEED TAKES IT WITH THEM. See `Hud.CleanFeedRoot`:
            // this card is a child of `HUD.tscn` in the Godot build for exactly this reason, and
            // a nested Canvas keeps its own sortingOrder while inheriting the parent's active
            // state. Falls back to this component's own transform when there is no HUD, which is
            // the headless-probe case.
            var hud = UnityEngine.Object.FindFirstObjectByType<Hud>();
            canvasGo.transform.SetParent(hud != null ? hud.CleanFeedRoot : transform, false);

            // ⚠️ THE NESTED CANVAS IS STRETCHED TO ITS PARENT. A child Canvas's RectTransform is
            // NOT driven the way a root one's is, so a fresh GameObject arrives here 0 by 0 at
            // the parent's centre and everything under it is laid out against nothing. This card
            // survived that because its backdrop and its column are both stretched or centred;
            // `YouCard` tried the same parenting on 2026-08-27, is anchored bottom-left with a
            // fixed rect, and `HudOverflowProbe` found it 274 units off the right edge.
            var canvasRt = canvasGo.AddComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ⚠️ `overrideSorting`, BECAUSE THIS IS A NESTED CANVAS. The note above says a nested
            // Canvas "keeps its own sortingOrder", and that is only true once it is told to
            // override the parent's: without this the 90 is ignored and the intermission card
            // draws in hierarchy order under the HUD's own rows.
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 90;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            AspectSafeCanvas.Apply(scaler);

            // ⚠️ THE BACKDROP IS DEEP NAVY AT 0.82, NOT BLACK AT 0.45. `RoleSwapCard.tscn`
            // authors `Color(0.015686, 0.031373, 0.219608, 0.82)` — the same ink the outlines
            // use, near-opaque. At 0.45 black the lit street reads straight through every line
            // of loose text on this card, which is most of why it photographed as mud.
            MenuKit.Backdrop(canvasGo.transform, new Color(UiTheme.WoodDark.r, UiTheme.WoodDark.g, UiTheme.WoodDark.b, 0.82f));

            // ⚠️⚠️ ONE CENTRED COLUMN OF CONTAINERS, NOT A SET OF HAND-PICKED OFFSETS, AND THE
            // PORT REPRODUCED THE VERSION THE ORIGINAL THREW AWAY. `RoleSwapCard.tscn`'s own
            // header records why it was rewritten: two panels anchored to the screen centre and
            // positioned by offsets knew nothing about the labels stacked above them, so a long
            // roster name grew a panel past the offset meant to stop it and drew over the line
            // above. 🧑, with a screenshot: *"shit wrapsaround and not proper"*.
            //
            // A layout group's children cannot overlap each other: each gets its own band and
            // the column grows downward from the middle, so a name of any length makes a TALLER
            // card and never a collision. That is the structural fix, and it is why nothing
            // below needs a hand-picked position any more.
            var column = BuildColumn(canvasGo.transform);

            // ⚠️ 34 AND 36, AND THE HEADLINE IS THE LARGER OF THE TWO. It is the news; the title
            // is only the bookmark. Both are the .tscn's own sizes, and 36 was chosen there
            // against the worst-case sentence — a 14-character name and a four-figure score —
            // so it fits the column's 860 floor without wrapping.
            _title = ColumnLabel(column, "TitleLabel", 34, UiTheme.CreamMuted, 46);
            _headline = ColumnLabel(column, "HeadlineLabel", 36, UiTheme.Defense, 50);

            var swapRows = BuildPanel(column.transform, "SwapPanel", 10.0f, out _swapPanel);

            // ⚠️ THE CAPTION IS A COLUMN TO THE LEFT OF THE VALUE, NOT A HEADER ABOVE IT, and
            // the caption column is the only fixed width on this card. Two captions whose text
            // never changes are what make the two value columns start at the same x.
            _tayaName = BuildSwapRow(swapRows, "NEXT TAYA", 32, UiTheme.Defense);
            _attackerNames = BuildSwapRow(swapRows, "ATTACKERS", 24, UiTheme.Offense);

            var standings = BuildPanel(column.transform, "StandingsPanel", 6.0f, out _standingsPanel);

            // ⚠️ THE ROW COUNT COMES FROM `PlayerCount`, so a card built for four seats is not a
            // card that assumes four seats.
            for (int i = 0; i < Core.Balance.PlayerCount; i++) _rows.Add(BuildStandingsRow(standings));

            _bufferPrompt = ColumnLabel(column, "BufferPrompt", 24, UiTheme.Amber, 52);

            _fight = MenuKit.Label(canvasGo.transform, "", 48, UiTheme.Amber,
                Centre, Vector2.zero, new Vector2(1200, 100));
            _fight.enabled = false;
        }

        /// <summary>The column floor. A MINIMUM, never a maximum: a genuinely long pairing makes
        /// the card wider rather than clipping, which is the rule the .tscn states outright.
        /// </summary>
        private const float ColumnWidth = 860.0f;

        /// <summary>The caption column. See <see cref="BuildSwapRow"/>.</summary>
        private const float CaptionWidth = 190.0f;

        private static VerticalLayoutGroup BuildColumn(Transform parent)
        {
            var go = new GameObject("Column", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Centre;
            rt.anchorMax = Centre;
            rt.pivot = Centre;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(ColumnWidth, 0.0f);

            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = 16.0f;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.childControlHeight = true;
            column.childControlWidth = true;

            // ⚠️ THE FITTER IS ON HEIGHT ONLY. The width is the floor above; letting the fitter
            // own both would collapse the column onto its longest line and undo the floor.
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return column;
        }

        private static Text ColumnLabel(VerticalLayoutGroup column, string name, int size,
                                        Color colour, float height)
        {
            var label = MenuKit.Label(column.transform, "", size, colour,
                Centre, Vector2.zero, new Vector2(ColumnWidth, height));

            label.gameObject.name = name;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ⚠️ A LAYOUT ELEMENT, BECAUSE A LAYOUT GROUP IGNORES sizeDelta. Without it every
            // row in this column collapses to the font's own line height and the card reads as
            // a stack of touching text.
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            return label;
        }

        /// <summary>One caption-and-value row of the swap panel.</summary>
        private static Text BuildSwapRow(VerticalLayoutGroup rows, string caption, int valueSize,
                                         Color valueColour)
        {
            var rowGo = new GameObject(caption, typeof(RectTransform));
            rowGo.transform.SetParent(rows.transform, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 22.0f;
            row.childForceExpandWidth = false;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childAlignment = TextAnchor.MiddleLeft;

            var captionLabel = MenuKit.Label(rowGo.transform, caption, 24, UiTheme.CreamMuted,
                Centre, Vector2.zero, new Vector2(CaptionWidth, 40.0f), TextAnchor.MiddleLeft);

            var captionElement = captionLabel.gameObject.AddComponent<LayoutElement>();
            captionElement.minWidth = CaptionWidth;
            captionElement.preferredWidth = CaptionWidth;
            captionElement.minHeight = 40.0f;

            var value = MenuKit.Label(rowGo.transform, "", valueSize, valueColour,
                Centre, Vector2.zero, new Vector2(400.0f, 40.0f), TextAnchor.MiddleLeft);

            value.horizontalOverflow = HorizontalWrapMode.Wrap;

            var valueElement = value.gameObject.AddComponent<LayoutElement>();
            valueElement.flexibleWidth = 1.0f;
            valueElement.minHeight = 40.0f;

            return value;
        }

        /// <summary>Rank, name, score. The rank and score columns are minimums so the name
        /// column starts and ends at the same x on every row.</summary>
        private static Text[] BuildStandingsRow(VerticalLayoutGroup standings)
        {
            var rowGo = new GameObject("Row", typeof(RectTransform));
            rowGo.transform.SetParent(standings.transform, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18.0f;
            row.childForceExpandWidth = false;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childAlignment = TextAnchor.MiddleLeft;

            var rank = Cell(rowGo.transform, "Rank", TextAnchor.MiddleLeft, 44.0f, 0.0f);
            var who = Cell(rowGo.transform, "Name", TextAnchor.MiddleLeft, 0.0f, 1.0f);
            var score = Cell(rowGo.transform, "Score", TextAnchor.MiddleRight, 120.0f, 0.0f);

            return new[] { rank, who, score };
        }

        private static Text Cell(Transform parent, string name, TextAnchor align,
                                 float width, float flexible)
        {
            var label = MenuKit.Label(parent, "", 26, UiTheme.Cream,
                Centre, Vector2.zero, new Vector2(Mathf.Max(width, 200.0f), 36.0f), align);

            label.gameObject.name = name;

            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 36.0f;
            element.flexibleWidth = flexible;

            if (width > 0.0f) { element.minWidth = width; element.preferredWidth = width; }

            return label;
        }

        /// <summary>
        /// A wood panel whose height comes from the rows inside it.
        ///
        /// ⚠️⚠️ THE LAYOUT GROUP AND THE IMAGE ARE ON THE SAME OBJECT, AND SPLITTING THEM IS
        /// WHY THE PANELS FIRST DREW AS BLACK SLIVERS WITH THE ROWS SPILLING OUT UNDER THEM. A
        /// `ContentSizeFitter` on a child that a parent `VerticalLayoutGroup` controls is a
        /// fight the parent wins: it writes the child's height from that child's own preferred
        /// size, and a panel whose only content was a stretched sub-object has no preferred
        /// height at all, so it got zero. With the group on the panel itself, the panel's
        /// preferred height IS the sum of its rows, which is exactly what the parent then asks
        /// it for. No fitter anywhere, and nothing to conflict.
        /// </summary>
        private static VerticalLayoutGroup BuildPanel(Transform parent, string name,
                                                      float spacing, out CanvasGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var rows = go.AddComponent<VerticalLayoutGroup>();
            rows.spacing = spacing;

            // The .tscn's own content margins on both panels.
            rows.padding = new RectOffset(22, 22, 14, 14);
            rows.childForceExpandHeight = false;
            rows.childForceExpandWidth = true;
            rows.childControlHeight = true;
            rows.childControlWidth = true;

            group = go.AddComponent<CanvasGroup>();
            return rows;
        }
    }
}
