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
        private INetProvider _provider;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;
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
            NetAuthority.Provider=_provider;
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
        public IEnumerator FollowingUltimateStagesAheadAndKeepsItsAcceptedGroundAnchor()
        {
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            var ultimate=_who.AbilitySystem.Kit.Ultimate;
            var carrier=_who.GetComponent<Carrier>();var verbs=_who.GetComponent<CombatVerbs>();
            Vector3 ownerStart=_who.transform.position;
            var ctx=new AbilityContext(_who,carrier,verbs,ownerStart,Vector3.forward,ownerStart+Vector3.forward*10);
            Vector3 shown=ultimate.TelegraphCentre(ctx);
            Assert.Greater(Vector3.Distance(shown,ownerStart),3,"Following giant still grows around the player's camera.");
            pet.SampleIdleForCapture(GhostPetCompanion.FidgetState.CatSmile,.9f);
            ultimate.Activate(ctx);
            Assert.IsFalse(pet.PlayIdleGesture(GhostPetCompanion.FidgetState.CatSmile),"Idle gesture overrides an accepted invocation.");
            Assert.AreEqual(Vector3.zero,pet.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="KuroExpressions").localScale,"The invocation kept a cute expression frozen on the familiar.");
            // Turn the live owner away after committing. The accepted cast and
            // ground reticle must stay together through the windup.
            _who.transform.rotation=Quaternion.Euler(0,150,0);
            yield return new WaitForSeconds(ultimate.Windup+.75f);
            var field=Object.FindFirstObjectByType<HeroHazards.SeanceVoidComponent>();
            Assert.IsNotNull(field);Assert.IsTrue(pet.IsRageFormVisible);
            Assert.Less(Vector3.Distance(shown,field.transform.position),.08f);
            Assert.Less(Vector3.Distance(pet.DevourGround,field.transform.position),.08f);
            var towardsOwner=ownerStart-pet.DevourGround;towardsOwner.y=0;
            Assert.Greater(Vector3.Dot(pet.transform.forward,towardsOwner.normalized),.98f,"Owner sees the giant's back.");
            Assert.Less(Vector3.ProjectOnPlane(_who.transform.position-ownerStart,Vector3.up).magnitude,.05f,"Casting teleported Nemu.");
        }

        [UnityTest]
        public IEnumerator ResetDuringInvocationReleasesTheFamiliarWithoutGrowingIt()
        {
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            // The fixture just teleported the owner; its trailing pet has not
            // necessarily caught up. Compare with the authored follow anchor,
            // not that transient pre-cast position on the far side of the map.
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var followTarget=(Transform)typeof(GhostPetCompanion).GetField("_target",flags).GetValue(pet);
            var followOffset=(Vector3)typeof(GhostPetCompanion).GetField("_localOffset",flags).GetValue(pet);
            _who.AbilitySystem.Kit.AddUltimateCharge(100);
            yield return Press(Verb.Ultimate);
            Assert.IsTrue(_who.AbilitySystem.Kit.Ultimate.IsWindingUp);
            _who.AbilitySystem.ResetKit();
            yield return new WaitForSeconds(.9f);
            Assert.IsFalse(pet.IsDevouring);Assert.IsFalse(pet.IsRageFormVisible);
            Assert.IsNull(Object.FindFirstObjectByType<HeroHazards.SeanceVoidComponent>());
            Assert.Less(Vector3.Distance(pet.transform.position,followTarget.TransformPoint(followOffset)),.2f,"Cancelled staging did not restore the familiar's normal shoulder offset.");
        }
        [UnityTest] public IEnumerator QuickKeyboardTapReachesStunRecovery() => QuickRecoveryTap(false);
        [UnityTest] public IEnumerator QuickControllerTapReachesStunRecovery() => QuickRecoveryTap(true);
        private IEnumerator QuickRecoveryTap(bool controller)
        {
            var device=controller?(UnityEngine.InputSystem.InputDevice)UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>()
                :UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            var action=new UnityEngine.InputSystem.InputAction("RecoveryProbe",UnityEngine.InputSystem.InputActionType.Button,
                controller?"<Gamepad>/buttonSouth":"<Keyboard>/space");
            var reader=_who.GetComponent<PlayerInputReader>()??_who.gameObject.AddComponent<PlayerInputReader>();
            reader.enabled=false;
            typeof(PlayerInputReader).GetField("_jump",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(reader,action);
            var inputSettings=UnityEngine.InputSystem.InputSystem.settings;
            var oldBackground=inputSettings.backgroundBehavior;
            var oldEditorInput=inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.EnableDevice(device);
            action.Enable();
            UnityEngine.InputSystem.InputSystem.Update();
            try
            {
                _who.ApplyStagger(4,StunElement.Ice,6);
                _who.Intent.Clear();_who.Intent.CommitFrame();
                // Both hardware events arrive before one physics tick. A held-state
                // sample alone sees false, even though Input System observed a press.
                if(controller)
                {
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent((UnityEngine.InputSystem.Gamepad)device,
                        new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));

                }
                else
                {
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent((UnityEngine.InputSystem.Keyboard)device,
                        new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space));

                }
                UnityEngine.InputSystem.InputSystem.Update();
                Assert.IsTrue(action.WasPressedThisFrame(),$"Fixture never delivered a hardware press: controls={action.controls.Count}, enabled={device.enabled}, phase={action.phase}, value={action.ReadValue<float>()}.");
                Assert.IsTrue(action.IsPressed(),"Fixture press is not held.");
                reader.SendMessage("Update");
                if(controller)UnityEngine.InputSystem.InputSystem.QueueStateEvent((UnityEngine.InputSystem.Gamepad)device,new UnityEngine.InputSystem.LowLevel.GamepadState());
                else UnityEngine.InputSystem.InputSystem.QueueStateEvent((UnityEngine.InputSystem.Keyboard)device,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.Update();
                Assert.IsFalse(action.IsPressed(),"Fixture must release before physics.");
                reader.SendMessage("Update");
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(1,_who.StunMashPresses,"A quick physical tap was lost before recovery.");
                yield return new WaitForSeconds(.15f);
                Assert.AreEqual(1,_who.StunMashPresses,"One tap became repeated recovery.");
            }
            finally
            {
                action.Dispose();UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
                inputSettings.backgroundBehavior=oldBackground;
                inputSettings.editorInputBehaviorInPlayMode=oldEditorInput;
            }
        }

        [UnityTest]
        public IEnumerator PossessedFamiliarCanDevourThroughTheKeyboardReader()
        {
            yield return Press(Verb.Skill2);
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            Assert.IsTrue(pet.IsPossessed);
            _who.AbilitySystem.Kit.AddUltimateCharge(100);
            var inputSettings=UnityEngine.InputSystem.InputSystem.settings;
            var background=inputSettings.backgroundBehavior;var editorInput=inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
            var action=new UnityEngine.InputSystem.InputAction("DevourProbe",UnityEngine.InputSystem.InputActionType.Button,"<Keyboard>/f");
            var reader=_who.GetComponent<PlayerInputReader>()??_who.gameObject.AddComponent<PlayerInputReader>();reader.enabled=false;
            typeof(PlayerInputReader).GetField("_ultimate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(reader,action);
            action.Enable();UnityEngine.InputSystem.InputSystem.Update();
            try
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                UnityEngine.InputSystem.InputSystem.Update();
                Assert.IsTrue(action.IsPressed(),"The fixture's device was not received.");
                reader.SendMessage("Update");
                _who.AbilitySystem.SendMessage("Update");
                Assert.IsTrue(_who.AbilitySystem.Kit.Ultimate.IsWindingUp,"Possession swallowed the physical ultimate input.");
                yield return new WaitForSeconds(_who.AbilitySystem.Kit.Ultimate.Windup+.15f);
                Assert.IsTrue(_who.AbilitySystem.Kit.Ultimate.IsActive,"The accepted windup did not release the ultimate.");
                Assert.IsTrue(pet.IsDevouring);Assert.IsFalse(pet.IsPossessed);
            }
            finally
            {
                action.Dispose();UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                inputSettings.backgroundBehavior=background;inputSettings.editorInputBehaviorInPlayMode=editorInput;
            }
        }

        [UnityTest]
        public IEnumerator OrdinaryProjectionReturnNeverBecomesTheGiant()
        {
            yield return Press(Verb.Skill2);
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            Assert.IsTrue(pet.IsPossessed);
            yield return Press(Verb.Skill2);
            for(int i=0;i<8;i++)
            {
                Assert.IsFalse(pet.IsRageFormVisible,"Normal E recall borrowed the ultimate transformation.");
                yield return new WaitForSeconds(.08f);
            }
        }

        private void RecoverySnapshot(int episode,int ack,float left=4,int presses=0,bool trip=false)
            => _who.ApplyNetworkState(left,4,trip?StunElement.None:StunElement.Ice,6,trip?0:presses,
                trip?left:0,trip?4:0,trip?presses:0,0,100,0,0,episode,ack);

        [UnityTest]
        public IEnumerator OldSnapshotKeepsOnlyUnacknowledgedRecoveryForTheSameStun()
        {
            NetAuthority.Provider=new OwnerProvider();
            int episode=_who.RecoveryEpisode+1;
            RecoverySnapshot(episode,0);
            Assert.IsTrue(_who.RecoverFromInput());
            float predicted=_who.StunLeft;
            RecoverySnapshot(episode,0);
            Assert.AreEqual(1,_who.StunMashPresses,"Old snapshot erased an in-flight tap.");
            Assert.AreEqual(predicted,_who.StunLeft,.001f);
            RecoverySnapshot(episode,0);
            Assert.AreEqual(predicted,_who.StunLeft,.001f,"Replaying a snapshot spent the tap twice.");
            RecoverySnapshot(episode,1,predicted,1);
            Assert.AreEqual(1,_who.StunMashPresses);
            Assert.AreEqual(predicted,_who.StunLeft,.001f,"Acknowledged tap was predicted again.");
            RecoverySnapshot(episode+1,0);
            Assert.AreEqual(0,_who.StunMashPresses,"Fresh ice inherited taps from previous ice.");
            Assert.AreEqual(4,_who.StunLeft,.001f);
            RecoverySnapshot(episode,1,predicted,1);
            Assert.AreEqual(4,_who.StunLeft,.001f,"Stale episode replaced a fresh stun.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TripRecoverySurvivesOldSnapshotsWithoutDoubleSpending()
        {
            NetAuthority.Provider=new OwnerProvider();
            int episode=_who.RecoveryEpisode+1;
            RecoverySnapshot(episode,0,4,0,true);
            Assert.IsTrue(_who.RecoverFromInput());
            float left=_who.TripLeft;
            RecoverySnapshot(episode,0,4,0,true);
            Assert.AreEqual(1,_who.MashPresses);
            Assert.AreEqual(left,_who.TripLeft,.001f);
            RecoverySnapshot(episode,0,4,0,true);
            Assert.AreEqual(left,_who.TripLeft,.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecoveryRequestsCannotReplayCrossEpisodesOrBypassTheRateCap()
        {
            _who.ApplyStagger(4,StunElement.Ice,6);
            int episode=_who.RecoveryEpisode;
            Assert.IsTrue(_who.AcceptRecoveryRequest(episode,1));
            float left=_who.StunLeft;
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,1));
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,2),"Same-frame spam bypassed Core's cap.");
            Assert.AreEqual(2,_who.RecoveryAcknowledged,"Refused prediction was not acknowledged.");
            Assert.AreEqual(left,_who.StunLeft,.001f);
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,999));
            _who.ApplyStagger(5,StunElement.Ice,6);
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,3),"Old request affected a new ice hold.");
            Assert.AreEqual(0,_who.StunMashPresses);
            yield return null;
        }

        private sealed class HostReplicaProvider : INetProvider
        {
            public bool IsHost=>true;
            public bool IsNetworked=>true;
            public int LocalSlot=>0;
            public int LocalPeerId=>0;
            public bool IsSeatlessReferee=>false;
        }
        private sealed class OwnerProvider : INetProvider
        {
            public bool IsHost=>false;public bool IsNetworked=>true;
            public int LocalSlot=>1;public int LocalPeerId=>1;
            public bool IsSeatlessReferee=>false;
        }
        [UnityTest]
        public IEnumerator DestroyedBotCannotRemainTheHostSimulationOwner()
        {
            foreach(var reader in _who.GetComponents<PlayerInputReader>())Object.Destroy(reader);
            foreach(var brain in _who.GetComponents<AIController>())Object.Destroy(brain);
            yield return null;
            NetAuthority.Provider=new HostReplicaProvider();
            var ai=_who.gameObject.AddComponent<AIController>();ai.enabled=false;
            _who.IsBot=true;_who.ForgetInputSource();
            Assert.IsTrue(_who.IsLocallySimulated());
            // MatchRpc's handover destroys the bot and invalidates the cache.
            // An intervening visual/ability query can refill it before Destroy runs.
            Object.Destroy(ai);_who.IsBot=false;_who.ForgetInputSource();
            _who.IsLocallySimulated();
            yield return null;
            Assert.IsNull(_who.GetComponent<AIController>());
            Assert.IsFalse(_who.IsLocallySimulated(),"Destroyed AI left a permanent stale host-ownership cache.");
        }

        [UnityTest]
        public IEnumerator PredictedRecallIgnoresOldEchoUntilANewMovementEpoch()
        {
            NetAuthority.Provider=new OwnerProvider();
            var before=_who.transform.position;
            _who.BeginAbilityPrediction(1);_who.Teleport(before+Vector3.right*3);_who.EndAbilityPrediction();
            Assert.IsTrue(_who.AwaitingAuthoritativeTeleport);
            _who.ApplyNetworkTransform(before,0,Vector3.zero,true,true);
            Assert.Greater(_who.transform.position.x-before.x,2.9f,"An old echo erased the predicted recall.");
            Assert.IsFalse(_who.RefuseAbilityTeleport(0),"Another slot's refusal cancelled the teleport.");
            _who.AdoptMovementEpoch(1);
            _who.ApplyNetworkTransform(before+Vector3.right*3.1f,0,Vector3.zero,true,true,true);
            Assert.IsFalse(_who.AwaitingAuthoritativeTeleport);
            Assert.AreEqual(1,_who.MovementEpoch);
            Assert.AreEqual(before.x+3.1f,_who.transform.position.x,.01f);
            yield return null;
        }

        private sealed class DelayedAimProbe : HeroAbility
        {
            public AbilityContext Seen;
            public DelayedAimProbe():base("aim-probe","AIM","probe",0,0,
                TumbangPreso.UI.AbilityGlyph.Burst){Windup=.4f;}
            protected override void OnActivate(AbilityContext ctx){Seen=ctx;}
        }
        [UnityTest]
        public IEnumerator DelayedCastKeepsTheAcceptedAimInsteadOfTheReplicaIntent()
        {
            var ability=new DelayedAimProbe();
            var requested=new AbilityContext(_who,null,null,new Vector3(1,0,2),Vector3.right,new Vector3(4,0,3));
            ability.Activate(requested);
            var replica=new AbilityContext(_who,null,null,Vector3.zero,Vector3.back,new Vector3(-5,0,-5));
            ability.Tick(replica,.41f);
            Assert.IsNotNull(ability.Seen);
            Assert.AreEqual(requested.AimPoint,ability.Seen.AimPoint);
            Assert.AreEqual(requested.Position,ability.Seen.Position);
            Assert.AreEqual(requested.Forward,ability.Seen.Forward);
            yield return null;
        }
        [UnityTest]
        public IEnumerator RemoteHumanProjectionDoesNotAcquireAIOrSimulateFlight()
        {
            foreach(var reader in _who.GetComponents<PlayerInputReader>())Object.Destroy(reader);
            foreach(var brain in _who.GetComponents<AIController>())Object.Destroy(brain);
            yield return null;
            _who.IsBot=false;_who.ForgetInputSource();
            NetAuthority.Provider=new HostReplicaProvider();
            Assert.IsFalse(_who.IsLocallySimulated());
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            pet.BeginPossession(_who);pet.SetPlayerInput(Vector2.up);
            Vector3 before=pet.transform.position;
            yield return new WaitForSeconds(.2f);
            Assert.IsNull(_who.GetComponent<AIController>(),"Remote possession stole body simulation from its owner.");
            Assert.Less(Vector3.Distance(before,pet.transform.position),.001f,"Host replica invented flight from local input.");
            pet.EndPossession(false);
        }
        [UnityTest]
        public IEnumerator ZeroScoutInputDoesNotFallBackToTheBodyBotsWalkingDirection()
        {
            yield return Press(Verb.Skill2);
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            pet.SetPlayerInput(Vector2.zero);
            _who.Intent.Move=Vector2.right;
            Vector3 before=pet.transform.position;
            yield return new WaitForSeconds(.2f);
            Assert.Less(Vector3.ProjectOnPlane(pet.transform.position-before,Vector3.up).magnitude,.001f);
        }
        [UnityTest]
        public IEnumerator AuthoritativeRecallClearsTheOldReplicaInterpolationTarget()
        {
            foreach(var reader in _who.GetComponents<PlayerInputReader>())Object.Destroy(reader);
            foreach(var brain in _who.GetComponents<AIController>())Object.Destroy(brain);
            yield return null;
            _who.IsBot=false;_who.ForgetInputSource();NetAuthority.Provider=new HostReplicaProvider();
            _who.ApplyNetworkTransform(new Vector3(0,.12f,-10),0,Vector3.right,true,false,true);
            var destination=new Vector3(2,.12f,-10);_who.Teleport(destination);
            yield return new WaitForSeconds(.2f);
            Assert.Less(Vector3.ProjectOnPlane(_who.transform.position-destination,Vector3.up).magnitude,.01f);
        }

        [UnityTest]
        public IEnumerator AuthoritativeFlightRejectsInvalidHeightWallsAndUnfundedTravel()
        {
            var pet=_who.GetComponent<CharacterVisual>().Companion;
            pet.BeginPossession(_who);pet.SetPlayerInput(Vector2.zero);
            Vector3 start=pet.transform.position;
            Assert.IsFalse(pet.AcceptFlightPose(new Vector3(float.NaN,1,0),0));
            Assert.IsFalse(pet.AcceptFlightPose(start,float.PositiveInfinity));
            Assert.IsFalse(pet.AcceptFlightPose(start+Vector3.up*3,0));
            Assert.IsFalse(pet.AcceptFlightPose(start+Vector3.right*8,0),"A first packet bought an arena crossing.");
            var next=start+Vector3.right*.2f;
            next.y=VfxShapes.GroundPoint(next).y+.9f;
            Assert.IsTrue(pet.AcceptFlightPose(next,20));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=next+Vector3.right*.6f;wall.transform.localScale=new Vector3(.2f,3,3);
            Physics.SyncTransforms();
            Assert.IsFalse(pet.AcceptFlightPose(next+Vector3.right*1.2f,20));
            Assert.Less(Vector3.Distance(next,pet.transform.position),.001f);
            Object.Destroy(wall);pet.EndPossession(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FamiliarSnapshotRebuildsTheRemainingEffectWithoutAChargeOrTeleport()
        {
            yield return Press(Verb.Skill2);
            var kit=(NemuHeroKit)_who.AbilitySystem.Kit;
            var before=_who.transform.position;
            int charges=kit.Skill2.ChargesRemaining;float meter=kit.UltimateCharge;
            var ground=VfxShapes.GroundPoint(new Vector3(0,1,-5));
            kit.RestoreFamiliar(_who,2,ground,.7f,107);
            kit.RestoreFamiliar(_who,2,ground,.6f,107);
            Assert.Less(Vector3.Distance(before,_who.transform.position),.001f,"Replacing possession recalled Nemu.");
            yield return null;
            var fields=Object.FindObjectsByType<HeroHazards.SeanceVoidComponent>(FindObjectsSortMode.None)
                .Where(f=>f.OwnerSlot==_who.PlayerSlot && f.isActiveAndEnabled).ToArray();
            Assert.AreEqual(1,fields.Length,"Repeated snapshot duplicated the field.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(107,_who.GetComponent<CharacterVisual>().Companion.transform.eulerAngles.y)),.1f,"Snapshot lost the accepted giant facing.");
            Assert.Less(Vector3.Distance(fields[0].transform.position,ground),.03f);
            Assert.AreEqual(charges,kit.Skill2.ChargesRemaining);Assert.AreEqual(meter,kit.UltimateCharge);
            Assert.IsFalse(_who.GetComponent<CharacterVisual>().Companion.IsPossessed);
            yield return new WaitForSeconds(.8f);
            Assert.IsFalse(kit.Ultimate.IsActive);
            Assert.IsFalse(_who.GetComponent<CharacterVisual>().Companion.IsDevouring);
            Assert.IsFalse(Object.FindObjectsByType<HeroHazards.SeanceVoidComponent>(FindObjectsSortMode.None)
                .Any(f=>f.OwnerSlot==_who.PlayerSlot && f.isActiveAndEnabled));
        }
        [UnityTest]
        public IEnumerator ProjectionSnapshotRestoresOnlyItsRemainingTrip()
        {
            var kit=(NemuHeroKit)_who.AbilitySystem.Kit;
            var position=new Vector3(1,.9f,-8);
            int charges=kit.Skill2.ChargesRemaining;
            kit.RestoreFamiliar(_who,1,position,.6f);
            Assert.IsTrue(kit.Skill2.IsActive);
            Assert.AreEqual(charges,kit.Skill2.ChargesRemaining);
            Assert.Less(Vector3.Distance(_who.GetComponent<CharacterVisual>().Companion.transform.position,position),.01f);
            yield return new WaitForSeconds(.8f);
            Assert.IsFalse(kit.Skill2.IsActive);
            Assert.IsFalse(_who.GetComponent<CharacterVisual>().Companion.IsPossessed);
        }

        [UnityTest]
        public IEnumerator ModelRebindDoesNotBakeRemoteSmoothingIntoItsAlignment()
        {
            var visual=_who.GetComponent<CharacterVisual>();var root=visual.ModelRoot;
            Vector3 aligned=root.localPosition;
            root.localPosition+=new Vector3(3,0,7);
            visual.AlignToCapsuleFloor();
            Assert.AreEqual(aligned.x,root.localPosition.x,.001f);
            Assert.AreEqual(aligned.z,root.localPosition.z,.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedRosterSnapshotPreservesTheActiveFamiliar()
        {
            var visual=_who.GetComponent<CharacterVisual>();var pet=visual.Companion;
            pet.Devour(7);
            var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
            visual.ApplyModel(entry.Model,entry.Tint,(AnimationClip[])entry.Clips.Clone(),
                (Color[])entry.Palette.Clone(),entry.PetModel);
            yield return null;
            Assert.AreSame(pet,visual.Companion,"An unchanged roster update deleted the live ghost.");
            Assert.IsTrue(pet.IsDevouring);
        }

        [UnityTest]
        public IEnumerator ReplicaDoesNotStoreAnImpulseForALaterOwnershipHandover()
        {
            foreach(var reader in _who.GetComponents<PlayerInputReader>())Object.Destroy(reader);
            foreach(var brain in _who.GetComponents<AIController>())Object.Destroy(brain);
            yield return null;
            _who.IsBot=false;_who.ForgetInputSource();NetAuthority.Provider=new HostReplicaProvider();
            var before=_who.transform.position;
            _who.ApplyImpulse(Vector3.right*10);
            NetAuthority.Provider=_provider;
            yield return new WaitForSeconds(.2f);
            Assert.Less(Vector3.ProjectOnPlane(_who.transform.position-before,Vector3.up).magnitude,.02f,
                "A replica stored an old force and launched when simulation ownership changed.");
        }
        [UnityTest]
        public IEnumerator AClientCannotOriginateResolvedImpacts()
        {
            NetAuthority.Provider=new OwnerProvider();
            var before=_who.transform.position;
            _who.ApplyResolvedImpact(Vector3.right*10);
            yield return new WaitForSeconds(.15f);
            Assert.Less(Vector3.ProjectOnPlane(_who.transform.position-before,Vector3.up).magnitude,.02f);
        }
        [UnityTest]
        public IEnumerator CarapaceStopsResolvedImpactWithoutDisablingOwnMovement()
        {
            _who.AbilitySystem.BindHero("dante");
            var ctx=new AbilityContext(_who,_who.GetComponent<Carrier>(),_who.GetComponent<CombatVerbs>());
            _who.AbilitySystem.Kit.Skill2.Activate(ctx);
            var before=_who.transform.position;
            _who.ApplyResolvedImpact(Vector3.right*10);
            yield return new WaitForSeconds(.15f);
            Assert.Less(Vector3.ProjectOnPlane(_who.transform.position-before,Vector3.up).magnitude,.02f);
            _who.ApplyImpulse(Vector3.right*6);
            yield return new WaitForSeconds(.3f);
            Assert.Greater(_who.transform.position.x-before.x,.35f,"Armor incorrectly disabled the caster's own locomotion impulse.");
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
