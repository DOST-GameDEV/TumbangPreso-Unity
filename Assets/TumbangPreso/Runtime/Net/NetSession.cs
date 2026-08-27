using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
#if MULTIPLAY_SDK
using Unity.Services.Multiplay;
#endif
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The live network session: the adapter between the transport and the game's own
    /// bookkeeping.
    ///
    /// ⚠️⚠️ THE LOBBY LOGIC IS NOT IN HERE AND MUST NOT MOVE IN HERE. Seating, reconnection,
    /// seat reclaim, leader election and join codes live in <see cref="LobbySession"/>, which
    /// knows nothing about any transport and is therefore unit tested in milliseconds. This
    /// class is deliberately thin: it starts and stops a transport, and it forwards connection
    /// events into that bookkeeping. Every line of rules that creeps in here is a line that can
    /// only be tested with four machines.
    ///
    /// ⚠️ AND IT ANSWERS NetAuthority. Every host-authoritative path in the game already asks
    /// "do I decide this, or do I ask?" through that seam, so making multiplayer real is a
    /// matter of installing this provider rather than editing every verb.
    /// </summary>
    public sealed class NetSession : MonoBehaviour, INetProvider
    {
        public static NetSession Instance { get; private set; }

        public const int DefaultPort = LobbySession.DefaultPort;

        public LobbySession Lobby { get; } = new LobbySession();

        /// <summary>The online pool browser. Idle until a screen calls StartBrowsing.</summary>
        public ServerQuery Query { get; private set; }

        /// <summary>The local network beacon for LAN game discovery.</summary>
        public LanBeacon Beacon => _beacon;

        public event Action<string> StatusChanged;

        private NetworkManager _nm;
        private UnityTransport _utp;

        [Serializable]
        private sealed class ConnectionHello
        {
            public int Protocol;
            public string Token;
            public string Name;
        }

        /// <summary>
        /// The custom-message schema spoken by this build. This is deliberately separate from
        /// the marketing version: a peer with an older movement or ability payload cannot safely
        /// join and "mostly work". That failure presents as wrong characters, missing powers,
        /// or a frozen body, which is much worse than a clear version-mismatch refusal.
        /// </summary>
        public const int ProtocolVersion = 2;

        private const string SeatAssignmentMessage = "tp.seat.assignment.v1";
        private readonly Dictionary<ulong, ConnectionHello> _helloByClient =
            new Dictionary<ulong, ConnectionHello>();
        private bool _seatHandlerRegistered;
        private LanBeacon _beacon;
#if MULTIPLAY_SDK
        private IServerQueryHandler _serverQueryHandler;
#endif

        // INetProvider
        public bool IsHost => _nm == null || !_nm.IsListening || _nm.IsServer;
        public bool IsNetworked => _nm != null && _nm.IsListening;
        public int LocalSlot { get; private set; }
        public bool IsSeatlessReferee => _nm != null && _nm.IsServer && !_nm.IsClient;

        public string Status { get; private set; } = "offline";
        public string RelayJoinCode { get; private set; }
        public bool IsRelay { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _nm = GetComponent<NetworkManager>();
            if (_nm == null) _nm = gameObject.AddComponent<NetworkManager>();

            var rpc = GetComponent<MatchRpc>();
            if (rpc == null) rpc = gameObject.AddComponent<MatchRpc>();
            rpc.Initialize(_nm);

            _utp = GetComponent<UnityTransport>();
            if (_utp == null) _utp = gameObject.AddComponent<UnityTransport>();

            _nm.NetworkConfig ??= new NetworkConfig();
            _nm.NetworkConfig.NetworkTransport = _utp;
            _nm.NetworkConfig.ConnectionApproval = true;

            // ⚠️ SCENE MANAGEMENT OFF. The game loads its own scenes through SceneFlow, and
            // letting the netcode also drive scene loads means two systems racing to decide
            // which scene a client is in. The symptom is a client stuck on a black screen while
            // the host plays on.
            _nm.NetworkConfig.EnableSceneManagement = false;

            _beacon = GetComponent<LanBeacon>();
            if (_beacon == null) _beacon = gameObject.AddComponent<LanBeacon>();

            // ⚠️ THE ONLINE BROWSER SITS BESIDE THE LAN BEACON, NOT INSIDE IT. They answer
            // different questions: "what is on this network" and "what is on the pool", and
            // a build that cannot reach the pool must still find a game on the LAN.
            Query = GetComponent<ServerQuery>();
            if (Query == null) Query = gameObject.AddComponent<ServerQuery>();

            NetAuthority.Provider = this;

            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnected;
            _nm.ConnectionApprovalCallback += ApproveConnection;
        }

        private void OnDestroy()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientDisconnectCallback -= OnClientDisconnected;
                _nm.ConnectionApprovalCallback -= ApproveConnection;

                if (_seatHandlerRegistered && _nm.CustomMessagingManager != null)
                    _nm.CustomMessagingManager.UnregisterNamedMessageHandler(SeatAssignmentMessage);
            }

            if (Instance == this)
            {
                Instance = null;

                // ⚠️ HAND AUTHORITY BACK TO SOLO. Leaving a dead provider installed makes every
                // host-side path answer "not the host" after a disconnect, and the single
                // player game silently stops awarding points.
                NetAuthority.Provider = new SoloProvider();
            }
        }

        /// <summary>
        /// ⚠️ CREATED ON DEMAND SO SINGLE PLAYER PAYS NOTHING FOR IT. A NetworkManager that
        /// exists but never listens is harmless, but building it lazily keeps the offline game
        /// genuinely offline, which is what makes the headless probes reproducible.
        /// </summary>
        public static NetSession Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("~NetSession");
            return go.AddComponent<NetSession>();
        }

        // -------------------------------------------------------------------

        public bool StartHost(int port = DefaultPort, bool dedicated = false)
        {
            if (_nm != null && _nm.IsListening) Stop();

            Configure("0.0.0.0", port);
            ConfigureClientHello();
            IsRelay = false;
            RelayJoinCode = null;

            Lobby.IsDedicated = dedicated;
            Lobby.OpenLobby(new System.Random(Environment.TickCount));

            if (dedicated)
            {
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }

            bool ok = dedicated ? _nm.StartServer() : _nm.StartHost();
            SetStatus(ok
                ? $"hosting on {port}, join code {Lobby.JoinCode}"
                : "failed to start hosting");

            if (ok)
            {
                LocalSlot = dedicated ? -1 : 0;

                _beacon.HostName = Settings.SettingsStore.Current.PlayerName;
                _beacon.JoinCode = Lobby.JoinCode;
                _beacon.Port = port;
                _beacon.InProgress = false;
                PublishLobbyCounts();
                _beacon.StartAdvertising();

                if (dedicated)
                {
                    _ = StartMultiplayServerAsync(port);
                }
            }

            return ok;
        }

        private void Update()
        {
#if MULTIPLAY_SDK
            if (_serverQueryHandler != null)
            {
                _serverQueryHandler.CurrentPlayers = (ushort)Lobby.SeatedPeerCount();
                _serverQueryHandler.MaxPlayers = (ushort)LobbySession.MaxPlayers;
                _serverQueryHandler.Map = UI.SceneFlow.SelectedMap;
                _serverQueryHandler.UpdateServerCheck();
            }
#endif
        }

        /// <summary>
        /// Registers this process with the Multiplay fleet and starts answering SQP queries.
        /// </summary>
        // ⚠⚠ DEFERRED, 2026-08-20, NOT IN PROGRESS. This superseded the E.2 decision
        // (`Unity_UGS_Networking_Prompts.md`, 2026-08-19) that chose Multiplay Hosting over the
        // Singapore VPS: com.unity.services.multiplay cannot be installed on Unity 6000.5 at all.
        // Every published version of it, 1.1.1 through 1.3.1, ships
        // Editor/Authoring/Assets/CreateMultiplayConfigMenu.cs, which calls EndNameEditAction.
        // Unity 6000.5 marks that obsolete as an ERROR rather than a warning, so the package's
        // authoring assembly fails to build and takes the whole project's compile down with it.
        // The package was therefore removed from the manifest.
        //
        // ⚠ Checked, not assumed, that the consolidated package does not rescue this.
        // com.unity.services.multiplayer@2.3.0 does ship a Server/ assembly, but its contents are
        // Sessions and Matchmaker server support (MultiplayerServerService,
        // MatchmakerServerExtensions). There is no ServerQueryHandler, no allocation callback
        // surface, and no server.json reader. SQP and fleet allocation both still need the
        // blocked package.
        //
        // ⚠ RELAY PEER HOSTING IS NOW THE PRIMARY ONLINE PATH, not a fallback behind this. See
        // NetSession.StartHost's Relay branch and Port_Plan.md Phase 5. The body below is gated
        // on MULTIPLAY_SDK, defined nowhere, kept rather than deleted because the fleet path is
        // real shipped behaviour. Two ways back, whichever lands first: Unity publishes a
        // multiplay build that compiles on 6.5, in which case re-add the package and define
        // MULTIPLAY_SDK and nothing else changes; or this is re-ported onto
        // MultiplayerServerService.CreateSessionAsync, which is Unity's actual replacement API but
        // exposes sessions rather than a ServerQueryHandler, so the SQP heartbeat in Update
        // becomes the service's job rather than ours. That port needs a real fleet to verify, so
        // it is deliberately not guessed at here. The dedicated Linux server build is unaffected
        // either way: it still serves clients today, it just does not register with a fleet.
        public async Task StartMultiplayServerAsync(int port, string serverName = "Tumbang Preso Dedicated", string map = "Eskinita")
        {
#if !MULTIPLAY_SDK
            Debug.LogWarning(
                "[Net] Multiplay fleet registration skipped: com.unity.services.multiplay is not " +
                "installed because it does not compile on Unity 6000.5. Dedicated hosting still " +
                "serves clients, it just does not report itself to the fleet.");
            SetStatus($"dedicated server on port {port} (fleet registration unavailable)");
            await Task.CompletedTask;
#else
            try
            {
                await Unity.Services.Core.UnityServices.InitializeAsync();
                var serverConfig = MultiplayService.Instance.ServerConfig;
                ushort serverPort = serverConfig != null && serverConfig.Port != 0 ? serverConfig.Port : (ushort)port;
                ushort queryPort = serverConfig != null && serverConfig.QueryPort != 0 ? serverConfig.QueryPort : (ushort)(port + 1);

                _serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
                    (ushort)LobbySession.MaxPlayers,
                    serverName,
                    "TumbangPreso",
                    Application.version,
                    map);

                await MultiplayService.Instance.ReadyServerForPlayersAsync();
                SetStatus($"multiplay server ready on port {serverPort} (query {queryPort})");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Multiplay initialization skipped/failed: {e.Message}");
            }
