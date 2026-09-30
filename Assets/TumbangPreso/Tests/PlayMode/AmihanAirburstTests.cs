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
        [UnityTest] public IEnumerator AcceptedWindupDropsAndLiftsOnlyAtRelease()
        {
            yield return Open();
            var shoe=Track(new GameObject("Airburst held slipper")).AddComponent<Slipper>();
            shoe.SeatOfOrigin=1; shoe.OwnerSlot=1; Assert.IsTrue(shoe.HostForceEquip(_victim));
            var kit=new AmihanHeroKit();
            var ctx=new AbilityContext(_caster,_caster.GetComponent<Carrier>(),_caster.GetComponent<CombatVerbs>());
            kit.Ultimate.Activate(ctx); Assert.IsTrue(kit.Ultimate.IsWindingUp);
            var storm=Object.FindFirstObjectByType<AmihanStorm>(); Assert.IsNotNull(storm); Track(storm.gameObject);
            kit.Ultimate.Tick(ctx,2.4f); Assert.IsFalse(storm.Released); Assert.IsFalse(_victim.IsWhirled);
            Assert.AreEqual(SlipperState.Held,shoe.State);
            Vector3 start=_victim.transform.position;
            kit.Ultimate.Tick(ctx,.11f); Assert.IsTrue(storm.Released); Assert.IsTrue(_victim.IsWhirled);
            Assert.IsNull(_victim.GetComponent<Carrier>().Held);
            Assert.AreEqual(SlipperState.InFlight,shoe.State); Assert.AreEqual(-1,shoe.ThrowerSlot);
            float high=start.y;
            for(int n=0;n<35;n++) { yield return new WaitForFixedUpdate(); high=Mathf.Max(high,_victim.transform.position.y); }
            Assert.Greater(high-start.y,.6f,"A visible body arc, not a road-level slide.");
            Assert.Greater(_victim.transform.position.z-start.z,5f,"The accepted carry must actually move the motor quickly.");
            yield return new WaitForSeconds(2);
            Assert.AreEqual(SlipperState.Loose,shoe.State,"Existing host flight must finish with a retrievable slipper.");
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
    }
}
