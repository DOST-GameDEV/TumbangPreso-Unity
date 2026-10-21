using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE CAST UNDER THE WORLD'S AMBIENT OCCLUSION, RENDERED (owner, 2026-10-04: "can we also test
    /// ambient occlusion for shading the characters too?", then, after pressing the F8 test switch:
    /// "need you to actually render the character with the ao, because to me it doesnt change
    /// anythign for me"). This enters Play on Ilalim, waits for the match's bodies, freezes the
    /// clock, and renders the GAME'S OWN CAMERA (its whole image-effect chain: the world look,
    /// `WorldOutline`'s occlusion and composite, the grade) at one character from a few distances
    /// with `WorldOutline.CharacterAoTest` at 0 (the cast left out, as shipped), 0.5 and 1. It
    /// writes each frame, and the full-minus-off difference times eight, to
    /// Logs/ilalim-unity/character_ao/, with a report of how many pixels changed and by how much.
    /// It asserts nothing about the look: it exists to be looked at.
    /// </summary>
    [Category("WallClock")]
    public sealed class CharacterAoProbe
    {
        private const string Scene = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string Folder = "Logs/ilalim-unity/character_ao";

        [UnityTest, Timeout(240000)]
        public IEnumerator RenderACharacterWithAndWithoutAmbientOcclusion()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1f;
            float waited = 0f;
            while (waited < 40f && (Camera.main == null || Object.FindObjectsByType<CharacterMotor>().Length < 2)) { waited += Time.unscaledDeltaTime; yield return null; }
            // Let the round start and the bodies settle on the ground.
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) yield return null;

            var cam = Camera.main;
            Assert.IsNotNull(cam, "No main camera in Play on " + Scene);
            var outline = cam.GetComponent<Visual.WorldOutline>();
            var motors = Object.FindObjectsByType<CharacterMotor>();
            Assert.Greater(motors.Length, 0, "No characters in Play on " + Scene);
            // A body the rig is not hiding from its own first-person view: the first one that is not the camera's own.
            CharacterMotor subject = null;
            float nearest = float.MaxValue;
            foreach (var m in motors)
            {
                float d = Vector3.Distance(m.transform.position, cam.transform.position);
                if (d < 1.5f) continue;      // the local player, whose body the first-person rig hides
                if (d < nearest) { nearest = d; subject = m; }
            }
            if (subject == null) subject = motors[0];

            Time.timeScale = 0f;
            yield return null;
            Directory.CreateDirectory(Folder);
            var report = new StringBuilder();
            report.AppendLine("CHARACTER AMBIENT OCCLUSION PROBE on " + Scene);
            report.AppendLine($"camera {cam.name}, WorldOutline {(outline != null ? "present, enabled " + outline.enabled : "ABSENT")}, " +
                              $"look {(Visual.WorldLookPresentation.Current != null ? Visual.WorldLookPresentation.Current.Look.Map + " weight " + Visual.WorldLookPresentation.Current.Weight : "NONE")}, " +
                              $"profile AO {Visual.WorldLookProfile.Current.AmbientOcclusion} radius {Visual.WorldLookProfile.Current.AmbientOcclusionRadius}");
            report.AppendLine($"subject {subject.name} at {subject.transform.position}, {motors.Length} characters");

            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;
            float savedTest = Visual.WorldOutline.CharacterAoTest;
            // The rig writes the camera every LateUpdate; with the clock frozen and Render called by hand here, the pose below holds.
            Vector3 centre = subject.transform.position + Vector3.up * 0.9f;
            Vector3 front = subject.transform.forward; front.y = 0f; front = front.sqrMagnitude > 1e-4f ? front.normalized : Vector3.forward;
            var shots = new (string name, float metres, float fov, float side)[]
            {
                ("front_3m", 3.0f, 40f, 0.35f), ("front_5m", 5.0f, 26f, 0.35f), ("front_9m", 9.0f, 16f, 0.35f), ("side_5m", 5.0f, 26f, 1.3f),
            };
            const int w = 1280, h = 720;
            foreach (var s in shots)
            {
                Vector3 dir = Quaternion.AngleAxis(s.side * Mathf.Rad2Deg, Vector3.up) * front;
                cam.transform.position = centre + dir * s.metres + Vector3.up * 0.35f;
                cam.transform.rotation = Quaternion.LookRotation(centre - cam.transform.position);
                cam.fieldOfView = s.fov;
                Color32[] off = null, full = null;
                foreach (float level in new[] { 0f, 0.5f, 1f })
                {
                    Visual.WorldOutline.CharacterAoTest = level;
                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    rt.Create();
                    cam.targetTexture = rt;
                    cam.Render();
                    var previous = RenderTexture.active; RenderTexture.active = rt;
                    var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
                    RenderTexture.active = previous; cam.targetTexture = null;
                    File.WriteAllBytes($"{Folder}/{s.name}_ao{Mathf.RoundToInt(level * 100):000}.png", image.EncodeToPNG());
                    if (level == 0f) off = image.GetPixels32(); else if (level == 1f) full = image.GetPixels32();
                    Object.Destroy(image); rt.Release(); Object.Destroy(rt);
                }
                // Where the cast's occlusion landed: |full - off| x 8.
                int changed = 0, biggest = 0; long sum = 0;
                var diff = new Color32[off.Length];
                for (int i = 0; i < off.Length; i++)
                {
                    int d = Mathf.Max(Mathf.Abs(off[i].r - full[i].r), Mathf.Max(Mathf.Abs(off[i].g - full[i].g), Mathf.Abs(off[i].b - full[i].b)));
                    if (d > 1) { changed++; sum += d; }
                    if (d > biggest) biggest = d;
                    byte v = (byte)Mathf.Min(255, d * 8);
                    diff[i] = new Color32(v, v, v, 255);
                }
                var diffImage = new Texture2D(w, h, TextureFormat.RGB24, false);
                diffImage.SetPixels32(diff); diffImage.Apply();
                File.WriteAllBytes($"{Folder}/{s.name}_diff_x8.png", diffImage.EncodeToPNG());
                Object.Destroy(diffImage);
                report.AppendLine($"{s.name,-10} {s.metres} m, fov {s.fov}: {changed} of {off.Length} pixels changed ({100f * changed / off.Length:F2}%), " +
                                  $"largest {biggest}/255, mean of the changed {(changed > 0 ? (float)sum / changed : 0f):F1}/255");
            }
            Visual.WorldOutline.CharacterAoTest = savedTest;
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
            Time.timeScale = 1f;
            File.WriteAllText(Folder + "/report.txt", report.ToString());
            Debug.Log("[CharacterAoProbe]\n" + report);
#else
            yield break;
#endif
        }
    }
}
