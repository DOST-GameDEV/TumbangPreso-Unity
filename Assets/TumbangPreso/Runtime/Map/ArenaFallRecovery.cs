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
    /// ⚠️ THE CATCH IS AT `ArenaStage.CatchY` (the data's `catchY`, about y -22: owner,
    /// 2026-10-05, "falling off threshold is too high, you need to fall further"), ABOVE TWO
    /// OTHER FLOORS. On this map `MatchRpc.AcceptMove` believes a pose down to
    /// `ArenaStage.MoveFloorY` (about -40; -5 on every other map), so a remote body is seen all
    /// the way down to the catch with 18 m to spare, and the `KillPlane` (about -60 here)
    /// returns a body to its round spawn, which is the one place the owner's rule says a fall
    /// must not send it. That plane stays as the last resort.
    ///
    /// ⚠️ A FALLING BODY CROSSES HALF A METRE BETWEEN TWO POSES (terminal speed is 25 m/s), and
    /// the host sees a remote body only where its last accepted pose put it. So a body in the
    /// air under the lowest deck is also caught when its own speed carries it past the line
    /// within `PoseLead` (two physics steps): every peer then sees the catch at the same depth,
    /// whoever simulates the body.
    ///
    /// ⚠️ A SLIPPER IS TAKEN AT `ArenaStage.SlipperCatchY`, NOT AT THE BODY'S LINE. The slipper's
    /// own flight returns it to its mark under `Balance.VoidY` (-12), at once and with no
    /// delay; taken just under the decks, it gets this map's delayed return first.
    ///
    /// ⚠️ A SLIPPER THAT LEAVES THE STAGE COMES STRAIGHT BACK TO ITS NEAREST EDGE. The owner,
    /// 2026-10-05: "issue with the bots is that if their slipper goes off the platform they cant
    /// retrieve it", and, of the balloon, "the slipper will just spawn/tp back to the nearest edge
    /// to be able to retrieve it". WHAT WAS HAPPENING: a flight ends where `Slipper.FindGroundY`
    /// says the ground is, and over the shaft that cast finds nothing and answers 0, which is this
    /// stage's deck height. So a slipper thrown or knocked past an edge, or into a gap between two
    /// pieces, did not fall: it "landed" in the air at deck height, loose and out of reach, up to
    /// 9 m out in the corners of the square walls. Nothing ever took it (the catch below is 4.5 m
    /// down) and a bot walked to the lip and stood there for the rest of the round. NOW the host
    /// takes any slipper with nothing of the stage under it (resting, or flying down through deck
    /// height) and sets it down `EdgeReturnSeconds` later at `ArenaStage.TryNearestStandable` of
    /// where it left: inset a metre from every edge, on a disc, ring or arc of the CURRENT
    /// layout, never a ramp, never a bonus piece. It is out of play meanwhile (switched off, as
    /// on any map recovery), so a bot sees no slipper, waits on its throwing ring, and fetches it
    /// when it is back on floor it can walk to. `ArenaBalloon` uses the same return. A slipper
    /// HELD by a body that fell keeps the fall's own rule: the eight seconds, to its owner.
    /// A slipper resting on a piece too high for its owner is `Slipper`'s own existing rule
    /// (more than 1.2 m over the owner's feet goes to the owner), so a loft never strands one.
    ///
    /// ⚠️ ONLY THE HOST DECIDES (`NetAuthority.ShouldResolve()`, round or free roam alike, as
    /// `RooftopRecovery` does). Nothing here is sent: the carry is the `Drone` edge recovery
    /// kind, whose kind, phase and pose already ride `SyncUnit`. What every peer does run is
    /// `Update`, which only puts the grey-box drone over a body whose replicated kind is Drone.
    /// </summary>
    public sealed class ArenaFallRecovery : MonoBehaviour
    {
        public const float SlipperDelay=8,SafeMargin=1,SafeRefresh=.2f,PoseLead=.04f;
        /// <summary>From leaving the stage to being set back on it; how often resting slippers are
        /// looked at; how far over the deck a falling slipper is taken (its flight would end in
        /// the air a step or two later); and how far down "nothing under it" looks.</summary>
        public const float EdgeReturnSeconds=1.6f,LooseCheck=.2f,FlightSkim=.25f,VoidDepth=60;
        public static ArenaFallRecovery Instance { get; private set; }
        private static readonly RaycastHit[] FloorHits=new RaycastHit[16];
        private static readonly Vector3[] Around={Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
        private readonly Dictionary<Slipper,float> _lost=new Dictionary<Slipper,float>();
        private readonly List<Slipper> _finished=new List<Slipper>();
        private struct Away{public float At;public Vector3 From;}
        private readonly Dictionary<Slipper,Away> _away=new Dictionary<Slipper,Away>();
        private Slipper[] _seen=System.Array.Empty<Slipper>();
        private bool[] _wasActive=System.Array.Empty<bool>();
        private float _nextLoose,_nextScan;
        private readonly Dictionary<CharacterMotor,Vector3> _safe=new Dictionary<CharacterMotor,Vector3>();
        private readonly Dictionary<CharacterMotor,ArenaDrone> _drones=new Dictionary<CharacterMotor,ArenaDrone>();
        /// <summary>The stage kit's drone, left inactive in the scene by `ArenaSceneBuilder` and
        /// copied for each carry. Null: `ArenaDrone` draws its grey-box.</summary>
        public GameObject DroneTemplate;
        private Transform _droneRoot;
        private ArenaStage _stage;
        private SliceRunner _slice;
        private int _round=-1;
        private float _nextSafe;
        private bool Live=>gameObject.scene==SceneManager.GetActiveScene()&&ArenaStage.Instance!=null&&GameServices.Round!=null;

        private void OnEnable(){if(gameObject.scene==SceneManager.GetActiveScene())Instance=this;}
        private void OnDisable(){if(Instance==this)Instance=null;Watch(null);_lost.Clear();_away.Clear();_safe.Clear();}
        private void OnDestroy(){if(_droneRoot!=null)Destroy(_droneRoot.gameObject);}

        public float SecondsUntilReturn(Slipper slipper)=>_lost.TryGetValue(slipper,out float end)?Mathf.Max(0,end-Time.time)
            :_away.TryGetValue(slipper,out var away)?Mathf.Max(0,away.At-Time.time):0;

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
            _round=round;_lost.Clear();_away.Clear();_safe.Clear();
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
            if(!who.IsGrounded||p.y<=ArenaStage.SlipperCatchY||!Floor(p,.4f,.7f,out float y))return;
            foreach(var side in Around)
                if(!Floor(p+side*SafeMargin,.6f,1.2f,out float beside)||Mathf.Abs(beside-y)>.6f)return;
            _safe[who]=new Vector3(p.x,y,p.z);
        }

        private Vector3 ChooseLanding(CharacterMotor who)
        {
            // The last safe spot, if THIS layout still has floor there; else the nearest
            // platform; the round spawn only when the stage has no answer at all.
            if(_safe.TryGetValue(who,out var safe)&&Floor(safe,1.5f,2.5f,out float y)&&y>ArenaStage.SlipperCatchY)
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

        /// <summary>
        /// HOST: takes a slipper out of play and books it back onto the stage, loose, `seconds`
        /// from now at the nearest standable point to `leftAt`. False if it is not this peer's
        /// to decide or the slipper is already away.
        /// </summary>
        public bool HostSendAway(Slipper slipper,Vector3 leftAt,float seconds)
        {
            if(!NetAuthority.ShouldResolve()||slipper==null||_away.ContainsKey(slipper)||_lost.ContainsKey(slipper))return false;
            if(!slipper.HostBeginMapRecovery())return false;
            _away.Add(slipper,new Away{At=Time.time+Mathf.Max(0,seconds),From=leftAt});
            return true;
        }

        /// <summary>True when nothing of the stage is under the point: it is over the shaft or a gap.</summary>
        public static bool OverTheVoid(Vector3 p)=>!Floor(p,.4f,VoidDepth,out _);

        private void Return(Slipper shoe,Vector3 from)
        {
            // No stage, or a layout with nothing to stand on: the slipper's own return, to its owner.
            if(_stage==null||!_stage.TryNearestStandable(from,out var near)){shoe.HostFinishMapRecovery();return;}
            if(Floor(near,1.5f,2.5f,out float y))near.y=y;
            shoe.HostFinishMapRecoveryAt(near+Vector3.up*Core.Balance.SlipperRestHeight);
        }

        private static bool Fallen(CharacterMotor who,Vector3 p)
        {
            float line=ArenaStage.CatchY;
            if(p.y<line)return true;
            return !who.IsGrounded&&p.y<ArenaStage.Instance.LowestUnderside&&p.y+Mathf.Min(0,who.Velocity.y)*PoseLead<line;
        }

        private void FixedUpdate()
        {
            if(!Live||!NetAuthority.ShouldResolve())return;
            Instance=this;Watch(ArenaStage.Instance);
            SyncRound();
            if(_slice==null)_slice=Object.FindFirstObjectByType<SliceRunner>();
            // A slipper that has left the stage (see the class note): resting on nothing, flying
            // down through deck height over nothing, or under the decks. Straight back to the edge.
            bool sweep=Time.time>=_nextLoose;
            if(sweep)_nextLoose=Time.time+LooseCheck;
            if(_slice!=null&&_slice.Slippers!=null)
                foreach(var slipper in _slice.Slippers)
                {
                    if(slipper==null||!slipper.gameObject.activeSelf||slipper.State==SlipperState.Held)continue;
                    var at=slipper.transform.position;
                    bool gone=at.y<ArenaStage.SlipperCatchY;
                    if(!gone&&slipper.State==SlipperState.Loose)gone=sweep&&OverTheVoid(at);
                    else if(!gone)gone=slipper.Velocity.y<0&&at.y<ArenaStage.Instance.transform.position.y+FlightSkim&&OverTheVoid(at);
                    if(gone)HostSendAway(slipper,at,EdgeReturnSeconds);
                }
            bool remember=Time.time>=_nextSafe;
            if(remember)_nextSafe=Time.time+SafeRefresh;
            foreach(var who in GameServices.Round.Players)
            {
                if(who==null||!who.gameObject.activeInHierarchy||who.IsEdgeRecovering)continue;
                var p=who.transform.position;
                if(!Fallen(who,p)){if(remember)RememberSafe(who);continue;}
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
            _finished.Clear();
            foreach(var entry in _away)
            {
                var shoe=entry.Key;
                // Back in play already (a round began and re-equipped it): nothing left to do.
                if(shoe==null||shoe.gameObject.activeSelf){_finished.Add(shoe);continue;}
                if(Time.time<entry.Value.At)continue;
                Return(shoe,entry.Value.From);_finished.Add(shoe);
            }
            foreach(var shoe in _finished)_away.Remove(shoe);
        }

        // Every peer, off each slipper's replicated state: a puff where one left the stage, and a
        // ring (from the sky, if the balloon sent it back) where one is set down again.
        private void WatchSlippers()
        {
            if(Time.unscaledTime>=_nextScan)
            {
                _nextScan=Time.unscaledTime+2;
                var found=FindObjectsByType<Slipper>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                bool same=found.Length==_seen.Length;
                for(int i=0;same&&i<found.Length;i++)same=System.Array.IndexOf(_seen,found[i])>=0;
                if(!same)
                {
                    _seen=found;_wasActive=new bool[found.Length];
                    for(int i=0;i<found.Length;i++)_wasActive[i]=found[i]!=null&&found[i].gameObject.activeSelf;
                }
            }
            var fx=ArenaFx.Instance;
            for(int i=0;i<_seen.Length;i++)
            {
                var shoe=_seen[i];if(shoe==null)continue;
                bool active=shoe.gameObject.activeSelf;
                if(active==_wasActive[i])continue;
                _wasActive[i]=active;
                var p=shoe.transform.position;
                if(fx==null||GameServices.Round==null||!GameServices.Round.RoundActive)continue;
                if(!active)
                {
                    if(!OverTheVoid(p))continue;
                    fx.Ring(p,.2f,1.4f,ArenaFx.White,.7f,.35f,ArenaFx.Cell.ThinRing);
                    fx.Dots(p,Vector3.up,70,8,1,3.5f,ArenaFx.White,.8f,.3f,.6f,.22f,-1);
                }
                else if(shoe.State==SlipperState.Loose)
                {
                    bool sky=ArenaBalloon.ReturningFromSky(shoe.SeatOfOrigin);
                    fx.Ring(p,.2f,sky?2.6f:1.7f,ArenaFx.Gold,.9f,.5f,ArenaFx.Cell.ThinRing);
                    fx.Ring(p,.1f,sky?1.6f:1.0f,ArenaFx.White,.8f,.35f);
                    fx.Pillar(p,sky?36:3.2f,sky?.9f:.5f,sky?ArenaFx.White:ArenaFx.Gold,.85f,sky?.4f:.45f);
                    if(sky)fx.Sparks(p+Vector3.up*.1f,Vector3.up,65,14,2,7,ArenaFx.Gold,.95f,.3f,.7f,.12f,9);
                    fx.Glint(p+Vector3.up*.5f,1.2f,ArenaFx.White,.9f,.4f);
                    ArenaFx.Cue("sfx_arena_slipper_return",p,.96f,1.06f);
                }
            }
        }

        // Every peer. Presentation only, off the replicated kind and the body's own motion.
        private void Update()
        {
            var round=GameServices.Round;if(round==null)return;
            if(ArenaStage.Instance!=null)WatchSlippers();
            var players=round.Players;
            for(int i=0;i<players.Count;i++)
            {
                var who=players[i];
                if(who==null||!who.gameObject.activeInHierarchy)continue;
                // Dropping through the shaft: the body has lost itself to the fall (the same
                // tumble a car's throw gives), re-armed while it lasts and let go at the catch.
                if(ArenaStage.IsShaftFall(who))Visual.WindTumble.Attach(who)?.Throw(.2f);
                if(who.EdgeKind!=EdgeRecoveryKind.Drone)continue;
                if(!_drones.TryGetValue(who,out var drone)||drone==null)
                {
                    if(_droneRoot==null)_droneRoot=new GameObject("Arena drones").transform;
                    drone=ArenaDrone.Build(_droneRoot,DroneTemplate);_drones[who]=drone;
                }
                drone.Attend(who);
            }
        }
    }
}
