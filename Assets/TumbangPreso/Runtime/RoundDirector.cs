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

        /// <summary>
        /// The taya finished the reset channel and the can is standing again.
        ///
        /// ⚠️ `round_manager.gd` DECLARES `lata_restored` AND THE HUD IS ITS ONLY LISTENER
        /// (`hud.gd:88`). The port had the state change and no signal, so nothing could be told
        /// about it: see `Hud.OnLataRestored` for what was missing on screen.
        /// </summary>
        public event Action LataRestored;

        public bool RoundActive { get; private set; }

        /// <summary>
        /// ⚠️⚠️ IT STARTS FULL, NOT AT ZERO, AND THAT WAS A REPORTED BUG. `round_manager.gd`
        /// declares `var time_left: float = ROUND_TIME`, so during the ready-up window — which
        /// is the first thing a player ever sees of a match — the Godot HUD reads **01:30**.
        /// Defaulting to 0 here drew **00:00** over a free-roam phase that had not started
        /// counting, which reads as a clock that has already expired and was reported as the
        /// match "starting with no time left".
        /// </summary>
        public float TimeLeft { get; private set; } = Balance.RoundTime;

        public Lata Lata { get; set; }

        private readonly List<CharacterMotor> _players = new List<CharacterMotor>();
        private float _throwCooldownLeft;
        private float _defenseTickAccum;
        private float _tayaCampTimer;
        private float _tayaCampTickAccum;
        private bool _tayaInsideCampZone;
        private readonly float[] _attackerIdleTimer = new float[Balance.PlayerCount];
        private readonly float[] _attackerIdleTickAccum = new float[Balance.PlayerCount];

        /// <summary>Shove credit, for the Sabotage score. slot -> (shover, at time).</summary>
        private readonly Dictionary<int, (int by, float at)> _shoveCredit =
            new Dictionary<int, (int, float)>();

        private float _clock;

        public IReadOnlyList<CharacterMotor> Players => _players;
        public float TayaCampSeconds => _tayaCampTimer;
        public bool IsTayaCampWarningActive => TournamentRules.IsCampWarning(_tayaCampTimer);
        public bool IsTayaCampPenaltyActive => TournamentRules.IsCampPenalty(_tayaCampTimer);

        public float AttackerIdleSeconds(int slot)
            => slot >= 0 && slot < _attackerIdleTimer.Length ? _attackerIdleTimer[slot] : 0.0f;

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

        /// <summary>
        /// Rehydrates the live round on a joining client without replaying BeginRound's
        /// teleports, score events, or item hand-outs. Roles are round state, not connection
        /// state: a player who disconnected as an attacker may be the current taya when they
        /// return.
        /// </summary>
        public void ApplySnapshot(float timeLeft, bool roundActive, int defenderSlot)
        {
            TimeLeft = Mathf.Clamp(timeLeft, 0.0f, Balance.RoundTime);
            RoundActive = roundActive;

            foreach (var player in _players)
            {
                if (player == null) continue;
                player.IsDefender = player.PlayerSlot == defenderSlot;
                player.RoundActive = roundActive;
                player.GetComponentInChildren<Visual.CharacterNameplate>()?.Refresh();
            }
        }

        public void BeginRound()
        {
            RoundActive = true;
            TimeLeft = Balance.RoundTime;
            _throwCooldownLeft = 0.0f;
            _defenseTickAccum = 0.0f;
            _tayaCampTimer = 0.0f;
            _tayaCampTickAccum = 0.0f;
            _tayaInsideCampZone = false;
            System.Array.Clear(_attackerIdleTimer, 0, _attackerIdleTimer.Length);
            System.Array.Clear(_attackerIdleTickAccum, 0, _attackerIdleTickAccum.Length);
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

        public void ResetForNewMatch()
        {
            _players.Clear();

            RoundActive = false;
            TimeLeft = Balance.RoundTime;
            _throwCooldownLeft = 0.0f;
            _defenseTickAccum = 0.0f;
            _tayaCampTimer = 0.0f;
            _tayaCampTickAccum = 0.0f;
            _tayaInsideCampZone = false;
            System.Array.Clear(_attackerIdleTimer, 0, _attackerIdleTimer.Length);
            System.Array.Clear(_attackerIdleTickAccum, 0, _attackerIdleTickAccum.Length);
            _shoveCredit.Clear();
            Lata = null;
        }

        private void FixedUpdate()
        {
            if (!RoundActive) return;

            float dt = Time.fixedDeltaTime;
            _clock += dt;
            TimeLeft -= dt;

            if (_throwCooldownLeft > 0.0f)
                _throwCooldownLeft = Mathf.Max(0.0f, _throwCooldownLeft - dt);

            StepTournamentPenalties(dt);
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

            // A camping taya must not earn +10 defence while paying only -5 for
            // camping. Once the grace period expires, can-ring income is suspended.
            if (IsTayaCampPenaltyActive)
            {
                _defenseTickAccum = 0.0f;
                return;
            }

            _defenseTickAccum += dt;
            while (_defenseTickAccum >= Balance.DefenseTickInterval)
            {
                _defenseTickAccum -= Balance.DefenseTickInterval;
                GameServices.Match.AddScore(GameServices.Match.DefenderSlot, ScoreEvent.DefenseTick);
            }
        }

        private void StepTournamentPenalties(float dt)
        {
            if (GameServices.Match == null) return;

            // 1. TAYA CAN-CAMPING MONITOR
            int defenderSlot = GameServices.Match.DefenderSlot;
            var taya = PlayerAt(defenderSlot);

            // ⚠️⚠️ A UNIT THAT CANNOT ACT CANNOT STALL, AND CHARGING IT ANYWAY IS A SECOND
            // PUNISHMENT FOR BEING HIT. Both tournament clocks below exist to answer "is this
            // player refusing to play", and a stunned, staggered or frozen body is not refusing
            // anything: it is already paying the price the verb that hit it was for. Hero
            // Strike is where this stopped being theoretical, because its kits stun far more
            // often than Classic's do. Measured 2026-08-23 across a whole four round match:
            // 69 unretrieved-slipper penalties and 9 camping penalties in Hero Strike against
            // 0 and 0 in Classic, on bots making the same decisions in both. That difference
            // was the abilities, not the play, and -5 per second while frozen is a stun that
            // silently costs a round.
            bool tayaCanAct = taya != null && taya.CanAct();

            if (taya != null && tayaCanAct && Lata != null && Lata.IsUpright)
            {
                float distToCan = Vector3.Distance(new Vector3(taya.transform.position.x, 0, taya.transform.position.z),
                                                   new Vector3(Lata.transform.position.x, 0, Lata.transform.position.z));

                _tayaInsideCampZone = TournamentRules.IsTayaCamping(_tayaInsideCampZone, distToCan);
                _tayaCampTimer = TournamentRules.StepViolationTimer(
                    _tayaCampTimer, _tayaInsideCampZone, dt);

                if (_tayaInsideCampZone)
                {
                    if (TournamentRules.IsCampPenalty(_tayaCampTimer))
                    {
                        _tayaCampTickAccum += dt;
                        while (_tayaCampTickAccum >= Balance.TournamentPenaltyInterval)
                        {
                            _tayaCampTickAccum -= Balance.TournamentPenaltyInterval;
                            GameServices.Match.AddScore(defenderSlot, ScoreEvent.TayaCampPenalty);
                            Visual.ComicPopup.Spawn(taya.transform.position + Vector3.up * 1.5f, "CAMPING! -5", UI.UiTheme.Defense, 1.0f);
                            UI.Hud.Instance?.PopHitmarker(UI.UiTheme.Defense, "⚠️");
                        }
                    }
                }
                else
                {
                    _tayaCampTickAccum = 0.0f;
                }
            }
            else if (taya != null && !tayaCanAct)
            {
                // Hold the clock rather than clearing it: a taya stunned ON the can has not
                // left it, and wiping the timer would make being hit a way to launder four
                // seconds of camping.
                _tayaCampTickAccum = 0.0f;
            }
            else
            {
                _tayaCampTimer = 0.0f;
                _tayaCampTickAccum = 0.0f;
                _tayaInsideCampZone = false;
            }

            // 2. UNRETRIEVED SLIPPER IDLE MONITOR
            var slippers = FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var p in _players)
            {
                if (p == null || p.IsDefender || p.HoldingSlipper)
                {
                    if (p != null && p.PlayerSlot >= 0 && p.PlayerSlot < _attackerIdleTimer.Length)
                    {
                        _attackerIdleTimer[p.PlayerSlot] = 0.0f;
                        _attackerIdleTickAccum[p.PlayerSlot] = 0.0f;
                    }
                    continue;
                }

                int slot = p.PlayerSlot;
                if (slot < 0 || slot >= _attackerIdleTimer.Length) continue;

                // Check if this attacker's slipper is loose on the ground
                bool hasLooseSlipper = false;
                foreach (var s in slippers)
                {
                    if (s != null && s.OwnerSlot == slot && s.State == SlipperState.Loose)
                    {
                        hasLooseSlipper = true;
                        break;
                    }
                }

                // The anti-stall clock follows the unresolved objective, not the chalk
                // line. Otherwise an empty-handed attacker can idle one step inside the
                // danger box, be untargetable, and avoid the tournament rule forever.
                if (hasLooseSlipper)
                {
                    // Same rule as the camp clock above: the timer HOLDS while the attacker is
                    // stunned rather than advancing, so a chain of hero crowd control cannot
                    // post the penalty on somebody who was never given a chance to run.
                    if (!p.CanAct())
                    {
                        _attackerIdleTickAccum[slot] = 0.0f;
                        continue;
                    }

                    _attackerIdleTimer[slot] = TournamentRules.StepViolationTimer(
                        _attackerIdleTimer[slot], true, dt);
                    if (TournamentRules.IsSlipperPenalty(_attackerIdleTimer[slot]))
                    {
                        _attackerIdleTickAccum[slot] += dt;
                        while (_attackerIdleTickAccum[slot] >= Balance.TournamentPenaltyInterval)
                        {
                            _attackerIdleTickAccum[slot] -= Balance.TournamentPenaltyInterval;
                            GameServices.Match.AddScore(slot, ScoreEvent.UnretrievedSlipperPenalty);
                            Visual.ComicPopup.Spawn(p.transform.position + Vector3.up * 1.5f, "FETCH SLIPPER! -5", UI.UiTheme.Offense, 1.0f);
                            UI.Hud.Instance?.PopHitmarker(UI.UiTheme.Offense, "⚠️");
                        }
                    }
                }
                else
                {
                    _attackerIdleTimer[slot] = 0.0f;
                    _attackerIdleTickAccum[slot] = 0.0f;
                }
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
        public void NotifyLataRestored()
        {
            _throwCooldownLeft = Balance.ThrowRestoreCooldown;
            LataRestored?.Invoke();
        }

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
            taya.AbilitySystem?.OnTagScored();

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

            ApplyTagPenalty(taya, victim);
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
        private void ApplyTagPenalty(CharacterMotor taya, CharacterMotor victim)
        {
            victim.ApplyStagger(Balance.TagStunTime);
            Visual.DizzyStars.Attach(victim.transform, Balance.TagStunTime, UI.UiTheme.Defense);
            Visual.ComicPopup.Spawn(victim.transform.position, "TAGGED!", UI.UiTheme.Defense, 1.4f);

            // The tag is the taya's moment: the hit itself, the victim going down, and the
            // announcer, all off the one resolution so they cannot disagree.
            // ⚠️ THE FREEZE IS THE HIT'S WEIGHT. Without it a tag is instant and reads as the
            // victim teleporting rather than as being caught.
            Hitstop.Trigger();

            // ⚠️ A VERB WITH NO FEEDBACK IS A VERB THE PLAYER CANNOT TELL THEY PERFORMED.
            // The burst is what makes a tag land visibly on the victim rather than merely
            // changing a number on the scoreboard.
            Visual.ImpactBurst.SpawnAt(victim.transform.position);

            // ⚠️ THE FLASH WAS BUILT AND NEVER CALLED. It is the read on the BODY, where the
            // burst is the read in the air: without it a tagged player sees particles beside
            // someone who looks untouched.
            victim.GetComponentInChildren<Visual.CharacterVisual>()?.FlashHit();
            Vector3 hitDirection = victim.transform.position - taya.transform.position;
            victim.GetComponentInChildren<Visual.CharacterSquashStretch>()?
                .Impact(hitDirection, 0.30f);
            taya.GetComponentInChildren<Visual.CharacterSquashStretch>()?
                .DashStretch(taya.transform.forward, 0.18f);

            // ⚠️ THE SHAKE GOES TO THE VICTIM'S OWN CAMERA AND NOWHERE ELSE. Shaking every
            // rig would make one player's tag jolt three other screens.
            var rig = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;

            if (rig != null && rig.IsFollowing(victim))
            {
                Vector3 impact = victim.transform.position - taya.transform.position;
                rig.ImpactPunch(impact.sqrMagnitude > 0.01f ? impact.normalized : Vector3.back, 1.0f);
            }

            GameServices.Audio?.PlayImpact("tag", "downed", victim.transform.position, 1.0f);
            GameServices.Voice?.OnAttackerTagged();
            UI.Hud.ReportStyle(taya.PlayerSlot, 36.0f, "HULI!");
            victim.Stamina.RefillAndClearFatigue();
            victim.Teleport(SafeZonePointFor(victim));
        }

        /// <summary>
        /// Where a tagged attacker is sent: THEIR OWN SPAWN MARK.
        ///
        /// ⚠️⚠️ THIS COMPUTED A RING POINT AND THE RING POINT IS INSIDE THE SCENERY. 🧑 on this
        /// build: *"i also get teleported in weird places (inside house), everyones supposed to
        /// be teleported at the spawn"*. The old body took the victim's bearing from the origin
        /// and pushed them out to `Confinement.AttackerSpawnRing()` along it — a number derived
        /// from the box geometry, which knows nothing about what the map has BUILT at that
        /// bearing. Both arenas are dressed streets: at most bearings the ring lands in a house,
        /// a sari-sari stall or a parked jeep, and it kept the victim's CURRENT y, so it did not
        /// even land on the floor it was aiming at.
        ///
        /// `character_base.gd:1509` hands `spawn_position` to the penalty and nothing else, and
        /// that mark was placed by the map author and floor-probed at seating time
        /// (`MatchHost.SeatOnFloor`), so it is known-good ground by construction. There is no
        /// geometry query to get right here, which is the point: the safe spot is not computed,
        /// it is remembered.
        ///
        /// ⚠️ THE FALLBACK IS THE OLD RING, NOT THE ORIGIN. A seat that somehow never went
        /// through `SeatOnFloor` has a zero mark, and zero is the lata's own mark — teleporting
        /// a tagged attacker on top of the can is worse than the ring ever was.
        /// </summary>
        private static Vector3 SafeZonePointFor(CharacterMotor victim)
        {
            Vector3 spawn = victim.SpawnPosition;
            if (spawn.sqrMagnitude > 0.01f) return spawn;

            Vector3 p = victim.transform.position;
            float ring = Confinement.AttackerSpawnRing();

            Vector3 dir = new Vector3(p.x, 0.0f, p.z);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            return new Vector3(dir.x * ring, p.y, dir.z * ring);
        }
    }
}
