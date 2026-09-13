using System;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Settings return adapter for the owner's approved controller diagram.
    /// Its original art, callout buttons, leader lines and builder are explicitly preserved.
    /// The enclosing settings session remains responsible for saving or discarding bindings.
    /// </summary>
    public sealed class TumpControllerView : MonoBehaviour
    {
        private ControllerMapScreen _screen;
        private Action _back;
        private bool _waiting;
        public bool IsOpen => _screen != null && _screen.IsOpen;

        public void Open(Transform owner, TumpSettingsSession session, Action back)
        {
            _back = back;
            // The original standalone screen loads saved bindings on entry. Keep any draft
            // reset/rebind in this settings transaction without changing that screen's behavior.
            string draft = session.Actions.SaveBindingOverridesAsJson();
            _screen = ControllerMapScreen.Open();
            session.Actions.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(draft)) session.Actions.LoadBindingOverridesFromJson(draft);
            Rebinding.Invalidate();
            _waiting = true;
        }

        private void Update()
        {
            if (!_waiting || IsOpen) return;
            _waiting = false; _screen = null;
            _back?.Invoke();
        }

        private void OnDisable()
        {
            _waiting = false;
            if (IsOpen) _screen.Close();
            _screen = null;
        }
    }
}
