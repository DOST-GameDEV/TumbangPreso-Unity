using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The setup screen's background: the map you actually have selected, live in 3D,
    /// swapping the instant you cycle the picker. Converted from `scripts/ui/map_preview.gd`.
    ///
    /// It replaced a blueprint-grid placeholder that had outlived its job — by the time two
    /// dressed maps existed, the screen was asking the player to choose between two names on
    /// a backdrop that showed neither.
    ///
    /// ⚠️⚠️ THE CAMERA SWAYS, IT DOES NOT ORBIT. A full orbit eventually swings behind the
    /// facades and shows the player the back of a set. A slow sway keeps the tuned angle and
    /// only ever leaves it by a few degrees, so every frame is one somebody chose. This is
    /// the OPPOSITE of the character preview, where the model turns and the camera holds
    /// still — do not unify the two.
    ///
    /// ⚠️ MAPS ARE CACHED AND DEACTIVATED, NEVER DESTROYED. Re-instancing an arena on every
    /// press of the picker is a visible hitch; keeping the inactive one parked makes cycling
    /// feel free after the first visit to each map.
    ///
    /// ⚠️ AND THEY ARE SILENCED ON THE WAY IN. Every map carries an ambience player set to
    /// play on awake — instancing one here starts the street bed over the menu, and cycling
    /// the picker restarts it on every press. The sources are stripped BEFORE the instance is
    /// ever enabled, so the sound never gets a frame to start in.
    /// </summary>
    public sealed class MapPreview : MonoBehaviour
    {
        /// <summary>Where the camera sits when a map says nothing. Deliberately a legible
        /// three-quarter view rather than anything clever — a new map should look acceptable
        /// before anyone has tuned it, and obviously untuned rather than subtly wrong.</summary>
        public const float DefaultYaw = 34.0f;
        public const float DefaultDistance = 19.0f;
        public const float DefaultHeight = 8.5f;

        public const float SwayDegrees = 7.0f;
        public const float SwayPeriod = 26.0f;

        /// <summary>What the camera aims at above the floor — roughly head height on a
        /// standing Person, so the shot is framed on the fight rather than on the road.</summary>
        public const float LookHeight = 1.6f;

        public const int PreviewLayer = 29;

        private UnityEngine.Camera _camera;
        private RenderTexture _texture;
        private RawImage _surface;
        private GameObject _current;
        private string _currentName;
        private float _time;

        private readonly System.Collections.Generic.Dictionary<string, GameObject> _cache =
            new System.Collections.Generic.Dictionary<string, GameObject>();

        public void Attach(RectTransform panel)
        {
            _texture = new RenderTexture(1280, 720, 24) { name = "MapPreviewRT" };

            var surfaceGo = new GameObject("MapSurface", typeof(RectTransform), typeof(RawImage));
            surfaceGo.transform.SetParent(panel, false);
            surfaceGo.transform.SetAsFirstSibling();   // it is a BACKDROP

            _surface = surfaceGo.GetComponent<RawImage>();
            _surface.texture = _texture;
            _surface.raycastTarget = false;
            MenuKit.Stretch(_surface.rectTransform);

            var camGo = new GameObject("MapPreviewCamera");
            _camera = camGo.AddComponent<UnityEngine.Camera>();
            _camera.targetTexture = _texture;
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.fieldOfView = 42.0f;
            _camera.farClipPlane = 400.0f;
        }

        /// <summary>Show a map by its scene/prefab name, instancing it once and caching it.</summary>
        public void Show(string mapName)
        {
            if (_currentName == mapName) return;

            if (_current != null) _current.SetActive(false);

            if (!_cache.TryGetValue(mapName, out var instance))
            {
                var prefab = Resources.Load<GameObject>($"Maps/{mapName}");
                if (prefab == null)
                {
                    Debug.Log($"[MapPreview] no preview prefab for {mapName}; showing nothing.");
                    _currentName = mapName;
                    return;
                }

                instance = Instantiate(prefab);
                Silence(instance);
                SetLayerRecursively(instance, PreviewLayer);
                _cache[mapName] = instance;
            }

            instance.SetActive(true);
            _current = instance;
            _currentName = mapName;
        }

        /// <summary>Strip every audio source before the instance can play a frame of ambience
        /// over the menu.</summary>
        private static void Silence(GameObject go)
        {
            foreach (var src in go.GetComponentsInChildren<AudioSource>(true))
            {
                src.playOnAwake = false;
                src.Stop();
                src.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            _time += Time.unscaledDeltaTime;

            float sway = Mathf.Sin(_time / SwayPeriod * Mathf.PI * 2.0f) * SwayDegrees;
            float yaw = DefaultYaw + sway;

            var look = new Vector3(0.0f, LookHeight, 0.0f);
            var rot = Quaternion.Euler(0.0f, yaw, 0.0f);

            Vector3 offset = rot * new Vector3(0.0f, 0.0f, -DefaultDistance);
            Vector3 pos = look + offset + Vector3.up * DefaultHeight;

            _camera.transform.position = pos;
            _camera.transform.rotation = Quaternion.LookRotation((look - pos).normalized, Vector3.up);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDestroy()
        {
            if (_texture != null) _texture.Release();
            foreach (var pair in _cache) if (pair.Value != null) Destroy(pair.Value);
        }
    }
}
