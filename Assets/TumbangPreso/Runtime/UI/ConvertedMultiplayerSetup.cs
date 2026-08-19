using System;
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
    /// The online path allocates a Relay server via UGS, while the LAN path opens a direct socket.
    ///
    /// ⚠️ LAN IS SEARCHED FIRST, THEN ONLINE LOBBIES when resolving join codes.
    /// A player typing a code does not know or care whether the host is across the room or on the WAN.
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
            _net.StatusChanged += OnStatusChanged;

            SetText("BannerLabel", "MULTIPLAYER");

            OnClick("HostOnlineButton", async () =>
            {
                SetText("StatusLabel", "Allocating online session...");
                bool ok = await _net.StartRelayHost();
                if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("HostButton", () =>
            {
                SetText("StatusLabel", "Starting LAN session...");
                if (_net.StartHost()) SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("JoinButton", Join);
            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.ModeSelect));

            BindAddressField();
            _net.BrowseLan();
            _net.Query.StartBrowsing();
            SetText("StatusLabel", _net.Status);
        }

        private void OnStatusChanged(string s) => SetText("StatusLabel", s);

        /// <summary>Seeds the join address edit box and handles enter key submission.</summary>
        private void BindAddressField()
        {
            var t = Node("JoinAddressEdit");
            if (t == null) return;

            _address = t.GetComponent<InputField>();
            if (_address == null) return;

            if (string.IsNullOrWhiteSpace(_address.text)) _address.text = "";
            _address.onSubmit.RemoveAllListeners();
            _address.onSubmit.AddListener(_ => Join());
        }

        private async void Join()
        {
            string addr = _address == null || string.IsNullOrWhiteSpace(_address.text)
                ? ""
                : _address.text.Trim();

            if (string.IsNullOrEmpty(addr))
            {
                SetText("StatusLabel", "Enter a 4-character join code or IP address.");
                return;
            }

            // Direct IP or hostname
            if (addr.Contains(".") || addr.Contains(":") || addr.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                SetText("StatusLabel", $"Connecting to {addr}...");
                bool ok = _net.StartClient(addr);
                if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                return;
            }

            // 4-character join code lookup (searches LAN beacon first, then UGS online lobby)
            if (addr.Length >= Net.LobbySession.JoinCodeLength)
            {
                string code = addr.ToUpperInvariant();
                SetText("StatusLabel", $"Resolving join code {code}...");

                var resolved = await _net.Query.ResolveCodeAsync(code);
                if (resolved.HasValue && resolved.Value.Found)
                {
                    var match = resolved.Value;
                    if (match.IsLan)
                    {
                        SetText("StatusLabel", $"Joining LAN host {match.HostName} ({match.Address}:{match.Port})...");
                        bool ok = _net.StartClient(match.Address, match.Port);
                        if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                    }
                    else
                    {
                        SetText("StatusLabel", $"Joining online host {match.HostName} via Relay...");
                        bool ok = await _net.StartRelayClient(match.RelayCode);
                        if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                    }
                    return;
                }

                SetText("StatusLabel", $"No active match found for code '{code}'.");
                return;
            }

            SetText("StatusLabel", "Join code must be 4 characters.");
        }

        private void OnDestroy()
        {
            if (_net != null)
            {
                _net.StatusChanged -= OnStatusChanged;
                _net.Query?.StopBrowsing();
            }
        }
    }
}
