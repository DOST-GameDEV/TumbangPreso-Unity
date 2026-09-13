using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowAimIntegrationProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest]
        public IEnumerator PreviewAndReleasedThrowRetainTheSameAimInBothModes()
        {
            const string output="Logs/throw-aim-integration-v1";Directory.CreateDirectory(output);
            var rows=new System.Collections.Generic.List<string>{"mode,aim_offset_degrees,velocity_error,point_shift_metres"};
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,mode);
                NetAuthority.Provider=new SoloProvider();GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);var carrier=who.GetComponent<Carrier>();
                var shoe=carrier.Held;Assert.IsNotNull(shoe);
                who.Teleport(new Vector3(0,.12f,-10));who.Intent.Clear();who.Intent.Parked=false;
                who.Intent.AimPoint=new Vector3(0,.18f,0);
                Assert.False(who.IsDefender);Assert.True(GameServices.Round.CanThrow(who));
                var rig=Object.FindFirstObjectByType<CameraSystem.CameraRig>();rig.Follow(who);
                who.transform.rotation=Quaternion.identity;rig.SetAimSource(CameraSystem.AimSource.Movement);
                yield return null;
                var toTarget=who.Intent.AimPoint-rig.transform.position;
                typeof(CameraSystem.CameraRig).GetField("_pitchDeg",Private).SetValue(rig,
                    Mathf.Atan2(-toTarget.y,new Vector2(toTarget.x,toTarget.z).magnitude)*Mathf.Rad2Deg);
                carrier.enabled=false;
                who.Intent.Set(Verb.SpecialAbility,true);
                Step(carrier,.016f);Assert.True(carrier.IsCharging);
                Set(carrier,"_charge",.35f);Set(carrier,"_aimHeldSeconds",.35f);Set(carrier,"_aimMovement",.8f);
                carrier.ApplyObservedCharge(true,.35f,0);
                if(mode==GameMode.HeroStrike)
                {
                    who.AbilitySystem.BindHero("zack",new HeroBuild{HeroId="zack",Slot2VariantId="zack.2.discharge"});
                    ((ZackHeroKit)who.AbilitySystem.Kit).IsOverchargeThrowActive=true;
                }
                yield return GameplayShots.Render(rig.Camera,mode+"-steady-aim",true,output);
                var aim=carrier.AimPoint();var expected=carrier.LaunchVelocityNow();
                var viewPoint=rig.Camera.WorldToViewportPoint(aim);
                Assert.Greater(viewPoint.z,0,"Aim review camera must face the actual target.");
                Assert.Less(Vector2.Distance(new Vector2(viewPoint.x,viewPoint.y),new Vector2(.5f,.5f)),.05f);
                var readout=Object.FindFirstObjectByType<TumpMatchReadout>();
                typeof(TumpMatchReadout).GetMethod("LateUpdate",Private).Invoke(readout,null);
                var reticle=(UnityEngine.UI.Text)typeof(TumpMatchReadout).GetField("_crosshair",Private).GetValue(readout);
                Assert.Less(Vector2.Distance(reticle.rectTransform.anchorMin,new Vector2(viewPoint.x,viewPoint.y)),.001f,
                    "Reticle must project the effective throw aim after the camera updates.");
                float shift=Vector3.Distance(aim,who.Intent.AimPoint);
                Assert.Greater(shift,.001f,"Fixture must release a visibly offset aim, not a zero crossing.");
                float angle=carrier.AimAngularOffset.magnitude;
                who.Intent.Set(Verb.SpecialAbility,false);
                Step(carrier,.016f);
                Assert.False(carrier.IsCharging);Assert.IsNull(carrier.Held);
                float error=Vector3.Distance(expected,shoe.Velocity);
                Assert.Less(error,.001f,"Clearing charge or consuming the infusion changed the visible launch solution.");
                rows.Add(FormattableString.Invariant($"{mode},{angle:F5},{error:F6},{shift:F5}"));
                carrier.enabled=true;
                yield return PlayModeWorld.Reset();
            }
            File.WriteAllLines(Path.Combine(output,"release.csv"),rows);
        }

        private static void Set(Carrier carrier,string name,float value)=>typeof(Carrier).GetField(name,Private).SetValue(carrier,value);
        private static void Step(Carrier carrier,float dt)=>typeof(Carrier).GetMethod("StepAttacker",Private).Invoke(carrier,new object[]{dt});
    }
}
