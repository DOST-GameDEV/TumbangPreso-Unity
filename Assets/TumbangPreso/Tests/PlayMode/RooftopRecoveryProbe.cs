using System.Collections;
using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
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
                who.Teleport(new Vector3(17.8f,.1f,0));yield return new WaitForSeconds(.25f);
                input.Move=Vector2.right;yield return new WaitForSeconds(.6f);
                Assert.IsFalse(RooftopRecovery.OutsideDeck(who.transform.position),"Walking passed through the outer fence");
                input.Jump=true;yield return new WaitForSeconds(.10f);input.Jump=false;
                float until=Time.time+3,minY=who.transform.position.y;
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
        public IEnumerator EveryOuterFenceStopsWalkingButAllowsJumpFallAndMashInBothModes()
        {
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong,mode);
                Time.timeScale=1;GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                var input=who.gameObject.AddComponent<RoofInput>();
                Object.FindFirstObjectByType<CameraRig>().SetAimSource(AimSource.Movement);
                var starts=new[]{new Vector3(17.8f,.1f,0),new Vector3(-17.8f,.1f,-8),
                    new Vector3(0,.1f,20.8f),new Vector3(0,.1f,-20.8f)};
                var directions=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
                for(int side=0;side<starts.Length;side++)
                {
                    input.Move=Vector2.zero;input.Jump=false;
                    who.Teleport(starts[side]);yield return new WaitForSeconds(.3f);
                    input.Move=directions[side];yield return new WaitForSeconds(.6f);
                    Assert.IsFalse(RooftopRecovery.OutsideDeck(who.transform.position),$"{mode}/side{side}: fence allows walking through");
                    input.Jump=true;yield return new WaitForSeconds(.1f);input.Jump=false;
                    float until=Time.time+3,minY=who.transform.position.y;
                    while(Time.time<until&&!who.IsTripped){minY=Mathf.Min(minY,who.transform.position.y);yield return null;}
                    input.Move=Vector2.zero;
                    Assert.Less(minY,-.35f,$"{mode}/side{side}: no physical descent");
                    Assert.IsTrue(who.IsTripped,$"{mode}/side{side}: jump did not reach fall recovery");
                    for(int tap=0;tap<14&&who.CanMashUp;tap++)
                    {
                        input.Jump=true;yield return new WaitForSeconds(.065f);
                        input.Jump=false;yield return new WaitForSeconds(.065f);
                    }
                    yield return new WaitForSeconds(.7f);
                    Assert.IsFalse(who.IsTripped,$"{mode}/side{side}: mash recovery did not finish");
                    Debug.Log($"[Roof fence] {mode}/side{side}: walk blocked, jump descent {minY:F3}, mash returned control");
                }
            }
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator OrdinarySpeedFallStruggleAndReturnPresentation()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            Time.timeScale=1;GameServices.Round.BeginRound();
            var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
            var input=who.gameObject.AddComponent<RoofInput>();
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
            who.Teleport(new Vector3(17.8f,.1f,0));yield return new WaitForSeconds(.3f);
            var witness=new GameObject("Roof recovery motion witness").AddComponent<Camera>();
            witness.enabled=false;witness.fieldOfView=52;witness.nearClipPlane=.05f;witness.farClipPlane=400;
            witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var contacts=who.gameObject.AddComponent<RecoveryContacts>();
            bool fell=false;float downAt=-1;
            try
            {
                yield return ImprovementEvidenceProbe.Record(witness,"roof-recovery",9,who,t=>
                {
                    if(who.IsTripped&&!fell){fell=true;downAt=t;}
                    input.Move=!fell?Vector2.right:Vector2.zero;
                    input.Jump=(!fell&&t>.7f&&who.IsGrounded)||
                        (fell&&who.CanMashUp&&t-downAt>.7f&&(t-downAt)%.16f<.065f);
                },new Vector3(-3,1.5f,-3));
                Assert.IsTrue(fell);Assert.IsFalse(who.IsTripped);
                Assert.IsTrue(rig.IsLocalFpp,"Recovery did not restore the ordinary first-person view");
                Assert.Greater(contacts.GroundedSamples,20,"Recovery contact was not measured through the sequence");
                Assert.Greater(contacts.MinimumGap,-.035f,"The recovering body penetrated its real support");
                Assert.Less(contacts.MaximumGap,.035f,"The recovering body floated above its real support");
            }
            finally
            {
                string output=Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/improvement-baseline-v1";
                Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"recovery-contact.csv"),contacts.Rows.ToString());
                Object.Destroy(contacts);input.enabled=false;Object.Destroy(input);Object.Destroy(witness.gameObject);
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
        public IEnumerator OffRoofStockCannotBeGrabbedAndResetCancelsItsDeadline()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            GameServices.Round.BeginRound();
            var who=GameServices.Round.PlayerAt(1);var shoe=who.GetComponent<Carrier>().Held;
            shoe.HostThrow(who,new Vector3(RooftopRecovery.HalfX+2,1.1f,0),Vector3.down);
            yield return new WaitForSeconds(.55f);
            Assert.IsFalse(shoe.gameObject.activeSelf,"Off-roof stock did not enter its loss penalty");
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

        [DefaultExecutionOrder(11000)]
        private sealed class RecoveryContacts:MonoBehaviour
        {
            public readonly StringBuilder Rows=new StringBuilder("time,trip,mash,root_y,drawn_bottom,support_y,palm_y\n");
            public int GroundedSamples;public float MinimumGap=float.PositiveInfinity,MaximumGap=float.NegativeInfinity;
            private Mesh _mesh;private float _next;
            private void LateUpdate()
            {
                if(Time.time<_next)return;_next=Time.time+.05f;
                var who=GetComponent<CharacterMotor>();float bottom=float.PositiveInfinity;
                if(_mesh==null)_mesh=new Mesh();
                foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    skin.BakeMesh(_mesh);
                    foreach(var v in _mesh.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(v).y);
                }
                float support=float.NegativeInfinity;
                foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up*.1f,Vector3.down,2,~0,QueryTriggerInteraction.Ignore))
                    if(hit.normal.y>.5f&&hit.collider.GetComponentInParent<CharacterMotor>()==null&&hit.collider.GetComponentInParent<Slipper>()==null)
                        support=Mathf.Max(support,hit.point.y);
                var hand=GetComponent<CharacterVisual>()?.HandAnchor;
                if(who.IsTripped&&who.IsGrounded&&!float.IsInfinity(support))
                {
                    GroundedSamples++;float gap=bottom-support;
                    MinimumGap=Mathf.Min(MinimumGap,gap);MaximumGap=Mathf.Max(MaximumGap,gap);
                }
                Rows.AppendLine(FormattableString.Invariant($"{Time.time:F4},{who.TripLeft:F4},{who.MashPresses},{transform.position.y:F4},{bottom:F4},{support:F4},{(hand!=null?hand.position.y:0):F4}"));
            }
            private void OnDestroy(){if(_mesh!=null)Object.Destroy(_mesh);}
        }
    }
}
