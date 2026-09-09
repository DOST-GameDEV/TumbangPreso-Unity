using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class KuroAvoidanceTests
    {
        [Test]
        public void TheLiveMawIsRecognizedAndOffersASideRoute()
        {
            var go=new GameObject("Actual Kuro footprint");
            HazardVolume.Attach(go,4,0);
            try
            {
                Vector3 from=new Vector3(-6,0,0),to=new Vector3(6,0,0);
                Assert.IsFalse(HazardMap.TryFindBlocker(from,to,1,AiTuning.HazardAvoidMargin,3,out _),
                    "The previous cap should reproduce the ignored live field.");
                Assert.IsTrue(HazardMap.TryFindBlocker(from,to,1,AiTuning.HazardAvoidMargin,
                    AiTuning.HazardAvoidMaxRadius,out var blocker));
                Vector3 heading=HazardMap.SteerAround(from,to,blocker,AiTuning.HazardAvoidMargin);
                Assert.Greater(Mathf.Abs(heading.z),.1f,"The accepted footprint still steers straight through the maw.");
                Assert.Less(blocker.Radius+AiTuning.HazardAvoidMargin,Balance.ConfinementRadius);
            }
            finally { Object.DestroyImmediate(go);HazardMap.Clear(); }
        }
    }
}
