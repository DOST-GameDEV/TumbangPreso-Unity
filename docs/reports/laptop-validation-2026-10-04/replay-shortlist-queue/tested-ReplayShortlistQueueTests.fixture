using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ReplayShortlistQueueTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const long Match = 101;
        private const ulong Peer = 7;
        private GameObject _routerRoot, _archiveRoot;
        private MatchRpc _rpc;
        private MatchReplayArchive _archive;

        [SetUp] public void Before()
        {
            Assert.IsNull(Object.FindAnyObjectByType<MatchReplayArchive>(), "Use an empty native EditMode scene without replacing a live archive.");
            _routerRoot = new GameObject("Dormant replay queue receiver");
            _routerRoot.SetActive(false);
            _rpc = _routerRoot.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(_rpc, Match);
            Field(_rpc, "_clipEpoch").SetValue(_rpc, Match);
            _archiveRoot = new GameObject("Current authoritative replay shortlist");
            _archive = _archiveRoot.AddComponent<MatchReplayArchive>();
            _archive.enabled = false;
            Assert.AreSame(_archive, Object.FindAnyObjectByType<MatchReplayArchive>());
        }

        [TearDown] public void After()
        {
            if (_archiveRoot != null) Object.DestroyImmediate(_archiveRoot);
            if (_routerRoot != null) Object.DestroyImmediate(_routerRoot);
        }

        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, Hidden);
        private IDictionary Queues => (IDictionary)Field(_rpc, "_clipSends").GetValue(_rpc);
        private object[] Sends(ulong peer = Peer) => Queues.Contains(peer) ? ((IEnumerable)Queues[peer]).Cast<object>().ToArray() : Array.Empty<object>();
        private static long Id(object send) => ((MatchReplayArchive.Retained)send.GetType().GetField("Retained").GetValue(send)).Clip.Id;
        private long[] Ids(ulong peer = Peer) => Sends(peer).Select(Id).ToArray();

        private static MatchReplayArchive.Retained Clip(long id, long match = Match) =>
            (MatchReplayArchive.Retained)Activator.CreateInstance(typeof(MatchReplayArchive.Retained), Hidden,
                null, new object[] { new RecordedMatchClip { MatchId = match, Id = id }, new byte[8], (int)id }, null);

        private void Queue(long id, ulong peer = Peer) => typeof(MatchRpc).GetMethod("QueueReplay", Hidden)
            .Invoke(_rpc, new object[] { peer, Clip(id) });

        private void Shortlist(params long[] ids)
        {
            var clips = (IList)Field(_archive, "_clips").GetValue(_archive);
            clips.Clear();
            foreach (long id in ids) clips.Add(Clip(id));
        }

        private void Refresh() => typeof(MatchRpc).GetMethod("SendReplayShortlist", Hidden).Invoke(_rpc, new object[] { Peer });

        private object BeginHead(int offset)
        {
            var head = Sends()[0];
            head.GetType().GetField("Began").SetValue(head, true);
            head.GetType().GetField("Offset").SetValue(head, offset);
            return head;
        }

        [Test] public void SaturatedQueueAdmitsTheNewPreferredClipAndDropsObsoleteFootage()
        {
            Queue(1); Queue(2); Queue(3); Shortlist(4, 3, 2);
            Refresh();
            CollectionAssert.AreEqual(new long[] { 4, 3, 2 }, Ids());
            Assert.LessOrEqual(Sends().Length, MatchReplayArchive.Capacity);
        }

        [Test] public void AnObsoleteStartedClipCannotBlockTheCurrentShortlist()
        {
            Queue(1); Queue(2); Queue(3); var obsolete = BeginHead(256); Shortlist(4, 3, 2);
            Refresh();
            CollectionAssert.AreEqual(new long[] { 4, 3, 2 }, Ids());
            Assert.IsFalse(Sends().Contains(obsolete));
        }

        [Test] public void AStillRetainedStartedClipKeepsItsProgressBeforeNewPriorityFootage()
        {
            Queue(2); Queue(1); Queue(3); var active = BeginHead(256); Shortlist(4, 3, 2);
            Refresh();
            CollectionAssert.AreEqual(new long[] { 2, 4, 3 }, Ids());
            Assert.AreSame(active, Sends()[0]);
            Assert.AreEqual(256, active.GetType().GetField("Offset").GetValue(active));
            Assert.IsTrue((bool)active.GetType().GetField("Began").GetValue(active));
        }

        [Test] public void ANewAudienceReceivesTheExistingArchivePriorityOrder()
        {
            Shortlist(4, 3, 2); Refresh();
            CollectionAssert.AreEqual(new long[] { 4, 3, 2 }, Ids());
        }

        [Test] public void UnchangedShortlistKeepsQueueObjectsAndAnotherPeersWork()
        {
            Queue(3); Queue(2); Queue(1); Queue(8, 9); Shortlist(3, 2, 1);
            var before = Sends(); var other = Sends(9);
            Refresh();
            CollectionAssert.AreEqual(before, Sends());
            CollectionAssert.AreEqual(other, Sends(9));
        }

        [Test] public void AlreadyAcknowledgedFootageIsNotQueuedAgain()
        {
            ((IDictionary)Field(_rpc, "_clipReady").GetValue(_rpc))[Peer] = new HashSet<long> { 4 };
            Shortlist(4, 3, 2); Refresh();
            CollectionAssert.AreEqual(new long[] { 3, 2 }, Ids());
        }

        [Test] public void APreviousMatchCannotSupplyTheNewMatchesTransferQueue()
        {
            ((IList)Field(_archive, "_clips").GetValue(_archive)).Add(Clip(4, Match - 1));
            Refresh();
            Assert.IsEmpty(Ids());
        }
    }
}
