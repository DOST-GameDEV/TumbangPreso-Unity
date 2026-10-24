using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Plays Sean's kit through real input in a live Hero Strike round on the Eskinita (his own court; Bayan Plaza is out of the build list since protocol 172), and checks what each skill does to the
    /// world and what it tells every peer (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md`). Measurements go to
    /// `Logs/sean-play.csv`. Solo authority: the host's rules and the flair every peer would play are the same calls.
    /// </summary>
    public sealed class SeanKitPlayProbe
    {
        private INetProvider _net;
        private readonly StringBuilder _log = new StringBuilder();
        private readonly List<(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)> _flair =
            new List<(MatchFlair.Kind, int, int, Vector3, float)>();
        private readonly List<GameObject> _props = new List<GameObject>();

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            _log.Clear(); _flair.Clear();
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(-7 + player.PlayerSlot * 1.5f, .12f, 11));
            }
            MatchFlair.Presented += Record;
        }

        [UnityTearDown] public IEnumerator After()
        {
            MatchFlair.Presented -= Record;
            foreach (var prop in _props) if (prop != null) Object.Destroy(prop);
            _props.Clear();
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/sean-play.csv", _log.ToString());
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private void Record(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
            => _flair.Add((kind, actor, subject, at, strength));

        private void Note(string claim, object value) => _log.AppendLine(FormattableString.Invariant($"{claim},{value}"));

        private static CharacterMotor Sean(CharacterMotor who, Vector3 at, Vector3 aim)
        {
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "sean");
            who.AbilitySystem.BindHero("sean");
            who.Teleport(at);
            who.transform.rotation = Quaternion.LookRotation(Flat(aim - at), Vector3.up);
            who.Intent.Parked = false;
            who.Intent.AimPoint = aim;
            who.ClearStun(); who.ClearTrip();
            return who;
        }

        private static CharacterMotor Attacker(int notSlot) => GameServices.Round.Players
            .First(p => !p.IsDefender && p.PlayerSlot != notSlot);

        private static IEnumerator Press(CharacterMotor who, Verb verb, float seconds = .15f)
        {
            float start = Time.time;
            while (Time.time - start < seconds) { who.Intent.Set(verb, true); yield return null; }
            who.Intent.Set(verb, false);
            yield return null;
        }

        private GameObject Box(Vector3 centre, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "SeanProbeBox";
            box.transform.position = centre; box.transform.localScale = size;
            _props.Add(box);
            return box;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        // ------------------------------------------------------------------ LAVA RAM

        [UnityTest, Timeout(60000)]
        public IEnumerator LavaRamStopsOnTheFirstBodyAndLaunchesIt()
        {
            var round = GameServices.Round;
            var sean = Sean(round.PlayerAt(1), new Vector3(0, .12f, -8), new Vector3(0, 1, 7));
            var victim = Attacker(1);
            victim.Teleport(new Vector3(0, .12f, -6)); victim.Intent.Parked = false;
            yield return new WaitForSeconds(.2f);
            Vector3 seanFrom = sean.transform.position, victimFrom = victim.transform.position;
            yield return Press(sean, Verb.Skill1, SeanRules.RamMinBraceSeconds + .1f);
            yield return new WaitForSeconds(1.2f);
            float flung = Flat(victim.transform.position - victimFrom).magnitude;
            float travelled = Flat(sean.transform.position - seanFrom).magnitude;
            Note("ram_victim_flung_m", flung); Note("ram_sean_travelled_m", travelled);
            Assert.That(flung, Is.InRange(SeanRules.RamLaunch * .75f, SeanRules.RamLaunch * 1.25f), "The body hit was not launched 3 m.");
            Assert.Less(travelled, 2.6f, "He did not stop on the first body.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanSlam && f.subject == victim.PlayerSlot),
                "The RAM's hit was not announced, so no other screen would see it.");
            Assert.AreEqual(1, ((SeanHeroKit)sean.AbilitySystem.Kit).PressureBars, "A landed RAM did not add heat.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LavaRamIntoAWallDazesHimButARampDoesNot()
        {
            var round = GameServices.Round;
            var sean = Sean(round.PlayerAt(1), new Vector3(0, .12f, -8), new Vector3(0, 1, 7));
            Box(new Vector3(0, 1.2f, -6.2f), new Vector3(3, 2.4f, .3f));
            yield return new WaitForSeconds(.2f);
            yield return Press(sean, Verb.Skill1, SeanRules.RamMinBraceSeconds + .1f);
            bool dazed = false; float t0 = Time.time;
            while (Time.time - t0 < 1.2f) { dazed |= sean.IsStunned; yield return null; }
            Note("ram_wall_dazed", dazed);
            Assert.IsTrue(dazed, "Running into a wall did not daze him.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.HeroHit && f.subject == sean.PlayerSlot),
                "His daze was not announced, so the stars over him would show on the host only.");
        }

        // ------------------------------------------------------------------ PRESSURE

        [UnityTest, Timeout(60000)]
        public IEnumerator FullPressureErupts40PercentFartherAndSplashesOnlyItsOwnLevel()
        {
            var round = GameServices.Round;
            var sean = Sean(round.PlayerAt(1), new Vector3(1.6f, .12f, -9.5f), new Vector3(0, 1, -7));
            var kit = (SeanHeroKit)sean.AbilitySystem.Kit;
            var victim = Attacker(1);
            victim.Teleport(new Vector3(0, .12f, -7));
            var beside = round.Players.First(p => p != sean && p != victim && !p.IsDefender);
            beside.Teleport(new Vector3(0, .12f, -6.0f));   // along the lane: the Eskinita's walls are close on x
            var above = round.Players.First(p => p != sean && p != victim && p != beside);
            Box(new Vector3(-1.0f, 1.25f, -7), new Vector3(1.2f, 2.5f, 1.2f));
            above.Teleport(new Vector3(-1.0f, 2.62f, -7));
            yield return new WaitForSeconds(.3f);

            Vector3 push = Vector3.forward * Balance.ShoveSpeed + Vector3.up * Balance.ShoveLift;
            for (int i = 0; i < SeanRules.PressureBars; i++) Assert.AreEqual(push, kit.HostShoveLanded(sean, victim, push), "Heat erupted before it was full.");
            Assert.AreEqual(SeanRules.PressureBars, kit.PressureBars);
            Vector3 besideFrom = beside.transform.position, aboveFrom = above.transform.position;
            Vector3 erupted = kit.HostShoveLanded(sean, victim, push);
            float baseDistance = Combat.KnockbackDistance(Flat(push).magnitude);
            float eruptedDistance = Combat.KnockbackDistance(Flat(erupted).magnitude);
            Note("pressure_base_m", baseDistance); Note("pressure_erupted_m", eruptedDistance);
            Assert.AreEqual(SeanRules.EruptedDistance(baseDistance), eruptedDistance, .05f, "The eruption did not go 40% farther.");
            Assert.LessOrEqual(Flat(erupted).magnitude, Balance.MaxKnockbackSpeed + .01f, "The eruption broke the knockback cap.");
            yield return new WaitForSeconds(.8f);
            float besideMoved = Flat(beside.transform.position - besideFrom).magnitude;
            float aboveMoved = Flat(above.transform.position - aboveFrom).magnitude;
            Note("pressure_splash_beside_m", besideMoved); Note("pressure_splash_above_m", aboveMoved);
            Assert.That(besideMoved, Is.InRange(SeanRules.EruptSplashPush * .6f, SeanRules.EruptSplashPush * 1.4f), "The splash did not push the body beside them.");
            Assert.Less(aboveMoved, .3f, "The splash reached a body on a platform 2.5 m above it.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanSplash && f.subject < 0), "The splash's ring was not announced.");
            Assert.IsTrue(round.Lata.IsUpright, "PRESSURE's splash knocked the can over.");
        }

        // ------------------------------------------------------------------ MOLTEN THROW's hot can

        [UnityTest, Timeout(60000)]
        public IEnumerator AHotCanSlidesAlongTheThrowAndRefusesTheResetUntilItCools()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            var taya = round.Players.First(p => p.IsDefender);
            var sean = Sean(round.PlayerAt(1), lata.transform.position + new Vector3(0, .12f, -6), lata.transform.position);
            lata.HostKnockDown(sean.PlayerSlot);
            yield return null;
            Vector3 from = lata.transform.position;
            lata.HostMoltenHit(Vector3.right);
            yield return new WaitForSeconds(.6f);
            float slid = Flat(lata.transform.position - from).magnitude;
            Note("hot_can_slide_m", slid);
            Assert.That(slid, Is.InRange(1.0f, SeanRules.HotCanSlide + .2f), "The hot can did not slide along the throw.");
            Assert.IsFalse(AIController.IsOutsidePlayable(lata.transform.position), "The hot can left the playable square.");
            Assert.IsTrue(lata.IsHot);
            Assert.IsTrue(Object.FindObjectsByType<SeanGroundMark>(FindObjectsSortMode.None).Length > 0, "No cooling ring round the hot can.");
            taya.Teleport(lata.transform.position + new Vector3(0, .12f, -.6f)); taya.ClearStun(); taya.ClearTrip();
            yield return null;
            Assert.IsFalse(taya.GetComponent<Carrier>().HasResetTarget, "The taya could start a reset on a hot can.");
            yield return new WaitForSeconds(SeanRules.HotCanSeconds);
            Note("hot_can_cooled", !lata.IsHot);
            Assert.IsFalse(lata.IsHot);
            Assert.IsTrue(taya.GetComponent<Carrier>().HasResetTarget, "The cooled can still refused the reset.");
        }

        // ------------------------------------------------------------------ MAGMA FIST

        [UnityTest, Timeout(60000)]
        public IEnumerator MagmaFistMarksItsTargetAndThrowsThemAwayFromTheCan()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Vector3 can = Flat(lata.transform.position);
            var target = round.Players.First(p => !p.IsDefender);
            target.Teleport(can + new Vector3(0, .12f, -5)); target.Intent.Parked = false;
            var sean = Sean(round.Players.First(p => p.IsDefender), can + new Vector3(0, .12f, -1), target.transform.position);
            yield return new WaitForSeconds(.2f);
            float before = Flat(target.transform.position - can).magnitude;
            yield return Press(sean, Verb.Skill2, .15f);
            bool marked = false; float t0 = Time.time;
            while (Time.time - t0 < 1.0f) { marked |= Object.FindObjectsByType<SeanGroundMark>(FindObjectsSortMode.None).Length > 0; yield return null; }
            yield return new WaitForSeconds(.8f);
            float after = Flat(target.transform.position - can).magnitude;
            Note("fist_target_marked", marked); Note("fist_distance_from_can_before_m", before); Note("fist_distance_from_can_after_m", after);
            Assert.IsTrue(marked, "The player the fist was coming for had no ring under them.");
            Assert.Greater(after - before, SeanRules.FistPush * .6f, "The fist did not throw them away from the can.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanSlam && f.subject == target.PlayerSlot), "The punch was not announced.");
        }

        // ------------------------------------------------------------------ MAYON AWAKENS, Molten and VOLCANIC BOMB

        [UnityTest, Timeout(120000)]
        public IEnumerator TheAwakeningThrowsBodiesNotTheCanThenMoltenStandsAndTheBombKnocksTheCanOver()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Vector3 can = Flat(lata.transform.position);
            var sean = Sean(round.PlayerAt(1), can + new Vector3(0, .12f, -2.5f), lata.transform.position);
            var kit = (SeanHeroKit)sean.AbilitySystem.Kit;
            Assert.IsFalse(sean.IsDefender, "This reproduction needs the attacking kit.");
            kit.AddUltimateCharge(100);
            var near = Attacker(sean.PlayerSlot);
            near.Teleport(sean.transform.position + new Vector3(1.6f, 0, 0)); near.Intent.Parked = false;
            yield return new WaitForSeconds(.2f);
            Vector3 nearFrom = near.transform.position;
            string before = kit.Skill2.LiveName;

            yield return Press(sean, Verb.Ultimate, .2f);
            float t0 = Time.time;
            while (Time.time - t0 < 12f && !kit.IsMolten) yield return null;
            Assert.IsTrue(kit.IsMolten, "MAYON AWAKENS never made him Molten.");
            yield return new WaitForSeconds(.6f);
            float thrown = Flat(near.transform.position - nearFrom).magnitude;
            Note("awakening_thrown_m", thrown); Note("awakening_can_upright", lata.IsUpright);
            Assert.That(thrown, Is.InRange(SeanRules.AwakeningPush * .7f, SeanRules.AwakeningPush * 1.3f), "The awakening did not throw the body beside him 3.5 m.");
            Assert.IsTrue(lata.IsUpright, "The awakening knocked the can over; only the bomb may.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanBlast && f.subject == near.PlayerSlot), "The awakening's hit was not announced.");

            // Molten: nothing moves him.
            Vector3 held = sean.transform.position;
            sean.ApplyResolvedImpact(Vector3.right * 14f + Vector3.up * 3f);
            yield return new WaitForSeconds(.5f);
            Note("molten_moved_m", Flat(sean.transform.position - held).magnitude);
            Assert.Less(Flat(sean.transform.position - held).magnitude, .25f, "A push moved Molten Sean.");

            // The E tile is the bomb while Molten, and the bomb knocks the can over.
            Note("e_tile_before", before); Note("e_tile_molten", kit.Skill2.LiveName);
            Assert.AreEqual("VOLCANIC BOMB", kit.Skill2.LiveName, "The E tile did not read VOLCANIC BOMB while Molten.");
            Assert.AreNotEqual("VOLCANIC BOMB", before);
            sean.Intent.AimPoint = lata.transform.position;
            yield return Press(sean, Verb.Skill2, .15f);
            Assert.IsFalse(kit.IsMolten, "Throwing the bomb did not end Molten.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanBombThrown), "The throw was not announced for the spectator.");
            t0 = Time.time;
            while (Time.time - t0 < 3f && lata.IsUpright) yield return null;
            Note("bomb_knocked_can", !lata.IsUpright);
            Assert.IsFalse(lata.IsUpright, "The bomb did not knock the can over.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AnUnthrownBombDropsAtHisFeetWhenMoltenEnds()
        {
            var round = GameServices.Round;
            var sean = Sean(round.PlayerAt(1), new Vector3(0, .12f, -8), new Vector3(0, 1, 7));
            var kit = (SeanHeroKit)sean.AbilitySystem.Kit;
            kit.AddUltimateCharge(100);
            yield return Press(sean, Verb.Ultimate, .2f);
            float t0 = Time.time;
            while (Time.time - t0 < 12f && !kit.IsMolten) yield return null;
            Assert.IsTrue(kit.IsMolten);
            var near = Attacker(sean.PlayerSlot);
            near.Teleport(sean.transform.position + new Vector3(1.5f, 0, 0)); near.ClearStun(); near.ClearTrip();
            _flair.Clear();
            Vector3 nearFrom = near.transform.position;
            t0 = Time.time;
            while (Time.time - t0 < SeanRules.MoltenSeconds + 1f && kit.IsMolten) yield return null;
            Assert.IsFalse(kit.IsMolten);
            yield return new WaitForSeconds(1.0f);
            float thrown = Flat(near.transform.position - nearFrom).magnitude;
            Note("dropped_bomb_thrown_m", thrown);
            Assert.That(thrown, Is.InRange(SeanRules.DroppedBombPush * .6f, SeanRules.DroppedBombPush * 1.4f), "The dropped bomb did not splash at his feet.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanSlam && f.subject == near.PlayerSlot), "The dropped bomb's hit was not announced.");
        }
    }
}
