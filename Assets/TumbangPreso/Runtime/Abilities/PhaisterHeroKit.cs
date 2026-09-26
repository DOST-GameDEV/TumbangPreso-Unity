using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER, VOODOO, IN THE NEW SHAPE (ABILITY-2, owner 2026-09-26: *"Voodoo To phaister"*, with
    /// the table and his answers). `docs/reports/ability-rework-2026-09-26/plan.md` § 3.5; numbers in
    /// `Core.VoodooRules`.
    ///
    /// | Slot | Name | Owner |
    /// |---|---|---|
    /// | Signature | SHADOW BLINK | *"Teleport"*, *"refine everything about shadow blink"* (the mechanic is kept; its presentation is the plan's § 5 pass) |
    /// | Attacking | CURSE: DISORIENTED | *"Fake screen/ fake slipper Aim. Make them hallucinate"*, *"hallucinations but make it so that some of the shit they see are real"* |
    /// | Defending | CURSE: VULNERABLE | *"easier to tag and phaister can go out of box and tag them"* |
    /// | Ultimate | HIGOP | *"casts a blackhole ... pulls everyone towards it. No button mashhing"*, *"really slowly cast"*, *"pulls players ands slipeprs except for her shit and no escape for entire duration but they can try to"* |
    ///
    /// ⚠️ HEX AND GRAND COVEN ARE REPLACED. `RitualBuildSeconds` stays because the introduction's warm-up
    /// (`PhaisterRitualWarmup`) still times itself by it; `CaptureCoven`/`RestoreCoven` keep their names
    /// because the rejoin snapshot (`MatchRpc`) calls them, and now carry the black hole.
    /// </summary>
    public sealed class PhaisterHeroKit : HeroKit
    {
        public const float RitualBuildSeconds = 1.55f;

        /// <summary>Kept for the throw path; nothing in the new kit infuses a throw.</summary>
        public bool IsWitchfireInfused { get; set; }

        /// <summary>Kept for the throw path; HIGOP does not charge her throws.</summary>
        public bool IsEclipseActive => false;

        public bool CaptureCoven(out Vector3 centre, out float preparation, out float remaining)
            => ((Higop)Ultimate).Capture(out centre, out preparation, out remaining);

        public void RestoreCoven(CharacterMotor motor, Vector3 centre, float preparation, float remaining)
        {
            if (motor == null || NetAuthority.ShouldResolve() ||
                float.IsNaN(preparation) || float.IsInfinity(preparation) ||
                float.IsNaN(remaining) || float.IsInfinity(remaining)) return;
            var context = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>(),
                centre, motor.transform.forward, centre);
            using (NetCue.SuppressRelay())
                ((Higop)Ultimate).Restore(context, centre, preparation, remaining);
        }

        public PhaisterHeroKit() : base("phaister", "PHAISTER")
        {
            Skill1 = new ShadowPhaseBlinkAbility();
            AttackingSkill = new CurseDisoriented();
            DefendingSkill = new CurseVulnerable();
            Ultimate = new Higop();
        }

        public override float UltimateCost => VoodooRules.HigopCost;

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
                : base("phaister_skill1", "SHADOW BLINK",
                       "Hold to pick a spot, let go and you are simply there. Whoever you left standing gets shoved back.",
                       VoodooRules.BlinkCooldown, 0.4f, AbilityGlyph.PhaisterShadowBlink,
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
                PhaisterArrivalSeal.Create(destination,facing);

                ctx.Motor.Teleport(destination);

                // ⚠️ THE ARRIVAL HAS ITS OWN CUE AND ITS OWN POSITION. `sfx_ghost_teleport` above
                // plays where she LEFT; up to 5.5 m of street later she simply appears, and until
                // 2026-08-26 that end of the ability made no sound at all. A cue fired at
                // `startPos` cannot cover it: `AudioDirector` parks a pooled voice at the point it
                // is given, which is the same fault `LrtTrainFlyby` records about a moving train.
                NetCue.Play("sfx_blink_arrive", destination);



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

                    other.ApplyResolvedImpact(repel.normalized * 6.0f + Vector3.up * 2.0f);
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

        // ================================================================== CURSE: DISORIENTED (attacking)

        private sealed class CurseDisoriented : HeroAbility
        {
            public override bool DefersPredictedEffect => true;

            public CurseDisoriented()
                : base("phaister_skill2", "CURSE: DISORIENTED",
                       "Attacking. Hold to aim, release to throw a cursed doll. Whoever it strikes sees things that are not there, mixed with things that are.",
                       VoodooRules.DisorientCooldown, 0.0f, AbilityGlyph.PhaisterHexSigil,
                       summary: "Throw a cursed doll. The one it hits hallucinates.",
                       telegraphRadius: VoodooRules.DollHitRadius, telegraphRange: VoodooRules.DollMaxRange,
                       castAction: "hero-phaister-hex", viewmodelAction: "cast-hex",
                       castCue: "sfx_cast_phaister_doll")
            {
                AimByHolding(3.0f, VoodooRules.DollMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Ward;
            }

            public override bool CanActivate(AbilityContext ctx) => base.CanActivate(ctx) && !ctx.Motor.IsDefender;

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_phaister_grunt", ctx.Position);
                Vector3 from = ctx.Position + Vector3.up * 1.5f + ctx.Forward * 0.4f;
                VoodooDoll.Spawn(from, AimedDestination(ctx), ctx.Motor.PlayerSlot);
            }
        }

        // ================================================================== CURSE: VULNERABLE (defending)

        private sealed class CurseVulnerable : HeroAbility
        {
            public CurseVulnerable()
                : base("phaister_skill2d", "CURSE: VULNERABLE",
                       "Defending. Drive a pin through the doll: every attacker in front of you is Vulnerable for 5 s. Taggable from further, and you may leave the box to tag them.",
                       VoodooRules.VulnerableCooldown, 0.0f, AbilityGlyph.PhaisterEclipse,
                       summary: "Curse the attackers in front of you. Tag them anywhere.",
                       telegraphRadius: VoodooRules.VulnerableConeRange * 0.5f, telegraphRange: VoodooRules.VulnerableConeRange * 0.5f,
                       castAction: "hero-phaister-hex", viewmodelAction: "cast-hex",
                       castCue: "sfx_cast_phaister_pin") { }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_phaister_grunt", ctx.Position);
                Vector3 fwd = ctx.Forward; fwd.y = 0.0f; fwd = fwd.sqrMagnitude > 0.001f ? fwd.normalized : Vector3.forward;
                VoodooConeFlash.Spawn(ctx.Position, fwd);
                var round = ctx.Round;
                if (round == null || !NetAuthority.ShouldResolve()) return;
                float half = VoodooRules.VulnerableConeDegrees * 0.5f;
                foreach (var p in round.Players)
                {
                    if (p == null || p == ctx.Motor || p.IsDefender) continue;
                    Vector3 d = p.transform.position - ctx.Position; d.y = 0.0f;
                    if (d.magnitude > VoodooRules.VulnerableConeRange || Vector3.Angle(fwd, d) > half) continue;
                    p.ApplyVulnerable();
                    MatchFlair.Announce(MatchFlair.Kind.HeroCursed, ctx.Motor.PlayerSlot, p.PlayerSlot, p.transform.position, StatusRules.VulnerableSeconds);
                }
            }
        }

        // ================================================================== HIGOP (ultimate)

        private sealed class Higop : HeroAbility
        {
            private GameObject _hole;
            private Vector3 _centre;

            public Higop()
                : base("phaister_ultimate", "HIGOP",
                       "Slowly call a black hole where you aim. For 5 s it swallows every other player and every slipper but yours toward its heart. Run, and it drags you back.",
                       0.0f, VoodooRules.HigopSeconds, AbilityGlyph.PhaisterEclipse,
                       summary: "A black hole drags every player and slipper to it.",
                       telegraphRadius: VoodooRules.HigopRadius, telegraphRange: VoodooRules.HigopMaxRange,
                       castAction: "hero-phaister-eclipse", viewmodelAction: "coven-eclipse",
                       castCue: "sfx_cast_phaister_higop")
            {
                TelegraphStyle = GroundReticle.Style.Ward;
                // ⚠️ THE SLOW CAST IS THE WIND-UP (owner: *"i want her to really slowly cast the black
                // whole"*): she is rooted while the power surges through her (her cast clip), and the
                // spot is marked for everyone to read.
                Windup = VoodooRules.HigopCastSeconds;
                AimByHolding(3.0f, VoodooRules.HigopMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
            }

            public override void Activate(AbilityContext ctx)
            {
                base.Activate(ctx);
                if (!IsWindingUp || ctx?.Motor == null) return;
                _centre = AimedDestination(ctx);
                if (_hole != null) UnityEngine.Object.Destroy(_hole);
                _hole = VoodooBlackHole.Spawn(_centre, ctx.Motor.PlayerSlot, Windup, Duration);
            }

            public bool Capture(out Vector3 centre, out float preparation, out float remaining)
            {
                centre = _centre; preparation = WindupRemaining; remaining = DurationRemaining;
                return IsWindingUp || IsActive;
            }

            public void Restore(AbilityContext ctx, Vector3 centre, float preparation, float remaining)
            {
                if (IsWindingUp || IsActive || (preparation <= 0 && remaining <= 0)) return;
                _centre = centre;
                preparation = Mathf.Clamp(preparation, 0, Windup);
                remaining = Mathf.Clamp(remaining, 0, Duration);
                if (preparation > 0) RestoreWindupClock(ctx, preparation);
                else RestoreLiveClock(remaining);
                if (_hole != null) UnityEngine.Object.Destroy(_hole);
                _hole = VoodooBlackHole.Spawn(centre, ctx.Motor != null ? ctx.Motor.PlayerSlot : -1, preparation, preparation > 0 ? Duration : remaining);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (!IntroductionVoiced) NetCue.Play("hero_phaister_ult", ctx.Position);
                NetCue.Play("sfx_phaister_higop_open", _centre);
                if (_hole == null && ctx?.Motor != null)
                {
                    _centre = AimedDestination(ctx);
                    _hole = VoodooBlackHole.Spawn(_centre, ctx.Motor.PlayerSlot, 0.0f, Duration);
                }
            }

            public override void Reset()
            {
                if (_hole != null) UnityEngine.Object.Destroy(_hole);
                _hole = null; base.Reset();
            }
        }
    }
}
