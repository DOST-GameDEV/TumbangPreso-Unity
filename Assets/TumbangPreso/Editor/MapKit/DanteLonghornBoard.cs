using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// A STORYBOARD, NOT THE GAME: three ways the longhorn could come into Dante's CONTINENTAL DRIFT as it is now (the ASTRA
    /// cutscene, merged 2026-10-18). The owner: "see how we can incorporate the longhorn". Each proposal is drawn in the real
    /// cutscene (`HeroIntroductionScene`, "dante", sampled at its own moments and shot from its own camera, as the intro film
    /// does) with the real longhorn (`Models/UltimateProps/longhorn`, its painted atlas, its `arm-r`, `head` and `jaw`
    /// joints) stood in it by hand. Nothing here changes the cutscene, the ultimate or any rule.
    ///   P1 THE SPIRIT IN THE HANG: the longhorn rises behind him as a spirit (the shader Makiling is drawn in) while his fists
    ///      tremble overhead, its own fist raised, and its fist comes down with his slam.
    ///   P2 THE SPIRIT INSIDE THE SUIT: as each wave of Mountain Form's armour closes on him the longhorn shows through him, his
    ///      size, and at the slam its silhouette breaks out forward along the first band.
    ///   P3 THE CHARGE: after the slam the longhorn itself (solid, painted) bursts out of the crater and charges down the lane
    ///      the bands run along.
    /// Request: a line `<tag>` in `Temp/dante-longhorn-board.request` (outside Play). Each moment is drawn twice, from the
    /// cutscene's own camera and from one wide camera, into `Logs/paete-ability-film/longhorn_<tag>/`.
    /// </summary>
    [InitializeOnLoad]
    internal static class DanteLonghornBoard
    {
        private const string Request = "Temp/dante-longhorn-board.request", OutDir = "Logs/paete-ability-film";
        private const int Width = 960, Height = 540;
        private static double _next;
        static DanteLonghornBoard() { EditorApplication.update += Poll; }

        // The longhorn's flat colours (the old cutscene's `UltimateProp.EarthPalette`, kept with its code under
        // tools/cutscene_rework/old/pre-astra-merge-2026-10-18): its rock, horns and muzzle are painted in its atlas.
        private static readonly Color[] Earth =
        {
            Hex(0x4B4441), Hex(0x322D2C), Hex(0x6B6058), Hex(0xFF7A18), Hex(0xFFC548), Hex(0xE6D6B4), Hex(0xA98F6C), Hex(0xFFA21F),
            Hex(0xFFF1B8), Hex(0xC23A16), Hex(0xF2E8D0), Hex(0x1E1A1A), Hex(0x8C8178), Hex(0xB8420E), Hex(0x7D726B), Hex(0x151212),
        };
        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            string tag = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            tag = new string(tag.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            if (tag.Length == 0) tag = "v00";
            Run(tag);
        }

        private static void Run(string tag)
        {
            string dir = Path.Combine(OutDir, "longhorn_" + tag);
            if (Directory.Exists(dir)) foreach (string old in Directory.GetFiles(dir)) File.Delete(old);
            Directory.CreateDirectory(dir);
            var report = new System.Text.StringBuilder();
            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            RenderTexture rt = null;
            HeroIntroductionScene intro = null;
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                // The intro film's own stage (`PaeteAbilityFilm.RunIntro`): a plain floor, a sun, him and three of the cast.
                var cam = In(new GameObject("~BoardCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.nearClipPlane = 0.05f; cam.farClipPlane = 260f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.40f, 0.46f, 0.56f);
                var sun = In(new GameObject("~BoardSun")).AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.05f; sun.color = new Color(1f, 0.98f, 0.95f);
                sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane));
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(10f, 1f, 10f);
                var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    var plain = new Material(shader) { color = new Color(0.56f, 0.56f, 0.53f), hideFlags = HideFlags.HideAndDontSave };
                    if (plain.HasProperty("_Glossiness")) plain.SetFloat("_Glossiness", 0f);
                    owned.Add(plain); ground.GetComponent<MeshRenderer>().sharedMaterial = plain;
                }
                var art = RosterBook.Load()?.FindPersonArt("dante");
                if (art == null || art.Model == null) throw new InvalidOperationException("The roster has no art for dante.");
                var sourceGo = In(new GameObject("~BoardSource"));
                sourceGo.AddComponent<CharacterController>();
                var source = sourceGo.AddComponent<CharacterMotor>();
                var body = In((GameObject)Object.Instantiate(art.Model));
                body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f));
                body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var a in body.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
                var animator = body.GetComponentInChildren<Animator>(true);
                var copy = new MatchPoseHistory.Copy(body);
                copy.ShowOnlyForCapture(true);
                var clip = HeroAbilityClips.BuildUltimateIntroduction(animator != null ? animator.transform : body.transform, "dante");
                var stage = In(new GameObject("~BoardStage"));
                HeroIntroductionScene.FilmStandIns.Clear(); HeroIntroductionScene.FilmCan = null; HeroIntroductionScene.FilmCompanion = null;
                var places = new[] { (new Vector3(-3.4f, 0f, 3.6f), 150f), (new Vector3(3.0f, 0f, 6.4f), 215f), (new Vector3(-1.2f, 0f, 9.0f), 175f) };
                int stood = 0;
                foreach (string other in new[] { "sean", "zack", "cheska" })
                {
                    var otherArt = RosterBook.Load()?.FindPersonArt(other);
                    if (otherArt == null || otherArt.Model == null) continue;
                    var holder = In(new GameObject("~BoardStandIn-" + other));
                    holder.transform.SetPositionAndRotation(places[stood].Item1, Quaternion.Euler(0f, places[stood].Item2, 0f));
                    var stand = (GameObject)Object.Instantiate(otherArt.Model, holder.transform);
                    stand.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    stand.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(stand, ToonSkin.PersonOutlineWidth, otherArt.Palette);
                    foreach (var a in stand.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                    holder.SetActive(false);
                    HeroIntroductionScene.FilmStandIns.Add(holder);
                    stood++;
                }
                HeroIntroductionScene.FilmCan = new Vector3(0f, 0f, 5.2f);
                intro = new HeroIntroductionScene(stage.transform, "dante", source, copy);
                intro.SetVisibleForCapture(true);

                // The longhorn, twice: solid and painted (as the old cutscene dressed it, a heavier ink for its size), and
                // as a spirit in the shader Makiling is drawn in.
                var solidHolder = In(new GameObject("~Longhorn"));
                var solid = UltimateProp.Spawn("longhorn", solidHolder.transform, Earth, ToonSkin.PersonOutlineWidth * 3.5f);
                var spiritHolder = In(new GameObject("~LonghornSpirit"));
                var spirit = UltimateProp.SpawnSpirit("longhorn", spiritHolder.transform, Earth, new Color(0.62f, 0.26f, 0.10f), new Color(1f, 0.62f, 0.22f), out var ghost);
                if (solid == null || spirit == null) throw new InvalidOperationException("Models/UltimateProps/longhorn is missing.");
                Transform Joint(GameObject m, string n) { foreach (var t in m.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
                var solidArm = Joint(solid, "arm-r"); var spiritArm = Joint(spirit, "arm-r");
                Quaternion solidRest = solidArm != null ? solidArm.localRotation : Quaternion.identity, spiritRest = spiritArm != null ? spiritArm.localRotation : Quaternion.identity;
                var bounds = new Bounds(); bool any = false;
                foreach (var r in solid.GetComponentsInChildren<Renderer>(true)) { if (any) bounds.Encapsulate(r.bounds); else { bounds = r.bounds; any = true; } }
                int parts = 0, triangles = 0;
                foreach (var f in solid.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh != null) { parts++; for (int s = 0; s < f.sharedMesh.subMeshCount; s++) triangles += (int)(f.sharedMesh.GetIndexCount(s) / 3); }
                report.AppendLine("the longhorn: " + parts + " meshes, " + triangles + " triangles");
                report.AppendLine("the longhorn as built: " + bounds.size.x.ToString("0.00") + " m wide, " + bounds.size.y.ToString("0.00") + " high, " + bounds.size.z.ToString("0.00") + " long; arm joint " + (solidArm != null ? "found" : "missing"));

                rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                owned.Add(shot);
                float end = intro.Seconds;
                report.AppendLine("the cutscene is " + end.ToString("0.00") + " s");

                // Each proposal: its moments, and where the longhorn is at each (which one, its place, turn, size, presence,
                // how far its arm is raised: 0 hanging, 1 overhead).
                var boards = new List<(string name, float[] times, Func<float, (bool solid, Vector3 at, float yaw, float size, float presence, float arm)> pose)>
                {
                    ("p1", new[] { 2.20f, 2.55f, 2.95f, 3.20f, 3.30f, 3.50f }, t =>
                    {
                        // Up out of the court behind him as he leaps and hangs; its fist up with his; down with his slam.
                        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.05f, 2.65f, t));
                        float arm = t < 3.18f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.3f, 2.9f, t)) : Mathf.Lerp(1f, -0.25f, Mathf.InverseLerp(3.18f, 3.27f, t));
                        float fade = 1f - Mathf.InverseLerp(3.45f, 3.7f, t);
                        return (false, new Vector3(0f, Mathf.Lerp(-5.5f, 0.3f, rise), -4.2f), 0f, 0.80f, rise * fade, arm);
                    }),
                    ("p2", new[] { 0.62f, 0.92f, 1.22f, 1.60f, 3.27f, 3.42f }, t =>
                    {
                        // Shown through him on each wave of the suit, his size; at the slam it breaks out forward.
                        float pulse = 0f;
                        foreach (float w in new[] { 0.58f, 0.88f, 1.18f }) pulse = Mathf.Max(pulse, Mathf.Exp(-6f * Mathf.Abs(t - w - 0.04f)));
                        float out_ = Mathf.InverseLerp(3.25f, 3.55f, t);
                        float presence = t < 2f ? pulse * 0.7f : Mathf.Sin(Mathf.Clamp01(out_) * Mathf.PI) * 1.0f;
                        return (false, new Vector3(0f, -0.15f, Mathf.Lerp(-0.2f, 4.5f, out_)), 0f, t < 2f ? 0.30f : Mathf.Lerp(0.30f, 0.55f, out_), presence, t < 2f ? 0.4f : 0.0f);
                    }),
                    ("p3", new[] { 3.25f, 3.33f, 3.42f, 3.55f, end - 0.15f, end }, t =>
                    {
                        // Out of the crater and down the lane, head low, as the first bands go.
                        float u = Mathf.InverseLerp(3.27f, end, t);
                        float up = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.25f, 3.40f, t));
                        return (true, new Vector3(0f, Mathf.Lerp(-3.2f, 0f, up), Mathf.Lerp(1.2f, 9.0f, u)), 0f, 0.48f, up, 0f);
                    }),
                };
                foreach (var (name, times, pose) in boards)
                    for (int k = 0; k < times.Length; k++)
                    {
                        float t = Mathf.Min(end, times[k]);
                        if (clip != null) clip.SampleAnimation(animator != null ? animator.gameObject : body, t);
                        intro.Sample(t, audible: false);
                        var p = pose(t);
                        solidHolder.SetActive(p.solid && p.presence > 0.01f);
                        spiritHolder.SetActive(!p.solid && p.presence > 0.01f);
                        var holder = p.solid ? solidHolder.transform : spiritHolder.transform;
                        holder.SetPositionAndRotation(stage.transform.TransformPoint(p.at), stage.transform.rotation * Quaternion.Euler(0f, p.yaw, 0f));
                        holder.localScale = Vector3.one * p.size;
                        if (ghost != null) UltimateProp.Presence(ghost, p.presence);
                        var arm = p.solid ? solidArm : spiritArm;
                        if (arm != null) arm.localRotation = (p.solid ? solidRest : spiritRest) * Quaternion.Euler(-130f * p.arm, 0f, 0f);
                        for (int view = 0; view < 2; view++)
                        {
                            Vector3 eye, focus; float fov;
                            if (view == 0) intro.Shot(t, out eye, out focus, out fov);
                            else { eye = stage.transform.TransformPoint(new Vector3(12.5f, 3.6f, 1.5f)); focus = stage.transform.TransformPoint(new Vector3(0f, 2.0f, 1.5f)); fov = 52f; }
                            cam.fieldOfView = fov;
                            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                            cam.Render();
                            var keep = RenderTexture.active; RenderTexture.active = rt;
                            shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); shot.Apply();
                            RenderTexture.active = keep;
                            File.WriteAllBytes(Path.Combine(dir, name + "_" + k + "_" + (view == 0 ? "cut" : "wide") + "_" + t.ToString("0.00") + ".png"), shot.EncodeToPNG());
                        }
                    }
                report.AppendLine("written to " + dir.Replace('\\', '/'));
                File.WriteAllText(Path.Combine(dir, "done.txt"), report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(Path.Combine(dir, "failed.txt"), report + "\n" + failure);
                Debug.LogError("[DanteLonghornBoard] " + failure);
            }
            finally
            {
                try { intro?.Dispose(); } catch (Exception) { }
                HeroIntroductionScene.FilmStandIns.Clear(); HeroIntroductionScene.FilmCan = null;
                RenderTexture.active = null;
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
