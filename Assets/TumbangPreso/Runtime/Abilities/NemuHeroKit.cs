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
    /// PRUSISYON possesses every other player for 15 s (`CharacterMotor.Possession.cs`).
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
            // Both retired familiar modes (Astral Hijack's projection, then the Haunt) have nothing to restore: PRUSISYON's
            // state is the POSSESSED clock on each victim, which `SyncUnit` already carries.
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
                // Kuro carries the slipper: with pets off (`Visual.Pets`) there is nobody to fetch it, so the cast is refused
                // rather than spending the shared cooldown on nothing.
                if (Kuro(ctx) == null) return false;
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
                       castAction: "hero-nemu-guard", viewmodelAction: "kuro-guard",
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

        // ================================================================== PRUSISYON (ultimate)

        /// <summary>
        /// PRUSISYON (owner brief 2026-10-11, `docs/reports/nemu-ultimate-2026-10-11/direction.md`): she hushes the world grey
        /// and the dead she is always half-listening to file past in procession and step into every other player. Each is
        /// POSSESSED for 15 s (`CharacterMotor.Possession.cs`): a ghost rides them and contests their steering toward the
        /// worst place for them. The cutscene shows the entering, so play starts with the riders seated (never shown twice).
        /// ⚠️ The ID, glyph and cost are the retired KURO: HAUNT!'s, kept for saves, mastery and the wire. Kuro no longer
        /// chases anyone; `RestoreSeance`/`ReceiveHaunt` stay as inert endpoints for the unchanged familiar message.
        /// </summary>
        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;

            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "PRUSISYON",
                       "Nemu hushes the world and the dead walk in procession into every other player. They are Possessed for 15 s: a ghost drags their steering toward danger. Push against it to hold your ground.",
                       0, NemuRules.PossessedSeconds, AbilityGlyph.NemuSeanceVoid,
                       summary: "Every other player is Possessed for 15 s.",
                       castAction: "hero-nemu-seance", viewmodelAction: "seance-channel",
                       castCue: "sfx_cast_nemu_seance")
            { Windup = UltimateWindup; }

            public override bool CanActivate(AbilityContext ctx) => base.CanActivate(ctx) && !IsActive;

            /// <summary>Retired Haunt recovery: there is no familiar effect to restore.</summary>
            public void RestoreSeance(AbilityContext ctx, Vector3 position, float remaining, bool smoothPose = false) { }

            /// <summary>Retired Haunt movement: a stray familiar message changes nothing.</summary>
            public bool ReceiveHaunt(AbilityContext ctx, long phase, float clock, Vector3 position, float remaining, float yaw) => false;

            private static bool Eligible(CharacterMotor motor) => motor != null &&
                motor.gameObject.activeInHierarchy && motor.PlayerSlot >= 0 && motor.PlayerSlot < Balance.PlayerCount;

            protected override void OnActivate(AbilityContext ctx)
            {
                DurationRemaining = Mathf.Min(Duration, ctx.Round?.TimeLeft ?? Duration);
                NetCue.Play("hero_nemu_ult", ctx.Position);
                // Only the host writes the status (`ApplyPossessed` refuses elsewhere); `SyncUnit` carries it to every peer.
                var round = ctx.Round;
                if (round == null || !NetAuthority.ShouldResolve()) return;
                var players = round.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!Eligible(p) || p == ctx.Motor || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;
                    p.ApplyPossessed();
                }
            }
        }
    }
}
