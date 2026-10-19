using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// ⚠️ THE JOIN SCREEN'S SENTENCES, ASSERTED WHERE THEY COST A MILLISECOND. The report they answer
    /// (2026-10-20): a LAN row over Hamachi, "it's stuck on joining", and the failure that followed it
    /// named the transport's envelope or said "Could not reach" over a version refusal.
    /// </summary>
    public sealed class JoinFailureRulesTests
    {
        private const string Lan = "25.17.144.85:8910";

        [Fact]
        public void TheTransportGivingUpIsNoAnswerAndNamesTheFirewall()
        {
            string line = JoinFailureRules.FailureLine(Lan,
                "disconnected: [Disconnect Event][Client-1][TransportClientId-0] Failed to connect to server.", 174);
            Assert.Equal("No answer from " + Lan + ". The host's firewall may be blocking the game.", line);
        }

        [Fact]
        public void TheSeatDeadlineIsNoAnswerToo()
        {
            string line = JoinFailureRules.FailureLine(Lan, "Connection timed out before a seat was assigned.", 174);
            Assert.StartsWith("No answer from " + Lan, line);
        }

        [Fact]
        public void ABareDisconnectIsNoAnswer() =>
            Assert.StartsWith("No answer", JoinFailureRules.FailureLine(Lan, "disconnected", 174));

        [Fact]
        public void AVersionRefusalNamesBothProtocols()
        {
            string line = JoinFailureRules.FailureLine(Lan, "disconnected: Game version mismatch (network protocol 173)", 174);
            Assert.Equal("That host runs another version (protocol 173, yours 174). Use the same build.", line);
            Assert.DoesNotContain("Could not reach", line);
        }

        [Fact]
        public void AVersionRefusalWithoutANumberStillSaysVersion() =>
            Assert.Equal("That host runs another version of the game. Use the same build.",
                JoinFailureRules.FailureLine(Lan, "disconnected: Game version mismatch", 174));

        [Fact]
        public void AFullRoomSaysFull() =>
            Assert.Equal("That room is full.",
                JoinFailureRules.FailureLine(Lan, "disconnected: This game is full: 4 players, 4 spectators", 174));

        [Fact]
        public void TheHostsOwnSentenceStands() =>
            Assert.Equal("Skill rules differ from the host. Use matching game builds.",
                JoinFailureRules.FailureLine(Lan, "disconnected: Skill rules differ from the host. Use matching game builds.", 174));

        [Fact]
        public void AnyOtherStatusKeepsTheOldHeadlineAndItsDetail()
        {
            Assert.Equal("Could not reach " + Lan + ".", JoinFailureRules.FailureLine(Lan, "", 174));
            Assert.Equal("Could not reach " + Lan + ".  (invalid relay join code)",
                JoinFailureRules.FailureLine(Lan, "invalid relay join code", 174));
        }

        [Fact]
        public void WaitingCountsWholeSecondsAndHintsOnlyAfterTheThreshold()
        {
            Assert.Equal("Joining " + Lan + "... 3 s", JoinFailureRules.WaitingLine(Lan, 3.7f));
            string late = JoinFailureRules.WaitingLine(Lan, JoinFailureRules.NoAnswerHintSeconds + 1);
            Assert.StartsWith("Joining " + Lan + "... 9 s", late);
            Assert.Contains("firewall", late);
        }

        [Fact]
        public void ARoomCodeNeverGetsTheFirewallHint()
        {
            Assert.False(JoinFailureRules.LooksLikeAddress("PURB"));
            Assert.DoesNotContain("firewall", JoinFailureRules.WaitingLine("PURB", 30));
            Assert.True(JoinFailureRules.LooksLikeAddress(Lan));
            Assert.True(JoinFailureRules.LooksLikeAddress("localhost"));
        }

        [Fact]
        public void WaitingNeverPrintsANegativeOrRunawayCount()
        {
            Assert.Equal("Joining " + Lan + "... 0 s", JoinFailureRules.WaitingLine(Lan, -2));
            Assert.Equal("Joining " + Lan + "... 0 s", JoinFailureRules.WaitingLine(Lan, float.NaN));
        }
    }
}
