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
    public static class RooftopStreetLifeAuthor
    {
        public const string RootName="Street activity below roof";
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/StreetContext";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();
            FinishLoadedScene(report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/roof-street-context");File.WriteAllText("Logs/roof-street-context/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var city=map.transform.Find("Dressing/Metro rooftops");
            if(city==null)throw new InvalidOperationException("Missing retained rooftop city.");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=city.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var root=new GameObject(RootName).transform;root.SetParent(city,false);
            float roadTop=city.Find("Connected city roads").GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y);
            Park(root,"East parked tricycle", "eskinita-street/eskinita-passenger-tricycle",24.75f,-8,0,roadTop,report);
            Park(root,"West parked tricycle", "terminal/bayan-passenger-tricycle",-24.75f,10,180,roadTop,report);
            var centres=new List<Vector3>();var crossings=new List<Vector3>();
            void Quad(List<Vector3> points,float x,float z,float w,float d)
            {
                float y=roadTop+.003f;
                foreach(var p in new[]{new Vector3(x-w/2,y,z-d/2),new Vector3(x-w/2,y,z+d/2),new Vector3(x+w/2,y,z+d/2),new Vector3(x+w/2,y,z-d/2)})points.Add(root.InverseTransformPoint(p));
            }
            foreach(float x in new[]{-23.5f,23.5f})
            {
                for(int i=-20;i<=20;i++)
                {
                    float z=i*4;
                    if(Mathf.Abs(Mathf.Abs(z)-26.5f)<4||Mathf.Abs(Mathf.Abs(z)-52)<4||Mathf.Abs(Mathf.Abs(z)-22)<3)continue;
                    Quad(centres,x,z,.12f,2);
                }
                foreach(float z in new[]{-22f,22f})
                    for(int stripe=0;stripe<6;stripe++)Quad(crossings,x,z+(stripe-2.5f)*.48f,4.8f,.28f);
            }
            foreach(float z in new[]{-26.5f,26.5f})
                for(int i=-20;i<=20;i++)
                {
                    float x=i*4;if(Mathf.Abs(Mathf.Abs(x)-23.5f)<4||Mathf.Abs(Mathf.Abs(x)-52)<4)continue;
                    Quad(centres,x,z,2,.12f);
                }
            SavePaint(root,"Faded centre dashes",centres,new Color(.75f,.61f,.30f));
            SavePaint(root,"Corner crossing blocks",crossings,new Color(.78f,.76f,.68f));
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(pair=>pair.Key==null||pair.Key.bounds!=pair.Value))
                throw new InvalidOperationException("Street scenery changed existing collision.");
            report.AppendLine($"Two retained-model parked tricycles; {centres.Count/4} centre dashes and four crossings; two paint renderers, no new collision/shadow casters. Existing roads, facades and roof retained.");
        }
        private static void Park(Transform parent,string name,string model,float x,float z,float yaw,float roadTop,StringBuilder report)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/"+model+".glb");
            if(source==null)throw new InvalidOperationException("Missing retained tricycle "+model);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localScale=Vector3.one;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            Bounds BoundsOf(){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
            var bounds=BoundsOf();if(bounds.size.x>bounds.size.z){go.transform.rotation=Quaternion.Euler(0,yaw+90,0);bounds=BoundsOf();}
            go.transform.position+=new Vector3(x-bounds.center.x,roadTop-bounds.min.y,z-bounds.center.z);bounds=BoundsOf();
            float street=x>0?23.5f:-23.5f;
            if(bounds.min.x<street-2.85f||bounds.max.x>street+2.85f||Mathf.Abs(bounds.min.y-roadTop)>.002f||bounds.size.z>4.5f)
                throw new InvalidOperationException(name+" does not fit its supported street-edge site: "+bounds);
            foreach(var collider in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            foreach(var node in go.GetComponentsInChildren<Transform>())node.gameObject.isStatic=true;
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Parked with original tyre contact on the retained lower street; background scenery below the playing roof.");
            report.AppendLine(name+": "+bounds+", road top "+roadTop);
        }
        private static void SavePaint(Transform root,string name,List<Vector3> points,Color colour)
        {
            var indices=new List<int>();for(int i=0;i<points.Count;i+=4)indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string stem=Folder+"/"+name.Replace(" ","");var saved=AssetDatabase.LoadAssetAtPath<Mesh>(stem+".asset");
            if(saved==null){AssetDatabase.CreateAsset(mesh,stem+".asset");saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var material=AssetDatabase.LoadAssetAtPath<Material>(stem+".mat");
            var paintShader=Shader.Find("TumbangPreso/CourtSurface");
            if(material==null){material=new Material(paintShader);AssetDatabase.CreateAsset(material,stem+".mat");}
            material.shader=paintShader;material.SetColor("_Medium",colour);material.SetFloat("_Mode",0);material.SetFloat("_Weight",.3f);
            // Reuse the existing depth-biased ground paint. Thin standard-shader
            // planes can fight the road at this high camera's depth precision.
            material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.renderQueue=-1;
            material.SetOverrideTag("RenderType","Transparent");EditorUtility.SetDirty(material);
            var go=new GameObject(name);go.transform.SetParent(root,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=saved;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Paint just above the retained asphalt surface, aligned to the existing street grid.");
        }
    }
}
