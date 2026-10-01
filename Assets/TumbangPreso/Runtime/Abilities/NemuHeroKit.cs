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
    /// cooldown and recall anchor. Catch protects the can through its owned clock.
    /// Haunt chases visible players sequentially using the existing companion.
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

        public bool ReceiveHaunt(CharacterMotor motor, long phase, float clock, Vector3 position, float remaining, float yaw)
            => ((NightmareSeanceVoidAbility)Ultimate).ReceiveHaunt(
                new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>()),
                phase, clock, position, remaining, yaw);

        public NemuHeroKit() : base("nemu", "NEMU")
        {
            Skill1 = new KuroSit(this);
            AttackingSkill = new KuroFetch(this);
            DefendingSkill = new KuroGuard(this);
            Ultimate = new NightmareSeanceVoidAbility();
        }

        public override float UltimateCost => 15.0f;

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

        // ================================================================== KURO: CATCH! (defending)

        private sealed class KuroGuard : HeroAbility, IPreparedWorldReplication
        {
            private readonly NemuHeroKit _kit;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            private GhostPetCompanion _kuro;
            private Lata _can;
            private bool _approvedReplica;

            public KuroGuard(NemuHeroKit kit)
                : base("nemu_skill2d", "KURO: CATCH!",
                       "Defending. Command Kuro to protect the upright can from knockdown for 5 s.",
                       NecroRules.GuardCooldown, NecroRules.GuardSeconds, AbilityGlyph.NemuKuroGuard,
                       summary: "Protect the upright can for 5 s.",
                       castAction: "hero-nemu-seance", viewmodelAction: "seance-channel",
                       castCue: "sfx_cast_nemu_guard") { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx.Motor.IsDefender && ctx.Round?.Lata?.IsUpright == true;

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.ShareBasicCooldown(CooldownRemaining, mayLower: false);
                _approvedReplica = ctx.IsApprovedReplay;
                BindCan(ctx);
            }
            private void BindCan(AbilityContext ctx)
            {
                _can = ctx?.Round?.Lata;
                _can?.AddAbilityProtection(this, _approvedReplica);
                BindCompanion(ctx);
            }
            private void BindCompanion(AbilityContext ctx)
            {
                if (_can == null) return;
                _kuro = Kuro(ctx);
                _kuro?.BeginErrand(() => _can != null
                    ? _can.transform.position + Vector3.forward * 1.2f : ctx.Position,
                    NecroRules.GuardMoveSpeed, NecroRules.GuardScale);
            }
            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_can == null) { BindCan(ctx); return; }
                if (_can != ctx?.Round?.Lata || (NetAuthority.ShouldResolve() && !_can.IsUpright))
                { DurationRemaining = 0; return; }
                _can.AddAbilityProtection(this, _approvedReplica);
                if (_kuro == null) BindCompanion(ctx);
            }
            public bool CapturePreparedWorld(out Vector3 centre, out float preparation, out float remaining)
            {
                bool active = IsActive && _can != null;
                centre = active ? _can.transform.position : Vector3.zero;
                preparation = 0; remaining = active ? DurationRemaining : 0;
                return active;
            }
            public bool RestorePreparedWorld(AbilityContext ctx, Vector3 centre, float preparation, float remaining)
            {
                if (ctx?.Motor == null || !float.IsFinite(centre.x) || !float.IsFinite(centre.y)
                    || !float.IsFinite(centre.z) || !float.IsFinite(preparation) || preparation != 0
                    || !float.IsFinite(remaining) || remaining < 0 || remaining > Duration) return false;
                if (remaining == 0)
                { EndEarly(ctx); OnEnd(ctx); return false; }
                if (ctx.Round?.Lata == null || (NetAuthority.ShouldResolve() && !ctx.Round.Lata.IsUpright)) return false;
                RestoreLiveClock(remaining);
                // This callback is reached through the validated shared recovery
                // route, whose context is not an ordinary cast-playback context.
                _approvedReplica = true;
                BindCan(ctx);
                return false;
            }
            protected override void OnEnd(AbilityContext ctx)
            {
                _can?.RemoveAbilityProtection(this); _can = null;
                _approvedReplica = false;
                _kuro?.EndErrand(); _kuro = null;
            }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            private readonly RaycastHit[] _sightHits = new RaycastHit[32];
            private readonly RaycastHit[] _moveHits = new RaycastHit[32];
            private GhostPetCompanion _familiar;
            private CharacterMotor _castMotor, _target;
            private Vector3 _castAnchor, _approachStart;
            private Quaternion _castFacing;
            private bool _approaching;
            private int _caughtMask;
            private float _syncLeft, _receivedClock;
            private long _receivedPhase;

            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "KURO: HAUNT!",
                       "Kuro becomes a monster and chases every player he sees, one at a time. Contact inflicts Haunted for 7.5 s. He returns after chasing everyone or when the round ends.",
                       0, CustomGameRules.MaxRoundSeconds, AbilityGlyph.NemuSeanceVoid,
                       summary: "Kuro chases seen players and inflicts Haunted on contact.",
                       telegraphRadius: .4f, telegraphRange: 3.5f,
                       castAction: "hero-nemu-seance", viewmodelAction: "seance-channel",
                       castCue: "sfx_cast_nemu_seance")
            { TelegraphStyle = GroundReticle.Style.Maw; Windup = UltimateWindup; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !IsActive && Kuro(ctx) != null && !Kuro(ctx).IsDevouring;

            protected override void OnAcceptedUltimatePhase(long phase)
            {
                if (IsActive && _castMotor != null)
                    Net.MatchRpc.Instance?.BroadcastFamiliarEffect(_castMotor.PlayerSlot);
            }

            public void RestoreSeance(AbilityContext ctx, Vector3 position, float remaining)
            {
                var pet = Kuro(ctx); if (pet == null) return;
                _castAnchor = position; _approaching = false; _familiar = pet;
                // Recovery moves the existing monster and adopts its clock. It does
                // not recreate the retired pull field or replay a hit/resource spend.
                bool starting = !pet.IsDevouring;
                pet.RestoreDevour(position, Duration, remaining, true);
                if (starting)
                {
                    ctx.Motor.AbilitySystem.Kit.Skill1.EndEarly(ctx);
                    ctx.Motor.AbilitySystem.Kit.Skill2.EndEarly(ctx);
                }
                RestoreLiveClock(remaining);
            }

            public override Vector3 TelegraphCentre(AbilityContext ctx)
                => IsWindingUp || IsActive ? _castAnchor : ResolveAnchor(ctx);

            private static Vector3 ResolveAnchor(AbilityContext ctx)
            {
                var pet = Kuro(ctx);
                if (pet != null && pet.IsPossessed) return VfxShapes.GroundPoint(pet.transform.position);
                return VfxShapes.GroundPoint(GhostPetMotion.Move(ctx.Motor, ctx.Position, ctx.Forward * 3.5f));
            }

            public override void Activate(AbilityContext ctx)
            {
                _castMotor = ctx.Motor; _castAnchor = ResolveAnchor(ctx); _familiar = Kuro(ctx);
                _familiar?.PrepareForInvocation();
                _approaching = _familiar != null && !_familiar.IsPossessed;
                _approachStart = _familiar != null ? _familiar.transform.position : _castAnchor;
                Vector3 facing = ctx.Position - _castAnchor; facing.y = 0;
                if (facing.sqrMagnitude < .01f) facing = -ctx.Forward;
                _castFacing = Quaternion.LookRotation(facing.normalized, Vector3.up);
                base.Activate(ctx);
                if (HadSharedIntroduction && _familiar != null)
                { _approaching = false; _familiar.PreviewRevealedInvocation(_castAnchor, _castFacing); }
            }

            public override void Tick(AbilityContext ctx, float dt)
            {
                if (IsWindingUp && _approaching && _familiar != null)
                {
                    float p = Mathf.SmoothStep(0, 1, 1 - Mathf.Max(0, WindupRemaining - dt) / Windup);
                    _familiar.ApplyCastAnchor(Vector3.Lerp(_approachStart, _castAnchor + Vector3.up * .9f, p));
                    _familiar.transform.rotation = Quaternion.Slerp(_familiar.transform.rotation, _castFacing, p);
                }
                base.Tick(ctx, dt);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _caughtMask = 0; _target = null; _syncLeft = 0;
                DurationRemaining = Mathf.Min(Duration, ctx.Round?.TimeLeft ?? Duration);
                _familiar = Kuro(ctx); _castMotor = ctx.Motor; _approaching = false;
                NetCue.Play("hero_nemu_ult", ctx.Position);
                ctx.Motor.AbilitySystem?.Kit?.Skill1?.EndEarly(ctx);
                ctx.Motor.AbilitySystem?.Kit?.Skill2?.EndEarly(ctx);
                if (_familiar != null)
                {
                    _familiar.ApplyCastAnchor(_castAnchor + Vector3.up * .9f);
                    _familiar.transform.rotation = _castFacing;
                    _familiar.Devour(DurationRemaining, HadSharedIntroduction);
                }
                Net.MatchRpc.Instance?.BroadcastFamiliarEffect(ctx.Motor.PlayerSlot);
            }

            private static bool Eligible(CharacterMotor motor) => motor != null &&
                motor.gameObject.activeInHierarchy && motor.PlayerSlot >= 0 && motor.PlayerSlot < Balance.PlayerCount;

            private bool Visible(CharacterMotor motor, Vector3 ground)
            {
                Vector3 origin = ground + Vector3.up * .8f;
                Vector3 delta = motor.transform.position + Vector3.up * .8f - origin;
                if (delta.sqrMagnitude < .0001f) return true;
                int count = Physics.RaycastNonAlloc(origin, delta.normalized, _sightHits, delta.magnitude,
                    ~0, QueryTriggerInteraction.Ignore);
                if (count == _sightHits.Length) return false;
                for (int i = 0; i < count; i++)
                {
                    var collider = _sightHits[i].collider;
                    if (collider.GetComponentInParent<CharacterMotor>() != null ||
                        collider.GetComponentInParent<Slipper>() != null) continue;
                    return false;
                }
                return true;
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!NetAuthority.ShouldResolve()) return;
                if (ctx.Round?.RoundActive != true || _familiar == null || !Eligible(ctx.Motor))
                { DurationRemaining = 0; return; }
                var players = ctx.Round.Players;
                int pending = 0; float nearest = float.PositiveInfinity;
                Vector3 ground = _familiar.DevourGround;
                if (!Eligible(_target) || (_caughtMask & (1 << _target.PlayerSlot)) != 0 ||
                    !Visible(_target, ground)) _target = null;
                CharacterMotor choice = null;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    if (!Eligible(player) || (_caughtMask & (1 << player.PlayerSlot)) != 0) continue;
                    pending++;
                    if (_target != null || !Visible(player, ground)) continue;
                    float distance = (player.transform.position - ground).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; choice = player; }
                }
                if (pending == 0) { DurationRemaining = 0; return; }
                if (_target == null) _target = choice;
                if (_target != null)
                {
                    Vector3 delta = _target.transform.position - ground; delta.y = 0;
                    // The familiar uses the arena even when Nemu is the confined defender.
                    Vector3 next = GhostPetMotion.Move(null, ground,
                        Vector3.ClampMagnitude(delta, Mathf.Max(0, dt) * 10f), _moveHits);
                    _familiar.MoveDevour(next);
                    if (delta.sqrMagnitude > .0001f)
                        _familiar.transform.rotation = Quaternion.LookRotation(delta, Vector3.up);
                    Vector3 contact = _target.transform.position - next;
                    if (new Vector2(contact.x, contact.z).sqrMagnitude <= 1.1f * 1.1f &&
                        Mathf.Abs(contact.y) <= 2f && Visible(_target, next))
                    {
                        _target.ApplyHaunted(); _caughtMask |= 1 << _target.PlayerSlot; _target = null;
                    }
                }
                _syncLeft -= dt;
                if (_syncLeft <= 0)
                { _syncLeft = .1f; Net.MatchRpc.Instance?.BroadcastFamiliarEffect(ctx.Motor.PlayerSlot); }
            }

            public bool ReceiveHaunt(AbilityContext ctx, long phase, float clock, Vector3 position, float remaining, float yaw)
            {
                if (phase == AcceptedUltimatePhase && !IsActive && !IsWindingUp) return false;
                if (phase == _receivedPhase && (clock > _receivedClock ||
                    (clock == _receivedClock && remaining > 0))) return false;
                _receivedPhase = phase; _receivedClock = clock;
                if (remaining <= 0)
                { RestoreLiveClock(0); OnEnd(ctx); }
                else RestoreSeance(ctx, position, remaining);
                var pet = Kuro(ctx);
                if (pet != null) pet.transform.rotation = Quaternion.Euler(0, yaw, 0);
                AdoptUltimatePhase(phase);
                return true;
            }

            public override void Reset()
            {
                base.Reset();
                _receivedPhase = 0; _receivedClock = 0; _caughtMask = 0; _target = null;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                // Publish the terminal clock before releasing the companion.
                if (_castMotor != null) Net.MatchRpc.Instance?.BroadcastFamiliarEffect(_castMotor.PlayerSlot);
                _familiar?.StopDevouring();
                _familiar = null; _castMotor = null; _target = null; _approaching = false;
            }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }
    }
}
