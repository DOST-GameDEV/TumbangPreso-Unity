using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in separate-process evidence, never an alternative gameplay path.
    // Only seat1's owning client supplies input; host and observer only record it.
    [DefaultExecutionOrder(-250)]
    public sealed class NetThrowProbe : MonoBehaviour
    {
        public static bool Active;
        private StreamWriter _writer;
        private double _began=-1,_next;
        private CharacterMotor _who;
        private bool _warmupPrepared;
        private static string Argument(string key)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);
            return at>=0 && at+1<args.Length ? args[at+1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Active=Argument("-tp-throwtrace")!=null && !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if(!Active)return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(Argument("-tp-throwmode")=="hero" ? GameMode.HeroStrike : GameMode.Classic));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(!Active)return;
            var go=new GameObject("~NetThrowProbe");DontDestroyOnLoad(go);
            var probe=go.AddComponent<NetThrowProbe>();
            go.AddComponent<NetThrowLateSample>().Sample=probe.RecordFrame;
            string path=Path.GetFullPath(Argument("-tp-throwtrace"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer=new StreamWriter(path){AutoFlush=true};
            probe._writer.WriteLine("time,elapsed,host,local,round,held,charge,spin,torso,arm,shoeState,shoeSpin,bodyX,bodyZ,heldSeat,heldState,heldHolder");
        }
        private void Update()
        {
            if(!Active){_writer?.Dispose();_writer=null;enabled=false;return;}
            var round=GameServices.Round;
            if(!NetAuthority.IsNetworked || round==null || GameServices.Match==null)return;
            _who=round.PlayerAt(1);if(_who==null)return;
            foreach(var ai in FindObjectsByType<AIController>(FindObjectsSortMode.None))ai.enabled=false;
            foreach(var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
            foreach(var motor in round.Players)
                if(motor!=null && motor!=_who){motor.Intent.Clear();motor.Intent.Parked=true;}
            if(GameServices.Match.RoundNumber<1)
            {
                if(!_warmupPrepared){_who.Intent.Clear();_who.Intent.CommitFrame();_warmupPrepared=true;}
                if(!NetAuthority.IsHost)return;
                // Reproduce the observed warmup sequence through ordinary movement
                // and Grab, before the owning client arrives. No forced equip/reset.
                var target=FindObjectsByType<Slipper>(FindObjectsSortMode.None).FirstOrDefault(s=>s.SeatOfOrigin==0);
                if(target==null)return;
                bool held=_who.GetComponent<Carrier>().Held==target;
                var delta=target.transform.position-_who.transform.position;
                bool close=delta.magnitude<Balance.PickupRadius*.9f;
                _who.Intent.Parked=false;
                _who.Intent.Move=held || close ? Vector2.zero : new Vector2(delta.x,delta.z).normalized;
                _who.Intent.AimPoint=target.transform.position;_who.Intent.FaceAimPoint=true;
                _who.Intent.Set(Verb.Grab,!held && close && _who.CanAct());
                return;
            }
            if(!round.RoundActive)return;
            var ready=FindFirstObjectByType<ReadyGate>();if(ready!=null && ready.CountingDown)return;
            double now=NetworkManager.Singleton.ServerTime.Time;
            if(_began<0){_who.Intent.Clear();_who.Intent.CommitFrame();_began=now;}
            float elapsed=(float)(now-_began);
            if(NetAuthority.LocalSlot==1)
            {
                _who.Intent.Parked=false;
                _who.Intent.Move=elapsed>2 && elapsed<3 ? Vector2.right*.2f : Vector2.zero;
                _who.Intent.AimPoint=new Vector3(2,.2f,0);_who.Intent.FaceAimPoint=true;
                _who.Intent.SpinInput=elapsed<4 ? 0 : elapsed<10 ? -.7f : .8f;
                _who.Intent.Set(Verb.SpecialAbility,elapsed>=2 && elapsed<18);
            }
            if(elapsed>24){_writer?.Dispose();_writer=null;Application.Quit();}
        }
        private void RecordFrame()
        {
            if(!Active || _writer==null || _who==null || _began<0 || NetworkManager.Singleton==null)return;
            double now=NetworkManager.Singleton.ServerTime.Time;if(now<_next)return;_next=now+.05;
            var carrier=_who.GetComponent<Carrier>();
            var bones=_who.GetComponentsInChildren<Transform>();
            var torso=bones.FirstOrDefault(t=>t.name=="torso");var arm=bones.FirstOrDefault(t=>t.name=="arm-right");
            var shoe=FindObjectsByType<Slipper>(FindObjectsSortMode.None).FirstOrDefault(s=>s.SeatOfOrigin==1);
            _writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F4},{1:F4},{2},{3},{4},{5},{6:F4},{7:F4},{8:F3},{9:F3},{10},{11:F4},{12:F4},{13:F4},{14},{15},{16}",
                now,now-_began,NetAuthority.IsHost?1:0,NetAuthority.LocalSlot,GameServices.Match.RoundNumber,
                carrier.Held!=null?1:0,carrier.ObservedChargePower,carrier.ObservedPektusSpin,
                torso!=null?Mathf.DeltaAngle(0,torso.localEulerAngles.x):0,arm!=null?Mathf.DeltaAngle(0,arm.localEulerAngles.x):0,
                shoe!=null?(int)shoe.State:-1,shoe!=null?shoe.PektusSpin:0,_who.transform.position.x,_who.transform.position.z,
                carrier.Held!=null?carrier.Held.SeatOfOrigin:-1,carrier.Held!=null?(int)carrier.Held.State:-1,
                carrier.Held!=null && carrier.Held.Holder!=null?carrier.Held.Holder.PlayerSlot:-1));
        }
        public static void TraceHoldingWrite(CharacterMotor who,Slipper shoe,string operation)
        {
            if(!Active || who==null || who.PlayerSlot!=1)return;
            Debug.Log($"[ThrowHolderWrite] local={NetAuthority.LocalSlot} host={NetAuthority.IsHost} operation={operation} shoe={(shoe!=null?shoe.SeatOfOrigin:-1)} state={(shoe!=null?(int)shoe.State:-1)} holder={(shoe!=null && shoe.Holder!=null?shoe.Holder.PlayerSlot:-1)}\n{Environment.StackTrace}");
        }
        private void OnDestroy(){_writer?.Dispose();_writer=null;}
    }
    [DefaultExecutionOrder(10000)]
    internal sealed class NetThrowLateSample : MonoBehaviour
    {
        public Action Sample;
        private void LateUpdate()=>Sample?.Invoke();
    }
}
