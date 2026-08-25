using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The gameplay RPC and message transport, converted from the @rpc surface spread across
    /// carrier.gd, character_base.gd and main.gd.
    ///
    /// ⚠️ EVERY VERB IS A REQUEST TO THE HOST, NEVER A LOCAL RESOLUTION. A client that
    /// resolved its own tag would be authoritative over somebody else's stun. The pattern is
    /// always the same: the client asks, the host decides using the same rule the solo game
    /// uses, and the host broadcasts what happened. That is why NetAuthority.ShouldResolve
    /// exists and why nothing here calls a gameplay method directly.
    ///
    /// ⚠️ POSITION AND FACING TRAVEL WITH THE REQUEST. The host must judge the verb against
    /// where the client believed it was standing, not where the host currently thinks it is,
    /// otherwise every lunge is judged a frame or two late and misses on a lagged connection
    /// while looking like a direct hit on the client's screen.
    ///
    /// ⚠️ AND THE VISUAL HALF IS SEPARATE FROM THE RESOLUTION HALF. A charge-up read
    /// broadcasts on its own because the other players need to see a wind-up
    /// before it resolves; folding it into the result would show the tell and the tag on the
    /// same frame, which removes the only warning the game gives.
    /// </summary>
    public sealed class MatchRpc : MonoBehaviour
    {
        public static MatchRpc Instance { get; private set; }

        private NetworkManager _nm;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(NetworkManager nm)
        {
            _nm = nm;
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            var cm = _nm.CustomMessagingManager;

            cm.RegisterNamedMessageHandler("Identify", OnIdentifyMsg);
            cm.RegisterNamedMessageHandler("Seating", OnSeatingMsg);
            cm.RegisterNamedMessageHandler("DeclareReady", OnDeclareReadyMsg);
            cm.RegisterNamedMessageHandler("BeginCountdown", OnBeginCountdownMsg);
            cm.RegisterNamedMessageHandler("SyncMap", OnSyncMapMsg);
            cm.RegisterNamedMessageHandler("SelectMap", OnSelectMapMsg);
            cm.RegisterNamedMessageHandler("SyncDiff", OnSyncDiffMsg);
            cm.RegisterNamedMessageHandler("SelectDiff", OnSelectDiffMsg);
            cm.RegisterNamedMessageHandler("SyncLobbyPicks", OnSyncLobbyPicksMsg);
            cm.RegisterNamedMessageHandler("SelectLobbyPick", OnSelectLobbyPickMsg);
            cm.RegisterNamedMessageHandler("SyncPicks", OnSyncPicksMsg);
            cm.RegisterNamedMessageHandler("SyncWorld", OnSyncWorldMsg);
            cm.RegisterNamedMessageHandler("SyncLata", OnSyncLataMsg);
            cm.RegisterNamedMessageHandler("SyncSlipper", OnSyncSlipperMsg);
            cm.RegisterNamedMessageHandler("SubmitMove", OnSubmitMoveMsg);
            cm.RegisterNamedMessageHandler("SyncUnit", OnSyncUnitMsg);
            cm.RegisterNamedMessageHandler("ReqPunch", OnReqPunchMsg);
            cm.RegisterNamedMessageHandler("ReqLunge", OnReqLungeMsg);
            cm.RegisterNamedMessageHandler("ReqShove", OnReqShoveMsg);
            cm.RegisterNamedMessageHandler("LungeCharge", OnLungeChargeMsg);
            cm.RegisterNamedMessageHandler("ShoveCharge", OnShoveChargeMsg);
            cm.RegisterNamedMessageHandler("ReqGrab", OnReqGrabMsg);
            cm.RegisterNamedMessageHandler("ReqThrow", OnReqThrowMsg);
            cm.RegisterNamedMessageHandler("ReqReset", OnReqResetMsg);
            cm.RegisterNamedMessageHandler("ReqEmote", OnReqEmoteMsg);
            cm.RegisterNamedMessageHandler("PlayEmote", OnPlayEmoteMsg);
            cm.RegisterNamedMessageHandler("StartMatch", OnStartMatchMsg);
        }

        private static CharacterMotor Unit(int slot)
        {
            var round = GameServices.Round;
            return round != null ? round.PlayerAt(slot) : null;
        }

        private static Slipper FindSlipper(int ownerSlot)
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s.OwnerSlot == ownerSlot) return s;

            return null;
        }

        // -------------------------------------------------------------------
        // IDENTITY AND SEATING
        // -------------------------------------------------------------------

        public void IdentifyServerRpc(string token, string name, int charPick, int canPick, int slipperPick)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (NetAuthority.IsHost)
            {
                HandleIdentify(0, token, name, charPick, canPick, slipperPick);
                return;
            }

            using var writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(token ?? "");
            writer.WriteValueSafe(name ?? "");
            writer.WriteValueSafe(charPick);
            writer.WriteValueSafe(canPick);
            writer.WriteValueSafe(slipperPick);
            _nm.CustomMessagingManager.SendNamedMessage("Identify", NetworkManager.ServerClientId, writer);
        }

        private void OnIdentifyMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out string token);
            reader.ReadValueSafe(out string name);
            reader.ReadValueSafe(out int charPick);
            reader.ReadValueSafe(out int canPick);
            reader.ReadValueSafe(out int slipperPick);

            HandleIdentify(senderClientId, token, name, charPick, canPick, slipperPick);
        }

        private void HandleIdentify(ulong senderClientId, string token, string name, int charPick, int canPick, int slipperPick)
        {
            int peerId = (int)senderClientId;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return;

            var record = lobby.Admit(peerId, token, name);
            if (charPick >= 0)
            {
                lobby.SetPicks(peerId, charPick, canPick, slipperPick);
            }

            if (senderClientId != _nm.LocalClientId)
            {
                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(record.Seat);
                writer.WriteValueSafe(record.Spectator);
                writer.WriteValueSafe(lobby.LeaderPeerId);
                writer.WriteValueSafe(lobby.MatchInProgress);
                writer.WriteValueSafe(lobby.JoinCode ?? "");
                _nm.CustomMessagingManager.SendNamedMessage("Seating", senderClientId, writer);
            }

            NetSession.Instance?.SetStatus($"{lobby.PeerCount} connected, seat {record.Seat}");

            HostLateJoin(peerId);
            BroadcastLobbyPicks();
            BroadcastPicks();
            BroadcastWorldSnapshot();
        }

        private void OnSeatingMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out bool spectator);
            reader.ReadValueSafe(out int leaderId);
            reader.ReadValueSafe(out bool inProgress);
            string joinCode = "";
            if (reader.Length > reader.Position)
            {
                reader.ReadValueSafe(out joinCode);
            }

            var net = NetSession.Instance;
            if (net != null)
            {
                if (!string.IsNullOrEmpty(joinCode))
                {
                    net.Lobby.SetJoinCode(joinCode);
                }
                net.SetLocalSeating(seat, spectator);
            }

            if (inProgress && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != UI.SceneFlow.SelectedMap)
            {
                UI.SceneFlow.StartMatch();
            }
            else
            {
                var installer = FindFirstObjectByType<MatchInstaller>();
                installer?.RebindLocalSeat(seat, spectator);
            }
        }

        // -------------------------------------------------------------------
        // THE READY GATE
        // -------------------------------------------------------------------

        public void DeclareReadyServerRpc(int peerId)
        {
            if (NetAuthority.IsHost)
            {
                FindFirstObjectByType<ReadyGate>()?.DeclareReady(peerId);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(peerId);
            _nm.CustomMessagingManager.SendNamedMessage("DeclareReady", NetworkManager.ServerClientId, writer);
        }

        private void OnDeclareReadyMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            FindFirstObjectByType<ReadyGate>()?.DeclareReady((int)senderClientId);
        }

        public void BeginCountdownClientRpc()
        {
            if (!NetAuthority.IsHost) return;

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                _nm.CustomMessagingManager.SendNamedMessageToAll("BeginCountdown", writer);
            }
        }

        private void OnBeginCountdownMsg(ulong senderClientId, FastBufferReader reader)
        {
            FindFirstObjectByType<ReadyGate>()?.StartLocalCountdown();
        }

        // -------------------------------------------------------------------
        // MOVEMENT AND POSITION SYNCHRONIZATION
        // -------------------------------------------------------------------

        public void SubmitMoveServerRpc(int slot, Vector3 pos, float yaw, Vector3 velocity)
        {
            if (NetAuthority.IsHost)
            {
                ApplyUnitMove(slot, pos, yaw, velocity);
                SyncUnitTransformClientRpc(slot, pos, yaw, velocity);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(pos);
            writer.WriteValueSafe(yaw);
            writer.WriteValueSafe(velocity);
            _nm.CustomMessagingManager.SendNamedMessage("SubmitMove", NetworkManager.ServerClientId, writer);
        }

        private void OnSubmitMoveMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out float yaw);
            reader.ReadValueSafe(out Vector3 velocity);

            ApplyUnitMove(slot, pos, yaw, velocity);
            SyncUnitTransformClientRpc(slot, pos, yaw, velocity);
        }

        private static void ApplyUnitMove(int slot, Vector3 pos, float yaw, Vector3 velocity)
        {
            var unit = Unit(slot);
            if (unit == null) return;

            var cc = unit.GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                cc.enabled = false;
                unit.transform.position = pos;
                unit.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                cc.enabled = true;
            }
            else
            {
                unit.transform.position = pos;
                unit.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        public void SyncUnitTransformClientRpc(int slot, Vector3 pos, float yaw, Vector3 velocity)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(pos);
            writer.WriteValueSafe(yaw);
            writer.WriteValueSafe(velocity);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncUnit", writer);
        }

        private void OnSyncUnitMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out float yaw);
            reader.ReadValueSafe(out Vector3 velocity);

            if (slot == NetAuthority.LocalSlot) return;

            ApplyUnitMove(slot, pos, yaw, velocity);
        }

        // -------------------------------------------------------------------
        // COMBAT VERBS
        // -------------------------------------------------------------------

        public void RequestPunchServerRpc(int slot, Vector3 from, Vector3 facing)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && who.IsDefender)
                {
                    who.GetComponent<CombatVerbs>()?.HostResolvePunch(from, facing);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(from);
            writer.WriteValueSafe(facing);
            _nm.CustomMessagingManager.SendNamedMessage("ReqPunch", NetworkManager.ServerClientId, writer);
        }

        private void OnReqPunchMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 from);
            reader.ReadValueSafe(out Vector3 facing);

            var who = Unit(slot);
            if (who != null && who.IsDefender)
            {
                who.GetComponent<CombatVerbs>()?.HostResolvePunch(from, facing);
            }
        }

        public void RequestLungeServerRpc(int slot, Vector3 from, Vector3 facing, float power)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && who.IsDefender)
                {
                    who.GetComponent<CombatVerbs>()?.HostResolveLunge(from, facing, power);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(from);
            writer.WriteValueSafe(facing);
            writer.WriteValueSafe(power);
            _nm.CustomMessagingManager.SendNamedMessage("ReqLunge", NetworkManager.ServerClientId, writer);
        }

        private void OnReqLungeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 from);
            reader.ReadValueSafe(out Vector3 facing);
            reader.ReadValueSafe(out float power);

            var who = Unit(slot);
            if (who != null && who.IsDefender)
            {
                who.GetComponent<CombatVerbs>()?.HostResolveLunge(from, facing, power);
            }
        }

        public void RequestShoveServerRpc(int slot, Vector3 from, Vector3 facing)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && !who.IsDefender)
                {
                    who.GetComponent<CombatVerbs>()?.HostResolveShove(from, facing);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(from);
            writer.WriteValueSafe(facing);
            _nm.CustomMessagingManager.SendNamedMessage("ReqShove", NetworkManager.ServerClientId, writer);
        }

        private void OnReqShoveMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 from);
            reader.ReadValueSafe(out Vector3 facing);

            var who = Unit(slot);
            if (who != null && !who.IsDefender)
            {
                who.GetComponent<CombatVerbs>()?.HostResolveShove(from, facing);
            }
        }

        public void LungeChargeServerRpc(int slot, bool active)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(active);
                _nm.CustomMessagingManager.SendNamedMessageToAll("LungeCharge", writer);
            }
        }

        private void OnLungeChargeMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out bool active);
            Unit(slot)?.GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction(active ? "lunge" : null);
        }

        public void ShoveChargeServerRpc(int slot, bool active)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(active);
                _nm.CustomMessagingManager.SendNamedMessageToAll("ShoveCharge", writer);
            }
        }

        private void OnShoveChargeMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out bool active);
            Unit(slot)?.GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction(active ? "shove" : null);
        }

        public void RequestGrabServerRpc(int slot, int slipperOwnerSlot)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                var slipper = FindSlipper(slipperOwnerSlot);
                if (who != null && slipper != null && slipper.CanBeGrabbedBy(who))
                {
                    who.GetComponent<Carrier>()?.HostPickUp(slipper);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(slipperOwnerSlot);
            _nm.CustomMessagingManager.SendNamedMessage("ReqGrab", NetworkManager.ServerClientId, writer);
        }

        private void OnReqGrabMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out int slipperOwnerSlot);

            var who = Unit(slot);
            var slipper = FindSlipper(slipperOwnerSlot);
            if (who != null && slipper != null && slipper.CanBeGrabbedBy(who))
            {
                who.GetComponent<Carrier>()?.HostPickUp(slipper);
            }
        }

        public void RequestThrowServerRpc(int slot, Vector3 origin, Vector3 aimPoint, float charge)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                var carrier = who != null ? who.GetComponent<Carrier>() : null;
                if (carrier != null && carrier.Held != null && GameServices.Round != null && GameServices.Round.CanThrow(who))
                {
                    carrier.HostThrowAt(origin, aimPoint, charge);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(origin);
            writer.WriteValueSafe(aimPoint);
            writer.WriteValueSafe(charge);
            _nm.CustomMessagingManager.SendNamedMessage("ReqThrow", NetworkManager.ServerClientId, writer);
        }

        private void OnReqThrowMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 origin);
            reader.ReadValueSafe(out Vector3 aimPoint);
            reader.ReadValueSafe(out float charge);

            var who = Unit(slot);
            var carrier = who != null ? who.GetComponent<Carrier>() : null;
            if (carrier != null && carrier.Held != null && GameServices.Round != null && GameServices.Round.CanThrow(who))
            {
                carrier.HostThrowAt(origin, aimPoint, charge);
            }
        }

        public void RequestResetServerRpc(int slot)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && who.IsDefender)
                {
                    GameServices.Round?.Lata?.HostRestore();
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(slot);
            _nm.CustomMessagingManager.SendNamedMessage("ReqReset", NetworkManager.ServerClientId, writer);
        }

        private void OnReqResetMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            var who = Unit(slot);
            if (who != null && who.IsDefender)
            {
                GameServices.Round?.Lata?.HostRestore();
            }
        }

        public void RequestEmoteServerRpc(int slot, string id)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                var player = who != null ? who.GetComponent<Social.EmotePlayer>() : null;
                if (player != null && player.CanEmote())
                {
                    PlayEmoteClientRpc(slot, id);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(id ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("ReqEmote", NetworkManager.ServerClientId, writer);
        }

        private void OnReqEmoteMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out string id);

            var who = Unit(slot);
            var player = who != null ? who.GetComponent<Social.EmotePlayer>() : null;
            if (player != null && player.CanEmote())
            {
                PlayEmoteClientRpc(slot, id);
            }
        }

        private void PlayEmoteClientRpc(int slot, string id)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(id ?? "");
                _nm.CustomMessagingManager.SendNamedMessageToAll("PlayEmote", writer);
            }
        }

        private void OnPlayEmoteMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out string id);
            Unit(slot)?.GetComponent<Social.EmotePlayer>()?.Play(id);
        }

        // -------------------------------------------------------------------
        // LOBBY SETUP SYNCHRONIZATION (N5)
        // -------------------------------------------------------------------

        public static event Action<int> OnMapChanged;
        public static event Action<int> OnDifficultyChanged;
        public static event Action<int[]> OnLobbyPicksSynced;
        public static event Action OnMatchStarted;

        public void HostStartMatch()
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                _nm.CustomMessagingManager.SendNamedMessageToAll("StartMatch", writer);
            }
            OnMatchStarted?.Invoke();
        }

        private void OnStartMatchMsg(ulong senderClientId, FastBufferReader reader)
        {
            OnMatchStarted?.Invoke();
            UI.SceneFlow.StartMatch();
        }

        public void SelectMapServerRpc(int mapIndex)
        {
            if (NetAuthority.IsHost)
            {
                SyncMapClientRpc(mapIndex);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(mapIndex);
            _nm.CustomMessagingManager.SendNamedMessage("SelectMap", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectMapMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int mapIndex);
            SyncMapClientRpc(mapIndex);
        }

        private void SyncMapClientRpc(int mapIndex)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(mapIndex);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncMap", writer);
            }
            OnMapChanged?.Invoke(mapIndex);
        }

        private void OnSyncMapMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int mapIndex);
            OnMapChanged?.Invoke(mapIndex);
        }

        public void SelectDifficultyServerRpc(int difficulty)
        {
            if (NetAuthority.IsHost)
            {
                SyncDifficultyClientRpc(difficulty);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(difficulty);
            _nm.CustomMessagingManager.SendNamedMessage("SelectDiff", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectDiffMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int diff);
            SyncDifficultyClientRpc(diff);
        }

        private void SyncDifficultyClientRpc(int difficulty)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(difficulty);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncDiff", writer);
            }
            OnDifficultyChanged?.Invoke(difficulty);
        }

        private void OnSyncDiffMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int diff);
            OnDifficultyChanged?.Invoke(diff);
        }

        public void SelectLobbyPickServerRpc(int peerId, int character, int can, int slipper)
        {
            if (NetAuthority.IsHost)
            {
                var lobby = NetSession.Instance?.Lobby;
                if (lobby != null)
                {
                    lobby.SetPicks(peerId, character, can, slipper);
                    BroadcastLobbyPicks();
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(peerId);
            writer.WriteValueSafe(character);
            writer.WriteValueSafe(can);
            writer.WriteValueSafe(slipper);
            _nm.CustomMessagingManager.SendNamedMessage("SelectLobbyPick", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectLobbyPickMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int peerId);
            reader.ReadValueSafe(out int character);
            reader.ReadValueSafe(out int can);
            reader.ReadValueSafe(out int slipper);

            var lobby = NetSession.Instance?.Lobby;
            if (lobby != null)
            {
                lobby.SetPicks((int)senderClientId, character, can, slipper);
                BroadcastLobbyPicks();
            }
        }

        public void BroadcastLobbyPicks()
        {
            if (!NetAuthority.IsHost) return;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return;

            var table = new int[Balance.PlayerCount * 4];
            for (int i = 0; i < table.Length; i++) table[i] = -1;

            foreach (var peer in lobby.Peers)
            {
                if (peer.Seat >= 0 && peer.Seat < Balance.PlayerCount)
                {
                    table[peer.Seat * 4] = peer.Seat;
                    table[peer.Seat * 4 + 1] = peer.CharacterPick;
                    table[peer.Seat * 4 + 2] = peer.CanPick;
                    table[peer.Seat * 4 + 3] = peer.SlipperPick;
                }
            }

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(table);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncLobbyPicks", writer);
            }

            OnLobbyPicksSynced?.Invoke(table);
        }

        private void OnSyncLobbyPicksMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int[] table);
            OnLobbyPicksSynced?.Invoke(table);
        }

        // -------------------------------------------------------------------
        // PICKS SYNCHRONIZATION
        // -------------------------------------------------------------------

        public void SyncPicksClientRpc(int[] table)
        {
            if (table == null) return;

            var book = RosterBook.Load();

            for (int i = 0; i + 3 < table.Length; i += 4)
            {
                int slot = table[i];
                int charIndex = table[i + 1];

                var who = Unit(slot);
                if (who == null) continue;

                if (charIndex >= 0 && who.CharacterIndex != charIndex)
                {
                    who.CharacterIndex = charIndex;
                    var person = book != null ? book.PersonArt(charIndex) : null;
                    if (person != null)
                    {
                        var vis = who.GetComponent<Visual.CharacterVisual>();
                        vis?.ApplyModel(person.Model, person.Tint, person.Clips, person.Palette);
                    }
                    if (charIndex < Roster.People.Count)
                    {
                        who.PlayerName = Roster.People[charIndex].Name;
                    }
                }

                ApplySlipperSkin(slot, table[i + 3]);
            }
        }

        public void BroadcastPicks()
        {
            if (!NetAuthority.IsHost) return;

            var round = GameServices.Round;
            if (round == null) return;

            var table = new int[Balance.PlayerCount * 4];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var who = round.PlayerAt(slot);

                table[slot * 4] = slot;
                table[slot * 4 + 1] = who != null ? who.CharacterIndex : -1;
                table[slot * 4 + 2] = SkinOfLataFor(slot);
                table[slot * 4 + 3] = SkinOfSlipperFor(slot);
            }

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(table);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncPicks", writer);
            }

            SyncPicksClientRpc(table);
        }

        private void OnSyncPicksMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int[] table);
            SyncPicksClientRpc(table);
        }

        private static int SkinOfLataFor(int slot)
        {
            var lata = GameServices.Round?.Lata;
            return lata != null && GameServices.Match?.DefenderSlot == slot ? lata.SkinIndex : -1;
        }

        private static int SkinOfSlipperFor(int slot)
        {
            var s = FindSlipper(slot);
            return s != null ? s.SkinIndex : -1;
        }

        private static void ApplySlipperSkin(int slot, int skin)
        {
            var s = FindSlipper(slot);
            if (s != null && skin >= 0) s.SkinIndex = skin;
        }

        // -------------------------------------------------------------------
        // PROP AND WORLD STATE REPLICATION
        // -------------------------------------------------------------------

        public void SyncLataClientRpc(Vector3 pos, Quaternion rot, bool isUpright, int skinIndex)
        {
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;

            lata.transform.position = pos;
            lata.transform.rotation = rot;
            lata.SkinIndex = skinIndex;
        }

        public void SyncSlipperClientRpc(int ownerSlot, int holderSlot, Vector3 pos, Quaternion rot, int state)
        {
            var s = FindSlipper(ownerSlot);
            if (s == null) return;

            if (state == (int)SlipperState.Held && holderSlot >= 0)
            {
                var holder = Unit(holderSlot);
                var carrier = holder != null ? holder.GetComponent<Carrier>() : null;
                carrier?.NotifyHolding(s);
            }
            else
            {
                s.transform.SetPositionAndRotation(pos, rot);
            }
        }

        public void SyncWorldSnapshotClientRpc(int roundNumber, int defenderSlot, float timeLeft, int[] scores, bool inProgress)
        {
            GameServices.Match?.ApplySnapshot(scores, roundNumber, inProgress);
        }

        public void BroadcastWorldSnapshot()
        {
            if (!NetAuthority.IsHost) return;

            BroadcastPicks();

            var match = GameServices.Match;
            var round = GameServices.Round;
            if (match == null) return;

            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = match.ScoreFor(i);

            SyncWorldSnapshotClientRpc(match.RoundNumber, match.DefenderSlot, round != null ? round.TimeLeft : Balance.RoundTime, scores, match.MatchInProgress);

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(256, Allocator.Temp);
                writer.WriteValueSafe(match.RoundNumber);
                writer.WriteValueSafe(match.DefenderSlot);
                writer.WriteValueSafe(round != null ? round.TimeLeft : Balance.RoundTime);
                writer.WriteValueSafe(scores);
                writer.WriteValueSafe(match.MatchInProgress);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncWorld", writer);
            }

            if (round?.Lata != null)
            {
                var l = round.Lata;
                SyncLataClientRpc(l.transform.position, l.transform.rotation, l.IsUpright, l.SkinIndex);

                if (_nm != null && _nm.CustomMessagingManager != null)
                {
                    using var writer = new FastBufferWriter(64, Allocator.Temp);
                    writer.WriteValueSafe(l.transform.position);
                    writer.WriteValueSafe(l.transform.rotation);
                    writer.WriteValueSafe(l.IsUpright);
                    writer.WriteValueSafe(l.SkinIndex);
                    _nm.CustomMessagingManager.SendNamedMessageToAll("SyncLata", writer);
                }
            }

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var s = FindSlipper(slot);
                if (s != null)
                {
                    int holderSlot = s.Holder != null ? s.Holder.PlayerSlot : -1;
                    SyncSlipperClientRpc(s.OwnerSlot, holderSlot, s.transform.position, s.transform.rotation, (int)s.State);

                    if (_nm != null && _nm.CustomMessagingManager != null)
                    {
                        using var writer = new FastBufferWriter(64, Allocator.Temp);
                        writer.WriteValueSafe(s.OwnerSlot);
                        writer.WriteValueSafe(holderSlot);
                        writer.WriteValueSafe(s.transform.position);
                        writer.WriteValueSafe(s.transform.rotation);
                        writer.WriteValueSafe((int)s.State);
                        _nm.CustomMessagingManager.SendNamedMessageToAll("SyncSlipper", writer);
                    }
                }

                var unit = Unit(slot);
                if (unit != null)
                {
                    SyncUnitTransformClientRpc(slot, unit.transform.position, unit.transform.eulerAngles.y, unit.Velocity);
                }
            }
        }

        private void OnSyncWorldMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int roundNumber);
            reader.ReadValueSafe(out int defenderSlot);
            reader.ReadValueSafe(out float timeLeft);
            reader.ReadValueSafe(out int[] scores);
            reader.ReadValueSafe(out bool inProgress);

            SyncWorldSnapshotClientRpc(roundNumber, defenderSlot, timeLeft, scores, inProgress);
        }

        private void OnSyncLataMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out Quaternion rot);
            reader.ReadValueSafe(out bool isUpright);
            reader.ReadValueSafe(out int skinIndex);

            SyncLataClientRpc(pos, rot, isUpright, skinIndex);
        }

        private void OnSyncSlipperMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int ownerSlot);
            reader.ReadValueSafe(out int holderSlot);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out Quaternion rot);
            reader.ReadValueSafe(out int state);

            SyncSlipperClientRpc(ownerSlot, holderSlot, pos, rot, state);
        }

        // -------------------------------------------------------------------
        // LATE JOIN AND DISCONNECT
        // -------------------------------------------------------------------

        public void HostLateJoin(int peerId)
        {
            if (!NetAuthority.IsHost) return;
            if (!_spawned.Add(peerId)) return;

            var lobby = NetSession.Instance?.Lobby;
            var peerRecord = lobby?.PeerInSeat(peerId);
            if (peerRecord != null && peerRecord.Seat >= 0)
            {
                var unit = Unit(peerRecord.Seat);
                if (unit != null)
                {
                    var ai = unit.GetComponent<AIController>();
                    if (ai != null) Destroy(ai);

                    unit.IsBot = false;
                    unit.PlayerName = peerRecord.Name;
                }
            }

            BroadcastWorldSnapshot();
        }

        public void HostPeerLeft(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            _spawned.Remove(peerId);

            var lobby = NetSession.Instance?.Lobby;
            if (lobby != null)
            {
                var peer = lobby.PeerInSeat(peerId);
                int seat = peer != null ? peer.Seat : -1;

                lobby.Depart(peerId);

                if (seat >= 0)
                {
                    var unit = Unit(seat);
                    if (unit != null)
                    {
                        unit.IsBot = true;
                        if (unit.GetComponent<AIController>() == null)
                        {
                            unit.gameObject.AddComponent<AIController>();
                        }
                    }
                }
            }

            FindFirstObjectByType<ReadyGate>()?.OnPeerLeft(peerId);
            BroadcastWorldSnapshot();
        }

        private readonly HashSet<int> _spawned = new HashSet<int>();
    }
}

