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
        public IEnumerator RosterCatalogueWarmupCoalescesAndHandsOffAfterCancellation()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var cached = typeof(RosterBook).GetField("_cached", flags);
            var tried = typeof(RosterBook).GetField("_tried", flags);
            var pending = typeof(RosterBook).GetField("_pending", flags);
            var oldCached = cached.GetValue(null); var oldTried = tried.GetValue(null); var oldPending = pending.GetValue(null);
            var first = default(System.Collections.IEnumerator); var second = default(System.Collections.IEnumerator);
            var cancelled = default(System.Collections.IEnumerator);
            void Clear() { cached.SetValue(null, null); tried.SetValue(null, false); pending.SetValue(null, null); }
            try
            {
                Clear(); first = RosterBook.Warmup(); second = RosterBook.Warmup();
                Assert.IsTrue(first.MoveNext()); Assert.IsTrue(second.MoveNext());
                Assert.IsInstanceOf<ResourceRequest>(first.Current); Assert.AreSame(first.Current, second.Current);
                yield return first.Current;
                Assert.IsFalse(first.MoveNext()); Assert.IsFalse(second.MoveNext());
                var book = RosterBook.Load(); Assert.IsNotNull(book);
                Assert.AreSame(book, cached.GetValue(null)); Assert.IsTrue((bool)tried.GetValue(null));
                Assert.IsNull(pending.GetValue(null));
                Assert.AreSame(book, Resources.Load<RosterBook>(RosterBook.ResourcePath));
                Assert.IsFalse(RosterBook.Warmup().MoveNext());
                Assert.AreEqual(Core.Roster.GetPeople(Core.GameMode.Classic)[0].Id,
                    book.PersonArt(0, Core.GameMode.Classic).Id);

                Clear(); cancelled = RosterBook.Warmup(); Assert.IsTrue(cancelled.MoveNext());
                var request = cancelled.Current;
                (cancelled as System.IDisposable)?.Dispose();
                yield return request;
                Assert.AreSame(book, RosterBook.Load()); Assert.IsNull(pending.GetValue(null));

                Clear(); tried.SetValue(null, true);
                Assert.IsNull(RosterBook.Load()); Assert.IsFalse(RosterBook.Warmup().MoveNext());
            }
            finally
            {
                (first as System.IDisposable)?.Dispose(); (second as System.IDisposable)?.Dispose();
                (cancelled as System.IDisposable)?.Dispose();
                cached.SetValue(null, oldCached); tried.SetValue(null, oldTried); pending.SetValue(null, oldPending);
            }
        }

        [UnityTest]
        public IEnumerator ViewmodelMeshWarmupRetainsSourceGeometryWithoutBuildingActors()
        {
            int actors = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length;
            int viewmodels = Object.FindObjectsByType<CameraSystem.ViewmodelArms>(FindObjectsSortMode.None).Length;
            float progress = 0;
            var warm = CameraSystem.ViewmodelMeshAssets.Warmup(RosterBook.Load(), done =>
            { Assert.GreaterOrEqual(done, progress); progress = done; });
            int requests = 0;
            while (warm.MoveNext())
            {
                Assert.IsInstanceOf<ResourceRequest>(warm.Current);
                requests++; yield return warm.Current;
            }
            Assert.Greater(requests, 0); Assert.AreEqual(1, progress);
            var cache = (System.Collections.Generic.Dictionary<string, Mesh>)typeof(CameraSystem.ViewmodelMeshAssets)
                .GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (string path in new[] { "Models/viewmodel_arm", "Models/tsinelas_classic",
                "Models/FppDetails/inday_left_arm", "Models/FppDetails/inday_right_arm",
                "Models/RosterArms/paete_left", "Models/RosterArms/paete_right",
                "Models/RosterArms/rafi_left", "Models/RosterArms/rafi_right" })
            {
                Assert.IsTrue(cache.TryGetValue(path, out var mesh) && mesh != null, path);
                Assert.AreSame(mesh, CameraSystem.ViewmodelMeshAssets.Load(path), path);
                var source = Resources.Load<Mesh>(path);
                Assert.AreNotSame(source, mesh, path);
                CollectionAssert.AreEqual(source.vertices, mesh.vertices, path);
                CollectionAssert.AreEqual(source.triangles, mesh.triangles, path);
            }
            var retained = cache.ToArray();
            yield return CameraSystem.ViewmodelMeshAssets.Warmup(RosterBook.Load());
            foreach (var pair in retained) Assert.AreSame(pair.Value, cache[pair.Key]);
            Assert.IsNull(CameraSystem.ViewmodelMeshAssets.Load("Models/RosterArms/not-a-roster-member_left"));
            Assert.IsNull(CameraSystem.ViewmodelMeshAssets.Load(null));
            Assert.AreEqual(actors, Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(viewmodels, Object.FindObjectsByType<CameraSystem.ViewmodelArms>(FindObjectsSortMode.None).Length);
            Debug.Log($"[ViewmodelMeshWarmup] retained={cache.Count} asyncRequests={requests}; no actors or viewmodels created.");
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

        /// <summary>
        /// ⚠️⚠️ THERE IS ONE LOADING SCREEN AT BOOT AND NONE ON THE WAY TO THE HUB. Menu hops and
        /// the hub's map previews used to raise `HubLoading` with a "GETTING READY" heading, a
        /// second loading screen straight after the splash. Requested 2026-09-30: removed, with
        /// the arenas' assets loaded by the splash instead. This asserts the curtain never
        /// appears on the title-to-hub journey and that the preview still arrives on its own.
        /// </summary>
        [UnityTest, Timeout(180000)]
        public IEnumerator MenuHopsAndTheHubOpenWithoutASecondLoadingScreen()
        {
            bool boot = SceneFlow.BootedThroughSplash, networked = SceneFlow.Networked;
            bool hubEnabled = ConvertedMatchSetup.HubEnabled;
            try
            {
                SceneFlow.BootedThroughSplash = false; SceneFlow.Networked = false;
                ConvertedMatchSetup.HubEnabled = true; TumpHub.PendingEntry = HubEntry.Home;
                SceneFlow.Go(SceneFlow.MainMenu);
                Assert.IsNull(Object.FindFirstObjectByType<HubLoading>(), "A menu hop raised a loading curtain.");
                float until = Time.realtimeSinceStartup + 60;
                ConvertedMainMenu menu = null;
                while (menu == null || !menu.IsInitialized)
                {
                    Assert.IsFalse(HubLoading.Visible, "A loading curtain covered the title screen.");
                    Assert.Less(Time.realtimeSinceStartup, until, "The title screen did not initialize.");
                    yield return null;
                    menu = SceneManager.GetActiveScene().name == SceneFlow.MainMenu
                        ? Object.FindFirstObjectByType<ConvertedMainMenu>() : null;
                }
                Assert.IsNull(menu.InitializationError);

                SceneFlow.Go(SceneFlow.MatchSetup);
                Assert.IsNull(Object.FindFirstObjectByType<HubLoading>(), "Entering the hub raised a loading curtain.");
                until = Time.realtimeSinceStartup + 150;
                ConvertedMatchSetup hub = null;
                while (hub == null || !hub.IsInitialized || hub.Preview == null ||
                       hub.Preview.Showing != SceneFlow.SelectedMap)
                {
                    Assert.IsFalse(HubLoading.Visible, "A loading curtain covered the hub or its map preview.");
                    Assert.Less(Time.realtimeSinceStartup, until, "The hub or its selected map preview did not arrive.");
                    yield return null;
                    hub = SceneManager.GetActiveScene().name == SceneFlow.MatchSetup
                        ? Object.FindFirstObjectByType<ConvertedMatchSetup>() : null;
                }
                Assert.IsNull(hub.InitializationError);
                Assert.IsFalse(MatchInstaller.PreviewOnly, "The preview load retained the match-install suppression gate.");
            }
            finally
            {
                SceneFlow.BootedThroughSplash = boot;
                SceneFlow.Networked = networked; ConvertedMatchSetup.HubEnabled = hubEnabled;
            }
        }

        /// <summary>
        /// Each arena is instanced the first time it is picked, with no curtain, and a second pass
        /// through every map reuses those instances without another scene load.
        /// </summary>
        [UnityTest, Timeout(180000)]
        public IEnumerator CustomMapSwitchesShowEachArenaWithoutALoadingCurtainAndReuseIt()
        {
            bool networked = SceneFlow.Networked, hubEnabled = ConvertedMatchSetup.HubEnabled;
            string selected = SceneFlow.SelectedMap;
            int loadsOnSecondPass = 0;
            bool counting = false;
            void CountLoad(Scene scene, LoadSceneMode mode) { if (counting) loadsOnSecondPass++; }
            try
            {
                SceneFlow.Networked = false; ConvertedMatchSetup.HubEnabled = true;
                TumpHub.PendingEntry = HubEntry.Home;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
                var controller = Object.FindFirstObjectByType<ConvertedMatchSetup>();
                Assert.IsNotNull(controller);
                // The hub builds itself after the scene load reports done; wait for it rather
                // than for a curtain, which no longer exists.
                float built = Time.realtimeSinceStartup + 30;
                while (TumpHub.Current == null || !controller.IsInitialized)
                {
                    Assert.Less(Time.realtimeSinceStartup, built, "The hub did not build.");
                    yield return null;
                }
                TumpHub.Current.Push<HubHost>();
                yield return null;
                SceneManager.sceneLoaded += CountLoad;
                for (int pass = 0; pass < 2; pass++)
                {
                    counting = pass == 1;
                    foreach (string map in SceneFlow.Maps)
                    {
                        controller.SelectMap(map);
                        var preview = controller.Preview;
                        Assert.IsNotNull(preview);
                        float until = Time.realtimeSinceStartup + 60;
                        while (preview.Showing != map)
                        {
                            Assert.IsFalse(HubLoading.Visible, "Picking a map raised a loading curtain: " + map);
                            Assert.Less(Time.realtimeSinceStartup, until, "The picked map never showed: " + map);
                            yield return null;
                        }
                        yield return null;
                        if (pass != 0) continue;
                        preview.Camera.Render();
                        var target = RenderTexture.GetTemporary(320, 180, 0, RenderTextureFormat.ARGB32);
                        var image = new Texture2D(320, 180, TextureFormat.RGB24, false);
                        var previous = RenderTexture.active;
                        try
                        {
                            Graphics.Blit(preview.Camera.targetTexture, target); RenderTexture.active = target;
                            image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply();
                            Assert.Greater(image.GetPixels32().Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count(), 64,
                                "Map preview is blank or flat: " + map);
                        }
                        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.Destroy(image); }
                    }
                }
                Assert.AreEqual(0, loadsOnSecondPass, "Revisiting an already shown map started another scene load.");
                Assert.IsFalse(MatchInstaller.PreviewOnly, "A preview load retained the match-install suppression gate.");
            }
            finally
            {
                SceneManager.sceneLoaded -= CountLoad;
                SceneFlow.Networked = networked;
                ConvertedMatchSetup.HubEnabled = hubEnabled; SceneFlow.SelectedMap = selected;
            }
        }
    }
}
