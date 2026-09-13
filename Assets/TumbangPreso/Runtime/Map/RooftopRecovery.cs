using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    /// <summary>Sa Bubong's actual edge fall and delayed return of lost slippers.</summary>
    public sealed class RooftopRecovery : MonoBehaviour
    {
        public const float RoofY=.1f,HalfX=14,HalfZ=18,SlipperDelay=10;
        public static RooftopRecovery Instance { get; private set; }
        private readonly Dictionary<Slipper,float> _lost=new Dictionary<Slipper,float>();
        private readonly List<Slipper> _finished=new List<Slipper>();
        private SliceRunner _slice;
        private int _round=-1;
        private readonly bool[] _falling=new bool[4];
        private bool Live=>gameObject.scene==SceneManager.GetActiveScene()&&gameObject.scene.name=="SaBubong"&&GameServices.Round!=null;
        private void OnEnable(){if(gameObject.scene==SceneManager.GetActiveScene())Instance=this;}
        private void OnDisable(){if(Instance==this)Instance=null;_lost.Clear();}

        public static bool OutsideDeck(Vector3 p)=>Mathf.Abs(p.x)>HalfX||Mathf.Abs(p.z)>HalfZ;
        public static bool InPool(Vector3 p)=>p.x< -8.2f&&p.x> -13.8f&&p.z>1.55f&&p.z<11.85f;
        public float SecondsUntilReturn(Slipper slipper)=>_lost.TryGetValue(slipper,out float end)?Mathf.Max(0,end-Time.time):0;

        public bool WaitingForReturnWithoutLooseStock()
        {
            if(!Live)return false;
            if(_slice==null)_slice=Object.FindFirstObjectByType<SliceRunner>();
            if(_slice?.Slippers==null)return false;
            bool waiting=false;
            foreach(var shoe in _slice.Slippers)
            {
                if(shoe==null)continue;
                if(shoe.gameObject.activeSelf&&shoe.State==SlipperState.Loose)return false;
                // The defender's deliberately parked stock has OwnerSlot -1.
                if(!shoe.gameObject.activeSelf&&shoe.OwnerSlot>=0)waiting=true;
            }
            return waiting;
        }

        private void SyncRound()
        {
            int round=GameServices.Match!=null?GameServices.Match.RoundNumber:0;
            if(round==_round)return;
            // SliceRunner.ResetWorld reactivates/equips the round's stock. A loss
            // deadline must never recall a newly assigned shoe into another round.
            _round=round;_lost.Clear();System.Array.Clear(_falling,0,_falling.Length);
        }

        public bool TryLoseSlipper(Slipper slipper)
        {
            if(!Live||!NetAuthority.ShouldResolve()||slipper==null||!slipper.gameObject.activeSelf)return false;
            var p=slipper.transform.position;
            bool pastEdge=OutsideDeck(p)&&p.y<=RoofY+slipper.RestHeight+.025f;
            bool unreachable=InPool(p)&&(slipper.State==SlipperState.Loose||p.y<.82f+slipper.RestHeight);
            if(!pastEdge&&!unreachable)return false;
            Lose(slipper);return true;
        }

        private void Lose(Slipper slipper)
        {
            SyncRound();
            if(_lost.ContainsKey(slipper))return;
            if(!slipper.HostBeginMapRecovery())return;
            _lost.Add(slipper,Time.time+SlipperDelay);
        }

        private void FixedUpdate()
        {
            if(!Live||!NetAuthority.ShouldResolve())return;
            Instance=this;
            SyncRound();
            if(_slice==null)_slice=Object.FindFirstObjectByType<SliceRunner>();
            if(_slice!=null&&_slice.Slippers!=null)
                foreach(var slipper in _slice.Slippers)
                    if(slipper!=null&&slipper.State==SlipperState.Loose)TryLoseSlipper(slipper);
            foreach(var who in GameServices.Round.Players)
            {
                if(who==null||!who.gameObject.activeSelf||who.PlayerSlot<0||who.PlayerSlot>=_falling.Length)continue;
                var p=who.transform.position;
                if(who.IsGrounded&&p.y>=RoofY-.05f)_falling[who.PlayerSlot]=false;
                if(OutsideDeck(p)&&p.y<RoofY-.35f)_falling[who.PlayerSlot]=true;
                if(!_falling[who.PlayerSlot]||p.y>=-2)continue;
                // A real two-metre descent has happened. Returning prone keeps
                // the sporting recovery quick, with the existing mash input.
                var held=who.GetComponent<Carrier>()?.Held;
                if(held!=null)Lose(held);
                who.Respawn();who.ApplyFallRecovery();
                _falling[who.PlayerSlot]=false;
            }
            _finished.Clear();
            foreach(var entry in _lost)
            {
                var shoe=entry.Key;
                if(shoe==null||shoe.gameObject.activeSelf){_finished.Add(shoe);continue;}
                if(Time.time<entry.Value)continue;
                shoe.HostFinishMapRecovery();_finished.Add(shoe);
            }
            foreach(var shoe in _finished)_lost.Remove(shoe);
        }
    }
}
