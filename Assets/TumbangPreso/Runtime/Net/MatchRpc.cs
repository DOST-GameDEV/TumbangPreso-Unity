using System;
using System.Collections;
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
        private readonly LobbySeatInfo[] _replicatedSeats = new LobbySeatInfo[Balance.PlayerCount];

        public LobbySeatInfo GetSeatInfo(int slot)
        {
            if (slot < 0 || slot >= Balance.PlayerCount) return null;
            if (NetAuthority.IsHost)
            {
                var lobby = NetSession.Instance?.Lobby;
                var peer = lobby?.PeerInSeat(slot);
                if (peer != null)
                {
                    return new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = peer.PeerId,
                        Name = peer.Name,
                        Occupied = true,
                        Spectator = peer.Spectator,
                        CharacterPick = peer.CharacterPick,
                        CanPick = peer.CanPick,
                        SlipperPick = peer.SlipperPick
                    };
                }
                return new LobbySeatInfo { Seat = slot, Occupied = false };
            }
            return _replicatedSeats[slot] ?? new LobbySeatInfo { Seat = slot, Occupied = false };
        }

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < Balance.PlayerCount; i++)
            {
                _replicatedSeats[i] = new LobbySeatInfo { Seat = i, Occupied = false };
            }
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(NetworkManager nm)
        {
            _nm = nm;
            RegisterHandlers();

            // ⚠️ A CLIENT ASKS FOR THE WORLD ONCE ITS ARENA EXISTS, rather than trusting the
            // snapshot the host sent at connect time. Transport finishes before SceneFlow has
            // finished building the seats, so on a cold relaunch that first snapshot lands in
            // an empty scene and the joiner sits there with no lata and no seat.
            if (!NetAuthority.IsHost && isActiveAndEnabled)
            {
                StartCoroutine(RequestSnapshotWhenArenaReady());
            }
        }

        private void RegisterHandlers()
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            var cm = _nm.CustomMessagingManager;

            cm.RegisterNamedMessageHandler("Identify", OnIdentifyMsg);
            cm.RegisterNamedMessageHandler("Seating", OnSeatingMsg);
            cm.RegisterNamedMessageHandler("DeclareReady", OnDeclareReadyMsg);
            cm.RegisterNamedMessageHandler("BeginCountdown", OnBeginCountdownMsg);
            cm.RegisterNamedMessageHandler("VoteRematch", OnVoteRematchMsg);
            cm.RegisterNamedMessageHandler("RematchTally", OnRematchTallyMsg);
            cm.RegisterNamedMessageHandler("BeginRematch", OnBeginRematchMsg);
            cm.RegisterNamedMessageHandler("SyncMap", OnSyncMapMsg);
            cm.RegisterNamedMessageHandler("SelectMap", OnSelectMapMsg);
            cm.RegisterNamedMessageHandler("SyncMode", OnSyncModeMsg);
            cm.RegisterNamedMessageHandler("SelectMode", OnSelectModeMsg);
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
            cm.RegisterNamedMessageHandler("ReqSnapshot", OnReqSnapshotMsg);
            cm.RegisterNamedMessageHandler("SyncAbility", OnSyncAbilityMsg);
            cm.RegisterNamedMessageHandler("RebindSeat", OnRebindSeatMsg);
            cm.RegisterNamedMessageHandler("ReqCue", OnReqCueMsg);
            cm.RegisterNamedMessageHandler("PlayCue", OnPlayCueMsg);
            cm.RegisterNamedMessageHandler("ReqBlink", OnReqBlinkMsg);
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
            int resolvedCharPick = charPick >= 0 ? charPick : 0;
            int resolvedCanPick = canPick >= 0 ? canPick : 0;
            int resolvedSlipperPick = slipperPick >= 0 ? slipperPick : 0;
            lobby.SetPicks(peerId, resolvedCharPick, resolvedCanPick, resolvedSlipperPick);

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

            // ⚠️ THE MODE IS THE FIRST THING A JOINER IS TOLD, for the reason `HostStartMatch`
            // gives: everything below it is interpreted through the mode, and a late joiner may
            // be about to build an arena from it.
            SyncModeClientRpc((int)UI.SceneFlow.SelectedMode);

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
        // THE REMATCH VOTE
        //
        // ⚠️⚠️ THREE MESSAGES, NOT ONE, AND THE MIDDLE ONE IS THE POINT. A vote that only
        // travelled peer-to-host would start the rematch correctly and leave the other three
        // players staring at a button they had already pressed, with no way to tell whether
        // anybody else had. `match_result.gd` draws the tally for the same reason: waiting is
        // only tolerable when you can see what you are waiting for.
        //
        // ⚠️ IT MIRRORS THE READY GATE ABOVE DELIBERATELY, down to resolving the host's own
        // sender id of 0 at the door. The two are the same problem (count the PEERS, not the
        // characters, because bot-filled seats cannot press anything) and a second shape for it
        // is a second thing to get wrong.
        // -------------------------------------------------------------------

        public void VoteRematchServerRpc(int peerId)
        {
            if (NetAuthority.IsHost)
            {
                FindFirstObjectByType<UI.MatchResult>()?.HostReceiveVote(peerId);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(peerId);
            _nm.CustomMessagingManager.SendNamedMessage("VoteRematch", NetworkManager.ServerClientId, writer);
        }

        private void OnVoteRematchMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            FindFirstObjectByType<UI.MatchResult>()?.HostReceiveVote((int)senderClientId);
        }

        /// <summary>HOST ONLY. Broadcasts "n of m have voted" so every screen can draw it.</summary>
        public void RematchTallyClientRpc(int votes, int expected)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(votes);
            writer.WriteValueSafe(expected);
            _nm.CustomMessagingManager.SendNamedMessageToAll("RematchTally", writer);
        }

        private void OnRematchTallyMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int votes);
            reader.ReadValueSafe(out int expected);
            FindFirstObjectByType<UI.MatchResult>()?.ShowTally(votes, expected);
        }

        /// <summary>HOST ONLY. Every playing peer has voted; everyone starts.</summary>
        public void BeginRematchClientRpc()
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            _nm.CustomMessagingManager.SendNamedMessageToAll("BeginRematch", writer);
        }

        private void OnBeginRematchMsg(ulong senderClientId, FastBufferReader reader)
        {
            FindFirstObjectByType<UI.MatchResult>()?.BeginRematchLocally();
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
        // WORLD SOUND, AND THE ONE ABILITY EFFECT THAT MOVES SOMEBODY ELSE'S BODY
        //
        // ⚠️⚠️ THE CUE RELAY IS THE ANSWER TO A MEASURED FAULT, NOT A CONVENIENCE.
        // `tools/audit_audio_reach.py` reports every `GameServices.Audio` call whose enclosing
        // method sits behind an open `NetAuthority.ShouldResolve()` return, and two of them are
        // the loudest events in the game: `Carrier.HostThrowAt` plays `throw_release` and
        // `Lata.HostKnockDown` plays `lata_seal`. Both are host-only, so in a networked match a
        // client has never heard a throw leave a hand or the can go over. `TumbangPreso.NetCue`
        // is the call site's half; this is the wire.
        //
        // ⚠️ IT SENDS PER CLIENT RATHER THAN `SendNamedMessageToAll`, BECAUSE THE PEER THAT MADE
        // THE SOUND HAS ALREADY PLAYED IT. `NetCue` plays locally first so the player who threw
        // hears it on the frame they threw, with no round trip; relaying to everyone would give
        // that one peer the sound twice, a few tens of milliseconds apart, which is a flam rather
        // than an echo and is worse than either.
        // -------------------------------------------------------------------

        /// <summary>Play a world cue on every peer except the one that already played it.</summary>
        public void BroadcastCue(string id, Vector3 position, float volumeScale)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (!NetAuthority.IsHost)
            {
                using var ask = new FastBufferWriter(96, Allocator.Temp);
                ask.WriteValueSafe(id ?? "");
                ask.WriteValueSafe(position);
                ask.WriteValueSafe(volumeScale);
                _nm.CustomMessagingManager.SendNamedMessage("ReqCue", NetworkManager.ServerClientId, ask);
                return;
            }

            HostRelayCue(id, position, volumeScale, _nm.LocalClientId);
        }

        /// <summary>
        /// ⚠️ `except` IS THE PEER THAT ALREADY HEARD IT, and on the host's own cue that is the
        /// host. A dedicated server is a referee with no seat (`NetAuthority.IsSeatlessReferee`),
        /// so on the VPS path nothing is excluded that anybody was listening on: the local
        /// `PlayAt` in `NetCue` goes to a machine with no player at it and this reaches all four.
        /// </summary>
        private void HostRelayCue(string id, Vector3 position, float volumeScale, ulong except)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;

            foreach (ulong client in _nm.ConnectedClientsIds)
            {
                if (client == except) continue;

                using var writer = new FastBufferWriter(96, Allocator.Temp);
                writer.WriteValueSafe(id ?? "");
                writer.WriteValueSafe(position);
                writer.WriteValueSafe(volumeScale);
                _nm.CustomMessagingManager.SendNamedMessage("PlayCue", client, writer);
            }
        }

        private void OnReqCueMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out string id);
            reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out float volumeScale);

            // ⚠️ THE HOST PLAYS IT TOO. It is not the sender, so it did not play it locally, and
            // a host that only relayed would be the one machine that could not hear a client's
            // throw. It is excluded from the relay below for the opposite reason, so both
            // branches together mean every peer plays every cue exactly once.
            GameServices.Audio?.PlayAtVaried(id, position, 0.94f, 1.06f, volumeScale);
            HostRelayCue(id, position, volumeScale, senderClientId);
        }

        private void OnPlayCueMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string id);
            reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out float volumeScale);

            GameServices.Audio?.PlayAtVaried(id, position, 0.94f, 1.06f, volumeScale);
        }

        /// <summary>
        /// A client asking the host to resolve Phaister's blink knockback.
        ///
        /// ⚠️⚠️ THE CLIENT SENDS AN INTENT, NEVER A RESULT, which is `NetAuthority`'s rule and
        /// the reason this carries a POINT and a FACING rather than a list of who was hit. The
        /// host runs the same `OverlapSphere` the solo game runs, from the position the client
        /// believed it blinked out of, and decides for itself who that reached. A message that
        /// could name its victims is a client that can stagger anybody it likes.
        ///
        /// ⚠️ THE SEAT COMES FROM THE SENDER'S OWN LOBBY RECORD, NOT FROM THE MESSAGE. Trusting
        /// a slot in the payload would let a peer resolve a blink on somebody else's behalf and
        /// exclude whoever it wanted from the shove.
        /// </summary>
        public void RequestBlinkShoveServerRpc(int slot, Vector3 at, Vector3 facing)
        {
            if (NetAuthority.IsHost)
            {
                Abilities.PhaisterHeroKit.ResolveBlinkShove(slot, at, facing);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(at);
            writer.WriteValueSafe(facing);
            _nm.CustomMessagingManager.SendNamedMessage("ReqBlink", NetworkManager.ServerClientId, writer);
        }

        private void OnReqBlinkMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out Vector3 at);
            reader.ReadValueSafe(out Vector3 facing);

            var peer = NetSession.Instance?.Lobby?.PeerById((int)senderClientId);
            int seat = peer != null ? peer.Seat : -1;
            if (seat < 0) return;

            Abilities.PhaisterHeroKit.ResolveBlinkShove(seat, at, facing);
        }

        // -------------------------------------------------------------------
        // LOBBY SETUP SYNCHRONIZATION (N5)
        // -------------------------------------------------------------------

        public static event Action<int> OnMapChanged;
        public static event Action<int> OnDifficultyChanged;
        public static event Action<int[]> OnLobbyPicksSynced;
        public static event Action<LobbySeatInfo[]> OnLobbyRosterSynced;
        public static event Action OnMatchStarted;

        public void HostStartMatch()
        {
            if (!NetAuthority.IsHost) return;

            // ⚠️⚠️ THE MODE GOES FIRST, BEFORE `StartMatch`, AND THE ORDER IS THE WHOLE POINT.
            // `OnStartMatchMsg` calls `UI.SceneFlow.StartMatch()`, which loads the arena scene
            // and builds every seat through `MatchInstaller`, and `MatchInstaller` reads
            // `SceneFlow.SelectedMode` to choose the roster AND to decide whether to install a
            // `HeroAbilitySystem` at all. A mode that arrives one message later arrives after the
            // bodies exist, which is exactly the *"other ppl not seeing the character"* fault:
            // the client builds the whole match in whatever mode its own menu happened to hold.
            // See § THE GAME MODE, WHICH WAS NEVER REPLICATED AT ALL.
            SyncModeClientRpc((int)UI.SceneFlow.SelectedMode);

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

        // -------------------------------------------------------------------
        // § THE GAME MODE, WHICH WAS NEVER REPLICATED AT ALL
        //
        // ⚠️⚠️ THIS IS THE ROOT OF *"its heavily broken with other ppl not seeing the
        // character"* (🧑, 2026-08-27) AND IT IS BIGGER THAN THE SKINS. `UI.SceneFlow.SelectedMode`
        // is a plain static set by whoever last touched the mode toggle in `ConvertedMatchSetup`.
        // The map is replicated (`SyncMap`), the difficulty is replicated (`SyncDiff`), the picks
        // are replicated, the seats are replicated. **The mode is not, and it decides more than
        // any of them.**
        //
        // ⚠️⚠️ A CLIENT WHOSE MENU LAST SAID CLASSIC, JOINING A HERO STRIKE MATCH, BUILDS A
        // DIFFERENT GAME. `MatchInstaller` reads `SelectedMode` in at least three places:
        //
        //   * `_book.PersonArt(motor.CharacterIndex, SceneFlow.SelectedMode)` resolves the model
        //     against `Roster.GetPeople(mode)`, which is the twelve street characters in Classic
        //     and the five heroes in Hero Strike. **A hero index looked up in the street cast is
        //     a different person**, and past the end of the list `Resolve` falls back to `art[0]`
        //     so several seats collapse onto the same wrong body. That is *"they see the older
        //     version of the skin"* precisely: the older roster.
        //   * `if (SceneFlow.SelectedMode == GameMode.HeroStrike)` gates installing
        //     `HeroAbilitySystem` at all, so a client in the wrong mode gives four seats no kit.
        //   * `CharacterMotor.Mode` feeds `Roster.GetPeople(Mode)` for the nameplate, so the
        //     labels disagree with the bodies.
        //
        // ⚠️ IT IS SENT THE SAME WAY THE MAP IS, AND DELIBERATELY NOT AS PART OF THE PICK TABLE.
        // The mode has to be true BEFORE any seat is built, and the pick table arrives after the
        // arena exists. Same shape as `SelectMap` so there is one idiom for "a lobby setting the
        // host owns": client asks, host decides, host tells everybody.
        // -------------------------------------------------------------------

        public static event Action<int> OnModeChanged;

        public void SelectModeServerRpc(int mode)
        {
            if (NetAuthority.IsHost)
            {
                SyncModeClientRpc(mode);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(mode);
            _nm.CustomMessagingManager.SendNamedMessage("SelectMode", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectModeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int mode);
            SyncModeClientRpc(mode);
        }

        public void SyncModeClientRpc(int mode)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(mode);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncMode", writer);
            }
            ApplyMode(mode);
        }

        private void OnSyncModeMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int mode);
            ApplyMode(mode);
        }

        /// <summary>
        /// ⚠️ IT WRITES `SceneFlow.SelectedMode` DIRECTLY RATHER THAN RAISING AN EVENT AND HOPING
        /// SOMEBODY LISTENS. Every reader of the mode reads that static, so the static is the
        /// thing that has to be right; the event is for screens that want to redraw.
        /// </summary>
        private static void ApplyMode(int mode)
        {
            var wanted = mode == (int)GameMode.HeroStrike ? GameMode.HeroStrike : GameMode.Classic;
            UI.SceneFlow.SelectedMode = wanted;

            // ⚠️ THE LIVE SEATS ARE CORRECTED TOO, because a mode message can arrive after the
            // arena has been built: on a late join the host sends this from `HostSyncPeer` when
            // the client already has four bodies standing in the street. `CharacterMotor.Mode`
            // feeds the roster lookup behind the nameplate, so leaving it stale is a screen that
            // disagrees with the models.
            var round = GameServices.Round;
            if (round != null)
            {
                foreach (var p in round.Players)
                    if (p != null) p.Mode = wanted;
            }

            OnModeChanged?.Invoke(mode);
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

        public void SelectLobbyPickServerRpc(int character, int can, int slipper)
        {
            if (NetAuthority.IsHost)
            {
                var lobby = NetSession.Instance?.Lobby;
                if (lobby != null)
                {
                    // ⚠️ HOST'S OWN PEER ID COMES FROM LOCAL CLIENT ID, NEVER FROM LOCAL SEAT.
                    // LocalSlot is 0-3 (a seat) while _peers is keyed by transport client ID.
                    int hostPeerId = _nm != null ? (int)_nm.LocalClientId : 0;
                    lobby.SetPicks(hostPeerId, character, can, slipper);
                    BroadcastLobbyPicks();
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(0);
            writer.WriteValueSafe(character);
            writer.WriteValueSafe(can);
            writer.WriteValueSafe(slipper);
            _nm.CustomMessagingManager.SendNamedMessage("SelectLobbyPick", NetworkManager.ServerClientId, writer);
        }

        public void SelectLobbyPickServerRpc(int peerId, int character, int can, int slipper)
            => SelectLobbyPickServerRpc(character, can, slipper);

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

            var seats = new LobbySeatInfo[Balance.PlayerCount];
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var peer = lobby.PeerInSeat(slot);
                if (peer != null)
                {
                    seats[slot] = new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = peer.PeerId,
                        Name = peer.Name ?? "",
                        Occupied = true,
                        Spectator = peer.Spectator,
                        CharacterPick = peer.CharacterPick,
                        CanPick = peer.CanPick,
                        SlipperPick = peer.SlipperPick
                    };
                }
                else
                {
                    seats[slot] = new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = -1,
                        Name = "",
                        Occupied = false,
                        Spectator = false,
                        CharacterPick = -1,
                        CanPick = -1,
                        SlipperPick = -1
                    };
                }
                _replicatedSeats[slot] = seats[slot];
            }

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(512, Allocator.Temp);
                writer.WriteValueSafe(Balance.PlayerCount);
                for (int i = 0; i < Balance.PlayerCount; i++)
                {
                    var s = seats[i];
                    writer.WriteValueSafe(s.Seat);
                    writer.WriteValueSafe(s.PeerId);
                    writer.WriteValueSafe(s.Name ?? "");
                    writer.WriteValueSafe(s.Occupied);
                    writer.WriteValueSafe(s.Spectator);
                    writer.WriteValueSafe(s.CharacterPick);
                    writer.WriteValueSafe(s.CanPick);
                    writer.WriteValueSafe(s.SlipperPick);
                }
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncLobbyPicks", writer);
            }

            var table = new int[Balance.PlayerCount * 4];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                if (seats[slot].Occupied)
                {
                    table[slot * 4] = slot;
                    table[slot * 4 + 1] = seats[slot].CharacterPick;
                    table[slot * 4 + 2] = seats[slot].CanPick;
                    table[slot * 4 + 3] = seats[slot].SlipperPick;
                }
            }

            OnLobbyPicksSynced?.Invoke(table);
            OnLobbyRosterSynced?.Invoke(seats);
        }

        private void OnSyncLobbyPicksMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);
            var seats = new LobbySeatInfo[Mathf.Max(count, Balance.PlayerCount)];
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out int seat);
                reader.ReadValueSafe(out int peerId);
                reader.ReadValueSafe(out string name);
                reader.ReadValueSafe(out bool occupied);
                reader.ReadValueSafe(out bool spectator);
                reader.ReadValueSafe(out int charPick);
                reader.ReadValueSafe(out int canPick);
                reader.ReadValueSafe(out int slipperPick);

                var info = new LobbySeatInfo
                {
                    Seat = seat,
                    PeerId = peerId,
                    Name = name,
                    Occupied = occupied,
                    Spectator = spectator,
                    CharacterPick = charPick,
                    CanPick = canPick,
                    SlipperPick = slipperPick
                };
                if (seat >= 0 && seat < _replicatedSeats.Length)
                {
                    _replicatedSeats[seat] = info;
                }
                if (i < seats.Length) seats[i] = info;
            }

            var table = new int[Balance.PlayerCount * 4];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                if (slot < _replicatedSeats.Length && _replicatedSeats[slot] != null && _replicatedSeats[slot].Occupied)
                {
                    table[slot * 4] = slot;
                    table[slot * 4 + 1] = _replicatedSeats[slot].CharacterPick;
                    table[slot * 4 + 2] = _replicatedSeats[slot].CanPick;
                    table[slot * 4 + 3] = _replicatedSeats[slot].SlipperPick;
                }
            }

            OnLobbyPicksSynced?.Invoke(table);
            OnLobbyRosterSynced?.Invoke(seats);
        }

        // -------------------------------------------------------------------
        // PICKS SYNCHRONIZATION
        // -------------------------------------------------------------------

        /// <summary>
        /// Put every seat in the character its owner picked.
        ///
        /// ⚠️⚠️ THIS METHOD HAD THREE SEPARATE FAULTS AND TOGETHER THEY ARE
        /// *"apparently only host sees the skin of other players"* AND *"in heroes gamemode,
        /// frequently they see the older version of the skin"* (🧑, 2026-08-27). All three are
        /// invisible on the host, because the host never runs the client half of this.
        ///
        /// ⚠️⚠️ 1. IT RESOLVED THE ART AGAINST THE WRONG ROSTER, AND THAT IS THE HERO STRIKE
        /// BUG EXACTLY. `RosterBook` has two overloads: `PersonArt(index)` resolves against
        /// `Roster.People`, which is the CLASSIC twelve, and `PersonArt(index, mode)` resolves
        /// against `Roster.GetPeople(mode)`. This called the first one. In Hero Strike a
        /// `CharacterIndex` is an index into the FIVE HEROES, so every client took a hero index,
        /// looked it up in the street cast, and applied a completely different character's
        /// model. `Roster.At` returns null past the end and `Resolve` then falls back to
        /// `art[0]`, so out-of-range picks all collapsed onto the same wrong body. **That is
        /// literally "the older version of the skin": it is the Classic roster, which is the
        /// older one.** `MatchInstaller` line 494 has always used the mode-aware overload, which
        /// is why a locally spawned seat looked right and a replicated one did not.
        ///
        /// ⚠️⚠️ 2. IT ONLY APPLIED THE MODEL WHEN THE INDEX CHANGED. The guard was
        /// `who.CharacterIndex != charIndex`, so a seat that already carried the right NUMBER
        /// was never given the right ART. That is the common case on a joining client: the seats
        /// are built from whatever the lobby table said at spawn time and then this sync arrives
        /// agreeing with it, so the one message whose whole job is to fix the model decided
        /// there was nothing to do. Applying art is idempotent; skipping it is not.
        ///
        /// ⚠️⚠️ 3. IT DROPPED THE PET. `MatchInstaller` passes `art.PetModel` as a sixth
        /// argument and this passed five, so every client rebuilt Nemu without Kuro. Her entire
        /// kit is him (`docs/TODO.md` § 28), so on a client she was a hero with three powers that
        /// referenced an object that was not there.
        ///
        /// ⚠️ THE MODE IS READ FROM `SceneFlow.SelectedMode`, WHICH IS REPLICATED AS OF THE SAME
        /// SESSION AND WAS NOT BEFORE IT. See § THE GAME MODE, WHICH WAS NEVER REPLICATED AT ALL:
        /// the host now sends it ahead of `StartMatch` and ahead of a late joiner's snapshot, so
        /// both ends agree before any seat exists. Sending it again inside this table would be a
        /// second source of truth for the same fact, and it would arrive too late to matter:
        /// the seats are already built by the time a pick table is read.
        /// </summary>
        public void SyncPicksClientRpc(int[] table)
        {
            if (table == null) return;

            var book = RosterBook.Load();
            var mode = UI.SceneFlow.SelectedMode;

            for (int i = 0; i + 3 < table.Length; i += 4)
            {
                int slot = table[i];
                int charIndex = table[i + 1];

                var who = Unit(slot);
                if (who == null) continue;

                if (charIndex >= 0)
                {
                    who.CharacterIndex = charIndex;

                    var person = book != null ? book.PersonArt(charIndex, mode) : null;
                    if (person != null && person.Model != null)
                    {
                        var vis = who.GetComponent<Visual.CharacterVisual>();
                        vis?.ApplyModel(person.Model, person.Tint, person.Clips,
                                        person.Palette, person.PetModel);
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

            lata.ApplySnapshotState(pos, rot, isUpright, skinIndex);
        }

        public void SyncSlipperClientRpc(int ownerSlot, int holderSlot, Vector3 pos,
                                         Quaternion rot, int state, Vector3 velocity,
                                         float pektusSpin, int affinity, int throwerSlot)
        {
            var s = FindSlipper(ownerSlot);
            if (s == null) return;

            var holder = holderSlot >= 0 ? Unit(holderSlot) : null;
            s.ApplySnapshotState((SlipperState)state, holder, pos, rot, velocity,
                                 pektusSpin, (SlipperAffinity)affinity, throwerSlot);
        }

        public void SyncWorldSnapshotClientRpc(int roundNumber, int defenderSlot,
                                               float timeLeft, int[] scores,
                                               bool inProgress, bool roundActive)
        {
            GameServices.Match?.ApplySnapshot(scores, roundNumber, inProgress);
            GameServices.Round?.ApplySnapshot(timeLeft, roundActive, defenderSlot);
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

            bool roundActive = round != null && round.RoundActive;
            float timeLeft = round != null ? round.TimeLeft : Balance.RoundTime;

            SyncWorldSnapshotClientRpc(match.RoundNumber, match.DefenderSlot, timeLeft, scores,
                                       match.MatchInProgress, roundActive);

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(256, Allocator.Temp);
                writer.WriteValueSafe(match.RoundNumber);
                writer.WriteValueSafe(match.DefenderSlot);
                writer.WriteValueSafe(timeLeft);
                writer.WriteValueSafe(scores);
                writer.WriteValueSafe(match.MatchInProgress);
                writer.WriteValueSafe(roundActive);
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
                    SyncSlipperClientRpc(s.OwnerSlot, holderSlot, s.transform.position,
                        s.transform.rotation, (int)s.State, s.Velocity, s.PektusSpin,
                        (int)s.Affinity, s.ThrowerSlot);

                    if (_nm != null && _nm.CustomMessagingManager != null)
                    {
                        using var writer = new FastBufferWriter(128, Allocator.Temp);
                        writer.WriteValueSafe(s.OwnerSlot);
                        writer.WriteValueSafe(holderSlot);
                        writer.WriteValueSafe(s.transform.position);
                        writer.WriteValueSafe(s.transform.rotation);
                        writer.WriteValueSafe((int)s.State);
                        writer.WriteValueSafe(s.Velocity);
                        writer.WriteValueSafe(s.PektusSpin);
                        writer.WriteValueSafe((int)s.Affinity);
                        writer.WriteValueSafe(s.ThrowerSlot);
                        _nm.CustomMessagingManager.SendNamedMessageToAll("SyncSlipper", writer);
                    }
                }

                var unit = Unit(slot);
                if (unit != null)
                {
                    SyncUnitTransformClientRpc(slot, unit.transform.position, unit.transform.eulerAngles.y, unit.Velocity);
                    BroadcastAbilityState(slot, unit);
                }
            }
        }

        // -------------------------------------------------------------------
        // § ABILITY STATE ACROSS A RECONNECT
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"or if u retain ur skill cooldowns and charges and shi"*. Before
        // this the answer was no. The world snapshot carried the round, the scores, the clock,
        // the lata, the slippers, the picks and every unit transform, and **not one byte of
        // ability state**, so a client that dropped and came back rebuilt its kit from the
        // constructor: cooldowns zero, charges full, ultimate meter empty.
        //
        // ⚠️⚠️ AND IT CUTS BOTH WAYS. Reconnecting to refresh a 62 s cooldown is the cheat;
        // losing 115 banked charge to a dropped packet is the bug that actually gets reported.
        // The host never had either, because its own kits are continuous objects that were never
        // rebuilt, which is exactly why this survived every single-machine test.
        //
        // ⚠️ IT IS A SEPARATE NAMED MESSAGE RATHER THAN MORE FIELDS ON `SyncWorld`, because
        // `SyncWorld` is per-MATCH and this is per-SEAT. Widening it would have meant packing four
        // seats' kits into one payload and unpacking them against a seat order the receiving side
        // has to already agree about, which is the shape of bug § 32.2 records three of.
        // -------------------------------------------------------------------

        private void BroadcastAbilityState(int slot, CharacterMotor unit)
        {
            if (!NetAuthority.IsHost) return;

            var kit = unit != null && unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
            if (kit == null) return;

            float s1Cd = kit.Skill1 != null ? kit.Skill1.CooldownRemaining : 0.0f;
            int s1Ch = kit.Skill1 != null ? kit.Skill1.ChargesRemaining : 0;
            float s2Cd = kit.Skill2 != null ? kit.Skill2.CooldownRemaining : 0.0f;
            int s2Ch = kit.Skill2 != null ? kit.Skill2.ChargesRemaining : 0;
            float ultCd = kit.Ultimate != null ? kit.Ultimate.CooldownRemaining : 0.0f;

            SyncAbilityStateClientRpc(slot, kit.UltimateCharge, s1Cd, s1Ch, s2Cd, s2Ch, ultCd);

            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(kit.UltimateCharge);
            writer.WriteValueSafe(s1Cd);
            writer.WriteValueSafe(s1Ch);
            writer.WriteValueSafe(s2Cd);
            writer.WriteValueSafe(s2Ch);
            writer.WriteValueSafe(ultCd);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncAbility", writer);
        }

        /// <summary>
        /// ⚠️ THE HOST APPLIES NOTHING. Its kit is the authority and is already correct; letting
        /// it write its own broadcast back over itself would round-trip every value through the
        /// wire's precision for no reason, and would overwrite a cooldown that started in the
        /// same frame the snapshot was built.
        /// </summary>
        public void SyncAbilityStateClientRpc(int slot, float ultimateCharge,
                                              float skill1Cooldown, int skill1Charges,
                                              float skill2Cooldown, int skill2Charges,
                                              float ultimateCooldown)
        {
            if (NetAuthority.IsHost) return;

            var unit = Unit(slot);
            var kit = unit != null && unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
            if (kit == null) return;

            kit.ApplyNetworkSnapshot(ultimateCharge, skill1Cooldown, skill1Charges,
                                     skill2Cooldown, skill2Charges, ultimateCooldown);
        }

        private void OnSyncAbilityMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out float ultimateCharge);
            reader.ReadValueSafe(out float s1Cd);
            reader.ReadValueSafe(out int s1Ch);
            reader.ReadValueSafe(out float s2Cd);
            reader.ReadValueSafe(out int s2Ch);
            reader.ReadValueSafe(out float ultCd);

            SyncAbilityStateClientRpc(slot, ultimateCharge, s1Cd, s1Ch, s2Cd, s2Ch, ultCd);
        }

        private void OnSyncWorldMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int roundNumber);
            reader.ReadValueSafe(out int defenderSlot);
            reader.ReadValueSafe(out float timeLeft);
            reader.ReadValueSafe(out int[] scores);
            reader.ReadValueSafe(out bool inProgress);
            reader.ReadValueSafe(out bool roundActive);

            SyncWorldSnapshotClientRpc(roundNumber, defenderSlot, timeLeft, scores, inProgress,
                                       roundActive);
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
            reader.ReadValueSafe(out Vector3 velocity);
            reader.ReadValueSafe(out float pektusSpin);
            reader.ReadValueSafe(out int affinity);
            reader.ReadValueSafe(out int throwerSlot);

            SyncSlipperClientRpc(ownerSlot, holderSlot, pos, rot, state, velocity, pektusSpin,
                                 affinity, throwerSlot);
        }

        // -------------------------------------------------------------------
        // LATE JOIN AND DISCONNECT
        // -------------------------------------------------------------------

        public void HostLateJoin(int peerId)
        {
            if (!NetAuthority.IsHost) return;
            if (!_spawned.Add(peerId)) return;

            var lobby = NetSession.Instance?.Lobby;
            var peerRecord = lobby?.PeerById(peerId);
            if (peerRecord != null && peerRecord.Seat >= 0)
            {
                var unit = Unit(peerRecord.Seat);
                if (unit != null)
                {
                    var ai = unit.GetComponent<AIController>();
                    if (ai != null) Destroy(ai);

                    unit.IsBot = false;
                    unit.PlayerName = peerRecord.Name;

                    // ⚠⚠ THE MIRROR OF `HostPeerLeft`'S CALL. The returning player drives this
                    // body now, so the host must STOP broadcasting it or its own stale copy
                    // fights the transforms that player is submitting at 50 Hz.
                    //
                    // ⚠️ `Destroy` IS DEFERRED TO THE END OF THE FRAME, so `GetComponent` would
                    // still answer "there is an AIController here" if the cache were rebuilt on
                    // this line. It is INVALIDATED here and rebuilt on the next physics step,
                    // by which time the component is really gone.
                    unit.ForgetInputSource();
                }
            }

            HostSyncPeer(peerId);
        }

        /// <summary>
        /// Asks the host to rehydrate this process once its arena objects exist. Transport
        /// connection can finish before the client-controlled SceneFlow has built its seats, so
        /// the connection-time snapshot alone is not sufficient for a cold app relaunch.
        ///
        /// ⚠️ IT TRAVELS AS A NAMED MESSAGE, NOT AN [ServerRpc]. This component is a plain
        /// MonoBehaviour on the Relay path, so there is no NetworkBehaviour to carry one, and
        /// every other request in this file already goes through CustomMessagingManager.
        /// </summary>
        public void RequestWorldSnapshot()
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (NetAuthority.IsHost)
            {
                HostSyncPeer((int)_nm.LocalClientId);
                return;
            }

            using var writer = new FastBufferWriter(sizeof(byte), Allocator.Temp);
            writer.WriteValueSafe((byte)0);
            _nm.CustomMessagingManager.SendNamedMessage("ReqSnapshot", NetworkManager.ServerClientId, writer);
        }

        private void OnReqSnapshotMsg(ulong senderClientId, FastBufferReader reader)
        {
            HostSyncPeer((int)senderClientId);
        }

        private IEnumerator RequestSnapshotWhenArenaReady()
        {
            // NetBootstrap starts transport and scene loading independently. Wait for the
            // client-owned arena to finish installing before asking the host to rehydrate it.
            // This also handles a slow disk or a cold app relaunch without timing guesses.
            while (_nm != null && _nm.IsClient && !NetAuthority.IsHost)
            {
                var round = GameServices.Round;
                if (round != null && round.Lata != null &&
                    round.Players.Count >= Balance.PlayerCount)
                {
                    yield return null; // let camera and HUD finish their Start methods
                    RequestWorldSnapshot();
                    yield break;
                }

                yield return null;
            }
        }

        private void HostSyncPeer(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            var lobby = NetSession.Instance?.Lobby;
            var peerRecord = lobby?.PeerById(peerId);
            if (peerRecord != null && peerRecord.Seat >= 0)
            {
                var match = GameServices.Match;
                var round = GameServices.Round;
                SendRebindLocalSeat(peerId,
                                    peerRecord.Seat,
                                    match != null ? match.DefenderSlot : -1,
                                    round != null && round.RoundActive,
                                    peerRecord.Name);
            }

            // The joiner needs the whole world state, not just its own seat. Broadcast is
            // intentionally idempotent and also repairs any packet-lagged observer.
            BroadcastWorldSnapshot();
        }

        public void HostPeerLeft(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            _spawned.Remove(peerId);

            var lobby = NetSession.Instance?.Lobby;
            if (lobby != null)
            {
                var peer = lobby.PeerById(peerId);
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

                        // ⚠⚠ THE SEAT JUST CHANGED HANDS AND `CharacterMotor` CACHES WHO DRIVES IT.
                        // Without this the host keeps treating the body as remote-driven and
                        // never broadcasts its transform, so the bot that just took over is a
                        // statue on every client's screen. See `StepNetworkTransform`.
                        unit.ForgetInputSource();
                    }
                }
            }

            FindFirstObjectByType<ReadyGate>()?.OnPeerLeft(peerId);

            // ⚠️ THE REMATCH VOTE HAS THE SAME HOLE AND IS CLOSED AT THE SAME PLACE. A peer that
            // quits from the result screen drops the expected count, and with nobody
            // re-evaluating, the players still watching wait forever on a gate that is already
            // satisfied. See MatchResult.OnPeerLeft.
            FindFirstObjectByType<UI.MatchResult>()?.OnPeerLeft(peerId);

            BroadcastWorldSnapshot();
        }

        /// <summary>
        /// The host tells only the reconnecting process which seat it controls. The world
        /// bodies are scene objects rather than NetworkObjects, so Netcode cannot transfer
        /// ownership for us; input, camera, HUD, and role presentation must be rebound as one
        /// atomic operation.
        /// </summary>
        private void SendRebindLocalSeat(int peerId, int seat, int defenderSlot,
                                         bool roundActive, string playerName)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if ((ulong)peerId == _nm.LocalClientId)
            {
                ApplyRebindLocalSeat(seat, defenderSlot, roundActive, playerName);
                return;
            }

            using var writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(seat);
            writer.WriteValueSafe(defenderSlot);
            writer.WriteValueSafe(roundActive);
            writer.WriteValueSafe(playerName ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("RebindSeat", (ulong)peerId, writer);
        }

        private void OnRebindSeatMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int defenderSlot);
            reader.ReadValueSafe(out bool roundActive);
            reader.ReadValueSafe(out string playerName);

            ApplyRebindLocalSeat(seat, defenderSlot, roundActive, playerName);
        }

        private void ApplyRebindLocalSeat(int seat, int defenderSlot, bool roundActive,
                                          string playerName)
        {
            var net = NetSession.Instance;
            net?.ApplyAssignedSeat(seat);

            var round = GameServices.Round;
            if (round == null || seat < 0) return;

            CharacterMotor local = null;
            foreach (var unit in round.Players)
            {
                if (unit == null) continue;

                unit.IsDefender = unit.PlayerSlot == defenderSlot;
                unit.RoundActive = roundActive;

                var reader = unit.GetComponent<PlayerInputReader>();
                if (unit.PlayerSlot == seat)
                {
                    local = unit;
                    unit.IsBot = false;
                    unit.PlayerName = playerName;

                    var ai = unit.GetComponent<AIController>();
                    if (ai != null)
                    {
                        ai.enabled = false;
                        Destroy(ai);
                    }

                    if (reader == null) unit.gameObject.AddComponent<PlayerInputReader>();
                    else reader.enabled = true;
                }
                else if (reader != null)
                {
                    reader.enabled = false;
                    Destroy(reader);
                }

                unit.GetComponentInChildren<Visual.CharacterNameplate>()?.Refresh();
            }

            if (local == null) return;

            var spectator = FindFirstObjectByType<CameraSystem.SpectatorCamera>();
            if (spectator != null) spectator.enabled = false;

            var camera = UnityEngine.Camera.main;
            var rig = camera != null ? camera.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig == null) rig = FindFirstObjectByType<CameraSystem.CameraRig>();
            if (rig != null)
            {
                rig.Follow(local);
                rig.SetAimSource(CameraSystem.AimSource.Mouse);
                rig.SetActive(true);
            }

            UI.Hud.Instance?.Bind(local);
            var youCard = FindFirstObjectByType<UI.YouCard>();
            if (youCard != null)
            {
                youCard.Bind(local);
                youCard.Refresh();
            }

            var pause = FindFirstObjectByType<PauseWatcher>();
            if (pause != null) pause.Local = local;

            foreach (var slipper in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                slipper.SetOwnerGlow(slipper.OwnerSlot == seat);
        }

        private readonly HashSet<int> _spawned = new HashSet<int>();
    }
}
