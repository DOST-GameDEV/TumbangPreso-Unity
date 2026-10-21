using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private LobbySession _seatSwapLobby;
        private LobbySeatSwapRequests _seatSwaps;
        private LobbySeatSwapOffer _seatSwapOffer;
        private long _lastSeatSwapOfferId;
        public LobbySeatSwapOffer SeatSwapOffer => _seatSwapOffer != null
            && Time.realtimeSinceStartupAsDouble < _seatSwapOffer.ExpiresAt ? _seatSwapOffer : null;
        public string SeatSwapResult { get; private set; } = "";

        private void ResetSeatSwapTransport()
        { _seatSwapLobby = null; _seatSwaps = null; _seatSwapOffer = null; _lastSeatSwapOfferId = 0; SeatSwapResult = ""; }

        private void TickSeatSwaps()
        {
            var net = NetSession.Instance;
            bool available = net != null && NetAuthority.IsNetworked && !net.Lobby.MatchInProgress
                && !UI.Hub.HubQueueWatch.QueueRoom;
            if (_seatSwapOffer != null && (!available || Time.realtimeSinceStartupAsDouble >= _seatSwapOffer.ExpiresAt))
                _seatSwapOffer = null;
            if (!NetAuthority.IsHost || _seatSwaps == null || _seatSwaps.Count == 0) return;
            foreach (var ended in _seatSwaps.Expire(Time.realtimeSinceStartupAsDouble,
                         !available || !ReferenceEquals(net?.Lobby, _seatSwapLobby)))
                EndSeatSwap(ended, 0);
        }

        private void HostRequestSeatSwap(int peerId, int targetSeat)
        {
            var lobby = NetSession.Instance?.Lobby;
            if (!NetAuthority.IsHost || lobby == null || lobby.MatchInProgress || UI.Hub.HubQueueWatch.QueueRoom) return;
            if (!ReferenceEquals(_seatSwapLobby, lobby))
            { _seatSwapLobby = lobby; _seatSwaps = new LobbySeatSwapRequests(lobby); }
            foreach (var ended in _seatSwaps.Expire(Time.realtimeSinceStartupAsDouble)) EndSeatSwap(ended, 0);
            var request = _seatSwaps.Request(peerId, targetSeat, Time.realtimeSinceStartupAsDouble);
            if (request == null) return;
            SendSeatSwapOffer(request, request.Requester.PeerId, false);
            SendSeatSwapOffer(request, request.Recipient.PeerId, true);
        }

        private void SendSeatSwapOffer(LobbySeatSwapRequest request, int peerId, bool incoming)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            var offer = new LobbySeatSwapOffer { Id = request.Id, FromSeat = request.FromSeat, ToSeat = request.ToSeat,
                RequesterName = request.Requester.Name, Incoming = incoming, ExpiresAt = request.ExpiresAt };
            if (peerId == (int)_nm.LocalClientId) { ApplySeatSwapOffer(offer); return; }
            using var writer = new FastBufferWriter(512, Allocator.Temp);
            writer.WriteValueSafe(offer.Id); writer.WriteValueSafe(offer.FromSeat); writer.WriteValueSafe(offer.ToSeat);
            writer.WriteValueSafe(offer.Incoming);
            writer.WriteValueSafe((float)Math.Max(0, offer.ExpiresAt - Time.realtimeSinceStartupAsDouble));
            writer.WriteValueSafe(offer.RequesterName ?? "PLAYER");
            _nm.CustomMessagingManager.SendNamedMessage("SeatSwapOffer", (ulong)peerId, writer);
        }
        private void OnSeatSwapOfferMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || reader.Length - reader.Position > 512 || !reader.TryBeginRead(21)) return;
            var offer = new LobbySeatSwapOffer();
            try
            {
                reader.ReadValueSafe(out offer.Id); reader.ReadValueSafe(out offer.FromSeat); reader.ReadValueSafe(out offer.ToSeat);
                reader.ReadValueSafe(out offer.Incoming); reader.ReadValueSafe(out float seconds);
                reader.ReadValueSafe(out offer.RequesterName);
                if (reader.Position != reader.Length || offer.Id <= 0 || !ValidSlot(offer.FromSeat) || !ValidSlot(offer.ToSeat)
                    || offer.FromSeat == offer.ToSeat || !Finite(seconds) || seconds <= 0 || seconds > LobbySeatSwapRequests.Lifetime
                    || offer.RequesterName == null || offer.RequesterName.Length > 100) return;
                offer.ExpiresAt = Time.realtimeSinceStartupAsDouble + seconds;
            }
            catch (Exception error) when (error is OverflowException || error is ArgumentException) { return; }
            ApplySeatSwapOffer(offer);
        }
        private void ApplySeatSwapOffer(LobbySeatSwapOffer offer)
        {
            if (offer.Id <= _lastSeatSwapOfferId) return;
            _lastSeatSwapOfferId = offer.Id; _seatSwapOffer = offer; SeatSwapResult = "";
        }

        public void RespondToSeatSwap(long requestId, bool accept)
        {
            var offer = SeatSwapOffer;
            if (offer == null || !offer.Incoming || offer.Id != requestId) return;
            if (NetAuthority.IsHost)
            { HostRespondToSeatSwap(_nm != null ? (int)_nm.LocalClientId : 0, requestId, accept); return; }
            if (_nm?.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(requestId); writer.WriteValueSafe(accept);
            _nm.CustomMessagingManager.SendNamedMessage("SeatSwapReply", NetworkManager.ServerClientId, writer);
            _seatSwapOffer = null;
        }
        private void OnSeatSwapReplyMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost || senderClientId > int.MaxValue || reader.Length - reader.Position != 9
                || !reader.TryBeginRead(9)) return;
            reader.ReadValueSafe(out long id); reader.ReadValueSafe(out bool accept);
            HostRespondToSeatSwap((int)senderClientId, id, accept);
        }
        private void HostRespondToSeatSwap(int peerId, long id, bool accept)
        {
            if (!NetAuthority.IsHost || _seatSwaps == null) return;
            TickSeatSwaps();
            var ended = _seatSwaps.Respond(peerId, id, accept, Time.realtimeSinceStartupAsDouble, out bool swapped);
            if (ended == null) return;
            if (swapped)
            {
                _lobbyReady.Remove(ended.Requester.PeerId); _lobbyReady.Remove(ended.Recipient.PeerId);
                SendSeating(ended.Requester.PeerId); SendSeating(ended.Recipient.PeerId);
                BroadcastLobbyPicks(); BroadcastReadyTally();
            }
            EndSeatSwap(ended, swapped ? (byte)1 : accept ? (byte)0 : (byte)2);
        }
        private void EndSeatSwap(LobbySeatSwapRequest ended, byte outcome)
        {
            SendSeatSwapEnd(ended.Requester.PeerId, ended.Id, outcome);
            SendSeatSwapEnd(ended.Recipient.PeerId, ended.Id, outcome);
        }
        private void SendSeatSwapEnd(int peerId, long id, byte outcome)
        {
            if (_nm?.CustomMessagingManager == null) return;
            if (peerId == (int)_nm.LocalClientId) { ApplySeatSwapEnd(id, outcome); return; }
            if (NetSession.Instance?.Lobby.PeerById(peerId) == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(id); writer.WriteValueSafe(outcome);
            _nm.CustomMessagingManager.SendNamedMessage("SeatSwapEnd", (ulong)peerId, writer);
        }
        private void OnSeatSwapEndMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || reader.Length - reader.Position != 9
                || !reader.TryBeginRead(9)) return;
            reader.ReadValueSafe(out long id); reader.ReadValueSafe(out byte outcome);
            if (outcome > 2) return;
            ApplySeatSwapEnd(id, outcome);
        }
        private void ApplySeatSwapEnd(long id, byte outcome)
        {
            if (id != _lastSeatSwapOfferId) return;
            _seatSwapOffer = null;
            SeatSwapResult = outcome == 1 ? "Seats switched." : outcome == 2 ? "Seat switch declined." : "Seat switch expired or cancelled.";
        }
    }
}
