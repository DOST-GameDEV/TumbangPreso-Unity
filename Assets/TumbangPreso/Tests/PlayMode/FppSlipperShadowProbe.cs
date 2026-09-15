using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class FppSlipperShadowProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots; _spectator=GameLaunch.Spectator; _seat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone(); _pinned=SceneFlow.RulesPinned; _net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots; GameLaunch.Spectator=_spectator; GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FirstPersonSlipperAndChargePartsCastNoSeparateWorldShadow()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            NetAuthority.Provider=new SoloProvider();
            var who=GameServices.Round.PlayerAt(1);
            foreach(var player in GameServices.Round.Players)
            {
                player.ClearStun(); player.ClearTrip();
                player.Teleport(player==who?new Vector3(0,.12f,-8):new Vector3(10+player.PlayerSlot*2,.12f,-14));
            }
            var shoe=who.GetComponent<Carrier>().Held;
            Assert.IsNotNull(shoe);
            var rig=Object.FindFirstObjectByType<CameraRig>(); rig.Follow(who);
            rig.SetAimSource(AimSource.Movement);
            typeof(CameraRig).GetField("_pitchDeg",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,38f);
            string output=Environment.GetEnvironmentVariable("TUMP_FPP_SHADOW_REVIEW")??"Logs/fpp-shadow-review";
            foreach(string hero in new[]{"zack","sean"})
            {
                who.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,hero);
                who.AbilitySystem.BindHero(hero);
                var art=RosterBook.Load().PersonArt(who.CharacterIndex,GameMode.HeroStrike);
                who.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                yield return null;
                var world=shoe.GetComponentInChildren<MeshFilter>().GetComponent<Renderer>();
                var worldMode=world.shadowCastingMode;
                var arms=rig.GetComponentInChildren<ViewmodelArms>(true);
                Assert.IsNotNull(arms);
                foreach(bool charged in new[]{false,true})
                {
                    if(charged)
                    {
                        if(who.AbilitySystem.Kit is ZackHeroKit zack) zack.RestoreJoiningCharges(who,2,0);
                        else ((SeanHeroKit)who.AbilitySystem.Kit).RestoreJoiningIgnition(who,2);
                    }
                    arms.SetHolding(true); arms.MatchSkin(shoe);
                    yield return null; yield return null;
                    var view=arms.transform.Find("RightPivot/Arm/HeldSlipper");
                    Assert.IsNotNull(view);
                    var renderers=view.GetComponentsInChildren<Renderer>();
                    yield return GameplayShots.Render(rig.Camera,hero+(charged?"-charged":"-plain"),false,output);
                    Assert.IsTrue(renderers.Any(r=>r.enabled),"The fix hid the first-person shoe.");
                    foreach(var renderer in renderers)
                        Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode,hero+" FPP caster: "+renderer.name);
                    Assert.AreEqual(worldMode,world.shadowCastingMode,"First-person matching changed the actual world shoe.");
                    if(charged) Assert.Greater(renderers.Length,1,"Charge attachments were not exercised.");
                }
                who.AbilitySystem.ResetKit();
            }
        }
    }
}
