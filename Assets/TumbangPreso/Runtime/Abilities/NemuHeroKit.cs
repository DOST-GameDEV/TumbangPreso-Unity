using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class NemuHeroKit : HeroKit
    {
        public bool IsPhantomPhaseActive => Skill1 != null && Skill1.IsActive;
        public override float MovementSpeedScale => IsPhantomPhaseActive ? Balance.NemuPhaseSpeedScale : 1f;

        public void RestoreFamiliar(CharacterMotor motor,int mode,Vector3 position,float remaining,float? yaw=null)
        {
            if(motor==null || remaining<=0)return;
            var ctx=new AbilityContext(motor,motor.GetComponent<Carrier>(),motor.GetComponent<CombatVerbs>());
            using(NetCue.SuppressRelay())
            {
                if(mode==1)
                {
                    if(Ultimate.IsActive || Ultimate.IsWindingUp)return;
                    ((GhostlyPoltergeistAbility)Skill2).RestoreProjection(ctx,position,remaining);
                }
                else if(mode==2)
                    ((NightmareSeanceVoidAbility)Ultimate).RestoreSeance(ctx,position,remaining);
                else return;
                if(yaw.HasValue && !float.IsNaN(yaw.Value) && !float.IsInfinity(yaw.Value))
                {
                    var pet=motor.GetComponent<CharacterVisual>()?.Companion;
                    if(pet!=null)pet.transform.rotation=Quaternion.Euler(0,yaw.Value,0);
                }
            }
        }

        public NemuHeroKit() : base("nemu", "NEMU")
        {
            Skill1 = new PhantomPhaseAbility();
            Skill2 = new GhostlyPoltergeistAbility();
            Ultimate = new NightmareSeanceVoidAbility();
        }

        /// <summary>
        /// ⚠️⚠️ THE CHEAPEST ULTIMATE IN THE GAME, AND THAT IS THE POINT OF PRICING THEM
        /// SEPARATELY AT ALL. Seance Void is a zone that drags and slows. It knocks nobody
        /// down, stuns nobody, scores nothing on its own and ends no round: it is the one
        /// ultimate that sets up a play rather than being one.
        ///
        /// Under a single shared cost it was worth exactly what Thunderstrike was worth, so
        /// Nemu paid a round-ending price for a round-shaping power. At 90 she casts it most
        /// rounds, which is what a setup tool should do.
        ///
        /// ⚠️ 10 CHARGES, THE FLOOR OF THE RANGE, WHICH IS 10 LATA KNOCKDOWNS. Was 90 against a
        /// knockdown worth 25, which is 3.6. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 10.0f;

        private sealed class PhantomPhaseAbility : HeroAbility
        {
            private NemuVeilPresentation _veil;
            private bool _wasHolding;
            private bool _longFadeSlow;

            public PhantomPhaseAbility()
                // ⚠️⚠️ 52 s, UP FROM 8.0, AND IT SITS BETWEEN SEAN'S 50 AND DANTE'S 62. Tag
                // immunity is the strongest defensive verb in the game: for 2.5 s the taya
                // simply cannot do their job. It is priced under Carapace only because picking
                // up a tsinelas cancels it, so it cannot be used to complete the retrieval it
                // makes possible. 1.7 casts a round.
                //
                // ⚠️ A COOLDOWN AND NOT CHARGES: it moves and protects her own body and puts
                // nothing on the floor. `HeroAbility.MaxCharges` carries the rule.
                // ⚠️ NEMU IS THE SUBJECT OF HER OWN KIT. Kuro still carries the projection and
                // the ultimate's landing point, but repeating his name in every title made the
                // hero read like an accessory to her pet. This tile teaches the actual verb:
                // Nemu phases, surges and denies a tag.
                : base("nemu_skill1", "PHANTOM VEIL",
                       "Surge into a ghostly veil. Move faster and ignore tags briefly; picking up a new slipper ends the veil.",
                       52.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.NemuPhase,
                       summary: "Move faster and ignore tags. A new pickup ends the veil.",
                       castAction: "hero-nemu-ghoststep",
                       viewmodelAction: "ghost-step",
                       castCue: "sfx_cast_nemu_veil")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _longFadeSlow=ctx.HasVariant("nemu.1.fade");
                if (_longFadeSlow) ctx.Motor.EnterSpeedZone(.65f);
                _wasHolding=ctx.Motor.HoldingSlipper;
                NetCue.Play("hero_nemu_grunt",ctx.Position);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(.04f);
                ctx.Motor.ApplyImpulse(ctx.Forward*5.5f);
                if (_veil!=null) _veil.Release();
                _veil=NemuVeilPresentation.Attach(ctx.Motor,Duration);
            }

            protected override void OnTick(AbilityContext ctx,float dt)
            {
                bool holding=ctx.Motor.HoldingSlipper;
                // Reclaiming ends the veil. A shoe held BEFORE the cast is not a
                // new acquisition and must not waste the full cooldown next frame.
                if (holding && !_wasHolding) DurationRemaining=0;
                _wasHolding=holding;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                if (_longFadeSlow) {ctx.Motor.ExitSpeedZone(.65f);_longFadeSlow=false;}
                if (_veil!=null) {_veil.Release();_veil=null;}
            }
        }

        private sealed class GhostlyPoltergeistAbility : HeroAbility
        {
            private GameObject _projectedGhost;

            public GhostlyPoltergeistAbility()
                // ⚠️⚠️ ONE CHARGE A ROUND AND NO RECHARGE. It puts a body on the court that
                // everyone else has to react to, which is the charge half of the split even
                // though what it leaves is a pet rather than a zone: the test is whether the
                // ability creates a thing the other three play around, and Kuro is exactly that.
                //
                // ⚠️⚠️ IT WAS TWO, AND THE SECOND PIP WAS TELLING THE PLAYER SOMETHING UNTRUE
                // ABOUT WHAT THE KEY DOES. 🧑 2026-08-27: *"why does nemu have 2 charges if its
                // just recast? should just show 1"*. This is the only power in the game where the
                // SECOND press of the same key is part of the SAME cast, so a deck tile showing
                // two pips reads as "you get two presses" when what you actually get is one trip
                // out and one trip home. One pip and one trip is the honest version of the tile,
                // and it is also the version the ability's own sentence already describes.
                //
                // ⚠️ THE REACTIVATION IS FREE AND MUST STAY FREE. `CanReactivate` returns the
                // trip home, and `HeroKit.Fire` deliberately does not gate a reactivation on
                // readiness. A charge is spent on the way OUT only, so a player can never be
                // stranded in a possession with no charge left to come back with. At one charge
                // that is no longer a nicety, it is the only thing standing between the player
                // and a permanent possession.
                : base("nemu_skill2", "ASTRAL HIJACK",
                       "Scout as your familiar; recast to bring Nemu to it. Possession waits until it finishes feeding or returning.",
                       0.0f, 6.0f, TumbangPreso.UI.AbilityGlyph.NemuAstralPet,
                       summary: "Possess your familiar; recast to teleport to it.",
                       castAction: "hero-nemu-project",
                       viewmodelAction: "project-spirit",
                       castCue: "sfx_cast_nemu_hijack",
                       charges: 1)
            {
            }

            public void RestoreProjection(AbilityContext ctx,Vector3 position,float remaining)
            {
                var pet=ctx.Motor.GetComponent<CharacterVisual>()?.Companion;
                if(pet==null)return;
                if(!pet.IsPossessed)pet.BeginPossession(ctx.Motor,ctx.GainScale("nemu.2.leash"));
                pet.ApplyCastAnchor(position);RestoreLiveClock(remaining);
            }

            public override bool CanReactivate => true;

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                var pet=ctx.Motor.GetComponent<CharacterVisual>()?.Companion;
                return pet==null || (!pet.IsDevouring && !pet.IsReturning);
            }

            /// <summary>
            /// ⚠️ HOW MUCH LONGER KURO'S PROJECTED BODY LIVES THAN THE ABILITY THAT SPAWNED IT.
            /// Half a second, and the only thing it has to be is greater than zero: it makes
            /// `OnEnd` the thing that removes the ghost in every run, rather than a race between
            /// two independent clocks that nothing was keeping in step. The old arrangement was
            /// a 4.0 s ghost under a 6.0 s ability, which lost that race by two seconds.
            /// </summary>
            private const float ProjectionOutlivesAbilityBy = 0.5f;

            protected override void OnActivate(AbilityContext ctx)
            {

                var visual = ctx.Motor.GetComponent<Visual.CharacterVisual>();
                if (visual != null && visual.Companion != null)
                {
                    visual.Companion.BeginPossession(ctx.Motor,
                        ctx.GainScale("nemu.2.leash"));
                }
                else
                {
                    // ⚠️ ITS LIFETIME IS THIS ABILITY'S, NOT A NUMBER OF ITS OWN. The margin
                    // exists so `OnEnd` below always reaches the ghost before the ghost reaches
                    // its own expiry: whichever of the two runs first decides whether Nemu gets
                    // a trip home, and it has to be this one every time.
                    _projectedGhost = HeroHazards.SpawnGhostPoltergeist(
                        ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot,
                        Duration + ProjectionOutlivesAbilityBy);
                }
            }

            protected override void OnEnd(AbilityContext ctx) => FinishProjection(ctx,true);
            protected override void OnCancelled(AbilityContext ctx) => FinishProjection(ctx,false);

            private void FinishProjection(AbilityContext ctx,bool relocate)
            {
                var caster=ctx?.Motor;
                var visual=caster!=null?caster.GetComponent<CharacterVisual>():null;
                if(visual!=null && visual.Companion!=null && visual.Companion.IsPossessed)
                {
                    if(relocate) NetCue.Play("sfx_ghost_teleport",visual.Companion.transform.position);
                    visual.Companion.EndPossession(teleportNemu:relocate);
                }
                if(_projectedGhost!=null)
                {
                    if(relocate && caster!=null)
                    {
                        Vector3 destination=_projectedGhost.transform.position;
                        NetCue.Play("sfx_ghost_teleport",destination);
                        if(Application.isPlaying)caster.Teleport(destination);else caster.transform.position=destination;
                    }
                    if(Application.isPlaying)UnityEngine.Object.Destroy(_projectedGhost);
                    else UnityEngine.Object.DestroyImmediate(_projectedGhost);
                    _projectedGhost=null;
                }
            }
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
                _familiar=pet;pet.transform.rotation=_castFacing;pet.RestoreDevour(position,Duration,remaining);
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
                    companion.Devour(Duration);
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
