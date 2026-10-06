using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ReplayCameraClearanceTests
    {
        readonly List<GameObject> owned=new();
        static readonly Vector3 Eye=new(1234,4,-987);
        Camera camera;Vector3 corner;float radius;
        static readonly MethodInfo Clear=typeof(RecordedWorldView).GetMethod("Clear",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void Before()
        {
            var root=Own("Dormant replay lens");camera=root.AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=Eye;camera.nearClipPlane=.08f;camera.fieldOfView=58;camera.aspect=16f/9;
            var corners=new Vector3[4];camera.CalculateFrustumCorners(new Rect(0,0,1,1),camera.nearClipPlane,Camera.MonoOrStereoscopicEye.Mono,corners);
            corner=corners[0];radius=0;
            foreach(var value in corners){if(value.x>corner.x)corner=value;radius=Mathf.Max(radius,value.magnitude);}
            Assert.Greater(corner.x,.06f);Assert.IsNotNull(Clear);
        }
        [TearDown] public void After(){foreach(var root in owned)if(root!=null)Object.DestroyImmediate(root);owned.Clear();Physics.SyncTransforms();}
        GameObject Own(string name){var root=new GameObject(name);owned.Add(root);return root;}
        BoxCollider Wall(string kind)
        {
            var root=Own(kind);root.transform.position=Eye+new Vector3(corner.x,0,camera.nearClipPlane);
            var box=root.AddComponent<BoxCollider>();box.size=new Vector3(.035f,2,.4f);
            if(kind=="Trigger")box.isTrigger=true;
            if(kind=="Can")root.AddComponent<Lata>().enabled=false;
            if(kind=="Body")root.AddComponent<CharacterMotor>().enabled=false;
            if(kind=="Shoe")root.AddComponent<Slipper>().enabled=false;
            Physics.SyncTransforms();return box;
        }
        bool Accepts()=> (bool)Clear.Invoke(null,Clear.GetParameters().Length==2
            ?new object[]{Eye+Vector3.forward*5,Eye}
            :new object[]{Eye+Vector3.forward*5,Eye,radius});
        [Test] public void AReplayEyeCannotAcceptSceneryInsideItsNearPlane()
        {
            var wall=Wall("Opaque scenery");Vector3 nearCorner=Eye+corner;
            Assert.Less(Vector3.Distance(nearCorner,wall.ClosestPoint(nearCorner)),.0001f);
            Assert.IsFalse(Physics.Linecast(Eye+Vector3.forward*5,Eye,~0,QueryTriggerInteraction.Ignore),
                "The original center-to-eye ray is genuinely clear.");
            Assert.IsFalse(Accepts(),"The native camera near-plane corner is inside opaque scenery despite a clear central ray.");
        }
        [Test] public void AnUnobstructedReplayPoseRemainsAvailable(){Assert.IsTrue(Accepts());}
        [TestCase("Trigger")] [TestCase("Can")] [TestCase("Body")] [TestCase("Shoe")]
        public void ExistingIgnoredGeometryRemainsEligible(string kind){Wall(kind);Assert.IsTrue(Accepts());}
        [Test] public void AWallAcrossTheSightlineStillRejectsTheAngle()
        {
            var root=Own("Scenery on sightline");root.transform.position=Eye+Vector3.forward*2;
            root.AddComponent<BoxCollider>();Physics.SyncTransforms();Assert.IsFalse(Accepts());
        }
    }
}
