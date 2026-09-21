using System;

namespace TumbangPreso.Core
{
    public enum MatchMomentKind : byte
    { FirstKnockdown, AccurateThree, AccurateFive, DoubleCatch, TripleCatch, LateKnockdown, LeadChange }

    // Authoritative recognition only. Receiving or rendering this never awards points.
    public readonly struct MatchMoment
    {
        public readonly long MatchId, Sequence;
        public readonly int Round, Actor, Count, Bonus;
        public readonly MatchMomentKind Kind;
        public MatchMoment(long matchId, long sequence, int round, int actor, MatchMomentKind kind, int count = 0, int bonus = 0)
        { MatchId=matchId; Sequence=sequence; Round=round; Actor=actor; Kind=kind; Count=count; Bonus=bonus; }
        public bool IsValid => MatchId > 0 && Sequence > 0 && Round > 0 && Round <= 64
            && Actor >= 0 && Actor < Balance.PlayerCount && Count >= 0 && Count <= 10000
            && (Bonus == 0 || Bonus == 10 || Bonus == 20 || Bonus == 25)
            && Enum.IsDefined(typeof(MatchMomentKind), Kind);
        public int Priority => Kind == MatchMomentKind.TripleCatch || Kind == MatchMomentKind.AccurateFive ? 3
            : Kind == MatchMomentKind.DoubleCatch || Kind == MatchMomentKind.AccurateThree ? 2 : 1;
    }
}
