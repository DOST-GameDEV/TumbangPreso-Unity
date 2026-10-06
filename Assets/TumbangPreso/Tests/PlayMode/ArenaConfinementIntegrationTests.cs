using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using UnityEngine;
namespace TumbangPreso.PlayTests
{
    public sealed class ArenaConfinementIntegrationTests
    {
        private bool round; private float radius;
        [SetUp] public void Before() { round=Confinement.Round; radius=Confinement.Radius; }
        [TearDown] public void After() { Confinement.Use(round,radius); }
        [TestCase("plaza")][TestCase("tore")][TestCase("krus")][TestCase("hukay")][TestCase("entablado")]
        public void ThrowAndPlantRulesAgreeAtEveryBearingOfTheLayoutsLine(string layout)
        {
            Confinement.Use(true,ArenaStage.BoxRadiusFor(layout));
            for(int angle=0;angle<360;angle+=15)
            {
                var direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                var c=ThrowContext.Default(); c.X=direction.x*(Confinement.Radius+.1f); c.Z=direction.y*(Confinement.Radius+.1f);
                Assert.IsTrue(ThrowRules.CanThrow(c),layout+" outside "+angle);
                c.X=direction.x*(Confinement.Radius-.1f); c.Z=direction.y*(Confinement.Radius-.1f);
                Assert.IsFalse(ThrowRules.CanThrow(c),layout+" inside "+angle);
                float x=direction.x*(Confinement.Radius+2),z=direction.y*(Confinement.Radius+2);
                Confinement.ClampToBox(ref x,ref z);
                Assert.AreEqual(Confinement.Radius,new Vector2(x,z).magnitude,.001f);
                x=c.X;z=c.Z;PaeteRules.PlantSpotOutsideBox(ref x,ref z);
                Assert.IsFalse(Confinement.IsInsideBox(x,z));
                Assert.AreEqual(Confinement.Radius+PaeteRules.PlantBoxMargin,new Vector2(x,z).magnitude,.001f);
            }
        }
        [Test] public void LeavingArenaRestoresTheSquareAndItsDiagonalRule()
        {
            Confinement.Use(true,9.3f); Confinement.Reset();
            Assert.IsFalse(Confinement.Round);
            Assert.AreEqual(Balance.ConfinementRadius,Confinement.Radius);
            Assert.IsTrue(Confinement.IsInsideBox(6.9f,6.9f));
            var c=ThrowContext.Default();c.X=6.9f;c.Z=6.9f;
            Assert.IsFalse(ThrowRules.CanThrow(c));
        }
    }
}
