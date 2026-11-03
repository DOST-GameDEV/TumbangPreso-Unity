using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Tests
{
    /// <summary>The pale line down a short kit pill: the middle quad must not squeeze the art's long middle.</summary>
    public class KitSliceCentreTests
    {
        private const float Left = .15f, Right = .85f, UvPerUnit = .003f;

        private static void Quad(VertexHelper vh, float x0, float x1, float u0, float u1)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, 0), Color.white, new Vector2(u0, 0));
            vh.AddVert(new Vector3(x0, 56), Color.white, new Vector2(u0, 1));
            vh.AddVert(new Vector3(x1, 56), Color.white, new Vector2(u1, 1));
            vh.AddVert(new Vector3(x1, 0), Color.white, new Vector2(u1, 0));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        private static float U(VertexHelper vh, int index)
        {
            var v = new UIVertex();
            vh.PopulateUIVertex(ref v, index);
            return v.uv0.x;
        }

        [Test]
        public void ShortPillCentreGetsOnlyTheArtItHasRoomFor()
        {
            using (var vh = new VertexHelper())
            {
                Quad(vh, 0, 47, 0, Left);
                Quad(vh, 47, 49, Left, Right);
                Quad(vh, 49, 96, Right, 1);
                KitSlice.NarrowCentre(vh, Left, Right, UvPerUnit);

                Assert.That(U(vh, 4), Is.EqualTo(Left).Within(1e-5f));
                Assert.That(U(vh, 6), Is.EqualTo(Left + 2 * UvPerUnit).Within(1e-5f));
                Assert.That(U(vh, 3), Is.EqualTo(Left).Within(1e-5f), "the left end keeps its art");
                Assert.That(U(vh, 8), Is.EqualTo(Right).Within(1e-5f), "the right end keeps its art");
            }
        }

        [Test]
        public void LongPillCentreIsLeftAlone()
        {
            using (var vh = new VertexHelper())
            {
                Quad(vh, 47, 947, Left, Right);
                KitSlice.NarrowCentre(vh, Left, Right, UvPerUnit);
                Assert.That(U(vh, 2), Is.EqualTo(Right).Within(1e-5f));
            }
        }
    }
}
