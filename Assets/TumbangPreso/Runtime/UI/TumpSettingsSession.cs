using System;
using System.Threading.Tasks;
using TumbangPreso.Core;
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
        private string _nameDraft, _nameOwnerId;
        private Net.PlayerAccount _nameOwner;
        public InputActionAsset Actions { get; }
        public bool Listening => _rebind != null;
        public bool Saving { get; private set; }
        public string PlayerName => _nameDraft ?? CurrentPlayerName;
        private static string CurrentPlayerName => GameServices.Account?.DisplayName ?? SettingsStore.Current.PlayerName;
        public event Action<string> Changed;
        public TumpSettingsSession()
        {
            Actions = Resources.Load<InputActionAsset>("TumbangPreso");
            Rebinding.Load(Actions); Snapshot();
        }
        public bool Dirty => _nameDraft != null || JsonUtility.ToJson(SettingsStore.Current) != _settings ||
                             Bindings() != _bindings || GenericPadBridge.Enabled != _genericSupport;
        private string Bindings() => Actions != null ? Actions.SaveBindingOverridesAsJson() : "";
        private void Snapshot() { _settings = JsonUtility.ToJson(SettingsStore.Current); _bindings = Bindings(); _genericSupport = GenericPadBridge.Enabled; }
        public void Preview(Action apply = null) { apply?.Invoke(); Changed?.Invoke(""); }
        public void EditPlayerName(string value)
        {
            if (Saving) return;
            if (_nameDraft == null)
            {
                _nameOwner = GameServices.Account;
                _nameOwnerId = _nameOwner?.PlayerId;
            }
            _nameDraft = value == CurrentPlayerName ? null : value;
            Preview();
        }

        private bool NameOwnerIsCurrent => ReferenceEquals(_nameOwner, GameServices.Account) &&
                                           _nameOwnerId == GameServices.Account?.PlayerId;

        public async Task<bool> SaveAsync()
        {
            if (Saving) return false;
            bool rename = _nameDraft != null;
            string name = null;
            if (rename && !AccountRules.TryDisplayName(_nameDraft, out name))
            {
                Changed?.Invoke($"Use {AccountRules.DisplayNameMin} to {AccountRules.DisplayNameMax} characters for your player name: letters, numbers, spaces, dots, hyphens or underscores.");
                return false;
            }
            if (rename && !NameOwnerIsCurrent)
            {
                Changed?.Invoke("The active profile changed. Reopen Settings to rename it.");
                return false;
            }

            Saving = true;
            string message = "Changes saved.";
            try
            {
                Changed?.Invoke("Saving...");
                if (rename && _nameOwner == null) SettingsStore.Current.PlayerName = name;
                SettingsStore.Current.Validate(); SettingsStore.Save(); SettingsStore.Current.Apply();
                Rebinding.Save(Actions); Snapshot();
                if (rename && _nameOwner != null && name != _nameOwner.DisplayName)
                {
                    var account = _nameOwner;
                    if (!await account.SetProfileAsync(name, account.Bio, account.Country, account.Pronouns) ||
                        !NameOwnerIsCurrent)
                    {
                        message = "The active profile changed. Reopen Settings to rename it.";
                        return false;
                    }
                    Snapshot();
                }
                _nameDraft = null;
                return true;
            }
            catch (Exception e)
            {
                message = e.Message;
                return false;
            }
            finally
            {
                Saving = false;
                Changed?.Invoke(message);
            }
        }
        public void Discard()
        {
            if (Saving) return;
            _nameDraft = null;
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
                // The operation ends during input update, before the view reads legacy Back.
                if (outcome == RebindOutcome.Cancelled) ScreenTakeover.ConsumeEscape();
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
