using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class RenderCopyAnimationSamplingTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest,Timeout(180000)]
        public IEnumerator CurrentRigCopiesSampleRootedClipsWithoutAutomaticPoseChangesOrGameplayComponents()
        {
            int sampled=0;
            foreach(var hero in Core.Roster.HeroPeople)
            {
                var model=RosterBook.Load().FindPersonArt(hero.Id).Model;
                yield return GeneratedMotionAssets.Warmup(model);
                var originalAnimator=model.GetComponentInChildren<Animator>();
                Assert.IsNotNull(originalAnimator);
                var track=new MatchPoseHistory.Track(null,model);
                track.Record(0);track.Record(.05f);
                var stage=new GameObject("RenderCopySamplingFixture");stage.SetActive(false);
                try
                {
                    var copy=track.Clone(stage.transform);Assert.IsNotNull(copy);
                    track.Apply(copy,track.Newest);
                    var root=track.CopiedBone(copy,originalAnimator.transform);Assert.IsNotNull(root);
                    var sampler=root.GetComponent<Animator>();Assert.IsNotNull(sampler);
                    Assert.IsFalse(sampler.enabled);Assert.IsFalse(sampler.applyRootMotion);
                    Assert.IsNull(sampler.runtimeAnimatorController);
                    Assert.IsEmpty(stage.GetComponentsInChildren<MonoBehaviour>(true));
                    var rig=DanceClip.ResourceName(originalAnimator.transform);
                    var set=GeneratedMotionAssets.For(RootedMotion.Folder,rig);Assert.IsNotNull(set);
                    stage.SetActive(true);
                    foreach(var clip in set.Clips)
                    {
                        track.Apply(copy,track.Newest);
                        var nodes=copy.Bones;var rotations=nodes.Select(x=>x.localRotation).ToArray();
                        var positions=nodes.Select(x=>x.localPosition).ToArray();
                        clip.SampleAnimation(root.gameObject,clip.length*.37f);
                        Assert.IsTrue(Enumerable.Range(0,nodes.Length).Any(index=>Quaternion.Angle(rotations[index],nodes[index].localRotation)>.1f
                            || (positions[index]-nodes[index].localPosition).sqrMagnitude>1e-7f),hero.Id+" / "+clip.name);
                        var heldRotations=nodes.Select(x=>x.localRotation).ToArray();
                        var heldPositions=nodes.Select(x=>x.localPosition).ToArray();
                        yield return null;yield return null;
                        for(int index=0;index<nodes.Length;index++)
                        {
                            Assert.Less(Quaternion.Angle(heldRotations[index],nodes[index].localRotation),.01f,"Automatic Animator pose overwrite");
                            Assert.Less(Vector3.Distance(heldPositions[index],nodes[index].localPosition),.0001f);
                        }
                        sampled++;
                    }
                }
                finally{Object.DestroyImmediate(stage);}
            }
            Assert.AreEqual(36,sampled);
            Debug.Log("[RenderCopySampling] currentRigs=9 sampledClips=36 automaticPoseChanges=0 gameplayComponents=0");
        }
    }
}
