using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Playables;

namespace TumbangPreso.Tests
{
    public sealed class ThrowMotionTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [Test]
        public void FullWindupMovesTheTorsoAndOffHandOnEveryRetainedPerson()
        {
            foreach(var entry in Resources.Load<RosterBook>("RosterBook").People)
            {
                var seat=new GameObject("Throw motion "+entry.Id);
                CharacterAnimator driver=null;
                try
                {
                    var motor=seat.AddComponent<CharacterMotor>();Call(motor,"Awake");
                    var carrier=seat.AddComponent<Carrier>();Call(carrier,"Awake");
                    var shoe=new GameObject("Held test slipper");shoe.transform.SetParent(seat.transform);
                    var slipper=shoe.AddComponent<Slipper>();
                    typeof(Carrier).GetProperty("Held").GetSetMethod(true).Invoke(carrier,new object[]{slipper});
                    motor.HoldingSlipper=true;
                    driver=seat.AddComponent<CharacterAnimator>();Call(driver,"Awake");
                    var model=Object.Instantiate(entry.Model,seat.transform);driver.Bind(model,entry.Clips);
                    var graph=(PlayableGraph)typeof(CharacterAnimator).GetField("_graph",Private).GetValue(driver);
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);graph.Evaluate(0);
                    var torso=model.GetComponentsInChildren<Transform>().First(t=>t.name=="torso");
                    var off=model.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-left");
                    var leg=model.GetComponentsInChildren<Transform>().First(t=>t.name=="leg-left");
                    var torsoBefore=torso.localRotation;var offBefore=off.localRotation;var legBefore=leg.localRotation;
                    carrier.ApplyObservedCharge(true);
                    typeof(Carrier).GetField("_observedCharge",Private).SetValue(carrier,TumbangPreso.Core.Balance.ChargeFullTime);
                    Call(driver,"StepChargePose");graph.Evaluate(0);Call(driver,"LateUpdate");
                    Assert.Greater(Quaternion.Angle(torsoBefore,torso.localRotation),6,entry.Id+" has no body preparation.");
                    Assert.Greater(Quaternion.Angle(offBefore,off.localRotation),6,entry.Id+" has no balancing off-hand.");
                    Assert.Less(Quaternion.Angle(legBefore,leg.localRotation),.1f,"Upper-body windup must leave the gait's legs alone.");
                    carrier.ApplyObservedCharge(false);Call(driver,"StepChargePose");graph.Evaluate(0);Call(driver,"LateUpdate");
                    Assert.Less(Quaternion.Angle(torsoBefore,torso.localRotation),.1f,entry.Id+" retains the cancelled windup.");
                }
                finally{if(driver!=null)Call(driver,"ReleaseGraph");Object.DestroyImmediate(seat);}
            }
        }
        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void PreparingEitherCurveKeepsTheFirstPersonGripAtArmsLength(float spin)
        {
            var root=new GameObject("First person grip framing");
            try
            {
                var arms=root.AddComponent<CameraSystem.ViewmodelArms>();arms.EnsureBuilt();arms.SetHolding(true);
                var hand=root.transform.Find("RightPivot/Arm");
                arms.SetCharge(0);arms.StepVisuals(0,true);
                var before=hand.TransformPoint(Vector3.up*CameraSystem.ViewmodelArms.ArmLength);
                arms.SetCharge(1,spin);arms.StepVisuals(0,true);
                var prepared=hand.TransformPoint(Vector3.up*CameraSystem.ViewmodelArms.ArmLength);
                Assert.Greater(prepared.z,.40f,"The slipper rotated back into the camera.");
                Assert.Greater(prepared.y,-.22f,"Pektus dropped the hand below the frame.");
                Assert.Less(Vector3.Distance(before,prepared),.20f,"Preparation lost the established grip composition.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void SignedReleaseAndInterruptionSettleAtThirtySixtyAnd144Frames(float spin)
        {
            foreach(int fps in new[]{30,60,144})
            {
                var root=new GameObject("Throw timing contract");
                try
                {
                    var arms=root.AddComponent<CameraSystem.ViewmodelArms>();arms.EnsureBuilt();arms.SetHolding(true);
                    var hand=root.transform.Find("RightPivot/Arm");
                    arms.SetCharge(1,spin);arms.StepVisuals(0,true);
                    var before=hand.localRotation;arms.SetCharge(-1);
                    Assert.IsTrue(arms.PlayAction(ThrowGesture.Action(spin)));
                    Assert.Less(Quaternion.Angle(before,hand.localRotation),.001f,"Release snapped before the first sample.");
                    for(int i=0;i<fps;i++)arms.StepVisuals(1f/fps);
                    Assert.Less(Quaternion.Angle(Quaternion.identity,hand.localRotation),.1f,"The completed throw left the wrist twisted.");
                    arms.PlayAction(ThrowGesture.Action(spin));arms.StepVisuals(.05f);arms.CancelAction();
                    for(int i=0;i<fps;i++)arms.StepVisuals(1f/fps);
                    Assert.Less(Quaternion.Angle(Quaternion.identity,hand.localRotation),.1f,"Interrupted throw failed to settle.");
                }
                finally{Object.DestroyImmediate(root);}
            }
        }

        private static object Call(object target,string name,params object[] args)
            =>target.GetType().GetMethod(name,Private).Invoke(target,args);
    }
}
