using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
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
        [UnityTest] public IEnumerator RemoteWindupsAndRoleChangesKeepTheirOwnMeaning()
        {
            yield return Load(SceneFlow.BayanPlaza);
            var round=GameServices.Round;var taya=round.PlayerAt(0);var thrower=round.PlayerAt(2);
            var verbs=taya.GetComponent<CombatVerbs>();var carrier=thrower.GetComponent<Carrier>();
            var camera=Camera.main;var rig=camera.GetComponent<CameraRig>();rig.enabled=false;
            taya.Teleport(new Vector3(0,taya.transform.position.y,2));taya.transform.rotation=Quaternion.Euler(0,180,0);
            thrower.Teleport(new Vector3(2.5f,thrower.transform.position.y,2));thrower.transform.rotation=Quaternion.Euler(0,180,0);
            var rpc=Net.MatchRpc.Instance;bool owned=rpc==null;if(owned)rpc=new GameObject("Windup message witness").AddComponent<Net.MatchRpc>();
            var provider=NetAuthority.Provider;NetAuthority.Provider=new ClockObserver();
            var plate=taya.GetComponentInChildren<CharacterNameplate>();var ring=plate.transform.Find("NameplateRing");
            var shoe=carrier.Held;var beforePosition=taya.transform.position;
            try
            {
                SendWindup(rpc,0,true,.8f,1);Assert.Greater(verbs.ObservedLungeCharge,0);
                Assert.AreEqual(0,verbs.LungeChargeRatio,"Remote presentation must not start the gameplay charge.");
                Assert.AreEqual(0,verbs.LungeCooldownLeft);
                SendWindup(rpc,0,false,0,1,99);Assert.Greater(verbs.ObservedLungeCharge,0,"Non-host packets cannot cancel a pose.");
                SendWindup(rpc,0,true,float.NaN,1);Assert.IsTrue(float.IsFinite(verbs.ObservedLungeCharge));
                SendWindup(rpc,2,true,.8f,null);Assert.Greater(carrier.ObservedChargePower,0,"Legacy throw prefix is still accepted.");
                SendWindup(rpc,0,false,0,2);Assert.Greater(verbs.ObservedLungeCharge,0,"Unknown suffix cannot rewrite a pose.");
                foreach(float distance in new[]{8f,12f})foreach(string state in new[]{"before","after","comfort"})
                {
                    WorldCueProfile.Current.ExchangePoses=state=="before"?0:1;
                    Settings.SettingsStore.Current.ReducedEffects=state=="comfort";Settings.SettingsStore.Current.ReducedUiMotion=state=="comfort";
                    camera.transform.position=taya.transform.position+new Vector3(0,1.3f,-distance);
                    camera.transform.LookAt(taya.transform.position+new Vector3(.8f,.8f,0));camera.fieldOfView=75;
                    SendWindup(rpc,0,true,Balance.LungeChargeTime*.8f,1);SendWindup(rpc,2,true,Balance.ChargeFullTime*.8f,null);
                    yield return null;yield return null;
                    yield return GameplayShots.Render(camera,"windups-"+distance+"m-"+state,false,Output,taya,960,540);
                }
                SendWindup(rpc,0,false,0,1);Assert.AreEqual(-1,verbs.ObservedLungeCharge);
                SendWindup(rpc,2,false,0,null);yield return new WaitForSeconds(ThrowGesture.CancelSeconds+.05f);
                Assert.AreSame(shoe,carrier.Held);Assert.AreEqual(SlipperState.Held,shoe.State);
                SendWindup(rpc,0,true,.3f,1);yield return new WaitForSeconds(.9f);Assert.AreEqual(-1,verbs.ObservedLungeCharge,"A lost stop expires without a stuck windup.");
                Assert.AreEqual(0,verbs.LungeCooldownLeft);Assert.Less(Vector2.Distance(new Vector2(beforePosition.x,beforePosition.z),new Vector2(taya.transform.position.x,taya.transform.position.z)),.01f);
                Settings.SettingsStore.Current.ReducedUiMotion=false;
                round.ApplySnapshot(round.TimeLeft,true,1);round.ApplySnapshot(round.TimeLeft,true,0);
                Vector3 rest=ring.localScale;yield return new WaitForSeconds(.10f);yield return null;
                Assert.Greater(ring.localScale.x,rest.x,"The changed role gets one brief ring beat.");
                yield return new WaitForSeconds(.4f);Assert.AreEqual(rest.x,ring.localScale.x,.001f);
                Settings.SettingsStore.Current.ReducedUiMotion=true;round.ApplySnapshot(round.TimeLeft,true,1);round.ApplySnapshot(round.TimeLeft,true,0);
                yield return new WaitForSeconds(.1f);Assert.AreEqual(rest.x,ring.localScale.x,.001f);
                var boundary=Object.FindAnyObjectByType<CourtBoundaryPresentation>();
                round.EndRound();yield return null;Assert.IsFalse(boundary.Armed);
                round.BeginRound();yield return null;Assert.IsTrue(boundary.Armed);
                float r=Balance.ConfinementRadius;
                Assert.IsTrue(MotionFoley.TryChalkCrossing(new Vector3(r-.2f,0,0),new Vector3(r+.2f,0,0),out var crossing));Assert.AreEqual(r,crossing.x,.0001f);
                Assert.IsFalse(MotionFoley.TryChalkCrossing(new Vector3(r-.2f,0,r+2),new Vector3(r+.2f,0,r+2),out _));
                Assert.IsFalse(MotionFoley.TryChalkCrossing(Vector3.zero,Vector3.right,out _));
            }
            finally{NetAuthority.Provider=provider;rig.enabled=true;if(owned)Object.Destroy(rpc.gameObject);}
        }
        private static void SendWindup(Net.MatchRpc rpc,int seat,bool active,float seconds,byte? kind,ulong sender=0)
        {
            using var writer=new FastBufferWriter(24,Allocator.Temp);
            writer.WriteValueSafe(seat);writer.WriteValueSafe(active);writer.WriteValueSafe(seconds);writer.WriteValueSafe(0f);
            if(kind.HasValue)writer.WriteValueSafe(kind.Value);
            using var reader=new FastBufferReader(writer,Allocator.Temp);Private(rpc,"OnThrowChargeMsg",sender,reader);
        }
    }
}
