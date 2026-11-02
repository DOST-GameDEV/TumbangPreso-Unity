using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The cast's crease shade and the portraits' dark-tone lift, on a character display's camera and on no other.
    /// A match draws its crease shade in `WorldOutline`, which needs the match's camera and look; a display has
    /// neither, so this runs the same kernel as a pass of its own (`PreviewCrease.shader`) on the display's frame.
    /// The shade goes in before the grade, as it does in play. The lift goes in after it, because it is a curve on
    /// the finished tones, so the grade's component is sandwiched between this one and `StyleRestore`, which calls <see cref="Lift"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PreviewCrease : MonoBehaviour
    {
        /// <summary>The match's Full character shading (`GameSettings.CharacterShadingShare`).</summary>
        public const float FullShare = .6f;
        /// <summary>How deep the shade goes toward the cavity hue, the cast's own (`WorldOutline` composite).</summary>
        public const float Depth = .6f;
        public const float LiftKnee = .4f, LiftPower = .5f;

        private static readonly int ParamsId = Shader.PropertyToID("_CreaseParams"), ViewId = Shader.PropertyToID("_CreaseView"),
            HueId = Shader.PropertyToID("_CreaseHue"), LiftId = Shader.PropertyToID("_LiftParams"),
            CreaseTexId = Shader.PropertyToID("_CreaseTex"), DepthNormalsId = Shader.PropertyToID("_CameraDepthNormalsTexture");

        /// <summary>The kernel's radius in scene units for the subject on show; zero or less for a subject that is not a person.</summary>
        public System.Func<float> Radius;

        private Camera _camera;
        private Material _material;
        private bool _manual;

        private float LiveShare => Settings.GameSettings.CharacterShadingShare(Settings.SettingsStore.Current.CharacterShading);
        private float CurrentRadius => Radius != null ? Radius() : 0;

        /// <summary>True when the subject is a person and the pass can run.</summary>
        public bool Active => CurrentRadius > 0 && !_camera.orthographic && Material != null;

        private Material Material
        {
            get
            {
                if (_material != null) return _material;
                // Reached only by Shader.Find, so GameBuilder.EnsureRuntimeShaders lists it or a build draws no shade.
                var shader = Shader.Find("Hidden/TumbangPreso/PreviewCrease");
                if (shader == null) return null;
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                return _material;
            }
        }

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnDestroy() { if (_material != null) Destroy(_material); }

        // The depth-normals prepass is only asked for while there is a person to shade.
        private void LateUpdate()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            bool need = Active && LiveShare > 0;
            if (need) _camera.depthTextureMode |= DepthTextureMode.DepthNormals;
            else _camera.depthTextureMode &= ~DepthTextureMode.DepthNormals;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            float share = LiveShare;
            if (_manual || share <= 0 || !Active) { Graphics.Blit(source, destination); return; }
            Shade(source, destination, share);
        }

        private void Shade(RenderTexture source, RenderTexture destination, float share)
        {
            var look = Visual.WorldLookProfile.Current;
            float radius = CurrentRadius, tan = Mathf.Tan(_camera.fieldOfView * .5f * Mathf.Deg2Rad);
            var material = Material;
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            var occlusion = RenderTexture.GetTemporary(source.width, source.height, 0, format, RenderTextureReadWrite.Linear);
            var soft = RenderTexture.GetTemporary(source.width, source.height, 0, format, RenderTextureReadWrite.Linear);
            // The bias follows the radius down, or a tight kernel sits wholly inside its own bias.
            material.SetVector(ParamsId, new Vector4(share, radius, Mathf.Min(.02f, radius * .1f), 0));
            material.SetVector(ViewId, new Vector4(tan * source.width / source.height, tan, _camera.farClipPlane, 0));
            material.SetVector(HueId, (Vector4)look.CavityHue.linear * Depth);
            Graphics.Blit(source, occlusion, material, 0);
            Graphics.Blit(occlusion, soft, material, 1);
            material.SetTexture(CreaseTexId, soft);
            Graphics.Blit(source, destination, material, 2);
            RenderTexture.ReleaseTemporary(occlusion);
            RenderTexture.ReleaseTemporary(soft);
        }

        /// <summary>The lift on a finished frame. Called by `StyleRestore`, after the grade.</summary>
        public void Lift(RenderTexture source, RenderTexture destination, float power = LiftPower)
        {
            if (_manual || !Active) { Graphics.Blit(source, destination); return; }
            var material = Material;
            material.SetVector(LiftId, new Vector4(LiftKnee, power, 0, 0));
            Graphics.Blit(source, destination, material, 3);
        }

        /// <summary>
        /// The shade on a finished render, for a caller outside play mode (the portrait baker): edit mode sends no
        /// OnRenderImage, and the camera's own depth-normals texture is gone by the time Render returns, so the depth
        /// and normals are drawn here with the engine's own shader, which is what the prepass does.
        /// </summary>
        public void ShadeByHand(RenderTexture colour, float share)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (share <= 0 || !Active) return;
            _manual = true;
            var depthNormals = RenderTexture.GetTemporary(colour.width, colour.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var shaded = RenderTexture.GetTemporary(Plain(colour));
            var savedTarget = _camera.targetTexture;
            var savedClear = _camera.backgroundColor;
            var savedGlobal = Shader.GetGlobalTexture(DepthNormalsId);
            try
            {
                _camera.targetTexture = depthNormals;
                _camera.backgroundColor = new Color(.5f, .5f, 1, 1);
                _camera.RenderWithShader(Shader.Find("Hidden/Internal-DepthNormalsTexture"), "RenderType");
                _camera.targetTexture = savedTarget;
                _camera.backgroundColor = savedClear;
                Shader.SetGlobalTexture(DepthNormalsId, depthNormals);
                Shade(colour, shaded, share);
                Graphics.Blit(shaded, colour);
            }
            finally
            {
                _camera.targetTexture = savedTarget;
                _camera.backgroundColor = savedClear;
                Shader.SetGlobalTexture(DepthNormalsId, savedGlobal);
                RenderTexture.ReleaseTemporary(depthNormals);
                RenderTexture.ReleaseTemporary(shaded);
                _manual = false;
            }
        }

        /// <summary>The same colour format without the multisampling or depth a scratch copy does not need.</summary>
        private static RenderTextureDescriptor Plain(RenderTexture like)
        {
            var descriptor = like.descriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            return descriptor;
        }

        /// <summary>The lift on a finished frame, in place, for the baker.</summary>
        public void LiftByHand(RenderTexture image, float power)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (!Active) return;
            var lifted = RenderTexture.GetTemporary(Plain(image));
            Lift(image, lifted, power);
            Graphics.Blit(lifted, image);
            RenderTexture.ReleaseTemporary(lifted);
        }
    }
}
