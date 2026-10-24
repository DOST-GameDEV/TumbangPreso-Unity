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
            // The Eskinita seats Dante here, and his Earthbound halves every push he takes: the 0.72 m this measured was the
            // splash's 1.5 m times his 0.5, not a short splash. The splash is judged against what this body takes.
            float besideScale = Mathf.Clamp01(beside.AbilitySystem?.Kit?.IncomingKnockbackDistanceScale ?? 1f);
            Debug.Log("[SeanKitPlayProbe] splash body: " + beside.AbilitySystem?.Kit?.GetType().Name + " x" + besideScale);
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
            float besideWanted = SeanRules.EruptSplashPush * besideScale;
            Note("pressure_splash_beside_wanted_m", besideWanted);
            Assert.That(besideMoved, Is.InRange(besideWanted * .6f, besideWanted * 1.4f), "The splash did not push the body beside them.");
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
        public IEnumerator MagmaFistIsPlacedWhereHeAimsThrowsThemAwayFromTheCanAndKnocksTheirSlipperLoose()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Vector3 can = Flat(lata.transform.position);
            var target = round.Players.First(p => !p.IsDefender);
            target.Teleport(can + new Vector3(0, .12f, -5)); target.Intent.Parked = false;
            var sean = Sean(round.Players.First(p => p.IsDefender), can + new Vector3(0, .12f, -1), target.transform.position);
            // They hold their own slipper: the fist knocks it out of the hand, onto the spot they were thrown from.
            var shoe = target.GetComponent<Carrier>().Held
                ?? Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).First(x => x.OwnerSlot == target.PlayerSlot);
            if (target.GetComponent<Carrier>().Held != shoe) Assert.IsTrue(shoe.HostForceEquip(target), "Could not put the slipper in their hand.");
            yield return new WaitForSeconds(.2f);
            Vector3 stood = Flat(target.transform.position);
            float before = Flat(target.transform.position - can).magnitude;
            // A prayer and a long press: held, the ring sits where he aims (here, on the target); the release opens it there.
            sean.Intent.AimPoint = target.transform.position + Vector3.up;
            yield return Press(sean, Verb.Skill2, .4f);
            bool marked = false; float placedOff = float.MaxValue, t0 = Time.time;
            while (Time.time - t0 < 1.0f)
            {
                marked |= Object.FindObjectsByType<SeanGroundMark>(FindObjectsSortMode.None).Length > 0;
                foreach (var f in Object.FindObjectsByType<SeanMagmaFist>(FindObjectsSortMode.None))
                    placedOff = Mathf.Min(placedOff, Flat(f.transform.position - target.transform.position).magnitude);
                yield return null;
            }
            yield return new WaitForSeconds(.8f);
            float after = Flat(target.transform.position - can).magnitude;
            Note("fist_placed_off_target_m", placedOff); Note("fist_target_marked", marked); Note("fist_distance_from_can_before_m", before); Note("fist_distance_from_can_after_m", after);
            Assert.Less(placedOff, .4f, "The puddle did not open where he aimed.");
            Assert.IsTrue(marked, "The spot had no warning ring on it.");
            Assert.Greater(after - before, SeanRules.FistPush * .6f, "The fist did not throw them away from the can.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanSlam && f.subject == target.PlayerSlot), "The punch was not announced.");
            float shoeFromSpot = (Flat(shoe.transform.position) - stood).magnitude;
            Note("fist_slipper_state", shoe.State.ToString()); Note("fist_slipper_from_spot_m", shoeFromSpot);
            Note("fist_target_from_slipper_m", Flat(target.transform.position - shoe.transform.position).magnitude);
            Assert.IsNull(target.GetComponent<Carrier>().Held, "They kept the slipper in their hand.");
            Assert.AreEqual(SlipperState.Loose, shoe.State, "The slipper is not loose on the court.");
            Assert.Less(shoeFromSpot, .6f, "The slipper did not stay on the spot they were thrown from.");
            Assert.Greater(Flat(target.transform.position - shoe.transform.position).magnitude, SeanRules.FistPush * .6f,
                "The slipper went with them.");
        }

        // ------------------------------------------------------------------ MAYON AWAKENS v2: the mountain, Molten, its bombs

        private static CharacterMotor Taya() => GameServices.Round.Players.First(p => p.IsDefender);

        private static SeanVolcano MountainOf(CharacterMotor sean)
            => Object.FindObjectsByType<SeanVolcano>().FirstOrDefault(v => v != null && v.OwnerSlot == sean.PlayerSlot);

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMountainRisesWhereHeAimedThrowsEveryoneOutAndBombardsTheTaya()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Vector3 can = Flat(lata.transform.position);
            Vector3 lane = Vector3.back;
            Vector3 aim = can + lane * 9f + Vector3.up * .12f;
            var sean = Sean(round.PlayerAt(1), can + lane * 3f + Vector3.up * .12f, aim);
            var kit = (SeanHeroKit)sean.AbilitySystem.Kit;
            Assert.IsFalse(sean.IsDefender, "This reproduction needs the attacking kit.");
            kit.AddUltimateCharge(100);
            var taya = Taya();
            taya.Teleport(aim + Vector3.right * 2.2f); taya.Intent.Clear(); taya.Intent.Parked = true; taya.ClearStun();
            var near = Attacker(sean.PlayerSlot);
            near.Teleport(aim + Vector3.left * 1.8f); near.Intent.Parked = true; near.ClearStun();
            yield return new WaitForSeconds(.2f);
            Vector3 tayaFrom = taya.transform.position, nearFrom = near.transform.position;

            yield return Press(sean, Verb.Ultimate, .3f);
            float t0 = Time.time;
            while (Time.time - t0 < 12f && !kit.IsMolten) yield return null;
            Assert.IsTrue(kit.IsMolten, "MAYON AWAKENS never made him Molten.");
            var mountain = MountainOf(sean);
            Assert.IsNotNull(mountain, "No mountain rose.");
            float placedOff = Flat(mountain.Foot - aim).magnitude;
            Note("mountain_placed_off_aim_m", placedOff);
            Assert.Less(placedOff, .6f, "The mountain did not rise where he aimed.");
            var solid = mountain.GetComponentInChildren<Collider>();
            Assert.IsTrue(solid != null && solid.enabled, "The mountain is not solid.");

            yield return new WaitForSeconds(.6f);
            float tayaThrown = Flat(taya.transform.position - tayaFrom).magnitude, nearThrown = Flat(near.transform.position - nearFrom).magnitude;
            Note("rise_threw_taya_m", tayaThrown); Note("rise_threw_attacker_m", nearThrown);
            // Judged against each body's own knockback scale (Dante's Earthbound halves every push he takes).
            float tayaScale = Mathf.Clamp01(taya.AbilitySystem?.Kit?.IncomingKnockbackDistanceScale ?? 1f);
            float nearScale = Mathf.Clamp01(near.AbilitySystem?.Kit?.IncomingKnockbackDistanceScale ?? 1f);
            Note("rise_taya_scale", tayaScale); Note("rise_attacker_scale", nearScale);
            Assert.Greater(tayaThrown, SeanRules.VolcanoBlastPush * tayaScale * .6f, "The rise did not throw the taya out.");
            Assert.Greater(nearThrown, SeanRules.VolcanoBlastPush * nearScale * .6f, "The rise did not throw the attacker beside it out.");
            Assert.IsTrue(lata.IsUpright, "The rise knocked the can over; only a bomb may.");
            Assert.IsTrue(_flair.Any(f => f.kind == MatchFlair.Kind.SeanBlast && f.subject == taya.PlayerSlot), "The rise's hit was not announced.");

            // Molten: nothing moves him, and E is his role skill again (the mountain throws the bombs).
            Vector3 held = sean.transform.position;
            sean.ApplyResolvedImpact(Vector3.right * 14f + Vector3.up * 3f);
            yield return new WaitForSeconds(.5f);
            Note("molten_moved_m", Flat(sean.transform.position - held).magnitude);
            // SEARED lands once their slide is over (so it never shortens the throw).
            Note("rise_taya_seared_s", taya.SearedLeft);
            Assert.Greater(taya.SearedLeft, 0f, "The rise did not SEAR them.");
            Assert.Less(Flat(sean.transform.position - held).magnitude, .25f, "A push moved Molten Sean.");
            Assert.AreNotEqual("VOLCANIC BOMB", kit.Skill2.LiveName, "E still reads the old bomb.");

            // It lobs at the taya (his opponent), never at his own side.
            t0 = Time.time;
            while (Time.time - t0 < 3f && !_flair.Any(f => f.kind == MatchFlair.Kind.SeanVolcanoBomb)) yield return null;
            var bombs = _flair.Where(f => f.kind == MatchFlair.Kind.SeanVolcanoBomb).ToList();
            Note("bombs_by_1_8_s", bombs.Count);
            Assert.IsTrue(bombs.Count > 0, "The mountain threw no bomb.");
            Assert.IsTrue(bombs.All(f => f.subject == taya.PlayerSlot), "A bomb was thrown at his own side.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMountainNeverStandsOnTheCanItsBombsKnockTheCanOverAndItSinks()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Vector3 can = Flat(lata.transform.position);
            Vector3 lane = Vector3.back;
            var sean = Sean(round.PlayerAt(1), can + lane * 6f + Vector3.up * .12f, can + Vector3.up * .12f);
            var kit = (SeanHeroKit)sean.AbilitySystem.Kit;
            kit.AddUltimateCharge(100);
            var taya = Taya();
            taya.Intent.Clear(); taya.Intent.Parked = true;
            yield return Press(sean, Verb.Ultimate, .3f);
            float t0 = Time.time;
            while (Time.time - t0 < 12f && !kit.IsMolten) yield return null;
            var mountain = MountainOf(sean);
            Assert.IsNotNull(mountain, "No mountain rose.");
            float fromCan = Flat(mountain.Foot - can).magnitude;
            Note("mountain_from_can_m", fromCan);
            Assert.GreaterOrEqual(fromCan, SeanRules.VolcanoClearOfCan - .1f, "Aimed on the can, the mountain stood on it.");

            // The taya back beside the can after the rise threw them: the next bomb comes down on both.
            yield return new WaitForSeconds(.3f);
            taya.Teleport(can + Vector3.right * 1.2f + Vector3.up * .12f); taya.ClearStun();
            t0 = Time.time;
            while (Time.time - t0 < 4f && lata.IsUpright)
            {
                if (Flat(taya.transform.position - can).magnitude > 1.6f) { taya.Teleport(can + Vector3.right * 1.2f + Vector3.up * .12f); taya.ClearStun(); }
                yield return null;
            }
            Note("bomb_knocked_can", !lata.IsUpright);
            Assert.IsFalse(lata.IsUpright, "A bomb landing by the can did not knock it over.");

            // It cools and sinks when the eruption ends, and Molten ends with it.
            t0 = Time.time;
            while (Time.time - t0 < SeanRules.MoltenSeconds + SeanRules.VolcanoSinkSeconds + 2f && MountainOf(sean) != null) yield return null;
            Note("mountain_gone", MountainOf(sean) == null);
            Assert.IsNull(MountainOf(sean), "The mountain never sank.");
            Assert.IsFalse(kit.IsMolten, "Molten outlived its mountain.");
        }
    }
}
