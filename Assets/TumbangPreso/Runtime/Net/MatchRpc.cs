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
            base.OnNetworkSpawn();
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
        public void BeginCountdownClientRpc() { }
    }
}
