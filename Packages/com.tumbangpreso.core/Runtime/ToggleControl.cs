namespace TumbangPreso.Core
{
    /// <summary>Turns physical hold state into an optional latch, without inventing repeated presses.</summary>
    public sealed class ToggleControl
    {
        private bool _down, _latched, _blocked, _knownMode, _toggle;

        public bool Read(bool down, bool toggle, bool allowed = true)
        {
            if (_knownMode && _toggle != toggle) Reset();
            _knownMode = true; _toggle = toggle;
            if (!allowed)
            {
                _latched = false; _blocked = down; _down = down;
                return false;
            }
            if (_blocked)
            {
                if (!down) _blocked = false;
                _down = down;
                return false;
            }
            bool pressed = down && !_down;
            _down = down;
            if (!toggle) { _latched = false; return down; }
            if (pressed) _latched = !_latched;
            return _latched;
        }

        // A menu/role/round boundary cannot reactivate an old physically held key.
        public void Reset() { _latched = false; _blocked = true; }
    }
}
