using System.Collections;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterNameplate
    {
        // TextMesh native initialization caches its default font before a caller
        // can assign ours. Pay that first-use cost behind loading, not at spawn.
        public static IEnumerator WarmupFont()
        {
            var font = MenuKit.Font;
            if (font == null) yield break;
            yield return null;
            var primer = new GameObject("Nameplate font primer");
            primer.hideFlags = HideFlags.HideAndDontSave;
            primer.SetActive(false);
            try
            {
                var label = primer.AddComponent<TextMesh>();
                label.font = font; label.fontSize = LabelFontSize;
                label.text = "TAYA PLAYER 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz# ñÑ";
                font.RequestCharactersInTexture(label.text, LabelFontSize, FontStyle.Normal);
            }
            finally { Object.Destroy(primer); }
            yield return null;
        }
    }
}
