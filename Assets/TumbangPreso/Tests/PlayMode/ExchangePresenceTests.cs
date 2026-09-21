using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ExchangePresenceTests
    {
        private bool _bots, _spectator, _pinned; private int _seat; private CustomRules _rules;
        private const string Output = "Logs/exchange-presence-motion";
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();Directory.CreateDirectory(Output);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest]
        public IEnumerator RealStartBrakeTurnYieldsToThrowAndTeleportWithoutLosingTheGrip()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var who=GameServices.Round.PlayerAt(1);who.Teleport(new Vector3(0,.2f,-7));who.ClearStun();who.ClearTrip();
            who.transform.rotation=Quaternion.identity;who.Intent.Parked=false;
            var input=who.gameObject.AddComponent<MotionInput>();
            var anim=who.GetComponent<CharacterAnimator>();var carrier=who.GetComponent<Carrier>();
            var held=carrier.Held;Assert.IsNotNull(held);
            var witness=new GameObject("Weight shift witness").AddComponent<Camera>();witness.enabled=false;
            witness.transform.SetPositionAndRotation(new Vector3(3,1.3f,-3),Quaternion.LookRotation(new Vector3(-3,-.3f,-3)));
            var csv=new StringBuilder("stage,time,forward,side,x,z\n");
            float forward=0,braking=0,side=0;
            yield return new WaitForSeconds(.4f);
            try
            {
                input.Move=Vector2.up;yield return Sample("start",.7f);
                yield return GameplayShots.Render(witness,"start",false,Output,who);
                input.Move=Vector2.zero;yield return Sample("brake",.4f);
                yield return GameplayShots.Render(witness,"brake",false,Output,who);
                input.Move=Vector2.right;input.Facing=Vector3.right;yield return Sample("turn",.45f);
                yield return GameplayShots.Render(witness,"turn",false,Output,who);
                Assert.Greater(forward,1,"The actual moving rig never loaded into its start.");
                Assert.Less(braking,-.15f,"Braking had no visible opposing chest response.");
                Assert.Greater(side,.35f,"Turning/side acceleration produced no lateral weight shift.");
                Assert.AreSame(who,held.Holder);Assert.AreSame(held,carrier.Held);
                anim.PlayAction("throw");yield return null;yield return null;
                Assert.Less(anim.LocomotionLean.sqrMagnitude,.00001f,"Locomotion bent the authored throw pose.");
                input.Move=Vector2.zero;who.Teleport(new Vector3(2,.2f,-7));
                yield return null;yield return null;
                Assert.Less(anim.LocomotionLean.sqrMagnitude,.00001f,"A teleport manufactured a braking kick.");
                yield return new WaitForSeconds(.6f);
                anim.enabled=false;yield return null;anim.enabled=true;yield return null;
                Assert.Less(anim.LocomotionLean.sqrMagnitude,.01f,"Re-enabling accumulated an old overlay.");
            }
            finally{File.WriteAllText(Path.Combine(Output,"weight.csv"),csv.ToString());Object.Destroy(witness.gameObject);Object.Destroy(input);}
            IEnumerator Sample(string stage,float seconds)
            {
                float began=Time.time;
                while(Time.time-began<seconds)
                {
                    yield return null;var lean=anim.LocomotionLean;
                    if(stage=="start")forward=Mathf.Max(forward,lean.x);
                    if(stage=="brake")braking=Mathf.Min(braking,lean.x);
                    if(stage=="turn")side=Mathf.Max(side,Mathf.Abs(lean.y));
                    Assert.LessOrEqual(Mathf.Abs(lean.x),8.001f);Assert.LessOrEqual(Mathf.Abs(lean.y),5.001f);
                    csv.AppendLine(FormattableString.Invariant($"{stage},{Time.time:F4},{lean.x:F4},{lean.y:F4},{who.transform.position.x:F4},{who.transform.position.z:F4}"));
                }
            }
        }

        [UnityTest]
        public IEnumerator NearbyAcceptedImpactScattersExistingBirdOnceWithoutChangingTheMatch()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            foreach(var who in GameServices.Round.Players)who.Teleport(new Vector3(20+who.PlayerSlot*3,.2f,-20));
            var life=Object.FindAnyObjectByType<AmbientLife>();Assert.IsNotNull(life);
            var spec=life.Animals.First(a=>a.Bird);life.StageBirdVisitForReview(spec.Id);
            var bird=life.transform.Find("Ambient "+spec.Id);var from=bird.position;
            int reactions=life.ImpactReactions;var scores=Enumerable.Range(0,4).Select(GameServices.Match.ScoreFor).ToArray();
            MatchFlair.Play(MatchFlair.Kind.Throw,1,-1,from);yield return null;
            Assert.AreEqual(reactions,life.ImpactReactions,"Ordinary release should not scatter the neighborhood.");
            MatchFlair.Play(MatchFlair.Kind.LataDown,1,-1,from);
            Assert.Greater(life.ImpactReactions,reactions,"Nearby accepted tin impact did not affect the existing bird.");
            reactions=life.ImpactReactions;
            MatchFlair.Play(MatchFlair.Kind.LataDown,1,-1,from);
            Assert.AreEqual(reactions,life.ImpactReactions,"The same impact immediately restarted the reaction.");
            yield return new WaitForSeconds(.3f);
            Assert.Greater(Vector3.Distance(from,bird.position),.6f,"Bird did not actually fly along its safe authored exit.");
            CollectionAssert.AreEqual(scores,Enumerable.Range(0,4).Select(GameServices.Match.ScoreFor).ToArray());
            life.StageBirdVisitForReview(spec.Id);life.enabled=false;
            MatchFlair.Play(MatchFlair.Kind.Thunder,1,-1,from);
            Assert.AreEqual(reactions,life.ImpactReactions,"Disabled scenery still received events.");
            life.enabled=true;
        }

        [DefaultExecutionOrder(-300)] private sealed class MotionInput:MonoBehaviour
        {
            public Vector2 Move;public Vector3 Facing=Vector3.forward;
            private void Update()
            {var who=GetComponent<CharacterMotor>();who.Intent.Move=Move;who.Intent.FaceAimPoint=true;who.Intent.AimPoint=who.transform.position+Facing*20;}
        }
    }
}
