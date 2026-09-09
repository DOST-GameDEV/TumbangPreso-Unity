using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // A repeatable edit of the existing maps. Gameplay geometry and imported livery
    // retain their own owners; this author owns facade meshes and peripheral detail.
    public static class NeighborhoodFinishAuthor
    {
        private const string Folder = "Assets/TumbangPreso/Art/NeighborhoodFinish";
        private static readonly string[] Models = { "env_church_facade", "env_bell_tower", "env_municipal_hall", "env_sari_sari_store", "env_shade_tree" };
        private static readonly Dictionary<string, Material[]> Surfaces = new Dictionary<string, Material[]>();

        public static void Run()
        {
            Apply();
            EditorApplication.Exit(0);
        }

        public static void ReviewCast()
        {
            RosterBookBuilder.BuildFromMenu();
            HeroTurnaroundProbe.Execute();
            string output = Environment.GetEnvironmentVariable("TUMP_CAST_REVIEW") ?? "Logs/cast-finish-v1";
            Directory.CreateDirectory(output);
            var shoot = typeof(HeroTurnaroundProbe).GetMethod("ShootHeroTurnaround",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            foreach (var entry in RosterBook.Load().People.Where(p => p != null && p.Model != null))
            {
                string path = AssetDatabase.GetAssetPath(entry.Model);
                shoot.Invoke(null, new object[] { entry.Id, entry.Id, path,
                    output + "/" + entry.Id + "-turnaround-v1.png", new StringBuilder() });
            }
            ModelSheet.RunCast();
        }

        public static void Apply()
        {
            PrepareMaterials();
            var report = new StringBuilder();
            foreach (string map in new[] { "Eskinita", "BayanPlaza", "IlalimNgTulay" })
            {
                string path = "Assets/TumbangPreso/Scenes/Maps/" + map + ".unity";
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                report.AppendLine(map + " before ambient=" + RenderSettings.ambientLight + " fog=" + RenderSettings.fogColor);
                FinishLoadedScene(map,report);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/neighborhood-finish.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void PrepareMaterials()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            foreach (string model in Models)
            {
                string path = "Assets/TumbangPreso/Art/models/" + model;
                AssetDatabase.ImportAsset(path + ".obj", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var mats = new List<Material>();
                string name = null;
                foreach (string line in File.ReadAllLines(path + ".mtl"))
                {
                    if (line.StartsWith("newmtl ")) name = line.Substring(7).Trim();
                    if (!line.StartsWith("Kd ")) continue;
                    var rgb = line.Split(' ').Skip(1).Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
                    mats.Add(Material(model + "_" + name, new Color(rgb[0], rgb[1], rgb[2])));
                }
                Surfaces[model] = mats.ToArray();
            }
        }

        public static void FinishLoadedScene(string map, StringBuilder report = null)
        {
            if (Surfaces.Count == 0) PrepareMaterials();
            report ??= new StringBuilder();
            var previous = GameObject.Find("NeighborhoodFinish");
            if (previous != null) Object.DestroyImmediate(previous);
            var group = new GameObject("NeighborhoodFinish").transform;
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                string original = AssetDatabase.GetAssetPath(filter.sharedMesh);
                string model = Path.GetFileNameWithoutExtension(original);
                if (!Surfaces.TryGetValue(model, out var mats)) continue;
                var mesh = AssetDatabase.LoadAllAssetsAtPath(original).OfType<Mesh>().First();
                filter.sharedMesh = mesh;
                filter.GetComponent<MeshRenderer>().sharedMaterials = mats;
                report.AppendLine("  authored " + filter.name + " mesh=" + mesh.name + " triangles=" + mesh.triangles.Length / 3);
            }
            SetLight(map);
            if (map == "Eskinita") Frontages(group);
            if (map == "BayanPlaza") Plaza(group);
            if (map == "IlalimNgTulay") Guideway(group);
            report.AppendLine("  finish renderers=" + group.GetComponentsInChildren<Renderer>().Length);
            ShadeTrees(map,group);
        }

        private static void SetLight(string map)
        {
            bool bridge = map == "IlalimNgTulay";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = bridge ? new Color(.62f,.65f,.62f) : new Color(.62f,.64f,.59f);
            RenderSettings.ambientEquatorColor = bridge ? new Color(.47f,.49f,.46f) : new Color(.44f,.42f,.36f);
            RenderSettings.ambientGroundColor = new Color(.25f,.23f,.20f);
            RenderSettings.ambientIntensity = 1f;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(1f,.91f,.78f);
                light.intensity = bridge ? 1.08f : 1.12f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = bridge ? .75f : .86f;
                light.shadowBias = .035f;
                light.shadowNormalBias = .25f;
            }
            var grade = Object.FindFirstObjectByType<MapGrade>();
            if (grade != null) grade.Set(1f, 1.03f, map == "Eskinita" ? 1.06f : 1.02f, 1f, 1.9f);
        }

        private static Material Material(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            mat.SetFloat("_Glossiness", .18f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Block(Transform root, string name, Vector3 at, Vector3 size, Material material, bool mounted = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.position = at;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            if (mounted) AirborneByDesign.Attach(go, "Architectural trim fixed to the existing building or viaduct, not a standing prop.");
            return go;
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void Frontages(Transform root)
        {
            var plaster = Material("weathered_plaster", new Color(.64f,.61f,.51f));
            var metal = Material("painted_steel", new Color(.23f,.29f,.24f));
            var wood = Material("aged_timber", new Color(.37f,.24f,.14f));
            var roof = Material("galvanized_roof", new Color(.44f,.47f,.43f));
            var houses = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("Bahay_") && Mathf.Abs(t.position.z) < 18).OrderBy(t => t.name).ToArray();
            int index = 0;
            foreach (var house in houses)
            {
                Bounds bounds = BoundsOf(house);
                float side = Mathf.Sign(bounds.center.x);
                float x = side < 0 ? bounds.max.x : bounds.min.x;
                float z = bounds.center.z;
                float y = Mathf.Clamp(bounds.max.y * .5f, 2.8f, 4.3f);
                float width = Mathf.Min(3.3f,bounds.size.z * .65f);
                // Upper storey additions never introduce an obstacle on a retrieval route.
                var balcony = new GameObject("Frontage_" + house.name).transform;
                balcony.SetParent(root);
                if (index++ % 2 == 0)
                {
                    Block(balcony,"BalconySlab",new Vector3(x-side*.4f,y,z),new Vector3(.85f,.15f,width),plaster,true);
                    Block(balcony,"BalconyRail",new Vector3(x-side*.78f,y+.87f,z),new Vector3(.055f,.07f,width),metal,true);
                    for (float p=-width*.47f;p<=width*.47f;p+=.26f)
                        Block(balcony,"Baluster",new Vector3(x-side*.78f,y+.48f,z+p),new Vector3(.045f,.8f,.045f),metal,true);
                    foreach(float p in new[]{-width*.48f,width*.48f})
                        Block(balcony,"ReturnRail",new Vector3(x-side*.4f,y+.87f,z+p),new Vector3(.8f,.07f,.055f),metal,true);
                }
                else
                {
                    for(int p=0;p<12;p++)
                        Block(balcony,"WindowLouver",new Vector3(x-side*.07f,y+p*.095f,z),new Vector3(.16f,.05f,1.1f),wood,true);
                }
                Block(balcony,"DoorEave",new Vector3(x-side*.40f,2.45f,z+.65f),new Vector3(.88f,.09f,1.65f),roof,true);
                Block(balcony,"Downpipe",new Vector3(x-side*.08f,(y+1.25f)*.5f,z-width*.52f),new Vector3(.075f,y+1.05f,.075f),metal,true);
                for(int p=0;p<3;p++)
                    Block(balcony,"FoundationCourse",new Vector3(x-side*.035f,.18f+p*.16f,z),new Vector3(.075f,.13f,width),plaster,true);
            }
        }

        private static void Plaza(Transform root)
        {
            var stone = Material("plaza_limestone",new Color(.49f,.47f,.40f));
            var dark = Material("plaza_joint",new Color(.31f,.32f,.28f));
            // A perimeter paving rhythm frames the open court without marking false rules.
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=-10;i<=10;i++)
                {
                    float top=GroundHeight(side*10.6f,i*.9f);
                    Block(root,"PlazaPerimeterPaver",new Vector3(side*10.6f,top+.006f,i*.9f),new Vector3(1.0f,.014f,.84f),stone);
                }
                Block(root,"PlazaPerimeterJoint",new Vector3(side*10.05f,.11f,0),new Vector3(.045f,.018f,18.9f),dark,true);
            }
        }

        private static void Guideway(Transform root)
        {
            var concrete = Material("bridge_cast_concrete",new Color(.43f,.45f,.40f));
            var dark = Material("bridge_bearing",new Color(.18f,.20f,.18f));
            var pipe = Material("bridge_drain",new Color(.39f,.38f,.32f));
            foreach(float z in new[]{-19f,-10f,10f,19f})
            {
                Block(root,"TransversePierCap",new Vector3(0,7.69f,z),new Vector3(10.2f,.58f,1.45f),concrete,true);
                foreach(float side in new[]{-1f,1f})
                {
                    Block(root,"BearingPad",new Vector3(side*4.45f,7.97f,z),new Vector3(1.12f,.09f,1.23f),dark,true);
                    Block(root,"CapitalStep",new Vector3(side*4.45f,7.27f,z),new Vector3(2.1f,.26f,1.35f),concrete,true);
                    Block(root,"DeckDrain",new Vector3(side*5.15f,7.40f,z+1.18f),new Vector3(.11f,.98f,.11f),pipe,true);
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                Block(root,"SoffitRib",new Vector3(side*2.4f,7.85f,0),new Vector3(.28f,.26f,48f),concrete,true);
                for(float z=-22;z<24;z+=4)
                    Block(root,"DeckConstructionJoint",new Vector3(0,7.989f,z),new Vector3(10.1f,.018f,.04f),dark,true);
            }
        }

        private static float GroundHeight(float x,float z)
        {
            Physics.SyncTransforms();
            var ground=Physics.RaycastAll(new Vector3(x,3,z),Vector3.down,4)
                .Where(h => h.normal.y>.9f && h.point.y<.4f).Select(h=>h.point.y).ToArray();
            return ground.Length>0 ? ground.Max() : .1f;
        }

        private static void ShadeTrees(string map,Transform root)
        {
            var candidates=new List<GameObject>();
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var filter=renderer.GetComponent<MeshFilter>();
                if(filter==null || !AssetDatabase.GetAssetPath(filter.sharedMesh).ToLowerInvariant().Contains("tree"))continue;
                var original=PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject) ?? renderer.gameObject;
                if(candidates.Contains(original))continue;
                var bounds=BoundsOf(original.transform);
                if(bounds.size.y<3.5f || bounds.center.magnitude>38f)continue;
                if(Mathf.Abs(bounds.center.x)<10 && Mathf.Abs(bounds.center.z)<13)continue;
                candidates.Add(original);
            }
            var targets=map=="BayanPlaza" ? new[]{new Vector3(-14,0,-13),new Vector3(14,0,-13),new Vector3(-14,0,13),new Vector3(14,0,13)}
                : map=="Eskinita" ? new[]{new Vector3(-14,0,0),new Vector3(14,0,8)}
                : new[]{new Vector3(-17,0,14)};
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/env_shade_tree.obj");
            int index=0;
            foreach(var target in targets)
            {
                if(candidates.Count==0)break;
                var original=candidates.OrderBy(c=>Vector3.SqrMagnitude(BoundsOf(c.transform).center-target)).First();
                candidates.Remove(original);
                var bounds=BoundsOf(original.transform);
                foreach(var renderer in original.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                tree.name="ShadeTree_"+map+"_"+index;
                tree.transform.SetParent(root,false);
                tree.transform.position=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                tree.transform.rotation=Quaternion.Euler(0,index++*67f,0);
                tree.transform.localScale=Vector3.one*Mathf.Clamp(bounds.size.y/7.55f,.8f,1.05f);
                foreach(var renderer in tree.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Surfaces["env_shade_tree"];
                tree.isStatic=true;
                AirborneByDesign.Attach(tree,"Background shade tree rooted at the replaced vegetation's measured ground; outside the clear court, on the existing scenery plate.");
            }
        }
    }
}
