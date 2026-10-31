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
                // ⚠️ One cell per hero: the fixed 3x4 grid drew three hatched cells for nine heroes,
                // which read as locked heroes and led nowhere (2026-10-19 rework).
                Assert.IsNull(selector.GetComponentsInChildren<HubButton>(true)
                        .FirstOrDefault(b => b.name == "Portrait" + people.Count),
                        "The grid has a cell with no hero behind it");
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

        /// <summary>The version stamped on the review pictures. Bump it with every layout pass, so a
        /// chat client that caches by filename never shows the previous picture (`CLAUDE.md` § 6.1).</summary>
        private const string ReviewVersion = "v8";

        /// <summary>
        /// The pictures a character select change is judged on: the timed screen as a queue match
        /// meets it, at every brief shape plus ultrawide and the small window, a locked-in state,
        /// Larger text, and the untimed Classic route. Saved to `Logs/shots-native-ui`.
        /// </summary>
        [UnityTest, Timeout(240000)]
        public IEnumerator CharacterSelectionReviewCaptures()
        {
            Settings.SettingsStore.Current.LargerText = false;
            Settings.SettingsStore.Current.HighContrastHud = false;
            // Bots in the empty seats, as the owner's queue screenshot had them.
            var withBots = CustomGameRules.Defaults(GameMode.HeroStrike); withBots.Bots = CustomGameRules.MaxBots;
            SceneFlow.PinSelectedRules(withBots);
            yield return HubFlowTests.OpenHome();
            var people = Roster.GetPeople(GameMode.HeroStrike);
            int nemu = Enumerable.Range(0, people.Count).First(i => people[i].Id == "nemu");
            Settings.SettingsStore.Current.CharacterPick = nemu;
            var timed = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = true);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return HubFlowTests.Press("Portrait" + nemu);
            yield return HubFlowTests.Press("SelectionAbility0");
            var shapes = new[]
            {
                new Vector2Int(1600, 680), new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
                new Vector2Int(960, 540), new Vector2Int(1280, 960), new Vector2Int(2560, 1080),
                new Vector2Int(877, 482),
            };
            foreach (var size in shapes)
            {
                // 877x482 is the owner's small window and sits under the capture's reading floor
                // (13.95 px), so it is photographed without the bounds pass the brief shapes get.
                bool brief = size.x >= 960;
                string where = $"nemu/{size.x}x{size.y}";
                yield return TumpUiCapture.Capture($"CharSelect-{ReviewVersion}-nemu-{size.x}x{size.y}",
                    TumpHub.Current.Canvas, size.x, size.y, false, checkActionBounds: brief,
                    inspectViewport: () => AssertSelectionPicture(timed, where));
            }
            int dante = Enumerable.Range(0, people.Count).First(i => people[i].Id == "dante");
            yield return HubFlowTests.Press("Portrait" + dante);
            yield return HubFlowTests.Press("SelectionAbility1");
            // ⚠️ THE REPRODUCER: Dante's ATTACKING power at 1600x680 drew its cooldown over its
            // sentence in `CharSelect-v1-locked-1600x680.png` (the owner's screenshot showed the
            // same fault on NEMU at their window). `CrispUiText` has the cause.
            yield return TumpUiCapture.Capture($"CharSelect-{ReviewVersion}-dante-1600x680",
                TumpHub.Current.Canvas, 1600, 680, false, checkActionBounds: true,
                inspectViewport: () => AssertSelectionPicture(timed, "dante/1600x680"));
            yield return HubFlowTests.Press("SelectButton");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreSame(timed, TumpHub.Current.Top, "Offline lock-in stays on the selector.");
            yield return TumpUiCapture.Capture($"CharSelect-{ReviewVersion}-locked-1600x680",
                TumpHub.Current.Canvas, 1600, 680, false, checkActionBounds: true,
                inspectViewport: () => AssertSelectionPicture(timed, "locked/1600x680"));
            TumpHub.Current.Home();

            Settings.SettingsStore.Current.LargerText = true;
            Settings.SettingsStore.Current.HighContrastHud = true;
            var larger = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = true);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return HubFlowTests.Press("SelectionAbility0");
            yield return TumpUiCapture.Capture($"CharSelect-{ReviewVersion}-larger-1280x720",
                TumpHub.Current.Canvas, 1280, 720, false, checkActionBounds: true,
                inspectViewport: () => AssertSelectionPicture(larger, "larger/1280x720"));
            TumpHub.Current.Home();
            Settings.SettingsStore.Current.LargerText = false;
            Settings.SettingsStore.Current.HighContrastHud = false;

            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            Settings.SettingsStore.Current.CharacterPick = 1;
            var classic = TumpHub.Current.Push<HubCharacterSelect>(s => s.Timed = false);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return TumpUiCapture.Capture($"CharSelect-{ReviewVersion}-classic-1600x680",
                TumpHub.Current.Canvas, 1600, 680, false, checkActionBounds: true,
                inspectViewport: () => AssertSelectionPicture(classic, "classic/1600x680"));
            yield return TumpUiCapture.Capture($"CharSelect-{ReviewVersion}-classic-1280x960",
                TumpHub.Current.Canvas, 1280, 960, false, checkActionBounds: true,
                inspectViewport: () => AssertSelectionPicture(classic, "classic/1280x960"));
            TumpHub.Current.Home();
        }

        /// <summary>
        /// The two faults the 2026-10-19 rework fixed, measured on the drawn picture rather than on
        /// the layout that claimed to be right: every ability line is DRAWN inside its own box and
        /// the boxes stack without overlapping, and the chalk ring sits on the feet the preview
        /// actually rendered.
        /// </summary>
        private static void AssertSelectionPicture(HubCharacterSelect selector, string where)
        {
            var labels = selector.GetComponentsInChildren<Text>();
            float previousBottom = float.PositiveInfinity;
            foreach (string name in new[] { "AbilityName", "AbilityMeta", "AbilityCost", "AbilitySummary" })
            {
                var text = labels.FirstOrDefault(t => t.name == name);
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                var rect = text.rectTransform.rect;
                var mesh = text.canvasRenderer.GetMesh();
                if (mesh != null && mesh.vertexCount > 0)
                {
                    Assert.GreaterOrEqual(mesh.bounds.min.y, rect.yMin - 3,
                        where + "/" + name + " draws below its own box, over the line under it");
                    Assert.LessOrEqual(mesh.bounds.max.y, rect.yMax + 3, where + "/" + name + " draws above its own box");
                }
                float top = text.rectTransform.anchoredPosition.y;
                Assert.LessOrEqual(top, previousBottom + 0.5f, where + "/" + name + " overlaps the line above it");
                previousBottom = top - text.rectTransform.sizeDelta.y;
            }

            var preview = selector.GetComponentInChildren<ModelPreview>();
            var ring = selector.GetComponentsInChildren<HubChalk>().FirstOrDefault(c => c.name == "ChalkCircle");
            Assert.IsNotNull(preview?.Target, where + " has no hero picture");
            Assert.IsNotNull(ring, where + " has no chalk circle");
            var stage = (RectTransform)ring.transform.parent;
            float ringCentre = (ring.rectTransform.anchoredPosition.y + ring.rectTransform.sizeDelta.y * 0.5f) / stage.rect.height;
            float feet = LowestDrawnRow(preview.Target) / (float)preview.Target.height;
            Debug.Log($"[CharacterSelectFloor] {where}: feet={feet:F3} ring={ringCentre:F3} stage={stage.rect.size}");
            Assert.GreaterOrEqual(feet, 0, where + " drew no hero");
            // The soles stand INSIDE the circle: the lowest drawn row (the toes) lies within the
            // ring's outline, front edge to back edge. v1 failed this by a head: Nemu's toes at
            // 0.26 of the stage, the ring's centre at 0.12 and its half height 0.06.
            float ringHalf = ring.rectTransform.sizeDelta.y * 0.5f / stage.rect.height;
            Assert.GreaterOrEqual(feet, ringCentre - ringHalf,
                where + " draws the hero's feet in front of the chalk circle");
            Assert.LessOrEqual(feet, ringCentre + ringHalf,
                where + " draws the hero's feet behind the chalk circle, floating over it");
        }

        /// <summary>The lowest row of the preview with anything opaque in it: where the soles are drawn.</summary>
        private static int LowestDrawnRow(RenderTexture target)
        {
            var before = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                var colours = pixels.GetPixels32();
                for (int y = 0; y < target.height; y++)
                    for (int x = 0; x < target.width; x++)
                        if (colours[y * target.width + x].a > 40) return y;
                return -1;
            }
            finally { RenderTexture.active = before; Object.Destroy(pixels); }
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
            int lockedSlot = System.Array.IndexOf(EconomyRules.UnlockedHeroes, "zack");
            object oldCache = cacheField.GetValue(wallet);
            try
            {
                var localCache = System.Activator.CreateInstance(cacheField.FieldType, true);
                cacheField.FieldType.GetField("Known").SetValue(localCache, true);
                cacheField.SetValue(wallet, localCache);
                queryField.SetValue(net, null); // A LAN transport, with no hosted UGS query to update.
                relayField.SetValue(net, true);
                Assert.IsTrue(WalletStore.OnlineRoute);
                // Every hero is unlocked today, so this case locks one to keep the locked presentation covered.
                EconomyRules.UnlockedHeroes[lockedSlot] = "";
                Assert.IsTrue(HubOwnership.OwnsHero("dante"));
                Assert.IsFalse(HubOwnership.OwnsHero("zack"));

                yield return HubFlowTests.OpenRoomPicker();
                var selector = TumpHub.Current.Top as HubCharacterSelect;
                Assert.IsNotNull(selector,"The lobby's CHARACTER door did not open the selector.");
                var portraits = selector.GetComponentsInChildren<HubButton>(true);
                var owned = portraits.Single(b => b.name == "Portrait0");
                var unowned = portraits.Single(b => b.name == "Portrait3");
                var ownedFace = owned.GetComponentsInChildren<Image>().Last(i => i.name == "Face"); // The plate is also Face; the picture sits on it.
                var unownedFace = unowned.GetComponentsInChildren<Image>().Last(i => i.name == "Face"); // The plate is also Face; the picture sits on it.
                Assert.AreEqual(Color.white, ownedFace.color);
                Assert.IsTrue(unowned.IsInteractable());
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
                EconomyRules.UnlockedHeroes[lockedSlot] = "zack";
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
            // This fixture hosts one human. Queue acceptance does not override a
            // custom room's explicit NONE policy; request the needed bot seats.
            if(!queued)selectedRules.Bots=CustomGameRules.MaxBots;
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
            var cards = TumpHub.Current.Top.GetComponentsInChildren<UnityEngine.UI.RawImage>().Where(i=>i.name=="CourtImage").ToArray();
            Assert.AreEqual(SceneFlow.MapRegistry.Length, cards.Length);
            foreach (var card in cards) Assert.IsNotNull(card.texture, card.transform.parent.name + " must show its actual court.");
            // Layout captures have their own qualified fixture. Do not consume
            // this real twelve-second ballot while taking multiple screenshots.
            yield return HubFlowTests.Press("VoteMap1");
            yield return HubFlowTests.Press("LockMapVote");
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
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "GO!" }, ticks);
            Assert.IsFalse(PresentationClock.Held);
            Assert.IsFalse(HubLoading.Visible);
        }

        // ⚠️⚠️ A CUSTOM ROOM STARTS WITH WHOEVER IS IN IT (owner 2026-10-17, `eec7970b0`), AND A SEAT
        // THE HOST EMPTIES OF ITS BOT HAS NO BODY IN THE MATCH (owner 2026-10-19, `40b7e5d9c`). This
        // replaced `CustomRoomWithBotsOffRequiresFourHumanSeats`, which still asserted the refusal the
        // first of those removed. It walks the lobby's own REMOVE BOT and START GAME presses on a real
        // LAN host into a running round; `CustomBotPolicyStartTests` covers the policy cases in isolation.
        [UnityTest, Timeout(90000)]
        public IEnumerator CustomRoomWithEveryBotRemovedStartsWithOnlyTheHostsSeatInTheMatch()
        {
            Settings.SettingsStore.Current.HighContrastHud = false;
            Settings.SettingsStore.Current.LargerText = false;
            // ⚠️ HOME'S CARD READS CLASSIC AND THE ROOM IS HERO STRIKE, ON PURPOSE. Entering the lobby
            // passes through HOME, whose refresh used to reset the room to the card's defaults: the
            // mode flipped and every bot was gone (`HubHome.ApplyChoice`, UX-1.5a).
            HubHome.Choice = 1;
            yield return HubFlowTests.OpenHome();
            var hosting = TumpHub.Current.Host.HostRoom("NO BOT ADMISSION", SceneFlow.Eskinita,
                GameMode.HeroStrike, RoomVisibility.Private, false);
            float until = Time.realtimeSinceStartup + 15;
            while (!hosting.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(hosting.IsCompleted && !hosting.IsFaulted && string.IsNullOrEmpty(hosting.Result));
            until = Time.realtimeSinceStartup + 5;
            while (!(TumpHub.Current.Top is HubLobby) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
            Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode, "Passing through HOME changed the room's mode.");

            // A new room opens with a bot in every empty seat (`HostRoom`); the host takes all three out.
            int host = NetAuthority.LocalSlot;
            until = Time.realtimeSinceStartup + 5;
            while (TumpHub.Current.Host.Seats().Any(s => s.Slot != host && !s.Bot) && Time.realtimeSinceStartup < until) yield return null;
            var opened = TumpHub.Current.Host.Seats();
            Assert.IsTrue(opened.All(s => s.Slot == host || s.Bot),
                "A new room must open with a bot in every empty seat. Seats " +
                string.Join(", ", opened.Select(s => s.Slot + (s.Occupied ? "=person" : s.Bot ? "=bot" : "=open"))) +
                "; host seat " + host + ", Bots " + SceneFlow.SelectedRules.Bots + ", BotSeats " +
                SceneFlow.SelectedRules.BotSeats + ", pinned " + SceneFlow.RulesPinned + ", can edit " +
                TumpHub.Current.Host.CanEditBots);
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                if (seat != host) yield return HubFlowTests.Press("RemoveBot" + seat);
            Assert.AreEqual(0, SceneFlow.SelectedRules.Bots, "Removing the last bot must switch the room's bots off.");
            foreach (var seat in TumpHub.Current.Host.Seats())
                if (seat.Slot != host) Assert.IsFalse(seat.Occupied || seat.Bot, "P" + (seat.Slot + 1) + " must read OPEN.");

            // The host chooses the court, so no ballot stands between selection and the arena.
            var rules = SceneFlow.SelectedRules.Clone(); rules.MapVote = false;
            MatchRpc.Instance.SelectRulesServerRpc(CustomGameRules.ToWire(rules));
            // A stale accepted-bot policy must not refill the chairs the room emptied.
            TumpHub.Current.Host.AcceptBots();
            Assert.IsTrue(AIController.BotsEnabled);
            yield return HubFlowTests.Press("StartGame");
            until = Time.realtimeSinceStartup + 6;
            while (!(TumpHub.Current.Top is HubCharacterSelect) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top, "START GAME must open selection with three empty seats.");
            Assert.IsTrue(MatchRpc.Instance.CharacterSelecting);
            Assert.IsFalse(MatchRpc.Instance.QueueMapVoting);
            Assert.IsFalse(AIController.BotsEnabled, "The room's NONE must retire the stale policy at START.");

            yield return HubFlowTests.Press("SelectButton");
            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != SceneFlow.Eskinita && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(SceneFlow.Eskinita, SceneManager.GetActiveScene().name);
            until = Time.realtimeSinceStartup + 20;
            while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive, "A host on their own must be able to play the court.");

            var lobby = NetSession.Instance.Lobby;
            Assert.IsTrue(lobby.MatchInProgress);
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                if (seat == host)
                {
                    Assert.IsNotNull(GameServices.Round.PlayerAt(seat), "The host's own seat has no body.");
                    continue;
                }
                Assert.IsNull(GameServices.Round.PlayerAt(seat),
                    "P" + (seat + 1) + " has no player and no bot, so it must have no body.");
                Assert.IsTrue(lobby.IsSeatBenched(seat),
                    "P" + (seat + 1) + " has no body, so a mid-match arrival must watch rather than sit in it.");
            }
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
                var arrival = Object.FindFirstObjectByType<MatchArrivalPresentation>();
                var beat = arrival != null ? typeof(MatchArrivalPresentation).GetField("_shownBeat",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) : null;
                var phase = new ArrivalCapturePhase
                {
                    frame = index, realtime = Time.realtimeSinceStartup, unityFrame = Time.frameCount,
                    loadingVisible = HubLoading.Visible, loadingPreparing = HubLoading.Preparing,
                    clockHeld = PresentationClock.Held, roundActive = GameServices.Round?.RoundActive == true,
                    arrivalPresent = arrival != null, beat = beat != null ? (int)beat.GetValue(arrival) : -99,
                    position = camera.transform.position, rotation = camera.transform.rotation,
                    fieldOfView = camera.fieldOfView
                };
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, $"arrival-{index:00}.json"), JsonUtility.ToJson(phase, true));
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image);
            }
        }

        [System.Serializable] private sealed class ArrivalCapturePhase
        {
            public int frame, unityFrame, beat;
            public float realtime, fieldOfView;
            public bool loadingVisible, loadingPreparing, clockHeld, roundActive, arrivalPresent;
            public Vector3 position;
            public Quaternion rotation;
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
