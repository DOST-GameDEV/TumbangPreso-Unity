using System.Collections.Generic;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// ⚠️⚠️ COMPANION BODIES ON EVERY PEER (HERO-10 v3, plan 9.12; `docs/SKILL_NETWORK_CONTRACT.md` "Companion bodies"). Phaister's
    /// VOODOO DOLL is a real body in a companion seat (`Core.CompanionSeats`: 4 + its owner). The HOST owns it: it spawns it, runs its
    /// brain and resolves everything it does. Every other peer builds a REPLICA with no brain from one message, `CompanionSet`: the
    /// whole list of live companions (seat, kind, where, facing) scoped to the match and round, sent whenever the list changes and to
    /// every peer that asks for a snapshot. A replica is kept or made for each seat in the list and every other companion goes.
    ///
    /// After that the doll is just a body: its pose rides `SyncUnit`, its actions `PlayAction` and `ThrowCharge`, its slipper
    /// `SyncSlipper`/`SlipperPose`, a teleport `Teleport`, all keyed by its seat, which `ValidBody` admits and `Unit` (the round's
    /// `BodyAt`) resolves. Ability, score and seat-ownership messages stay four seats wide (`ValidSlot`): a companion has no kit,
    /// its points are its owner's before they are ever sent, and no peer can claim its seat.
    /// </summary>
    public sealed partial class MatchRpc
    {
        /// <summary>The kinds of companion a `CompanionSet` can name.</summary>
        private const byte CompanionVoodooDoll = 1;

        private RoundDirector _companionRound;

        /// <summary>Any body's seat: a player's or a companion's.</summary>
        private static bool ValidBody(int seat) => CompanionSeats.IsBody(seat);

        /// <summary>The host listens to its round's companion list (re-hooked if the round service is replaced).</summary>
        private void HookCompanions()
        {
            var round = GameServices.Round;
            if (ReferenceEquals(round, _companionRound)) return;
            if (_companionRound != null) _companionRound.CompanionsChanged -= HostBroadcastCompanions;
            _companionRound = round;
            if (round != null) round.CompanionsChanged += HostBroadcastCompanions;
        }

        private void HostBroadcastCompanions() => SendCompanionSet(null);

        /// <summary>The live companions, to everybody or to one peer (a snapshot reply).</summary>
        private void SendCompanionSet(ulong? only)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            var round = GameServices.Round;
            if (round == null) return;
            int count = 0;
            foreach (var c in round.Companions) if (c != null) count++;
            using var writer = new FastBufferWriter(32 + count * 24, Allocator.Temp);
            writer.WriteValueSafe(EnsurePresentationMatch());
            writer.WriteValueSafe(GameServices.Match?.RoundNumber ?? 0);
            writer.WriteValueSafe((byte)count);
            foreach (var c in round.Companions)
            {
                if (c == null) continue;
                writer.WriteValueSafe(c.PlayerSlot);
                writer.WriteValueSafe(CompanionVoodooDoll);
                writer.WriteValueSafe(c.transform.position);
                writer.WriteValueSafe(c.transform.eulerAngles.y);
            }
            if (only.HasValue) _nm.CustomMessagingManager.SendNamedMessage("CompanionSet", only.Value, writer);
            else _nm.CustomMessagingManager.SendNamedMessageToAll("CompanionSet", writer);
        }

        /// <summary>
        /// A replica for every companion the host lists and none for any other. ⚠️ A list from an EARLIER round than this peer is
        /// in is stale and ignored (the host sends a fresh one at every change); a later round is accepted, because a peer can be a
        /// frame behind the host at a round boundary and the host's list is the truth.
        /// </summary>
        private void OnCompanionSetMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId)) return;
            if (!reader.TryBeginRead(sizeof(long) + sizeof(int) + 1)) return;
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int roundNumber);
            reader.ReadValueSafe(out byte count);
            var round = GameServices.Round;
            if (round == null || match != PresentationMatchId || count > Balance.PlayerCount) return;
            if (roundNumber < (GameServices.Match?.RoundNumber ?? 0)) return;

            var listed = new List<(int seat, Vector3 at, float yaw)>(count);
            for (int i = 0; i < count; i++)
            {
                if (!reader.TryBeginRead(sizeof(int) + 1 + 12 + 4)) return;
                reader.ReadValueSafe(out int seat);
                reader.ReadValueSafe(out byte kind);
                reader.ReadValueSafe(out Vector3 at);
                reader.ReadValueSafe(out float yaw);
                if (!CompanionSeats.IsCompanion(seat) || kind != CompanionVoodooDoll || !Finite(at) || !Finite(yaw)) return;
                listed.Add((seat, at, yaw));
            }

            var keep = new HashSet<int>();
            foreach (var (seat, at, yaw) in listed)
            {
                keep.Add(seat);
                if (round.BodyAt(seat) != null) continue;
                var owner = round.PlayerAt(CompanionSeats.OwnerOf(seat));
                if (owner != null) Abilities.VoodooDollBody.Spawn(owner, at, yaw, brain: false);
            }
            var leaving = new List<CharacterMotor>();
            foreach (var c in round.Companions) if (c != null && !keep.Contains(c.PlayerSlot)) leaving.Add(c);
            foreach (var c in leaving)
            {
                round.UnregisterCompanion(c);
                Destroy(c.gameObject);
            }
        }
    }
}
