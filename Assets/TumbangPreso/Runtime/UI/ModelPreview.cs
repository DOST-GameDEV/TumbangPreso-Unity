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
    /// ⚠️⚠️ THE MODEL TURNS, THE CAMERA DOES NOT ORBIT — for the IDLE SWEEP. `character_preview.gd:447`
    /// writes `pivot.rotation.y`, so the subject rotates on its own spot while the camera holds
    /// the tuned three-quarter angle. The PLAYER'S drag is the other way round: `_apply_camera`
    /// adds their yaw to `CAMERA_YAW_DEGREES`, so dragging orbits the camera and leaves the
    /// lighting on the subject where it was. Both are transcribed as they are.
    ///
    /// ⚠️ AND THE IDLE SWEEP YIELDS TO THE PLAYER PERMANENTLY. Once they drag, it stops for
    /// good (`_user_took_over`). Left running, a subject someone is trying to inspect rotates
    /// out from under the angle they just chose, which makes the control feel broken rather
    /// than free. Do not "restore" the sweep on a timer after a drag.
    /// </summary>
    /// <remarks>
    /// ⚠️⚠️ FOUR THINGS WERE WRONG HERE AT ONCE AND THE SCREEN WAS UNUSABLE BECAUSE OF IT.
    /// Reported as *"model isnt movable and its stretched"*, against a shot of a character
    /// filling the frame at arm's length:
    ///
    ///  1. **NOTHING EVER CALLED `Orbit` OR `Zoom`.** Both were written, both were public, and
    ///     no input reached either of them, while the panel printed a hint line telling the
    ///     player to drag, scroll and right-click. Three controls, all of them lies. This is the
    ///     built-and-never-called failure the port keeps repeating.
    ///  2. **THE RENDER TEXTURE WAS A FIXED 512x640** stretched across a panel of a completely
    ///     different shape, so every subject was squashed horizontally. The target now follows
    ///     the rect it is drawn into.
    ///  3. **THE FRAMING IGNORED ASPECT AND FITTED HEIGHT ALONE.** `_frame()` takes the LARGEST
    ///     of three distances and the second and third are the ones a flat slipper and a wide
    ///     T-pose need. Its own header records that fitting on height alone put the camera
    ///     inside the tsinelas.
    ///  4. **NO CLIP WAS PLAYING, SO THE SUBJECT STOOD IN ITS BIND POSE.** A Kenney rig's rest
    ///     pose is arms straight out, which reads as unfinished art and is also nearly twice as
    ///     wide as the real silhouette, which wrecks the framing on top of it.
    ///
    /// ⚠️ THE FOV IS 42, FROM `CharacterSelect.tscn`'s own Camera3D. It was 32 here, which is a
    /// third narrower and pushes the auto-framed distance out by the same ratio.
    /// </remarks>
    public sealed class ModelPreview : MonoBehaviour
    {
        public const float CameraYawDegrees = 18.0f;
        public const float CameraPitchDegrees = 16.0f;

        /// <summary>A lata or a tsinelas lies on the ground and has to be looked DOWN at;
        /// a standing Person does not. `_frame()` lerps between the two on the subject's own
        /// height:width ratio rather than switching on a category.</summary>
        public const float CameraPitchFlatDegrees = 52.0f;

        /// <summary>The Camera3D in `CharacterSelect.tscn`. Godot's `keep_aspect` defaults to
        /// KEEP_HEIGHT, so this is a VERTICAL angle and transcribes straight across.</summary>
        public const float FieldOfView = 42.0f;

        /// <summary>How much room to leave around the model when framing it. Above 1 so the
        /// silhouette never touches the panel edge.</summary>
        public const float FrameMargin = 1.62f;

        /// <summary>Where the camera aims up the model's own height — above centre, because
        /// a portrait framed on the navel reads as a mistake.</summary>
        public const float AimHeightRatio = 0.54f;

        /// <summary>
        /// Pushes the subject to the RIGHT of frame, as a FRACTION OF THE VISIBLE WIDTH at
        /// whatever distance it ended up being framed from. Every control on this screen sits in
        /// the left third, so a centred subject would stand behind them.
        ///
        /// ⚠️ A RATIO, NOT A CONSTANT OFFSET. Godot's `h_offset` is a world-unit translation
        /// along the camera's own X, so its on-screen effect scales with distance: a fixed value
        /// tuned for a Person framed from ~5 units pushed the lata, framed from well under a
        /// metre, off the right edge entirely.
        /// </summary>
        public const float FrameHorizontalOffsetRatio = -0.19f;

        /// <summary>Matches the match's own PERSON_SCALE. The rigs are authored small and every
        /// Person in a match is scaled up by it; previewing at native scale frames a doll.</summary>
        public const float PreviewScale = 2.38f;

        /// <summary>
        /// ⚠️⚠️ THE SUBJECT IS TURNED TO FACE THE CAMERA, AND THIS IS THE Z FLIP AGAIN. Godot
        /// looks down -Z, so a model at rotation zero has its FRONT toward a camera parked on
        /// +Z, which is where `_apply_camera` puts it. Unity looks down +Z, so the same
        /// arrangement photographs the back of the character's head: the screen whose entire job
        /// is "who is this, what do they look like" was showing everybody from behind.
        ///
        /// Turned on the subject rather than on the camera because the camera's framing, its
        /// horizontal offset and its pitch were all measured against that viewpoint, and moving
        /// the viewpoint invalidates all three.
        /// </summary>
        public const float FacingYaw = 180.0f;

        /// <summary>
        /// ⚠️⚠️ THE .tscn's TWO ENERGIES ARE 1.35 AND 0.45 AND THEY ARE SCALED HERE, NOT
        /// RETUNED. One factor keeps the RATIO between key and fill exactly as authored, which is
        /// the part that makes the shot read as lit rather than flat.
        ///
        /// ⚠️ IT WENT TO 1.0 FOR ONE BUILD AND BLEW THE FACE OUT TO NEAR WHITE. The reasoning
        /// was that the tonemap and the ambient had since been built, so the compensation was no
        /// longer needed. Measured against the toon bench, which is the render that reads
        /// correctly: the bench lights a character with ONE key at 1.15 plus its ambient, and
        /// 1.35 + 0.45 unscaled is 1.8 of direct light, 57% more. 0.66 puts the key at 0.891 and
        /// the fill at 0.297, so the direct total lands at 1.19 against the bench's 1.15. That
        /// agreement is why the number is this and not something rounder.
        /// </summary>
        public const float LightExposure = 0.66f;

        /// <summary>
        /// ⚠️⚠️ THE PREVIEW HAD NO AMBIENT OF ITS OWN AND INHERITED THE MENU'S, WHICH IS WHY THE
        /// CAST WENT BLUE. Reported as *"the characters looked blue"*, and measured off the two
        /// renders of the same model: the toon bench gives Berto skin (255,169,101) and a shirt
        /// (112,163,112), while the character screen gave (120,101,102) and (83,209,166). That is
        /// not a lighting nuance, it is a different character.
        ///
        /// The cause is that `TumbangPreso/Toon` is a SURFACE shader with no `noambient`, so
        /// Unity adds `RenderSettings.ambientLight` into the lit pass AFTER the tonemap and
        /// outside the palette remap. On a menu scene that ambient is Unity's default skybox
        /// wash, which is blue-grey, so every Person got a raw blue layer added on top of its
        /// palette. Godot never had this: `CharacterSelect.tscn`'s SubViewport is `own_world_3d`
        /// and carries its own Environment, and this is that Environment's ambient.
        ///
        /// ⚠️ IT IS THE PREVIEW'S 1.35, NOT THE ARENA'S 1.65. `ToonProbe` sets the arena value
        /// because the bench exists to answer "does a character look right in a MATCH". This
        /// screen is a different Environment in the same game and the two are authored apart.
        ///
        /// ⚠️ AND IT IS GLOBAL STATE, WHICH IS THE ONE UGLY PART. `RenderSettings.ambientLight`
        /// has no per-layer form in the built-in pipeline, so isolating by layer (which is what
        /// keeps foreign LIGHTS off the subject) cannot reach it. Written on every Show so a map
        /// load cannot land after it and win, and restored on destroy.
        /// </summary>
        /// <remarks>
        /// ⚠️ THE SKY CONTRIBUTION IS PART OF THE SUM AND LEAVING IT OUT OVEREXPOSED THE FACE.
        /// The Environment reads `ambient_light_energy = 1.35` AND
        /// `ambient_light_sky_contribution = 0.35`, and Godot blends the two sources by that
        /// fraction: only 65% of the ambient comes from the colour, the rest from a sky this
        /// preview does not have. Taking the colour at the full 1.35 put 0.847 of untonemapped
        /// light on every surface and Berto rendered as pale yellow with no skin tone left.
        /// 0.65 of it is 0.5506, which is the number that belongs here.
        /// </remarks>
        public static readonly Color PreviewAmbient =
            new Color(0.62745f, 0.57647f, 0.52157f) * (1.35f * 0.65f);

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

        /// <summary>
        /// ⚠️⚠️ EVERY PREVIEW NEEDS ITS OWN STAGE, AND SHARING ONE PUT FOUR SUBJECTS IN EVERY
        /// TILE. The rig isolates itself by LAYER, which is enough when there is one of it and
        /// wrong the moment there are several: the tutorial's premise strip builds four, they
        /// all parked at `Stage`, and each tile's camera then photographed all four models
        /// stacked inside one another. The strip read as four identical people, two of them
        /// upside down, because the framing was measured against the union of the four.
        ///
        /// Far enough apart that a neighbour is outside a 42-degree frustum from two units
        /// away by three orders of magnitude, which is cheaper than reasoning about it again.
        /// </summary>
        public const float StageSpacing = 200.0f;

        private static int _stageCount;

        /// <summary>The layer the preview camera draws and nothing else does.</summary>
        public const int PreviewLayer = 30;

        private UnityEngine.Camera _camera;
        private RenderTexture _texture;
        private RawImage _surface;
        private RectTransform _panel;
        private Transform _pivot;
        private GameObject _model;

        private float _turnPhase;
        private bool _userTookOver;

        // The measured shot, kept so the player's offsets can be layered on it and so
        // right-click can restore it exactly.
        private float _frameDistance = 4.0f;
        private float _framePitch = CameraPitchDegrees;
        private Vector3 _frameAim = Stage;

        /// <summary>This instance's own patch of nowhere. See StageSpacing.</summary>
        private Vector3 _stage = Stage;
        private float _frameAspect = 1.0f;

        // The player's offsets on top of it. All three survive a subject change on purpose:
        // somebody who found the angle they want to judge slippers from should keep it while
        // cycling slippers rather than re-finding it four times.
        private float _userYaw;
        private float _userPitch;
        private float _userZoom = 1.0f;

        private bool _needsFrame;

        /// <summary>Set by <see cref="SetTileFraming"/>. See that function.</summary>
        private bool _centreSubject;
        private bool _uniformExtent;

        /// <summary>Set by <see cref="EnableTileInteraction"/>. See that function.</summary>
        private bool _wheelZooms = true;

        public bool WheelZooms => _wheelZooms;

        /// <summary>
        /// The three things a test can look at.
        ///
        /// ⚠️ THEY EXIST BECAUSE "the model is not movable and it is stretched" IS INVISIBLE TO
        /// EVERY CHECK THAT IS NOT AN EYE, and an eye is not a regression test. The camera says
        /// whether a drag reached the preview at all, the target says whether the picture is
        /// being squashed onto a rect of another shape, and the subject says whether anything
        /// was instanced to look at. All three shipped broken at once.
        /// </summary>
        public UnityEngine.Camera PreviewCamera => _camera;

        public RenderTexture Target => _texture;

        public GameObject Subject => _model;

        /// <summary>
        /// Subject CENTRED in its box and pulled in to fill it, for a caller showing this rig as
        /// a small picture rather than as a screen backdrop.
        ///
        /// ⚠️ TWO CHANGES, AND BOTH ARE NEEDED. `FrameMargin` leaves 62% air, which is right on
        /// the CHARACTER screen where the figure shares the frame with a wood panel and was
        /// measured against a T-pose. In a 190 px tile that same margin is the big empty box
        /// that was reported. And `FrameHorizontalOffsetRatio` always shoves the subject
        /// off-centre to clear that panel; a tile has no panel to clear, so the offset just
        /// walks the subject toward one edge and any zoom then pushes it out of frame.
        ///
        /// ⚠️ AND IT MUST BE CALLED AFTER <see cref="Show"/>, because `uniformExtent` changes
        /// what the framing COMPUTES and the subject was already measured on the way in.
        /// </summary>
        public void SetTileFraming(float factor, bool uniformExtent = false)
        {
            _centreSubject = true;
            _uniformExtent = uniformExtent;
            _userZoom = Mathf.Clamp(factor, ZoomMin, ZoomMax);
            _needsFrame = true;
        }

        /// <summary>
        /// Lets the player turn a TILE's subject the way the CHARACTER screen lets them turn a
        /// full-size one, without taking the wheel.
        ///
        /// ⚠️ THE WHEEL IS DELIBERATELY LEFT ALONE AND THAT IS THE WHOLE POINT OF THIS FUNCTION.
        /// These tiles live inside the tutorial page's scroll view, and a preview that consumed
        /// the wheel would zoom a slipper while the player was trying to scroll the page. Drag
        /// and right-click are gestures a scroller has no use for; zoom stays exclusive to the
        /// CHARACTER screen, which has nothing to compete with.
        /// </summary>
        public void EnableTileInteraction() => _wheelZooms = false;

        /// <summary>Builds the render target and camera. Call once, then <see cref="Show"/>.</summary>
        public void Attach(RectTransform panel)
        {
            _panel = panel;

            var surfaceGo = new GameObject("PreviewSurface", typeof(RectTransform), typeof(RawImage));
            surfaceGo.transform.SetParent(panel, false);
            _surface = surfaceGo.GetComponent<RawImage>();
            MenuKit.Stretch(_surface.rectTransform);

            // ⚠️ RAYCASTABLE, OR THE DRAG NEVER ARRIVES. This is the surface the player grabs,
            // and it sits behind the wood panel and both buttons, so those still get first
            // refusal on anything that lands on them.
            _surface.raycastTarget = true;
            surfaceGo.AddComponent<ModelPreviewInput>().Bind(this);

            _stage = Stage + Vector3.right * (StageSpacing * _stageCount++);

            var stageGo = new GameObject("PreviewStage");
            stageGo.transform.position = _stage;
            _pivot = stageGo.transform;

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(_pivot, false);

            _camera = camGo.AddComponent<UnityEngine.Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;

            // Transparent, so the panel's own wood shows through behind the model rather than
            // a flat slab of colour that has to be matched by hand.
            _camera.backgroundColor = new Color(0, 0, 0, 0);
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.fieldOfView = FieldOfView;
            _camera.nearClipPlane = 0.05f;

            // ⚠️⚠️ THE PORTRAIT NEEDS ITS OWN GRADE NOW THAT `Toon.shader` NO LONGER TONEMAPS.
            // The curve moved to a full-screen camera pass so that the sky and the world get it
            // too (see ColourGrade), which means any camera rendering toon surfaces has to carry
            // one or the subject clips to white with nothing to roll it off.
            //
            // ⚠️ AND THE NUMBERS ARE THIS SCREEN'S, NOT A MAP'S. `CharacterSelect.tscn` has its
            // own Environment: tonemap mode 3 at exposure 0.92 with white 1.9, and an adjustment
            // at contrast 1.03 and saturation 1.18. Adopting an arena's grade here instead would
            // make the character you pick a different colour from the one who walks out.
            camGo.AddComponent<Visual.ColourGrade>()
                 .Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);

            BuildLights();
            IsolateFromForeignLights();
            EnsureTexture();
        }

        /// <summary>
        /// Takes the preview layer out of every light that is not this rig's own.
        ///
        /// ⚠️⚠️ THIS IS WHAT `own_world_3d` DID FOR FREE AND ITS ABSENCE BLEACHED THE SUBJECT
        /// WHITE. Godot's SubViewport is a separate world, so the map's sun cannot reach the
        /// character on the CHARACTER screen. Unity has one world, and the setup screen loads
        /// the chosen arena live behind its panels, so the preview was lit by its own key at
        /// 1.35, its own fill at 0.45 AND Eskinita's key at 1.15 on top: every surface saturated
        /// and the model rendered as a white silhouette with an ink border.
        ///
        /// ⚠️ RE-RUN WHEN A SUBJECT IS SHOWN, not only at Attach. The arena behind this screen
        /// loads asynchronously and brings its light with it, so a pass that ran once at build
        /// time misses the one light that actually causes this.
        /// </summary>
        /// <summary>See <see cref="PreviewAmbient"/>. Remembers what it replaced so a screen
        /// that tears this rig down leaves the scene as it found it.</summary>
        private void ApplyPreviewAmbient()
        {
            if (!_ambientSaved)
            {
                _savedAmbientMode = RenderSettings.ambientMode;
                _savedAmbient = RenderSettings.ambientLight;
                _savedAmbientIntensity = RenderSettings.ambientIntensity;
                _ambientSaved = true;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = PreviewAmbient;
            RenderSettings.ambientIntensity = 1.0f;
        }

        private bool _ambientSaved;
        private UnityEngine.Rendering.AmbientMode _savedAmbientMode;
        private Color _savedAmbient;
        private float _savedAmbientIntensity;

        private void IsolateFromForeignLights()
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include,
                                                           FindObjectsSortMode.None))
            {
                if (light == null || light.transform.IsChildOf(_pivot)) continue;

                light.cullingMask &= ~(1 << PreviewLayer);
            }
        }

        /// <summary>
        /// The two lights out of `CharacterSelect.tscn`'s SubViewport, transcribed with the
        /// same handedness flip the map importer uses.
        ///
        /// ⚠️ IT LIGHTS ITSELF. A map brings its own sun; a bare `.glb` brings nothing, so a
        /// character dropped into an empty preview renders as a black silhouette. The rig is
        /// part of the scene, not decoration.
        /// </summary>
        private void BuildLights()
        {
            Light(new Vector3(-0.5f, -0.433013f, 0.75f), new Vector3(0.0f, 0.866025f, 0.5f),
                  1.35f * LightExposure, "PreviewKey", shadows: true);

            Light(new Vector3(0.642788f, -0.262003f, -0.719846f),
                  new Vector3(0.0f, 0.939693f, -0.342020f), 0.45f * LightExposure,
                  "PreviewFill", shadows: false);
        }

        /// <summary>
        /// Godot's basis Z and Y columns to a Unity light rotation. Godot shines along its own
        /// -Z and Unity along its +Z, and the mirror maps one onto the other, so a faithful
        /// basis conversion is all that is needed and no extra sign flip belongs here.
        /// </summary>
        private void Light(Vector3 gz, Vector3 gy, float intensity, string name, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_pivot, false);

            var forward = new Vector3(-gz.x, -gz.y, gz.z);
            var up = new Vector3(gy.x, gy.y, -gy.z);

            go.transform.rotation = Quaternion.LookRotation(forward, up);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << PreviewLayer;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        /// <summary>
        /// ⚠️⚠️ THE TARGET IS SIZED FROM THE PANEL, AND A FIXED SIZE IS WHY EVERY SUBJECT WAS
        /// STRETCHED. A 512x640 texture drawn across a rect of another shape is scaled on both
        /// axes independently, so a character rendered square came out squashed by however much
        /// the two aspect ratios differed. It also feeds the framing: `_frame()` divides by the
        /// aspect to decide how far back to sit.
        ///
        /// ⚠️ AND IT IS READ IN LateUpdate, NEVER IN Attach. `rect.width` is 0 before the first
        /// layout pass, and every number derived from it is nonsense.
        /// </summary>
        private void EnsureTexture()
        {
            if (_panel == null || _camera == null) return;

            var rect = _panel.rect;

            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 64, 2048);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 64, 2048);

            if (rect.width < 1.0f || rect.height < 1.0f) return;
            if (_texture != null && _texture.width == width && _texture.height == height) return;

            var old = _texture;

            _texture = new RenderTexture(width, height, 24) { name = "PreviewRT" };
            _camera.targetTexture = _texture;
            _surface.texture = _texture;

            // ⚠️⚠️ THE CAMERA'S ASPECT DOES NOT FOLLOW ITS TARGET TEXTURE ON ITS OWN ONCE IT HAS
            // RENDERED ONCE, AND THAT IS THE WHOLE OF "its stretched". A camera created before
            // its target caches the aspect of the MAIN DISPLAY, so a square-ish frustum was
            // being drawn into a 16:9 texture and every subject came out wide. Measured off the
            // capture: the face rendered at 2.38 wide-to-tall against the mesh's own 1.36, a
            // factor of 1.75, which is 1600/900 to within a percent. Setting the target texture
            // is not enough; the aspect has to be re-derived.
            _camera.aspect = (float)width / height;

            if (old != null) old.Release();

            _frameAspect = (float)width / height;
            _needsFrame = true;
        }

        /// <summary>
        /// Swap in a model.
        ///
        /// ⚠️ THE CLIPS COME IN FROM THE ROSTER ASSET. They are sub-assets of the `.glb` and
        /// nothing else references them, so an asset nothing points at is stripped from the
        /// build. `RosterEntryAsset.Clips` is the reference that makes them ship.
        /// </summary>
        private GameObject _pet;

        public void Show(GameObject prefab, AnimationClip[] clips) => Show(prefab, clips, null, null);

        public void Show(GameObject prefab, AnimationClip[] clips, Color[] palette) => Show(prefab, clips, palette, null);

        /// <summary>
        /// ⚠️ THE SCREEN AND THE MATCH MUST APPLY THE PALETTE THE SAME WAY. What you pick and
        /// what walks out cannot look like two different characters, and the only way to
        /// guarantee that is for both to go through `ToonSkin` with the same sixteen colours.
        /// </summary>
        public void Show(GameObject prefab, AnimationClip[] clips, Color[] palette, GameObject petModel)
        {
            if (_model != null) Destroy(_model);
            if (_pet != null) Destroy(_pet);

            _idle = null;

            if (prefab == null) return;

            _model = Instantiate(prefab, _pivot);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            _model.transform.localScale = Vector3.one * PreviewScale;
            _model.transform.localRotation = Quaternion.Euler(0.0f, FacingYaw, 0.0f);

            SetLayerRecursively(_model, PreviewLayer);

            // ⚠️ THE PREVIEW WEARS THE SAME MATERIAL THE MATCH DOES. In Godot the palette
            // `.tres` carries the toon shading and the ink outline, and this screen applies it
            // by the same mechanism a spawn does, precisely so what you pick and what walks out
            // cannot look like two different characters.
            // ⚠️⚠️ NO `* PreviewScale`, AND THE ONE THAT WAS HERE IS "zach in char select is
            // buggy, his face specifically is weird af". `PersonOutlineWidth` is ALREADY a WORLD
            // width and already carries the 2.38 — `CharacterVisual` says so at its own call site
            // and warns against exactly this multiply — so scaling it by `PreviewScale` here
            // asked for a 45 mm ink border on a character who wears 19 mm everywhere else,
            // 2.38 times too thick.
            //
            // ⚠️ AND THE FACE IS WHERE IT SHOWS FIRST, WHICH IS WHY IT READ AS A FACE BUG RATHER
            // THAN AN OUTLINE ONE. The features are 20 mm pixels on the skull's own front plane
            // (`build_person_voxel.py`, FACE_PIXELS). The outline is an inverted hull that pushes
            // every vertex along its SMOOTHED normal, so at 45 mm each eye grows a shell wider
            // than the eye itself and fans sideways off the corner normals: the eyes smear, the
            // smile grows a tooth, and the hair merges into one lump. At the correct 19 mm the
            // shell sits inside the feature and draws the border it is supposed to.
            //
            // This screen is the one place a player looks at a character up close, so it is also
            // the only place the error was ever going to be visible.
            Visual.ToonSkin.Apply(_model, Visual.ToonSkin.PersonOutlineWidth, palette);

            if (petModel != null)
            {
                _pet = Instantiate(petModel, _pivot);
                _pet.transform.localScale = Vector3.one * PreviewScale;
                SetLayerRecursively(_pet, PreviewLayer);
                var companion = _pet.AddComponent<Visual.GhostPetCompanion>();
                companion.Bind(_model.transform, new Vector3(-0.52f, 0.50f, -0.05f), PreviewScale);
                Visual.ToonSkin.Apply(_pet, Visual.ToonSkin.PersonOutlineWidth, palette);
            }

            PlayIdle(clips);
            IsolateFromForeignLights();
            ApplyPreviewAmbient();

            // A new subject gets a fresh sweep: the player picked this one to look at, and the
            // arc's phase carrying over means one pick greets you front-on and the next shows
            // you its ear.
            _turnPhase = 0.0f;

            _needsFrame = true;
        }

        /// <summary>
        /// Stands the preview in its idle.
        ///
        /// ⚠️⚠️ WITHOUT THIS THE SCREEN SHOWS A T-POSE, which is what was reported. The rig's
        /// rest pose is arms straight out: it reads as unfinished art more loudly than any
        /// modelling flaw would, and it is also nearly twice as wide as the real silhouette, so
        /// it wrecks the framing measured off it.
        ///
        /// ⚠️ PLAYABLES, NOT AN AnimatorController. Same reasoning as `CharacterAnimator`: a
        /// controller is an authored asset that would have to be built by hand for each of the
        /// twelve rigs, and the clips already ship inside the models.
        /// </summary>
        private void PlayIdle(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0 || _model == null) return;

            AnimationClip idle = null;

            // ⚠️ MATCHED CASE-INSENSITIVELY AND WITH THE .gd's OWN FALLBACK CHAIN.
            // `character_visual.gd` resolves a pose through a list rather than one literal,
            // precisely so a rig that spells it differently still animates, and this screen
            // hard-matched the string "idle". A rig that misses turns into a bind pose, which on
            // these models is arms straight out.
            foreach (string want in IdleNames)
            {
                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    if (!string.Equals(clip.name, want, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    idle = clip;
                    break;
                }

                if (idle != null) break;
            }

            // Better a wrong pose than a T-pose: the rest pose is the one thing guaranteed to
            // read as broken art, so anything the asset ships beats it.
            if (idle == null) idle = clips[0];
            if (idle == null) return;

            _idle = idle;

            // ⚠️⚠️ AN ANIMATOR ON THE PREVIEW FIGHTS THE SAMPLING AND WINS. `SampleAnimation`
            // writes the pose straight onto the transforms; an Animator that is enabled with no
            // controller then writes its own bind pose back over it in the same frame, and the
            // result is the smeared half-posed figure this screen showed. The match's own
            // animator is a Playables graph on a live unit, which is a different arrangement
            // entirely; a menu portrait needs one clip and no state machine.
            //
            // ⚠️⚠️ AND A PLAYABLES GRAPH WAS TRIED HERE AND IS WRONG. On the reasoning that a
            // disabled Animator leaves nothing for `SampleAnimation` to bind to, this was
            // rewritten to build a graph the way `CharacterAnimator` does. It compiles, it runs,
            // and it animates NOTHING: `ModelPreviewTests` caught it immediately with *"'arm-left'
            // has not moved in 30 frames"*. The premise was simply false — `SampleAnimation`
            // binds curves to transforms BY PATH and needs neither an Avatar nor an enabled
            // Animator, which is exactly why it suits a portrait. The arms-out silhouette that
            // prompted the rewrite is not a bind pose at all; it is what this rig's `idle`
            // actually looks like, confirmed against the toon bench, which poses nothing and
            // renders the arms DOWN.
            foreach (var animator in _model.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
        }

        /// <summary>`character_visual.gd` resolves a pose through a chain. See PlayIdle.</summary>
        private static readonly string[] IdleNames = { "idle", "static" };

        /// <summary>The clip the preview stands in, sampled by hand. See PlayIdle.</summary>
        private AnimationClip _idle;

        /// <summary>
        /// ⚠️ SAMPLED RATHER THAN PLAYED THROUGH A GRAPH, AND THAT IS DELIBERATE.
        /// `AnimationClip.SampleAnimation` binds the curves to transforms BY PATH, which needs no
        /// Avatar and no controller, and the `.glb` rigs arrive from glTFast with neither. It is
        /// also the whole of what a portrait needs: one clip, looping, no blending and no state.
        /// A graph was tried here and animated nothing. See PlayIdle.
        /// </summary>
        private void SampleIdle()
        {
            if (_idle == null || _model == null) return;

            _idle.SampleAnimation(_model, Time.unscaledTime % Mathf.Max(0.01f, _idle.length));
        }

        /// <summary>
        /// ⚠️⚠️ A GENERIC ANIMATOR WITH NO AVATAR PLAYS NOTHING, AND IT SAYS NOTHING ABOUT IT.
        /// The `.glb` rigs come in through glTFast, which emits an Animator with a null
        /// controller (correct for Playables) and NO Avatar, and an animation output bound to an
        /// avatar-less Animator silently drives no transforms. The model then stands in its bind
        /// pose, which on these rigs is arms straight out: the exact T-pose
        /// `character_preview.gd` warns about, reached by a completely different route.
        ///
        /// `BuildGenericAvatar` derives the binding from the hierarchy that is already there, so
        /// nothing about the asset or its clips has to change.
        /// </summary>
        public static void EnsureAvatar(Animator animator)
        {
            if (animator == null || animator.avatar != null) return;

            var avatar = AvatarBuilder.BuildGenericAvatar(animator.gameObject, "");

            if (avatar == null || !avatar.isValid)
            {
                Debug.LogWarning($"[Preview] could not build a generic avatar for " +
                                 $"{animator.name}; it will stand in its bind pose.");
                return;
            }

            avatar.name = animator.name + "Avatar";
            animator.avatar = avatar;
        }

        /// <summary>
        /// Fits the camera to the model's real bounds.
        ///
        /// ⚠️⚠️ FIT BOTH AXES, NOT JUST HEIGHT, AND TAKE THE LARGEST OF THREE. Fitting on height
        /// alone is right for a standing Person and catastrophic for a tsinelas, which is 0.432
        /// long and 0.078 tall: framing that to be 0.078 units tall on screen puts the camera
        /// inside it. The third term fits the widest horizontal extent against the SHORT axis of
        /// the frame, which is true whichever way that axis ends up pointing once the camera is
        /// looking down at a flat object. `character_preview.gd` measured 10% of the slipper
        /// outside the frame before the third term was added.
        ///
        /// ⚠️ AND THE PITCH SCALES WITH HOW FLAT THE SUBJECT IS. 16 degrees is a natural gaze on
        /// a standing Person and still nearly edge-on to a tsinelas. Flatness is height/width,
        /// about 1.0 for a Person and 0.18 for a slipper, so lerping on it looks each subject in
        /// its own most legible face with no per-entry tuning value.
        /// </summary>
        private void Frame()
        {
            if (_model == null) return;

            bool any = false;
            Bounds bounds = default;

            foreach (var r in _model.GetComponentsInChildren<Renderer>())
            {
                // ⚠️ A SKINNED RENDERER'S BOUNDS ARE THE REST POSE UNLESS THIS IS SET, and the
                // rest pose here is the T-pose. Asking for the posed bounds is what makes the
                // measurement agree with what is actually on screen.
                if (r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;

                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            if (!any) return;

            float height = Mathf.Max(bounds.size.y, 0.001f);

            // The widest the subject can present as it turns: it rotates about Y, so at some
            // point in the sweep its longest horizontal axis faces the camera.
            float width = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.z), 0.001f);

            _frameAim = new Vector3(bounds.center.x,
                                    bounds.min.y + height * AimHeightRatio,
                                    bounds.center.z);

            float halfFov = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspect = Mathf.Max(0.01f, _frameAspect);

            _frameDistance = Mathf.Max(
                Mathf.Max((height * FrameMargin * 0.5f) / halfFov,
                          (width * FrameMargin * 0.5f) / (halfFov * aspect)),
                (width * FrameMargin * 0.5f) / halfFov);

            // ⚠️ UNIFORM MODE: EVERY SUBJECT'S LONGEST AXIS GETS THE SAME SCREEN LENGTH.
            // 🧑, looking at the tutorial's four tiles: *"why not make everything size of lata"*.
            // Fitting is not sizing. The rule above takes whichever axis needs the camera
            // furthest back, so a tall narrow lata binds on HEIGHT and fills its tile top to
            // bottom, while a figure with its arms out binds on WIDTH and is then only as tall
            // as its own aspect allows. Both "fill the frame" and only one looks big.
            //
            // ⚠️ OPT-IN, AND THE CHARACTER SCREEN MUST NOT GET IT. There the subject is alone at
            // full size and should use the whole frame.
            if (_uniformExtent)
            {
                float extent = Mathf.Max(height, width);
                _frameDistance = (extent * FrameMargin * 0.5f) / (halfFov * Mathf.Min(aspect, 1.0f));
            }

            float flatness = Mathf.Clamp01(height / width);
            _framePitch = Mathf.Lerp(CameraPitchFlatDegrees, CameraPitchDegrees, flatness);

            _needsFrame = false;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            EnsureTexture();

            // ⚠️ RE-DERIVED EVERY FRAME, NOT ONLY WHEN THE TARGET IS BUILT. Setting it once in
            // `EnsureTexture` is correct until something resets it, and a camera's aspect is
            // reset by the engine on a resolution change as well as by any code that assigns
            // `targetTexture`. One frame of the wrong aspect is a visibly stretched portrait, and
            // this costs a float divide.
            if (_texture != null && _texture.height > 0)
                _camera.aspect = (float)_texture.width / _texture.height;

            SampleIdle();

            if (_needsFrame) Frame();

            // THE MODEL TURNS. The idle sweep is a there-and-back through TurnDegrees either
            // side of front-on, over TurnPeriod seconds. It stops the moment the player drags.
            if (_model != null && !_userTookOver)
            {
                _turnPhase += Time.unscaledDeltaTime;
                float sweep = Mathf.Sin(_turnPhase / TurnPeriod * Mathf.PI * 2.0f) * TurnDegrees;
                _model.transform.localRotation = Quaternion.Euler(0.0f, FacingYaw + sweep, 0.0f);
            }

            ApplyCamera();
        }

        /// <summary>
        /// Puts the camera where the measured framing says, plus whatever the player has
        /// dragged. The single place the camera transform is written, so the automatic and the
        /// manual paths cannot disagree.
        /// </summary>
        private void ApplyCamera()
        {
            float distance = _frameDistance * _userZoom;
            float yaw = CameraYawDegrees + _userYaw;
            float pitch = Mathf.Clamp(_framePitch + _userPitch, OrbitPitchMin, OrbitPitchMax);

            var rot = Quaternion.Euler(pitch, yaw, 0.0f);

            _camera.transform.rotation = rot;

            // ⚠️ GODOT'S `h_offset` IS A TRANSLATION ALONG THE CAMERA'S OWN X, applied after
            // `look_at` (`Camera3D::get_camera_transform`). So it moves the camera sideways and
            // leaves the aim where it was, which is what this reproduces. It has to follow the
            // zoom too, or dollying in walks the subject back behind the wood panel.
            float halfFov = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float offset = _centreSubject
                ? 0.0f
                : FrameHorizontalOffsetRatio * (2.0f * distance * halfFov) * _frameAspect;

            _camera.transform.position = _frameAim
                                         - (rot * Vector3.forward) * distance
                                         + (rot * Vector3.right) * offset;
        }

        /// <summary>
        /// Drag to orbit the camera around the subject.
        ///
        /// ⚠️ THE FIRST DRAG ENDS THE IDLE SWEEP FOR GOOD. See the class note.
        /// </summary>
        public void Orbit(Vector2 delta)
        {
            _userTookOver = true;

            // ⚠️⚠️ THE TWO AXES DO NOT TAKE THE SAME CORRECTION, AND ASSUMING THEY DID IS WHY
            // THIS HAS BEEN WRONG TWICE. The signs were first copied straight from
            // `character_preview.gd:363`, which subtracts on both; then BOTH were flipped on an
            // argument about handedness. The screen still turned the wrong way.
            //
            // A drag is a GRAB: the surface of the subject nearest the camera follows the
            // pointer. That is what the original does on both axes, and `PreviewDragProbe`
            // measures it here by projecting a fixed world point on that near surface and
            // watching which way it travels.
            //
            //  X — ALREADY CORRECT, MEASURED. A rightward drag moved the near face from
            //      viewport 0.689 to 0.712, i.e. right, which is the grab. The .gd reaches the
            //      same picture through a minus because its own camera sits on the opposite
            //      side of the subject at the same yaw. **Do not flip this a third time on an
            //      argument about handedness. Run the probe.**
            //  Y — INVERTED, AND THIS IS THE HALF THAT WAS BROKEN. Two things differ from the
            //      .gd rather than one, and they do NOT cancel: `PointerEventData.delta` counts
            //      UP as positive where `InputEventMouseMotion.relative` counts DOWN, and a
            //      positive pitch here puts the camera ABOVE the subject where the .gd's puts
            //      it below. Reported as *"i move model up, it goes down"*, and measured as
            //      0.415 -> 0.397 on a 40 px upward drag before this minus.
            _userYaw += delta.x * OrbitSensitivity;
            _userPitch = Mathf.Clamp(_userPitch - delta.y * OrbitSensitivity,
                                     OrbitPitchMin - _framePitch, OrbitPitchMax - _framePitch);
        }

        public void Zoom(float wheel)
        {
            if (!_wheelZooms) return;

            _userTookOver = true;
            _userZoom = Mathf.Clamp(_userZoom - wheel * ZoomStep, ZoomMin, ZoomMax);
        }

        /// <summary>Back to the measured shot, and back to turning on its own.</summary>
        public void ResetView()
        {
            _userYaw = 0.0f;
            _userPitch = 0.0f;
            _userZoom = 1.0f;
            _userTookOver = false;
            _turnPhase = 0.0f;

            if (_model != null)
                _model.transform.localRotation = Quaternion.Euler(0.0f, FacingYaw, 0.0f);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDisable()
        {
            RestoreAmbient();
        }

        private void OnDestroy()
        {
            if (_texture != null) _texture.Release();
            if (_pivot != null) Destroy(_pivot.gameObject);

            RestoreAmbient();
        }

        private void RestoreAmbient()
        {
            if (!_ambientSaved) return;

            RenderSettings.ambientMode = _savedAmbientMode;
            RenderSettings.ambientLight = _savedAmbient;
            RenderSettings.ambientIntensity = _savedAmbientIntensity;
            _ambientSaved = false;
        }
    }
}
