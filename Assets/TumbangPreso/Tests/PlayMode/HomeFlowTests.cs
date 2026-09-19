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
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

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
        public IEnumerator TitleIsOnePressAndKeepsHerStreetMoving()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(0.5f);
            var canvas = GameObject.Find("OwnerHomeCanvas").GetComponent<Canvas>();
            Assert.IsNotNull(canvas);

            var buttons = canvas.GetComponentsInChildren<Button>().Where(b => b.isActiveAndEnabled).ToArray();
            CollectionAssert.AreEquivalent(new[] { "StartButton" }, buttons.Select(b => b.name));

            var prompt = canvas.GetComponentsInChildren<Text>().Single(t => t.name == "ContinuePrompt");
            StringAssert.Contains("to continue", prompt.text);
            Assert.AreEqual(OwnerUiTheme.Current.Display, prompt.font,
                "Her caption is Darumadrop; Paalalabas is the caption face on the login.");

            var air = canvas.GetComponentInChildren<OwnerMenuAir>();
            Assert.IsNotNull(air, "The sky and the cast shadow are the title's only motion in the air.");
            Assert.IsNotNull(canvas.GetComponentInChildren<OwnerMenuLeaves>());
            Assert.IsNotNull(canvas.GetComponentInChildren<OwnerRoadDust>());

            yield return UiRuntimeShots.Capture("Home-street-v1",1920,1080);
            yield return UiRuntimeShots.Capture("Home-street-v1-720p",1280,720);
            yield return UiRuntimeShots.Capture("Home-street-v1-shortwide",1920,820);
        }

        [UnityTest]
        public IEnumerator PreparationRemainsReachableFromThePracticeLobby()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return new WaitForSecondsRealtime(.3f);
            yield return PressWhen("StartButton");
            yield return PressWhen("ClassicButton");
            yield return PressWhen("PracticeButton");
            yield return WaitForScene(SceneFlow.MatchSetup);
            yield return PressWhen("ProfileButton");
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHub>().IsOpen);
            // ⚠⚠ TWO MORE NAMES THIS FIXTURE HELD AND THE GAME NO LONGER BUILDS. The hub's
            // way out is `ClosePlayerHub` (`PlayerHub.OwnerPainted`), not `PlayerHub.cs`'s rail
            // footer `HubClose`; and preparation reaches the character/equipment picker through
            // `LoadoutButton`, "CHANGE LOADOUT", because `OpenLoadout` IS `OpenCharacterSelect`.
            // `CharacterButton` is the converted lobby's door and `ConvertedMatchSetup` still
            // wires it, so it compiles, builds and is simply never on screen. § 124.11.
            yield return PressWhen("ClosePlayerHub");yield return null;
            yield return PressWhen("LoadoutButton");
            // ⚠️⚠️ THE PICKER A PLAYER SEES IS `TumpPickerView`, NOT THE CONVERTED PANEL.
            // `OpenLoadout` still switches `CharacterSelectPanel` on and `ConvertedCharacterSelect`
            // still owns the choice, so asking the scene for that component finds it and proves
            // nothing: its authored controls (`BackButton`, `ConfirmButton` and the two arrows)
            // stay switched off, which is exactly what this fixture was reading when it reported
            // a picker with no way out. The owner-painted screen's door is `TumpBack`.
            yield return WaitForButton("TumpBack");
            var picker=Object.FindFirstObjectByType<TumpPickerView>();
            Assert.IsNotNull(picker,"Preparation must still expose the real character/equipment picker.");
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(button=>button.name=="CustomDoor" && button.isActiveAndEnabled));
            yield return UiRuntimeShots.Capture("Picker-from-lobby-brand-v1",1920,1080);
            yield return PressWhen("TumpBack");yield return null;
            Assert.IsNotNull(ActiveButton("LoadoutButton"));
        }

        [UnityTest]
        public IEnumerator PlaySeparatesRulesFromRoutesAndNeverPromisesAClassicLadder()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return PressWhen("StartButton");
            yield return WaitForScene(SceneFlow.ModeSelect);
            Assert.IsNotNull(ActiveButton("TutorialButton"));
            yield return PressWhen("ClassicButton");
            Assert.IsNotNull(ActiveButton("PracticeButton"));
            Assert.IsNotNull(ActiveButton("CustomButton"));
            Assert.IsFalse(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.name == "RankedButton" && b.isActiveAndEnabled));
            Assert.IsNotNull(ActiveButton("HeroStrikeButton"),"Game choice stays visible beside its access routes.");
            yield return PressWhen("HeroStrikeButton");
            Assert.IsNotNull(ActiveButton("RankedButton"));
            // ⚠⚠ THIS LINE LOOKED FOR `PlayChoiceCanvas/TumpMark` AND THE GAME HAS BUILT
            // `OwnerPlayCanvas` SINCE THE OWNER-PAINTED PASS. `TumpPlayView.Install` adds
            // `CourtPlayView`, whose canvas is `OwnerPlayCanvas` and whose logo is
            // `OriginalOwnerLogo`; `PlaySelectionScreen` still exists and still owns
            // `RequestedLobbyMode`, which is the only part of it this flow uses, so the old name
            // resolved to nothing and the test died on a null. § 124.11 again.
            // The assertion itself is unchanged and is worth keeping: a Default-imported texture
            // yields a null sprite and draws as a white rectangle where the logo should be.
            var logo = GameObject.Find("OwnerPlayCanvas").GetComponentsInChildren<Image>(true)
                .Single(i => i.name == "OriginalOwnerLogo");
            Assert.IsNotNull(logo.sprite,
                "A texture imported as Default must not become a white logo rectangle.");
            yield return UiRuntimeShots.Capture("Play-brand-v2",1920,1080);
            yield return UiRuntimeShots.Capture("Play-brand-v2-720p",1280,720);
            yield return UiRuntimeShots.Capture("Play-brand-v2-4by3",1200,900);
            yield return PressWhen("RankedButton");
            yield return WaitForScene(SceneFlow.MatchSetup);
            Assert.IsFalse(PlaySelectionScreen.RequestedLobbyMode.HasValue);
            Assert.IsFalse(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked,
                           "Opening the ranked destination must not open a LAN room.");
        }

        // A scene load is asynchronous, so asserting the scene name after a fixed sleep tests the
        // machine rather than the flow.
        private static IEnumerator WaitForScene(string name)
        {
            float end = Time.realtimeSinceStartup + 20f;
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
        public IEnumerator LoadingStoryCanBeOpenedAdvancedAndClosedWithoutSkippingReadiness()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.Splash);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsNotNull(Object.FindFirstObjectByType<SplashScreen>());
            // ⚠⚠ THE STORY DOOR IS A NAMED LINK NOW, NOT THE ARTWORK. `SplashScreen.cs`'s
            // `LoadingArtButton` belongs to the converted loading screen;
            // `BuildCourtLoadingSurface` is what the game builds, and it puts the door on a
            // labelled `LoadingStories` link instead of on the illustration, because artwork that
            // is secretly a button is `CLAUDE.md` § 6.3's invisible door. Both are still on disk.
            yield return PressWhen("LoadingStories");
            var story = GameObject.Find("LoadingStoryRoot");
            Assert.IsNotNull(story);
            var text = story.GetComponentsInChildren<Text>()
                .FirstOrDefault(t => LoadingPresentation.Stories.Contains(t.text));
            Assert.IsNotNull(text, "The story sheet shows none of LoadingPresentation.Stories: " +
                string.Join(" | ", story.GetComponentsInChildren<Text>().Select(t => t.name + "=" + t.text)));
            string first = text.text;
            yield return PressWhen("LoadingStoryNext");
            Assert.AreNotEqual(first, text.text);
            yield return new WaitForSecondsRealtime(15.1f);
            Assert.AreEqual(SceneFlow.Splash, SceneManager.GetActiveScene().name,
                "Reading must hold the loading screen past its maximum random dwell.");
            yield return PressWhen("LoadingStoryClose");
            Assert.IsFalse(story.activeSelf);
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
            yield return WaitForButton("StartButton");
        }
    }
}
