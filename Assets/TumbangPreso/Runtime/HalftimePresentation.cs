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
        public static bool Playing=>Instance!=null&&Instance.Active&&Instance.IsHalftime;
        public bool Active {get;private set;}
        public bool IsHalftime {get;private set;}
        public long MatchId {get;private set;}
        public long ClipId {get;private set;}
        public int CompletedRound {get;private set;}
        public int NextTaya {get;private set;}
        public double Began {get;private set;}
        public float Duration=>IsHalftime?10:3;
        public float Remaining=>Active?Mathf.Max(0,Duration-(float)(SharedUltimatePhase.Now-Began)):0;
        public bool HasReplay=>_view?.Ready==true;
        public string FallbackReason {get;private set;}
        private RecordedWorldView _view;
        private RecordedMatchClip _clip;
        private bool _attempted,_standings;
        private Scene _scene;
        public static bool IsMiddleBreak(int completed,int total)=>total>=6&&completed==total/2&&completed<total;
        public static HalftimePresentation Ensure()
        {
            if(Instance!=null)return Instance;
            return GameServices.Match!=null?GameServices.Match.gameObject.AddComponent<HalftimePresentation>():null;
        }
        private void Awake()=>Instance=this;
        public void BeginHost(int nextRound,int nextTaya)
        {
            if(!NetAuthority.ShouldResolve())return;
            var archive=FindAnyObjectByType<MatchReplayArchive>();
            long clip=IsMiddleBreak(nextRound-1,GameServices.Match.TotalRounds)&&archive!=null&&archive.Clips.Count>0?archive.Clips[0].Clip.Id:0;
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
            if(Active&&MatchId==match&&CompletedRound==completed)return false;
            if(SharedUltimatePhase.Now-began>=(halftime?10:3))return false;
            End(false);MatchId=match;CompletedRound=completed;NextTaya=nextTaya;Began=began;ClipId=clip;IsHalftime=halftime;
            _scene=SceneManager.GetActiveScene();Active=true;_attempted=false;_standings=false;FallbackReason=null;
            if(halftime){PresentationClock.RequestScale(requestedScale);PresentationClock.Hold();FreshInput();}
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
            if(!_attempted&&age>=.2f)
            {
                _attempted=true;
                var archive=FindAnyObjectByType<MatchReplayArchive>();
                if(archive!=null)foreach(var retained in archive.Clips)if(retained.Clip.Id==ClipId&&retained.Clip.MatchId==MatchId)_clip=retained.Clip;
                if(_clip==null)_clip=Net.MatchRpc.Instance?.ReceivedReplay(ClipId);
                if(_clip!=null&&_clip.Duration<=4.5f&&!Settings.SettingsStore.Current.ReducedUiMotion)
                {
                    try{_view=new RecordedWorldView(transform,_clip);}
                    catch(Exception failure){Debug.LogWarning("[Replay] View unavailable: "+failure.Message);}
                }
                if(_view?.Ready!=true)
                { Debug.Log("[Replay] clip="+ClipId+" unavailable: "+(_clip==null?"canonical bytes missing":_view?.UnavailableReason??"view preference or scene"));_view?.Dispose();_view=null;FallbackReason=ClipId==0?"No complete highlight this half":"Replay unavailable on this screen"; }
            }
            if(_view?.Ready==true&&age<5.8f)
            {
                // Real-time setup, a brief contact slowdown, then full consequence.
                float elapsed=Mathf.Max(0,age-.35f),before=_clip.Contact-_clip.Start-.18f;
                float offset=elapsed<=before?elapsed:elapsed<=before+.86f?before+(elapsed-before)*.5f:elapsed-.43f;
                _view.Draw(_clip.Start+offset);
            }
            if(!_standings&&_attempted&&(_view==null||age>=5.8f))
            {
                _standings=true;_view?.Dispose();_view=null;
                FindAnyObjectByType<UI.RoleSwapCard>()?.ShowScheduledBreak(CompletedRound+1,NextTaya,Remaining,FallbackReason);
            }
        }
        public void End(bool advance)
        {
            bool wasActive=Active,held=IsHalftime;Active=false;IsHalftime=false;_clip=null;_view?.Dispose();_view=null;
            if(wasActive&&held){PresentationClock.Release();FreshInput();}
            if(advance&&wasActive&&NetAuthority.ShouldResolve()&&GameServices.Match?.IsWarmupBuffer==true)GameServices.Match.AdvanceRound();
        }
        private static void FreshInput(){if(GameServices.Round!=null)foreach(var actor in GameServices.Round.Players)actor?.Intent.RequireFreshActions();}
        private void OnDisable()=>End(false);
        private void OnDestroy(){End(false);if(Instance==this)Instance=null;}
    }
}
