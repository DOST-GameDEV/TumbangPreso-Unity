using System;
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
        private Visual.CharacterAnimator _animator;

        public string Current { get; private set; }
        public bool IsEmoting => !string.IsNullOrEmpty(Current);

        private void Awake() => _motor = GetComponent<CharacterMotor>();

        /// <summary>
        /// ⚠️ RE-ASKED WHILE IT IS NULL, NOT CACHED ONCE IN Awake. `CharacterVisual.ApplyModel`
        /// adds `CharacterAnimator` to this same GameObject well after this component's own
        /// Awake has already run — the model, and everything it carries, is instanced later —
        /// so caching the reference once left it null for the rest of the object's life and no
        /// emote could ever pass the `HasEmoteClip` gate below. Same fault `Carrier.Hand()`
        /// carries its own note about, for the same reason.
        /// </summary>
        private Visual.CharacterAnimator Animator
        {
            get
            {
                if (_animator == null) _animator = GetComponent<Visual.CharacterAnimator>();
                return _animator;
            }
        }

        /// <summary>`character_base.gd::can_emote`: `state == NORMAL and not is_emoting()`.
        /// The second half is what stops a press mid-emote from restarting the clip and
        /// re-triggering the camera swing on top of itself.</summary>
        public bool CanEmote() =>
            _motor != null && _motor.CanAct() && !IsEmoting;

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

        /// <summary>
        /// Applied on every peer, including the one that asked.
        ///
        /// ⚠️⚠️ ONLY FIRES IF THE ANIMATION ACTUALLY RESOLVES ON THIS RIG. `character_visual.gd
        /// ::play_emote` returns false and sets nothing when the animator is missing or none of
        /// the id's candidate clips exist, and `character_base.gd::play_emote` reads that return
        /// value before ever calling `rig.begin_emote_view()`. Doing the same here is what keeps
        /// the camera from swinging to third person to orbit a body that never moved: an unknown
        /// id, or a model still mid-swap with no `CharacterAnimator` bound yet, leaves `Current`
        /// null and fires nothing, exactly as if the press had never happened.
        /// </summary>
        public void Play(string id)
        {
            if (Animator == null || !Animator.HasEmoteClip(id)) return;

            Current = id;
            EmoteStarted?.Invoke(id);
        }

        public void Stop()
        {
            if (!IsEmoting) return;

            Current = null;
            EmoteStopped?.Invoke();
        }

        /// <summary>
        /// `character_base.gd::_cancel_emote_on_input` + `_on_state_changed_emote`, folded into
        /// one poll rather than two signal handlers, with the same effect: any real input or any
        /// loss of the NORMAL playable state ends the emote immediately.
        ///
        /// ⚠️ JUST-PRESSED FOR THE BUTTONS, HELD FOR THE AXES, MATCHING THE .gd EXACTLY.
        /// `input_just_pressed` for jump/sprint/grab/special_ability means a button already
        /// held down before the emote started does not retroactively cancel it the instant the
        /// emote begins; `input_pressed` for the four move directions means the axis is read
        /// live, the same way movement itself is. Reading held state for the buttons too was the
        /// port's own bug: it cancelled an emote on frame one whenever sprint was already down.
        ///
        /// ⚠️ NO LUNGE HERE, AND THAT IS NOT AN OMISSION. The .gd's cancel list is exactly
        /// move/jump/sprint/grab/special_ability; lunge is E-held resolved by `Carrier`, and
        /// `Verb.Grab` already fires on the same press before the hold decides what it becomes.
        /// </summary>
        private void Update()
        {
            if (!IsEmoting) return;

            if (Animator != null && Animator.EmoteClipFinished) { Stop(); return; }

            var intent = _motor.Intent;
            bool acted = intent.MoveAxis.sqrMagnitude > 0.01f
                         || intent.JustPressed(Verb.Jump)
                         || intent.JustPressed(Verb.Sprint)
                         || intent.JustPressed(Verb.Grab)
                         || intent.JustPressed(Verb.SpecialAbility);

            if (acted || !_motor.CanAct()) Stop();
        }
    }
}
