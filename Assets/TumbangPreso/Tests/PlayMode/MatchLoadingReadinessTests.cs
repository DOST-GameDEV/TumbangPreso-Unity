using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class MatchLoadingReadinessTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()
        { HubLoading.Cancel(); yield return PlayModeWorld.Reset(); }

        private sealed class LoadingPeer : INetProvider
        {
            public bool Networked;
            public bool IsHost => true;
            public bool IsNetworked => Networked;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }

        [UnityTest]
        public IEnumerator LoadingDeckIsRetainedAndAnUnstartedArenaLoadCanBeCancelled()
        {
            yield return LoadingArtwork.Warmup();
            var cached = (Texture2D[])typeof(LoadingArtwork).GetField("Cached", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(3, cached.Length); Assert.IsTrue(cached.All(texture => texture != null));
            var retained = cached.ToArray();
            var warm = LoadingArtwork.Warmup();
            Assert.IsFalse(warm.MoveNext(), "A prepared loading deck scheduled fresh asset reads.");
            int loads = 0;
            void Loaded(Scene scene, LoadSceneMode mode) { loads++; }
            SceneManager.sceneLoaded += Loaded;
            var source = SceneManager.GetActiveScene();
            try
            {
                for (int index = 0; index < 3; index++)
                {
                    Assert.IsTrue(HubLoading.Begin(SceneFlow.Eskinita));
                    var owner = Object.FindFirstObjectByType<HubLoading>();
                    Assert.IsTrue(HubLoading.Begin(SceneFlow.Eskinita));
                    Assert.AreSame(owner, Object.FindFirstObjectByType<HubLoading>());
                    var artwork = Object.FindFirstObjectByType<LoadingArtwork>();
                    var front = artwork.GetComponentsInChildren<RawImage>().Single(i => i.name == "CurrentIllustration");
                    Assert.AreSame(retained[artwork.FrameIndex], front.texture);
                    HubLoading.Cancel();
                    yield return null;
                    Assert.AreEqual(source, SceneManager.GetActiveScene()); Assert.AreEqual(0, loads);
                }
            }
            finally { SceneManager.sceneLoaded -= Loaded; HubLoading.Cancel(); }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator OfflineAndNetworkArenaEntryShareOneAsynchronousLoadingOwner()
        {
            var provider = NetAuthority.Provider;
            bool networked = SceneFlow.Networked, pinned = SceneFlow.RulesPinned;
            var rules = SceneFlow.SelectedRules.Clone();
            var launch = typeof(GameLaunch).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => !field.IsInitOnly).ToDictionary(field => field, field => field.GetValue(null));
            var seats = new System.Collections.Generic.Dictionary<int, string>(GameLaunch.SeatTokens);
            int loads = 0;
            void Loaded(Scene scene, LoadSceneMode mode) { if (scene.name == SceneFlow.Eskinita) loads++; }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                SceneFlow.PinSelectedRules(Core.CustomGameRules.Defaults(Core.GameMode.Classic));
                foreach (bool online in new[] { false, true })
                {
                    GameLaunch.PendingAction = ""; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
                    GameLaunch.GuidedTutorial = false; GameLaunch.TrainingRange = false;
                    SceneFlow.Networked = online;
                    NetAuthority.Provider = new LoadingPeer { Networked = online };
                    var source = SceneManager.GetActiveScene(); int before = loads;
                    SceneFlow.Go(SceneFlow.Eskinita);
                    var owner = Object.FindFirstObjectByType<HubLoading>(); Assert.IsNotNull(owner);
                    Assert.IsTrue((bool)typeof(HubLoading).GetField("_ownsSceneLoad", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner));
                    Assert.AreEqual(source, SceneManager.GetActiveScene()); Assert.AreEqual(before, loads);
                    yield return null;
                    SceneFlow.Go(SceneFlow.Eskinita);
                    Assert.AreSame(owner, Object.FindFirstObjectByType<HubLoading>());
                    float deadline = Time.realtimeSinceStartup + 70;
                    while (HubLoading.Visible && Time.realtimeSinceStartup < deadline)
                    {
                        Assert.IsTrue(owner == null || string.IsNullOrEmpty(owner.FailureReason), owner != null ? owner.FailureReason : "");
                        yield return null;
                    }
                    Assert.IsFalse(HubLoading.Visible); Assert.AreEqual(before + 1, loads);
                    var destination = SceneManager.GetActiveScene();
                    Assert.AreEqual(SceneFlow.Eskinita, destination.name); Assert.AreNotEqual(source, destination);
                    var installer = Object.FindObjectsByType<MatchInstaller>(FindObjectsSortMode.None)
                        .Single(i => i.gameObject.scene == destination);
                    Assert.IsTrue(installer.IsPrepared); Assert.IsNotNull(Hud.Instance);
                }
            }
            finally
            {
                SceneManager.sceneLoaded -= Loaded; HubLoading.Cancel(); NetAuthority.Provider = provider;
                SceneFlow.Networked = networked;
                foreach (var field in launch) field.Key.SetValue(null, field.Value);
                GameLaunch.SeatTokens.Clear();
                foreach (var seat in seats) GameLaunch.SeatTokens.Add(seat.Key, seat.Value);
                if (pinned) SceneFlow.PinSelectedRules(rules); else SceneFlow.UnpinSelectedRules();
            }
        }

        [Test]
        public void TransportCleanupCancelsWaitingArrivalAndDoesNotRestoreOldHitstopSpeed()
        {
            GameServices.Ensure();
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float scale = PresentationClock.RequestedScale;
            var loadingRoot = new GameObject("Transport cleanup loading"); loadingRoot.SetActive(false);
            var loading = loadingRoot.AddComponent<HubLoading>();
            var current = typeof(HubLoading).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = current.GetValue(null);
            var arrivalRoot = new GameObject("Transport cleanup arrival");
            var arrival = arrivalRoot.AddComponent<MatchArrivalPresentation>();
            var run = arrival.Run();
            var cleanup = typeof(Net.NetSession).GetMethod("EndTransportPresentation", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                current.SetValue(null, loading); PresentationClock.RequestScale(.5f);
                Assert.IsTrue(run.MoveNext()); Assert.IsTrue(run.MoveNext());
                Assert.IsTrue(PresentationClock.Held); Assert.AreEqual(0, Time.timeScale);
                cleanup.Invoke(null, null);
                Assert.IsFalse(PresentationClock.Held); Assert.AreEqual(1, Time.timeScale);
                Assert.IsFalse(run.MoveNext(), "Cancelled externally driven arrival kept waiting and could retake the camera.");

                Settings.SettingsStore.Current.ReducedEffects = false;
                PresentationClock.RequestScale(.5f); Hitstop.Trigger(.08f, .05f);
                Assert.IsTrue(Hitstop.Active);
                cleanup.Invoke(null, null); Hitstop.Step();
                Assert.IsFalse(Hitstop.Active); Assert.AreEqual(1, Time.timeScale);

                var half = HalftimePresentation.Ensure();
                typeof(HalftimePresentation).GetProperty("Active").SetValue(half, true);
                typeof(HalftimePresentation).GetProperty("IsHalftime").SetValue(half, true);
                typeof(PresentationClock).GetMethod("Hold", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                int round = GameServices.Match.RoundNumber;
                cleanup.Invoke(null, null);
                Assert.IsFalse(half.Active); Assert.IsFalse(PresentationClock.Held);
                Assert.AreEqual(1, Time.timeScale); Assert.AreEqual(round, GameServices.Match.RoundNumber);
            }
            finally
            {
                (run as System.IDisposable)?.Dispose(); arrival.Cancel();
                HalftimePresentation.Instance?.End(false); SharedUltimatePhase.Instance?.Cancel();
                current.SetValue(null, previous); Object.DestroyImmediate(arrivalRoot); Object.DestroyImmediate(loadingRoot);
                Hitstop.End(); PresentationClock.RequestScale(scale); Settings.SettingsStore.Current.ReducedEffects = reduced;
            }
        }

        private static CharacterMotor IntroductionActor(string hero, bool installModel)
        {
            GameServices.Ensure(); GameServices.Round.Clear();
            var owner = new GameObject("Introduction preparation actor");
            var actor = owner.AddComponent<CharacterMotor>(); actor.enabled = false; actor.PlayerSlot = 1;
            var visual = owner.AddComponent<Visual.CharacterVisual>(); visual.enabled = false;
            var modelRoot = new GameObject("Visual").transform; modelRoot.SetParent(owner.transform, false);
            visual.SetModelRoot(modelRoot);
            var abilities = owner.AddComponent<Abilities.HeroAbilitySystem>(); abilities.enabled = false; abilities.BindHero(hero);
            if (installModel)
            {
                var art = RosterBook.Load().FindPersonArt(hero);
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                owner.GetComponent<Visual.CharacterAnimator>().enabled = false;
            }
            GameServices.Round.Register(actor);
            return actor;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadingPreparationRetainsBothIntroductionVariantsBeforeArenaWork()
        {
            var actor = IntroductionActor("cheska", true);
            var kit = actor.AbilitySystem.Kit;
            kit.AddUltimateCharge(7); kit.Skill1.ApplyNetworkSnapshot(5, 0);
            var position = actor.transform.position;
            var phaseRoot = new GameObject("Waiting introduction view"); phaseRoot.SetActive(false);
            var phase = phaseRoot.AddComponent<SharedUltimatePhase>();
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            var commits = (System.Collections.Generic.List<UltimateCommit>)typeof(SharedUltimatePhase).GetField("_commits", hidden).GetValue(phase);
            commits.Add(new UltimateCommit(1, 1, position, Vector3.forward, Vector3.up, 0).WithIdentity(kit));
            var ready = typeof(SharedUltimatePhase).GetMethod("PreparePresentation", hidden);
            float progress = 0; int preparationTurns = 0;
            System.Action<float> report = done =>
            {
                Assert.GreaterOrEqual(done, progress); progress = done;
                if (done > .25f)
                {
                    Assert.IsNotNull(Visual.UltimateIntroductionCache.Find(actor, false));
                    Assert.IsNotNull(Visual.UltimateIntroductionCache.Find(actor, true));
                }
            };
            var work = (IEnumerator)typeof(HubLoading).GetMethod("PrepareMatchVisuals", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { report });
            try
            {
                while (work.MoveNext())
                {
                    if (Visual.UltimateIntroductionCache.Preparing)
                    {
                        preparationTurns++;
                        Assert.IsFalse((bool)ready.Invoke(phase, null), "Active presentation competed with loading preparation.");
                    }
                    yield return work.Current;
                }
                Assert.GreaterOrEqual(preparationTurns, 2);
                Assert.AreEqual(1, progress); Assert.IsFalse(Visual.UltimateIntroductionCache.Preparing);
                var empty = Visual.UltimateIntroductionCache.Find(actor, false);
                var held = Visual.UltimateIntroductionCache.Find(actor, true);
                Assert.IsNotNull(empty); Assert.IsNotNull(held); Assert.AreNotSame(empty, held);
                Assert.IsTrue((bool)ready.Invoke(phase, null));
                Assert.IsFalse(Visual.UltimateIntroductionCache.PrepareRound().MoveNext());
                Assert.AreSame(empty, Visual.UltimateIntroductionCache.Find(actor, false));
                Assert.AreSame(held, Visual.UltimateIntroductionCache.Find(actor, true));
                Assert.AreEqual(position, actor.transform.position);
                Assert.AreEqual(7, kit.UltimateCharge); Assert.AreEqual(5, kit.Skill1.CooldownRemaining);
                Assert.IsFalse(kit.Ultimate.ReservedForIntroduction);
            }
            finally { (work as System.IDisposable)?.Dispose(); Object.Destroy(phaseRoot); }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator UnsupportedIntroductionRigIsRememberedAndCancelledPreparationReleasesItsOwner()
        {
            var actor = IntroductionActor("dante", false);
            var visual = actor.GetComponent<Visual.CharacterVisual>();
            var source = new GameObject("UnsupportedIntroductionSource");
            var replacement = new GameObject("ReplacementIntroductionSource");
            var model = new GameObject("UnsupportedIntroductionModel"); model.transform.SetParent(actor.transform, false);
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Visual.CharacterVisual).GetField("_instance", hidden).SetValue(visual, model);
            var sourceProperty = typeof(Visual.CharacterVisual).GetProperty("SourceModel"); sourceProperty.SetValue(visual, source);
            var work = (IEnumerator)typeof(HubLoading).GetMethod("PrepareMatchVisuals", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { null });
            try
            {
                LogAssert.Expect(LogType.Warning, "[IntroductionPrewarm] No compatible introduction for dante on UnsupportedIntroductionSource.");
                Assert.IsTrue(work.MoveNext()); Assert.IsTrue(Visual.UltimateIntroductionCache.Preparing);
                int stages = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(t => t.name == "~IntroductionPrewarm");
                Assert.IsTrue(Visual.UltimateIntroductionCache.HasResult(actor, false));
                Assert.IsNull(Visual.UltimateIntroductionCache.Find(actor, false));
                Assert.IsFalse(Visual.UltimateIntroductionCache.WarmOne(actor));
                Assert.AreEqual(stages, Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(t => t.name == "~IntroductionPrewarm"));
                (work as System.IDisposable)?.Dispose();
                Assert.IsFalse(Visual.UltimateIntroductionCache.Preparing);
                sourceProperty.SetValue(visual, replacement);
                LogAssert.Expect(LogType.Warning, "[IntroductionPrewarm] No compatible introduction for dante on ReplacementIntroductionSource.");
                Assert.IsTrue(Visual.UltimateIntroductionCache.WarmOne(actor), "A different source inherited the old source's failure.");
                Assert.IsFalse(Visual.UltimateIntroductionCache.WarmOne(actor));
                yield return null;
            }
            finally
            {
                (work as System.IDisposable)?.Dispose();
                Object.Destroy(source); Object.Destroy(replacement);
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator OldRoundOrOldSceneCannotCompleteANewLoadingCurtain()
        {
            bool range = GameLaunch.TrainingRange;
            try
            {
                GameLaunch.TrainingRange = false;
                GameServices.Ensure();
                GameServices.Round.ApplySnapshot(90, true, 0);
                Assert.IsFalse(HubLoading.Begin(SceneFlow.Eskinita, true));
                // An empty temporary scene exercises loading ownership, not map assets.
                var destination = SceneManager.CreateScene(SceneFlow.Eskinita);
                SceneManager.SetActiveScene(destination);
                var root = new GameObject("Unprepared installation");
                var installer = root.AddComponent<MatchInstaller>(); installer.enabled = false;
                yield return null; yield return null;
                var loading = Object.FindFirstObjectByType<HubLoading>();
                var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Single(c => c.name == "TumpLoadingCanvas");
                var percent = canvas.GetComponentsInChildren<Text>().Single(t => t.name == "Percent");
                Assert.IsTrue(HubLoading.Visible); Assert.IsFalse(installer.IsPrepared);
                Assert.AreEqual("60%", percent.text, "A surviving global round bypassed this destination's setup.");
                typeof(MatchInstaller).GetProperty("InstallationError").SetValue(installer, "test installation failed");
                yield return null; yield return null;
                Assert.IsNotEmpty(loading.FailureReason);
                Assert.AreEqual("!", percent.text); Assert.IsTrue(HubLoading.Visible);
                var back = canvas.GetComponentsInChildren<Button>().Single(b => b.name == "LoadingReturn");
                Assert.IsTrue(back.interactable);
                Assert.AreEqual(back.gameObject, UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject);
                Assert.IsFalse(HubLoading.Begin(SceneFlow.MatchSetup, true));
                Assert.IsFalse(HubLoading.Visible); Assert.IsFalse(canvas.gameObject.activeSelf);
                yield return null;

                typeof(MatchInstaller).GetProperty("InstallationError").SetValue(installer, null);
                typeof(MatchInstaller).GetField("_installed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(installer, true);
                Assert.IsTrue(installer.IsPrepared);
                HubLoading.Begin(SceneFlow.Eskinita, true);
                yield return null; yield return null;
                canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(c => c.name == "TumpLoadingCanvas");
                percent = canvas.GetComponentsInChildren<Text>().Single(t => t.name == "Percent");
                Assert.AreEqual("0%", percent.text, "A deferred same-scene rematch adopted its predecessor's installer.");
                Assert.IsTrue(HubLoading.Visible);
            }
            finally { HubLoading.Cancel(); GameLaunch.TrainingRange = range; }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator CustomMapSwitchesReusePreparedScenesAndLooksAfterTheLoadingCurtain()
        {
            bool networked = SceneFlow.Networked, hubEnabled = ConvertedMatchSetup.HubEnabled;
            string selected = SceneFlow.SelectedMap;
            int loadsAfterReady = 0;
            void CountLoad(Scene scene, LoadSceneMode mode) { loadsAfterReady++; }
            try
            {
                SceneFlow.Networked = false; ConvertedMatchSetup.HubEnabled = true;
                TumpHub.PendingEntry = HubEntry.Home;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
                var controller = Object.FindFirstObjectByType<ConvertedMatchSetup>();
                Assert.IsNotNull(controller);
                float until = Time.realtimeSinceStartup + 150;
                bool sawCurtain = false;
                while (controller.Preview == null || !controller.Preview.IsPrepared || HubLoading.Visible)
                {
                    sawCurtain |= HubLoading.Visible;
                    var loading = Object.FindFirstObjectByType<HubLoading>();
                    if (loading != null) Assert.IsNull(loading.FailureReason, loading.FailureReason);
                    Assert.Less(Time.realtimeSinceStartup, until, "Custom previews did not finish behind loading.");
                    yield return null;
                }
                Assert.IsTrue(sawCurtain);
                Assert.IsFalse(MatchInstaller.PreviewOnly, "Preparation retained the match-install suppression gate.");
                var preview = controller.Preview;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var cache = (System.Collections.Generic.Dictionary<string, Scene>)typeof(MapPreviewSurface).GetField("_cache", flags).GetValue(preview);
                var looks = (System.Collections.Generic.Dictionary<string, Visual.WorldLookPresentation>)typeof(MapPreviewSurface).GetField("_looks", flags).GetValue(preview);
                Assert.AreEqual(SceneFlow.Maps.Length, cache.Count);
                Assert.AreEqual(SceneFlow.Maps.Length, looks.Count);
                var preparedScenes = cache.ToDictionary(x => x.Key, x => x.Value);
                var preparedLooks = looks.ToDictionary(x => x.Key, x => x.Value);
                TumpHub.Current.Push<HubHost>();
                yield return null;
                SceneManager.sceneLoaded += CountLoad;
                var timer = new System.Diagnostics.Stopwatch();
                for (int pass = 0; pass < 2; pass++)
                    foreach (string map in SceneFlow.Maps)
                    {
                        timer.Restart(); controller.SelectMap(map); timer.Stop();
                        Assert.AreEqual(map, preview.Showing, "A prepared custom selection still waits for scene setup.");
                        Assert.AreEqual(preparedScenes[map], cache[map]);
                        Assert.AreSame(preparedLooks[map], looks[map]);
                        Assert.AreSame(looks[map], Visual.WorldLookPresentation.Current);
                        Debug.Log(System.FormattableString.Invariant($"[CustomMapSwitch] pass={pass} map={map} selectMs={timer.Elapsed.TotalMilliseconds:F3}"));
                        yield return null;
                        if (pass == 0)
                        {
                            preview.Camera.Render();
                            var target = RenderTexture.GetTemporary(320, 180, 0, RenderTextureFormat.ARGB32);
                            var image = new Texture2D(320, 180, TextureFormat.RGB24, false);
                            var previous = RenderTexture.active;
                            try
                            {
                                Graphics.Blit(preview.Camera.targetTexture, target); RenderTexture.active = target;
                                image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply();
                                Assert.Greater(image.GetPixels32().Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count(), 64,
                                    "Prepared preview is blank or flat: " + map);
                                string directory = System.IO.Path.Combine(Application.dataPath, "../Logs/custom-preview-loading-20260927");
                                System.IO.Directory.CreateDirectory(directory);
                                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, map + ".png"), image.EncodeToPNG());
                            }
                            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.Destroy(image); }
                        }
                    }
                Assert.AreEqual(0, loadsAfterReady, "Changing a prepared map started another scene load.");
                Assert.AreEqual(SceneFlow.Maps.Length, looks.Count);
            }
            finally
            {
                SceneManager.sceneLoaded -= CountLoad;
                HubLoading.Cancel(); SceneFlow.Networked = networked;
                ConvertedMatchSetup.HubEnabled = hubEnabled; SceneFlow.SelectedMap = selected;
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator MenuTransitionsBlockPointerAndKeepOneCurtainThroughHubPreparation()
        {
            bool boot = SceneFlow.BootedThroughSplash, networked = SceneFlow.Networked;
            bool hubEnabled = ConvertedMatchSetup.HubEnabled;
            var priorScene = SceneManager.GetActiveScene();
            try
            {
                SceneFlow.BootedThroughSplash = false; SceneFlow.Networked = false;
                ConvertedMatchSetup.HubEnabled = true; TumpHub.PendingEntry = HubEntry.Home;
                SceneFlow.Go(SceneFlow.MainMenu);
                var loading = Object.FindFirstObjectByType<HubLoading>();
                Assert.IsNotNull(loading);
                Assert.AreEqual(priorScene, SceneManager.GetActiveScene(), "Scene work started before the curtain got a frame.");
                Assert.IsTrue(HubLoading.BeginMenu(SceneFlow.MainMenu));
                Assert.AreSame(loading, Object.FindFirstObjectByType<HubLoading>(), "Repeated navigation replaced the in-flight owner.");
                yield return null;
                Canvas.ForceUpdateCanvases();
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    { position = new Vector2(Screen.width * .5f, Screen.height * .5f) };
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                var loadingCanvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(x => x.name == "TumpLoadingCanvas");
                Assert.IsNotEmpty(hits, $"Loading rect={((RectTransform)loadingCanvas.transform).rect}, graphic depth={loadingCanvas.GetComponent<Image>().depth}, screen={Screen.width}x{Screen.height}");
                Assert.AreEqual("TumpLoadingCanvas", hits[0].gameObject.name, "Pointer input passes through the loading art.");
                float until = Time.realtimeSinceStartup + 150;
                while (HubLoading.Visible)
                {
                    Assert.IsNull(loading.FailureReason, loading.FailureReason);
                    Assert.Less(Time.realtimeSinceStartup, until);
                    yield return null;
                }
                var menu = Object.FindFirstObjectByType<ConvertedMainMenu>();
                Assert.IsNotNull(menu); Assert.IsTrue(menu.IsInitialized); Assert.IsTrue(menu.IsPrepared);
                Assert.IsNull(menu.InitializationError);

                SceneFlow.Go(SceneFlow.MatchSetup);
                loading = Object.FindFirstObjectByType<HubLoading>();
                Assert.IsNotNull(loading);
                int ownerId = loading.GetHashCode();
                until = Time.realtimeSinceStartup + 150;
                bool enteredHub = false;
                while (HubLoading.Visible)
                {
                    Assert.AreSame(loading, Object.FindFirstObjectByType<HubLoading>(), "Hub Wire replaced rather than adopted the transition curtain.");
                    Assert.IsNull(loading.FailureReason, loading.FailureReason);
                    Assert.Less(Time.realtimeSinceStartup, until);
                    enteredHub |= SceneManager.GetActiveScene().name == SceneFlow.MatchSetup;
                    yield return null;
                }
                var hub = Object.FindFirstObjectByType<ConvertedMatchSetup>();
                Assert.IsTrue(enteredHub); Assert.IsNotNull(hub); Assert.IsTrue(hub.IsInitialized);
                Assert.IsTrue(hub.Preview.IsPrepared); Assert.IsFalse(MatchInstaller.PreviewOnly);
                Debug.Log($"[MenuLoadingCheck] title initialized; hub curtain {ownerId} retained through preview readiness; pointer blocked.");
            }
            finally
            {
                HubLoading.Cancel(); SceneFlow.BootedThroughSplash = boot;
                SceneFlow.Networked = networked; ConvertedMatchSetup.HubEnabled = hubEnabled;
            }
        }
    }
}
