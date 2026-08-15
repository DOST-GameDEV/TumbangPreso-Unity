using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Social
{
    /// <summary>One emote a player can pick off the wheel.</summary>
    public struct EmoteDef
    {
        /// <summary>Stable id. ⚠️ CROSSES THE WIRE, so never rename one.</summary>
        public string Id;

        /// <summary>Short label, for the wheel segment where space is tight.</summary>
        public string Label;

        /// <summary>Full name, for the centre of the wheel once a segment is selected.</summary>
        public string Name;

        public EmoteDef(string id, string label, string name)
        {
            Id = id;
            Label = label;
            Name = name;
        }
    }

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
        public static readonly IReadOnlyList<EmoteDef> All = new[]
        {
            new EmoteDef("yes",    "NOD",     "NOD"),
            new EmoteDef("no",     "NOPE",    "NOPE"),
            new EmoteDef("sit",    "SIT",     "SIT DOWN"),
            new EmoteDef("crouch", "VICTORY", "VICTORY POSE"),
            new EmoteDef("dead",   "DEAD",    "PLAY DEAD"),
            new EmoteDef("tpose",  "T-POSE",  "T-POSE"),
            new EmoteDef("bow",    "BOW",     "BOW"),
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

    /// <summary>
    /// Plays an emote on a unit and tells the other peers.
    ///
    /// ⚠️⚠️ AN EMOTE IS CANCELLED BY ANY REAL INPUT, AND THAT IS A SAFETY RULE RATHER THAN A
    /// POLISH ONE. Emotes are played standing still, often deliberately, and a player who
    /// emotes at the wrong moment must be able to abort instantly by simply playing. Without
    /// that, an emote is a self-inflicted stun, and the taya is one lunge away.
    ///
    /// ⚠️ AND IT IS REFUSED WHILE THE PLAYER CANNOT ACT. Emoting out of a stun would let a
    /// player hide the animation that tells everybody else they are stunned.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class EmotePlayer : MonoBehaviour
    {
        public event Action<string> EmoteStarted;
        public event Action EmoteStopped;

        private CharacterMotor _motor;

        public string Current { get; private set; }
        public bool IsEmoting => !string.IsNullOrEmpty(Current);

        private void Awake() => _motor = GetComponent<CharacterMotor>();

        public bool CanEmote() => _motor != null && _motor.CanAct() && !_motor.HoldingSlipper;

        /// <summary>
        /// Local request. ⚠️ IT ASKS THE HOST RATHER THAN PLAYING IMMEDIATELY on a client, so a
        /// peer cannot show everyone an emote the host would have refused. In single player
        /// this is a host with no peers and it plays straight away.
        /// </summary>
        public void Request(string id)
        {
            if (!Emotes.IsKnown(id) || !CanEmote()) return;

            if (NetAuthority.ShouldResolve()) HostPlay(id);
            // Phase 5: else send the request to the host here.
        }

        /// <summary>Host-side. Validates, then this is what gets broadcast.</summary>
        public void HostPlay(string id)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (!Emotes.IsKnown(id) || !CanEmote()) return;

            Play(id);
        }

        /// <summary>Applied on every peer, including the one that asked.</summary>
        public void Play(string id)
        {
            Current = id;
            EmoteStarted?.Invoke(id);
        }

        public void Stop()
        {
            if (!IsEmoting) return;

            Current = null;
            EmoteStopped?.Invoke();
        }

        private void Update()
        {
            if (!IsEmoting) return;

            // Any movement or verb aborts it. See the class note: this is what stops an emote
            // being a self-inflicted stun.
            var intent = _motor.Intent;
            bool acted = intent.MoveAxis.sqrMagnitude > 0.01f
                         || intent.Pressed(Verb.Jump)
                         || intent.Pressed(Verb.Sprint)
                         || intent.Pressed(Verb.Grab)
                         || intent.Pressed(Verb.Lunge)
                         || intent.Pressed(Verb.SpecialAbility);

            if (acted || !_motor.CanAct()) Stop();
        }
    }
}
