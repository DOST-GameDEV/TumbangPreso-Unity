using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ZackBankLoadMotionTests
    {
        [Test] public void BankLoadHasItsOwnActionsAndKeepsImmediateEightSecondMechanics()
        {
            var skill = new ZackHeroKit().AttackingSkill;
            Assert.AreEqual("hero-zack-bankshot", skill.CastAction);
            Assert.AreEqual("bank-load", skill.ViewmodelAction);
            Assert.AreEqual(35f, skill.Cooldown); Assert.AreEqual(8f, skill.Duration);
            Assert.AreEqual(0f, skill.Windup);
        }

        [Test] public void ShippingRosterContainsTheLoadAndRetainsTheOldClips()
        {
            var roster = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            var clip = roster.Clips.Single(c => c != null && c.name == "hero-zack-bankshot");
            Assert.That(clip.length, Is.InRange(.63f, .65f));
            Assert.GreaterOrEqual(roster.Clips.Length, 38);
            foreach (string previous in new[] { "hero-zack-charge", "hero-zack-circuit", "hero-zack-sprint", "hero-zack-summon" })
                Assert.IsTrue(roster.Clips.Any(c => c != null && c.name == previous), previous);
            var model = Object.Instantiate(roster.Model);
            try
            {
                var arm = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "arm-left");
                clip.SampleAnimation(model, 0); var neutral = arm.localRotation;
                clip.SampleAnimation(model, .28f);
                Assert.Greater(Quaternion.Angle(neutral, arm.localRotation), 40f);
                clip.SampleAnimation(model, clip.length);
                Assert.Less(Quaternion.Angle(neutral, arm.localRotation), .1f);
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test] public void OwnerLoadUsesItsOwnClipAndTwoHandPathInsteadOfRecall()
        {
            var go = new GameObject("Bank load owner dispatch"); go.SetActive(false);
            try
            {
                var arms = go.AddComponent<ViewmodelArms>();
                const BindingFlags instance = BindingFlags.NonPublic | BindingFlags.Instance;
                const BindingFlags shared = BindingFlags.NonPublic | BindingFlags.Static;
                var field = typeof(ViewmodelArms).GetField("_clip", instance);
                Assert.IsTrue(arms.PlayAction("overcharge")); var recall = field.GetValue(arms);
                Assert.IsTrue(arms.PlayAction("bank-load"));
                Assert.AreNotSame(recall, field.GetValue(arms));
                Assert.AreSame(typeof(ViewmodelArms).GetField("BankLoadClip", shared).GetValue(null), field.GetValue(arms));
                var paths = (System.Collections.IDictionary)typeof(ViewmodelArms).GetField("CastPaths", shared).GetValue(null);
                Assert.IsTrue(paths.Contains("bank-load"));
                Assert.AreNotSame(paths["overcharge"], paths["bank-load"]);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void OwnerPoseCompositionKeepsTheLoadedShoeInsideTheRealLens()
        {
            // Editor-only composition study: real hand renderer/path and shipping lens/mount.
            // This complements, never replaces, the accepted-cast PlayMode checks.
            var root=new GameObject("Bank load owner pose study");
            var previousAmbient=RenderSettings.ambientLight;
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            RenderTexture target=null;Texture2D pixels=null;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion=false;
                var camera=root.AddComponent<Camera>();camera.enabled=false;
                camera.fieldOfView=CameraRig.FppFieldOfView;camera.nearClipPlane=.05f;camera.farClipPlane=10;
                camera.aspect=16f/9;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.08f,.13f,.22f);camera.allowHDR=false;
                var mount=new GameObject("Shipping hand mount");mount.transform.SetParent(root.transform,false);
                mount.transform.localPosition=CameraRig.ViewmodelSeat;mount.transform.localScale=Vector3.one*CameraRig.ViewmodelScale;
                var arms=mount.AddComponent<ViewmodelArms>();arms.EnsureBuilt();arms.SetHero("zack");
                var source=new GameObject("Held source");source.transform.SetParent(root.transform,false);source.transform.localPosition=Vector3.one*100;
                var shoe=source.AddComponent<Slipper>();
                var art=Resources.Load<RosterEntryAsset>("Roster/slipper_loafers");
                var model=Object.Instantiate(art.Model,source.transform);
                TumbangPreso.Visual.ToonSkin.ApplySlipper(model,TumbangPreso.Visual.ToonSkin.PropOutlineWidth);
                arms.MatchSkin(shoe);arms.SetHolding(true);
                foreach(var t in mount.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var lightObject=new GameObject("Pose light");lightObject.transform.SetParent(root.transform,false);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(30,-25,0);light.cullingMask=1<<31;
                RenderSettings.ambientLight=new Color(.55f,.57f,.62f);
                Assert.IsTrue(arms.PlayAction("bank-load"));
                var held=mount.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="HeldSlipper");
                string output=System.Environment.GetEnvironmentVariable("TUMP_BANK_OWNER_POSES");
                if(!string.IsNullOrEmpty(output))System.IO.Directory.CreateDirectory(output);
                bool render=!string.IsNullOrEmpty(output)&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
                if(render){target=new RenderTexture(640,360,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();camera.targetTexture=target;pixels=new Texture2D(640,360,TextureFormat.RGB24,false);}
                int index=0;
                foreach(float time in new[]{0f,.08f,.18f,.28f,.42f,.64f})
                {
                    arms.SeekAction("bank-load",time);arms.StepVisuals(0,true);
                    var at=camera.WorldToViewportPoint(held.bounds.center);
                    Assert.Greater(at.z,camera.nearClipPlane,"Shoe behind lens at "+time);
                    Assert.That(at.x,Is.InRange(0f,1f),"Shoe outside horizontal frame at "+time);
                    Assert.That(at.y,Is.InRange(0f,1f),"Shoe outside vertical frame at "+time);
                    if(render){var previous=RenderTexture.active;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0);pixels.Apply();RenderTexture.active=previous;System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"owner-pose-"+index.ToString("D2")+".png"),pixels.EncodeToPNG());}
                    index++;
                }
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);
                if(target!=null){target.Release();Object.DestroyImmediate(target);}
                if(pixels!=null)Object.DestroyImmediate(pixels);
                RenderSettings.ambientLight=previousAmbient;Settings.SettingsStore.Current.ReducedUiMotion=reduced;
            }
        }
    }
}
