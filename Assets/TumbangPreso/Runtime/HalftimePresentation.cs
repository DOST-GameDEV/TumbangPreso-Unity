using System;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    // One host-authored real-time boundary for the complete between-round package.
    public sealed class HalftimePresentation : MonoBehaviour
    {
        public static HalftimePresentation Instance {get;private set;}
        public static bool Playing=>Instance!=null&&Instance.Active;
        public const float BreakDuration = 3.5f;
        // Owner, October 22: "kahit tagalan mo ren halftime replay". Two more seconds for the slower replay.
        public const float HalftimeDuration = 12;
        // Arena transforms its stage during the ordinary break, so that break is longer there and
        // the live picture is kept. Read from the map on every peer, so it stays shared with nothing sent.
        public static bool LiveStageBreak=>Map.ArenaStage.Instance!=null;
        public static float OrdinaryBreakDuration=>LiveStageBreak?Map.ArenaStage.BreakSeconds:BreakDuration;
        // ARENA'S HALFTIME IS TWO ACTS ON THE ONE CLOCK: this package untouched (replay, then standings),
        // THEN the stage's transformation, the same 8 s as its ordinary break. So halftime is longer there
        // only, by the map, on every peer; nothing is sent. Every part of that show reads its start here.
        public static float StageShowLead(bool halftime)=>halftime&&LiveStageBreak?Map.ArenaStage.HalftimeLeadSeconds:0;
        public static float DurationFor(bool halftime)=>halftime?(LiveStageBreak?Map.ArenaStage.HalftimeLeadSeconds+Map.ArenaStage.BreakSeconds:HalftimeDuration):OrdinaryBreakDuration;
        public bool Active {get;private set;}
        public bool IsHalftime {get;private set;}
        public long MatchId {get;private set;}
        public long ClipId {get;private set;}
        public int CompletedRound {get;private set;}
        public int NextTaya {get;private set;}
        public double Began {get;private set;}
        public float Duration=>DurationFor(IsHalftime);
        public double StageShowBegan=>Began+StageShowLead(IsHalftime);
        public float StageShowAge=>(float)(SharedUltimatePhase.Now-StageShowBegan);
        public float StageShowDuration=>Duration-StageShowLead(IsHalftime);
        // False through Arena's halftime package, and always on every other map.
        public bool StageShowPlaying=>Active&&LiveStageBreak&&(!IsHalftime||SharedUltimatePhase.Now-Began>=Map.ArenaStage.HalftimeLeadSeconds);
        public float Remaining=>Active?Mathf.Max(0,Duration-(float)(SharedUltimatePhase.Now-Began)):0;
        public bool HasReplay=>_view?.Ready==true;
        public RenderTexture ReplayFrame=>_view?.Target;
        public RenderTexture FrozenFrame=>_frame?.Texture;
        public string FallbackReason {get;private set;}
        private RoundBreakFrame _frame;
        private RecordedWorldView _view;
        private RecordedMatchClip _clip;
        private bool _attempted,_standings,_stageShow;
        // The setup in real time, the 0.8 s around the contact at 0.3x, then the consequence in real time.
        private const float SlowLead=.35f,SlowSpan=.8f,SlowRate=.3f;
        private const float ReplaySlowdown=SlowSpan/SlowRate-SlowSpan,ReplayEndAge=7.6f;
        public static float ReplayOffset(float elapsed,float contactFromStart)
        {
            float before=Mathf.Max(0,contactFromStart-SlowLead),slowFor=SlowSpan/SlowRate;
            if(elapsed<=before)return elapsed;
            if(elapsed<=before+slowFor)return before+(elapsed-before)*SlowRate;
            return before+SlowSpan+(elapsed-before-slowFor);
        }
        private float _replayBegan;
        private Scene _scene;
        // The wipe opens the package and hides the replay's build, leaves on its first drawn frame, and crosses again to the standings.
        private UI.ReplayStinger _cover;
        private bool _exitSwept;
        // PAYO FORCES A HALF TIME (owner, 2026-10-12, of the map that turns to night at half time: "force it to have").
        // Four-round matches also play their middle break (owner, 2026-10-21). On a map that sets this lower it has one
        // at its middle whatever its length (2 rounds: after round 1). It is set by the MAP when its scene loads
        // (`Map.PayoNight`), on every peer alike, and put back to the default when the scene goes: nothing is sent, the host's
        // stamp and every peer's check of it (`Receive`) read the same number because they stand in the same scene.
        public const int DefaultMiddleBreakMinRounds=4;
        public static int MiddleBreakMinRounds=DefaultMiddleBreakMinRounds;
        public static bool IsMiddleBreak(int completed,int total)=>total>=MiddleBreakMinRounds&&completed==total/2&&completed>=1&&completed<total;
        public static HalftimePresentation Ensure()
        {
            if(Instance!=null)return Instance;
            return GameServices.Match!=null?GameServices.Match.gameObject.AddComponent<HalftimePresentation>():null;
        }
        private void Awake(){Instance=this;_frame=gameObject.AddComponent<RoundBreakFrame>();}
        public void BeginHost(int nextRound,int nextTaya)
        {
            if(!NetAuthority.ShouldResolve())return;
            var archive=FindAnyObjectByType<MatchReplayArchive>();
            long clip=0;
            if(IsMiddleBreak(nextRound-1,GameServices.Match.TotalRounds)&&archive!=null&&archive.Clips.Count>0)
            {
                clip=archive.Clips[0].Clip.Id;
                // Prefer a complete story already staged for the whole audience.
                // A late join still shares the same end through the fallback.
                if(NetAuthority.IsNetworked&&Net.MatchRpc.Instance!=null)
                    foreach(var candidate in archive.Clips)if(Net.MatchRpc.Instance.ReplayReadyForAudience(candidate.Clip.Id)){clip=candidate.Clip.Id;break;}
            }
            Receive(GameServices.Match.PresentationMatchId,nextRound-1,nextTaya,SharedUltimatePhase.Now,clip,
                IsMiddleBreak(nextRound-1,GameServices.Match.TotalRounds),PresentationClock.RequestedScale);
            Net.MatchRpc.Instance?.BroadcastBreak();
        }
        public bool Receive(long match,int completed,int nextTaya,double began,long clip,bool halftime,float requestedScale)
        {
            if(GameServices.Match==null||match!=GameServices.Match.PresentationMatchId||completed!=GameServices.Match.RoundNumber
                ||completed<1||completed>=GameServices.Match.TotalRounds||nextTaya!=Core.MatchRules.DefenderSlotFor(completed+1)
                ||double.IsNaN(began)||double.IsInfinity(began)||began>SharedUltimatePhase.Now+1||clip<0
                ||halftime!=IsMiddleBreak(completed,GameServices.Match.TotalRounds)||!float.IsFinite(requestedScale)||requestedScale<0||requestedScale>4)return false;
            if(MatchId==match&&CompletedRound==completed)return false;
            if(SharedUltimatePhase.Now-began>=DurationFor(halftime))return false;
            End(false);
            if(halftime)FindAnyObjectByType<UI.RoleSwapCard>()?.DismissAndPractice();
            _frame.Freeze();
            // Freeze also parks the UI input modules, which this break still needs; only its still image is dropped.
            if(LiveStageBreak&&(!halftime||SharedUltimatePhase.Now-began>=Map.ArenaStage.HalftimeLeadSeconds))_frame.SetImageVisible(false);
            SharedUltimatePhase.Instance?.Cancel();
            MatchId=match;CompletedRound=completed;NextTaya=nextTaya;Began=began;ClipId=clip;IsHalftime=halftime;
            _scene=SceneManager.GetActiveScene();Active=true;_attempted=false;_standings=false;_stageShow=false;_replayBegan=.35f;FallbackReason=null;
            PresentationClock.RequestScale(requestedScale);PresentationClock.Hold();FreshInput();
            if(!halftime)FindAnyObjectByType<UI.RoleSwapCard>()?.ShowScheduledBreak(completed+1,nextTaya,Remaining,null);
            _exitSwept=false;
            if(halftime&&clip>0&&SharedUltimatePhase.Now-began<1)_cover=UI.ReplayStinger.Cover(transform,UI.HudDraw.Honey);
            return true;
        }
        private void Update()
        {
            if(!Active)return;
            var match=GameServices.Match;
            if(match==null||!match.MatchInProgress||match.PresentationMatchId!=MatchId||match.RoundNumber!=CompletedRound||SceneManager.GetActiveScene()!=_scene)
            {End(false);return;}
            float age=(float)(SharedUltimatePhase.Now-Began);
            if(age>=Duration){End(true);return;}
            if(!IsHalftime)return;
            // A late peer still waiting for the clip's bytes is not left behind a held wipe.
            if(_cover!=null&&(_attempted||age>=1.5f)){_cover.Release();_cover=null;}
            if(StageShowPlaying)
            {
                // The package is over: its replay, its still frame and the centred card give way to the live stage.
                if(_stageShow)return;
                _stageShow=true;_clip=null;_view?.Dispose();_view=null;_frame.SetImageVisible(false);
                if(!_standings){_standings=true;FindAnyObjectByType<UI.RoleSwapCard>()?.ShowScheduledBreak(CompletedRound+1,NextTaya,Remaining,FallbackReason);}
                return;
            }
            if(!_attempted&&age>=.2f)
            {
                var archive=FindAnyObjectByType<MatchReplayArchive>();
                if(archive!=null)foreach(var retained in archive.Clips)if(retained.Clip.Id==ClipId&&retained.Clip.MatchId==MatchId)_clip=retained.Clip;
                if(_clip==null)_clip=Net.MatchRpc.Instance?.ReceivedReplay(ClipId);
                // The host timestamp can already be old when a peer receives the
                // break. Keep looking for its nominated bytes while footage can
                // still fit; receipt latency is not evidence of an invalid clip.
                bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
                if(_clip==null&&ClipId>0&&!reduced&&age<ReplayEndAge-ReplaySlowdown)return;
                _attempted=true;
                if(_clip!=null&&_clip.Duration<=4.5f&&age+_clip.Duration+ReplaySlowdown<=ReplayEndAge&&!reduced)
                {
                    try
                    {
                        _view=new RecordedWorldView(transform,_clip);
                        _replayBegan=Mathf.Max(.35f,(float)(SharedUltimatePhase.Now-Began));
                        if(_replayBegan+_clip.Duration+ReplaySlowdown>ReplayEndAge){_view.Dispose();_view=null;}
                    }
                    catch(Exception failure){Debug.LogWarning("[Replay] View unavailable: "+failure.Message);}
                }
                if(_view?.Ready!=true)
                { Debug.Log("[Replay] clip="+ClipId+" unavailable: "+(_clip==null?"canonical bytes missing":_view?.UnavailableReason??"view preference or scene"));_view?.Dispose();_view=null;FallbackReason=ClipId==0?"No complete highlight this half":"Replay unavailable on this screen"; }
            }
            if(_view?.Ready==true&&!_exitSwept&&age>=ReplayEndAge-UI.ReplayStinger.InSeconds)
            {_exitSwept=true;UI.ReplayStinger.Sweep(transform,UI.HudDraw.Honey,true);}
            if(_view?.Ready==true&&age<ReplayEndAge)
            {
                // Real-time setup, a long contact slowdown, then full consequence.
                float elapsed=Mathf.Max(0,age-_replayBegan);
                float offset=Mathf.Min(ReplayOffset(elapsed,_clip.Contact-_clip.Start),_clip.Duration);
                try{_view.Draw(_clip.Start+offset);}
                catch(Exception failure){Debug.LogWarning("[Replay] Recorded view failed: "+failure.Message);_view.Dispose();_view=null;FallbackReason="Replay unavailable on this screen";}
            }
            _frame.SetImageVisible(_view?.Ready!=true||age>=ReplayEndAge);
            if(!_standings&&_attempted&&(_view==null||age>=ReplayEndAge))
            {
                _standings=true;_view?.Dispose();_view=null;
                FindAnyObjectByType<UI.RoleSwapCard>()?.ShowScheduledBreak(CompletedRound+1,NextTaya,Remaining,FallbackReason);
            }
        }
        public void End(bool advance)
        {
            bool wasActive=Active;Active=false;IsHalftime=false;
            _clip=null;_view?.Dispose();_view=null;
            if(_cover!=null){_cover.Release();_cover=null;}
            _frame?.Release();
            if(wasActive){PresentationClock.Release();FreshInput();}
            if(advance&&wasActive&&NetAuthority.ShouldResolve()&&GameServices.Match?.IsWarmupBuffer==true)GameServices.Match.AdvanceRound();
        }
        private static void FreshInput(){if(GameServices.Round!=null)foreach(var actor in GameServices.Round.Players)actor?.Intent.RequireFreshActions();}
        private void OnDisable()=>End(false);
        private void OnDestroy(){End(false);if(Instance==this)Instance=null;}
    }
}
