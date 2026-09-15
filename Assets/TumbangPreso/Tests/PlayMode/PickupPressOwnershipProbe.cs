using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PickupPressOwnershipProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest]
        public IEnumerator PickupOwnsItsPressAcrossUpdatesBeforeTheNextPhysicsCommit()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza);
            GameServices.Round.BeginRound();
            var who = GameServices.Round.PlayerAt(1);
            var carrier = who.GetComponent<Carrier>();
            var combat = who.GetComponent<CombatVerbs>();
            carrier.enabled = false; combat.enabled = false;
            who.Teleport(new Vector3(0, .12f, -10)); who.transform.rotation = Quaternion.identity;
            who.Intent.Clear(); who.Intent.CommitFrame(); who.Intent.Parked = false;
            var victim = GameServices.Round.PlayerAt(2);
            victim.Teleport(new Vector3(0, .12f, -9));
            var shoe = carrier.Held;
            Assert.IsNotNull(shoe); Assert.True(shoe.HostDisarm());
            shoe.transform.position = who.transform.position + Vector3.right * .4f;
            float stamina = who.Stamina.Current;
            var carrierUpdate = typeof(Carrier).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var combatUpdate = typeof(CombatVerbs).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            void UpdateConsumers() { carrierUpdate.Invoke(carrier, null); combatUpdate.Invoke(combat, null); }
            who.Intent.Set(Verb.Grab, true);
            // This is the real component order at >50 rendered FPS: several Update passes
            // can read one edge before CharacterMotor commits the next physics snapshot.
            for (int update = 0; update < 3; update++)
            {
                Assert.True(who.Intent.JustPressed(Verb.Grab));
                UpdateConsumers();
                Assert.AreSame(shoe, carrier.Held);
                Debug.Log($"[PickupPress] update={update} shove={combat.ShoveCooldownLeft} stamina={who.Stamina.Current}");
                Assert.AreEqual(0, combat.ShoveCooldownLeft, "The successful pickup press was reused as a shove");
                Assert.AreEqual(stamina, who.Stamina.Current, .001f, "Pickup spent shove stamina");
            }
            who.Intent.CommitFrame();
            UpdateConsumers();
            Assert.AreEqual(0, combat.ShoveCooldownLeft, "Holding after pickup must not shove");
            who.Intent.Set(Verb.Grab, false);
            UpdateConsumers();
            who.Intent.CommitFrame();
            who.Intent.Set(Verb.Grab, true);
            UpdateConsumers();
            Assert.Greater(combat.ShoveCooldownLeft, 0, "A fresh press while carrying must still shove");
            Assert.AreEqual(stamina - Balance.ShoveStaminaCost, who.Stamina.Current, .001f);
            Assert.Greater(combat.LastShoveLandedAt, -1);
        }
    }
}
