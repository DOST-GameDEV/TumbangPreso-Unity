using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Nemu's ultimate, LET THEM IN (owner brief 2026-10-11, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
    /// section 2): every other player is POSSESSED for 15 s and a ghost contests their steering, the way a Left 4
    /// Dead 2 survivor fights a Jockey. The ghost never takes the stick. It ADDS its pull to what the player asks
    /// for and the sum is clamped to a full walk, so a player pushing straight against it keeps most of their speed
    /// and is only held in place at the peak of a surge.
    ///
    /// Everything here is a pure function of the seat, the status clock and replicated positions, so the body's own
    /// peer (which moves it), the host (which owns the status) and every observer (which draws the rider leaning)
    /// all compute the same pull. Grip is the one local value: the simulating peer keeps it, like the stick.
    /// </summary>
    public static class NemuRules
    {
        /// <summary>Owner, 2026-10-11: *"Apply POSSESSED for 15 gameplay seconds"*.</summary>
        public const float PossessedSeconds = 15.0f;

        /// <summary>Owner, 2026-10-11: *"maybe possessed players get 10% slow too"*. A flat 10 % off the walk, on top of the contest.</summary>
        public const float PossessedSpeedScale = 0.9f;

        /// <summary>Owner, 2026-10-11: *"pls limit ult to below 6 seconds"*. The cutscene's authored length.</summary>
        public const float ProcessionSeconds = 5.8f;

        /// <summary>The rider finds its seat (the pull ramps in), and climbs out at the end (it ramps out).</summary>
        public const float SettleSeconds = 0.6f;
        public const float LeaveSeconds = 1.2f;

        /// <summary>
        /// The pull, as a share of a full walk. DRIFT: hands-off you walk at 42 % where it wants, and pushing straight
        /// against it you keep 58 %. TELL: it slackens to 20 % as it inhales before a surge (the read). SURGE: 105 %,
        /// so hands-off you are walked at full speed and pushing straight against it at full grip you are HELD, drifting
        /// 5 % of a walk its way: never thrown, never stopped from acting.
        /// </summary>
        public const float DriftPull = 0.42f;
        public const float TellPull = 0.20f;
        public const float SurgePull = 1.05f;
        public const float TellSeconds = 0.5f;
        public const float SurgeSeconds = 0.75f;
        /// <summary>How quickly a surge reaches full strength and lets go.</summary>
        public const float SurgeAttack = 0.1f, SurgeRelease = 0.15f;

        /// <summary>
        /// GRIP, the skill expression. Fighting it (at least half a stick, more than 110 degrees from the pull) wears it
        /// down 0.12 a second to 0.45; otherwise it recovers 0.05 a second. At the floor, steady fighting keeps 81 % of a
        /// walk and a surge keeps 53 %.
        /// </summary>
        public const float GripFloor = 0.45f;
        public const float GripFallPerSecond = 0.12f;
        public const float GripRecoverPerSecond = 0.05f;
        public const float ResistMinInput = 0.5f;
        public const float ResistMinDegrees = 110.0f;

        /// <summary>The pull sways round its lure by up to this, and nearly straightens during a surge.</summary>
        public const float SwayDegrees = 28.0f;
        public const float SurgeSwayScale = 0.25f;

        /// <summary>Closer than this to the lure there is no direction to pull in.</summary>
        public const float LureDeadZone = 0.3f;

        public const int SurgesPerPossession = 6;

        /// <summary>
        /// When each seat's ghost surges, seconds into the possession. TYPED PER SEAT so four victims never lurch within
        /// 0.15 s of each other, irregular so a player cannot count it, every surge clear of the settle and of the leave.
        /// </summary>
        private static readonly float[][] Surges =
        {
            new[] { 1.9f, 4.4f, 6.6f, 8.9f, 10.9f, 12.7f },
            new[] { 2.4f, 4.9f, 7.1f, 9.5f, 11.5f, 12.95f },
            new[] { 1.6f, 3.9f, 6.2f, 8.4f, 10.6f, 12.3f },
            new[] { 2.8f, 5.2f, 7.4f, 9.7f, 11.25f, 12.5f },
        };

        public static float SurgeStart(int seat, int index) => Surges[SeatRow(seat)][index];

        private static int SeatRow(int seat) => ((seat % Surges.Length) + Surges.Length) % Surges.Length;

        public enum Phase { None, Settle, Drift, Tell, Surge, Leave }

        /// <summary>What the ghost is doing at <paramref name="elapsed"/> seconds into the possession.</summary>
        public static Phase PhaseAt(float elapsed, int seat)
        {
            if (!(elapsed >= 0.0f) || elapsed >= PossessedSeconds) return Phase.None;
            if (elapsed >= PossessedSeconds - LeaveSeconds) return Phase.Leave;
            foreach (float s in Surges[SeatRow(seat)])
            {
                if (elapsed >= s && elapsed < s + SurgeSeconds) return Phase.Surge;
                if (elapsed >= s - TellSeconds && elapsed < s) return Phase.Tell;
            }
            return elapsed < SettleSeconds ? Phase.Settle : Phase.Drift;
        }

        /// <summary>0 to 1 within a surge (attack, hold, release); 0 outside one.</summary>
        public static float SurgeEnvelope(float elapsed, int seat)
        {
            foreach (float s in Surges[SeatRow(seat)])
            {
                float u = elapsed - s;
                if (u < 0.0f || u >= SurgeSeconds) continue;
                float rise = Clamp01(u / SurgeAttack), fall = Clamp01((SurgeSeconds - u) / SurgeRelease);
                return Math.Min(rise, fall);
            }
            return 0.0f;
        }

        /// <summary>0 to 1 through a tell (how far the ghost has inhaled); 0 outside one.</summary>
        public static float TellAmount(float elapsed, int seat)
        {
            foreach (float s in Surges[SeatRow(seat)])
            {
                float u = elapsed - (s - TellSeconds);
                if (u >= 0.0f && u < TellSeconds) return u / TellSeconds;
            }
            return 0.0f;
        }

        /// <summary>The pull's strength (a share of a full walk) before grip.</summary>
        public static float Strength(float elapsed, int seat)
        {
            var phase = PhaseAt(elapsed, seat);
            if (phase == Phase.None) return 0.0f;
            float settle = Clamp01(elapsed / SettleSeconds);
            float leave = Clamp01((PossessedSeconds - elapsed) / LeaveSeconds);
            float pull = DriftPull;
            if (phase == Phase.Tell) pull = TellPull;
            float surge = SurgeEnvelope(elapsed, seat);
            if (surge > 0.0f) pull = DriftPull + (SurgePull - DriftPull) * surge;
            return pull * settle * leave;
        }

        /// <summary>The sway round the lure, degrees, a seeded pair of slow sines.</summary>
        public static float SwayAt(float elapsed, int seat)
        {
            double t = elapsed;
            float sway = (float)(0.7 * Math.Sin(0.9 * t + 1.7 * seat) + 0.3 * Math.Sin(2.3 * t + seat));
            float straighten = 1.0f - (1.0f - SurgeSwayScale) * SurgeEnvelope(elapsed, seat);
            return SwayDegrees * sway * straighten;
        }

        /// <summary>
        /// Where the ghosts pull their host: always the worst place for them, which is always good for Nemu. The taya
        /// is pulled away from the can; an attacker toward the taya; with no taya on court, toward the can. Returns
        /// false (no direction) when the host is already on the lure.
        /// </summary>
        public static bool Lure(int seat, float x, float z, bool isTaya, bool hasTaya, float tayaX, float tayaZ,
                                float canX, float canZ, out float dx, out float dz)
        {
            if (isTaya)
            {
                dx = x - canX; dz = z - canZ;
                if (dx * dx + dz * dz < LureDeadZone * LureDeadZone)
                {
                    // Standing on the can: a fixed way out per seat, so every peer agrees.
                    double a = (SeatRow(seat) * 90.0 + 45.0) * Math.PI / 180.0;
                    dx = (float)Math.Sin(a); dz = (float)Math.Cos(a);
                    return true;
                }
            }
            else if (hasTaya) { dx = tayaX - x; dz = tayaZ - z; }
            else { dx = canX - x; dz = canZ - z; }
            float length = (float)Math.Sqrt(dx * dx + dz * dz);
            if (!(length >= LureDeadZone)) { dx = 0.0f; dz = 0.0f; return false; }
            dx /= length; dz /= length;
            return true;
        }

        /// <summary>The ghost's pull (world x/z, a share of a full walk) before grip: the lure, swayed, scaled.</summary>
        public static void Pull(float elapsed, int seat, float lureX, float lureZ, out float px, out float pz)
        {
            float strength = Strength(elapsed, seat);
            double a = SwayAt(elapsed, seat) * Math.PI / 180.0;
            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            px = (lureX * c + lureZ * s) * strength;
            pz = (-lureX * s + lureZ * c) * strength;
        }

        /// <summary>THE CONTEST: the player's wish plus the gripped pull, clamped to a full walk.</summary>
        public static void Contest(float inputX, float inputZ, float pullX, float pullZ, float grip, out float wx, out float wz)
        {
            float g = Clamp(grip, GripFloor, 1.0f);
            wx = inputX + pullX * g; wz = inputZ + pullZ * g;
            float m = (float)Math.Sqrt(wx * wx + wz * wz);
            if (m > 1.0f) { wx /= m; wz /= m; }
        }

        /// <summary>True while the player is fighting the pull: enough stick, far enough from its direction.</summary>
        public static bool Resisting(float inputX, float inputZ, float pullX, float pullZ)
        {
            float im = (float)Math.Sqrt(inputX * inputX + inputZ * inputZ);
            float pm = (float)Math.Sqrt(pullX * pullX + pullZ * pullZ);
            if (im < ResistMinInput || pm < 1e-4f) return false;
            float cos = (inputX * pullX + inputZ * pullZ) / (im * pm);
            return cos < (float)Math.Cos(ResistMinDegrees * Math.PI / 180.0);
        }

        public static float StepGrip(float grip, bool resisting, float dt)
        {
            if (!(dt > 0.0f)) return Clamp(grip, GripFloor, 1.0f);
            float next = resisting ? grip - GripFallPerSecond * dt : grip + GripRecoverPerSecond * dt;
            return Clamp(next, GripFloor, 1.0f);
        }

        private static float Clamp01(float v) => v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        private static float Clamp(float v, float lo, float hi) => float.IsNaN(v) ? hi : v < lo ? lo : v > hi ? hi : v;
    }
}
