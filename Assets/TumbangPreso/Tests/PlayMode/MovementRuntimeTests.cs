using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MovementRuntimeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private CharacterMotor _actor;
        private INetProvider _provider;
        private CustomRules _rules;
        private bool _pinned, _tutorial, _spectator, _bots;
        private int _seat;
        private GameObject Keep(GameObject go) { _objects.Add(go); return go; }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            _tutorial = GameLaunch.GuidedTutorial; _spectator = GameLaunch.Spectator; _bots = GameLaunch.AllBots; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = new SoloProvider();
            GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false; GameLaunch.SoloSeat = 1;
            SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.HeroStrike)); GameServices.Ensure(); GameServices.Round.Clear();
            var floor = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.transform.position = Vector3.down * .5f; floor.transform.localScale = new Vector3(40, 1, 40);
            var root = Keep(new GameObject("Movement revision actor", typeof(CharacterController)));
            var cc = root.GetComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
            _actor = root.AddComponent<CharacterMotor>(); _actor.PlayerSlot = 1; _actor.Mode = GameMode.HeroStrike; _actor.IsBot = false;
            _actor.transform.position = StandingPoint(); GameServices.Round.Register(_actor);
            var can = Keep(new GameObject("Movement revision can")); can.transform.position = new Vector3(6, 0, 6); GameServices.Round.Lata = can.AddComponent<Lata>();
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound(); SetRole(false);
            yield return SettleOnFloor();
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in _objects) if (go != null) Object.Destroy(go); _objects.Clear(); yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _provider; GameLaunch.GuidedTutorial = _tutorial; GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _bots; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules); if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private Vector3 StandingPoint()
        {
            var cc = _actor.GetComponent<CharacterController>();
            return new Vector3(0, cc.height * .5f + cc.skinWidth - cc.center.y + .05f, -2);
        }
        private IEnumerator SettleOnFloor()
        {
            Physics.SyncTransforms();
            for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();
            float until = Time.time + 1;
            while (!_actor.IsGrounded && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.IsTrue(_actor.IsGrounded, $"Fixture must settle: position={_actor.transform.position}, velocity={_actor.Velocity}, scale={Time.timeScale}.");
        }
        private void SetRole(bool defender)
        {
            GameServices.Match.ApplySnapshot(new int[4], defender ? 2 : 1, true);
            GameServices.Round.ApplySnapshot(90, true, GameServices.Match.DefenderSlot, true); _actor.Intent.Parked = false;
            Assert.AreEqual(defender, _actor.IsDefender, "Fixture must publish the intended defender slot.");
        }
        [UnityTest] public IEnumerator ActualMotorUsesExplicitSpeedsInBothModesAndRoles()
        {
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            foreach (bool defender in new[] { false, true })
            foreach (bool run in new[] { false, true })
            {
                _actor.Intent.Clear(); _actor.Mode = mode; _actor.CharacterIndex = run ? 7 : 2;
                SetRole(defender); _actor.Stamina.RefillAndClearFatigue(); _actor.Teleport(StandingPoint());
                yield return SettleOnFloor();
                _actor.Intent.Move = Vector2.up; _actor.Intent.Set(Verb.Sprint, run);
                Vector3 start = _actor.transform.position; float began = Time.fixedTime;
                for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
                float elapsed = Time.fixedTime - began;
                float measured = Vector3.Distance(start, _actor.transform.position) / elapsed;
                float expected = Stamina.MovementSpeed(defender, run);
                TestContext.WriteLine($"{mode}, defender={defender}, run={run}: measured {measured:F4} m/s, expected {expected:F4}");
                Assert.AreEqual(expected, measured, .08f);
                Assert.AreEqual(run, _actor.Stamina.IsSprinting);
            }
        }
        [UnityTest] public IEnumerator ActualFlatGroundJumpMatchesHeightAndAirTime()
        {
            float ground = _actor.transform.position.y; _actor.Intent.Set(Verb.Jump, true); _actor.Intent.BufferPress(Verb.Jump);
            yield return new WaitForFixedUpdate(); _actor.Intent.Set(Verb.Jump, false);
            float began = Time.fixedTime, peak = _actor.transform.position.y, until = Time.time + 2;
            Assert.IsFalse(_actor.IsGrounded, "The real jump input must leave the floor.");
            Assert.That(_actor.Velocity.y, Is.InRange(5.30f, 5.76f), "First launch sample allows one physics gravity step.");
            while (!_actor.IsGrounded && Time.time < until)
            {
                peak = Mathf.Max(peak, _actor.transform.position.y); yield return new WaitForFixedUpdate();
            }
            float air = Time.fixedTime - began;
            TestContext.WriteLine($"Actual jump height {peak - ground:F4} m, airborne {air:F4} s.");
            Assert.IsTrue(_actor.IsGrounded); Assert.That(peak - ground, Is.InRange(.78f, .96f)); Assert.That(air, Is.InRange(.54f, .64f));
        }
        [UnityTest] public IEnumerator ActualMotorFallSpeedCapsAtTwentyFive()
        {
            _actor.Intent.Clear();_actor.Teleport(new Vector3(0,100,-2));Physics.SyncTransforms();
            float until=Time.time+2.5f;bool reached=false;
            while(Time.time<until)
            {
                yield return new WaitForFixedUpdate();
                Assert.GreaterOrEqual(_actor.Velocity.y,-25.001f,"Ordinary falling must never exceed the configured cap.");
                if(_actor.Velocity.y<=-24.99f){reached=true;break;}
            }
            Assert.IsTrue(reached,"The real motor must reach terminal falling speed during a sufficiently long fall.");
            for(int i=0;i<8;i++)
            {yield return new WaitForFixedUpdate();Assert.AreEqual(-25,_actor.Velocity.y,.001f);}
            TestContext.WriteLine($"Actual terminal fall speed {-_actor.Velocity.y:F4} m/s.");
        }
        [UnityTest] public IEnumerator FatigueKeepsWalkSpeedWithoutSprintOrRegeneration()
        {
            Assert.IsTrue(_actor.Stamina.Spend(Balance.StaminaMax)); _actor.Intent.Move = Vector2.up; _actor.Intent.Set(Verb.Sprint, true);
            Vector3 start = _actor.transform.position; float began = Time.fixedTime;
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            float measured = Vector3.Distance(start, _actor.transform.position) / (Time.fixedTime - began);
            Assert.AreEqual(2.5f, measured, .08f); Assert.IsTrue(_actor.Stamina.IsFatigued);
            Assert.IsFalse(_actor.Stamina.IsSprinting); Assert.AreEqual(0, _actor.Stamina.Current);
        }
        [UnityTest] public IEnumerator ShoveAndLungeWorkWithZeroStaminaAndFatigue()
        {
            var verbs = _actor.gameObject.AddComponent<CombatVerbs>();
            Assert.IsTrue(_actor.Stamina.Spend(Balance.StaminaMax));
            _actor.Intent.Set(Verb.Lunge, true); _actor.Intent.BufferPress(Verb.Lunge);
            float until = Time.time + .25f;
            while (verbs.ShoveCooldownLeft <= 0 && Time.time < until) yield return null;
            Assert.Greater(verbs.ShoveCooldownLeft, 0, "Real shove input must work during fatigue.");
            Assert.AreEqual(0, _actor.Stamina.Current); Assert.IsTrue(_actor.Stamina.IsFatigued);
            _actor.Intent.Clear();
            // Reset only the cooldown so the authoritative replica path gets the same state.
            typeof(CombatVerbs).GetField("_shoveCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(verbs, 0f);
            Assert.IsTrue(verbs.HostResolveShove(_actor.transform.position, Vector3.forward));
            Assert.AreEqual(0, _actor.Stamina.Current);
            SetRole(true);
            Assert.IsTrue(verbs.HostResolveLunge(_actor.transform.position, Vector3.forward, 1));
            Assert.AreEqual(0, _actor.Stamina.Current); Assert.IsTrue(_actor.Stamina.IsFatigued);
        }

        [UnityTest] public IEnumerator RealLungeInputUsesTapAndFullHoldCooldowns()
        {
            SetRole(true); var verbs = _actor.gameObject.AddComponent<CombatVerbs>();
            _actor.Intent.Set(Verb.Lunge, true); yield return new WaitForSeconds(.04f);
            _actor.Intent.Set(Verb.Lunge, false);
            float tapUntil = Time.time + .2f; while (verbs.LungeCooldownLeft <= 0 && Time.time < tapUntil) yield return null;
            Assert.That(verbs.LungeCooldownLeft, Is.InRange(.38f, .51f));
            yield return new WaitForSeconds(.65f);
            _actor.Intent.Set(Verb.Lunge, true); yield return new WaitForSeconds(.55f);
            _actor.Intent.Set(Verb.Lunge, false);
            float fullUntil = Time.time + .2f; while (verbs.LungeCooldownLeft <= 0 && Time.time < fullUntil) yield return null;
            Assert.That(verbs.LungeCooldownLeft, Is.InRange(2.38f, 2.51f));
        }
    }
}
