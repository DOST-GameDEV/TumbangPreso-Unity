using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// BOULDER, FILMED WHERE IT IS PLAYED. The bench is a dark floor with bloom; the game is a sunlit court and no camera
    /// that blooms. This films Basilio's BOULDER on a real map opened as a PREVIEW scene (the scene the owner has open is
    /// not touched and nothing is saved), with real bodies and the real tsinelas, the way `ZackCircuitFilm.cs` films the
    /// Quick Circuit.
    ///
    /// Each frame is nine pictures. DOWN: third person (close on his hand for the cast, back for the throw, close on the
    /// hit), his own eyes, and the eyes of the player it is thrown at, 6 m in front of him. ACROSS: the OLD look (the
    /// stone inlay drawn on the shoe), the NEW look, and the new look through the bench's bloom.
    ///
    /// ⚠️ IT IS STAGED, AND WHAT IS STAGED IS SAID HERE SO NOBODY TAKES IT FOR THE GAME:
    ///   * NOTHING IS HOOKED INTO A MATCH. The new look is the bench's effect (`FxStudyDanteBoulder`) stood on the court
    ///     and played from its own clock; the real tsinelas is put where that effect says the shoe is. No cast, no
    ///     `Slipper`, no throw and no hit of the game's runs.
    ///   * HIS BODY IS A STILL POSE. Nothing animates outside Play, so he stands as his model stands and the shoe hangs
    ///     in the air beside his right hip where a held shoe would be: his hand is not on it.
    ///   * HIS OWN EYES ARE A CAMERA AT HIS HEAD, AND THE SHOE IS HELD UP INTO ITS VIEW. The first-person arms are not
    ///     drawn. A shoe at his hip is out of sight from his own eyes, so for this row alone a second copy of the effect
    ///     holds it where a first-person hand would (half a metre ahead, low and to the right): it shows how the stone
    ///     sits in his view and how big, not the hand that holds it, and the rocks rise to it from the ground.
    ///   * THE OLD LOOK is `Shaders/BoulderLoad` laid on a copy of the shoe's mesh by the same arithmetic
    ///     `DanteBoulderCoating.RefreshMesh` uses, while the shoe is loaded. That component itself is not run (it reads
    ///     a live `Slipper`). The old look has no cast and no hit of its own, so its column is the shoe alone then.
    ///
    /// Request: a line `<tag> [map] [x z yaw] [flip]` in `Temp/dante-boulder-film.request` (its own file, so no other
    /// film's reader is touched). `map` is `eskinita` (the default), `ilalim` or `lagoon`; `x z yaw` say where he stands;
    /// `flip` turns the shoe end for end if its toe was guessed wrong. A tag that begins `still` draws and keeps eight
    /// moments only. Frames go to `Logs/paete-ability-film/fxbench_dantecourt_<tag>_frames/`, named like a bench film so
    /// the same tools join them, then `done_fxbench_dantecourt_<tag>.txt` with what it costs to draw, or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class DanteBoulderFilmPoll
    {
        private const string Request = "Temp/dante-boulder-film.request";
        private static double _next;
        static DanteBoulderFilmPoll() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // An editor in the background does not look for changed files by itself: look now, and if that starts a
            // compile the request waits for the reload.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                // `ping`: answered at once and films nothing. A save script asks it first, because this reader does not
                // run while the editor is in Play: no answer means do not save now.
                if (words[0] == "ping") { Directory.CreateDirectory("Logs/paete-ability-film"); File.WriteAllText("Logs/paete-ability-film/done_dante_ping.txt", DateTime.Now.ToString("HH:mm:ss")); continue; }
                float[] spot = null;
                var plain = System.Globalization.CultureInfo.InvariantCulture;
                if (words.Length >= 5 && float.TryParse(words[2], System.Globalization.NumberStyles.Float, plain, out float x)
                    && float.TryParse(words[3], System.Globalization.NumberStyles.Float, plain, out float z)
                    && float.TryParse(words[4], System.Globalization.NumberStyles.Float, plain, out float yaw)) spot = new[] { x, z, yaw };
                PaeteAbilityFilm.RunDanteCourt(words[0], words.Length > 1 ? words[1] : "eskinita", spot, words.Contains("flip"));
            }
        }
    }

    public static partial class PaeteAbilityFilm
    {
        private const int DanteCellWidth = 480, DanteCellHeight = 270;
        private const float DanteTargetAhead = 6.0f, DanteCourtLight = 0.6f, DanteStandInShoe = 0.44f, DanteEye = 1.15f;

        // Each map he can be filmed on: its scene, and where he stands and which way he faces (x, z, degrees).
        private static (string scene, Vector3 spot) DanteMapOf(string map)
        {
            switch ((map ?? "").ToLowerInvariant())
            {
                case "ilalim": return ("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity", new Vector3(0f, 0f, 0f));
                case "lagoon": return ("Assets/TumbangPreso/Scenes/Maps/LagoonCove.unity", new Vector3(0f, 0f, 0f));
                // Eskinita: on the pale court, clear of the houses either side (the spot the Quick Circuit film stands on).
                default: return ("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity", new Vector3(-2.5f, -6.0f, 0f));
            }
        }

        internal static void RunDanteCourt(string tag, string map, float[] spot, bool flip)
        {
            var chosen = DanteMapOf(map);
            if (spot != null) chosen.spot = new Vector3(spot[0], spot[1], spot[2]);
            float yaw = chosen.spot.z;
            Directory.CreateDirectory(OutDir);
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToLowerInvariant();
            if (tag.Length == 0) tag = "v00";
            string named = "fxbench_dantecourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it.");
                return;
            }

            Scene scene = default;
            bool lit = false;
            var owned = new List<Object>();
            var report = new System.Text.StringBuilder();
            FxStudyDanteBoulder fx = null, fxEye = null;
            Vector3 benchKey = FxStudyDanteBoulder.KeyLight;
            try
            {
                try { scene = EditorSceneManager.OpenPreviewScene(chosen.scene); }
                catch (Exception refused) { report.AppendLine("The map did not open as a preview scene (" + refused.Message + ")."); }
                if (!scene.IsValid()) throw new InvalidOperationException("No map to film on.");
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }
                report.AppendLine("map: " + chosen.scene);
                bool stills = tag.StartsWith("still");

                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var cam = In(new GameObject("~DanteFilmCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.06f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.70f, 0.88f);

                Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                float road = 0f;
                var physics = scene.GetPhysicsScene();
                var over = new Vector3(chosen.spot.x, 30f, chosen.spot.y);
                if (physics.IsValid() && physics.Raycast(over, Vector3.down, out var under, 80f, ~0, QueryTriggerInteraction.Ignore))
                { road = under.point.y; report.AppendLine("road found at y " + road.ToString("0.000") + " on " + under.collider.name); }
                else report.AppendLine("no road found in the preview scene's physics; standing at y 0");
                Vector3 stand = new Vector3(chosen.spot.x, road, chosen.spot.y), targetAt = stand + forward * DanteTargetAhead;

                // The stone is shaded by a light of its own: on a court that is the map's sun.
                Light sun = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var light in root.GetComponentsInChildren<Light>(false))
                        if (sun == null && light.type == LightType.Directional && light.enabled) sun = light;
                if (sun != null) { FxStudyDanteBoulder.KeyLight = -sun.transform.forward; report.AppendLine("the stone is lit from the map's sun (" + sun.name + ")"); }
                else report.AppendLine("no sun found on the map: the stone keeps the bench's light");

                // A person: a holder that faces the way he faces, and his model under it turned the way models are.
                GameObject Person(string id, Vector3 at, float facing, out GameObject model)
                {
                    model = null;
                    var holder = In(new GameObject("~DanteFilm-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, facing, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) return holder;
                    model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    return holder;
                }
                report.AppendLine("he stands at (" + stand.x.ToString("0.0") + ", " + stand.z.ToString("0.0") + ") facing " + yaw.ToString("0") + " degrees; the target is " + DanteTargetAhead.ToString("0.0") + " m in front of him");
                Person("dante", stand, yaw, out var danteModel);
                if (danteModel == null) report.AppendLine("Basilio's model was not found: there is no caster in this film.");
                var target = Person("zack", targetAt, yaw + 180f, out _);
                Person("amihan", stand + forward * 2.6f - right * 3.6f, yaw + 90f, out _);

                // The real tsinelas, as a match dresses it: its sole's middle on the holder, its length along the holder's z.
                var shoe = In(new GameObject("~DanteFilm-tsinelas"));
                float fit = 1f;
                var shoeArt = RosterBook.Load()?.SlipperArt(0);
                var inlays = new List<GameObject>();
                if (shoeArt != null && shoeArt.Model != null)
                {
                    var model = (GameObject)Object.Instantiate(shoeArt.Model, shoe.transform);
                    foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                    var box = new Bounds(); bool any = false;
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) { if (any) box.Encapsulate(renderer.bounds); else { box = renderer.bounds; any = true; } }
                    // Its long way lies along the holder's z; if the model's is along x, it is turned a quarter.
                    if (box.size.x > box.size.z) { model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * model.transform.localRotation; box = new Bounds(new Vector3(-box.center.z, box.center.y, box.center.x), new Vector3(box.size.z, box.size.y, box.size.x)); }
                    if (flip) { model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation; box.center = new Vector3(-box.center.x, box.center.y, -box.center.z); }
                    fit = box.size.z / DanteStandInShoe;
                    model.transform.localPosition += new Vector3(-box.center.x, -box.min.y - 0.024f * fit, -box.center.z);
                    report.AppendLine("the real tsinelas is " + box.size.z.ToString("0.000") + " m long, " + box.size.x.ToString("0.000") + " wide, " + box.size.y.ToString("0.000") + " high: the rocks are drawn at " + fit.ToString("0.00") + " of the size they were carved at" + (flip ? " (shoe turned end for end)" : ""));

                    // THE OLD LOOK, staged: the stone inlay shader on a copy of each of the shoe's meshes, by the sums of
                    // `DanteBoulderCoating.RefreshMesh`.
                    var inlayShader = Resources.Load<Shader>("Shaders/BoulderLoad");
                    if (inlayShader == null) report.AppendLine("Shaders/BoulderLoad is missing: the old column is the bare shoe");
                    else foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var mesh = filter.sharedMesh;
                        if (mesh == null) continue;
                        var go = new GameObject("BoulderStoneInlay") { hideFlags = HideFlags.DontSave };
                        go.transform.SetParent(filter.transform, false);
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = go.AddComponent<MeshRenderer>();
                        var material = new Material(inlayShader) { hideFlags = HideFlags.HideAndDontSave };
                        owned.Add(material);
                        var slots = new Material[Mathf.Max(1, mesh.subMeshCount)];
                        for (int i = 0; i < slots.Length; i++) slots[i] = material;
                        renderer.sharedMaterials = slots;
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        Vector3 size = mesh.bounds.size;
                        Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
                        Vector3 normal = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
                        if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = along == Vector3.up ? Vector3.right : Vector3.up;
                        Vector3 across = Vector3.Cross(normal, along);
                        float length = Mathf.Max(size.x, size.y, size.z), width = Mathf.Abs(Vector3.Dot(size, across));
                        var block = new MaterialPropertyBlock();
                        block.SetVector("_Centre", mesh.bounds.center);
                        block.SetVector("_Along", along / Mathf.Max(length, .0001f));
                        block.SetVector("_Across", across / Mathf.Max(width, .0001f));
                        block.SetFloat("_Lift", Mathf.Max(length * .002f, .0001f));
                        renderer.SetPropertyBlock(block);
                        inlays.Add(go);
                    }
                }
                else report.AppendLine("the roster's first tsinelas was not found: there is no shoe in this film");

                // THE NEW LOOK, staged: the bench's effect stood where he stands, +x the way he throws.
                var stage = In(new GameObject("~DanteFilm-stage"));
                stage.transform.SetPositionAndRotation(stand, Quaternion.Euler(0f, yaw - 90f, 0f));
                // Held at his right hip; it hits the target's chest. (These bodies stand about 1.3 m tall.)
                var hit = new Vector3(DanteTargetAhead - 0.36f, 0.74f, 0f);
                fx = new FxStudyDanteBoulder(stage.transform, new Vector3(0.30f, 0.62f, -0.44f), hit, false, fit, DanteCourtLight);
                // For his own eyes only: held up where a first-person hand holds a shoe.
                fxEye = new FxStudyDanteBoulder(stage.transform, new Vector3(0.52f, DanteEye - 0.26f, -0.22f), hit, false, fit, DanteCourtLight);

                var plainTarget = new RenderTexture(DanteCellWidth, DanteCellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var hdrTarget = new RenderTexture(DanteCellWidth, DanteCellHeight, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                var shown = new RenderTexture(DanteCellWidth, DanteCellHeight, 0, RenderTextureFormat.ARGB32);
                owned.Add(plainTarget); owned.Add(hdrTarget); owned.Add(shown);
                var bloomShader = Shader.Find("Hidden/TumbangPreso/FxStudyBloom");
                Material bloomMaterial = bloomShader != null ? new Material(bloomShader) { hideFlags = HideFlags.HideAndDontSave } : null;
                if (bloomMaterial != null) owned.Add(bloomMaterial); else report.AppendLine("the bench bloom shader is missing: the bloom column is the plain picture again");
                var cell = new Texture2D(DanteCellWidth, DanteCellHeight, TextureFormat.RGB24, false);
                var sheet = new Texture2D(DanteCellWidth * 3, DanteCellHeight * 3, TextureFormat.RGB24, false);
                owned.Add(cell); owned.Add(sheet);
                string frames = Path.Combine(OutDir, named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                void Shoot(bool bloom, int column, int row)
                {
                    cam.allowHDR = bloom && bloomMaterial != null;
                    cam.targetTexture = cam.allowHDR ? hdrTarget : plainTarget;
                    Shader.SetGlobalFloat("_FxStudyBloom", cam.allowHDR ? 1f : 0f);
                    cam.Render();
                    if (cam.allowHDR) Bloom(hdrTarget, shown, bloomMaterial, BloomIntensity);
                    var keep = RenderTexture.active; RenderTexture.active = cam.allowHDR ? shown : plainTarget;
                    cell.ReadPixels(new Rect(0, 0, DanteCellWidth, DanteCellHeight), 0, 0); cell.Apply();
                    RenderTexture.active = keep;
                    Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                    sheet.SetPixels(column * DanteCellWidth, (2 - row) * DanteCellHeight, DanteCellWidth, DanteCellHeight, cell.GetPixels());
                }

                // The effect's own times (`FxStudyDanteBoulder`): cast 0.20, loaded from 0.92, thrown 2.50, hit 2.90.
                const float loadedFrom = 0.20f, hitAt = 2.90f;
                float[] kept = { 0.45f, 0.72f, 0.80f, 1.30f, 1.80f, 2.75f, 2.93f, 3.30f };
                float seconds = fx.Seconds;
                int count = Mathf.CeilToInt(seconds * BenchRate) + 1;
                string cost = null;
                for (int f = 0; f < count; f++)
                {
                    float age = Mathf.Min(seconds, f / (float)BenchRate);
                    bool keep = !stills || kept.Any(k => Mathf.RoundToInt(k * BenchRate) == f);
                    bool loaded = age >= loadedFrom && age < hitAt;
                    if (!keep) continue;
                    for (int view = 0; view < 3; view++)
                    {
                        Vector3 eye, look; float fov;
                        if (view == 0)
                        {
                            // The bench's own three places (close on his hand, back for the throw, close on the hit), from
                            // his right. The far one stands 6 m off and looks wider, where the bench's stands 11 m off:
                            // on this court that put a lamp post in front of the lens.
                            Vector3 handAt = stage.transform.TransformPoint(new Vector3(0.30f, 0.62f, -0.44f)), hitAt3 = stage.transform.TransformPoint(hit);
                            Vector3 l0 = handAt + forward * 0.1f + Vector3.down * 0.20f, e0 = l0 + forward * 1.25f + Vector3.up * 0.75f + right * 2.35f;
                            Vector3 l1 = (handAt + hitAt3) * 0.5f, e1 = l1 + right * 6.2f + Vector3.up * 1.7f + forward * 0.4f;
                            Vector3 l2 = hitAt3 - forward * 0.55f + Vector3.down * 0.30f, e2 = l2 - forward * 0.9f + Vector3.up * 1.05f + right * 4.0f;
                            float back = FxStudyKit.Ease(FxStudyKit.Run(age, 1.85f, 2.40f)), near = FxStudyKit.Ease(FxStudyKit.Run(age, 3.05f, 3.65f));
                            look = Vector3.Lerp(Vector3.Lerp(l0, l1, back), l2, near);
                            eye = Vector3.Lerp(Vector3.Lerp(e0, e1, back), e2, near);
                            fov = Mathf.Lerp(Mathf.Lerp(34f, 52f, back), 34f, near);
                        }
                        else if (view == 1) { eye = stand + Vector3.up * DanteEye + forward * 0.12f; look = stand + forward * DanteTargetAhead + Vector3.up * 0.75f; fov = 70f; }
                        else { eye = targetAt + Vector3.up * DanteEye - forward * 0.15f; look = stand + Vector3.up * 0.8f; fov = 70f; }
                        cam.fieldOfView = fov;
                        cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                        // Nobody sees their own body from behind their own eyes.
                        if (danteModel != null) danteModel.SetActive(view != 1);
                        target.SetActive(view != 2);

                        // The shoe is where this view's copy of the effect has it.
                        var playing = view == 1 ? fxEye : fx;
                        playing.ShoePose(age, out Vector3 shoeAt, out Quaternion shoeTurn);
                        shoe.transform.SetPositionAndRotation(stage.transform.TransformPoint(shoeAt), stage.transform.rotation * shoeTurn);
                        // OLD: the inlay on the shoe while it is loaded, and nothing else.
                        fx.Root.gameObject.SetActive(false); fxEye.Root.gameObject.SetActive(false);
                        foreach (var inlay in inlays) inlay.SetActive(loaded);
                        Shoot(false, 0, view);
                        // NEW: the stone, plain and through the bench's bloom.
                        foreach (var inlay in inlays) inlay.SetActive(false);
                        playing.Root.gameObject.SetActive(true);
                        playing.Sample(age, eye);
                        Shoot(false, 1, view);
                        Shoot(true, 2, view);
                    }
                    // ⚠️ WHAT THE NEW LOOK COSTS, MEASURED at the worst moment (a tenth of a second after the hit, when the
                    // rocks, the rubble and the dust are all out), from the target's eyes, which see all of it:
                    //   * every renderer that is on, times the passes its material draws, is the draw calls; nothing batches;
                    //   * posing it (`Sample`) is timed over 300 calls, with what it allocates;
                    //   * drawing it is the camera's render timed 30 times with it on and 30 with it off. This is the EDITOR,
                    //     outside Play, at this film's small picture: it says what posing costs and roughly what drawing
                    //     adds, not what a phone will do.
                    if (cost == null && age >= hitAt + 0.10f)
                    {
                        fxEye.Root.gameObject.SetActive(false); fx.Root.gameObject.SetActive(true);
                        foreach (var inlay in inlays) inlay.SetActive(false);
                        Vector3 from = cam.transform.position;
                        fx.Sample(age, from);
                        int calls = 0, triangles = 0, renderers = 0;
                        foreach (var renderer in fx.Root.GetComponentsInChildren<MeshRenderer>(false))
                        {
                            if (!renderer.enabled || renderer.sharedMaterial == null) continue;
                            var filter = renderer.GetComponent<MeshFilter>();
                            if (filter == null || filter.sharedMesh == null) continue;
                            int passes = Mathf.Max(1, renderer.sharedMaterial.passCount), own = 0;
                            for (int s = 0; s < filter.sharedMesh.subMeshCount; s++) own += (int)(filter.sharedMesh.GetIndexCount(s) / 3);
                            renderers++; calls += passes; triangles += own * passes;
                        }
                        var watch = new System.Diagnostics.Stopwatch();
                        long before = GC.GetAllocatedBytesForCurrentThread();
                        watch.Start();
                        for (int n = 0; n < 300; n++) fx.Sample(age, from);
                        watch.Stop();
                        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                        double pose = watch.Elapsed.TotalMilliseconds / 300.0;
                        cam.allowHDR = false; cam.targetTexture = plainTarget;
                        double Draw(bool with)
                        {
                            fx.Root.gameObject.SetActive(with);
                            cam.Render();
                            var clock = System.Diagnostics.Stopwatch.StartNew();
                            for (int n = 0; n < 30; n++) cam.Render();
                            GL.Flush();
                            return clock.Elapsed.TotalMilliseconds / 30.0;
                        }
                        double without = Draw(false), withIt = Draw(true);
                        // AND THE HIT AS A MATCH PLAYS IT: a copy asked for the hit alone (`DanteBoulderMoment.Hit`), which
                        // makes only what a hit draws. Counted and timed the same way, at the same moment.
                        fx.Root.gameObject.SetActive(false);
                        var watchBuild = System.Diagnostics.Stopwatch.StartNew();
                        var only = new FxStudyDanteBoulder(stage.transform, new Vector3(0.30f, 0.62f, -0.44f), hit, false, fit, DanteCourtLight, FxStudyDanteBoulder.PartHit);
                        double built = watchBuild.Elapsed.TotalMilliseconds;
                        only.Sample(age, from);
                        int oCalls = 0, oTriangles = 0, oRenderers = 0;
                        foreach (var renderer in only.Root.GetComponentsInChildren<MeshRenderer>(false))
                        {
                            if (!renderer.enabled || renderer.sharedMaterial == null) continue;
                            var filter = renderer.GetComponent<MeshFilter>();
                            if (filter == null || filter.sharedMesh == null) continue;
                            int passes = Mathf.Max(1, renderer.sharedMaterial.passCount), own = 0;
                            for (int s = 0; s < filter.sharedMesh.subMeshCount; s++) own += (int)(filter.sharedMesh.GetIndexCount(s) / 3);
                            oRenderers++; oCalls += passes; oTriangles += own * passes;
                        }
                        long oBefore = GC.GetAllocatedBytesForCurrentThread();
                        var oWatch = System.Diagnostics.Stopwatch.StartNew();
                        for (int n = 0; n < 300; n++) only.Sample(age, from);
                        double oPose = oWatch.Elapsed.TotalMilliseconds / 300.0;
                        long oAllocated = GC.GetAllocatedBytesForCurrentThread() - oBefore;
                        cam.Render();
                        var oClock = System.Diagnostics.Stopwatch.StartNew();
                        for (int n = 0; n < 30; n++) cam.Render();
                        double oDraw = oClock.Elapsed.TotalMilliseconds / 30.0;
                        only.Dispose();
                        fx.Root.gameObject.SetActive(true);
                        string match = "  THE HIT AS A MATCH PLAYS IT (hit part only): " + oRenderers + " renderers, " + oCalls + " draw calls, " + oTriangles + " triangles drawn; made in " + built.ToString("0.00") + " ms; posing "
                                     + oPose.ToString("0.0000") + " ms a frame, " + (oAllocated / 300.0).ToString("0") + " bytes a frame; scene " + oDraw.ToString("0.000") + " ms with it\n";
                        cost = "new look " + (age - hitAt).ToString("0.00") + " s after the hit: " + renderers + " renderers on, " + calls + " draw calls, " + triangles + " triangles drawn\n"
                             + "  posing it: " + pose.ToString("0.0000") + " ms a frame, " + (allocated / 300.0).ToString("0") + " bytes allocated a frame (300 calls)\n"
                             + "  drawing the scene from the target's eyes: " + without.ToString("0.000") + " ms without it, " + withIt.ToString("0.000") + " ms with it (editor, outside Play, " + DanteCellWidth + "x" + DanteCellHeight + ", time to SUBMIT, not GPU time)\n"
                             + match
                             + "  the old look is one more draw of the shoe's own mesh while it is loaded";
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine(count + " frames at " + BenchRate + " a second (" + seconds.ToString("0.00") + " s), in " + frames.Replace('\\', '/'));
                report.AppendLine("each frame: rows are third person, his own eyes, the target's eyes; columns are OLD (the inlay), NEW, NEW with the bench bloom");
                report.AppendLine("of its laid-on light (the split's glow, the two blinks) " + DanteCourtLight.ToString("0.00") + " is kept; the bench keeps all of it");
                report.AppendLine(cost ?? "cost not counted");
                report.AppendLine(lit ? "lit by the map's own lighting settings" : "the map's lighting settings could not be put in force: lit by the open scene's ambient");
                report.AppendLine("STAGED: nothing here is the game's cast, throw or hit; his body is a still pose; his own eyes have no first-person arms");
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[DanteBoulderFilm] " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, report + "\n" + failure);
                Debug.LogError("[DanteBoulderFilm] " + failure);
            }
            finally
            {
                try { fx?.Dispose(); fxEye?.Dispose(); } catch (Exception) { }
                FxStudyDanteBoulder.KeyLight = benchKey;
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
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
