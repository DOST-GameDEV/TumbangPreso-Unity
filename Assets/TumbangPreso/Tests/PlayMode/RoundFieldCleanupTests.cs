using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RoundFieldCleanupTests
    {
        private bool _bots,_spectator,_pinned;private int _seat;private Core.CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest]
        public IEnumerator RoundEndRemovesLiveFieldsImmediatelyButKeepsMapAndRecordedGeometry()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,Core.GameMode.HeroStrike);
            var mapObstacle=new GameObject("Authored obstacle probe");var permanent=HazardVolume.Attach(mapObstacle,1,-1);int mapHazards=HazardMap.Count;
            var at=new Vector3(3,0,3);
            HeroHazards.SpawnIceSheet(at,2,8,1,1,silent:true);
            HeroHazards.SpawnIceBarricade(at+Vector3.right*3,Vector3.forward,8,1,1,false,silent:true);
            HeroHazards.SpawnFireTrail(at+Vector3.back*3,1,8,1,Vector3.forward);
            HeroHazards.SpawnShockTrail(at+Vector3.left*3,1,8,2,1,Vector3.forward);
            HeroHazards.SpawnSupernovaCrater(at+Vector3.forward*3,2,8,1);
            HeroHazards.SpawnHexSigil(at+Vector3.right*5,2,8,3,1,silent:true);
            var pillar=DanteFissurePillar.Create(at+Vector3.left*5,Vector3.forward,1,8);
            yield return null;
            var fields=WorldEffectSnapshot.Capture();Assert.AreEqual(7,fields.Count);
            var stage=new GameObject("Retained field lifetime proof");
            using(var recorded=new RecordedFieldView(stage.transform,fields.First(f=>f.Type==WorldEffectSnapshot.Kind.Fissure)))
            {
                GameServices.Round.EndRound();
                foreach(var field in fields)Assert.IsFalse(field.Source.activeInHierarchy,"An old field stayed active while the round clock was stopped.");
                Assert.IsEmpty(WorldEffectSnapshot.Capture());
                Assert.IsTrue(permanent.isActiveAndEnabled);Assert.AreEqual(mapHazards,HazardMap.Count);
                Assert.IsNotNull(recorded.Root,"Round cleanup destroyed pure recorded geometry.");
                Assert.IsEmpty(recorded.Root.GetComponentsInChildren<Collider>(true));
                Object.FindAnyObjectByType<SliceRunner>().ResetWorld(1);
                Assert.AreEqual(mapHazards,HazardMap.Count,"New-round reset forgot permanent map avoidance.");
                Assert.IsTrue(permanent.isActiveAndEnabled);
                yield return null;
                Assert.IsTrue(pillar==null,"The old solid pillar survived deferred destruction.");
            }
            Object.Destroy(stage);Object.Destroy(mapObstacle);
        }

        [UnityTest]
        public IEnumerator AcceptedRoundInactiveSnapshotRetiresReplicaFields()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,Core.GameMode.HeroStrike);
            var pillar=DanteFissurePillar.Create(Vector3.zero,Vector3.forward,1,8);
            yield return null;Assert.IsTrue(pillar.gameObject.activeSelf);
            GameServices.Round.ApplySnapshot(0,false,0,true);
            Assert.IsFalse(pillar.gameObject.activeInHierarchy);
            Assert.IsEmpty(WorldEffectSnapshot.Capture());
            yield return null;Assert.IsTrue(pillar==null);
        }
    }
}
