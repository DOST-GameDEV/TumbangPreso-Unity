using System.Collections;
using NUnit.Framework;
using System.Linq;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MobileInputViewportTests
    {
        [UnityTest] public IEnumerator ScreenHoldsRespectPointerOwnershipAndGlobalRetirement()
        {
            yield return PlayModeWorld.Reset();
            bool active = TouchInput.Active; TouchInput.Active = true;
            var root = new GameObject("Screen hold ownership"); var hold = root.AddComponent<TouchHoldControl>(); hold.Bind(null, "CurveRight");
            var owner = new PointerEventData(EventSystem.current) { pointerId = 7 };
            var other = new PointerEventData(EventSystem.current) { pointerId = 9 };
            try
            {
                hold.OnPointerDown(owner); hold.OnPointerDown(other); hold.OnPointerUp(other);
                Assert.IsTrue(TouchInput.ScreenHeld("CurveRight"));
                Assert.AreEqual(.6f, TouchInput.ReadCurve(.35f, 2.5f, .1f), .001f);
                Assert.AreEqual(.25f, TouchInput.ReadCurve(.35f, 2.5f, .1f), .001f, "The initial step must be consumed once.");
                TouchInput.ReleaseAll(); Assert.IsFalse(TouchInput.ScreenHeld("CurveRight"));
                hold.OnPointerDown(other); hold.OnPointerUp(owner);
                Assert.IsTrue(TouchInput.ScreenHeld("CurveRight"), "A retired pointer must not release the new owner.");
                hold.enabled = false; Assert.IsFalse(TouchInput.ScreenHeld("CurveRight"));
                TouchInput.ReleaseAll(); Assert.AreEqual(0, TouchInput.ReadCurve(.35f, 2.5f, .1f));
            }
            finally { Object.DestroyImmediate(root); TouchInput.ReleaseAll(); TouchInput.Active = active; }
        }

        [UnityTest, Timeout(180000)] public IEnumerator PhonePlayerInfoCurveAndMenuReachTheirLiveConsumers()
        {
            yield return PlayModeWorld.Reset();
            bool force = TouchHud.ForceVisible, training = GameLaunch.GuidedTutorial, network = SceneFlow.Networked;
            var deviceBefore = LastInputDevice.Current;
            UnityEngine.InputSystem.Touchscreen screen = null;
            var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
            var background = inputSettings.backgroundBehavior;
            var editorInput = inputSettings.editorInputBehaviorInPlayMode;
            var rules = SceneFlow.SelectedRules;
            try
            {
                TouchHud.ForceVisible = true; GameLaunch.GuidedTutorial = true; SceneFlow.Networked = false;
                inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                SceneFlow.SetSelectedRules(Core.CustomGameRules.Defaults(Core.GameMode.HeroStrike));
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                GuidedTraining route = null; float until = Time.realtimeSinceStartup + 25;
                while (Time.realtimeSinceStartup < until)
                {
                    route = Object.FindFirstObjectByType<GuidedTraining>();
                    if (route != null && (bool)typeof(GuidedTraining).GetField("_ready", flags).GetValue(route)) break;
                    yield return null;
                }
                Assert.IsNotNull(route); Assert.IsTrue((bool)typeof(GuidedTraining).GetField("_ready", flags).GetValue(route));
                var actor = (CharacterMotor)typeof(GuidedTraining).GetField("_local", flags).GetValue(route);
                var touch = TouchHud.Instance; Assert.IsNotNull(touch);
                until = Time.realtimeSinceStartup + 40;
                while ((ReadyGate.HidesHud || PresentationClock.BlocksInput || !touch.Canvas.enabled || UI.Hub.HubLoading.Visible) && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsFalse(ReadyGate.HidesHud); Assert.IsFalse(PresentationClock.BlocksInput);
                yield return null; yield return null;
                screen = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(screen, new UnityEngine.InputSystem.LowLevel.TouchState
                    { touchId = 71, phase = UnityEngine.InputSystem.TouchPhase.Began, position = new Vector2(900, 700) });
                yield return null;
                LastInputDevice.Sample();
                Assert.IsTrue(Hud.OnTouch, "The consumer test must use a real Input System touch device, not only force the picture on.");
                var pointer = new PointerEventData(EventSystem.current) { pointerId = 81 };
                var interact = touch.Buttons.Single(b => b.Entry.Verb == Verb.Interact);
                interact.OnPointerDown(pointer); yield return new WaitForSeconds(.25f); interact.OnPointerUp(pointer);
                until = Time.realtimeSinceStartup + 4;
                while (route.CurrentLesson == GuidedTraining.Lesson.Ready && Time.realtimeSinceStartup < until) yield return null;
                Assert.AreEqual(GuidedTraining.Lesson.Look, route.CurrentLesson, "Actual touch Interact must ready the tutorial.");
                var enter = typeof(GuidedTraining).GetMethod("EnterLesson", flags);
                enter.Invoke(route, new object[] { GuidedTraining.Lesson.Pektus });
                var fire = touch.Buttons.Single(b => b.Entry.Verb == Verb.SpecialAbility);
                fire.OnPointerDown(pointer); yield return new WaitForSeconds(.3f);
                var curve = touch.Canvas.GetComponentsInChildren<TouchHoldControl>().Single(b => b.name == "TouchCurveRight");
                curve.OnPointerDown(new PointerEventData(EventSystem.current) { pointerId = 82 });
                yield return new WaitForSeconds(.25f);
                Assert.Greater(actor.GetComponent<Carrier>().CurrentPektusSpin, 0, "The live reader/carrier must consume the touch curve.");
                yield return TumpUiCapture.Capture("PhonePlayer-curve", touch.Canvas, 1600, 720, false, includeWorld: true);
                curve.OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 82 }); fire.OnPointerUp(pointer);
                yield return new WaitForSeconds(.2f);
                enter.Invoke(route, new object[] { GuidedTraining.Lesson.AbilityInfo });
                var info = touch.Canvas.GetComponentsInChildren<TouchHoldControl>().Single(b => b.name == "TouchAbilityInfo");
                info.OnPointerDown(pointer); yield return null; yield return null;
                var readout = Object.FindFirstObjectByType<TumpPowerReadout>();
                var detail = (Component)typeof(TumpPowerReadout).GetField("_detail", flags).GetValue(readout);
                Assert.IsTrue(detail.gameObject.activeSelf, "The actual power descriptions must respond to held Info.");
                yield return new WaitForSeconds(3);
                Assert.AreNotEqual(GuidedTraining.Lesson.AbilityInfo, route.CurrentLesson, "Held phone Info must complete its lesson.");
                yield return TumpUiCapture.Capture("PhonePlayer-info", touch.Canvas, 1600, 720, false, includeWorld: true, underlays: new[] { detail.GetComponentInParent<Canvas>(), Object.FindFirstObjectByType<GuidedTrainingHud>().GetComponent<Canvas>() });
                info.OnPointerUp(pointer); yield return null;
                Assert.IsFalse(detail.gameObject.activeSelf);
                touch.Canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "TouchPlayerMenu").onClick.Invoke();
                yield return null;
                var pause = Object.FindFirstObjectByType<PausePanel>(); Assert.IsNotNull(pause); Assert.IsTrue(actor.Intent.Parked);
                Assert.IsFalse(TouchInput.ScreenHeld("AbilityInfo")); Assert.AreEqual(0, TouchInput.ReadCurve(.35f, 2.5f, .1f));
                pause.Close();
            }
            finally
            {
                TouchInput.ReleaseAll(); TouchHud.ForceVisible = force; GameLaunch.GuidedTutorial = training;
                if (screen != null && screen.added) UnityEngine.InputSystem.InputSystem.RemoveDevice(screen);
                typeof(LastInputDevice).GetProperty("Current").SetValue(null, deviceBefore);
                inputSettings.backgroundBehavior = background; inputSettings.editorInputBehaviorInPlayMode = editorInput;
                SceneFlow.Networked = network; SceneFlow.SetSelectedRules(rules);
            }
            yield return PlayModeWorld.Reset();
        }

        [UnityTest] public IEnumerator PhoneTrainingUsesVisibleTouchActionsInsteadOfKeyboardExitGlyphs()
        {
            yield return PlayModeWorld.Reset();
            bool force = TouchHud.ForceVisible;
            var root = new GameObject("Phone training readout");
            try
            {
                TouchHud.ForceVisible = true;
                var hud = GuidedTrainingHud.Build(root.transform);
                hud.SetLesson(0, GuidedTraining.LessonCount, "READY UP", "Every match needs you to ready up before it starts.", "[TAP] READY UP", UiTheme.Offense);
                yield return null;
                var canvas = hud.GetComponent<Canvas>();
                Assert.IsNull(hud.transform.Find("Key_ENTER"));
                Assert.IsFalse(hud.GetComponentsInChildren<RectTransform>().Any(t => t.name == "Key_ENTER" || t.name == "Key_BACKSPACE"));
                Assert.AreEqual(2, hud.GetComponentsInChildren<UnityEngine.UI.Button>().Length);
                yield return TumpUiCapture.Capture("PhoneTraining-ready", canvas, 1600, 720, false, checkActionBounds: true);
                hud.SetLesson(GuidedTraining.LessonCount, GuidedTraining.LessonCount, "TUTORIAL COMPLETE", "Feel free to test everything you just learned while you are still here.", "", UiTheme.Offense);
                yield return null;
                Assert.IsFalse(hud.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "SkipTrainingLesson").gameObject.activeSelf);
                yield return TumpUiCapture.Capture("PhoneTraining-complete", canvas, 1600, 720, false, checkActionBounds: true);
            }
            finally { Object.DestroyImmediate(root); TouchHud.ForceVisible = force; }
            yield return PlayModeWorld.Reset();
        }

        [UnityTest] public IEnumerator PhoneSettingsExposeArrangeWithoutScrollingAndKeepNestedReturnPaths()
        {
            yield return PlayModeWorld.Reset();
            bool force = TouchHud.ForceVisible;
            string saved = JsonUtility.ToJson(Settings.SettingsStore.Current);
            var root = new GameObject("Phone settings interaction");
            var view = root.AddComponent<TumpSettingsView>();
            int arrange = 0, back = 0;
            try
            {
                TouchHud.ForceVisible = true;
                Settings.SettingsStore.Current.LargerText = false;
                view.Open(root.transform, () => back++, () => {}, () => arrange++);
                yield return null;
                var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(c => c.name == "OwnerSettingsCanvas");
                var scroll = canvas.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                var button = canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "TouchLayoutAction");
                yield return TumpUiCapture.Capture("PhoneSettings-controls", canvas, 1600, 720, false, checkActionBounds: true,
                    inspectViewport: () =>
                    {
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, button.transform);
                        Assert.GreaterOrEqual(bounds.min.y, scroll.viewport.rect.yMin, "Arrange must be visible on arrival.");
                        Assert.LessOrEqual(bounds.max.y, scroll.viewport.rect.yMax);
                        var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                        float height = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]),
                            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[1]));
                        Assert.GreaterOrEqual(height, 95.5f, "The phone control must keep the shared144-unit touch floor.");
                    });
                ClickPhoneControl(canvas, "TouchLayoutAction");
                Assert.AreEqual(1, arrange); Assert.IsFalse(canvas.gameObject.activeSelf);
                view.Resume(); yield return null;
                ClickPhoneControl(canvas, "InputDeviceValue"); yield return null;
                var popup = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b => b.name == "Option0");
                popup.onClick.Invoke(); yield return null;
                Assert.IsTrue(canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name.StartsWith("Binding_")), "External keyboard controls stay reachable.");
                ClickPhoneControl(canvas, "InputDeviceValue"); yield return null;
                Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b => b.name == "Option2").onClick.Invoke();
                yield return null;
                for (int i = 1; i < TumpSettingsView.Sections.Length; i++)
                {
                    ClickPhoneControl(canvas, "SettingsSection" + i); yield return null;
                    Assert.IsNull(SettingsOptionMenu.OpenOption);
                    yield return TumpUiCapture.Capture("PhoneSettings-" + TumpSettingsView.Sections[i], canvas, 1600, 720, false, checkActionBounds: true);
                }
                Settings.SettingsStore.Current.ReducedUiMotion = !Settings.SettingsStore.Current.ReducedUiMotion;
                view.Session.Preview(); ClickPhoneControl(canvas, "TumpSettingsBack"); yield return null;
                ClickPhoneControl(canvas, "KeepEditing"); yield return null;
                Assert.IsTrue(view.Session.Dirty); Assert.AreEqual(0, back);
                ClickPhoneControl(canvas, "TumpSettingsBack"); yield return null;
                ClickPhoneControl(canvas, "DiscardAndBack"); yield return null;
                Assert.AreEqual(1, back);
            }
            finally
            {
                Object.DestroyImmediate(root); TouchHud.ForceVisible = force;
                Settings.SettingsStore.Restore(JsonUtility.FromJson<Settings.GameSettings>(saved));
            }
            yield return PlayModeWorld.Reset();
        }
        private static void ClickPhoneControl(Canvas canvas, string name)
        {
            Canvas.ForceUpdateCanvases();
            var button = canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == name);
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, name + " has no pointer target.");
            Assert.AreSame(button, hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>(), name + " is obscured by another target.");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest] public IEnumerator PhoneMenuKeepsItsDoorsAndReplayLibraryAvoidsDesktopFileOperations()
        {
            yield return PlayModeWorld.Reset();
            bool force = TouchHud.ForceVisible;
            try
            {
                TouchHud.ForceVisible = true;
                yield return HubFlowTests.OpenHome();
                var hub = UI.Hub.TumpHub.Current; hub.Push<UI.Hub.HubMenu>();
                yield return new WaitForSecondsRealtime(.5f);
                Assert.AreEqual(8, hub.Top.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b => b.name.StartsWith("Menu")));
                foreach (var size in new[] { new Vector2Int(1600, 720), new Vector2Int(1280, 720) })
                    yield return TumpUiCapture.Capture("PhoneHomeMenu-" + size.x, hub.Canvas, size.x, size.y, false, checkActionBounds: true);
                ClickPhoneControl(hub.Canvas, "MenuREPLAYS"); yield return null;
                Assert.IsInstanceOf<UI.Hub.HubReplays>(hub.Top);
                Assert.IsFalse(hub.Top.GetComponentsInChildren<UnityEngine.UI.InputField>().Any(), "Phone recordings should not ask for desktop absolute paths.");
                Assert.IsFalse(hub.Top.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name == "OpenReplayFolder"));
                yield return TumpUiCapture.Capture("PhoneReplayLibrary", hub.Canvas, 1600, 720, false, checkActionBounds: true);
                hub.Back(); yield return null;
                Assert.IsInstanceOf<UI.Hub.HubHome>(hub.Top);
                hub.Push<UI.Hub.HubMenu>(); yield return null;
                ClickPhoneControl(hub.Canvas, "MenuSETTINGS"); yield return null;
                Assert.IsNotNull(Object.FindFirstObjectByType<TumpSettingsView>());
            }
            finally { TouchHud.ForceVisible = force; }
            yield return PlayModeWorld.Reset();
        }

        [UnityTest] public IEnumerator TouchSettingsHaveWorkingSwipeControlsAndCutoutInsets()
        {
            yield return PlayModeWorld.Reset();
            string saved=JsonUtility.ToJson(Settings.SettingsStore.Current);
            var root=new GameObject("Touch settings journey");
            var view=root.AddComponent<TumpSettingsView>();
            try
            {
                view.Open(root.transform,()=>{},()=>{},()=>{});
                typeof(TumpSettingsView).GetField("_device",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                    .SetValue(view,InputDeviceKind.Touch);
                view.ShowSection(0);yield return null;
                var canvas=(Canvas)typeof(TumpSettingsView).GetField("_canvas",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view);
                var sensitivity=System.Array.Find(Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Exclude),
                    slider=>slider.name=="TouchSensitivityValue");
                Assert.IsNotNull(sensitivity,"The touch page must expose the swipe setting used by actual look input.");
                sensitivity.value=2.5f;
                Assert.AreEqual(2.5f,Settings.SettingsStore.Current.MouseSensitivity,.001f);
                var invert=System.Array.Find(Object.FindObjectsByType<UnityEngine.UI.Toggle>(FindObjectsInactive.Exclude),
                    toggle=>toggle.name=="TouchInvertYValue");
                Assert.IsNotNull(invert);invert.isOn=!invert.isOn;
                Assert.AreEqual(invert.isOn,Settings.SettingsStore.Current.InvertY);
                var safe=canvas.GetComponentInChildren<UiSafeArea>();Assert.IsNotNull(safe);
                safe.Fit(new Rect(90,24,2220,1056),new Vector2(2400,1080));
                Canvas.ForceUpdateCanvases();
                var design=(RectTransform)safe.transform.GetChild(0);safe.KeepDesignInside(design);
                Assert.AreEqual(90f/2400,((RectTransform)safe.transform).anchorMin.x,.0001f);
                Assert.LessOrEqual(design.rect.width*design.localScale.x,((RectTransform)safe.transform).rect.width+.01f);
                Assert.LessOrEqual(design.rect.height*design.localScale.y,((RectTransform)safe.transform).rect.height+.01f);
                Assert.IsTrue(view.Session.Dirty);
                view.Session.Discard();
                Assert.AreEqual(JsonUtility.FromJson<Settings.GameSettings>(saved).MouseSensitivity,Settings.SettingsStore.Current.MouseSensitivity);
            }
            finally { Object.DestroyImmediate(root);Settings.SettingsStore.Restore(JsonUtility.FromJson<Settings.GameSettings>(saved)); }
            yield return PlayModeWorld.Reset();
        }
        [TestCase(960)] [TestCase(1200)] [TestCase(2400)]
        public void EqualScreenFractionProducesEqualLook(float width)
        {
            Assert.Less(Vector2.Distance(new Vector2(60,30),TouchLookArea.LookUnits(new Vector2(width/3,width/6),width)),.0001f);
        }
        [UnityTest] public IEnumerator ForeignPointerCannotMoveOrReleaseLookAndGlobalCancelRetiresIt()
        {
            yield return PlayModeWorld.Reset();
            var root=new GameObject("Look ownership",typeof(RectTransform));var look=root.AddComponent<TouchLookArea>();
            var owner=new PointerEventData(EventSystem.current){pointerId=4,delta=Vector2.right*20};
            var other=new PointerEventData(EventSystem.current){pointerId=8,delta=Vector2.up*200};
            try
            {
                look.OnPointerDown(owner);look.OnPointerDown(other);look.OnDrag(other);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
                look.OnDrag(owner);var value=TouchInput.LookDelta;Assert.Greater(value.x,0);
                look.OnPointerUp(other);Assert.AreEqual(value,TouchInput.LookDelta);
                TouchInput.ReleaseAll();look.OnDrag(owner);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
                look.OnPointerDown(other);look.OnDrag(other);Assert.Greater(TouchInput.LookDelta.y,0);
                look.enabled=false;Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
            }
            finally { Object.DestroyImmediate(root);TouchInput.ReleaseAll(); }
        }
        [UnityTest] public IEnumerator CutoutInsetsEdgeContentWithoutMovingTheAimCentre()
        {
            yield return PlayModeWorld.Reset();
            var owner=new GameObject("Safe viewport fixture");var hud=owner.AddComponent<Hud>();
            var readout=hud.GetComponent<TumpMatchReadout>();var canvas=readout.Canvas;
            var safe=canvas.GetComponentInChildren<UiSafeArea>();
            try
            {
                Canvas.ForceUpdateCanvases();safe.Fit(new Rect(100,30,2160,1050),new Vector2(2400,1080));Canvas.ForceUpdateCanvases();
                var rect=(RectTransform)safe.transform;
                Assert.AreEqual(100f/2400,rect.anchorMin.x,.0001f);Assert.AreEqual(30f/1080,rect.anchorMin.y,.0001f);
                Assert.AreEqual(2260f/2400,rect.anchorMax.x,.0001f);
                var aim=canvas.GetComponentInChildren<HudReticle>();Assert.AreSame(canvas.transform,aim.transform.parent);
                Assert.AreEqual(new Vector2(.5f,.5f),aim.rectTransform.anchorMin);
            }
            finally { Object.DestroyImmediate(owner); }
            yield return PlayModeWorld.Reset();
        }
        [UnityTest] public IEnumerator CurrentKitsUseDragAimOnlyForTheirActualHeldAbilitiesInBothRoles()
        {
            yield return PlayModeWorld.Reset();GameServices.Ensure();bool active=TouchInput.Active;TouchInput.Active=true;
            var body=new GameObject("Mobile current kit policy",typeof(CharacterController));
            var actor=body.AddComponent<CharacterMotor>();actor.enabled=false;actor.PlayerSlot=1;actor.IsPerson=true;
            var system=actor.AbilitySystem??body.AddComponent<Abilities.HeroAbilitySystem>();system.enabled=false;
            var face=new GameObject("Current skill touch",typeof(RectTransform),typeof(CanvasGroup));
            var painter=face.AddComponent<OwnerTouchSurface>();var button=face.AddComponent<TouchButton>();
            var pointer=new PointerEventData(EventSystem.current){pointerId=7,delta=Vector2.right*24};
            int aimed=0,immediate=0;
            try
            {
                foreach(var hero in Core.Roster.HeroPeople)
                {
                    system.BindHero(hero.Id);
                    foreach(bool defending in new[]{false,true})
                    {
                        system.Kit.SetRole(defending,new Abilities.AbilityContext(actor,null,null));
                        foreach(var verb in new[]{Verb.Skill1,Verb.Skill2,Verb.Ultimate})
                        {
                            var ability=verb==Verb.Skill1?system.Kit.Skill1:verb==Verb.Skill2?system.Kit.Skill2:system.Kit.Ultimate;
                            button.BindOwnerPresentation(InputCatalogue.For(verb),face.GetComponent<CanvasGroup>(),painter,null,null);
                            button.RefreshIcon(actor);TouchInput.ReleaseAll();button.OnPointerDown(pointer);button.OnDrag(pointer);
                            Assert.IsTrue(TouchInput.Pressed(verb));
                            if(ability!=null&&ability.HoldToAim){aimed++;Assert.Greater(TouchInput.LookDelta.x,0,hero.Id+" "+verb+" role="+defending);}
                            else {immediate++;Assert.AreEqual(Vector2.zero,TouchInput.LookDelta,hero.Id+" "+verb+" role="+defending);}
                            button.OnPointerUp(pointer);Assert.IsFalse(TouchInput.Pressed(verb));
                        }
                    }
                }
                Assert.Greater(aimed,0);Assert.Greater(immediate,0);
            }
            finally { Object.DestroyImmediate(face);Object.DestroyImmediate(body);TouchInput.ReleaseAll();TouchInput.Active=active; }
            yield return PlayModeWorld.Reset();
        }
    }
}
