using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Social
{
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
