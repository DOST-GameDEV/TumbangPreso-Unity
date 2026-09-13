using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class RooftopSwimmingProbe
    {
        private static string Output=>Environment.GetEnvironmentVariable("TUMP_SWIM_REVIEW")??"Logs/roof-swimming-v1";
        private readonly StringBuilder _trace=new StringBuilder();
        private CameraRig _rig;
        private float _lowestJump;
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();Directory.CreateDirectory(Output);
            _trace.Clear();_trace.AppendLine("mode,stage,time,x,y,z,vy,eye_y,swimming,grounded,holding,clip");
        }
        [UnityTearDown] public IEnumerator After()
        {
            File.WriteAllText(Path.Combine(Output,"swimming.csv"),_trace.ToString());
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest,Timeout(180000)]
        public IEnumerator SwimFromStepsRetrieveFloatingSlipperAndWalkBackOutInBothModes()
        {
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong,mode);
                Time.timeScale=1;GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);
                who.Intent.Parked=false;
                var drive=who.gameObject.AddComponent<SwimmingInput>();
                _rig=Object.FindFirstObjectByType<CameraRig>();_rig.Follow(who);_rig.SetAimSource(AimSource.Movement);
                var animator=who.GetComponent<CharacterAnimator>();
                var shoe=who.GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
                who.Teleport(new Vector3(-10.8f,.1f,-2.5f));
                yield return new WaitForSeconds(.2f);
                yield return WalkTo(who,drive,new Vector2(-10.8f,7),mode,"enter");
                yield return Settle(who,mode,"tread-holding",1.4f);
                Assert.IsTrue(who.IsSwimming,"Pool is still a solid walking surface or buoyancy never engaged");
                Assert.IsFalse(who.IsGrounded,"Swimmer stands on the basin floor");
                Assert.That(who.transform.position.y,Is.InRange(-1.10f,-.82f));
                Assert.IsTrue(animator.SwimmingMotionPlaying,"Body did not select a serialized swim loop");
                Assert.Greater(_rig.Camera.transform.position.y,RooftopPool.SurfaceY+.08f,"Settled FPP eye is submerged");
                Assert.AreSame(shoe,who.GetComponent<Carrier>().Held,"Entering water lost the held slipper");
                yield return GameplayShots.Render(_rig.Camera,mode+"-tread-held-fpp",false,Output);

                shoe.HostThrow(who,new Vector3(-13.8f,1,7),Vector3.down*3);
                yield return Settle(who,mode,"float-stock",1);
                Assert.IsTrue(shoe.gameObject.activeSelf,"Accessible water incorrectly invoked off-roof loss");
                Assert.AreEqual(SlipperState.Loose,shoe.State);
                Assert.That(shoe.transform.position.y-RooftopPool.SurfaceY,Is.InRange(.01f,.6f),"Slipper did not float at the water surface");
                Assert.AreEqual(0,RooftopRecovery.Instance.SecondsUntilReturn(shoe));
                yield return WalkTo(who,drive,new Vector2(-13.8f,7),mode,"retrieve");
                // Pickup consumes a press edge, not a held search request.
                drive.Grab=true;
                yield return Settle(who,mode,"pickup",.4f);drive.Grab=false;
                Assert.AreSame(shoe,who.GetComponent<Carrier>().Held,"Actual swim approach and grab input did not retrieve floating stock");

                yield return WalkTo(who,drive,new Vector2(-10.8f,6),mode,"swim-return");
                yield return WalkTo(who,drive,new Vector2(-10.8f,-2.5f),mode,"exit-steps");
                yield return Settle(who,mode,"dry",.5f);
                Assert.IsFalse(who.IsSwimming,"Leaving the basin retained swimming");
                Assert.IsTrue(who.IsGrounded,"The existing movement input could not exit via steps");
                Assert.Greater(who.transform.position.y,0);
                Assert.IsFalse(animator.SwimmingMotionPlaying,"Dry locomotion retained water motion");
                Assert.AreSame(shoe,who.GetComponent<Carrier>().Held);

                // Enter from the opposite side with a normal jump, rather than
                // only qualifying the shallow staircase's gentler descent.
                who.Teleport(new Vector3(-8.4f,.1f,7));
                yield return new WaitForSeconds(.25f);
                drive.Move=Vector2.left;drive.Jump=true;
                yield return new WaitForSeconds(.1f);drive.Jump=false;
                _lowestJump=who.transform.position.y;
                yield return WalkTo(who,drive,new Vector2(-12.5f,7),mode,"jump-entry");
                Assert.Greater(_lowestJump,-1.16f,"Normal deck jump hit the basin floor or submerged the first-person eye");
                yield return Settle(who,mode,"jump-settle",1.4f);
                Assert.IsTrue(who.IsSwimming);Assert.IsFalse(who.IsGrounded);
                Assert.That(who.transform.position.y,Is.InRange(-1.10f,-.82f));
                Object.Destroy(drive);
                yield return PlayModeWorld.Reset();
            }
        }

        [UnityTest,Timeout(180000)]
        public IEnumerator BodyAndFirstPersonStrokesAtOrdinarySpeedInBothModes()
        {
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong,mode);
                Time.timeScale=1;GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                _rig=Object.FindFirstObjectByType<CameraRig>();_rig.Follow(who);_rig.SetAimSource(AimSource.Movement);
                who.Teleport(new Vector3(-14,RooftopPool.SurfaceY-RooftopPool.FloatDepth,3));
                yield return new WaitForSeconds(.3f);
                var drive=who.gameObject.AddComponent<SwimmingInput>();
                var shoe=who.GetComponent<Carrier>().Held;bool released=false;
                var witness=new GameObject("Swimming motion witness").AddComponent<Camera>();
                witness.enabled=false;witness.fieldOfView=52;witness.nearClipPlane=.05f;witness.farClipPlane=400;
                witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                try
                {
                    yield return ImprovementEvidenceProbe.Record(witness,mode+"-swimming",8,who,t=>
                    {
                        drive.Move=t<1?Vector2.zero:t<3?Vector2.up:t<4?Vector2.zero:t<6?Vector2.down:Vector2.zero;
                        if(t>3&&!released){shoe.HostThrow(who,new Vector3(-12,1,8),Vector3.down);released=true;}
                        Trace(who,mode,"motion");
                    },new Vector3(3.6f,2.0f,-3.6f));
                    Assert.IsTrue(who.IsSwimming);
                    Assert.IsTrue(who.GetComponent<CharacterAnimator>().SwimmingMotionPlaying);
                }
                finally{drive.enabled=false;Object.Destroy(drive);Object.Destroy(witness.gameObject);}
            }
        }

        private IEnumerator WalkTo(CharacterMotor who,SwimmingInput drive,Vector2 target,GameMode mode,string stage)
        {
            float until=Time.time+12;
            while(Time.time<until)
            {
                var p=who.transform.position;var delta=target-new Vector2(p.x,p.z);
                if(delta.magnitude<.16f)break;
                drive.Move=delta.normalized;Trace(who,mode,stage);yield return null;
            }
            drive.Move=Vector2.zero;
            Assert.Less(Vector2.Distance(target,new Vector2(who.transform.position.x,who.transform.position.z)),.3f,
                $"{mode}/{stage}: real movement stopped at {who.transform.position}");
        }
        private IEnumerator Settle(CharacterMotor who,GameMode mode,string stage,float duration)
        {
            float until=Time.time+duration;
            while(Time.time<until){Trace(who,mode,stage);yield return null;}
        }
        private void Trace(CharacterMotor who,GameMode mode,string stage)
        {
            var p=who.transform.position;
            if(stage=="jump-entry")_lowestJump=Mathf.Min(_lowestJump,p.y);
            _trace.AppendLine(FormattableString.Invariant($"{mode},{stage},{Time.time:F4},{p.x:F4},{p.y:F4},{p.z:F4},{who.Velocity.y:F4},{_rig.Camera.transform.position.y:F4},{who.IsSwimming},{who.IsGrounded},{who.HoldingSlipper},{who.GetComponent<CharacterAnimator>().SwimmingMotionPlaying}"));
        }
        [DefaultExecutionOrder(-300)]
        private sealed class SwimmingInput:MonoBehaviour
        {
            public Vector2 Move;public bool Grab,Jump;
            private void Update(){var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;who.Intent.Set(Verb.Grab,Grab);who.Intent.Set(Verb.Jump,Jump);}
        }
    }
}
