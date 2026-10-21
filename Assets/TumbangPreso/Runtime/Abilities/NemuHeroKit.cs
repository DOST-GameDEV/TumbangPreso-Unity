using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Nemu (owner rework 2026-10-21): SOULBOUND passive, PEEKER signature (a hidden ghost, `NemuBlindfold`), FETCHER and
    /// KEEPER role skills. Each ghost that ends leaves a dormant ghost (`NemuDormant`) and its skill's 15 s cooldown waits
    /// until she reclaims it. LET THEM IN possesses every other player for 15 s (`CharacterMotor.Possession.cs`).
    /// </summary>
    public sealed class NemuHeroKit : HeroKit
    {
        /// <summary>Kept for the wire and the tag rule: Phantom Veil is gone, so it is never active.</summary>
        public bool IsPhantomPhaseActive => false;

        /// <summary>Phantom Veil is gone; a rejoiner has no veil to restore.</summary>
        public bool RestoreJoiningVeil(CharacterMotor motor, float remaining) => false;

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
            Skill1 = new DontLook();
            AttackingSkill = new Skitter();
            DefendingSkill = new Lookout();
            Ultimate = new NightmareSeanceVoidAbility();
        }

        public override float UltimateCost => 15.0f;

        // ================================================================== SOULBOUND (passive)

        /// <summary>The dormant ghost this peer's Nemu would reclaim with Grab, for the prompt; null when none.</summary>
        public NemuDormant ReclaimTarget { get; private set; }

        /// <summary>How far through the 1.5 s reclaim she is, 0 to 1.</summary>
        public float ReclaimRatio => Mathf.Clamp01(_reclaim / NemuRules.ReclaimSeconds);

        private float _reclaim;
        private NemuDormant _reclaiming;

        public override void Tick(AbilityContext ctx, float dt)
        {
            base.Tick(ctx, dt);
            StepReclaim(ctx, dt);
        }

        /// <summary>
        /// SOULBOUND's reclaim (owner 2026-10-21: *"Nemu must RETRIEVE SLIPPER / RESET CAN near the ghost (1 meter distance) for
        /// 1.5 seconds to reclaim the ghost"*). Run by the peer that moves her: her own screen for a person, the host for a bot.
        /// Holding Grab within reach of one of her dormant ghosts fills the bar; letting go or stepping away empties it, like
        /// the can's reset. A slipper she could pick up, or a fallen can she could reset, keeps the button (Carrier answers
        /// first). As with the can, the client shows its own bar and the HOST reclaims (`MatchRpc.RequestNemuReclaim`).
        /// </summary>
        private void StepReclaim(AbilityContext ctx, float dt)
        {
            var motor = ctx?.Motor;
            bool bot = motor != null && motor.IsBot;
            if (motor == null || !(bot ? NetAuthority.ShouldResolve() : motor.IsLocalViewer))
            { ReclaimTarget = null; EndReclaim(motor, false); return; }

            // ⚠️ A BOT DOES NOT WALK BACK FOR ITS GHOSTS: the AI has no errand for it, so its dormant ghosts reclaim themselves
            // after the same 1.5 s, wherever it is. People make the trip.
            NemuDormant target;
            if (bot) target = GameServices.Round?.RoundActive == true ? FirstDormant(motor.PlayerSlot) : null;
            else
            {
                target = motor.CanAct() && GameServices.Round?.RoundActive == true && !GrabIsTaken(motor)
                    ? NemuDormant.InReach(motor.PlayerSlot, motor.transform.position) : null;
            }
            ReclaimTarget = target;
            bool holding = target != null && (bot || motor.Intent.Pressed(Verb.Grab));
            if (!holding || target != _reclaiming) EndReclaim(motor, true);
            if (!holding) return;

            if (_reclaiming == null)
            {
                _reclaiming = target;
                if (!bot) motor.GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("grab");
                if (NetAuthority.ShouldRequest())
                    Net.MatchRpc.Instance?.RequestNemuReclaim(motor.PlayerSlot, target.Kind, Net.MatchRpc.ReclaimPhase.Start);
            }
            _reclaim += dt;
            target.LocalProgress = ReclaimRatio;
            if (_reclaim < NemuRules.ReclaimSeconds) return;

            _reclaim = 0f;
            _reclaiming = null;
            target.LocalProgress = 0f;
            if (NetAuthority.ShouldRequest())
            {
                Net.MatchRpc.Instance?.RequestNemuReclaim(motor.PlayerSlot, target.Kind, Net.MatchRpc.ReclaimPhase.Complete);
                return;
            }
            target.Reclaim();
        }

        private void EndReclaim(CharacterMotor motor, bool report)
        {
            if (_reclaiming == null) { _reclaim = 0f; return; }
            if (report && motor != null && NetAuthority.ShouldRequest())
                Net.MatchRpc.Instance?.RequestNemuReclaim(motor.PlayerSlot, _reclaiming.Kind, Net.MatchRpc.ReclaimPhase.Cancel);
            _reclaiming.LocalProgress = 0f;
            _reclaiming = null;
            _reclaim = 0f;
        }

        /// <summary>The button already means something else here: a slipper to pick up, or a fallen can to reset.</summary>
        private static bool GrabIsTaken(CharacterMotor motor)
        {
            var carrier = motor.GetComponent<Carrier>();
            if (motor.IsDefender) return carrier != null && carrier.HasResetTarget;
            if (motor.HoldingSlipper) return false;
            foreach (var slipper in BotSlipperInventory.All)
                if (slipper != null && slipper.CanBeGrabbedBy(motor)) return true;
            return false;
        }

        private static NemuDormant FirstDormant(int slot)
        {
            foreach (var d in NemuDormant.Live) if (d != null && !d.Reclaimed && !d.Expired && d.OwnerSlot == slot) return d;
            return null;
        }

        /// <summary>
        /// A Nemu skill whose cooldown waits for its ghost (SOULBOUND). It is spent at the cast like any other, but while its
        /// ghost is out, or lies dormant waiting for her, the clock is held full; it runs only once she has reclaimed it.
        /// Every peer can see both (the ghost frames carry them), so the owner's own tray holds too.
        /// </summary>
        private abstract class SoulboundAbility : HeroAbility
        {
            protected SoulboundAbility(string id, string name, string description, float cooldown, float duration,
                AbilityGlyph glyph, string summary, string castAction, string viewmodelAction, string castCue,
                float telegraphRadius = 0f, float telegraphRange = 0f)
                : base(id, name, description, cooldown, duration, glyph, summary: summary,
                       telegraphRadius: telegraphRadius, telegraphRange: telegraphRange,
                       castAction: castAction, viewmodelAction: viewmodelAction, castCue: castCue) { }

            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;

            protected abstract NemuDormant.Ghost Ghost { get; }

            /// <summary>Her ghost of this skill is on the court (any phase, leaving included).</summary>
            protected abstract bool GhostOut(int slot);

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && NemuDormant.For(ctx.Motor.PlayerSlot, Ghost) == null;

            /// <summary>The dormant ghost this skill is waiting on, to read how its window ended.</summary>
            private NemuDormant _dormant;

            /// <summary>
            /// The skill's clock follows its ghost (owner's third pass, 2026-10-21): held full while the ghost is out; while it
            /// lies dormant the tile counts down its 10 s window (`NemuRules.DormantSeconds`); reclaimed in time it takes the
            /// normal 15 s, left to fade it takes 25 s (`NecroRules.SoulboundMissedCooldown`). Every peer sees the ghost and
            /// its end (the frames carry reclaimed and faded apart), and the host's own clock reaches the owner's tray anyway.
            /// </summary>
            public override void Tick(AbilityContext ctx, float dt)
            {
                base.Tick(ctx, dt);
                int slot = ctx?.Motor != null ? ctx.Motor.PlayerSlot : -1;
                if (slot < 0) return;
                if (_dormant != null)
                {
                    if (_dormant.Reclaimed) { CooldownRemaining = Cooldown; _dormant = null; }
                    else if (_dormant.Expired) { CooldownRemaining = NecroRules.SoulboundMissedCooldown; _dormant = null; }
                    else CooldownRemaining = Mathf.Max(_dormant.TimeLeft, .01f);
                    return;
                }
                _dormant = null; // a destroyed one (the round ended, or a frame retired it) leaves no verdict
                var waiting = NemuDormant.For(slot, Ghost);
                if (waiting != null)
                {
                    _dormant = waiting;
                    CooldownRemaining = Mathf.Max(waiting.TimeLeft, .01f);
                    return;
                }
                if (CooldownRemaining > 0f && (IsActive || GhostOut(slot))) CooldownRemaining = Cooldown;
            }

            public override void Reset()
            {
                base.Reset();
                _dormant = null;
            }

            /// <summary>The host leaves her dormant ghost where this one ended.</summary>
            protected void GoDormant(Vector3 at, Vector3 facing, int slot)
            {
                // ⚠️ THE GATE IS WRITTEN OUT, NOT HOISTED: `tools/audit_ability_authority.py` reads it by brace depth.
                if (NetAuthority.ShouldResolve())
                    NemuDormant.Spawn(Ghost, at, facing, slot);
            }
        }

        // ================================================================== PEEKER (signature)

        /// <summary>
        /// PEEKER (owner 2026-10-21, retuning DON'T LOOK, the overhaul of KURO: SIT!: *"a tripwire blind"*). Hold to aim,
        /// release: Blindfold, one of her ghosts, waits at the spot and fades from sight over 1.5 s; its 2 m trigger ring stays
        /// drawn on the court. Whoever comes inside it first has it lunge at their eyes (BLINDED: covered and 25 % slower for
        /// 2.5 s). Untripped, it goes after 15 s. Either way it then lies dormant until she reclaims it (SOULBOUND).
        /// ⚠️ The ID is Sit's, kept for saves, mastery and the wire. The cast is host-confirmed and only the host springs it.
        /// </summary>
        private sealed class DontLook : SoulboundAbility
        {
            public DontLook()
                : base("nemu_skill1", "PEEKER",
                       "Hide a ghost at a spot. The first player to step inside its ring is blinded and slowed by 25% for 2.5 s. Lasts 15 s, then lies dormant until you retrieve it.",
                       NecroRules.DontLookCooldown, 0.0f, AbilityGlyph.NemuDontLook,
                       summary: "Hide a ghost. The first to step in its ring is blinded and slowed.",
                       castAction: "hero-nemu-dontlook", viewmodelAction: "dontlook-hush",
                       castCue: "sfx_cast_nemu_dontlook",
                       telegraphRadius: NecroRules.DontLookTriggerRadius, telegraphRange: NecroRules.DontLookMaxRange)
            {
                AimByHolding(NecroRules.DontLookMinRange, NecroRules.DontLookMaxRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Maw;
            }

            protected override NemuDormant.Ghost Ghost => NemuDormant.Ghost.Peeker;

            protected override bool GhostOut(int slot)
            {
                foreach (var g in NemuBlindfold.Live) if (g != null && g.OwnerSlot == slot) return true;
                return false;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                if (NetAuthority.ShouldResolve())
                    NemuBlindfold.Place(AimedDestination(ctx), ctx.Forward, ctx.Motor.PlayerSlot);
            }
        }

        // ================================================================== FETCHER (attacking)

        /// <summary>
        /// FETCHER (owner 2026-10-21, renaming SKITTER, the overhaul of KURO: FETCH!: *"Spawn siya ghost na kukunin slipper for
        /// her"*). A greedy little green ghost of hers (`NemuSkitter`) pops out beside her, scrambles to her loose slipper, grabs
        /// it and hurries it back, then waits by her holding it out until she takes it with INTERACT. The taya no longer makes
        /// it drop it (owner 2026-10-21). When it is done it lies dormant until she reclaims it (SOULBOUND).
        /// ⚠️ The ID is Fetch's, kept for saves, mastery and the wire. HOST-CONFIRMED so the host accepts the cast and moves
        /// or lands the slipper. Protocol163's complete ghost frames carry its path, grab and release to every peer.
        /// </summary>
        private sealed class Skitter : SoulboundAbility
        {
            private Slipper _shoe;
            private NemuSkitter _ghost;
            private bool _carrying;
            private float _nextBroadcast;

            public Skitter()
                : base("nemu_skill2", "FETCHER",
                       "Attacking. A ghost fetches your loose slipper and waits by you until you take it. Then it lies dormant until you retrieve it.",
                       NecroRules.FetchCooldown, NecroRules.FetchSeconds, AbilityGlyph.NemuSkitter,
                       summary: "A ghost brings your slipper back and waits for you.",
                       castAction: "hero-nemu-skitter", viewmodelAction: "skitter-flick",
                       castCue: "sfx_cast_nemu_fetch") { }

            protected override NemuDormant.Ghost Ghost => NemuDormant.Ghost.Fetcher;

            protected override bool GhostOut(int slot)
            {
                foreach (var g in NemuSkitter.Live) if (g != null && g.OwnerSlot == slot) return true;
                return false;
            }

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
                if (!NetAuthority.ShouldResolve()) return;
                _shoe = OwnLooseSlipper(ctx.Motor);
                _carrying = false;
                var owner = ctx.Motor.transform;
                _ghost = NemuSkitter.Summon(owner.position + owner.right * .5f + Vector3.up * .3f, ctx.Forward, ctx.Motor.PlayerSlot);
                // The host owns this ghost's live path; peers receive its complete visual state.
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
            }

            /// <summary>Where its feet go so its hands are on <paramref name="hold"/>.</summary>
            private Vector3 FeetFor(Vector3 hold) => hold - (_ghost.HoldPoint - _ghost.Position);

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Peer visuals follow the host frame; a delayed shoe pose cannot choose another path or grab.
                if (!NetAuthority.ShouldResolve()) return;
                if (_shoe == null || !_shoe.gameObject.activeInHierarchy ||
                    _shoe.OwnerSlot != ctx.Motor.PlayerSlot || _shoe.State != SlipperState.Loose)
                {
                    // A normal pickup or round ownership change wins over an old
                    // fetch. Never reposition equipment that is now held or thrown.
                    // She took it out of its claws (her interact): the hand-over, its happy spin on every peer. It ends the
                    // moment she has it (owner 2026-10-21, third pass) and goes dormant beside her.
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
                // keeps following her past its 8 s. The taya no longer makes it drop it (owner 2026-10-21).
                var owner = ctx.Motor.transform;
                _ghost.Step(owner.position + owner.right * .6f + Vector3.up * .15f, NecroRules.FetchSpeed, dt);
                bool waiting = (owner.position - _ghost.Position).sqrMagnitude < 1.4f * 1.4f;
                if (waiting) _ghost.Face(owner.position, dt);
                _ghost.Offering = waiting;
                DurationRemaining = Duration;
                // Carried: the host walks the (loose) slipper in its hands, crosswise between them, and tells everyone.
                _shoe.transform.SetPositionAndRotation(_ghost.HoldPoint, _ghost.CarryRotation);
                if (Time.time >= _nextBroadcast) { _nextBroadcast = Time.time + 0.1f; Net.MatchRpc.Instance?.BroadcastSlipperState(_shoe); }
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

            protected override void OnEnd(AbilityContext ctx) => Finish(ctx, dormant: true);

            /// <summary>A refused cast or the round's end takes it back with no dormant ghost left behind.</summary>
            protected override void OnCancelled(AbilityContext ctx) => Finish(ctx, dormant: false);

            private void Finish(AbilityContext ctx, bool dormant)
            {
                // Every peer's copy ends here; a carry that ended at her side was a delivery (its happy spin).
                bool delivered = _carrying && _ghost != null && ctx?.Motor != null &&
                    (ctx.Motor.transform.position - _ghost.HoldPoint).sqrMagnitude < 1.6f * 1.6f;
                GroundCarriedSlipper();
                if (dormant && _ghost != null && ctx?.Motor != null) GoDormant(_ghost.Position, _ghost.Forward, ctx.Motor.PlayerSlot);
                _ghost?.Leave(delivered);
                _ghost = null;
                _shoe = null;
            }
        }

        // ================================================================== KEEPER (defending)

        /// <summary>
        /// KEEPER (owner 2026-10-21, retuning LOOKOUT, the overhaul of KURO: CATCH!: *"spawn siya ghost na tutulungan siya
        /// magdefend (pede ren magtayo lata)"*). A small lantern ghost of hers (`NemuLookout`) minds the can for 15 s. Every
        /// time the can is down it stands it back up in 1.5 s, as often as it is knocked, until its time is up; a lift under
        /// way when its time runs out is finished first. Attackers no longer scare it off or undo its progress. Then it lies
        /// dormant until she reclaims it (SOULBOUND).
        /// ⚠️ The ID is Catch's, kept for saves, mastery and the wire. Host-confirmed: peers receive its lift/cheer state.
        /// Only the host stands the can up (`Lata.HostRestore`, then the usual can broadcast).
        /// </summary>
        private sealed class Lookout : SoulboundAbility, IPreparedWorldReplication
        {
            private NemuLookout _ghost;
            private Lata _can;
            private float _lift;

            public Lookout()
                : base("nemu_skill2d", "KEEPER",
                       "Defending. A lantern ghost guards the can for 15 s, standing it back up 1.5 s after every knockdown. Then it lies dormant until you retrieve it.",
                       NecroRules.LookoutCooldown, NecroRules.LookoutSeconds, AbilityGlyph.NemuLookout,
                       summary: "A ghost guards the can and keeps lifting it back up.",
                       castAction: "hero-nemu-lookout", viewmodelAction: "lookout-release",
                       castCue: "sfx_cast_nemu_guard") { }

            protected override NemuDormant.Ghost Ghost => NemuDormant.Ghost.Keeper;

            protected override bool GhostOut(int slot)
            {
                foreach (var g in NemuLookout.Live) if (g != null && g.OwnerSlot == slot) return true;
                return false;
            }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx.Motor.IsDefender && ctx.Round?.Lata != null;

            protected override void OnActivate(AbilityContext ctx) => Bind(ctx);

            private void Bind(AbilityContext ctx)
            {
                _can = ctx?.Round?.Lata;
                _lift = 0f;
                if (_can == null || ctx?.Motor == null) return;
                if (NetAuthority.ShouldResolve())
                    _ghost = NemuLookout.Summon(_can.transform, ctx.Motor.transform.position, ctx.Motor.PlayerSlot);
            }

            /// <summary>The nearest attacker to the can who can act, for the ghost to watch.</summary>
            private static CharacterMotor NearestAttacker(AbilityContext ctx, Vector3 can)
            {
                CharacterMotor best = null; float distance = float.MaxValue;
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
                if (!NetAuthority.ShouldResolve()) return;
                if (_can == null) { Bind(ctx); if (_can == null) { DurationRemaining = 0f; return; } }
                if (_can != ctx?.Round?.Lata) { DurationRemaining = 0f; return; }
                var near = NearestAttacker(ctx, _can.transform.position);
                if (_ghost != null) _ghost.Watching = near != null ? near.transform.position : (Vector3?)null;
                if (_can.IsUpright) { _lift = 0f; if (_ghost != null) _ghost.Lift = 0f; return; }
                if (ctx.Round == null || !ctx.Round.RoundActive) return;
                // ⚠️ IT FINISHES THE LIFT BEFORE IT GOES (owner 2026-10-21: a Keeper that expired mid-lift left the can down
                // with no RESET CAN prompt). While the can is down its clock cannot run out; it ends just after the can is up.
                DurationRemaining = Mathf.Max(DurationRemaining, .01f);
                if (PresentationClock.Held) return;
                // The can is down: it stands it up, whoever is near (owner 2026-10-21: attackers no longer undo it).
                _lift += dt;
                if (_ghost != null) _ghost.Lift = _lift / NecroRules.LookoutRestoreSeconds;
                if (_lift < NecroRules.LookoutRestoreSeconds) return;
                _lift = 0f;
                // It keeps its grip until the can is really up (a peer waits on the host's can broadcast), and cheers.
                if (_ghost != null) { _ghost.Lift = 1f; _ghost.Cheer(); }
                // ⚠️ THE GATE IS WRITTEN OUT, NOT HOISTED: `tools/audit_ability_authority.py` reads it by brace depth.
                if (NetAuthority.ShouldResolve())
                {
                    _can.HostRestore();
                    Net.MatchRpc.Instance?.BroadcastLataState();
                }
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

            protected override void OnEnd(AbilityContext ctx) => Finish(ctx, dormant: true);

            /// <summary>A refused cast or the round's end takes it back with no dormant ghost left behind.</summary>
            protected override void OnCancelled(AbilityContext ctx) => Finish(ctx, dormant: false);

            private void Finish(AbilityContext ctx, bool dormant)
            {
                if (dormant && _ghost != null && ctx?.Motor != null) GoDormant(_ghost.Position, _ghost.Forward, ctx.Motor.PlayerSlot);
                _ghost?.Leave(); _ghost = null;
                _can = null; _lift = 0f;
            }
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
