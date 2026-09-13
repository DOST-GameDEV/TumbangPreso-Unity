using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Native roofdeck scene author. Menu registration follows recovery qualification.</summary>
    public static class SaBubongBuilder
    {
        public const string ScenePath="Assets/TumbangPreso/Scenes/Maps/SaBubong.unity";
        private const string Folder="Assets/TumbangPreso/Art/SaBubong";
        private static Material Concrete,Edge,Tile,Steel,Timber,Paint,Glass;
        public static void Run()
        {
            Build();EditorApplication.Exit(0);
        }

        public static void Repeatability()
        {
            string output=Environment.GetEnvironmentVariable("TUMP_ROOF_REPEAT")??"Logs/sa-bubong-repeatability-v1";
            Directory.CreateDirectory(output);SortedDictionary<string,string> first=null;
            for(int run=1;run<=2;run++)
            {
                Build();
                var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                var current=MapRepeatabilityCheck.Capture(scene);
                MapRepeatabilityCheck.Write(output,"SaBubong","run"+run,current);
                if(run==1){first=current;continue;}
                var changes=MapRepeatabilityCheck.Differences(first,current);
                File.WriteAllLines(Path.Combine(output,"differences.txt"),changes);
                string report=$"SaBubong: {changes.Count} changed rows; {current.Count} compared rows";
                File.WriteAllText(Path.Combine(output,"report.txt"),report);Debug.Log(report);
                bool supported=MapGeometryCheck.Execute(true);
                EditorApplication.Exit(changes.Count==0&&supported?0:1);
            }
        }

        public static void Review()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Object.FindFirstObjectByType<EnvColourPass>()?.Apply();
            TumbangPreso.Settings.GraphicsProfiles.Apply(1);
            var camera=new GameObject("Roofdeck review witness").AddComponent<Camera>();
            camera.enabled=false;camera.nearClipPlane=.05f;camera.farClipPlane=400;camera.fieldOfView=65;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled=true;
            var shots=new[]{
                ("aerial",new Vector3(28,28,-33),new Vector3(0,0,2)),
                ("court",new Vector3(0,1.65f,-12),new Vector3(0,1.65f,11)),
                ("pool-and-shade",new Vector3(3,1.65f,0),new Vector3(-11,1.65f,0)),
                ("open-edge",new Vector3(10,1.65f,-2),new Vector3(20,0,0))};
            foreach(var shot in shots)
            {
                camera.transform.SetPositionAndRotation(shot.Item2,Quaternion.LookRotation(shot.Item3-shot.Item2));
                var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};
                rt.Create();camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;
                var display=RenderTexture.GetTemporary(1600,1000,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                bool write=GL.sRGBWrite;GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Graphics.Blit(rt,display);GL.sRGBWrite=write;RenderTexture.active=display;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
                Directory.CreateDirectory("Logs/sa-bubong-layout-v1");
                File.WriteAllBytes("Logs/sa-bubong-layout-v1/"+shot.Item1+".png",image.EncodeToPNG());
                RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(display);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            }
            Object.DestroyImmediate(camera.gameObject);EditorApplication.Exit(0);
        }

        public static void Build()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("SaBubong").transform;root.gameObject.AddComponent<MapGrade>();
            root.gameObject.AddComponent<EnvColourPass>();
            root.gameObject.AddComponent<RooftopRecovery>();
            Concrete=Mat("Warm roof concrete",new Color(.60f,.57f,.49f));
            Edge=Mat("Weathered parapet",new Color(.73f,.68f,.55f));
            Tile=Mat("Resident green tile",new Color(.35f,.49f,.42f));
            Steel=Mat("Painted steel",new Color(.22f,.28f,.29f));
            Timber=Mat("Shaded timber",new Color(.43f,.29f,.17f));
            Paint=Mat("Court chalk",new Color(.85f,.79f,.62f));
            Glass=Mat("Quiet window",new Color(.19f,.29f,.34f));
            var dressing=Group(root,"Dressing");
            var floor=Group(root,"Floor");
            Box(floor,"Structural roof slab",new Vector3(0,-.1f,0),new Vector3(28,.4f,36),Concrete,true);
            var paving=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/CivicTown/civic_paving.mat");
            GroundSkin(floor,paving);
            var shell=Group(dressing,"Condo building");
            Box(shell,"Substantial building body",new Vector3(0,-13.125f,0),new Vector3(27.5f,25.85f,35.5f),Edge);
            for(int storey=0;storey<7;storey++)
            {
                float y=-2.4f-storey*3.35f;
                Box(shell,"Floor band",new Vector3(0,y,0),new Vector3(27.8f,.25f,35.8f),Concrete);
                for(int bay=0;bay<7;bay++)foreach(float side in new[]{-1f,1f})
                {
                    Box(shell,"Occupied balcony bay",new Vector3(side*13.83f,y-1.65f,-14.4f+bay*4.8f),new Vector3(.12f,1.55f,2.55f),Glass);
                    Box(shell,"Balcony sill",new Vector3(side*14.06f,y-2.43f,-14.4f+bay*4.8f),new Vector3(.66f,.18f,2.95f),Concrete);
                }
                for(int bay=0;bay<5;bay++)foreach(float side in new[]{-1f,1f})
                    Box(shell,"End facade window",new Vector3((bay-2)*4.9f,y-1.55f,side*17.79f),new Vector3(2.75f,1.65f,.10f),Glass);
            }
            Bake(shell,"CondoShell");
            Rail(dressing,"West parapet",new Vector3(-13.8f,.1f,0),36,true);
            Rail(dressing,"North parapet",new Vector3(0,.1f,17.8f),28,false);
            Rail(dressing,"South parapet",new Vector3(0,.1f,-17.8f),28,false);
            Rail(dressing,"East north parapet",new Vector3(13.8f,.1f,10.6f),14.4f,true);
            Rail(dressing,"East south parapet",new Vector3(13.8f,.1f,-10.6f),14.4f,true);
            // The visible opening reaches the physical edge atX14. Its fall
            // recovery is implemented separately before this map enters the menu.
            Box(dressing,"Edge warning paint",new Vector3(13.13f,.107f,0),new Vector3(.16f,.012f,6.5f),Mat("Faded safety ochre",new Color(.67f,.48f,.15f)));
            Pool(dressing);Shade(dressing);Stairhead(dressing);Laundry(dressing);
            Skyline(dressing);
            Gameplay(root);
            var sun=new GameObject("Sun").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;
            MapAtmosphereAuthor.Apply("SaBubong");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))
                EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            MapFinalInventory.WriteLoadedScene("SaBubong","Logs/sa-bubong-layout-v1");
            Debug.Log("[Sa Bubong] Scene and map registration authored. Recovery is an initial implementation requiring physics,input and network qualification.");
        }

        private static void GroundSkin(Transform root,Material material)
        {
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Roof paving";quad.transform.SetParent(root,false);
            quad.transform.localPosition=new Vector3(0,.101f,0);quad.transform.localRotation=Quaternion.Euler(90,0,0);
            quad.transform.localScale=new Vector3(28,36,1);Object.DestroyImmediate(quad.GetComponent<Collider>());
            var m=Mat("Roof paving",Color.white);m.CopyPropertiesFromMaterial(material);m.mainTextureScale=new Vector2(28f/6,36f/6);
            m.SetTextureScale("_BumpMap",m.mainTextureScale);EditorUtility.SetDirty(m);
            var renderer=quad.GetComponent<Renderer>();renderer.sharedMaterial=m;renderer.shadowCastingMode=ShadowCastingMode.Off;
            var coat=GameObject.CreatePrimitive(PrimitiveType.Quad);coat.name="Painted recreation court";coat.transform.SetParent(root,false);
            coat.transform.localPosition=new Vector3(0,.103f,0);coat.transform.localRotation=Quaternion.Euler(90,0,0);coat.transform.localScale=new Vector3(16,24,1);
            Object.DestroyImmediate(coat.GetComponent<Collider>());
            var paint=Mat("Recreation court coating",Color.white);
            foreach(string name in new[]{"roof-court-paint.png","roof-court-normal.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/"+name);
                importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.mipmapEnabled=true;
                if(name.Contains("normal"))importer.textureType=TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            paint.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/roof-court-paint.png");
            paint.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/roof-court-normal.png"));
            paint.EnableKeyword("_NORMALMAP");paint.SetFloat("_BumpScale",.65f);
            paint.mainTextureScale=new Vector2(4,6);paint.SetTextureScale("_BumpMap",new Vector2(4,6));EditorUtility.SetDirty(paint);
            coat.GetComponent<Renderer>().sharedMaterial=paint;coat.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        private static void Rail(Transform root,string name,Vector3 at,float length,bool alongZ)
        {
            var group=Group(root,name);group.localPosition=at;
            Vector3 size=alongZ?new Vector3(.28f,.38f,length):new Vector3(length,.38f,.28f);
            Box(group,"Low concrete kerb",new Vector3(0,.19f,0),size,Edge,true);
            size.y=1.15f;size.x=alongZ?.12f:length;size.z=alongZ?length:.12f;
            // One simple collision surface; visible pickets remain lightweight.
            var collision=new GameObject("Rail collision").AddComponent<BoxCollider>();collision.transform.SetParent(group,false);
            collision.center=new Vector3(0,.91f,0);collision.size=size;
            for(int rail=0;rail<2;rail++)Box(group,"Horizontal rail",new Vector3(0,.68f+rail*.58f,0),
                alongZ?new Vector3(.085f,.085f,length):new Vector3(length,.085f,.085f),Steel);
            int posts=Mathf.CeilToInt(length/.68f);
            for(int i=0;i<=posts;i++)
            {
                float v=Mathf.Lerp(-length*.5f,length*.5f,i/(float)posts);
                Box(group,"Rail picket",new Vector3(alongZ?0:v,.79f,alongZ?v:0),new Vector3(.065f,.94f,.065f),Steel);
            }
            Bake(group,name.Replace(" ",""));
        }

        private static void Pool(Transform root)
        {
            var group=Group(root,"Resident pool");group.localPosition=new Vector3(-10.7f,.1f,6.7f);
            Box(group,"Pool basin",new Vector3(0,.29f,0),new Vector3(4.5f,.58f,9.4f),Tile,true);
            var water=Mat("Pool water",new Color(.14f,.48f,.51f));water.SetFloat("_Glossiness",.64f);EditorUtility.SetDirty(water);
            Box(group,"Still pool surface",new Vector3(0,.60f,0),new Vector3(3.9f,.035f,8.8f),water);
            foreach(float side in new[]{-1f,1f})
            {
                Box(group,"Pool edge coping",new Vector3(side*2.12f,.65f,0),new Vector3(.3f,.16f,9.4f),Edge);
                Box(group,"Pool end coping",new Vector3(0,.65f,side*4.56f),new Vector3(4.5f,.16f,.3f),Edge);
            }
            Rail(root,"Pool court fence",new Vector3(-8.15f,.1f,6.7f),10.3f,true);
            Rail(root,"Pool south fence",new Vector3(-10.7f,.1f,1.55f),5.3f,false);
            Rail(root,"Pool north fence",new Vector3(-10.7f,.1f,11.85f),5.3f,false);
        }

        private static void Shade(Transform root)
        {
            var group=Group(root,"Residents shade");group.localPosition=new Vector3(-10.25f,.1f,-8.4f);
            foreach(float x in new[]{-2.1f,2.1f})foreach(float z in new[]{-2.2f,2.2f})
                Box(group,"Shade post",new Vector3(x,1.6f,z),new Vector3(.23f,3.2f,.23f),Timber,true);
            Box(group,"Shade roof",new Vector3(0,3.2f,0),new Vector3(4.9f,.20f,5.3f),Tile);
            for(int i=0;i<2;i++)
            {
                float z=i==0?-1.25f:1.25f;
                Box(group,"Resident table",new Vector3(0,.81f,z),new Vector3(2.1f,.14f,.8f),Timber,true);
                foreach(float side in new[]{-1f,1f})Box(group,"Table leg",new Vector3(side*.75f,.39f,z),new Vector3(.14f,.78f,.55f),Timber,true);
                foreach(float side in new[]{-1f,1f})
                {
                    Box(group,"Bench seat",new Vector3(0,.43f,z+side*.8f),new Vector3(2,.13f,.38f),Timber,true);
                    foreach(float x in new[]{-.7f,.7f})Box(group,"Bench leg",new Vector3(x,.185f,z+side*.8f),new Vector3(.12f,.37f,.3f),Timber,true);
                }
            }
            // The roof meets all four posts; table and bench tops meet their legs.
            // Bake this supported assembly, retaining its individual collision shapes.
            Bake(group,"ResidentShade");
        }

        private static void Stairhead(Transform root)
        {
            var group=Group(root,"Residential stairhead");group.localPosition=new Vector3(8,.1f,15.0f);
            Box(group,"Concrete stairhouse",new Vector3(0,1.5f,0),new Vector3(4.6f,3,4),Edge,true);
            Box(group,"Stairhouse roof",new Vector3(0,3.07f,0),new Vector3(5,.24f,4.4f),Concrete,true);
            Box(group,"Steel access door",new Vector3(-.65f,1.04f,-2.018f),new Vector3(1.05f,2.08f,.06f),Steel);
            Box(group,"Ventilation louvre",new Vector3(1.2f,2.25f,-2.025f),new Vector3(.9f,.6f,.07f),Glass);
            Box(group,"Resident noticeboard",new Vector3(1.22f,1.25f,-2.043f),new Vector3(.88f,.8f,.09f),Timber);
            Box(group,"Notice paper",new Vector3(1.1f,1.3f,-2.099f),new Vector3(.36f,.5f,.02f),Paint);
            // These are wall-mounted details, with backs meeting the concrete/board.
            Bake(group,"ResidentStairhead");
        }

        private static void Laundry(Transform root)
        {
            var group=Group(root,"Laundry service corner");
            ResidentLaundryAuthor.Place(group,"courtyard-line",new Vector3(-8.5f,2.7f,14.7f),0,.1f);
            Box(group,"Water tank plinth",new Vector3(-2.9f,.27f,15.5f),new Vector3(1.6f,.34f,1.6f),Concrete,true);
            var tank=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tank.name="Resident water tank";tank.transform.SetParent(group,false);
            tank.transform.localPosition=new Vector3(-2.9f,1.12f,15.5f);tank.transform.localScale=new Vector3(1.35f,.69f,1.35f);
            tank.GetComponent<Renderer>().sharedMaterial=Mat("Tank steel",new Color(.52f,.57f,.56f));
        }

        private static void Skyline(Transform root)
        {
            var city=Group(root,"Metro rooftops");
            var sites=new[]{new Vector3(32,36,4),new Vector3(-32,33,1),new Vector3(33.5f,42,-35.5f),new Vector3(-33.5f,35,-35.5f),
                new Vector3(33.5f,38,35.5f),new Vector3(-33.5f,32,35.5f),new Vector3(62,47,6),new Vector3(-62,41,11),
                new Vector3(5,46,66),new Vector3(-10,44,-63),new Vector3(59,37,58),new Vector3(-60,47,61),
                new Vector3(63,34,-56),new Vector3(-66,38,-61)};
            for(int i=0;i<sites.Length;i++)
            {
                string model="building-skyscraper-"+(char)('a'+i%5);
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/commercial/"+model+".glb");
                if(asset==null)throw new InvalidOperationException("Missing retained skyline model "+model);
                var building=(GameObject)PrefabUtility.InstantiatePrefab(asset);building.name="CityBlock_"+i;
                building.transform.SetParent(city,false);building.transform.localRotation=Quaternion.Euler(0,(i%4)*90,0);
                var renderers=building.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;
                foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
                float scale=Mathf.Min(sites[i].y/b.size.y,20/b.size.x,20/b.size.z);
                building.transform.localScale=Vector3.one*scale;b=renderers[0].bounds;
                foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
                building.transform.position=new Vector3(sites[i].x-b.center.x,-26.049f-b.min.y,sites[i].z-b.center.z);
                building.isStatic=true;
            }
            var ground=Box(city,"Distant city ground",new Vector3(0,-26.45f,0),new Vector3(220,.8f,220),Concrete);
            ground.isStatic=true;
            var asphalt=Mat("City asphalt",new Color(.42f,.42f,.39f));
            asphalt.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/models/textures/asphalt.png");EditorUtility.SetDirty(asphalt);
            foreach(float x in new[]{-49f,-18f,18f,49f})
                Box(city,"Connected city road X",new Vector3(x,-26.025f,0),new Vector3(5.7f,.04f,190),asphalt);
            foreach(float z in new[]{-49f,-22f,22f,49f})
                Box(city,"Connected city road Z",new Vector3(0,-26.024f,z),new Vector3(190,.04f,5.7f),asphalt);
            var houses=Group(root,"Bahay");int houseIndex=0;
            foreach(float x in new[]{-40f,-28f,28f,40f})foreach(float z in new[]{-38f,-30f,-12f,0f,12f,30f,38f})
            {
                if(sites.Any(p=>Mathf.Abs(p.x-x)<7&&Mathf.Abs(p.z-z)<7))continue;
                string type=houseIndex%2==0?"a":"c";
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/city/building-type-"+type+".glb");
                var house=(GameObject)PrefabUtility.InstantiatePrefab(asset);house.name="Bahay_RoofBelow_"+houseIndex++;
                house.transform.SetParent(houses,false);house.transform.localScale=Vector3.one*4.5f;
                house.transform.localRotation=Quaternion.Euler(0,x<0?90:270,0);
                var renderers=house.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
                house.transform.position=new Vector3(x-b.center.x,-26.049f-b.min.y,z-b.center.z);
            }
        }

        private static void Gameplay(Transform root)
        {
            var match=Group(root,"~Match");match.gameObject.AddComponent<MatchInstaller>();
            var kill=Group(root,"KillPlane");kill.localPosition=Vector3.down*10;kill.gameObject.AddComponent<KillPlane>();
            var trigger=kill.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;
            trigger.size=new Vector3(KillPlane.PlaneExtent,KillPlane.PlaneThickness,KillPlane.PlaneExtent);
            var bounds=Group(root,"Bounds");
            foreach(float side in new[]{-1f,1f})
            {
                var x=Group(bounds,"Limit X "+side).gameObject.AddComponent<BoxCollider>();x.isTrigger=true;x.center=new Vector3(side*15,5,0);x.size=new Vector3(.2f,30,40);
                var z=Group(bounds,"Limit Z "+side).gameObject.AddComponent<BoxCollider>();z.isTrigger=true;z.center=new Vector3(0,5,side*19);z.size=new Vector3(32,30,.2f);
            }
            var chalk=Group(root,"Chalk");float r=Balance.ConfinementRadius;
            foreach(float side in new[]{-1f,1f})
            {
                Box(chalk,"Court X",new Vector3(side*r,.109f,0),new Vector3(.085f,.015f,r*2),Paint);
                Box(chalk,"Court Z",new Vector3(0,.109f,side*r),new Vector3(r*2,.015f,.085f),Paint);
                Box(chalk,"Throwing line",new Vector3(0,.109f,side*Confinement.ThrowingLine()),new Vector3(14,.015f,.06f),Paint);
            }
            var spawns=Group(root,"SpawnPoints");Group(spawns,"Spawn0").localPosition=new Vector3(0,.1f,0);
            foreach(var renderer in chalk.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;
            for(int i=1;i<4;i++)Group(spawns,"Spawn"+i).localPosition=new Vector3((i-2)*3,.1f,-Confinement.AttackerSpawnRing());
        }

        private static Transform Group(Transform parent,string name)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static Material Mat(string name,Color color)
        {
            string path=Folder+"/"+name.Replace(" ","")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",.10f);EditorUtility.SetDirty(m);return m;
        }
        private static GameObject Box(Transform parent,string name,Vector3 at,Vector3 size,Material material,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;
            if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static void Bake(Transform root,string name)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>();var groups=filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray();
            var temporary=new List<Mesh>();var parts=new List<CombineInstance>();
            foreach(var group in groups)
            {
                var mesh=new Mesh();mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                temporary.Add(mesh);parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});
            }
            var final=new Mesh{name=name};final.CombineMeshes(parts.ToArray(),false,true);
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null)AssetDatabase.CreateAsset(final,path);else{EditorUtility.CopySerialized(final,saved);Object.DestroyImmediate(final);final=saved;EditorUtility.SetDirty(saved);}
            foreach(var filter in filters)
            {
                Object.DestroyImmediate(filter.GetComponent<Renderer>());Object.DestroyImmediate(filter);
            }
            foreach(var mesh in temporary)Object.DestroyImmediate(mesh);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=final;root.gameObject.AddComponent<MeshRenderer>().sharedMaterials=groups.Select(g=>g.Key).ToArray();
        }
    }
}
