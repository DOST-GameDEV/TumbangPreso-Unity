using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Tests
{
    public sealed class CrispUiTextTests
    {
        [Test]
        public void EditableTextKeepsNativeCaretAndSelectionCoordinates()
        {
            var root = new GameObject("EditableTextQuality", typeof(RectTransform), typeof(Canvas), typeof(InputField));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().scaleFactor = .75f;
                var ordinary = new GameObject("Ordinary", typeof(RectTransform)).AddComponent<Text>();
                var crisp = new GameObject("Editable", typeof(RectTransform)).AddComponent<CrispUiText>();
                foreach (var label in new Text[] { ordinary, crisp })
                {
                    label.transform.SetParent(root.transform, false);
                    label.rectTransform.sizeDelta = new Vector2(500, 80);
                    label.font = OwnerUiTheme.Current.Reading; label.fontSize = 40;
                    label.text = "UMKB"; label.alignment = TextAnchor.MiddleLeft;
                }
                using var a = new VertexHelper(); using var b = new VertexHelper();
                typeof(Text).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(VertexHelper) }, null).Invoke(ordinary, new object[] { a });
                typeof(CrispUiText).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(VertexHelper) }, null).Invoke(crisp, new object[] { b });
                Assert.AreEqual(a.currentVertCount, b.currentVertCount);
                Assert.AreEqual(ordinary.cachedTextGenerator.verts.Count, crisp.cachedTextGenerator.verts.Count);
                UIVertex x = default, y = default;
                for (int i = 0; i < a.currentVertCount; i++)
                {
                    a.PopulateUIVertex(ref x, i); b.PopulateUIVertex(ref y, i);
                    Assert.AreEqual(x.position, y.position, "Editable ink must use the caret's exact native coordinate space.");
                    Assert.AreEqual(ordinary.cachedTextGenerator.verts[i].position, crisp.cachedTextGenerator.verts[i].position);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
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
