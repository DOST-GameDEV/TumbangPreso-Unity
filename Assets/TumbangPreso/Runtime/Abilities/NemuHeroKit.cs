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

        private sealed class PhantomPhaseAbility : HeroAbility
        {
            private GameObject _phantomLightGo;

            public PhantomPhaseAbility()
                : base("nemu_skill1", "GHOST STEP",
                       "You go part ghost: faster, and the taya cannot tag you. Picking up a tsinelas ends it early.",
                       8.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.NemuPhase,
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
                : base("nemu_skill2", "ASTRAL PROJECTION",
                       "Sends Kuro your spirit pet out ahead. Possess Kuro, then press again to teleport your body to Kuro.",
                       9.0f, 6.0f, TumbangPreso.UI.AbilityGlyph.NemuAstralPet,
                       summary: "Possess Kuro your spirit pet. Press again to teleport to it.",
                       castAction: "hero-nemu-project",
                       viewmodelAction: "project-spirit")
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
                       telegraphRadius: 3.2f, telegraphRange: 3.5f,
                       castAction: "hero-nemu-seance",
                       viewmodelAction: "seance-channel")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_nemu_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                Vector3 voidPos = ctx.Position + ctx.Forward * 3.5f;
                HeroHazards.SpawnSeanceVoid(voidPos, 3.2f, 5.0f, ctx.Motor.PlayerSlot);
                Visual.AbilityVfx.SpawnVoidWisps(voidPos, 3.2f, 5.0f);
            }
        }
    }
}
