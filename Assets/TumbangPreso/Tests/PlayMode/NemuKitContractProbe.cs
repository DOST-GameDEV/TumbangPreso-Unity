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
        [UnityTest] public IEnumerator QuickKeyboardTapCannotShortenTimedStun() => QuickRecoveryTap(false);
        [UnityTest] public IEnumerator QuickControllerTapCannotShortenTimedStun() => QuickRecoveryTap(true);
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
                Assert.AreEqual(0,_who.StunMashPresses,"A quick physical tap shortened a timed stun.");
                yield return new WaitForSeconds(.15f);
                Assert.AreEqual(0,_who.StunMashPresses,"Released input reactivated retired recovery.");
            }
            finally
            {
                action.Dispose();UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
                inputSettings.backgroundBehavior=oldBackground;
                inputSettings.editorInputBehaviorInPlayMode=oldEditorInput;
            }
        }



        private void RecoverySnapshot(int episode,int ack,float left=4,int presses=0,bool trip=false)
            => _who.ApplyNetworkState(trip?0:left,trip?0:4,trip?StunElement.None:StunElement.Ice,6,trip?0:presses,
                trip?left:0,trip?4:0,trip?presses:0,0,100,0,0,episode,ack);

        [UnityTest]
        public IEnumerator TimedStunSnapshotsCannotRestoreRetiredRecoveryInputs()
        {
            NetAuthority.Provider=new OwnerProvider();
            int episode=_who.RecoveryEpisode+1;
            RecoverySnapshot(episode,0);
            Assert.IsFalse(_who.RecoverFromInput());
            float predicted=_who.StunLeft;
            RecoverySnapshot(episode,0);
            Assert.AreEqual(0,_who.StunMashPresses,"A snapshot restored retired mash progress.");
            Assert.AreEqual(predicted,_who.StunLeft,.001f);
            RecoverySnapshot(episode,0);
            Assert.AreEqual(predicted,_who.StunLeft,.001f,"Replaying a snapshot spent the tap twice.");
            RecoverySnapshot(episode,1,predicted,1);
            Assert.AreEqual(0,_who.StunMashPresses);
            Assert.AreEqual(predicted,_who.StunLeft,.001f,"Acknowledged tap was predicted again.");
            RecoverySnapshot(episode+1,0);
            Assert.AreEqual(0,_who.StunMashPresses,"Fresh ice inherited taps from previous ice.");
            Assert.AreEqual(4,_who.StunLeft,.001f);
            RecoverySnapshot(episode,1,predicted,1);
            Assert.AreEqual(4,_who.StunLeft,.001f,"Stale episode replaced a fresh stun.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TripSnapshotsStayAuthoritativeWithoutRecoveryPrediction()
        {
            NetAuthority.Provider=new OwnerProvider();
            int episode=_who.RecoveryEpisode+1;
            RecoverySnapshot(episode,0,4,0,true);
            Assert.IsFalse(_who.RecoverFromInput());
            float left=_who.TripLeft;
            RecoverySnapshot(episode,0,4,0,true);
            Assert.AreEqual(0,_who.MashPresses);
            Assert.AreEqual(left,_who.TripLeft,.001f);
            RecoverySnapshot(episode,0,4,0,true);
            Assert.AreEqual(left,_who.TripLeft,.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RetiredRecoveryRequestsCannotAffectAnyEpisode()
        {
            _who.ApplyStagger(4,StunElement.Ice,6);
            int episode=_who.RecoveryEpisode;
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,1));
            float left=_who.StunLeft;
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,1));
            Assert.IsFalse(_who.AcceptRecoveryRequest(episode,2),"Same-frame spam bypassed Core's cap.");
            Assert.AreEqual(0,_who.RecoveryAcknowledged,"Retired recovery requests must not create live acknowledgements.");
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
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
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
        public IEnumerator ScoutMovementAndRecallRespectSolidWallsAndCourtBounds()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Scout route wall";
            wall.transform.position=new Vector3(2,1.5f,-10);wall.transform.localScale=new Vector3(.25f,3,8);
            Physics.SyncTransforms();
            var from=new Vector3(0,1,-10);
            var blocked=GhostPetMotion.Move(_who,from,Vector3.right*5);
            Assert.Less(blocked.x,1.7f,"The familiar passed through a body-blocking wall.");
            var beyond=GhostPetMotion.ClampToCourt(_who,new Vector3(1000,1,1000));
            Assert.LessOrEqual(beyond.x,AIController.PlayableMaxX);Assert.LessOrEqual(beyond.z,AIController.PlayableMaxZ);
            var below=GhostPetMotion.ClampToCourt(_who,new Vector3(-1000,1,-1000));
            Assert.GreaterOrEqual(below.x,AIController.PlayableMinX);Assert.GreaterOrEqual(below.z,AIController.PlayableMinZ);
            var landing=GhostPetMotion.Recall(_who,wall.transform.position,_who.transform.position);
            Assert.IsTrue(GhostPetMotion.CanLand(_who,landing),"Recall selected a solid obstruction.");
            var method=typeof(CameraSystem.CameraRig).GetMethod("ConstrainCompanionCamera",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            var view=(Vector3)method.Invoke(null,new object[]{from,from+Vector3.right*4});
            Assert.Less(view.x,1.8f,"The possession camera crossed the wall.");
            yield return null;
        }


    }
}
