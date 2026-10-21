using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ViewmodelMeshOwnershipTests
    {
        const string Arm = "Models/RosterArms/rafi_left";
        static readonly MethodInfo Reset = typeof(ViewmodelMeshAssets).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic);
        [UnitySetUp] public IEnumerator Before() { Reset.Invoke(null, null); yield return null; }
        [UnityTearDown] public IEnumerator After() { Reset.Invoke(null, null); yield return null; }

        [Test] public void OutlineWeldingLeavesSourceMeshUntouched()
        {
            var source = Resources.Load<Mesh>(Arm); Assert.IsNotNull(source);
            var originalTangents = source.tangents;
            var working = ViewmodelMeshAssets.Load(Arm);
            try
            {
                CollectionAssert.AreEqual(source.vertices, working.vertices);
                CollectionAssert.AreEqual(source.triangles, working.triangles);
                CollectionAssert.AreEqual(source.uv, working.uv);
                OutlineNormals.Weld(working);
                Assert.AreEqual(working.vertexCount, working.tangents.Length);
                CollectionAssert.AreEqual(originalTangents, source.tangents,
                    "Rendering preparation cannot edit the serialized source mesh.");
                Assert.AreNotSame(source, working);
                Assert.AreSame(working, ViewmodelMeshAssets.Load(Arm), "One working copy is shared per path.");
            }
            finally
            {
                // Preserve source data even while reproducing the old in-place mutation.
                if (working == source) { source.tangents = originalTangents; OutlineNormals.Forget(source); }
            }
        }
        [UnityTest] public IEnumerator ResetReleasesWorkingMeshButRetainsSource()
        {
            var source = Resources.Load<Mesh>(Arm); var working = ViewmodelMeshAssets.Load(Arm);
            Reset.Invoke(null, null); yield return null;
            Assert.IsTrue(working == null, "The cache owns and retires its working copy.");
            Assert.IsTrue(source != null, "The source asset belongs to Resources, not the working cache.");
            Assert.AreSame(source, Resources.Load<Mesh>(Arm));
        }
        [UnityTest] public IEnumerator WarmupSharesTheSynchronousWorkingCopy()
        {
            var source = Resources.Load<Mesh>("Models/viewmodel_arm"); Assert.IsNotNull(source);
            float progress = 0;
            var warm = ViewmodelMeshAssets.Warmup(null, value => { Assert.GreaterOrEqual(value, progress); progress = value; });
            Assert.IsTrue(warm.MoveNext());
            var working = ViewmodelMeshAssets.Load("Models/viewmodel_arm");
            yield return warm.Current;
            while (warm.MoveNext()) yield return warm.Current;
            Assert.AreEqual(1, progress);
            Assert.AreSame(working, ViewmodelMeshAssets.Load("Models/viewmodel_arm"));
            Assert.AreNotSame(source, working);
            CollectionAssert.AreEqual(source.vertices, working.vertices);
        }
        [Test] public void UnknownMeshStillReturnsNull()
        {
            Assert.IsNull(ViewmodelMeshAssets.Load(null));
            Assert.IsNull(ViewmodelMeshAssets.Load("Models/RosterArms/not-a-real-mesh"));
        }
    }
}
