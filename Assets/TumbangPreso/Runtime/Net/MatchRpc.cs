using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The gameplay RPCs, converted from the `@rpc` surface spread across `carrier.gd`,
    /// `character_base.gd` and `main.gd`.
    ///
    /// ⚠️⚠️ EVERY VERB IS A REQUEST TO THE HOST, NEVER A LOCAL RESOLUTION. A client that
    /// resolved its own tag would be authoritative over somebody else's stun. The pattern is
    /// always the same: the client asks, the host decides using the same rule the solo game
    /// uses, and the host broadcasts what happened. That is why `NetAuthority.ShouldResolve`
    /// exists and why nothing here calls a gameplay method directly.
    ///
    /// ⚠️ POSITION AND FACING TRAVEL WITH THE REQUEST. The host must judge the verb against
    /// where the client BELIEVED it was standing, not where the host currently thinks it is —
    /// otherwise every lunge is judged a frame or two late and misses on a lagged connection
    /// while looking like a direct hit on the client's screen.
    ///
    /// ⚠️ AND THE VISUAL HALF IS SEPARATE FROM THE RESOLUTION HALF. A charge-up read
    /// broadcasts on its own (`*ChargeVisual`) because the other players need to SEE a wind-up
    /// before it resolves; folding it into the result would show the tell and the tag on the
    /// same frame, which removes the only warning the game gives.
    /// </summary>
    public sealed class MatchRpc : NetworkBehaviour
    {
        public static MatchRpc Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            base.OnNetworkSpawn();

            if (!IsServer) StartCoroutine(RequestSnapshotWhenArenaReady());
        }

        private IEnumerator RequestSnapshotWhenArenaReady()
        {
            // NetBootstrap starts transport and scene loading independently. Wait for the
            // client-owned arena to finish installing before asking the host to rehydrate it.
            // This also handles a slow disk or a cold app relaunch without timing guesses.
            while (IsSpawned)
            {
                var round = GameServices.Round;
                if (round != null && round.Lata != null &&
                    round.Players.Count >= Core.Balance.PlayerCount)
                {
                    yield return null; // let camera and HUD finish their Start methods
                    RequestWorldSnapshotServerRpc();
                    yield break;
                }

                yield return null;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            base.OnNetworkDespawn();
        }

        private static CharacterMotor Unit(int slot)
        {
            var round = GameServices.Round;
            return round != null ? round.PlayerAt(slot) : null;
        }

        // -------------------------------------------------------------------
        // THE TAYA'S TWO TAG VERBS
        // -------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void RequestPunchServerRpc(int slot, Vector3 from, Vector3 facing)
        {
            var who = Unit(slot);
            if (who == null || !who.IsDefender) return;

            var verbs = who.GetComponent<CombatVerbs>();
            verbs?.HostResolvePunch(from, facing);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestLungeServerRpc(int slot, Vector3 from, Vector3 facing, float power)
        {
            var who = Unit(slot);
            if (who == null || !who.IsDefender) return;

            var verbs = who.GetComponent<CombatVerbs>();
            verbs?.HostResolveLunge(from, facing, power);
        }

        /// <summary>An attacker shoving a rival. Attackers only — the taya has the tag verbs.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestShoveServerRpc(int slot, Vector3 from, Vector3 facing)
        {
            var who = Unit(slot);
            if (who == null || who.IsDefender) return;

            var verbs = who.GetComponent<CombatVerbs>();
            verbs?.HostResolveShove(from, facing);
        }

        // -------------------------------------------------------------------
        // WIND-UP READS — broadcast on their own, see the class note.
        // -------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void LungeChargeServerRpc(int slot, bool active)
            => LungeChargeClientRpc(slot, active);

        [ClientRpc]
        private void LungeChargeClientRpc(int slot, bool active)
            => Unit(slot)?.GetComponentInChildren<Visual.CharacterAnimator>()
                ?.PlayAction(active ? "lunge" : null);

        [ServerRpc(RequireOwnership = false)]
        public void ShoveChargeServerRpc(int slot, bool active)
            => ShoveChargeClientRpc(slot, active);

        [ClientRpc]
        private void ShoveChargeClientRpc(int slot, bool active)
            => Unit(slot)?.GetComponentInChildren<Visual.CharacterAnimator>()
                ?.PlayAction(active ? "shove" : null);

        // -------------------------------------------------------------------
        // THE SLIPPER
        // -------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void RequestGrabServerRpc(int slot, int slipperOwnerSlot)
        {
            var who = Unit(slot);
            var slipper = FindSlipper(slipperOwnerSlot);
            if (who == null || slipper == null) return;

            // ⚠️ THE HOST RE-CHECKS ELIGIBILITY. A client asking for a slipper it cannot
            // legally take is not an error — it is a client one frame behind — and the answer
            // is to refuse quietly, not to trust the request.
            if (!slipper.CanBeGrabbedBy(who)) return;

            who.GetComponent<Carrier>()?.HostPickUp(slipper);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestThrowServerRpc(int slot, Vector3 origin, Vector3 aimPoint, float charge)
        {
            var who = Unit(slot);
            var carrier = who != null ? who.GetComponent<Carrier>() : null;
            if (carrier == null || carrier.Held == null) return;

            if (GameServices.Round == null || !GameServices.Round.CanThrow(who)) return;

            carrier.HostThrowAt(origin, aimPoint, charge);
        }

        /// <summary>The taya's righting channel completing.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestResetServerRpc(int slot)
        {
            var who = Unit(slot);
            if (who == null || !who.IsDefender) return;

            GameServices.Round?.Lata?.HostRestore();
        }

        private static Slipper FindSlipper(int ownerSlot)
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s.OwnerSlot == ownerSlot) return s;

            return null;
        }

        // -------------------------------------------------------------------
        // EMOTES
        //
        // ⚠️ THE EMOTE REPLICATES; THE CAMERA SWING DOES NOT. Every peer plays the clip, and
        // only the emoting player's own rig changes view. See CameraRig's note.
        // -------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void RequestEmoteServerRpc(int slot, string id)
        {
            var who = Unit(slot);
            var player = who != null ? who.GetComponent<Social.EmotePlayer>() : null;

            // Validated host-side, so a peer cannot show everyone an emote the host refused.
            if (player == null || !player.CanEmote()) return;

            PlayEmoteClientRpc(slot, id);
        }

        [ClientRpc]
        private void PlayEmoteClientRpc(int slot, string id)
            => Unit(slot)?.GetComponent<Social.EmotePlayer>()?.Play(id);

        [ServerRpc(RequireOwnership = false)]
        public void StopEmoteServerRpc(int slot) => StopEmoteClientRpc(slot);

        [ClientRpc]
        private void StopEmoteClientRpc(int slot)
            => Unit(slot)?.GetComponent<Social.EmotePlayer>()?.Stop();

        // -------------------------------------------------------------------
        // THE READY GATE
        // -------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void DeclareReadyServerRpc(int peerId)
            => FindFirstObjectByType<ReadyGate>()?.DeclareReady(peerId);

        /// <summary>
        /// ⚠️ EVERY PEER RUNS ITS OWN COUNTDOWN rather than being told when it finished. A
        /// client that only learns about the round when it begins gets no 3 · 2 · 1 at all.
        /// </summary>
        [ClientRpc]
        public void BeginCountdownClientRpc()
        {
            var gate = FindFirstObjectByType<ReadyGate>();
            gate?.StartLocalCountdown();
        }

        // -------------------------------------------------------------------
        // LOBBY SETUP SYNCHRONIZATION (N5)
        // -------------------------------------------------------------------

        public static event System.Action<int> OnMapChanged;
        public static event System.Action<int> OnDifficultyChanged;
        public static event System.Action<int[]> OnLobbyPicksSynced;

        [ServerRpc(RequireOwnership = false)]
        public void SelectMapServerRpc(int mapIndex)
        {
            if (!NetAuthority.IsHost) return;
            SyncMapClientRpc(mapIndex);
        }

        [ClientRpc]
        private void SyncMapClientRpc(int mapIndex) => OnMapChanged?.Invoke(mapIndex);

        [ServerRpc(RequireOwnership = false)]
        public void SelectDifficultyServerRpc(int difficulty)
        {
            if (!NetAuthority.IsHost) return;
            SyncDifficultyClientRpc(difficulty);
        }

        [ClientRpc]
        private void SyncDifficultyClientRpc(int difficulty) => OnDifficultyChanged?.Invoke(difficulty);

        [ServerRpc(RequireOwnership = false)]
        public void SelectLobbyPickServerRpc(int peerId, int character, int can, int slipper)
        {
            if (!NetAuthority.IsHost) return;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby != null)
            {
                lobby.SetPicks(peerId, character, can, slipper);
                BroadcastLobbyPicks();
            }
        }

        public void BroadcastLobbyPicks()
        {
            if (!NetAuthority.IsHost) return;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return;

            var table = new int[Core.Balance.PlayerCount * 4];
            for (int i = 0; i < table.Length; i++) table[i] = -1;

            foreach (var peer in lobby.Peers)
            {
                if (peer.Seat >= 0 && peer.Seat < Core.Balance.PlayerCount)
                {
                    table[peer.Seat * 4] = peer.Seat;
                    table[peer.Seat * 4 + 1] = peer.CharacterPick;
                    table[peer.Seat * 4 + 2] = peer.CanPick;
                    table[peer.Seat * 4 + 3] = peer.SlipperPick;
                }
            }

            SyncLobbyPicksClientRpc(table);
        }

        [ClientRpc]
        private void SyncLobbyPicksClientRpc(int[] table) => OnLobbyPicksSynced?.Invoke(table);

        // -------------------------------------------------------------------
        // PICKS
        //
        // ⚠️⚠️ THE WHOLE TABLE IS SENT, NOT A DELTA, AND THAT IS WHAT MAKES LATE JOIN WORK.
        // A peer arriving mid-match missed every individual pick message that ever went out;
        // broadcasting the full table on any change means a late joiner is correct after one
        // message instead of needing a replay of the session's history.
        // -------------------------------------------------------------------

        /// <summary>slot, character, can, slipper: flattened, four ints per seat.</summary>
        [ClientRpc]
        public void SyncPicksClientRpc(int[] table)
        {
            if (table == null) return;

            for (int i = 0; i + 3 < table.Length; i += 4)
            {
                var who = Unit(table[i]);
                if (who == null) continue;

                who.CharacterIndex = table[i + 1];

                // ⚠️ THE CAN AND SLIPPER PICKS BELONG TO THE PROPS, NOT THE PERSON. A seat's
                // can skin is worn by the lata on the round that seat defends, and its
                // slipper skin by that seat's own tsinelas.
                ApplySlipperSkin(table[i], table[i + 3]);
            }
        }

        /// <summary>Called host-side whenever anything about the picks changes.</summary>
        public void BroadcastPicks()
        {
            if (!NetAuthority.IsHost) return;

            var round = GameServices.Round;
            if (round == null) return;

            var table = new int[Core.Balance.PlayerCount * 4];

            for (int slot = 0; slot < Core.Balance.PlayerCount; slot++)
            {
                var who = round.PlayerAt(slot);

                table[slot * 4] = slot;
                table[slot * 4 + 1] = who != null ? who.CharacterIndex : -1;
                table[slot * 4 + 2] = SkinOfLataFor(slot);
                table[slot * 4 + 3] = SkinOfSlipperFor(slot);
            }

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
        // PROP AND WORLD STATE REPLICATION (N8)
        // -------------------------------------------------------------------

        [ClientRpc]
        public void SyncLataClientRpc(Vector3 pos, Quaternion rot, bool isUpright, int skinIndex)
        {
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;

            lata.ApplySnapshotState(pos, rot, isUpright, skinIndex);
        }

        [ClientRpc]
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

        [ClientRpc]
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

            var scores = new int[Core.Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = match.ScoreFor(i);

            SyncWorldSnapshotClientRpc(
                match.RoundNumber,
                match.DefenderSlot,
                round != null ? round.TimeLeft : Core.Balance.RoundTime,
                scores,
                match.MatchInProgress,
                round != null && round.RoundActive);

            if (round?.Lata != null)
            {
                var l = round.Lata;
                SyncLataClientRpc(l.transform.position, l.transform.rotation, l.IsUpright, l.SkinIndex);
            }

            for (int slot = 0; slot < Core.Balance.PlayerCount; slot++)
            {
                var s = FindSlipper(slot);
                if (s != null)
                {
                    int holderSlot = s.Holder != null ? s.Holder.PlayerSlot : -1;
                    SyncSlipperClientRpc(s.OwnerSlot, holderSlot, s.transform.position,
                        s.transform.rotation, (int)s.State, s.Velocity, s.PektusSpin,
                        (int)s.Affinity, s.ThrowerSlot);
                }
            }
        }

        // -------------------------------------------------------------------
        // LATE JOIN AND DISCONNECT (N9)
        // -------------------------------------------------------------------

        /// <summary>
        /// A peer that arrives mid-match or reconnects to reclaim a seat.
        ///
        /// ⚠️ IT IS SPAWNED ONCE AND ONLY ONCE. The original guards on a spawned-peer set
        /// because the connect and the identify both fire, and a peer spawned twice is two
        /// bodies answering one set of keys.
        /// </summary>
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
                    // Reclaiming seat: remove AI controller if it was active
                    var ai = unit.GetComponent<AIController>();
                    if (ai != null) Destroy(ai);

                    unit.IsBot = false;
                    unit.PlayerName = peerRecord.Name;
                }
            }

            HostSyncPeer(peerId);
        }

        /// <summary>
        /// Called by a client after its arena objects exist. Transport connection can finish
        /// before the client-controlled SceneFlow has built its seats, so the connection-time
        /// snapshot alone is not sufficient for a cold app relaunch.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestWorldSnapshotServerRpc(ServerRpcParams rpcParams = default)
        {
            HostSyncPeer((int)rpcParams.Receive.SenderClientId);
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
                var target = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { (ulong)peerId }
                    }
                };
                RebindLocalSeatClientRpc(
                    peerRecord.Seat,
                    match != null ? match.DefenderSlot : -1,
                    round != null && round.RoundActive,
                    peerRecord.Name,
                    target);
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

                // AI takeover on the disconnected peer's seat so match continues smoothly
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

        /// <summary>
        /// The host tells only the reconnecting process which seat it controls. The world
        /// bodies are scene objects rather than NetworkObjects, so Netcode cannot transfer
        /// ownership for us; input, camera, HUD, and role presentation must be rebound as one
        /// atomic operation.
        /// </summary>
        [ClientRpc]
        private void RebindLocalSeatClientRpc(int seat, int defenderSlot, bool roundActive,
                                              string playerName,
                                              ClientRpcParams rpcParams = default)
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

        private readonly System.Collections.Generic.HashSet<int> _spawned =
            new System.Collections.Generic.HashSet<int>();
    }
}
