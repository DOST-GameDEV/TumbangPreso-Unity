using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Zack's two held commands on the wire (protocol 184, owner refinement 2026-10-26).
    ///
    /// * REEL ("ReqZackReel"): the Live Wire reel-in is Nemu's reclaim shape. His own peer shows the hold and reports Start,
    ///   Cancel and Complete; the host times the hold itself, re-checks that the wire is his, still strung and in reach, and only
    ///   then reels it in. The dynamic-field message carries the result to every peer.
    /// * ZAP ("ReqZackZap" up, "ZackZap" down): while Overclocked he holds the ultimate. His peer picks the rival it aims at and
    ///   reports it (on change and as a keep-alive). The host validates range, sight and aim every frame, runs the lock, Zaps,
    ///   spends the fuel, and broadcasts the channel and the fuel left. A missed keep-alive ends the channel, so a dropped
    ///   release can never leave a beam running.
    /// </summary>
    public sealed partial class MatchRpc
    {
        public enum ZackReelPhase : byte { Start, Cancel, Complete }

        private const float ZackReelClockLeeway = 0.1f;
        private readonly Dictionary<int, (int EventId, float StartedAt)> _zackReelStart = new Dictionary<int, (int, float)>();

        public void RequestZackReel(int slot, int eventId, ZackReelPhase phase)
        {
            if (!NetAuthority.IsNetworked || NetAuthority.IsHost) { HostApplyZackReel(slot, eventId, phase); return; }
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(32 + GameplayActionScope.WireBytes, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(eventId);
            writer.WriteValueSafe((byte)phase);
            writer.WriteNetworkSerializable(CaptureActionScope(slot));
            _nm.CustomMessagingManager.SendNamedMessage("ReqZackReel", NetworkManager.ServerClientId, writer);
        }

        private void OnReqZackReelMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost || !reader.TryBeginRead(9 + GameplayActionScope.WireBytes)) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out int eventId);
            reader.ReadValueSafe(out byte phase);
            if (eventId <= 0 || phase > (byte)ZackReelPhase.Complete) return;
            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)
                || !ReadCurrentActionScope(ref reader, who, out _)) return;
            HostApplyZackReel(slot, eventId, (ZackReelPhase)phase);
        }

        private void HostApplyZackReel(int slot, int eventId, ZackReelPhase phase)
        {
            if (!NetAuthority.ShouldResolve()) return;
            // A cancel ends only the hold it names; a request for a wire this seat cannot reel is ignored, so a stale or
            // forged id never knocks out a valid hold in progress.
            if (phase == ZackReelPhase.Cancel)
            {
                if (_zackReelStart.TryGetValue(slot, out var held) && held.EventId == eventId) _zackReelStart.Remove(slot);
                return;
            }
            var wire = ZackLiveWire.HostReelable(Unit(slot), eventId);
            if (wire == null) return;
            if (phase == ZackReelPhase.Start)
            {
                if (!_zackReelStart.TryGetValue(slot, out var open) || open.EventId != eventId)
                    _zackReelStart[slot] = (eventId, Time.time);
                return;
            }
            if (!_zackReelStart.TryGetValue(slot, out var started) || started.EventId != eventId
                || Time.time - started.StartedAt < ZackRules.LiveWireReelHoldSeconds - ZackReelClockLeeway) return;
            _zackReelStart.Remove(slot);
            wire.HostReelIn();
        }

        // ------------------------------------------------------------------ the Overclock zap

        public void RequestZackZap(int slot, bool active, int target)
        {
            if (!NetAuthority.IsNetworked || NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16 + GameplayActionScope.WireBytes, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(active);
            writer.WriteValueSafe((sbyte)Mathf.Clamp(target, -1, Balance.PlayerCount - 1));
            writer.WriteNetworkSerializable(CaptureActionScope(slot));
            _nm.CustomMessagingManager.SendNamedMessage("ReqZackZap", NetworkManager.ServerClientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void OnReqZackZapMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost || !reader.TryBeginRead(6 + GameplayActionScope.WireBytes)) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out bool active);
            reader.ReadValueSafe(out sbyte target);
            if (target < -1 || target >= Balance.PlayerCount) return;
            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)
                || !ReadCurrentActionScope(ref reader, who, out _)) return;
            if (who.AbilitySystem?.Kit is ZackHeroKit zack) zack.HostZapCommand(who, active, target);
        }

        /// <summary>⚠️ HOST ONLY. The channel as the host runs it, with the fuel left, to every client.</summary>
        public void BroadcastZackZap(int slot, ZackHeroKit.ZapState state)
        {
            if (!NetAuthority.IsNetworked || !NetAuthority.IsHost || _nm?.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId);
            writer.WriteValueSafe(GameServices.Match?.RoundNumber ?? -1);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(state.Active);
            writer.WriteValueSafe((sbyte)state.Target);
            writer.WriteValueSafe((byte)Mathf.RoundToInt(Mathf.Clamp01(state.Lock / ZackRules.ZapLockSeconds) * 255));
            writer.WriteValueSafe(state.Grounded);
            writer.WriteValueSafe(state.Fuel);
            _nm.CustomMessagingManager.SendNamedMessageToAll("ZackZap", writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnZackZapMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !reader.TryBeginRead(8 + 4 + 4 + 1 + 1 + 1 + 1 + 4)) return;
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int round);
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out bool active);
            reader.ReadValueSafe(out sbyte target);
            reader.ReadValueSafe(out byte lockByte);
            reader.ReadValueSafe(out bool grounded);
            reader.ReadValueSafe(out float fuel);
            if (match != PresentationMatchId || round != (GameServices.Match?.RoundNumber ?? -1) || !ValidSlot(slot)
                || target < -1 || target >= Balance.PlayerCount || !float.IsFinite(fuel)
                || fuel < 0.0f || fuel > ZackRules.OverclockSeconds + 0.01f) return;
            var who = Unit(slot);
            if (who?.AbilitySystem?.Kit is ZackHeroKit zack)
                zack.ApplyHostZap(who, new ZackHeroKit.ZapState
                {
                    Active = active, Target = target, Lock = lockByte / 255.0f * ZackRules.ZapLockSeconds,
                    Grounded = grounded, Fuel = fuel
                });
        }

        private void ResetZackTransport() => _zackReelStart.Clear();
    }
}
