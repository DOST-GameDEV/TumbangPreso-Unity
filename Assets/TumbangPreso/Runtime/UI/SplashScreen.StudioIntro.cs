using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI
{
    public sealed partial class SplashScreen
    {
        [SerializeField] private VideoClip _portableClip;
        private VideoClip StudioClip
        {
            get
            {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                return _portableClip != null ? _portableClip : _clip;
#else
                return _clip != null ? _clip : _portableClip;
#endif
            }
        }
        private GameObject _studioCanvas;
        private VideoPlayer _studioPlayer;
        private RenderTexture _studioTarget;
        private RawImage _studioPicture;
        private Image _studioFade;
        private bool _studioFailed, _studioEnded, _studioFrameReady, _studioSkipped;
        private const float StudioPrepareBudget = 3f;
        private const float StudioFadeSeconds = .22f;

        private IEnumerator PlayStudioIntro()
        {
            HideConvertedContent();
            var clip = StudioClip;
            if (clip == null) yield break;
            _studioFailed = _studioEnded = _studioFrameReady = _studioSkipped = false;
            _studioCanvas = new GameObject("StudioIntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _studioCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1600;
            var scaler = _studioCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 1;
            AspectSafeCanvas.Apply(scaler);
            var backdrop = new GameObject("StudioWhite", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(_studioCanvas.transform,false);
            var white = backdrop.GetComponent<Image>(); white.color = Color.white; Stretch(white.rectTransform);
            var picture = new GameObject("StudioVideo", typeof(RectTransform), typeof(RawImage));
            picture.transform.SetParent(_studioCanvas.transform,false);
            _studioPicture = picture.GetComponent<RawImage>(); Stretch(_studioPicture.rectTransform);
            var fit = picture.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = clip.height > 0 ? (float)clip.width/clip.height : 16f/9;
            _studioPicture.enabled = false; _studioPicture.raycastTarget = false;
            var curtain = new GameObject("StudioWhiteFade", typeof(RectTransform), typeof(Image));
            curtain.transform.SetParent(_studioCanvas.transform,false);
            _studioFade = curtain.GetComponent<Image>(); _studioFade.color = new Color(1,1,1,0);
            Stretch(_studioFade.rectTransform);
            _studioTarget = new RenderTexture(1280,720,0); _studioTarget.Create();
            _studioPicture.texture = _studioTarget;
            _studioPlayer = _studioCanvas.AddComponent<VideoPlayer>();
            _studioPlayer.playOnAwake = false; _studioPlayer.isLooping = false;
            _studioPlayer.clip = clip; _studioPlayer.renderMode = VideoRenderMode.RenderTexture;
            _studioPlayer.targetTexture = _studioTarget; _studioPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _studioPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _studioPlayer.sendFrameReadyEvents = true;
            _studioPlayer.frameReady += StudioFrameReady;
            _studioPlayer.errorReceived += StudioVideoFailed;
            _studioPlayer.loopPointReached += StudioVideoEnded;
            ScreenTakeover.Register(this, () => _studioCanvas != null || _menuCurtain == this);
            _studioPlayer.Prepare();
            float began = Time.realtimeSinceStartup;
            while (!_studioPlayer.isPrepared && !_studioFailed)
            {
                if (SkipStudio()) break;
                if (Time.realtimeSinceStartup-began >= StudioPrepareBudget)
                { StudioVideoFailed(_studioPlayer,"preparation timed out"); break; }
                yield return null;
            }
            if (!_studioSkipped && !_studioFailed)
            {
                _studioPlayer.Play();
                // Keep the existing cue and saved gain; the early boot hook may already own it.
                if (_sting != null && !BootSting.Started)
                    GameServices.Audio?.PlayClipUi(_sting, Mathf.Clamp01(Settings.SettingsStore.Current.SfxGain));
                began = Time.realtimeSinceStartup;
                float budget = Mathf.Clamp((float)clip.length+2, 3, 15);
                while (!_studioEnded && !_studioFailed)
                {
                    if (SkipStudio()) break;
                    if (Time.realtimeSinceStartup-began >= budget)
                    { StudioVideoFailed(_studioPlayer,"playback timed out"); break; }
                    yield return null;
                }
            }
            for (float t=0;t<StudioFadeSeconds;t+=Time.unscaledDeltaTime)
            {
                _studioFade.color = new Color(1,1,1,Mathf.Clamp01(t/StudioFadeSeconds));
                yield return null;
            }
            _studioFade.color = Color.white;
            _studioPlayer.Stop();
            BootSting.Stop();
            // Retain opaque white until the loading surface has been built underneath.
        }

        private bool SkipStudio()
        {
            if (!InputLayer.MenuNav.StudioSkipPressed) return false;
            _studioSkipped = true;
            ScreenTakeover.ConsumeEscape();
            return true;
        }
        private void StudioFrameReady(VideoPlayer player,long frame)
        { _studioFrameReady = true; if (_studioPicture != null) _studioPicture.enabled = true; }
        private void StudioVideoEnded(VideoPlayer player) => _studioEnded = true;
        private void StudioVideoFailed(VideoPlayer player,string reason)
        { _studioFailed = true; Debug.LogWarning("[Splash] studio intro unavailable: " + reason + "; continuing to loading."); }
        private void ReleaseStudioIntro()
        {
            if (_studioPlayer != null)
            {
                _studioPlayer.frameReady -= StudioFrameReady;
                _studioPlayer.errorReceived -= StudioVideoFailed;
                _studioPlayer.loopPointReached -= StudioVideoEnded;
                _studioPlayer.Stop(); _studioPlayer.targetTexture = null;
            }
            if (_studioTarget != null) { _studioTarget.Release(); Destroy(_studioTarget); }
            if (_studioCanvas != null) { _studioCanvas.SetActive(false); Destroy(_studioCanvas); }
            _studioCanvas = null; _studioPlayer = null; _studioTarget = null;
            _studioPicture = null; _studioFade = null;
            ScreenTakeover.Unregister(this);
        }
    }
}
