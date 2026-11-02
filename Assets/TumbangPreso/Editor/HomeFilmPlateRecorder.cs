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
        [Serializable] class Shot { public string clip; public int i; public float x, y, z, yaw, pitch, roll, f, dt; public Vector3[] subjects; public float[] zack, rago, can; }
        [Serializable] class Track { public string map; public string[] bones; public int warm; public float green; public Shot[] frames; }

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

            var zack = Figure.Build("zack", track.bones);
            var rago = Figure.Build("sean", track.bones);
            var can = BuildCan();

            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            for (int i = 0; i < track.warm; i++) yield return null; // Let the street spread out and the flock settle.

            // The film's light turns green on its own beat, so the street's cycle is set to agree with it.
            if (traffic != null && track.green >= 0f) SetClockForGreen(traffic, track.green);

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
                    camera.transform.SetPositionAndRotation(new Vector3(s.x, s.y, s.z), Quaternion.Euler(s.pitch, s.yaw, s.roll));
                    camera.fieldOfView = 2f * Mathf.Atan(540f / s.f) * Mathf.Rad2Deg;
                    zack.Apply(s.zack); rago.Apply(s.rago); PlaceCan(can, s.can);
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
        /// <summary>The court's surface in Kanto (kanto_layout.json, gameplay.walk).</summary>
        const float Floor = 0.12f;
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

            public static Figure Build(string id, string[] names)
            {
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
                f._lift = Floor - (bounds.min.y - body.transform.position.y);
                Debug.Log("[HomeFilmPlates] " + id + " lift=" + f._lift + " missing bones=" + Array.FindAll(f._bones, b => b == null).Length);

                if (id == "zack")
                {
                    var slipper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/tsinelas_tsinelas.glb"));
                    TumbangPreso.Visual.ToonSkin.ApplySlipper(slipper, TumbangPreso.Visual.ToonSkin.PropOutlineWidth);
                    Dress(slipper);
                    f._slipper = slipper.transform;
                    f.BuildEyes(all);
                }
                return f;
            }

            /// <summary>
            /// Eyes only glow, and never change shape: copies of his atlas with the eye paint itself recoloured, by the film's
            /// own rule (the dark paint on the face plane, inside the band read off the glb, y 0.441 to 0.474).
            /// </summary>
            void BuildEyes(Transform[] all)
            {
                foreach (var r in _body.GetComponentsInChildren<Renderer>(true))
                {
                    var filter = r.GetComponent<MeshFilter>();
                    var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : filter != null ? filter.sharedMesh : null;
                    if (mesh == null || !mesh.isReadable || !r.name.Contains("head")) continue;
                    var png = File.ReadAllBytes("Assets/TumbangPreso/Art/CharacterRedesign/zack/zack-redesign-atlas.png");
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
                        if (high < 0.441f || low > 0.474f || Mathf.Min(a.z, b.z, c.z) < 0.1f) continue;
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
                    var glow = new Color32(255, 250, 150, 255);
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
    }
}
