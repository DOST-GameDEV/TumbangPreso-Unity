using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A full-world ink outline, as a screen-space pass over the camera's depth and normals.
    /// PROTOTYPE. Off by default, and it must stay off until it is measured.
    ///
    /// ⚠️⚠️ A WORLD OUTLINE ALREADY SHIPPED ONCE AND WAS REVERTED ON 2026-07-29. Do not treat
    /// this as new ground. The full history lives on <see cref="EnvColourPass"/> and is repeated
    /// on <see cref="ToonSkin"/>: Phase 8 put the whole map on the toon shader with an
    /// inverted-hull border, it was played, and it came back as *"the current shaders look
    /// terrible and are causing severe lag on other PCs. The toon shader is creating ugly,
    /// horizontal banded shadows."*
    ///
    /// Two faults, both structural rather than tuning:
    ///
    ///  1. **BANDING**, from a two-band toon ramp stepping across large flat surfaces.
    ///  2. **COST**, from an inverted hull on every mesh in a dressed street. Eskinita is
    ///     roughly 450 renderers, so that is 450 extra draw calls and their vertex work.
    ///
    /// ⚠️ A SCREEN-SPACE PASS AVOIDS BOTH BY CONSTRUCTION, WHICH IS THE ONLY REASON THIS RETRY
    /// IS WORTH DOING RATHER THAN A SECOND MISTAKE.
    ///
    ///  * It applies **no toon lighting to the world at all**. The world keeps its plainly lit
    ///    materials exactly as `EnvColourPass` leaves them, and this pass only draws an edge
    ///    overlay. There is no ramp anywhere in it, so fault 1 has no mechanism to recur.
    ///  * It needs **no per-mesh hull**. One full-screen pass finds every edge in the frame, so
    ///    the cost stops scaling with how dressed the street is. Fault 2 is bounded differently.
    ///
    /// ⚠️ IT IS STILL NOT FREE, AND THE HONEST NUMBER MATTERS MORE THAN THE FEATURE.
    /// `DepthTextureMode.DepthNormals` in the built-in pipeline is a real depth-normals PREPASS:
    /// one extra rasterisation of the whole opaque scene through a replacement shader, plus the
    /// full-screen pass itself. So the trade is "one extra scene pass, no extra per-mesh draw"
    /// against "one extra draw per mesh". On a dressed street the prepass wins; on a bare one it
    /// loses. Nobody has measured either on the machines that reported the lag, which is exactly
    /// why <see cref="_prototypeEnabled"/> defaults to false.
    /// </summary>
    /// <remarks>
    /// § WHY THIS IS AN `OnRenderImage` PASS AND NOT A RENDERER FEATURE.
    ///
    /// ⚠️ THE PROJECT IS ON THE BUILT-IN RENDER PIPELINE, NOT URP.
    /// `ProjectSettings/GraphicsSettings.asset` carries `m_CustomRenderPipeline: {fileID: 0}`,
    /// so there is no `ScriptableRenderer` and no `ScriptableRendererFeature` to hang this off.
    /// `Graphics.Blit` from `OnRenderImage` is the whole toolbox. <see cref="ColourGrade"/> is
    /// the working precedent in this repo and this follows its shape deliberately: the same
    /// `Shader.Find` with the same stripping note, the same lazily built hidden material, the
    /// same pass-through when the effect is a no-op.
    /// </remarks>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class WorldOutline : MonoBehaviour
    {
        /// <summary>
        /// How the pass handles surfaces that already carry an inverted-hull border.
        /// See the long note on <see cref="_exclusion"/> for which of these to use and why.
        /// </summary>
        public enum Exclusion
        {
            /// <summary>Outline everything, cast included. Cheapest, and wrong. See the note.</summary>
            Overlap = 0,

            /// <summary>Anything already rendering on `TumbangPreso/Toon` is masked out.</summary>
            ToonSurfaces = 1,

            /// <summary>Anything on <see cref="_excludedLayers"/> is masked out.</summary>
            Layers = 2,
        }

        // ------------------------------------------------------------------ the toggle
        //
        // ⚠️⚠️ OFF BY DEFAULT, AND THE NAME SAYS PROTOTYPE ON PURPOSE. This is a retry of a
        // feature that shipped, was played and was reverted. A prototype that quietly becomes
        // the look because it happened to be enabled on one camera in one scene is the failure
        // mode to guard against here, not a rendering bug. Nothing in the game turns this on:
        // it has to be switched on by hand, or by a probe that says in its own log that it did.

        [Header("Prototype")]
        [SerializeField] private bool _prototypeEnabled;

        [Header("Ink")]
        [SerializeField] private Color _colour = new Color(0.0156863f, 0.0313725f, 0.219608f, 1.0f);
        [SerializeField, Range(0.0f, 1.0f)] private float _opacity = 1.0f;

        // ------------------------------------------------------------------ § THE FAR PLANE
        //
        // ⚠️⚠️ THE PRECISION OF THIS WHOLE FEATURE IS SET BY A NUMBER THAT HAS NOTHING TO DO
        // WITH IT, AND THAT IS THE FIRST THING TO CHANGE IF THE EDGES COME OUT SPECKLED.
        //
        // The built-in pipeline's `_CameraDepthNormalsTexture` is one 32-bit texel per pixel:
        // two 8-bit channels of depth and two of normal. So depth is 65,536 steps spread EVENLY
        // over the frustum, and the step size is far/65536 at every distance. `CameraRig` never
        // sets `farClipPlane`, so the match camera runs Unity's default of 1000 m and the step
        // is 15 mm. Worked through against `_depthBias`'s default of 0.035, that quantisation
        // contributes about 0.003 to the relative depth difference at 10 m, 0.03 at 1 m, and
        // 0.06 at half a metre. In other words the deadzone covers it comfortably at arm's
        // length and further, and runs out when the camera is pressed against a surface.
        //
        // ⚠️ THE FIX IS NOT A BIGGER DEADZONE, IT IS A SHORTER FRUSTUM. Both arenas are tens of
        // metres across: `SpectatorCamera` sets its own far plane, `MapPreview` uses 400, and
        // every probe in this repo picks something between 40 and 260. A match camera at 1000 m
        // is spending 95 percent of a 16-bit depth range on empty space beyond the map. Dropping
        // it to 200 divides every number above by five and costs nothing else. That is a change
        // to `CameraRig`, which this prototype has no business making, so it is recorded here.
        //
        // ⚠️ THE NORMALS HAVE THEIR OWN LIMIT AND NO EQUIVALENT FIX. They are packed by a
        // spheremap transform into two 8-bit channels, which is roughly a degree of angular
        // resolution and worse for normals pointing away from the camera. So the normals term
        // finds a wall corner easily and finds a SHALLOW crease unreliably: a fold of a couple
        // of degrees sits inside the packing error, and pushing `_normalSensitivity` up far
        // enough to catch it starts painting noise on curved surfaces instead. A G-buffer would
        // not have this problem. Built-in forward rendering does, and saying so is more useful
        // than tuning around it.

        [Header("Edge")]
        [SerializeField, Range(0.5f, 4.0f)] private float _thickness = 1.0f;
        [SerializeField, Range(0.0f, 200.0f)] private float _depthSensitivity = 40.0f;
        [SerializeField, Range(0.0f, 0.25f)] private float _depthBias = 0.035f;
        [SerializeField, Range(0.0f, 8.0f)] private float _normalSensitivity = 1.6f;
        [SerializeField, Range(0.0f, 1.0f)] private float _normalBias = 0.18f;

        // A negative value adopts `RenderSettings.fogStartDistance` and `fogEndDistance`.
        [Header("Distance")]
        [SerializeField] private float _fadeStart = -1.0f;
        [SerializeField] private float _fadeEnd = -1.0f;

        [Header("Exclusion")]
        [SerializeField] private Exclusion _exclusion = Exclusion.ToonSurfaces;
        [SerializeField] private LayerMask _excludedLayers;
        [SerializeField, Range(0.0f, 1.0f)] private float _maskStrength = 1.0f;
        [SerializeField] private float _maskDepthTolerance = 0.0008f;
        [SerializeField] private float _rescanSeconds = 1.0f;

        /// <summary>The whole feature, in one switch. Nothing in the game sets this true.</summary>
        public bool PrototypeEnabled
        {
            get => _prototypeEnabled;
            set => _prototypeEnabled = value;
        }

        public void SetInk(Color colour, float opacity)
        {
            _colour = colour;
            _opacity = Mathf.Clamp01(opacity);
        }

        public void SetEdge(float thickness, float depthSensitivity, float normalSensitivity)
        {
            _thickness = Mathf.Clamp(thickness, 0.5f, 4.0f);
            _depthSensitivity = Mathf.Max(0.0f, depthSensitivity);
            _normalSensitivity = Mathf.Max(0.0f, normalSensitivity);
        }

        public void SetExclusion(Exclusion mode, LayerMask layers)
        {
            _exclusion = mode;
            _excludedLayers = layers;
            _nextScan = 0.0f;
        }

        // ------------------------------------------------------------------

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int DepthSensitivityId = Shader.PropertyToID("_DepthSensitivity");
        private static readonly int DepthBiasId = Shader.PropertyToID("_DepthBias");
        private static readonly int NormalSensitivityId = Shader.PropertyToID("_NormalSensitivity");
        private static readonly int NormalBiasId = Shader.PropertyToID("_NormalBias");
        private static readonly int FadeStartId = Shader.PropertyToID("_FadeStart");
        private static readonly int FadeEndId = Shader.PropertyToID("_FadeEnd");
        private static readonly int MaskId = Shader.PropertyToID("_WorldOutlineMask");
        private static readonly int MaskStrengthId = Shader.PropertyToID("_MaskStrength");
        private static readonly int MaskToleranceId = Shader.PropertyToID("_MaskDepthTolerance");
        private static readonly int ViewRayId = Shader.PropertyToID("_ViewRay");

        /// <summary>Pass 0 in `WorldOutline.shader`. See the note there: blitting without a pass
        /// index runs the mask pass over the whole frame and paints it white.</summary>
        private const int CompositePass = 0;

        private const int MaskPass = 1;

        private const string ToonShaderName = "TumbangPreso/Toon";

        private Camera _camera;
        private Material _material;
        private Material _maskMaterial;
        private RenderTexture _mask;
        private CommandBuffer _maskBuffer;
        private bool _bufferAttached;
        private bool _depthNormalsRequested;
        private bool _missing;

        private readonly List<Renderer> _excluded = new List<Renderer>();
        private float _nextScan;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>
        /// § THE DEPTH-NORMALS PREPASS, AND PUTTING IT BACK WHEN THIS IS SWITCHED OFF.
        ///
        /// ⚠️⚠️ THE FLAG IS OR-ED IN AND CLEARED ONLY IF THIS COMPONENT WAS THE ONE THAT SET IT.
        /// `Camera.depthTextureMode` is a shared bitfield with no owner, so assigning it outright
        /// would silently switch off anything else that had asked for depth, and clearing it
        /// unconditionally on disable would do the same thing one frame later. Nothing else in
        /// this project asks for it TODAY, which is precisely why a future second caller would
        /// break in a way nobody could reproduce.
        ///
        /// ⚠️ AND IT IS RECONCILED EVERY FRAME RATHER THAN ONLY IN OnEnable, because the toggle
        /// is a serialized field somebody flips in the inspector while the game is running. That
        /// is the whole point of it being a prototype knob.
        /// </summary>
        private void RequestDepthNormals(bool want)
        {
            if (_camera == null || want == _depthNormalsRequested) return;

            if (want) _camera.depthTextureMode |= DepthTextureMode.DepthNormals;
            else _camera.depthTextureMode &= ~DepthTextureMode.DepthNormals;

            _depthNormalsRequested = want;
        }

        private bool Live => _prototypeEnabled && !_missing && _opacity > 0.0f;

        private void LateUpdate() => RequestDepthNormals(Live);

        private void OnDisable()
        {
            RequestDepthNormals(false);
            DetachBuffer();
        }

        private void OnDestroy()
        {
            DetachBuffer();

            if (_material != null) DestroyImmediate(_material);
            if (_maskMaterial != null) DestroyImmediate(_maskMaterial);

            if (_mask != null)
            {
                _mask.Release();
                DestroyImmediate(_mask);
            }
        }

        // ------------------------------------------------------------------ the exclusion mask
        //
        // ⚠️⚠️ § THE DOUBLE-OUTLINE PROBLEM, WHICH IS THE REAL DESIGN QUESTION IN THIS FEATURE.
        //
        // Every character, both hero props and the first-person arms already carry an ink border
        // from `Toon.shader`'s OUTLINE pass. A world pass that knows nothing about them draws a
        // second border on the same silhouettes. Three things were considered:
        //
        //  A. **ACCEPT THE OVERLAP.** Cheapest, and it is worse than it first sounds. Work out
        //     where each border actually lands. The depth-normals prepass renders the object
        //     through a replacement shader chosen by the SubShader's `RenderType` tag, so the
        //     hull Pass is NOT in it: the prepass records the character's TRUE silhouette, while
        //     the hull paints a band just OUTSIDE that silhouette. A Roberts cross therefore
        //     fires straddling the true silhouette, half of it landing on ink the hull already
        //     drew and half of it landing inside the character. The visible result is a border
        //     thickened INWARD, in screen space, on top of a hull border that is measured in
        //     WORLD space. Those two disagree with distance by construction: at two metres the
        //     hull is fat and the screen line adds a rim, at thirty metres the hull has shrunk
        //     under a pixel and the screen line is the whole border. The cast's edge weight would
        //     change as they run away from you.
        //
        //     ⚠️ AND THE SILHOUETTE IS NOT THE WORST OF IT. The normals term draws CREASES, which
        //     is exactly what it is for on a building. On a character it puts lines down the
        //     elbows, across the chest, and along every fold the mesh happens to have. Godot
        //     draws no such lines: a Person is a two-band face inside one ink silhouette, and
        //     that is the look the art was signed off against. Worse, those creases are computed
        //     from `_CameraDepthNormalsTexture`, whose normals are two 8-bit channels through a
        //     spheremap transform, so a crease that sits right on the threshold flickers between
        //     frames as the limb swings. A shimmering line across a running character is the kind
        //     of thing that gets reported as "the shaders look terrible" in exactly those words.
        //
        //  B. **A LAYER MASK.** Rejected as the DEFAULT, kept as an option. `TagManager.asset`
        //     defines no custom layers at all: the whole game is on Default, and the only
        //     layer work in the project is `ModelPreview` and `MapPreviewSurface` re-layering
        //     whole subtrees to isolate a preview camera. Introducing a Characters layer means
        //     touching every spawn path, and it collides head-on with those two, which overwrite
        //     `gameObject.layer` across the subtree and would have to learn to put it back.
        //     Layer bits are also load-bearing for physics queries in this project. That is a lot
        //     of blast radius for a prototype, so <see cref="Exclusion.Layers"/> exists for
        //     whoever wants it later and is not what this ships pointing at.
        //
        //  C. **A STENCIL.** The technically neatest answer, and it is closed off right now: the
        //     hull and lit passes in `Toon.shader` would have to write a stencil reference, and
        //     that file is owned elsewhere this cycle. Worth revisiting if this feature survives
        //     measurement, because it costs nothing per frame and needs no second renderer walk.
        //
        // ⚠️ SO THE RECOMMENDATION IS D, WHICH IS B'S MECHANISM WITHOUT B'S BLAST RADIUS: MASK BY
        // THE SHADER THE SURFACE IS ALREADY ON. "Carries `TumbangPreso/Toon`" and "already has a
        // hull outline" are the same set, by definition rather than by bookkeeping, and the
        // project states the invariant that makes it safe out loud in `ColourGrade.shader`:
        // *"the world is deliberately not on the toon shader"*. So the query needs no new layer,
        // no new tag, no change to any spawn path, and it cannot drift out of date the way a
        // hand-maintained list would. It is the same style of derivation the match rules use for
        // the taya, where the role is computed rather than accumulated.
        //
        // ⚠️ THE SET IS FOUND BY SCANNING RATHER THAN BY REGISTRATION, AND THAT IS THE ONE PART
        // OF THIS THAT IS PROTOTYPE-GRADE. `ToonSkin.Apply` is the single funnel every outlined
        // surface in the game passes through, so the shipping version of this should have the
        // renderer register itself there and this walk should disappear. That file is owned
        // elsewhere this cycle, so a rescan on a timer stands in. It is one
        // `FindObjectsByType<Renderer>` per second over roughly 450 renderers, which is cheap
        // enough to prototype behind and too rude to ship.

        private void Rescan()
        {
            _excluded.Clear();

            if (_exclusion == Exclusion.Overlap) return;

            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || !renderer.enabled) continue;

                bool wanted = _exclusion == Exclusion.Layers
                    ? (_excludedLayers.value & (1 << renderer.gameObject.layer)) != 0
                    : IsToonSurface(renderer);

                if (wanted) _excluded.Add(renderer);
            }
        }

        private static bool IsToonSurface(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null) return false;

            foreach (var material in materials)
            {
                if (material == null || material.shader == null) continue;
                if (material.shader.name == ToonShaderName) return true;
            }

            return false;
        }

        /// <summary>
        /// ⚠️ REFILLED IN OnPreRender AND ATTACHED ONCE, rather than added and removed per frame.
        /// A `CommandBuffer` the camera holds a reference to can be cleared and rewritten in
        /// place, and the add/remove pair is the half of this API that leaks: a component
        /// disabled between the two leaves a buffer executing against a dead material for the
        /// life of the camera.
        ///
        /// ⚠️ AT `BeforeImageEffectsOpaque`, WHICH IS BOTH LATE ENOUGH AND PATH-AGNOSTIC. The
        /// depth-normals prepass has run by then, so `_CameraDepthNormalsTexture` is bound for
        /// the mask pass's own occlusion test, and the event exists in forward and deferred
        /// alike. `AfterForwardOpaque` would have quietly stopped firing if any camera in this
        /// game were ever switched to deferred.
        /// </summary>
        private void OnPreRender()
        {
            // ⚠️⚠️ THE DEPTH-NORMALS REQUEST IS REPEATED HERE BECAUSE `LateUpdate` DOES NOT RUN IN
            // EDIT MODE, AND WITHOUT THIS THE WHOLE PASS IS A SILENT NO-OP IN EVERY PROBE. Unity
            // does not tick `Update`/`LateUpdate` on a component outside play mode, but it DOES
            // call `OnPreRender` and `OnRenderImage` for `Camera.Render()`. So an edit-mode
            // capture reached the compositing code with `_CameraDepthNormalsTexture` never
            // requested and therefore never generated: the edge test sampled an empty texture,
            // found no edges, and blitted the frame through unchanged.
            //
            // ⚠️ MEASURED, NOT REASONED. `WorldOutlineProbe` rendered the same angle with the
            // prototype off and on and the two PNGs came back visually identical, differing by
            // about 30 bytes of compression noise. That is what sent me here. Requesting it from
            // the callback that actually runs makes the pass work in a probe as well as in play,
            // which is the difference between a feature that can be judged and one that cannot.
            //
            // `LateUpdate` is kept: it is what CLEARS the request when the prototype is switched
            // off at runtime, and clearing on the render callback would fight the frame it is
            // drawing.
            RequestDepthNormals(Live);

            if (!Live || _exclusion == Exclusion.Overlap)
            {
                DetachBuffer();
                return;
            }

            if (!EnsureMaterials())
            {
                DetachBuffer();
                return;
            }

            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + Mathf.Max(0.1f, _rescanSeconds);
                Rescan();
            }

            EnsureMaskTexture();

            if (_maskBuffer == null)
                _maskBuffer = new CommandBuffer { name = "World Outline Exclusion Mask" };

            _maskBuffer.Clear();
            _maskBuffer.SetRenderTarget(_mask);
            _maskBuffer.ClearRenderTarget(false, true, Color.black);

            _maskMaterial.SetFloat(MaskToleranceId, _maskDepthTolerance);

            foreach (var renderer in _excluded)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                var materials = renderer.sharedMaterials;
                int submeshes = materials == null || materials.Length == 0 ? 1 : materials.Length;

                for (int i = 0; i < submeshes; i++)
                    _maskBuffer.DrawRenderer(renderer, _maskMaterial, i, MaskPass);
            }

            // ⚠️ PUT THE CAMERA'S TARGET BACK. A command buffer leaves whatever it last bound
            // bound, and this one binds a one-channel mask texture. Everything the camera draws
            // after this event, the image effect chain included, would land in it. This is the
            // single most common way a `CameraEvent` buffer breaks a frame, and the symptom is
            // a black screen rather than a wrong outline, so it is worth one line to rule out.
            _maskBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

            if (!_bufferAttached)
            {
                _camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, _maskBuffer);
                _bufferAttached = true;
            }
        }

        private void DetachBuffer()
        {
            if (!_bufferAttached || _camera == null || _maskBuffer == null) return;

            _camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, _maskBuffer);
            _bufferAttached = false;
        }

        private void EnsureMaskTexture()
        {
            int width = Mathf.Max(1, _camera.pixelWidth);
            int height = Mathf.Max(1, _camera.pixelHeight);

            if (_mask != null && _mask.width == width && _mask.height == height) return;

            if (_mask != null)
            {
                _mask.Release();
                DestroyImmediate(_mask);
            }

            // ⚠️ ONE CHANNEL, NO DEPTH BUFFER, NO MSAA. The mask carries a single 0-or-1 coverage
            // value, the occlusion test happens in the fragment against the prepass rather than
            // against an attached depth buffer (see the shader), and a multisampled mask would
            // have to agree with the camera's own sample count, which is somebody else's setting.
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;

            _mask = new RenderTexture(width, height, 0, format)
            {
                name = "WorldOutlineMask",
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
                hideFlags = HideFlags.HideAndDontSave,
            };

            _mask.Create();
        }

        private bool EnsureMaterials()
        {
            if (_material != null && _maskMaterial != null) return true;

            // ⚠️ THE SHADER IS NOT IN `GameBuilder.EnsureRuntimeShaders` AND THAT IS DELIBERATE.
            // Everything reached only by `Shader.Find` is stripped from a player build, which is
            // the rule that file exists to enforce, and adding this one would ship a prototype's
            // shader variants into every build of a feature that is switched off. So the miss
            // path here names the fix instead of merely warning: if this outline is ever wanted
            // in a BUILD rather than in the editor, add "TumbangPreso/WorldOutline" to the
            // `wanted` array in `Assets/TumbangPreso/Editor/GameBuilder.cs`.
            var shader = Shader.Find("TumbangPreso/WorldOutline");

            if (shader == null)
            {
                Debug.LogError(
                    "[WorldOutline] TumbangPreso/WorldOutline was not found. In a player build " +
                    "that means it was stripped: a shader only Shader.Find reaches is dropped " +
                    "unless it is listed in GameBuilder.EnsureRuntimeShaders. The prototype is " +
                    "off for this camera.");

                _missing = true;
                RequestDepthNormals(false);
                return false;
            }

            if (_material == null)
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            // ⚠️ A SECOND MATERIAL INSTANCE ON THE SAME SHADER. The two passes are written into
            // one shader file so there is only one thing to find and one thing to strip, but they
            // are used at different points in the frame: the mask is drawn per renderer during
            // camera rendering and the composite is blitted afterwards. Two instances means
            // neither pass has to reason about what the other left set on the shared uniforms,
            // which is the sort of coupling that shows up as the mask flickering on alternate
            // frames rather than as an error anybody can search for.
            if (_maskMaterial == null)
                _maskMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            return true;
        }

        /// <summary>
        /// § WHY THIS RUNS BEFORE <see cref="ColourGrade"/>, AND HOW THAT ORDER IS GUARANTEED.
        ///
        /// ⚠️⚠️ THE INK HAS TO GO THROUGH THE SAME GRADE THE HULL'S INK GOES THROUGH, OR THE TWO
        /// BORDERS IN ONE FRAME ARE TWO DIFFERENT COLOURS. This is not a matter of taste and it
        /// is not close.
        ///
        /// The hull border is geometry: `Toon.shader`'s OUTLINE pass writes `_OutlineColor` into
        /// the frame like any other surface, so `ColourGrade` tonemaps and grades it along with
        /// everything else. Eskinita runs exposure 0.92, contrast 1.03 and saturation 1.18, and
        /// the ACES curve is steepest exactly where a near-black navy sits. Run this pass AFTER
        /// the grade and the world's ink is the raw swatch while the cast's ink is the graded
        /// one, in the same picture, on adjacent pixels. Run it BEFORE and the two are the same
        /// number going into the same curve, so they are the same number coming out. Exactly,
        /// not approximately.
        ///
        /// ⚠️ AND `SkyEvent` MAKES IT MOVE. `ColourGrade.SetEventGrade` multiplies brightness
        /// down as far as 0.15 for the length of an event. The cast's border darkens with the
        /// world during one whether anybody planned it or not, so a world border that did not
        /// would separate from it for those five seconds and then rejoin.
        ///
        /// ⚠️ THE EDGE DETECTION ITSELF DOES NOT CARE, which is what makes the choice clean. The
        /// Roberts cross reads depth and normals, never colour, so no grade can shift where a
        /// line lands. Only the ink's final colour is at stake, and that argument runs one way.
        ///
        /// ⚠️⚠️ THE ORDER IS ENFORCED BY `[ImageEffectOpaque]`, NOT BY COMPONENT ORDER, AND THAT
        /// IS THE LOAD-BEARING PART. Unity runs image effects on one camera in COMPONENT ORDER,
        /// and `ColourGrade` is added from code in three different places at three different
        /// times (`CameraRig.Awake`, `SpectatorCamera`, `MapPreviewSurface`), so which of the two
        /// components ends up first on a given camera is an accident of who called AddComponent
        /// first. `[ImageEffectOpaque]` takes it out of the question: an opaque-tagged effect
        /// runs after opaque geometry and before transparents, which is unconditionally before
        /// every untagged effect on the same camera.
        ///
        /// ⚠️ AND THAT SLOT IS INDEPENDENTLY THE RIGHT ONE. The depth-normals prepass only
        /// contains opaque geometry, so an edge detector has nothing to gain by waiting for the
        /// transparents, and quite a lot to lose: running after them would ink a hard line across
        /// smoke and impact bursts using the depth of whatever is behind them. Compositing before
        /// them means the VFX draw over the ink, which is what they already do over the hull.
        /// </summary>
        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // ⚠️ SWITCHED OFF STILL COSTS ONE PASS-THROUGH BLIT, exactly as an identity
            // `ColourGrade` does and for the same reason: a camera carrying an `OnRenderImage`
            // component renders to an intermediate target whether the effect does anything or
            // not. So the way to pay nothing for this prototype is to REMOVE the component from
            // the camera, not to leave it on the camera with the toggle off. Nothing in the game
            // adds it, so no camera pays this unless somebody put it there.
            if (!Live || !EnsureMaterials())
            {
                Graphics.Blit(source, destination);
                return;
            }

            _material.SetColor(OutlineColorId, _colour);
            _material.SetFloat(OpacityId, _opacity);
            _material.SetFloat(ThicknessId, _thickness);
            _material.SetFloat(DepthSensitivityId, _depthSensitivity);
            _material.SetFloat(DepthBiasId, _depthBias);
            _material.SetFloat(NormalSensitivityId, _normalSensitivity);
            _material.SetFloat(NormalBiasId, _normalBias);

            ApplyFade();
            ApplyViewRay();

            bool masked = _exclusion != Exclusion.Overlap && _mask != null && _bufferAttached;

            _material.SetTexture(MaskId, masked ? (Texture)_mask : Texture2D.blackTexture);
            _material.SetFloat(MaskStrengthId, masked ? _maskStrength : 0.0f);
            _material.SetTexture(MainTexId, source);

            // ⚠️ THE PASS INDEX IS NOT OPTIONAL. `Graphics.Blit` without one runs EVERY pass in
            // the SubShader, and pass 1 is the exclusion mask: blitted full-screen it writes
            // white over the entire frame. See the note at the top of the shader.
            Graphics.Blit(source, destination, _material, CompositePass);
        }

        /// <summary>
        /// ⚠️ THE FADE DEFAULTS TO THE MAP'S OWN FOG RATHER THAN TO A CONSTANT HERE, for the same
        /// reason `ColourGrade` reads its numbers off `MapGrade` rather than hard-coding
        /// Eskinita's: the two arenas fog differently. Ilalim ng Tulay runs linear fog to 110 m
        /// and Eskinita's starts at 14 m, so one baked pair of distances would be wrong on one of
        /// them. A map with fog switched off falls back to the camera's far plane, which fades
        /// nothing and is the honest answer to "this map has no haze to match".
        /// </summary>
        private void ApplyFade()
        {
            float start = _fadeStart;
            float end = _fadeEnd;

            if (start < 0.0f || end < 0.0f)
            {
                if (RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear)
                {
                    start = RenderSettings.fogStartDistance;
                    end = RenderSettings.fogEndDistance;
                }
                else
                {
                    start = _camera.farClipPlane;
                    end = _camera.farClipPlane;
                }
            }

            _material.SetFloat(FadeStartId, start);
            _material.SetFloat(FadeEndId, Mathf.Max(end, start + 0.001f));
        }

        /// <summary>
        /// The frustum half-extents at unit view depth, so the shader can rebuild the view ray
        /// per pixel for its grazing-angle compensation.
        ///
        /// ⚠️ WRITTEN EVERY FRAME BECAUSE THE FIELD OF VIEW MOVES. `CameraRig.ApplyLens` swaps
        /// between the first-person and third-person lens every frame based on the mode, and it
        /// says in its own comment that it does so rather than on the transition. A cached ray
        /// would be wrong for the whole of every emote.
        /// </summary>
        private void ApplyViewRay()
        {
            float halfHeight = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * _camera.aspect;

            _material.SetVector(ViewRayId, new Vector4(halfWidth, halfHeight, 0.0f, 0.0f));
        }
    }
}
