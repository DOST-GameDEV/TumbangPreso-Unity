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
            if(!SwimmingAnimationAuthor.Execute())throw new InvalidOperationException("Swimming clips were not baked.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("SaBubong").transform;root.gameObject.AddComponent<MapGrade>();
            root.gameObject.AddComponent<EnvColourPass>();
            root.gameObject.AddComponent<RooftopRecovery>();
            root.gameObject.AddComponent<RooftopPool>();
            Concrete=Mat("Warm roof concrete",new Color(.60f,.57f,.49f));
            Edge=Mat("Weathered parapet",new Color(.73f,.68f,.55f));
            Tile=Mat("Resident green tile",new Color(.35f,.49f,.42f));
            Steel=Mat("Painted steel",new Color(.22f,.28f,.29f));
            Timber=Mat("Shaded timber",new Color(.43f,.29f,.17f));
            Paint=Mat("Court chalk",new Color(.85f,.79f,.62f));
            Glass=Mat("Quiet window",new Color(.19f,.29f,.34f));
            var dressing=Group(root,"Dressing");
            var floor=Group(root,"Floor");
            foreach(var section in RoofSections(RooftopRecovery.HalfX,RooftopRecovery.HalfZ))
                Box(floor,"Structural roof slab",new Vector3(section.center.x,-.1f,section.center.y),
                    new Vector3(section.width,.4f,section.height),Concrete,true);
            var paving=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/CivicTown/civic_paving.mat");
            GroundSkin(floor,paving);
            var shell=Group(dressing,"Condo building");
            // The basin slab rests on this structural level, rather than cutting
            // through the old building cap. Keep the city-ground foot unchanged.
            const float buildingBottom=-26.05f;
            float basinUnderside=RooftopPool.FloorY-.2f;
            Box(shell,"Substantial building body",new Vector3(0,(buildingBottom+basinUnderside)*.5f,0),
                new Vector3(37.5f,basinUnderside-buildingBottom,43.5f),Edge);
            foreach(var section in RoofSections(18.75f,21.75f))
                Box(shell,"Upper building around pool",new Vector3(section.center.x,-.9f,section.center.y),
                    new Vector3(section.width,1.4f,section.height),Edge);
            for(int storey=0;storey<7;storey++)
            {
                float y=-2.4f-storey*3.35f;
                Box(shell,"Floor band",new Vector3(0,y,0),new Vector3(37.8f,.25f,43.8f),Concrete);
                for(int bay=0;bay<7;bay++)foreach(float side in new[]{-1f,1f})
                {
                    Box(shell,"Occupied balcony bay",new Vector3(side*18.83f,y-1.65f,-14.4f+bay*4.8f),new Vector3(.12f,1.55f,2.55f),Glass);
                    Box(shell,"Balcony sill",new Vector3(side*19.06f,y-2.43f,-14.4f+bay*4.8f),new Vector3(.66f,.18f,2.95f),Concrete);
                }
                for(int bay=0;bay<5;bay++)foreach(float side in new[]{-1f,1f})
                    Box(shell,"End facade window",new Vector3((bay-2)*4.9f,y-1.55f,side*21.79f),new Vector3(2.75f,1.65f,.10f),Glass);
            }
            Bake(shell,"CondoShell");
            Rail(dressing,"West parapet",new Vector3(-18.8f,.1f,0),44,true);
            Rail(dressing,"East parapet",new Vector3(18.8f,.1f,0),44,true);
            Rail(dressing,"North parapet",new Vector3(0,.1f,21.8f),38,false);
            Rail(dressing,"South parapet",new Vector3(0,.1f,-21.8f),38,false);
            // Parent the basin to the structure that supports it. The geometry
            // gate can then inspect that parent's actual lower support triangles.
            Pool(shell);Shade(dressing);Stairhead(dressing);Laundry(dressing);
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
            var m=Mat("Roof paving",Color.white);m.CopyPropertiesFromMaterial(material);
            m.mainTextureScale=Vector2.one;m.SetTextureScale("_BumpMap",Vector2.one);EditorUtility.SetDirty(m);
            int part=0;
            foreach(var section in RoofSections(RooftopRecovery.HalfX,RooftopRecovery.HalfZ))
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Roof paving";quad.transform.SetParent(root,false);
                quad.transform.localPosition=new Vector3(section.center.x,.101f,section.center.y);
                quad.transform.localRotation=Quaternion.Euler(90,0,0);quad.transform.localScale=new Vector3(section.width,section.height,1);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                var filter=quad.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);
                var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++){var point=quad.transform.TransformPoint(vertices[i]);uv[i]=new Vector2(point.x,point.z)/6;}
                mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=SaveMesh(mesh,"RoofPavingPart"+part++);
                quad.GetComponent<Renderer>().sharedMaterial=m;quad.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
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
            Vector3 size=alongZ?new Vector3(.28f,.18f,length):new Vector3(length,.18f,.28f);
            Box(group,"Low concrete kerb",new Vector3(0,.09f,0),size,Edge,true);
            size.y=.60f;size.x=alongZ?.12f:length;size.z=alongZ?length:.12f;
            // One simple collision surface; visible pickets remain lightweight.
            var collision=new GameObject("Rail collision").AddComponent<BoxCollider>();collision.transform.SetParent(group,false);
            collision.center=new Vector3(0,.46f,0);collision.size=size;
            for(int rail=0;rail<2;rail++)Box(group,"Horizontal rail",new Vector3(0,.32f+rail*.40f,0),
                alongZ?new Vector3(.085f,.085f,length):new Vector3(length,.085f,.085f),Steel);
            int posts=Mathf.CeilToInt(length/.68f);
            for(int i=0;i<=posts;i++)
            {
                float v=Mathf.Lerp(-length*.5f,length*.5f,i/(float)posts);
                Box(group,"Rail picket",new Vector3(alongZ?0:v,.46f,alongZ?v:0),new Vector3(.065f,.60f,.065f),Steel);
            }
            Bake(group,name.Replace(" ",""));
        }

        private static Rect[] RoofSections(float hx,float hz)=>new[]{
            Rect.MinMaxRect(-hx,-hz,RooftopPool.MinX,hz),
            Rect.MinMaxRect(RooftopPool.MaxX,-hz,hx,hz),
            Rect.MinMaxRect(RooftopPool.MinX,-hz,RooftopPool.MaxX,RooftopPool.MinZ),
            Rect.MinMaxRect(RooftopPool.MinX,RooftopPool.MaxZ,RooftopPool.MaxX,hz)};

        private static Mesh SaveMesh(Mesh mesh,string name)
        {
            string path=Folder+"/"+name+".asset";mesh.name=name;var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }

        private static void Pool(Transform root)
        {
            var group=Group(root,"Resident swimming pool");
            float x=(RooftopPool.MinX+RooftopPool.MaxX)*.5f,z=(RooftopPool.MinZ+RooftopPool.MaxZ)*.5f;
            float width=RooftopPool.MaxX-RooftopPool.MinX,length=RooftopPool.MaxZ-RooftopPool.MinZ;
            Box(group,"Pool floor",new Vector3(x,RooftopPool.FloorY-.1f,z),new Vector3(width,.2f,length),Tile,true);
            foreach(float edge in new[]{RooftopPool.MinX,RooftopPool.MaxX})
            {
                Box(group,"Pool side wall",new Vector3(edge,-.75f,z),new Vector3(.20f,1.70f,length),Tile,true);
                Box(group,"Pool coping",new Vector3(edge,.13f,z),new Vector3(.32f,.12f,length+.2f),Edge,true);
            }
            foreach(float edge in new[]{RooftopPool.MinZ,RooftopPool.MaxZ})
            {
                Box(group,"Pool end wall",new Vector3(x,-.75f,edge),new Vector3(width,1.70f,.20f),Tile,true);
                Box(group,"Pool coping",new Vector3(x,.13f,edge),new Vector3(width+.2f,.12f,.32f),Edge,true);
            }
            // Broad entry steps give swimmers a visible way back to the deck.
            for(int i=0;i<7;i++)
            {
                float top=.10f-i*.24f;float depth=top-RooftopPool.FloorY;
                Box(group,"Pool entry step "+i,new Vector3(RooftopPool.MaxX-1.4f,RooftopPool.FloorY+depth*.5f,RooftopPool.MinZ+.28f+i*.45f),
                    new Vector3(2.4f,depth,.46f),i%2==0?Edge:Tile,true);
            }
            var water=Mat("Pool water",new Color(.14f,.48f,.51f));
            water.shader=Shader.Find("TumbangPreso/RoofPoolWater");
            if(water.shader==null)throw new InvalidOperationException("Missing roof pool shader");
            water.SetColor("_Color",new Color(.14f,.48f,.51f,.68f));water.SetFloat("_Glossiness",.64f);EditorUtility.SetDirty(water);
            var surface=GameObject.CreatePrimitive(PrimitiveType.Quad);surface.name="Water surface";surface.transform.SetParent(group,false);
            surface.transform.position=new Vector3(x,RooftopPool.SurfaceY,z);surface.transform.rotation=Quaternion.Euler(90,0,0);
            surface.transform.localScale=new Vector3(width-.2f,length-.2f,1);Object.DestroyImmediate(surface.GetComponent<Collider>());
            surface.GetComponent<Renderer>().sharedMaterial=water;surface.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(surface,"Water surface contained by the physical pool basin. Body buoyancy is handled by RooftopPool, not a solid water collider.");
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
                Box(group,"Resident table",new Vector3(.15f,.81f,z),new Vector3(1.7f,.14f,.8f),Timber,true);
                foreach(float side in new[]{-1f,1f})Box(group,"Table leg",new Vector3(.15f+side*.6f,.39f,z),new Vector3(.14f,.78f,.55f),Timber,true);
                foreach(float side in new[]{-1f,1f})
                {
                    Box(group,"Bench seat",new Vector3(.15f,.43f,z+side*.8f),new Vector3(1.6f,.13f,.38f),Timber,true);
                    foreach(float x in new[]{-.5f,.5f})Box(group,"Bench leg",new Vector3(.15f+x,.185f,z+side*.8f),new Vector3(.12f,.37f,.3f),Timber,true);
                }
            }
            // The roof meets all four posts; table and bench tops meet their legs.
            // Bake this supported assembly, retaining its individual collision shapes.
            Bake(group,"ResidentShade");
        }

        private static void Stairhead(Transform root)
        {
            var group=Group(root,"Residential stairhead");group.localPosition=new Vector3(8,.1f,18.5f);
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
            ResidentLaundryAuthor.Place(group,"courtyard-line",new Vector3(-8.5f,2.7f,19.0f),0,.1f);
            Box(group,"Water tank plinth",new Vector3(-2.9f,.27f,19.5f),new Vector3(1.6f,.34f,1.6f),Concrete,true);
            var tank=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tank.name="Resident water tank";tank.transform.SetParent(group,false);
            tank.transform.localPosition=new Vector3(-2.9f,1.12f,19.5f);tank.transform.localScale=new Vector3(1.35f,.69f,1.35f);
            tank.GetComponent<Renderer>().sharedMaterial=Mat("Tank steel",new Color(.52f,.57f,.56f));
        }

        private static void Skyline(Transform root)
        {
            var city=Group(root,"Metro rooftops");
            var occupied=new List<Bounds>{new Bounds(new Vector3(0,-13,0),new Vector3(39,26,45))};
            var sites=new[]{new Vector3(38,36,4),new Vector3(-38,33,1),new Vector3(38,42,-39),new Vector3(-38,35,-39),
                new Vector3(38,38,39),new Vector3(-38,32,39),new Vector3(70,47,6),new Vector3(-70,41,11),
                new Vector3(5,46,74),new Vector3(-10,44,-74),new Vector3(66,37,67),new Vector3(-66,47,69),
                new Vector3(70,34,-66),new Vector3(-69,38,-70)};
            for(int i=0;i<sites.Length;i++)
            {
                string model=i==0?"building-m":i==3?"building-n":"building-skyscraper-"+(char)('a'+i%5);
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
                if(i==0||i==3)CityPalette(building,i%3);
                occupied.Add(DrawnBounds(building));
                building.isStatic=true;
            }
            var ground=Box(city,"Distant city ground",new Vector3(0,-26.45f,0),new Vector3(220,.8f,220),Concrete);
            ground.isStatic=true;
            var asphalt=Mat("City asphalt",new Color(.42f,.42f,.39f));
            asphalt.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/models/textures/asphalt.png");EditorUtility.SetDirty(asphalt);
            var roads=Group(city,"Connected city roads");var roadMeshes=new List<Mesh>();
            foreach(float x in new[]{-52f,-23.5f,23.5f,52f})
                CityRoad(roads,new Vector3(x,-26.025f,0),new Vector3(5.7f,.04f,190),asphalt,roadMeshes);
            foreach(float z in new[]{-52f,-26.5f,26.5f,52f})
                CityRoad(roads,new Vector3(0,-26.024f,z),new Vector3(190,.04f,5.7f),asphalt,roadMeshes);
            Bake(roads,"CityRoads");foreach(var mesh in roadMeshes)Object.DestroyImmediate(mesh);
            var houses=Group(city,"Street frontages");int houseIndex=0,attempt=0;
            // Buildings face actual streets, with a setback for the footpath.
            // All occupied footprints include the retained towers and this condo.
            foreach(float roadX in new[]{-52f,-23.5f,23.5f,52f})foreach(float side in new[]{-1f,1f})
            foreach(float z in new[]{-84f,-72f,-60f,-36f,-12f,0f,12f,36f,60f,72f,84f})
            {
                int index=attempt++;
                string type=new[]{"m","h","f","b","c","e"}[(index+Mathf.RoundToInt(Mathf.Abs(roadX)))%6];
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/commercial/building-"+type+".glb");
                if(asset==null)throw new InvalidOperationException("Missing retained city building "+type);
                var house=(GameObject)PrefabUtility.InstantiatePrefab(asset);
                house.transform.SetParent(houses,false);house.transform.localScale=Vector3.one*(7.2f+(index%3)*.6f);
                house.transform.localRotation=Quaternion.Euler(0,side<0?90:270,0);
                var b=DrawnBounds(house);
                float edge=roadX+side*4.15f;
                house.transform.position+=new Vector3(edge-(side<0?b.max.x:b.min.x),-26.049f-b.min.y,z-b.center.z);
                b=DrawnBounds(house);
                if(CityRoadOverlap(b)||occupied.Any(other=>FootprintsOverlap(other,b,.55f)))
                {Object.DestroyImmediate(house);continue;}
                house.name="StreetBuilding_"+houseIndex++;
                CityPalette(house,index%3);occupied.Add(b);house.isStatic=true;
                // A continuous kerb below the frontage connects its entrance to the road.
                Box(city,"Frontage footpath",new Vector3(roadX+side*3.53f,-26.0f,b.center.z),
                    new Vector3(1.34f,.10f,b.size.z+.3f),Concrete);
            }
            Debug.Log("[Sa Bubong] "+houseIndex+" measured street-front buildings around14retained towers; no footprint overlaps or road intrusions.");
            int trees=0;
            foreach(float x in new[]{-86f,-62f,-34f,34f,62f,86f})foreach(float z in new[]{-84f,-58f,-34f,0f,34f,58f,84f})
            {
                if((Mathf.RoundToInt(Mathf.Abs(x))+Mathf.RoundToInt(Mathf.Abs(z)))%3==0)continue;
                var footprint=new Bounds(new Vector3(x,-23,z),new Vector3(5,6,5));
                if(CityRoadOverlap(footprint)||occupied.Any(b=>FootprintsOverlap(b,footprint,.8f)))continue;
                MapFinalPassAuthor.PlaceTree(city,"CityGarden_"+trees,trees%2==0?"mango-yard":"narra-young",
                    new Vector3(x,-26.049f,z),5.5f+trees%3*.35f,4.8f,trees*61);
                occupied.Add(footprint);trees++;
            }
            Debug.Log("[Sa Bubong] "+trees+" small planted sites in clear city lots, away from roads and building footprints.");
        }

        private static void CityRoad(Transform parent,Vector3 at,Vector3 size,Material material,List<Mesh> meshes)
        {
            var road=Box(parent,"Asphalt street",at,size,material);
            var filter=road.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);
            var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++){var p=road.transform.TransformPoint(vertices[i]);uv[i]=new Vector2(p.x,p.z)/6;}
            mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=mesh;meshes.Add(mesh);
        }

        private static Bounds DrawnBounds(GameObject go)
        {var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
        private static bool FootprintsOverlap(Bounds a,Bounds b,float gap)
            =>a.min.x-gap<b.max.x&&a.max.x+gap>b.min.x&&a.min.z-gap<b.max.z&&a.max.z+gap>b.min.z;
        private static bool CityRoadOverlap(Bounds b)
            =>new[]{-52f,-23.5f,23.5f,52f}.Any(x=>b.min.x<x+2.9f&&b.max.x>x-2.9f)
              ||new[]{-52f,-26.5f,26.5f,52f}.Any(z=>b.min.z<z+2.9f&&b.max.z>z-2.9f);
        private static void CityPalette(GameObject go,int variant)
        {
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/models/kits/commercial/Textures/tumbang-warm-"+(char)('a'+variant)+".png");
            if(atlas==null)throw new InvalidOperationException("Missing established commercial colour atlas");
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
                {
                    string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                    string path=Folder+"/CityPalette_"+id+"_"+source.name.Replace("/","_")+"_"+variant+".mat";
                    var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(m==null){m=new Material(source);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(source);
                    foreach(string property in new[]{"baseColorTexture","_BaseMap","_MainTex"})
                        if(m.HasProperty(property))m.SetTexture(property,atlas);
                    EditorUtility.SetDirty(m);return m;
                }).ToArray();
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
                var x=Group(bounds,"Limit X "+side).gameObject.AddComponent<BoxCollider>();x.isTrigger=true;x.center=new Vector3(side*(RooftopRecovery.HalfX+1),5,0);x.size=new Vector3(.2f,30,50);
                var z=Group(bounds,"Limit Z "+side).gameObject.AddComponent<BoxCollider>();z.isTrigger=true;z.center=new Vector3(0,5,side*(RooftopRecovery.HalfZ+1));z.size=new Vector3(42,30,.2f);
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
