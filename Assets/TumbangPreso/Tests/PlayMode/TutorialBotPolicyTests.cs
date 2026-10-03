using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class TutorialBotPolicyTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _installerRoot, _botRoot;
        private MatchInstaller _installer;
        private CharacterMotor _body;
        private AIController _brain;
        private bool _tutorial, _bots, _spectator, _pinned;
        private int _seat, _pick;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _tutorial = GameLaunch.GuidedTutorial; _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator;
            _seat = GameLaunch.SoloSeat; _pick = Settings.SettingsStore.Current.CharacterPick;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            _installerRoot = new GameObject("Tutorial selection policy");
            _installer = _installerRoot.AddComponent<MatchInstaller>(); _installer.enabled = false;
            _botRoot = new GameObject("Tutorial input policy");
            _body = _botRoot.AddComponent<CharacterMotor>(); _body.enabled = false;
            _body.IsBot = true; _body.Intent.Parked = false;
            _brain = _botRoot.AddComponent<AIController>(); _brain.enabled = false;
        }
        [UnityTearDown] public IEnumerator After()
        {
            Object.Destroy(_installerRoot); Object.Destroy(_botRoot); yield return PlayModeWorld.Reset();
            GameLaunch.GuidedTutorial = _tutorial; GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator;
            GameLaunch.SoloSeat = _seat; Settings.SettingsStore.Current.CharacterPick = _pick;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private int Pick(int slot) => (int)typeof(MatchInstaller).GetMethod("AiCharacterIndex", Hidden).Invoke(_installer, new object[] { slot });
        private void Press(Verb verb, bool down) => typeof(AIController).GetMethod("Press", Hidden).Invoke(_brain, new object[] { _body.Intent, verb, down });

        [Test] public void TutorialUsesDistinctCharactersForEveryHumanPickAndSeat()
        {
            GameLaunch.GuidedTutorial = true;
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(mode));
                for (int human = 0; human < Balance.PlayerCount; human++)
                for (int chosen = -1; chosen < Roster.GetPeople(mode).Count; chosen++)
                {
                    GameLaunch.SoloSeat = human; Settings.SettingsStore.Current.CharacterPick = chosen;
                    var seen = new HashSet<int>();
                    for (int slot = 0; slot < Balance.PlayerCount; slot++)
                    {
                        int pick = slot == human && chosen >= 0 ? chosen : Pick(slot);
                        Assert.IsTrue(seen.Add(pick), $"Duplicate tutorial character {pick}: {mode}, human seat {human}, pick {chosen}.");
                    }
                }
            }
        }
        [Test] public void TutorialBotsCannotPressUltimateButRetainOrdinarySkills()
        {
            GameLaunch.GuidedTutorial = true;
            Press(Verb.Ultimate, true); Assert.IsFalse(_body.Intent.Pressed(Verb.Ultimate));
            Press(Verb.Skill1, true); Press(Verb.Skill2, true);
            Assert.IsTrue(_body.Intent.Pressed(Verb.Skill1)); Assert.IsTrue(_body.Intent.Pressed(Verb.Skill2));
        }
        [Test] public void NormalMatchSelectionAndUltimateInputAreUnchanged()
        {
            GameLaunch.GuidedTutorial = false;
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(mode));
                Settings.SettingsStore.Current.CharacterPick = 2;
                for (int slot = 0; slot < Balance.PlayerCount; slot++)
                    Assert.AreEqual(MatchInstaller.ResolveAiCharacterIndex(slot, 2, mode), Pick(slot));
            }
            Press(Verb.Ultimate, true); Assert.IsTrue(_body.Intent.Pressed(Verb.Ultimate));
        }
        [Test] public void TutorialStudentUltimateInputIsNotSuppressed()
        {
            GameLaunch.GuidedTutorial = true; _body.IsBot = false;
            Press(Verb.Ultimate, true); Assert.IsTrue(_body.Intent.Pressed(Verb.Ultimate));
        }
    }
}
