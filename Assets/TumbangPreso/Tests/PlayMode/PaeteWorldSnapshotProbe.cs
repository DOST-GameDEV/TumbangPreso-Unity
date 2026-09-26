using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S THREE WORLD EFFECTS, PUT BACK FOR A REJOINER (TODO HERO-9, "NEXT" row (3)). `WorldEffectSnapshot.Kind.Plant`,
    /// `Thorns` and `Sentry` were captured, validated, restored and drawn in replay since protocol 55, and no test had ever
    /// applied one. The first question this answers found a real fault: a restored sentry ran 0.45 s behind everybody else's
    /// for its whole life, because `PaeteSentry.Spawn` took the seed's flight off an age that was already past it. Template:
    /// `IceWorldSnapshotProbe`.
    /// </summary>
    public sealed class PaeteWorldSnapshotProbe
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        private static void Floor()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";
            floor.transform.position=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(30,.5f,30);
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator APlantThornsAndAMidCrawlSentryComeBackAtTheirOwnAgeAndPlace()
        {
            Floor();
            // A seedling six seconds old with nine to its next shot; a thorn construct a second in; a guardian mid-crawl, between
            // its second and third haul (`PaeteSentryBody.Heaves`), which is where a wrong age shows most.
            PaetePlant.Restore(new Vector3(-4,0,-3),1,6f,9f);
            PaeteThorns.Restore(new Vector3(4,0,-3),2,1f);
            PaeteSentry.Spawn(new Vector3(0,0,1),new Vector3(0,0,4),1,1.12f);
            yield return null;
            var captured=WorldEffectSnapshot.Capture();
            Assert.AreEqual(3,captured.Count,"The joining snapshot omits one of Paete's plant, thorns or sentry.");
            float sentryAge=Object.FindFirstObjectByType<PaeteSentry>().Age;

            Assert.True(WorldEffectSnapshot.Apply(captured,.25f));
            yield return null;
            var restored=WorldEffectSnapshot.Capture();
            Assert.AreEqual(3,restored.Count);
            foreach(var source in captured)
            {
                var copy=restored.Single(field=>field.Type==source.Type);
                Assert.Less(Vector3.Distance(source.Position,copy.Position),.02f,$"{source.Type} moved on restore.");
                Assert.AreEqual(source.Owner,copy.Owner,$"{source.Type} changed owner on restore.");
                Assert.That(copy.Remaining,Is.InRange(source.Remaining-.45f,source.Remaining-.2f),$"{source.Type} restored at the wrong age.");
            }
            // The plant's shot clock carries over (nine seconds less what the snapshot was late by), not a fresh reload.
            var plant=restored.Single(field=>field.Type==WorldEffectSnapshot.Kind.Plant);
            Assert.That(plant.FirstScale,Is.InRange(8.6f,8.85f),"The seedling's next shot restarted or skipped on restore.");
            // ⚠️ THE SENTRY IS AT THE SAME AGE, NOT 0.45 S BEHIND (it was, before this probe).
            var sentry=Object.FindFirstObjectByType<PaeteSentry>();
            Assert.AreEqual(sentryAge+.25f,sentry.Age,.1f,"A rejoiner's guardian runs on a different clock from everybody else's.");
            // And it does not race its roots under the court a second time: that happened once, at the cast.
            Assert.IsEmpty(Object.FindObjectsByType<PaeteRootRidge>(FindObjectsSortMode.None),"A restored guardian sent its roots again.");

            Assert.True(WorldEffectSnapshot.Apply(captured,.35f));yield return null;
            Assert.AreEqual(3,WorldEffectSnapshot.Capture().Count,"A repeated complete batch duplicated one of Paete's effects.");
            Assert.AreEqual(1,Object.FindObjectsByType<PaeteSentry>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,PaetePlant.Live.Count);
        }
    }
}
