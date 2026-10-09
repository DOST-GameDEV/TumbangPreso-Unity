using System.IO;
using System.Linq;
using TumbangPreso;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// A TEMPORARY test map for the character reworks: a plain grid floor, grid walls, a few blocks to jump on, and the
    /// redesigned heroes standing in a row. Owner, 2026-10-06, listing the reworks still to do (first-person hand
    /// animation, the Classic cast, the skills' animation and effects): *"to test these. i want to use the same lineup
    /// temp scene but rework it so its actually just a regular prototype map. and then make it so i can press a key while
    /// looking at one of the heroes to switch to them"*.
    ///
    /// ⚠️ A SCENE OF ITS OWN, OUTSIDE THE MAP POOL (owner, 2026-10-06, of the first version, a copy of Eskinita under
    /// Eskinita's name that came up as an ordinary match: "just create a separate scene thats outside the map pool").
    /// `Scenes/Temp/CharacterPrototype.unity` is built from nothing, is not in the build settings and is not a map the
    /// game can pick. It holds the game's launcher (`MatchInstaller`), and the scene's own `PrototypeMapPlay` asks for
    /// the Training Range before the launcher wakes: one player you control, no round, no bots. The characters' look
    /// (outline and ambient occlusion) is found by scene name, so `WorldLookProfile.Find` knows this scene by name.
    ///
    /// IN PLAY: look at a hero in the row and press H to become them (`SwitchToLookedAtHero`).
    /// </summary>
    public static class CharacterPrototypeMap
    {
        private const string Folder = "Assets/TumbangPreso/Scenes/Temp";
        private const string Target = Folder + "/CharacterPrototype.unity";
        private const string OldTarget = Folder + "/Eskinita.unity";
        private const string GridTexture = Folder + "/prototype-grid.png";
        private const string GridMaterial = Folder + "/prototype-grid.mat";
        private const string DarkMaterial = Folder + "/prototype-grid-dark.mat";
        private const string Trigger = "Temp/character-prototype-map.request";
        private const string LineupRoot = "REDESIGN LINEUP (temp)";
        /// <summary>Half the side of the walled square, and how far the floor runs past it.</summary>
        private const float Half = 22f, Apron = 30f, WallHeight = 6f;
        /// <summary>How close to the middle of the view a hero has to be to be picked, in degrees.</summary>
        private const float PickCone = 10f;
        /// <summary>How far behind the heroes the Classic row stands.</summary>
        private const float ClassicRowGap = 4f;
        private static readonly string[] ClassicIds =
        {
            "bayan", "maring", "totoy", "inday", "kuya_boy", "ate_girlie",
            "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena",
        };

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            RequestRange();
            if (!File.Exists(Trigger)) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Trigger) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                // "roster" in the request rebuilds the roster book first: the Classic twelve on their redesigns, and
                // everyone's first-person arms cut again from the models they now wear.
                bool roster = File.ReadAllText(Trigger).Contains("roster");
                File.Delete(Trigger);
                if (roster) CharacterRedesignLineup.UseRedesignsInRoster();
                Build();
            };
        }

        [MenuItem("Tumbang Preso/Character Redesign/Build Prototype Map (temp scene)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[PrototypeMap] Leave play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/TumbangPreso/Scenes", "Temp");
            AssetDatabase.DeleteAsset(Target);
            // The first version was a copy of Eskinita under Eskinita's own name; it is removed with the rebuild.
            AssetDatabase.DeleteAsset(OldTarget);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const float ground = 0f;
            int removed = 0;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f, 0.58f, 0.64f);
            // The game's launcher builds the player, the camera and the HUD in code, and carries nothing a map sets but
            // these two switches (as every map has on its own launcher object).
            new GameObject("~Match").AddComponent<MatchInstaller>();

            var light = new GameObject("Prototype Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.05f; light.color = new Color(1f, 0.98f, 0.95f);
            light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var pale = MakeMaterial(GridMaterial, new Color(0.78f, 0.80f, 0.86f));
            var dark = MakeMaterial(DarkMaterial, new Color(0.50f, 0.54f, 0.64f), mapShader: true);

            // `MatchInstaller.MeasurePlayableBounds` reads the boxes under a node called `Bounds`: the tall ones are the
            // walls a thrown tsinelas comes back from, the flat one is ground.
            var bounds = new GameObject("Bounds");
            float reach = Half + Apron;
            Box(bounds.transform, "Ground", new Vector3(0f, ground - 0.5f, 0f), new Vector3(reach * 2f, 1f, reach * 2f), pale);
            Box(bounds.transform, "Wall North", new Vector3(0f, ground + WallHeight * .5f, Half + .5f), new Vector3(Half * 2f + 2f, WallHeight, 1f), dark);
            Box(bounds.transform, "Wall South", new Vector3(0f, ground + WallHeight * .5f, -Half - .5f), new Vector3(Half * 2f + 2f, WallHeight, 1f), dark);
            Box(bounds.transform, "Wall East", new Vector3(Half + .5f, ground + WallHeight * .5f, 0f), new Vector3(1f, WallHeight, Half * 2f), dark);
            Box(bounds.transform, "Wall West", new Vector3(-Half - .5f, ground + WallHeight * .5f, 0f), new Vector3(1f, WallHeight, Half * 2f), dark);

            // Things to move on: three steps, a long low platform, a tall block, a doorway's worth of wall.
            var blocks = new GameObject("Prototype Blocks");
            for (int i = 0; i < 3; i++)
                Box(blocks.transform, "Step " + (i + 1), new Vector3(-14f + i * 2f, ground + .25f * (i + 1), -10f), new Vector3(2f, .5f * (i + 1), 4f), dark);
            Box(blocks.transform, "Platform", new Vector3(-6f, ground + .75f, -10f), new Vector3(6f, 1.5f, 4f), dark);
            Box(blocks.transform, "Tall Block", new Vector3(14f, ground + 1.5f, -10f), new Vector3(4f, 3f, 4f), dark);
            Box(blocks.transform, "Low Wall", new Vector3(14f, ground + .5f, 4f), new Vector3(6f, 1f, .5f), dark);
            Box(blocks.transform, "Cover", new Vector3(-14f, ground + 1f, 4f), new Vector3(.5f, 2f, 5f), dark);
            // ⚠️ SOMETHING TO SWING FROM (owner, 2026-10-07: "add a long hanging block to the prototype map"): a beam
            // 16 m long in the open, nothing under it, its underside 5.5 m up and its top 6.5 m. Paete's LIANA LEAP
            // reaches 8 m: aimed at its underside he swings under it, aimed at its side he goes over onto it.
            Box(blocks.transform, "Hanging Beam", new Vector3(0f, ground + 6f, 3f), new Vector3(16f, 1f, 1f), dark);

            // The heroes, in a row behind the court, facing it.
            var root = new GameObject(LineupRoot);
            var centre = new Vector3(0f, ground, 11f);
            root.transform.position = centre;
            var book = RosterBook.Load();
            var heroes = CharacterRedesignLineup.Heroes;
            const float spacing = 2.6f;
            int placed = 0;
            var figures = new System.Collections.Generic.List<Transform>();
            var ids = new System.Collections.Generic.List<string>();
            Physics.SyncTransforms();
            for (int i = 0; i < heroes.Length; i++)
            {
                var (id, name) = heroes[i];
                var entry = book != null ? book.People.FirstOrDefault(p => p != null && p.Id == id) : null;
                string model = "Assets/TumbangPreso/Art/CharacterRedesign/" + id + "/" + id + "-redesign.glb";
                float x = (i - (heroes.Length - 1) * .5f) * spacing;
                var figure = CharacterRedesignLineup.Place(model, name + " (redesign)", root.transform, centre + new Vector3(x, 0f, 0f), entry != null ? entry.Palette : null);
                if (figure == null) continue;
                placed++; figures.Add(figure.transform); ids.Add(id);
            }
            // What the map does in play: no can, skills always ready, and H on the hero you are looking at.
            var play = root.AddComponent<Diagnostics.PrototypeMapPlay>();
            play.Figures = figures.ToArray(); play.HeroIds = ids.ToArray();

            // ⚠️ THE CLASSIC TWELVE, A SECOND ROW BEHIND THE HEROES (owner, 2026-10-07: "put them into the
            // CharacterPrototype map as another row of characters"). They are not in the hero roster the range plays,
            // so each figure carries its own model and clips, and H puts that BODY on the player (`PrototypeMapPlay`):
            // the kit and the first-person arms stay the hero's.
            var classics = new System.Collections.Generic.List<Diagnostics.PrototypeMapPlay.Body>();
            for (int i = 0; i < ClassicIds.Length; i++)
            {
                string id = ClassicIds[i];
                string model = "Assets/TumbangPreso/Art/CharacterRedesign/" + id + "/" + id + "-redesign.glb";
                float x = (i - (ClassicIds.Length - 1) * .5f) * spacing;
                var figure = CharacterRedesignLineup.Place(model, id + " (classic redesign)", root.transform, centre + new Vector3(x, 0f, ClassicRowGap), null);
                if (figure == null) continue;
                placed++;
                classics.Add(new Diagnostics.PrototypeMapPlay.Body
                {
                    Id = id, Figure = figure.transform, Model = AssetDatabase.LoadAssetAtPath<GameObject>(model),
                    Clips = AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray(),
                });
            }
            play.Classics = classics.ToArray();

            // ⚠️ THE TALL BLOCK NEEDS A WAY UP (owner, 2026-10-06: "theres also no way to get up the huge block, should
            // probably put a jump pad there, we already have one"). The game's own pad, at the block's foot: 13 m/s
            // tops out at 4.2 m, over the block's 3 m. A second, at the pad's own 24 m/s (14 m up), stands in the open
            // for a long fall to watch the hands and the landing with.
            Pad("Jump Pad (onto the tall block)", new Vector3(14f, ground, -7.0f), 13f);
            Pad("Jump Pad (high, for a long fall)", new Vector3(6f, ground, -3f), 24f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Target);
            Selection.activeGameObject = root;
            var view = SceneView.lastActiveSceneView;
            if (view != null) view.LookAt(centre + Vector3.up, Quaternion.Euler(18f, 0f, 0f), 14f);
            Debug.Log("[PrototypeMap] Built " + Target + ": " + placed
                + " figures: the heroes in front, the Classic twelve behind. Press Play, look at one and press H to become them.");
        }

        private static void Pad(string name, Vector3 at, float speed)
        {
            var go = new GameObject(name);
            go.transform.position = at;
            go.AddComponent<JumpPad>().LaunchSpeed = speed;
        }

        /// <summary>
        /// A box with its texture laid in METRES on every face, so the grid's squares are one metre wherever they are.
        /// A stock cube stretches one copy of the texture over each face whatever its size.
        /// </summary>
        private static void Box(Transform parent, string name, Vector3 centre, Vector3 size, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.isStatic = true;
            var h = size * .5f;
            var v = new System.Collections.Generic.List<Vector3>();
            var n = new System.Collections.Generic.List<Vector3>();
            var uv = new System.Collections.Generic.List<Vector2>();
            var tri = new System.Collections.Generic.List<int>();
            void Face(Vector3 normal, Vector3 right, Vector3 up, float w, float t, float d)
            {
                int at = v.Count;
                Vector3 c = normal * d;
                v.Add(c - right * w - up * t); v.Add(c - right * w + up * t); v.Add(c + right * w + up * t); v.Add(c + right * w - up * t);
                for (int i = 0; i < 4; i++) n.Add(normal);
                // World metres, so neighbouring boxes share one grid.
                foreach (int i in new[] { at, at + 1, at + 2, at + 3 })
                {
                    Vector3 world = centre + v[i];
                    uv.Add(new Vector2(Vector3.Dot(world, right), Vector3.Dot(world, up)));
                }
                tri.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
            }
            Face(Vector3.up, Vector3.right, Vector3.forward, h.x, h.z, h.y);
            Face(Vector3.down, Vector3.left, Vector3.forward, h.x, h.z, h.y);
            Face(Vector3.forward, Vector3.left, Vector3.up, h.x, h.y, h.z);
            Face(Vector3.back, Vector3.right, Vector3.up, h.x, h.y, h.z);
            Face(Vector3.right, Vector3.forward, Vector3.up, h.z, h.y, h.x);
            Face(Vector3.left, Vector3.back, Vector3.up, h.z, h.y, h.x);
            var mesh = new Mesh { name = "Prototype " + name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static Material MakeMaterial(string path, Color tint, bool mapShader = false)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexture);
            if (texture == null)
            {
                // Two metres of checker to a tile: four one-metre squares, a fine line every metre, a bolder one every two.
                const int size = 256;
                var image = new Texture2D(size, size, TextureFormat.RGB24, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        bool odd = ((x * 2 / size) + (y * 2 / size)) % 2 == 1;
                        float value = odd ? .80f : .94f;
                        int cx = x % (size / 2), cy = y % (size / 2);
                        if (cx < 2 || cy < 2) value = 1f;
                        if (x < 3 || y < 3) value = .62f;
                        // A cross of dots at each quarter metre, as a builder's grid has.
                        if ((cx % (size / 8) == 0 && cy % (size / 8) < 2) || (cy % (size / 8) == 0 && cx % (size / 8) < 2)) value = Mathf.Min(value, .88f);
                        image.SetPixel(x, y, new Color(value, value, value));
                    }
                image.Apply();
                File.WriteAllBytes(GridTexture, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(GridTexture);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexture);
                var importer = AssetImporter.GetAtPath(GridTexture) as TextureImporter;
                if (importer != null) { importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 8; importer.SaveAndReimport(); }
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            // ⚠️ THE WALLS AND BLOCKS WEAR THE MAP'S OWN SHADER (2026-10-08), THE FLOOR DOES NOT. The shipped maps' dressing wears
            // `TumbangPreso/NearFade` (`EnvColourPass`), and what it does is part of what is tested here: the screen-door at the
            // lens, and the sky opened through the walls for Paete's ultimate (owner: "in the prototype map, the walls cover most
            // of her body"). Its surface properties are Standard's own names, so the grid and its tint carry over.
            // ⚠️ The floor went onto it too at first and the owner, crouched, saw it stipple away round him ("why is the distance
            // fade happening"): the shader spares the ground only when it is a standing eye's height below the lens (0.8 m or
            // more), and a crouch or a slide puts the eye lower than that. So the floor stays Standard here. The same will
            // happen on a block's top when he crouches on one, and on any shipped ground that wears this shader: that is the
            // shader's rule meeting the movement rework's crouch, and is theirs to settle.
            var wanted = mapShader ? Shader.Find(TumbangPreso.Visual.NearFade.ShaderName) : Shader.Find("Standard");
            if (wanted != null && material.shader != wanted) material.shader = wanted;
            // ⚠️ AND THE WALLS DO NOT FADE AT THE LENS HERE (owner, 2026-10-08, walking up to one: "distance fade isnt just
            // happening in the floor, but also the walls, outside of the cutscene"; "it just fades out when im too near").
            // They are on the map's shader only so the cutscene can open the sky through them; its near band is shut (a
            // millimetre), which leaves the sky's window working and nothing else.
            if (mapShader) { material.SetFloat("_NearFadeStart", 0.001f); material.SetFloat("_NearFadeEnd", 0f); }
            material.mainTexture = texture;
            // The mesh's UVs are metres; the tile is two metres.
            material.mainTextureScale = new Vector2(.5f, .5f);
            material.color = tint;
            material.SetFloat("_Glossiness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The same launch the menu's practice button sets, when Play starts in this scene.</summary>
        private static void RequestRange()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Target) return;
            GameLaunch.Reset();
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;
            GameLaunch.TrainingRange = true;
            GameLaunch.SelectedMap = "eskinita";
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SelectedMode = Core.GameMode.HeroStrike;
            Debug.Log("[PrototypeMap] Play: Training Range requested. Look at a hero and press H to become them.");
        }

        private static bool _pickHeld;

        /// <summary>
        /// ⚠️ LOOK AT A HERO IN THE ROW AND PRESS H. The hero nearest the middle of the view, within `PickCone` degrees,
        /// becomes the player's character through the Training Range's own switch (`PracticeRange.ChangeCharacter`), which
        /// is what its pause menu calls: the kit, the body and the first-person arms all change. Only in this scene, only
        /// in play.
        /// </summary>
        private static void SwitchToLookedAtHero()
        {
            if (!Application.isPlaying) return;
            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (keys == null) return;
            bool down = keys.hKey.isPressed;
            bool pressed = down && !_pickHeld;
            _pickHeld = down;
            if (!pressed) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Target) return;
            var range = PracticeRange.Instance;
            var camera = Camera.main;
            var root = GameObject.Find(LineupRoot);
            if (range == null || range.Local == null || camera == null || root == null) return;

            string best = null; float bestAngle = PickCone;
            foreach (Transform figure in root.transform)
            {
                var hero = CharacterRedesignLineup.Heroes.FirstOrDefault(h => figure.name.StartsWith(h.Name + " ("));
                if (hero.Id == null) continue;
                var renderers = figure.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var box = renderers[0].bounds;
                foreach (var r in renderers) box.Encapsulate(r.bounds);
                float angle = Vector3.Angle(camera.transform.forward, box.center - camera.transform.position);
                if (angle < bestAngle) { bestAngle = angle; best = hero.Id; }
            }
            if (best == null) { Debug.Log("[PrototypeMap] H: no hero near the middle of the view."); return; }

            var roster = Core.Roster.GetPeople(range.Local.Mode);
            int index = -1;
            for (int i = 0; i < roster.Count; i++) if (roster[i].Id == best) { index = i; break; }
            bool ok = index >= 0 && range.ChangeCharacter(index);
            Debug.Log("[PrototypeMap] H: " + (ok ? "now playing " + best : "could not switch to " + best + " (index " + index + ")") + ".");
        }
    }
}
