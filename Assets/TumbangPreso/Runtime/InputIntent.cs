using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>The verbs a unit can press. One name per Godot input action.</summary>
    public enum Verb
    {
        Sprint,
        Jump,
        SpecialAbility, // Left click. Throw charge for an attacker, punch for the taya.
        Grab,           // E. Contextual: pickup, shove, or the lata reset channel.
        Lunge,          // Right click. The taya's dash tag.
        EmoteWheel,     // B
        Skill1,         // Hero Skill 1. Q
        Skill2,         // Hero Skill 2. E
        Ultimate,       // Hero Ultimate. F
    }

    /// <summary>
    /// ONE FRAME OF INTENT, WHOEVER PRODUCED IT.
    ///
    /// ⚠️⚠️ THIS INDIRECTION IS THE SINGLE BEST THING IN THE GODOT CODEBASE AND IT IS BEING
    /// PORTED DELIBERATELY RATHER THAN SIMPLIFIED AWAY. A bot presses the same buttons a
    /// human does: `ai_set_intent()` writes the same table `input_pressed()` reads, so ONE
    /// physics step serves both and there is no second code path where a bot can do
    /// something a player cannot, or obey a rule a player is not held to.
    ///
    /// It is also what made the AI rewrite affordable and what makes the whole AI layer a
    /// transcription in this port rather than a redesign. Every temptation to let a bot
    /// call a gameplay method directly ("it is simpler, it is only for the AI") reintroduces
    /// the divergence this prevents. Do not take it.
    ///
    /// ⚠️ EDGES ARE DERIVED, NOT REPORTED. The producer sets only the held state; this type
    /// diffs against the previous frame to answer "just pressed" and "just released". A
    /// producer that had to report edges itself could report an impossible pair (released
    /// without ever being pressed), and an AI that missed a frame would silently never fire
    /// a tap-only verb like the shove.
    /// </summary>
    public sealed class InputIntent
    {
        private readonly HashSet<Verb> _held = new HashSet<Verb>();
        private readonly HashSet<Verb> _heldPrev = new HashSet<Verb>();

        public Vector2 Move { get; set; }

        /// <summary>Lateral throw spin for Pektus curve shots (-1.0 left to +1.0 right).</summary>
        public float SpinInput { get; set; }

        /// <summary>Where this unit is aiming, in world space. AI writes it directly;
        /// a human's comes from the camera ray.</summary>
        public Vector3 AimPoint
        {
            get => _aimPoint;
            set { _aimPoint = value; HasAimPoint = true; }
        }

        private Vector3 _aimPoint;

        /// <summary>
        /// ⚠️⚠️ "NOBODY HAS WRITTEN AN AIM POINT" IS A REAL STATE AND IT IS NOT THE ORIGIN. The
        /// .gd spells it `ai_aim_point == Vector3.INF` and branches on it, because a default
        /// Vector3 is a legal position in the arena — the middle of the base circle, which is
        /// exactly where the lata stands. A human seat that has never had a point written would
        /// otherwise aim at the can from wherever it stood, which looks like a working aim
        /// assist right up until the player turns around.
        /// </summary>
        public bool HasAimPoint { get; private set; }

        /// <summary>
        /// Turn the body toward <see cref="AimPoint"/> even when no movement key is held.
        ///
        /// Bots use this while charging a throw. The throw solver has always aimed the
        /// tsinelas at the lata, but a movement-aimed body only changed yaw while walking, so
        /// a bot that arrived with its back to the can could release a correct backwards shot.
        /// Keeping the request in the shared intent preserves the one-input-path rule and lets
        /// the ordinary motor apply the same bounded turn used for movement.
        /// </summary>
        public bool FaceAimPoint { get; set; }

        /// <summary>
        /// ⚠️ PARKED INPUT IS NOT THE SAME AS NO INPUT. A unit whose input is parked (a
        /// menu is open, the round is over, they are mid-emote) must report everything
        /// released rather than simply stop updating, or a verb held across the boundary
        /// stays held forever and the player walks out of the pause menu already sprinting.
        /// </summary>
        public bool Parked { get; set; }

        public void Set(Verb v, bool pressed)
        {
            if (pressed) _held.Add(v);
            else _held.Remove(v);
        }

        /// <summary>Call once at the end of every producer's frame, after all Set calls.</summary>
        public void CommitFrame()
        {
            _heldPrev.Clear();
            foreach (var v in _held) _heldPrev.Add(v);
        }

        public void Clear()
        {
            _held.Clear();
            Move = Vector2.zero;
            SpinInput = 0.0f;
            HasAimPoint = false;
            FaceAimPoint = false;
        }

        /// <summary>Clears producer-owned aiming state without releasing held verbs.</summary>
        public void ClearAim()
        {
            HasAimPoint = false;
            FaceAimPoint = false;
        }

        /// <summary>
        /// The verbs this unit is currently allowed to use, or null for "all of them".
        ///
        /// ⚠️⚠️ IT EXISTS FOR THE GUIDED ROUTE AND NOTHING ELSE, AND IT DEFAULTS TO NULL SO
        /// EVERY MATCH IS UNTOUCHED. 🧑, 2026-08-26: *"i dont want there to be bots and other
        /// shit like skills or throwing until the tutorial wants u to actually do it bcz its
        /// confusing that i can do a lot of shit, theres a tendency to not follow and focus on
        /// tutorial"*. A tutorial that hands a player nine verbs on lesson one is not teaching
        /// an order, it is presenting a menu.
        ///
        /// ⚠️ IT FILTERS HERE RATHER THAN AT THE CONSUMERS, for the same reason `Parked` does:
        /// there are a dozen readers of this table across the carrier, the verbs, the motor and
        /// the ability system, and a rule enforced at each of them is a rule that is missing from
        /// whichever one gets written next. One table, one gate.
        ///
        /// ⚠️ AND IT IS A LOCK, NOT A REBIND. The key still exists and still says what it does;
        /// pressing it simply resolves to nothing until the route has taught it. Rebinding or
        /// disabling actions would fight `Settings.Rebinding` and would show the player a
        /// different control scheme from the one they will play with.
        /// </summary>
        private HashSet<Verb> _allowed;

        /// <summary>Restricts this unit to the given verbs. Null clears the restriction.</summary>
        public void AllowOnly(HashSet<Verb> verbs) => _allowed = verbs;

        private bool Locked(Verb v) => _allowed != null && !_allowed.Contains(v);

        public bool Pressed(Verb v) => !Parked && !Locked(v) && _held.Contains(v);

        public bool JustPressed(Verb v)
            => !Parked && !Locked(v) && _held.Contains(v) && !_heldPrev.Contains(v);

        public bool JustReleased(Verb v)
            => !Parked && !Locked(v) && !_held.Contains(v) && _heldPrev.Contains(v);

        public Vector2 MoveAxis => Parked ? Vector2.zero : Move;
    }
}
