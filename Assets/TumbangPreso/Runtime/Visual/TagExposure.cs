using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CAN THE TAYA TAG THIS BODY RIGHT NOW, AND HOW CLOSE IS THAT TO CHANGING (owner, 2026-10-23: *"maybe use pacman as
    /// reference"*, *"they get slowly covered by a color highlight everytime theyre taggable"*, *"it climbs up their body
    /// really quick"*). Design: docs/reports/tag-readability-2026-10-23/README.md.
    ///
    /// One reading for every cue on every screen, from replicated state only, as a LEVEL the body fills to:
    ///   * <see cref="State.Taggable"/> (<see cref="RoundDirector.CanBeTaggedNow"/>, the call the tag makes): full. In the
    ///     last second of Vulnerable, when its end will make them safe, the level drains with the time left.
    ///   * While the can is down nobody is marked, the taya standing it up included: the fill climbs only once the can
    ///     is up (owner, 2026-10-23: "ppl get highlighted the moment i click raise can (or even if can isnt raised)",
    ///     "they js get highlighted the moment na nakataas na can"). The restore channel had filled bodies early.
    ///   * <see cref="State.Safe"/>: empty. Safe is the default look; only the exception is marked.
    /// <see cref="Fill"/> is the level as drawn: it climbs quickly and drains a little slower, so every change is seen as
    /// the colour moving. `TayaIdentity` advances every body once a frame, so the fill keeps time whether or not a
    /// camera is drawing that body. Presentation only: it decides nothing.
    /// </summary>
    public static class TagExposure
    {
        public enum State { Safe, Taggable }

        /// <summary>The last stretch of Vulnerable that drains.</summary>
        public const float EndingSeconds = 1f;
        /// <summary>Feet to head when someone becomes taggable ("really quick").</summary>
        public const float ClimbSeconds = .22f;
        /// <summary>Head to feet when someone becomes safe.</summary>
        public const float DrainSeconds = .3f;

        public static State Read(CharacterMotor actor, out float level)
        {
            level = 0;
            var round = GameServices.Round;
            if (round == null || actor == null || actor.IsDefender || !round.RoundActive) return State.Safe;
            if (round.CanBeTaggedNow(actor))
            {
                level = 1;
                // Vulnerable is running out, and nothing else keeps them exposed: they are about to be safe.
                if (actor.IsVulnerable && actor.VulnerableLeft < EndingSeconds && !(actor.HoldingSlipper && actor.IsInsideBox()))
                    level = Mathf.Clamp01(actor.VulnerableLeft / EndingSeconds);
                return State.Taggable;
            }
            return State.Safe;
        }

        private sealed class Drawn { public float Fill; public int Frame = -1; public float At = -1; }
        private static readonly Dictionary<CharacterMotor, Drawn> _drawn = new Dictionary<CharacterMotor, Drawn>();
        private static int _prunedAt;

        /// <summary>
        /// The fill height as drawn this frame, 0 (feet) to 1 (over the head): the level from <see cref="Read"/>, reached by
        /// climbing or draining rather than in a jump. Advanced once a frame however many cues ask; Reduced UI motion
        /// snaps straight to the level.
        /// </summary>
        public static float Fill(CharacterMotor actor)
        {
            if (actor == null) return 0;
            int frame = Time.frameCount;
            if (frame - _prunedAt > 600) { _prunedAt = frame; Prune(); }
            if (!_drawn.TryGetValue(actor, out var drawn)) _drawn[actor] = drawn = new Drawn();
            if (drawn.Frame == frame) return drawn.Fill;
            // Real time since this body was last advanced, so a body nobody asked about for a while catches up.
            float now = Time.unscaledTime;
            float dt = drawn.At < 0 ? 0 : Mathf.Clamp(now - drawn.At, 0, 1);
            drawn.Frame = frame; drawn.At = now;
            Read(actor, out float level);
            if (Settings.SettingsStore.Current.ReducedUiMotion) drawn.Fill = level;
            else drawn.Fill = Mathf.MoveTowards(drawn.Fill, level, dt / (level > drawn.Fill ? ClimbSeconds : DrainSeconds));
            return drawn.Fill;
        }

        private static readonly List<CharacterMotor> _gone = new List<CharacterMotor>();
        private static void Prune()
        {
            _gone.Clear();
            foreach (var pair in _drawn) if (pair.Key == null) _gone.Add(pair.Key);
            foreach (var key in _gone) _drawn.Remove(key);
            _gone.Clear();
            foreach (var pair in _floors) if (pair.Key == null) _gone.Add(pair.Key);
            foreach (var key in _gone) _floors.Remove(key);
        }

        // ── WHEN THE CUES STEP ASIDE, AND WHERE THE FLOOR MARKS SIT (owner, 2026-10-23: "all of the markers and circles
        // disappear during ult cutscenes", "think abt how ur markers look when ground is uneven, ppl are doing ults",
        // "amihan flying", "falling down", "in some cases the circle should disappear BUT they should still be highlighted").
        //
        // The body is the main signal; the floor mark supports it. So:
        //   * every cutscene (a sealed ultimate, the opening film, arrival, halftime, the role-swap card) and every replay
        //     on this screen fades ALL of it out, body and floor, in 0.15 s, and back after;
        //   * otherwise the body keeps its fill or outline in the air, over a void, in water; only the floor mark goes:
        //     it sits on the floor under the body (a jump or a flight leaves it on the ground like a shadow), tilted to
        //     the slope, fading out between 1.5 and 5 m up, and gone when there is no floor under them at all.

        /// <summary>True while a cutscene or a replay owns this screen.</summary>
        public static bool Quiet => QuietOverride ??
            (SpectatorCutscene.Hides || SharedUltimatePhase.CinematicShowing || CameraSystem.CatchReconstruction.AnyPlaying ||
             CameraSystem.LocalReplayPlayback.Active);

        /// <summary>For tests and review tools: force <see cref="Quiet"/> on or off; null is the game's own answer.</summary>
        public static bool? QuietOverride;

        private static float _presence = 1;
        private static int _presenceFrame = -1;
        /// <summary>1 in play, fading to 0 over 0.15 s while <see cref="Quiet"/>. Multiplies every tag cue.</summary>
        public static float Presence
        {
            get
            {
                if (_presenceFrame == Time.frameCount) return _presence;
                _presenceFrame = Time.frameCount;
                float target = Quiet ? 0 : 1;
                _presence = Settings.SettingsStore.Current.ReducedUiMotion ? target
                    : Mathf.MoveTowards(_presence, target, Mathf.Min(Time.unscaledDeltaTime, .1f) / .15f);
                return _presence;
            }
        }

        /// <summary>Height above the floor where a floor mark starts to fade, and where it is gone.</summary>
        public const float FadeFrom = 1.5f, FadeTo = 5f;
        private const float MaxTilt = 28f;

        private sealed class Floor { public int Frame = -1, TiltFrame = -999; public bool Found; public Vector3 At; public Quaternion Tilt = Quaternion.identity; public float Fade, Lift; }
        private static readonly Dictionary<CharacterMotor, Floor> _floors = new Dictionary<CharacterMotor, Floor>();

        /// <summary>
        /// Where a floor mark of <paramref name="radius"/> belongs under <paramref name="actor"/>: on the floor (not the
        /// feet), tilted to the slope, with how much of it to draw (fading with height). False with no floor in reach.
        /// The tilt samples four points round the edge every few frames; on steps it settles on the higher side, so the mark
        /// sits over a lower step rather than sinking into a higher one.
        /// </summary>
        public static bool OnFloor(CharacterMotor actor, float radius, out Vector3 at, out Quaternion tilt, out float fade)
        {
            at = default; tilt = Quaternion.identity; fade = 0;
            if (actor == null) return false;
            if (!_floors.TryGetValue(actor, out var floor)) _floors[actor] = floor = new Floor();
            int frame = Time.frameCount;
            if (floor.Frame != frame)
            {
                floor.Frame = frame;
                Vector3 feet = actor.transform.position;
                floor.Found = VfxShapes.TryGroundUnder(feet, .6f, FadeTo + 1, out float y);
                if (floor.Found)
                {
                    float height = feet.y - y;
                    floor.Fade = 1 - Mathf.Clamp01((height - FadeFrom) / (FadeTo - FadeFrom));
                    if (frame - floor.TiltFrame >= 4)
                    {
                        floor.TiltFrame = frame;
                        float r = radius * .85f, top = y;
                        float Probe(Vector3 offset)
                        {
                            var p = new Vector3(feet.x, y, feet.z) + offset;
                            if (!VfxShapes.TryGroundUnder(p, .45f, .45f, out float h)) return y;
                            top = Mathf.Max(top, h); return h;
                        }
                        float east = Probe(Vector3.right * r), west = Probe(Vector3.left * r);
                        float north = Probe(Vector3.forward * r), south = Probe(Vector3.back * r);
                        var normal = new Vector3(-(east - west) / (2 * r), 1, -(north - south) / (2 * r)).normalized;
                        if (Vector3.Angle(Vector3.up, normal) > MaxTilt) normal = Vector3.Slerp(Vector3.up, normal, MaxTilt / Vector3.Angle(Vector3.up, normal));
                        floor.Tilt = Quaternion.FromToRotation(Vector3.up, normal);
                        // Lift by part of whatever stands above the centre (a step), so no edge sinks into it.
                        floor.Lift = Mathf.Max(0, top - y) * .5f;
                    }
                    floor.At = new Vector3(feet.x, y + floor.Lift, feet.z);
                }
            }
            if (!floor.Found || floor.Fade <= .001f) return false;
            at = floor.At; tilt = floor.Tilt; fade = floor.Fade;
            return true;
        }

        /// <summary>The cue colour the player chose (Accessibility, Taggable colour); the taya's blue by default.</summary>
        public static Color Colour => Settings.TagColours.Current;

        /// <summary>True when <paramref name="state"/> wants the body marked at all.</summary>
        public static bool Marked(State state) => state != State.Safe;
    }
}
