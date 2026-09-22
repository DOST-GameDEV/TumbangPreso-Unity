using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The transient layer of the match HUD: the toast, the hit mark and the score pops
    /// (TODO VISUAL-1.4).
    ///
    /// ⚠️⚠️ A POINT IS SHOWN WHERE IT GOES, NOT IN THE MIDDLE OF THE SCREEN. A score used to be a
    /// centre toast ("+50  SABOTAGE") or nothing at all, because the side feed took ordinary
    /// contact. Now every award that is an EVENT (knockdown, tag, sabotage, the last-tsinelas
    /// award) rises as "+N" from under the scorer's chip into it, so the player sees who scored
    /// and where the total lives in one movement, with no word to read.
    ///
    /// ⚠️ THE TOTAL IS NEVER ANIMATED. `MatchDirector.ScoreFor` is still the only number in the
    /// chip and it is already final when the pop starts. The pop is a label for the change, not
    /// a counter that could disagree with the host.
    ///
    /// ⚠️ TICKS NEVER POP. Passive defence and the two tournament penalties fire once a second
    /// for as long as they last; `Hud.OnScored` already records why a message on every tick is
    /// "a message that never leaves the screen and says nothing while it is there".
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private CourtPopupGraphic _toastPlate;
        private HudBadge _hitMark;
        private const float HitLife = .28f, PopLife = .85f;
        private readonly Text[] _scorePops = new Text[4];
        private readonly float[] _popStarted = { -10, -10, -10, -10 };

        private void BuildScorePops()
        {
            for (int i = 0; i < 4; i++)
            {
                var pop = Ink(_scoreRoot, "ScorePop" + i, "", 30, true);
                OwnerUiLayout.Place(pop.rectTransform, ChipX[i], ChipHeight, ChipWidth, 40);
                pop.enabled = false; pop.raycastTarget = false; _scorePops[i] = pop;
            }
        }

        internal static bool PopsFor(ScoreEvent kind) =>
            kind == ScoreEvent.LataKnocked || kind == ScoreEvent.Tag
            || kind == ScoreEvent.Sabotage || kind == ScoreEvent.LastTsinelasStanding;

        private void StartScorePop(int slot, ScoreEvent kind, int localSlot)
        {
            if (slot < 0 || slot >= 4 || _scorePops[slot] == null || !PopsFor(kind)) return;
            int points = MatchRules.PointsFor(kind);
            if (points == 0) return;
            var pop = _scorePops[slot];
            pop.text = (points > 0 ? "+" : "") + points;
            pop.color = slot == localSlot ? CourtPresentationPalette.Gold : CourtPresentationPalette.Paper;
            _popStarted[slot] = Time.unscaledTime;
            pop.enabled = true;
        }

        /// <summary>Review captures only: raises the pop a real award would, without scoring.</summary>
        public void ScorePopForShot(int slot, ScoreEvent kind) => StartScorePop(slot, kind, _aimOwner != null ? _aimOwner.PlayerSlot : -1);

        /// <summary>For tests: the words the pop above a seat's chip is showing, or empty.</summary>
        public string ScorePopText(int slot) =>
            slot >= 0 && slot < 4 && _scorePops[slot] != null && _scorePops[slot].enabled ? _scorePops[slot].text : "";

        private void PaintScorePops()
        {
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            for (int i = 0; i < 4; i++)
            {
                var pop = _scorePops[i]; if (pop == null || !pop.enabled) continue;
                float u = (Time.unscaledTime - _popStarted[i]) / PopLife;
                if (u >= 1 || !_scoreRoot.gameObject.activeInHierarchy) { pop.enabled = false; continue; }
                // Rises from under the chip into its score, easing out, and fades in the last
                // third. Reduced UI motion keeps it still under the chip and only fades it.
                float rise = still ? 0 : 1 - (1 - u) * (1 - u);
                float y = Mathf.Lerp(ChipHeight + 6, ChipHeight * .35f, rise);
                OwnerUiLayout.Place(pop.rectTransform, ChipX[i], y, ChipWidth, 40);
                var c = pop.color; c.a = u < .65f ? 1 : Mathf.Clamp01((1 - u) / .35f); pop.color = c;
            }
        }

        private void PaintHitMark()
        {
            if (_hitMark == null || !_hitMark.enabled) return;
            float u = 1 - Mathf.Clamp01(_hitLeft / HitLife);
            float scale = Settings.SettingsStore.Current.ReducedUiMotion ? 1 : Mathf.Lerp(1.35f, 1, Mathf.Clamp01(u / .35f));
            _hitMark.rectTransform.localScale = Vector3.one * scale;
            var c = _hitMark.color; c.a = u < .6f ? 1 : Mathf.Clamp01((1 - u) / .4f); _hitMark.color = c;
        }

        /// <summary>Fits the brush plate to the toast's words; hidden with them.</summary>
        private void SizeToastPlate()
        {
            if (_toastPlate == null || _toast == null) return;
            bool show = _toast.enabled && !string.IsNullOrEmpty(_toast.text);
            _toastPlate.enabled = show;
            if (!show) return;
            float width = Mathf.Min(1080, _toast.preferredWidth + 90);
            _toastPlate.rectTransform.sizeDelta = new Vector2(width, 64);
        }
    }
}
