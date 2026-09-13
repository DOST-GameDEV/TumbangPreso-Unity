using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Small maintained resident corners, outside the court and pool access.</summary>
    public static class RooftopResidentsAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/Residents";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath,OpenSceneMode.Single);
            FinishLoadedScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            EditorApplication.Exit(MapGeometryCheck.Execute(true)?0:1);
        }
        public static void FinishLoadedScene()
        {
            var dressing=GameObject.Find("SaBubong/Dressing");
            if(dressing==null)throw new InvalidOperationException("Sa roof dressing is missing");
            var old=dressing.transform.Find("Resident garden corners");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var shade=dressing.transform.Find("Residents shade");
            var oldTablePlant=shade.Find("Resident small-aloe");if(oldTablePlant!=null)Object.DestroyImmediate(oldTablePlant.gameObject);
            Directory.CreateDirectory(Folder);
            foreach(string id in new[]{"pail-sansevieria","terracotta-broadleaf","small-aloe"})
            {
                string target=Folder+"/"+id+".glb",source="MapSource/environment/roof-residents/study-v1/"+id+".glb";
                if(!File.Exists(target)||!File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(source)))File.Copy(source,target,true);
            }
            AssetDatabase.Refresh();
            var root=new GameObject("Resident garden corners").transform;root.SetParent(dressing.transform,false);
            Plant(root,"pail-sansevieria",new Vector3(-13.25f,.101f,-9.8f),25,1);
            Plant(root,"terracotta-broadleaf",new Vector3(-13.3f,.101f,-8.9f),-35,1.1f);
            Plant(root,"small-aloe",new Vector3(-13.08f,.101f,-10.65f),75,1);
            Plant(root,"pail-sansevieria",new Vector3(5.08f,.101f,17.1f),-80,1.05f);
            Plant(root,"terracotta-broadleaf",new Vector3(10.92f,.101f,17.55f),-15,1);
            Plant(root,"small-aloe",new Vector3(11.42f,.101f,17.86f),60,1.1f);
            // A small table plant belongs on the shade table's actual top.
            Plant(shade,"small-aloe",new Vector3(-10.67f,.98f,-7.15f),20,.82f);
            Basins(root);
            Mural(root);
        }
        private static void Plant(Transform root,string id,Vector3 at,float yaw,float scale)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+id+".glb");
            if(asset==null)throw new InvalidOperationException("Missing resident plant "+id);
            var plant=(GameObject)PrefabUtility.InstantiatePrefab(asset);plant.name="Resident "+id;
            plant.transform.SetParent(root,false);plant.transform.position=at;plant.transform.rotation=Quaternion.Euler(0,yaw,0);plant.transform.localScale=Vector3.one*scale;
            var renderers=plant.GetComponentsInChildren<Renderer>();float bottom=renderers.Min(r=>r.bounds.min.y);
            plant.transform.position+=Vector3.up*(at.y-bottom);
            // Only the pot obstructs feet. Leaves never snag a retrieval route.
            if(at.y<.2f){var shape=plant.AddComponent<BoxCollider>();shape.center=new Vector3(0,.15f,0);shape.size=new Vector3(.35f,.30f,.35f);}
        }
        private static Material Mat(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Glossiness",.12f);EditorUtility.SetDirty(material);return material;
        }
        private static void Basins(Transform root)
        {
            var wash=Mat("FadedWashBasin",new Color(.45f,.52f,.43f));
            var group=new GameObject("Stacked wash basins").transform;group.SetParent(root,false);group.localPosition=new Vector3(-5.2f,.101f,20.2f);
            // Two nested broad shallow basins next to the drying corner, with
            // visible hollow interiors and substantial rolled rims.
            for(int layer=0;layer<2;layer++)
            {
                var mesh=new Mesh{name="Nested basin"};var vertices=new Vector3[60];var triangles=new System.Collections.Generic.List<int>();
                float[] radii={.25f,.34f,.36f,.32f,.235f};float[] ys={0,.14f,.16f,.14f,.025f};
                for(int ring=0;ring<5;ring++)for(int i=0;i<12;i++){float a=i*Mathf.PI/6;vertices[ring*12+i]=new Vector3(Mathf.Cos(a)*radii[ring],ys[ring]+layer*.07f,Mathf.Sin(a)*radii[ring]);}
                for(int ring=0;ring<4;ring++)for(int i=0;i<12;i++){int a=ring*12+i,b=ring*12+(i+1)%12;triangles.AddRange(new[]{a,a+12,b,b,a+12,b+12});}
                for(int i=1;i<11;i++)triangles.AddRange(new[]{48,48+i+1,48+i});
                mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();
                var part=new GameObject("Nested basin "+layer);part.transform.SetParent(group,false);
                part.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"Basin"+layer);part.AddComponent<MeshRenderer>().sharedMaterial=wash;
            }
        }
        private static void Mural(Transform root)
        {
            // Original resident-painted growing leaves on the stairhouse side.
            // Flush vector mesh colour, no framed sign or duplicated shop typography.
            var group=new GameObject("Resident leaf mural").transform;group.SetParent(root,false);
            group.localPosition=new Vector3(10.303f,.8f,18.25f);group.localRotation=Quaternion.Euler(0,90,0);
            Physics.SyncTransforms();
            if(!Physics.Raycast(group.position+Vector3.right*.05f,Vector3.left,out var wall,.1f,~0,QueryTriggerInteraction.Ignore)
                ||Mathf.Abs(wall.point.x-10.3f)>.005f)throw new InvalidOperationException("Resident mural misses the stairhouse wall");
            AirborneByDesign.Attach(group.gameObject,"Painted directly on the stairhouse east wall, with a measured3mm surface offset; not a free-standing sign.");
            var colour=Mat("MuralClay",new Color(.58f,.35f,.23f));
            Polygon(group,"Painted pot",new[]{new Vector2(-.25f,0),new Vector2(.25f,0),new Vector2(.34f,.42f),new Vector2(-.34f,.42f)},colour);
            var leaves=Mat("MuralLeaf",new Color(.37f,.47f,.32f));
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*.26f;float height=.75f+(i%2)*.25f;
                Polygon(group,"Painted leaf "+i,new[]{new Vector2(0,.38f),new Vector2(x-.12f,height-.08f),new Vector2(x,height+.15f),new Vector2(x+.12f,height-.06f)},leaves);
            }
        }
        private static void Polygon(Transform parent,string name,Vector2[] points,Material material)
        {
            var mesh=new Mesh{name=name};mesh.vertices=points.Select(p=>new Vector3(p.x,p.y,0)).ToArray();
            float area=0;for(int i=0;i<points.Length;i++){var next=points[(i+1)%points.Length];area+=points[i].x*next.y-next.x*points[i].y;}
            var triangles=new System.Collections.Generic.List<int>();for(int i=1;i<points.Length-1;i++)triangles.AddRange(area>0?new[]{0,i,i+1}:new[]{0,i+1,i});
            mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();
            var part=new GameObject(name);part.transform.SetParent(parent,false);
            part.AddComponent<MeshFilter>().sharedMesh=Save(mesh,name.Replace(" ",""));var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static Mesh Save(Mesh source,string name)
        {
            string path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){AssetDatabase.CreateAsset(source,path);return source;}
            EditorUtility.CopySerialized(source,mesh);Object.DestroyImmediate(source);EditorUtility.SetDirty(mesh);return mesh;
        }
    }
}
