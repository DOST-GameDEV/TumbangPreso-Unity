using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
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

        public bool IsJoinable => !InProgress && Players < MaxPlayers;
    }

    /// <summary>
    /// Local network host discovery over UDP broadcast with multi-interface routing.
    ///
    /// ⚠️⚠️ MULTI-INTERFACE BROADCAST IS CRITICAL. Broadcasting solely to 255.255.255.255
    /// sends packets through whichever adapter the OS routing table prioritises (frequently a
    /// virtual adapter such as Radmin, Hamachi, or WSL). That caused beacons on the Godot dev
    /// machine to be delivered into an empty virtual network while local LAN peers heard nothing.
    /// Enumerating all active IPv4 interfaces and computing subnet-directed broadcasts
    /// ensures packets reach the physical Ethernet and Wi-Fi segments.
    ///
    /// ⚠️ .NET EXPOSES THE REAL PREFIX via NetworkInterface.GetIPProperties(), which improves on
    /// Godot's hard-coded /24 assumption.
    ///
    /// ⚠️ FOUR SECONDS IS FOUR MISSED BEACONS, NOT AN ARBITRARY TIMEOUT. UDP broadcast is lossy
    /// and a single dropped packet is normal. A shorter window makes healthy hosts flicker in
    /// and out of the browser.
    ///
    /// ⚠️ EMITS Changed ON REAL STATE CHANGES ONLY, ignoring per-frame LastSeen updates, so
    /// UI lists do not rebuild under an active cursor.
    /// </summary>
    public sealed class LanBeacon : MonoBehaviour
    {
        public const int DiscoveryPort = 8911;
        public const string Magic = "tumbang-preso-lan";
        public const float BeaconInterval = 1.0f;
        public const float EntryTimeout = 4.0f;

        public event Action EntriesChanged;

        private UdpClient _listener;
        private UdpClient _sender;
        private readonly Dictionary<string, LanEntry> _seen = new Dictionary<string, LanEntry>();
        private string _lastSignature = "";
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

        /// <summary>
        /// Entries sorted: joinable first (not in progress and not full), then by fill descending,
        /// then alphabetically by name.
        /// </summary>
        public List<LanEntry> SortedEntries
        {
            get
            {
                lock (_seen)
                {
                    var list = new List<LanEntry>(_seen.Values);
                    list.Sort((a, b) =>
                    {
                        if (a.IsJoinable != b.IsJoinable)
                            return b.IsJoinable.CompareTo(a.IsJoinable);

                        if (a.Players != b.Players)
                            return b.Players.CompareTo(a.Players);

                        return string.Compare(a.HostName, b.HostName, StringComparison.OrdinalIgnoreCase);
                    });
                    return list;
                }
            }
        }

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

            lock (_seen)
            {
                if (_seen.Count > 0)
                {
                    _seen.Clear();
                    RaiseIfChanged();
                }
            }
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

        /// <summary>
        /// Constructs wire payload string: magic|port|players|max|inProgress|joinCode|hostName.
        /// ⚠️ THE NAME GOES LAST because it is the only free-form field.
        /// </summary>
        public static string BuildPayload(int port, int players, int maxPlayers, bool inProgress, string joinCode, string hostName)
        {
            return string.Join("|",
                Magic,
                port.ToString(),
                players.ToString(),
                maxPlayers.ToString(),
                inProgress ? "1" : "0",
                joinCode ?? "",
                hostName ?? "");
        }

        private void Broadcast()
        {
            if (_sender == null) return;

            string payload = BuildPayload(Port, Players, MaxPlayers, InProgress, JoinCode, HostName);
            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            var endpoints = GetBroadcastEndpoints();
            foreach (var ep in endpoints)
            {
                try
                {
                    _sender.Send(bytes, bytes.Length, ep);
                }
                catch
                {
                    // Ignore transient send failures on individual inactive or restricted interfaces.
                }
            }
        }

        /// <summary>
        /// Collects limited broadcast (255.255.255.255) plus directed subnet broadcast for each
        /// active IPv4 network interface.
        /// </summary>
        public static List<IPEndPoint> GetBroadcastEndpoints()
        {
            var endpoints = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Broadcast, DiscoveryPort)
            };

            try
            {
                var seenAddresses = new HashSet<IPAddress>();
                seenAddresses.Add(IPAddress.Broadcast);

                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var u in ipProps.UnicastAddresses)
                    {
                        if (u.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(u.Address)) continue;

                        if (u.IPv4Mask != null && !u.IPv4Mask.Equals(IPAddress.Any))
                        {
                            var bcast = CalculateSubnetBroadcast(u.Address, u.IPv4Mask);
                            if (seenAddresses.Add(bcast))
                            {
                                endpoints.Add(new IPEndPoint(bcast, DiscoveryPort));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lan] Interface enumeration warning: {e.Message}");
            }

            return endpoints;
        }

        /// <summary>Calculates the directed broadcast address for a given IP and subnet mask.</summary>
        public static IPAddress CalculateSubnetBroadcast(IPAddress ip, IPAddress mask)
        {
            byte[] ipBytes = ip.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();

            if (ipBytes.Length != maskBytes.Length) return IPAddress.Broadcast;

            byte[] broadcastBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastBytes);
        }

        private void OnReceive(IAsyncResult ar)
        {
            if (_listener == null) return;

            try
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _listener.EndReceive(ar, ref from);

                if (TryParsePayload(Encoding.UTF8.GetString(data), from.Address.ToString(), out var entry))
                {
                    string key = $"{entry.Address}:{entry.Port}";
                    lock (_seen)
                    {
                        _seen[key] = entry;
                    }
                    RaiseIfChanged();
                }

                if (Listening) _listener.BeginReceive(OnReceive, null);
            }
            catch (ObjectDisposedException)
            {
                // Socket closed during shutdown.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lan] receive failed: {e.Message}");
            }
        }

        /// <summary>
        /// Validates unauthenticated network payload safely without throwing.
        /// </summary>
        public static bool TryParsePayload(string payload, string remoteAddress, out LanEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(payload)) return false;

            string[] parts = payload.Split('|');
            if (parts.Length < 7 || parts[0] != Magic) return false;

            if (!int.TryParse(parts[1], out int port) || port <= 0) return false;
            if (!int.TryParse(parts[2], out int players)) players = 0;
            if (!int.TryParse(parts[3], out int max)) max = 4;

            string name = parts[6];
            if (name.Length > Core.Balance.PlayerNameMax)
                name = name.Substring(0, Core.Balance.PlayerNameMax);

            entry = new LanEntry
            {
                Address = remoteAddress,
                Port = port,
                HostName = Settings.GameSettings.SanitiseName(name),
                JoinCode = parts[5],
                Players = Mathf.Clamp(players, 0, 64),
                MaxPlayers = Mathf.Clamp(max, 1, 64),
                InProgress = parts[4] == "1",
                LastSeen = Time.unscaledTime,
            };

            return true;
        }

        private void Expire()
        {
            lock (_seen)
            {
                if (_seen.Count == 0) return;

                var dead = new List<string>();
                foreach (var kv in _seen)
                {
                    if (Time.unscaledTime - kv.Value.LastSeen > EntryTimeout)
                        dead.Add(kv.Key);
                }

                if (dead.Count > 0)
                {
                    foreach (var k in dead) _seen.Remove(k);
                    RaiseIfChanged();
                }
            }
        }

        private void RaiseIfChanged()
        {
            string signature = ComputeSignature();
            if (signature == _lastSignature) return;

            _lastSignature = signature;
            EntriesChanged?.Invoke();
        }

        private string ComputeSignature()
        {
            var sb = new StringBuilder();
            foreach (var e in SortedEntries)
            {
                sb.Append($"{e.Address}:{e.Port}:{e.HostName}:{e.JoinCode}:{e.Players}/{e.MaxPlayers}:{e.InProgress};");
            }
            return sb.ToString();
        }
    }
}
