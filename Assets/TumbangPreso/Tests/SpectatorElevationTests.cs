using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorElevationTests
    {
        private GameObject _root;
        private SpectatorDirector _director;
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        [SetUp] public void Before()
        {
            _root = new GameObject("Elevated spectator shot"); _root.SetActive(false);
            _director = _root.AddComponent<SpectatorDirector>();
        }
        [TearDown] public void After() { if (_root != null) Object.DestroyImmediate(_root); }
        [Test] public void RaisingTheCourtRaisesTheComposedEyeByTheSameAmount()
        {
            var solve = typeof(SpectatorDirector).GetMethod("Solve", Hidden);
            Vector3 Eye(Vector3 focus)
            {
                object[] args = { focus, 3f, 35f, Vector3.zero };
                solve.Invoke(_director, args); return (Vector3)args[3];
            }
            Vector3 low = Eye(new Vector3(2, 1.5f, -3));
            Vector3 raised = Eye(new Vector3(2, 21.5f, -3));
            Assert.That(raised.y - low.y, Is.EqualTo(20f).Within(.001f),
                "The automatic camera stayed at world-floor height while its subjects moved to an elevated court.");
            Assert.That(raised.x, Is.EqualTo(low.x).Within(.001f));
            Assert.That(raised.z, Is.EqualTo(low.z).Within(.001f));
        }
        [Test] public void TheFallbackAlsoMovesWithTheCourt()
        {
            var safe = typeof(SpectatorDirector).GetMethod("SafePose", BindingFlags.NonPublic | BindingFlags.Static);
            var low = (Vector3)safe.Invoke(null, new object[] { new Vector3(2, 1.5f, -3) });
            var raised = (Vector3)safe.Invoke(null, new object[] { new Vector3(2, 21.5f, -3) });
            Assert.That(raised.y - low.y, Is.EqualTo(20f).Within(.001f),
                "A rejected shot fell back below an elevated map.");
        }
    }
}
