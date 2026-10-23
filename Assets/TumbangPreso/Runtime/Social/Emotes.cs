using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Social
{
    /// <summary>
    /// The emote wheel's contents and the rules around playing one.
    ///
    /// ⚠️⚠️ THE SET IS FIXED AND IS NOT PLAYER-CONFIGURABLE. That was decided explicitly and
    /// reversed a configurable version, so do not add a customisation screen back. A fixed set
    /// means every player recognises every emote instantly, which is the whole social point of
    /// them, and it means the wheel's geometry is constant rather than reflowing per player.
    ///
    /// ⚠️ THE LABEL AND THE NAME ARE SEPARATE FIELDS FOR A LAYOUT REASON. A wheel segment is
    /// narrow and a long word either clips or shrinks the whole ring's type size; the centre
    /// has room for the full name once a segment is selected. Merging them means either the
    /// ring is cramped or the centre is terse.
    /// </summary>
    public static class Emotes
    {
        /// <summary>
        /// ⚠️ SEVEN, AND THE COUNT IS LOAD-BEARING FOR THE WHEEL. Segments are 360/count, and
        /// selection is derived from the cursor angle. Adding one silently re-aims every
        /// player's muscle memory, so treat the count as part of the contract rather than as an
        /// array length.
        /// </summary>
        /// <para>⚠️⚠️ A NEW SET ON THE OLD IDS (owner, 2026-10-23: "give our game completely new emotes"). The ids
        /// cross the network, so they never change; what each one MEANS did: one feeling per slice, after the emotes
        /// players actually use in Fortnite, PUBG and CoD (`HeroAbilityClips.Emotes.cs` has the research and the
        /// clips). `crouch` is HA HA and `tpose` is COME AT ME. An older peer still plays its own clip for the id.</para>
        public static readonly IReadOnlyList<EmoteDef> All = new[]
        {
            new EmoteDef("yes",    "LET'S GO", "LET'S GO!"),
            new EmoteDef("no",     "NO WAY",   "NO WAY"),
            new EmoteDef("sit",    "TOO EASY", "TOO EASY"),
            new EmoteDef("crouch", "HA HA",    "HA HA HA"),
            new EmoteDef("dance",  "DANCE",    "STREET DANCE"),
            new EmoteDef("tpose",  "COME AT ME", "COME AT ME"),
            new EmoteDef("bow",    "GG",       "GOOD GAME"),
        };

        public static int Count => All.Count;

        public static bool IsKnown(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            foreach (var e in All)
                if (e.Id == id) return true;

            return false;
        }

        /// <summary>
        /// Which segment a cursor angle selects. Angle in degrees, 0 at the top, clockwise.
        ///
        /// ⚠️ THE MODULO IS NOT DEFENSIVE PADDING. An angle can arrive at exactly 360 from a
        /// cursor sitting dead on the boundary, and without the wrap that indexes one past the
        /// end at the single most reachable position on the wheel.
        /// </summary>
        public static int SegmentFor(float angleDegrees)
        {
            float span = 360.0f / Count;
            float a = angleDegrees % 360.0f;
            if (a < 0.0f) a += 360.0f;

            return Mathf.FloorToInt(a / span) % Count;
        }
    }
}
