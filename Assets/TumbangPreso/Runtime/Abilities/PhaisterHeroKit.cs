using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class PhaisterHeroKit : HeroKit
    {
        public bool IsWitchfireInfused { get; set; } = true;
        public bool IsEclipseActive => Ultimate != null && Ultimate.IsActive;

        public PhaisterHeroKit() : base("phaister", "PHAISTER")
        {
            Skill1 = new HexSigilAbility();
            Skill2 = new ShadowPhaseBlinkAbility();
            Ultimate = new GrandCovenEclipseAbility();
        }

        /// <summary>
        /// ⚠️ 13 CHARGES, BETWEEN DANTE'S 12 AND SEAN'S 15. Grand Coven paints the street, slows
        /// and afflicts inside it, and changes the sky, but like Titan Fissure it converts into
        /// score only through a follow-up. Was 115 against a knockdown worth 25, which is 4.6.
        /// `Balance`'s ultimate economy block has the request and the pacing arithmetic.
        /// </summary>
        public override float UltimateCost => 13.0f;

        // ⚠️⚠️ `ResolveBlinkShove` IS DELETED AND SO IS THE `ReqBlink` MESSAGE BEHIND IT. This
        // was the host's half of a bespoke request for ONE power, written while the ability
        // layer had no cast replication at all. `MatchRpc`'s `ReqAbility` now carries every
        // cast, the host runs the same kit code the solo game runs, and the knockback resolves
        // inside `ShadowPhaseBlinkAbility.ResolveShove` on the host as a consequence of that.
        // A verb wired twice is a verb wired once and maintained never.

        /// <summary>
        /// Skill 1: HEX (2 charges per round). HOLD to place, release to chalk it.
        ///
        /// ⚠️⚠️ IT WAS A FIXED 4.5 m IN FRONT OF HER NOSE AND THAT IS THE WHOLE ABILITY BEING
        /// GUESSED AT. 🧑 2026-09-02: *"Her q should be holdable and person using her and only
        /// that person should be able to see where the held skill will go"*, *"if person
        /// releases it will be cast"*. A ward is a trap: its value is entirely in WHERE it is,
        /// which means the one thing the power was not letting her decide was the only decision
        /// in it. Aiming by walking backwards and forwards to change a fixed offset is not
        /// aiming, it is arithmetic performed with your feet.
        ///
        /// ⚠️ THE RANGE BAND IS 2.2 TO 5.5 m, WHICH IS DELIBERATELY THE BLINK'S. Her two placed
        /// powers now reach the same distance and ramp over the same 0.55 s, so learning one
        /// teaches the other; the maximum came off `ShadowPhaseBlinkAbility.MaxRange` for that
        /// reason rather than from the old 4.5. The minimum is a body length clear of her, so a
        /// panicked release still lands a ward she can back away over rather than one she is
        /// standing in.
        ///
        /// ⚠️⚠️ `maxHoldSeconds: 0`, SO ONLY THE RELEASE CASTS IT, AND THAT IS ALSO WHAT HE
        /// ASKED FOR ON THE BLINK: *"i want it to cast only when i let go"* (2026-08-27). The
        /// hold buys nothing after the 0.55 s ramp, so `docs/VISION.md` § 4's *"nothing may
        /// reward waiting"* is answered the same way it is there: she keeps full movement, the
        /// anti-camp clock keeps running, and a longer hold pays out exactly zero.
        ///
        /// ⚠️⚠️ AND THE TELEGRAPH IS A WARD RATHER THAN A RING, WHICH IS THE OTHER HALF OF THE
        /// SAME REPORT: *"can u make a magic circle for her q as well? its ugly bcz its js a
        /// shadow"*. `HeroHazards.SpawnHexSigil` already draws `VfxShapes.WardCircle` when the
        /// hex lands; the aim mark is now the same inscription, so the promise and the thing
        /// promised are one object. See `GroundReticle.Style`.
        ///
        /// ⚠️ ONLY SHE SEES IT. `GroundReticle.Show` refuses to draw for anybody the camera is
        /// not looking through, which is his *"and only that personn should be able to see"* and
        /// was already true for the blink. A held aim is a decision that has not been made yet;
        /// painting it on the road for the other three hands away the one thing hold-to-aim
        /// buys, which is that you may change your mind.
        /// </summary>
        private sealed class HexSigilAbility : HeroAbility
        {
            private const float HexRadius = 2.4f;
            private const float SigilLifetime = 6.0f;

            /// <summary>Nearest she may chalk one. A ward under her own feet is not a trap.</summary>
            private const float MinRange = 2.2f;

            /// <summary>Furthest, at a full hold. The blink's reach, deliberately.</summary>
            private const float MaxRange = 5.5f;

            public HexSigilAbility()
                : base("phaister_skill1", "HEX",
                       "Hold to place a hex circle on the road, then let go to chalk it. Anyone who walks into it loses their footing.",
                       0.0f, SigilLifetime, AbilityGlyph.PhaisterHexSigil,
                       summary: "Hold to aim, release to chalk. Walk in and you stumble.",
                       telegraphRadius: HexRadius,
                       telegraphRange: MaxRange,
                       castAction: "hero-phaister-hex",
                       viewmodelAction: "cast-hex",
                       castCue: "sfx_cast_phaister_hex",
                       charges: 2,
                       rechargedBy: Recharge.Never)
            {
                AimByHolding(MinRange, MaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = Visual.GroundReticle.Style.Ward;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ HER OWN CAST VOICE. This ability had none at all, so the only thing marking
                // the cast was the ward's own `sfx_hex_cast`; every other hero in the game
                // grunts when they spend a skill. See `tools/generate_hero_audio.py` for why she
                // had no voice to play until now.
                NetCue.Play("hero_phaister_grunt", ctx.Position);

                // ⚠️⚠️ THE WARD GOES WHERE THE RING WAS, NOT WHERE SHE IS POINTING NOW. This is
                // the same read `ShadowPhaseBlinkAbility` makes and for the same reason: the
                // player has been looking at that circle for up to half a second, and a cast
                // that recomputes the destination from `ctx.Forward` would place it wherever the
                // mouse happened to be on the frame the finger came up. The fallback is the
                // aimed reach along the current facing, which is what a kit with no system
                // attached (a probe, a headless match) gets.
                Vector3 targetPos = AimedDestination(ctx);

                int slot = ctx.Motor != null ? ctx.Motor.PlayerSlot : -1;

                HeroHazards.SpawnHexSigil(targetPos,
                    HexRadius * ctx.CostScale("phaister.1.brand"), SigilLifetime, slot,
                    ctx.GainScale("phaister.1.brand"));
                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchSigil, 1.5f);
            }
        }

        /// <summary>
        /// Skill 2: SHADOW BLINK (36.0 s cooldown). HOLD to aim, release to go.
        ///
        /// ⚠️⚠️ IT WAS NEVER A TELEPORT AND THE EFFECT SAID IT WAS. The old `OnActivate`
        /// computed `endPos = startPos + forward * 4.2` **only to feed the visual**, and then
        /// moved the body with `ctx.Motor.ApplyImpulse(pushDir * 12.0f)`: she was shoved four
        /// metres through whatever happened to be in the way while two glyphs claimed she had
        /// vanished and reappeared. 🧑, having played it: *"let her HOLD e to control where she
        /// will go and make it a teleport abilitiy and make it prettier"*. Feel starts here and
        /// not in the particles, so this is the half of that request that is not art.
        ///
        /// ⚠️⚠️ `CharacterMotor.Teleport`, NEVER AN IMPULSE. That method clamps X and Z
        /// INDEPENDENTLY to the playable rectangle, which is `CLAUDE.md` § 4's *"the box is a
        /// SQUARE, not a circle"*: a radial clamp and a square one disagree by 2.9 m on the
        /// diagonal, which is exactly where somebody blinks when they are cutting a corner. Its
        /// own note records that it is the path that once broke confinement outright, *"a seat
        /// 45.8 m out on X against a half width of 8.6"*, and therefore the one that has been
        /// fixed. An impulse toward a corner would have to be caught by `Confine` on the way,
        /// which is what she was relying on.
        ///
        /// ⚠️ AND HOLDING MUST NOT BE WORTH ANYTHING BY ITSELF. `docs/VISION.md` § 4:
        /// *"Nothing may reward waiting."* The reach stops growing at 0.55 s and the hold fires
        /// itself at 1.10 s; she keeps full movement throughout and nothing about aiming touches
        /// the anti-camp or anti-stall clocks. `HeroAbility`'s hold-to-aim section has the
        /// reasoning for each of the three.
        /// </summary>
        private sealed class ShadowPhaseBlinkAbility : HeroAbility
        {
            /// <summary>Nearest she can put herself. Under a body length is not an escape.</summary>
            private const float MinRange = 2.0f;

            /// <summary>
            /// Furthest, at a full hold.
            ///
            /// ⚠️ 5.5 m IS 39 PER CENT OF THE 14 m BOX AND IT REPLACES A FIXED 4.2. The old
            /// impulse carried `12² / (2 · Balance.Friction)`, which is the distance she
            /// travelled if nothing stopped her; a teleport always arrives, so the honest
            /// comparison is that the maximum went up slightly and the MINIMUM came down. What
            /// she gains is the choice, which is the whole ability.
            /// </summary>
            private const float MaxRange = 5.5f;

            /// <summary>The mark she leaves on arrival, and what the reticle promises.</summary>
            private const float ArrivalMark = 1.15f;

            /// <summary>How far the shove at the departure point reaches.</summary>
            private const float ShoveRadius = 2.5f;

            public ShadowPhaseBlinkAbility()
                : base("phaister_skill2", "SHADOW BLINK",
                       "Hold to pick a spot, let go and you are simply there. Whoever you left standing gets shoved back.",
                       52.0f, 0.4f, AbilityGlyph.PhaisterShadowBlink,
                       summary: "Hold to aim, release to teleport. Shoves whoever you left.",
                       telegraphRadius: ArrivalMark,
                       telegraphRange: MaxRange,
                       castAction: "hero-phaister-blink",
                       viewmodelAction: "blink",
                       castCue: "sfx_cast_phaister_blink")
            {
                // ⚠️⚠️ `maxHoldSeconds: 0` MEANS THE RELEASE IS THE ONLY THING THAT CASTS IT.
                // It was 1.10 s and it fired itself at the ceiling; 🧑 2026-08-27, having played
                // it: *"u cant control the E of phaister and it autocasts after some seconds, i
                // want it to cast only when i let go"*. `HeroAbility.MaxAimSeconds` carries why
                // this does not reopen the "nothing may reward waiting" objection the ceiling was
                // written for: the reach still stops growing at 0.55 s, so holding longer buys
                // nothing, and she is neither rooted nor exempt from the anti-camp clock while
                // she aims.
                AimByHolding(MinRange, MaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);

                // ⚠️⚠️ THE ONLY POWER IN THE GAME THAT AIMS AT A PLACE YOU WILL BE STANDING, AND
                // IT IS WHY `AimBeacon` EXISTS. 🧑 2026-08-27: *"to teleport u have to hold her E
                // skill and all it shows is a frigging shadow, it's very easy to miss and not in
                // her theme at all"*. The ring stays and gains a torn `Rift` standing in it, which
                // is the same shape `HeroHazards.SpawnShadowRift` tears at the place she leaves:
                // the aim mark, the departure and the arrival are now one visual idea rather than
                // a grey decal followed by two unrelated effects.
                AimBeacon = true;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️⚠️ `sfx_ghost_teleport` IS OFF THE DEPARTURE AS OF 2026-09-02, AND THE
                // ARGUMENT THAT KEPT IT HERE IS WORTH READING BEFORE PUTTING IT BACK. It was:
                // *"a blink IS the physical event Nemu's phase is, and two heroes who share an
                // element are allowed to share the sound of the one thing they both literally
                // do"*, after `docs/TODO.md` § 21.4 took the borrowed cues off her ward and her
                // ultimate.
                //
                // What that reasoning missed is that Nemu was playing the same cue on BOTH of her
                // skills, so it was not two heroes sharing one event, it was three powers across
                // the two heroes least able to afford it sharing one sound. 🧑 2026-09-02: *"make
                // it unique throughout each character"*. Her departure is `sfx_cast_phaister_blink`
                // now, sounded centrally off `HeroAbility.CastCue`.
                //
                // ⚠️ THE ARRIVAL IS UNCHANGED. `sfx_blink_arrive` still plays at the far end, up
                // to 5.5 m away, and its own note has why a cue fired at `startPos` cannot cover
                // that.
                NetCue.Play("hero_phaister_grunt", ctx.Position);

                Vector3 startPos = ctx.Position;
                Vector3 facing = ctx.Forward;

                // Where the ring the player has been looking at for the last half second is.
                // ⚠️ THROUGH `HeroAbility.AimedDestination` SINCE 2026-09-02. This was the only
                // copy of that read until the hex and both of Cheska's placed powers needed the
                // same three lines; it is on the base class now, with the same fallback.
                Vector3 destination = AimedDestination(ctx);

                // ⚠️⚠️ THE TWO ENDS ARE TWO DIFFERENT EFFECTS AND NEITHER IS THE OTHER MIRRORED.
                // `HeroHazards.SpawnShadowRift` is a torn vertical sheet at the place she left;
                // `SpawnShadowArrival` is written characters falling onto the place she reached.
                // They shared one `SpawnCastGlyph` call until 2026-08-26, which is most of why
                // the blink read as "the hex again, twice".
                HeroHazards.SpawnShadowRift(startPos, facing);
                HeroHazards.SpawnShadowArrival(destination);

                ctx.Motor.Teleport(destination);

                // ⚠️ THE ARRIVAL HAS ITS OWN CUE AND ITS OWN POSITION. `sfx_ghost_teleport` above
                // plays where she LEFT; up to 5.5 m of street later she simply appears, and until
                // 2026-08-26 that end of the ability made no sound at all. A cue fired at
                // `startPos` cannot cover it: `AudioDirector` parks a pooled voice at the point it
                // is given, which is the same fault `LrtTrainFlyby` records about a moving train.
                NetCue.Play("sfx_blink_arrive", destination);

                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchScatter, 1.0f);

                HostShove(ctx.Motor, startPos, facing);
            }

            /// <summary>
            /// The knockback at the departure point, resolved by whoever owns the bodies.
            ///
            /// ⚠️⚠️ IT MOVES SOMEBODY ELSE'S BODY, WHICH IS THE ONE THING A CLIENT MAY NEVER DO.
            /// The old version ran `Physics.OverlapSphere` and `ApplyImpulse` on whichever peer
            /// cast the ability, so in a networked match a client shoved three bodies the host
            /// owns and the host then corrected all three on its next transform sync: the
            /// victims rubber-band, and the caster's screen and everybody else's disagree about
            /// where the fight is. `CLAUDE.md` § 4: *"Contact resolves by DISTANCE on the host"*.
            ///
            /// ⚠️⚠️ AND A VERB WITH `ShouldResolve` MUST ALSO HANDLE `ShouldRequest`.
            /// `NetAuthority`'s own note records what the other shape costs: the lunge guarded
            /// its sweep with "if not networked or host" and had no else branch, so *"the verb
            /// was simply dead for three of the four players in every networked match, for
            /// weeks"*.
            ///
            /// ⚠️⚠️ THE REQUEST HALF IS NOW THE GENERAL ONE AND THIS METHOD SENDS NOTHING.
            /// `MatchRpc.RequestAbilityCastServerRpc` replicates the CAST, the host re-runs this
            /// very ability from the caster's reported pose, and the knockback happens on the
            /// host as part of that. The bespoke `ReqBlink` message this used to send is deleted:
            /// with a cast rpc in place it was a second wire for a verb that already had one, and
            /// it double-resolved the shove on the host for a client that was on a build carrying
            /// both.
            /// </summary>
            private static void HostShove(CharacterMotor caster, Vector3 at, Vector3 facing)
            {
                if (caster == null) return;

                ResolveShove(caster.PlayerSlot, at, facing);
            }

            /// <summary>
            /// The host's half.
            ///
            /// ⚠️ IT TAKES A SLOT RATHER THAN A MOTOR, because the host is resolving a cast
            /// about a seat and must look the body up itself. A client that could hand over a
            /// `CharacterMotor` reference would be a client naming its own victims.
            /// </summary>
            public static void ResolveShove(int casterSlot, Vector3 at, Vector3 facing)
            {
                if (!NetAuthority.ShouldResolve()) return;

                facing.y = 0.0f;
                if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
                facing.Normalize();

                var hits = Physics.OverlapSphere(at, ShoveRadius);
                foreach (var hit in hits)
                {
                    var other = hit.GetComponentInParent<CharacterMotor>();
                    if (other == null || other.PlayerSlot == casterSlot) continue;

                    Vector3 repel = other.transform.position - at;
                    repel.y = 0.0f;
                    if (repel.sqrMagnitude < 0.01f) repel = facing;

                    other.ApplyImpulse(repel.normalized * 6.0f + Vector3.up * 2.0f);
                    other.ApplyStagger(0.4f);
                }
            }
        }

        /// <summary>
        /// Ultimate: GRAND COVEN ECLIPSE (115 charge). The street goes dark.
        ///
        /// ⚠️⚠️ WHAT IT DOES CHANGED ON 2026-08-26 AND THE OLD VERSION DID ALMOST NOTHING.
        /// `docs/TODO.md` § 23 left this open in as many words: *"Phaister's eclipse curse
        /// staggers for 0.50 s, below `Balance.MinStunDown`, so her ultimate does not hold
        /// anybody and gets no coat"*. `CharacterMotor.ApplyStagger` forces anything at or under
        /// the then-1.20 s floor back to `StunElement.None`, so the most expensive power in her kit
        /// applied a knockback hitch, drew no element coat, raised no mash card, and was
        /// unmashable and unnoticeable at the same time. It was left alone rather than retuned
        /// because it is a balance question; this is the answer, written down.
        ///
        /// ⚠️⚠️ **1.60 s, FIVE PRESSES, AND ONLY INSIDE THE REACH.** The three numbers together:
        ///
        ///   * **1.60 s** cleared `Balance.MinStunDown` by 0.40 when that floor was 1.20, which
        ///     was then the smallest hold that actually IS one; anything at or under the floor is
        ///     silently demoted and this entry is the record of what that costs. ⚠️ THE FLOOR IS
        ///     0.60 AS OF § 83.14, so this now clears it by a full second and the hold is not the
        ///     marginal thing it was written as.
        ///   * **5 presses** against Cheska's 9, Dante's 8, Zack's 7, Nemu's 6 and Sean's 4.
        ///     `docs/TODO.md` § 23's rule is *"how hard the skill is supposed to hit"*, and the
        ///     thing that separates this from Cheska's nova is that it can hold **three people
        ///     at once**. A multi-target hold has to be shorter per victim than a single-target
        ///     one or it is three novas for one price. `perPress = (1.60 - 1.20) / 5 = 0.08 s`,
        ///     so a player who answered it was free in about 1.2 s against 1.6 unanswered. At
        ///     the 0.60 floor the same five presses buy 0.20 s each and free them in 0.6 s, which
        ///     is the point of § 83.14: every declared press pays.
        ///   * **The 5 m reach**, where it used to hit `round.Players` with no distance test at
        ///     all: an ultimate that reaches a player standing in the far corner of a 14 m box
        ///     cannot be positioned against, and positioning is the counterplay. The reach is
        ///     drawn on the ground by `SpawnGrandCovenEclipse`, so what it hits is what it says.
        ///
        /// ⚠️ AND THE SKY IS THE OTHER HALF OF WHAT IT DOES. 🧑: *"i want the sky to look
        /// ominous and shit and change for a brief moment into night and filled with magic"*.
        /// `Visual.SkyEvent.Look.Eclipse` runs for the whole duration, so the three people she
        /// caught mash their way out of a hold in a street that has gone dark around them.
        /// </summary>
        private sealed class GrandCovenEclipseAbility : HeroAbility
        {
            // -------------------------------------------------------------------
            // § WHY THIS ULTIMATE FELT LIKE NOTHING, AND WHAT IT IS NOW
            //
            // ⚠️⚠️ 🧑 2026-08-27, HAVING PLAYED IT: *"her ult doesnt last that long and it feels
            // like it does nothing. i want the eclipse to last for a few seconds even when it is
            // done and to do something more impactful as its an ult"*.
            //
            // ⚠️⚠️ THE DIAGNOSIS IS THAT IT WAS A ONE-FRAME POWER WEARING A FIVE-SECOND COAT.
            // `Curse` ran ONCE, inside `OnActivate`, against whoever happened to be within 5.0 m
            // on that single frame. Everything else the ultimate owned (the falling eclipse, the
            // ground ring, the aura, the weather) then played for five seconds over an arena in
            // which the power had already completely finished happening. A player who walked into
            // the ring one frame after the cast walked through a light show. That is why the most
            // expensive ability in the game (`UltimateCost` 115, the highest of the six) read as
            // a screensaver: the ring was drawing a boundary that meant nothing.
            //
            // ⚠️⚠️ SO IT IS A ZONE NOW, WHICH IS THE CHANGE THAT MAKES THE DRAWING TRUE. For the
            // whole duration, anyone inside the reach is cursed, and re-cursed as the hold
            // expires. The ring on the road now means what a ring on the road means everywhere
            // else in this game: do not stand there.
            //
            // ⚠️⚠️ AND IT IS DELIBERATELY USEFUL IN BOTH ROLES, WHICH 🧑 ASKED FOR BY NAME:
            // *"give everyone more creative ults that have an impact that effects or is usable
            // either in attacker/defender roles or both roles"*. A one-shot stun is an attacker's
            // tool only. An area the taya cannot stand in is a hole opened in the defence; the
            // same area centred on the lata by a DEFENDING Phaister is the retrieval run made
            // impossible for its duration. One power, opposite uses, chosen by where she stands,
            // which is the counterplay `docs/VISION.md` § 1.1 asks Hero Strike for.
            // -------------------------------------------------------------------

            /// <summary>
            /// How far the curse reaches. The same number the ground ring draws.
            ///
            /// ⚠️⚠️ 5.0 m BECAME 9.0 m ON INSTRUCTION, AND IT IS THE ONE FOOTPRINT IN THIS GAME
            /// THAT IS DELIBERATELY ALMOST THE WHOLE ARENA. 🧑 2026-08-27, watching it: *"i want
            /// the circle there to be a bit bigger too, that is so easy to dodge bro"*, then
            /// *"make it like very very big"*, then the design in his own words: *"that its
            /// undodgable or hard to run away from bcz i want ppl to try to run from it"* and
            /// *"make it liek really really big that they have to run the whole time its being
            /// casted in order to dodge"*.
            ///
            /// ⚠️⚠️ IT IS NOT ACTUALLY UNDODGEABLE AND MUST NOT BECOME SO. `docs/VISION.md` § 4
            /// forbids anything with no counterplay, and the counterplay here is the one he asked
            /// for by name: **running**. The arena is a 14 by 14 square, so its corners are 9.9 m
            /// from the centre. A 9.0 m circle centred on a caster standing in the middle leaves
            /// only the corners safe, and `CovenCircleBuild` takes **1.55 s** to draw itself on
            /// top of the 0.4 s wind-up. At `Balance.Speed` 4.6 m/s that is about 9 m of running,
            /// which is exactly enough to reach a corner from the middle if you start moving the
            /// moment the first ring lands and not if you hesitate. That is the whole power.
            ///
            /// ⚠️ WHICH IS WHY THE STAGED BUILD IS A BALANCE FEATURE AND NOT DECORATION. The
            /// build time IS the dodge window. Shortening it, or drawing the circle instantly,
            /// makes this the only genuinely unavoidable effect in the game.
            ///
            /// ⚠️ AND `docs/VISION.md` § 2 IS PAID BY IT BEING LINE ART. Rule 3 allows a big
            /// footprint spent on DETAIL rather than AREA; almost all of the road inside this
            /// circle is still road. Rule 2 allows one big ultimate at a time. It is the largest
            /// thing in the game and it paints nearly none of the floor it covers.
            /// </summary>
            /// ⚠️⚠️ AND THEN 9.0 BECAME 10.5 ON A SECOND PASS: *"make it even bigger after
            /// okay"*. At 10.5 m the circle covers the entire 14 by 14 box **including its
            /// corners**, which are 9.9 m from the middle. It is therefore genuinely
            /// undodgeable-by-standing-still, which is what he asked for.
            ///
            /// ⚠️⚠️ IT IS STILL ESCAPABLE, AND THE THING THAT MAKES IT SO IS THAT IT IS CENTRED
            /// ON HER RATHER THAN ON THE ARENA. A Phaister standing at (3, 3) leaves the far
            /// corner at (-7, -7) fourteen metres away, comfortably outside. So the counterplay
            /// `docs/VISION.md` § 4 requires is intact and is exactly the one he described:
            /// *"i want ppl to try to run from it"*. You run AWAY FROM HER, and where she chose
            /// to stand decides how far you have to go. A witch who walks to the middle before
            /// casting covers everything and has spent her positioning to do it.
            private const float Reach = 10.5f;

            /// <summary>
            /// See the class note. Must stay above `Balance.MinStunDown`.
            ///
            /// ⚠️ 1.60 s IS UNCHANGED PER APPLICATION, AND THAT IS THE POINT OF MAKING IT A ZONE
            /// RATHER THAN A LONGER STUN. A single 5 s hold in a 1-vs-3 game is the stun chain
            /// `CLAUDE.md` § 4 caps with `Max()`; what makes this an ultimate is that the hold
            /// COMES BACK while you are still standing in it. Leaving is always available and is
            /// always the right answer, which is what keeps it from being a hold nobody can play
            /// against.
            /// </summary>
            private const float CurseHold = 1.60f;

            /// <summary>Presses to break it. Fewer than every single-target hold in the game.</summary>
            private const int CurseBreakPresses = 5;

            /// <summary>
            /// How often the zone re-checks who is standing in it, in seconds.
            ///
            /// ⚠️⚠️ IT IS SLOWER THAN THE HOLD IS LONG, DELIBERATELY, AND THE GAP IS THE
            /// COUNTERPLAY. At 1.85 s against a 1.60 s hold there is a quarter-second window
            /// after every break in which a player who has just mashed free can run, and a player
            /// who stays gets caught again. A re-check faster than the hold would be an
            /// inescapable lock, which is the one thing a 1-vs-3 game cannot have: three people
            /// held forever by one press is a round that ends without being played.
            ///
            /// ⚠️ AND IT IS NOT A DAMAGE TICK. Re-applying a stagger through
            /// `CharacterMotor.ApplyStagger` overlaps via `Max()` rather than adding, so a player
            /// standing in the zone is held, not held for progressively longer.
            /// </summary>
            private const float RecurseEvery = 1.85f;

            private float _sinceCurse;

            public GrandCovenEclipseAbility()
                // ⚠️⚠️ THE DURATION WENT FROM 5.0 s TO 7.0 s, AND IT IS THE ONE NUMBER HERE THAT
                // BUYS THE "LASTS LONGER" HALF OF THE REQUEST DIRECTLY. With `SkyEvent.SecondsFor`
                // adding the 3.20 s fall on top, the street is under her weather for 10.2 s and
                // the zone is live for 7.0 of them, against 5.0 s of sky and one frame of effect
                // before. 7.0 also lines it up with Zack's Thunderstrike, which was already the
                // longest, so the two most "lasting" ultimates in the game now read as the same
                // KIND of power.
                : base("phaister_ultimate", "GRAND COVEN",
                       "Pulls an eclipse over the street. Anyone caught under it is cursed where they stand, again and again, until they get out.",
                       0.0f, 7.0f, AbilityGlyph.PhaisterEclipse,
                       summary: "Night falls. Nobody can stand under it.",
                       telegraphRadius: Reach,
                       telegraphRange: 0.0f,
                       castAction: "hero-phaister-eclipse",
                       viewmodelAction: "coven-eclipse",
                       castCue: "sfx_cast_phaister_coven")
            {
                TelegraphStyle = Visual.GroundReticle.Style.Ward;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️⚠️ THIS LINE CALLED `sfx_ghost_appear` UNTIL 2026-08-26, AND
                // `docs/TODO.md` § 21.4 RECORDED IT AS ALREADY FIXED. The cue was made
                // (`sfx_eclipse_toll`), given a mix level and registered, and the CALL SITE was
                // never changed, so the biggest moment in the newest kit went on logging
                // `no cue registered` and playing nothing.
                NetCue.Play("hero_phaister_ult", ctx.Position);
                NetCue.Play("sfx_eclipse_toll", ctx.Position);

                HeroHazards.SpawnGrandCovenEclipse(ctx.Position, Reach, Duration);
                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchEclipse, Duration);

                var kit = ctx.Motor.AbilitySystem?.Kit as PhaisterHeroKit;
                if (kit != null) kit.IsWitchfireInfused = true;

                // ⚠️⚠️ THE CENTRE IS REMEMBERED, BECAUSE THE ZONE MUST NOT FOLLOW HER. The
                // eclipse is hung over the point she cast it from and the ground ring is drawn
                // there; a zone that tracked her would be a 5.0 m aura she carries, which is a
                // completely different and far stronger power, and it would leave the ring on the
                // road pointing at nothing. `SpawnGrandCovenEclipse` parents its visuals to a
                // world position for the same reason.
                _centre = ctx.Position;
                _sinceCurse = 0.0f;

                Curse(_centre, ctx.Motor != null ? ctx.Motor.PlayerSlot : -1);
            }

            private Vector3 _centre;

            /// <summary>
            /// The zone, which is what makes this an ultimate rather than a flash.
            ///
            /// ⚠️ THE FIRST CURSE IS IN `OnActivate` AND NOT HERE, so casting it on somebody
            /// standing next to you still holds them on the frame it lands. This only answers the
            /// question "who is still standing in it", which is a different question and is asked
            /// on a slower clock. See <see cref="RecurseEvery"/> for why the two rates differ and
            /// why that gap is the counterplay rather than an oversight.
            /// </summary>
            protected override void OnTick(AbilityContext ctx, float dt)
            {
                _sinceCurse += dt;
                if (_sinceCurse < RecurseEvery) return;

                _sinceCurse = 0.0f;
                Curse(_centre, ctx != null && ctx.Motor != null ? ctx.Motor.PlayerSlot : -1);
            }

            /// <summary>
            /// ⚠️ HOST-SIDE, LIKE EVERY OTHER THING IN THIS GAME THAT MOVES A BODY THAT IS NOT
            /// YOURS. The old version staggered `round.Players` from whichever peer cast it. It
            /// took no request path because there is nothing for a client to ask for: the host
            /// is already running the same ultimate's `OnActivate` for that seat in a solo match,
            /// and in a networked one the ability layer is not replicated at all yet
            /// (`docs/TODO.md` § 25). What matters here is that a client cannot hold three
            /// bodies it does not own.
            /// </summary>
            private void Curse(Vector3 at, int mySlot)
            {
                if (!NetAuthority.ShouldResolve()) return;

                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == mySlot) continue;

                    Vector3 diff = p.transform.position - at;
                    diff.y = 0.0f;
                    if (diff.magnitude > Reach) continue;

                    p.ApplyStagger(CurseHold, StunElement.Hex, CurseBreakPresses);

                    // ⚠️ RELAYED. The stagger is a RULE and stays here behind the host gate;
                    // the aura and the CURSED! plate are what three other people could not see.
                    // See `Visual.MatchFlair`.
                    Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroCursed,
                                               mySlot, p.PlayerSlot,
                                               p.transform.position, CurseHold);
                }
            }
        }
    }
}
