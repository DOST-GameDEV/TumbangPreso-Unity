using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Records the real map through a HOME film's camera, frame by frame, as the plates the film's
    /// characters and effects are composited over (ArtSource/home-scene/README.md).
    ///
    /// It looks through the same MapPreviewSurface as the recorded map previews, so the plates have the
    /// map's lighting, grade, ink outline and running life (traffic, signals, pigeons), and only the
    /// camera differs. Run through tools/run_unity_guarded.py; ArtSource/home-scene/scripts/plates.mjs
    /// writes the camera file and starts it.
    /// </summary>
    [InitializeOnLoad]
    public static class HomeFilmPlateRecorder
    {
        const string CamKey = "HomeFilmPlateRecorder.Cam", OutKey = "HomeFilmPlateRecorder.Out";

        /// <summary>One picture. `dt` is how far the street then runs; 0 takes another picture of the same moment.</summary>
        [Serializable] class Shot { public string clip; public int i; public bool skip; public float x, y, z, yaw, pitch, roll, f, dt; public Vector3[] subjects; public float[] a, b, kuro, can, moths; }
        /// <summary>One person in the film: who, where their eye paint is, and what colour it lights. No atlas means no glow.</summary>
        [Serializable] class Member { public string id; public bool slipper; public string atlas; public float eyeLow, eyeHigh; public Color32 glow; }
        /// <summary>`floor` is the court's surface in this map. `green` winds the signals and `train` the LRT, in seconds after the first plate; negative leaves them alone.</summary>
        [Serializable] class Track { public string map; public float floor; public Member[] cast; public bool kuro; public string[] bones; public int warm; public float green; public float train = -1f; public int trainDir = 1; public Shot[] frames; }

        static HomeFilmPlateRecorder()
        {
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
        }

        public static void Record()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch only: see ArtSource/home-scene/scripts/plates.mjs.");
            SessionState.SetString(CamKey, Argument("-tp-plate-cam") ?? throw new ArgumentException("-tp-plate-cam"));
            SessionState.SetString(OutKey, Argument("-tp-plate-out") ?? throw new ArgumentException("-tp-plate-out"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }

        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || string.IsNullOrEmpty(SessionState.GetString(CamKey, ""))) return;
            var go = new GameObject("~HomeFilmPlates");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<MapPreviewRecordingHost>().StartCoroutine(Guard(Run()));
        }

        static IEnumerator Guard(IEnumerator work)
        {
            while (true)
            {
                bool moved;
                try { moved = work.MoveNext(); }
                catch (Exception error) { Debug.LogException(error); SessionState.EraseString(CamKey); EditorApplication.Exit(1); yield break; }
                if (!moved) break;
                yield return work.Current;
            }
            SessionState.EraseString(CamKey);
            Debug.Log("[HomeFilmPlates] done");
            EditorApplication.Exit(0);
        }

        static IEnumerator Run()
        {
            var track = JsonUtility.FromJson<Track>(File.ReadAllText(SessionState.GetString(CamKey, "")));
            string output = SessionState.GetString(OutKey, "");
            foreach (var shot in track.frames) Directory.CreateDirectory(Path.Combine(output, shot.clip));
            GameServices.Ensure();

            var root = new GameObject("Plates " + track.map, typeof(RectTransform), typeof(RawImage));
            var view = root.AddComponent<MapPreviewSurface>();
            view.PreferRecordedPreview = false;
            view.Show(track.map);
            while (view.Showing != track.map || view.Camera == null) yield return null;
            yield return null; yield return null; // The world look lifts the ground in its first Update (MAP_PREVIEW_RECORDING.md).

            const int width = 1920, height = 1080;
            var camera = view.Camera;
            var source = camera.targetTexture;
            var descriptor = source.descriptor; descriptor.width = width; descriptor.height = height;
            var target = new RenderTexture(descriptor) { name = source.name, filterMode = source.filterMode };
            target.Create();
            typeof(MapPreviewSurface).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, target);
            camera.targetTexture = target;
            root.GetComponent<RawImage>().texture = target;
            source.Release(); Object.Destroy(source);
            camera.aspect = 16f / 9f;
            camera.nearClipPlane = 0.1f;
            camera.enabled = false;
            view.enabled = false; // Its Update would put the sway back on the camera.

            var traffic = Object.FindFirstObjectByType<KantoTraffic>();
            var flyby = Object.FindFirstObjectByType<LrtTrainFlyby>();
            if (flyby != null)
                foreach (var r in flyby.GetComponentsInChildren<Renderer>(true))
                    Debug.Log("[HomeFilmPlates] train part " + r.name + " on=" + r.enabled + "/" + r.gameObject.activeInHierarchy + " layer=" + r.gameObject.layer + " at " + r.bounds.center.ToString("F1"));
            foreach (var probe in new[] { new Vector3(0f, 30f, 0f), new Vector3(4f, 30f, -6f), new Vector3(-4f, 30f, 5f) })
                Debug.Log("[HomeFilmPlates] floor under " + probe.ToString("F1") + " = " + (Physics.Raycast(probe, Vector3.down, out var ground, 60f) ? ground.point.y.ToString("F3") + " on " + ground.collider.name : "nothing"));
            if (traffic != null)
                foreach (var signal in traffic.Signals)
                    if (signal != null) Debug.Log("[HomeFilmPlates] signal " + signal.name + " at " + signal.bounds.center.ToString("F2") + " faces " + signal.transform.forward.ToString("F2"));
            var scenery = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.gameObject.scene.name == track.map && r.bounds.size.magnitude < MaxSpan) scenery.Add(r);
            var hidden = new List<(Renderer r, ShadowCastingMode was)>();
            Debug.Log("[HomeFilmPlates] " + track.map + " traffic=" + (traffic != null ? traffic.Drivers.Length.ToString() : "none") + " look weight=" + TumbangPreso.Visual.WorldCueProfile.LightingWeight);

            // ⚠️ THE MAP'S FLOCK WANDERS ON TO THE COURT AND THE LENS (owner, 2026-11-02: "dont put them there"). Not in the film.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == "Pigeons" && t.gameObject.scene.name == track.map) t.gameObject.SetActive(false);

            var first = Figure.Build(track.cast[0], track.bones, track.floor);
            var second = Figure.Build(track.cast[1], track.bones, track.floor);
            var kuro = track.kuro ? Pet.Build() : null;
            var moths = new Moths();
            var can = BuildCan();

            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            for (int i = 0; i < track.warm; i++) yield return null; // Let the street spread out and the flock settle.

            // The film's light turns green on its own beat, so the street's cycle is set to agree with it.
            if (traffic != null && track.green >= 0f) SetClockForGreen(traffic, track.green);
            if (flyby != null && track.train >= 0f) SetClockForTrain(flyby, track.train, track.trainDir);

            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var display = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            bool srgb = GL.sRGBWrite;
            var clock = Stopwatch.StartNew();
            try
            {
                for (int n = 0; n < track.frames.Length; n++)
                {
                    var s = track.frames[n];
                    Time.captureDeltaTime = s.dt > 0f ? s.dt : 1f / 60f;
                    // Only the street's clock runs: `plates.mjs <frame...>` still lands its picks on the right traffic and train.
                    if (n % 120 == 0)
                        Debug.Log("[HomeFilmPlates] clock " + n + " road=" + (traffic != null && traffic.DriverCount > 0 ? traffic.DriverPosition(0).ToString("F1") : "none") + " train=" + (flyby != null ? flyby.transform.position.ToString("F1") : "none"));
                    if (s.skip) { yield return null; continue; }
                    camera.transform.SetPositionAndRotation(new Vector3(s.x, s.y, s.z), Quaternion.Euler(s.pitch, s.yaw, s.roll));
                    camera.fieldOfView = 2f * Mathf.Atan(540f / s.f) * Mathf.Rad2Deg;
                    first.Apply(s.a); second.Apply(s.b); PlaceCan(can, s.can);
                    kuro?.Apply(s.kuro);
                    moths.Apply(s.moths);
                    Clear(scenery, hidden, camera.transform.position, s.subjects);
                    camera.Render();
                    GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    Graphics.Blit(target, display);
                    RenderTexture.active = display;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(output, s.clip, s.i.ToString("D5") + ".jpg"), image.EncodeToJPG(96));
                    RenderTexture.active = previous;
                    GL.sRGBWrite = srgb;
                    if (n % 120 == 0) Debug.Log("[HomeFilmPlates] " + n + "/" + track.frames.Length + " " + clock.Elapsed.TotalSeconds.ToString("F0") + "s");
                    if (s.dt > 0f) yield return null;
                }
            }
            finally
            {
                Time.captureDeltaTime = capture;
                RenderTexture.active = previous;
                GL.sRGBWrite = srgb;
                Object.Destroy(image);
                RenderTexture.ReleaseTemporary(display);
            }
        }

        const float PersonScale = 2.38f;
        /// <summary>The can's scale, from the film: 0.62 m tall.</summary>
        const float CanScale = 0.62f / 0.383f;

        static Vector3 Mirror(float x, float y, float z) => new Vector3(-x, y, z);
        static Quaternion Mirror(float x, float y, float z, float w) => new Quaternion(x, -y, -z, w);

        static void Dress(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = MapPreviewSurface.PreviewLayer;
            foreach (var a in go.GetComponentsInChildren<Animator>(true)) a.enabled = false;
        }

        static Transform BuildCan()
        {
            var entry = RosterBook.Load().Cans.Find(e => e != null && e.Id == "metal");
            var model = entry != null ? entry.Model : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/lata_metal.obj");
            var root = new GameObject("FilmCan").transform;
            var body = Object.Instantiate(model, root);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            TumbangPreso.Visual.ToonSkin.Apply(body, TumbangPreso.Visual.ToonSkin.PropOutlineWidth);
            Dress(root.gameObject);
            root.localScale = Vector3.one * CanScale;
            return root;
        }

        static void PlaceCan(Transform can, float[] c)
        {
            can.position = new Vector3(c[0], c[1], c[2]);
            can.rotation = Quaternion.Euler(0f, 180f, 0f) * Mirror(c[3], c[4], c[5], c[6]);
            can.position += Vector3.up * c[7] * CanScale;
        }

        /// <summary>The game's own model on the game's own skin, as a spawn gets it (LobbyCast.Build), wearing one row of the film's acting.</summary>
        sealed class Figure
        {
            Transform _body, _slipper;
            Transform[] _bones;
            float _lift;
            Renderer _head;
            Texture2D[] _lit;

            public static Figure Build(Member who, string[] names, float floor)
            {
                string id = who.id;
                var art = RosterBook.Load().FindPersonArt(id);
                var body = Object.Instantiate(art.Model);
                body.name = "Film " + id;
                body.transform.localScale = Vector3.one * PersonScale;
                TumbangPreso.Visual.ToonSkin.Apply(body, TumbangPreso.Visual.ToonSkin.PersonOutlineWidth, art.Palette);
                Dress(body);
                var f = new Figure { _body = body.transform, _bones = new Transform[names.Length] };
                var all = body.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < names.Length; i++) f._bones[i] = Array.Find(all, t => t.name == names[i]);
                // Soles on the court: the same measure as LobbyCast.MeasureFootLift, from his own idle.
                var idle = art.Clips != null ? Array.Find(art.Clips, c => c != null && c.name.EndsWith("idle")) : null;
                if (idle != null) idle.SampleAnimation(body, 0f);
                var bounds = new Bounds(body.transform.position, Vector3.zero);
                foreach (var r in body.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                f._lift = floor - (bounds.min.y - body.transform.position.y);
                Debug.Log("[HomeFilmPlates] " + id + " lift=" + f._lift + " missing bones=" + Array.FindAll(f._bones, b => b == null).Length);

                if (who.slipper)
                {
                    var slipper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/tsinelas_tsinelas.glb"));
                    TumbangPreso.Visual.ToonSkin.ApplySlipper(slipper, TumbangPreso.Visual.ToonSkin.PropOutlineWidth);
                    Dress(slipper);
                    f._slipper = slipper.transform;
                }
                if (!string.IsNullOrEmpty(who.atlas)) f.BuildEyes(who);
                return f;
            }

            /// <summary>
            /// Eyes only glow, and never change shape: copies of the atlas with the eye paint itself recoloured, by the film's
            /// own rule (the dark paint on the face plane, inside the band read off the glb).
            /// </summary>
            void BuildEyes(Member who)
            {
                foreach (var r in _body.GetComponentsInChildren<Renderer>(true))
                {
                    var filter = r.GetComponent<MeshFilter>();
                    var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : filter != null ? filter.sharedMesh : null;
                    if (mesh == null || !mesh.isReadable || !r.name.Contains("head")) continue;
                    var png = File.ReadAllBytes(who.atlas);
                    var rest = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    rest.LoadImage(png);
                    var pixels = rest.GetPixels32();
                    bool[] eye = new bool[pixels.Length];
                    var v = mesh.vertices; var uv = mesh.uv; var tri = mesh.triangles;
                    // A rigid head mesh is in the head bone's space, a skinned one in the model's.
                    float up = r is SkinnedMeshRenderer ? 0f : (_body.InverseTransformPoint(r.transform.position).y);
                    int marked = 0;
                    for (int t = 0; t < tri.Length; t += 3)
                    {
                        Vector3 a = v[tri[t]], b = v[tri[t + 1]], c = v[tri[t + 2]];
                        float low = Mathf.Min(a.y, b.y, c.y) + up, high = Mathf.Max(a.y, b.y, c.y) + up;
                        if (high < who.eyeLow || low > who.eyeHigh || Mathf.Min(a.z, b.z, c.z) < 0.1f) continue;
                        Vector2 p = uv[tri[t]], q = uv[tri[t + 1]], w = uv[tri[t + 2]];
                        int x0 = Mathf.FloorToInt(Mathf.Min(p.x, q.x, w.x) * rest.width), x1 = Mathf.CeilToInt(Mathf.Max(p.x, q.x, w.x) * rest.width);
                        int y0 = Mathf.FloorToInt(Mathf.Min(p.y, q.y, w.y) * rest.height), y1 = Mathf.CeilToInt(Mathf.Max(p.y, q.y, w.y) * rest.height);
                        for (int y = Mathf.Max(0, y0); y < Mathf.Min(rest.height, y1); y++)
                            for (int x = Mathf.Max(0, x0); x < Mathf.Min(rest.width, x1); x++)
                            {
                                var px = pixels[y * rest.width + x];
                                if (Mathf.Max(px.r, px.g, px.b) >= 56 || eye[y * rest.width + x]) continue;
                                eye[y * rest.width + x] = true; marked++;
                            }
                    }
                    Debug.Log("[HomeFilmPlates] eye texels=" + marked + " on " + r.name);
                    _head = r;
                    _lit = new Texture2D[6];
                    var glow = who.glow;
                    for (int n = 0; n < 6; n++)
                    {
                        var copy = (Color32[])pixels.Clone();
                        for (int i = 0; i < copy.Length; i++) if (eye[i]) copy[i] = Color32.Lerp(pixels[i], glow, n / 5f);
                        var tex = new Texture2D(rest.width, rest.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                        tex.SetPixels32(copy); tex.Apply(); _lit[n] = tex;
                    }
                    return;
                }
                Debug.LogWarning("[HomeFilmPlates] no readable head mesh for the eyes");
            }

            // A row: x z yaw glow, slipper state, position 3, rotation 4, scale, then each bone's position 3 and rotation 4.
            public void Apply(float[] d)
            {
                _body.SetPositionAndRotation(new Vector3(d[0], _lift, d[1]), Quaternion.Euler(0f, 180f - d[2], 0f));
                for (int i = 0; i < _bones.Length; i++)
                {
                    if (_bones[i] == null) continue;
                    int o = 13 + i * 7;
                    _bones[i].localPosition = Mirror(d[o], d[o + 1], d[o + 2]);
                    _bones[i].localRotation = Mirror(d[o + 3], d[o + 4], d[o + 5], d[o + 6]);
                }
                if (_head != null)
                {
                    var block = new MaterialPropertyBlock();
                    _head.GetPropertyBlock(block);
                    block.SetTexture("_MainTex", _lit[Mathf.Clamp(Mathf.RoundToInt(d[3] * 5f), 0, 5)]);
                    _head.SetPropertyBlock(block);
                }
                if (_slipper == null) return;
                int state = Mathf.RoundToInt(d[4]);
                _slipper.gameObject.SetActive(state != 0);
                if (state == 0) return;
                var q = Mirror(d[8], d[9], d[10], d[11]);
                if (state == 2)
                {
                    _slipper.SetPositionAndRotation(new Vector3(d[5], d[6], d[7]), Quaternion.Euler(0f, 180f, 0f) * q);
                }
                else
                {
                    _slipper.SetPositionAndRotation(_body.TransformPoint(Mirror(d[5], d[6], d[7])), _body.rotation * q);
                }
                _slipper.localScale = Vector3.one * d[12] * PersonScale;
            }
        }

        /// <summary>
        /// Kuro, the game's own model in the dress `GhostPetCompanion.ApplyAppearance` gives him, wearing his own authored faces.
        /// A row: x y z yaw pitch roll scale, then a face from `Faces`. Nothing of his behaviour runs: the film acts him.
        /// </summary>
        sealed class Pet
        {
            /// <summary>`GhostPetCompanion.ExpressionFor`: rest, CuriousPeek, HappyHop, GoofyDizzy, ShyPout.</summary>
            static readonly (string[] show, bool eyeL, bool eyeR, bool mouth)[] Faces =
            {
                (new string[0], false, false, false),
                (new[] { "KuroSparkleL", "KuroSparkleR", "KuroOhMouth" }, false, false, true),
                (new[] { "KuroHappyEyeL", "KuroHappyEyeR", "KuroGrinMouth" }, true, true, true),
                (new[] { "KuroCrossLeft", "KuroCrossRight", "KuroGoofyMouth" }, true, true, true),
                (new[] { "KuroShyEye", "KuroPoutMouth" }, false, true, true),
            };

            Transform _root, _expressions, _eyeL, _eyeR, _mouth;
            Vector3 _restEyeL, _restEyeR, _restMouth;
            readonly Dictionary<string, Transform> _parts = new Dictionary<string, Transform>();
            readonly Dictionary<Transform, Vector3> _rest = new Dictionary<Transform, Vector3>();

            static Transform Named(Transform root, string name) => Array.Find(root.GetComponentsInChildren<Transform>(true), t => t.name == name);

            public static Pet Build()
            {
                var art = RosterBook.Load().FindPersonArt("nemu");
                var body = Object.Instantiate(art.PetModel);
                body.name = "Film Kuro";
                foreach (var b in body.GetComponentsInChildren<Behaviour>(true)) b.enabled = false;
                TumbangPreso.Visual.GhostPetCompanion.ApplyAppearance(body, art.Palette);
                var rage = Named(body.transform, "RageForm");
                if (rage != null) rage.gameObject.SetActive(false);
                var restored = Named(body.transform, "RestoredCalm");
                var expressions = Named(body.transform, "KuroExpressions");
                foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                    if (!(restored != null && r.transform.IsChildOf(restored)) && !(expressions != null && r.transform.IsChildOf(expressions))) r.enabled = false;
                var p = new Pet { _root = body.transform, _expressions = expressions };
                if (expressions != null)
                {
                    foreach (var r in expressions.GetComponentsInChildren<Renderer>(true))
                    {
                        TumbangPreso.Visual.ToonSkin.Apply(r, 0, null);
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        r.receiveShadows = false;
                    }
                    foreach (var t in expressions.GetComponentsInChildren<Transform>(true))
                        if (t != expressions) { p._parts[t.name] = t; p._rest[t] = t.localScale; }
                }
                foreach (var t in restored != null ? restored.GetComponentsInChildren<Transform>(true) : new Transform[0])
                {
                    if (t.name == "ghost-eye-l") { p._eyeL = t; p._restEyeL = t.localScale; }
                    else if (t.name == "ghost-eye-r") { p._eyeR = t; p._restEyeR = t.localScale; }
                    else if (t.name == "ghost-mouth-dot") { p._mouth = t; p._restMouth = t.localScale; }
                }
                Dress(body);
                Debug.Log("[HomeFilmPlates] kuro parts=" + p._parts.Count + " eyes=" + (p._eyeL != null) + "," + (p._eyeR != null) + " mouth=" + (p._mouth != null));
                return p;
            }

            public void Apply(float[] d)
            {
                _root.SetPositionAndRotation(new Vector3(d[0], d[1], d[2]), Quaternion.Euler(d[4], 180f - d[3], d[5]));
                _root.localScale = Vector3.one * d[6];
                var face = Faces[Mathf.Clamp(Mathf.RoundToInt(d[7]), 0, Faces.Length - 1)];
                if (_expressions != null)
                {
                    _expressions.localScale = face.show.Length > 0 ? Vector3.one : Vector3.zero;
                    foreach (var kv in _rest) kv.Key.localScale = Array.IndexOf(face.show, kv.Key.name) >= 0 ? kv.Value : Vector3.zero;
                }
                if (_eyeL != null) _eyeL.localScale = face.eyeL ? Vector3.zero : _restEyeL;
                if (_eyeR != null) _eyeR.localScale = face.eyeR ? Vector3.zero : _restEyeR;
                if (_mouth != null) _mouth.localScale = face.mouth ? Vector3.zero : _restMouth;
            }
        }

        /// <summary>
        /// Her Teleport's swarm: the game's own moth (`PhaisterSwarm`), each placed by the film so it is deterministic and passes
        /// properly behind and in front of the cast. A row is seven numbers a moth: x y z yaw pitch wing scale.
        /// </summary>
        sealed class Moths
        {
            readonly List<(Transform body, Transform l, Transform r, Quaternion restL, Quaternion restR)> _pool = new List<(Transform, Transform, Transform, Quaternion, Quaternion)>();

            public void Apply(float[] d)
            {
                int n = d == null ? 0 : d.Length / 7;
                while (_pool.Count < n)
                {
                    var prop = TumbangPreso.Visual.PhaisterProp.Spawn("moth", null, null, TumbangPreso.Visual.PhaisterProp.InsectOutlineWidth);
                    Dress(prop);
                    Transform l = TumbangPreso.Visual.PhaisterProp.Find(prop, "wing-l"), r = TumbangPreso.Visual.PhaisterProp.Find(prop, "wing-r");
                    _pool.Add((prop.transform, l, r, l != null ? l.localRotation : Quaternion.identity, r != null ? r.localRotation : Quaternion.identity));
                }
                for (int i = 0; i < _pool.Count; i++)
                {
                    var m = _pool[i];
                    m.body.gameObject.SetActive(i < n);
                    if (i >= n) continue;
                    int o = i * 7;
                    m.body.SetPositionAndRotation(new Vector3(d[o], d[o + 1], d[o + 2]), Quaternion.Euler(d[o + 4], d[o + 3], 0f));
                    m.body.localScale = Vector3.one * d[o + 6];
                    if (m.l != null) m.l.localRotation = m.restL * Quaternion.Euler(0f, 0f, d[o + 5]);
                    if (m.r != null) m.r.localRotation = m.restR * Quaternion.Euler(0f, 0f, -d[o + 5]);
                }
            }
        }

        /// <summary>Bigger than this is the street itself (MapPreviewSurface.MaxOccluderSpan).</summary>
        const float MaxSpan = 14f;
        /// <summary>A prop this close to the lens is a blur across the frame, not a part of the shot.</summary>
        const float LensClearance = 2.2f;

        /// <summary>
        /// The cast is composited over the plate, so nothing in the map may stand in front of them. Whatever
        /// would is drawn for its shadow only, for just the frames it is in the way.
        /// </summary>
        static void Clear(List<Renderer> scenery, List<(Renderer r, ShadowCastingMode was)> hidden, Vector3 eye, Vector3[] subjects)
        {
            foreach (var h in hidden) if (h.r != null) h.r.shadowCastingMode = h.was;
            hidden.Clear();
            foreach (var r in scenery)
            {
                if (r == null || !r.enabled) continue;
                var b = r.bounds;
                bool away = (b.ClosestPoint(eye) - eye).sqrMagnitude > LensClearance * LensClearance;
                if (away && (subjects == null || !Blocks(eye, b, subjects))) continue;
                hidden.Add((r, r.shadowCastingMode));
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        static bool Blocks(Vector3 eye, Bounds b, Vector3[] subjects)
        {
            foreach (var at in subjects)
            {
                Vector3 to = at - eye;
                float far = to.magnitude;
                if (far < 0.5f) continue;
                if (b.IntersectRay(new Ray(eye, to / far), out float hit) && hit < far - 0.6f) return true;
            }
            return false;
        }

        /// <summary>Winds the signals so the Z road's green opens this many seconds after the first plate.</summary>
        static void SetClockForGreen(KantoTraffic traffic, float seconds)
        {
            float cycle = 2f * (traffic.GreenSeconds + traffic.AmberSeconds + traffic.AllRedSeconds);
            float zGreenStarts = cycle / 2f;
            typeof(KantoTraffic).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(traffic, Mathf.Repeat(zGreenStarts - seconds, cycle));
        }

        /// <summary>
        /// Winds the LRT so the consist is level with the court this many seconds after the first plate. The run is the flyby's
        /// own arithmetic, stepped here at the plates' rate: it gathers speed out of one station, crosses, and brakes into the other.
        /// `dir` picks the track: from the lot, the deck's parapet hides nearly all of the far one (+1), so -1 is the side to shoot.
        /// </summary>
        static void SetClockForTrain(LrtTrainFlyby flyby, float seconds, int dir)
        {
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(LrtTrainFlyby);
            type.GetField("_dir", Own).SetValue(flyby, dir);
            float level = -dir * flyby.CentreZ;
            float along = flyby.StartZ, run = 0f;
            const float Step = 1f / 60f;
            while (along < level && along < flyby.EndZ)
            {
                float ends = Mathf.Min(along - flyby.StartZ, flyby.EndZ - along);
                along += flyby.Speed * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(ends / Mathf.Max(1f, flyby.StationEase))) * Step;
                run += Step;
            }
            Debug.Log("[HomeFilmPlates] train run to the court " + run.ToString("F2") + " s, wanted level at " + seconds.ToString("F2") + " s");
            if (seconds < run) throw new InvalidOperationException("The train needs " + run + " s from the station to the court: start it earlier in the warm-up.");
            type.GetField("_isRunning", Own).SetValue(flyby, false);
            type.GetField("_timer", Own).SetValue(flyby, flyby.Interval - (seconds - run));
            type.GetMethod("StopRumble", Own).Invoke(flyby, null);
            type.GetMethod("Place", Own).Invoke(flyby, new object[] { flyby.StartZ });
        }
    }
}
