using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public enum SlipperState
    {
        Loose,      // on the ground, grabbable
        Held,       // in somebody's hand
        InFlight,   // thrown
    }

    public enum SlipperAffinity
    {
        Normal,
        FireExplosive,  // Sean Skill 2 (Ignition Cannon)
        ElectricZap,    // Zack Skill 2 (Overcharge Throw)
    }

    /// <summary>
    /// The tsinelas. Ammunition, and the thing the whole game is actually about.
    ///
    /// ⚠️ THE GAME'S THESIS IS THE RETRIEVAL, NOT THE THROW. Throwing is safe and free;
    /// getting your slipper back is what costs you, because an Attacker becomes taggable the
    /// moment they pick one up inside the box. Anything that makes retrieval cheaper is a
    /// change to the core loop, not a convenience.
    /// </summary>
    public sealed class Slipper : MonoBehaviour
    {
        [SerializeField] private int _skinIndex = -1;
        [SerializeField] private int _ownerSlot = -1;

        public int SkinIndex { get => _skinIndex; set => _skinIndex = value; }
        public SlipperAffinity Affinity { get; set; } = SlipperAffinity.Normal;
        private GameObject _affinityVfxGo;


        /// <summary>
        /// § THE OWNERSHIP LOCK. An OWNED tsinelas may only be picked up by the seat that owns
        /// it; an UNOWNED one is free to anybody who may act.
        ///
        /// ⚠️⚠️ THIS RULE HAS NOW BEEN CALLED THREE TIMES AND ALL THREE ARE KEPT HERE, BECAUSE
        /// NONE OF THEM WAS A MISTAKE BEING CORRECTED. They are three calls on one trade-off and
        /// whoever reads this next should see that it was weighed rather than drifted.
        ///
        ///  1. **2026-08-01 morning, LOCKED.** *"Each slipper is uniquely color-coded and tied
        ///     strictly to its owner. Opponents cannot pick up or tamper with another player's
        ///     slipper."*
        ///  2. **2026-08-01 evening, OPEN.** *"allow bots and humans to pick up the slippers of
        ///     others"*, then *"let ai grab other slippers too but make it so that they dont
        ///     perma take from me, they can take from me tho but not all the time"*. The
        ///     argument was that a slipper you can LOSE to a rival is more contested than one
        ///     nobody may touch, and the converging-on-the-nearest failure the morning rule
        ///     feared was answered in the AI instead, by a claim rule plus a distance handicap
        ///     on a human's own shoe.
        ///  3. **2026-09-19, LOCKED AGAIN, and this is the shipped rule.** 🧑: *"we should
        ///     disable being able to take other people's tsinelas when you are attacking."*
        ///
        /// ⚠️⚠️ WHAT CALL 3 COSTS, STATED PLAINLY SO NOBODY REDISCOVERS IT AS A BUG: call 2's
        /// contest is gone. The three-way rivalry over a loose shoe was real and it is not being
        /// replaced by anything. What it buys is that "which one is mine" stops being decoration
        /// and becomes the rule: the owner glow, the recall mark and the landed rim all answer a
        /// question the grab now actually enforces, and an attacker can no longer be deleted
        /// from a round by a rival walking off with their ammunition.
        ///
        /// ⚠️⚠️ AND THE AI'S TWO SOFTENERS FOR CALL 2 ARE DEAD RATHER THAN DELETED.
        /// `AIController.ChooseSlipper`, `IsNearestClaimant` and `HumanSlipperBias` exist only
        /// to stop bots converging on, and repeatedly stealing, a human's shoe. **They were
        /// already unreachable before this change** (the live fetch path is `MySlipper`, which
        /// has always been owner-scoped), so this rule did not strand them and removing them is
        /// not part of it. `docs/TODO.md` § 154 carries that as its own item.
        ///
        /// ⚠️⚠️ AN UNOWNED TSINELAS IS STILL FREE AND THAT IS NOT A LOOPHOLE, IT IS THE PRACTICE
        /// LOBBY. `SliceRunner.EquipOwnedSlippers` sets `OwnerSlot = -1` in two cases that look
        /// identical through this field and are opposite in intent: the taya's shoe, which is
        /// **deactivated** and therefore refused on the first line of
        /// <see cref="IsGrabbableIgnoringReach"/> anyway, and an ABSENT seat's spare, which stays
        /// on the road on purpose. That file's own note: *"a seat that does not exist has simply
        /// left its slippers in the street, which is what an empty practice lobby is"*, and
        /// `SoloPracticeTests` fails outright if those stop being retrievable.
        ///
        /// `OwnerSlot` is still assigned at round start and is still what the recall mark and
        /// the owner glow read. It is no longer only a label.
        ///
        /// ⚠️ IT IS STILL NOT AN ADDRESS. <see cref="SeatOfOrigin"/> below is the identity and
        /// that argument is unchanged: this field is rewritten every round.
        /// </summary>
        public int OwnerSlot { get => _ownerSlot; set => _ownerSlot = value; }

        /// <summary>
        /// The seat this tsinelas was BUILT for, which never changes for the life of the match.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE `OwnerSlot` WAS BEING USED AS AN ADDRESS AND IT IS NOT ONE.
        /// `docs/TODO.md` § 78.1 is the measurement that found this on a two-process LAN run.
        /// `MatchRpc.FindSlipper` looked a slipper up BY `OwnerSlot`, and `OwnerSlot` is state
        /// the game rewrites every round: `SliceRunner.EquipOwnedSlippers` disowns the taya's
        /// shoe with `OwnerSlot = -1`, and `FindSlipper` skips anything negative. So the moment a
        /// seat became taya its tsinelas became **unaddressable on both peers at once** — the
        /// host stopped broadcasting it (`FindSlipper(defenderSlot)` is null, and
        /// `BroadcastSlipperStateIfChanged(null)` returns) and a client could not have applied it
        /// anyway (`SyncSlipperClientRpc` opens with `FindSlipper(ownerSlot)` on a `-1`).
        ///
        /// The visible result was that **every non-host peer rendered the taya carrying a
        /// slipper for the whole round**, frozen in the state it held when they were last an
        /// attacker, because `Carrier` parents the shoe to the carry anchor on every peer and
        /// nothing ever told the client to let go. The host could not see it: on the host that
        /// object is correctly parked out of play. § 38's thesis, one object further in.
        ///
        /// ⚠️ **AN IDENTITY MUST NOT BE A PIECE OF MUTABLE STATE.** This is the identity;
        /// `OwnerSlot` goes back to being what its own note above says it is, a LABEL that the
        /// foot arrow and the owner glow read.
        ///
        /// ⚠️ IT IS NOT ON THE WIRE AND DOES NOT NEED TO BE. `MatchInstaller.BuildSlipper`
        /// assigns it by seat on every peer, so both ends already agree on it without being told,
        /// which is the same reason the taya role is derived rather than replicated
        /// (`VISION.md` § 4).
        /// </summary>
        public int SeatOfOrigin { get; set; } = -1;

        public SlipperState State { get; private set; } = SlipperState.Loose;

        /// <summary>
        /// The one writer of <see cref="State"/>.
        ///
        /// ⚠️⚠️ § THE LANDED HIGHLIGHT IS CLEARED BY LEAVING Loose, AND IT HAS TO BE DRIVEN
        /// FROM HERE. The rim answers "where did the one you just threw end up", so it has to
        /// go out the moment that stops being a live question. The obvious places to clear it
        /// are the grab and the throw, and those are two of the SEVERAL paths that move a
        /// slipper out of Loose rather than all of them: the round-start force-equip is a
        /// third. Hung off the state itself it cannot be stranded lit on a slipper somebody is
        /// already holding, whatever route put it in their hand.
        ///
        /// Turning it back ON is deliberately not symmetric and does not live here: only
        /// <see cref="Land"/> knows whether an arrival at Loose ended a flight or was a
        /// teleport home.
        /// </summary>
        private void SetState(SlipperState next)
        {
            // ⚠️⚠️ § THE RECALL BEAM IS DRIVEN FROM HERE AND THE RIM IS NOT, WHICH LOOKS
            // INCONSISTENT AND IS NOT. `SetLandedHighlight(false)` above returns on its first
            // line when the flag is already false, so it is not a reliable "the state moved"
            // signal: a tsinelas going from Held to Loose with no landed rim on it repaints
            // nothing at all. The beam stands on ANY loose tsinelas of yours, so it needs the
            // transition itself. `Visual.SlipperBeam` carries why the two differ.
            //
            // ⚠️ ON A REAL CHANGE ONLY. `ApplySnapshotState` calls this on every reliable state
            // packet including keepalives, and a repaint per packet would rebuild four materials
            // at the wire's cadence.
            bool changed = State != next;
            if (State == SlipperState.InFlight && next != SlipperState.InFlight) FinishChain(ThrowChainEnd.Miss);

            State = next;
            if (next != SlipperState.Loose) SetLandedHighlight(false);
            if (changed) RefreshBeam();
        }

        public CharacterMotor Holder { get; private set; }

        private Vector3 _velocity;

        /// <summary>
        /// Where this slipper is going, for anything that has to predict it.
        ///
        /// ⚠️ READ BY THE TAYA'S INTERCEPT, which walks this forward under gravity to find the
        /// part of the arc a body can actually stand in. Without it the AI could only ask
        /// "is anything in flight", which is not a point to run to.
        /// </summary>
        public Vector3 Velocity => _velocity;
        private float _flightTime;

        /// <summary>Time in the air since the throw, which a deflect deliberately does NOT
        /// reset. See <see cref="Balance.MaxAirborneTime"/>.</summary>
        private float _airborneTotal;
        private int _throwerSlot = -1;
        private float _throwerIgnoreLeft;
        // Contact episodes, not a timed immunity: separation re-arms this body.
        private int _bodyContacts;
        private MatchDirector _chainMatch;
        private long _chainThrow, _chainEpoch;
        private int _chainOwner, _launchCanSerial;
        private void FinishChain(ThrowChainEnd outcome)
        {
            if (_chainThrow == 0) return;
            int serial = GameServices.Round != null && GameServices.Round.Lata != null
                ? GameServices.Round.Lata.HostKnockdownSerial : _launchCanSerial;
            if (_chainMatch != null && _chainMatch == GameServices.Match)
                _chainMatch.FinishHostThrowChain(_chainEpoch, _chainOwner, _chainThrow, outcome,
                    serial, serial > _launchCanSerial);
            _chainThrow = 0; _chainMatch = null;
        }
        private int _bankCount;
        private float _closestCanFlat = float.PositiveInfinity;
        private bool _nearMissReported;

        public int BankCount => _bankCount;
        public bool HasScoringCredit => _throwerSlot >= 0;
        public int ThrowerSlot => _throwerSlot;

        public float FlightScale => Roster.SlipperFlightScale(_skinIndex);
        public float ThrowLock => ThrowRules.ThrowLockFor(_skinIndex);

        /// <summary>
        /// How a carried tsinelas is turned in the hand.
        ///
        /// ⚠️ CONVERTED FROM THE .gd's BAKED `CARRY_BASIS`, which is X=(0,0,1), Y=(0,1,0),
        /// Z=(-1,0,0) — a quarter turn about Y. Under the handedness flip that becomes
        /// Z=(1,0,0) forward with Y up, i.e. +90° about Y here. Without it the slipper lies
        /// across the palm sideways, which reads as a bug in the grab rather than in a
        /// rotation nobody applied.
        /// </summary>
        public static readonly Quaternion CarryRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

        /// <summary>
        /// Light the slipper that belongs to the local player.
        ///
        /// ⚠️ PER-PEER, DELIBERATELY NOT REPLICATED. "Yours" is a different slipper on every
        /// machine, so this is computed locally and never sent — a networked glow would light
        /// one slipper for everybody.
        /// </summary>
        public void SetOwnerGlow(bool mine)
        {
            if (_glowOn == mine) return;
            _glowOn = mine;
            RefreshHighlight();
        }

        /// <summary>
        /// § THE LANDED HIGHLIGHT. See <see cref="Balance.LandedRimStrength"/> for what it is
        /// and why it is not the owner glow.
        ///
        /// ⚠️ TURNING IT ON IS NOT SYMMETRIC WITH TURNING IT OFF, and only this call site can
        /// do it: only <see cref="Land"/> knows whether an arrival at Loose ended a flight or
        /// was a teleport. Clearing it is driven from the STATE instead, in
        /// <see cref="SetState"/>, so it cannot be stranded lit on a slipper somebody is
        /// already holding whatever route put it in their hand.
        /// </summary>
        private void SetLandedHighlight(bool on)
        {
            if (_landedHighlightOn == on) return;
            _landedHighlightOn = on;
            RefreshHighlight();
        }

        /// <summary>
        /// Whichever of the two rims wins right now, written to every renderer on this slipper.
        /// Called whenever any input to that decision moves: the landed flag, the owner flag,
        /// or the player's colour pick.
        ///
        /// ⚠️⚠️ THE LANDED HIGHLIGHT WINS, AND THE TIE IS NOT ARBITRARY. Your own slipper coming
        /// to rest after a throw is the single most likely moment you have actually lost track
        /// of it, which is precisely when the owner glow's "this one is yours" has the least to
        /// add: you already know it is yours, you threw it. The glow resumes on its own the
        /// moment the highlight clears, because both are recomputed from here.
        ///
        /// ⚠️⚠️ THROUGH A MaterialPropertyBlock, NEVER BY WRITING THE MATERIAL. `ToonSkin`
        /// CACHES its materials and hands the same instance to every slipper wearing that skin,
        /// so setting a colour on the material would light all four at once. A property block
        /// is per-renderer, which is the granularity this feature needs. The Godot original hit
        /// the same wall from the other side and had to duplicate the resource by hand.
        ///
        /// ⚠️ THE SETTING IS READ HERE RATHER THAN CACHED, so Off is honoured by every repaint
        /// including ones triggered by something else entirely, and there is no second copy of
        /// the player's choice to fall out of date.
        ///
        /// ⚠️ AND IT TINTS THE OUTLINE PASS, NOT ONLY THE RIM. This is the half that makes the
        /// feature look like the thing that was asked for: the reference is Valorant's enemy
        /// outline, where the silhouette is traced in the chosen colour and the body underneath
        /// keeps its own. Godot could not do this cheaply because a tsinelas there wears a
        /// StandardMaterial3D with no rim uniform at all, so it had to chain an inverted hull by
        /// hand and cut the emission right down to stop the slipper being repainted solid.
        /// `TumbangPreso/Toon` already carries BOTH the rim term and the inverted-hull outline
        /// pass, and `MatchInstaller.BuildSlipper` already puts it on every tsinelas, so here
        /// the correct version is the cheap one. Do not reintroduce the emission workaround.
        /// </summary>
        private void RefreshHighlight()
        {
            // ⚠️⚠️ § ONLY YOURS LIGHTS. 🧑 2026-09-19: *"other player's tsinelas does not get
            // highlighted now. only yours."*
            //
            // ⚠️⚠️ THE GATE IS `_glowOn`, WHICH IS ALREADY THE PER-PEER ANSWER TO "IS THIS MINE",
            // rather than a second read of `OwnerSlot` here. `MatchInstaller.BuildSlipper`,
            // `MatchInstaller.RebindLocalSeat` and `MatchRpc` all write it, on every peer, and
            // every one of them writes `slipper.OwnerSlot == seat`. A second derivation in this
            // file would be a third place to keep in step with a seat that moves on a rejoin,
            // and `docs/TODO.md` § 78.1 is what a stale answer to that question already cost
            // once: every non-host peer drew the taya carrying a shoe for a whole round.
            //
            // ⚠️⚠️ AND IT IS FAITHFUL TO WHAT THIS RIM IS FOR, WHICH IS WHY IT IS A GATE AND NOT
            // A SECOND COLOUR. The note below already says the landed highlight answers *"where
            // did the one YOU just threw end up"*: that question has never had a meaning on
            // somebody else's tsinelas, and under § THE OWNERSHIP LOCK a rival's shoe is not
            // ammunition this player can ever use, so lighting it was pointing at a thing the
            // grab is going to refuse.
            bool landed = _landedHighlightOn
                          && _glowOn
                          && Settings.SlipperHighlights.Enabled(
                                 Settings.SettingsStore.Current.SlipperHighlight);

            float rim;
            Color rimColour;
            Color outline;

            if (landed)
            {
                rim = Balance.LandedRimStrength;
                rimColour = Settings.SlipperHighlights.ColourOf(
                                Settings.SettingsStore.Current.SlipperHighlight);
                outline = rimColour;
            }
            else if (_glowOn)
            {
                rim = Balance.OwnerRimStrength;
                rimColour = OwnerRimColour;
                outline = Visual.ToonSkin.Ink;
            }
            else
            {
                rim = 0.0f;
                rimColour = OwnerRimColour;
                outline = Visual.ToonSkin.Ink;
            }

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                // ⚠️⚠️ AN EFFECT PARENTED TO THIS PROP IS NOT PART OF THIS PROP, AND THE RIM PASS
                // HAD NO WAY TO KNOW THAT UNTIL § THE RECALL BEAM ARRIVED. `VfxRenderTag`'s own
                // note makes this argument for `InputEdgeTests`' toon-outline sweep one level up:
                // the rule is *"is this part of the model"* rather than a skip list somebody has
                // to remember to extend. Writing rim properties into a beam's block is harmless
                // today, because its material carries none of them, and it is the shape of the
                // fault that put a landed-rim assertion on the wrong renderer.
                if (r.GetComponent<Visual.VfxRenderTag>() != null) continue;

                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);

                block.SetFloat(RimStrengthId, rim);
                block.SetColor(RimColorId, rimColour);
                block.SetColor(OutlineColorId, outline);

                r.SetPropertyBlock(block);
            }

            // Every input the beam reads has just been re-decided, so it follows from here as
            // well as from the state. See `RefreshBeam`.
            RefreshBeam();
        }

        /// <summary>
        /// § THE RECALL BEAM, decided. `Visual.SlipperBeam` draws it; this owns whether it draws
        /// at all and in what colour.
        ///
        /// ⚠️⚠️ THE THREE CLAUSES ARE THE SAME THREE THE REST OF THE PRESENTATION ALREADY ASKS,
        /// WHICH IS THE WHOLE REASON THE BEAM CANNOT ARGUE WITH ANYTHING. **Mine** is `_glowOn`,
        /// the per-peer flag `MatchInstaller` and `MatchRpc` maintain, not a second reading of
        /// `OwnerSlot`. **On** is the player's own `SlipperHighlight` setting, so Off silences the
        /// rim, the screen mark's colour source and this together rather than leaving one lit.
        /// **Loose** is the state, asked here rather than restated anywhere else.
        ///
        /// ⚠️ IT IS BROADER THAN THE LANDED RIM ON PURPOSE. The rim answers *"where did the one
        /// you just threw end up"* and is therefore cleared by a recovery that teleports the shoe
        /// home; the beam answers *"your tsinelas is lying over there"*, which is true however it
        /// got there. 🧑 asked for it *"when your tsinelas is in the ground"*.
        ///
        /// ⚠️ NOTHING IS BUILT UNTIL SOMETHING IS LIT. `SlipperBeam.Attach` is only reached on the
        /// frame a beam is actually wanted, so three of the four tsinelas in a round never carry
        /// one.
        /// </summary>
        private void RefreshBeam()
        {
            int pick = Settings.SettingsStore.Current.SlipperHighlight;

            bool lit = State == SlipperState.Loose
                       && _glowOn
                       && gameObject.activeInHierarchy
                       && Settings.SlipperHighlights.Enabled(pick);

            if (!lit)
            {
                if (_beam != null) _beam.Set(false, Color.white);
                return;
            }

            if (_beam == null) _beam = Visual.SlipperBeam.Attach(this);
            if (_beam != null) _beam.Set(true, Settings.SlipperHighlights.ColourOf(pick));
        }

        private Visual.SlipperBeam _beam;

        /// <summary>
        /// `slipper.gd::OWNER_RIM_COLOR`. Gold, and deliberately NOT the UI theme's highlight:
        /// it has to be a colour no entry in the landed palette can be confused with.
        /// </summary>
        private static readonly Color OwnerRimColour = new Color(1.0f, 0.86f, 0.35f, 1.0f);

        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        private bool _glowOn;
        private bool _landedHighlightOn;

        /// <summary>
        /// ⚠️ THE LIVE REPAINT, and it is why the setting has an event at all. The settings
        /// panel is reachable from the in-match pause menu, so a colour change has to reach
        /// slippers that are already lying on the ground.
        /// </summary>
        private void OnEnable() => Settings.SettingsStore.SlipperHighlightChanged += RefreshHighlight;

        /// <summary>
        /// ⚠️ A SLIPPER IS DESTROYED AT EVERY ROUND RESET and the event is static, so failing to
        /// detach here would keep every slipper the match ever built alive and write materials
        /// onto destroyed objects. Godot hit exactly this: five
        /// "Parameter 'material' is null" errors, one per surface, from a slipper freed a moment
        /// earlier while an autoload signal was still connected to it.
        /// </summary>
        private void OnDisable()
        {
            Settings.SettingsStore.SlipperHighlightChanged -= RefreshHighlight;
            FinishChain(ThrowChainEnd.Miss);
        }

        /// <summary>
        /// ⚠️ THE PREVIEW MUST CALL THIS SAME FUNCTION. The dotted aim arc and the real
        /// flight stay one line only while both integrate the velocity produced here. They
        /// were measured agreeing to 0.000 m on three of the four skins.
        ///
        /// ⚠️ AND THE THROW LEAVES FROM THE SIGHT LINE, NOT THE HAND. Measured: from the
        /// hand, the flight sags 0.38 to 0.43 m below the line the player is aiming along and
        /// peaks within 0.2 m of them, so the slipper drops out of the bottom of the screen
        /// the instant it is released. From the sight line it is 0.001 to 0.043 m. The path
        /// was always right; the starting height was not.
        /// </summary>
        public Vector3 LaunchVelocity(Vector3 aimDirection, float charge01)
        {
            float speed = ThrowRules.LaunchSpeedFor(_skinIndex, charge01);
            return aimDirection.normalized * speed;
        }

        /// <summary>
        /// The velocity that puts this slipper THROUGH a point, at the speed its charge buys.
        ///
        /// ⚠️⚠️ THE PORT THREW AT A FIXED 45 DEGREES AND THIS IS THE FUNCTION THAT WAS MISSING.
        /// `slipper.gd::_solve_arc` solves the launch ANGLE for the target, and its header notes
        /// that `trajectory_preview.gd` calls the same function precisely so the dotted line and
        /// the flight cannot drift apart. Unity had the "cannot drift apart" half — the preview
        /// integrates whatever `Carrier` hands it — around the wrong velocity.
        ///
        /// What a fixed 45 does to the game: the throw ignores where the player is aiming
        /// vertically and lobs at maximum-range angle whatever the distance, so a can eight
        /// metres away and a can two metres away get the same towering arc, and the aim line
        /// leaves the frame near-vertically instead of pointing at the mark. Reported against a
        /// cropped frame of exactly that: *"THIS charge outline is so ugly, it doesnt behave
        /// naturally"*.
        ///
        /// ⚠️ THE LOW ROOT, NOT THE HIGH ONE. `(v² - root)` is the flatter of the two solutions:
        /// less airtime, harder to body-block, and the one a person throwing a slipper at a can
        /// actually produces. The .gd drops the lob root for the same reason.
        ///
        /// ⚠️ AND OUT OF RANGE THROWS ALONG THE PLAYER'S OWN LINE rather than falling back to
        /// 45. The .gd is explicit about why: aiming at a distant wall would otherwise fire a
        /// lob straight up, which is a far stranger thing to have happen than a throw that
        /// visibly does not get there.
        /// </summary>
        public Vector3 LaunchVelocityTo(Vector3 origin, Vector3 target, float charge01)
        {
            float speed = ThrowRules.LaunchSpeedFor(_skinIndex, charge01);
            return SolveArc(origin, target, speed) * speed;
        }

        /// <summary>The unit launch direction from origin to target at a given speed.</summary>
        public static Vector3 SolveArc(Vector3 origin, Vector3 target, float speed)
        {
            Vector3 toTarget = target - origin;
            Vector3 flat = new Vector3(toTarget.x, 0.0f, toTarget.z);
            float distance = flat.magnitude;

            // Straight up, straight down, or on top of us: no arc to solve, throw along the
            // line. Also guards the division below.
            if (distance < 0.05f || speed < 0.01f)
                return toTarget.magnitude > 0.01f ? toTarget.normalized : Vector3.forward;

            float g = Balance.Gravity;
            float v2 = speed * speed;

            float discriminant = v2 * v2 - g * (g * distance * distance + 2.0f * toTarget.y * v2);

            if (discriminant < 0.0f) return toTarget.normalized;

            float root = Mathf.Sqrt(discriminant);
            float tangent = (v2 - root) / (g * distance);

            return (flat.normalized + Vector3.up * tangent).normalized;
        }

        /// <summary>
        /// The HELD skin's own resting origin height, measured off the mesh rather than assumed.
        ///
        /// ⚠️ THE FOURTH SKIN MISSES BY 0.263 m AND THE ARC IS NOT THE BUG. A crocs RESTS
        /// 0.161 m off the ground against the other three at 0.034 to 0.056, while a preview
        /// that stops at a fixed floor epsilon draws its line to the ground. The line is right;
        /// the tall skin simply stops higher.
        /// </summary>
        /// <summary>
        /// The vector from this object's ORIGIN to the middle of the shoe it actually draws, in
        /// world space at whatever rotation and scale it is wearing right now.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE THE ORIGIN IS NOT IN THE MIDDLE OF THE SHOE, AND THAT IS A
        /// REQUIREMENT RATHER THAN AN ACCIDENT. `docs/TODO.md` § 70.2 fixed every slipper mesh as
        /// **centred on XY and seated on Z = 0** — measured, *"every one has `min.y == 0.0000` in
        /// glTF space"* — so the authored origin sits on the SOLE, at one END of the shoe. Placing
        /// that origin at a hand therefore hangs the whole visible shoe off into space beside it.
        ///
        /// 🧑 2026-08-29, with a posed screenshot: *"also slipper floats for everyone including
        /// bots, it isnt on their arms i had to do this pose to show it but it floats for all
        /// poses"*. `docs/TODO.md` § 80.5.
        ///
        /// ⚠️⚠️ AND `CarryTests` IS BLIND TO IT BY CONSTRUCTION, WHICH IS WHY IT SHIPPED. That
        /// test measures `Distance(slipper.transform.position, anchor.position) - RestHeight`,
        /// which is the ORIGIN against the anchor. `RideAnchor` sets exactly that, so the
        /// assertion is arithmetically satisfied while the drawn shoe is anywhere at all. **The
        /// test measures the origin and the player sees the mesh.**
        ///
        /// ⚠️ THE VIEWMODEL ALREADY SOLVED THIS, IN THE OTHER VIEW AND WITH THE SAME REASONING.
        /// `ViewmodelArms.NormaliseHeldSize` subtracts `grip * (bounds.center * k)` for precisely
        /// this, and § 79.8 records the first-person version of the bug as its attempt 1: *"the
        /// shoe hung in space beside the hand"*. The two carries were fixed years apart in
        /// developer time and only one of them got the correction.
        ///
        /// ⚠️ READ OFF `Renderer.bounds`, WHICH IS SOLVED PER SKIN RATHER THAN GUESSED. Nine
        /// slippers from five sources have five ideas of where a shoe's origin is; a constant
        /// would be right for one of them. The world AABB of a ROTATED mesh is centred on that
        /// mesh's rotated centre, so this is the true offset and not an approximation, and it
        /// costs nothing because the renderer maintains those bounds anyway.
        /// </summary>
        public Vector3 DrawnCentreOffset
        {
            get
            {
                var r = GetComponentInChildren<Renderer>();
                return r != null ? r.bounds.center - transform.position : Vector3.zero;
            }
        }

        public float CarrySupportExtent(Vector3 palmNormal)
        {
            var renderer=GetComponentInChildren<Renderer>();
            if(renderer==null)return Balance.SlipperRestHeight;
            var filter=renderer.GetComponent<MeshFilter>();
            if(filter==null || filter.sharedMesh==null)return Balance.SlipperRestHeight;
            var normal=palmNormal.sqrMagnitude>.0001f?palmNormal.normalized:Vector3.up;
            var extents=filter.sharedMesh.bounds.extents;
            var matrix=filter.transform.localToWorldMatrix;
            // Project the oriented local bounds onto the palm normal. A world
            // AABB grows as a flat shoe turns upright; it is not sole thickness.
            float extent=Mathf.Abs(Vector3.Dot(normal,matrix.MultiplyVector(Vector3.right)))*extents.x
                +Mathf.Abs(Vector3.Dot(normal,matrix.MultiplyVector(Vector3.up)))*extents.y
                +Mathf.Abs(Vector3.Dot(normal,matrix.MultiplyVector(Vector3.forward)))*extents.z;
            return Mathf.Max(Balance.SlipperRestHeight,extent);
        }

        public float RestHeight
        {
            get
            {
                var r = GetComponentInChildren<Renderer>();
                return r != null ? Mathf.Max(Balance.SlipperRestHeight, r.bounds.extents.y)
                                 : Balance.SlipperRestHeight;
            }
        }

        /// <summary>
        /// Takes this tsinelas out of whoever was holding it before <paramref name="next"/>.
        ///
        /// ⚠️⚠️ WITHOUT THIS A LOOSE SLIPPER RIDES SOMEBODY'S HAND, AND THAT IS TWO OF THE FOUR
        /// THINGS 🧑 PHOTOGRAPHED IN THE 4.70 TUTORIAL AT ONCE. `Carrier.RideAnchor` writes
        /// `Held`'s transform every LateUpdate and asks nothing about the slipper's STATE, so a
        /// carrier that was never told it had lost one keeps dragging it: the shoe hangs at hand
        /// height wherever that body goes, and because it is LOOSE it also lights up, prompts and
        /// can be picked up. *"theres a floating slipper check ss"* and *"i can pick up slippers
        /// from ppl's hands wtf?"* are the same defect seen from two angles.
        ///
        /// ⚠️ THE ROUTE IN WAS `HostForceEquip`, WHICH IS THE ROUND-START ARMING AND RUNS IN
        /// EVERY MATCH. It wrote `Holder` and told the NEW carrier, and nothing anywhere told
        /// the old one. `ApplySnapshotState` had the clearing line from the day it was written,
        /// for exactly this reason, and it was the only writer of four that did.
        /// `TrainingStreetProbe` measures it: before this, the guided route reached its punch
        /// lesson with a LOOSE tsinelas resting 0.91 m off the road in the dummy's hand.
        ///
        /// ⚠️ IT IS `slipper.gd`'S RULE RESTATED: *"Two writers of the same relationship is how
        /// it ends up half-cleared."* Every write of `Holder` in this file goes through here.
        /// </summary>
        private void ReleasePreviousHolder(CharacterMotor next)
        {
            if (Holder == null || Holder == next) return;

            var previous = Holder;
            var carrier = previous.GetComponent<Carrier>();

            // ⚠️⚠️ ONLY IF THAT CARRIER IS STILL POINTING AT *THIS* TSINELAS. `Holder` can be
            // stale in the other direction too: a body that has since picked up a different
            // shoe is holding something real, and clearing its hand from here would be this
            // same bug with the sign flipped.
            if (carrier != null && carrier.Held != this) return;

            previous.HoldingSlipper = false;
            carrier?.NotifyHolding(null);
        }

        /// <summary>
        /// Everything <see cref="CanBeGrabbedBy"/> asks EXCEPT where the body is standing.
        ///
        /// ⚠️⚠️ IT EXISTS SO THE RETRIEVAL SLIDE CAN WIDEN THE REACH WITHOUT RESTATING THE RULE,
        /// AND RESTATING IT IS EXACTLY WHAT WENT WRONG. `CombatVerbs.AnySlideTargetAhead` tested
        /// `s.State == SlipperState.Loose` by hand and nothing else, so its idea of an eligible
        /// tsinelas and this file's idea of one were two rules that happened to agree on one
        /// clause. `docs/TODO.md` § 94.1's whole finding is what a second answer to "whose shoe
        /// is this" costs, and § 146.4's own note says the pickup rule must not be restated.
        ///
        /// ⚠️ THE REACH IS THE ONLY THING A SLIDE RELAXES, and it relaxes it to a SEGMENT rather
        /// than a bigger circle: the ground the body actually covers. Everything else on this
        /// method applies to a slide exactly as it applies to a walk-up.
        ///
        /// ⚠️⚠️ AND § THE OWNERSHIP LOCK IS ENFORCED HERE, WHICH IS THE ONLY PLACE IT COULD BE.
        /// Seven call sites ask this question (the walk-up, the retrieval slide's predicate and
        /// its sweep, the host's re-check of a client's request, both HUD prompts and the
        /// recall mark), and `docs/TODO.md` § 94.1 is what a second answer to *"whose shoe is
        /// this"* costs. <see cref="OwnerSlot"/> carries the rule and the three times it has been
        /// called.
        /// </summary>
        // This shared identity gate also applies to force equip and credited
        // throws. Ownerless equipment is an explicit training exception.
        public bool OwnershipAllows(CharacterMotor who)
        {
            if (who == null) return false;
            if (_ownerSlot >= 0) return _ownerSlot == who.PlayerSlot;
            // Ownerless equipment is deliberate training stock, never a live
            // multiplayer escape from the ownership rule. The absent seat is
            // still its stable address; it does not become the human's identity.
            if (!PracticeSandbox.Allowed) return false;
            if (GameLaunch.GuidedTutorial) return true;
            var round = GameServices.Round;
            if (round == null || SeatOfOrigin < 0 || SeatOfOrigin >= Balance.PlayerCount ||
                round.PlayerAt(SeatOfOrigin) != null) return false;
            int seated = 0;
            for (int i = 0; i < Balance.PlayerCount; i++) if (round.PlayerAt(i) != null) seated++;
            return seated == 1 && round.PlayerAt(who.PlayerSlot) == who;
        }

        public bool IsGrabbableIgnoringReach(CharacterMotor who)
        {
            if(!gameObject.activeInHierarchy)return false;
            if (State != SlipperState.Loose || who == null) return false;
            if (who.IsDefender) return false;   // the taya has the tag, not the ammunition

            // § THE OWNERSHIP LOCK. 🧑 2026-09-19: *"we should disable being able to take other
            // people's tsinelas when you are attacking."*
            //
            // ⚠️⚠️ THE TEST IS "OWNED BY SOMEBODY ELSE", NOT "NOT OWNED BY ME", AND THE
            // DIFFERENCE IS A WHOLE GAME MODE. A negative `OwnerSlot` is an UNOWNED shoe, and an
            // empty practice lobby is made almost entirely of them: with the bot seats set to
            // NONE, three of the four tsinelas belong to seats that were never built and are left
            // lying in the street deliberately. Writing this as `!= who.PlayerSlot` takes every
            // one of them away and leaves the single human with nothing to practise retrieving,
            // which `SoloPracticeTests` asserts against by name.
            if (!OwnershipAllows(who)) return false;

            // The same empty-hand rule as Carrier's local pickup route. A delayed
            // remote grab must not overwrite a newer, already accepted possession.
            if(who.HoldingSlipper) return false;
            if (!who.CanAct()) return false;

            return true;
        }

        public bool CanBeGrabbedBy(CharacterMotor who)
        {
            if (!IsGrabbableIgnoringReach(who)) return false;

            float d = Vector3.Distance(who.transform.position, transform.position);
            return d <= Balance.PickupRadius;
        }

        /// <summary>
        /// ⚠️ CONTESTED PICKUPS RESOLVE HOST-SIDE, and the ordering is what makes it safe: the
        /// first grab moves the slipper out of LOOSE, so a same-frame second grab fails on the
        /// first line of <see cref="CanBeGrabbedBy"/>. There is no window in which two
        /// attackers both succeed.
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ IT TELLS THE CARRIER ITSELF, RATHER THAN LEAVING THAT TO THE CALLER.
        ///
        /// **This was not a live bug**, and saying so matters: all three shipped callers
        /// (`Carrier.HostPickUp`, `Carrier.TryGrab`, and the networked grab through `MatchRpc`)
        /// already called `NotifyHolding` on the very next line. It was a TRAP, found by a test
        /// that called this directly and got a slipper that believed it was held, a motor that
        /// reported holding one, and a `Carrier` with nothing in hand — after which nothing
        /// moved the slipper (the carry reads `Held`) and the local player's viewmodel stayed
        /// empty (the rig reads `Held` too). That is *"the slippers just float when you hold
        /// it"* one careless call site away, in a codebase where the pickup already has three.
        ///
        /// `slipper.gd` owns this relationship for exactly that reason and says so: *"Written
        /// only by `slipper.gd` through `notify_holding()`, so there is one owner of the
        /// relationship. Two writers of the same relationship is how it ends up half-cleared."*
        /// The port had two. The existing calls stay and are harmless — `NotifyHolding` is
        /// idempotent now — but nothing depends on them any more.
        /// </remarks>
        public bool HostGrab(CharacterMotor who)
        {
            if (!NetAuthority.ShouldResolve()) return false;
            if (!CanBeGrabbedBy(who)) return false;

            ReleasePreviousHolder(who);
            SetState(SlipperState.Held);
            Holder = who;
            who.HoldingSlipper = true;
            _velocity = Vector3.zero;

            who.GetComponent<Carrier>()?.NotifyHolding(this);

            // ⚠️⚠️ THE RETRIEVAL IS RECORDED HERE BECAUSE THIS IS THE ONE PLACE ONE HAPPENS.
            // `docs/VISION.md` § 0: *"the tension is the retrieval, not the throw"*, and until
            // `docs/TODO.md` § 147 nothing in the game wrote down that the most important moment
            // in its own design had occurred. A walk-up pickup and the committed slide both reach
            // this method, so both are recorded and neither needs to remember to.
            //
            // ⚠️ IT AWARDS NOTHING AND IS HOST-SIDE ONLY, which is a known and accepted asymmetry
            // rather than an oversight: every other producer of a marker runs on every peer
            // through `MatchFlair`, and a pickup has no flair event to ride. A client's own log
            // therefore has one kind fewer in it. Adding a wire message for a caption would cost
            // a protocol field for something no rule reads. `MatchHighlights`' class note carries
            // the general form of the argument.
            Diagnostics.MatchHighlights.NoteRetrieval(
                who.PlayerSlot,
                Diagnostics.HighlightWatch.MetresFromTaya(transform.position),
                GameServices.Round != null && GameServices.Round.RoundActive
                    ? GameServices.Round.TimeLeft
                    : -1.0f);

            return true;
        }

        /// <summary>
        /// § THE ROUND-START EQUIP. `slipper.gd::host_force_equip`.
        ///
        /// ⚠️⚠️ IT BYPASSES <see cref="CanBeGrabbedBy"/> ON PURPOSE AND THAT IS THE POINT OF IT.
        /// This is not a pickup: nobody walked over to it and nobody pressed anything. It is the
        /// game handing an attacker their own tsinelas at the whistle, and every clause in the
        /// grab gate is about a CONTESTED pickup mid-round. The pickup RADIUS in particular
        /// cannot be satisfied here — the owner was teleported to their mark on the same frame,
        /// and a distance measured against an interpolated transform misses by enough to leave
        /// one attacker of three empty-handed at random. Godot measured exactly that and its
        /// note names the count: *"one of three slippers left LOOSE at its own player's feet"*.
        ///
        /// ⚠️ IT STILL REFUSES THE TAYA. That is a RULE, not a precondition: the defender has
        /// the tag, never the ammunition, and it is the one clause of the gate that has to
        /// survive.
        ///
        /// ⚠️ AND IT DOES NOT PLAY THE PICKUP SOUND OR THE GRAB CLIP. `NotifyHolding` fires
        /// both, which is right for a pickup and wrong for three seats being armed on the same
        /// frame of a countdown: it stacked three "pickup" cues on one frame and started a
        /// reach-down animation nobody performed. The relationship is written the same way it
        /// is in <see cref="HostGrab"/>, through the carrier, and only the feedback is skipped.
        /// </summary>
        public bool HostForceEquip(CharacterMotor who)
        {
            if (!NetAuthority.ShouldResolve()) return false;
            if (who == null || who.IsDefender || !OwnershipAllows(who)) return false;

            // A warmup grab or a recall may already occupy this hand. Replacing
            // only Carrier.Held leaves the displaced shoe claiming the same holder,
            // so later snapshots silently equip it again after the real throw.
            var displaced=who.GetComponent<Carrier>()?.Held;
            if(displaced!=null && displaced!=this && displaced.Holder==who && displaced.HostDisarm())
            {
                displaced.transform.position=who.transform.position;
                displaced.Land(false,FindGroundY(who.transform.position,Balance.SlipperRestHeight));
                Net.MatchRpc.Instance?.BroadcastSlipperState(displaced);
            }

            ReleasePreviousHolder(who);
            SetState(SlipperState.Held);
            Holder = who;
            who.HoldingSlipper = true;
            _velocity = Vector3.zero;

            who.GetComponent<Carrier>()?.NotifyEquipped(this);
            return true;
        }

        /// <summary>
        /// Takes this tsinelas out of whoever is holding it, without throwing it. The inverse of
        /// <see cref="HostForceEquip"/>.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE NOTHING EMPTIED THE NEW TAYA'S HAND, AND THE TAYA IS THE ONE
        /// UNIT THAT MAY NOT CARRY ONE. 🧑 2026-08-29, off the built player with a tsinelas in
        /// his own first-person hands and `TAYA` on his card: *"why the fuck does taya have
        /// slipper in practice mode? Does this also happen in multiplayer"*. **It does.**
        /// `SliceRunner.EquipOwnedSlippers` runs host-side on every peer's behalf and is the only
        /// thing that touches ownership at a round change; it disowns and deactivates the slipper
        /// whose INDEX matches the defender's seat, and never looks at what anybody is actually
        /// holding. `Carrier.Held` was assigned to `null` in exactly one place in the whole file,
        /// at the end of a throw.
        ///
        /// ⚠️ SO IT IS NOT ONLY THE TAYA'S OWN SHOE, WHICH IS WHY DEACTIVATING BY INDEX COULD
        /// NEVER HAVE FIXED IT. `OwnerSlot` was a label and not a lock when this was written
        /// (`docs/TODO.md` § 79.9 records that decision being taken deliberately and twice), so
        /// the incoming taya was frequently holding somebody ELSE'S tsinelas, which that loop
        /// leaves active and equipped by construction. ⚠️ § THE OWNERSHIP LOCK closed that route
        /// on 2026-09-19 and this method is still needed: the taya is still holding THEIR OWN
        /// shoe at the whistle, and `HostForceEquip` still puts one in a hand without asking the
        /// lock at all.
        ///
        /// ⚠️ AND THE VIEWMODEL IS A SEPARATE MESH, so even for a matching index, switching the
        /// world object off left the first-person copy in frame. `Carrier.NotifyHolding(null)`
        /// clears `HoldingSlipper`, which is what `ViewmodelArms.SetHolding` reads.
        /// </summary>
        public bool HostDisarm()
        {
            if (!NetAuthority.ShouldResolve()) return false;
            if (State != SlipperState.Held || Holder == null) return false;

            var previous = Holder;

            SetState(SlipperState.Loose);
            Holder = null;
            _velocity = Vector3.zero;

            previous.HoldingSlipper = false;

            // ⚠️ ONLY IF THAT CARRIER IS STILL POINTING AT *THIS* TSINELAS, which is the same
            // guard `ReleasePreviousHolder` carries and for the same reason: a body that has
            // since picked up a different shoe is holding something real.
            var carrier = previous.GetComponent<Carrier>();
            if (carrier != null && carrier.Held == this) carrier.NotifyHolding(null);

            return true;
        }

        public float PektusSpin { get; private set; }

        public bool HostBeginMapRecovery()
        {
            if(!NetAuthority.ShouldResolve()||!gameObject.activeSelf)return false;
            HostDisarm();Holder=null;SetState(SlipperState.Loose);
            _velocity=Vector3.zero;SetLandedHighlight(false);gameObject.SetActive(false);return true;
        }

        public void HostFinishMapRecovery()
        {
            if(!NetAuthority.ShouldResolve())return;
            transform.position=OwnerMark();gameObject.SetActive(true);
            Land(false,FindGroundY(transform.position,Balance.SlipperRestHeight));
        }

        /// <summary>
        /// Restores authoritative slipper state for a late join without firing pickup sounds,
        /// animation, scoring, or throw feedback. Both halves of the carrier relationship are
        /// rewritten so a reclaimed player never has a slipper visually in hand while the
        /// slipper still believes somebody else owns it.
        /// </summary>
        public void ApplySnapshotState(SlipperState state, CharacterMotor holder,
                                       Vector3 position, Quaternion rotation,
                                       Vector3 velocity, float pektusSpin,
                                       SlipperAffinity affinity, int throwerSlot)
        {
            bool enteringEmpoweredFlight = state == SlipperState.InFlight
                && (affinity == SlipperAffinity.FireExplosive || affinity == SlipperAffinity.ElectricZap)
                && (State != SlipperState.InFlight || Affinity != affinity);
            if (enteringEmpoweredFlight && Holder != null && Holder.PlayerSlot == throwerSlot)
            {
                if (affinity == SlipperAffinity.FireExplosive && Holder.AbilitySystem?.Kit is Abilities.SeanHeroKit sean)
                    sean.ConsumeIgnition();
                else if (affinity == SlipperAffinity.ElectricZap && Holder.AbilitySystem?.Kit is Abilities.ZackHeroKit zack)
                    zack.ConsumeMagnetCharge();
            }
            ReleasePreviousHolder(holder);

            // ⚠️⚠️ A HELD SLIPPER IS PLACED BY THE HAND THAT HOLDS IT, NOT BY THE WIRE. `Carrier`
            // parents the tsinelas to the carry anchor every FixedUpdate on every peer, and the
            // host streams this snapshot at the same 50 Hz. Writing both meant two authors for
            // one transform: the packet put it where the host's hand was a step ago, the carry
            // put it where this screen's hand is now, and the shoe visibly buzzed between the two
            // for as long as anybody held it. The STATE and the HOLDER are authoritative; while
            // it is in a hand, the position is a consequence of them.
            if (state != SlipperState.Held)
            {
                // ⚠️⚠️ THE WALLS APPLY TO THE REPLICA TOO, AND THIS IS THE TSINELAS HALF OF 🧑
                // 2026-08-29's *"if u werent host, the bots and slippers were going out of
                // map"*. Only the non-host saw it because only the host runs `BounceOffBounds`
                // and the resting clamp: `FixedUpdate` returns immediately on a peer that is not
                // simulating, so on a client the arena had no edges at all and a shoe went
                // wherever the last packet said.
                //
                // ⚠️ THE SAME LIMITS THE HOST BOUNCES AND RESTS AGAINST, margin included, so this
                // can only ever refuse a position the host would also have refused. A tsinelas
                // that came to rest outside the wall is an attacker deleted from the round, which
                // is what the note on the resting clamp already says at length.
                position.x = ClampToPlayableAxis(position.x, AIController.PlayableHalfX);
                position.z = ClampToPlayableAxis(position.z, AIController.PlayableHalfZ);

                // ⚠️ THE LID IS CLAMPED ON THE REPLICA TOO, and only downward: a client must not
                // draw a tsinelas above a ceiling the host bounced it off, but it must not push
                // one UP off the ground either, so this is a ceiling rather than an axis clamp.
                float ceiling = AIController.PlayableCeilingY - Balance.SlipperHitRadius;
                if (position.y > ceiling) position.y = ceiling;

                transform.SetPositionAndRotation(position, rotation);
            }

            // ⚠️⚠️ THE LANDED RIM IS LIT HERE TOO, AND UNTIL 2026-09-19 A CLIENT NEVER SAW ONE
            // AT ALL. `Land` is the only thing that can turn it on and `Land` is reached only
            // from inside a `NetAuthority.ShouldResolve()` gate, so for the whole life of
            // § THE LANDED HIGHLIGHT it was a host-only feature wearing the name of a player
            // one. Nothing failed and nothing logged: a non-host player simply threw their
            // tsinelas and it came to rest unlit, which reads as the setting being broken.
            // This is `docs/TODO.md` § 38's thesis on one more object.
            //
            // ⚠️ THE TRANSITION IS THE SIGNAL, because that is the only thing a client is told.
            // `SetState` clears the flag on every move out of Loose, so this has to be written
            // AFTER it for the same reason `Land` writes it last.
            //
            // ⚠️ ONE CASE DIVERGES FROM THE HOST AND IT DIVERGES THE HELPFUL WAY. A tsinelas the
            // host RECOVERS rather than lands (stranded on a roof, or lost under the world)
            // passes `fromFlight: false` and is deliberately not lit, because the rim would point
            // at a place the throw never reached. The client cannot tell those apart from one
            // InFlight-to-Loose edge, so it lights them. It is still this player's own shoe, it
            // is still Loose, and the rim is still telling them where it is. Putting the
            // distinction on the wire would cost a field and a `ProtocolVersion` bump
            // (`CLAUDE.md` § 4a) for a cosmetic tie-break, and the two halves of the game have to
            // be rebuilt and shipped together whenever that number moves.
            bool landedNow = State == SlipperState.InFlight && state == SlipperState.Loose;

            SetState(state);
            if (landedNow && !NetAuthority.ShouldResolve()) SetLandedHighlight(true);
            Holder = state == SlipperState.Held ? holder : null;
            _velocity = state == SlipperState.InFlight ? velocity : Vector3.zero;
            PektusSpin = state == SlipperState.InFlight
                ? Mathf.Clamp(pektusSpin, -Balance.MaxPektusSpin, Balance.MaxPektusSpin)
                : 0.0f;
            Affinity = state == SlipperState.InFlight ? affinity : SlipperAffinity.Normal;
            _throwerSlot = state == SlipperState.InFlight ? throwerSlot : -1;
            if (enteringEmpoweredFlight)
            {
                if (_affinityVfxGo != null) Destroy(_affinityVfxGo);
                if (affinity == SlipperAffinity.FireExplosive) CreateFireFlightVisual();
                else CreateZapFlightVisual();
            }
            else if (state != SlipperState.InFlight || affinity == SlipperAffinity.Normal)
            {
                if (_affinityVfxGo != null && (_affinityVfxGo.name == "FireSlipperVfx" || _affinityVfxGo.name == "ZapSlipperVfx"))
                {
                    Destroy(_affinityVfxGo); _affinityVfxGo = null;
                }
            }
            _flightTime = 0.0f;
            _airborneTotal = 0.0f;
            _throwerIgnoreLeft = 0.0f;
            _bodyContacts = 0;
            _bankCount = 0;
            _closestCanFlat = float.PositiveInfinity;
            _nearMissReported = false;

            if (Holder != null)
            {
                Holder.HoldingSlipper = true;
                Holder.GetComponent<Carrier>()?.NotifyEquipped(this);
            }
        }

        /// <summary>
        /// Moves a replica along the host's path WITHOUT touching what it is or who has it.
        ///
        /// ⚠️⚠️ THIS EXISTS SO THE POSE MAY TRAVEL ON A DIFFERENT CHANNEL FROM THE STATE, AND
        /// THAT IS THE WHOLE REASON IT IS A SEPARATE METHOD RATHER THAN A FLAG ON
        /// `ApplySnapshotState`. `MatchRpc` sends a shoe in flight fifty times a second, which
        /// must be unreliable or one lost packet head-of-line blocks the stream and delivers a
        /// burst (`MatchRpc.PoseDelivery`, `docs/TODO.md` § 71.3 and § 77.1). Its state, holder,
        /// affinity and thrower are events and must be reliable. Two channels for one object have
        /// no ordering between them, so if the unreliable packets also CARRIED the state, a pose
        /// sent a step before a throw could arrive a step after it and put the tsinelas back in
        /// the hand it had just left. Carrying no state at all is what makes that unthinkable
        /// rather than unlikely.
        ///
        /// ⚠️ WHAT THAT STILL LEAVES, SO NOBODY REDISCOVERS IT: carrying no state removes the
        /// CORRUPTION, not the ordering. A pose sent while the shoe was in flight can still arrive
        /// after the reliable packet that says it has landed and move a resting tsinelas by one
        /// step's travel, until the next keepalive corrects it. The bound is 0.5 s and about
        /// 0.3 m, position only. The fix if it is ever seen is a `Time.fixedTime` stamp on the
        /// message and a refusal here of anything older than the last applied; it was not built
        /// because no two-machine run has ever watched this stream. `docs/TODO.md` § 77.1.
        ///
        /// ⚠️ A HELD SHOE IS PLACED BY THE HAND AND NEVER BY THE WIRE, which is the same rule
        /// `ApplySnapshotState` states at length: `Carrier` parents it to the carry anchor on
        /// every peer, and writing a position underneath that is the two-author buzz of § 38.8.
        ///
        /// ⚠️ THE WALLS AND THE LID APPLY HERE TOO, and they are the same numbers the host bounces
        /// and rests against, so this can only refuse a position the host would have refused. It
        /// is the § 71.3 clamp, on the path that now carries most of the packets.
        /// </summary>
        public void ApplySnapshotPose(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (State == SlipperState.Held) return;

            position.x = ClampToPlayableAxis(position.x, AIController.PlayableHalfX);
            position.z = ClampToPlayableAxis(position.z, AIController.PlayableHalfZ);

            float ceiling = AIController.PlayableCeilingY - Balance.SlipperHitRadius;
            if (position.y > ceiling) position.y = ceiling;

            transform.SetPositionAndRotation(position, rotation);

            // ⚠️ THE VELOCITY IS THE SMOOTHING HINT AND ONLY MEANS ANYTHING IN FLIGHT. A loose
            // shoe has none, and writing one would give a resting tsinelas a drift this peer has
            // no authority to invent.
            if (State == SlipperState.InFlight) _velocity = velocity;
        }

        public void HostThrow(CharacterMotor thrower, Vector3 origin, Vector3 velocity, SlipperAffinity affinity = SlipperAffinity.Normal, float pektusSpin = 0.0f)
        {
            if (!NetAuthority.ShouldResolve()) return;
            // Null identifies an environmental ability displacement, which may
            // move any loose shoe without granting somebody else's shot credit.
            if (thrower != null && !OwnershipAllows(thrower)) return;
            FinishChain(ThrowChainEnd.Miss); // A credited flight replaced by a new launch has ended.
            _chainMatch = GameServices.Match;
            _chainOwner = thrower != null ? thrower.PlayerSlot : -1;
            _launchCanSerial = GameServices.Round != null && GameServices.Round.Lata != null
                ? GameServices.Round.Lata.HostKnockdownSerial : 0;
            _chainEpoch = _chainMatch != null ? _chainMatch.HostChainEpoch : 0;
            _chainThrow = _chainMatch != null && thrower != null
                ? _chainMatch.BeginHostThrowChain(_chainOwner, _launchCanSerial) : 0;
            SetState(SlipperState.InFlight);
            _throwerSlot = thrower != null ? thrower.PlayerSlot : -1;
            Affinity = affinity;
            PektusSpin = Mathf.Clamp(pektusSpin, -Balance.MaxPektusSpin, Balance.MaxPektusSpin);
            _bankCount = 0;
            _airborneTotal = 0.0f;
            _closestCanFlat = float.PositiveInfinity;
            _nearMissReported = false;

            // ⚠️⚠️ NO `PEKTUS!` CALLOUT. `ComicPopup`'s own rule is *"A CAST GETS NO WORD"*, and
            // this is the purest example of one: the player held the curve key, watched the arc
            // bend, and the game shouted the name of the thing they had just done. It fired on
            // EVERY spun throw, and Hero Strike measures 127 to 173 throws a match
            // (`CLAUDE.md` § 7.1), so it was one of the most frequent strings in the game.
            //
            // ⚠️ THE CURVE STILL ANNOUNCES ITSELF, JUST NOT IN WORDS. `Carrier` prints PEKTUS
            // CURVE on the charge meter while you are aiming it, the shoe visibly bends, and
            // `BANK!` still fires when the spin actually banks off something, which is the part
            // the thrower did NOT choose and could not see coming.

            if (thrower != null) thrower.HoldingSlipper = false;

            // ⚠️ A THROW IS A RELEASE TOO. `Carrier.HostThrowAt` clears its own `Held` on the way
            // through, but the AI, the networked throw and any future caller do not all pass the
            // holder as the thrower, and a carrier still pointing at a slipper in flight drags it
            // back out of the air.
            // A throw has no next holder. Passing the thrower here used the grab
            // helper's same-holder exemption and left Carrier.Held attached on
            // direct throws. RideAnchor then overwrote the flight every frame.
            // Carrier.HostThrowAt also clears Held, but this entry point must be
            // complete on its own. MapRetrievalProbe isolates both call paths.
            ReleasePreviousHolder(null);
            Holder = null;

            transform.position = origin;
            _velocity = velocity;
            _flightTime = 0.0f;

            // You cannot block your own throw on release.
            _throwerIgnoreLeft = Balance.ThrowerIgnoreTime;
            _bodyContacts = 0;

            if (_affinityVfxGo != null) Destroy(_affinityVfxGo);

            if (Affinity == SlipperAffinity.FireExplosive) CreateFireFlightVisual();
            else if (Affinity == SlipperAffinity.ElectricZap) CreateZapFlightVisual();
            else if (UI.SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                _affinityVfxGo = new GameObject("HeroSlipperTrail");
                _affinityVfxGo.transform.SetParent(transform, false);
                var trail = _affinityVfxGo.AddComponent<TrailRenderer>();
                trail.time = 0.22f;
                trail.startWidth = 0.14f;
                trail.endWidth = 0.0f;
                var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(1.0f, 0.95f, 0.7f, 0.6f) };
                trail.material = mat;
                trail.startColor = mat.color;
                trail.endColor = new Color(1.0f, 0.95f, 0.7f, 0.0f);
            }
        }

        private void CreateFireFlightVisual()
        {
            _affinityVfxGo = new GameObject("FireSlipperVfx");
            _affinityVfxGo.transform.SetParent(transform, false);
            var light = _affinityVfxGo.AddComponent<Light>();
            light.color = Visual.AbilityVfx.FireHotColour; light.range = 2.1f; light.intensity = .65f;
            light.shadows = LightShadows.None;
            var trail = _affinityVfxGo.AddComponent<TrailRenderer>();
            trail.time = .22f; trail.startWidth = .14f; trail.endWidth = 0;
            trail.minVertexDistance = .04f;
            var material = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
            trail.sharedMaterial = material; Visual.VfxRenderTag.Own(_affinityVfxGo, material);
            trail.startColor = Visual.AbilityVfx.FireHotColour;
            trail.endColor = new Color(1, .22f, .015f, 0);
        }

        private void CreateZapFlightVisual()
        {
            _affinityVfxGo = new GameObject("ZapSlipperVfx");
            _affinityVfxGo.transform.SetParent(transform, false);
            var light = _affinityVfxGo.AddComponent<Light>();
            light.color = UI.UiTheme.HeroElectricBright; light.range = 1.6f; light.intensity = .45f;
            light.shadows = LightShadows.None;
            var material = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
            Visual.VfxRenderTag.Own(_affinityVfxGo, material);
            for (int i = 0; i < 2; i++)
            {
                var rail = new GameObject(i == 0 ? "PositiveTrace" : "ReturnTrace");
                rail.transform.SetParent(_affinityVfxGo.transform, false);
                rail.transform.localPosition = new Vector3(i == 0 ? -.035f : .035f, 0, 0);
                var trail = rail.AddComponent<TrailRenderer>();
                trail.time = i == 0 ? .16f : .11f; trail.startWidth = i == 0 ? .035f : .018f;
                trail.endWidth = 0; trail.minVertexDistance = .035f; trail.sharedMaterial = material;
                trail.startColor = i == 0 ? new Color(1, .9f, .25f) : new Color(1, .99f, .8f);
                trail.endColor = new Color(1, .9f, .25f, 0);
            }
        }

        private void TriggerAffinityImpact()
        {
            if (Affinity == SlipperAffinity.FireExplosive)
            {
                // ⚠️⚠️ 2.6 m, DOWN FROM 4.5, BECAUSE THIS IS A SKILL'S PAYLOAD AND NOT AN
                // ULTIMATE. At 4.5 m it covered **32.5 per cent of the 14 by 14 box**, the same
                // area as Zack's Thunderstrike, off Sean's second skill. `docs/VISION.md` § 2
                // rule 1 asks a skill for 1.8 to 2.5 m and rule 2 reserves "big" for one
                // ultimate at a time.
                //
                // ⚠️ THE REACH IS REPLACED BY A HARD VERTICAL, WHICH IS RULE 3. A smaller flat
                // blast is still a puddle, so `CreateExplosion` is given a taller, faster
                // silhouette to work with rather than a wider one: the knockback is unchanged
                // at 13.0 and the stun at 1.4 s, so what a direct hit DOES is untouched. What
                // changed is how far away it can be felt by someone who was nowhere near it.
                // ⚠️ THE SLIPPER STYLE, because a tsinelas going off is the game's joke and not
                // an ultimate. It shared the supernova's fireball, flash, shake and sound, which
                // told the player the two were the same size of event.
                //
                // ⚠️⚠️ FLARE SHOT TIGHTENS THE CRATER, AND IT IS READ HERE RATHER THAN AT THE
                // THROW. `AdoptState` can hand a peer a shoe that is already in flight with no
                // record of how it left the hand, so a flag latched in `HostThrow` would be
                // false on exactly the machines that did not watch the throw and they would
                // draw a 2.6 m fireball over a 1.95 m blast. Every peer binds the seat's
                // checked build in `MatchInstaller`, so asking the thrower at impact gives the
                // same answer everywhere. The fraction is read off the row rather than written
                // here, for the reason `Carrier`'s throw note gives.
                var caster = GameServices.Round?.PlayerAt(_throwerSlot);
                float blastRadius = 2.6f * (caster != null && caster.AbilitySystem != null
                    ? caster.AbilitySystem.VariantCost("sean.2.flare")
                    : 1.0f);
                Abilities.HeroHazards.CreateExplosion(transform.position, blastRadius, 13.0f, 1.4f, _throwerSlot, "BOOM!",
                    style: Abilities.HeroHazards.ExplosionStyle.Slipper);
            }
            else if (Affinity == SlipperAffinity.ElectricZap)
            {
                NetCue.Play("ability_flick_dash", transform.position);

                var round = GameServices.Round;
                if (round != null)
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == _throwerSlot) continue;

                        // ⚠️⚠️ 2.0 m, DOWN FROM 5.5, AND 5.5 WAS THE WORST NUMBER IN THE GAME.
                        // It staggered everyone within **48 per cent of the box** and drew
                        // NOTHING on the floor to say so, which is worse than a puddle: a
                        // player knocked about by a tsinelas that landed six metres away has
                        // been given no way to understand what hit them, and no telegraph
                        // rule can help because there was no telegraph to be wrong.
                        //
                        // ⚠️ AND CUTTING IT IS WHAT SPLITS ZACK FROM SEAN. Their kits shipped
                        // as three matching slots, and this was Zack's copy of Sean's Ignition
                        // Cannon. Static Charge is a SPEED skill now: the throw flies faster
                        // and flatter and jolts whoever it actually lands on.
                        // `docs/Hero_Strike_Balance.md` § 4.4.
                        if (Vector3.Distance(transform.position, p.transform.position) <= 2.0f)
                        {
                            // ⚠️ THE STAGGER STAYS HERE AND THE FLOURISH TRAVELS. The stun is a
                            // RULE and belongs on the host behind this gate; the stars, the jolt
                            // and the zap ring are what three other people could not see. See
                            // `Visual.MatchFlair`, which draws them from the seat number.
                            p.ApplyStagger(1.5f);
                            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.Zap,
                                                       _throwerSlot, p.PlayerSlot,
                                                       transform.position);
                        }
                    }
                }
            }

            if (_affinityVfxGo != null)
            {
                Destroy(_affinityVfxGo);
                _affinityVfxGo = null;
            }
            Affinity = SlipperAffinity.Normal;
            PektusSpin = 0.0f;
        }

        /// <summary>
        /// Host-side flight. ⚠️ EVERY CONTACT HERE IS A DISTANCE CHECK, deliberately: an
        /// overlap volume fires on whichever peer owns the body, and 16 of 36 were measured
        /// failing to land.
        /// </summary>
        private void FixedUpdate()
        {
            if (State != SlipperState.InFlight) return;
            if (!NetAuthority.ShouldResolve()) return;

            float dt = Time.fixedDeltaTime;
            _flightTime += dt;
            _airborneTotal += dt;
            if (_throwerIgnoreLeft > 0.0f) _throwerIgnoreLeft -= dt;

            _velocity.y -= Balance.Gravity * dt;

            // Apply lateral Magnus acceleration from Pektus spin
            if (Mathf.Abs(PektusSpin) > 0.01f)
            {
                Vector3 flatVel = new Vector3(_velocity.x, 0.0f, _velocity.z);
                if (flatVel.sqrMagnitude > 0.1f)
                {
                    Vector3 lateral = Vector3.Cross(flatVel.normalized, Vector3.up).normalized;
                    _velocity += lateral * (PektusSpin * Balance.PektusCurveStrength * dt);
                }
            }

            // -------------------------------------------------------------------
            // ⚠️⚠️ A TSINELAS HAS A TERMINAL SPEED, AND THIS IS A GUARD RATHER THAN A CURE.
            // 🧑 2026-08-27: *"appparently slippers randomly fly to sky too? idk how playtesters
            // did that"*. The exact source is NOT identified and this does not claim to have
            // found it; what it does is bound the symptom so a single bad frame cannot remove a
            // slipper from the match.
            //
            // ⚠️ THERE ARE SEVERAL PLACES A LARGE VELOCITY CAN BE MANUFACTURED and none of them
            // is obviously wrong on its own: `Deflect` off the lata multiplies the incoming speed
            // by `LataRecoilScale`, so two recoils in quick succession compound; a
            // `Vector3.Reflect` in `BounceOffObstacles` falls back to `-disp.normalized` which is
            // ZERO if the slipper did not move that frame, and reflecting about a zero normal
            // returns the velocity unchanged rather than reversing it; and `HeroHazards`
            // teleports loose slippers every frame during Nemu's ultimate, which can drive one
            // into a collider that then ejects it.
            //
            // ⚠️ 34 m/s IS ABOVE ANYTHING THE GAME CAN LEGITIMATELY PRODUCE. The hardest legal
            // throw leaves the hand well under this, so a slipper that reaches it has been given
            // energy by a defect. Clamping preserves the DIRECTION, so a hard throw still flies
            // hard and only the impossible case is cut. **If this clamp ever fires in normal
            // play the number is wrong; if the sky-launch stops being reported, the cause is
            // still out there and is worth finding.** `docs/TODO.md` § 32.
            const float TerminalSpeed = 34.0f;
            if (_velocity.sqrMagnitude > TerminalSpeed * TerminalSpeed)
            {
                _velocity = _velocity.normalized * TerminalSpeed;
            }

            Vector3 prevPos = transform.position;
            transform.position += _velocity * dt;

            BounceOffObstacles(prevPos, dt);
            BounceOffBounds();
            SpinInFlight(dt);
            if(RooftopRecovery.Instance!=null&&RooftopRecovery.Instance.TryLoseSlipper(this))return;

            // ⚠️ LOST BELOW THE WORLD IS A REAL CASE, NOT A SAFETY NET. A slipper that
            // clears the arena falls forever and the round quietly loses a piece of its
            // ammunition — the attacker who owns it has nothing to fetch and simply stops
            // playing. Return it to its mark instead.
            if (transform.position.y < Balance.VoidY) { Land(fromFlight: false); return; }

            var round = GameServices.Round;
            if (round != null && _bodyContacts != 0)
                foreach (var player in round.Players)
                    if (player != null && !HitsBody(player)) _bodyContacts &= ~(1 << player.PlayerSlot);

            // ⚠️⚠️ THE TAYA'S BODY IS TESTED BEFORE THE CAN, AND EVERY OTHER BODY AFTER IT.
            // 🧑 2026-08-29: *"make sure defender cna block too"*, and *"feels like shit get past
            // defender very easily"*. `Lata.Connects` is a FLAT distance with no vertical test, and
            // the can check ran first, so a taya standing between a throw and the can lost the tie
            // inside a single physics step: at 18.5 m/s a step covers 0.37 m, which is most of the
            // window their body occupies. The taya was doing the one thing their role is for and
            // the can was scored through them.
            //
            // ⚠️ ONLY THE TAYA IS HOISTED. An ATTACKER standing over the can must not shield it —
            // that would let the throwing side park a body on the objective, which is the exact
            // abuse the taya's own camp penalty exists to stop on the other side. The loop below
            // still catches them, after the can, as it always did.
            //
            // ⚠️ AND IT IS NOT A FREE TURTLE. `ScoreTayaCampPenalty` is -5 a second after
            // `TayaCampGracePeriod` inside `TayaCampRadius`, so standing on the can to block
            // everything costs the taya the round. The counterweight was already in the rules;
            // this line is what makes the trade a real one rather than a lost tie.
            if (round != null && _throwerIgnoreLeft <= 0.0f)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || !p.IsDefender || p.PlayerSlot == _throwerSlot) continue;
                    if (!HitsBody(p)) continue;
                    // Keep simulating the rebound, but don't restart its lift, body
                    // impulse, sound and flair on every step spent inside one body.
                    int contactBit = 1 << p.PlayerSlot;
                    if ((_bodyContacts & contactBit) != 0) return;
                    _bodyContacts |= contactBit;
                    TriggerAffinityImpact();
                    HostBlockedBy(p);
                    return;
                }
            }

            // The can next: it is the thing being aimed at.
            if (round?.Lata != null && round.Lata.IsUpright && round.Lata.Connects(transform.position))
            {
                int before = round.Lata.HostKnockdownSerial;
                TriggerAffinityImpact();
                round.Lata.HostKnockDown(_throwerSlot);
                FinishChain(_throwerSlot >= 0 && round.Lata.HostKnockdownSerial > before
                    ? ThrowChainEnd.Hit : ThrowChainEnd.Miss);
                // REBOUND belongs to the shared can that was hit, never the thrower's
                // own loadout. Host-owned velocity already replicates to every peer.
                // The restore shield still returns the shoe without awarding a hit.
                float rebound = Roster.CanReboundScale(round.Lata.SkinIndex);
                Deflect(-_velocity * Balance.LataRecoilScale * rebound,
                        Balance.LataRecoilLiftScale * rebound);
                return;
            }

            TrackClassicNearMiss(round?.Lata);

            // ⚠️ THEN ANY STANDING BODY, ATTACKERS INCLUDED. Three of them crowding one box
            // means friendly fire is part of the traffic, and a slipper that passed through
            // teammates would make the Defender's body block the only block in the game.
            if (round != null && _throwerIgnoreLeft <= 0.0f)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == _throwerSlot) continue;
                    if (!HitsBody(p)) continue;
                    // Keep simulating the rebound, but don't restart its lift, body
                    // impulse, sound and flair on every step spent inside one body.
                    int contactBit = 1 << p.PlayerSlot;
                    if ((_bodyContacts & contactBit) != 0) return;
                    _bodyContacts |= contactBit;
                    TriggerAffinityImpact();
                    HostBlockedBy(p);
                    return;
                }
            }

            // ⚠️ AGAINST THE GROUND UNDER IT, NOT AGAINST WORLD ZERO. See GroundY: a flight that
            // ends at an absolute height either stops in mid-air over a raised slab or carries on
            // through a lowered one, and both were reachable on the shipped maps.
            // ⚠️ THE TWO ENDINGS ARE SPLIT, AND THEY WERE ONE CONDITION. Touching the ground is
            // a flight ENDING where the player can see it; running out of flight time is the
            // simulation giving up on a slipper that never arrived. Only the first is a landing
            // the thud and § THE LANDED HIGHLIGHT should fire for, which is exactly the
            // distinction the Godot original draws by grouping the timeout with the void
            // recovery instead.
            // Only support below this step's swept height can end its flight.
            // The broad placement query sees Ilalim's 9.04 m guideway from a
            // 3.6 m throw, so it previously landed and recovered on the first step.
            // Include the previous height so a fast descent cannot skip a raised
            // slab. Pass this same support into Land rather than reselecting a roof.
            Vector3 supportAt = transform.position;
            supportAt.y = Mathf.Max(prevPos.y, supportAt.y);
            float flightGround = FindGroundY(supportAt, Balance.SlipperRestHeight);
            if (transform.position.y <= flightGround + Balance.SlipperRestHeight)
                Land(fromFlight: true, landingGround: flightGround);
            else if (_flightTime >= Balance.MaxFlightTime
                     || _airborneTotal >= Balance.MaxAirborneTime)
                Land(fromFlight: false);
        }

        /// <summary>
        /// Turns a genuinely close miss into useful drama. It waits until the slipper is
        /// moving away from its closest approach, so a hit on the next physics step is never
        /// announced as a miss. Hero Strike already has affinity feedback; this is part of
        /// Classic Mode's street-skill identity.
        /// </summary>
        private void TrackClassicNearMiss(Lata lata)
        {
            if (_nearMissReported || _throwerSlot < 0 || lata == null
                || UI.SceneFlow.SelectedMode != GameMode.Classic || _flightTime < 0.12f)
                return;

            Vector3 delta = transform.position - lata.transform.position;
            delta.y = 0.0f;
            float distance = delta.magnitude;

            if (distance < _closestCanFlat)
            {
                _closestCanFlat = distance;
                return;
            }

            if (_closestCanFlat > 1.35f || distance < _closestCanFlat + 0.12f) return;

            _nearMissReported = true;

            // ⚠️⚠️ NO `SABLAY!` CALLOUT. A near miss is the one event in the game the player is
            // ALREADY looking directly at: they threw at the can, they are watching the shoe, and
            // it went past. The word said nothing the frame did not, and it fired on a 1.35 m
            // threshold, which in Hero Strike is most misses of most throws.
            //
            // The whip-past sound communicates a near miss without a cosmetic reward.
            // ⚠️⚠️ THROUGH `NetCue`: `FixedUpdate` OPENS WITH `ShouldResolve()`, SO EVERY SOUND
            // BELOW IT WAS HOST-ONLY. 🧑 2026-08-29: *"non hosts dont have sfx in some plarts"*.
            // `tools/audit_audio_reach.py` could not see this class of fault at all, because it
            // looks for a gate at the same brace depth and this one is two frames up the stack.
            // See `docs/TODO.md` § 83.12.
            NetCue.PlayVaried("slipper_bounce", lata.transform.position,
                              1.08f, 1.18f, 0.55f);
            // ⚠️⚠️ RELAYED, BECAUSE THE ENCLOSING VERB IS HOST-RESOLVED AND WHAT IT DRAWS IS
            // FOR EVERYBODY. 🧑 2026-08-29: *"make sure that all host sided shit is seen by
            // everyone and not js host"*. See `Visual.MatchFlair` and
            // `tools/audit_presentation_reach.py`, which is what found this one.
            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.NearMiss,
                                       _throwerSlot, -1, lata.transform.position);
        }

        /// <summary>
        /// Does the slipper's current position sit inside this body's capsule?
        ///
        /// ⚠️⚠️ A CAPSULE, NOT A SPHERE, AND THE PORT ASKED FOR A SPHERE AROUND THE FEET. 🧑 on
        /// this build: *"the players dont block the slipper thats thrown, theyre supposed to"*.
        /// The old test was
        ///
        ///     Vector3.Distance(p.transform.position, transform.position) &gt; HitRadius + radius
        ///
        /// and `p.transform.position` on these seats is the SOLE OF THE FOOT — `BuildSeat` gives
        /// the controller `center = (0, 0.8, 0)` precisely because the origin is at the feet. So
        /// the test asked whether the slipper was within 0.58 m OF THE GROUND AT THEIR TOES. A
        /// throw travels at chest height: measured against a 1.6 m seat, a slipper passing dead
        /// centre through the torso is about 1.2 m from that origin, more than double the
        /// window. The block could only ever fire on a slipper already rolling along the floor,
        /// which is to say never during a throw. The taya's entire passive verb was unreachable,
        /// and so was the friendly fire that keeps three attackers honest around one box.
        ///
        /// `slipper.gd::_first_body_hit` splits it in two on purpose, and its own comment says
        /// why: *"Capsule, not sphere: a slipper passing over a crouched head and one passing
        /// through a chest are different events and a sphere conflates them."* Flat distance
        /// decides whether the throw is on the body's LINE; the height band decides whether it
        /// is at a height the body occupies.
        ///
        /// ⚠️ THE BAND IS BUILT FROM THE CONTROLLER'S OWN CENTRE, NOT FROM THE TRANSFORM. The
        /// .gd's `dy` is measured against a CENTRED body, so it can write ±height/2 directly.
        /// Copying that expression literally against a feet-origin transform reintroduces the
        /// same off-by-a-body error one layer down, and it would look like a faithful port.
        /// </summary>
        private bool HitsBody(CharacterMotor who)
        {
            var cc = who.GetComponent<CharacterController>();

            // ⚠️ GUARDED. The old line dereferenced this straight off a `GetComponent` and a
            // seat mid-teardown at a round boundary is a null here, not a bug worth throwing on.
            float radius = cc != null ? cc.radius : 0.4f;
            float height = cc != null ? cc.height : 1.6f;

            Vector3 centre = who.transform.position + (cc != null ? cc.center : new Vector3(0.0f, height * 0.5f, 0.0f));

            Vector3 flat = new Vector3(transform.position.x - centre.x, 0.0f,
                                       transform.position.z - centre.z);

            // ⚠️⚠️ THE TAYA CATCHES WIDER, AND ONLY THE TAYA. `Balance.DefenderBlockRadiusBonus`
            // carries the quotes and the reasoning; the two things to know here are that it is
            // added to the FLAT test only, so a throw over the head still misses, and that it is
            // read off `IsDefender` on the frame of the test, so it reverts with the role and
            // leaves no state behind when the taya rotates.
            //
            // ⚠️ THE `CharacterController` IS DELIBERATELY NOT TOUCHED. That is what keeps the
            // body the size it looks: 🧑 asked for *"bigger horizontally but not fatter"*, and a
            // fatter capsule would shove attackers, snag on the map and change the spawn settle.
            float reach = Balance.SlipperHitRadius + radius
                          + (who.IsDefender ? Balance.DefenderBlockRadiusBonus : 0.0f);

            if (flat.magnitude > reach) return false;

            float dy = transform.position.y - centre.y;
            return dy >= -height * 0.5f && dy <= height * 0.5f;
        }

        /// <summary>
        /// Keep a throw inside the arena.
        ///
        /// ⚠️ ENERGY IS LOST ON THE BOUNCE. A perfectly elastic wall returns a slipper at
        /// throw speed, which is a projectile nobody threw and which can still knock the lata
        /// down — a point scored by the wall. 0.45 carries it clear of the boundary without
        /// being a shot.
        ///
        /// ⚠️ THE FORM IS `-sign(position) * abs(velocity)`, NOT A PLAIN SIGN FLIP. A flip
        /// would send a slipper that is somehow ALREADY outside and travelling inward back
        /// <summary>
        /// Bounces off solid obstacle colliders (such as viaduct pillars and roadside walls)
        /// in the arena, enabling tactical bank shots.
        /// </summary>
        private void BounceOffObstacles(Vector3 previousPos, float dt)
        {
            Vector3 disp = transform.position - previousPos;
            float dist = disp.magnitude;
            if (dist < 0.001f) return;

            var hits = Physics.SphereCastAll(previousPos, Balance.SlipperHitRadius, disp.normalized, dist,
                                             ~0, QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0) return;

            RaycastHit closest = default;
            float closestDist = float.MaxValue;
            bool hitFound = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.isTrigger) continue;
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                if (hit.collider.GetComponentInParent<Lata>() != null) continue;
                if (hit.collider.GetComponentInParent<Slipper>() != null) continue;
                if (hit.collider.name.StartsWith("Floor", StringComparison.OrdinalIgnoreCase)) continue;

                var contact=hit;
                if(hit.distance<=.0001f)
                {
                    // An initial sphere overlap reports a synthetic reverse normal
                    // and point (0,0,0). It is not a contact at the court origin.
                    // Resolve a local surface before classifying ground versus wall.
                    var meshCollider=hit.collider as MeshCollider;
                    if(meshCollider==null||meshCollider.convex)
                    {
                        var point=hit.collider.ClosestPoint(previousPos);
                        var outward=previousPos-point;
                        if(outward.sqrMagnitude>.000001f)
                        {
                            contact.point=point;contact.normal=outward.normalized;
                            if(Vector3.Dot(contact.normal,disp)>=0)continue;
                        }
                        else
                        {
                            // A deeply embedded launch stays local and rebounds out.
                            contact.point=previousPos;
                        }
                    }
                    else if(hit.collider.Raycast(new Ray(previousPos,disp.normalized),out var local,dist+Balance.SlipperHitRadius))
                        contact=local;
                    else continue;
                }

                // Vertical / obstacle hits (ground hits are handled by Land)
                if (Vector3.Dot(contact.normal, Vector3.up) > 0.6f) continue;

                if (contact.distance < closestDist)
                {
                    closestDist = contact.distance;
                    closest = contact;
                    hitFound = true;
                }
            }

            if (!hitFound) return;

            float restitution = Mathf.Abs(PektusSpin) >= Balance.PektusBankSpinThreshold
                                && _bankCount == 0
                ? Balance.PektusBankRestitution
                : Balance.BounceRestitution;

            Vector3 normal = closest.normal;
            normal.y = 0.0f;
            if (normal.sqrMagnitude > 0.001f) normal.Normalize();
            else normal = -disp.normalized;

            _velocity = Vector3.Reflect(_velocity, normal) * restitution;
            transform.position = closest.point + normal * (Balance.SlipperHitRadius + 0.02f);

            _bankCount++;
            NetCue.PlayVaried("slipper_land", transform.position, 0.88f, 1.08f, 0.85f);

            if (_bankCount == 1 && Mathf.Abs(PektusSpin) >= Balance.PektusBankSpinThreshold)
            {
                // ⚠️ RELAYED. `FixedUpdate` is host-gated, so the popup and the style award were
                // drawn on one screen. See `Visual.MatchFlair`.
                Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.BankShot,
                                           _throwerSlot, -1, transform.position);
            }

            if (_bankCount > Balance.MaxScoringBanks)
            {
                FinishChain(ThrowChainEnd.Miss);
                _throwerSlot = -1;
            }
        }

        /// <summary>
        /// One axis of the wall the tsinelas is allowed to reach, margin included.
        ///
        /// ⚠️ IT IS SHARED BY THE BOUNCE, THE RESTING PLACE AND THE REPLICA so the three cannot
        /// drift. All three had the same two lines written out separately, which is how the
        /// replica came to be missing them entirely.
        /// </summary>
        private static float ClampToPlayableAxis(float value, float half)
        {
            float limit = half - Balance.SlipperHitRadius;
            return limit > 0.0f ? Mathf.Clamp(value, -limit, limit) : value;
        }

        private void BounceOffBounds()
        {
            float limitX = AIController.PlayableHalfX - Balance.SlipperHitRadius;
            float limitZ = AIController.PlayableHalfZ - Balance.SlipperHitRadius;

            Vector3 p = transform.position;
            bool bounced = false;
            float restitution = Mathf.Abs(PektusSpin) >= Balance.PektusBankSpinThreshold
                                && _bankCount == 0
                ? Balance.PektusBankRestitution
                : Balance.BounceRestitution;

            if (limitX > 0.0f && Mathf.Abs(p.x) > limitX)
            {
                p.x = Mathf.Sign(p.x) * limitX;
                _velocity.x = -Mathf.Sign(p.x) * Mathf.Abs(_velocity.x) * restitution;
                bounced = true;
            }

            if (limitZ > 0.0f && Mathf.Abs(p.z) > limitZ)
            {
                p.z = Mathf.Sign(p.z) * limitZ;
                _velocity.z = -Mathf.Sign(p.z) * Mathf.Abs(_velocity.z) * restitution;
                bounced = true;
            }

            // ⚠️⚠️ AND THE SKY IS A WALL TOO, WHICH IT WAS NOT UNTIL 2026-08-29. 🧑: *"make sure
            // theres invisible bounds in the sky as well as those walls that return the slippers
            // or make them bounce"*. X and Z were walled and Y was open, so a hard throw aimed
            // high went over the top of a 6 m wall, and the RESTING clamp then dragged it back to
            // an edge it had never touched: the shoe teleported to a wall from open air, which
            // reads as the throw being eaten. A tsinelas nobody can retrieve is an attacker
            // deleted from the round.
            //
            // ⚠️ IT IS A CEILING AND NOT A FLOOR. Nothing here touches downward travel: the
            // ground, the kill plane and the resting height own that, and reflecting upward off
            // a low Y would turn every landing into a bounce.
            float ceiling = AIController.PlayableCeilingY - Balance.SlipperHitRadius;

            if (p.y > ceiling)
            {
                p.y = ceiling;
                _velocity.y = -Mathf.Abs(_velocity.y) * restitution;
                bounced = true;
            }

            transform.position = p;

            if (!bounced) return;

            _bankCount++;
            NetCue.PlayVaried("slipper_land", transform.position, 0.88f, 1.08f, 0.85f);

            if (_bankCount == 1 && Mathf.Abs(PektusSpin) >= Balance.PektusBankSpinThreshold)
            {
                // ⚠️ RELAYED. `FixedUpdate` is host-gated, so the popup and the style award were
                // drawn on one screen. See `Visual.MatchFlair`.
                Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.BankShot,
                                           _throwerSlot, -1, transform.position);
            }

            // One authored bank can still score. Further wall contacts remain valid
            // physics but lose player credit, preventing pinball loops from farming cans.
            if (_bankCount > Balance.MaxScoringBanks)
            {
                FinishChain(ThrowChainEnd.Miss);
                _throwerSlot = -1;
            }
        }

        /// <summary>
        /// ⚠️ SPIN AND TUMBLE AT ONCE. A real thrown slipper does both; doing only the spin is
        /// what made an earlier version read as "flying perfectly flat".
        ///
        /// ⚠️ AND IT ROTATES ABOUT THE MESH CENTRE. With the origin at the sole, a thrown
        /// slipper orbits its own underside instead of spinning in place.
        /// </summary>
        private void SpinInFlight(float dt)
        {
            transform.Rotate(Vector3.up, Balance.SlipperSpinSpeedDeg * dt, Space.Self);
            transform.Rotate(Vector3.right, Balance.SlipperTumbleSpeedDeg * dt, Space.Self);
        }

        /// <summary>
        /// ⚠️⚠️ A BLOCK COSTS THE TAYA POSITION, AND THAT IS THE POINT. Body-blocking is the
        /// taya's entire passive verb, and until it was fixed the only thing it produced was a
        /// sound at a world position: no flash, no recoil, nothing on the blocker's own
        /// screen. A verb with no feedback is a verb the player cannot tell they performed.
        ///
        /// ⚠️ A PUSH AND NOT A STUN, AND THAT WAS A DELIBERATE REVERSAL. A stagger was the
        /// obvious way to make blocking cost something and it is wrong: three attackers
        /// throwing at one box would chain stuns onto the defender, and Max() bounds the
        /// DURATION of one stun without bounding how often the next one starts. Knockback
        /// costs position, which is what a block is actually about, and cannot lock anybody
        /// out of the game.
        /// </summary>
        private void HostBlockedBy(CharacterMotor blocker)
        {
            FinishChain(ThrowChainEnd.Block);
            // A body block is a hit too — it is the taya's entire passive verb.
            //
            // ⚠️⚠️ AND IT WAS DRAWN ON ONE SCREEN. `FixedUpdate` is host-gated, so the flash, the
            // burst and the squash reached the host and nobody else, which for the taya's only
            // unpressed verb means three players out of four blocked a throw with no sign that
            // anything happened. See `Visual.MatchFlair`.
            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.Block,
                                       _throwerSlot, blocker.PlayerSlot, transform.position,
                                       _velocity.magnitude);

            // ⚠️⚠️ AND IT MAKES A SOUND, WHICH IT DID NOT. `slipper.gd:1170` plays `hit_body` on
            // exactly this path. This function's own header says a verb with no feedback is a
            // verb the player cannot tell they performed, and it then gave the block a flash and
            // a burst and left it silent — so the one thing a taya can do without pressing
            // anything was the one thing they could not hear.
            // ⚠️⚠️ THROUGH `NetCue`: `FixedUpdate` OPENS WITH `ShouldResolve()`, SO EVERY SOUND
            // BELOW IT WAS HOST-ONLY. 🧑 2026-08-29: *"non hosts dont have sfx in some plarts"*.
            // `tools/audit_audio_reach.py` could not see this class of fault at all, because it
            // looks for a gate at the same brace depth and this one is two frames up the stack.
            // See `docs/TODO.md` § 83.12.
            NetCue.PlayImpact("hit_body", "guard_block", transform.position, 0.72f);

            float speed = Combat.BlockKnockbackSpeed(_skinIndex, blocker.CharacterIndex, blocker.Mode);
            Vector3 along = _velocity;
            along.y = 0.0f;
            blocker.ApplyImpulse(along.normalized * speed);

            // ⚠️ AWAY FROM THE BLOCKER, NOT MIRRORED. A true reflection sends it wherever the
            // incoming angle points, which is as often as not deeper into the box.
            Vector3 away = blocker.transform.position - transform.position;
            away.y = 0.0f;
            Deflect(-away.normalized * Balance.LaunchSpeed * Balance.DeflectSpeedScale, 1.0f);
        }

        public void Deflect(Vector3 horizontal, float liftScale)
        {
            FinishChain(ThrowChainEnd.Block);
            _velocity = horizontal;
            _velocity.y = Balance.DeflectLift * liftScale;
            _flightTime = 0.0f;
            _throwerSlot = -1; // a deflected slipper credits nobody
        }

        /// <summary>
        /// The height of the floor under a point, or 0 when nothing is there.
        ///
        /// ⚠️⚠️ NEITHER ARENA'S FLOOR IS AT y = 0, AND EVERY PLACE THIS PORT ASSUMED IT WAS PUT A
        /// SLIPPER IN THE AIR. Eskinita's asphalt is a slab with its own thickness and Bayan
        /// Plaza's paving is another; a rest height added to world zero is only correct by
        /// accident. Cast from well above and take the first hit, which is what "lying on the
        /// ground" means whatever the map is built out of.
        ///
        /// ⚠️ TRIGGERS ARE IGNORED. The confinement chalk, the hazard discs and the kill plane
        /// are all colliders a downward ray would otherwise stop on, and a slipper resting on
        /// the chalk's trigger volume floats by exactly its thickness.
        ///
        /// ⚠️ AND A MISS RETURNS ZERO RATHER THAN LEAVING THE SLIPPER WHERE IT WAS. Off the edge
        /// of the map there is no floor to rest on, and `VoidY` already owns that case: a
        /// slipper below the world is recovered to its spawn, not balanced on nothing.
        /// </summary>
        public static float GroundY(Vector3 at)
            => FindGroundY(at, 6.0f);

        private static float FindGroundY(Vector3 at, float scanAbove)
        {
            var from = new Vector3(at.x, at.y + scanAbove, at.z);

            var hits = Physics.RaycastAll(from, Vector3.down, 40.0f, ~0,
                                          QueryTriggerInteraction.Ignore);

            float best = float.NegativeInfinity;

            foreach (var hit in hits)
            {
                // ⚠️⚠️ A BODY IS NOT THE GROUND, AND SKIPPING THIS PUT SLIPPERS ON PEOPLE'S
                // HEADS. Every slipper starts at its owner's FEET, so the first thing a downward
                // cast from above that mark meets is the owner's own capsule — and the slipper
                // was then placed 2 m up, out of its own pickup radius. `AnyAttackerCanPickUpAny
                // Slipper` caught it immediately: the attacker was standing on a grabbable
                // tsinelas that was actually floating over their head.
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                if (hit.collider.GetComponentInParent<Slipper>() != null) continue;
                if (hit.collider.GetComponentInParent<Lata>() != null) continue;

                if (hit.point.y > best) best = hit.point.y;
            }

            if(RooftopPool.TrySurface(at,out float water))best=Mathf.Max(best,water);
            return float.IsNegativeInfinity(best) ? 0.0f : best;
        }

        /// <param name="fromFlight">
        /// ⚠️⚠️ TRUE ONLY WHERE A FLIGHT ACTUALLY ENDED ON THE GROUND, and it carries two
        /// consequences rather than one: the landing thud, and § THE LANDED HIGHLIGHT. Both
        /// want it true in exactly the same single place.
        ///
        /// It is a parameter rather than a line inside this function because the same function
        /// is reached by the void recovery and by the flight timing out, and those are
        /// teleports home rather than landings. Godot's note names the symptom from the audio
        /// side: putting the sound in unconditionally played a triple thud at the start of
        /// every round, when three slippers are returned to their marks on one frame.
        /// </param>
        private void Land(bool fromFlight, float? landingGround = null)
        {
            SetState(SlipperState.Loose);
            _velocity = Vector3.zero;

            // ⚠️ THE TUMBLE IS CLEARED ON LANDING. `_apply_landed` sets `rotation = Vector3.ZERO`
            // on both the body and its Visual, and skipping it leaves the slipper resting at
            // whatever angle the spin happened to stop at — standing on its edge, or on its toe,
            // or half inside the road. It is also the pose the pickup prompt is judged against.
            transform.rotation = Quaternion.identity;

            var visual = transform.Find("Visual");
            if (visual != null) visual.localRotation = Quaternion.identity;

            // ⚠️ THE SKIN'S OWN REST HEIGHT, NOT A LITERAL. The four skins rest between 0.034
            // and 0.161 off the ground, so one number leaves the tall one buried.
            //
            // ⚠️⚠️ AND IT IS MEASURED FROM THE GROUND UNDERNEATH, NOT FROM WORLD ZERO. This read
            // `Mathf.Max(p.y, RestHeight)`, which is only the ground when the ground happens to
            // be at y = 0 — and neither arena's floor is. 🧑 2026-08-16, on a Bayan Plaza
            // capture: *"also ur slippers are floating"*. The .gd's own note is about the mesh
            // ORIGIN sitting at the volume centroid, so "resting on the floor is half a slipper
            // up" — half a slipper up FROM THE FLOOR, which is what has to be found first.
            Vector3 p = transform.position;
            float rest = (landingGround ?? GroundY(p)) + RestHeight;

            // ⚠️⚠️ A SLIPPER THAT COMES TO REST OUT OF REACH DELETES AN ATTACKER FROM THE ROUND,
            // AND IT IS NOT RARE. `GroundY` casts down from six metres up and takes the HIGHEST
            // surface it meets, which is exactly right for a raised slab and exactly wrong for a
            // roof, an awning, a market stall or a hero hazard: the flight ends ON TOP of it and
            // the tsinelas stays there for the rest of the round.
            //
            // ⚠️ MEASURED, NOT IMAGINED. `AiDiagnosticProbe` on 2026-08-23 caught two of the
            // four tsinelas ending a Hero Strike round frozen at y = 3.46 and y = 3.39, both
            // reading `grabbable=False`, with their owners standing directly underneath them in
            // FETCH for 28 of the 30 seconds sampled. The whole-match probe then showed what
            // that costs: 3 throws, 1 knockdown and 679 unretrieved-slipper penalties, because
            // the anti-stall rule keeps charging an attacker who has nothing it can reach. It is
            // most of what "ai is broken af in every game mode" actually was. The bots were
            // playing correctly against a board that had taken their pieces away.
            //
            // ⚠️ IT RETURNS TO THE OWNER RATHER THAN DROPPING STRAIGHT DOWN. Dropping would put
            // it inside whatever it landed on top of, and the void branch above already declares
            // the intended rule for ammunition that leaves play: give it back rather than let
            // the round quietly lose a piece of itself.
            if (rest > ReachablePlaneY() + Balance.SlipperMaxRestReach)
            {
                p = OwnerMark();
                rest = GroundY(p) + RestHeight;

                // A recovery is not a landing: no thud, and no landed highlight pointing at a
                // place the throw never reached.
                fromFlight = false;
            }

            // ⚠⚠ AND THE RESTING PLACE IS WALLED, NOT ONLY THE FLIGHT. `BounceOffBounds`
            // runs inside `FixedUpdate` and that returns immediately unless the state is
            // InFlight, so every path that puts a slipper DOWN was outside the arena walls by
            // construction: a landing at the very edge, a deflection resolved on the last frame
            // of flight, or the owner-mark recovery above. `BotBehaviourProbe` caught one at
            // x = 9.28 against a playable half width of 8.6, which is ammunition sitting
            // somewhere no attacker is allowed to walk to, and therefore an attacker deleted
            // from the round exactly as the note above describes.
            //
            // ⚠️ THE SAME LIMITS THE BOUNCE USES, so a slipper cannot come to rest anywhere a
            // flight would have been turned back from.
            float restLimitX = AIController.PlayableHalfX - Balance.SlipperHitRadius;
            float restLimitZ = AIController.PlayableHalfZ - Balance.SlipperHitRadius;

            if (restLimitX > 0.0f) p.x = Mathf.Clamp(p.x, -restLimitX, restLimitX);
            if (restLimitZ > 0.0f) p.z = Mathf.Clamp(p.z, -restLimitZ, restLimitZ);

            transform.position = new Vector3(p.x, rest, p.z);

            // ⚠️ AND IT MAKES A SOUND. A throw that hit a body played one cue and a throw that
            // hit the can played another, but a throw that simply MISSED, 38 of 71 flights in
            // the baseline, landed in silence. The most common outcome was the one the game
            // said nothing about.
            //
            // ⚠️ GATED ON `fromFlight` NOW. It used to fire on every route into this function,
            // including the round reset that teleports three slippers home on the same frame.
            if (fromFlight)
            {
                TriggerAffinityImpact();
                NetCue.PlayVaried("slipper_land", transform.position, 0.88f, 1.08f, 0.88f);
            }

            // § THE LANDED HIGHLIGHT. Written AFTER the state above, never before: SetState
            // clears the flag on any move out of Loose, so setting it first would be undone by
            // the very transition that brought us here.
            SetLandedHighlight(fromFlight);
        }

        /// <summary>
        /// The height a body's feet are actually at, which is what a pickup is measured from.
        /// `CanBeGrabbedBy` compares against `who.transform.position`, so this is the plane a
        /// reachable slipper has to be near.
        /// </summary>
        private float ReachablePlaneY()
        {
            var round = GameServices.Round;
            if (round == null) return 0.0f;

            var owner = _ownerSlot >= 0 ? round.PlayerAt(_ownerSlot) : null;
            if (owner != null) return owner.transform.position.y;

            return round.Lata != null ? round.Lata.transform.position.y : 0.0f;
        }

        /// <summary>
        /// Where an unreachable slipper is given back. The owner's own feet when there is an
        /// owner standing somewhere, and the seat's mark on the attacker ring when there is
        /// not, which is the position `SliceRunner.ResetWorld` would have chosen.
        /// </summary>
        private Vector3 OwnerMark()
        {
            var round = GameServices.Round;
            var owner = round != null && _ownerSlot >= 0 ? round.PlayerAt(_ownerSlot) : null;
            if (owner != null) return owner.transform.position;

            float ring = Confinement.AttackerSpawnRing();
            float bearing = Mathf.Max(0, _ownerSlot) * Mathf.PI * 0.5f;
            return new Vector3(Mathf.Sin(bearing) * ring, 0.0f, Mathf.Cos(bearing) * ring);
        }

        /// <summary>
        /// ⚠️⚠️ THE SWEEP EXISTS BECAUSE LANDING IS NOT THE ONLY WAY TO END UP STRANDED. A
        /// tsinelas can land legitimately on a hero hazard that is still standing, pass the
        /// check in `Land`, and be left floating when the hazard expires underneath it a few
        /// seconds later. Nothing runs on that slipper afterwards: `FixedUpdate` returns on the
        /// first line while it is Loose, so there is no path back into `Land` at all and the
        /// piece is gone for the rest of the round.
        ///
        /// ⚠️ HALF A SECOND, NOT EVERY FRAME. The check is a raycast per slipper and the fault
        /// it catches is measured in whole seconds, so polling it on the frame buys nothing.
        /// </summary>
        private float _reachSweepAccum;

        private void Update()
        {
            if (State != SlipperState.Loose) return;
            if (!NetAuthority.ShouldResolve()) return;

            _reachSweepAccum += Time.deltaTime;
            if (_reachSweepAccum < 0.5f) return;
            _reachSweepAccum = 0.0f;

            if (transform.position.y <= ReachablePlaneY() + Balance.SlipperMaxRestReach) return;

            Vector3 mark = OwnerMark();
            transform.position = new Vector3(mark.x, GroundY(mark) + RestHeight, mark.z);
            SetLandedHighlight(false);
        }
    }
}
