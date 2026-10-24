using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ SEAN, THE LAVA BRAWLER (owner, 2026-10-24: *"I want sean to be a brawler strength based character that relies on his
    /// muscles"*, mixed with lava, born near Mayon). PRESSURE, LAVA RAM, MOLTEN THROW, MAGMA FIST and MAYON AWAKENS. Every
    /// number is in `SeanRules`; the plan, the six beats and the owner's answers are in `docs/reports/sean-kit-2026-10-24/plan.md`.
    ///
    /// ⚠️ VOLCANIC BOMB IS THE E BUTTON WHILE MOLTEN, in both roles. The ultimate's own button goes through the shared cutscene
    /// path (`HeroAbilitySystem.SubmitSharedUltimate`), which has no second press, while the E press already travels the
    /// ordinary predicted cast path to the host and every observer. Both E abilities therefore answer "throw the bomb" from
    /// `Activate` while Molten, which is the one entry point the owner, the host and the observers all run.
    ///
    /// ⚠️ THE OLD KIT'S PUBLIC SURFACE IS KEPT WHERE OTHER FILES STILL CALL IT: the Stoke constants (the FX study scenes), the
    /// ignition members (MOLTEN THROW loads the held slipper exactly as Empowered Throw did, on the same `FireExplosive`
    /// affinity), `AdoptGate` (the retired Cinder Gate's world object) and the Supernova pose and apex (his cutscene, until it
    /// is rebuilt as MAYON AWAKENS).
    /// </summary>
    public sealed class SeanHeroKit : HeroKit, ITimedKitReplication, IWorldEffectBinding
    {
        // ------------------------------------------------------------------ PRESSURE (passive)

        private int _pressureBars;
        private float _pressureIdle;
        public int PressureBars => _pressureBars;
        /// <summary>Molten keeps the heat full: every shove and RAM erupts.</summary>
        public bool PressureFull => IsMolten || SeanRules.PressureFull(_pressureBars);
        public override float PassiveDuration => SeanRules.PressureCoolSeconds;
        private float PressureCoolLeft => _pressureBars > 0 ? Mathf.Max(0, SeanRules.PressureCoolSeconds - _pressureIdle) : 0;

        /// <summary>The old Steady Ember timer, kept for `Diagnostics.NetSeanProbe`. PRESSURE has no throw-charge bonus.</summary>
        public float SteadyEmberRemaining => 0;

        /// <summary>
        /// Host-side. A body hit landed (a shove, LAVA RAM or MAGMA FIST): adds heat, or spends a full charge on the eruption the
        /// caller is about to apply. Returns whether this hit erupts.
        /// </summary>
        internal bool HostLandHit(CharacterMotor me)
        {
            if (!NetAuthority.ShouldResolve()) return false;
            bool erupt = PressureFull;
            _pressureIdle = 0;
            _pressureBars = erupt && !IsMolten ? 0 : SeanRules.PressureAfterHit(_pressureBars);
            if (me != null) Net.MatchRpc.Instance?.BroadcastTimedKitState(me.PlayerSlot);
            return erupt;
        }

        /// <summary>Host-side. A MAGMA FIST hit adds heat; it never spends it (only a shove or a RAM erupts).</summary>
        internal void HostAddHeat(CharacterMotor me)
        {
            if (!NetAuthority.ShouldResolve()) return;
            _pressureIdle = 0;
            _pressureBars = SeanRules.PressureAfterHit(_pressureBars);
            if (me != null) Net.MatchRpc.Instance?.BroadcastTimedKitState(me.PlayerSlot);
        }

        /// <summary>
        /// The shove hook (`CombatVerbs.ApplyShoveTo`, host or solo). An erupting shove sends its target `EruptDistanceScale`
        /// farther (under the knockback cap) and splashes anyone beside them.
        /// </summary>
        public Vector3 HostShoveLanded(CharacterMotor me, CharacterMotor victim, Vector3 push)
        {
            if (me == null || victim == null || !HostLandHit(me)) return push;
            push = Erupted(push);
            Splash(me, victim);
            // The shove draws its own stars; the eruption is what makes it land one weight heavier.
            MatchFlair.Announce(MatchFlair.Kind.SeanSlam, me.PlayerSlot, victim.PlayerSlot, me.transform.position, 0);
            return push;
        }

        /// <summary>A ground push made `EruptDistanceScale` longer, never past the cap. Its lift is kept.</summary>
        internal static Vector3 Erupted(Vector3 push)
        {
            var flat = new Vector3(push.x, 0, push.z);
            float speed = flat.magnitude;
            if (speed < .01f) return push;
            float erupted = Combat.SpeedForDistance(SeanRules.EruptedDistance(Combat.KnockbackDistance(speed)));
            flat = flat / speed * erupted;
            return new Vector3(flat.x, push.y, flat.z);
        }

        /// <summary>The eruption's splash round the one it erupted on: everyone beside them pushed, never the can.</summary>
        internal static void Splash(CharacterMotor me, CharacterMotor victim)
        {
            Vector3 at = victim.transform.position;
            // The ring on every screen (the host resolves; a client only learns of it from this).
            MatchFlair.Announce(MatchFlair.Kind.SeanSplash, me.PlayerSlot, -1, at, SeanRules.EruptSplashRadius);
            HostBlast(me, at, SeanRules.EruptSplashRadius, SeanRules.EruptSplashPush, SeanRules.EruptSplashStaggerSeconds,
                knocksCan: false, MatchFlair.Kind.SeanSplash, except: victim);
        }

        /// <summary>
        /// ⚠️⚠️ SEAN'S OWN BLAST, AND NOT `HeroHazards.CreateExplosion`, for three reasons that function cannot answer
        /// (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 7, items 2 to 4):
        ///  1. **The ring is the hitbox and the number is the number.** That function falls off to 35% at its edge and adds a
        ///     fixed 5.5 m/s lift, so "everyone thrown 4 m" was only ever true at the centre. Here everyone inside the ring
        ///     slides the written distance, solved from `Friction` like every push in the game.
        ///  2. **Only the bomb knocks the can over.** That function knocks it over for any attacking caster, which gave the
        ///     awakening and an erupting shove next to the can a knockdown the kit never had.
        ///  3. **A blast reaches its own level.** It measured flat distance, so a roof above a street blast was thrown too.
        /// Host-side; each body hit is announced, so every peer draws it and the victim feels it.
        /// </summary>
        internal static void HostBlast(CharacterMotor me, Vector3 center, float radius, float push, float stagger,
            bool knocksCan, MatchFlair.Kind kind, CharacterMotor except = null)
        {
            if (me == null || !NetAuthority.ShouldResolve()) return;
            var round = GameServices.Round;
            if (round == null) return;
            float speed = Combat.SpeedForDistance(push);
            foreach (var p in Rivals(me))
            {
                if (p == except) continue;
                Vector3 to = p.transform.position - center;
                if (!SeanRules.WithinReach(to.y, SeanRules.BlastReachHeight)) continue;
                to.y = 0;
                if (to.magnitude > radius) continue;
                if (to.sqrMagnitude < .01f) { to = p.transform.position - me.transform.position; to.y = 0; }
                if (to.sqrMagnitude < .01f) to = me.transform.forward;
                p.ApplyResolvedImpact(to.normalized * speed + Vector3.up * Balance.ShoveLift);
                if (stagger > 0) p.ApplyStagger(stagger);
                MatchFlair.Announce(kind, me.PlayerSlot, p.PlayerSlot, center, stagger);
            }
            var can = round.Lata;
            if (!knocksCan || can == null || !SeanRules.BombKnocksCan(me.IsDefender)) return;
            Vector3 c = can.transform.position - center;
            if (!SeanRules.WithinReach(c.y, SeanRules.BlastReachHeight)) return;
            c.y = 0;
            if (c.magnitude <= radius) can.HostKnockDown(me.PlayerSlot);
        }

        public override void Tick(AbilityContext context, float dt)
        {
            base.Tick(context, dt);
            // A caught Sean leaves no bomb behind: Molten ends at once and nothing drops (feel-and-readability.md § 4).
            if (IsMolten && context?.Motor != null && context.Motor.IsTagged)
            {
                _bombThrown = true;
                ((MayonAwakensAbility)Ultimate).EndMolten(context);
            }
            StepHeatGlow(context?.Motor);
            if (!float.IsFinite(dt) || dt <= 0 || _pressureBars <= 0) return;
            _pressureIdle += dt;
            int cooled = SeanRules.PressureAfterIdle(_pressureBars, _pressureIdle);
            if (cooled == _pressureBars) return;
            _pressureBars = cooled;
            if (context?.Motor != null && NetAuthority.ShouldResolve())
                Net.MatchRpc.Instance?.BroadcastTimedKitState(context.Motor.PlayerSlot);
        }

        private GameObject _heatGlow;

        /// <summary>
        /// Everyone reads "his next hit erupts" from his body: a glow while the heat is full, on every peer (the bars reach
        /// clients through `HeatWire`). First pass: the lava fists of the art pass replace it with the three-step fist glow.
        /// </summary>
        private void StepHeatGlow(CharacterMotor me)
        {
            bool full = me != null && SeanRules.PressureFull(_pressureBars) && !IsMolten;
            if (full && _heatGlow == null)
                _heatGlow = AbilityVfx.AttachAura(me.transform, AbilityVfx.Aura.FireEmber, SeanRules.PressureCoolSeconds + 1f);
            else if (!full) ReleaseHeatGlow();
        }

        private void ReleaseHeatGlow()
        {
            if (_heatGlow != null) { _heatGlow.SetActive(false); UnityEngine.Object.Destroy(_heatGlow); }
            _heatGlow = null;
        }

        public override void Reset() { ReleaseHeatGlow(); _pressureBars = 0; _pressureIdle = 0; _bombThrown = false; base.Reset(); }
        public override void ResetForRound(AbilityContext context)
        { ReleaseHeatGlow(); _pressureBars = 0; _pressureIdle = 0; _bombThrown = false; base.ResetForRound(context); }

        // ------------------------------------------------------------------ MOLTEN and VOLCANIC BOMB

        private bool _bombThrown;
        /// <summary>The 8 s after MAYON AWAKENS, until the bomb leaves his hands.</summary>
        public bool IsMolten => Ultimate != null && Ultimate.IsActive && !_bombThrown;

        /// <summary>Owner, 2026-10-24: "No knockback" while Molten.</summary>
        public override float IncomingKnockbackDistanceScale => SeanRules.IncomingKnockbackScale(IsMolten);

        /// <summary>Whether the E press throws the bomb instead of the role skill.</summary>
        internal bool BombReady => IsMolten;

        /// <summary>
        /// Whether the E button presents as VOLCANIC BOMB: while it is ready, and for a second after the throw, so the cast
        /// callout and the event feed that read it on the cast's frame name the bomb rather than the role skill.
        /// </summary>
        internal bool ShowsBomb => BombReady || Time.time < _bombShownUntil;
        private float _bombShownUntil = -1;
        internal const string BombName = "VOLCANIC BOMB";
        internal const string BombDescription =
            "Hurl the lava off your arms: a 4.8 m splash throws everyone 4 m, knocks the can over (never your own) and leaves a slowing pool.";

        /// <summary>
        /// The E press while Molten, on every peer that runs the cast. The bomb lands where he aims, as far as he aimed and no
        /// farther than `BombMaxRange`; throwing it ends Molten.
        /// </summary>
        internal void ThrowBomb(AbilityContext ctx)
        {
            if (ctx?.Motor == null || !IsMolten) return;
            Vector3 from = ctx.Motor.transform.position;
            Vector3 way = ctx.AimPoint - from; way.y = 0;
            if (way.sqrMagnitude < .01f) { way = ctx.Forward; way.y = 0; }
            if (way.sqrMagnitude < .0001f) way = ctx.Motor.transform.forward;
            float reach = SeanRules.BombLandingDistance(way.magnitude);
            if (reach < 1f) reach = SeanRules.BombMaxRange * .5f;
            Vector3 land = from + way.normalized * reach;
            // The court under his aim, found from above, so a roof he aims at is where it lands.
            if (VfxShapes.TryGroundUnder(land, 6f, 8f, out float ground)) land.y = ground;
            else land.y = Slipper.FindGroundY(land, from.y);
            _bombThrown = true;
            _bombShownUntil = Time.time + 1f;
            SeanVolcanicBomb.Spawn(ctx.Motor, land, thrown: true);
            // The spectator director cuts to where it will land (the bomb itself is drawn on every peer from the cast).
            if (NetAuthority.ShouldResolve())
                MatchFlair.Announce(MatchFlair.Kind.SeanBombThrown, ctx.Motor.PlayerSlot, -1, land, SeanRules.BombRadius);
            NetCue.Play("sfx_cast_sean_cannon", from);
            ((MayonAwakensAbility)Ultimate).EndMolten(ctx);
        }

        /// <summary>Molten ran out with the bomb still in his hands: it drops at his feet (owner, 2026-10-24).</summary>
        internal void DropBomb(AbilityContext ctx)
        {
            if (ctx?.Motor == null || _bombThrown) return;
            _bombThrown = true;
            SeanVolcanicBomb.Spawn(ctx.Motor, ctx.Motor.transform.position, thrown: false);
        }

        private void ReadyRoleSkillsForBomb()
        {
            (AttackingSkill as MoltenThrowAbility)?.ClearForBomb();
            (DefendingSkill as MagmaFistAbility)?.ClearForBomb();
        }

        // ------------------------------------------------------------------ the old kit's public surface

        /// <summary>Stoke Step's numbers, kept for the FX study scenes that still film it (`Visual.FxStudy`).</summary>
        public const float StokeDistance = 2f, StokeAnticipation = .18f, StokeRecovery = .25f;
        public static float StokeSpeed => Mathf.Sqrt(2f * Balance.Friction * StokeDistance);
        public static float StokeTravelSeconds => StokeSpeed / Balance.Friction;

        /// <summary>True while MOLTEN THROW has the held slipper coated (the ember on the shoe, the status icon).</summary>
        public bool IsIgnitionCannonActive { get; set; }
        public bool IsEmpoweredThrowLoadedFor(Slipper shoe) => ((MoltenThrowAbility)AttackingSkill).LoadedFor(shoe);
        private bool _joinChargeStateSettled;

        public void ConsumeIgnition() { _joinChargeStateSettled = true; IsIgnitionCannonActive = false; }

        public bool RestoreJoiningIgnition(CharacterMotor motor, float remaining)
        {
            // Initial hydration cannot overwrite a newer cast or resurrect a
            // charge already consumed while the joining snapshot was in flight.
            if (motor == null || _joinChargeStateSettled || IsIgnitionCannonActive || AttackingSkill.IsActive) return false;
            _joinChargeStateSettled = true;
            RestoreIgnition(motor, remaining);
            return true;
        }

        public void RestoreIgnition(CharacterMotor motor, float remaining)
        {
            if (motor == null) return;
            var context = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>());
            ((MoltenThrowAbility)AttackingSkill).RestoreCharge(context, remaining);
        }

        /// <summary>The retired Cinder Gate's world object still reports in; nothing places one any more.</summary>
        public void AdoptGate(SeanCinderGate gate) { }

        /// <summary>The Supernova cutscene's apex (its hand-back still computes from it until MAYON AWAKENS replaces it).</summary>
        public const float SupernovaApex = 14.0f * .55f - .5f * Core.Balance.CharacterGravity * .55f * .55f;
        public static float SupernovaApexRise(Vector3 feet)
        {
            const float head = 1.5f;
            return Physics.Raycast(feet + Vector3.up * 1.0f, Vector3.up, out var roof, SupernovaApex + head, ~0, QueryTriggerInteraction.Ignore)
                ? Mathf.Clamp(roof.distance + 1.0f - head, 0f, SupernovaApex) : SupernovaApex;
        }
        /// <summary>No Supernova pose plays any more: MAYON AWAKENS has no leap.</summary>
        public float SupernovaPoseTime => -1;

        // ------------------------------------------------------------------ replication

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(AttackingSkill, IsIgnitionCannonActive ? AttackingSkill.DurationRemaining : 0,
                Ultimate, IsMolten ? Ultimate.DurationRemaining : 0,
                passiveRemaining: SeanRules.HeatWire(_pressureBars, PressureCoolLeft), passiveCapacity: SeanRules.HeatWireCapacity);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            if (motor == null) return false;
            bool changed = RestoreJoiningIgnition(motor, state.PersonalRemaining);
            float molten = Mathf.Clamp(state.UltimateRemaining, 0, SeanRules.MoltenSeconds);
            if (molten > 0 && !Ultimate.IsActive)
            {
                ((MayonAwakensAbility)Ultimate).RestoreMolten(molten);
                _bombThrown = false;
                changed = true;
            }
            // The bars and the cooling, so his fists glow the same on every screen (the host keeps deciding them).
            if (!NetAuthority.ShouldResolve())
            {
                SeanRules.HeatFromWire(state.PassiveRemaining, out int bars, out float cool);
                changed |= bars != _pressureBars;
                _pressureBars = bars; _pressureIdle = bars > 0 ? SeanRules.PressureCoolSeconds - cool : 0;
            }
            return changed;
        }

        public HeroMovementState CaptureMovementState() => ((LavaRamAbility)Skill1).CaptureMovement();
        public bool RestoreJoiningMovement(CharacterMotor motor, HeroMovementState state, float age)
            => motor != null && ((LavaRamAbility)Skill1).RestoreMovement(
                new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>()), state, age);
        public void AdoptMovementFields(int owner) { } // LAVA RAM leaves nothing in the world.
        public void RebindWorldEffects(CharacterMotor motor) { }

        // ------------------------------------------------------------------ the kit

        public SeanHeroKit() : base("sean", "RAGO")
        {
            Skill1 = new LavaRamAbility(this);
            AttackingSkill = new MoltenThrowAbility(this);
            DefendingSkill = new MagmaFistAbility(this);
            Ultimate = new MayonAwakensAbility(this);
        }

        /// <summary>15 charges, as Supernova was: the bomb can knock the lata over by itself.</summary>
        public override float UltimateCost => SeanRules.UltimateCost;

        /// <summary>The bodies a RAM or a fist may hit: everyone else in the round who is on the court and not already tagged.</summary>
        internal static IEnumerable<CharacterMotor> Rivals(CharacterMotor me)
        {
            var round = GameServices.Round;
            if (round == null || me == null) yield break;
            foreach (var p in round.Players)
                if (p != null && p != me && p.gameObject.activeInHierarchy && !p.IsTagged) yield return p;
        }

        // ================================================================== LAVA RAM (signature, both roles)

        private sealed class LavaRamAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly SeanHeroKit _kit;
            private bool _movementKnown, _movementRestored, _hit;
            private Vector3 _heading;
            private GameObject _aura;
            private const float Recover = .25f;

            public LavaRamAbility(SeanHeroKit kit)
                : base("sean_skill1", "LAVA RAM",
                    "Brace, then charge along your aim. You stop on the first player you hit and launch them. A wall stops you instead, dazed.",
                    SeanRules.RamCooldown, SeanRules.RamTravelSeconds + Recover, AbilityGlyph.SeanRush,
                    summary: "Charge; the first player you hit is launched. Walls daze you.",
                    castAction: "hero-sean-dash", viewmodelAction: "thrust-fire", castCue: "sfx_cast_sean_rush")
            { _kit = kit; Windup = SeanRules.RamMinBraceSeconds; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx?.Motor != null && ctx.Motor.IsGrounded && !ctx.Motor.IsFlying;

            public override void Activate(AbilityContext ctx)
            {
                // The brace is drawn from the press on every peer; it decides nothing.
                Visual.SeanStokeFire.Brace(ctx);
                base.Activate(ctx);
                if (_kit.IsMolten) CooldownRemaining = Mathf.Min(CooldownRemaining, SeanRules.RamMoltenCooldown);
            }

            public HeroMovementState CaptureMovement() => IsActive
                ? new HeroMovementState { Remaining = DurationRemaining, Wake = Array.Empty<Vector3>() }
                : HeroMovementState.Empty;

            public bool RestoreMovement(AbilityContext ctx, HeroMovementState state, float age)
            {
                if (!state.Valid(Duration, 0, age) || (state.Wake?.Length ?? 0) != 0
                    || (_movementKnown && (!_movementRestored || !IsActive))) return false;
                float remaining = Mathf.Max(0, state.Remaining - age);
                if (_movementKnown) remaining = Mathf.Min(remaining, DurationRemaining);
                _movementKnown = true; _movementRestored = remaining > 0;
                if (remaining <= 0) { EndEarly(ctx); return true; }
                // The movement snapshot owns position and velocity: restore only the clock, never a second impulse.
                _hit = true;
                RestoreLiveClock(remaining);
                return true;
            }

            public override void Tick(AbilityContext ctx, float dt)
            {
                if ((IsWindingUp || IsActive) && ctx?.Motor != null
                    && (!ctx.Motor.RoundActive || ctx.Motor.IsStunned || ctx.Motor.IsFeared || ctx.Motor.IsRooted))
                { _movementKnown = true; _movementRestored = false; RollBackPredictedCast(ctx, false); }
                base.Tick(ctx, dt);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _movementKnown = true; _movementRestored = false; _hit = false;
                if (ctx?.Motor == null || !ctx.Motor.IsGrounded || ctx.Motor.IsFlying
                    || ctx.Motor.IsStunned || ctx.Motor.IsFeared || ctx.Motor.IsRooted)
                { EndEarly(ctx); return; }
                _heading = ctx.Forward; _heading.y = 0;
                if (_heading.sqrMagnitude < .0001f) { EndEarly(ctx); return; }
                _heading.Normalize();
                // The motor owns friction, collision and confinement: one impulse, no teleport.
                ctx.Motor.ApplyImpulse(_heading * SeanRules.RamSpeed);
                if (Visual.SeanStokeFire.Streak) Visual.SeanStokeFire.Burst(ctx.Motor, _heading);
                else _aura = AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.FireEmber, SeanRules.RamTravelSeconds);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_hit || ctx?.Motor == null || DurationRemaining <= Recover) return;
                var me = ctx.Motor;
                Vector3 at = me.transform.position;

                // The first body in front of him stops the charge. Every peer that runs it stops his slide (only the one that
                // simulates his body actually moves it); the host alone moves the body he hit.
                CharacterMotor first = null; float best = float.MaxValue;
                foreach (var p in Rivals(me))
                {
                    Vector3 to = p.transform.position - at; to.y = 0;
                    float d = to.magnitude;
                    if (d > SeanRules.RamContactReach + .45f || d < .01f) continue;
                    if (Vector3.Dot(to / d, _heading) < .35f) continue;
                    if (d < best) { best = d; first = p; }
                }
                if (first != null)
                {
                    _hit = true;
                    me.HaltExternalSlide();
                    if (NetAuthority.ShouldResolve())
                    {
                        Vector3 push = _heading * SeanRules.RamLaunchSpeed + Vector3.up * Balance.ShoveLift;
                        if (_kit.HostLandHit(me)) { push = Erupted(push); Splash(me, first); }
                        first.ApplyResolvedImpact(push);
                        first.ApplyStagger(SeanRules.RamStumbleSeconds);
                        MatchFlair.Announce(MatchFlair.Kind.SeanSlam, me.PlayerSlot, first.PlayerSlot, at,
                            SeanRules.RamStumbleSeconds);
                        NetCue.Play("sfx_explosion_heavy", first.transform.position);
                    }
                    return;
                }

                // A wall in front of him: he runs into it and is dazed (the host decides the stun). A surface you can walk
                // on (a ramp, a step's top) is not a wall, so charging up a slope never dazes him.
                if (Physics.Raycast(at + Vector3.up * .7f, _heading, out var wall, .55f, ~0, QueryTriggerInteraction.Ignore)
                    && wall.normal.y < .6f
                    && wall.collider.GetComponentInParent<CharacterMotor>() == null
                    && wall.collider.GetComponentInParent<Slipper>() == null
                    && wall.collider.GetComponentInParent<Lata>() == null)
                {
                    _hit = true;
                    me.HaltExternalSlide();
                    if (NetAuthority.ShouldResolve())
                    {
                        me.ApplyStagger(SeanRules.RamWallStunSeconds);
                        // His own daze: stars over him for everyone (the punish window), the thud on his own view.
                        MatchFlair.Announce(MatchFlair.Kind.HeroHit, me.PlayerSlot, me.PlayerSlot, wall.point,
                            SeanRules.RamWallStunSeconds);
                    }
                    NetCue.Play("sfx_slipper_burst", wall.point);
                }
            }

            private void ReleaseAura()
            {
                if (_aura != null) { _aura.SetActive(false); UnityEngine.Object.Destroy(_aura); }
                _aura = null;
            }
            protected override void OnEnd(AbilityContext ctx) => ReleaseAura();
            protected override void OnCancelled(AbilityContext ctx) => ReleaseAura();
            public override void Reset() { ReleaseAura(); base.Reset(); _movementKnown = _movementRestored = _hit = false; }
        }

        // ================================================================== MOLTEN THROW (attacking)

        private sealed class MoltenThrowAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly SeanHeroKit _kit;
            private Slipper _loaded;
            private int _joiningSeat = -1;

            public MoltenThrowAbility(SeanHeroKit kit)
                : base("sean_skill2", "MOLTEN THROW",
                       "Attacking. Coat your held slipper in lava for one throw within 8 s. It flies flat and fast. A can it knocks over is blasted away and lands hot: no reset for 1.5 s.",
                       SeanRules.MoltenThrowCooldown, SeanRules.MoltenThrowLoadSeconds, AbilityGlyph.SeanIgnite,
                       summary: "One heavy throw. The can it knocks over flies and stays hot.",
                       castAction: "hero-sean-ignite", viewmodelAction: "ignite", castCue: "sfx_cast_sean_cannon")
            { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (_kit.BombReady) return base.CanActivate(ctx);
                return base.CanActivate(ctx) && !ctx.Motor.IsDefender && ctx.Carrier?.Held != null
                    && ctx.Carrier.Held.State == SlipperState.Held && ctx.Carrier.Held.Holder == ctx.Motor;
            }

            public override void Activate(AbilityContext ctx)
            {
                if (_kit.BombReady) { _kit.ThrowBomb(ctx); CooldownRemaining = Cooldown; return; }
                base.Activate(ctx);
            }

            // Even with a slipper loaded: the coat stays on the shoe, and E is the bomb until Molten ends.
            internal void ClearForBomb() => CooldownRemaining = 0;

            public override string LiveName => _kit.ShowsBomb ? BombName : EffectiveName;
            public override AbilityGlyph LiveGlyph => _kit.ShowsBomb ? _kit.Ultimate.Glyph : Glyph;
            public override string LiveDescription => _kit.ShowsBomb ? BombDescription : EffectiveDescription;

            public bool LoadedFor(Slipper shoe)
                => _kit.IsIgnitionCannonActive && DurationRemaining > 0 && shoe != null
                    && shoe.State == SlipperState.Held && shoe.Holder != null
                    && (_loaded == shoe || (_loaded == null && _joiningSeat >= 0
                        && shoe.Affinity == SlipperAffinity.FireExplosive && shoe.Holder.PlayerSlot == _joiningSeat));

            protected override void OnActivate(AbilityContext ctx)
            {
                _loaded = ctx.Carrier?.Held; _joiningSeat = -1;
                _kit._joinChargeStateSettled = true;
                _kit.IsIgnitionCannonActive = _loaded != null;
                if (_loaded != null && NetAuthority.ShouldResolve())
                {
                    _loaded.Affinity = SlipperAffinity.FireExplosive;
                    Net.MatchRpc.Instance?.BroadcastSlipperState(_loaded);
                }
                RefreshEmber(ctx);
            }

            private void RefreshEmber(AbilityContext ctx)
            {
                var shoe = ctx.Carrier != null ? ctx.Carrier.Held : null;
                if (LoadedFor(shoe)) SeanIgnitionVisual.Ensure(shoe.GetComponentInChildren<MeshFilter>(), shoe, _kit);
            }

            public void RestoreCharge(AbilityContext ctx, float remaining)
            {
                if (remaining <= 0) { EndEarly(ctx); _kit.IsIgnitionCannonActive = false; return; }
                RestoreLiveClock(Mathf.Min(remaining, SeanRules.MoltenThrowLoadSeconds));
                // Equipment recovery carries the held affinity on the exact object; never adopt an arbitrary held shoe.
                _loaded = null; _joiningSeat = ctx.Motor.PlayerSlot;
                _kit.IsIgnitionCannonActive = true;
                RefreshEmber(ctx);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!_kit.IsIgnitionCannonActive || ctx.Motor.IsDefender) { EndEarly(ctx); return; }
                if (_loaded == null && _joiningSeat >= 0)
                {
                    var held = ctx.Carrier?.Held;
                    if (held == null) return;
                    if (held.Holder != ctx.Motor || held.Affinity != SlipperAffinity.FireExplosive) return;
                    _loaded = held; _joiningSeat = -1;
                }
                if (_loaded == null || ctx.Carrier?.Held != _loaded
                    || _loaded.State != SlipperState.Held || _loaded.Holder != ctx.Motor)
                { EndEarly(ctx); return; }
                RefreshEmber(ctx);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsIgnitionCannonActive = false;
                if (_loaded != null && _loaded.State != SlipperState.InFlight
                    && _loaded.Affinity == SlipperAffinity.FireExplosive && NetAuthority.ShouldResolve())
                {
                    _loaded.Affinity = SlipperAffinity.Normal;
                    Net.MatchRpc.Instance?.BroadcastSlipperState(_loaded);
                }
                _loaded = null; _joiningSeat = -1;
            }

            public override void Reset() { OnEnd(null); base.Reset(); _kit._joinChargeStateSettled = false; }
        }

        // ================================================================== MAGMA FIST (defending)

        private sealed class MagmaFistAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly SeanHeroKit _kit;

            public MagmaFistAbility(SeanHeroKit kit)
                : base("sean_skill2d", "MAGMA FIST",
                    "Defending. Punch a fist of lava at the rival nearest your aim within 9 m. It curves after them; under their feet a giant fist punches up and throws them away from the can.",
                    SeanRules.FistCooldown, 0, AbilityGlyph.SeanCinderGate,
                    summary: "A curving lava fist that punches a rival up and away from the can.",
                    telegraphRadius: SeanRules.FistPunchRadius, telegraphRange: SeanRules.FistLockRange,
                    castAction: "hero-sean-gate", viewmodelAction: "cinder-draw", castCue: "sfx_cast_sean_rush")
            { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (_kit.BombReady) return base.CanActivate(ctx);
                return base.CanActivate(ctx) && ctx.Motor.IsDefender && Target(ctx) != null;
            }

            public override void Activate(AbilityContext ctx)
            {
                if (_kit.BombReady) { _kit.ThrowBomb(ctx); CooldownRemaining = Cooldown; return; }
                base.Activate(ctx);
            }

            internal void ClearForBomb() => CooldownRemaining = 0;

            public override string LiveName => _kit.ShowsBomb ? BombName : EffectiveName;
            public override AbilityGlyph LiveGlyph => _kit.ShowsBomb ? _kit.Ultimate.Glyph : Glyph;
            public override string LiveDescription => _kit.ShowsBomb ? BombDescription : EffectiveDescription;

            /// <summary>The rival nearest his aim inside the lock range and cone, or null.</summary>
            internal static CharacterMotor Target(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return null;
                Vector3 at = ctx.Position; Vector3 aim = ctx.Forward; aim.y = 0;
                if (aim.sqrMagnitude < .0001f) return null;
                aim.Normalize();
                CharacterMotor best = null; float bestAngle = float.MaxValue;
                foreach (var p in Rivals(ctx.Motor))
                {
                    if (p.IsDefender) continue;
                    Vector3 to = p.transform.position - at; to.y = 0;
                    float angle = Vector3.Angle(aim, to);
                    if (!SeanRules.FistCanLock(to.magnitude, angle)) continue;
                    if (angle < bestAngle) { bestAngle = angle; best = p; }
                }
                return best;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                var target = Target(ctx);
                if (target == null || ctx?.Motor == null) return;
                Vector3 way = ctx.Forward; way.y = 0;
                SeanMagmaFist.Spawn(ctx.Motor, _kit, ctx.Position + way.normalized * .7f + Vector3.up * .9f, way, target);
            }
        }

        // ================================================================== MAYON AWAKENS (ultimate)

        private sealed class MayonAwakensAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            // The cutscene shows the awakening, so released after it there is no second wind-up.
            protected override bool IntroductionIsTheWindup => true;
            private readonly SeanHeroKit _kit;
            private GameObject _aura;
            private AbilityContext _context;

            public MayonAwakensAbility(SeanHeroKit kit)
                : base("sean_ultimate", "MAYON AWAKENS",
                       "Let the mountain out: everyone close is blasted away and you are Molten for 8 s, with no knockback and every hit erupting. Press E to throw the Volcanic Bomb.",
                       0.0f, SeanRules.MoltenSeconds, AbilityGlyph.SeanSupernova,
                       summary: "Blast everyone close, turn Molten, then throw the Volcanic Bomb with E.",
                       telegraphRadius: SeanRules.AwakeningRadius, telegraphRange: 0.0f,
                       castAction: "hero-sean-supernova", viewmodelAction: "supernova-slam",
                       castCue: "sfx_cast_sean_supernova")
            {
                _kit = kit;
                TelegraphStyle = Visual.GroundReticle.Style.Ember;
                Windup = UltimateWindup;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _context = ctx;
                _kit._bombThrown = false;
                _kit.ReadyRoleSkillsForBomb();
                if (ctx?.Motor == null) return;
                var me = ctx.Motor;
                NetCue.Play("hero_sean_ult", me.transform.position);
                var squash = me.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.08f);
                // The ring on every peer; the push from the host. Bodies only: the awakening never knocks the can over.
                HeroHazards.CreateExplosionVisual(me.transform.position, SeanRules.AwakeningRadius, "",
                    HeroHazards.ExplosionStyle.Fire);
                HostBlast(me, me.transform.position, SeanRules.AwakeningRadius, SeanRules.AwakeningPush,
                    SeanRules.AwakeningStaggerSeconds, knocksCan: false, MatchFlair.Kind.SeanBlast);
                StartAura(me);
            }

            private void StartAura(CharacterMotor me)
            {
                ReleaseAura();
                _aura = AbilityVfx.AttachAura(me.transform, AbilityVfx.Aura.FireEmber, Mathf.Max(.1f, DurationRemaining));
            }

            /// <summary>The bomb left his hands: Molten is over, and nothing drops.</summary>
            internal void EndMolten(AbilityContext ctx) => EndEarly(ctx);

            internal void RestoreMolten(float remaining)
            {
                RestoreLiveClock(remaining);
                if (_context?.Motor != null) StartAura(_context.Motor);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                ReleaseAura();
                _kit.DropBomb(ctx ?? _context);
                _context = null;
            }

            // A round ending mid-Molten drops nothing.
            protected override void OnCancelled(AbilityContext ctx)
            { ReleaseAura(); _kit._bombThrown = true; _context = null; }

            private void ReleaseAura()
            {
                if (_aura != null) { _aura.SetActive(false); UnityEngine.Object.Destroy(_aura); }
                _aura = null;
            }
        }
    }
}
