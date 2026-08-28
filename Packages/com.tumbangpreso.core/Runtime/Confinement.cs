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
        /// <summary>
        /// True while this position is inside the box. Strictly less than, matching
        /// is_inside_box() in character_base.gd, so a body exactly on the chalk counts as
        /// OUT and may therefore throw. The throw gate is the negation of this, so the
        /// two can never disagree about the boundary case.
        /// </summary>
        public static bool IsInsideBox(float x, float z, float radius = Balance.ConfinementRadius)
        {
            float ax = x < 0 ? -x : x;
            float az = z < 0 ? -z : z;
            return (ax > az ? ax : az) < radius;
        }

        /// <summary>True while this position is in the safe zone outside the danger box.</summary>
        public static bool IsInsideSafeZone(float x, float z, float radius = Balance.ConfinementRadius)
            => !IsInsideBox(x, z, radius);

        /// <summary>
        /// Clamp a Defender back into the box. X and Z independently: that is what makes
        /// it a square.
        /// </summary>
        public static void ClampToBox(ref float x, ref float z, float radius = Balance.ConfinementRadius)
        {
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
        public static float AttackerSpawnRing(float radius = Balance.ConfinementRadius) =>
            radius + Balance.SafeZoneMargin;

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
        public static float ThrowingLine(float radius = Balance.ConfinementRadius) => radius + 1.0f;
    }
}
