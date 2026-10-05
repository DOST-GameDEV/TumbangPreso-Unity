using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Tests
{
    public sealed class CrispUiTextTests
    {
        [TestCase(.75f, false)] [TestCase(1f, false)] [TestCase(1.5f, false)]
        [TestCase(.75f, true)] [TestCase(1f, true)] [TestCase(1.5f, true)]
        public void ActualFontGetsMoreSamplesWithoutChangingLayout(float scale, bool reading)
        {
            var root = new GameObject("TextQuality", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.scaleFactor = scale;
                var ordinary = new GameObject("Ordinary", typeof(RectTransform)).AddComponent<Text>();
                var crisp = new GameObject("Crisp", typeof(RectTransform)).AddComponent<CrispUiText>();
                foreach (var label in new Text[] { ordinary, crisp })
                {
                    label.transform.SetParent(root.transform, false);
                    label.rectTransform.sizeDelta = new Vector2(700, 180);
                    label.font = reading ? OwnerUiTheme.Current.Reading : OwnerUiTheme.Current.Display;
                    label.fontSize = 40; label.text = "Join game\nReady to play";
                    label.alignment = TextAnchor.MiddleLeft; label.supportRichText = false;
                }
                using var normalMesh = new VertexHelper(); using var crispMesh = new VertexHelper();
                typeof(Text).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(ordinary, new object[] { normalMesh });
                typeof(CrispUiText).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(crisp, new object[] { crispMesh });
                Assert.Greater(crispMesh.currentVertCount, 0);
                Assert.AreEqual(normalMesh.currentVertCount, crispMesh.currentVertCount);
                Assert.AreEqual(ordinary.preferredWidth, crisp.preferredWidth);
                Assert.AreEqual(ordinary.preferredHeight, crisp.preferredHeight);
                Assert.IsTrue(crisp.font.GetCharacterInfo('J', out var detailed, Mathf.RoundToInt(40 * scale * 2)));
                Assert.IsTrue(ordinary.font.GetCharacterInfo('J', out var native, Mathf.RoundToInt(40 * scale)));
                Assert.Greater(detailed.glyphHeight, native.glyphHeight);
                Debug.Log($"[TextQuality] reading={reading} scale={scale} glyphPixels={native.glyphHeight}->{detailed.glyphHeight} layout={crisp.preferredWidth}x{crisp.preferredHeight}");
                UIVertex a = default, b = default;
                for (int i = 0; i < normalMesh.currentVertCount; i++)
                {
                    normalMesh.PopulateUIVertex(ref a, i); crispMesh.PopulateUIVertex(ref b, i);
                    Assert.LessOrEqual(Vector3.Distance(a.position, b.position), 4f,
                        "Raster hinting may shift ink slightly but must not change line placement.");
                }
                crisp.text = "";
                typeof(CrispUiText).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(crisp, new object[] { crispMesh });
                Assert.IsFalse((bool)typeof(Text).GetField("m_DisableFontTextureRebuiltCallback", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crisp));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
