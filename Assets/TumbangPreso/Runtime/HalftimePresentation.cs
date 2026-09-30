using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    // One host-authored real-time boundary for the complete between-round package.
    public sealed class HalftimePresentation : MonoBehaviour
    {
        public static HalftimePresentation Instance {get;private set;}
        public static bool Playing=>Instance!=null&&Instance.Active;
        public const float BreakDuration = 10;
        public bool Active {get;private set;}
        public bool IsHalftime {get;private set;}
        public long MatchId {get;private set;}
        public long ClipId {get;private set;}
        public int CompletedRound {get;private set;}
        public int NextTaya {get;private set;}
        public double Began {get;private set;}
        public float Duration=>BreakDuration;
        public float Remaining=>Active?Mathf.Max(0,Duration-(float)(SharedUltimatePhase.Now-Began)):0;
        public bool HasReplay=>false;
        public RenderTexture ReplayFrame=>null;
        public RenderTexture FrozenFrame=>_frame?.Texture;
        public string FallbackReason {get;private set;}
        private RoundBreakFrame _frame;
        private Scene _scene;
        public static bool IsMiddleBreak(int completed,int total)=>total>=6&&completed==total/2&&completed<total;
        public static HalftimePresentation Ensure()
        {
            if(Instance!=null)return Instance;
            return GameServices.Match!=null?GameServices.Match.gameObject.AddComponent<HalftimePresentation>():null;
        }
        private void Awake(){Instance=this;_frame=gameObject.AddComponent<RoundBreakFrame>();}
        public void BeginHost(int nextRound,int nextTaya)
        {
            if(!NetAuthority.ShouldResolve())return;
            Receive(GameServices.Match.PresentationMatchId,nextRound-1,nextTaya,SharedUltimatePhase.Now,0,
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
            if(SharedUltimatePhase.Now-began>=BreakDuration)return false;
            End(false);_frame.Freeze();SharedUltimatePhase.Instance?.Cancel();
            MatchId=match;CompletedRound=completed;NextTaya=nextTaya;Began=began;ClipId=clip;IsHalftime=halftime;
            _scene=SceneManager.GetActiveScene();Active=true;FallbackReason=null;
            PresentationClock.RequestScale(requestedScale);PresentationClock.Hold();FreshInput();
            FindAnyObjectByType<UI.RoleSwapCard>()?.ShowScheduledBreak(completed+1,nextTaya,Remaining,null);
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
        }
        public void End(bool advance)
        {
            bool wasActive=Active;Active=false;IsHalftime=false;
            _frame?.Release();
            if(wasActive){PresentationClock.Release();FreshInput();}
            if(advance&&wasActive&&NetAuthority.ShouldResolve()&&GameServices.Match?.IsWarmupBuffer==true)GameServices.Match.AdvanceRound();
        }
        private static void FreshInput(){if(GameServices.Round!=null)foreach(var actor in GameServices.Round.Players)actor?.Intent.RequireFreshActions();}
        private void OnDisable()=>End(false);
        private void OnDestroy(){End(false);if(Instance==this)Instance=null;}
    }
}
