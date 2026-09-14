using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class BlackIceFootingProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest]
        public IEnumerator BlackIceTradesItsOuterLaneForStrongerFootingPenalty()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
            foreach(var other in GameServices.Round.Players)
                if(other!=who)other.Teleport(new Vector3(9,.12f,5+other.PlayerSlot));
            var variant=HeroLoadoutRules.VariantById("cheska.1.blackice");Assert.IsNotNull(variant);
            var speed=new float[2];var edge=new bool[2];var radius=new float[2];
            for(int index=0;index<2;index++)
            {
                Vector3 center=new Vector3(0,.12f,-4);
                radius[index]=2.3f*(index==1?1+variant.Cost:1);
                var field=HeroHazards.SpawnIceSheet(center,radius[index],5,0,index==1?1+variant.Gain:1);
                who.Intent.Clear();who.Intent.Parked=false;
                who.Teleport(center+Vector3.right*1.9f);
                yield return null;yield return null;
                edge[index]=who.IsOnIce;
                who.Teleport(center);yield return null;yield return null;
                Assert.IsTrue(who.IsOnIce);
                who.Intent.Move=Vector2.up;
                yield return new WaitForSeconds(.3f);
                Vector3 from=who.transform.position;float began=Time.time;
                yield return new WaitForSeconds(.2f);
                speed[index]=Vector3.ProjectOnPlane(who.transform.position-from,Vector3.up).magnitude/(Time.time-began);
                Assert.IsTrue(who.IsOnIce,"The movement sample left its controlled ice footprint.");
                Assert.IsFalse(who.IsTripped,"The authored ice is traction loss, not a compulsory trip.");
                who.Intent.Move=Vector2.zero;Object.Destroy(field);
                yield return null;yield return null;
                Assert.IsFalse(who.IsOnIce);Assert.AreEqual(1,who.SpeedMultiplier,.001f);
            }
            File.WriteAllText("Logs/black-ice-footing.csv",FormattableString.Invariant(
                $"variant,radius,edge_iced,actual_speed\nsheet,{radius[0]},{edge[0]},{speed[0]}\nblackice,{radius[1]},{edge[1]},{speed[1]}\n"));
            Assert.IsTrue(edge[0]);Assert.IsFalse(edge[1]);
            Assert.Greater(speed[0],.5f);Assert.Greater(speed[1],.3f);
            Assert.Less(speed[1],speed[0]*.85f,"The focused patch did not create a stronger real movement cost.");
        }
    }
}
