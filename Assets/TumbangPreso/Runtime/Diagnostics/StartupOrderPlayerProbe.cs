using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.Diagnostics
{
    /// <summary>Opt-in unattended package acceptance; never installed during normal play.</summary>
    public sealed class StartupOrderPlayerProbe : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public bool passed,studioSeen;
            public string error;
            public float mainVisibleSeconds;
            public long firstFrame,lastFrame;
            public double firstVideoTime,lastVideoTime;
            public List<string> stages=new List<string>();
        }
        private readonly Receipt _receipt=new Receipt();
        private string _folder;
        private int _phase;
        private float _started,_phaseAt,_mainAt;
        private bool _finished;
        private HubSceneVideo _video;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs(); int at=Array.IndexOf(args,"-tp-startup-order-probe");
            if(Application.isEditor || at<0 || at+1>=args.Length) return;
            var root=new GameObject("~StartupOrderPlayerProbe"); DontDestroyOnLoad(root);
            var probe=root.AddComponent<StartupOrderPlayerProbe>(); probe._folder=Path.GetFullPath(args[at+1]);
            Directory.CreateDirectory(probe._folder); probe._started=Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if(_finished) return;
            try { Step(); }
            catch(Exception error) { Finish(false,error.ToString()); }
        }
        private static Canvas CanvasNamed(string name)=>UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).FirstOrDefault(c=>c.name==name);
        private void Stage(string name) { _receipt.stages.Add(name+" at "+(Time.realtimeSinceStartup-_started).ToString("0.000")+"s"); Debug.Log("[StartupOrderPlayer] "+name); }
        private static void Require(bool value,string error) { if(!value) throw new InvalidOperationException(error); }
        private void Step()
        {
            Require(Time.realtimeSinceStartup-_started<120,"Startup acceptance exceeded its stopping condition");
            Require(GameObject.Find("OwnerLoadingCanvas")==null && GameObject.Find("SplashCanvas")==null,"Retired illustrated loading appeared");
            _receipt.studioSeen|=GameObject.Find("StudioIntroCanvas")!=null;
            var login=UnityEngine.Object.FindFirstObjectByType<SignInScreen>();
            var title=CanvasNamed("OwnerHomeCanvas");
            if(_phase==3 && SceneManager.GetActiveScene().name==SceneFlow.MainMenu)
                Require(title!=null && title.gameObject.activeInHierarchy,"Main/title loading stopped being visible before lobby arrival");
            if(_phase==0 && login!=null && login.IsOpen)
            {
                Require(_receipt.studioSeen,"BH studio stage was not observed before login");
                Require(title==null || !title.gameObject.activeInHierarchy,"Main/title preceded login");
                Stage("login-first"); _phase=1; _phaseAt=Time.realtimeSinceStartup;
            }
            else if(_phase==1 && Time.realtimeSinceStartup-_phaseAt>.5f)
            {
                var canvas=CanvasNamed("OwnerSignInCanvas");
                var guest=canvas.GetComponentsInChildren<Button>().Single(b=>b.name=="GuestAccount");
                var rect=(RectTransform)guest.transform; Canvas.ForceUpdateCanvases();
                var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                    position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
                var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
                Require(hits.Count>0,"Guest had no real UI raycast");
                var receiver=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                Require(receiver==guest.gameObject,"Guest was covered");
                ExecuteEvents.Execute(receiver,data,ExecuteEvents.pointerClickHandler); _phase=2;
            }
            else if(_phase==2 && login!=null && !login.IsOpen)
            {
                Require(title!=null && title.gameObject.activeInHierarchy,"Main/title not revealed after admission");
                Stage("main-loading-visible"); _mainAt=Time.realtimeSinceStartup; _phase=3;
            }
            else if(_phase==3 && SceneManager.GetActiveScene().name==SceneFlow.MatchSetup)
            {
                _receipt.mainVisibleSeconds=Time.realtimeSinceStartup-_mainAt;
                Require(_receipt.mainVisibleSeconds>=4.95f,"Main/title did not remain visible five seconds");
                Stage("lobby-arrived"); _phase=4;
            }
            else if(_phase==4 && TumpHub.Current!=null && TumpHub.Current.ShowingHome)
            {
                _video=UnityEngine.Object.FindFirstObjectByType<HubSceneVideo>();
                if(_video==null || !_video.ShowingVideo || _video.Player==null || !_video.Player.isPlaying) return;
                _receipt.firstFrame=_video.Player.frame; _receipt.firstVideoTime=_video.Player.time;
                _phaseAt=Time.realtimeSinceStartup; Stage("first-home-video-playing"); _phase=5;
            }
            else if(_phase==5 && Time.realtimeSinceStartup-_phaseAt>=1.2f)
            {
                _receipt.lastFrame=_video.Player.frame; _receipt.lastVideoTime=_video.Player.time;
                Require(_receipt.lastFrame>_receipt.firstFrame+2 && _receipt.lastVideoTime>_receipt.firstVideoTime+.3,"First Home video froze");
                Stage("first-home-decoded-frames-advance"); Finish(true,"");
            }
        }
        private void Finish(bool passed,string error)
        {
            _finished=true; _receipt.passed=passed; _receipt.error=error;
            File.WriteAllText(Path.Combine(_folder,"result.json"),JsonUtility.ToJson(_receipt,true));
            Application.Quit(passed?0:1);
        }
    }
}
