using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HomeRosterPreparationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(180000)]
        public IEnumerator EveryCurrentHeroRootedSetSamplesItsActualRig()
        {
            var book=RosterBook.Load();int sets=0,sampled=0;
            var names=new[]{RootedMotion.Struggle,RootedMotion.Heave,RootedMotion.Breakout,RootedMotion.Feared};
            foreach(var hero in Core.Roster.HeroPeople)
            {
                var model=book.FindPersonArt(hero.Id).Model;
                yield return GeneratedMotionAssets.Warmup(model);
                var holder=new GameObject("RootedRigSampleFixture");holder.SetActive(false);
                try
                {
                    var instance=Object.Instantiate(model,holder.transform);
                    var animator=instance.GetComponentInChildren<Animator>();
                    var root=animator!=null?animator.transform:instance.transform;
                    string rig=DanceClip.ResourceName(root);
                    var set=GeneratedMotionAssets.For(RootedMotion.Folder,rig);
                    Assert.IsNotNull(set,"Missing baked rooted set for current hero "+hero.Id+" / "+rig);
                    var nodes=root.GetComponentsInChildren<Transform>(true);
                    var rotations=nodes.Select(x=>x.localRotation).ToArray();
                    var positions=nodes.Select(x=>x.localPosition).ToArray();
                    var scales=nodes.Select(x=>x.localScale).ToArray();
                    foreach(string name in names)
                    {
                        var clip=set.Clips.Single(x=>x.name==name);Assert.Greater(clip.length,0);
                        for(int index=0;index<nodes.Length;index++)
                        {nodes[index].localRotation=rotations[index];nodes[index].localPosition=positions[index];nodes[index].localScale=scales[index];}
                        clip.SampleAnimation(root.gameObject,clip.length*.37f);
                        int moved=Enumerable.Range(0,nodes.Length).Count(index=>Quaternion.Angle(rotations[index],nodes[index].localRotation)>.1f
                            || (positions[index]-nodes[index].localPosition).sqrMagnitude>1e-7f);
                        Assert.Greater(moved,0,"Serialized clip did not drive this current rig: "+hero.Id+" / "+name);
                        sampled++;
                    }
                    sets++;Debug.Log($"[RootedCurrentRig] hero={hero.Id} rig={rig} sampledClips=4 nonBindPoses=4");
                }
                finally{Object.DestroyImmediate(holder);}
            }
            Assert.AreEqual(9,sets);Assert.AreEqual(36,sampled);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator ActualHomePreloadPreparesRosterMeshesAndMotionBeforeReadyWithoutSpawningActors()
        {
            const BindingFlags hidden = BindingFlags.Static | BindingFlags.NonPublic;
            var book = RosterBook.Load();
            Assert.IsNotNull(book);
            var models = book.People.Where(x => x != null).SelectMany(x => new[] { x.Model, x.ArmModel, x.PetModel })
                .Concat(book.Cans.Where(x => x != null).Select(x => x.Model))
                .Concat(book.Slippers.Where(x => x != null).Select(x => x.Model))
                .Where(x => x != null).Distinct().ToArray();
            var renderers = models.SelectMany(x => x.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();
            var meshes = renderers.Select(x => x is SkinnedMeshRenderer skin ? skin.sharedMesh : x.GetComponent<MeshFilter>()?.sharedMesh)
                .Where(x => x != null && x.isReadable).Distinct().ToArray();
            Assert.Greater(meshes.Length, 20, "Exercise the real roster, pets, alternate arms and equipment.");
            foreach (var mesh in meshes) OutlineNormals.Forget(mesh);
            var prepared = typeof(OutlineNormals).GetMethod("Prepared", hidden);
            Assert.IsNotNull(prepared);
            var motion = (Dictionary<string, GeneratedAnimationSet>)typeof(GeneratedMotionAssets).GetField("Cache", hidden).GetValue(null);
            motion.Clear();
            var palette = renderers.Select(x => x.sharedMaterials).ToArray();
            int actors = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length;
            int animators = Object.FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None).Length;
            var owner = new GameObject("HomePreparationRouteFixture");
            owner.SetActive(false);
            var menu = owner.AddComponent<ConvertedMainMenu>();
            try
            {
                var preload = (IEnumerator)typeof(ConvertedMainMenu).GetMethod("PreloadHomeAssets", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
                yield return preload;
                Assert.IsTrue((bool)typeof(ConvertedMainMenu).GetField("_homeAssetsReady", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu));
                var missing = meshes.Where(x => !(bool)prepared.Invoke(null, new object[] { x })).Select(x => x.name).ToArray();
                Assert.IsEmpty(missing, "Home reports ready while roster meshes still need their first outline bake: " + string.Join(", ", missing));
                var paete = book.FindPersonArt("paete").Model;
                var animator = paete.GetComponentInChildren<Animator>();
                string rig = DanceClip.ResourceName(animator != null ? animator.transform : paete.transform);
                Assert.IsTrue(motion.ContainsKey(RootedMotion.Folder + "/" + rig), "Authored recovery/rooted motion must already be retained before Home is ready.");
                Assert.AreEqual(actors, Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(animators, Object.FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None).Length);
                for (int i = 0; i < renderers.Length; i++) CollectionAssert.AreEqual(palette[i], renderers[i].sharedMaterials);
                foreach (var model in models) Assert.IsFalse(OutlineNormals.Warmup(model).MoveNext(), "No cold outline work should remain at first use: " + model.name);
                Debug.Log($"[HomeRosterPreparation] models={models.Length} readableMeshes={meshes.Length} retainedMotion={motion.Count} ready=true spawnedActors=0 unchangedAuthoredMaterials=true");
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
