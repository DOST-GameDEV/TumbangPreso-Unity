using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `multiplayer_setup.gd`.
    ///
    /// ⚠️ HOSTING ONLINE AND HOSTING ON THE LAN ARE TWO DIFFERENT BUTTONS, and they always were.
    /// The online path is what the Singapore VPS serves, and collapsing them into one control
    /// removes the mode the team actually ships to players outside the room.
    /// </summary>
    public sealed class ConvertedMultiplayerSetup : ConvertedScreen
    {
        private Net.NetSession _net;
        private InputField _address;

        /// <summary>`multiplayer_setup.gd` backs out to the mode screen on Escape.</summary>
        protected override string CancelTarget => SceneFlow.ModeSelect;

        protected override void Wire()
        {
            _net = Net.NetSession.Ensure();
            _net.StatusChanged += s => SetText("StatusLabel", s);

            SetText("BannerLabel", "MULTIPLAYER");

            OnClick("HostOnlineButton", async () =>
            {
                bool ok = await _net.StartRelayHost();
                if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("HostButton", () =>
            {
                if (_net.StartHost()) SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("JoinButton", Join);
            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.ModeSelect));

            BindAddressField();
            _net.BrowseLan();
            SetText("StatusLabel", _net.Status);
        }

        /// <summary>The converted JoinAddressEdit is a real LineEdit now, so this only seeds it.</summary>
        private void BindAddressField()
        {
            var t = Node("JoinAddressEdit");
            if (t == null) return;

            _address = t.GetComponent<InputField>();
            if (_address == null) return;

            if (string.IsNullOrWhiteSpace(_address.text)) _address.text = "127.0.0.1";
        }

        private async void Join()
        {
            string addr = _address == null || string.IsNullOrWhiteSpace(_address.text)
                ? "127.0.0.1"
                : _address.text.Trim();

            // If it contains dots, colons, or is localhost/standard IP, use direct transport.
            // Otherwise, treat as a Relay join code.
            if (addr.Contains(".") || addr.Contains(":") || addr.Equals("localhost", System.StringComparison.OrdinalIgnoreCase))
            {
                _net.StartClient(addr);
            }
            else
            {
                await _net.StartRelayClient(addr);
            }
        }
    }
}
