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
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Bayan town continuation, with planted plots and a lower open settlement.</summary>
    public static class BayanTownContextAuthor
    {
        private const string Folder = "Assets/TumbangPreso/Art/BayanTownContext";
        private const int Tile = 64, Pad = 2, Cell = Tile + Pad * 2, AtlasWidth = Cell * 6, AtlasHeight = AtlasWidth + 8;
        private static readonly Color32[] Extra =
        {
            new Color32(80,101,57,255), new Color32(101,117,67,255), new Color32(65,84,48,255),
            new Color32(89,68,46,255), new Color32(145,139,120,255), new Color32(72,70,63,255),
            new Color32(158,151,130,255),
        };
        private sealed class Template
        {
            public Mesh Mesh;
            public Vector3 Scale, Size, Centre;
        }
        private sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Indices = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv)
            {
                int n = Vertices.Count; Vertices.AddRange(new[] { a, b, c, d });
                Uvs.AddRange(new[] { uv, uv, uv, uv }); Indices.AddRange(new[] { n, n+1, n+2, n, n+2, n+3 });
            }
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uv)
            {
                int n = Vertices.Count; Vertices.AddRange(new[] { a, b, c });
                Uvs.AddRange(new[] { uv, uv, uv }); Indices.AddRange(new[] { n, n+1, n+2 });
            }
            public void Box(Vector3 at, Vector3 size, Vector2 uv)
            {
                Vector3 a = at-size*.5f, b = at+size*.5f;
                Quad(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),uv);
                Quad(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),uv);
                Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),uv);
                Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),uv);
                Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),uv);
                Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),uv);
            }
            public Mesh BuildMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (Vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(Vertices); mesh.SetUVs(0,Uvs); mesh.SetTriangles(Indices,0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }

        [Serializable] private sealed class Block { public string id;public float x,z;public bool distant;public int gardenSlot; }
        [Serializable] private sealed class Road { public float x0,z0,x1,z1,width; }
        [Serializable] private sealed class Plan { public float retainedHalfExtent,groundHalfExtent,roadY,yardY,columnOffset;public int slotsPerBlock;public Block[] blocks;public Road[] roads; }
        private const string RootName="BayanTownContext";
        private static Plan ReadPlan()=>JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/bayan-town-context-20260924.json"));
        public static void ClearPrevious(string map)
        {
            if(map!="BayanPlaza")return;
            var old=GameObject.Find("BayanPlaza/Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/bayan-context");File.WriteAllText("Logs/bayan-context/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious("BayanPlaza");var map=GameObject.Find("BayanPlaza");var plan=ReadPlan();
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var templates=new Dictionary<char,Template>();Material sourceMaterial=null;
            foreach(var filter in map.transform.Find("Dressing/Bahay").GetComponentsInChildren<MeshFilter>())
            {
                var raw=MapSurfaceAuthor.SourceMeshForAuthoring(filter.sharedMesh);
                string path=AssetDatabase.GetAssetPath(raw),name=Path.GetFileNameWithoutExtension(path);
                if(!name.StartsWith("building-type-"))continue;
                char kind=name[name.Length-1];if(!"aceo".Contains(kind)||templates.ContainsKey(kind))continue;
                var scale=filter.transform.lossyScale;
                templates.Add(kind,new Template{Mesh=filter.sharedMesh,Scale=scale,
                    Size=Vector3.Scale(filter.sharedMesh.bounds.size,scale),Centre=Vector3.Scale(filter.sharedMesh.bounds.center,scale)});
                sourceMaterial??=filter.GetComponent<MeshRenderer>().sharedMaterial;
            }
            if(templates.Count!=4)throw new InvalidOperationException("Expected Bayan's retained a/c/e/o building families.");
            Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();
            var atlas=BuildAtlas(out Vector2 wall,out Vector2 roof,out Vector2 window,out Vector2 trim);
            var draft=new Material(sourceMaterial){name="Bayan distant town",mainTexture=atlas,color=Color.white};
            draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;
            draft.SetOverrideTag("TumpSurfaceSource","");draft.SetFloat("_SurfaceStrength",0);
            string materialPath=Folder+"/BayanTown.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){AssetDatabase.CreateAsset(draft,materialPath);material=draft;}
            else{EditorUtility.CopySerialized(draft,material);Object.DestroyImmediate(draft);EditorUtility.SetDirty(material);}
            var root=new GameObject(RootName).transform;root.SetParent(map.transform.Find("Dressing"),false);
            int full=0,proxy=0,gardens=0,vertices=0,index=0;var occupied=new List<Bounds>();
            foreach(var block in plan.blocks)
            {
                var g=new Geometry();var combine=new List<CombineInstance>();var temporary=new List<Mesh>();
                for(int slot=0;slot<plan.slotsPerBlock;slot++)
                {
                    float side=slot<2?-1:1,x=slot%2==0?-plan.columnOffset:plan.columnOffset;
                    if(slot==block.gardenSlot)
                    {
                        Ground(g,new Vector3(x,plan.yardY+.004f,side*10),new Vector2(8.5f,8.5f),ExtraUv(1));
                        Tree(g,new Vector3(x,.102f,side*10),5.4f+(index%3)*.5f);gardens++;continue;
                    }
                    char family="aceo"[(index+slot*3)%4];var template=templates[family];
                    Vector3 size=template.Size*(block.distant?.9f:1);
                    float z=side*(5.4f+size.z*.5f);Vector3 centreWorld=new Vector3(block.x+x,plan.yardY+size.y*.5f,block.z+z);
                    var bounds=new Bounds(centreWorld,size);CheckHouse(bounds,plan,occupied,block.id);occupied.Add(bounds);
                    string address="Bahay_BayanTown_"+block.id+"_"+slot;
                    int facade=EnvColourPass.FacadeIndexFor(address),colour=EnvColourPass.RoofIndexFor(address);
                    if(block.distant)
                    {ProxyHouse(g,new Vector3(x,plan.yardY,z),size,family,Remap(wall,facade,colour),Remap(roof,facade,colour),Remap(window,facade,colour),Remap(trim,facade,colour));proxy++;}
                    else
                    {
                        var mesh=Object.Instantiate(template.Mesh);temporary.Add(mesh);
                        var uv=mesh.uv;for(int i=0;i<uv.Length;i++)uv[i]=Remap(uv[i],facade,colour);mesh.uv=uv;
                        var rotation=Quaternion.Euler(0,side<0?0:180,0);Vector3 centre=rotation*template.Centre;
                        Vector3 at=new Vector3(x-centre.x,plan.yardY-(template.Centre.y-template.Size.y*.5f),z-centre.z);
                        combine.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(at,rotation,template.Scale)});full++;
                    }
                }
                if(g.Vertices.Count>0)
                {
                    var detail=g.BuildMesh(block.id+" gardens");temporary.Add(detail);
                    combine.Add(new CombineInstance{mesh=detail,transform=Matrix4x4.identity});
                }
                var assembled=new Mesh{name=block.id};assembled.indexFormat=IndexFormat.UInt32;assembled.CombineMeshes(combine.ToArray(),true,true);
                assembled.RecalculateBounds();vertices+=assembled.vertexCount;
                var go=MeshObject(root,block.id,assembled,material);go.transform.localPosition=new Vector3(block.x,0,block.z);
                foreach(var mesh in temporary)Object.DestroyImmediate(mesh);index++;
            }
            var ground=TownGround(plan);vertices+=ground.vertexCount;MeshObject(root,"Connected town ground",ground,material);
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length || solids.Any(p=>p.Key==null || p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Town context changed original collision.");
            AssetDatabase.SaveAssets();report.AppendLine($"Bayan context: {plan.blocks.Length} blocks, {full} retained-family homes, {proxy} simplified distant homes, {gardens} planted shared plots, {vertices} vertices. One palette material, no new colliders or distant shadows.");
        }
        private static Rect RoadRect(Road road)
        {
            float half=road.width*.5f;
            return Rect.MinMaxRect(Mathf.Min(road.x0,road.x1)-half,Mathf.Min(road.z0,road.z1)-half,
                Mathf.Max(road.x0,road.x1)+half,Mathf.Max(road.z0,road.z1)+half);
        }
        private static void CheckHouse(Bounds b,Plan plan,List<Bounds> occupied,string id)
        {
            float edge=plan.retainedHalfExtent;
            if(b.min.x<edge && b.max.x>-edge && b.min.z<edge && b.max.z>-edge)throw new InvalidOperationException(id+" enters retained town.");
            var footprint=Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z);
            foreach(var road in plan.roads)if(footprint.Overlaps(RoadRect(road)))throw new InvalidOperationException(id+" enters a connected road.");
            foreach(var other in occupied)if(b.Intersects(other))throw new InvalidOperationException(id+" overlaps an earlier house.");
        }
        private static Mesh TownGround(Plan plan)
        {
            var xs=new SortedSet<float>{-plan.groundHalfExtent,-plan.retainedHalfExtent,plan.retainedHalfExtent,plan.groundHalfExtent};
            var zs=new SortedSet<float>(xs);var roads=plan.roads.Select(RoadRect).ToArray();
            foreach(var r in roads)
            {foreach(float x in new[]{r.xMin-1.6f,r.xMin,r.xMax,r.xMax+1.6f})xs.Add(x);foreach(float z in new[]{r.yMin-1.6f,r.yMin,r.yMax,r.yMax+1.6f})zs.Add(z);}
            var xv=xs.ToArray();var zv=zs.ToArray();var g=new Geometry();var street=new bool[xv.Length-1,zv.Length-1];var drawn=new bool[xv.Length-1,zv.Length-1];
            for(int x=0;x<xv.Length-1;x++)for(int z=0;z<zv.Length-1;z++)
            {
                var p=new Vector2((xv[x]+xv[x+1])*.5f,(zv[z]+zv[z+1])*.5f);
                if(Mathf.Abs(p.x)<plan.retainedHalfExtent && Mathf.Abs(p.y)<plan.retainedHalfExtent)continue;
                bool road=roads.Any(r=>r.Contains(p));bool walk=roads.Any(r=>Rect.MinMaxRect(r.xMin-1.6f,r.yMin-1.6f,r.xMax+1.6f,r.yMax+1.6f).Contains(p));
                street[x,z]=road;drawn[x,z]=true;
                Ground(g,new Vector3(p.x,road?plan.roadY:plan.yardY,p.y),new Vector2(xv[x+1]-xv[x],zv[z+1]-zv[z]),ExtraUv(road?5:walk?6:4));
            }
            for(int x=0;x<street.GetLength(0)-1;x++)for(int z=0;z<street.GetLength(1);z++)
                if(drawn[x,z]&&drawn[x+1,z]&&street[x,z]!=street[x+1,z])
                {float a=zv[z],b=zv[z+1];if(street[x,z]){float swap=a;a=b;b=swap;}g.Quad(new Vector3(xv[x+1],plan.roadY,a),new Vector3(xv[x+1],plan.yardY,a),new Vector3(xv[x+1],plan.yardY,b),new Vector3(xv[x+1],plan.roadY,b),ExtraUv(6));}
            for(int x=0;x<street.GetLength(0);x++)for(int z=0;z<street.GetLength(1)-1;z++)
                if(drawn[x,z]&&drawn[x,z+1]&&street[x,z]!=street[x,z+1])
                {float a=xv[x],b=xv[x+1];if(street[x,z+1]){float swap=a;a=b;b=swap;}g.Quad(new Vector3(a,plan.roadY,zv[z+1]),new Vector3(a,plan.yardY,zv[z+1]),new Vector3(b,plan.yardY,zv[z+1]),new Vector3(b,plan.roadY,zv[z+1]),ExtraUv(6));}
            return g.BuildMesh("Bayan connected town ground");
        }

        private static Texture2D BuildAtlas(out Vector2 wall,out Vector2 roof,out Vector2 window,out Vector2 trim)
        {
            var original=new Texture2D(2,2,TextureFormat.RGBA32,false);
            original.LoadImage(File.ReadAllBytes("Assets/TumbangPreso/Art/models/kits/city/Textures/colormap.png"));
            wall=Closest(original,new Color32(238,235,222,255));roof=Closest(original,new Color32(97,203,139,255));
            window=Closest(original,new Color32(86,125,157,255));trim=Closest(original,new Color32(45,48,49,255));
            Object.DestroyImmediate(original);
            var pixels=new Color32[AtlasWidth*AtlasHeight];
            for(int r=0;r<6;r++)
            {
                var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                source.LoadImage(File.ReadAllBytes("Assets/TumbangPreso/Resources/Models/roofs/"+EnvColourPass.RoofAtlases[r]+".png"));
                var raw=source.GetPixels32();
                for(int f=0;f<6;f++)for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)
                {
                    int sx=Mathf.Clamp(Mathf.FloorToInt((x-Pad+.5f)*source.width/Tile),0,source.width-1);
                    int sy=Mathf.Clamp(Mathf.FloorToInt((y-Pad+.5f)*source.height/Tile),0,source.height-1);
                    Color32 p=raw[sy*source.width+sx];Color tint=EnvColourPass.FacadeTints[f];
                    pixels[(f*Cell+y)*AtlasWidth+r*Cell+x]=new Color32((byte)Mathf.RoundToInt(p.r*tint.r),
                        (byte)Mathf.RoundToInt(p.g*tint.g),(byte)Mathf.RoundToInt(p.b*tint.b),p.a);
                }
                Object.DestroyImmediate(source);
            }
            for(int y=AtlasWidth;y<AtlasHeight;y++)for(int x=0;x<AtlasWidth;x++)pixels[y*AtlasWidth+x]=Extra[Mathf.Min(x/16,Extra.Length-1)];
            var result=new Texture2D(AtlasWidth,AtlasHeight,TextureFormat.RGBA32,false);
            result.SetPixels32(pixels);result.Apply();
            string path=Folder+"/BayanTownPalette.png";File.WriteAllBytes(path,result.EncodeToPNG());Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Vector2 Closest(Texture2D texture,Color32 wanted)
        {
            var pixels=texture.GetPixels32();int best=0;float distance=float.MaxValue;
            for(int i=0;i<pixels.Length;i++)
            {var p=pixels[i];float d=(p.r-wanted.r)*(p.r-wanted.r)+(p.g-wanted.g)*(p.g-wanted.g)+(p.b-wanted.b)*(p.b-wanted.b);if(d<distance){distance=d;best=i;}}
            return new Vector2((best%texture.width+.5f)/texture.width,(best/texture.width+.5f)/texture.height);
        }
        private static Vector2 Remap(Vector2 uv,int facade,int roof)
            =>new Vector2((roof*Cell+Pad+Mathf.Clamp01(uv.x)*Tile)/AtlasWidth,(facade*Cell+Pad+Mathf.Clamp01(uv.y)*Tile)/AtlasHeight);
        private static Vector2 ExtraUv(int index)=>new Vector2((index*16+8f)/AtlasWidth,(AtlasWidth+4f)/AtlasHeight);

        private static GameObject MeshObject(Transform root,string name,Mesh mesh,Material material)
        {
            string path=Folder+"/Meshes/"+name.Replace(' ','_')+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=saved;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go;
        }

        private static void ProxyHouse(Geometry g,Vector3 at,Vector3 size,char family,Vector2 wall,Vector2 roof,Vector2 window,Vector2 trim)
        {
            if(family=='b')
            {
                HouseVolume(g,at+new Vector3(-size.x*.26f,0,0),new Vector3(size.x*.48f,size.y*.67f,size.z),wall,roof,window,trim);
                HouseVolume(g,at+new Vector3(size.x*.24f,0,0),new Vector3(size.x*.52f,size.y,size.z),wall,roof,window,trim);
            }
            else HouseVolume(g,at,size,wall,roof,window,trim);
        }
        private static void HouseVolume(Geometry g,Vector3 at,Vector3 size,Vector2 wall,Vector2 roof,Vector2 window,Vector2 trim)
        {
            float rise=Mathf.Min(1.3f,size.y*.25f),body=size.y-rise;
            g.Box(at+Vector3.up*body*.5f,new Vector3(size.x,body,size.z),wall);
            g.Box(at+Vector3.up*body,new Vector3(size.x+.24f,.12f,size.z+.24f),trim);
            float w=size.x*.5f+.14f,d=size.z*.5f+.14f;
            Vector3 a=at+new Vector3(-w,body,-d),b=at+new Vector3(-w,body,d),c=at+new Vector3(w,body,-d),e=at+new Vector3(w,body,d);
            Vector3 near=at+new Vector3(0,size.y,d),far=at+new Vector3(0,size.y,-d);
            g.Quad(a,b,near,far,roof);g.Quad(far,near,e,c,roof);
            g.Triangle(b,e,near,wall);g.Triangle(c,a,far,wall);
            int floors=Mathf.Max(1,Mathf.RoundToInt(body/2.2f)),columns=Mathf.Clamp(Mathf.RoundToInt(size.x/2.2f),1,3);
            for(int floor=0;floor<floors;floor++)for(int col=0;col<columns;col++)foreach(float side in new[]{-1f,1f})
            {
                var p=at+new Vector3((col-(columns-1)*.5f)*Mathf.Min(2.2f,size.x/(columns+.2f)),1.15f+floor*2.2f,side*(size.z*.5f+.025f));
                Window(g,p,.96f,.96f,side,trim);
                Window(g,p+Vector3.forward*side*.006f,.74f,.72f,side,window);
            }
        }
        private static void Window(Geometry g,Vector3 at,float width,float height,float facing,Vector2 uv)
        {
            var x=Vector3.right*width*.5f;var y=Vector3.up*height*.5f;
            if(facing>0)g.Quad(at-x-y,at+x-y,at+x+y,at-x+y,uv);
            else g.Quad(at-x+y,at+x+y,at+x-y,at-x-y,uv);
        }
        private static void Ground(Geometry g,Vector3 at,Vector2 size,Vector2 uv)
        {var x=Vector3.right*size.x*.5f;var z=Vector3.forward*size.y*.5f;g.Quad(at-x-z,at-x+z,at+x+z,at+x-z,uv);}
        private static void Street(Geometry g,Vector2 a,Vector2 b,float width)
        {
            var direction=(b-a).normalized;var side=new Vector2(-direction.y,direction.x)*width*.5f;
            Vector3 P(Vector2 v)=>new Vector3(v.x,.092f,v.y);
            g.Quad(P(a+side),P(b+side),P(b-side),P(a-side),ExtraUv(5));
        }
        private static void Tree(Geometry g,Vector3 at,float height)
        {
            g.Box(at+Vector3.up*height*.40f,new Vector3(.35f,height*.80f,.35f),ExtraUv(3));
            for(int crown=0;crown<3;crown++)
            {
                Vector3 p=at+new Vector3((crown-1)*1.55f,height*.72f+(crown==1?.7f:0),crown==1?.4f:0);
                Vector3 top=p+Vector3.up*.95f,bottom=p-Vector3.up*.85f;
                for(int side=0;side<6;side++)
                {
                    float a=side*Mathf.PI/3,b=(side+1)*Mathf.PI/3;
                    Vector3 x=p+new Vector3(Mathf.Cos(a)*2.1f,0,Mathf.Sin(a)*2.1f),z=p+new Vector3(Mathf.Cos(b)*2.1f,0,Mathf.Sin(b)*2.1f);
                    g.Triangle(x,top,z,ExtraUv(crown));g.Triangle(z,bottom,x,ExtraUv(crown));
                }
            }
        }
    }
}
