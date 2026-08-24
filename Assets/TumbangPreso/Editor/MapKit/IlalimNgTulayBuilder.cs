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

        /// <summary>Underside of the LRT guideway, measured from `env_lrt_viaduct_deck.obj`.</summary>
        public const float ViaductSoffit = 8.0f;

        /// <summary>
        /// Rail head, measured from `env_lrt_viaduct_deck.obj`: `lrt_steel_rail` spans y 10.200
        /// to 10.360. The shipped map put the train at 10.300, which sank its wheels 60 mm into
        /// the rail it is supposed to be riding on.
        /// </summary>
        public const float RailHead = 10.360f;

        /// <summary>
        /// Centre of the westbound track, measured from the two rails that make it up:
        /// `lrt_steel_rail` boxes at x -2.360..-2.280 and -0.920..-0.840, so the centre is
        /// -1.600. The shipped value was right; only the height was wrong.
        /// </summary>
        public const float WestboundTrackX = -1.600f;

        // ------------------------------------------------------------------
        // Ground tints. Named here because this is the only map whose ground is built from
        // code, so EnvColourPass (which walks a `Dressing` node it does not have) never
        // reaches it.
        // ------------------------------------------------------------------

        private static readonly Color ConcreteApron = new Color(0.640f, 0.618f, 0.576f);
        private static readonly Color HazeGround = new Color(0.788f, 0.760f, 0.700f);
        private static readonly Color ChalkWhite = new Color(0.960f, 0.950f, 0.910f);
        private static readonly Color BoostPadGlow = new Color(0.100f, 0.760f, 0.900f);
        private static readonly Color CordYellow = new Color(0.900f, 0.800f, 0.120f);
        private static readonly Color BrothSlick = new Color(0.560f, 0.380f, 0.200f);
        private static readonly Color CardboardTan = new Color(0.720f, 0.560f, 0.340f);
        private static readonly Color PotholeGrey = new Color(0.300f, 0.300f, 0.320f);

        /// <summary>Under-bridge signage. Not tuned by eye: the first is the faded enamel red
        /// every "BAWAL UMIHI DITO" placard in the country is painted in, the second the tarpaulin
        /// blue a barangay notice is printed on.</summary>
        private static readonly Color BawalRed = new Color(0.720f, 0.180f, 0.150f);
        private static readonly Color TarpBlue = new Color(0.160f, 0.360f, 0.620f);

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

            const string skyMatPath = "Assets/TumbangPreso/Art/models/materials/Sky.mat";
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
            if (skyMat != null) RenderSettings.skybox = skyMat;
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
            // ⚠️ `CrossRow`, `Belt` AND `Malayo` ARE `EnvColourPass.FacadeGroups` ENTRIES, and
            // the last two are the ones it fades 68 per cent toward the sky. That fade is what
            // makes Eskinita's horizon sit BEHIND its street instead of beside it, and it is
            // free here for the cost of using the same two words.
            // ⚠️⚠️ THE NEAR BLOCKS ARE NOT IN A FACADE GROUP, AND THAT IS DELIBERATE. Eskinita
            // and Bayan Plaza dress themselves out of the Kenney City Kit, whose walls ship
            // near-white, so EnvColourPass MULTIPLYING a Manila facade tint into them is what
            // gives those maps their colour. This project's own env_building_block_* meshes
            // already carry the palette baked into their .mtl: block_a is cream 0.886/0.824/0.675
            // and block_b is terracotta 0.710/0.400/0.298. Multiplying terracotta by terracotta
            // gives 0.50/0.16/0.09, which is nearly black, and that is exactly how the whole
            // shopfront line came out of the first capture. Same palette, reached by the model
            // rather than by the pass.
            //
            // The far belt DOES stay in a facade group, because there the pass fades 68 per cent
            // toward the sky and a fade LIGHTENS: that is the horizon treatment the other two
            // maps have and it is what makes distance read as distance.
            var crossRow = Group(parent, "Gilid");
            var belt = Group(parent, "Belt");

            string[] blocks = { "env_building_block_a", "env_building_block_b",
                                "env_building_block_c", "env_building_block_d" };

            // The two cross rows that close the street, just past the walls and just inside the
            // built ground so their footings are never over the plate's edge.
            for (int end = -1; end <= 1; end += 2)
            {
                float z = end * (CorridorHalfZ - 3.0f);
                int i = 0;

                for (float x = -8.0f; x <= 8.0f; x += 4.0f)
                {
                    var block = InstantiateProp(blocks[(i + (end > 0 ? 2 : 0)) % blocks.Length],
                                                new Vector3(x, SurfaceTop(x), z),
                                                Quaternion.Euler(0.0f, end > 0 ? 180.0f : 0.0f, 0.0f),
                                                crossRow);
                    if (block != null) block.name = $"CrossBlock_{(end > 0 ? "N" : "S")}{i}";
                    i++;
                }
            }

            // The far belt. Thin and irregular on purpose: it exists to give the horizon a
            // profile, and a dense wall of blocks at this distance reads as a fence.
            var rng = new System.Random(20260824);
            int made = 0;

            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 30.0f + ring * 14.0f;

                for (int step = 0; step < 14; step++)
                {
                    if (rng.NextDouble() > 0.62) continue;

                    float angle = step / 14.0f * Mathf.PI * 2.0f + ring * 0.31f;
                    float x = Mathf.Sin(angle) * radius * 1.15f;
                    float z = Mathf.Cos(angle) * radius * 1.55f;

                    // Never in the street itself: the corridor is closed by the cross rows.
                    if (Mathf.Abs(x) < BacklotOuterX && Mathf.Abs(z) < CorridorHalfZ) continue;

                    var far = InstantiateProp(blocks[rng.Next(blocks.Length)],
                                              new Vector3(x, HazeTop, z),
                                              Quaternion.Euler(0.0f, rng.Next(4) * 90.0f, 0.0f),
                                              belt);
                    if (far == null) continue;

                    float lift = 1.0f + (float)rng.NextDouble() * 1.4f;
                    far.transform.localScale = new Vector3(1.6f, lift, 1.6f);
                    far.name = $"BeltX_{made}";
                    made++;
                }
            }
        }

        private static void BuildHeroStructures(Transform parent)
        {
            // "Tulay" is this map only: the guideway, its columns and the consist keep the
            // concrete and livery their own .mtl files carry, so the group name is deliberately
            // NOT one EnvColourPass paints. A facade tint on a railway viaduct would put a
            // Manila house colour on the one structure the map is named after.
            var heroGo = Group(parent, "Tulay");

            // 1. The guideway. Its footprint is 6.9 m by 40 m and the columns under it cover
            // about 3 per cent of that, so no support test can ever call it held up. It is
            // marked rather than special-cased, and the mark carries the reason.
            var deck = InstantiateProp("env_lrt_viaduct_deck", Vector3.zero, Quaternion.identity, heroGo);
            if (deck != null)
            {
                var deckCol = deck.AddComponent<BoxCollider>();
                deckCol.center = new Vector3(0.0f, 9.0f, 0.0f);
                deckCol.size = new Vector3(6.8f, 2.2f, 40.0f);

                AirborneByDesign.Attach(deck, "The LRT-2 guideway. Its soffit is 8.0 m up and the " +
                                              "four column rows under it cover 3 per cent of its footprint.");
            }

            // 2. The columns.
            //
            // ⚠️⚠️ NO COLUMN MAY STAND INSIDE THE CHALK. The shipped map put two of them at
            // z = -5.0, which is inside a box the taya is CLAMPED into: a 3.4 m wide obstacle in
            // the one room the defender cannot leave. That does not get reported as a placement
            // bug, it gets reported as the taya getting stuck or as one side of the can being
            // impossible to defend. Both live rows are now outside |z| = 7.
            //
            // ⚠️ AND THEY ARE SCALED TO 0.6 ON X AND Z. At full size the pair left a 1.6 m gap
            // down the middle of a 14 m carriageway, which is a chokepoint on the only line
            // between the south spawns and the can. At 0.6 the gap is 4.4 m and the columns
            // still read as columns. Y is untouched, so the shaft still meets the soffit at 8.0.
            CreateViaductPillar(heroGo, new Vector3(-3.2f, 0.0f, 10.0f), "LrtPillar_NorthWest");
            CreateViaductPillar(heroGo, new Vector3(3.2f, 0.0f, 10.0f), "LrtPillar_NorthEast");
            CreateViaductPillar(heroGo, new Vector3(-3.2f, 0.0f, -10.0f), "LrtPillar_SouthWest");
            CreateViaductPillar(heroGo, new Vector3(3.2f, 0.0f, -10.0f), "LrtPillar_SouthEast");
            CreateViaductPillar(heroGo, new Vector3(-3.2f, 0.0f, 19.0f), "LrtPillar_FarNorthWest");
            CreateViaductPillar(heroGo, new Vector3(3.2f, 0.0f, 19.0f), "LrtPillar_FarNorthEast");
            CreateViaductPillar(heroGo, new Vector3(-3.2f, 0.0f, -19.0f), "LrtPillar_FarSouthWest");
            CreateViaductPillar(heroGo, new Vector3(3.2f, 0.0f, -19.0f), "LrtPillar_FarSouthEast");

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
            trainSystemGo.transform.localPosition = new Vector3(WestboundTrackX, RailHead, 0.0f);

            var flyby = trainSystemGo.AddComponent<LrtTrainFlyby>();
            flyby.TrackX = WestboundTrackX;
            flyby.TrackY = RailHead;
            flyby.Speed = 24.0f;
            flyby.Interval = 24.0f;
            flyby.InitialDelay = 5.0f;

            AirborneByDesign.Attach(trainSystemGo, "The LRT-2 consist, riding the westbound rail " +
                                                   $"head at y = {RailHead:F3} on the guideway.");

            InstantiateProp("env_lrt_train_car", new Vector3(0.0f, 0.0f, 7.5f),
                            Quaternion.identity, trainSystemGo.transform);
            InstantiateProp("env_lrt_train_car", new Vector3(0.0f, 0.0f, -7.5f),
                            Quaternion.Euler(0.0f, 180.0f, 0.0f), trainSystemGo.transform);
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
                                           new Vector3(0.0f, 0.4f, 0.0f), Color.cyan, 3.5f, 1.5f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PadVisual";
            quad.transform.SetParent(padGo.transform, false);
            quad.transform.localPosition = new Vector3(0.0f, 0.012f, 0.0f);
            quad.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            quad.transform.localScale = new Vector3(1.8f, 1.8f, 1.0f);
            Paint(quad, BoostPadGlow);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        /// <summary>Column footprint scale. See the note at the call site in BuildHeroStructures.</summary>
        private const float PillarScale = 0.6f;

        private static void CreateViaductPillar(Transform parent, Vector3 pos, string name)
        {
            var pillar = InstantiateProp("env_lrt_pillar", new Vector3(pos.x, SurfaceTop(pos.x), pos.z),
                                         Quaternion.identity, parent);
            if (pillar == null) return;

            pillar.name = name;

            // ⚠️ Y IS LEFT AT 1.0. The shaft is modelled 0.000 to 8.000 and the guideway soffit is
            // at 8.000; scaling the height would either pull the column off the deck or push it
            // through it, and neither is visible from the ground where the join is 8 m up.
            pillar.transform.localScale = new Vector3(PillarScale, 1.0f, PillarScale);

            // Local space, so it scales with the transform. Measured from `env_lrt_pillar.obj`:
            // x +/-1.700, z -1.675..1.100, y 0..8.000.
            var col = pillar.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, 4.0f, -0.2875f);
            col.size = new Vector3(3.40f, 8.0f, 2.775f);

            // Bot steering radius, sized to what the column actually occupies after the scale
            // rather than to the number the unscaled model would have wanted.
            HazardVolume.Attach(pillar, 1.70f * PillarScale + 0.4f, -1);

            AddPointLight(pillar.transform, "MercuryVaporLamp", new Vector3(0.0f, 5.2f, -1.45f),
                          new Color(0.88f, 0.96f, 1.0f), 7.5f, 1.1f);
        }

        private static void BuildStreetProps(Transform parent)
        {
            // Four groups, and only two of them are ones `EnvColourPass` paints.
            //   Tindahan  the branded shops. They keep their own palette; PC Express red and
            //             blue, the pisonet's cyan screen and the pares cart's steel are the
            //             point of them and a facade tint would flatten all three.
            //   Kalat     street clutter, which the other maps also leave alone.
            //   Bahay     the shopfront row, which SHOULD take the Manila palette and the roof
            //             atlases, because it is the same row of houses the other maps have.
            //   Kable     poles and wires, untouched, matching Eskinita's group of the same name.
            var streetGo = Group(parent, "Tindahan");
            var kalat = Group(parent, "Kalat");
            var bahay = Group(parent, "Gilid");
            var kable = Group(parent, "Kable");

            // 1. The pisonet, against the east shopfront line.
            //
            // ⚠️ PLACED FROM ITS OWN MEASURED FOOTPRINT, NOT FROM ITS ORIGIN. `env_pisonet_kiosk`
            // runs local z -1.075..0.425, so a 90 degree yaw puts its near edge 1.075 m toward
            // the road from wherever it is dropped. Anything closer in than x = 8.1 therefore
            // reaches over the chalk at x = 7.0 and becomes an obstacle inside the taya's box.
            const float pisonetX = 9.0f;
            var pisonet = InstantiateProp("env_pisonet_kiosk",
                                          new Vector3(pisonetX, SurfaceTop(pisonetX), 3.5f),
                                          Quaternion.Euler(0.0f, 90.0f, 0.0f), streetGo.transform);
            if (pisonet != null)
            {
                pisonet.name = "Pisonet_Kiosk";

                var col = pisonet.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 0.98f, -0.325f);
                col.size = new Vector3(1.08f, 1.96f, 1.50f);

                var arcade = pisonet.AddComponent<PisonetInteractive>();
                arcade.ScreenLight = AddPointLight(pisonet.transform, "ScreenGlow",
                                                   new Vector3(0.0f, 1.22f, -0.10f),
                                                   new Color(0.0f, 0.90f, 1.0f), 3.0f, 1.2f);
            }

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

            // 5. Jersey barriers closing the two ends of the carriageway, outside the walls so
            // they frame the street without narrowing the play area.
            for (int end = -1; end <= 1; end += 2)
            {
                float z = end * (WallHalfZ + 1.4f);

                for (float x = -RoadHalfX + 1.0f; x < RoadHalfX; x += 2.0f)
                    AddJerseyBarrier(kalat, new Vector3(x, SurfaceTop(x), z),
                                     Quaternion.Euler(0.0f, 90.0f, 0.0f));
            }

            // 6. Utility poles on both pavements.
            //
            // ⚠️⚠️ THE CROSSARM HAS TO REACH OVER THE ROAD, AND IT WAS POINTING THE OTHER WAY.
            // `env_post_electric` is 7.4 m across its own X (measured: -0.800 to 6.600) because
            // almost all of that is the wire span, with the post itself near the origin. Yawed
            // the wrong way, all four poles hung their wires out over the back lots where no
            // camera ever looks, and the street had bare sky between the pavements. It was found
            // by `MapGeometryCheck`, not by looking: the support grid reported "0.212 x5,
            // 0.150 x20", meaning twenty of the twenty-five squares under each pole were over
            // the shopfront apron instead of over the carriageway.
            foreach (float z in new[] { -12.0f, 10.0f })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float x = side * (PavementOuterX - 1.2f);
                    InstantiateProp("env_post_electric", new Vector3(x, SurfaceTop(x), z),
                                    Quaternion.Euler(0.0f, side < 0 ? 180.0f : 0.0f, 0.0f), kable);
                }
            }

            // 7. The shopfront line either side, standing on the apron rather than on air. The
            // shipped map put five of these at |x| = 10.5 while the ground stopped at |x| = 9.
            const float shopX = 13.0f;
            string[] west = { "env_building_block_a", "env_building_block_b", "env_building_block_c" };
            string[] east = { "env_building_block_d", "env_building_block_a", "env_building_block_b" };

            for (int i = 0; i < 3; i++)
            {
                float z = -12.0f + i * 12.0f;

                var w = InstantiateProp(west[i], new Vector3(-shopX, SurfaceTop(-shopX), z),
                                        Quaternion.Euler(0.0f, 90.0f, 0.0f), bahay);
                if (w != null) w.name = $"Shophouse_W{i}";

                var e = InstantiateProp(east[i], new Vector3(shopX, SurfaceTop(shopX), z + 4.0f),
                                        Quaternion.Euler(0.0f, -90.0f, 0.0f), bahay);
                if (e != null) e.name = $"Shophouse_E{i}";
            }

            // 8. Clutter, all of it on the pavements and none of it in the box.
            AddClutter(kalat, "env_crate_stack", -9.4f, -3.2f, 25.0f);
            AddClutter(kalat, "env_oil_drum", -8.6f, -4.6f, 0.0f);
            AddClutter(kalat, "env_monobloc_chair", 8.2f, 1.2f, -60.0f);
            AddClutter(kalat, "env_monobloc_chair", 8.6f, -3.4f, 140.0f);
            AddClutter(kalat, "env_tire", 9.6f, -8.6f, 0.0f);
            AddClutter(kalat, "env_tire", -9.8f, 9.4f, 20.0f);
            AddClutter(kalat, "env_crate_stack", 9.2f, 7.4f, -15.0f);
            AddClutter(kalat, "env_bollard", -8.2f, -8.4f, 0.0f);
            AddClutter(kalat, "env_bollard", -8.2f, -6.4f, 0.0f);

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

            // ⚠️ FLAT ON THE COLUMN'S ROAD-FACING FACE, WHICH IS A MEASURED PLANE AND NOT A
            // GUESS. `env_lrt_pillar` runs x +/-1.700 and is scaled to 0.6, so a column at
            // x = -3.2 presents its inner face at -3.2 + 1.700 * 0.6 = -2.18. The first pass put
            // these at -2.05, which is 130 mm of daylight between a painted sign and the
            // concrete it is painted on. Its z span after the same scale is z -1.005..+0.660,
            // so the placard has to sit inside that too.
            float face = 3.2f - 1.7f * PillarScale;

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
            const float width = 1.10f;
            const float height = 0.72f;

            var go = Slab(parent, name, pos, new Vector3(width, height, 0.04f), tint);
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            Object.DestroyImmediate(go.GetComponent<Collider>());

            AirborneByDesign.Attach(go, "Painted flat on the LRT column face at x = " +
                                        $"{Mathf.Abs(pos.x):F2}. A sign on a wall has nothing under it.");

            // ⚠️ THE LETTERS ARE CHILDREN OF THE PLATE AND ARE SIZED IN ITS LOCAL SPACE, which is
            // a unit cube scaled to the plate. Everything below is therefore a fraction of the
            // plate, so changing the plate size cannot leave the text behind.
            float lineHeight = 1.0f / (lines.Length + 0.6f);

            for (int i = 0; i < lines.Length; i++)
            {
                float cy = 0.5f - lineHeight * (i + 0.8f);
                PaintText(go.transform, lines[i], cy, lineHeight * 0.72f, name + "_L" + i);
            }
        }

        /// <summary>
        /// One line of blocky text, laid out in the parent plate's local space.
        ///
        /// ⚠️ THE ALPHABET IS `PcExpressSignAuthor.Font`, SHARED ON PURPOSE. The map already had
        /// one 5-by-7 face for the shop fascia; a second copy here would be a second thing to
        /// keep in step, and the one that drifts is always the one nobody is looking at.
        /// </summary>
        private static void PaintText(Transform plate, string text, float centreY, float glyphH,
                                      string name)
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
                        Paint(bar, ChalkWhite);
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

        private static void AddJerseyBarrier(Transform parent, Vector3 pos, Quaternion rot)
        {
            var jb = InstantiateProp("env_jersey_barrier", pos, rot, parent);
            if (jb == null) return;

            var col = jb.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, 0.425f, 0.0f);
            col.size = new Vector3(0.62f, 0.85f, 2.0f);
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

            var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "HazardVisual";
            visual.transform.SetParent(hazardGo.transform, false);
            visual.transform.localPosition = new Vector3(0.0f, 0.010f, 0.0f);
            visual.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            visual.transform.localScale = new Vector3(size.x, size.z, 1.0f);
            Paint(visual, burstColor);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
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
