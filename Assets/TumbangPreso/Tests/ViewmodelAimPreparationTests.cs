using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ViewmodelAimPreparationTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [TestCase("frost-sweep")]
        [TestCase("raise-barricade")]
        [TestCase("summon-lightning")]
        [TestCase("cast-hex")]
        [TestCase("blink")]
        public void HeldPreparationAndCancellationConvergeAcrossFrameRates(string action)
        {
            Quaternion expected=Quaternion.identity;
            foreach (int fps in new[] {30,60,144})
            {
                var go=new GameObject("Aim preparation contract");
                try
                {
                    var arms=go.AddComponent<ViewmodelArms>();arms.EnsureBuilt();
                    var right=(Transform)typeof(ViewmodelArms).GetField("_rightArm",Private).GetValue(arms);
                    var step=typeof(ViewmodelArms).GetMethod("StepAction",Private);
                    arms.SetAimPreview(action);
                    for(int i=0;i<fps/2;i++)step.Invoke(arms,new object[]{1f/fps});
                    Assert.Greater(Quaternion.Angle(Quaternion.identity,right.localRotation),5,"Held aim has no visible preparation.");
                    if(fps==30)expected=right.localRotation;
                    else Assert.Less(Quaternion.Angle(expected,right.localRotation),.05f);
                    var before=right.localRotation;
                    Assert.IsTrue(arms.PlayAction(action));
                    Assert.Less(Quaternion.Angle(before,right.localRotation),.001f,"Release snaps before the first animation sample.");
                    arms.CancelAction(action);
                    arms.SetAimPreview(null);
                    for(int i=0;i<fps/2;i++)step.Invoke(arms,new object[]{1f/fps});
                    Assert.Less(Quaternion.Angle(Quaternion.identity,right.localRotation),.05f,"Interrupted preparation is stuck on screen.");
                }
                finally {Object.DestroyImmediate(go);}
            }
        }

        [Test]
        public void ThrowChargeOwnsTheGripWhileAnAimPreviewIsRequested()
        {
            var go=new GameObject("Charge priority contract");
            try
            {
                var arms=go.AddComponent<ViewmodelArms>();arms.EnsureBuilt();
                var right=(Transform)typeof(ViewmodelArms).GetField("_rightArm",Private).GetValue(arms);
                var step=typeof(ViewmodelArms).GetMethod("StepAction",Private);
                arms.SetCharge(.65f);step.Invoke(arms,new object[]{.02f});
                var charge=right.localRotation;
                arms.SetAimPreview("frost-sweep");step.Invoke(arms,new object[]{.02f});
                Assert.Less(Quaternion.Angle(charge,right.localRotation),.001f);
            }
            finally {Object.DestroyImmediate(go);}
        }
    }
}
