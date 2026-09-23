using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Fitted plaster texture and construction detail for the six near apartments only.</summary>
    public static class RooftopApartmentFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/ApartmentFinish",RootName="Apartment construction finish";
        private const int Tile=128,Pad=2,Cell=Tile+Pad*2,Size=Cell*4;
        private const float Metres=4;
        [Serializable] private sealed class Site {public float height;}
        [Serializable] private sealed class Plan {public Site[] sites;}
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/rooftop-apartment-finish");File.WriteAllText("Logs/rooftop-apartment-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var roofs=map.transform.Find("Dressing/Metro rooftops/Inhabited neighbor roofs");
            if(roofs==null||roofs.childCount!=6)throw new InvalidOperationException("Expected six retained roof neighbors.");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var palette=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/SaBubong/Neighbors/RoofNeighbors.mat");
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/sabubong-neighbor-roofs-20260924.json"));
            var material=Material(palette);int index=0,vertices=0;
            foreach(Transform roof in roofs)
            {
                string originalPath="Assets/TumbangPreso/Art/SaBubong/Neighbors/RoofNeighbor"+index+".asset";
                var original=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);if(original==null)throw new InvalidOperationException(originalPath);
                var fitted=Textured(original,"Apartment plaster "+index);vertices+=fitted.vertexCount;
                var saved=Save(fitted,Folder+"/Apartment"+index+".asset");
                var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(saved));importer.userData="TUMP_ROOF_APARTMENT_SOURCE:"+originalPath;importer.SaveAndReimport();
                roof.GetComponent<MeshFilter>().sharedMesh=saved;roof.GetComponent<MeshRenderer>().sharedMaterial=material;
                var old=roof.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var detail=new RooftopNeighborsAuthor.Geometry();float h=plan.sites[index].height;
                // Four concrete corners wrap around the retained plaster body.
                foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
                {
                    detail.Box(new Vector3(x*5.66f,h*.5f,z*5.37f),new Vector3(.28f,h,.24f),3);
                    detail.Box(new Vector3(x*5.87f,h*.5f,z*5.16f),new Vector3(.24f,h,.28f),3);
                }
                for(float y=3.3f;y<h-1.3f;y+=3.15f)
                {
                    foreach(float side in new[]{-1f,1f})foreach(float x in new[]{-3.45f,0,3.45f})
                    {
                        detail.Box(new Vector3(x,y-.85f,side*5.4f),new Vector3(2.55f,.16f,.18f),3);
                        foreach(float edge in new[]{-1f,1f})detail.Box(new Vector3(x+edge*1.16f,y,side*5.36f),new Vector3(.10f,1.56f,.08f),4);
                    }
                    foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-2.8f,2.8f})
                    {
                        detail.Box(new Vector3(side*5.9f,y-.84f,z),new Vector3(.18f,.16f,2.45f),3);
                        foreach(float edge in new[]{-1f,1f})detail.Box(new Vector3(side*5.86f,y,z+edge*1.13f),new Vector3(.08f,1.60f,.10f),4);
                    }
                }
                for(float y=6.4f;y<h-2;y+=6.3f)foreach(float side in new[]{-1f,1f})
                {detail.Box(new Vector3(0,y,side*5.32f),new Vector3(11.4f,.08f,.04f),3);detail.Box(new Vector3(side*5.82f,y,0),new Vector3(.04f,.08f,10.4f),3);}
                var mesh=detail.Mesh("Apartment fitted construction "+index);vertices+=mesh.vertexCount;
                if(mesh.bounds.min.x<-6||mesh.bounds.max.x>6||mesh.bounds.min.z<-5.5f||mesh.bounds.max.z>5.5f)
                    throw new InvalidOperationException("Apartment finish exceeds its retained plot.");
                var go=new GameObject(RootName);go.transform.SetParent(roof,false);go.isStatic=true;
                go.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Folder+"/Construction"+index+".asset");
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=palette;r.shadowCastingMode=ShadowCastingMode.Off;
                AirborneByDesign.Attach(go,"Corner strips and window sills fitted to the retained apartment body.");index++;
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Apartment finish changed collision.");
            report.AppendLine($"Six apartments: local metre-scaled plaster atlas, wrapped concrete corners, fitted window jambs/sills, restrained two-storey reveals. {vertices}total derived vertices/12renderers/two materials, original bodies/roof use/collision retained. Outer district unchanged.");
        }
        private static Vector2 Uv(int colour,float u,float v)=>new Vector2(((colour%4)*Cell+Pad+.5f+u*(Tile-1))/Size,((colour/4)*Cell+Pad+.5f+v*(Tile-1))/Size);
        private static Mesh Textured(Mesh source,string name)
        {
            var v=source.vertices;var uv=source.uv;var colours=source.colors;var points=new List<Vector3>();var coords=new List<Vector2>();var roles=new List<Color>();var indices=new List<int>();
            if(v.Length%4!=0||colours.Length!=v.Length)throw new InvalidOperationException("Expected the retained quad-authored source.");
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int colour,Color role,float u,float t)
            {
                int n=points.Count;points.AddRange(new[]{a,b,c,d});roles.AddRange(new[]{role,role,role,role});
                coords.AddRange(new[]{Uv(colour,0,0),Uv(colour,u,0),Uv(colour,u,t),Uv(colour,0,t)});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            for(int i=0;i<v.Length;i+=4)
            {
                int colour=Mathf.Clamp(Mathf.FloorToInt(uv[i].x*16),0,15);
                if(Mathf.RoundToInt(colours[i].r*32)!=1)
                {
                    int n=points.Count;points.AddRange(new[]{v[i],v[i+1],v[i+2],v[i+3]});
                    for(int j=0;j<4;j++){coords.Add(Uv(colour,.5f,.5f));roles.Add(colours[i+j]);}
                    indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});continue;
                }
                var x=v[i+1]-v[i];var y=v[i+3]-v[i];float width=x.magnitude,height=y.magnitude;x.Normalize();y.Normalize();
                for(float a=0;a<width-.0001f;a+=Metres)for(float b=0;b<height-.0001f;b+=Metres)
                {
                    float w=Mathf.Min(Metres,width-a),h=Mathf.Min(Metres,height-b);var p=v[i]+x*a+y*b;
                    Quad(p,p+x*w,p+x*w+y*h,p+y*h,colour,colours[i],w/Metres,h/Metres);
                }
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.SetUVs(0,coords);mesh.SetColors(roles);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            if((mesh.bounds.center-source.bounds.center).sqrMagnitude>.000001f||(mesh.bounds.size-source.bounds.size).sqrMagnitude>.000001f)
                throw new InvalidOperationException("Texturing changed apartment silhouette.");return mesh;
        }
        private static Material Material(Material original)
        {
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(original.mainTexture)));
            var swatches=source.GetPixels();Object.DestroyImmediate(source);var pixels=new Color[Size*Size];
            for(int colour=0;colour<16;colour++)for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)
            {
                float u=Mathf.Clamp(x-Pad,0,Tile-1)/(float)(Tile-1),v=Mathf.Clamp(y-Pad,0,Tile-1)/(float)(Tile-1),shade=1;
                if(colour<3)
                {
                    float field=Noise(u,v,4,colour+17),brush=Noise(u,v,13,colour+71),fine=Noise(u,v,47,colour+101);
                    // Broad maintained paint fields and restrained brush-scale variation.
                    // Only the three plaster swatches receive this texture, never glass/wood/cloth.
                    shade=1+(Mathf.SmoothStep(0,1,field)-.5f)*.30f+(brush-.5f)*.10f+(fine-.5f)*.035f;
                }
                var c=swatches[colour]*shade;c.a=1;pixels[((colour/4)*Cell+y)*Size+(colour%4)*Cell+x]=c;
            }
            var tex=new Texture2D(Size,Size,TextureFormat.RGBA32,false);tex.SetPixels(pixels);tex.Apply();string texPath=Folder+"/ApartmentSurfaces.png";
            File.WriteAllBytes(texPath,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texPath);importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.SaveAndReimport();
            var draft=new Material(original){name="ApartmentSurfaces",mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)};
            draft.SetOverrideTag("TumpApartmentSource",AssetDatabase.GetAssetPath(original));
            string path=Folder+"/ApartmentSurfaces.mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);}return saved;
        }
        internal static float Noise(float u,float v,int cells,int seed)
        {
            float x=u*cells,y=v*cells;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float tx=Mathf.SmoothStep(0,1,x-ix),ty=Mathf.SmoothStep(0,1,y-iy);
            float Hash(int a,int b){uint h=(uint)((a%cells)*73856093^(b%cells)*19349663^seed*83492791);h=(h^(h>>13))*1274126177u;return (h&65535)/65535f;}
            return Mathf.Lerp(Mathf.Lerp(Hash(ix,iy),Hash(ix+1,iy),tx),Mathf.Lerp(Hash(ix,iy+1),Hash(ix+1,iy+1),tx),ty);
        }
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
