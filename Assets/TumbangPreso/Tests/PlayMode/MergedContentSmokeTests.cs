using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Map;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MergedContentSmokeTests
    {
        private GameObject _source, _stage;
        private bool _allBots, _spectator, _pinned;
        private int _seat;
        private Core.CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            _allBots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator;
            _seat = GameLaunch.SoloSeat; _pinned = UI.SceneFlow.RulesPinned;
            _rules = UI.SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_source != null) Object.Destroy(_source);
            if (_stage != null) Object.Destroy(_stage);
            yield return null;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _allBots; GameLaunch.Spectator = _spectator;
            GameLaunch.SoloSeat = _seat;
            UI.SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) UI.SceneFlow.PinSelectedRules(_rules); else UI.SceneFlow.UnpinSelectedRules();
        }

        [UnityTest] public IEnumerator ReplayCopyClearsLiveTransientCoatsWithoutChangingOtherArtState()
        {
            _source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var live = _source.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetFloat("_FlashAmount", .9f); block.SetFloat("_CaughtAmount", .7f);
            block.SetFloat("_FrostAmount", .6f); block.SetFloat("_RimStrength", 2.1f);
            block.SetColor("_RimColor", Color.cyan); live.SetPropertyBlock(block);
            _stage = new GameObject("Detached replay surface"); _stage.SetActive(false);
            var track = new MatchPoseHistory.Track(null, _source);
            track.Record(10); track.Record(10.05f);
            var copy = track.Clone(_stage.transform);
            Assert.IsNotNull(copy); Assert.AreEqual(1, copy.Renderers.Length);
            var copied = new MaterialPropertyBlock(); copy.Renderers[0].GetPropertyBlock(copied);
            Assert.Zero(copied.GetFloat("_FlashAmount"));
            Assert.Zero(copied.GetFloat("_CaughtAmount"));
            Assert.Zero(copied.GetFloat("_FrostAmount"));
            Assert.AreEqual(2.1f, copied.GetFloat("_RimStrength"));
            Assert.AreEqual(Color.cyan, copied.GetColor("_RimColor"));
            Assert.AreSame(live.sharedMaterial, copy.Renderers[0].sharedMaterial);
            var unchanged = new MaterialPropertyBlock(); live.GetPropertyBlock(unchanged);
            Assert.AreEqual(.9f, unchanged.GetFloat("_FlashAmount"));
            Assert.AreEqual(.7f, unchanged.GetFloat("_CaughtAmount"));
            Assert.AreEqual(.6f, unchanged.GetFloat("_FrostAmount"));
            Assert.IsNull(copy.Root.GetComponent<Collider>(), "Replay copies must remain render-only.");
            yield return null;
        }

        [UnityTest] public IEnumerator MergedArenaSceneAndHeroCatalogResolveActualImportedArt()
        {
            var book = RosterBook.Load(); Assert.IsNotNull(book);
            foreach (var hero in Core.Roster.HeroPeople)
            {
                var entry = book.FindPersonArt(hero.Id);
                Assert.IsNotNull(entry, hero.Id + " roster entry missing");
                Assert.IsNotNull(entry.Model, hero.Id + " model missing");
                Assert.IsTrue(entry.Model.GetComponentsInChildren<Renderer>(true).Any(), hero.Id + " imported model has no renderers");
            }
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Arena, Core.GameMode.HeroStrike);
            var stage = ArenaStage.Instance;
            Assert.IsNotNull(stage); Assert.Greater(stage.LayoutCount, 1);
            Assert.IsNotNull(ArenaFallRecovery.Instance);
            Assert.AreEqual(stage.MoveFloor, Net.MatchRpc.MoveFloorY);
            Assert.IsTrue(AIController.EdgeSense);
            Assert.IsNotNull(CharacterMotor.MapFall);
            Assert.IsNotNull(Abilities.PaeteVine.MapCatch);
            Assert.IsNotNull(GameServices.Round.Lata);
            Assert.AreEqual(4, GameServices.Round.Players.Count);
            foreach (var player in GameServices.Round.Players)
                Assert.IsNotNull(player.GetComponent<Visual.CharacterVisual>().Model);
        }
    }
}
