using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TemporaryReadingFontTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [Test] public void SerializedThemeAndLoadingTipsUseTheSameRealBoldFace()
        {
            var font = Resources.Load<Font>("UI/fonts/Nunito-Bold");
            Assert.IsNotNull(font); Assert.IsTrue(font.dynamic);
            Assert.AreSame(font, OwnerUiTheme.Current.ReadingFont);
            Assert.AreSame(font, OwnerUiTheme.Current.Reading);
            Assert.AreSame(font, HubStyle.ReadingFont);
            Assert.IsNotNull(Resources.Load<Font>("UI/fonts/Lydian-Regular"), "Temporary replacement retains the original asset.");
        }
        [Test] public void MissingThemeReferenceAlsoFallsBackToNunito()
        {
            var theme = ScriptableObject.CreateInstance<OwnerUiTheme>();
            try { theme.ReadingFont = null; Assert.AreSame(Resources.Load<Font>("UI/fonts/Nunito-Bold"), theme.Reading); }
            finally { Object.DestroyImmediate(theme); }
        }
        [UnityTest] public IEnumerator ActualTutorialParagraphsUseNunitoAndExpandWithoutClipping()
        {
            var owner = new GameObject("Temporary reading-font review");
            var hud = GuidedTrainingHud.Build(owner.transform);
            try
            {
                string[] copy = {
                    "Jump around the arena.",
                    "Scroll the mouse wheel to curve the throw. Use this to make the throw harder to block.",
                    "Role abilities change depending on which role you take each round. It adapts to your role, helping you escape tags when attacking or chase attackers when defending"
                };
                foreach (var words in copy)
                {
                    hud.SetLesson(1, GuidedTraining.LessonCount, "ROLE ABILITIES", words, "", Color.white);
                    yield return null; Canvas.ForceUpdateCanvases();
                    var body = hud.GetComponentsInChildren<Text>(true).Single(t => t.name == "LessonBody");
                    var card = hud.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "ObjectiveCard");
                    LayoutRebuilder.ForceRebuildLayoutImmediate(card);
                    Assert.AreSame(Resources.Load<Font>("UI/fonts/Nunito-Bold"), body.font);
                    Assert.AreEqual(28, body.fontSize, "Owner requested smaller replacement reading text.");
                    Assert.AreEqual(FontStyle.Normal, body.fontStyle, "Use authored bold glyphs, not synthetic bold.");
                    Assert.AreEqual(words, body.text);
                    Assert.LessOrEqual(body.preferredHeight, body.rectTransform.rect.height + 2, "Tutorial paragraph clips after font replacement.");
                    Assert.AreSame(OwnerUiTheme.Current.Display, hud.GetComponentsInChildren<Text>(true).Single(t => t.name == "LessonTitle").font);
                }
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    yield return TumpUiCapture.Capture("Temporary-Nunito-smaller-tutorial", hud.GetComponent<Canvas>(), 960, 540, false, false, checkActionBounds: true);
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
