using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.Tests
{
    public class RebindActionStateTests
    {
        [TestCase(false, "cancel")]
        [TestCase(true, "cancel")]
        [TestCase(false, "dispose")]
        [TestCase(true, "dispose")]
        [TestCase(false, "bound")]
        [TestCase(true, "bound")]
        [TestCase(false, "conflict")]
        [TestCase(true, "conflict")]
        public void EndingARebindRestoresTheOriginalActionState(bool enabled, string ending)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            const string bindingsKey = "tumbangpreso.bindings";
            bool hadBindings = PlayerPrefs.HasKey(bindingsKey);
            string savedBindings = PlayerPrefs.GetString(bindingsKey, "");
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Player");
            var jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            map.AddAction("Grab", InputActionType.Button, "<Keyboard>/e");
            asset.AddActionMap(map);
            jump.ApplyBindingOverride(0, "<Keyboard>/q");
            if (enabled) jump.Enable();
            string before = asset.SaveBindingOverridesAsJson();
            RebindSession session = null;
            Keyboard keyboard = null;
            int reports = 0;
            RebindOutcome? observed = null;
            bool? callbackEnabled = null;
            string conflict = null;
            try
            {
                session = RebindSession.Begin(asset, "Jump", InputDeviceKind.KeyboardMouse, (outcome, reason) =>
                {
                    reports++;
                    observed = outcome;
                    conflict = reason;
                    callbackEnabled = jump.enabled;
                });
                Assert.IsNotNull(session);
                Assert.IsFalse(jump.enabled, "Listening must suppress the action in either original state.");
                var operation = (InputActionRebindingExtensions.RebindingOperation)typeof(RebindSession)
                    .GetField("_operation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                Assert.IsTrue(operation.started);
                if (ending == "cancel") operation.Cancel();
                else if (ending == "dispose") session.Dispose();
                else
                {
                    keyboard = InputSystem.AddDevice<Keyboard>();
                    operation.AddCandidate(ending == "bound" ? keyboard.f12Key : keyboard.eKey, 1f);
                    operation.Complete();
                }

                Assert.AreEqual(ending == "dispose" ? 0 : 1, reports);
                if (ending != "dispose")
                {
                    Assert.AreEqual(ending == "cancel" ? RebindOutcome.Cancelled
                        : ending == "bound" ? RebindOutcome.Bound : RebindOutcome.Conflict, observed);
                    Assert.AreEqual(enabled, callbackEnabled, "Restore state before reporting to the settings screen.");
                }
                if (ending == "bound") Assert.AreEqual("<Keyboard>/f12", jump.bindings[0].effectivePath);
                else Assert.AreEqual(before, asset.SaveBindingOverridesAsJson(), "An unaccepted control changed the previous override.");
                if (ending == "conflict") Assert.AreEqual(Rebinding.LabelFor("Grab"), conflict);
                Assert.AreEqual(enabled, jump.enabled, "Closing must not activate a previously disabled action.");
                session.Cancel(); session.Dispose();
                Assert.AreEqual(ending == "dispose" ? 0 : 1, reports, "Closing twice must not report twice.");
                Assert.AreEqual(enabled, jump.enabled);
            }
            finally
            {
                session?.Dispose();
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                asset.Disable(); UnityEngine.Object.DestroyImmediate(asset);
                if (hadBindings) PlayerPrefs.SetString(bindingsKey, savedBindings);
                else PlayerPrefs.DeleteKey(bindingsKey);
                PlayerPrefs.Save(); Rebinding.Invalidate();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ARefusedDeviceLeavesActionStateAndBindingAlone(bool enabled)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Player");
            var action = map.AddAction("ToggleFullscreen", InputActionType.Button, "<Keyboard>/f11");
            asset.AddActionMap(map);
            if (enabled) action.Enable();
            string before = asset.SaveBindingOverridesAsJson();
            try
            {
                Assert.IsNotNull(RebindSession.RefusalFor(asset, "ToggleFullscreen", InputDeviceKind.Gamepad));
                Assert.IsNull(RebindSession.Begin(asset, "ToggleFullscreen", InputDeviceKind.Gamepad,
                    (_, __) => Assert.Fail("A refused start must not report a completed operation.")));
                Assert.AreEqual(enabled, action.enabled);
                Assert.AreEqual(before, asset.SaveBindingOverridesAsJson());
            }
            finally
            {
                asset.Disable(); UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
