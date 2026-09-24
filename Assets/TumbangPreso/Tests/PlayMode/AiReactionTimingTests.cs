using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiReactionTimingTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ReactionUsesObservedTimeNotNumberOfPlannerCalls()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var bot=Object.FindObjectsByType<AIController>(FindObjectsSortMode.None).First();
            bot.enabled=false;bot.SeatDifficulty=Difficulty.Normal;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var reaction=typeof(AIController).GetMethod("Reacted",flags);
            var personality=(AiPersonalityRoll)typeof(AIController).GetField("_self",flags).GetValue(bot);
            float lapse=(float)typeof(AIController).GetProperty("LapseScale",flags).GetValue(bot);
            float delay=AiTuning.For(Difficulty.Normal).React*personality.Nerves*lapse;
            bool Seen(string key,bool condition=true)=>(bool)reaction.Invoke(bot,new object[]{key,condition});
            try
            {
                Time.timeScale=1;
                Assert.IsFalse(Seen("timing-regression"),"The first sight must not bypass reaction time");
                // Decisions are intermittent. Two observations separated by the
                // reaction delay must not count as only two render frames.
                yield return new WaitForSeconds(delay+.06f);
                Assert.IsTrue(Seen("timing-regression"),"A continuously visible opportunity outlasted reaction time but the bot still ignored it");
                Assert.IsFalse(Seen("timing-regression",false));
                for(int i=0;i<100;i++)Assert.IsFalse(Seen("timing-regression"),"Repeated queries in one frame manufactured reaction time");
                Time.timeScale=0;
                yield return new WaitForSecondsRealtime(delay+.06f);
                Assert.IsFalse(Seen("timing-regression"),"Paused wall time advanced perception");
                Time.timeScale=1;
                yield return new WaitForSeconds(delay+.06f);
                Assert.IsTrue(Seen("timing-regression"));
            }
            finally{Time.timeScale=1;bot.enabled=true;}
        }
    }
}
