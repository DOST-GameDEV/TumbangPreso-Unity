using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The mountain beast's turnaround for the owner (`docs/reports/dante-presentation-2026-10-16/plan.md` section 5.5: a verdict
    /// BEFORE it goes into the scene). The real glb, dressed as the game dresses it (`DanteProp`: toon paint, ink, the cut-in
    /// light), Basilio's own model standing on its saddle for scale, five views. Writes PNGs to `TUMP_EVIDENCE` (default Logs).
    /// </summary>
    [Category("WallClock")]
    public sealed class DanteBeastReview
    {
        [UnityTest]
        public IEnumerator TurnaroundWithBasilioForScale()
        {
            yield return PlayModeWorld.Reset();
            string dir = System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Application.dataPath, "..", "Logs", "dante-beast");
            Directory.CreateDirectory(dir);
            string tag = System.Environment.GetEnvironmentVariable("TUMP_BEAST_TAG") ?? "v1";

            var stage = new GameObject("Beast review");
            var beast = DanteProp.Spawn("beast", stage.transform);
            Assert.IsNotNull(beast, "beast.glb must load from Resources/Models/DanteProps.");
            Assert.IsNotNull(DanteProp.Find(beast, "glow-eyes"), "The eyes are a named glow node.");
            foreach (var n in new[] { "head", "brow", "jaw", "horn-left", "horn-right", "arm-left", "arm-right", "fist-left", "fist-right", "back-0", "back-1", "back-2" })
                Assert.IsNotNull(DanteProp.Find(beast, n), "Named node " + n);

            var entry = Resources.Load<RosterBook>("RosterBook").People.FirstOrDefault(p => p.Id == "dante");
            GameObject him = null;
            if (entry != null && entry.Model != null)
            {
                him = Object.Instantiate(entry.Model, stage.transform, false);
                him.transform.localPosition = new Vector3(0f, 3.4f, -.25f);
                him.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(him, ToonSkin.PersonOutlineWidth, entry.Palette);
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(stage.transform, false); floor.transform.localScale = new Vector3(14f, .1f, 14f); floor.transform.localPosition = new Vector3(0, -.05f, 0);
            floor.GetComponent<Renderer>().material.color = new Color(.55f, .50f, .44f);
            foreach (var t in stage.GetComponentsInChildren<Transform>()) t.gameObject.layer = 30;

            var sunObject = new GameObject("Beast review key"); var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = .95f; sun.color = new Color(1, .96f, .90f); sun.cullingMask = 1 << 30;
            sun.transform.rotation = Quaternion.Euler(38, -30, 0);
            var cameraObject = new GameObject("Beast review camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false; camera.allowHDR = true; camera.cullingMask = 1 << 30; camera.fieldOfView = 34;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.30f, .32f, .36f);
            cameraObject.AddComponent<ColourGrade>();
            var oldAmbient = RenderSettings.ambientLight; var oldMode = RenderSettings.ambientMode; bool oldFog = RenderSettings.fog;
            var rt = new RenderTexture(1200, 900, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1200, 900, TextureFormat.RGB24, false);
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .55f, .60f); RenderSettings.fog = false;
                yield return null;
                var target = new Vector3(0, 1.9f, .4f);
                foreach (var (name, yaw, pitch, dist, focus) in new[]
                {
                    ("front", 0f, 8f, 15f, target), ("three-quarter", 38f, 14f, 15f, target), ("side", 90f, 6f, 15f, target),
                    ("back", 180f, 16f, 15f, target), ("face", 12f, 2f, 6.2f, new Vector3(0, 2.0f, 3.0f)),
                })
                {
                    var dir3 = Quaternion.Euler(-pitch, yaw, 0) * Vector3.forward;
                    camera.transform.position = focus + dir3 * dist;
                    camera.transform.LookAt(focus);
                    camera.targetTexture = rt; camera.Render(); camera.targetTexture = null;
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, $"beast-{tag}-{name}.png"), tex.EncodeToPNG());
                    yield return null;
                }
                Debug.Log("[DanteBeastReview] wrote " + dir);
            }
            finally
            {
                RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientMode = oldMode; RenderSettings.fog = oldFog;
                Object.DestroyImmediate(stage); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(sunObject);
                rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            }
            yield return PlayModeWorld.Reset();
        }
    }
}
