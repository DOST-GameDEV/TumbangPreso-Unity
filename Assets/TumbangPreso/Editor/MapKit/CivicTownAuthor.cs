using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Civic paving and connected town blocks around the retained plaza.</summary>
    public static class CivicTownAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/CivicTown";
        [Serializable] private sealed class Road { public string axis;public float centre,width,from,to; }
        [Serializable] private sealed class FrontRows { public float eastFacadeX,westFacadeX;public float[] proposedLotZ; }
        [Serializable] private sealed class Plan { public Road[] roads;public FrontRows frontRows; }
        private static Plan ReadPlan()=>JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/bayan-town-plan-v1.json"));

        public static void ClearPrevious(string map)
        {
            if(map!="BayanPlaza")return;
            var previous=GameObject.Find(map+"/Dressing/CivicTown");if(previous!=null)Object.DestroyImmediate(previous);
            var terrain=GameObject.Find(map+"/TownGround");if(terrain!=null)Object.DestroyImmediate(terrain);
            var houses=GameObject.Find(map+"/Dressing/Bahay");
            if(houses!=null)
            {
                foreach(var t in houses.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==houses.transform&&t.name.StartsWith("Bahay_Civic_")).ToArray())
                    Object.DestroyImmediate(t.gameObject);
                if(houses.transform.childCount==0)Object.DestroyImmediate(houses);
            }
        }

        public static void PrepareExistingPlacement(string map)
        {
            if(map!="BayanPlaza")return;
            var dressing=GameObject.Find(map+"/Dressing").transform;
            var floor=dressing.parent.Find("Floor");
            if(floor==null)throw new InvalidOperationException("Missing retained base floor.");
            var atZero=floor.position;atZero.y=0;floor.position=atZero;
            var baseCollider=floor.GetComponentInChildren<BoxCollider>();
            if(baseCollider==null)throw new InvalidOperationException("Missing retained base-floor collider.");
            // The old visual top was.085 and collision top.1, both above the
            // dropped road. Keep it as a lower fallback beneath the new terrain.
            // Collider.bounds can still describe the previous physics pose
            // immediately after the transform reset. Read its authored box in
            // transform space, so repeated saves cannot toggle the floor height.
            float top=float.NegativeInfinity;
            foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
                top=Mathf.Max(top,baseCollider.transform.TransformPoint(baseCollider.center+
                    Vector3.Scale(baseCollider.size*.5f,new Vector3(x,y,z))).y);
            atZero.y=-.15f-top;floor.position=atZero;
            if(PrefabUtility.IsPartOfPrefabInstance(floor))PrefabUtility.RecordPrefabInstancePropertyModifications(floor);
            var far=dressing.Find("TreesFar");if(far!=null)far.gameObject.SetActive(false);
            var near=dressing.Find("TreesNear");
            if(near!=null)foreach(Transform tree in near)
            {
                // Remove the duplicated outer tree row where the new street runs.
                // Terminal trees cannot be moved behind the vehicles into traffic.
                if(new[]{"Puno_8","Puno_10","Puno_40","Puno_42","Puno_12","Puno_20","Puno_28","Puno_24","Puno_26"}.Contains(tree.name))
                {tree.gameObject.SetActive(false);continue;}
                var at=tree.position;
                if(Mathf.Abs(at.x)>14&&Mathf.Abs(at.z)<15)at.x=Mathf.Sign(at.x)*14.25f;
                tree.position=at;
            }
        }

        public static void FinishLoadedScene(string map,StringBuilder report)
        {
            if(map!="BayanPlaza")return;
            Directory.CreateDirectory(Folder);var plan=ReadPlan();
            var dressing=GameObject.Find(map+"/Dressing").transform;
            var root=new GameObject("CivicTown").transform;root.SetParent(dressing,false);
            foreach(string oldGroup in new[]{"Slab","Apron"})
            {
                var group=dressing.Find(oldGroup);
                if(group!=null)group.gameObject.SetActive(false);
            }
            var oldPaving=dressing.Find("MapFinalPass/PlazaStonePaving");
            if(oldPaving!=null)oldPaving.GetComponent<Renderer>().enabled=false;
            var belt=dressing.Find("Belt");if(belt!=null)belt.gameObject.SetActive(false);
            foreach(var t in dressing.GetComponentsInChildren<Transform>(true))
                if(t.name.StartsWith("Stall_")||new[]{"Clutter_4","Clutter_5","Clutter_6","Clutter_7","NaveSideVent"}.Contains(t.name)||
                    (t.parent!=null&&t.parent.name=="Ground"&&t.name.StartsWith("Plant_")))t.gameObject.SetActive(false);

            Ground(root,plan);
            CivicArchitecture(root);
            var houses=dressing.Find("Bahay");
            if(houses==null){houses=new GameObject("Bahay").transform;houses.SetParent(dressing,false);}
            int index=0;
            string[] kinds={"a","c","e","o","c"};
            foreach(int side in new[]{-1,1})
            for(int lot=0;lot<plan.frontRows.proposedLotZ.Length;lot++)
            {
                bool vendor=(side<0&&lot==1)||(side>0&&lot==3);
                float edge=side<0?plan.frontRows.westFacadeX:plan.frontRows.eastFacadeX;
                if(vendor)edge=side*26f;
                float z=plan.frontRows.proposedLotZ[lot];
                House(houses,root,kinds[(lot+(side>0?1:0))%kinds.Length],index++,side<0?90:270,
                    new Vector3(edge,.1f,z),side<0?"west":"east",true,report);
                if(vendor)Vendor(root,side<0?"fruit-cart":"frying-cart",new Vector3(side*24.4f,.1f,z+2.0f),side<0?90:270);
                // An alley and a second roof line give the town real depth.
                House(houses,root,kinds[(lot+2)%kinds.Length],index++,side<0?90:270,
                    new Vector3(side*33.5f,.1f,z+.35f),side<0?"west":"east",false,report);
            }
            foreach(int side in new[]{-1,1})
            foreach(float x in new[]{-43.5f,-35.5f,-27.5f,-10.8f,-3.6f,3.6f,10.8f,27.5f,35.5f,43.5f})
                House(houses,root,kinds[index%kinds.Length],index++,side<0?0:180,
                    new Vector3(x,.1f,side<0?-26.1f:35.4f),side<0?"south":"north",false,report);
            foreach(var at in new[]{new Vector3(-11.8f,.1f,20.5f),new Vector3(8,.1f,23.5f),
                new Vector3(-50,.1f,8),new Vector3(50,.1f,10),new Vector3(-49,.1f,-8),new Vector3(49,.1f,-8)})
                Tree(root,at);
            report.AppendLine("Bayan civic draft:6m texture repeat with1.5m slabs; connected four-street grid;"+index+" retained chunky houses in frontage/back/corner blocks;2 recessed vendor pockets. Whole-place critique remains open.");
        }

        private static bool InRoad(Plan plan,float x,float z)
        {
            return plan.roads.Any(r=>r.axis=="x"?Mathf.Abs(x-r.centre)<r.width*.5f&&z>=r.from&&z<=r.to:
                Mathf.Abs(z-r.centre)<r.width*.5f&&x>=r.from&&x<=r.to);
        }

        private static void Ground(Transform root,Plan plan)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/civic-concrete-albedo.png");
            if(texture==null)throw new InvalidOperationException("Missing original civic paving texture.");
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            if(!importer.mipmapEnabled||importer.wrapMode!=TextureWrapMode.Repeat||importer.anisoLevel!=4)
            {importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();}
            var paving=Mat("civic_paving",Color.white);paving.mainTexture=texture;paving.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(paving);
            var road=Mat("town_asphalt",new Color(.55f,.53f,.48f));
            road.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/models/textures/asphalt.png");EditorUtility.SetDirty(road);
            var earth=Mat("town_yards",new Color(.46f,.47f,.38f));
            foreach(var renderer in root.parent.parent.Find("Floor").GetComponentsInChildren<Renderer>())renderer.sharedMaterial=earth;
            var xs=new SortedSet<float>{-55,-24.1f,-21.5f,-15.5f,15.5f,21.5f,24.1f,55};
            var zs=new SortedSet<float>{-55,-26.1f,-23.5f,-17.5f,26.8f,32.8f,35.4f,55};
            var xv=xs.ToArray();var zv=zs.ToArray();var vertices=new List<Vector3>();
            var indices=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
            var streetCells=new bool[xv.Length-1,zv.Length-1];
            for(int x=0;x+1<xv.Length;x++)for(int z=0;z+1<zv.Length;z++)
            {
                float cx=(xv[x]+xv[x+1])*.5f,cz=(zv[z]+zv[z+1])*.5f;
                bool street=InRoad(plan,cx,cz);
                streetCells[x,z]=street;
                bool civic=Mathf.Abs(cx)<15.5f&&cz>-17.5f&&cz<26.8f;
                bool walk=plan.roads.Any(r=>r.axis=="x"?Mathf.Abs(cx-r.centre)<r.width*.5f+2.6f:Mathf.Abs(cz-r.centre)<r.width*.5f+2.6f);
                int group=street?0:civic||walk?1:2;
                Quad(vertices,indices[group],xv[x],xv[x+1],zv[z],zv[z+1],street?0:.102f);
            }
            // The road is genuinely lower than its footpath. Close the vertical
            // kerb faces rather than leaving a floating textured sheet at its edge.
            for(int x=0;x+1<streetCells.GetLength(0);x++)for(int z=0;z<streetCells.GetLength(1);z++)
                if(streetCells[x,z]!=streetCells[x+1,z])
                {
                    float at=xv[x+1],a=zv[z],b=zv[z+1];
                    if(streetCells[x,z]){float swap=a;a=b;b=swap;}
                    Face(vertices,indices[1],new Vector3(at,0,a),new Vector3(at,.102f,a),new Vector3(at,.102f,b),new Vector3(at,0,b));
                }
            for(int x=0;x<streetCells.GetLength(0);x++)for(int z=0;z+1<streetCells.GetLength(1);z++)
                if(streetCells[x,z]!=streetCells[x,z+1])
                {
                    float at=zv[z+1],a=xv[x],b=xv[x+1];
                    if(streetCells[x,z+1]){float swap=a;a=b;b=swap;}
                    Face(vertices,indices[1],new Vector3(a,0,at),new Vector3(a,.102f,at),new Vector3(b,.102f,at),new Vector3(b,0,at));
                }
            const float baseY=-.166f;
            for(int z=0;z+1<zv.Length;z++)
            {
                float leftTop=streetCells[0,z]?0:.102f,rightTop=streetCells[streetCells.GetLength(0)-1,z]?0:.102f;
                Face(vertices,indices[2],new Vector3(-55,baseY,zv[z+1]),new Vector3(-55,leftTop,zv[z+1]),new Vector3(-55,leftTop,zv[z]),new Vector3(-55,baseY,zv[z]));
                Face(vertices,indices[2],new Vector3(55,baseY,zv[z]),new Vector3(55,rightTop,zv[z]),new Vector3(55,rightTop,zv[z+1]),new Vector3(55,baseY,zv[z+1]));
            }
            for(int x=0;x+1<xv.Length;x++)
            {
                float southTop=streetCells[x,0]?0:.102f,northTop=streetCells[x,streetCells.GetLength(1)-1]?0:.102f;
                Face(vertices,indices[2],new Vector3(xv[x],baseY,-55),new Vector3(xv[x],southTop,-55),new Vector3(xv[x+1],southTop,-55),new Vector3(xv[x+1],baseY,-55));
                Face(vertices,indices[2],new Vector3(xv[x+1],baseY,55),new Vector3(xv[x+1],northTop,55),new Vector3(xv[x],northTop,55),new Vector3(xv[x],baseY,55));
            }
            Face(vertices,indices[2],new Vector3(-55,baseY,-55),new Vector3(55,baseY,-55),new Vector3(55,baseY,55),new Vector3(-55,baseY,55));
            var paint=Mat("civic_crossing",new Color(.70f,.68f,.61f));
            foreach(float side in new[]{-1f,1f})for(int i=0;i<8;i++)
            {
                float x=side*18.5f-2.6f+i*.74f;
                Quad(vertices,indices[3],x-.17f,x+.17f,8.5f,10.5f,.001f);
            }
            for(int i=0;i<8;i++)
            {
                float z=-23.1f+i*.74f;Quad(vertices,indices[3],3.5f,5.5f,z-.17f,z+.17f,.001f);
            }
            var terrain=MeshObject(root,"TownGround",vertices,indices,new[]{road,paving,earth,paint});
            // Ground belongs beside the physical floor, outside scenery fading.
            // This keeps the paving intact at the player's feet and retains its
            // material contract instead of treating the floor as an occluding prop.
            terrain.transform.SetParent(root.parent.parent,true);
            terrain.AddComponent<MeshCollider>().sharedMesh=terrain.GetComponent<MeshFilter>().sharedMesh;
            // Clear material approaches connect the actual civic doors to the plaza.
            var brick=Mat("civic_approach",new Color(.43f,.32f,.24f));
            Box(root,"Church entrance approach",new Vector3(-4.2f,.104f,13.1f),new Vector3(3,.006f,2.0f),brick);
            Box(root,"Hall entrance approach",new Vector3(7.8f,.104f,12.9f),new Vector3(4.0f,.006f,1.4f),brick);
        }

        private static void House(Transform houses,Transform root,string kind,int index,float yaw,Vector3 anchor,string side,bool details,StringBuilder report)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/city/building-type-"+kind+".glb");
            if(asset==null)throw new InvalidOperationException("Missing retained house "+kind);
            var house=(GameObject)PrefabUtility.InstantiatePrefab(asset);house.name="Bahay_Civic_"+index;
            house.transform.SetParent(houses,false);house.transform.localScale=Vector3.one*5;
            house.transform.rotation=Quaternion.Euler(0,yaw,0);var b=BoundsOf(house);
            bool alongX=side=="west"||side=="east";
            float front=side=="west"?b.max.x:side=="east"?b.min.x:side=="south"?b.max.z:b.min.z;
            house.transform.position=alongX?new Vector3(anchor.x-front,anchor.y-b.min.y,anchor.z-b.center.z):
                new Vector3(anchor.x-b.center.x,anchor.y-b.min.y,anchor.z-front);
            house.isStatic=true;
            if(details&&(kind=="a"||kind=="c"))
            {
                var detailAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/retained-house-details/retained-house-"+kind+"-details.glb");
                if(detailAsset==null)throw new InvalidOperationException("Missing fitted house detail "+kind);
                var addition=(GameObject)PrefabUtility.InstantiatePrefab(detailAsset);addition.name="FittedTownDetails_"+index;
                addition.transform.SetParent(root,false);addition.transform.SetPositionAndRotation(house.transform.position,house.transform.rotation);
                addition.isStatic=true;
            }
            report.AppendLine("  town house "+index+" "+kind+" "+BoundsOf(house));
        }

        private static void Vendor(Transform root,string kind,Vector3 anchor,float yaw)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/street-stalls/"+kind+".glb");
            if(asset==null)throw new InvalidOperationException("Missing original vendor "+kind);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="CivicVendor_"+kind;go.transform.SetParent(root,false);
            go.transform.SetPositionAndRotation(anchor,Quaternion.Euler(0,yaw,0));var b=BoundsOf(go);go.transform.position+=Vector3.up*(anchor.y-b.min.y);go.isStatic=true;
        }

        private static void Tree(Transform root,Vector3 anchor)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/urban-trees/plaza-shade.glb");
            if(asset==null)throw new InvalidOperationException("Missing original broadleaf source.");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="CivicGardenTree";go.transform.SetParent(root,false);
            var b=BoundsOf(go);go.transform.localScale=Vector3.one*(6.2f/b.size.y);b=BoundsOf(go);
            go.transform.position=anchor-new Vector3(b.center.x,b.min.y,b.center.z);go.isStatic=true;
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                {
                    string name=m.name.Contains("bark")?"tree_bark":m.name.Contains("new growth")?"tree_new_growth":
                        m.name.Contains("shaded foliage")?"tree_shaded_foliage":"tree_foliage";
                    var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/MapFinalPass/"+name+".mat");
                    if(material==null)throw new InvalidOperationException("Missing shared broadleaf material: "+name);
                    return material;
                }).ToArray();
        }

        private static void CivicArchitecture(Transform root)
        {
            var stone=Mat("civic_window_stone",new Color(.65f,.61f,.51f));
            var wood=Mat("civic_window_timber",new Color(.29f,.23f,.16f));
            foreach(float z in new[]{18.75f,22.25f})
            {
                ArchedWindow(root,new Vector3(-6.995f,2.35f,z),270,stone,wood);
                ArchedWindow(root,new Vector3(-1.405f,2.35f,z),90,stone,wood);
            }
            ArchedWindow(root,new Vector3(-4.2f,2.5f,25.195f),0,stone,wood);
            Box(root,"Nave rear stone footing",new Vector3(-4.2f,.277f,25.24f),new Vector3(5.65f,.35f,.23f),stone);
            foreach(float x in new[]{3.3f,6.3f,9.3f,12.3f})
            {
                var window=new GameObject("Hall rear shuttered window").transform;window.SetParent(root,false);
                window.position=new Vector3(x,4.6f,19.595f);
                AirborneByDesign.Attach(window.gameObject,"Closed framed window fixed into the municipal rear wall.");
                Box(window,"Window backing",new Vector3(0,0,.025f),new Vector3(1.1f,1.45f,.05f),wood);
                foreach(float side in new[]{-1f,1f})
                {
                    Box(window,"Stone jamb",new Vector3(side*.55f,0,.085f),new Vector3(.15f,1.6f,.17f),stone);
                    Box(window,"Stone head or sill",new Vector3(0,side*.725f,.085f),new Vector3(1.25f,.15f,.17f),stone);
                }
                for(int i=0;i<7;i++)Box(window,"Timber louver",new Vector3(0,-.57f+i*.19f,.09f),new Vector3(.95f,.10f,.10f),wood);
            }
            var door=new GameObject("Hall rear service door").transform;door.SetParent(root,false);door.position=new Vector3(7.8f,.102f,19.595f);
            Box(door,"Closed timber door",new Vector3(0,1.04f,.045f),new Vector3(1.12f,2.08f,.09f),wood);
            foreach(float x in new[]{-.65f,.65f})Box(door,"Door stone jamb",new Vector3(x,1.13f,.085f),new Vector3(.20f,2.26f,.17f),stone);
            var head=Box(door,"Door stone lintel",new Vector3(0,2.25f,.085f),new Vector3(1.5f,.22f,.17f),stone);
            AirborneByDesign.Attach(head,"Lintel spans the two grounded door jambs.");
        }

        private static void ArchedWindow(Transform root,Vector3 at,float yaw,Material stone,Material wood)
        {
            var mount=new GameObject("Nave arched shuttered window").transform;mount.SetParent(root,false);
            mount.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
            var outer=new List<Vector3>{new Vector3(-.7f,0,0),new Vector3(.7f,0,0)};
            var inner=new List<Vector3>{new Vector3(-.55f,.15f,0),new Vector3(.55f,.15f,0)};
            for(int i=0;i<=12;i++)
            {
                float angle=i*Mathf.PI/12;
                outer.Add(new Vector3(Mathf.Cos(angle)*.7f,1.1f+Mathf.Sin(angle)*.7f,0));
                inner.Add(new Vector3(Mathf.Cos(angle)*.55f,1.1f+Mathf.Sin(angle)*.55f,0));
            }
            var v=new List<Vector3>();var ix=new[]{new List<int>(),new List<int>()};
            for(int i=0;i<outer.Count;i++)
            {
                int j=(i+1)%outer.Count;var front=Vector3.forward*.15f;
                Face(v,ix[0],outer[i]+front,outer[j]+front,inner[j]+front,inner[i]+front);
                Face(v,ix[0],outer[i],outer[j],outer[j]+front,outer[i]+front);
                Face(v,ix[0],inner[i]+front,inner[j]+front,inner[j],inner[i]);
                int n=v.Count;v.AddRange(new[]{new Vector3(0,.8f,.035f),inner[i]+Vector3.forward*.035f,inner[j]+Vector3.forward*.035f});
                ix[1].AddRange(new[]{n,n+1,n+2});
            }
            var frame=MeshObject(mount,"CivicArchedWindow",v,ix,new[]{stone,wood});
            AirborneByDesign.Attach(mount.gameObject,"Arched stone frame and closed shutters are fixed into the nave wall between its buttresses.");
            for(int i=0;i<6;i++)
            {
                float y=.32f+i*.245f;
                float aboveSpring=Mathf.Max(0,y+.0575f-1.1f);
                float half=Mathf.Min(.50f,Mathf.Sqrt(Mathf.Max(0,.55f*.55f-aboveSpring*aboveSpring))-.03f);
                Box(mount,"Arched shutter louver",new Vector3(0,y,.095f),new Vector3(half*2,.115f,.11f),wood);
            }
        }

        private static Material Mat(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",.12f);EditorUtility.SetDirty(m);return m;
        }
        private static GameObject Box(Transform parent,string name,Vector3 at,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go;
        }
        private static Bounds BoundsOf(GameObject go)
        {var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static void Quad(List<Vector3> vertices,List<int> indices,float left,float right,float bottom,float top,float y)
        {
            int n=vertices.Count;vertices.AddRange(new[]{new Vector3(left,y,bottom),new Vector3(left,y,top),new Vector3(right,y,top),new Vector3(right,y,bottom)});
            indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        private static void Face(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        private static GameObject MeshObject(Transform root,string name,List<Vector3> vertices,List<int>[] indices,Material[] materials)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.subMeshCount=indices.Length;
            for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
            mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x/6f,v.z/6f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
            var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=materials;go.isStatic=true;return go;
        }
    }
}
