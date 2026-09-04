using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// What kind of moment the director has noticed, in the order it outranks other moments.
    ///
    /// ⚠️⚠️ THE ORDER IS THE PRIORITY AND IT IS `docs/VISION.md` § 0 RATHER THAN A RANKING OF HOW
    /// MUCH THINGS MOVE. *"The tension is the retrieval, not the throw. Throwing is safe and free;
    /// going back in for your tsinelas is the only moment you can be caught."* So a retrieval with
    /// the taya closing is the top of this list, and a throw being charged is second from the
    /// bottom, above only an empty arena.
    ///
    /// ⚠️⚠️ AND IT IS AN ENUM RATHER THAN A FLOAT SCORE, WHICH IS THE WHOLE CHANGE FROM WHAT THIS
    /// REPLACED. `SpectatorDirector.ScoreSubject` summed six continuous terms over four bodies
    /// every frame, so the leader changed several times a second and the only thing stopping the
    /// camera following it was a 2.4 s hold on the SUBJECT. A hold on a person is not a hold on a
    /// play: a retrieval that runs four seconds could be cut away from at 2.4 because somebody
    /// else briefly scored higher. **An ordered list of named beats can be committed to.**
    ///
    /// ⚠️ LOWER VALUES OUTRANK HIGHER ONES, so the enum reads top to bottom in priority order and
    /// a comparison is `<`. That is backwards from a score and deliberate: a new beat inserted in
    /// the middle should be a one-line edit that visibly changes the ranking, not a number
    /// somebody has to balance against five others.
    /// </summary>
    public enum SpectatorBeat
    {
        /// <summary>An armed attacker inside the chalk with the taya closing on them.</summary>
        Retrieval = 0,

        /// <summary>An ultimate winding up or running.</summary>
        Ultimate = 1,

        /// <summary>The can has just been hit or knocked over.</summary>
        LataHit = 2,

        /// <summary>A lunge charging, or a tag that has just landed.</summary>
        Tag = 3,

        /// <summary>A thrown tsinelas has just come to rest near the can.</summary>
        SlipperLanded = 4,

        /// <summary>Somebody is on the floor.</summary>
        Downed = 5,

        /// <summary>The taya is channelling the can back onto its mark.</summary>
        Reset = 6,

        /// <summary>An attacker is charging a throw.</summary>
        ThrowPrep = 7,

        /// <summary>Nothing is happening. Establish the arena.</summary>
        Quiet = 8,
    }

    /// <summary>
    /// How a shot is composed. Nine genuinely different camera solves.
    ///
    /// ⚠️⚠️ THIS ENUM IS THE POINT OF THE WHOLE PASS. The director this replaces had **one**
    /// solve: an orbit bearing around a focus point at a distance driven by the spread between
    /// two things. A retrieval, an ultimate, a knockdown and a quiet beat were that same solve at
    /// a different radius, which is why it read as automatic rather than directed. The brief said
    /// so in as many words: *"do not implement every shot as the same follow camera at different
    /// distances."*
    ///
    /// ⚠️ EACH ONE IS A DIFFERENT ANSWER TO "WHERE DOES THE LENS GO", NOT A DIFFERENT DISTANCE.
    /// A two-shot stands off the axis between two people; a chase stands outside and behind the
    /// line of travel; an objective shot puts the can in the foreground and the thrower in depth;
    /// a hero shot is low and close on one body. Those are four different positions for the same
    /// four bodies, and no radius turns one into another.
    /// </summary>
    public enum ShotType
    {
        /// <summary>Retriever, taya and lata, from off the axis between them.</summary>
        RetrievalTwoShot,

        /// <summary>The lata large in the foreground, the thrower small in depth behind it.</summary>
        Objective,

        /// <summary>Behind and outside the chase line, both parties in frame.</summary>
        Chase,

        /// <summary>High and wide: the whole footprint and everyone standing in it.</summary>
        UltimateWide,

        /// <summary>Low three-quarter, the caster dominant in frame.</summary>
        UltimateHero,

        /// <summary>The taya and the can, from outside the box looking in.</summary>
        Defender,

        /// <summary>The body on the floor and whatever is about to reach it.</summary>
        Recovery,

        /// <summary>High, wide, slow. The arena as a place.</summary>
        QuietEstablish,

        /// <summary>
        /// Over the shoulder of somebody whose hands and target are both readable.
        ///
        /// ⚠️⚠️ DECLARED AND DELIBERATELY NOT EMITTED BY THE AUTOPILOT, AND THAT IS AN HONEST
        /// ANSWER RATHER THAN AN UNFINISHED ONE. The brief's condition is *"POV only when the
        /// action and hands remain readable"*, and this game's first-person view is the one shot
        /// where that cannot be guaranteed from outside: `SpectatorCamera.StepPovArms` borrows
        /// the watched player's viewmodel arms, the yaw is TAKEN from their body, and a player
        /// who whips round mid-retrieval produces exactly the *"fast or nauseating rotation"*
        /// this pass exists to remove. **A director that cannot tell in advance whether a shot
        /// will be readable must not choose it.**
        ///
        /// ⚠️ A HUMAN OPERATOR STILL HAS IT, ON A KEY, WHERE IT HAS ALWAYS BEEN
        /// (`SpectatorCamera`'s `_povToggle`). This is about what the AUTOPILOT picks, not about
        /// what the mode offers. `docs/TODO.md` § 134.5 records the decision so the next session
        /// does not read the gap as an oversight and wire it up.
        /// </summary>
        Pov,
    }

    /// <summary>
    /// One thing worth pointing a camera at, with everything the planner needs to frame it and to
    /// decide when to leave it.
    ///
    /// ⚠️⚠️ IT CARRIES A START TIME AND AN EXPECTED DURATION BECAUSE COMMITMENT IS THE FEATURE.
    /// The brief: *"stay on a retrieval until success, tag, abandonment, or timeout; stay on an
    /// ultimate through impact; stay on a lata hit through the can falling; do not leave during
    /// the outcome frame."* None of that is expressible about a score, because a score has no
    /// beginning and no end. It is all expressible about an event.
    ///
    /// ⚠️ `Reason` IS DIAGNOSTIC AND IS NOT DRAWN ANYWHERE BY DEFAULT. `SpectatorCamera`'s status
    /// line shows `ShotName()`, which is three or four words; this is the sentence a capture log
    /// prints so a failure in a recorded run can be traced to the decision that produced it.
    /// `docs/TODO.md` § 134.3 is a baseline written from reading the code, and the only way to
    /// keep it honest afterwards is for the code to say what it did.
    /// </summary>
    public readonly struct SpectatorInterest
    {
        public readonly SpectatorBeat Beat;

        /// <summary>Who the shot is about. Never null on a real interest.</summary>
        public readonly CharacterMotor Main;

        /// <summary>The other party: the taya, the caster's victim, the thrower. May be null.</summary>
        public readonly CharacterMotor Secondary;

        /// <summary>
        /// The objective this beat is about, in world space: the can, the impact point, the mark.
        ///
        /// ⚠️⚠️ IT IS A POINT AND NOT A `Transform`, so an interest survives the thing it names
        /// being destroyed. A slipper that has just landed near the can is a legitimate beat and
        /// is also an object that can be picked up two frames later; holding its transform would
        /// mean either a null check on every frame of the shot or a camera that jumps to the
        /// origin.
        /// </summary>
        public readonly Vector3 Objective;

        /// <summary>Whether <see cref="Objective"/> means anything for this beat.</summary>
        public readonly bool HasObjective;

        public readonly ShotType Shot;

        /// <summary>`Time.unscaledTime` when this beat began.</summary>
        public readonly float StartedAt;

        /// <summary>How long this kind of beat is expected to last, in seconds.</summary>
        public readonly float ExpectedSeconds;

        /// <summary>
        /// The shortest time this shot may be held before anything of the same or lower rank may
        /// take it, in seconds.
        ///
        /// ⚠️ IT IS PER-BEAT RATHER THAN ONE CONSTANT, which the old `MinShotSeconds` was. A can
        /// falling over is done in about a second and holding it for 2.4 is a shot of an empty
        /// street; an ultimate needs to be held through its impact, which for `SUPERNOVA` is a
        /// launch and a landing.
        /// </summary>
        public readonly float CommitSeconds;

        /// <summary>Why the director chose this. Written to the capture log, never to the screen.</summary>
        public readonly string Reason;

        public SpectatorInterest(SpectatorBeat beat, CharacterMotor main, CharacterMotor secondary,
                                 Vector3 objective, bool hasObjective, ShotType shot,
                                 float startedAt, float expectedSeconds, float commitSeconds,
                                 string reason)
        {
            Beat = beat;
            Main = main;
            Secondary = secondary;
            Objective = objective;
            HasObjective = hasObjective;
            Shot = shot;
            StartedAt = startedAt;
            ExpectedSeconds = expectedSeconds;
            CommitSeconds = commitSeconds;
            Reason = reason;
        }

        public bool Valid => Main != null;

        /// <summary>How long this beat has been running, in seconds.</summary>
        public float Age => Time.unscaledTime - StartedAt;

        /// <summary>True while the shot may not be taken by an equal or lower-ranked beat.</summary>
        public bool Committed => Age < CommitSeconds;

        /// <summary>True once the beat has outlived what it was expected to need.</summary>
        public bool Expired => Age > ExpectedSeconds;
    }

    /// <summary>
    /// Watches the match and answers "what is the most interesting thing happening right now".
    ///
    /// ⚠️⚠️ IT READS THE AUTHORITATIVE OBJECTS AND SUBSCRIBES TO THE EVENTS THE GAME ALREADY
    /// RAISES. The brief: *"use existing events from `MatchDirector`, `RoundDirector`, `Lata`,
    /// `Carrier`, `HeroAbilitySystem` and `MatchStatsCollector`. Do not repeatedly search the
    /// entire scene when an event already exists."* So the can's knockdown arrives on
    /// `Lata.UprightChanged`, the tag on `RoundDirector.Tagged`, and the ultimate on
    /// `HeroAbilitySystem.UltimateStarted`, which is the same event the introduction card hangs
    /// the camera and the lower third can never disagree about who is casting.
    ///
    /// ⚠️ THE ONE THING WITH NO EVENT IS A SLIPPER COMING TO REST, and it is polled on a timer
    /// rather than every frame. `Slipper` raises nothing on landing, and `FindObjectsByType` is
    /// the only way to see four of them; `Hud.UpdatePickupPrompt` solves the identical problem
    /// with a 0.20 s scan and this matches it. **A camera that costs the match its frame rate is
    /// a worse camera than one that notices a landing a fifth of a second late.**
    ///
    /// ⚠️⚠️ IT WRITES NOTHING AND OWNS NOTHING. No gameplay state, no RPC, no `InputIntent`, no
    /// collider. `SpectatorCamera`'s header is explicit that a cinematic auto-cam may only ever
    /// write a POSE, and this is one level further out than that: it does not even write a pose,
    /// it answers a question.
    /// </summary>
    public sealed class SpectatorInterestModel
    {
        // -------------------------------------------------------------------
        // § HOW LONG EACH BEAT IS WORTH
        //
        // ⚠️ EVERY NUMBER BELOW IS AGAINST SOMETHING IN `Balance` OR AGAINST A BROADCAST
        // MINIMUM, AND SAYS WHICH. A duration picked by feel is a duration the next person
        // retunes by feel, and `CLAUDE.md` § 2.3: *"an entry that says '40% of the arena' beats
        // one that says 'too big'."*
        // -------------------------------------------------------------------

        /// <summary>
        /// The shortest a shot may be held, in seconds. Below this a viewer has not finished
        /// reading the frame.
        ///
        /// ⚠️ CARRIED OVER UNCHANGED FROM `SpectatorDirector.MinShotSeconds` WITH ITS REASONING:
        /// *"under about two seconds a viewer has not finished reading the frame before it
        /// changes, and a director who cuts faster than that is editing rather than covering."*
        /// </summary>
        public const float MinCommit = 2.4f;

        /// <summary>
        /// A retrieval's expected life, in seconds.
        ///
        /// ⚠️ THE CROSSING, MEASURED. `Confinement.ConfinementRadius` is 7.0 so the box is 14 m
        /// across, and an attacker moves at `Speed * AttackerSpeedScale` = 2.53 m/s: in and out
        /// is about 5.5 s at a walk and under 3 sprinting. This is the sprinting figure plus a
        /// beat, because a retrieval that has run longer than this is somebody hesitating on the
        /// chalk rather than making a run.
        /// </summary>
        public const float RetrievalSeconds = 4.2f;

        /// <summary>
        /// How long an ultimate is held for, in seconds.
        ///
        /// ⚠️⚠️ IT IS A FLOOR, NOT A FIXED LENGTH: the shot also holds while
        /// `HeroAbility.IsActive`, so `SUPERNOVA`'s launch and landing are one shot rather than a
        /// cut in the middle of Sean's flight. `HeroAbility.Windup` is 0.4 s before the blast, so
        /// this covers the wind-up, the impact and a beat of aftermath.
        /// </summary>
        public const float UltimateSeconds = 3.4f;

        /// <summary>How long a knocked can is held for, in seconds. Long enough for it to fall.</summary>
        public const float LataHitSeconds = 2.6f;

        /// <summary>A tag's outcome, in seconds. `Balance.TagStunTime` is 5.0; this is the moment.</summary>
        public const float TagSeconds = 2.8f;

        /// <summary>A landed tsinelas near the can, in seconds.</summary>
        public const float SlipperSeconds = 2.0f;

        /// <summary>Somebody on the floor, in seconds.</summary>
        public const float DownedSeconds = 2.6f;

        /// <summary>
        /// The reset channel, in seconds.
        ///
        /// ⚠️ `Combat.ResetChannelFor` IS 1.30 s ON PASIP AND 1.79 s ON BOYBEN, so this holds the
        /// longest can plus the stand-up. A shot that leaves mid-channel shows the taya starting
        /// something and never finishing it.
        /// </summary>
        public const float ResetSeconds = 2.4f;

        /// <summary>A charging throw, in seconds. `Balance.ChargeFullTime` is 2.5.</summary>
        public const float ThrowPrepSeconds = 2.5f;

        /// <summary>A quiet establishing shot, in seconds.</summary>
        public const float QuietSeconds = 5.0f;

        /// <summary>
        /// How near the taya has to be for a retrieval to be a CHASE rather than a walk, in metres.
        ///
        /// ⚠️⚠️ 9.0 m IS THE SAME NUMBER THE OLD SCORE USED AND IT IS KEPT DELIBERATELY.
        /// `ScoreSubject` faded proximity over `gap / 9.0f`, with the reasoning *"a taya six
        /// metres behind a retriever is a chase; one on the far side of the arena is two
        /// unrelated people."* That judgement was right; what was wrong was that it produced a
        /// continuous term rather than a yes.
        /// </summary>
        public const float ChaseGap = 9.0f;

        /// <summary>How near the can a landing counts as a beat, in metres.</summary>
        public const float NearLata = 4.0f;

        /// <summary>Seconds between slipper scans. See the class note.</summary>
        private const float SlipperScanInterval = 0.20f;

        // -------------------------------------------------------------------

        private SpectatorInterest _current;

        /// <summary>The beat the camera should be covering, or an invalid interest.</summary>
        public SpectatorInterest Current => _current;

        /// <summary>What the last decision was, for the capture log.</summary>
        public string LastDecision { get; private set; } = "starting up";

        // Latched one-shot events, with the moment they fired and who they were about.
        private float _lataHitAt = -99.0f;
        private CharacterMotor _lataHitBy;

        private float _tagAt = -99.0f;
        private CharacterMotor _tagDefender;
        private CharacterMotor _tagVictim;

        private float _slipperAt = -99.0f;
        private Vector3 _slipperPoint;

        private float _ultimateAt = -99.0f;
        private CharacterMotor _ultimateCaster;
        private Abilities.HeroAbility _ultimateAbility;

        private RoundDirector _hookedRound;
        private MatchDirector _hookedMatch;
        private Lata _hookedLata;

        private float _slipperScanAt = -99.0f;
        private Slipper[] _slippers;
        private readonly Dictionary<int, SlipperState> _lastSlipperState =
            new Dictionary<int, SlipperState>();

        /// <summary>
        /// Subscribes to whatever exists right now.
        ///
        /// ⚠️ RETRIED EVERY TICK BECAUSE THE DIRECTORS MAY NOT EXIST YET. `AIController.Subscribe`
        /// carries the same note for the same reason: it is a handful of null checks and it
        /// removes an ordering bug that would show up only as a camera that never noticed
        /// anything.
        /// </summary>
        public void Hook()
        {
            var round = GameServices.Round;
            if (round != _hookedRound)
            {
                if (_hookedRound != null) _hookedRound.Tagged -= OnTagged;
                if (round != null) round.Tagged += OnTagged;
                _hookedRound = round;
            }

            var match = GameServices.Match;
            if (match != _hookedMatch)
            {
                if (_hookedMatch != null) _hookedMatch.Scored -= OnScored;
                if (match != null) match.Scored += OnScored;
                _hookedMatch = match;
            }

            var lata = round != null ? round.Lata : null;
            if (lata != _hookedLata)
            {
                if (_hookedLata != null) _hookedLata.UprightChanged -= OnLataUpright;
                if (lata != null) lata.UprightChanged += OnLataUpright;
                _hookedLata = lata;
            }

            Abilities.HeroAbilitySystem.UltimateStarted -= OnUltimateStarted;
            Abilities.HeroAbilitySystem.UltimateStarted += OnUltimateStarted;
        }

        public void Unhook()
        {
            if (_hookedRound != null) _hookedRound.Tagged -= OnTagged;
            if (_hookedMatch != null) _hookedMatch.Scored -= OnScored;
            if (_hookedLata != null) _hookedLata.UprightChanged -= OnLataUpright;
            Abilities.HeroAbilitySystem.UltimateStarted -= OnUltimateStarted;

            _hookedRound = null;
            _hookedMatch = null;
            _hookedLata = null;
        }

        // -------------------------------------------------------------------
        // § THE EVENTS THE GAME ALREADY RAISES
        // -------------------------------------------------------------------

        private void OnLataUpright(bool upright)
        {
            if (upright) return;

            _lataHitAt = Time.unscaledTime;
            _lataHitBy = null;   // filled in by `OnScored` when the credit arrives
        }

        private void OnScored(int slot, ScoreEvent what)
        {
            var round = GameServices.Round;
            if (round == null) return;

            switch (what)
            {
                case ScoreEvent.LataKnocked:
                    _lataHitAt = Time.unscaledTime;
                    _lataHitBy = SeatOf(round, slot);
                    break;

                // ⚠️ THE TAG ALSO ARRIVES ON `RoundDirector.Tagged`, WHICH CARRIES BOTH SEATS.
                // This branch is the backstop for a tag scored through a path that does not raise
                // it, and it is deliberately not `break`-less: a duplicate latch is one shot, not
                // two, because the timestamp simply moves forward inside the same beat.
                case ScoreEvent.Tag:
                    _tagAt = Time.unscaledTime;
                    _tagDefender = SeatOf(round, slot);
                    break;
            }
        }

        private void OnTagged(int defenderSlot, int attackerSlot)
        {
            var round = GameServices.Round;
            if (round == null) return;

            _tagAt = Time.unscaledTime;
            _tagDefender = SeatOf(round, defenderSlot);
            _tagVictim = SeatOf(round, attackerSlot);
        }

        private void OnUltimateStarted(CharacterMotor caster, Abilities.HeroKit kit,
                                       Abilities.HeroAbility ultimate)
        {
            _ultimateAt = Time.unscaledTime;
            _ultimateCaster = caster;
            _ultimateAbility = ultimate;
        }

        // -------------------------------------------------------------------
        // § THE DECISION
        // -------------------------------------------------------------------

        /// <summary>
        /// Re-decides what the camera should be on, honouring the commitment of the shot in hand.
        ///
        /// ⚠️⚠️ THE COMMITMENT RULE IS THE WHOLE FUNCTION AND IT IS THREE LINES. A held beat is
        /// only displaced by something that **outranks** it, and only once it has had its
        /// `CommitSeconds`. Everything else in here is finding candidates. The brief's
        /// exceptions (occlusion, invalid framing, camera collision) are NOT handled here on
        /// purpose: those are properties of a POSE, not of a beat, and they are answered by
        /// `SpectatorDirector.ValidatePose` re-solving the same interest from a different bearing
        /// rather than by abandoning the play.
        /// </summary>
        public SpectatorInterest Decide()
        {
            Hook();

            var round = GameServices.Round;
            if (round == null)
            {
                LastDecision = "no round";
                return _current;
            }

            var candidate = FindBest(round);

            if (!_current.Valid)
            {
                _current = candidate;
                LastDecision = "first shot: " + candidate.Reason;
                return _current;
            }

            // ⚠️ THE HELD BEAT IS RE-VALIDATED BEFORE ANYTHING IS COMPARED TO IT. A retrieval
            // whose retriever has left the box is over whatever its clock says, and holding it
            // would be the "shot holds too long" failure from the baseline.
            bool stillTrue = StillTrue(_current, round);

            if (stillTrue && _current.Committed)
            {
                LastDecision = "committed to " + _current.Beat;
                return _current;
            }

            if (stillTrue && !_current.Expired && candidate.Beat >= _current.Beat)
            {
                LastDecision = "holding " + _current.Beat + " over " + candidate.Beat;
                return _current;
            }

            LastDecision = (stillTrue ? "upgraded to " : "dropped, now ") + candidate.Reason;
            _current = candidate;
            return _current;
        }

        /// <summary>
        /// Is the beat in hand still describing something that is happening?
        ///
        /// ⚠️⚠️ THIS IS WHERE "DO NOT LEAVE DURING THE OUTCOME FRAME" LIVES. A retrieval stays
        /// true while the retriever is taggable OR for a beat after they stop being: the frame in
        /// which they get out, or get caught, is the frame the viewer is watching for, and a
        /// condition that flips to false the instant it resolves cuts on exactly that frame.
        /// `OutcomeGrace` is that beat.
        /// </summary>
        private static bool StillTrue(SpectatorInterest interest, RoundDirector round)
        {
            if (!interest.Valid) return false;
            if (interest.Main == null) return false;

            switch (interest.Beat)
            {
                case SpectatorBeat.Retrieval:
                    return interest.Main.IsTaggable() || interest.Age < OutcomeGrace;

                case SpectatorBeat.Ultimate:
                {
                    var kit = interest.Main.AbilitySystem != null
                        ? interest.Main.AbilitySystem.Kit : null;
                    var ult = kit != null ? kit.Ultimate : null;

                    if (ult != null && (ult.IsWindingUp || ult.IsActive)) return true;
                    return interest.Age < interest.ExpectedSeconds;
                }

                case SpectatorBeat.Downed:
                    return interest.Main.IsStunned || interest.Main.IsTripped
                           || interest.Age < OutcomeGrace;

                case SpectatorBeat.Reset:
                    return ChannelOf(interest.Main) > 0.0f || interest.Age < OutcomeGrace;

                case SpectatorBeat.ThrowPrep:
                    return IsCharging(interest.Main) || interest.Age < OutcomeGrace;

                // The latched one-shots live exactly as long as they said they would.
                default:
                    return interest.Age < interest.ExpectedSeconds;
            }
        }

        /// <summary>
        /// How long a resolved beat is held after it stops being true, in seconds.
        ///
        /// ⚠️ THE OUTCOME IS THE SHOT. A retrieval that ends in a tag and a retrieval that ends
        /// with somebody sprinting clear are the two things a viewer is watching for, and both
        /// happen on the frame the condition goes false. Cutting there shows the run and hides
        /// the result, which is the last row of the baseline failure list in `docs/TODO.md`
        /// § 134.3.
        /// </summary>
        private const float OutcomeGrace = 1.15f;

        private SpectatorInterest FindBest(RoundDirector round)
        {
            float now = Time.unscaledTime;
            var taya = DefenderOf(round);

            // ---- 1. a retrieval with the taya closing ---------------------------------
            CharacterMotor bestRetriever = null;
            float bestGap = float.MaxValue;

            foreach (var unit in round.Players)
            {
                if (unit == null || !unit.RoundActive || unit == taya) continue;
                if (!unit.IsTaggable()) continue;

                float gap = taya != null
                    ? Flat(unit.transform.position, taya.transform.position)
                    : float.MaxValue;

                if (gap >= bestGap) continue;

                bestGap = gap;
                bestRetriever = unit;
            }

            if (bestRetriever != null)
            {
                // ⚠️ THE SHOT DEPENDS ON WHETHER IT IS A CHASE. A taya two metres behind is a
                // chase and wants the camera outside the line of travel; a taya across the arena
                // is a lone runner and wants the two-shot that keeps the can in frame, because
                // the can is what they are running for.
                bool chased = bestGap <= ChaseGap;

                return new SpectatorInterest(
                    SpectatorBeat.Retrieval, bestRetriever, taya,
                    LataPoint(round), round.Lata != null,
                    chased ? ShotType.Chase : ShotType.RetrievalTwoShot,
                    now, RetrievalSeconds, MinCommit,
                    chased
                        ? $"retrieval, taya {bestGap:0.0} m behind"
                        : $"retrieval, taya {bestGap:0.0} m away");
            }

            // ---- 2. an ultimate ---------------------------------------------------------
            var casting = LiveUltimate(round);
            if (casting != null)
            {
                // ⚠️⚠️ THE HERO SHOT AND THE WIDE SHOT ARE CHOSEN BY THE ULTIMATE'S FOOTPRINT,
                // NOT BY THE HERO. `HeroAbility.TelegraphRadius` is already the authored answer to
                // "how much floor does this cover", and `docs/VISION.md` § 2 rule 2 says an
                // ultimate *"may be big"*. A big one needs the wide; a tight one is a shot of one
                // person doing something, and a wide of that is a shot of an empty street.
                var kit = casting.AbilitySystem != null ? casting.AbilitySystem.Kit : null;
                var ult = kit != null ? kit.Ultimate : null;
                float footprint = ult != null && ult.HasTelegraph ? ult.TelegraphRadius : 0.0f;

                return new SpectatorInterest(
                    SpectatorBeat.Ultimate, casting, NearestOther(round, casting),
                    casting.transform.position, true,
                    footprint >= 3.0f ? ShotType.UltimateWide : ShotType.UltimateHero,
                    now, UltimateSeconds, MinCommit,
                    $"ultimate {(ult != null ? ult.Name : "?")}, footprint {footprint:0.0} m");
            }

            // ---- 3. the can ---------------------------------------------------------------
            if (now - _lataHitAt < LataHitSeconds && round.Lata != null)
            {
                var who = _lataHitBy != null ? _lataHitBy : NearestTo(round, LataPoint(round));

                return new SpectatorInterest(
                    SpectatorBeat.LataHit, who != null ? who : taya, taya,
                    LataPoint(round), true, ShotType.Objective,
                    _lataHitAt, LataHitSeconds, MinCommit * 0.75f,
                    "the can is going over");
            }

            // ---- 4. a tag, landed or winding up -------------------------------------------
            if (now - _tagAt < TagSeconds && _tagDefender != null)
            {
                return new SpectatorInterest(
                    SpectatorBeat.Tag, _tagVictim != null ? _tagVictim : _tagDefender,
                    _tagDefender, LataPoint(round), round.Lata != null,
                    ShotType.Recovery, _tagAt, TagSeconds, MinCommit,
                    "tag landed");
            }

            if (taya != null && LungeCharging(taya))
            {
                var quarry = NearestOther(round, taya);

                return new SpectatorInterest(
                    SpectatorBeat.Tag, taya, quarry, LataPoint(round), round.Lata != null,
                    ShotType.Chase, now, TagSeconds, MinCommit,
                    "lunge charging");
            }

            // ---- 5. a tsinelas that has just landed near the can ---------------------------
            ScanSlippers(now);

            if (now - _slipperAt < SlipperSeconds)
            {
                return new SpectatorInterest(
                    SpectatorBeat.SlipperLanded, taya != null ? taya : AnyPlayer(round), taya,
                    _slipperPoint, true, ShotType.Objective,
                    _slipperAt, SlipperSeconds, MinCommit * 0.75f,
                    "a tsinelas landed by the can");
            }

            // ---- 6. somebody on the floor --------------------------------------------------
            foreach (var unit in round.Players)
            {
                if (unit == null || !unit.RoundActive) continue;
                if (!unit.IsStunned && !unit.IsTripped) continue;

                return new SpectatorInterest(
                    SpectatorBeat.Downed, unit, NearestOther(round, unit),
                    LataPoint(round), round.Lata != null, ShotType.Recovery,
                    now, DownedSeconds, MinCommit,
                    unit.IsTripped ? "tripped" : "stunned");
            }

            // ---- 7. the reset channel -------------------------------------------------------
            if (taya != null && ChannelOf(taya) > 0.0f)
            {
                return new SpectatorInterest(
                    SpectatorBeat.Reset, taya, null, LataPoint(round), round.Lata != null,
                    ShotType.Defender, now, ResetSeconds, MinCommit,
                    "the taya is resetting the can");
            }

            // ---- 8. a throw being charged ----------------------------------------------------
            foreach (var unit in round.Players)
            {
                if (unit == null || !unit.RoundActive || unit == taya) continue;
                if (!IsCharging(unit)) continue;

                return new SpectatorInterest(
                    SpectatorBeat.ThrowPrep, unit, taya, LataPoint(round), round.Lata != null,
                    ShotType.Objective, now, ThrowPrepSeconds, MinCommit,
                    "charging a throw");
            }

            // ---- 9. nothing ------------------------------------------------------------------
            //
            // ⚠️ THE QUIET SHOT IS THE TAYA AND THE CAN, NOT AN EMPTY STREET. `ScoreSubject`'s own
            // note: *"a defender alone in shot is a person standing near a tin can"*, which is
            // true when something else is happening and is exactly the right picture when nothing
            // is. The can is the thing every plan in the game is about.
            return new SpectatorInterest(
                SpectatorBeat.Quiet, taya != null ? taya : AnyPlayer(round), null,
                LataPoint(round), round.Lata != null, ShotType.QuietEstablish,
                now, QuietSeconds, MinCommit,
                "quiet: establishing");
        }

        // -------------------------------------------------------------------

        private void ScanSlippers(float now)
        {
            if (now >= _slipperScanAt)
            {
                _slipperScanAt = now + SlipperScanInterval;
                _slippers = Object.FindObjectsByType<Slipper>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }

            if (_slippers == null) return;

            var round = GameServices.Round;
            var lata = round != null ? round.Lata : null;
            if (lata == null) return;

            foreach (var s in _slippers)
            {
                if (s == null) continue;

                // ⚠️⚠️ KEYED ON THE SEAT, NOT ON THE OBJECT. There is exactly one tsinelas per
                // player by construction (`Balance.PlayerCount` of them, `OwnerSlot` 0 to 3), so
                // the seat is a stable, meaningful key that survives a slipper being pooled or
                // respawned between rounds. It also avoids `Object.GetInstanceID`, which Unity
                // 6.5 marks obsolete in favour of `GetEntityId` and which `csc.rsp` therefore
                // turns into a hard error in this assembly.
                int id = s.OwnerSlot;
                if (id < 0) continue;

                var state = s.State;

                bool known = _lastSlipperState.TryGetValue(id, out var last);
                _lastSlipperState[id] = state;

                if (!known || last != SlipperState.InFlight || state != SlipperState.Loose)
                    continue;

                if (Flat(s.transform.position, lata.transform.position) > NearLata) continue;

                _slipperAt = now;
                _slipperPoint = s.transform.position;
                return;
            }
        }

        private static CharacterMotor LiveUltimate(RoundDirector round)
        {
            foreach (var unit in round.Players)
            {
                if (unit == null || !unit.RoundActive) continue;

                var kit = unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
                var ult = kit != null ? kit.Ultimate : null;

                if (ult != null && (ult.IsWindingUp || ult.IsActive)) return unit;
            }

            return null;
        }

        private static bool LungeCharging(CharacterMotor taya)
        {
            var verbs = taya.GetComponent<CombatVerbs>();
            return verbs != null && verbs.ObservedLungeCharge >= 0.0f;
        }

        private static float ChannelOf(CharacterMotor who)
        {
            var carrier = who.GetComponent<Carrier>();
            return carrier != null ? carrier.ChannelRatio : 0.0f;
        }

        private static bool IsCharging(CharacterMotor who)
        {
            var carrier = who.GetComponent<Carrier>();
            return carrier != null && carrier.IsCharging;
        }

        private static Vector3 LataPoint(RoundDirector round)
            => round.Lata != null ? round.Lata.transform.position : Vector3.zero;

        private static CharacterMotor SeatOf(RoundDirector round, int slot)
        {
            foreach (var p in round.Players)
                if (p != null && p.PlayerSlot == slot) return p;

            return null;
        }

        private static CharacterMotor DefenderOf(RoundDirector round)
        {
            foreach (var p in round.Players)
                if (p != null && p.IsDefender) return p;

            return null;
        }

        private static CharacterMotor AnyPlayer(RoundDirector round)
        {
            foreach (var p in round.Players)
                if (p != null && p.RoundActive) return p;

            return null;
        }

        private static CharacterMotor NearestOther(RoundDirector round, CharacterMotor from)
        {
            if (from == null) return null;

            CharacterMotor best = null;
            float bestGap = float.MaxValue;

            foreach (var p in round.Players)
            {
                if (p == null || p == from || !p.RoundActive) continue;

                float gap = Flat(p.transform.position, from.transform.position);
                if (gap >= bestGap) continue;

                bestGap = gap;
                best = p;
            }

            return best;
        }

        private static CharacterMotor NearestTo(RoundDirector round, Vector3 point)
        {
            CharacterMotor best = null;
            float bestGap = float.MaxValue;

            foreach (var p in round.Players)
            {
                if (p == null || !p.RoundActive) continue;

                float gap = Flat(p.transform.position, point);
                if (gap >= bestGap) continue;

                bestGap = gap;
                best = p;
            }

            return best;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0.0f;
            b.y = 0.0f;
            return Vector3.Distance(a, b);
        }
    }
}
