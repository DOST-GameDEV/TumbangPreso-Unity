using System;
using System.Collections;
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
    public sealed class LagoonRecoveryProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private CustomRules _rules;
        private static string Output=>Environment.GetEnvironmentVariable("TUMP_LAGOON_RECOVERY")??"Logs/lagoon-fall-recovery";
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(120000)]
        public IEnumerator PlatformFallSwimsThenJumpClimbsAtTheBridgeInBothModes()=>ExerciseEdge(true);

        [UnityTest,Timeout(120000)]
        public IEnumerator RooftopFallCatchesAndClimbsItsActualRailingInBothModes()=>ExerciseEdge(false);

        [UnityTest,Timeout(150000)]
        public IEnumerator CurrentCastFitsTheGripAndRecoveryCancelsCleanly()
        {
            var book=RosterBook.Load();Assert.IsNotNull(book);
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong,mode);Time.timeScale=1;GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);var people=Roster.GetPeople(mode);
                for(int i=0;i<people.Count;i++)
                {
                    who.ClearTrip();who.ClearStun();who.CharacterIndex=i;
                    var art=book.FindPersonArt(people[i].Id);Assert.IsNotNull(art);
                    who.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                    who.Intent.Clear();who.Intent.Parked=false;
                    // Entry physics is covered separately. Place this rig just outside
                    // the real rail to isolate its anatomy/contact and reset contract.
                    var cc=who.GetComponent<CharacterController>();cc.enabled=false;who.transform.position=new Vector3(19.2f,.1f,5);cc.enabled=true;
                    who.Stamina.Step(.01f,true,true);
                    Assert.IsTrue(MapEdgeGeometry.TryRooftop(who,out var anchor),people[i].Id);
                    Assert.IsTrue(who.BeginEdgeRecovery(anchor));yield return new WaitForSeconds(.65f);
                    Assert.Greater(who.Stamina.IdleSeconds,.50f,"Edge ownership froze resource clocks.");
                    var animator=who.GetComponent<CharacterAnimator>();Assert.IsTrue(animator.EdgeRigReady,people[i].Id+" lacks measured grip bones");
                    Assert.Less(animator.EdgeGripError,.10f,people[i].Id+" misses the physical rail");
                    if(mode==GameMode.HeroStrike)yield return CaptureEdge(who,anchor.Grip,anchor.Outward,"grip-"+people[i].Id);
                    int episode=who.RecoveryEpisode;float before=who.TripLeft;
                    PresentationClock.RequestScale(0);
                    try{Assert.IsFalse(who.AcceptRecoveryRequest(episode,1));Assert.AreEqual(before,who.TripLeft);}
                    finally{PresentationClock.RequestScale(1);}
                    Assert.IsTrue(who.AcceptRecoveryRequest(episode,2));Assert.IsFalse(who.AcceptRecoveryRequest(episode,2));
                    Assert.IsFalse(who.AcceptRecoveryRequest(episode,999));
                    who.ClearTrip();Assert.IsFalse(who.IsEdgeRecovering);Assert.IsFalse(who.AcceptRecoveryRequest(episode,3));
                    who.Teleport(new Vector3(17,.1f,5));yield return null;
                }
                Assert.IsTrue(who.AcceptNetworkPoseSerial(10));Assert.IsFalse(who.AcceptNetworkPoseSerial(9));
                Assert.IsFalse(who.AcceptNetworkPoseSerial(10));Assert.IsTrue(who.AcceptNetworkPoseSerial(11));
                Assert.IsFalse(who.AcceptNetworkPoseSerial(0));
            }
        }

        private IEnumerator ExerciseEdge(bool lagoon)
        {
            // Owner correction: historical tests expected centre respawn + prone
            // recovery. Actual water/rail anchoring now replaces that requirement.
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(lagoon?SceneFlow.Lagoon:SceneFlow.SaBubong,mode);
                Time.timeScale=1;GameServices.Round.BeginRound();var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                var shoe=who.GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
                var input=who.gameObject.AddComponent<LagoonInput>();var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                who.Teleport(lagoon?new Vector3(20,.04f,17):new Vector3(18,.1f,5));yield return new WaitForSeconds(.30f);
                input.Move=Vector2.right;yield return new WaitForSeconds(.35f);input.Jump=true;yield return new WaitForSeconds(.10f);input.Jump=false;
                float deadline=Time.time+5,minY=who.transform.position.y;
                while(Time.time<deadline&&(lagoon?(!who.IsSwimming||who.HoldingSlipper):!who.IsEdgeRecovering))
                {minY=Mathf.Min(minY,who.transform.position.y);yield return null;}
                input.Move=Vector2.zero;
                if(lagoon)
                {
                    Assert.IsTrue(who.IsSwimming, "A platform fall must remain a swim.");Assert.IsFalse(who.IsTripped);
                    Assert.Less(minY,LagoonWater.SurfaceY);Assert.Greater(Vector3.Distance(who.transform.position,who.SpawnPosition),10);
                    yield return new WaitForSeconds(.35f);Assert.IsTrue(who.IsSwimming);Assert.IsFalse(who.IsEdgeRecovering);
                    input.Move=Vector2.left;deadline=Time.time+4;
                    while(who.transform.position.x>22.65f&&Time.time<deadline)yield return null;
                    input.Move=Vector2.zero;yield return new WaitForSeconds(.08f);
                    Assert.IsTrue(MapEdgeGeometry.TryLagoon(who,out _), "No reachable edge beside the actual east promenade.");
                    input.Jump=true;yield return new WaitForSeconds(.10f);input.Jump=false;
                }
                Assert.IsTrue(who.IsEdgeRecovering,mode+" did not enter actual edge recovery.");
                Assert.AreEqual(lagoon?EdgeRecoveryKind.Lagoon:EdgeRecoveryKind.Rooftop,who.EdgeKind);
                Assert.IsFalse(who.HoldingSlipper);Assert.IsFalse(shoe.gameObject.activeSelf);
                var grip=who.EdgeGrip;var outward=who.EdgeOutward;
                Assert.That(grip.x,Is.EqualTo(lagoon?21.75f:18.86f).Within(.18f));
                yield return new WaitForSeconds(.65f);Assert.AreEqual(1,who.EdgePhase);
                var animator=who.GetComponent<TumbangPreso.Visual.CharacterAnimator>();Assert.IsTrue(animator.EdgeRigReady);
                Assert.Less(animator.EdgeGripError,.10f,"Visible palms must meet the actual lip.");
                float before=who.TripLeft;
                yield return new WaitForSeconds(!lagoon&&mode==GameMode.Classic?Balance.TripAutoRecoverSeconds+.2f:.35f);
                Assert.That(who.TripLeft,Is.EqualTo(before).Within(.001f),"Waiting cannot complete an edge climb.");
                int count=who.MashPresses;input.Jump=true;yield return new WaitForSeconds(.40f);
                Assert.AreEqual(count+1,who.MashPresses,"A held button must remain one press.");input.Jump=false;yield return new WaitForSeconds(.13f);
                if(mode==GameMode.Classic)
                {
                    yield return CaptureEdge(who,grip,outward,(lagoon?"Lagoon":"Rooftop")+"-hang");
                    yield return CaptureOwner(who,rig,(lagoon?"Lagoon":"Rooftop")+"-hang-owner");
                }
                int taps=0;
                while(who.CanMashUp&&taps++<18)
                {input.Jump=true;yield return new WaitForSeconds(.065f);input.Jump=false;yield return new WaitForSeconds(.065f);}
                deadline=Time.time+2;
                while(who.IsEdgeRecovering&&(who.EdgePhase!=2||who.EdgePhaseRatio<.5f)&&Time.time<deadline)yield return null;
                if(mode==GameMode.Classic)yield return CaptureEdge(who,grip,outward,(lagoon?"Lagoon":"Rooftop")+"-pull");
                deadline=Time.time+2;while(who.IsEdgeRecovering&&Time.time<deadline)yield return null;
                Assert.IsFalse(who.IsEdgeRecovering);Assert.IsFalse(who.IsTripped);Assert.IsTrue(who.CanMove());
                Assert.Less(Vector3.Distance(who.transform.position,grip),2,"Climb must finish just inside the same edge.");
                Assert.Greater(Vector3.Distance(who.transform.position,who.SpawnPosition),10,"Climb teleported to spawn.");
                Assert.That(who.transform.position.y,Is.EqualTo(lagoon?.056f:.12f).Within(.18f));
                if(mode==GameMode.Classic)yield return CaptureEdge(who,grip,outward,(lagoon?"Lagoon":"Rooftop")+"-on-deck");
                float remaining=lagoon?LagoonWater.Instance.SecondsUntilReturn(shoe):RooftopRecovery.Instance.SecondsUntilReturn(shoe);
                if(remaining>.15f){yield return new WaitForSeconds(remaining-.12f);Assert.IsFalse(shoe.gameObject.activeSelf,"Stock returned early.");}
                yield return new WaitForSeconds(.30f);Assert.IsTrue(shoe.gameObject.activeSelf);Assert.AreEqual(SlipperState.Loose,shoe.State);
                Object.Destroy(input);
            }
        }

        private static IEnumerator CaptureEdge(CharacterMotor who,Vector3 grip,Vector3 outward,string label)
        {
            float scale=Time.timeScale;Time.timeScale=0;
            var camera=new GameObject("Edge motion witness").AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=52;camera.nearClipPlane=.05f;camera.farClipPlane=400;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.transform.position=grip+outward*4+Vector3.Cross(Vector3.up,outward)*3+Vector3.up*.35f;
            camera.transform.LookAt(grip-Vector3.up*.45f);
            // Render yields layout frames then runs before LateUpdate. Apply this
            // production post-graph pose at pre-cull for this witness only.
            var animator=who.GetComponent<CharacterAnimator>();
            var late=typeof(CharacterAnimator).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Camera.CameraCallback pose=cam=>{if(cam==camera)late.Invoke(animator,null);};Camera.onPreCull+=pose;
            try{yield return GameplayShots.Render(camera,label,false,Output,observedSubject:who,width:1280,height:800);}
            finally{Camera.onPreCull-=pose;Object.Destroy(camera.gameObject);Time.timeScale=scale;}
        }

        private static IEnumerator CaptureOwner(CharacterMotor who,CameraRig rig,string label)
        {
            var animator=who.GetComponent<CharacterAnimator>();
            var late=typeof(CharacterAnimator).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Camera.CameraCallback pose=cam=>{if(cam==rig.Camera)late.Invoke(animator,null);};Camera.onPreCull+=pose;
            bool cinema=TumbangPreso.Settings.SettingsStore.Current.CinematicCameraMotion;
            try
            {
                yield return GameplayShots.Render(rig.Camera,label,true,Output,width:1280,height:720);
                TumbangPreso.Settings.SettingsStore.Current.CinematicCameraMotion=false;yield return null;
                yield return GameplayShots.Render(rig.Camera,label+"-reduced-camera",true,Output,width:1280,height:720);
            }
            finally{TumbangPreso.Settings.SettingsStore.Current.CinematicCameraMotion=cinema;Camera.onPreCull-=pose;}
        }
        // Exercise the existing intent consumer and actual controller/physics.
        // Physical input devices and separate peers retain their final-integration gate.
        private sealed class LagoonInput:MonoBehaviour
        {
            public Vector2 Move;public bool Jump;
            private void Update(){var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;who.Intent.Set(Verb.Jump,Jump);}
        }
    }
}
