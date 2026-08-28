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

        /// <summary>
        /// What the LOBBY camera aims at, and it is lower than the map shot's on purpose.
        ///
        /// ⚠️⚠️ AIMING LOWER PUSHES THE SUBJECT HIGHER UP THE FRAME, WHICH IS THE WHOLE POINT.
        /// `Logs/shots-runtime/Lobby-v2.png` has the cast correctly sized and standing dead centre
        /// vertically, so their legs are behind the two corner panels and the line reads as four
        /// floating torsos. The furniture is at the BOTTOM of the screen, so the cast has to be
        /// above the middle, and moving the aim point down the body is what does that without
        /// changing how big they are.
        ///
        /// ⚠️ IT WENT BACK UP TO 1.15 WHEN THE FURNITURE LEFT THE BOTTOM OF THE SCREEN. While both
        /// columns sat in the bottom corners the cast had to be lifted clear of them, and 0.70 did
        /// that. With the settings under the banner and the lobby card top-right there is nothing
        /// down there to clear, and lifting a much larger cast now pushes its feet off the top of
        /// the road: 1.15 is roughly the waist, which centres the bodies in the frame.
        /// </summary>
        public const float LobbyLookHeight = 1.15f;

        /// <summary>From `MatchSetup.tscn`'s Camera3D. The one property of that node that IS
        /// read, because `_apply_camera` writes position and basis but never the FOV.</summary>
        private const float FieldOfView = 58.0f;

        /// <summary>
        /// The LOBBY shot's field of view, which is narrower than the map shot's on purpose.
        ///
        /// ⚠️⚠️ A CLOSE SHOT AT 58 DEGREES DISTORTS THE PEOPLE ON THE ENDS OF THE LINE, and
        /// solving the framing by moving the camera alone cannot avoid it. 58 vertical on 16:9 is
        /// about 89 degrees horizontal; framing four characters to fill half the height at that
        /// angle puts the camera around 3 m away, which leaves the outer two at 34 degrees off
        /// axis and visibly stretched, widest at the shoulders, exactly the sort of thing
        /// `ModelPreview`'s own header records being reported as *"model isnt movable and its
        /// stretched"*.
        ///
        /// At 32 degrees the same framing sits about 7 m back and the outer characters are 17
        /// degrees off axis. It is also the reason the arena behind them still reads: a longer
        /// lens keeps more of the street at a usable size instead of hurling it toward the
        /// vanishing point.
        ///
        /// ⚠️ THE MAP SHOT KEEPS 58, which is the value `MatchSetup.tscn`'s own Camera3D carries
        /// and the one every map's `Distance` and `Height` were tuned against. Changing it would
        /// silently re-frame all three arenas on the practice screen.
        /// </summary>
        private const float LobbyFieldOfView = 32.0f;

        /// <summary>Half the screen is enough behind a scrim, and it halves the cost.</summary>
        private const int Width = 960;
        private const int Height = 540;

        /// <summary>
        /// ⚠️⚠️ THE PREVIEW ARENA MUST BE ON ITS OWN LAYER, AND ITS ABSENCE IS THE FULL-WIDTH
        /// BAND ACROSS EVERY MENU. This screen loads a whole dressed street ADDITIVELY into the
        /// menu's own world, at the origin, on the DEFAULT layer, while the menu camera ships
        /// with a culling mask of every layer. So the arena was never confined to the render
        /// texture it exists for: the road slab sits a couple of units from the menu camera and
        /// gets drawn straight into the frame, which is the grey band with a cream kerb line
        /// running the full width of MatchSetup and CharacterSelect. It reads as a UI seam
        /// because a slab seen almost edge-on is a straight horizontal strip, and it was
        /// diagnosed as one twice.
        ///
        /// ⚠️ IT IS VISIBLE IN THE CAPTURES BECAUSE OF HOW THEY ARE TAKEN, and that is not a
        /// reason to dismiss it. `UiRuntimeShots.Capture` flips the Overlay canvas to
        /// ScreenSpaceCamera at a plane distance of 1 so a camera can photograph it at all, which
        /// puts any geometry nearer than 1 unit IN FRONT of the whole UI. In the shipped player
        /// the canvas is an Overlay and the same slab is merely behind it, which is not correct
        /// either: it is lit by the arena's sun, it costs a draw, and any transparent panel shows
        /// it. Confining it is the fix in both cases.
        ///
        /// 29 rather than `ModelPreview.PreviewLayer` 30, so the character portrait and the map
        /// behind it stay independently cullable.
        /// </summary>
        public const int PreviewLayer = 29;

        private RawImage _surface;
        private RenderTexture _target;
        private Camera _camera;
        private string _showing;
        private bool _busy;

        /// <summary>
        /// The camera that photographs the arena, for anything that has to project a world point
        /// into this surface's rect.
        ///
        /// ⚠️ IT IS NULL UNTIL THE FIRST SWAP COMPLETES. `EnsureCamera` runs at the END of
        /// `Swap`, after the scene load, so a caller that reads this from its own `Start` gets
        /// nothing. Wait for <see cref="MapShown"/>.
        /// </summary>
        public Camera Camera => _camera;

        /// <summary>Where the play area is, in world space: the average of the map's spawn
        /// markers. See <see cref="AimAt"/>.</summary>
        public Vector3 Pivot => _pivot;

        /// <summary>The map currently in the surface, or null before the first swap.</summary>
        public string Showing => _showing;

        /// <summary>The tuned angle, without the sway. A caller placing something in front of
        /// the camera wants the shot's yaw, not this frame's wobble.</summary>
        public float Yaw => _yaw;

        /// <summary>
        /// Raised when a map has finished loading and the camera exists, with the map's id.
        ///
        /// ⚠️⚠️ NOTHING ELSE CAN TELL. `Show` starts a coroutine and returns immediately, so a
        /// caller that loads a map and then places something into it on the next line places it
        /// into a scene that is not there yet. That is a silent no-op followed by a cast standing
        /// at the world origin, inside the menu camera's view, which is the exact class of fault
        /// `PreviewLayer`'s note describes as "the grey band across every menu".
        /// </summary>
        public event System.Action<string> MapShown;

        /// <summary>
        /// Swaps between the MAP shot (wide, high, for picking an arena) and the LOBBY shot
        /// (close, low, for looking at four people). See <see cref="SceneFlow.MapEntry"/>.
        ///
        /// ⚠️ IT RE-AIMS IMMEDIATELY RATHER THAN WAITING FOR THE NEXT SWAP, because the lobby
        /// turns it on after the first map is already showing and the practice screen turns it
        /// off on a screen the player is looking at.
        /// </summary>
        public bool LobbyShot
        {
            get => _lobbyShot;
            set
            {
                if (_lobbyShot == value) return;

                _lobbyShot = value;

                if (_showing != null) AimAt(_showing);

                if (_camera != null)
                {
                    // ⚠️ THE LENS AS WELL AS THE POSITION. `EnsureCamera` sets the FOV and only
                    // runs on a map swap, so flipping this on a screen that is already showing a
                    // map moved the camera and left it on the wide lens: the four-person framing
                    // measured for 32 degrees, rendered at 58.
                    _camera.fieldOfView = _lobbyShot ? LobbyFieldOfView : FieldOfView;
                    ApplyCamera();
                }
            }
        }

        private bool _lobbyShot;

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

                // ⚠️ AFTER the strip, never before. `StripMatchObjects` destroys whole
                // GameObjects, and re-layering a subtree that is about to be deleted is wasted
                // work on the one screen that must not stall.
                Confine(loaded);
            }

            _showing = map;

            AimAt(map);
            EnsureCamera();

            // ⚠️ AFTER EnsureCamera, because it attaches the grade to that camera and the camera
            // does not exist on the first swap until EnsureCamera has run.
            ApplyMapEnvironment(map);

            _surface.texture = _target;
            _surface.color = Color.white;

            _busy = false;

            // ⚠️ LAST, AFTER THE CAMERA AND THE ENVIRONMENT. A listener's whole reason to exist
            // is to put something INTO this map, and the two things it needs (a camera to be
            // framed by and a scene to be parented into) are both set up above.
            MapShown?.Invoke(map);
        }

        /// <summary>
        /// Moves a GameObject into the arena currently on screen, on the preview layer, so it is
        /// lit by that map's sun, fogged by its fog and graded by its grade.
        ///
        /// ⚠️⚠️ THIS IS WHY THE LOBBY CAST IS NOT A SECOND RENDER TEXTURE. Compositing four
        /// `ModelPreview` rigs over this surface would be four cameras and four targets, each lit
        /// by its own private key light and none of them by anything the map knows about: the
        /// characters would sit ON the picture rather than IN it. Parenting into the arena costs
        /// nothing extra to draw and gets the map's whole lighting environment for free.
        ///
        /// ⚠️ THE LAYER IS SET AFTER THE REPARENT, NOT BEFORE. `SetLayerRecursively` walks the
        /// subtree it is given, and a model instantiated under a different parent may have had
        /// children added since; doing it here means a caller cannot forget, and forgetting is
        /// what puts geometry in front of every menu (see <see cref="PreviewLayer"/>).
        ///
        /// ⚠️ IT RETURNS FALSE RATHER THAN THROWING WHEN THE MAP IS NOT LOADED YET. `Show` is a
        /// coroutine; a caller that has not waited for <see cref="MapShown"/> gets an honest no.
        /// </summary>
        public bool Adopt(GameObject go)
        {
            if (go == null) return false;
            if (_showing == null) return false;
            if (!_cache.TryGetValue(_showing, out var scene)) return false;
            if (!scene.IsValid() || !scene.isLoaded) return false;

            SceneManager.MoveGameObjectToScene(go, scene);
            SetLayerRecursively(go.transform, PreviewLayer);

            return true;
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

            // ⚠️ THE LOBBY IS A DIFFERENT SHOT OF THE SAME SET, AND IT SHARES THE YAW ON PURPOSE.
            // See `MapEntry.LobbyDistance`: the angle is a judgement about which way to look down
            // the street and does not change with range; only how close and how high do.
            _distance = _lobbyShot ? entry.LobbyDistance : entry.Distance;
            _height = _lobbyShot ? entry.LobbyHeight : entry.Height;

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

                // ⚠️ CANNOT LEAVE THE GAME WITH ZERO LISTENERS, AND STRUCTURALLY CANNOT.
                // `root` here is one of THIS additively loaded arena scene's own root objects.
                // The game's one real AudioListener lives on `~GameServices`, which is
                // DontDestroyOnLoad and was created long before any preview arena loads, so it is
                // never a descendant of `root` and this loop can never reach it. What this
                // destroys is only a listener the arena scene itself happened to bake in (a
                // camera carried over from Godot import), which must go regardless, since the
                // scene is invisible geometry behind a render texture and must not compete for
                // the real listener slot.
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
        /// <summary>
        /// ⚠️⚠️ AN ADDITIVE SCENE'S LIGHTING IS NEVER APPLIED, AND THAT IS "its completely dark,
        /// no sky". Ambient, fog and the skybox are not properties of the objects in a scene:
        /// they live in that scene's own RenderSettings, and Unity only ever uses the ACTIVE
        /// scene's. This screen loads the arena ADDITIVELY, so everything `TscnImporter` baked
        /// out of the Godot Environment (the warm 0.673 ambient, the linear fog from 14 to 58,
        /// and the sky itself) was sitting in a scene that was never active, and the preview was
        /// lit by the MENU's default settings instead: a flat dark blue with no sky behind it.
        /// Against the Godot build's own capture of the same screen, which shows a dusk street
        /// with the sky visible above the rooftops, the port rendered a navy silhouette.
        ///
        /// ⚠️ THE VALUES ARE COPIED OUT, NOT LEFT ACTIVE. Making the arena the active scene would
        /// do the same job in one line and would also send every GameObject the menu creates from
        /// then on into the arena scene, including the character portrait's own rig, which then
        /// gets unloaded with the map on the next arrow press. So the scene is made active just
        /// long enough to READ its settings and is handed straight back.
        ///
        /// ⚠️ AND IT IS CACHED PER MAP. Cycling the picker is the one thing a player does fast on
        /// this screen, and the whole caching design here exists so that costs nothing.
        /// </summary>
        private struct MapEnvironment
        {
            public Color Ambient;
            public UnityEngine.Rendering.AmbientMode Mode;
            public float Intensity;
            public Material Skybox;
            public bool Fog;
            public Color FogColour;
            public FogMode FogMode;
            public float FogStart;
            public float FogEnd;

            /// <summary>The map's own colour grade. See Visual.MapGrade.</summary>
            public float Brightness;
            public float Contrast;
            public float Saturation;
            public float Exposure;
            public float White;
        }

        private readonly System.Collections.Generic.Dictionary<string, MapEnvironment> _envs =
            new System.Collections.Generic.Dictionary<string, MapEnvironment>();

        public void ReapplyEnvironment()
        {
            if (!string.IsNullOrEmpty(_showing))
                ApplyMapEnvironment(_showing);
        }

        private void ApplyMapEnvironment(string map)
        {
            if (!_envs.TryGetValue(map, out var env))
            {
                if (!_cache.TryGetValue(map, out var scene) || !scene.IsValid() || !scene.isLoaded)
                    return;

                var previous = SceneManager.GetActiveScene();

                if (!SceneManager.SetActiveScene(scene)) return;

                env = new MapEnvironment
                {
                    Ambient = RenderSettings.ambientLight,
                    Mode = RenderSettings.ambientMode,
                    Intensity = RenderSettings.ambientIntensity,
                    Skybox = RenderSettings.skybox,
                    Fog = RenderSettings.fog,
                    FogColour = RenderSettings.fogColor,
                    FogMode = RenderSettings.fogMode,
                    FogStart = RenderSettings.fogStartDistance,
                    FogEnd = RenderSettings.fogEndDistance,
                    Brightness = 1.0f,
                    Contrast = 1.0f,
                    Saturation = 1.0f,
                    Exposure = 0.0f,
                    White = 1.9f,
                };

                // ⚠️ SEARCHED INSIDE THIS SCENE, NOT WITH FindObjectOfType. Every map this screen
                // has shown stays loaded and deactivated on purpose, so a global search would
                // find whichever map's grade happened to be first and colour Eskinita with Bayan
                // Plaza's numbers.
                foreach (var root in scene.GetRootGameObjects())
                {
                    var grade = root.GetComponentInChildren<Visual.MapGrade>(true);
                    if (grade == null) continue;

                    env.Brightness = grade.Brightness;
                    env.Contrast = grade.Contrast;
                    env.Saturation = grade.Saturation;
                    env.Exposure = grade.Exposure;
                    env.White = grade.White;
                    break;
                }

                if (previous.IsValid()) SceneManager.SetActiveScene(previous);

                _envs[map] = env;
            }

            RenderSettings.ambientMode = env.Mode;
            RenderSettings.ambientLight = env.Ambient;
            RenderSettings.ambientIntensity = env.Intensity;
            RenderSettings.fog = env.Fog;
            RenderSettings.fogColor = env.FogColour;
            RenderSettings.fogMode = env.FogMode;
            RenderSettings.fogStartDistance = env.FogStart;
            RenderSettings.fogEndDistance = env.FogEnd;

            // ⚠️ THE SKY IS THE HALF THAT WAS MOST VISIBLE. Without it the preview camera clears
            // to whatever the menu's skybox is, which is nothing, so the top of every map
            // preview was flat black above the rooftops.
            if (env.Skybox != null) RenderSettings.skybox = env.Skybox;

            // The map you are picking is graded the way it will be when you play it.
            if (_camera == null) return;

            var cameraGrade = _camera.GetComponent<Visual.ColourGrade>();

            if (cameraGrade == null)
                cameraGrade = _camera.gameObject.AddComponent<Visual.ColourGrade>();

            cameraGrade.Set(env.Brightness, env.Contrast, env.Saturation,
                            env.Exposure, env.White);
        }

        /// <summary>
        /// Puts the whole arena on <see cref="PreviewLayer"/> so only this screen's camera can
        /// see it. See that constant for what leaking out of the render texture looked like.
        ///
        /// ⚠️ THE LIGHTS ARE LEFT ALONE ON PURPOSE. A directional light culls by its own mask,
        /// and the arena's sun ships with every layer set, so moving the geometry to 29 does not
        /// unlight it. Narrowing the sun here would instead unlight the arena the moment the same
        /// scene is loaded for a real match.
        /// </summary>
        private static void Confine(Scene scene)
        {
            if (!scene.IsValid()) return;

            foreach (var root in scene.GetRootGameObjects())
                SetLayerRecursively(root.transform, PreviewLayer);
        }

        private static void SetLayerRecursively(Transform t, int layer)
        {
            t.gameObject.layer = layer;

            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i), layer);
        }

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

            _camera.fieldOfView = _lobbyShot ? LobbyFieldOfView : FieldOfView;
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.depth = -10;

            // The other half of Confine: this camera draws the arena and nothing else does.
            _camera.cullingMask = 1 << PreviewLayer;

            // ⚠️ AND EVERY OTHER CAMERA IS TOLD TO IGNORE IT. The menu scenes ship a UI camera
            // with a culling mask of every layer, so confining the geometry is only half a fix:
            // without this the same slab is still drawn by whichever camera owns the screen.
            // Same shape as `ModelPreview.IsolateFromForeignLights`, and for the same reason.
            foreach (var other in FindObjectsByType<Camera>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None))
            {
                if (other == null || other == _camera) continue;

                other.cullingMask &= ~(1 << PreviewLayer);
            }

            // ⚠️ THE RENDER TEXTURE'S ASPECT DOES NOT REACH THE CAMERA ON ITS OWN. Same fault
            // `ModelPreview.EnsureTexture` carries a note about: a camera that has already
            // rendered keeps the aspect it cached, so the arena came out stretched into a 16:9
            // target. Derived from the target rather than assumed.
            _camera.aspect = (float)Width / Height;

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

            float look = _lobbyShot ? LobbyLookHeight : LookHeight;
            _camera.transform.LookAt(_pivot + new Vector3(0.0f, look, 0.0f));
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
