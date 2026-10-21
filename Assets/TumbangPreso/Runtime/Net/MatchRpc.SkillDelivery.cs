using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private const int MaxPendingSkillCasts = 64;
        private const float SkillBodyWaitSeconds = 2f;
        private readonly List<(SkillCastMessage cast, float expires)> _pendingSkillCasts =
            new List<(SkillCastMessage, float)>(8);

        private void QueueAcceptedSkill(SkillCastMessage cast)
        {
            PrepareSkillReceipts();
            if (cast.Event <= _lastSkillEvent[cast.Seat]) return;
            _lastSkillEvent[cast.Seat] = cast.Event;
            if (_pendingSkillCasts.Count == MaxPendingSkillCasts)
            {
                _pendingSkillCasts.RemoveAt(0);
                QueueWorldSnapshotRefresh(cast.Round);
            }
            _pendingSkillCasts.Add((cast, Time.unscaledTime + SkillBodyWaitSeconds));
            FlushPendingSkills();
        }

        private void FlushPendingSkills()
        {
            if (_pendingSkillCasts.Count == 0) return;
            PrepareSkillReceipts();
            int blockedSeats = 0;
            bool recover = false;
            for (int i = 0; i < _pendingSkillCasts.Count;)
            {
                var pending = _pendingSkillCasts[i];
                var cast = pending.cast;
                if (NetAuthority.IsHost || cast.Match != PresentationMatchId || cast.Round != GameServices.Match?.RoundNumber)
                { _pendingSkillCasts.RemoveAt(i); continue; }
                if (Time.unscaledTime > pending.expires)
                {
                    _pendingSkillCasts.RemoveAt(i);
                    recover = true;
                    continue;
                }
                int seatBit = 1 << cast.Seat;
                if ((blockedSeats & seatBit) != 0 || Skill(Unit(cast.Seat), cast.Slot)?.Id != cast.AbilityId.ToString()
                    || (cast.HasFamiliar && Familiar(cast.Seat) == null))
                {
                    blockedSeats |= seatBit;
                    i++;
                    continue;
                }
                // Preserve each actor's cast/command order. Readiness on one actor
                // must not hold up already-installed actors elsewhere in the match.
                _pendingSkillCasts.RemoveAt(i);
                PlayReceivedAbility(cast);
            }
            if (recover) QueueWorldSnapshotRefresh(GameServices.Match?.RoundNumber ?? 0);
        }
    }
}
