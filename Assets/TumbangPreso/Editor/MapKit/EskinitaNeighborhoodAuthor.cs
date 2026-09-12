using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Measured residential lots using the retained solid house family.</summary>
    public static class EskinitaNeighborhoodAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/EskinitaNeighborhood";
        [Serializable] private sealed class Slot { public string id,retainedFamily,boundary;public string[] assets;public float proposedZ,width; }
        [Serializable] private sealed class Plan { public float proposedFacadeEdgeX,frontBoundaryX,crossStreetWidth,cornerFacadeEdgeZ;public float[] crossStreetCentreZ;public Slot[] slots; }

        public static void ClearPrevious(string map)
        {
            if(map!="Eskinita")return;
            var group=GameObject.Find(map+"/Dressing/NeighborhoodRework");if(group!=null)Object.DestroyImmediate(group);
            var houses=GameObject.Find(map+"/Dressing/Bahay");
            if(houses!=null)foreach(var child in houses.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.parent==houses.transform&&(t.name.StartsWith("Bahay_Rework_")||t.name.StartsWith("Sasakyan_Rework_"))).ToArray())
                Object.DestroyImmediate(child.gameObject);
        }

        public static void FinishLoadedScene(string map,StringBuilder report)
        {
            if(map!="Eskinita")return;
            Directory.CreateDirectory(Folder);
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/eskinita-neighborhood-plan-v1.json"));
            var dressing=GameObject.Find(map+"/Dressing").transform;
            var root=new GameObject("NeighborhoodRework").transform;root.SetParent(dressing,false);
            var houses=dressing.Find("Bahay");
            if(houses==null)throw new InvalidOperationException("Missing retained neighborhood source group.");
            var finish=GameObject.Find("NeighborhoodFinish");
            if(finish!=null)foreach(var child in finish.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.parent==finish.transform&&t.name.StartsWith("Frontage_Bahay_")).ToArray())Object.DestroyImmediate(child.gameObject);
            foreach(string group in new[]{"Bakod","Kanto","Likod","Malayo"})
            {var old=dressing.Find(group);if(old!=null)old.gameObject.SetActive(false);}
            var utility=dressing.Find("Kable");
            if(utility!=null)foreach(Transform t in utility)
                if(t.name.StartsWith("Sampay_"))t.gameObject.SetActive(false);

            int homeCount=0,carCount=0;
            foreach(var slot in plan.slots.OrderBy(s=>s.id,StringComparer.Ordinal))
            {
                var original=houses.Find(slot.id);
                if(original==null)throw new InvalidOperationException("Missing measured lot source: "+slot.id);
                original.gameObject.SetActive(false);
                bool home=slot.id.StartsWith("Bahay_");int side=slot.id.EndsWith("_W")?-1:1;
                string path=home?"Assets/TumbangPreso/Art/models/kits/city/building-type-"+slot.retainedFamily+".glb":slot.assets[0];
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset==null)throw new InvalidOperationException("Missing retained lot asset: "+path);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
                go.name=(home?"Bahay_Rework_":"Sasakyan_Rework_")+slot.id;
                go.transform.SetParent(houses,false);go.transform.localScale=Vector3.one*(home?5:1.75f);
                PlaceFront(go.transform,new Vector3(side*plan.proposedFacadeEdgeX,.1f,slot.proposedZ),side<0?90:270,side);
                go.isStatic=true;
                var bounds=BoundsOf(go);
                if(bounds.size.z>slot.width+.1f)throw new InvalidOperationException("Retained body outgrew its measured lot: "+slot.id);
                if(home)
                {
                    var detail=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/retained-house-details/retained-house-"+slot.retainedFamily+"-details.glb");
                    if(detail==null)throw new InvalidOperationException("Missing fitted detail: "+slot.retainedFamily);
                    var added=(GameObject)PrefabUtility.InstantiatePrefab(detail);added.name="HouseFinish_"+slot.id;
                    added.transform.SetParent(root,false);added.transform.SetPositionAndRotation(go.transform.position,go.transform.rotation);
                    if(slot.retainedFamily!="a"&&slot.retainedFamily!="c")
                        AirborneByDesign.Attach(added,"Window/cladding/terrace finishes are fitted to this retained solid house; source frame and wall positions are preserved.");
                    homeCount++;
                }
                else {CheckVehicleSupport(go,.1f);carCount++;}
                Plot(root,slot,side,plan.frontBoundaryX,plan.proposedFacadeEdgeX);
                report.AppendLine("  residential lot "+slot.id+" "+bounds);
            }
            BackAndCornerBlocks(root,houses,plan,report);
            Streets(root,plan);
            ReconcileStreetShops(dressing,plan);
            ReconcileLooseDressing(dressing,plan);
            ExtendDistantGround(dressing.parent);
            EskinitaUtilityAuthor.Finish(dressing,root,report);
            report.AppendLine("Eskinita neighborhood draft:"+homeCount+" retained-family homes,"+carCount+" grounded private parking plots; aligned closed fronts and rebuilt corner/back street layers. Spatial/art review remains open.");
        }

        private static void Plot(Transform root,Slot slot,int side,float boundary,float facade)
        {
            var plot=new GameObject("Plot_"+slot.id).transform;plot.SetParent(root,false);
            plot.SetPositionAndRotation(new Vector3(side*(boundary+.06f),.1f,slot.proposedZ),Quaternion.Euler(0,side<0?90:270,0));
            float w=slot.width+.35f,gate=slot.boundary=="closed-drive-gate"?3.1f:1.4f;
            bool masonry=slot.boundary=="masonry-and-grille";
            float baseHeight=masonry?.60f:.20f,totalHeight=masonry?1.22f:1.10f;
            var concrete=Mat("lot_concrete",new Color(.55f,.53f,.46f));
            var steel=Mat("lot_steel",new Color(.36f,.39f,.34f));
            var paving=Mat("yard_paving",new Color(.46f,.45f,.39f));
            Box(plot,"Private yard",new Vector3(0,-.003f,-(facade-boundary+2.4f)*.5f),new Vector3(w,.022f,facade-boundary+2.4f),paving);
            foreach(float edge in new[]{-1f,1f})
            {
                Box(plot,"Boundary pier",new Vector3(edge*(w*.5f-.11f),totalHeight*.5f,-.06f),new Vector3(.22f,totalHeight,.24f),concrete);
                float panel=(w-gate)*.5f-.22f;
                Box(plot,"Masonry boundary base",new Vector3(edge*(gate*.5f+panel*.5f),baseHeight*.5f,-.06f),new Vector3(panel,baseHeight,.18f),concrete);
                var rail=Box(plot,"Upper boundary rail",new Vector3(edge*(gate*.5f+panel*.5f),totalHeight-.045f,-.06f),new Vector3(panel,.09f,.09f),steel);
                AirborneByDesign.Attach(rail,"Upper rail spans the masonry piers and vertical grille bars.");
                int bars=Mathf.Max(2,Mathf.CeilToInt(panel/.30f));
                for(int i=0;i<bars;i++)Box(plot,"Boundary grille",new Vector3(edge*(gate*.5f+(i+.5f)*panel/bars),(baseHeight+totalHeight)*.5f,-.06f),new Vector3(.045f,totalHeight-baseHeight,.06f),steel);
            }
            foreach(float x in new[]{-gate*.5f,gate*.5f})Box(plot,"Gate jamb",new Vector3(x,totalHeight*.5f,-.06f),new Vector3(.12f,totalHeight,.12f),steel);
            foreach(float y in new[]{.14f,totalHeight-.06f})
            {
                var rail=Box(plot,"Closed gate rail",new Vector3(0,y,-.06f),new Vector3(gate,.10f,.10f),steel);
                AirborneByDesign.Attach(rail,"Closed gate leaf is supported by its hinges/jambs.");
            }
            int leaves=Mathf.CeilToInt(gate/.27f);
            for(int i=0;i<leaves;i++)
            {
                var bar=Box(plot,"Closed gate bar",new Vector3(-gate*.5f+(i+.5f)*gate/leaves,(.14f+totalHeight-.06f)*.5f,-.06f),new Vector3(.045f,totalHeight-.20f,.06f),steel);
                AirborneByDesign.Attach(bar,"Vertical bar is part of the closed hinged gate leaf.");
            }
        }

        private static void BackAndCornerBlocks(Transform root,Transform houses,Plan plan,StringBuilder report)
        {
            string[] kinds={"c","e","a","o"};int index=0;
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-20f,-12f,-4f,4f,12f,20f})
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/city/building-type-"+kinds[index%4]+".glb");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="Bahay_Rework_Back_"+index++;go.transform.SetParent(houses,false);go.transform.localScale=Vector3.one*5;
                PlaceFront(go.transform,new Vector3(side*19.8f,.1f,z),side<0?90:270,side);go.isStatic=true;
            }
            foreach(int side in new[]{-1,1})foreach(float x in new[]{-21.8f,-14.2f,-6.6f,1.0f,8.6f,16.2f,23.8f})
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/city/building-type-"+kinds[index%4]+".glb");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="Bahay_Rework_Corner_"+index++;go.transform.SetParent(houses,false);go.transform.localScale=Vector3.one*5;
                go.transform.rotation=Quaternion.Euler(0,side<0?0:180,0);var b=BoundsOf(go);
                float front=side<0?b.max.z:b.min.z;
                bool shopPocket=(side>0&&Mathf.Abs(x+14.2f)<.1f)||(side<0&&Mathf.Abs(x-16.2f)<.1f);
                go.transform.position=new Vector3(x-b.center.x,.1f-b.min.y,side*(plan.cornerFacadeEdgeZ+(shopPocket?3:0))-front);go.isStatic=true;
            }
            report.AppendLine("  neighborhood back/corner houses:"+index);
        }

        private static void Streets(Transform root,Plan plan)
        {
            var asphalt=Mat("continued_asphalt",new Color(.48f,.46f,.41f));
            asphalt.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/models/textures/asphalt.png");asphalt.mainTextureScale=new Vector2(12,2);EditorUtility.SetDirty(asphalt);
            var pavement=Mat("continued_pavement",new Color(.57f,.55f,.48f));
            foreach(float z in plan.crossStreetCentreZ)
            {
                Box(root,"Neighborhood cross-street",new Vector3(0,.089f,z),new Vector3(78,.02f,plan.crossStreetWidth),asphalt);
                foreach(float direction in new[]{-1f,1f})
                    Box(root,"Cross-street footpath",new Vector3(0,.095f,z+direction*(plan.crossStreetWidth*.5f+.80f)),new Vector3(78,.03f,1.60f),pavement);
            }
            foreach(float side in new[]{-1f,1f})
                Box(root,"Street continuation",new Vector3(0,.089f,side*23.1f),new Vector3(16.2f,.02f,6.8f),asphalt);
        }

        private static void ReconcileStreetShops(Transform dressing,Plan plan)
        {
            foreach(var t in dressing.GetComponentsInChildren<Transform>(true))
            {
                if(t.name!="SariSari_W"&&t.name!="SariSari_E")continue;
                bool north=t.name.EndsWith("_W");
                t.position=Vector3.zero;t.rotation=Quaternion.Euler(0,north?180:0,0);var b=BoundsOf(t.gameObject);
                t.position=new Vector3((north?-14.2f:16.2f)-b.center.x,.1f-b.min.y,(north?35.2f:-35.2f)-b.center.z);
            }
        }
        private static void ReconcileLooseDressing(Transform dressing,Plan plan)
        {
            var kalat=dressing.Find("Kalat");int chairs=0,benches=0,plants=0;
            var homes=plan.slots.Where(s=>s.id.StartsWith("Bahay_")).OrderBy(s=>s.id,StringComparer.Ordinal).ToArray();
            foreach(var t in dressing.GetComponentsInChildren<Transform>(true).OrderBy(t=>t.name,StringComparer.Ordinal))
            {
                if(t.parent==kalat&&(t.name.StartsWith("KalatTaas_")||t.name=="Kalat_19"||t.name=="Kalat_20"))
                {t.gameObject.SetActive(false);continue;}
                if(t.parent!=kalat&&t.parent?.name!="Puno")continue;
                var filter=t.GetComponentInChildren<MeshFilter>(true);if(filter==null)continue;
                string asset=AssetDatabase.GetAssetPath(filter.sharedMesh);
                if(asset.Contains("env_monobloc_chair"))
                {
                    if(chairs>=6){t.gameObject.SetActive(false);continue;}
                    bool north=chairs%2==0;int row=chairs++/2;
                    PlaceCentre(t,new Vector3(north?-16.2f:18.2f,.1f,(north?1:-1)*(35.0f+row*.62f)),north?270:90);
                }
                else if(asset.Contains("stall-bench"))
                {
                    if(benches>=2){t.gameObject.SetActive(false);continue;}
                    bool north=benches++==0;
                    PlaceCentre(t,new Vector3(north?-11.8f:13.8f,.1f,north?35.4f:-35.4f),north?90:270);
                }
                else if(asset.Contains("env_halaman_lata")||(t.parent?.name=="Puno"&&t.name.StartsWith("Halaman_")))
                {
                    if(plants>=homes.Length){t.gameObject.SetActive(false);continue;}
                    var home=homes[plants++];int side=home.id.EndsWith("_W")?-1:1;
                    PlaceCentre(t,new Vector3(side*8.68f,.1f,home.proposedZ-home.width*.28f),plants*31);
                }
            }
        }
        private static void ExtendDistantGround(Transform map)
        {
            // The paintings are correctly distant, but the old200m visual floor
            // ended before them and exposed their lower edges in wider views.
            // Extend only that visual base; playable collision remains unchanged.
            var floor=map.Find("Floor");if(floor==null)return;
            foreach(var filter in floor.GetComponentsInChildren<MeshFilter>())
            {
                var size=filter.sharedMesh.bounds.size;
                var scale=filter.transform.localScale;var parent=filter.transform.parent.lossyScale;
                scale.x=600/size.x/parent.x;scale.z=600/size.z/parent.z;filter.transform.localScale=scale;
            }
        }
        private static void PlaceCentre(Transform t,Vector3 anchor,float yaw)
        {
            t.gameObject.SetActive(true);t.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));
            var b=BoundsOf(t.gameObject);t.position=anchor-new Vector3(b.center.x,b.min.y,b.center.z);
        }
        private static void PlaceFront(Transform t,Vector3 anchor,float yaw,int side)
        {
            t.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));var b=BoundsOf(t.gameObject);
            float edge=side<0?b.max.x:b.min.x;
            t.position=new Vector3(anchor.x-edge,anchor.y-b.min.y,anchor.z-b.center.z);
        }
        private static void CheckVehicleSupport(GameObject vehicle,float ground)
        {
            var renderers=vehicle.GetComponentsInChildren<MeshRenderer>();
            var wheels=renderers.Where(r=>r.name.StartsWith("wheel-")).ToArray();
            if(wheels.Length!=4)throw new InvalidOperationException("Expected four retained wheels: "+vehicle.name);
            var body=renderers.FirstOrDefault(r=>r.name=="body");
            if(body==null)throw new InvalidOperationException("Missing retained vehicle chassis: "+vehicle.name);
            foreach(var wheel in wheels)
            {
                if(Mathf.Abs(wheel.bounds.min.y-ground)>.002f||!wheel.bounds.Intersects(body.bounds))
                    throw new InvalidOperationException("Vehicle wheel is not grounded/connected: "+vehicle.name+"/"+wheel.name);
            }
            foreach(var renderer in renderers.Where(r=>!r.name.StartsWith("wheel-")))
                AirborneByDesign.Attach(renderer.gameObject,"Retained vehicle part attached to its chassis; author verifies four grounded wheels intersect the body. Wheels retain the ordinary grounding check.");
        }
        private static Bounds BoundsOf(GameObject go)
        {var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static Material Mat(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",.12f);EditorUtility.SetDirty(m);return m;
        }
        private static GameObject Box(Transform root,string name,Vector3 at,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
            go.transform.localPosition=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go;
        }
    }
}
