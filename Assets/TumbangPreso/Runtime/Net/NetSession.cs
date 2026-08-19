using System;
using System.Threading.Tasks;
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

        public event Action<string> StatusChanged;

        private NetworkManager _nm;
        private UnityTransport _utp;
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

            _utp = GetComponent<UnityTransport>();
            if (_utp == null) _utp = gameObject.AddComponent<UnityTransport>();

            _nm.NetworkConfig ??= new NetworkConfig();
            _nm.NetworkConfig.NetworkTransport = _utp;

            // ⚠️ SCENE MANAGEMENT OFF. The game loads its own scenes through SceneFlow, and
            // letting the netcode also drive scene loads means two systems racing to decide
            // which scene a client is in. The symptom is a client stuck on a black screen while
            // the host plays on.
            _nm.NetworkConfig.EnableSceneManagement = false;

            _beacon = GetComponent<LanBeacon>();
            if (_beacon == null) _beacon = gameObject.AddComponent<LanBeacon>();

            // ⚠️ THE ONLINE BROWSER SITS BESIDE THE LAN BEACON, NOT INSIDE IT. They answer
            // different questions — "what is on this network" and "what is on the pool" — and
            // a build that cannot reach the pool must still find a game on the LAN.
            Query = GetComponent<ServerQuery>();
            if (Query == null) Query = gameObject.AddComponent<ServerQuery>();

            NetAuthority.Provider = this;

            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientDisconnectCallback -= OnClientDisconnected;
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
            Configure("0.0.0.0", port);
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
                _beacon.MaxPlayers = LobbySession.MaxPlayers;
                _beacon.Players = 1;
                _beacon.InProgress = false;
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
        // ⚠ The body is gated on MULTIPLAY_SDK, which is currently defined nowhere, because
        // com.unity.services.multiplay cannot be installed on Unity 6000.5 at all. Every
        // published version of it, 1.1.1 through 1.3.1, ships
        // Editor/Authoring/Assets/CreateMultiplayConfigMenu.cs, which calls EndNameEditAction.
        // Unity 6000.5 marks that obsolete as an ERROR rather than a warning, so the package's
        // authoring assembly fails to build and takes the whole project's compile down with it.
        // The package was therefore removed from the manifest.
        //
        // ⚠ The code is kept rather than deleted because the fleet path is real shipped
        // behaviour, not dead code. Two ways back, whichever lands first: Unity publishes a
        // multiplay build that compiles on 6.5, in which case re-add the package and define
        // MULTIPLAY_SDK and nothing else changes; or this is re-ported onto the session API in
        // com.unity.services.multiplayer 2.x, which is Unity's stated replacement but exposes
        // MultiplayerServerService.CreateSessionAsync instead of a ServerQueryHandler, so the
        // SQP heartbeat in Update becomes the service's job rather than ours. That port needs a
        // real fleet to verify, so it is deliberately not guessed at here.
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
            Configure(address, port);
            IsRelay = false;
            RelayJoinCode = null;

            bool ok = _nm.StartClient();
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
            SetStatus("signing in to online services...");
            bool authOk = await NetIdentity.EnsureSignedInAsync();
            if (!authOk)
            {
                SetStatus("online authentication failed");
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
                    _beacon.MaxPlayers = LobbySession.MaxPlayers;
                    _beacon.Players = 1;
                    _beacon.InProgress = false;
                    _beacon.StartAdvertising();

                    if (Query != null)
                    {
                        _ = Query.CreateHostedLobbyAsync(
                            Settings.SettingsStore.Current.PlayerName,
                            Lobby.JoinCode,
                            relayCode,
                            Lobby.SeatedPeerCount(),
                            Lobby.PeerCount);
                    }
                }

                return ok;
            }
            catch (Exception e)
            {
                SetStatus($"relay allocation failed: {e.Message}");
                Debug.LogWarning($"[Net] Relay allocation exception: {e}");
                return false;
            }
        }

        /// <summary>
        /// Connects to a host through a UGS Relay join code.
        /// </summary>
        public async Task<bool> StartRelayClient(string relayJoinCode)
        {
            if (string.IsNullOrWhiteSpace(relayJoinCode))
            {
                SetStatus("invalid relay join code");
                return false;
            }

            SetStatus("signing in to online services...");
            bool authOk = await NetIdentity.EnsureSignedInAsync();
            if (!authOk)
            {
                SetStatus("online authentication failed");
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

                bool ok = _nm.StartClient();
                SetStatus(ok ? "connecting to relay host..." : "failed to start relay client");
                return ok;
            }
            catch (Exception e)
            {
                SetStatus($"relay connection failed: {e.Message}");
                Debug.LogWarning($"[Net] Relay join exception: {e}");
                return false;
            }
        }

        public void Stop()
        {
            _beacon.StopAll();
            if (Query != null) _ = Query.DeleteHostedLobbyAsync();

            if (_nm != null && _nm.IsListening) _nm.Shutdown();

            Lobby.EndMatch();
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

        // -------------------------------------------------------------------

        private void OnClientConnected(ulong clientId)
        {
            if (!IsHost)
            {
                if (clientId == _nm.LocalClientId) SetStatus("connected");
                return;
            }

            // ⚠️ THE HOST SEATS THEM, AND THE HOST DECIDES. A client sends its token and name;
            // where it sits is not up to it. See LobbySession.RuleOnArrival for why the branch
            // order matters: a returning player outranks a newcomer for their own seat.
            var s = Settings.SettingsStore.Current;
            string token = clientId == _nm.LocalClientId
                ? NetIdentity.Token
                : $"{NetIdentity.LocalToken}_peer_{clientId}";
            var record = Lobby.Admit((int)clientId, token, s.PlayerName);

            _beacon.Players = Lobby.PeerCount;
            if (Query != null && IsRelay)
            {
                _ = Query.UpdateHostedLobbyAsync(Lobby.SeatedPeerCount(), Lobby.PeerCount, Lobby.MatchInProgress);
            }
            MatchRpc.Instance?.HostLateJoin((int)clientId);
            SetStatus($"{Lobby.PeerCount} connected, seat {record.Seat}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (IsHost)
            {
                // ⚠️ THE SEAT IS HELD, NOT FREED, so a reconnecting player gets their own chair
                // back rather than finding a stranger in it holding their score.
                MatchRpc.Instance?.HostPeerLeft((int)clientId);
                _beacon.Players = Lobby.PeerCount;
                if (Query != null && IsRelay)
                {
                    _ = Query.UpdateHostedLobbyAsync(Lobby.SeatedPeerCount(), Lobby.PeerCount, Lobby.MatchInProgress);
                }
                SetStatus($"{Lobby.PeerCount} connected");
                return;
            }

            SetStatus("disconnected");
        }

        private void SetStatus(string s)
        {
            Status = s;
            StatusChanged?.Invoke(s);
            Debug.Log($"[Net] {s}");
        }
    }
}
