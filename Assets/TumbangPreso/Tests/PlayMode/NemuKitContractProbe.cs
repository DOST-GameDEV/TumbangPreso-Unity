using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuKitContractProbe
    {
        private bool _allBots,_spectator,_pinned;
        private int _soloSeat;
        private CustomRules _rules;
        private CharacterMotor _who;
        [UnitySetUp] public IEnumerator Before()
        {
            _allBots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_soloSeat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSeconds(.4f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))reader.enabled=false;
            _who=GameServices.Round.PlayerAt(1);_who.IsBot=true;
            _who.CharacterIndex=Roster.GetPeople(GameMode.HeroStrike).Select((p,i)=>(p,i)).First(p=>p.p.Id=="nemu").i;
            var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
            _who.GetComponent<CharacterVisual>().ApplyModel(entry.Model,entry.Tint,entry.Clips,entry.Palette,entry.PetModel);
            _who.AbilitySystem.BindHero("nemu");_who.Intent.Clear();_who.Intent.Parked=false;
            Camera.main.GetComponent<CameraSystem.CameraRig>().SetAimSource(CameraSystem.AimSource.Movement);
            _who.Intent.FaceAimPoint=false;
            _who.Teleport(new Vector3(0,.12f,-10));
            yield return new WaitForSeconds(.15f);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_allBots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_soloSeat;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private IEnumerator Press(Verb verb)
        {
            _who.Intent.Set(verb,true);yield return new WaitForSeconds(.08f);
            _who.Intent.Set(verb,false);yield return new WaitForSeconds(.04f);
        }
        [UnityTest]
        public IEnumerator SeanceActuallyPullsAPlayerAndLooseSlipperWithoutMovingTheCan()
        {
            yield return Press(Verb.Skill2);
            foreach(var brain in _who.GetComponents<AIController>())brain.enabled=false;
            _who.Intent.Move=Vector2.zero;
            var pet=_who.GetComponent<CharacterVisual>().Companion;pet.SetPlayerInput(Vector2.zero);
            pet.transform.position=new Vector3(0,1,0);
            var victim=GameServices.Round.PlayerAt(2);victim.Intent.Clear();victim.Intent.Parked=false;
            victim.Teleport(new Vector3(2.7f,.12f,0));victim.ClearStun();
            var shoe=victim.GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
            shoe.HostDisarm();shoe.transform.position=new Vector3(-2.8f,.15f,0);
            var can=GameServices.Round.Lata;Vector3 canStart=can.transform.position;
            _who.AbilitySystem.Kit.AddUltimateCharge(100);
            yield return Press(Verb.Ultimate);
            yield return new WaitForSeconds(.5f);
            var field=Object.FindFirstObjectByType<HeroHazards.SeanceVoidComponent>();Assert.IsNotNull(field);
            float before=Vector3.ProjectOnPlane(victim.transform.position-field.transform.position,Vector3.up).magnitude;
            float shoeBefore=Vector3.ProjectOnPlane(shoe.transform.position-field.transform.position,Vector3.up).magnitude;
            yield return new WaitForSeconds(.45f);
            float after=Vector3.ProjectOnPlane(victim.transform.position-field.transform.position,Vector3.up).magnitude;
            float shoeAfter=Vector3.ProjectOnPlane(shoe.transform.position-field.transform.position,Vector3.up).magnitude;
            Assert.Less(after,before-.25f,"The imposing familiar did not actually draw its victim inward.");
            Assert.Less(shoeAfter,shoeBefore,"The loose slipper did not move toward the familiar.");
            Assert.Less(Vector3.Distance(canStart,can.transform.position),.02f,"The pull moved the objective.");
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/nemu-intake.csv",new[]{"target,before,after",
                $"player,{before:F4},{after:F4}",$"slipper,{shoeBefore:F4},{shoeAfter:F4}"});
        }

        [UnityTest]
        public IEnumerator ScoutMovementAndRecallRespectSolidWallsAndCourtBounds()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Scout route wall";
            wall.transform.position=new Vector3(2,1.5f,-10);wall.transform.localScale=new Vector3(.25f,3,8);
            Physics.SyncTransforms();
            var from=new Vector3(0,1,-10);
            var blocked=GhostPetMotion.Move(_who,from,Vector3.right*5);
            Assert.Less(blocked.x,1.7f,"The familiar passed through a body-blocking wall.");
            var beyond=GhostPetMotion.ClampToCourt(_who,new Vector3(1000,1,1000));
            Assert.LessOrEqual(beyond.x,AIController.PlayableHalfX);Assert.LessOrEqual(beyond.z,AIController.PlayableHalfZ);
            var landing=GhostPetMotion.Recall(_who,wall.transform.position,_who.transform.position);
            Assert.IsTrue(GhostPetMotion.CanLand(_who,landing),"Recall selected a solid obstruction.");
            var method=typeof(CameraSystem.CameraRig).GetMethod("ConstrainCompanionCamera",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            var view=(Vector3)method.Invoke(null,new object[]{from,from+Vector3.right*4});
            Assert.Less(view.x,1.8f,"The possession camera crossed the wall.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator APassingScareDoesNotRefreshItsStunEveryFrame()
        {
            yield return Press(Verb.Skill2);
            foreach(var brain in _who.GetComponents<AIController>())brain.enabled=false;
            _who.Intent.Move=Vector2.zero;
            var pet=_who.GetComponent<CharacterVisual>().Companion;pet.SetPlayerInput(Vector2.zero);
            pet.transform.position=new Vector3(0,1,-5);
            var victim=GameServices.Round.PlayerAt(2);victim.Intent.Clear();victim.Intent.Parked=false;
            victim.Teleport(new Vector3(.2f,.12f,-5));victim.ClearStun();
            yield return new WaitForSeconds(.1f);
            Assert.IsTrue(victim.IsStunned,"The passing ghost did not make contact.");
            yield return new WaitForSeconds(.5f);
            Assert.Less(Vector3.ProjectOnPlane(victim.transform.position-pet.transform.position,Vector3.up).magnitude,1.6f);
            Assert.IsFalse(victim.IsStunned,"Remaining near the ghost permanently refreshed the stagger.");
        }

        [UnityTest] public IEnumerator ResetProjectionDoesNotTeleport() => CheckProjectionEnd(0);
        [UnityTest] public IEnumerator RefusedProjectionDoesNotTeleport() => CheckProjectionEnd(1);
        [UnityTest] public IEnumerator NormalProjectionRecastStillTeleports() => CheckProjectionEnd(2);
        private IEnumerator CheckProjectionEnd(int kind)
        {
            yield return Press(Verb.Skill2);
            var pet=_who.GetComponent<CharacterVisual>().Companion;Assert.IsTrue(pet.IsPossessed);
            foreach(var brain in _who.GetComponents<AIController>())brain.enabled=false;
            _who.Intent.Move=Vector2.zero;pet.SetPlayerInput(Vector2.zero);
            Vector3 before=_who.transform.position;
            pet.transform.position=before+Vector3.right*2;
            Vector3 destination=pet.transform.position;
            if(kind==0)_who.AbilitySystem.ResetKit();
            else if(kind==1)_who.AbilitySystem.Kit.Skill2.RollBackPredictedCast(
                new AbilityContext(_who,_who.GetComponent<Carrier>(),_who.GetComponent<CombatVerbs>()));
            else yield return Press(Verb.Skill2);
            yield return null;yield return null;
            Assert.IsFalse(pet.IsPossessed);
            Assert.Less(Vector3.Distance(_who.transform.position,kind==2?destination:before),.15f,
                kind==2?"Normal recall stopped relocating Nemu.":"Cancellation performed a teleport that was never accepted.");
        }
        [UnityTest]
        public IEnumerator ResetSeanceRemovesTheLivePullImmediately()
        {
            _who.AbilitySystem.Kit.AddUltimateCharge(100);
            yield return Press(Verb.Ultimate);
            yield return new WaitForSeconds(.5f);
            var field=Object.FindFirstObjectByType<HeroHazards.SeanceVoidComponent>();
            Assert.IsNotNull(field);Assert.IsTrue(_who.AbilitySystem.Kit.Ultimate.IsActive);
            _who.AbilitySystem.ResetKit();
            Assert.IsFalse(field.gameObject.activeInHierarchy,"The reset left a live pull until end of frame.");
            yield return null;
            Assert.IsTrue(field==null);
            Assert.IsFalse(_who.GetComponent<CharacterVisual>().Companion.IsDevouring);
        }

        [UnityTest]
        public IEnumerator VeilSurvivesAnAlreadyHeldSlipperAndItsSustainedSpeedRestores()
        {
            Assert.IsTrue(_who.HoldingSlipper,"The cast must start with a real held slipper.");
            _who.Intent.Move=Vector2.up;yield return new WaitForSeconds(.5f);
            float before=new Vector2(_who.Velocity.x,_who.Velocity.z).magnitude;
            Assert.Greater(before,1);
            var walkStart=_who.transform.position;float walkAt=Time.time;
            yield return new WaitForSeconds(.3f);
            float walkActual=Vector3.ProjectOnPlane(_who.transform.position-walkStart,Vector3.up).magnitude/(Time.time-walkAt);
            yield return Press(Verb.Skill1);
            yield return new WaitForSeconds(.6f);
            Assert.IsTrue(_who.AbilitySystem.Kit.Skill1.IsActive,"A shoe held before casting cancelled Veil.");
            float during=new Vector2(_who.Velocity.x,_who.Velocity.z).magnitude;
            Assert.AreEqual(before*Balance.NemuPhaseSpeedScale,during,.08f,"The speed gain vanished with the opening impulse.");
            var phaseStart=_who.transform.position;float phaseAt=Time.time;
            yield return new WaitForSeconds(.3f);
            float phaseActual=Vector3.ProjectOnPlane(_who.transform.position-phaseStart,Vector3.up).magnitude/(Time.time-phaseAt);
            Assert.Greater(walkActual,1,"The reference was stuck against scene collision.");
            Assert.Greater(phaseActual,walkActual*1.10f,"The speed gain never reached actual movement.");
            var veil=_who.GetComponentInChildren<NemuVeilPresentation>();Assert.IsNotNull(veil);
            Assert.IsEmpty(veil.GetComponentsInChildren<Light>());
            _who.Intent.Move=Vector2.zero;
            yield return new WaitForSeconds(2.2f);
            _who.Intent.Move=Vector2.up;yield return new WaitForSeconds(.25f);
            Assert.IsFalse(_who.AbilitySystem.Kit.Skill1.IsActive);
            float after=new Vector2(_who.Velocity.x,_who.Velocity.z).magnitude;
            Assert.AreEqual(before,after,.08f);
            Assert.IsTrue(veil==null,"Veil's visual survived the end of immunity.");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/nemu-veil-speed.csv",$"phase,speed\nbefore,{before:F4}\nduring,{during:F4}\nafter,{after:F4}\nactual_walk,{walkActual:F4}\nactual_phase,{phaseActual:F4}\n");
        }
        [UnityTest]
        public IEnumerator ANewSlipperAcquisitionEndsVeilWithoutLeavingItsVisual()
        {
            yield return Press(Verb.Skill1);
            var shoe=_who.GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
            // Arrange the existing possession transition through the real slipper
            // methods; this test is about the buff's response, not throw legality.
            shoe.HostDisarm();yield return null;yield return null;
            Assert.IsFalse(_who.HoldingSlipper);
            Assert.IsTrue(_who.AbilitySystem.Kit.Skill1.IsActive);
            Assert.IsTrue(shoe.HostForceEquip(_who));yield return null;yield return null;
            Assert.IsFalse(_who.AbilitySystem.Kit.Skill1.IsActive,"A genuine new acquisition failed to end Veil.");
            Assert.AreEqual(1,_who.AbilitySystem.Kit.MovementSpeedScale,.001f);
            yield return new WaitForSeconds(.4f);
            Assert.IsNull(_who.GetComponentInChildren<NemuVeilPresentation>());
        }
        [UnityTest]
        public IEnumerator FeedingAndReturningFamiliarDoNotConsumeAPossessionCharge()
        {
            var pet=_who.GetComponent<CharacterVisual>().Companion;Assert.IsNotNull(pet);
            pet.Devour(.7f);
            int charges=_who.AbilitySystem.Kit.Skill2.ChargesRemaining;
            yield return Press(Verb.Skill2);
            Assert.IsFalse(pet.IsPossessed);Assert.AreEqual(charges,_who.AbilitySystem.Kit.Skill2.ChargesRemaining);
            yield return new WaitForSeconds(.65f);
            Assert.IsTrue(pet.IsReturning);
            yield return Press(Verb.Skill2);
            Assert.IsFalse(pet.IsPossessed);Assert.AreEqual(charges,_who.AbilitySystem.Kit.Skill2.ChargesRemaining);
            yield return new WaitForSeconds(.85f);
            yield return Press(Verb.Skill2);
            Assert.IsTrue(pet.IsPossessed,"The charge stayed unusable after the familiar returned.");
            Assert.AreEqual(charges-1,_who.AbilitySystem.Kit.Skill2.ChargesRemaining);
        }
    }
}
