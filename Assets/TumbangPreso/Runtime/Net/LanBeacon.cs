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

        /// <summary>Seats actually being played, spectators excluded. The number a player reads.</summary>
        public int Players;

        /// <summary>Seat capacity, always 4. What <see cref="Players"/> is drawn against.</summary>
        public int MaxPlayers;

        /// <summary>
        /// Seats a newcomer cannot have: seated peers plus seats held for a dropped player.
        ///
        /// ⚠️⚠️ THIS IS WHY A SECOND COUNT EXISTS AT ALL. The beacon carried one number and it
        /// was `LobbySession.PeerCount`, which counts CONNECTIONS. A lobby with two players and
        /// six spectators advertised 8/4 and every browser filtered it out as full, while a
        /// lobby holding a seat for somebody who had dropped advertised 3/4 and refused the next
        /// person to press join. Joinability is decided by this field and readability by
        /// <see cref="Players"/>; they are different questions and they had one answer.
        /// </summary>
        public int Occupied;

        /// <summary>Every attached human, spectators included.</summary>
        public int Connections;

        /// <summary>Connection ceiling, 12. Larger than <see cref="MaxPlayers"/> on purpose.</summary>
        public int MaxConnections;

        public bool InProgress;
        public float LastSeen;

        /// <summary>A free CHAIR, and room on the wire for the socket that would take it.</summary>
        public bool IsJoinable =>
            !InProgress && Occupied < MaxPlayers && Connections < MaxConnections;

        /// <summary>Room to attach and watch, even when every chair is taken.</summary>
        public bool CanSpectate => Connections < MaxConnections;
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
        public int MaxPlayers = LobbySession.MaxPlayers;
        public int Occupied;
        public int Connections;
        public int MaxConnections = LobbySession.MaxConnections;
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
        /// Constructs the wire payload:
        /// `magic|port|seated|maxSeats|inProgress|joinCode|occupied|connections|maxConnections|hostName`.
        ///
        /// ⚠️ THE NAME GOES LAST because it is the only free-form field, and the new counts are
        /// therefore inserted BEFORE it rather than appended. A parser that reads the name as
        /// "everything after field 8" cannot be confused by a name containing the separator.
        ///
        /// ⚠️ THE OLD SEVEN-FIELD LAYOUT IS STILL READ, not still written. `TryParsePayload`
        /// accepts it and fills the three new counts from the single old one, so a build from
        /// before this change is listed rather than silently missing from the browser.
        /// </summary>
        public static string BuildPayload(int port, int seated, int maxPlayers, bool inProgress,
                                          string joinCode, string hostName)
            => BuildPayload(port, seated, maxPlayers, inProgress, joinCode, hostName,
                            seated, seated, LobbySession.MaxConnections);

        public static string BuildPayload(int port, int seated, int maxPlayers, bool inProgress,
                                          string joinCode, string hostName,
                                          int occupied, int connections, int maxConnections)
        {
            return string.Join("|",
                Magic,
                port.ToString(),
                seated.ToString(),
                maxPlayers.ToString(),
                inProgress ? "1" : "0",
                joinCode ?? "",
                occupied.ToString(),
                connections.ToString(),
                maxConnections.ToString(),
                hostName ?? "");
        }

        private void Broadcast()
        {
            if (_sender == null) return;

            string payload = BuildPayload(Port, Players, MaxPlayers, InProgress, JoinCode, HostName,
                                          Occupied, Connections, MaxConnections);
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
            if (!int.TryParse(parts[2], out int seated)) seated = 0;
            if (!int.TryParse(parts[3], out int maxSeats)) maxSeats = LobbySession.MaxPlayers;

            bool extended = parts.Length >= 10;
            int occupied = seated;
            int connections = seated;
            int maxConnections = LobbySession.MaxConnections;

            if (extended)
            {
                if (!int.TryParse(parts[6], out occupied)) occupied = seated;
                if (!int.TryParse(parts[7], out connections)) connections = seated;
                if (!int.TryParse(parts[8], out maxConnections))
                    maxConnections = LobbySession.MaxConnections;
            }

            // ⚠️ THE NAME IS EVERYTHING FROM ITS INDEX ONWARDS, not one field. A player name is
            // the only value on this wire that a person types, and rejoining the remainder is
            // what keeps a name containing the separator from truncating rather than corrupting.
            int nameIndex = extended ? 9 : 6;
            string name = string.Join("|", parts, nameIndex, parts.Length - nameIndex);
            if (name.Length > Core.Balance.PlayerNameMax)
                name = name.Substring(0, Core.Balance.PlayerNameMax);

            entry = new LanEntry
            {
                Address = remoteAddress,
                Port = port,
                HostName = Settings.GameSettings.SanitiseName(name),
                JoinCode = parts[5],
                Players = Mathf.Clamp(seated, 0, 64),
                MaxPlayers = Mathf.Clamp(maxSeats, 1, 64),
                Occupied = Mathf.Clamp(occupied, 0, 64),
                Connections = Mathf.Clamp(connections, 0, 64),
                MaxConnections = Mathf.Clamp(maxConnections, 1, 64),
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
                sb.Append($"{e.Address}:{e.Port}:{e.HostName}:{e.JoinCode}:" +
                          $"{e.Players}/{e.MaxPlayers}:{e.Occupied}:" +
                          $"{e.Connections}/{e.MaxConnections}:{e.InProgress};");
            }
            return sb.ToString();
        }
    }
}