#endif
        }

        public bool StartClient(string address, int port = DefaultPort)
        {
            if (_nm != null && _nm.IsListening) Stop();

            Configure(address, port);
            ConfigureClientHello();
            IsRelay = false;
            RelayJoinCode = null;

            bool ok = _nm.StartClient();
            if (ok) RegisterSeatHandler();
            SetStatus(ok ? $"connecting to {address}:{port}" : "failed to connect");
            return ok;
        }

        /// <summary>
        /// Allocates a Relay session and starts hosting through Unity Transport.
        ///
        /// ⚠️ CAPACITY IS MaxConnections (12), NOT MaxPlayers (4). Four seats is a rules
        /// constraint, twelve connections is a capacity ceiling so spectators can join a full game.
        ///
        /// ⚠️ JOIN CODE PRESERVATION. The game's 4-character confusable-free join code is
        /// generated in LobbySession as usual; the UGS Relay join code is an internal transport
        /// handle mapped behind this session.
        /// </summary>
        public async Task<bool> StartRelayHost(int maxConnections = LobbySession.MaxConnections)
        {
            if (_nm != null && _nm.IsListening) Stop();

            SetStatus("signing in to online services...");

            // ⚠ NO FALLBACK HERE, DELIBERATELY. Relay needs a real signed-in session, so there
            // is nothing to degrade to: the local token works for LAN and cannot allocate a
            // relay. The status now carries which of the three situations stopped it rather
            // than the single "authentication failed" that covered all of them.
            bool authOk = await NetIdentity.EnsureSignedInAsync();
            if (!authOk)
            {
                SetStatus($"cannot go online: {NetIdentity.StateReason}");
                return false;
            }

            SetStatus("allocating relay server...");
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                RelayJoinCode = relayCode;
                IsRelay = true;

                // ⚠ Was new RelayServerData(allocation, "dtls"). That constructor belonged to
                // com.unity.transport 1.x and exists in none of the packages this project resolves
                // today. The conversion now lives in Unity.Services.Relay.Models.AllocationUtils,
                // shipped by com.unity.services.multiplayer, as an extension method.
                var relayServerData = allocation.ToRelayServerData("dtls");
                _utp.SetRelayServerData(relayServerData);

                Lobby.IsDedicated = false;
                Lobby.OpenLobby(new System.Random(Environment.TickCount));
                ConfigureClientHello();

                bool ok = _nm.StartHost();
                SetStatus(ok
                    ? $"relay hosting active, code {Lobby.JoinCode} (relay {relayCode})"
                    : "failed to start relay host");

                if (ok)
                {
                    LocalSlot = 0;
                    _beacon.HostName = Settings.SettingsStore.Current.PlayerName;
                    _beacon.JoinCode = Lobby.JoinCode;
                    _beacon.Port = DefaultPort;
                    _beacon.InProgress = false;
                    PublishLobbyCounts();
                    _beacon.StartAdvertising();

                    if (Query != null)
                    {
                        _ = Query.CreateHostedLobbyAsync(
                            Settings.SettingsStore.Current.PlayerName,
                            Lobby.JoinCode,
                            relayCode,
                            Lobby.SeatedPeerCount(),
                            Lobby.OccupiedSeatCount());
                    }
                }

                if (!ok)
                {
                    IsRelay = false;
                    RelayJoinCode = null;
                }

                return ok;
            }
            catch (Exception e)
            {
                IsRelay = false;
                RelayJoinCode = null;
                SetStatus($"relay allocation failed: {e.Message}");
                NetIdentity.ReportServiceCallFailed("Relay allocation", e);
                return false;
            }
        }

        /// <summary>
        /// Connects to a host through a UGS Relay join code.
        /// </summary>
        public async Task<bool> StartRelayClient(string relayJoinCode)
        {
            if (_nm != null && _nm.IsListening) Stop();

            if (string.IsNullOrWhiteSpace(relayJoinCode))
            {
                SetStatus("invalid relay join code");
                return false;
            }

            SetStatus("signing in to online services...");

            // ⚠ NO FALLBACK HERE, DELIBERATELY. Relay needs a real signed-in session, so there
            // is nothing to degrade to: the local token works for LAN and cannot allocate a
            // relay. The status now carries which of the three situations stopped it rather
            // than the single "authentication failed" that covered all of them.
            bool authOk = await NetIdentity.EnsureSignedInAsync();
            if (!authOk)
            {
                SetStatus($"cannot go online: {NetIdentity.StateReason}");
                return false;
            }

            SetStatus($"joining relay allocation {relayJoinCode}...");
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode.Trim());
                var relayServerData = joinAllocation.ToRelayServerData("dtls");
                _utp.SetRelayServerData(relayServerData);

                IsRelay = true;
                RelayJoinCode = relayJoinCode.Trim();

                ConfigureClientHello();
                bool ok = _nm.StartClient();
                if (ok) RegisterSeatHandler();
                else
                {
                    IsRelay = false;
                    RelayJoinCode = null;
                }
                SetStatus(ok ? "connecting to relay host..." : "failed to start relay client");
                return ok;
            }
            catch (Exception e)
            {
                IsRelay = false;
                RelayJoinCode = null;
                SetStatus($"relay connection failed: {e.Message}");
                NetIdentity.ReportServiceCallFailed("Relay join", e);
                return false;
            }
        }

        public void Stop()
        {
            _beacon.StopAll();
            if (Query != null) _ = Query.DeleteHostedLobbyAsync();

            if (_nm != null && _nm.IsListening) _nm.Shutdown();

            _helloByClient.Clear();

            // ⚠️⚠️ `EndMatch` IS NOT ENOUGH HERE AND THAT IS WHY `Reset` EXISTS. `NetSession`
            // owns ONE `LobbySession` for the lifetime of the process, so host, quit to menu,
            // host again used to reach `OpenLobby` with the previous session's peer table,
            // its leader id and `MatchInProgress` still set. A brand new lobby then believed
            // it already had four players, obeyed a leader whose transport was gone, and
            // answered Spectate to the first person who tried to join it.
            Lobby.EndMatch();
            Lobby.Reset();
            Lobby.IsDedicated = false;
            LocalSlot = 0;
            IsRelay = false;
            RelayJoinCode = null;
            SetStatus("offline");
        }

        public void BrowseLan() => _beacon.StartListening();

        public System.Collections.Generic.IEnumerable<LanEntry> LanEntries => _beacon.Entries;

        private void Configure(string address, int port)
        {
            _utp.SetConnectionData(address, (ushort)port);

            // ⚠️ A GENEROUS TIMEOUT ON PURPOSE. This game is played on venue wifi and Philippine
            // home connections, and a peer briefly stalling is normal. Dropping them fast means
            // dropping them often, and a dropped seat costs the other three a real player for
            // the rest of the match.
            _utp.DisconnectTimeoutMS = 30000;
            _utp.ConnectTimeoutMS = 2000;
            _utp.MaxConnectAttempts = 12;
        }

        public void SetLocalSeating(int seat, bool spectator)
        {
            LocalSlot = seat;
            GameLaunch.Spectator = spectator;
            SetStatus($"seated in slot {seat} (spectator={spectator})");
        }

        public void SetStatusForHost()
        {
            SetStatus($"{Lobby.PeerCount} connected");
        }

        /// <summary>
        /// Republishes the three counts every browser reads: how many chairs are being PLAYED,
        /// how many are UNAVAILABLE (played plus held for a dropped peer), and how many sockets
        /// are attached.
        ///
        /// ⚠️⚠️ ONE NUMBER USED TO SERVE ALL THREE AND IT WAS `Lobby.PeerCount`. That counts
        /// connections, so two players and six spectators advertised 8/4 and the LAN browser
        /// struck the lobby out as full; the other direction, a seat held for somebody who
        /// dropped mid-match, advertised as free and then refused whoever pressed join. Both are
        /// reports of "the server browser lies", and neither is fixable while the concepts share
        /// a field. `LobbySession.OccupiedSeatCount` and `ConnectedHumanCount` are the two
        /// missing questions.
        /// </summary>
        private void PublishLobbyCounts()
        {
            if (_beacon == null) return;

            _beacon.MaxPlayers = LobbySession.MaxPlayers;
            _beacon.MaxConnections = LobbySession.MaxConnections;
            _beacon.Players = Lobby.SeatedPeerCount();
            _beacon.Occupied = Lobby.OccupiedSeatCount();
            _beacon.Connections = Lobby.ConnectedHumanCount();
            _beacon.InProgress = Lobby.MatchInProgress;

            if (Query != null && IsRelay)
            {
                _ = Query.UpdateHostedLobbyAsync(Lobby.SeatedPeerCount(),
                                                 Lobby.OccupiedSeatCount(),
                                                 Lobby.MatchInProgress);
            }
        }

        /// <summary>
        /// Put the player's durable reconnect identity into Netcode's approval payload. A
        /// NetworkClientId belongs to one transport connection and necessarily changes after a
        /// disconnect; using it as identity is what made a returning defender reappear in the
        /// attacker's old seat.
        /// </summary>
        private void ConfigureClientHello()
        {
            var settings = Settings.SettingsStore.Current;
            var hello = new ConnectionHello
            {
                Protocol = ProtocolVersion,
                Token = NetIdentity.Token,
                Name = string.IsNullOrWhiteSpace(settings.PlayerName) ? "Player" : settings.PlayerName.Trim()
            };

            _nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(hello));
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
                                       NetworkManager.ConnectionApprovalResponse response)
        {
            var hello = DecodeHello(request.Payload);
            bool protocolMatches = hello != null && hello.Protocol == ProtocolVersion;
            bool hasCapacity = _nm == null ||
                               Math.Max(_nm.ConnectedClientsIds.Count, _helloByClient.Count)
                               < LobbySession.MaxConnections;

            response.Approved = protocolMatches && hasCapacity;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = !protocolMatches
                ? $"Game version mismatch (network protocol {ProtocolVersion})"
                : hasCapacity ? string.Empty : "Lobby is full";

            if (response.Approved) _helloByClient[request.ClientNetworkId] = hello;
        }

        private static ConnectionHello DecodeHello(byte[] payload)
        {
            try
            {
                if (payload != null && payload.Length > 0)
                {
                    var decoded = JsonUtility.FromJson<ConnectionHello>(Encoding.UTF8.GetString(payload));
                    if (decoded != null && !string.IsNullOrWhiteSpace(decoded.Token))
                    {
                        decoded.Token = decoded.Token.Trim();
                        if (decoded.Token.Length > 128)
                            decoded.Token = decoded.Token.Substring(0, 128);
                        decoded.Name = string.IsNullOrWhiteSpace(decoded.Name) ? "Player" : decoded.Name.Trim();
                        return decoded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Ignoring malformed connection identity: {e.Message}");
            }

            return null;
        }

        private void RegisterSeatHandler()
        {
            if (_seatHandlerRegistered || _nm?.CustomMessagingManager == null) return;

            _nm.CustomMessagingManager.RegisterNamedMessageHandler(
                SeatAssignmentMessage, OnSeatAssignmentMessage);
            _seatHandlerRegistered = true;
        }

        private void OnSeatAssignmentMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            ApplyAssignedSeat(seat);
        }

        private void SendSeatAssignment(ulong clientId, int seat)
        {
            if (_nm == null) return;

            if (_nm.IsClient && clientId == _nm.LocalClientId)
            {
                ApplyAssignedSeat(seat);
                return;
            }

            using var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(seat);
            _nm.CustomMessagingManager.SendNamedMessage(SeatAssignmentMessage, clientId, writer);
        }

        /// <summary>Applies the host's authoritative seat on this process.</summary>
        public void ApplyAssignedSeat(int seat)
        {
            LocalSlot = seat;
            GameLaunch.Spectator = seat < 0;
            SetStatus(seat >= 0 ? $"connected as seat {seat + 1}" : "connected as spectator");
        }

        // -------------------------------------------------------------------

        private void OnClientConnected(ulong clientId)
        {
            if (!IsHost)
            {
                if (clientId == _nm.LocalClientId)
                {
                    MatchRpc.Instance?.Initialize(_nm);
                    SetStatus("connected");
                    var s = Settings.SettingsStore.Current;
                    int charPick = s.CharacterPick >= 0 ? s.CharacterPick : 0;
                    int canPick = s.CanPick >= 0 ? s.CanPick : 0;
                    int slipperPick = s.SlipperPick >= 0 ? s.SlipperPick : 0;
                    MatchRpc.Instance?.IdentifyServerRpc(NetIdentity.Token, s.PlayerName, charPick, canPick, slipperPick);
                }
                return;
            }

            MatchRpc.Instance?.Initialize(_nm);

            // ⚠️ THE HOST SEATS THEM, AND THE HOST DECIDES. A client sends its token and name;
            // where it sits is not up to it. See LobbySession.RuleOnArrival for why the branch
            // order matters: a returning player outranks a newcomer for their own seat.
            var settings = Settings.SettingsStore.Current;
            ConnectionHello hello;
            if (clientId == _nm.LocalClientId)
            {
                hello = new ConnectionHello
                {
                    Token = NetIdentity.Token,
                    Name = settings.PlayerName
                };
            }
            else if (!_helloByClient.TryGetValue(clientId, out hello))
            {
                Debug.LogWarning($"[Net] Connected client {clientId} has no approved identity; disconnecting.");
                _nm.DisconnectClient(clientId, "Missing approved identity");
                return;
            }

            var record = Lobby.Admit((int)clientId, hello.Token, hello.Name,
                                     out int replacedPeerId);
            SendSeatAssignment(clientId, record.Seat);

            // A relaunch can establish the new socket before the generous 30 second timeout
            // retires the old one. The durable token has already moved the seat above, so the
            // stale transport is now both unnecessary and dangerous: without disconnecting it,
            // it can keep submitting movement and verbs for the same player.
            if (replacedPeerId >= 0 && replacedPeerId != (int)clientId &&
                _nm.ConnectedClients.ContainsKey((ulong)replacedPeerId))
            {
                _helloByClient.Remove((ulong)replacedPeerId);
                _nm.DisconnectClient((ulong)replacedPeerId, "Replaced by reconnect");
            }

            // ⚠️ THE HOST NEVER SENDS ITSELF AN IdentifyServerRpc, so its own character, can and
            // slipper picks reach the lobby from here or not at all. Every other seat's picks
            // still arrive on the RPC.
            if (clientId == _nm.LocalClientId)
            {
                int charPick = settings.CharacterPick >= 0 ? settings.CharacterPick : 0;
                int canPick = settings.CanPick >= 0 ? settings.CanPick : 0;
                int slipperPick = settings.SlipperPick >= 0 ? settings.SlipperPick : 0;
                Lobby.SetPicks((int)clientId, charPick, canPick, slipperPick);
            }

            PublishLobbyCounts();
            MatchRpc.Instance?.BroadcastLobbyPicks();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (IsHost && clientId != _nm.LocalClientId)
            {
                // ⚠️ THE SEAT IS HELD, NOT FREED, so a reconnecting player gets their own chair
                // back rather than finding a stranger in it holding their score.
                MatchRpc.Instance?.HostPeerLeft((int)clientId);
                _helloByClient.Remove(clientId);
                PublishLobbyCounts();
                MatchRpc.Instance?.BroadcastLobbyPicks();
                SetStatus($"{Lobby.PeerCount} connected");
                return;
            }

            // ⚠️⚠️ THE REASON IS THE WHOLE POINT OF THIS BRANCH NOW. A refused approval arrives
            // here as an ordinary disconnect, so a build-version mismatch, a full lobby and a
            // host that vanished were all one word: "disconnected". The player then has nothing
            // to act on, and a version mismatch in particular is a thing they CAN fix.
            string reason = _nm != null ? _nm.DisconnectReason : null;
            SetStatus(string.IsNullOrWhiteSpace(reason) ? "disconnected" : $"disconnected: {reason}");

            // A client that has lost the host is offline, and every gameplay path asks
            // NetAuthority rather than this class. Leaving the relay flags set made the next
            // join attempt reuse a dead allocation.
            LocalSlot = 0;
            IsRelay = false;
            RelayJoinCode = null;
            _helloByClient.Clear();
            Lobby.Reset();
        }

        public void SetStatus(string s)
        {
            Status = s;
            StatusChanged?.Invoke(s);
            Debug.Log($"[Net] {s}");
        }
    }
}
