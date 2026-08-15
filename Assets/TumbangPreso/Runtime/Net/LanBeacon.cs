using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>One host seen on the local network.</summary>
    public struct LanEntry
    {
        public string Address;
        public int Port;
        public string HostName;
        public string JoinCode;
        public int Players;
        public int MaxPlayers;
        public bool InProgress;
        public float LastSeen;
    }

    /// <summary>
    /// Local network host discovery, over plain UDP broadcast.
    ///
    /// ⚠️⚠️ DELIBERATELY TRANSPORT AGNOSTIC. The netcode stack is not decided (see
    /// docs/Port_Plan.md phase 5), and discovery does not need it: a broadcast that advertises
    /// "there is a host at this address and port" works the same whether the game session is
    /// carried by Mirror or by Netcode for GameObjects. Binding discovery to a transport would
    /// mean rewriting it when the decision lands, for no benefit.
    ///
    /// ⚠️ THE PORT AND THE MAGIC STRING ARE CARRIED OVER EXACTLY. Anything else and a Unity
    /// build cannot see a Godot build on the same LAN during the transition, which is exactly
    /// when you want to compare them side by side.
    ///
    /// ⚠️ AND THE HOST BROADCASTS RATHER THAN CLIENTS SCANNING. Scanning a subnet means probing
    /// 254 addresses and looks like a port scan to any network anybody cares about, including a
    /// competition venue's.
    /// </summary>
    public sealed class LanBeacon : MonoBehaviour
    {
        public const int DiscoveryPort = 8911;
        public const string Magic = "tumbang-preso-lan";
        public const float BeaconInterval = 1.0f;

        /// <summary>
        /// ⚠️ FOUR SECONDS IS FOUR MISSED BEACONS, NOT AN ARBITRARY TIMEOUT. UDP broadcast is
        /// lossy and a single dropped packet is normal, so a shorter window makes healthy hosts
        /// flicker in and out of the browser, which reads as an unstable game.
        /// </summary>
        public const float EntryTimeout = 4.0f;

        private UdpClient _listener;
        private UdpClient _sender;
        private readonly Dictionary<string, LanEntry> _seen = new Dictionary<string, LanEntry>();
        private float _nextBeacon;

        public bool Advertising { get; private set; }
        public bool Listening { get; private set; }

        /// <summary>What a host advertises. Set before calling <see cref="StartAdvertising"/>.</summary>
        public string HostName = "";
        public string JoinCode = "";
        public int Port = 8910;
        public int Players;
        public int MaxPlayers = 4;
        public bool InProgress;

        public IEnumerable<LanEntry> Entries => _seen.Values;

        public void StartAdvertising()
        {
            if (Advertising) return;

            try
            {
                _sender = new UdpClient { EnableBroadcast = true };
                Advertising = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lan] could not open the broadcast socket: {e.Message}");
            }
        }

        public void StartListening()
        {
            if (Listening) return;

            try
            {
                _listener = new UdpClient();
                _listener.Client.SetSocketOption(SocketOptionLevel.Socket,
                                                 SocketOptionName.ReuseAddress, true);
                _listener.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                _listener.BeginReceive(OnReceive, null);
                Listening = true;
            }
            catch (Exception e)
            {
                // ⚠️ NOT FATAL, AND THIS HAPPENS FOR ORDINARY REASONS: another copy of the game
                // already listening, or a firewall. The browser should show nothing rather than
                // the game refusing to open a menu.
                Debug.LogWarning($"[Lan] could not listen on {DiscoveryPort}: {e.Message}");
            }
        }

        public void StopAll()
        {
            Advertising = false;
            Listening = false;

            try { _sender?.Close(); } catch { }
            try { _listener?.Close(); } catch { }

            _sender = null;
            _listener = null;
        }

        private void OnDisable() => StopAll();
        private void OnDestroy() => StopAll();

        private void Update()
        {
            if (Advertising && Time.unscaledTime >= _nextBeacon)
            {
                _nextBeacon = Time.unscaledTime + BeaconInterval;
                Broadcast();
            }

            Expire();
        }

        private void Broadcast()
        {
            if (_sender == null) return;

            // magic|port|players|max|inProgress|joinCode|hostName
            // ⚠️ THE NAME GOES LAST because it is the only field that can contain anything,
            // and putting it last means a name with a separator in it cannot corrupt the
            // fields before it.
            string payload = string.Join("|",
                Magic,
                Port.ToString(),
                Players.ToString(),
                MaxPlayers.ToString(),
                InProgress ? "1" : "0",
                JoinCode ?? "",
                HostName ?? "");

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                _sender.Send(bytes, bytes.Length,
                             new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lan] broadcast failed: {e.Message}");
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            if (_listener == null) return;

            try
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _listener.EndReceive(ar, ref from);

                Parse(Encoding.UTF8.GetString(data), from.Address.ToString());

                if (Listening) _listener.BeginReceive(OnReceive, null);
            }
            catch (ObjectDisposedException)
            {
                // Socket closed while a receive was pending. Ordinary shutdown.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lan] receive failed: {e.Message}");
            }
        }

        /// <summary>
        /// ⚠️ EVERY FIELD IS VALIDATED BECAUSE THIS IS UNAUTHENTICATED NETWORK INPUT. Anything
        /// on the LAN can send a packet to this port. Nothing here may throw on a malformed
        /// payload, and nothing may be trusted into a UI without a length bound.
        /// </summary>
        private void Parse(string payload, string address)
        {
            if (string.IsNullOrEmpty(payload)) return;

            string[] parts = payload.Split('|');
            if (parts.Length < 7 || parts[0] != Magic) return;

            if (!int.TryParse(parts[1], out int port)) return;
            if (!int.TryParse(parts[2], out int players)) players = 0;
            if (!int.TryParse(parts[3], out int max)) max = 4;

            string name = parts[6];
            if (name.Length > Core.Balance.PlayerNameMax)
                name = name.Substring(0, Core.Balance.PlayerNameMax);

            string key = $"{address}:{port}";

            lock (_seen)
            {
                _seen[key] = new LanEntry
                {
                    Address = address,
                    Port = port,
                    HostName = name,
                    JoinCode = parts[5],
                    Players = Mathf.Clamp(players, 0, 64),
                    MaxPlayers = Mathf.Clamp(max, 1, 64),
                    InProgress = parts[4] == "1",
                    LastSeen = Time.unscaledTime,
                };
            }
        }

        private void Expire()
        {
            lock (_seen)
            {
                if (_seen.Count == 0) return;

                var dead = new List<string>();
                foreach (var kv in _seen)
                    if (Time.unscaledTime - kv.Value.LastSeen > EntryTimeout) dead.Add(kv.Key);

                foreach (var k in dead) _seen.Remove(k);
            }
        }
    }
}
