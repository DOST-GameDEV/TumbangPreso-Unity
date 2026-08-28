using System;
using System.Collections.Concurrent;
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

        /// <summary>
        /// Who sent this advertisement, as a per-PROCESS id rather than a per-machine one.
        ///
        /// ⚠️⚠️ IT EXISTS SO A HOST STOPS FINDING ITSELF IN ITS OWN BROWSER. 🧑 2026-08-29:
        /// *"na kikita sarili sa lobby (join a game)"*. A host broadcasts to 255.255.255.255 and
        /// to every interface's directed broadcast, and its own listener is bound to
        /// `IPAddress.Any` on the same port, so it receives every packet it sends. The row looked
        /// exactly like a real game because it WAS one, which is why nothing about it read as a
        /// bug until somebody pressed join on it.
        ///
        /// ⚠️ ADDRESS AND PORT CANNOT ANSWER THIS. `remoteAddress` for our own packet is whichever
        /// local interface the OS looped it back through, which is not knowable in advance, and
        /// on a machine with a virtual adapter it is routinely not the address we would guess.
        /// An id we minted and can compare exactly is the only reliable form of the question.
        ///
        /// ⚠️ AND PER PROCESS, NOT PER MACHINE. Two builds running side by side on one PC for a
        /// two-window test are two different games and each must still see the other.
        /// </summary>
        public string BeaconId;

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

        /// <summary>
        /// The layout that carries <see cref="LanEntry.BeaconId"/>.
        ///
        /// ⚠️⚠️ A NEW MAGIC RATHER THAN A NEW FIELD COUNT, BECAUSE THE NAME IS THE LAST FIELD AND
        /// IT MAY CONTAIN THE SEPARATOR. `TryParsePayload` reads the host name as "everything
        /// from index N onwards" precisely so a player called `A|B` truncates nothing, and that
        /// is exactly what makes `parts.Length` useless as a version discriminator: a v1 packet
        /// from a player with one pipe in their name has the same field count as a v2 packet.
        /// The magic is field 0, is never free-form, and settles it in one comparison.
        ///
        /// ⚠️ v1 IS STILL READ. A build from before this change advertises the old magic and is
        /// listed rather than silently missing from the browser, with an empty id: see
        /// <see cref="IsOurOwn"/> for why an empty id can never match ours.
        /// </summary>
        public const string MagicV2 = "tumbang-preso-lan2";
        public const float BeaconInterval = 1.0f;
        public const float EntryTimeout = 4.0f;

        public event Action EntriesChanged;

        private UdpClient _listener;
        private UdpClient _sender;
        private readonly Dictionary<string, LanEntry> _seen = new Dictionary<string, LanEntry>();
        private string _lastSignature = "";
        private float _nextBeacon;

        /// <summary>
        /// Packets parsed on the socket thread, waiting for <see cref="Update"/> to take them.
        ///
        /// ⚠️⚠️ THIS QUEUE IS WHY LAN DISCOVERY WORKS AT ALL, AND IT IS NOT AN OPTIMISATION.
        /// `BeginReceive` calls <see cref="OnReceive"/> on a THREAD POOL thread. The old code
        /// wrote straight into `_seen` and then raised <see cref="EntriesChanged"/> from there,
        /// which lands in `ConvertedMultiplayerSetup.RefreshLanBrowser` — `Text`, `SetActive`,
        /// `rectTransform`. Every one of those throws off the main thread. So did the
        /// `Time.unscaledTime` that used to stamp `LastSeen` inside `TryParsePayload`.
        ///
        /// ⚠️ THE THROW HAPPENED BEFORE THE RE-ARM, so the socket was never handed back to
        /// `BeginReceive` and discovery stopped DEAD on the very first beacon received. The host
        /// kept advertising perfectly, which is exactly the reported shape: hosting works, and
        /// nothing ever appears in the browser.
        ///
        /// ⚠️ THE TESTS COULD NOT SEE IT. Every `TryParsePayload` case calls it from the test
        /// thread, which IS Unity's main thread, so the Unity call inside it was legal there and
        /// the parser looked correct in isolation. It was only ever wrong on the socket thread.
        /// </summary>
        private readonly ConcurrentQueue<LanEntry> _inbox = new ConcurrentQueue<LanEntry>();

        public bool Advertising { get; private set; }
        public bool Listening { get; private set; }

        /// <summary>What a host advertises. Set before calling <see cref="StartAdvertising"/>.</summary>
        public string HostName = "";
        public string JoinCode = "";
        public int Port = 8910;

        /// <summary>
        /// This process's beacon id, minted once at load and never reused.
        ///
        /// ⚠️ A GUID RATHER THAN `NetIdentity.Token`. The token is the player's PERSISTENT
        /// identity, kept across launches so a reconnecting peer gets its seat back; two windows
        /// of the same build on one machine would share it, and the second window would then
        /// filter the first one out of its browser as itself. This has to be per process and
        /// nothing else in the project already is.
        /// </summary>
        public static readonly string ProcessBeaconId = Guid.NewGuid().ToString("N").Substring(0, 12);
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

                // ⚠️ THE FLAG IS SET BEFORE THE FIRST `BeginReceive`, NOT AFTER IT. A beacon that
                // arrives between the two runs `OnReceive` with `Listening` still false, and the
                // re-arm at the end of it is skipped, so discovery dies on the first packet of a
                // busy network. Setting it first costs nothing: the failure path below clears it.
                Listening = true;
                _listener.BeginReceive(OnReceive, null);
            }
            catch (Exception e)
            {
                Listening = false;
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

            // ⚠️ Anything the socket thread queued but Update() never took would otherwise be
            // drained into a FRESH browse session and shown as live hosts that were last seen
            // before the previous session was torn down.
            while (_inbox.TryDequeue(out _)) { }

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

            DrainInbox();
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
            => BuildPayload(port, seated, maxPlayers, inProgress, joinCode, hostName,
                            occupied, connections, maxConnections, ProcessBeaconId);

        /// <summary>
        /// The v2 layout:
        /// `magicV2|port|seated|maxSeats|inProgress|joinCode|occupied|connections|maxConnections|beaconId|hostName`.
        ///
        /// ⚠️ THE ID GOES BEFORE THE NAME, like every other field, for the reason the note above
        /// gives: the name is the only free-form value on this wire and it is therefore the only
        /// one that may hold the separator, so it has to stay last.
        /// </summary>
        public static string BuildPayload(int port, int seated, int maxPlayers, bool inProgress,
                                          string joinCode, string hostName,
                                          int occupied, int connections, int maxConnections,
                                          string beaconId)
        {
            return string.Join("|",
                MagicV2,
                port.ToString(),
                seated.ToString(),
                maxPlayers.ToString(),
                inProgress ? "1" : "0",
                joinCode ?? "",
                occupied.ToString(),
                connections.ToString(),
                maxConnections.ToString(),
                beaconId ?? "",
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

        /// <summary>
        /// Socket-thread half of discovery: parse, queue, re-arm. Touches NOTHING owned by Unity.
        ///
        /// ⚠️⚠️ THE RE-ARM IS IN A `finally`, AND THAT IS THE WHOLE POINT. It used to sit at the
        /// end of the `try`, so ANY throw above it — a Unity call, a malformed packet, another
        /// program on port 8911 — consumed the pending receive and never asked for another one.
        /// One bad datagram permanently ended discovery for the rest of the process, and the only
        /// evidence was a single warning line. A parse failure must cost one packet, not the
        /// socket.
        /// </summary>
        private void OnReceive(IAsyncResult ar)
        {
            var listener = _listener;
            if (listener == null) return;

            try
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = listener.EndReceive(ar, ref from);

                if (TryParsePayload(Encoding.UTF8.GetString(data), from.Address.ToString(), out var entry))
                {
                    // Handed to Update(); see _inbox. LastSeen is stamped there, on the main thread.
                    _inbox.Enqueue(entry);
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket closed during shutdown. Do not re-arm; there is nothing to re-arm onto.
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lan] receive failed: {e.Message}");
            }

            try
            {
                if (Listening) listener.BeginReceive(OnReceive, null);
            }
            catch (ObjectDisposedException)
            {
                // Raced with StopAll.
            }
            catch (Exception e)
            {
                Listening = false;
                Debug.LogWarning($"[Lan] could not re-arm the discovery socket: {e.Message}");
            }
        }

        /// <summary>
        /// Main-thread half: drains what the socket thread parsed and stamps it with the clock.
        /// </summary>
        private void DrainInbox()
        {
            bool touched = false;

            while (_inbox.TryDequeue(out var entry))
            {
                // ⚠️⚠️ OUR OWN ADVERTISEMENT IS DROPPED HERE, AND THAT IS WHY THIS FILTER IS ON
                // THE MAIN THREAD RATHER THAN IN `TryParsePayload`. The parser is a pure function
                // over a payload and a remote address, it runs on the socket thread, and it is
                // exercised directly by the tests; asking it about `this` beacon's live state
                // would make it neither pure nor testable. The drain is the first point that has
                // both the parsed entry and the component that sent it.
                if (IsOurOwn(entry)) continue;

                entry.LastSeen = Time.unscaledTime;
                string key = $"{entry.Address}:{entry.Port}";
                lock (_seen)
                {
                    _seen[key] = entry;
                }
                touched = true;
            }

            if (touched) RaiseIfChanged();
        }

        /// <summary>
        /// Is this entry the packet we just sent, come back to us?
        ///
        /// ⚠️⚠️ THIS IS THE WHOLE OF 🧑 2026-08-29's *"na kikita sarili sa lobby (join a game)"*.
        /// A host sends to `IPAddress.Broadcast` and to every interface's directed broadcast, and
        /// its own listener is bound to `IPAddress.Any` on the same port, so it hears itself on
        /// every interval. The row was indistinguishable from a real game because it was one, and
        /// pressing JOIN on it asks the transport to connect to a server this process already is.
        ///
        /// ⚠️ GATED ON `Advertising`, so a machine that is only BROWSING filters nothing. Without
        /// that gate a client would compare against an id it never broadcasts, which is harmless
        /// but says something untrue about what this method is for.
        ///
        /// ⚠️ AN EMPTY ID NEVER MATCHES. A v1 host on the network advertises no id at all, and
        /// `ProcessBeaconId` is a 12-character GUID slice that is never empty, so an old build is
        /// listed rather than mistaken for us. That is the correct failure direction: showing one
        /// row too many is a nuisance and hiding a real host is a game nobody can join.
        /// </summary>
        private bool IsOurOwn(LanEntry entry)
        {
            if (!Advertising) return false;
            if (string.IsNullOrEmpty(entry.BeaconId)) return false;

            return entry.BeaconId == ProcessBeaconId;
        }

        /// <summary>
        /// Validates unauthenticated network payload safely without throwing.
        /// </summary>
        public static bool TryParsePayload(string payload, string remoteAddress, out LanEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(payload)) return false;

            string[] parts = payload.Split('|');
            if (parts.Length < 7) return false;

            // ⚠️ THE VERSION IS FIELD 0 AND NOTHING ELSE. See `MagicV2` for why the field count
            // cannot be asked this question.
            bool v2 = parts[0] == MagicV2;
            if (!v2 && parts[0] != Magic) return false;

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
            // v2 spends one more field on the beacon id, so the name starts one later.
            string beaconId = v2 && parts.Length >= 11 ? parts[9] : "";

            int nameIndex = v2 && parts.Length >= 11 ? 10 : extended ? 9 : 6;
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
                BeaconId = beaconId,

                // ⚠️ NOT STAMPED HERE. This runs on the socket thread and `Time.unscaledTime` is
                // a Unity call that throws off the main thread; `DrainInbox` stamps it instead.
                // Leaving the field at 0 is safe because an entry only reaches `_seen` through
                // that drain, and `Expire` only ever reads what the drain has written.
                LastSeen = 0f,
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
