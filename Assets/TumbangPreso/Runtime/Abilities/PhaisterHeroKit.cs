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
            Skill1 = new KulamHexSigilAbility();
            Skill2 = new ShadowPhaseBlinkAbility();
            Ultimate = new GrandCovenEclipseAbility();
        }

        public override float UltimateCost => 115.0f;

        /// <summary>
        /// The host's half of Shadow Blink's knockback, for <see cref="Net.MatchRpc"/>.
        ///
        /// ⚠️ IT IS ON THE KIT RATHER THAN THE ABILITY BECAUSE THE ABILITY IS PRIVATE, and it
        /// should stay private: an ability is an instance owned by one hero's kit, and a network
        /// message arriving for seat 2 has no instance to talk to. The resolution is stateless,
        /// so a static entry point is the honest shape rather than a workaround.
        /// </summary>
        public static void ResolveBlinkShove(int casterSlot, Vector3 at, Vector3 facing)
            => ShadowPhaseBlinkAbility.ResolveShove(casterSlot, at, facing);

        /// <summary>
        /// Skill 1: KULAM HEX (2 charges per round)
        /// Chalks a ward on the road. Anyone who walks into it loses their footing.
        /// </summary>
        private sealed class KulamHexSigilAbility : HeroAbility
        {
            private const float HexRadius = 2.4f;
            private const float SigilLifetime = 6.0f;

            public KulamHexSigilAbility()
                : base("phaister_skill1", "KULAM HEX",
                       "Chalks a kulam circle on the road. Anyone who walks into it loses their footing.",
                       0.0f, SigilLifetime, AbilityGlyph.PhaisterHexSigil,
                       summary: "A circle on the ground. Walk in and you stumble.",
                       telegraphRadius: HexRadius,
                       telegraphRange: 4.5f,
                       castAction: "hero-phaister-hex",
                       viewmodelAction: "cast-hex",
                       charges: 2,
                       rechargedBy: Recharge.Never)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ HER OWN CAST VOICE. This ability had none at all, so the only thing marking
                // the cast was the ward's own `sfx_hex_cast`; every other hero in the game
                // grunts when they spend a skill. See `tools/generate_hero_audio.py` for why she
                // had no voice to play until now.
                NetCue.Play("hero_phaister_grunt", ctx.Position);

                var forwardAim = ctx.Forward;
                Vector3 targetPos = ctx.Position + forwardAim * 4.5f;
                int slot = ctx.Motor != null ? ctx.Motor.PlayerSlot : -1;

                HeroHazards.SpawnKulamHexSigil(targetPos, HexRadius, SigilLifetime, slot);
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
                       36.0f, 0.4f, AbilityGlyph.PhaisterShadowBlink,
                       summary: "Hold to aim, release to teleport. Shoves whoever you left.",
                       telegraphRadius: ArrivalMark,
                       telegraphRange: MaxRange,
                       castAction: "hero-phaister-blink",
                       viewmodelAction: "blink")
            {
                AimByHolding(MinRange, MaxRange, rampSeconds: 0.55f, maxHoldSeconds: 1.10f);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️ `sfx_ghost_teleport` IS NEMU'S AND IS KEPT DELIBERATELY. `docs/TODO.md`
                // § 21.4 took the borrowed cues off her ward and her ultimate; this one stays
                // because a blink IS the physical event Nemu's phase is, and two heroes who
                // share an element are allowed to share the sound of the one thing they both
                // literally do. What she needed was her own THROAT over it, which is the line
                // below. It is now a NETWORKED cue: see `NetCue`.
                NetCue.Play("hero_phaister_grunt", ctx.Position);
                NetCue.Play("sfx_ghost_teleport", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "BLINK!", UiTheme.HeroWitchBright, 1.20f);

                Vector3 startPos = ctx.Position;
                Vector3 facing = ctx.Forward;

                // Where the ring the player has been looking at for the last half second is.
                var system = ctx.Motor != null ? ctx.Motor.AbilitySystem : null;
                Vector3 destination = system != null
                    ? system.AimDestination(this)
                    : startPos + facing * AimRangeFor(HeldSecondsOnCast);

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
            /// weeks"*. The request is `MatchRpc.RequestBlinkShoveServerRpc`.
            /// </summary>
            private static void HostShove(CharacterMotor caster, Vector3 at, Vector3 facing)
            {
                if (caster == null) return;

                if (!NetAuthority.ShouldResolve())
                {
                    Net.MatchRpc.Instance?.RequestBlinkShoveServerRpc(
                        caster.PlayerSlot, at, facing);
                    return;
                }

                ResolveShove(caster.PlayerSlot, at, facing);
            }

            /// <summary>
            /// The host's half. Public so <see cref="Net.MatchRpc"/> can run it for a client.
            ///
            /// ⚠️ IT TAKES A SLOT RATHER THAN A MOTOR, because the host is resolving a request
            /// about a seat and must look the body up itself. A client that could hand over a
            /// `CharacterMotor` reference would be a client naming its own victims.
            /// </summary>
            public static void ResolveShove(int casterSlot, Vector3 at, Vector3 facing)
            {
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
        /// the 1.20 s floor back to `StunElement.None`, so the most expensive power in her kit
        /// applied a knockback hitch, drew no element coat, raised no mash card, and was
        /// unmashable and unnoticeable at the same time. It was left alone rather than retuned
        /// because it is a balance question; this is the answer, written down.
        ///
        /// ⚠️⚠️ **1.60 s, FIVE PRESSES, AND ONLY INSIDE THE REACH.** The three numbers together:
        ///
        ///   * **1.60 s** clears `Balance.MinStunDown` by 0.40, which is the smallest hold that
        ///     actually IS one. Anything at 1.20 or under is silently demoted and this entry is
        ///     the record of what that costs.
        ///   * **5 presses** against Cheska's 9, Dante's 8, Zack's 7, Nemu's 6 and Sean's 4.
        ///     `docs/TODO.md` § 23's rule is *"how hard the skill is supposed to hit"*, and the
        ///     thing that separates this from Cheska's nova is that it can hold **three people
        ///     at once**. A multi-target hold has to be shorter per victim than a single-target
        ///     one or it is three novas for one price. `perPress = (1.60 - 1.20) / 5 = 0.08 s`,
        ///     so a player who answers it is free in about 1.2 s against 1.6 unanswered.
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
            /// <summary>How far the curse reaches. The same number the ground ring draws.</summary>
            private const float Reach = 5.0f;

            /// <summary>See the class note. Must stay above `Balance.MinStunDown`.</summary>
            private const float CurseHold = 1.60f;

            /// <summary>Presses to break it. Fewer than every single-target hold in the game.</summary>
            private const int CurseBreakPresses = 5;

            public GrandCovenEclipseAbility()
                : base("phaister_ultimate", "GRAND COVEN",
                       "Pulls an eclipse over the street. Everyone caught under it is cursed where they stand until they fight it off.",
                       0.0f, 5.0f, AbilityGlyph.PhaisterEclipse,
                       summary: "Night falls. Everyone under it is held.",
                       telegraphRadius: Reach,
                       telegraphRange: 0.0f,
                       castAction: "hero-phaister-eclipse",
                       viewmodelAction: "coven-eclipse")
            {
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
                ComicPopup.Spawn(ctx.Position, "GRAND COVEN ECLIPSE!", UiTheme.HeroWitchBright, 2.0f);

                HeroHazards.SpawnGrandCovenEclipse(ctx.Position, Reach, Duration);
                AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.WitchEclipse, Duration);

                var kit = ctx.Motor.AbilitySystem?.Kit as PhaisterHeroKit;
                if (kit != null) kit.IsWitchfireInfused = true;

                Curse(ctx.Position, ctx.Motor != null ? ctx.Motor.PlayerSlot : -1);
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
                    AbilityVfx.AttachAura(p.transform, AbilityVfx.Aura.WitchEclipse, 2.5f);
                    ComicPopup.Spawn(p.transform.position + Vector3.up * 1.3f, "CURSED!",
                                     UiTheme.HeroWitchBright, 1.2f);
                }
            }
        }
    }
}
