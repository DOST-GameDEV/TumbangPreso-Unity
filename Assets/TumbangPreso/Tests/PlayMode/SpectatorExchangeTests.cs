using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorExchangeTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator DownCanRecoveryFramesTheOwnerAndTheirActualShoe()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round=GameServices.Round;var owner=round.PlayerAt(1);var taya=round.PlayerAt(0);
            foreach(var actor in round.Players)actor.Teleport(new Vector3(8,.12f,-7+actor.PlayerSlot*3));
            taya.Teleport(new Vector3(1.5f,.12f,0));owner.Teleport(new Vector3(0,.12f,-3));
            var shoe=Object.FindObjectsByType<Slipper>().First(s=>s.OwnerSlot==1);
            shoe.HostThrow(owner,round.Lata.transform.position+new Vector3(0,1.2f,-2),Vector3.forward*12);
            float until=Time.time+3;while((round.Lata.IsUpright||shoe.State!=SlipperState.Loose)&&Time.time<until)yield return null;
            Assert.IsFalse(round.Lata.IsUpright);Assert.AreEqual(SlipperState.Loose,shoe.State);
            owner.Teleport(shoe.transform.position+Vector3.back*2);
            var model=new SpectatorInterestModel();
            try
            {
                var interest=model.Decide();Assert.AreEqual(SpectatorBeat.Retrieval,interest.Beat);
                Assert.AreSame(owner,interest.Main);Assert.AreSame(taya,interest.Secondary);Assert.AreSame(shoe,interest.RetrievalShoe);
                Assert.AreEqual(shoe.transform.position,interest.Objective);Assert.AreEqual(ShotType.RetrievalTwoShot,interest.Shot);
                Assert.IsFalse(owner.IsTaggable(),"Camera interest must not invent a legal tag while the can is down");
                yield return new WaitForSeconds(1.3f);
                Assert.AreEqual(interest.StartedAt,model.Decide().StartedAt,"A developing recovery keeps one camera commitment");
            }
            finally{model.Unhook();}
        }
        [UnityTest]
        public IEnumerator CatchStopsTrackingTheVictimsTeleportWhileTheTayaCanContinue()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round=GameServices.Round;var taya=round.PlayerAt(0);var victim=round.PlayerAt(1);
            foreach(var actor in round.Players)actor.Teleport(new Vector3(8,.12f,-7+actor.PlayerSlot*3));
            taya.Teleport(new Vector3(0,.12f,-4));victim.Teleport(new Vector3(0,.12f,-3));taya.transform.forward=Vector3.forward;
            var model=new SpectatorInterestModel();
            try
            {
                Assert.AreSame(victim,model.Decide().Main);
                Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position,taya.transform.forward));
                Assert.IsTrue(victim.IsStunned);var next=model.Decide();
                Assert.AreNotSame(victim,next.Main,"A live spectator must not follow the caught body back to spawn");
                Assert.IsTrue(taya.CanAct());Assert.IsFalse(PresentationClock.Held);
            }
            finally{model.Unhook();}
        }
    }
}
