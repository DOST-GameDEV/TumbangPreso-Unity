using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// § THE SEAT IS ANNOUNCED TWICE BY TWO PROTOCOLS, SO APPLYING IT MUST BE IDEMPOTENT.
    ///
    /// ⚠️⚠️ THE COUNT IS THE ASSERTION, NOT THE SEAT NUMBER. Every one of these tests would pass
    /// against the broken code if it only checked `LocalSlot`: the seat was always CORRECT, it
    /// was just applied over and over. Each application makes `MatchInstaller` move the camera,
    /// the HUD and the input reader onto the chair again, and measured on two real peers over a
    /// loopback transport ONE join produced six of them. That is what a joining player saw as
    /// the view snapping about. 🧑 2026-08-28: *"when a non host player tries to join, it just
    /// bounces back and forth a lot of times"*.
    /// </summary>
    public class SeatAnnouncementTests
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private NetSession _net;
        private int _seatingChanges;

        /// ⚠️ `NetSession` IS A `DontDestroyOnLoad` SINGLETON, SO IT SURVIVES BETWEEN TESTS AND
        /// THE SEAT STATE HAS TO BE CLEARED BY HAND. Without this the second test in the file
        /// starts with the first one's seat already applied, its own first announcement is
        /// correctly deduplicated away, and the test fails for a reason that has nothing to do
        /// with the code under test. `Stop()` is the product's own reset and clears both
        /// `LocalSlot` and the applied flag; it is safe on a session that was never listening.
        [SetUp]
        public void SetUp()
        {
            _net = NetSession.Ensure();
            _net.Stop();
            _seatingChanges = 0;
            _net.SeatingChanged += Count;
        }

        [TearDown]
        public void TearDown()
        {
            if (_net != null) _net.SeatingChanged -= Count;
        }

        private void Count() => _seatingChanges++;

        [UnityTest]
        public IEnumerator OneSeatAssignmentRaisesOneSeatingChange()
        {
            yield return null;

            _net.ApplyAssignedSeat(1);

            Assert.AreEqual(1, _seatingChanges,
                "ApplyAssignedSeat raised SeatingChanged more than once for a single assignment");
            Assert.AreEqual(1, _net.LocalSlot);
            Assert.IsFalse(GameLaunch.Spectator);
        }

        /// <summary>
        /// The exact shape the wire produces: `tp.seat.assignment.v1` lands first and `Seating`
        /// repeats the same chair a moment later. See `NetSession.ApplyAssignedSeat`.
        /// </summary>
        [UnityTest]
        public IEnumerator RepeatingTheSameSeatAcrossBothProtocolsRebuildsOnce()
        {
            yield return null;

            _net.ApplyAssignedSeat(1);          // tp.seat.assignment.v1
            _net.SetLocalSeating(1, false);     // Seating, same chair
            _net.ApplyAssignedSeat(1);          // a repeat of the first
            _net.SetLocalSeating(1, false);     // and of the second

            Assert.AreEqual(1, _seatingChanges,
                "four announcements of ONE seat must rebuild the local seat once, not four times");
            Assert.AreEqual(1, _net.LocalSlot);
        }

        /// <summary>⚠️ THE DEDUPLICATION MUST NOT SWALLOW A REAL MOVE, which is the obvious way
        /// to get this wrong: a seat swap and a drop to spectator are both genuine changes.</summary>
        [UnityTest]
        public IEnumerator AGenuineSeatChangeStillRebuilds()
        {
            yield return null;

            _net.ApplyAssignedSeat(1);
            _net.SetLocalSeating(2, false);
            _net.SetLocalSeating(-1, true);

            Assert.AreEqual(3, _seatingChanges, "each real change must rebuild the local seat");
            Assert.AreEqual(-1, _net.LocalSlot);
            Assert.IsTrue(GameLaunch.Spectator);
        }

        /// <summary>
        /// ⚠️⚠️ SEAT 0 IS A REAL SEAT AND `LocalSlot` DEFAULTS TO 0, so "has a seat been applied"
        /// cannot be answered by the seat number alone. Without the separate flag the host's very
        /// first announcement of seat 0 looks like a no-op and the arena is never told to wire
        /// anything up: the whole screen stays dead. This is the test that catches that.
        /// </summary>
        [UnityTest]
        public IEnumerator TheFirstAnnouncementOfSeatZeroIsNotSwallowed()
        {
            yield return null;

            Assert.AreEqual(0, _net.LocalSlot, "precondition: LocalSlot starts at 0");

            _net.ApplyAssignedSeat(0);

            Assert.AreEqual(1, _seatingChanges,
                "seat 0 is a real seat; its first announcement must rebuild the local seat");
        }
    }
}
