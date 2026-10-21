using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// SOULBOUND's reclaim on the wire (protocol 175). It is the can reset's split: Nemu's own peer shows its bar
    /// (`NemuHeroKit.StepReclaim`) and reports Start, Cancel and Complete; the host times the channel itself, re-checks that
    /// she can act and is still in reach of that dormant ghost, and only then reclaims it. The ghost frames carry the result.
    /// </summary>
    public sealed partial class MatchRpc
    {
        public enum ReclaimPhase : byte { Start, Cancel, Complete }

        /// <summary>A little slack on the reach and the clock for a pose and a press that crossed the wire.</summary>
        private const float ReclaimReachLeeway = 0.5f, ReclaimClockLeeway = 0.1f;

        private readonly Dictionary<int, (NemuDormant.Ghost Kind, float StartedAt)> _reclaimStart =
            new Dictionary<int, (NemuDormant.Ghost, float)>();

        public void RequestNemuReclaim(int slot, NemuDormant.Ghost kind, ReclaimPhase phase)
        {
            if (NetAuthority.IsHost)
            {
                HostApplyReclaim(slot, kind, phase);
                return;
            }
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe((byte)kind);
            writer.WriteValueSafe((byte)phase);
            writer.WriteNetworkSerializable(CaptureActionScope(slot));
            _nm.CustomMessagingManager.SendNamedMessage("ReqReclaim", NetworkManager.ServerClientId, writer);
        }

        private void OnReqReclaimMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost || !reader.TryBeginRead(6 + GameplayActionScope.WireBytes)) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out byte kind);
            reader.ReadValueSafe(out byte phase);
            if (kind < 1 || kind > 3 || phase > (byte)ReclaimPhase.Complete) return;
            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)
                || !ReadCurrentActionScope(ref reader, who, out _)) return;
            HostApplyReclaim(slot, (NemuDormant.Ghost)kind, (ReclaimPhase)phase);
        }

        private void HostApplyReclaim(int slot, NemuDormant.Ghost kind, ReclaimPhase phase)
        {
            if (!NetAuthority.IsHost) return;
            var dormant = HostReclaimable(slot, kind);
            if (phase == ReclaimPhase.Cancel || dormant == null)
            {
                CloseReclaim(slot);
                return;
            }
            if (phase == ReclaimPhase.Start)
            {
                if (!_reclaimStart.TryGetValue(slot, out var open) || open.Kind != kind)
                {
                    CloseReclaim(slot);
                    _reclaimStart[slot] = (kind, Time.time);
                    BroadcastActionExceptOwner(slot, "grab");
                }
                return;
            }
            if (!_reclaimStart.TryGetValue(slot, out var started) || started.Kind != kind
                || Time.time - started.StartedAt < NemuRules.ReclaimSeconds - ReclaimClockLeeway) return;
            _reclaimStart.Remove(slot);
            dormant.Reclaim();
        }

        private void CloseReclaim(int slot)
        {
            if (!_reclaimStart.TryGetValue(slot, out var open)) return;
            _reclaimStart.Remove(slot);
            var dormant = NemuDormant.For(slot, open.Kind);
            if (dormant != null) dormant.Progress = 0f;
        }

        /// <summary>Her dormant ghost of this kind, if she may reclaim it now: she is Nemu, can act, and stands in reach.</summary>
        private NemuDormant HostReclaimable(int slot, NemuDormant.Ghost kind)
        {
            var who = Unit(slot);
            if (who == null || !(who.AbilitySystem?.Kit is NemuHeroKit) || !who.CanAct()
                || GameServices.Round?.RoundActive != true) return null;
            var dormant = NemuDormant.For(slot, kind);
            if (dormant == null) return null;
            Vector3 a = who.transform.position, b = dormant.Position;
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b) <= NemuRules.ReclaimRadius + ReclaimReachLeeway ? dormant : null;
        }

        /// <summary>Drops a channel whose conditions stopped holding, and shows its progress on the ghost for every peer.</summary>
        private void HostStepReclaimChannels()
        {
            if (_reclaimStart.Count == 0) return;
            List<int> dead = null;
            foreach (var kv in _reclaimStart)
            {
                var dormant = HostReclaimable(kv.Key, kv.Value.Kind);
                if (dormant == null) { (dead ??= new List<int>()).Add(kv.Key); continue; }
                dormant.Progress = Mathf.Clamp01((Time.time - kv.Value.StartedAt) / NemuRules.ReclaimSeconds);
            }
            if (dead == null) return;
            foreach (int slot in dead) CloseReclaim(slot);
        }
    }
}
