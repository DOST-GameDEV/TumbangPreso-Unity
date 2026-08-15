using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A live 3D model on a UI panel, converted from `scripts/ui/character_preview.gd`.
    ///
    /// Godot used a SubViewport with `own_world_3d` so the preview brought its own lighting
    /// and none of it leaked into the menu. Unity's equivalent is a second Camera rendering
    /// to a RenderTexture, with the model parked on a dedicated layer nothing else draws.
    ///
    /// ⚠️ THE MODEL SITS FAR FROM THE ORIGIN AND ON ITS OWN LAYER. Both halves matter: the
    /// offset keeps it out of any arena that happens to be loaded behind the menu, and the
    /// layer keeps the preview camera from photographing the arena instead of the model.
    ///
    /// ⚠️⚠️ THE MODEL TURNS, THE CAMERA DOES NOT ORBIT. `character_preview.gd:447` writes
    /// `pivot.rotation.y` — the subject rotates on its own spot while the camera holds the
    /// tuned three-quarter angle. Orbiting the camera instead looks similar for one frame and
    /// then diverges: the framing offset and the look-down angle are measured against a fixed
    /// viewpoint, and moving the viewpoint invalidates both.
    ///
    /// ⚠️ AND THE IDLE SWEEP YIELDS TO THE PLAYER PERMANENTLY. Once they drag, it stops for
    /// good (`_user_took_over`). Left running, a subject someone is trying to inspect rotates
    /// out from under the angle they just chose, which makes the control feel broken rather
    /// than free. Do not "restore" the sweep on a timer after a drag.
    /// </summary>
    public sealed class ModelPreview : MonoBehaviour
    {
        public const float CameraYawDegrees = 18.0f;
        public const float CameraPitchDegrees = 16.0f;

        /// <summary>A lata or a tsinelas lies on the ground and has to be looked DOWN at;
        /// a standing Person does not.</summary>
        public const float CameraPitchFlatDegrees = 52.0f;

        /// <summary>How much room to leave around the model when framing it. Above 1 so the
        /// silhouette never touches the panel edge.</summary>
        public const float FrameMargin = 1.62f;

        /// <summary>Where the camera aims up the model's own height — above centre, because
        /// a portrait framed on the navel reads as a mistake.</summary>
        public const float AimHeightRatio = 0.54f;

        public const float FrameHorizontalOffsetRatio = -0.19f;
        public const float PreviewScale = 2.38f;

        public const float TurnDegrees = 38.0f;
        public const float TurnPeriod = 9.0f;

        public const float OrbitSensitivity = 0.4f;
        public const float OrbitPitchMin = -55.0f;
        public const float OrbitPitchMax = 70.0f;

        public const float ZoomMin = 0.55f;
        public const float ZoomMax = 2.2f;
        public const float ZoomStep = 0.12f;

        /// <summary>Where preview models live, well clear of any loaded arena.</summary>
        public static readonly Vector3 Stage = new Vector3(0.0f, -500.0f, 0.0f);

        /// <summary>The layer the preview camera draws and nothing else does.</summary>
        public const int PreviewLayer = 30;

        private UnityEngine.Camera _camera;
        private RenderTexture _texture;
        private RawImage _surface;
        private Transform _pivot;
        private GameObject _model;

        private float _turnPhase;
        private float _modelYaw;
        private bool _userTookOver;
        private float _orbitPitch;
        private float _zoom = 1.0f;
        private float _radius = 3.0f;
        private float _aimHeight = 1.0f;
        private bool _flat;

        /// <summary>Builds the render target and camera. Call once, then <see cref="Show"/>.</summary>
        public void Attach(RectTransform panel)
        {
            _texture = new RenderTexture(512, 640, 24) { name = "PreviewRT" };

            var surfaceGo = new GameObject("PreviewSurface", typeof(RectTransform), typeof(RawImage));
            surfaceGo.transform.SetParent(panel, false);
            _surface = surfaceGo.GetComponent<RawImage>();
            _surface.texture = _texture;
            MenuKit.Stretch(_surface.rectTransform);

            var stageGo = new GameObject("PreviewStage");
            stageGo.transform.position = Stage;
            _pivot = stageGo.transform;

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(_pivot, false);

            _camera = camGo.AddComponent<UnityEngine.Camera>();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;

            // Transparent, so the panel's own wood shows through behind the model rather than
            // a flat slab of colour that has to be matched by hand.
            _camera.backgroundColor = new Color(0, 0, 0, 0);
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.fieldOfView = 32.0f;
            _camera.nearClipPlane = 0.05f;

            // Its own light, so the preview reads the same whatever scene is behind the menu.
            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(_pivot, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.cullingMask = 1 << PreviewLayer;
            lightGo.transform.rotation = Quaternion.Euler(38.0f, 150.0f, 0.0f);
        }

        /// <summary>
        /// Swap in a model. <paramref name="flat"/> for a lata or tsinelas, which lies on the
        /// ground and needs the steeper look-down angle.
        /// </summary>
        public void Show(GameObject prefab, bool flat)
        {
            if (_model != null) Destroy(_model);
            if (prefab == null) return;

            _flat = flat;

            _model = Instantiate(prefab, _pivot);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            _model.transform.localScale = Vector3.one * PreviewScale;

            // A new subject gets a fresh sweep: the player picked this one to look at.
            _turnPhase = 0.0f;
            _modelYaw = 0.0f;
            _userTookOver = false;

            SetLayerRecursively(_model, PreviewLayer);
            FrameModel();
        }

        /// <summary>
        /// ⚠️ THE FRAMING IS MEASURED FROM THE MODEL, NOT TYPED IN. Every roster entry is a
        /// different height, and a fixed camera distance either crops the tall ones or leaves
        /// the short ones as a speck. Read the bounds, fit the distance.
        /// </summary>
        private void FrameModel()
        {
            var bounds = new Bounds(_model.transform.position, Vector3.zero);
            bool any = false;

            foreach (var r in _model.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (!any) { _radius = 3.0f; _aimHeight = 1.0f; return; }

            float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float half = Mathf.Max(0.05f, extent * 0.5f);

            _radius = half * FrameMargin / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _aimHeight = bounds.size.y * AimHeightRatio;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            // THE MODEL TURNS. The idle sweep is a there-and-back through TurnDegrees either
            // side of front-on, over TurnPeriod seconds — it stops the moment the player drags.
            if (_model != null)
            {
                if (!_userTookOver)
                {
                    _turnPhase += Time.unscaledDeltaTime;
                    float sweep = Mathf.Sin(_turnPhase / TurnPeriod * Mathf.PI * 2.0f) * TurnDegrees;
                    _modelYaw = sweep;
                }

                _model.transform.localRotation = Quaternion.Euler(0.0f, _modelYaw, 0.0f);
            }

            // THE CAMERA HOLDS ITS ANGLE. Pitch is the only thing the player moves, and a
            // flat subject is looked down at more steeply than a standing one.
            float pitch = Mathf.Clamp(
                (_flat ? CameraPitchFlatDegrees : CameraPitchDegrees) + _orbitPitch,
                OrbitPitchMin, OrbitPitchMax);

            var rot = Quaternion.Euler(pitch, CameraYawDegrees, 0.0f);

            Vector3 target = Stage
                             + Vector3.up * _aimHeight
                             + Vector3.right * (_radius * FrameHorizontalOffsetRatio);

            _camera.transform.position = target - (rot * Vector3.forward) * (_radius * _zoom);
            _camera.transform.rotation = rot;
        }

        /// <summary>
        /// Drag to turn the model and tilt the view.
        ///
        /// ⚠️ THE FIRST DRAG ENDS THE IDLE SWEEP FOR GOOD. See the class note.
        /// </summary>
        public void Orbit(Vector2 delta)
        {
            _userTookOver = true;
            _modelYaw += delta.x * OrbitSensitivity;
            _orbitPitch = Mathf.Clamp(_orbitPitch - delta.y * OrbitSensitivity,
                OrbitPitchMin, OrbitPitchMax);
        }

        public void Zoom(float wheel)
            => _zoom = Mathf.Clamp(_zoom - wheel * ZoomStep, ZoomMin, ZoomMax);

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDestroy()
        {
            if (_texture != null) _texture.Release();
            if (_pivot != null) Destroy(_pivot.gameObject);
        }
    }
}
