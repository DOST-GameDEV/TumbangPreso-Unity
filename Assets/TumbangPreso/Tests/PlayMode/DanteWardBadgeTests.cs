using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteWardBadgeTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator ShieldCueFollowsAssignedAgeAndCamera()
        {
            int mip=QualitySettings.globalTextureMipmapLimit;QualitySettings.globalTextureMipmapLimit=2;
            GameObject eye=null; DanteCarapaceVisual ward=null;
            try
            {
                yield return MapRetrievalProbe.Load("Eskinita",Core.GameMode.HeroStrike);
                var who=GameServices.Round.PlayerAt(1);
                var art=RosterBook.Load().FindPersonArt("dante");
                var visual=who.GetComponent<CharacterVisual>();
                visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                eye=new GameObject("Ward badge observer");var camera=eye.AddComponent<Camera>();camera.enabled=false;
                foreach(float duration in new[]{15f,20f})
                foreach(bool recorded in new[]{false,true})
                {
                    ward=recorded?DanteCarapaceVisual.Recorded(visual.Model,who.transform,false,duration,0)
                        :DanteCarapaceVisual.Attach(who,false,duration);
                    Assert.IsNotNull(ward);
                    var node=who.transform.Find("DanteShieldBadge");
                    Assert.IsNotNull(node,"The active ward has no shield-logo cue.");
                    var badge=node.GetComponent<SpriteRenderer>();
                    Assert.AreSame(AbilityIcons.For(AbilityGlyph.DanteShield),badge.sprite);
                    Assert.IsEmpty(node.GetComponentsInChildren<Collider>());
                    foreach(float age in new[]{.4f,duration-.01f,duration,duration+1,3f,-.1f})
                    {
                        ward.StepTo(age);
                        for(int side=-1;side<=1;side+=2)
                        {
                            camera.transform.position=who.transform.position+new Vector3(side*3,2.5f,-4);
                            camera.transform.LookAt(who.transform.position+Vector3.up);
                            camera.Render();
                            Assert.AreEqual(age>=0&&age<duration,badge.enabled,"Cue has its own incorrect clock.");
                            if(badge.enabled)Assert.Greater(Vector3.Dot(node.forward,(node.position-camera.transform.position).normalized),.99f);
                        }
                    }
                    ward.StepTo(3);
                    if(!recorded)
                    {
                        var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
                        rig.SetAimSource(CameraSystem.AimSource.Movement);rig.Camera.Render();
                        Assert.IsTrue(rig.IsLocalFpp,"The owner visibility assertion must exercise first person.");
                        Assert.IsFalse(badge.enabled,"Own first-person cue obstructs play.");
                    }
                    if(duration==20&&!recorded)
                        yield return GameplayShots.Render(camera,"active-shield",false,"Logs/dante-ward-badge",who);
                    Object.Destroy(ward.gameObject);ward=null;yield return null;yield return null;
                    Assert.IsTrue(node==null,"Detached shield cue outlives its ward.");
                }
            }
            finally
            {
                QualitySettings.globalTextureMipmapLimit=mip;
                if(ward!=null)Object.DestroyImmediate(ward.gameObject);
                if(eye!=null)Object.DestroyImmediate(eye);
            }
        }
    }
}
