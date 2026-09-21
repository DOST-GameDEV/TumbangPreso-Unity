using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TumbangPreso.Diagnostics
{
    // Opt-in built-player UI qualification. Normal play and tournament launches never install it.
    public sealed partial class OwnerUiPlayerReview : MonoBehaviour
    {
        [Serializable]private sealed class FrameWindow
        {
            public string mode,gpu,cpu;public int width,height,samples,framesOver33Ms;
            public int resultPolls,gc0,gc1,gc2;public bool pollEveryFrame;
            public float duration,averageFps,medianMs,p95Ms,p99Ms,maxMs;
        }
        private struct FrameContext
        { public float real,simulation,left;public int gc0,gc1,gc2; }
        [Serializable]private sealed class Report
        { public bool passed;public string error;public List<string> stages=new List<string>();public List<FrameWindow> frameWindows=new List<FrameWindow>(); }
        private readonly Report _report=new Report();
        private string _folder;
        private float _deadline;
        private bool _finished;
        private string _frameMode;
        private float _frameStarted;
        private readonly List<float> _frameTimes=new List<float>(8192);
        private readonly List<FrameContext> _frameContexts=new List<FrameContext>(8192);
        private int _resultPolls,_gc0Start,_gc1Start,_gc2Start;
        private bool _pollEveryFrame;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tp-uireview");
            if(Application.isEditor || at<0 || at+1>=args.Length || args.Contains("-tp-tournament"))return;
            var go=new GameObject("~OwnerUiPlayerReview");DontDestroyOnLoad(go);
            var probe=go.AddComponent<OwnerUiPlayerReview>();probe._folder=Path.GetFullPath(args[at+1]);
            probe._pollEveryFrame=args.Contains("-tp-review-frame-poll");
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
            {
                _frameTimes.Add(Time.unscaledDeltaTime*1000f);
                _frameContexts.Add(new FrameContext{real=Time.realtimeSinceStartup-_frameStarted,
                    simulation=Time.time,left=GameServices.Round.TimeLeft,
                    gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2)});
            }
            if(!_finished && Time.realtimeSinceStartup>_deadline)Finish(false,"UI review timed out after "+_report.stages.LastOrDefault());
        }
        private void StartFrameWindow(string mode)
        {
            _frameTimes.Clear();_frameContexts.Clear();_resultPolls=0;
            _gc0Start=GC.CollectionCount(0);_gc1Start=GC.CollectionCount(1);_gc2Start=GC.CollectionCount(2);
            _frameStarted=Time.realtimeSinceStartup;_frameMode=mode;
        }
        private void StopFrameWindow()
        {
            if(_frameMode==null)return;
            string mode=_frameMode;_frameMode=null;
            var window=new FrameWindow{mode=mode,width=Screen.width,height=Screen.height,samples=_frameTimes.Count,
                duration=Time.realtimeSinceStartup-_frameStarted,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,
                resultPolls=_resultPolls,pollEveryFrame=_pollEveryFrame,gc0=GC.CollectionCount(0)-_gc0Start,
                gc1=GC.CollectionCount(1)-_gc1Start,gc2=GC.CollectionCount(2)-_gc2Start};
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
            File.WriteAllLines(Path.Combine(_folder,mode+"-frame-context.csv"),new[]{"sample,real_seconds,simulation_seconds,round_left,gc0,gc1,gc2"}.Concat(
                _frameContexts.Select((v,i)=>FormattableString.Invariant($"{i},{v.real:F6},{v.simulation:F6},{v.left:F6},{v.gc0},{v.gc1},{v.gc2}"))));
        }
        private IEnumerator WaitForResult()
        {
            // Result discovery is UI work. Sampling frame times must not require a
            // scene-wide Selectable allocation/search on every gameplay frame.
            // Keep an explicit old-poll mode for the equivalent measurement control.
            float until=Time.realtimeSinceStartup+65;
            var pause=new WaitForSecondsRealtime(.1f);
            while(Time.realtimeSinceStartup<until)
            {
                _resultPolls++;
                if(Find("ResultRematch")!=null)yield break;
                if(_pollEveryFrame)yield return null;else yield return pause;
            }
            throw new InvalidOperationException("The real result screen did not arrive.");
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
            var folder=Path.Combine(_folder,name+"-frames");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,real_seconds,art_scale,target_scale,dust_vertices,cloud_x,cloud_y\n");
            var control=Find("SettingsButton");var pointer=Pointer(control);
            var motion=control.GetComponentInChildren<OwnerUiMotion>();
            if(!(control is OwnerPaintedAction)||motion==null)throw new InvalidOperationException("Painted home feedback missing.");
            Settings.SettingsStore.Current.ReducedUiMotion=reduced;
            EventSystem.current.SetSelectedGameObject(null);
            ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.4f);
            float start=Time.realtimeSinceStartup,nextImage=0,min=1,max=1;int frame=0;
            bool hover=false,down=false,up=false;
            var dust=UnityEngine.Object.FindFirstObjectByType<OwnerRoadDust>();
            var clouds=UnityEngine.Object.FindFirstObjectByType<OwnerMenuClouds>();
            if(clouds==null || clouds.GetComponent<RawImage>().material.GetTexture("_SkyMask")==null)
                throw new InvalidOperationException("Cloud motion data missing from the built player.");
            var skyMaterial=clouds.GetComponent<RawImage>().material;
            while(Time.realtimeSinceStartup-start<(reduced?4:12))
            {
                float age=Time.realtimeSinceStartup-start;
                if(age>.5f&&!hover){hover=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);}
                if(age>1.5f&&!down){down=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerDownHandler);}
                if(age>1.9f&&!up){up=true;ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerUpHandler);EventSystem.current.SetSelectedGameObject(null);ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerExitHandler);}
                yield return new WaitForEndOfFrame();
                float scale=motion.transform.localScale.x;min=Mathf.Min(min,scale);max=Mathf.Max(max,scale);
                int vertices=dust!=null?dust.canvasRenderer.GetMesh().vertexCount:0;
                if(Mathf.Abs(control.transform.localScale.x-1)>.001f || Mathf.Abs(scale-motion.transform.localScale.y)>.001f)
                    throw new InvalidOperationException("Supplied artwork stretched or its hit target moved.");
                if(reduced && (Mathf.Abs(scale-1)>.001f || vertices!=0))throw new InvalidOperationException("Reduced menu still animates.");
                if(!reduced && vertices==0)throw new InvalidOperationException("Background dust has no rendered geometry.");
                var cloudShift=skyMaterial.GetVector("_CloudDrift");
                if(reduced && cloudShift!=Vector4.zero)throw new InvalidOperationException("Reduced motion still moves clouds.");
                csv.AppendLine(FormattableString.Invariant($"{frame},{Time.realtimeSinceStartup-start:F6},{scale:F6},{control.transform.localScale.x:F6},{vertices},{cloudShift.x:F6},{cloudShift.y:F6}"));
                if(age>=nextImage)
                {
                    var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,frame.ToString("00000")+".jpg"),image.EncodeToJPG(93));Destroy(image);nextImage=age+1f/15f;
                }
                frame++;
            }
            File.WriteAllText(Path.Combine(folder,"frames.csv"),csv.ToString());
            if(!reduced && (max<1.015f || min>.99f))throw new InvalidOperationException("Hover/press feedback did not respond.");
            Stage(reduced?"reduced home motion verified":"normal home artwork and ground dust verified");
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
        private IEnumerator ReviewPcLoadout(bool hero)
        {
            yield return Click("LoadoutButton");
            yield return WaitFor(()=>GameObject.Find("OwnerLoadoutCanvas")!=null);
            string id=hero?"cheska":"maring";
            yield return Click("Portrait_"+id);
            var canvas=GameObject.Find("OwnerLoadoutCanvas").GetComponent<Canvas>();
            foreach(var choice in canvas.GetComponentsInChildren<CollectionChoice>())
                if(choice.GetComponentsInChildren<Text>(true).Length!=0)
                    throw new InvalidOperationException("A roster tile repeats a character name.");
            foreach(var text in canvas.GetComponentsInChildren<Text>())
            {
                if(!hero && (text.name=="Traits" || text.name.StartsWith("TraitLabel")))
                    throw new InvalidOperationException("Classic character stats returned to the picker.");
                if(text.name.StartsWith("TraitValue"))throw new InvalidOperationException("Redundant stat numbers returned.");
                if((text.name=="Heading" || text.name=="Traits" || text.name=="SelectedName") && text.font!=OwnerUiTheme.Current.Display)
                    throw new InvalidOperationException("The loadout heading lost Darumadrop.");
            }
            var preview=canvas.GetComponentInChildren<ModelPreview>();
            var surface=preview.GetComponentInChildren<ModelPreviewInput>();
            var point=RectTransformUtility.WorldToScreenPoint(null,((RectTransform)surface.transform).TransformPoint(((RectTransform)surface.transform).rect.center));
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=point,delta=new Vector2(90,0)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject!=surface.gameObject)
                throw new InvalidOperationException("The loadout model cannot receive mouse dragging.");
            var rotation=preview.PreviewCamera.transform.rotation;
            ExecuteEvents.Execute(surface.gameObject,pointer,ExecuteEvents.dragHandler);
            yield return new WaitForSecondsRealtime(.15f);
            if(Quaternion.Angle(rotation,preview.PreviewCamera.transform.rotation)<1)
                throw new InvalidOperationException("Dragging did not turn the preview camera.");
            pointer.button=PointerEventData.InputButton.Right;
            ExecuteEvents.Execute(surface.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(1366,768),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return WaitFor(()=>Screen.width==size.x&&Screen.height==size.y,8);
                yield return Shot((hero?"Hero":"Classic")+"-loadout-"+size.x+"x"+size.y);
            }
            Screen.SetResolution(1366,768,FullScreenMode.Windowed);
            yield return WaitFor(()=>Screen.width==1366&&Screen.height==768,8);
            foreach(int category in new[]{1,2})
            {
                yield return Click("TumpCategory"+category);
                yield return Shot((hero?"Hero":"Classic")+"-equipment-"+category);
            }
            yield return Click("TumpCategory0");
            yield return Click("TumpUseLoadout");
            Stage((hero?"Hero":"Classic")+" PC loadout, icon choices and mouse drag verified");
        }
        private IEnumerator StartReadyRound()
        {
            yield return WaitFor(()=>Hud.Instance!=null&&GameServices.Round!=null);
            var gate=UnityEngine.Object.FindFirstObjectByType<ReadyGate>();
            if(gate!=null&&gate.AwaitingReady)gate.StartLocalCountdown();
            yield return WaitFor(()=>gate==null||!gate.CountingDown);
            yield return WaitFor(()=>GameServices.Round.RoundActive);
        }
        private IEnumerator RecordEntryJourney()
        {
            var folder=Path.Combine(_folder,"entry-frames");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,real_seconds,stage\n");
            string[] actions={"SignInTab","CreateAccountTab","TermsLink","AcceptGuidelines","GuestAccount"};
            float[] times={1.0f,2.6f,4.0f,5.7f,7.2f};
            float start=Time.realtimeSinceStartup,nextImage=0;int step=0,frame=0;bool termsVerified=false;
            while(Time.realtimeSinceStartup-start<8.8f)
            {
                float age=Time.realtimeSinceStartup-start;
                if(step<actions.Length && age>=times[step])
                {
                    var control=Find(actions[step]);if(control==null)throw new InvalidOperationException("Missing entry control: "+actions[step]);
                    var pointer=Pointer(control);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                    if(hits.Count==0 || (hits[0].gameObject!=control.gameObject && !hits[0].gameObject.transform.IsChildOf(control.transform)))
                        throw new InvalidOperationException("Entry control covered: "+actions[step]);
                    ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(control.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                    step++;
                }
                yield return new WaitForEndOfFrame();
                if(step==4 && !termsVerified && age>6.1f)
                {
                    var terms=Find("TermsAcceptance") as Toggle;
                    if(terms==null || !terms.isOn || terms.graphic.canvasRenderer.GetAlpha()<.95f)
                        throw new InvalidOperationException("Agree did not visibly check Terms.");
                    termsVerified=true;Stage("Terms agreement visibly checks the signup box");
                }
                csv.AppendLine(FormattableString.Invariant($"{frame},{Time.realtimeSinceStartup-start:F6},{step}"));
                if(age>=nextImage)
                {
                    var image=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder,frame.ToString("00000")+".jpg"),image.EncodeToJPG(93));Destroy(image);nextImage=age+1f/15f;
                }
                frame++;
            }
            File.WriteAllText(Path.Combine(folder,"frames.csv"),csv.ToString());
            if(!termsVerified)throw new InvalidOperationException("Terms journey was not observed.");
            Stage("Both supplied login layouts, Terms and Guest recorded");
        }

        private IEnumerator MenuOnly()
        {
            Stage("cold loading and login music gate");
            yield return null;
            int silentFrames=0;float until=Time.realtimeSinceStartup+80;
            while(Find("GuestAccount")==null && Find("ContinueAccount")==null && Time.realtimeSinceStartup<until)
            {
                if(GameServices.Music!=null && GameServices.Music.Current!=null)throw new InvalidOperationException("Music began during loading.");
                if(GameObject.Find("OwnerLoadingCanvas")!=null && BootSting.Playing)throw new InvalidOperationException("Studio cue continued into illustrated loading.");
                silentFrames++;yield return null;
            }
            if(Find("GuestAccount")==null && Find("ContinueAccount")==null)throw new InvalidOperationException("Startup entrance did not arrive.");
            if(GameServices.Music.Current!=null)throw new InvalidOperationException("Music began before leaving startup login.");
            Stage("loading and login silent for "+silentFrames+" observed frames");
            if(Find("SignInBack")!=null)throw new InvalidOperationException("Startup unexpectedly has Back.");
            foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(960,540),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return WaitFor(()=>Screen.width==size.x&&Screen.height==size.y,8);
                yield return Shot("Login-"+size.x+"x"+size.y);
            }
            if(Find("GuestAccount")!=null)yield return RecordEntryJourney();else yield return Click("ContinueAccount");
            yield return WaitFor(()=>GameServices.Music.Current=="menu");
            if(GameServices.Music.GetComponents<AudioSource>().Count(a=>a.isPlaying)!=1)throw new InvalidOperationException("Expected exactly one menu music source.");
            Stage("menu music begins on revealed home");
            EventSystem.current.SetSelectedGameObject(null);
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(960,540)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return WaitFor(()=>Screen.width==size.x&&Screen.height==size.y,8);
                yield return Shot("Home-"+size.x+"x"+size.y);
            }
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            yield return WaitFor(()=>Screen.width==1920&&Screen.height==1080,8);
            yield return Motion("normal",false);yield return Motion("reduced",true);
            Settings.SettingsStore.Current.ReducedUiMotion=false;
            yield return Click("SettingsButton");yield return Click("SettingsCredits");
            yield return WaitFor(()=>GameObject.Find("OwnerCreditsCanvas")!=null);
            yield return Click("CreditsBack");yield return Click("TumpSettingsBack");
            yield return Click("StartButton");yield return WaitFor(()=>GameObject.Find("OwnerPlayCanvas")!=null);
            yield return Click("BackButton");
            Stage("settings credits and Play/Back remain reachable");
        }

        private IEnumerator RecoveryOnly()
        {
            Stage("recovery menu boundary setup");
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");
            else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            yield return Click("StartButton");yield return Click("ClassicButton");yield return Click("PracticeButton");
            yield return WaitFor(()=>Find("PrimaryButton")!=null);yield return Click("PrimaryButton");yield return StartReadyRound();
            foreach(var brain in UnityEngine.Object.FindObjectsByType<AIController>())brain.enabled=false;
            var watcher=UnityEngine.Object.FindAnyObjectByType<PauseWatcher>();var who=watcher.Local;
            var inputSettings=UnityEngine.InputSystem.InputSystem.settings;var oldBackground=inputSettings.backgroundBehavior;
            inputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
            var reader=who.GetComponent<PlayerInputReader>();
            var original=typeof(PlayerInputReader).GetField("_actions",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var actions=UnityEngine.Object.Instantiate(Resources.Load<UnityEngine.InputSystem.InputActionAsset>("TumbangPreso"));
            try
            {
                actions.devices=new UnityEngine.InputSystem.InputDevice[]{pad};original.SetValue(reader,actions);reader.SendMessage("Awake");reader.enabled=true;
                if(Settings.Rebinding.ResolveBindingIndexFor(actions,"Jump",InputLayer.InputDeviceKind.Gamepad,out var jump,out int binding))
                    jump.ApplyBindingOverride(binding,"<Gamepad>/buttonSouth");
                InputLayer.TouchInput.ReleaseAll();InputLayer.TouchInput.Active=false;
                who.ClearTrip();who.ClearStun();who.ApplyTrip();
                var pause=Panel.Open<PausePanel>(watcher);pause.Local=who;yield return null;
                EventSystem.current.SetSelectedGameObject(Find("ResumeMatch").gameObject);yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));
                yield return new WaitForSecondsRealtime(.2f);
                if(pause.gameObject.activeInHierarchy || who.MashPresses!=0)throw new InvalidOperationException("Menu Submit also mashed recovery or did not resume.");
                yield return new WaitForSecondsRealtime(.2f);
                if(who.MashPresses!=0)throw new InvalidOperationException("Held menu Submit leaked after resume.");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());yield return new WaitForSecondsRealtime(.15f);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));
                yield return new WaitForSecondsRealtime(.12f);
                if(who.MashPresses!=1)throw new InvalidOperationException("Fresh recovery press did not arrive after menu release.");
                Stage("native Resume consumes Submit; hold stays consumed; fresh press recovers");yield return Shot("recovery-after-menu");
            }
            finally
            {
                reader.enabled=false;actions.Disable();UnityEngine.Object.Destroy(actions);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);inputSettings.backgroundBehavior=oldBackground;
            }
        }

        private IEnumerator Walk()
        {
            if(Environment.GetCommandLineArgs().Contains("-tp-whole-matches"))
            {yield return WholeMatchesOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-halftime-only"))
            {yield return HalftimeOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-live-ultimates-only"))
            {yield return LiveUltimatesOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-busy-exchange-only"))
            {yield return BusyExchangeOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-introduction-bodies-only"))
            {yield return IntroductionBodiesOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-spectator-review-only"))
            {yield return SpectatorOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-gameplay-review-only"))
            {yield return GameplayOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-recovery-review-only"))
            {yield return RecoveryOnly();yield break;}
            if(Environment.GetCommandLineArgs().Contains("-tp-menu-review-only"))
            {yield return MenuOnly();yield break;}
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
            yield return Shot("03-settings");
            yield return Click("SettingsSection0");yield return Click("ControllerMapAction");
            var controller=GameObject.Find("ControllerMapCanvas").GetComponent<Canvas>();
            if(controller.GetComponentsInChildren<ControllerCalloutButton>().Count(b=>b.name.StartsWith("Callout_"))!=18
                || controller.transform.Find("Leaders").Cast<Transform>().Count(t=>t.name.StartsWith("Leader_"))<18)
                throw new InvalidOperationException("The central controller lost its mapped callouts or lines.");
            foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(1366,768),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return WaitFor(()=>Screen.width==size.x&&Screen.height==size.y,8);
                yield return Shot("Controller-map-"+size.x+"x"+size.y);
            }
            Screen.SetResolution(1366,768,FullScreenMode.Windowed);
            yield return WaitFor(()=>Screen.width==1366&&Screen.height==768,8);
            yield return Click("Done");yield return Shot("Controller-bindings");
            yield return Click("TumpSettingsBack");Stage("controller diagram and settings return verified");
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
                yield return ReviewPcLoadout(mode=="HeroStrikeButton");
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
                yield return WaitForResult();StopFrameWindow();yield return Shot(mode+"-result");
                yield return Click("ResultTab1");yield return Shot(mode+"-details");
                if(mode=="ClassicButton")
                {
                    string nextMap=SceneFlow.Maps[(Array.IndexOf(SceneFlow.Maps,SceneFlow.SelectedMap)+1)%SceneFlow.Maps.Length];
                    Stage("Classic real rematch");yield return Click("ResultRematch");
                    yield return WaitFor(()=>Find("ResultRematch")==null);yield return StartReadyRound();
                    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!=nextMap)
                        throw new InvalidOperationException("Rematch did not load the announced next map.");
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
