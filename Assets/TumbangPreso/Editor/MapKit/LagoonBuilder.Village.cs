using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class LagoonBuilder
    {
        private enum VillageHome { ThatchGable, RepairShelter, PatchedMetal, ScreenVeranda, HipThatch, CommunityShade }
        private static readonly List<Transform> VillageBatches=new List<Transform>();
        private static readonly List<Vector3> VillageResidentPlaces=new List<Vector3>();

        private static void Village(Transform homes,Transform root)
        {
            VillageBatches.Clear();VillageResidentPlaces.Clear();
            var connected=new[]{
                (new Vector3(-25.6f,.75f,-10.5f),-85f,5.4f,4.5f,VillageHome.ThatchGable,new Vector3(-20,0,-10.5f)),
                (new Vector3(-28,1.0f,10),-100f,6.5f,5.3f,VillageHome.RepairShelter,new Vector3(-20,0,10)),
                (new Vector3(25.5f,1.05f,-12),100f,5.5f,4.8f,VillageHome.PatchedMetal,new Vector3(20,0,-12)),
                (new Vector3(27,.55f,9),80f,5.5f,4.8f,VillageHome.ScreenVeranda,new Vector3(20,0,9)),
                (new Vector3(-9,1.1f,27),8f,5.7f,5.0f,VillageHome.HipThatch,new Vector3(-9,0,20)),
                (new Vector3(8.5f,.8f,26),-10f,6.1f,4.7f,VillageHome.CommunityShade,new Vector3(8.5f,0,20)),
                (new Vector3(-29,.65f,-22),-55f,5.0f,4.5f,VillageHome.ScreenVeranda,new Vector3(-20,0,-18)),
                (new Vector3(18,1.25f,32),-25f,4.9f,4.5f,VillageHome.PatchedMetal,new Vector3(14,0,20))};
            int index=0;
            foreach(var spec in connected)
            {
                var home=VillageHouse(homes,index++,spec.Item1,spec.Item2,spec.Item3,spec.Item4,spec.Item5,true);
                var entrance=home.TransformPoint(new Vector3(0,.04f,-spec.Item4*.5f-1.25f));
                ResidentialSpur(root,spec.Item6,entrance,index);
            }
            // Separate dwellings over the water, grouped with deliberate gaps for
            // boats rather than a repeated ring of identical connected huts.
            var detached=new[]{
                (new Vector3(-42,1.1f,23),-68f,4.6f,4.2f,VillageHome.ThatchGable),
                (new Vector3(-48,.7f,-8),-31f,4.2f,3.8f,VillageHome.PatchedMetal),
                (new Vector3(-36,.95f,37),18f,5.0f,4.3f,VillageHome.ScreenVeranda),
                (new Vector3(-20,1.25f,48),-12f,4.8f,4.6f,VillageHome.HipThatch),
                (new Vector3(-5,.75f,52),16f,4.2f,4.0f,VillageHome.ThatchGable),
                (new Vector3(15,1.0f,48),-18f,5.2f,4.5f,VillageHome.PatchedMetal),
                (new Vector3(32,1.2f,41),-46f,4.6f,4.1f,VillageHome.HipThatch),
                (new Vector3(46,.65f,24),72f,5.1f,4.3f,VillageHome.RepairShelter),
                (new Vector3(50,1.05f,1),114f,4.2f,3.7f,VillageHome.ThatchGable),
                (new Vector3(41,.85f,-28),134f,4.8f,4.2f,VillageHome.ScreenVeranda)};
            foreach(var spec in detached)VillageHouse(homes,index++,spec.Item1,spec.Item2,spec.Item3,spec.Item4,spec.Item5,false);
        }

        private static Transform VillageHouse(Transform parent,int id,Vector3 position,float yaw,float width,float depth,VillageHome kind,bool playable)
        {
            var home=Group(parent,(playable?"Neighbourhood home ":"Detached stilt home ")+id+" "+kind);
            home.localPosition=position;home.localRotation=Quaternion.Euler(0,yaw,0);VillageBatches.Add(home);
            Deck(home,"Supported household platform",new Vector3(0,0,-.45f),width+.7f,depth+2.2f);
            var frame=Mat("Weathered household timber frame",new Color(.39f,.32f,.24f));
            var wood=Mat("Household timber planks "+id,new Color(.45f+(id%3)*.045f,.37f+(id%4)*.025f,.27f+(id%2)*.035f));
            var bamboo=Mat("Split bamboo screen",new Color(.60f,.52f,.35f));
            var repair=Mat("New household repair timber",new Color(.60f,.48f,.32f));
            bool open=kind==VillageHome.RepairShelter||kind==VillageHome.CommunityShade;
            float wallHeight=2.15f+(id%3)*.12f;
            foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f})
            {
                Pole(home,"House post",new Vector3(x,wallHeight*.5f,z),.13f,wallHeight,frame,playable);
            }
            foreach(float z in new[]{-depth*.5f-1.37f,depth*.5f+.47f})
                foreach(float side in new[]{-1f,1f})
                    Beam(home,"Braced piles below household",new Vector3(side*(width*.5f+.17f),-.25f,z),new Vector3(0,-1.10f,z),.105f,frame);
            if(!open)
            {
                Wall(home,"Back wall",width,wallHeight,wood,new Vector3(0,0,depth*.5f),180,id%2==0,false,playable);
                Wall(home,"Side wall",depth,wallHeight,kind==VillageHome.ScreenVeranda?bamboo:wood,new Vector3(-width*.5f,0,0),90,id%2!=0,false,playable,kind==VillageHome.ScreenVeranda);
                Wall(home,"Side wall",depth,wallHeight,wood,new Vector3(width*.5f,0,0),-90,id%2==0,false,playable);
                Wall(home,"Door and window frontage",width,wallHeight,wood,new Vector3(0,0,-depth*.5f),0,id%2==0,true,playable);
                foreach(float side in new[]{-1f,1f})
                    Box(home,"Door jamb",new Vector3(-.75f+side*.54f,.94f,-depth*.5f-.07f),new Vector3(.10f,1.88f,.16f),repair);
                Box(home,"Door lintel",new Vector3(-.75f,1.91f,-depth*.5f-.07f),new Vector3(1.2f,.11f,.16f),frame);
                Box(home,"Window sill",new Vector3(1.20f,.83f,-depth*.5f-.16f),new Vector3(1.22f,.11f,.38f),repair);
                var shutter=Box(home,"Opened plank shutter",new Vector3(1.88f,1.29f,-depth*.5f-.12f),new Vector3(.55f,.90f,.09f),repair);
                shutter.transform.localRotation=Quaternion.Euler(0,28+(id%3)*13,0);
                // An actual dark interior is visible through the openings.
                Box(home,"Interior storage chest",new Vector3(width*.23f,.34f,depth*.2f),new Vector3(1.1f,.64f,.60f),frame,playable);
            }
            else
            {
                Wall(home,"Shelter back screen",width,wallHeight*.63f,wood,new Vector3(0,0,depth*.5f),180,true,false,playable);
                Box(home,"Long communal workbench",new Vector3(0,.74f,0),new Vector3(width*.64f,.14f,.9f),repair,playable);
                foreach(float x in new[]{-width*.24f,width*.24f})Box(home,"Workbench trestle",new Vector3(x,.34f,0),new Vector3(.16f,.68f,.66f),frame);
            }
            VillageRoof(home,id,width+.9f,depth+.9f,wallHeight,kind);
            var porch=Group(home,"Household veranda");porch.localPosition=new Vector3(0,0,-depth*.5f-.8f);
            foreach(float side in new[]{-1f,1f})
            {
                Pole(porch,"Porch upright",new Vector3(side*(width*.5f-.1f),wallHeight*.43f,0),.11f,wallHeight*.86f,frame,playable);
                Rail(porch,new Vector3(side*(width*.25f+.35f),0,-.52f),Mathf.Max(.7f,width*.5f-1.15f),false);
            }
            var awning=Box(home,"Small separate porch awning",new Vector3(0,wallHeight*.86f,-depth*.5f-.60f),new Vector3(width+.8f,.08f,1.7f),kind==VillageHome.PatchedMetal?_steel:wood);
            awning.transform.localRotation=Quaternion.Euler(9,0,0);
            HouseholdDetails(home,id,width,depth,wallHeight,kind,playable);
            if(playable && (id==0||id==1||id==3||id==5))VillageResidentPlaces.Add(home.TransformPoint(new Vector3(width*.21f,.04f,-depth*.5f-.65f)));
            // A boat-access ladder belongs to detached houses too.
            float drop=home.position.y-LagoonWater.SurfaceY+.35f;
            var ladder=Group(home,"Boat access ladder");ladder.localPosition=new Vector3(width*.36f,-drop*.5f,-depth*.5f-1.52f);
            ladder.localRotation=Quaternion.Euler(-9,0,0);
            foreach(float x in new[]{-.32f,.32f})Box(ladder,"Ladder side",new Vector3(x,0,0),new Vector3(.07f,drop,.075f),frame);
            int rungs=Mathf.CeilToInt(drop/.30f);
            for(int i=0;i<rungs;i++)Box(ladder,"Ladder rung",new Vector3(0,-drop*.5f+(i+.5f)*drop/rungs,0),new Vector3(.68f,.065f,.09f),repair);
            return home;
        }

        private static void Wall(Transform parent,string name,float width,float height,Material material,Vector3 at,float yaw,bool vertical,bool openings,bool solid,bool woven=false)
        {
            var wall=Group(parent,name);wall.localPosition=at;wall.localRotation=Quaternion.Euler(0,yaw,0);
            if(woven){WovenPanel(wall,width,height,material,solid);return;}
            int columns=Mathf.CeilToInt(width/.29f);float board=width/columns;
            for(int i=0;i<columns;i++)
            {
                float x=-width*.5f+(i+.5f)*board;
                if(openings&&x> -1.29f&&x< -.21f)
                    Box(wall,"Plank above entry",new Vector3(x,(height+1.87f)*.5f,0),new Vector3(board-.018f,height-1.87f,.085f),material,solid);
                else if(openings&&x>.65f&&x<1.75f)
                {
                    Box(wall,"Plank below open window",new Vector3(x,.40f,0),new Vector3(board-.018f,.80f,.085f),material,solid);
                    Box(wall,"Plank above open window",new Vector3(x,(height+1.73f)*.5f,0),new Vector3(board-.018f,height-1.73f,.085f),material,solid);
                }
                else Box(wall,"Wall board",new Vector3(x,height*.5f,0),new Vector3(board-.018f,height,.085f),material,solid);
            }
            if(!vertical && !openings)
                for(float y=.22f;y<height;y+=.25f)
                    Box(wall,"Horizontal weatherboard overlap",new Vector3(0,y,.05f),new Vector3(width,.035f,.045f),material);
        }

        private static void WovenPanel(Transform parent,float width,float height,Material material,bool solid)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();var normals=new List<Vector3>();
            void Patch(float x,float y,float w,float h,float z)
            {
                int start=vertices.Count;
                var face=new[]{new Vector3(x-w*.5f,y-h*.5f,z),new Vector3(x-w*.5f,y+h*.5f,z),
                    new Vector3(x+w*.5f,y+h*.5f,z),new Vector3(x+w*.5f,y-h*.5f,z)};
                vertices.AddRange(face);vertices.AddRange(face);
                uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right,Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
                normals.AddRange(Enumerable.Repeat(Vector3.back,4));normals.AddRange(Enumerable.Repeat(Vector3.forward,4));
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3,start+4,start+6,start+5,start+4,start+7,start+6});
            }
            int columns=Mathf.CeilToInt(width/.22f),rows=Mathf.CeilToInt(height/.22f);
            float cellX=width/columns,cellY=height/rows;
            for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)
            {
                float xx=-width*.5f+(x+.5f)*cellX,yy=(y+.5f)*cellY;
                bool over=(x+y)%2==0;
                Patch(xx,yy,cellX*.74f,cellY+.008f,over?.008f:-.008f);
                Patch(xx,yy,cellX+.008f,cellY*.57f,over?-.009f:.009f);
            }
            var mesh=new Mesh{name="PlainWovenPanel"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            // Separate front/back vertices retain the correct light-facing normal.
            mesh.SetNormals(normals);mesh.RecalculateBounds();
            string id=FormattableString.Invariant($"WovenPanel_{width:F2}_{height:F2}");
            parent.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,id);parent.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(solid){var c=parent.gameObject.AddComponent<BoxCollider>();c.center=Vector3.up*height*.5f;c.size=new Vector3(width,height,.07f);}
            foreach(float x in new[]{-width*.5f,width*.5f})Box(parent,"Screen edge batten",new Vector3(x,height*.5f,0),new Vector3(.065f,height,.08f),_wood);
        }

        private static void VillageRoof(Transform home,int id,float width,float depth,float eave,VillageHome kind)
        {
            var roof=Group(home,"House-specific roof "+kind);
            bool metal=kind==VillageHome.PatchedMetal||kind==VillageHome.RepairShelter;
            var baseMat=metal?Mat("Weathered corrugated roof "+id,id%2==0?new Color(.50f,.52f,.49f):new Color(.47f,.31f,.23f)):
                Mat("Layered palm thatch "+id,new Color(.51f+(id%3)*.035f,.43f+(id%2)*.035f,.29f));
            float rise=kind==VillageHome.ThatchGable?1.24f:kind==VillageHome.HipThatch?.92f:.72f;
            if(kind==VillageHome.HipThatch)
            {
                HippedThatch(roof,id,width,depth,eave,rise,baseMat);return;
            }
            float half=width*.5f,slope=Mathf.Atan2(rise,half)*Mathf.Rad2Deg,slant=Mathf.Sqrt(half*half+rise*rise);
            foreach(float side in new[]{-1f,1f})
            {
                var sheet=Box(roof,"Pitched roof foundation",new Vector3(side*half*.5f,eave+rise*.5f,0),new Vector3(slant+.10f,metal?.08f:.16f,depth),baseMat);
                sheet.transform.localRotation=Quaternion.Euler(0,0,-side*slope);
                int strips=Mathf.CeilToInt(depth/(metal?.26f:.18f));
                for(int i=0;i<strips;i++)
                {
                    float uneven=metal?0:Mathf.Sin(i*2.31f+id)*.06f;
                    var strip=Box(roof,metal?"Raised metal rib":"Overlapping thatch bundle",
                        new Vector3(side*half*.5f,eave+rise*.5f+.08f,-depth*.5f+(i+.5f)*depth/strips),
                        new Vector3(slant+.12f+uneven,metal?.025f:.075f,metal?.045f:depth/strips*.94f),baseMat);
                    strip.transform.localRotation=sheet.transform.localRotation;
                }
                for(int beam=0;beam<4;beam++)
                {
                    var rafter=Box(roof,"Exposed structural rafter",new Vector3(side*half*.5f,eave+rise*.5f-.12f,-depth*.36f+beam*depth*.24f),new Vector3(slant,.10f,.10f),_dark);
                    rafter.transform.localRotation=sheet.transform.localRotation;
                }
            }
            Box(roof,"Bound ridge",new Vector3(0,eave+rise+.10f,0),new Vector3(metal?.18f:.29f,metal?.08f:.17f,depth+.12f),metal?_steel:baseMat);
            if(kind==VillageHome.PatchedMetal)
            {
                var patch=Box(roof,"Localized roof repair",new Vector3(-half*.47f,eave+rise*.5f+.11f,depth*.21f),new Vector3(slant*.69f,.035f,depth*.27f),_steel);
                patch.transform.localRotation=Quaternion.Euler(0,0,slope);
            }
        }

        private static void HippedThatch(Transform roof,int id,float width,float depth,float eave,float rise,Material material)
        {
            var p=new[]{new Vector3(-width*.5f,eave,-depth*.5f),new Vector3(width*.5f,eave,-depth*.5f),
                new Vector3(width*.5f,eave,depth*.5f),new Vector3(-width*.5f,eave,depth*.5f),
                new Vector3(0,eave+rise,-depth*.24f),new Vector3(0,eave+rise,depth*.24f)};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var face in new[]{new[]{0,1,4},new[]{1,2,5,4},new[]{2,3,5},new[]{3,0,4,5}})
            {
                var indices=face.ToArray();
                if(Vector3.Cross(p[indices[1]]-p[indices[0]],p[indices[2]]-p[indices[0]]).y<0)Array.Reverse(indices);
                int start=vertices.Count;foreach(int index in indices){vertices.Add(p[index]);uv.Add(new Vector2(p[index].x,p[index].z));}
                for(int i=1;i<indices.Length-1;i++)triangles.AddRange(new[]{start,start+i,start+i+1});
            }
            var mesh=new Mesh{name="HippedThatch"+id};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var shape=Group(roof,"Four sloping thatch faces");shape.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"HippedThatch"+id);
            shape.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            foreach(var edge in new[]{(0,1),(1,2),(2,3),(3,0),(0,4),(1,4),(2,5),(3,5),(4,5)})
                Beam(roof,"Bound thatch edge",p[edge.Item1],p[edge.Item2],.13f,material);
            for(int band=1;band<7;band++)
            {
                float t=band/7f;
                foreach(float sign in new[]{-1f,1f})
                {
                    Vector3 start=Vector3.Lerp(new Vector3(sign*width*.5f,eave,-depth*.5f),new Vector3(0,eave+rise,-depth*.24f),t);
                    Vector3 end=Vector3.Lerp(new Vector3(sign*width*.5f,eave,depth*.5f),new Vector3(0,eave+rise,depth*.24f),t);
                    Beam(roof,"Layered hip thatch course",start+Vector3.up*.025f,end+Vector3.up*.025f,.055f,material);
                }
            }
        }
        private static void Beam(Transform parent,string name,Vector3 from,Vector3 to,float thickness,Material material)
        {
            var delta=to-from;var beam=Box(parent,name,(from+to)*.5f,new Vector3(thickness,delta.magnitude,thickness),material);
            beam.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
        }
        private static void HouseholdDetails(Transform home,int id,float width,float depth,float height,VillageHome kind,bool playable)
        {
            var shelf=Box(home,"Porch storage shelf",new Vector3(-width*.32f,.67f,-depth*.5f-.69f),new Vector3(1.0f,.08f,.48f),_wood,playable);
            foreach(float x in new[]{-.36f,.36f})Box(home,"Shelf leg",shelf.transform.localPosition+new Vector3(x,-.34f,0),new Vector3(.08f,.64f,.36f),_dark);
            for(int i=0;i<2;i++)
            {
                var jar=GameObject.CreatePrimitive(PrimitiveType.Cylinder);jar.name="Household water container";jar.transform.SetParent(home,false);
                jar.transform.localPosition=new Vector3(-width*.32f+(i-.5f)*.32f,.86f,-depth*.5f-.66f);jar.transform.localScale=new Vector3(.25f,.18f,.25f);
                jar.GetComponent<Renderer>().sharedMaterial=Mat("Household clay container",new Color(.57f,.37f,.25f));Object.DestroyImmediate(jar.GetComponent<Collider>());
            }
            if(id%3==0)
            {
                // Simple original stripes, not an attributed traditional mat design.
                var mat=Mat("Plain household mat",new Color(.60f,.47f,.30f));
                Box(home,"Rolled household mat",new Vector3(width*.30f,.13f,-depth*.5f-.65f),new Vector3(.30f,.26f,1.03f),mat);
            }
            if(kind==VillageHome.RepairShelter)
            {
                for(int i=0;i<5;i++)Box(home,"Boat repair board stock",new Vector3(width*.29f,.10f+i*.075f,depth*.12f),new Vector3(.6f,.06f,2.6f-i*.11f),i==4?_fresh:_wood);
                Box(home,"Repair mallet handle",new Vector3(-.3f,.89f,-.1f),new Vector3(.48f,.05f,.055f),_wood);
                Box(home,"Repair mallet head",new Vector3(-.55f,.91f,-.1f),new Vector3(.14f,.12f,.23f),_dark);
            }
            if(kind==VillageHome.RepairShelter || kind==VillageHome.ScreenVeranda)
            {
                // A loose net on a repair/drying frame: geometry has a sag and a
                // local function, not a repeated decorative pattern on the houses.
                var net=Group(home,"Hanging work net");net.localPosition=new Vector3(width*.38f,.72f,depth*.20f);
                var fibre=Mat("Plain net cord",new Color(.43f,.41f,.30f));
                for(int column=0;column<7;column++)for(int row=0;row<5;row++)
                {
                    Vector3 Point(int x,int y)=>new Vector3(x*.20f,y*.24f,Mathf.Sin(x*.5f)*.10f+y*.015f);
                    if(column<6)Beam(net,"Net horizontal cord",Point(column,row),Point(column+1,row),.013f,fibre);
                    if(row<4)Beam(net,"Net vertical cord",Point(column,row),Point(column,row+1),.013f,fibre);
                }
                foreach(float x in new[]{-.06f,1.26f})Pole(net,"Net drying stake",new Vector3(x,.10f,0),.045f,1.62f,_dark,false);
            }
            if(playable&&id%3==2)
                ResidentLaundryAuthor.Place(home.parent.parent,"courtyard-line",home.TransformPoint(new Vector3(-2,1.95f,-depth*.5f-.5f)),home.eulerAngles.y,home.position.y+.04f);
            if(playable)Plant(home.parent.parent,home.TransformPoint(new Vector3(width*.36f,.04f,-depth*.5f-.94f)),id);
        }

        private static void ResidentialSpur(Transform parent,Vector3 start,Vector3 end,int id)
        {
            var span=end-start;float length=new Vector2(span.x,span.z).magnitude;if(length<.3f)return;
            var bridge=Group(parent,"Offset residential spur "+id);bridge.position=(start+end)*.5f;
            bridge.rotation=Quaternion.LookRotation(new Vector3(span.x,0,span.z));VillageBatches.Add(bridge);
            float angle=-Mathf.Atan2(span.y,length)*Mathf.Rad2Deg;
            var support=Box(bridge,"Continuous plank access",Vector3.down*.1f,new Vector3(2.9f,.20f,Mathf.Sqrt(length*length+span.y*span.y)),_dark,true);
            support.transform.localRotation=Quaternion.Euler(angle,0,0);
            int boards=Mathf.CeilToInt(length/.34f);
            for(int i=0;i<boards;i++)
            {
                float t=(i+.5f)/boards;
                var board=Box(bridge,"Crosswise access board",new Vector3(0,(t-.5f)*span.y+.02f,(t-.5f)*length),new Vector3(2.95f,.04f,length/boards-.014f),i%19==0?_fresh:_wood);
                board.transform.localRotation=Quaternion.Euler(angle,0,0);
            }
            int posts=Mathf.Max(1,Mathf.CeilToInt(length/2.1f));
            for(int i=0;i<=posts;i++)foreach(float side in new[]{-1f,1f})
            {
                float t=i/(float)posts,y=(t-.5f)*span.y;
                float ground=bridge.position.y+y-LagoonWater.FloorY;
                Pole(bridge,"Spur support to seabed",new Vector3(side*1.33f,y-ground*.5f,(t-.5f)*length),.105f,ground,_dark,true);
                Pole(bridge,"Spur handrail post",new Vector3(side*1.34f,y+.42f,(t-.5f)*length),.075f,.84f,_wood,false);
            }
            foreach(float side in new[]{-1f,1f})
            {
                var rail=Box(bridge,"Sloping access handrail",new Vector3(side*1.34f,.80f,0),new Vector3(.075f,.075f,Mathf.Sqrt(length*length+span.y*span.y)),_wood);
                rail.transform.localRotation=Quaternion.Euler(angle,0,0);
            }
        }
        private static void PlaceVillageResidents(Transform parent)
        {
            string[] ids={"maring","totoy","kuya_boy","ate_girlie"};
            for(int i=0;i<VillageResidentPlaces.Count;i++)
            {
                var point=VillageResidentPlaces[i];var owner=Group(parent,"Neighbour at household veranda "+i);
                owner.position=point;owner.rotation=Quaternion.LookRotation(-new Vector3(point.x,0,point.z));
                var resident=owner.gameObject.AddComponent<LagoonResident>();resident.Art=RosterBook.Load().FindPersonArt(ids[i%ids.Length]);resident.ReactionStyle=i;
            }
        }
        private static GameObject Pole(Transform parent,string name,Vector3 at,float radius,float height,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
    }
}
