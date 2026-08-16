using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The live map render behind the setup screen, ported from `map_preview.gd` and
    /// `MatchSetup.tscn`'s SubViewport.
    ///
    /// ⚠️⚠️ THIS IS NOT A SCREENSHOT AND IT WAS NOT OPTIONAL. Godot puts a `SubViewport` with
    /// `own_world_3d` behind the setup panels and renders the chosen arena into it live, which
    /// is why picking a map on that screen shows you the map.
    ///
    /// ⚠️⚠️ THE FRAMING COMES FROM `GameLaunch.MAPS[..].preview`, NOT FROM THE .tscn's CAMERA
    /// NODE, AND COPYING THE NODE IS THE BUG THIS FILE SHIPPED WITH. `MatchSetup.tscn` parks
    /// its Camera3D at (0, 8.5, 19) with an identity basis, and `map_preview.gd::_apply_camera`
    /// OVERWRITES both every single frame from the registry entry — yaw 0, distance 22,
    /// **height 16** — and then calls `look_at`. The authored transform is a placeholder that
    /// is never seen for even one frame.
    ///
    /// Taking it literally produced exactly what was reported: a camera 8.5 m up with NO pitch,
    /// staring level down an empty street at the backs of the far houses, where Godot looks
    /// down at the play area from 16 m. The difference is a 33° tilt and it is the whole shot.
    ///
    /// ⚠️ AND IT SWAYS. `SWAY_DEGREES 7` over a `SWAY_PERIOD` of 26 s. Deliberately a sway and
    /// not an orbit: a full orbit eventually swings behind the facades and shows the player the
    /// back of a set, where a sway only ever leaves the tuned angle by a few degrees, so every
    /// frame is one somebody chose.
    ///
    /// ⚠️ THE PIVOT IS THE PLAY AREA, WHICH IS THE AVERAGE OF THE MAP'S `SpawnPoints`. Every map
    /// carries them (it is how the match places characters), so this finds the court without
    /// knowing anything map-specific. Falling back to the origin is safe: both current maps put
    /// their base circle there anyway.
    ///
    /// ⚠️ AND IT FAILS TO NOTHING. If the arena scene is missing from the build or the load
    /// throws, the surface stays transparent and the screen still works.
    /// </summary>
    /// <remarks>
    /// ⚠️ IT STARTS TRANSPARENT IN THE EDITOR TOO. A RawImage with no texture draws an opaque
    /// WHITE quad, and this one is full-screen behind every panel on the setup screen.
    ///
    /// ⚠️ MAPS ARE CACHED, NEVER RELOADED, matching `map_preview.gd`'s own note: cycling the
    /// picker after the first visit to each map costs nothing. Godot re-parents the inactive
    /// map out of the tree rather than hiding it, because `visible = false` does not stop a
    /// WorldEnvironment or a DirectionalLight3D. Unity's additive scenes have the same problem
    /// and the same answer: the inactive arena's roots are deactivated AND its lights disabled
    /// explicitly, so two maps cannot fight over the sky.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public sealed class MapPreviewSurface : MonoBehaviour
    {
        /// <summary>
        /// Where the camera sits when a map's registry entry says nothing. Deliberately a
        /// legible three-quarter view rather than anything clever, from `map_preview.gd:46`.
        /// </summary>
        public const float DefaultYaw = 34.0f;
        public const float DefaultDistance = 19.0f;
        public const float DefaultHeight = 8.5f;

        public const float SwayDegrees = 7.0f;
        public const float SwayPeriod = 26.0f;

        /// <summary>What the camera aims at above the floor: roughly head height on a standing
        /// Person, so the shot is framed on the fight rather than on the road.</summary>
        public const float LookHeight = 1.6f;

        /// <summary>From `MatchSetup.tscn`'s Camera3D. The one property of that node that IS
        /// read, because `_apply_camera` writes position and basis but never the FOV.</summary>
        private const float FieldOfView = 58.0f;

        /// <summary>Half the screen is enough behind a scrim, and it halves the cost.</summary>
        private const int Width = 960;
        private const int Height = 540;

        private RawImage _surface;
        private RenderTexture _target;
        private Camera _camera;
        private string _showing;
        private bool _busy;

        private Vector3 _pivot;
        private float _yaw = DefaultYaw;
        private float _distance = DefaultDistance;
        private float _height = DefaultHeight;
        private float _time;

        /// <summary>Every arena this screen has shown, kept loaded and deactivated. See the
        /// remark on caching.</summary>
        private readonly System.Collections.Generic.Dictionary<string, Scene> _cache =
            new System.Collections.Generic.Dictionary<string, Scene>();

        private void OnEnable()
        {
            _surface = GetComponent<RawImage>();
            if (_surface.texture != null) return;

            _surface.color = new Color(1, 1, 1, 0);
            _surface.raycastTarget = false;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            Show(SceneFlow.SelectedMap);
        }

        public void Show(string map)
        {
            if (_busy || map == _showing) return;
            StartCoroutine(Swap(map));
        }

        private IEnumerator Swap(string map)
        {
            _busy = true;

            // ⚠️ THE OUTGOING MAP IS PARKED, NOT UNLOADED, exactly as `map_preview.gd` parks
            // its instance in `_cache`. Unloading and reloading a dressed street on every arrow
            // press is a visible stall on the one screen a player cycles fastest.
            Park(_showing);

            if (_cache.TryGetValue(map, out var cached) && cached.IsValid() && cached.isLoaded)
            {
                Unpark(map);
            }
            else
            {
                if (!Application.CanStreamedLevelBeLoaded(map))
                {
                    Debug.LogWarning($"[MapPreview] '{map}' is not in the build settings; " +
                                     "the setup screen keeps its backdrop.");
                    _busy = false;
                    yield break;
                }

                // ⚠️⚠️ SET BEFORE THE LOAD, NOT AFTER. `MatchInstaller.Start` runs the instant
                // the additive scene finishes loading, and by the time this coroutine resumes it
                // has already spawned four characters, the can and the directors. Stripping them
                // afterwards left a frame of bots mid-spawn behind the menu and a round timer
                // that had started. The flag makes the installer stand down before it builds
                // anything.
                MatchInstaller.PreviewOnly = true;

                var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Additive);
                while (load != null && !load.isDone) yield return null;

                MatchInstaller.PreviewOnly = false;

                var loaded = SceneManager.GetSceneByName(map);
                _cache[map] = loaded;

                StripMatchObjects(loaded);
                Silence(loaded);
            }

            _showing = map;

            AimAt(map);
            EnsureCamera();

            _surface.texture = _target;
            _surface.color = Color.white;

            _busy = false;
        }

        /// <summary>
        /// ⚠️ THE INACTIVE MAP'S LIGHTS ARE TURNED OFF EXPLICITLY, and deactivating the roots
        /// is not enough on its own to make that certain across render pipelines. This is the
        /// same failure `map_preview.gd` records at length: a hidden map "keeps lighting the
        /// world and keeps fighting the other map's environment for it, which reads as the sky
        /// and fog changing while the geometry does not".
        /// </summary>
        private void Park(string map)
        {
            if (map == null || !_cache.TryGetValue(map, out var scene)) return;
            if (!scene.IsValid() || !scene.isLoaded) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;
                root.SetActive(false);
            }
        }

        private void Unpark(string map)
        {
            if (!_cache.TryGetValue(map, out var scene)) return;
            if (!scene.IsValid() || !scene.isLoaded) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                root.SetActive(true);
                foreach (var light in root.GetComponentsInChildren<Light>(true)) light.enabled = true;
            }
        }

        /// <summary>
        /// ⚠️ THE PIVOT IS THE AVERAGE OF THE MAP'S SPAWN MARKERS, from
        /// `map_preview.gd::_play_area_centre`. Where the round happens is what the shot should
        /// be about, and every map ships those markers because the match uses them.
        ///
        /// ⚠️ AND THE Z IS ALREADY MIRRORED. The map conversion flips Z (Godot is right-handed
        /// with -Z forward), so reading the markers out of the converted scene gives the pivot
        /// in Unity space with no second flip needed. It is the CAMERA OFFSET below that has to
        /// be mirrored by hand, because that one is computed from the registry's Godot yaw.
        /// </summary>
        private void AimAt(string map)
        {
            _pivot = Vector3.zero;
            _yaw = DefaultYaw;
            _distance = DefaultDistance;
            _height = DefaultHeight;

            var entry = SceneFlow.PreviewFor(map);
            _yaw = entry.Yaw;
            _distance = entry.Distance;
            _height = entry.Height;

            if (!_cache.TryGetValue(map, out var scene) || !scene.IsValid()) return;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                var points = FindByName(root.transform, "SpawnPoints");
                if (points == null) continue;

                foreach (Transform child in points)
                {
                    sum += child.position;
                    count++;
                }
            }

            if (count > 0) _pivot = sum / count;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name) return root;

            foreach (Transform child in root)
            {
                var hit = FindByName(child, name);
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ A MAP SCENE BRINGS THE WHOLE MATCH WITH IT. Its installer spawns four characters,
        /// a can, the slippers and the directors the moment it loads, so an arena dropped in for
        /// a preview would start a game behind the menu. Everything that makes it a match rather
        /// than a set is removed here; only the geometry stays.
        /// </summary>
        private static void StripMatchObjects(Scene scene)
        {
            if (!scene.IsValid()) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var installer in root.GetComponentsInChildren<MatchInstaller>(true))
                    Destroy(installer.gameObject);

                foreach (var motor in root.GetComponentsInChildren<CharacterMotor>(true))
                    Destroy(motor.gameObject);

                foreach (var hud in root.GetComponentsInChildren<Canvas>(true))
                    Destroy(hud.gameObject);

                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                    Destroy(listener);

                // The arena's own camera would fight ours for the display.
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                    Destroy(cam.gameObject);
            }
        }

        /// <summary>
        /// ⚠️⚠️ EVERY MAP CARRIES AN AUTOPLAYING AMBIENCE BED, and instancing one here starts
        /// the street playing over the menu — then restarts it on every arrow press.
        /// `map_preview.gd::_silence` strips the streams before the instance ever enters the
        /// tree, and walks the whole subtree rather than looking for the `Ambience` node by
        /// name: the name is a convention both maps happen to share, and a map that grew a
        /// second player somewhere else would silently start making noise on the menu.
        /// </summary>
        private static void Silence(Scene scene)
        {
            if (!scene.IsValid()) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                {
                    source.playOnAwake = false;
                    source.Stop();
                    source.clip = null;
                    source.enabled = false;
                }
            }
        }

        private void EnsureCamera()
        {
            if (_target == null)
            {
                _target = new RenderTexture(Width, Height, 24) { name = "MapPreview" };
                _target.Create();
            }

            if (_camera == null)
            {
                var go = new GameObject("MapPreviewCamera");
                _camera = go.AddComponent<Camera>();

                // ⚠️ IT LIVES IN THE MENU SCENE, NOT THE ARENA, so swapping maps does not take
                // the camera with it and leave a black rectangle.
                go.transform.SetParent(null, true);
            }

            _camera.fieldOfView = FieldOfView;
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.depth = -10;

            ApplyCamera();
        }

        private void Update()
        {
            if (!Application.isPlaying || _camera == null || _showing == null) return;

            _time += Time.unscaledDeltaTime;
            ApplyCamera();
        }

        /// <summary>
        /// `map_preview.gd::_apply_camera`, with the one handedness flip the conversion needs.
        ///
        /// Godot: `offset = Basis(UP, deg2rad(yaw + sway)) * Vector3(0, 0, distance)`, which is
        /// `(d·sin a, 0, d·cos a)`. The map conversion mirrors Z, so the same point in Unity is
        /// `(d·sin a, 0, -d·cos a)`. Written out rather than expressed as a rotated forward
        /// vector, because the sign is the whole bug and a reader has to be able to check it.
        /// </summary>
        private void ApplyCamera()
        {
            float sway = Mathf.Sin(_time * Mathf.PI * 2.0f / SwayPeriod) * SwayDegrees;
            float a = (_yaw + sway) * Mathf.Deg2Rad;

            var offset = new Vector3(Mathf.Sin(a) * _distance, 0.0f, -Mathf.Cos(a) * _distance);

            _camera.transform.position = _pivot + offset + new Vector3(0.0f, _height, 0.0f);
            _camera.transform.LookAt(_pivot + new Vector3(0.0f, LookHeight, 0.0f));
        }

        private void OnDestroy()
        {
            if (_camera != null) Destroy(_camera.gameObject);

            if (_target == null) return;

            _target.Release();
            Destroy(_target);
        }
    }
}
