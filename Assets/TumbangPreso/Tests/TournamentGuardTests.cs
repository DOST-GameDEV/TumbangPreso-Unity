using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// A tournament match cannot start carrying a practice or developer switch.
    ///
    /// ⚠️⚠️ THIS IS THE HALF THAT CANNOT LIVE IN `Core.Tests`. `TournamentPreset` owns the list
    /// of names and is asserted there in about a millisecond; the switches themselves are Unity
    /// statics, so proving they are actually reachable, actually readable and actually cleared
    /// needs this assembly. Splitting it any other way would leave the list asserted and the
    /// values unchecked, which is a test of the documentation.
    ///
    /// ⚠️ THE FIXTURE RESTORES EVERY SWITCH IT TOUCHES. These are process-wide statics and this
    /// suite runs beside sixty others; a test that left `AllBots` set would hand the next fixture
    /// a four-bot match, which is § 126.8's whole finding committed on purpose.
    /// </summary>
    public class TournamentGuardTests
    {
        private bool _sandbox, _allBots, _spectator, _tutorial, _preview, _bots, _thumb, _replay;
        private CustomRules _rules;

        [SetUp]
        public void Remember()
        {
            _sandbox = PracticeSandbox.Wanted;
            _allBots = GameLaunch.AllBots;
            _spectator = GameLaunch.Spectator;
            _tutorial = GameLaunch.GuidedTutorial;
            _preview = MatchInstaller.PreviewOnly;
            _bots = AIController.BotsEnabled;
            _thumb = InputLayer.TouchHud.ForceVisible;
            _replay = CameraSystem.SpectatorCamera.ProbeReplayRequest;
            _rules = UI.SceneFlow.SelectedRules?.Clone();
        }

        [TearDown]
        public void Restore()
        {
            PracticeSandbox.Wanted = _sandbox;
            GameLaunch.AllBots = _allBots;
            GameLaunch.Spectator = _spectator;
            GameLaunch.GuidedTutorial = _tutorial;
            MatchInstaller.PreviewOnly = _preview;
            AIController.BotsEnabled = _bots;
            InputLayer.TouchHud.ForceVisible = _thumb;
            CameraSystem.SpectatorCamera.ProbeReplayRequest = _replay;
            if (_rules != null) UI.SceneFlow.SetSelectedRules(_rules);
        }

        [Test]
        public void EveryNamedModifierIsReadableByThisBuild()
        {
            // ⚠️⚠️ THE FAIL-CLOSED PATH IS THE POINT OF THIS TEST. `TournamentGuard.Read` answers
            // `known: false` for a name it has never heard of, so adding a row to the core's list
            // and forgetting the accessor produces an UNREADABLE reading rather than a quiet
            // "off". This asserts nobody has left one in that state.
            var unreadable = new List<string>();

            foreach (var r in TournamentGuard.LiveModifiers())
                if (!r.Known) unreadable.Add(r.Name);

            Assert.IsEmpty(unreadable,
                "these modifiers are named in TournamentPreset.Modifiers and TournamentGuard " +
                "cannot read them, so the tournament check silently skips them: " +
                string.Join(", ", unreadable));
        }

        [Test]
        public void TheGuardReadsEveryModifierTheCoreNames()
        {
            Assert.AreEqual(TournamentPreset.Modifiers.Length,
                            TournamentGuard.LiveModifiers().Count,
                            "the core names a different number of modifiers than the guard reads");
        }

        [Test]
        public void APracticeSwitchLeftOnIsRefusedByName()
        {
            PracticeSandbox.Wanted = true;

            string refusal = TournamentGuard.Refusal(TournamentPreset.Rules());

            Assert.IsNotEmpty(refusal, "a lit practice sandbox was not refused");
            StringAssert.Contains("PracticeSandbox.Wanted", refusal);
            StringAssert.Contains("ON", refusal);
        }

        [Test]
        public void EveryModifierIsRefusedWhenItIsWrong()
        {
            // One at a time, so a refusal that only ever notices the first switch is caught.
            foreach (var m in TournamentPreset.Modifiers)
            {
                ResetAll();

                bool safe = TournamentPreset.SafeValue(m.Name);
                SetByName(m.Name, !safe);

                string refusal = TournamentGuard.Refusal(TournamentPreset.Rules());
                StringAssert.Contains(m.Name, refusal,
                    $"{m.Name} set to {!safe} was not reported");
            }
        }

        [Test]
        public void ApplyClearsEverythingAndSaysWhatItCleared()
        {
            PracticeSandbox.Wanted = true;
            GameLaunch.AllBots = true;
            GameLaunch.Spectator = true;
            GameLaunch.GuidedTutorial = true;
            MatchInstaller.PreviewOnly = true;
            AIController.BotsEnabled = false;
            InputLayer.TouchHud.ForceVisible = true;
            CameraSystem.SpectatorCamera.ProbeReplayRequest = true;

            var changed = TournamentGuard.Apply();

            Assert.AreEqual(TournamentPreset.Modifiers.Length, changed.Count,
                            "Apply did not report clearing every switch that was wrong");

            Assert.IsEmpty(TournamentGuard.Refusal(),
                           "the machine is still not tournament ready after Apply");

            // And the rules half went through the real setter.
            Assert.AreEqual(GameMode.Classic, UI.SceneFlow.SelectedMode);
            Assert.AreEqual(4, UI.SceneFlow.SelectedRoundCount);
        }

        [Test]
        public void ApplyPinsTheRulesSoAScreenCannotRestoreOverThem()
        {
            // docs/TODO.md 143.18: ConvertedMatchSetup restores GameSettings.CustomRulesWire when
            // it opens, which is correct for a player returning to a lobby and wrong for a match
            // somebody configured on purpose. Without the pin, the screen an operator sets a
            // bracket match up in silently replaced the whole tournament rule set.
            ResetAll();
            UI.SceneFlow.UnpinSelectedRules();

            TournamentGuard.Apply();

            Assert.IsTrue(UI.SceneFlow.RulesPinned,
                          "Apply did not pin the rules, so match setup will restore over them");
        }

        [Test]
        public void ApplyDoesNotWriteTheTournamentPresetIntoThePlayersOwnPreference()
        {
            // A tournament preset is a fact about the room being run, not a preference this
            // player expressed. AdoptRemoteRules already draws that distinction for a rule set
            // arriving from a host, and one bracket match on a shared laptop must not replace
            // whatever that player last chose for their own custom games.
            ResetAll();

            var settings = Settings.SettingsStore.Current;
            Assume.That(settings, Is.Not.Null);

            settings.CustomRulesWire = "1|0|8|90|0|3|0|1|0";

            TournamentGuard.Apply();

            Assert.AreEqual("1|0|8|90|0|3|0|1|0", settings.CustomRulesWire,
                            "Apply overwrote the player's saved custom rules with the "
                            + "tournament preset");
        }

        [Test]
        public void AnExplicitChoiceReleasesThePin()
        {
            // The pin narrows the restore rather than removing it: a player who deliberately
            // sets a rule set has expressed the preference the restore exists to honour.
            ResetAll();
            TournamentGuard.Apply();
            Assume.That(UI.SceneFlow.RulesPinned, Is.True);

            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));

            Assert.IsFalse(UI.SceneFlow.RulesPinned,
                           "an explicit SetSelectedRules did not release the pin, so a player "
                           + "could not change the rules after a tournament match");
        }

        [Test]
        public void ApplyIsQuietWhenThereWasNothingToClear()
        {
            ResetAll();
            TournamentGuard.Apply();

            Assert.IsEmpty(TournamentGuard.Apply(),
                           "Apply reported changes on an already clean machine");
        }

        [Test]
        public void AHeroStrikeRuleSetIsNotATournamentMatch()
        {
            ResetAll();

            var rules = TournamentPreset.Rules();
            rules.Mode = GameMode.HeroStrike;
            rules.Rounds = 8;

            StringAssert.Contains("mode is HeroStrike", TournamentGuard.Refusal(rules));
        }

        [Test]
        public void TheReportNamesEverySwitchSoAnOperatorCanReadIt()
        {
            ResetAll();
            string report = TournamentGuard.Report();

            foreach (var m in TournamentPreset.Modifiers)
                StringAssert.Contains(m.Name, report);

            StringAssert.Contains("READY", report);
        }

        [Test]
        public void ThePracticeSandboxCannotBeActiveInANetworkedMatch()
        {
            // ⚠️⚠️ THE STALE CLAIM THIS SETTLES: a previous note warned of a
            // `const bool NoCooldowns = true` in `PracticeSandbox`. There is no such constant and
            // there never was on this branch. What is real is the guard, and it is asked EVERY
            // READ rather than latched, so a sandbox switched on offline stops answering true the
            // moment a session exists rather than needing anybody to clear it.
            var previous = NetAuthority.Provider;
            try
            {
                PracticeSandbox.Wanted = true;
                NetAuthority.Provider = new NetworkedProviderStub();

                Assert.IsFalse(PracticeSandbox.Allowed,
                               "the sandbox believes it is allowed in a networked match");
                Assert.IsFalse(PracticeSandbox.Active,
                               "the sandbox is ACTIVE in a networked match");
            }
            finally
            {
                NetAuthority.Provider = previous;
            }
        }

        private sealed class NetworkedProviderStub : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }

        private static void ResetAll()
        {
            PracticeSandbox.Wanted = false;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;
            GameLaunch.GuidedTutorial = false;
            MatchInstaller.PreviewOnly = false;
            AIController.BotsEnabled = true;
            InputLayer.TouchHud.ForceVisible = false;
            CameraSystem.SpectatorCamera.ProbeReplayRequest = false;
        }

        private static void SetByName(string name, bool value)
        {
            switch (name)
            {
                case "PracticeSandbox.Wanted": PracticeSandbox.Wanted = value; break;
                case "GameLaunch.AllBots": GameLaunch.AllBots = value; break;
                case "GameLaunch.Spectator": GameLaunch.Spectator = value; break;
                case "GameLaunch.GuidedTutorial": GameLaunch.GuidedTutorial = value; break;
                case "MatchInstaller.PreviewOnly": MatchInstaller.PreviewOnly = value; break;
                case "AIController.BotsEnabled": AIController.BotsEnabled = value; break;
                case "TouchHud.ForceVisible": InputLayer.TouchHud.ForceVisible = value; break;
                case "SpectatorCamera.ProbeReplayRequest":
                    CameraSystem.SpectatorCamera.ProbeReplayRequest = value; break;
                default:
                    Assert.Fail($"{name} is named in TournamentPreset.Modifiers and this test " +
                                $"cannot set it. Both switches need the row.");
                    break;
            }
        }
    }
}
