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

        /// <summary>
        /// ⚠️ THE CHEAPEST OF THE FOUR BURST ULTIMATES, BECAUSE IT IS THE ONLY ONE THAT CAN
        /// WHIFF ENTIRELY. Titan Fissure resolves in a 50 degree cone in front of Dante, so a
        /// court that has scattered gives him nothing at all, where Thunderstrike and Glacial
        /// Nova both go off around the caster and cannot miss. Paying less for a power you have
        /// to earn the position for is the right way round.
        ///
        /// ⚠️ 12 CHARGES, WHICH IS 12 LATA KNOCKDOWNS. Was 110 against a knockdown worth 25,
        /// which is 4.4. `docs/Hero_Strike_Balance.md` § 3.1 and `Balance`'s economy block.
        /// </summary>
        public override float UltimateCost => 12.0f;

        private sealed class SeismicStompAbility : HeroAbility
        {
            public SeismicStompAbility()
                // ⚠️⚠️ TWO CHARGES A ROUND AND NO RECHARGE. It leaves a cracked decal on the
                // court, so it is on the charge half of the split, and it is deliberately one
                // of the abilities that simply RUNS OUT: a kit where everything comes back is a
                // kit with cooldowns and extra bookkeeping. Dante's second charge is the last
                // one he gets, and choosing when to spend it is the skill.
                //
                // ⚠️ 2.2 m, DOWN FROM 2.4. Marginal, and it is what brings the blast from 9.23
                // per cent of the box to 7.76, inside the 3 to 8 per cent `docs/VISION.md` § 2
                // rule 1 asks for. The area it loses comes back as relief: the decal now carries
                // a raised cracked lip and standing rock chunks rather than being a flat plane.
                : base("dante_skill1", "SEISMIC STOMP",
                       "Slams the ground under you. Shoves nearby players off their feet and kicks loose tsinelas out of reach.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.DanteStomp,
                       summary: "Ground slam. Shoves players and tsinelas away from you.",
                       telegraphRadius: 2.2f, telegraphRange: 0.0f,
                       castAction: "hero-dante-stomp",
                       viewmodelAction: "stomp-heavy",
                       castCue: "sfx_cast_dante_stomp",
                       charges: 2)
            {
                TelegraphStyle = Visual.GroundReticle.Style.Fissure;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                float radius = 2.2f * ctx.GainScale("dante.1.tremor");
                float knockback = 10.0f * ctx.CostScale("dante.1.tremor");
                // Play heavy titan grunt and bass thud
                NetCue.Play("hero_dante_grunt", ctx.Position);

                // Squash and stretch ground thump
                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.35f);

                // Small vertical hop
                ctx.Motor.ApplyImpulse(Vector3.up * 4.0f);

                // Cracked lava decal and volcanic debris, calibrated to the 2.2 m telegraph.
                HeroHazards.SpawnCrackedLavaDecal(ctx.Position, radius, 4.0f);
                // ⚠️ THE RADIUS IS THE TELEGRAPH'S. The rocks are thrown off the RIM of the break
                // rather than out of a point at the centre, so this has to be the same 2.2 m the
                // decal and the blast use or eight rocks would leave ground that showed nothing.
                HeroHazards.SpawnVolcanicRockDebris(ctx.Position, 8, radius);
                Visual.AbilityVfx.SpawnMagmaEruption(ctx.Position, radius);

                // Explosion shockwave & comic floatie
                // ⚠️ THE EXPLOSION ALREADY SAYS "THUD!" AT THIS EXACT POINT. A "BONK!" on top
                // of it is two words in the same place for one stomp.
                // ⚠️ QUAKE, NOT FIRE. Dante breaks the ground; he does not set it alight. The
                // style drops the fireball entirely, throws rock instead of embers and plays
                // `sfx_quake_slam` rather than the leftover bomb every kit used to share.
                HeroHazards.CreateExplosion(ctx.Position, radius, knockback, 1.2f, ctx.Motor.PlayerSlot, "THUD!",
                    style: HeroHazards.ExplosionStyle.Quake, facing: ctx.Forward);

                var round = ctx.Round;

                // ⚠️⚠️ LONG TREMOR SWEEPS FEET INSTEAD OF THROWING BODIES, AND THAT IS WHAT
                // MAKES IT AN ABILITY RATHER THAN A PERCENTAGE. 🧑 2026-09-02: *"i want each
                // loadout skill to feel thoroughly unique and actually add value and feel like a
                // niche kit"*. Every alternate in the table moved one number, and a player cannot
                // feel 25 per cent of a knockback; they can absolutely feel whether the person
                // they stomped flew away or fell over at their feet.
                //
                // ⚠️ IT IS A SIDEGRADE AND NOT A BONUS, WHICH IS THE RULE THE WHOLE SYSTEM RESTS
                // ON (`AbilityVariant`, `HeroLoadoutTests.IsBudgetNeutral`). The trip REPLACES
                // the launch the cost already paid for: a knocked-down attacker is still beside
                // the lata and can mash out of it, where a launched one is metres away and
                // upright. Which of those you want depends on whether you are the taya, so it is
                // a decision rather than an upgrade.
                //
                // ⚠️ HOST-ONLY, LIKE EVERY OTHER THING IN THIS METHOD THAT MOVES A BODY.
                // `tools/audit_ability_authority.py` reads exactly this gate at exactly this
                // brace depth and every `other` row it prints must read HOST-ONLY.
                if (round != null && NetAuthority.ShouldResolve()
                    && ctx.HasVariant("dante.1.tremor"))
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        if (diff.magnitude > radius) continue;

                        p.ApplyTrip();
                    }
                }

                if (round != null && NetAuthority.ShouldResolve())
                {
                    // Repel slippers with extra force
                    foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (s != null)
                        {
                            Vector3 sDiff = s.transform.position - ctx.Position;
                            sDiff.y = 0.0f;
                            // ⚠️ 2.6 m, DOWN FROM 3.2, SO THE SLIPPER KICK STAYS INSIDE THE
                            // THING THE PLAYER CAN SEE. The telegraph is 2.2 m and this used to
                            // reach a metre past it, which is the fault `HeroAbility.TelegraphRadius`
                            // exists to stop: a tsinelas flying away from ground that showed
                            // nothing reads as the game taking it, not as Dante kicking it.
                            // A small margin is kept because a slipper is an object with width.
                            if (sDiff.magnitude <= 2.6f)
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
            private bool _heavySlow;

            // ⚠️ NO TELEGRAPH, AND A ZERO HERE IS A STATEMENT RATHER THAN AN OMISSION. This puts
            // nothing on the ground; drawing a ring under a self-buff would tell the player
            // there is an area to stand in or out of when there is not.
            public DemonicCarapaceAbility()
                // ⚠️⚠️ 62 s, UP FROM 9.0, AND IT IS THE LONGEST COOLDOWN IN THE GAME. Four
                // seconds of immunity to stun, shove and slip is a free retrieval, and a free
                // retrieval is a point: `docs/VISION.md` § 0 says the run back in for your
                // tsinelas is the only moment you can be caught, and this is the one power that
                // deletes that moment outright. At 9 s it was up for four seconds out of every
                // nine. Not quite one and a half casts a round is the right number for a power
                // that ignores the game's central risk.
                //
                // ⚠️ A COOLDOWN AND NOT CHARGES: it protects your own body, so the rule on
                // `HeroAbility.MaxCharges` applies. It also puts nothing on the floor, which is
                // the other half of the same rule.
                : base("dante_skill2", "DEMONIC CARAPACE",
                       "Armours you for a few seconds. Nothing can stun, shove or slip you, so you can walk in and take what you need.",
                       62.0f, 4.0f, TumbangPreso.UI.AbilityGlyph.DanteShield,
                       summary: "Nothing stuns, shoves or slips you while it holds.",
                       castAction: "hero-dante-roar",
                       viewmodelAction: "carapace-guard",
                       castCue: "sfx_cast_dante_carapace")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _heavySlow = ctx.HasVariant("dante.2.plating");
                if (_heavySlow) ctx.Motor.EnterSpeedZone(0.70f);
                NetCue.Play("guard_block", ctx.Position);

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
                if (_heavySlow)
                {
                    ctx.Motor.ExitSpeedZone(0.70f);
                    _heavySlow = false;
                }

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
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.DanteFissure,
                       summary: "Splits the ground ahead. Launches whoever is in front of you.",
                       telegraphRadius: 4.5f, telegraphRange: 2.2f,
                       castAction: "hero-dante-fissure",
                       viewmodelAction: "fissure-slam",
                       castCue: "sfx_cast_dante_fissure")
            {
                TelegraphStyle = Visual.GroundReticle.Style.Fissure;
                Windup = UltimateWindup;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_dante_ult", ctx.Position);
                HeroHazards.SpawnVolcanicRockDebris(ctx.Position, 14, 4.5f);
                Visual.AbilityVfx.SpawnMagmaEruption(ctx.Position + ctx.Forward * 2.5f, 4.5f);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.4f);

                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;
                forward.Normalize();

                var round = ctx.Round;
                var directlyHit = new HashSet<int>();
                if (round != null && NetAuthority.ShouldResolve())
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
                                // ⚠️ 8. An ultimate, one step under Cheska because the
                                // fissure knocks down as well as holding, so the victim is
                                // already paying twice.
                                p.ApplyStagger(2.2f, StunElement.Stone, 8);
                                DizzyStars.Attach(p.transform, 2.2f, UiTheme.HeroEarthBright);
                                ComicPopup.Bonk(p.transform.position);
                                Visual.HitFeel.Land(p, Visual.HitFeel.Weight.Ultimate,
                                                    UiTheme.HeroEarthBright, ctx.Position);
                            }
                        }
                    }
                }

                // ⚠️⚠️ THE BLAST DRAWS NO WORD OF ITS OWN. This ultimate used to put
                // "EARTHQUAKE!" plus "KABOOM!" plus one "BONK!" per victim on screen inside a
                // single frame: five callouts, four of which the player already knew from the
                // fissure opening in front of them. `null` is the documented way to ask
                // `CreateExplosion` for the physics without the caption.
                // ⚠️ The front points where the fissure was thrown. This is the one blast in the
                // game that is AIMED (cast 2.2 m ahead), so it is the one that has a direction
                // worth drawing.
                HeroHazards.CreateExplosion(ctx.Position + forward * 2.2f, 4.5f, 14.0f, 1.8f,
                    ctx.Motor.PlayerSlot, null, directlyHit, HeroHazards.ExplosionStyle.Quake, forward);

                // ⚠️⚠️ TWO PILLARS ON THE FISSURE LINE, DOWN FROM FOUR IN A 28 DEGREE ARC, AND
                // THIS IS A READABILITY FIX RATHER THAN A BALANCE ONE. `docs/VISION.md` § 2
                // rule 2: *"An ultimate may be big. One at a time. A single cast should not
                // paint the floor twice."* This cast placed THREE geometries on one keypress: a
                // 4.5 m explosion (32.5 per cent of the box), four 1.4 m pillar hazards fanned
                // out to 3.8 m (a further 12.6 per cent), and a 5.5 m / 50 degree launch cone
                // matching neither. Four pillars in an arc read as a second ability going off
                // beside the first.
                //
                // Two, flanking the crack at +/- 1.6 m across the facing, read as the EDGES of
                // the fissure: the same silhouette job, one effect instead of two, and 6.3 per
                // cent instead of 12.6. `docs/Hero_Strike_Balance.md` § 1.2 and § 3.2.
                Vector3 across = Vector3.Cross(Vector3.up, forward).normalized;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 offset = forward * 2.9f + across * (1.6f * side);
                    HeroHazards.SpawnEarthPillar(ctx.Position + offset, 5.0f);
                }
            }
        }
    }
}
