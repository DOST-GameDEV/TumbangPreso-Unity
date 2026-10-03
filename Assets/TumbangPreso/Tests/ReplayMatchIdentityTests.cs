using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ReplayMatchIdentityTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root, _can;
        private MatchDirector _match, _previousMatch;
        private RoundDirector _round, _previousRound;
        private MatchPoseHistory _history;
        private MatchReplayArchive _archive;
        private Action _beginIdentity, _sampleBodies, _checkArchive, _bindProps;

        [SetUp] public void Before()
        {
            Assert.IsFalse(NetAuthority.IsNetworked, "This dormant fixture must not use a live session");
            _previousMatch = GameServices.Match; _previousRound = GameServices.Round;
            _root = new GameObject("Dormant replay match boundary"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>(); _round = _root.AddComponent<RoundDirector>();
            _history = _root.AddComponent<MatchPoseHistory>(); _archive = _root.AddComponent<MatchReplayArchive>();
            typeof(GameServices).GetProperty("Match").SetValue(null, _match);
            typeof(GameServices).GetProperty("Round").SetValue(null, _round);
            _beginIdentity = Method(_match, "BeginPresentationMatch");
            _sampleBodies = Method(_history, "LateUpdate"); _checkArchive = Method(_archive, "Update");
            _bindProps = Method(_archive, "BindProps");
            _beginIdentity(); _match.ApplySnapshot(new int[4], 1, true);
            _sampleBodies(); _checkArchive();
            _can = new GameObject("Recorded can"); _can.SetActive(false); _can.transform.SetParent(_root.transform);
            _round.Lata = _can.AddComponent<Lata>();
        }

        [TearDown] public void After()
        {
            typeof(GameServices).GetProperty("Match").SetValue(null, _previousMatch);
            typeof(GameServices).GetProperty("Round").SetValue(null, _previousRound);
            Object.DestroyImmediate(_root);
        }

        private static Action Method(object target, string name) => (Action)Delegate.CreateDelegate(
            typeof(Action), target, target.GetType().GetMethod(name, Hidden));
        private object Read(object target, string name) => target.GetType().GetField(name, Hidden).GetValue(target);
        private void Write(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
        private IList Props => (IList)Read(_archive, "_props");
        private IList Fields => (IList)Read(_archive, "_fields");
        private IDictionary FieldIds => (IDictionary)Read(_archive, "_fieldIds");
        private void ChangeIdentity()
        {
            long old = _match.PresentationMatchId; _beginIdentity();
            Assert.AreNotEqual(old, _match.PresentationMatchId, "A new local match has a new presentation identity");
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void BodyHistoryFollowsMatchAndRoundBoundary(bool newMatch, bool newRound)
        {
            var track = new MatchPoseHistory.Track(null, _can); track.Record(10); track.Record(10.05f);
            ((MatchPoseHistory.Track[])Read(_history, "_tracks"))[0] = track;
            Assert.IsTrue(_history.ForSeat(0).Ready);
            if (newMatch) ChangeIdentity();
            if (newRound) _match.ApplySnapshot(new int[4], 2, true);
            _sampleBodies(); // Round is dormant: only the real boundary logic runs.
            if (newMatch || newRound) Assert.IsNull(_history.ForSeat(0), "A prior round/match cannot supply a new highlight's lead-in");
            else { Assert.AreSame(track, _history.ForSeat(0)); Assert.AreEqual(10, track.Oldest); }
        }

        private MatchReplayArchive.Retained SeedArchive()
        {
            _bindProps(); Assert.AreEqual(1, Props.Count);
            var track = (MatchPoseHistory.Track)Props[0].GetType().GetField("Track").GetValue(Props[0]);
            track.Record(10); track.Record(10.05f); Assert.IsTrue(track.Ready);
            Fields.Add(new RecordedFieldFrame { Time = 10 }); FieldIds.Add(_can, 7);
            Write(_archive, "_fieldSequence", 7); Write(_archive, "_unsafeAt", 10f);
            var bytes = new byte[] { 1, 2, 3 };
            var retained = (MatchReplayArchive.Retained)Activator.CreateInstance(typeof(MatchReplayArchive.Retained),
                Hidden, null, new object[] { new RecordedMatchClip(), bytes, 1 }, null);
            ((IList)Read(_archive, "_clips")).Add(retained);
            return retained;
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void LiveArchiveHistoryResetsButSameMatchShortlistSurvivesRounds(bool newMatch, bool newRound)
        {
            var retained = SeedArchive(); var prop = Props[0]; var field = Fields[0];
            if (newMatch) ChangeIdentity();
            if (newRound) _match.ApplySnapshot(new int[4], 2, true);
            _checkArchive();
            if (newMatch || newRound)
            {
                Assert.IsEmpty(Props, "Previous match props cannot carry old pose samples into a fresh round 1");
                Assert.IsEmpty(Fields, "Previous match fields cannot appear in a fresh round 1 highlight");
                Assert.IsEmpty(FieldIds); Assert.AreEqual(0, Read(_archive, "_fieldSequence"));
                Assert.AreEqual(-100f, Read(_archive, "_unsafeAt"));
            }
            else { Assert.AreSame(prop, Props[0]); Assert.AreSame(field, Fields[0]); Assert.AreEqual(7, FieldIds[_can]); }
            if (newMatch) Assert.IsEmpty(_archive.Clips, "New match retires the previous shortlist");
            else Assert.AreSame(retained, _archive.Clips[0], "Ordinary rounds preserve detached highlights");
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, retained.Bytes, "Boundary cleanup cannot mutate detached data");
        }
    }
}
