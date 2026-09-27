using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class IceWorldSnapshotProbe
    {
        private sealed class SnapshotOwner : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SnapshotReceiverRejectsPredictionsAndNewerEventsBeforeReplacingFields()
        {
            var previous = NetAuthority.Provider;
            GameObject root = null;
            try
            {
                yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
                GameServices.Match.ApplySnapshot(new int[4], 1, true);
                GameServices.Round.ApplySnapshot(60, true, 0, true);
                var actor = GameServices.Round.PlayerAt(1);
                actor.AbilitySystem.BindHero("cheska");
                NetAuthority.Provider = new SnapshotOwner();
                root = new GameObject("Snapshot receiver contract"); root.SetActive(false);
                var router = root.AddComponent<MatchRpc>();
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 12345L);
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var begin = typeof(MatchRpc).GetMethod("OnWorldFieldBeginMsg", flags);
                var end = typeof(MatchRpc).GetMethod("OnWorldFieldEndMsg", flags);
                var pending = typeof(MatchRpc).GetField("_worldFieldBatch", flags);
                var accepted = typeof(MatchRpc).GetField("_lastWorldFieldGeneration", flags);
                var sentinel = HeroHazards.SpawnIceSheet(Vector3.zero, 1, 5, 1, 1);
                var header = new WorldSnapshotHeader
                {
                    Match = 12345, Round = 1, Generation = 1, Count = 0, RoundClock = 60,
                    Scene = new FixedString128Bytes(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name),
                };
                void Begin()
                {
                    using var writer = new FastBufferWriter(WorldSnapshotHeader.MaxWireBytes, Allocator.Temp);
                    writer.WriteNetworkSerializable(header);
                    using var reader = new FastBufferReader(writer, Allocator.Temp);
                    begin.Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
                }
                void End()
                {
                    using var writer = new FastBufferWriter(4, Allocator.Temp);
                    writer.WriteValueSafe(header.Generation);
                    using var reader = new FastBufferReader(writer, Allocator.Temp);
                    end.Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
                }
                actor.AbilitySystem.TrackSkillRequest(0, 11);
                Begin(); Assert.IsNull(pending.GetValue(router));
                Assert.IsTrue(sentinel.activeSelf);
                header.Generation = 2; header.OwnerRequest = 11;
                Begin(); Assert.IsNotNull(pending.GetValue(router));
                actor.AbilitySystem.TrackSkillRequest(0, 12);
                End(); Assert.Zero((int)accepted.GetValue(router));
                Assert.IsTrue(sentinel.activeSelf, "An in-flight snapshot erased a newer local prediction.");
                var events = (long[])typeof(MatchRpc).GetField("_lastSkillEvent", flags).GetValue(router);
                events[2] = 9;
                header.Generation = 3; header.OwnerRequest = 12; header.SkillEvent = 8;
                Begin(); Assert.IsNull(pending.GetValue(router));
                header.Generation = 4; header.SkillEvent = 9;
                Begin(); End();
                Assert.AreEqual(4, (int)accepted.GetValue(router));
                Assert.IsFalse(sentinel.activeSelf, "A current complete empty snapshot did not replace fields.");
            }
            finally
            {
                NetAuthority.Provider = previous;
                if (root != null) Object.Destroy(root);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RestoredPlantsBindTheirOwnersAndOneResetPreservesAnotherPlayersPlant()
        {
            yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            var one = GameServices.Round.PlayerAt(1);
            var two = GameServices.Round.PlayerAt(2);
            one.IsDefender = two.IsDefender = false;
            one.AbilitySystem.BindHero("paete"); two.AbilitySystem.BindHero("paete");
            var first = PaetePlant.Restore(new Vector3(-10, 0, -8), 1, 4, 0);
            PaetePlant.Restore(new Vector3(10, 0, -8), 2, 5, 1);
            var fields = WorldEffectSnapshot.Capture().Where(f => f.Type == WorldEffectSnapshot.Kind.Plant).ToArray();
            Assert.AreEqual(2, fields.Length);
            Assert.IsTrue(WorldEffectSnapshot.Apply(fields, .25f));
            var restored = PaetePlant.OwnedBy(1);
            var other = PaetePlant.OwnedBy(2);
            Assert.IsNotNull(restored); Assert.IsNotNull(other);
            Assert.AreNotSame(first, restored);
            Assert.IsFalse(first.isActiveAndEnabled);
            var skill = one.AbilitySystem.Kit.AttackingSkill;
            Assert.IsTrue(skill.IsActive, "A visible restored plant left its owning ability inactive.");
            Assert.AreEqual(PaeteRules.PlantLifeSeconds - 4.25f, skill.DurationRemaining, .001f);
            Assert.IsTrue(skill.ReactivateReady);
            var context = new AbilityContext(one, one.GetComponent<Carrier>(), one.GetComponent<CombatVerbs>(),
                one.transform.position, Vector3.forward, Vector3.zero);
            skill.Reactivate(context);
            Assert.AreSame(restored, PaetePlant.OwnedBy(1));
            Assert.IsFalse(restored.ShotReady);
            Assert.AreEqual(2, WorldEffectSnapshot.Capture().Count(f => f.Type == WorldEffectSnapshot.Kind.Plant));
            one.AbilitySystem.ResetKit();
            Assert.IsNull(PaetePlant.OwnedBy(1));
            yield return null;
            Assert.IsTrue(other != null && other.isActiveAndEnabled, "Resetting one kit destroyed another player's plant.");
        }

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
