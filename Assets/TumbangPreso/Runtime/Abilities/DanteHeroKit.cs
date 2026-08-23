using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class DanteHeroKit : HeroKit
    {
        public bool IsDemonicCarapaceActive => Skill2 != null && Skill2.IsActive;

        public DanteHeroKit() : base("dante", "DANTE")
        {
            Skill1 = new SeismicStompAbility();
            Skill2 = new DemonicCarapaceAbility();
            Ultimate = new DemonTitanFissureAbility();
        }

        private sealed class SeismicStompAbility : HeroAbility
        {
            public SeismicStompAbility()
                : base("dante_skill1", "SEISMIC STOMP",
                       "Slams the ground under you. Shoves nearby players off their feet and kicks loose tsinelas out of reach.",
                       6.5f, 0.0f, TumbangPreso.UI.AbilityGlyph.Slam,
                       summary: "Ground slam. Shoves players and tsinelas away from you.",
                       telegraphRadius: 2.4f, telegraphRange: 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // Play heavy titan grunt and bass thud
                GameServices.Audio?.PlayAt("hero_dante_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_explosion_heavy", ctx.Position);

                // Squash and stretch ground thump
                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.35f);

                // Small vertical hop
                ctx.Motor.ApplyImpulse(Vector3.up * 4.0f);

                // Spawn cracked lava decal on ground & volcanic rock debris (calibrated to 2.4m)
                HeroHazards.SpawnCrackedLavaDecal(ctx.Position, 2.4f, 4.0f);
                HeroHazards.SpawnVolcanicRockDebris(ctx.Position, 8);
                Visual.AbilityVfx.SpawnMagmaEruption(ctx.Position, 2.4f);

                // Explosion shockwave & comic floatie
                // ⚠️ THE EXPLOSION ALREADY SAYS "THUD!" AT THIS EXACT POINT. A "BONK!" on top
                // of it is two words in the same place for one stomp.
                HeroHazards.CreateExplosion(ctx.Position, 2.4f, 10.0f, 1.2f, ctx.Motor.PlayerSlot, "THUD!");

                var round = ctx.Round;
                if (round != null)
                {
                    // Repel slippers with extra force
                    foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (s != null)
                        {
                            Vector3 sDiff = s.transform.position - ctx.Position;
                            sDiff.y = 0.0f;
                            if (sDiff.magnitude <= 3.2f)
                            {
                                s.Deflect(sDiff.normalized * 18.0f + Vector3.up * 4.5f, 1.2f);
                            }
                        }
                    }
                }
            }
        }

        private sealed class DemonicCarapaceAbility : HeroAbility
        {
            private GameObject _auraGo;
            private readonly GameObject[] _shieldPlates = new GameObject[3];

            // ⚠️ NO TELEGRAPH, AND A ZERO HERE IS A STATEMENT RATHER THAN AN OMISSION. This puts
            // nothing on the ground; drawing a ring under a self-buff would tell the player
            // there is an area to stand in or out of when there is not.
            public DemonicCarapaceAbility()
                : base("dante_skill2", "DEMONIC CARAPACE",
                       "Armours you for a few seconds. Nothing can stun, shove or slip you, so you can walk in and take what you need.",
                       9.0f, 4.0f, TumbangPreso.UI.AbilityGlyph.Shield,
                       summary: "Nothing stuns, shoves or slips you while it holds.")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("guard_block", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "CARAPACE!", UiTheme.HeroEarthBright, 1.25f);

                // Clear any existing stuns immediately
                ctx.Motor.ClearStun();

                // Spawn fiery protective aura sphere
                if (_auraGo != null) UnityEngine.Object.Destroy(_auraGo);
                _auraGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _auraGo.name = "DemonicCarapaceAura";
                _auraGo.transform.SetParent(ctx.Motor.transform, false);
                _auraGo.transform.localPosition = new Vector3(0, 0.9f, 0);
                _auraGo.transform.localScale = Vector3.one * 1.85f;

                // ⚠️⚠️ AT 0.35 ALPHA ON AN OPAQUE MATERIAL THIS WAS A SOLID SPHERE AROUND DANTE'S
                // HEAD. Carapace is a four second self-buff, so for four seconds out of every
                // nine the player casting it could see nothing at all: the aura is drawn at
                // 1.85 scale centred at eye height, and in first person that is the inside of
                // an orange ball. The single worst instance of the opaque-VFX fault, and it was
                // on the one ability whose whole purpose is to let you go and fetch a tsinelas.
                VfxMaterial.Ghost(_auraGo.GetComponent<Renderer>(),
                                  new Color(UiTheme.HeroMagmaCore.r, UiTheme.HeroMagmaCore.g,
                                            UiTheme.HeroMagmaCore.b, 0.28f), 0.55f);

                // Spawn 3 orbiting magma shield plates
                for (int i = 0; i < 3; i++)
                {
                    if (_shieldPlates[i] != null) UnityEngine.Object.Destroy(_shieldPlates[i]);
                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plate.name = $"MagmaPlate_{i}";
                    plate.transform.SetParent(ctx.Motor.transform, false);
                    plate.transform.localScale = new Vector3(0.5f, 0.7f, 0.15f);
                    // The plates are the READABLE half of the buff: three solid slabs orbiting
                    // at 1.25 m tell every other player Dante cannot be stunned right now, from
                    // any angle, without a nameplate icon anybody has to learn.
                    VfxMaterial.Solid(plate.GetComponent<Renderer>(), UiTheme.HeroMagmaCore, 0.6f);
                    VfxMaterial.StripCollider(plate);
                    _shieldPlates[i] = plate;
                }

                // ⚠️⚠️ EMBERS RISING OFF HOT ROCK, FOR EXACTLY AS LONG AS THE ARMOUR HOLDS.
                // Carapace is the one buff other players most need to read: swinging at a
                // carapaced Dante is wasted, and there was nothing at range that said so. The
                // three orbiting plates carry it up close; the embers carry it across the court.
                // Negative gravity is the whole read, because heat is the only thing in this
                // game that goes up.
                Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.MagmaEmber, Duration);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Constantly purge stuns while active
                ctx.Motor.ClearStun();

                // Pulse aura & rotate magma plates
                if (_auraGo != null)
                {
                    float pulse = 1.85f + Mathf.Sin(Time.time * 12.0f) * 0.15f;
                    _auraGo.transform.localScale = Vector3.one * pulse;
                }

                float rotSpeed = 160.0f;
                for (int i = 0; i < 3; i++)
                {
                    if (_shieldPlates[i] != null)
                    {
                        float angle = (Time.time * rotSpeed + i * 120.0f) * Mathf.Deg2Rad;
                        float radius = 1.25f;
                        Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.9f + Mathf.Sin(Time.time * 4.0f + i) * 0.1f, Mathf.Sin(angle) * radius);
                        _shieldPlates[i].transform.localPosition = pos;
                        _shieldPlates[i].transform.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90.0f, 0);
                    }
                }
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                if (_auraGo != null)
                {
                    UnityEngine.Object.Destroy(_auraGo);
                    _auraGo = null;
                }

                for (int i = 0; i < 3; i++)
                {
                    if (_shieldPlates[i] != null)
                    {
                        UnityEngine.Object.Destroy(_shieldPlates[i]);
                        _shieldPlates[i] = null;
                    }
                }
            }
        }

        private sealed class DemonTitanFissureAbility : HeroAbility
        {
            // 4.5 m at 2.2 m out is where `CreateExplosion` actually goes off below. The 5.5 m
            // cone in the description is the wider LAUNCH check, which only catches players
            // inside 50 degrees of the facing, so it is not a circle and a circle must not
            // claim it.
            public DemonTitanFissureAbility()
                : base("dante_ultimate", "TITAN FISSURE",
                       "Splits the court ahead of you. Everyone caught in the crack is thrown into the air and left dizzy.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Burst,
                       summary: "Splits the ground ahead. Launches whoever is in front of you.",
                       telegraphRadius: 4.5f, telegraphRange: 2.2f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_dante_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_explosion_heavy", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "EARTHQUAKE!", UiTheme.HeroEarthBright, 1.5f);
                HeroHazards.SpawnVolcanicRockDebris(ctx.Position, 14);
                Visual.AbilityVfx.SpawnMagmaEruption(ctx.Position + ctx.Forward * 2.5f, 4.5f);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.4f);

                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;
                forward.Normalize();

                var round = ctx.Round;
                var directlyHit = new HashSet<int>();
                if (round != null)
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        float d = diff.magnitude;

                        if (d <= 5.5f)
                        {
                            float angle = Vector3.Angle(forward, diff.normalized);
                            if (angle <= 50.0f)
                            {
                                directlyHit.Add(p.PlayerSlot);
                                Vector3 launch = forward * 8.0f + Vector3.up * 10.0f;
                                p.ApplyImpulse(launch);
                                p.ApplyStagger(2.2f);
                                DizzyStars.Attach(p.transform, 2.2f, UiTheme.HeroEarthBright);
                                ComicPopup.Bonk(p.transform.position);
                            }
                        }
                    }
                }

                // ⚠️⚠️ THE BLAST DRAWS NO WORD OF ITS OWN. This ultimate used to put
                // "EARTHQUAKE!" plus "KABOOM!" plus one "BONK!" per victim on screen inside a
                // single frame: five callouts, four of which the player already knew from the
                // fissure opening in front of them. `null` is the documented way to ask
                // `CreateExplosion` for the physics without the caption.
                HeroHazards.CreateExplosion(ctx.Position + forward * 2.2f, 4.5f, 14.0f, 1.8f,
                    ctx.Motor.PlayerSlot, null, directlyHit);

                // Spawn 4 basalt earth pillars in forward arc
                for (int i = -1; i <= 2; i++)
                {
                    float angle = i * 28.0f;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * forward * (i % 2 == 0 ? 3.8f : 2.6f);
                    HeroHazards.SpawnEarthPillar(ctx.Position + offset, 5.0f);
                }
            }
        }
    }
}
