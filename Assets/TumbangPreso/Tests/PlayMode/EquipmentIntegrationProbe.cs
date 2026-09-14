using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class EquipmentIntegrationProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private CustomRules _rules;
        private INetProvider _net;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone(); _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest]
        public IEnumerator EveryHeldShoeUsesItsHandlingAndReleasesItsVisibleAimInBothModes()
        {
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, mode);
                NetAuthority.Provider = new SoloProvider(); GameServices.Round.BeginRound();
                var owner = GameServices.Round.PlayerAt(1);
                var carrier = owner.GetComponent<Carrier>();
                var shoe = carrier.Held;
                owner.Teleport(new Vector3(0, .12f, -10));
                owner.Intent.AimPoint = new Vector3(0, .18f, 0);
                owner.Intent.Parked = false;
                carrier.enabled = false;
                for (int index = 0; index < Roster.Slippers.Count; index++)
                {
                    shoe.SkinIndex = index;
                    Assert.True(shoe.HostForceEquip(owner));
                    typeof(Carrier).GetField("_throwLockLeft", Private).SetValue(carrier, 0f);
                    owner.Intent.Set(Verb.SpecialAbility, true);
                    typeof(Carrier).GetMethod("StepAttacker", Private).Invoke(carrier, new object[] { .016f });
                    Assert.True(carrier.IsCharging);
                    typeof(Carrier).GetField("_charge", Private).SetValue(carrier, .5f);
                    typeof(Carrier).GetField("_aimHeldSeconds", Private).SetValue(carrier, .5f);
                    typeof(Carrier).GetField("_aimMovement", Private).SetValue(carrier, .4f);
                    int sequence = (int)typeof(Carrier).GetField("_aimSequence", Private).GetValue(carrier);
                    var expectedOffset = ThrowAimRules.Sample(.5f, .4f, Time.time,
                        owner.PlayerSlot * .73f + (sequence % 4096) * .618034f, index);
                    Assert.Less(Vector2.Distance(carrier.AimAngularOffset,
                        new Vector2(expectedOffset.Yaw, expectedOffset.Pitch)), .00001f, Roster.Slippers[index].Id);
                    var expectedVelocity = carrier.LaunchVelocityNow();
                    owner.Intent.Set(Verb.SpecialAbility, false);
                    typeof(Carrier).GetMethod("StepAttacker", Private).Invoke(carrier, new object[] { .016f });
                    Assert.IsNull(carrier.Held);
                    Assert.Less(Vector3.Distance(expectedVelocity, shoe.Velocity), .001f, Roster.Slippers[index].Id);
                }
                yield return PlayModeWorld.Reset();
            }
        }

        [UnityTest]
        public IEnumerator ActualCanHitUsesSharedEquipmentInBothModesAndDuringRestoreProtection()
        {
            const string output = "Logs/equipment-integration-v1";
            Directory.CreateDirectory(output);
            var rows = new List<string> { "mode,can,protected,recoil_speed,lift" };
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, mode);
                NetAuthority.Provider = new SoloProvider(); GameServices.Round.BeginRound();
                var round = GameServices.Round;
                var can = round.Lata;
                var mark = can.transform.position;
                var owner = round.PlayerAt(1);
                var shoe = owner.GetComponent<Carrier>().Held;
                Assert.IsNotNull(shoe);
                foreach (var player in round.Players)
                    if (player != null) player.Teleport(new Vector3(4, .12f, 4 + player.PlayerSlot));
                shoe.enabled = false;
                for (int index = 0; index < Roster.Cans.Count; index++)
                {
                    foreach (bool protectedHit in new[] { false, true })
                    {
                        can.ApplySnapshotState(mark, Quaternion.identity, false, index);
                        can.ApplySnapshotState(mark, Quaternion.identity, true, index);
                        if (!protectedHit) yield return new WaitForSeconds(Balance.ThrowRestoreCooldown + .05f);
                        Assert.AreEqual(protectedHit, can.IsProtected);
                        // Call the production physics step with known incoming velocity.
                        // The hit must be found by its normal can contact path, not Deflect.
                        shoe.HostThrow(owner, mark + new Vector3(0, .35f, -.6f), Vector3.forward * 20);
                        typeof(Slipper).GetMethod("FixedUpdate", Private).Invoke(shoe, null);
                        float scale = Roster.CanReboundScale(index);
                        Assert.AreEqual(-20 * Balance.LataRecoilScale * scale, shoe.Velocity.z, .002f, Roster.Cans[index].Id);
                        Assert.AreEqual(Balance.DeflectLift * Balance.LataRecoilLiftScale * scale, shoe.Velocity.y, .002f);
                        Assert.AreEqual(protectedHit, can.IsUpright, "Protection must bounce without scoring or toppling.");
                        rows.Add(FormattableString.Invariant($"{mode},{Roster.Cans[index].Id},{protectedHit},{-shoe.Velocity.z:F4},{shoe.Velocity.y:F4}"));
                    }
                }
                yield return PlayModeWorld.Reset();
            }
            File.WriteAllLines(Path.Combine(output, "can-recoil.csv"), rows);
        }
    }
}
