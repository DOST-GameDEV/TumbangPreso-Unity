using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Repeatable map composition, original foliage and physically honest dressing.</summary>
    public static class MapFinalPassAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/MapFinalPass";
        public static void Run()
        {
            var report=new StringBuilder();
            var maps=new[]{"Eskinita","BayanPlaza","IlalimNgTulay"};
            string selected=Environment.GetEnvironmentVariable("TUMP_MAP_AUTHOR");
            if(!string.IsNullOrEmpty(selected))
            {
                if(!maps.Contains(selected))throw new InvalidOperationException("Unknown map author target: "+selected);
                maps=new[]{selected};
            }
            foreach(string map in maps)
            {
                var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                NeighborhoodFinishAuthor.FinishLoadedScene(map,report);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                MapFinalInventory.WriteLoadedScene(map, Environment.GetEnvironmentVariable("TUMP_MAP_INVENTORY") ?? "Logs/map-spatial-current");
            }
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/map-final-author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }

        public static void FinishLoadedScene(string map,StringBuilder report)
        {
            MapPlaceAuthor.ClearPrevious(map);
            EskinitaNeighborhoodAuthor.ClearPrevious(map);
            MapPlaceAuthor.PrepareExistingPlacement(map);
            CivicTownAuthor.ClearPrevious(map);
            CivicTownAuthor.PrepareExistingPlacement(map);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var old=GameObject.Find("MapFinalPass");if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject("MapFinalPass").transform;
            var dressing=GameObject.Find(map+"/Dressing")??GameObject.Find("Dressing");
            if(dressing!=null)root.SetParent(dressing.transform,false);
            ReplaceTrees(map,root,report);ArrangeFurniture(map,report);
            ArrangeLooseDressing(map,report);SetPaintedDistance(map,report);FinishLight(map);
            if(map=="BayanPlaza"){CompleteCivicBuildings(root);PlazaPaving(root);FinishCivicUse(root,report);}
            MapPlaceAuthor.FinishLoadedScene(map,report);
            CivicTownAuthor.FinishLoadedScene(map,report);
            EskinitaNeighborhoodAuthor.FinishLoadedScene(map,report);
            // Street placement owns the poles. Rebuild their connected conductors
            // only after that placement has reached its final measured position.
            if(map=="IlalimNgTulay")UtilityConductors(root,report);
            report.AppendLine(map+": final-pass renderers="+root.GetComponentsInChildren<Renderer>().Length);
        }

        private static string Hierarchy(Transform t)
        {string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
        private static string StableHierarchy(Transform t)
        {
            string p = t.name + "[" + t.GetSiblingIndex() + "]";
            while (t.parent != null) { t = t.parent; p = t.name + "[" + t.GetSiblingIndex() + "]/" + p; }
            return p;
        }
        private static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>(true);var b=renderers[0].bounds;
            foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
        }
        private static Material Mat(string name,Color color,float smooth=.12f)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;mat.SetFloat("_Glossiness",smooth);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);return mat;
        }
        private static GameObject Block(Transform root,string name,Vector3 at,Vector3 size,Material mat,bool mounted=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
            go.transform.position=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;
            if(mounted)AirborneByDesign.Attach(go,"Architectural member attached to the existing building, not a freestanding prop.");
            return go;
        }
        private static GameObject MeshObject(Transform root,string name,List<Vector3> vertices,List<int>[] indices,Material[] materials)
        {
            var mesh=new Mesh{name=name};mesh.indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;
            mesh.SetVertices(vertices);mesh.subMeshCount=indices.Length;
            for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(mesh,path);
            else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
            var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=materials;go.isStatic=true;return go;
        }
        private static void Quad(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}

        private static void ReplaceTrees(string map,Transform root,StringBuilder report)
        {
            var sources=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>!r.transform.IsChildOf(root)).Select(r=>(renderer:r,model:Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh))))
                .Where(p=>p.model.StartsWith("tree",StringComparison.OrdinalIgnoreCase)||p.model=="env_tree"||p.model=="env_tree_far"||p.model=="env_shade_tree")
                .OrderBy(p=>Hierarchy(p.renderer.transform)).ToArray();
            int count=0,solid=0,farPlaza=0;
            foreach(var source in sources)
            {
                var r=source.renderer;r.enabled=false;
                // The first shade pass duplicated these original tree locations.
                if(source.model=="env_shade_tree")continue;
                var b=r.bounds;var at=new Vector3(b.center.x,b.min.y,b.center.z);
                if(new Vector2(at.x,at.z).magnitude>95)continue;
                float halfX=map=="Eskinita"?8.1f:map=="BayanPlaza"?12.5f:11;
                float halfZ=map=="Eskinita"?17.5f:map=="BayanPlaza"?12.5f:16.5f;
                if(Mathf.Abs(at.x)<7 && Mathf.Abs(at.z)<7)at.z=(at.z<0?-1:1)*(halfZ+2);
                bool near=map=="BayanPlaza"?Hierarchy(r.transform).Contains("/TreesNear/"):Mathf.Abs(at.x)<25 && Mathf.Abs(at.z)<28;
                if(map=="BayanPlaza" && !near && farPlaza++%2!=0)continue;
                string kind=b.size.y<3.5f?"courtyard-tree":map=="BayanPlaza"&&near?"plaza-shade":"street-broadleaf";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/urban-trees/"+kind+".glb");
                if(prefab==null)throw new InvalidOperationException("Missing authored tree: "+kind);
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab);tree.name="Broadleaf_"+count;tree.transform.SetParent(root,false);
                var raw=BoundsOf(tree);float height=Mathf.Clamp(b.size.y,1.8f,near?7.8f:8.6f);
                float scale=height/Mathf.Max(.1f,raw.size.y);tree.transform.localScale=Vector3.one*scale;
                tree.transform.rotation=Quaternion.Euler(0,(count*67)%360,0);
                // Scene ground is flat here; retain authored scenery levels outside
                // the court, but seat reachable trunks on the actual physical floor.
                bool reachable=Mathf.Abs(at.x)<halfX-.5f && Mathf.Abs(at.z)<halfZ-.5f;
                if(reachable)at.y=VfxShapes.GroundPoint(at+Vector3.up*1.5f).y;
                tree.transform.position=at-Vector3.up*raw.min.y*scale;tree.isStatic=true;
                foreach(var foliage in tree.GetComponentsInChildren<MeshRenderer>())
                {
                    foliage.sharedMaterials=foliage.sharedMaterials.Select(m=>m.name.Contains("bark")?
                        Mat("tree_bark",new Color(.34f,.25f,.16f)):m.name.Contains("new growth")?
                        Mat("tree_new_growth",new Color(.34f,.46f,.23f)):m.name.Contains("shaded foliage")?
                        Mat("tree_shaded_foliage",new Color(.23f,.34f,.18f)):
                        Mat("tree_foliage",new Color(.29f,.41f,.21f))).ToArray();
                }
                if(reachable)
                {
                    var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.33f;trunk.height=3.1f;trunk.center=new Vector3(0,1.55f,0);solid++;
                }
                else AirborneByDesign.Attach(tree,"Peripheral vegetation rooted on the existing scenery plate outside reachable play; original terrain elevation retained.");
                count++;
            }
            report.AppendLine(map+": replaced "+count+" cone/conifer trees; "+solid+" reachable trunks have collision.");
        }

        private static Transform FurnitureRoot(Transform t)
        {
            for(var p=t;p!=null;p=p.parent)
                if(p.name.StartsWith("Kalat_")||p.name.StartsWith("Clutter_")||p.name.StartsWith("Bench_")||p.name.StartsWith("Stool_"))return p;
            return PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject)?.transform??t;
        }
        private static void Place(Transform t,Vector3 at,float yaw)
        {t.gameObject.SetActive(true);t.position=at;t.rotation=Quaternion.Euler(0,yaw,0);}
        private static void ArrangeFurniture(string map,StringBuilder report)
        {
            if(map=="IlalimNgTulay")return; // Its kiosks/carts already have real solid footprints.
            var candidates=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>Hierarchy(r.transform).StartsWith(map+"/Dressing/"))
                .Select(r=>(root:FurnitureRoot(r.transform),file:AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh)))
                .Where(p=>p.file.Contains("stall-bench")||p.file.Contains("stall-stool")||p.file.Contains("monobloc_chair")||p.file.Contains("crate_stack"))
                .GroupBy(p=>p.root).Select(g=>g.First()).OrderBy(p=>p.root.name).ToArray();
            int benches=0,seats=0,crates=0,moved=0;
            foreach(var p in candidates)
            {
                bool bench=p.file.Contains("bench"),crate=p.file.Contains("crate");int n=bench?benches++:crate?crates++:seats++;
                int cap=map=="BayanPlaza"?(bench?6:crate?2:6):(bench?2:crate?4:6);
                if(n>=cap){p.root.gameObject.SetActive(false);continue;}
                Vector3 at;float yaw;
                if(map=="BayanPlaza")
                {
                    float side=n%2==0?-1:1;
                    at=bench?new Vector3(side*14.0f,.1f,new[]{7.4f,4.0f,-6.0f}[n/2]):
                        new Vector3(side*(crate?17.2f:13.4f),.1f,(side<0?10.4f:-10.4f)+(crate?0:(n/2-1)*.8f));
                    // The bench's long axis is local Z, as with the garden hedge.
                    // People sit facing the court instead of along its boundary.
                    yaw=bench?(side<0?0:180):(side<0?90:270);
                }
                else
                {
                    float end=n%2==0?1:-1;
                    // The shop's drawn front ends at x=+/-5.95. Keep a real
                    // customer aisle, seats beneath shade and separate crate bays.
                    // The earlier four-crate recipe placed two stacks on each other.
                    at=new Vector3(-end*(bench?3.9f:crate?8.6f:2.95f+(n/2)*.8f),.1f,
                        end*(bench?19.6f:crate?20.4f+(n/2)*1.3f:20.65f));
                    yaw=bench?(end>0?90:270):(end>0?180:0);
                }
                PlaceByDrawnBase(p.root,at,yaw);moved++;
            }
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(map=="BayanPlaza" && t.name.StartsWith("Stall_"))t.position=new Vector3(Mathf.Sign(t.position.x)*15.2f,.1f,t.position.z);
                if(map=="Eskinita" && (t.name=="SariSari_W"||t.name=="SariSari_E"))
                    t.position=new Vector3(t.name=="SariSari_W"?-7.15f:7.15f,.1f,t.name=="SariSari_W"?20.0f:-20.0f);
            }
            report.AppendLine(map+": grouped "+moved+" non-solid furniture pieces beyond the playable walls; excess seats retained inactive.");
        }

        private static void FinishLight(string map)
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientIntensity=1;
            RenderSettings.ambientSkyColor=new Color(.64f,.69f,.72f);
            RenderSettings.ambientEquatorColor=map=="IlalimNgTulay"?new Color(.49f,.52f,.52f):new Color(.48f,.47f,.42f);
            RenderSettings.ambientGroundColor=new Color(.32f,.29f,.24f);
            foreach(var sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(sun.type==LightType.Directional){sun.color=new Color(1,.95f,.86f);sun.intensity=map=="BayanPlaza"?1.03f:1.0f;}
            Object.FindFirstObjectByType<MapGrade>()?.Set(1,1.02f,1,1,1.9f);
        }

        private static void PlaceByDrawnBase(Transform root, Vector3 at, float yaw)
        {
            var bounds = DrawnBoundsAtOrigin(root, yaw);
            root.position = at - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.gameObject);
        }

        private static Bounds DrawnBoundsAtOrigin(Transform root, float yaw)
        {
            // Derive the offset from a fixed origin, not the previous placement.
            // Repeated subtraction of a large world coordinate accumulated tiny
            // vertical drift on every author run. This is absolute construction.
            root.gameObject.SetActive(true); root.position = Vector3.zero;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            return BoundsOf(root.gameObject);
        }

        private static void RetainInactive(Transform root)
        {
            root.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.gameObject);
        }

        private static void ArrangeLooseDressing(string map, StringBuilder report)
        {
            // The actual 95-degree owner views showed pots/fences intersecting
            // legal retrieval routes even after the seating pass. These selected
            // objects have no collision or interaction. Keep rich roadside groups
            // beyond the physical boundary instead of invisible walk-through props.
            // Include inactive candidates so every author run makes the same choice.
            var candidates = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => Hierarchy(r.transform).StartsWith(map + "/Dressing/", StringComparison.Ordinal))
                .Select(r => (root: FurnitureRoot(r.transform), model: Path.GetFileNameWithoutExtension(
                    AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh))))
                .Where(p => p.model == "env_halaman_lata" || p.model == "env_tire" || p.model == "env_oil_drum" ||
                    p.model == "env_bollard" || p.model == "env_wall_corrugated" || p.model == "plant" ||
                    p.model == "hedge" || p.model == "fence" || p.model == "fence-broken" ||
                    (map == "IlalimNgTulay" && (p.model == "env_monobloc_chair" || p.model == "env_crate_stack")))
                .GroupBy(p => p.root).Select(g => g.First())
                // Ilalim contains several siblings named env_bollard/env_tire.
                // Name-only ties inherited enumeration order and swapped their
                // placements after each save/reopen. Sibling identity is stable.
                .OrderBy(p => StableHierarchy(p.root), StringComparer.Ordinal).ToArray();
            var count = new Dictionary<string, int>();
            int moved = 0, retained = 0;
            foreach (var item in candidates)
            {
                // A real kiosk, hazard or other obstacle retains its existing
                // physical role. This pass relocates only deliberately non-solid art.
                if (item.root.GetComponentsInChildren<Collider>(true).Length != 0) continue;
                string path = Hierarchy(item.root);
                bool hedge = item.model == "hedge";
                bool fence = item.model == "fence" || item.model == "fence-broken";
                if (hedge && !(item.root.name.StartsWith("EdgeHedge_") || item.root.name.StartsWith("RimHedge_") ||
                    path.Contains("/Clutter/") || path.Contains("/Kalat/"))) continue;
                string kind = hedge ? "hedge" : fence ? "fence" : item.model;
                int n = count.TryGetValue(kind, out int previous) ? previous : 0;
                count[kind] = n + 1;
                int cap = hedge ? (map == "BayanPlaza" ? 8 : 2) : fence ? 4 :
                    item.model == "env_halaman_lata" ? 6 : item.model == "plant" ? 4 :
                    item.model == "env_bollard" ? 4 : item.model == "env_wall_corrugated" ? 2 :
                    item.model == "env_oil_drum" ? 2 : 4;
                if (n >= cap) { RetainInactive(item.root); retained++; continue; }
                float side = n % 2 == 0 ? -1 : 1;
                Vector3 at;
                float yaw = side < 0 ? 90 : 270;
                if (map == "BayanPlaza")
                {
                    if (hedge)
                    {
                        // This source hedge is long along local Z. A quarter
                        // turn made fingers pointing into the benches instead of
                        // a garden border parallel to the side of the court.
                        yaw = side < 0 ? 0 : 180;
                        var bounds = DrawnBoundsAtOrigin(item.root, yaw);
                        // Leave the end stalls' customer space free of hedges.
                        at = new Vector3(side * (12.5f + bounds.extents.x + .3f), .1f, -5.4f + (n / 2) * 3.6f);
                    }
                    else at = new Vector3(side * 16.8f, .1f, (n / 2 == 0 ? -8.8f : 8.8f));
                }
                else if (map == "Eskinita")
                {
                    float end = -side;
                    if (item.model == "env_halaman_lata")
                        at = new Vector3(side * 1.65f, .1f, end * (18.7f + (n / 2) * .8f));
                    else if (item.model == "plant")
                        at = new Vector3(side * (1.5f + (n / 2) * 1.2f), .1f, end * 22.1f);
                    else if (item.model == "env_tire")
                        at = new Vector3(side * 9.7f, .1f, end * (19 + (n / 2) * 1.0f));
                    else if (item.model == "env_oil_drum")
                        at = new Vector3(side * 9.7f, .1f, end * 21.1f);
                    else if (item.model == "env_bollard")
                        at = new Vector3(side * (.8f + (n / 2) * 7.5f), .1f, end * 18.2f);
                    else if (item.model == "env_wall_corrugated")
                        at = new Vector3(side * 9.4f, .1f, end * 22.0f);
                    else at = new Vector3(side * 9.4f, .1f, end * (14.7f + n / 2));
                }
                else
                {
                    // Leave all interactive shops, carts and their colliders in
                    // place. Small loose art belongs at the two pavement ends.
                    float end = side;
                    float x = item.model == "env_halaman_lata" ? 8.0f :
                        item.model == "env_monobloc_chair" ? 8.8f : 10.0f;
                    float z = item.model == "env_tire" ? 18.0f :
                        item.model == "env_oil_drum" ? 20.5f : 19.0f;
                    at = new Vector3(side * x, .21f, end * (z + (n / 2) * .95f));
                }
                PlaceByDrawnBase(item.root, at, yaw); moved++;
            }
            report.AppendLine(map + ": grouped " + moved + " non-solid plants/tyres/edge pieces; " + retained + " excess pieces retained inactive.");
        }

        private static void SetPaintedDistance(string map, StringBuilder report)
        {
            if (map != "Eskinita") return;
            int changed = 0;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!Hierarchy(renderer.transform).StartsWith("Eskinita/", StringComparison.Ordinal)) continue;
                if (!renderer.sharedMaterials.Any(m => m != null && m.name == "Mountain")) continue;
                var t = renderer.transform;
                Vector3 at;
                float width;
                // The painting's top22% is transparent. Keep its actual peaks
                // above the roofline without returning to the old looming cutouts.
                if (t.name == "MountainBackdrop") { at = new Vector3(0, 28, 180); width = 180; }
                else if (t.name == "Quad") { at = new Vector3(155, 34, 25); width = 140; }
                else if (t.name == "Quad (1)") { at = new Vector3(-145, 34, -65); width = 140; }
                else continue;
                var texture = renderer.sharedMaterial.mainTexture;
                float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 1.926f;
                // Unity's quad front is local -Z. Pointing +Z toward the court
                // culled the painting entirely in the first placement review.
                t.position = at; t.rotation = Quaternion.LookRotation(new Vector3(at.x, 0, at.z));
                t.localScale = new Vector3(width / t.parent.lossyScale.x, width / aspect / t.parent.lossyScale.y, 1);
                // Preserve the supplied painting and material. Only its distance,
                // apparent scale and original aspect change. A distant cutout has
                // no physical surface for throws and must not cast a giant shadow.
                foreach (var collider in t.GetComponents<Collider>()) Object.DestroyImmediate(collider);
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                AirborneByDesign.Attach(t.gameObject, "Painted distant landscape behind the neighborhood; no reachable collision.");
                changed++;
            }
            report.AppendLine("Eskinita: staged " + changed + " retained mountain paintings as distant landscape without altering their artwork.");
        }

        private static void FinishCivicUse(Transform root, StringBuilder report)
        {
            // The original square mixed ghost railings, random boulders and parked
            // tricycles into reachable court space. Give those edges actual uses:
            // an open civic forecourt, aligned hoops and a southern tricycle bay.
            var sceneRoots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in sceneRoots)
            {
                string p = Hierarchy(t);
                if (p.StartsWith("BayanPlaza/Dressing/Monument/Rail_", StringComparison.Ordinal) ||
                    p == "BayanPlaza/Dressing/Monument/MonLantern_1" ||
                    p == "BayanPlaza/Dressing/Landmarks/Flagpole" ||
                    p.StartsWith("BayanPlaza/Dressing/Ground/Rock_", StringComparison.Ordinal))
                    RetainInactive(t);
            }

            int lamp = 0, planter = 0;
            foreach (var t in sceneRoots.OrderBy(StableHierarchy, StringComparer.Ordinal))
            {
                string p = Hierarchy(t);
                if (p.StartsWith("BayanPlaza/Dressing/Landmarks/Lantern_", StringComparison.Ordinal) &&
                    t.parent != null && t.parent.name == "Landmarks")
                {
                    float x = lamp % 2 == 0 ? -13.3f : 13.3f, z = lamp / 2 == 0 ? 13.3f : -13.3f;
                    if (lamp++ >= 4) { RetainInactive(t); continue; }
                    PlaceByDrawnBase(t, new Vector3(x,.1f,z), t.eulerAngles.y);
                }
                if (t.name == "RingNorth" || t.name == "RingSouth" ||
                    t.name == "CourtHoopEast" || t.name == "CourtHoopWest")
                {
                    bool east = t.name == "RingNorth" || t.name == "CourtHoopEast";
                    t.name = east ? "CourtHoopEast" : "CourtHoopWest";
                    // The source hoop projects toward local -Z; the previous
                    // north/south pair faced away and blocked the civic frontage.
                    // Its narrow pole fits between the side garden beds.
                    PlaceByDrawnBase(t, new Vector3(east?13.05f:-13.05f,.1f,0), east?90:270);
                    Block(root, east?"EastHoopBracket":"WestHoopBracket",
                        t.TransformPoint(new Vector3(0,3.07f,.36f)),new Vector3(.30f,.045f,.065f),
                        Mat("hoop_bracket",new Color(.51f,.52f,.48f)),true);
                }
                if (t.parent != null && t.parent.name == "Monument" &&
                    (t.name.StartsWith("RimHedge_") || t.name.StartsWith("MonHedge_")))
                {
                    if (planter >= 4) { RetainInactive(t); continue; }
                    PlaceByDrawnBase(t, new Vector3(1.9f + planter++ * 3.2f,.1f,-13.5f), 0);
                }
                if (p.StartsWith("BayanPlaza/Dressing/Vehicles/Tricycle_", StringComparison.Ordinal) &&
                    t.parent != null && t.parent.name == "Vehicles")
                {
                    RetainInactive(t);
                }
            }
            foreach (Transform tree in root)
            {
                if (!tree.name.StartsWith("Broadleaf_")) continue;
                var at = tree.position;
                // Shade can overhang a parked vehicle; its trunk cannot stand
                // inside it. Keep trees behind the waiting/parking strip.
                if (at.x >= -12 && at.x <= 2 && at.z >= -18 && at.z <= -14)
                {
                    at.z = -19.8f; tree.position = at;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tree);
                }
            }
            var stone = Mat("terminal_paving",new Color(.47f,.46f,.41f));
            var paint = Mat("terminal_bay_marks",new Color(.76f,.74f,.66f));
            var wood = Mat("terminal_shade_timber",new Color(.36f,.25f,.16f));
            var roofMat = Mat("terminal_shade_roof",new Color(.43f,.45f,.40f));
            Block(root,"TricycleWaitingApron",new Vector3(-5.3f,.105f,-15.8f),new Vector3(14.2f,.012f,3.5f),stone);
            foreach(float x in new[]{-11.7f,-8.25f,-4.75f,-1.25f})
                Block(root,"TricycleBayMark",new Vector3(x,.116f,-15.8f),new Vector3(.045f,.012f,2.3f),paint);
            var tricycle=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/terminal/bayan-passenger-tricycle.glb");
            if(tricycle==null)throw new InvalidOperationException("Missing original terminal tricycle source.");
            for(int i=0;i<3;i++)
            {
                var parked=(GameObject)PrefabUtility.InstantiatePrefab(tricycle);
                parked.name="TerminalTricycle_"+i;parked.transform.SetParent(root,false);
                PlaceByDrawnBase(parked.transform,new Vector3(-10+i*3.5f,.1f,-15.8f),0);
            }
            foreach(float x in new[]{-.8f,1.8f})foreach(float z in new[]{-17.2f,-14.4f})
            {
                Block(root,"WaitingShadePost",new Vector3(x,1.45f,z),new Vector3(.22f,2.7f,.22f),wood);
                Block(root,"WaitingShadePostFoot",new Vector3(x,.18f,z),new Vector3(.36f,.16f,.36f),stone);
            }
            foreach(float z in new[]{-17.2f,-14.4f})
                Block(root,"WaitingShadeBeam",new Vector3(.5f,2.73f,z),new Vector3(2.82f,.18f,.24f),wood,true);
            Block(root,"WaitingShadeRoof",new Vector3(.5f,2.89f,-15.8f),new Vector3(3.0f,.18f,3.25f),roofMat,true);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/town/stall-bench.glb");
            if(model==null)throw new InvalidOperationException("Missing retained terminal bench source.");
            var bench=(GameObject)PrefabUtility.InstantiatePrefab(model);bench.name="TerminalWaitingBench";
            bench.transform.SetParent(root,false);bench.transform.localScale=Vector3.one*2.6f;
            PlaceByDrawnBase(bench.transform,new Vector3(.5f,.1f,-15.8f),180);
            report.AppendLine("Bayan: inward-facing east/west hoops; 4 perimeter lanterns; open monument approach; 3 original passenger tricycles in a marked roadside bay with shaded waiting, clear of tree trunks.");
        }

        private static void CompleteCivicBuildings(Transform root)
        {
            var plaster=Mat("civic_lime_plaster",new Color(.72f,.68f,.57f));var roof=Mat("civic_roof_clay",new Color(.48f,.24f,.15f));
            var timber=Mat("civic_timber",new Color(.30f,.23f,.16f));
            Block(root,"ChurchNave",new Vector3(-4.2f,3.0f,20.2f),new Vector3(5.6f,5.8f,10.0f),plaster);
            Roof(root,"ChurchNaveRoof",-4.2f,15.1f,25.4f,6.0f,5.9f,8.05f,roof);
            foreach(float x in new[]{-7.15f,-1.25f})foreach(float z in new[]{17.0f,20.5f,24.0f})
            {
                Block(root,"NaveButtress",new Vector3(x,2.25f,z),new Vector3(.34f,4.3f,.52f),plaster);
                Block(root,"NaveSideVent",new Vector3(x,4.7f,z),new Vector3(.04f,1.05f,.60f),timber,true);
            }
            // The retained hall already has depth, but its roof ends were open.
            var v=new List<Vector3>();var ix=new List<int>();
            foreach(float x in new[]{1.6f,14.0f})
            {
                int n=v.Count;v.AddRange(new[]{new Vector3(x,6.97f,13.6f),new Vector3(x,8.65f,16.6f),new Vector3(x,6.97f,19.6f)});
                ix.AddRange(x<7?new[]{n,n+1,n+2}:new[]{n,n+2,n+1});
            }
            var ends=MeshObject(root,"HallRoofEndWalls",v,new[]{ix},new[]{plaster});AirborneByDesign.Attach(ends,"Gable end walls close the retained municipal hall roof.");
        }
        private static void Roof(Transform root,string name,float x,float front,float back,float width,float eave,float peak,Material mat)
        {
            var p=new[]{new Vector3(x-width/2,eave,front),new Vector3(x+width/2,eave,front),new Vector3(x,peak,front),new Vector3(x-width/2,eave,back),new Vector3(x+width/2,eave,back),new Vector3(x,peak,back)};
            int[] triangles={0,2,1,3,4,5,0,3,5,0,5,2,2,5,4,2,4,1,0,1,4,0,4,3};
            var vertices=triangles.Select(i=>p[i]).ToList();var ids=Enumerable.Range(0,vertices.Count).ToList();
            var go=MeshObject(root,name,vertices,new[]{ids},new[]{mat});AirborneByDesign.Attach(go,"Pitched roof seated on the authored nave walls.");
        }
        private static void PlazaPaving(Transform root)
        {
            var vertices=new List<Vector3>();var indices=new[]{new List<int>(),new List<int>()};
            for(int x=-8;x<8;x++)for(int z=-8;z<8;z++)
            {
                float a=x*1.5f,b=z*1.5f;float y=.101f;
                Quad(vertices,indices[(Math.Abs(x*13+z*7)%7)==0?1:0],new Vector3(a,y,b),new Vector3(a,y,b+1.5f),new Vector3(a+1.5f,y,b+1.5f),new Vector3(a+1.5f,y,b));
            }
            MeshObject(root,"PlazaStonePaving",vertices,indices,new[]{Mat("plaza_stone",new Color(.52f,.50f,.45f)),Mat("plaza_stone_variation",new Color(.55f,.53f,.48f))});
        }

        private static void UtilityConductors(Transform root,StringBuilder report)
        {
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(r.name.StartsWith("SidewalkWire_"))r.enabled=false;
            var vertices=new List<Vector3>();var indices=new List<int>();int spans=0;
            foreach(string side in new[]{"W","E"})
            {
                var poles=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("SidewalkPole_"+side+"_")).OrderBy(r=>r.bounds.center.z).ToArray();
                for(int p=0;p+1<poles.Length;p++)foreach(float offset in new[]{-.58f,0,.58f})
                {
                    var a=new Vector3(poles[p].bounds.center.x+offset,poles[p].bounds.max.y-.38f,poles[p].bounds.center.z);
                    var b=new Vector3(poles[p+1].bounds.center.x+offset,poles[p+1].bounds.max.y-.38f,poles[p+1].bounds.center.z);
                    int start=vertices.Count;
                    for(int s=0;s<=18;s++)
                    {
                        float t=s/18f;var at=Vector3.Lerp(a,b,t)-Vector3.up*(4*t*(1-t)*.42f);
                        for(int k=0;k<5;k++){float angle=k*Mathf.PI*2/5;vertices.Add(at+new Vector3(Mathf.Cos(angle)*.022f,Mathf.Sin(angle)*.022f,0));}
                    }
                    for(int s=0;s<18;s++)for(int k=0;k<5;k++){int n=start+s*5+k,j=start+s*5+(k+1)%5;indices.AddRange(new[]{n,j,j+5,n,j+5,n+5});}
                    spans++;
                }
            }
            var cable=MeshObject(root,"IlalimUtilityConductors",vertices,new[]{indices},new[]{Mat("utility_cable",new Color(.075f,.078f,.075f),.08f)});
            AirborneByDesign.Attach(cable,"Tensioned utility conductors join the existing pole crossarms above pedestrian height.");
            report.AppendLine("Ilalim: "+spans+" connected conductor spans,44mm visual diameter, no oversized tube bundles.");
        }
        private static void ShelteredShopfronts(Transform root)
        {
            var steel=Mat("shop_canopy_steel",new Color(.34f,.38f,.35f));var timber=Mat("shop_canopy_bracket",new Color(.28f,.25f,.20f));
            var stores=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="PC_Express_Store"||t.name.StartsWith("Pisonet_Kiosk_")).ToArray();
            foreach(var store in stores)
            {
                var b=BoundsOf(store.gameObject);float side=Mathf.Sign(b.center.x);float edge=side<0?b.max.x:b.min.x;float z=b.center.z;
                float width=Mathf.Min(3.4f,b.size.z*.8f);
                Block(root,"ShelteredShopCanopy",new Vector3(edge-side*.36f,2.95f,z),new Vector3(.95f,.10f,width),steel,true);
                foreach(float dz in new[]{-.42f,.42f})Block(root,"CanopyWallBracket",new Vector3(edge-side*.18f,2.73f,z+width*dz),new Vector3(.5f,.36f,.065f),timber,true);
            }
        }
        private static void NeighborhoodPockets(Transform root)
        {
            var timber=Mat("neighborhood_bench_wood",new Color(.38f,.25f,.14f));
            var metal=Mat("neighborhood_canopy",new Color(.44f,.44f,.38f));
            foreach(float end in new[]{-1f,1f})
            {
                float x=-end*4.1f,z=end*19.7f;
                foreach(float dx in new[]{-1.7f,1.7f})Block(root,"NeighborhoodShadePost",new Vector3(x+dx,1.45f,z),new Vector3(.10f,2.7f,.10f),timber);
                Block(root,"NeighborhoodShadeRoof",new Vector3(x,2.86f,z+.45f*end),new Vector3(3.7f,.10f,1.55f),metal,true);
            }
        }
    }
}
