using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Distant Eskinita blocks, grouped by street block and coloured from the retained palette.</summary>
    public static class EskinitaDistrictAuthor
    {
        private const string Folder = "Assets/TumbangPreso/Art/EskinitaDistrict";
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

        public static void Add(Transform owner, Dictionary<char, Transform> sources, StringBuilder report)
        {
            Directory.CreateDirectory(Folder + "/Meshes"); AssetDatabase.Refresh();
            Vector2 wall, roof, window, trim;
            Texture2D atlas = BuildAtlas(out wall, out roof, out window, out trim);
            string materialPath = Folder + "/EskinitaDistrict.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var sourceMaterial = sources['a'].GetComponent<MeshRenderer>().sharedMaterial;
            if (material == null) { material = new Material(sourceMaterial); AssetDatabase.CreateAsset(material,materialPath); }
            else EditorUtility.CopySerialized(sourceMaterial,material);
            material.name = "Eskinita distant neighborhood";
            material.SetOverrideTag("TumpSurfaceSource", "");
            material.mainTexture = atlas; material.mainTextureScale = Vector2.one; material.mainTextureOffset = Vector2.zero;
            material.SetFloat("_SurfaceStrength",0); // Fine surface marks do not belong on distant silhouettes.
            EditorUtility.SetDirty(material);

            var templates = new Dictionary<char,Template>();
            foreach (var pair in sources)
            {
                var filter = pair.Value.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh.subMeshCount != 1)
                    throw new InvalidOperationException("Expected one retained atlas mesh for family " + pair.Key);
                var scale = pair.Value.lossyScale;
                templates.Add(pair.Key,new Template { Mesh=filter.sharedMesh,Scale=scale,
                    Size=Vector3.Scale(filter.sharedMesh.bounds.size,scale),Centre=Vector3.Scale(filter.sharedMesh.bounds.center,scale) });
            }
            var root = new GameObject("DistantDistrict").transform; root.SetParent(owner,false);
            var road = new Geometry(); int blocks=0, full=0, simplified=0, yards=0, vertices=0;
            var cells = new List<Vector2>();
            foreach (float z in new[] { -192f,-156f,-120f,-84f,84f,120f,156f,192f })
                foreach (float x in new[] { -144f,-108f,-72f,-36f,0f,36f,72f,108f,144f }) cells.Add(new Vector2(x,z));
            foreach (float x in new[] { -144f,-108f,-72f,72f,108f,144f })
                foreach (float z in new[] { -54f,-18f,18f,54f }) cells.Add(new Vector2(x,z));

            foreach (var cell in cells)
            {
                var geometry = new Geometry();
                var combine = new List<CombineInstance>();
                var temporary = new List<Mesh>();
                bool proxy = Mathf.Max(Mathf.Abs(cell.x),Mathf.Abs(cell.y)) > 100;
                string key = "Block_" + ((int)cell.x) + "_" + ((int)cell.y);
                // Two small residential rows face one street; each side has a quiet rear yard.
                foreach (float side in new[] { -1f,1f })
                    Ground(geometry,new Vector3(0,.096f,side*10),new Vector2(28,11.5f),ExtraUv(4));
                for (int slot=0;slot<6;slot++)
                {
                    int column=slot%3; float side=slot<3?-1:1;
                    float x=(column-1)*8.8f;
                    if (slot==4 && blocks%4==0)
                    { Tree(geometry,new Vector3(x,.1f,side*8.5f),4.5f+(blocks%3)*.45f); yards++; continue; }
                    char family="aceob"[(blocks*2+slot*3)%5];
                    var template=templates[family];
                    string address="Bahay_District_"+key+"_"+slot;
                    int facade=EnvColourPass.FacadeIndexFor(address), colour=EnvColourPass.RoofIndexFor(address);
                    float centreZ=side*(5.4f+template.Size.z*.5f);
                    if (proxy)
                    {
                        ProxyHouse(geometry,new Vector3(x,.1f,centreZ),template.Size,family,
                            Remap(wall,facade,colour),Remap(roof,facade,colour),Remap(window,facade,colour),Remap(trim,facade,colour));
                        simplified++;
                    }
                    else
                    {
                        var mesh=Object.Instantiate(template.Mesh); temporary.Add(mesh);
                        var uv=mesh.uv; for(int i=0;i<uv.Length;i++)uv[i]=Remap(uv[i],facade,colour); mesh.uv=uv;
                        var rotation=Quaternion.Euler(0,side<0?0:180,0);
                        Vector3 centre=rotation*template.Centre;
                        Vector3 at=new Vector3(x-centre.x,.1f-(template.Centre.y-template.Size.y*.5f),centreZ-centre.z);
                        combine.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(at,rotation,template.Scale)}); full++;
                    }
                }
                Mesh details=geometry.BuildMesh(key+" quiet plots"); temporary.Add(details);
                combine.Add(new CombineInstance{mesh=details,transform=Matrix4x4.identity});
                var assembled=new Mesh{name=key};
                if(temporary.Sum(m=>m.vertexCount)>65535)assembled.indexFormat=IndexFormat.UInt32;
                assembled.CombineMeshes(combine.ToArray(),true,true);
                assembled.RecalculateBounds();
                vertices+=assembled.vertexCount;
                var go=MeshObject(root,key,assembled,material);
                go.transform.localPosition=new Vector3(cell.x,0,cell.y);
                var bounds=go.GetComponent<Renderer>().bounds;
                if(bounds.min.x<48 && bounds.max.x> -48 && bounds.min.z<66 && bounds.max.z> -66)
                    throw new InvalidOperationException(key+" enters the retained neighborhood.");
                foreach(var mesh in temporary)Object.DestroyImmediate(mesh);
                blocks++;
            }

            foreach(float z in new[]{-192f,-156f,-120f,-84f,84f,120f,156f,192f})
                Street(road,new Vector2(-162,z),new Vector2(162,z),6);
            foreach(float z in new[]{-54f,-18f,18f,54f})
                foreach(float side in new[]{-1f,1f})Street(road,new Vector2(side*54,z),new Vector2(side*162,z),6);
            foreach(float x in new[]{-162f,-126f,-90f,-54f,54f,90f,126f,162f})
                Street(road,new Vector2(x,-204),new Vector2(x,204),6);
            foreach(float x in new[]{-18f,18f})foreach(float side in new[]{-1f,1f})
                Street(road,new Vector2(x,side*72),new Vector2(x,side*204),6);
            foreach(float xSide in new[]{-1f,1f})foreach(float zSide in new[]{-1f,1f})
            {
                Street(road,new Vector2(xSide*32.2f,zSide*53),new Vector2(xSide*54,zSide*70),6);
                Street(road,new Vector2(xSide*44,zSide*50),new Vector2(xSide*54,zSide*54),6);
            }
            MeshObject(root,"District streets",road.BuildMesh("District streets"),material);
            AssetDatabase.SaveAssets();
            report.AppendLine($"Distant district:{blocks} street blocks,{full} retained mesh houses,{simplified} simplified distant houses,{yards} planted shared yards,{vertices} vertices; one shared palette material; distant shadows off.");
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
            string path=Folder+"/EskinitaDistrictPalette.png";File.WriteAllBytes(path,result.EncodeToPNG());Object.DestroyImmediate(result);
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
            g.Box(at+Vector3.up*height*.36f,new Vector3(.32f,height*.72f,.32f),ExtraUv(3));
            for(int crown=0;crown<3;crown++)
            {
                Vector3 p=at+new Vector3((crown-1)*1.15f,height*.72f+(crown==1?.7f:0),crown==1?.4f:0);
                Vector3 top=p+Vector3.up*1.7f,bottom=p-Vector3.up*1.25f;
                for(int side=0;side<6;side++)
                {
                    float a=side*Mathf.PI/3,b=(side+1)*Mathf.PI/3;
                    Vector3 x=p+new Vector3(Mathf.Cos(a)*1.6f,0,Mathf.Sin(a)*1.6f),z=p+new Vector3(Mathf.Cos(b)*1.6f,0,Mathf.Sin(b)*1.6f);
                    g.Triangle(x,top,z,ExtraUv(crown));g.Triangle(z,bottom,x,ExtraUv(crown));
                }
            }
        }
    }
}
