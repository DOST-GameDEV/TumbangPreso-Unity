using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PauseOwnerRebindTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator ReopeningAfterLocalSeatRebindParksAndReleasesTheCurrentBody()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior; var editor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            var owner = new GameObject("Pause owner");
            var firstGo = new GameObject("Former local body"); var currentGo = new GameObject("Current local body");
            var first = firstGo.AddComponent<CharacterMotor>(); first.enabled = false;
            var current = currentGo.AddComponent<CharacterMotor>(); current.enabled = false;
            PausePanel panel = null;
            try
            {
                SceneFlow.Networked = true;
                var watcher = owner.AddComponent<PauseWatcher>(); watcher.enabled = false; watcher.Local = first;
                panel = Panel.Open<PausePanel>(watcher); panel.Local = first; yield return null;
                Assert.IsTrue(first.Intent.Parked); panel.Close(); Assert.IsFalse(first.Intent.Parked);
                yield return null;
                watcher.Local = current;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update();
                watcher.SendMessage("Update");
                Assert.AreSame(current, panel.Local);
                Assert.IsTrue(current.Intent.Parked, "Reopening the existing menu did not park its newly bound local body.");
                Assert.IsFalse(first.Intent.Parked, "The menu parked its old body after the local seat changed.");
                Assert.AreEqual(1, Time.timeScale, "A network menu must leave match time running.");
                panel.Close(); Assert.IsFalse(current.Intent.Parked); Assert.IsFalse(first.Intent.Parked);
            }
            finally
            {
                if (panel != null) panel.Close();
                Object.DestroyImmediate(owner); Object.DestroyImmediate(firstGo); Object.DestroyImmediate(currentGo);
                InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background; settings.editorInputBehaviorInPlayMode = editor;
                SceneFlow.Networked = false; Hitstop.End(); PresentationClock.RequestScale(1);
            }
        }
    }
}
