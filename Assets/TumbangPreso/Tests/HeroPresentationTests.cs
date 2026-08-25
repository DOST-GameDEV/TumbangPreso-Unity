using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The Hero Strike presentation layer: its palette, its copy, its telegraphs and its deck
    /// arithmetic.
    ///
    /// ⚠️⚠️ EVERY TEST IN HERE IS A REGRESSION TEST FOR SOMETHING THAT SHIPPED. The hero UI
    /// named seventeen colours inline and drifted into a hue family the rest of the game does
    /// not use; two hero accents sat on top of the two role colours the art direction reserves;
    /// the ground telegraph invented its own radii and nine of twelve disagreed with the ability
    /// they were drawn for; and four of fifteen ability descriptions were silently cut off
    /// mid-word on the screen a player uses to choose a hero. None of the four is visible in a
    /// code review of the file that contains it.
    ///
    /// `docs/Hero_Strike_UI.md` is the design these assert.
    /// </summary>
    public sealed class HeroPresentationTests
    {
        private static readonly string[] Heroes = { "cheska", "dante", "nemu", "sean", "zack" };

        // ------------------------------------------------------------------ the colour law

        /// <summary>
        /// ⚠️⚠️ `Art_Direction.md` § 1: ORANGE `#f87020` MEANS OFFENSE, BLUE `#0080e8` MEANS
        /// DEFENCE, AND NOTHING ELSE IN THE FRAME MAY SIT NEAR THOSE TWO HUES. They track the
        /// role, which rotates every round, so they are the only two colours a player has to
        /// READ rather than merely see.
        ///
        /// Two accents were breaking it. Dante was `#ff6d00`, hue 26, FOUR degrees off Offense:
        /// a saturated orange fill sitting beside other saturated orange fills that mean "this
        /// player is an attacker". Cheska was `#00e5ff`, hue 187, twenty off Defence.
        ///
        /// ⚠️ 25 DEGREES IS THE FLOOR AND IT IS DELIBERATELY NOT GENEROUS. Anything wider than
        /// about 30 makes the warm half of the wheel unusable for three heroes whose elements
        /// are fire, magma and lightning. The tightest the shipping set gets is Sean at 27.
        /// </summary>


        [Test]
        public void InspectDanteMesh()
        {
            var book = Resources.Load<RosterBook>("RosterBook");
            var dante = book.People.Find(p => p.Id == "dante");
            Assert.IsNotNull(dante, "Dante missing");
            var model = dante.Model;
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                Debug.Log($"[DANTE_RENDERER] name={r.name} type={r.GetType().Name} mats={r.sharedMaterials.Length}");
                if (r is SkinnedMeshRenderer smr)
                {
                    Debug.Log($"  Mesh submeshes={smr.sharedMesh.subMeshCount} verts={smr.sharedMesh.vertexCount}");
                }
            }
        }

        [Test]
        public void NoHeroAccentSitsOnARoleColour()
        {
            float offense = Hue(UiTheme.Offense);
            float defense = Hue(UiTheme.Defense);

            foreach (string hero in Heroes)
            {
                var accent = UiTheme.ColorForHero(hero);
                float hue = Hue(accent);

                Assert.GreaterOrEqual(HueDistance(hue, offense), 25.0f,
                    $"{hero}'s accent is {HueDistance(hue, offense):0.#} degrees from Offense " +
                    "orange, so it can be read as 'this player is an attacker'");

                Assert.GreaterOrEqual(HueDistance(hue, defense), 25.0f,
                    $"{hero}'s accent is {HueDistance(hue, defense):0.#} degrees from Defence " +
                    "blue, so it can be read as 'this player is the taya'");
            }
        }

        /// <summary>
        /// ⚠️ FIVE HEROES ON A 60 PX TILE RIM NEED REAL SEPARATION. Two accents 15 degrees apart
        /// are one colour at that size, and the rim is the entire "is this mine and is it up"
        /// signal. The tightest pair that ships is Dante's jade against Cheska's mint at 34.
        /// </summary>
        [Test]
        public void TheFiveHeroAccentsAreTellableApart()
        {
            for (int i = 0; i < Heroes.Length; i++)
            {
                for (int j = i + 1; j < Heroes.Length; j++)
                {
                    float a = Hue(UiTheme.ColorForHero(Heroes[i]));
                    float b = Hue(UiTheme.ColorForHero(Heroes[j]));

                    Assert.GreaterOrEqual(HueDistance(a, b), 30.0f,
                        $"{Heroes[i]} and {Heroes[j]} are only {HueDistance(a, b):0.#} degrees " +
                        "apart, which is one colour on a deck tile");
                }
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE HERO CHROME IS THE WOOD SET AT ALPHA, AND IT HAS TO STAY THAT WAY. The first
        /// pass invented a slate-blue glass palette, 🧑: *"i lowk dont get why we use light blue
        /// and shit in some parts of ui, it doesnt really look good with brown"*. Asserting the
        /// RGB rather than eyeballing it is what stops the next restyle drifting back off the
        /// brand one plate at a time.
        /// </summary>
        [Test]
        public void TheHeroChromeCarriesNoHueOfItsOwn()
        {
            // The plates are near-black and see-through, so the court reads through them. What
            // must never come back is a plate with a HUE, which is what the imported slate blue
            // was: `rgba(16, 22, 34)` is a desaturated navy, and three of them across the bottom
            // of the frame is a second palette competing with the game's own.
            AssertNeutral(UiTheme.HeroPlate, "HeroPlate");
            AssertNeutral(UiTheme.HeroPlateRaised, "HeroPlateRaised");
            AssertNeutral(UiTheme.HeroPlateSunk, "HeroPlateSunk");

            Assert.Less(UiTheme.HeroPlate.a, 0.85f,
                "the tray plate is nearly opaque, which makes it furniture rather than a window");
            Assert.Less(UiTheme.HeroPlateRaised.a, 0.75f,
                "an ability tile has to let the court through or it is a hole cut in the game");

            // Rim, glyph and number all come off Cream, so the deck cannot introduce a colour.
            AssertSameRgb(UiTheme.HeroRim, UiTheme.Cream, "HeroRim");
            AssertSameRgb(UiTheme.HeroRimLit, UiTheme.Cream, "HeroRimLit");
            AssertSameRgb(UiTheme.HeroGlyphOn, UiTheme.Cream, "HeroGlyphOn");
            AssertSameRgb(UiTheme.HeroGlyphOff, UiTheme.Cream, "HeroGlyphOff");
            AssertSameRgb(UiTheme.HeroNumber, UiTheme.Cream, "HeroNumber");

            Assert.Less(UiTheme.HeroGlyphOff.a, UiTheme.HeroGlyphOn.a,
                "an unavailable glyph must be the SAME colour at lower alpha, never a second " +
                "hue arriving through the back door on the state players look at most");

            Assert.Less(UiTheme.HeroRim.a, UiTheme.HeroRimLit.a,
                "a resting rim must be dimmer than a lit one, or 'ready' says nothing");
        }

        /// <summary>
        /// ⚠️ NEAR-BLACK OR NEAR-GREY, MEASURED AS SATURATION RATHER THAN EYEBALLED. A plate can
        /// be warm (the wood family) or cold (the imported slate) at the same lightness and the
        /// difference is invisible in a hex code and obvious on screen. Saturation is the number
        /// that separates them: the slate blue plate sat at 0.53.
        /// </summary>
        private static void AssertNeutral(Color c, string name)
        {
            Color.RGBToHSV(c, out _, out float saturation, out float value);

            Assert.Less(value, 0.20f, $"{name} is too light to sit under a glyph");
            Assert.Less(saturation, 0.35f,
                $"{name} has a hue of its own, which is how the hero UI drifted off the brand " +
                "the first time");
        }

        // ------------------------------------------------------------------ the copy

        /// <summary>
        /// ⚠️⚠️ FOUR OF FIFTEEN DESCRIPTIONS USED TO STOP MID-WORD ON CHARACTER SELECT. That card
        /// draws into a 46 px box at 14 pt, which is three lines, with
        /// `VerticalWrapMode.Truncate`, and truncation is silent. `Summary` exists so the short
        /// box gets a line written to fit it and the tray keeps the full sentence.
        ///
        /// ⚠️ 62 CHARACTERS IS MEASURED, NOT CHOSEN. The details card is about 360 px wide at
        /// 14 pt, which is roughly 45 characters a line, and the box holds three.
        /// </summary>
        [Test]
        public void EverySummaryFitsTheCardItIsDrawnIn()
        {
            foreach (string hero in Heroes)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);

                foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                {
                    Assert.IsNotNull(ability, $"{hero} is missing an ability");

                    Assert.IsNotEmpty(ability.Summary,
                        $"{hero}: {ability.Name} has no summary, so character select draws a " +
                        "blank line where the tactical readout goes");

                    Assert.LessOrEqual(ability.Summary.Length, 62,
                        $"{hero}: {ability.Name}'s summary is {ability.Summary.Length} " +
                        "characters and the card holds about 62 before it truncates silently");

                    Assert.IsNotEmpty(ability.Description,
                        $"{hero}: {ability.Name} has no description for the inspect tray");
                }
            }
        }

        /// <summary>
        /// ⚠️ THE NAME HAS TO FIT THE HEADER ROW BESIDE THE COOLDOWN CHIP. "GLACIAL BLIZZARD
        /// NOVA" and "THUNDERSTRIKE OVERDRIVE" both pushed the `[AREA BURST] · 9s CD` meta off
        /// the end of the row, which is the half of that line carrying the numbers.
        /// </summary>
        [Test]
        public void EveryAbilityNameFitsItsHeaderRow()
        {
            foreach (string hero in Heroes)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);

                foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                {
                    Assert.IsNotEmpty(ability.Name, $"{hero}: an ability has no name");
                    Assert.LessOrEqual(ability.Name.Length, 18,
                        $"{hero}: '{ability.Name}' is {ability.Name.Length} characters and " +
                        "pushes the cooldown meta off the end of the header row");
                }
            }
        }

        // ------------------------------------------------------------------ the telegraphs

        /// <summary>
        /// The radius and forward offset every ground-placed power draws, checked against what
        /// its `OnActivate` actually spawns.
        ///
        /// ⚠️⚠️ THE HUD USED TO INVENT THESE AND NINE OF THE TWELVE WERE WRONG.
        /// `HeroAbilitySystem.UpdateReticle` drew 7.5 m for ANY ultimate, 5.0 m for ANY first
        /// skill and 3.5 m for ANY second, and pushed the ring forward only when the kit happened
        /// to be Cheska's. Dante's 2.4 m stomp drew a 5.0 m ring. Nemu's 3.2 m void drew 7.5 m,
        /// centred on Nemu, when it lands 3.5 m in front of them.
        ///
        /// ⚠️ THIS TABLE IS THE SECOND COPY OF EACH NUMBER AND THAT IS THE POINT, exactly as
        /// `Design.md` is the second copy of every balance number: a value written twice in two
        /// places that must agree is a value a test can catch drifting. Change a spawn radius in
        /// a kit and this row goes red.
        /// </summary>
        [Test]
        public void TelegraphsMatchWhatTheAbilityPlaces()
        {
            AssertTelegraph("cheska", 1, 2.3f, 2.8f);   // SpawnIceSheet(pos + fwd*2.8, 2.3)
            AssertTelegraph("cheska", 2, 1.6f, 2.2f);   // barricade HazardVolume 1.6 at fwd*2.2
            AssertTelegraph("cheska", 3, 4.6f, 0.0f);   // nova freeze check <= 4.6 at self

            AssertTelegraph("dante", 1, 2.2f, 0.0f);    // CreateExplosion(pos, 2.2)
            AssertTelegraph("dante", 2, 0.0f, 0.0f);    // self-buff, nothing on the ground
            AssertTelegraph("dante", 3, 4.5f, 2.2f);    // CreateExplosion(pos + fwd*2.2, 4.5)

            AssertTelegraph("nemu", 1, 0.0f, 0.0f);     // mobility
            AssertTelegraph("nemu", 2, 0.0f, 0.0f);     // projectile decoy
            AssertTelegraph("nemu", 3, 2.8f, 3.5f);     // SpawnSeanceVoid(pos + fwd*3.5, 2.8)

            AssertTelegraph("sean", 1, 0.0f, 0.0f);     // dash
            AssertTelegraph("sean", 2, 0.0f, 0.0f);     // throw empower
            AssertTelegraph("sean", 3, 4.8f, 0.0f);     // CreateExplosion(pos, 4.8)

            AssertTelegraph("zack", 1, 0.0f, 0.0f);     // dash
            AssertTelegraph("zack", 2, 0.0f, 0.0f);     // throw empower
            AssertTelegraph("zack", 3, 4.5f, 0.0f);     // CreateThunderstrike(pos, 4.5)
        }

        /// <summary>
        /// ⚠️⚠️ THE BOTS ARE THE CANARY FOR WHETHER A HUMAN CAN READ THE FLOOR, AND THIS MAKES
        /// THE CANARY AUTOMATIC. `docs/VISION.md` § 2 states it directly, and
        /// `AiTuning.HazardAvoidMaxRadius` is the cap that exists because of it: there is no way
        /// around a disc that covers half the arena, so a bot that tries walks the perimeter
        /// until the round ends. Measured, when avoidance was first switched on with no cap:
        /// `BotBehaviourProbe`'s Hero Strike run fell from 78 to 97 throws in four rounds to
        /// **17**, while Classic, which has no hazards, did not move.
        ///
        /// As of the 2026-08-25 footprint pass every registered hazard is under the cap, so the
        /// cap binds nothing and avoidance applies to all of them. **That is a property worth
        /// keeping and it is not visible in the file that would break it**: a new ability
        /// registering a 4 m zone compiles, runs, looks fine in the editor, and strands every
        /// bot on the perimeter of every map it is cast on.
        ///
        /// ⚠️ IT ASSERTS THE TELEGRAPH RADIUS RATHER THAN THE `HazardVolume`, because the volume
        /// only exists at runtime and `TelegraphsMatchWhatTheAbilityPlaces` above already pins
        /// the telegraph to what the ability actually spawns. The two tests together cover the
        /// spawn without needing a live scene.
        /// </summary>
        [Test]
        public void EveryRegisteredHazardStaysUnderTheBotAvoidanceCap()
        {
            foreach (string hero in Heroes)
            {
                var kit = Abilities.HeroAbilitySystem.CreateKitFor(hero);

                foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                {
                    if (ability == null || !ability.HasTelegraph) continue;

                    // ⚠️ ULTIMATES ARE EXEMPT AND THAT IS `docs/VISION.md` § 2 RULE 2: *"An
                    // ultimate may be big. One at a time."* Supernova, Thunderstrike, Glacial
                    // Nova and Titan Fissure are all over the cap by design and none of them
                    // registers a persistent `HazardVolume`: they are instantaneous blasts, so
                    // there is no ground for a bot to path around after the frame they fire.
                    // Seance Void is the one ultimate that DOES persist, and it is checked.
                    if (ability == kit.Ultimate && ability.Id != "nemu_ultimate") continue;

                    Assert.LessOrEqual(ability.TelegraphRadius, AiTuning.HazardAvoidMaxRadius,
                        $"{hero}/{ability.Id} leaves a {ability.TelegraphRadius} m hazard, over "
                        + $"the {AiTuning.HazardAvoidMaxRadius} m cap. The bots will walk "
                        + "straight through it: see AiTuning.HazardAvoidMaxRadius for what that "
                        + "measured last time, and docs/Hero_Strike_Balance.md § 3.3.");
                }
            }
        }

        /// <summary>
        /// ⚠️ A RANGE WITHOUT A RADIUS IS A RING NOBODY CAN SEE. `HasTelegraph` is keyed on the
        /// radius alone, so an ability given an offset and no size would silently draw nothing
        /// while looking correctly filled in from the constructor call.
        /// </summary>
        [Test]
        public void NoAbilityCarriesAnOffsetWithoutASize()
        {
            foreach (string hero in Heroes)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);

                foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                {
                    if (ability.TelegraphRange <= 0.0f) continue;

                    Assert.IsTrue(ability.HasTelegraph,
                        $"{hero}: {ability.Name} is placed {ability.TelegraphRange} m forward " +
                        "but has no radius, so its telegraph draws nothing at all");
                }
            }
        }

        // ------------------------------------------------------------------ the deck

        /// <summary>
        /// ⚠️⚠️ A `HorizontalLayoutGroup` OVERFLOWS ITS RECT SILENTLY. Three cards that no longer
        /// fit are laid out past the edge of the plate, and the overflow lands under the
        /// first-person hands where it is least visible and most annoying. The identity below is
        /// the whole reason those numbers are named constants rather than literals.
        /// </summary>
        [Test]
        public void TheHeroDeckWidthMatchesItsChildren()
        {
            float children = Hud.DeckPadding * 2.0f
                             + Hud.DeckSpacing * (Hud.DeckCardCount - 1)
                             + Hud.SkillCardWidth * 2.0f
                             + Hud.UltimateCardWidth;

            Assert.AreEqual(Hud.DeckWidth, children, 0.001f,
                $"the deck plate is {Hud.DeckWidth} wide but its children need {children}; " +
                "the cards will be laid out past the edge of it");
        }

        /// <summary>
        /// ⚠️ THE DECK HAS TO CLEAR THE VIEWMODEL. It is a badge, not a bar: it was 592 x 122 at
        /// `y = 24`, which is over half a 1080p screen's width of chrome sitting on the hands.
        /// </summary>
        [Test]
        public void TheHeroDeckStaysASlimBadge()
        {
            Assert.LessOrEqual(Hud.DeckWidth, 280.0f, "the deck has grown back into a bar");
            Assert.LessOrEqual(Hud.DeckHeight, 80.0f, "the deck is tall enough to cover the hands");
        }

        [Test]
        public void CooldownReadoutIsPreciseOnlyWhenItMatters()
        {
            Assert.AreEqual("9", AbilityDeckHud.CooldownLabel(8.01f));
            Assert.AreEqual("3", AbilityDeckHud.CooldownLabel(3.0f));
            Assert.AreEqual("2.9", AbilityDeckHud.CooldownLabel(2.94f));
            Assert.AreEqual("0.1", AbilityDeckHud.CooldownLabel(0.06f));
            Assert.AreEqual(string.Empty, AbilityDeckHud.CooldownLabel(0.0f));
            Assert.AreEqual(0.5f, AbilityDeckHud.CooldownSweep(4.0f, 8.0f), 0.001f);
            Assert.AreEqual(0.0f, AbilityDeckHud.CooldownSweep(2.0f, 0.0f), 0.001f);
        }

        // ------------------------------------------------------------------ cast outcomes

        /// <summary>
        /// ⚠️⚠️ "ON COOLDOWN" AND "CANNOT ACT RIGHT NOW" ARE DIFFERENT ANSWERS AND THE WHOLE
        /// ANTI-CLUNK FIX RESTS ON THEM BEING TOLD APART. One is a refusal the UI answers with a
        /// red tick and clears; the other is buffered and retried for 0.30 s. They used to come
        /// back as the same `false`, so a skill pressed during a five second stun was eaten with
        /// no feedback anywhere, which reads to a player as the game dropping their input.
        /// </summary>
        [Test]
        public void ACoolingAbilityAnswersDifferentlyFromAnEmptyMeter()
        {
            var kit = new ProbeKit();

            Assert.AreEqual(HeroKit.CastOutcome.NoCharge, kit.CastUltimate(null),
                "an empty meter has to say so, not fall through to a generic refusal");

            kit.AddUltimateCharge(HeroKit.UltimateMax);
            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastUltimate(null),
                "a full meter was refused");

            Assert.AreEqual(HeroKit.CastOutcome.NoCharge, kit.CastUltimate(null),
                "the meter was spent, so the next press is a charge refusal");
        }

        /// <summary>
        /// ⚠️ A SKILL ON COOLDOWN SAYS `Cooling`, AND THAT IS WHAT THE RED TICK HANGS OFF. It is
        /// also the outcome that must NOT be buffered: holding it would fire the skill the
        /// instant it came back, seconds after the press, which is the same complaint wearing a
        /// helpful face.
        /// </summary>
        [Test]
        public void ASkillOnCooldownSaysSo()
        {
            var kit = new ProbeKit();

            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(null),
                "a ready skill was refused");

            Assert.AreEqual(HeroKit.CastOutcome.Cooling, kit.CastSkill1(null),
                "a skill still on cooldown has to say cooling, not a generic refusal");

            Assert.AreEqual(HeroKit.CastOutcome.Missing, kit.CastSkill2(null),
                "a hero without a second power must draw nothing rather than a refusal");
        }

        /// <summary>
        /// A stand-in kit, because the real ones need a live `CharacterMotor` to cast and this
        /// file is EditMode. Same shape as the probe in `InputMapAndAbilityTests`.
        /// </summary>
        private sealed class ProbeAbility : HeroAbility
        {
            public ProbeAbility(float cooldown)
                : base("probe", "PROBE", "A stand-in.", cooldown, 0.0f, AbilityGlyph.Burst)
            {
            }

            // ⚠️ IT SKIPS THE MOTOR CHECK, WHICH IS THE ONLY REASON THIS CAN RUN IN EditMode.
            // The base refuses a null context outright, and a null context is exactly what a
            // test without a scene has.
            public override bool CanActivate(AbilityContext ctx) => IsReady;
        }

        private sealed class ProbeKit : HeroKit
        {
            public ProbeKit() : base("probe", "PROBE")
            {
                Skill1 = new ProbeAbility(5.0f);
                Skill2 = null;
                Ultimate = new ProbeAbility(0.0f);
            }
        }

        /// <summary>
        /// ⚠️ THE BUFFER WINDOW IS BOUNDED AT BOTH ENDS. Long enough to cover a stagger, which
        /// the shove and the hazard pulses apply for 0.20 to 0.35 s; short enough that a press
        /// made a second ago cannot come out on its own later, at a moment the player did not
        /// choose. That second failure is the one buffering usually introduces.
        /// </summary>
        [Test]
        public void TheInputBufferIsShortEnoughToBeAnAid()
        {
            Assert.GreaterOrEqual(HeroAbilitySystem.InputBufferWindow, 0.20f,
                "shorter than a stagger, so it cannot do the job it exists for");
            Assert.LessOrEqual(HeroAbilitySystem.InputBufferWindow, 0.40f,
                "long enough that a stale press fires at a moment nobody asked for");
        }

        /// <summary>
        /// ⚠️ EVERY HERO ABILITY HAS A BESPOKE 3RD-PERSON BODY ACTION AND 1ST-PERSON VIEWMODEL ACTION.
        /// Generic fallback clips ("dash"/"shove"/"jump") are forbidden on hero abilities.
        /// </summary>
        [Test]
        public void EveryHeroAbilityHasBespokeCastAndViewModelActions()
        {
            var vm = new GameObject("TestVM").AddComponent<CameraSystem.ViewmodelArms>();

            foreach (string hero in Heroes)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);

                foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                {
                    Assert.IsNotNull(ability, $"{hero} is missing an ability");
                    Assert.IsFalse(string.IsNullOrEmpty(ability.CastAction),
                        $"{hero}: {ability.Name} is missing a CastAction");
                    Assert.IsFalse(string.IsNullOrEmpty(ability.ViewmodelAction),
                        $"{hero}: {ability.Name} is missing a ViewmodelAction");

                    Assert.IsFalse(ability.CastAction == "dash" || ability.CastAction == "shove" || ability.CastAction == "jump",
                        $"{hero}: {ability.Name} still uses generic fallback CastAction '{ability.CastAction}'");

                    Assert.IsTrue(vm.PlayAction(ability.ViewmodelAction),
                        $"{hero}: {ability.Name} ViewmodelAction '{ability.ViewmodelAction}' is not supported by ViewmodelArms");
                }
            }

            Object.DestroyImmediate(vm.gameObject);
        }

        /// <summary>
        /// ⚠️ VIEWMODEL ARMS MUST STYLE BESPOKE SKIN TONES, SLEEVES, MARKINGS, AND ACCESSORIES
        /// FOR EVERY HERO AND EVERY CLASSIC CHARACTER MATCHING THEIR TPP MODEL.
        /// </summary>
        [Test]
        public void ViewmodelArms_StylesUniqueSkinToneAndAccessories_ForEveryCharacter()
        {
            var vm = new GameObject("TestVM_CharacterArms").AddComponent<CameraSystem.ViewmodelArms>();

            string[] allCharacters =
            {
                // Heroes
                "sean", "zack", "dante", "cheska", "nemu",
                // Classic Roster
                "bayan", "maring", "totoy", "inday", "kuya_boy", "ate_girlie",
                "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena",
                "classic"
            };

            foreach (string charId in allCharacters)
            {
                vm.SetCharacter(charId);

                string norm = CameraSystem.ViewmodelArms.NormalizeCharacterId(charId);
                Assert.AreEqual(norm, vm.CurrentHeroId, $"ViewmodelArms failed to normalize character {charId}");
                Assert.AreEqual(CameraSystem.ViewmodelArms.SkinColorForCharacter(charId), vm.CurrentSkinColor,
                    $"ViewmodelArms applied wrong skin tone for {charId}");

                var renderers = vm.GetComponentsInChildren<MeshRenderer>(true);
                int accessoryCount = 0;

                foreach (var r in renderers)
                {
                    Assert.IsNotNull(r.sharedMaterial, $"{charId}: Renderer on {r.gameObject.name} has null material");
                    Assert.IsNotNull(r.sharedMaterial.shader, $"{charId}: Renderer on {r.gameObject.name} has null shader");
                    if (r.gameObject.name.StartsWith("~HeroAccessory_")) accessoryCount++;
                }

                Assert.Greater(accessoryCount, 0, $"{charId} viewmodel arms are missing bespoke sleeves/markings/accessories");
            }

            Object.DestroyImmediate(vm.gameObject);
        }

        /// <summary>
        /// ⚠️ THE HELD SLIPPER MUST REMAIN PARENTED UNDER RightPivot/Arm ACROSS CHARACTER SWAPS AND PLAYING ACTIONS.
        /// </summary>
        [Test]
        public void ViewmodelArms_PreservesHeldSlipperAndActions_AcrossCharacterSwaps()
        {
            var vm = new GameObject("TestVM_Slipper").AddComponent<CameraSystem.ViewmodelArms>();
            vm.EnsureBuilt();

            var rightArm = vm.transform.Find("RightPivot/Arm");
            Assert.IsNotNull(rightArm, "RightPivot/Arm transform missing from ViewmodelArms");

            var heldSlipper = rightArm.Find("HeldSlipper");
            Assert.IsNotNull(heldSlipper, "HeldSlipper missing or not parented to RightPivot/Arm");

            string[] testRoster =
            {
                "sean", "zack", "dante", "cheska", "nemu",
                "bayan", "maring", "totoy", "inday", "kuya_boy", "ate_girlie",
                "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena"
            };

            foreach (string charId in testRoster)
            {
                vm.SetCharacter(charId);

                // HeldSlipper must survive accessory clearing
                heldSlipper = rightArm.Find("HeldSlipper");
                Assert.IsNotNull(heldSlipper, $"{charId}: HeldSlipper was destroyed during SetCharacter");

                vm.SetHolding(true);
                Assert.IsTrue(heldSlipper.gameObject.activeSelf, $"{charId}: SetHolding(true) failed");

                vm.SetHolding(false);
                Assert.IsFalse(heldSlipper.gameObject.activeSelf, $"{charId}: SetHolding(false) failed");

                // If hero, check ability actions; otherwise check throw / grab
                if (System.Array.IndexOf(Heroes, charId) >= 0)
                {
                    var kit = HeroAbilitySystem.CreateKitFor(charId);
                    foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                    {
                        Assert.IsTrue(vm.PlayAction(ability.ViewmodelAction),
                            $"{charId}: PlayAction failed for {ability.ViewmodelAction}");
                    }
                }
                else
                {
                    Assert.IsTrue(vm.PlayAction("throw"));
                    Assert.IsTrue(vm.PlayAction("slam"));
                }
            }

            Object.DestroyImmediate(vm.gameObject);
        }

        // ------------------------------------------------------------------ helpers

        private static void AssertTelegraph(string hero, int slot, float radius, float range)
        {
            var kit = HeroAbilitySystem.CreateKitFor(hero);
            var ability = slot == 1 ? kit.Skill1 : slot == 2 ? kit.Skill2 : kit.Ultimate;

            Assert.IsNotNull(ability, $"{hero} slot {slot} is missing");

            Assert.AreEqual(radius, ability.TelegraphRadius, 0.001f,
                $"{hero}: {ability.Name} draws a {ability.TelegraphRadius} m ring but places " +
                $"a {radius} m effect");

            Assert.AreEqual(range, ability.TelegraphRange, 0.001f,
                $"{hero}: {ability.Name} draws its ring {ability.TelegraphRange} m ahead but " +
                $"places the effect {range} m ahead");
        }

        [Test]
        public void EveryAbilityAcrossAllHeroesHasAUniqueBespokeGlyph()
        {
            var seenGlyphs = new HashSet<AbilityGlyph>();
            int totalAbilities = 0;

            foreach (string hero in Heroes)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);
                var abilities = new[] { kit.Skill1, kit.Skill2, kit.Ultimate };

                foreach (var ability in abilities)
                {
                    totalAbilities++;
                    Assert.IsTrue(System.Enum.IsDefined(typeof(AbilityGlyph), ability.Glyph),
                        $"{hero} {ability.Name} has an undefined glyph value: {ability.Glyph}");

                    Assert.IsFalse(seenGlyphs.Contains(ability.Glyph),
                        $"Duplicate glyph detected! {hero}'s ability '{ability.Name}' reuses glyph {ability.Glyph}, which is already used by another ability.");

                    seenGlyphs.Add(ability.Glyph);

                    // Ensure Sprite generation works and has a non-empty label
                    var sprite = AbilityIcons.For(ability.Glyph);
                    Assert.IsNotNull(sprite, $"AbilityIcons.For returned null for glyph {ability.Glyph}");

                    string label = AbilityIcons.LabelFor(ability.Glyph);
                    Assert.IsNotEmpty(label, $"AbilityIcons.LabelFor returned empty for glyph {ability.Glyph}");
                }
            }

            Assert.AreEqual(15, totalAbilities, "Expected 15 total abilities across 5 heroes");
            Assert.AreEqual(15, seenGlyphs.Count, "Expected 15 unique glyphs across 15 abilities");
        }

        private static void AssertSameRgb(Color actual, Color expected, string name)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f, $"{name} red channel left the wood set");
            Assert.AreEqual(expected.g, actual.g, 0.001f, $"{name} green channel left the wood set");
            Assert.AreEqual(expected.b, actual.b, 0.001f, $"{name} blue channel left the wood set");
        }

        /// <summary>Hue in degrees, 0 to 360. Unity's own conversion, so it cannot disagree.</summary>
        private static float Hue(Color c)
        {
            Color.RGBToHSV(c, out float h, out _, out _);
            return h * 360.0f;
        }

        /// <summary>The short way round a 360 degree wheel.</summary>
        /// <summary>
        /// ⚠️ NEMU VIEWMODEL ARMS MUST INSTANTIATE BESPOKE DRAPED HOODIE SLEEVES, INNER SHADOW CAVITY,
        /// GLOWING CUFF RIM, DELICATE SPIRIT HANDS, AND DYNAMIC CLOTH PHYSICS SOLVER.
        /// </summary>
        [Test]
        public void Nemu_ViewmodelArms_CreatesBespokeDrapedHoodieSleevesAndClothPhysics()
        {
            var vm = new GameObject("TestVM_NemuCloth").AddComponent<CameraSystem.ViewmodelArms>();
            vm.EnsureBuilt();
            vm.SetCharacter("nemu");

            var rightArm = vm.transform.Find("RightPivot/Arm");
            var leftArm = vm.transform.Find("LeftPivot/Arm");
            Assert.IsNotNull(rightArm, "RightPivot/Arm missing");
            Assert.IsNotNull(leftArm, "LeftPivot/Arm missing");

            // Verify sleeve and cloth physics on right arm
            var rightSleeve = rightArm.Find("~HeroAccessory_HoodieSleeve");
            Assert.IsNotNull(rightSleeve, "Right hoodie sleeve missing");
            var rightCloth = rightSleeve.GetComponent<CameraSystem.ViewmodelClothPhysics>();
            Assert.IsNotNull(rightCloth, "Right hoodie sleeve is missing ViewmodelClothPhysics component");
            Assert.IsTrue(rightCloth.HasDeformableMesh, "Right hoodie cloth physics failed to bind deformable mesh");

            // Verify inner cavity and cuff rim
            var rightInner = rightArm.Find("~HeroAccessory_HoodieInnerLining");
            Assert.IsNotNull(rightInner, "Right hoodie inner shadow lining cavity missing");
            var rightCuff = rightArm.Find("~HeroAccessory_HoodieCuffRim");
            Assert.IsNotNull(rightCuff, "Right hoodie cuff rim band missing");

            // Verify spirit hand
            var rightHand = rightArm.Find("~HeroAccessory_SpiritHand");
            Assert.IsNotNull(rightHand, "Right spirit hand missing");

            // Verify cloth physics simulation step and impulse recovery
            vm.StepVisuals(0.016f);
            Assert.IsFalse(float.IsNaN(rightCloth.ClothOffset.x), "Cloth offset produced NaN");
            Assert.IsFalse(float.IsNaN(rightCloth.ClothAngle.x), "Cloth angle produced NaN");

            // Test recoil impulse on action
            Assert.IsTrue(vm.PlayAction("throw"));
            vm.StepVisuals(0.016f);
            Assert.AreNotEqual(Vector3.zero, rightCloth.ClothOffset + rightCloth.ClothAngle, "Throw action did not perturb cloth physics");

            // Step forward in time and verify cloth recovers stably
            for (int i = 0; i < 60; i++)
            {
                vm.StepVisuals(0.016f);
            }
            Assert.Less(rightCloth.ClothOffset.magnitude, 0.05f, "Cloth offset failed to damp back to rest");

            Object.DestroyImmediate(vm.gameObject);
        }

        private static float HueDistance(float a, float b)
        {
            float d = Mathf.Abs(a - b) % 360.0f;
            return d > 180.0f ? 360.0f - d : d;
        }
    }
}

