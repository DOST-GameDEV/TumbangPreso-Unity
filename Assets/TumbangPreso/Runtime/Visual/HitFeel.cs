using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What a hit FEELS like to the person who took it.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A HIT IN THIS GAME HAD NO IMPACT FRAME AT ALL. Landing an ability
    /// on somebody played a `bump`, attached `DizzyStars` and applied an impulse, and all three
    /// of those describe the AFTERMATH. Nothing marked the instant of contact, which is the beat
    /// every fighting game, every shooter's hit confirm and Valorant's headshot all spend their
    /// budget on. The result is a mode where powers connect and nothing lands.
    ///
    /// Three things happen at once here and each is doing a different job:
    ///
    ///  * **Hitstop.** A very short freeze on the victim's own view. It is the whole reason a
    ///    hit reads as weight rather than as a teleport.
    ///  * **A punch on the victim's camera**, in the direction the hit came from, so the hit has
    ///    a bearing and not just a magnitude. A player who knows WHERE it came from can turn.
    ///  * **A chromatic pulse** at the frame's edges through `ColourGrade.PulseChromatic`, which
    ///    already existed for ultimates and had no smaller caller.
    ///
    /// ⚠️⚠️ IT RUNS ON THE VICTIM AND NEVER ON THE CASTER, AND THAT IS A RULE RATHER THAN A
    /// PREFERENCE. Two reasons, both of which have a shipped equivalent elsewhere in this repo:
    ///
    ///  1. **It would leak information.** Freezing the caster's frame tells them a hit landed on
    ///     somebody they may not be able to see, which is a fact the game makes them read off
    ///     the world. `docs/VISION.md` § 4 is a list of things a bracket forbids and this is the
    ///     same shape as all of them.
    ///  2. **There are four seats.** One knockdown that stuttered every screen in the match
    ///     would mean a four-player fight is mostly stutter. `CheskaHeroKit`'s nova already
    ///     learned the local version of this lesson with its camera shake.
    ///
    /// ⚠️ IT IS ALSO WHY THIS IS NOT `Time.timeScale`. A global time scale freezes the physics
    /// step for all four players and the round clock with it, so it would be a genuine gameplay
    /// pause rather than a presentation one. The freeze here is a CAMERA hold: the world keeps
    /// simulating and only the victim's view lags behind it for a few frames, which is what
    /// makes it safe to use in a networked match at all.
    ///
    /// `docs/Hero_Strike_Balance.md` § 4.1.
    /// </summary>
    public static class HitFeel
    {
        /// <summary>
        /// How hard the hit was. ⚠️ THREE STEPS AND NO FLOAT, deliberately: a caller passing its
        /// own 0.37 is a caller inventing a number, which is exactly the drift
        /// `HeroAbility.TelegraphRadius` was created to stop. Anything that needs a fourth
        /// weight adds one here where the whole set can be compared.
        /// </summary>
        public enum Weight
        {
            /// <summary>A graze or a zone tick. Barely a nudge, but not nothing.</summary>
            Jolt,

            /// <summary>A skill connecting properly.</summary>
            Solid,

            /// <summary>Put on the floor. The heaviest thing a skill can do.</summary>
            Knockdown,

            /// <summary>An ultimate connecting. The only step that earns a real freeze.</summary>
            Ultimate,
        }

        // ⚠️⚠️ THE FREEZE IS MEASURED IN MILLISECONDS AND THE NUMBERS ARE SMALL FOR A REASON.
        // 70 ms is about four frames at 60 Hz, which is the shortest hold the eye reliably reads
        // as impact rather than as a dropped frame. Past about 120 ms it stops being punctuation
        // and starts being a stutter the player tries to compensate for by moving, which in a
        // game whose whole tension is a run back in for your slipper is an active cost.
        private static readonly float[] FreezeSeconds = { 0.02f, 0.05f, 0.07f, 0.11f };
        private static readonly float[] PunchStrength = { 0.25f, 0.60f, 1.00f, 1.40f };
        private static readonly float[] ChromaticPeak = { 0.10f, 0.22f, 0.35f, 0.55f };

        /// <summary>
        /// Report that <paramref name="victim"/> just took a hit.
        ///
        /// ⚠️ EVERY ARGUMENT IS OPTIONAL EXCEPT THE VICTIM, AND THE WHOLE THING IS A NO-OP OFF A
        /// NULL. This is called from inside ability `OnActivate` and `OnTick` bodies, several of
        /// which run in EditMode tests with no camera, no rig and no live scene at all.
        /// </summary>
        /// <param name="from">
        /// Where the hit came FROM, for the camera punch bearing. `Vector3.zero` means "no
        /// direction", which is correct for a zone the victim walked into: there is nothing to
        /// turn toward.
        /// </param>
        public static void Land(CharacterMotor victim, Weight weight,
                                Color accent, Vector3 from = default)
        {
            if (victim == null) return;

            // ⚠️⚠️ THE LOCAL CHECK IS THE ENTIRE SAFETY OF THIS FUNCTION. `CameraRig.Following`
            // is the character THIS machine is looking through, so a hit on anybody else costs
            // one reference comparison and returns. Without it a four-player match applies every
            // hit in the round to one screen.
            var rig = FindLocalRig();
            if (rig == null || rig.Following != victim) return;

            int step = (int)weight;

            rig.HoldFrame(FreezeSeconds[step]);

            Vector3 bearing = victim.transform.position - from;
            bearing.y = 0.0f;

            // A zero `from` leaves the bearing pointing at the victim's own position, which
            // normalises to nothing useful, so an undirected hit punches straight back instead.
            if (from == default || bearing.sqrMagnitude < 0.0004f) bearing = -victim.transform.forward;

            rig.ImpactPunch(bearing.normalized, PunchStrength[step]);

            var grade = rig.GetComponent<ColourGrade>();
            if (grade != null) grade.PulseChromatic(ChromaticPeak[step], FreezeSeconds[step] * 3.0f);

            // ⚠️ THE ACCENT IS THE ATTACKER'S HERO COLOUR, NOT THE VICTIM'S. Being able to tell
            // WHO hit you is the difference between a fight and a shove from nowhere, and
            // `UiTheme`'s five accents are already asserted 30 degrees clear of each other by
            // `HeroPresentationTests` so they survive being seen for a tenth of a second.
            DamageVignette.Flash(rig, accent, FreezeSeconds[step] * 4.0f);
        }

        private static CameraSystem.CameraRig _cached;

        /// <summary>
        /// ⚠️ CACHED, BECAUSE THIS IS CALLED FROM INSIDE A PER-VICTIM LOOP ON AN ULTIMATE.
        /// Glacial Nova can hit three players in one frame and `FindFirstObjectByType` is a
        /// scene walk. The cache is revalidated rather than trusted, so a rig destroyed at a
        /// scene change does not strand this on a dead reference: `CLAUDE.md` § 7.1 records a
        /// HUD string rebuilt every frame costing a probe an eighth of its frames, and this is
        /// the same class of cost in a smaller place.
        /// </summary>
        private static CameraSystem.CameraRig FindLocalRig()
        {
            if (_cached != null) return _cached;

            _cached = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            return _cached;
        }
    }
}
