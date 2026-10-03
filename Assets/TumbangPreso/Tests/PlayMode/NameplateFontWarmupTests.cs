using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NameplateFontWarmupTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator WarmupPreparesNameplateGlyphSizeAndRetiresItsPrimer()
        {
            yield return CharacterNameplate.WarmupFont();
            yield return null;
            var font = MenuKit.Font;
            Assert.IsNotNull(font);
            foreach (char glyph in "TAYA PLAYER 0123456789#")
                if (glyph != ' ')
                    Assert.IsTrue(font.GetCharacterInfo(glyph, out _, CharacterNameplate.LabelFontSize, FontStyle.Normal),
                        $"Nameplate glyph {glyph} was not prepared at its actual size");
            foreach (var label in Resources.FindObjectsOfTypeAll<TextMesh>())
                Assert.AreNotEqual("Nameplate font primer", label.gameObject.name, "A loading-only primer survived warmup");
        }
    }
}
