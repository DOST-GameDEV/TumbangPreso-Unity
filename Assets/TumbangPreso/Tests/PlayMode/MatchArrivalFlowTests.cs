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
                Assert.IsFalse(WalletStore.OnlineRoute);
                for (int index = 0; index < people.Count; index++)
                {
                    var portrait = selector.GetComponentsInChildren<HubButton>(true)
                        .Single(b => b.name == "Portrait" + index);
                    Assert.IsTrue(portrait.IsInteractable(), "Offline play keeps every hero available");
                    Assert.IsFalse(portrait.Hatched, "Offline portraits must not look locked");
                    Assert.AreEqual(Color.white, portrait.GetComponentsInChildren<Image>()
                        .Single(i => i.name == "Face").color,
                        "Offline portraits must keep their full image");
                }
                for (int index = people.Count; index < 12; index++)
                    Assert.IsFalse(selector.GetComponentsInChildren<HubButton>(true)
                        .Single(b => b.name == "Portrait" + index).IsInteractable(),
                        "Empty grid slots are not selectable heroes");
                for (int hero = 0; hero < people.Count; hero++)
                {
                    yield return HubFlowTests.Press("Portrait" + hero);
                    var kit = HeroAbilitySystem.CreateKitFor(people[hero].Id);
                    var slots = kit.ScreenSlots;
                    for (int slot = 0; slot < slots.Length; slot++)
                    {
                        var ability = slots[slot].Ability;
                        yield return HubFlowTests.Press("SelectionAbility" + slot);
                        Assert.AreSame(selector, TumpHub.Current.Top, "Skill inspection must stay inline.");
                        var settings = Settings.SettingsStore.Current;
                        var variant = HeroLoadoutRules.SidegradesOpen && slots[slot].LoadoutSlot > 0 ? HeroBuildRules.Equipped(HeroBuildRules.RowFor(settings.HeroBuilds,
                            people[hero].Id), people[hero].Id, slots[slot].LoadoutSlot, settings.AbilityChallenges) : null;
                        bool alternate = variant != null && !variant.IsDefault;
                        var labels = selector.GetComponentsInChildren<Text>();
                        Assert.AreEqual(alternate ? variant.Description : ability.Summary,
                            labels.Single(t => t.name == "AbilitySummary").text);
                        Assert.AreEqual((alternate ? variant.Name : ability.Name).ToUpperInvariant(),
                            labels.Single(t => t.name == "AbilityName").text);
                        StringAssert.Contains(AbilityIcons.LabelFor(ability.Glyph),
                            labels.Single(t => t.name == "AbilityMeta").text);
                        Canvas.ForceUpdateCanvases();
                        var button = selector.GetComponentsInChildren<HubButton>(true)
                            .Single(b => b.name == "SelectionAbility" + slot);
                        var symbol = button.GetComponentInChildren<TumpAbilitySymbol>();
                        Assert.AreEqual(ability.Glyph, symbol.Glyph);
                        Assert.Greater(symbol.canvasRenderer.GetMesh().vertexCount, 0,
                            people[hero].Id + "/" + slot + " rendered no skill symbol");
                        var illustration = AbilityIcons.Illustration(ability.Glyph);
                        if (illustration != null)
                            Assert.AreSame(illustration.texture, symbol.mainTexture,
                                people[hero].Id + "/" + slot + " retained another hero's skill texture");
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

        [UnityTest]
        public IEnumerator ClosedSidegradesShowTheAuthoredSkillDespiteASavedUnlockedAlternate()
        {
            Assert.IsFalse(HeroLoadoutRules.SidegradesOpen);
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            yield return HubFlowTests.OpenHome();

            var alternate = HeroLoadoutRules.VariantsFor("dante", 1).First(v => !v.IsDefault);
            var settings = Settings.SettingsStore.Current;
            settings.CharacterPick = 0;
            settings.HeroBuilds = new List<HeroBuild>
            {
                new HeroBuild { HeroId = "dante", Slot1VariantId = alternate.Id },
            };
            settings.AbilityChallenges = new List<AbilityChallengeProgress>
            {
                new AbilityChallengeProgress { VariantId = alternate.Id, Count = 999 },
            };
            Assert.AreEqual(alternate.Id, HeroBuildRules.Equipped(settings.HeroBuilds[0],
                "dante", 1, settings.AbilityChallenges).Id, "fixture must contain an unlocked stale alternate");

            var selector = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = false);
            yield return HubFlowTests.Press("SelectionAbility0");
            var labels = selector.GetComponentsInChildren<Text>();
            var authored = HeroAbilitySystem.CreateKitFor("dante").ScreenSlots[0].Ability;
            Assert.AreEqual(authored.Name.ToUpperInvariant(), labels.Single(t => t.name == "AbilityName").text);
            Assert.AreEqual(authored.Summary, labels.Single(t => t.name == "AbilitySummary").text);
            Assert.AreNotEqual(alternate.Name.ToUpperInvariant(), labels.Single(t => t.name == "AbilityName").text);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator OnlineUnownedHeroDimsButRemainsInspectableWithoutAServiceCall()
        {
            yield return HubFlowTests.OpenHome();
            Settings.SettingsStore.Current.CharacterPick = 0;
            var hosting = TumpHub.Current.Host.HostRoom("PORTRAIT CHECK", SceneFlow.Eskinita,
                GameMode.HeroStrike, RoomVisibility.Private, false);
            float until = Time.realtimeSinceStartup + 15;
            while (!hosting.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(hosting.IsCompleted && !hosting.IsFaulted && string.IsNullOrEmpty(hosting.Result),
                "The local LAN host did not open for the no-service selector check");
            until = Time.realtimeSinceStartup + 5;
            while (!(TumpHub.Current.Top is HubLobby) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top,
                "The local room must be visible before opening its character selector.");

            var net = NetSession.Instance;
            var wallet = GameServices.Wallet;
            Assert.IsNotNull(net); Assert.IsNotNull(wallet);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var relayField = typeof(NetSession).GetField("<IsRelay>k__BackingField", flags);
            var queryField = typeof(NetSession).GetField("<Query>k__BackingField", flags);
            var cacheField = typeof(WalletStore).GetField("_cache", flags);
            Assert.IsNotNull(relayField); Assert.IsNotNull(queryField); Assert.IsNotNull(cacheField);
            object oldRelay = relayField.GetValue(net), oldQuery = queryField.GetValue(net);
            object oldCache = cacheField.GetValue(wallet);
            try
            {
                var localCache = System.Activator.CreateInstance(cacheField.FieldType, true);
                cacheField.FieldType.GetField("Known").SetValue(localCache, true);
                cacheField.SetValue(wallet, localCache);
                queryField.SetValue(net, null); // A LAN transport, with no hosted UGS query to update.
                relayField.SetValue(net, true);
                Assert.IsTrue(WalletStore.OnlineRoute);
                Assert.IsTrue(HubOwnership.OwnsHero("dante"));
                Assert.IsFalse(HubOwnership.OwnsHero("zack"));

                yield return HubFlowTests.Press("CharacterDoor");
                var selector = TumpHub.Current.Top as HubCharacterSelect;
                Assert.IsNotNull(selector,"The lobby's CHARACTER door did not open the selector.");
                var portraits = selector.GetComponentsInChildren<HubButton>(true);
                var owned = portraits.Single(b => b.name == "Portrait0");
                var unowned = portraits.Single(b => b.name == "Portrait3");
                var ownedFace = owned.GetComponentsInChildren<Image>().Single(i => i.name == "Face");
                var unownedFace = unowned.GetComponentsInChildren<Image>().Single(i => i.name == "Face");
                Assert.IsFalse(owned.Hatched); Assert.AreEqual(Color.white, ownedFace.color);
                Assert.IsTrue(unowned.Hatched); Assert.IsTrue(unowned.IsInteractable());
                Assert.AreEqual(0.62f, unownedFace.color.r, 0.001f);
                Assert.AreEqual(0.62f, unownedFace.color.g, 0.001f);
                Assert.AreEqual(0.62f, unownedFace.color.b, 0.001f);
                Assert.AreEqual(1.0f, unownedFace.color.a, 0.001f);
                yield return TumpUiCapture.Capture("qa11-owned-hero", TumpHub.Current.Canvas, 1280, 720, false);

                yield return HubFlowTests.Press("Portrait3");
                Assert.AreEqual(Roster.HeroPeople[3].Name,
                    selector.GetComponentsInChildren<Text>().Single(t => t.name == "Heading").text);
                yield return HubFlowTests.Press("SelectionAbility0");
                Assert.AreEqual(HeroAbilitySystem.CreateKitFor("zack").Skill1.Name,
                    selector.GetComponentsInChildren<Text>().Single(t => t.name == "AbilityName").text);
                yield return TumpUiCapture.Capture("qa11-unowned-hero", TumpHub.Current.Canvas, 1280, 720, false);
                yield return HubFlowTests.Press("SelectButton");
                Assert.AreSame(selector, TumpHub.Current.Top,
                    "An unowned online hero may be inspected, but cannot be confirmed for play");
            }
            finally
            {
                relayField.SetValue(net, oldRelay);
                queryField.SetValue(net, oldQuery);
                cacheField.SetValue(wallet, oldCache);
            }
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
        public IEnumerator QueuedLockInVotesThenLoadsIntroducesAndStartsWithoutReadyInput() => StartFlow(true, true);

        [UnityTest, Timeout(90000)]
        public IEnumerator CustomStartSelectsVotesLoadsAndBeginsWithoutSecondReady() => StartFlow(false, true);

        [UnityTest, Timeout(90000)]
        public IEnumerator CustomHostSelectedCourtSkipsVotingAndBeginsWithoutSecondReady() => StartFlow(false, false);

        private IEnumerator StartFlow(bool queued, bool mapVote)
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

            var selectedRules = SceneFlow.SelectedRules.Clone(); selectedRules.MapVote = mapVote;
            MatchRpc.Instance.SelectRulesServerRpc(CustomGameRules.ToWire(selectedRules));

            // This is the local queued-room state path on a real LAN host, not a UGS matchmaking claim.
            if (queued)
            {
                HubQueueWatch.Begin(GameMode.HeroStrike, QueueStake.Casual);
                HubQueueWatch.AcceptBots(); TumpHub.Current.Host.AcceptBots();
            }
            else
            {
                TumpHub.Current.Host.AcceptBots();
                TumpHub.Current.Host.StartGame();
            }
            until = Time.realtimeSinceStartup + 6;
            while (!(TumpHub.Current.Top is HubCharacterSelect) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top);
            yield return HubFlowTests.Press("SelectButton");
            if (queued || mapVote)
            {
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

            }
            string expectedMap = queued || mapVote ? SceneFlow.BayanPlaza : SceneFlow.Eskinita;

            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != expectedMap && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(expectedMap, SceneManager.GetActiveScene().name);
            until = Time.realtimeSinceStartup + 10;
            while (Object.FindFirstObjectByType<MatchArrivalPresentation>() == null && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<MatchArrivalPresentation>());
            var gate = Object.FindFirstObjectByType<ReadyGate>();
            Assert.IsNotNull(gate);
            var ticks = new List<string>();
            gate.CountdownTick += ticks.Add;
            until = Time.realtimeSinceStartup + 20;
            string captureFolder = System.Environment.GetEnvironmentVariable("TUMP_ARRIVAL_CAPTURE");
            float nextCapture = 0; int captureIndex = 0;
            while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < until)
            {
                if (!string.IsNullOrEmpty(captureFolder) && Time.realtimeSinceStartup >= nextCapture &&
                    SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    CaptureArrivalCamera(captureFolder, captureIndex++);
                    nextCapture = Time.realtimeSinceStartup + .75f;
                }
                yield return null;
            }
            gate.CountdownTick -= ticks.Add;
            Assert.IsTrue(GameServices.Round.RoundActive, "No key was pressed: the queued match must start itself.");
            CollectionAssert.AreEqual(new[] { "5", "4", "3", "2", "1", "START!" }, ticks);
            Assert.IsFalse(PresentationClock.Held);
            Assert.IsFalse(HubLoading.Visible);
        }

        private static void CaptureArrivalCamera(string directory, int index)
        {
            var camera = Camera.main; if (camera == null) return;
            System.IO.Directory.CreateDirectory(directory);
            var target = RenderTexture.GetTemporary(640, 360, 24);
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var image = new Texture2D(640, 360, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); image.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, $"arrival-{index:00}.png"), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator CustomRoomHasNoSecondReadyOptionAndLegacyRulesStillIntroduceTheCourt()
        {
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("MenuButton");
            yield return HubFlowTests.Press("MenuMATCHRULES");
            var rules = Object.FindFirstObjectByType<CustomGameScreen>();
            Assert.IsNotNull(rules);
            var buttons = GameObject.Find("OwnerCustomGameCanvas").GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var b in buttons) if (b.name == "RoomRulesTab") b.onClick.Invoke();
            yield return null;
            Assert.IsNull(System.Array.Find(buttons, b => b.name == "ManualReadyNext"));
            rules.Close();
            var manual = CustomGameRules.Defaults(GameMode.Classic); manual.ManualReady = true;
            SceneFlow.PinSelectedRules(manual); SceneFlow.Networked = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(2);
            var gate = Object.FindFirstObjectByType<ReadyGate>();
            Assert.IsNotNull(gate); Assert.IsTrue(gate.AwaitingReady);
            Assert.IsFalse(gate.CountingDown);
            Assert.IsFalse(GameServices.Round.RoundActive);
            Assert.IsNotNull(Object.FindFirstObjectByType<MatchArrivalPresentation>());
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
