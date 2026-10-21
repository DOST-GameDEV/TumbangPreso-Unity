using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class CurrentPauseExitTests
    {
        private INetProvider _provider;
        private bool _networked;
        private CustomRules _rules;
        private bool _pinned;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; _networked = SceneFlow.Networked;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.Classic));
            NetAuthority.Provider = new SoloProvider(); SceneFlow.Networked = false;
            GameServices.Ensure();
        }
        [UnityTearDown] public IEnumerator After()
        {
            HalftimePresentation.Instance?.End(false);
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _provider; SceneFlow.Networked = _networked;
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else { SceneFlow.AdoptRemoteRules(_rules); SceneFlow.UnpinSelectedRules(); }
            PresentationClock.RequestScale(1);
        }
        [UnityTest, Timeout(45000)] public IEnumerator CurrentLeaveButtonRetiresTheLiveRoundAndLoadsHome()
            => Leave(false);
        [UnityTest, Timeout(45000)] public IEnumerator CurrentLeaveButtonCancelsTheBreakWithoutADeferredNextRound()
            => Leave(true);
        private IEnumerator Leave(bool intermission)
        {
            // An arena-classified minimal world exercises the actual cursor-close branch.
            // This does not claim acceptance of the populated shipping map.
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita));
            Assert.IsTrue(SceneFlow.InMatch);
            var round = GameServices.Round; var match = GameServices.Match;
            var owner = new GameObject("Current pause exit actor");
            var actor = owner.AddComponent<CharacterMotor>(); actor.enabled = false; actor.PlayerSlot = 0;
            round.Register(actor); match.StartMatch(); round.BeginRound();
            var watcher = owner.AddComponent<PauseWatcher>(); watcher.enabled = false; watcher.Local = actor;
            if (intermission)
            {
                round.EndRound(); match.BeginIntermission();
                Assert.IsTrue(HalftimePresentation.Playing); Assert.IsTrue(PresentationClock.Held);
            }
            else Assert.IsTrue(round.RoundActive);
            int starts = 0, ends = 0;
            System.Action<int, int> started = (_, __) => starts++;
            System.Action<int> ended = _ => ends++;
            match.RoundStarted += started; match.MatchEnded += ended;
            try
            {
                var pause = Panel.Open<PausePanel>(watcher); pause.Local = actor;
                yield return null;
                var canvas = (Canvas)typeof(Panel).GetField("Canvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pause);
                var leave = canvas.GetComponentsInChildren<Button>().Single(button => button.name == "LeaveMatch");
                Assert.IsTrue(leave.IsInteractable()); Assert.IsTrue(Panel.AnyOpen);
                leave.onClick.Invoke();
                float until = Time.realtimeSinceStartup + 20;
                while (Time.realtimeSinceStartup < until && (TumpHub.Current == null || !(TumpHub.Current.Top is HubHome))) yield return null;
                Assert.IsNotNull(TumpHub.Current); Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.IsFalse(SceneFlow.InMatch); Assert.IsFalse(Panel.AnyOpen);
                Assert.IsFalse(round.RoundActive); Assert.IsFalse(match.MatchInProgress);
                Assert.IsFalse(match.IsWarmupBuffer); Assert.IsFalse(HalftimePresentation.Playing);
                Assert.IsFalse(PresentationClock.Held); Assert.AreEqual(1, Time.timeScale);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState); Assert.IsTrue(Cursor.visible);
                Assert.AreEqual(0, round.Players.Count);
                if (intermission) yield return new WaitForSecondsRealtime(HalftimePresentation.BreakDuration + .5f);
                else yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, starts, "The abandoned arena advanced after HOME loaded.");
                Assert.AreEqual(0, ends, "Leaving must not manufacture a completed result.");
                Assert.IsFalse(round.RoundActive); Assert.IsFalse(match.MatchInProgress);
            }
            finally { match.RoundStarted -= started; match.MatchEnded -= ended; }
        }
    }
}
