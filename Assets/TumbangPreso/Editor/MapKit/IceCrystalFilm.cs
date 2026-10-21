using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// The sculpted and baked ice crystals (`tools/build_ice_crystals.py`, `Resources/Shaders/IceCrystal.shader`), each
    /// turned once round on a dark floor and filmed inside the OPEN editor, without touching the open scene.
    ///
    /// ⚠️ WHY IT IS ITS OWN FILE WITH ITS OWN REQUEST. `PaeteAbilityFilm` and its effects bench are being worked on by
    /// other sessions while this one is written, and a film that shared their request file or their partial class
    /// would be saved over by them. So this one watches `Temp/ice-crystal-film.request` (one line: a tag) and nothing
    /// else, and is built the way `PaeteAbilityFilm.FxBench.cs` is: a PREVIEW scene that is closed again, nothing
    /// opened or saved, never in Play.
    ///
    /// The stage is what a crystal has to be judged on: a dark floor, one key light (the shader's own, a property, high,
    /// to one side and a little in front, so the faces turned to it go pale and the ones turned away go deep and are
    /// lit through), and soft additive halo cards. The lens and the light stay; the crystal turns through 360 degrees
    /// in 120 frames. Five subjects, so 600 frames: the spike, the shard, the stub, the cluster, and then the spike AS
    /// IT IS MEANT TO BE USED: twelve of them leaning outward in a ring, an ice lotus.
    ///
    /// ⚠️ THERE ARE TWO SETS AND THE FIRST IS THE GAME'S. The owner looked at both and said of the first: "this old one
    /// was better". So `Models/IceCrystals` with `Shaders/IceCrystal` is the first set, the INSTALLED one, and it is
    /// filmed exactly as it was when he approved it, halos and all: a tip card, a floor card and one broad card
    /// behind. The second set (many more faces, a flood of light at the foot, edges drawn in the atlas) is kept
    /// beside it and loaded by nothing: `Models/IceCrystalsRound2/ice2_*` with `Shaders/IceCrystalRound2`. A request
    /// line of `tag 2` films that one, with its own tight halos, so the two can still be laid side by side.
    ///
    /// Writes `Logs/ice-crystals/film_<tag>_frames/f000.png` onward and then `Logs/ice-crystals/done_<tag>.txt`, or
    /// `failed_<tag>.txt` with the exception. `tools/ice_crystal_film.py` asks for it, waits, and joins the video.
    ///
    /// ⚠️ NOTHING BLOOMS HERE, as nothing blooms on the effects bench: a halo in this film is a card the game can draw.
    /// </summary>
    [InitializeOnLoad]
    public static class IceCrystalFilm
    {
        private const string Request = "Temp/ice-crystal-film.request", OutDir = "Logs/ice-crystals";
        private const int Width = 960, Height = 540, Turn = 120;
        private static readonly string[] Kinds = { "spike", "shard", "stub", "cluster" };
        private static double _nextPoll;

        /// <summary>Where one set of crystals lives and what draws it.</summary>
        private sealed class Set
        {
            public string Models, AssetFolder, Prefix, Shader;
            public bool RoundTwo;
        }

        private static readonly Set Installed = new Set
        {
            Models = "Models/IceCrystals/", AssetFolder = "Assets/TumbangPreso/Resources/Models/IceCrystals/",
            Prefix = "ice_", Shader = "Shaders/IceCrystal", RoundTwo = false,
        };

        private static readonly Set KeptAside = new Set
        {
            Models = "Models/IceCrystalsRound2/", AssetFolder = "Assets/TumbangPreso/Resources/Models/IceCrystalsRound2/",
            Prefix = "ice2_", Shader = "Shaders/IceCrystalRound2", RoundTwo = true,
        };

        // The lotus: twelve spikes, each typed. How far each leans from upright, how big it is, how it is turned about
        // its own length, and how far off its even place round the ring it stands. No two alike, none a mirror.
        private static readonly float[] LotusLean = { 52f, 40f, 60f, 46f, 56f, 38f, 50f, 62f, 43f, 58f, 41f, 54f };
        private static readonly float[] LotusSize = { 0.62f, 0.76f, 0.58f, 0.70f, 0.66f, 0.80f, 0.60f, 0.72f, 0.64f, 0.78f, 0.59f, 0.68f };
        private static readonly float[] LotusTwist = { 20f, 140f, 260f, 75f, 310f, 190f, 40f, 230f, 110f, 350f, 165f, 285f };
        private static readonly float[] LotusOff = { 0f, 5f, -3f, 6f, -4f, 2f, -6f, 4f, 0f, -5f, 3f, -2f };

        static IceCrystalFilm() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // Whoever asked has usually just written a model, a map or the shader, and an editor in the background
            // does not look for changes by itself: look now, and if that starts a compile, the request waits.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0) Run(words[0], words.Length > 1 && words[1] == "2");
            }
        }

        [MenuItem("Tumbang Preso/Effects/Film Ice Crystals")]
        private static void RunFromMenu() => Run("menu");

        [MenuItem("Tumbang Preso/Effects/Film Ice Crystals (second set, kept aside)")]
        private static void RunSecondFromMenu() => Run("menu2", true);

        /// <summary>
        /// The normal map and the mask are DATA and the shader reads them as plain numbers, so both must be imported
        /// linear and as default textures. A texture dropped into the project is imported as sRGB colour, which would
        /// bend every mask toward black and tilt every normal. Returns how many imports it had to correct.
        /// </summary>
        public static int EnsureImports() => EnsureImports(Installed);

        private static int EnsureImports(Set set)
        {
            int changed = 0;
            foreach (string kind in Kinds)
                foreach (string suffix in new[] { "_normal", "_mask", "_albedo" })
                {
                    var importer = AssetImporter.GetAtPath(set.AssetFolder + set.Prefix + kind + suffix + ".png") as TextureImporter;
                    if (importer == null) continue;
                    bool data = suffix != "_albedo";
                    bool wrong = importer.textureType != TextureImporterType.Default || importer.sRGBTexture == data
                              || importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp
                              || importer.alphaSource != TextureImporterAlphaSource.FromInput;
                    if (!wrong) continue;
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = !data;
                    // The glint and the thickness ride in alpha: it is a channel, not a coverage.
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                    changed++;
                }
            return changed;
        }

        public static void Run(string tag, bool secondSet = false)
        {
            var set = secondSet ? KeptAside : Installed;
            Directory.CreateDirectory(OutDir);
            // A tag ends up in file names, and it came from a file anyone can write.
            tag = new string((tag ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string donePath = Path.Combine(OutDir, "done_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it.");
                return;
            }

            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            RenderTexture rt = null;
            try
            {
                int corrected = EnsureImports(set);
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }

                var ice = Resources.Load<Shader>(set.Shader);
                var haloShader = Resources.Load<Shader>("Shaders/IceCrystalHalo");
                if (ice == null || !ice.isSupported) throw new InvalidOperationException(set.Shader + " is missing or did not compile (read Logs/Editor.log).");
                if (haloShader == null || !haloShader.isSupported) throw new InvalidOperationException("Shaders/IceCrystalHalo is missing or did not compile.");

                var cam = In(new GameObject("~IceFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
                cam.allowHDR = false; cam.fieldOfView = 26f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                var backdrop = new Color(0.030f, 0.033f, 0.042f);
                cam.backgroundColor = backdrop;

                // The one key light. The crystals do not read the scene's lights (the shader has the reason), so this
                // lamp lights nothing here; it stands where the shader is told the light is, so the two cannot disagree
                // if something lit is ever added to this stage.
                var key = In(new GameObject("~IceFilmKey")).AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 0.55f; key.color = new Color(0.72f, 0.86f, 1f);
                key.shadows = LightShadows.None;
                Vector3 towardKey = new Vector3(-0.62f, 0.60f, -0.22f).normalized;
                key.transform.rotation = Quaternion.LookRotation(-towardKey, Vector3.up);

                // The floor is the backdrop's colour, so no horizon line is drawn across the picture.
                var floorMaterial = new Material(Shader.Find("Unlit/Color")) { color = backdrop, hideFlags = HideFlags.HideAndDontSave };
                owned.Add(floorMaterial);
                var floor = In(GameObject.CreatePrimitive(PrimitiveType.Plane));
                floor.name = "~IceFilmFloor";
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.transform.localScale = new Vector3(20f, 1f, 20f);
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

                var card = new Mesh { name = "~IceHaloCard", hideFlags = HideFlags.HideAndDontSave };
                card.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
                card.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                card.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                card.RecalculateBounds();
                owned.Add(card);

                rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                owned.Add(shot);
                string frames = Path.Combine(OutDir, "film_" + tag + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                var notes = new List<string> { secondSet ? "the second set, kept aside (" + set.Models + ")" : "the installed set (" + set.Models + ")" };
                if (corrected > 0) notes.Add(corrected + " texture imports corrected to what the shader reads");
                int frame = 0;

                Material Dressed(string name)
                {
                    var material = new Material(ice) { name = "IceCrystal_" + name, hideFlags = HideFlags.HideAndDontSave };
                    owned.Add(material);
                    Texture2D Map(string suffix)
                    {
                        var map = Resources.Load<Texture2D>(set.Models + name + suffix);
                        if (map == null) throw new InvalidOperationException("No map at Resources/" + set.Models + name + suffix + ".");
                        return map;
                    }
                    material.SetTexture("_MainTex", Map("_albedo"));
                    material.SetTexture("_BumpMap", Map("_normal"));
                    material.SetTexture("_MaskTex", Map("_mask"));
                    material.SetVector("_KeyDir", towardKey);
                    return material;
                }

                // One crystal, stood on its parent's origin whatever its own origin is. The parent must not have been
                // moved yet: the crystal is measured where it stands.
                GameObject Stand(string name, Transform parent, Material material, out float tall, out float wide, out int triangles, out Vector3 tip)
                {
                    var prefab = Resources.Load<GameObject>(set.Models + name);
                    if (prefab == null) throw new InvalidOperationException("No model at Resources/" + set.Models + name + ".");
                    var model = (GameObject)Object.Instantiate(prefab, parent);
                    model.transform.localPosition = Vector3.zero;
                    triangles = 0;
                    Vector3 highest = Vector3.down * 1000f;
                    var bounds = new Bounds(Vector3.zero, Vector3.zero);
                    bool any = false;
                    foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var mesh = filter.sharedMesh;
                        if (mesh == null) continue;
                        if (mesh.tangents == null || mesh.tangents.Length != mesh.vertexCount)
                        {
                            // The normal map is laid in the mesh's tangents. A model that came in without them gets a
                            // copy that has them; the note says so, because the bake was made against the exporter's.
                            mesh = Object.Instantiate(mesh);
                            mesh.hideFlags = HideFlags.HideAndDontSave;
                            mesh.RecalculateTangents();
                            owned.Add(mesh);
                            filter.sharedMesh = mesh;
                            notes.Add(name + ": the model has no tangents, they were worked out here");
                        }
                        triangles += mesh.triangles.Length / 3;
                        // Where its point is: the tip is cut off centre, so a halo set over the middle would hang beside it.
                        foreach (var corner in mesh.vertices)
                        {
                            Vector3 at = filter.transform.TransformPoint(corner);
                            if (at.y > highest.y) highest = at;
                        }
                        var renderer = filter.GetComponent<MeshRenderer>();
                        if (renderer == null) continue;
                        renderer.sharedMaterial = material;
                        if (!any) { bounds = renderer.bounds; any = true; } else bounds.Encapsulate(renderer.bounds);
                    }
                    if (!any) throw new InvalidOperationException(name + " has no mesh to draw.");
                    model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
                    tall = bounds.size.y; wide = Mathf.Max(bounds.size.x, bounds.size.z);
                    tip = highest.y > -999f ? highest + model.transform.localPosition : new Vector3(0f, tall, 0f);
                    return model;
                }

                Transform Halo(Transform parent, string label, Vector3 local, float size, Color colour, float strength, bool flat)
                {
                    var look = new Material(haloShader) { hideFlags = HideFlags.HideAndDontSave };
                    look.SetColor("_Color", colour); look.SetFloat("_Strength", strength);
                    owned.Add(look);
                    var go = new GameObject("~IceHalo_" + label);
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = local;
                    go.transform.localScale = Vector3.one * size;
                    if (flat) go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    go.AddComponent<MeshFilter>().sharedMesh = card;
                    go.AddComponent<MeshRenderer>().sharedMaterial = look;
                    return go.transform;
                }

                void Shoot(Transform pivot, Material material, Vector3 eye, Vector3 focus, (Transform at, Vector3 home, float toward)[] facing)
                {
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                    for (int f = 0; f < Turn; f++, frame++)
                    {
                        pivot.rotation = Quaternion.Euler(0f, 360f * f / Turn, 0f);
                        material.SetFloat("_Age", f / 60f);
                        // The upright cards face the lens, and sit a little toward it (or behind) so the crystal does not
                        // cut them in half. Each keeps its own place on the crystal as the crystal turns.
                        foreach (var (at, home, toward) in facing)
                        {
                            at.rotation = cam.transform.rotation;
                            Vector3 turned = pivot.rotation * home;
                            at.position = turned + (eye - turned).normalized * toward;
                        }
                        cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = rt;
                        shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); shot.Apply();
                        RenderTexture.active = keep;
                        File.WriteAllBytes(Path.Combine(frames, "f" + frame.ToString("000") + ".png"), shot.EncodeToPNG());
                    }
                }

                var aqua = new Color(0.05f, 0.50f, 1.00f);
                var cyan = new Color(0.02f, 0.70f, 0.90f);
                foreach (string kind in Kinds)
                {
                    string name = set.Prefix + kind;
                    var pivot = In(new GameObject("~IcePivot_" + name));
                    var material = Dressed(name);
                    Stand(name, pivot.transform, material, out float tall, out float wide, out int triangles, out Vector3 tipAt);

                    (Transform at, Vector3 home, float toward)[] facing;
                    if (!set.RoundTwo)
                    {
                        // ⚠️ THE INSTALLED SET'S HALOS ARE THE ONES THE OWNER APPROVED, NUMBER FOR NUMBER: one at the tip,
                        // one broad one behind the whole crystal, one lying on the floor at the foot. Do not tidy them.
                        var tip = Halo(pivot.transform, "tip", new Vector3(0f, tall * 0.90f, 0f), Mathf.Max(0.5f, wide * 1.5f), aqua, 0.34f, false);
                        float behind = Mathf.Max(tall * 1.5f, wide * 1.7f);
                        // Its middle is never lower than half its own height, so the floor does not cut a line across it.
                        float bodyHeight = Mathf.Max(tall * 0.50f, behind * 0.5f);
                        var body = Halo(pivot.transform, "body", new Vector3(0f, bodyHeight, 0f), behind, new Color(0.03f, 0.22f, 0.62f), 0.26f, false);
                        Halo(pivot.transform, "foot", new Vector3(0f, 0.004f, 0f), wide * 2.6f, new Color(0.03f, 0.34f, 0.80f), 0.55f, true);
                        facing = new[] { (tip, new Vector3(0f, tall * 0.90f, 0f), wide * 0.55f), (body, new Vector3(0f, bodyHeight, 0f), -wide * 0.8f) };
                    }
                    else
                    {
                        // The second set's: tight and cyan, on the foot's flood and at the tip, and a pool on the floor.
                        var foot = Halo(pivot.transform, "foot", Vector3.zero, wide * 1.7f, cyan, 0.30f, false);
                        var tip = Halo(pivot.transform, "tip", Vector3.zero, Mathf.Max(0.30f, wide * 0.9f), cyan, 0.22f, false);
                        Halo(pivot.transform, "pool", new Vector3(0f, 0.004f, 0f), wide * 2.1f, cyan, 0.50f, true);
                        // The foot card is never set lower than most of its own half height, so the floor does not cut a straight line across its glow.
                        facing = new[] { (foot, new Vector3(0f, Mathf.Max(tall * 0.13f, wide * 1.7f * 0.40f), 0f), wide * 0.55f),
                                         (tip, Vector3.Lerp(new Vector3(0f, tall, 0f), tipAt, 0.9f) - Vector3.up * tall * 0.05f, wide * 0.30f) };
                    }

                    float reach = Mathf.Max(tall * 1.18f, wide * 1.5f);
                    float distance = reach * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    Vector3 focus = new Vector3(0f, tall * 0.50f, 0f);
                    Vector3 eye = focus + Quaternion.Euler(9f, 0f, 0f) * new Vector3(0f, 0f, -distance);
                    Shoot(pivot.transform, material, eye, focus, facing);
                    notes.Add(name + ": " + triangles + " triangles, " + tall.ToString("0.00") + " m tall, " + wide.ToString("0.00") + " m wide");
                    Object.DestroyImmediate(pivot);
                }

                // THE LOTUS: where the spike is headed. Twelve of them in a ring, feet together, leaning outward.
                {
                    string spike = set.Prefix + "spike";
                    var pivot = In(new GameObject("~IcePivot_lotus"));
                    var material = Dressed(spike);
                    var up = Vector3.up;
                    for (int i = 0; i < 12; i++)
                    {
                        var holder = new GameObject("~IcePetal_" + i);
                        holder.transform.SetParent(pivot.transform, false);
                        Stand(spike, holder.transform, material, out _, out _, out _, out _);
                        Vector3 outward = Quaternion.Euler(0f, i * 30f + LotusOff[i], 0f) * Vector3.forward;
                        Quaternion tilt = Quaternion.AngleAxis(LotusLean[i], Vector3.Cross(up, outward));
                        holder.transform.localRotation = tilt * Quaternion.Euler(0f, LotusTwist[i], 0f);
                        holder.transform.localScale = Vector3.one * LotusSize[i];
                        // Its foot is sunk a little into the heart of the flower, so the twelve grow from one place.
                        holder.transform.localPosition = outward * 0.07f - (tilt * up) * 0.10f;
                    }
                    var glow = set.RoundTwo ? cyan : aqua;
                    var heart = Halo(pivot.transform, "heart", Vector3.zero, 1.25f, glow, 0.42f, false);
                    Halo(pivot.transform, "pool", new Vector3(0f, 0.004f, 0f), 2.6f, glow, 0.45f, true);
                    float distance = 2.2f * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    Vector3 focus = new Vector3(0f, 0.22f, 0f);
                    Vector3 eye = focus + Quaternion.Euler(33f, 0f, 0f) * new Vector3(0f, 0f, -distance);
                    Shoot(pivot.transform, material, eye, focus, new[] { (heart, new Vector3(0f, 0.50f, 0f), 0.30f) });
                    notes.Add("lotus: twelve " + spike + " in a ring");
                    Object.DestroyImmediate(pivot);
                }

                File.WriteAllText(donePath, frame + " frames, " + Turn + " a subject, in " + frames.Replace('\\', '/') + "\n" + string.Join("\n", notes) + "\n");
                Debug.Log("[IceCrystalFilm] " + tag + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, failure.ToString());
                Debug.LogError("[IceCrystalFilm] " + failure);
            }
            finally
            {
                if (rt != null) { RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); }
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
