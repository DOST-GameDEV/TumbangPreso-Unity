using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TumbangPreso
{
    // Keep the last presented world image beneath scheduled standings or recorded halftime footage.
    public sealed class RoundBreakFrame : MonoBehaviour
    {
        public RenderTexture Texture => _latest;
        public int CapturedFrames { get; private set; }
        RenderTexture _latest;
        RawImage _image;
        Canvas _canvas;
        Camera _camera;
        RoundBreakFrameCapture _capture;
        bool _locked;
        bool _imageVisible = true;
        Material _composite;
        readonly Vector3[] _corners = new Vector3[4];
        readonly List<BaseInputModule> _modules = new List<BaseInputModule>();

        void OnEnable() => RenderPipelineManager.endCameraRendering += CameraRendered;
        void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= CameraRendered;
            Release();
        }

        void Update()
        {
            var main = Camera.main;
            if (main == null || main == _camera) return;
            if (_capture != null) Destroy(_capture);
            _camera = main;
            _capture = main.gameObject.AddComponent<RoundBreakFrameCapture>();
            _capture.Owner = this;
        }

        void CameraRendered(ScriptableRenderContext context, Camera camera)
        {
            if (GraphicsSettings.currentRenderPipeline == null || camera != _camera) return;
            Record(camera.targetTexture, camera.pixelWidth, camera.pixelHeight);
        }

        internal void Record(RenderTexture source, int width, int height)
        {
            if ((_locked && _latest != null) || GameServices.Match?.MatchInProgress != true) return;
            width = Mathf.Max(1, width); height = Mathf.Max(1, height);
            if (_latest == null || _latest.width != width || _latest.height != height)
            {
                DisposeFrame();
                _latest = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    { name = "LastRoundView", filterMode = FilterMode.Bilinear };
                _latest.Create();
            }
            if (source != null) Graphics.Blit(source, _latest);
            else ScreenCapture.CaptureScreenshotIntoRenderTexture(_latest);
            CapturedFrames++;
            if (_locked) ShowFrame();
        }

        public void Freeze()
        {
            CaptureVisibleOverlay();
            _locked = true; _imageVisible = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                foreach (var module in EventSystem.current.GetComponents<BaseInputModule>())
                {
                    if (!module.enabled) continue;
                    _modules.Add(module); module.enabled = false;
                }
            }
            if (_latest != null) ShowFrame();
        }

        void CaptureVisibleOverlay()
        {
            // A catch/introduction may be the actual view at the boundary.
            // Snapshot its displayed texture before that presenter tears down.
            RawImage chosen = null;
            int order = int.MinValue;
            foreach (var picture in FindObjectsByType<RawImage>())
            {
                if (!picture.isActiveAndEnabled || picture.texture is not RenderTexture || picture.color.a <= .001f
                    || picture.GetComponentInParent<RoundBreakFrame>() != null) continue;
                var canvas = picture.canvas;
                if (canvas == null || !canvas.isActiveAndEnabled || canvas.sortingOrder < order) continue;
                picture.rectTransform.GetWorldCorners(_corners);
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
                Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]);
                if (low.x > 1 || low.y > 1 || high.x < Screen.width - 1 || high.y < Screen.height - 1) continue;
                chosen = picture; order = canvas.sortingOrder;
            }
            if (chosen == null || _latest == null) return;
            Color tint = chosen.color;
            foreach (var group in chosen.GetComponentsInParent<CanvasGroup>())
            {
                tint.a *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            if (tint.a <= .001f) return;
            if (_composite == null) _composite = new Material(Graphic.defaultGraphicMaterial) { hideFlags = HideFlags.HideAndDontSave };
            _composite.SetColor("_Color", tint);
            Graphics.Blit(chosen.texture, _latest, _composite);
            CapturedFrames++;
        }

        void ShowFrame()
        {
            if (_canvas == null)
            {
                var root = new GameObject("FrozenRoundView", typeof(RectTransform), typeof(Canvas));
                root.transform.SetParent(transform, false);
                _canvas = root.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 259;
                var picture = new GameObject("LastFrame", typeof(RectTransform), typeof(RawImage));
                picture.transform.SetParent(root.transform, false);
                _image = picture.GetComponent<RawImage>(); _image.raycastTarget = false;
                var rect = _image.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            _image.texture = _latest; _canvas.gameObject.SetActive(_imageVisible);
        }

        public void SetImageVisible(bool visible)
        {
            _imageVisible = visible;
            if (_canvas != null) _canvas.gameObject.SetActive(_locked && visible);
        }

        public void Release()
        {
            _locked = false;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            foreach (var module in _modules) if (module != null) module.enabled = true;
            _modules.Clear();
        }

        void DisposeFrame()
        {
            if (_latest == null) return;
            _latest.Release(); Destroy(_latest); _latest = null;
        }

        void OnDestroy()
        {
            Release(); DisposeFrame();
            if (_capture != null) Destroy(_capture);
            if (_composite != null) Destroy(_composite);
        }
    }

    internal sealed class RoundBreakFrameCapture : MonoBehaviour
    {
        internal RoundBreakFrame Owner;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Owner != null) Owner.Record(source, source.width, source.height);
            Graphics.Blit(source, destination);
        }
    }
}
