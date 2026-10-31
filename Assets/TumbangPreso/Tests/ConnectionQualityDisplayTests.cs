using NUnit.Framework;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    public sealed class ConnectionQualityDisplayTests
    {
        [Test] public void ABriefSpikeDoesNotFlashTheWarning()
        {
            var display=new ConnectionQualityDisplay();
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Healthy,display.Advance(true,NetSession.LinkState.Poor,.3f));
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Healthy,display.Advance(true,NetSession.LinkState.Good,1));
        }
        [Test] public void SustainedMildLagBecomesYellowAndSevereTroubleEscalatesQuickly()
        {
            var display=new ConnectionQualityDisplay();display.Advance(true,NetSession.LinkState.Poor,1);
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Mild,display.Advance(true,NetSession.LinkState.Poor,.6f));
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Severe,display.Advance(true,NetSession.LinkState.Bad,.4f));
        }
        [Test] public void RecoveryMustStayHealthyBeforeTheWarningClears()
        {
            var display=new ConnectionQualityDisplay();display.Advance(true,NetSession.LinkState.Bad,.4f);
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Severe,display.Advance(true,NetSession.LinkState.Good,1));
            display.Advance(true,NetSession.LinkState.Bad,.2f);
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Severe,display.Advance(true,NetSession.LinkState.Good,2));
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Healthy,display.Advance(true,NetSession.LinkState.Good,1.1f));
        }
        [Test] public void OfflineAndInvalidFrameTimeCannotInventLag()
        {
            var display=new ConnectionQualityDisplay();display.Advance(true,NetSession.LinkState.Bad,.4f);
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Healthy,display.Advance(false,NetSession.LinkState.Bad,100));
            Assert.AreEqual(ConnectionQualityDisplay.Tier.Healthy,display.Advance(true,NetSession.LinkState.Poor,float.NaN));
        }
    }
}
