using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private const byte ViewVersion = SpectatorViewState.CurrentVersion;
        private const float ViewInterval = .1f;
        private readonly Dictionary<ulong, ulong> _viewPeers = new();
        private readonly Dictionary<ulong, byte> _viewVersions = new();
        private byte _viewVersion = SpectatorViewState.LegacyVersion;
        private readonly Dictionary<ulong, int> _viewWatchers = new();
        private readonly List<ulong> _retiredViewPeers = new();
        private readonly SpectatorViewState[] _pendingViews = new SpectatorViewState[Core.Balance.PlayerCount];
        private readonly bool[] _hasPendingView = new bool[Core.Balance.PlayerCount];
        private readonly uint[] _viewOwnerSequence = new uint[Core.Balance.PlayerCount], _viewRelaySequence = new uint[Core.Balance.PlayerCount];
        private readonly int[] _viewOwnerEpoch = new int[Core.Balance.PlayerCount];
        private readonly int[] _viewOwnerRound = new int[Core.Balance.PlayerCount];
        private readonly long[] _viewOwnerMatch = new long[Core.Balance.PlayerCount];
        private readonly ulong[] _viewOwnerNonce = new ulong[Core.Balance.PlayerCount];
        private readonly float[] _viewCredits = new float[Core.Balance.PlayerCount], _viewRefilledAt = new float[Core.Balance.PlayerCount];
        private ulong _viewNonce;
        private bool _viewHelloSent, _viewAcknowledged;
        private byte _viewPublishMask;
        private int _viewSentMask = -1, _viewRequestedSlot = -2;
        private uint _viewPublishedSequence;
        private float _viewPublishedAt = -99, _viewRelayedAt = -99;
        public int SpectatorViewsPublished { get; private set; }
        public int SpectatorViewsReceived { get; private set; }
        public int SpectatorViewsRelayed { get; private set; }
        public byte SpectatorViewWireVersion => _viewVersion;
        public int SpectatorViewPeersAtVersion(byte version)
        {
            int count = 0; foreach (byte peerVersion in _viewVersions.Values) if (peerVersion == version) count++;
            return count;
        }
        public SpectatorViewState LastPublishedSpectatorView { get; private set; }

        private void ResetSpectatorViewTransport()
        {
            _viewPeers.Clear(); _viewVersions.Clear(); _viewWatchers.Clear(); _retiredViewPeers.Clear();
            _viewVersion = SpectatorViewState.LegacyVersion;
            Array.Clear(_hasPendingView, 0, _hasPendingView.Length);
            Array.Clear(_viewOwnerSequence, 0, _viewOwnerSequence.Length);
            Array.Clear(_viewRelaySequence, 0, _viewRelaySequence.Length);
            Array.Clear(_viewOwnerMatch, 0, _viewOwnerMatch.Length);
            for (int i = 0; i < Core.Balance.PlayerCount; i++) { _viewCredits[i] = 8; _viewRefilledAt[i] = Time.unscaledTime; }
            _viewNonce = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0); if (_viewNonce == 0) _viewNonce = 1;
            _viewHelloSent = _viewAcknowledged = false; _viewPublishMask = 0;
            _viewSentMask = -1; _viewRequestedSlot = -2; _viewPublishedSequence = 0;
            _viewPublishedAt = _viewRelayedAt = -99;
            SpectatorViewsPublished = SpectatorViewsReceived = SpectatorViewsRelayed = 0;
            LastPublishedSpectatorView = default;
            ObservedPlayerView.ClearRemote();
        }
        private void SuspendSpectatorViews()
        {
            // Keep capability/subscription identity when this component is merely
            // disabled on the same transport. A replacement manager resets it all.
            Array.Clear(_hasPendingView, 0, _hasPendingView.Length);
            ObservedPlayerView.ClearRemote();
        }
        private void RegisterSpectatorViewMessages(CustomMessagingManager manager)
        {
            ResetSpectatorViewTransport();
            // An old transport callback cannot act on a replacement manager/session.
            manager.RegisterNamedMessageHandler("SpectatorViewHello", (sender, reader) => {
                if (isActiveAndEnabled && ReferenceEquals(_nm?.CustomMessagingManager, manager)) OnViewHello(sender, reader); });
            manager.RegisterNamedMessageHandler("SpectatorViewReady", (sender, reader) => {
                if (isActiveAndEnabled && ReferenceEquals(_nm?.CustomMessagingManager, manager)) OnViewReady(sender, reader); });
            manager.RegisterNamedMessageHandler("SpectatorViewRequest", (sender, reader) => {
                if (isActiveAndEnabled && ReferenceEquals(_nm?.CustomMessagingManager, manager)) OnViewRequest(sender, reader); });
            manager.RegisterNamedMessageHandler("SubmitSpectatorView", (sender, reader) => {
                if (isActiveAndEnabled && ReferenceEquals(_nm?.CustomMessagingManager, manager)) OnSubmitView(sender, reader); });
            manager.RegisterNamedMessageHandler("SpectatorViewPose", (sender, reader) => {
                if (isActiveAndEnabled && ReferenceEquals(_nm?.CustomMessagingManager, manager)) OnViewPose(sender, reader); });
        }
        private void LateUpdate()
        {
            if (_nm == null || !_nm.IsListening || _nm.CustomMessagingManager == null) return;
            if (_nm.IsServer) TickHostViews(); else TickOwnerView();
        }
        private void TickOwnerView()
        {
            if (!_nm.IsConnectedClient) return;
            if (!_viewHelloSent)
            {
                // Establish v2 first, then offer v3 with the same nonce. A v2 host ignores
                // the offer and keeps its original pose stream, without repeated probes.
                using var hello = new FastBufferWriter(9, Allocator.Temp);
                hello.WriteValueSafe(SpectatorViewState.LegacyVersion); hello.WriteValueSafe(_viewNonce);
                _nm.CustomMessagingManager.SendNamedMessage("SpectatorViewHello", NetworkManager.ServerClientId, hello);
                using var lensHello = new FastBufferWriter(9, Allocator.Temp);
                lensHello.WriteValueSafe(ViewVersion); lensHello.WriteValueSafe(_viewNonce);
                _nm.CustomMessagingManager.SendNamedMessage("SpectatorViewHello", NetworkManager.ServerClientId, lensHello);
                _viewHelloSent = true;
            }
            if (!_viewAcknowledged) return;
            int wanted = ObservedPlayerView.RequestedSlot;
            if (!GameLaunch.Spectator || !ValidSlot(wanted)) wanted = -1;
            if (_viewRequestedSlot != wanted)
            {
                using var request = new FastBufferWriter(9, Allocator.Temp);
                request.WriteValueSafe(_viewNonce); request.WriteValueSafe((sbyte)wanted);
                _nm.CustomMessagingManager.SendNamedMessage("SpectatorViewRequest", NetworkManager.ServerClientId, request);
                _viewRequestedSlot = wanted;
            }
            int slot = NetAuthority.LocalSlot;
            if (!ValidSlot(slot) || (_viewPublishMask & (1 << slot)) == 0 || Time.unscaledTime - _viewPublishedAt < ViewInterval) return;
            var actor = Unit(slot);
            if (!ObservedPlayerView.TryLocal(actor, out var state) || !state.Matches(PresentationMatchId, GameServices.Match?.RoundNumber ?? 0, actor)) return;
            state.Sequence = NextViewSerial(ref _viewPublishedSequence); state.SentAt = (float)_nm.ServerTime.Time;
            if (!state.IsValid) return;
            using var writer = new FastBufferWriter(SpectatorViewState.MaxWireBytes + SpectatorViewState.LensBytes + 8, Allocator.Temp);
            writer.WriteValueSafe(_viewNonce); state.Write(writer, _viewVersion);
            _nm.CustomMessagingManager.SendNamedMessage("SubmitSpectatorView", NetworkManager.ServerClientId, writer, NetworkDelivery.UnreliableSequenced);
            LastPublishedSpectatorView = state;
            SpectatorViewsPublished++;
            _viewPublishedAt = Time.unscaledTime;
        }
        private bool ViewPeerMayWatch(ulong peer)
        {
            if (peer > int.MaxValue || !_nm.ConnectedClients.ContainsKey(peer)) return false;
            var record = NetSession.Instance?.Lobby?.PeerById((int)peer);
            return record != null && record.Spectator && record.Seat < 0;
        }
        private byte NeededViewMask()
        {
            int mask = 0;
            foreach (var watcher in _viewWatchers)
                if (ViewPeerMayWatch(watcher.Key) && ValidSlot(watcher.Value)) mask |= 1 << watcher.Value;
            if (GameLaunch.Spectator && ValidSlot(ObservedPlayerView.RequestedSlot)) mask |= 1 << ObservedPlayerView.RequestedSlot;
            return (byte)mask;
        }
        private void SendViewReady(ulong peer, ulong nonce, byte mask)
        {
            using var writer = new FastBufferWriter(10, Allocator.Temp);
            writer.WriteValueSafe(_viewVersions.TryGetValue(peer, out byte version) ? version : SpectatorViewState.LegacyVersion);
            writer.WriteValueSafe(nonce); writer.WriteValueSafe(mask);
            _nm.CustomMessagingManager.SendNamedMessage("SpectatorViewReady", peer, writer);
        }
        private void OnViewHello(ulong sender, FastBufferReader reader)
        {
            if (!_nm.IsServer || sender == _nm.LocalClientId || reader.Length - reader.Position != 9 || !reader.TryBeginRead(9)
                || !_nm.ConnectedClients.ContainsKey(sender)) return;
            reader.ReadValueSafe(out byte version); reader.ReadValueSafe(out ulong nonce);
            if (!SpectatorViewState.Supports(version) || nonce == 0) return;
            if (!_viewPeers.TryGetValue(sender, out var previous) || previous != nonce)
            {
                _viewWatchers.Remove(sender);
                var record = sender <= int.MaxValue ? NetSession.Instance?.Lobby?.PeerById((int)sender) : null;
                if (record != null && ValidSlot(record.Seat)) _viewOwnerSequence[record.Seat] = 0;
                _viewVersions.Remove(sender);
            }
            _viewPeers[sender] = nonce;
            if (!_viewVersions.TryGetValue(sender, out byte known) || version > known) _viewVersions[sender] = version;
            SendViewReady(sender, nonce, NeededViewMask());
        }
        private void OnViewReady(ulong sender, FastBufferReader reader)
        {
            if (_nm.IsServer || !FromHost(sender) || !_viewHelloSent || reader.Length - reader.Position != 10 || !reader.TryBeginRead(10)) return;
            reader.ReadValueSafe(out byte version); reader.ReadValueSafe(out ulong nonce); reader.ReadValueSafe(out byte mask);
            if (!SpectatorViewState.Supports(version) || nonce != _viewNonce || (mask & ~15) != 0 || version < _viewVersion) return;
            _viewVersion = version;
            _viewAcknowledged = true; _viewPublishMask = mask;
        }
        private void OnViewRequest(ulong sender, FastBufferReader reader)
        {
            if (!_nm.IsServer || reader.Length - reader.Position != 9 || !reader.TryBeginRead(9)) return;
            reader.ReadValueSafe(out ulong nonce); reader.ReadValueSafe(out sbyte slot);
            if (!_viewPeers.TryGetValue(sender, out var expected) || expected != nonce || !ViewPeerMayWatch(sender)
                || slot < -1 || slot >= Core.Balance.PlayerCount) return;
            if (slot < 0) _viewWatchers.Remove(sender); else _viewWatchers[sender] = slot;
            RefreshViewMask();
        }
        private void RefreshViewMask()
        {
            byte mask = NeededViewMask(); if (_viewSentMask == mask) return;
            _viewSentMask = mask;
            foreach (var peer in _viewPeers)
                if (_nm.ConnectedClients.ContainsKey(peer.Key)) SendViewReady(peer.Key, peer.Value, mask);
        }
        private void OnSubmitView(ulong sender, FastBufferReader reader)
        {
            int length = reader.Length - reader.Position;
            if (!_nm.IsServer || length < SpectatorViewState.MinWireBytes + 8 || length > SpectatorViewState.MaxWireBytes + SpectatorViewState.LensBytes + 8 || !reader.TryBeginRead(8)) return;
            reader.ReadValueSafe(out ulong nonce);
            if (!_viewPeers.TryGetValue(sender, out var expected) || expected != nonce || sender > int.MaxValue
                || !_viewVersions.TryGetValue(sender, out byte version)
                || !SpectatorViewState.TryRead(ref reader, out var state, version) || (state.Flags & SpectatorViewState.BotView) != 0
                || !SenderOwnsClaimedSeat(sender, state.Slot, out var actor)
                || !state.Matches(PresentationMatchId, GameServices.Match?.RoundNumber ?? 0, actor)
                || !state.Timely((float)_nm.ServerTime.Time) || Vector3.Distance(actor.transform.position, state.Eye) > 80
                || (NeededViewMask() & (1 << state.Slot)) == 0) return;
            int slot = state.Slot;
            bool sameScope = _viewOwnerMatch[slot] == state.Match && _viewOwnerEpoch[slot] == state.Epoch
                && _viewOwnerRound[slot] == state.Round && _viewOwnerNonce[slot] == nonce;
            if (sameScope && !SpectatorViewState.Newer(state.Sequence, _viewOwnerSequence[slot])) return;
            float now = Time.unscaledTime;
            _viewCredits[slot] = Mathf.Min(8, _viewCredits[slot] + (now - _viewRefilledAt[slot]) * 25);
            _viewRefilledAt[slot] = now;
            if (_viewCredits[slot] < 1) return;
            _viewCredits[slot] -= 1;
            _viewOwnerMatch[slot] = state.Match; _viewOwnerEpoch[slot] = state.Epoch; _viewOwnerSequence[slot] = state.Sequence;
            _viewOwnerRound[slot] = state.Round; _viewOwnerNonce[slot] = nonce;
            _pendingViews[slot] = state; _hasPendingView[slot] = true;
        }
        private void TickHostViews()
        {
            _retiredViewPeers.Clear();
            foreach (var peer in _viewPeers) if (!_nm.ConnectedClients.ContainsKey(peer.Key)) _retiredViewPeers.Add(peer.Key);
            foreach (var peer in _retiredViewPeers) { _viewPeers.Remove(peer); _viewVersions.Remove(peer); _viewWatchers.Remove(peer); }
            RefreshViewMask();
            if (Time.unscaledTime - _viewRelayedAt < ViewInterval) return;
            _viewRelayedAt = Time.unscaledTime;
            byte mask = NeededViewMask();
            for (int slot = 0; slot < Core.Balance.PlayerCount; slot++)
            {
                if ((mask & (1 << slot)) == 0) { _hasPendingView[slot] = false; continue; }
                var actor = Unit(slot); if (actor == null) continue;
                SpectatorViewState state;
                if (actor.IsBot) state = ObservedPlayerView.BotPose(actor);
                else if (slot == NetAuthority.LocalSlot && ObservedPlayerView.TryLocal(actor, out state)) { }
                else if (_hasPendingView[slot]) state = _pendingViews[slot];
                else continue;
                _hasPendingView[slot] = false;
                if (actor.IsBot || slot == NetAuthority.LocalSlot) state.SentAt = (float)_nm.ServerTime.Time;
                state.Sequence = NextViewSerial(ref _viewRelaySequence[slot]);
                if (!state.IsValid || !state.Matches(PresentationMatchId, GameServices.Match?.RoundNumber ?? 0, actor)
                    || !state.Timely((float)_nm.ServerTime.Time)) continue;
                ObservedPlayerView.InstallRemote(state, actor);
                foreach (var watcher in _viewWatchers)
                    if (watcher.Value == slot && ViewPeerMayWatch(watcher.Key) && _viewPeers.ContainsKey(watcher.Key))
                    {
                        using var writer = new FastBufferWriter(SpectatorViewState.MaxWireBytes + SpectatorViewState.LensBytes, Allocator.Temp);
                        state.Write(writer, _viewVersions.TryGetValue(watcher.Key, out byte version) ? version : SpectatorViewState.LegacyVersion);
                        _nm.CustomMessagingManager.SendNamedMessage("SpectatorViewPose", watcher.Key, writer, NetworkDelivery.UnreliableSequenced);
                        SpectatorViewsRelayed++;
                    }
            }
        }
        private void OnViewPose(ulong sender, FastBufferReader reader)
        {
            if (_nm.IsServer || !FromHost(sender) || !_viewAcknowledged || !GameLaunch.Spectator
                || !SpectatorViewState.TryRead(ref reader, out var state, _viewVersion) || state.Slot != ObservedPlayerView.RequestedSlot
                || !state.Timely((float)_nm.ServerTime.Time)) return;
            if (ObservedPlayerView.InstallRemote(state, Unit(state.Slot))) SpectatorViewsReceived++;
        }
        private static uint NextViewSerial(ref uint value) { value++; if (value == 0) value++; return value; }
    }
}
