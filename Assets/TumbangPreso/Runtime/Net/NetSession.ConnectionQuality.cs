using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class NetSession
    {
        private CustomMessagingManager _qualityRouter;
        private readonly Dictionary<ulong, LinkRoundTrips> _qualityTrips = new();
        private readonly Dictionary<ulong, double> _qualityEchoAt = new();
        private double _qualityNextAt;
        private ulong _qualityNonce;

        private void ResetQualityProbe()
        {
            if (_qualityRouter != null) _qualityRouter.OnUnnamedMessage -= OnQualityPacket;
            _qualityRouter = null; _qualityTrips.Clear(); _qualityEchoAt.Clear(); _qualityNextAt = 0;
            _qualityNonce = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0);
            if (_qualityNonce == 0) _qualityNonce = 1;
        }
        private bool IsQualityPeer(ulong sender)
        {
            if (_nm == null || !_nm.IsListening) return false;
            if (!_nm.IsServer) return _nm.IsConnectedClient && _everConnected && sender == NetworkManager.ServerClientId;
            if (sender == _nm.LocalClientId || sender > int.MaxValue || Lobby.PeerById((int)sender) == null) return false;
            foreach (ulong peer in _nm.ConnectedClientsIds) if (peer == sender) return true;
            return false;
        }
        private void UpdateQualityProbe()
        {
            var router = _nm != null && _nm.IsListening ? _nm.CustomMessagingManager : null;
            if (!ReferenceEquals(router, _qualityRouter))
            {
                if (_qualityRouter != null) _qualityRouter.OnUnnamedMessage -= OnQualityPacket;
                _qualityRouter = router;
                if (router != null) router.OnUnnamedMessage += OnQualityPacket;
            }
            if (router == null || _nm == null || (!_nm.IsServer && !_nm.IsConnectedClient)) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < _qualityNextAt) return;
            _qualityNextAt = now + 1;
            if (_nm.IsServer)
            {
                foreach (ulong peer in _nm.ConnectedClientsIds)
                    if (IsQualityPeer(peer)) SendQualityProbe(peer, now);
            }
            else if (_everConnected && _seatApplied) SendQualityProbe(NetworkManager.ServerClientId, now);
        }
        private void SendQualityProbe(ulong peer, double now)
        {
            if (!_qualityTrips.TryGetValue(peer, out var trips))
            {
                if (_qualityNonce == 0) _qualityNonce = 1;
                _qualityTrips[peer] = trips = new LinkRoundTrips(_qualityNonce);
            }
            uint serial = trips.Begin(now); if (serial == 0) return;
            using var writer = new FastBufferWriter(LinkQualityPacket.Bytes, Allocator.Temp);
            var buffer = writer;
            LinkQualityPacket.Write(ref buffer, false, trips.Nonce, serial);
            _qualityRouter.SendUnnamedMessage(peer, writer, NetworkDelivery.UnreliableSequenced);
        }
        private void OnQualityPacket(ulong sender, FastBufferReader reader)
        {
            if (!IsQualityPeer(sender) || !LinkQualityPacket.TryRead(ref reader, out bool reply, out ulong nonce, out uint serial)) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (reply)
            {
                if (_qualityTrips.TryGetValue(sender, out var trips)) trips.Accept(nonce, serial, now);
                return;
            }
            // Same-sized echo, at most twice a second per admitted connection.
            // Legacy peers silently ignore this optional unnamed message.
            if (_qualityEchoAt.TryGetValue(sender, out double last) && now - last < .5) return;
            _qualityEchoAt[sender] = now;
            using var writer = new FastBufferWriter(LinkQualityPacket.Bytes, Allocator.Temp);
            var buffer = writer;
            LinkQualityPacket.Write(ref buffer, true, nonce, serial);
            _qualityRouter?.SendUnnamedMessage(sender, writer, NetworkDelivery.UnreliableSequenced);
        }
        private long MeasuredPeerRtt(ulong peer, long fallback)
            => _qualityTrips.TryGetValue(peer, out var trips) && trips.HasReply ? trips.RttMs : fallback;
        private double WorstQualityReplyAge()
        {
            double worst = -1, now = Time.realtimeSinceStartupAsDouble;
            foreach (var trips in _qualityTrips.Values) worst = Math.Max(worst, trips.ReplyAge(now));
            return worst;
        }
    }
}
