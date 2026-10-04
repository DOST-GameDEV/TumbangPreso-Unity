using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER, VOODOO, v3: THE OWNER'S OWN TABLE (HERO-10, 2026-09-27). *"the only one i like from last session is the
    /// teleport but that has to be improved too"*, then the table. `docs/reports/phaister-kit-2026-09-27/plan.md` section 9 has
    /// every beat on every layer; the numbers live in `Core.VoodooRules` (his quoted, the rest marked proposed).
    ///
    /// | Slot | Name | Owner |
    /// |---|---|---|
    /// | Passive | VOODOO | *"Whenever Phaister marks someone she takes 10% of their speed and they slow down by 10%"* (`CharacterMotor.VoodooSpeedScale`) |
    /// | Signature | TELEPORT | *"Teleport to the target location instantly."*, 35 s |
    /// | Attacking | CURSE: DRAIN | *"Mark a person with a curse ... After a 1.5 seconds delay, inflict Drained"*, 35 s |
    /// | Defending | CURSE: HEX | *"Mark a person with a curse ... After 10 seconds it can be recast to inflict Hex"*, 35 s |
    /// | Ultimate | VOODOO DOLL | *"The voodoo doll becomes a sentient being that assists you in attacking or defending for the rest of the round."*, 12 points (`VoodooDollBody`) |
    ///
    /// ⚠️ BOTH CURSES MARK BY THE REACH, NOT A THROW (owner: *"actually dont throw needle to mark them"*, *"i want her to just hold
    /// her hand out towards someone for like 2 seconds or smth and thats marked"*). The body owns the reach and the mark
    /// (`CharacterMotor.Voodoo.cs`, host-resolved, replicated in `SyncUnit`); these abilities only start it and answer its end.
    /// MANIKA MISCHIEF, SPOTLIGHT PIN and OMEN are retired; their effect classes stay for their era of films and are not cast.
    /// </summary>
    public sealed class PhaisterHeroKit : HeroKit
    {
        public const float RitualBuildSeconds = 1.55f;

        /// <summary>Kept for the throw path; nothing in the new kit infuses a throw.</summary>
        public bool IsWitchfireInfused { get; set; }

        /// <summary>Kept for the throw path; HIGOP does not charge her throws.</summary>
        public bool IsEclipseActive => false;

        public PhaisterHeroKit() : base("phaister", "SORAYA")
        {
            Skill1 = new Teleport();
            AttackingSkill = new CurseDrain();
            DefendingSkill = new CurseHex();
            Ultimate = new VoodooDoll();
        }

        /// <summary>VOODOO DOLL, *"12 Objective Points"*.</summary>
        public override float UltimateCost => VoodooRules.DollCost;

        /// <summary>
        /// Whom her curse would reach from here: of the players `CharacterMotor.VoodooReachIsValid` lets her start on (in reach,
        /// in her aim's cone, in sight), the one nearest her facing, the nearer on a tie. Null when there is nobody, which refuses
        /// the press rather than spending 35 s on an empty hand. A body a curse cannot land on (Geo's Shield) is skipped.
        /// Read by the cast on the host, by her own screen's check before it asks, and by the bots.
        /// </summary>
        public static CharacterMotor ReachTargetFor(CharacterMotor caster, Vector3 facing)
        {
            var round = GameServices.Round;
            if (caster == null || round == null) return null;
            facing.y = 0.0f;
            if (facing.sqrMagnitude < 0.0001f) facing = caster.transform.forward;
            facing.y = 0.0f;

            CharacterMotor best = null;
            float bestAngle = float.MaxValue, bestDistance = float.MaxValue;
            foreach (var p in round.Players)
            {
                if (p == null || p == caster || p.RefusesVoodoo || !caster.VoodooReachIsValid(p, starting: true)) continue;
                Vector3 d = p.transform.position - caster.transform.position; d.y = 0.0f;
                float distance = d.magnitude;
                float angle = distance < 0.05f ? 0.0f : Vector3.Angle(facing, d);
                // Two players a degree apart are the same aim; the nearer is the one she means.
                bool better = angle < bestAngle - 1.0f || (angle <= bestAngle + 1.0f && distance < bestDistance);
                if (!better) continue;
                best = p; bestAngle = angle; bestDistance = distance;
            }
            return best;
        }

        // ================================================================== TELEPORT (signature)

        private sealed class Teleport : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;

            /// <summary>The mark she leaves on arrival, and what the reticle promises.</summary>
            private const float ArrivalMark = 1.15f;

            public Teleport()
                : base("phaister_skill1", "TELEPORT",
                       "Hold to pick a spot and let go: a swarm of moths carries you there at once, slipper still in hand.",
                       VoodooRules.TeleportCooldown, 0.4f, AbilityGlyph.PhaisterShadowBlink,
                       summary: "Aim a spot, let go, and you are there.",
                       telegraphRadius: ArrivalMark,
                       telegraphRange: VoodooRules.TeleportMaxRange,
                       castAction: "hero-phaister-swarm",
                       viewmodelAction: "swarm-burst",
                       castCue: "sfx_cast_phaister_blink")
            {
                // ⚠️⚠️ `maxHoldSeconds: 0` MEANS THE RELEASE IS THE ONLY THING THAT CASTS IT. It was 1.10 s and it fired itself
                // at the ceiling; 🧑 2026-08-27, having played it: *"u cant control the E of phaister and it autocasts after
                // some seconds, i want it to cast only when i let go"*. The reach still stops growing at 0.55 s, so holding
                // longer buys nothing, and she is neither rooted nor exempt from the anti-camp clock while she aims.
                AimByHolding(VoodooRules.TeleportMinRange, VoodooRules.TeleportMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);

                // The one power that aims at a place she will be standing, which is why `AimBeacon` exists.
                AimBeacon = true;
                // HERO-10 (film v7: holding it showed nothing and she just stood): her own sigil where she will land, three moths
                // circling it (`PhaisterAimSigil`), and her tell, wrists crossed at her chest with moths crawling from her cuffs.
                AimPoseAction = "hero-phaister-swarm-aim";
            }

            private PhaisterAimSigil _sigil;
            private PhaisterCuffMoths _cuffs;
            public override bool DrawsOwnAim => true;

            public override void PresentAim(CharacterMotor caster, Vector3 at, float heldSeconds)
            {
                if (_sigil == null) _sigil = PhaisterAimSigil.Create(PhaisterAimSigil.Kind.Arrival);
                _sigil.Show(caster, at);
            }

            public override void PresentAimBody(CharacterMotor caster, float heldSeconds)
            {
                if (_cuffs == null) _cuffs = PhaisterCuffMoths.On(caster);
            }

            public override void EndAim()
            {
                if (_sigil != null) _sigil.Release();
                _sigil = null;
            }

            public override void EndAimBody()
            {
                if (_cuffs != null) _cuffs.Release();
                _cuffs = null;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // Her departure sounds centrally off `HeroAbility.CastCue` (`sfx_cast_phaister_blink`); the arrival has its own
                // cue at its own position below, because a cue parked where she left cannot cover a spot 5.5 m away.
                NetCue.Play("hero_phaister_grunt", ctx.Position);

                Vector3 startPos = ctx.Position;
                Vector3 facing = ctx.Forward;
                Vector3 destination = AimedDestination(ctx);

                // She bursts into moths here, they stream along the aim and knit her back at the far end (`PhaisterSwarm`); the
                // arrival sigil burns out under her. Every peer runs this cast, so every screen sees the same act.
                PhaisterSwarm.Play(ctx.Motor.transform, startPos, destination, facing);
                PhaisterArrivalSeal.Create(destination, facing);

                ctx.Motor.Teleport(destination);
                NetCue.Play("sfx_phaister_swarm_knit", destination);

                // ⚠️ NO SHOVE AT THE DEPARTURE ANY MORE (plan 9.6): *"Teleport to the target location instantly."* is the whole
                // of the owner's text, and the 2.5 m knockback VANISHING ACT carried was not in it. Its host-side resolver is
                // deleted with it, so there is no second path that could still move another body.
            }
        }

        // ================================================================== THE CURSES (role abilities)

        /// <summary>
        /// ⚠️⚠️ WHAT BOTH CURSES SHARE: ONE PRESS STARTS A 2 s REACH AT THE PLAYER SHE FACES, AND THE BODY DOES THE REST.
        ///
        /// A tap, not a hold (plan 9.5): a phone's aiming thumb is the one that would have to hold it. The press is refused when
        /// `ReachTargetFor` finds nobody. The HOST picks the target again from its own copy of the bodies and calls
        /// `CharacterMotor.HostBeginVoodooReach`; from there the body runs the reach, breaks it (range, her aim, her sight, a stun)
        /// or turns it into a mark, and replicates all of it. Every peer then hears how it ended through
        /// `CharacterMotor.VoodooReachEnded`, which is where this answers:
        ///
        ///  * MARKED: the curse is on them. DRAIN needs nothing more (the body drains them 1.5 s later); HEX waits for its recast.
        ///  * BROKEN: the effect ends and HALF THE COOLDOWN comes back (`VoodooRules.ReachBrokenRefund`, proposed), on every peer
        ///    at once, so the owner (whose cooldown the host may never lower, `HeroAbility.ApplyNetworkSnapshot`) and the host agree.
        ///
        /// ⚠️ A PEER CAN MISS THE WHOLE REACH. One that starts and snaps inside one `SyncUnit` interval (the target already at the
        /// edge of her cone) is never seen reaching by a client, so no event fires there. After the reach's own length plus a
        /// grace the ability reads the caster's replicated result (`VoodooReachSucceeded`) instead, which the host sets on every
        /// end, so a missed reach settles the same way a seen one does.
        ///
        /// HOST-CONFIRMED: the owner's screen waits for the host before it believes a reach started, because the host alone knows
        /// whether its copy of the target was still in reach.
        /// </summary>
        private abstract class Curse : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;

            /// <summary>How long past the reach's own 2 s a peer that never saw it waits before reading the result.</summary>
            private const float MissedReachGrace = 0.75f;

            private readonly VoodooMarkKind _kind;
            private CharacterMotor _caster;
            protected int CasterSlot = -1;
            private bool _sawReach, _settled;
            protected bool Marked { get; private set; }
            private float _sinceStart;

            protected Curse(VoodooMarkKind kind, string id, string name, string description, float cooldown, float duration,
                            AbilityGlyph glyph, string summary, string castAction, string viewmodelAction, string castCue)
                : base(id, name, description, cooldown, duration, glyph, summary: summary,
                       castAction: castAction, viewmodelAction: viewmodelAction, castCue: castCue)
            {
                _kind = kind;
            }

            /// <summary>Every peer, the moment her reach turns into a mark (a curse's own follow-through).</summary>
            protected virtual void OnMarked(CharacterMotor caster) { }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ReachTargetFor(ctx.Motor, ctx.Forward) != null;

            public override void Activate(AbilityContext ctx)
            {
                Follow(null);
                if (ctx?.Motor != null) CasterSlot = ctx.Motor.PlayerSlot;
                _sawReach = _settled = false;
                Marked = false;
                _sinceStart = 0.0f;
                base.Activate(ctx);
            }

            public override void Tick(AbilityContext ctx, float dt)
            {
                if (ctx?.Motor != null) CasterSlot = ctx.Motor.PlayerSlot;
                base.Tick(ctx, dt);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                Follow(ctx.Motor);
                NetCue.Play("hero_phaister_grunt", ctx.Position);
                if (!NetAuthority.ShouldResolve()) return;
                var target = ReachTargetFor(ctx.Motor, ctx.Forward);
                // `CanActivate` asked the same question this frame, so this is only a guard: nothing to reach is a broken reach.
                if (target == null || !ctx.Motor.HostBeginVoodooReach(_kind, target)) Settle(marked: false);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_caster == null || _settled) return;
                _sinceStart += dt;
                if (_caster.IsVoodooReaching && _caster.VoodooReachKind == _kind) { _sawReach = true; return; }
                // A reach this peer saw ends through the event; one it never saw is read off the host's result.
                if (!_sawReach && _sinceStart >= VoodooRules.ReachSeconds + MissedReachGrace)
                    Settle(_caster.VoodooReachSucceeded);
            }

            /// <summary>0 to 1 through the reach on this peer, for her tile.</summary>
            public float ReachProgress => _caster != null && _caster.IsVoodooReaching ? _caster.VoodooReachProgress : Marked ? 1.0f : 0.0f;

            private void Follow(CharacterMotor caster)
            {
                if (_caster == caster) return;
                if (_caster != null) _caster.VoodooReachEnded -= OnReachEnded;
                _caster = caster;
                if (_caster != null) _caster.VoodooReachEnded += OnReachEnded;
            }

            private void OnReachEnded(CharacterMotor body, bool marked)
            {
                if (body != _caster || _settled || !IsActive) return;
                Settle(marked);
            }

            private void Settle(bool marked)
            {
                if (_settled) return;
                _settled = true;
                Marked = marked;
                if (marked) { if (_caster != null) OnMarked(_caster); return; }
                // ⚠️ THE REFUND IS TAKEN OFF WHAT IS LEFT, NOT SET: a reach broken late has already run down 2 s of the cooldown,
                // and every peer takes the same amount off at the same event.
                CooldownRemaining = Mathf.Max(0.0f, CooldownRemaining - Cooldown * VoodooRules.ReachBrokenRefund);
                EndEarly(null);
            }

            /// <summary>The body her curse of this kind is on, or null.</summary>
            protected CharacterMotor OwnMark()
            {
                var round = GameServices.Round;
                if (round == null || CasterSlot < 0) return null;
                foreach (var p in round.Players)
                    if (p != null && p.VoodooMark == _kind && p.VoodooMarkSource == CasterSlot) return p;
                return null;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                Follow(null);
                _settled = true;
            }

            public override void Reset()
            {
                Follow(null);
                _settled = true;
                Marked = false;
                base.Reset();
            }
        }

        // ================================================================== CURSE: DRAIN (attacking)

        /// <summary>
        /// *"Mark a person with a curse and attach their soul to the voodoo doll. After a 1.5 seconds delay, inflict Drained"*.
        /// DRAINED is the owner's status: *"Depletes stamina to 0. Prevents stamina recovery for 2.5 seconds."* The body does the
        /// delay and the drain (`CharacterMotor.StepVoodoo`); this ability lives as long as the reach plus that delay.
        /// </summary>
        private sealed class CurseDrain : Curse
        {
            public CurseDrain()
                : base(VoodooMarkKind.Drain, "phaister_skill2", "CURSE: DRAIN",
                       "Attacking. Reach for the player you face for 2 s to curse them. 1.5 s later they are Drained: no stamina, no refill.",
                       VoodooRules.DrainCooldown, VoodooRules.ReachSeconds + VoodooRules.DrainDelaySeconds,
                       AbilityGlyph.PhaisterCursedDoll,
                       "Curse a player; 1.5 s later their stamina is gone.",
                       castAction: "hero-phaister-drain", viewmodelAction: "reach-drain",
                       castCue: "sfx_cast_phaister_drain") { }

            /// <summary>
            /// Plan 9.5, the wind-up: *"both hands on the doll of them, twisting it tighter and tighter (she keeps walking)"*, the
            /// last hard wring on the 1.5 s the body drains them. Both hands, so the slipper stays at her belt through it.
            /// </summary>
            protected override void OnMarked(CharacterMotor caster)
            {
                caster.HoldSlipperAtBelt(VoodooRules.DrainDelaySeconds + 0.35f);
                caster.GetComponentInChildren<CharacterAnimator>()?.PlayAction("hero-phaister-wring", "wring");
                GameServices.Audio?.PlayAt("sfx_phaister_wring", caster.transform.position);
            }
        }

        // ================================================================== CURSE: HEX (defending)

        /// <summary>
        /// *"Mark a person with a curse and attach their soul to the voodoo doll. After 10 seconds it can be recast to inflict
        /// Hex"*. HEXED: *"Hallucinations of slippers randomly appear on your screen for 7.5 seconds."*
        ///
        /// ⚠️⚠️ THE RECAST IS A REACTIVATION, AND THAT IS WHY IT WORKS WHILE THE 35 s COOLDOWN RUNS. The cooldown is spent at the
        /// press like any power's; the ability stays ACTIVE for the reach plus the mark's life (`VoodooRules.HexMarkLifeSeconds`),
        /// and `HeroKit.Fire` routes a press on an active `CanReactivate` power to `Reactivate` without asking the cooldown.
        /// `ReactivateReady` is true only while her armed mark is on somebody, so a press before 10 s is NOT YET and the deck
        /// counts down to it (`ReactivateReadyIn`). The recast sets the hex off through `CharacterMotor.HostDetonateHex` on the
        /// host and never restarts the cooldown. When the mark is gone (set off, frayed away at 25 s, cleansed) the power ends.
        /// </summary>
        private sealed class CurseHex : Curse
        {
            /// <summary>How long her mark may be missing on this peer before the power ends (a snapshot's lag, not a rule).</summary>
            private const float MarkGoneGrace = 0.5f;

            private float _markGoneFor;

            public CurseHex()
                : base(VoodooMarkKind.Hex, "phaister_skill2d", "CURSE: HEX",
                       "Defending. Reach for the player you face for 2 s to curse them. From 10 s on, press again: they see phantom slippers.",
                       VoodooRules.HexCooldown, VoodooRules.ReachSeconds + VoodooRules.HexMarkLifeSeconds,
                       AbilityGlyph.PhaisterVulnerable,
                       "Curse a player; after 10 s, recast to haunt their eyes.",
                       castAction: ReachAction, viewmodelAction: ReachView, castCue: ReachCue) { }

            // ⚠️ TWO PRESSES, TWO BODIES (Paete's BAKYA BLOOM is the precedent): the first press is the reach, the recast is the
            // stab. `HeroAbilitySystem.PlayCastConfirm` reads these AFTER `Activate`/`Reactivate` return, on every peer.
            private const string ReachAction = "hero-phaister-hexreach", ReachView = "reach-hex", ReachCue = "sfx_cast_phaister_hexreach";
            private const string StabAction = "hero-phaister-hexstab", StabView = "hex-stab", StabCue = "sfx_cast_phaister_hexstab";

            public override bool CanReactivate => true;

            public override bool ReactivateReady
            {
                get { var marked = OwnMark(); return marked != null && marked.VoodooHexArmed; }
            }

            public override float ReactivateReadyIn
            {
                get
                {
                    var marked = OwnMark();
                    if (marked != null) return Mathf.Max(0.0f, VoodooRules.HexArmSeconds - marked.VoodooMarkAge);
                    return Marked ? 0.0f : VoodooRules.ReachSeconds * (1.0f - ReachProgress) + VoodooRules.HexArmSeconds;
                }
            }

            public override void Activate(AbilityContext ctx)
            {
                CastAction = ReachAction; ViewmodelAction = ReachView; CastCue = ReachCue;
                _markGoneFor = 0.0f;
                base.Activate(ctx);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                base.OnTick(ctx, dt);
                if (!Marked) return;
                // Her mark frayed away, was cleansed, or was set off by a recast on another peer's word: the power is over.
                _markGoneFor = OwnMark() != null ? 0.0f : _markGoneFor + dt;
                if (_markGoneFor >= MarkGoneGrace) DurationRemaining = 0.0f;
            }

            public override void Reactivate(AbilityContext ctx)
            {
                CastAction = StabAction; ViewmodelAction = StabView; CastCue = StabCue;
                // The stab takes both hands (the doll up at her face, the pin into its eye).
                ctx?.Motor?.HoldSlipperAtBelt(0.6f);
                if (ctx?.Motor != null) CasterSlot = ctx.Motor.PlayerSlot;
                // ⚠️ NO HEXED FLAIR (film v12): `MatchFlair`'s hero hit draws dizzy stars for the whole 7.5 s and a comic word at
                // the victim's head, which on their own screen is a block in their lens. HEXED is shown by its own pictures: the
                // stab, the band across their eyes for everyone else, the phantoms on their screen, its badge and its sound.
                var victim = OwnMark();
                if (NetAuthority.ShouldResolve() && victim != null) victim.HostDetonateHex(CasterSlot);
                EndEarly(ctx);
            }
        }

        // ================================================================== VOODOO DOLL (ultimate)

        /// <summary>
        /// ⚠️⚠️ VOODOO DOLL (HERO-10 v3, the owner's table): *"The voodoo doll becomes a sentient being that assists you in attacking
        /// or defending for the rest of the round."*, 12 objective points (`VoodooRules.DollCost`). It replaces OMEN (the black eye),
        /// keeping the id `phaister_ultimate`. After the shared introduction (the cutscene every peer watches) the HOST stands the
        /// doll up beside her (`VoodooDollBody.HostSpawn`: her companion seat, a Hard AI, its own slipper when she attacks); every
        /// other peer receives it through `CompanionSet`. It lives until the round ends. Nothing is aimed: it wakes where she is.
        /// A second cast while it lives does nothing new (`HostSpawn` hands back the one she has), so the meter is not spent twice:
        /// `CheckUltimate` refuses while her doll stands.
        ///
        /// Owner rules for its look (plan 9.2): it is its OWN character; she never dies, faints or controls it. When it wakes THE
        /// CIRCLE opens in the sky over it (*"a big magic circle in the sky or smth when she ults"*), drawn by the doll's body on
        /// every peer (`Visual.VoodooSkyCircle`).
        /// </summary>
        private sealed class VoodooDoll : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;

            public VoodooDoll()
                : base("phaister_ultimate", "VOODOO DOLL",
                       "Your voodoo doll wakes beside you as its own fighter for the round, with its own slipper. Its points are yours.",
                       // Its own glyph: the doll hung on three strings from THE CIRCLE (it borrowed OMEN's eclipse until 2026-09-29).
                       0.0f, 0.0f, AbilityGlyph.PhaisterVoodooDoll,
                       summary: "Wake the voodoo doll to fight on your side for the round.",
                       castAction: "hero-phaister-omen", viewmodelAction: "omen-rise")
            {
            }

            /// <summary>
            /// Refused while her doll already stands (one companion per player). ⚠️ Asked by the HOST before it accepts a cast, and
            /// never of a cast already reserved for its introduction: a peer running the accepted cast may receive the host's
            /// `CompanionSet` first, and must still play it.
            /// </summary>
            public override bool CanActivate(AbilityContext ctx)
            {
                var round = GameServices.Round;
                if (NetAuthority.ShouldResolve() && !ReservedForIntroduction && ctx?.Motor != null && round != null
                    && round.BodyAt(CompanionSeats.For(ctx.Motor.PlayerSlot)) != null) return false;
                return base.CanActivate(ctx);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                if (!IntroductionVoiced) NetCue.Play("hero_phaister_ult", ctx.Position);
                // The host decides; every other peer gets the body from `CompanionSet`.
                if (NetAuthority.ShouldResolve()) VoodooDollBody.HostSpawn(ctx.Motor);
            }
        }
    }
}
