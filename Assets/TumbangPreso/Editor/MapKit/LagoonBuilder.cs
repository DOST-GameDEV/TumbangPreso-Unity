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
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // Selected composition C: a sporting deck inside a supported village loop.
    // Every repeatable authoring operation is scoped to this new scene/folder.
    public static partial class LagoonBuilder
    {
        public const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Lagoon.unity";
        private const string Folder = "Assets/TumbangPreso/Art/Lagoon";
        private static Material _wood, _fresh, _dark, _wet, _rope, _chalk, _steel, _glass;
        public static void Run() { Build(); EditorApplication.Exit(0); }
        public static void BuildExpansionPlayer()
        {
            Build();RosterBookBuilder.BuildFromMenu();AssetDatabase.SaveAssets();
            GameBuilder.BuildWindows();
        }
        public static void BuildAndReview()
        {
            bool savedOnly=Environment.GetCommandLineArgs().Contains("-tp-review-saved-scene");
            if(!savedOnly)Build();
            // A saved-only pass starts with fresh native mesh buffers and does
            // not regenerate, copy-serialize or mutate any authored mesh assets.
            if(!savedOnly)foreach(string path in Directory.GetFiles(Folder,"*.asset"))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            string output=Environment.GetEnvironmentVariable("TUMP_LAGOON_REVIEW")??"Logs/lagoon-authoring-v1";
            Directory.CreateDirectory(output);
            Object.FindFirstObjectByType<EnvColourPass>()?.Apply();
            // Posed resident art in these editor stills; gameplay is not running.
            foreach(var resident in Object.FindObjectsByType<LagoonResident>())
            {
                if(resident.Art?.Model==null)continue;
                var preview=Object.Instantiate(resident.Art.Model,resident.transform);preview.name="Editor resident witness";
                preview.transform.localScale=Vector3.one*2.38f;preview.transform.localRotation=Quaternion.Euler(0,CharacterVisual.PersonModelYaw,0);
                ToonSkin.Apply(preview,ToonSkin.PersonOutlineWidth,resident.Art.Palette);
                resident.Art.Clips.FirstOrDefault(c=>c!=null&&c.name=="idle")?.SampleAnimation(preview,resident.ReactionStyle*.37f);
            }
            foreach(var boat in Object.FindObjectsByType<MooredBoatMotion>())boat.SampleForAuthoring(0);
            var camera=new GameObject("Expansion art review").AddComponent<Camera>();
            camera.enabled=false;camera.nearClipPlane=.05f;camera.farClipPlane=380;camera.fieldOfView=65;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var outline=camera.gameObject.AddComponent<WorldOutline>();outline.PrototypeEnabled=true;
            bool deckStudy=Environment.GetCommandLineArgs().Contains("-tp-deck-study");
            var materials=Object.FindObjectsByType<Renderer>().SelectMany(r=>r.sharedMaterials)
                .Where(m=>m!=null&&m.HasProperty("_SurfaceStrength")).Distinct().ToArray();
            var strengths=materials.Select(m=>m.GetFloat("_SurfaceStrength")).ToArray();
            var lights=Object.FindObjectsByType<Light>();var shadows=lights.Select(l=>l.shadows).ToArray();
            var views=new[]{
                ("lagoon-overview",new Vector3(34,29,-39),new Vector3(0,0,3)),
                ("lagoon-court",new Vector3(0,1.65f,-11),new Vector3(0,1.6f,18)),
                ("lagoon-homes",new Vector3(17,1.65f,1),new Vector3(26,1.6f,10)),
                ("lagoon-water-entry",new Vector3(26,-.05f,2),new Vector3(21,-.5f,0)),
                ("lagoon-boat-landing",new Vector3(-4,1.8f,-23),new Vector3(-9,-.5f,-29))};
            if(deckStudy)
            {
                camera.fieldOfView=95;
                views=new[]{"deck-baseline","deck-no-detail","deck-no-outline","deck-no-shadows","deck-msaa4"}
                    .Select(name=>(name,new Vector3(0,1.286f,-8.2f),new Vector3(0,1.286f,10))).ToArray();
            }
            int width=deckStudy?1280:1440,height=deckStudy?720:900;
            try
            {
            foreach(var view in views)
            {
                if(deckStudy)
                {
                    for(int i=0;i<materials.Length;i++)materials[i].SetFloat("_SurfaceStrength",view.Item1=="deck-no-detail"?0:strengths[i]);
                    for(int i=0;i<lights.Length;i++)lights[i].shadows=view.Item1=="deck-no-shadows"?LightShadows.None:shadows[i];
                    outline.PrototypeEnabled=view.Item1!="deck-no-outline";
                }
                camera.transform.SetPositionAndRotation(view.Item2,Quaternion.LookRotation(view.Item3-view.Item2));
                int samples=!deckStudy||view.Item1=="deck-msaa4"?4:1;camera.allowMSAA=samples>1;
                var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf){antiAliasing=samples};rt.Create();
                camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;
                var display=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                bool write=GL.sRGBWrite;GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Graphics.Blit(rt,display);GL.sRGBWrite=write;RenderTexture.active=display;
                var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(output+"/"+view.Item1+".png",image.EncodeToPNG());
                RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(display);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            }
            }
            finally
            {
                for(int i=0;i<materials.Length;i++)materials[i].SetFloat("_SurfaceStrength",strengths[i]);
                for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];
            }
            Object.DestroyImmediate(camera.gameObject);
            File.WriteAllLines(output+"/scene-dependencies.txt",AssetDatabase.GetDependencies(ScenePath,true));
            if(Environment.GetCommandLineArgs().Contains("-tp-map-art-only")){EditorApplication.Exit(0);return;}
            RosterBookBuilder.BuildFromMenu();
            RafiPortraitAuthor.Build();
            HeroTurnaroundProbe.RunOne();
        }
        public static void Build()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            _wood=Mat("Seasoned timber",new Color(.46f,.33f,.21f));
            _fresh=Mat("Fresh repair wood",new Color(.55f,.41f,.27f));
            _dark=Mat("Dark structural timber",new Color(.30f,.235f,.17f));
            _wet=Mat("Wet lower timber piles",new Color(.22f,.25f,.20f));
            _rope=Mat("Plain rope bindings",new Color(.65f,.58f,.41f));
            _chalk=Mat("Court chalk",new Color(.92f,.88f,.71f));
            _steel=Mat("Galvanised steel",new Color(.48f,.54f,.54f));
            _glass=Mat("Shaded window glass",new Color(.14f,.24f,.27f));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Lagoon").transform; root.gameObject.AddComponent<MapGrade>();
            root.gameObject.AddComponent<EnvColourPass>(); var water=root.gameObject.AddComponent<LagoonWater>();
            var floor=Group(root,"Floor"); Deck(floor,"Community sporting deck",Vector3.zero,28,26);
            var paths=Group(root,"Village circulation");
            Deck(paths,"West promenade",new Vector3(-20,0,0),3.5f,43.5f);
            Deck(paths,"East promenade",new Vector3(20,0,0),3.5f,43.5f);
            Deck(paths,"North promenade",new Vector3(0,0,20),36.5f,3.5f);
            Deck(paths,"South promenade",new Vector3(0,0,-20),36.5f,3.5f);
            Deck(paths,"West court bridge",new Vector3(-16.1f,0,0),4.3f,4);
            Deck(paths,"East court bridge",new Vector3(16.1f,0,0),4.3f,4);
            Deck(paths,"North court bridge",new Vector3(0,0,15.6f),4,5.3f);
            Deck(paths,"South court bridge",new Vector3(0,0,-15.6f),4,5.3f);
            var rails=Group(root,"Court and bridge handrails");
            foreach(float sign in new[]{-1f,1f})
            {
                Rail(rails,new Vector3(sign*14,0,7.5f),11,true);
                Rail(rails,new Vector3(sign*14,0,-7.5f),11,true);
                Rail(rails,new Vector3(8,0,sign*13),12,false);
                Rail(rails,new Vector3(-8,0,sign*13),12,false);
                Rail(rails,new Vector3(sign*16.1f-1.85f,0,2),.60f,false);
                Rail(rails,new Vector3(sign*16.1f+1.85f,0,2),.60f,false);
                Rail(rails,new Vector3(sign*16.1f,0,-2),4.3f,false);
                Rail(rails,new Vector3(2,0,sign*15.6f),5.3f,true);
                Rail(rails,new Vector3(-2,0,sign*15.6f),5.3f,true);
            }
            // Continuous routes keep generous openings at bridges, homes and stairs.
            foreach(float sign in new[]{-1f,1f})
            {
                Rail(rails,new Vector3(sign*18.25f,0,10.1f),16.2f,true);
                Rail(rails,new Vector3(sign*18.25f,0,-10.1f),16.2f,true);
                foreach(var segment in new[]{new Vector2(-21.75f,-14.9f),new Vector2(-7.1f,-1.6f),new Vector2(1.6f,6.1f),new Vector2(13.9f,21.75f)})
                    Rail(rails,new Vector3(sign*21.75f,0,(segment.x+segment.y)*.5f),segment.y-segment.x,true);
                Rail(rails,new Vector3(sign*10.1f,0,18.25f),16.2f,false);
                Rail(rails,new Vector3(sign*10.1f,0,-18.25f),16.2f,false);
            }
            foreach(var segment in new[]{new Vector2(-18.25f,-12.9f),new Vector2(-5.1f,5.1f),new Vector2(12.9f,18.25f)})
                Rail(rails,new Vector3((segment.x+segment.y)*.5f,0,21.75f),segment.y-segment.x,false);
            foreach(var segment in new[]{new Vector2(-18.25f,-10.6f),new Vector2(-7.4f,18.25f)})
                Rail(rails,new Vector3((segment.x+segment.y)*.5f,0,-21.75f),segment.y-segment.x,false);
            var homes=Group(root,"Supported homes");
            Village(homes,root);
            Stairs(root,new Vector3(-16.1f,0,2.18f),180);
            Stairs(root,new Vector3(16.1f,0,2.18f),180);
            Stairs(root,new Vector3(21.65f,0,0),-90);
            Stairs(root,new Vector3(-21.65f,0,0),90);
            var landing=Deck(root,"Boat landing",new Vector3(-9,-.55f,-25),8,4);
            for(int step=0;step<3;step++)Box(root,"Landing step",new Vector3(-9,-.1f-step*.18f,-22-step*.42f),new Vector3(3,.22f,.45f),_wood,true);
            VillageBoats(root);
            Water(root,water); Horizon(root); Gameplay(root);
            PlaceVillageResidents(root);
            var sun=new GameObject("Lagoon sun").AddComponent<Light>();sun.transform.SetParent(root,false);sun.type=LightType.Directional;
            MapAtmosphereAuthor.Apply("Lagoon");
            RenderSettings.fogColor=new Color(.55f,.72f,.77f);RenderSettings.fogStartDistance=105;RenderSettings.fogEndDistance=270;
            RenderSettings.ambientSkyColor=new Color(.61f,.74f,.80f);RenderSettings.ambientEquatorColor=new Color(.51f,.60f,.58f);
            RenderSettings.ambientGroundColor=new Color(.27f,.31f,.30f);
            var sky=RenderSettings.skybox;sky.SetColor("_Zenith",new Color(.29f,.52f,.71f));sky.SetColor("_Horizon",new Color(.66f,.79f,.83f));
            sky.SetFloat("_CloudOpacity",.79f);sky.SetFloat("_CloudSpeed",.038f/360);EditorUtility.SetDirty(sky);
            var report=new System.Text.StringBuilder();
            MapSurfaceAuthor.FinishLoadedScene("Lagoon",report);
            // Finish construction-specific surfaces before combining by material.
            // The custom water, resident models and moving laundry stay separate.
            foreach(var group in new[]{floor,paths,rails}) Bake(group,group.name.Replace(" ",""));
            for(int i=0;i<VillageBatches.Count;i++)Bake(VillageBatches[i],"VillageBatch"+i);
            int stair=0;
            foreach(Transform part in root)
                if(part.name=="Broad water access steps")Bake(part,"WaterStairs"+stair++);
            foreach(Transform island in root.Find("Seeded island and mountain background"))Bake(island,island.name.Replace(" ",""));
            LagoonBirdAuthor.FinishLoadedScene(report);
            LagoonGableFinishAuthor.FinishLoadedScene(report);
            LagoonHipFinishAuthor.FinishLoadedScene(report);
            LagoonVerandaFinishAuthor.FinishLoadedScene(report);
            LagoonMetalFinishAuthor.FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))
                EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            string output=Environment.GetEnvironmentVariable("TUMP_LAGOON_REVIEW")??"Logs/lagoon-authoring-v1";Directory.CreateDirectory(output);File.WriteAllText(output+"/surfaces.txt",report.ToString());
            Debug.Log("[Lagoon] Generated supported court, loop, eight connected homes, ten detached homes, six watercraft and water access. Native art/route review remains required.");
        }

        private static Transform Deck(Transform parent,string name,Vector3 at,float width,float depth)
        {
            var deck=Group(parent,name);deck.localPosition=at;
            Box(deck,"Structural deck slab",new Vector3(0,-.10f,0),new Vector3(width,.20f,depth),_dark);
            // Collision meets the boards; its invisible plane must not be a
            // second rendered surface coplanar with their top faces.
            var walking=Group(deck,"Continuous deck collision").gameObject.AddComponent<BoxCollider>();
            walking.center=new Vector3(0,-.064f,0);walking.size=new Vector3(width,.20f,depth);
            // Staggered board lengths, rather than one giant stretched wood texture.
            int rows=Mathf.CeilToInt(depth/.38f);float board=depth/rows;
            for(int z=0;z<rows;z++)
            {
                int pieces=Mathf.Max(1,Mathf.CeilToInt(width/5));
                float length=width/pieces;
                for(int x=0;x<pieces+(z%2);x++)
                {
                    float left=Mathf.Max(-width*.5f,-width*.5f+(x-(z%2)*.5f)*length);
                    float right=Mathf.Min(width*.5f,-width*.5f+(x+1-(z%2)*.5f)*length);
                    Box(deck,"Deck board",new Vector3((left+right)*.5f,.018f,-depth*.5f+(z+.5f)*board),
                        new Vector3(right-left-.018f,.036f,board-.013f),((x*37+z*19+z*z)%47)==0?_fresh:_wood);
                }
            }
            int px=Mathf.Max(1,Mathf.CeilToInt(width/5)),pz=Mathf.Max(1,Mathf.CeilToInt(depth/5));
            for(int x=0;x<=px;x++)for(int z=0;z<=pz;z++)
            {
                float xx=Mathf.Lerp(-width*.5f+.18f,width*.5f-.18f,x/(float)px);
                float zz=Mathf.Lerp(-depth*.5f+.18f,depth*.5f-.18f,z/(float)pz);
                float height=deck.position.y-LagoonWater.FloorY;
                if(name.Contains("household"))Pole(deck,"Household pile to waterbed",new Vector3(xx,-height*.5f,zz),.09f,height,_dark,true);
                else Box(deck,"Timber pile to waterbed",new Vector3(xx,-height*.5f,zz),new Vector3(.26f,height,.26f),_dark,true);
                if(name.Contains("household"))Pole(deck,"Tide-darkened pile",new Vector3(xx,LagoonWater.SurfaceY-deck.position.y-.46f,zz),.094f,.92f,_wet,false);
                else Box(deck,"Damp lower pile",new Vector3(xx,LagoonWater.SurfaceY-deck.position.y-.46f,zz),new Vector3(.272f,.92f,.272f),_wet);
            }
            for(int z=0;z<=pz;z++)
                Box(deck,"Load-bearing crossbeam",new Vector3(0,-.28f,Mathf.Lerp(-depth*.5f+.18f,depth*.5f-.18f,z/(float)pz)),new Vector3(width,.26f,.22f),_dark);
            return deck;
        }
        private static void Rail(Transform parent,Vector3 at,float length,bool alongZ)
        {
            var rail=Group(parent,"Supported handrail");rail.localPosition=at;
            int count=Mathf.CeilToInt(length/1.8f);
            for(int i=0;i<=count;i++)
            {
                float v=Mathf.Lerp(-length*.5f,length*.5f,i/(float)count);
                Box(rail,"Rail post",new Vector3(alongZ?0:v,.45f,alongZ?v:0),new Vector3(.13f,.9f,.13f),_dark);
            }
            for(int i=0;i<2;i++)Box(rail,"Horizontal timber rail",new Vector3(0,.36f+i*.43f,0),
                alongZ?new Vector3(.09f,.09f,length):new Vector3(length,.09f,.09f),_wood);
            var shape=rail.gameObject.AddComponent<BoxCollider>();shape.center=Vector3.up*.43f;
            shape.size=alongZ?new Vector3(.12f,.86f,length):new Vector3(length,.86f,.12f);
        }
        private static void Gable(Transform parent,int index,Material material,float front)
        {
            var go=Group(parent,"Ventilated timber gable");
            var mesh=new Mesh{name="HomeGable"+index};
            mesh.vertices=new[]{new Vector3(-3.3f,2.76f,front),new Vector3(0,3.94f,front),new Vector3(3.3f,2.76f,front),
                new Vector3(-3.3f,2.76f,3.2f),new Vector3(0,3.94f,3.2f),new Vector3(3.3f,2.76f,3.2f)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.zero,Vector2.up,Vector2.right};
            mesh.triangles=new[]{0,1,2,3,5,4};mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"HomeGable"+index);go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        private static void Stairs(Transform parent,Vector3 at,float yaw)
        {
            var stair=Group(parent,"Broad water access steps");stair.localPosition=at;stair.localRotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<12;i++)
            {
                float top=-i*.18f;
                Box(stair,"Water step",new Vector3(0,top-.09f,-i*.43f),new Vector3(3,.18f,.46f),i>6?_wet:_wood,true);
                foreach(float side in new[]{-1f,1f})
                {
                    Box(stair,"Step support to bed",new Vector3(side*1.30f,(top+LagoonWater.FloorY)*.5f,-i*.43f),new Vector3(.12f,top-LagoonWater.FloorY,.12f),_dark);
                    if(i%3==0)Box(stair,"Handrail post",new Vector3(side*1.4f,top+.35f,-i*.43f),new Vector3(.10f,.9f,.10f),_wood);
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                var rail=Box(stair,"Sloping handrail",new Vector3(side*1.4f,-.36f,-2.35f),new Vector3(.09f,.09f,5.3f),_wood);
                rail.transform.localRotation=Quaternion.Euler(-22.7f,0,0);
            }
        }
        private static void Plant(Transform parent,Vector3 at,int index)
        {
            string[] ids={"terracotta-broadleaf","pail-sansevieria","small-aloe"};
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/SaBubong/Residents/"+ids[index%3]+".glb");
            if(asset==null)throw new InvalidOperationException("Missing retained plant");
            var plant=(GameObject)PrefabUtility.InstantiatePrefab(asset);plant.transform.SetParent(parent,false);plant.transform.position=at;
            float floor=plant.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);plant.transform.position+=Vector3.up*(at.y-floor);
        }
        private static void Water(Transform root,LagoonWater controller)
        {
            var bed=Mat("Pale lagoon sand",new Color(.56f,.58f,.44f));bed.shader=Shader.Find("TumbangPreso/LagoonBed");
            if(bed.shader==null)throw new InvalidOperationException("Lagoon bed shader missing");EditorUtility.SetDirty(bed);
            Box(root,"Sandy lagoon bed",new Vector3(0,LagoonWater.FloorY-.2f,0),new Vector3(180,.4f,180),bed,true);
            var surface=GameObject.CreatePrimitive(PrimitiveType.Quad);surface.name="Moving lagoon surface";surface.transform.SetParent(root,false);
            surface.transform.position=new Vector3(0,LagoonWater.SurfaceY,0);surface.transform.rotation=Quaternion.Euler(90,0,0);surface.transform.localScale=new Vector3(1600,1600,1);
            Object.DestroyImmediate(surface.GetComponent<Collider>());var material=Mat("Lagoon water",new Color(.045f,.46f,.53f,.96f));
            material.shader=Shader.Find("TumbangPreso/LagoonWater");if(material.shader==null)throw new InvalidOperationException("Lagoon water shader missing");
            EditorUtility.SetDirty(material);surface.GetComponent<Renderer>().sharedMaterial=material;surface.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            controller.SetSurface(surface.GetComponent<Renderer>());
            AirborneByDesign.Attach(surface,"Visual water plane; shared swimming buoyancy uses the real surface height.");
        }
        private static void Horizon(Transform root)
        {
            var horizon=Group(root,"Seeded island and mountain background");
            // Fixed randomness: the owner asked for varied islands, while peers,
            // previews and rebuilds must see exactly the same geography.
            var rng=new System.Random(20260922);
            var islands=new[]{new Vector3(-92,0,54),new Vector3(-73,0,76),new Vector3(-53,0,88),
                new Vector3(-8,0,96),new Vector3(27,0,91),new Vector3(59,0,80),new Vector3(90,0,61),
                new Vector3(105,0,21),new Vector3(109,0,-33),new Vector3(-119,0,-26),new Vector3(-101,0,-59)};
            for(int i=0;i<islands.Length;i++)
            {
                var at=islands[i]+new Vector3((float)rng.NextDouble()*9-4,0,(float)rng.NextDouble()*11-5);
                Island(horizon,i,at,13+(float)rng.NextDouble()*23,9+(float)rng.NextDouble()*16,4+(float)rng.NextDouble()*12,rng.Next(),false);
            }
            for(int i=0;i<9;i++)
            {
                float angle=(-78+i*20f+(float)rng.NextDouble()*8)*Mathf.Deg2Rad;
                float distance=172+(float)rng.NextDouble()*45;
                Island(horizon,30+i,new Vector3(Mathf.Sin(angle)*distance,0,Mathf.Cos(angle)*distance),
                    32+(float)rng.NextDouble()*23,22+(float)rng.NextDouble()*18,21+(float)rng.NextDouble()*27,rng.Next(),true);
            }
        }

        private static void Island(Transform parent,int id,Vector3 at,float width,float depth,float height,int seed,bool distant)
        {
            var rng=new System.Random(seed);
            var island=Group(parent,(distant?"Mountain island ":"Coastal island ")+id);island.localPosition=at;
            island.localRotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
            const int sectors=24,rings=5;
            float phase=(float)rng.NextDouble()*6.283f;
            var peaks=new Vector2[3];
            for(int p=0;p<3;p++)peaks[p]=new Vector2(((float)rng.NextDouble()-.5f)*1.2f,((float)rng.NextDouble()-.5f)*.9f);
            float Height(float x,float z,float radius)
            {
                float ridge=0;
                for(int p=0;p<3;p++)
                {
                    var delta=new Vector2(x,z)-peaks[p];
                    float peak=Mathf.Exp(-(delta.x*delta.x*3.1f+delta.y*delta.y*5.2f));
                    ridge=Mathf.Max(ridge,peak*(p==0?1:.70f+p*.10f));
                }
                float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((1-radius)*3));
                return LagoonWater.SurfaceY-.12f+height*(.10f+ridge*.9f)*edge;
            }
            var vertices=new Vector3[1+rings*sectors];vertices[0]=new Vector3(0,Height(0,0,0),0);
            for(int ring=1;ring<=rings;ring++)for(int j=0;j<sectors;j++)
            {
                float a=j*Mathf.PI*2/sectors,r=ring/(float)rings;
                float coast=1+.13f*Mathf.Sin(a*3+phase)+.06f*Mathf.Cos(a*5-phase);
                float x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r;
                vertices[1+(ring-1)*sectors+j]=new Vector3(x*width*coast,Height(x,z,r),z*depth*coast);
            }
            var land=new List<int>();var shore=new List<int>();
            for(int j=0;j<sectors;j++)land.AddRange(new[]{0,1+(j+1)%sectors,1+j});
            for(int ring=1;ring<rings;ring++)for(int j=0;j<sectors;j++)
            {
                int a=1+(ring-1)*sectors+j,b=1+(ring-1)*sectors+(j+1)%sectors,c=a+sectors,d=b+sectors;
                var triangles=ring==rings-1?shore:land;triangles.AddRange(new[]{a,b,c,b,d,c});
            }
            var mesh=new Mesh{name="IslandTerrain"+id};mesh.vertices=vertices;mesh.subMeshCount=2;
            mesh.SetTriangles(land,0);mesh.SetTriangles(shore,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            // Broad planes and real volume, not a textured cardboard ridge.
            island.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"IslandTerrain"+id);
            island.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{
                Mat((distant?"Distant mountain haze ":"Coastal colour silhouette ")+id,
                    distant?new Color(.36f+id%3*.025f,.48f+id%2*.025f,.53f):new Color(.27f+id%3*.025f,.40f+id%2*.03f,.28f)),
                Mat((distant?"Distant shore haze ":"Rocky sandy shore ")+id,
                    distant?new Color(.39f,.51f,.55f):new Color(.53f,.54f,.42f))};
            AirborneByDesign.Attach(island.gameObject,"Distant terrain footprint reaches below the water surface; decorative and outside playable bounds.");
            if(!distant)
            {
                for(int t=0;t<5;t++)
                {
                    int vertex=1+sectors*(1+rng.Next(3))+rng.Next(sectors);
                    Palm(island,vertices[vertex],id*5+t,4.3f+(float)rng.NextDouble()*2.7f);
                }
            }
        }
        private static void Palm(Transform parent,Vector3 at,int id,float height)
        {
            var palm=Group(parent,"Shore palm "+id);palm.localPosition=at;
            var bark=Mat("Palm bark",new Color(.40f,.33f,.24f));
            var leaf=Mat("Palm foliage",new Color(.24f,.38f,.23f));
            for(int part=0;part<3;part++)
            {
                var trunk=Box(palm,"Leaning palm trunk",new Vector3(part*.14f,height*(part+.5f)/3,0),new Vector3(.32f,height/3+.05f,.32f),bark);
                trunk.transform.localRotation=Quaternion.Euler(0,0,-4);
            }
            string asset=Folder+"/PalmLeaf.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
            if(mesh==null)
            {
                var front=new[]{new Vector3(0,0,0),new Vector3(-.30f,.26f,.95f),new Vector3(-.20f,.10f,2.0f),
                    new Vector3(0,-.48f,3.1f),new Vector3(.20f,.10f,2.0f),new Vector3(.30f,.26f,.95f)};
                var vertices=front.Concat(front).ToArray();var triangles=new[]{0,2,1,0,3,2,0,4,3,0,5,4,6,7,8,6,8,9,6,9,10,6,10,11};
                mesh=new Mesh{name="PalmLeaf"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh,"PalmLeaf");
            }
            for(int i=0;i<7;i++)
            {
                var frond=Group(palm,"Arching palm frond");frond.localPosition=new Vector3(.32f,height,0);frond.localRotation=Quaternion.Euler(i%2*8,i*360/7f+id*17,0);
                frond.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;frond.gameObject.AddComponent<MeshRenderer>().sharedMaterial=leaf;
            }
        }
        private static void Gameplay(Transform root)
        {
            Group(root,"~Match").gameObject.AddComponent<MatchInstaller>();
            var kill=Group(root,"KillPlane");kill.localPosition=Vector3.down*9;kill.gameObject.AddComponent<KillPlane>();
            var shape=kill.gameObject.AddComponent<BoxCollider>();shape.isTrigger=true;shape.size=new Vector3(300,2,300);
            var bounds=Group(root,"Bounds");
            foreach(float sign in new[]{-1f,1f})
            {
                var x=Group(bounds,"Lagoon X limit "+sign).gameObject.AddComponent<BoxCollider>();x.isTrigger=true;x.center=new Vector3(sign*(LagoonWater.Limit+.1f),5,0);x.size=new Vector3(.2f,25,90);
                var z=Group(bounds,"Lagoon Z limit "+sign).gameObject.AddComponent<BoxCollider>();z.isTrigger=true;z.center=new Vector3(0,5,sign*(LagoonWater.Limit+.1f));z.size=new Vector3(90,25,.2f);
            }
            var chalk=Group(root,"Chalk");float r=Balance.ConfinementRadius;
            foreach(float side in new[]{-1f,1f})
            {
                Box(chalk,"Court X",new Vector3(side*r,.043f,0),new Vector3(.09f,.012f,r*2),_chalk);
                Box(chalk,"Court Z",new Vector3(0,.043f,side*r),new Vector3(r*2,.012f,.09f),_chalk);
                Box(chalk,"Throwing line",new Vector3(0,.043f,side*Confinement.ThrowingLine()),new Vector3(14,.012f,.07f),_chalk);
            }
            var spawns=Group(root,"SpawnPoints");Group(spawns,"Spawn0").localPosition=new Vector3(0,.04f,0);
            for(int i=1;i<4;i++)Group(spawns,"Spawn"+i).localPosition=new Vector3((i-2)*3,.04f,-Confinement.AttackerSpawnRing());
        }
        private static Transform Group(Transform parent,string name)
        {var result=new GameObject(name).transform;result.SetParent(parent,false);return result;}
        private static GameObject Box(Transform parent,string name,Vector3 at,Vector3 size,Material mat,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static Material Mat(string name,Color colour)
        {
            string path=Folder+"/"+name.Replace(" ","")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=colour;mat.SetFloat("_Glossiness",.14f);EditorUtility.SetDirty(mat);return mat;
        }
        private static Mesh Save(Mesh source,string name)
        {
            if(source.vertexCount==0||!float.IsFinite(source.bounds.center.sqrMagnitude)||!float.IsFinite(source.bounds.extents.sqrMagnitude)
                ||source.vertices.Any(v=>!float.IsFinite(v.sqrMagnitude)))throw new InvalidOperationException("Non-finite/empty authored mesh: "+name);
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(source,path);return source;}
            EditorUtility.CopySerialized(source,saved);Object.DestroyImmediate(source);EditorUtility.SetDirty(saved);return saved;
        }
        private static void Bake(Transform root,string name)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()!=null).ToArray();
            // Keep horizontal planks thin after batching. Combining them with
            // three-metre piles made WorldOutline treat every tiny board gap as
            // a structural edge, producing noisy dashes at ordinary player eyes.
            var boards=filters.Where(f=>f.name=="Deck board").ToArray();
            int plane=0;
            foreach(var group in boards.GroupBy(f=>Mathf.RoundToInt(f.transform.position.y*1000)))
                BakeParts(Group(root,"Thin deck surface "+plane),group.ToArray(),name+"Boards"+plane++);
            BakeParts(root,filters.Except(boards).ToArray(),name);
        }
        private static void BakeParts(Transform root,MeshFilter[] filters,string name)
        {
            if(filters.Length==0)return;
            var groups=filters.SelectMany(f=>f.GetComponent<MeshRenderer>().sharedMaterials
                .Select((material,submesh)=>(filter:f,material,submesh)).Where(part=>part.submesh<f.sharedMesh.subMeshCount))
                .GroupBy(part=>part.material).ToArray();
            var meshes=new List<Mesh>();var parts=new List<CombineInstance>();
            foreach(var group in groups)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Select(part=>new CombineInstance{mesh=part.filter.sharedMesh,subMeshIndex=part.submesh,transform=root.worldToLocalMatrix*part.filter.transform.localToWorldMatrix}).ToArray(),true,true);
                meshes.Add(mesh);parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});
            }
            var final=new Mesh{name=name,indexFormat=IndexFormat.UInt32};final.CombineMeshes(parts.ToArray(),false,true);
            foreach(var filter in filters){Object.DestroyImmediate(filter.GetComponent<Renderer>());Object.DestroyImmediate(filter);}
            foreach(var mesh in meshes)Object.DestroyImmediate(mesh);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(final,name);
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterials=groups.Select(g=>g.Key).ToArray();
        }
    }
}
