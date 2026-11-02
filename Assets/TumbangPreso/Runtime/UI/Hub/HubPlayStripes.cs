using UnityEngine;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Runs PLAY's stripes (Shaders/PlayBarber), faster while the button is highlighted (owner, 2026-11-02).
    /// The clock is added up here because a shader's speed times its time would jump when the speed changes.
    /// </summary>
    public sealed class HubPlayStripes : MonoBehaviour
    {
        private const float IdleSpeed = 0.12f, HighlightSpeed = 0.36f, Ease = 6f;
        // 278 px a stripe and a 750 px yarn tile meet again every 375 stripes, so the wrap shows nothing.
        private const float Loop = 375f;
        private static readonly int Phase = Shader.PropertyToID("_PlayStripePhase");

        public HubButton Button;
        private float _speed = IdleSpeed, _phase;

        public static float SpeedFor(float lift) => Mathf.Lerp(IdleSpeed, HighlightSpeed, Mathf.Clamp01(lift));

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float lift = Button != null && Button.IsInteractable() ? Button.LiftTarget : 0f;
            _speed = Mathf.Lerp(_speed, SpeedFor(lift), 1f - Mathf.Exp(-Ease * dt));
            _phase = Mathf.Repeat(_phase + _speed * dt, Loop);
            Shader.SetGlobalFloat(Phase, _phase);
        }
    }
}
