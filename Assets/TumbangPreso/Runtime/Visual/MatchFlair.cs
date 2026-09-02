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
        }

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
            switch (kind)
            {
                case Kind.Tag:
                    PlayTag(Seat(actor), Seat(subject), at);
                    break;

                case Kind.Block:
                    PlayBlock(Seat(subject), at, strength);
                    break;

                case Kind.BankShot:
                    ComicPopup.Spawn(at + Vector3.up * 0.35f, "BANK!", UI.UiTheme.Highlight, 1.0f);
                    UI.Hud.ReportStyle(actor, 18.0f, "BANK SHOT");
                    break;

                case Kind.NearMiss:
                    UI.Hud.ReportStyle(actor, 10.0f, "SO CLOSE");
                    break;

                case Kind.Zap:
                    PlayZap(Seat(subject), at);
                    break;

                case Kind.LataDown:
                    UI.Hud.ReportStyle(actor, 42.0f, "TUMBA!");
                    break;

                case Kind.Throw:
                    UI.Hud.ReportStyle(actor, 5.0f + Mathf.Abs(strength) * 7.0f,
                                       Mathf.Abs(strength) >= 0.4f ? "PEKTUS CURVE" : "LET FLY");
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
                    break;

                case Kind.Thunder:
                    ComicPopup.Zap(at);
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

            HitFeel.Land(victim, weight, accent);
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
            if (victim != null)
            {
                DizzyStars.Attach(victim.transform, Balance.TagStunTime, UI.UiTheme.Defense);
                victim.GetComponentInChildren<CharacterVisual>()?.FlashHit();
            }

            ComicPopup.Spawn(at, "TAGGED!", UI.UiTheme.Defense, 1.4f);
            ImpactBurst.SpawnAt(at);
            Hitstop.Trigger();

            if (taya != null && victim != null)
            {
                Vector3 hitDirection = victim.transform.position - taya.transform.position;
                victim.GetComponentInChildren<CharacterSquashStretch>()?.Impact(hitDirection, 0.30f);
                taya.GetComponentInChildren<CharacterSquashStretch>()?
                    .DashStretch(taya.transform.forward, 0.18f);

                var rig = Camera.main != null
                    ? Camera.main.GetComponent<CameraSystem.CameraRig>()
                    : null;

                if (rig != null && rig.IsFollowing(victim))
                {
                    Vector3 impact = hitDirection;
                    rig.ImpactPunch(impact.sqrMagnitude > 0.01f ? impact.normalized : Vector3.back,
                                    1.0f);
                }
            }

            if (taya != null) UI.Hud.ReportStyle(taya.PlayerSlot, 36.0f, "HULI!");

            // ⚠️ THE ANNOUNCER IS SPOKEN PER PEER RATHER THAN RELAYED, which is `NetCue`'s rule
            // for a commentary track: each machine says its own line off the event it has just
            // been told about, and nobody hears somebody else's announcer.
            GameServices.Voice?.OnAttackerTagged();
        }

        private static void PlayBlock(CharacterMotor blocker, Vector3 at, float alongSpeed)
        {
            if (blocker != null)
            {
                blocker.GetComponentInChildren<CharacterVisual>()?.FlashHit();
                blocker.GetComponentInChildren<CharacterSquashStretch>()?
                    .Impact((at - blocker.transform.position).normalized * alongSpeed, 0.22f);

                UI.Hud.ReportStyle(blocker.PlayerSlot, 12.0f, "HARANG!");
            }

            ImpactBurst.SpawnAt(blocker != null ? blocker.transform.position : at);
        }

        private static void PlayZap(CharacterMotor jolted, Vector3 at)
        {
            ComicPopup.Zap(at);

            if (jolted == null) return;

            DizzyStars.Attach(jolted.transform, 1.5f, UI.UiTheme.HeroElectricBright);
            HitFeel.Land(jolted, HitFeel.Weight.Jolt, UI.UiTheme.HeroElectricBright);
        }

        /// <summary>The body in a seat, on THIS peer, or null. -1 means "nobody".</summary>
        private static CharacterMotor Seat(int slot)
        {
            if (slot < 0) return null;

            var round = GameServices.Round;
            if (round == null) return null;

            foreach (var p in round.Players)
                if (p != null && p.PlayerSlot == slot) return p;

            return null;
        }
    }
}
