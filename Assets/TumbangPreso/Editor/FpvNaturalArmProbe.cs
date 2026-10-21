using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// The first-person arms as the GAME's own camera frames them, for the heroes on the natural arms.
    ///
    /// `FppArmsSnapshotTool` photographs the arms through a 58 degree lens on a mount of its own, which is a review of
    /// the arm's dress and says nothing about where the hands sit on the player's screen. This mounts them the way
    /// `CameraRig` does (`ViewmodelSeat`, the 95 degree lens, the framing at full weight: 8 cm lower, 0.64 scale) so a
    /// placement can be judged, and logs what each arm is wearing. Writes `Logs/shots-fpv-natural/`.
    ///
    ///     python tools/run_unity_guarded.py -batchmode -force-d3d11 -executeMethod TumbangPreso.EditorTools.FpvNaturalArmProbe.Run -logFile Logs/fpv-natural.log
    /// </summary>
    public static class FpvNaturalArmProbe
    {
        private const string OutDir = "Logs/shots-fpv-natural";
        private static readonly string[] Heroes = { "sean", "zack", "dante", "cheska", "nemu", "phaister", "rafi", "amihan", "paete", "bayan" };

        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("~FpvProbeCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = CameraRig.FppFieldOfView;
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.40f, 0.46f, 0.56f);

            var sun = new GameObject("~FpvProbeSun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.05f; sun.color = new Color(1f, 0.98f, 0.95f);
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Shader.SetGlobalFloat("_CharacterSmoothShade", Visual.ToonSkin.CharacterSmoothShade);

            var mount = new GameObject("~ViewmodelArms");
            mount.transform.SetParent(camGo.transform, false);
            // `CameraRig` seats it, and the framing at full weight lowers it 8 cm and brings it to 0.64.
            mount.transform.localPosition = CameraRig.ViewmodelSeat + Vector3.down * 0.08f;
            mount.transform.localScale = Vector3.one * 0.64f;
            var arms = mount.AddComponent<ViewmodelArms>();
            arms.EnsureBuilt();

            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var book = RosterBook.Load();

            foreach (string id in Heroes)
            {
                arms.SetCharacter(id);
                var entry = book != null ? book.FindPersonArt(id) : null;
                if (entry != null && entry.Model != null)
                    foreach (var skin in entry.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        foreach (var m in skin.sharedMaterials)
                            Debug.Log("[FpvProbe] " + id + " body renderer " + skin.name + " material " + (m == null ? "null" : m.name + " shader " + m.shader.name
                                + " textures " + string.Join(",", m.GetTexturePropertyNames().Select(n => n + "=" + (m.GetTexture(n) == null ? "null" : m.GetTexture(n).name)))));
                foreach (var r in mount.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    var mesh = r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                    var mat = r.sharedMaterial;
                    string uvs = "";
                    if (mesh != null && mesh.uv != null && mesh.uv.Length > 0)
                        uvs = " uv " + mesh.uv.Aggregate(Vector2.positiveInfinity, Vector2.Min) + " to " + mesh.uv.Aggregate(Vector2.negativeInfinity, Vector2.Max);
                    Debug.Log("[FpvProbe] " + id + " drawn " + r.transform.parent.name + "/" + r.name + " mesh " + (mesh == null ? "null" : mesh.name + " " + mesh.bounds.size.ToString("F3"))
                        + uvs + " material " + (mat == null ? "null" : mat.name + " tex " + (mat.mainTexture == null ? "null" : mat.mainTexture.name)
                        + " usePalette " + (mat.HasProperty("_UsePalette") ? mat.GetFloat("_UsePalette") : -1f)
                        + " shader " + mat.shader.name + " queue " + mat.renderQueue + " color " + (mat.HasProperty("_Color") ? mat.GetColor("_Color").ToString() : "none")
                        + " st " + (mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") + "/" + mat.GetTextureOffset("_MainTex") : "none")
                        + " keywords " + string.Join("|", mat.shaderKeywords)
                        + " colors " + (mesh != null && mesh.colors32 != null && mesh.colors32.Length > 0 ? mesh.colors32[0].ToString() : "none")
                        + " normals " + (mesh != null && mesh.normals != null ? mesh.normals.Length : 0)) + " natural " + arms.NaturalArms);
                }
                // The body itself, dressed as the game dresses it, in the same frame as the arms: if it is painted and the
                // arms are not, the fault is in how the arms are dressed and not in the texture or the lights.
                GameObject body = null;
                if (entry != null && entry.Model != null && System.Environment.GetEnvironmentVariable("TUMP_FPV_DIAGNOSE") == id)
                {
                    body = Object.Instantiate(entry.Model);
                    body.transform.position = camGo.transform.position + camGo.transform.forward * 1.6f + Vector3.down * 0.55f;
                    body.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    Visual.ToonSkin.Apply(body, Visual.ToonSkin.PersonOutlineWidth, entry.Palette);
                    var tex = arms.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.mainTexture != null)?.mainTexture;
                    if (tex != null)
                    {
                        var small = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGB32);
                        Graphics.Blit(tex, small);
                        var read = new Texture2D(8, 8, TextureFormat.RGB24, false);
                        var keep = RenderTexture.active; RenderTexture.active = small;
                        read.ReadPixels(new Rect(0, 0, 8, 8), 0, 0); read.Apply(); RenderTexture.active = keep;
                        Debug.Log("[FpvProbe] atlas on the GPU, 8x8: top row " + read.GetPixel(4, 6) + " bottom row " + read.GetPixel(4, 1) + " size " + tex.width + "x" + tex.height);
                        RenderTexture.ReleaseTemporary(small); Object.DestroyImmediate(read);
                    }
                }
                foreach (bool holding in new[] { false, true })
                {
                    arms.SetHolding(holding);
                    arms.StepVisuals(0.016f, snap: true);
                    cam.Render();
                    Save(rt, Path.Combine(OutDir, "fpv_" + id + (holding ? "_holding" : "_empty") + ".png"));
                }
                if (body != null)
                {
                    // Which difference whitens the arm: its own toon material, the palette switch, or its mesh.
                    var armRenderers = mount.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name == "Arm" && r.enabled).ToArray();
                    var bodyMaterial = body.GetComponentsInChildren<SkinnedMeshRenderer>(true).First().sharedMaterial;
                    var own = armRenderers.Select(r => r.sharedMaterial).ToArray();
                    foreach (var r in armRenderers) r.sharedMaterial = bodyMaterial;
                    cam.Render(); Save(rt, Path.Combine(OutDir, "diag_" + id + "_body_material.png"));
                    for (int i = 0; i < armRenderers.Length; i++)
                    {
                        var copy = new Material(own[i]); copy.SetFloat("_UsePalette", 0f);
                        armRenderers[i].sharedMaterial = copy;
                    }
                    cam.Render(); Save(rt, Path.Combine(OutDir, "diag_" + id + "_no_palette.png"));
                    for (int i = 0; i < armRenderers.Length; i++) armRenderers[i].sharedMaterial = own[i];
                    Debug.Log("[FpvProbe] body material " + bodyMaterial.name + " usePalette " + bodyMaterial.GetFloat("_UsePalette")
                        + " palette0 " + bodyMaterial.GetVectorArray("_Palette")?.FirstOrDefault() + " arm palette0 " + own[0].GetVectorArray("_Palette")?.FirstOrDefault());
                    // What the arm's own UVs read off the atlas file, and what an unlit textured arm looks like.
                    var file = new Texture2D(2, 2);
                    file.LoadImage(File.ReadAllBytes("Assets/TumbangPreso/Art/CharacterRedesign/" + id + "/" + id + "-redesign-atlas.png"));
                    var armMesh = armRenderers[0].GetComponent<MeshFilter>().sharedMesh;
                    var uv0 = armMesh.uv; var list = new System.Collections.Generic.List<Vector4>(); armMesh.GetUVs(0, list);
                    string samples = "";
                    for (int i = 0; i < uv0.Length; i += Mathf.Max(1, uv0.Length / 8))
                        samples += " uv" + uv0[i].ToString("F3") + "=" + (Color32)file.GetPixelBilinear(uv0[i].x, uv0[i].y);
                    Debug.Log("[FpvProbe] arm uv samples off the file:" + samples + " | channels: uv2 " + armMesh.uv2.Length + " uv3 " + armMesh.uv3.Length
                        + " tangents " + armMesh.tangents.Length + " submeshes " + armMesh.subMeshCount + " verts " + armMesh.vertexCount);
                    var bodyMesh = body.GetComponentsInChildren<SkinnedMeshRenderer>(true).First().sharedMesh;
                    Debug.Log("[FpvProbe] body mesh uv " + bodyMesh.uv.Length + " uv2 " + bodyMesh.uv2.Length + " colors " + bodyMesh.colors32.Length + " verts " + bodyMesh.vertexCount);
                    foreach (var any in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (any.transform.IsChildOf(body.transform)) continue;
                        string path = any.name; for (var t = any.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                        var f = any.GetComponent<MeshFilter>();
                        Debug.Log("[FpvProbe] scene renderer " + path + " type " + any.GetType().Name + " enabled " + any.enabled + " active " + any.gameObject.activeInHierarchy
                            + " materials " + string.Join(",", any.sharedMaterials.Select(m => m == null ? "null" : m.name + ":" + m.shader.name))
                            + " mesh " + (f != null && f.sharedMesh != null ? f.sharedMesh.name + " sub " + f.sharedMesh.subMeshCount : "none"));
                    }
                    var unlit = new Material(Shader.Find("Unlit/Texture")) { mainTexture = own[0].mainTexture };
                    foreach (var r in armRenderers) r.sharedMaterial = unlit;
                    cam.Render(); Save(rt, Path.Combine(OutDir, "diag_" + id + "_unlit.png"));
                    for (int i = 0; i < armRenderers.Length; i++) armRenderers[i].sharedMaterial = own[i];
                    Object.DestroyImmediate(body);
                }
            }
            cam.targetTexture = null;
            Debug.Log("[FpvProbe] done");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// One hero's hands through a whole jump, as a strip: standing, the take-off dip, rising, the top, falling, the
        /// landing slam, the rebound, settled. The spring is the game's own (`ViewmodelArms.StepAirSpring`), driven by a
        /// jump's arc instead of a body. `-hero <id>` picks the hero (Dante without it).
        ///
        ///     python tools/run_unity_guarded.py -batchmode -force-d3d11 -executeMethod TumbangPreso.EditorTools.FpvNaturalArmProbe.RunJump -hero dante -logFile Logs/fpv-jump.log
        /// </summary>
        public static void RunJump()
        {
            Directory.CreateDirectory(OutDir);
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-hero");
            string id = at >= 0 && at + 1 < args.Length ? args[at + 1] : "dante";
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("~FpvProbeCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = CameraRig.FppFieldOfView; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.40f, 0.46f, 0.56f);
            var sun = new GameObject("~FpvProbeSun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.05f; sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Shader.SetGlobalFloat("_CharacterSmoothShade", Visual.ToonSkin.CharacterSmoothShade);
            var mount = new GameObject("~ViewmodelArms");
            mount.transform.SetParent(camGo.transform, false);
            mount.transform.localPosition = CameraRig.ViewmodelSeat + Vector3.down * 0.08f;
            mount.transform.localScale = Vector3.one * 0.64f;
            var arms = mount.AddComponent<ViewmodelArms>();
            arms.EnsureBuilt(); arms.SetCharacter(id); arms.SetHolding(false);

            const int w = 640, h = 360;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            // A standing jump: 7 m/s up under 20 m/s2, about 0.7 s in the air, then half a second on the ground.
            const float dt = 1f / 60f, up = 7f, gravity = 20f;
            float[] shots = { 0.00f, 0.05f, 0.18f, 0.35f, 0.55f, 0.69f, 0.74f, 0.80f, 0.90f, 1.20f };
            var strip = new Texture2D(w * 5, h * 2, TextureFormat.RGB24, false);
            float time = -0.2f, height = 0f, speed = 0f; bool grounded = true; int shot = 0;
            while (shot < shots.Length)
            {
                if (grounded && time >= 0f && time < dt) { grounded = false; speed = up; }
                if (!grounded) { speed -= gravity * dt; height += speed * dt; if (height <= 0f && speed < 0f) { grounded = true; height = 0f; } }
                arms.StepVisuals(dt, snap: true);
                arms.StepAirSpring(grounded, grounded ? 0f : speed, true, dt);
                arms.PoseAirMotion();
                if (time >= shots[shot])
                {
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = rt;
                    strip.ReadPixels(new Rect(0, 0, w, h), (shot % 5) * w, (1 - shot / 5) * h);
                    RenderTexture.active = keep;
                    Debug.Log("[FpvProbe] jump " + id + " t " + shots[shot].ToString("F2") + " grounded " + grounded + " speed " + speed.ToString("F1") + " lift " + arms.AirLift.ToString("F3"));
                    shot++;
                }
                time += dt;
            }
            strip.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, "jump_" + id + ".png"), strip.EncodeToPNG());
            cam.targetTexture = null;
            EditorApplication.Exit(0);
        }

        private static void Save(RenderTexture rt, string path)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
