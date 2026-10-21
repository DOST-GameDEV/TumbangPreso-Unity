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
        public IEnumerator ChasePatienceMeasuresTimeSinceRealProgress()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var bot=Object.FindObjectsByType<AIController>(FindObjectsSortMode.None).First();
            var quarry=GameServices.Round.Players.First(p=>p!=bot.GetComponent<CharacterMotor>());
            Assert.IsFalse(quarry.IsStunned);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var chase=typeof(AIController).GetMethod("ChaseIsGoingSomewhere",flags);
            bool Progress()=>(bool)chase.Invoke(bot,new object[]{quarry});
            Time.timeScale=1;
            Assert.IsTrue(Progress());
            yield return new WaitForSeconds(AiTuning.ChasePatienceSeconds+.06f);
            Assert.IsFalse(Progress(),"The taya kept a stale chase beyond its authored patience");
            Assert.IsTrue(Progress(),"A new chase must get its own window");
            yield return new WaitForSeconds(AiTuning.ChasePatienceSeconds*.65f);
            var target=(Vector3)typeof(AIController).GetMethod("At",flags).Invoke(bot,new object[]{quarry});
            bot.GetComponent<CharacterMotor>().Teleport(Vector3.MoveTowards(bot.transform.position,target,AiTuning.ChaseProgressMetres+.2f));
            Assert.IsTrue(Progress(),"Closing distance must refresh patience");
            yield return new WaitForSeconds(AiTuning.ChasePatienceSeconds*.65f);
            Assert.IsTrue(Progress(),"Patience was measured from chase start instead of the last progress");
        }

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
