using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug=UnityEngine.Debug;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    [InitializeOnLoad]
    public static class MapPreviewRecorder
    {
        const string MapsKey="MapPreviewRecorder.Maps",OutputKey="MapPreviewRecorder.Output",EncoderKey="MapPreviewRecorder.Encoder";
        static MapPreviewRecorder()
        {
            EditorApplication.playModeStateChanged-=Changed;
            EditorApplication.playModeStateChanged+=Changed;
        }
        /// <summary>Batch entry. Run in an isolated project copy; -tp-preview-maps accepts comma-separated scene IDs.</summary>
        public static void Record()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use the isolated batch recording command in docs/MAP_PREVIEW_RECORDING.md.");
            SessionState.SetString(MapsKey,Argument("-tp-preview-maps")??string.Join(",",SceneFlow.Maps));
            SessionState.SetString(OutputKey,Argument("-tp-preview-output")??"Logs/map-recording");
            SessionState.SetString(EncoderKey,Argument("-tp-preview-encoder")??"ffmpeg");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static string Argument(string key)
        {
            var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];return null;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||string.IsNullOrEmpty(SessionState.GetString(MapsKey,"")))return;
            var go=new GameObject("~MapPreviewRecording");Object.DontDestroyOnLoad(go);
            go.AddComponent<MapPreviewRecordingHost>().StartCoroutine(Observe(Run()));
        }
        static IEnumerator Observe(IEnumerator work)
        {
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0)
            {
                bool moved;object current=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error)
                {
                    Debug.LogException(error);
                    while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){Debug.LogException(cleanup);}
                    SessionState.EraseString(MapsKey);EditorApplication.Exit(1);yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
        }
        static IEnumerator Run()
        {
            string[] maps=SessionState.GetString(MapsKey,"").Split(',');string output=SessionState.GetString(OutputKey,"Logs/map-recording");
            string encoderPath=SessionState.GetString(EncoderKey,"ffmpeg");Directory.CreateDirectory(output);GameServices.Ensure();
            foreach(string map in maps)
            {
                if(Array.IndexOf(SceneFlow.Maps,map)<0)throw new InvalidOperationException("Unregistered capture map: "+map);
                yield return Capture(map,output,encoderPath);
            }
            SessionState.EraseString(MapsKey);Debug.Log("[MapPreviewRecorder] All requested recordings completed.");EditorApplication.Exit(0);
        }
        static IEnumerator Capture(string map,string output,string encoderPath)
        {
            string video=Path.Combine(output,map+"-loop.mp4"),poster=Path.Combine(output,map+"-poster.png");
            if(File.Exists(video)||File.Exists(poster))throw new IOException("Use a fresh recording output directory; existing media is preserved: "+map);
            if(SceneManager.GetSceneByName(map).isLoaded)throw new InvalidOperationException("Capture requires an isolated empty scene: "+map);
            string fingerprint=MapPreviewFreshness.SourceFingerprint(map);
            var root=new GameObject("Capture "+map,typeof(RectTransform),typeof(RawImage));var view=root.AddComponent<MapPreviewSurface>();view.PreferRecordedPreview=false;view.Show(map);
            while(view.Showing!=map||view.Camera==null)yield return null;
            const int width=1920,height=1080,fps=30,frames=780;
            var source=view.Camera.targetTexture;var descriptor=source.descriptor;descriptor.width=width;descriptor.height=height;
            var target=new RenderTexture(descriptor){name=source.name,filterMode=source.filterMode};target.Create();
            typeof(MapPreviewSurface).GetField("_target",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,target);
            view.Camera.targetTexture=target;root.GetComponent<RawImage>().texture=target;source.Release();Object.Destroy(source);
            var encoder=new Process{StartInfo=new ProcessStartInfo{FileName=encoderPath,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true}};
            foreach(string arg in new[]{"-y","-loglevel","warning","-f","rawvideo","-pixel_format","rgb24","-video_size","1920x1080","-framerate","30","-i","pipe:0","-vf","vflip","-an","-c:v","libx264","-preset","slower","-crf","14","-pix_fmt","yuv420p","-movflags","+faststart",video})encoder.StartInfo.ArgumentList.Add(arg);
            if(!encoder.Start())throw new InvalidOperationException("Encoder did not start.");var errors=encoder.StandardError.ReadToEndAsync();
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);var display=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            float capture=Time.captureDeltaTime;var previous=RenderTexture.active;bool srgb=GL.sRGBWrite;
            var time=typeof(MapPreviewSurface).GetField("_time",BindingFlags.Instance|BindingFlags.NonPublic);var aim=typeof(MapPreviewSurface).GetMethod("ApplyCamera",BindingFlags.Instance|BindingFlags.NonPublic);
            try
            {
                Time.captureDeltaTime=1f/fps;
                for(int frame=0;frame<frames;frame++)
                {
                    time.SetValue(view,frame/(float)fps);aim.Invoke(view,null);view.Camera.Render();GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                    Graphics.Blit(target,display);RenderTexture.active=display;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                    if(frame==0)File.WriteAllBytes(poster,image.EncodeToPNG());var data=image.GetRawTextureData<byte>();encoder.StandardInput.BaseStream.Write(data.ToArray(),0,data.Length);
                    RenderTexture.active=previous;GL.sRGBWrite=srgb;if(frame%150==0)Debug.Log("[MapPreviewRecorder] "+map+" frame="+frame+"/"+frames);yield return null;
                }
            }
            finally{Time.captureDeltaTime=capture;RenderTexture.active=previous;GL.sRGBWrite=srgb;encoder.StandardInput.Close();Object.Destroy(image);RenderTexture.ReleaseTemporary(display);}
            while(!encoder.HasExited)yield return null;File.WriteAllText(Path.Combine(output,map+"-encoder.txt"),errors.Result);
            if(encoder.ExitCode!=0)throw new InvalidOperationException("Encoder failed: "+map);encoder.Dispose();
            if(fingerprint!=MapPreviewFreshness.SourceFingerprint(map))throw new InvalidOperationException("Capture source changed: "+map);
            File.WriteAllText(Path.Combine(output,map+"-capture-source.txt"),fingerprint+"\n");
            var ownedScene=SceneManager.GetSceneByName(map);
            Object.Destroy(root);yield return null;
            // The live preview caches additive scenes for its menu lifetime. This
            // offline recorder owns that scene and must retire it between maps,
            // otherwise earlier suns/ambient scripts contaminate later captures.
            if(ownedScene.IsValid()&&ownedScene.isLoaded)
            {
                var unload=SceneManager.UnloadSceneAsync(ownedScene);if(unload!=null)yield return unload;
            }
            Debug.Log("[MapPreviewRecorder] "+map+" completed; owned scene unloaded.");
        }
    }
}
