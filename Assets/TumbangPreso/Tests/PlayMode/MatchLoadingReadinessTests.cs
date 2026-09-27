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

        [UnityTest, Timeout(180000)]
        public IEnumerator CustomMapSwitchesReusePreparedScenesAndLooksAfterTheLoadingCurtain()
        {
            bool networked = SceneFlow.Networked, hubEnabled = ConvertedMatchSetup.HubEnabled;
            string selected = SceneFlow.SelectedMap;
            int loadsAfterReady = 0;
            void CountLoad(Scene scene, LoadSceneMode mode) { loadsAfterReady++; }
            try
            {
                SceneFlow.Networked = false; ConvertedMatchSetup.HubEnabled = true;
                TumpHub.PendingEntry = HubEntry.Home;
                yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
                var controller = Object.FindFirstObjectByType<ConvertedMatchSetup>();
                Assert.IsNotNull(controller);
                float until = Time.realtimeSinceStartup + 150;
                bool sawCurtain = false;
                while (controller.Preview == null || !controller.Preview.IsPrepared || HubLoading.Visible)
                {
                    sawCurtain |= HubLoading.Visible;
                    var loading = Object.FindFirstObjectByType<HubLoading>();
                    if (loading != null) Assert.IsNull(loading.FailureReason, loading.FailureReason);
                    Assert.Less(Time.realtimeSinceStartup, until, "Custom previews did not finish behind loading.");
                    yield return null;
                }
                Assert.IsTrue(sawCurtain);
                Assert.IsFalse(MatchInstaller.PreviewOnly, "Preparation retained the match-install suppression gate.");
                var preview = controller.Preview;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var cache = (System.Collections.Generic.Dictionary<string, Scene>)typeof(MapPreviewSurface).GetField("_cache", flags).GetValue(preview);
                var looks = (System.Collections.Generic.Dictionary<string, Visual.WorldLookPresentation>)typeof(MapPreviewSurface).GetField("_looks", flags).GetValue(preview);
                Assert.AreEqual(SceneFlow.Maps.Length, cache.Count);
                Assert.AreEqual(SceneFlow.Maps.Length, looks.Count);
                var preparedScenes = cache.ToDictionary(x => x.Key, x => x.Value);
                var preparedLooks = looks.ToDictionary(x => x.Key, x => x.Value);
                TumpHub.Current.Push<HubHost>();
                yield return null;
                SceneManager.sceneLoaded += CountLoad;
                var timer = new System.Diagnostics.Stopwatch();
                for (int pass = 0; pass < 2; pass++)
                    foreach (string map in SceneFlow.Maps)
                    {
                        timer.Restart(); controller.SelectMap(map); timer.Stop();
                        Assert.AreEqual(map, preview.Showing, "A prepared custom selection still waits for scene setup.");
                        Assert.AreEqual(preparedScenes[map], cache[map]);
                        Assert.AreSame(preparedLooks[map], looks[map]);
                        Assert.AreSame(looks[map], Visual.WorldLookPresentation.Current);
                        Debug.Log(System.FormattableString.Invariant($"[CustomMapSwitch] pass={pass} map={map} selectMs={timer.Elapsed.TotalMilliseconds:F3}"));
                        yield return null;
                        if (pass == 0)
                        {
                            preview.Camera.Render();
                            var target = RenderTexture.GetTemporary(320, 180, 0, RenderTextureFormat.ARGB32);
                            var image = new Texture2D(320, 180, TextureFormat.RGB24, false);
                            var previous = RenderTexture.active;
                            try
                            {
                                Graphics.Blit(preview.Camera.targetTexture, target); RenderTexture.active = target;
                                image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply();
                                Assert.Greater(image.GetPixels32().Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count(), 64,
                                    "Prepared preview is blank or flat: " + map);
                                string directory = System.IO.Path.Combine(Application.dataPath, "../Logs/custom-preview-loading-20260927");
                                System.IO.Directory.CreateDirectory(directory);
                                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, map + ".png"), image.EncodeToPNG());
                            }
                            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.Destroy(image); }
                        }
                    }
                Assert.AreEqual(0, loadsAfterReady, "Changing a prepared map started another scene load.");
                Assert.AreEqual(SceneFlow.Maps.Length, looks.Count);
            }
            finally
            {
                SceneManager.sceneLoaded -= CountLoad;
                HubLoading.Cancel(); SceneFlow.Networked = networked;
                ConvertedMatchSetup.HubEnabled = hubEnabled; SceneFlow.SelectedMap = selected;
            }
        }
    }
}
