using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.Diagnostics
{
    // Opt-in built-player UI qualification. Normal play and tournament launches never install it.
    public sealed class OwnerUiPlayerReview : MonoBehaviour
    {
        [Serializable]private sealed class FrameWindow
        {
            public string mode,gpu,cpu;public int width,height,samples,framesOver33Ms;
            public float duration,averageFps,medianMs,p95Ms,p99Ms,maxMs;
        }
        [Serializable]private sealed class Report
        { public bool passed;public string error;public List<string> stages=new List<string>();public List<FrameWindow> frameWindows=new List<FrameWindow>(); }
        private readonly Report _report=new Report();
        private string _folder;
        private float _deadline;
        private bool _finished;
        private string _frameMode;
        private float _frameStarted;
        private readonly List<float> _frameTimes=new List<float>(8192);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tp-uireview");
            if(Application.isEditor || at<0 || at+1>=args.Length || args.Contains("-tp-tournament"))return;
            var go=new GameObject("~OwnerUiPlayerReview");DontDestroyOnLoad(go);
            var probe=go.AddComponent<OwnerUiPlayerReview>();probe._folder=Path.GetFullPath(args[at+1]);
            Directory.CreateDirectory(probe._folder);probe._deadline=Time.realtimeSinceStartup+120;probe.StartCoroutine(probe.Guard(probe.Walk()));
        }
        private IEnumerator Guard(IEnumerator sequence)
        {
            var stack=new Stack<IEnumerator>();stack.Push(sequence);
            while(stack.Count>0 && !_finished)
            {
                object next=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null){Finish(false,failure.ToString());yield break;}
                if(!more){stack.Pop();continue;}
                if(next is IEnumerator child){stack.Push(child);continue;}
                yield return next;
            }
            if(!_finished)Finish(true,"");
        }
        private void Update()
        {
            if(_frameMode!=null && GameServices.Round!=null && GameServices.Round.RoundActive)
                _frameTimes.Add(Time.unscaledDeltaTime*1000f);
            if(!_finished && Time.realtimeSinceStartup>_deadline)Finish(false,"UI review timed out after "+_report.stages.LastOrDefault());
        }
        private void StartFrameWindow(string mode)
        { _frameTimes.Clear();_frameStarted=Time.realtimeSinceStartup;_frameMode=mode; }
        private void StopFrameWindow()
        {
            if(_frameMode==null)return;
            string mode=_frameMode;_frameMode=null;
            var window=new FrameWindow{mode=mode,width=Screen.width,height=Screen.height,samples=_frameTimes.Count,
                duration=Time.realtimeSinceStartup-_frameStarted,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType};
            if(_frameTimes.Count>0)
            {
                var sorted=_frameTimes.OrderBy(v=>v).ToArray();
                float Percentile(float p)=>sorted[Mathf.Clamp(Mathf.CeilToInt(p*sorted.Length)-1,0,sorted.Length-1)];
                window.medianMs=Percentile(.5f);window.p95Ms=Percentile(.95f);window.p99Ms=Percentile(.99f);window.maxMs=sorted[sorted.Length-1];
                window.averageFps=1000f/Mathf.Max(.001f,_frameTimes.Average());window.framesOver33Ms=_frameTimes.Count(v=>v>33.333f);
            }
            _report.frameWindows.Add(window);
            File.WriteAllLines(Path.Combine(_folder,mode+"-frame-times.csv"),new[]{"sample,frame_ms"}.Concat(
                _frameTimes.Select((value,index)=>FormattableString.Invariant($"{index},{value:F6}"))));
        }
        private void Stage(string label){_report.stages.Add(label);_deadline=Time.realtimeSinceStartup+100;Debug.Log("[OwnerUiReview] "+label);}
        private void Finish(bool passed,string error)
        {
            if(_finished)return;StopFrameWindow();_finished=true;_report.passed=passed;_report.error=error;
            File.WriteAllText(Path.Combine(_folder,"result.json"),JsonUtility.ToJson(_report,true));
            Debug.Log("[OwnerUiReview] "+(passed?"PASS":"FAIL "+error));Application.Quit(passed?0:1);
        }
        private IEnumerator WaitFor(Func<bool> ready,float seconds=45)
        {
            float until=Time.realtimeSinceStartup+seconds;
            while(!ready() && Time.realtimeSinceStartup<until)yield return null;
            if(!ready())throw new InvalidOperationException("Expected UI state did not arrive.");
        }
        private static Selectable Find(string name)=>UnityEngine.Object.FindObjectsByType<Selectable>().FirstOrDefault(s=>s.name==name && s.isActiveAndEnabled && s.IsInteractable());
        private static PointerEventData Pointer(Selectable control)
        {
            var rect=(RectTransform)control.transform;var canvas=control.GetComponentInParent<Canvas>();
            return new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center))};
        }
        private IEnumerator Click(string name)
        {
            yield return WaitFor(()=>Find(name)!=null);
            // A newly built control can exist before its first rendered/raycast frame.
            yield return new WaitForEndOfFrame();
            var control=Find(name);Canvas.ForceUpdateCanvases();var pointer=Pointer(control);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Selectable>()!=control)
            {
                Debug.Log("[OwnerUiHitFailure] "+name+" point="+pointer.position+" hits="+string.Join(",",hits.Select(hit=>hit.gameObject.name)));
                yield return Shot(name+"-hit-failure");
                throw new InvalidOperationException(name+" has no reachable hit target.");
            }
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.12f);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return new WaitForSecondsRealtime(.2f);
        }
        private IEnumerator Shot(string name)
        {
            float until=Time.realtimeSinceStartup+2;
            while(UnityEngine.Object.FindObjectsByType<OwnerUiMotion>(FindObjectsSortMode.None).Any(m=>m.Entering) && Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSecondsRealtime(.15f);
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image==null)throw new InvalidOperationException("Player screenshot missing.");
            File.WriteAllBytes(Path.Combine(_folder,name+".png"),image.EncodeToPNG());
            File.AppendAllText(Path.Combine(_folder,"viewports.csv"),name+","+image.width+","+image.height+","+Screen.fullScreenMode+"\n");Destroy(image);
        }
        private IEnumerator Motion(string name,bool reduced)
        {
            // The current home design changes ink/underline, never the hit target's scale.
            var folder=Path.Combine(_folder,name+"-frames");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,real_seconds,focus_amount,control_scale,label_scale\n");
            var control=Find("SettingsButton");var pointer=Pointer(control);
            var field=typeof(HomeMenuAction).GetField("_shown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if(!(control is HomeMenuAction)||field==null)throw new InvalidOperationException("Current home ink feedback missing.");
            EventSystem.current.SetSelectedGameObject(null);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.2f);
            float start=Time.realtimeSinceStartup,nextImage=0,min=1,max=0;int frame=0;
            bool hover=false,down=false,up=false;
            while(Time.realtimeSinceStartup-start<2.3f)
            {
                float age=Time.realtimeSinceStartup-start;
                if(age>.35f&&!hover){hover=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);}
                if(age>1&&!down){down=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerDownHandler);}
                if(age>1.25f&&!up){up=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerUpHandler);EventSystem.current.SetSelectedGameObject(null);ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerExitHandler);}
                yield return new WaitForEndOfFrame();
                float focus=(float)field.GetValue(control);min=Mathf.Min(min,focus);max=Mathf.Max(max,focus);
                float scale=control.transform.localScale.x,labelScale=control.GetComponentInChildren<Text>().transform.localScale.x;
                if(Mathf.Abs(scale-1)>.001f||Mathf.Abs(labelScale-1)>.001f)throw new InvalidOperationException("Home target or label unexpectedly scaled.");
                if(reduced && Mathf.Abs(focus-(hover&&!up?1:0))>.001f)throw new InvalidOperationException("Reduced home ink feedback kept interpolating.");
                csv.AppendLine(FormattableString.Invariant($"{frame},{Time.realtimeSinceStartup-start:F6},{focus:F6},{scale:F6},{labelScale:F6}"));
                if(age>=nextImage)
                {
                    var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,frame.ToString("00000")+".jpg"),image.EncodeToJPG(88));Destroy(image);nextImage=age+.10f;
                }
                frame++;
            }
            File.WriteAllText(Path.Combine(folder,"frames.csv"),csv.ToString());
            if(max-min<.9f)throw new InvalidOperationException("Home ink feedback did not visibly respond.");
            Stage(reduced?"reduced home ink verified":"normal home ink verified");
        }
        private IEnumerator ReducedTextAction()
        {
            yield return Click("StartButton");var control=Find("BackButton");var pointer=Pointer(control);
            var motion=control.GetComponentInChildren<OwnerUiMotion>();
            if(motion==null)throw new InvalidOperationException("Native text-action feedback missing.");
            bool before=Settings.SettingsStore.Current.ReducedUiMotion;
            Settings.SettingsStore.Current.ReducedUiMotion=false;
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.3f);
            Settings.SettingsStore.Current.ReducedUiMotion=true;
            var csv=new System.Text.StringBuilder("real_seconds,scale\n");float start=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-start<.7f)
            {
                yield return new WaitForEndOfFrame();csv.AppendLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-start:F6},{motion.transform.localScale.x:F6}"));
            }
            File.WriteAllText(Path.Combine(_folder,"reduced-preference-transition.csv"),csv.ToString());
            if(Mathf.Abs(motion.transform.localScale.x-1)>.001f)throw new InvalidOperationException("Reduced preference did not settle.");
            foreach(bool entered in new[]{false,true,false})
            {
                if(entered)ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                else ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                for(int i=0;i<4;i++){yield return new WaitForEndOfFrame();if(Mathf.Abs(motion.transform.localScale.x-1)>.001f)throw new InvalidOperationException("Steady reduced text action still scales.");}
            }
            Settings.SettingsStore.Current.ReducedUiMotion=before;yield return Click("BackButton");
            Stage("reduced text-action transition and steady state verified");
        }
        private IEnumerator StartReadyRound()
        {
            yield return WaitFor(()=>Hud.Instance!=null&&GameServices.Round!=null);
            var gate=UnityEngine.Object.FindFirstObjectByType<ReadyGate>();
            if(gate!=null&&gate.AwaitingReady)gate.StartLocalCountdown();
            yield return WaitFor(()=>gate==null||!gate.CountingDown);
            yield return WaitFor(()=>GameServices.Round.RoundActive);
        }
        private IEnumerator Walk()
        {
            Stage("cold boot");
            yield return WaitFor(()=>Find("GuestAccount")!=null || Find("ContinueAccount")!=null || Find("StartButton")!=null,80);
            Settings.SettingsStore.Current.Fullscreen=false;
            Screen.SetResolution(1366,768,FullScreenMode.Windowed);
            yield return WaitFor(()=>Screen.width==1366&&Screen.height==768,8);
            if(Find("GuestAccount")!=null)
            {
                if(Find("SignInBack")!=null)throw new InvalidOperationException("Startup sign-in unexpectedly has Back.");
                yield return Shot("01-startup");yield return Click("GuestAccount");
            }
            else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            yield return WaitFor(()=>GameObject.Find("OwnerHomeCanvas")!=null);yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("02-home");yield return Motion("normal",false);
            Stage("settings and reduced motion");yield return Click("SettingsButton");yield return Click("SettingsSection4");
            if(!(Find("ReducedUiMotionValue") is Toggle))throw new InvalidOperationException("Motion toggle missing.");
            yield return Shot("03-settings");yield return Click("TumpSettingsBack");
            // Compare rendering without writing the shared standalone binding preferences.
            bool reducedBefore=Settings.SettingsStore.Current.ReducedUiMotion;Settings.SettingsStore.Current.ReducedUiMotion=true;
            yield return Motion("reduced",true);Settings.SettingsStore.Current.ReducedUiMotion=reducedBefore;
            yield return ReducedTextAction();
            foreach(string mode in new[]{"ClassicButton","HeroStrikeButton"})
            {
                SceneFlow.SetSelectedRules(Core.CustomGameRules.Defaults(mode=="ClassicButton"?Core.GameMode.Classic:Core.GameMode.HeroStrike));
                Stage(mode+" prepare");yield return Click("StartButton");yield return Click(mode);yield return Click("PracticeButton");
                yield return WaitFor(()=>GameObject.Find("OwnerPreparationCanvas")!=null);
                yield return WaitFor(()=>UnityEngine.Object.FindFirstObjectByType<MapPreviewSurface>()?.GetComponent<RawImage>().texture!=null);
                if(SceneFlow.SelectedRules.Rounds!=8)throw new InvalidOperationException("The normal default is not eight rounds.");
                yield return Shot(mode+"-setup");Stage(mode+" short custom rehearsal; normal default remains8");yield return Click("CustomGameButton");
                var rounds=Core.CustomGameRules.MinRounds;
                while(SceneFlow.SelectedRules.Rounds>rounds)yield return Click("RoundsPrevious");
                while(SceneFlow.SelectedRules.RoundSeconds>Core.CustomGameRules.MinRoundSeconds)yield return Click("SecondsPrevious");
                yield return Click("UseRulesButton");yield return Click("PrimaryButton");
                yield return StartReadyRound();
                Stage(mode+" playing");yield return new WaitForSecondsRealtime(1);yield return Shot(mode+"-hud");
                var watcher=UnityEngine.Object.FindFirstObjectByType<PauseWatcher>();var pause=Panel.Open<PausePanel>(watcher);pause.Local=watcher.Local;
                yield return WaitFor(()=>Find("ResumeMatch")!=null);yield return Shot(mode+"-pause");yield return Click("ResumeMatch");
                Stage(mode+" waiting for real result");StartFrameWindow(mode);
                yield return WaitFor(()=>Find("ResultRematch")!=null,65);StopFrameWindow();yield return Shot(mode+"-result");
                yield return Click("ResultTab1");yield return Shot(mode+"-details");
                if(mode=="ClassicButton")
                {
                    Stage("Classic real rematch");yield return Click("ResultRematch");
                    yield return WaitFor(()=>Find("ResultRematch")==null);yield return StartReadyRound();
                    yield return Shot("Classic-rematch");
                    watcher=UnityEngine.Object.FindFirstObjectByType<PauseWatcher>();pause=Panel.Open<PausePanel>(watcher);pause.Local=watcher.Local;
                    yield return Click("LeaveMatch");
                }
                else yield return Click("ResultMainMenu");
                yield return WaitFor(()=>GameObject.Find("OwnerHomeCanvas")!=null);
            }
            Stage("complete");
        }
    }
}
