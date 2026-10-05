namespace TumbangPreso.Core
{
    /// <summary>
    /// The Defender's Box, which is a SQUARE.
    ///
    /// ⚠️⚠️ A SQUARE, NOT A CIRCLE, AND THE CHALK IS THE TRUTH. The map builders draw the
    /// marker as four straight court lines at |x| = |z| = radius, and the clamp below
    /// clamps X and Z INDEPENDENTLY to match. A square and a circle of the same "radius"
    /// agree only at the four edge midpoints; on the diagonals they disagree by 2.07
    /// units, which is exactly where a taya moves when covering a corner. If either the
    /// clamp or the test ever becomes radial, the throwing line and the chalk stop
    /// agreeing and nobody will be able to see why. That is not hypothetical: it happened
    /// on 2026-07-29 and cost a session.
    ///
    /// ⚠️ THE TAYA IS CLAMPED IN, EVERYONE ELSE IS MERELY IN DANGER. The Defender cannot
    /// leave the box. Attackers move freely everywhere; the box is dangerous to them, not
    /// closed to them. The Safe Zone is simply everything outside it, and an Attacker
    /// there cannot be tagged, full stop.
    /// </summary>
    public static class Confinement
    {
        // ⚠️⚠️ THE BOX CAN BE ROUND, ON A MAP THAT SAYS SO (owner, 2026-10-06, after the Arena's first online
        // match: "we'll do round bounds.. because you're able to stand on one spot of a circular platform ring
        // and not be able to throw, but also move to another spot on the same ring and shoot. same for tagging",
        // and of its size: "should depend on the area that the specific stage has.. make it dynamically
        // changing"). The square above is still every other map's, and still the default. The Arena's stage is
        // rings round the can, and a square of 7 cut a ring at radius 8 into four arcs you could throw from
        // and four you could not. A map sets the shape and the radius for itself (`Use`) and gives them back
        // when it goes (`Reset`); each of the Arena's layouts has its own radius (`Map.ArenaStage`).
        // ⚠️ EVERY PEER SETS THE SAME, FROM STATE IT ALREADY SHARES (the layout), so nothing is sent; and the
        // chalk is drawn from these same two values (`Visual.CourtBoundaryPresentation`), so the line a player
        // sees and the rule that judges them cannot disagree, which is the warning at the top of this file.

        /// <summary>True while the box is a circle of `Radius` round the can; false, the square of half side `Radius`.</summary>
        public static bool Round { get; private set; }
        /// <summary>The box's size on the map being played: `Balance.ConfinementRadius` unless the map said otherwise.</summary>
        public static float Radius { get; private set; } = Balance.ConfinementRadius;
        /// <summary>Counts up on every change, so a drawing of the box knows to redraw.</summary>
        public static int Version { get; private set; }

        /// <summary>A map's own box. Call `Reset` when the map goes.</summary>
        public static void Use(bool round, float radius)
        {
            if (radius <= 0.5f) radius = Balance.ConfinementRadius;
            if (round == Round && radius == Radius) return;
            Round = round; Radius = radius; Version++;
        }

        /// <summary>The ground under the box changed (a new layout of the same size): its drawing redraws.</summary>
        public static void Touch() => Version++;

        /// <summary>Back to the game's own box: a square of `Balance.ConfinementRadius`.</summary>
        public static void Reset() => Use(false, Balance.ConfinementRadius);

        /// <summary>
        /// True while this position is inside the box. Strictly less than, matching
        /// is_inside_box() in character_base.gd, so a body exactly on the chalk counts as
        /// OUT and may therefore throw. The throw gate is the negation of this, so the
        /// two can never disagree about the boundary case.
        /// </summary>
        public static bool IsInsideBox(float x, float z) => IsInsideBox(x, z, Radius);

        /// <summary>As above against a given size, in the shape the map plays (`Round`).</summary>
        public static bool IsInsideBox(float x, float z, float radius)
        {
            if (Round) return x * x + z * z < radius * radius;
            float ax = x < 0 ? -x : x;
            float az = z < 0 ? -z : z;
            return (ax > az ? ax : az) < radius;
        }

        /// <summary>True while this position is in the safe zone outside the danger box.</summary>
        public static bool IsInsideSafeZone(float x, float z) => !IsInsideBox(x, z, Radius);
        public static bool IsInsideSafeZone(float x, float z, float radius) => !IsInsideBox(x, z, radius);

        /// <summary>
        /// Clamp a Defender back into the box. X and Z independently: that is what makes
        /// it a square.
        /// </summary>
        public static void ClampToBox(ref float x, ref float z) => ClampToBox(ref x, ref z, Radius);

        /// <summary>As above against a given size. Round: pulled straight back toward the can.</summary>
        public static void ClampToBox(ref float x, ref float z, float radius)
        {
            if (Round)
            {
                float d2 = x * x + z * z;
                if (d2 > radius * radius) { float k = radius / (float)System.Math.Sqrt(d2); x *= k; z *= k; }
                return;
            }

            if (x < -radius) x = -radius;
            else if (x > radius) x = radius;

            if (z < -radius) z = -radius;
            else if (z > radius) z = radius;
        }

        /// <summary>
        /// Confinement applies to the Defender, and only while the round is live. Written
        /// as its own predicate because the role rotates every round and a cached copy is
        /// one more thing that can be stale on a client.
        /// </summary>
        public static bool IsConfined(bool roundActive, bool isDefender) => roundActive && isDefender;

        /// <summary>
        /// Where the attackers spawn: a ring outside the box, at radius + margin.
        ///
        /// ⚠️ SPAWNS ARE COMPUTED FROM THE BOX, NOT READ FROM MAP MARKERS. "Outside the
        /// box" is the rule, and a marker that drifted half a metre inside the radius
        /// would spawn an Attacker VULNERABLE on frame one. That reads as a rules bug and
        /// gets debugged as one, when it is a map bug.
        /// </summary>
        public static float AttackerSpawnRing() => Radius + Balance.SafeZoneMargin;
        public static float AttackerSpawnRing(float radius) => radius + Balance.SafeZoneMargin;

        /// <summary>
        /// Where you have to stand to throw: just outside the chalk.
        ///
        /// ⚠️ THE THROWING LINE, NOT THE BOX, IS WHAT HAS TO FIT THE MAP. A box at 8.0
        /// puts the line on Eskinita's walls, which would leave an attacker no legal
        /// ground to throw from on the east and west sides at all, only the two open
        /// ends. There is a third bound beyond this one: the AI's standoff ring at
        /// radius + 1.2 must also clear the wall faces at x = ±8.6, and violating it did
        /// not look like a bounds bug. It looked like broken pathfinding, with bots
        /// "walking up the houses" while actually jammed against a wall trying to reach a
        /// goal they could never stand on. Throws over a whole match went 14 to 59 and
        /// knockdowns 5 to 23 once the ring fitted again.
        /// </summary>
        public static float ThrowingLine() => Radius + 1.0f;
        public static float ThrowingLine(float radius) => radius + 1.0f;
    }
}
