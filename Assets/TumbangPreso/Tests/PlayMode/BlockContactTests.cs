using System.Collections;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BlockContactTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator AContinuousBodyContactDeflectsOnceAndSeparationRearmsIt()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round = GameServices.Round; var taya = round.PlayerAt(0); var thrower = round.PlayerAt(1);
            var shoe = thrower.GetComponent<Carrier>().Held;
            int count = 0, credited = -99;
            void Contact(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
            { if (kind == MatchFlair.Kind.Block && subject == 0) { count++; if (count == 1) credited = actor; } }
            MatchFlair.Presented += Contact;
            try
            {
                taya.Teleport(new Vector3(3, .1f, -3));
                shoe.HostThrow(thrower, new Vector3(3, 1.3f, -3.5f), Vector3.forward * .1f);
                for (int i = 0; i < 30 && count == 0; i++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, count, "The first physical contact must still deflect.");
                Assert.AreEqual(1, credited, "The real thrower belongs in the accepted block event.");
                // Follow the rebounding shoe to preserve overlap, as the moving taya
                // did in the captured match. Never invoke the private contact method.
                for (int i = 0; i < 7; i++)
                {
                    Vector3 at = shoe.transform.position; at.y = .1f;
                    taya.Teleport(at + Vector3.forward * .15f);
                    yield return new WaitForFixedUpdate();
                }
                Assert.AreEqual(1, count, "One continuous overlap restarted the impulse and block every physics step.");
                taya.Teleport(new Vector3(-5, .1f, -3)); yield return new WaitForFixedUpdate();
                Vector3 reenter = shoe.transform.position + shoe.Velocity * Time.fixedDeltaTime;
                reenter.y = .1f; taya.Teleport(reenter);
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(2, count, "A later separate contact must still be possible.");
            }
            finally { MatchFlair.Presented -= Contact; }
        }
    }
}
