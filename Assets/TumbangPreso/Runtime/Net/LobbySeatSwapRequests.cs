using System;
using System.Collections.Generic;

namespace TumbangPreso.Net
{
    /// <summary>A host-owned request binds both identities and seats until consent or expiry.</summary>
    public sealed class LobbySeatSwapRequest
    {
        public readonly long Id;
        public readonly PeerRecord Requester, Recipient;
        public readonly int FromSeat, ToSeat;
        public readonly double ExpiresAt;
        internal LobbySeatSwapRequest(long id, PeerRecord from, PeerRecord to, double expiresAt)
        { Id = id; Requester = from; Recipient = to; FromSeat = from.Seat; ToSeat = to.Seat; ExpiresAt = expiresAt; }
    }

    public sealed class LobbySeatSwapRequests
    {
        public const double Lifetime = 20;
        private readonly LobbySession _lobby;
        private readonly Dictionary<long, LobbySeatSwapRequest> _pending = new Dictionary<long, LobbySeatSwapRequest>();
        private long _nextId = DateTime.UtcNow.Ticks;
        public LobbySeatSwapRequests(LobbySession lobby) { _lobby = lobby; }
        public int Count => _pending.Count;
        public bool Busy(int peerId)
        {
            foreach (var request in _pending.Values)
                if (request.Requester.PeerId == peerId || request.Recipient.PeerId == peerId) return true;
            return false;
        }
        public LobbySeatSwapRequest Request(int peerId, int targetSeat, double now)
        {
            if (!Finite(now) || _lobby.MatchInProgress || targetSeat < 0 || targetSeat >= LobbySession.MaxPlayers) return null;
            var from = _lobby.PeerById(peerId); var to = _lobby.PeerInSeat(targetSeat);
            if (from == null || to == null || ReferenceEquals(from, to) || from.Spectator || to.Spectator
                || from.Seat < 0 || to.Seat < 0 || Busy(peerId) || Busy(to.PeerId)) return null;
            var request = new LobbySeatSwapRequest(++_nextId, from, to, now + Lifetime);
            _pending.Add(request.Id, request); return request;
        }
        public LobbySeatSwapRequest Respond(int peerId, long id, bool accept, double now, out bool swapped)
        {
            swapped = false;
            if (!_pending.TryGetValue(id, out var request) || request.Recipient.PeerId != peerId) return null;
            // An unauthorized sender cannot consume somebody else's request.
            _pending.Remove(id);
            if (accept && Valid(request, now))
                swapped = _lobby.TrySwapSeats(request.Requester, request.Recipient, request.FromSeat, request.ToSeat);
            return request;
        }
        public List<LobbySeatSwapRequest> Expire(double now, bool cancelAll = false)
        {
            var ended = new List<LobbySeatSwapRequest>();
            foreach (var request in _pending.Values)
                if (cancelAll || !Valid(request, now)) ended.Add(request);
            foreach (var request in ended) _pending.Remove(request.Id);
            return ended;
        }
        private bool Valid(LobbySeatSwapRequest request, double now)
            => Finite(now) && now < request.ExpiresAt && !_lobby.MatchInProgress
                && ReferenceEquals(_lobby.PeerById(request.Requester.PeerId), request.Requester)
                && ReferenceEquals(_lobby.PeerById(request.Recipient.PeerId), request.Recipient)
                && request.Requester.Seat == request.FromSeat && request.Recipient.Seat == request.ToSeat
                && !request.Requester.Spectator && !request.Recipient.Spectator;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>Only the recipient sees Yes/No; the requester sees a waiting state.</summary>
    public sealed class LobbySeatSwapOffer
    {
        public long Id;
        public int FromSeat, ToSeat;
        public string RequesterName;
        public bool Incoming;
        public double ExpiresAt;
    }
}
