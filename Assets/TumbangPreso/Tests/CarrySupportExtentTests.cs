using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class CarrySupportExtentTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PalmUsesTheHandRatherThanALongPropOrASmallSkinColouredInset(bool inset)
        {
            var root=new GameObject("Palm source ownership");var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=new Mesh();
            try
            {
                var source=cube.GetComponent<MeshFilter>().sharedMesh;
                var hand=Matrix4x4.TRS(new Vector3(inset?.37f:.275f,0,0),Quaternion.identity,new Vector3(inset?.1f:.15f,.10f,.12f));
                var other=Matrix4x4.TRS(new Vector3(inset?.25f:.6f,0,0),Quaternion.identity,
                    inset?new Vector3(.02f,.14f,.013f):new Vector3(.5f,.06f,.06f));
                mesh.CombineMeshes(new[]{new CombineInstance{mesh=source,transform=hand},new CombineInstance{mesh=source,transform=other}});
                var weights=new BoneWeight[mesh.vertexCount];var uv=new Vector2[mesh.vertexCount];
                for(int i=0;i<mesh.vertexCount;i++)
                {
                    weights[i]=new BoneWeight{boneIndex0=0,weight0=1};
                    bool skin=inset?i>=source.vertexCount:i<source.vertexCount;
                    uv[i]=skin?new Vector2(13.5f/16,2.5f/16):new Vector2(1.5f/16,6.5f/16);
                }
                mesh.boneWeights=weights;mesh.bindposes=new[]{Matrix4x4.identity};mesh.uv=uv;
                var bone=new GameObject("arm-right");bone.transform.SetParent(root.transform);
                var renderer=root.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.bones=new[]{bone.transform};
                Assert.True(Visual.CharacterVisual.PalmCentre(renderer,0,out var palm));
                Assert.That(palm.x,Is.InRange(inset?.32f:.20f,inset?.43f:.36f),"The palm followed a prop/inset instead of the physical hand.");
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(cube);Object.DestroyImmediate(mesh);}
        }

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
