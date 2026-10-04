using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's fall (docs/ARENA_MAP_BRIEF.md, ARENA-1.1 point 3). The host catches a body
    /// that drops under the stage, a drone carries it back to the last safe place it stood, and
    /// it then takes the tag's own five seconds (owner: "same 5 second tag freeze").
    ///
    /// ⚠️ THE CATCH IS AT `ArenaStage.CatchY`, ABOVE TWO OTHER FLOORS. `MatchRpc.AcceptMove`
    /// refuses a pose under y -5, so a remote body the host waited any longer for would stop
    /// arriving, and the `KillPlane` at -10 returns a body to its round spawn, which is the
    /// one place the owner's rule says a fall must not send it. That plane stays as the last
    /// resort.
    ///
    /// ⚠️ ONLY THE HOST DECIDES (`NetAuthority.ShouldResolve()`, round or free roam alike, as
    /// `RooftopRecovery` does). Nothing here is sent: the carry is the `Drone` edge recovery
    /// kind, whose kind, phase and pose already ride `SyncUnit`. What every peer does run is
    /// `Update`, which only puts the grey-box drone over a body whose replicated kind is Drone.
    /// </summary>
    public sealed class ArenaFallRecovery : MonoBehaviour
    {
        public const float SlipperDelay=8,SafeMargin=1,SafeRefresh=.2f;
        public static ArenaFallRecovery Instance { get; private set; }
        private static readonly RaycastHit[] FloorHits=new RaycastHit[16];
        private static readonly Vector3[] Around={Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
        private readonly Dictionary<Slipper,float> _lost=new Dictionary<Slipper,float>();
        private readonly List<Slipper> _finished=new List<Slipper>();
        private readonly Dictionary<CharacterMotor,Vector3> _safe=new Dictionary<CharacterMotor,Vector3>();
        private readonly Dictionary<CharacterMotor,ArenaDrone> _drones=new Dictionary<CharacterMotor,ArenaDrone>();
        private Transform _droneRoot;
        private ArenaStage _stage;
        private SliceRunner _slice;
        private int _round=-1;
        private float _nextSafe;
        private bool Live=>gameObject.scene==SceneManager.GetActiveScene()&&ArenaStage.Instance!=null&&GameServices.Round!=null;

        private void OnEnable(){if(gameObject.scene==SceneManager.GetActiveScene())Instance=this;}
        private void OnDisable(){if(Instance==this)Instance=null;Watch(null);_lost.Clear();_safe.Clear();}
        private void OnDestroy(){if(_droneRoot!=null)Destroy(_droneRoot.gameObject);}

        public float SecondsUntilReturn(Slipper slipper)=>_lost.TryGetValue(slipper,out float end)?Mathf.Max(0,end-Time.time):0;

        private void Watch(ArenaStage stage)
        {
            if(_stage==stage)return;
            if(_stage!=null)_stage.LayoutApplied-=OnLayoutApplied;
            _stage=stage;
            if(_stage!=null)_stage.LayoutApplied+=OnLayoutApplied;
        }

        // A safe spot belongs to the layout it was stood on. The next layout may have nothing
        // there, or a wall. Lost slippers keep their deadline: they return to their owner.
        private void OnLayoutApplied(int layout)=>_safe.Clear();

        private void SyncRound()
        {
            int round=GameServices.Match!=null?GameServices.Match.RoundNumber:0;
            if(round==_round)return;
            // SliceRunner.ResetWorld reactivates and equips the round's stock, as on Sa Bubong:
            // a loss deadline must never recall a newly assigned shoe into another round.
            _round=round;_lost.Clear();_safe.Clear();
        }

        /// <summary>The highest floor under a point, ignoring bodies, slippers and the can.</summary>
        private static bool Floor(Vector3 at,float above,float depth,out float y)
        {
            y=0;bool found=false;
            int count=Physics.RaycastNonAlloc(at+Vector3.up*above,Vector3.down,FloorHits,above+depth,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=FloorHits[i].collider;
                if(hit==null||hit.GetComponentInParent<CharacterMotor>()!=null||hit.GetComponentInParent<Slipper>()!=null||hit.GetComponentInParent<Lata>()!=null)continue;
                if(found&&FloorHits[i].point.y<=y)continue;
                y=FloorHits[i].point.y;found=true;
            }
            return found;
        }

        // Stood on the stage with a metre of floor on every side: the inset from the edge that
        // the design asks for is taken here, when the spot is remembered, not at the landing.
        private void RememberSafe(CharacterMotor who)
        {
            var p=who.transform.position;
            if(!who.IsGrounded||p.y<=ArenaStage.CatchY||!Floor(p,.4f,.7f,out float y))return;
            foreach(var side in Around)
                if(!Floor(p+side*SafeMargin,.6f,1.2f,out float beside)||Mathf.Abs(beside-y)>.6f)return;
            _safe[who]=new Vector3(p.x,y,p.z);
        }

        private Vector3 ChooseLanding(CharacterMotor who)
        {
            // The last safe spot, if THIS layout still has floor there; else the nearest
            // platform; the round spawn only when the stage has no answer at all.
            if(_safe.TryGetValue(who,out var safe)&&Floor(safe,1.5f,2.5f,out float y)&&y>ArenaStage.CatchY)
                return new Vector3(safe.x,y+.02f,safe.z);
            if(_stage!=null&&_stage.TryNearestStandable(who.transform.position,out var near))
                return near+Vector3.up*.02f;
            return who.SpawnPosition;
        }

        private void Lose(Slipper slipper)
        {
            if(_lost.ContainsKey(slipper))return;
            if(!slipper.HostBeginMapRecovery())return;
            _lost.Add(slipper,Time.time+SlipperDelay);
        }

        private void FixedUpdate()
        {
            if(!Live||!NetAuthority.ShouldResolve())return;
            Instance=this;Watch(ArenaStage.Instance);
            SyncRound();
            if(_slice==null)_slice=Object.FindFirstObjectByType<SliceRunner>();
            // A slipper thrown or dropped into the pit: the same delayed return as a held one.
            if(_slice!=null&&_slice.Slippers!=null)
                foreach(var slipper in _slice.Slippers)
                    if(slipper!=null&&slipper.gameObject.activeSelf&&slipper.State!=SlipperState.Held
                       &&slipper.transform.position.y<ArenaStage.CatchY)Lose(slipper);
            bool remember=Time.time>=_nextSafe;
            if(remember)_nextSafe=Time.time+SafeRefresh;
            foreach(var who in GameServices.Round.Players)
            {
                if(who==null||!who.gameObject.activeInHierarchy||who.IsEdgeRecovering)continue;
                var p=who.transform.position;
                if(p.y>=ArenaStage.CatchY){if(remember)RememberSafe(who);continue;}
                var carried=who.GetComponent<Carrier>()?.Held;if(carried!=null)Lose(carried);
                var landing=ChooseLanding(who);
                // The body faces its travel; `SyncUnit` refuses a kind without a unit Outward.
                var outward=Vector3.ProjectOnPlane(p-landing,Vector3.up);
                outward=outward.sqrMagnitude>.0004f?outward.normalized:Vector3.back;
                who.BeginEdgeRecovery(new MapEdgeAnchor(EdgeRecoveryKind.Drone,p,landing,outward));
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

        // Every peer. Presentation only, off the replicated kind.
        private void Update()
        {
            var round=GameServices.Round;if(round==null)return;
            foreach(var who in round.Players)
            {
                if(who==null||who.EdgeKind!=EdgeRecoveryKind.Drone||!who.gameObject.activeInHierarchy)continue;
                if(!_drones.TryGetValue(who,out var drone)||drone==null)
                {
                    if(_droneRoot==null)_droneRoot=new GameObject("Arena drones").transform;
                    drone=ArenaDrone.Build(_droneRoot);_drones[who]=drone;
                }
                drone.Attend(who);
            }
        }
    }
}
