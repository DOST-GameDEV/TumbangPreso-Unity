using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Whether a bot that is asking to move is actually going anywhere, measured from where its
    /// body ends up rather than from the velocity it wished for.
    ///
    /// ⚠️⚠️ `AIController.StepUnstick` READ `CharacterMotor.Velocity`, AND THAT IS THE WISH, NOT
    /// THE RESULT. `CharacterMotor.FixedUpdate` writes the steered target speed into `_velocity`
    /// and then hands it to `CharacterController.Move`, which is what a wall refuses. A bot pinned
    /// against geometry therefore reported full walking speed for as long as it pushed, the
    /// `StuckSpeed` test never passed, and the unstick sidestep never fired.
    ///
    /// Measured (docs/reports/claude-engineering-2026-09-15, C1): Hero Strike on Ilalim ng Tulay,
    /// the C1-only build `32e073fd` over `091f9210`, seed 4242, seat 0 in Fetch for 77 seconds at
    /// exactly (-3.34, 0.08, 9.90), `move=(-1, 0)`, `vel=(-2.40, -2.00, 0.00)`, `stuck=0`, its shoe
    /// loose and reachable at (-8.30, 0.26, 11.92): **68 unretrieved-slipper penalties in one
    /// match**. The historical `2fde55d3` seed 42 outlier had a fetching seat frozen at
    /// (-4.63, 0.08, 11.11) for five seconds in the same stretch of the map, and
    /// `AIController.StepUnstick` has read the wished velocity on both builds.
    ///
    /// ⚠️ A WINDOW, NOT ONE FRAME. The world steps at `Time.fixedDeltaTime` and the AI thinks per
    /// rendered frame, so a single frame can legitimately carry no physics step and no movement.
    /// Speed is judged over at least `Window` seconds of accumulated frames.
    /// </summary>
    public sealed class AiStuckWatch
    {
        /// <summary>Shortest span resolved speed is judged over.</summary>
        public const float Window = 0.2f;

        private Vector3 _windowStart;
        private float _windowTime;
        private bool _hasStart;
        private bool _drovePerWindow;

        /// <summary>Seconds of continuous driving without resolved movement.</summary>
        public float StuckSeconds { get; private set; }

        /// <summary>The last judged planar speed, for diagnostics.</summary>
        public float ResolvedSpeed { get; private set; }

        public void Reset()
        {
            _hasStart = false;
            _windowTime = 0.0f;
            _drovePerWindow = false;
            StuckSeconds = 0.0f;
        }

        /// <summary>
        /// Feed one frame. Returns true on the frame the bot has been pressing to move for
        /// `AiTuning.StuckTrigger` without its body covering `AiTuning.StuckSpeed`.
        /// </summary>
        public bool Step(Vector3 position, bool driving, float dt)
        {
            if (!_hasStart)
            {
                _windowStart = position;
                _windowTime = 0.0f;
                _drovePerWindow = driving;
                _hasStart = true;
                return false;
            }

            _windowTime += Mathf.Max(0.0f, dt);
            _drovePerWindow |= driving;
            if (!driving)
            {
                // Standing still on purpose is never stuck.
                StuckSeconds = 0.0f;
                _windowStart = position;
                _windowTime = 0.0f;
                _drovePerWindow = false;
                return false;
            }

            if (_windowTime < Window) return false;

            Vector3 moved = position - _windowStart;
            moved.y = 0.0f;
            ResolvedSpeed = moved.magnitude / _windowTime;

            if (_drovePerWindow && ResolvedSpeed < AiTuning.StuckSpeed) StuckSeconds += _windowTime;
            else StuckSeconds = 0.0f;

            _windowStart = position;
            _windowTime = 0.0f;
            _drovePerWindow = false;

            if (StuckSeconds < AiTuning.StuckTrigger) return false;
            StuckSeconds = 0.0f;
            return true;
        }
    }
}
