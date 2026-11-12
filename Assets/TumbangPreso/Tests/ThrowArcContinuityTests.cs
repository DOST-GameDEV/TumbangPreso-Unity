using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ThrowArcContinuityTests
    {
        private const float FullSpeed = Balance.LaunchSpeed;

        [Test]
        public void LaunchAngleStaysContinuousAcrossTheRangeEdge()
        {
            var origin = new Vector3(0, 1.6f, 0);
            // Sweep the sight line from well below the horizon to above it, which
            // walks the aim point through the range edge the same way a crosshair does.
            for (float elevation = -20; elevation <= 25; elevation += 5)
            {
                Vector3 previous = default;
                bool crossed = false, wasInRange = true;
                for (float distance = 2; distance <= 60; distance += .005f)
                {
                    var line = Quaternion.AngleAxis(-elevation, Vector3.right) * Vector3.forward;
                    var target = origin + line * distance;
                    var launch = Slipper.SolveArc(origin, target, FullSpeed);
                    bool inRange = InRange(target - origin, FullSpeed);
                    if (distance > 2)
                        Assert.Less(Vector3.Angle(previous, launch), 1.5f,
                            $"Launch jumped at elevation {elevation} and distance {distance:0.00}.");
                    crossed |= wasInRange && !inRange;
                    wasInRange = inRange;
                    previous = launch;
                }
                Assert.IsTrue(crossed, $"Elevation {elevation} never crossed the range edge.");
            }
        }

        [Test]
        public void OutOfRangeThrowsAtTheFarthestReachingAngle()
        {
            var origin = new Vector3(0, 1.6f, 0);
            foreach (float elevation in new[] { -10f, 0f, 15f })
            {
                var line = Quaternion.AngleAxis(-elevation, Vector3.right) * Vector3.forward;
                var launch = Slipper.SolveArc(origin, origin + line * 200, FullSpeed);
                float pitch = Mathf.Asin(launch.y) * Mathf.Rad2Deg;
                Assert.That(pitch, Is.EqualTo(45 + elevation * .5f).Within(.01f));
                Assert.AreEqual(0, launch.x, 1e-5f, "Out of range throws keep the aim heading.");
            }
        }

        private static bool InRange(Vector3 toTarget, float speed)
        {
            float distance = new Vector2(toTarget.x, toTarget.z).magnitude, v2 = speed * speed, g = Balance.Gravity;
            return v2 * v2 - g * (g * distance * distance + 2 * toTarget.y * v2) >= 0;
        }
    }
}
