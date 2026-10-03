using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AmihanAirburstTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        CharacterMotor _caster, _victim, _outside;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        { foreach(var go in _built) if(go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        CharacterMotor Seat(int slot, Vector3 at)
        {
            var go=Track(new GameObject("Airburst seat "+slot,typeof(CharacterController)));
            var motor=go.AddComponent<CharacterMotor>(); motor.PlayerSlot=slot;
            motor.IsDefender=slot==0; motor.IsBot=false; motor.Mode=GameMode.HeroStrike;
            go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>();
            var cc=go.GetComponent<CharacterController>();
            at.y=-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f;
            go.transform.position=at; GameServices.Round.Register(motor); return motor;
        }
        IEnumerator Open()
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameServices.Ensure(); GameServices.Round.Clear();
            var floor=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.localScale=new Vector3(60,1,60); floor.transform.position=Vector3.down*.5f;
            var can=Track(new GameObject("Airburst can")); can.transform.position=new Vector3(6,0,6);
            GameServices.Round.Lata=can.AddComponent<Lata>();
            _caster=Seat(0,new Vector3(0,0,-4)); _victim=Seat(1,Vector3.zero);
            _outside=Seat(2,new Vector3(5,0,-4));
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            Physics.SyncTransforms(); yield return new WaitForSeconds(.2f);
        }
        AmihanStorm Storm()
        {
            var storm=AmihanStorm.Spawn(_caster.transform.position,Vector3.forward,0,null);
            Track(storm.gameObject); return storm;
        }
        [UnityTest] public IEnumerator ReleaseWhirlsOnlyCaughtPlayers()
        {
            yield return Open(); var storm=Storm();
            Assert.IsFalse(_victim.IsWhirled); storm.Release();
            Assert.IsTrue(_victim.IsWhirled,"Airburst must apply the current Wiki Whirled status.");
            Assert.IsFalse(_caster.IsWhirled); Assert.IsFalse(_outside.IsWhirled);
            float left=_victim.WhirledLeft; storm.Release(); Assert.AreEqual(left,_victim.WhirledLeft);
        }
        [UnityTest] public IEnumerator CaughtLooseSlipperBecomesAirborne()
        {
            yield return Open();
            var shoe=Track(new GameObject("Airburst loose slipper")).AddComponent<Slipper>();
            shoe.SeatOfOrigin=1; shoe.OwnerSlot=1;
            shoe.transform.position=new Vector3(1,.15f,1); shoe.HostScatter(Vector3.zero);
            float start=shoe.transform.position.y; var storm=Storm(); storm.Release();
            float high=start;
            for(int i=0;i<15;i++) { yield return new WaitForFixedUpdate(); high=Mathf.Max(high,shoe.transform.position.y); }
            Assert.Greater(high-start,.25f,"Airburst must lift the slipper instead of sliding it on the road.");
        }
        // v3.2: the cutscene shows the windup and the release; the accepted cast releases at once in play.
        [UnityTest] public IEnumerator AcceptedCastReleasesAtOnceAndLifts()
        {
            yield return Open();
            var shoe=Track(new GameObject("Airburst held slipper")).AddComponent<Slipper>();
            shoe.SeatOfOrigin=1; shoe.OwnerSlot=1; Assert.IsTrue(shoe.HostForceEquip(_victim));
            var kit=new AmihanHeroKit();
            var ctx=new AbilityContext(_caster,_caster.GetComponent<Carrier>(),_caster.GetComponent<CombatVerbs>());
            Vector3 start=_victim.transform.position;
            kit.Ultimate.Activate(ctx); Assert.IsFalse(kit.Ultimate.IsWindingUp);
            var storm=Object.FindFirstObjectByType<AmihanStorm>(); Assert.IsNotNull(storm); Track(storm.gameObject);
            Assert.IsTrue(storm.Released); Assert.IsTrue(_victim.IsWhirled);
            Assert.IsNull(_victim.GetComponent<Carrier>().Held);
            Assert.AreEqual(SlipperState.InFlight,shoe.State); Assert.AreEqual(-1,shoe.ThrowerSlot);
            float high=start.y;
            for(int n=0;n<35;n++) { yield return new WaitForFixedUpdate(); high=Mathf.Max(high,_victim.transform.position.y); }
            Assert.Greater(high-start.y,.6f,"A visible body arc, not a road-level slide.");
            Assert.Greater(_victim.transform.position.z-start.z,5f,"The accepted carry must actually move the motor quickly.");
            yield return new WaitForSeconds(2);
            Assert.AreEqual(SlipperState.Loose,shoe.State,"Existing host flight must finish with a retrievable slipper.");
        }
        [UnityTest] public IEnumerator SixtyDegreeBoundaryAppliesToBodiesAndSlippers()
        {
            yield return Open();
            Vector3 inside=Quaternion.AngleAxis(29.9f,Vector3.up)*Vector3.forward*6;
            Vector3 outside=Quaternion.AngleAxis(30.1f,Vector3.up)*Vector3.forward*8;
            Vector3 origin=_caster.transform.position;
            _victim.Teleport(origin+inside); _outside.Teleport(origin+outside);
            var caught=Track(new GameObject("Inside60degree slipper")).AddComponent<Slipper>();
            caught.SeatOfOrigin=1;caught.OwnerSlot=1;caught.transform.position=origin+inside+Vector3.up*.1f;
            var missed=Track(new GameObject("Outside60degree slipper")).AddComponent<Slipper>();
            missed.SeatOfOrigin=2;missed.OwnerSlot=2;missed.transform.position=origin+outside+Vector3.up*.1f;
            Physics.SyncTransforms(); var storm=Storm();storm.Release();
            Assert.IsTrue(_victim.IsWhirled);Assert.IsFalse(_outside.IsWhirled);
            Assert.AreEqual(SlipperState.InFlight,caught.State);Assert.AreEqual(SlipperState.Loose,missed.State);
            Assert.IsTrue(AmihanStorm.InsideFan(origin,Vector3.forward,origin+inside));
            Assert.IsFalse(AmihanStorm.InsideFan(origin,Vector3.forward,origin+outside));
        }
        [UnityTest] public IEnumerator ReservedAirburstReleasesOnlyAfterIntroduction()
        {
            yield return Open();var kit=new AmihanHeroKit();kit.AddUltimateCharge(15);
            var ctx=new AbilityContext(_caster,_caster.GetComponent<Carrier>(),_caster.GetComponent<CombatVerbs>());
            const System.Reflection.BindingFlags hidden=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var result=typeof(HeroKit).GetMethod("ReserveUltimate",hidden).Invoke(kit,new object[]{ctx});
            Assert.AreEqual(HeroKit.CastOutcome.Cast,result);Assert.AreEqual(0,kit.UltimateCharge);
            Assert.IsTrue(kit.Ultimate.ReservedForIntroduction);Assert.IsFalse(kit.Ultimate.IsWindingUp);
            Assert.IsNull(Object.FindFirstObjectByType<AmihanStorm>());
            typeof(HeroAbility).GetMethod("BeginReservedActivation",hidden).Invoke(kit.Ultimate,new object[]{ctx});
            var storm=Object.FindFirstObjectByType<AmihanStorm>();Assert.IsNotNull(storm);Track(storm.gameObject);
            // v3.2: no live delay after the cutscene (`AmihanRules.StormSurgeDelaySeconds`).
            Assert.IsFalse(kit.Ultimate.IsWindingUp);Assert.IsTrue(storm.Released);Assert.IsTrue(_victim.IsWhirled);
            Assert.AreEqual(0,kit.UltimateCharge,"Reservation spent twice.");
        }
        [UnityTest] public IEnumerator ExpandedCourtContactAndWarningShareTheMapWideReach()
        {
            yield return Open();
            var bounds=new Vector4(AIController.PlayableMinX,AIController.PlayableMaxX,AIController.PlayableMinZ,AIController.PlayableMaxZ);
            try
            {
                AIController.PlayableMinX=-16;AIController.PlayableMaxX=16;
                AIController.PlayableMinZ=-13;AIController.PlayableMaxZ=24;
                GameServices.Match.ApplySnapshot(new int[4],2,true);
                GameServices.Round.ApplySnapshot(100,true,1,true);
                Assert.IsFalse(_caster.IsDefender);Assert.IsFalse(_outside.IsDefender);
                Vector3 origin=new Vector3(-15.6f,0,-12.6f),target=new Vector3(15.6f,0,23.6f);
                _caster.Teleport(origin);_outside.Teleport(target);Physics.SyncTransforms();
                Assert.Greater(Vector3.Distance(_caster.transform.position,_outside.transform.position),40f);
                Vector3 forward=target-origin;forward.y=0;forward.Normalize();
                var storm=AmihanStorm.Spawn(_caster.transform.position,forward,0,null);Track(storm.gameObject);
                var fan=storm.GetComponentInChildren<Visual.AmihanStormFan>();Assert.IsNotNull(fan);
                float displayed=(float)typeof(Visual.AmihanStormFan).GetField("_range",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(fan);
                Assert.AreEqual(AmihanStorm.FanRange,displayed,.001f);
                Assert.Greater(displayed,Vector3.Distance(origin,target));
                var shoe=Track(new GameObject("Expanded-court caught slipper")).AddComponent<Slipper>();
                shoe.SeatOfOrigin=2;shoe.OwnerSlot=2;shoe.transform.position=_outside.transform.position+Vector3.up*.1f;
                storm.Release();Assert.IsTrue(_outside.IsWhirled);Assert.AreEqual(SlipperState.InFlight,shoe.State);
            }
            finally
            {
                AIController.PlayableMinX=bounds.x;AIController.PlayableMaxX=bounds.y;
                AIController.PlayableMinZ=bounds.z;AIController.PlayableMaxZ=bounds.w;
            }
        }
        sealed class Observer : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 2;
            public int LocalPeerId => 2;
            public bool IsSeatlessReferee => false;
        }
        [UnityTest] public IEnumerator ObserverCannotResolveTheWind()
        {
            yield return Open();
            var shoe=Track(new GameObject("Observer loose slipper")).AddComponent<Slipper>();
            shoe.SeatOfOrigin=1; shoe.OwnerSlot=1;
            shoe.transform.position=new Vector3(1,.15f,1); shoe.HostScatter(Vector3.zero);
            var at=shoe.transform.position; var provider=NetAuthority.Provider;
            try
            {
                NetAuthority.Provider=new Observer(); var storm=Storm(); storm.Release();
                Assert.IsFalse(_victim.IsWhirled); Assert.IsFalse(_victim.IsCarried);
                Assert.AreEqual(SlipperState.Loose,shoe.State); Assert.AreEqual(at,shoe.transform.position);
            }
            finally { NetAuthority.Provider=provider; }
        }
        [UnityTest] public IEnumerator AirburstCarriesDefenderAcrossAuthoredCourt()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled=false;
            foreach(var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled=false;
            foreach(var player in GameServices.Round.Players) { player.Intent.Clear(); player.Teleport(new Vector3(6,.12f,-6)); }
            var caster=GameServices.Round.PlayerAt(1); var victim=GameServices.Round.PlayerAt(0);
            caster.Teleport(new Vector3(0,.12f,-5)); caster.transform.rotation=Quaternion.identity;
            caster.AbilitySystem.BindHero("amihan");
            var art=RosterBook.Load().FindPersonArt("amihan");
            caster.GetComponent<Visual.CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            victim.Teleport(new Vector3(0,.12f,-2));
            var origin=victim.transform.position;
            var storm=AmihanStorm.Spawn(caster.transform.position,Vector3.forward,1,null); Track(storm.gameObject);
            var eye=Track(new GameObject("Airburst court witness")).AddComponent<Camera>();eye.enabled=false;
            eye.transform.position=new Vector3(5,3,-7);eye.transform.LookAt(new Vector3(0,1,0));eye.fieldOfView=65;
            storm.Release(); yield return new WaitForSeconds(.2f);
            Assert.IsTrue(victim.IsWhirled); Assert.Greater(victim.transform.position.y-origin.y,.4f);
            yield return GameplayShots.Render(eye,"airborne",false,"Logs/airburst-court-captures",victim);
            yield return new WaitForSeconds(1.3f);
            float travel=victim.transform.position.z-origin.z;
            Debug.Log("[AirburstCourt] defenderTravel="+travel+" endpoint="+victim.transform.position);
            Assert.Greater(travel,7f,"Strong wind crosses the actual court toward its confinement edge.");
            Assert.LessOrEqual(Mathf.Abs(victim.transform.position.z),Balance.ConfinementRadius+.2f);
            yield return GameplayShots.Render(eye,"landed",false,"Logs/airburst-court-captures",victim);
        }
        IEnumerator PrepareReset()
        {
            yield return Open(); var lata=GameServices.Round.Lata;
            _caster.Teleport(lata.transform.position+Vector3.back*.5f);
            lata.HostKnockDown(1);Assert.IsFalse(lata.IsUpright);
            _caster.Intent.Set(Verb.Grab,true);
        }
        [UnityTest] public IEnumerator WhirledDefenderCannotResetUntilExpiry()
        {
            yield return PrepareReset();_caster.ApplyWhirled();
            Assert.IsFalse(_caster.GetComponent<Carrier>().HasResetTarget);
            Assert.IsTrue(_caster.CanAct(),"Whirled is not a general stun.");
            yield return new WaitForSeconds(GameServices.Round.Lata.ResetChannelTime+.1f);
            Assert.IsFalse(GameServices.Round.Lata.IsUpright,"Whirled must prevent can resetting.");
            Assert.AreEqual(0,_caster.GetComponent<Carrier>().ChannelRatio);
            yield return new WaitForSeconds(2.6f);
            Assert.IsTrue(GameServices.Round.Lata.IsUpright,"Held reset can start fresh after the status expires.");
        }
        [UnityTest] public IEnumerator WhirledCancelsAnAlreadyRunningReset()
        {
            yield return PrepareReset();yield return new WaitForSeconds(.3f);
            Assert.Greater(_caster.GetComponent<Carrier>().ChannelRatio,0);
            _caster.ApplyWhirled();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,_caster.GetComponent<Carrier>().ChannelRatio);
            Assert.IsFalse(GameServices.Round.Lata.IsUpright);
        }
        [UnityTest] public IEnumerator WhirledHostGateRefusesTheSameReset()
        {
            yield return PrepareReset();var root=Track(new GameObject("Whirled reset authority"));root.SetActive(false);
            var router=root.AddComponent<Net.MatchRpc>();
            var gate=typeof(Net.MatchRpc).GetMethod("HostMayChannelReset",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.IsTrue((bool)gate.Invoke(router,new object[]{0}));
            _caster.ApplyWhirled();Assert.IsFalse((bool)gate.Invoke(router,new object[]{0}));
        }
    }
}
