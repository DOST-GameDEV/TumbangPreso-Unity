using System.Collections;
using System.Linq;
using System.Reflection;
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
    /// <summary>
    /// Nemu's PRUSISYON (`docs/reports/nemu-ultimate-2026-10-11/direction.md` section 8): who is possessed, by whom, for how
    /// long, and that an observer can neither cause it nor keep it. Replaces the retired KURO: HAUNT! contract.
    /// </summary>
    public sealed class NemuPrusisyonTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => !Host;
            public int LocalSlot => Host ? 0 : 3;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _prior;
        private Peer _peer;
        private CharacterMotor[] _players;
        private NemuHeroKit _kit;
        private AbilityContext _ctx;

        [UnitySetUp] public IEnumerator Before()
        {
            _prior = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            GameServices.Ensure(); NetAuthority.Provider = _peer = new Peer();
            GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            _players = new CharacterMotor[4];
            var locations = new[] { Vector3.zero, new Vector3(4, 0, 3), new Vector3(-4, 0, 3), new Vector3(0, 0, -4) };
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("Prusisyon seat " + i);
                _players[i] = root.AddComponent<CharacterMotor>(); _players[i].enabled = false;
                _players[i].PlayerSlot = i; _players[i].Mode = GameMode.HeroStrike;
                root.transform.position = locations[i]; GameServices.Round.Register(_players[i]);
            }
            _players[3].IsDefender = true;
            var system = _players[0].gameObject.AddComponent<HeroAbilitySystem>();
            system.enabled = false; system.BindHero("nemu"); _kit = (NemuHeroKit)system.Kit;
            _ctx = new AbilityContext(_players[0], null, null);
            typeof(HeroAbility).GetProperty("Windup").SetValue(_kit.Ultimate, 0f);
        }

        [UnityTearDown] public IEnumerator After()
        {
            _kit?.ResetForRound(_ctx); yield return PlayModeWorld.Reset(); NetAuthority.Provider = _prior;
        }

        private void Cast() { using (NetCue.SuppressRelay()) _kit.Ultimate.Activate(_ctx); }

        [Test] public void TheUltimateKeepsItsIdentityAndPossessesEveryOtherPlayerForFifteenSeconds()
        {
            Assert.AreEqual("nemu_ultimate", _kit.Ultimate.Id);
            Assert.AreEqual("PRUSISYON", _kit.Ultimate.Name);
            Assert.AreEqual(15, _kit.UltimateCost);
            int gained = 0;
            foreach (var p in _players) p.StatusGained += (who, kind) => { if (kind == StatusKind.Possessed) gained++; };
            Cast();
            Assert.IsFalse(_players[0].IsPossessed, "Nemu possessed herself.");
            for (int i = 1; i < 4; i++)
            {
                Assert.IsTrue(_players[i].IsPossessed, "Seat " + i + " was not possessed.");
                Assert.AreEqual(15f, _players[i].PossessedLeft, 1e-4f);
                Assert.AreEqual(15f, _players[i].StatusLeft(StatusKind.Possessed), 1e-4f);
                Assert.AreEqual(0f, _players[i].PossessionElapsed, 1e-4f);
            }
            Assert.AreEqual(3, gained);
        }

        [Test] public void AnObserverCannotPossessAnybody()
        {
            _peer.Host = false; Cast();
            foreach (var p in _players) Assert.IsFalse(p.IsPossessed);
        }

        [Test] public void ARefreshNeverAddsAndCleanseOrRoundClearEndsIt()
        {
            _players[1].ApplyPossessed();
            _players[1].ApplyNetworkPossessed(9f);
            _players[1].ApplyPossessed();
            Assert.AreEqual(15f, _players[1].PossessedLeft, 1e-4f, "A refresh added time instead of Max().");
            _players[1].CleanseStatuses();
            Assert.IsFalse(_players[1].IsPossessed);
            _players[2].ApplyPossessed(); _players[2].ClearStatuses();
            Assert.IsFalse(_players[2].IsPossessed);
        }

        [Test] public void TheWireValueIsBoundedAndItsOwnClockRunsOut()
        {
            _players[1].ApplyNetworkPossessed(99f);
            Assert.AreEqual(15f, _players[1].PossessedLeft, 1e-4f);
            _players[1].ApplyNetworkPossessed(float.NaN);
            Assert.IsFalse(_players[1].IsPossessed);
            _players[1].ApplyNetworkPossessed(.1f);
            typeof(CharacterMotor).GetMethod("StepStatuses", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_players[1], new object[] { .2f });
            Assert.IsFalse(_players[1].IsPossessed);
        }

        [Test] public void TheGhostsPullTowardTheWorstPlaceAndNeverMoveAStunnedBody()
        {
            Cast();
            // Let the rider settle so the pull is at drift strength.
            foreach (var p in _players) p.ApplyNetworkPossessed(15f - 3.0f);
            // An attacker toward the taya (seat 3 at (0,0,-4)).
            Assert.IsTrue(_players[1].PossessionPull(out var pull));
            var toTaya = (_players[3].transform.position - _players[1].transform.position).normalized;
            Assert.Greater(Vector3.Dot(pull.normalized, toTaya), Mathf.Cos(30f * Mathf.Deg2Rad), "Pulled away from the taya.");
            Assert.Greater(pull.magnitude, .3f);
            // The taya away from the can (no can here: the origin).
            Assert.IsTrue(_players[3].PossessionPull(out var tayaPull));
            Assert.Greater(Vector3.Dot(tayaPull.normalized, Vector3.back), Mathf.Cos(30f * Mathf.Deg2Rad), "The taya was not pulled off the can.");
            _players[1].ApplyTagged();
            Assert.IsFalse(_players[1].PossessionPull(out _), "A stunned body was still dragged.");
        }

        [Test] public void TheContestHoldsAgainstTheFightAndWearsDownTheGrip()
        {
            _players[1].ApplyPossessed();
            // 3.8 s in: seat 1 is drifting (its second surge is at 2.4 to 3.15, its next tell from 4.4).
            Assert.AreEqual(NemuRules.Phase.Drift, NemuRules.PhaseAt(3.8f, 1));
            _players[1].ApplyNetworkPossessed(15f - 3.8f);
            var wish = typeof(CharacterMotor).GetMethod("PossessedWish", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsTrue(_players[1].PossessionPull(out var pull));
            var handsOff = (Vector3)wish.Invoke(_players[1], new object[] { Vector3.zero, 1f / 60f });
            Assert.AreEqual(pull.magnitude, handsOff.magnitude, .02f, "Hands off, the ghost should walk the body at its pull.");
            var against = -pull.normalized;
            Vector3 fought = Vector3.zero;
            for (int i = 0; i < 120; i++) fought = (Vector3)wish.Invoke(_players[1], new object[] { against, 1f / 60f });
            Assert.IsTrue(_players[1].PossessionResisting);
            Assert.Less(_players[1].PossessionGrip, 1f, "Fighting did not wear the grip down.");
            Assert.Greater(Vector3.Dot(fought, against), .5f, "Fighting straight against the drift should keep most of a walk.");
        }

        [Test] public void AStrayHauntMessageChangesNothing()
        {
            Assert.IsFalse(_kit.ReceiveHaunt(_players[0], 4, 99, Vector3.one, 5, 90));
            _kit.RestoreFamiliar(_players[0], 2, Vector3.one, 5, 90);
            Assert.IsFalse(_kit.Ultimate.IsActive);
            foreach (var p in _players) Assert.IsFalse(p.IsPossessed);
        }
    }

    /// <summary>
    /// The contest in a real match on a real map: real physics steps move the bodies. A hands-off possessed attacker walks
    /// toward the taya; one fighting it still makes ground but less than free; a stunned one does not move; the rider is
    /// on them. Records `Logs/nemu-possession.csv`.
    /// </summary>
    public sealed class NemuPossessionMovementTests
    {
        private bool _allBots, _spectator, _pinned;
        private int _soloSeat;
        private CustomRules _rules;
        private INetProvider _provider;

        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider;
            _allBots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _soloSeat = GameLaunch.SoloSeat;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSeconds(.4f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            Camera.main.GetComponent<CameraSystem.CameraRig>().SetAimSource(CameraSystem.AimSource.Movement);
            yield return new WaitForSeconds(.15f);
        }

        [UnityTearDown] public IEnumerator After()
        {
            NetAuthority.Provider = _provider;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _allBots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _soloSeat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        private static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        [UnityTest, Timeout(90000)]
        public IEnumerator APossessedAttackerIsDraggedTowardTheTayaButCanFightIt()
        {
            var round = GameServices.Round;
            var taya = round.Players.First(p => p.IsDefender);
            var victim = round.Players.First(p => !p.IsDefender && p.PlayerSlot != 1);
            foreach (var p in round.Players) { p.Intent.Clear(); p.Intent.Parked = false; p.Intent.FaceAimPoint = false; }
            Vector3 can = round.Lata.transform.position;
            taya.Teleport(can + new Vector3(0f, .12f, 1.2f));
            Vector3 start = can + new Vector3(0f, .12f, 6.0f);
            victim.Teleport(start);
            yield return new WaitForSeconds(.3f);

            // Hands off: the ghost walks them toward the taya.
            victim.ApplyPossessed();
            Assert.IsNotNull(victim.GetComponentInChildren<PossessedRider>(), "Nobody can see the rider.");
            yield return new WaitForSeconds(.6f);
            float d0 = Flat(victim.transform.position, taya.transform.position);
            yield return new WaitForSeconds(1.0f);
            float d1 = Flat(victim.transform.position, taya.transform.position);
            float drift = d0 - d1;
            Assert.Greater(drift, .6f, "A hands-off possessed attacker was not walked toward the taya.");

            // Unpossessed, walking away from the taya for a second: the free reference.
            victim.CleanseStatuses(); Assert.IsFalse(victim.IsPossessed);
            victim.Teleport(start); yield return new WaitForSeconds(.3f);
            Vector3 away = (start - taya.transform.position); away.y = 0; away.Normalize();
            victim.Intent.Move = new Vector2(away.x, away.z);
            Vector3 f0 = victim.transform.position; yield return new WaitForSeconds(1.0f);
            float free = Vector3.Dot(victim.transform.position - f0, away);
            victim.Intent.Move = Vector2.zero;

            // Possessed and fighting it the same way: still makes ground, but less.
            victim.Teleport(start); yield return new WaitForSeconds(.3f);
            victim.ApplyPossessed(); yield return new WaitForSeconds(.6f);
            victim.Intent.Move = new Vector2(away.x, away.z);
            Vector3 r0 = victim.transform.position; yield return new WaitForSeconds(1.0f);
            float resisted = Vector3.Dot(victim.transform.position - r0, away);
            bool wearing = victim.PossessionGrip < 1f;
            victim.Intent.Move = Vector2.zero;

            // Stunned: the ghost waits.
            victim.ApplyTagged(); yield return new WaitForSeconds(.15f);
            Vector3 s0 = victim.transform.position; yield return new WaitForSeconds(.8f);
            float stunned = Flat(victim.transform.position, s0);

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines("Logs/nemu-possession.csv", new[] { "measure,metres",
                $"hands_off_drift_toward_taya_1s,{drift:F3}", $"free_walk_away_1s,{free:F3}",
                $"possessed_fighting_away_1s,{resisted:F3}", $"stunned_moved_0.8s,{stunned:F3}" });

            Assert.Greater(free, .5f, "The reference walk did not move.");
            Assert.Greater(resisted, .15f * free, "Fighting it should still make ground: never a takeover.");
            Assert.Less(resisted, .95f * free, "Fighting it should cost speed: the ghost contests the steering.");
            Assert.IsTrue(wearing, "Fighting it did not wear the ghost's grip.");
            Assert.Less(stunned, .1f, "A stunned possessed body was dragged.");
        }
    }
}
