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
        public IEnumerator CompletedTutorialRemainsPlayableUntilItsRealQuit()
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
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual(GuidedTraining.Lesson.Complete, route.CurrentLesson);
                Assert.AreEqual(SceneFlow.Eskinita, SceneManager.GetActiveScene().name);
                Assert.IsTrue(GameServices.Match.MatchInProgress);
                Assert.IsTrue(GameServices.Round.RoundActive);
                Assert.IsFalse(hud.GetComponentsInChildren<Button>(true).First(b => b.name == "SkipTrainingLesson").gameObject.activeSelf);
                var local = (CharacterMotor)typeof(GuidedTraining).GetField("_local", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(route);
                Assert.IsFalse(local.IsDefender); Assert.IsTrue(local.CanAct());
                var defender = GameServices.Round.Players.First(p => p != null && p.IsDefender);
                Assert.IsTrue(defender.gameObject.activeSelf);
                until = Time.time + 5;
                while (GameServices.Round.Lata.IsProtected && Time.time < until) yield return null;
                GameServices.Round.Lata.HostKnockDown(-1);
                Assert.IsFalse(GameServices.Round.Lata.IsUpright);
                until = Time.time + 12;
                while (!GameServices.Round.Lata.IsUpright && Time.time < until) yield return null;
                Assert.IsTrue(GameServices.Round.Lata.IsUpright, "The completed range defender must actually reset the can.");
                Assert.IsFalse(defender.Intent.Pressed(Verb.SpecialAbility));
                Assert.IsFalse(defender.Intent.Pressed(Verb.Lunge));
                Press(hud.GetComponentsInChildren<Button>().First(b => b.name == "QuitTraining"));
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

        private static IEnumerator OpenRevisedTraining()
        {
            GameLaunch.Reset(); GameLaunch.GuidedTutorial = true;
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            float until = Time.unscaledTime + 20;
            while (Object.FindFirstObjectByType<GuidedTraining>() == null && Time.unscaledTime < until) yield return null;
            var route = Object.FindFirstObjectByType<GuidedTraining>(); Assert.IsNotNull(route);
            var ready = typeof(GuidedTraining).GetField("_ready", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            while (!(bool)ready.GetValue(route) && Time.unscaledTime < until) yield return null;
            Assert.IsTrue((bool)ready.GetValue(route));
            while (UI.Hub.HubLoading.Visible && Time.unscaledTime < until) yield return null;
            Assert.IsFalse(UI.Hub.HubLoading.Visible);
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
        }
        private static void SelectLesson(GuidedTraining route, GuidedTraining.Lesson lesson)
            => typeof(GuidedTraining).GetMethod("EnterLesson", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(route, new object[] { lesson });
        private static CharacterMotor Student(GuidedTraining route)
            => (CharacterMotor)typeof(GuidedTraining).GetField("_local", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(route);

        [UnityTest, Timeout(180000)]
        public IEnumerator RevisedTutorialHasTwentyOrderedLessonsAndHonestMovement()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); var local = Student(route);
                Assert.AreEqual(20, GuidedTraining.LessonCount);
                Assert.IsNotNull(InputGlyphs.For("MOUSE", true));
                StringAssert.Contains("Mouse_Simple", InputGlyphs.For("MOUSE", true).name);
                Assert.AreEqual(GuidedTraining.Lesson.Ready, route.CurrentLesson);
                Assert.IsFalse(local.IsDefender);
                Assert.IsFalse(GameServices.Round.Lata.gameObject.activeSelf);
                SelectLesson(route, GuidedTraining.Lesson.Move);
                local.Intent.Move = Vector2.up;
                yield return new WaitForSeconds(.3f);
                Assert.AreEqual(GuidedTraining.Lesson.Move, route.CurrentLesson, "Less than five metres cannot complete movement.");
                float until = Time.time + 8;
                while (route.CurrentLesson == GuidedTraining.Lesson.Move && Time.time < until) yield return null;
                Assert.AreEqual(GuidedTraining.Lesson.Sprint, route.CurrentLesson);
                local.Intent.Move = Vector2.up; local.Intent.Set(Verb.Sprint, true);
                yield return new WaitForSeconds(.5f);
                Assert.AreEqual(GuidedTraining.Lesson.Sprint, route.CurrentLesson, "A short sprint cannot satisfy ten metres.");
                until = Time.time + 12;
                while (route.CurrentLesson == GuidedTraining.Lesson.Sprint && Time.time < until) yield return null;
                Assert.AreEqual(GuidedTraining.Lesson.Jump, route.CurrentLesson);
                local.Intent.Clear(); route.enabled = false;
                foreach (GuidedTraining.Lesson lesson in System.Enum.GetValues(typeof(GuidedTraining.Lesson)))
                {
                    SelectLesson(route, lesson); yield return null;
                    bool defender = lesson == GuidedTraining.Lesson.Block || lesson == GuidedTraining.Lesson.Punch
                        || lesson == GuidedTraining.Lesson.DefenderReset || lesson == GuidedTraining.Lesson.ResetAndTag || lesson == GuidedTraining.Lesson.Lunge;
                    Assert.AreEqual(defender, local.IsDefender, lesson.ToString());
                    Assert.AreEqual(defender || lesson == GuidedTraining.Lesson.ThrowAndRetrieve || lesson == GuidedTraining.Lesson.Complete,
                        GameServices.Round.Lata.gameObject.activeSelf, lesson + " can visibility");
                }
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TutorialBlockExerciseRequiresThreeRealBodyBlocks()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); var local = Student(route);
                SelectLesson(route, GuidedTraining.Lesson.Block);
                int blocks = 0;
                void Observe(Visual.MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
                { if (kind == Visual.MatchFlair.Kind.Block && subject == local.PlayerSlot) blocks++; }
                Visual.MatchFlair.Presented += Observe;
                try
                {
                    local.Teleport(new Vector3(-4, 0, 0));
                    yield return new WaitForSeconds(3.5f);
                    Assert.AreEqual(0, blocks, "A missed throw must not count as a body block.");
                    Assert.AreEqual(GuidedTraining.Lesson.Block, route.CurrentLesson);
                    float until = Time.time + 18;
                    while (route.CurrentLesson == GuidedTraining.Lesson.Block && Time.time < until)
                    {
                        local.Teleport(new Vector3(2, 0, 4));
                        yield return new WaitForSeconds(.1f);
                        if (blocks < 3) Assert.AreEqual(GuidedTraining.Lesson.Block, route.CurrentLesson);
                    }
                    Assert.That(blocks, Is.GreaterThanOrEqualTo(3));
                    Assert.AreEqual(GuidedTraining.Lesson.Punch, route.CurrentLesson);
                }
                finally { Visual.MatchFlair.Presented -= Observe; }
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator ReadyAndLookRequireTheirActualInputAndThreeSeconds()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            var input = UnityEngine.InputSystem.InputSystem.settings;
            var background = input.backgroundBehavior; var editor = input.editorInputBehaviorInPlayMode;
            input.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); var local = Student(route);
                yield return new WaitForSeconds(.3f);
                Assert.AreEqual(GuidedTraining.Lesson.Ready, route.CurrentLesson);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                UnityEngine.InputSystem.InputSystem.Update(); route.SendMessage("Update");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.Update();
                yield return new WaitForSecondsRealtime(.8f);
                Assert.AreEqual(GuidedTraining.Lesson.Look, route.CurrentLesson);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.AreEqual(GuidedTraining.Lesson.Look, route.CurrentLesson, "Idle time must not count as looking.");
                local.Intent.LookDelta = new Vector2(.2f, .1f);
                yield return new WaitForSecondsRealtime(2.6f);
                Assert.AreEqual(GuidedTraining.Lesson.Look, route.CurrentLesson, "Looking for less than three seconds must not finish.");
                float until = Time.unscaledTime + 3;
                while (route.CurrentLesson == GuidedTraining.Lesson.Look && Time.unscaledTime < until) yield return null;
                local.Intent.LookDelta = Vector2.zero;
                Assert.AreEqual(GuidedTraining.Lesson.Move, route.CurrentLesson);
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                input.backgroundBehavior = background; input.editorInputBehaviorInPlayMode = editor;
                QualitySettings.globalTextureMipmapLimit = mip;
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator CombinedTutorialExercisesRequireTheOrderedRealOutcomes()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); var local = Student(route);
                var lata = GameServices.Round.Lata; var carrier = local.GetComponent<Carrier>();
                SelectLesson(route, GuidedTraining.Lesson.ThrowAndRetrieve);
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual(GuidedTraining.Lesson.ThrowAndRetrieve, route.CurrentLesson, "Starting with a slipper is not completing the exercise.");
                float until = Time.time + 5;
                while (lata.IsProtected && Time.time < until) yield return null;
                var shoe = carrier.Held; Assert.IsNotNull(shoe);
                carrier.HostThrowAt(carrier.ThrowOrigin(), lata.transform.position + Vector3.up * .28f, 1);
                Assert.AreEqual(SlipperState.InFlight, shoe.State);
                until = Time.time + 6;
                while (lata.IsUpright && Time.time < until) yield return null;
                Assert.IsFalse(lata.IsUpright, "The real thrown slipper must hit the can.");
                Assert.AreEqual(GuidedTraining.Lesson.ThrowAndRetrieve, route.CurrentLesson, "A knock alone is not retrieval and escape.");
                until = Time.time + 6;
                while (shoe.State != SlipperState.Loose && Time.time < until) yield return null;
                Assert.AreEqual(SlipperState.Loose, shoe.State);
                local.Teleport(shoe.transform.position);
                Assert.IsTrue(shoe.HostGrab(local));
                local.Teleport(new Vector3(2, 0, 2)); yield return null;
                Assert.AreEqual(GuidedTraining.Lesson.ThrowAndRetrieve, route.CurrentLesson, "Holding inside the box cannot satisfy escape.");
                local.Teleport(local.SpawnPosition);
                yield return new WaitForSeconds(.8f);
                Assert.AreEqual(GuidedTraining.Lesson.Shove, route.CurrentLesson);

                SelectLesson(route, GuidedTraining.Lesson.ResetAndTag);
                var dummy = (CharacterMotor)typeof(GuidedTraining).GetField("_dummy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(route);
                Assert.IsFalse(dummy.gameObject.activeSelf, "The target cannot be tagged before the can-down exercise is armed.");
                until = Time.time + 5;
                while (lata.IsUpright && Time.time < until) yield return null;
                Assert.IsFalse(lata.IsUpright); Assert.IsTrue(dummy.gameObject.activeSelf);
                local.Teleport(lata.transform.position + Vector3.back);
                local.Intent.Set(Verb.Grab, true);
                until = Time.time + 5;
                while (!lata.IsUpright && Time.time < until) yield return null;
                local.Intent.Set(Verb.Grab, false);
                Assert.IsTrue(lata.IsUpright); Assert.AreEqual(GuidedTraining.Lesson.ResetAndTag, route.CurrentLesson);
                until = Time.time + 5;
                while (lata.IsProtected && Time.time < until) yield return null;
                local.Teleport(dummy.transform.position + Vector3.back);
                local.transform.forward = Vector3.forward;
                Assert.IsTrue(local.GetComponent<CombatVerbs>().HostResolvePunch(local.transform.position, local.transform.forward));
                yield return new WaitForSeconds(.1f);
                Assert.IsFalse(dummy.gameObject.activeSelf, "The actually tagged practice target must disappear.");
                yield return new WaitForSeconds(.7f);
                Assert.AreEqual(GuidedTraining.Lesson.Lunge, route.CurrentLesson);
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator CurveLungeAndEmoteLessonsNeedTheActualActions()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); var local = Student(route);
                SelectLesson(route, GuidedTraining.Lesson.Pektus);
                var carrier = local.GetComponent<Carrier>(); var shoe = carrier.Held;
                foreach (float spin in new[] { 0f, 1f })
                {
                    Assert.AreSame(shoe, carrier.Held);
                    carrier.HostThrowAt(carrier.ThrowOrigin(), local.transform.position + local.transform.forward * 4, .5f, spin);
                    float until = Time.time + 8;
                    while (shoe.State != SlipperState.Loose && Time.time < until) yield return null;
                    Assert.AreEqual(SlipperState.Loose, shoe.State);
                    Assert.AreEqual(GuidedTraining.Lesson.Pektus, route.CurrentLesson, "A curved throw alone still needs retrieval.");
                    local.Teleport(shoe.transform.position); Assert.IsTrue(shoe.HostGrab(local));
                    yield return new WaitForSeconds(.8f);
                    Assert.AreEqual(spin == 0 ? GuidedTraining.Lesson.Pektus : GuidedTraining.Lesson.ThrowAndRetrieve,
                        route.CurrentLesson, "Only a genuinely curved throw plus retrieval can finish.");
                }
                SelectLesson(route, GuidedTraining.Lesson.Lunge);
                var dummy = (CharacterMotor)typeof(GuidedTraining).GetField("_dummy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(route);
                local.Intent.Set(Verb.SpecialAbility, true);
                Assert.IsFalse(local.Intent.Pressed(Verb.SpecialAbility), "The lunge exercise must not accept the old stationary-punch shortcut.");
                local.Intent.Set(Verb.SpecialAbility, false);
                float deadline = Time.time + 5;
                while (GameServices.Round.Lata.IsProtected && Time.time < deadline) yield return null;
                local.Teleport(dummy.transform.position + Vector3.back * 2.5f); local.transform.forward = Vector3.forward;
                int score = GameServices.Match.ScoreFor(local.PlayerSlot);
                Assert.IsTrue(local.GetComponent<CombatVerbs>().HostResolveLunge(local.transform.position, local.transform.forward, 1));
                deadline = Time.time + 3;
                while (route.CurrentLesson == GuidedTraining.Lesson.Lunge && Time.time < deadline) yield return null;
                Assert.AreNotEqual(GuidedTraining.Lesson.Lunge, route.CurrentLesson);
                Assert.Greater(GameServices.Match.ScoreFor(local.PlayerSlot), score);
                // Select after any Classic-only unavailable-kit skip has finished.
                route.StopAllCoroutines(); SelectLesson(route, GuidedTraining.Lesson.Emote);
                local.Intent.Clear();
                var emote = local.GetComponent<TumbangPreso.Social.EmotePlayer>();
                Assert.IsTrue(emote.CanEmote());
                emote.HostPlay(TumbangPreso.Social.Emotes.All[0].Id);
                Assert.IsTrue(emote.IsEmoting);
                yield return new WaitForSeconds(.8f);
                Assert.AreEqual(GuidedTraining.Lesson.Complete, route.CurrentLesson);
                Assert.AreEqual(SceneFlow.Eskinita, SceneManager.GetActiveScene().name);
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TrainingScrollPromptsHaveVisibleNativeMeshes()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); route.enabled = false;
                var hud = Object.FindFirstObjectByType<GuidedTrainingHud>();
                hud.SetLesson(7, GuidedTraining.LessonCount, "CURVE THROW", "Scroll to curve the throw, then retrieve your slipper.",
                    "[WHEEL UP] / [WHEEL DOWN] CURVE", UiTheme.Offense);
                yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                var row = hud.GetComponentsInChildren<RectTransform>().Single(r => r.name == "KeyRow");
                var wheels = row.GetComponentsInChildren<Graphic>().Where(g => g.GetType().Name == "TrainingWheelGlyph").ToArray();
                Assert.AreEqual(2, wheels.Length);
                foreach (var wheel in wheels)
                {
                    Assert.IsNotNull(wheel.GetComponent<CanvasRenderer>());
                    var mesh = wheel.canvasRenderer.GetMesh(); Assert.IsNotNull(mesh);
                    Assert.Greater(mesh.vertexCount, 50, "The layout box alone is not a visible scroll icon.");
                    Assert.IsTrue(mesh.colors32.Any(c => c.a > 0)); Assert.IsFalse(wheel.canvasRenderer.cull);
                }
                foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                    yield return TumpUiCapture.Capture("Feedback0930-training-wheel-" + size.x + "x" + size.y,
                        hud.GetComponent<Canvas>(), size.x, size.y, false, true, checkActionBounds: true);
                hud.SetLesson(7, GuidedTraining.LessonCount, "CURVE THROW", "Rebound keys keep their own prompts.",
                    "[Q] / [E] CURVE", UiTheme.Offense);
                yield return null;
                Assert.IsFalse(row.GetComponentsInChildren<Graphic>().Any(g => g.GetType().Name == "TrainingWheelGlyph"));
                Assert.AreEqual(2, row.GetComponentsInChildren<Image>().Count(i => i.sprite != null && i.sprite.name.StartsWith("xelu:")));
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator HiddenTrainingCanAlsoHidesItsIndependentClock()
        {
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                yield return OpenRevisedTraining();
                var route = Object.FindFirstObjectByType<GuidedTraining>(); route.enabled = false;
                var can = GameServices.Round.Lata;
                var clock = Visual.LataClockPresentation.For(can); Assert.IsNotNull(clock);
                foreach (var lesson in new[] { GuidedTraining.Lesson.Look, GuidedTraining.Lesson.Throw,
                    GuidedTraining.Lesson.Retrieve, GuidedTraining.Lesson.Pektus, GuidedTraining.Lesson.ThrowAndRetrieve,
                    GuidedTraining.Lesson.Block, GuidedTraining.Lesson.AbilityInfo, GuidedTraining.Lesson.Complete })
                {
                    SelectLesson(route, lesson); yield return null;
                    Assert.AreEqual(can.gameObject.activeInHierarchy, clock.gameObject.activeInHierarchy, lesson.ToString());
                }
                SelectLesson(route, GuidedTraining.Lesson.Look); yield return null;
                yield return TumpUiCapture.Capture("Feedback0930-training-hidden-can",
                    Object.FindFirstObjectByType<GuidedTrainingHud>().GetComponent<Canvas>(), 960, 540, false, true);
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
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
