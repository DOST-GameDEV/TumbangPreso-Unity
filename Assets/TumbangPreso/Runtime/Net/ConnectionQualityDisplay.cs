namespace TumbangPreso.Net
{
    // Presentation hysteresis over measured transport quality; never changes simulation.
    public sealed class ConnectionQualityDisplay
    {
        public enum Tier { Healthy, Mild, Severe }
        public Tier Current { get; private set; }
        private Tier _pending;
        private float _pendingSeconds;
        public Tier Advance(bool connected, NetSession.LinkState quality, float unscaledSeconds)
        {
            if (!connected) { Current = _pending = Tier.Healthy; _pendingSeconds = 0; return Current; }
            var target = quality == NetSession.LinkState.Bad ? Tier.Severe
                : quality == NetSession.LinkState.Poor ? Tier.Mild : Tier.Healthy;
            if (target == Current) { _pending = target; _pendingSeconds = 0; return Current; }
            if (_pending != target) { _pending = target; _pendingSeconds = 0; }
            if (float.IsFinite(unscaledSeconds) && unscaledSeconds > 0) _pendingSeconds += unscaledSeconds;
            float settle = target > Current ? (target == Tier.Severe ? .35f : 1.5f) : 3f;
            if (_pendingSeconds >= settle) { Current = target; _pendingSeconds = 0; }
            return Current;
        }
    }
}
