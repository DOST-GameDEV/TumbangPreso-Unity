using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>A printed UI stage follows the actual preview's projected feet.</summary>
    public sealed class TumpPreviewPlinth : MonoBehaviour
    {
        private ModelPreview _preview;
        private RectTransform _plinth;
        private GameObject _subject;
        private Renderer[] _renderers;
        public void Bind(ModelPreview preview, RectTransform plinth) { _preview = preview; _plinth = plinth; }
        private void LateUpdate()
        {
            if (_preview == null || _plinth == null || _preview.Subject == null || _preview.PreviewCamera == null) return;
            if (_subject != _preview.Subject)
            {
                _subject = _preview.Subject;
                _renderers = _subject.GetComponentsInChildren<Renderer>();
            }
            bool found = false; Bounds bounds = default;
            foreach (var renderer in _renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return;
            var foot = _preview.PreviewCamera.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            _plinth.anchorMin = _plinth.anchorMax = new Vector2(foot.x, foot.y);
            _plinth.pivot = new Vector2(.5f, .5f);
            _plinth.anchoredPosition = new Vector2(0, -8);
            _plinth.sizeDelta = new Vector2(310, 42);
        }
    }
}
