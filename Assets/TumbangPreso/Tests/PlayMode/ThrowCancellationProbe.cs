using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowCancellationProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator CancelledWindupReturnsToCarryAndAllowsImmediateAction()
        {
            yield return MapRetrievalProbe.Load("BayanPlaza");
            var who = GameServices.Round.PlayerAt(1);
            who.Teleport(new Vector3(0, .18f, -8)); who.transform.rotation = Quaternion.identity;
            var carrier = who.GetComponent<Carrier>(); var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            var animator = who.GetComponent<CharacterAnimator>();
            var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(who, true); rig.SetAimSource(AimSource.Movement);
            var camera = new GameObject("Cancel witness").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var cancel = typeof(CharacterAnimator).GetField("_throwCancelTime", BindingFlags.Instance | BindingFlags.NonPublic);
            var release = typeof(CharacterAnimator).GetField("_throwReleaseTime", BindingFlags.Instance | BindingFlags.NonPublic);
            int throws = 0; bool sawReturn = false, returned = false;
            void Count(MatchFlair.Kind kind, int actor, int target, Vector3 at, float strength)
            { if (kind == MatchFlair.Kind.Throw) throws++; }
            MatchFlair.Presented += Count;
            try
            {
                // The shared observed-charge seam drives the same body and owner
                // preparation. No invented successful throw or real input writer.
                yield return ImprovementEvidenceProbe.Record(camera, "charge-cancel", 3, who, t =>
                {
                    carrier.ApplyObservedCharge(t >= .3f && t < 1.3f, Balance.ChargeFullTime * .9f, .65f);
                    float phase = (float)cancel.GetValue(animator);
                    sawReturn |= t > 1.3f && phase >= 0;
                    returned |= t > 1.7f && phase < 0;
                }, new Vector3(2.4f, 1.2f, -2.6f));
                Assert.IsTrue(sawReturn); Assert.IsTrue(returned);
                Assert.AreSame(shoe, carrier.Held); Assert.AreEqual(SlipperState.Held, shoe.State);
                Assert.AreEqual(0, throws, "Cancellation cannot invent a release event.");
                carrier.ApplyObservedCharge(true, Balance.ChargeFullTime * .9f);
                yield return null; yield return null;
                carrier.ApplyObservedCharge(false);
                yield return null; yield return null;
                Assert.GreaterOrEqual((float)cancel.GetValue(animator), 0);
                animator.PlayAction("throw");
                Assert.Less((float)cancel.GetValue(animator), 0, "An accepted action preempts return immediately.");
                Assert.AreEqual(0, (float)release.GetValue(animator));
                // Render-only action does not award or create physical ammunition.
                Assert.AreSame(shoe, carrier.Held); Assert.AreEqual(0, throws);
            }
            finally
            {
                MatchFlair.Presented -= Count; carrier.ApplyObservedCharge(false);
                Object.Destroy(camera.gameObject);
            }
        }
    }
}
