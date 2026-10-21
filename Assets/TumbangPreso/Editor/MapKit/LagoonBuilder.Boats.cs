using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class LagoonBuilder
    {
        private static void VillageBoats(Transform root)
        {
            Watercraft(root,0,new Vector3(-9,LagoonWater.SurfaceY-.03f,-29),12,6.2f,1.15f,false,new Vector3(-9,-.40f,-26.82f));
            Watercraft(root,1,new Vector3(11,LagoonWater.SurfaceY-.03f,-28),-25,5.4f,.95f,false,new Vector3(10,.79f,-21.75f));
            Watercraft(root,2,new Vector3(-18,LagoonWater.SurfaceY-.03f,12),22,5.1f,1.0f,false,new Vector3(-14,.79f,11.43f));
            Watercraft(root,3,new Vector3(43,LagoonWater.SurfaceY-.03f,19),-15,6.5f,1.2f,false,new Vector3(43,.6f,23));
            Watercraft(root,4,new Vector3(-34,LagoonWater.SurfaceY-.03f,26),18,8.4f,1.8f,true,new Vector3(-40,1.1f,23));
            Watercraft(root,5,new Vector3(29,LagoonWater.SurfaceY-.03f,38),-40,9.0f,2.0f,true,new Vector3(30,1.2f,40));
        }
        private static void Watercraft(Transform parent,int id,Vector3 at,float yaw,float length,float width,bool shelter,Vector3 tie)
        {
            var boat=Group(parent,(shelter?"Sheltered houseboat ":"Pointed working canoe ")+id);
            boat.position=at;boat.rotation=Quaternion.Euler(0,yaw,0);
            var paint=Mat("Working boat paint "+id,id%3==0?new Color(.27f,.45f,.45f):id%3==1?new Color(.48f,.31f,.22f):new Color(.48f,.48f,.31f));
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var outer=new List<int>();var inner=new List<int>();
            const int sections=11,profile=10;
            for(int i=0;i<sections;i++)
            {
                float t=i/(float)(sections-1),z=(t-.5f)*length;
                float half=Mathf.Max(.035f,width*.5f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.64f));
                float rise=Mathf.Pow(Mathf.Abs(t*2-1),2)*.34f;
                float inside=Mathf.Max(.016f,half-.07f);
                var cross=new[]{new Vector2(-half,.27f),new Vector2(-half*.65f,-.15f),new Vector2(0,-.32f),
                    new Vector2(half*.65f,-.15f),new Vector2(half,.27f),new Vector2(inside,.24f),
                    new Vector2(inside*.64f,.10f),new Vector2(0,.085f),new Vector2(-inside*.64f,.10f),new Vector2(-inside,.24f)};
                foreach(var p in cross){vertices.Add(new Vector3(p.x,p.y+rise,z));uv.Add(new Vector2(z,p.y));}
            }
            for(int i=0;i<sections-1;i++)for(int j=0;j<profile;j++)
            {
                int a=i*profile+j,b=i*profile+(j+1)%profile,c=a+profile,d=b+profile;
                (j<4?outer:inner).AddRange(new[]{a,b,c,b,d,c});
            }
            for(int i=1;i<profile-1;i++)
            {inner.AddRange(new[]{0,i+1,i});int end=(sections-1)*profile;inner.AddRange(new[]{end,end+i,end+i+1});}
            var mesh=new Mesh{name="CurvedWatercraft"+id};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=2;
            mesh.SetTriangles(outer,0);mesh.SetTriangles(inner,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var hull=Group(boat,"Curved keel and planked shell");hull.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"CurvedWatercraft"+id);
            hull.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{paint,_wood};
            foreach(float z in new[]{-length*.20f,length*.18f})Box(boat,"Fitted cross seat",new Vector3(0,.21f,z),new Vector3(width*.91f,.08f,.29f),_fresh);
            var paddle=Group(boat,"Stored working paddle");paddle.localPosition=new Vector3(width*.15f,.37f,-.18f);paddle.localRotation=Quaternion.Euler(0,11+id*3,0);
            Box(paddle,"Paddle shaft",Vector3.zero,new Vector3(.047f,.047f,2.75f),_wood);
            var blade=Box(paddle,"Paddle blade",new Vector3(0,0,-1.10f),new Vector3(.21f,.040f,.62f),_fresh);
            if(shelter)
            {
                var roof=Mat("Houseboat palm shelter",new Color(.53f,.45f,.31f));
                Box(boat,"Raised household floor",new Vector3(0,.31f,-.25f),new Vector3(width*.86f,.09f,length*.50f),_wood);
                foreach(float x in new[]{-width*.42f,width*.42f})foreach(float z in new[]{-length*.26f,length*.19f})
                    Pole(boat,"Shelter post",new Vector3(x,1.0f,z),.045f,1.45f,_dark,false);
                foreach(float side in new[]{-1f,1f})
                {
                    var cover=Box(boat,"Houseboat shelter roof",new Vector3(side*width*.25f,1.82f,-length*.035f),new Vector3(width*.63f,.10f,length*.55f),roof);
                    cover.transform.localRotation=Quaternion.Euler(0,0,-side*20);
                }
                Box(boat,"Rolled shelter side",new Vector3(width*.46f,1.63f,-.25f),new Vector3(.14f,.15f,length*.42f),roof);
                Box(boat,"Sheltered household storage",new Vector3(-width*.17f,.61f,-length*.17f),new Vector3(.62f,.51f,.61f),_dark);
                Box(boat,"Plain sleeping mat",new Vector3(0,.375f,0),new Vector3(width*.59f,.025f,length*.24f),_rope);
            }
            // Keep the island/house batches separate so distant scenery can cull.
            VillageBatches.Add(boat);
            var motion=boat.gameObject.AddComponent<MooredBoatMotion>();motion.Phase=id*1.7f;motion.DockTie=tie;
            motion.BowLocal=new Vector3(0,.40f,length*.5f-.12f);
        }
    }
}
