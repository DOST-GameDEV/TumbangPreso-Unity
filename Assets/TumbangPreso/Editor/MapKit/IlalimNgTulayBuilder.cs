using System.Collections.Generic;
using System.IO;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Builds "Ilalim ng Tulay", the LRT Gilmore strip: a length of Aurora Boulevard under the
    /// elevated guideway, with a PC Express showroom on one pavement and a pisonet and a pares
    /// cart on the other.
    ///
    /// ⚠️⚠️ THE CHALK BOX IS THE CARRIAGEWAY, AND THAT IS THE WHOLE MAP. `Balance.ConfinementRadius`
    /// is 7.0, so the defender's box is 14 m across, and the road here is 14 m across kerb to
    /// kerb because it is drawn FROM that constant. The taya is confined to the tarmac; the
    /// attackers work from the pavements and from the two ends of the street. A player can read
    /// where they are allowed to stand off the kerb line without ever looking at the chalk,
    /// which is the point.
    ///
    /// ⚠️⚠️ THE FIRST VERSION OF THIS MAP HAD NO CHALK AT ALL. It drew a "throwing line" at
    /// z = 3.0 and a "base circle" at z = 13.5, neither derived from anything, while the can
    /// spawns at the world origin (`MatchInstaller.BuildLata`) and the box is centred there.
    /// The circle was therefore 13.5 m from the can it was drawn for. Everything positional in
    /// here now comes from `Balance` and `Confinement` or from a measured model bound, and
    /// `MapGeometryCheck` refuses the scene if that stops being true.
    ///
    /// ⚠️ EVERY PROP IS PLACED WITH <see cref="SurfaceTop"/> AND NEVER WITH A TYPED-IN HEIGHT.
    /// The old builder put pavement props at y = 0.15, which is the plaza tile's ORIGIN. The
    /// tile is 0.062 m thick, so every chair, kiosk, cart and pole on both pavements stood
    /// 62 mm inside the floor, and the pavement itself floated 0.15 m over open air because
    /// nothing was ever built underneath it.
    /// </summary>
    public static class IlalimNgTulayBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string ModelsDir = "Assets/TumbangPreso/Art/models";
        private const string KitsDir = ModelsDir + "/kits";

        private static readonly Dictionary<(Material, Texture2D), Material> KitMaterials =
            new Dictionary<(Material, Texture2D), Material>();

        // ------------------------------------------------------------------
        // The cross section. Everything in the file is derived from this block.
        // ------------------------------------------------------------------

        /// <summary>Top of the asphalt, and the height the collision floor is set to.</summary>
        /// <remarks>
        /// ⚠️ ZERO, NOT 0.06. The can spawns at `Vector3.zero`, the chalk is drawn at y = 0.01
        /// and the character controllers stand on the `Bounds` floor collider whose top is 0.
        /// The road tile mesh is 0.060 m thick, so it is SUNK by its own thickness to bring its
        /// visible surface up to the plane the rules already use. Lifting the collision floor to
        /// meet the tile instead would leave the can hovering 60 mm over its own map.
        /// </remarks>
        public const float RoadTop = 0.000f;

        /// <summary>Measured from `env_road_tile.obj`: y spans 0.000 to 0.060.</summary>
        public const float RoadTileThickness = 0.060f;

        /// <summary>Measured from `env_kerb_tile.obj`: y spans 0.000 to 0.150.</summary>
        public const float KerbTop = 0.150f;

        /// <summary>Measured from `env_kerb_tile.obj`: z spans -0.175 to 0.175.</summary>
        public const float KerbHalfDepth = 0.175f;

        /// <summary>Measured from `env_plaza_tile.obj`: y spans 0.000 to 0.062.</summary>
        public const float PlazaTileThickness = 0.062f;

        /// <summary>The walking surface of both pavements.</summary>
        public const float PavementTop = KerbTop + PlazaTileThickness;

        /// <summary>
        /// ⚠️⚠️ THE KERB LINE IS THE CHALK LINE. Derived, so it cannot drift from the rule the
        /// way the old hand-placed decals did.
        /// </summary>
        public static float RoadHalfX => Balance.ConfinementRadius;

        /// <summary>Where the pavement ends and the shopfronts begin. Also the wall face.</summary>
        public const float PavementOuterX = 11.0f;

        /// <summary>Ground under the shopfront line, so nothing in the building row stands on air.</summary>
        public const float BacklotOuterX = 20.0f;

        /// <summary>How far the street is built past the walls before the backdrop closes it.</summary>
        public const float CorridorHalfZ = 24.0f;

        /// <summary>The north and south wall faces. Unchanged from the shipped map.</summary>
        public const float WallHalfZ = 16.5f;

        /// <summary>
        /// The far ground plate.
        ///
        /// ⚠️⚠️ WITHOUT THIS THE MAP ENDED IN WHITE SKY IN EVERY DIRECTION. Three of the four
        /// showcase renders taken of the first version show bare skybox starting a metre past
        /// the pavement, because the ground was 100 tiles and nothing else. Eskinita and Bayan
        /// Plaza both carry a plate out to +/-100; this matches them.
        /// </summary>
        public const float HazeHalf = 120.0f;

        /// <summary>Top of the far plate, which is exactly where the road tiles' undersides sit.</summary>
        public const float HazeTop = RoadTop - RoadTileThickness;

        /// <summary>Underside of the LRT guideway.</summary>
        public const float ViaductSoffit = 8.0f;

        /// <summary>`road-bridge` is 0.52 m tall and is scaled 2x on Y.</summary>
        public const float GuidewayTop = ViaductSoffit + 1.04f;

        /// <summary>`track-detailed` is 0.15 m tall and sits on the guideway.</summary>
        public const float RailHead = GuidewayTop + 0.15f;

        public const float WestboundTrackX = -2.35f;
        public const float EastboundTrackX = 2.35f;
        public const float GuidewayWidth = 10.5f;
        public const float GuidewayLength = 48.0f;
        public const float TrainScale = 2.0f;
        public const float TrainConsistHalfLength = 7.8f;
        /// <summary>
        /// ⚠️ THE UNITY PREFAB ROOT IS ALREADY NORMALISED TO ITS WHEEL UNDERSIDE. The glTF
        /// POSITION accessor reaches -0.3595 in source space, but adding that correction here
        /// applies the importer's root transform twice and floats the wheels 0.719 m over rail.
        /// `MapGeometryCheck` measures the rendered prefab bounds and gates this exact join.
        /// </summary>
        public const float TrainRootY = RailHead;

        // ------------------------------------------------------------------
        // Ground tints. Named here because this is the only map whose ground is built from
        // code, so EnvColourPass (which walks a `Dressing` node it does not have) never
        // reaches it.
        // ------------------------------------------------------------------

        private static readonly Color ConcreteApron = new Color(0.640f, 0.618f, 0.576f);
        private static readonly Color HazeGround = new Color(0.788f, 0.760f, 0.700f);
        private static readonly Color ChalkWhite = new Color(0.960f, 0.950f, 0.910f);
        private static readonly Color BoostPadGlow = new Color(0.120f, 0.680f, 0.530f);
        private static readonly Color BoostPadPlum = new Color(0.550f, 0.280f, 0.620f);
        private static readonly Color BoostPadGold = new Color(0.750f, 0.580f, 0.200f);
        private static readonly Color CordYellow = new Color(0.900f, 0.800f, 0.120f);
        private static readonly Color BrothSlick = new Color(0.560f, 0.380f, 0.200f);
        private static readonly Color CardboardTan = new Color(0.720f, 0.560f, 0.340f);
        private static readonly Color PotholeGrey = new Color(0.300f, 0.300f, 0.320f);

        /// <summary>Under-bridge signage. Not tuned by eye: the first is the faded enamel red
        /// every "BAWAL UMIHI DITO" placard in the country is painted in, the second the tarpaulin
        /// blue a barangay notice is printed on.</summary>
        private static readonly Color BawalRed = new Color(0.720f, 0.180f, 0.150f);
        private static readonly Color TarpBlue = new Color(0.160f, 0.360f, 0.620f);
        private static readonly Color SignCream = new Color(0.900f, 0.830f, 0.650f);
        private static readonly Color SignMaroon = new Color(0.430f, 0.120f, 0.130f);
        private static readonly Color SignWood = new Color(0.230f, 0.150f, 0.105f);
        private static readonly Color Chalkboard = new Color(0.105f, 0.150f, 0.125f);

        [MenuItem("Tumbang Preso/Build Ilalim Ng Tulay Map")]
        public static void BuildFromMenu() => Execute();

        public static void Build()
        {
            bool ok = Execute();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Execute()
        {
            Debug.Log("[IlalimNgTulayBuilder] Starting map construction...");

            KitMaterials.Clear();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mapRoot = new GameObject("IlalimNgTulay");
            mapRoot.transform.position = Vector3.zero;
            mapRoot.AddComponent<EnvColourPass>();

            var grade = mapRoot.AddComponent<MapGrade>();
            grade.Set(1.05f, 1.12f, 1.15f, 0.15f, 1.85f);

            BuildLighting(mapRoot.transform);
            BuildGameplayRig(mapRoot.transform);
            BuildBounds(mapRoot.transform);
            BuildChalk(mapRoot.transform);

            // ⚠️⚠️ THE NODE IS CALLED `Dressing` AND ITS CHILDREN CARRY THE OTHER MAPS' GROUP
            // NAMES, AND THAT IS THE WHOLE REASON THIS MAP LOOKS LIKE THE OTHER TWO. Ilalim ng
            // Tulay shipped with everything under a node called `Geometry`, and
            // `EnvColourPass.DressingRoot` looks for a child named exactly `Dressing`: the pass
            // therefore walked nothing, repainted nothing, and printed "repainted 0 of 0" while
            // both other maps were getting the seeded Manila facade palette, the six roof
            // atlases, the warm-neutral road correction and the belt fade. That is why this map
            // read as a different game's asset pack next to Eskinita and Bayan Plaza. The names
            // below (`Kalsada`, `Apron`, `Bahay`, `Likod`, `Malayo`, `Belt`, `CrossRow`, `Kanto`)
            // are the ones already in `EnvColourPass.RoadGroups` and `FacadeGroups`, and the
            // instance prefixes (`Bahay_`, `Cross_`, `BeltX_`, `MalayoX_`) are the ones
            // `IsBuilding` already recognises. Nothing new was invented for this map; it opted
            // in to what was there.
            var dressing = new GameObject("Dressing");
            dressing.transform.SetParent(mapRoot.transform, false);

            BuildGroundAndRoads(dressing.transform);
            BuildBackdrop(dressing.transform);
            BuildHeroStructures(dressing.transform);
            BuildLrtTrainSystem(dressing.transform);
            BuildOverclockPad(dressing.transform);
            BuildStreetProps(dressing.transform);
            BuildTripHazards(dressing.transform);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(saved
                ? $"[IlalimNgTulayBuilder] Successfully created and saved {ScenePath}"
                : $"[IlalimNgTulayBuilder] FAILED to save scene at {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return saved;
        }

        /// <summary>
        /// The height of the walkable surface at a point on the street.
        ///
        /// ⚠️⚠️ NOTHING IN THIS FILE MAY WRITE A Y COORDINATE FOR A PROP BY HAND. Every one of
        /// the buried props on the shipped map came from a literal 0.15 that was correct for
        /// the road and wrong by 62 mm for the pavement. Asking here costs one call and cannot
        /// be wrong for a surface that exists.
        /// </summary>
        public static float SurfaceTop(float x)
        {
            float ax = Mathf.Abs(x);

            if (ax <= RoadHalfX - KerbHalfDepth * 2.0f) return RoadTop;
            if (ax <= RoadHalfX) return KerbTop;
            if (ax <= PavementOuterX) return PavementTop;

            return KerbTop; // the shopfront apron behind the pavement
        }

        private static void BuildLighting(Transform parent)
        {
            var lightGroup = new GameObject("Lighting");
            lightGroup.transform.SetParent(parent, false);

            // Late Gilmore afternoon: the sun comes down the street from the north east so the
            // guideway lays a hard diagonal band of shade across the carriageway.
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(lightGroup.transform, false);
            sunGo.transform.rotation = Quaternion.Euler(55.0f, 35.0f, 0.0f);

            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.4f;

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.90f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.45f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.165f, 0.208f, 0.294f);

            // ⚠️ FOG CARRIES THE LAST 40 M, WHICH THE BACKDROP GEOMETRY CANNOT. The far plate
            // stops the map ending in sky; fog stops the far plate ending in a hard line. It
            // starts past the south wall so nothing inside the walls is ever tinted by it.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.855f, 0.855f, 0.845f);
            RenderSettings.fogStartDistance = WallHalfZ + 6.0f;
            RenderSettings.fogEndDistance = 110.0f;

            // The old panorama blows out to white at every street opening. The committed warm
            // Kenney panorama carries an actual cloud horizon, so the corridor ends in Manila
            // haze rather than in an empty render background. "Morning" is the asset name; its
            // low warm sun is rotated and graded here as the same after-school hour as Eskinita.
            const string skyPath = "Assets/TumbangPreso/Art/models/materials/skyboxes/skybox-morning.png";
            var sky = AssetDatabase.LoadAssetAtPath<Texture2D>(skyPath);
            var skyShader = Shader.Find("Skybox/Panoramic");
            if (sky != null && skyShader != null)
            {
                var skyMat = new Material(skyShader) { name = "Ilalim_LateAfternoonSky" };
                skyMat.SetTexture("_MainTex", sky);
                skyMat.SetFloat("_Exposure", 0.82f);
                skyMat.SetFloat("_Rotation", 72.0f);
                RenderSettings.skybox = skyMat;
            }
        }

        private static void BuildGameplayRig(Transform parent)
        {
            var matchGo = new GameObject("~Match");
            matchGo.transform.SetParent(parent, false);
            matchGo.AddComponent<MatchInstaller>();

            var killGo = new GameObject("KillPlane");
            killGo.transform.SetParent(parent, false);
            killGo.transform.position = new Vector3(0.0f, -10.0f, 0.0f);
            killGo.AddComponent<KillPlane>();
            var kpBox = killGo.AddComponent<BoxCollider>();
            kpBox.isTrigger = true;
            kpBox.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);

            // ⚠️ THE MARKERS ARE DERIVED AND ARE NOT WHAT THE MATCH ACTUALLY SPAWNS FROM.
            // `Confinement.AttackerSpawnRing()` decides that, from `Balance`, on every map. What
            // these are for is `MapPreviewSurface`, which pivots the setup screen's live 3D
            // backdrop on their average. The shipped set averaged to z = 0.375 by accident;
            // this set averages to the can, on purpose.
            var spawnsGo = new GameObject("SpawnPoints");
            spawnsGo.transform.SetParent(parent, false);

            float ring = Confinement.AttackerSpawnRing();

            CreateSpawnMarker(spawnsGo.transform, "Spawn0", new Vector3(0.0f, 0.1f, 0.0f));
            CreateSpawnMarker(spawnsGo.transform, "Spawn1", new Vector3(-3.0f, 0.1f, -ring));
            CreateSpawnMarker(spawnsGo.transform, "Spawn2", new Vector3(0.0f, 0.1f, -ring));
            CreateSpawnMarker(spawnsGo.transform, "Spawn3", new Vector3(3.0f, 0.1f, -ring));
        }

        private static void CreateSpawnMarker(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
        }

        /// <summary>
        /// ⚠️⚠️ THE COLLISION GROUND HAS TO MATCH THE VISIBLE GROUND, AND ON THE SHIPPED MAP IT
        /// DID NOT. There was one flat floor collider at y = 0 and nothing else, while the
        /// pavement was drawn 0.212 m up. Every body that walked onto either pavement walked
        /// through it to the shin. The pavement and the kerb get their own boxes here, and the
        /// step is 0.212 m against a `CharacterController.stepOffset` of 0.30, so it is climbable
        /// by exactly the margin that keeps a run up the kerb from snagging.
        /// </summary>
        private static void BuildBounds(Transform parent)
        {
            var boundsGo = new GameObject("Bounds");
            boundsGo.transform.SetParent(parent, false);

            // The carriageway, whose top is the plane every rule already uses.
            var floor = boundsGo.AddComponent<BoxCollider>();
            floor.center = new Vector3(0.0f, RoadTop - 0.25f, 0.0f);
            floor.size = new Vector3(BacklotOuterX * 2.0f, 0.5f, CorridorHalfZ * 2.0f);

            for (int side = -1; side <= 1; side += 2)
            {
                float mid = side * (RoadHalfX + PavementOuterX) * 0.5f;
                AddBoundsBox(boundsGo.transform, side < 0 ? "PavementWest" : "PavementEast",
                             new Vector3(mid, PavementTop - 0.25f, 0.0f),
                             new Vector3(PavementOuterX - RoadHalfX, 0.5f, CorridorHalfZ * 2.0f));

                AddBoundsBox(boundsGo.transform, side < 0 ? "KerbWest" : "KerbEast",
                             new Vector3(side * (RoadHalfX - KerbHalfDepth), KerbTop - 0.25f, 0.0f),
                             new Vector3(KerbHalfDepth * 2.0f, 0.5f, CorridorHalfZ * 2.0f));
            }

            // ⚠️ THE WALL FACE IS THE PLAYER-REACHABLE EDGE, NOT THE WALL'S CENTRE. A 1.0 m thick
            // wall centred on the pavement edge takes half a metre of pavement away from the
            // player and the bots both, and `AIController.PlayableHalfX` is read from the CENTRE
            // (`MatchInstaller.Start`), so the AI then aims at ground it cannot occupy. Thin, and
            // pushed out by its own half-thickness.
            const float wallThickness = 0.4f;
            float wallX = PavementOuterX + wallThickness * 0.5f;
            float wallZ = WallHalfZ + wallThickness * 0.5f;

            AddBoundsBox(boundsGo.transform, "WallWest",
                         new Vector3(-wallX, 3.0f, 0.0f), new Vector3(wallThickness, 6.0f, CorridorHalfZ * 2.0f));
            AddBoundsBox(boundsGo.transform, "WallEast",
                         new Vector3(wallX, 3.0f, 0.0f), new Vector3(wallThickness, 6.0f, CorridorHalfZ * 2.0f));
            AddBoundsBox(boundsGo.transform, "WallNorth",
                         new Vector3(0.0f, 3.0f, wallZ), new Vector3(PavementOuterX * 2.0f, 6.0f, wallThickness));
            AddBoundsBox(boundsGo.transform, "WallSouth",
                         new Vector3(0.0f, 3.0f, -wallZ), new Vector3(PavementOuterX * 2.0f, 6.0f, wallThickness));
        }

        private static void AddBoundsBox(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = centre;
            col.size = size;
        }

        /// <summary>
        /// Four straight lines at |x| = |z| = the confinement radius, plus the throwing line.
        ///
        /// ⚠️⚠️ COPIED IN SHAPE FROM `SceneBuilder.BuildChalk`, AND THE REASON IS THE SAME: THE
        /// BOX IS A SQUARE. A circle and a square of the same radius disagree by 2.9 m on the
        /// diagonal, which is exactly the corner a taya moves to. Drawing this from the constant
        /// rather than by hand is the only thing that keeps the paint and the clamp agreeing.
        /// </summary>
        private static void BuildChalk(Transform parent)
        {
            var root = new GameObject("Chalk");
            root.transform.SetParent(parent, false);

            float r = Balance.ConfinementRadius;
            const float w = 0.12f;

            // North and south are chalk on the tarmac, the way both other maps draw all four.
            AddLine(root.transform, "North", new Vector3(0, 0.010f, r), new Vector3(r * 2 + w, 0.02f, w));
            AddLine(root.transform, "South", new Vector3(0, 0.010f, -r), new Vector3(r * 2 + w, 0.02f, w));

            // ⚠️⚠️ EAST AND WEST ARE PAINTED ON TOP OF THE KERB, NOT ON THE ROAD, BECAUSE THE
            // KERB IS WHERE THE RULE ALREADY IS. Drawn flat at |x| = 7.0 like the other two they
            // were invisible: the kerb occupies x 6.65 to 7.00 and stands 0.15 m up, so a 0.02 m
            // chalk line at the same X is behind and under it from every angle a player has. It
            // was drawn, it was correct, and it could not be seen in any of the seven renders.
            //
            // Painting the kerb top instead is both the fix and the better image, since a
            // painted kerb is what a Manila street actually has. The line spans the full kerb
            // width so its outer edge lands exactly on |x| = 7.0, which is the boundary
            // `Confinement.ClampToBox` enforces.
            float kerbMid = RoadHalfX - KerbHalfDepth;
            float kerbWidth = KerbHalfDepth * 2.0f;

            AddLine(root.transform, "East", new Vector3(kerbMid, KerbTop + 0.005f, 0),
                    new Vector3(kerbWidth, 0.01f, r * 2 + w));
            AddLine(root.transform, "West", new Vector3(-kerbMid, KerbTop + 0.005f, 0),
                    new Vector3(kerbWidth, 0.01f, r * 2 + w));

            // Attackers must be at or past this to throw. Derived, never typed.
            float t = Confinement.ThrowingLine();
            AddLine(root.transform, "ThrowLineNorth", new Vector3(0, 0.010f, t), new Vector3(r * 2, 0.02f, 0.06f));
            AddLine(root.transform, "ThrowLineSouth", new Vector3(0, 0.010f, -t), new Vector3(r * 2, 0.02f, 0.06f));

            // The can's own ring, at the origin, because that is where `MatchInstaller` puts it.
            var circle = InstantiateProp("env_base_circle_decal", new Vector3(0.0f, RoadTop + 0.004f, 0.0f),
                                         Quaternion.identity, root.transform);
            if (circle != null)
            {
                circle.name = "BaseCircle";
                circle.transform.localScale = new Vector3(1.7f, 1.0f, 1.7f);
            }
        }

        private static void AddLine(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Paint(go, ChalkWhite);

            // Chalk is a marking, not geometry: nothing may collide with it.
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        /// <summary>
        /// ⚠️⚠️ THE PLATES CARRY THE TILES' OWN ALBEDO, NOT A COLOUR PICKED TO LOOK RIGHT TODAY.
        /// `EnvColourPass` multiplies a tint into every renderer in `Kalsada` and `Slab` at
        /// runtime, so a plate hand-tinted to match the tiles in the editor would separate from
        /// them the instant the pass ran. Both start from the tile's `.mtl` `Kd` and are
        /// multiplied by the same tint, so they cannot drift.
        /// </summary>
        private static readonly Color AsphaltAlbedo = new Color(0.290f, 0.306f, 0.341f);
        private static readonly Color ConcreteAlbedo = new Color(0.718f, 0.698f, 0.651f);

        private static void BuildGroundAndRoads(Transform parent)
        {
            // `Kalsada` and `Slab` are `EnvColourPass`'s own group names. See the note in
            // `Execute`: this is how the map gets the same road and pavement treatment as
            // Eskinita and Bayan Plaza rather than a second palette invented here.
            var road = Group(parent, "Kalsada");
            var slab = Group(parent, "Slab");
            var floor = Group(parent, "Malayo");

            // 1. The far plate. Everything else in the map rests on this, directly or through
            // one more layer, so no renderer anywhere is left standing over nothing. It goes in
            // `Malayo` so it takes the same fade toward the sky the other maps' distance does.
            var haze = Slab(floor, "MalayoX_Ground",
                            new Vector3(0.0f, HazeTop - 0.40f, 0.0f),
                            new Vector3(HazeHalf * 2.0f, 0.80f, HazeHalf * 2.0f),
                            HazeGround);
            Object.DestroyImmediate(haze.GetComponent<Collider>());

            // 2. The shopfront apron, one solid plate per side from the kerb to the building
            // line. This is what the pavement tiles rest ON; without it they were a floating
            // 0.15 m shelf with daylight underneath, which is the single most visible fault in
            // the shipped map's own showcase render.
            for (int side = -1; side <= 1; side += 2)
            {
                float mid = side * (RoadHalfX + BacklotOuterX) * 0.5f;
                var apron = Slab(slab, side < 0 ? "ApronWest" : "ApronEast",
                                 new Vector3(mid, (HazeTop + KerbTop) * 0.5f, 0.0f),
                                 new Vector3(BacklotOuterX - RoadHalfX, KerbTop - HazeTop, CorridorHalfZ * 2.0f),
                                 ConcreteAlbedo);
                Object.DestroyImmediate(apron.GetComponent<Collider>());
            }

            // The sub-base under the carriageway, so the asphalt tiles have mass under them and
            // the street reads as cut into the ground rather than laid on top of it.
            var subBase = Slab(road, "RoadSubBase",
                               new Vector3(0.0f, (HazeTop + RoadTop) * 0.5f - 0.02f, 0.0f),
                               new Vector3(RoadHalfX * 2.0f, RoadTileThickness + 0.04f, CorridorHalfZ * 2.0f),
                               AsphaltAlbedo);
            Object.DestroyImmediate(subBase.GetComponent<Collider>());

            // 3. Asphalt. Sunk by the tile's own thickness so its SURFACE is y = 0.
            for (float z = -CorridorHalfZ + 1.0f; z < CorridorHalfZ; z += 2.0f)
            {
                for (float x = -RoadHalfX + 1.0f; x < RoadHalfX; x += 2.0f)
                {
                    InstantiateProp("env_road_tile", new Vector3(x, RoadTop - RoadTileThickness, z),
                                    Quaternion.identity, road);
                }

                // ⚠️⚠️ THE KERB RUNS ALONG THE STREET, NOT ACROSS IT. `env_kerb_tile` is 2.0 m on
                // its local X and 0.35 m on its local Z, so laid unrotated on a street that runs
                // along Z it becomes a 2 m bar lying ACROSS the carriageway with a 1.65 m gap
                // after it. The shipped map laid 50 of them that way, which is what the loose
                // pale slabs strewn over the road in `ilalim_thrower_view.png` actually are.
                for (int side = -1; side <= 1; side += 2)
                {
                    InstantiateProp("env_kerb_tile",
                                    new Vector3(side * (RoadHalfX - KerbHalfDepth), RoadTop, z),
                                    Quaternion.Euler(0.0f, 90.0f, 0.0f), slab);
                }

                // 4. Pavement, resting on the apron.
                for (int side = -1; side <= 1; side += 2)
                {
                    for (float px = RoadHalfX + 1.0f; px < PavementOuterX; px += 2.0f)
                    {
                        InstantiateProp("env_plaza_tile", new Vector3(side * px, KerbTop, z),
                                        Quaternion.identity, slab);
                    }
                }
            }
        }

        private static Transform Group(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>
        /// What closes the view, in three bands.
        ///
        /// ⚠️⚠️ THE STREET HAS TO END IN A CITY, NOT IN SKY. The walls stop a player at
        /// z = +/-16.5, but the camera sees far past them, and the shipped map had nothing at
        /// all out there: the carriageway simply stopped and the skybox began, in both
        /// directions and on both sides. A cross row past each wall closes the corridor, a
        /// shopfront row closes the sides, and a thinned-out far belt breaks the horizon behind
        /// both.
        /// </summary>
        private static void BuildBackdrop(Transform parent)
        {
            // ⚠️⚠️ KIT BUILDINGS ARE COLOURED BY A COMPLETE ATLAS REPLACEMENT, NOT BY
            // `EnvColourPass`. Commercial and industrial colormaps contain blue and orange
            // swatches close to the two role hues. `make_ilalim_kit_palettes.py` moves those
            // swatches into the Manila environment set while keeping the kit's UV layout.
            // These groups therefore stay outside `FacadeGroups`; multiplying another tint
            // into an already-coloured atlas is the exact failure recorded in EnvColourPass.
            var near = Group(parent, "Gilid");
            var skyline = Group(parent, "SkylineKit");
            var backlot = Group(parent, "BacklotKit");

            BuildSideFacade(near, -1, "building-k", -12.0f, "tumbang-warm-b", "Shophouse_W0");
            BuildSideFacade(near, -1, "building-b", -4.2f, "tumbang-warm-a", "Shophouse_W1");
            BuildSideFacade(near, -1, "building-c", 0.8f, "tumbang-warm-c", "Shophouse_W2");
            BuildSideFacade(near, -1, "building-e", 12.5f, "tumbang-warm-a", "Shophouse_W3");

            BuildSideFacade(near, 1, "building-e", -12.0f, "tumbang-warm-c", "Shophouse_E0");
            BuildSideFacade(near, 1, "building-b", -5.0f, "tumbang-warm-b", "Shophouse_E1");
            BuildSideFacade(near, 1, "building-j", 3.5f, "tumbang-warm-a", "Shophouse_E2");
            BuildSideFacade(near, 1, "building-d", 12.0f, "tumbang-warm-c", "Shophouse_E3");

            BuildBackgroundStreets(parent);

            // The rail-corridor back lots are low and broad. They are deliberately beyond the
            // player walls, where their tanks and roofs carry the Gilmore silhouette without
            // becoming cover or spending the ability floor budget.
            string[] industrial = { "building-a", "building-g", "building-p", "building-t" };
            for (int i = 0; i < industrial.Length; i++)
            {
                float side = i % 2 == 0 ? -1.0f : 1.0f;
                float x = side * (23.0f + i * 2.5f);
                float z = -8.0f + i * 8.0f;
                var building = InstantiateKitProp("industrial", industrial[i],
                    new Vector3(x, HazeTop, z), Quaternion.Euler(0.0f, side < 0 ? 90.0f : -90.0f, 0.0f),
                    Vector3.one * 6.0f, backlot, i % 2 == 0 ? "tumbang-warm-a" : "tumbang-warm-b");
                if (building != null) building.name = $"Backlot_{i}";
            }

            BuildOuterDistrict(backlot, skyline);

            // Low-detail towers make an irregular city horizon at a fraction of the geometry
            // of the old repeated hand-built blocks. Fog, rather than a second colour multiply,
            // handles their distance.
            var rng = new System.Random(20260824);
            string[] low = { "low-detail-building-a", "low-detail-building-b", "low-detail-building-c",
                             "low-detail-building-d", "low-detail-building-e", "low-detail-building-f" };
            int made = 0;

            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 36.0f + ring * 15.0f;

                for (int step = 0; step < 16; step++)
                {
                    if (rng.NextDouble() > 0.70) continue;

                    float angle = step / 16.0f * Mathf.PI * 2.0f + ring * 0.27f;
                    float x = Mathf.Sin(angle) * radius * 1.18f;
                    float z = Mathf.Cos(angle) * radius * 1.48f;
                    if (Mathf.Abs(x) < BacklotOuterX + 2.0f && Mathf.Abs(z) < CorridorHalfZ + 3.0f) continue;

                    float scale = 6.5f + (float)rng.NextDouble() * 3.0f;
                    var far = InstantiateKitProp("commercial", low[rng.Next(low.Length)],
                        new Vector3(x, HazeTop, z), Quaternion.Euler(0.0f, rng.Next(4) * 90.0f, 0.0f),
                        Vector3.one * scale, skyline, $"tumbang-warm-{(char)('a' + rng.Next(3))}");
                    if (far == null) continue;

                    far.name = $"Skyline_{made}";
                    made++;
                }
            }
        }

        private static void BuildOuterDistrict(Transform backlot, Transform skyline)
        {
            string[] commercial = { "building-a", "building-c", "building-f", "building-h",
                                    "building-i", "building-k", "building-l", "building-n" };
            string[] industrial = { "building-a", "building-c", "building-e", "building-g",
                                    "building-l", "building-q", "building-r", "building-t" };
            string[] low = { "low-detail-building-a", "low-detail-building-b", "low-detail-building-c",
                             "low-detail-building-d", "low-detail-building-e", "low-detail-building-f" };

            int made = 0;
            foreach (int side in new[] { -1, 1 })
            {
                // Dense second row immediately behind the playable shopfronts. Alternating
                // commercial and industrial forms reads as Gilmore back lots, not one repeated
                // apartment prefab, and removes the beige void visible through side gaps.
                for (int i = 0; i < 8; i++)
                {
                    float z = -28.0f + i * 8.0f;
                    bool warehouse = (i + (side > 0 ? 1 : 0)) % 3 == 0;
                    string kit = warehouse ? "industrial" : "commercial";
                    string model = warehouse ? industrial[i % industrial.Length]
                                             : commercial[(i + 2) % commercial.Length];
                    var building = InstantiateKitProp(kit, model,
                        new Vector3(side * (24.0f + (i % 2) * 2.5f), HazeTop, z),
                        Quaternion.Euler(0.0f, side < 0 ? 90.0f : -90.0f, 0.0f),
                        Vector3.one * (warehouse ? 5.6f : 5.2f), backlot,
                        warehouse ? $"tumbang-warm-{(i % 2 == 0 ? 'a' : 'b')}"
                                  : $"tumbang-warm-{(char)('a' + i % 3)}");
                    if (building != null) building.name = $"DistrictInner_{made++}";
                }

                // A lower outer row holds the side horizon. It is intentionally continuous;
                // fog provides the variation, while holes here are what made the map look like
                // a floating island from first-person cameras.
                for (int i = 0; i < 11; i++)
                {
                    float z = -60.0f + i * 12.0f;
                    var building = InstantiateKitProp("commercial", low[(i + (side > 0 ? 2 : 0)) % low.Length],
                        new Vector3(side * 40.0f, HazeTop, z),
                        Quaternion.Euler(0.0f, side < 0 ? 90.0f : -90.0f, 0.0f),
                        Vector3.one * (7.5f + (i % 3)), skyline,
                        $"tumbang-warm-{(char)('a' + (i + 1) % 3)}");
                    if (building != null) building.name = $"DistrictOuter_{made++}";
                }
            }

            // Far end blocks sit beyond the background intersections and leave the 14 m road
            // opening clear. They are deep in fog, so they close the ground plane without
            // recreating the near cross-row wall the traffic pass removed.
            foreach (int end in new[] { -1, 1 })
            {
                for (int slot = -5; slot <= 5; slot++)
                {
                    if (Mathf.Abs(slot) <= 1) continue;
                    float x = slot * 10.0f;
                    var building = InstantiateKitProp("commercial", low[Mathf.Abs(slot + end) % low.Length],
                        new Vector3(x, HazeTop, end * 56.0f),
                        Quaternion.Euler(0.0f, end > 0 ? 0.0f : 180.0f, 0.0f),
                        Vector3.one * (8.0f + Mathf.Abs(slot) % 3), skyline,
                        $"tumbang-warm-{(char)('a' + Mathf.Abs(slot + end) % 3)}");
                    if (building != null) building.name = $"DistrictFar_{made++}";
                }
            }

            // Back-lot tanks are grouped with warehouses rather than scattered under the
            // bridge, which makes them environmental evidence instead of random kit props.
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 3; i++)
                {
                    var tank = InstantiateKitProp("industrial", "detail-tank",
                        new Vector3(side * 30.0f, HazeTop, -14.0f + i * 14.0f),
                        Quaternion.Euler(0.0f, i * 90.0f, 0.0f), Vector3.one * 5.5f,
                        backlot, i % 2 == 0 ? "tumbang-warm-a" : "tumbang-warm-b");
                    if (tank != null) tank.name = $"BacklotTank_{side}_{i}";
                }
            }
        }

        private static void BuildBackgroundStreets(Transform parent)
        {
            var road = Group(parent, "Kalsada");
            var pavement = Group(parent, "Slab");
            var district = Group(parent, "BackgroundStreet");
            float farLength = HazeHalf - CorridorHalfZ;
            float farMid = (HazeHalf + CorridorHalfZ) * 0.5f;

            // The playable road already reaches z = +/-24. These backed slabs continue it to
            // two visible intersections, then turn left and right between building corners.
            // No cross-row facade stands in the carriageway, so the road reads as a street that
            // carries on through Manila rather than as a set built against a wall.
            foreach (int end in new[] { -1, 1 })
            {
                var extension = Slab(road, end < 0 ? "RoadContinuationSouth" : "RoadContinuationNorth",
                    new Vector3(0.0f, -RoadTileThickness * 0.5f, end * farMid),
                    new Vector3(RoadHalfX * 2.0f, RoadTileThickness, farLength), AsphaltAlbedo);
                Object.DestroyImmediate(extension.GetComponent<Collider>());

                foreach (int side in new[] { -1, 1 })
                {
                    var sidewalk = Slab(pavement, $"BackgroundSidewalk_{end}_{side}",
                        new Vector3(side * 9.0f, (HazeTop + PavementTop) * 0.5f, end * farMid),
                        new Vector3(PavementOuterX - RoadHalfX, PavementTop - HazeTop, farLength),
                        ConcreteAlbedo);
                    Object.DestroyImmediate(sidewalk.GetComponent<Collider>());

                    var cross = Slab(road, $"BackgroundCrossroad_{end}_{side}",
                        new Vector3(side * 23.5f, -RoadTileThickness * 0.5f, end * 31.0f),
                        new Vector3(33.0f, RoadTileThickness, 9.0f), AsphaltAlbedo);
                    Object.DestroyImmediate(cross.GetComponent<Collider>());
                }

                string[] cornerModels = { "building-n", "building-l", "building-i", "building-e" };
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    int side = sideIndex == 0 ? -1 : 1;
                    for (int row = 0; row < 2; row++)
                    {
                        float x = side * (13.5f + row * 7.5f);
                        float z = end * (31.5f + row * 7.0f);
                        var building = InstantiateKitProp("commercial",
                            cornerModels[(end > 0 ? 2 : 0) + sideIndex],
                            new Vector3(x, HazeTop, z),
                            Quaternion.Euler(0.0f, end > 0 ? 0.0f : 180.0f, 0.0f),
                            Vector3.one * (row == 0 ? 5.0f : 5.8f), district,
                            $"tumbang-warm-{(char)('a' + (row + sideIndex + (end > 0 ? 1 : 0)) % 3)}");
                        if (building != null)
                            building.name = $"IntersectionShop_{end}_{side}_{row}";
                    }

                    var light = InstantiateKitProp("roads", "traffic-light",
                        new Vector3(side * 8.4f, RoadTop, end * 29.0f),
                        Quaternion.Euler(0.0f, end > 0 ? 180.0f : 0.0f, 0.0f),
                        Vector3.one * 10.0f, district, "tumbang-warm-a");
                    if (light != null) light.name = $"BackgroundTrafficLight_{end}_{side}";
                }
            }
        }

        private static void BuildSideFacade(Transform parent, int side, string model, float z,
                                            string palette, string name)
        {
            var building = InstantiateKitProp("commercial", model,
                new Vector3(side * 13.5f, KerbTop, z),
                Quaternion.Euler(0.0f, side < 0 ? -90.0f : 90.0f, 0.0f),
                Vector3.one * 5.0f, parent, palette);
            if (building == null) return;

            building.name = name;

            // The model origin is not its shop face. Move the rendered near edge to the wall
            // at |x| = 11 so the row closes the side without taking pavement from the player.
            Bounds bounds = RenderBounds(building);
            float nearX = side < 0 ? bounds.max.x : bounds.min.x;
            building.transform.position += new Vector3(side * PavementOuterX - nearX, 0.0f, 0.0f);
        }

        private static void BuildHeroStructures(Transform parent)
        {
            // "Tulay" is this map only: the guideway, its columns and the consist keep the
            // concrete and livery their own .mtl files carry, so the group name is deliberately
            // NOT one EnvColourPass paints. A facade tint on a railway viaduct would put a
            // Manila house colour on the one structure the map is named after.
            var heroGo = Group(parent, "Tulay");

            // 1. The guideway. Twelve 4 m bridge bays make a 10.5 by 48 m dual-track deck.
            // The previous 6.88 m deck covered 49 per cent of the road and read as a footbridge.
            // This one covers 75 per cent, which is the width argued in the v2 plan.
            var guideway = new GameObject("LrtGuideway");
            guideway.transform.SetParent(heroGo, false);
            AirborneByDesign.Attach(guideway, "The LRT-2 guideway. Its soffit is 8.0 m up and " +
                                                    "the live support rows stand at z = +/-10.");

            for (float z = -GuidewayLength * 0.5f + 2.0f; z < GuidewayLength * 0.5f; z += 4.0f)
            {
                var bay = InstantiateKitProp("roads", "road-bridge",
                    new Vector3(0.0f, ViaductSoffit, z), Quaternion.identity,
                    new Vector3(GuidewayWidth, 2.0f, 4.0f), guideway.transform, "tumbang-warm-a");
                if (bay != null) bay.name = $"GuidewayBay_{z + GuidewayLength * 0.5f:F0}";
            }

            var deckCol = guideway.AddComponent<BoxCollider>();
            deckCol.center = new Vector3(0.0f, ViaductSoffit + 0.52f, 0.0f);
            deckCol.size = new Vector3(GuidewayWidth, 1.04f, GuidewayLength);

            // Two real detailed tracks sit on the deck. Their 3 m width leaves 1.4 m between
            // tracks and 1.4 m to each parapet, enough for a 2.6 m city carriage at 2x scale.
            foreach (float trackX in new[] { WestboundTrackX, EastboundTrackX })
            {
                var track = new GameObject(trackX < 0 ? "WestboundTrack" : "EastboundTrack");
                track.transform.SetParent(guideway.transform, false);
                AirborneByDesign.Attach(track, "Detailed rail and sleepers resting on the LRT guideway deck.");

                for (float z = -GuidewayLength * 0.5f + 2.0f; z < GuidewayLength * 0.5f; z += 4.0f)
                {
                    InstantiateKitProp("train", "track-detailed",
                        new Vector3(trackX, GuidewayTop, z), Quaternion.identity,
                        new Vector3(3.0f, 1.0f, 4.0f), track.transform, "tumbang-lrt");
                }
            }

            // 2. The columns.
            //
            // ⚠️⚠️ NO COLUMN MAY STAND INSIDE THE CHALK. The shipped map put two of them at
            // z = -5.0, which is inside a box the taya is CLAMPED into: a 3.4 m wide obstacle in
            // the one room the defender cannot leave. That does not get reported as a placement
            // bug, it gets reported as the taya getting stuck or as one side of the can being
            // impossible to defend. Both live rows are now outside |z| = 7.
            //
            // The 1.4 m wide kit pillars at x = +/-4.45 leave a measured 7.5 m centre gap and
            // 1.85 m to each kerb. Cheska's 3.2 m wall cuts 43 per cent rather than closing it.
            foreach (float z in new[] { -19.0f, -10.0f, 10.0f, 19.0f })
            {
                CreateViaductPillar(heroGo, new Vector3(-4.45f, 0.0f, z),
                                    $"LrtPillar_{(z > 0 ? "North" : "South")}West_{Mathf.Abs(z):F0}");
                CreateViaductPillar(heroGo, new Vector3(4.45f, 0.0f, z),
                                    $"LrtPillar_{(z > 0 ? "North" : "South")}East_{Mathf.Abs(z):F0}");
            }

            // 3. PC Express.
            //
            // ⚠️⚠️ THE SHOP FACE IS THE WEST WALL, and that is what makes the showroom worth
            // building. Measured from `env_pc_express_store.obj`: the glass, the shutter and the
            // signboard are all at local z = -3.15 and the awning reaches local z = -4.10, so
            // rotating -90 degrees puts the face at originX + 3.15 and the awning tip 0.95 m
            // further into the street. Setting originX so the face lands exactly on the wall
            // face means the shop closes the west side instead of sitting in the middle of the
            // carriageway, which is where the shipped placement put 2.1 m of it.
            const float shopFaceLocalZ = -3.15f;
            float storeX = -PavementOuterX + shopFaceLocalZ;

            var pcex = InstantiateProp("env_pc_express_store", new Vector3(storeX, KerbTop, 5.5f),
                                       Quaternion.Euler(0.0f, -90.0f, 0.0f), heroGo);
            if (pcex != null)
            {
                pcex.name = "PC_Express_Store";

                // Local space, because the instance carries the rotation. The awning is left out
                // deliberately: it is 2.2 m up and a player is meant to walk under it.
                var pcexCol = pcex.AddComponent<BoxCollider>();
                pcexCol.center = new Vector3(0.125f, 2.19f, 0.0f);
                pcexCol.size = new Vector3(5.05f, 4.38f, 6.30f);

                AddPointLight(pcex.transform, "InteriorWarmLight", new Vector3(0.0f, 1.8f, -1.2f),
                              new Color(1.0f, 0.95f, 0.82f), 7.5f, 1.3f);
                // ⚠️ THE SIGN LIGHT FOLLOWED THE SIGN FROM GREEN TO RED. It was
                // #00873e, which was right when the fascia was a green lightbox and, once the
                // fascia became the real red one, was washing a green cast across white letters
                // on a red board: the three colours that carry the recognition were the three it
                // was destroying. Warm white lets the board be the colour it is painted.
                AddPointLight(pcex.transform, "SignboardGlowLight", new Vector3(0.0f, 3.5f, -3.6f),
                              new Color(1.0f, 0.94f, 0.86f), 5.5f, 1.5f);
                AddPointLight(pcex.transform, "RgbShowcaseLight", new Vector3(-0.4f, 1.2f, -2.4f),
                              new Color(0.2f, 0.8f, 1.0f), 4.0f, 1.1f);

                BuildPcExpressExterior(pcex.transform);

                var officialLogo = InstantiateProp("env_pc_express_logo_3d", Vector3.zero,
                    Quaternion.identity, pcex.transform);
                if (officialLogo != null)
                {
                    officialLogo.name = "PC_Express_Official_Raised_Logo";
                    AirborneByDesign.Attach(officialLogo,
                        "Raised official PC Express letters mounted on the storefront lightbox.");
                }
            }
        }

        private static void BuildPcExpressExterior(Transform store)
        {
            // The supplied exterior is a glass showroom under one red-blue fascia. The source
            // model had a striped market awning and one undivided opening, so even the correct
            // colours still read as a stall. These pieces give it the metal-and-glass rhythm of
            // the real shop while leaving the walk-under clearance and collider unchanged.
            Color metal = new Color(0.18f, 0.20f, 0.23f);
            Color brandBlue = new Color(0.055f, 0.180f, 0.490f);
            Color brandRed = new Color(0.855f, 0.098f, 0.129f);

            var overhang = Slab(store, "ShowroomOverhang", new Vector3(0.0f, 2.62f, -3.56f),
                                new Vector3(5.15f, 0.16f, 0.82f), brandBlue);
            Object.DestroyImmediate(overhang.GetComponent<Collider>());
            AirborneByDesign.Attach(overhang, "Bolted to the PC Express glass facade at y = 2.62.");

            var redLip = Slab(store, "ShowroomRedLip", new Vector3(0.0f, 2.56f, -3.98f),
                              new Vector3(5.15f, 0.10f, 0.08f), brandRed);
            Object.DestroyImmediate(redLip.GetComponent<Collider>());
            AirborneByDesign.Attach(redLip, "The red leading edge is part of the showroom overhang.");

            foreach (float x in new[] { -2.36f, -1.18f, 0.0f, 1.18f, 2.36f })
            {
                var mullion = Slab(store, $"GlassMullion_{x:F2}",
                    new Vector3(x, 1.32f, -3.19f), new Vector3(0.065f, 2.42f, 0.075f), metal);
                Object.DestroyImmediate(mullion.GetComponent<Collider>());
                AirborneByDesign.Attach(mullion, "Mounted to the PC Express showroom glazing.");
            }

            var kick = Slab(store, "ShowroomKickPlate", new Vector3(0.0f, 0.22f, -3.20f),
                            new Vector3(4.90f, 0.30f, 0.08f), metal);
            Object.DestroyImmediate(kick.GetComponent<Collider>());
            AirborneByDesign.Attach(kick, "Mounted across the foot of the PC Express glazing.");

            foreach (float x in new[] { -0.12f, 0.12f })
            {
                var handle = Slab(store, $"DoorHandle_{x:F2}",
                    new Vector3(x, 1.22f, -3.25f), new Vector3(0.035f, 0.42f, 0.035f), ChalkWhite);
                Object.DestroyImmediate(handle.GetComponent<Collider>());
                AirborneByDesign.Attach(handle, "Mounted on the centre glass doors.");
            }
        }

        private static Light AddPointLight(Transform parent, string name, Vector3 localPos,
                                           Color colour, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        private static void BuildLrtTrainSystem(Transform parent)
        {
            var trainSystemGo = new GameObject("LrtTrainSystem");
            trainSystemGo.transform.SetParent(Group(parent, "Tulay"), false);
            trainSystemGo.transform.localPosition = new Vector3(WestboundTrackX, TrainRootY, 0.0f);

            var flyby = trainSystemGo.AddComponent<LrtTrainFlyby>();
            flyby.TrackX = WestboundTrackX;
            flyby.TrackY = TrainRootY;
            flyby.Speed = 18.0f;
            flyby.Interval = 24.0f;
            flyby.InitialDelay = 5.0f;
            flyby.OverheadHalfZ = WallHalfZ + TrainConsistHalfLength;

            AirborneByDesign.Attach(trainSystemGo, "The LRT-2 consist, riding the westbound rail " +
                                                   $"head at y = {RailHead:F3} on the guideway.");

            InstantiateKitProp("train", "train-electric-city-a", new Vector3(0.0f, 0.0f, -5.1f),
                Quaternion.identity, Vector3.one * TrainScale, trainSystemGo.transform, "tumbang-lrt");
            InstantiateKitProp("train", "train-electric-city-b", Vector3.zero,
                Quaternion.identity, Vector3.one * TrainScale, trainSystemGo.transform, "tumbang-lrt");
            InstantiateKitProp("train", "train-electric-city-c", new Vector3(0.0f, 0.0f, 5.1f),
                Quaternion.identity, Vector3.one * TrainScale, trainSystemGo.transform, "tumbang-lrt");
        }

        /// <summary>
        /// ⚠️ THE PAD IS ON THE PAVEMENT OUTSIDE THE SHOP, NOT ON THE KERB. The shipped pad sat
        /// at x = -4.8, which the new cross section puts in the carriageway, and the old one put
        /// INSIDE the PC Express collider. A speed pad a player cannot reach without walking
        /// through a wall is a dead feature that still costs a light and a trigger every frame.
        /// </summary>
        private static void BuildOverclockPad(Transform parent)
        {
            const float padX = -9.0f;
            const float padZ = 5.5f;

            var padGo = new GameObject("OverclockTurboPad");
            padGo.transform.SetParent(Group(parent, "Hazards"), false);
            padGo.transform.localPosition = new Vector3(padX, SurfaceTop(padX), padZ);

            var col = padGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0.0f, 0.2f, 0.0f);
            col.size = new Vector3(2.0f, 0.4f, 2.0f);

            var boost = padGo.AddComponent<OverclockBoostPad>();
            boost.PadLight = AddPointLight(padGo.transform, "OverclockRgbLight",
                                           new Vector3(0.0f, 0.4f, 0.0f), BoostPadGlow, 3.5f, 1.5f);

            // A hardware display pad, not another opaque ability puddle. The dark plate keeps
            // its footprint quiet and three safe-hue light bars carry the PC-shop read.
            var basePlate = Slab(padGo.transform, "PadBase", new Vector3(0.0f, 0.018f, 0.0f),
                                 new Vector3(1.8f, 0.03f, 1.8f), PotholeGrey);
            Object.DestroyImmediate(basePlate.GetComponent<Collider>());

            Color[] bars = { BoostPadPlum, BoostPadGlow, BoostPadGold };
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = Slab(padGo.transform, $"RgbBar_{i}",
                    new Vector3(-0.46f + i * 0.46f, 0.040f, 0.0f),
                    new Vector3(0.20f, 0.015f, 1.48f), bars[i]);
                Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
        }

        /// <summary>Raw `bridge-pillar-wide` is 0.14 by 0.5 by 0.14 m.</summary>
        private const float PillarHorizontalScale = 10.0f;
        private const float PillarVerticalScale = 16.0f;
        private const float PillarWorldHalf = 0.70f;

        private static void CreateViaductPillar(Transform parent, Vector3 pos, string name)
        {
            var pillar = InstantiateKitProp("roads", "bridge-pillar-wide",
                new Vector3(pos.x, SurfaceTop(pos.x), pos.z), Quaternion.identity,
                new Vector3(PillarHorizontalScale, PillarVerticalScale, PillarHorizontalScale),
                parent, "tumbang-warm-a");
            if (pillar == null) return;

            pillar.name = name;

            // Local space. The non-uniform instance scale turns this measured raw kit bound
            // into the 1.4 by 8.0 by 1.4 m support argued in the map plan.
            var col = pillar.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, 0.25f, 0.0f);
            col.size = new Vector3(0.14f, 0.50f, 0.14f);

            HazardVolume.Attach(pillar, PillarWorldHalf + 0.4f, -1);

            AddPointLight(pillar.transform, "MercuryVaporLamp", new Vector3(0.0f, 0.325f, -0.09f),
                          new Color(0.88f, 0.96f, 1.0f), 7.5f, 1.1f);
        }

        private static void BuildStreetProps(Transform parent)
        {
            // Four groups, and only two of them are ones `EnvColourPass` paints.
            //   Tindahan  the branded shops. They keep their own palette; PC Express red and
            //             blue, the pisonet's cyan screen and the pares cart's steel are the
            //             point of them and a facade tint would flatten all three.
            //   Kalat     street clutter, which the other maps also leave alone.
            //   Kable     poles and wires, untouched, matching Eskinita's group of the same name.
            var streetGo = Group(parent, "Tindahan");
            var kalat = Group(parent, "Kalat");
            var kable = Group(parent, "Kable");

            // 1. The pisonet cluster, against the east shopfront line.
            //
            // ⚠️⚠️ ONE CABINET DID NOT READ AS A BUSINESS. It looked like a random computer on
            // a pavement, which is exactly what 🧑 reported. Three terminals, chairs, one awning
            // and a rate board make the same joke legible without explanation: showroom PCs on
            // the west, one-peso street PCs on the east.
            const float pisonetX = 9.70f;
            for (int i = 0; i < 3; i++)
            {
                float z = 1.7f + i * 1.75f;
                var pisonet = InstantiateProp("env_pisonet_kiosk",
                    new Vector3(pisonetX, SurfaceTop(pisonetX), z),
                    Quaternion.Euler(0.0f, 90.0f, 0.0f), streetGo.transform);
                if (pisonet == null) continue;

                pisonet.name = $"Pisonet_Kiosk_{i + 1}";
                var col = pisonet.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 0.98f, -0.325f);
                col.size = new Vector3(1.08f, 1.96f, 1.50f);

                var arcade = pisonet.AddComponent<PisonetInteractive>();
                arcade.ScreenLight = AddPointLight(pisonet.transform, "ScreenGlow",
                    new Vector3(0.0f, 1.22f, -0.10f), new Color(0.20f, 0.82f, 0.72f), 2.2f, 0.75f);

                AddClutter(kalat, "env_monobloc_chair", 8.15f, z, i % 2 == 0 ? -75.0f : -105.0f);
            }

            var pisonetAwning = InstantiateKitProp("commercial", "detail-awning-wide",
                new Vector3(10.55f, 2.45f, 3.45f), Quaternion.Euler(0.0f, 90.0f, 0.0f),
                new Vector3(6.8f, 3.0f, 5.5f), streetGo.transform, "tumbang-warm-b");
            if (pisonetAwning != null)
                AirborneByDesign.Attach(pisonetAwning, "Bolted to the east shopfront above the pisonet row.");

            AddFramedWallSign(streetGo.transform, "Pisonet_Rate_Fascia",
                new Vector3(10.96f, 2.82f, 3.45f), 90.0f, new Vector2(3.55f, 0.86f),
                SignWood, SignCream, SignMaroon, new[] { "PISONET", "P1  5 MIN" },
                "Framed rate fascia bolted above the three pisonet terminals.");

            // 2. The pares cart, same pavement, further south.
            const float paresX = 8.8f;
            var pares = InstantiateProp("env_street_pares_cart",
                                        new Vector3(paresX, SurfaceTop(paresX), -5.0f),
                                        Quaternion.Euler(0.0f, 90.0f, 0.0f), streetGo.transform);
            if (pares != null)
            {
                pares.name = "Street_Pares_Cart";

                var col = pares.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 1.3f, -0.17f);
                col.size = new Vector3(2.03f, 2.60f, 1.72f);

                pares.AddComponent<StreetParesInteractive>();
                AddPointLight(pares.transform, "FoodWarmerLight", new Vector3(0.5f, 1.5f, 0.15f),
                              new Color(1.0f, 0.70f, 0.30f), 3.8f, 1.0f);
            }

            var paresParasol = InstantiateKitProp("commercial", "detail-parasol-a",
                new Vector3(9.65f, SurfaceTop(9.65f), -6.9f), Quaternion.identity,
                Vector3.one * 4.5f, streetGo.transform, "tumbang-warm-c");
            if (paresParasol != null) paresParasol.name = "Pares_Parasol";

            BuildParesBoard(streetGo.transform);

            // 3. The delivery trike outside PC Express, on the west pavement.
            const float trikeX = -9.1f;
            var trike = InstantiateProp("env_cargo_tricycle_boxes",
                                        new Vector3(trikeX, SurfaceTop(trikeX), 1.0f),
                                        Quaternion.Euler(0.0f, 30.0f, 0.0f), streetGo.transform);
            if (trike != null)
            {
                trike.name = "Cargo_Tricycle_Boxes";

                var col = trike.AddComponent<BoxCollider>();
                col.center = new Vector3(0.255f, 0.68f, -0.05f);
                col.size = new Vector3(2.07f, 1.36f, 2.24f);

                // The source mesh ends its frame at y = 0.93 while the handlebar begins at
                // y = 1.025 and z = -0.69. This short stem closes the measured 95 mm gap.
                var stem = Slab(trike.transform, "HandlebarStem",
                    new Vector3(-0.55f, 0.98f, -0.56f), new Vector3(0.10f, 0.11f, 0.18f),
                    new Color(0.62f, 0.64f, 0.66f));
                Object.DestroyImmediate(stem.GetComponent<Collider>());
                AirborneByDesign.Attach(stem, "Joins the cargo tricycle frame to its handlebar.");

                var gripJoin = Slab(trike.transform, "HandlebarGripJoin",
                    new Vector3(-0.55f, 1.05f, -0.725f), new Vector3(0.10f, 0.05f, 0.23f),
                    new Color(0.62f, 0.64f, 0.66f));
                Object.DestroyImmediate(gripJoin.GetComponent<Collider>());
                AirborneByDesign.Attach(gripJoin, "Joins the cargo tricycle handlebar to its grip.");
            }

            // 4. The bridge hoop.
            //
            // ⚠️⚠️ THIS IS THE MAP'S ONE SKILL TOY AND IT AWARDS NOTHING. A ring bolted up beside
            // an LRT column is the most Manila object there is, and putting a tsinelas through it
            // is a shot only this map lets a player take. `BridgeHoop` fires a callout and, in
            // Classic, Street Hype. It cannot reach the score: `MatchDirector.AddScore` stays the
            // only place a point is made.
            //
            // ⚠️ IT STANDS ON THE PAVEMENT ON ITS OWN POST, RATHER THAN HANGING OFF THE COLUMN.
            // A ring bolted to concrete is the better image and it is a floating prop the moment
            // the column moves 20 cm. `env_basketball_ring` carries a post to the ground, so it
            // is held up by the same rule as everything else on the map.
            BuildBridgeHoop(streetGo);

            // 5. Aurora Boulevard continues beyond both gameplay walls. The first version put
            // a solid row of jersey barriers across each end and a row of buildings behind it,
            // which read as a road terminating at the arena. Real kit vehicles now carry the
            // boundary beyond |z| = 16.5. Their nearest body edge stays outside the wall, and
            // they have no collider, hazard or gameplay role.
            BuildBoundaryTraffic(kalat);

            // 6. Two complete kit utility spans, rotated ALONG the pavements. The v8 placement
            // ran their 12.95 m cable length across the road and made the wires another arena
            // ceiling. Each 2.57 m deep assembly now stays between |x| = 8.4 and the shopfront,
            // with one staggered run per sidewalk and no cable or post over the chalk.
            // One full `electricity-pole-wide` per segment duplicates a pole at every join,
            // which is why the v13 capture showed fat pairs in the pavement even after the
            // line moved to the kerb. Build one continuous line from wire-only spans and one
            // single post per shared endpoint instead.
            const float cableSpan = 12.0862f;
            const float cableMinZ = -5.6591f;
            const float cableMaxZ = 6.4271f;
            foreach (float x in new[] { -10.65f, 10.65f })
            {
                // Different offsets keep a pole pair out of both hero storefronts: west avoids
                // PC Express at z = 5.5, east avoids the pisonet row at z = 1.7..5.2.
                float stagger = x < 0.0f ? 3.0f : 4.5f;
                for (int segment = -6; segment <= 6; segment++)
                {
                    var wire = InstantiateKitProp("roads", "electricity-wires-wide",
                        new Vector3(x, SurfaceTop(x), segment * cableSpan + stagger),
                        Quaternion.Euler(0.0f, 90.0f, 0.0f),
                        Vector3.one * 12.0f, kable, "tumbang-warm-a");
                    if (wire != null)
                    {
                        wire.name = $"SidewalkWire_{(x < 0 ? "W" : "E")}_{segment + 6}";
                        AirborneByDesign.Attach(wire,
                            "Continuous utility wires carried by the single posts at both endpoints.");
                    }
                }

                float firstCentre = -6.0f * cableSpan + stagger;
                for (int joint = 0; joint <= 13; joint++)
                {
                    float z = joint == 0
                        ? firstCentre + cableMinZ
                        : firstCentre + (joint - 1) * cableSpan + cableMaxZ;
                    var pole = InstantiateKitProp("roads", "electricity-pole-single",
                        new Vector3(x, SurfaceTop(x), z), Quaternion.Euler(0.0f, 90.0f, 0.0f),
                        Vector3.one * 12.0f, kable, "tumbang-warm-a");
                    if (pole != null)
                        pole.name = $"SidewalkPole_{(x < 0 ? "W" : "E")}_{joint}";
                }
            }

            // 7. Clutter, all of it on the pavements and none of it in the box.
            AddClutter(kalat, "env_crate_stack", -9.4f, -3.2f, 25.0f);
            AddClutter(kalat, "env_oil_drum", -8.6f, -4.6f, 0.0f);
            AddClutter(kalat, "env_monobloc_chair", 8.2f, 1.2f, -60.0f);
            AddClutter(kalat, "env_monobloc_chair", 8.6f, -3.4f, 140.0f);
            AddClutter(kalat, "env_tire", 9.6f, -8.6f, 0.0f);
            AddClutter(kalat, "env_tire", -9.8f, 9.4f, 20.0f);
            AddClutter(kalat, "env_crate_stack", 9.2f, 7.4f, -15.0f);
            AddClutter(kalat, "env_bollard", -8.2f, -8.4f, 0.0f);
            AddClutter(kalat, "env_bollard", -8.2f, -6.4f, 0.0f);

            BuildRepairBladeSign(streetGo.transform);

            BuildUnderBridgeSignage(kalat);
        }

        private static void BuildBridgeHoop(Transform parent)
        {
            // West pavement, facing the street, level with the south column row so the two read
            // as one piece of under-bridge furniture.
            const float hoopX = -8.9f;
            const float hoopZ = -10.0f;

            var hoop = InstantiateProp("env_basketball_ring",
                                       new Vector3(hoopX, SurfaceTop(hoopX), hoopZ),
                                       Quaternion.Euler(0.0f, -90.0f, 0.0f), parent);
            if (hoop == null) return;

            hoop.name = "BridgeHoop";

            // Measured from `env_basketball_ring.obj`: the ring is at local y 3.05..3.09, radius
            // 0.25 about local (0, 0). The catch is 0.30 because a tsinelas is 0.28 long and a
            // shot that clips the rim and drops through still counts in the game this imitates.
            var ring = hoop.AddComponent<BridgeHoop>();
            ring.RingCentre = new Vector3(0.0f, 3.07f, 0.0f);
            ring.RingRadius = 0.30f;

            // The post is thin and stands on the pavement outside the box, so it gets a collider
            // for bank shots the same way the columns do.
            var col = hoop.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, 1.55f, 0.62f);
            col.size = new Vector3(0.24f, 3.10f, 0.24f);

            // The generated ring stops 0.23 m short of its backboard. This bracket is the
            // missing support, not decoration: without it the rim visibly floats in the air.
            var bracket = Slab(hoop.transform, "RimBracket", new Vector3(0.0f, 3.07f, 0.37f),
                               new Vector3(0.10f, 0.08f, 0.24f), BawalRed);
            Object.DestroyImmediate(bracket.GetComponent<Collider>());
            AirborneByDesign.Attach(bracket, "The bracket joins the hoop rim to its backboard at y = 3.07.");
        }

        /// <summary>
        /// The signage that makes a Manila underpass a Manila underpass.
        ///
        /// ⚠️ FLAVOUR ONLY, AND DELIBERATELY ON THE COLUMN BASES WHERE NOTHING IS EVER FOUGHT
        /// OVER. `docs/VISION.md` § 2 counts everything that shares the box; this is outside it,
        /// flat against concrete, and never overlaps an ability footprint.
        /// </summary>
        private static void BuildUnderBridgeSignage(Transform parent)
        {
            var sign = Group(parent, "Karatula");

            // Flat on the measured inner face: centres at |x| = 4.45 minus a 0.70 m half width.
            float face = 4.45f - PillarWorldHalf;

            // ⚠️⚠️ THE TEXT IS THE WHOLE POINT AND A BLANK PLATE IS WORSE THAN NOTHING. The first
            // pass put untitled red rectangles on the columns; in the capture they read as
            // missing textures, not as signage. The line every under-bridge column in the country
            // carries is the joke, so if it cannot be read it is not worth the draw call.
            // ⚠️ THE YAW PUTS THE PRINTED FACE TOWARD THE ROAD, AND THE FIRST PASS HAD IT
            // BACKWARDS ON ALL THREE. The lettering is a child at the plate's local -Z, and a
            // yaw of +90 maps local -Z to world -X: on the WEST column that points into the
            // concrete. Both plates rendered as blank coloured rectangles with their text buried
            // inside the pillar, which in a capture reads as a missing texture.
            AddPlacard(sign, "Bawal_West", new Vector3(-face, 1.25f, -10.0f), -90.0f, BawalRed,
                       new[] { "BAWAL", "UMIHI", "DITO" });
            AddPlacard(sign, "Bawal_East", new Vector3(face, 1.25f, 10.0f), 90.0f, BawalRed,
                       new[] { "BAWAL", "UMIHI", "DITO" });

            // A barangay tarpaulin on the far column, the other half of the vocabulary.
            AddPlacard(sign, "Tarpaulin", new Vector3(-face, 1.65f, 19.0f), -90.0f, TarpBlue,
                       new[] { "BARANGAY", "PAT ROL" });
        }

        private static void AddPlacard(Transform parent, string name, Vector3 pos, float yaw,
                                       Color tint, string[] lines)
        {
            AddWallPlacard(parent, name, pos, yaw, new Vector2(1.10f, 0.72f), tint, lines,
                "Painted flat on the LRT column face at x = " +
                $"{Mathf.Abs(pos.x):F2}. A sign on a wall has nothing under it.");
        }

        private static void AddWallPlacard(Transform parent, string name, Vector3 pos, float yaw,
                                           Vector2 size, Color tint, string[] lines, string reason)
        {
            var go = Slab(parent, name, pos, new Vector3(size.x, size.y, 0.04f), tint);
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            AirborneByDesign.Attach(go, reason);

            // Letters are fractions of the plate local space, so every sign size uses the same
            // font and line layout without leaving the text behind when its width changes.
            float lineHeight = 1.0f / (lines.Length + 0.6f);

            for (int i = 0; i < lines.Length; i++)
            {
                float cy = 0.5f - lineHeight * (i + 0.8f);
                PaintText(go.transform, lines[i], cy, lineHeight * 0.72f, name + "_L" + i,
                          ChalkWhite);
            }
        }

        private static void AddFramedWallSign(Transform parent, string name, Vector3 pos, float yaw,
                                              Vector2 size, Color frameColour, Color faceColour,
                                              Color ink, string[] lines, string reason)
        {
            Quaternion rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            Vector3 normal = rotation * Vector3.back;

            var frame = Slab(parent, name + "_Frame", pos,
                new Vector3(size.x + 0.18f, size.y + 0.18f, 0.09f), frameColour);
            frame.transform.localRotation = rotation;
            Object.DestroyImmediate(frame.GetComponent<Collider>());
            AirborneByDesign.Attach(frame, reason);

            var face = Slab(parent, name, pos + normal * 0.065f,
                new Vector3(size.x, size.y, 0.045f), faceColour);
            face.transform.localRotation = rotation;
            Object.DestroyImmediate(face.GetComponent<Collider>());
            AirborneByDesign.Attach(face, reason);

            float lineHeight = 1.0f / (lines.Length + 0.6f);
            for (int i = 0; i < lines.Length; i++)
            {
                float cy = 0.5f - lineHeight * (i + 0.8f);
                PaintText(face.transform, lines[i], cy, lineHeight * 0.72f, name + "_L" + i, ink);
            }
        }

        private static void BuildRepairBladeSign(Transform parent)
        {
            const float x = -10.15f;
            const float y = 2.55f;
            const float z = -4.0f;

            foreach (float bracketY in new[] { 2.18f, 2.90f })
            {
                var bracket = Slab(parent, $"RepairBladeBracket_{bracketY:F2}",
                    new Vector3(-10.58f, bracketY, z), new Vector3(0.78f, 0.055f, 0.055f), SignWood);
                Object.DestroyImmediate(bracket.GetComponent<Collider>());
                AirborneByDesign.Attach(bracket, "Bracket joining the projecting repair sign to the west shopfront.");
            }

            AddFramedWallSign(parent, "Repair_Blade", new Vector3(x, y, z), 0.0f,
                new Vector2(1.05f, 1.48f), SignWood, TarpBlue, SignCream,
                new[] { "PC", "REPAIR" }, "Projecting blade sign carried by two shopfront brackets.");
        }

        private static void BuildParesBoard(Transform parent)
        {
            const float x = 8.05f;
            const float z = -3.75f;
            float surface = SurfaceTop(x);

            foreach (float legZ in new[] { z - 0.30f, z + 0.30f })
            {
                var leg = Slab(parent, $"ParesBoardLeg_{legZ:F2}",
                    new Vector3(x, surface + 0.30f, legZ), new Vector3(0.055f, 0.60f, 0.055f), SignWood);
                Object.DestroyImmediate(leg.GetComponent<Collider>());
            }

            AddWallPlacard(parent, "Pares_A_Board", new Vector3(x, surface + 0.78f, z), 90.0f,
                new Vector2(0.92f, 0.72f), Chalkboard, new[] { "PARES", "MAMI" },
                "Small menu board carried by its two pavement legs beside the pares cart.");
        }

        /// <summary>
        /// One line of blocky text, laid out in the parent plate's local space.
        ///
        /// ⚠️ THE ALPHABET IS `PcExpressSignAuthor.Font`, SHARED ON PURPOSE. The map already had
        /// one 5-by-7 face for the shop fascia; a second copy here would be a second thing to
        /// keep in step, and the one that drifts is always the one nobody is looking at.
        /// </summary>
        private static void PaintText(Transform plate, string text, float centreY, float glyphH,
                                      string name, Color ink)
        {
            const float margin = 0.08f;
            float gap = 0.16f;
            float glyphW = (1.0f - margin * 2.0f - gap * glyphH * (text.Length - 1)) / text.Length;
            float x = -0.5f + margin;

            var root = new GameObject(name);
            root.transform.SetParent(plate, false);

            foreach (char c in text)
            {
                if (c == ' ') { x += glyphW + gap * glyphH; continue; }
                if (!PcExpressSignAuthor.Font.TryGetValue(c, out var rows))
                {
                    x += glyphW + gap * glyphH;
                    continue;
                }

                for (int col = 0; col < 5; col++)
                {
                    int run = -1;

                    for (int row = 0; row <= 7; row++)
                    {
                        bool lit = row < 7 && rows[row][col] == '1';

                        if (lit && run < 0) run = row;
                        if (lit || run < 0) continue;

                        float top = centreY + glyphH * (0.5f - run / 7.0f);
                        float bottom = centreY + glyphH * (0.5f - row / 7.0f);

                        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        bar.name = "g";
                        bar.transform.SetParent(root.transform, false);
                        bar.transform.localPosition = new Vector3(
                            x + glyphW * (col + 0.5f) / 5.0f, (top + bottom) * 0.5f, -0.62f);
                        bar.transform.localScale = new Vector3(glyphW / 5.0f, top - bottom, 0.30f);
                        Paint(bar, ink);
                        Object.DestroyImmediate(bar.GetComponent<Collider>());

                        run = -1;
                    }
                }

                x += glyphW + gap * glyphH;
            }
        }

        private static void AddClutter(Transform parent, string model, float x, float z, float yaw)
        {
            InstantiateProp(model, new Vector3(x, SurfaceTop(x), z),
                            Quaternion.Euler(0.0f, yaw, 0.0f), parent);
        }

        private static void BuildBoundaryTraffic(Transform parent)
        {
            var traffic = Group(parent, "BoundaryTraffic");
            (string model, float x, float z, float yaw, string palette)[] vehicles =
            {
                ("delivery", -3.8f, 19.0f, 180.0f, "tumbang-warm-a"),
                ("sedan", 2.6f, 18.45f, 0.0f, "tumbang-warm-c"),
                ("truck", -2.7f, -18.70f, 180.0f, "tumbang-warm-b"),
                ("van", 3.5f, -18.55f, 0.0f, "tumbang-warm-a"),
                ("taxi", 0.2f, 22.1f, 180.0f, "tumbang-warm-b"),
                ("van", -2.8f, 30.0f, 180.0f, "tumbang-warm-c"),
                ("truck", 3.1f, 35.0f, 0.0f, "tumbang-warm-a"),
                ("taxi", -3.0f, -28.5f, 0.0f, "tumbang-warm-b"),
                ("delivery", 3.0f, -33.5f, 180.0f, "tumbang-warm-c"),
            };

            for (int i = 0; i < vehicles.Length; i++)
            {
                var spec = vehicles[i];
                var vehicle = InstantiateKitProp("car", spec.model,
                    new Vector3(spec.x, RoadTop, spec.z), Quaternion.Euler(0.0f, spec.yaw, 0.0f),
                    Vector3.one * 1.35f, traffic, spec.palette);
                if (vehicle == null) continue;

                vehicle.name = $"BoundaryVehicle_{i}_{spec.model}";

                // Kenney cars import with their prefab root below the visible tyres. Placing the
                // root at road zero repeats Eskinita's measured 0.263 m vehicle float. Solve from
                // the rendered underside so every tyre, not an invisible origin, touches road.
                Bounds bounds = RenderBounds(vehicle);
                vehicle.transform.position += Vector3.up * (RoadTop - bounds.min.y);
                AirborneByDesign.Attach(vehicle,
                    "Wheel-supported boundary traffic. The rendered tyre underside is solved to road y = 0.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ NO HAZARD GOES NEAR THE CAN. The shipped map centred a pothole on the world
        /// origin, which is where the can spawns and therefore where every retrieval in the
        /// match converges. A hazard there does not add risk to a choice, it taxes the one move
        /// the whole game is about. The two road hazards sit off the spawn-to-can line at
        /// |x| = 3.4, where a runner meets them only if they cut a corner.
        /// </summary>
        private static void BuildTripHazards(Transform parent)
        {
            var hazardGroup = new GameObject("StreetTripHazards");
            hazardGroup.transform.SetParent(Group(parent, "Hazards"), false);

            CreateTripHazard(hazardGroup.transform, "TripHazard_PisonetCord",
                             8.4f, 1.6f, new Vector3(1.4f, 0.4f, 2.6f),
                             "CORD TRIP!", CordYellow);

            CreateTripHazard(hazardGroup.transform, "TripHazard_ParesSpill",
                             8.6f, -6.8f, new Vector3(1.8f, 0.4f, 1.8f),
                             "NADULAS!", BrothSlick);

            CreateTripHazard(hazardGroup.transform, "TripHazard_GpuBoxDebris",
                             -9.2f, 7.8f, new Vector3(1.6f, 0.4f, 2.2f),
                             "BOX TRIP!", CardboardTan);

            CreateTripHazard(hazardGroup.transform, "TripHazard_RoadPotholeWest",
                             -3.4f, 4.6f, new Vector3(2.2f, 0.4f, 1.6f),
                             "POTHOLE!", PotholeGrey);

            CreateTripHazard(hazardGroup.transform, "TripHazard_RoadPotholeEast",
                             3.6f, -5.2f, new Vector3(2.0f, 0.4f, 1.6f),
                             "POTHOLE!", PotholeGrey);
        }

        private static void CreateTripHazard(Transform parent, string name, float x, float z,
                                             Vector3 size, string popupText, Color burstColor)
        {
            var hazardGo = new GameObject(name);
            hazardGo.transform.SetParent(parent, false);
            hazardGo.transform.localPosition = new Vector3(x, SurfaceTop(x), z);

            var col = hazardGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0.0f, 0.2f, 0.0f);
            col.size = size;

            var trip = hazardGo.AddComponent<StreetTripHazard>();
            trip.TripDuration = 2.5f;
            trip.PopupText = popupText;
            trip.BurstColor = burstColor;
            trip.HazardRadius = Mathf.Max(size.x, size.z) * 0.6f;

            BuildTripHazardVisual(hazardGo.transform, name, size, burstColor);
        }

        private static void BuildTripHazardVisual(Transform parent, string name, Vector3 size, Color colour)
        {
            // ⚠️ THE TRIGGER FOOTPRINT IS NOT A COLOURED MAT. The v1 visual drew every hazard
            // as one opaque rectangle, exactly the "puddles everywhere" failure VISION § 2
            // forbids for abilities. These small objects show the cause while the invisible
            // trigger keeps the forgiving gameplay radius.
            if (name.Contains("PisonetCord"))
            {
                for (int i = -1; i <= 1; i++)
                {
                    var cord = Slab(parent, $"Cord_{i}", new Vector3(i * 0.16f, 0.018f, i * 0.28f),
                                    new Vector3(0.055f, 0.025f, size.z * 0.82f), colour);
                    cord.transform.localRotation = Quaternion.Euler(0.0f, -13.0f + i * 7.0f, 0.0f);
                    Object.DestroyImmediate(cord.GetComponent<Collider>());
                }
                return;
            }

            if (name.Contains("GpuBox"))
            {
                for (int i = 0; i < 3; i++)
                {
                    var box = InstantiateKitProp("factory", i == 2 ? "box-wide" : "box-small",
                        new Vector3(-0.45f + i * 0.42f, 0.0f, (i % 2 == 0 ? -0.28f : 0.24f)),
                        Quaternion.Euler(0.0f, -18.0f + i * 23.0f, i == 1 ? 8.0f : 0.0f),
                        Vector3.one * 0.72f, parent, "tumbang-warm-a");
                    if (box != null) box.name = $"DroppedGpuBox_{i}";
                }
                return;
            }

            int blobs = name.Contains("Pothole") ? 3 : 4;
            for (int i = 0; i < blobs; i++)
            {
                var blob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                blob.name = name.Contains("Pothole") ? $"BrokenAsphalt_{i}" : $"BrothSpill_{i}";
                blob.transform.SetParent(parent, false);
                float angle = i * 2.17f;
                blob.transform.localPosition = new Vector3(Mathf.Sin(angle) * size.x * 0.16f, 0.008f,
                                                            Mathf.Cos(angle) * size.z * 0.16f);
                blob.transform.localScale = new Vector3(size.x * (0.23f + i * 0.025f), 0.006f,
                                                        size.z * (0.18f + (blobs - i) * 0.018f));
                Paint(blob, colour);
                Object.DestroyImmediate(blob.GetComponent<Collider>());
            }
        }

        // ------------------------------------------------------------------

        private static GameObject Slab(Transform parent, string name, Vector3 centre, Vector3 size, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            Paint(go, tint);
            return go;
        }

        /// <summary>
        /// ⚠️⚠️ A REAL MATERIAL, NOT `MaterialKit.Dress`. Dress writes the tint into a
        /// `MaterialPropertyBlock`, which is a RUNTIME override and is not serialised into a
        /// scene file. Every plate built by this method at edit time would therefore save with
        /// the shared white material and load back white, and the ground would be a sheet of
        /// paper. Materials created here have no asset path, so Unity writes them into the
        /// scene, which is what is wanted for geometry only this map has.
        /// </summary>
        private static void Paint(GameObject go, Color tint)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var mat = new Material(MaterialKit.Lit.shader) { name = $"Ilalim_{go.name}" };
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.08f);

            renderer.sharedMaterial = mat;
        }

        private static readonly string[] KitTextureProperties =
        {
            "_BaseMap", "_MainTex", "baseColorTexture", "_BaseColorMap",
        };

        /// <summary>
        /// Instantiate a Kenney kit model and replace its complete colour atlas.
        ///
        /// ⚠️⚠️ THIS IS AN ATLAS SWAP, NOT A TINT. These kits are one mesh and one material,
        /// with walls, trim, roof and windows all sampling different regions of one image.
        /// Multiplying a colour into the material cannot move a saturated blue roof without
        /// also crushing the cream wall. The generated warm atlases preserve every UV and move
        /// the source swatches instead.
        /// </summary>
        private static GameObject InstantiateKitProp(string kit, string modelName, Vector3 position,
                                                      Quaternion rotation, Vector3 scale,
                                                      Transform parent, string palette)
        {
            string path = $"{KitsDir}/{kit}/{modelName}.glb";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[IlalimNgTulayBuilder] Missing kit model at {path}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"{kit}_{modelName}";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;

            string texturePath = $"{KitsDir}/{kit}/Textures/{palette}.png";
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            if (atlas == null)
            {
                Debug.LogWarning($"[IlalimNgTulayBuilder] Missing kit palette at {texturePath}");
                return instance;
            }

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var source = renderer.sharedMaterial;
                if (source == null) continue;

                var key = (source, atlas);
                if (!KitMaterials.TryGetValue(key, out var material) || material == null)
                {
                    material = new Material(source) { name = $"{source.name}_{palette}" };
                    bool set = false;

                    foreach (string property in KitTextureProperties)
                    {
                        if (!material.HasProperty(property)) continue;
                        material.SetTexture(property, atlas);
                        set = true;
                    }

                    if (!set)
                    {
                        Debug.LogWarning($"[IlalimNgTulayBuilder] '{source.shader.name}' has no " +
                                         $"known texture property for palette {palette}.");
                    }

                    KitMaterials[key] = material;
                }

                renderer.sharedMaterial = material;
            }

            return instance;
        }

        private static Bounds RenderBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject InstantiateProp(string modelName, Vector3 position,
                                                  Quaternion rotation, Transform parent)
        {
            string path = $"{ModelsDir}/{modelName}.obj";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[IlalimNgTulayBuilder] Missing model asset at {path}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = modelName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;

            return instance;
        }
    }
}
