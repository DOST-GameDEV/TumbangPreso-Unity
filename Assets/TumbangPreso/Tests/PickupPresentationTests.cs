using NUnit.Framework;
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
