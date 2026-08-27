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
        private float _chromaticPeak;
        private float _chromaticUntil;
        private float _chromaticDuration;

        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int WhiteId = Shader.PropertyToID("_White");
        private static readonly int ChromaticId = Shader.PropertyToID("_Chromatic");
        private static readonly int ChromaticRadialId = Shader.PropertyToID("_ChromaticRadial");

        /// <summary>Short, bounded screen-space colour split for local ultimate impact.</summary>
        public void PulseChromatic(float strength, float duration)
        {
            if (Time.unscaledTime >= _chromaticUntil) _chromaticPeak = 0.0f;
            _chromaticPeak = Mathf.Max(_chromaticPeak, Mathf.Clamp01(strength));
            _chromaticDuration = Mathf.Max(0.05f, duration);
            _chromaticUntil = Mathf.Max(_chromaticUntil, Time.unscaledTime + _chromaticDuration);
        }

        private float CurrentChromatic
        {
            get
            {
                float left = _chromaticUntil - Time.unscaledTime;
                if (left <= 0.0f) return 0.0f;
                return _chromaticPeak * Mathf.Clamp01(left / Mathf.Max(_chromaticDuration, 0.05f));
            }
        }

        // ------------------------------------------------------- § THE PERSISTENT COLOUR SPLIT
        //
        // ⚠️⚠️ THE STYLE'S SPLIT IS ADDED TO THE IMPACT'S, IT DOES NOT REPLACE IT, AND A `Max`
        // WOULD HAVE BEEN THE OBVIOUS WRONG ANSWER. `Settings.RenderStyles`'s Chromatic row holds
        // a base of 0.25 on for the whole match. The transient peaks it has to coexist with are
        // `HitFeel.ChromaticPeak`, which is 0.10 / 0.22 / 0.35 / 0.55 by hit weight, and
        // `HeroAbilitySystem`'s ultimate at 0.95.
        //
        // Under `Mathf.Max` the two lightest hits are SWALLOWED: max(0.25, 0.10) is 0.25, which is
        // the frame not moving. The whole job of that pulse is to tell the player they were hit,
        // and in Chromatic mode it would fire and show nothing. Under a sum every hit moves the
        // frame by its own full peak whatever the base is, so the impact keeps the exact amplitude
        // it was tuned at: 0.25 + 0.55 is 0.80 and still inside the range, and only an ultimate
        // (0.25 + 0.95) saturates, which costs nothing because 0.95 was already within five per
        // cent of the top.
        //
        // ⚠️ IT IS READ OFF A STATIC RATHER THAN OFF `SettingsStore.Current`, for the reason
        // `AntiAliasModes.FxaaActive` records: this is inside `OnRenderImage`, on every camera,
        // every frame, and `SettingsStore.Current` loads and validates the whole settings file the
        // first time it is touched.
        //
        // ⚠️ AND IT IS ZERO IN THE DEFAULT STYLE, so `IsIdentity` below still returns true on a
        // map that grades nothing and the pass is still skipped outright there. Chromatic mode
        // un-skips that existing blit; it adds no new pass.

        private float EffectiveChromatic =>
            Mathf.Clamp01(Settings.RenderStyles.PersistentChromatic + CurrentChromatic);

        // ------------------------------------------------------------------ the event grade
        //
        // ⚠️⚠️ IT MULTIPLIES THE MAP'S GRADE RATHER THAN REPLACING IT, AND THAT IS THE WHOLE
        // REASON IT IS A SECOND PAIR OF FIELDS INSTEAD OF A CALL TO `Set`. `AdoptFromScene`
        // writes the numbers off the loaded map's `MapGrade`, and Eskinita's saturation of 1.18
        // is a real part of how that street looks. A `Visual.SkyEvent` that called `Set` to
        // desaturate for five seconds would have to remember and restore five values, and would
        // silently overwrite a map that had been regraded in the meantime. A multiplier composes
        // with whatever the map says and returns to a clean 1.0.
        //
        // ⚠️ AND IT IS DRIVEN FROM OUTSIDE RATHER THAN TICKED HERE. The event owns the curve; it
        // already has to blend ambient, fog, the sun and the skybox on the same clock, and a
        // second opinion about how far through it is would show up as the frame and the world
        // disagreeing for a frame or two at each end.

        private float _eventBrightness = 1.0f;
        private float _eventSaturation = 1.0f;

        /// <summary>
        /// A whole-frame multiplier for the length of one <see cref="SkyEvent"/>. 1, 1 is off.
        /// </summary>
        public void SetEventGrade(float brightness, float saturation)
        {
            _eventBrightness = Mathf.Clamp(brightness, 0.15f, 1.0f);
            _eventSaturation = Mathf.Clamp(saturation, 0.0f, 1.0f);
        }

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

        /// <summary>
        /// ⚠️⚠️ THE CAMERA HAS TO BE HDR OR THE TONEMAP BELOW IS DECORATION, AND THIS IS THE
        /// MISSING HALF OF EVERY "the game is washed out" PASS THIS PROJECT HAS DONE.
        ///
        /// The reasoning that moved the ACES curve out of `Toon.shader` and onto this camera was
        /// right, and `TscnImporter` states the conclusion it licensed: *"an ambient over 1 rolls
        /// off instead of clipping, so the compensation is no longer needed"*. That is only true
        /// of a floating-point frame. `OnRenderImage` receives the camera's target, and on an LDR
        /// target every channel was CLAMPED TO 1.0 when the surface shader wrote it — so the
        /// sky, Eskinita's (1.02, 0.96, 0.86) ambient and every lit face arrive here already flat
        /// white, and a roll-off curve applied to a flat white image returns a flat white image.
        ///
        /// Measured on `Logs/shots-play/*.png` from this build: the sky is 255,255,255 across the
        /// whole upper frame and the street and the cast sit within a few percent of each other,
        /// against a Godot reference of the same geometry that is warm and separated. 🧑, three
        /// times now: *"the characters are all light as frick, same with map and game overall
        /// ... this is a reoccuring problem"*.
        ///
        /// ⚠️ SO THE FIX IS NOT MORE GRADING AND NOT LESS AMBIENT. An earlier pass darkened the
        /// ambient to hide this and was correctly reverted; darkening the input to a broken
        /// roll-off buys back the highlights by throwing away the midtones. The ambient is the
        /// number Godot actually uses. What was missing is somewhere for it to go.
        ///
        /// ⚠️ SET HERE RATHER THAN AT EACH CAMERA, because "grades" and "needs headroom to grade"
        /// are one requirement, and there are five cameras in this game that mount this component
        /// (the match rig, the spectator, the character portrait, the map preview and the editor
        /// benches). A sixth added later gets it for free instead of being the one that forgets.
        ///
        /// ⚠️⚠️ AND `allowMSAA` IS ASSERTED BESIDE IT, WHICH IS AN ASSERTION RATHER THAN A FIX.
        /// Every camera serialised into a scene in this project already reads `m_AllowMSAA: 1`
        /// (checked across all 27 converted scenes), and a camera created with `AddComponent`
        /// defaults to true, so nothing today is turning it off. What this covers is the pairing:
        /// an image effect is the thing that forces a camera off the backbuffer and into an
        /// intermediate render target, and `Camera.allowMSAA` is the flag that decides whether
        /// that intermediate is allowed to be multisampled. The component that creates the
        /// requirement is the right place to state it, exactly as `allowHDR` is above, and the
        /// alternative is a camera added in a later session that grades correctly and quietly
        /// renders without anti-aliasing.
        ///
        /// ⚠️ THE HDR LINE ABOVE IS ALSO THE RISK TO IT, AND THAT IS WORTH SAYING PLAINLY. An
        /// HDR intermediate is a floating-point target; a multisampled floating-point target is
        /// a heavier allocation and the engine is free to decline it. Whether MSAA survives this
        /// camera stack is therefore not knowable from the settings, which is why
        /// <see cref="PostAntiAlias.ReportOnce"/> reads the sample count off the target that was
        /// actually delivered and why every anti-aliasing mode above Off also carries FXAA.
        /// </summary>
        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_camera == null) return;

            _camera.allowHDR = true;
            _camera.allowMSAA = true;
        }

        private Camera _camera;

        private bool IsIdentity =>
            Mathf.Approximately(_brightness * _eventBrightness, 1.0f)
            && Mathf.Approximately(_contrast, 1.0f)
            && Mathf.Approximately(_saturation * _eventSaturation, 1.0f)
            && _exposure <= 0.0f
            && EffectiveChromatic <= 0.0f;

        /// <summary>
        /// ⚠️ A NO-OP GRADE STILL COSTS A FULL-SCREEN BLIT, so it is skipped outright. This runs
        /// on the match camera every frame and Bayan Plaza grades nothing.
        ///
        /// ⚠️ THE ANTI-ALIASING REPORT IS TAKEN HERE AND NOT IN `PostAntiAlias`, EVEN THOUGH IT
        /// BELONGS TO THAT FILE. Unity runs image effects in component order and this one is
        /// added first on both gameplay cameras, so `source` here IS the target the camera
        /// rendered into. By the time the chain reaches the filter, `source` is an intermediate
        /// the engine allocated to pass one effect's output to the next and its sample count
        /// says nothing about the rasteriser. The call latches after the first screen camera, so
        /// putting it ahead of the identity return costs nothing on the frames after that.
        /// </summary>
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            PostAntiAlias.ReportOnce(_camera, source);

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

            _material.SetFloat(BrightnessId, _brightness * _eventBrightness);
            _material.SetFloat(ContrastId, _contrast);
            _material.SetFloat(SaturationId, _saturation * _eventSaturation);
            _material.SetFloat(ExposureId, _exposure);
            _material.SetFloat(WhiteId, _white);
            _material.SetFloat(ChromaticId, EffectiveChromatic);

            // ⚠️ THE SHAPE OF THE SPLIT IS THE STYLE'S, NOT THE PULSE'S, so a hit taken in
            // Chromatic mode fringes radially like everything else in that frame: they are the
            // same lens. In the default Toon style this writes 0 and the shader takes the same
            // flat horizontal branch it always has, so the shipped impact effect is unchanged.
            _material.SetFloat(ChromaticRadialId,
                               Settings.RenderStyles.RadialSplit ? 1.0f : 0.0f);

            Graphics.Blit(source, destination, _material);
        }

        private void OnDestroy()
        {
            if (_material != null) DestroyImmediate(_material);
        }
    }
}
