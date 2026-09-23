using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Abilities;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class MatchArrivalFlowTests
    {
        private string _settings;
        private CustomRules _rules;
        private bool _pinned;

        [UnityTest, Timeout(180000)]
        public IEnumerator CharacterSelectionExplainsItsKitWithoutPausingTheClock()
        {
            foreach (bool large in new[] { false, true })
            {
                Settings.SettingsStore.Current.LargerText = large;
                Settings.SettingsStore.Current.HighContrastHud = large;
                SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return HubFlowTests.OpenHome();
                // Entry/routing is covered by HubFlowTests. This exercises the inline Learn
                // layer for every selectable hero without a live selection deadline expiring.
                var selector = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = false);
                var people = Roster.GetPeople(GameMode.HeroStrike);
                for (int hero = 0; hero < people.Count; hero++)
                {
                    yield return HubFlowTests.Press("Portrait" + hero);
                    var kit = HeroAbilitySystem.CreateKitFor(people[hero].Id);
                    HeroAbility[] abilities = { kit.Skill1, kit.Skill2, kit.Ultimate };
                    for (int slot = 0; slot < abilities.Length; slot++)
                    {
                        yield return HubFlowTests.Press("SelectionAbility" + slot);
                        Assert.AreSame(selector, TumpHub.Current.Top, "Skill inspection must stay inline.");
                        var settings = Settings.SettingsStore.Current;
                        var variant = slot < 2 ? HeroBuildRules.Equipped(HeroBuildRules.RowFor(settings.HeroBuilds,
                            people[hero].Id), people[hero].Id, slot + 1, settings.AbilityChallenges) : null;
                        bool alternate = variant != null && !variant.IsDefault;
                        var labels = selector.GetComponentsInChildren<Text>();
                        Assert.AreEqual(alternate ? variant.Description : abilities[slot].Summary,
                            labels.Single(t => t.name == "AbilitySummary").text);
                        Assert.AreEqual((alternate ? variant.Name : abilities[slot].Name).ToUpperInvariant(),
                            labels.Single(t => t.name == "AbilityName").text);
                        StringAssert.Contains(AbilityIcons.LabelFor(abilities[slot].Glyph),
                            labels.Single(t => t.name == "AbilityMeta").text);
                        Canvas.ForceUpdateCanvases();
                        foreach (var label in labels.Where(t => t.name.StartsWith("Ability")))
                        {
                            Assert.GreaterOrEqual(label.fontSize, HubStyle.Floor);
                            Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 3,
                                people[hero].Id + "/" + slot + "/" + label.name);
                        }
                    }
                }
                yield return HubFlowTests.Shots(large ? "CharacterSelect-learn-large" : "CharacterSelect-learn");
                TumpHub.Current.Home();
            }
            var timed = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = true);
            yield return null;
            var clock = timed.GetComponentsInChildren<Text>().Single(t => t.name == "Clock");
            int before = int.Parse(clock.text);
            yield return HubFlowTests.Press("SelectionAbility1");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreSame(timed, TumpHub.Current.Top);
            Assert.Less(int.Parse(clock.text), before, "Reading a skill must not hold the selection timer.");
            TumpHub.Current.Home();
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            var classic = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = false);
            Assert.IsNull(classic.transform.Find("SelectionAbilities"), "Classic stays neutral and has no hero kit.");
        }

        [UnitySetUp] public IEnumerator Before()
        {
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            NetSession.Instance?.Stop(); HubQueueWatch.End();
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            NetSession.Instance?.Stop(); HubQueueWatch.End(); SceneFlow.Networked = false;
            yield return PlayModeWorld.Reset();
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Settings.SettingsStore.Save();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator QueuedLockInVotesThenLoadsIntroducesAndStartsWithoutReadyInput()
        {
            HubHome.Choice = 2;
            Settings.SettingsStore.Current.HighContrastHud = false;
            Settings.SettingsStore.Current.LargerText = false;
            yield return HubFlowTests.OpenHome();
            var hosting = TumpHub.Current.Host.HostRoom("ARRIVAL CHECK", SceneFlow.Eskinita,
                GameMode.HeroStrike, RoomVisibility.Private, false);
            float until = Time.realtimeSinceStartup + 15;
            while (!hosting.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(hosting.IsCompleted && !hosting.IsFaulted && string.IsNullOrEmpty(hosting.Result));

            // This is the local queued-room state path on a real LAN host, not a UGS matchmaking claim.
            HubQueueWatch.Begin(GameMode.HeroStrike, QueueStake.Casual);
            HubQueueWatch.AcceptBots(); TumpHub.Current.Host.AcceptBots();
            until = Time.realtimeSinceStartup + 6;
            while (!(TumpHub.Current.Top is HubCharacterSelect) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top);
            yield return HubFlowTests.Press("SelectButton");
            until = Time.realtimeSinceStartup + 5;
            while (!(TumpHub.Current.Top is HubMapVote) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubMapVote>(TumpHub.Current.Top);
            Assert.IsFalse(SceneFlow.SelectedRules.ManualReady);
            var cards = TumpHub.Current.Top.GetComponentsInChildren<UnityEngine.UI.RawImage>();
            Assert.AreEqual(SceneFlow.MapRegistry.Length, cards.Length);
            foreach (var card in cards) Assert.IsNotNull(card.texture, card.transform.parent.name + " must show its actual court.");
            yield return HubFlowTests.Shots("MapVote");
            yield return HubFlowTests.Press("VoteMap1");
            Assert.AreEqual(1, TumpHub.Current.Host.MapVoteFor(NetAuthority.LocalSlot));

            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != SceneFlow.BayanPlaza && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(SceneFlow.BayanPlaza, SceneManager.GetActiveScene().name);
            until = Time.realtimeSinceStartup + 10;
            while (Object.FindFirstObjectByType<MatchArrivalPresentation>() == null && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<MatchArrivalPresentation>());
            var gate = Object.FindFirstObjectByType<ReadyGate>();
            Assert.IsNotNull(gate);
            var ticks = new List<string>();
            gate.CountdownTick += ticks.Add;
            until = Time.realtimeSinceStartup + 20;
            while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < until) yield return null;
            gate.CountdownTick -= ticks.Add;
            Assert.IsTrue(GameServices.Round.RoundActive, "No key was pressed: the queued match must start itself.");
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "START!" }, ticks);
            Assert.IsFalse(PresentationClock.Held);
            Assert.IsFalse(HubLoading.Visible);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ManualCustomRoomWaitsAndItsOptionCanBeChanged()
        {
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("MenuButton");
            yield return HubFlowTests.Press("MenuMATCHRULES");
            var rules = Object.FindFirstObjectByType<CustomGameScreen>();
            Assert.IsNotNull(rules);
            var buttons = GameObject.Find("OwnerCustomGameCanvas").GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var b in buttons) if (b.name == "RoomRulesTab") b.onClick.Invoke();
            yield return null;
            bool before = SceneFlow.SelectedRules.ManualReady;
            var toggle = System.Array.Find(buttons, b => b.name == "ManualReadyNext");
            Assert.IsNotNull(toggle); Assert.IsTrue(toggle.IsInteractable());
            toggle.onClick.Invoke(); yield return null;
            Assert.AreEqual(!before, SceneFlow.SelectedRules.ManualReady);
            rules.Close();
            var manual = CustomGameRules.Defaults(GameMode.Classic); manual.ManualReady = true;
            SceneFlow.PinSelectedRules(manual); SceneFlow.Networked = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(2);
            var gate = Object.FindFirstObjectByType<ReadyGate>();
            Assert.IsNotNull(gate); Assert.IsTrue(gate.AwaitingReady);
            Assert.IsFalse(gate.CountingDown);
            Assert.IsFalse(GameServices.Round.RoundActive);
            Assert.IsNull(Object.FindFirstObjectByType<MatchArrivalPresentation>());
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LeavingDuringIntroductionReleasesCameraAndClock()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic); rules.ManualReady = false;
            SceneFlow.PinSelectedRules(rules); SceneFlow.Networked = false;
            Settings.SettingsStore.Current.ReducedUiMotion = true;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            float until = Time.realtimeSinceStartup + 10;
            while (!PresentationClock.Held && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(PresentationClock.Held);
            yield return new WaitForSecondsRealtime(.3f);
            var cam = Camera.main; var position = cam.transform.position; var rotation = cam.transform.rotation;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(Vector3.Distance(position, cam.transform.position), .001f, "Reduced motion must retain its stable shot.");
            Assert.Less(Quaternion.Angle(rotation, cam.transform.rotation), .01f);
            yield return PlayModeWorld.Reset();
            Assert.IsFalse(PresentationClock.Held, "Leaving before START must release the presentation hold.");
            Assert.IsNull(Object.FindFirstObjectByType<MatchArrivalPresentation>());
        }
    }
}
