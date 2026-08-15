using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The live map render behind the setup screen, ported from `MatchSetup.tscn`'s SubViewport.
    ///
    /// ⚠️⚠️ THIS IS NOT A SCREENSHOT AND IT WAS NOT OPTIONAL. Godot puts a `SubViewport` with
    /// `own_world_3d` behind the setup panels and renders the chosen arena into it live, which
    /// is why picking a map on that screen shows you the map. The conversion skipped every
    /// viewport node, so the screen came out as flat navy with panels floating on it and the
    /// MAP selector changed a word and nothing else.
    ///
    /// ⚠️ THE CAMERA TRANSFORM IS THE ONE FROM THE .tscn: y 8.5, z 19, fov 58. It was framed
    /// against these arenas; picking a new one by eye reframes both maps at once.
    ///
    /// ⚠️ AND IT FAILS TO NOTHING. If the arena scene is missing from the build or the load
    /// throws, the surface stays transparent and the screen still works. A menu that cannot be
    /// used because its decoration broke is worse than a menu with no decoration.
    /// </summary>
    /// <remarks>
    /// ⚠️ IT STARTS TRANSPARENT IN THE EDITOR TOO. A RawImage with no texture draws an opaque
    /// WHITE quad, and this one is full-screen behind every panel on the setup screen. Left at
    /// the default it turns the whole backdrop white in any capture and in any scene view, which
    /// looks exactly like a broken panel conversion and sent one pass chasing the wrong bug.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public sealed class MapPreviewSurface : MonoBehaviour
    {
        private static readonly Vector3 CameraPosition = new Vector3(0.0f, 8.5f, 19.0f);
        private const float FieldOfView = 58.0f;

        /// <summary>Half the screen is enough behind a scrim, and it halves the cost.</summary>
        private const int Width = 960;
        private const int Height = 540;

        private RawImage _surface;
        private RenderTexture _target;
        private Camera _camera;
        private Scene _loaded;
        private string _showing;
        private bool _busy;

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

            if (_loaded.IsValid() && _loaded.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(_loaded);
                while (unload != null && !unload.isDone) yield return null;
            }

            if (!Application.CanStreamedLevelBeLoaded(map))
            {
                Debug.LogWarning($"[MapPreview] '{map}' is not in the build settings; " +
                                 "the setup screen keeps its backdrop.");
                _busy = false;
                yield break;
            }

            var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Additive);
            while (load != null && !load.isDone) yield return null;

            _loaded = SceneManager.GetSceneByName(map);
            _showing = map;

            StripMatchObjects();
            EnsureCamera();

            _surface.texture = _target;
            _surface.color = Color.white;

            _busy = false;
        }

        /// <summary>
        /// ⚠️ A MAP SCENE BRINGS THE WHOLE MATCH WITH IT. Its installer spawns four characters,
        /// a can, the slippers and the directors the moment it loads, so an arena dropped in for
        /// a preview would start a game behind the menu: bots would run, sounds would fire, and
        /// the match timer would already be counting when the player pressed START. Everything
        /// that makes it a match rather than a set is removed here; only the geometry stays.
        /// </summary>
        private void StripMatchObjects()
        {
            if (!_loaded.IsValid()) return;

            foreach (var root in _loaded.GetRootGameObjects())
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

                // ⚠️ IT LIVES IN THE MENU SCENE, NOT THE ARENA, so unloading the arena to swap
                // maps does not take the camera with it and leave a black rectangle.
                go.transform.SetParent(null, true);
            }

            _camera.transform.position = CameraPosition;
            _camera.transform.rotation = Quaternion.identity;
            _camera.fieldOfView = FieldOfView;
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.depth = -10;
        }

        private void OnDestroy()
        {
            if (_camera != null) Destroy(_camera.gameObject);

            if (_target == null) return;

            _target.Release();
            Destroy(_target);
        }
    }

    /// <summary>
    /// Hover and press feedback for a bare TextureButton: the selector arrows.
    ///
    /// ⚠️ THESE MAKE A NOISE TOO. `arrow_button.gd` covers the pennants and the wood set covers
    /// the buttons, but the little arrows either side of MAP and BOTS are plain TextureButtons
    /// with their own two connections at their call site in the Godot build. Leaving them silent
    /// makes the one control a player clicks most on that screen the only dead one.
    /// </summary>
    public sealed class TextureButtonFeedback : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        private Image _image;
        private Vector3 _home = Vector3.one;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _home = transform.localScale;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home * 1.12f;
            if (_image != null) _image.color = UiTheme.Amber;
            MenuSfx.Hover();
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home;
            if (_image != null) _image.color = Color.white;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home * 0.92f;
            MenuSfx.Click();
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home * 1.12f;
        }
    }
}
