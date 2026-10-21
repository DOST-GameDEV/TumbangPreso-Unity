using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

namespace TumbangPreso.PlayTests
{
    public sealed class HudConfettiRandomTests
    {
        GameObject root;HudConfetti confetti;Random.State saved;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();saved=Random.state;
            root=new GameObject("Cosmetic confetti",typeof(RectTransform),typeof(CanvasRenderer));
            confetti=root.AddComponent<HudConfetti>();confetti.rectTransform.sizeDelta=new Vector2(500,300);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if(root!=null)Object.Destroy(root);yield return PlayModeWorld.Reset();Random.state=saved;
        }
        [Test] public void CosmeticBurstLeavesTheGameplayRandomSequenceUntouched()
        {
            Random.InitState(20261006);var expected=NextFour();Random.InitState(20261006);
            confetti.Burst(Color.red,Color.blue,Color.yellow);
            CollectionAssert.AreEqual(expected,NextFour(),"A HUD celebration consumed the random sequence used by ordinary bot choices.");
        }
        [UnityTest] public IEnumerator TheVisualPoolStillDrawsAndExpires()
        {
            confetti.Burst(Color.red,Color.blue,Color.yellow);Assert.IsTrue(confetti.Live);
            using(var vertices=new VertexHelper())
            {
                typeof(HudConfetti).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(confetti,new object[]{vertices});
                Assert.Greater(vertices.currentVertCount,0,"The burst no longer draws its authored pool.");
            }
            float until=Time.realtimeSinceStartup+5;
            while(confetti.Live&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsFalse(confetti.Live,"The cosmetic pool no longer retires after its lifetime.");
            using(var vertices=new VertexHelper())
            {
                typeof(HudConfetti).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(confetti,new object[]{vertices});
                Assert.AreEqual(0,vertices.currentVertCount,"Expired confetti remains drawn.");
            }
        }
        static float[] NextFour()=>new[]{Random.value,Random.value,Random.value,Random.value};
    }
}
