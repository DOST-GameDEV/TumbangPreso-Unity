using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class KuroFormTests
    {
        [TestCase(GhostPetCompanion.FidgetState.CatSmile,"KuroCatMouth")]
        [TestCase(GhostPetCompanion.FidgetState.GoofyDizzy,"KuroCrossLeft")]
        [TestCase(GhostPetCompanion.FidgetState.ShyPout,"KuroShyEye")]
        public void CuteExpressionsUseAuthoredShapesAndClearBeforeACast(GhostPetCompanion.FidgetState gesture,string visiblePart)
        {
            var source=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu").PetModel;
            var owner=new GameObject("Expression owner");var pet=Object.Instantiate(source);
            try
            {
                var companion=pet.AddComponent<GhostPetCompanion>();companion.Bind(owner.transform,Vector3.zero,1);
                var nodes=pet.GetComponentsInChildren<Transform>(true);
                var group=nodes.Single(n=>n.name=="KuroExpressions");
                companion.SampleIdleForCapture(gesture,GhostPetCompanion.IdleGestureDuration(gesture)*.5f);
                Assert.AreEqual(Vector3.one,group.localScale);
                Assert.AreEqual(Vector3.one,nodes.Single(n=>n.name==visiblePart).localScale);
                Assert.AreEqual(Vector3.zero,nodes.Single(n=>n.name=="ghost-mouth").localScale,"Neutral and expression mouths overlap.");
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.None,0);
                Assert.AreEqual(Vector3.zero,group.localScale);
                Assert.Greater(nodes.Single(n=>n.name=="ghost-mouth").localScale.sqrMagnitude,0);
                companion.SampleIdleForCapture(gesture,GhostPetCompanion.IdleGestureDuration(gesture)*.5f);
                companion.Devour(2);companion.StepTo(.1f);
                Assert.AreEqual(Vector3.zero,group.localScale,"A cute idle expression survives into the ultimate.");
            }
            finally {Object.DestroyImmediate(pet);Object.DestroyImmediate(owner);}
        }

        [Test]
        public void BakedTrailerClipsMatchTheLiveGestureSampler()
        {
            var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
            var target=new GameObject("Clip comparison origin");var pet=Object.Instantiate(entry.PetModel);
            try
            {
                var companion=pet.AddComponent<GhostPetCompanion>();companion.Bind(target.transform,Vector3.zero,1);
                var nodes=pet.GetComponentsInChildren<Transform>();
                foreach(GhostPetCompanion.FidgetState gesture in System.Enum.GetValues(typeof(GhostPetCompanion.FidgetState)))
                {
                    string name=gesture==GhostPetCompanion.FidgetState.None?"Hover":gesture.ToString();
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TumbangPreso/Art/animations/kuro-idles/kuro-"+name+".anim");
                    Assert.IsNotNull(clip,name+" has no reusable clip.");
                    float time=GhostPetCompanion.IdleGestureDuration(gesture)*.37f;
                    companion.SampleIdleForCapture(gesture,time);
                    var positions=nodes.Select(n=>n.localPosition).ToArray();
                    var rotations=nodes.Select(n=>n.localRotation).ToArray();
                    var scales=nodes.Select(n=>n.localScale).ToArray();
                    companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.HappyHop,.2f);
                    clip.SampleAnimation(pet,time);
                    for(int i=0;i<nodes.Length;i++)
                    {
                        Assert.Less(Vector3.Distance(positions[i],nodes[i].localPosition),.001f,name+" position "+nodes[i].name);
                        Assert.Less(Quaternion.Angle(rotations[i],nodes[i].localRotation),.6f,name+" rotation "+nodes[i].name);
                        Assert.Less(Vector3.Distance(scales[i],nodes[i].localScale),.001f,name+" scale "+nodes[i].name);
                    }
                }
            }
            finally {Object.DestroyImmediate(pet);Object.DestroyImmediate(target);}
        }

        [Test]
        public void IdlePersonalityCanBeSampledRepeatablyWithoutChangingGameplayRandom()
        {
            var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
            var target=new GameObject("Idle capture origin");
            var pet=Object.Instantiate(entry.PetModel);
            var random=Random.state;
            try
            {
                Random.InitState(734);var before=Random.state;float expected=Random.value;Random.state=before;
                var companion=pet.AddComponent<GhostPetCompanion>();companion.Bind(target.transform,Vector3.zero,1);
                var nodes=pet.GetComponentsInChildren<Transform>();
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.CuriousPeek,.6f);
                var positions=nodes.Select(n=>n.localPosition).ToArray();
                var rotations=nodes.Select(n=>n.localRotation).ToArray();
                var scales=nodes.Select(n=>n.localScale).ToArray();
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.HappyHop,.4f);
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.CuriousPeek,.6f);
                for(int i=0;i<nodes.Length;i++)
                {
                    Assert.Less(Vector3.Distance(positions[i],nodes[i].localPosition),.000001f);
                    Assert.Less(Quaternion.Angle(rotations[i],nodes[i].localRotation),.02f);
                    Assert.Less(Vector3.Distance(scales[i],nodes[i].localScale),.000001f);
                }
                Assert.AreEqual(expected,Random.value,"Idle personality consumed the gameplay random stream.");
                var eye=nodes.Single(n=>n.name=="ghost-eye-l");
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.None,0);
                float awake=eye.localScale.y;
                companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.SleepySnooze,.9f);
                Assert.Less(eye.localScale.y,awake*.2f,"Sleep only moved the root and never closed the real eyes.");
                companion.Devour(1);
                Assert.IsFalse(companion.SampleIdleForCapture(GhostPetCompanion.FidgetState.HappyHop,.2f));
                Assert.IsFalse(companion.PlayIdleGesture(GhostPetCompanion.FidgetState.CuriousPeek));
            }
            finally {Random.state=random;Object.DestroyImmediate(pet);Object.DestroyImmediate(target);}
        }

        [Test]
        public void TheActualFamiliarHasAWorkingFaceAndReturnsToItsOwnMaterials()
        {
            var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
            Assert.IsNotNull(entry.PetModel,"The reimport lost the roster's actual familiar reference.");
            var target=new GameObject("Kuro owner");
            var pet=Object.Instantiate(entry.PetModel);
            try
            {
                var transforms=pet.GetComponentsInChildren<Transform>();
                var mouth=transforms.Single(t=>t.name=="ghost-mouth");
                var left=transforms.Single(t=>t.name=="ghost-eye-l");
                var tail=transforms.Where(t=>t.name.Contains("ghost-tail")).ToArray();
                Assert.AreEqual(3,tail.Length);
                Assert.IsTrue(transforms.Any(t=>t.name=="ghost-arm-l"));
                var mouthRest=mouth.localScale;var eyeRest=left.localRotation;
                var sourceMaterials=pet.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).Distinct().ToArray();
                var colors=sourceMaterials.Select(m=>m.color).ToArray();
                var companion=pet.AddComponent<GhostPetCompanion>();companion.Bind(target.transform,Vector3.zero,2.38f);
                var calm=transforms.Single(t=>t.name=="CalmForm");
                var rage=transforms.Single(t=>t.name=="RageForm");
                Assert.IsFalse(rage.gameObject.activeSelf,"Both forms appear while following Nemu.");
                Assert.IsNotNull(Resources.Load<AnimationClip>("KuroRageInhale"));
                companion.Devour(1);companion.StepTo(.25f);
                var hand=transforms.Single(t=>t.name=="RageHandLeft");var handBefore=hand.localRotation;
                companion.StepTo(.7f);
                Assert.IsTrue(companion.IsRageFormVisible);Assert.IsTrue(rage.gameObject.activeSelf);
                Assert.IsFalse(calm.gameObject.activeSelf,"Small face remained visible inside the giant.");
                Assert.Greater(Quaternion.Angle(handBefore,hand.localRotation),1,"Authored grasp animation never reached its real hand bone.");
                var renderers=rage.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(renderers);
                var bounds=renderers[0].bounds;
                foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Assert.Greater(bounds.size.y,4.2f,"The giant does not tower over the1.6m players.");
                Assert.Less(bounds.size.y,5.8f,"Giant geometry escaped its authored scale.");
                Assert.Less(bounds.size.x,8,"Geometry reaches outside the4m gameplay radius.");
                // Skinned bounds include unused bind-pose space. This imported
                // nested rig requires BakeMesh's scale compensation; false leaves
                // bone scale in the baked vertices and scales it twice in world.
                float lowest=float.PositiveInfinity;
                foreach(var skin in rage.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var baked=new Mesh();skin.BakeMesh(baked,true);
                    foreach(var vertex in baked.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(vertex).y);
                    Object.DestroyImmediate(baked);
                }
                Assert.GreaterOrEqual(lowest,companion.DevourGround.y-.025f,"The giant's actual surface is buried in the road.");
                var hood=renderers.First(r=>r.name.StartsWith("RageHood_part0")).sharedMaterial.color;
                Assert.Greater(hood.b,hood.g*1.4f,"The exported giant lost its purple palette.");
                Assert.Less(hood.g,.6f,"Viewport colors exported as the default white material.");
                Assert.IsTrue(renderers.Any(r=>r.name.StartsWith("RageEyeLeft")));
                Assert.IsTrue(renderers.Any(r=>r.name.StartsWith("RageEyeRight")));
                Assert.IsTrue(renderers.Any(r=>r.name=="KuroEyeWisp"),"Floating eyes have no spirit aura.");
                Assert.IsFalse(pet.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("KuroTooth")),"Rejected procedural triangle teeth came back.");
                Assert.IsFalse(pet.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),"The visual familiar blocks the street.");
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(GhostPetCompanion).GetMethod("StepDevour",flags).Invoke(companion,new object[]{.5f});
                typeof(GhostPetCompanion).GetMethod("StepReturn",flags).Invoke(companion,new object[]{.85f});
                Assert.IsFalse(companion.IsRageFormVisible);Assert.IsTrue(calm.gameObject.activeSelf);
                Assert.IsFalse(rage.gameObject.activeSelf);
                Assert.AreEqual(mouthRest,mouth.localScale,"The return leaves the familiar screaming.");
                Assert.Less(Quaternion.Angle(eyeRest,left.localRotation),.01f);
                Assert.Less(Vector3.Distance(Vector3.one*2.38f,pet.transform.localScale),.001f);
                companion.StepTo(.7f);
                Assert.AreEqual(mouthRest,mouth.localScale,"Timeline sampling revived an expired transformation.");
                for(int i=0;i<colors.Length;i++)Assert.AreEqual(colors[i],sourceMaterials[i].color,"Rage repainted the shared source material.");
            }
            finally {Object.DestroyImmediate(pet);Object.DestroyImmediate(target);}
        }
    }
}
