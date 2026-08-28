using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>Result of resolving a 4-character join code across LAN and online.</summary>
    public struct ResolvedMatch
    {
        public bool Found;
        public bool IsLan;
        public string Address;
        public int Port;
        public string RelayCode;
        public string JoinCode;
        public string HostName;
        public int Seated;
        public int Occupied;
        public int MaxPlayers;
        public bool InProgress;

        public bool IsJoinable => !InProgress && Occupied < MaxPlayers;
    }

    /// <summary>
    /// Online lobby discovery and unified LAN/online join code resolution.
    ///
    /// ⚠️ RETIREMENT OF THE FIXED VPS POOL (2026-08-19). The original Godot implementation
    /// queried a fixed pool of dedicated server processes on a Singapore VPS (139.180.212.110
    /// across ports 8910-8917 with a +10 status port offset). That fixed pool is retired in
    /// favor of UGS Lobby discovery and Multiplay on-demand fleet allocation. The legacy UDP
    /// unicast query loop, status port offset, and pool address constants are removed.
    ///
    /// ⚠️ LAN-FIRST JOIN CODE RESOLUTION SURVIVES. A join code is an opaque handle for 'the
    /// match my friend is in', and the player does not know whether it is hosted on the LAN or
    /// online. Resolution searches LanBeacon first, then queries UGS Lobby data.
    ///
    /// ⚠️ CODE RESOLUTION GUARDS. A code box resolving on the first keystroke finds nothing
    /// and must not report failure until the full 4-character code is typed.
    ///
    /// ⚠️ TWO COUNTS ARE KEPT DISTINCT:
    /// - Seated count: spectators excluded. How full the match is (shown in UI).
    /// - Occupied count: all humans attached. Whether the session is free to claim.
    /// A lobby holding only a spectator shows 0/4 seated while correctly reporting 1 occupied.
    /// </summary>
    public sealed class ServerQuery : MonoBehaviour
    {
        /// <summary>Interval between background online lobby queries to respect UGS rate limits.</summary>
        public const float QueryInterval = 4.0f;

        /// <summary>Interval for UGS lobby heartbeats (service expires lobbies after 30s without ping).</summary>
        public const float HeartbeatInterval = 15.0f;

        /// <summary>Raised when the visible online server list changes.</summary>
        public event Action ServersChanged;

        public sealed class Entry
        {
            public string Id;
            public string Name;
            public string JoinCode;
            public string RelayCode;
            public int Seated;
            public int Occupied;
            public int Capacity;
            public bool InProgress;
            public float LastSeen;

            public int Players => Seated;
            public bool IsJoinable => !InProgress && Occupied < Capacity;
        }

        private readonly Dictionary<string, Entry> _seen = new Dictionary<string, Entry>();
        private string _lastSignature = "";
        private bool _browsing;
        private float _sinceQuery;
        private float _sinceHeartbeat;
        private string _activeHostLobbyId;
        private bool _queryInFlight;

        // ⚠️⚠️ LOBBY CREATION IS A NETWORK ROUND TRIP AND PLAYERS ARRIVE DURING IT. `NetSession`
        // fires `CreateHostedLobbyAsync` and does not await it, so `_activeHostLobbyId` is null
        // for as long as UGS takes to answer. Every `UpdateHostedLobbyAsync` in that window used
        // to return on its first line, and the update that got dropped was usually the one that
        // mattered: the first player joining. The lobby then advertised 0 seated until somebody
        // else connected, which on a two-player match is forever.
        private bool _hasPendingCounts;
        private int _pendingSeated;
        private int _pendingOccupied;
        private bool _pendingInProgress;
        private bool _creatingLobby;

        public IEnumerable<Entry> Servers => _seen.Values;

        public void StartBrowsing()
        {
            if (_browsing) return;

            _browsing = true;
            _sinceQuery = QueryInterval; // Query immediately
        }

        public void StopBrowsing()
        {
            _browsing = false;
            lock (_seen)
            {
                if (_seen.Count > 0)
                {
                    _seen.Clear();
                    RaiseIfChanged();
                }
            }
        }

        private void Update()
        {
            if (_browsing)
            {
                _sinceQuery += Time.unscaledDeltaTime;
                if (_sinceQuery >= QueryInterval && !_queryInFlight)
                {
                    _sinceQuery = 0.0f;
                    _ = RefreshOnlineLobbiesAsync();
                }
            }

            if (!string.IsNullOrEmpty(_activeHostLobbyId))
            {
                _sinceHeartbeat += Time.unscaledDeltaTime;
                if (_sinceHeartbeat >= HeartbeatInterval)
                {
                    _sinceHeartbeat = 0.0f;
                    _ = SendHeartbeatAsync();
                }
            }
        }

        /// <summary>
        /// Queries public UGS Lobbies and updates the visible list.
        /// </summary>
        public async Task RefreshOnlineLobbiesAsync()
        {
            if (_queryInFlight) return;
            _queryInFlight = true;

            try
            {
                // ⚠ SILENT ON PURPOSE. The reason online is unavailable was logged once, at
                // boot, by NetIdentity itself. This call awaits that same settled attempt, so
                // logging here again is what turned one situation into 21 identical warnings.
                bool authOk = await NetIdentity.EnsureSignedInAsync();
                if (!authOk) return;

                var options = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };

                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
                var freshIds = new HashSet<string>();

                if (response?.Results != null)
                {
                    lock (_seen)
                    {
                        foreach (var lobby in response.Results)
                        {
                            if (lobby == null || string.IsNullOrEmpty(lobby.Id)) continue;
                            freshIds.Add(lobby.Id);

                            string joinCode = "";
                            string relayCode = "";
                            int seated = lobby.Players?.Count ?? 0;
                            int occupied = seated;
                            bool inProgress = false;

                            if (lobby.Data != null)
                            {
                                if (lobby.Data.TryGetValue("JoinCode", out var jc)) joinCode = jc.Value;
                                if (lobby.Data.TryGetValue("RelayCode", out var rc)) relayCode = rc.Value;
                                if (lobby.Data.TryGetValue("Seated", out var s) && int.TryParse(s.Value, out int sVal)) seated = sVal;
                                if (lobby.Data.TryGetValue("Occupied", out var o) && int.TryParse(o.Value, out int oVal)) occupied = oVal;
                                if (lobby.Data.TryGetValue("InProgress", out var ip)) inProgress = ip.Value == "1";
                            }

                            if (!_seen.TryGetValue(lobby.Id, out var entry))
                            {
                                entry = new Entry { Id = lobby.Id };
                                _seen[lobby.Id] = entry;
                            }

                            entry.Name = lobby.Name;
                            entry.JoinCode = joinCode;
                            entry.RelayCode = relayCode;
                            entry.Seated = seated;
                            entry.Occupied = occupied;
                            entry.Capacity = lobby.MaxPlayers;
                            entry.InProgress = inProgress;
                            entry.LastSeen = Time.unscaledTime;
                        }

                        // Remove lobbies no longer present in query
                        var dead = new List<string>();
                        foreach (var key in _seen.Keys)
                        {
                            if (!freshIds.Contains(key)) dead.Add(key);
                        }
                        foreach (var k in dead) _seen.Remove(k);
                    }

                    RaiseIfChanged();
                }
            }
            catch (Exception e)
            {
                NetIdentity.ReportServiceCallFailed("Lobby query", e);
            }
            finally
            {
                _queryInFlight = false;
            }
        }

        /// <summary>
        /// Unified join code resolution: checks LAN beacon first, then queries UGS Lobby data.
        /// Requires a complete 4-character code before executing to avoid premature failure reports.
        /// </summary>
        public async Task<ResolvedMatch?> ResolveCodeAsync(string rawCode)
        {
            if (string.IsNullOrWhiteSpace(rawCode)) return null;

            string code = rawCode.Trim().ToUpperInvariant();
            if (code.Length < LobbySession.JoinCodeLength) return null;

            // 1. Check LAN beacon first
            var beacon = GetComponent<LanBeacon>() ?? FindFirstObjectByType<LanBeacon>();
            if (beacon != null)
            {
                foreach (var entry in beacon.Entries)
                {
                    if (string.Equals(entry.JoinCode, code, StringComparison.OrdinalIgnoreCase))
                    {
                        return new ResolvedMatch
                        {
                            Found = true,
                            IsLan = true,
                            Address = entry.Address,
                            Port = entry.Port,
                            JoinCode = entry.JoinCode,
                            HostName = entry.HostName,
                            Seated = entry.Players,
                            Occupied = entry.Players,
                            MaxPlayers = entry.MaxPlayers,
                            InProgress = entry.InProgress
                        };
                    }
                }
            }

            // 2. Query UGS Lobby by custom join code in indexed data (S1)
            try
            {
                bool authOk = await NetIdentity.EnsureSignedInAsync();
                if (!authOk) return null;

                var options = new QueryLobbiesOptions
                {
                    Count = 1,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.S1, code, QueryFilter.OpOptions.EQ)
                    }
                };

                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
                if (response?.Results != null && response.Results.Count > 0)
                {
                    var lobby = response.Results[0];
                    string relayCode = "";
                    string hostName = lobby.Name;
                    int seated = lobby.Players?.Count ?? 0;
                    int occupied = seated;
                    bool inProgress = false;

                    if (lobby.Data != null)
                    {
                        if (lobby.Data.TryGetValue("RelayCode", out var rc)) relayCode = rc.Value;
                        if (lobby.Data.TryGetValue("HostName", out var hn)) hostName = hn.Value;
                        if (lobby.Data.TryGetValue("Seated", out var s) && int.TryParse(s.Value, out int sVal)) seated = sVal;
                        if (lobby.Data.TryGetValue("Occupied", out var o) && int.TryParse(o.Value, out int oVal)) occupied = oVal;
                        if (lobby.Data.TryGetValue("InProgress", out var ip)) inProgress = ip.Value == "1";
                    }

                    return new ResolvedMatch
                    {
                        Found = true,
                        IsLan = false,
                        RelayCode = relayCode,
                        JoinCode = code,
                        HostName = hostName,
                        Seated = seated,
                        Occupied = occupied,
                        MaxPlayers = lobby.MaxPlayers,
                        InProgress = inProgress
                    };
                }
            }
            catch (Exception e)
            {
                NetIdentity.ReportServiceCallFailed("Lobby join-code lookup", e);
            }

            return null;
        }

        /// <summary>
        /// Registers a new UGS Lobby when hosting online via Relay.
        /// </summary>
        public async Task<string> CreateHostedLobbyAsync(string hostName, string joinCode, string relayCode, int seated, int occupied)
        {
            _creatingLobby = true;
            try
            {
                bool authOk = await NetIdentity.EnsureSignedInAsync();
                if (!authOk) return null;

                string lobbyName = string.IsNullOrWhiteSpace(hostName) ? "Tumbang Preso Lobby" : hostName.Trim();

                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode, DataObject.IndexOptions.S1) },
                        { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode ?? "") },
                        { "HostName", new DataObject(DataObject.VisibilityOptions.Public, lobbyName) },
                        { "Seated", new DataObject(DataObject.VisibilityOptions.Public, seated.ToString(), DataObject.IndexOptions.N1) },
                        { "Occupied", new DataObject(DataObject.VisibilityOptions.Public, occupied.ToString(), DataObject.IndexOptions.N2) },
                        { "InProgress", new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.S2) }
                    }
                };

                // ⚠️ CAPACITY IS THE SEAT COUNT, NOT THE CONNECTION COUNT, AND THE TWO ARE
                // DELIBERATELY DIFFERENT NUMBERS. The Relay allocation is sized at
                // `MaxConnections` (12) so spectators can attend a full match; a UGS lobby whose
                // `MaxPlayers` was also 12 would advertise "2/12" in the browser and would keep
                // answering the AvailableSlots filter long after all four chairs were taken.
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, LobbySession.MaxPlayers, options);
                _activeHostLobbyId = lobby.Id;
                _sinceHeartbeat = 0.0f;
                Debug.Log($"[Query] Created UGS Lobby {lobby.Id} with JoinCode {joinCode}");

                // Anything that happened while the round trip was in flight is applied now.
                if (_hasPendingCounts)
                {
                    _hasPendingCounts = false;
                    await UpdateHostedLobbyAsync(_pendingSeated, _pendingOccupied, _pendingInProgress);
                }

                return lobby.Id;
            }
            catch (Exception e)
            {
                NetIdentity.ReportServiceCallFailed("Lobby creation", e);
                return null;
            }
            finally
            {
                _creatingLobby = false;
            }
        }

        /// <summary>
        /// Updates dynamic match counts and progress state in UGS Lobby data.
        /// </summary>
        public async Task UpdateHostedLobbyAsync(int seated, int occupied, bool inProgress)
        {
            if (string.IsNullOrEmpty(_activeHostLobbyId))
            {
                // Remember the LATEST state rather than the first one, so a burst of joins and
                // leaves during creation collapses into the truth at the end of it.
                if (_creatingLobby)
                {
                    _hasPendingCounts = true;
                    _pendingSeated = seated;
                    _pendingOccupied = occupied;
                    _pendingInProgress = inProgress;
                }
                return;
            }

            try
            {
                var options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "Seated", new DataObject(DataObject.VisibilityOptions.Public, seated.ToString(), DataObject.IndexOptions.N1) },
                        { "Occupied", new DataObject(DataObject.VisibilityOptions.Public, occupied.ToString(), DataObject.IndexOptions.N2) },
                        { "InProgress", new DataObject(DataObject.VisibilityOptions.Public, inProgress ? "1" : "0", DataObject.IndexOptions.S2) }
                    }
                };

                await LobbyService.Instance.UpdateLobbyAsync(_activeHostLobbyId, options);
            }
            catch (Exception e)
            {
                NetIdentity.ReportServiceCallFailed("Lobby update", e);
            }
        }

        private async Task SendHeartbeatAsync()
        {
            if (string.IsNullOrEmpty(_activeHostLobbyId)) return;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_activeHostLobbyId);
            }
            catch (Exception e)
            {
                NetIdentity.ReportServiceCallFailed("Lobby heartbeat", e);
            }
        }

        /// <summary>
        /// Deletes the UGS Lobby on session termination so dead lobbies do not linger in browsers.
        /// </summary>
        public async Task DeleteHostedLobbyAsync()
        {
            // ⚠️ A LOBBY CAN BE CREATED AFTER THE PLAYER HAS ALREADY QUIT. Hosting online and
            // backing out again inside the creation round trip used to leave a live lobby with
            // nobody behind it, which the browser then advertised until the 30 second heartbeat
            // expiry retired it. Waiting for the creation to settle is what makes the delete
            // reach the id that is about to exist.
            while (_creatingLobby) await Task.Yield();

            if (string.IsNullOrEmpty(_activeHostLobbyId)) return;

            string id = _activeHostLobbyId;
            _activeHostLobbyId = null;
            _hasPendingCounts = false;

            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(id);
                Debug.Log($"[Query] Deleted UGS Lobby {id}");
            }
            catch (Exception e)
            {
                NetIdentity.ReportServiceCallFailed("Lobby deletion", e);
            }
        }

        private void RaiseIfChanged()
        {
            var sb = new StringBuilder();
            lock (_seen)
            {
                foreach (var e in _seen.Values)
                {
                    sb.Append($"{e.Id}:{e.Name}:{e.JoinCode}:{e.Seated}/{e.Occupied}/{e.Capacity}:{e.InProgress};");
                }
            }

            string signature = sb.ToString();
            if (signature == _lastSignature) return;

            _lastSignature = signature;
            ServersChanged?.Invoke();
        }

        private void OnDestroy()
        {
            StopBrowsing();
            _ = DeleteHostedLobbyAsync();
        }
    }
}
