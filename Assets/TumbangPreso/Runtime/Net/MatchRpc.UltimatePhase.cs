using System;
using System.Collections.Generic;
using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private long _ultimateRequestSequence;
        private readonly Dictionary<ulong, long> _lastUltimateRequest = new Dictionary<ulong, long>();
        public long RequestSharedUltimate(int seat, Vector3 position, Vector3 forward, Vector3 aim, float held)
        {
            if (_nm?.CustomMessagingManager == null || !ValidSlot(seat)) return 0;
            long request = ++_ultimateRequestSequence;
            var pet = Familiar(seat);
            var cast = new UltimateCommit(seat, request, position, forward, aim, held, pet != null, pet != null ? pet.transform.position : Vector3.zero);
            if (NetAuthority.ShouldResolve())
            { Unit(seat)?.AbilitySystem?.AcceptSharedUltimate(cast); return request; }
            using var writer = new FastBufferWriter(96, Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId); writer.WriteValueSafe(GameServices.Match?.RoundNumber ?? 0);
            WriteUltimateCommit(writer, cast);
            _nm.CustomMessagingManager.SendNamedMessage("ReqUltimate", NetworkManager.ServerClientId, writer);
            return request;
        }
        private static void WriteUltimateCommit(FastBufferWriter writer, UltimateCommit cast)
        {
            writer.WriteValueSafe(cast.Seat); writer.WriteValueSafe(cast.Request);
            writer.WriteValueSafe(cast.Position); writer.WriteValueSafe(cast.Forward);
            writer.WriteValueSafe(cast.Aim); writer.WriteValueSafe(cast.Held);
            writer.WriteValueSafe(cast.HasFamiliar); writer.WriteValueSafe(cast.FamiliarPosition);
        }
        private static UltimateCommit ReadUltimateCommit(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat); reader.ReadValueSafe(out long request);
            reader.ReadValueSafe(out Vector3 position); reader.ReadValueSafe(out Vector3 forward);
            reader.ReadValueSafe(out Vector3 aim); reader.ReadValueSafe(out float held);
            reader.ReadValueSafe(out bool hasFamiliar); reader.ReadValueSafe(out Vector3 familiarPosition);
            return new UltimateCommit(seat, request, position, forward, aim, held, hasFamiliar, familiarPosition);
        }
        private static bool ValidUltimateCommit(UltimateCommit cast) => ValidSlot(cast.Seat) && cast.Request >= 0
            && Finite(cast.Position) && Finite(cast.Forward) && cast.Forward.sqrMagnitude > .001f
            && Finite(cast.Aim) && Finite(cast.Held) && cast.Held >= 0 && cast.Held <= 30
            && (!cast.HasFamiliar || Finite(cast.FamiliarPosition));
        private void OnReqUltimateMsg(ulong sender, FastBufferReader reader)
        {
            if (!NetAuthority.ShouldResolve() || !reader.TryBeginRead(77)) return;
            reader.ReadValueSafe(out long match); reader.ReadValueSafe(out int round);
            var cast = ReadUltimateCommit(ref reader);
            if (!SenderOwnsClaimedSeat(sender, cast.Seat, out var actor)) return;
            if (match != PresentationMatchId || round != GameServices.Match?.RoundNumber
                || cast.Request <= 0 || !ValidUltimateCommit(cast) || !PlausibleIntentPose(actor, cast.Position))
            { DenySharedUltimate(sender, cast.Request, cast.Seat); return; }
            if (_lastUltimateRequest.TryGetValue(sender, out long last) && cast.Request <= last)
            {
                var phase = SharedUltimatePhase.Instance;
                bool accepted = false;
                if (phase != null && phase.Active && cast.Request == last)
                    foreach (var prior in phase.Commits) if (prior.Request == cast.Request && prior.Seat == cast.Seat) accepted = true;
                if (accepted) BroadcastUltimatePhase(phase, sender);
                else DenySharedUltimate(sender, cast.Request, cast.Seat);
                return;
            }
            _lastUltimateRequest[sender] = cast.Request;
            if (actor.AbilitySystem?.CheckSharedUltimate(cast) != HeroKit.CastOutcome.Cast)
            { DenySharedUltimate(sender, cast.Request, cast.Seat); return; }
            var pet = Familiar(cast.Seat);
            if (pet != null)
            {
                if (pet.IsPossessed && (!cast.HasFamiliar || !pet.AcceptFlightPose(cast.FamiliarPosition, pet.transform.eulerAngles.y)))
                { DenySharedUltimate(sender, cast.Request, cast.Seat); return; }
                cast = new UltimateCommit(cast.Seat, cast.Request, cast.Position, cast.Forward, cast.Aim, cast.Held, true, pet.transform.position);
            }
            else if (cast.HasFamiliar) { DenySharedUltimate(sender, cast.Request, cast.Seat); return; }
            var outcome = actor.AbilitySystem?.AcceptSharedUltimate(cast) ?? HeroKit.CastOutcome.Missing;
            if (outcome != HeroKit.CastOutcome.Cast) DenySharedUltimate(sender, cast.Request, cast.Seat);
            else BroadcastAbilityState(cast.Seat, actor);
        }
        private void DenySharedUltimate(ulong client, long request, int seat)
        {
            if (_nm?.CustomMessagingManager == null || request <= 0) return;
            using var writer = new FastBufferWriter(24, Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId); writer.WriteValueSafe(request); writer.WriteValueSafe(seat);
            _nm.CustomMessagingManager.SendNamedMessage("UltDenied", client, writer);
        }
        private void OnUltimateDeniedMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !reader.TryBeginRead(20)) return;
            reader.ReadValueSafe(out long match); reader.ReadValueSafe(out long request); reader.ReadValueSafe(out int seat);
            if (match != PresentationMatchId || seat != NetAuthority.LocalSlot || !ValidSlot(seat)) return;
            Unit(seat)?.AbilitySystem?.RefuseSharedUltimate(request);
        }
        public void BroadcastUltimatePhase(SharedUltimatePhase phase, ulong? client = null)
        {
            if (!NetAuthority.ShouldResolve() || phase == null || !phase.Active || !phase.Sealed || phase.Commits.Count < 1
                || _nm?.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(512, Allocator.Temp);
            writer.WriteValueSafe(phase.MatchId); writer.WriteValueSafe(phase.Round);
            writer.WriteValueSafe(phase.PhaseId); writer.WriteValueSafe(phase.Began);
            writer.WriteValueSafe(PresentationClock.RequestedScale);writer.WriteValueSafe(phase.FrozenRoundTime); writer.WriteValueSafe(phase.Commits.Count);
            foreach (var cast in phase.Commits) WriteUltimateCommit(writer, cast);
            if (client.HasValue) _nm.CustomMessagingManager.SendNamedMessage("UltimatePhase", client.Value, writer);
            else _nm.CustomMessagingManager.SendNamedMessageToAll("UltimatePhase", writer);
        }
        private void OnUltimatePhaseMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !reader.TryBeginRead(40)) return;
            reader.ReadValueSafe(out long match); reader.ReadValueSafe(out int round);
            reader.ReadValueSafe(out long phase); reader.ReadValueSafe(out double began);
            reader.ReadValueSafe(out float resume);reader.ReadValueSafe(out float frozen); reader.ReadValueSafe(out int count);
            if (match != PresentationMatchId || round < 1 || round > 64 || phase <= 0
                || double.IsNaN(began) || double.IsInfinity(began) || !Finite(resume) || resume < 0 || resume > 1
                || !Finite(frozen)||frozen<0||frozen>Core.CustomGameRules.MaxRoundSeconds
                || count < 1 || count > 4 || !reader.TryBeginRead(count * 65)) return;
            var commits = new UltimateCommit[count]; int seats = 0;
            for (int i = 0; i < count; i++)
            {
                var cast = ReadUltimateCommit(ref reader);
                if (!ValidUltimateCommit(cast) || (seats & (1 << cast.Seat)) != 0) return;
                seats |= 1 << cast.Seat; commits[i] = cast;
            }
            SharedUltimatePhase.Ensure()?.Receive(match, round, phase, began, resume, commits,frozen);
        }
    }
}
