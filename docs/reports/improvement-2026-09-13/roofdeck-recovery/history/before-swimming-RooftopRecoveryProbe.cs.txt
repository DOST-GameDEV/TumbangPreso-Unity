using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class RooftopRecoveryProbe
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest,Timeout(180000)]
        public IEnumerator ActualEdgeFallUsesMashAndDelaysSlipperReturnInBothModes()
        {
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong,mode);
                Time.timeScale=1;GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);var shoe=who.GetComponent<Carrier>().Held;
                Assert.IsNotNull(shoe);who.Intent.Parked=false;
                var input=who.gameObject.AddComponent<RoofInput>();
                Object.FindFirstObjectByType<CameraRig>().SetAimSource(AimSource.Movement);
                who.Teleport(new Vector3(13.0f,.1f,0));yield return new WaitForSeconds(.25f);
                input.Move=Vector2.right;float until=Time.time+3,minY=who.transform.position.y;
                while(Time.time<until&&!who.IsTripped)
                {minY=Mathf.Min(minY,who.transform.position.y);yield return null;}
                input.Move=Vector2.zero;
                Assert.Less(minY,-.35f,"This must be a real descent past the ledge,not an early trigger on the roof");
                Assert.IsTrue(who.IsTripped,"Falling returned no mashable recovery");
                Assert.IsFalse(shoe.gameObject.activeSelf);
                Assert.IsFalse(shoe.IsGrabbableIgnoringReach(who),"A delayed request can grab an unavailable shoe");
                Assert.IsFalse(who.HoldingSlipper);
                var recovery=Object.FindFirstObjectByType<RooftopRecovery>();
                Assert.That(recovery.SecondsUntilReturn(shoe),Is.InRange(9.7f,10f));
                input.Jump=true;yield return new WaitForSeconds(.65f);
                Assert.AreEqual(1,who.MashPresses,"Holding the input must not manufacture repeated presses");
                input.Jump=false;yield return new WaitForSeconds(.12f);
                int pulses=0;
                while(who.CanMashUp&&pulses++<16)
                {
                    input.Jump=true;yield return new WaitForSeconds(.065f);
                    input.Jump=false;yield return new WaitForSeconds(.065f);
                }
                yield return new WaitForSeconds(.5f);
                Assert.IsFalse(who.IsTripped,"Accepted press edges did not finish getting up");
                Assert.IsFalse(who.IsStunned,"Standing up left a hidden trip stun behind");
                yield return new WaitForSeconds(Mathf.Max(0,recovery.SecondsUntilReturn(shoe)-.12f));
                Assert.IsFalse(shoe.gameObject.activeSelf,"The ten-second penalty returned early");
                yield return new WaitForSeconds(.24f);
                Assert.IsTrue(shoe.gameObject.activeSelf);
                Assert.AreEqual(SlipperState.Loose,shoe.State);
                Assert.IsFalse(RooftopRecovery.OutsideDeck(shoe.transform.position));
                Assert.IsFalse(RooftopRecovery.InPool(shoe.transform.position));
                who.Teleport(shoe.transform.position+Vector3.back*.25f);yield return new WaitForSeconds(.2f);
                Assert.IsTrue(shoe.HostGrab(who),"Returned shoe is still not retrievable");
                Debug.Log($"[Roof recovery] {mode}: actual descent {minY:F3},held input1press,{pulses}tap pulses,10s return and actual pickup");
            }
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator ObjectiveSnapshotsOnlyAnnounceRealTransitions()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var can=GameServices.Round.Lata;
            int events=0;can.UprightChanged+=up=>events++;
            for(int i=0;i<6;i++)can.ApplySnapshotState(can.transform.position,Quaternion.identity,true,can.SkinIndex);
            Assert.AreEqual(0,events,"Steady state replayed a restored announcement");
            can.ApplySnapshotState(can.transform.position,Quaternion.Euler(90,0,0),false,can.SkinIndex);
            can.ApplySnapshotState(can.transform.position,Quaternion.Euler(90,0,0),false,can.SkinIndex);
            Assert.AreEqual(1,events);
            can.ApplySnapshotState(can.transform.position,Quaternion.identity,true,can.SkinIndex);
            can.ApplySnapshotState(can.transform.position,Quaternion.identity,true,can.SkinIndex);
            Assert.AreEqual(2,events,"Real restore did not announce exactly once");
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator InaccessiblePoolStockCannotBeGrabbedAndResetCancelsItsDeadline()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            GameServices.Round.BeginRound();
            var who=GameServices.Round.PlayerAt(1);var shoe=who.GetComponent<Carrier>().Held;
            shoe.HostThrow(who,new Vector3(-10.7f,1.1f,6.7f),Vector3.down);
            yield return new WaitForSeconds(.55f);
            Assert.IsFalse(shoe.gameObject.activeSelf,"The fenced pool stranded a shoe instead of recovering it");
            who.Teleport(shoe.transform.position);
            Assert.IsFalse(shoe.HostGrab(who),"Inactive stock accepted an authoritative grab");
            var recovery=Object.FindFirstObjectByType<RooftopRecovery>();
            Object.FindFirstObjectByType<SliceRunner>().ResetWorld(0);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(shoe.gameObject.activeSelf);
            Assert.AreEqual(0,recovery.SecondsUntilReturn(shoe));
            shoe.HostForceEquip(who);yield return new WaitForFixedUpdate();
            Assert.AreSame(shoe,who.GetComponent<Carrier>().Held);
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator LaundryMovesBelowFixedPegs()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            yield return new WaitForSeconds(.15f);
            var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            int cloths=0;float motion=0,pinned=0;
            foreach(var filter in filters)
            {
                if(!filter.name.StartsWith("Cloth_"))continue;
                cloths++;
                var before=filter.sharedMesh.vertices;
                yield return new WaitForSeconds(.17f);
                var after=filter.sharedMesh.vertices;
                for(int i=0;i<before.Length;i++)
                {
                    float distance=Vector3.Distance(before[i],after[i]);motion=Mathf.Max(motion,distance);
                    if(Mathf.Abs(before[i].y)<.001f)pinned=Mathf.Max(pinned,distance);
                }
            }
            Assert.AreEqual(4,cloths);
            Assert.Greater(motion,.003f,"Imported clothes are static or the local vertical axis is wrong");
            Assert.Less(pinned,.0001f,"The pinned top of the garment detached from its line");
        }

        // Exercises the real intent consumer and physics. Keyboard/controller/
        // touch reader and separate-process qualification is a separate matrix.
        private sealed class RoofInput:MonoBehaviour
        {
            public Vector2 Move;public bool Jump;
            private void Update(){var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;who.Intent.Set(Verb.Jump,Jump);}
        }
    }
}
