using System.IO;
using System.Linq;
using TumbangPreso;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// A TEMPORARY review scene: the redesigned heroes lined up on Ilalim ng Tulay, each with the
    /// hero it replaces standing behind it. Owner, 2026-10-05: *"set up a temp unity scene in the
    /// map ilalim ng tulay with the designs of the new characters lined up"*.
    ///
    /// ⚠️ IT NEVER TOUCHES THE REAL MAP. `IlalimNgTulay.unity` is COPIED to
    /// `Scenes/Temp/RedesignLineup_Ilalim.unity` and only the copy is opened and saved. The copy
    /// is not in the build settings. Delete the `Scenes/Temp` folder and this file when the
    /// review is over (docs/CHARACTER_REDESIGN_DANTE.md).
    ///
    /// The models are dressed the way the game dresses them (`ToonSkin.Apply`, `PersonScale`),
    /// and posed on frame 0 of their own `idle`. Nothing animates in edit mode.
    /// </summary>
    public static class CharacterRedesignLineup
    {
        private const string Source = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string Folder = "Assets/TumbangPreso/Scenes/Temp";
        // ⚠️ THE COPY KEEPS THE MAP'S NAME. The map's bright look, and with it the outline and the
        // ambient occlusion, is found BY SCENE NAME (`WorldLookProfile.Find`). Under the first name,
        // `RedesignLineup_Ilalim`, the test map had no look at all, so no occlusion could show on
        // it. Owner, 2026-10-05: "i dont think the ao is enabled on the test map". It is told apart
        // from the real one by its folder, and it is not in the build settings.
        private const string Target = Folder + "/IlalimNgTulay.unity";
        private const string OldTarget = Folder + "/RedesignLineup_Ilalim.unity";
        private const string Trigger = "Temp/character-redesign-lineup.request";
        private const float Spacing = 2.3f;
        private const float RowGap = 3.2f;

        /// <summary>
        /// ⚠️ THE SMALL-HEAD TEST. Owner, 2026-10-05: *"try making ones with a smaller head too, add it
        /// to the lineup. make a toggle so i can test the current vs the small head design"*.
        /// Each redesign stands in the row TWICE at the same spot: once as built, once with its
        /// `head` bone scaled by this about the neck, so the hair, ears and anything else skinned
        /// to the head shrink with it and the body is untouched. Only one of the two is shown;
        /// the toggle swaps them in place, which is the only fair way to compare a proportion.
        /// It is a bone scale, not a rebuilt model: cloth that is partly weighted to the head (a
        /// collar, a cowl) is pulled in a little with it. If a size is chosen, the heads get
        /// rebuilt at that size in the Blender scripts.
        /// </summary>
        private const float SmallHead = 0.84f;
        private const string SmallTag = " (redesign, small head)";
        private const string CurrentTag = " (redesign)";

        private static readonly (string Id, string Name)[] Heroes =
        {
            ("dante", "Basilio"), ("cheska", "Yasmin"), ("sean", "Rago"), ("zack", "Isagani"),
            ("nemu", "Nemu"), ("rafi", "Ilyas"), ("amihan", "Amihan"),
            // Owner, 2026-10-05: "should probably work on a paete and phaister rework". Lineup figures
            // only; the roster still plays their real models.
            ("phaister", "Soraya"), ("paete", "Paete"),
        };

        [InitializeOnLoadMethod]
        private static void RunOnceWhenAsked()
        {
            if (!File.Exists(Trigger)) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Trigger) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                bool roster = File.ReadAllText(Trigger).Contains("roster");
                File.Delete(Trigger);
                if (roster) UseRedesignsInRoster();
                Build();
            };
        }

        /// <summary>
        /// Owner, 2026-10-05: "we'll be using the character redesigns from now on instead of the older
        /// models". Rebuilds the roster book from `RosterBookBuilder.PersonModels` (all nine heroes
        /// point at their redesigns) and authors the swim and recovery clip sets a new rig lacks,
        /// which a player build checks for. Existing sets are left alone.
        /// </summary>
        [MenuItem("Tumbang Preso/Character Redesign/Use Redesigns In Roster")]
        public static void UseRedesignsInRoster()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[RedesignLineup] Leave play mode first."); return; }
            RosterBookBuilder.BuildFromMenu();
            bool swim = SwimmingAnimationAuthor.EnsureMissing();
            bool recovery = RecoveryAnimationAuthor.EnsureMissing();
            bool valid = AuthoredAnimationBuildCheck.Validate(out int rigs, out string error);
            Debug.Log("[RedesignLineup] Roster rebuilt on the redesigns. Swim sets " + (swim ? "ok" : "FAILED")
                + ", recovery sets " + (recovery ? "ok" : "FAILED")
                + (valid ? ", " + rigs + " rigs validated." : ", validation FAILED: " + error));
        }

        [MenuItem("Tumbang Preso/Character Redesign/Build Lineup On Ilalim (temp scene)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[RedesignLineup] Leave play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/TumbangPreso/Scenes", "Temp");
            AssetDatabase.DeleteAsset(Target);
            AssetDatabase.DeleteAsset(OldTarget);
            if (!AssetDatabase.CopyAsset(Source, Target))
            { Debug.LogError("[RedesignLineup] Could not copy " + Source); return; }

            var scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
            // ⚠️ THE SCENE PLAYS AS THE GAME'S OWN TRAINING RANGE. Owner, 2026-10-05: first "for this
            // scene i need the gameplay to be completely gone, and no startup camera cinematic",
            // then "for no gameplay, just add a player character so i can still move around". A
            // player is not a prefab here: `MatchInstaller` builds the body, the input, the camera
            // rig and the HUD in code, so the launcher STAYS in the copy and is told to run the
            // Training Range (`GameLaunch.TrainingRange`, see `RequestRange`): one player you
            // control, no ready gate, no round that starts by itself, characters changed from the
            // pause menu.
            var spawn = GameObject.Find("Spawn0");
            Vector3 centre = spawn != null ? spawn.transform.position : Vector3.zero;

            var root = new GameObject("REDESIGN LINEUP (temp)");
            // Behind the court, clear of the can and of where the player is put down.
            centre += new Vector3(0.0f, 0.0f, 6.0f);
            root.transform.position = centre;
            var book = RosterBook.Load();
            float start = -0.5f * Spacing * (Heroes.Length - 1);
            var bounds = new Bounds(centre + Vector3.up, Vector3.one);
            int placed = 0;

            for (int i = 0; i < Heroes.Length; i++)
            {
                var (id, name) = Heroes[i];
                var entry = book != null ? book.People.FirstOrDefault(p => p != null && p.Id == id) : null;
                Color[] palette = entry != null ? entry.Palette : null;
                float x = start + i * Spacing;

                string redesign = "Assets/TumbangPreso/Art/CharacterRedesign/" + id + "/" + id + "-redesign.glb";
                var front = Place(redesign, name + CurrentTag, root.transform, centre + new Vector3(x, 0.0f, 0.0f), palette);
                // (The bone-scaled "small head" copies stood here for the comparison. The owner chose the
                // small head, 0.84 of the first size, and the heads are now BUILT at that size in the
                // Blender scripts, so there is one figure a hero again.)
                // The hero it replaces, straight behind, for a like-for-like look. The original's
                // palette is the roster entry's own, which the swap did not change.
                string original = "Assets/TumbangPreso/Art/characters/persons/team-" + id + ".glb";
                var back = Place(original, name + " (original)", root.transform, centre + new Vector3(x, 0.0f, RowGap), palette);

                foreach (var go in new[] { front, back })
                {
                    if (go == null) continue;
                    placed++;
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                }
            }

            // A camera for the Game view, and the Scene view framed on the row.
            var camGo = new GameObject("Lineup Camera (temp)");
            camGo.transform.SetParent(root.transform, true);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 32.0f;
            camGo.transform.position = centre + new Vector3(0.0f, 2.2f, -15.5f);
            camGo.transform.LookAt(centre + new Vector3(0.0f, 1.1f, 1.2f));
            // Off: in play the game's own camera follows the player. Turn it on by hand for a
            // fixed view of the row in the Game view while NOT playing.
            camGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.Frame(bounds, true);
                view.LookAt(bounds.center, Quaternion.Euler(8.0f, 0.0f, 0.0f), bounds.extents.x * 1.25f);
            }
            Debug.Log("[RedesignLineup] " + placed + " figures placed in " + Target
                + ". Front row: redesigns. Back row: the originals. The real Ilalim scene was not touched.");
        }

        private const string SmoothPref = "TumbangPreso.CharacterRedesign.SmoothShade";
        private static readonly int SmoothShadeId = Shader.PropertyToID("_CharacterSmoothShade");
        private const string RadiusPref = "TumbangPreso.CharacterRedesign.AoRadius";
        //   metres. 0.35 is the look's own; the smaller ones keep the shade inside a crease.
        private static readonly float[] Radii = { 0.14f, 0.09f, 0.22f, 0.35f };

        /// <summary>
        /// ⚠️ A LOOK TEST, EDITOR ONLY. Owner, 2026-10-05: *"i think we have a celshader set on for
        /// the character shading in game, can we use regular shading + AO"*. Flips the toon
        /// shader's global `_CharacterSmoothShade` (see `Toon.shader`, § SMOOTH SHADING) and, with
        /// it, the cast's share of the screen-space ambient occlusion (`WorldOutline.CharacterAoTest`,
        /// the F8 test switch's own value). The occlusion is a camera effect of the map's world
        /// look, so it only shows in PLAY mode on a map; the smooth shading shows everywhere,
        /// the edit-mode lineup included. Remembered across reloads; a built player is untouched.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RestoreShading()
        {
            RequestRange();
            ApplyShading(EditorPrefs.GetBool(SmoothPref, true));
            EditorApplication.playModeStateChanged += change =>
            {
                ApplyShading(EditorPrefs.GetBool(SmoothPref, true));
                if (change == PlayModeStateChange.EnteredPlayMode) { RedressLineup(); WearTheLook(); }
                _flyYaw = float.NaN;
            };
            // The game writes its own look every frame it draws; keep ours on top of it.
            EditorApplication.update += Fly;
            EditorApplication.update += () =>
            {
                if (Shader.GetGlobalFloat(SmoothShadeId) != (EditorPrefs.GetBool(SmoothPref, true) ? 1.0f : 0.0f))
                    ApplyShading(EditorPrefs.GetBool(SmoothPref, true));
            };
        }

        /// <summary>
        /// ⚠️ THE ORIGINALS WENT SOLID BLACK IN PLAY MODE (owner's screenshot, 2026-10-05). They take
        /// their colours from the roster PALETTE, which `ToonSkin.Apply` hands to the renderer at
        /// dress time and which a saved scene does not keep; the redesigns paint from a texture
        /// and survived. So the whole lineup is dressed again when play starts.
        /// </summary>
        private static void RedressLineup()
        {
            var root = GameObject.Find("REDESIGN LINEUP (temp)");
            if (root == null) return;
            var book = RosterBook.Load();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.parent != root.transform || child.childCount == 0) continue;
                var hero = Heroes.FirstOrDefault(h => child.name.StartsWith(h.Name + " ("));
                if (hero.Id == null) continue;
                var entry = book != null ? book.People.FirstOrDefault(p => p != null && p.Id == hero.Id) : null;
                ToonSkin.Apply(child.GetChild(0).gameObject, ToonSkin.PersonOutlineWidth, entry != null ? entry.Palette : null);
            }
        }

        /// <summary>
        /// ⚠️ THE TEST MAP HAS NO GAME CAMERA. A map scene carries none; a match spawns its rig, and
        /// the outline, the ambient occlusion and the map's bright look all ride on that camera.
        /// The lineup's own camera was a bare one, so in play it drew no occlusion whatever the
        /// switches said. When play starts in the lineup, its camera is dressed the way the map's
        /// own editor previews dress theirs (`IlalimSceneBuilder`): grade, outline, look.
        /// </summary>
        private static void WearTheLook()
        {
            var root = GameObject.Find("REDESIGN LINEUP (temp)");
            if (root == null) return;
            // With the launcher in the scene the game dresses its own camera.
            if (Object.FindAnyObjectByType<MatchInstaller>() != null) return;
            var camera = root.GetComponentInChildren<Camera>(true);
            if (camera == null) return;
            camera.tag = "MainCamera";
            camera.depthTextureMode |= DepthTextureMode.DepthNormals;
            try
            {
                if (camera.GetComponent<ColourGrade>() == null) camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                if (camera.GetComponent<WorldOutline>() == null) camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
                if (camera.GetComponent<WorldLookCamera>() == null) camera.gameObject.AddComponent<WorldLookCamera>();
                WorldLookPresentation look = Object.FindAnyObjectByType<WorldLookPresentation>();
                if (look == null)
                {
                    var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                    look = WorldLookPresentation.InstallPreview(root.transform, root.transform.position.y, sun);
                }
                Debug.Log("[RedesignLineup] Play: the lineup camera " + (look != null
                    ? "wears the look of " + look.Look.Map + ", with the outline and ambient occlusion."
                    : "has the outline effect, but NO world look was found for this scene, so there is no ambient occlusion."));
            }
            catch (System.Exception e) { Debug.LogWarning("[RedesignLineup] Could not dress the lineup camera: " + e.Message); }
        }

        /// <summary>
        /// Runs after every domain reload, which includes the one that starts play, and before the
        /// scene's objects wake. If play is starting in the lineup scene, the launch is set the way
        /// the menu's own practice button sets it (`ConvertedMatchSetup.StartPractice`).
        /// </summary>
        private static void RequestRange()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Target) return;
            GameLaunch.Reset();
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;
            GameLaunch.TrainingRange = true;
            GameLaunch.SelectedMap = "ilalim_ng_tulay";
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SelectedMode = Core.GameMode.HeroStrike;
            Debug.Log("[RedesignLineup] Play: Training Range requested. Requested = " + PracticeRange.Requested);
        }

        private static float _flyYaw = float.NaN, _flyPitch;
        private static double _flyClock;

        /// <summary>
        /// With no match there is no player to walk about as, so in play the lineup camera flies:
        /// hold the RIGHT mouse button to look, WASD to move, Q and E down and up, Shift faster.
        /// Only in the lineup scene, only in play mode. The Game view shows the outline and the
        /// ambient occlusion; the Scene view does not draw them.
        /// </summary>
        private static void Fly()
        {
            if (!Application.isPlaying) return;
            var root = GameObject.Find("REDESIGN LINEUP (temp)");
            var camera = root != null ? root.GetComponentInChildren<Camera>() : null;
            var keys = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (camera == null || keys == null || mouse == null) return;
            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Clamp((float)(now - _flyClock), 0.0f, 0.1f);
            _flyClock = now;
            var t = camera.transform;
            if (float.IsNaN(_flyYaw)) { _flyYaw = t.eulerAngles.y; _flyPitch = Mathf.DeltaAngle(0.0f, t.eulerAngles.x); }
            if (mouse.rightButton.isPressed)
            {
                var look = mouse.delta.ReadValue();
                _flyYaw += look.x * 0.12f;
                _flyPitch = Mathf.Clamp(_flyPitch - look.y * 0.12f, -85.0f, 85.0f);
                t.rotation = Quaternion.Euler(_flyPitch, _flyYaw, 0.0f);
            }
            var move = new Vector3(
                (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.eKey.isPressed ? 1 : 0) - (keys.qKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            if (move.sqrMagnitude > 0.0f)
                t.position += t.TransformDirection(move.normalized) * (keys.leftShiftKey.isPressed ? 9.0f : 3.0f) * dt;
        }

        private static void ApplyShading(bool smooth)
        {
            Shader.SetGlobalFloat(SmoothShadeId, smooth ? 1.0f : 0.0f);
            // Smooth with occlusion is the game's own look now, so the comparison is against it OFF.
            WorldOutline.CharacterAoTest = smooth ? 1.0f : 0.0f;
            WorldOutline.CharacterAoRadiusTest = smooth ? EditorPrefs.GetFloat(RadiusPref, Radii[0]) : -1.0f;
        }

        [MenuItem("Tumbang Preso/Character Redesign/Cycle Character AO Size %#k")]
        public static void CycleAoRadius()
        {
            float now = EditorPrefs.GetFloat(RadiusPref, Radii[0]);
            int at = System.Array.FindIndex(Radii, r => Mathf.Abs(r - now) < 0.001f);
            float next = Radii[(at + 1) % Radii.Length];
            EditorPrefs.SetFloat(RadiusPref, next);
            ApplyShading(EditorPrefs.GetBool(SmoothPref, true));
            Debug.Log("[RedesignLineup] Character AO size: " + next.ToString("0.00") + " m"
                + (Mathf.Abs(next - 0.35f) < 0.001f ? " (the game's own)" : "")
                + ". It shows in play mode on a map, with smooth shading on.");
        }

        [MenuItem("Tumbang Preso/Character Redesign/Toggle Shading (cel or smooth + AO) %#j")]
        public static void ToggleShading()
        {
            bool smooth = !EditorPrefs.GetBool(SmoothPref, true);
            EditorPrefs.SetBool(SmoothPref, smooth);
            ApplyShading(smooth);
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log("[RedesignLineup] Character shading: " + (smooth
                ? "SMOOTH, with the cast's ambient occlusion on (the occlusion shows in play mode on a map)"
                : "CEL (the shipped two-band look)") + ".");
        }

        private static GameObject Place(string path, string label, Transform parent, Vector3 at, Color[] palette)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[RedesignLineup] Missing model: " + path); return null; }

            var pivot = new GameObject(label);
            pivot.transform.SetParent(parent, true);
            // Faces the lineup camera, which stands at -z.
            pivot.transform.SetPositionAndRotation(at, Quaternion.Euler(0.0f, 180.0f, 0.0f));

            var model = Object.Instantiate(prefab, pivot.transform);
            model.name = Path.GetFileNameWithoutExtension(path);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw, 0.0f);
            model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (sub is AnimationClip clip && clip.name == "idle")
                { clip.SampleAnimation(model, 0.0f); break; }
            }

            // Feet on the ground under the figure: the lowest rendered point goes to the surface a
            // ray finds below it, or to the spawn's own height where nothing is hit.
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return pivot;
            float low = renderers.Min(r => r.bounds.min.y);
            float ground = at.y;
            if (Physics.Raycast(at + Vector3.up * 5.0f, Vector3.down, out var hit, 20.0f, ~0, QueryTriggerInteraction.Ignore))
                ground = hit.point.y;
            pivot.transform.position += Vector3.up * (ground - low);
            return pivot;
        }
    }
}
