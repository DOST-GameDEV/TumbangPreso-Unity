using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// GLACIAL WALL AS A MATCH BUILDS IT. `CheskaWallFilm` films the approved look on its own; this films the REAL wall:
    /// `HeroHazards.SpawnIceBarricade` with `CryoRules`' length, radius and 10 s life, exactly as the cast calls it, so
    /// the look is seen through `CheskaGlacialWallPresenter` at the size and place the match puts it, with the slabs
    /// that block still in the wall and not drawn.
    ///
    /// Three passes, each at the real speed, 60 frames a second: the RISE (0 to 1.4 s), the END OF A FULL LIFE
    /// (8.6 s to just past 10 s, the sink and the expiry), and a second wall BROKEN EARLY at 1.5 s (the break's chunks
    /// and the crystals sinking behind them). Each frame is six cells. TOP: the target's eyes 7 m in front of her, her
    /// own eyes, the side. BOTTOM: above, a close three-quarter, and WHAT BLOCKS: the wall's own slabs (its colliders)
    /// drawn from the target's eyes with the crystals hidden, so the picture and the hit area are compared at the
    /// same moments.
    ///
    /// Request: a line `<tag>` in `Temp/cheska-wall-game.request`. Frames go to
    /// `Logs/paete-ability-film/fxbench_cheskawallgame_<tag>_frames/`, named like a bench film so `fxvideo.py` joins
    /// them, then `done_fxbench_cheskawallgame_<tag>.txt`, or `failed_...` with the exception. Never in Play; the open
    /// scene is not touched (a preview scene of Eskinita, daylight, no bloom).
    /// </summary>
    [InitializeOnLoad]
    internal static class CheskaWallGameFilmPoll
    {
        private const string Request = "Temp/cheska-wall-game.request";
        private static double _next;
        static CheskaWallGameFilmPoll() { EditorApplication.update += Poll; }

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
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0) PaeteAbilityFilm.RunCheskaWallGame(words[0]);
            }
        }
    }

    public static partial class PaeteAbilityFilm
    {
        internal static void RunCheskaWallGame(string tag)
        {
            const string map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
            Vector3 standAt = new Vector3(-2.5f, 0f, -6.0f);
            const float aimAhead = 3.0f, targetAhead = 7.0f, breakAt = 1.5f;
            float life = Core.CryoRules.GlacialWallSeconds;
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_cheskawallgame_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it."); return; }

            Scene scene = default;
            bool lit = false, wasOn = CheskaGlacialWallLook.Enabled;
            var owned = new List<Object>();
            var spawned = new List<GameObject>();
            var report = new System.Text.StringBuilder();
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(map);
                if (!scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                CheskaGlacialWallLook.Enabled = true;

                var cam = In(new GameObject("~WallGameCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                Vector3 forward = Vector3.forward, right = Vector3.right;
                float Road(Vector3 at)
                {
                    var physics = scene.GetPhysicsScene();
                    return physics.IsValid() && physics.Raycast(at + Vector3.up * 30f, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
                }
                Vector3 stand = standAt; stand.y = Road(stand);
                Vector3 wallAt = stand + forward * aimAhead; wallAt.y = Road(wallAt);
                Vector3 targetAt = stand + forward * targetAhead; targetAt.y = Road(targetAt);

                GameObject Person(string id, Vector3 at, float faces)
                {
                    var holder = In(new GameObject("~WallGame-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, faces, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                var her = Person("cheska", stand, 0f);
                var target = Person("dante", targetAt, 180f);

                // The wall exactly as the cast makes it (`CheskaHeroKit.GlacialWall.OnActivate`).
                GameObject Wall()
                {
                    var go = HeroHazards.SpawnIceBarricade(wallAt, forward, life, silent: true,
                        arcLength: Core.CryoRules.GlacialWallArcLength, arcRadius: Core.CryoRules.GlacialWallArcRadius);
                    In(go); spawned.Add(go);
                    return go;
                }
                var full = Wall();
                var fullLook = full.GetComponentInChildren<CheskaGlacialWallPresenter>(true);
                if (fullLook == null) throw new InvalidOperationException("The real wall carries no reworked look.");
                var slabs = full.GetComponentsInChildren<Renderer>(true).Where(r => !r.transform.IsChildOf(fullLook.transform)).ToArray();
                int slabColliders = full.GetComponentsInChildren<Collider>(true).Length;
                int calls = 0, triangles = 0;
                foreach (var r in fullLook.GetComponentsInChildren<MeshRenderer>(true))
                {
                    calls++;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) triangles += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                }
                report.AppendLine("bake: " + CheskaGlacialWallLook.BakeReport);
                report.AppendLine("the real wall: " + slabColliders + " colliders kept, " + slabs.Length + " old renderers switched off (" +
                                  slabs.Count(r => r.enabled) + " still on); the look: " + calls + " draws, " + triangles + " triangles");
                // From the meshes themselves (a switched-off renderer's bounds cannot be trusted), corner by corner.
                Bounds Of(IEnumerable<MeshFilter> parts)
                {
                    var b = new Bounds(); bool any = false;
                    foreach (var f in parts)
                    {
                        if (f == null || f.sharedMesh == null) continue;
                        var m = f.sharedMesh.bounds;
                        for (int c = 0; c < 8; c++)
                        {
                            var p = f.transform.TransformPoint(m.center + Vector3.Scale(m.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                            if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                        }
                    }
                    return b;
                }
                Bounds drawn = Of(fullLook.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name.StartsWith("Ice-")));
                Bounds blocks = Of(full.GetComponentsInChildren<Collider>(true).Select(c => c.GetComponent<MeshFilter>()));
                report.AppendLine("standing, at rest height (the crystals' rise overshoots 12 percent for a moment): drawn top " + (drawn.max.y - wallAt.y).ToString("0.00") +
                                  " m, blocking top " + (blocks.max.y - wallAt.y).ToString("0.00") + " m; drawn width " + drawn.size.x.ToString("0.00") +
                                  " m, blocking width " + blocks.size.x.ToString("0.00") + " m");

                var rt = new RenderTexture(WallCellW, WallCellH, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                owned.Add(rt);
                var cell = new Texture2D(WallCellW, WallCellH, TextureFormat.RGB24, false);
                var sheet = new Texture2D(WallCellW * 3, WallCellH * 2, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                cam.targetTexture = rt;

                void Shoot(Vector3 eye, Vector3 look, float fov, int column, int row)
                {
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = rt;
                    cell.ReadPixels(new Rect(0, 0, WallCellW, WallCellH), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    sheet.SetPixels(column * WallCellW, (1 - row) * WallCellH, WallCellW, WallCellH, cell.GetPixels());
                }
                Vector3 mid = wallAt + Vector3.up * 0.95f;
                Vector3 targetEye = targetAt + Vector3.up * 1.5f - forward * 0.15f;
                int frame = 0;
                void Frame(GameObject wallGo, GameObject lookGo, Renderer[] blockers, GameObject extra)
                {
                    target.SetActive(false);
                    Shoot(targetEye, mid, 70f, 0, 0);
                    target.SetActive(true);
                    her.SetActive(false);
                    Shoot(stand + Vector3.up * 1.5f + forward * 0.12f, mid - Vector3.up * 0.1f, 70f, 1, 0);
                    her.SetActive(true);
                    Shoot(wallAt + right * 7.5f + Vector3.up * 1.7f + forward * 0.3f, mid, 45f, 2, 0);
                    Shoot(wallAt + Vector3.up * 9.5f - forward * 2.6f, wallAt, 50f, 0, 1);
                    Shoot(wallAt + forward * 3.4f + right * 2.6f + Vector3.up * 1.5f, mid, 50f, 1, 1);
                    // What blocks: the slabs drawn, the look and any chunks hidden, from the target's eyes.
                    if (lookGo != null) lookGo.SetActive(false);
                    if (extra != null) extra.SetActive(false);
                    bool standing = wallGo != null && blockers != null;
                    if (standing) foreach (var r in blockers) if (r != null) r.enabled = true;
                    target.SetActive(false);
                    Shoot(targetEye, mid, 70f, 2, 1);
                    target.SetActive(true);
                    if (standing) foreach (var r in blockers) if (r != null) r.enabled = false;
                    if (lookGo != null) lookGo.SetActive(true);
                    if (extra != null) extra.SetActive(true);
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + frame.ToString("000") + ".png"), sheet.EncodeToPNG());
                    frame++;
                }

                // 1. The rise.
                for (float age = 0f; age <= 1.4f + 1e-4f; age += 1f / BenchRate) { fullLook.StepTo(age); Frame(full, fullLook.gameObject, slabs, null); }
                int rise = frame;
                // 2. The end of a full life: the sink, then the expiry the way the wall's own Update ends it.
                bool ended = false;
                GameObject fullLookGo = fullLook.gameObject;
                for (float age = life - 1.4f; age <= life + 0.2f + 1e-4f; age += 1f / BenchRate)
                {
                    if (!ended && age >= life)
                    {
                        ended = true;
                        bool quiet = CheskaGlacialWallPresenter.End(full.transform, 0f);
                        report.AppendLine("expiry: the look " + (quiet ? "ended it by sinking (no chunk burst)" : "did NOT take the ending"));
                        if (fullLookGo.scene != scene) In(fullLookGo);
                        Object.DestroyImmediate(full); full = null;
                    }
                    if (fullLook != null) fullLook.StepTo(age);
                    Frame(full, fullLookGo, full != null ? slabs : null, null);
                }
                int sink = frame - rise;
                if (fullLookGo != null) Object.DestroyImmediate(fullLookGo);
                // 3. A second wall broken early, as the third slipper hit does it.
                var broken = Wall();
                var brokenLook = broken.GetComponentInChildren<CheskaGlacialWallPresenter>(true);
                var brokenSlabs = broken.GetComponentsInChildren<Renderer>(true).Where(r => !r.transform.IsChildOf(brokenLook.transform)).ToArray();
                GameObject brokenLookGo = brokenLook.gameObject, chunks = null;
                CheskaWallBreak burst = null;
                float brokeAt = -1f;
                for (float age = breakAt - 0.2f; age <= breakAt + 1.0f + 1e-4f; age += 1f / BenchRate)
                {
                    if (brokeAt < 0f && age >= breakAt)
                    {
                        brokeAt = age;
                        brokenLook.StepTo(age);
                        bool quiet = CheskaGlacialWallPresenter.End(broken.transform, life - age);
                        report.AppendLine("early break at " + age.ToString("0.00") + " s: " + (quiet ? "the look swallowed the chunk burst (WRONG)" : "the chunk burst plays and the crystals sink"));
                        if (!quiet) CheskaIceVisuals.ShatterWall(broken.transform);
                        chunks = GameObject.Find("BarricadeThaw");
                        if (chunks != null) { In(chunks); spawned.Add(chunks); burst = chunks.GetComponent<CheskaWallBreak>(); }
                        if (brokenLookGo.scene != scene) In(brokenLookGo);
                        Object.DestroyImmediate(broken); broken = null;
                    }
                    if (brokenLook != null) brokenLook.StepTo(age);
                    if (burst != null) burst.StepTo(age - brokeAt);
                    Frame(broken, brokenLookGo, broken != null ? brokenSlabs : null, chunks);
                }
                if (brokenLookGo != null) Object.DestroyImmediate(brokenLookGo);
                report.AppendLine(frame + " frames at " + BenchRate + " a second: rise " + rise + " (0 to 1.4 s), end of life " + sink +
                                  " (" + (life - 1.4f).ToString("0.0") + " to " + (life + 0.2f).ToString("0.0") + " s), early break " + (frame - rise - sink) + " (" + breakAt + " s)");
                report.AppendLine("cells: top = target's eyes, her eyes, side; bottom = above, close three-quarter, WHAT BLOCKS (the wall's own slabs) from the target's eyes. No bloom.");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[CheskaWallGameFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[CheskaWallGameFilm] " + failure);
            }
            finally
            {
                CheskaGlacialWallLook.Enabled = wasOn;
                foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
                HazardMap.PruneInactive();
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture texture) texture.Release();
                    Object.DestroyImmediate(thing);
                }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
