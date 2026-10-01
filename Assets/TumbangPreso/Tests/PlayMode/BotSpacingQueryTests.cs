using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using TumbangPreso.Core;

namespace TumbangPreso.PlayTests
{
    public sealed class BotSpacingQueryTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private AIController _brain;
        private IDictionary _board;
        private Func<List<float>> _read;
        private Action<float> _claim;
        private AIController Brain(int seat)
        {
            var motor = new GameObject("Spacing bot " + seat).AddComponent<CharacterMotor>();
            motor.enabled = false; motor.PlayerSlot = seat;
            var brain = motor.gameObject.AddComponent<AIController>(); brain.enabled = false;
            return brain;
        }
        private static Func<List<float>> Read(AIController brain) =>
            (Func<List<float>>)Delegate.CreateDelegate(typeof(Func<List<float>>), brain,
                typeof(AIController).GetMethod("RivalBearings", Hidden));
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _brain = Brain(1); _read = Read(_brain);
            _claim = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), _brain,
                typeof(AIController).GetMethod("Claim", Hidden));
            _board = (IDictionary)typeof(AIController).GetField("_claims", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _board.Clear();
        }
        [UnityTearDown] public IEnumerator After()
        { _board?.Clear(); yield return PlayModeWorld.Reset(); }
        private void Put(int seat, float bearing, float age = 0)
        {
            var type = typeof(AIController).GetNestedType("BearingClaim", BindingFlags.NonPublic);
            _board[seat] = Activator.CreateInstance(type, bearing, Time.time - age);
        }
        [Test] public void WarmRivalSpacingReadsAllocateNothing()
        {
            Put(0, .2f); Put(2, 1f); Put(3, -2f);
            for (int i = 0; i < 5; i++) _read();
            using (var calibration = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            { Assert.IsTrue(calibration.Valid); GC.KeepAlive(new byte[4096]); calibration.Stop(); Assert.Greater(calibration.Count, 0); }
            int allocations;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 512,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            { for (int i = 0; i < 100; i++) _read(); recorder.Stop(); allocations = recorder.Count; }
            TestContext.WriteLine("100 warm spacing queries: " + allocations + " GC.Alloc events");
            Assert.AreEqual(0, allocations);
        }
        [Test] public void OwnAndExpiredClaimsDoNotAffectRivalSpacing()
        {
            _claim(.8f); Put(0, .2f); Put(2, 1f, 1000); Put(3, -2f);
            CollectionAssert.AreEquivalent(new[] { .2f, -2f }, _read());
            Assert.AreEqual(4, _board.Count, "Reading must not mutate the shared board.");
        }
        [Test] public void RefreshAndEmptyBoardCannotReuseOldBearings()
        {
            Put(0, .2f); CollectionAssert.AreEqual(new[] { .2f }, _read());
            Put(0, 1.2f); CollectionAssert.AreEqual(new[] { 1.2f }, _read());
            _board.Clear(); Assert.IsEmpty(_read());
            Put(3, -1f); CollectionAssert.AreEqual(new[] { -1f }, _read());
        }
        [Test] public void OneBotsReadCannotOverwriteAnotherBotsCurrentQuery()
        {
            Put(0, .2f); _claim(.8f); Put(2, 1f);
            var first = _read(); var second = Read(Brain(2))();
            CollectionAssert.AreEquivalent(new[] { .2f, 1f }, first);
            CollectionAssert.AreEquivalent(new[] { .2f, .8f }, second);
        }

        private CharacterMotor RegisterRival(int seat, Vector3 at)
        {
            GameServices.Ensure();
            var motor = Brain(seat).GetComponent<CharacterMotor>();
            motor.Mode = GameMode.HeroStrike; motor.transform.position = at;
            GameServices.Round.Register(motor); return motor;
        }
        private CharacterMotor HauntReader()
        {
            var reader = _brain.GetComponent<CharacterMotor>();
            reader.Mode = GameMode.HeroStrike; reader.transform.position = Vector3.zero;
            reader.ApplyHaunted(); Assert.IsTrue(reader.IsHaunted); return reader;
        }
        [Test] public void HauntedCannotReadUnseenRivalClaims()
        {
            RegisterRival(0, Vector3.right * 10); HauntReader();
            Put(0, .2f);
            Assert.IsEmpty(_read(), "A hidden actor's live spacing claim bypassed Haunted.");
            Put(0, 1.2f);
            Assert.IsEmpty(_read(), "Moving the invisible claim must not leak a new bearing.");
        }
        [Test] public void HauntedSpacingFollowsTheSameSightBoundaryAndClearsNormally()
        {
            var rival = RegisterRival(0, Vector3.right * 7); var reader = HauntReader();
            Put(0, .2f); CollectionAssert.AreEqual(new[] { .2f }, _read());
            rival.transform.position = Vector3.right * 7.01f;
            Put(0, 1.2f); Assert.IsEmpty(_read());
            reader.ClearStatuses(); CollectionAssert.AreEqual(new[] { 1.2f }, _read());
            reader.ApplyHaunted(); Assert.IsEmpty(_read());
            reader.Mode = GameMode.Classic;
            CollectionAssert.AreEqual(new[] { 1.2f }, _read(), "Classic spacing must stay unrestricted.");
        }
        [Test] public void HauntedSpacingIncludesVisibleCompanionsButNotAbsentBodies()
        {
            RegisterRival(0, Vector3.right * 10); HauntReader();
            var companion = Brain(4).GetComponent<CharacterMotor>();
            companion.Mode = GameMode.HeroStrike; companion.transform.position = Vector3.right * 2;
            Assert.IsTrue(GameServices.Round.RegisterCompanion(companion));
            Put(4, .4f); Put(3, .3f);
            CollectionAssert.AreEqual(new[] { .4f }, _read());
            companion.gameObject.SetActive(false); Assert.IsEmpty(_read());
            companion.gameObject.SetActive(true); CollectionAssert.AreEqual(new[] { .4f }, _read());
        }
    }
}
