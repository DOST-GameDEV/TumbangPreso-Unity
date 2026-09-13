using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class CarrySupportExtentTests
    {
        [Test]
        public void RotatingAFlatShoeDoesNotIncreaseItsPalmClearance()
        {
            var root=new GameObject("Tilted shoe");
            try
            {
                var shoe=root.AddComponent<Slipper>();
                var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.transform.SetParent(root.transform);
                mesh.transform.localScale=new Vector3(.2f,.04f,.6f);
                float expected=Mathf.Max(Balance.SlipperRestHeight,.02f);
                foreach(float tilt in new[]{0f,25f,65f,90f,135f})
                {
                    root.transform.rotation=Quaternion.Euler(tilt,35,12);
                    Assert.AreEqual(expected,shoe.CarrySupportExtent(root.transform.up),.0001f);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test]
        public void TheVisibleShoeCentreStaysAgainstTheTiltedPalmEvenWithAnOffsetMeshOrigin()
        {
            var actor=new GameObject("Carry pose");var root=new GameObject("Offset-origin shoe");
            try
            {
                actor.AddComponent<CharacterMotor>();var carrier=actor.AddComponent<Carrier>();
                var palm=new GameObject("Palm").transform;palm.SetParent(actor.transform);
                palm.position=new Vector3(.4f,1,.3f);palm.rotation=Quaternion.Euler(70,25,18);
                var shoe=root.AddComponent<Slipper>();
                var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.transform.SetParent(root.transform);
                mesh.transform.localPosition=new Vector3(.4f,.02f,.3f);mesh.transform.localScale=new Vector3(.2f,.04f,.6f);
                typeof(Carrier).GetField("_hand",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(carrier,palm);
                typeof(Carrier).GetProperty("Held").GetSetMethod(true).Invoke(carrier,new object[]{shoe});
                typeof(Carrier).GetMethod("RideAnchor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(carrier,null);
                var expected=palm.position+palm.up*Mathf.Max(Balance.SlipperRestHeight,.02f);
                Assert.Less(Vector3.Distance(expected,mesh.GetComponent<Renderer>().bounds.center),.0001f);
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
    }
}
