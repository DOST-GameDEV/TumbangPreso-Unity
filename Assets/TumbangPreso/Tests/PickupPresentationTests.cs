using NUnit.Framework;
using TumbangPreso.Visual;
using System.Reflection;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The pickup's first-person hand (owner, 2026-11-07: "make it all feel better"). `Carrier.PresentPickup`
    /// asks the arm for `pickup` or, on a slide, `scoop` through a ternary, which the literal scan in
    /// `NationalsHardeningTests.EveryVerbActionNameResolvesOnTheViewmodelArm` cannot see, so they are named here.
    /// </summary>
    public sealed class PickupPresentationTests
    {
        [Test]
        public void PollingTheHeldSkinKeepsThePickupAtItsStreetOriginUntilTheLift()
        {
            var root = new GameObject("Pickup polling regression");
            var actor = new GameObject("Pickup source actor"); actor.transform.SetParent(root.transform);
            var carrier = actor.AddComponent<Carrier>();
            var source = new GameObject("Pickup source shoe"); source.transform.SetParent(root.transform);
            var shoe = source.AddComponent<Slipper>();
            var camera = new GameObject("Pickup eye").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
            camera.transform.position = Vector3.up * 1.5f; camera.enabled = false;
            var handRoot = new GameObject("Pickup hands"); handRoot.transform.SetParent(camera.transform, false);
            try
            {
                Object.Instantiate(RosterBook.Load().Slippers[0].Model, source.transform);
                var arms = handRoot.AddComponent<CameraSystem.ViewmodelArms>(); arms.EnsureBuilt();
                void Set(string name, object value) => typeof(Carrier).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(carrier, value);
                Vector3 ground = new Vector3(0, .07f, .65f);
                typeof(Carrier).GetProperty("Held").SetValue(carrier, shoe);
                float duration = PickupMotion.Seconds(PickupStyle.Standing);
                Set("_tossFrom", ground); Set("_tossDuration", duration); Set("_tossLeft", duration);
                var held = arms.transform.Find("RightPivot/Arm/HeldSlipper");
                for (int poll = 0; poll < 2; poll++)
                {
                    arms.SetPickupSource(carrier); arms.SetHolding(true); arms.MatchSkin(shoe);
                    arms.RefreshPickupPresentation();
                    Assert.Less(Vector3.Distance(arms.transform.InverseTransformPoint(held.position), arms.DrawnFromWorld(ground)), .001f,
                        "The live holding/skin poll replaced the street origin with the fixed carry pose.");
                }
                Set("_tossLeft", duration * .5f);
                arms.SetPickupSource(carrier); arms.StepVisuals(.1f); arms.MatchSkin(shoe);
                Vector3 palm = held.localPosition;
                arms.RefreshPickupPresentation();
                Assert.Less(Vector3.Distance(palm, held.localPosition), .001f,
                    "After contact the shoe must rise with the hand, rather than lag behind it.");
                Set("_tossLeft", 0f);
                arms.SetPickupSource(carrier); arms.MatchSkin(shoe); arms.RefreshPickupPresentation();
                Assert.Greater(Vector3.Distance(arms.transform.InverseTransformPoint(held.position), arms.DrawnFromWorld(ground)), .15f);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PickupMotionBendsAfterGrabAndDistinguishesTheCurrentMovement()
        {
            Assert.AreEqual(PickupStyle.Standing, PickupMotion.Choose(false, false, false, 0, false));
            Assert.AreEqual(PickupStyle.Moving, PickupMotion.Choose(false, false, false, 3, false));
            Assert.AreEqual(PickupStyle.RunStop, PickupMotion.Choose(false, false, false, 0, true));
            Assert.AreEqual(PickupStyle.Crouched, PickupMotion.Choose(false, true, false, 0, true));
            Assert.AreEqual(PickupStyle.Sliding, PickupMotion.Choose(true, true, false, 7, true));
            Assert.AreEqual(PickupStyle.QuickSnatch, PickupMotion.Choose(false, false, true, 0, false));
            Assert.AreEqual(0, PickupMotion.Bend(0));
            Assert.AreEqual(1, PickupMotion.Bend(.30f), "The bend reaches the shoe before lifting it.");
            Assert.AreEqual(0, PickupMotion.Lift(.30f), "The shoe stays on the street until the hand reaches it.");
            Assert.AreEqual(0, PickupMotion.Bend(1));
            Assert.AreEqual(1, PickupMotion.Lift(1));
            Assert.Greater(PickupMotion.EyeDrop(PickupStyle.RunStop), PickupMotion.EyeDrop(PickupStyle.Crouched));
        }

        [Test]
        public void ThePickupAndScoopHandsResolveOnTheViewmodelArm()
        {
            var go = new GameObject("PickupArmsProbe");
            go.SetActive(false);
            try
            {
                var arms = go.AddComponent<CameraSystem.ViewmodelArms>();
                Assert.IsTrue(arms.PlayAction("pickup"), "The first-person pickup reach resolves to no clip.");
                Assert.IsTrue(arms.PlayAction("scoop"), "The first-person slide scoop resolves to no clip.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
