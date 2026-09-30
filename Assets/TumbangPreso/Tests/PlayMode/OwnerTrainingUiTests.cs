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
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerTrainingUiTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest, Timeout(180000)]
        public IEnumerator FinishedTutorialReturnsToTheLobbyHubThroughItsRealExit()
        {
            bool training = GameLaunch.GuidedTutorial;
            try
            {
                SceneFlow.Networked = false;
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
                GameLaunch.GuidedTutorial = true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                float until = Time.realtimeSinceStartup + 15;
                while (route == null && Time.realtimeSinceStartup < until)
                { route = Object.FindFirstObjectByType<GuidedTraining>(); yield return null; }
                Assert.IsNotNull(route);
                var ready = typeof(GuidedTraining).GetField("_ready", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                while (!(bool)ready.GetValue(route) && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue((bool)ready.GetValue(route));
                typeof(GuidedTraining).GetMethod("EnterLesson", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(route, new object[] { GuidedTraining.Lesson.Complete });
                var hud = Object.FindFirstObjectByType<GuidedTrainingHud>();
                Press(hud.GetComponentsInChildren<Button>().First(b => b.name == "SkipTrainingLesson"));
                until = Time.realtimeSinceStartup + 130;
                while ((SceneManager.GetActiveScene().name != SceneFlow.MatchSetup || UI.Hub.HubLoading.Visible) && Time.realtimeSinceStartup < until)
                    yield return null;
                Assert.AreEqual(SceneFlow.MatchSetup, SceneManager.GetActiveScene().name);
                Assert.IsFalse(UI.Hub.HubLoading.Visible);
                Assert.IsNotNull(UI.Hub.TumpHub.Current);
                Assert.IsFalse(GameLaunch.GuidedTutorial);
                Assert.IsFalse(GameServices.Match.MatchInProgress);
            }
            finally { GameLaunch.GuidedTutorial = training; }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator XeluControlsRenderThroughTheRealTrainingKeyRow()
        {
            bool training = GameLaunch.GuidedTutorial;
            try
            {
                SceneFlow.Networked = false;
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                GameLaunch.GuidedTutorial = true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                float until = Time.realtimeSinceStartup + 15;
                GuidedTrainingHud hud = null;
                while (hud == null && Time.realtimeSinceStartup < until)
                { hud = Object.FindFirstObjectByType<GuidedTrainingHud>(); yield return null; }
                Assert.IsNotNull(hud);
                Object.FindFirstObjectByType<GuidedTraining>().enabled = false;
                Hud.Instance.SetTrainingDeckHidden(false);
                yield return null;
                var readout = Object.FindFirstObjectByType<TumpMatchReadout>();
                var liveGlyphs = readout.Canvas.GetComponentsInChildren<Image>()
                    .Where(i => i.name == "BindingGlyph" && i.enabled && i.sprite != null && i.sprite.name.StartsWith("xelu:")).ToArray();
                Assert.AreEqual(3, liveGlyphs.Length, "The live power HUD did not adopt the current keyboard prompt images.");
                hud.SetLesson(0, GuidedTraining.LessonCount, "YOUR CONTROLS", "Your current binding chooses the picture.",
                    "[F]  READY / SHOVE  [RMB]  PICK UP", UiTheme.Offense);
                yield return null;
                var keyRow = hud.GetComponentsInChildren<RectTransform>().Single(r => r.name == "KeyRow");
                var glyphs = keyRow.GetComponentsInChildren<Image>().Where(i => i.sprite != null && i.sprite.name.StartsWith("xelu:")).ToArray();
                Assert.AreEqual(2, glyphs.Length);
                var pad = OwnerUiLayout.Rect(hud.transform, "PadPromptSamples");
                OwnerUiLayout.Place(pad, 800, 90, 640, 180);
                foreach (var family in new[] { InputGlyphs.PadFamily.Xbox, InputGlyphs.PadFamily.PlayStation })
                {
                    int x = family == InputGlyphs.PadFamily.Xbox ? 0 : 240;
                    var image = OwnerUiLayout.Rect(pad, family + "Confirm").gameObject.AddComponent<Image>();
                    image.sprite = InputGlyphs.For("BUTTON SOUTH", true, family);
                    image.preserveAspect = true; image.raycastTarget = false;
                    OwnerUiLayout.Place(image.rectTransform, x, 0, 76, 76);
                    var label = OwnerUiLayout.Text(pad, family + "Label", family == InputGlyphs.PadFamily.Xbox ? "XBOX A" : "PLAYSTATION CROSS", 28);
                    OwnerUiLayout.Place(label.rectTransform, x, 86, 220, 70); label.color = Color.white;
                    label.gameObject.AddComponent<Outline>().effectColor = UiTheme.InGameOutline;
                }
                foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                    yield return TumpUiCapture.Capture("Feedback0930-xelu-controls-" + size.x + "x" + size.y,
                        hud.GetComponent<Canvas>(), size.x, size.y, false, true, underlays: new[] { readout.Canvas });
            }
            finally { GameLaunch.GuidedTutorial = training; }
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator TrainingUsesOwnerThemeAndRealSkipQuitCallbacks()
        {
            bool training=GameLaunch.GuidedTutorial,networked=SceneFlow.Networked;
            try
            {
                SceneFlow.Networked=false;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                GameLaunch.GuidedTutorial=true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                GuidedTrainingHud hud=null;float until=Time.realtimeSinceStartup+15;
                while(hud==null && Time.realtimeSinceStartup<until){hud=Object.FindFirstObjectByType<GuidedTrainingHud>();yield return null;}
                Assert.IsNotNull(hud);var canvas=hud.GetComponent<Canvas>();Assert.IsNotNull(canvas.GetComponent<OwnerUiCanvas>());
                var counter=hud.GetComponentsInChildren<Text>().First(t=>t.name=="LessonCounter");
                var body=hud.GetComponentsInChildren<Text>().First(t=>t.name=="LessonBody");
                Assert.IsNotEmpty(body.text);
                var route=Object.FindFirstObjectByType<GuidedTraining>();
                foreach(var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("TrainingSidebar-look-"+size.x+"x"+size.y,canvas,size.x,size.y,false,true,checkActionBounds:true);
                for(int step=0;step<=GuidedTraining.LessonCount;step++)
                {
                    Assert.AreEqual(step,(int)route.CurrentLesson,"A real lesson was skipped by the UI review driver.");
                    yield return TumpUiCapture.Capture("TrainingSidebar-lesson-"+step,canvas,960,540,false,true,checkActionBounds:true);
                    if(step==GuidedTraining.LessonCount)break;
                    Press(hud.GetComponentsInChildren<Button>().First(b=>b.name=="SkipTrainingLesson"));
                    until=Time.realtimeSinceStartup+5;
                    while((int)route.CurrentLesson==step && Time.realtimeSinceStartup<until)yield return null;
                    yield return null;
                }
                // Check the renderer setter before the lesson controller next publishes its real progress.
                hud.SetProgress(.5f);
                var fill=hud.GetComponentsInChildren<Image>().First(i=>i.name=="ProgressFill");
                Assert.That(fill.rectTransform.anchorMax.x,Is.EqualTo(.5f).Within(.001f));
                yield return TumpUiCapture.Capture("TrainingSidebar-complete",canvas,1280,720,false,true,checkActionBounds:true);
                Press(hud.GetComponentsInChildren<Button>().First(b=>b.name=="QuitTraining"));
                until=Time.realtimeSinceStartup+10;
                while(SceneManager.GetActiveScene().name!=SceneFlow.MatchSetup && Time.realtimeSinceStartup<until)yield return null;
                Assert.AreEqual(SceneFlow.MatchSetup,SceneManager.GetActiveScene().name);Assert.False(GameLaunch.GuidedTutorial);
            }
            finally{GameLaunch.GuidedTutorial=training;SceneFlow.Networked=networked;}
        }
        [UnityTest, Timeout(180000)]
        public IEnumerator TutorialUsesReadableGlyphsAndEnterSkip()
        {
            bool training = GameLaunch.GuidedTutorial;
            int mip = QualitySettings.globalTextureMipmapLimit;
            var input = UnityEngine.InputSystem.InputSystem.settings;
            var background = input.backgroundBehavior; var editor = input.editorInputBehaviorInPlayMode;
            input.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keys = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            UnityEngine.InputSystem.InputSystem.EnableDevice(keys);
            QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
                GameLaunch.GuidedTutorial = true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                var route = Object.FindFirstObjectByType<GuidedTraining>();
                float until = Time.unscaledTime + 15;
                while (route == null && Time.unscaledTime < until)
                { yield return null; route = Object.FindFirstObjectByType<GuidedTraining>(); }
                Assert.IsNotNull(route);
                var ready = typeof(GuidedTraining).GetField("_ready", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                while (!(bool)ready.GetValue(route) && Time.unscaledTime < until) yield return null;
                Assert.IsTrue((bool)ready.GetValue(route));
                var first = route.CurrentLesson;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.N));
                UnityEngine.InputSystem.InputSystem.Update(); route.SendMessage("Update");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.Update();
                yield return new WaitForSecondsRealtime(.8f);
                Assert.AreEqual(first, route.CurrentLesson, "Retired N binding must not skip a lesson.");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter));
                UnityEngine.InputSystem.InputSystem.Update(); route.SendMessage("Update");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.Update();
                yield return new WaitForSecondsRealtime(.8f);
                Assert.AreEqual((int)first + 1, (int)route.CurrentLesson, "Enter must advance exactly one lesson.");
                route.enabled = false;
                var hud = Object.FindFirstObjectByType<GuidedTrainingHud>();
                foreach (var name in new[] { "SkipTrainingLesson", "QuitTraining" })
                {
                    var action = hud.GetComponentsInChildren<Button>().Single(b => b.name == name);
                    var label = action.GetComponentInChildren<Text>();
                    Assert.AreSame(OwnerUiTheme.Current.Display, label.font);
                    Assert.IsFalse(label.text.Contains("ENTER") || label.text.Contains("BACKSPACE"));
                    var glyph = action.GetComponentsInChildren<Image>().Single(i => i.sprite != null);
                    Assert.AreSame(InputGlyphs.For(name == "SkipTrainingLesson" ? "ENTER" : "BACKSPACE", true), glyph.sprite);
                    Assert.That(glyph.rectTransform.rect.height, Is.GreaterThanOrEqualTo(64));
                }
                hud.SetLesson(1, GuidedTraining.LessonCount, "MOVE AROUND", "Move around the arena.", "[W] [A] [S] [D] MOVE", UiTheme.Offense);
                yield return null;
                var row = hud.GetComponentsInChildren<RectTransform>().Single(r => r.name == "KeyRow");
                var prompts = row.GetComponentsInChildren<Image>().Where(i => i.sprite != null).ToArray();
                Assert.AreEqual(4, prompts.Length);
                Assert.IsTrue(prompts.All(i => i.rectTransform.rect.height >= 64));
                foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                    yield return TumpUiCapture.Capture("Feedback0930-training-prompts-" + size.x + "x" + size.y,
                        hud.GetComponent<Canvas>(), size.x, size.y, false, true, checkActionBounds: true);
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keys);
                input.backgroundBehavior = background; input.editorInputBehaviorInPlayMode = editor;
                QualitySettings.globalTextureMipmapLimit = mip; GameLaunch.GuidedTutorial = training;
            }
        }

        private static void Press(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.IsNotEmpty(hits);Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>());
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
