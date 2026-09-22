using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The HUD steps back while the spectacle peaks (TODO VISUAL-1.5).
    ///
    /// ⚠️⚠️ THE CAN AND THE SLIPPERS KEEP THE COLOUR PEAK. Sepak U hides its HUD during a super
    /// so the ball stays the brightest thing on screen; the findings file records it. During an
    /// accepted ultimate (`UltimatePresentationDirector.Playing`) and a replay
    /// (`SpectatorCamera.Replaying`) the chips, the feed and the power deck fade to 30 percent
    /// over 0.25 s and come back over 0.4 s. The clock, the reticle, the prompt and the danger
    /// frame are never faded: they answer "what do I do now", which an ultimate does not change.
    ///
    /// ⚠️ 30 PERCENT, NOT ZERO. A score or a cooldown that vanished mid-ultimate would read as a
    /// reset; a dimmed one reads as "still there, not now".
    /// </summary>
    public sealed partial class TumpMatchReadout
    {
        private CanvasGroup[] _recede;
        private float _recedeAlpha = 1, _recedeLookupAt;
        private UltimatePresentationDirector _ultimate;
        public const float RecededAlpha = .3f;
        /// <summary>For tests: the current alpha of the receding groups.</summary>
        public float RecedeAlpha => _recedeAlpha;

        private void PaintRecede()
        {
            if (_root == null) return;
            if (_recede == null)
            {
                var names = new[] { "MatchScores", "MatchEventFeed", "PowerSeals" };
                _recede = new CanvasGroup[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    var target = _root.Find(names[i]); if (target == null) continue;
                    if (!target.TryGetComponent<CanvasGroup>(out var group)) group = target.gameObject.AddComponent<CanvasGroup>();
                    group.blocksRaycasts = false; group.interactable = false; _recede[i] = group;
                }
            }
            // ⚠️ LOOKED UP ONCE A SECOND, NOT EVERY FRAME, AND NEVER THROUGH `Instance`, which
            // would CREATE the director. `CLAUDE.md` § 7.1: a HUD cost paid every frame once
            // took an eighth of the 6x probe's frames.
            if (Time.unscaledTime >= _recedeLookupAt)
            {
                _recedeLookupAt = Time.unscaledTime + 1;
                if (_ultimate == null) _ultimate = FindFirstObjectByType<UltimatePresentationDirector>();
                if (_spectatorCamera == null) _spectatorCamera = FindFirstObjectByType<CameraSystem.SpectatorCamera>();
            }
            var ultimate = _ultimate;
            bool peak = (ultimate != null && ultimate.Playing) || (_spectatorCamera != null && _spectatorCamera.Replaying);
            float dt = Time.unscaledDeltaTime;
            _recedeAlpha = Mathf.MoveTowards(_recedeAlpha, peak ? RecededAlpha : 1, dt / (peak ? .25f : .4f) * (1 - RecededAlpha));
            foreach (var group in _recede) if (group != null) group.alpha = _recedeAlpha;
        }
    }
}
