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
        private Text _scores;
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
            _timer.text = $"ROUND {GameServices.Match.RoundNumber}/{Balance.Rounds}   {left:0.0}s";

            // ⚠️ THE CLOCK GOES AMBER UNDER PRESSURE RATHER THAN RED. Red means destructive or
            // out of bounds everywhere else in this palette, and a timer running out is
            // neither: it is the round ending normally, for everybody, on schedule.
            _timer.color = left <= 10.0f ? UiTheme.Highlight : UiTheme.Cream;
        }

        private void UpdateScores()
        {
            var m = GameServices.Match;
            var sb = new System.Text.StringBuilder();

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                bool isTaya = slot == m.DefenderSlot;

                // ⚠️ THE TAYA IS MARKED BY ROLE, NOT BY SEAT NUMBER. It rotates every round, so
                // a fixed per-seat colour would be telling the player the wrong thing for three
                // rounds out of four.
                sb.Append(isTaya ? "[TAYA] P" : "       P");
                sb.Append(slot + 1);
                sb.Append("  ");
                sb.Append(m.ScoreFor(slot));
                sb.Append('\n');
            }

            _scores.text = sb.ToString();
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

        private void Build()
        {
            var canvasGo = new GameObject("HudCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _timer = MakeText(canvasGo.transform, "Timer", new Vector2(0.5f, 1.0f),
                              new Vector2(0, -40), 34, TextAnchor.UpperCenter);

            _scores = MakeText(canvasGo.transform, "Scores", new Vector2(0.0f, 1.0f),
                               new Vector2(200, -40), 26, TextAnchor.UpperLeft);

            _status = MakeText(canvasGo.transform, "Status", new Vector2(0.0f, 0.0f),
                               new Vector2(200, 190), 26, TextAnchor.LowerLeft);

            // Dead centre and large: the countdown is the one moment the HUD is allowed to
            // take the middle of the screen, because nothing is in play behind it yet.
            _countdown = MakeText(canvasGo.transform, "Countdown", new Vector2(0.5f, 0.5f),
                                  Vector2.zero, 120, TextAnchor.MiddleCenter);
            _countdown.enabled = false;

            _readyPrompt = MakeText(canvasGo.transform, "ReadyPrompt", new Vector2(0.5f, 0.5f),
                                    new Vector2(0, -140), 32, TextAnchor.MiddleCenter);
            _readyPrompt.text = "Press [R] when you're ready";
            _readyPrompt.enabled = false;

            // Its own canvas, deliberately: the arrows are positioned from screen centre in
            // raw pixels, and putting them under the scaled HUD canvas would move them.
            var indicatorGo = new GameObject("OffscreenIndicators");
            indicatorGo.transform.SetParent(transform, false);
            _indicators = indicatorGo.AddComponent<OffscreenIndicators>();

            BuildStaminaBar(canvasGo.transform);
            BuildCrosshair(canvasGo.transform);
        }

        private static Text MakeText(Transform parent, string name, Vector2 anchor,
                                     Vector2 offset, int size, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = UiTheme.Cream;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var rt = t.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(600, 220);

            return t;
        }

        private void BuildStaminaBar(Transform parent)
        {
            var back = new GameObject("StaminaBack");
            back.transform.SetParent(parent, false);

            var backImg = back.AddComponent<Image>();
            backImg.color = UiTheme.WoodDark;

            var brt = backImg.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 0.0f);
            brt.anchorMax = new Vector2(0.5f, 0.0f);
            brt.pivot = new Vector2(0.5f, 0.0f);
            brt.anchoredPosition = new Vector2(0, 60);
            brt.sizeDelta = new Vector2(420, 26);

            var fill = new GameObject("StaminaFill");
            fill.transform.SetParent(back.transform, false);

            _staminaFill = fill.AddComponent<Image>();
            _staminaFill.color = UiTheme.Cream;
            _staminaFill.type = Image.Type.Filled;
            _staminaFill.fillMethod = Image.FillMethod.Horizontal;

            var frt = _staminaFill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3, 3);
            frt.offsetMax = new Vector2(-3, -3);
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
