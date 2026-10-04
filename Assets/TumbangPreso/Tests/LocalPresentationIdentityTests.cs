using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class LocalPresentationIdentityTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private MatchDirector _match;
        private Action _begin;
        [SetUp] public void Before()
        {
            Assert.IsFalse(NetAuthority.IsNetworked);
            _root = new GameObject("Dormant local presentation identity"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>();
            _begin = (Action)Delegate.CreateDelegate(typeof(Action), _match,
                typeof(MatchDirector).GetMethod("BeginPresentationMatch", Hidden));
        }
        [TearDown] public void After() => Object.DestroyImmediate(_root);
        [Test] public void LocalRestartAdvancesEvenWhenWallClockIsBehindPreviousIdentity()
        {
            long previous = DateTime.UtcNow.AddMinutes(1).Ticks;
            typeof(MatchDirector).GetProperty("PresentationMatchId").SetValue(_match, previous);
            _begin(); Assert.Greater(_match.PresentationMatchId, previous);
        }
        [Test] public void RapidRepeatedLocalStartsAlwaysHaveDistinctIdentities()
        {
            _begin(); long previous = _match.PresentationMatchId;
            for (int i = 0; i < 32; i++)
            {
                _begin(); Assert.Greater(_match.PresentationMatchId, previous);
                previous = _match.PresentationMatchId;
            }
        }
        [Test] public void CurrentLocalStartStillResetsMomentBookkeeping()
        {
            foreach (string field in new[] { "_momentSequence", "_receivedMomentSequence" })
                typeof(MatchDirector).GetField(field, Hidden).SetValue(_match, 7L);
            typeof(MatchDirector).GetField("_firstKnockdownRound", Hidden).SetValue(_match, 3);
            _begin(); Assert.Greater(_match.PresentationMatchId, 0);
            Assert.AreEqual(0L, typeof(MatchDirector).GetField("_momentSequence", Hidden).GetValue(_match));
            Assert.AreEqual(0L, typeof(MatchDirector).GetField("_receivedMomentSequence", Hidden).GetValue(_match));
            Assert.AreEqual(0, typeof(MatchDirector).GetField("_firstKnockdownRound", Hidden).GetValue(_match));
        }
    }
}
