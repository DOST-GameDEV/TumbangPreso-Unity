using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly int[] _flightGenerations = new int[Balance.PlayerCount];
        private readonly HeroKit[] _flightClockKits = new HeroKit[Balance.PlayerCount];
        private readonly bool[] _flightRefreshSent = new bool[Balance.PlayerCount];
        private readonly int[] _flightRefreshEpochs = new int[Balance.PlayerCount];
        private readonly long[] _flightRefreshEpisodes = new long[Balance.PlayerCount];
        private readonly long[] _flightRefreshRequests = new long[Balance.PlayerCount];
        private readonly long[] _flightRefreshEvents = new long[Balance.PlayerCount];
        private long _flightClockMatch;
        private int _flightClockRound;

        private void ResetFeatherfallSnapshots()
        {
            System.Array.Clear(_flightGenerations, 0, _flightGenerations.Length);
            System.Array.Clear(_flightClockKits, 0, _flightClockKits.Length);
            System.Array.Clear(_flightRefreshSent, 0, _flightRefreshSent.Length);
            _flightClockMatch = 0; _flightClockRound = 0;
        }

        private void ResetFeatherfallTransport()
        {
            ResetFeatherfallSnapshots();
            if (NetAuthority.IsHost) return;
            // Request IDs restart with the real message manager, not with a resnapshot.
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var actor = Unit(slot);
                if (!(actor?.AbilitySystem?.Kit is AmihanHeroKit kit)) continue;
                kit.CancelFeatherfall(actor);
                actor.InvalidateFlightEpisode();
            }
        }

        private static void IdentifyFeatherfallTakeoff(CharacterMotor actor, int slot, long episode)
        {
            if (slot == 1 && actor?.AbilitySystem?.Kit is AmihanHeroKit kit && !kit.IsDefending)
                actor.IdentifyFlightTakeoff(episode);
        }

        private static bool IsFeatherfallSlot(CharacterMotor actor, int slot)
            => slot == 1 && actor?.AbilitySystem?.Kit is AmihanHeroKit kit && !kit.IsDefending;

        private static bool ValidFeatherfallIntent(CharacterMotor actor, int slot, long intent)
        {
            if (intent == long.MinValue) return false;
            if (!IsFeatherfallSlot(actor, slot)) return intent == 0;
            if (actor.IsDefender) return false;
            var ability = actor.AbilitySystem.Kit.AttackingSkill;
            return intent == 0 ? !ability.IsActive && !actor.IsFlying
                : intent == actor.FlightEpisode && ability.IsActive && actor.IsAloft;
        }

        private static void ApplyFeatherfallRecast(CharacterMotor actor, long episode)
        {
            if (episode == 0 || actor == null || actor.FlightEpisode != episode || !(actor.AbilitySystem?.Kit is AmihanHeroKit kit)) return;
            var context = new AbilityContext(actor, actor.GetComponent<Carrier>(), actor.GetComponent<CombatVerbs>());
            kit.AttackingSkill.EndEarly(context);
            actor.EndFlight();
            actor.GetComponent<Visual.CharacterAnimator>()?.CancelHeroAction("hero-amihan-updraft", "updraft-lift");
        }

        private void AdoptFeatherfallRoundClock(int round)
        {
            if (_flightClockMatch != PresentationMatchId || _flightClockRound != round) ResetFeatherfallSnapshots();
            _flightClockMatch = PresentationMatchId; _flightClockRound = round;
            for (int i = 0; i < _flightClockKits.Length; i++) _flightClockKits[i] = Unit(i)?.AbilitySystem?.Kit;
        }

        private void SendFeatherfallSnapshot(int slot, ulong peer, int generation, AmihanHeroKit kit)
        {
            var actor = Unit(slot);
            var round = GameServices.Round;
            if (actor == null || round == null) return;
            PrepareSkillReceipts();
            byte phase = actor.FlightPhase;
            if (!round.RoundActive || actor.IsDefender) phase = 0;
            float remaining = phase == 1 ? kit.AttackingSkill.DurationRemaining : 0;
            long processedRequest = _lastSkillRequest.TryGetValue(peer, out var request) ? request.request : 0;
            using var writer = new FastBufferWriter(160, Allocator.Temp);
            // Keep the common TimedKit header. Only Amihan reads the appended fields.
            writer.WriteValueSafe(slot); writer.WriteValueSafe(GameServices.Match.RoundNumber);
            writer.WriteValueSafe(kit.HeroId); writer.WriteValueSafe(remaining);
            writer.WriteValueSafe(0f); writer.WriteValueSafe((float)_nm.ServerTime.Time); writer.WriteValueSafe(false);
            writer.WriteValueSafe(PresentationMatchId); writer.WriteValueSafe(generation);
            writer.WriteValueSafe(actor.MovementEpoch); writer.WriteValueSafe(_skillEventSequence);
            writer.WriteValueSafe(processedRequest); writer.WriteValueSafe(round.TimeLeft);
            writer.WriteValueSafe(actor.FlightCeiling); writer.WriteValueSafe(phase);
            writer.WriteValueSafe(_unitPoseSerial[slot]);
            writer.WriteValueSafe(actor.FlightEpisode);
            _nm.CustomMessagingManager.SendNamedMessage("TimedKit", peer, writer, NetworkDelivery.ReliableSequenced);
        }

        private void ReadFeatherfallSnapshot(ref FastBufferReader reader, int slot, int round, float remaining,
            float ultimateRemaining, float sentAt, bool ultimatePending)
        {
            if (!reader.TryBeginRead(57)) return;
            reader.ReadValueSafe(out long match); reader.ReadValueSafe(out int generation);
            reader.ReadValueSafe(out int epoch); reader.ReadValueSafe(out long eventWatermark);
            reader.ReadValueSafe(out long requestWatermark); reader.ReadValueSafe(out float capturedClock);
            reader.ReadValueSafe(out float ceiling); reader.ReadValueSafe(out byte phase);
            reader.ReadValueSafe(out ulong poseSerial);
            reader.ReadValueSafe(out long episode);
            if (!ValidSlot(slot) || match <= 0 || match != PresentationMatchId || generation <= 0 || epoch < 0
                || eventWatermark < 0 || requestWatermark < 0 || phase > 2 || poseSerial == 0
                || episode == long.MinValue || (phase != 0 && episode == 0) || (episode < 0 && -episode > eventWatermark)
                || ultimateRemaining != 0 || ultimatePending || !Finite(sentAt)
                || !Finite(remaining) || remaining < 0 || remaining > AmihanRules.UpdraftSeconds
                || !Finite(capturedClock) || capturedClock < 0 || capturedClock > CustomGameRules.MaxRoundSeconds
                || !Finite(ceiling) || Mathf.Abs(ceiling) > 256 || (phase != 1 && remaining != 0)) return;
            var actor = Unit(slot);
            var director = GameServices.Round;
            if (actor == null || !(actor.AbilitySystem?.Kit is AmihanHeroKit kit) || director == null
                || GameServices.Match?.RoundNumber != round || epoch != actor.MovementEpoch
                || (phase != 0 && (actor.IsDefender || !director.RoundActive))) return;
            if (generation <= _flightGenerations[slot]) return;
            PrepareSkillReceipts();
            if (eventWatermark < _lastSkillEvent[slot]
                || (slot == NetAuthority.LocalSlot && _skillRequestSequence > _skillRequestScopeFloor
                    && requestWatermark < _skillRequestSequence))
            {
                RefreshFeatherfallOnce(slot, round, epoch);
                return;
            }
            if (_flightClockMatch != match || _flightClockRound != round
                || generation != _lastWorldFieldGeneration || _worldFieldRound != round
                || _flightClockKits[slot] != kit)
            {
                RefreshFeatherfallOnce(slot, round, epoch);
                return;
            }
            float live = AmihanRules.FlightRemainingAtClock(remaining, capturedClock, director.TimeLeft);
            // Valid identity must still reach an interrupted owner. Cancelling motion does not
            // assert ground contact, and a terminal episode can never be lifted a second time.
            if (actor.FlightEpisodeIsTerminal(episode) || actor.IsRooted || actor.IsTagged || actor.IsTripped
                || actor.IsSwimming || actor.IsEdgeRecovering)
            {
                phase = 0;
                live = 0;
            }
            using (NetCue.SuppressRelay())
            {
                if (!kit.RestoreFeatherfall(actor, live, ceiling, phase, poseSerial, episode))
                {
                    RefreshFeatherfallOnce(slot, round, epoch);
                    return;
                }
            }
            _flightGenerations[slot] = generation;
            _lastSkillEvent[slot] = System.Math.Max(_lastSkillEvent[slot], eventWatermark);
            if (phase == 1 && live <= 0) RefreshFeatherfallOnce(slot, round, epoch);
        }

        private bool RecordFeatherfallRefresh(int slot, int epoch, long episode, long request, long skillEvent)
        {
            if (_flightRefreshSent[slot] && _flightRefreshEpochs[slot] == epoch && _flightRefreshEpisodes[slot] == episode
                && request <= _flightRefreshRequests[slot] && skillEvent <= _flightRefreshEvents[slot]) return false;
            _flightRefreshSent[slot] = true;
            _flightRefreshEpochs[slot] = epoch; _flightRefreshEpisodes[slot] = episode;
            _flightRefreshRequests[slot] = request; _flightRefreshEvents[slot] = skillEvent;
            return true;
        }

        private void RefreshFeatherfallOnce(int slot, int round, int epoch)
        {
            if (!isActiveAndEnabled || _nm == null || !_nm.IsConnectedClient || _nm.ShutdownInProgress || _nm.CustomMessagingManager == null) return;
            long request = slot == NetAuthority.LocalSlot && _skillRequestSequence > _skillRequestScopeFloor ? _skillRequestSequence : 0;
            // Budget follows observed actor/input progress, never the episode in repeated rejected packets.
            if (!RecordFeatherfallRefresh(slot, epoch, Unit(slot)?.FlightEpisode ?? 0, request, _lastSkillEvent[slot])) return;
            StartCoroutine(RefreshAfterExpiredPreparation(round));
        }
    }
}
