using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorBodyLensContractTests
    {
        private readonly List<GameObject> owned = new();
        private static readonly Vector3 Origin = new(1234, 4, -987);
        private SpectatorDirector director;
        private CharacterVisual visual;
        private MeshCollider witness;
        private static readonly MethodInfo Validate = typeof(SpectatorDirector).GetMethod(
            "ValidatePose", BindingFlags.NonPublic | BindingFlags.Instance);
        [SetUp] public void Before()
        {
            var camera = Own("Dormant director"); camera.SetActive(false);
            director = camera.AddComponent<SpectatorDirector>();
            typeof(SpectatorDirector).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(director, null);
            var actor = Own("Body whose render model extends beyond its rules capsule");
            actor.transform.position = Origin;
            var motor = actor.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.IsPerson = true;
            var mount = new GameObject("Visual"); mount.transform.SetParent(actor.transform, false);
            visual = actor.AddComponent<CharacterVisual>(); visual.enabled = false;
            // EditMode does not dispatch MonoBehaviour.Awake; initialize its real lifecycle first.
            typeof(CharacterVisual).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(visual, null);
            visual.SetModelRoot(mount.transform);
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(source);
            source.name = "Closed render body fixture"; source.transform.position = Origin + Vector3.right * 20;
            Object.DestroyImmediate(source.GetComponent<BoxCollider>());
            visual.ApplyModel(source, Color.white);
            // Native triangle geometry witness only, under the real installed model hierarchy.
            witness = visual.Model.AddComponent<MeshCollider>();
            witness.sharedMesh = visual.Model.GetComponent<MeshFilter>().sharedMesh;
            Physics.SyncTransforms();
        }
        [TearDown] public void After()
        {
            foreach(var item in owned) if(item != null) Object.DestroyImmediate(item);
            owned.Clear(); Physics.SyncTransforms();
        }
        private GameObject Own(string name) { var go=new GameObject(name);owned.Add(go);return go; }
        private bool Accepts(Vector3 eye, Vector3 focus) => (bool)Validate.Invoke(director,
            new object[]{eye,focus,true});
        [Test] public void EyeInsideAnOpaqueRenderBodyIsNotAValidPose()
        {
            Vector3 eye = visual.Model.GetComponent<Renderer>().bounds.center + Vector3.right * .8f;
            Assert.Less(Vector3.Distance(eye,witness.ClosestPoint(eye)), .0001f,
                "Native closed mesh must actually contain the eye, not just its AABB.");
            var capsule=visual.GetComponent<CharacterController>();
            Assert.Greater(Vector3.Distance(eye,capsule.ClosestPoint(eye)),.05f,
                "Eye is outside the rules capsule; gameplay reach cannot represent the model.");
            Assert.IsFalse(Accepts(eye,eye+Vector3.forward*5),
                "The camera eye is in an opaque body despite being outside its capsule.");
        }
        [Test] public void ABodyCrossingOnlyTheSightlineRemainsAllowed()
        {
            Vector3 center=visual.Model.GetComponent<Renderer>().bounds.center;
            Assert.IsTrue(Accepts(center-Vector3.forward*5,center+Vector3.forward*5));
        }
        [Test] public void AnEyeClearOfBothBodyAndSightlineRemainsAllowed()
        {
            Vector3 eye=visual.Model.GetComponent<Renderer>().bounds.center+Vector3.right*5;
            Assert.IsTrue(Accepts(eye,eye+Vector3.forward*5));
        }
        [Test] public void NameplateOrEffectOutsideTheInstalledModelDoesNotBlockTheEye()
        {
            Vector3 eye=visual.Model.GetComponent<Renderer>().bounds.center+Vector3.right*5;
            var label=GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(label);
            label.name="Nameplate or effect outside model";label.transform.SetParent(visual.ModelRoot,false);
            label.transform.position=eye;Object.DestroyImmediate(label.GetComponent<BoxCollider>());
            Assert.IsTrue(Accepts(eye,eye+Vector3.forward*5));
        }
        [Test] public void HiddenRenderBodiesDoNotBlockTheEye()
        {
            Vector3 eye=visual.Model.GetComponent<Renderer>().bounds.center;
            visual.Model.GetComponent<Renderer>().enabled=false;
            Assert.IsTrue(Accepts(eye,eye+Vector3.forward*5));
        }
        [TestCase(false)] [TestCase(true)]
        public void AShortGlideCannotRenderAnEyeInsideABody(bool startInside)
        {
            const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(SpectatorDirector).GetField("_bearingDeg",hidden).SetValue(director,30f);
            object[] pose={Vector3.zero,0f,0f};
            typeof(SpectatorDirector).GetMethod("ComputeShot",hidden).Invoke(director,pose);
            Vector3 desired=(Vector3)pose[0], start=desired-Vector3.right*4;
            Vector3 velocity=Vector3.zero;
            Vector3 intermediate=Vector3.SmoothDamp(start,desired,ref velocity,
                SpectatorDirector.PositionSmoothTime,Mathf.Infinity,.5f);
            Vector3 bodyCenter=visual.Model.GetComponent<Renderer>().bounds.center;
            visual.transform.position+=(startInside?start:intermediate)-bodyCenter;
            Physics.SyncTransforms();
            var bounds=visual.Model.GetComponent<Renderer>().bounds;
            Assert.IsFalse(bounds.Contains(desired),"The desired shot is genuinely clear.");
            Assert.IsTrue(bounds.Contains(startInside?start:intermediate));
            director.transform.position=start;
            typeof(SpectatorDirector).GetMethod("FlyToShot",hidden).Invoke(director,new object[]{.5f});
            Assert.IsFalse(bounds.Contains(director.transform.position),
                "The actual smoothed eye must recover from or avoid the body enclosure.");
        }
    }
}
