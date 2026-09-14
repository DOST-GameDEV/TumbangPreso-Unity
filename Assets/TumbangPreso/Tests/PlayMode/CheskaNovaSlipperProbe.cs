using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class CheskaNovaSlipperProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private INetProvider _net;
        private CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator NovaLaunchesNearbyLooseSlippersAndKeepsHeldAndOutsideOnesSafe()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            var round = GameServices.Round;
            foreach (var player in round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(6, .12f, 7 + player.PlayerSlot));
            }
            var caster = round.PlayerAt(1);
            caster.Teleport(new Vector3(0, .12f, -8)); caster.transform.rotation = Quaternion.identity;
            caster.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "cheska");
            caster.AbilitySystem.BindHero("cheska"); caster.AbilitySystem.Kit.AddUltimateCharge(100);
            caster.Intent.Parked = false;
            var held = caster.GetComponent<Carrier>().Held;
            var loose = round.PlayerAt(2).GetComponent<Carrier>().Held;
            var outside = round.PlayerAt(3).GetComponent<Carrier>().Held;
            Assert.IsNotNull(held); Assert.IsNotNull(loose); Assert.IsNotNull(outside);
            loose.HostDisarm(); outside.HostDisarm();
            var looseStart = new Vector3(-1.2f, 0, -8);
            looseStart.y = Slipper.GroundY(looseStart) + loose.RestHeight;
            var outsideStart = new Vector3(-6, 0, -8);
            outsideStart.y = Slipper.GroundY(outsideStart) + outside.RestHeight;
            loose.transform.position = looseStart; outside.transform.position = outsideStart;
            yield return new WaitForSeconds(.15f);
            bool launched = false, accepted = false;
            float start = Time.time, greatestOutward = 0;
            while (Time.time - start < 1.1f)
            {
                caster.Intent.Set(Verb.Ultimate, Time.time - start < .2f);
                accepted |= caster.AbilitySystem.LastAnswer(Abilities.HeroAbilitySystem.Slot.Ultimate) == Abilities.HeroKit.CastOutcome.Cast;
                launched |= loose.State == SlipperState.InFlight;
                greatestOutward = Mathf.Max(greatestOutward, looseStart.x - loose.transform.position.x);
                yield return null;
            }
            caster.Intent.Clear();
            File.WriteAllText("Logs/cheska-nova-slippers.csv", FormattableString.Invariant(
                $"accepted,launched,outward,held_same,outside_move\n{accepted},{launched},{greatestOutward},{caster.GetComponent<Carrier>().Held == held},{Vector3.Distance(outsideStart, outside.transform.position)}\n"));
            Assert.True(accepted, "The real Nova input was not accepted.");
            Assert.True(launched, "Nova changed a loose slipper's velocity without starting its flight.");
            Assert.Greater(greatestOutward, 2, "The nearby slipper did not actually fly outward.");
            Assert.AreSame(held, caster.GetComponent<Carrier>().Held);
            Assert.AreEqual(SlipperState.Held, held.State);
            Assert.Less(Vector3.Distance(outsideStart, outside.transform.position), .06f);
        }
    }
}
