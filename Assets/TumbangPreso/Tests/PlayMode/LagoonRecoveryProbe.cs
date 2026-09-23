using System;
using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
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
        public IEnumerator ActualPlatformFallRequiresPressesAndReturnsStockInBothModes()
        {
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Lagoon,mode);Time.timeScale=1;GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                var shoe=who.GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
                var input=who.gameObject.AddComponent<LagoonInput>();
                var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(who);rig.SetAimSource(AimSource.Movement);
                who.Teleport(new Vector3(20,.04f,17));yield return new WaitForSeconds(.3f);
                input.Move=Vector2.right;yield return new WaitForSeconds(.55f);
                Assert.Less(who.transform.position.x,22,"Walking should respect the existing outer rail.");
                Assert.IsFalse(who.IsTripped,"Grounded walking is not a platform fall.");
                input.Jump=true;yield return new WaitForSeconds(.10f);input.Jump=false;
                float until=Time.time+4,minY=who.transform.position.y;
                while(Time.time<until&&!who.IsTripped){minY=Mathf.Min(minY,who.transform.position.y);yield return null;}
                input.Move=Vector2.zero;
                Assert.Less(minY,LagoonWater.SurfaceY,"Recovery must follow a real descent into water.");
                Assert.IsTrue(who.IsTripped,mode+" ordinary platform fall did not enter mash recovery.");
                Assert.Less(Vector3.Distance(who.transform.position,who.SpawnPosition),.5f,"Fall did not return to the safe deck spawn.");
                Assert.IsFalse(who.HoldingSlipper);Assert.IsFalse(shoe.gameObject.activeSelf);
                var water=Object.FindFirstObjectByType<LagoonWater>();
                Assert.That(water.SecondsUntilReturn(shoe),Is.InRange(LagoonWater.SlipperReturnDelay-.3f,LagoonWater.SlipperReturnDelay));
                float before=who.TripLeft;yield return new WaitForSeconds(.45f);
                Assert.That(who.TripLeft,Is.EqualTo(before).Within(.001f),"Ordinary waiting must not fill the mash meter.");
                input.Jump=true;yield return new WaitForSeconds(.55f);
                Assert.AreEqual(1,who.MashPresses,"Holding a button is one press, not an automatic mash.");
                Assert.IsTrue(who.IsTripped);
                if(mode==GameMode.Classic)yield return GameplayShots.Render(rig.Camera,"Lagoon-fall-prone",true,Output,width:1280,height:720);
                input.Jump=false;yield return new WaitForSeconds(.12f);
                int taps=0;
                while(who.CanMashUp&&taps++<16)
                {input.Jump=true;yield return new WaitForSeconds(.065f);input.Jump=false;yield return new WaitForSeconds(.065f);}
                yield return new WaitForSeconds(.55f);
                Assert.IsFalse(who.IsTripped);Assert.IsFalse(who.IsStunned);Assert.IsTrue(who.CanMove());
                if(mode==GameMode.Classic)yield return GameplayShots.Render(rig.Camera,"Lagoon-fall-standing",true,Output,width:1280,height:720);
                yield return new WaitForSeconds(Mathf.Max(0,water.SecondsUntilReturn(shoe)-.13f));
                Assert.IsFalse(shoe.gameObject.activeSelf,"Lost stock returned before Lagoon's existing delay.");
                yield return new WaitForSeconds(.25f);Assert.IsTrue(shoe.gameObject.activeSelf);
                Assert.AreEqual(SlipperState.Loose,shoe.State);Assert.Greater(shoe.transform.position.y,-.2f);
                who.Teleport(shoe.transform.position+Vector3.back*.25f);yield return new WaitForSeconds(.2f);
                Assert.IsTrue(shoe.HostGrab(who),"Returned stock must be retrievable.");
                Debug.Log($"[Lagoon fall] {mode}: actual descent{minY:F3}, no passive progress, held1press, {taps}tap pulses, delayed stock return/pickup.");
            }
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
