using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The in-match HUD, built from code.
    ///
    /// ⚠️⚠️ IT ASKS THE RULES, IT DOES NOT MIRROR THEM. The VULNERABLE row comes from
    /// `IsTaggable`, which is the same call the tag itself makes, and the crosshair comes from
    /// `CanThrow`, which is the same call the throw makes. That is a rule and not a
    /// convenience: a HUD with its own opinion about legality will eventually promise safety
    /// the tag ignores, or grey out a throw the rules would have allowed, and the player sees
    /// no reason for either.
    ///
    /// ⚠️ BUILT IN CODE RATHER THAN AUTHORED, for the same reason the arena is. It can be
    /// diffed, it can be regenerated, and it cannot drift away from the constants it displays.
    /// The visual design is deliberately plain: the real look uses the team's own wood-panel
    /// chrome and their own font, and that is Phase 6 art rather than Phase 3 plumbing.
    ///
    /// ⚠️ LEGACY UI TEXT ON PURPOSE, NOT AN OVERSIGHT. TextMeshPro needs a font asset built
    /// from a real font file, and the game's font is not imported yet. Legacy Text with a
    /// built-in font has no asset dependency at all, which means the HUD works in a headless
    /// test and in a freshly cloned repo. Swap it when the real font lands.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] private CharacterMotor _local;

        private Canvas _canvas;
        private Text _timer;
        private Text _round;
        private Text _status;
        private Image _staminaFill;
        private Image _crosshair;

        private readonly List<StatusRow> _rows = new List<StatusRow>();

        private Text _countdown;
        private Text _readyPrompt;
        private OffscreenIndicators _indicators;

        /// <summary>
        /// The screen-edge arrows. Resolved here rather than inside the indicator, because
        /// the HUD already works out the local unit once a frame and a second scan would be
        /// a second answer to the same question.
        /// </summary>
        private void UpdateIndicators()
        {
            if (_indicators == null) return;

            var carrier = _local.GetComponent<Carrier>();

            // Your own slipper is the one that answers to your seat. `OwnerSlot` is what
            // makes "yours" well-defined at all.
            Transform mine = null;
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s.OwnerSlot == _local.PlayerSlot) { mine = s.transform; break; }

            var lata = GameServices.Round?.Lata;
            _indicators.UpdateArrows(_local, carrier, mine, lata != null ? lata.transform : null);
        }

        public void Bind(CharacterMotor local) => _local = local;

        /// <summary>
        /// "Press [R] when you're ready" — the pre-round free-roam prompt. Driven by
        /// <see cref="ReadyGate"/>; the HUD does not decide when the window is open.
        /// </summary>
        public void ShowReadyPrompt(bool show)
        {
            if (_readyPrompt != null) _readyPrompt.enabled = show;
        }

        /// <summary>One tick of the 3 · 2 · 1 · GO!, centred.</summary>
        public void ShowCountdownTick(string tick)
        {
            if (_countdown == null) return;

            _countdown.enabled = true;
            _countdown.text = tick;

            // GO! is the one that reads as a release rather than a count, so it takes the
            // highlight while the numbers stay cream.
            _countdown.color = tick == "GO!" ? UiTheme.Highlight : UiTheme.Cream;
        }

        public void HideCountdown()
        {
            if (_countdown != null) _countdown.enabled = false;
        }

        private void Awake() => Build();

        private void Update()
        {
            if (_local == null || GameServices.Match == null || GameServices.Round == null) return;

            UpdateTimer();
            UpdateScores();
            UpdateStamina();
            UpdateStatus();
            UpdateIndicators();

            _crosshair.enabled = StatusStack.ShowCrosshair(_local);
        }

        private void UpdateTimer()
        {
            float left = Mathf.Max(0.0f, GameServices.Round.TimeLeft);

            // The announcer's clock warnings ride the same value the clock draws, so "thirty
            // seconds" is spoken on the frame the HUD first shows 30. Each fires once per
            // round; the director owns that latch.
            GameServices.Voice?.TickClock(left);

            // ⚠️ MINUTES AND SECONDS, AND THE ROUND ON ITS OWN LINE. `HUD.tscn` draws "01:30"
            // in the 44px timer face and "ROUND 1 / 4 · TAYA: P1" underneath it in body text.
            // One run-on line of "ROUND 1/4 89.4s" is a debug readout, not a clock.
            _timer.text = $"{Mathf.FloorToInt(left / 60.0f):00}:{Mathf.FloorToInt(left % 60.0f):00}";

            if (_round != null)
            {
                _round.text = $"ROUND {GameServices.Match.RoundNumber} / {Balance.Rounds}" +
                              $"  ·  TAYA: P{GameServices.Match.DefenderSlot + 1}";
            }

            // ⚠️ THE CLOCK GOES AMBER UNDER PRESSURE RATHER THAN RED. Red means destructive or
            // out of bounds everywhere else in this palette, and a timer running out is
            // neither: it is the round ending normally, for everybody, on schedule.
            _timer.color = left <= 10.0f ? UiTheme.Highlight : UiTheme.Cream;
        }

        private void UpdateScores()
        {
            var m = GameServices.Match;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                if (_scoreNames[slot] == null) continue;

                bool isTaya = slot == m.DefenderSlot;

                _scoreNames[slot].text = NameFor(slot);

                // ⚠️ THE TAYA IS MARKED BY ROLE, NOT BY SEAT NUMBER. It rotates every round, so
                // a fixed per-seat colour would be telling the player the wrong thing for three
                // rounds out of four.
                _scoreNames[slot].color = isTaya ? UiTheme.Card : UiTheme.Offense;

                _scoreMarks[slot].text = isTaya ? "TAYA" : "";
                _scoreValues[slot].text = m.ScoreFor(slot).ToString();
            }
        }

        /// <summary>
        /// ⚠️ THE NAME, NOT "P3", WHEN THERE IS ONE. An empty name is legal and falls back to
        /// the seat label, which is the same contract the lobby board keeps: a player who never
        /// opened Settings still has a row that reads.
        /// </summary>
        private static string NameFor(int slot)
        {
            if (slot == GameLaunch.SoloSeat)
            {
                string chosen = Settings.SettingsStore.Current.PlayerName;
                if (!string.IsNullOrWhiteSpace(chosen)) return chosen.ToUpperInvariant();
            }

            var entry = Roster.At(Roster.People, slot);
            return entry != null ? entry.Name : $"P{slot + 1}";
        }

        private void UpdateStamina()
        {
            float ratio = StatusStack.StaminaRatio(_local);
            _staminaFill.fillAmount = ratio;

            // ⚠️ THE BAR IS A POINT POOL, NOT A TIMER, and it has to read as one. The pool is
            // dimensioned to roughly one crossing of the danger zone, so what the player needs
            // from it is "can I get back out from here", not "how many seconds of running".
            _staminaFill.color = _local.Stamina.IsFatigued
                ? UiTheme.Danger
                : (ratio < 0.25f ? UiTheme.Highlight : UiTheme.Cream);
        }

        private void UpdateStatus()
        {
            StatusStack.Collect(_local, _local.GetComponent<Carrier>(),
                                _local.GetComponent<CombatVerbs>(), _rows);

            var sb = new System.Text.StringBuilder();
            foreach (var row in _rows)
            {
                sb.Append(row.Label);

                // ⚠️ VULNERABLE PRINTS NO NUMBER AND THAT IS CORRECT. It lasts exactly as long
                // as the player chooses to stand in the box holding a slipper. Printing
                // "VULNERABLE 0.0s" would read as an effect that had already expired, at the
                // single most dangerous moment in the game.
                if (row.Timed && row.Remaining > 0.0f) sb.Append($"  {row.Remaining:0.0}s");
                sb.Append('\n');
            }

            _status.text = sb.ToString();
            _status.color = _local.IsTaggable() ? UiTheme.Impact : UiTheme.Cream;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// The arrangement is `HUD.tscn`'s, and so is the styling.
        ///
        /// ⚠️⚠️ THE PLAIN VERSION OF THIS WAS PHASE-3 PLUMBING AND IT SHIPPED. Built-in font,
        /// no cards, no outlines, everything one flat cream: the scoreboard was four lines of
        /// small text floating on the sky and the timer was a line of yellow above them. The
        /// real HUD is a translucent INK card top-left, a card around the clock, a card for the
        /// lata and the YOU card bottom-left, all in a face that carries a 6px ink outline
        /// because it is read at a glance, mid-sprint, over a live 3D scene.
        ///
        /// ⚠️ THE POSITIONS ARE THE .tscn's OFFSETS, NOT PICKED BY EYE. Scoreboard at 16,28
        /// from the top-left; the clock centred at 28 down; the lata card 16 in from the
        /// bottom-right. Those were arrived at against these arenas.
        /// </summary>
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

            BuildScoreboard(canvasGo.transform);
            BuildClock(canvasGo.transform);
            BuildStatusCard(canvasGo.transform);

            // Dead centre and large: the countdown is the one moment the HUD is allowed to take
            // the middle of the screen, because nothing is in play behind it yet.
            _countdown = HudText(canvasGo.transform, "Countdown", "HudBanner",
                                 new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 160),
                                 TextAnchor.MiddleCenter);

            _countdown.fontSize = 120;
            _countdown.enabled = false;

            _readyPrompt = HudText(canvasGo.transform, "ReadyPrompt", "HudBody",
                                   new Vector2(0.5f, 0.5f), new Vector2(0, -140),
                                   new Vector2(900, 60), TextAnchor.MiddleCenter);

            _readyPrompt.text = "Press [R] when you're ready";
            _readyPrompt.enabled = false;

            // Its own canvas, deliberately: the arrows are positioned from screen centre in
            // raw pixels, and putting them under the scaled HUD canvas would move them.
            var indicatorGo = new GameObject("OffscreenIndicators");
            indicatorGo.transform.SetParent(transform, false);
            _indicators = indicatorGo.AddComponent<OffscreenIndicators>();

            BuildCrosshair(canvasGo.transform);
        }

        /// <summary>
        /// Top-left: SCORES over one row per seat, on a translucent ink card.
        ///
        /// ⚠️ ONE ROW PER SEAT, NOT ONE BLOCK OF TEXT. `HUD.tscn` authors ScoreRow0..3, each a
        /// name on the left and a number hard against the right edge. A single label with spaces
        /// in it cannot right-align the numbers, so the scores wander with the length of the
        /// name above them and the column stops reading as a column.
        ///
        /// ⚠️ AND THE TAYA IS MARKED BY ROLE. It rotates every round, so colouring a seat
        /// permanently would tell the player the wrong thing for three rounds out of four.
        /// </summary>
        private void BuildScoreboard(Transform parent)
        {
            var card = Card(parent, "Scoreboard", new Vector2(0.0f, 1.0f),
                            new Vector2(16, -28), new Vector2(440, 0));

            var title = HudTextIn(card.transform, "ScoreTitle", "HudCaption", TextAnchor.MiddleLeft);
            title.text = "SCORES";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 40.0f;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var rowGo = new GameObject($"ScoreRow{slot}");
                rowGo.transform.SetParent(card.transform, false);

                var row = rowGo.AddComponent<HorizontalLayoutGroup>();
                row.childControlHeight = true;
                row.childControlWidth = true;
                row.childForceExpandHeight = false;
                row.childForceExpandWidth = false;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.spacing = 8.0f;

                rowGo.AddComponent<LayoutElement>().preferredHeight = 42.0f;

                var name = HudTextIn(rowGo.transform, "Name", "HudBody", TextAnchor.MiddleLeft);
                name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

                var mark = HudTextIn(rowGo.transform, "Taya", "HudCaption", TextAnchor.MiddleRight);
                mark.gameObject.AddComponent<LayoutElement>().preferredWidth = 96.0f;

                var score = HudTextIn(rowGo.transform, "Score", "HudScore", TextAnchor.MiddleRight);
                score.gameObject.AddComponent<LayoutElement>().preferredWidth = 56.0f;

                _scoreNames[slot] = name;
                _scoreMarks[slot] = mark;
                _scoreValues[slot] = score;
            }
        }

        private readonly Text[] _scoreNames = new Text[Balance.PlayerCount];
        private readonly Text[] _scoreMarks = new Text[Balance.PlayerCount];
        private readonly Text[] _scoreValues = new Text[Balance.PlayerCount];

        /// <summary>Top-centre: the clock on its own card, with the round line under it.</summary>
        private void BuildClock(Transform parent)
        {
            var column = new GameObject("TopCentre");
            column.transform.SetParent(parent, false);

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
            rt.sizeDelta = new Vector2(320, 0);

            var card = Card(column.transform, "TimerCard", Vector2.zero, Vector2.zero, Vector2.zero);
            card.gameObject.AddComponent<LayoutElement>().preferredWidth = 260.0f;

            _timer = HudTextIn(card.transform, "TimerLabel", "HudTimer", TextAnchor.MiddleCenter);

            _round = HudTextIn(column.transform, "RoundLabel", "HudBody", TextAnchor.MiddleCenter);
            _round.gameObject.AddComponent<LayoutElement>().minHeight = 46.0f;
        }

        /// <summary>
        /// The active-effects list and the stamina pool, above the YOU card.
        ///
        /// ⚠️⚠️ THE BOTTOM-LEFT CORNER BELONGS TO `YouCard`, WHICH `MatchInstaller` ALREADY
        /// BUILDS. Drawing a second card there put two overlapping panels in the corner, one
        /// wood and one ink, saying different things about the same player. This block sits
        /// ABOVE it and carries only what YouCard does not: the status stack and the stamina
        /// bar. Two components, one corner, no overlap.
        /// </summary>
        private void BuildStatusCard(Transform parent)
        {
            var card = Card(parent, "StatusCard", new Vector2(0.0f, 0.0f),
                            new Vector2(16, 214), new Vector2(460, 0));

            _status = HudTextIn(card.transform, "StatusLabel", "HudBody", TextAnchor.UpperLeft);
            _status.gameObject.AddComponent<LayoutElement>().minHeight = 96.0f;

            var back = new GameObject("StaminaBack");
            back.transform.SetParent(card.transform, false);

            var backImg = back.AddComponent<Image>();
            backImg.sprite = GodotTheme.Box(UiTheme.WoodDark, UiTheme.WoodEdge, 3, 6);
            backImg.type = Image.Type.Sliced;

            back.AddComponent<LayoutElement>().preferredHeight = 26.0f;

            var fill = new GameObject("StaminaFill");
            fill.transform.SetParent(back.transform, false);

            _staminaFill = fill.AddComponent<Image>();
            _staminaFill.sprite = GodotTheme.Box(Color.white, new Color(0, 0, 0, 0), 0, 4);
            _staminaFill.type = Image.Type.Filled;
            _staminaFill.fillMethod = Image.FillMethod.Horizontal;

            var frt = _staminaFill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3, 3);
            frt.offsetMax = new Vector2(-3, -3);
        }

        /// <summary>
        /// A `HudCard`: the translucent INK slab the HUD's blocks sit on.
        ///
        /// ⚠️ TRANSLUCENT INK, NOT WOOD. The menus are wood over a photograph; the HUD is over
        /// a live arena that has to stay readable through it. Same theme, different variation,
        /// and swapping one for the other is how a HUD ends up hiding the game.
        /// </summary>
        private static VerticalLayoutGroup Card(Transform parent, string name, Vector2 anchor,
                                                Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();

            var group = go.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.spacing = 2.0f;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = "HudCard";
            skin.Apply();

            var rt = go.GetComponent<RectTransform>();

            if (size != Vector2.zero || anchor != Vector2.zero || offset != Vector2.zero)
            {
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = anchor;
                rt.anchoredPosition = offset;
                rt.sizeDelta = new Vector2(size.x, 0.0f);
            }

            return group;
        }

        private static Text HudText(Transform parent, string name, string variation,
                                    Vector2 anchor, Vector2 offset, Vector2 size,
                                    TextAnchor align)
        {
            var t = HudTextIn(parent, name, variation, align);

            var rt = t.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;

            return t;
        }

        /// <summary>A HUD label in one of the theme's Hud* variations, outline included.</summary>
        private static Text HudTextIn(Transform parent, string name, string variation,
                                      TextAnchor align)
        {
            GodotTheme.TryText(variation, out var style);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<Text>();
            t.font = MenuKit.Font;
            t.fontSize = style.Size;
            t.color = style.Colour;
            t.alignment = align;
            t.alignByGeometry = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            if (style.Outline > 0)
            {
                var ring = go.AddComponent<GodotOutline>();
                ring.OutlineColour = style.OutlineColour;
                ring.Radius = Mathf.Max(1.0f, style.Outline * 0.5f);
            }

            return t;
        }

        private void BuildCrosshair(Transform parent)
        {
            var go = new GameObject("Crosshair");
            go.transform.SetParent(parent, false);

            _crosshair = go.AddComponent<Image>();
            _crosshair.color = UiTheme.Offense;

            var rt = _crosshair.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(10, 10);
        }
    }
}
