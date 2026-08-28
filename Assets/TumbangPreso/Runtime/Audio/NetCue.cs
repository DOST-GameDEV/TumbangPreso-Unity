using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// A world sound that every peer hears, rather than only the one that made it.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE AN AUDIT SAID IT HAD TO, AND THE AUDIT IS WORTH READING BEFORE
    /// TOUCHING THIS. 🧑 2026-08-26: *"Verify SFX actually reach every peer in multiplayer.
    /// This is unverified and is the risky one."* `tools/audit_audio_reach.py` walks every
    /// `GameServices.Audio` call in the runtime tree, finds the enclosing method and reports
    /// whether a `NetAuthority.ShouldResolve()` early return is open at that brace depth. Two
    /// call sites came back HOST-ONLY on the first run and both are load-bearing sounds:
    ///
    ///   * `Carrier.HostThrowAt` plays `throw_release`. **Nobody but the host has ever heard a
    ///     throw in a networked match**, and the throw is the game's most frequent verb.
    ///   * `Lata.HostKnockDown` plays `lata_seal`. That is the sound of the OBJECTIVE going
    ///     over, which is the single most important event in a round.
    ///
    /// ⚠️ THE FIX IS NOT TO MOVE THE CALL OUT OF THE GATE. The gate is correct: the host is the
    /// only peer that may DECIDE a throw happened. What was wrong is that deciding and announcing
    /// were the same line. This separates them, which is the shape `NetAuthority`'s class note
    /// already describes for every verb: *"host decides -> HostResolveX() ... host announces ->
    /// RpcX()"*.
    ///
    /// ⚠️⚠️ AND IT IS A NO-OP IN SINGLE PLAYER, WHICH IS WHY IT CAN BE USED EVERYWHERE.
    /// `NetAuthority.IsNetworked` is false with no transport running, so this is exactly
    /// `GameServices.Audio.PlayAt` in a solo match, in the bot probes and in every editor check.
    /// Nothing about the offline game changes, and no call site has to ask which mode it is in.
    ///
    /// ⚠️ THE CALLER PLAYS IT LOCALLY EITHER WAY, so the peer that made the sound hears it on the
    /// frame it happened rather than after a round trip. The host's relay then goes to everyone
    /// EXCEPT that peer, so nothing is heard twice.
    ///
    /// ⚠️ IT IS FOR WORLD EVENTS ONLY. A refusal tick, a UI click and the announcer are local by
    /// definition and must not come through here: `HeroAbilitySystem.PlayRefusal`'s note is
    /// explicit that its cue is *"on the player rather than at a world point"*, and broadcasting
    /// it would play one player's mis-press in three other people's ears.
    /// </summary>
    public static class NetCue
    {
        private static int _relaySuppressionDepth;

        /// <summary>
        /// A replicated copy plays the cue locally but must not send it back around the wire.
        /// This scope also covers delayed wind-ups when HeroAbilitySystem ticks a remote kit.
        /// Without it one cast becomes one cue per peer, each peer relays that copy again, and
        /// a four-player ultimate is heard sixteen times.
        /// </summary>
        /// <summary>
        /// ⚠️ IT RETURNS A STRUCT, NOT AN `IDisposable`, AND THAT IS A MEASUREMENT NOT A STYLE
        /// CHOICE. This scope wraps `HeroKit.Tick`, which runs once per seat per frame on every
        /// peer: four seats at 60 fps is 240 scopes a second, and a class here is 240 garbage
        /// objects a second for the whole match. `using` on a struct with a concrete type binds
        /// `Dispose` directly and boxes nothing. Assigning the result to an `IDisposable` local
        /// would box it and put the allocation straight back.
        /// </summary>
        public static RelayScope SuppressRelay()
        {
            _relaySuppressionDepth++;
            return default;
        }

        public readonly struct RelayScope : IDisposable
        {
            public void Dispose() => _relaySuppressionDepth = Math.Max(0, _relaySuppressionDepth - 1);
        }

        /// <summary>Play a world cue here, and on every other peer.</summary>
        public static void Play(string id, Vector3 position)
        {
            GameServices.Audio?.PlayAt(id, position);
            Relay(id, position, 1.0f);
        }

        /// <summary>As <see cref="Play"/>, with the pitch window <see cref="AudioDirector"/> uses.</summary>
        public static void PlayVaried(string id, Vector3 position,
                                      float pitchMin = 0.94f, float pitchMax = 1.06f,
                                      float volumeScale = 1.0f)
        {
            GameServices.Audio?.PlayAtVaried(id, position, pitchMin, pitchMax, volumeScale);
            Relay(id, position, volumeScale);
        }

        /// <summary>
        /// ⚠️ THE REMOTE COPY IS NOT PITCH-VARIED, AND THAT IS DELIBERATE RATHER THAN LAZY. The
        /// window exists so a repeated sample does not expose itself as one recording
        /// (`AudioDirector.PlayAtVaried`), and it is a per-LISTENER effect: every peer rolling
        /// its own pitch gives the same variety for free, while sending the roll would cost four
        /// bytes a cue to make four machines agree about something no player can compare.
        /// </summary>
        private static void Relay(string id, Vector3 position, float volumeScale)
        {
            if (!NetAuthority.IsNetworked) return;
            if (_relaySuppressionDepth > 0) return;
            if (string.IsNullOrEmpty(id)) return;

            Net.MatchRpc.Instance?.BroadcastCue(id, position, volumeScale);
        }
    }
}
