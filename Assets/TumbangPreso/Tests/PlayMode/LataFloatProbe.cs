using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The lata has to land ON the road, not on whatever else a downward ray happens to find
    /// on its first frame. Reported as *"cans are floating"*: see the note on
    /// <see cref="Visual.CharacterNameplate.Build"/> for the race this catches.
    /// </summary>
    public class LataFloatProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator LataSitsOnTheGroundNotOnANameplateRing()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            var lata = Object.FindFirstObjectByType<Lata>();
            Assert.IsNotNull(lata, "no Lata found in scene");

            Assert.Less(lata.transform.position.y, 0.3f,
                $"the lata is sitting at y={lata.transform.position.y:F3}, well above the road. " +
                "It likely snapped to a decorative collider instead of the ground.");
        }
    }
}
