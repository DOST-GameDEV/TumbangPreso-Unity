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
        [UnityTest]
        public IEnumerator LataSitsOnTheGroundNotOnANameplateRing()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            var lata = Object.FindFirstObjectByType<Lata>();
            Assert.IsNotNull(lata, "no Lata found in scene");

            Assert.Less(lata.transform.position.y, 0.3f,
                $"the lata is sitting at y={lata.transform.position.y:F3}, well above the road. " +
                "It likely snapped to a decorative collider instead of the ground.");
        }
    }
}
