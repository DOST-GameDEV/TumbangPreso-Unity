using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class IceTractionProbe
    {
        private bool _allBots,_spectator,_pinned;
        private int _soloSeat;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _allBots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_soloSeat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=_allBots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_soloSeat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest]
        public IEnumerator FrozenFootingCarriesMomentumAndOverlappingSheetsReleaseIndependently()
        {
            yield return PlayModeWorld.Reset();
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSeconds(.4f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))reader.enabled=false;
            var who=GameServices.Round.PlayerAt(1);who.IsBot=true;
            var start=new Vector3(-5,.12f,-5);
            who.Teleport(start);who.Intent.Clear();who.Intent.Parked=false;who.Intent.Move=Vector2.up;
            yield return new WaitForSeconds(.8f);
            Assert.Greater(who.Velocity.magnitude,1,"The dry reference never moved.");
            who.Intent.Move=Vector2.zero;
            var dryStart=who.transform.position;
            yield return new WaitForSeconds(.4f);
            float dry=(who.transform.position-dryStart).magnitude;
            who.Teleport(start);
            var first=HeroHazards.SpawnIceSheet(start,2.3f,6,0);
            who.Intent.Move=Vector2.up;
            yield return new WaitForSeconds(.8f);
            Assert.IsTrue(who.IsOnIce);
            Assert.Less(who.SpeedMultiplier,1);
            who.Intent.Move=Vector2.zero;var iceStart=who.transform.position;
            yield return new WaitForSeconds(.16f);
            Assert.Greater(new Vector2(who.Velocity.x,who.Velocity.z).magnitude,.35f,"Releasing input stopped instantly on ice.");
            yield return new WaitForSeconds(.4f);
            float ice=(who.transform.position-iceStart).magnitude;
            Assert.Greater(ice,dry+.10f,"Ice did not measurably carry momentum beyond the dry stop.");
            var second=HeroHazards.SpawnIceSheet(who.transform.position,2.3f,6,0);
            yield return null;
            Object.Destroy(first);yield return null;yield return null;
            Assert.IsTrue(who.IsOnIce,"Leaving one sheet cleared another overlapping sheet.");
            Object.Destroy(second);yield return null;yield return null;
            Assert.IsFalse(who.IsOnIce);
            Assert.AreEqual(1,who.SpeedMultiplier,.001f);
            var own=HeroHazards.SpawnIceSheet(who.transform.position,2.3f,1,who.PlayerSlot);
            yield return null;
            Assert.IsFalse(who.IsOnIce,"The caster lost its existing immunity to its own sheet.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ice-traction-distance.csv",$"surface,stopping_distance\ndry,{dry:F4}\nice,{ice:F4}\n");
            who.Intent.Clear();Object.Destroy(own);
        }
    }
}
