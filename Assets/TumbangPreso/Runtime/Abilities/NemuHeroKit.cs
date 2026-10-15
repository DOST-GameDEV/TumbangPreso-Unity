using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Nemu's current Wiki passive and DON'T LOOK signature (Blindfold, a hidden ghost, `NemuBlindfold`) use the shared basic
    /// cooldown. Catch protects the can through its owned clock.
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
            Skill1 = new DontLook(this);
            AttackingSkill = new Skitter(this);
            DefendingSkill = new Lookout(this);
            Ultimate = new NightmareSeanceVoidAbility();
        }

        public override float UltimateCost => 15.0f;

        // ================================================================== DON'T LOOK (signature)

        /// <summary>
        /// DON'T LOOK (owner 2026-10-15, the overhaul of KURO: SIT!: *"lets make her like gekko ... have diff ghosts and summon
        /// diff ghosts at the same time"*, and the signature *"a tripwire blind"*). Hold to aim, release: Blindfold, one of her
        /// ghosts, waits at the spot and fades from sight after 3 s; whoever comes near first has it lunge at their eyes
        /// (BLINDED: covered 2 s, a 50 % slow wearing off over 5 s). The ghost is `NemuBlindfold`; Kuro is not sent.
        /// ⚠️ The ID is Sit's, kept for saves, mastery and the wire. The cast is host-confirmed, so every peer places the
        /// same ghost from the accepted cast (Cold Feet's pattern) and only the host springs it.
        /// </summary>
        private sealed class DontLook : HeroAbility
        {
            private readonly NemuHeroKit _kit;

            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;

            public DontLook(NemuHeroKit kit)
                : base("nemu_skill1", "DON'T LOOK",
                       "Hide a ghost at a spot. The first player to come near is blinded for 2 s, then slowed (wears off over 5 s). Lasts 30 s.",
                       NecroRules.DontLookCooldown, 0.0f, AbilityGlyph.NemuDontLook,
                       summary: "Hide a ghost. The first to come near is blinded, then slowed.",
                       telegraphRadius: NecroRules.DontLookTriggerRadius, telegraphRange: NecroRules.DontLookMaxRange,
                       castAction: "hero-nemu-dontlook", viewmodelAction: "dontlook-hush",
                       castCue: "sfx_cast_nemu_dontlook")
            {
                _kit = kit;
                AimByHolding(NecroRules.DontLookMinRange, NecroRules.DontLookMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Maw;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.ShareBasicCooldown(CooldownRemaining, mayLower: false);
                if (ctx?.Motor == null) return;
                NemuBlindfold.Place(AimedDestination(ctx), ctx.Forward, ctx.Motor.PlayerSlot);
            }
        }

        // ================================================================== SKITTER (attacking)

        /// <summary>
        /// SKITTER (owner 2026-10-15, the overhaul of KURO: FETCH!: *"Spawn siya ghost na kukunin slipper for her"*). A greedy
        /// little green ghost of hers (`NemuSkitter`) pops out beside her, scrambles to her loose slipper, grabs it and hurries
        /// it back, leaving it on the ground beside her to pick up. A taya who gets near it on the way back makes it drop it.
        /// Kuro is not sent. ⚠️ The ID is Fetch's, kept for saves, mastery and the wire. HOST-CONFIRMED (Fetch was predicted)
        /// so every peer runs the same ghost from the accepted cast; only the host moves or lands the slipper (protocol95's
        /// delivery rules unchanged), and a peer's ghost lets go when the host's slipper stops following it.
        /// </summary>
        private sealed class Skitter : HeroAbility
        {
            private readonly NemuHeroKit _kit;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            private Slipper _shoe;
            private NemuSkitter _ghost;
            private bool _carrying;
            private float _nextBroadcast, _apart;

            public Skitter(NemuHeroKit kit)
                : base("nemu_skill2", "SKITTER",
                       "Attacking. A ghost fetches your loose slipper and waits by you until you take it. If the taya gets close, it drops it.",
                       NecroRules.FetchCooldown, 8.0f, AbilityGlyph.NemuSkitter,
                       summary: "A ghost brings your slipper back and waits for you. The taya can make it drop.",
                       castAction: "hero-nemu-skitter", viewmodelAction: "skitter-flick",
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
                _carrying = false; _apart = 0f;
                var owner = ctx.Motor.transform;
                _ghost = NemuSkitter.Summon(owner.position + owner.right * .5f + Vector3.up * .3f, ctx.Forward, ctx.Motor.PlayerSlot);
                // Every peer runs this from the accepted cast, so its sounds are local, not relayed (`audit_cue_relay.py`).
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
            }

            /// <summary>Where its feet go so its hands are on <paramref name="hold"/>.</summary>
            private Vector3 FeetFor(Vector3 hold) => hold - (_ghost.HoldPoint - _ghost.Position);

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_shoe == null || !_shoe.gameObject.activeInHierarchy ||
                    _shoe.OwnerSlot != ctx.Motor.PlayerSlot || _shoe.State != SlipperState.Loose)
                {
                    // A normal pickup or round ownership change wins over an old
                    // fetch. Never reposition equipment that is now held or thrown.
                    // She took it out of its claws (her interact): the hand-over, its happy spin on every peer.
                    if (_carrying && _shoe != null && _shoe.State == SlipperState.Held && _shoe.Holder == ctx.Motor)
                        _ghost?.Leave(delivered: true);
                    _carrying = false;
                    DurationRemaining = 0.0f;
                    return;
                }
                if (_ghost == null) { DurationRemaining = 0.0f; return; }
                if (!_carrying)
                {
                    _ghost.Step(FeetFor(_shoe.transform.position + Vector3.up * .1f), NecroRules.FetchSpeed, dt);
                    // Close in, its arms stretch out at it.
                    _ghost.ReachFor = (_ghost.Position - _shoe.transform.position).sqrMagnitude < 1.4f * 1.4f
                        ? _shoe.transform.position : (Vector3?)null;
                    if (_ghost.Arrived)
                    {
                        _carrying = true;
                        // Its hands go round the slipper's two ends, so it needs to know how long it is and which way.
                        Bounds size = SlipperSize(_shoe);
                        _ghost.Grab(Mathf.Max(size.size.x, size.size.z) * .5f, size.size.x >= size.size.z);
                        GameServices.Audio?.PlayAt("sfx_possess_enter", _ghost.Position);
                    }
                    return;
                }
                // Carrying: it hurries back to her side, low, and then WAITS THERE (owner 2026-10-15: *"the guy js waits
                // annd stays there until she picks it up from him"*, *"it will follow her until she gets it"*). It holds the
                // slipper out to her, still loose and within her pickup reach, and she takes it with her interact button
                // (the normal grab, `Carrier.TryPickup`); that pickup ends it above, with its happy spin. Until then it
                // keeps following her past its 8 s, so the skill's clock holds full while it waits.
                var owner = ctx.Motor.transform;
                _ghost.Step(owner.position + owner.right * .6f + Vector3.up * .15f, NecroRules.FetchSpeed, dt);
                bool waiting = (owner.position - _ghost.Position).sqrMagnitude < 1.4f * 1.4f;
                if (waiting) _ghost.Face(owner.position, dt);
                _ghost.Offering = waiting;
                DurationRemaining = Duration;
                Vector3 at = _ghost.HoldPoint;
                if (!NetAuthority.ShouldResolve())
                {
                    // A peer's ghost lets go when the host's slipper stops following it (dropped, or delivered there).
                    _apart = (_shoe.transform.position - at).sqrMagnitude > 1.5f * 1.5f ? _apart + dt : 0f;
                    if (_apart > .4f) DurationRemaining = 0.0f;
                    return;
                }
                // Carried: the host walks the (loose) slipper in its hands, crosswise between them, and tells everyone.
                _shoe.transform.SetPositionAndRotation(at, _ghost.CarryRotation);
                if (Time.time >= _nextBroadcast) { _nextBroadcast = Time.time + 0.1f; Net.MatchRpc.Instance?.BroadcastSlipperState(_shoe); }
                // THE INTERCEPT (owner: the taya can stop it): a taya who can act and is on it makes it drop it.
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
            }

            /// <summary>The slipper's size in its own axes (measured at the grab, while it lies at its rest rotation).</summary>
            private static Bounds SlipperSize(Slipper shoe)
            {
                var b = new Bounds(shoe.transform.position, Vector3.one * .2f);
                bool any = false;
                var undo = shoe.transform.rotation;
                shoe.transform.rotation = Quaternion.identity;
                foreach (var r in shoe.GetComponentsInChildren<MeshRenderer>())
                {
                    if (r.GetComponent<Visual.VfxRenderTag>() != null) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                shoe.transform.rotation = undo;
                return b;
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
                // Every peer's copy ends here; a carry that ended at her side was a delivery (its happy spin).
                bool delivered = _carrying && _ghost != null && ctx?.Motor != null &&
                    (ctx.Motor.transform.position - _ghost.HoldPoint).sqrMagnitude < 1.6f * 1.6f;
                GroundCarriedSlipper();
                _ghost?.Leave(delivered);
                _ghost = null;
                _shoe = null;
            }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== LOOKOUT (defending)

        /// <summary>
        /// LOOKOUT (owner 2026-10-15, the overhaul of KURO: CATCH!: *"spawn siya ghost na tutulungan siya magdefend (pede ren
        /// magtayo lata) but medyo small and not that good but it helps in some cases"*, *"only stays for like 15 seconds"*).
        /// A small lantern ghost of hers (`NemuLookout`) minds the can for 15 s. If the can is down it stands it back up,
        /// slowly (3.5 s against the taya's 1.5 s), and an attacker within 1.5 m of the can scares it off the job. It does it
        /// once; then its job is done and it goes. It no longer makes the can knock-proof (protocol109's protection).
        /// ⚠️ The ID is Catch's, kept for saves, mastery and the wire. Host-confirmed: every peer runs the same ghost and
        /// clock; only the host stands the can up (`Lata.HostRestore`, then the usual can broadcast).
        /// </summary>
        private sealed class Lookout : HeroAbility, IPreparedWorldReplication
        {
            private readonly NemuHeroKit _kit;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            private NemuLookout _ghost;
            private Lata _can;
            private float _lift;
            private bool _done;

            public Lookout(NemuHeroKit kit)
                : base("nemu_skill2d", "LOOKOUT",
                       "Defending. A lantern ghost guards the can for 15 s. If knocked down, it slowly lifts it back up once. Attackers scare it off.",
                       NecroRules.LookoutCooldown, NecroRules.LookoutSeconds, AbilityGlyph.NemuLookout,
                       summary: "A ghost guards the can and slowly lifts it back up, once.",
                       castAction: "hero-nemu-lookout", viewmodelAction: "lookout-release",
                       castCue: "sfx_cast_nemu_guard") { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx.Motor.IsDefender && ctx.Round?.Lata != null;

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.ShareBasicCooldown(CooldownRemaining, mayLower: false);
                Bind(ctx);
            }

            private void Bind(AbilityContext ctx)
            {
                _can = ctx?.Round?.Lata;
                _lift = 0f; _done = false;
                if (_can == null || ctx?.Motor == null) return;
                _ghost = NemuLookout.Summon(_can.transform, ctx.Motor.transform.position, ctx.Motor.PlayerSlot);
            }

            /// <summary>The nearest attacker to the can who can act, and how near.</summary>
            private static CharacterMotor NearestAttacker(AbilityContext ctx, Vector3 can, out float distance)
            {
                CharacterMotor best = null; distance = float.MaxValue;
                var round = ctx?.Round;
                if (round == null) return null;
                foreach (var p in round.Players)
                {
                    if (p == null || p.IsDefender || p.IsTagged || !p.gameObject.activeInHierarchy) continue;
                    Vector3 d = p.transform.position - can; d.y = 0f;
                    if (d.magnitude < distance) { distance = d.magnitude; best = p; }
                }
                return best;
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_can == null) { Bind(ctx); if (_can == null) { DurationRemaining = 0f; return; } }
                if (_can != ctx?.Round?.Lata) { DurationRemaining = 0f; return; }
                var near = NearestAttacker(ctx, _can.transform.position, out float distance);
                bool scared = near != null && distance <= NecroRules.LookoutScareRadius;
                if (_ghost != null) { _ghost.Watching = near != null ? near.transform.position : (Vector3?)null; _ghost.Scared = scared && !_can.IsUpright; }
                if (_done) return;
                if (_can.IsUpright) { _lift = 0f; if (_ghost != null) _ghost.Lift = 0f; return; }
                if (ctx.Round == null || !ctx.Round.RoundActive || PresentationClock.Held) return;
                // The can is down: it strains to stand it up, and loses ground twice as fast while an attacker is on it.
                _lift = scared ? Mathf.Max(0f, _lift - dt * 2f) : _lift + dt;
                if (_ghost != null) _ghost.Lift = _lift / NecroRules.LookoutRestoreSeconds;
                if (_lift < NecroRules.LookoutRestoreSeconds) return;
                _done = true;
                // It keeps its grip until the can is really up (a peer waits on the host's can broadcast), and cheers.
                if (_ghost != null) { _ghost.Lift = 1f; _ghost.Cheer(); }
                // ⚠️ THE GATE IS WRITTEN OUT, NOT HOISTED: `tools/audit_ability_authority.py` reads it by brace depth.
                if (NetAuthority.ShouldResolve())
                {
                    _can.HostRestore();
                    Net.MatchRpc.Instance?.BroadcastLataState();
                }
                // Its job done, it goes (after its cheer).
                DurationRemaining = Mathf.Min(DurationRemaining, 1.0f);
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
                if (ctx.Round?.Lata == null) return false;
                RestoreLiveClock(remaining);
                Bind(ctx);
                return false;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _ghost?.Leave(); _ghost = null;
                _can = null; _lift = 0f;
            }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== PRUSISYON (ultimate)

        /// <summary>
        /// LET THEM IN (owner brief 2026-10-11, renamed from PRUSISYON 2026-10-13, `docs/reports/nemu-ultimate-2026-10-11/
        /// direction.md` section 11): she stops holding the dead back; her shadow floods the court and the dead pour into every
        /// other player. Each is POSSESSED for 15 s (`CharacterMotor.Possession.cs`): the soul in them contests their steering
        /// toward the worst place for them, and all anyone sees of it is their aura. The cutscene shows the entry, so play
        /// starts with them already possessed (never shown twice).
        /// ⚠️ The ID, glyph and cost are the retired KURO: HAUNT!'s, kept for saves, mastery and the wire. Kuro no longer
        /// chases anyone; `RestoreSeance`/`ReceiveHaunt` stay as inert endpoints for the unchanged familiar message.
        /// </summary>
        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;

            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "LET THEM IN",
                       "Every other player is Possessed for 15 s: the soul inside drags their steering toward danger. Push against it.",
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
                // No cast voice: the cutscene opens on her silent prayer (owner on v10: *"kahit remove the laughing na just start
                // with this"*). `hero_nemu_ult` stays generated but unplayed.
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
