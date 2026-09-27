using System.Collections;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class GeneratedMotionAssetsTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(30000)]
        public IEnumerator BakedMotionWarmupRetainsTheExactAssetsWithoutCreatingActors()
        {
            var model = RosterBook.Load().FindPersonArt("paete").Model;
            Assert.IsNotNull(model);
            var animator = model.GetComponentInChildren<Animator>();
            string rig = DanceClip.ResourceName(animator != null ? animator.transform : model.transform);
            Assert.IsNotEmpty(rig);
            int actors = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length;
            int graphs = Object.FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None).Length;
            yield return GeneratedMotionAssets.Warmup(model);
            var loaded = GeneratedMotionAssets.For(RootedMotion.Folder, rig);
            Assert.IsNotNull(loaded);
            Assert.AreSame(Resources.Load<GeneratedAnimationSet>(RootedMotion.Folder + "/" + rig), loaded);
            Assert.IsNotEmpty(loaded.Clips);
            yield return GeneratedMotionAssets.Warmup(model);
            Assert.AreSame(loaded, GeneratedMotionAssets.For(RootedMotion.Folder, rig));
            Assert.AreEqual(actors, Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(graphs, Object.FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None).Length);
            Assert.IsNull(GeneratedMotionAssets.For(RootedMotion.Folder, null));
        }
    }
}
