using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class SpectatorCamera
    {
        private Canvas _lensBars;
        private Image[] _lensEdges;
        private Rect _lensViewport = new Rect(0, 0, 1, 1);
        private bool _borrowedClip, _borrowedAspect;
        private float _originalNear, _originalFar;
        public Rect ObservedViewport => _lensViewport;

        public static Rect FitObservedViewport(float sourceAspect, float outputAspect)
        {
            if (sourceAspect <= 0 || outputAspect <= 0) return new Rect(0, 0, 1, 1);
            if (sourceAspect < outputAspect)
            {
                float width = sourceAspect / outputAspect;
                return new Rect((1 - width) * .5f, 0, width, 1);
            }
            float height = outputAspect / sourceAspect;
            return new Rect(0, (1 - height) * .5f, 1, height);
        }
        private void ApplyObservedLens(float aspect, float near = 0, float far = 0)
        {
            if (_camera == null) return;
            if (near > 0 && far > near)
            {
                if (!_borrowedClip) { _originalNear = _camera.nearClipPlane; _originalFar = _camera.farClipPlane; _borrowedClip = true; }
                _camera.nearClipPlane = near; _camera.farClipPlane = far;
            }
            else if (_borrowedClip)
            {
                _camera.nearClipPlane = _originalNear; _camera.farClipPlane = _originalFar; _borrowedClip = false;
            }
            // Use the full output. Deriving it from a rounded cropped pixel rect can
            // change the fitted viewport a fraction of a pixel on every frame.
            var target = _camera.targetTexture;
            float output = (target != null ? target.width : Screen.width)
                / (float)Mathf.Max(1, target != null ? target.height : Screen.height);
            Rect fit = FitObservedViewport(aspect, output);
            if (_camera.rect != fit) _camera.rect = fit;
            if (aspect > 0)
            {
                if (!_borrowedAspect || _camera.aspect != aspect) _camera.aspect = aspect;
                _borrowedAspect = true;
            }
            else if (_borrowedAspect)
            {
                _camera.ResetAspect();
                _borrowedAspect = false;
            }
            bool bars = fit.width < .999f || fit.height < .999f;
            if (!bars)
            {
                _lensViewport = fit;
                if (_lensBars != null) _lensBars.enabled = false;
                return;
            }
            if (_lensBars == null)
            {
                _lensBars = OwnerUiLayout.Canvas(transform, "ObservedLensBars", 90);
                _lensBars.GetComponent<InputLayer.ScreenFocus>().enabled = false;
                _lensEdges = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    _lensEdges[i] = OwnerUiLayout.Rect(_lensBars.transform, "LensEdge" + i).gameObject.AddComponent<Image>();
                    _lensEdges[i].color = Color.black; _lensEdges[i].raycastTarget = false;
                }
            }
            _lensBars.enabled = true;
            if (_lensViewport == fit) return;
            _lensViewport = fit;
            Edge(0, new Vector2(0, 0), new Vector2(fit.xMin, 1));
            Edge(1, new Vector2(fit.xMax, 0), Vector2.one);
            Edge(2, new Vector2(0, 0), new Vector2(1, fit.yMin));
            Edge(3, new Vector2(0, fit.yMax), Vector2.one);
            void Edge(int index, Vector2 min, Vector2 max)
            {
                var rect = _lensEdges[index].rectTransform; rect.anchorMin = min; rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
        }
    }
}
