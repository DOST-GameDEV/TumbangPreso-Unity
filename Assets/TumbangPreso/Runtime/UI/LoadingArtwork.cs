using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Shared loading-only illustration deck. Five-second holds, no buttons or popups.</summary>
    public sealed class LoadingArtwork : MonoBehaviour
    {
        public const float HoldSeconds = 5;
        private static readonly string[] Paths =
        {
            "UI/loading-illustrations/01-street-court-v1",
            "UI/loading-illustrations/02-lagoon-deck-v1",
            "UI/loading-illustrations/03-rooftop-court-v1",
        };
        private static int _nextStart;
        private static readonly Texture2D[] Cached = new Texture2D[3];
        private RawImage _front, _back;
        private Text _tip;
        private float _nextChange, _changedAt;
        public int FrameIndex { get; private set; }

        public static LoadingArtwork Install(RectTransform parent)
        {
            var root = OwnerUiLayout.Rect(parent, "LoadingIllustrations");
            OwnerUiLayout.Fill(root); root.SetAsFirstSibling();
            var deck = root.gameObject.AddComponent<LoadingArtwork>();
            deck._back = Picture(root, "PreviousIllustration");
            deck._front = Picture(root, "CurrentIllustration");
            deck.FrameIndex = _nextStart++ % Paths.Length;
            deck.Show(deck.FrameIndex, false);
            return deck;
        }

        private static RawImage Picture(Transform parent, string name)
        {
            var image = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = Vector2.one * .5f;
            image.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            return image;
        }

        public void BindTip(Text tip) { _tip = tip; RefreshTip(); }

        private void Show(int index, bool blend)
        {
            FrameIndex = index;
            if (Cached[index] == null) Cached[index] = Resources.Load<Texture2D>(Paths[index]);
            _back.texture = _front.texture;
            _back.GetComponent<AspectRatioFitter>().aspectRatio = _front.GetComponent<AspectRatioFitter>().aspectRatio;
            _front.texture = Cached[index];
            if (_front.texture != null)
                _front.GetComponent<AspectRatioFitter>().aspectRatio = _front.texture.width / (float)_front.texture.height;
            _changedAt = Time.unscaledTime;
            _nextChange = _changedAt + HoldSeconds;
            bool fade = blend && !Settings.SettingsStore.Current.ReducedUiMotion && _back.texture != null;
            _front.color = new Color(1, 1, 1, fade ? 0 : 1);
            _back.enabled = fade;
            RefreshTip();
        }

        private void RefreshTip()
        {
            if (_tip != null) _tip.text = LoadingPresentation.Tips[FrameIndex % LoadingPresentation.Tips.Length];
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextChange) Show((FrameIndex + 1) % Paths.Length, true);
            if (!_back.enabled) return;
            float alpha = Settings.SettingsStore.Current.ReducedUiMotion ? 1 : Mathf.Clamp01((Time.unscaledTime - _changedAt) / .4f);
            _front.color = new Color(1, 1, 1, alpha);
            if (alpha >= 1) _back.enabled = false;
        }
    }
}
