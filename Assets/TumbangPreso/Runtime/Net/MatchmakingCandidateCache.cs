using System;
using System.Collections.Generic;
using TumbangPreso.Core;

namespace TumbangPreso.Net
{
    // Queue-local failures are not player penalties. A replacement allocation can
    // be tried immediately; the same failed endpoint gets time to disappear/repair.
    public sealed class MatchmakingCandidateCache
    {
        public const double RetrySeconds = 30;
        private const int Limit = 64;
        private readonly Dictionary<(string lobby, string relay), double> _failed =
            new Dictionary<(string, string), double>();

        public bool CanTry(ServerQuery.Entry entry, string skillContract, double now, int seatsNeeded = 1)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id) || string.IsNullOrWhiteSpace(entry.RelayCode)
                || !entry.MatchesSkillContract(skillContract) || !Finite(now)) return false;
            int capacity = entry.Capacity > 0 ? entry.Capacity : LobbySession.MaxPlayers;
            if (seatsNeeded < 1 || seatsNeeded > LobbySession.MaxPlayers
                || capacity - Math.Max(entry.Seated, entry.Occupied) < seatsNeeded) return false;
            var key = (entry.Id, entry.RelayCode);
            if (!_failed.TryGetValue(key, out double until)) return true;
            if (now < until) return false;
            _failed.Remove(key);
            return true;
        }

        public void Failed(string lobbyId, string attemptedRelay, double now)
        {
            if (string.IsNullOrEmpty(lobbyId) || !Finite(now)) return;
            var key = (lobbyId, attemptedRelay);
            if (!_failed.ContainsKey(key) && _failed.Count >= Limit)
            {
                (string lobby, string relay) oldest = default;
                double earliest = double.PositiveInfinity;
                foreach (var pair in _failed)
                    if (pair.Value < earliest) { oldest = pair.Key; earliest = pair.Value; }
                _failed.Remove(oldest);
            }
            _failed[key] = now + RetrySeconds;
        }

        public void Clear() => _failed.Clear();
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
