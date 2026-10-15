using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ BLINDED, Nemu's DON'T LOOK (owner 2026-10-15, replacing KURO: SIT!). Piring, the ghost she leaves waiting, lunges
    /// at the eyes of whoever walks into it: their screen is covered for 2 s and they are slowed by a slow that starts at
    /// 50 % and wears off over 5 s (`NecroRules.DontLookSpeedScale`, League's "decaying slow").
    ///
    ///  * ONE CLOCK, five seconds. The eyes are covered while more than three of them remain (`NecroRules.DontLookCoversEyes`),
    ///    and the slow is a pure function of what is left, so the wire carries one float (`SyncUnit`, protocol 161).
    ///  * The HOST springs the ghost and writes the clock (`ApplyBlinded`); the body's own peer reads the replicated clock
    ///    for its speed (`StatusSpeedScale`) and its screen (`UI.TumpHudEffects`), like every timed status.
    ///  * Refreshed by `Max` (`CLAUDE.md` section 4); refused by status immunity; cleansable.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        private float _blindedLeft;

        public float BlindedLeft => _blindedLeft;
        public bool IsBlinded => _blindedLeft > 0.0f;

        /// <summary>True for the first 2 s, while the ghost's hands are over the eyes.</summary>
        public bool EyesCovered => NecroRules.DontLookCoversEyes(_blindedLeft);

        /// <summary>Seconds since the ghost sprang, or -1 when not Blinded.</summary>
        public float BlindedElapsed => IsBlinded ? StatusRules.BlindedSeconds - _blindedLeft : -1.0f;

        /// <summary>The decaying slow on this body: 0.5 at the spring, 1 once it has worn off.</summary>
        public float BlindedSpeedScale => NecroRules.DontLookSpeedScale(_blindedLeft);

        public void ApplyBlinded(float seconds = StatusRules.BlindedSeconds)
        {
            if (!MayMutateGameplayState() || StatusImmune || !float.IsFinite(seconds) || seconds <= 0.0f) return;
            bool fresh = _blindedLeft <= 0.0f;
            _blindedLeft = StatusRules.Refresh(_blindedLeft, Mathf.Min(seconds, StatusRules.BlindedSeconds));
            if (fresh) StatusGained?.Invoke(this, StatusKind.Blinded);
        }

        /// <summary>The host's replicated clock. Bounded and finite (the receiver has already checked).</summary>
        public void ApplyNetworkBlinded(float left)
        {
            bool fresh = _blindedLeft <= 0.0f && left > 0.0f;
            _blindedLeft = float.IsFinite(left) ? Mathf.Clamp(left, 0.0f, StatusRules.BlindedSeconds) : 0.0f;
            if (fresh) StatusGained?.Invoke(this, StatusKind.Blinded);
        }

        private void StepBlinded(float dt)
        {
            if (_blindedLeft > 0.0f) _blindedLeft = Mathf.Max(0.0f, _blindedLeft - dt);
        }

        private void ClearBlinded() => _blindedLeft = 0.0f;
    }
}
