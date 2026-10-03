using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ReplayPropBindingTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root, _can;
        private RoundDirector _previous, _round;
        private MatchReplayArchive _archive;
        private Action _bind;
        [SetUp] public void Before()
        {
            _previous = GameServices.Round;
            _root = new GameObject("Dormant replay prop binding"); _root.SetActive(false);
            _round = _root.AddComponent<RoundDirector>(); _archive = _root.AddComponent<MatchReplayArchive>();
            typeof(GameServices).GetProperty("Round").SetValue(null, _round);
            _can = new GameObject("Recorded can"); _can.SetActive(false); _can.transform.SetParent(_root.transform);
            _round.Lata = _can.AddComponent<Lata>();
            _bind = (Action)Delegate.CreateDelegate(typeof(Action), _archive,
                typeof(MatchReplayArchive).GetMethod("BindProps", Hidden));
        }
        [TearDown] public void After()
        {
            typeof(GameServices).GetProperty("Round").SetValue(null, _previous);
            Object.DestroyImmediate(_root);
        }
        private IList Props => (IList)typeof(MatchReplayArchive).GetField("_props", Hidden).GetValue(_archive);
        private object CanEntry()
        {
            foreach (object entry in Props)
                if ((GameObject)entry.GetType().GetField("Source").GetValue(entry) == _can) return entry;
            return null;
        }
        private static MatchPoseHistory.Track Track(object entry)
            => (MatchPoseHistory.Track)entry.GetType().GetField("Track").GetValue(entry);
        private float UnsafeAt => (float)typeof(MatchReplayArchive).GetField("_unsafeAt", Hidden).GetValue(_archive);
        [Test] public void StableBindingPreservesRecordedTrackAndSafeWindow()
        {
            _bind(); var entry = CanEntry(); Assert.IsNotNull(entry); var track = Track(entry);
            track.Record(10); track.Record(10.05f); Assert.IsTrue(track.Ready);
            float before = UnsafeAt; _bind();
            Assert.AreSame(entry, CanEntry()); Assert.AreSame(track, Track(CanEntry()));
            Assert.IsTrue(track.Ready); Assert.AreEqual(10f, track.Oldest);
            Assert.AreEqual(before, UnsafeAt);
        }
        [Test] public void ReplacingVisualModelStartsANewTrackAndRefusesOldWindow()
        {
            _bind(); var original = Track(CanEntry());
            var visual = new GameObject("Visual"); visual.transform.SetParent(_can.transform);
            _bind(); var next = Track(CanEntry());
            Assert.AreNotSame(original, next); Assert.AreSame(visual, next.Source);
            Assert.IsFalse(next.Ready); Assert.GreaterOrEqual(UnsafeAt, 0);
        }
        [Test] public void RemovedAndReturningCanCannotReuseTheDiscardedTrack()
        {
            _bind(); var original = Track(CanEntry()); var can = _round.Lata;
            _round.Lata = null; _bind(); Assert.IsNull(CanEntry()); Assert.GreaterOrEqual(UnsafeAt, 0);
            _round.Lata = can; _bind(); Assert.IsNotNull(CanEntry());
            Assert.AreNotSame(original, Track(CanEntry()));
        }
        [Test] public void MeasureWarmedBindingManagedAllocationAndCost()
        {
            _bind(); var entry = CanEntry(); var track = Track(entry);
            for (int i=0;i<100;i++) _bind();
            const int calls=2000, batches=7; var micros=new double[batches]; var bytes=new double[batches];
            for (int batch=0;batch<batches;batch++)
            {
                long allocated=GC.GetAllocatedBytesForCurrentThread(), began=Stopwatch.GetTimestamp();
                for (int i=0;i<calls;i++) _bind();
                micros[batch]=(Stopwatch.GetTimestamp()-began)*1000000.0/Stopwatch.Frequency/calls;
                bytes[batch]=(GC.GetAllocatedBytesForCurrentThread()-allocated)/(double)calls;
            }
            Array.Sort(micros); Array.Sort(bytes);
            TestContext.Progress.WriteLine(FormattableString.Invariant(
                $"REPLAY_BIND median_us={micros[3]:F6}; managed_bytes_per_call={bytes[3]:F3}; calls={calls}; batches={batches}"));
            Assert.AreSame(entry, CanEntry()); Assert.AreSame(track, Track(CanEntry()));
            Assert.GreaterOrEqual(bytes[3], 0); Assert.Greater(micros[3], 0);
        }
    }
}
