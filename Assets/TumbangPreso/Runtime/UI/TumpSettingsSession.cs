using System;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.UI
{
    /// <summary>Nonvisual transaction for native settings. Mapping behavior stays in Rebinding.</summary>
    public sealed class TumpSettingsSession : IDisposable
    {
        private string _settings, _bindings, _beforeRebind;
        private RebindSession _rebind;
        private bool _genericSupport;
        public InputActionAsset Actions { get; }
        public bool Listening => _rebind != null;
        public event Action<string> Changed;
        public TumpSettingsSession()
        {
            Actions = Resources.Load<InputActionAsset>("TumbangPreso");
            Rebinding.Load(Actions); Snapshot();
        }
        public bool Dirty => JsonUtility.ToJson(SettingsStore.Current) != _settings || Bindings() != _bindings || GenericPadBridge.Enabled != _genericSupport;
        private string Bindings() => Actions != null ? Actions.SaveBindingOverridesAsJson() : "";
        private void Snapshot() { _settings = JsonUtility.ToJson(SettingsStore.Current); _bindings = Bindings(); _genericSupport = GenericPadBridge.Enabled; }
        public void Preview(Action apply = null) { apply?.Invoke(); Changed?.Invoke(""); }
        public void Save()
        {
            SettingsStore.Current.Validate(); SettingsStore.Save(); SettingsStore.Current.Apply();
            Rebinding.Save(Actions); Snapshot(); Changed?.Invoke("Changes saved.");
        }
        public void Discard()
        {
            CancelRebind(); SettingsStore.Restore(JsonUtility.FromJson<GameSettings>(_settings));
            RestoreBindings(_bindings);
            if (GenericPadBridge.Enabled != _genericSupport) GenericPadBridge.Enabled = _genericSupport;
            Changed?.Invoke("Changes discarded.");
        }
        private void RestoreBindings(string json)
        {
            if (Actions == null) return;
            Actions.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json)) Actions.LoadBindingOverridesFromJson(json);
            Rebinding.Invalidate(); Rebinding.Save(Actions);
        }
        public void ResetControls()
        {
            if (Actions == null) return;
            // Stage the defaults without erasing the saved binding snapshot.
            Actions.RemoveAllBindingOverrides(); Rebinding.Invalidate(); Changed?.Invoke("Controls reset. Save to keep them.");
        }
        public void BeginRebind(string action, InputDeviceKind device)
        {
            if (_rebind != null) return;
            string refusal = RebindSession.RefusalFor(Actions, action, device);
            if (refusal != null) { Changed?.Invoke(refusal); return; }
            _beforeRebind = Bindings();
            Changed?.Invoke("Press a control for " + Rebinding.LabelFor(action) + ". Cancel to stop.");
            _rebind = RebindSession.Begin(Actions, action, device, (outcome, conflict) =>
            {
                _rebind = null;
                if (outcome != RebindOutcome.Bound) RestoreBindings(_beforeRebind);
                Changed?.Invoke(outcome == RebindOutcome.Bound ? "Control updated. Save to keep it."
                    : outcome == RebindOutcome.Conflict ? "Already used by " + conflict + ". Choose another control."
                    : "Rebind cancelled.");
            });
        }
        public void CancelRebind()
        {
            if (_rebind == null) return;
            _rebind.Dispose(); _rebind = null; RestoreBindings(_beforeRebind);
            Changed?.Invoke("Rebind cancelled.");
        }
        public void Dispose() { if (_rebind != null) CancelRebind(); }
    }
}
