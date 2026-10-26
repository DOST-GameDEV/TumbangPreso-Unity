using UnityEngine;
using UnityEngine.EventSystems;

namespace TumbangPreso.InputLayer
{
    // Screen-only holds never add gameplay verbs or change the wire format.
    public sealed class TouchHoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private TouchHud _owner;
        private string _action;
        private bool _captured;
        private int _pointer;
        private PointerEventData.InputButton _button;
        private uint _version;
        public void Bind(TouchHud owner, string action) { Release(); _owner = owner; _action = action; }
        public void OnPointerDown(PointerEventData data)
        {
            if (_version != TouchInput.ReleaseVersion) Release();
            if (!TouchInput.Active || _captured || _action == null || (_owner != null && TouchHud.Instance != _owner)) return;
            _captured = true; _pointer = data.pointerId; _button = data.button; _version = TouchInput.ReleaseVersion;
            TouchInput.SetScreenHold(this, _action, true);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (_captured && data.pointerId == _pointer && data.button == _button) Release();
        }
        private void Release() { TouchInput.SetScreenHold(this, _action, false); _captured = false; _version = TouchInput.ReleaseVersion; }
        private void Update()
        {
            if (_captured && (_version != TouchInput.ReleaseVersion || (_owner != null && TouchHud.Instance != _owner))) Release();
        }
        private void OnDisable() => Release();
    }
}
