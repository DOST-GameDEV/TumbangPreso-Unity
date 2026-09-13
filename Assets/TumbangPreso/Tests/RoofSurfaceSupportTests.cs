using NUnit.Framework;
using TumbangPreso.EditorTools.MapKit;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class RoofSurfaceSupportTests
    {
        [Test]
        public void GeometryGateSeesTheActualLowRoofAndRejectsItsBoundingBoxTop()
        {
            var building=new GameObject("Stepped mesh");var mesh=new Mesh();
            var low=GameObject.CreatePrimitive(PrimitiveType.Cube);var high=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                mesh.CombineMeshes(new[]{
                    new CombineInstance{mesh=low.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(new Vector3(-2,1,0),Quaternion.identity,new Vector3(4,2,4))},
                    new CombineInstance{mesh=high.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(new Vector3(2,3,0),Quaternion.identity,new Vector3(4,6,4))}});
                building.AddComponent<MeshFilter>().sharedMesh=mesh;var support=building.AddComponent<MeshRenderer>();
                prop.transform.SetParent(building.transform,false);prop.transform.localScale=Vector3.one*.5f;
                prop.transform.localPosition=new Vector3(-2,2.25f,0);
                var renderer=prop.GetComponent<MeshRenderer>();
                Assert.That(MapGeometryCheck.MeasureParentSupport(renderer,support),Is.EqualTo(2).Within(.001f));
                prop.transform.localPosition=new Vector3(-2,6.25f,0);
                float actual=MapGeometryCheck.MeasureParentSupport(renderer,support);
                Assert.That(actual,Is.EqualTo(2).Within(.001f));
                Assert.Greater(renderer.bounds.min.y-actual,MapGeometryCheck.FloatTolerance,"A unit floating at the overall bounds top must fail");
                Assert.IsEmpty(building.GetComponents<MeshCollider>());
            }
            finally{Object.DestroyImmediate(building);Object.DestroyImmediate(low);Object.DestroyImmediate(high);Object.DestroyImmediate(mesh);}
        }

        [TestCase(0)][TestCase(90)]
        public void AUnitOnALowWingUsesThatRoofInsteadOfTheTowersBounds(float yaw)
        {
            var building=new GameObject("Stepped building");
            var unit=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                foreach(var spec in new[]{new Vector3(-2,1,0),new Vector3(2,3,0)})
                {
                    var wing=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wing.transform.SetParent(building.transform,false);
                    wing.transform.localPosition=spec;wing.transform.localScale=new Vector3(4,spec.y*2,4);
                }
                building.transform.rotation=Quaternion.Euler(0,yaw,0);
                unit.transform.localScale=new Vector3(.6f,.5f,.6f);
                unit.transform.position=building.transform.TransformPoint(new Vector3(-2,9,0));
                using(var roof=new RoofSurfaceSupport(building.transform))Assert.IsTrue(roof.Seat(unit.transform));
                Assert.That(unit.GetComponent<Renderer>().bounds.min.y,Is.EqualTo(2).Within(.001f));
                Assert.IsEmpty(building.GetComponentsInChildren<MeshCollider>(),"Temporary authoring colliders must not ship");
            }
            finally{Object.DestroyImmediate(building);Object.DestroyImmediate(unit);}
        }

        [Test]
        public void AnUnsupportedFootprintIsRefusedWithoutMovingThePropIntoThinAir()
        {
            var building=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var unit=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                building.transform.localScale=new Vector3(2,4,2);
                unit.transform.localScale=new Vector3(5,.5f,5);unit.transform.position=Vector3.up*8;
                using(var roof=new RoofSurfaceSupport(building.transform))Assert.IsFalse(roof.Seat(unit.transform));
                Assert.AreEqual(Vector3.up*8,unit.transform.position);
            }
            finally{Object.DestroyImmediate(building);Object.DestroyImmediate(unit);}
        }
    }
}
