using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ZackCurrentHandsTests
    {
        [Test]
        public void BareHandsAddOnlyCurrentAndReactToTheTimedPower()
        {
            var go=new GameObject("Zack bare hand review");var current=new ZackCurrentHands();
            try
            {
                var arms=go.AddComponent<ViewmodelArms>();arms.EnsureBuilt();arms.SetCharacter("zack");
                Assert.IsTrue(current.Build(arms));
                var root=go.transform.Find("~HandCompanion Zack current");Assert.IsNotNull(root);
                Assert.IsEmpty(root.GetComponentsInChildren<MeshRenderer>(true),"No coins, keys, caps or other carried geometry.");
                current.Step(arms,new ViewmodelArms.HandMood{Grounded=true},.02f);
                Assert.IsFalse(root.GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled));
                current.Step(arms,new ViewmodelArms.HandMood{Grounded=true,Overclock=25},.02f);
                Assert.IsTrue(root.GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled),"The power still reads as electricity on bare hands.");
            }
            finally{current.Destroy();Object.DestroyImmediate(go);}
        }
    }
}
