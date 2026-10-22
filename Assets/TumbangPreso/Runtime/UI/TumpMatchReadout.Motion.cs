using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// ⚠️ THE MATCH BAR MOVES WHEN THE MATCH DOES (owner, 2026-10-22: "everything has to be animated").
    ///   * SCORES ROLL: a new total rolls up into the chip while the old one rolls out above it, like an odometer.
    ///     The label always holds the real `ScoreFor` (see `ScoreFeedback`: never animated digits as points); only
    ///     its position and the outgoing ghost move.
    ///   * THE CROWN SLIDES: when the lead changes, the crown lifts off the old leader's chip and drops onto the new
    ///     one with a bounce, instead of blinking between them. Chips keep seat order (VISUAL-1.4).
    ///   * POINTS FLY HOME: a tag or a knockdown sends its +points from where it happened, across the screen, into
    ///     the scorer's chip, where the existing pop and glow land it.
    /// Reduced UI motion keeps every value and crown in place and skips the flight.
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private const float RollSeconds = .32f, RollTravel = 22f, CrownSeconds = .42f, FlightSeconds = .6f;
        private readonly Text[] _scoreGhosts = new Text[4];
        private readonly Vector2[] _scoreHome = new Vector2[4];
        private readonly float[] _rollAt = { -100, -100, -100, -100 };
        private readonly bool[] _crownShown = new bool[4];
        private readonly float[] _crownAt = { -100, -100, -100, -100 };
        private readonly Vector2[] _crownHome = new Vector2[4];

        private sealed class Flight { public Text Label; public Vector2 From; public int Slot; public float Born; public bool Live; }
        private readonly System.Collections.Generic.List<Flight> _flights = new System.Collections.Generic.List<Flight>();
        private bool _flightsBound;

        private void BuildScoreGhost(int i, RectTransform row)
        {
            _scoreHome[i] = _scores[i].rectTransform.anchoredPosition;
            var ghost = Object.Instantiate(_scores[i].gameObject, row).GetComponent<Text>();
            ghost.name = "ScoreRollOut"; ghost.text = ""; ghost.raycastTarget = false;
            _scoreGhosts[i] = ghost;
            _crownHome[i] = _crowns[i].rectTransform.anchoredPosition;
        }

        /// <summary>The chip's total changed from <paramref name="previous"/>: roll it.</summary>
        private void StartScoreRoll(int i, string previous)
        {
            if (_scoreGhosts[i] == null || Settings.SettingsStore.Current.ReducedUiMotion) return;
            _scoreGhosts[i].text = previous; _scoreGhosts[i].fontSize = _scores[i].fontSize;
            _rollAt[i] = Time.unscaledTime;
        }

        private void PaintCrown(int i, bool leader)
        {
            if (leader != _crownShown[i]) { _crownShown[i] = leader; _crownAt[i] = Time.unscaledTime; }
            // A crown leaving stays drawn while it lifts away.
            bool leaving = !leader && Time.unscaledTime - _crownAt[i] < CrownSeconds && !Settings.SettingsStore.Current.ReducedUiMotion;
            _crowns[i].Show(leader || leaving ? HudBadge.Glyph.Crown : HudBadge.Glyph.None, HudDraw.Honey, Color.clear);
        }

        private void PaintMotion()
        {
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            float now = Time.unscaledTime;
            for (int i = 0; i < 4; i++)
            {
                if (_scores[i] == null || _scoreGhosts[i] == null) continue;
                float u = still ? 1 : Mathf.Clamp01((now - _rollAt[i]) / RollSeconds);
                float e = 1 - (1 - u) * (1 - u) * (1 - u);
                // `anchoredPosition` y grows upward: the new total rises from below, the old one leaves above.
                _scores[i].rectTransform.anchoredPosition = _scoreHome[i] + new Vector2(0, -RollTravel * (1 - e));
                bool rolling = u < 1;
                _scoreGhosts[i].enabled = rolling;
                if (rolling)
                {
                    _scoreGhosts[i].rectTransform.anchoredPosition = _scoreHome[i] + new Vector2(0, RollTravel * e);
                    var g = _scores[i].color; g.a = 1 - e; _scoreGhosts[i].color = g;
                }

                if (_crowns[i] == null) continue;
                float k = still ? 1 : Mathf.Clamp01((now - _crownAt[i]) / CrownSeconds);
                Vector2 home = _crownHome[i];
                if (_crownShown[i])
                {
                    // Drops in from above, lands, bounces once.
                    float drop = k < .6f ? 1 - (k / .6f) * (k / .6f) : 0;
                    float bounce = k >= .6f ? Mathf.Sin((k - .6f) / .4f * Mathf.PI) * .25f : 0;
                    _crowns[i].rectTransform.anchoredPosition = home + new Vector2(0, 46 * drop + 10 * bounce);
                    _crowns[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - k) * 25);
                    _crowns[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(.6f, 1, Mathf.Clamp01(k * 3));
                }
                else
                {
                    _crowns[i].rectTransform.anchoredPosition = home + new Vector2(0, 40 * k * k);
                    _crowns[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -20 * k);
                    _crowns[i].rectTransform.localScale = Vector3.one * (1 - k);
                }
            }
            PaintFlights(now, still);
            PaintSpectatorCards();
        }

        private void BindFlights(bool on)
        {
            if (on == _flightsBound) return;
            _flightsBound = on;
            if (on) Visual.MatchFlair.Presented += OnFlightMoment; else Visual.MatchFlair.Presented -= OnFlightMoment;
            if (!on) foreach (var f in _flights) { f.Live = false; if (f.Label != null) f.Label.enabled = false; }
        }

        private void OnFlightMoment(Visual.MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (this == null) { Visual.MatchFlair.Presented -= OnFlightMoment; return; }
            if (Settings.SettingsStore.Current.ReducedUiMotion || actor < 0 || actor >= 4 || _scoreRows[actor] == null) return;
            if (GameServices.Round == null || !GameServices.Round.RoundActive || Canvas == null || !_scoreRoot.gameObject.activeInHierarchy) return;
            int points = kind == Visual.MatchFlair.Kind.Tag ? MatchRules.PointsFor(ScoreEvent.Tag)
                : kind == Visual.MatchFlair.Kind.LataDown ? MatchRules.PointsFor(ScoreEvent.LataKnocked) : 0;
            if (points <= 0) return;
            var camera = Camera.main;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(at + Vector3.up * .8f);
            if (screen.z <= .1f) return;
            var root = (RectTransform)Canvas.transform;
            var eye = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, eye, out var from)) return;
            Flight flight = null;
            foreach (var f in _flights) if (!f.Live) { flight = f; break; }
            if (flight == null)
            {
                flight = new Flight();
                flight.Label = OwnerUiLayout.Text(root, "PointsInFlight", "", 44, OwnerUiLayout.TypeRole.Display);
                flight.Label.alignment = TextAnchor.MiddleCenter; flight.Label.raycastTarget = false; flight.Label.fontStyle = FontStyle.Italic;
                flight.Label.horizontalOverflow = HorizontalWrapMode.Overflow; flight.Label.verticalOverflow = VerticalWrapMode.Overflow;
                var edge = flight.Label.gameObject.AddComponent<Outline>(); edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(2, -2);
                var rect = flight.Label.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(200, 60);
                _flights.Add(flight);
            }
            flight.Label.text = "+" + points; flight.Label.color = PlayerIdentity.Colour(actor);
            flight.From = from; flight.Slot = actor; flight.Born = Time.unscaledTime; flight.Live = true; flight.Label.enabled = true;
            flight.Label.rectTransform.SetAsLastSibling();
        }

        private void PaintFlights(float now, bool still)
        {
            if (_flights.Count == 0) return;
            var root = Canvas != null ? (RectTransform)Canvas.transform : null;
            foreach (var f in _flights)
            {
                if (!f.Live) continue;
                float u = (now - f.Born) / FlightSeconds;
                if (still || u >= 1 || root == null || _scoreRows[f.Slot] == null) { f.Live = false; f.Label.enabled = false; continue; }
                // The chip's centre, in the same space as the start point.
                Vector2 to = root.InverseTransformPoint(_scoreRows[f.Slot].TransformPoint(_scoreRows[f.Slot].rect.center));
                // A beat to be seen where it happened (it swells), then an arc home that speeds up as it goes.
                float hang = Mathf.Clamp01(u / .25f), travel = Mathf.Clamp01((u - .2f) / .8f);
                float t = travel * travel;
                Vector2 lift = new Vector2(0, 120);
                Vector2 a = f.From, b = Vector2.Lerp(f.From, to, .4f) + lift;
                Vector2 p = Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, to, t), t);
                f.Label.rectTransform.anchoredPosition = p + new Vector2(0, 26 * hang * (1 - travel));
                float scale = Mathf.Lerp(.4f, 1.25f, 1 - (1 - hang) * (1 - hang)) * Mathf.Lerp(1, .55f, t);
                f.Label.rectTransform.localScale = Vector3.one * scale;
                var c = f.Label.color; c.a = travel > .9f ? (1 - travel) / .1f : 1; f.Label.color = c;
            }
        }
    }
}
