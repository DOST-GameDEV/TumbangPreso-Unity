using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER'S VOODOO DOLL AS A FIFTH BODY (HERO-10 v3, plan 9.12), in a real Hero Strike round on Bayan Plaza, host-resolved
    /// as the solo host resolves it. The owner's rules, each checked: it is a body in her companion seat on her side, never a player;
    /// its own slipper is in its hand when it attacks and parked when it defends (*"Own slipper, throws"*); its points are hers (*"The
    /// doll gives points gained to Phaister"*); tagging it stuns it where it stands and pays nobody (*"The doll does not give points
    /// when tagged/sabotaged"*); it is a Hard AI that moves at its own slow share of a player's speed; and it leaves with the round.
    /// </summary>
    public sealed class VoodooDollBodyTests
    {
        private INetProvider _net;
        private Vector3 _can;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            _can = GameServices.Round.Lata != null ? GameServices.Round.Lata.transform.position : Vector3.zero;
            _can.y = 0.0f;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(_can + new Vector3(-9.0f + player.PlayerSlot * 0.9f, .12f, -11.0f));
            }
            yield return null;
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private static CharacterMotor Attacker() => GameServices.Round.Players.First(p => p != null && !p.IsDefender);
        private static CharacterMotor Taya() => GameServices.Round.Players.First(p => p != null && p.IsDefender);

        [UnityTest, Timeout(60000)]
        public IEnumerator AttackingItIsAFifthBodyOnHerSideWithItsOwnSlipperAndItsPointsAreHers()
        {
            var round = GameServices.Round;
            var her = Attacker();
            her.AbilitySystem.BindHero("phaister");
            her.Teleport(_can + new Vector3(-3f, .12f, -8f));
            int slippersBefore = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).Length;

            var doll = VoodooDollBody.HostSpawn(her);
            yield return null;

            Assert.IsNotNull(doll, "The host did not spawn the doll.");
            int seat = CompanionSeats.For(her.PlayerSlot);
            Assert.AreEqual(seat, doll.PlayerSlot);
            Assert.AreSame(doll, round.BodyAt(seat));
            Assert.IsFalse(round.Players.Contains(doll), "The doll became a player.");
            Assert.IsTrue(round.Bodies.Contains(doll));
            Assert.IsFalse(doll.IsDefender, "It is not on her side.");
            Assert.AreEqual(VoodooRules.DollSpeedScale, doll.BodySpeedScale, 1e-4f);
            Assert.AreEqual(Difficulty.Astig, doll.GetComponent<AIController>().SeatDifficulty, "It is not a Hard AI.");
            Assert.IsNull(doll.AbilitySystem, "The doll has no skills.");
            Assert.AreSame(doll, VoodooDollBody.HostSpawn(her), "A second cast made a second doll.");

            var shoe = doll.GetComponent<VoodooDollBody>().Shoe;
            Assert.IsNotNull(shoe, "It has no slipper of its own.");
            Assert.AreEqual(seat, shoe.SeatOfOrigin);
            Assert.AreEqual(seat, shoe.OwnerSlot);
            Assert.IsTrue(doll.HoldingSlipper, "Attacking, its slipper is not in its hand.");
            Assert.AreEqual(slippersBefore + 1, Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).Length);
            Assert.AreNotSame(her.GetComponent<Carrier>().Held, shoe, "It took her slipper.");

            // Its points are hers.
            var match = GameServices.Match;
            int hers = match.ScoreFor(her.PlayerSlot);
            match.AddScore(seat, ScoreEvent.LataKnocked);
            Assert.AreEqual(hers + MatchRules.PointsFor(ScoreEvent.LataKnocked), match.ScoreFor(her.PlayerSlot),
                "A knockdown by the doll did not score for her.");

            // Tagging it stuns it where it stands and pays nobody.
            var taya = Taya();
            int tayas = match.ScoreFor(taya.PlayerSlot);
            hers = match.ScoreFor(her.PlayerSlot);
            doll.Teleport(_can + new Vector3(1.5f, .12f, 1.5f));
            yield return null;
            Vector3 where = doll.transform.position;
            int companionTags = 0, playerTags = 0;
            System.Action<int, int> onCompanion = (_, __) => companionTags++;
            System.Action<int, int> onPlayer = (_, __) => playerTags++;
            round.CompanionTagged += onCompanion;
            round.Tagged += onPlayer;
            try
            {
                Assert.IsTrue(doll.IsTaggable(), "The doll inside the box is not taggable.");
                round.ResolveTag(taya, doll);
            }
            finally
            {
                round.CompanionTagged -= onCompanion;
                round.Tagged -= onPlayer;
            }
            Assert.AreEqual(1, companionTags);
            Assert.AreEqual(0, playerTags, "Tagging the doll was reported as a player's tag.");
            Assert.IsTrue(doll.IsStunned, "The tag did not stun the doll.");
            Assert.AreEqual(tayas, match.ScoreFor(taya.PlayerSlot), "Tagging the doll paid the taya.");
            Assert.AreEqual(hers, match.ScoreFor(her.PlayerSlot), "Tagging the doll cost her.");
            yield return null;
            Assert.Less(Vector3.Distance(where, doll.transform.position), 0.5f, "The tag sent the doll home; it stays where it stands.");

            // It leaves with the round, and takes its slipper.
            round.EndRound();
            yield return null;
            Assert.IsTrue(doll == null, "The doll outlived the round.");
            Assert.IsTrue(shoe == null, "Its slipper outlived it.");
            Assert.IsNull(round.BodyAt(seat));
            Assert.AreEqual(0, round.Companions.Count);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator DefendingItGuardsWithHerAndItsSlipperIsParked()
        {
            var round = GameServices.Round;
            var her = Taya();
            her.AbilitySystem.BindHero("phaister");
            her.Teleport(_can + new Vector3(1.5f, .12f, 1.5f));
            var doll = VoodooDollBody.HostSpawn(her);
            yield return null;
            Assert.IsNotNull(doll);
            Assert.IsTrue(doll.IsDefender, "The taya's doll is not defending.");
            Assert.IsFalse(doll.HoldingSlipper, "Defending, it holds a slipper.");
            var shoe = doll.GetComponent<VoodooDollBody>().Shoe;
            Assert.IsFalse(shoe.gameObject.activeSelf, "Defending, its slipper is not parked.");

            // A companion taya's tag scores for her (the doll's points are hers).
            var victim = Attacker();
            victim.Teleport(doll.transform.position + doll.transform.forward * 0.6f);
            yield return null;
            int hers = GameServices.Match.ScoreFor(her.PlayerSlot);
            if (victim.IsTaggable())
            {
                round.ResolveTag(doll, victim);
                Assert.AreEqual(hers + MatchRules.PointsFor(ScoreEvent.Tag), GameServices.Match.ScoreFor(her.PlayerSlot),
                    "The doll's tag did not score for her.");
            }
            else Assert.Inconclusive("The staged attacker was not taggable where it stood.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ItPlaysTheRoundAsAHardAIAtItsOwnSlowPace()
        {
            var her = Attacker();
            her.AbilitySystem.BindHero("phaister");
            her.Teleport(_can + new Vector3(-4f, .12f, -9f));
            var doll = VoodooDollBody.HostSpawn(her);
            Assert.IsNotNull(doll);
            Vector3 from = doll.transform.position;
            float travelled = 0f;
            Vector3 last = from;
            float until = Time.time + 8f;
            while (Time.time < until)
            {
                yield return null;
                if (doll == null) break;
                travelled += Vector3.Distance(new Vector3(last.x, 0, last.z), new Vector3(doll.transform.position.x, 0, doll.transform.position.z));
                last = doll.transform.position;
            }
            Assert.IsTrue(doll != null, "The doll vanished mid-round.");
            Assert.Greater(travelled, 1.5f, "Its brain never moved it.");
            // It can never outrun a player: its whole share of their speed is 0.65 (a sprint included).
            Assert.Less(travelled / 8f, Balance.Speed * Balance.SprintScale * VoodooRules.DollSpeedScale * 1.15f, "It moved faster than its share.");
        }

        private sealed class Client : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 3;
            public int LocalPeerId => 3;
            public bool IsSeatlessReferee => false;
        }

        /// <summary>
        /// ⚠️ A CLIENT BUILDS AND DROPS THE DOLL FROM THE HOST'S LIST (`MatchRpc.OnCompanionSetMsg`, protocol 90): a listed companion
        /// becomes a brainless replica in its seat with its own slipper; a list from an earlier round changes nothing; a list without
        /// it takes it away with its slipper; a list from anybody but the host is ignored.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AClientBuildsAndDropsTheDollFromTheHostsCompanionList()
        {
            var round = GameServices.Round;
            var her = Attacker();
            her.AbilitySystem.BindHero("phaister");
            int seat = CompanionSeats.For(her.PlayerSlot);
            // The match's own router if there is one (a second one refuses to exist), else a bare one for the test.
            GameObject host = null;
            var rpc = MatchRpc.Instance;
            if (rpc == null) { host = new GameObject("CompanionSet receiver"); rpc = host.AddComponent<MatchRpc>(); }
            var matchId = typeof(MatchRpc).GetProperty(nameof(MatchRpc.PresentationMatchId));
            long previousMatch = rpc.PresentationMatchId;
            matchId.GetSetMethod(true).Invoke(rpc, new object[] { 100L });
            NetAuthority.Provider = new Client();
            int roundNumber = GameServices.Match.RoundNumber;
            void Deliver(ulong sender, long match, int number, params int[] seats)
            {
                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(match); writer.WriteValueSafe(number); writer.WriteValueSafe((byte)seats.Length);
                foreach (int s in seats)
                {
                    writer.WriteValueSafe(s); writer.WriteValueSafe((byte)1);
                    writer.WriteValueSafe(her.transform.position + Vector3.right * 1.3f); writer.WriteValueSafe(90f);
                }
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                typeof(MatchRpc).GetMethod("OnCompanionSetMsg", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rpc, new object[] { sender, reader });
            }
            try
            {
                Deliver(7, 100, roundNumber, seat);
                Assert.IsNull(round.BodyAt(seat), "A peer that is not the host made a doll.");
                Deliver(NetworkManager.ServerClientId, 99, roundNumber, seat);
                Assert.IsNull(round.BodyAt(seat), "Another match's list made a doll.");

                Deliver(NetworkManager.ServerClientId, 100, roundNumber, seat);
                yield return null;
                var doll = round.BodyAt(seat);
                Assert.IsNotNull(doll, "The host's list did not build the doll.");
                Assert.IsNull(doll.GetComponent<AIController>(), "A replica has a brain.");
                var shoe = doll.GetComponent<VoodooDollBody>().Shoe;
                Assert.IsNotNull(shoe);
                Assert.AreEqual(seat, shoe.SeatOfOrigin);

                Deliver(NetworkManager.ServerClientId, 100, roundNumber - 1);
                yield return null;
                Assert.IsTrue(doll != null, "A stale round's empty list removed the doll.");

                Deliver(NetworkManager.ServerClientId, 100, roundNumber);
                yield return null;
                Assert.IsTrue(doll == null, "The host's empty list did not remove the doll.");
                Assert.IsTrue(shoe == null, "Its slipper outlived it.");
                Assert.AreEqual(0, round.Companions.Count);
            }
            finally
            {
                NetAuthority.Provider = new SoloProvider();
                if (rpc != null) matchId.GetSetMethod(true).Invoke(rpc, new object[] { previousMatch });
                if (host != null) Object.Destroy(host);
            }
        }
    }
}
