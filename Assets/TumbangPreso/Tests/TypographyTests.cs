using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Tests
{
    public sealed class TypographyTests
    {
        [Test]
        public void BriefFacesAreDynamicAndThreeDistinctRoles()
        {
            Assert.IsTrue(MenuKit.Font.dynamic, "Headings must rasterize at their actual size.");
            Assert.IsTrue(MenuKit.AccentFont.dynamic, "Brush labels must not upscale a 16-pixel atlas.");
            Assert.IsTrue(MenuKit.BodyFont.dynamic, "Reading text must stay crisp across resolutions.");
            Assert.AreNotSame(MenuKit.Font, MenuKit.AccentFont);
            Assert.AreNotSame(MenuKit.AccentFont, MenuKit.BodyFont);
        }

        [Test]
        public void LydianLinesHaveRoomForTheirActualLetterforms()
        {
            var go = new GameObject("Typography sample", typeof(RectTransform), typeof(Text));
            try
            {
                var label = go.GetComponent<Text>();
                label.fontSize = 32;
                MenuKit.Read(label);
                var settings = label.GetGenerationSettings(new Vector2(600, 200));
                var generator = new TextGenerator();
                Assert.IsTrue(generator.Populate("One more round.\nA good game stays with you.", settings));
                Assert.GreaterOrEqual(generator.lines.Count, 2);
                float spacing = Mathf.Abs(generator.lines[1].topY - generator.lines[0].topY);
                Assert.GreaterOrEqual(spacing, 25.6f,
                    "The supplied font's positive signed descent collapsed lines to half their ink height.");
                Assert.AreEqual(FontStyle.Normal, label.fontStyle);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
