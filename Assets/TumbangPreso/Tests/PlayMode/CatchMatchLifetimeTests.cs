using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class CatchMatchLifetimeTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private bool _network, _reduced, _cinematic, _pinned, _tutorial;
        private int _solo;
        private CustomRules _rules;
        private CharacterMotor _taya, _victim;
        private CatchReconstruction _view;
        private MatchDirector _match;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; _network = SceneFlow.Networked;
            _solo = GameLaunch.SoloSeat; _tutorial = GameLaunch.GuidedTutorial;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            _reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            _cinematic = Settings.SettingsStore.Current.CinematicCameraMotion;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            SceneFlow.Networked = false; NetAuthority.Provider = new SoloProvider();
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players)
            { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
            float until = Time.unscaledTime + 3;
            while (GameServices.Round.Lata.IsProtected && Time.unscaledTime < until) yield return null;
            _taya = GameServices.Round.PlayerAt(0); _victim = GameServices.Round.PlayerAt(1);
            _view = Object.FindAnyObjectByType<CatchReconstruction>(); _match = GameServices.Match;
            Assert.IsNotNull(_view); Assert.Greater(_match.PresentationMatchId, 0);
            Assert.IsTrue(Camera.main.GetComponent<CameraRig>().IsFollowing(_victim));
            var can = GameServices.Round.Lata.transform.position;
            _victim.Teleport(can + Vector3.back * 2.2f);
            _taya.Teleport(can + Vector3.back * 3.2f); _taya.transform.forward = Vector3.forward;
            _victim.transform.forward = Vector3.forward;
            for (int i = 2; i < 4; i++) GameServices.Round.PlayerAt(i).Teleport(can + new Vector3(-5, 0, i * 2));
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(_victim.IsTaggable());
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_view != null) _view.End();
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.SoloSeat = _solo; GameLaunch.GuidedTutorial = _tutorial;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            Settings.SettingsStore.Current.ReducedUiMotion = _reduced;
            Settings.SettingsStore.Current.CinematicCameraMotion = _cinematic;
            yield return PlayModeWorld.Reset();
        }
        [UnityTest, Timeout(90000)]
        public IEnumerator PhaistersDollTagStartsTheActualVictimCatchReplay()
        {
            var doll = Abilities.VoodooDollBody.Spawn(_taya, _taya.transform.position,
                _taya.transform.eulerAngles.y, brain: false);
            Assert.IsNotNull(doll);
            doll.IsDefender = true; doll.RoundActive = true; doll.enabled = false;
            _taya.Teleport(_taya.transform.position+Vector3.left*6);
            yield return new WaitForSeconds(.4f);
            var history = Object.FindAnyObjectByType<MatchPoseHistory>();
            Assert.IsNotNull(history.ForSeat(doll.PlayerSlot), "Companion contact needs its own real pose history.");
            Assert.IsTrue(doll.GetComponent<CombatVerbs>().HostResolvePunch(doll.transform.position,doll.transform.forward));
            Assert.Greater(_victim.StunLeft,0);
            Assert.IsTrue(_view.Playing,"A doll's accepted tag must show the same victim catch replay as a player's.");
        }

        private void Catch()
        {
            Assert.IsTrue(_taya.GetComponent<CombatVerbs>().HostResolvePunch(_taya.transform.position, _taya.transform.forward));
            Assert.IsTrue(_view.Playing, "Accepted contact must begin the actual retained catch view.");
        }
        private void Adopt(bool fresh)
        {
            long previous = _match.PresentationMatchId; int round = _match.RoundNumber;
            NetAuthority.Provider = new Client();
            _match.AdoptPresentationMatch(fresh ? previous + 1 : previous);
            Assert.AreEqual(round, _match.RoundNumber);
            Assert.AreEqual(fresh ? previous + 1 : previous, _match.PresentationMatchId);
        }
        private static void SetUltimate(bool active) =>
            typeof(SharedUltimatePhase).GetProperty("Active").SetValue(SharedUltimatePhase.Ensure(), active);
        [UnityTest] public IEnumerator AnUltimateEndsTheTagReplayAndAPendingTagNeverStartsOne()
        {
            Catch(); float stun = _victim.StunLeft;
            try
            {
                SetUltimate(true);
                _view.SendMessage("LateUpdate");
                Assert.IsFalse(_view.Playing, "The ultimate owns the screen: the tag replay ends.");
                Assert.AreEqual(stun, _victim.StunLeft, "Only the picture goes; the tag's recovery stands.");
                MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, _victim.transform.position);
                _view.SendMessage("LateUpdate");
                Assert.IsFalse(_view.Playing, "A tag during an ultimate starts no replay over it.");
            }
            finally { SetUltimate(false); }
            _view.SendMessage("LateUpdate");
            Assert.IsFalse(_view.Playing, "And it does not come back when the ultimate ends.");
            yield return null;
        }
        [UnityTest] public IEnumerator FreshSameRoundIdentityRetiresAnActiveCatchBeforeOldRecoveryArrives()
        {
            Catch(); float stun = _victim.StunLeft; Adopt(true);
            _view.SendMessage("LateUpdate");
            Assert.IsFalse(_view.Playing, "A previous match's detached catch survived a fresh identity at the same round.");
            Assert.AreEqual(stun, _victim.StunLeft, "Presentation retirement must not clear accepted recovery.");
            yield return null;
        }
        [UnityTest] public IEnumerator FreshSameRoundIdentityCannotUseAPendingOldContactForLaterState()
        {
            Vector3 at = _victim.transform.position;
            MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, at);
            Assert.IsFalse(_view.Playing);
            Adopt(true);
            // Flair, new match identity and body recovery can arrive in separate
            // transport callbacks before the next presentation tick.
            _victim.ApplyStagger(Balance.TagStunTime); _victim.Teleport(_victim.SpawnPosition);
            _view.SendMessage("LateUpdate");
            Assert.IsFalse(_view.Playing, "A retired match's pending contact attached to later body state.");
            Assert.Greater(_victim.StunLeft, .3f); yield return null;
        }
        [UnityTest] public IEnumerator SameIdentityKeepsAnAlreadyAcceptedCatchAndRecovery()
        {
            Catch(); float stun = _victim.StunLeft; Adopt(false);
            _view.SendMessage("LateUpdate");
            Assert.IsTrue(_view.Playing); Assert.AreEqual(stun, _victim.StunLeft);
            yield return null;
        }
        [UnityTest] public IEnumerator AnOldPlayingCatchCannotSuppressANewMatchTagEvent()
        {
            Catch(); Adopt(true); _victim.ClearStun();
            MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, _victim.transform.position);
            Assert.IsFalse(_view.Playing, "The old view must retire before the same-victim duplicate shortcut.");
            Assert.IsTrue((bool)typeof(CatchReconstruction).GetField("_pending", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_view),
                "A legitimate new-match tag must still wait for its body state.");
            yield return null;
        }
    }
}
