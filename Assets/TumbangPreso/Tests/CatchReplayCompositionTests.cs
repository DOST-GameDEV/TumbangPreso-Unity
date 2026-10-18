using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class CatchReplayCompositionTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private readonly List<GameObject> _owned=new();
        private Camera _camera;
        private CatchReconstruction _view;
        private static readonly Vector3 Origin=new(1800,40,-1800);
        [SetUp] public void Before()
        {
            _view=Own("Composition camera owner").AddComponent<CatchReconstruction>();
            _view.enabled=false;
            _camera=Own("Dormant composition lens").AddComponent<Camera>();_camera.enabled=false;
            _camera.fieldOfView=58;_camera.aspect=16f/9;_camera.nearClipPlane=.08f;
            Set("_camera",_camera);Set("_shotSide",1f);
        }
        [TearDown] public void After(){foreach(var root in _owned)if(root!=null)Object.DestroyImmediate(root);_owned.Clear();Physics.SyncTransforms();}
        private GameObject Own(string name){var root=new GameObject(name);_owned.Add(root);return root;}
        private MatchPoseHistory.Copy Body(string name,Vector3 position)
        {
            var root=Own(name);root.transform.position=Origin+position;
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.transform.SetParent(root.transform,false);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.localPosition=Vector3.up*.85f;mesh.transform.localScale=new Vector3(.65f,1.7f,.65f);
            return new MatchPoseHistory.Copy(root);
        }
        private void Set(string name,object value)=>typeof(CatchReconstruction).GetField(name,Private).SetValue(_view,value);
        private bool Place()=> (bool)typeof(CatchReconstruction).GetMethod("PlaceShotCamera",Private).Invoke(_view,null);
        [TestCase(1f,16f/9)] [TestCase(6f,16f/9)] [TestCase(10f,16f/9)]
        [TestCase(1f,4f/3)] [TestCase(6f,4f/3)] [TestCase(10f,4f/3)]
        public void RecordedApproachKeepsTheVictimWholeAndProminent(float separation,float aspect)
        {
            _camera.aspect=aspect;
            var actor=Body("Recorded tagger",Vector3.back*separation*.5f);
            var victim=Body("Recorded victim",Vector3.forward*separation*.5f);
            Set("_actorCopy",actor);Set("_victimCopy",victim);Physics.SyncTransforms();
            Assert.IsTrue(Place(),"An open court must retain a usable replay angle");
            var bounds=victim.Renderers[0].bounds;
            float bottom=1,top=0;
            foreach(float x in new[]{bounds.min.x,bounds.max.x})
                foreach(float y in new[]{bounds.min.y,bounds.max.y})
                    foreach(float z in new[]{bounds.min.z,bounds.max.z})
                    {
                        var point=_camera.WorldToViewportPoint(new Vector3(x,y,z));
                        Assert.Greater(point.z,_camera.nearClipPlane,"Victim must remain in front of the lens");
                        Assert.That(point.x,Is.InRange(.05f,.95f),"Victim clips horizontally during recorded approach");
                        Assert.That(point.y,Is.InRange(.05f,.95f),"Victim clips vertically during recorded approach");
                        bottom=Mathf.Min(bottom,point.y);top=Mathf.Max(top,point.y);
                    }
            Assert.Greater(top-bottom,.26f,"The victim must remain prominent during a distant approach");
        }
        [Test] public void AReplayLensRejectsNearPlaneSceneryEvenWithAClearCenterRay()
        {
            Set("_actorCopy",Body("Recorded tagger",Vector3.back*.5f));
            Set("_victimCopy",Body("Recorded victim",Vector3.forward*.5f));
            Assert.IsTrue(Place());
            var corners=new Vector3[4];
            _camera.CalculateFrustumCorners(new Rect(0,0,1,1),_camera.nearClipPlane,Camera.MonoOrStereoscopicEye.Mono,corners);
            var wall=Own("Near plane obstruction");wall.transform.position=_camera.transform.TransformPoint(corners[0]);
            wall.transform.rotation=_camera.transform.rotation;
            wall.AddComponent<BoxCollider>().size=new Vector3(.02f,.12f,.02f);Physics.SyncTransforms();
            Vector3 focus=(Vector3)typeof(CatchReconstruction).GetField("_shotFocus",Private).GetValue(_view);
            Assert.IsFalse(Physics.Linecast(focus,_camera.transform.position,~0,QueryTriggerInteraction.Ignore),"Center-ray regression fixture must be genuinely clear");
            Assert.IsFalse(Place(),"The lens footprint is inside scenery");
        }
        [Test] public void RetainedTouchCannotUseAnotherVictimsPeakOrTheReturningHand()
        {
            var owner=Own("Recorded contact author");
            var motor=owner.AddComponent<CharacterMotor>();motor.enabled=false;
            var animator=owner.AddComponent<TumbangPreso.Visual.CharacterAnimator>();animator.enabled=false;
            var source=Own("Recorded contact model");source.transform.SetParent(owner.transform,false);
            var track=new MatchPoseHistory.Track(motor,source);
            void Record(float time,int subject,float weight)
            {
                var type=animator.GetType();
                type.GetField("_tgApplied",Private).SetValue(animator,true);
                type.GetField("_tagContactValid",Private).SetValue(animator,true);
                type.GetField("_tagContactSubject",Private).SetValue(animator,subject);
                type.GetField("_tagRenderedWeight",Private).SetValue(animator,weight);
                try {track.Record(time);}
                finally {type.GetField("_tgApplied",Private).SetValue(animator,false);}
            }
            Record(100,1,.2f);Record(100.1f,1,1);Record(100.19f,2,1);Record(100.3f,1,.3f);
            var args=new object[]{100f,100.4f,1,100.18f,0f};
            var choose=typeof(MatchPoseHistory.Track).GetMethod("TryTagContactFrame",Private);
            Assert.IsTrue((bool)choose.Invoke(track,args));
            Assert.That((float)args[4],Is.EqualTo(100.1f).Within(.0001f),"Retain the accepted victim's full touch");
        }
        [Test] public void DestroyedVictimRigRetiresItsSkinSampleWithoutChangingTheAcceptedPoint()
        {
            var owner=Own("Contact owner");var animator=owner.AddComponent<TumbangPreso.Visual.CharacterAnimator>();animator.enabled=false;
            var target=Own("Contact victim").AddComponent<CharacterMotor>();target.enabled=false;
            var type=animator.GetType();
            type.GetField("_tagContactValid",Private).SetValue(animator,true);
            type.GetField("_tagSkinContact",Private).SetValue(animator,true);
            type.GetField("_tagSkinVictim",Private).SetValue(animator,target);
            Vector3 accepted=new(1,.7f,2);type.GetField("_tagContact",Private).SetValue(animator,accepted);
            var bone=Own("Victim rig bone").transform;
            var vertexType=type.GetNestedType("TagSoleVertex",BindingFlags.NonPublic);
            var vertex=System.Activator.CreateInstance(vertexType,new object[]{Vector3.zero,
                new BoneWeight{boneIndex0=0,weight0=1},new[]{bone},new[]{Matrix4x4.identity}});
            foreach(var name in new[]{"_tagSkinA","_tagSkinB","_tagSkinC"})type.GetField(name,Private).SetValue(animator,vertex);
            Assert.IsTrue((bool)vertexType.GetProperty("Valid").GetValue(vertex));
            Object.DestroyImmediate(bone.gameObject);
            Assert.DoesNotThrow(()=>type.GetMethod("RefreshTagSkinContact",Private).Invoke(animator,null));
            Assert.IsFalse((bool)type.GetField("_tagSkinContact",Private).GetValue(animator));
            Assert.AreEqual(accepted,type.GetField("_tagContact",Private).GetValue(animator));
            Assert.IsTrue((bool)type.GetField("_tagContactValid",Private).GetValue(animator),"Rig loss cannot manufacture or remove an accepted receipt");
        }
    }
}
