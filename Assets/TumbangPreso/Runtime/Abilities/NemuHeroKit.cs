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
                : base("nemu_skill1", "PHANTOM PHASE", "Phases into spirit realm for 2.5s, immune to tags and shoves.", 8.0f, 2.5f)
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
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
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
            public GhostlyPoltergeistAbility()
                : base("nemu_skill2", "ASTRAL PROJECTION", "Projects into ghost to fly freely; press again or wait to teleport to it.", 9.0f, 6.0f)
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
                    HeroHazards.SpawnGhostPoltergeist(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot);
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
            }
        }

        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "NIGHTMARE SEANCE VOID", "Pulls dropped slippers inward and applies drowsy slow.", 0.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_nemu_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ghost_teleport", ctx.Position);
                Vector3 voidPos = ctx.Position + ctx.Forward * 4.5f;
                HeroHazards.SpawnSeanceVoid(voidPos, 7.5f, 5.0f, ctx.Motor.PlayerSlot);
            }
        }
    }
}
