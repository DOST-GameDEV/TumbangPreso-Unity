using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeBotOperatorTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(90000)]
        public IEnumerator ClassicPointerMenuRestoresChosenBotAsAttackerAndDefender() => Operate(GameMode.Classic, false);

        [UnityTest, Timeout(90000)]
        public IEnumerator HeroSubmitMenuRestoresChosenBotAsAttackerAndDefender() => Operate(GameMode.HeroStrike, true);

        private static IEnumerator Operate(GameMode mode, bool submit)
        {
            bool network = SceneFlow.Networked, bots = AIController.BotsEnabled, sandbox = PracticeSandbox.Wanted;
            bool tutorial = GameLaunch.GuidedTutorial, rangeFlag = GameLaunch.TrainingRange;
            bool spectator = GameLaunch.Spectator, allBots = GameLaunch.AllBots, pinned = SceneFlow.RulesPinned;
            int soloSeat = GameLaunch.SoloSeat;
            var rules = SceneFlow.SelectedRules.Clone();
            var settings = Settings.SettingsStore.Current; int difficulty = settings.AiDifficulty, pick = settings.CharacterPick;
            try
            {
                SceneFlow.Networked = false; MatchAbandon.Forget();
                GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
                GameLaunch.TrainingRange = true; GameLaunch.SoloSeat = 1; PracticeSandbox.Clear();
                settings.AiDifficulty = AIController.NoBotsIndex; AIController.BotsEnabled = false;
                settings.CharacterPick = 0;
                SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                float until = Time.realtimeSinceStartup + 20;
                while (!PracticeRange.Active && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(PracticeRange.Active, "The actual offline range must finish startup.");
                Assert.IsFalse(UI.Hub.HubLoading.Visible, "Startup loading must finish before exercising menu targets.");
                var range = PracticeRange.Instance; var local = range.Local;
                Assert.AreEqual(mode, local.Mode); Assert.AreEqual(1, local.PlayerSlot);
                Assert.AreEqual(1, GameServices.Round.Players.Count);
                var watcher = Object.FindFirstObjectByType<PauseWatcher>(); Assert.IsNotNull(watcher);
                var prepared = watcher.GetComponentInChildren<PausePanel>(true); Assert.IsNotNull(prepared);
                var menu = Panel.Open<PausePanel>(watcher); yield return null;
                Assert.AreSame(prepared, menu); Assert.IsTrue(Panel.AnyOpen); Assert.Zero(Time.timeScale);
                Assert.IsTrue(local.Intent.Parked); Assert.IsTrue(range.CanEdit);
                var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(c => c.name == "OwnerPauseCanvas");
                var scroll = canvas.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single(s => s.name == "TrainingScroll");
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                var seatChoice = canvas.GetComponentsInChildren<TumpChoice>().Single(c => c.name == "SeatChoice");
                var behaviour = canvas.GetComponentsInChildren<TumpChoice>().Single(c => c.name == "BehaviourChoice");
                var present = canvas.GetComponentsInChildren<UnityEngine.UI.Toggle>().Single(t => t.name == "PresentToggle");
                int[] botSeats = Enumerable.Range(0, Balance.PlayerCount).Where(s => s != local.PlayerSlot).ToArray();
                int targetSeat = botSeats[2], untouchedSeat = botSeats[0];
                yield return Choose(canvas, seatChoice, 2, submit);
                Assert.AreEqual(2, seatChoice.Value); Assert.IsFalse(present.isOn);
                Assert.IsFalse(range.BotPresent(targetSeat)); Assert.IsFalse(range.BotPresent(untouchedSeat));
                Press(present, submit); yield return null;
                var bot = GameServices.Round.PlayerAt(targetSeat); Assert.IsNotNull(bot);
                var brain = bot.GetComponent<AIController>(); Assert.IsNotNull(brain);
                var shoe = bot.GetComponent<Carrier>().Held; Assert.IsNotNull(shoe);
                Assert.AreEqual(targetSeat, bot.PlayerSlot); Assert.IsFalse(bot.IsDefender);
                Assert.IsTrue(range.BotIdle(targetSeat)); Assert.IsFalse(brain.enabled); Assert.IsTrue(bot.Intent.Parked);
                Assert.AreEqual(targetSeat, shoe.OwnerSlot); Assert.IsTrue(shoe.gameObject.activeSelf);
                Assert.IsNull(GameServices.Round.PlayerAt(untouchedSeat), "Changing a non-default menu seat mutated another bot.");
                yield return Choose(canvas, behaviour, 1, submit);
                Assert.IsFalse(range.BotIdle(targetSeat)); Assert.IsTrue(brain.enabled); Assert.IsFalse(bot.Intent.Parked);
                Press(present, submit); yield return null;
                Assert.IsFalse(range.BotPresent(targetSeat)); Assert.IsNull(GameServices.Round.PlayerAt(targetSeat));
                Assert.IsFalse(bot.gameObject.activeSelf); Assert.IsFalse(shoe.gameObject.activeSelf);
                Assert.AreEqual(1, behaviour.Value, "Removal must preserve the chosen Active behaviour.");
                Press(present, submit); yield return null;
                Assert.AreSame(bot, GameServices.Round.PlayerAt(targetSeat)); Assert.AreSame(brain, bot.GetComponent<AIController>());
                Assert.AreSame(shoe, bot.GetComponent<Carrier>().Held); Assert.IsTrue(brain.enabled);
                Assert.IsFalse(bot.IsDefender); Assert.IsFalse(bot.Intent.Parked);

                // The defender picker is near the top; returning the real scroll to
                // its top exercises the same reused controls rather than SetDefender.
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                var defender = canvas.GetComponentsInChildren<TumpChoice>().Single(c => c.name == "DefenderChoice");
                yield return Choose(canvas, defender, targetSeat, submit);
                Assert.AreEqual(targetSeat, GameServices.Match.DefenderSlot); Assert.IsTrue(bot.IsDefender);
                Assert.IsNull(bot.GetComponent<Carrier>().Held); Assert.IsFalse(shoe.gameObject.activeSelf);
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                Assert.IsTrue(present.isOn); Assert.AreEqual(1, behaviour.Value);
                Press(present, submit); yield return null;
                Assert.IsNull(GameServices.Round.PlayerAt(targetSeat)); Assert.IsFalse(bot.gameObject.activeSelf);
                Press(present, submit); yield return null;
                Assert.AreSame(bot, GameServices.Round.PlayerAt(targetSeat)); Assert.IsTrue(bot.IsDefender);
                Assert.IsTrue(brain.enabled); Assert.IsNull(bot.GetComponent<Carrier>().Held);
                Assert.IsFalse(shoe.gameObject.activeSelf); Assert.IsFalse(range.BotPresent(untouchedSeat));
                Assert.IsTrue(local.Intent.Parked); Assert.Zero(Time.timeScale); Assert.IsFalse(TumpChoice.OpenChoice != null);
                var resume = canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "ResumeMatch");
                Press(resume, submit);
                Assert.IsFalse(menu.gameObject.activeSelf); Assert.IsFalse(Panel.AnyOpen); Assert.IsFalse(local.Intent.Parked);
                Assert.Greater(Time.timeScale, 0); Assert.AreSame(bot, GameServices.Round.PlayerAt(targetSeat));
                until = Time.time + 1;
                while (brain.Plan == AiPlan.Idle && Time.time < until) yield return null;
                Assert.AreNotEqual(AiPlan.Idle, brain.Plan, "The restored active bot's real planner never resumed.");
                Assert.IsTrue(brain.enabled); Assert.IsTrue(bot.CanAct());
                Assert.AreEqual(AIController.NoBotsIndex, settings.AiDifficulty); Assert.IsFalse(AIController.BotsEnabled);
                Assert.AreEqual(2, GameServices.Round.Players.Count, "The menu route must register only the local and selected bot.");
            }
            finally
            {
                GameServices.Round?.EndRound(); GameServices.Match?.ResetForNewMatch();
                SceneFlow.Networked = network; AIController.BotsEnabled = bots; PracticeSandbox.Wanted = sandbox;
                settings.AiDifficulty = difficulty; settings.CharacterPick = pick;
                SceneFlow.AdoptRemoteRules(rules);
                if (pinned) SceneFlow.PinSelectedRules(rules); else SceneFlow.UnpinSelectedRules();
                GameLaunch.GuidedTutorial = tutorial; GameLaunch.TrainingRange = rangeFlag;
                GameLaunch.Spectator = spectator; GameLaunch.AllBots = allBots; GameLaunch.SoloSeat = soloSeat;
            }
        }

        private static IEnumerator Choose(Canvas canvas, TumpChoice choice, int value, bool submit)
        {
            Press(choice.GetComponent<UnityEngine.UI.Button>(), submit); yield return null;
            Assert.IsTrue(choice.IsOpen); Assert.IsTrue(ScreenTakeover.EscapeIsSpoken);
            var option = canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Choice" + value);
            Press(option, submit); yield return null;
            Assert.AreEqual(value, choice.Value); Assert.IsFalse(choice.IsOpen);
        }

        private static void Press(UnityEngine.UI.Selectable control, bool submit)
        {
            Assert.IsNotNull(EventSystem.current, "The actual menu requires its EventSystem.");
            Assert.IsTrue(control.IsActive()); Assert.IsTrue(control.IsInteractable());
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)control.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, "Operator control has no visible pointer target: " + control.name);
            Assert.AreSame(control, hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Selectable>(),
                "Another visible control intercepted " + control.name);
            if (submit)
            {
                control.Select();
                ExecuteEvents.Execute(control.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
            else ExecuteEvents.Execute(control.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
