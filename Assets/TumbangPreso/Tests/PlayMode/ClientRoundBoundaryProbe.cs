using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ClientRoundBoundaryProbe
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator ClientClockExpiryDoesNotStartItsOwnIntermission()
        {
            GameServices.Ensure();var previous=NetAuthority.Provider;
            try
            {
                NetAuthority.Provider=new ClientProvider();
                var match=GameServices.Match;var round=GameServices.Round;
                match.ResetForNewMatch();round.ResetForNewMatch();
                match.ApplySnapshot(new[]{0,0,0,0},1,true);
                round.ApplySnapshot(.001f,true,0,true);
                int intermissions=0;System.Action<int,int> changed=(a,b)=>intermissions++;
                match.IntermissionStarted+=changed;
                try
                {
                    yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                    Debug.Log("[ClientRoundBoundary] local expiry: warmup="+match.IsWarmupBuffer+" active="+round.RoundActive+" events="+intermissions);
                    Assert.AreEqual(0,intermissions,"A client clock fired the authoritative intermission event");
                    Assert.False(match.IsWarmupBuffer,"Local expiry invented warm-up state before the host snapshot");
                    Assert.True(round.RoundActive,"A client stopped the live round without host state");
                    Assert.AreEqual(0,round.TimeLeft,.001f,"The presentation clock should stop at zero while awaiting the host");
                }
                finally{match.IntermissionStarted-=changed;}
            }
            finally{NetAuthority.Provider=previous;}
        }
        [UnityTest]
        public IEnumerator HostClockStillStartsOneIntermission()
        {
            GameServices.Ensure();
            var previous = NetAuthority.Provider;
            var match = GameServices.Match;
            var round = GameServices.Round;
            int intermissions = 0;
            System.Action<int, int> changed = (a, b) => intermissions++;
            try
            {
                NetAuthority.Provider = null;
                Assert.True(NetAuthority.ShouldResolve());
                match.ResetForNewMatch();
                round.ResetForNewMatch();
                match.ApplySnapshot(new[] { 0, 0, 0, 0 }, 1, true);
                round.ApplySnapshot(.001f, true, 0, true);
                match.IntermissionStarted += changed;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, intermissions);
                Assert.True(match.IsWarmupBuffer);
                Assert.False(round.RoundActive);
                Assert.AreEqual(0, round.TimeLeft);
                Assert.AreEqual(1, match.RoundNumber);
            }
            finally
            {
                match.IntermissionStarted -= changed;
                NetAuthority.Provider = previous;
            }
        }

        [UnityTest]
        public IEnumerator AcceptedSnapshotsRestoreLateJoinBufferAndClearItForLiveRound()
        {
            GameServices.Ensure();
            var previous = NetAuthority.Provider;
            var match = GameServices.Match;
            var round = GameServices.Round;
            int worldEvents = 0;
            System.Action<int, int> changed = (a, b) => worldEvents++;
            try
            {
                NetAuthority.Provider = new ClientProvider();
                match.ResetForNewMatch();
                round.ResetForNewMatch();
                match.IntermissionStarted += changed;
                match.RoundStarted += changed;
                // Same order as the accepted SyncWorld path, without a local live-round edge.
                match.ApplySnapshot(new[] { 10, 20, 30, 40 }, 1, true);
                round.ApplySnapshot(0, false, 0, true);
                Assert.True(match.IsWarmupBuffer, "A late join must see the actual host buffer");
                round.ApplySnapshot(0, false, 0, true);
                yield return null;
                match.ApplySnapshot(new[] { 10, 20, 30, 40 }, 2, true);
                round.ApplySnapshot(30, true, 1, true);
                Assert.False(match.IsWarmupBuffer, "The new live round must clear stale warm-up");
                Assert.True(round.RoundActive);
                Assert.AreEqual(2, match.RoundNumber);
                Assert.AreEqual(30, match.ScoreFor(2));
                Assert.AreEqual(0, worldEvents, "Snapshot hydration must not reset the world");
                // Real match end also clears a stale buffer.
                match.IsWarmupBuffer = true;
                match.ApplySnapshot(new[] { 10, 20, 30, 40 }, 2, false);
                round.ApplySnapshot(0, false, 1, false);
                Assert.False(match.IsWarmupBuffer);
                // Pre-match snapshots are not an intermission.
                match.ResetForNewMatch();
                match.IsWarmupBuffer = true;
                match.ApplySnapshot(new[] { 0, 0, 0, 0 }, 0, false);
                round.ApplySnapshot(30, false, 0, false);
                Assert.False(match.IsWarmupBuffer);
            }
            finally
            {
                match.IntermissionStarted -= changed;
                match.RoundStarted -= changed;
                NetAuthority.Provider = previous;
            }
        }

        private sealed class ClientProvider:INetProvider
        {
            public bool IsHost=>false;public bool IsNetworked=>true;
            public int LocalSlot=>1;public int LocalPeerId=>2;public bool IsSeatlessReferee=>false;
        }
    }
}
