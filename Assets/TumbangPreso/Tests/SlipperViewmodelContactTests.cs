using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class SlipperViewmodelContactTests
    {
        [Test]
        public void EveryFirstPersonGripHasVisibleHandContactForEveryShoe()
        {
            var root = new GameObject("First person grip study");
            var prop = new GameObject("Grip source shoe");
            var rows = new List<string>{"character,shoe,charge,gap_metres"};
            var errors = new List<string>();
            var geometry = new List<string>();
            try
            {
                var arms=root.AddComponent<ViewmodelArms>(); arms.EnsureBuilt();
                var shoe=prop.AddComponent<Slipper>(); var book=RosterBook.Load();
                foreach(var person in Roster.AllPeople)
                {
                    arms.SetCharacter(person.Id);
                    var hand=root.transform.Find("RightPivot/Arm").GetComponent<MeshFilter>();
                    var held=hand.transform.Find("HeldSlipper").GetComponent<MeshFilter>();
                    Assert.IsNotNull(hand); Assert.IsNotNull(held);
                    foreach(var art in book.Slippers)
                    {
                        if(art==null||art.Model==null)continue;
                        var model=Object.Instantiate(art.Model,prop.transform);
                        try
                        {
                            arms.MatchSkin(shoe); arms.SetHolding(true);
                            foreach(float charge in new[]{-1f,0f,.5f,1f})
                            {
                                arms.SetCharge(charge); arms.StepVisuals(.2f,true); arms.MatchSkin(shoe);
                                float gap=MeshContactDistance(hand,held);
                                rows.Add(FormattableString.Invariant($"{person.Id},{art.Id},{charge},{gap:F5}"));
                                if(float.IsInfinity(gap)||gap>.035f)errors.Add($"{person.Id}/{art.Id}/{charge}: {gap:F4}m");
                            }
                        }
                        finally{Object.DestroyImmediate(model);}
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(prop);Object.DestroyImmediate(root);
                string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/slipper-contact";
                Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"first-person-contact.csv"),rows);
                File.WriteAllLines(Path.Combine(output,"first-person-geometry.txt"),geometry);
            }
            Assert.IsEmpty(errors,string.Join("\n",errors));
        }

        [Test]
        public void RenderRepresentativeFirstPersonGrips()
        {
            var root=new GameObject("FPP rendered grip");var prop=new GameObject("FPP shoe source");
            var eye=new GameObject("FPP witness").AddComponent<Camera>();eye.enabled=false;
            eye.nearClipPlane=.01f;eye.fieldOfView=70;eye.aspect=4f/3;eye.clearFlags=CameraClearFlags.SolidColor;
            eye.backgroundColor=new Color(.12f,.14f,.18f);
            var light=new GameObject("FPP light").AddComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(30,-20,0);
            var target=new RenderTexture(800,600,24);var pixels=new Texture2D(800,600,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                var arms=root.AddComponent<ViewmodelArms>();arms.EnsureBuilt();var shoe=prop.AddComponent<Slipper>();
                prop.transform.position=Vector3.one*100;
                foreach(string person in new[]{"bayan","rafi","paete","inday"})
                foreach(string id in new[]{"tsinelas","crocs","loafers"})
                {
                    var model=Object.Instantiate(RosterBook.Load().Slippers.First(a=>a.Id==id).Model,prop.transform);
                    try
                    {
                        arms.SetCharacter(person);arms.SetCharge(-1);arms.SetHolding(true);arms.StepVisuals(.5f,true);arms.MatchSkin(shoe);
                        eye.targetTexture=target;eye.Render();RenderTexture.active=target;
                        pixels.ReadPixels(new Rect(0,0,800,600),0,0);pixels.Apply();
                        string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/slipper-contact";
                        Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,person+"-"+id+".png"),pixels.EncodeToPNG());
                    }
                    finally{Object.DestroyImmediate(model);}
                }
            }
            finally
            {
                RenderTexture.active=previous;eye.targetTexture=null;
                Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);Object.DestroyImmediate(eye.gameObject);
                Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(prop);Object.DestroyImmediate(root);
            }
        }

        static float MeshContactDistance(MeshFilter hand,MeshFilter shoe)
        {
            var hv=hand.sharedMesh.vertices;var sv=shoe.sharedMesh.vertices;var triangles=shoe.sharedMesh.triangles;
            for(int i=0;i<sv.Length;i++)sv[i]=shoe.transform.TransformPoint(sv[i]);
            float tip=hand.sharedMesh.bounds.max.y-hand.sharedMesh.bounds.size.y*.25f;
            float nearest=float.PositiveInfinity;
            foreach(var vertex in hv.Distinct())
            {
                if(vertex.y<tip)continue;
                var point=hand.transform.TransformPoint(vertex);
                for(int i=0;i<triangles.Length;i+=3)
                    nearest=Mathf.Min(nearest,TriangleDistanceSquared(point,sv[triangles[i]],sv[triangles[i+1]],sv[triangles[i+2]]));
            }
            return Mathf.Sqrt(nearest);
        }
        static float TriangleDistanceSquared(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var n=Vector3.Cross(ab,ac);
            float n2=n.sqrMagnitude;
            if(n2>.0000000001f)
            {
                var projected=p-n*(Vector3.Dot(p-a,n)/n2);var q=projected-a;
                float aa=Vector3.Dot(ab,ab),bb=Vector3.Dot(ac,ac),cross=Vector3.Dot(ab,ac);
                float u=(bb*Vector3.Dot(q,ab)-cross*Vector3.Dot(q,ac))/n2;
                float v=(aa*Vector3.Dot(q,ac)-cross*Vector3.Dot(q,ab))/n2;
                if(u>=0&&v>=0&&u+v<=1)return (p-projected).sqrMagnitude;
            }
            return Mathf.Min(EdgeDistance(p,a,b),Mathf.Min(EdgeDistance(p,b,c),EdgeDistance(p,c,a)));
        }
        static float EdgeDistance(Vector3 p,Vector3 a,Vector3 b)
        {
            var d=b-a;float t=d.sqrMagnitude>.0000000001f?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0;
            return (p-a-d*t).sqrMagnitude;
        }
    }
}
