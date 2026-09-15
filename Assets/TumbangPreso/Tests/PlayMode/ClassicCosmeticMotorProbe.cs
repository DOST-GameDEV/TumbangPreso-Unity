using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ClassicCosmeticMotorProbe
    {
        private bool _bots,_spectator,_pinned;private int _seat;
        private CustomRules _rules;private INetProvider _net;
        [UnitySetUp]public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown]public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator AllTwelveClassicPicksWalkTheSameDistanceThroughTheRealMotor()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.Classic);
            NetAuthority.Provider=new SoloProvider();GameServices.Round.BeginRound();
            var walker=GameServices.Round.PlayerAt(1);walker.Mode=GameMode.Classic;walker.IsDefender=false;
            foreach(var player in GameServices.Round.Players)
                if(player!=walker)player.Teleport(new Vector3(-12+player.PlayerSlot*4,.12f,8));
            float reference=-1;
            for(int index=0;index<Roster.ClassicPeople.Count;index++)
            {
                walker.Intent.Clear();walker.Intent.Parked=false;walker.CharacterIndex=index;
                walker.ClearStun();walker.ClearTrip();walker.Teleport(new Vector3(0,.12f,-10));
                walker.Intent.Move=Vector2.up;
                for(int frame=0;frame<20;frame++)yield return new WaitForFixedUpdate();
                var start=walker.transform.position;
                for(int frame=0;frame<20;frame++)yield return new WaitForFixedUpdate();
                float distance=Vector2.Distance(new Vector2(start.x,start.z),new Vector2(walker.transform.position.x,walker.transform.position.z));
                Assert.Greater(distance,.5f,"The real motor never moved for "+Roster.ClassicPeople[index].Id);
                if(reference<0)reference=distance;
                Assert.That(distance,Is.EqualTo(reference).Within(.02f),Roster.ClassicPeople[index].Id+" still changes Classic movement.");
                Debug.Log("[ClassicCosmetic] "+Roster.ClassicPeople[index].Id+" distance="+distance.ToString("F5"));
            }
            walker.Intent.Clear();
        }
    }
}
