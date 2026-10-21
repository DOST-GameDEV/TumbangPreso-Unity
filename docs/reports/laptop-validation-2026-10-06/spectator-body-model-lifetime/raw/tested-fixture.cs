using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorBodyModelLifetimeTests
    {
        readonly List<GameObject> owned=new();
        static readonly Vector3 Origin=new(1234,4,-987);
        SpectatorDirector director; CharacterVisual visual; CharacterMotor motor;
        static readonly MethodInfo Check=typeof(SpectatorDirector).GetMethod("BodyLensIsClear",BindingFlags.NonPublic|BindingFlags.Instance);
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            var camera=Own("Dormant model-lifetime director");camera.SetActive(false);
            director=camera.AddComponent<SpectatorDirector>();
            var actor=Own("Registered body");actor.transform.position=Origin;
            motor=actor.AddComponent<CharacterMotor>();motor.enabled=false;motor.IsPerson=true;
            Assert.IsNotNull(GameServices.Round);GameServices.Round.Register(motor);
            var mount=Own("Visual mount");mount.transform.SetParent(actor.transform,false);
            visual=actor.AddComponent<CharacterVisual>();visual.enabled=false;visual.SetModelRoot(mount.transform);
        }
        [UnityTearDown] public IEnumerator After()
        {
            GameServices.Round?.Unregister(motor);
            foreach(var item in owned)if(item!=null)Object.DestroyImmediate(item);
            owned.Clear();yield return PlayModeWorld.Reset();
        }
        GameObject Own(string name){var go=new GameObject(name);owned.Add(go);return go;}
        GameObject Source(float offset=0,bool hidden=false)
        {
            var root=Own("Closed model source");root.transform.position=Origin+Vector3.right*30;
            var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.name="Opaque body mesh";
            body.transform.SetParent(root.transform,false);body.transform.localPosition=Vector3.right*offset;
            Object.DestroyImmediate(body.GetComponent<BoxCollider>());body.SetActive(!hidden);
            return root;
        }
        bool Clear(Vector3 eye)=>(bool)Check.Invoke(director,new object[]{eye});
        Renderer Mesh()=>visual.Model.GetComponentInChildren<Renderer>(true);
        [Test] public void ReplacingAModelWithinTheFrameInvalidatesItsOldEnclosure()
        {
            visual.ApplyModel(Source(),Color.white);
            Vector3 oldEye=Mesh().bounds.center;Assert.IsFalse(Clear(oldEye));
            int frame=Time.frameCount,version=visual.ModelVersion;
            visual.ApplyModel(Source(4),Color.white);
            Assert.AreEqual(frame,Time.frameCount);Assert.Greater(visual.ModelVersion,version);
            Vector3 newEye=Mesh().bounds.center;
            Assert.Greater(Vector3.Distance(oldEye,newEye),5);
            Assert.IsFalse(Clear(newEye),"The newly installed body cannot be absent from lens validation until the next frame.");
            Assert.IsTrue(Clear(oldEye),"The retired model cannot keep blocking its former enclosure.");
        }
        [Test] public void ABodyEnabledWithinTheFrameCannotBeMissingFromCachedRenderers()
        {
            visual.ApplyModel(Source(hidden:true),Color.white);
            var body=Mesh();Vector3 eye=visual.Model.transform.position;
            Assert.IsTrue(Clear(eye));int frame=Time.frameCount;
            body.gameObject.SetActive(true);Physics.SyncTransforms();eye=body.bounds.center;
            Assert.AreEqual(frame,Time.frameCount);
            Assert.IsFalse(Clear(eye),"A newly visible installed mesh must participate immediately.");
        }
        [Test] public void MovingABodyReadsCurrentBoundsWithoutChangingModelIdentity()
        {
            visual.ApplyModel(Source(),Color.white);Vector3 oldEye=Mesh().bounds.center;
            Assert.IsFalse(Clear(oldEye));var model=visual.Model;
            motor.transform.position+=Vector3.right*5;Physics.SyncTransforms();
            Assert.AreSame(model,visual.Model);Assert.IsTrue(Clear(oldEye));
            Assert.IsFalse(Clear(Mesh().bounds.center));
        }
        [Test] public void RepeatedWarmPoseChecksAllocateNoManagedMemory()
        {
            visual.ApplyModel(Source(),Color.white);
            var check=(Func<Vector3,bool>)Check.CreateDelegate(typeof(Func<Vector3,bool>),director);
            Vector3 eye=Mesh().bounds.center;for(int i=0;i<8;i++)check(eye);
            long before=GC.GetAllocatedBytesForCurrentThread();bool clear=false;
            for(int i=0;i<64;i++)clear|=check(eye);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.IsFalse(clear);Assert.AreEqual(0,allocated,"Warm lens checks should reuse model renderer references.");
        }
    }
}
