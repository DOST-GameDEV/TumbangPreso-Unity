using System.IO;
using NUnit.Framework;

namespace TumbangPreso.Tests
{
    public sealed class ClientCueAuthorityTests
    {
        [TestCase("bump_swing",true)]
        [TestCase("slide_scrape",true)]
        [TestCase("throw_charge",true)]
        [TestCase("jump",true)]
        [TestCase("land",true)]
        [TestCase("score_award",false)]
        [TestCase("lata_knockdown",false)]
        [TestCase("hero_sean_ult",false)]
        [TestCase("match_win",false)]
        [TestCase("ui_error",false)]
        public void ClientsCannotUseTheAudioRequestToImpersonateAnOutcome(string cue,bool allowed)
            => Assert.AreEqual(allowed,NetCue.ClientMayRelay(cue));

        [Test]
        public void TheReceivePathChecksCueOwnershipAndPositionBeforePlayingIt()
        {
            string source=File.ReadAllText("Assets/TumbangPreso/Runtime/Net/MatchRpc.cs");
            int start=source.IndexOf("private void OnReqCueMsg");
            int play=source.IndexOf("GameServices.Audio?.PlayAtVaried(id",start);
            string before=source.Substring(start,play-start);
            StringAssert.Contains("NetCue.ClientMayRelay(id)",before);
            StringAssert.Contains("TrySenderSeat(senderClientId,out int cueSeat)",before);
            StringAssert.Contains("PlausibleIntentPose(Unit(cueSeat),position)",before);
        }
    }
}
