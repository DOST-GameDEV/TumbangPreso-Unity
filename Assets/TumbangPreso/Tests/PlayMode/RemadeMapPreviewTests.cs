using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class RemadeMapPreviewTests
    {
        int _idleDelay = -1;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
#if UNITY_EDITOR
            _idleDelay = UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = 1;
#endif
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
#if UNITY_EDITOR
            if (_idleDelay >= 0) UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = _idleDelay;
#endif
        }
        [UnityTest, Timeout(90000)] public IEnumerator KantoPreviewContainsVisibleMapGeometry() => Inspect(SceneFlow.Kanto);
        [UnityTest, Timeout(90000)] public IEnumerator LagoonCovePreviewContainsVisibleMapGeometry() => Inspect(SceneFlow.LagoonCove);
        [UnityTest, Timeout(90000)] public IEnumerator IlalimPreviewContainsVisibleMapGeometry() => Inspect(SceneFlow.IlalimNgTulay);
        [UnityTest, Timeout(90000)]
        public IEnumerator LateStreetVoicesRemainSilentInMapPreviewButPlayInGameScope()
        {
            var root = new GameObject("Preview sound check", typeof(RectTransform), typeof(RawImage));
            var preview = root.AddComponent<MapPreviewSurface>();
            bool oldPause = AudioListener.pause; AudioListener.pause = false;
            try
            {
                preview.Show(SceneFlow.IlalimNgTulay); float until = Time.realtimeSinceStartup + 45;
                while (preview.Showing != SceneFlow.IlalimNgTulay && Time.realtimeSinceStartup < until) yield return null;
                Assert.AreEqual(SceneFlow.IlalimNgTulay, preview.Showing);
                yield return null; yield return null;
                var life = Object.FindFirstObjectByType<SidewalkLife>(); Assert.IsNotNull(life);
                Assert.AreEqual(MapPreviewSurface.PreviewLayer, life.gameObject.layer);
                int Voices()
                {
                    int count = 0;
                    foreach (var source in life.GetComponentsInChildren<AudioSource>(true))
                        if (source.enabled && source.clip != null) count++;
                    return count;
                }
                // Drive its authored story through the real runtime step. This reaches sources
                // created after MapPreviewSurface.Silence, without a long idle wall-clock wait.
                for (int i = 0; i < 1000; i++) life.Simulate(.1f);
                int previewVoices = Voices();
                life.gameObject.layer = 0;
                for (int i = 0; i < 1000; i++) life.Simulate(.1f);
                int gameVoices = Voices();
                Directory.CreateDirectory("Logs/ilalim-preview-audio1002");
                File.WriteAllText("Logs/ilalim-preview-audio1002/voices.txt",
                    $"previewVoices={previewVoices}\ngameVoices={gameVoices}\n");
                Assert.Greater(gameVoices, 0, "The authored street never created a normal game-scope voice.");
                Assert.Zero(previewVoices, "Late-created street voices bypassed the map preview's audio silence.");
            }
            finally { AudioListener.pause = oldPause; Object.Destroy(root); }
            yield return null;
        }

        IEnumerator Inspect(string map)
        {
            var root = new GameObject("Remade preview check", typeof(RectTransform), typeof(RawImage));
            var image = root.GetComponent<RawImage>(); var preview = root.AddComponent<MapPreviewSurface>();
            try
            {
                preview.Show(map); float until = Time.realtimeSinceStartup + 45;
                while (preview.Showing != map && Time.realtimeSinceStartup < until) yield return null;
                Assert.AreEqual(map, preview.Showing, "The selected map did not finish the actual additive preview route.");
                yield return null; yield return null;
                var camera = preview.Camera; Assert.IsNotNull(camera); Assert.IsNotNull(camera.targetTexture);
                var planes = GeometryUtility.CalculateFrustumPlanes(camera); int total = 0, enabled = 0, visible = 0, badShaders = 0, escaped = 0;
                foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (renderer.gameObject.scene.name != map) continue;
                    total++;
                    if (renderer.gameObject.layer != MapPreviewSurface.PreviewLayer) escaped++;
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    enabled++;
                    if ((camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) visible++;
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader") badShaders++;
                }
                var directory = "Logs/feedback-0930/remade-previews"; Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, map + ".txt"), $"showing={preview.Showing}\ntotal={total}\nenabled={enabled}\nvisible={visible}\nbadShaders={badShaders}\nescaped={escaped}\ncameraEnabled={camera.enabled}\nlookScoped={TumbangPreso.Visual.WorldLookPresentation.HandlesCamera(camera)}\n");
                camera.Render(); var previous = RenderTexture.active; RenderTexture.active = camera.targetTexture;
                var pixels = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
                try
                {
                    pixels.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), 0, 0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(directory, map + ".png"), pixels.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; Object.Destroy(pixels); }
                Assert.Greater(enabled, 20, "The preview lost the remade map's renderer roots.");
                Assert.Greater(visible, 10, "Loaded geometry is outside the preview camera/layer scope.");
                Assert.AreEqual(0, badShaders, "The map contains missing, unsupported or error shaders.");
                Assert.AreEqual(0, escaped, "Runtime map geometry escaped the preview layer and can draw behind the menu.");
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}
