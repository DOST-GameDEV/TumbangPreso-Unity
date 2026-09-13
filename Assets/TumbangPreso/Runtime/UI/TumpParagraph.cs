using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Measured supporting copy with one line of breathing room for font descenders and wrapping.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class TumpParagraph : MonoBehaviour, ILayoutElement
    {
        private Text _text;
        private Text Label => _text != null ? _text : (_text = GetComponent<Text>());
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => -1;
        public float preferredWidth => -1;
        public float flexibleWidth => 1;
        public float minHeight => preferredHeight;
        public float preferredHeight => Label.preferredHeight + Label.fontSize * Label.lineSpacing;
        public float flexibleHeight => 0;
        public int layoutPriority => 2;
    }
}
