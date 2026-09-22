using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed partial class TumpMatchReadout
    {
        // The accepted score event provides emphasis; ScoreFor remains the only
        // displayed number. Never count animated digits as points or infer a bonus.
        private readonly float[] _scoreMomentUntil = new float[4];
        private readonly int[] _scoreRowSeats = { -1, -1, -1, -1 };
        private readonly OwnerScoreStrip[] _scoreAccents = new OwnerScoreStrip[4];
        private MatchDirector _scoreEvents;
        private int _scoreMomentRound;
        private const float ScoreMomentLife = .62f;

        private void OnEnable() => BindScoreEvents();
        private void OnDisable()
        {
            if (_scoreEvents != null) _scoreEvents.Scored -= OnScoreMoment;
            _scoreEvents = null; ClearScoreMoments();
        }
        private void BindScoreEvents()
        {
            var match = GameServices.Match;
            if (_scoreEvents != match)
            {
                if (_scoreEvents != null) _scoreEvents.Scored -= OnScoreMoment;
                _scoreEvents = match; ClearScoreMoments();
                if (_scoreEvents != null) _scoreEvents.Scored += OnScoreMoment;
            }
            int round = match != null ? match.RoundNumber : 0;
            if (round != _scoreMomentRound) { _scoreMomentRound = round; ClearScoreMoments(); }
        }
        private void OnScoreMoment(int slot, ScoreEvent kind)
        {
            BindScoreEvents();
            if (slot < 0 || slot >= 4 || Canvas == null || !Canvas.gameObject.activeInHierarchy
                || GameServices.Round == null || !GameServices.Round.RoundActive
                || (kind != ScoreEvent.LataKnocked && kind != ScoreEvent.Tag)) return;
            _scoreMomentUntil[slot] = Time.unscaledTime + ScoreMomentLife;
            _scoreAt = 0; // Refresh rank/total before the accented row is drawn.
        }
        private void ClearScoreMoments()
        {
            Array.Clear(_scoreMomentUntil, 0, _scoreMomentUntil.Length);
            for (int i = 0; i < 4; i++)
            {
                if (_scores[i] != null) _scores[i].rectTransform.localScale = Vector3.one;
                if (_scoreAccents[i] != null) _scoreAccents[i].SetMoment(0, Color.clear);
                if (_chipCards[i] != null) _chipCards[i].SetMoment(0, Color.clear);
            }
        }
        private void PaintScoreMoments()
        {
            BindScoreEvents();
            bool visible = Canvas != null && Canvas.gameObject.activeInHierarchy
                && GameServices.Round != null && GameServices.Round.RoundActive;
            if (!visible) { ClearScoreMoments(); return; }
            var settings = Settings.SettingsStore.Current;
            for (int i = 0; i < 4; i++)
            {
                int slot = _scoreRowSeats[i];
                float remaining = slot >= 0 ? Mathf.Max(0, _scoreMomentUntil[slot] - Time.unscaledTime) : 0;
                float u = remaining > 0 ? 1 - remaining / ScoreMomentLife : 1;
                float accent = remaining > 0 ? (1 - u) * settings.EffectiveFlashIntensity : 0;
                if (_scores[i] != null)
                {
                    float bump = !settings.ReducedUiMotion && remaining > 0
                        ? .07f * Mathf.Sin(Mathf.Clamp01(u / .45f) * Mathf.PI) : 0;
                    _scores[i].rectTransform.localScale = Vector3.one * (1 + bump);
                }
                if (_scoreAccents[i] != null)
                    _scoreAccents[i].SetMoment(accent, slot >= 0 ? PlayerIdentity.Colour(slot) : Color.clear);
                // VISUAL-1.4: the chip glows in the scorer's seat colour.
                if (_chipCards[i] != null)
                    _chipCards[i].SetMoment(accent, slot >= 0 ? PlayerIdentity.Colour(slot) : Color.clear);
            }
        }
    }
}
