using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Photographs § THE RECALL MARK in every state it has, over the real street, at both the
    /// reference shape and the short wide window 🧑 actually plays in.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `CLAUDE.md` § 6.2b IS A LIST OF FOUR WAYS A SCREEN SHIPS BROKEN
    /// AFTER BEING "RENDERED", AND THE MARK COULD HAVE HIT ALL FOUR. It has four states and the
    /// first one built was the easy one; it is drawn over a live 3D scene whose background is
    /// *"sky one frame, asphalt the next"*; it has to survive a window shorter than 16:9; and it
    /// shares a screen with chrome that knows nothing about it. A shot of the loose state, at
    /// 1920 x 1080, over an empty scene, with the HUD off, would have been a picture of a
    /// different feature.
    ///
    /// ⚠️⚠️ AND IT IS A PICTURE RATHER THAN AN ASSERTION ON PURPOSE, WHICH IS THE HALF THIS
    /// PROBE CANNOT DO. `PlayerHubLayoutProbe` was green through every one of § 6.2c's seven
    /// readability faults, because a label that fits its box fits its box whether the picture is
    /// beautiful or butchered. This writes the frames; a person still has to look at them.
    /// `docs/TODO.md` § 154.5 carries that as an open item and so does `Attention.md`.
    ///
    /// ⚠️ IT ASSERTS THE ONE THING IT HONESTLY CAN: that the mark was ON SCREEN and in the state
    /// the filename claims, so a run that quietly photographed four frames of nothing fails
    /// instead of producing four files somebody signs off.
    /// </summary>
    public class SlipperRecallShots
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private const string OutDir = "Logs/shots-recall";

        /// <summary>
        /// ⚠️ HIS WINDOW, MEASURED RATHER THAN GUESSED. `CLAUDE.md` § 6.2b records that
        /// `Fullscreen` is false in his `settings.json` and that he plays in a short wide window,
        /// and `InputSurfaceProbe.ProbeResolutions` already carries the shape. 1600 x 720 is
        /// 2.22:1, wider than 16:9 and 360 units shorter, which is the half that breaks layouts.
        /// </summary>
        private const int ShortWide = 1600;
        private const int ShortHigh = 720;

        [UnityTest]
        public IEnumerator LiveRecallFollowsRebindingAndDeviceChangesWithoutRebuilding()
        {
            var asset=Resources.Load<InputActionAsset>("TumbangPreso");
            string overrides=asset.SaveBindingOverridesAsJson();
            var oldDevice=LastInputDevice.Current;
            bool oldTouch=TouchInput.Active;var oldMove=TouchInput.Move;
            bool bots=GameLaunch.AllBots,spectator=GameLaunch.Spectator,pinned=UI.SceneFlow.RulesPinned;
            int seat=GameLaunch.SoloSeat;var rules=UI.SceneFlow.SelectedRules.Clone();
            var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            var inputSettings=InputSystem.settings;var background=inputSettings.backgroundBehavior;var editorInput=inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(pad);
            try
            {
                yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
                GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);
                who.Teleport(new Vector3(0,.12f,-10));who.transform.rotation=Quaternion.identity;
                Object.FindFirstObjectByType<CameraSystem.CameraRig>().Follow(who);
                var mine=who.GetComponent<Carrier>().Held;Assert.IsNotNull(mine);
                Assert.IsTrue(mine.HostDisarm());
                mine.transform.position=new Vector3(0,mine.RestHeight,-4);
                var recall=Object.FindFirstObjectByType<UI.SlipperRecall>();Assert.IsNotNull(recall);
                TouchInput.Active=false;TouchInput.Move=Vector2.zero;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F10));InputSystem.Update();keyboard.MakeCurrent();LastInputDevice.Sample();
                yield return null;
                recall.Track(who,mine);Assert.IsTrue(recall.Drawing);
                var cap=GameObject.Find("RecallKeyCap").GetComponent<Image>();
                var label=GameObject.Find("RecallKeyLabel").GetComponent<Text>();
                var marker=(RectTransform)cap.transform.parent;
                var before=marker.anchoredPosition;

                Assert.IsNull(Rebinding.TryRebind(asset,"Grab",keyboard.f10Key));
                recall.Track(who,mine);
                Assert.AreEqual("F10",UI.Hud.KeyLabelFor("Grab"));
                var f10=UI.InputGlyphs.For("F10",onDark:true);
                Assert.IsTrue(f10!=null?cap.enabled&&cap.sprite==f10:label.enabled&&label.text=="F10",
                    "The live mark retained its previous binding after a successful rebind.");
                var keyboardSprite=cap.sprite;
                mine.transform.position+=Vector3.right*1.25f;recall.Track(who,mine);
                Assert.Greater(Vector2.Distance(before,marker.anchoredPosition),1,"Rebinding stopped world tracking.");

                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
                InputSystem.Update();LastInputDevice.Sample();recall.Track(who,mine);
                Assert.AreEqual(InputDeviceKind.Gamepad,LastInputDevice.Current);
                Assert.IsTrue(cap.enabled&&cap.sprite!=null&&!label.enabled,"Gamepad pickup glyph was not visible.");
                Assert.AreNotSame(keyboardSprite,cap.sprite,"Device change left the keyboard cap cached.");

                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                TouchInput.Active=true;TouchInput.Move=Vector2.right;LastInputDevice.Sample();recall.Track(who,mine);
                Assert.AreEqual(InputDeviceKind.Touch,LastInputDevice.Current);
                Assert.IsTrue(recall.Drawing);Assert.IsFalse(cap.enabled||label.enabled,"Touch retained a keyboard/pad instruction.");

                TouchInput.Active=false;TouchInput.Move=Vector2.zero;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F10));InputSystem.Update();
                Assert.IsTrue(keyboard.f10Key.isPressed,"The synthetic return key did not reach the Input System.");
                keyboard.MakeCurrent();LastInputDevice.Sample();
                recall.Track(who,mine);
                Assert.AreEqual(InputDeviceKind.KeyboardMouse,LastInputDevice.Current);
                Assert.IsTrue(f10!=null?cap.enabled&&cap.sprite==f10:label.enabled&&label.text=="F10");
                Assert.AreSame(marker,cap.transform.parent,"The test must exercise the existing cached mark.");
                Assert.IsTrue(recall.Drawing);
            }
            finally
            {
                asset.LoadBindingOverridesFromJson(overrides);Rebinding.Invalidate();Rebinding.Save(asset);
                TouchInput.Active=oldTouch;TouchInput.Move=oldMove;
                InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(keyboard);
                inputSettings.backgroundBehavior=background;inputSettings.editorInputBehaviorInPlayMode=editorInput;
                typeof(LastInputDevice).GetMethod("Set",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null,new object[]{oldDevice});
                GameLaunch.AllBots=bots;GameLaunch.Spectator=spectator;GameLaunch.SoloSeat=seat;
                UI.SceneFlow.AdoptRemoteRules(rules);if(pinned)UI.SceneFlow.PinSelectedRules(rules);else UI.SceneFlow.UnpinSelectedRules();
            }
        }

        [UnityTest]
        public IEnumerator CapturedRebindConflictsPreservePriorOverridesAndDeviceFamilies()
        {
            var shared=Resources.Load<InputActionAsset>("TumbangPreso");string saved=shared.SaveBindingOverridesAsJson();
            var asset=Object.Instantiate(shared);asset.RemoveAllBindingOverrides();
            var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editorInput=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(pad);
            RebindSession session=null;
            try
            {
                Assert.IsTrue(Rebinding.ResolveBindingIndexFor(asset,"Grab",InputDeviceKind.KeyboardMouse,out var grab,out int index));
                grab.ApplyBindingOverride(index,"<Keyboard>/f10");
                RebindOutcome? outcome=null;
                session=RebindSession.Begin(asset,"Grab",InputDeviceKind.KeyboardMouse,(result,_)=>outcome=result);
                Assert.IsNotNull(session);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();
                // Unity's interactive operation deliberately waits50ms after a
                // candidate. Empty batch frames are not a wall-clock deadline.
                for(float end=Time.realtimeSinceStartup+1;!outcome.HasValue&&Time.realtimeSinceStartup<end;)yield return null;
                Assert.AreEqual(RebindOutcome.Conflict,outcome);
                Assert.AreEqual("<Keyboard>/f10",grab.bindings[index].effectivePath,"A refused replacement erased the player's previous key.");
                Assert.IsTrue(grab.enabled);
                session.Dispose();session=null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();yield return null;

                outcome=null;
                session=RebindSession.Begin(asset,"Grab",InputDeviceKind.KeyboardMouse,(result,_)=>outcome=result);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F9));InputSystem.Update();
                for(float end=Time.realtimeSinceStartup+1;!outcome.HasValue&&Time.realtimeSinceStartup<end;)yield return null;
                Assert.AreEqual(RebindOutcome.Bound,outcome);
                Assert.AreEqual("<Keyboard>/f9",grab.bindings[index].effectivePath);
                Assert.AreEqual("<Gamepad>/buttonWest",Rebinding.PathFor(asset,"Grab",InputDeviceKind.Gamepad));
                session.Dispose();session=null;

                Assert.IsNull(Rebinding.TryRebind(asset,"SpecialAbility",keyboard.f8Key),"A keyboard key must be accepted for a mouse action on the same controls page.");
                Assert.AreEqual("<Keyboard>/f8",Rebinding.PathFor(asset,"SpecialAbility",InputDeviceKind.KeyboardMouse));
                Assert.AreEqual("<Gamepad>/rightTrigger",Rebinding.PathFor(asset,"SpecialAbility",InputDeviceKind.Gamepad));
                Assert.IsNull(Rebinding.TryRebind(asset,"Grab",pad.buttonWest));
                Assert.AreEqual("<Gamepad>/buttonWest",Rebinding.PathFor(asset,"Grab",InputDeviceKind.Gamepad));
                Assert.AreEqual("<Keyboard>/f9",grab.bindings[index].effectivePath);

                string fullscreen=Rebinding.PathFor(asset,"ToggleFullscreen",InputDeviceKind.KeyboardMouse);
                Assert.IsNotNull(Rebinding.TryRebind(asset,"ToggleFullscreen",pad.rightStickButton));
                Assert.AreEqual(fullscreen,Rebinding.PathFor(asset,"ToggleFullscreen",InputDeviceKind.KeyboardMouse));
                outcome=null;session=RebindSession.Begin(asset,"Grab",InputDeviceKind.KeyboardMouse,(result,_)=>outcome=result);
                session.Cancel();Assert.AreEqual(RebindOutcome.Cancelled,outcome);
                Assert.AreEqual("<Keyboard>/f9",grab.bindings[index].effectivePath);
            }
            finally
            {
                session?.Dispose();InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(keyboard);Object.Destroy(asset);
                settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editorInput;
                shared.LoadBindingOverridesFromJson(saved);Rebinding.Invalidate();Rebinding.Save(shared);
            }
        }

        [UnityTest]
        public IEnumerator TheRecallMarkIsPhotographedInEveryState()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 30; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round, "the arena registered no round");

            // ⚠️⚠️ THE HIGHLIGHT COLOUR IS SET EXPLICITLY, AND THE FIRST RUN OF THE BEAM FRAMES
            // IS WHY. `SettingsStore.Current` persists to disk, so these shots were taken in
            // whatever colour this machine happened to have chosen last: the witness frame came
            // back RED while the shipped default is Blue. `LandedHighlightTests` opens the same
            // way and records the same reason, that a stale `settings.json` must not be able to
            // decide what a test measures or what a render shows.
            //
            // ⚠️ RAISED AS WELL AS WRITTEN. Setting the field alone leaves every listener on
            // whatever it last cached, which for the beam means a column already standing in the
            // old colour.
            Settings.SettingsStore.Current.SlipperHighlight = Settings.SlipperHighlights.Default;
            Settings.SettingsStore.RaiseSlipperHighlightChanged();

            // ⚠️⚠️ THE ROUND IS STARTED, AND NOT DOING SO COST THIS PROBE TWO RUNS. A freshly
            // loaded arena sits in the READY window: the HUD reads *"Warm up freely. Powers start
            // with the round"*, `RoundActive` is false, and `CharacterMotor.CanAct()` is
            // `RoundActive && !IsStunned`. So `IsGrabbableIgnoringReach` refuses every tsinelas
            // in the arena and the mark correctly draws its ring with no cap, from 0.88 m, which
            // reads exactly like a broken pickup radius. **The shot was of the warm-up, not of a
            // round.** `SoloPracticeTests` opens the same way and for the same reason.
            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "no SliceRunner in the arena");
            if (!runner.Running) runner.Begin();

            for (int i = 0; i < 120 && !round.RoundActive; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(round.RoundActive,
                "the round never went active, so every verb in the arena is refused and the " +
                "frames below would be pictures of the warm-up");

            // The seat this screen belongs to. `MatchInstaller.HumanSeat` gives exactly one body
            // a `PlayerInputReader`, and the mark, the owner glow and the HUD all key off it.
            CharacterMotor me = null;
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (m.GetComponent<PlayerInputReader>() == null) continue;
                me = m;
                break;
            }

            Assert.IsNotNull(me, "no seat carries a PlayerInputReader, so there is no local player");
            Assert.IsFalse(me.IsDefender,
                "the local seat is the taya this round, and a taya owns no tsinelas to recall");

            Slipper mine = null;
            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                if (s.OwnerSlot == me.PlayerSlot) { mine = s; break; }

            Assert.IsNotNull(mine, $"no tsinelas answers to seat {me.PlayerSlot}");

            // ⚠️ THE SEAT IS SILENCED FIRST. This probe points the body by hand to aim the FPP
            // camera (`CameraRig.ApplyFpp` takes its yaw from the body's forward), and a reader
            // or a planner writing that same transform every frame would turn the shot into a
            // photograph of whatever the seat felt like doing.
            var reader = me.GetComponent<PlayerInputReader>();
            var bot = me.GetComponent<AIController>();
            if (reader != null) reader.enabled = false;
            if (bot != null) bot.enabled = false;

            // ⚠️⚠️ THE OTHER SEATS' ABILITIES ARE SWITCHED OFF, AND THE FIRST RUN OF THIS PROBE IS
            // WHY. In Hero Strike the three bots open fire immediately, and the first frame it
            // wrote came back with `STUNNED · 1.1s` and `WITCHFIRE` down the left of the screen
            // and the whole edge washed green. That is two separate faults in one picture: the
            // shot is of a stunned player rather than of a retrieval, and `CanAct()` is false
            // while stunned, so `IsGrabbableIgnoringReach` refuses and the mark draws its ring
            // with no cap in the very frames the cap is the subject of.
            //
            // ⚠️ IT IS THE PLANNERS' ABILITIES RATHER THAN THE PLANNERS, on purpose. Turning the
            // bots off entirely would photograph an empty street, and `CLAUDE.md` § 6.2b's whole
            // argument is that a screen has to be shot over the background it really has.
            // `AIController.AbilitiesEnabled` exists for exactly this and `InputEdgeTests` cites
            // it by name (`docs/TODO.md` § 42).
            foreach (var other in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))
                other.AbilitiesEnabled = false;

            var hud = Object.FindFirstObjectByType<UI.Hud>();
            Assert.IsNotNull(hud, "no HUD in the arena, so there is no mark to photograph");

            // ⚠️⚠️ THE READY PROMPT IS CLOSED, AND LEAVING IT OPEN MADE ONE FRAME LIE ABOUT THE
            // THING IT EXISTS TO SHOW. `TumpMatchReadout.Prompts` tests `ReadyWindow` BEFORE it
            // tests whether a tsinelas is in reach, so with the window open the hand-off frame
            // photographed *"[R] Ready · Warm up freely"* where the `[X] Pick up` that takes the
            // mark's place should be. The round is already live by this line; this closes the
            // banner the gate leaves behind, which is what a player sees a few seconds in.
            hud.ShowReadyPrompt(false);

            // ---- 1 · IN FLIGHT ------------------------------------------------------------
            // The ring alone, with no cap: there is nothing to press yet.
            Vector3 from = me.transform.position;

            // ⚠️ A REAL ARC RATHER THAN A DROP. `HostThrow` takes an ORIGIN and a VELOCITY, and
            // handing it a zero velocity photographs a tsinelas falling straight down, which is
            // not the state a player is ever confused by. The numbers are a comfortable mid
            // throw: the landing point is read back off the object afterwards rather than
            // predicted, so nothing here has to agree with `Balance.Gravity`.
            Vector3 muzzle = from + me.transform.forward * 0.6f + Vector3.up * 1.3f;
            mine.HostThrow(me, muzzle, me.transform.forward * 11.0f + Vector3.up * 3.5f);
            Assert.AreEqual(SlipperState.InFlight, mine.State, "the throw did not take");

            Face(me, mine.transform.position);
            for (int i = 0; i < 6; i++) yield return null;

            yield return Shot(mine, me, "recall-1-inflight", SlipperState.InFlight);

            // ---- 2 · LOOSE, AT RANGE ------------------------------------------------------
            // Ring plus the live GRAB cap. This is the state the whole feature is for.
            for (int i = 0; i < 400 && mine.State != SlipperState.Loose; i++)
                yield return new WaitForFixedUpdate();

            Assert.AreEqual(SlipperState.Loose, mine.State,
                $"the tsinelas never landed. y={mine.transform.position.y:F2}");

            // ⚠️⚠️ THE TSINELAS MOVES AND THE BODY DOES NOT, WHICH IS `InputEdgeTests`' OWN
            // RECORDED LESSON AND COST THIS PROBE A RUN. Walking the seat to a distance looks
            // equivalent and is not: `CharacterMotor.Confine` clamps X and Z back into the
            // confinement square on every step, so a body placed by hand is dragged off the
            // mark before the shot and the frame is taken from wherever the clamp left it. The
            // first version of the hot frame failed on exactly that, from 0.96 m, which is well
            // inside `Balance.PickupRadius`. A loose slipper has no such constraint.
            //
            // ⚠️ AND IT IS PLACED TOWARD THE CAN rather than along an arbitrary bearing, so six
            // metres out is still inside the arena on both maps instead of through a wall.
            var lata = round.Lata;
            Vector3 inward = lata != null ? (lata.transform.position - me.transform.position)
                                          : me.transform.forward;
            inward.y = 0.0f;
            if (inward.sqrMagnitude < 0.01f) inward = me.transform.forward;
            inward.Normalize();

            yield return Settle(mine, me, inward, 6.0f, faceShoe: true);

            // § THE RECALL BEAM, which is the world half of the same question and is asserted in
            // the same frame the screen mark is, on purpose: *"there shouldnt be any conflict
            // with the slipperRecall"* is a claim about the two of them TOGETHER, and a picture
            // of each on its own cannot show it.
            var beam = mine.GetComponent<Visual.SlipperBeam>();
            Assert.IsNotNull(beam,
                "no beam was built for a loose tsinelas this seat owns, with the highlight on");
            Assert.IsTrue(beam.Drawing,
                $"the beam is not standing. state={mine.State} owner={mine.OwnerSlot} " +
                $"seat={me.PlayerSlot} setting={Settings.SettingsStore.Current.SlipperHighlight}");

            Color want = Settings.SlipperHighlights.ColourOf(
                Settings.SettingsStore.Current.SlipperHighlight);

            Assert.AreEqual(want.r, beam.Colour.r, 0.01f, "beam red");
            Assert.AreEqual(want.g, beam.Colour.g, 0.01f, "beam green");
            Assert.AreEqual(want.b, beam.Colour.b, 0.01f, "beam blue");

            // ⚠️⚠️ THE SHADER IS ASSERTED SEPARATELY FROM THE PICTURE BECAUSE THE PICTURE CANNOT
            // SHOW THE DIFFERENCE RELIABLY. 🧑 asked for this effect to be *"a shader not a
            // model"*, and `VfxMaterial.Beam`'s fallback is a flat coloured cylinder standing in
            // the same place at the same height: from the player's own eyes looking down at the
            // road the two frames are very nearly identical, and one of them is the version he
            // rejected. A lookup that fails in a player and nowhere else is exactly the split
            // `GameBuilder.EnsureRuntimeShaders` exists to prevent, so this is the editor-side
            // guard for it.
            Assert.IsTrue(beam.Shaded,
                "the beam fell back to a flat column, so TumbangPreso/SlipperBeam did not load");

            MeasureBeamFrameFraction(beam, "range", 1280, 720);

            yield return Shot(mine, me, "recall-2-loose-at-range", SlipperState.Loose);

            // ⚠️ A SIDE CAMERA, BECAUSE A COLUMN CANNOT BE JUDGED FROM ABOVE IT. The frames above
            // are the player's own eyes looking down at the road, which is the right view for the
            // ring and the wrong one for a 2.2 m vertical: § 127.3 records the identical finding
            // about a floor marker caught only edge-on. This is the one that shows its height
            // against a body.
            yield return Witness(mine, me, "beam-witness");

            // The same frame at his own window, which is the shape § 6.2b says nobody has seen.
            yield return Shot(mine, me, "recall-2-loose-at-range-shortwide",
                              SlipperState.Loose, ShortWide, ShortHigh);

            foreach (float fade in new[] { .15f, .5f, 1f })
            {
                yield return Settle(mine, me, inward, Balance.PickupRadius + fade, faceShoe: true);
                MeasureBeamFrameFraction(beam, "fade-" + Mathf.RoundToInt(fade * 100), 1280, 720);
                MeasureBeamFrameFraction(beam, "fade-wide-" + Mathf.RoundToInt(fade * 100), ShortWide, ShortHigh);
            }

            // ---- 3 · IN REACH, WHERE THE MARK HANDS OVER ----------------------------------
            // ⚠️⚠️ THIS FRAME ASSERTS AN ABSENCE, AND IT IS THE MOST VALUABLE ONE HERE. The first
            // build drew a thicker ring inside `Balance.PickupRadius` and the render of it went
            // straight through the middle card of the ability deck, because at arm's length a
            // tsinelas on the road is below the bottom of a first-person frame and the mark
            // clamped downward into the fullest part of the screen. `SlipperRecall.Track` hands
            // over to the readout's own `[X] Pick up` there instead. **A probe that only ever
            // asserts things are drawn cannot catch a marker that draws in the wrong place.**
            for (int i = 0; i < 300 && !me.CanAct(); i++) yield return null;

            yield return Settle(mine, me, inward, Balance.PickupRadius * 0.5f, faceShoe: true);

            Assert.IsTrue(mine.CanBeGrabbedBy(me),
                $"the hand-off frame was taken from outside the pickup radius, so it proves " +
                $"nothing about the hand-off. " +
                $"d={Vector3.Distance(me.transform.position, mine.transform.position):0.00} " +
                $"radius={Balance.PickupRadius} holding={me.HoldingSlipper} " +
                $"owner={mine.OwnerSlot} seat={me.PlayerSlot} canAct={me.CanAct()}");

            // ⚠️⚠️ BOTH STAND DOWN, AND ASSERTING ONLY ONE OF THEM WOULD MISS THE POINT OF THE
            // WHOLE HAND-OFF. The mark leaves the screen and the beam leaves the world, on the
            // same `Balance.PickupRadius`, so the player standing over their own tsinelas sees
            // `[X] Pick up` and nothing else competing with it.
            Assert.IsFalse(beam.Drawing,
                $"the beam is still standing inside the pickup radius. " +
                $"d={Vector3.Distance(me.transform.position, mine.transform.position):0.00} " +
                $"radius={Balance.PickupRadius} fade={Visual.SlipperBeam.FadeMetres}");

            yield return Shot(mine, me, "recall-3-in-reach-handover", SlipperState.Loose,
                              drawing: false);

            // ---- 4 · CLAMPED TO THE EDGE, WITH THE CHEVRON --------------------------------
            // Turn away so the tsinelas leaves the frame entirely. This is the state that
            // replaced `OffscreenIndicators`' slipper arrow and it is the one a player meets
            // most: you threw it, you turned, and now you have to find it again.
            yield return Settle(mine, me, inward, 6.0f, faceShoe: false);

            yield return Shot(mine, me, "recall-4-clamped", SlipperState.Loose);
            yield return Shot(mine, me, "recall-4-clamped-shortwide",
                              SlipperState.Loose, ShortWide, ShortHigh);

            if (reader != null) reader.enabled = true;
            if (bot != null) bot.enabled = true;
        }

        /// <summary>
        /// A camera beside the tsinelas rather than above it, so a vertical reads as a vertical.
        ///
        /// ⚠️ IT FRAMES THE SHOE AND THE PLAYER TOGETHER. A column photographed alone has no
        /// scale in it, and the one thing a person has to judge here is whether 2.2 m is right
        /// next to a body, which is the number `SlipperBeam.Height` picked against the two maps
        /// that are built under a roof.
        /// </summary>
        private static IEnumerator Witness(Slipper mine, CharacterMotor me, string name)
        {
            Vector3 shoe = mine.transform.position;
            Vector3 mid = (shoe + me.transform.position) * 0.5f;

            Vector3 along = me.transform.position - shoe;
            along.y = 0.0f;
            if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
            along.Normalize();

            // Off to the side of the line between the two, so neither hides the other.
            Vector3 side = Vector3.Cross(Vector3.up, along);

            var go = new GameObject("BeamWitnessCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 45.0f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200.0f;
            cam.transform.position = mid + side * 5.5f + Vector3.up * 1.7f;
            cam.transform.LookAt(shoe + Vector3.up * 1.0f);

            yield return GameplayShots.Render(cam, name, flipCanvases: false, outDir: OutDir);

            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Points the body, which is what points the first-person camera.
        /// `CameraRig.ApplyFpp` recovers its yaw from `_character.transform.forward` on purpose
        /// (*"yaw goes on the body, pitch stays on the rig"*), so this is the supported way to
        /// aim a shot rather than a workaround.
        /// </summary>
        /// <summary>
        /// Puts the tsinelas a given distance in front of the seat, at the seat's own footing.
        ///
        /// ⚠️ THE HEIGHT COMES FROM THE BODY RATHER THAN FROM A GROUND CAST. `CanBeGrabbedBy`
        /// measures a 3D distance from `who.transform.position`, so matching that height is what
        /// makes the reach in this probe the reach the game uses. `SoloPracticeTests` places a
        /// shoe at exactly `me.transform.position` for the same reason.
        /// </summary>
        private static void Park(Slipper shoe, CharacterMotor who, Vector3 direction, float metres)
        {
            shoe.transform.position = who.transform.position + direction * metres;
        }

        /// <summary>
        /// Parks the shoe, aims the body, lets the frame settle, then parks it AGAIN.
        ///
        /// ⚠️⚠️ THE SECOND PARK IS THE WHOLE REASON THIS IS A METHOD, AND THE FIRST TWO RUNS OF
        /// THIS PROBE ARE WHY IT EXISTS. The body is live: three bots are playing, a shove is not
        /// an ability and is not switched off with them, and `CharacterMotor` keeps applying
        /// gravity and `Confine` whether or not this seat's reader is disabled. Parking once and
        /// then waiting four frames measured 2.74 m from a shoe placed at 0.88 m, because the
        /// distance was set against a body that had since been pushed. Placing it last is what
        /// makes the shot's geometry the geometry that was asserted.
        /// </summary>
        private static IEnumerator Settle(Slipper shoe, CharacterMotor who, Vector3 direction,
                                          float metres, bool faceShoe)
        {
            Park(shoe, who, direction, metres);
            yield return null;
            yield return null;

            Park(shoe, who, direction, metres);

            // ⚠️ AWAY FROM THE SHOE IS THE CLAMPED FRAME'S WHOLE SUBJECT. Facing it would put it
            // back on screen and photograph the on-screen state under the clamped state's name.
            Face(who, faceShoe ? shoe.transform.position
                               : who.transform.position - direction * 10.0f);

            yield return null;
        }

        private static void Face(CharacterMotor who, Vector3 at)
        {
            Vector3 look = at - who.transform.position;
            look.y = 0.0f;
            if (look.sqrMagnitude < 0.0001f) return;
            who.transform.rotation = Quaternion.LookRotation(look.normalized);
        }

        private static void MeasureBeamFrameFraction(Visual.SlipperBeam beam, string label, int width, int height)
        {
            // Same-camera, same-frame A/B includes the column, pool, lamp and bloom.
            // Count changed pixels, not just white ones: a saturated blue wash can
            // obscure play without turning white. This is visual coverage, not GPU cost.
            Assert.IsTrue(beam.Drawing, label + ": there is no beam to measure");
            var camera = Camera.main;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var colour = beam.Colour;
            string folder = System.Environment.GetEnvironmentVariable("TUMP_BEAM_EVIDENCE");
            if (string.IsNullOrWhiteSpace(folder))
                folder = System.IO.Path.Combine(OutDir, "beam-fraction-" + System.DateTime.UtcNow.Ticks);
            System.IO.Directory.CreateDirectory(folder);
            try
            {
                camera.targetTexture = target;
                beam.Set(false, colour); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                var before = pixels.GetPixels32();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, label + "-off.png"), pixels.EncodeToPNG());
                beam.Set(true, colour); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                var after = pixels.GetPixels32();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, label + "-on.png"), pixels.EncodeToPNG());
                int changed = 0, newlyWhite = 0;
                for (int i = 0; i < before.Length; i++)
                {
                    int delta = Mathf.Abs(after[i].r - before[i].r) + Mathf.Abs(after[i].g - before[i].g) + Mathf.Abs(after[i].b - before[i].b);
                    if (delta > 9) changed++;
                    if (after[i].r > 245 && after[i].g > 245 && after[i].b > 245
                        && (before[i].r <= 245 || before[i].g <= 245 || before[i].b <= 245)) newlyWhite++;
                }
                float fraction = changed / (float)before.Length, white = newlyWhite / (float)before.Length;
                string row = System.FormattableString.Invariant($"{label},{width},{height},{changed},{fraction:F6},{newlyWhite},{white:F6}");
                System.IO.File.AppendAllText(System.IO.Path.Combine(folder, "coverage.csv"), row + "\n");
                Debug.Log("[RecallBeamCoverage] " + row);
                Assert.Greater(changed, 0, label + ": the beam never contributed visible pixels");
                Assert.Less(fraction, .12f, label + ": recall effect exceeds the 12-percent frame budget");
            }
            finally
            {
                beam.Set(true, colour); camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                target.Release(); Object.Destroy(target); Object.Destroy(pixels);
            }
        }

        /// <summary>
        /// One frame, through `GameplayShots.Render`, with the mark asserted to be drawing first.
        ///
        /// ⚠️⚠️ THE ASSERTION IS THE POINT OF THIS WRAPPER. A probe that writes four PNGs and
        /// checks nothing cannot fail, and § 124.11's finding is that a green probe for a screen
        /// nobody can reach is worse than a red one. `SlipperRecall.Drawing` is asked directly,
        /// so a run in which `Track` refused every frame
        /// goes red rather than producing pictures of an empty street for somebody to approve.
        /// </summary>
        private static IEnumerator Shot(Slipper mine, CharacterMotor me, string name,
                                        SlipperState expected, int width = 0, int height = 0,
                                        bool drawing = true)
        {
            Assert.AreEqual(expected, mine.State,
                $"{name} was taken with the tsinelas in {mine.State} rather than {expected}");

            var recall = Object.FindFirstObjectByType<UI.SlipperRecall>();
            Assert.IsNotNull(recall, $"{name}: the HUD built no SlipperRecall at all");
            Assert.AreEqual(drawing, recall.Drawing,
                $"{name}: the recall mark is {(recall.Drawing ? "drawing" : "not drawing")} and " +
                $"this frame wants it {(drawing ? "drawn" : "gone")}. state={mine.State} " +
                $"owner={mine.OwnerSlot} seat={me.PlayerSlot} defender={me.IsDefender} " +
                $"holding={me.HoldingSlipper} " +
                $"d={Vector3.Distance(me.transform.position, mine.transform.position):0.00}");

            var cam = Camera.main;
            Assert.IsNotNull(cam, $"{name}: no main camera");

            yield return GameplayShots.Render(cam, name, flipCanvases: true, outDir: OutDir,
                                              width: width, height: height);
        }
    }
}
