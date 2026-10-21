using System;
using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly long[] _nemuSent = new long[Balance.PlayerCount], _nemuReceived = new long[Balance.PlayerCount];
        private readonly bool[] _nemuWasLive = new bool[Balance.PlayerCount];
        private readonly Dictionary<long, Component>[] _nemuReplicas = new Dictionary<long, Component>[Balance.PlayerCount];
        private readonly GameplayActionScope[] _nemuScopes = new GameplayActionScope[Balance.PlayerCount];
        private float _nextNemuFrame;
        private long _nemuReceivedMatch;
        public void CancelNemuGhostPresentation() => ResetNemuGhostTransport();

        private void ResetNemuGhostTransport()
        {
            for (int i = 0; i < Balance.PlayerCount; i++) ClearNemuReplicas(i);
            Array.Clear(_nemuReceived, 0, _nemuReceived.Length); _nemuReceivedMatch = 0; _nextNemuFrame = 0;
        }
        private void ClearNemuReplicas(int seat)
        {
            if (_nemuReplicas[seat] == null) return;
            foreach (var replica in _nemuReplicas[seat].Values) RetireNemuReplica(replica);
            _nemuReplicas[seat].Clear();
        }
        private static void RetireNemuReplica(Component replica)
        { if (replica != null) { replica.gameObject.SetActive(false); Destroy(replica.gameObject); } }

        private NemuGhostFrame CaptureNemuGhosts(int seat)
        {
            var body = Unit(seat);
            var poses = new List<NemuGhostPose>(NemuGhostFrame.MaxGhosts);
            if (body?.AbilitySystem?.Kit is NemuHeroKit && GameServices.Round?.RoundActive == true)
            {
                foreach (var ghost in NemuBlindfold.Live)
                    if (ghost != null && ghost.isActiveAndEnabled && ghost.OwnerSlot == seat) poses.Add(ghost.CaptureNetwork());
                foreach (var ghost in NemuSkitter.Live)
                    if (ghost != null && ghost.isActiveAndEnabled && ghost.OwnerSlot == seat) poses.Add(ghost.CaptureNetwork());
                foreach (var ghost in NemuLookout.Live)
                    if (ghost != null && ghost.isActiveAndEnabled && ghost.OwnerSlot == seat) poses.Add(ghost.CaptureNetwork());
                foreach (var ghost in NemuDormant.Live)
                    if (ghost != null && ghost.isActiveAndEnabled && ghost.OwnerSlot == seat) poses.Add(ghost.CaptureNetwork());
            }
            return new NemuGhostFrame { Seat = seat, Scope = new GameplayActionScope { Match = EnsurePresentationMatch(),
                Round = GameServices.Match?.RoundNumber ?? 0, Epoch = body?.MovementEpoch ?? 0 },
                Sequence = ++_nemuSent[seat], RoundClock = GameServices.Round?.TimeLeft ?? 0, Ghosts = poses.ToArray() };
        }
        private void SendNemuGhostFrame(NemuGhostFrame frame, ulong? peer = null)
        {
            if (!frame.IsValid || _nm?.CustomMessagingManager == null || !_nm.IsServer) return;
            using var writer = new FastBufferWriter(NemuGhostFrame.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(frame);
            if (peer.HasValue) _nm.CustomMessagingManager.SendNamedMessage("NemuGhosts", peer.Value, writer, NetworkDelivery.ReliableSequenced);
            else _nm.CustomMessagingManager.SendNamedMessageToAll("NemuGhosts", writer, NetworkDelivery.ReliableSequenced);
        }
        private void SendNemuGhostSnapshot(int seat, ulong peer)
        { if (NetAuthority.IsHost && ValidSlot(seat)) SendNemuGhostFrame(CaptureNemuGhosts(seat), peer); }
        private void TickNemuGhosts()
        {
            if (!NetAuthority.IsHost)
            {
                for (int seat = 0; seat < Balance.PlayerCount; seat++)
                {
                    var body = Unit(seat);
                    if (body == null || !(body.AbilitySystem?.Kit is NemuHeroKit) || GameServices.Round?.RoundActive != true
                        || !_nemuScopes[seat].Matches(PresentationMatchId, GameServices.Match?.RoundNumber ?? -1, body.MovementEpoch))
                        ClearNemuReplicas(seat);
                }
                return;
            }
            if (_nm?.CustomMessagingManager == null || !_nm.IsServer || Time.unscaledTime < _nextNemuFrame) return;
            _nextNemuFrame = Time.unscaledTime + .1f;
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                if (!_nemuWasLive[seat] && !(Unit(seat)?.AbilitySystem?.Kit is NemuHeroKit)) continue;
                var frame = CaptureNemuGhosts(seat);
                bool active = frame.Ghosts.Length > 0;
                if (active || _nemuWasLive[seat]) SendNemuGhostFrame(frame);
                _nemuWasLive[seat] = active;
            }
        }
        private void OnNemuGhostsMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !NemuGhostFrame.TryRead(ref reader, out var frame)) return;
            ApplyNemuGhostFrame(frame);
        }
        private bool ApplyNemuGhostFrame(NemuGhostFrame frame)
        {
            if (!frame.IsValid || frame.Scope.Match != PresentationMatchId || frame.Scope.Round != (GameServices.Match?.RoundNumber ?? -1)) return false;
            var body = Unit(frame.Seat);
            if (body == null || frame.Scope.Epoch != body.MovementEpoch
                || (frame.Ghosts.Length > 0 && (!(body.AbilitySystem?.Kit is NemuHeroKit) || GameServices.Round?.RoundActive != true))) return false;
            if (_nemuReceivedMatch != frame.Scope.Match)
            { ResetNemuGhostTransport(); _nemuReceivedMatch = frame.Scope.Match; }
            if (frame.Sequence <= _nemuReceived[frame.Seat]) return false;
            var replicas = _nemuReplicas[frame.Seat] ??= new Dictionary<long, Component>();
            foreach (var pose in frame.Ghosts)
                if (replicas.TryGetValue(pose.Id, out var existing) && existing != null
                    && !(pose.Kind == 1 && existing is NemuBlindfold || pose.Kind == 2 && existing is NemuSkitter
                        || pose.Kind == 3 && existing is NemuLookout || pose.Kind == 4 && existing is NemuDormant)) return false;
            var missing = new List<long>(replicas.Keys);
            float elapsed = Mathf.Max(0, frame.RoundClock - (GameServices.Round?.TimeLeft ?? frame.RoundClock));
            foreach (var pose in frame.Ghosts)
            {
                // A delayed hydration cannot resurrect a lifetime that expired in simulation time.
                // A dormant ghost (kind 4) waits its window; faded (phase 3) it is leaving like any other.
                float limit = pose.Kind == 1 ? (pose.Phase == 1 ? Core.NecroRules.DontLookLifeSeconds :
                    pose.Phase == 2 ? Core.NecroRules.DontLookLungeSeconds + Core.NecroRules.DontLookBlindSeconds + .45f :
                    Core.NecroRules.DontLookBlindSeconds + .45f) : pose.Kind == 2 ? Core.NecroRules.FetchSeconds + .45f
                    : pose.Kind == 3 ? Core.NecroRules.LookoutSeconds + Core.NecroRules.LookoutRestoreSeconds + .6f : Core.NemuRules.DormantSeconds + .1f;
                if (pose.Phase == 4 || pose.Kind == 4 && pose.Phase == 3 ? pose.PhaseAge + elapsed >= (pose.Kind == 3 ? .6f : .45f)
                    : (pose.Kind == 1 && pose.Phase != 1 ? pose.PhaseAge : pose.Age) + elapsed >= limit) continue;
                missing.Remove(pose.Id);
                replicas.TryGetValue(pose.Id, out var replica);
                if (pose.Kind == 1)
                {
                    var ghost = replica as NemuBlindfold;
                    if (ghost == null) ghost = NemuBlindfold.Place(pose.Position, pose.Forward, frame.Seat, Mathf.Max(.01f, pose.Age));
                    ghost.ApplyNetwork(pose, elapsed); replicas[pose.Id] = ghost;
                }
                else if (pose.Kind == 2)
                {
                    var ghost = replica as NemuSkitter;
                    if (ghost == null) ghost = NemuSkitter.Summon(pose.Position, pose.Forward, frame.Seat);
                    ghost.ApplyNetwork(pose, elapsed); replicas[pose.Id] = ghost;
                }
                else if (pose.Kind == 3)
                {
                    var ghost = replica as NemuLookout;
                    if (ghost == null) ghost = NemuLookout.Summon(GameServices.Round?.Lata?.transform, pose.Position, frame.Seat);
                    ghost.ApplyNetwork(pose, elapsed); replicas[pose.Id] = ghost;
                }
                else
                {
                    var ghost = replica as NemuDormant;
                    if (ghost == null) ghost = NemuDormant.Spawn((NemuDormant.Ghost)pose.Flags, pose.Position, pose.Forward, frame.Seat);
                    ghost.ApplyNetwork(pose, elapsed); replicas[pose.Id] = ghost;
                }
            }
            foreach (long id in missing) { RetireNemuReplica(replicas[id]); replicas.Remove(id); }
            _nemuReceived[frame.Seat] = frame.Sequence; _nemuScopes[frame.Seat] = frame.Scope;
            return true;
        }
    }
}
