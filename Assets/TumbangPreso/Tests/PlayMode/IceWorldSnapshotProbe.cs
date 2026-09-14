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
        public IEnumerator RestoredIceKeepsItsRemainingLifeShapeAndCasterResources()
        {
            Floor();
            var owner=new GameObject("Snapshot caster");var system=owner.AddComponent<HeroAbilitySystem>();system.BindHero("cheska");
            system.Kit.Skill1.ApplyNetworkSnapshot(0,1);system.Kit.Skill2.ApplyNetworkSnapshot(0,0);system.Kit.AddUltimateCharge(7);
            var sheet=HeroHazards.SpawnIceSheet(new Vector3(-2,0,0),1.495f,5,1,1.35f).GetComponent<HeroHazards.IceSheetComponent>();
            var wall=HeroHazards.SpawnIceBarricade(new Vector3(2,0,0),Vector3.forward,6,1.4f,.6f,true)
                .GetComponent<HeroHazards.IceBarricadeComponent>();
            sheet.RestoreRemaining(2);wall.RestoreRemaining(3);
            var snapshot=IceWorldSnapshot.Capture();Assert.AreEqual(2,snapshot.Count);
            Assert.IsTrue(IceWorldSnapshot.Apply(snapshot,.4f));
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
            Assert.IsTrue(IceWorldSnapshot.Apply(snapshot,1));yield return null;
            Assert.AreEqual(2,IceWorldSnapshot.Capture().Count,"A repeated complete snapshot duplicated fields.");
            yield return new WaitForSeconds(2.15f);
            Assert.IsEmpty(IceWorldSnapshot.Capture(),"A restored field restarted its full lifetime.");
        }

        [UnityTest]
        public IEnumerator InvalidSnapshotsLeaveTheWorldAndExpiredFieldsDoNotRevive()
        {
            Floor();
            var scenery=GameObject.CreatePrimitive(PrimitiveType.Cube);scenery.name="Unrelated scenery";
            scenery.transform.position=new Vector3(8,.5f,8);
            var wall=HeroHazards.SpawnIceBarricade(Vector3.zero,Vector3.forward,6);
            var fields=IceWorldSnapshot.Capture();Assert.AreEqual(1,fields.Count);
            var invalid=fields[0];invalid.Remaining=float.NaN;
            Assert.IsFalse(IceWorldSnapshot.Apply(new[]{invalid},0));
            Assert.IsTrue(wall.activeInHierarchy);Assert.AreEqual(1,IceWorldSnapshot.Capture().Count);
            var expired=fields[0];expired.Remaining=.1f;
            Assert.IsTrue(IceWorldSnapshot.Apply(new[]{expired},.2f));
            yield return null;
            Assert.IsEmpty(IceWorldSnapshot.Capture());
            Assert.IsTrue(scenery!=null && scenery.activeInHierarchy && scenery.GetComponent<Collider>().enabled);
        }
    }
}
