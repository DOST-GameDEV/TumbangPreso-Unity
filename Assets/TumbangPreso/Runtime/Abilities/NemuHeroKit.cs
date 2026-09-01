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
        /// rounds, which is what a setup tool should do.
        ///
        /// ⚠️ 10 CHARGES, THE FLOOR OF THE RANGE, WHICH IS 10 LATA KNOCKDOWNS. Was 90 against a
        /// knockdown worth 25, which is 3.6. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 10.0f;

        private sealed class PhantomPhaseAbility : HeroAbility
        {
            private GameObject _phantomLightGo;
            private bool _longFadeSlow;

            public PhantomPhaseAbility()
                // ⚠️⚠️ 36 s, UP FROM 8.0, AND IT SITS BETWEEN SEAN'S 34 AND DANTE'S 45. Tag
                // immunity is the strongest defensive verb in the game: for 2.5 s the taya
                // simply cannot do their job. It is priced under Carapace only because picking
                // up a tsinelas cancels it, so it cannot be used to complete the retrieval it
                // makes possible. 2.5 casts a round.
                //
                // ⚠️ A COOLDOWN AND NOT CHARGES: it moves and protects her own body and puts
                // nothing on the floor. `HeroAbility.MaxCharges` carries the rule.
                // ⚠️ NEMU IS THE SUBJECT OF HER OWN KIT. Kuro still carries the projection and
                // the ultimate's landing point, but repeating his name in every title made the
                // hero read like an accessory to her pet. This tile teaches the actual verb:
                // Nemu phases, surges and denies a tag.
                : base("nemu_skill1", "PHANTOM VEIL",
                       "Slip beyond the taya's reach: surge forward, move faster, and become untouchable until you reclaim a tsinelas.",
                       52.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.NemuPhase,
                       summary: "Phase forward, outrun pursuit, and deny the next tag.",
                       castAction: "hero-nemu-ghoststep",
                       viewmodelAction: "ghost-step")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _longFadeSlow = ctx.HasVariant("nemu.1.fade");
                if (_longFadeSlow) ctx.Motor.EnterSpeedZone(0.65f);
                NetCue.Play("hero_nemu_grunt", ctx.Position);
                NetCue.Play("sfx_ghost_teleport", ctx.Position);

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
                    NetCue.Play("slipper_land", ctx.Position);
                    ComicPopup.Spawn(ctx.Position, "PHASE BROKEN!", UiTheme.HeroSpiritBright, 1.0f);
                    return;
                }

                // Speed boost during phantom phase
                ctx.Motor.ApplyImpulse(ctx.Forward * 3.0f * dt);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                if (_longFadeSlow)
                {
                    ctx.Motor.ExitSpeedZone(0.65f);
                    _longFadeSlow = false;
                }

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
                       "Possess your familiar and scout the street in spirit form. Press again to pull Nemu through the veil.",
                       0.0f, 6.0f, TumbangPreso.UI.AbilityGlyph.NemuAstralPet,
                       summary: "Possess your familiar; recast to teleport to it.",
                       castAction: "hero-nemu-project",
                       viewmodelAction: "project-spirit",
                       charges: 1)
            {
            }

            public override bool CanReactivate => true;

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
                NetCue.Play("sfx_ghost_teleport", ctx.Position);

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

            protected override void OnEnd(AbilityContext ctx)
            {
                var visual = ctx.Motor.GetComponent<Visual.CharacterVisual>();
                if (visual != null && visual.Companion != null && visual.Companion.IsPossessed)
                {
                    NetCue.Play("sfx_ghost_teleport", visual.transform.position);
                    visual.Companion.EndPossession(teleportNemu: true);
                }
                else if (_projectedGhost != null)
                {
                    Vector3 destination = _projectedGhost.transform.position;
                    NetCue.Play("sfx_ghost_teleport", destination);

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

        /// <summary>
        /// Ultimate: DEVOURING SEANCE. The familiar becomes the ritual's mouth.
        ///
        /// ⚠️⚠️ IT WAS A VORTEX THAT APPEARED OUT OF NOTHING AND 🧑 CALLED IT: *"her black hole
        /// dont make sense lowkey? maybe just make nemu's pet the black whole and make it look
        /// like it got bigger and is sucking everyone up, change the text that says its a
        /// blackhole"*. He is right, and the reason is worth stating because it is a design rule
        /// rather than a preference: **every other thing Nemu does is Kuro**, and her most
        /// expensive power was the one that ignored him. A hole in the road three metres in front
        /// of a girl with a spirit pet is a physics effect wearing her colour.
        ///
        /// ⚠️⚠️ SO THE ULTIMATE IS THE PET, AND THAT CHANGES WHERE IT LANDS. It opens **on Kuro**
        /// whenever Kuro is out, which makes Astral Projection a setup for it: send the pet
        /// somewhere, then unbind it there. With no pet out it falls back to a point in front of
        /// her, so the power is never unusable, and the fallback is deliberately the WORSE
        /// option: the reward for playing her kit as a kit is that she chooses the spot in
        /// advance. 🧑, in the same session: *"for nemu i want her skills to involve her pet more
        /// as well as her ult"*.
        ///
        /// ⚠️ THE WORD "VOID" IS GONE FROM EVERY STRING A PLAYER READS. The name, the tactical
        /// sentence and the select-screen summary all say what it now is. `Id` is unchanged at
        /// `nemu_ultimate`, because ids are keys: `HeroPresentationTests`, the HUD deck and the
        /// ability tray all index off it, and renaming a key to match a label is how a rename
        /// becomes six silent lookup failures.
        ///
        /// ⚠️ THE FOOTPRINT AND THE HAZARD ARE UNCHANGED AT 2.8 m. This is a presentation and
        /// fiction change, not a balance one: the drag, the slow and the radius are what
        /// `Hero_Strike_Balance.md` measured and what the bots path around. The note below on
        /// 2.8 versus 3.2 is still the reason for the number.
        /// </summary>
        private sealed class NightmareSeanceVoidAbility : HeroAbility
        {
            /// <summary>Where it opens when Kuro is not out. Her own reach, as before.</summary>
            private const float FallbackRange = 3.5f;

            public NightmareSeanceVoidAbility()
                : base("nemu_ultimate", "DEVOURING SEANCE",
                       "Tear open a hungry spirit maw at your familiar's position, dragging players and loose tsinelas into the ritual.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.NemuSeanceVoid,
                       summary: "Open a consuming seance where your familiar stands.",
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
                Windup = UltimateWindup;
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
                Vector3 at = onPet
                    ? companion.transform.position
                    : ctx.Position + ctx.Forward * FallbackRange;

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
                if (onPet) companion.Devour(7.0f);

                HeroHazards.SpawnKuroUnbound(at, 4.0f, 7.0f, ctx.Motor.PlayerSlot, onPet);
            }
        }
    }
}
