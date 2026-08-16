using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The Godot Environment's `adjustment_*` grade, applied to a camera's whole frame.
    ///
    /// ⚠️⚠️ THE PORT HAD NO EQUIVALENT OF THIS AT ALL. Both arenas and the character preview
    /// enable it, and it is a visible part of the look rather than a subtlety: Eskinita runs
    /// contrast 1.03 and saturation 1.18 over everything, so the Godot street is warmer and more
    /// saturated than the same geometry under the same lights in this build was.
    ///
    /// ⚠️ THE VALUES COME FROM THE MAP, NOT FROM A CONSTANT HERE. Eskinita grades and Bayan
    /// Plaza does not (it has no `adjustment_enabled` line at all), so hard-coding one set would
    /// be wrong on one of the two maps. <see cref="MapGrade"/> is what the importer leaves in the
    /// scene, and the default of 1/1/1 is an exact no-op for a map that grades nothing.
    ///
    /// ⚠️ AND IT IS OPT-IN PER CAMERA. A blit runs on every camera it is attached to, and the
    /// menu has several (the map preview and the character portrait each own one). Grading the
    /// character portrait with the ARENA's numbers would make the person you pick a slightly
    /// different colour from the person who walks out.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class ColourGrade : MonoBehaviour
    {
        [SerializeField] private float _brightness = 1.0f;
        [SerializeField] private float _contrast = 1.0f;
        [SerializeField] private float _saturation = 1.0f;

        /// <summary>Zero disables the tonemap. See the shader.</summary>
        [SerializeField] private float _exposure;
        [SerializeField] private float _white = 1.9f;

        private Material _material;

        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int WhiteId = Shader.PropertyToID("_White");

        public void Set(float brightness, float contrast, float saturation,
                        float exposure, float white)
        {
            _brightness = brightness;
            _contrast = contrast;
            _saturation = saturation;
            _exposure = exposure;
            _white = white;
        }

        /// <summary>Copies whatever grade the loaded map carries. Nothing found is a no-op grade
        /// rather than an error: a map is allowed not to grade.</summary>
        public void AdoptFromScene()
        {
            var grade = FindFirstObjectByType<MapGrade>(FindObjectsInactive.Include);

            if (grade == null)
            {
                Set(1.0f, 1.0f, 1.0f, 0.0f, 1.9f);
                return;
            }

            Set(grade.Brightness, grade.Contrast, grade.Saturation,
                grade.Exposure, grade.White);
        }

        private bool IsIdentity =>
            Mathf.Approximately(_brightness, 1.0f)
            && Mathf.Approximately(_contrast, 1.0f)
            && Mathf.Approximately(_saturation, 1.0f)
            && _exposure <= 0.0f;

        /// <summary>
        /// ⚠️ A NO-OP GRADE STILL COSTS A FULL-SCREEN BLIT, so it is skipped outright. This runs
        /// on the match camera every frame and Bayan Plaza grades nothing.
        /// </summary>
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (IsIdentity)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (_material == null)
            {
                // ⚠️ A SHADER ONLY `Shader.Find` REACHES IS STRIPPED FROM THE PLAYER. Same rule
                // `ToonSkin` carries: it has to be listed in GameBuilder.EnsureRuntimeShaders or
                // this grades nothing in a build while working perfectly in the editor.
                var shader = Shader.Find("TumbangPreso/ColourGrade");

                if (shader == null)
                {
                    Graphics.Blit(source, destination);
                    enabled = false;
                    return;
                }

                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            _material.SetFloat(BrightnessId, _brightness);
            _material.SetFloat(ContrastId, _contrast);
            _material.SetFloat(SaturationId, _saturation);
            _material.SetFloat(ExposureId, _exposure);
            _material.SetFloat(WhiteId, _white);

            Graphics.Blit(source, destination, _material);
        }

        private void OnDestroy()
        {
            if (_material != null) DestroyImmediate(_material);
        }
    }
}
