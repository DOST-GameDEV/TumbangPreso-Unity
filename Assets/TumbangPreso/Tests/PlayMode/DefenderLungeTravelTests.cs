using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DefenderLungeTravelTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        CharacterMotor _taya;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }

        CharacterMotor Seat(int slot, Vector3 at)
        {
            var go = Track(new GameObject("LungeSeat" + slot, typeof(CharacterController)));
            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = slot;
            motor.IsDefender = slot == MatchRules.DefenderSlotFor(1);
            motor.IsBot = false;
            motor.Mode = UI.SceneFlow.SelectedMode;
            go.AddComponent<Carrier>();
            go.AddComponent<CombatVerbs>();
            var cc = go.GetComponent<CharacterController>();
            at.y = -(cc.center.y - cc.height * 0.5f - cc.skinWidth) + 0.05f;
            go.transform.position = at;
            Physics.SyncTransforms();
            GameServices.Round.Register(motor);
            return motor;
        }

        IEnumerator Open(GameMode mode)
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            GameServices.Ensure();
            GameServices.Round.Clear();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.localScale = new Vector3(30, 1, 30);
            floor.transform.position = new Vector3(0, -0.5f, 0);
            Physics.SyncTransforms();
            var can = Track(new GameObject("LungeCan"));
            can.transform.position = new Vector3(6, 0, 6);
            GameServices.Round.Lata = can.AddComponent<Lata>();
            _taya = Seat(0, new Vector3(0, 0, -4));
            GameServices.Match.StartMatch();
            GameServices.Round.BeginRound();
            yield return new WaitForSeconds(0.2f);
        }

        IEnumerator FullCharge()
        {
            _taya.Intent.Set(Verb.Lunge, true);
            yield return new WaitForSeconds(Balance.LungeChargeTime + 0.05f);
            _taya.Intent.Set(Verb.Lunge, false);
            yield return null;
        }

        IEnumerator LocalTravel(GameMode mode)
        {
            yield return Open(mode);
            Vector3 start = _taya.transform.position;
            yield return FullCharge();
            yield return new WaitForSeconds(Balance.LungeActiveTime + 0.12f);
            float travel = _taya.transform.position.z - start.z;
            Assert.That(travel, Is.InRange(2.7f, 3.1f), "Actual controller travel with fixed-step friction.");
            Assert.Greater(_taya.GetComponent<CombatVerbs>().LungeCooldownLeft, 0);
        }

        [UnityTest] public IEnumerator ClassicPressTravelsThreeMetres() => LocalTravel(GameMode.Classic);
        [UnityTest] public IEnumerator HeroStrikePressTravelsThreeMetres() => LocalTravel(GameMode.HeroStrike);

        [UnityTest] public IEnumerator HostRequestTravelsTheSameDistanceAndRejectsRepeat()
        {
            yield return Open(GameMode.Classic);
            var verbs = _taya.GetComponent<CombatVerbs>();
            Vector3 start = _taya.transform.position;
            Assert.IsTrue(verbs.HostResolveLunge(start, Vector3.forward, 1));
            Assert.IsFalse(verbs.HostResolveLunge(start, Vector3.forward, 1));
            yield return new WaitForSeconds(Balance.LungeActiveTime + 0.12f);
            Assert.That(_taya.transform.position.z - start.z, Is.InRange(2.7f, 3.1f));
        }

        [UnityTest] public IEnumerator SweepTagsBeyondTheOldReachButNotBeyondActualTravel()
        {
            yield return Open(GameMode.Classic);
            var victim = Seat(1, _taya.transform.position + Vector3.forward * 3.7f);
            victim.HoldingSlipper = true;
            Assert.IsTrue(victim.IsTaggable());
            Assert.IsTrue(GameServices.Match.MatchInProgress);
            int hits = 0;
            System.Action<int, ScoreEvent> scored = (slot, e) => { if (slot == 0 && e == ScoreEvent.Tag) hits++; };
            GameServices.Match.Scored += scored;
            try
            {
                yield return FullCharge();
                yield return new WaitForSeconds(Balance.LungeActiveTime + 0.12f);
                Assert.AreEqual(1, hits, "The real travelled sweep reaches an attacker the 1m dash could not.");
                Assert.IsNotNull(victim);
            }
            finally { GameServices.Match.Scored -= scored; }
        }

        [UnityTest] public IEnumerator DistantAttackerIsNotTagged()
        {
            yield return Open(GameMode.Classic);
            var victim = Seat(1, _taya.transform.position + Vector3.forward * 5.8f);
            victim.HoldingSlipper = true;
            Assert.IsTrue(victim.IsTaggable());
            int hits = 0;
            System.Action<int, ScoreEvent> scored = (slot, e) => { if (e == ScoreEvent.Tag) hits++; };
            GameServices.Match.Scored += scored;
            try
            {
                yield return FullCharge();
                yield return new WaitForSeconds(Balance.LungeActiveTime + 0.12f);
                Assert.AreEqual(0, hits);
            }
            finally { GameServices.Match.Scored -= scored; }
        }
    }
}
