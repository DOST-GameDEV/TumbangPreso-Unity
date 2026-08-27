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

        /// <summary>
        /// How far the deck and its rails are DRAWN, as opposed to how much of it the map is
        /// built around.
        ///
        /// ⚠️⚠️ THE VIADUCT USED TO END IN MID AIR AT z = +/-24 AND THE TRAIN DID NOT. 🧑, off
        /// the 2026-08-25 build, with a shot of a carriage hanging in the sky past the skyline:
        /// *"this was train earlier btw it wasnt on tracks it was js floating there"*, then
        /// *"its weird that the bridge js cuts off, i want the rails to continue past map"*.
        ///
        /// `LrtTrainFlyby` runs the consist from z = -48 to z = +48 and PARKS IT AT -48 between
        /// passes, so for about 21 of every 24 seconds a train sat 24 m beyond the south end of
        /// a 48 m deck with nothing under it at all. The ride height was never wrong: the
        /// geometry check measures the consist resting on the rail head at y = 9.190 and it
        /// always did. What was wrong is that the deck stopped and the route did not.
        ///
        /// ⚠️ SO THE STRUCTURE AND THE SCENERY ARE NOW TWO DIFFERENT NUMBERS. `GuidewayLength`
        /// stays 48.0 and still owns the deck collider and the supported bays, because
        /// `Hero_Strike_Balance.md` § 1 measures footprints against the built map and the play
        /// corridor is only 33 m long. This one only adds bays and rail, out past the fog line,
        /// so the line reads as a line that goes somewhere.
        ///
        /// 112 m covers z = +/-56, which is the train's own travel of +/-48 plus its
        /// `TrainConsistHalfLength` of 7.8 with a bay to spare, so no part of the consist is
        /// ever over open air at either end of its run.
        /// </summary>
        public const float GuidewayVisualLength = 112.0f;

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
        /// <summary>
        /// The 240 m plate that closes the world.
        ///
        /// ⚠⚠ IT WAS BEING GIVEN A FACADE TINT AND A ROOF ATLAS, AND THAT IS WHY THE DISTANCE
        /// READ PINK. The plate was named `MalayoX_Ground` and parented under `Malayo`, and
        /// `EnvColourPass.IsBuilding` matches any instance whose name starts with `MalayoX_`. So
        /// the pass classified the GROUND as a mid-rise, replaced this colour with one of the
        /// seeded Manila facade tints and mapped a corrugated ROOF texture across it. In
        /// `ilalim_depth_overview_v19.png` every gap between the district blocks shows it: warm
        /// pink desert where there should be haze. It sits in its own `Lupa` group now, which no
        /// list in that pass names, so the colour below is the colour it keeps.
        ///
        /// ⚠ AND THE COLOUR CAME DOWN WITH IT. 0.788, 0.760, 0.700 was chosen to survive a
        /// facade tint it should never have been getting. Unpainted it is far too warm and too
        /// light for ground seen at 60 to 120 m through fog, so it is pulled toward the
        /// concrete apron and desaturated.
        /// </summary>
        private static readonly Color HazeGround = new Color(0.688f, 0.668f, 0.632f);
        private static readonly Color ChalkWhite = new Color(0.960f, 0.950f, 0.910f);
        private static readonly Color BoostPadGlow = new Color(0.120f, 0.680f, 0.530f);
        private static readonly Color BoostPadPlum = new Color(0.550f, 0.280f, 0.620f);
        private static readonly Color BoostPadGold = new Color(0.750f, 0.580f, 0.200f);
        private static readonly Color CordYellow = new Color(0.900f, 0.800f, 0.120f);
        private static readonly Color CardboardTan = new Color(0.720f, 0.560f, 0.340f);
        private static readonly Color PotholeGrey = new Color(0.300f, 0.300f, 0.320f);

        // ⚠️ `BrothSlick` WAS DELETED WITH THE PARES SPILL ON 2026-08-26. It had one
        // reader. See `BuildTripHazards` for why a slick was the wrong hazard for a game whose
        // only verb here is a trip.

        /// <summary>The three tones a hazard needs and a flat mat does not: the shadow inside an
        /// opening, the lit top edge of the lip around it, and the raw broken face between the
        /// two. `VISION.md` § 2 rule 3 asks for detail rather than area, and a hole reads as a
        /// hole because you can see that its edge is thick.</summary>
        private static readonly Color HazardVoid = new Color(0.030f, 0.032f, 0.038f);
        private static readonly Color HazardLip = new Color(0.310f, 0.318f, 0.336f);
        private static readonly Color HazardBreak = new Color(0.185f, 0.190f, 0.205f);

        /// <summary>⚠ THE SIGN PALETTE LIVES IN `StreetSignKit`, NOT HERE. Six sign colours used
        /// to be declared in this file and read by one method each. They are shared by eleven
        /// sign systems now, and a second copy of a colour is a second thing to keep in step for
        /// no gain: the faded enamel red every BAWAL placard in the country is painted in is one
        /// colour, wherever it is used.</summary>

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

            // ⚠️⚠️ THE FOURTH ARGUMENT IS THE TONEMAP EXPOSURE AND IT USED TO READ 0.15, WHICH
            // IS WHY THE SHIPPED PLAYER RENDERED THIS MAP BLACK. 🧑, off the .exe: *"New map is
            // just black wtf, i cant see shit properly"*.
            //
            // Nothing about the lighting rig was wrong. `BuildLighting` sets sun 1.15, trilight
            // ambient and a 0.85/0.90/0.98 sky, and the scene file carries all of it correctly.
            // `ColourGrade.shader` then multiplies the WHOLE FRAME by this number before the
            // ACES curve, so it is the one value in the map that can darken everything at once.
            //
            // Eskinita runs 0.92 and Bayan Plaza runs 0 (tonemap off). `TscnImporter` can only
            // ever emit 0 or a Godot `tonemap_exposure` that defaults to 1.0, so ⚠️ **0.15 is
            // not a number any import path can produce.** This is the one map built from code
            // rather than imported, and the value was typed by hand.
            //
            // Run the shader's own curve on it, with `_White` 1.85 so the divisor is
            // 1.85/1.9 = 0.9737. A mid-grey linear 0.5:
            //
            //     x      = 0.5 * 0.15 * 0.6 / 0.9737                        = 0.0462
            //     mapped = x(2.51x + 0.03) / (x(2.43x + 0.59) + 0.14)       = 0.0391
            //
            // The same pixel at 0.92 is 0.4088, so the map rendered **10.5x darker than
            // Eskinita**. The frame contrast of 1.12 then finished it: `lerp(0.5, c, 1.12)`
            // reaches zero at c = 0.05357, which works back through the tonemap to an input
            // threshold of 0.5922. ⚠️⚠️ **Every linear pixel below 0.59 clipped to pure black
            // before it reached the screen.** The arena sits under a solid viaduct with the sun
            // shadowed out, so the whole street was under that threshold and only the emissive
            // HUD, the sign lights and the road paint survived.
            //
            // ⚠️ WHY FOUR SIGNED-OFF SHOWCASE RENDERS MISSED IT. The grade is a camera pass, the
            // defect is worst under the deck, and a showcase camera is not pointed there.
            // `MapGeometryCheck` measures geometry and cannot see brightness at all.
            // `MapGradeSanityTests` now asserts the band so this cannot recur silently.
            //
            // ⚠️⚠️ THE CONTRAST IS NOW 1.03, AND THE RENDER IS WHAT DECIDED IT. The note here
            // used to read "the contrast stays 1.12 for now", with Eskinita's 1.03 named as the
            // fallback "if the street reads too crushed at 0.92". 🧑, off the build that shipped
            // the 0.92 exposure: *"less dark as before but still dark"*. That is the render this
            // value was waiting on, so the fallback is taken.
            //
            // Fixing the exposure did most of the work and did not finish it, because exposure
            // and contrast crush the street in two different ways and only one of them was
            // corrected. Running the same arithmetic forward at the SHIPPED 0.92 exposure, with
            // brightness 1.05, `lerp(0.5, c, contrast)` reaching zero at c = 0.5 - 0.5/contrast:
            //
            //     contrast 1.12: c = 0.05357, /1.05 = 0.05102 tonemapped, linear = 0.0966
            //     contrast 1.03: c = 0.01456, /1.05 = 0.01387 tonemapped, linear = 0.0422
            //
            // ⚠️ So at 1.12 every linear pixel below **0.0966** was still clipping to pure black
            // even after the exposure fix, and 1.03 pulls that floor down to **0.0422**: 2.3x
            // less of the range crushed. The map is lit at 0.5922 in the old build and 0.0966 in
            // the current one, which is why it went from unplayable to merely dark rather than
            // to correct. Under a solid viaduct with the sun shadowed out, the shadowed pavement
            // and the shopfronts in the deck's shade are exactly the values in that band.
            //
            // ⚠️ Eskinita has run 1.03 the whole time and is the map nobody has called dark, so
            // this is matching a known-good frame rather than inventing a number. Saturation
            // stays 1.15: the complaint is value, not colour, and 1.15 is what the cast was
            // graded against.
            var grade = mapRoot.AddComponent<MapGrade>();
            grade.Set(1.05f, 1.03f, 1.15f, 0.92f, 1.85f);

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
            BuildRoadSurfaceDetail(dressing.transform);
            BuildStreetFurniture(dressing.transform);
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
            var haze = Slab(Group(parent, "Lupa"), "FarGroundPlate",
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

            // 3. Asphalt. Sunk by the tile's own thickness so its SURFACE is y = 0.
            //
            // ⚠️⚠️ THE TILES ARE LAID BEFORE THE SUB-BASE NOW, AND THAT ORDER IS THE FIX. The
            // sub-base used to be built first, from typed-in arithmetic, and it landed with its
            // top face at exactly y = 0.0: the same plane as the asphalt surface above it. Two
            // coplanar opaque quads over the whole carriageway is textbook z-fighting, and it
            // does not read as a rendering artefact to a player, it reads as the street being
            // broken. 🧑: *"the floor bugs and looks like its phasing everytime i move"*, and
            // the phasing IS the depth comparison flipping per pixel as the camera moves.
            //
            // ⚠️ IT WAS INVISIBLE BEFORE THE GRADE WAS FIXED, which is why it surfaced only now.
            // At the old contrast every linear pixel below 0.0966 clipped to pure black, and the
            // asphalt sat under that: the road was one flat black shape, so two black shapes
            // fighting looked like one black shape. § 9.2 brightening the map is what exposed it.
            float tileBottom = RoadTop - RoadTileThickness;

            for (float z = -CorridorHalfZ + 1.0f; z < CorridorHalfZ; z += 2.0f)
            {
                for (float x = -RoadHalfX + 1.0f; x < RoadHalfX; x += 2.0f)
                {
                    var tile = InstantiateProp("env_road_tile", new Vector3(x, tileBottom, z),
                                               Quaternion.identity, road);

                    // ⚠️ MEASURED FROM THE FIRST TILE RATHER THAN ASSUMED FROM THE CONSTANT.
                    // `RoadTileThickness` is what the tile is BELIEVED to be; `bounds.min.y` is
                    // where the mesh actually ends, whatever its pivot happens to be. Solving the
                    // sub-base against the real underside is what stops this from silently
                    // becoming coplanar again the day the tile model is replaced.
                    if (tile != null) tileBottom = Mathf.Min(tileBottom, RenderBounds(tile).min.y);
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

            // The sub-base under the carriageway, so the asphalt tiles have mass under them and
            // the street reads as cut into the ground rather than laid on top of it.
            //
            // ⚠️⚠️ ITS TOP SITS BELOW THE TILES, NOT ON THEIR SURFACE. See the note above the
            // asphalt loop: this used to be solved to y = 0.0, which is the driving surface
            // itself. `SubBaseClearance` is the gap that keeps the two planes apart, and it is a
            // whole centimetre rather than a float epsilon because the depth buffer's precision
            // at this camera's far plane is what decides the fight, not the size of the number.
            const float SubBaseClearance = 0.01f;
            const float SubBaseThickness = 0.12f;

            float subBaseTop = tileBottom - SubBaseClearance;

            var subBase = Slab(road, "RoadSubBase",
                               new Vector3(0.0f, subBaseTop - SubBaseThickness * 0.5f, 0.0f),
                               new Vector3(RoadHalfX * 2.0f, SubBaseThickness, CorridorHalfZ * 2.0f),
                               AsphaltAlbedo);
            Object.DestroyImmediate(subBase.GetComponent<Collider>());
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

            // ⚠⚠ THE ROW USED TO BE FOUR INSTANCES A SIDE AT ONE SCALE, FLUSH TO ONE LINE,
            // AND THAT IS WHY IT READ AS A COMB. Every near facade was `Vector3.one * 5.0` with
            // its rendered near edge solved onto |x| = 11.0, so the parapet ran dead level, the
            // shopfronts made one unbroken plane, and the two sides mirrored each other closely
            // enough that `ilalim_corridor_v14.png` has no front and no back. Width, height,
            // setback and palette now vary per instance, and the two sides carry different
            // sequences on purpose: a mirrored street reads as a level-editor corridor.
            //
            // The setback is the one that does the most work. A shopfront pushed back 0.6 to
            // 1.1 m makes a doorway recess, gives the awning something to sit under and gives
            // the pavement clutter somewhere to gather, which is what turns a row of boxes into
            // premises. It is spent OUTWARD, away from the pavement, so the 4 m of legal flank
            // in § 1 is never touched.
            var westRow = new List<GameObject>();
            var eastRow = new List<GameObject>();

            westRow.Add(BuildSideFacade(near, -1, "building-k", -13.4f, 6.15f, 0.00f, "tumbang-warm-b", "Shophouse_W0"));
            westRow.Add(BuildSideFacade(near, -1, "building-f", -7.6f, 4.55f, 0.85f, "tumbang-warm-c", "Shophouse_W1"));
            westRow.Add(BuildSideFacade(near, -1, "building-b", -1.9f, 5.30f, 0.30f, "tumbang-warm-a", "Shophouse_W2"));
            westRow.Add(BuildSideFacade(near, -1, "building-c", 9.8f, 5.85f, 0.55f, "tumbang-warm-c", "Shophouse_W3"));
            westRow.Add(BuildSideFacade(near, -1, "building-m", 14.9f, 4.40f, 1.10f, "tumbang-warm-a", "Shophouse_W4"));

            eastRow.Add(BuildSideFacade(near, 1, "building-e", -14.2f, 4.70f, 0.95f, "tumbang-warm-c", "Shophouse_E0"));
            eastRow.Add(BuildSideFacade(near, 1, "building-h", -8.4f, 6.05f, 0.15f, "tumbang-warm-a", "Shophouse_E1"));
            eastRow.Add(BuildSideFacade(near, 1, "building-b", -2.6f, 4.90f, 0.70f, "tumbang-warm-b", "Shophouse_E2"));
            eastRow.Add(BuildSideFacade(near, 1, "building-j", 4.1f, 5.60f, 0.00f, "tumbang-warm-a", "Shophouse_E3"));
            eastRow.Add(BuildSideFacade(near, 1, "building-d", 11.6f, 6.20f, 0.45f, "tumbang-warm-c", "Shophouse_E4"));
            eastRow.Add(BuildSideFacade(near, 1, "building-l", 16.8f, 4.60f, 0.90f, "tumbang-warm-b", "Shophouse_E5"));

            BuildRoofline(parent, westRow, -1);
            BuildRoofline(parent, eastRow, 1);
            BuildSecondShopRow(parent);
            BuildShopSigns(parent, westRow, eastRow);

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
            BuildDistrictDetail(backlot, skyline);

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


        /// <summary>
        /// The district band: 30 to 120 m, massing and silhouette only.
        ///
        /// ⚠️⚠️ THE FAULT THIS ANSWERS IS "REPEATED PIECES", NOT "NOT ENOUGH PIECES".
        /// `BuildOuterDistrict` already puts 60-odd buildings out here and the v14 captures still
        /// read as a kit, because every one of them is the same KIND of object: a rectangular
        /// block, standing on the ground, at one of three palettes. What a real rail corridor has
        /// between the blocks is the stuff below, and none of it was on the map: yard fencing,
        /// storage tanks grouped with the warehouse that owns them, chimneys and pipe runs, a
        /// crane, rooftop hoardings, lamp columns down the far pavements, and traffic on the
        /// cross streets rather than only on the main one.
        ///
        /// ⚠️ NOTHING HERE IS INSIDE |z| = 16.5 OR |x| = 20. It is all beyond both gameplay walls
        /// and beyond the shopfront apron, so it cannot become cover, cannot be walked into, and
        /// cannot spend any of the ability floor budget. It exists to be looked past.
        /// </summary>
        private static void BuildDistrictDetail(Transform backlot, Transform skyline)
        {
            var rng = new System.Random(20260825);

            // 1. Yard fencing along the back lots. A warehouse without a fence reads as a model;
            //    with one it reads as a plot somebody owns.
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 14; i++)
                {
                    float z = -34.0f + i * 5.6f;
                    PlaceKit(backlot, "roads", "construction-fence", "tumbang-warm-b",
                             new Vector3(side * 21.4f, HazeTop, z), 0.0f, Vector3.one * 5.4f,
                             $"YardFence_{(side < 0 ? "W" : "E")}_{i}");
                }
            }

            // 2. Plant grouped with the warehouses that own it, never scattered on its own.
            (float x, float z, string model, float scale)[] plant =
            {
                (-27.5f, -20.0f, "chimney-large", 5.0f),
                (-31.0f,   6.0f, "chimney-medium", 4.4f),
                ( 28.5f, -26.0f, "chimney-medium", 4.6f),
                ( 32.0f,  14.0f, "chimney-large", 5.2f),
            };

            foreach (var spec in plant)
            {
                PlaceKit(backlot, "industrial", spec.model, "tumbang-warm-b",
                         new Vector3(spec.x, HazeTop, spec.z), 0.0f, Vector3.one * spec.scale,
                         $"BacklotStack_{spec.x:F0}_{spec.z:F0}");
            }

            // ⚠⚠ A PIPE RACK NEEDS THE RACK. The first version put six pipe runs 2.6 to 4.2 m
            // up with nothing beneath them, which the gate reported as six props standing on the
            // sky and which looks exactly as wrong as it sounds from the guideway shot. Each run
            // gets a pair of trestles down to the yard, the same answer the bridge hoop's rim
            // bracket got: build the support, do not excuse the absence of one.
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1.0f : 1.0f;
                float x = side * (25.5f + (i % 2) * 1.6f);
                float y = HazeTop + 2.6f + (i % 3) * 0.8f;
                float z = -22.0f + i * 9.0f;

                var pipe = PlaceKit(backlot, "factory", i % 3 == 0 ? "pipe-large-long" : "pipe-large",
                                    "tumbang-warm-b", new Vector3(x, y, z), 90.0f,
                                    Vector3.one * 3.2f, $"BacklotPipe_{i}");
                if (pipe == null) continue;

                AirborneByDesign.Attach(pipe,
                    $"Pipe run carried by its two trestles at y = {y:F2} in the back lot.");

                foreach (float legZ in new[] { z - 2.6f, z + 2.6f })
                {
                    var trestle = Slab(backlot, $"BacklotPipeTrestle_{i}_{legZ:F0}",
                                       new Vector3(x, (HazeTop + y) * 0.5f, legZ),
                                       new Vector3(0.34f, y - HazeTop, 0.34f),
                                       new Color(0.355f, 0.340f, 0.310f));
                    Object.DestroyImmediate(trestle.GetComponent<Collider>());
                }
            }

            PlaceKit(backlot, "factory", "crane", "tumbang-warm-b",
                     new Vector3(-33.0f, HazeTop, -30.0f), 34.0f, Vector3.one * 4.2f, "BacklotCrane");

            PlaceKit(backlot, "factory", "hopper-high-square", "tumbang-warm-a",
                     new Vector3(30.5f, HazeTop, -6.0f), -18.0f, Vector3.one * 4.0f, "BacklotHopper");

            // 3. Rooftop hoardings. Aurora Boulevard advertises at roof height, and a hoarding is
            //    the only thing out here that breaks a parapet line without adding a storey.
            (float x, float z, float y, float yaw, float scale)[] hoardings =
            {
                (-24.5f, -12.0f, 12.4f,  90.0f, 7.5f),
                ( 25.5f,   4.5f, 13.8f, -90.0f, 8.2f),
                (-26.0f,  17.5f, 11.2f,  90.0f, 6.8f),
                ( 27.0f, -22.0f, 12.9f, -90.0f, 7.8f),
                (  9.0f,  34.0f, 10.6f, 180.0f, 7.0f),
                ( -8.5f, -34.5f,  9.8f,   0.0f, 6.6f),
            };

            // ⚠⚠ THEY STAND ON MASTS, AND THE FIRST VERSION DID NOT. Six hoardings were placed
            // at a typed height with `AirborneByDesign` and the words "bolted to a district
            // parapet" on them. Nothing was under any of them: in
            // `ilalim_depth_overview_v19.png` they read as green boards hanging in the sky over
            // the rooftops, which is worse than having no hoardings at all because it is the one
            // kind of wrongness a viewer cannot explain to themselves. A highway hoarding on a
            // vacant lot is the honest object, it needs no roof to be solved against, and its
            // mast is what tells the eye how far away it is.
            for (int i = 0; i < hoardings.Length; i++)
            {
                var spec = hoardings[i];
                var board = InstantiateKitProp("roads", i % 2 == 0 ? "sign-highway-wide" : "sign-highway-detailed",
                    new Vector3(spec.x, spec.y, spec.z), Quaternion.Euler(0.0f, spec.yaw, 0.0f),
                    Vector3.one * spec.scale, skyline, $"tumbang-warm-{(char)('a' + i % 3)}");
                if (board == null) continue;

                board.name = $"Hoarding_{i}";
                AirborneByDesign.Attach(board,
                    $"Advertising hoarding carried by its two masts to the lot at y = {spec.y:F1}.");

                float lean = Mathf.Abs(Mathf.Sin(spec.yaw * Mathf.Deg2Rad)) > 0.5f ? 0.0f : 1.0f;
                foreach (float offset in new[] { -spec.scale * 0.30f, spec.scale * 0.30f })
                {
                    var mast = Slab(skyline, $"HoardingMast_{i}_{offset:F1}",
                        new Vector3(spec.x + offset * lean, (HazeTop + spec.y) * 0.5f,
                                    spec.z + offset * (1.0f - lean)),
                        new Vector3(0.42f, spec.y - HazeTop, 0.42f),
                        new Color(0.345f, 0.335f, 0.320f));
                    Object.DestroyImmediate(mast.GetComponent<Collider>());
                }
            }

            // 4. Lamp columns down the two background pavements, which is what actually tells the
            //    eye that the road continues rather than stops at the intersection.
            foreach (int end in new[] { -1, 1 })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float z = end * (20.0f + i * 9.0f);
                        PlaceKit(skyline, "roads", "light-square-double", "tumbang-warm-a",
                                 new Vector3(side * 9.6f, HazeTop, z), side < 0 ? 90.0f : -90.0f,
                                 Vector3.one * 9.5f,
                                 $"DistrictLamp_{(end < 0 ? "S" : "N")}_{(side < 0 ? "W" : "E")}_{i}");
                    }
                }
            }

            // 5. Traffic on the CROSS streets. The boundary traffic already runs the main road;
            //    a junction with nothing turning through it is what made the intersections read
            //    as painted-on rather than as somewhere the street goes.
            string[] cars = { "sedan", "taxi", "van", "delivery", "truck" };
            foreach (int end in new[] { -1, 1 })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float x = side * (14.0f + i * 6.5f);
                        float z = end * 31.0f + (i % 2 == 0 ? 2.1f : -2.1f);
                        var car = PlaceKit(skyline, "car", cars[rng.Next(cars.Length)],
                                           $"tumbang-warm-{(char)('a' + rng.Next(3))}",
                                           new Vector3(x, RoadTop, z), side < 0 ? 90.0f : -90.0f,
                                           Vector3.one * 1.35f,
                                           $"CrossTraffic_{(end < 0 ? "S" : "N")}_{(side < 0 ? "W" : "E")}_{i}");
                        ExcuseSuperstructure(car,
                            "Cross-street traffic beyond the gameplay wall. Its wheels are solved " +
                            "onto road y = 0 and are still gated; the body above them is carried by them.");
                    }
                }
            }

            // 6. A stabled freight consist in the rail corridor, well past the north wall. The map
            //    is named for a railway and the only rolling stock on it was the one that flies
            //    over the street every 24 s.
            for (int i = 0; i < 4; i++)
            {
                string carriage = i == 0 ? "train-diesel-a"
                                : i == 1 ? "train-carriage-container-red"
                                : i == 2 ? "train-carriage-flatbed" : "train-carriage-tank";
                // Every carriage in this kit carries `wheels-front` and `wheels-back` at
                // y = 0.3595 in source space, so the body sits 0.319 m over the yard at 2x scale
                // and is held there by its own bogies, exactly like a car.
                var stock = PlaceKit(backlot, "train", carriage, "tumbang-lrt",
                                     new Vector3(-38.0f, HazeTop, 26.0f + i * 9.4f), 0.0f,
                                     Vector3.one * 2.0f, $"StabledCarriage_{i}");
                ExcuseSuperstructure(stock,
                    "Stabled rail vehicle in the back lot. Its bogies are solved onto the yard " +
                    "and are still gated; the body above them is carried by them.");
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

        /// <summary>
        /// One shophouse on a near row.
        ///
        /// ⚠⚠ THE SETBACK IS SPENT AWAY FROM THE PLAYER, NEVER INTO THE PAVEMENT. The near
        /// edge is still solved from the RENDERED bounds rather than from the model origin,
        /// because the origin is not the shop face and hard-coding an offset per model is how
        /// five buildings ended up standing on air 1.5 m past the ground. A setback of `s`
        /// moves that solved edge to |x| = 11 + s, which opens a recess in the row and leaves
        /// every one of the 4 m of legal flank exactly where it was.
        /// </summary>
        private static GameObject BuildSideFacade(Transform parent, int side, string model, float z,
                                                  float scale, float setback, string palette,
                                                  string name)
        {
            var building = InstantiateKitProp("commercial", model,
                new Vector3(side * 13.5f, KerbTop, z),
                Quaternion.Euler(0.0f, side < 0 ? -90.0f : 90.0f, 0.0f),
                Vector3.one * scale, parent, palette);
            if (building == null) return null;

            building.name = name;

            Bounds bounds = RenderBounds(building);
            float nearX = side < 0 ? bounds.max.x : bounds.min.x;
            building.transform.position +=
                new Vector3(side * (PavementOuterX + setback) - nearX, 0.0f, 0.0f);
            return building;
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

            // ⚠️ THE BAYS RUN THE VISUAL LENGTH, THE COLLIDER RUNS THE STRUCTURAL ONE. See
            // `GuidewayVisualLength`: past z = +/-24 these are scenery carrying the line out of
            // the map, and giving them deck collider would put 64 m of solid surface into a
            // street whose geometry has been measured against a 48 m deck.
            for (float z = -GuidewayVisualLength * 0.5f + 2.0f; z < GuidewayVisualLength * 0.5f; z += 4.0f)
            {
                var bay = InstantiateKitProp("roads", "road-bridge",
                    new Vector3(0.0f, ViaductSoffit, z), Quaternion.identity,
                    new Vector3(GuidewayWidth, 2.0f, 4.0f), guideway.transform, "tumbang-warm-a");
                if (bay != null) bay.name = $"GuidewayBay_{z + GuidewayVisualLength * 0.5f:F0}";
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

                // The rail follows the deck the whole way. A deck that continues under a train
                // with the rail stopping short would trade one floating object for another.
                for (float z = -GuidewayVisualLength * 0.5f + 2.0f; z < GuidewayVisualLength * 0.5f; z += 4.0f)
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

            // ⚠️⚠️ THE EXTENDED DECK NEEDS COLUMNS OR IT IS A 112 m SLAB HANGING IN THE AIR,
            // which is the same complaint in a bigger size. The live rows above keep the 9 m
            // rhythm out to z = +/-19; these continue it to +/-55 so the line recedes on its own
            // supports instead of stopping being a viaduct at the fog line.
            //
            // ⚠️ THEY ARE SCENERY AND ARE BUILT DELIBERATELY DIFFERENTLY. `CreateViaductPillar`
            // attaches a `HazardVolume` and a mercury vapour lamp, and neither belongs 40 m
            // outside the arena: the hazard is gameplay in a place no player can stand, and
            // twenty-four more real-time point lights is a frame cost paid for something the fog
            // is already eating. `docs/VISION.md` § 2 counts what shares the box, and this is
            // outside it in the same sense the column placards are.
            foreach (float z in new[] { -55.0f, -46.0f, -37.0f, -28.0f, 28.0f, 37.0f, 46.0f, 55.0f })
            {
                CreateDistantViaductPillar(heroGo, -4.45f, z);
                CreateDistantViaductPillar(heroGo, 4.45f, z);
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
                // The intensity came down from 1.5 after v16. The fascia carries its own
                // emission on five plates, and a 1.5 point light 0.45 m in front of them clipped
                // the white letters to flat paper and washed the red field to pink across its
                // middle. See the note in tools/build_pc_express_logo_mesh.py.
                AddPointLight(pcex.transform, "SignboardGlowLight", new Vector3(0.0f, 3.5f, -3.6f),
                              new Color(1.0f, 0.94f, 0.86f), 4.6f, 0.85f);
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

            // ⚠️ THE CARS GO IN BEFORE THE FLYBY, because the flyby's `TrackY` has to be the
            // MEASURED ride height and there is nothing to measure until the meshes exist.
            InstantiateKitProp("train", "train-electric-city-a", new Vector3(0.0f, 0.0f, -5.1f),
                Quaternion.identity, Vector3.one * TrainScale, trainSystemGo.transform, "tumbang-lrt");
            InstantiateKitProp("train", "train-electric-city-b", Vector3.zero,
                Quaternion.identity, Vector3.one * TrainScale, trainSystemGo.transform, "tumbang-lrt");
            InstantiateKitProp("train", "train-electric-city-c", new Vector3(0.0f, 0.0f, 5.1f),
                Quaternion.identity, Vector3.one * TrainScale, trainSystemGo.transform, "tumbang-lrt");

            // ⚠️⚠️ THE RIDE HEIGHT IS SOLVED FROM THE CARS, NOT ASSUMED FROM THE MODEL ORIGIN.
            // `TrainRootY` is `RailHead`, and putting the ROOT there only seats the train if the
            // kit's origin happens to sit at the bottom of the wheels. It is a Kenney city-kit
            // model scaled by `TrainScale` 2.0, so any offset baked into that origin is doubled
            // before it reaches the rail, and the consist rides that far off the track. 🧑, off
            // the 2026-08-25 build: the trains float.
            //
            // ⚠️ AND `AirborneByDesign` IS WHY NO CHECK CAUGHT IT. It is attached to the consist
            // root below, so `MapGeometryCheck` excuses every renderer under it from the
            // resting test: the one check that measures whether things sit on other things was
            // told, correctly, that a train on a viaduct is meant to be in the air. An exemption
            // from "rests on the ground" was silently also an exemption from "rests on the rail".
            // Solving the height here is what makes the printed reason true rather than a claim.
            Bounds consist = RenderBounds(trainSystemGo);
            float rideY = TrainRootY + (RailHead - consist.min.y);
            trainSystemGo.transform.localPosition = new Vector3(WestboundTrackX, rideY, 0.0f);

            var flyby = trainSystemGo.AddComponent<LrtTrainFlyby>();
            flyby.TrackX = WestboundTrackX;
            // ⚠️⚠️ THE FLYBY HAS TO CARRY THE CORRECTED HEIGHT TOO. `LrtTrainFlyby` writes
            // `transform.position = (TrackX, TrackY, z)` every frame it runs, so a seated
            // transform with a stale `TrackY` is re-lifted the moment the first train departs
            // and the fix would appear to work only until the 5 s initial delay elapsed.
            flyby.TrackY = rideY;
            flyby.Speed = 18.0f;
            // ⚠️⚠️ THESE TWO ARE THE LIVE VALUES AND THE FIELD DEFAULTS ARE NOT.
            // `LrtTrainFlyby.Interval` is a public serialized field, so whatever this builder
            // writes is baked into `IlalimNgTulay.unity` and the default in the class is dead
            // text the moment the scene exists. Changing one without the other and rebuilding
            // the scene is how a tuning change appears to do nothing.
            //
            // ⚠️ 78 AND 6, ON REPORT: *"i want train to play rarely / like maybe when they
            // open the game"*. At 24 s a 90 s round carried three or four passes. Now a round
            // opens with one, so the player learns the map has a train, and sees at most one
            // more. See the notes on the fields themselves for the balance consequence.
            flyby.Interval = 150.0f;
            flyby.InitialDelay = 6.0f;
            flyby.OverheadHalfZ = WallHalfZ + TrainConsistHalfLength;

            AirborneByDesign.Attach(trainSystemGo, "The LRT-2 consist, riding the westbound rail " +
                                                   $"head at y = {RailHead:F3} on the guideway. " +
                                                   $"Root seated at y = {rideY:F3}, solved from " +
                                                   "the car bounds rather than the model origin.");
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

        /// <summary>
        /// A support column for the scenery run of the guideway, past the play corridor.
        ///
        /// ⚠️⚠️ IT IS SEATED AND SIZED BY MEASUREMENT, NOT BY REUSING THE LIVE ROW'S NUMBERS.
        /// `CreateViaductPillar` places at `SurfaceTop(x)`, which is the carriageway, and
        /// `PillarVerticalScale` is tuned to reach the soffit FROM the carriageway. Out here the
        /// road has ended: the geometry check measures solid floor only across z = +/-16.7, and
        /// what these land on is the `FarGroundPlate` at `HazeTop`, which is a different height.
        /// Reusing the live numbers would leave every one of them either sunk into the haze
        /// plate or short of the deck, and a column short of the deck is a floating column.
        ///
        /// So the kit bound is measured, scaled to exactly span `HazeTop` to `ViaductSoffit`,
        /// then translated so its foot sits on the plate. This is the rule `BuildSideFacade` and
        /// `ShopFaceX` already follow, and it cannot drift when the soffit or the haze moves.
        /// </summary>
        private static void CreateDistantViaductPillar(Transform parent, float x, float z)
        {
            var pillar = InstantiateKitProp("roads", "bridge-pillar-wide",
                new Vector3(x, HazeTop, z), Quaternion.identity,
                new Vector3(PillarHorizontalScale, PillarVerticalScale, PillarHorizontalScale),
                parent, "tumbang-warm-a");
            if (pillar == null) return;

            pillar.name = $"LrtPillarFar_{(z > 0 ? "North" : "South")}{(x < 0 ? "West" : "East")}_{Mathf.Abs(z):F0}";

            // No collider: `InstantiateKitProp` may hand one over and nothing out here is ever
            // touched, so it is removed rather than left for the physics broadphase to carry.
            var stray = pillar.GetComponent<Collider>();
            if (stray != null) Object.DestroyImmediate(stray);

            float want = ViaductSoffit - HazeTop;
            Bounds raw = RenderBounds(pillar);
            if (raw.size.y > 0.001f)
            {
                float scale = PillarVerticalScale * (want / raw.size.y);
                pillar.transform.localScale = new Vector3(PillarHorizontalScale, scale, PillarHorizontalScale);
            }

            // Re-measure after scaling: the kit's pivot is not its foot, so the scale moves the
            // bottom as well as the top and only a second reading can seat it.
            Bounds seated = RenderBounds(pillar);
            pillar.transform.position += new Vector3(0.0f, HazeTop - seated.min.y, 0.0f);
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
                               new Vector3(0.10f, 0.08f, 0.24f), StreetSignKit.BawalRed);
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
        // ------------------------------------------------------------------
        // Signage.
        //
        // ⚠⚠ THE PLATE BUILDER, THE FRAME BUILDER AND THE LETTER PAINTER THAT USED TO LIVE
        // HERE ARE GONE, AND SO IS THE LOOK THEY PRODUCED. `AddWallPlacard`, `AddFramedWallSign`
        // and `PaintText` gave every business on the strip the same horizontal rectangle with
        // the same 5-by-7 face at the same stroke weight, wall-flush at the same height, and
        // § 9.2 of the map document was ticked off by giving each of them a different STRING.
        // In `ilalim_street_life_v14.png` the six of them read as one sign painter with one
        // stencil set. `StreetSignKit` holds eleven sign SYSTEMS instead, sharing the one glyph
        // table this repository is allowed to have and varying aspect, weight, tracking, slant,
        // relief, silhouette, mounting and material.
        //
        // ⚠ THE WALL PLANE IS SOLVED FROM THE BUILDING, NOT TYPED IN. The near rows now carry
        // per-instance setbacks, so "the shopfront is at |x| = 11" stopped being true the moment
        // the row gained variety. Asking the rendered bounds is the same rule `BuildSideFacade`
        // already uses, and it cannot drift when a setback moves.
        // ------------------------------------------------------------------

        private static float ShopFaceX(GameObject building, int side)
        {
            if (building == null) return side * PavementOuterX;

            Bounds bounds = RenderBounds(building);
            return side < 0 ? bounds.max.x : bounds.min.x;
        }

        // ------------------------------------------------------------------
        // ⚠⚠ THE WALL PLANE WAS SOLVED FROM THE BUILDING AND THE WALL'S EXTENT WAS NOT, WHICH
        // IS THE SAME DRIFT `ShopFaceX` EXISTS TO PREVENT, LEFT OPEN ON THE OTHER TWO AXES. A
        // facade sign took its x from the rendered bounds and then typed in its z centre, its
        // width and its height, so nothing tied the lettering to the wall it is painted on. The
        // moment a shopfront's model, scale or setback changed, the sign stopped fitting it.
        //
        // `Sign_ComputerParts` is where it showed: 4.60 m of lettering placed at z = -1.2 on
        // `Shophouse_W2`, which is `building-b` at scale 5.30, and the run of capitals reaches
        // past the north end of that facade into the gap before `Shophouse_W3` at z = 9.8.
        // `PaintedWall` draws letters and NO plate, on purpose, so the overhang is not a board
        // sticking out past a corner that a player would read as a sign. It is loose capitals
        // hanging in mid air over the pavement. 🧑, on the 2026-08-25 build: "floating texg
        // here pls remove".
        //
        // ⚠ FIT, DO NOT JUST CLAMP THE CENTRE. Sliding a 4.60 m sign back inside a shorter wall
        // moves the word off the shopfront it names; narrowing it to the wall keeps it centred
        // on its own building. Width is surrendered first and the centre only moves after the
        // sign already fits, so a sign that was always inside its facade is left exactly where
        // it was authored.
        // ------------------------------------------------------------------

        private static void FitToFacade(GameObject building, ref Vector3 wallPoint,
                                        ref Vector2 size, float margin = 0.15f)
        {
            if (building == null) return;

            Bounds bounds = RenderBounds(building);

            float usableZ = bounds.size.z - margin * 2.0f;
            float usableY = bounds.size.y - margin * 2.0f;
            if (usableZ <= 0.0f || usableY <= 0.0f) return;

            if (size.x > usableZ) size.x = usableZ;
            if (size.y > usableY) size.y = usableY;

            wallPoint.z = ClampSpan(wallPoint.z, bounds.min.z, bounds.max.z, size.x, margin);
            wallPoint.y = ClampSpan(wallPoint.y, bounds.min.y, bounds.max.y, size.y, margin);
        }

        /// <summary>Keeps a span of <paramref name="extent"/> inside min..max, inset by a margin.</summary>
        private static float ClampSpan(float centre, float min, float max, float extent, float margin)
        {
            float half = extent * 0.5f;
            float lo = min + margin + half;
            float hi = max - margin - half;
            return lo > hi ? (min + max) * 0.5f : Mathf.Clamp(centre, lo, hi);
        }

        private static void BuildUnderBridgeSignage(Transform parent)
        {
            var sign = Group(parent, "Karatula");

            // Flat on the measured inner face: centres at |x| = 4.45 minus a 0.70 m half width.
            float face = 4.45f - PillarWorldHalf;

            // ⚠⚠ THE TEXT IS THE WHOLE POINT AND A BLANK PLATE IS WORSE THAN NOTHING. The first
            // pass put untitled red rectangles on the columns; in the capture they read as
            // missing textures, not as signage. The line every under-bridge column in the country
            // carries is the joke, so if it cannot be read it is not worth the draw call.
            // ⚠ THE YAW PUTS THE PRINTED FACE TOWARD THE ROAD, AND THE FIRST PASS HAD IT
            // BACKWARDS ON ALL THREE. The lettering is a child at the plate's local -Z, and a
            // yaw of +90 maps local -Z to world -X: on the WEST column that points into the
            // concrete. Both plates rendered as blank coloured rectangles with their text buried
            // inside the pillar, which in a capture reads as a missing texture.
            StreetSignKit.Placard(sign, "Bawal_West", new Vector3(-face, 1.25f, -10.0f), -90.0f,
                new Vector2(1.10f, 0.72f), StreetSignKit.BawalRed,
                new[] { "BAWAL", "UMIHI", "DITO" },
                "Enamel notice painted flat on the west LRT column face at x = " + face.ToString("F2") + ".");

            StreetSignKit.Placard(sign, "Bawal_East", new Vector3(face, 1.25f, 10.0f), 90.0f,
                new Vector2(1.10f, 0.72f), StreetSignKit.BawalRed,
                new[] { "BAWAL", "UMIHI", "DITO" },
                "Enamel notice painted flat on the east LRT column face at x = " + face.ToString("F2") + ".");

            // The civic voice, and the only cloth sign on the map. § 9.2 keeps it to one.
            StreetSignKit.Tarpaulin(sign, "Barangay_Tarp", new Vector3(-face, 1.74f, 19.0f), -90.0f,
                new Vector2(1.72f, 1.02f), StreetSignKit.TarpBlue,
                new[] { "BARANGAY", "PATROL" },
                "Barangay tarpaulin lashed at four corners to the far west LRT column.");
        }

        /// <summary>
        /// The businesses of the strip, one sign system each.
        ///
        /// ⚠⚠ THE ORDER IN THIS METHOD IS THE ORDER ALONG THE PAVEMENT, AND NO TWO NEIGHBOURS
        /// DRAW FROM THE SAME ROW OF § 10.4's TABLE. That adjacency rule is the whole point: a
        /// street with eleven sign types placed at random still shows two identical fascias side
        /// by side, and the eye finds that pair before it finds the variety. Reading either
        /// `pavement_west` or `pavement_east` from the showcase set is how the rule is checked,
        /// because those are the only two frames that put several businesses in one picture.
        ///
        /// ⚠ PC EXPRESS IS NOT IN THIS LIST. Its fascia is geometry on `env_pc_express_store.obj`,
        /// authored by `PcExpressSignAuthor` and carrying the traced official mark, because it is
        /// a real brand and the one recorded exception to the role-hue law.
        /// </summary>
        private static void BuildShopSigns(Transform parent, List<GameObject> west,
                                           List<GameObject> east)
        {
            var sign = Group(parent, "Karatula");
            float pavementW = SurfaceTop(-9.0f);
            float pavementE = SurfaceTop(9.0f);

            // ---------------- west pavement, south to north ----------------

            // ⚠ The two strapped banners are fitted for the same reason the painted word is.
            // They carry a plate, so an overhang reads as a board past a corner rather than as
            // loose letters, but it is the same sign hanging off the same unchecked wall.
            var labadaWall = west.Count > 0 ? west[0] : null;
            var labadaPoint = new Vector3(ShopFaceX(labadaWall, -1) + 0.06f, 3.05f, -13.4f);
            var labadaSize = new Vector2(0.74f, 3.30f);
            FitToFacade(labadaWall, ref labadaPoint, ref labadaSize);

            StreetSignKit.VerticalBanner(sign, "Sign_Labada", labadaPoint,
                -90.0f, labadaSize, StreetSignKit.ShopGreen, StreetSignKit.Ink,
                "LABADA", "Printed banner strapped to the west shopfront at two wall bands.");

            StreetSignKit.TinSheet(sign, "Sign_Xerox",
                new Vector3(-10.30f, pavementW, -9.0f), -90.0f,
                new Vector2(1.95f, 0.86f), 2.42f, StreetSignKit.RustedTin, StreetSignKit.SignMaroon,
                new[] { "XEROX PRINT" },
                "Hand-painted tin sheet nailed to its own two posts on the west pavement.");

            BuildRepairBladeSign(sign);

            // -------------------------------------------------------------------
            // ⚠️⚠️ `Sign_ComputerParts` IS DELETED, AND THIS IS THE SECOND TIME IT WAS REPORTED
            // AS FLOATING. 🧑 2026-08-25: *"floating texg here pls remove"*, answered by
            // `FitToFacade`; 🧑 2026-08-27, with a screenshot of the same wall: *"flowing
            // computer parts text pls remove"*.
            //
            // ⚠️⚠️ FITTING IT WAS THE WRONG FIX AND THE SECOND REPORT IS THE PROOF.
            // `StreetSignKit.PaintedWall` draws LOOSE CAPITALS AND NO PLATE, by construction:
            // its whole idea is paint straight onto a render, so every letter is its own piece of
            // geometry standing a few centimetres off a wall with nothing behind it. On a
            // stepped voxel facade under a viaduct, at any angle other than straight on, that
            // reads as text hanging in the air whether or not it is inside the wall's bounds.
            // Constraining the RECT never had anything to do with the thing being reported.
            //
            // ⚠️ THE STRIP LOSES NOTHING IT NEEDED. `Sign_PcRepair` (a projecting blade, four
            // metres south) and PC Express's own fascia already say what this row of shops sells,
            // and both are carried by real geometry. § 10.4's adjacency rule is unaffected: the
            // blade and the hung LOAD panel either side of the gap are different systems.
            //
            // ⚠️ AND `PaintedWall` STAYS IN `StreetSignKit`. It is a sign SYSTEM, it is correct
            // on a flat plastered wall, and deleting the only caller is not a reason to delete
            // the tool. If it is used again, it wants a wall with no relief.
            // -------------------------------------------------------------------

            // PC Express sits here, at z = 5.5. See the method note.

            StreetSignKit.HungPanel(sign, "Sign_Load",
                new Vector3(-10.42f, 2.44f, 15.0f), -90.0f, new Vector2(1.28f, 0.56f), 0.24f,
                StreetSignKit.ShopOchre, StreetSignKit.SignMaroon,
                new[] { "LOAD" },
                "Panel hung on two drop rods beneath the north west shopfront awning.");

            // ---------------- east pavement, south to north ----------------

            StreetSignKit.Pylon(sign, "Sign_Billiards",
                new Vector3(9.55f, pavementE, -14.0f), 90.0f, new Vector2(1.72f, 0.88f), 3.95f,
                StreetSignKit.ShopPlum, StreetSignKit.Ink,
                new[] { "BILLIARDS" },
                "Double-sided pylon on its own post at the east shopfront edge.");

            StreetSignKit.HungPanel(sign, "Sign_Barber",
                new Vector3(10.40f, 2.38f, -9.6f), 90.0f, new Vector2(1.34f, 0.58f), 0.22f,
                StreetSignKit.PlasticWhite, StreetSignKit.SignMaroon,
                new[] { "BARBER" },
                "Panel hung on two drop rods beneath the south east shopfront awning.");

            BuildParesBoard(sign);

            var palutoWall = east.Count > 2 ? east[2] : null;
            var palutoPoint = new Vector3(ShopFaceX(palutoWall, 1) - 0.06f, 2.95f, -0.4f);
            var palutoSize = new Vector2(0.70f, 3.05f);
            FitToFacade(palutoWall, ref palutoPoint, ref palutoSize);

            StreetSignKit.VerticalBanner(sign, "Sign_Paluto", palutoPoint,
                90.0f, palutoSize, StreetSignKit.SignMaroon, StreetSignKit.SignCream,
                "PALUTO", "Printed banner strapped to the east shopfront at two wall bands.");

            // ⚠ ABOVE THE AWNING, NOT BEHIND IT. At y = 2.86 the fascia sat inside the
            // `detail-awning-wide` mounted at 2.45, so the canopy cut a diagonal across its top
            // right corner in `ilalim_street_life_v20.png` and ate two letters. A shop fascia
            // goes over its own awning.
            //
            // ⚠️⚠️ AND ITS WALL PLANE IS SOLVED FROM `Shophouse_E3`, NOT TYPED IN. 🧑 2026-08-27,
            // with a screenshot: *"the pisonet sign"* is phasing. `10.94` was a literal, and
            // `BuildSideFacade` gives every shophouse its own per-instance SETBACK (E3 is
            // `building-j` at scale 5.60 with setback 0.00, and any of those three may be
            // retuned), so a typed x is only correct until somebody moves the building. This is
            // the exact drift `ShopFaceX` and `FitToFacade` were written for a wall away, on the
            // west side, and the east side was left typed. Same fix, same two calls: take the
            // face from the rendered bounds, then narrow the 3.45 m fascia to whatever the facade
            // can actually hold so it cannot overhang into the gap before `Shophouse_E4`.
            var pisonetWall = east.Count > 3 ? east[3] : null;
            var pisonetPoint = new Vector3(ShopFaceX(pisonetWall, 1) - 0.06f, 3.42f, 3.45f);
            var pisonetSize = new Vector2(3.45f, 0.84f);
            FitToFacade(pisonetWall, ref pisonetPoint, ref pisonetSize);

            StreetSignKit.FramedFascia(sign, "Sign_Pisonet",
                pisonetPoint, 90.0f, pisonetSize,
                StreetSignKit.SignCream, StreetSignKit.SignMaroon,
                new[] { "PISONET", "P1  5 MIN" },
                "Framed rate fascia bolted above the three pisonet terminals.");

            StreetSignKit.TinSheet(sign, "Sign_Goma",
                new Vector3(10.28f, pavementE, 9.2f), 90.0f,
                new Vector2(1.80f, 0.92f), 2.30f, StreetSignKit.RustedTin, StreetSignKit.SignCream,
                new[] { "GOMA" },
                "Hand-painted tin sheet nailed to its own two posts on the east pavement.");

            StreetSignKit.RoofLetters(sign, "Sign_Panaderia",
                new Vector3(ShopFaceX(east.Count > 4 ? east[4] : null, 1) + 0.55f,
                            RooflineOf(east.Count > 4 ? east[4] : null), 11.6f),
                90.0f, 3.60f, 0.82f, StreetSignKit.SignCream, "PANADERIA",
                "Free-standing roof letters on the north east parapet, carried by their truss.");
        }

        /// <summary>Top of a facade's rendered bounds, or a sane default if it failed to load.</summary>
        private static float RooflineOf(GameObject building) =>
            building != null ? RenderBounds(building).max.y : 6.0f;

        private static void BuildRepairBladeSign(Transform parent)
        {
            // ⚠ A BLADE IS READ WHILE MOVING, so it faces along the pavement rather than across
            // it. That is the one sign on the strip a player sees before they see the shop.
            StreetSignKit.Blade(parent, "Sign_PcRepair", new Vector3(-10.22f, 2.62f, -4.0f), 0.0f,
                new Vector2(1.02f, 1.55f), StreetSignKit.TarpBlue, StreetSignKit.SignCream,
                new[] { "PC", "REPAIR" },
                "Projecting blade sign carried by two brackets on the west shopfront.");
        }

        private static void BuildParesBoard(Transform parent)
        {
            const float x = 8.05f;
            StreetSignKit.ABoard(parent, "Sign_Pares", new Vector3(x, SurfaceTop(x), -3.75f), 90.0f,
                new Vector2(0.94f, 0.74f), new[] { "PARES", "MAMI" },
                "Chalk A-board standing on its own two legs beside the pares cart.");
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

                // ⚠⚠ THE EXCUSE THAT USED TO BE ON THIS LINE WAS COVERING A BUG, NOT A
                // DESIGN. Kenney cars import with their prefab root below the visible tyres, so
                // this solved the underside to road zero and then attached `AirborneByDesign`
                // because the gate still reported a 0.259 m float afterwards. Both halves were
                // wrong together: the solve read `Renderer.bounds`, whose world AABB cache had
                // not yet taken the position written one line earlier, so the correction was the
                // model's own local underside and the car ended up floating by precisely the
                // amount the excuse then forgave. `TryVisibleBounds` solves it through the
                // transform matrix instead, the wheels touch the road, and the excuse is gone:
                // a car parked on a street is not airborne by design.
                if (TryVisibleBounds(vehicle, out Bounds bounds))
                    vehicle.transform.position += Vector3.up * (RoadTop - bounds.min.y);

                ExcuseSuperstructure(vehicle,
                    "Boundary traffic. Its wheels are solved onto road y = 0 and are still gated; " +
                    "the body above them is carried by them.");
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

            // ⚠️⚠️ SEVEN BECAME FOUR ON 2026-08-26, AND THE COUNT WAS THE DEFECT. 🧑, playing
            // the build: there were *"too many"* of them and they did not read as things you
            // fall over. Seven triggers inside a street this size is not a hazard, it is a
            // hazard FIELD: the interesting choice a hazard buys is "do I cut this corner", and
            // it stops existing the moment every line across the road crosses one. It also fed
            // the re-trip loop directly, because two hazards 2.6 m apart could hand a player
            // back and forth. `CharacterMotor.IsTripImmune` fixes the loop; this fixes the cause.
            //
            // ⚠️ CUT BY FICTION, NOT BY POSITION. What went were the three that read as another
            // one already on the map:
            //   * `TripHazard_RoadPotholeWest` and `_RoadPotholeEast` were "a hole in the road",
            //     which is what the loose manhole and the sunken trench already are, and their
            //     whole visual was three flat grey cylinders. That is the puddle failure
            //     `VISION.md` § 2 rule 3 forbids: footprint doing the work that detail should.
            //     They also sat 2.6 m and 2.8 m from the manhole and the trench respectively,
            //     which is exactly the pair spacing the re-trip loop needed.
            //   * `TripHazard_ParesSpill` was a SLICK. Sliding and tripping are different
            //     verbs and the game only has the one, so it promised something it could not
            //     deliver, and it was drawn with the same four flat cylinders.
            //
            // The four that stayed each have a different silhouette, sit on something
            // `BuildRoadSurfaceDetail` already drew, and are at least 5.5 m from each other:
            // cord at (8.4, 1.6), trench at (4.6, -2.6), manhole at (-4.6, 2.4), boxes at
            // (-9.2, 7.8). The closest pair is the cord and the trench at 5.66 m, which is more
            // than an attacker covers during `Balance.TripGraceAfterGetUp`.
            CreateTripHazard(hazardGroup.transform, "TripHazard_PisonetCord",
                             8.4f, 1.6f, new Vector3(1.4f, 0.4f, 2.6f),
                             "CORD TRIP!", CordYellow);

            // -------------------------------------------------------------------
            // ⚠️⚠️ TWO BECAME ONE ON 2026-08-27, THE THIRD CUT FOR THE SAME REPORTED FEELING.
            // 🧑: *"lessen trip areas in map, maybe js one is okay, its overstimulating to have
            // allat"*. Seven to four, four to two, two to one. Three cuts is not somebody being
            // fussy about a number; it is the map having one trip hazard's worth of design in it
            // and the builder having authored several.
            //
            // ⚠️ THE CORD IS THE ONE THAT STAYS, AND IT IS NOT THE CLOSEST CALL. It is the only
            // one attached to a business that is already on the street: three pisonet terminals,
            // three chairs and a cable running to them, all authored by `BuildPisonetRow` before
            // any hazard existed. `TripHazard_GpuBoxDebris` at (-9.2, 7.8) was cardboard drawn
            // for the hazard's own sake, on a corner nothing else happens on, so removing it
            // costs the map no fiction at all.
            //
            // ⚠️ AND THE DISTANCE RULE FROM THE NOTE BELOW STILL BINDS ANYTHING ADDED BACK. The
            // cord stands 8.55 m from the can against a `CONFINEMENT_RADIUS` of 7.0, so it is met
            // by cutting a wide corner and never on a straight run for a tsinelas.
            // -------------------------------------------------------------------

            // -------------------------------------------------------------------
            // ⚠️⚠️ FOUR BECAME TWO ON 2026-08-27, AND THE TWO THAT WENT WERE THE TWO NEAREST THE
            // CAN. 🧑, playing the 4.72 build: *"theres too many places where u can trip can we
            // remove some? its annoying for everyone"*. That is the SECOND time this count has
            // been cut for the same reported feeling (seven to four on 2026-08-26, see above),
            // which is the tell that the count was never the real variable.
            //
            // ⚠️⚠️ WHAT MAKES A HAZARD ANNOYING IS NOT HOW MANY THERE ARE, IT IS WHETHER IT SITS
            // ON THE MOVE THE GAME IS ABOUT. `TripHazard_LooseManhole` at (-4.60, 2.40) and
            // `TripHazard_SunkenTrench` at (4.60, -2.60) stood **5.19 m and 5.30 m from the
            // world origin**, which is the can. `CONFINEMENT_RADIUS` is 7.0, so both sat squarely
            // inside the ring every single retrieval in the match runs through: not a risk
            // attached to a choice, a toll on the only route. The note above this method already
            // states the principle (*"a hazard there does not add risk to a choice, it taxes the
            // one move the whole game is about"*) and then applies it only to a 1.40 m exclusion,
            // which is far too small to mean it.
            //
            // ⚠️ THE TWO THAT STAY ARE AT 8.55 m AND 12.06 m FROM THE CAN, both outside
            // confinement entirely. You meet them running the long way round or cutting a wide
            // corner, which is a decision, and never on a straight run for your tsinelas.
            //
            // ⚠️ IF A THIRD IS EVER WANTED, THE BOUND IS DISTANCE FROM THE ORIGIN AND NOT THE
            // COUNT. Anything inside `Balance.ConfinementRadius` of the can is on the retrieval
            // line by construction, whatever it is drawn as and however few of them there are.
            // -------------------------------------------------------------------

            BuildFormerHazardDressing(parent);
        }

        // -------------------------------------------------------------------
        // § THE STREET KEEPS THE OBJECTS, IT JUST STOPS TRIPPING YOU OVER THEM
        //
        // ⚠️⚠️ 🧑 2026-08-27, immediately after the last cut: *"if u removed the trip shit can u
        // atleast keep the models that was in play area before? js delete the trip mechanic on
        // them, bcz i dontw ant play area to look empty"*. He is right, and the three cuts before
        // this one all made the same mistake: they deleted the OBJECT to delete the RULE.
        //
        // ⚠️⚠️ THE MECHANIC AND THE PROP ARE NOT THE SAME THING AND SHOULD NEVER HAVE BEEN
        // DELETED TOGETHER. An open manhole with its cast rim tipped up beside it, a resurfaced
        // trench that settled, and a pile of dropped GPU boxes are all street. They are what
        // Aurora Boulevard under the LRT actually looks like, they were drawn to the standard
        // `docs/VISION.md` § 2 rule 3 asks for (a silhouette, not a coloured mat), and the ROAD
        // is the one part of this map with nothing else on it. Taking them out to answer *"too
        // many places where u can trip"* threw away eleven pieces of authored geometry to remove
        // three trigger volumes.
        //
        // ⚠️ SO THIS BUILDS THE IDENTICAL VISUALS AND NOTHING ELSE. No `BoxCollider`, no
        // `StreetTripHazard`, no entry in `Hazards`. `BuildTripHazardVisual` branches on the
        // NAME, so each of these keeps the substring its artwork is selected by; that is the
        // whole reason the visuals could be kept without being copied.
        //
        // ⚠️⚠️ AND NOTHING AVOIDS THEM ANY MORE, WHICH IS THE POINT. `AIController`'s hazard
        // avoidance walks `StreetTripHazard` components, so a bot that used to path around the
        // manhole now walks straight over it, exactly as a player does. A prop that still bent
        // the bots' routes would be a hazard wearing a different name.
        //
        // ⚠️ THEY CARRY NO COLLIDER AT ALL, so `MapGeometryCheck`'s box-clearance rule has
        // nothing to catch even though the manhole and the trench sit 5.19 m and 5.30 m from the
        // can, inside `CONFINEMENT_RADIUS`. That is fine for paint and was NOT fine for a
        // trigger; the distinction is the entire content of the note above.
        // -------------------------------------------------------------------

        private static void BuildFormerHazardDressing(Transform parent)
        {
            // ⚠️ UNDER `Kalat` (litter), NOT UNDER `Hazards`. Where a thing lives in the hierarchy
            // is how the next reader learns what it is, and these are clutter now.
            var group = new GameObject("FormerHazardProps");
            group.transform.SetParent(Group(parent, "Kalat"), false);

            // Cut as a hazard 2026-08-27 (see above). Kept as road.
            CreateRoadDressing(group.transform, "Dressing_LooseManhole",
                               -4.6f, 2.4f, new Vector3(1.8f, 0.4f, 1.8f), HazardLip);

            CreateRoadDressing(group.transform, "Dressing_SunkenTrench",
                               4.6f, -2.6f, new Vector3(2.4f, 0.4f, 1.5f), HazardLip);

            // Cut as a hazard in the same pass that took the count to one. Kept as pavement clutter.
            CreateRoadDressing(group.transform, "Dressing_GpuBoxDebris",
                               -9.2f, 7.8f, new Vector3(1.6f, 0.4f, 2.2f), CardboardTan);
        }

        /// <summary>
        /// One former hazard's artwork, with nothing that can trip anybody.
        ///
        /// ⚠️ IT SHARES `BuildTripHazardVisual` RATHER THAN COPYING IT. A second copy of eleven
        /// slabs is a second thing to keep in step, and the day somebody retunes the manhole rim
        /// only one of the two would move.
        /// </summary>
        private static void CreateRoadDressing(Transform parent, string name, float x, float z,
                                               Vector3 size, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, SurfaceTop(x), z);

            BuildTripHazardVisual(go.transform, name, size, colour);
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
                // ⚠️⚠️ A CORD ONLY TRIPS YOU IF IT IS OFF THE GROUND, AND THESE WERE LYING ON
                // IT. Three flat strips at y = 0.018 are a painted line: nothing about them said
                // a foot would catch. They are now lifted to 0.055 m in the middle and pinned at
                // both ends by something that explains the lift, which is the same argument as
                // the manhole rim and the trench lip: the trippable part is a raised EDGE, and
                // the player has to be able to see it before they run at it.
                for (int i = -1; i <= 1; i++)
                {
                    var cord = Slab(parent, $"Cord_{i}", new Vector3(i * 0.16f, 0.055f, i * 0.28f),
                                    new Vector3(0.055f, 0.030f, size.z * 0.82f), colour);
                    cord.transform.localRotation = Quaternion.Euler(0.0f, -13.0f + i * 7.0f, 0.0f);
                    Object.DestroyImmediate(cord.GetComponent<Collider>());
                }

                // The extension block the cords run out of, at the shop end, and the coil of
                // slack beside it. It is what a player reads FIRST from a distance, because it
                // is the only part of this hazard tall enough to catch the eye, and it names the
                // cause: somebody ran power out to the pisonet across the pavement.
                var block = Slab(parent, "ExtensionBlock", new Vector3(0.0f, 0.055f, size.z * 0.44f),
                                 new Vector3(0.42f, 0.11f, 0.20f), new Color(0.94f, 0.92f, 0.86f));
                block.transform.localRotation = Quaternion.Euler(0.0f, -9.0f, 0.0f);
                Object.DestroyImmediate(block.GetComponent<Collider>());

                for (int i = 0; i < 3; i++)
                {
                    var coil = Slab(parent, $"CordCoil_{i}",
                                    new Vector3(0.30f + i * 0.02f, 0.032f + i * 0.020f,
                                                size.z * 0.40f - i * 0.05f),
                                    new Vector3(0.30f - i * 0.05f, 0.026f, 0.30f - i * 0.05f),
                                    colour);
                    coil.transform.localRotation = Quaternion.Euler(0.0f, 21.0f * i, 0.0f);
                    Object.DestroyImmediate(coil.GetComponent<Collider>());
                }

                // ⚠️ THE TAPE IS NOT DECORATION. It is the one piece of this hazard that says
                // "somebody knew this was a problem", which is what makes running into it the
                // player's fault rather than the map's.
                var tape = Slab(parent, "CordTape", new Vector3(-0.30f, 0.014f, -size.z * 0.30f),
                                new Vector3(0.34f, 0.012f, 0.16f), new Color(0.86f, 0.84f, 0.80f));
                tape.transform.localRotation = Quaternion.Euler(0.0f, -22.0f, 0.0f);
                Object.DestroyImmediate(tape.GetComponent<Collider>());
                return;
            }

            if (name.Contains("LooseManhole"))
            {
                // The lid, tipped up on one edge, and the dark hole under it. Flat enough to
                // stay well under `StepOffset`, so the box clearance rule never sees it.
                var hole = Slab(parent, "OpenShaft", new Vector3(0.10f, 0.008f, 0.05f),
                                new Vector3(0.78f, 0.012f, 0.78f), HazardVoid);
                Object.DestroyImmediate(hole.GetComponent<Collider>());

                // ⚠️⚠️ THE RIM IS THE WHOLE READ, AND IT WAS MISSING. The
                // hazards did not read as things you fall over, and a dark square on tarmac is
                // exactly why: that is a stain. A dark square with a RAISED CAST EDGE standing
                // proud of the road is an opening, and the edge is the part a toe actually
                // catches. Twelve segments on a 0.46 m circle is a ring at this size: eight
                // reads as an octagon from a player's eye height and twenty is geometry nobody
                // sees.
                //
                // ⚠️ 0.07 m TALL, FAR UNDER `CharacterController.stepOffset` (0.30 m).
                // The rim must never become something a body climbs, or the hazard would start
                // shoving players sideways instead of tripping them. It carries no collider at
                // all, like every other piece here.
                for (int i = 0; i < 12; i++)
                {
                    float a = i * (Mathf.PI * 2.0f / 12.0f);
                    var seg = Slab(parent, $"ShaftRim_{i}",
                                   new Vector3(0.10f + Mathf.Sin(a) * 0.46f, 0.035f,
                                               0.05f + Mathf.Cos(a) * 0.46f),
                                   new Vector3(0.15f, 0.07f, 0.26f), HazardLip);
                    seg.transform.localRotation =
                        Quaternion.Euler(0.0f, a * Mathf.Rad2Deg, 0.0f);
                    Object.DestroyImmediate(seg.GetComponent<Collider>());
                }

                // The raw shaft wall between the rim and the void, so the opening has depth
                // rather than being a decal with a frame around it.
                var throat = Slab(parent, "ShaftThroat", new Vector3(0.10f, 0.018f, 0.05f),
                                  new Vector3(0.86f, 0.030f, 0.86f), HazardBreak);
                Object.DestroyImmediate(throat.GetComponent<Collider>());

                var lid = Slab(parent, "TippedLid", new Vector3(-0.46f, 0.060f, -0.16f),
                               new Vector3(0.72f, 0.05f, 0.72f), new Color(0.205f, 0.208f, 0.222f));
                lid.transform.localRotation = Quaternion.Euler(0.0f, 18.0f, 26.0f);
                Object.DestroyImmediate(lid.GetComponent<Collider>());
                return;
            }

            if (name.Contains("SunkenTrench"))
            {
                // A resurfaced cut that settled, with the hazard paint somebody sprayed on it.
                //
                // ⚠️⚠️ THE BROKEN EDGE IS WHAT MAKES IT A TRENCH RATHER THAN
                // A PATCH. The previous draw was a slightly darker rectangle with three stripes
                // on it, which from a player's eye height is a differently coloured piece of
                // road: nothing about it said the surface STOPPED. What a settled cut looks like
                // is a ragged asphalt lip down each long side standing a few centimetres over a
                // floor that has dropped away, and the lip is the part a foot catches.
                var trenchFloor = Slab(parent, "TrenchFloor", new Vector3(0.0f, 0.006f, 0.0f),
                                       new Vector3(size.x * 0.62f, 0.010f, size.z * 0.78f),
                                       HazardVoid);
                Object.DestroyImmediate(trenchFloor.GetComponent<Collider>());

                var cut = Slab(parent, "SettledCut", new Vector3(0.0f, 0.012f, 0.0f),
                               new Vector3(size.x * 0.78f, 0.016f, size.z * 0.82f),
                               HazardBreak);
                Object.DestroyImmediate(cut.GetComponent<Collider>());

                // ⚠️ THE RAGGEDNESS IS DERIVED FROM THE INDEX, NEVER FROM `Random`.
                // This builder is re-run by `IlalimNgTulayPipeline` and its output is compared
                // against the scene by `MapGradeSanityTests`; an edge that rolled dice would
                // differ on every rebuild and no one could tell a regression from noise.
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float t = (i - 2.0f) / 2.0f;
                        var chunk = Slab(parent, $"TrenchLip_{(side > 0 ? "E" : "W")}_{i}",
                                         new Vector3(side * size.x * 0.34f,
                                                     0.024f + 0.012f * (i % 2),
                                                     t * size.z * 0.30f),
                                         new Vector3(0.16f + 0.05f * (i % 3),
                                                     0.048f + 0.022f * (i % 2),
                                                     size.z * 0.17f), HazardLip);
                        chunk.transform.localRotation =
                            Quaternion.Euler(0.0f, side * (9.0f + i * 6.0f), 0.0f);
                        Object.DestroyImmediate(chunk.GetComponent<Collider>());
                    }
                }

                for (int i = -1; i <= 1; i++)
                {
                    var stripe = Slab(parent, $"TrenchPaint_{i + 1}",
                                      new Vector3(i * size.x * 0.20f, 0.022f, 0.0f),
                                      new Vector3(0.11f, 0.010f, size.z * 0.62f), colour);
                    stripe.transform.localRotation = Quaternion.Euler(0.0f, 24.0f, 0.0f);
                    Object.DestroyImmediate(stripe.GetComponent<Collider>());
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

            // ⚠️⚠️ THERE IS NO GENERIC VISUAL ANY MORE, AND THAT IS THE POINT. Every branch
            // above returns, so reaching this line means a hazard was added with no drawing of
            // its own. What used to be here was a ring of flat cylinders, which is precisely the
            // failure that got three of the seven hazards deleted on 2026-08-26: a coloured mat
            // is a footprint pretending to be an object, and it teaches a player nothing about
            // why they fell. A loud editor warning costs one line and one rebuild; a silent mat
            // ships.
            Debug.LogWarning($"[IlalimNgTulay] {name} has no trip-hazard visual. A hazard must " +
                             "show its cause: a raised edge, a broken lip or a real object. See " +
                             "BuildTripHazardVisual, and VISION.md section 2 rule 3.");
        }

        // ------------------------------------------------------------------


        // ==================================================================
        // The composition pass. `docs/Ilalim_Ng_Tulay.md` § 10.
        // ==================================================================

        /// <summary>
        /// Instantiate a kit model and solve its Y from the RENDERED underside.
        ///
        /// ⚠️⚠️ EVERY PROP ADDED BY THIS PASS GOES THROUGH HERE, AND THAT IS NOT TIDINESS. The
        /// kits do not agree with each other about where a model's origin is: Kenney cars sit
        /// with their root below the tyres, several road props carry their origin at the top of
        /// a post, and the commercial buildings put it at a corner. Placing any of them at the
        /// surface height puts them into it or over it by a different amount each time, which is
        /// exactly the family of faults `MapGeometryCheck` was written for and which no render
        /// catches. Asking the rendered bounds cannot be wrong for a model that loaded.
        /// </summary>
        private static GameObject PlaceKit(Transform parent, string kit, string model, string palette,
                                           Vector3 groundPoint, float yaw, Vector3 scale, string name)
        {
            var go = InstantiateKitProp(kit, model, groundPoint, Quaternion.Euler(0.0f, yaw, 0.0f),
                                        scale, parent, palette);
            if (go == null) return null;

            go.name = name;

            // ⚠⚠ ACTIVE RENDERERS ONLY, AND THAT IS NOT THE SAME SET `RenderBounds` USES.
            // `RenderBounds` passes `includeInactive: true`, while `MapGeometryCheck` measures
            // `FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude)`. For most kit models
            // the two agree, and for the ones that ship a disabled variant renderer they do not:
            // the union with a stale inactive bound sits lower than the visible mesh, so solving
            // against it lifts the prop by the difference. That is exactly the 0.259 m the
            // Kenney cars are excused for in `BuildBoundaryTraffic`, and measuring the set the
            // gate measures fixes it instead of excusing it.
            if (!TryVisibleBounds(go, out Bounds bounds)) return go;

            go.transform.position += Vector3.up * (groundPoint.y - bounds.min.y);
            return go;
        }


        /// <summary>
        /// Excuse everything on a wheeled vehicle EXCEPT its wheels.
        ///
        /// ⚠️⚠️ `MapGeometryCheck` REQUIRES EVERY RENDERER TO REST, ONE AT A TIME, and a vehicle
        /// cannot satisfy that: its body is held up by its own wheels and its doors are held up
        /// by its body. The shipped answer was `AirborneByDesign` on the whole vehicle, which
        /// works and costs the one thing worth keeping. With the wheels excused too, a vehicle
        /// placed 0.26 m over the tarmac reports nothing at all, which is exactly what happened:
        /// the boundary cars floated for four versions behind an excuse whose own text named the
        /// number it was hiding.
        ///
        /// ⚠️ SO THE WHEELS STAY GATED. They are the parts that touch the road, they are the
        /// parts `TryVisibleBounds` solves against, and leaving them measurable is what makes
        /// the solve verifiable instead of asserted. Every Kenney car and every rail carriage in
        /// these kits names them `wheels-front`, `wheels-back` or `wheel-*`, and the elevated
        /// gate already keys off the same prefix for the LRT consist.
        /// </summary>
        private static void ExcuseSuperstructure(GameObject vehicle, string reason)
        {
            if (vehicle == null) return;

            foreach (var renderer in vehicle.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer.name.StartsWith("wheel", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                AirborneByDesign.Attach(renderer.gameObject, reason);
            }
        }

        /// <summary>
        /// The union of the ENABLED meshes on a prop, in world space, solved from each mesh's
        /// own bounds through its transform matrix.
        ///
        /// ⚠⚠ IT DOES NOT READ `Renderer.bounds`, AND THAT IS THE ENTIRE POINT OF IT.
        /// `Renderer.bounds` is a CACHED world AABB, and in an edit-mode builder the cache is
        /// still holding the value from the prefab's own origin at the moment a freshly
        /// instantiated object is measured. Solving a ground offset from it therefore reads the
        /// model's LOCAL underside as if it were a world height and lifts the prop by exactly
        /// that much: four stabled rail carriages, four different models, all four landing
        /// 0.319 m too high, and the Kenney cars in `BuildBoundaryTraffic` carrying a standing
        /// excuse for the same 0.259 m rather than a fix.
        ///
        /// ⚠ `Transform.localToWorldMatrix` IS COMPUTED ON DEMAND AND CANNOT BE STALE, and
        /// `Mesh.bounds` is a property of the asset rather than of the instance. Pushing the
        /// eight corners of the second through the first is exact, needs no transform flush, and
        /// gives the same answer whether it is called one line after `InstantiatePrefab` or a
        /// frame later.
        /// </summary>
        private static bool TryVisibleBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: false))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;

                var renderer = filter.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled) continue;

                Bounds local = mesh.bounds;
                Matrix4x4 toWorld = filter.transform.localToWorldMatrix;

                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1.0f : 1.0f,
                                           (corner & 2) == 0 ? -1.0f : 1.0f,
                                           (corner & 4) == 0 ? -1.0f : 1.0f);
                    Vector3 world = toWorld.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));

                    if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                    else bounds.Encapsulate(world);
                }
            }

            return any;
        }

        /// <summary>
        /// Roof equipment on the near shophouses.
        ///
        /// ⚠️⚠️ THIS IS THE CHEAPEST DETAIL ON THE MAP AND THE v14 SET HAD NONE OF IT. Every
        /// near facade ended in a flat parapet, so the whole street silhouetted as one stepped
        /// rectangle against the sky, and the eye has nothing to catch on above the awning line.
        /// A water tank, a run of aircon boxes, an aerial and a washing line cost four props and
        /// change the top edge of the frame in `corridor`, `pavement_west` and `overview` at
        /// once. It is also the honest read: every one of these buildings would have a tank.
        ///
        /// ⚠️ THE ROOF HEIGHT IS THE BUILDING'S OWN RENDERED TOP, so a prop placed on it rests on
        /// the building's bounds by construction and cannot be reported floating. Do not swap it
        /// for a typed height; the row now carries five different scales per side.
        /// </summary>
        private static void BuildRoofline(Transform parent, List<GameObject> row, int side)
        {
            var roofs = Group(parent, "Bubong");

            for (int i = 0; i < row.Count; i++)
            {
                var building = row[i];
                if (building == null) continue;

                Bounds bounds = RenderBounds(building);
                float top = bounds.max.y;
                float z = bounds.center.z;

                // ⚠⚠ OFFSETS ARE FRACTIONS OF THE BUILDING'S OWN EXTENTS, NEVER METRES. The
                // first version stepped a fixed 1.15 m and 3.10 m in from the rendered edge,
                // which is inside a 6.2-scale shophouse and OUTSIDE a 4.4-scale one: the roof
                // aerials on `Shophouse_W1` and `Shophouse_E2` landed past the parapet and the
                // gate correctly reported them standing 7.7 m and 6.3 m over the apron with the
                // building beside them rather than under them. A fraction of the half-extent
                // cannot leave the roof however the row is rescaled, and the row now carries
                // five different scales a side on purpose.
                float inner = bounds.center.x - side * bounds.extents.x * 0.42f;
                float outer = bounds.center.x + side * bounds.extents.x * 0.38f;
                float deep = Mathf.Min(1.2f, bounds.extents.z * 0.42f);
                string palette = $"tumbang-warm-{(char)('a' + (i + (side > 0 ? 1 : 0)) % 3)}";
                string tag = side < 0 ? "W" : "E";

                // A tank on four legs. The single most Filipino thing that can stand on a roof.
                if (i % 2 == 0)
                {
                    PlaceKit(roofs, "industrial", "detail-tank", palette,
                             new Vector3(outer, top, z + deep * 0.7f), i * 37.0f, Vector3.one * 1.45f,
                             $"RoofTank_{tag}{i}");
                }
                else
                {
                    PlaceKit(roofs, "industrial", i % 4 == 1 ? "chimney-small" : "chimney-medium",
                             palette, new Vector3(outer, top, z + deep), 0.0f, Vector3.one * 2.2f,
                             $"RoofStack_{tag}{i}");
                }

                // Aircon plant, clustered rather than spread: it is one building's kit.
                for (int unit = 0; unit < 2 + i % 2; unit++)
                {
                    PlaceKit(roofs, "factory", unit == 0 ? "box-wide" : "box-small", palette,
                             new Vector3(inner, top, z - deep + unit * deep * 0.7f),
                             12.0f * unit, Vector3.one * 0.85f, $"RoofPlant_{tag}{i}_{unit}");
                }

                // An aerial, at a different height on every roof so the skyline is not a comb.
                // ⚠ THE MAST STANDS ON THE ROOF AND IS NOT EXCUSED, so if it ever wanders off
                // the building again the gate says so. Only the CROSSARMS are excused, because a
                // crossarm bolted to a mast is the one thing here that genuinely hangs in air.
                float mast = 1.4f + (i % 3) * 0.55f;
                var pole = Slab(roofs, $"RoofAerial_{tag}{i}",
                                new Vector3(bounds.center.x, top + mast * 0.5f, z + deep * 0.5f),
                                new Vector3(0.06f, mast, 0.06f), new Color(0.28f, 0.30f, 0.32f));
                Object.DestroyImmediate(pole.GetComponent<Collider>());

                for (int arm = 0; arm < 3; arm++)
                {
                    var cross = Slab(pole.transform, $"Arm_{arm}",
                                     new Vector3(0.0f, 0.18f + arm * 0.22f, 0.0f),
                                     new Vector3(7.0f - arm * 1.6f, 0.30f, 0.5f),
                                     new Color(0.30f, 0.32f, 0.34f));
                    Object.DestroyImmediate(cross.GetComponent<Collider>());
                    AirborneByDesign.Attach(cross,
                        $"Aerial crossarm clamped to the mast standing on the {(side < 0 ? "west" : "east")} " +
                        $"shophouse roof at y = {top:F2}.");
                }

                // ⚠️ THE PREFIX IS `Sampay` ON PURPOSE. `EnvColourPass.WindPrefix` sweeps the whole
                // map root for it and sways anything it finds, so naming the line correctly is
                // the entire cost of getting moving laundry on this map.
                if (i % 2 == 1)
                {
                    var line = InstantiateProp("env_laundry_line",
                        new Vector3(side * (PavementOuterX + 2.2f), top - 1.1f, z),
                        Quaternion.Euler(0.0f, 90.0f, 0.0f), roofs);
                    if (line != null)
                    {
                        line.name = $"Sampay_Roof_{tag}{i}";
                        AirborneByDesign.Attach(line,
                            $"Washing strung across the {(side < 0 ? "west" : "east")} shophouse roof at y = {top - 1.1f:F2}.");
                    }
                }
            }
        }

        /// <summary>
        /// A second row of shopfronts behind the first, on the apron.
        ///
        /// ⚠️⚠️ THE GAP BETWEEN THE NEAR ROW AND THE FAR BELT IS WHAT MADE THE MAP LOOK ASSEMBLED.
        /// `ilalim_overview_v14.png` and `ilalim_corridor_v14.png` both show pale apron and haze
        /// ground THROUGH the setbacks and past the ends of the near row, because the next thing
        /// behind it was 13 m further out and half a storey shorter. The row below is deliberately
        /// taller than the one in front of it and offset so it shows through the gaps rather than
        /// lining up with them, which is what gives the street a middle distance at all.
        /// </summary>
        private static void BuildSecondShopRow(Transform parent)
        {
            var backRow = Group(parent, "Gilid");

            (int side, string model, float z, float x, float scale, string palette)[] row =
            {
                (-1, "building-i", -18.5f, 16.4f, 6.6f, "tumbang-warm-a"),
                (-1, "building-a", -10.5f, 17.8f, 7.4f, "tumbang-warm-c"),
                (-1, "building-n",  -3.0f, 16.9f, 6.2f, "tumbang-warm-b"),
                (-1, "building-g",   4.6f, 17.5f, 7.0f, "tumbang-warm-a"),
                (-1, "building-l",  12.8f, 16.6f, 6.4f, "tumbang-warm-c"),
                (-1, "building-c",  20.0f, 17.9f, 7.2f, "tumbang-warm-b"),
                ( 1, "building-a", -20.5f, 17.6f, 7.1f, "tumbang-warm-b"),
                ( 1, "building-l", -12.8f, 16.5f, 6.3f, "tumbang-warm-a"),
                ( 1, "building-f",  -5.4f, 17.9f, 7.5f, "tumbang-warm-c"),
                ( 1, "building-n",   2.2f, 16.7f, 6.1f, "tumbang-warm-b"),
                ( 1, "building-i",  10.4f, 17.4f, 6.9f, "tumbang-warm-a"),
                ( 1, "building-g",  18.6f, 16.8f, 6.7f, "tumbang-warm-c"),
            };

            foreach (var spec in row)
            {
                PlaceKit(backRow, "commercial", spec.model, spec.palette,
                         new Vector3(spec.side * spec.x, KerbTop, spec.z),
                         spec.side < 0 ? -90.0f : 90.0f, Vector3.one * spec.scale,
                         $"BackShop_{(spec.side < 0 ? "W" : "E")}_{spec.z:F0}");
            }
        }

        /// <summary>
        /// What a carriageway has on it.
        ///
        /// 🧑, 2026-08-25: *"u can put shit in the box too bcz it feels empty but not too much"*,
        /// *"js make sure the shit u put in it makes sense"*.
        ///
        /// ⚠️⚠️ EVERYTHING HERE IS FLAT, COLLIDERLESS AND DESATURATED, AND ALL THREE ARE RULES
        /// RATHER THAN STYLE. Flat, because `MapGeometryCheck.CheckBoxIsClear` fails the build on
        /// any solid collider taller than `StepOffset` inside the chalk and the taya is CLAMPED in
        /// there and cannot walk around anything. Colliderless, because a slab primitive arrives
        /// with a box collider and thirty road markings would be thirty findings. Desaturated,
        /// because `VISION.md` § 2 spends this exact surface on ability telegraphs: fire orange,
        /// ice cyan and void purple read loudest on quiet mid-value ground, and a bright decal in
        /// the box competes with the thing the box exists to display.
        ///
        /// ⚠️ THERE IS NO CENTRE LINE, AND THE WORN STRIP DOWN THE MIDDLE IS WHY. A painted centre
        /// line on a 14 m road runs through `Vector3.zero`, which is where the can spawns and
        /// where every retrieval in the match converges; drawing the eye there with a hard white
        /// stripe fights the lata for the most important square metre on the map. Two lane dashes
        /// at |x| = 3.5 and a patched, resurfaced middle say the same thing about the road and
        /// leave the centre quiet.
        /// </summary>
        private static void BuildRoadSurfaceDetail(Transform parent)
        {
            var marks = Group(parent, "Marka");

            Color lanePaint = new Color(0.700f, 0.680f, 0.620f);
            Color fadedPaint = new Color(0.560f, 0.548f, 0.505f);
            Color patch = new Color(0.238f, 0.250f, 0.278f);
            Color oldPatch = new Color(0.330f, 0.340f, 0.360f);
            Color ironwork = new Color(0.205f, 0.208f, 0.222f);
            Color stain = new Color(0.185f, 0.180f, 0.180f);

            GameObject Mark(string name, Vector3 centre, Vector3 size, Color tint)
            {
                var go = Slab(marks, name, centre, size, tint);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                return go;
            }

            // 1. Two dashed lane lines. 2.4 m of paint, 3.0 m of gap, the length of the corridor.
            for (int i = -8; i <= 8; i++)
            {
                float z = i * 5.4f;
                foreach (float x in new[] { -3.5f, 3.5f })
                {
                    Mark($"LaneDash_{(x < 0 ? "W" : "E")}_{i + 8}",
                         new Vector3(x, RoadTop + 0.006f, z), new Vector3(0.14f, 0.012f, 2.4f),
                         i % 3 == 0 ? fadedPaint : lanePaint);
                }
            }

            // 2. A crossing at each end. Outside the chalk by 3.8 m, so it never draws a line
            //    through the box, and worn enough to read as paint that has been driven over.
            foreach (int end in new[] { -1, 1 })
            {
                for (int bar = 0; bar < 6; bar++)
                {
                    float z = end * (10.8f + bar * 0.98f);
                    Mark($"Crossing_{(end < 0 ? "S" : "N")}_{bar}",
                         new Vector3(0.0f, RoadTop + 0.005f, z),
                         new Vector3(12.6f - bar * 0.4f, 0.010f, 0.48f),
                         bar % 2 == 0 ? lanePaint : fadedPaint);
                }
            }

            // 3. The resurfaced middle. Long, low-contrast rectangles of newer asphalt that
            //    explain the missing centre line and give the box texture at zero height.
            (float x, float z, float w, float l)[] patches =
            {
                (-0.9f, -4.6f, 3.2f, 7.4f),
                ( 1.4f,  3.1f, 2.6f, 9.0f),
                (-2.2f,  9.8f, 2.0f, 5.2f),
                ( 4.6f, -2.6f, 2.5f, 3.4f),   // the resurfaced trench the TRENCH! hazard sits in
                ( 4.9f, -9.4f, 2.4f, 4.6f),
                (-5.4f,  6.2f, 1.9f, 3.8f),
                ( 0.4f, -13.6f, 4.4f, 5.0f),
            };

            for (int i = 0; i < patches.Length; i++)
            {
                var p = patches[i];
                Mark($"AsphaltPatch_{i}", new Vector3(p.x, RoadTop + 0.004f, p.z),
                     new Vector3(p.w, 0.008f, p.l), i % 2 == 0 ? patch : oldPatch);
            }

            // 4. Manholes and inspection covers. Two stand inside the chalk, which is where a
            //    real road puts them, and both clear the can by more than `LataClearance`.
            (float x, float z, float r)[] covers =
            {
                (-4.6f,  2.4f, 0.36f),
                ( 4.2f, -3.6f, 0.32f),
                (-5.2f, -11.5f, 0.38f),
                ( 5.6f, 11.9f, 0.34f),
                ( 2.9f, 14.8f, 0.30f),
                (-1.8f, -16.2f, 0.36f),
            };

            for (int i = 0; i < covers.Length; i++)
            {
                var c = covers[i];
                var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lid.name = $"ManholeCover_{i}";
                lid.transform.SetParent(marks, false);
                lid.transform.localPosition = new Vector3(c.x, RoadTop + 0.008f, c.z);
                lid.transform.localScale = new Vector3(c.r * 2.0f, 0.008f, c.r * 2.0f);
                Paint(lid, ironwork);
                Object.DestroyImmediate(lid.GetComponent<Collider>());

                var collar = Mark($"ManholeCollar_{i}", new Vector3(c.x, RoadTop + 0.004f, c.z),
                                  new Vector3(c.r * 2.34f, 0.006f, c.r * 2.34f), oldPatch);
                collar.name = $"ManholeCollar_{i}";
            }

            // 5. Gutter grates against the kerb line, where the water actually goes.
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = -14.0f + i * 7.0f + (side > 0 ? 3.5f : 0.0f);
                    Mark($"GutterGrate_{(side < 0 ? "W" : "E")}_{i}",
                         new Vector3(side * 6.72f, RoadTop + 0.010f, z),
                         new Vector3(0.42f, 0.012f, 0.92f), ironwork);
                }
            }

            // 6. Skid arcs and one oil stain. Both live outside the chalk: they are the marks a
            //    vehicle leaves, and no vehicle is allowed inside the box.
            foreach (int end in new[] { -1, 1 })
            {
                for (int i = 0; i < 3; i++)
                {
                    var skid = Mark($"Skid_{(end < 0 ? "S" : "N")}_{i}",
                                    new Vector3(-2.6f + i * 0.42f, RoadTop + 0.003f, end * (9.4f + i * 0.5f)),
                                    new Vector3(0.20f, 0.006f, 3.6f - i * 0.4f), stain);
                    skid.transform.localRotation = Quaternion.Euler(0.0f, end * (6.0f + i * 2.0f), 0.0f);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Mark($"OilStain_{i}", new Vector3(3.9f + i * 0.30f, RoadTop + 0.003f, -11.8f + i * 0.5f),
                     new Vector3(1.5f - i * 0.35f, 0.006f, 1.1f - i * 0.22f), stain);
            }
        }

        /// <summary>
        /// Lamps, bins, planting and the parked clutter of a working pavement.
        ///
        /// ⚠️⚠️ IT ALL STANDS AT THE SHOPFRONT EDGE AND NONE OF IT IS MID-PAVEMENT. The 4 m of
        /// legal standing room on each long side is the reason this map exists at all
        /// (`docs/Ilalim_Ng_Tulay.md` § 1: Eskinita has 1.6 m and it is the fault that map is
        /// judged on). A bin placed for the composition, halfway across the pavement, spends the
        /// map's one advantage. The rule is the same one the utility line already follows.
        ///
        /// ⚠️ THE CLUSTERS ARE PER PREMISES, NOT PER METRE. Props are grouped where a business
        /// would put them, so each pile reads as somebody's stock, somebody's bins or somebody's
        /// seating rather than as scatter, which is the difference § 10 draws between detail and
        /// object count.
        /// </summary>
        private static void BuildStreetFurniture(Transform parent)
        {
            var street = Group(parent, "Kasangkapan");
            var kalat = Group(parent, "Kalat");

            // 1. A lamp column every 12 m, staggered across the street so the two rows never
            //    line up into a corridor of pairs.
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = -16.0f + i * 11.5f + (side > 0 ? 5.75f : 0.0f);
                    float x = side * 10.15f;
                    PlaceKit(street, "roads", side < 0 ? "light-curved" : "light-square",
                             "tumbang-warm-a", new Vector3(x, SurfaceTop(x), z),
                             side < 0 ? 90.0f : -90.0f, Vector3.one * 9.0f,
                             $"StreetLamp_{(side < 0 ? "W" : "E")}_{i}");
                }
            }

            // 2. The sari-sari store: the one business on the strip that needs no sign, because
            //    the goods in the window are the sign. It closes the gap between the pisonet row
            //    and the north end of the east pavement.
            var sariSari = InstantiateProp("env_sari_sari_store",
                new Vector3(9.55f, SurfaceTop(9.55f), 12.6f),
                Quaternion.Euler(0.0f, -90.0f, 0.0f), street);
            if (sariSari != null)
            {
                sariSari.name = "Sari_Sari_Store";
                var col = sariSari.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 1.30f, -0.35f);
                col.size = new Vector3(2.40f, 2.60f, 1.70f);
                AddPointLight(sariSari.transform, "SariSariBulb", new Vector3(0.0f, 2.05f, -0.55f),
                              new Color(1.0f, 0.92f, 0.72f), 4.2f, 1.05f);
            }

            AddClutter(kalat, "env_monobloc_chair", 8.35f, 11.6f, -70.0f);
            AddClutter(kalat, "env_monobloc_chair", 8.30f, 13.5f, 110.0f);
            AddClutter(kalat, "env_crate_stack", 10.05f, 14.1f, 12.0f);
            AddClutter(kalat, "env_halaman_lata", 8.80f, 12.4f, 0.0f);
            AddClutter(kalat, "env_halaman_lata", 8.95f, 13.9f, 40.0f);

            // 3. The vulcanising yard on the west pavement, under its tin sign. Tyres, a drum
            //    and a bucket read as a trade the moment they are in a heap rather than a line.
            AddClutter(kalat, "env_tire", -9.35f, -8.55f, 0.0f);
            AddClutter(kalat, "env_tire", -9.75f, -9.15f, 25.0f);
            AddClutter(kalat, "env_tire", -9.05f, -9.60f, 62.0f);
            AddClutter(kalat, "env_oil_drum", -10.05f, -10.2f, 0.0f);
            AddClutter(kalat, "env_bollard", -8.35f, -11.4f, 0.0f);

            // 4. The delivery corner outside PC Express: a hand truck's worth of boxes and a bin.
            for (int i = 0; i < 4; i++)
            {
                PlaceKit(kalat, "factory", i % 2 == 0 ? "box-large" : "box-wide", "tumbang-warm-a",
                         new Vector3(-10.10f + (i % 2) * 0.42f, SurfaceTop(-10.1f), 7.6f + i * 0.46f),
                         14.0f * i, Vector3.one * (0.90f - i * 0.06f), $"DeliveryBox_{i}");
            }

            var bin = PlaceKit(kalat, "roads", "dumpster", "tumbang-warm-b",
                               new Vector3(-9.95f, SurfaceTop(-9.95f), -14.6f), 90.0f,
                               Vector3.one * 3.6f, "ShopfrontDumpster");
            if (bin != null)
            {
                // The kit model ships with both lids modelled open, so they stand 0.49 m over
                // the pavement on their hinges. The body rests; the lids are the excused part.
                foreach (var lid in new[] { "lid-left", "lid-right" })
                {
                    var piece = bin.transform.Find(lid);
                    if (piece != null)
                        AirborneByDesign.Attach(piece.gameObject, "Open dumpster lid, on its hinge.");
                }
            }

            // 5. Planting, two clusters only. A street tree every ten metres is a suburb.
            PlaceKit(street, "city", "planter", "tumbang-warm-c",
                     new Vector3(-8.60f, SurfaceTop(-8.6f), 12.9f), 0.0f, Vector3.one * 4.5f,
                     "PavementPlanter_W");
            PlaceKit(street, "city", "tree-small", "tumbang-warm-c",
                     new Vector3(-8.60f, SurfaceTop(-8.6f), 12.9f), 30.0f, Vector3.one * 3.2f,
                     "PavementTree_W");
            PlaceKit(street, "town", "hedge", "tumbang-warm-c",
                     new Vector3(9.90f, SurfaceTop(9.9f), -12.2f), 90.0f, Vector3.one * 4.0f,
                     "PavementHedge_E");

            // 6. A parked tricycle at the north end. It is the vehicle that is allowed inside the
            //    walls, because it is parked ON THE PAVEMENT and not on the carriageway.
            var trike = InstantiateProp("env_tricycle",
                new Vector3(-9.45f, SurfaceTop(-9.45f), -2.2f),
                Quaternion.Euler(0.0f, 104.0f, 0.0f), street);
            if (trike != null) trike.name = "Parked_Tricycle";

            // 7. Washing between the two pavements is the layer that puts something ACROSS the
            //    street at eye-lift height without touching the guideway's shadow band.
            foreach (float z in new[] { -6.4f, 8.8f })
            {
                var line = InstantiateProp("env_laundry_line",
                    new Vector3(-10.4f, 3.35f, z), Quaternion.Euler(0.0f, 90.0f, 0.0f), street);
                if (line == null) continue;

                line.name = $"Sampay_Street_{z:F0}";
                AirborneByDesign.Attach(line,
                    $"Washing strung along the west shopfront at y = 3.35, z = {z:F1}.");
            }
        }

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
