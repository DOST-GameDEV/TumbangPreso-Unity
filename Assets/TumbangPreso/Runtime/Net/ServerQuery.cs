using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// ONLINE LOBBY LIST AND JOIN CODES, converted from `scripts/systems/server_query.gd`.
    ///
    /// Players need two things the LAN browser cannot give them: a list of what is running on
    /// the online pool, and a short code they can read down a phone to a friend.
    ///
    /// Online play is a FIXED POOL of dedicated processes on one VM — several copies of the
    /// game, each bound to its own port, each refereeing exactly one match. This is how a
    /// client finds out what those processes are doing.
    ///
    /// ⚠️ PLAIN UDP UNICAST, BESIDE THE TRANSPORT AND NEVER ON TOP OF IT. A build that cannot
    /// reach the status ports still plays exactly as before by typing an address, which is
    /// how this game worked first. Do not fold this into the netcode transport.
    /// </summary>
    public sealed class ServerQuery : MonoBehaviour
    {
        /// <summary>Marks a packet as ours before anything else is parsed. A stray datagram
        /// on a UDP port is not an error worth logging, it is simply not for us.</summary>
        public const string Magic = "tumbang-preso-query";

        /// <summary>
        /// Bumped when the SHAPE of these packets changes. Both ends check it: a client that
        /// cannot read a reply must ignore it rather than draw half a row.
        ///
        /// A pool is deployed together from one build, so the game version is constant across
        /// every server a client can reach here and a field for it would only be a field to
        /// get wrong.
        /// </summary>
        public const int ProtocolVersion = 1;

        /// <summary>
        /// ⚠️⚠️ WIDEN THIS BEFORE WIDENING THE POOL. A status port is its game port plus this
        /// offset, so with an offset of 10 the eleventh server's game port collides with the
        /// first server's STATUS port and would silently answer for it.
        /// </summary>
        public const int StatusPortOffset = 10;

        /// <summary>The pool's address from outside — a VPS in Singapore.
        /// Empty is a valid, honest state: a build with nowhere to point sends nothing and
        /// says so on screen rather than sitting on "searching…" forever.</summary>
        public const string PoolAddress = "139.180.212.110";

        /// <summary>Inclusive. One process per port, one match per process. This range must
        /// stay within <see cref="StatusPortOffset"/> of its start.</summary>
        public const int PoolPortFirst = 8910;
        public const int PoolPortLast = 8917;

        /// <summary>Fast enough that a lobby filling up is visible about as quickly as a
        /// player can read the screen, slow enough that a handful of tiny datagrams a second
        /// are invisible next to the game's own traffic.</summary>
        public const float QueryInterval = 1.0f;

        /// <summary>
        /// ⚠️ FOUR QUERIES, NOT ONE. A server that has gone down must lose its row, but a
        /// single dropped datagram is ordinary on UDP and must not blink a row out from under
        /// a cursor that is about to click it.
        /// </summary>
        public const float EntryTimeout = 4.0f;

        /// <summary>Raised when the visible list actually CHANGES, not once per reply — a
        /// list that rebuilds every second cannot be clicked.</summary>
        public event Action ServersChanged;

        public sealed class Entry
        {
            public string Address;
            public int Port;
            public string Name;
            public int Players;
            public int Capacity;
            public bool InProgress;
            public float LastSeen;
        }

        private readonly Dictionary<string, Entry> _seen = new Dictionary<string, Entry>();
        private string _lastSignature = "";

        private UdpClient _client;
        private bool _browsing;
        private float _sinceQuery;

        public IEnumerable<Entry> Servers => _seen.Values;

        public void StartBrowsing()
        {
            if (_browsing) return;

            try
            {
                _client = new UdpClient(0) { EnableBroadcast = false };
                _client.Client.ReceiveTimeout = 1;
                _browsing = true;
                _sinceQuery = QueryInterval;   // ask immediately
            }
            catch (SocketException e)
            {
                Debug.Log($"[Query] cannot open a client socket: {e.Message}. " +
                          "Typing an address still works.");
            }
        }

        public void StopBrowsing()
        {
            _browsing = false;
            _client?.Close();
            _client = null;
            _seen.Clear();
        }

        private void Update()
        {
            if (!_browsing || _client == null) return;

            _sinceQuery += Time.unscaledDeltaTime;
            if (_sinceQuery >= QueryInterval)
            {
                _sinceQuery = 0.0f;
                SendQueries();
            }

            Receive();
            ExpireStale();
        }

        private void SendQueries()
        {
            if (string.IsNullOrEmpty(PoolAddress)) return;

            byte[] packet = Encoding.UTF8.GetBytes($"{Magic}|{ProtocolVersion}|query");

            for (int port = PoolPortFirst; port <= PoolPortLast; port++)
            {
                try
                {
                    _client.Send(packet, packet.Length, PoolAddress, port + StatusPortOffset);
                }
                catch (SocketException)
                {
                    // A pool member being unreachable is the normal case for an empty pool,
                    // not an error worth a line in the log every second.
                }
            }
        }

        private void Receive()
        {
            while (_client.Available > 0)
            {
                IPEndPoint from = null;

                try
                {
                    byte[] data = _client.Receive(ref from);
                    Parse(Encoding.UTF8.GetString(data), from);
                }
                catch (SocketException) { return; }
            }
        }

        /// <summary>
        /// `magic|version|name|players|capacity|inProgress`.
        ///
        /// ⚠️ A MALFORMED REPLY IS DROPPED SILENTLY. This is a public UDP port; anything at
        /// all can arrive on it, and a parse failure is not a fault worth reporting.
        /// </summary>
        private void Parse(string text, IPEndPoint from)
        {
            string[] parts = text.Split('|');
            if (parts.Length < 6) return;
            if (parts[0] != Magic) return;
            if (!int.TryParse(parts[1], out int version) || version != ProtocolVersion) return;

            int gamePort = from.Port - StatusPortOffset;
            string key = $"{from.Address}:{gamePort}";

            if (!_seen.TryGetValue(key, out var entry))
            {
                entry = new Entry { Address = from.Address.ToString(), Port = gamePort };
                _seen[key] = entry;
            }

            entry.Name = parts[2];
            int.TryParse(parts[3], out entry.Players);
            int.TryParse(parts[4], out entry.Capacity);
            entry.InProgress = parts[5] == "1";
            entry.LastSeen = Time.unscaledTime;

            RaiseIfChanged();
        }

        private void ExpireStale()
        {
            var dead = new List<string>();

            foreach (var pair in _seen)
                if (Time.unscaledTime - pair.Value.LastSeen > EntryTimeout) dead.Add(pair.Key);

            if (dead.Count == 0) return;

            foreach (string key in dead) _seen.Remove(key);
            RaiseIfChanged();
        }

        /// <summary>
        /// ⚠️ IT FIRES ON A CHANGE, NOT ON EVERY REPLY. Eight servers answering once a second
        /// is eight events a second, and a list that rebuilds under the cursor cannot be
        /// clicked. The signature is what the player can actually SEE.
        /// </summary>
        private void RaiseIfChanged()
        {
            var sb = new StringBuilder();

            foreach (var e in _seen.Values)
                sb.Append($"{e.Address}:{e.Port}:{e.Players}/{e.Capacity}:{e.InProgress};");

            string signature = sb.ToString();
            if (signature == _lastSignature) return;

            _lastSignature = signature;
            ServersChanged?.Invoke();
        }

        private void OnDestroy() => StopBrowsing();
    }
}
