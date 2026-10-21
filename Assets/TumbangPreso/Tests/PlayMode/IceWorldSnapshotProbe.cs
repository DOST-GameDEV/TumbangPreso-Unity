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

        [UnityTest] public IEnumerator GlacialArcRestoresItsFiveSlabsAndAgedLife()
        {
            Floor();
            var wall=HeroHazards.SpawnIceBarricade(Vector3.zero,Vector3.forward,10,arcLength:5,arcRadius:3)
                .GetComponent<HeroHazards.IceBarricadeComponent>();
            wall.HitsToShatter=3;wall.RestoreRemaining(6);
            var original=wall.GetComponentsInChildren<MeshCollider>().Select(c=>c.transform.localPosition).ToArray();
            Assert.AreEqual(5,original.Length);
            var fields=WorldEffectSnapshot.Capture();Assert.IsTrue(WorldEffectSnapshot.Apply(fields,.5f));yield return null;
            var restored=Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>();
            var slabs=restored.GetComponentsInChildren<MeshCollider>();
            Assert.AreEqual(5,slabs.Length,"Restoring the compact arc must not rebuild the old straight three-piece wall.");
            for(int i=0;i<5;i++)Assert.Less(Vector3.Distance(original[i],slabs[i].transform.localPosition),.001f);
            Assert.That(restored.Remaining,Is.InRange(5.3f,5.51f));
        }
        [UnityTest] public IEnumerator GlacialArcRestoresDamageAlreadyTaken()
        {
            Floor();
            var wall=HeroHazards.SpawnIceBarricade(Vector3.zero,Vector3.forward,10,arcLength:5,arcRadius:3)
                .GetComponent<HeroHazards.IceBarricadeComponent>();
            wall.HitsToShatter=3;wall.HostSlipperHit();wall.HostSlipperHit();
            Assert.IsTrue(WorldEffectSnapshot.Apply(WorldEffectSnapshot.Capture(),0));yield return null;
            var restored=Object.FindFirstObjectByType<HeroHazards.IceBarricadeComponent>();
            Assert.IsNotNull(restored);restored.HostSlipperHit();yield return null;
            Assert.IsTrue(restored==null,"A restored wall that already took two hits must shatter on the next hit.");
        }
        [UnityTest] public IEnumerator GlacialArcReplayKeepsFiveRenderOnlySlabs()
        {
            Floor();HeroHazards.SpawnIceBarricade(Vector3.zero,Vector3.forward,10,arcLength:5,arcRadius:3);
            var field=WorldEffectSnapshot.Capture().Single();var parent=new GameObject("Arc replay witness");
            using(var view=new TumbangPreso.CameraSystem.RecordedFieldView(parent.transform,field))
            {
                Assert.AreEqual(5,view.Root.GetComponentsInChildren<MeshFilter>().Length,"Replay must use the captured arc geometry.");
                Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>());
                Assert.IsNull(view.Root.GetComponentInChildren<HeroHazards.IceBarricadeComponent>());
            }
            Object.Destroy(parent);yield return null;
        }

        [UnityTest] public IEnumerator ArcWallRecordingKeepsRadiusLengthAndRemainingHit()
        {
            var field=new WorldEffectSnapshot.Field {Type=WorldEffectSnapshot.Kind.Barricade,Position=Vector3.zero,
                Forward=Vector3.forward,Duration=10,Remaining=7,Radius=3,FirstScale=5,SecondScale=1,Owner=-1};
            TumbangPreso.CameraSystem.RecordedPoseTrack.Sample Pose(float time)=>new TumbangPreso.CameraSystem.RecordedPoseTrack.Sample
            {Time=time,Epoch=1,Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{true}};
            var end=field;end.Remaining=6;
            var clip=new TumbangPreso.CameraSystem.RecordedMatchClip {MatchId=1,Id=1,Round=1,Actor=1,Subject=-1,
                Mode=GameMode.HeroStrike,Map=TumbangPreso.UI.SceneFlow.Eskinita,Reason="Arc wall witness",Start=0,End=1,Contact=.5f,
                Objects=new[]{new TumbangPreso.CameraSystem.RecordedObjectTrack {Kind=TumbangPreso.CameraSystem.RecordedObjectKind.Can,
                    Seat=-1,Skin=-1,Pose=new TumbangPreso.CameraSystem.RecordedPoseTrack(new[]{""},new[]{Pose(0),Pose(1)})}},
                FieldFrames=new[]{new TumbangPreso.CameraSystem.RecordedFieldFrame {Time=0,Lighting=TumbangPreso.CameraSystem.RecordedEnvironment.Capture(),
                    Fields=new[]{new TumbangPreso.CameraSystem.RecordedField {Id=1,State=field}}},
                    new TumbangPreso.CameraSystem.RecordedFieldFrame {Time=1,Lighting=TumbangPreso.CameraSystem.RecordedEnvironment.Capture(),
                    Fields=new[]{new TumbangPreso.CameraSystem.RecordedField {Id=1,State=end}}}}};
            Assert.IsTrue(TumbangPreso.CameraSystem.RecordedMatchClip.TryDecode(clip.Encode(),out var decoded,out var error),error);
            var restored=decoded.FieldFrames[1].Fields[0].State;
            Assert.AreEqual(3,restored.Radius);Assert.AreEqual(5,restored.FirstScale);Assert.AreEqual(1,restored.SecondScale);
            Assert.AreEqual(6,restored.Remaining);
            byte[] WithVersion(byte[] bytes,int version)
            {
                using var packed=new System.IO.MemoryStream(bytes);
                using var zip=new System.IO.Compression.DeflateStream(packed,System.IO.Compression.CompressionMode.Decompress);
                using var raw=new System.IO.MemoryStream();zip.CopyTo(raw);raw.Position=4;
                using(var writer=new System.IO.BinaryWriter(raw,System.Text.Encoding.UTF8,true))writer.Write(version);
                raw.Position=0;using var output=new System.IO.MemoryStream();
                using(var encoder=new System.IO.Compression.DeflateStream(output,System.IO.Compression.CompressionLevel.Fastest,true))raw.CopyTo(encoder);
                return output.ToArray();
            }
            Assert.IsFalse(TumbangPreso.CameraSystem.RecordedMatchClip.TryDecode(WithVersion(clip.Encode(),13),out _,out var oldArcError));
            StringAssert.Contains("Arc wall",oldArcError);
            foreach(var frame in clip.FieldFrames)
            {
                var legacy=frame.Fields[0].State;legacy.Radius=0;legacy.FirstScale=1;legacy.SecondScale=1;
                frame.Fields[0]=new TumbangPreso.CameraSystem.RecordedField {Id=1,State=legacy};
            }
            Assert.IsTrue(TumbangPreso.CameraSystem.RecordedMatchClip.TryDecode(WithVersion(clip.Encode(),13),out var oldClip,out var oldError),oldError);
            Assert.AreEqual(0,oldClip.FieldFrames[0].Fields[0].State.Radius);
            yield return null;
        }
        [UnityTest] public IEnumerator ArcWallSnapshotRejectsAmbiguousShapeAndHitData()
        {
            var field=new WorldEffectSnapshot.Field {Type=WorldEffectSnapshot.Kind.Barricade,Position=Vector3.zero,
                Forward=Vector3.forward,Duration=10,Remaining=7,Radius=3,FirstScale=5,SecondScale=1,Owner=-1};
            Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            var invalid=field;invalid.SecondScale=.5f;Assert.IsFalse(WorldEffectSnapshot.Valid(invalid));
            invalid=field;invalid.Split=true;Assert.IsFalse(WorldEffectSnapshot.Valid(invalid));
            invalid=field;invalid.FirstScale=20;Assert.IsFalse(WorldEffectSnapshot.Valid(invalid));
            invalid=field;invalid.Radius=.1f;Assert.IsFalse(WorldEffectSnapshot.Valid(invalid));
            yield return null;
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
            system.Kit.Skill1.ApplyNetworkSnapshot(9,0);system.Kit.Skill2.ApplyNetworkSnapshot(13,0);system.Kit.AddUltimateCharge(7);
            float skill1Cooldown=system.Kit.Skill1.CooldownRemaining,skill2Cooldown=system.Kit.Skill2.CooldownRemaining;
            int skill1Charges=system.Kit.Skill1.ChargesRemaining,skill2Charges=system.Kit.Skill2.ChargesRemaining;
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
            Assert.AreEqual(skill1Charges,system.Kit.Skill1.ChargesRemaining);Assert.AreEqual(skill2Charges,system.Kit.Skill2.ChargesRemaining);
            Assert.AreEqual(skill1Cooldown,system.Kit.Skill1.CooldownRemaining,.1f);
            Assert.AreEqual(skill2Cooldown,system.Kit.Skill2.CooldownRemaining,.1f);
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
