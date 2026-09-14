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
        [Serializable]private sealed class Report { public bool passed;public string error;public List<string> stages=new List<string>(); }
        private readonly Report _report=new Report();
        private string _folder;
        private float _deadline;
        private bool _finished;
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
        private void Update(){if(!_finished && Time.realtimeSinceStartup>_deadline)Finish(false,"UI review timed out after "+_report.stages.LastOrDefault());}
        private void Stage(string label){_report.stages.Add(label);_deadline=Time.realtimeSinceStartup+100;Debug.Log("[OwnerUiReview] "+label);}
        private void Finish(bool passed,string error)
        {
            if(_finished)return;_finished=true;_report.passed=passed;_report.error=error;
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
            yield return WaitFor(()=>Find(name)!=null);var control=Find(name);Canvas.ForceUpdateCanvases();var pointer=Pointer(control);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Selectable>()!=control)throw new InvalidOperationException(name+" has no reachable hit target.");
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.12f);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return new WaitForSecondsRealtime(.2f);
        }
        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image==null)throw new InvalidOperationException("Player screenshot missing.");
            File.WriteAllBytes(Path.Combine(_folder,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        private IEnumerator Motion(string name,bool reduced)
        {
            var folder=Path.Combine(_folder,name+"-frames");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,real_seconds,art_scale\n");var control=Find("StartButton");var pointer=Pointer(control);
            var art=control.transform.Find("PaintedArtwork");float minScale=2,maxScale=0;
            float start=Time.realtimeSinceStartup;int frame=0;bool hover=false,down=false,up=false;
            while(Time.realtimeSinceStartup-start<2.3f)
            {
                float age=Time.realtimeSinceStartup-start;
                if(age>.35f && !hover){hover=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);}
                if(age>1 && !down){down=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerDownHandler);}
                if(age>1.25f && !up){up=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerExitHandler);}
                yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder,frame.ToString("00000")+".jpg"),image.EncodeToJPG(88));Destroy(image);
                float scale=art.localScale.x;minScale=Mathf.Min(minScale,scale);maxScale=Mathf.Max(maxScale,scale);
                csv.Append(frame).Append(',').Append((Time.realtimeSinceStartup-start).ToString("0.000000",System.Globalization.CultureInfo.InvariantCulture))
                    .Append(',').Append(scale.ToString("0.000000",System.Globalization.CultureInfo.InvariantCulture)).Append('\n');frame++;
                yield return new WaitForSecondsRealtime(.05f);
            }
            File.WriteAllText(Path.Combine(folder,"frames.csv"),csv.ToString());
            if(reduced && maxScale-minScale>.01f)throw new InvalidOperationException("Reduced UI motion still changes action scale.");
            if(!reduced && maxScale-minScale<.01f)throw new InvalidOperationException("Normal action feedback has no visible scale response.");
            Stage(reduced?"reduced-motion recorded":"normal-motion recorded");
        }
        private IEnumerator Walk()
        {
            Stage("cold boot");
            yield return WaitFor(()=>Find("GuestAccount")!=null || Find("ContinueAccount")!=null || Find("StartButton")!=null,80);
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
            foreach(string mode in new[]{"ClassicButton","HeroStrikeButton"})
            {
                Stage(mode+" prepare");yield return Click("StartButton");yield return Click(mode);yield return Click("PracticeButton");
                yield return WaitFor(()=>GameObject.Find("OwnerPreparationCanvas")!=null);
                yield return WaitFor(()=>UnityEngine.Object.FindFirstObjectByType<MapPreviewSurface>()?.GetComponent<RawImage>().texture!=null);
                yield return Shot(mode+"-setup");yield return Click("CustomGameButton");
                var rounds=Core.CustomGameRules.MinRounds;
                while(SceneFlow.SelectedRules.Rounds>rounds)yield return Click("RoundsPrevious");
                while(SceneFlow.SelectedRules.RoundSeconds>Core.CustomGameRules.MinRoundSeconds)yield return Click("SecondsPrevious");
                yield return Click("UseRulesButton");yield return Click("PrimaryButton");
                yield return WaitFor(()=>Hud.Instance!=null && GameServices.Round!=null && GameServices.Round.RoundActive);
                Stage(mode+" playing");yield return new WaitForSecondsRealtime(1);yield return Shot(mode+"-hud");
                var watcher=UnityEngine.Object.FindFirstObjectByType<PauseWatcher>();var pause=Panel.Open<PausePanel>(watcher);pause.Local=watcher.Local;
                yield return WaitFor(()=>Find("ResumeMatch")!=null);yield return Shot(mode+"-pause");yield return Click("ResumeMatch");
                Stage(mode+" waiting for real result");yield return WaitFor(()=>Find("ResultRematch")!=null,65);yield return Shot(mode+"-result");
                yield return Click("ResultTab1");yield return Shot(mode+"-details");
                if(mode=="ClassicButton")
                {
                    Stage("Classic real rematch");yield return Click("ResultRematch");
                    yield return WaitFor(()=>Find("ResultRematch")==null && GameServices.Round!=null && GameServices.Round.RoundActive);
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
