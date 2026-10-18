using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class PerformanceTimingCaptureTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void RoutineTimingWritesSamplesWithoutBinaryProfilerCapture()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Timing checks require an isolated profile.");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs",
                "performance-timing-capture", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);
            var owner = new GameObject("Inactive timing capture check");
            owner.SetActive(false);
            bool enabled = Profiler.enabled, binary = Profiler.enableBinaryLog;
            try
            {
                var review = owner.AddComponent<OwnerUiPlayerReview>();
                void Set(string name, object value) => typeof(OwnerUiPlayerReview)
                    .GetField(name, Hidden).SetValue(review, value);
                void Call(string name, params object[] arguments) => typeof(OwnerUiPlayerReview)
                    .GetMethod(name, Hidden).Invoke(review, arguments);
                Set("_folder", folder);
                Set("_performanceReview", true);
                Set("_measureMenus", true);
                Set("_deadline", float.MaxValue);
                Call("StartFrameWindow", "routine");
                Assert.IsFalse(Profiler.enabled, "Routine timings must exclude profiler collection overhead.");
                Assert.IsFalse(Profiler.enableBinaryLog);
                Call("Update");
                Call("StopFrameWindow");
                Assert.AreEqual(2, File.ReadAllLines(Path.Combine(folder, "routine-frame-times.csv")).Length,
                    "A real timing sample must remain after disabling binary collection.");
                Assert.IsTrue(File.Exists(Path.Combine(folder, "routine-frame-context.csv")));
                Assert.IsTrue(File.Exists(Path.Combine(folder, "result.json")));
                Assert.IsEmpty(Directory.GetFiles(folder, "*.raw"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Profiler.enableBinaryLog = binary;
                Profiler.enabled = enabled;
            }
        }
    }
}
