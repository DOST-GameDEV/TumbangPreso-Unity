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
                var planes = GeometryUtility.CalculateFrustumPlanes(camera); int total = 0, enabled = 0, visible = 0, badShaders = 0;
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (renderer.gameObject.scene.name != map) continue;
                    total++;
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    enabled++;
                    if ((camera.cullingMask & (1 << renderer.gameObject.layer)) != 0 && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) visible++;
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader") badShaders++;
                }
                var directory = "Logs/feedback-0930/remade-previews"; Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, map + ".txt"), $"showing={preview.Showing}\ntotal={total}\nenabled={enabled}\nvisible={visible}\nbadShaders={badShaders}\ncameraEnabled={camera.enabled}\nlookScoped={TumbangPreso.Visual.WorldLookPresentation.HandlesCamera(camera)}\n");
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
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}
