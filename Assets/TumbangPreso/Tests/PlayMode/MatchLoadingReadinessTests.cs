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
    }
}
