using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>Short-lived, one-use intent. Only a real transport departure may consume it.</summary>
    public sealed class PeerLeaveIntents
    {
        // Longer than the transport's eight-second silence timeout, independent of game pause.
        public const double LifetimeSeconds = 12;
        private readonly Dictionary<int, (long Match, double At)> _pending = new();
        public int Count => _pending.Count;

        public bool Remember(int peer, long match, double now)
        {
            if (peer < 0 || match <= 0 || !Finite(now) || now < 0) return false;
            if (!_pending.ContainsKey(peer) && _pending.Count >= Balance.PlayerCount)
            {
                var stale = new List<int>();
                foreach (var entry in _pending)
                    if (entry.Value.Match != match || now - entry.Value.At > LifetimeSeconds)
                        stale.Add(entry.Key);
                foreach (int key in stale) _pending.Remove(key);
                if (_pending.Count >= Balance.PlayerCount) return false;
            }
            _pending[peer] = (match, now);
            return true;
        }

        public bool Consume(int peer, long match, double now)
        {
            if (!_pending.TryGetValue(peer, out var intent)) return false;
            _pending.Remove(peer);
            return intent.Match == match && Finite(now) && now >= intent.At
                && now - intent.At <= LifetimeSeconds;
        }

        public void Clear() => _pending.Clear();
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
