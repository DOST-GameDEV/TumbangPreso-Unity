using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class IlalimSkylineFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/IlalimSkyline/",RootName="IlalimSkylineFinishes",SourceTag="TumpIlalimSkylineSource";
        private sealed class Face
        {
            public Vector3 Normal,Horizontal;public float Plane;
            public readonly List<Vector2[]> Triangles=new List<Vector2[]>();
            public Vector2 Min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),Max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            public Vector3 Point(Vector2 p)=>Horizontal*p.x+Vector3.up*p.y+Normal*Plane;
            public bool Contains(Vector2 p)=>Triangles.Any(t=>Inside(p,t[0],t[1],t[2]));
            private static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
            {
                float Cross(Vector2 x,Vector2 y)=>x.x*y.y-x.y*y.x;
                float x=Cross(b-a,p-a),y=Cross(c-b,p-b),z=Cross(a-c,p-c);
                return (x>=-.000001f&&y>=-.000001f&&z>=-.000001f)||(x<=.000001f&&y<=.000001f&&z<=.000001f);
            }
            public bool Contains(Rect r)
            {
                foreach(float x in new[]{r.xMin,r.center.x,r.xMax})foreach(float y in new[]{r.yMin,r.center.y,r.yMax})
                    if(!Contains(new Vector2(x,y)))return false;
                return true;
            }
        }
        private sealed class Geometry
        {
            public readonly List<Vector3> Vertices=new List<Vector3>(),Normals=new List<Vector3>();
            public readonly List<int>[] Indices={new List<int>(),new List<int>()};
            public void Quad(Face face,Rect rect,Transform source,Transform owner,int material)
            {
                var matrix=owner.worldToLocalMatrix*source.localToWorldMatrix;
                var normal=matrix.inverse.transpose.MultiplyVector(face.Normal).normalized;
                float scale=source.TransformVector(face.Normal).magnitude;int first=Vertices.Count;
                foreach(var p in new[]{new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),new Vector2(rect.xMin,rect.yMax)})
                {Vertices.Add(matrix.MultiplyPoint3x4(face.Point(p)+face.Normal*(.027f/scale)));Normals.Add(normal);}
                Indices[material].AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            public Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name};if(Vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(Vertices);mesh.SetNormals(Normals);mesh.subMeshCount=2;
                mesh.SetTriangles(Indices[0],0);mesh.SetTriangles(Indices[1],1);mesh.RecalculateBounds();return mesh;
            }
        }
        public static void ClearPrevious(string map)
        {
            if(map!="IlalimNgTulay")return;
            var skyline=GameObject.Find("IlalimNgTulay/Dressing/SkylineKit");if(skyline==null)return;
            foreach(var renderer in skyline.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    string source=materials[i].GetTag(SourceTag,false);if(string.IsNullOrEmpty(source))continue;
                    materials[i]=AssetDatabase.LoadAssetAtPath<Material>(source)??throw new InvalidOperationException("Missing skyline material source.");changed=true;
                }
                if(changed){renderer.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            }
            var old=GameObject.Find("IlalimNgTulay/Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/ilalim-skyline");File.WriteAllText("Logs/ilalim-skyline/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious("IlalimNgTulay");var map=GameObject.Find("IlalimNgTulay");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var owner=new GameObject(RootName).transform;owner.SetParent(map.transform.Find("Dressing"),false);
            AirborneByDesign.Attach(owner.gameObject,"Thin facade finishes fitted to the retained skyline walls; no new freestanding bodies.");
            var geometry=new Dictionary<char,Geometry>{{'a',new Geometry()},{'b',new Geometry()},{'c',new Geometry()}};
            var counts=new Dictionary<char,int>{{'a',0},{'b',0},{'c',0}};var palettes=new Dictionary<string,Texture2D>();var materials=new Dictionary<string,Material>();
            int panels=0,divisions=0;
            foreach(var renderer in map.transform.Find("Dressing/SkylineKit").GetComponentsInChildren<MeshRenderer>())
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                var original=MapSurfaceAuthor.SourceMeshForAuthoring(filter.sharedMesh);
                string model=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(original));
                if(!model.StartsWith("low-detail-building-",StringComparison.Ordinal))continue;
                char kind=model[model.Length-1];if(!geometry.ContainsKey(kind))continue;counts[kind]++;
                var g=geometry[kind];var faces=Faces(filter.sharedMesh,kind=='a');
                float scaleY=renderer.transform.TransformVector(Vector3.up).magnitude;
                foreach(var face in faces)
                {
                    float scaleX=renderer.transform.TransformVector(face.Horizontal).magnitude;
                    float width=(face.Max.x-face.Min.x)*scaleX,height=(face.Max.y-face.Min.y)*scaleY;
                    if(width<1.2f||height<2.8f)continue;
                    float pitch=kind=='a'?3:kind=='b'?2.75f:3.2f;
                    for(float y=filter.sharedMesh.bounds.min.y+1.8f/scaleY;y<face.Max.y-.35f/scaleY;y+=pitch/scaleY)
                    {
                        if(kind=='a')
                        {
                            var band=Rect.MinMaxRect(face.Min.x+.10f/scaleX,y-.05f/scaleY,face.Max.x-.10f/scaleX,y+.05f/scaleY);
                            if(face.Contains(band)){g.Quad(face,band,renderer.transform,owner,0);divisions++;}continue;
                        }
                        int columns=width>=3.5f?2:1;
                        for(int column=0;column<columns;column++)
                        {
                            float x=Mathf.Lerp(face.Min.x,face.Max.x,(column+1f)/(columns+1f));
                            float w=Mathf.Min(kind=='b'?1.12f:.85f,width/(columns+1f)*.80f)/scaleX,h=1.15f/scaleY;
                            var outer=new Rect(x-w*.5f-.07f/scaleX,y-h*.5f-.07f/scaleY,w+.14f/scaleX,h+.14f/scaleY);
                            if(!face.Contains(outer))continue;
                            var inner=new Rect(x-w*.5f,y-h*.5f,w,h);Frame(g,face,outer,inner,renderer.transform,owner);panels++;
                        }
                    }
                    if(kind=='a'&&width>=3.3f)
                    {
                        float x=(face.Min.x+face.Max.x)*.5f;
                        var strip=Rect.MinMaxRect(x-.035f/scaleX,face.Min.y+.1f/scaleY,x+.035f/scaleX,face.Max.y-.1f/scaleY);
                        if(face.Contains(strip)){g.Quad(face,strip,renderer.transform,owner,0);divisions++;}
                    }
                }
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    var source=slots[i];string sourcePath=AssetDatabase.GetAssetPath(source),texturePath=AssetDatabase.GetAssetPath(source.mainTexture);
                    if(!palettes.TryGetValue(texturePath,out var palette)){palette=GlassPalette(texturePath);palettes.Add(texturePath,palette);}
                    if(!materials.TryGetValue(sourcePath,out var saved))
                    {
                        var draft=new Material(source){mainTexture=palette};draft.SetOverrideTag(SourceTag,sourcePath);
                        string path=Folder+"Facade_"+AssetDatabase.AssetPathToGUID(sourcePath)+".mat";saved=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);Object.DestroyImmediate(draft);EditorUtility.SetDirty(saved);}
                        materials.Add(sourcePath,saved);
                    }
                    slots[i]=saved;
                }
                renderer.sharedMaterials=slots;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            if(counts['a']!=11||counts['b']!=9||counts['c']!=14)throw new InvalidOperationException("Reassess changed skyline instance counts.");
            var trim=Mat("FacadeFrames",new Color(.52f,.55f,.54f),.12f);var glass=Mat("WindowGlass",new Color(.26f,.38f,.46f),.35f);
            int vertices=0;
            foreach(var pair in geometry)
            {
                if(pair.Value.Vertices.Count==0)throw new InvalidOperationException("No fitting facade details for variant "+pair.Key);
                var mesh=pair.Value.Mesh("Ilalim skyline "+pair.Key);vertices+=mesh.vertexCount;
                string path=Folder+"Facades_"+pair.Key+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
                var go=new GameObject("Facades_"+pair.Key);go.transform.SetParent(owner,false);go.isStatic=true;
                go.AddComponent<MeshFilter>().sharedMesh=saved;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{trim,glass};
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Skyline finish changed collision.");
            AssetDatabase.SaveAssets();report.AppendLine($"34retained skyline bodies:11a/9b/14c;{panels}fitted window groups,{divisions}glass divisions,{vertices}overlay vertices in3renderers. Original silhouettes, transforms and collision retained.");
        }
        private static List<Face> Faces(Mesh mesh,bool glass)
        {
            var groups=new Dictionary<string,Face>();var points=mesh.vertices;var uv=mesh.uv;var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                int a=indices[i],b=indices[i+1],c=indices[i+2];float wanted=glass?.09375f:.46875f;
                if(Mathf.Abs(uv[a].x-wanted)>.001f||Mathf.Abs(uv[b].x-wanted)>.001f||Mathf.Abs(uv[c].x-wanted)>.001f)continue;
                var normal=Vector3.Cross(points[b]-points[a],points[c]-points[a]).normalized;if(Mathf.Abs(normal.y)>.01f)continue;
                float plane=Vector3.Dot(normal,points[a]);string key=Mathf.RoundToInt(normal.x*1000)+"_"+Mathf.RoundToInt(normal.z*1000)+"_"+Mathf.RoundToInt(plane*10000);
                if(!groups.TryGetValue(key,out var face)){face=new Face{Normal=normal,Horizontal=Vector3.Cross(Vector3.up,normal).normalized,Plane=plane};groups.Add(key,face);}
                var triangle=new[]{a,b,c}.Select(v=>new Vector2(Vector3.Dot(points[v],face.Horizontal),points[v].y)).ToArray();face.Triangles.Add(triangle);
                foreach(var p in triangle){face.Min=Vector2.Min(face.Min,p);face.Max=Vector2.Max(face.Max,p);}
            }
            return groups.Values.ToList();
        }
        private static void Frame(Geometry g,Face face,Rect outer,Rect inner,Transform source,Transform owner)
        {
            g.Quad(face,Rect.MinMaxRect(outer.xMin,outer.yMin,outer.xMax,inner.yMin),source,owner,0);
            g.Quad(face,Rect.MinMaxRect(outer.xMin,inner.yMax,outer.xMax,outer.yMax),source,owner,0);
            g.Quad(face,Rect.MinMaxRect(outer.xMin,inner.yMin,inner.xMin,inner.yMax),source,owner,0);
            g.Quad(face,Rect.MinMaxRect(inner.xMax,inner.yMin,outer.xMax,inner.yMax),source,owner,0);
            g.Quad(face,inner,source,owner,1);
        }
        private static Material Mat(string name,Color color,float smooth)
        {
            string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(material);return material;
        }
        private static Texture2D GlassPalette(string source)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(source));var pixels=texture.GetPixels32();
            for(int i=0;i<pixels.Length;i++)if((i%texture.width)*16/texture.width==1)
            {var p=pixels[i];float value=.2126f*p.r+.7152f*p.g+.0722f*p.b;pixels[i]=new Color32((byte)(value*.70f),(byte)(value*.86f),(byte)(value*.95f),p.a);}
            texture.SetPixels32(pixels);texture.Apply();string path=Folder+"Glass_"+Path.GetFileName(source);File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
