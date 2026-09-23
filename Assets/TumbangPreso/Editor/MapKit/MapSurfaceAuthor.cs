using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Persistent, source-preserving construction finishes for the existing maps.</summary>
    public static class MapSurfaceAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/EnvironmentSurfaces";
        private const string SourceTag="TumpSurfaceSource";
        private const string SourceNameTag="TumpSurfaceName";
        private static readonly Dictionary<(Mesh,Vector3,bool),Mesh> RoleMeshes=new Dictionary<(Mesh,Vector3,bool),Mesh>();
        private static readonly Dictionary<string,Material> SurfaceMaterials=new Dictionary<string,Material>(StringComparer.Ordinal);

        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_SURFACE_AUTHOR")??"Logs/map-surfaces-v1";
            string selected=Environment.GetEnvironmentVariable("TUMP_SURFACE_MAP");
            if(!string.IsNullOrEmpty(selected)&&!UI.SceneFlow.Maps.Contains(selected))throw new InvalidOperationException("Unknown map "+selected);
            Directory.CreateDirectory(output);var report=new StringBuilder();
            foreach(string map in UI.SceneFlow.Maps.Where(m=>string.IsNullOrEmpty(selected)||m==selected))
            {
                var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                int assigned=FinishLoadedScene(map,report);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                int restored=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&
                    r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_SurfaceKind")&&m.GetFloat("_SurfaceKind")>0));
                if(restored!=assigned)throw new InvalidOperationException(map+" lost authored surface overrides after reopening: "+restored+" / "+assigned);
                MapFinalInventory.WriteLoadedScene(map,output);
            }
            File.WriteAllText(Path.Combine(output,"coverage.txt"),report.ToString());Debug.Log(report.ToString());
            EditorApplication.Exit(0);
        }

        public static int FinishLoadedScene(string map,StringBuilder report,Transform scope=null)
        {
            RoleMeshes.Clear();SurfaceMaterials.Clear();
            var shader=Shader.Find("TumbangPreso/NearFade");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Architectural surface shader is unavailable or failed");
            Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
            var decisions=new SortedDictionary<string,int>(StringComparer.Ordinal);int changed=0;
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(scope!=null&&!renderer.transform.IsChildOf(scope))continue;
                if(!renderer.enabled)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                var original=OriginalMesh(filter.sharedMesh);string asset=AssetDatabase.GetAssetPath(original);
                string model=Path.GetFileNameWithoutExtension(asset);
                bool paletteParts=HasPartRoles(asset,model);
                var materials=renderer.sharedMaterials;bool mapped=false,write=filter.sharedMesh!=original;
                filter.sharedMesh=original;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=OriginalMaterial(materials[i]);if(source==null)continue;
                    write|=materials[i]!=source;materials[i]=source;
                    string name=SourceName(source).ToLowerInvariant();
                    int kind=paletteParts&&(name.Contains("colormap")||name.StartsWith("utility_original_aged",StringComparison.Ordinal))?1:Family(name);
                    if(kind==0)kind=ModelFamily(asset,model,name);
                    kind=ContextFamily(name,model,renderer.transform,kind);
                    string decision;
                    if(kind==0)decision=RetentionReason(name,renderer.transform)+" "+SourceName(source);
                    else
                    {
                        string context=map+"/"+Hierarchy(renderer.transform)+"/"+i;
                        var variant=Variant(PersistentSource(source,context),kind,paletteParts,map,renderer.bounds.min.y);
                        if(variant==null)decision="retained transparent/procedural "+SourceName(source);
                        else
                        {
                            materials[i]=variant;mapped=true;write=true;
                            filter.sharedMesh=DetailMesh(original,renderer.transform.lossyScale,paletteParts);
                            decision=(paletteParts?"authored part roles":"family "+kind)+" "+SourceName(source);
                        }
                    }
                    decisions[decision]=decisions.TryGetValue(decision,out int count)?count+1:1;
                }
                if(write)
                {
                    renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(filter);
                    if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    if(PrefabUtility.IsPartOfPrefabInstance(filter))PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                }
                if(mapped)changed++;
            }
            report.AppendLine(map+": "+changed+" renderers receive explicit surface roles.");
            foreach(var entry in decisions)report.AppendLine("  "+entry.Value+" | "+entry.Key);
            return changed;
        }

        public static void Repeatability()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_SURFACE_REPEAT")??"Logs/map-surface-repeatability-v1";
            string selected=Environment.GetEnvironmentVariable("TUMP_SURFACE_MAP");
            if(!string.IsNullOrEmpty(selected)&&!UI.SceneFlow.Maps.Contains(selected))throw new InvalidOperationException("Unknown map "+selected);
            Directory.CreateDirectory(output);var report=new StringBuilder();bool passed=true;
            foreach(string map in UI.SceneFlow.Maps.Where(m=>string.IsNullOrEmpty(selected)||m==selected))
            {
                SortedDictionary<string,string> first=null;
                for(int run=1;run<=2;run++)
                {
                    var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                    var before=MapRepeatabilityCheck.Capture(scene);
                    int mappedBefore=Object.FindObjectsByType<MeshRenderer>().Count(r=>r.enabled&&r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_SurfaceKind")&&m.GetFloat("_SurfaceKind")>0));
                    int mappedAfter=FinishLoadedScene(map,new StringBuilder());
                    if(mappedBefore!=mappedAfter)throw new InvalidOperationException(map+" lost or changed finish coverage during repeatability: "+mappedBefore+" -> "+mappedAfter);EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                    scene=EditorSceneManager.OpenScene(scene.path,OpenSceneMode.Single);
                    var current=MapRepeatabilityCheck.Capture(scene);MapRepeatabilityCheck.Write(output,map,"run"+run,current);
                    // Surface author may only change mesh/material references and
                    // generated assets, never transforms, colliders or gameplay.
                    bool Protected(string key)=>!key.StartsWith("Asset/",StringComparison.Ordinal)&&
                        !key.Contains("/UnityEngine.MeshRenderer[")&&!key.Contains("/UnityEngine.MeshFilter[");
                    var physicalBefore=new SortedDictionary<string,string>(before.Where(p=>Protected(p.Key)).ToDictionary(p=>p.Key,p=>p.Value));
                    var physicalAfter=new SortedDictionary<string,string>(current.Where(p=>Protected(p.Key)).ToDictionary(p=>p.Key,p=>p.Value));
                    var physical=MapRepeatabilityCheck.Differences(physicalBefore,physicalAfter);
                    File.WriteAllLines(Path.Combine(output,map+"-protected-run"+run+".txt"),physical);
                    report.AppendLine(map+" run"+run+": "+physical.Count+" protected changes / "+physicalAfter.Count+" rows");passed&=physical.Count==0;
                    if(first==null){first=current;continue;}
                    var differences=MapRepeatabilityCheck.Differences(first,current);
                    File.WriteAllLines(Path.Combine(output,map+"-differences.txt"),differences);
                    report.AppendLine(map+": "+differences.Count+" repeat differences / "+current.Count+" rows");passed&=differences.Count==0;
                }
            }
            File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString());Debug.Log(report.ToString());
            if(!passed)throw new InvalidOperationException("Surface author changed protected content or a second saved run");
            EditorApplication.Exit(0);
        }

        public static void RestoreLoadedSceneSources()
        {
            // Geometry generators recognize their original mesh/material names.
            // Restore those inputs before rebuilding, then apply finishes last.
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(filter.sharedMesh==null)continue;filter.sharedMesh=OriginalMesh(filter.sharedMesh);
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null)continue;
                renderer.sharedMaterials=renderer.sharedMaterials.Select(OriginalMaterial).ToArray();
                foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&!string.IsNullOrEmpty(material.GetTag(SourceNameTag,false)))material.name=SourceName(material);
                if(PrefabUtility.IsPartOfPrefabInstance(filter))PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static bool HasPartRoles(string asset,string model)
        {
            if((asset.Contains("/kits/commercial/")||asset.Contains("/kits/city/"))&&model.StartsWith("building-",StringComparison.Ordinal))return true;
            if(asset.Contains("/kits/commercial/")&&model.StartsWith("low-detail-building-",StringComparison.Ordinal))return true;
            if(asset.Contains("/kits/industrial/")&&new[]{"building-a","building-e","building-g","building-p","building-q","building-r","building-t","chimney-medium","chimney-large"}.Contains(model))return true;
            if(asset.Contains("/kits/car/")&&new[]{"sedan","van","delivery","taxi","truck"}.Contains(model))return true;
            if(asset.Contains("/kits/roads/")&&new[]{"road-bridge","light-square-double","light-square","light-curved","electricity-pole-single","sign-highway-detailed","sign-highway-wide","traffic-light"}.Contains(model))return true;
            if(asset.Contains("/kits/town/")&&model=="lantern")return true;
            if(asset.Contains("/kits/city/")&&model=="planter")return true;
            if(asset.Contains("/kits/factory/")&&(model=="box-small"||model=="box-wide"))return true;
            return asset.Contains("/kits/train/")&&new[]{"track-detailed","train-electric-city-a","train-electric-city-b","train-electric-city-c","train-diesel-a"}.Contains(model);
        }

        private static int ModelFamily(string asset,string model,string material)
        {
            // Only homogeneous imported parts are mapped wholesale. Mixed atlas
            // vehicles, trains and utility poles keep their authored parts until
            // an explicit per-face assignment exists; hue is not a material type.
            if(asset.Contains("/kits/roads/")&&model=="bridge-pillar-wide")return 2;
            if(asset.Contains("/kits/roads/")&&model=="construction-fence")return 8;
            if(asset.Contains("/kits/factory/")&&(model.StartsWith("pipe-",StringComparison.Ordinal)||model=="hopper-high-square"))return 8;
            if(asset.Contains("/kits/industrial/")&&model=="detail-tank")return 8;
            if(asset.Contains("/kits/town/"))
            {
                if(model=="stall-bench"||model=="stall-stool"||model=="planks")return 4;
                if(model=="hedge")return 15;
            }
            if(asset.Contains("/kits/forest/")&&model=="plant")return 15;
            if(asset.Contains("/kits/train/")&&model.StartsWith("train-carriage-",StringComparison.Ordinal))return 8;
            return 0;
        }

        private static int ContextFamily(string name,string model,Transform item,int fallback)
        {
            if(name=="cream goods"||name=="oxblood goods")
            {
                if(model=="retained-house-o-details")return 9; // Actual hanging towels.
                if(model=="retained-house-a-details")return 16; // Sealed sachets.
            }
            if(name=="stock_paper"||name=="stock_green"||name=="stock_oxblood")
            {
                string context=Hierarchy(item).ToLowerInvariant();
                if(context.Contains("bread loaf")||context.Contains("serving dish"))return 0;
                if(context.Contains("folded laundry")||context.Contains("folded stock"))return 9;
                if(context.Contains("barber chair")&&name=="stock_oxblood")return 11;
                if(context.Contains("washing machine"))return 8;
                if(context.Contains("photocopier"))return 16;
                return 18; // Supported boxes, packaging and paper reams.
            }
            return fallback;
        }

        private static string RetentionReason(string name,Transform item)
        {
            string context=Hierarchy(item).ToLowerInvariant();
            if(context.Contains("_jeepney"))return "retained supplied jeepney livery/finish";
            if(name=="ilalim_padbase")return "retained gameplay pad presentation";
            if(name.Contains("chalk")||name.Contains("crossing")||name.Contains("mark")||name=="score"||name.Contains("sign")||name.Contains("banner")||name.Contains("plaque")||name.Contains("mural")||
                name.Contains("lanedash")||name.Contains("throwline")||name.Contains("wall_paint")||name.Contains("rgbbar")||name=="piso_led_timer"||name=="piso_screen")return "retained readable artwork/marking";
            if(name.Contains("bread")||name.Contains("snacks")||name.Contains("watermelon")||name.Contains("sauce")||context.Contains("bread loaf")||context.Contains("serving dish"))return "retained stylized food form/palette";
            if(name=="mountain")return "retained supplied mountain art";
            if(name.Contains("poolwater")||name.Contains("poolceramic")||name=="recreationcourtcoating")return "retained authored water/ceramic/court";
            if(name.Contains("oilstain")||name.Contains("skid_")||name=="plaza_joint"||name=="ilalim_north"||name=="ilalim_south"||name=="ilalim_east"||name=="ilalim_west")return "retained authored ground marking/contact";
            if(name=="env_sari_sari_store_dark"||name=="env_church_facade_dark")return "retained recess shadow";
            return "retained/unassigned";
        }

        private static int Family(string name)
        {
            // Protect readable painted imagery and game markings. The coverage
            // report exposes every unmatched family instead of coating it blindly.
            if(name=="soil"||name=="potting soil"||name=="town_yards")return 17;
            if(name=="residentgreentile")return 19;
            if(name=="piso_trim")return 16;
            if(name=="env_sari_sari_store_tarp"||name=="env_sari_sari_store_stripe")return 9;
            if(name=="substantial dark window trim"||name=="band"||name=="edge"||name=="post"||name=="frame"||name=="trim"||name.StartsWith("sign_backing_",StringComparison.Ordinal))return 8;
            if(name=="mat_floor")return 2;
            if(name.StartsWith("dark cookware",StringComparison.Ordinal))return 8;
            if(name.StartsWith("sauce bottle",StringComparison.Ordinal))return 16;
            if(name.StartsWith("ilalim_arm_",StringComparison.Ordinal)||name.Contains("backlotpipetrestle")||name.Contains("guttergrate")||name.Contains("manholecover")||name.Contains("hoardingmast"))return 8;
            if(name.Contains("backgroundsidewalk")||name.Contains("apron"))return 13;
            if(name.Contains("backgroundcrossroad")||name.Contains("roadcontinuation")||name.Contains("fargroundplate")||name.Contains("manholecollar"))return 12;
            if(name.Contains("roadsubbase"))return 2;
            if(name.StartsWith("ilalim_cord",StringComparison.Ordinal))return name.Contains("tape")?16:11;
            if(name=="ilalim_extensionblock")return 16;
            if(name=="drum")return 8;
            if(name.Contains("mural"))return 0;
            if(name.Contains("chalk")||name.Contains("crossing")||name.Contains("mark")||name.Contains("sign")||name=="score"||name.Contains("banner"))return 0;
            if(name.Contains("skylinewindow")||name.Contains("glass")||name.Contains("glazing")||name=="stock_screen"||name=="quietwindow")return 7;
            if(name.Contains("plaster")||name=="env_municipal_hall_body")return 1;
            if(name=="env_bell_tower_stone"||name=="env_church_facade_stone"||name=="stone")return 3;
            if(name=="env_municipal_hall_plinth"||name=="env_municipal_hall_band"||name=="env_bell_tower_band"||name=="step")return 2;
            if(name=="env_bell_tower_window"||name=="env_municipal_hall_window"||name=="smoked windscreen"||name=="headlamp lens")return 7;
            if(name=="board"||name=="stems")return 4;
            if(name=="shop_floor"||name=="civic_approach")return 13;
            if(name=="bridge_drain"||name=="hoop_bracket"||name=="ring"||name=="tin"||name=="rim"||name=="piso_coinbox"||name=="dark cookware")return 8;
            if(name=="terminal oxblood paint"||name.StartsWith("muted oxblood cart",StringComparison.Ordinal))return 8;
            if(name.StartsWith("green shade",StringComparison.Ordinal)||name.StartsWith("plum shade",StringComparison.Ordinal)||name=="warm canopy")return 9;
            if(name=="reused cream pail"||name=="fadedwashbasin"||name=="quiet worn pail band"||name=="piso_bezel"||name=="piso_chair_red")return 16;
            if(name=="service_cord"||name=="utility_cable"||name=="dark vinyl seats")return 11;
            if(name.Contains("aerial")||name=="utility_wire")return 8;
            if(name.Contains("limestone")||name=="civic_window_stone")return 3;
            if(name.Contains("bark"))return 14;
            if(name.Contains("foliage")||name.Contains("leaf")||name=="tree_new_growth")return 15;
            if(name.Contains("timber")||name.Contains("wood")||name.Contains("clothespin"))return 4;
            if(name=="plaza_terracotta"||name.Contains("roof")&&name.Contains("clay"))return 6;
            if(name.Contains("terracotta")||name.Contains("clay")||name.Contains("pot terracotta"))return 10;
            if(name.Contains("roof")&&name.Contains("tile"))return 6;
            if(name.Contains("roof")&&!name.Contains("concrete")&&!name.Contains("paving")&&!name.Contains("coating"))return 5;
            if(name.Contains("concrete")||name.Contains("parapet")||name.Contains("limestone_base")||name.Contains("coping")||name.Contains("ledge")||name.Contains("bearing"))return 2;
            if(name.Contains("steel")||name.Contains("metal")||name=="wire"||name=="rust"||name=="shop_dark_frame")return 8;
            if(name.Contains("cloth")||name.Contains("fabric")||name.Contains("cotton")||name.Contains("braided")||name.Contains("towel")||name.Contains("shirt")||name.Contains("shorts"))return 9;
            if(name.Contains("rubber")||name.Contains("tyre"))return 11;
            if(name.Contains("asphalt"))return 12;
            if(name.Contains("paving")||name.Contains("pavement"))return 13;
            if(name.Contains("plastic"))return 16;
            return 0;
        }

        private static string Identity(Object asset)
        {
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long local))
                throw new InvalidOperationException("Surface source is not a persistent asset: "+asset.name);
            return guid+"_"+local;
        }

        private static string Hierarchy(Transform node)
        {
            string path=node.name+"["+node.GetSiblingIndex()+"]";
            while(node.parent!=null){node=node.parent;path=node.name+"["+node.GetSiblingIndex()+"]/"+path;}
            return path;
        }

        private static string SourceName(Material source)
        {
            string name=source.GetTag(SourceNameTag,false);
            if(!string.IsNullOrEmpty(name))return name;
            if(AssetDatabase.GetAssetPath(source).StartsWith(Folder+"/Sources/",StringComparison.Ordinal))
                throw new InvalidOperationException("Recover the original semantic name before reauthoring "+AssetDatabase.GetAssetPath(source));
            return source.name;
        }

        private static string SourcePath(string context)
        {
            string key;using(var sha=SHA256.Create())key=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(context))).Replace("-","").Substring(0,20);
            return Folder+"/Sources/"+key+".mat";
        }

        public static void RecoverSourceNames()
        {
            const string baselines="Assets/TumbangPreso/TempSurfaceNameBaseline/";
            string output=Environment.GetEnvironmentVariable("TUMP_SURFACE_NAMES_OUTPUT")??"Logs/surface-source-names-v1";
            Directory.CreateDirectory(output);var report=new StringBuilder();int updated=0;
            foreach(string map in UI.SceneFlow.Maps)
            {
                EditorSceneManager.OpenScene(baselines+map+".unity",OpenSceneMode.Single);
                foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var mats=renderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)
                    {
                        var original=mats[i];if(original==null)continue;
                        string path=SourcePath(map+"/"+Hierarchy(renderer.transform)+"/"+i);
                        var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(saved==null)continue;
                        if(!SameSurfaceProperties(original,saved))throw new InvalidOperationException("Baseline source mismatch: "+map+"/"+Hierarchy(renderer.transform)+" -> "+path);
                        saved.SetOverrideTag(SourceNameTag,original.name);EditorUtility.SetDirty(saved);updated++;
                        report.AppendLine(path+" | "+original.name);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            var unresolved=new SortedSet<string>(StringComparer.Ordinal);
            foreach(string map in UI.SceneFlow.Maps)
            {
                EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                foreach(var mat in renderer.sharedMaterials)
                {
                    var source=OriginalMaterial(mat);if(source==null)continue;
                    string path=AssetDatabase.GetAssetPath(source);
                    if(path.StartsWith(Folder+"/Sources/",StringComparison.Ordinal)&&string.IsNullOrEmpty(source.GetTag(SourceNameTag,false)))unresolved.Add(path);
                }
            }
            File.WriteAllText(Path.Combine(output,"recovered.txt"),report.ToString());File.WriteAllLines(Path.Combine(output,"unresolved.txt"),unresolved);
            Debug.Log("[Surface source names] recovered="+updated+" unresolved="+unresolved.Count);
            if(unresolved.Count!=0)throw new InvalidOperationException("Some currently referenced source names could not be recovered");
            EditorApplication.Exit(0);
        }

        private static bool SameSurfaceProperties(Material a,Material b)
        {
            if(a.shader!=b.shader)return false;
            for(int i=0;i<ShaderUtil.GetPropertyCount(a.shader);i++)
            {
                string property=ShaderUtil.GetPropertyName(a.shader,i);
                switch(ShaderUtil.GetPropertyType(a.shader,i))
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        if(Vector4.Distance(a.GetColor(property),b.GetColor(property))>.000001f)return false;break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        if(Vector4.Distance(a.GetVector(property),b.GetVector(property))>.000001f)return false;break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        if(Mathf.Abs(a.GetFloat(property)-b.GetFloat(property))>.000001f)return false;break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        if(a.GetTexture(property)!=b.GetTexture(property)||a.GetTextureScale(property)!=b.GetTextureScale(property)||a.GetTextureOffset(property)!=b.GetTextureOffset(property))return false;break;
                }
            }
            return true;
        }

        private static Material PersistentSource(Material source,string context)
        {
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string _,out long _))return source;
            // A few authored aerials store their material inline in the scene.
            // Preserve an exact source copy instead of losing its settings or
            // claiming a transient instance has a stable asset GUID.
            Directory.CreateDirectory(Folder+"/Sources");
            string path=SourcePath(context);var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            string name=SourceName(source);
            if(saved==null){saved=new Material(source);saved.SetOverrideTag(SourceNameTag,name);AssetDatabase.CreateAsset(saved,path);}
            else{EditorUtility.CopySerialized(source,saved);saved.SetOverrideTag(SourceNameTag,name);EditorUtility.SetDirty(saved);}
            return saved;
        }
        private static T Resolve<T>(string identity) where T:Object
        {
            int split=identity.LastIndexOf('_');if(split<0||!long.TryParse(identity.Substring(split+1),out long id))return null;
            string path=AssetDatabase.GUIDToAssetPath(identity.Substring(0,split));
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<T>().FirstOrDefault(a=>
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a,out string _,out long value)&&value==id);
        }
        internal static Mesh SourceMeshForAuthoring(Mesh mesh)=>OriginalMesh(mesh);
        internal static string SourceMaterialNameForAuthoring(Material material)=>SourceName(OriginalMaterial(material));

        private static Mesh OriginalMesh(Mesh mesh)
        {
            string path=AssetDatabase.GetAssetPath(mesh);
            if(!path.StartsWith(Folder+"/Meshes/",StringComparison.Ordinal))return mesh;
            var identity=Path.GetFileNameWithoutExtension(path).Substring("Surface_".Length).Split('_');
            return Resolve<Mesh>(identity[0]+"_"+identity[1])
                ??throw new InvalidOperationException("Lost original palette mesh for "+path);
        }
        private static Material OriginalMaterial(Material material)
        {
            if(material==null)return null;string original=material.GetTag(SourceTag,false);
            return string.IsNullOrEmpty(original)?material:Resolve<Material>(original)
                ??throw new InvalidOperationException("Lost original surface material "+original);
        }

        private static int PartRole(string source,Vector2 uv,Vector3 position)
        {
            string model=Path.GetFileNameWithoutExtension(source);float u=uv.x;
            bool At(float column)=>Mathf.Abs(u-column)<.018f;
            if(source.Contains("/kits/roads/"))
            {
                if(model=="sign-highway-detailed"||model=="sign-highway-wide")
                {
                    if(uv.y>=.5f&&(At(.21875f)||At(.34375f)))return 0; // Sign-face colors stay clean.
                    if(At(.34375f)||At(.46875f)||At(.59375f))return 8;
                }
                if(model=="traffic-light")
                {
                    if(At(.59375f)||At(.71875f)||At(.96875f))return 0; // Preserve the three signal lenses.
                    if(At(.21875f)||At(.34375f)||At(.46875f)||At(.84375f))return 8;
                }
                if(model=="road-bridge")
                {
                    if(At(.34375f)||At(.46875f))return 2;
                    if(At(.28125f))return 12;
                    if(At(.03125f)||At(.40625f))return 0; // Existing road markings.
                }
                if(model=="electricity-pole-single")
                {
                    if(At(.71875f))return 4;
                    if(At(.34375f))return 8;
                    if(At(.46875f))return position.y<.15f?2:8;
                }
                if(model=="light-square-double"||model=="light-square"||model=="light-curved")
                {
                    if(At(.46875f))return 8;
                    if(At(.53125f))return 0; // Retain the light emitter.
                }
            }
            else if(source.Contains("/kits/city/")&&model=="planter")
            {
                if(At(.09375f))return 2;
                if(At(.21875f))return 15;
                if(At(.59375f))return 17;
            }
            else if(source.Contains("/kits/town/")&&model=="lantern")
            {
                if(At(.21875f))return 8;
                if(At(.34375f))return 7;
                if(At(.96875f))return 2;
            }
            else if(source.Contains("/kits/factory/")&&(model=="box-small"||model=="box-wide"))
            {
                if(At(.71875f))return 18;
                if(At(.96875f))return 16; // Measured raised packing tape.
            }
            else if(source.Contains("/kits/car/"))
            {
                // Verified UV rows: glass/lenses0, dark running gear1, paint2.
                // A palette column alone would confuse body paint with plates.
                int row=Mathf.FloorToInt(uv.y*4);
                if(row==0&&(At(.09375f)||At(.21875f)||At(.34375f)))return 7;
                if(row==1)
                {
                    if(At(.34375f))return 11;
                    if(At(.46875f)||At(.71875f))return 8;
                    if(At(.84375f))return 0; // Preserve plates/white markings.
                }
                if(row==2&&(At(.46875f)||At(.59375f)||At(.84375f)||At(.96875f)))return 8;
            }
            else if(source.Contains("/kits/industrial/"))
            {
                if(model.StartsWith("chimney-",StringComparison.Ordinal))
                {
                    if(At(.09375f))return 8;
                    if(At(.46875f)||At(.21875f))return 2;
                }
                if(At(.09375f))return 8; // Pipes, flashing and window frames.
                if(At(.21875f))return uv.y>=.25f&&uv.y<.5f?1:0; // Retain small authored door/vent regions.
                if(At(.46875f)||At(.59375f))return 2; // Concrete base/stack and measured steps.
                if(At(.71875f))return 7;
            }
            else if(source.Contains("/kits/train/")&&(model.StartsWith("train-electric-city-",StringComparison.Ordinal)||model=="train-diesel-a"))
            {
                if(At(.09375f))return 7;
                if(At(.84375f))return 0; // Authored dark grille/marking regions.
                if(At(.21875f)||At(.34375f)||At(.59375f)||At(.71875f)||At(.96875f))return 8;
            }
            else if(source.Contains("/kits/train/")&&model=="track-detailed")
            {
                if(At(.21875f))return 4;
                if(At(.96875f))return 8;
            }
            else if(source.Contains("/kits/commercial/")&&model.StartsWith("low-detail-building-",StringComparison.Ordinal))
            {
                if(At(.09375f))return 7;
                if(At(.46875f))return 1;
            }
            else
            {
                bool residential=source.Contains("/kits/city/");
                // Verified Unity UV studies; imported V is flipped from raw glTF.
                if(At(.09375f))return uv.y>=.5f?5:8;
                if(At(.21875f))return residential?4:1;
                if(At(.46875f))return 1;
                if(At(.59375f))return 2;
                if(At(.71875f))return 7;
            }
            throw new InvalidOperationException("Unreviewed atlas role "+uv+" in "+source);
        }

        private static Mesh DetailMesh(Mesh original,Vector3 scale,bool palette)
        {
            scale=new Vector3(Mathf.Max(.0001f,Mathf.Round(Mathf.Abs(scale.x)*10000)/10000),
                Mathf.Max(.0001f,Mathf.Round(Mathf.Abs(scale.y)*10000)/10000),Mathf.Max(.0001f,Mathf.Round(Mathf.Abs(scale.z)*10000)/10000));
            var key=(original,scale,palette);
            if(RoleMeshes.TryGetValue(key,out var cached)&&cached!=null)return cached;
            if(palette&&original.colors.Length!=0)throw new InvalidOperationException("Existing vertex colors require a separate role channel: "+original.name);
            var existingUv=new List<Vector2>();original.GetUVs(2,existingUv);
            if(existingUv.Count!=0)throw new InvalidOperationException("UV3 already carries source data: "+original.name);
            var mesh=Object.Instantiate(original);var uv=mesh.uv;var roles=new Color[uv.Length];
            string sourcePath=AssetDatabase.GetAssetPath(original);var sourceVertices=original.vertices;
            if(palette)for(int i=0;i<uv.Length;i++)
                roles[i]=new Color(PartRole(sourcePath,uv[i],sourceVertices[i])/32f,0,0,1);
            if(palette)
            {
                var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                    if(roles[indices[i]].r!=roles[indices[i+1]].r||roles[indices[i]].r!=roles[indices[i+2]].r)
                    {Object.DestroyImmediate(mesh);throw new InvalidOperationException("A triangle crosses material roles in "+original.name);}
                mesh.colors=roles;
            }
            var vertices=mesh.vertices;var normals=mesh.normals;var detail=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=Vector3.Scale(vertices[i],scale);var normal=normals[i];
                var n=new Vector3(Mathf.Abs(normal.x)/scale.x,Mathf.Abs(normal.y)/scale.y,Mathf.Abs(normal.z)/scale.z).normalized;
                if(n.y>.2f&&n.y<.99f)
                {var v=n.x>n.z?new Vector2(p.z,p.x):new Vector2(p.x,p.z);v.y/=n.y;detail[i]=v;}
                else detail[i]=n.y>.65f?new Vector2(p.x,p.z):n.x>n.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
            }
            mesh.SetUVs(2,detail);
            string sizes=FormattableString.Invariant($"{scale.x:F4}_{scale.y:F4}_{scale.z:F4}");
            string path=Folder+"/Meshes/Surface_"+Identity(original)+"_"+sizes+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            RoleMeshes[key]=saved;return saved;
        }

        private static Material Variant(Material source,int kind,bool roles,string map,float bottom)
        {
            bool city=map=="SaBubong"&&bottom<-5;
            string path=Folder+"/Materials/"+Identity(source)+"_"+kind+"_"+(roles?"atlas":"plain")+"_"+map+(city?"_city":"")+".mat";
            if(SurfaceMaterials.TryGetValue(path,out var cached))return cached;
            var copy=Visual.NearFade.CopySurfaceForAuthoring(source);if(copy==null)return null;
            copy.SetFloat("_SurfaceKind",kind);copy.SetFloat("_SurfaceVertexRoles",roles?1:0);
            copy.SetFloat("_SurfaceCoordinates",1);
            copy.SetFloat("_SurfaceHasTexture",copy.GetTexture("_MainTex")!=null?1:0);
            copy.SetFloat("_SurfaceScale",kind==13?.6333333f:1);
            copy.SetFloat("_SurfaceStrength",map=="IlalimNgTulay"?1.15f:map=="SaBubong"?.80f:1);
            copy.SetFloat("_SurfaceBaseY",city?-26.049f:map=="IlalimNgTulay"?.212f:.1f);
            copy.SetFloat("_SurfaceDebug",0);copy.SetOverrideTag(SourceTag,Identity(source));
            copy.name=SourceName(source)+" / "+(roles?"architectural parts":"surface "+kind);
            var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(copy,path);saved=copy;}
            else{EditorUtility.CopySerialized(copy,saved);Object.DestroyImmediate(copy);EditorUtility.SetDirty(saved);}
            SurfaceMaterials[path]=saved;return saved;
        }

        public static void RoleStudy()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_SURFACE_STUDY")??"Logs/map-surface-role-study-v1";
            Directory.CreateDirectory(output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.72f,.74f,.76f);
            var sun=new GameObject("Study sun").AddComponent<Light>();sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(36,-35,0);sun.intensity=1;
            var camera=new GameObject("Surface study camera").AddComponent<Camera>();
            camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.16f,.18f,.19f);camera.fieldOfView=38;camera.nearClipPlane=.03f;
            var roleMaterial=new Material(Shader.Find("TumbangPreso/NearFade"));
            roleMaterial.SetFloat("_SurfaceDebug",2);
            if(ShaderUtil.ShaderHasError(roleMaterial.shader))throw new InvalidOperationException("Surface shader failed compilation");
            File.WriteAllText(Path.Combine(output,"scope.txt"),
                "Read-only imported geometry study. Source material versus UV-column false colors; not production art.\n"+
                "For u<.16: purple v<.5, red v>=.5. Green:.16<=u<.34, blue:.34<=u<.60, gold:u>=.60. Confirm actual parts before semantic role assignment.\n");
            string selectedModels=Environment.GetEnvironmentVariable("TUMP_SURFACE_STUDY_MODELS");
            var models=string.IsNullOrEmpty(selectedModels)?new[]{"commercial/building-b","commercial/building-m","commercial/building-skyscraper-a",
                "city/building-type-a","city/building-type-c","city/building-type-e"}:selectedModels.Split(',');
            var roleStats=new StringBuilder();
            foreach(string relative in models)
            {
                string model=Path.GetFileName(relative);
                string path="Assets/TumbangPreso/Art/models/kits/"+relative+".glb";
                var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var renderers=instance.GetComponentsInChildren<MeshRenderer>();
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                instance.transform.localScale*=3.2f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                instance.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                var filters=instance.GetComponentsInChildren<MeshFilter>();
                foreach(var filter in filters)
                {
                    var sourceMesh=filter.sharedMesh;var sourceUv=sourceMesh.uv;var vertices=sourceMesh.vertices;
                    roleStats.AppendLine(relative+" / "+sourceMesh.name);
                    foreach(var group in Enumerable.Range(0,sourceUv.Length).GroupBy(i=>sourceUv[i]).OrderBy(g=>g.Key.x).ThenBy(g=>g.Key.y))
                    {
                        var region=new Bounds(vertices[group.First()],Vector3.zero);
                        foreach(int vertex in group)region.Encapsulate(vertices[vertex]);
                        roleStats.AppendLine(FormattableString.Invariant($"uv {group.Key.x:F5},{group.Key.y:F5}: {group.Count()} vertices; min {region.min:F4}; max {region.max:F4}"));
                    }
                }
                for(int pass=0;pass<2;pass++)
                {
                    if(pass==1)
                    {
                        foreach(var f in filters)
                        {
                            var mesh=Object.Instantiate(f.sharedMesh);
                            mesh.colors=mesh.uv.Select(uv=>uv.x<.16f?(uv.y<.5f?new Color(.7f,.15f,.85f):new Color(.90f,.20f,.12f)):
                                uv.x<.34f?new Color(.18f,.74f,.48f):uv.x<.60f?new Color(.20f,.40f,.94f):new Color(1,.72f,.16f)).ToArray();
                            f.sharedMesh=mesh;
                        }
                        foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(_=>roleMaterial).ToArray();
                    }
                    foreach(int side in new[]{1,-1})
                    {
                        camera.transform.position=new Vector3(side*4.6f,3.5f,side*5.4f);
                        camera.transform.LookAt(new Vector3(0,bounds.size.y*.5f,0));
                        Capture(camera,Path.Combine(output,model+(pass==0?"-source":"-uv-columns")+(side>0?"-front":"-back")+".png"));
                    }
                }
                for(int i=0;i<filters.Length;i++)Object.DestroyImmediate(filters[i].sharedMesh);
                Object.DestroyImmediate(instance);
            }
            File.WriteAllText(Path.Combine(output,"uv-regions.txt"),roleStats.ToString());
            if(ShaderUtil.ShaderHasError(roleMaterial.shader))throw new InvalidOperationException("Surface shader failed during rendering");
            Object.DestroyImmediate(roleMaterial);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(sun.gameObject);
            EditorApplication.Exit(0);
        }

        private static void Capture(Camera camera,string path)
        {
            var hdr=new RenderTexture(800,800,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};hdr.Create();
            var display=RenderTexture.GetTemporary(800,800,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous=RenderTexture.active;bool write=GL.sRGBWrite;
            var pixels=new Texture2D(800,800,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=hdr;camera.Render();GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Graphics.Blit(hdr,display);RenderTexture.active=display;pixels.ReadPixels(new Rect(0,0,800,800),0,0);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;GL.sRGBWrite=write;
                RenderTexture.ReleaseTemporary(display);hdr.Release();Object.DestroyImmediate(hdr);Object.DestroyImmediate(pixels);
            }
        }
    }
}
