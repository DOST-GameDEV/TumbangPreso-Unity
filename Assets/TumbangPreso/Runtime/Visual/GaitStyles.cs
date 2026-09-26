using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ EVERY BODY'S WALK AND RUN, WRITTEN ONE AT A TIME FROM WHO THEY ARE. NEVER ONE FORMULA FOR THE CAST.
    ///
    /// Owner, 2026-09-27: *"do it one by oen dont generate the same one for all"*, *"manually do it for each character"*,
    /// *"think of their personalities and shti and how it will show up in walking"*, and the standing rule that came out of
    /// it (CLAUDE.md section 0: never stamp one change across the whole cast). Each entry below starts from the character's
    /// lore (`docs/CHARACTER_ORIGINS.md`, `docs/BADJAO_EXPANSION.md` for Rafi) and says, in a sentence, how that person moves
    /// and why; then the walk, the run, and whatever only they do. When an entry is changed, film THAT body
    /// (`WalkArmsProbe`, `TUMP_WALK_BODIES=<id>`) and look at it; a change to one entry is never copied into the others.
    ///
    /// ⚠️ THE ARM SPREADS ARE MEASURED PER MODEL (bind pose, mesh units, 2026-09-27). The shared Kenney-style body pivots its
    /// arm 0.100 out, the arm block is 0.156 thick, and the hips are 0.158 wide at the height the fist hangs, so an arm
    /// straight down lays its fist 13 mesh cm inside the hips and one at 27 degrees just clears them. That is why this rig
    /// was designed standing in an A. Sean's torso is 0.198 wide top to bottom (a slab of muscle), Phaister's robe 0.220 to
    /// 0.235, Paete's arms 0.482 long from a 0.150 pivot, Nemu's sleeve hangs 0.150 below a 0.196 arm. Each spread below is
    /// chosen against its own body, and where the fist still brushes the hips at the moment it passes them, that is accepted
    /// rather than bought by sliding the shoulder off the torso (see `Gait.ArmSpread`).
    /// </summary>
    public static class GaitStyles
    {
        /// <summary>The style for a model instance (its name is the glb's, plus "(Clone)").</summary>
        public static GaitStyle For(string modelName)
        {
            string name = string.IsNullOrEmpty(modelName) ? "" : modelName.Replace("(Clone)", "").Trim();
            switch (name)
            {
                case "team-sean": return Sean;
                case "team-zack": return Zack;
                case "team-dante": return Dante;
                case "team-cheska": return Cheska;
                case "team-nemu": return Nemu;
                case "team-phaister": return Phaister;
                case "team-rafi": return Rafi;
                case "team-amihan": return Amihan;
                case "team-paete": return Paete;
                case "team-custom": case "team-custom-base": return Custom;
                case "team-bayan": case "character-male-f": return Bayan;
                case "character-female-f": return Maring;
                case "character-male-a": return Totoy;
                case "team-inday": case "character-female-a": return Inday;
                case "character-male-b": return KuyaBoy;
                case "character-female-b": return AteGirlie;
                case "character-male-c": return Tikboy;
                case "character-female-c": return Bebang;
                case "character-male-d": return JunJun;
                case "character-female-d": return LolaPacing;
                case "character-male-e": return MangKanor;
                case "character-female-e": return AlingNena;
                case "team-iggy": return Iggy;
                default: return Custom;
            }
        }

        private static float Wave(float time, float hz) => Mathf.Sin(2f * Mathf.PI * hz * time);

        // =====================================================================================================================
        // HERO STRIKE
        // =====================================================================================================================

        /// <summary>
        /// SEAN. *"Waits for one opening. Makes it count."* Patient, particular, the strongest body in the cast; Zack calls him
        /// slow. So he walks like a man who has already decided: upright and square, chest out, every step put down with its
        /// weight and no hurry, the big arms held off his lats because a torso that wide will not let them hang close, and the
        /// shoulders doing the driving. The head does not move; he is watching. The run is a fighter closing distance: fists
        /// carried up in front, shoulders punching, heavy on the ground.
        /// ⚠️ 2026-09-27, owner: *"make sean look more muscular by moving his arms out"*. His shoulders now sit 5.5 cm further
        /// out in the model (`tools/reshape_hero_arms.py`), on the sides of the chest, and his arms are 0.225 long instead of
        /// 0.293, so the lats carry the arms off the body at 22 degrees with the chest up (leaning back 1.5), not a long arm
        /// splayed to clear a wide torso.
        /// </summary>
        public static readonly GaitStyle Sean = new GaitStyle
        {
            Name = "sean",
            Walk = new Gait
            {
                LegForward = 36, LegBack = 32, LegSnap = .8f, Stance = 3,
                ArmSpread = 22, ArmForward = 24, ArmBack = 16, ArmCarry = 3, ArmSnap = 1, ArmLag = 0.095f,
                Lean = -1.5f, LeanPulse = 1.2f, Roll = 2.5f, Twist = 9,
                HeadPitch = -2, HeadSteady = .95f, Sway = .03f, Stomp = .02f, Glide = 1.1f,
            },
            Run = new Gait
            {
                LegForward = 46, LegBack = 42, LegSnap = .9f, Stance = 3,
                ArmSpread = 18, ArmForward = 38, ArmBack = 30, ArmCarry = 26, ArmSnap = .8f, ArmLag = 0.057f,
                Lean = 11, LeanPulse = 1.5f, Roll = 1, Twist = 13,
                HeadPitch = -4, HeadSteady = .75f, Bounce = .04f, BounceDelay = .06f, Sway = .01f, Stomp = .03f, Glide = 1.1f,
            },
            // The shoulder drive: the chest turns sharply into each step and holds, rather than rocking evenly.
            Quirk = (ref GaitPose p, in GaitMoment m) =>
            {
                float t = p.TorsoYaw / Mathf.Max(.01f, m.Gait.Twist);
                p.TorsoYaw = Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(t), .55f) * m.Gait.Twist;
                p.HeadYaw = -p.TorsoYaw * m.Gait.HeadSteady;
            },
        };

        /// <summary>
        /// ZACK. *"Finds the angle before you see the opening."* Makes difficult plays look casual and casual plays unnecessarily
        /// difficult. The walk is a swagger: leaning back a touch, head cocked and bobbing on every step like there is music,
        /// hips swinging, one arm doing most of the swinging while the other barely bothers, loose and a little late. Then the
        /// run flips it: electric, quick feet, snapping arms, the fastest-looking run in the cast even when it is not.
        /// ⚠️ 2026-09-27: arms 0.185 long (`tools/reshape_hero_arms.py`) and carried at 24 degrees, because his sleeves are the
        /// same yellow as his jacket and at 18 an arm sank into the body in the film.
        /// </summary>
        public static readonly GaitStyle Zack = new GaitStyle
        {
            Name = "zack",
            Walk = new Gait
            {
                LegForward = 42, LegBack = 36, LegSnap = 1.15f, Stance = 2,
                ArmSpread = 24, ArmForward = 30, ArmBack = 24, ArmCarry = -2, ArmSnap = 1, ArmLag = 0.12f, ArmFavour = .45f,
                Lean = -2.5f, Roll = 4.5f, Twist = 6,
                HeadPitch = -1, HeadTilt = 5, HeadNod = 3.5f, HeadSteady = .3f,
                Bounce = .045f, BounceDelay = .04f, Sway = .05f, Glide = 1.3f,
            },
            Run = new Gait
            {
                LegForward = 54, LegBack = 50, LegSnap = 1.3f, Stance = 1,
                ArmSpread = 20, ArmForward = 62, ArmBack = 46, ArmCarry = 12, ArmSnap = 1.3f, ArmLag = 0.038f,
                Lean = 13, Roll = 1, Twist = 8,
                HeadPitch = -3, HeadTilt = 2, HeadNod = 1, HeadSteady = .7f,
                Bounce = .06f, BounceDelay = .05f, Sway = .01f, Glide = 1.3f,
            },
        };

        /// <summary>
        /// DANTE. *"Holds the difficult space. Refuses to be rushed."* Stone armour, a mountain's stubbornness, the one trusted to
        /// keep a crowded game fair. He is the heaviest walker: a wide stance, long unhurried steps that land and stay landed
        /// (a stomp on each), the whole body rocking from foot to foot like a boulder being walked, arms in their stone held
        /// away from the body and barely swinging, chin down under the horn with a stare. The run is a bull's charge: head and
        /// horn first, low, still stomping.
        /// </summary>
        public static readonly GaitStyle Dante = new GaitStyle
        {
            Name = "dante",
            Walk = new Gait
            {
                LegForward = 40, LegBack = 36, LegSnap = .65f, Stance = 6,
                ArmSpread = 22, ArmForward = 16, ArmBack = 12, ArmCarry = 2, ArmSnap = .8f, ArmLag = 0.12f,
                Lean = 3, LeanPulse = 1.8f, Roll = 5.5f, RollDelay = .04f, Twist = 3,
                HeadPitch = 4, HeadSteady = .5f, Sway = .075f, Stomp = .05f, Glide = 1.35f,
            },
            Run = new Gait
            {
                LegForward = 50, LegBack = 44, LegSnap = .75f, Stance = 5,
                ArmSpread = 24, ArmForward = 34, ArmBack = 26, ArmCarry = 6, ArmSnap = .9f, ArmLag = 0.12f,
                Lean = 16, LeanPulse = 2, Roll = 3, Twist = 4,
                HeadPitch = 5, HeadSteady = .35f, Bounce = .01f, Sway = .04f, Stomp = .05f, Glide = 1.4f,
            },
        };

        /// <summary>
        /// CHESKA. *"Reads the space. Leaves you the harder route."* Quiet, practical, walks the court's edges testing the footing.
        /// Her walk is the tidiest in the cast: feet on one line, small exact arm swing, upright and level, no bounce, and the
        /// head slowly turning to take in the court because that is what she is always doing. The run belongs to the ice: a
        /// speed skater's long smooth stride, shoulders turning into it, almost no rise and fall.
        /// </summary>
        public static readonly GaitStyle Cheska = new GaitStyle
        {
            Name = "cheska",
            Walk = new Gait
            {
                LegForward = 42, LegBack = 38, LegSnap = 1, Stance = -1,
                ArmSpread = 16, ArmForward = 22, ArmBack = 16, ArmCarry = 2, ArmSnap = 1, ArmLag = 0.095f,
                Lean = 2, Roll = 2, Twist = 4,
                HeadPitch = 0, HeadSteady = 1, Bounce = .012f, Sway = .02f, Glide = 1.3f,
            },
            Run = new Gait
            {
                LegForward = 56, LegBack = 54, LegSnap = .9f, Stance = 0,
                ArmSpread = 14, ArmForward = 50, ArmBack = 42, ArmCarry = 4, ArmSnap = 1, ArmLag = 0.095f,
                Lean = 15, Roll = 1.5f, Twist = 11,
                HeadPitch = -3, HeadSteady = 1, Bounce = .008f, Sway = .015f, Glide = 1.35f,
            },
            // Reading the court: a slow look to one side and the other while she walks, gone when she runs.
            Quirk = (ref GaitPose p, in GaitMoment m) => p.HeadYaw += 9f * Wave(m.Time, .22f) * (1f - m.Run),
        };

        /// <summary>
        /// NEMU. *"Looks distracted. Already knows your next move."* Takes the long way home, seems to miss half the conversation,
        /// Kuro a shadow at her shoulder. ⚠️ SECOND PASS, 2026-09-27 (owner: *"nemu walks so awkward wtf"*). The first pass tried
        /// "dreamy" with a head cocked 7 degrees and wandering on its own clock, short shuffling steps and sleeves trailing: on a
        /// head that is half her height the tilt read as a broken neck, and her dark sleeves vanished into her dark coat. Now she
        /// walks like someone who has already read the play: hands held behind her back (the arms swept back and still, which
        /// also puts them where the silhouette shows them), level and unhurried, even steps with no bounce, a small slow look to
        /// one side now and then. The run keeps the same idea at speed: low, arms straight back, head up, gone.
        /// </summary>
        public static readonly GaitStyle Nemu = new GaitStyle
        {
            Name = "nemu",
            Walk = new Gait
            {
                LegForward = 38, LegBack = 34, LegSnap = 1, Stance = -1,
                ArmSpread = 12, ArmForward = 3, ArmBack = 3, ArmCarry = -24, ArmSnap = 1, ArmLag = .1f,
                Lean = 4, Roll = 2, Twist = 3,
                HeadPitch = -1, HeadTilt = 2, HeadSteady = .9f, Sway = .02f, Glide = 1.4f,
            },
            Run = new Gait
            {
                LegForward = 52, LegBack = 50, LegSnap = 1.2f, Stance = 0,
                ArmSpread = 12, ArmForward = 5, ArmBack = 5, ArmCarry = -58, ArmSnap = 1, ArmLag = .08f,
                Lean = 20, Roll = 1, Twist = 2,
                HeadPitch = -12, HeadSteady = .3f, Bounce = .02f, Sway = .01f, Glide = 1.45f,
            },
            // Watching the court without turning to it: a slow small look to one side and back, walking only.
            Quirk = (ref GaitPose p, in GaitMoment m) => p.HeadYaw += 6f * Wave(m.Time, .17f) * (1f - m.Run),
        };

        /// <summary>
        /// PHAISTER. *"Sets the stage. Lets you discover the trick."* A stage magician who enjoys the setup more than the win.
        /// She struts: one foot placed in front of the other across the centre line, hips swinging, chin up, arms swinging
        /// mostly in front of her with a flourish (and wide enough to swing past the robe). The run is an exit with a cape that
        /// is not there: arms flung out and back, a bounce in it, the hat leading.
        /// </summary>
        public static readonly GaitStyle Phaister = new GaitStyle
        {
            Name = "phaister",
            Walk = new Gait
            {
                LegForward = 42, LegBack = 34, LegSnap = 1.15f, Stance = -3,
                ArmSpread = 22, ArmForward = 32, ArmBack = 14, ArmCarry = 8, ArmSnap = 1.1f, ArmLag = 0.12f,
                Lean = -1.5f, Roll = 5, RollDelay = .05f, Twist = 7,
                HeadPitch = -5, HeadSteady = .5f, Bounce = .02f, Sway = .085f, Glide = 1.35f,
            },
            Run = new Gait
            {
                LegForward = 54, LegBack = 48, LegSnap = 1.1f, Stance = -1,
                ArmSpread = 34, ArmForward = 26, ArmBack = 34, ArmCarry = -20, ArmSnap = 1, ArmLag = 0.114f,
                Lean = 12, Roll = 2, Twist = 5,
                HeadPitch = -5, HeadSteady = .6f, Bounce = .07f, BounceDelay = .05f, Sway = .02f, Glide = 1.4f,
            },
            // A performer's head: it rides the hips a beat late, like she knows she is being watched.
            Quirk = (ref GaitPose p, in GaitMoment m) =>
                p.HeadRoll += 2.5f * -Mathf.Cos(2f * Mathf.PI * (m.Phase - .15f)) * (1f - m.Run),
        };

        /// <summary>
        /// RAFI. *"Draws you into the wrong current. Leaves with his slipper."* Grew up on a Sama Dilaut boat deck, relaxed, teasing,
        /// likes making a rival commit early. He walks with sea legs: a wide stance, the body rolling side to side a beat late
        /// like a deck under him, leaning back easy, and every few seconds a glance over his shoulder at whoever is chasing.
        /// ⚠️ SECOND PASS, 2026-09-27 (owner on the first film: *"walk of these 2 characters suck"*). His arms are the same
        /// tattooed skin as his chest, so hung 18 degrees off his sides they vanished into it and only the fists showed, bobbing
        /// at his hips like stubs. His arms are shorter now (0.185, `tools/reshape_hero_arms.py`) and hang well clear (28 degrees)
        /// so the silhouette carries them, loose and late, opening further out on the back swing (a quirk below).
        /// The run is water: long and low, arms loose and wide.
        /// </summary>
        public static readonly GaitStyle Rafi = new GaitStyle
        {
            Name = "rafi",
            Walk = new Gait
            {
                LegForward = 42, LegBack = 38, LegSnap = .9f, Stance = 6,
                ArmSpread = 28, ArmForward = 30, ArmBack = 26, ArmCarry = 2, ArmSnap = .85f, ArmLag = .11f,
                Lean = -2, Roll = 7, RollDelay = .12f, Twist = 6,
                HeadPitch = -1, HeadTilt = -3, HeadSteady = .75f, Bounce = .012f, Sway = .1f, Glide = 1.35f,
            },
            Run = new Gait
            {
                LegForward = 56, LegBack = 50, LegSnap = .9f, Stance = 4,
                ArmSpread = 26, ArmForward = 46, ArmBack = 40, ArmCarry = 2, ArmSnap = .9f, ArmLag = .1f,
                Lean = 13, Roll = 3.5f, RollDelay = .08f, Twist = 10,
                HeadPitch = -3, HeadSteady = .8f, Bounce = .025f, Sway = .045f, Glide = 1.4f,
            },
            Quirk = (ref GaitPose p, in GaitMoment m) =>
            {
                // Loose arms: they open out past the hip on the back swing. ⚠️ Not in across the belly on the forward swing, which
                // the first version did: against his tattooed chest the arm vanished again (film v11).
                p.SpreadLeft += .25f * Mathf.Max(0f, -p.ArmLeft); p.SpreadRight += .25f * Mathf.Max(0f, -p.ArmRight);
                // The tease: a look back over the shoulder about every five seconds, walking only.
                float t = Mathf.Repeat(m.Time, 5f);
                float look = t < .9f ? Mathf.Sin(Mathf.PI * t / .9f) : 0f;
                p.HeadYaw += 32f * look * look * (1f - m.Run);
            },
        };

        /// <summary>
        /// AMIHAN. *"Reads the wind. Gets there first."* Bright, proud, cannot stand a stalled game, always the one moving; the
        /// fastest hero. ⚠️ SECOND PASS, 2026-09-27 (owner: *"walk of these 2 characters suck"*). Her sleeves are the same cream as
        /// her open coat, so arms hung 17 degrees off her sides disappeared into the coat and only her fists showed, down by her
        /// knees, like a toddler dragging her hands. Now her arms are carried out clear of the coat (30 degrees) and swing high in
        /// front, and the walk SKIPS: every other step lifts higher than the one before, the uneven bounce of someone too
        /// impatient to just walk (a quirk below), head up and bobbing with it. Her run is the wind at her back: arms swept back
        /// and out like she is being pushed, a long floating stride with real air under it.
        /// </summary>
        public static readonly GaitStyle Amihan = new GaitStyle
        {
            Name = "amihan",
            Walk = new Gait
            {
                LegForward = 40, LegBack = 36, LegSnap = 1.2f, Stance = 0,
                ArmSpread = 30, ArmForward = 44, ArmBack = 26, ArmCarry = 6, ArmSnap = 1.1f, ArmLag = .06f,
                Lean = 6, Roll = 2.5f, Twist = 7,
                HeadPitch = -5, HeadNod = 2, HeadSteady = .6f, Bounce = .07f, BounceDelay = .05f, Sway = .02f, Glide = 1.2f,
            },
            Run = new Gait
            {
                LegForward = 60, LegBack = 58, LegSnap = 1.1f, Stance = 0,
                ArmSpread = 34, ArmForward = 14, ArmBack = 18, ArmCarry = -42, ArmSnap = 1, ArmLag = .08f,
                Lean = 18, Roll = 1, Twist = 5,
                HeadPitch = -9, HeadSteady = .7f, Bounce = .1f, BounceDelay = .08f, Sway = .01f, Glide = 1.35f,
            },
            // The skip: the rise after one foot lands higher than after the other, walking only.
            Quirk = (ref GaitPose p, in GaitMoment m) =>
                p.RootUp *= 1f + .7f * (1f - m.Run) * Mathf.Sin(2f * Mathf.PI * m.Phase),
        };

        /// <summary>
        /// PAETE. *"Never hurries. Always arrives."* The guardian tree of Makiling, a little amused by everyone else's panic.
        /// ⚠️ SECOND PASS, 2026-09-27 (owner: *"paete weird walking animation make his sit hage more weight"*). The first pass
        /// let his feet slide 20 per cent a step (`Glide` 1.2), which on a 1.84 m tree reads as floating, and his footfall
        /// was a 3 cm dip. Weight is built from three things here: the feet never slide (glide 1.02, so the cadence falls to
        /// about 2.7 steps a second at his size), every footfall SINKS him (a 7 per cent of leg drop, the chest pitching into
        /// it and the head nodding a beat later), and the impact runs up the body (the long arms jolt forward on each landing,
        /// a quirk below). Between steps he rocks well over the planted foot, the trunk barely twisting, the crown swaying
        /// late on its own. The run is the same weight at a trot: still planted, still sinking, longer strides.
        /// </summary>
        public static readonly GaitStyle Paete = new GaitStyle
        {
            Name = "paete",
            Walk = new Gait
            {
                LegForward = 38, LegBack = 34, LegSnap = .6f, Stance = 5,
                ArmSpread = 15, ArmForward = 18, ArmBack = 16, ArmCarry = 3, ArmSnap = .8f, ArmLag = .1f,
                Lean = 4, LeanPulse = 2.5f, Roll = 5.5f, RollDelay = .1f, Twist = 2,
                HeadPitch = 2, HeadNod = 3, HeadSteady = .4f, Sway = .075f, Stomp = .07f, Glide = 1.02f,
            },
            Run = new Gait
            {
                LegForward = 48, LegBack = 44, LegSnap = .7f, Stance = 5,
                ArmSpread = 16, ArmForward = 30, ArmBack = 28, ArmCarry = 4, ArmSnap = .85f, ArmLag = .09f,
                Lean = 9, LeanPulse = 3, Roll = 4, RollDelay = .08f, Twist = 3,
                HeadPitch = 1, HeadNod = 3, HeadSteady = .5f, Bounce = .02f, Sway = .05f, Stomp = .07f, Glide = 1.05f,
            },
            Quirk = (ref GaitPose p, in GaitMoment m) =>
            {
                // The impact running up the body: a sharp pulse just after each footfall (twice a cycle), felt in the arms.
                float after = Mathf.Repeat(2f * (m.Phase - .25f) - .06f, 1f); // 0 just after each contact (phase .25 and .75)
                float impact = Mathf.Exp(-after * 9f);
                p.ArmLeft += 7f * impact; p.ArmRight += 7f * impact;
                // The canopy: the head sways slowly on its own, the way a crown moves after the trunk.
                p.HeadRoll += 2.5f * Wave(m.Time, .45f);
            },
        };

        /// <summary>
        /// THE CUSTOM HERO. A player's own fighter, so no personality is assumed: an athletic, confident, even walk and a clean
        /// sprinter's run that sits under any face a player builds.
        /// </summary>
        public static readonly GaitStyle Custom = new GaitStyle
        {
            Name = "custom",
            Walk = new Gait
            {
                LegForward = 40, LegBack = 36, LegSnap = 1, Stance = 1,
                ArmSpread = 18, ArmForward = 28, ArmBack = 20, ArmCarry = 2, ArmSnap = 1, ArmLag = 0.114f,
                Lean = 3, Roll = 3, Twist = 5,
                HeadSteady = .7f, Bounce = .02f, Sway = .03f, Glide = 1.43f,
            },
            Run = new Gait
            {
                LegForward = 52, LegBack = 48, LegSnap = 1.05f, Stance = 1,
                ArmSpread = 16, ArmForward = 54, ArmBack = 40, ArmCarry = 10, ArmSnap = 1, ArmLag = 0.076f,
                Lean = 12, Roll = 1.5f, Twist = 8,
                HeadPitch = -2, HeadSteady = .8f, Bounce = .04f, BounceDelay = .05f, Sway = .015f, Glide = 1.44f,
            },
        };

        // =====================================================================================================================
        // CLASSIC: THE NEIGHBOURHOOD. Named for who they are in a Filipino street game; each walks like that person.
        // =====================================================================================================================

        /// <summary>BAYAN, the town's regular player: steady, fit, no nonsense. A clean athletic walk and a proper sprint.</summary>
        public static readonly GaitStyle Bayan = new GaitStyle
        {
            Name = "bayan",
            Walk = new Gait
            {
                LegForward = 42, LegBack = 38, LegSnap = 1, Stance = 2,
                ArmSpread = 18, ArmForward = 30, ArmBack = 22, ArmCarry = 2, ArmSnap = 1, ArmLag = 0.114f,
                Lean = 3, Roll = 3, Twist = 5, HeadSteady = .8f, Bounce = .02f, Sway = .03f, Glide = 1.37f,
            },
            Run = new Gait
            {
                LegForward = 54, LegBack = 50, LegSnap = 1, Stance = 1,
                ArmSpread = 16, ArmForward = 58, ArmBack = 42, ArmCarry = 12, ArmSnap = 1, ArmLag = 0.076f,
                Lean = 12, Roll = 1.5f, Twist = 8, HeadPitch = -2, HeadSteady = .8f, Bounce = .045f, BounceDelay = .05f, Sway = .015f, Glide = 1.4f,
            },
        };

        /// <summary>MARING, the cheerful one from the market: a brisk, bouncy, hip-swinging walk, and a skipping run.</summary>
        public static readonly GaitStyle Maring = new GaitStyle
        {
            Name = "maring",
            Walk = new Gait
            {
                LegForward = 38, LegBack = 34, LegSnap = 1.15f, Stance = -1,
                ArmSpread = 17, ArmForward = 28, ArmBack = 18, ArmCarry = 5, ArmSnap = 1, ArmLag = 0.114f,
                Lean = 2, Roll = 3.5f, Twist = 5, HeadTilt = 2, HeadNod = 1.5f, HeadSteady = .5f, Bounce = .035f, Sway = .05f, Glide = 1.39f,
            },
            Run = new Gait
            {
                LegForward = 50, LegBack = 46, LegSnap = 1.2f, Stance = 0,
                ArmSpread = 20, ArmForward = 44, ArmBack = 34, ArmCarry = 6, ArmSnap = 1, ArmLag = 0.095f,
                Lean = 9, Roll = 2, Twist = 6, HeadSteady = .6f, Bounce = .07f, BounceDelay = .06f, Sway = .02f, Glide = 1.39f,
            },
        };

        /// <summary>TOTOY, the little boy of the street: all energy. Bouncing, head bobbing, arms everywhere; the run flails.</summary>
        public static readonly GaitStyle Totoy = new GaitStyle
        {
            Name = "totoy",
            Walk = new Gait
            {
                LegForward = 40, LegBack = 38, LegSnap = 1.3f, Stance = 2,
                ArmSpread = 20, ArmForward = 40, ArmBack = 34, ArmCarry = 0, ArmSnap = 1.1f, ArmLag = 0.057f,
                Lean = 3, Roll = 3, Twist = 7, HeadNod = 4, HeadSteady = .3f, Bounce = .06f, Sway = .03f, Glide = 1.15f,
            },
            Run = new Gait
            {
                LegForward = 54, LegBack = 52, LegSnap = 1.3f, Stance = 3,
                ArmSpread = 26, ArmForward = 60, ArmBack = 56, ArmCarry = 4, ArmSnap = 1.2f, ArmLag = 0.038f,
                Lean = 10, Roll = 3, Twist = 10, HeadNod = 3, HeadSteady = .3f, Bounce = .08f, BounceDelay = .04f, Sway = .03f, Glide = 1.19f,
            },
        };

        /// <summary>INDAY, the playful girl: neat little skipping steps, a tilted head, a light bouncing run.</summary>
        public static readonly GaitStyle Inday = new GaitStyle
        {
            Name = "inday",
            Walk = new Gait
            {
                LegForward = 36, LegBack = 34, LegSnap = 1.2f, Stance = -2,
                ArmSpread = 16, ArmForward = 30, ArmBack = 24, ArmCarry = 3, ArmSnap = 1, ArmLag = 0.095f,
                Lean = 2, Roll = 2.5f, Twist = 4, HeadTilt = 4, HeadNod = 2, HeadSteady = .5f, Bounce = .05f, BounceDelay = .03f, Sway = .03f, Glide = 1.35f,
            },
            Run = new Gait
            {
                LegForward = 50, LegBack = 48, LegSnap = 1.2f, Stance = 0,
                ArmSpread = 18, ArmForward = 46, ArmBack = 40, ArmCarry = 6, ArmSnap = 1, ArmLag = 0.076f,
                Lean = 10, Roll = 1.5f, Twist = 6, HeadTilt = 2, HeadSteady = .6f, Bounce = .075f, BounceDelay = .05f, Sway = .015f, Glide = 1.37f,
            },
        };

        /// <summary>KUYA BOY, the cool big brother: laid back, leaning back a little, arms low and lazy, head cocked.</summary>
        public static readonly GaitStyle KuyaBoy = new GaitStyle
        {
            Name = "kuya_boy",
            Walk = new Gait
            {
                LegForward = 40, LegBack = 36, LegSnap = .95f, Stance = 3,
                ArmSpread = 18, ArmForward = 20, ArmBack = 18, ArmCarry = -2, ArmSnap = .9f, ArmLag = 0.12f,
                Lean = -1.5f, Roll = 4, RollDelay = .05f, Twist = 4, HeadTilt = -3, HeadSteady = .6f, Sway = .05f, Glide = 1.52f,
            },
            Run = new Gait
            {
                LegForward = 52, LegBack = 48, LegSnap = 1, Stance = 2,
                ArmSpread = 18, ArmForward = 48, ArmBack = 38, ArmCarry = 8, ArmSnap = 1, ArmLag = 0.114f,
                Lean = 10, Roll = 2, Twist = 7, HeadTilt = -1, HeadSteady = .7f, Bounce = .03f, Sway = .02f, Glide = 1.51f,
            },
        };

        /// <summary>ATE GIRLIE, the composed older sister: upright, chin up, feet placed neatly, small graceful arm swing.</summary>
        public static readonly GaitStyle AteGirlie = new GaitStyle
        {
            Name = "ate_girlie",
            Walk = new Gait
            {
                LegForward = 38, LegBack = 32, LegSnap = 1.1f, Stance = -2.5f,
                ArmSpread = 15, ArmForward = 20, ArmBack = 14, ArmCarry = 4, ArmSnap = 1, ArmLag = 0.12f,
                Lean = 0, Roll = 3.5f, RollDelay = .04f, Twist = 4, HeadPitch = -3, HeadSteady = .8f, Bounce = .01f, Sway = .06f, Glide = 1.5f,
            },
            Run = new Gait
            {
                LegForward = 48, LegBack = 44, LegSnap = 1.1f, Stance = -1,
                ArmSpread = 18, ArmForward = 40, ArmBack = 30, ArmCarry = 6, ArmSnap = 1, ArmLag = 0.095f,
                Lean = 9, Roll = 2, Twist = 5, HeadPitch = -3, HeadSteady = .8f, Bounce = .04f, Sway = .02f, Glide = 1.54f,
            },
        };

        /// <summary>TIKBOY, the mischief-maker: hunched, quick small steps, eyes up; he scampers when he runs.</summary>
        public static readonly GaitStyle Tikboy = new GaitStyle
        {
            Name = "tikboy",
            Walk = new Gait
            {
                LegForward = 34, LegBack = 34, LegSnap = 1.2f, Stance = 3,
                ArmSpread = 18, ArmForward = 24, ArmBack = 24, ArmCarry = 6, ArmSnap = 1.1f, ArmLag = 0.076f,
                Lean = 7, Roll = 2.5f, Twist = 5, HeadPitch = -6, HeadSteady = .5f, Bounce = .02f, Sway = .03f, Glide = 1.23f,
            },
            Run = new Gait
            {
                LegForward = 48, LegBack = 46, LegSnap = 1.3f, Stance = 3,
                ArmSpread = 20, ArmForward = 44, ArmBack = 40, ArmCarry = 6, ArmSnap = 1.2f, ArmLag = 0.057f,
                Lean = 15, Roll = 2, Twist = 7, HeadPitch = -8, HeadSteady = .6f, Bounce = .05f, BounceDelay = .03f, Sway = .02f, Glide = 1.25f,
            },
        };

        /// <summary>BEBANG, the tomboy who plays with the boys: strong, athletic, bouncing on the balls of her feet.</summary>
        public static readonly GaitStyle Bebang = new GaitStyle
        {
            Name = "bebang",
            Walk = new Gait
            {
                LegForward = 42, LegBack = 38, LegSnap = 1.1f, Stance = 2,
                ArmSpread = 18, ArmForward = 32, ArmBack = 26, ArmCarry = 3, ArmSnap = 1, ArmLag = 0.095f,
                Lean = 3, Roll = 3, Twist = 7, HeadSteady = .7f, Bounce = .03f, Sway = .035f, Glide = 1.3f,
            },
            Run = new Gait
            {
                LegForward = 56, LegBack = 52, LegSnap = 1.1f, Stance = 1,
                ArmSpread = 16, ArmForward = 58, ArmBack = 46, ArmCarry = 12, ArmSnap = 1.1f, ArmLag = 0.057f,
                Lean = 13, Roll = 1.5f, Twist = 9, HeadPitch = -2, HeadSteady = .8f, Bounce = .05f, BounceDelay = .05f, Sway = .015f, Glide = 1.34f,
            },
        };

        /// <summary>JUN-JUN, the sleepy kid dragged out to play: a shuffle, head down, arms hardly bothering; a reluctant jog.</summary>
        public static readonly GaitStyle JunJun = new GaitStyle
        {
            Name = "jun_jun",
            Walk = new Gait
            {
                LegForward = 36, LegBack = 34, LegSnap = .95f, Stance = 2,
                ArmSpread = 15, ArmForward = 12, ArmBack = 10, ArmCarry = 0, ArmSnap = .9f, ArmLag = 0.12f,
                Lean = 3, Roll = 3.5f, RollDelay = .08f, Twist = 2, HeadPitch = 6, HeadSteady = .3f, Sway = .04f, Glide = 1.63f,
            },
            Run = new Gait
            {
                LegForward = 52, LegBack = 48, LegSnap = 1, Stance = 2,
                ArmSpread = 18, ArmForward = 32, ArmBack = 28, ArmCarry = 2, ArmSnap = 1, ArmLag = 0.12f,
                Lean = 8, Roll = 2.5f, Twist = 4, HeadPitch = 3, HeadSteady = .4f, Bounce = .02f, Sway = .03f, Glide = 1.59f,
            },
        };

        /// <summary>
        /// LOLA PACING, the grandmother who still plays: small careful steps, stooped forward with the face held up, arms
        /// close and slightly forward for balance, no bounce at all. Her run is a determined, stooped hurry.
        /// </summary>
        public static readonly GaitStyle LolaPacing = new GaitStyle
        {
            Name = "lola_pacing",
            Walk = new Gait
            {
                LegForward = 30, LegBack = 28, LegSnap = .9f, Stance = 3,
                ArmSpread = 14, ArmForward = 10, ArmBack = 8, ArmCarry = 8, ArmSnap = 1, ArmLag = 0.12f,
                Lean = 11, Roll = 2, Twist = 2, HeadPitch = -9, HeadSteady = .3f, Sway = .035f, Glide = 1.53f,
            },
            Run = new Gait
            {
                LegForward = 44, LegBack = 40, LegSnap = 1, Stance = 3,
                ArmSpread = 16, ArmForward = 26, ArmBack = 20, ArmCarry = 12, ArmSnap = 1, ArmLag = 0.114f,
                Lean = 15, Roll = 2, Twist = 3, HeadPitch = -11, HeadSteady = .3f, Bounce = .01f, Sway = .03f, Glide = 1.54f,
            },
        };

        /// <summary>MANG KANOR, the neighbourhood tito: belly first, leaning back, wide and rolling, arms out; a huffing jog.</summary>
        public static readonly GaitStyle MangKanor = new GaitStyle
        {
            Name = "mang_kanor",
            Walk = new Gait
            {
                LegForward = 40, LegBack = 36, LegSnap = .8f, Stance = 5,
                ArmSpread = 22, ArmForward = 20, ArmBack = 16, ArmCarry = 0, ArmSnap = .9f, ArmLag = 0.12f,
                Lean = -3, Roll = 4.5f, RollDelay = .05f, Twist = 3, HeadPitch = -1, HeadSteady = .6f, Sway = .06f, Stomp = .015f, Glide = 1.52f,
            },
            Run = new Gait
            {
                LegForward = 44, LegBack = 40, LegSnap = .9f, Stance = 5,
                ArmSpread = 24, ArmForward = 36, ArmBack = 30, ArmCarry = 6, ArmSnap = 1, ArmLag = 0.12f,
                Lean = 6, Roll = 3.5f, Twist = 4, HeadPitch = -2, HeadNod = 2, HeadSteady = .5f, Bounce = .03f, Sway = .045f, Stomp = .02f, Glide = 1.69f,
            },
        };

        /// <summary>ALING NENA, the auntie on an errand: brisk and purposeful, leaning in, arms pumping short and business-like.</summary>
        public static readonly GaitStyle AlingNena = new GaitStyle
        {
            Name = "aling_nena",
            Walk = new Gait
            {
                LegForward = 36, LegBack = 34, LegSnap = 1.1f, Stance = 1,
                ArmSpread = 17, ArmForward = 26, ArmBack = 20, ArmCarry = 5, ArmSnap = 1.1f, ArmLag = 0.076f,
                Lean = 5, Roll = 2.5f, Twist = 5, HeadPitch = -1, HeadSteady = .8f, Bounce = .015f, Sway = .03f, Glide = 1.38f,
            },
            Run = new Gait
            {
                LegForward = 48, LegBack = 46, LegSnap = 1.1f, Stance = 1,
                ArmSpread = 18, ArmForward = 42, ArmBack = 34, ArmCarry = 10, ArmSnap = 1.1f, ArmLag = 0.057f,
                Lean = 10, Roll = 2, Twist = 6, HeadPitch = -2, HeadSteady = .8f, Bounce = .03f, Sway = .02f, Glide = 1.41f,
            },
        };

        /// <summary>IGGY: the lanky clown of the group, loose-limbed and a bit floppy, everything swinging late.</summary>
        public static readonly GaitStyle Iggy = new GaitStyle
        {
            Name = "iggy",
            Walk = new Gait
            {
                LegForward = 44, LegBack = 40, LegSnap = 1.05f, Stance = 2,
                ArmSpread = 20, ArmForward = 34, ArmBack = 28, ArmCarry = 0, ArmSnap = .9f, ArmLag = 0.12f,
                Lean = 1, Roll = 4, RollDelay = .08f, Twist = 6, HeadNod = 2, HeadSteady = .3f, Bounce = .03f, Sway = .045f, Glide = 1.36f,
            },
            Run = new Gait
            {
                LegForward = 56, LegBack = 52, LegSnap = 1.05f, Stance = 2,
                ArmSpread = 22, ArmForward = 52, ArmBack = 44, ArmCarry = 6, ArmSnap = 1, ArmLag = 0.12f,
                Lean = 11, Roll = 2.5f, Twist = 9, HeadNod = 2, HeadSteady = .4f, Bounce = .05f, Sway = .02f, Glide = 1.4f,
            },
        };
    }
}
