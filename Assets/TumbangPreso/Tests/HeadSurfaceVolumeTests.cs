using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class HeadSurfaceVolumeTests
    {
        [Test]
        public void ActualSurfacesContainSolidPartsAndExcludeAirInsideTheirCombinedBounds()
        {
            var root = new GameObject("Separated head surface reference");
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = new Mesh();
            try
            {
                var source = primitive.GetComponent<MeshFilter>().sharedMesh;
                mesh.CombineMeshes(new[] {
                    new CombineInstance { mesh = source, transform = Matrix4x4.Translate(Vector3.left * 2) },
                    new CombineInstance { mesh = source, transform = Matrix4x4.Translate(Vector3.right * 2) }
                });
                mesh.boneWeights = Enumerable.Repeat(new BoneWeight { boneIndex0 = 0, weight0 = 1 }, mesh.vertexCount).ToArray();
                mesh.bindposes = new[] { Matrix4x4.identity };
                var head = new GameObject("head"); head.transform.SetParent(root.transform);
                var skin = root.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh; skin.bones = new[] { head.transform };
                var volume = new HeadSurfaceVolume(root.transform);
                Assert.True(volume.Contains(new Vector3(-2, .1f, .1f)));
                Assert.True(volume.Contains(new Vector3(2, -.1f, .1f)));
                Assert.False(volume.Contains(new Vector3(0, .1f, .1f)), "A single combined AABB would count this air as head.");
                Assert.False(volume.Contains(new Vector3(2, .6f, .1f)));
                Assert.False(volume.Contains(new Vector3(-3, .1f, .1f)));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(primitive); Object.DestroyImmediate(mesh); }
        }
    }
}
