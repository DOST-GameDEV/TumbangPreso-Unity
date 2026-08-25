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
        /// rounds, which is what a setup tool should do. 90 is 3.6 lata knockdowns.
        /// `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 90.0f;

        private sealed class PhantomPhaseAbility : HeroAbility
        {
            private GameObject _phantomLightGo;

            public PhantomPhaseAbility()
                // ⚠️⚠️ 36 s, UP FROM 8.0, AND IT SITS BETWEEN SEAN'S 34 AND DANTE'S 45. Tag
                // immunity is the strongest defensive verb in the game: for 2.5 s the taya
                // simply cannot do their job. It is priced under Carapace only because picking
                // up a tsinelas cancels it, so it cannot be used to complete the retrieval it
                // makes possible. 2.5 casts a round.
                //
                // ⚠️ A COOLDOWN AND NOT CHARGES: it moves and protects her own body and puts
                // nothing on the floor. `HeroAbility.MaxCharges` carries the rule.
                : base("nemu_skill1", "GHOST STEP",
                       "You go part ghost: faster, and the taya cannot tag you. Picking up a tsinelas ends it early.",
                       36.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.NemuPhase,
                       summary: "Faster, and untaggable. Picking up a tsinelas ends it.",
                       castAction: "hero-nemu-ghoststep",
                       viewmodelAction: "ghost-step")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_nemu_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "PHANTOM!", UiTheme.HeroSpiritBright, 1.25f);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.3f);

                // Mini forward slip
                ctx.Motor.ApplyImpulse(ctx.Forward * 5.5f);

                if (_phantomLightGo != null) UnityEngine.Object.Destroy(_phantomLightGo);
                _phantomLightGo = new GameObject("PhantomGhostLight");
                _phantomLightGo.transform.SetParent(ctx.Motor.transform, false);
                _phantomLightGo.transform.localPosition = new Vector3(0, 1.0f, 0);
                var light = _phantomLightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = UiTheme.HeroSpiritBright;
                light.range = 5.0f;
                light.intensity = 3.5f;

                // ⚠️⚠️ THE AURA IS THE ONLY THING THAT TELLS ANYBODY ELSE SHE CANNOT BE TAGGED.
                // Phantom Phase grants tag immunity for 2.5 s, and until this landed the ONLY
                // sign of it was a point light on her own model, which a taya chasing her from
                // behind cannot see at all. A taya who lunges at a phased Nemu and gets nothing
                // has been given no way to know why. World-simulated, so the motes trail behind
                // her instead of riding along.
                //
                // ⚠️ IT IS SIZED TO THE ABILITY'S OWN DURATION AND DESTROYS ITSELF. Nothing in
                // `OnEnd` has to remember it, which is where the previous aura leak came from.
                Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.VoidWisp, Duration);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (ctx.Motor.HoldingSlipper)
                {
                    DurationRemaining = 0.0f;
                    GameServices.Audio?.PlayAt("slipper_land", ctx.Position);
                    ComicPopup.Spawn(ctx.Position, "PHASE BROKEN!", UiTheme.HeroSpiritBright, 1.0f);
                    return;
                }

                // Speed boost during phantom phase
                ctx.Motor.ApplyImpulse(ctx.Forward * 3.0f * dt);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                if (_phantomLightGo != null)
                {
                    UnityEngine.Object.Destroy(_phantomLightGo);
                    _phantomLightGo = null;
                }
            }
        }

        private sealed class GhostlyPoltergeistAbility : HeroAbility
        {
            private GameObject _projectedGhost;

            public GhostlyPoltergeistAbility()
                // ⚠️⚠️ TWO CHARGES A ROUND AND NO RECHARGE. It puts a body on the court that
                // everyone else has to react to, which is the charge half of the split even
                // though what it leaves is a pet rather than a zone: the test is whether the
                // ability creates a thing the other three play around, and Kuro is exactly that.
                //
                // ⚠️ THE REACTIVATION IS FREE AND MUST STAY FREE. `CanReactivate` returns the
                // trip home, and `HeroKit.Fire` deliberately does not gate a reactivation on
                // readiness. A charge is spent on the way OUT only, so a player can never be
                // stranded in a possession with no charge left to come back with.
                : base("nemu_skill2", "ASTRAL PROJECTION",
                       "Sends Kuro your spirit pet out ahead. Possess Kuro, then press again to teleport your body to Kuro.",
                       0.0f, 6.0f, TumbangPreso.UI.AbilityGlyph.NemuAstralPet,
                       summary: "Possess Kuro your spirit pet. Press again to teleport to it.",
                       castAction: "hero-nemu-project",
                       viewmodelAction: "project-spirit",
                       charges: 2)
            {
            }

            public override bool CanReactivate => true;

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                ComicPopup.Boo(ctx.Position);

                var visual = ctx.Motor.GetComponent<Visual.CharacterVisual>();
                if (visual != null && visual.Companion != null)
                {
                    visual.Companion.BeginPossession(ctx.Motor);
                }
                else
                {
                    _projectedGhost = HeroHazards.SpawnGhostPoltergeist(
                        ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot);
                }
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                var visual = ctx.Motor.GetComponent<Visual.CharacterVisual>();
                if (visual != null && visual.Companion != null && visual.Companion.IsPossessed)
                {
                    GameServices.Audio?.PlayAt("sfx_ghost_teleport", visual.transform.position);
                    visual.Companion.EndPossession(teleportNemu: true);
                }
                else if (_projectedGhost != null)
                {
                    Vector3 destination = _projectedGhost.transform.position;
                    GameServices.Audio?.PlayAt("sfx_ghost_teleport", destination);

                    // Runtime movement goes through CharacterMotor so its controller and
                    // ground-settle state stay coherent. EditMode ability tests have no live
                    // controller, but still exercise the complete reactivation lifecycle.
                    if (Application.isPlaying) ctx.Motor.Teleport(destination);
                    else ctx.Motor.transform.position = destination;

                    if (Application.isPlaying) UnityEngine.Object.Destroy(_projectedGhost);
                    else UnityEngine.Object.DestroyImmediate(_projectedGhost);
                    _projectedGhost = null;
                }
            }
        }

        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "SEANCE VOID",
                       "Opens a vortex in front of you. It drags players and loose tsinelas in, and slows anyone caught inside.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.NemuSeanceVoid,
                       summary: "A vortex ahead. Drags players and loose tsinelas into it.",
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
                       telegraphRadius: 2.8f, telegraphRange: 3.5f,
                       castAction: "hero-nemu-seance",
                       viewmodelAction: "seance-channel")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_nemu_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                Vector3 voidPos = ctx.Position + ctx.Forward * 3.5f;
                HeroHazards.SpawnSeanceVoid(voidPos, 2.8f, 5.0f, ctx.Motor.PlayerSlot);
                Visual.AbilityVfx.SpawnVoidWisps(voidPos, 2.8f, 5.0f);
            }
        }
    }
}
