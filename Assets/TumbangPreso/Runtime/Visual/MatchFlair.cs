// V2 owner map: accepted contact | one burst/moment | rule or power colour |
// actual contact; travel | streak | seat identity | moving object. Events are
// host accepted; camera confirmation belongs only to the participating viewer.
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The moments a match is made of, drawn on every screen instead of on the host's.
    ///
    /// ⚠️⚠️ IT IS `NetCue` FOR THE THINGS YOU LOOK AT, AND IT EXISTS FOR THE SAME REASON.
    /// 🧑 2026-08-29, as the last item of the batch: *"ur final task is to make sure that all
    /// host sided shit is seen by everyone and not js host"*.
    ///
    /// `NetCue` closed the audio half by separating deciding from announcing. The visual half was
    /// still wrong in exactly the same places and for exactly the same reason: a tag, a block, a
    /// bank shot and a zap are all resolved inside a `NetAuthority.ShouldResolve()` gate, and the
    /// popup, the burst, the stars, the camera punch and the style award were written on the line
    /// after the resolution. `tools/audit_presentation_reach.py` found **41 such call sites**
    /// across seven methods, which is most of the feedback in the game.
    ///
    /// ⚠️⚠️ THE PRESENTATION IS REBUILT FROM SEATS AND A POSITION, NOT SENT AS OBJECTS. Every
    /// peer already has all four bodies and the whole roster; what it lacks is the EVENT. So the
    /// wire carries a kind, up to two seat numbers and a point, and each machine draws its own
    /// copy from its own scene. That is four bytes and two ints against a description of a
    /// particle system, and it is why a client's camera punch lands on the client's own camera.
    ///
    /// ⚠️ `Announce` IS THE HOST'S CALL AND `Play` IS EVERYBODY'S. Same split as `NetCue.Play`
    /// and `MatchRpc.OnPlayCueMsg`: the caller plays it locally on the frame it happened rather
    /// than after a round trip, and the relay goes to everyone except that peer.
    ///
    /// ⚠️ AND IT IS A NO-OP DIFFERENCE OFFLINE. `NetAuthority.IsNetworked` is false with no
    /// transport, so in practice, in the tutorial and in every probe this is a direct call with
    /// one extra branch.
    /// </summary>
    public static class MatchFlair
    {
        // Every peer observes the same accepted event. Subscribers present it;
        // they never award score or infer an outcome from a camera effect.
        public static event System.Action<Kind, int, int, Vector3, float> Presented;

        /// <summary>
        /// What happened. ⚠️ THE VALUES ARE ON THE WIRE, so appending is free and reordering is
        /// a protocol break. `NetSession.ProtocolVersion` is what makes that safe.
        /// </summary>
        public enum Kind : byte
        {
            /// <summary>The taya caught somebody. `actor` is the taya, `subject` the victim.</summary>
            Tag = 0,

            /// <summary>A tsinelas was stopped by a body. `subject` is the blocker.</summary>
            Block = 1,

            /// <summary>A throw banked off the scenery and still counts. `actor` threw it.</summary>
            BankShot = 2,

            /// <summary>A throw missed the can by very little. `actor` threw it.</summary>
            NearMiss = 3,

            /// <summary>Zack's charged tsinelas went off. `subject` is who it jolted, or -1.</summary>
            Zap = 4,

            /// <summary>The can went over. `actor` is the thrower.</summary>
            LataDown = 5,

            /// <summary>A tsinelas left a hand. `actor` threw it; `strength` carries the spin.</summary>
            Throw = 6,

            // -----------------------------------------------------------------
            // § A HERO POWER LANDING ON SOMEBODY
            //
            // ⚠️⚠️ EVERY ONE OF THESE IS `ApplyStagger` FOLLOWED BY A FLOURISH INSIDE A
            // `ShouldResolve()` GATE, WHICH IS § 83.16's FAULT ONE FILE OVER. The SOUND on each
            // of them was moved to `NetCue` months ago and the LOOK was not, so a client heard
            // the poltergeist connect and saw nothing hit anybody.
            //
            // ⚠️ THEY ARE SEPARATE KINDS RATHER THAN ONE KIND PLUS A WORD ON THE WIRE, and that
            // is what keeps the payload at what it already was: kind, two seats, a point and a
            // scalar. A popup string per hit is a per-frame allocation and a second thing to
            // keep in step. `strength` carries the stagger's own duration, so the stars last
            // exactly as long as the hold does on every screen.
            //
            // ⚠️ AND THE COLOUR IS DERIVED FROM THE CASTER, NOT SENT. `UiTheme.BrightForHero`
            // off `actor`'s `CharacterIndex` is the same lookup the local code did by writing
            // the accent literally, and it cannot drift from the hero it belongs to.
            // -----------------------------------------------------------------

            /// <summary>A skill connected. Stars for `strength` seconds, in the caster's accent.</summary>
            HeroHit = 7,

            /// <summary>As <see cref="HeroHit"/>, plus the `BAM!` a burn pulse draws.</summary>
            HeroBam = 8,

            /// <summary>Nemu's poltergeist reaching somebody: stars and `BOO!`.</summary>
            HeroBoo = 9,

            /// <summary>Phaister's eclipse: the aura and `CURSED!`.</summary>
            HeroCursed = 10,

            /// <summary>Her hex sigil ticking: `HEXED!`.</summary>
            HeroHexed = 11,

            /// <summary>Zack's zone jolting somebody: the zap ring and stars.</summary>
            HeroZapped = 12,

            /// <summary>Cheska's ice catching somebody out: `WHOA!` at `at`.</summary>
            HeroWhoa = 13,

            /// <summary>A wall of ice coming down. `at` is where; nobody is hit.</summary>
            IceShatter = 14,

            /// <summary>Zack's bolt reaching the street. `at` is the strike.</summary>
            Thunder = 15,

            /// <summary>Existing ultimate camera feedback, only on the named victim's view.</summary>
            UltimateImpact = 16,

            // -----------------------------------------------------------------
            // § SEAN'S HITS (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md`)
            //
            // ⚠️ `at` IS WHERE THE HIT CAME FROM, NOT WHERE IT LANDED, for the three body hits: the victim's camera is
            // punched from it and their body recoils away from it. The landing point is the victim's own body. `strength`
            // is the stagger, so the stars last as long as the hold, exactly as `HeroHit`.
            // -----------------------------------------------------------------

            /// <summary>LAVA RAM, MAGMA FIST, an erupting shove or the dropped bomb: knockdown weight.</summary>
            SeanSlam = 17,

            /// <summary>The awakening blast or the thrown bomb: ultimate weight.</summary>
            SeanBlast = 18,

            /// <summary>PRESSURE's splash on a player beside the one erupted on: a jolt, no stars.</summary>
            SeanSplash = 19,

            /// <summary>Something tried to move Molten Sean. `actor` pushed, `subject` is Sean, `at` is where it came from.</summary>
            SeanAbsorb = 20,

            /// <summary>A VOLCANIC BOMB left his hands. `actor` is Sean, `at` where it will land, `strength` its radius.
            /// Unused since MAYON AWAKENS v2 (the volcano throws them); kept so the numbers after it never move.</summary>
            SeanBombThrown = 21,

            /// <summary>His volcano lobbed a bomb (MAYON AWAKENS v2). `actor` is Sean, `subject` the opponent it was thrown at,
            /// `at` where it lands, `strength` its index in the eruption. Every peer draws the flight from the crater; the host
            /// alone resolves the splash when it lands.</summary>
            SeanVolcanoBomb = 22,
        }

        /// <summary>The highest kind on the wire. The relay drops anything above it (`MatchRpc.BroadcastFlair`, `OnFlairMsg`).</summary>
        public const Kind LastKind = Kind.SeanVolcanoBomb;

        /// <summary>
        /// Draw this on every screen. Host-side; a client that reaches it draws its own copy and
        /// sends nothing, which is what `MatchRpc.BroadcastFlair` enforces.
        /// </summary>
        public static void Announce(Kind kind, int actor, int subject, Vector3 at,
                                    float strength = 0.0f)
        {
            Play(kind, actor, subject, at, strength);
            Net.MatchRpc.Instance?.BroadcastFlair((byte)kind, actor, subject, at, strength);
        }

        /// <summary>
        /// Draw it here, from this peer's own scene.
        ///
        /// ⚠️ EVERY LOOKUP IS GUARDED, BECAUSE A REPLICATED EVENT CAN ARRIVE ONE FRAME EITHER
        /// SIDE OF THE SEAT IT NAMES. A client is routinely mid-build (`docs/TODO.md` § 82.1),
        /// and a null here must be a missing flourish rather than an exception inside a message
        /// handler, which would drop every message queued behind it.
        /// </summary>
        public static void Play(Kind kind, int actor, int subject, Vector3 at,
                                float strength = 0.0f)
        {
            // ⚠️⚠️ THE MOMENT IS RECORDED HERE AND THAT IS WHY IT IS HERE RATHER THAN AT THE
            // FORTY CALL SITES THAT RAISE THESE. `MatchFlair` is already the one place every
            // notable event in the game passes through on EVERY peer, which is exactly what a
            // highlight record needs and exactly what the forty resolution sites are not: each of
            // those runs behind `NetAuthority.ShouldResolve()`, so a record written there would be
            // the host's alone and three players' replays would have nothing in them.
            // `docs/TODO.md` § 147.
            //
            // ⚠️ IT AWARDS NOTHING. `Diagnostics.MatchHighlights` cannot reach `AddScore` and no
            // caller of it can either, which is `docs/VISION.md` § 4 kept rather than restated.
            RecordHighlight(kind, actor, subject, at, strength);
            // ⚠️⚠️ EACH LISTENER ON ITS OWN, AND A THROWING ONE IS LOGGED AND PASSED (owner 2026-10-21: *"when tagged it
            // doesnt teleport the character back to the safezone"*). The callers announce BEFORE they finish the rule: the
            // tag stuns, announces, then refills and teleports; the block announces, then knocks back. A plain `Invoke`
            // let one broken HUD piece (`UI.MatchEventFeed`, stale across an editor play session) throw out of every tag
            // and block, so the victim stayed where they were caught. Presentation may fail; it may never cancel a rule.
            var listeners = Presented;
            if (listeners != null)
                foreach (System.Action<Kind, int, int, Vector3, float> listener in listeners.GetInvocationList())
                {
                    try { listener(kind, actor, subject, at, strength); }
                    catch (System.Exception failure) { Debug.LogException(failure); }
                }

            switch (kind)
            {
                case Kind.Tag:
                    PlayTag(Seat(actor), Seat(subject), at);
                    break;

                case Kind.Block:
                    PlayBlock(actor, Seat(subject), at, strength);
                    break;

                case Kind.BankShot:
                    ComicPopup.Spawn(at + Vector3.up * 0.35f, "BANK!", UI.UiTheme.Highlight, 1.0f);
                    break;

                case Kind.NearMiss:
                    break;

                case Kind.Zap:
                    PlayZap(Seat(subject), at);
                    break;

                case Kind.LataDown:
                    PlayCanConfirmation(Seat(actor), at);
                    UI.CanDownStamp.Play(actor);
                    break;

                case Kind.Throw:
                    break;

                case Kind.HeroHit:
                    PlayHeroHit(Seat(actor), Seat(subject), strength, null, HitFeel.Weight.Solid);
                    break;

                case Kind.HeroBam:
                    PlayHeroHit(Seat(actor), Seat(subject), strength, "BAM!", HitFeel.Weight.Jolt);
                    break;

                case Kind.HeroBoo:
                    PlayHeroHit(Seat(actor), Seat(subject), strength, "BOO!", HitFeel.Weight.Solid);
                    break;

                case Kind.HeroCursed:
                    PlayCursed(Seat(subject), strength);
                    break;

                case Kind.HeroHexed:
                    PlayHeroHit(Seat(actor), Seat(subject), strength, "HEXED!", HitFeel.Weight.Jolt);
                    break;

                case Kind.HeroZapped:
                    PlayZap(Seat(subject), at);
                    PlayHeroHit(Seat(actor), Seat(subject), strength, null, HitFeel.Weight.Jolt);
                    break;

                case Kind.HeroWhoa:
                    ComicPopup.Whoa(at);
                    break;

                case Kind.IceShatter:
                    ComicPopup.Freeze(at);
                    // ⚠️ GLACIAL WALL BREAKS ON EVERY SCREEN (ABILITY-2). The host counts the three hits
                    // and shatters its copy; this flair reaches every peer, so each breaks its own.
                    foreach (var wall in Object.FindObjectsByType<Abilities.HeroHazards.IceBarricadeComponent>(FindObjectsSortMode.None))
                        if (wall != null && (wall.transform.position - at).sqrMagnitude < 1.5f * 1.5f) wall.Shatter();
                    break;

                case Kind.Thunder:
                    ComicPopup.Zap(at);
                    break;

                case Kind.UltimateImpact:
                    HitFeel.Land(Seat(subject), HitFeel.Weight.Ultimate, AccentOf(Seat(actor)), at);
                    break;

                case Kind.SeanSlam:
                    SeanImpact.Body(Seat(actor), Seat(subject), at, strength, HitFeel.Weight.Knockdown);
                    break;

                case Kind.SeanBlast:
                    SeanImpact.Body(Seat(actor), Seat(subject), at, strength, HitFeel.Weight.Ultimate);
                    break;

                case Kind.SeanSplash:
                    // No subject is the splash itself (its ring, `strength` its radius); a subject is somebody it caught.
                    if (subject < 0) SeanImpact.Ring(at, strength);
                    else SeanImpact.Body(Seat(actor), Seat(subject), at, strength, HitFeel.Weight.Jolt);
                    break;

                case Kind.SeanAbsorb:
                    SeanImpact.Absorb(Seat(actor), Seat(subject), at);
                    break;

                case Kind.SeanBombThrown:
                    break;   // the bomb draws itself on every peer from the cast; the spectator director listens for this

                case Kind.SeanVolcanoBomb:
                    Abilities.SeanVolcano.LaunchBomb(actor, subject, at);
                    break;
            }
        }

        /// <summary>
        /// Turns a flair event into a highlight marker where one is justified.
        ///
        /// ⚠️⚠️ NOT EVERY FLAIR IS A HIGHLIGHT AND THE LIST IS SHORT ON PURPOSE. `Kind` has
        /// several values and most of them are a hero power landing on a body, which happens
        /// several times a round in Hero Strike and is the thing `docs/VISION.md` § 2 already
        /// calls unreadable when there is too much of it. A highlight reel of every stagger is a
        /// reel nobody watches. `docs/TODO.md` § 147: *"detect events that can be established
        /// reliably"*, which is a statement about confidence and about scarcity at once.
        ///
        /// ⚠️ THE KNOCKDOWN CARRIES ITS DISTANCE IN `strength`, which the wire already has room
        /// for and which nothing else on `LataDown` was using. Measuring it here from the actor's
        /// CURRENT position would be measuring where the thrower has run to since.
        /// </summary>
        private static void PlayCanConfirmation(CharacterMotor scorer, Vector3 at)
        {
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig == null) return;
            // Anyone standing near the can feels it go over, fading to nothing at nine metres.
            if (scorer == null || !rig.IsFollowing(scorer))
            {
                float near = 1.0f - Mathf.Clamp01(Vector3.Distance(rig.transform.position, at) / 9.0f);
                if (near > 0.0f) rig.Shake(.3f * near, .16f);
                return;
            }
            UI.Hud.TriggerHitmarker(UI.UiTheme.Offense);
            Vector3 towardCan = at - scorer.transform.position;
            // The scorer's own payoff (owner, October 31: "hitting the can doesnt feel that satisfying"): the view hangs
            // for a few frames, punches toward the can and the frame's edges split. A camera hold on this screen only,
            // not `Hitstop`: the match, and the retrieval run that starts now, never stop.
            rig.HoldFrame(.065f);
            rig.ImpactPunch(towardCan.sqrMagnitude > .01f ? towardCan.normalized : scorer.transform.forward, .7f);
            // The lens snaps in on the hit, and the tin is heard in your own head however far away the can is.
            rig.ViewPunch(new Vector3(-3.5f, 0, 2.5f), -7f);
            GameServices.Audio?.PlayReplayCue("can_knockdown", 1f, .75f, 0f);
            rig.GetComponent<ColourGrade>()?.PulseChromatic(.3f, .24f);
        }

        private static void RecordHighlight(Kind kind, int actor, int subject, Vector3 at,
                                            float strength)
        {
            var round = GameServices.Round;
            float secondsLeft = round != null && round.RoundActive ? round.TimeLeft : -1.0f;

            switch (kind)
            {
                case Kind.BankShot:
                    Diagnostics.MatchHighlights.Note(Core.HighlightKind.BankShot, actor);
                    break;

                case Kind.Block:
                    Diagnostics.MatchHighlights.Note(Core.HighlightKind.Block, subject, actor);
                    break;

                case Kind.Tag:
                    Diagnostics.MatchHighlights.Note(Core.HighlightKind.Tag, actor, subject);
                    break;

                case Kind.LataDown:
                    Diagnostics.MatchHighlights.NoteKnockdown(actor, strength, secondsLeft);
                    break;
            }
        }

        /// <summary>
        /// A hero power landing on a body: the stars, the weight, and the popup if there is one.
        ///
        /// ⚠️ THE STARS LAST AS LONG AS THE HOLD, which is why `strength` is the stagger's own
        /// duration rather than a fixed number. A crown that outlives the stun tells three other
        /// players somebody is still held when they are not, and the taya reads exactly that to
        /// decide whether to commit.
        ///
        /// ⚠️ AND A SHORT ONE DRAWS NO STARS. The micro-staggers are a quarter of a second and
        /// `CharacterMotor.ApplyStagger` demotes them to `StunElement.None` anyway; a crown that
        /// appears and vanishes inside three frames is a flicker, not a read.
        /// </summary>
        private static void PlayHeroHit(CharacterMotor caster, CharacterMotor victim,
                                        float seconds, string word, HitFeel.Weight weight)
        {
            if (victim == null) return;

            Color accent = AccentOf(caster);

            if (seconds >= 0.5f) DizzyStars.Attach(victim.transform, seconds, accent);

            if (!string.IsNullOrEmpty(word))
                ComicPopup.Spawn(victim.transform.position + Vector3.up * 1.2f, word, accent, 1.0f);

            // ⚠️⚠️ THE BEARING IS THE CASTER'S BODY, AND LEAVING IT OUT DISABLED HALF OF
            // `HitFeel`. `Land`'s fourth argument is where the hit came FROM, and its own header
            // says why it is there: *"so the hit has a bearing and not just a magnitude. A player
            // who knows WHERE it came from can turn."* Omitted, `from` defaults to
            // `Vector3.zero`, `Land` reads that as "no direction" and punches along
            // `-victim.transform.forward` instead: **every hero skill in the game shoved the
            // victim's camera straight backwards regardless of where the attack actually came
            // from**, which is not a missing effect but a WRONG one, and a player turning to face
            // it turned away. The two ultimates routed through UltimateImpact
            // (`CheskaHeroKit`, `DanteHeroKit`) preserve their accepted `ctx.Position`;
            // this method routes all five of the skill-weight hits and passed nothing.
            //
            // ⚠️ THE CASTER WAS ALREADY IN HAND: `AccentOf(caster)` on the line above is the
            // same body, looked up for the same reason (being able to tell WHO hit you).
            //
            // ⚠️ NULL IS A LEGAL ANSWER AND MEANS "NO BEARING". `Seat(actor)` returns null for a
            // caster this peer has no body for, exactly as `AccentOf` handles; `default` is what
            // `Land` already documents as "there is nothing to turn toward".
            //
            // ⚠️ IT LEAKS NOTHING. `Land` returns immediately unless the rig is following the
            // VICTIM, so this runs on the victim's machine about a hit they have just taken. The
            // attacker's screen is not touched, which is the rule `HitFeel`'s header states.
            HitFeel.Land(victim, weight, accent,
                         caster != null ? caster.transform.position : default);
        }

        private static void PlayCursed(CharacterMotor victim, float seconds)
        {
            if (victim == null) return;

            AbilityVfx.AttachAura(victim.transform, AbilityVfx.Aura.WitchEclipse, 2.5f);

            ComicPopup.Spawn(victim.transform.position + Vector3.up * 1.3f, "CURSED!",
                             UI.UiTheme.HeroWitchBright, 1.2f);

            if (seconds >= 0.5f)
                DizzyStars.Attach(victim.transform, seconds, UI.UiTheme.HeroWitchBright);
        }

        /// <summary>
        /// The caster's own accent, looked up rather than sent.
        ///
        /// ⚠️ `UiTheme.BrightForHero` IS THE ONE TABLE, so a kit retuned to a different element
        /// cannot leave a stale colour behind on the wire. A missing body falls back to the earth
        /// accent, which is what that method answers for an unknown id anyway.
        /// </summary>
        private static Color AccentOf(CharacterMotor caster)
        {
            if (caster == null) return UI.UiTheme.HeroEarthBright;
            string heroId = caster.AbilitySystem?.HeroId;
            if (!string.IsNullOrEmpty(heroId)) return UI.UiTheme.BrightForHero(heroId);

            var heroes = Roster.GetPeople(GameMode.HeroStrike);
            if (heroes == null || caster.CharacterIndex < 0 || caster.CharacterIndex >= heroes.Count)
                return UI.UiTheme.HeroEarthBright;

            return UI.UiTheme.BrightForHero(heroes[caster.CharacterIndex].Id);
        }

        /// <summary>
        /// The tag, whole: the stars, the popup, the burst, the flash, the two squashes and the
        /// victim's own camera.
        ///
        /// ⚠️⚠️ THE STAGGER AND THE TELEPORT ARE **NOT** IN HERE AND MUST NOT BE. This is
        /// presentation; the stun, the stamina refill and the trip home are RULES, and
        /// `RoundDirector.ApplyTagPenalty` owns them behind the host gate where they belong. A
        /// client that could stun a body from a message is a client that decides, which is the
        /// one thing `CLAUDE.md` § 4 forbids. The victim's stun arrives replicated, as it always
        /// did.
        ///
        /// ⚠️ THE SHAKE IS STILL THE VICTIM'S OWN CAMERA ONLY, and running this on four machines
        /// is what finally makes that true. `IsFollowing` was already the test; on the host it
        /// could only ever pass for the host's own view, so three players were tagged with no
        /// jolt at all while the host got one for somebody else's.
        /// </summary>
        private static void PlayTag(CharacterMotor taya, CharacterMotor victim, Vector3 at)
        {
            taya?.GetComponent<CharacterAnimator>()?.PresentTagContact(victim, at);
            if (victim != null)
            {
                DizzyStars.Attach(victim.transform, Balance.TagStunTime, UI.UiTheme.Defense);
                victim.GetComponentInChildren<CharacterVisual>()?.FlashHit();
            }

            var rig = Camera.main != null
                ? Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;
            // The participant already receives the HUD tag/score feedback. A second word
            // immediately above this close contact sits behind the clock in first person.
            // Keep the world callout for other players and broadcast/third-person views.
            bool participantFpp = rig != null && rig.IsLocalFpp &&
                ((taya != null && rig.IsFollowing(taya)) || (victim != null && rig.IsFollowing(victim)));
            // Still spawn/dedupe normally so presentation does not change the shared
            // random stream. Only this participant's first-person rendering is hidden.
            ComicPopup.Spawn(at, "TAGGED!", UI.UiTheme.Defense, 1.4f,
                ComicPopup.Weight.Cast, participantFpp ? rig.Following : null);
            TagContactAccent.Play(taya, victim, at);
            Hitstop.Trigger();

            if (taya != null && victim != null)
            {
                Vector3 hitDirection = victim.transform.position - taya.transform.position;
                // The caught body reacts: a recoil away from the hand, then dazed for the stun (`CharacterAnimator.TagFeel`).
                victim.GetComponent<CharacterAnimator>()?.PresentCaught(hitDirection);
                // The host sends the victim home on this same frame, so the body the hand lands on is left behind for
                // everyone to see caught, knocked back and yanked home (`CaughtAfterimage`).
                CaughtAfterimage.Play(victim, at, at - taya.transform.position);
                victim.GetComponentInChildren<CharacterSquashStretch>()?.Impact(hitDirection, 0.30f);
                taya.GetComponentInChildren<CharacterSquashStretch>()?
                    .DashStretch(taya.transform.forward, 0.18f);

                if (rig != null && rig.IsFollowing(victim))
                {
                    Vector3 impact = hitDirection;
                    rig.ImpactPunch(impact.sqrMagnitude > 0.01f ? impact.normalized : Vector3.back,
                                    1.0f);
                }
            }


            // ⚠️ THE ANNOUNCER IS SPOKEN PER PEER RATHER THAN RELAYED, which is `NetCue`'s rule
            // for a commentary track: each machine says its own line off the event it has just
            // been told about, and nobody hears somebody else's announcer.
            GameServices.Voice?.OnAttackerTagged();
            GameServices.HeroVoice?.OnTagged(taya, victim);
        }

        private static void PlayBlock(int thrower, CharacterMotor blocker, Vector3 at, float alongSpeed)
        {
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
            if (blocker != null)
            {
                blocker.GetComponentInChildren<CharacterVisual>()?.FlashHit();
                blocker.GetComponentInChildren<CharacterSquashStretch>()?
                    .Impact((at - blocker.transform.position).normalized * alongSpeed, 0.4f);

                // Owner, October 31: "di masyado ramdam if a slipper hits u". Taken on your own screen it now has the
                // hit frame, a punch from where it came, an edge flash in the thrower's colour and a thump. In first
                // person a popup would spawn inside the head, so only those watching the body get the word.
                HitFeel.Land(blocker, HitFeel.Weight.Solid,
                             thrower >= 0 ? UI.PlayerIdentity.Colour(thrower) : UI.HudDraw.Alarm, at);
                if (rig != null && rig.IsFollowing(blocker))
                {
                    InputLayer.Rumble.Pulse(.4f, .55f, .14f);
                    // In first person your body is not drawn: the view is jolted away from the hit and tilts, the lens
                    // flares, and PAK is right in your head.
                    Vector3 local = rig.transform.InverseTransformPoint(at);
                    float side = local.sqrMagnitude > .01f ? Mathf.Clamp(local.x / Mathf.Max(.2f, Mathf.Abs(local.z)), -1f, 1f) : 0f;
                    rig.ViewPunch(new Vector3(-7f, -side * 6f, side >= 0f ? 6f : -6f), 9f);
                    GameServices.Audio?.PlayReplayCue("slipper_hit", 1f, 1f, 0f);
                }
                else ComicPopup.Spawn(blocker.transform.position + Vector3.up * 1.2f, "PAK!", UI.UiTheme.Highlight, 1.0f);
            }

            // The thrower is told it connected.
            var throwerBody = Seat(thrower);
            if (throwerBody != null && throwerBody != blocker && rig != null && rig.IsFollowing(throwerBody))
                UI.Hud.TriggerHitmarker(UI.UiTheme.Highlight);

            ImpactBurst.SpawnSlipperHit(at, thrower >= 0 ? UI.PlayerIdentity.Colour(thrower) : UI.UiTheme.Impact,
                                        rig != null && blocker != null && rig.IsFollowing(blocker));
        }

        private static void PlayZap(CharacterMotor jolted, Vector3 at)
        {
            ComicPopup.Zap(at);

            if (jolted == null) return;

            DizzyStars.Attach(jolted.transform, 1.5f, UI.UiTheme.HeroElectricBright);

            // ⚠️ `at` IS THE ZAP'S OWN ORIGIN AND IS THE RIGHT BEARING HERE, not a caster. This
            // is a hazard tick rather than a swing, so what the victim needs to turn toward is
            // where the electricity is, which is the same choice `CheskaHeroKit` and
            // `DanteHeroKit` make by passing `ctx.Position`. Without it the punch fell back to
            // `-victim.transform.forward` and pointed at nothing. See `PlayHeroHit`.
            HitFeel.Land(jolted, HitFeel.Weight.Jolt, UI.UiTheme.HeroElectricBright, at);
        }

        /// <summary>The body in a seat, on THIS peer, or null. -1 means "nobody".</summary>
        private static CharacterMotor Seat(int slot)
        {
            if (slot < 0) return null;

            // `BodyAt`: a companion's seat (Phaister's doll) has a body too.
            return GameServices.Round != null ? GameServices.Round.BodyAt(slot) : null;
        }
    }
}
