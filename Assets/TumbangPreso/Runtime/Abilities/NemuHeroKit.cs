using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ NEMU, NECRO, IN THE NEW SHAPE (ABILITY-2, owner 2026-09-26: *"NECRO maps to nemu"*, the table and
    /// his answers). `docs/reports/ability-rework-2026-09-26/plan.md` § 3.4; numbers in `Core.NecroRules`.
    /// KURO IS THE KIT: every ability sends her ghost pet somewhere (`GhostPetCompanion.BeginErrand`).
    ///
    /// | Slot | Name | Owner |
    /// |---|---|---|
    /// | Signature | TERRIFY | *"Leave Kuro somewhere and everyone there gets feared"*; Feared = *"Flee from kuro and drop slipper"* |
    /// | Attacking | KURO FETCH | *"Kuro Slipper retrieve"*; the taya can intercept |
    /// | Defending | KURO GUARD | *"kuro aids withh blocking and becomes a bit bigger"*, *"give her like an AI to think abt where to stand but dont make it infallible"* |
    /// | Ultimate | KURO PLAYS (NOT YET) | *"HARD BOT and fulfills whatever role u have and ghets separate copy of ur skills"* |
    ///
    /// ⚠️⚠️ THE ULTIMATE IS STILL DEVOURING SEANCE. KURO PLAYS is a fifth unit in a four-seat match (a
    /// seatless motor, a bot brain, scoring routed to her seat, a second taya body, a snapshot for a
    /// rejoiner) and the plan gives it its own design pass; building it blind would put a broken seat in
    /// every match. It is the next Necro item in TODO ABILITY-2.
    /// </summary>
    public sealed class NemuHeroKit : HeroKit
    {
        /// <summary>Kept for the wire and the tag rule: Phantom Veil is gone, so it is never active.</summary>
        public bool IsPhantomPhaseActive => false;

        /// <summary>Phantom Veil is gone; a rejoiner has no veil to restore.</summary>
        public bool RestoreJoiningVeil(CharacterMotor motor, float remaining) => false;

        public override float MovementSpeedScale => 1f;

        public void RestoreFamiliar(CharacterMotor motor, int mode, Vector3 position, float remaining, float? yaw = null)
        {
            // Mode 1 was Astral Hijack's projection, which the new kit does not have; mode 2 is the seance.
            if (motor == null || remaining <= 0 || mode != 2) return;
            var ctx = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>());
            using (NetCue.SuppressRelay())
                ((NightmareSeanceVoidAbility)Ultimate).RestoreSeance(ctx, position, remaining);
            if (yaw.HasValue && !float.IsNaN(yaw.Value) && !float.IsInfinity(yaw.Value))
            {
                var pet = motor.GetComponent<CharacterVisual>()?.Companion;
                if (pet != null) pet.transform.rotation = Quaternion.Euler(0, yaw.Value, 0);
            }
        }

        public NemuHeroKit() : base("nemu", "NEMU")
        {
            Skill1 = new Terrify();
            AttackingSkill = new KuroFetch();
            DefendingSkill = new KuroGuard();
            Ultimate = new NightmareSeanceVoidAbility();
        }

        /// <summary>The seance's price (unchanged until KURO PLAYS replaces it).</summary>
        public override float UltimateCost => 10.0f;

        private static GhostPetCompanion Kuro(AbilityContext ctx) => ctx?.Motor?.GetComponent<CharacterVisual>()?.Companion;

        // ================================================================== TERRIFY (signature)

        private sealed class Terrify : HeroAbility
        {
            private Vector3 _spot;
            private readonly HashSet<int> _feared = new HashSet<int>();
            private GhostPetCompanion _kuro;

            public Terrify()
                : base("nemu_skill1", "TERRIFY",
                       "Hold to aim, release to leave Kuro haunting a spot for 4 s. Anyone who comes near him drops their slipper and runs in terror.",
                       NecroRules.TerrifyCooldown, NecroRules.TerrifyHauntSeconds, AbilityGlyph.NemuSeanceVoid,
                       summary: "Kuro haunts a spot. Whoever comes near is Feared.",
                       telegraphRadius: NecroRules.TerrifyRadius, telegraphRange: NecroRules.TerrifyMaxRange,
                       castAction: "hero-nemu-project", viewmodelAction: "project-spirit",
                       castCue: "sfx_cast_nemu_hijack")
            {
                AimByHolding(2.0f, NecroRules.TerrifyMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Maw;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _spot = AimedDestination(ctx);
                _feared.Clear();
                _kuro = Kuro(ctx);
                Vector3 spot = _spot;
                _kuro?.BeginErrand(() => spot, 11.0f, 1.25f);
                NetCue.Play("sfx_ghost_teleport", _spot);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!NetAuthority.ShouldResolve()) return;
                var round = ctx?.Round;
                if (round == null) return;
                foreach (var p in round.Players)
                {
                    if (p == null || p == ctx.Motor || _feared.Contains(p.PlayerSlot)) continue;
                    Vector3 d = p.transform.position - _spot; d.y = 0.0f;
                    if (d.magnitude > NecroRules.TerrifyRadius) continue;
                    _feared.Add(p.PlayerSlot);
                    p.ApplyFeared(_spot);
                    MatchFlair.Announce(MatchFlair.Kind.HeroHit, ctx.Motor.PlayerSlot, p.PlayerSlot, p.transform.position, 1.5f);
                }
            }

            protected override void OnEnd(AbilityContext ctx) { _kuro?.EndErrand(); _kuro = null; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== KURO FETCH (attacking)

        private sealed class KuroFetch : HeroAbility
        {
            private Slipper _shoe;
            private GhostPetCompanion _kuro;
            private bool _carrying;
            private float _nextBroadcast;

            public KuroFetch()
                : base("nemu_skill2", "KURO FETCH",
                       "Attacking. Kuro flies to your slipper and brings it back to your hand. If the taya tags him on the way, he drops it.",
                       NecroRules.FetchCooldown, 8.0f, AbilityGlyph.NemuAstralPet,
                       summary: "Kuro fetches your slipper. The taya can make him drop it.",
                       castAction: "hero-nemu-project", viewmodelAction: "project-spirit",
                       castCue: "sfx_cast_nemu_hijack") { }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx) || ctx.Motor.IsDefender || ctx.Motor.HoldingSlipper) return false;
                return OwnLooseSlipper(ctx.Motor) != null;
            }

            private static Slipper OwnLooseSlipper(CharacterMotor who)
            {
                foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                    if (s != null && s.OwnerSlot == who.PlayerSlot && s.State == SlipperState.Loose) return s;
                return null;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _shoe = OwnLooseSlipper(ctx.Motor);
                _kuro = Kuro(ctx);
                _carrying = false;
                var shoe = _shoe;
                var owner = ctx.Motor;
                _kuro?.BeginErrand(() => shoe == null ? owner.transform.position
                                         : (_carrying ? owner.transform.position + Vector3.up * 0.4f : shoe.transform.position),
                                   NecroRules.FetchSpeed, 1.0f);
                NetCue.Play("sfx_ghost_teleport", ctx.Position);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_shoe == null || _kuro == null) { DurationRemaining = 0.0f; return; }
                if (!_carrying)
                {
                    if (_shoe.State != SlipperState.Loose) { DurationRemaining = 0.0f; return; }
                    if (_kuro.ErrandArrived) { _carrying = true; NetCue.Play("sfx_possess_enter", _kuro.transform.position); }
                    return;
                }
                if (!NetAuthority.ShouldResolve()) return;
                // Carried: the host walks the (loose) slipper under Kuro and tells everyone where it is.
                Vector3 at = _kuro.transform.position + Vector3.down * 0.35f;
                _shoe.transform.position = at;
                if (Time.time >= _nextBroadcast) { _nextBroadcast = Time.time + 0.1f; Net.MatchRpc.Instance?.BroadcastSlipperState(_shoe); }
                // THE INTERCEPT (owner: the taya can stop him): a taya who can act and is on him makes him drop it.
                var round = ctx?.Round;
                if (round != null)
                    foreach (var p in round.Players)
                        if (p != null && p.IsDefender && p.CanAct() &&
                            (p.transform.position - at).sqrMagnitude < NecroRules.FetchInterceptRadius * NecroRules.FetchInterceptRadius)
                        {
                            _shoe.transform.position = new Vector3(at.x, Slipper.GroundY(at) + 0.05f, at.z);
                            Net.MatchRpc.Instance?.BroadcastSlipperState(_shoe);
                            MatchFlair.Announce(MatchFlair.Kind.Block, ctx.Motor.PlayerSlot, p.PlayerSlot, at, 4f);
                            DurationRemaining = 0.0f;
                            return;
                        }
                if ((ctx.Motor.transform.position - at).sqrMagnitude < 1.2f * 1.2f)
                {
                    _shoe.HostForceEquip(ctx.Motor);
                    Net.MatchRpc.Instance?.BroadcastSlipperState(_shoe);
                    DurationRemaining = 0.0f;
                }
            }

            protected override void OnEnd(AbilityContext ctx) { _kuro?.EndErrand(); _kuro = null; _shoe = null; _carrying = false; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== KURO GUARD (defending)

        private sealed class KuroGuard : HeroAbility
        {
            private GhostPetCompanion _kuro;
            private Vector3 _spot, _pending;
            private float _think, _react;
            private readonly HashSet<Slipper> _blocked = new HashSet<Slipper>();

            public KuroGuard()
                : base("nemu_skill2d", "KURO GUARD",
                       "Defending. Kuro grows and guards the can for 6 s, moving to block the throws he sees coming. He is quick, not perfect.",
                       NecroRules.GuardCooldown, NecroRules.GuardSeconds, AbilityGlyph.NemuPhase,
                       summary: "Kuro grows and blocks throws at the can.",
                       castAction: "hero-nemu-seance", viewmodelAction: "seance-channel",
                       castCue: "sfx_cast_nemu_seance") { }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kuro = Kuro(ctx);
                _blocked.Clear();
                var lata = ctx.Round?.Lata;
                _spot = _pending = lata != null ? lata.transform.position + Vector3.forward * 1.2f : ctx.Position;
                _think = 0.0f; _react = 0.0f;
                _kuro?.BeginErrand(() => _spot, NecroRules.GuardMoveSpeed, NecroRules.GuardScale);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                var round = ctx?.Round;
                var lata = round?.Lata;
                if (lata == null || _kuro == null) return;
                // ⚠️ THE FALLIBLE AI (owner: *"dont make it infallible"*): he re-decides every 0.35 s and acts
                // on it 0.25 s later, standing between the can and whichever attacker holding a slipper is
                // closest to it. He never predicts a curve and never sees a throw before it leaves the hand.
                _think -= dt;
                if (_think <= 0.0f)
                {
                    _think = NecroRules.GuardThinkSeconds;
                    CharacterMotor threat = null; float best = float.MaxValue;
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.IsDefender || !p.HoldingSlipper) continue;
                        float dd = (p.transform.position - lata.transform.position).sqrMagnitude;
                        if (dd < best) { best = dd; threat = p; }
                    }
                    Vector3 toward = threat != null ? threat.transform.position - lata.transform.position : ctx.Motor.transform.forward;
                    toward.y = 0.0f;
                    _pending = lata.transform.position + (toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.forward) * 1.4f;
                    _react = NecroRules.GuardReactSeconds;
                }
                if (_react > 0.0f) { _react -= dt; if (_react <= 0.0f) _spot = _pending; }

                if (!NetAuthority.ShouldResolve()) return;
                float reach = NecroRules.GuardBlockRadius * NecroRules.GuardScale;
                foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                {
                    if (s == null || s.State != SlipperState.InFlight || _blocked.Contains(s)) continue;
                    if ((s.transform.position - _kuro.transform.position).sqrMagnitude > reach * reach) continue;
                    _blocked.Add(s);
                    Vector3 away = s.transform.position - _kuro.transform.position; away.y = 0.0f;
                    s.Deflect((away.sqrMagnitude > 0.01f ? away.normalized : -s.Velocity.normalized) * Balance.LaunchSpeed * Balance.DeflectSpeedScale, 1.0f);
                    NetCue.PlayImpact("hit_body", "guard_block", s.transform.position, 0.8f);
                }
            }

            protected override void OnEnd(AbilityContext ctx) { _kuro?.EndErrand(); _kuro = null; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            /// <summary>Where it opens when Kuro is not out. Her own reach, as before.</summary>
            private GameObject _field;
            private GhostPetCompanion _familiar;
            private const float FallbackRange = 3.5f;
            private Vector3 _castAnchor,_approachStart;
            private Quaternion _castFacing;
            private bool _approaching;

            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "DEVOURING SEANCE",
                       "Send your familiar ahead as a giant, pulling rivals and loose slippers inward. While possessed, it transforms in place.",
                       0.0f, 7.0f, TumbangPreso.UI.AbilityGlyph.NemuSeanceVoid,
                       summary: "Send a giant spirit ahead to pull rivals and slippers inward.",
                       // ⚠️⚠️ 2.8 m, DOWN FROM 3.2, AND THE 0.4 m BUYS THE BOTS BACK.
                       // `AiTuning.HazardAvoidMaxRadius` is 3.0 and this was the ONE registered
                       // hazard in the game above it, so it was the one thing the bots were
                       // told to walk straight through rather than around. Its own note says
                       // *"when the ability footprints come down, every hazard falls under this
                       // cap and avoidance starts applying to all of them with no further
                       // change here. That is the intended end state."* This is that change.
                       //
                       // ⚠️ THE AREA COMES BACK AS THE FUNNEL. `docs/VISION.md` § 2 rule 3: a
                       // smaller flat plane is still a puddle. The void reads vertically now,
                       // through a deeper core and pulled debris, rather than by being wide.
                       telegraphRadius: 4.0f, telegraphRange: 3.5f,
                       castAction: "hero-nemu-seance",
                       viewmodelAction: "seance-channel",
                       castCue: "sfx_cast_nemu_seance")
            {
                TelegraphStyle = Visual.GroundReticle.Style.Maw;
                Windup = UltimateWindup;
            }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx) || IsActive) return false;
                var pet=ctx.Motor.GetComponent<CharacterVisual>()?.Companion;
                return pet==null || !pet.IsDevouring;
            }

            public void RestoreSeance(AbilityContext ctx,Vector3 position,float remaining)
            {
                remaining=Mathf.Clamp(remaining,0,Duration);
                var pet=ctx.Motor.GetComponent<CharacterVisual>()?.Companion;
                if(pet==null)return;
                _castAnchor=position;_approaching=false;
                Vector3 facing=ctx.Position-position;facing.y=0;
                if(facing.sqrMagnitude<.01f)facing=-ctx.Forward;
                _castFacing=Quaternion.LookRotation(facing.normalized,Vector3.up);
                // The predicted root has its own scheduled destruction. Recreate
                // that short-lived field on confirmation so its lifetime agrees
                // with the authoritative ghost/ability clock, even at high latency.
                if(_field!=null){_field.SetActive(false);UnityEngine.Object.Destroy(_field);}
                _field=HeroHazards.SpawnKuroUnbound(position,4,remaining,ctx.Motor.PlayerSlot,true,false);
                _familiar=pet;pet.transform.rotation=_castFacing;pet.RestoreDevour(position,Duration,remaining,true);
                ctx.Motor.AbilitySystem.Kit.Skill2.EndEarly(ctx);
                RestoreLiveClock(remaining);
            }

            public override Vector3 TelegraphCentre(AbilityContext ctx)
            {
                if(IsWindingUp || IsActive)return _castAnchor;
                return ResolveAnchor(ctx);
            }

            private static Vector3 ResolveAnchor(AbilityContext ctx)
            {
                var companion = ctx.Motor.GetComponent<Visual.CharacterVisual>()?.Companion;
                if(companion!=null && companion.IsPossessed)return VfxShapes.GroundPoint(companion.transform.position);
                return VfxShapes.GroundPoint(GhostPetMotion.Move(ctx.Motor,ctx.Position,ctx.Forward*FallbackRange));
            }

            public override void Activate(AbilityContext ctx)
            {
                _castAnchor=ResolveAnchor(ctx);
                _familiar=ctx.Motor.GetComponent<CharacterVisual>()?.Companion;
                _familiar?.PrepareForInvocation();
                _approaching=_familiar!=null && !_familiar.IsPossessed;
                _approachStart=_familiar!=null?_familiar.transform.position:_castAnchor;
                Vector3 towardsCaster=ctx.Position-_castAnchor;towardsCaster.y=0;
                if(towardsCaster.sqrMagnitude<.01f)towardsCaster=-ctx.Forward;
                _castFacing=Quaternion.LookRotation(towardsCaster.normalized,Vector3.up);
                base.Activate(ctx);
                if(HadSharedIntroduction && _familiar!=null)
                {
                    _approaching=false;
                    _familiar.PreviewRevealedInvocation(_castAnchor,_castFacing);
                }
            }

            public override void Tick(AbilityContext ctx,float dt)
            {
                if(IsWindingUp && _approaching && _familiar!=null)
                {
                    float p=Mathf.SmoothStep(0,1,1-Mathf.Max(0,WindupRemaining-dt)/Windup);
                    _familiar.ApplyCastAnchor(Vector3.Lerp(_approachStart,_castAnchor+Vector3.up*.9f,p));
                    _familiar.transform.rotation=Quaternion.Slerp(_familiar.transform.rotation,_castFacing,p);
                }
                base.Tick(ctx,dt);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_nemu_ult", ctx.Position);

                // ⚠️ ON KURO IF KURO IS OUT. `CharacterVisual.Companion` is the pet, and it is
                // present whenever she has one whether or not it is currently possessed: an
                // ultimate cast from inside a possession opens under the body the player is
                // driving, which is the strongest version of this and needs no special case.
                var companion = ctx.Motor.GetComponent<Visual.CharacterVisual>()?.Companion;

                bool onPet = companion != null;
                Vector3 at = _castAnchor;

                // ⚠️⚠️ THE PET IS CONSUMED BY IT AND THAT IS THE ANIMATION. `Devour` swells Kuro
                // into the maw over the wind-up and hides the pet inside it, so what the other
                // three players see is the small thing that has been following her around all
                // round becoming the thing that is eating them. A vortex spawned beside an
                // unchanged pet would have been the old effect with a new name.
                // ⚠️⚠️ 2.8 m / 5.0 s BECAME 4.0 m / 7.0 s. 🧑 2026-08-27: *"make kuro's pull
                // stronger and longer ... make it pull everyone and everything"*. The strength is
                // in `SpawnKuroUnbound` (`PullStrength` 4.0 to 14.0); these two are the reach and
                // the life. At 2.8 m the maw covered 4 per cent of the court, so *"everyone"* was
                // usually nobody: an ultimate that pulls hard but cannot reach anybody is the
                // same complaint one step further in.
                //
                // ⚠️ 4.0 m IS 5.1 PER CENT OF THE 196 m² BOX AND IT IS STILL UNDER PHAISTER'S
                // ECLIPSE AT 5.0 m. `docs/VISION.md` § 2 rule 2 allows an ultimate to be big and
                // rule 4 caps what may OVERLAP; this is one zone, it paints no bright floor (the
                // bite is near-black by construction), and it is the only thing on the court while
                // it runs.
                _familiar=companion;
                if (onPet)
                {
                    // The following familiar crosses the existing3.5m cast reach
                    // during invocation. The controlled familiar keeps its anchor.
                    // Both then grow on that exact field, facing the caster so the
                    // first-person view sees the maw instead of a giant's back.
                    companion.ApplyCastAnchor(at+Vector3.up*.9f);
                    companion.transform.rotation=_castFacing;
                    companion.Devour(Duration,HadSharedIntroduction);
                    // Devour ends the ride without teleporting Nemu. Close its
                    // ability timer too, so a stale E recast cannot act as a ride.
                    ctx.Motor.AbilitySystem?.Kit?.Skill2?.EndEarly(ctx);
                }

                _field=HeroHazards.SpawnKuroUnbound(at, 4.0f, Duration, ctx.Motor.PlayerSlot, onPet);
                _approaching=false;
                Net.MatchRpc.Instance?.BroadcastFamiliarEffect(ctx.Motor.PlayerSlot);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                // Their matching timers own the normal close cue and return.
                _field=null;_familiar=null;_approaching=false;
            }
            protected override void OnCancelled(AbilityContext ctx)
            {
                // A denied or reset cast must not leave seven seconds of pull in
                // the next state. Disable immediately before deferred destruction.
                if (_field!=null)
                {
                    _field.SetActive(false);
                    if(Application.isPlaying)UnityEngine.Object.Destroy(_field);
                    else UnityEngine.Object.DestroyImmediate(_field);
                }
                if(_familiar!=null)_familiar.StopDevouring();
                _field=null;_familiar=null;
            }
        }
    }
}
