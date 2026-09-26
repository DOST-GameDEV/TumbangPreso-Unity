using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ PAETE, PLAYED (HERO-9 step 2): a real Hero Strike match on Bayan Plaza, every ability pressed
    /// through `InputIntent` exactly as a player or a bot presses it, host-resolved as in a match. It
    /// answers the questions the kit had never been asked in play: does the vine reel him, does the
    /// seedling grow, fire on the second press and come out of the ground to an opponent's Interact
    /// hold only after its 15 s, does BAWI take slippers even out of a hand, does the sentry root the
    /// bodies in 9 m, does 7 s of Interact break a player free, and does a tag free a rooted player.
    /// Each case writes what it saw to `Logs/paete-play.csv`, one row per claim.
    /// </summary>
    public sealed class PaeteKitPlayProbe
    {
        private INetProvider _net;
        private readonly StringBuilder _log = new StringBuilder();

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            _log.Clear();
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(10 + player.PlayerSlot * 2, .12f, -10));
            }
        }

        [UnityTearDown] public IEnumerator After()
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/paete-play.csv", _log.ToString());
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private void Note(string claim, object value) => _log.AppendLine(FormattableString.Invariant($"{claim},{value}"));

        private static CharacterMotor Paete(int slot, Vector3 at)
        {
            var who = GameServices.Round.PlayerAt(slot);
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "paete");
            who.AbilitySystem.BindHero("paete");
            who.Teleport(at); who.transform.rotation = Quaternion.identity;
            who.Intent.Parked = false; who.IsBot = true;
            return who;
        }

        private static IEnumerator Press(CharacterMotor who, Verb verb, float seconds = .15f)
        {
            float start = Time.time;
            while (Time.time - start < seconds) { who.Intent.Set(verb, true); yield return null; }
            who.Intent.Set(verb, false);
            yield return null;
        }

        private static IEnumerator Hold(CharacterMotor who, Verb verb, float seconds, Action each = null)
        {
            float start = Time.time;
            while (Time.time - start < seconds) { who.Intent.Set(verb, true); each?.Invoke(); yield return null; }
            who.Intent.Set(verb, false);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TheVinesReelHimForward()
        {
            var paete = Paete(1, new Vector3(0, .12f, -9));
            Vector3 start = paete.transform.position;
            paete.Intent.AimPoint = start + new Vector3(0, 1.2f, 8f);
            bool drew = false;
            yield return Press(paete, Verb.Skill1);
            float t0 = Time.time;
            while (Time.time - t0 < 1.4f) { drew |= Object.FindFirstObjectByType<Visual.PaeteVineReach>() != null; yield return null; }
            float travelled = Vector3.Distance(Flat(start), Flat(paete.transform.position));
            Note("vine_drew", drew); Note("vine_travel_m", travelled);
            Assert.IsTrue(drew, "No vine was drawn.");
            Assert.Greater(travelled, 3.0f, "Kapit-Baging did not carry him toward the anchor.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheSeedlingGrowsFiresAndComesOutOnlyWhenLoose()
        {
            var paete = Paete(1, new Vector3(0, .12f, -9));
            paete.Intent.AimPoint = paete.transform.position + new Vector3(0, 0, 4f);
            yield return Press(paete, Verb.Skill2);
            yield return new WaitForSeconds(.8f);
            var plant = PaetePlant.OwnedBy(paete.PlayerSlot);
            Note("plant_spawned", plant != null);
            Assert.IsNotNull(plant, "Punlang Tsinelas planted nothing.");

            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds);
            Assert.IsTrue(plant.ShotReady, "The seedling never grew its first wooden slipper.");
            var lata = GameServices.Round.Lata;
            paete.Intent.AimPoint = lata.transform.position;
            yield return Press(paete, Verb.Skill2);
            yield return null;
            bool fired = Object.FindFirstObjectByType<PaeteWoodenSlipper>() != null;
            Note("plant_fired", fired);
            Assert.IsTrue(fired, "The second press did not throw a wooden slipper.");

            // An opponent at the plant before its 15 s: the hold does nothing.
            var puller = GameServices.Round.PlayerAt(2);
            puller.Teleport(plant.transform.position + new Vector3(0, .12f, -1.0f)); puller.Intent.Parked = false;
            yield return Hold(puller, Verb.Interact, 1.5f);
            Note("plant_survives_early_pull", PaetePlant.OwnedBy(paete.PlayerSlot) != null);
            Assert.IsNotNull(PaetePlant.OwnedBy(paete.PlayerSlot), "The plant came out inside its rooted 15 s.");

            while (!plant.Pullable) yield return null;
            float peak = 0;
            yield return Hold(puller, Verb.Interact, PaeteRules.PlantPullSeconds + .4f, () => peak = Mathf.Max(peak, puller.PullingPlantProgress));
            yield return new WaitForSeconds(1.1f);
            Note("pull_progress_peak", peak); Note("plant_pulled", PaetePlant.OwnedBy(paete.PlayerSlot) == null);
            Assert.Greater(peak, .8f, "The pull never showed progress.");
            Assert.IsNull(PaetePlant.OwnedBy(paete.PlayerSlot), "Holding Interact at a loose seedling did not pull it out.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ThornsTakeASlipperOutOfAHand()
        {
            var round = GameServices.Round;
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            var paete = Paete(taya, new Vector3(0, .12f, -4));
            yield return null;
            Assert.IsTrue(paete.AbilitySystem.Kit.IsDefending, "The taya's kit is not in its defending role.");
            CharacterMotor holder = null;
            foreach (var p in round.Players) if (p != paete && p.GetComponent<Carrier>().Held != null) { holder = p; break; }
            Assert.IsNotNull(holder);
            var shoe = holder.GetComponent<Carrier>().Held;
            holder.Teleport(new Vector3(4.5f, .12f, -4));
            yield return new WaitForSeconds(.2f);
            yield return Press(paete, Verb.Skill2);
            yield return new WaitForSeconds(1.4f);
            float home = Vector3.Distance(Flat(shoe.transform.position), Flat(paete.transform.position));
            Note("thorn_took_from_hand", holder.GetComponent<Carrier>().Held != shoe); Note("thorn_slipper_distance_m", home);
            Assert.AreNotSame(shoe, holder.GetComponent<Carrier>().Held, "BAWI left the slipper in the holder's hand.");
            Assert.Less(home, 2.2f, "The slipper was not hauled home.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheSentryRootsAndATagOrSevenSecondsFreesThem()
        {
            var round = GameServices.Round;
            var paete = Paete(1, new Vector3(0, .12f, -9));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var a = round.PlayerAt(2); var b = round.PlayerAt(3);
            a.Teleport(new Vector3(2.5f, .12f, -4)); b.Teleport(new Vector3(-2.5f, .12f, -4));
            a.Intent.Parked = b.Intent.Parked = false;
            paete.Intent.AimPoint = new Vector3(0, 0, -4);
            yield return Press(paete, Verb.Ultimate, .2f);
            float t0 = Time.time;
            while (Time.time - t0 < 8f && !(a.IsRooted && b.IsRooted)) yield return null;
            Note("sentry_rooted_a", a.IsRooted); Note("sentry_rooted_b", b.IsRooted);
            Assert.IsTrue(a.IsRooted && b.IsRooted, "The sentry did not root both bodies inside 9 m.");
            Assert.Less(Vector3.Distance(Flat(a.transform.position), new Vector3(0, 0, -4)), 2.0f, "A caught body was not dragged in.");

            // Rooted: no movement.
            Vector3 held = a.transform.position;
            a.Intent.Move = new Vector2(1, 0);
            yield return new WaitForSeconds(.5f);
            a.Intent.Move = Vector2.zero;
            Note("rooted_moved_m", Vector3.Distance(held, a.transform.position));
            Assert.Less(Vector3.Distance(held, a.transform.position), .15f, "A rooted body walked.");

            // A tag frees b at once.
            b.ApplyTagged();
            yield return null;
            Note("tag_frees", !b.IsRooted);
            Assert.IsFalse(b.IsRooted, "A tag did not end Rooted.");

            // Seven seconds of Interact frees a; the struggle shows while they hold.
            bool struggled = false;
            yield return Hold(a, Verb.Interact, PaeteRules.BreakFreeHoldSeconds + .3f, () => struggled |= a.IsStruggling);
            Note("struggle_seen", struggled); Note("break_free", !a.IsRooted);
            Assert.IsTrue(struggled, "Holding Interact while rooted did not struggle.");
            Assert.IsFalse(a.IsRooted, "Seven seconds of Interact did not break free.");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    }
}
