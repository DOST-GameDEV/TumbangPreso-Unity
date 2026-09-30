using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Nemu's current Wiki passive and Kuro: Sit signature use the shared basic
    /// cooldown and recall anchor. Fetch, Guard and the legacy seance below still
    /// await their remaining Wiki behavior changes; this is not a full-kit claim.
    /// </summary>
    public sealed class NemuHeroKit : HeroKit
    {
        /// <summary>Kept for the wire and the tag rule: Phantom Veil is gone, so it is never active.</summary>
        public bool IsPhantomPhaseActive => false;

        /// <summary>Phantom Veil is gone; a rejoiner has no veil to restore.</summary>
        public bool RestoreJoiningVeil(CharacterMotor motor, float remaining) => false;

        // Kuro's current Wiki passive gives one 10 percent bonus while a basic
        // ability is cooling down. Read the existing clocks so expiry, round
        // resets and authoritative corrections cannot leave a stale bonus.
        public override float MovementSpeedScale =>
            Skill1?.CooldownRemaining > 0f || AttackingSkill?.CooldownRemaining > 0f ||
            DefendingSkill?.CooldownRemaining > 0f ? 1.1f : 1f;

        private void ShareBasicCooldown(float seconds, bool mayLower)
        {
            Skill1?.ApplyNetworkSnapshot(seconds, Skill1.ChargesRemaining, mayLower);
            AttackingSkill?.ApplyNetworkSnapshot(seconds, AttackingSkill.ChargesRemaining, mayLower);
            DefendingSkill?.ApplyNetworkSnapshot(seconds, DefendingSkill.ChargesRemaining, mayLower);
        }

        internal override void ApplySkillReceiptResources(HeroAbility ability, float cooldown,
            int charges, bool newerSkillRequestExists)
        {
            // A reply for the other slot must not refund or shorten a later cast,
            // even when that later request has already been acknowledged.
            if (newerSkillRequestExists) return;
            base.ApplySkillReceiptResources(ability, cooldown, charges, false);
            ShareBasicCooldown(cooldown, mayLower: true);
        }

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
            Skill1 = new KuroSit(this);
            AttackingSkill = new KuroFetch(this);
            DefendingSkill = new KuroGuard(this);
            Ultimate = new NightmareSeanceVoidAbility();
        }

        /// <summary>The seance's price (unchanged until KURO PLAYS replaces it).</summary>
        public override float UltimateCost => 10.0f;

        private static GhostPetCompanion Kuro(AbilityContext ctx) => ctx?.Motor?.GetComponent<CharacterVisual>()?.Companion;

        // ================================================================== KURO: SIT!

        private sealed class KuroSit : HeroAbility, IPreparedWorldReplication
        {
            private readonly NemuHeroKit _kit;
            private Vector3 _spot;
            private bool _hasAnchor;
            private CharacterMotor _owner;
            private GhostPetCompanion _kuro;

            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public override bool CanReactivate => true;
            public override bool ReactivateReady => IsActive && _hasAnchor && _owner != null && _owner.CanAct();

            public KuroSit(NemuHeroKit kit)
                : base("nemu_skill1", "KURO: SIT!",
                       "Hold to aim, release to leave Kuro at a location for 10 seconds. Reactivate to teleport back to him.",
                       NecroRules.BasicCooldown, NecroRules.SitSeconds, AbilityGlyph.NemuPhase,
                       summary: "Leave Kuro at a spot. Reactivate to return to him.",
                       telegraphRadius: 0.4f, telegraphRange: NecroRules.SitMaxRange,
                       castAction: "hero-nemu-project", viewmodelAction: "project-spirit",
                       castCue: "sfx_cast_nemu_terrify")
            {
                _kit = kit;
                AimByHolding(2.0f, NecroRules.SitMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Maw;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.ShareBasicCooldown(CooldownRemaining, mayLower: false);
                _spot = AimedDestination(ctx);
                _owner = ctx?.Motor;
                _hasAnchor = true;
                BindCompanion(ctx);
                NetCue.Play("sfx_ghost_teleport", _spot);
            }

            private void BindCompanion(AbilityContext ctx)
            {
                _kuro = Kuro(ctx);
                if (_kuro == null) return;
                _kuro.ApplyCastAnchor(_spot + Vector3.up * 0.9f);
                _kuro.BeginErrand(() => _spot, NecroRules.FetchSpeed);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // A delayed model load must adopt the existing anchor, not cast
                // again or restart the lifetime and shared cooldown.
                if (_hasAnchor && _kuro == null) BindCompanion(ctx);
            }

            public override Vector3 TelegraphCentre(AbilityContext ctx)
                => _hasAnchor && IsActive ? _spot : AimedDestination(ctx);

            public override void Reactivate(AbilityContext ctx)
            {
                if (!IsActive || !_hasAnchor || ctx?.Motor == null) return;
                if (!ctx.IsApprovedReplay && !ctx.Motor.CanAct()) return;
                // Teleport owns confinement, prediction and host transform delivery.
                // An observing replica cannot mutate another player's motor.
                ctx.Motor.Teleport(_spot);
                EndEarly(ctx);
            }

            public bool CapturePreparedWorld(out Vector3 centre, out float preparation, out float remaining)
            {
                bool active = _hasAnchor && IsActive;
                centre = active ? _spot : Vector3.zero;
                preparation = 0;
                remaining = active ? DurationRemaining : 0;
                return active;
            }

            public bool RestorePreparedWorld(AbilityContext ctx, Vector3 centre, float preparation, float remaining)
            {
                if (ctx?.Motor == null || !float.IsFinite(centre.x) || !float.IsFinite(centre.y)
                    || !float.IsFinite(centre.z) || !float.IsFinite(preparation) || preparation != 0
                    || !float.IsFinite(remaining) || remaining < 0 || remaining > Duration) return false;
                if (remaining == 0)
                {
                    EndEarly(ctx);
                    OnEnd(ctx);
                    return false;
                }
                _spot = centre;
                _owner = ctx.Motor;
                _hasAnchor = true;
                RestoreLiveClock(remaining);
                BindCompanion(ctx);
                // The interface returns true only for a restored preparation pose.
                // Sit has no windup; its live anchor was restored without a cast.
                return false;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kuro?.EndErrand();
                _kuro = null;
                _owner = null;
                _hasAnchor = false;
            }

            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== KURO FETCH (attacking)

        private sealed class KuroFetch : HeroAbility
        {
            private readonly NemuHeroKit _kit;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private Slipper _shoe;
            private GhostPetCompanion _kuro;
            private bool _carrying;
            private float _nextBroadcast;

            public KuroFetch(NemuHeroKit kit)
                : base("nemu_skill2", "KURO: FETCH!",
                       "Attacking. Kuro brings your loose slipper beside you for pickup. If the taya tags him on the way, he drops it.",
                       NecroRules.FetchCooldown, 8.0f, AbilityGlyph.NemuAstralPet,
                       summary: "Kuro fetches your slipper. The taya can make him drop it.",
                       castAction: "hero-nemu-project", viewmodelAction: "project-spirit",
                       castCue: "sfx_cast_nemu_fetch") { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx) || ctx.Motor.IsDefender || ctx.Motor.HoldingSlipper) return false;
                return OwnLooseSlipper(ctx.Motor) != null;
            }

            private static Slipper OwnLooseSlipper(CharacterMotor who)
            {
                foreach (var s in BotSlipperInventory.All)
                    if (s.OwnerSlot == who.PlayerSlot && s.State == SlipperState.Loose) return s;
                return null;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.ShareBasicCooldown(CooldownRemaining, mayLower: false);
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
                if (_shoe == null || !_shoe.gameObject.activeInHierarchy ||
                    _shoe.OwnerSlot != ctx.Motor.PlayerSlot || _shoe.State != SlipperState.Loose)
                {
                    // A normal pickup or round ownership change wins over an old
                    // fetch. Never reposition equipment that is now held or thrown.
                    _carrying = false;
                    DurationRemaining = 0.0f;
                    return;
                }
                if (_kuro == null) { DurationRemaining = 0.0f; return; }
                if (!_carrying)
                {
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
                            GroundCarriedSlipper();
                            MatchFlair.Announce(MatchFlair.Kind.Block, ctx.Motor.PlayerSlot, p.PlayerSlot, at, 4f);
                            NetCue.Play("sfx_nemu_fetch_drop", at);
                            DurationRemaining = 0.0f;
                            return;
                        }
                if ((ctx.Motor.transform.position - at).sqrMagnitude < 1.2f * 1.2f)
                {
                    _shoe.transform.position = ctx.Motor.transform.position + ctx.Motor.transform.right * 0.8f;
                    GroundCarriedSlipper();
                    DurationRemaining = 0.0f;
                }
            }

            private void GroundCarriedSlipper()
            {
                if (_carrying && _shoe != null && _shoe.gameObject.activeInHierarchy &&
                    _shoe.State == SlipperState.Loose && NetAuthority.ShouldResolve())
                {
                    // Reuse normal landing: terrain height, authored shoe rest pose,
                    // playable bounds and pickup highlights stay in one place.
                    _shoe.HostScatter(Vector3.zero);
                    Net.MatchRpc.Instance?.BroadcastSlipperState(_shoe);
                }
                _carrying = false;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                GroundCarriedSlipper();
                _kuro?.EndErrand();
                _kuro = null;
                _shoe = null;
            }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== KURO GUARD (defending)

        private sealed class KuroGuard : HeroAbility
        {
            private readonly NemuHeroKit _kit;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private GhostPetCompanion _kuro;
            private Vector3 _spot, _pending;
            private float _think, _react;
            private readonly HashSet<Slipper> _blocked = new HashSet<Slipper>();

            public KuroGuard(NemuHeroKit kit)
                : base("nemu_skill2d", "KURO GUARD",
                       "Defending. Kuro grows and guards the can for 6 s, moving to block the throws he sees coming. He is quick, not perfect.",
                       NecroRules.GuardCooldown, NecroRules.GuardSeconds, AbilityGlyph.NemuKuroGuard,
                       summary: "Kuro grows and blocks throws at the can.",
                       castAction: "hero-nemu-seance", viewmodelAction: "seance-channel",
                       castCue: "sfx_cast_nemu_guard") { _kit = kit; }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.ShareBasicCooldown(CooldownRemaining, mayLower: false);
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
                    NetCue.Play("sfx_nemu_guard_block", s.transform.position);
                }
            }

            protected override void OnEnd(AbilityContext ctx) { _kuro?.EndErrand(); _kuro = null; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            /// <summary>Where it opens when Kuro is not out. Her own reach, as before.</summary>
            private GameObject _field;
            private GhostPetCompanion _familiar;
            private CharacterMotor _castMotor;
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

            protected override void OnAcceptedUltimatePhase(long phaseId)
            {
                // Immediate activation binds its accepted lifetime after OnActivate.
                if (IsActive && _castMotor != null)
                    Net.MatchRpc.Instance?.BroadcastFamiliarEffect(_castMotor.PlayerSlot);
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
                _castMotor = ctx.Motor;
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
                _field=null;_familiar=null;_castMotor=null;_approaching=false;
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
                _field=null;_familiar=null;_castMotor=null;
            }
        }
    }
}
