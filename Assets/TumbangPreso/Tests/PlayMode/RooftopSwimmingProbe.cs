using System;
using System.Collections;
using System.IO;
using System.Linq;
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

        [UnityTest,Timeout(240000)]
        public IEnumerator EveryApprovedPersonBindsSwimmingAndReturnsFromSupportedRecovery()
        {
            var report=new StringBuilder("mode,person,swim_phase_change,eye_y,recovery_min_gap,recovery_max_gap\n");
            var mesh=new Mesh();var book=RosterBook.Load();
            try
            {
                foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
                {
                    yield return MapRetrievalProbe.Load(SceneFlow.SaBubong,mode);Time.timeScale=1;GameServices.Round.BeginRound();
                    var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                    _rig=Object.FindFirstObjectByType<CameraRig>();_rig.Follow(who);_rig.SetAimSource(AimSource.Movement);
                    var witness=new GameObject("Whole roster water camera").AddComponent<Camera>();witness.enabled=false;witness.fieldOfView=52;
                    witness.nearClipPlane=.04f;witness.farClipPlane=400;witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    try
                    {
                        var people=Roster.GetPeople(mode);
                        for(int i=0;i<people.Count;i++)
                        {
                            who.CharacterIndex=i;var art=book.FindPersonArt(people[i].Id);Assert.IsNotNull(art);
                            var visual=who.GetComponent<CharacterVisual>();visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                            who.ClearTrip();who.Intent.Clear();who.Intent.Parked=false;
                            who.Teleport(new Vector3(-13.2f,RooftopPool.SurfaceY-RooftopPool.FloatDepth,7));
                            yield return new WaitForSeconds(.3f);
                            var animator=who.GetComponent<CharacterAnimator>();float phase=animator.SwimmingPhase;
                            yield return new WaitForSeconds(.35f);
                            Assert.IsTrue(animator.SwimmingMotionPlaying,people[i].Id+" lacks its actual serialized swim motion");
                            float phaseChange=Mathf.Abs(Mathf.DeltaAngle(phase*Mathf.Rad2Deg,animator.SwimmingPhase*Mathf.Rad2Deg));
                            Assert.Greater(phaseChange,5,people[i].Id+" frozen swimming graph");
                            float eye=_rig.Camera.transform.position.y;Assert.Greater(eye,RooftopPool.SurfaceY+.08f,people[i].Id+" settled eye is underwater");
                            var at=who.transform.position+new Vector3(2.6f,2.3f,-2.8f);
                            witness.transform.SetPositionAndRotation(at,Quaternion.LookRotation(who.transform.position+Vector3.up*.8f-at));
                            yield return GameplayShots.Render(witness,people[i].Id+"-swimming-body",false,Output,who);
                            yield return GameplayShots.Render(_rig.Camera,people[i].Id+"-swimming-owner",false,Output);
                            // Physical rail descent is covered separately. Here the
                            // real recovery state samples every approved serialized rig.
                            who.Teleport(new Vector3(0,.1f,-10));yield return new WaitForSeconds(.2f);who.ApplyFallRecovery();
                            float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
                            float end=Time.time+2.8f,nextMash=Time.time+.42f;bool captured=false;
                            while(Time.time<end)
                            {
                                yield return null;
                                // The game's down state deliberately waits for
                                // accepted mash presses; duration is not a timer
                                // that automatically stands the player at2.5s.
                                if(Time.time>=nextMash&&who.CanMashUp){who.MashRecover();nextMash=Time.time+.18f;}
                                float bottom=float.PositiveInfinity;
                                foreach(var skin in visual.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                                {
                                    skin.BakeMesh(mesh);
                                    foreach(var vertex in mesh.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(vertex).y);
                                }
                                minimum=Mathf.Min(minimum,bottom-.1f);maximum=Mathf.Max(maximum,bottom-.1f);
                                if(!captured&&who.TripLeft<1.5f)
                                {
                                    captured=true;at=who.transform.position+new Vector3(2.2f,1.1f,2.2f);
                                    witness.transform.SetPositionAndRotation(at,Quaternion.LookRotation(who.transform.position+Vector3.up*.55f-at));
                                    yield return GameplayShots.Render(witness,people[i].Id+"-recovery-brace",false,Output,who);
                                }
                            }
                            Assert.IsFalse(who.IsTripped,people[i].Id+" failed to return to standing");
                            Assert.That(minimum,Is.GreaterThan(-.045f),people[i].Id+" recovery sinks into the court");
                            Assert.That(maximum,Is.LessThan(.08f),people[i].Id+" recovery floats above the court");
                            report.AppendLine(FormattableString.Invariant($"{mode},{people[i].Id},{phaseChange:F2},{eye:F3},{minimum:F4},{maximum:F4}"));
                        }
                    }
                    finally{Object.Destroy(witness.gameObject);}
                    yield return PlayModeWorld.Reset();
                }
            }
            finally{Object.Destroy(mesh);File.WriteAllText(Path.Combine(Output,"whole-cast-water-recovery.csv"),report.ToString());}
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
