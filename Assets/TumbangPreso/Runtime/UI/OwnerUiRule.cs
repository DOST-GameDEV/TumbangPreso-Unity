using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The rule under a link, sized to the words above it rather than to a number.
    ///
    /// ⚠️⚠️ UNITY'S LEGACY `Text` HAS NO UNDERLINE AND RICH TEXT IS OFF HERE.
    /// `OwnerUiLayout.Text` sets `supportRichText=false` deliberately, so a stray
    /// bracket in a username cannot become markup, which takes `&lt;u&gt;` with it.
    /// She drew both login links underlined, so the rule is drawn.
    ///
    /// ⚠️ THE WIDTH IS READ FROM THE LABEL, NOT WRITTEN DOWN. A hard-coded width
    /// is correct for exactly one string, and TERMS &amp; CONDITIONS is 344 units
    /// in her ink and 327 rendered, so the first version of this drew a rule that
    /// stuck out past its own caption. It re-reads whenever the text changes, so
    /// a translated string cannot inherit an English width either.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class OwnerUiRule : MonoBehaviour
    {
        public Text Label;
        public float Thickness = 3, Drop = 33;
        private string _seen;

        public static void Under(Text label, float thickness = 3, float drop = 33)
        {
            var rule = OwnerUiLayout.Rect(label.transform, "Underline").gameObject.AddComponent<Image>();
            rule.color = label.color;
            rule.raycastTarget = false;
            var fit = rule.gameObject.AddComponent<OwnerUiRule>();
            fit.Label = label; fit.Thickness = thickness; fit.Drop = drop;
        }

        private void LateUpdate()
        {
            if (Label == null) return;
            if (_seen == Label.text) return;
            _seen = Label.text;
            OwnerUiLayout.Place((RectTransform)transform, 0, Drop, Label.preferredWidth, Thickness);
            var colour = Label.color;
            GetComponent<Image>().color = colour;
        }
    }
}
