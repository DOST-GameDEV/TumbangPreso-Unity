using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The input map's one rule, and the ability layer's round boundary.
    ///
    /// ⚠️⚠️ BOTH OF THESE ARE REGRESSION TESTS FOR FAULTS THAT SHIPPED, NOT SPECULATION. The
    /// bindings carried four live collisions for weeks because nothing checked the asset, and
    /// `HeroAbilitySystem.ResetKit` had zero call sites for its whole life, so ultimate charge
    /// banked in the warm-up survived into round two. Neither is visible in a code review of the
    /// file that contains it: one lives in a JSON asset and the other is an absence.
    /// </summary>
    public sealed class InputMapAndAbilityTests
    {
        private static InputActionAsset LoadActions()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            Assert.IsNotNull(asset, "no InputActionAsset at Resources/TumbangPreso");
            return asset;
        }

        /// <summary>
        /// ⚠️⚠️ ONE CONTROL, ONE ACTION. `Rebinding.TryRebind` refuses a key another action
        /// already holds and names the action holding it, so the shipped defaults were breaking
        /// the rule the settings panel enforces on the player. Left click carried Throw AND
        /// Grab, E carried Grab, Lunge AND Skill 1, Q carried Throw AND Skill 2. Whichever
        /// consumer ran first won the press, which is why throw did not feel like it was on left
        /// click even though it was bound there.
        /// </summary>
        [Test]
        public void NoTwoActionsShareAControl()
        {
            var asset = LoadActions();

            // ⚠️ THE PLAYER'S OWN OVERRIDES ARE NOT LOADED HERE, DELIBERATELY. This asserts the
            // DEFAULTS are clean; a player who has rebound something is protected by TryRebind.
            asset.RemoveAllBindingOverrides();

            var clashes = Rebinding.FindDuplicateBindings(asset);

            Assert.IsEmpty(clashes,
                "two actions share a control in TumbangPreso.inputactions: " +
                string.Join(" | ", clashes));
        }

        /// <summary>Every rebindable action has to actually exist, or the panel draws a dead row.</summary>
        [Test]
        public void EveryRebindableActionExists()
        {
            var asset = LoadActions();
            var missing = new List<string>();

            foreach (string action in Rebinding.RebindableActions)
            {
                string shown = Rebinding.DisplayNameFor(asset, action);
                if (shown == "-") missing.Add(action);
            }

            Assert.IsEmpty(missing,
                "listed as rebindable but not present in the asset: " + string.Join(", ", missing));
        }

        /// <summary>And every action in the asset has a row, or it is unrebindable in practice.</summary>
        [Test]
        public void EveryActionHasARebindRow()
        {
            var asset = LoadActions();
            var map = asset.FindActionMap("Player", true);

            var listed = new HashSet<string>(Rebinding.RebindableActions);
            var orphans = new List<string>();

            foreach (var action in map.actions)
            {
                if (action.name == "Move" && (listed.Contains("MoveForward") || listed.Contains("Move"))) continue;
                if (!listed.Contains(action.name)) orphans.Add(action.name);
            }

            Assert.IsEmpty(orphans,
                "in the asset but missing from Rebinding.RebindableActions, so they have no " +
                "row in the settings panel: " + string.Join(", ", orphans));
        }

        /// <summary>
        /// ⚠️ THE ONE BINDING HE ASKED FOR BY NAME. 🧑 2026-08-23: *"i want throw to map to left
        /// click as well, why is throw in soemwher eelse"*. It always was on left click; Grab
        /// was on it too, which is why it did not behave like it. Asserted so a future pass
        /// cannot quietly move it again.
        /// </summary>
        [Test]
        public void ThrowIsOnLeftClickAndNothingElseIs()
        {
            var asset = LoadActions();
            asset.RemoveAllBindingOverrides();

            var map = asset.FindActionMap("Player", true);
            var throwAction = map.FindAction("SpecialAbility", true);

            bool onLeftClick = false;
            foreach (var b in throwAction.bindings)
                if (b.effectivePath == "<Mouse>/leftButton") onLeftClick = true;

            Assert.IsTrue(onLeftClick, "SpecialAbility (throw / punch) is not bound to left click");

            foreach (var action in map.actions)
            {
                if (action.name == "SpecialAbility") continue;

                foreach (var b in action.bindings)
                    Assert.AreNotEqual("<Mouse>/leftButton", b.effectivePath,
                        $"{action.name} also holds left click, so it competes with the throw");
            }
        }

        /// <summary>The three power prompts form the adjacent Q, E, F combat cluster.</summary>
        [Test]
        public void HeroPowerDefaultsMatchTheHudCluster()
        {
            var asset = LoadActions();
            asset.RemoveAllBindingOverrides();
            var map = asset.FindActionMap("Player", true);

            Assert.AreEqual("<Keyboard>/q", map.FindAction("Skill1", true).bindings[0].effectivePath);
            Assert.AreEqual("<Keyboard>/e", map.FindAction("Skill2", true).bindings[0].effectivePath);
            Assert.AreEqual("<Keyboard>/f", map.FindAction("Ultimate", true).bindings[0].effectivePath);
            Assert.AreEqual("<Keyboard>/x", map.FindAction("Grab", true).bindings[0].effectivePath,
                "contextual pickup must not compete with the E power key");
        }

        /// <summary>
        /// ⚠⚠ EVERY REBINDABLE ACTION BELONGS TO EXACTLY ONE GROUP. The settings panel now
        /// draws its rows by walking `Rebinding.Groups`, not `RebindableActions`, so an action
        /// missing from every group vanishes from the screen with no error at all. That is the
        /// same silent failure mode the `Rebinding` class note warns about for stale rows.
        /// </summary>
        [Test]
        public void SettingsGroupsCoverEveryActionExactlyOnce()
        {
            var counted = new Dictionary<string, int>();

            foreach (var group in Rebinding.Groups)
            {
                Assert.IsNotEmpty(group.Title, "a control group has no heading");

                foreach (string action in group.Actions)
                {
                    counted.TryGetValue(action, out int n);
                    counted[action] = n + 1;
                }
            }

            var missing = new List<string>();
            var twice = new List<string>();

            foreach (string action in Rebinding.RebindableActions)
            {
                counted.TryGetValue(action, out int n);
                if (n == 0) missing.Add(action);
                else if (n > 1) twice.Add(action);
            }

            Assert.IsEmpty(missing,
                "rebindable but in no settings group, so it has no row on screen: " +
                string.Join(", ", missing));
            Assert.IsEmpty(twice, "listed in more than one group: " + string.Join(", ", twice));

            var strays = new List<string>();
            var listed = new HashSet<string>(Rebinding.RebindableActions);
            foreach (var pair in counted)
                if (!listed.Contains(pair.Key)) strays.Add(pair.Key);

            Assert.IsEmpty(strays,
                "in a settings group but not rebindable, so the row would be dead: " +
                string.Join(", ", strays));
        }

        // ------------------------------------------------------------------ abilities

        /// <summary>
        /// ⚠️⚠️ THE BANK SURVIVES A ROUND BOUNDARY, ON INSTRUCTION. 🧑 2026-08-23: *"its okay
        /// for ult progress to persist after round and into next rounds"*. An earlier pass
        /// zeroed it here and that was wrong. What must NOT happen is charge accruing while the
        /// round clock is stopped, which is `PracticeMode` below.
        /// </summary>
        [Test]
        public void ResetForRoundKeepsUltimateCharge()
        {
            var kit = HeroAbilitySystem.CreateKitFor("cheska");

            kit.AddUltimateCharge(HeroKit.UltimateMax * 0.6f);
            float banked = kit.UltimateCharge;

            kit.ResetForRound(null);

            Assert.AreEqual(banked, kit.UltimateCharge, 0.0001f,
                "the round boundary emptied the ultimate bank; it is meant to carry over");
        }

        /// <summary>
        /// ⚠️⚠️ TWO KINDS OF "CHARGE" MEET AT ONE CALL AND THEY GO OPPOSITE WAYS. Since the
        /// 2026-08-25 economy rework the word is overloaded, and the two behaviours are one line
        /// apart inside `HeroKit.ResetForRound`, so a change aimed at either can silently take
        /// the other with it. Nothing asserted them together until now.
        ///
        ///  * The **ultimate meter** is a RESOURCE earned across the whole match and it
        ///    PERSISTS. 🧑 has asked for this twice: *"its okay for ult progress to persist
        ///    after round and into next rounds"* (2026-08-23), and again on 2026-08-25 after the
        ///    rework: *"i want ult charges to stay in between rounds ... Only ult tho"*.
        ///  * A skill's **charges** are a PER-ROUND allowance and they REFILL, because his rule
        ///    for those was *"charges ... that reset each round"*.
        ///
        /// ⚠️ THE "Only ult tho" IS THE HALF WORTH TESTING. Persisting the meter is easy to get
        /// right by accident; persisting it *without* also carrying a spent barricade into the
        /// next round is the part that needs pinning, and a test that only checked the meter
        /// would have passed on a build where skills never refilled either.
        /// </summary>
        [Test]
        public void UltimateChargePersistsButSkillChargesRefill()
        {
            var kit = HeroAbilitySystem.CreateKitFor("cheska");

            var go = new GameObject("RoundBoundaryProbeMotor");

            // Cheska's barricade spawns ice chips that self-destruct on a timer, and `Destroy`
            // outside play mode logs an error the framework promotes to a failure. Same
            // suppression and same reason as `ChargesComeBackOnPlayAndOnlyForTheSkillsThatShould`.
            bool ignoring = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                var ctx = new AbilityContext(go.AddComponent<CharacterMotor>(), null, null);

                // Bank most of an ultimate and spend the round's only barricade.
                kit.AddUltimateCharge(kit.UltimateCost * 0.75f);
                float banked = kit.UltimateCharge;

                kit.Skill2.Activate(ctx);
                Assert.AreEqual(0, kit.Skill2.ChargesRemaining, "the barricade did not spend");

                kit.ResetForRound(ctx);

                Assert.AreEqual(banked, kit.UltimateCharge, 0.0001f,
                    "the round boundary emptied the ultimate meter. It is a match-long resource "
                    + "and only ResetForMatch may clear it; see HeroKit.ResetForRound.");

                Assert.AreEqual(kit.Skill2.MaxCharges, kit.Skill2.ChargesRemaining,
                    "the barricade did not refill at the round boundary. Skill charges are a "
                    + "per-round allowance and every round starts full.");

                // ⚠️ AND THE MATCH BOUNDARY IS THE ONE THAT TAKES THE METER. Without this the
                // test above is satisfied by a build that never clears the meter at all, which
                // would carry an ultimate from one match into the next.
                kit.ResetForMatch(ctx);
                Assert.AreEqual(0.0f, kit.UltimateCharge, 0.0001f,
                    "ultimate charge survived into a new match");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = ignoring;
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>A new match starts everybody at zero, whatever last match left behind.</summary>
        [Test]
        public void ResetForMatchClearsUltimateCharge()
        {
            var kit = HeroAbilitySystem.CreateKitFor("cheska");

            kit.AddUltimateCharge(HeroKit.UltimateMax);
            kit.ResetForMatch(null);

            Assert.AreEqual(0.0f, kit.UltimateCharge, 0.0001f,
                "ultimate charge survived into a new match");
        }

        /// <summary>
        /// ⚠️⚠️ TIME PASSING EARNS NOTHING, IN OR OUT OF PRACTICE, AND THIS TEST USED TO ASSERT
        /// THE OPPOSITE. It was written when charge trickled at `UltimatePassiveChargePerSecond`
        /// 1.0/s and it checked the two halves of that: frozen in practice, resuming when live.
        /// The first half was right. The second half was asserting a bug.
        ///
        /// At 1.0/s against a max of 100, a player who did NOTHING reached 90 of the 100 needed
        /// in a 90 s round, so the meter was a 100 second clock and the objectives were a bonus
        /// on top. `docs/VISION.md` § 4 lists **"Nothing may reward waiting"** as a competitive
        /// requirement and names the ultimate charge in the same sentence, so the trickle was
        /// against the mode's own rules the whole time it was passing this test.
        ///
        /// 🧑 2026-08-25: *"make it so that ult has to be charged and isnt cooldown gated"*.
        /// The trickle is deleted, so both halves now assert zero and the test is named for what
        /// it actually checks. `docs/Hero_Strike_Balance.md` § 2.1.
        /// </summary>
        [Test]
        public void TimePassingEarnsNoUltimateCharge()
        {
            var kit = new ProbeKit { PracticeMode = true };

            for (int i = 0; i < 120; i++) kit.Tick(null, 1.0f / 60.0f);

            Assert.AreEqual(0.0f, kit.UltimateCharge, 0.0001f,
                "charge accrued during practice, so warm-up time earns an ultimate");

            // A whole 90 s round at 60 Hz, live, with the player doing nothing at all.
            kit.PracticeMode = false;
            for (int i = 0; i < 90 * 60; i++) kit.Tick(null, 1.0f / 60.0f);

            Assert.AreEqual(0.0f, kit.UltimateCharge, 0.0001f,
                "a full live round of standing still produced ultimate charge. Every point must "
                + "be earned by an act: see Balance's note on why the passive trickle was "
                + "deleted, and do not reintroduce one");
        }

        /// <summary>
        /// ⚠️⚠️ THE FIVE ULTIMATES DO NOT COST THE SAME, AND UNTIL 2026-08-25 THEY DID. The
        /// price was `HeroKit.UltimateMax`, a `const` shared by every kit, so a Thunderstrike
        /// that stuns everyone within 4.5 m of Zack's own feet with no aim and no counterplay
        /// cost exactly what Nemu's Seance Void costs, which is a zone that drags and slows and
        /// ends no round on its own.
        ///
        /// This asserts the ORDER rather than the five numbers, because the order is the design
        /// decision and the numbers are a tuning pass waiting on `BotBehaviourProbe`. The
        /// reasoning for each is on the `UltimateCost` override in its own kit, and the table is
        /// `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        [Test]
        public void UltimateCostsAreRankedByHowMuchTheUltimateSwingsARound()
        {
            float zack = HeroAbilitySystem.CreateKitFor("zack").UltimateCost;
            float cheska = HeroAbilitySystem.CreateKitFor("cheska").UltimateCost;
            float sean = HeroAbilitySystem.CreateKitFor("sean").UltimateCost;
            float dante = HeroAbilitySystem.CreateKitFor("dante").UltimateCost;
            float nemu = HeroAbilitySystem.CreateKitFor("nemu").UltimateCost;

            // Cannot-miss burst at the top; the setup zone at the bottom.
            Assert.Greater(zack, cheska, "Thunderstrike must cost more than Glacial Nova");
            Assert.Greater(cheska, sean, "Glacial Nova must cost more than Supernova");
            Assert.Greater(sean, dante, "Supernova must cost more than Titan Fissure");
            Assert.Greater(dante, nemu, "Titan Fissure must cost more than Seance Void");

            // ⚠️ AND EVERY ONE MUST BE REACHABLE IN A ROUND. The best objective in the game pays
            // `UltimateChargeLataKnock` 25, so a cost past 175 is more than seven knockdowns and
            // the power would effectively not exist. This is the bound, not a target.
            Assert.LessOrEqual(zack, Balance.UltimateChargeLataKnock * 7.0f,
                "the most expensive ultimate is out of reach inside a single round");

            // And none may be so cheap it is spammable off throws alone.
            Assert.GreaterOrEqual(nemu, Balance.UltimateChargeLegalThrow * 15.0f,
                "the cheapest ultimate can be bought with throws, which are safe and free");
        }

        /// <summary>
        /// ⚠️⚠️ AN ABILITY IS EITHER ON A COOLDOWN OR ON CHARGES, NEVER BOTH, AND NEVER NEITHER.
        /// A charge ability carries `Cooldown` 0, so if one ever also acquired a cooldown the
        /// deck would draw it as Cooling while it still had charges in hand, and if a cooldown
        /// ability lost its cooldown it would be castable every frame with nothing to say so.
        /// Both are silent failures in play and neither is visible in a review of the one file
        /// that would cause it.
        /// </summary>
        [Test]
        public void EveryShippedAbilityIsGatedByExactlyOneOfCooldownOrCharges()
        {
            foreach (string hero in new[] { "sean", "zack", "dante", "cheska", "nemu" })
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);

                foreach (var skill in new[] { kit.Skill1, kit.Skill2 })
                {
                    Assert.NotNull(skill, hero + " is missing a skill");

                    if (skill.UsesCharges)
                    {
                        Assert.AreEqual(0.0f, skill.Cooldown, 0.0001f,
                            $"{hero}/{skill.Id} has charges AND a {skill.Cooldown}s cooldown");
                        Assert.AreEqual(skill.MaxCharges, skill.ChargesRemaining,
                            $"{hero}/{skill.Id} does not start a round full");
                    }
                    else
                    {
                        // ⚠️ 30 s IS THE FLOOR AND IT IS 🧑'S NUMBER, NOT AN INFERENCE.
                        // 2026-08-25: *"make it long tho like 30seconds to 45 seconds"*. At the
                        // old 6 to 9 s cooldowns four seats cast 44 to 56 times in a 90 s round,
                        // one every 1.8 seconds, and nothing at that rate is a decision.
                        Assert.GreaterOrEqual(skill.Cooldown, 30.0f,
                            $"{hero}/{skill.Id} cools in {skill.Cooldown}s, under the 30s floor");
                        Assert.LessOrEqual(skill.Cooldown, 45.0f,
                            $"{hero}/{skill.Id} cools in {skill.Cooldown}s, over the 45s ceiling");
                    }
                }

                // ⚠️ THE ULTIMATE IS GATED BY THE METER AND MUST NOT ALSO CARRY A COOLDOWN.
                // Two gates on one power means a player who earned 150 points can still be told
                // to wait, which is the refusal `docs/Hero_Strike_UI.md` § 6 has no answer for.
                Assert.AreEqual(0.0f, kit.Ultimate.Cooldown, 0.0001f,
                    hero + "'s ultimate carries a cooldown as well as a charge cost");
                Assert.IsFalse(kit.Ultimate.UsesCharges,
                    hero + "'s ultimate is on ability charges as well as the ultimate meter");
            }
        }

        /// <summary>
        /// ⚠️⚠️ A RECHARGE IS AN EVENT, NEVER A TIMER, AND ONLY SOME SKILLS GET ONE. A kit where
        /// everything comes back is a kit with cooldowns and extra bookkeeping, which is the
        /// thing the charge split was introduced to get away from. This pins both halves: that
        /// the recharging skills recharge off the right event, and that the rest genuinely run
        /// out. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        [Test]
        public void ChargesComeBackOnPlayAndOnlyForTheSkillsThatShould()
        {
            var cheska = HeroAbilitySystem.CreateKitFor("cheska");

            // ⚠️ A REAL MOTOR, BECAUSE `OnActivate` SPAWNS. Cheska's barricade reads
            // `ctx.Position` and `ctx.Forward` off the motor's transform and then builds the
            // wall, so a null context throws before the charge bookkeeping this test is about
            // ever runs. `Nemu_AstralProjection_SupportsReactivation` in `RuntimeLayerTests`
            // sets one up the same way.
            var go = new GameObject("ChargeProbeMotor");

            // ⚠️⚠️ THE BARRICADE'S ICE CHIPS CALL `Destroy` ON A SELF-TIMER AND UNITY LOGS AN
            // ERROR FOR THAT OUTSIDE PLAY MODE. It is an EditMode artefact and nothing else:
            // `Destroy` is correct at runtime, which is the only place the barricade is ever
            // built for real, and the test framework promotes any unexpected `[Error]` to a
            // failure. Suppressed narrowly and restored in `finally`, so it cannot mask an error
            // from a later test.
            bool ignoring = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                var motor = go.AddComponent<CharacterMotor>();
                var ctx = new AbilityContext(motor, null, null);

                // The barricade is one charge, refilled by the retrieval. Spend it, then take
                // the risk the whole game is built around and get it back.
                Assert.AreEqual(1, cheska.Skill2.MaxCharges);
                cheska.Skill2.Activate(ctx);
                Assert.AreEqual(0, cheska.Skill2.ChargesRemaining, "the barricade did not spend");

                cheska.OnRechargeEvent(HeroAbility.Recharge.LataKnocked);
                Assert.AreEqual(0, cheska.Skill2.ChargesRemaining,
                    "the barricade recharged off the wrong event");

                cheska.OnRechargeEvent(HeroAbility.Recharge.OwnSlipperRetrieved);
                Assert.AreEqual(1, cheska.Skill2.ChargesRemaining,
                    "retrieving her own tsinelas did not hand the barricade back");

                // ⚠️ AND IT CANNOT OVERFILL. Two retrievals must not bank two walls.
                cheska.OnRechargeEvent(HeroAbility.Recharge.OwnSlipperRetrieved);
                Assert.AreEqual(1, cheska.Skill2.ChargesRemaining,
                    "the barricade banked past its cap");

                // The frost sheet is deliberately one of the ones that runs out.
                Assert.AreEqual(HeroAbility.Recharge.Never, cheska.Skill1.RechargedBy,
                    "the frost sheet acquired a recharge; it is meant to run out");

                var sean = HeroAbilitySystem.CreateKitFor("sean");
                sean.Skill2.Activate(ctx);
                Assert.AreEqual(1, sean.Skill2.ChargesRemaining, "the cannon did not spend");

                sean.OnRechargeEvent(HeroAbility.Recharge.LataKnocked);
                Assert.AreEqual(2, sean.Skill2.ChargesRemaining,
                    "knocking the lata over did not hand the ignition charge back");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = ignoring;
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// ⚠️⚠️ AND THE ULTIMATE IS STILL TESTABLE IN THERE. 🧑: *"BUt i want ppl to be
        /// able to test skills still and shit during buffer period"*. Free to cast, and the cast
        /// must not spend the bank, or practising costs a player the ultimate they carried in.
        /// </summary>
        [Test]
        public void PracticeCastsAreFreeAndDoNotSpendTheBank()
        {
            var kit = new ProbeKit { PracticeMode = true };
            kit.AddUltimateCharge(HeroKit.UltimateMax * 0.5f);
            float banked = kit.UltimateCharge;

            Assert.IsTrue(kit.IsUltimateReady,
                "the ultimate is not castable in practice, so nobody can ever rehearse it");
            Assert.IsTrue(kit.TryActivateUltimate(null), "the practice cast was refused");

            Assert.AreEqual(banked, kit.UltimateCharge, 0.0001f,
                "a practice cast spent the banked charge");
        }

        /// <summary>And once the round is live it costs the meter, exactly as before.</summary>
        [Test]
        public void LiveCastsSpendTheBank()
        {
            var kit = new ProbeKit { PracticeMode = false };
            kit.AddUltimateCharge(HeroKit.UltimateMax);

            Assert.IsTrue(kit.TryActivateUltimate(null), "a fully charged ultimate was refused");
            Assert.AreEqual(0.0f, kit.UltimateCharge, 0.0001f,
                "a live cast did not spend the meter");
        }

        /// <summary>An empty meter refuses outside practice, or the economy means nothing.</summary>
        [Test]
        public void AnEmptyMeterRefusesOutsidePractice()
        {
            var kit = new ProbeKit { PracticeMode = false };

            Assert.IsFalse(kit.IsUltimateReady, "an empty meter reported ready");
            Assert.IsFalse(kit.TryActivateUltimate(null), "an empty meter cast anyway");
        }

        /// <summary>
        /// A stand-in ability, so the round-boundary contract can be tested without a live
        /// arena.
        ///
        /// ⚠️ THE REAL KITS CANNOT BE ACTIVATED IN AN EDITMODE TEST. Their `OnActivate`
        /// spawns hazards, plays audio and reads `ctx.Motor`, none of which exist outside a
        /// running match. What is being tested here is the BASE CLASS contract every one of
        /// them inherits, and this exercises exactly that.
        /// </summary>
        private sealed class ProbeAbility : HeroAbility
        {
            public bool EndRan;

            public ProbeAbility() : base("probe", "PROBE", "A stand-in.", 5.0f, 3.0f,
                                         AbilityGlyph.Burst)
            {
            }

            protected override void OnEnd(AbilityContext ctx) => EndRan = true;

            /// <summary>
            /// ⚠️ THE BASE CHECK NEEDS A LIVE MOTOR AND THERE ISN'T ONE IN AN EDITMODE TEST.
            /// `HeroAbility.CanActivate` refuses a null context and asks `Motor.CanAct()`, both
            /// of which are about the WORLD. What is under test here is the kit's own economy,
            /// so this keeps the only part of the check that belongs to the ability itself.
            /// </summary>
            public override bool CanActivate(AbilityContext ctx) => IsReady;
        }

        private sealed class ProbeKit : HeroKit
        {
            public ProbeKit() : base("probe", "PROBE")
            {
                Skill1 = new ProbeAbility();
                Skill2 = new ProbeAbility();
                Ultimate = new ProbeAbility();
            }
        }

        /// <summary>A round boundary clears cooldowns too, or round two opens on round one's timers.</summary>
        [Test]
        public void ResetForRoundClearsCooldowns()
        {
            var kit = new ProbeKit();

            kit.Skill1.Activate(null);
            kit.Skill2.Activate(null);

            Assert.IsFalse(kit.Skill1.IsReady, "the harness failed to put skill 1 on cooldown");
            Assert.IsTrue(kit.Skill1.IsActive, "the harness failed to make skill 1 active");

            kit.ResetForRound(null);

            Assert.IsTrue(kit.Skill1.IsReady, "skill 1 is still cooling after a reset");
            Assert.IsTrue(kit.Skill2.IsReady, "skill 2 is still cooling after a reset");
            Assert.IsFalse(kit.Skill1.IsActive, "skill 1 is still running after a reset");
            Assert.IsFalse(kit.Skill2.IsActive, "skill 2 is still running after a reset");
        }

        /// <summary>
        /// ⚠⚠ AN ACTIVE ABILITY IS ENDED, NOT DROPPED, AND THE DIFFERENCE IS A PERMANENT
        /// BUFF. Demonic Carapace grants stun immunity in `OnActivate` and takes it back in
        /// `OnEnd`; Phantom Phase does the same for tag immunity. Zeroing the timer behind their
        /// backs at a round boundary would leave the grant switched on with no timer left to
        /// switch it off, so a hero mid-Carapace when the round ended would open the next one
        /// permanently unstunnable.
        /// </summary>
        [Test]
        public void ResetForRoundEndsAnActiveAbilityCleanly()
        {
            var kit = new ProbeKit();
            var skill = (ProbeAbility)kit.Skill1;

            skill.Activate(null);
            Assert.IsFalse(skill.EndRan, "the harness ended the ability before the reset");

            kit.ResetForRound(null);

            Assert.IsTrue(skill.EndRan,
                "ResetForRound dropped an active ability without running OnEnd, which leaks " +
                "whatever that ability had granted");
        }

        /// <summary>
        /// ⚠️ EVERY ABILITY NEEDS AN ICON AND A SENTENCE, because character select and the
        /// hold-to-read panel both draw them and a blank tile is worse than no tile. This is
        /// what stops a new hero from shipping with three empty slots.
        /// </summary>
        [Test]
        public void EveryAbilityIsPresentable()
        {
            foreach (string heroId in new[] { "cheska", "dante", "nemu", "sean", "zack" })
            {
                var kit = HeroAbilitySystem.CreateKitFor(heroId);

                foreach (var ability in new[] { kit.Skill1, kit.Skill2, kit.Ultimate })
                {
                    Assert.IsNotNull(ability, $"{heroId} is missing an ability");
                    Assert.IsNotEmpty(ability.Name, $"{heroId}: an ability has no name");
                    Assert.IsNotEmpty(ability.Description,
                        $"{heroId}: {ability.Name} has no description, so the inspect panel " +
                        "and character select would both draw an empty row");
                    Assert.IsNotNull(AbilityIcons.For(ability.Glyph),
                        $"{heroId}: {ability.Name} has no icon");
                    Assert.IsNotEmpty(AbilityIcons.LabelFor(ability.Glyph),
                        $"{heroId}: {ability.Name}'s glyph has no label");
                }
            }
        }

        // ------------------------------------------------------------------ audio

        /// <summary>
        /// ⚠⚠ THE ANNOUNCEMENTS DUCK THE BED AND THE IMPACTS DO NOT. `audio_manager.gd` 4.6
        /// hooks the duck at the play path so no other file has to know the music exists; the
        /// value of the table is entirely that the countdown, the round end and the score award
        /// do not each have to remember. A cue quietly dropping out of it is silent: the mix
        /// just gets muddier under the one moment that carries information.
        /// </summary>
        [Test]
        public void TheAnnouncementCuesDuckTheMusicBed()
        {
            foreach (string cue in new[] { "countdown_tick", "countdown_go", "round_end",
                                           "match_win", "round_lose", "score_award" })
                Assert.IsTrue(Audio.AudioCues.DucksMusic(cue),
                    $"'{cue}' no longer ducks the bed, so it plays over the top of it");

            // An impact ducks through `PlayImpact` by its own tiny amount scaled to the hit.
            // Putting it in this table as well would stack two ducks on one sound.
            foreach (string cue in new[] { "slipper_land", "bump_swing", "sfx_ice_freeze" })
                Assert.IsFalse(Audio.AudioCues.DucksMusic(cue),
                    $"'{cue}' is an impact and should not take the announcement duck as well");
        }

        /// <summary>
        /// ⚠️ THE MIX LEVEL OF A CUE IS ITS TRIM PLUS THE HEADROOM, AND THE TRIM CANNOT PUSH
        /// A CUE UP. The SFX bus was measured clipping at +2.0 dBFS with the music silent; the
        /// headroom is what stops a busy fight repeating that, and a positive trim would be a
        /// route around it rather than an expression inside it.
        /// </summary>
        [Test]
        public void NoCueTrimCanRaiseACueAboveTheHeadroom()
        {
            foreach (var pair in Audio.AudioCues.TrimDb)
                Assert.LessOrEqual(Audio.AudioCues.TrimFor(pair.Key), Audio.AudioCues.HeadroomDb,
                    $"'{pair.Key}' is trimmed above the mix headroom");

            Assert.AreEqual(Audio.AudioCues.HeadroomDb, Audio.AudioCues.TrimFor("no_such_cue"),
                            0.0001f, "an untrimmed cue should sit exactly at the headroom");
        }

        /// <summary>
        /// ⚠⚠ THE LIFT IS THE STAND-IN FOR A PRESSURE TRACK THAT HAS NOT BEEN DELIVERED. The
        /// Godot original says so and says what replaces it. If the numbers drift, the last
        /// fifteen seconds of a round stop reading as the round tightening, which is the only
        /// thing this exists to do.
        /// </summary>
        [Test]
        public void TheIntensityLiftIsAModestRampNearTheEndOfARound()
        {
            Assert.Greater(Audio.MusicDirector.LiftDb, 0.0f, "a lift that does not lift");
            Assert.LessOrEqual(Audio.MusicDirector.LiftDb, 6.0f,
                "more than 6 dB on the same bed reads as a mistake in the mix rather than as " +
                "pressure");

            Assert.Greater(Audio.MusicDirector.LiftSecondsLeft, 0.0f);
            Assert.Less(Audio.MusicDirector.LiftSecondsLeft, Core.Balance.RoundTime,
                "the lift would be on for the whole round, which is not a lift");

            Assert.Greater(Audio.MusicDirector.LiftTime, 0.0f,
                "an instant jump on one frame reads as a mix error");
            Assert.Less(Audio.MusicDirector.LiftTime, Audio.MusicDirector.LiftSecondsLeft,
                "the ramp must finish well before the clock does");
        }

        // ------------------------------------------------------------------ hazard steering

        /// <summary>
        /// ⚠️⚠️ THE BOTS USED TO WALK STRAIGHT THROUGH HERO HAZARDS, and it cost real points:
        /// `BotBehaviourProbe` measured Hero Strike unretrieved-slipper penalties swinging from
        /// 0 to 28 across identical runs while Classic sat at a flat 0. A hazard landing between
        /// an attacker and its tsinelas is the whole variance.
        /// </summary>
        [Test]
        public void HazardOnThePathIsFoundAndSteeredAround()
        {
            HazardMap.Clear();

            var go = new GameObject("TestHazard");
            try
            {
                go.transform.position = new Vector3(0.0f, 0.0f, 5.0f);
                HazardVolume.Attach(go, 2.0f, ownerSlot: 3);

                Vector3 from = Vector3.zero;
                Vector3 to = new Vector3(0.0f, 0.0f, 10.0f);

                Assert.IsTrue(HazardMap.TryFindBlocker(from, to, mySlot: 1, bodyRadius: 0.5f,
                                                       maxRadius: AiTuning.HazardAvoidMaxRadius,
                                                       out var blocker),
                    "a hazard sitting on the straight line was not reported as a blocker");

                Vector3 steer = HazardMap.SteerAround(from, to, blocker, 0.5f);

                Assert.Greater(Mathf.Abs(steer.x), 0.05f,
                    "the steer did not move sideways, so it still walks into the hazard");

                // ⚠️ AND IT STILL MAKES PROGRESS. Steering ninety degrees off would clear the
                // hazard and never arrive; the tangent has to keep most of its forward component.
                Assert.Greater(steer.z, 0.0f,
                    "the steer points away from the goal instead of around the hazard");
            }
            finally
            {
                Object.DestroyImmediate(go);
                HazardMap.Clear();
            }
        }

        /// <summary>Your own trail sits under your own feet by design. Avoiding it is a bug.</summary>
        [Test]
        public void YourOwnHazardIsNotAvoided()
        {
            HazardMap.Clear();

            var go = new GameObject("TestHazard");
            try
            {
                go.transform.position = new Vector3(0.0f, 0.0f, 5.0f);
                HazardVolume.Attach(go, 2.0f, ownerSlot: 1);

                Assert.IsFalse(HazardMap.TryFindBlocker(Vector3.zero, new Vector3(0, 0, 10),
                                                        mySlot: 1, bodyRadius: 0.5f,
                                                        maxRadius: AiTuning.HazardAvoidMaxRadius,
                                                        out _),
                    "a hero was told to walk around its own trail");
            }
            finally
            {
                Object.DestroyImmediate(go);
                HazardMap.Clear();
            }
        }

        /// <summary>A hazard behind you is not a reason to turn.</summary>
        [Test]
        public void HazardBehindIsIgnored()
        {
            HazardMap.Clear();

            var go = new GameObject("TestHazard");
            try
            {
                go.transform.position = new Vector3(0.0f, 0.0f, -5.0f);
                HazardVolume.Attach(go, 2.0f, ownerSlot: -1);

                Assert.IsFalse(HazardMap.TryFindBlocker(Vector3.zero, new Vector3(0, 0, 10),
                                                        mySlot: 1, bodyRadius: 0.5f,
                                                        maxRadius: AiTuning.HazardAvoidMaxRadius,
                                                        out _),
                    "a hazard behind the body was treated as a blocker");
            }
            finally
            {
                Object.DestroyImmediate(go);
                HazardMap.Clear();
            }
        }

        /// <summary>The give-up distance exists so a slipper inside a hazard is still fetched.</summary>
        [Test]
        public void GiveUpDistanceIsShorterThanTheAvoidMargin()
        {
            Assert.Greater(AiTuning.HazardAvoidGiveUp, AiTuning.HazardAvoidMargin,
                "a bot that gives up before it has cleared its own margin would never " +
                "avoid anything at all");
        }

        /// <summary>
        /// ⚠️⚠️ AN ARENA-SIZED HAZARD IS WALKED THROUGH, and this test is the tripwire on the
        /// ability footprints. Turning avoidance on without this cap took Hero Strike from
        /// 78-97 throws a match down to 17, because a disc covering most of a 14 by 14 box has
        /// no way round it and the bots walked the perimeter instead of playing.
        ///
        /// ⚠️ WHEN `docs/TODO.md` § 1 LANDS AND THE FOOTPRINTS COME DOWN, this stops being a
        /// special case on its own. Do not raise the cap to "fix" it; shrink the ability.
        /// </summary>
        [Test]
        public void AnArenaSizedHazardIsNotAvoided()
        {
            HazardMap.Clear();

            var go = new GameObject("TestHazard");
            try
            {
                go.transform.position = new Vector3(0.0f, 0.0f, 5.0f);
                HazardVolume.Attach(go, 7.5f, ownerSlot: -1);   // Nemu's Seance Void, as shipped

                Assert.IsFalse(HazardMap.TryFindBlocker(Vector3.zero, new Vector3(0, 0, 10),
                                                        mySlot: 1, bodyRadius: 0.5f,
                                                        maxRadius: AiTuning.HazardAvoidMaxRadius,
                                                        out _),
                    "a hazard wider than the cap was treated as avoidable, which strands the " +
                    "bots on the perimeter");
            }
            finally
            {
                Object.DestroyImmediate(go);
                HazardMap.Clear();
            }
        }
    }
}
