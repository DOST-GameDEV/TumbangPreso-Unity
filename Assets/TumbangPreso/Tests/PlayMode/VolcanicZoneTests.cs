using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Dante's cracked ground cools, then goes back into the road.
    ///
    /// ⚠️⚠️ THESE TWO BEHAVIOURS ARE THE ONLY PART OF THE VOLCANIC PASS NO CAPTURE CAN SHOW, AND
    /// THAT IS THE WHOLE REASON THE FILE EXISTS. `AbilityShowcaseProbe` runs in EDIT MODE, where
    /// `Update` never fires: every frame it takes is the zone at age zero. So the cooling curve
    /// and the sink are invisible to the harness that checks everything else about this effect,
    /// and `docs/VISION.md` § 5 is explicit that eyeballing has been wrong repeatedly and
    /// expensively here.
    ///
    /// ⚠️ IT ASSERTS THE DIRECTION AND THE ENDPOINT, NOT THE CURVE. The sink accelerates on
    /// `t * t` and the cooling holds and then falls, and pinning either shape here would mean
    /// re-authoring the test every time somebody adjusts the feel. What must not regress is that
    /// the zone is DOWN at the end, that it is fully under the road rather than part way, and
    /// that it was still up while it was the newest thing on the court.
    /// </summary>
    public class VolcanicZoneTests
    {
        private const float Radius = 2.2f;

        /// <summary>
        /// ⚠️ LONGER THAN THE 4.0 s THE STOMP ACTUALLY USES, TO BUY WALL-CLOCK MARGIN RATHER THAN
        /// TO TEST A DIFFERENT THING. Every boundary here is a FRACTION of the life, so the
        /// behaviour under test is identical at any duration; what a longer life buys is that the
        /// gap between "fully sunk" at 0.94 and "deleted" at 1.0 is 180 ms instead of 70, which
        /// is the difference between a sample that lands reliably and one that races a frame.
        /// `docs/TODO.md` § 6 has what a test that depends on machine load costs this project.
        /// </summary>
        private const float Life = 3.0f;

        [UnityTest]
        public IEnumerator TheCrackedGroundSinksUnderTheRoadBeforeItIsDeleted()
        {
            var zone = HeroHazards.SpawnCrackedLavaDecal(Vector3.zero, Radius, Life);
            Assert.IsNotNull(zone, "the decal did not spawn.");

            var cooling = zone.GetComponent<VolcanicCooling>();
            Assert.IsNotNull(cooling, "the decal has no VolcanicCooling, so nothing ends it.");

            float startY = zone.transform.position.y;

            // ⚠️ THE TALLEST PIECE, NOT THE DECAL, IS WHAT HAS TO CLEAR THE SURFACE. The crust
            // lies at 22 mm; an upheaval slab stands about `radius * 0.45`. A sink that only
            // buried the flat part would leave slabs standing in a hole and would pass a test
            // written against the decal's own height.
            float tallest = Radius * 0.45f;

            // Hold: still up while it is the newest thing on the court.
            yield return new WaitForSeconds(Life * 0.5f);

            Assert.AreEqual(startY, zone.transform.position.y, 0.001f,
                "the zone started sinking during the hold, so it looks spent while it is fresh.");

            // ⚠️ SAMPLED AT 0.96 OF THE LIFE: AFTER THE SINK FINISHES AT 0.94 AND BEFORE
            // `ExpiryCue` DELETES THE OBJECT AT 1.0. Both bounds matter. Sampling before 0.94
            // catches the zone mid-descent and measures a partial drop, which is what failed
            // when the sink ran all the way to 1.0; sampling after 1.0 is a null reference
            // rather than a failure.
            yield return new WaitForSeconds(Life * 0.46f);

            Assert.IsTrue(zone != null,
                "the zone was deleted before the sink could be measured; ExpiryCue is early.");

            float sunkY = zone.transform.position.y;
            float dropped = startY - sunkY;

            Assert.Greater(dropped, tallest,
                $"the zone dropped {dropped:F2} m, which does not clear its tallest slab at " +
                $"{tallest:F2} m, so it is still visible when it is deleted.");
        }

        [UnityTest]
        public IEnumerator TheRockCoolsRatherThanHoldingFullHeatToTheLastFrame()
        {
            var zone = HeroHazards.SpawnCrackedLavaDecal(Vector3.zero, Radius, Life);
            Assert.IsNotNull(zone);

            // The lava's own light is the glow, because there is no bloom pass in this project.
            // If it were still at full intensity on the last frame, the zone would go out by
            // being deleted rather than by cooling.
            var light = zone.GetComponentInChildren<Light>();
            Assert.IsNotNull(light, "the decal has no light, so the lava does not glow at all.");

            float startIntensity = light.intensity;
            Assert.Greater(startIntensity, 0.0f, "the lava light starts dark.");

            yield return new WaitForSeconds(Life * 0.9f);

            Assert.IsTrue(zone != null && light != null,
                "the zone was deleted before the cooling could be measured.");

            Assert.Less(light.intensity, startIntensity * 0.5f,
                $"the lava light is still at {light.intensity:F2} of a starting " +
                $"{startIntensity:F2} near the end of its life, so a spent zone and a live one " +
                "look the same and a player crossing it has nothing to read.");
        }
    }
}
