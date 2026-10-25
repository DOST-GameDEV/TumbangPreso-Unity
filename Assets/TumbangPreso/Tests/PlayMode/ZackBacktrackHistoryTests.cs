using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.PlayTests
{
    public sealed class ZackBacktrackHistoryTests
    {
        [TestCase(.5f, 1f, 45f)]
        [TestCase(1.5f, 2f, 90f)]
        [TestCase(2f, 8f, 180f)]
        public void RootReadMatchesVisiblePoseWithoutSmearingTeleport(float time, float x, float yaw)
        {
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var stage = new GameObject("Root history comparison");
            try
            {
                var track = new MatchPoseHistory.Track(null, model);
                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); track.Record(0);
                model.transform.SetPositionAndRotation(Vector3.right * 2, Quaternion.Euler(0, 90, 0)); track.Record(1);
                model.transform.SetPositionAndRotation(Vector3.right * 8, Quaternion.Euler(0, 180, 0)); track.Record(2);
                Assert.IsTrue(track.TryRootAt(time, out var position, out var rotation));
                Assert.AreEqual(x, position.x, .001f);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(0, yaw, 0), rotation), .01f);
                var copy = track.Clone(stage.transform);
                Assert.IsNotNull(copy);
                track.Apply(copy, time);
                Assert.Less(Vector3.Distance(position, copy.Root.transform.position), .001f);
                Assert.Less(Quaternion.Angle(rotation, copy.Root.transform.rotation), .01f);
            }
            finally { Object.DestroyImmediate(stage); Object.DestroyImmediate(model); }
        }
    }
}
