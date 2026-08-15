using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// One live round: the clock, the throw gate, the passive defence tick, and the tag.
    ///
    /// ⚠️⚠️ CONTACT IS RESOLVED BY DISTANCE ON THE HOST, NEVER BY A TRIGGER VOLUME. This is
    /// the single most important architectural decision carried over from the Godot build,
    /// and it was made from measurement, not taste: an overlap fires on whichever peer owns
    /// the body, and their `hit_probe` measured 16 of 36 overlaps failing to land, split by
    /// target. Sixteen distance checks a frame on the host is cheaper than one correct
    /// networked overlap, and it can only happen where the score is written.
    ///
    /// It also means the most correctness-critical code in the game has NO physics-engine
    /// dependency, which is most of why this port is tractable at all. Do not "improve" any
    /// of this into Unity triggers.
    /// </summary>
    public sealed class RoundDirector : MonoBehaviour
    {
        public event Action<int, int> Tagged; // (defenderSlot, attackerSlot)

        public bool RoundActive { get; private set; }
        public float TimeLeft { get; private set; }
        public Lata Lata { get; set; }

        private readonly List<CharacterMotor> _players = new List<CharacterMotor>();
        private float _throwCooldownLeft;
        private float _defenseTickAccum;

        /// <summary>Shove credit, for the Sabotage score. slot -> (shover, at time).</summary>
        private readonly Dictionary<int, (int by, float at)> _shoveCredit =
            new Dictionary<int, (int, float)>();

        private float _clock;

        public IReadOnlyList<CharacterMotor> Players => _players;

        public void Register(CharacterMotor m)
        {
            if (!_players.Contains(m)) _players.Add(m);
        }

        public void Clear() => _players.Clear();

        public CharacterMotor PlayerAt(int slot)
        {
            for (int i = 0; i < _players.Count; i++)
                if (_players[i].PlayerSlot == slot) return _players[i];
            return null;
        }

        public void BeginRound()
        {
            RoundActive = true;
            TimeLeft = Balance.RoundTime;
            _throwCooldownLeft = 0.0f;
            _defenseTickAccum = 0.0f;
            _shoveCredit.Clear();

            foreach (var p in _players) p.RoundActive = true;
        }

        /// <summary>⚠️ ANY LIVE FRAME CLEARS THE FREEZE. See Hitstop: no instance owns it, so
        /// this is one of several equally valid places to step it.</summary>
        private void Update() => Hitstop.Step();

        public void EndRound()
        {
            RoundActive = false;
            foreach (var p in _players) p.RoundActive = false;
        }

        private void FixedUpdate()
        {
            if (!RoundActive) return;

            float dt = Time.fixedDeltaTime;
            _clock += dt;
            TimeLeft -= dt;

            if (_throwCooldownLeft > 0.0f)
                _throwCooldownLeft = Mathf.Max(0.0f, _throwCooldownLeft - dt);

            StepPassiveDefence(dt);

            if (TimeLeft <= 0.0f)
            {
                EndRound();
                GameServices.Match.BeginIntermission();
            }
        }

        /// <summary>
        /// ⚠️ THE +10/s IS NOT INCOME, IT IS THE PRIZE FOR KEEPING THE CAN STANDING, and the
        /// distinction was settled by measurement rather than argument. The arithmetic says
        /// 90 uncontested seconds is 900 points; the probe says a taya who presses nothing
        /// collects 38 of them, because the attackers put the can down and it stays down
        /// (upright 4.7% of the round). "Uncontested" is simply not a state this game has.
        ///
        /// ⚠️ AND PLAYING STRICTLY DOMINATES HIDING. A turtling taya and a playing one bank
        /// the same passive income, because the tag does not compete with defence, it stacks
        /// on top of it. There is no passive exploit to close because the passive line is not
        /// on the frontier. DO NOT LOWER THIS NUMBER on the arithmetic alone.
        /// </summary>
        private void StepPassiveDefence(float dt)
        {
            if (Lata == null || !Lata.IsUpright) return;

            _defenseTickAccum += dt;
            while (_defenseTickAccum >= Balance.DefenseTickInterval)
            {
                _defenseTickAccum -= Balance.DefenseTickInterval;
                GameServices.Match.AddScore(GameServices.Match.DefenderSlot, ScoreEvent.DefenseTick);
            }
        }

        /// <summary>
        /// ⚠️ THE CROSSHAIR ASKS THIS SAME FUNCTION, so it greys out for exactly the reasons
        /// the throw refuses. A second opinion about legality is a crosshair that promises a
        /// throw the rules then refuse, which is the most confusing possible failure: the
        /// player sees no reason for nothing to happen.
        /// </summary>
        public bool CanThrow(CharacterMotor who)
        {
            if (who == null) return false;

            var ctx = new ThrowContext
            {
                RoundActive = RoundActive,
                IsDefender = who.IsDefender,
                HoldingSlipper = who.HoldingSlipper,
                LataUpright = Lata != null && Lata.IsUpright,
                ThrowCooldownLeft = _throwCooldownLeft,
                X = who.transform.position.x,
                Z = who.transform.position.z,
                ConfinementRadius = Balance.ConfinementRadius,
            };
            return ThrowRules.CanThrow(in ctx);
        }

        /// <summary>
        /// ⚠️ NOBODY MAY THROW FOR A MOMENT AFTER THE CAN IS STOOD BACK UP. It stops the lata
        /// being re-knocked by a slipper already charged and waiting on the last frame of the
        /// reset channel, which would make the channel unfinishable.
        /// </summary>
        public void NotifyLataRestored() => _throwCooldownLeft = Balance.ThrowRestoreCooldown;

        public void NoteShove(int victimSlot, int shoverSlot)
        {
            if (victimSlot < 0 || shoverSlot < 0 || victimSlot == shoverSlot) return;
            _shoveCredit[victimSlot] = (shoverSlot, _clock);
        }

        /// <summary>
        /// Host-side tag resolution. ⚠️ It asks IsTaggable(), the same function the HUD's
        /// VULNERABLE row asks, so the warning a player sees and the check that catches them
        /// are one function and cannot disagree.
        /// </summary>
        public void ResolveTag(CharacterMotor taya, CharacterMotor victim)
        {
            // ⚠️ THE HOST RE-CHECKS EVERYTHING THE CLIENT ALREADY CHECKED, and that is not
            // redundancy. A client says where it stood, which way it faced and how hard it
            // committed; it never says who it hit. A client that could report a result is a
            // client that can award itself 100 points.
            if (!NetAuthority.ShouldResolve()) return;

            if (!RoundActive || taya == null || victim == null) return;
            if (!taya.IsDefender || !victim.IsTaggable()) return;
            if (Lata == null || !Lata.IsUpright) return;

            GameServices.Match.AddScore(taya.PlayerSlot, ScoreEvent.Tag);

            // ⚠️ SABOTAGE: an attacker who shoved this victim shortly before the tag gets
            // credit for setting it up. Its window has never been measured, because in every
            // whole-match run recorded it fired ZERO times. Measuring the window needs the
            // event to happen first, so it is blocked on frequency, not on the number.
            if (_shoveCredit.TryGetValue(victim.PlayerSlot, out var credit)
                && _clock - credit.at <= Balance.SabotageWindow)
            {
                GameServices.Match.AddScore(credit.by, ScoreEvent.Sabotage);
                _shoveCredit.Remove(victim.PlayerSlot);
            }

            ApplyTagPenalty(victim);
            Tagged?.Invoke(taya.PlayerSlot, victim.PlayerSlot);
        }

        /// <summary>
        /// ⚠️⚠️ A TAG CLEANSES, AND THE SLIPPER GOES HOME WITH THEM. Both were reversals and
        /// both are anti-compounding rules. The moment an attacker is most likely to be
        /// tagged is the moment they are most likely to be empty, so the old behaviour piled
        /// a stun, a spent bar and a live fatigue lockout onto one mistake and the two
        /// INVISIBLE punishments outlasted the one the HUD showed. And leaving the slipper
        /// behind meant a taya who tagged well ended up standing on a heap of them.
        ///
        /// The penalty that remains is the teleport, the five seconds, and the whole trip to
        /// make again.
        /// </summary>
        private void ApplyTagPenalty(CharacterMotor victim)
        {
            victim.ApplyStagger(Balance.TagStunTime);

            // The tag is the taya's moment: the hit itself, the victim going down, and the
            // announcer, all off the one resolution so they cannot disagree.
            // ⚠️ THE FREEZE IS THE HIT'S WEIGHT. Without it a tag is instant and reads as the
            // victim teleporting rather than as being caught.
            Hitstop.Trigger();

            GameServices.Audio?.PlayAt("tag", victim.transform.position);
            GameServices.Audio?.PlayAt("downed", victim.transform.position);
            GameServices.Voice?.OnAttackerTagged();
            victim.Stamina.RefillAndClearFatigue();
            victim.Teleport(SafeZonePointFor(victim));
        }

        private static Vector3 SafeZonePointFor(CharacterMotor victim)
        {
            // Straight out along whichever axis they are furthest along, onto the spawn ring.
            Vector3 p = victim.transform.position;
            float ring = Confinement.AttackerSpawnRing();

            Vector3 dir = new Vector3(p.x, 0.0f, p.z);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            return new Vector3(dir.x * ring, p.y, dir.z * ring);
        }
    }
}
