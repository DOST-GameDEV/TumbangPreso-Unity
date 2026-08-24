using System;
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
    /// Programmatically generates and configures the "Ilalim ng Tulay" (LRT Gilmore Strip) map scene.
    /// Scene path: Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity
    /// </summary>
    public static class IlalimNgTulayBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string ModelsDir = "Assets/TumbangPreso/Art/models";

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

            // 1. Map Root
            var mapRoot = new GameObject("IlalimNgTulay");
            mapRoot.transform.position = Vector3.zero;

            // Environment Colour Pass
            mapRoot.AddComponent<EnvColourPass>();

            // Map Grade Component
            var grade = mapRoot.AddComponent<MapGrade>();
            grade.Set(1.05f, 1.12f, 1.15f, 0.15f, 1.85f);

            // 2. Lighting & Environment Setup
            BuildLighting(mapRoot.transform);

            // 3. Gameplay Systems & Rig (~Match, KillPlane, SpawnPoints)
            BuildGameplayRig(mapRoot.transform);

            // 4. Bounds & Collision Walls
            BuildBounds(mapRoot.transform);

            // 5. Decals (Throwing Line at Z=3.0, Base Circle at Z=13.5)
            BuildDecals(mapRoot.transform);

            // 6. Ground & Road Infrastructure
            var geomRoot = new GameObject("Geometry");
            geomRoot.transform.SetParent(mapRoot.transform, false);
            BuildGroundAndRoads(geomRoot.transform);

            // 7. Hero Structures (LRT Viaduct Deck, Pillars, PC Express Building)
            BuildHeroStructures(geomRoot.transform);

            // 8. Overhead LRT Train Flyby System
            BuildLrtTrainSystem(geomRoot.transform);

            // 9. PC Express Overclock Turbo Boost Pad
            BuildOverclockPad(geomRoot.transform);

            // 10. Street Props & Dressing (Pisonet, Pares Cart, Cargo Trike, Jersey Barriers, Buildings, Poles)
            BuildStreetProps(geomRoot.transform);

            // 11. Interactive Street Tripping Hazards
            BuildTripHazards(geomRoot.transform);

            // Ensure directories and save scene
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(saved
                ? $"[IlalimNgTulayBuilder] Successfully created and saved {ScenePath}"
                : $"[IlalimNgTulayBuilder] FAILED to save scene at {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return saved;
        }

        private static void BuildLighting(Transform parent)
        {
            var lightGroup = new GameObject("Lighting");
            lightGroup.transform.SetParent(parent, false);

            // Sun (Directional Light at 55 pitch, 35 yaw)
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

            // RenderSettings
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.90f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.45f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.165f, 0.208f, 0.294f); // #2a354b cool blue shadow

            const string skyMatPath = "Assets/TumbangPreso/Art/models/materials/Sky.mat";
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }
        }

        private static void BuildGameplayRig(Transform parent)
        {
            // ~Match (MatchInstaller)
            var matchGo = new GameObject("~Match");
            matchGo.transform.SetParent(parent, false);
            matchGo.AddComponent<MatchInstaller>();

            // KillPlane at Y = -10
            var killGo = new GameObject("KillPlane");
            killGo.transform.SetParent(parent, false);
            killGo.transform.position = new Vector3(0.0f, -10.0f, 0.0f);
            var kp = killGo.AddComponent<KillPlane>();
            var kpBox = killGo.AddComponent<BoxCollider>();
            kpBox.isTrigger = true;
            kpBox.size = new Vector3(KillPlane.PlaneExtent, KillPlane.PlaneThickness, KillPlane.PlaneExtent);

            // SpawnPoints (Preview pivot & match references)
            var spawnsGo = new GameObject("SpawnPoints");
            spawnsGo.transform.SetParent(parent, false);

            CreateSpawnMarker(spawnsGo.transform, "Spawn0", new Vector3(0.0f, 0.1f, 10.5f));  // Defender Mark
            CreateSpawnMarker(spawnsGo.transform, "Spawn1", new Vector3(-2.8f, 0.1f, -3.0f)); // Attacker 1
            CreateSpawnMarker(spawnsGo.transform, "Spawn2", new Vector3(0.0f, 0.1f, -3.0f));  // Attacker 2
            CreateSpawnMarker(spawnsGo.transform, "Spawn3", new Vector3(2.8f, 0.1f, -3.0f));  // Attacker 3
        }

        private static void CreateSpawnMarker(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
        }

        private static void BuildBounds(Transform parent)
        {
            var boundsGo = new GameObject("Bounds");
            boundsGo.transform.SetParent(parent, false);

            // Floor (Width 24m, Depth 40m, Thickness 0.5m)
            var floor = boundsGo.AddComponent<BoxCollider>();
            floor.center = new Vector3(0.0f, -0.25f, 0.0f);
            floor.size = new Vector3(24.0f, 0.5f, 40.0f);

            // Wall West (X = -8.6m)
            var wallWGo = new GameObject("WallWest");
            wallWGo.transform.SetParent(boundsGo.transform, false);
            var colW = wallWGo.AddComponent<BoxCollider>();
            colW.center = new Vector3(-8.6f, 3.0f, 0.0f);
            colW.size = new Vector3(1.0f, 6.0f, 40.0f);

            // Wall East (X = +8.6m)
            var wallEGo = new GameObject("WallEast");
            wallEGo.transform.SetParent(boundsGo.transform, false);
            var colE = wallEGo.AddComponent<BoxCollider>();
            colE.center = new Vector3(8.6f, 3.0f, 0.0f);
            colE.size = new Vector3(1.0f, 6.0f, 40.0f);

            // Wall North (Z = +16.5m)
            var wallNGo = new GameObject("WallNorth");
            wallNGo.transform.SetParent(boundsGo.transform, false);
            var colN = wallNGo.AddComponent<BoxCollider>();
            colN.center = new Vector3(0.0f, 3.0f, 16.5f);
            colN.size = new Vector3(20.0f, 6.0f, 1.0f);

            // Wall South (Z = -16.5m)
            var wallSGo = new GameObject("WallSouth");
            wallSGo.transform.SetParent(boundsGo.transform, false);
            var colS = wallSGo.AddComponent<BoxCollider>();
            colS.center = new Vector3(0.0f, 3.0f, -16.5f);
            colS.size = new Vector3(20.0f, 6.0f, 1.0f);
        }

        private static void BuildDecals(Transform parent)
        {
            var decalsGo = new GameObject("Decals");
            decalsGo.transform.SetParent(parent, false);

            // Throwing Line at Z = 3.0m (spanning X: -5.0m to +5.0m)
            var throwLine = GameObject.CreatePrimitive(PrimitiveType.Quad);
            throwLine.name = "ThrowingLine";
            throwLine.transform.SetParent(decalsGo.transform, false);
            throwLine.transform.localPosition = new Vector3(0.0f, 0.02f, 3.0f);
            throwLine.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            throwLine.transform.localScale = new Vector3(10.0f, 0.18f, 1.0f);
            
            // Remove standard collider from decal quad
            UnityEngine.Object.DestroyImmediate(throwLine.GetComponent<Collider>());

            // Base Circle at Z = 13.5m (Ring radius 1.2m)
            var baseCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseCircle.name = "BaseCircle";
            baseCircle.transform.SetParent(decalsGo.transform, false);
            baseCircle.transform.localPosition = new Vector3(0.0f, 0.015f, 13.5f);
            baseCircle.transform.localScale = new Vector3(2.4f, 0.005f, 2.4f);
            UnityEngine.Object.DestroyImmediate(baseCircle.GetComponent<Collider>());
        }

        private static void BuildGroundAndRoads(Transform parent)
        {
            var groundGo = new GameObject("GroundInfrastructure");
            groundGo.transform.SetParent(parent, false);

            // Central Asphalt Roadway (X: -5.0 to +5.0, Z: -18.0 to +18.0)
            for (int z = -18; z <= 18; z += 2)
            {
                for (int x = -4; x <= 4; x += 2)
                {
                    InstantiateProp("env_road_tile", new Vector3(x, 0.0f, z), Quaternion.identity, groundGo.transform);
                }

                // Curbs & Gutters at X = -5.0 and X = +5.0
                InstantiateProp("env_gutter_tile", new Vector3(-5.0f, 0.0f, z), Quaternion.identity, groundGo.transform);
                InstantiateProp("env_kerb_tile", new Vector3(-5.0f, 0.0f, z), Quaternion.identity, groundGo.transform);

                InstantiateProp("env_gutter_tile", new Vector3(5.0f, 0.0f, z), Quaternion.Euler(0, 180, 0), groundGo.transform);
                InstantiateProp("env_kerb_tile", new Vector3(5.0f, 0.0f, z), Quaternion.Euler(0, 180, 0), groundGo.transform);

                // Sidewalk Tiles (X: -8 to -6 on West, X: 6 to 8 on East)
                for (int sx = -8; sx <= -6; sx += 2)
                {
                    InstantiateProp("env_plaza_tile", new Vector3(sx, 0.15f, z), Quaternion.identity, groundGo.transform);
                }
                for (int sx = 6; sx <= 8; sx += 2)
                {
                    InstantiateProp("env_plaza_tile", new Vector3(sx, 0.15f, z), Quaternion.identity, groundGo.transform);
                }
            }
        }

        private static void BuildHeroStructures(Transform parent)
        {
            var heroGo = new GameObject("HeroStructures");
            heroGo.transform.SetParent(parent, false);

            // 1. LRT Overhead Viaduct Deck (Length 40m along Z, Underside at Y = 8.0m)
            var deck = InstantiateProp("env_lrt_viaduct_deck", new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity, heroGo.transform);
            if (deck != null)
            {
                var deckCol = deck.AddComponent<BoxCollider>();
                deckCol.center = new Vector3(0.0f, 9.0f, 0.0f);
                deckCol.size = new Vector3(6.8f, 2.2f, 40.0f);
            }

            // 2. Viaduct Tactical Pillars (Bank-shot & Cover Columns)
            CreateViaductPillar(heroGo.transform, new Vector3(-2.5f, 0.0f, 11.5f), "LrtPillar_TacticalLeft");
            CreateViaductPillar(heroGo.transform, new Vector3(2.5f, 0.0f, 11.5f), "LrtPillar_TacticalRight");
            CreateViaductPillar(heroGo.transform, new Vector3(-2.5f, 0.0f, -5.0f), "LrtPillar_SouthLeft");
            CreateViaductPillar(heroGo.transform, new Vector3(2.5f, 0.0f, -5.0f), "LrtPillar_SouthRight");

            // 3. PC Express Storefront (West Sidewalk X: -7.0, Z: 5.5, facing East into street +X with Euler 0, -90, 0)
            var pcex = InstantiateProp("env_pc_express_store", new Vector3(-7.0f, 0.15f, 5.5f), Quaternion.Euler(0.0f, -90.0f, 0.0f), heroGo.transform);
            if (pcex != null)
            {
                pcex.name = "PC_Express_Store";
                var pcexCol = pcex.AddComponent<BoxCollider>();
                pcexCol.center = new Vector3(0.0f, 2.0f, 0.0f);
                pcexCol.size = new Vector3(4.5f, 4.0f, 6.0f);

                // Interior warm light
                var intLightGo = new GameObject("InteriorWarmLight");
                intLightGo.transform.SetParent(pcex.transform, false);
                intLightGo.transform.localPosition = new Vector3(0.0f, 1.8f, 0.0f);
                var intLight = intLightGo.AddComponent<Light>();
                intLight.type = LightType.Point;
                intLight.color = new Color(1.0f, 0.95f, 0.82f);
                intLight.range = 7.5f;
                intLight.intensity = 1.3f;

                // Signboard Emissive Glow Light (PC Express Brand Green #00873e)
                var signLightGo = new GameObject("SignboardGlowLight");
                signLightGo.transform.SetParent(pcex.transform, false);
                signLightGo.transform.localPosition = new Vector3(0.0f, 3.5f, -3.2f);
                var signLight = signLightGo.AddComponent<Light>();
                signLight.type = LightType.Point;
                signLight.color = new Color(0.0f, 0.85f, 0.35f);
                signLight.range = 5.5f;
                signLight.intensity = 1.2f;

                // RGB Display Window Spotlight
                var rgbLightGo = new GameObject("RgbShowcaseLight");
                rgbLightGo.transform.SetParent(pcex.transform, false);
                rgbLightGo.transform.localPosition = new Vector3(-0.8f, 1.2f, -2.8f);
                var rgbLight = rgbLightGo.AddComponent<Light>();
                rgbLight.type = LightType.Point;
                rgbLight.color = new Color(0.2f, 0.8f, 1.0f);
                rgbLight.range = 4.0f;
                rgbLight.intensity = 1.1f;
            }
        }

        private static void BuildLrtTrainSystem(Transform parent)
        {
            var trainSystemGo = new GameObject("LrtTrainSystem");
            trainSystemGo.transform.SetParent(parent, false);
            trainSystemGo.transform.localPosition = new Vector3(-1.6f, 10.3f, 0.0f);

            var flyby = trainSystemGo.AddComponent<LrtTrainFlyby>();
            flyby.TrackX = -1.6f;
            flyby.TrackY = 10.3f;
            flyby.Speed = 24.0f;
            flyby.Interval = 24.0f;
            flyby.InitialDelay = 5.0f;

            // Train Car 1 (Lead Cab)
            InstantiateProp("env_lrt_train_car", new Vector3(0.0f, 0.0f, 7.5f), Quaternion.identity, trainSystemGo.transform);

            // Train Car 2 (Trailing Cab)
            InstantiateProp("env_lrt_train_car", new Vector3(0.0f, 0.0f, -7.5f), Quaternion.Euler(0.0f, 180.0f, 0.0f), trainSystemGo.transform);
        }

        private static void BuildOverclockPad(Transform parent)
        {
            var padGo = new GameObject("OverclockTurboPad");
            padGo.transform.SetParent(parent, false);
            padGo.transform.localPosition = new Vector3(-4.8f, 0.05f, 5.5f);

            var col = padGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.0f, 0.4f, 2.0f);

            var boost = padGo.AddComponent<OverclockBoostPad>();

            // RGB light
            var lightGo = new GameObject("OverclockRgbLight");
            lightGo.transform.SetParent(padGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 0.4f, 0.0f);
            var pLight = lightGo.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.range = 3.5f;
            pLight.intensity = 1.5f;
            pLight.color = Color.cyan;
            boost.PadLight = pLight;

            // Visual pad quad
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PadVisual";
            quad.transform.SetParent(padGo.transform, false);
            quad.transform.localPosition = new Vector3(0.0f, 0.02f, 0.0f);
            quad.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            quad.transform.localScale = new Vector3(1.8f, 1.8f, 1.0f);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static void CreateViaductPillar(Transform parent, Vector3 pos, string name)
        {
            var pillar = InstantiateProp("env_lrt_pillar", pos, Quaternion.identity, parent);
            if (pillar == null) return;

            pillar.name = name;

            // BoxCollider for physical collisions & slipper bank-shot ricochets
            var col = pillar.AddComponent<BoxCollider>();
            col.center = new Vector3(0.0f, 4.0f, 0.0f);
            col.size = new Vector3(1.6f, 8.0f, 1.6f);

            // HazardVolume for AI bot smooth steering around columns
            HazardVolume.Attach(pillar, 1.2f, -1);

            // Pillar Streetlamp Light
            var lampGo = new GameObject("MercuryVaporLamp");
            lampGo.transform.SetParent(pillar.transform, false);
            lampGo.transform.localPosition = new Vector3(0.0f, 5.2f, -1.45f);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(0.88f, 0.96f, 1.0f);
            lamp.range = 7.5f;
            lamp.intensity = 1.1f;
            lamp.shadows = LightShadows.None;
        }

        private static void BuildStreetProps(Transform parent)
        {
            var streetGo = new GameObject("StreetDressing");
            streetGo.transform.SetParent(parent, false);

            // 1. Pisonet Kiosk (East Sidewalk X = 6.8, Z = 3.5, facing West 90 deg into road)
            var pisonet = InstantiateProp("env_pisonet_kiosk", new Vector3(6.8f, 0.15f, 3.5f), Quaternion.Euler(0.0f, 90.0f, 0.0f), streetGo.transform);
            if (pisonet != null)
            {
                pisonet.name = "Pisonet_Kiosk";
                var col = pisonet.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 0.95f, -0.2f);
                col.size = new Vector3(1.1f, 1.9f, 1.6f);

                // Add interactive arcade component
                var arcade = pisonet.AddComponent<PisonetInteractive>();

                // Cyber CRT/LCD screen point light
                var screenLightGo = new GameObject("ScreenGlow");
                screenLightGo.transform.SetParent(pisonet.transform, false);
                screenLightGo.transform.localPosition = new Vector3(0.0f, 1.25f, -0.4f);
                var sLight = screenLightGo.AddComponent<Light>();
                sLight.type = LightType.Point;
                sLight.color = new Color(0.0f, 0.90f, 1.0f);
                sLight.range = 3.0f;
                sLight.intensity = 1.2f;
                arcade.ScreenLight = sLight;
            }

            // 2. Street Pares Food Cart (East Sidewalk X = 6.8, Z = -5.0, facing West 90 deg into road)
            var pares = InstantiateProp("env_street_pares_cart", new Vector3(6.8f, 0.15f, -5.0f), Quaternion.Euler(0.0f, 90.0f, 0.0f), streetGo.transform);
            if (pares != null)
            {
                pares.name = "Street_Pares_Cart";
                var col = pares.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 1.3f, 0.0f);
                col.size = new Vector3(1.9f, 2.6f, 1.8f);

                // Add interactive pares food cart component
                pares.AddComponent<StreetParesInteractive>();

                var brothLightGo = new GameObject("FoodWarmerLight");
                brothLightGo.transform.SetParent(pares.transform, false);
                brothLightGo.transform.localPosition = new Vector3(0.0f, 1.4f, 0.0f);
                var bLight = brothLightGo.AddComponent<Light>();
                bLight.type = LightType.Point;
                bLight.color = new Color(1.0f, 0.70f, 0.30f);
                bLight.range = 3.8f;
                bLight.intensity = 1.0f;
            }

            // 3. Cargo Delivery Tricycle with PC Boxes (West Sidewalk edge X = -5.5, Z = -2.5)
            var trike = InstantiateProp("env_cargo_tricycle_boxes", new Vector3(-5.5f, 0.05f, -2.5f), Quaternion.Euler(0.0f, 30.0f, 0.0f), streetGo.transform);
            if (trike != null)
            {
                trike.name = "Cargo_Tricycle_Boxes";
                var col = trike.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 0.75f, 0.0f);
                col.size = new Vector3(2.2f, 1.5f, 2.4f);
            }

            // 4. Jersey Roadside Barriers
            // Along Southern Road Boundary (Z = -15.5)
            for (float x = -4.0f; x <= 4.0f; x += 2.0f)
            {
                var jb = InstantiateProp("env_jersey_barrier", new Vector3(x, 0.0f, -15.5f), Quaternion.Euler(0.0f, 90.0f, 0.0f), streetGo.transform);
                if (jb != null)
                {
                    var col = jb.AddComponent<BoxCollider>();
                    col.center = new Vector3(0.0f, 0.425f, 0.0f);
                    col.size = new Vector3(0.6f, 0.85f, 2.0f);
                }
            }

            // Along Street Sidewalk Transitions
            InstantiateBarrierWithCollider(new Vector3(-5.2f, 0.0f, -11.0f), Quaternion.identity, streetGo.transform);
            InstantiateBarrierWithCollider(new Vector3(5.2f, 0.0f, -11.0f), Quaternion.identity, streetGo.transform);

            // 5. Utility Electric Poles
            InstantiateProp("env_post_electric", new Vector3(-5.8f, 0.15f, -12.0f), Quaternion.identity, streetGo.transform);
            InstantiateProp("env_post_electric", new Vector3(-5.8f, 0.15f, 10.0f), Quaternion.identity, streetGo.transform);
            InstantiateProp("env_post_electric", new Vector3(5.8f, 0.15f, -12.0f), Quaternion.Euler(0, 180, 0), streetGo.transform);
            InstantiateProp("env_post_electric", new Vector3(5.8f, 0.15f, 10.0f), Quaternion.Euler(0, 180, 0), streetGo.transform);

            // 6. Perimeter Shophouse Facades & Buildings
            // West Edge Buildings (X = -10.5)
            InstantiateProp("env_building_block_a", new Vector3(-10.5f, 0.15f, 12.0f), Quaternion.Euler(0, 90, 0), streetGo.transform);
            InstantiateProp("env_building_block_b", new Vector3(-10.5f, 0.15f, -6.0f), Quaternion.Euler(0, 90, 0), streetGo.transform);
            InstantiateProp("env_building_block_c", new Vector3(-10.5f, 0.15f, -13.0f), Quaternion.Euler(0, 90, 0), streetGo.transform);

            // East Edge Buildings (X = +10.5)
            InstantiateProp("env_building_block_d", new Vector3(10.5f, 0.15f, 12.0f), Quaternion.Euler(0, -90, 0), streetGo.transform);
            InstantiateProp("env_building_block_a", new Vector3(10.5f, 0.15f, -10.0f), Quaternion.Euler(0, -90, 0), streetGo.transform);

            // 7. Manila Street Clutter (Crates, Drums, Chairs)
            InstantiateProp("env_crate_stack", new Vector3(-6.8f, 0.15f, 1.5f), Quaternion.Euler(0, 25, 0), streetGo.transform);
            InstantiateProp("env_oil_drum", new Vector3(-6.5f, 0.15f, -0.5f), Quaternion.identity, streetGo.transform);
            InstantiateProp("env_monobloc_chair", new Vector3(6.5f, 0.15f, 1.2f), Quaternion.Euler(0, -60, 0), streetGo.transform);
        }

        private static void BuildTripHazards(Transform parent)
        {
            var hazardGroup = new GameObject("StreetTripHazards");
            hazardGroup.transform.SetParent(parent, false);

            // 1. Pisonet Extension Cord (East Sidewalk X: 5.2, Z: 2.0)
            CreateTripHazard(hazardGroup.transform, "TripHazard_PisonetCord",
                             new Vector3(5.2f, 0.15f, 2.0f), new Vector3(2.4f, 0.4f, 0.8f),
                             "CORD TRIP!", new Color(0.95f, 0.85f, 0.1f, 1.0f), 2.5f);

            // 2. Pares Broth Spill Puddle (East Sidewalk / Curb X: 5.2, Z: -6.5)
            CreateTripHazard(hazardGroup.transform, "TripHazard_ParesSpill",
                             new Vector3(5.2f, 0.15f, -6.5f), new Vector3(2.0f, 0.4f, 2.0f),
                             "NADULAS!", new Color(0.85f, 0.45f, 0.12f, 1.0f), 2.5f);

            // 3. Dropped PC Express Boxes & Packing Tape (West Sidewalk X: -5.2, Z: 7.5)
            CreateTripHazard(hazardGroup.transform, "TripHazard_GpuBoxDebris",
                             new Vector3(-5.2f, 0.15f, 7.5f), new Vector3(2.0f, 0.4f, 1.4f),
                             "BOX TRIP!", new Color(0.90f, 0.65f, 0.25f, 1.0f), 2.5f);

            // 4. Uneven Road Asphalt Pothole Trench (Central Road X: 0.0, Z: 0.0)
            CreateTripHazard(hazardGroup.transform, "TripHazard_RoadPothole",
                             new Vector3(0.0f, 0.05f, 0.0f), new Vector3(2.4f, 0.4f, 1.6f),
                             "POTHOLE!", new Color(0.45f, 0.45f, 0.50f, 1.0f), 2.5f);
        }

        private static void CreateTripHazard(Transform parent, string name, Vector3 pos, Vector3 size,
                                             string popupText, Color burstColor, float duration)
        {
            var hazardGo = new GameObject(name);
            hazardGo.transform.SetParent(parent, false);
            hazardGo.transform.localPosition = pos;

            var col = hazardGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;

            var trip = hazardGo.AddComponent<StreetTripHazard>();
            trip.TripDuration = duration;
            trip.PopupText = popupText;
            trip.BurstColor = burstColor;
            trip.HazardRadius = Mathf.Max(size.x, size.z) * 0.6f;

            // Visual marker plane on the ground
            var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "HazardVisual";
            visual.transform.SetParent(hazardGo.transform, false);
            visual.transform.localPosition = new Vector3(0.0f, 0.02f, 0.0f);
            visual.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            visual.transform.localScale = new Vector3(size.x, size.z, 1.0f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        }

        private static void InstantiateBarrierWithCollider(Vector3 pos, Quaternion rot, Transform parent)
        {
            var jb = InstantiateProp("env_jersey_barrier", pos, rot, parent);
            if (jb != null)
            {
                var col = jb.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, 0.425f, 0.0f);
                col.size = new Vector3(0.6f, 0.85f, 2.0f);
            }
        }

        private static GameObject InstantiateProp(string modelName, Vector3 position, Quaternion rotation, Transform parent)
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
