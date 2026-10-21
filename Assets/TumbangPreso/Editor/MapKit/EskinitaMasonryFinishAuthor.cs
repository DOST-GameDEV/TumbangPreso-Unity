using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Five deliberate household finishes, fitted to existing masonry and metal.</summary>
    public static class EskinitaMasonryFinishAuthor
    {
        public const string RootName="EskinitaMasonryFinishes";
        private const string Folder="Assets/TumbangPreso/Art/EskinitaHouseFinishes/Masonry";
        private const int Tile=192,Gutter=4,Pitch=Tile+2*Gutter;
        private readonly struct Finish
        {
            public readonly string Lot;public readonly float BaseHeight,BaseMix,WashX,WashY,RoofRepair;
            public Finish(string lot,float height,float mix,float x,float y,float repair)
            {Lot=lot;BaseHeight=height;BaseMix=mix;WashX=x;WashY=y;RoofRepair=repair;}
        }
        // Explicit lot decisions, not a random palette or an all-map texture pass.
        private static readonly Finish[] Homes={new Finish("0_W",.48f,.32f,-2.3f,.68f,100),
            new Finish("1_W",.64f,.40f,2.5f,.82f,1.64f),new Finish("3_W",.36f,.24f,-3.1f,.46f,100),
            new Finish("1_E",.54f,.30f,2.8f,.62f,-1.64f),new Finish("3_E",.43f,.36f,-1.7f,.75f,100)};

        public static void ClearPrevious(string map)
        {
            if(map!="Eskinita")return;
            var old=GameObject.Find("Eskinita/Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-masonry");File.WriteAllText("Logs/eskinita-masonry/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Eskinita");if(map==null)throw new InvalidOperationException("Eskinita only.");
            ClearPrevious("Eskinita");
            var colliders=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var root=new GameObject(RootName).transform;root.SetParent(map.transform.Find("Dressing"),false);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var finish in Homes)AddHome(map.transform,root,finish,report);
            if(root.GetComponentsInChildren<Collider>().Length!=0||colliders.Count!=map.GetComponentsInChildren<Collider>(true).Length||
               colliders.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Masonry finishes changed gameplay collision.");
            AssetDatabase.SaveAssets();report.AppendLine("Only five named Eskinita homes; original solids, openings, details and collision unchanged.");
        }

        private static void AddHome(Transform map,Transform root,Finish choice,StringBuilder report)
        {
            string instance="Bahay_Rework_Bahay_"+choice.Lot;
            var home=map.Find("Dressing/Bahay/"+instance);if(home==null)throw new InvalidOperationException("Missing "+instance);
            var target=new GameObject("Finish_"+choice.Lot).transform;target.SetParent(root,false);target.SetPositionAndRotation(home.position,home.rotation);
            var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            int wallCount=0,roofCount=0;
            foreach(var filter in home.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;var p=mesh.vertices;var n=mesh.normals;var sourceUv=mesh.uv;var triangles=mesh.triangles;
                var matrix=target.worldToLocalMatrix*filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
                for(int t=0;t<triangles.Length;t+=3)
                {
                    int a=triangles[t];Vector3 normal=normalMatrix.MultiplyVector(n[a]).normalized;
                    bool wall=Mathf.Abs(sourceUv[a].x-.46875f)<.018f&&Mathf.Abs(normal.y)<.04f;
                    bool roof=Mathf.Abs(sourceUv[a].x-.09375f)<.018f&&sourceUv[a].y>=.5f&&normal.y>.2f;
                    bool leaves=Mathf.Abs(sourceUv[a].x-.21875f)<.018f&&sourceUv[a].y<.25f;
                    if(!wall&&!roof&&!leaves)continue;
                    int tile=leaves?6:roof?4+(normal.x<0?1:0):Mathf.Abs(normal.z)>.9f?(normal.z>0?0:1):(normal.x>0?2:3);
                    if(wall)
                    {
                        // Retain the original broad plaster/grain. The first visual draft
                        // covered it and flattened the wall; only the maintained base needs
                        // this extra coating, fitted around the existing real openings.
                        var polygon=new List<Vector3>();for(int corner=0;corner<3;corner++)polygon.Add(matrix.MultiplyPoint3x4(p[triangles[t+corner]]));
                        polygon=ClipBelow(polygon,choice.BaseHeight);
                        for(int fan=1;fan+1<polygon.Count;fan++)
                        {
                            foreach(var point in new[]{polygon[0],polygon[fan],polygon[fan+1]})
                            {
                                positions.Add(point+normal*.01f);normals.Add(normal);
                                uv.Add(Uv(tile,new Vector2(((Mathf.Abs(normal.z)>.9f?point.x:point.z)+5)/10,point.y/6)));
                                indices.Add(positions.Count-1);
                            }
                            wallCount++;
                        }
                        continue;
                    }
                    for(int corner=0;corner<3;corner++)
                    {
                        int i=triangles[t+corner];Vector3 point=matrix.MultiplyPoint3x4(p[i]);Vector3 faceNormal=normalMatrix.MultiplyVector(n[i]).normalized;
                        Vector2 at=leaves?new Vector2(.5f,sourceUv[i].y/.25f):roof?
                            (Mathf.Abs(faceNormal.x)>Mathf.Abs(faceNormal.z)?new Vector2((point.z+5)/10,(point.x+5)/10):new Vector2((point.x+5)/10,(point.z+5)/10)):
                            new Vector2(((Mathf.Abs(faceNormal.z)>.9f?point.x:point.z)+5)/10,point.y/6);
                        positions.Add(point+faceNormal*.01f);normals.Add(faceNormal);uv.Add(Uv(tile,at));indices.Add(positions.Count-1);
                    }
                    if(wall)wallCount++;if(roof)roofCount++;
                }
            }
            if(wallCount<12||roofCount<4)throw new InvalidOperationException("Source face classification failed for "+choice.Lot);
            var draft=new Mesh{name="Eskinita_masonry_"+choice.Lot};draft.SetVertices(positions);draft.SetNormals(normals);draft.SetUVs(0,uv);draft.SetTriangles(indices,0);draft.RecalculateBounds();
            string path=Folder+"/"+choice.Lot+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);Object.DestroyImmediate(draft);}
            target.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;var renderer=target.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=MaterialFor(choice,instance);renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            target.gameObject.isStatic=true;
            AirborneByDesign.Attach(target.gameObject,"Finite material finish copied from this retained home's actual solid faces, preserving original openings and shadow-casting body.");
            report.AppendLine(choice.Lot+": "+wallCount+" wall/"+roofCount+" roof triangles, "+positions.Count+" finish vertices, one renderer/material; base course "+choice.BaseHeight+"m.");
        }

        private static Vector2 Uv(int tile,Vector2 point)=>new Vector2(
            (tile%4*Pitch+Gutter+.5f+Mathf.Clamp01(point.x)*(Tile-1))/(Pitch*4),
            (tile/4*Pitch+Gutter+.5f+Mathf.Clamp01(point.y)*(Tile-1))/(Pitch*2));

        private static List<Vector3> ClipBelow(List<Vector3> points,float height)
        {
            var clipped=new List<Vector3>();
            for(int i=0;i<points.Count;i++)
            {
                Vector3 a=points[i],b=points[(i+1)%points.Count];bool inside=a.y<=height,next=b.y<=height;
                if(inside)clipped.Add(a);
                if(inside!=next)clipped.Add(Vector3.LerpUnclamped(a,b,(height-a.y)/(b.y-a.y)));
            }
            return clipped;
        }

        private static Material MaterialFor(Finish choice,string instance)
        {
            string roofName=EnvColourPass.RoofAtlases[EnvColourPass.RoofIndexFor(instance)];
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            source.LoadImage(File.ReadAllBytes("Assets/TumbangPreso/Resources/Models/roofs/"+roofName+".png"));
            Color tint=EnvColourPass.FacadeTints[EnvColourPass.FacadeIndexFor(instance)];
            Color paint=source.GetPixelBilinear(.46875f,.4f)*tint;Color metal=source.GetPixelBilinear(.09375f,.7f)*tint;
            var texture=new Texture2D(Pitch*4,Pitch*2,TextureFormat.RGB24,false);var pixels=new Color32[texture.width*texture.height];
            for(int tile=0;tile<8;tile++)for(int y=0;y<Pitch;y++)for(int x=0;x<Pitch;x++)
            {
                float u=Mathf.Clamp01((x-Gutter)/(float)(Tile-1)),v=Mathf.Clamp01((y-Gutter)/(float)(Tile-1));
                Color color=tile<4?Plaster(u*10-5,v*6,tile,choice,paint):tile<6?Roof(u*10-5,v*10-5,tile,choice,metal):
                    Color.Lerp(new Color(.19f,.29f,.14f),new Color(.40f,.53f,.28f),v);
                color.a=1;pixels[(tile/4*Pitch+y)*texture.width+tile%4*Pitch+x]=color;
            }
            texture.SetPixels32(pixels);texture.Apply();string texturePath=Folder+"/"+choice.Lot+".png";
            File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.npotScale=TextureImporterNPOTScale.None;importer.sRGBTexture=true;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            string path=Folder+"/"+choice.Lot+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("TumbangPreso/NearFade"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);material.color=Color.white;
            material.SetFloat("_SurfaceKind",0);material.SetFloat("_SurfaceVertexRoles",0);material.SetFloat("_Glossiness",.16f);
            EditorUtility.SetDirty(material);return material;
        }

        private static Color Plaster(float x,float y,int side,Finish choice,Color paint)
        {
            float wash=.038f*Mathf.Sin(x*1.23f+choice.WashX+side)*Mathf.Sin(y*1.7f+choice.WashY);
            wash+=.024f*Mathf.Sin(x*2.4f-y*.7f+side*1.3f+choice.BaseHeight*5);
            wash+=.013f*Mathf.Sin(x*37+y*.8f)*Mathf.Sin(y*31+side);
            Color color=paint*(1+wash);
            if(y<choice.BaseHeight)color=Color.Lerp(color,new Color(.49f,.46f,.39f),choice.BaseMix);
            // A small, finite lower-wall repaint, not damage stamped over every wall.
            if(side==1||side==2)
            {
                float edge=Mathf.Max(Mathf.Abs((x-choice.WashX)/.73f),Mathf.Abs((y-choice.WashY)/.29f));
                color=Color.Lerp(color,paint*1.06f,(1-Mathf.SmoothStep(.82f,1,edge))*.7f);
            }
            return color;
        }
        private static Color Roof(float x,float y,int side,Finish choice,Color paint)
        {
            float sheet=Mathf.Floor((x+5)/.82f);
            float tone=(sheet%3-1)*.024f;
            float rib=Mathf.Pow(.5f+.5f*Mathf.Cos(x/.205f*Mathf.PI*2),4)*.055f;
            float seam=Mathf.Abs(Mathf.Repeat(x+5,.82f)-.025f)<.028f?-.12f:0;
            float lap=Mathf.Abs(y-(side==4?.9f:-.7f))<.035f?-.08f:0;
            if(side==5&&x>choice.RoofRepair&&x<choice.RoofRepair+.82f)paint=Color.Lerp(paint,new Color(.55f,.57f,.56f),.26f);
            return paint*(1+tone+rib+seam+lap);
        }
    }
}
