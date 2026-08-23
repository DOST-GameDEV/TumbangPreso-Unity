using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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
                if (!listed.Contains(action.name)) orphans.Add(action.name);

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
        /// ⚠️⚠️ NO CHARGE IS EARNED WHILE THE ROUND CLOCK IS STOPPED. 🧑 2026-08-23: *"i js
        /// want it to pause when the game isnt ongoing like during the buffer period in between
        /// rounds it should pause"*. `Tick` used to trickle on every frame it ran, so a player
        /// who took their time in the warm-up arrived at the whistle with free charge.
        /// </summary>
        [Test]
        public void PracticeModeFreezesTheBank()
        {
            var kit = new ProbeKit { PracticeMode = true };

            for (int i = 0; i < 120; i++) kit.Tick(null, 1.0f / 60.0f);

            Assert.AreEqual(0.0f, kit.UltimateCharge, 0.0001f,
                "the passive trickle ran during practice, so warm-up time earns an ultimate");

            kit.PracticeMode = false;
            for (int i = 0; i < 120; i++) kit.Tick(null, 1.0f / 60.0f);

            Assert.Greater(kit.UltimateCharge, 0.0f,
                "the passive trickle did not resume once the round went live");
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
