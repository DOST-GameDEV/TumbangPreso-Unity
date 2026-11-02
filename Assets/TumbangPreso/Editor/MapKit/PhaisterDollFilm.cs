using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// HER DOLL AND ITS SKY CIRCLE ON THE COURT, in direction A (`PhaisterLook`), from the game's own classes: `SkyCircle.Pose` with
    /// `VoodooSkyCircle`'s sums, and `PhaisterDollLook.Pose` with `VoodooDollPresence`'s ages. On Eskinita opened as a PREVIEW scene,
    /// in the map's daylight, no bloom, real models for scale, 60 pictures a second, four views in a grid.
    ///
    /// STAGED, AND SAID SO. No match runs. The control and strings are `VoodooSkyCircle` itself, built and stepped by hand because `Awake`
    /// and `LateUpdate` do not run outside Play. The doll walks on `GaitStyles.PhaisterDoll`'s numbers, applied by plain rotations
    /// because `CharacterAnimator` needs a match. Nobody has cast this in Play.
    ///
    /// Request: lines in `Temp/phaister-doll-film.request`: `ping`, or `&lt;tag&gt;`. Frames in
    /// `Logs/paete-ability-film/phaisterdollcourt_&lt;tag&gt;_frames/`, then `done_phaisterdollcourt_&lt;tag&gt;.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class PhaisterDollFilm
    {
        private const string Request = "Temp/phaister-doll-film.request", OutDir = "Logs/paete-ability-film";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellW = 720, CellH = 405, Rate = 60;
        private static double _next;

        static PhaisterDollFilm() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string word = line.Trim();
                if (word.Length == 0) continue;
                if (word == "ping") { File.WriteAllText(Path.Combine(OutDir, "done_phaisterdollcourt_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                if (word.StartsWith("intro "))
                {
                    // Her cutscene in this look: the film tool's own run, with the look switched on round it.
                    var keep = PhaisterLook.Direction;
                    PhaisterLook.Direction = PhaisterLook.Style.A;
                    try { typeof(PaeteAbilityFilm).GetMethod("RunIntroFor", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "phaister", word.Substring(6).Trim() }); }
                    finally { PhaisterLook.Direction = keep; }
                    continue;
                }
                Run(new string(word.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray()));
            }
        }

        private static GameObject Body(Scene scene, RosterEntryAsset art, Vector3 at, float yaw, float height, bool glow)
        {
            var go = (GameObject)Object.Instantiate(art.Model);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw + CharacterVisual.PersonModelYaw, 0f));
            ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, art.Palette);
            if (glow) PhaisterDollArt.ApplyGlow(go);
            foreach (var a in go.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            // Its measured height against what the game stands it at: a preview has no `CharacterVisual` to do this.
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.localScale *= height / Mathf.Max(0.1f, b.size.y);
            return go;
        }

        /// <summary>What the install of a new doll model has to prove and a picture cannot: its clips still resolve, each surface wears the
        /// texture meant for it with the palette remap falling through, and the bones the strings tie to were found.</summary>
        private static string InstallReport(RosterEntryAsset art, GameObject body)
        {
            var text = new System.Text.StringBuilder("model " + (art.Model != null ? art.Model.name : "MISSING") + ", clips " + art.Clips.Count(c => c != null) + " of " + art.Clips.Length + " resolved, walk " + art.Clips.Any(c => c != null && c.name == "walk") + "\n");
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial; var tex = m != null && m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var skin = r as SkinnedMeshRenderer;
                text.Append(r.name + ": shader " + (m != null ? m.shader.name : "none") + ", tex " + (tex != null ? tex.name + " " + tex.width : "none") + ", palette " + (m != null && m.HasProperty("_UsePalette") ? m.GetFloat("_UsePalette") : -1f) + ", tris " + (skin != null ? skin.sharedMesh.triangles.Length / 3 : 0) + ", bones " + (skin != null ? skin.bones.Length : 0) + "\n");
            }
            var first = body.GetComponentInChildren<SkinnedMeshRenderer>();
            foreach (string side in new[] { "left", "right" })
                text.Append(side + " hand: " + (CharacterVisual.HandBone(first, side, out int b, out var palm) ? first.bones[b].name + " palm " + palm.ToString("0.000") : "NOT FOUND") + "\n");
            return text.ToString();
        }

        private static void Run(string tag)
        {
            bool hang = tag.EndsWith("hang");
            string name = "phaisterdollcourt_" + tag, done = Path.Combine(OutDir, "done_" + name + ".txt"), failed = Path.Combine(OutDir, "failed_" + name + ".txt");
            if (File.Exists(done)) File.Delete(done);
            if (File.Exists(failed)) File.Delete(failed);
            var keep = PhaisterLook.Direction;
            var active = SceneManager.GetActiveScene();
            bool wasDirty = active.isLoaded && active.isDirty, lit = false;
            Scene scene = default;
            RenderTexture rt = null; Texture2D cell = null, sheet = null; GameObject camGo = null;
            VoodooSkyCircle strings = null; PhaisterDollLook look = null;
            float walked = 0f;
            try
            {
                PhaisterLook.Direction = PhaisterLook.Style.A;
                scene = EditorSceneManager.OpenPreviewScene(Map);
                var physics = scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                Vector3 Ground(float x, float z)
                {
                    var at = new Vector3(x, 0f, z);
                    if (physics.IsValid() && physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
                    return at;
                }
                Vector3 feet = Ground(1.3f, 0f), her = Ground(-1.2f, -0.8f), far = Ground(1.0f, -10f);
                var dollArt = PhaisterDollArt.LoadArt();
                var herArt = RosterBook.Load()?.FindPersonArt("phaister");
                var dollBody = Body(scene, dollArt, feet, 180f, 1.75f, true);
                string install = InstallReport(dollArt, dollBody);
                if (herArt != null) Body(scene, herArt, her, 180f, 1.6f, false);

                camGo = new GameObject("~PhaisterDollCamera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false; cam.nearClipPlane = 0.03f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);
                rt = new RenderTexture(CellW, CellH, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cell = new Texture2D(CellW, CellH, TextureFormat.RGB24, false);
                sheet = new Texture2D(CellW * 2, CellH * 2, TextureFormat.RGB24, false);
                cam.targetTexture = rt;

                // Everything hangs off one holder in the preview scene, at the origin, so world and local are the same.
                var holder = new GameObject("~PhaisterDollHolder");
                SceneManager.MoveGameObjectToScene(holder, scene);
                var dollRoot = new GameObject("~PhaisterDollBody");
                dollRoot.transform.SetParent(holder.transform, false);
                dollRoot.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, 180f, 0f));
                dollBody.transform.SetParent(dollRoot.transform, true);
                strings = VoodooSkyCircle.BuildForFilm(dollRoot, dollBody, holder.transform);
                var gait = GaitStyles.PhaisterDoll;
                Transform Bone(string n) => dollBody.GetComponentsInChildren<Transform>(true).FirstOrDefault(b => b.name == n);
                Transform legL = Bone("leg-left"), legR = Bone("leg-right"), armL = Bone("arm-left"), armR = Bone("arm-right"), torso = Bone("torso"), head = Bone("head");
                var rest = new[] { legL, legR, armL, armR, torso, head }.Select(b => b.localRotation).ToArray();
                float modelY = dollBody.transform.localPosition.y;
                look = new PhaisterDollLook(holder.transform);

                // Views: a player beyond the can, the taya's height across the court, her own over-the-shoulder, from under it.
                Vector3[] eye = { new Vector3(far.x, feet.y + 1.7f, far.z), her + new Vector3(-0.8f, 1.9f, -3.6f), feet + new Vector3(4.5f, 1.7f, -8.5f), feet + new Vector3(0.6f, 1.4f, -3f) };
                Vector3[] aim = { feet + Vector3.up * 3.8f, feet + Vector3.up * 1.6f, feet + Vector3.up * 3.6f, feet + Vector3.up * 7f };
                float[] fov = { 52f, 66f, 56f, 78f };

                // The clock: wake and open at 0; shut from 1.6 (it hands back open at age 1.4, shuts at 3.0); paid at 3; tagged at 4.4.
                const float Length = 12.6f, EndAt = 11.2f, PayAt = 3f, TagAt = 4.4f;
                string frames = Path.Combine(OutDir, name + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                int count = Mathf.CeilToInt(Length * Rate);
                PhaisterDollExit exit = null;
                var clock = new System.Diagnostics.Stopwatch();
                double busy = 0, idle = 0; int busyN = 0, idleN = 0;
                for (int f = 0; f < count; f++)
                {
                    float t = f / (float)Rate;
                    bool tagged = t >= TagAt && t < TagAt + StatusRules.TaggedSeconds;
                    // THE WALK: the doll's own gait numbers (`GaitStyles.PhaisterDoll.Evaluate`, what a match feeds its bones) at its
                    // half walking speed round a loop; it stands where it is tagged. Applied to its bones by plain rotations here:
                    // `CharacterAnimator` needs a running match.
                    float walking = tagged || t < 0.5f ? 0f : 1f;
                    if (walking > 0f) { walked += 1f / Rate; }
                    float loop = walked * 1.25f / 2.2f;
                    Vector3 at = feet + new Vector3(Mathf.Sin(loop) * 2.2f, 0f, (1f - Mathf.Cos(loop)) * -1.3f);
                    at.y = Ground(at.x, at.z).y;
                    Vector3 going = new Vector3(Mathf.Cos(loop) * 2.2f, 0f, Mathf.Sin(loop) * -1.3f);
                    var body = dollRoot.transform;
                    body.SetPositionAndRotation(at, Quaternion.LookRotation(going.normalized, Vector3.up));
                    var pose = walking > 0f ? gait.Evaluate(walked * 0.62f % 1f, 0f, t, 0.62f) : default;
                    void Swing(Transform bone, Quaternion r, float pitch, float roll)
                    {
                        bone.localRotation = r;
                        bone.rotation = Quaternion.AngleAxis(-pitch, body.right) * Quaternion.AngleAxis(roll, body.forward) * bone.rotation;
                    }
                    float droop = tagged ? 1f : 0f;
                    Swing(legL, rest[0], pose.LegLeft, 0f); Swing(legR, rest[1], pose.LegRight, 0f);
                    Swing(armL, rest[2], pose.ArmLeft, droop * 58f); Swing(armR, rest[3], pose.ArmRight, droop * -58f);
                    // `<tag>hang`: the arms as `CharacterAnimator.PoseLimb` poses them in a match, hanging from the shoulder at the gait's spread,
                    // because the plain film holds them out on the bind pose and cannot show them against the belly. Standing, the clip's idle holds them about 40 degrees out.
                    if (hang)
                    {
                        float rested = walking > 0f ? 0f : 40f;
                        void Hang(Transform limb, float side, float forward, float outward)
                        {
                            float o = outward * Mathf.Deg2Rad;
                            var want = body.TransformDirection(Quaternion.AngleAxis(-forward, Vector3.right) * new Vector3(side * Mathf.Sin(o), -Mathf.Cos(o), 0f));
                            limb.rotation = Quaternion.FromToRotation(limb.TransformDirection(Vector3.right * Mathf.Sign(limb.GetChild(0).localPosition.x)), want) * limb.rotation;
                        }
                        float sideL = Mathf.Sign(body.InverseTransformPoint(armL.position).x), sideR = Mathf.Sign(body.InverseTransformPoint(armR.position).x);
                        Hang(armL, sideL, sideL < 0 ? pose.ArmLeft : pose.ArmRight, (sideL < 0 ? pose.SpreadLeft : pose.SpreadRight) + rested);
                        Hang(armR, sideR, sideR < 0 ? pose.ArmLeft : pose.ArmRight, (sideR < 0 ? pose.SpreadLeft : pose.SpreadRight) + rested);
                    }
                    Swing(torso, rest[4], -pose.TorsoPitch - droop * 16f, pose.TorsoRoll);
                    Swing(head, rest[5], -pose.HeadPitch - droop * 22f, pose.HeadRoll);
                    var modelAt = dollBody.transform.localPosition; modelAt.y = modelY + pose.RootUp; dollBody.transform.localPosition = modelAt;
                    strings.FilmTagged = tagged;
                    if (t < EndAt) strings.Step(1f / Rate, t, eye[0]);
                    if (t >= EndAt)
                    {
                        if (exit == null) { exit = PhaisterDollLook.PlayExit(at); exit.transform.SetParent(holder.transform, true); exit.enabled = false; dollBody.SetActive(false); look.Destroy(); look = null; holder.GetComponentsInChildren<LineRenderer>().ToList().ForEach(l => l.enabled = false); foreach (var r in holder.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Bar") || r.name == "Peg" || r.name == "Cap" || r.name.StartsWith("WirePoint")) r.enabled = false; }
                        exit.Step(PhaisterDollExit.StartAge + t - EndAt);
                    }
                    for (int v = 0; v < 4; v++)
                    {
                        strings.enabled = false;
                        look?.Pose(t, at, at + Vector3.up * 2.2f, t >= TagAt ? t - TagAt : -1f, StatusRules.TaggedSeconds, t >= PayAt ? t - PayAt : -1f, her, eye[v]);
                        cam.fieldOfView = fov[v];
                        Vector3 seenFrom = v == 3 ? at + new Vector3(1.6f, 1.9f, -4.6f) : eye[v], seen = v == 3 ? at + Vector3.up * 1.7f : aim[v];
                        cam.fieldOfView = v == 3 ? 58f : fov[v];
                        cam.transform.SetPositionAndRotation(seenFrom, Quaternion.LookRotation(seen - seenFrom, Vector3.up));
                        clock.Restart();
                        cam.Render();
                        clock.Stop();
                        if (v == 3) { if (t > 0.6f && t < 1.4f) { busy += clock.Elapsed.TotalMilliseconds; busyN++; } else if (t > 2.6f && t < 2.9f) { idle += clock.Elapsed.TotalMilliseconds; idleN++; } }
                        var was = RenderTexture.active; RenderTexture.active = rt;
                        cell.ReadPixels(new Rect(0, 0, CellW, CellH), 0, 0); cell.Apply();
                        RenderTexture.active = was;
                        sheet.SetPixels(v % 2 * CellW, (1 - v / 2) * CellH, CellW, CellH, cell.GetPixels());
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                File.WriteAllText(done, count + " frames at " + Rate + " a second. views: far player | taya | side | close game camera following it. the real control and strings (VoodooSkyCircle.Step) on the doll, walking on its gait numbers applied by plain rotations; no match runs.\n"
                    + "seal texture: " + FxStudyKit.Mask("phaister_seal").format + " " + FxStudyKit.Mask("phaister_seal").width + ", " + (UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(FxStudyKit.Mask("phaister_seal")) / 1048576f).ToString("0.0") + " MB\n"
                    + "camera submit time, under view, CPU only: circle and wake " + (busy / Mathf.Max(1, busyN)).ToString("0.00") + " ms, retinue only " + (idle / Mathf.Max(1, idleN)).ToString("0.00") + " ms\n" + install);
            }
            catch (Exception e) { File.WriteAllText(failed, e.ToString()); Debug.LogError("[PhaisterDollFilm] " + e); }
            finally
            {
                PhaisterLook.Direction = keep;
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                if (cell != null) Object.DestroyImmediate(cell);
                if (sheet != null) Object.DestroyImmediate(sheet);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                if (active.isLoaded && active.isDirty && !wasDirty)
                    typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, new object[] { active });
            }
        }
    }
}
