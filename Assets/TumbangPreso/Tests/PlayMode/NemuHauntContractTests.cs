using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuHauntContractTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => !Host;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _prior;
        private Peer _peer;
        private CharacterMotor[] _players;
        private NemuHeroKit _kit;
        private GhostPetCompanion _pet;
        private AbilityContext _ctx;
        [UnitySetUp] public IEnumerator Before()
        {
            _prior = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            GameServices.Ensure(); NetAuthority.Provider = _peer = new Peer();
            GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            _players = new CharacterMotor[4];
            var locations = new[] { Vector3.zero, new Vector3(4,0,3), new Vector3(-4,0,3), new Vector3(0,0,-4) };
            for (int i=0; i<4; i++)
            {
                var root = new GameObject("Haunt seat " + i);
                _players[i] = root.AddComponent<CharacterMotor>(); _players[i].enabled = false;
                _players[i].PlayerSlot = i; _players[i].Mode = GameMode.HeroStrike;
                root.transform.position = locations[i]; GameServices.Round.Register(_players[i]);
            }
            var owner = _players[0]; var system = owner.gameObject.AddComponent<HeroAbilitySystem>();
            system.enabled = false; system.BindHero("nemu"); _kit = (NemuHeroKit)system.Kit;
            var visual = owner.gameObject.AddComponent<CharacterVisual>(); visual.enabled = false;
            var art = RosterBook.Load().FindPersonArt("nemu"); Assert.IsNotNull(art.PetModel);
            var petRoot = Object.Instantiate(art.PetModel);
            _pet = petRoot.GetComponent<GhostPetCompanion>() ?? petRoot.AddComponent<GhostPetCompanion>();
            _pet.Bind(owner.transform); _pet.enabled = false;
            typeof(CharacterVisual).GetProperty("Companion").SetValue(visual, _pet);
            _ctx = new AbilityContext(owner, null, null);
            typeof(HeroAbility).GetProperty("Windup").SetValue(_kit.Ultimate, 0f);
            Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator After()
        {
            _kit?.ResetForRound(_ctx); yield return PlayModeWorld.Reset(); NetAuthority.Provider = _prior;
        }
        private void Cast() { using (NetCue.SuppressRelay()) _kit.Ultimate.Activate(_ctx); }
        private void Step(float dt=.05f) { using (NetCue.SuppressRelay()) _kit.Ultimate.Tick(_ctx,dt); }
        [Test] public void AUnavailableCompanionRefusesHauntWithoutSpendingItsFifteenPoints()
        {
            var visual = _players[0].GetComponent<CharacterVisual>();
            typeof(CharacterVisual).GetProperty("Companion").SetValue(visual, null);
            _kit.AddUltimateCharge(15);
            Assert.IsFalse(_kit.Ultimate.CanActivate(_ctx));
            Assert.IsFalse(_kit.TryActivateUltimate(_ctx));
            Assert.AreEqual(15, _kit.UltimateCharge);
            Assert.IsFalse(_kit.Ultimate.IsActive);
            typeof(CharacterVisual).GetProperty("Companion").SetValue(visual, _pet);
            Assert.IsTrue(_kit.Ultimate.CanActivate(_ctx));
        }
        [Test] public void HauntChasesOnePlayerAtATimeAndReturnsAfterEveryoneIncludingCaster()
        {
            Assert.AreEqual(15, _kit.UltimateCost); Cast(); Assert.IsTrue(_pet.IsDevouring);
            Assert.IsEmpty(Object.FindObjectsByType<HeroHazards.SeanceVoidComponent>(FindObjectsSortMode.None));
            int[] hits = new int[4];
            for(int i=0;i<4;i++) { int seat=i; _players[i].StatusGained += (who,kind) => { if(kind==StatusKind.Haunted) hits[seat]++; }; }
            for(int tick=0; tick<400 && _kit.Ultimate.IsActive; tick++)
            {
                int before=0; foreach(int value in hits) before+=value;
                Step(); int after=0; foreach(int value in hits) after+=value;
                Assert.LessOrEqual(after-before,1,"One tick hit multiple players.");
            }
            CollectionAssert.AreEqual(new[]{1,1,1,1},hits);
            Assert.IsFalse(_kit.Ultimate.IsActive); Assert.IsFalse(_pet.IsDevouring); Assert.IsTrue(_pet.IsReturning);
        }
        [Test] public void OccludedPlayersAreNotHitAndTheChaseOutlivesTheRetiredSevenSecondField()
        {
            _players[2].gameObject.SetActive(false); _players[3].gameObject.SetActive(false);
            _players[1].transform.position = new Vector3(0,0,6);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="Haunt occlusion";
            wall.transform.position=new Vector3(0,1,4.6f); wall.transform.localScale=new Vector3(8,3,.5f);
            Physics.SyncTransforms(); Cast();
            for(int tick=0;tick<180;tick++) Step();
            Assert.IsTrue(_kit.Ultimate.IsActive); Assert.IsFalse(_players[1].IsHaunted);
            Object.DestroyImmediate(wall); Physics.SyncTransforms();
            for(int tick=0;tick<100 && _kit.Ultimate.IsActive;tick++) Step();
            Assert.IsTrue(_players[1].IsHaunted); Assert.IsFalse(_kit.Ultimate.IsActive);
        }
        [Test] public void ObserversCannotResolveContactsAndRoundResetReleasesTheMonster()
        {
            _peer.Host=false; Cast();
            for(int tick=0;tick<100;tick++) Step();
            foreach(var player in _players) Assert.IsFalse(player.IsHaunted);
            Assert.IsTrue(_pet.IsDevouring);
            _kit.ResetForRound(_ctx); Assert.IsFalse(_pet.IsDevouring); Assert.IsFalse(_kit.Ultimate.IsActive);
        }
        [Test] public void ReusedChaseSweepStopsAtAWallAndFailsClosedWhenItsBufferFills()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=new Vector3(2,1,0); wall.transform.localScale=new Vector3(.5f,3,8);
            Physics.SyncTransforms();
            var roomy=new RaycastHit[32];
            var safe=GhostPetMotion.Move(null,Vector3.zero,Vector3.right*5,roomy);
            Assert.Greater(safe.x,0); Assert.Less(safe.x,1.75f,"Chase sweep tunnelled through the wall.");
            var saturated=GhostPetMotion.Move(null,Vector3.zero,Vector3.right*5,new RaycastHit[1]);
            Assert.AreEqual(Vector3.zero,saturated,"An incomplete blocker query advanced the monster.");
            Object.DestroyImmediate(wall); Physics.SyncTransforms();
            var clear=GhostPetMotion.Move(null,Vector3.zero,Vector3.right*5,roomy);
            Assert.AreEqual(5,clear.x,.001f);
        }
        [Test] public void ADefendersFamiliarCanReachAnAttackerOutsideTheConfinementBox()
        {
            _players[0].IsDefender=true;
            _players[1].transform.position=new Vector3(7,0,0);
            _players[2].gameObject.SetActive(false); _players[3].gameObject.SetActive(false);
            Physics.SyncTransforms(); Cast();
            for(int tick=0;tick<200 && _kit.Ultimate.IsActive;tick++) Step();
            Assert.IsTrue(_players[1].IsHaunted); Assert.IsFalse(_kit.Ultimate.IsActive);
            Assert.AreEqual(Vector3.zero,_players[0].transform.position,"The chase moved Nemu herself.");
        }
    }
}
