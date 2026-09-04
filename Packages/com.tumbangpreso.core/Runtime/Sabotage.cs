namespace TumbangPreso.Core
{
    /// <summary>
    /// Why a candidate shove was refused, or <see cref="None"/> when it was taken.
    ///
    /// ⚠️⚠️ IT IS AN ENUM RATHER THAN A BOOL BECAUSE THE COMPLAINT WAS NEVER "TOO MANY
    /// SHOVES", IT WAS "SHOVES WITH NO REASON". 🧑 2026-09-03, off a played build: the bots
    /// *"follow players around only to push them, even when the shove has no meaningful effect
    /// on the game"*. A predicate that answers yes or no cannot be diagnosed from a match log,
    /// and the thing that has to be provable at the end of this work is not that the number went
    /// down: it is that **every shove a bot chose had an intelligible objective reason.** Naming
    /// the veto is what lets `AiDiagnosticProbe` print the distribution and lets a test assert
    /// on the CAUSE rather than on the outcome, so a rule that starts refusing for the wrong
    /// reason fails a test instead of merely looking quieter.
    /// </summary>
    public enum SabotageVeto
    {
        /// <summary>The shove is worth taking.</summary>
        None = 0,

        /// <summary>Only an attacker sabotages. The taya has the tag.</summary>
        NotAnAttacker,

        /// <summary>The defender can neither shove nor be shoved (`Combat`'s note).</summary>
        VictimIsDefender,

        /// <summary>
        /// The victim is empty-handed, so there is nothing for the taya to tag them for.
        ///
        /// ⚠️ THIS WAS A +1 ON A SCORE AND IS A HARD GATE NOW. `CharacterMotor.IsTaggable`
        /// needs a tsinelas in hand, so shoving an empty-handed rival at the taya sets up
        /// nothing at all: the old code said so in a comment and then scored it as a bonus.
        /// </summary>
        VictimNotVulnerable,

        /// <summary>There is no defender to shove anybody toward.</summary>
        NoTaya,

        /// <summary>
        /// The taya is down, stunned, displaced or otherwise unable to capitalise.
        ///
        /// ⚠️ A SHOVE INTO A STUNNED TAYA IS A FAVOUR. It hands the victim two free metres of
        /// separation and costs 25 stamina for it.
        /// </summary>
        TayaCannotAct,

        /// <summary>
        /// The victim is further away than one short step off the shove's own reach.
        ///
        /// ⚠️⚠️ THIS IS THE FAULT 🧑 ACTUALLY PHOTOGRAPHED. The old search radius was
        /// `4.16 * Sabotage`, up to 4.16 m, against a `Balance.ShoveRange` of **1.6 m**: two and
        /// a half shove-lengths of arena that the bot would WALK across to set up a press it had
        /// not yet earned. Sabotage is an opportunity, not a chase plan.
        /// </summary>
        OutOfApproachRange,

        /// <summary>
        /// The push moves the victim away from the taya, or sideways past it.
        ///
        /// ⚠️ THE OLD BAR WAS `aim > 0`, which admits a shove 89 degrees off the line to the
        /// taya. That is a shove that moves somebody two and a half metres and closes four
        /// centimetres, and it is most of what "random harassment" looked like on screen.
        /// </summary>
        PushesAwayFromTaya,

        /// <summary>
        /// The shove points the right way and still closes almost nothing.
        ///
        /// ⚠️ MEASURED IN METRES OFF THE PROJECTED ENDPOINT, NOT AS A DOT PRODUCT. A dot
        /// product is a direction test wearing a distance's clothes; two bodies nine metres
        /// apart and two bodies two metres apart produce the same dot for very different plays.
        /// </summary>
        NegligibleClosure,

        /// <summary>
        /// The victim lands closer and still lands somewhere the taya cannot reach them.
        ///
        /// ⚠️⚠️ THIS IS THE CHECK THE OLD CODE HAD NO EQUIVALENT OF AT ALL, and it is the one
        /// that makes the difference between "shoved toward the taya" and "shoved into danger".
        /// </summary>
        EndpointStaysSafe,

        /// <summary>A wall, barricade, pillar or prop stands in the shove's path.</summary>
        BlockedRoute,
    }

    /// <summary>
    /// The projected outcome of one candidate shove, and the reason it was or was not taken.
    ///
    /// ⚠️ EVERY FIELD IS SOMETHING A DIAGNOSTIC RUN HAS TO BE ABLE TO PRINT. The measurement
    /// this work is finished against is a list of shoves each with an intelligible reason, so
    /// the projection carries the arithmetic rather than only its verdict.
    /// </summary>
    public readonly struct SabotageProjection
    {
        public readonly SabotageVeto Veto;

        /// <summary>Flat distance from victim to taya before the shove, in metres.</summary>
        public readonly float DistanceBefore;

        /// <summary>Flat distance from the projected endpoint to the taya, in metres.</summary>
        public readonly float DistanceAfter;

        /// <summary>Where the victim is projected to come to rest.</summary>
        public readonly float EndpointX;
        public readonly float EndpointZ;

        /// <summary>The reach the endpoint was measured against. See <see cref="SabotageRules.DangerRadius"/>.</summary>
        public readonly float DangerRadius;

        public SabotageProjection(SabotageVeto veto, float distanceBefore, float distanceAfter,
                                  float endpointX, float endpointZ, float dangerRadius)
        {
            Veto = veto;
            DistanceBefore = distanceBefore;
            DistanceAfter = distanceAfter;
            EndpointX = endpointX;
            EndpointZ = endpointZ;
            DangerRadius = dangerRadius;
        }

        /// <summary>True when the shove is worth taking.</summary>
        public bool Meaningful => Veto == SabotageVeto.None;

        /// <summary>
        /// The same projection, refused for geometry.
        ///
        /// ⚠️ IT EXISTS SO THE CALLER CAN PAY FOR THE RAYCAST LAST. `Project` takes
        /// `routeBlocked` as a parameter rather than casting for itself (the core has no
        /// physics), and the caller only casts once the arithmetic has already agreed the shove
        /// is worth taking. Re-running `Project` with the answer would repeat every check for
        /// the one candidate per tick that got that far; this keeps the measurements and turns
        /// the verdict over.
        /// </summary>
        public SabotageProjection Blocked()
            => new SabotageProjection(SabotageVeto.BlockedRoute, DistanceBefore, DistanceAfter,
                                      EndpointX, EndpointZ, DangerRadius);

        /// <summary>How much ground the shove takes off the victim's escape, in metres.</summary>
        public float Closure => DistanceBefore - DistanceAfter;

        /// <summary>
        /// How good this shove is, for choosing between several legal ones.
        ///
        /// ⚠️ IT RANKS BY OUTCOME, NOT BY CONVENIENCE. The old score was
        /// `aim * 2 - TagDistanceWeight * d`, which is "pointing roughly right and standing
        /// nearby": it preferred the shove that was EASIEST to reach. This prefers the one that
        /// lands the victim deepest inside the taya's reach, and breaks ties by closure, which
        /// is the objective outcome the brief asks the bot to prove before it commits.
        /// </summary>
        public float Quality => Meaningful
            ? (DangerRadius - DistanceAfter) + Closure * 0.25f
            : float.NegativeInfinity;
    }

    /// <summary>
    /// When an attacker's shove creates immediate danger for another attacker, derived entirely
    /// from the gameplay constants that resolve it.
    ///
    /// ⚠️⚠️ NOT ONE NUMBER IN HERE IS TYPED IN AS A DISTANCE, AND THAT IS `Combat`'S RULE
    /// APPLIED TO A DECISION RATHER THAN TO AN IMPULSE. Its header: *"never hard-code a distance
    /// beside a speed, or moving Friction leaves one of the two silently wrong."* The same trap
    /// with a decision is worse, because a stale decision constant does not produce a wrong
    /// distance, it produces a bot that looks stupid: retune `ShoveSpeed`, `Friction`,
    /// `LungeSpeed` or `ShoveStun` and every bound below follows on its own.
    ///
    /// ⚠️⚠️ AND IT IS IN THE ENGINE-FREE CORE ON PURPOSE. `CLAUDE.md` § 4: the rules package
    /// holds *"every number arrived at by measurement rather than taste"*, and engine-free is
    /// what lets them be *"asserted in a second instead of playtested for an afternoon."* The
    /// fault this replaces was live for weeks in a 4,693-line `MonoBehaviour` where the only way
    /// to see it was to watch a match. `dotnet test` answers every rule below in about 40 ms.
    ///
    /// ⚠️ THE ONE THING IT CANNOT DECIDE IS GEOMETRY. Whether a wall stands between the victim
    /// and the endpoint is a raycast, so <see cref="Project"/> takes the answer as a parameter
    /// rather than pretending the core can see the map. The caller owes it the truth; the veto
    /// for it is named here so a test can still assert on the refusal.
    /// </summary>
    public static class SabotageRules
    {
        /// <summary>
        /// How far a shove actually carries a body: 2.50 m at the shipping constants.
        ///
        /// ⚠️ IT IS `Combat.ShoveDistance()` AND NOT A COPY OF IT. One expression, one answer.
        /// </summary>
        public static float ShoveTravel => Combat.ShoveDistance();

        /// <summary>
        /// The furthest the taya can tag from where it stands: 2.30 m at the shipping constants.
        ///
        /// ⚠️ THE LUNGE AT FULL COMMITMENT, OR THE PUNCH, WHICHEVER REACHES FURTHER. The taya
        /// has two tag verbs (`Balance`'s note: *"they answer different problems"*), so the
        /// reach a victim has to be pushed inside is the better of the two rather than either
        /// one alone. `Combat.LungeReach` is 2.30 and `Balance.PunchRange` is 1.70.
        /// </summary>
        public static float ActionableReach
        {
            get
            {
                float lunge = Combat.LungeReach();
                return lunge > Balance.PunchRange ? lunge : Balance.PunchRange;
            }
        }

        /// <summary>
        /// The share of the shove stun a taya actually gets to spend closing.
        ///
        /// ⚠️⚠️ IT IS A HALF, AND SPENDING THE WHOLE 1.25 s WOULD HAVE MADE THIS GATE MEANINGLESS.
        /// `Balance.ShoveStun` is 1.25 s and a taya moves at `Speed * DefenderSpeedScale` = 5.06
        /// m/s, so the whole window is **6.33 m of closing** on a 14 m box: a "danger radius" of
        /// 8.6 m admits two thirds of the arena and is not a filter at all. Half the window is
        /// the honest reading, because a taya spends the first half noticing and turning: a
        /// defender who sprints the entire stun at a body it has not yet looked at is a bot with
        /// perfect information, and `AiTuning`'s whole reaction model exists because that is
        /// exactly what this game refuses to ship.
        ///
        /// ⚠️ AND THE CONSERVATIVE DIRECTION IS THE CORRECT ONE HERE. Too small a share refuses
        /// a shove that would have worked, which costs one opportunity; too large a share
        /// admits the shove that made 🧑 file this, which costs the bot's credibility.
        /// </summary>
        public const float TayaResponseShare = 0.5f;

        /// <summary>
        /// How close to the taya the projected endpoint has to land: 5.46 m at the shipping
        /// constants. <see cref="ActionableReach"/> plus what a taya can close inside
        /// <see cref="TayaResponseShare"/> of the shove stun.
        /// </summary>
        public static float DangerRadius =>
            ActionableReach
            + Balance.Speed * Balance.DefenderSpeedScale
              * Balance.ShoveStun * TayaResponseShare;

        /// <summary>
        /// The share of its own travel a shove has to convert into closure: 0.60.
        ///
        /// ⚠️⚠️ THIS IS THE REPLACEMENT FOR `aim > 0` AND IT IS THE SINGLE BIGGEST CHANGE IN
        /// THIS FILE. Expressed as an angle it is about 53 degrees off the straight line to the
        /// taya, so a shove has to be recognisably AT somebody rather than merely not away from
        /// them. The old bar admitted 89.9 degrees, which is a body moved two and a half metres
        /// for four centimetres of closure, and that is what "no meaningful effect on the game"
        /// looks like from the outside.
        ///
        /// ⚠️ IT IS STATED AS A SHARE OF TRAVEL RATHER THAN AS AN ANGLE OR A DISTANCE, so it
        /// survives a retune of `ShoveSpeed` or `Friction` without either becoming a lie.
        /// </summary>
        public const float MinClosureShare = 0.60f;

        /// <summary>The metres of closure a shove has to buy: 1.50 m at the shipping constants.</summary>
        public static float MinClosure => ShoveTravel * MinClosureShare;

        /// <summary>
        /// How far a bot may be from a victim and still call it an opportunity: 3.20 m.
        ///
        /// ⚠️⚠️ TWICE `Balance.ShoveRange` AND NOT A METRE MORE. The shove connects at 1.6 m, so
        /// this is exactly one short step of adjustment: enough to get behind somebody who is
        /// already beside you, not enough to cross the arena to somebody who is not. The old
        /// radius was up to 4.16 m, and the gap between the two numbers IS the reported bug.
        ///
        /// ⚠️ AND IT IS THE SEARCH RADIUS, NOT THE FIRE RANGE. `DoSabotage` still only presses
        /// inside `ShoveRange * 0.9`; this bounds what the bot is allowed to look at.
        /// </summary>
        public static float MaxApproachRange => Balance.ShoveRange * 2.0f;

        /// <summary>
        /// The longest a bot may pursue one sabotage opportunity: 1.90 s.
        ///
        /// ⚠️⚠️ DERIVED FROM THE APPROACH, NOT PICKED. An attacker moves at
        /// `Speed * AttackerSpeedScale` = 2.53 m/s, so crossing <see cref="MaxApproachRange"/>
        /// takes 1.26 s; the half-step on top is the turn and the arc alignment the shove needs
        /// (`DoSabotage` drives all the way in precisely because the body only turns on a frame
        /// it walks). Past this the opportunity is gone and the bot goes back to its objective.
        ///
        /// ⚠️ THE POINT IS THE CEILING, NOT THE VALUE. 🧑's complaint was a bot TAILING a player,
        /// and a pursuit with no clock is a tail however good its entry condition is.
        /// </summary>
        public static float MaxPursuitSeconds =>
            MaxApproachRange / (Balance.Speed * Balance.AttackerSpeedScale) * 1.5f;

        /// <summary>
        /// How long a bot leaves a failed victim alone: 3.00 s.
        ///
        /// ⚠️ LONGER THAN `ShoveMissCooldown` (2.0 s) ON PURPOSE. That constant governs the
        /// VERB; this governs the DECISION, and a bot that re-enters the same failed plan the
        /// instant its swing cooldown clears is the tail again with extra steps.
        /// </summary>
        public const float TargetCooldownSeconds = 3.0f;

        /// <summary>
        /// The projected outcome of shoving <paramref name="victim"/> from
        /// <paramref name="shover"/>, given where the taya is.
        ///
        /// ⚠️⚠️ THE PUSH DIRECTION IS `victim - shover`, WHICH IS WHAT
        /// `CombatVerbs.HostResolveShove` ACTUALLY DOES. A projection that guessed a direction
        /// would be a second model of the same verb, and the two would drift. The bot may not
        /// aim a shove: it can only choose where to stand, which is why the approach side
        /// matters and why <see cref="LaunchPoint"/> exists.
        ///
        /// ⚠️ THE ORDER OF THE VETOES IS THE ORDER THEY COST THE LEAST TO ANSWER IN, and the
        /// cheap role checks come first so a diagnostic distribution is dominated by the
        /// interesting refusals rather than by three attackers noticing each other.
        /// </summary>
        public static SabotageProjection Project(
            bool shoverIsAttacker,
            float shoverX, float shoverZ,
            bool victimIsDefender, bool victimIsVulnerable,
            float victimX, float victimZ,
            bool tayaExists, bool tayaCanAct,
            float tayaX, float tayaZ,
            bool routeBlocked)
        {
            float danger = DangerRadius;

            if (!shoverIsAttacker)
                return Refuse(SabotageVeto.NotAnAttacker, danger);

            if (victimIsDefender)
                return Refuse(SabotageVeto.VictimIsDefender, danger);

            if (!victimIsVulnerable)
                return Refuse(SabotageVeto.VictimNotVulnerable, danger);

            if (!tayaExists)
                return Refuse(SabotageVeto.NoTaya, danger);

            if (!tayaCanAct)
                return Refuse(SabotageVeto.TayaCannotAct, danger);

            float pushX = victimX - shoverX;
            float pushZ = victimZ - shoverZ;
            float pushLength = Sqrt(pushX * pushX + pushZ * pushZ);

            // ⚠️ A ZERO-LENGTH PUSH IS NOT A DIRECTION. Two bodies at the same point give
            // `HostResolveShove` nothing to push along either, so there is no shove to project.
            if (pushLength < 0.05f || pushLength > MaxApproachRange)
                return Refuse(SabotageVeto.OutOfApproachRange, danger);

            float travel = ShoveTravel;
            float endX = victimX + pushX / pushLength * travel;
            float endZ = victimZ + pushZ / pushLength * travel;

            float before = Distance(victimX, victimZ, tayaX, tayaZ);
            float after = Distance(endX, endZ, tayaX, tayaZ);

            // ⚠️ THE DIRECTION AND THE AMOUNT ARE TWO SEPARATE REFUSALS, and reporting them
            // apart is how a diagnostic run tells "shoved the wrong way" from "shoved the right
            // way for nothing". Both used to be one `aim > 0`.
            if (after >= before)
                return new SabotageProjection(SabotageVeto.PushesAwayFromTaya,
                                              before, after, endX, endZ, danger);

            if (before - after < MinClosure)
                return new SabotageProjection(SabotageVeto.NegligibleClosure,
                                              before, after, endX, endZ, danger);

            if (after > danger)
                return new SabotageProjection(SabotageVeto.EndpointStaysSafe,
                                              before, after, endX, endZ, danger);

            // ⚠️ GEOMETRY LAST, BECAUSE IT IS THE ONLY ANSWER THE CALLER HAD TO PAY FOR. A
            // raycast per candidate per frame is the one expensive term here, so nothing asks
            // for it until the cheap arithmetic has already agreed the shove is worth taking.
            if (routeBlocked)
                return new SabotageProjection(SabotageVeto.BlockedRoute,
                                              before, after, endX, endZ, danger);

            return new SabotageProjection(SabotageVeto.None,
                                          before, after, endX, endZ, danger);
        }

        /// <summary>
        /// Where a bot should stand to shove <paramref name="victim"/> at the taya: on the far
        /// side of the victim, one shove-reach out along the line back from the taya.
        ///
        /// ⚠️⚠️ THIS IS THE OTHER HALF OF "DO NOT FOLLOW THEM AROUND". A bot that walks at the
        /// victim's CENTRE arrives beside them pointing wherever the approach happened to end,
        /// which is a shove into an arbitrary quadrant and then, on the next frame, another
        /// approach. Walking to the LAUNCH side means the shove either fires correctly or the
        /// opportunity expires; there is no state in which chasing is the productive move.
        ///
        /// ⚠️ IT USES `ShoveRange * 0.75` RATHER THAN THE FULL REACH. Standing at exactly the
        /// edge of the cone means one step of victim movement takes the press out of range, and
        /// the shove's own arc is 70 degrees off the facing, so a little inside the edge is
        /// where a press both connects and stays connected.
        /// </summary>
        public static void LaunchPoint(float victimX, float victimZ,
                                       float tayaX, float tayaZ,
                                       out float x, out float z)
        {
            float awayX = victimX - tayaX;
            float awayZ = victimZ - tayaZ;
            float length = Sqrt(awayX * awayX + awayZ * awayZ);

            if (length < 0.05f)
            {
                // Victim standing on the taya. There is no far side; stay put.
                x = victimX;
                z = victimZ;
                return;
            }

            float stand = Balance.ShoveRange * 0.75f;
            x = victimX + awayX / length * stand;
            z = victimZ + awayZ / length * stand;
        }

        // -------------------------------------------------------------------

        private static SabotageProjection Refuse(SabotageVeto veto, float danger)
            => new SabotageProjection(veto, 0.0f, 0.0f, 0.0f, 0.0f, danger);

        private static float Distance(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return Sqrt(dx * dx + dz * dz);
        }

        private static float Sqrt(float v) => (float)System.Math.Sqrt(v);
    }
}
