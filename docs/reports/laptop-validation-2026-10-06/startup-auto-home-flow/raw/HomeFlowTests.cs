using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class HomeFlowTests
    {
        private bool _boot,_offered;
        private Settings.GameSettings _settings;
        [UnitySetUp] public IEnumerator Before()
        {
            _boot=SceneFlow.BootedThroughSplash;_offered=SceneFlow.LoginStepOffered;_settings=Settings.SettingsStore.Current;
            yield return PlayModeWorld.Reset();GameServices.Ensure();
            Settings.SettingsStore.OverrideForTests(new Settings.GameSettings{PlayerName="Startup flow guest",AccountHasPassword=false});
            SceneFlow.BootedThroughSplash=true;SceneFlow.LoginStepOffered=false;
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();Settings.SettingsStore.OverrideForTests(_settings);
            SceneFlow.BootedThroughSplash=_boot;SceneFlow.LoginStepOffered=_offered;
        }

        /// <summary>
        /// ⚠️⚠️ THIS FIXTURE WAS ASSERTING A SCREEN THE GAME HAD ALREADY STOPPED BUILDING.
        /// It looked for `HomeScreen`, `HomeCanvas` and `IllustratedBackdrop`, and
        /// `ConvertedMainMenu.Wire` has built `TumpHomeView` instead since the owner-painted
        /// pass; `HomeScreen.Install` is only reached from `WireLegacyReference`, which nothing
        /// calls. That is § 114 and § 124.11's fault for the third time: a fixture driving a
        /// control the game no longer makes. It asserts the screen that actually ships now.
        ///
        /// ⚠️ THE TITLE HAS ONE PRESS AND NO SETTINGS DOOR SINCE 2026-09-18. 🧑: *"MAIN menu is
        /// getting revamped it will lose all buttons and will just have a tap to play"*, and on
        /// where TUTORIAL, SETTINGS and QUIT should go, *"throw them away gang no need"*.
        /// `TumpNativeFrontEndTests` walks to settings and credits through the lobby.
        /// </summary>
        [UnityTest]
        public IEnumerator LoadingTitleKeepsSuppliedPaintingAndHasNoContinueTarget()
        {
            // Inspect the loading composition independently of the automatic
            // scene arrival so the control cannot disappear mid-capture.
            var root=new GameObject("Loading title art control");
            root.AddComponent<TumpHomeView>().Build(root.transform,null,null);
            yield return null;
            var canvas = GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
            Assert.IsNotNull(canvas);

            var buttons = canvas.GetComponentsInChildren<Button>().Where(b => b.isActiveAndEnabled).ToArray();
            Assert.IsEmpty(buttons,"The loading title must not ask for another click.");
            Assert.IsFalse(canvas.GetComponentsInChildren<Text>().Any(t=>t.name=="ContinuePrompt"));
            var progress=canvas.transform.Find("OwnerMainMenuComposition/ActualProgress").GetComponent<Image>();
            Assert.IsNotNull(progress);Assert.IsFalse(progress.raycastTarget);

            var picture=canvas.GetComponentsInChildren<RawImage>().Single(i=>i.name=="OwnerMainMenuBackground");
            Assert.AreEqual(OwnerMenuArt.Texture("main2-background"),picture.texture);
            Assert.AreEqual(1920,picture.texture.width);Assert.AreEqual(1080,picture.texture.height);
            Assert.AreEqual(Graphic.defaultGraphicMaterial,picture.material);
            Assert.IsNull(canvas.GetComponentInChildren<OwnerMenuAir>());
            Assert.IsNull(canvas.GetComponentInChildren<OwnerMenuLeaves>());
            Assert.IsNull(canvas.GetComponentInChildren<OwnerRoadDust>());

            yield return TumpUiCapture.Capture("Title-clean-1080p",canvas,1920,1080,false);
            yield return TumpUiCapture.Capture("Title-clean-720p",canvas,1280,720,false);
            yield return TumpUiCapture.Capture("Title-clean-shortwide",canvas,1920,820,false);
        }

        /// <summary>
        /// ⚠️⚠️ UX-1, 2026-09-23: TAP TO START OPENS HOME, AND THIS TEST CHANGED BECAUSE THE FLOW DID,
        /// BY THE OWNER'S DESIGN. It walked TAP TO START → LET'S PLAY → the preparation board's
        /// PROFILE and CHANGE LOADOUT doors; those screens are retired (kept on disk, reached by
        /// nothing). Its intent is unchanged and asserted on the doors that replaced them: the
        /// profile is one press from where the player lands (HOME's name plate), and so is the
        /// loadout (HOME's LOADOUT sticker), and each backs out to HOME. Every press is a real
        /// raycast, so a covered door fails here.
        /// </summary>
        [UnityTest]
        public IEnumerator LoginArrivesAtHomeWhoseDoorsReachProfileAndLoadout()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return PressWhen("GuestAccount");
            yield return WaitForScene(SceneFlow.MatchSetup);
            yield return WaitForButton("PlayButton");
            Assert.IsInstanceOf<UI.Hub.HubHome>(UI.Hub.TumpHub.Current.Top);
            yield return new WaitForSecondsRealtime(.6f);
            yield return PressWhen("NamePlate");
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHub>().IsOpen, "The name plate is the door to profile settings.");
            yield return PressWhen("ClosePlayerHub"); yield return null;
            yield return PressWhen("LoadoutButton");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsInstanceOf<UI.Hub.HubLoadout>(UI.Hub.TumpHub.Current.Top);
            Assert.IsNotNull(ActiveButton("TsinelasTab"));
            Assert.IsNotNull(ActiveButton("LataTab"));
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.name == "SkillsTab" && b.isActiveAndEnabled), "SKILLS moved to the SKILL TREE.");
            yield return UiRuntimeShots.Capture("Hub-loadout-from-home-v1", 1920, 1080);
            yield return PressWhen("BackButton"); yield return new WaitForSecondsRealtime(.3f);
            Assert.IsInstanceOf<UI.Hub.HubHome>(UI.Hub.TumpHub.Current.Top);
        }

        /// <summary>
        /// ⚠️ THE SAME RULE AS BEFORE, ON THE OWNER'S GAMEMODE SELECT: the ruleset (Classic or Hero
        /// Strike) is separate from the stakes, and there is no Classic ladder. RANKED is always Hero
        /// Strike; CLASSIC asks which game and is casual. Choosing a mode opens no room.
        /// </summary>
        [UnityTest]
        public IEnumerator ModeSelectSeparatesRulesFromStakesAndNeverPromisesAClassicLadder()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return PressWhen("StartButton");
            yield return WaitForScene(SceneFlow.MatchSetup);
            yield return WaitForButton("ModeCard");
            yield return new WaitForSecondsRealtime(.6f);
            yield return PressWhen("ModeCard");
            yield return new WaitForSecondsRealtime(.4f);
            foreach (var card in new[] { "PracticeCard", "CustomCard", "ClassicCard", "RankedCard" })
                Assert.IsNotNull(ActiveButton(card));
            yield return PressWhen("ClassicCard");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsNotNull(ActiveButton("ClassicChoice"));
            Assert.IsNotNull(ActiveButton("HeroStrikeChoice"));
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.name.Contains("Ranked") && b.isActiveAndEnabled && b.GetComponentInParent<UI.Hub.HubClassicPopup>() != null),
                "The casual popup never offers a ladder.");
            yield return PressWhen("HeroStrikeChoice");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(2, UI.Hub.HubHome.Choice);
            yield return PressWhen("ModeCard");
            yield return new WaitForSecondsRealtime(.4f);
            yield return PressWhen("RankedCard");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(0, UI.Hub.HubHome.Choice);
            Assert.AreEqual(Core.GameMode.HeroStrike, SceneFlow.SelectedMode, "Ranked is always Hero Strike.");
            Assert.IsFalse(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked,
                           "Choosing the ranked mode must not open a room.");
        }

        // A scene load is asynchronous, so asserting the scene name after a fixed sleep tests the
        // machine rather than the flow.
        private static IEnumerator WaitForScene(string name)
        {
            float end = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(name, SceneManager.GetActiveScene().name);
        }

        // `First` on a control that moved throws "Sequence contains no matching element", which
        // names neither the control nor the screen and cost this pass two runs to place.
        private static Button ActiveButton(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
            Assert.IsNotNull(button, name + " is not on screen. " + SceneManager.GetActiveScene().name +
                " has: " + string.Join(", ", Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Where(b => b.isActiveAndEnabled).Select(b => b.name).Distinct().OrderBy(n => n)));
            return button;
        }

        private static IEnumerator WaitForButton(string name)
        {
            float end = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < end && !Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.name == name && b.isActiveAndEnabled)) yield return null;
            Assert.IsNotNull(ActiveButton(name));
        }

        // A callback can pass while artwork or another canvas consumes every real press.
        // Exercise the top raycast target before dispatching the pointer event to it.
        // An empty raycast says nothing about WHY. Report the three things that produce one:
        // a control placed off the screen, a canvas whose raycaster is switched off, and a rect
        // with no size.
        private static string Where(Button button, Vector2 position)
        {
            var rect = (RectTransform)button.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var canvas = button.GetComponentInParent<Canvas>();
            var raycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            string casters = string.Join(", ", raycasters.Select(r => r.name + (r.enabled ? "+" : "-")));
            return Hierarchy(rect) + " at " + position + " screen " + Screen.width + "x" + Screen.height +
                " rect " + rect.rect.size + " corners " + corners[0] + ".." + corners[2] +
                " canvas " + canvas.name + " scale " + canvas.scaleFactor + " order " + canvas.sortingOrder +
                " raycasters [" + casters + "]";
        }

        private static string Hierarchy(Transform node)
        { string path=node.name;while(node.parent!=null){node=node.parent;path=node.name+"/"+path;}return path; }

        /// <summary>
        /// ⚠⚠ A SCREEN IS NOT HITTABLE ON THE FRAME IT IS BUILT, AND A FIXED WAIT IS A COIN
        /// FLIP. `Graphic.depth` stays -1 until the canvas has been batched for a render, and
        /// `GraphicRaycaster` skips a graphic with depth -1 however well its rect lines up, so a
        /// press that lands before the first render of a freshly loaded scene raycasts NOTHING AT
        /// ALL rather than hitting the wrong thing. That is what these three fixtures were dying
        /// of: the same run blamed `ProfileButton` one time and `ClassicButton` the next, with no
        /// code between them, because each `WaitForSecondsRealtime` was a guess at how long a
        /// scene load takes on the machine of the day.
        ///
        /// ⚠ WAIT FOR THE CONTROL TO BE HITTABLE, NOT FOR A NUMBER OF SECONDS. The press
        /// itself still asserts, so a control that is genuinely covered or genuinely missing
        /// still fails, and it fails with the same message it did before.
        /// </summary>
        private static IEnumerator PressWhen(string name)
        {
            float end = Time.realtimeSinceStartup + 10f;
            Button button = null;
            while (Time.realtimeSinceStartup < end)
            {
                Canvas.ForceUpdateCanvases();
                button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
                if (button != null && Hits(button).Count > 0) break;
                yield return null;
            }
            Assert.IsNotNull(button, name + " never appeared.");
            Press(button);
        }

        private static IEnumerator PressWhen(Button button)
        {
            float end = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < end && Hits(button).Count == 0) { Canvas.ForceUpdateCanvases(); yield return null; }
            Press(button);
        }

        private static List<RaycastResult> Hits(Button button)
        {
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits;
        }

        private static void Press(string name) => Press(ActiveButton(name));
        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();
            string name=button.name;
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = Hits(button);
            Assert.IsNotEmpty(hits, name + " has no raycast target. " + Where(button, position));
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(),
                name + " is covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator LoadingTipsStayInlineAndReadinessStillGatesTheTitle()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.Splash);
            float surfaceUntil=Time.realtimeSinceStartup+30;
            while(GameObject.Find("OwnerLoadingCanvas")==null&&Time.realtimeSinceStartup<surfaceUntil)yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<SplashScreen>());
            var loadingCanvas = GameObject.Find("OwnerLoadingCanvas").GetComponent<Canvas>();
            Assert.IsNotNull(loadingCanvas.GetComponentInChildren<LoadingArtwork>());
            Assert.IsTrue(loadingCanvas.GetComponentsInChildren<Text>().Any(t => t.name == "InlineLoadingTip" && !string.IsNullOrEmpty(t.text)));
            Assert.IsFalse(loadingCanvas.GetComponentsInChildren<Button>().Any(b => b.name == "LoadingStories"));
            Assert.IsNull(GameObject.Find("LoadingStoryRoot"), "Tips must not open a second screen or hold readiness.");
            float end = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().name != SceneFlow.MainMenu && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(SceneFlow.MainMenu, SceneManager.GetActiveScene().name);
            // ⚠⚠ BOOT REACHES HER LOGIN FIRST, AND THE TITLE IS BEHIND IT. This fixture
            // asserted `StartButton` the moment the menu scene became active and got the sign-in
            // screen, which is what a first-time player gets and is the whole point of `_atBoot`:
            // `BootGuest` records the answer and steps aside. The other three tests load
            // `MainMenu` directly and so never meet it. Walking the guest door is the flow a
            // player walks, so the loading test now ends where a player ends: on her street.
            yield return WaitForButton("GuestAccount");
            yield return PressWhen("GuestAccount");
            yield return WaitForScene(SceneFlow.MatchSetup);
            yield return WaitForButton("PlayButton");
            Assert.IsTrue(UI.Hub.TumpHub.Current.AtHome);
        }
    }
}
