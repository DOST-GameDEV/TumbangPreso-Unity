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
    public sealed class IceWorldSnapshotProbe
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
        public IEnumerator MixedPersistentFieldsRestoreTheirOriginalFormAndRemainingLife()
        {
            Floor();
            HeroHazards.SpawnIceSheet(new Vector3(-7,0,-6),1.495f,5,1,1.35f);
            HeroHazards.SpawnIceBarricade(new Vector3(0,0,-6),Vector3.forward,6,1.4f,.6f,true);
            HeroHazards.SpawnFireTrail(new Vector3(7,0,-6),.8f,5,1,Vector3.right);
            HeroHazards.SpawnShockTrail(new Vector3(-7,0,0),.55f,5,2,1.45f,Vector3.back);
            HeroHazards.SpawnSupernovaCrater(Vector3.zero,2.1f,5,1);
            HeroHazards.SpawnHexSigil(new Vector3(7,0,0),1.44f,6,3,1.4f);
            DanteFissurePillar.Create(new Vector3(-7,0,6),Vector3.right,-1,5);
            yield return null;
            var captured=WorldEffectSnapshot.Capture();
            Assert.AreEqual(7,captured.Count,"The joining snapshot omits active non-ice hazards and solid pillars.");
            for(int i=0;i<captured.Count;i++){var field=captured[i];field.Remaining=1.3f;captured[i]=field;}
            Assert.True(WorldEffectSnapshot.Apply(captured,.25f));yield return null;yield return null;
            var restored=WorldEffectSnapshot.Capture();Assert.AreEqual(7,restored.Count);
            foreach(var source in captured)
            {
                var copy=restored.Single(field=>field.Type==source.Type);
                Assert.Less(Vector3.Distance(source.Position,copy.Position),.02f);
                Assert.AreEqual(source.Radius,copy.Radius,.001f);
                Assert.AreEqual(source.FirstScale,copy.FirstScale,.001f);
                Assert.AreEqual(source.SecondScale,copy.SecondScale,.001f);
                Assert.AreEqual(source.Owner,copy.Owner);Assert.AreEqual(source.Split,copy.Split);
                Assert.Less(Vector3.Distance(source.Forward,copy.Forward),.001f);
                Assert.That(copy.Remaining,Is.InRange(.75f,1.06f));
            }
            var pillar=Object.FindFirstObjectByType<DanteFissurePillar>();
            Assert.Greater(pillar.GetComponentInChildren<MeshCollider>().bounds.size.y,4,
                "Restored mature pillar replayed its birth as a tiny obstacle.");
            Assert.AreEqual(2,Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>()
                .GetComponentsInChildren<MeshCollider>().Length);
            Assert.True(WorldEffectSnapshot.Apply(captured,.35f));yield return null;
            Assert.AreEqual(7,WorldEffectSnapshot.Capture().Count,"A repeated complete batch duplicated a field.");
            yield return new WaitForSeconds(1.15f);
            Assert.IsEmpty(WorldEffectSnapshot.Capture(),"A restored non-ice field restarted its full duration.");
            Assert.IsNull(Object.FindFirstObjectByType<DanteFissurePillar>());
            Assert.IsNull(Object.FindFirstObjectByType<HeroHazards.SupernovaCraterComponent>());
        }

        [UnityTest]
        public IEnumerator RestoredIceKeepsItsRemainingLifeShapeAndCasterResources()
        {
            Floor();
            var owner=new GameObject("Snapshot caster");var system=owner.AddComponent<HeroAbilitySystem>();system.BindHero("cheska");
            system.Kit.Skill1.ApplyNetworkSnapshot(0,1);system.Kit.Skill2.ApplyNetworkSnapshot(0,0);system.Kit.AddUltimateCharge(7);
            var sheet=HeroHazards.SpawnIceSheet(new Vector3(-2,0,0),1.495f,5,1,1.35f).GetComponent<HeroHazards.IceSheetComponent>();
            var wall=HeroHazards.SpawnIceBarricade(new Vector3(2,0,0),Vector3.forward,6,1.4f,.6f,true)
                .GetComponent<HeroHazards.IceBarricadeComponent>();
            sheet.RestoreRemaining(2);wall.RestoreRemaining(3);
            var snapshot=WorldEffectSnapshot.Capture();Assert.AreEqual(2,snapshot.Count);
            Assert.IsTrue(WorldEffectSnapshot.Apply(snapshot,.4f));
            yield return null;
            var restoredSheet=Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>();
            var restoredWall=Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>();
            Assert.AreEqual(1,Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<HeroHazards.IceBarricadeComponent>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1.6f,restoredSheet.Remaining,.12f);Assert.AreEqual(2.6f,restoredWall.Remaining,.12f);
            Assert.AreEqual(1.495f,restoredSheet.Radius,.001f);Assert.AreEqual(1,restoredSheet.OwnerSlot);
            Assert.AreEqual(.3925f,restoredSheet.ChillMultiplier,.001f);Assert.AreEqual(1.35f,restoredSheet.SlipScale,.001f);
            Assert.AreEqual(2,restoredWall.GetComponentsInChildren<MeshCollider>().Length);
            Assert.IsTrue(restoredWall.Split);Assert.AreEqual(1.4f,restoredWall.SpanScale,.001f);
            var surface=restoredSheet.GetComponent<FrostSurfacePresentation>();Assert.AreEqual(5,surface.Duration);
            Assert.Greater(restoredSheet.transform.Find("FrozenSkin").GetComponent<Renderer>().sharedMaterial.GetFloat("_Growth"),1,
                "An already formed sheet replayed its initial growth.");
            Assert.AreEqual(1,system.Kit.Skill1.ChargesRemaining);Assert.Zero(system.Kit.Skill2.ChargesRemaining);
            Assert.AreEqual(7,system.Kit.UltimateCharge);
            Assert.IsTrue(WorldEffectSnapshot.Apply(snapshot,1));yield return null;
            Assert.AreEqual(2,WorldEffectSnapshot.Capture().Count,"A repeated complete snapshot duplicated fields.");
            yield return new WaitForSeconds(2.15f);
            Assert.IsEmpty(WorldEffectSnapshot.Capture(),"A restored field restarted its full lifetime.");
        }

        [UnityTest]
        public IEnumerator InvalidSnapshotsLeaveTheWorldAndExpiredFieldsDoNotRevive()
        {
            Floor();
            var scenery=GameObject.CreatePrimitive(PrimitiveType.Cube);scenery.name="Unrelated scenery";
            scenery.transform.position=new Vector3(8,.5f,8);
            var wall=HeroHazards.SpawnIceBarricade(Vector3.zero,Vector3.forward,6);
            var fields=WorldEffectSnapshot.Capture();Assert.AreEqual(1,fields.Count);
            var invalid=fields[0];invalid.Remaining=float.NaN;
            Assert.IsFalse(WorldEffectSnapshot.Apply(new[]{invalid},0));
            Assert.IsTrue(wall.activeInHierarchy);Assert.AreEqual(1,WorldEffectSnapshot.Capture().Count);
            var expired=fields[0];expired.Remaining=.1f;
            Assert.IsTrue(WorldEffectSnapshot.Apply(new[]{expired},.2f));
            yield return null;
            Assert.IsEmpty(WorldEffectSnapshot.Capture());
            Assert.IsTrue(scenery!=null && scenery.activeInHierarchy && scenery.GetComponent<Collider>().enabled);
        }
    }
}
