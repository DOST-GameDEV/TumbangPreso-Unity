using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed partial class WorldCourtCueTests
    {
        private static void Private(object owner,string method,params object[] args)
            =>owner.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(owner,args);
        [UnityTest] public IEnumerator TayaTargetsAreCameraScopedAndReadableAtTenMetres()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var round=GameServices.Round;var taya=round.PlayerAt(0);var target=round.PlayerAt(1);
            taya.Teleport(new Vector3(0,taya.transform.position.y,-6));target.Teleport(new Vector3(0,target.transform.position.y,4));
            foreach(var actor in round.Players)if(actor!=target && actor!=taya)actor.Teleport(new Vector3(6,actor.transform.position.y,6));
            var cam=Camera.main;var rig=cam.GetComponent<CameraRig>();rig.Follow(taya);rig.enabled=false;
            cam.transform.position=taya.transform.position+Vector3.up*1.3f;cam.transform.LookAt(target.transform.position+Vector3.up*.7f);
            yield return null;Assert.IsTrue(target.IsTaggable());Assert.IsTrue(CharacterVisual.CatchableFor(cam,target));
            var visual=target.GetComponent<CharacterVisual>();var plate=target.GetComponentInChildren<CharacterNameplate>();
            Assert.IsNotNull(plate);var surface=visual.Model.GetComponentInChildren<Renderer>();var block=new MaterialPropertyBlock();
            surface.GetPropertyBlock(block);float original=block.GetFloat("_RimStrength");
            var disc=plate.transform.Find("NameplateRing").GetComponent<MeshFilter>();var originalMesh=disc.sharedMesh;
            var observer=new GameObject("Neutral nested view").AddComponent<Camera>();observer.enabled=false;
            Private(visual,"BeginReadability",cam);Private(plate,"BeginCatchable",cam);
            surface.GetPropertyBlock(block);Assert.AreEqual(1,block.GetFloat("_TayaCue"));Assert.Greater(block.GetFloat("_RimStrength"),0);
            Assert.AreEqual("Catchable four brackets",disc.sharedMesh.name);
            Private(visual,"BeginReadability",observer);Private(plate,"BeginCatchable",observer);
            surface.GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_TayaCue"));Assert.AreSame(originalMesh,disc.sharedMesh);
            Private(visual,"EndReadability",observer);Private(plate,"EndCatchable",observer);
            surface.GetPropertyBlock(block);Assert.AreEqual(1,block.GetFloat("_TayaCue"));Assert.AreEqual("Catchable four brackets",disc.sharedMesh.name);
            Private(visual,"EndReadability",cam);Private(plate,"EndCatchable",cam);
            surface.GetPropertyBlock(block);Assert.AreEqual(original,block.GetFloat("_RimStrength"));Assert.AreSame(originalMesh,disc.sharedMesh);
            Object.Destroy(observer.gameObject);
            foreach(string state in new[]{"before","after","comfort"})
            {
                WorldCueProfile.Current.TayaTarget=state=="before"?0:1;
                WorldCueProfile.Current.DistanceReadability=state=="before"?0:1;
                SettingsStore.Current.ReducedEffects=state=="comfort";SettingsStore.Current.HighContrastHud=state=="comfort";
                SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                yield return GameplayShots.Render(cam,"taya-10m-"+state,false,Output,target,960,540);
            }
            rig.Follow(target);Assert.IsFalse(CharacterVisual.CatchableFor(cam,target));
            rig.Follow(taya);target.Teleport(new Vector3(8,target.transform.position.y,4));yield return null;
            Assert.IsFalse(CharacterVisual.CatchableFor(cam,target),"An outside attacker cannot carry a catchable cue.");
            rig.enabled=true;
        }
        [UnityTest] public IEnumerator ActualRestoreAndProtectionClocksSurviveRecording()
        {
            yield return Load(SceneFlow.Eskinita);
            var round=GameServices.Round;var taya=round.PlayerAt(0);var lata=round.Lata;var carrier=taya.GetComponent<Carrier>();
            var clock=LataClockPresentation.For(lata);Assert.IsNotNull(clock);
            yield return new WaitForSeconds(lata.ProtectionLeft+.05f);lata.HostKnockDown(1);Hitstop.End();yield return new WaitForSeconds(.22f);
            taya.Teleport(lata.transform.position+Vector3.back*.6f);
            for(int i=0;i<=Balance.SpawnSettleFrames;i++)yield return new WaitForFixedUpdate();
            taya.Intent.Parked=false;taya.Intent.Set(Verb.Grab,true);
            yield return new WaitForSeconds(.3f);
            Assert.Greater(carrier.ChannelRatio,.05f);Assert.AreEqual(carrier.ChannelRatio,clock.RestoreRatio,.04f);
            // Preserve the actual in-progress channel during the comparison.
            // A paused gameplay frame otherwise makes Carrier.CanAct false and
            // correctly cancels the hold before the camera renders it.
            carrier.enabled=false;Time.timeScale=0;yield return null;
            var source=MatchReplayArchive.PropModel(lata.gameObject);var history=new MatchPoseHistory.Track(null,source);
            var first=history.Capture(10);LataClockPresentation.Unpack(first.State,out float firstRestore,out _);
            Assert.AreEqual(clock.RestoreRatio,firstRestore,1f/255);
            var cameraGo=new GameObject("Shared clock witness");var cam=cameraGo.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.fieldOfView=58;
            // The first witness stood behind the working taya and photographed
            // their back. This front/side angle shows the actual can and collar.
            cam.transform.position=lata.transform.position+new Vector3(2,1.4f,3.5f);cam.transform.LookAt(lata.transform.position+Vector3.up*.1f);
            typeof(GameplayShots).GetMethod("Grade",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{cam});
            foreach(string state in new[]{"before","after","comfort"})
            {
                WorldCueProfile.Current.RestoreClock=state=="before"?0:1;
                SettingsStore.Current.ReducedEffects=state=="comfort";SettingsStore.Current.HighContrastHud=state=="comfort";SettingsStore.Current.HudScale=state=="comfort"?1.2f:1;
                yield return null;
                Assert.Greater(clock.RestoreRatio,.05f,"The witness must retain actual restore progress at capture time.");
                Assert.AreEqual(state!="before",clock.GetComponent<Renderer>().enabled);
                yield return GameplayShots.Render(cam,"restore-"+state,false,Output,taya,960,540);
            }
            carrier.enabled=true;Time.timeScale=1;taya.Intent.Set(Verb.Grab,false);yield return new WaitForSeconds(.06f);
            // The coroutine resumes before the clock's LateUpdate. Let the
            // released input and its presentation both publish that frame.
            yield return null;
            Assert.AreEqual(0,clock.RestoreRatio,.001f,"Cancel removes the clock immediately.");
            taya.Intent.Set(Verb.Grab,true);yield return new WaitForSeconds(lata.ResetChannelTime+.1f);
            taya.Intent.Set(Verb.Grab,false);yield return null;Assert.IsTrue(lata.IsUpright);Assert.Greater(clock.ProtectionRatio,0);
            Time.timeScale=0;yield return null;var last=history.Capture(11);
            LataClockPresentation.Unpack(last.State,out float lastRestore,out float protectedRatio);
            Assert.AreEqual(0,lastRestore);Assert.AreEqual(clock.ProtectionRatio,protectedRatio,1f/255);
            var clip=new RecordedMatchClip{MatchId=1,Id=1,Round=1,Actor=0,Subject=-1,Mode=GameMode.Classic,Map=SceneFlow.Eskinita,Reason="Clock witness",Start=10,End=11,Contact=10.5f,
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Can,Seat=-1,Skin=lata.SkinIndex,VisualKey=MatchReplayArchive.VisualKey(source),
                    Pose=new RecordedPoseTrack(new[]{""},new[]{RootOnly(first),RootOnly(last)})}}};
            Assert.IsTrue(RecordedMatchClip.TryDecode(clip.Encode(),out var decoded,out var error),error);
            Assert.AreEqual(first.State,decoded.Objects[0].Pose.Samples[0].State);Assert.AreEqual(last.State,decoded.Objects[0].Pose.Samples[1].State);
            // The existing bounded state field fits both clocks and its old flags.
            Assert.LessOrEqual(last.State,2048);LataClockPresentation.Unpack(3,out float legacyRestore,out float legacyProtection);
            Assert.AreEqual(0,legacyRestore+legacyProtection,"Old clips must not invent unavailable timing.");
            foreach(string state in new[]{"before","after"})
            {
                WorldCueProfile.Current.RestoreClock=state=="before"?0:1;yield return null;
                yield return GameplayShots.Render(cam,"protection-"+state,false,Output,taya,960,540);
            }
            var record=LataClockPresentation.Install(cameraGo.transform,null,true);
            record.Draw(lata.transform.position,lata.transform.rotation,firstRestore,0);
            Assert.AreEqual(firstRestore,record.RestoreRatio);Assert.AreEqual(0,record.ProtectionRatio);
            Assert.IsTrue(lata.IsProtected,"Recorded clock may not mutate the live can.");
            Time.timeScale=1;yield return new WaitForSeconds(lata.ProtectionLeft+.05f);
            Assert.AreEqual(0,clock.ProtectionRatio,.001f);Object.Destroy(cameraGo);
        }
        private static RecordedPoseTrack.Sample RootOnly(RecordedPoseTrack.Sample sample)
        {
            sample.Positions=new[]{sample.Positions[0]};sample.Rotations=new[]{sample.Rotations[0]};
            sample.Scales=new[]{sample.Scales[0]};sample.Active=new[]{sample.Active[0]};return sample;
        }
        private sealed class ClockObserver : INetProvider
        {public bool IsHost=>false;public bool IsNetworked=>true;public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;}
        [UnityTest] public IEnumerator ExistingCanMessageAcceptsOptionalClockAndKeepsLegacyPrefix()
        {
            yield return Load(SceneFlow.BayanPlaza);var lata=GameServices.Round.Lata;var clock=LataClockPresentation.For(lata);
            yield return new WaitForSeconds(lata.ProtectionLeft+.05f);lata.HostKnockDown(1);Hitstop.End();yield return null;
            var original=NetAuthority.Provider;var rpc=Net.MatchRpc.Instance;
            bool owned=rpc==null;if(owned)rpc=new GameObject("Clock message fixture").AddComponent<Net.MatchRpc>();
            try
            {
                NetAuthority.Provider=new ClockObserver();
                SendClock(rpc,lata,false,0,0);yield return null;Assert.AreEqual(0,clock.RestoreRatio);
                SendClock(rpc,lata,true,.4f,0);yield return null;
                Assert.GreaterOrEqual(clock.RestoreRatio,.4f);Assert.IsFalse(lata.IsUpright);Assert.AreEqual(0,lata.ProtectionLeft);
                SendClock(rpc,lata,true,float.NaN,0);yield return null;
                Assert.IsTrue(float.IsFinite(clock.RestoreRatio));
                SendClock(rpc,lata,true,0,0);yield return null;Assert.AreEqual(0,clock.RestoreRatio);
                SendClock(rpc,lata,true,.5f,0);yield return new WaitForSeconds(.7f);Assert.AreEqual(0,clock.RestoreRatio,"Stale remote progress expires.");
                SendClock(rpc,lata,true,.5f,0,99);yield return null;Assert.AreEqual(0,clock.RestoreRatio,"Non-host data may not write a clock.");
            }
            finally{NetAuthority.Provider=original;if(owned)Object.Destroy(rpc.gameObject);}
        }
        private static void SendClock(Net.MatchRpc rpc,Lata lata,bool suffix,float restore,float protection,ulong sender=0)
        {
            using var writer=new FastBufferWriter(64,Allocator.Temp);
            writer.WriteValueSafe(lata.transform.position);writer.WriteValueSafe(lata.transform.rotation);
            writer.WriteValueSafe(lata.IsUpright);writer.WriteValueSafe(lata.SkinIndex);
            if(suffix){writer.WriteValueSafe((byte)1);writer.WriteValueSafe(restore);writer.WriteValueSafe(protection);}
            using var reader=new FastBufferReader(writer,Allocator.Temp);
            Private(rpc,"OnSyncLataMsg",sender,reader);
        }
    }
}
