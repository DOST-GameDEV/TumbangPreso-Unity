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
        /// ⚠️ AND IT STARTS AT THE ROUND LENGTH THIS MATCH IS SET TO, NOT AT THE SHIPPED 90. The
        /// paragraph above is about the ready-up window reading a full clock rather than 00:00;
        /// on a custom 120 second match the same argument says it must read **02:00** there, and
        /// `Balance.RoundTime` would have drawn 01:30 for the first thing a player sees.
        public float TimeLeft { get; private set; } = RoundLength;

        /// <summary>
        /// How long a round of THIS match lasts.
        ///
        /// WARNING  `Balance.RoundTime` IS STILL THE SHIPPED NUMBER AND IS STILL WHAT
        /// `Design.md` GOVERNS. `CustomGameRules.Defaults` reads it, so a rule set nobody has
        /// edited answers exactly 90 here and every number in `Design.md` stays true of the
        /// shipped game. `CLAUDE.md` section 5's rule is about the value the game SHIPS at; a
        /// custom lobby is explicitly not that, and `CustomGameRules.CanBeRanked` refuses the
        /// ladder to any match that has moved it.
        ///
        /// WARNING  FOUR PLACES USED TO WRITE `Balance.RoundTime` DIRECTLY and all four read
        /// this now, because a round that STARTS at 120 and is CLAMPED at 90 by the snapshot is
        /// worse than either number on its own: the host and the client would disagree about the
        /// clock and it would read as a desync.
        /// </summary>
        private static float RoundLength => UI.SceneFlow.SelectedRoundSeconds;

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
        public void ApplySnapshot(float timeLeft, bool roundActive, int defenderSlot,
                                  bool matchInProgress = true)
        {
            // WARNING  THE CEILING IS THE ROUND LENGTH THIS MATCH IS ACTUALLY BEING PLAYED AT,
            // NOT THE SHIPPED 90. `Balance.RoundTime` is what the game ships and what
            // `Design.md` governs; a custom lobby may set 30 to 180
            // (`CustomGameRules.MinRoundSeconds` and `MaxRoundSeconds`). **This clamp is applied
            // to a number the HOST sent**, so a 120 second round would have arrived correct and
            // been cut to 90 on every client: the clock would read 01:30 while the host counted
            // 02:00, and it would look like a desync rather than like a clamp.
            TimeLeft = Mathf.Clamp(timeLeft, 0.0f, RoundLength);
            RoundActive = roundActive;

            // ⚠️⚠️ THE FREE-ROAM WINDOW IS WHY THIS IS NOT SIMPLY `= roundActive`, AND STAMPING
            // IT WAS WHY A CLIENT COULD NOT MOVE AT ALL.
            //
            // `CharacterMotor.RoundActive` DEFAULTS TO TRUE and nothing writes it until
            // `BeginRound` or `EndRound`. That default is what makes the pre-round window work:
            // the director says the round is not active (correctly, nothing scores yet) while the
            // four bodies say they may act, so everybody can walk around the arena they are about
            // to play in. `CharacterMotor` gates steering on `CanAct()`, which is
            // `RoundActive && !IsStunned`, so a body with the flag off cannot move a centimetre.
            //
            // A CLIENT replicates this snapshot at 5 Hz, and it was stamping the DIRECTOR's false
            // onto all four bodies before the first round had ever begun. The host's bodies kept
            // the default true. **So the host walked around the free-roam window and every client
            // stood frozen**, with the camera still turning because that is local and ungated.
            // 🧑 2026-08-27: *"host can move but everyone else is stuck even bots"* and *"i can
            // move camera and see updates but i cant move"*, both with
            // "Practice freely, scores are paused" still on screen.
            //
            // ⚠️⚠️ AND `[NetSeat]` SAID THE WIRING WAS FINE, WHICH IS WHAT SENT THIS LOOKING HERE.
            // `reader=True simulated=True` ruled out §§ 53.1 and 60.1 outright: the keyboard was
            // on the right body and the motor was simulating it. The gate was somewhere else.
            //
            // ⚠️ SO IT IS ONLY STAMPED ONCE A MATCH IS ACTUALLY RUNNING, which is exactly when the
            // host writes it too. The four states all agree now: before the match, both sides
            // leave the default true and everybody can free-roam; in a round, both are true;
            // during an intermission `EndRound` set the host's bodies false and this sets the
            // client's; after the match, `MatchEnded` reaches `EndRound` on both (§ 57.1).
            // ⚠️⚠️ AND SINCE 2026-09-03 IT ASKS `LastTsinelasDirector` FIRST, BECAUSE THIS LOOP
            // SILENTLY UNDID THAT WHOLE FORMAT ON EVERY CLIENT. `docs/TODO.md` § 130.13.
            //
            // A Last Tsinelas attacker who has lost their last tsinelas is out for the rest of
            // the round, and "out" is `RoundActive = false` on their body. **This line runs at
            // 5 Hz with `roundActive` true for the whole round**, so it put the flag straight
            // back up within 200 ms and the eliminated player carried on throwing, grabbing and
            // charging resets while the host ignored every request. The host was immune, because
            // `MatchRpc.HostSyncPeer` hands it its own snapshot and nothing else writes here.
            //
            // ⚠️ IT WAS FOUND BY READING THIS METHOD RATHER THAN BY PLAYING, AND IT WOULD NOT
            // HAVE SHOWN UP IN ANY TEST WE HAVE: the elimination and the re-enable are in two
            // different files, both correct on their own, and the only thing that puts them
            // together is a client in a live round.
            //
            // ⚠️ THE DIRECTOR IS ASKED RATHER THAN THE FLAG BEING PROTECTED, so there is exactly
            // one answer to "is this seat out" and it is the same one the HUD draws.
            if (matchInProgress)
            {
                var tsinelas = GameServices.Tsinelas;

                foreach (var player in _players)
                {
                    if (player == null) continue;

                    // ⚠️ ONLY EVER TO HOLD ONE DOWN, NEVER TO RAISE ONE. When the round is over
                    // `roundActive` is false and everybody stops, out or not.
                    if (roundActive && tsinelas != null && tsinelas.IsOut(player.PlayerSlot))
                    {
                        player.RoundActive = false;
                        continue;
                    }

                    player.RoundActive = roundActive;
                }
            }

            foreach (var player in _players)
            {
                if (player == null) continue;
                player.IsDefender = player.PlayerSlot == defenderSlot;
                player.GetComponentInChildren<Visual.CharacterNameplate>()?.Refresh();
            }
        }

        /// <summary>
        /// Restores the tournament clocks a reconnecting HUD reads. Scoring remains host-only;
        /// these values prevent a joiner from seeing a fresh warning meter while the host is
        /// already charging a penalty against that seat.
        /// </summary>
        public void ApplyNetworkTournamentState(float tayaCampSeconds, float[] attackerIdleSeconds)
        {
            _tayaCampTimer = Mathf.Max(0.0f, tayaCampSeconds);

            for (int slot = 0; slot < _attackerIdleTimer.Length; slot++)
            {
                float value = attackerIdleSeconds != null && slot < attackerIdleSeconds.Length
                    ? attackerIdleSeconds[slot]
                    : 0.0f;
                _attackerIdleTimer[slot] = Mathf.Max(0.0f, value);
            }
        }

        public void BeginRound()
        {
            RoundActive = true;
            TimeLeft = RoundLength;
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

            // ⚠️⚠️ THE WEATHER IS PUT BACK HERE, AND IT IS THE ONE PIECE OF AN ABILITY THAT CAN
            // OUTLIVE THE ROUND THAT CAST IT. `Visual.SkyEvent` writes `RenderSettings`, which is
            // scene-global: an ultimate cast in the last second of a round would otherwise still
            // be blending the street toward night while the scoreboard is up, and if anything
            // tore down the effect's own object in between, the map would stay dark with nothing
            // on screen to say why. The event restores from every exit it has; this is the one
            // that says WHEN, and it belongs to the rules rather than to the effect because the
            // rules are what decide a round is over.
            Visual.SkyEvent.StopAll();
        }

        public void ResetForNewMatch()
        {
            _players.Clear();

            RoundActive = false;
            TimeLeft = RoundLength;
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

            // Guided training is a practice range, not a scored round. The rules and every
            // verb stay live, including throw restoration protection and real ability
            // cooldowns, but the lesson must not end halfway through because 90 seconds passed.
            if (GameLaunch.GuidedTutorial)
            {
                TimeLeft = RoundLength;
                return;
            }

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

            // ⚠️ AN EMPTY CHAIR EARNS NOTHING. With the practice lobby set to NONE the taya seat
            // may not exist at all, and paying +10/s into a slot with no body behind it puts a
            // ghost at the top of the scoreboard for the whole round. The prize is for KEEPING
            // the can standing, and nobody is keeping it.
            if (PlayerAt(GameServices.Match.DefenderSlot) == null) return;

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

                            // ⚠️ NO WORD, FOR THE REASON SPELLED OUT ON THE SLIPPER PENALTY
                            // BELOW. Same shape exactly: the lata card already reads `LEAVE CAN
                            // RING 1.4s` and then `CAMPING · DEFENSE SCORE PAUSED` for as long as
                            // the taya stays, so a callout once a second repeats a line that is
                            // already on screen and cannot say anything new.
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

                            // ⚠️⚠️ NO WORD. THE OBJECTIVE CARD ALREADY SAYS IT, PERMANENTLY, AND
                            // THIS FIRED ONCE A SECOND ON TOP OF IT. 🧑 2026-08-27 with a
                            // screenshot of both at once: *"redundant as fuck -5a fetch slipper
                            // pls figure out which stay"*. Three surfaces were reporting one
                            // penalty every `TournamentPenaltyInterval`: this callout, a `-5
                            // SLIPPER IDLE` toast from `Hud.OnScored`, and the lata card's second
                            // line reading `FETCH SLIPPER · -5 / SECOND`.
                            //
                            // ⚠️ THE CARD LINE IS THE ONE THAT STAYS, AND IT IS NOT A TOSS-UP.
                            // It is the only one of the three that is a STATE rather than an
                            // event: it appears the moment the grace period lapses, says the rate
                            // rather than one tick of it, and goes away when the player picks the
                            // tsinelas up. A per-second flash cannot tell a player anything the
                            // second one did not, and it repeats for as long as the mistake does.
                            //
                            // ⚠️ THE HITMARKER STAYS BECAUSE IT IS NOT A WORD. It is the one
                            // non-text signal of the tick landing, which is what 🧑 asked to keep
                            // in the same breath as cutting the text: *"js remove some of the
                            // words that pop up"*.
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
        /// Whether an already-started throw wind-up may stay visually committed.
        ///
        /// The lata being down and the short restoration lock are transient release gates, not
        /// reasons to snap a charged arm back to idle. Starting still asks <see cref="CanThrow"/>
        /// and releasing still asks it again, so this cannot launch an illegal throw. It only
        /// keeps the animation and stored charge while the attacker holds the button.
        /// </summary>
        public bool CanMaintainThrowCharge(CharacterMotor who)
        {
            if (who == null) return false;

            var ctx = new ThrowContext
            {
                RoundActive = RoundActive,
                IsDefender = who.IsDefender,
                HoldingSlipper = who.HoldingSlipper,
                LataUpright = true,
                ThrowCooldownLeft = 0.0f,
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

            // ⚠️⚠️ EVERY PIECE OF THE TAG'S PRESENTATION USED TO BE WRITTEN HERE, INSIDE A
            // HOST-ONLY METHOD, AND SO HAPPENED ON ONE SCREEN. 🧑 2026-08-29: *"make sure that
            // all host sided shit is seen by everyone and not js host"*. The stars, the TAGGED!
            // popup, the hitstop, the burst, the victim's flash, both squashes, the camera punch
            // and the HULI! style award were nine lines of feedback for the taya's only scoring
            // verb, and three of the four people in the room got none of it.
            //
            // ⚠️ THE SHAKE IS THE CLEAREST CASE. `rig.IsFollowing(victim)` was already the right
            // test and it could only ever pass on the host, so a player who was tagged felt
            // nothing while the host got a jolt for somebody else's tag. Running the same code on
            // four machines is what makes that line mean what it says.
            //
            // ⚠️⚠️ AND THE RULES ARE STILL HOST-ONLY AND STAY IN THIS METHOD. The stagger above,
            // the stamina refill and the teleport below are decisions; `Visual.MatchFlair` draws
            // and touches no state. A client that could stun a body from a message is a client
            // that decides, which is `CLAUDE.md` § 4.
            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.Tag,
                                       taya.PlayerSlot, victim.PlayerSlot,
                                       victim.transform.position);

            // ⚠️⚠️ THROUGH `NetCue`, BECAUSE THE CALLER OPENS WITH `ShouldResolve()` AND THE TAG
            // IS THE TAYA'S ENTIRE PAYOFF. 🧑 2026-08-29: *"non hosts dont have sfx in some
            // plarts"*. `tools/audit_audio_reach.py` reported this line clean and it was not:
            // the audit looks for a gate at the SAME brace depth, and this method has none —
            // the gate is in the caller. Every sound reached by a host-resolved verb one call
            // deep has the same shape. See `docs/TODO.md` § 83.12.
            NetCue.PlayImpact("tag", "downed", victim.transform.position, 1.0f);

            // ⚠️ THE ANNOUNCER AND THE STYLE AWARD MOVED INTO `MatchFlair.PlayTag` WITH THE REST
            // OF THE PRESENTATION, so each peer speaks its own line off the event rather than
            // hearing the host's. `NetCue`'s header is explicit that a commentary track must not
            // be relayed as a world sound, and this is how that rule is kept while still reaching
            // everybody.
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
