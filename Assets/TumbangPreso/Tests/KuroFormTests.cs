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
                companion.Devour(1);companion.StepTo(.7f);
                Assert.Greater(mouth.localScale.y,mouthRest.y*5,"The real ghost mouth did not open.");
                Assert.Greater(Quaternion.Angle(eyeRest,left.localRotation),20,"The eyes did not change expression.");
                foreach(var eye in new[] {left,transforms.Single(t=>t.name=="ghost-eye-r")})
                {
                    var a=eye.TransformPoint(Vector3.right*.5f);var b=eye.TransformPoint(Vector3.left*.5f);
                    var centre=pet.transform.position;
                    bool aInner=Vector3.ProjectOnPlane(a-centre,pet.transform.up).sqrMagnitude<Vector3.ProjectOnPlane(b-centre,pet.transform.up).sqrMagnitude;
                    Assert.Less(Vector3.Dot((aInner?a:b)-(aInner?b:a),pet.transform.up),0,
                        "The inner eye corner points upward, making the raging face look sad.");
                }
                Assert.Less(mouth.GetComponent<Renderer>().bounds.max.y,left.GetComponent<Renderer>().bounds.min.y,
                    "The expanded mouth swallowed the angry eye shape.");
                Assert.Greater(pet.transform.localScale.x,2.38f*8,"The raging form stayed narrow.");
                Assert.IsEmpty(pet.GetComponentsInChildren<Collider>(),"The visual familiar blocks the street.");
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(GhostPetCompanion).GetMethod("StepDevour",flags).Invoke(companion,new object[]{.5f});
                typeof(GhostPetCompanion).GetMethod("StepReturn",flags).Invoke(companion,new object[]{.85f});
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
