using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI
{
    /// <summary>Recorded output of MapPreviewSurface, with its matching first-frame poster.</summary>
    public sealed class MapPreviewVideo : MonoBehaviour
    {
        const string Folder="UI/map-previews/";
        static readonly Dictionary<string,Texture2D> Posters=new Dictionary<string,Texture2D>();
        static readonly Dictionary<string,VideoClip> Clips=new Dictionary<string,VideoClip>();
        static readonly Dictionary<string,ResourceRequest> Pending=new Dictionary<string,ResourceRequest>();
        RawImage _image;
        VideoPlayer _player;
        RenderTexture _target;
        Texture2D _poster;
        bool _visible=true,_firstFrame,_failed,_resumeRequested,_reducedMotion;
        float _prepareStarted;
        public string Map { get; private set; }
        public bool HasFirstFrame=>_firstFrame;

        static string ClipPath(string map)
        {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return Folder+map+"-portable";
#else
            return Folder+map+"-loop";
#endif
        }

        public static IEnumerator Warmup(System.Action<float> completed=null)
        {
            var maps=SceneFlow.Maps;
            for(int i=0;i<maps.Length;i++)
            {
                string map=maps[i],poster=Folder+map+"-poster",clip=ClipPath(map);
                if(!Posters.ContainsKey(map))
                {
                    if(!Pending.TryGetValue(poster,out var load))Pending[poster]=load=Resources.LoadAsync<Texture2D>(poster);
                    yield return load;Posters[map]=load.asset as Texture2D;Pending.Remove(poster);
                }
                if(!Clips.ContainsKey(map))
                {
                    if(!Pending.TryGetValue(clip,out var load))Pending[clip]=load=Resources.LoadAsync<VideoClip>(clip);
                    yield return load;Clips[map]=load.asset as VideoClip;Pending.Remove(clip);
                }
                completed?.Invoke((i+1f)/maps.Length);yield return null;
            }
        }

        public static Texture2D PosterFor(string map)
        {
            if(!Posters.TryGetValue(map,out var poster)||poster==null)
                Posters[map]=poster=Resources.Load<Texture2D>(Folder+map+"-poster");
            return poster;
        }

        public bool Show(string map)
        {
            var poster=PosterFor(map);if(poster==null)return false;
            if(Map==map){SetVisible(_visible);return true;}
            Stop();Map=map;_poster=poster;_image=GetComponent<RawImage>();
            _image.texture=poster;_image.color=Color.white;_image.raycastTarget=false;
            if(!Clips.TryGetValue(map,out var clip)||clip==null)Clips[map]=clip=Resources.Load<VideoClip>(ClipPath(map));
            if(clip!=null&&!Settings.SettingsStore.Current.ReducedUiMotion&&_visible)Prepare(clip);
            return true;
        }

        void Prepare(VideoClip clip)
        {
            if(_player!=null||_failed||!isActiveAndEnabled||!_visible)return;
            _target=new RenderTexture((int)clip.width,(int)clip.height,0,RenderTextureFormat.ARGB32){name="RecordedMapPreview"};_target.Create();
            _player=gameObject.AddComponent<VideoPlayer>();_player.playOnAwake=false;_player.isLooping=true;
            _player.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
            _player.skipOnDrop=true;_player.waitForFirstFrame=true;_player.audioOutputMode=VideoAudioOutputMode.None;
            _player.renderMode=VideoRenderMode.RenderTexture;_player.targetTexture=_target;_player.clip=clip;
            _player.sendFrameReadyEvents=true;_player.prepareCompleted+=Prepared;_player.frameReady+=FirstFrame;_player.errorReceived+=Failed;
            _prepareStarted=Time.realtimeSinceStartup;_player.Prepare();
        }

        void Prepared(VideoPlayer player){if(player==_player&&!_failed&&_visible&&isActiveAndEnabled&&player.isActiveAndEnabled)player.Play();}
        void FirstFrame(VideoPlayer player,long frame)
        {
            if(player!=_player||_failed)return;
            _firstFrame=true;player.sendFrameReadyEvents=false;
            if(_image!=null)_image.texture=_target;
            if(!_visible)player.Pause();
        }
        void Failed(VideoPlayer player,string message)
        {
            if(player!=_player||_failed)return;
            _failed=true;_firstFrame=false;if(_image!=null)_image.texture=_poster;
            Debug.LogWarning("[MapPreviewVideo] "+Map+": "+message+"; retaining the recorded poster.");
            if(player!=null)player.Stop();
        }
        public void SetVisible(bool visible)
        {
            bool resumed=visible&&!_visible;
            _visible=visible;
            if(!visible){if(_player!=null)_player.Pause();return;}
            if(!isActiveAndEnabled)return;
            if(resumed&&!_firstFrame)_prepareStarted=Time.realtimeSinceStartup;
            if(Settings.SettingsStore.Current.ReducedUiMotion){if(_player!=null)_player.Pause();if(_image!=null)_image.texture=_poster;return;}
            if(_player!=null&&_player.isPrepared&&!_failed){if(_firstFrame&&_image!=null)_image.texture=_target;_player.Play();}
            else if(_player!=null&&!_failed&&_player.isActiveAndEnabled)
            {
                _firstFrame=false;if(_image!=null)_image.texture=_poster;
                _player.sendFrameReadyEvents=true;_prepareStarted=Time.realtimeSinceStartup;_player.Prepare();
            }
            else if(_player==null&&Map!=null&&Clips.TryGetValue(Map,out var clip)&&clip!=null)Prepare(clip);
        }
        void Update()
        {
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            if(reduced!=_reducedMotion){_reducedMotion=reduced;SetVisible(_visible);}
            if(_resumeRequested){_resumeRequested=false;SetVisible(_visible);}
            if(_visible&&_player!=null&&_player.isActiveAndEnabled&&!_firstFrame&&!_failed&&Time.realtimeSinceStartup-_prepareStarted>=30)
                Failed(_player,"Decoder did not provide its first frame");
        }
        void OnDisable(){if(_player!=null)_player.Pause();}
        void OnEnable(){if(!_firstFrame)_prepareStarted=Time.realtimeSinceStartup;_resumeRequested=true;}
        public void Stop()
        {
            if(_player!=null){_player.prepareCompleted-=Prepared;_player.frameReady-=FirstFrame;_player.errorReceived-=Failed;_player.Stop();_player.targetTexture=null;Destroy(_player);_player=null;}
            if(_target!=null){if(_image!=null&&_image.texture==_target)_image.texture=_poster;_target.Release();Destroy(_target);_target=null;}
            Map=null;_poster=null;_firstFrame=false;_failed=false;
        }
        void OnDestroy(){Stop();}
    }
}
