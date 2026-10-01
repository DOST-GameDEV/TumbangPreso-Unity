using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeRangeTests
    {
        private sealed class ResourcesProbe : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public ResourcesProbe() : base("probe", "probe", "", 10, charges: 3)
            { CooldownRemaining = 5; ChargesRemaining = 1; }
        }

        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [Test]
        public void ChargeAndCooldownRefillsAreIndependent()
        {
            var skill = new ResourcesProbe();
            skill.RefillForSandbox(cooldown: true, charges: false);
            Assert.Zero(skill.CooldownRemaining); Assert.AreEqual(1, skill.ChargesRemaining);
            skill = new ResourcesProbe();
            skill.RefillForSandbox(cooldown: false, charges: true);
            Assert.AreEqual(5, skill.CooldownRemaining); Assert.AreEqual(3, skill.ChargesRemaining);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator RangeControlsStayLocalAndReuseTheirPreparedMenuAndBodies()
        {
            bool network = SceneFlow.Networked, bots = AIController.BotsEnabled, sandbox = PracticeSandbox.Wanted;
            bool tutorial = GameLaunch.GuidedTutorial, rangeFlag = GameLaunch.TrainingRange;
            bool spectator = GameLaunch.Spectator, allBots = GameLaunch.AllBots;
            var oldRules = SceneFlow.SelectedRules;
            var settings = Settings.SettingsStore.Current;
            int difficulty = settings.AiDifficulty, pick = settings.CharacterPick;
            try
            {
                SceneFlow.Networked = false; MatchAbandon.Forget();
                GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
                GameLaunch.TrainingRange = true; PracticeSandbox.Clear();
                settings.AiDifficulty = AIController.NoBotsIndex; AIController.BotsEnabled = false;
                SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                float until = Time.realtimeSinceStartup + 20;
                while (!PracticeRange.Active && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(PracticeRange.Active);
                var range = PracticeRange.Instance;
                var local = range.Local;
                Assert.AreEqual(1, GameServices.Round.Players.Count);
                Assert.IsFalse(local.IsDefender); Assert.IsTrue(local.HoldingSlipper);
                Assert.IsNull(Object.FindFirstObjectByType<ReadyGate>());
                Assert.IsFalse(local.Intent.Parked);
                var watcher = Object.FindFirstObjectByType<PauseWatcher>();
                var prepared = watcher.GetComponentInChildren<PausePanel>(true);
                Assert.IsNotNull(prepared); Assert.IsFalse(prepared.gameObject.activeSelf);
                var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(c => c.name == "OwnerPauseCanvas");
                Assert.IsFalse(canvas.gameObject.activeSelf, "A prepared root canvas was visible before opening its owner.");
                Assert.IsNotNull(canvas.GetComponentInChildren<ScrollRect>(true));
                var menu = Panel.Open<PausePanel>(watcher);
                yield return null;
                Assert.AreSame(prepared, menu); Assert.IsTrue(local.Intent.Parked);
                Assert.Zero(Time.timeScale, "Training menu must keep offline simulation paused.");
                yield return TumpUiCapture.Capture("TrainingPauseControls", canvas, 1280, 720, false);
                Assert.IsTrue(range.CanEdit, "Pausing the world must not disable its training configuration menu.");
                Assert.IsTrue(canvas.GetComponentsInChildren<Button>().First(b => b.name == "DefenderChoice").interactable);
                Assert.IsTrue(canvas.GetComponentsInChildren<Toggle>().First(t => t.name == "Infinite staminaToggle").interactable);
                typeof(PresentationClock).GetMethod("Hold", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
                Assert.IsFalse(range.CanEdit, "A shared presentation hold must still block world changes.");
                typeof(PresentationClock).GetMethod("Release", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
                Assert.IsTrue(range.CanEdit);


                var toggle = canvas.GetComponentsInChildren<Toggle>().First(t => t.name == "Infinite staminaToggle");
                toggle.isOn = true;
                Assert.IsTrue(range.InfiniteStamina); Assert.IsFalse(range.InfiniteSkills);
                Assert.IsFalse(range.FullUltimate); Assert.IsFalse(PracticeSandbox.Wanted);
                Assert.IsTrue(range.ChangeCharacter(0));
                Assert.AreEqual(0, local.CharacterIndex);
                Assert.AreEqual(Roster.GetPeople(local.Mode)[0].Id, local.AbilitySystem.HeroId);
                Assert.IsTrue(local.Intent.Parked, "Model/kit change released menu input.");

                int botSeat = (local.PlayerSlot + 1) % Balance.PlayerCount;
                Assert.IsTrue(range.SetBot(botSeat, true, true));
                var bot = GameServices.Round.PlayerAt(botSeat);
                Assert.IsNotNull(bot); Assert.IsTrue(bot.IsDefender); Assert.IsTrue(bot.Intent.Parked);
                Assert.IsNotNull(bot.GetComponent<AIController>()); Assert.IsFalse(bot.GetComponent<AIController>().enabled);
                Assert.IsTrue(range.SetDefender(local.PlayerSlot));
                Assert.IsTrue(local.IsDefender); Assert.IsTrue(local.Intent.Parked); Assert.IsFalse(bot.IsDefender);
                Assert.IsTrue(range.SetBot(botSeat, false, true));
                Assert.IsNull(GameServices.Round.PlayerAt(botSeat)); Assert.IsFalse(bot.gameObject.activeSelf);
                Assert.IsTrue(range.SetBot(botSeat, true, false));
                Assert.AreSame(bot, GameServices.Round.PlayerAt(botSeat));
                Assert.IsTrue(bot.GetComponent<AIController>().enabled); Assert.IsFalse(bot.Intent.Parked);
                Assert.IsFalse(bot.IsDefender); Assert.IsTrue(bot.HoldingSlipper);

                var can = GameServices.Round.Lata;
                can.ApplySnapshotState(can.transform.position, Quaternion.identity, false, 0);
                Assert.IsTrue(range.Set(PracticeRange.Cheat.FreezeCan, true));
                can.HostRestore(); Assert.IsFalse(can.IsUpright);
                Assert.IsTrue(range.ResetRange()); Assert.IsTrue(can.IsUpright);
                Assert.IsTrue(range.FreezeCan, "Reset should not erase chosen training rules.");

                SceneFlow.Networked = true;
                Assert.IsFalse(PracticeRange.Active); Assert.IsFalse(PracticeSandbox.Allowed);
                Assert.IsFalse(range.SetBot(botSeat, false, true));
                Assert.IsFalse(range.Set(PracticeRange.Cheat.FullUltimate, true));
                Assert.IsFalse(range.ChangeCharacter(1)); Assert.IsFalse(range.ResetRange());
                Assert.IsFalse(PracticeRange.RefillsAbilities(local));
                SceneFlow.Networked = false;
                Assert.IsTrue(range.SetBot(botSeat, false, true));
                var choice = canvas.GetComponentsInChildren<TumpChoice>().First(c => c.name == "CharacterChoice");
                choice.GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(choice.IsOpen); Assert.IsTrue(ScreenTakeover.EscapeIsSpoken);
                choice.Close();
                canvas.GetComponentsInChildren<Button>().First(b => b.name == "ResumeMatch").onClick.Invoke();
                Assert.IsFalse(menu.gameObject.activeSelf); Assert.IsFalse(local.Intent.Parked);
                Assert.AreEqual(AIController.NoBotsIndex, settings.AiDifficulty); Assert.AreEqual(pick, settings.CharacterPick);
                Assert.IsFalse(AIController.BotsEnabled, "Training overwrote the saved bots-off preference.");
                GameLaunch.Reset(); Assert.IsFalse(PracticeRange.Active); Assert.IsFalse(GameLaunch.TrainingRange);
            }
            finally
            {
                GameServices.Round?.EndRound(); GameServices.Match?.ResetForNewMatch();
                SceneFlow.Networked = network; AIController.BotsEnabled = bots;
                settings.AiDifficulty = difficulty; PracticeSandbox.Wanted = sandbox;
                SceneFlow.AdoptRemoteRules(oldRules);
                GameLaunch.GuidedTutorial = tutorial; GameLaunch.TrainingRange = rangeFlag;
                GameLaunch.Spectator = spectator; GameLaunch.AllBots = allBots;
            }
        }
    }
}
