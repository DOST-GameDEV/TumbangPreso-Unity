using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativeHudTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator OrdinaryRoundStandingsAreCenteredAndReplaceTheLiveHud()
        {
            yield return Open(GameMode.Classic);
            var card = Object.FindFirstObjectByType<RoleSwapCard>();
            var readout = Object.FindFirstObjectByType<TumpMatchReadout>();
            var view = card.GetComponent<TumpRoundSwapView>();
            Assert.IsNotNull(card); Assert.IsNotNull(readout);
            var scores = (Scoreboard)typeof(MatchDirector).GetField("_scores", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(GameServices.Match);
            scores.SetAll(new[] { int.MaxValue, 999999, 10000, 2670 });
            card.ShowForShot(2, 1);
            yield return null;
            Assert.IsTrue(RoleSwapCard.Showing);
            Assert.IsFalse(readout.Canvas.enabled, "Ordinary breaks must hide the HUD just like halftime.");
            Assert.IsFalse(readout.Canvas.transform.Find("RoundClock/RoundLabel").GetComponent<Text>().enabled);
            yield return new WaitForSecondsRealtime(.3f);
            foreach (var shape in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                yield return TumpUiCapture.Capture("Feedback0930-round-break-" + shape.x + "x" + shape.y,
                    view.Canvas, shape.x, shape.y, false, true, inspectViewport: () =>
                    {
                        var popup = (RectTransform)view.Canvas.transform.Find("CourtBreakPopup");
                        var canvas = (RectTransform)view.Canvas.transform;
                        Assert.Less(Vector3.Distance(popup.TransformPoint(popup.rect.center),
                            canvas.TransformPoint(canvas.rect.center)), .5f, "Standings left screen center.");
                        foreach (var text in view.Canvas.GetComponentsInChildren<Text>().Where(t => t.name.StartsWith("StandingScore")))
                            Assert.LessOrEqual(text.preferredWidth, text.rectTransform.rect.width + .5f,
                                "Score overflow: " + text.text);
                    });
            card.DismissAndPractice();
            yield return null;
            Assert.IsFalse(RoleSwapCard.Showing); Assert.IsTrue(readout.Canvas.enabled);
            card.ShowForShot(2, 1);
            GameServices.Match.AdvanceRound();
            yield return null;
            Assert.IsFalse(RoleSwapCard.Showing); Assert.IsTrue(readout.Canvas.enabled);
        }

        [UnityTest]
        public IEnumerator LargeScoreChipsStayInsideTheBarAtNormalAndAccessibleSizes()
        {
            yield return Open(GameMode.Classic);
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var row = canvas.transform.Find("MatchScores/ScoreRow0") as RectTransform;
            Assert.IsNotNull(row);
            var score = row.Find("Score").GetComponent<Text>();
            var seat = row.Find("SeatTag").GetComponent<Text>();
            var settings = SettingsStore.Current;
            float oldScale = settings.HudScale;
            bool oldLarger = settings.LargerText;
            try
            {
                var cases = new[]
                {
                    (Value: 10000, Name: "five-digit-960x540", Width: 960, Height: 540, Scale: 1f, Larger: false),
                    (Value: -100000, Name: "negative-six-digit-1920x1080", Width: 1920, Height: 1080, Scale: 1f, Larger: false),
                    (Value: -888888, Name: "negative-widest-1280x720", Width: 1280, Height: 720, Scale: 1f, Larger: false),
                    (Value: 999999, Name: "six-digit-phone-2340x1080", Width: 2340, Height: 1080, Scale: 1f, Larger: false),
                    (Value: int.MinValue, Name: "minimum-int-accessible-1600x680", Width: 1600, Height: 680, Scale: 1.2f, Larger: true),
                };
                foreach (var sample in cases)
                {
                    settings.HudScale = sample.Scale;
                    settings.LargerText = sample.Larger;
                    yield return null;
                    yield return TumpUiCapture.Capture("CourtHud-score-" + sample.Name, canvas,
                        sample.Width, sample.Height, false, true, inspectViewport: () =>
                        {
                            score.text = ""; // Stage through the same formatter/fitter used by Scores.
                            TumpMatchReadout.PaintScoreValue(score, sample.Value);
                            score.rectTransform.localScale = Vector3.one * (score.fontSize < 36 ? 1f : 1.07f);
                            Assert.GreaterOrEqual(score.fontSize, 28);
                            Assert.LessOrEqual(score.preferredWidth, score.rectTransform.rect.width + 0.5f,
                                sample.Name + ": score font=" + score.fontSize + " preferred=" + score.preferredWidth +
                                " rect=" + score.rectTransform.rect.width);
                            var corners = new Vector3[4];
                            score.rectTransform.GetWorldCorners(corners);
                            foreach (var corner in corners)
                            {
                                var point = row.InverseTransformPoint(corner);
                                Assert.GreaterOrEqual(point.x, -0.5f);
                                Assert.LessOrEqual(point.x, row.rect.width + 0.5f,
                                    "Score pulse left its chip at " + sample.Name);
                            }
                            float glyphRight = row.InverseTransformPoint(score.rectTransform.TransformPoint(
                                new Vector3(score.rectTransform.rect.xMax, 0f, 0f))).x;
                            float glyphLeft = glyphRight - score.preferredWidth * score.rectTransform.localScale.x;
                            float seatRight = seat.rectTransform.anchoredPosition.x + seat.preferredWidth;
                            Assert.Greater(glyphLeft, seatRight,
                                "A long score covered the seat identity at " + sample.Name);
                        });
                }
            }
            finally
            {
                settings.HudScale = oldScale;
                settings.LargerText = oldLarger;
                score.rectTransform.localScale = Vector3.one;
                score.text = "";
                TumpMatchReadout.PaintScoreValue(score, GameServices.Match.ScoreFor(0));
            }
        }

        [UnityTest]
        public IEnumerator ClassicHudReflectsTheRealRoundRoleAndRecoveryState()
        {
            yield return Open(GameMode.Classic);
            var hud = Hud.Instance; var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            Assert.IsTrue(hud.NativePresentation); Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(4, canvas.GetComponentsInChildren<RectTransform>().Count(r => r.name.StartsWith("ScoreRow")));
            Assert.AreEqual(4, canvas.GetComponentsInChildren<Image>().Count(i => i.name == "PlayerPortrait" && i.enabled && i.sprite != null),
                "A sprite reference alone is not a visible portrait.");
            Assert.IsFalse(canvas.transform.Find("PowerSeals").gameObject.activeSelf, "Classic has no hero power UI.");
            var local = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m => m.PlayerSlot == GameLaunch.SoloSeat);
            foreach (var size in TumpUiCapture.HudViewports)
                yield return TumpUiCapture.Capture("CourtHud-Classic-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, true, checkActionBounds: true);
            // VISUAL-1.4 budget: the permanent HUD in ordinary FPP play stays under about 8
            // percent of a 1920x1080 frame. Measured on the real canvas at that size.
            float share = 1; string detail = "";
            yield return TumpUiCapture.Capture("CourtHud-area-1920x1080", canvas, 1920, 1080, false, true,
                inspectViewport: () => share = TumpUiCapture.HudShare(canvas, out detail));
            Debug.Log($"[HudArea] Classic ordinary play: {share * 100:0.00}% of 1920x1080; {detail}");
            Assert.Less(share, .08f, "Permanent HUD exceeds the VISUAL-1.4 budget: " + detail);
            // The transient layer: pictogram feed, a score pop into a chip, the hit mark and a toast.
            var readout = Object.FindFirstObjectByType<TumpMatchReadout>();
            var lata = GameServices.Round.Lata;
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block, 2, 0, lata.transform.position);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, GameServices.Match.DefenderSlot, (GameServices.Match.DefenderSlot + 2) % 4, lata.transform.position);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.LataDown, 1, -1, lata.transform.position);
            readout.ScorePopForShot(1, ScoreEvent.LataKnocked); readout.Hit(UiTheme.Offense); hud.ShowToast("TAGGED", 2);
            yield return null;
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), TumpUiCapture.OwnerWindow, new Vector2Int(960, 540) })
            {
                readout.ScorePopForShot(1, ScoreEvent.LataKnocked); readout.Hit(UiTheme.Offense);
                yield return TumpUiCapture.Capture("CourtHud-moments-" + size.x + "x" + size.y, canvas, size.x, size.y, false, true, checkActionBounds: true);
            }
            // VISUAL-1.4 accessibility frames: HUD 120 percent and High contrast, same moment.
            var settings = Settings.SettingsStore.Current;
            float scaleBefore = settings.HudScale; bool contrastBefore = settings.HighContrastHud;
            try
            {
                settings.HudScale = 1.2f; settings.HighContrastHud = true; yield return null;
                foreach (var size in new[] { new Vector2Int(1920, 1080), TumpUiCapture.OwnerWindow, new Vector2Int(960, 540) })
                {
                    readout.ScorePopForShot(1, ScoreEvent.LataKnocked); readout.Hit(UiTheme.Offense);
                    yield return TumpUiCapture.Capture("CourtHud-moments-hud120-contrast-" + size.x + "x" + size.y, canvas, size.x, size.y, false, true, checkActionBounds: true);
                }
            }
            finally { settings.HudScale = scaleBefore; settings.HighContrastHud = contrastBefore; }
            yield return null;
            // VISUAL-1.6 reticle states, one frame each: idle cooldown, half charge with left
            // pektus, full charge with right pektus, refused (can protected), taya in reach.
            try
            {
                var states = new (string name, float charge, float spin, float cooldown, bool refused, bool reach)[]
                {
                    ("cooldown", 0, 0, .6f, false, false), ("charge-left", .6f, -.55f, 0, false, false),
                    ("charge-full-right", 1, .9f, 0, false, false), ("refused", .7f, 0, 0, true, false), ("taya-reach", 0, 0, 0, false, true),
                };
                foreach (var state in states)
                {
                    readout.ReticleForShot(state.charge, state.spin, state.cooldown, state.refused, state.reach); yield return null;
                    yield return TumpUiCapture.Capture("CourtHud-reticle-" + state.name + "-1280x720", canvas, 1280, 720, false, true);
                }
            }
            finally { readout.EndReticleShot(); }
            // VISUAL-1.1: an armed attacker inside the box sees the Defense-blue frame, and no sentence.
            var effects = Object.FindFirstObjectByType<TumpHudEffects>();
            var can = GameServices.Round.Lata;
            float protectedUntil = Time.unscaledTime + 3;
            while (can.IsProtected && Time.unscaledTime < protectedUntil) yield return null;
            if (!can.IsUpright) can.HostRestore();
            local.Teleport(can.transform.position + new Vector3(0, 0, -2.5f)); yield return null;
            float frameBy = Time.unscaledTime + 1;
            while (!effects.DangerFrameVisible && Time.unscaledTime < frameBy) yield return null;
            if (local.IsTaggable())
            {
                Assert.IsTrue(effects.DangerFrameVisible, "A taggable attacker must see the danger frame.");
                Assert.AreNotEqual("You can be tagged", canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt").text);
                foreach (var size in new[] { new Vector2Int(1920, 1080), TumpUiCapture.OwnerWindow })
                    yield return TumpUiCapture.Capture("CourtHud-danger-" + size.x + "x" + size.y, canvas, size.x, size.y, false, true);
            }
            else Debug.Log("[HudDanger] staged attacker was not taggable (no slipper in hand); frame capture skipped.");
            bool before = local.IsDefender; local.IsDefender = true; yield return null;
            Assert.AreEqual("Defender", canvas.GetComponentsInChildren<Text>().First(t => t.name == "LocalRole").text);
            local.IsDefender = before;
            local.ApplyFallRecovery(); yield return null;
            var prompt = canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
            Assert.That(prompt.text.ToLowerInvariant(), Does.Contain("get up").Or.Contain("getting up"));
            yield return TumpUiCapture.Capture("CourtHud-recovery", canvas, 960, 540, false, true, checkActionBounds: true);
            local.ClearTrip();
            hud.ShowToast("Slipper returning · 10.0s", 1); yield return null;
            Assert.IsTrue(canvas.GetComponentsInChildren<Text>().First(t => t.name == "MatchToast").enabled);
            var swap = Object.FindFirstObjectByType<RoleSwapCard>();
            swap.ShowForShot(2, (GameServices.Match.DefenderSlot + 1) % 4); yield return null;
            var intermission = GameObject.Find("OwnerRoundSwapCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(intermission.GetComponentsInChildren<PaperSkin>(true));
            Assert.IsNotNull(intermission.GetComponentsInChildren<Image>().First(i=>i.name=="NextDefenderPortrait").sprite);
            foreach(var size in TumpUiCapture.HudViewports)
            {
                swap.ShowForShot(2,(GameServices.Match.DefenderSlot+1)%4);
                yield return TumpUiCapture.Capture("CourtBreak-"+size.x+"x"+size.y,intermission,size.x,size.y,false,true,checkActionBounds:true);
            }
            swap.DismissAndPractice(); Assert.IsFalse(intermission.gameObject.activeSelf);
        }
        [UnityTest]
        public IEnumerator HeroPowerDetailsAndSpectatorCleanFeedKeepTheirLiveContracts()
        {
            yield return Open(GameMode.HeroStrike);
            var hud = Hud.Instance; var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var local = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m => m.PlayerSlot == GameLaunch.SoloSeat);
            Assert.IsTrue(canvas.transform.Find("PowerSeals").gameObject.activeSelf);
            Assert.AreEqual(3, canvas.GetComponentsInChildren<OwnerAbilitySeal>().Length);
            float share = 1; string detail = "";
            yield return TumpUiCapture.Capture("OwnerHud-Hero-v1", canvas, 1920, 1080, false, true,
                inspectViewport: () => share = TumpUiCapture.HudShare(canvas, out detail));
            Debug.Log($"[HudArea] Hero Strike ordinary play: {share * 100:0.00}% of 1920x1080; {detail}");
            Assert.Less(share, .08f, "Permanent Hero Strike HUD exceeds the VISUAL-1.4 budget: " + detail);
            var readout = Object.FindFirstObjectByType<TumpPowerReadout>();
            var kit = local.GetComponent<Abilities.HeroAbilitySystem>().Kit;
            try
            {
                readout.OpenForCapture(kit); yield return null;
                var names = canvas.GetComponentsInChildren<Text>().Where(t => t.name.StartsWith("PowerName")).Select(t => t.text).ToArray();
                CollectionAssert.AreEquivalent(new[] { kit.Skill1.EffectiveName, kit.Skill2.EffectiveName, kit.Ultimate.EffectiveName }, names);
                float duration=kit.Skill1.Duration;
                var setter=typeof(Abilities.HeroAbility).GetProperty("Duration").GetSetMethod(true);
                try
                {
                    setter.Invoke(kit.Skill1,new object[]{duration+1});yield return null;
                    Assert.That(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="PowerTiming0").text,
                        Does.Contain((duration+1).ToString("0.#")+"s duration"),"A live kit must refresh changed timing details.");
                }
                finally{setter.Invoke(kit.Skill1,new object[]{duration});}
                yield return null;
                foreach (var size in TumpUiCapture.HudViewports)
                    yield return TumpUiCapture.Capture("CourtHud-held-skills-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, true, checkActionBounds: true);
            }
            finally { readout.CloseCapture(); }
            hud.EnterSpectatorMode(); yield return null;
            Assert.IsFalse(canvas.transform.Find("PowerSeals").gameObject.activeSelf);
            Assert.IsFalse(canvas.transform.Find("LocalState").gameObject.activeSelf);
            foreach (var size in TumpUiCapture.HudViewports)
                yield return TumpUiCapture.Capture("CourtHud-spectator-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, true, checkActionBounds: true);
            hud.SetCleanFeed(true); Assert.IsFalse(canvas.gameObject.activeSelf);
            hud.SetCleanFeed(false); Assert.IsTrue(canvas.gameObject.activeSelf);
            hud.ExitSpectatorMode(); yield return null;
            Assert.IsTrue(canvas.transform.Find("LocalState").gameObject.activeSelf);
        }
        [UnityTest]
        public IEnumerator RootedPromptTracksLiveBindingsDevicesAndHeldProgress()
        {
            var actions = Resources.Load<InputActionAsset>("TumbangPreso");
            string overrides = actions.SaveBindingOverridesAsJson();
            var oldDevice = LastInputDevice.Current;
            bool touchVisible = TouchHud.ForceVisible, touchActive = TouchInput.Active;
            bool reducedMotion = SettingsStore.Current.ReducedUiMotion;
            var oldMove = TouchInput.Move;
            var inputSettings = InputSystem.settings;
            var background = inputSettings.backgroundBehavior;
            var editorInput = inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pad = InputSystem.AddDevice<Gamepad>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(pad);
            TouchHud.ForceVisible = true;
            SettingsStore.Current.ReducedUiMotion = false;
            try
            {
                yield return Open(GameMode.HeroStrike);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                var reader = local.GetComponent<PlayerInputReader>();
                Assert.IsNotNull(reader);
                Assert.IsTrue(reader.isActiveAndEnabled, "The local seat needs an active hardware reader.");
                Assert.AreSame(actions, typeof(PlayerInputReader).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(reader), "The live binding and the reader must use the same input asset.");
                var interactAction = actions.FindActionMap("Player", true).FindAction("Interact", true);
                var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
                var prompt = canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
                var progress = canvas.GetComponentsInChildren<Image>(true).First(i => i.name == "ProgressFill");
                var glyph = canvas.GetComponentsInChildren<Image>(true).First(i => i.name == "ActionBindingGlyph");
                var touch = Object.FindFirstObjectByType<TouchHud>();
                var interact = touch.Buttons.First(b => b.Entry.Verb == Verb.Interact);
                local.ApplyRooted(45);
                TouchInput.ReleaseAll(); TouchInput.Active = false; TouchInput.Move = Vector2.zero;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F9));
                InputSystem.Update(); keyboard.MakeCurrent(); LastInputDevice.Sample();
                yield return null;
                Assert.That(prompt.text, Does.Contain("Remove Rooted"));
                Assert.IsTrue(glyph.enabled);Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("Interact"),true),glyph.sprite);
                Assert.IsTrue(progress.transform.parent.gameObject.activeSelf);

                Assert.IsNull(Rebinding.TryRebind(actions, "Interact", keyboard.f10Key));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F10));
                yield return new WaitForSeconds(.4f);
                Assert.AreEqual(InputDeviceKind.KeyboardMouse, LastInputDevice.Current);
                Assert.AreEqual("Removing Rooted", prompt.text);
                Assert.IsTrue(glyph.enabled);Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("Interact"),true),glyph.sprite);
                Debug.Log($"[RootedInput] device={keyboard.enabled} key={keyboard.f10Key.isPressed} action={interactAction.enabled}/{interactAction.IsPressed()} reader={reader.isActiveAndEnabled} intent={local.Intent.Pressed(Verb.Interact)} parked={local.Intent.Parked} locked={local.Intent.Locked(Verb.Interact)} held={PresentationClock.Held} local={local.IsLocallySimulated()} rooted={local.IsRooted} progress={local.BreakFreeProgress:F3}");
                Assert.IsTrue(keyboard.enabled && keyboard.f10Key.isPressed, "The synthetic F10 hold did not reach an enabled keyboard.");
                Assert.IsTrue(interactAction.enabled && interactAction.IsPressed(), "The configured Interact action did not read the F10 hold.");
                Assert.IsTrue(local.Intent.Pressed(Verb.Interact), "The reader did not publish an effective Interact hold; see RootedInput diagnostics.");
                Assert.Greater(local.BreakFreeProgress, 0);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null; yield return new WaitForFixedUpdate(); yield return null;
                float released = local.BreakFreeProgress;
                yield return TumpUiCapture.Capture("CourtHud-rooted-keyboard", canvas, 1280, 720, false, true, checkActionBounds: true);
                Assert.AreEqual(released, local.BreakFreeProgress, .001f, "Root escape keeps earned progress when released.");
                Assert.AreEqual(local.BreakFreeProgress, progress.rectTransform.anchorMax.x, .01f);

                Assert.IsNull(Rebinding.TryRebind(actions, "Interact", pad.rightStickButton));
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.RightStick));
                yield return new WaitForSeconds(.4f);
                Assert.AreEqual(InputDeviceKind.Gamepad, LastInputDevice.Current);
                Assert.Greater(local.BreakFreeProgress, released, "The prompted pad control must perform the hold.");
                Assert.AreEqual("Removing Rooted", prompt.text);
                Assert.IsTrue(glyph.enabled);Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("Interact"),true),glyph.sprite);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                yield return TumpUiCapture.Capture("CourtHud-rooted-pad", canvas, 960, 540, false, true, checkActionBounds: true);

                TouchInput.Active = true; TouchInput.Move = Vector2.right; LastInputDevice.Sample();
                TouchInput.Move = Vector2.zero;
                yield return new WaitForSeconds(.15f);
                Assert.AreEqual(InputDeviceKind.Touch, LastInputDevice.Current);
                Assert.AreEqual("Remove Rooted", prompt.text);Assert.IsFalse(glyph.enabled);
                Assert.Greater(interact.transform.localScale.x, 1, "The touch Interact button must be visibly emphasised.");
                float beforeTouch = local.BreakFreeProgress;
                interact.SetHeld(true);
                yield return new WaitForSeconds(.4f);
                interact.SetHeld(false);
                Assert.Greater(local.BreakFreeProgress, beforeTouch, "The prompted touch button must perform the hold.");
                yield return TumpUiCapture.Capture("CourtHud-rooted-touch", canvas, 1280, 720, false, true,
                    underlays: new[] { touch.Canvas }, checkActionBounds: true);

                Hud.Instance.EnterSpectatorMode(); yield return null;
                Assert.IsFalse(prompt.gameObject.activeInHierarchy, "Spectators must never see their former body's interaction prompt.");
                Hud.Instance.ExitSpectatorMode(); yield return null;
                Assert.IsTrue(prompt.gameObject.activeInHierarchy);
                interact.SetHeld(true);
                float deadline = Time.time + PaeteRules.BreakFreeHoldSeconds + .5f;
                while (local.IsRooted && Time.time < deadline) yield return null;
                interact.SetHeld(false); yield return null;
                Assert.IsFalse(local.IsRooted, "Holding the prompted control must finish the escape.");
                Assert.That(prompt.text, Does.Not.Contain("Rooted"));
                Assert.IsFalse(progress.transform.parent.gameObject.activeSelf);
            }
            finally
            {
                actions.LoadBindingOverridesFromJson(overrides); Rebinding.Invalidate(); Rebinding.Save(actions);
                TouchInput.ReleaseAll(); TouchInput.Active = touchActive; TouchInput.Move = oldMove;
                TouchHud.ForceVisible = touchVisible; SettingsStore.Current.ReducedUiMotion = reducedMotion;
                InputSystem.RemoveDevice(pad); InputSystem.RemoveDevice(keyboard);
                inputSettings.backgroundBehavior = background; inputSettings.editorInputBehaviorInPlayMode = editorInput;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { oldDevice });
            }
        }

        [UnityTest]
        public IEnumerator CanResetTouchCueUsesTheActualReachAndRestoreSetting()
        {
            bool visible = TouchHud.ForceVisible, active = TouchInput.Active;
            bool toggle = SettingsStore.Current.ToggleRestore, reduced = SettingsStore.Current.ReducedUiMotion;
            var device = LastInputDevice.Current; var move = TouchInput.Move;
            TouchHud.ForceVisible = true; SettingsStore.Current.ReducedUiMotion = false;
            try
            {
                yield return Open(GameMode.HeroStrike);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled = false; local.Intent.Clear(); local.IsDefender = true;
                var can = GameServices.Round.Lata;
                float protectionDeadline = Time.time + 3;
                while (can.IsProtected && Time.time < protectionDeadline) yield return null;
                can.HostKnockDown((local.PlayerSlot + 1) % 4);
                Assert.IsFalse(can.IsUpright);
                var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
                var prompt = canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
                var touch = Object.FindFirstObjectByType<TouchHud>();
                var grab = touch.Buttons.First(b => b.Entry.Verb == Verb.Grab);
                TouchInput.Active = true; TouchInput.Move = Vector2.right; LastInputDevice.Sample(); TouchInput.Move = Vector2.zero;
                local.Teleport(can.transform.position + Vector3.back * (Balance.InteractionRadius + 1));
                yield return new WaitForSeconds(.35f);
                Assert.AreEqual("Get close to the can to reset", prompt.text);
                Assert.AreEqual(1, grab.transform.localScale.x, .001f, "An unreachable can must not pulse a dead action.");
                local.Teleport(can.transform.position + Vector3.back * (Balance.InteractionRadius * .8f));
                SettingsStore.Current.ToggleRestore = false;
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual("Reset Can", prompt.text);
                Assert.Greater(grab.transform.localScale.x, 1);
                SettingsStore.Current.ToggleRestore = true; yield return null;
                Assert.AreEqual("Reset Can", prompt.text);
                local.Intent.Set(Verb.Grab, true); yield return new WaitForSeconds(.2f);
                Assert.Greater(local.GetComponent<Carrier>().ChannelRatio, 0);
                Assert.AreEqual("Resetting Can · tap to cancel", prompt.text);
                yield return TumpUiCapture.Capture("CourtHud-can-reset-touch", canvas, 1280, 720, false, true,
                    underlays: new[] { touch.Canvas }, checkActionBounds: true);
                local.Intent.Set(Verb.Grab, false); yield return null; yield return new WaitForFixedUpdate(); yield return null;
                Assert.AreEqual(0, local.GetComponent<Carrier>().ChannelRatio);
                Assert.AreEqual("Reset Can", prompt.text);
            }
            finally
            {
                TouchInput.ReleaseAll(); TouchInput.Active = active; TouchInput.Move = move; TouchHud.ForceVisible = visible;
                SettingsStore.Current.ToggleRestore = toggle; SettingsStore.Current.ReducedUiMotion = reduced;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { device });
            }
        }

        [UnityTest]
        public IEnumerator ArmedAttackerInsideTheBoxSeesTheDefenseFrame()
        {
            yield return Open(GameMode.Classic);
            var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
            local.GetComponent<PlayerInputReader>().enabled = false; local.Intent.Clear();
            Assert.IsFalse(local.IsDefender); Assert.IsTrue(local.HoldingSlipper, "Round start arms the attacker.");
            var can = GameServices.Round.Lata;
            local.Teleport(can.transform.position + new Vector3(0, 0, -2.5f)); local.transform.rotation = Quaternion.identity;
            TumpUiCapture.StageHudReview(local);
            var effects = Object.FindFirstObjectByType<TumpHudEffects>();
            float frameBy = Time.unscaledTime + 1;
            while (!effects.DangerFrameVisible && Time.unscaledTime < frameBy) yield return null;
            Assert.IsTrue(local.IsTaggable());
            Assert.IsTrue(effects.DangerFrameVisible, "An armed attacker inside the box must see the frame.");
            Assert.IsFalse(effects.CanDownFrameVisible);
            yield return new WaitForSecondsRealtime(.4f);
            LogCanMarker("attacker");
            yield return TumpUiCapture.Capture("CourtHud-attacker-danger-1280x720",
                GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>(), 1280, 720, false, true, OffscreenUnderlay());
        }

        [UnityTest]
        public IEnumerator TayaWhoseCanIsDownSeesTheOffenseFrameUntilTheReset()
        {
            int seat = GameLaunch.SoloSeat;
            GameLaunch.SoloSeat = MatchRules.DefenderSlotFor(1);
            try
            {
                yield return Open(GameMode.Classic);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled = false; local.Intent.Clear();
                Assert.IsTrue(local.IsDefender, "The local seat is the real round-one taya.");
                var can = GameServices.Round.Lata;
                var effects = Object.FindFirstObjectByType<TumpHudEffects>();
                // Chasing, looking away: the knockdown lands behind the taya.
                local.Teleport(can.transform.position + new Vector3(0, 0, 4)); local.transform.rotation = Quaternion.identity;
                float protectionDeadline = Time.time + 3;
                while (can.IsProtected && Time.time < protectionDeadline) yield return null;
                yield return null;
                Assert.IsFalse(effects.DangerFrameVisible || effects.CanDownFrameVisible, "An upright can frames nothing for the taya.");
                can.HostKnockDown((local.PlayerSlot + 1) % 4);
                Assert.IsFalse(can.IsUpright);
                float frameBy = Time.unscaledTime + 1;
                while (!effects.CanDownFrameVisible && Time.unscaledTime < frameBy) yield return null;
                Assert.IsTrue(effects.CanDownFrameVisible, "The taya whose can is down must see the frame.");
                Assert.IsFalse(effects.DangerFrameVisible, "The taya is never told they can be tagged.");
                yield return new WaitForSecondsRealtime(.4f);
                LogCanMarker("taya");
                yield return TumpUiCapture.Capture("CourtHud-taya-can-down-1280x720",
                    GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>(), 1280, 720, false, true, OffscreenUnderlay());
                can.HostRestore();
                float clearBy = Time.unscaledTime + .5f;
                while (effects.CanDownFrameVisible && Time.unscaledTime < clearBy) yield return null;
                Assert.IsFalse(effects.CanDownFrameVisible, "The frame clears once the can stands again.");
            }
            finally { GameLaunch.SoloSeat = seat; }
        }

        // The edge can marker is its own canvas; the renders show it with the frame.
        private static Canvas[] OffscreenUnderlay()
        {
            var marker = Object.FindFirstObjectByType<OffscreenIndicators>();
            var canvas = marker != null ? marker.GetComponentInChildren<Canvas>(true) : null;
            return canvas != null ? new[] { canvas } : null;
        }

        private static void LogCanMarker(string viewer)
        {
            var marker = Object.FindFirstObjectByType<OffscreenIndicators>();
            Debug.Log($"[CanMarker] {viewer}: present={marker != null} visible={marker != null && marker.CanMarkerVisible} " +
                      $"state={(marker != null ? marker.CanMarkerState : "")}");
        }

        [UnityTest]
        public IEnumerator PlantPromptRequiresAnEligibleOpponentPlantAndTracksThePull()
        {
            yield return Open(GameMode.HeroStrike);
            var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
            local.GetComponent<PlayerInputReader>().enabled = false;
            local.Intent.Clear(); local.Intent.Parked = false;
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var prompt = canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
            var progress = canvas.GetComponentsInChildren<Image>(true).First(i => i.name == "ProgressFill");
            Vector3 near = local.transform.position + Vector3.forward;
            int enemy = (local.PlayerSlot + 1) % 4;
            var plant = PaetePlant.Restore(near, local.PlayerSlot, PaeteRules.PlantRootedSeconds + 1, 3);
            yield return null;
            Assert.That(prompt.text, Does.Not.Contain("pull it out"), "The owner cannot uproot their own plant.");
            Object.Destroy(plant.gameObject); yield return null;
            plant = PaetePlant.Restore(near, enemy, 1, 3);
            yield return null;
            Assert.That(prompt.text, Does.Not.Contain("pull it out"), "The initial protected period has no available pull action.");
            Object.Destroy(plant.gameObject); yield return null;
            plant = PaetePlant.Restore(local.transform.position + Vector3.forward * (PaeteRules.PlantPullReach + .1f),
                enemy, PaeteRules.PlantRootedSeconds + 1, 3);
            yield return null;
            Assert.That(prompt.text, Does.Not.Contain("pull it out"), "The host's network tolerance is not local interaction reach.");
            plant.transform.position = near;
            yield return null;
            Assert.That(prompt.text, Does.Contain("pull it out"));
            local.Intent.Set(Verb.Interact, true);
            yield return new WaitForSeconds(.35f);
            Assert.Greater(local.PullingPlantProgress, 0);
            Assert.AreEqual(local.PullingPlantProgress, progress.rectTransform.anchorMax.x, .05f);
            local.Intent.Set(Verb.Interact, false);
            yield return null; yield return null;
            Assert.AreEqual(0, local.PullingPlantProgress, .001f, "Unlike root escape, releasing an uproot resets its progress.");
            Assert.IsFalse(progress.enabled);
            yield return TumpUiCapture.Capture("CourtHud-pull-available", canvas, 1280, 720, false, true, checkActionBounds: true);
            local.Intent.Set(Verb.Interact, true);
            float deadline = Time.time + PaeteRules.PlantPullSeconds + .5f;
            while (plant != null && !plant.IsPulled && Time.time < deadline) yield return null;
            local.Intent.Set(Verb.Interact, false); yield return null;
            Assert.IsTrue(plant == null || plant.IsPulled);
            Assert.That(prompt.text, Does.Not.Contain("pull it out"));
            Assert.IsFalse(progress.transform.parent.gameObject.activeSelf);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ReadyUpUsesLargeLiveXeluBindingsAndClearsAfterTheReadyWindow()
        {
            var actions = Resources.Load<InputActionAsset>("TumbangPreso");
            string overrides = actions.SaveBindingOverridesAsJson();
            var device = LastInputDevice.Current;
            bool touch = TouchInput.Active, force = TouchHud.ForceVisible;
            int mip = QualitySettings.globalTextureMipmapLimit;
            var keys = InputSystem.AddDevice<Keyboard>(); var pad = InputSystem.AddDevice<Gamepad>();
            QualitySettings.globalTextureMipmapLimit = 2;
            try
            {
                TouchInput.Active = false; TouchHud.ForceVisible = false;
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
                Hud.Instance.ShowReadyPrompt(true);
                var hud = Object.FindAnyObjectByType<TumpMatchReadout>();
                var prompt = hud.Canvas.GetComponentsInChildren<Text>(true).First(t => t.name == "ActionPrompt");
                var detail = hud.Canvas.GetComponentsInChildren<Text>(true).First(t => t.name == "ActionDetail");
                var glyph = hud.Canvas.GetComponentsInChildren<Image>(true).First(t => t.name == "ActionBindingGlyph");
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.KeyboardMouse });
                Assert.IsNull(Rebinding.TryRebind(actions, "ReadyUp", keys.f10Key));
                yield return null;
                Assert.AreEqual("Ready Up", prompt.text); Assert.IsEmpty(detail.text);
                Assert.IsTrue(glyph.enabled); Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("ReadyUp"), true), glyph.sprite);
                Assert.That(Hud.KeyLabelFor("ReadyUp"), Does.Contain("F10"));
                Assert.AreEqual(64, glyph.rectTransform.rect.height);
                foreach (var shape in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                    yield return TumpUiCapture.Capture("Feedback-ready-up-" + shape.x + "x" + shape.y, hud.Canvas, shape.x, shape.y, false, true);
                Assert.IsNull(Rebinding.TryRebind(actions, "ReadyUp", pad.rightStickButton));
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.Gamepad });
                yield return null;
                Assert.IsTrue(glyph.enabled); Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("ReadyUp"), true), glyph.sprite);
                Assert.AreEqual("Ready Up", prompt.text); Assert.IsEmpty(detail.text);
                TouchInput.Active = true;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.Touch });
                yield return null;
                Assert.AreEqual("Ready Up", prompt.text); Assert.IsFalse(glyph.enabled); Assert.IsEmpty(detail.text);
                TouchInput.Active = false;
                Hud.Instance.ShowReadyPrompt(false); yield return null;
                Assert.IsFalse(glyph.enabled); Assert.That(prompt.text, Does.Not.Contain("Ready Up"));
                Assert.AreEqual(1100, prompt.rectTransform.rect.width, "Non-ready action prompts keep their original layout.");
                Hud.Instance.EnterSpectatorMode(); yield return null;
                Assert.IsFalse(prompt.gameObject.activeInHierarchy);
            }
            finally
            {
                actions.LoadBindingOverridesFromJson(overrides); Rebinding.Invalidate(); Rebinding.Save(actions);
                TouchInput.Active = touch; TouchHud.ForceVisible = force;
                InputSystem.RemoveDevice(keys); InputSystem.RemoveDevice(pad);
                QualitySettings.globalTextureMipmapLimit = mip;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { device });
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LargerPowerControlsPreserveMarginsBindingsAndFinalizedKitText()
        {
            int mip = QualitySettings.globalTextureMipmapLimit;
            float hudScale = SettingsStore.Current.HudScale; bool larger = SettingsStore.Current.LargerText;
            var device = LastInputDevice.Current; bool touch = TouchInput.Active;
            QualitySettings.globalTextureMipmapLimit = 2;
            SettingsStore.Current.HudScale = 1; SettingsStore.Current.LargerText = false;
            try
            {
                TouchInput.Active = false;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.KeyboardMouse });
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                var system = local.GetComponent<HeroAbilitySystem>();
                var readout = Object.FindAnyObjectByType<TumpPowerReadout>();
                var hud = Object.FindAnyObjectByType<TumpMatchReadout>();
                var deck = (RectTransform)hud.Canvas.transform.Find("PowerSeals");
                foreach (string hero in new[] { "paete", "phaister", "zack" })
                {
                    system.BindHero(hero);
                    yield return new WaitForSecondsRealtime(TumpPowerReadout.RoleSwapSeconds + .1f);
                    var kit = system.Kit;
                    var skills = new[] { kit.Skill1, kit.Skill2, kit.Ultimate };
                    var names = skills.Select(a => a.EffectiveName).ToArray();
                    var descriptions = skills.Select(a => a.EffectiveDescription).ToArray();
                    yield return null;
                    Assert.AreEqual(Vector3.one * 1.25f, deck.localScale);
                    Assert.AreEqual(new Vector2(-40, 30), deck.anchoredPosition);
                    Assert.AreEqual(316 * 1.25f, readout.DeckRect().width, .1f);
                    CollectionAssert.AreEqual(names, skills.Select(a => a.EffectiveName).ToArray());
                    CollectionAssert.AreEqual(descriptions, skills.Select(a => a.EffectiveDescription).ToArray());
                    var glyphs = deck.GetComponentsInChildren<Image>().Where(i => i.name == "BindingGlyph").ToArray();
                    Assert.AreEqual(3, glyphs.Length); Assert.IsTrue(glyphs.All(g => g.enabled && g.sprite != null));
                    if (hero == "paete")
                    {
                        var sandbox = hud.Canvas.transform.Find("SandboxState") as RectTransform;
                        var hint = deck.Find("PowerInfoBinding") as RectTransform;
                        var corners = new Vector3[4]; sandbox.GetWorldCorners(corners); float bottom = corners[0].y;
                        hint.GetWorldCorners(corners); Assert.Greater(bottom, corners[1].y, "Practice status must clear the enlarged power hints.");
                        foreach (var shape in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                            yield return TumpUiCapture.Capture("Feedback-larger-powers-" + shape.x + "x" + shape.y, hud.Canvas, shape.x, shape.y, false, true);
                    }
                }
                SettingsStore.Current.HudScale = 1.2f; yield return null;
                Assert.AreEqual(1.5f, deck.localScale.x, .001f);
                Assert.AreEqual(new Vector2(-40, 30), deck.anchoredPosition);
                TouchInput.Active = true;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.Touch });
                yield return null;
                Assert.AreEqual(new Vector2(.5f, 0), deck.anchorMin);
                Assert.AreEqual(new Vector2(0, 34), deck.anchoredPosition);
                Assert.IsTrue(deck.GetComponentsInChildren<Image>(true).Where(i => i.name == "BindingGlyph").All(i => !i.enabled));
                readout.Tick(system, false); Assert.IsFalse(readout.DeckVisible);
            }
            finally
            {
                QualitySettings.globalTextureMipmapLimit = mip;
                SettingsStore.Current.HudScale = hudScale; SettingsStore.Current.LargerText = larger;
                TouchInput.Active = touch;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { device });
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator DeviceChangesThenHudScalingKeepTheAuthoredPowerMargins()
        {
            int mip = QualitySettings.globalTextureMipmapLimit;
            float scale = SettingsStore.Current.HudScale; bool larger = SettingsStore.Current.LargerText;
            var device = LastInputDevice.Current; bool touch = TouchInput.Active;
            try
            {
                QualitySettings.globalTextureMipmapLimit = 2;
                SettingsStore.Current.HudScale = 1; SettingsStore.Current.LargerText = false;
                TouchInput.Active = false;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.KeyboardMouse });
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
                var hud = Object.FindAnyObjectByType<TumpMatchReadout>();
                var deck = (RectTransform)hud.Canvas.transform.Find("PowerSeals");
                var scores = (RectTransform)hud.Canvas.transform.Find("MatchScores");
                Vector2 scorePosition = scores.anchoredPosition;
                foreach (bool useTouch in new[] { true, false, true, false })
                {
                    TouchInput.Active = useTouch;
                    typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { useTouch ? InputDeviceKind.Touch : InputDeviceKind.KeyboardMouse });
                    yield return null; yield return null;
                    Vector2 expected = useTouch ? new Vector2(0, 34) : new Vector2(-40, 30);
                    Assert.AreEqual(expected, deck.anchoredPosition);
                    foreach (float next in new[] { 1.2f, 1f })
                    {
                        SettingsStore.Current.HudScale = next;
                        yield return null; yield return null;
                        Assert.AreEqual(expected, deck.anchoredPosition, "Scaling after a device switch restored a stale power anchor.");
                        Assert.AreEqual(1.25f * next, deck.localScale.x, .001f);
                    }
                    Assert.AreEqual(scorePosition, scores.anchoredPosition, "Rebasing powers must not accumulate offsets in other groups.");
                }
            }
            finally
            {
                QualitySettings.globalTextureMipmapLimit = mip;
                SettingsStore.Current.HudScale = scale; SettingsStore.Current.LargerText = larger;
                TouchInput.Active = touch;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { device });
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator RetrieveAndResetUseTheirLiveGlyphsAndKeepRealActions()
        {
            var actions = Resources.Load<InputActionAsset>("TumbangPreso");
            string overrides = actions.SaveBindingOverridesAsJson(); var device = LastInputDevice.Current;
            bool touch = TouchInput.Active, toggle = SettingsStore.Current.ToggleRestore;
            int mip = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = 2;
            var keys = InputSystem.AddDevice<Keyboard>(); var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                TouchInput.Active = false; SettingsStore.Current.ToggleRestore = false;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.KeyboardMouse });
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
                Hud.Instance.ShowReadyPrompt(false);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat); local.Intent.Parked = false;
                var shoe = local.GetComponent<Carrier>().Held; Assert.IsNotNull(shoe);
                Assert.IsTrue(shoe.HostDisarm()); shoe.HostScatter(local.transform.position + Vector3.forward * .4f - shoe.transform.position);
                var hud = Object.FindAnyObjectByType<TumpMatchReadout>();
                var text = hud.Canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
                var glyph = hud.Canvas.GetComponentsInChildren<Image>(true).First(i => i.name == "ActionBindingGlyph");
                Assert.IsNull(Rebinding.TryRebind(actions, "Grab", keys.f10Key));
                yield return new WaitForSeconds(.25f);
                Assert.IsTrue(shoe.CanBeGrabbedBy(local)); Assert.AreEqual("Retrieve Slipper", text.text);
                Assert.IsTrue(glyph.enabled); Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("Grab"), true), glyph.sprite);
                Assert.That(Hud.KeyLabelFor("Grab"), Does.Contain("F10"));
                yield return TumpUiCapture.Capture("Feedback-retrieve-xelu", hud.Canvas, 960, 540, false, true);
                // Write the synthetic edge after the physics snapshot, in the same window
                // as PlayerInputReader.Update, so Carrier.Update can observe the press.
                yield return new WaitForFixedUpdate();
                local.Intent.Set(Verb.Grab, true); yield return new WaitForSeconds(.15f); local.Intent.Set(Verb.Grab, false);
                Assert.IsTrue(local.HoldingSlipper, "The pictured retrieve action must still pick up the real shoe.");
                yield return null; Assert.IsFalse(glyph.enabled); Assert.That(text.text, Does.Not.Contain("Retrieve Slipper"));
                var defender = GameServices.Round.Players.First(p => p.IsDefender); defender.Intent.Parked = false;
                var can = GameServices.Round.Lata;
                float until = Time.time + 4; while (can.IsProtected && Time.time < until) yield return null;
                can.HostKnockDown(local.PlayerSlot); Assert.IsFalse(can.IsUpright);
                defender.Teleport(can.transform.position + Vector3.back * .7f); Hud.Instance.Bind(defender);
                Assert.IsNull(Rebinding.TryRebind(actions, "Grab", pad.rightStickButton));
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.Gamepad });
                yield return null;
                Assert.AreEqual("Reset Can", text.text); Assert.IsTrue(glyph.enabled);
                Assert.AreSame(InputGlyphs.For(Hud.KeyLabelFor("Grab"), true), glyph.sprite);
                yield return TumpUiCapture.Capture("Feedback-reset-xelu", hud.Canvas, 1600, 680, false, true);
                defender.Intent.Set(Verb.Grab, true); yield return new WaitForSeconds(.2f);
                Assert.Greater(defender.GetComponent<Carrier>().ChannelRatio, 0); Assert.AreEqual("Resetting Can", text.text);
                Assert.IsFalse(glyph.enabled, "The old idle glyph must not leak into channel feedback.");
                yield return TumpUiCapture.Capture("Feedback-resetting-can-caption", hud.Canvas, 960, 540, false, true);
                defender.Intent.Set(Verb.Grab, false); yield return null; yield return new WaitForFixedUpdate(); yield return null;
                Assert.AreEqual("Reset Can", text.text); Assert.IsTrue(glyph.enabled);
                TouchInput.Active = true;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { InputDeviceKind.Touch });
                yield return null; Assert.AreEqual("Reset Can", text.text); Assert.IsFalse(glyph.enabled);
                defender.Intent.Set(Verb.Grab, true); until = Time.time + 4;
                while (!can.IsUpright && Time.time < until) yield return null;
                defender.Intent.Set(Verb.Grab, false); Assert.IsTrue(can.IsUpright); yield return null;
                Assert.That(text.text, Does.Not.Contain("Reset Can")); Assert.IsFalse(glyph.enabled);
            }
            finally
            {
                actions.LoadBindingOverridesFromJson(overrides); Rebinding.Invalidate(); Rebinding.Save(actions);
                InputSystem.RemoveDevice(keys); InputSystem.RemoveDevice(pad);
                TouchInput.Active = touch; SettingsStore.Current.ToggleRestore = toggle; QualitySettings.globalTextureMipmapLimit = mip;
                typeof(LastInputDevice).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { device });
            }
        }

        [UnityTest,Timeout(90000)]
        public IEnumerator RevisedHudSeparatesStatusesWarningActionAndFitsFourRounds()
        {
            var settings=SettingsStore.Current;float scale=settings.HudScale;var rules=SceneFlow.SelectedRules.Clone();
            try
            {
                yield return Open(GameMode.HeroStrike);
                var local=GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled=false;local.Intent.Clear();local.enabled=false;
                GameServices.Round.enabled=false;
                local.ApplyWhirled(30);local.ApplyChilled(30);local.ApplyRooted(30);
                var idle=new float[4];idle[local.PlayerSlot]=8;GameServices.Round.ApplyNetworkTournamentState(0,idle);
                var four=SceneFlow.SelectedRules.Clone();four.Rounds=4;SceneFlow.SetSelectedRules(four);
                var view=Object.FindFirstObjectByType<TumpMatchReadout>();var root=(RectTransform)view.Canvas.transform;
                foreach(float size in new[]{1f,1.2f})
                {
                    settings.HudScale=size;view.Tick(local,false,false,false,false);yield return null;
                    Assert.AreEqual("DO NOT IDLE - RETRIEVE YOUR SLIPPER",view.WarningText);
                    Assert.AreEqual(3,root.GetComponentsInChildren<RectTransform>().Count(x=>x.name.StartsWith("StatusChip")));
                    Assert.AreEqual(106,((RectTransform)root.Find("RoundClock/RoundTrack")).rect.width,.1f);
                    Assert.AreEqual(1.1f*size,root.Find("MatchScores").localScale.x,.001f);
                    Assert.IsFalse(root.GetComponentsInChildren<Text>(true).First(x=>x.name=="PowerInfoBinding").enabled);
                    foreach(var viewport in new[]{new Vector2Int(960,540),new Vector2Int(1600,680)})
                    {
                        var match=GameServices.Match;
                        typeof(MatchDirector).GetMethod("PresentHostMoment",BindingFlags.Instance|BindingFlags.NonPublic)
                            .Invoke(match,new object[]{local.PlayerSlot,MatchMomentKind.FirstKnockdown,1,0});
                        yield return TumpUiCapture.Capture("HarryHud-states-"+size+"-"+viewport.x+"x"+viewport.y,view.Canvas,viewport.x,viewport.y,false,true,
                            inspectViewport:()=>
                            {
                                var action=HudRevisionBounds(root,(RectTransform)root.Find("ContextualAction/PromptPlate"));
                                var warning=HudRevisionBounds(root,(RectTransform)root.Find("WarningMessage"));
                                var progress=HudRevisionBounds(root,(RectTransform)root.Find("ContextualAction/RecoveryProgress"));
                                Assert.IsTrue(action.Contains(progress.min)&&action.Contains(progress.max),"Progress must sit inside the action background");
                                Assert.IsFalse(action.Overlaps(warning));
                                var shownChips = root.GetComponentsInChildren<RectTransform>().Where(x => x.name.StartsWith("StatusChip")).ToArray();
                                for(int i=0;i<shownChips.Length;i++)
                                {
                                    var chip=HudRevisionBounds(root,shownChips[i]);
                                    Assert.IsFalse(chip.Overlaps(action));Assert.IsFalse(chip.Overlaps(warning));
                                    Assert.GreaterOrEqual(chip.xMin,root.rect.xMin);Assert.GreaterOrEqual(chip.yMin,root.rect.yMin);
                                    if(i>0)Assert.IsFalse(chip.Overlaps(HudRevisionBounds(root,shownChips[i-1])));
                                }
                            });
                    }
                }
                local.ClearStatuses();
                GameServices.Round.ApplyNetworkTournamentState(0,new float[4]);view.Tick(local,false,false,false,false);
                Assert.AreEqual("",view.WarningText);
            }
            finally{settings.HudScale=scale;SceneFlow.SetSelectedRules(rules);}
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator RequirementRefusalReachesWarningWithoutCastingAndFeedStaysSeparate()
        {
            float scale=SettingsStore.Current.HudScale;
            try
            {
                yield return Open(GameMode.HeroStrike);
                var round=GameServices.Round;var local=round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled=false;local.Intent.Clear();local.Intent.Parked=false;
                local.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");
                var powers=local.AbilitySystem;powers.BindHero("rafi");powers.enabled=false;
                local.GetComponent<Carrier>().Held?.HostDisarm();local.GetComponent<Carrier>().enabled=false;
                local.enabled=false;round.enabled=false;Assert.IsTrue(local.CanAct());
                var update=typeof(HeroAbilitySystem).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
                local.Intent.Set(Verb.Skill2,true);update.Invoke(powers,null);
                yield return new WaitForSeconds(HeroAbilitySystem.InputBufferWindow+.05f);
                update.Invoke(powers,null);
                Assert.AreEqual(HeroKit.CastOutcome.CannotAct,powers.LastAnswer(HeroAbilitySystem.Slot.Skill2));
                Assert.IsFalse(powers.Kit.Skill2.IsActive);Assert.AreEqual(0,powers.Kit.Skill2.CooldownRemaining);
                var view=Object.FindFirstObjectByType<TumpMatchReadout>();view.Tick(local,false,false,false,false);
                Assert.AreEqual("CANNOT CAST - ABILITY MUST MEET REQUIREMENTS",view.WarningText);
                SettingsStore.Current.HudScale=1.2f;yield return null;
                foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(1600,680)})
                {
                    // Repeating this unsuccessful input supplies a fresh real refusal after
                    // the first view's capture, without awarding or activating anything.
                    local.Intent.Set(Verb.Skill2,false);update.Invoke(powers,null);
                    local.Intent.Set(Verb.Skill2,true);update.Invoke(powers,null);
                    yield return new WaitForSeconds(HeroAbilitySystem.InputBufferWindow+.05f);update.Invoke(powers,null);
                    Visual.MatchFlair.Play(Visual.MatchFlair.Kind.LataDown,2,-1,round.Lata.transform.position);
                    Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag,0,3,round.Lata.transform.position);
                    Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block,2,1,round.Lata.transform.position);
                    yield return TumpUiCapture.Capture("HarryHud-refusal-feed-"+size.x+"x"+size.y,view.Canvas,size.x,size.y,false,true,
                        inspectViewport:()=>
                        {
                            var root=(RectTransform)view.Canvas.transform;
                            var warning=HudRevisionBounds(root,(RectTransform)root.Find("WarningMessage"));
                            foreach(var plate in root.Find("MatchEventFeed").GetComponentsInChildren<HudCard>())
                                if(plate.name=="EventPlate")Assert.IsFalse(warning.Overlaps(HudRevisionBounds(root,plate.rectTransform)));
                        });
                }
                local.Intent.Set(Verb.Skill2,false);update.Invoke(powers,null);
                var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.OwnerSlot==local.PlayerSlot);
                shoe.HostForceEquip(local);local.Intent.Set(Verb.Skill2,true);update.Invoke(powers,null);
                Assert.AreEqual(HeroKit.CastOutcome.Cast,powers.LastAnswer(HeroAbilitySystem.Slot.Skill2));
                view.Tick(local,false,false,false,false);Assert.AreEqual("",view.WarningText,"A successful corrected cast clears its old refusal");
            }
            finally{SettingsStore.Current.HudScale=scale;}
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ScoreFeedReflowsWithoutResettingExpiryAndHonoursReducedMotion()
        {
            bool reduced = SettingsStore.Current.ReducedUiMotion;
            try
            {
                yield return Open(GameMode.Classic);
                SettingsStore.Current.ReducedUiMotion = false;
                var feed = Object.FindFirstObjectByType<MatchEventFeed>();
                Assert.IsNotNull(feed);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(MatchEventFeed).GetMethod("Clear", flags).Invoke(feed, null);
                var at = GameServices.Round.Lata.transform.position;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.LataDown, 1, -1, at);
                yield return new WaitForSecondsRealtime(.22f);
                var first = feed.Entry(0);
                var itemsField = typeof(MatchEventFeed).GetField("_items", flags);
                var items = (System.Array)itemsField.GetValue(feed);
                float firstExpiry = (float)items.GetValue(0).GetType().GetField("Expires").GetValue(items.GetValue(0));
                var row0 = (RectTransform)feed.transform.Find("Event0");
                float oldY = row0.anchoredPosition.y;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, 0, 2, at);
                var row1 = (RectTransform)feed.transform.Find("Event1");
                Assert.AreEqual(oldY, row1.anchoredPosition.y, .1f, "Existing entry must start from its current position.");
                Assert.AreEqual(first, feed.Entry(1));
                yield return new WaitForSecondsRealtime(.08f);
                Assert.That(row1.anchoredPosition.y, Is.LessThan(oldY).And.GreaterThan(-60));
                float interruptedY = row1.anchoredPosition.y;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block, 3, 2, at);
                var row2 = (RectTransform)feed.transform.Find("Event2");
                Assert.AreEqual(interruptedY, row2.anchoredPosition.y, .1f, "An interrupted insertion must continue from the visible position.");
                items = (System.Array)itemsField.GetValue(feed);
                Assert.AreEqual(firstExpiry, (float)items.GetValue(2).GetType().GetField("Expires").GetValue(items.GetValue(2)));
                Assert.AreEqual(3, feed.Count);
                yield return new WaitForSecondsRealtime(.22f);
                Assert.AreEqual(-60, row1.anchoredPosition.y, .1f); Assert.AreEqual(-120, row2.anchoredPosition.y, .1f);
                var view = Object.FindFirstObjectByType<TumpMatchReadout>();
                yield return TumpUiCapture.Capture("HarryHud-feed-stack-960x540", view.Canvas, 960, 540, false, true);
                SettingsStore.Current.ReducedUiMotion = true;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, 0, 1, at);
                Assert.AreEqual(3, feed.Count);
                Assert.AreEqual(0, row0.anchoredPosition.y, .001f); Assert.AreEqual(-60, row1.anchoredPosition.y, .001f);
                items = (System.Array)itemsField.GetValue(feed);
                var newest = items.GetValue(0); var type = newest.GetType();
                float born = (float)type.GetField("Born").GetValue(newest);
                float expiry = (float)type.GetField("Expires").GetValue(newest);
                Assert.AreEqual(3, expiry - born, .001f);
                typeof(MatchEventFeed).GetMethod("Paint", flags).Invoke(feed, new object[] { expiry + .01f });
                Assert.Zero(feed.Count, "Every entry must expire without a new event.");
            }
            finally { SettingsStore.Current.ReducedUiMotion = reduced; }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator DenseStatusesRetainHauntedTimersAndReflowWithoutOverlap()
        {
            var settings = SettingsStore.Current; float originalScale = settings.HudScale; bool reduced = settings.ReducedUiMotion;
            try
            {
                yield return Open(GameMode.HeroStrike);
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled = false; local.Intent.Clear(); local.enabled = false;
                GameServices.Round.enabled = false;
                local.ApplyWhirled(30); local.ApplyChilled(30); local.ApplyRooted(30);
                local.ApplyConcussed(30); local.ApplyFeared(Vector3.zero,30); local.ApplyDisoriented(30);
                local.ApplyVulnerable(30); local.ApplyDrained(30); local.ApplyHexed(30); local.ApplyHaunted();
                var idle = new float[4]; idle[local.PlayerSlot] = 8; GameServices.Round.ApplyNetworkTournamentState(0,idle);
                var view = Object.FindFirstObjectByType<TumpMatchReadout>(); var root = (RectTransform)view.Canvas.transform;
                var live = new System.Collections.Generic.List<StatusKind>(); StatusIcons.Live(local,live);
                Assert.AreEqual(10,live.Count); Assert.Contains(StatusKind.Haunted,live);
                Assert.IsNotNull(StatusIcons.For(StatusKind.Haunted), "Haunted must import and bind its own icon.");
                settings.ReducedUiMotion = true;
                foreach(float scale in new[]{1f,1.2f})
                foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(1600,680)})
                {
                    settings.HudScale = scale; view.Tick(local,false,false,false,false); yield return null;
                    yield return TumpUiCapture.Capture("HarryHud-dense-status-"+scale+"-"+size.x+"x"+size.y,view.Canvas,size.x,size.y,false,true,
                        inspectViewport:()=>
                        {
                            view.Tick(local,false,false,false,false);
                            typeof(TumpMatchReadout).GetMethod("SizePromptPlate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);
                            var chips = root.GetComponentsInChildren<RectTransform>().Where(x=>x.name.StartsWith("StatusChip")).ToArray();
                            Assert.AreEqual(10,chips.Length);
                            var warning = HudRevisionBounds(root,(RectTransform)root.Find("WarningMessage"));
                            var action = HudRevisionBounds(root,(RectTransform)root.Find("ContextualAction/PromptPlate"));
                            foreach(var chip in chips)
                            {
                                var bounds = HudRevisionBounds(root,chip);
                                Assert.IsTrue(root.rect.Contains(bounds.min)&&root.rect.Contains(bounds.max),chip.name+" must stay on screen");
                                Assert.IsFalse(bounds.Overlaps(warning),chip.name+" overlaps warning");
                                if(root.Find("ContextualAction/PromptPlate").GetComponent<HudCard>().enabled)
                                    Assert.IsFalse(bounds.Overlaps(action),chip.name+" overlaps action");
                                foreach(var other in chips)if(other!=chip)Assert.IsFalse(bounds.Overlaps(HudRevisionBounds(root,other)));
                            }
                        });
                }
                local.ClearStatuses(); local.ClearStun(); local.ApplyWhirled(3); local.ApplyChilled(3); local.ApplyHaunted(1);
                settings.HudScale=1; settings.ReducedUiMotion=false;
                view.Tick(local,false,false,false,false); yield return new WaitForSecondsRealtime(.22f); view.Tick(local,false,false,false,false);
                var haunted = root.GetComponentsInChildren<Text>().First(x=>x.name=="StatusName"&&x.text=="HAUNTED").transform.parent as RectTransform;
                var before = haunted.anchoredPosition;
                local.ApplyHaunted(1); view.Tick(local,false,false,false,false);
                Assert.AreEqual(before,haunted.anchoredPosition,"Refreshing the same effect must not restart entry animation");
                local.ApplyNetworkStatuses(0,3,0,.5f); view.Tick(local,false,false,false,false);
                Assert.AreEqual(before,haunted.anchoredPosition,"Reflow begins at the existing position");
                yield return new WaitForSecondsRealtime(.22f); view.Tick(local,false,false,false,false);
                Assert.Less(haunted.anchoredPosition.y,before.y);
                Assert.AreEqual(.5f/StatusRules.HauntedSeconds,haunted.GetComponentInChildren<HudRing>().Fill,.001f);
                local.ApplyNetworkStatuses(0,0,0,0); view.Tick(local,false,false,false,false);
                Assert.IsFalse(haunted.gameObject.activeSelf,"Expired statuses disappear from actual shared timers");
            }
            finally { settings.HudScale=originalScale; settings.ReducedUiMotion=reduced; }
        }

        private static Rect HudRevisionBounds(RectTransform root,RectTransform target)
        {
            var points=new Vector3[4];target.GetWorldCorners(points);
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var point in points){Vector2 p=root.InverseTransformPoint(point);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator WarningStripIsThinSingleLineAndMatchesActionOpacity()
        {
            float oldScale = SettingsStore.Current.HudScale;
            try
            {
                yield return Open(GameMode.Classic);
                yield return new WaitForSeconds(1.4f);
                var round = GameServices.Round; round.enabled = false;
                var local = round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled = false;
                local.GetComponent<Carrier>().enabled = false;
                local.enabled = false; local.Intent.Clear();
                local.Teleport(new Vector3(0, .1f, 2));
                local.Intent.Set(Verb.SpecialAbility, true);
                var view = Object.FindFirstObjectByType<TumpMatchReadout>();
                var root = (RectTransform)view.Canvas.transform;
                foreach (float scale in new[] { 1f, 1.2f })
                {
                    SettingsStore.Current.HudScale = scale;
                    view.Tick(local, false, false, false, false);
                    Assert.That(view.WarningText, Does.Contain("OUTSIDE DANGER ZONE"));
                    foreach (var size in new[] { new Vector2Int(960, 540), new Vector2Int(1600, 680) })
                    {
                        Canvas.ForceUpdateCanvases();
                        var warning = (RectTransform)root.Find("WarningMessage");
                        Assert.LessOrEqual(warning.rect.height, 64, "Ordinary warnings should be a thin strip.");
                        yield return TumpUiCapture.Capture("Warning-strip-" + scale + "-" + size.x,
                            view.Canvas, size.x, size.y, false, false, inspectViewport: () =>
                            {
                                var text = warning.GetComponentInChildren<Text>();
                                Assert.AreEqual(1, text.cachedTextGenerator.lineCount, "The warning should fit one readable line.");
                                Assert.GreaterOrEqual(text.fontSize, 28);
                                Assert.LessOrEqual(text.preferredWidth, text.rectTransform.rect.width + 1);
                                var action = root.Find("ContextualAction/PromptPlate").GetComponent<HudCard>();
                                Assert.AreEqual(action.color.a, warning.GetComponent<HudCard>().color.a, .001f);
                                var bounds = HudRevisionBounds(root, warning);
                                Assert.GreaterOrEqual(bounds.xMin, root.rect.xMin);
                                Assert.LessOrEqual(bounds.xMax, root.rect.xMax);
                            });
                    }
                }
                view.Tick(local, true, false, false, false);
                Assert.AreEqual("", view.WarningText);
            }
            finally { SettingsStore.Current.HudScale = oldScale; }
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator RevisedWarningsFollowRefusedInputsAndHideForSpectators()
        {
            yield return Open(GameMode.Classic);yield return new WaitForSeconds(1.4f);
            var round=GameServices.Round;round.enabled=false;
            var local=round.PlayerAt(GameLaunch.SoloSeat);local.GetComponent<PlayerInputReader>().enabled=false;
            local.GetComponent<Carrier>().enabled=false;local.enabled=false;local.Intent.Clear();
            var view=Object.FindFirstObjectByType<TumpMatchReadout>();
            local.Teleport(new Vector3(0,.1f,2));local.Intent.Set(Verb.SpecialAbility,true);
            view.Tick(local,false,false,false,false);Assert.That(view.WarningText,Does.Contain("OUTSIDE DANGER ZONE"));
            local.Intent.Clear();yield return new WaitForSecondsRealtime(1.3f);
            view.Tick(local,false,false,false,false);Assert.AreEqual("",view.WarningText);
            local.Teleport(new Vector3(0,.1f,-8));round.Lata.HostKnockDown(2);Assert.IsFalse(round.Lata.IsUpright);
            local.Intent.Set(Verb.SpecialAbility,true);view.Tick(local,false,false,false,false);
            Assert.AreEqual("CANNOT THROW - CAN MUST BE UPRIGHT FIRST",view.WarningText);
            var defender=round.PlayerAt(GameServices.Match.DefenderSlot);defender.Intent.Parked=false;defender.Intent.Set(Verb.SpecialAbility,true);
            view.Tick(defender,false,false,false,false);Assert.AreEqual("CANNOT TAG - CAN MUST BE UPRIGHT FIRST",view.WarningText);
            local.Intent.Clear();local.Intent.Set(Verb.Sprint,true);local.Stamina.ApplyNetworkSnapshot(0,0,2.5f);
            view.Tick(local,false,false,false,false);Assert.That(view.WarningText,Does.StartWith("CANNOT RUN"));
            view.Tick(local,true,false,false,false);Assert.AreEqual("",view.WarningText);
        }

        [UnityTest,Timeout(90000)]
        public IEnumerator TimedRecoveryShowsStateWithoutMashPromptsOrPressCounts()
        {
            int quality=QualitySettings.GetQualityLevel(),mip=QualitySettings.globalTextureMipmapLimit;
            try
            {
                QualitySettings.SetQualityLevel(0,true);QualitySettings.globalTextureMipmapLimit=2;
                yield return Open(GameMode.HeroStrike);
                var local=GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                local.GetComponent<PlayerInputReader>().enabled=false;local.enabled=false;local.Intent.Clear();
                var view=Object.FindFirstObjectByType<TumpMatchReadout>();Assert.IsNotNull(view);
                local.ApplyStagger(2.5f,StunElement.Ice,9);view.Tick(local,false,false,false,false);
                Assert.IsTrue(local.IsFrozen);Assert.IsFalse(local.CanAct());
                Assert.IsTrue(view.Canvas.GetComponentsInChildren<Text>().Any(t=>t.isActiveAndEnabled&&t.name=="StatusName"&&t.text==StatusIcons.Name(StatusKind.Frozen)));
                var actionRoot=(RectTransform)typeof(TumpMatchReadout).GetField("_promptRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
                Assert.IsFalse(actionRoot.gameObject.activeSelf,"Timed Frozen is represented by its status indicator only.");
                NoMashText(view);
                yield return TumpUiCapture.Capture("Timed-recovery-Frozen",view.Canvas,960,540,false,true);
                local.ClearStun();local.ApplyTrip(2.5f);view.Tick(local,false,false,false,false);
                Assert.IsTrue(view.Canvas.GetComponentsInChildren<Text>().Any(t=>t.isActiveAndEnabled&&t.text=="Getting up"));
                NoMashText(view);
                local.ApplyNetworkState(0,0,StunElement.None,6,9,1.25f,2.5f,9,1.7f,100,0,0);
                view.Tick(local,false,false,false,false);
                var progress=(Image)typeof(TumpMatchReadout).GetField("_progress",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
                Assert.AreEqual(.5f,progress.rectTransform.anchorMax.x,.001f,"The recovery bar measures time, never old press counts.");
                Assert.AreEqual(0,local.MashPresses);NoMashText(view);
                yield return TumpUiCapture.Capture("Timed-recovery-get-up",view.Canvas,960,540,false,true);
            }
            finally {QualitySettings.SetQualityLevel(quality,true);QualitySettings.globalTextureMipmapLimit=mip;}
        }

        private static void NoMashText(TumpMatchReadout view)
        {
            foreach(var text in view.Canvas.GetComponentsInChildren<Text>())
            {
                if(!text.isActiveAndEnabled)continue;
                string value=text.text.ToLowerInvariant();
                Assert.IsFalse(value.Contains("mash")||value.Contains("presses")||value.Contains("shatter the ice"),text.name+": "+text.text);
            }
        }

        private static IEnumerator Open(GameMode mode)
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSecondsRealtime(.4f);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSecondsRealtime(3.7f);
            Assert.IsNotNull(Hud.Instance);
            Assert.AreEqual(8,GameServices.Match.TotalRounds,"Normal matches must start with the owner's eight-round default.");
            TumpUiCapture.StageHudReview(GameServices.Round.PlayerAt(GameLaunch.SoloSeat));
            yield return null;
        }
    }
}
