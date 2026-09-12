using System;
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
    /// <summary>Measured storefronts built around the retained chunky building bodies.</summary>
    public static class MapPlaceAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/PlaceRework";
        [Serializable] private sealed class Shop
        { public string id; public int side,palette; public float facadeX,z,width,depth; }
        [Serializable] private sealed class Plan
        { public Shop[] shops; public Vendor[] vendors; public float pavementY; public float[] crossingZ; }
        [Serializable] private sealed class Vendor
        { public string id,kind; public int side; public float x,z,width,depth; }

        public static void ClearPrevious(string map)
        {
            var previous=GameObject.Find(map+"/Dressing/PlaceRework");
            if(previous!=null)Object.DestroyImmediate(previous);
        }

        public static void PrepareExistingPlacement(string map)
        {
            if(map!="IlalimNgTulay")return;
            var root=GameObject.Find(map+"/Dressing");
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t.name=="PavementTree_W"||t.name=="PavementPlanter_W")
                    t.position=new Vector3(-9.8f,t.position.y,20.5f);
        }

        public static void FinishLoadedScene(string map,StringBuilder report)
        {
            if(map!="IlalimNgTulay")return;
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/ilalim-place-plan-v1.json"));
            var dressing=GameObject.Find(map+"/Dressing").transform;
            var root=new GameObject("PlaceRework").transform;root.SetParent(dressing,false);
            var all=dressing.GetComponentsInChildren<Transform>(true);
            PlaceUtilityPoles(all,report);
            int west=0,east=0;
            foreach(var shop in plan.shops)
            {
                string sourceName="Shophouse_"+(shop.side<0?"W":"E")+(shop.side<0?west++:east++);
                var source=all.FirstOrDefault(t=>t.name==sourceName&&t.parent.name=="Gilid");
                if(source==null)throw new InvalidOperationException("Missing retained shop body: "+sourceName);
                var body=Object.Instantiate(source.gameObject,root);
                body.name="RetainedShop_"+shop.id;body.SetActive(true);
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,shop.side<0?90:270,0));
                var bounds=BoundsOf(body);
                var scale=body.transform.localScale;scale.x*=shop.width/bounds.size.z;body.transform.localScale=scale;
                bounds=BoundsOf(body);
                float front=shop.side<0?bounds.max.x:bounds.min.x;
                body.transform.position=new Vector3(shop.side*14.45f-front,.15f-bounds.min.y,shop.z-bounds.center.z);
                CarryBuildingAttachments(all,source,body.transform,sourceName.Substring("Shophouse_".Length));
                source.gameObject.SetActive(false);
                BuildFrontage(root,shop,plan.pavementY);
            }

            // Keep the retained structural style, but give the street floor a real
            // retail threshold. Legacy scattered objects remain preserved inactive.
            foreach(var t in all)
            {
                if(t==null)continue;
                if(t.name=="Karatula" || t.name.StartsWith("Pisonet_Kiosk_") || t.name=="PC_Express_Store" ||
                   t.name=="Cargo_Tricycle_Boxes" || t.name=="Street_Pares_Cart" || t.name=="Pares_Parasol" ||
                   t.name=="Sari_Sari_Store" || t.name=="Parked_Tricycle" || t.name=="PavementHedge_E" ||
                   t.name=="ShopfrontDumpster" || t.name=="BridgeHoop" || t.name.StartsWith("DeliveryBox_") ||
                   t.name.StartsWith("Sampay_Street_") ||
                   t.name.StartsWith("BackShop_") || t.name=="FormerHazardProps" ||
                   t.name.StartsWith("Crossing_") ||
                   (t.parent.name=="Tindahan"&&t.name.Contains("awning")))t.gameObject.SetActive(false);
            }
            BuildPisonets(root,plan.shops.First(s=>s.id=="Pisonet"),plan.pavementY);
            BuildVendors(root,plan.vendors,plan.pavementY,report);
            MoveSingleCord(root,all,plan.pavementY);
            Crossings(root,plan.crossingZ);
            Notices(root);
            report.AppendLine("Ilalim place draft:11 retained building bodies with substantial recessed frontages;2 indoor pisonets;4 original street vendors; new sign faces; old crossing ladders and former-hazard decoration retired. Whole-place critique remains open.");
        }

        private static void CarryBuildingAttachments(Transform[] all,Transform source,Transform body,string key)
        {
            var roofBounds=BoundsOf(body.gameObject);
            foreach(var attachment in all)
            {
                if(attachment.parent==null||attachment.parent.name!="Bubong")continue;
                if(!(attachment.name.EndsWith("_"+key,StringComparison.Ordinal)||
                     attachment.name.Contains("_"+key+"_")))continue;
                var carried=Object.Instantiate(attachment.gameObject,body);
                carried.name="Carried_"+attachment.name;carried.SetActive(true);
                carried.transform.localPosition=source.InverseTransformPoint(attachment.position);
                carried.transform.localRotation=Quaternion.Inverse(source.rotation)*attachment.rotation;
                var scale=source.lossyScale;var other=attachment.lossyScale;
                carried.transform.localScale=new Vector3(other.x/scale.x,other.y/scale.y,other.z/scale.z);
                attachment.gameObject.SetActive(false);
                if(attachment.name.StartsWith("Sampay_Roof_"))
                {
                    var before=BoundsOf(carried);
                    var axis=carried.transform.InverseTransformDirection(Vector3.forward);
                    int component=Mathf.Abs(axis.x)>Mathf.Abs(axis.z)?0:2;
                    var fit=carried.transform.localScale;
                    fit[component]*=Mathf.Min(1,(roofBounds.size.z-.8f)/before.size.z);
                    carried.transform.localScale=fit;
                    var after=BoundsOf(carried);
                    carried.transform.position+=new Vector3(roofBounds.center.x-after.center.x,0,roofBounds.center.z-after.center.z);
                    after=BoundsOf(carried);
                    float baseY=roofBounds.max.y-.12f,top=after.max.y+.04f;
                    var steel=Mat("roof_laundry_steel",new Color(.31f,.35f,.30f));
                    foreach(float z in new[]{after.min.z,after.max.z})
                    {
                        var post=Box(body,"Roof laundry support",Vector3.zero,Vector3.one,steel,false,"Laundry support is seated on this building's roof and reaches its line.");
                        post.transform.SetPositionAndRotation(new Vector3(after.center.x,(baseY+top)*.5f,z),Quaternion.identity);
                        post.transform.localScale=new Vector3(.12f/body.lossyScale.z,(top-baseY)/body.lossyScale.y,.12f/body.lossyScale.x);
                    }
                }
            }
        }

        private static void PlaceUtilityPoles(Transform[] all,StringBuilder report)
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/kits/roads/electricity-pole-single.glb");
            if(original==null)throw new InvalidOperationException("Missing retained utility-pole source.");
            var originalMaterials=original.GetComponentInChildren<MeshRenderer>().sharedMaterials;
            var streetMaterials=originalMaterials.Select((material,index)=>
            {
                string path=Folder+"/utility_original_aged_"+index+".mat";
                var aged=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(aged==null){aged=new Material(material);AssetDatabase.CreateAsset(aged,path);}
                else aged.CopyPropertiesFromMaterial(material);
                aged.name="utility_original_aged_"+index;
                // The full-sun source timber was too orange beside role cues.
                // A subdued neutral tint keeps the source texture and material
                // separation while bringing it into the aged street palette.
                foreach(string property in new[]{"baseColorFactor","_BaseColor","_Color"})
                    if(aged.HasProperty(property))aged.SetColor(property,new Color(.58f,.63f,.68f,1));
                EditorUtility.SetDirty(aged);return aged;
            }).ToArray();
            int count=0;
            foreach(var pole in all.Where(t=>t.name.StartsWith("SidewalkPole_")))
            {
                // The old X10.65 shaft cut through shop walls and roof slabs.
                // Keep the longitudinal service rhythm, but move the utility row
                // into its own outer-pavement strip before rebuilding the wires.
                var at=pole.position;at.x=Mathf.Sign(at.x)*9.80f;pole.position=at;
                foreach(var filter in pole.GetComponentsInChildren<MeshFilter>())
                {
                    // Preserve the source's timber/metal material relationships.
                    // The commercial warm atlas painted this shaft pink/yellow
                    // in bands, making utility infrastructure look decorative.
                    filter.GetComponent<MeshRenderer>().sharedMaterials=streetMaterials;
                    var collider=filter.GetComponent<MeshCollider>();
                    if(collider==null)collider=filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh=filter.sharedMesh;
                }
                count++;
            }
            report.AppendLine("  utility poles: "+count+" retained at absX9.80 with mesh collision; conductor endpoints regenerated afterward.");
        }

        private static void BuildFrontage(Transform root,Shop shop,float pavement)
        {
            // Local +Z faces the street, with the boundary at Z0. The retained
            // building body sits behind this deliberately substantial shop room.
            var room=new GameObject("Frontage_"+shop.id).transform;room.SetParent(root,false);
            room.SetPositionAndRotation(new Vector3(shop.facadeX,pavement,shop.z),Quaternion.Euler(0,shop.side<0?90:270,0));
            float w=shop.width;
            var plaster=Mat("shop_plaster_"+shop.palette,shop.palette==0?new Color(.68f,.64f,.53f):
                shop.palette==1?new Color(.52f,.61f,.56f):new Color(.64f,.58f,.55f));
            var baseMat=Mat("shop_concrete",new Color(.42f,.43f,.40f));
            var frame=Mat("shop_dark_frame",new Color(.24f,.27f,.24f));
            var floor=Mat("shop_floor",new Color(.52f,.49f,.41f));
            Box(room,"Floor",new Vector3(0,-.108f,-1.86f),new Vector3(w,.346f,3.92f),floor);
            Box(room,"Rear wall",new Vector3(0,1.38f,-3.62f),new Vector3(w,.0f+2.76f,.32f),plaster);
            foreach(float side in new[]{-1f,1f})
            {
                Box(room,"Side wall",new Vector3(side*(w/2-.16f),1.38f,-1.84f),new Vector3(.32f,2.76f,3.84f),plaster);
                Box(room,"Substantial front pier",new Vector3(side*(w/2-.18f),1.36f,.03f),new Vector3(.36f,2.72f,.42f),baseMat);
            }
            Box(room,"Roof slab",new Vector3(0,2.87f,-1.67f),new Vector3(w+.30f,.30f,4.45f),baseMat,true,"Roof slab bears on the rear/side walls and front piers.");
            Box(room,"Front lintel",new Vector3(0,2.50f,.045f),new Vector3(w,.26f,.36f),plaster,false,"Lintel spans between the substantial front piers.");
            Box(room,"Shopfront lower wall",new Vector3(0,.48f,.06f),new Vector3(w-.5f,.96f,.28f),plaster,true);
            foreach(float x in new[]{-w*.27f,w*.27f})
                Box(room,"Window frame",new Vector3(x,1.74f,.10f),new Vector3(.14f,1.56f,.15f),frame);
            Box(room,"Counter sill",new Vector3(0,1.00f,.10f),new Vector3(w-.34f,.15f,.43f),frame,true);
            var glass=Mat("shop_glazing",new Color(.48f,.64f,.62f,.12f));
            glass.SetOverrideTag("RenderType","Transparent");
            glass.SetFloat("_Mode",2);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);
            glass.EnableKeyword("_ALPHABLEND_ON");glass.renderQueue=3000;EditorUtility.SetDirty(glass);
            var window=Box(room,"Glazed private shop boundary",new Vector3(0,1.72f,.12f),new Vector3(w-.48f,1.42f,.024f),glass,true);
            window.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            // Physical glazing/counter makes the closed play edge visible. Access
            // to these private interiors is from the service side of the block.
            for(int shelf=0;shelf<2;shelf++)
                Box(room,"Back shelf",new Vector3(0,1.0f+shelf*.62f,-3.30f),new Vector3(w-.8f,.12f,.46f),frame,false,"Retail shelf fixed into the rear wall.");
            string texture=shop.id=="PC_Express"?"Assets/TumbangPreso/Art/models/textures/pc_express_horizontal_rgb.png":
                Folder+"/Signs/"+shop.id+".png";
            PlaceShopSign(room,shop,texture);
            StockRoom(room,shop,w,frame,plaster);
            if(shop.id=="Load")WallPaint(room);
        }

        private static void WallPaint(Transform room)
        {
            var material=Mat("wall_paint_laro",Color.white);
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Signs/LaroGraffiti.png");
            material.SetOverrideTag("RenderType","TransparentCutout");material.SetFloat("_Mode",1);
            material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.Zero);
            material.SetInt("_ZWrite",1);material.SetFloat("_Cutoff",.4f);
            material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;EditorUtility.SetDirty(material);
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Original wall paint Tara Laro";
            go.transform.SetParent(room,false);go.transform.localPosition=new Vector3(.8f,.49f,.207f);
            go.transform.localRotation=Quaternion.Euler(0,180,0);go.transform.localScale=new Vector3(1.8f,.70f,1);
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(go,"Original lettering painted on the solid lower shop wall,7mm ahead of its surface.");go.isStatic=true;
        }

        private static void PlaceShopSign(Transform room,Shop shop,string texture)
        {
            // Pole geometry occupies only part of its crossarm AABB at sign
            // height. Reserve its actual shaft projection, including a margin,
            // instead of centering every sign through an existing street object.
            var free=new System.Collections.Generic.List<Vector2>{new Vector2(shop.z-shop.width*.5f+.25f,shop.z+shop.width*.5f-.25f)};
            foreach(var pole in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>r.name.StartsWith("SidewalkPole_")&&Mathf.Sign(r.bounds.center.x)==shop.side))
            {
                var block=HeightSlice(pole,2.55f,3.70f);
                if(block.size==Vector3.zero)continue;
                float a=block.min.z-.35f,b=block.max.z+.35f;
                for(int i=free.Count-1;i>=0;i--)
                {
                    var span=free[i];if(b<=span.x||a>=span.y)continue;
                    free.RemoveAt(i);
                    if(a>span.x)free.Add(new Vector2(span.x,a));
                    if(b<span.y)free.Add(new Vector2(b,span.y));
                }
            }
            if(free.Count==0)throw new InvalidOperationException("No clear sign mounting span: "+shop.id);
            var chosen=free.OrderByDescending(s=>s.y-s.x).ThenBy(s=>Mathf.Abs((s.x+s.y)*.5f-shop.z)).First();
            float width=Mathf.Min(3.15f,chosen.y-chosen.x);
            if(width<1.65f)throw new InvalidOperationException("Sign requires a new frontage layout: "+shop.id);
            float center=Mathf.Clamp(shop.z,chosen.x+width*.5f,chosen.y-width*.5f);
            float localX=room.InverseTransformPoint(new Vector3(room.position.x,room.position.y,center)).x;
            Sign(room,shop.id,texture,new Vector3(localX,2.92f,.65f),width,width*.25f);
        }

        private static Bounds HeightSlice(MeshRenderer renderer,float low,float high)
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices=mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
            var triangles=mesh.triangles;bool first=true;var bounds=new Bounds();
            void Include(Vector3 p){if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            for(int i=0;i<triangles.Length;i+=3)
            for(int e=0;e<3;e++)
            {
                var a=vertices[triangles[i+e]];var b=vertices[triangles[i+(e+1)%3]];
                if(a.y>=low&&a.y<=high)Include(a);
                foreach(float y in new[]{low,high})
                    if((a.y<y&&b.y>y)||(a.y>y&&b.y<y))Include(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
            }
            return bounds;
        }

        private static void StockRoom(Transform room,Shop shop,float width,Material frame,Material plaster)
        {
            var paper=Mat("stock_paper",new Color(.76f,.72f,.59f));
            var red=Mat("stock_oxblood",new Color(.47f,.23f,.24f));
            var green=Mat("stock_green",new Color(.32f,.48f,.36f));
            var metal=Mat("stock_metal",new Color(.53f,.55f,.50f));
            var dark=Mat("stock_screen",new Color(.10f,.16f,.14f));
            if(shop.id=="Pisonet")return;
            if(shop.id=="Laundry")
            {
                for(int i=0;i<3;i++)
                {
                    float x=(i-1)*1.25f;
                    Box(room,"Washing machine cabinet",new Vector3(x,.62f,-.91f),new Vector3(1.0f,1.12f,.80f),paper);
                    Disc(room,"Washer drum frame",new Vector3(x,.64f,-.49f),.35f,.07f,metal);
                    Disc(room,"Washer dark drum",new Vector3(x,.64f,-.44f),.26f,.06f,dark);
                    Box(room,"Washer controls",new Vector3(x,1.04f,-.485f),new Vector3(.73f,.12f,.045f),frame,false,"Controls are inset into the washer cabinet.");
                    Box(room,"Folded laundry",new Vector3(x,1.24f,-.85f),new Vector3(.72f,.16f,.56f),i==1?green:red);
                }
                return;
            }
            if(shop.id=="Barber")
            {
                foreach(float x in new[]{-1.2f,1.2f})
                {
                    Box(room,"Barber mirror frame",new Vector3(x,1.84f,-3.34f),new Vector3(1.10f,1.45f,.15f),frame,false,"Mirror is fixed to the rear shop wall.");
                    Box(room,"Barber mirror",new Vector3(x,1.84f,-3.25f),new Vector3(.92f,1.26f,.025f),metal,false,"Mirror face is seated in its frame.");
                    var chair=new GameObject("Barber chair").transform;chair.SetParent(room,false);
                    chair.localPosition=new Vector3(x,.065f,-1.6f);
                    Box(chair,"Pedestal",new Vector3(0,.35f,0),new Vector3(.42f,.70f,.42f),metal);
                    Box(chair,"Seat",new Vector3(0,.715f,0),new Vector3(.79f,.22f,.77f),red);
                    Box(chair,"Back",new Vector3(0,1.105f,-.32f),new Vector3(.79f,.78f,.18f),red);
                    MergeFixture(chair,"shop_barber_chair");
                }
                return;
            }
            Box(room,"Service counter body",new Vector3(0,.53f,-1.1f),new Vector3(width-1.1f,.94f,.67f),plaster);
            Box(room,"Service counter top",new Vector3(0,1.05f,-1.1f),new Vector3(width-1.0f,.13f,.82f),frame);
            if(shop.id=="PC_Express" || shop.id=="Repair")
            {
                for(int i=-1;i<=1;i++)
                {
                    float x=i*1.18f;
                    var monitor=new GameObject("Display monitor").transform;monitor.SetParent(room,false);
                    monitor.localPosition=new Vector3(x,1.115f,-1.08f);
                    Box(monitor,"Foot",new Vector3(0,.05f,.04f),new Vector3(.45f,.10f,.28f),frame);
                    Box(monitor,"Stem",new Vector3(0,.21f,0),new Vector3(.10f,.28f,.10f),frame);
                    Box(monitor,"Casing",new Vector3(0,.455f,0),new Vector3(.91f,.55f,.10f),frame);
                    Box(monitor,"Screen",new Vector3(0,.455f,.055f),new Vector3(.79f,.42f,.025f),dark);
                    MergeFixture(monitor,"shop_monitor");
                }
            }
            else if(shop.id=="Print")
            {
                Box(room,"Photocopier",new Vector3(-.8f,1.35f,-1.05f),new Vector3(1.32f,.48f,.71f),paper);
                Box(room,"Copier lid",new Vector3(-.8f,1.64f,-1.05f),new Vector3(1.37f,.12f,.74f),frame);
                for(int i=0;i<3;i++)Box(room,"Paper ream",new Vector3(.8f,1.15f+i*.13f,-1.05f),new Vector3(.70f,.12f,.52f),i==1?green:paper);
            }
            else if(shop.id=="Bakery" || shop.id=="Pares")
            {
                for(int i=-1;i<=1;i++)
                {
                    Box(room,"Food display tray",new Vector3(i*1.1f,1.15f,-1.02f),new Vector3(.85f,.075f,.54f),metal);
                    for(int j=0;j<3;j++)Box(room,shop.id=="Bakery"?"Bread loaf":"Covered serving dish",new Vector3(i*1.1f+(j-1)*.24f,1.29f,-1.02f),new Vector3(.21f,.20f,.35f),paper);
                }
            }
            // Stock is arranged on supported shelves in small, readable groups.
            // No loose road props or detached labels stand in for shop activity.
            for(int tier=0;tier<2;tier++)
            for(int col=0;col<6;col++)
                Box(room,shop.id=="Clothing"?"Folded stock":"Boxed shop stock",new Vector3((col-2.5f)*.70f,1.23f+tier*.62f,-3.27f),new Vector3(.53f,.34f,.36f),col%3==0?red:col%3==1?paper:green);
        }

        private static void Disc(Transform parent,string name,Vector3 at,float radius,float depth,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localRotation=Quaternion.Euler(90,0,0);
            go.transform.localScale=new Vector3(radius*2,depth*.5f,radius*2);
            go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic=true;AirborneByDesign.Attach(go,"Drum is mounted in the washing machine cabinet.");
        }

        private static void MergeFixture(Transform root,string assetName)
        {
            // These are connected props resting on their own base, not separate
            // floating panels. Group by material so their narrow mechanical
            // supports remain real geometry without a blanket airborne waiver.
            var pieces=root.GetComponentsInChildren<MeshFilter>();
            var groups=pieces.GroupBy(p=>p.GetComponent<Renderer>().sharedMaterial).ToArray();
            var temporary=new System.Collections.Generic.List<Mesh>();
            var combined=new System.Collections.Generic.List<CombineInstance>();
            foreach(var group in groups)
            {
                var part=new Mesh();
                part.CombineMeshes(group.Select(p=>new CombineInstance{mesh=p.sharedMesh,
                    transform=Matrix4x4.TRS(p.transform.localPosition,p.transform.localRotation,p.transform.localScale)}).ToArray(),true,true);
                temporary.Add(part);combined.Add(new CombineInstance{mesh=part,transform=Matrix4x4.identity});
            }
            string path=Folder+"/"+assetName+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name=assetName};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.CombineMeshes(combined.ToArray(),false,true);EditorUtility.SetDirty(mesh);
            foreach(var part in pieces)Object.DestroyImmediate(part.gameObject);
            foreach(var part in temporary)Object.DestroyImmediate(part);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterials=groups.Select(g=>g.Key).ToArray();
            root.gameObject.isStatic=true;
        }

        private static void BuildVendors(Transform root,Vendor[] vendors,float pavement,StringBuilder report)
        {
            foreach(var vendor in vendors)
            {
                string path="Assets/TumbangPreso/Art/models/street-stalls/"+vendor.kind+".glb";
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset==null)throw new InvalidOperationException("Missing original vendor asset: "+path);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name="Vendor_"+vendor.id;
                go.transform.SetParent(root,false);
                go.transform.SetPositionAndRotation(new Vector3(vendor.x,0,vendor.z),Quaternion.Euler(0,vendor.side<0?90:270,0));
                var b=BoundsOf(go);
                if(b.size.y<2.4f||b.size.y>2.8f)throw new InvalidOperationException("Unexpected vendor import scale: "+vendor.id+" "+b);
                go.transform.position+=Vector3.up*(pavement-b.min.y);
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
                {
                    filter.gameObject.isStatic=true;
                    var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;
                }
                report.AppendLine("  vendor "+vendor.id+" bounds="+BoundsOf(go));
            }
        }

        private static void BuildPisonets(Transform root,Shop shop,float pavement)
        {
            var room=root.Find("Frontage_"+shop.id);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/env_pisonet_kiosk.obj");
            if(prefab==null)throw new InvalidOperationException("Missing retained pisonet model.");
            for(int i=0;i<2;i++)
            {
                var machine=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                machine.name="InteriorPisonet_"+i;machine.transform.SetParent(room,false);
                machine.transform.localPosition=new Vector3(i==0?-1.12f:1.12f,.07f,-1.85f);
                machine.transform.localRotation=Quaternion.Euler(0,180,0);
                // The source already includes its chair. No duplicate stools or
                // collision-triggered coin/time claims in the scenic shop.
            }
        }

        private static void Crossings(Transform root,float[] positions)
        {
            var paint=Mat("worn_crossing",new Color(.65f,.63f,.56f));
            var pavement=Mat("pavement_extension",new Color(.60f,.58f,.51f));
            foreach(float z in positions)
            {
                for(int i=-7;i<=7;i++)Box(root,"Crosswalk stripe",new Vector3(i*.85f,.009f,z),new Vector3(.42f,.012f,2.4f),paint);
                foreach(float side in new[]{-1f,1f})
                    Box(root,"Connected sidewalk corner",new Vector3(side*9,.07f,Mathf.Sign(z)*25.25f),new Vector3(4,.284f,2.5f),pavement);
            }
        }

        private static void MoveSingleCord(Transform root,Transform[] old,float pavement)
        {
            var hazard=old.FirstOrDefault(t=>t.name=="TripHazard_PisonetCord");
            if(hazard==null)throw new InvalidOperationException("The existing single cord hazard was not found.");
            hazard.gameObject.SetActive(true);
            hazard.SetPositionAndRotation(new Vector3(-10.18f,pavement,0),Quaternion.identity);
            hazard.localScale=new Vector3(.60f,1,.65f);
            var metal=Mat("service_metal",new Color(.36f,.37f,.32f));
            var cord=Mat("service_cord",new Color(.50f,.42f,.20f));
            Box(root,"Repair power outlet",new Vector3(-10.86f,pavement+.57f,-.58f),new Vector3(.08f,.22f,.18f),metal,false,"Power outlet fixed to the repair shop's lower front wall.");
            var points=new[]{new Vector3(-10.82f,pavement+.52f,-.58f),new Vector3(-10.64f,pavement+.025f,-.58f),new Vector3(-10.24f,pavement+.025f,-.42f)};
            for(int i=0;i<points.Length-1;i++)
            {
                var part=Box(root,"Repair power lead",(points[i]+points[i+1])*.5f,new Vector3(.028f,.028f,Vector3.Distance(points[i],points[i+1])),cord,false,"Connected lead descends from the repair outlet to its single ground cord.");
                part.transform.rotation=Quaternion.LookRotation(points[i+1]-points[i]);
            }
        }

        private static void Notices(Transform root)
        {
            foreach(float side in new[]{-1f,1f})
            {
                var mount=new GameObject("Column notice mount").transform;mount.SetParent(root,false);
                mount.SetPositionAndRotation(new Vector3(side*3.72f,1.35f,side*10),Quaternion.Euler(0,side<0?90:270,0));
                Sign(mount,"Bawal",Folder+"/Signs/Bawal.png",Vector3.zero,1.12f,.75f);
            }
        }

        private static void Sign(Transform parent,string id,string texturePath,Vector3 at,float width,float height)
        {
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(tex==null)throw new InvalidOperationException("Missing authored sign face: "+texturePath);
            var backing=Mat("sign_backing",new Color(.25f,.27f,.24f));
            Box(parent,"Sign frame "+id,at,new Vector3(width+.12f,height+.12f,.22f),backing,false,"Sign frame is fixed to the shop roof fascia or column mounting face.");
            var material=Mat("sign_face_"+id,Color.white);material.mainTexture=tex;material.SetFloat("_Glossiness",.08f);
            EditorUtility.SetDirty(material);
            var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Sign face "+id;
            face.transform.SetParent(parent,false);face.transform.localPosition=at+Vector3.forward*.117f;
            face.transform.localRotation=Quaternion.Euler(0,180,0);face.transform.localScale=new Vector3(width,height,1);
            Object.DestroyImmediate(face.GetComponent<Collider>());face.GetComponent<Renderer>().sharedMaterial=material;
            face.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;face.isStatic=true;
            AirborneByDesign.Attach(face,"Lettering is fixed to its substantial shop/column sign frame.");
        }

        private static Material Mat(string name,Color color)
        {
            Directory.CreateDirectory(Folder);
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",.15f);EditorUtility.SetDirty(m);return m;
        }

        private static GameObject Box(Transform parent,string name,Vector3 at,Vector3 size,Material mat,bool solid=false,string support=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;
            if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());
            if(support!=null)AirborneByDesign.Attach(go,support);
            return go;
        }

        private static Bounds BoundsOf(GameObject go)
        {var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    }
}
