namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        private uint _visualActionSequence, _observedActionSequence;
        private string _observedActionName;
        private bool _usesObservedMotion;
        public string ObservedActionName => _clip != null ? _actionName ?? "" : "";
        public string ObservedAimName => _aimPreview ?? "";
        public float ObservedActionTime => _clip != null ? _clipTime : 0;
        public float ObservedVisualPhase => _phase;
        public uint ObservedActionSequence => _visualActionSequence;

        public void ApplyObservedMotion(string action, float elapsed, uint serial, string aim, float phase, float step, bool reset)
        {
            if (reset || serial != _observedActionSequence || action != _observedActionName)
            {
                if (string.IsNullOrEmpty(action)) CancelAction(); else PlayAction(action);
                _observedActionSequence = serial; _observedActionName = action;
            }
            if (!string.IsNullOrEmpty(action) && _clip != null && _actionName == action)
            {
                // StepAction advances once below. A newly captured clip can be
                // younger than this frame's delta, so do not clamp it ahead.
                _clipTime = elapsed - step;
                _clipBlendTime = UnityEngine.Mathf.Max(0, elapsed - step);
            }
            SetAimPreview(string.IsNullOrEmpty(aim) ? null : aim);
            _phase = phase - step;
            _usesObservedMotion = true;
        }
        public void StepObservedPresentation(float dt)
        { StepVisuals(dt); StepWorldShade(dt); }
    }
}
