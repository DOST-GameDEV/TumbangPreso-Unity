using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ PAETE, PLAYED (HERO-9 step 2): a real Hero Strike match on Bayan Plaza, every ability pressed
    /// through `InputIntent` exactly as a player or a bot presses it, host-resolved as in a match. It
    /// answers the questions the kit had never been asked in play: does the vine reel him, does the
    /// seedling grow, fire on the second press and come out of the ground to an opponent's Interact
    /// hold only after its 15 s, does THORN HARVEST take slippers even out of a hand, does the sentry root the
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
            // ⚠️⚠️ THE BODY AS WELL AS THE KIT (owner, 2026-09-26, of the skills film: *"idk why a fkn CHARACTER was the one
            // doing the shit instead of the plants"*). The match dresses every seat once, at install, from the pick it had
            // then (`MatchInstaller`), so re-binding the kit here left a curly-haired human casting Paete's vines and
            // thorns in every film, and the one seat that did get his body was the local one, hidden in first person.
            // Every converted seat now wears his model, and the local seat's first-person arms are matched to it,
            // because `ViewmodelArms` only re-reads the body when it is bound (a late swap kept human hands).
            var art = RosterBook.Load().FindPersonArt("paete");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            if (slot == GameLaunch.SoloSeat)
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(who);
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

        private static PaeteWoodenSlipper[] WoodenShots()
            => Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None);

        [UnityTest]
        public IEnumerator AutonomousBakyaLobNeedsNoSecondPress()
        {
            var who = Paete(1, new Vector3(0, .12f, -9));
            who.Intent.AimPoint = who.transform.position + Vector3.forward * 4f;
            yield return Press(who, Verb.Skill2);
            var plant = PaetePlant.OwnedBy(1); Assert.IsNotNull(plant);
            Vector3 muzzle = plant.Muzzle;
            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds + .5f);
            var shots = WoodenShots();
            Assert.AreEqual(1, shots.Length, "The grown plant must lob at the upright can without another Skill2 press.");
            Assert.Greater(Vector3.Distance(Flat(muzzle), Flat(shots[0].transform.position)), 2f);
            Assert.IsFalse(plant.ShotReady, "The automatic lob must consume the grown slipper.");
        }

        [UnityTest]
        public IEnumerator AutonomousLobsWaitForMaturityAndRepeatAtFiveSeconds()
        {
            // A long clear lane keeps this cadence check independent of can knockdown.
            var plant = PaetePlant.Spawn(new Vector3(0, 1, -25), new Vector3(0, 0, -25), 1, 0f);
            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds - .25f);
            Assert.AreEqual(0, WoodenShots().Length, "The seedling fired before maturity.");
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(1, WoodenShots().Length, "A mature plant did not automatically fire.");
            Assert.IsFalse(plant.ShotReady);
            yield return new WaitForSeconds(4.5f);
            Assert.AreEqual(0, WoodenShots().Length, "The next lob fired before its five-second interval.");
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(1, WoodenShots().Length, "The plant did not make its next lob after five seconds.");
        }

        [UnityTest]
        public IEnumerator NoUprightTargetKeepsTheGrownSlipperReady()
        {
            var can = GameServices.Round.Lata;
            can.ApplySnapshotState(can.transform.position, can.transform.rotation, false, can.SkinIndex);
            var plant = PaetePlant.Spawn(new Vector3(0, 1, -25), new Vector3(0, 0, -25), 1, 0f);
            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds + .25f);
            Assert.AreEqual(0, WoodenShots().Length);
            Assert.IsTrue(plant.ShotReady, "No target must not consume the grown slipper.");
            can.ApplySnapshotState(can.transform.position, Quaternion.identity, true, can.SkinIndex);
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(1, WoodenShots().Length, "The waiting mature plant did not fire when a valid target returned.");
        }

        [UnityTest]
        public IEnumerator PausedAndRetiringPlantsNeverAutomaticallyFire()
        {
            var plant = PaetePlant.Spawn(new Vector3(0, 1, -25), new Vector3(0, 0, -25), 1, 0f);
            yield return new WaitForSeconds(1f);
            float age = plant.Age, previous = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                for (int i = 0; i < 12; i++) yield return null;
                Assert.AreEqual(age, plant.Age, .0001f);
                Assert.AreEqual(0, WoodenShots().Length);
            }
            finally { Time.timeScale = previous; }
            plant.Wither();
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(0, WoodenShots().Length, "A withering plant fired a new slipper.");
        }

        private sealed class PlantReplicaProvider : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        [UnityTest]
        public IEnumerator ReplicaWaitsForTheMatchingAcceptedAutomaticShot()
        {
            NetAuthority.Provider = new PlantReplicaProvider();
            var plant = PaetePlant.Spawn(new Vector3(0, 1, -25), new Vector3(0, 0, -25), 1, 0f);
            plant.AdoptInstance(901);
            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds + .2f);
            Assert.AreEqual(0, WoodenShots().Length, "A nonhost must not invent autonomous shots.");
            var method = typeof(PaetePlant).GetMethod("ApplyAutomaticShot");
            Assert.IsNotNull(method, "The accepted automatic-shot delivery path is missing.");
            Vector3 target = GameServices.Round.Lata.transform.position;
            Assert.IsFalse((bool)method.Invoke(null, new object[] { 1, 900L, target }));
            Assert.AreEqual(0, WoodenShots().Length);
            Assert.IsTrue((bool)method.Invoke(null, new object[] { 1, 901L, target }));
            Assert.AreEqual(1, WoodenShots().Length);
            Assert.IsFalse(plant.ShotReady);
        }

        [UnityTest]
        public IEnumerator WoodenSlipperLeavesTheMuzzleAtAuthoredSpeed()
        {
            Vector3 origin = new Vector3(0f, 1.5f, -9f);
            var shoe = PaeteWoodenSlipper.Spawn(origin, origin + Vector3.forward * 8f, 1);
            var renderers = shoe.GetComponentsInChildren<Renderer>();
            Assert.AreEqual(3, renderers.Length, "The wooden sole and both straps must be drawn.");
            foreach (var renderer in renderers)
                Assert.IsTrue(renderer.enabled && renderer.bounds.size.sqrMagnitude > .001f);
            yield return new WaitForSeconds(.25f);
            float travel = Vector3.Distance(Flat(origin), Flat(shoe.transform.position));
            Note("wooden_slipper_quarter_second_travel", travel);
            Assert.Greater(travel, 2f, "A ready wooden slipper fell beside its muzzle instead of flying at 13 m/s.");
        }

        [UnityTest]
        public IEnumerator ReadyBakyaRecastThrowsAVisibleMovingSlipper()
        {
            // Keep the autonomous target unavailable while exercising the retained manual command.
            var target = GameServices.Round.Lata;
            target.ApplySnapshotState(target.transform.position, target.transform.rotation, false, target.SkinIndex);
            var paete = Paete(1, new Vector3(0, .12f, -9));
            Assert.IsFalse(paete.IsDefender, "This reproduction requires the attacking kit.");
            paete.Intent.AimPoint = paete.transform.position + Vector3.forward * 4f;
            yield return Press(paete, Verb.Skill2);
            var plant = PaetePlant.OwnedBy(paete.PlayerSlot);
            Assert.IsNotNull(plant);
            yield return new WaitForSeconds(PaeteRules.PlantFirstShotSeconds + .5f);
            Assert.IsTrue(plant.ShotReady);
            Vector3 muzzle = plant.Muzzle;
            paete.Intent.AimPoint = muzzle + Vector3.forward * 8f;
            yield return Press(paete, Verb.Skill2);
            yield return new WaitForSeconds(.1f);
            var shoe = Object.FindFirstObjectByType<PaeteWoodenSlipper>();
            Assert.IsNotNull(shoe, "The actual ready recast did not spawn its wooden slipper.");
            Assert.IsFalse(plant.ShotReady, "The successful command must consume the grown slipper.");
            float travel = Vector3.Distance(Flat(muzzle), Flat(shoe.transform.position));
            Note("ready_recast_slipper_travel", travel);
            Assert.Greater(travel, 2f, "The actual ready recast spawned a shoe that dropped beside the plant.");
            foreach (var renderer in shoe.GetComponentsInChildren<Renderer>())
                Assert.IsTrue(renderer.enabled && renderer.bounds.size.sqrMagnitude > .001f);
        }

        [UnityTest]
        public IEnumerator UngrownBakyaCommandStillRefusesToFire()
        {
            var plant = PaetePlant.Spawn(new Vector3(0, 1f, -9), new Vector3(0, 0, -5), 1);
            yield return new WaitForSeconds(.5f);
            Assert.IsFalse(plant.ShotReady);
            Assert.IsFalse(plant.Fire(plant.Muzzle + Vector3.forward * 8f));
            Assert.IsNull(Object.FindFirstObjectByType<PaeteWoodenSlipper>());
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
            Assert.Greater(travelled, 3.0f, "Liana Leap did not carry him toward the anchor.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheSeedlingGrowsFiresAndComesOutOnlyWhenLoose()
        {
            // A down can leaves the grown slipper waiting for this explicit manual command.
            var target = GameServices.Round.Lata;
            target.ApplySnapshotState(target.transform.position, target.transform.rotation, false, target.SkinIndex);
            var paete = Paete(1, new Vector3(0, .12f, -9));
            paete.Intent.AimPoint = paete.transform.position + new Vector3(0, 0, 4f);
            yield return Press(paete, Verb.Skill2);
            yield return new WaitForSeconds(.8f);
            var plant = PaetePlant.OwnedBy(paete.PlayerSlot);
            Note("plant_spawned", plant != null);
            Assert.IsNotNull(plant, "Bakya Bloom planted nothing.");

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
            // ⚠️ PLACED WHERE HE LOOKS (owner, 2026-09-26: *"castable and not cast on body"*): aimed 5 m away, past the holder,
            // so the rattan must travel there, burst THERE and haul the slipper into itself, not to his feet.
            var spot = new Vector3(5f, 0f, -4f);
            paete.Intent.AimPoint = spot;
            yield return Press(paete, Verb.Skill2);
            var thorns = Object.FindFirstObjectByType<PaeteThorns>();
            Assert.IsNotNull(thorns, "THORN HARVEST put no thorns down.");
            float placedOff = Vector3.Distance(Flat(thorns.Origin), Flat(spot));
            yield return new WaitForSeconds(1.4f);
            float home = Vector3.Distance(Flat(shoe.transform.position), Flat(spot));
            float fromHim = Vector3.Distance(Flat(shoe.transform.position), Flat(paete.transform.position));
            Note("thorn_took_from_hand", holder.GetComponent<Carrier>().Held != shoe); Note("thorn_slipper_distance_m", home);
            Note("thorn_placed_off_aim_m", placedOff); Note("thorn_slipper_from_paete_m", fromHim);
            Assert.Less(placedOff, 0.3f, "THORN HARVEST did not burst where he aimed it.");
            Assert.AreNotSame(shoe, holder.GetComponent<Carrier>().Held, "THORN HARVEST left the slipper in the holder's hand.");
            Assert.Less(home, 2.2f, "The slipper was not hauled into the thorns where he placed them.");
            Assert.Greater(fromHim, 3f, "The slipper went to his feet, not to the thorns he placed.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PlayerVinePullMovesBothUnequallyAndStopsAtContact()
        {
            int previous=Time.captureFramerate;Time.captureFramerate=60;
            try
            {
                var caster=Paete(1,new Vector3(-5,.12f,-6));
                var target=GameServices.Round.PlayerAt(2);target.Teleport(new Vector3(-5,.12f,0));
                caster.Intent.Parked=target.Intent.Parked=false;
                caster.Intent.Move=target.Intent.Move=Vector2.zero;
                yield return null;
                Vector3 a=caster.transform.position,b=target.transform.position;
                var pull=PaetePlayerPull.Begin(caster,target);Assert.IsNotNull(pull);
                float until=Time.time+2,minGap=99;
                while(pull!=null&&pull.Active&&Time.time<until)
                {
                    yield return new WaitForFixedUpdate();
                    minGap=Mathf.Min(minGap,Flat(target.transform.position-caster.transform.position).magnitude);
                }
                Assert.IsTrue(pull==null||!pull.Active,"Pull did not terminate.");
                float casterTravel=Flat(caster.transform.position-a).magnitude;
                float targetTravel=Flat(target.transform.position-b).magnitude;
                Assert.Greater(casterTravel,3);Assert.That(targetTravel,Is.InRange(.15f,1.3f));
                Assert.Greater(casterTravel,targetTravel*2);
                Assert.Greater(minGap,.55f,"Capsules crossed through each other.");
                Assert.Less(Flat(target.transform.position-caster.transform.position).magnitude,1.1f);
                var stopA=caster.transform.position;var stopB=target.transform.position;
                yield return new WaitForSeconds(.25f);
                Assert.Less(Flat(caster.transform.position-stopA).magnitude,.08f,"Caster retained a friction tail after contact.");
                Assert.Less(Flat(target.transform.position-stopB).magnitude,.08f,"Target retained a friction tail after contact.");
                Note("player_pull_caster",casterTravel);Note("player_pull_target",targetTravel);Note("player_pull_min_gap",minGap);
            }
            finally{Time.captureFramerate=previous;}
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ActualVineSkillPullsBothPlayersAndStopsAtContact()
        {
            int previous=Time.captureFramerate;Time.captureFramerate=60;
            try
            {
                var caster=Paete(1,new Vector3(-5,.12f,-6));
                var target=GameServices.Round.PlayerAt(2);target.Teleport(new Vector3(-5,.12f,0));
                caster.Intent.Parked=target.Intent.Parked=false;
                caster.Intent.Move=target.Intent.Move=Vector2.zero;
                yield return null;
                Vector3 a=caster.transform.position,b=target.transform.position;
                caster.Intent.AimPoint=target.transform.position+Vector3.up*.9f;
                yield return Press(caster,Verb.Skill1,.16f);
                var pull=Object.FindFirstObjectByType<PaetePlayerPull>();Assert.IsNotNull(pull,"Actual signature did not attach to the player.");
                float until=Time.time+2,minGap=99;
                while(pull!=null&&pull.Active&&Time.time<until)
                {
                    yield return new WaitForFixedUpdate();
                    minGap=Mathf.Min(minGap,Flat(target.transform.position-caster.transform.position).magnitude);
                }
                Assert.IsTrue(pull==null||!pull.Active,"Pull did not terminate.");
                float casterTravel=Flat(caster.transform.position-a).magnitude;
                float targetTravel=Flat(target.transform.position-b).magnitude;
                Assert.Greater(casterTravel,3);Assert.That(targetTravel,Is.InRange(.15f,1.3f));
                Assert.Greater(casterTravel,targetTravel*2);
                Assert.Greater(minGap,.55f,"Capsules crossed through each other.");
                Assert.Less(Flat(target.transform.position-caster.transform.position).magnitude,1.1f);
                var stopA=caster.transform.position;var stopB=target.transform.position;
                yield return new WaitForSeconds(.25f);
                Assert.Less(Flat(caster.transform.position-stopA).magnitude,.08f,"Caster retained a friction tail after contact.");
                Assert.Less(Flat(target.transform.position-stopB).magnitude,.08f,"Target retained a friction tail after contact.");
                Note("player_pull_caster",casterTravel);Note("player_pull_target",targetTravel);Note("player_pull_min_gap",minGap);
            }
            finally{Time.captureFramerate=previous;}
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PlayerVineReleasesBothWhenInterruptedAndPreservesNewImpact()
        {
            int previous=Time.captureFramerate;Time.captureFramerate=60;
            try
            {
                var caster=Paete(1,new Vector3(-5,.12f,-6));
                var target=GameServices.Round.PlayerAt(2);target.Teleport(new Vector3(-5,.12f,0));
                caster.Intent.Parked=target.Intent.Parked=false;
                yield return null;
                var pull=PaetePlayerPull.Begin(caster,target);Assert.IsNotNull(pull);
                yield return new WaitForSeconds(.2f);
                Vector3 before=target.transform.position;
                target.ApplyResolvedImpact(Vector3.right*5);
                Assert.IsFalse(pull.Active,"An accepted new impact must cancel both sides immediately.");
                yield return new WaitForSeconds(.12f);
                Assert.Greater(target.transform.position.x-before.x,.1f,"Ending the hook erased a newer impact.");
                yield return new WaitForSeconds(.35f);
                caster.Teleport(new Vector3(-5,.12f,-6));target.Teleport(new Vector3(-5,.12f,0));
                yield return null;
                pull=PaetePlayerPull.Begin(caster,target);Assert.IsNotNull(pull);
                target.enabled=false;
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(pull==null||!pull.Active,"Disabled participant left a live pull.");
                target.enabled=true;
            }
            finally{Time.captureFramerate=previous;}
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PlayerVineStopsAgainstAnObstacleWithoutCrossingIt()
        {
            int previous=Time.captureFramerate;Time.captureFramerate=60;
            GameObject wall=null;
            try
            {
                var caster=Paete(1,new Vector3(-5,.12f,-6));
                var target=GameServices.Round.PlayerAt(2);target.Teleport(new Vector3(-5,.12f,0));
                caster.Intent.Parked=target.Intent.Parked=false;
                yield return null;
                var pull=PaetePlayerPull.Begin(caster,target);Assert.IsNotNull(pull);
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position=new Vector3(-5,1,-3);wall.transform.localScale=new Vector3(2,3,.4f);
                Physics.SyncTransforms();
                yield return new WaitForSeconds(1.2f);
                Assert.IsTrue(pull==null||!pull.Active);
                Assert.Less(caster.transform.position.z,-3.4f,"Controller passed through the obstacle.");
                Assert.Greater(target.transform.position.z,-1.35f,"Blocked hook over-pulled its target.");
            }
            finally{Time.captureFramerate=previous;if(wall!=null)Object.Destroy(wall);}
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PlayerVineRejectsTouchingAndReleasesOnTeleportOrKitChange()
        {
            var caster=Paete(1,new Vector3(-5,.12f,-6));
            var target=GameServices.Round.PlayerAt(2);target.Teleport(caster.transform.position+Vector3.forward*.5f);
            caster.Intent.Parked=target.Intent.Parked=false;
            Assert.IsNull(PaetePlayerPull.Begin(caster,target),"Already touching bodies should not be yanked through each other.");
            target.Teleport(new Vector3(-5,.12f,0));yield return null;
            var pull=PaetePlayerPull.Begin(caster,target);Assert.IsNotNull(pull);
            target.Teleport(target.transform.position+Vector3.right);
            yield return new WaitForFixedUpdate();Assert.IsTrue(pull==null||!pull.Active,"Teleport epoch retained a prior attachment.");
            target.Teleport(new Vector3(-5,.12f,0));yield return null;
            pull=PaetePlayerPull.Begin(caster,target);Assert.IsNotNull(pull);
            caster.AbilitySystem.BindHero("sean");
            yield return new WaitForFixedUpdate();Assert.IsTrue(pull==null||!pull.Active,"A replacement kit inherited the old hook.");
        }

        private sealed class VineOwnerPeer : INetProvider
        { public bool IsHost=>false;public bool IsNetworked=>true;public int LocalSlot=>2;public int LocalPeerId=>7;public bool IsSeatlessReferee=>false; }

        [UnityTest, Timeout(60000)]
        public IEnumerator VineReceiverRejectsForeignStaleAndDuplicateStates()
        {
            var caster=Paete(1,new Vector3(-5,.12f,-6));
            var target=GameServices.Round.PlayerAt(2);target.Teleport(new Vector3(-5,.12f,0));
            caster.Intent.Parked=target.Intent.Parked=false;
            yield return null;
            var router=TumbangPreso.Net.MatchRpc.Instance;
            GameObject built=null;
            if(router==null){built=new GameObject("Vine receipt router");router=built.AddComponent<TumbangPreso.Net.MatchRpc>();}
            long match=GameServices.Match.PresentationMatchId;
            typeof(TumbangPreso.Net.MatchRpc).GetProperty("PresentationMatchId").SetValue(router,match);
            NetAuthority.Provider=new VineOwnerPeer();
            var state=new TumbangPreso.Net.PaeteVineState {Scope=new TumbangPreso.Net.GameplayActionScope {
                Match=match,Round=GameServices.Match.RoundNumber,Epoch=caster.MovementEpoch},Sequence=1,
                Owner=1,Target=2,TargetEpoch=target.MovementEpoch,Phase=TumbangPreso.Net.PaeteVinePhase.Player,
                Anchor=target.transform.position+Vector3.up*.9f,CasterEnd=new Vector3(-5,.12f,-1.824f),
                TargetEnd=new Vector3(-5,.12f,-1.044f),Duration=.8f,RoundClock=GameServices.Round.TimeLeft};
            var method=typeof(TumbangPreso.Net.MatchRpc).GetMethod("OnPaeteVineMsg",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            void Deliver(ulong sender,TumbangPreso.Net.PaeteVineState packet)
            {
                using var writer=new Unity.Netcode.FastBufferWriter(256,Unity.Collections.Allocator.Temp);
                writer.WriteNetworkSerializable(packet);
                using var reader=new Unity.Netcode.FastBufferReader(writer,Unity.Collections.Allocator.Temp);
                method.Invoke(router,new object[]{sender,reader});
            }
            try
            {
                Deliver(7,state);Assert.IsNull(Object.FindFirstObjectByType<PaetePlayerPull>(),"A non-host selected the victim.");
                var stale=state;stale.TargetEpoch++;Deliver(0,stale);
                Assert.IsNull(Object.FindFirstObjectByType<PaetePlayerPull>(),"A different target incarnation accepted the hook.");
                stale=state;stale.Scope.Round++;Deliver(0,stale);
                Assert.IsNull(Object.FindFirstObjectByType<PaetePlayerPull>());
                Deliver(0,state);var pull=Object.FindFirstObjectByType<PaetePlayerPull>();Assert.IsNotNull(pull);
                Deliver(0,state);Assert.IsTrue(pull.Active,"A duplicate restarted/replaced the current constraint.");
                Vector3 casterBefore=caster.transform.position,targetBefore=target.transform.position;
                yield return new WaitForSeconds(.24f);
                Assert.Less(Flat(caster.transform.position-casterBefore).magnitude,.01f,"A peer moved somebody else's body.");
                Assert.Greater(Flat(target.transform.position-targetBefore).magnitude,.03f,"Owning peer did not integrate its target pull.");
                state.Sequence=2;state.Phase=TumbangPreso.Net.PaeteVinePhase.Ended;state.Duration=0;Deliver(0,state);
                Assert.IsTrue(pull==null||!pull.Active);
                state.Sequence=1;state.Phase=TumbangPreso.Net.PaeteVinePhase.Player;state.Duration=.8f;Deliver(0,state);
                Assert.IsTrue(pull==null||!pull.Active,"Old start revived an ended hook.");
            }
            finally{NetAuthority.Provider=new SoloProvider();if(built!=null)Object.Destroy(built);}
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator VinePlayerQueryChoosesNearestVisibleBodyAndHonoursWallsAndRange()
        {
            var caster = Paete(1,new Vector3(-5,.12f,-6));
            var near = GameServices.Round.PlayerAt(2); var far = GameServices.Round.PlayerAt(3);
            near.Teleport(new Vector3(-5,.12f,-2)); far.Teleport(new Vector3(-5,.12f,.5f));
            yield return null;
            Vector3 aim = near.transform.position+Vector3.up;
            Assert.AreSame(near,PaeteVine.FindPlayer(caster,caster.transform.position,Vector3.forward,aim));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(-5,1,-4); wall.transform.localScale = new Vector3(2,3,.2f);
            try
            {
                Physics.SyncTransforms();
                Assert.IsNull(PaeteVine.FindPlayer(caster,caster.transform.position,Vector3.forward,aim),"A nearer wall must stop player attachment.");
            }
            finally { Object.Destroy(wall); }
            yield return null;
            near.Teleport(new Vector3(-5,.12f,3)); far.Teleport(new Vector3(12,.12f,4));
            yield return null;
            Assert.IsNull(PaeteVine.FindPlayer(caster,caster.transform.position,Vector3.forward,near.transform.position+Vector3.up),"No player latch beyond the existing eight-metre reach.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ThornContactPrecedesSnatchAndThePullHasReadableTravel()
        {
            int priorCaptureRate = Time.captureFramerate;
            Time.captureFramerate = 60;
            try
            {
            int taya = GameServices.Round.Players.First(p => p.IsDefender).PlayerSlot;
            var holder = GameServices.Round.Players.First(p => !p.IsDefender && p.GetComponent<Carrier>().Held != null);
            holder.Teleport(new Vector3(5,.12f,-4));
            yield return null; // The held prop follows its hand in LateUpdate before target capture.
            var shoe = holder.GetComponent<Carrier>().Held;
            var at = new Vector3(0,0,-4);
            var thorns = PaeteThorns.Spawn(at,at,taya);
            while(thorns.Age < PaeteRules.ThornReachSeconds-.06f) yield return null;
            Assert.AreSame(shoe,holder.GetComponent<Carrier>().Held,"No snatch before the vine tip arrives.");
            while(thorns.Age < PaeteRules.ThornHoldSeconds+.15f) yield return null;
            Assert.AreNotSame(shoe,holder.GetComponent<Carrier>().Held,"Contact must release the held slipper.");
            float early = Flat(shoe.transform.position-at).magnitude;
            Assert.Greater(early,2.5f,"The slower pull must have a readable travel phase, not teleport home.");
            while(thorns.Age < PaeteRules.ThornHoldSeconds+PaeteRules.ThornYankSeconds+.08f) yield return null;
            float arrived = Flat(shoe.transform.position-at).magnitude;
            Assert.Less(arrived,1.3f,"Pull must end beside the same construct.");
            Assert.Less(arrived,early-1f);
            Note("thorn_contact_seconds",PaeteRules.ThornReachSeconds);
            Note("thorn_pull_early_distance",early); Note("thorn_pull_final_distance",arrived);
            }
            finally { Time.captureFramerate = priorCaptureRate; }
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

        /// <summary>
        /// ⚠️⚠️ THE GUARDIAN NEVER STANDS ON THE CAN (owner, 2026-09-27: *"make it so that it cant block the can too (dont let it be
        /// placed in a place it STANDS on can)"*). Aimed straight at the lata through real input, the tree comes up at least
        /// `PaeteRules.SentryCanClearance` from it, toward him, and still catches the body standing beside the can.
        /// </summary>
        [UnityTest]
        public IEnumerator AimedAtTheCanTheGuardianComesUpBesideIt()
        {
            var round = GameServices.Round;
            var lata = round.Lata;
            Assert.IsNotNull(lata, "No lata on the court.");
            var can = Flat(lata.transform.position);
            var paete = Paete(1, can + new Vector3(0, .12f, -6));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var a = round.PlayerAt(2);
            a.Teleport(can + new Vector3(2.8f, .12f, 1f)); a.Intent.Parked = false;
            paete.Intent.AimPoint = lata.transform.position;
            yield return Press(paete, Verb.Ultimate, .2f);
            float t0 = Time.time;
            PaeteSentry sentry = null;
            while (Time.time - t0 < 8f && (sentry = Object.FindFirstObjectByType<PaeteSentry>()) == null) yield return null;
            Assert.IsNotNull(sentry, "The ultimate never raised its guardian.");
            float gap = Vector3.Distance(Flat(sentry.Centre), Flat(lata.transform.position));
            Note("sentry_can_gap_m", gap);
            Assert.GreaterOrEqual(gap, PaeteRules.SentryCanClearance - .05f, "The guardian came up on the can.");
            Assert.Less(Flat(sentry.Centre).z, can.z, "Aimed at the can, the guardian was not pushed back toward him.");
            while (Time.time - t0 < 9f && !a.IsRooted) yield return null;
            Note("sentry_can_still_catches", a.IsRooted);
            Assert.IsTrue(a.IsRooted, "Pushed off the can, it no longer caught the body standing beside it.");
        }

        /// <summary>
        /// ⚠️ A FILM, NOT A CLAIM (owner, 2026-09-26: *"in the video can u try to record as well people getting
        /// pulled and rooted towards it"*). Three players stand 5 to 6 m from where the seed will land; he casts
        /// MAKILING'S EMBRACE through real input and the whole of it is recorded: the introduction on his own
        /// screen (`owner/`), and the tree rising, the drag, the embrace and the sleep from a wide camera.
        /// Frames: `Logs/improvement-baseline-v1/paete-sentry-film/`. Runs only with TUMP_PAETE_FILM=1.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator FilmTheSentryPullingAndRootingThem()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            var round = GameServices.Round;
            // Paete is the local player (seat 1, `GameLaunch.SoloSeat`), a human with his own model, so
            // `owner/` is HIS screen, the introduction and all. (The first film left him a bot in the default
            // body: no introduction plays for a bot, and his arms were someone else's.)
            var paete = HumanPaete(new Vector3(0, .12f, -11));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var others = new[] { round.PlayerAt(0), round.PlayerAt(2), round.PlayerAt(3) };
            var stands = new[] { new Vector3(5.6f, .12f, -2.2f), new Vector3(-5.2f, .12f, -1.6f), new Vector3(1.8f, .12f, 2.2f) };
            for (int i = 0; i < others.Length; i++)
            {
                others[i].Teleport(stands[i]); others[i].Intent.Parked = false;
                others[i].transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -3) - stands[i]);
            }
            paete.Intent.AimPoint = new Vector3(0, 0, -3);
            var camera = new GameObject("PaeteSentryFilm").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 58; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            bool rootedAll = false;
            yield return ImprovementEvidenceProbe.Record(camera, "paete-sentry-film", 16f, paete,
                drive: t =>
                {
                    paete.Intent.Set(Verb.Ultimate, t < .25f);
                    rootedAll |= others[0].IsRooted && others[1].IsRooted && others[2].IsRooted;
                },
                witnessOffset: new Vector3(6f, 7f, 16f), witnessLookHeight: 2.6f);
            Object.Destroy(camera.gameObject);
            Note("film_all_rooted", rootedAll);
            Assert.IsTrue(rootedAll, "The film never showed all three rooted.");
        }

        /// <summary>
        /// ⚠️⚠️ THE ULTIMATE AS A VIDEO (HERO-9 "Film the cutscene ON HIS SCREEN in a match", and the owner,
        /// 2026-09-26: *"give me A video of his finished ULT animation + ppl getting pulled in too"*). Three
        /// views of ONE cast, each frame written as a JPG for `ffmpeg`:
        ///   owner/   HIS screen: `UltimatePhaseView` draws the cutscene through its own camera into a RawImage
        ///            named "UltimateScene", which `Camera.main` (all `Record` ever rendered) cannot see, so while
        ///            that picture is up its texture is copied; otherwise the live camera is rendered.
        ///   wide/    a fixed high shot of the court: the tree erupting and all three dragged in.
        ///   victim/  over the shoulder of one of the players it catches.
        /// ⚠️ `Time.captureFramerate` = 30 fixes game time per frame, so the film plays at true game speed even
        /// though the editor renders three views at a few frames a second (the old films ran 7 to 17 fps real
        /// time and read as slow motion). Runs only with TUMP_PAETE_FILM=1; frames under TUMP_EVIDENCE or Logs.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmTheUltimateOnHisScreen()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "paete-ult-film");
            foreach (string view in new[] { "owner", "wide", "victim" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var paete = HumanPaete(new Vector3(0, .12f, -11));
            paete.AbilitySystem.Kit.AddUltimateCharge(100);
            var others = new[] { round.PlayerAt(0), round.PlayerAt(2), round.PlayerAt(3) };
            var stands = new[] { new Vector3(5.6f, .12f, -2.2f), new Vector3(-5.2f, .12f, -1.6f), new Vector3(1.8f, .12f, 2.2f) };
            for (int i = 0; i < others.Length; i++)
            {
                others[i].Teleport(stands[i]); others[i].Intent.Parked = false;
                others[i].transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -3) - stands[i]);
            }
            paete.Intent.AimPoint = new Vector3(0, 0, -3);
            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("PaeteUltWide", 52);
            var victimCam = Make("PaeteUltVictim", 62);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Save(Texture source, string view, int index)
            {
                Graphics.Blit(source, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            void Shoot(Camera c, string view, int index)
            {
                RenderFilmView(c, hdr);
                Save(hdr, view, index);
            }
            bool rootedAll = false, sawScene = false, struggled = false, brokeOut = false, wasRooted = false;
            // ⚠️ THE MATCH CLOCK MUST NOT RUN UNDER A CUTSCENE (owner, 2026-09-26 night: *"ALSO match time should pause during
            // cutscenes"*). Read the round clock when the cutscene comes up, on its last frame, and a second after it hands back.
            float clockAtScene = -1f, clockAtSceneEnd = -1f, clockAfter = -1f; int lastSceneFrame = -1;
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            // The cutscene runs on the phase's wall clock; give it the film's frame clock so it lasts its real 5.0 s.
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            // Every world cue with its film time, so the video gets the game's own sound mixed in afterwards.
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            try
            {
                // ⚠️ 18 s (was 15): the 6.5 s cutscene, the hand-back catch, and then the caught player in `victim/` holds Interact
                // for the full break-out and gets out, on camera (owner, 2026-09-27: *"did u also animate already hhow theyre supposed
                // to get out of the tree by holding a button ?"*, *"can u show that in vid too?"*).
                const int frames = 30 * 18;
                var victim = others[1];
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    paete.Intent.Set(Verb.Ultimate, t < .25f);
                    // The filmed player fights free: Interact held from the moment they are held until the roots let go.
                    bool fighting = victim.IsRooted;
                    victim.Intent.Set(Verb.Interact, fighting);
                    struggled |= victim.IsStruggling;
                    if (wasRooted && !victim.IsRooted && !victim.IsTagged) brokeOut = true;
                    wasRooted = victim.IsRooted;
                    yield return null;
                    rootedAll |= others[0].IsRooted && others[1].IsRooted && others[2].IsRooted;
                    RawImage scene = null;
                    foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                        if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) scene = image;
                    if (scene != null)
                    {
                        // His theme plays from the introduction's own AudioSource, not as a world cue: log it once.
                        if (!sawScene) { cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_paete,1,0.2")); clockAtScene = round.TimeLeft; }
                        sawScene = true; Save(scene.texture, "owner", f);
                        clockAtSceneEnd = round.TimeLeft; lastSceneFrame = f;
                    }
                    else if (Camera.main != null) Shoot(Camera.main, "owner", f);
                    // Wide: far and high enough that the whole 9 m tree and all three stands fit.
                    wide.transform.position = new Vector3(11f, 7.5f, 9.5f);
                    wide.transform.LookAt(new Vector3(0, 3.0f, -3));
                    // Victim: from their side at knee-to-chest height, so the drag and the roots on their shins
                    // read (behind the head, the first cut, showed only the back of a head).
                    var toTree = new Vector3(0, 0, -3) - victim.transform.position; toTree.y = 0;
                    toTree = toTree.sqrMagnitude > .01f ? toTree.normalized : Vector3.back;
                    var side = Vector3.Cross(Vector3.up, toTree);
                    victimCam.transform.position = victim.transform.position + side * 3.4f - toTree * 1.2f + Vector3.up * 1.5f;
                    victimCam.transform.LookAt(victim.transform.position + toTree * 0.8f + Vector3.up * 1.0f);
                    Shoot(wide, "wide", f);
                    Shoot(victimCam, "victim", f);
                    if (lastSceneFrame >= 0 && f == lastSceneFrame + 30) clockAfter = round.TimeLeft;
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject); Object.Destroy(victimCam.gameObject);
                hdr.Release(); ldr.Release();
            }
            Debug.Log("[PaeteKitPlayProbe] shot report: " + UltimatePhaseView.LastShotReport);
            File.WriteAllText(Path.Combine(root, "shots.txt"), UltimatePhaseView.LastShotReport);
            string clock = FormattableString.Invariant($"round clock: {clockAtScene:F3} s left as the cutscene came up, {clockAtSceneEnd:F3} on its last frame, {clockAfter:F3} one second after it handed back");
            File.WriteAllText(Path.Combine(root, "clock.txt"), clock + System.Environment.NewLine);
            Debug.Log("[PaeteKitPlayProbe] " + clock);
            Note("ult_film_clock_during_cutscene_moved_s", clockAtScene - clockAtSceneEnd);
            Note("ult_film_saw_cutscene", sawScene);
            Note("ult_film_all_rooted", rootedAll);
            Note("ult_film_victim_struggled", struggled);
            Note("ult_film_victim_broke_out", brokeOut);
            Assert.IsTrue(sawScene, "The cutscene picture never came up on his screen.");
            Assert.IsTrue(rootedAll, "The film never showed all three rooted.");
            Assert.IsTrue(struggled && brokeOut, "The filmed player never struggled free of the roots by holding Interact.");
            Assert.Less(Mathf.Abs(clockAtScene - clockAtSceneEnd), 0.05f, "The match clock ran during the cutscene: " + clock);
            Assert.Greater(clockAtSceneEnd - clockAfter, 0.5f, "The match clock did not run again after the cutscene: " + clock);
        }

        /// <summary>
        /// ⚠️ PAETE'S BOTS, MEASURED (HERO-9 "Bots ... Not yet measured in `BotBehaviourProbe`"). The seeded
        /// whole-match probe picks its cast during the load, so Paete may never sit in it; this seats him in all
        /// four chairs with the real `AIController` pressing the real buttons, for two rounds so the taya turns
        /// over and both role abilities come up, and counts every ability each seat used (a cooldown or charge
        /// spent, the ultimate by its bar emptying, `BotBehaviourProbe.Count`'s rule). The bar is topped up every
        /// 20 s so the sentry's own decision (two bodies in 9 m) is what is tested, not the meter. Game time is
        /// stepped at `BotBehaviourProbe`'s 1/60 s, the only rate its bots are measured at.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator PaeteBotsUseEveryAbility()
        {
            var seats = new CharacterMotor[4];
            for (int i = 0; i < 4; i++)
            {
                seats[i] = Paete(i, new Vector3(-4.5f + 3f * i, .12f, -6f + (i % 2) * 3f));
                seats[i].Intent.Clear();
            }
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = true;
            var used = new System.Collections.Generic.Dictionary<string, int>();
            var lastCooldown = new System.Collections.Generic.Dictionary<HeroAbility, float>();
            var lastCharges = new System.Collections.Generic.Dictionary<HeroAbility, int>();
            var lastBar = new float[4];
            void Bump(string id) { used.TryGetValue(id, out int n); used[id] = n + 1; }
            float previousStep = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            try
            {
                const int frames = 60 * 190;
                for (int f = 0; f < frames; f++)
                {
                    if (f % (60 * 20) == 0) foreach (var who in seats) who.AbilitySystem.Kit.AddUltimateCharge(100);
                    yield return null;
                    for (int s = 0; s < 4; s++)
                    {
                        var kit = seats[s].AbilitySystem.Kit;
                        foreach (var ability in kit.AllAbilities)
                        {
                            if (ability == null || ability == kit.Ultimate) continue;
                            if (ability.UsesCharges)
                            {
                                if (lastCharges.TryGetValue(ability, out int before) && ability.ChargesRemaining < before) Bump(ability.Id);
                                lastCharges[ability] = ability.ChargesRemaining;
                            }
                            else
                            {
                                lastCooldown.TryGetValue(ability, out float before);
                                if (ability.CooldownRemaining > before + .01f) Bump(ability.Id);
                                lastCooldown[ability] = ability.CooldownRemaining;
                            }
                        }
                        if (lastBar[s] > kit.UltimateCost * .5f && kit.UltimateCharge <= .01f) Bump(kit.Ultimate.Id);
                        lastBar[s] = kit.UltimateCharge;
                    }
                }
            }
            finally { Time.captureDeltaTime = previousStep; }
            var kitIds = seats[0].AbilitySystem.Kit.AllAbilities;
            foreach (var ability in kitIds) { used.TryGetValue(ability.Id, out int n); Note("bot_uses_" + ability.Id, n); }
            string tally = string.Join(", ", System.Linq.Enumerable.Select(used, kv => kv.Key + "=" + kv.Value));
            Debug.Log("[PaeteKitPlayProbe] bot uses: " + tally);
            foreach (var ability in kitIds)
                Assert.IsTrue(used.ContainsKey(ability.Id), $"Paete's bots never used {ability.Id} in two rounds ({tally}).");
        }

        /// <summary>The local seat as Paete, human, in his own model (arms and all), for the films.</summary>
        private static CharacterMotor HumanPaete(Vector3 at)
        {
            var paete = Paete(GameLaunch.SoloSeat, at);
            paete.IsBot = false;
            return paete;
        }

        /// <summary>
        /// ⚠️ A FILM CAMERA SEES THE BODIES AS A SPECTATOR WOULD. The local seat's body is `ShadowsOnly` for its own
        /// first-person view (`CameraRig.ApplyFppSelfHide`), so the court camera drew Paete's vines coming out of nobody.
        /// Every camera but his own eyes shows every body, and never the private first-person arms.
        /// </summary>
        internal static void RenderFilmView(Camera c, RenderTexture target)
        {
            bool witness = c != Camera.main;
            var bodies = new System.Collections.Generic.List<Renderer>();
            var arms = new System.Collections.Generic.List<Renderer>();
            if (witness)
            {
                foreach (var p in GameServices.Round.Players)
                    foreach (var r in p.GetComponentsInChildren<Renderer>())
                        if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                        { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; bodies.Add(r); }
                foreach (var a in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                    foreach (var r in a.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; arms.Add(r); }
            }
            try
            {
                var before = c.targetTexture; c.targetTexture = target; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before;
            }
            finally
            {
                foreach (var r in bodies) if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                foreach (var r in arms) if (r != null) r.enabled = true;
            }
        }

        /// <summary>
        /// ⚠️ A FILM (HERO-9: *"a first-person vine capture"*): LIANA LEAP from his own eyes, the vines leaving
        /// the viewmodel hands, and the same leap from beside him. Runs only with TUMP_PAETE_FILM=1.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator FilmTheVineFromHisOwnEyes()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            var paete = HumanPaete(new Vector3(0, .12f, -9));
            paete.Intent.AimPoint = paete.transform.position + new Vector3(0, 1.2f, 8f);
            var camera = new GameObject("PaeteVineFilm").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 55; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            yield return ImprovementEvidenceProbe.Record(camera, "paete-vine-film", 3.2f, paete,
                drive: t => paete.Intent.Set(Verb.Skill1, t > .5f && t < .7f),
                witnessOffset: new Vector3(4.2f, 1.6f, 1.2f), witnessLookHeight: 1.1f);
            Object.Destroy(camera.gameObject);
            Note("film_vine", true);
        }

        /// <summary>
        /// ⚠️ A FILM OF HIS OTHER THREE SKILLS IN ONE MATCH (owner, 2026-09-27: *"ok can u show me hhow his other skills look like? and
        /// them working ty"*). Real input, game time fixed at 30 fps, every world cue logged for the mp4:
        ///   0.5   LIANA LEAP: the vines out to 8 m ahead, reeling him in.
        ///   2.6   BAKYA BLOOM: the pitcher planted between him and the can; it grows its wooden clog (3 s); the second press fires it
        ///         at the can.
        ///   9.9   THORN HARVEST: a second Paete, the taya, stamps; the slipper in a player's hand 4.5 m away is caught, held, hauled home.
        /// Views: `owner/` is his own screen when the acting Paete is the local seat, else a camera over the acting Paete's shoulder;
        /// `wide/` is a court camera placed for each skill. Runs only with TUMP_PAETE_FILM=1; frames under TUMP_EVIDENCE or Logs.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator FilmHisSkillsInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_PAETE_FILM") != "1") Assert.Ignore("Film only: set TUMP_PAETE_FILM=1.");
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "paete-skills-film");
            foreach (string view in new[] { "owner", "wide" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var lata = round.Lata;
            Assert.IsNotNull(lata, "No lata on the court.");
            var can = Flat(lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            // The attacking Paete is the local seat when it can be (his own screen), else another attacker seat.
            int attackSeat = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            // ⚠️ Film s1 started him behind the plaza's monument and never turned him to his plant: each skill has its own clear
            // spot now, facing what it acts on (the ultimate film's lane for the vine, a spot facing the plant and the can for the
            // bloom, the taya in the box facing the slipper's holder for the thorns), with a cut between.
            Vector3 start = can + new Vector3(0f, .12f, -12f);
            Vector3 bloomAt = can + new Vector3(-2.5f, .12f, -8.5f);
            var attacker = attackSeat == GameLaunch.SoloSeat ? HumanPaete(start) : Paete(attackSeat, start);
            var defender = Paete(taya, can + new Vector3(-3f, .12f, -1.5f));
            CharacterMotor holder = null;
            foreach (var p in round.Players)
                if (p != attacker && p != defender && p.GetComponent<Carrier>().Held != null) { holder = p; break; }
            if (holder != null) { holder.Teleport(can + new Vector3(1.5f, .12f, -1.5f)); holder.Intent.Parked = true; }
            var local = round.PlayerAt(GameLaunch.SoloSeat);
            Vector3 plantSpot = can + new Vector3(-1.5f, 0f, -4.8f);
            void Face(CharacterMotor who, Vector3 toward)
            {
                var d = toward - who.transform.position; d.y = 0f;
                if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
            }

            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("PaeteSkillsWide", 50);
            var shoulder = Make("PaeteSkillsShoulder", 60);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            void Shoot(Camera c, string view, int index)
            {
                RenderFilmView(c, hdr);
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frame = 0;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            bool planted = false, fired = false, reeled = false;
            float fireAt = -1f;
            Slipper shoe = holder != null ? holder.GetComponent<Carrier>().Held : null;
            try
            {
                const int frames = (int)(30 * 13.5f);
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    // LIANA LEAP.
                    attacker.Intent.AimPoint = start + new Vector3(0f, 1.2f, 8f);
                    if (f == 72) attacker.Teleport(bloomAt);
                    if (t >= 2.4f) { attacker.Intent.AimPoint = plantSpot; Face(attacker, fireAt > 0f && t >= fireAt - .3f ? lata.transform.position : (plantSpot + can) * .5f); }
                    Face(defender, holder != null ? holder.transform.position : can);
                    attacker.Intent.Set(Verb.Skill1, t > .5f && t < .7f);
                    // BAKYA BLOOM: plant, wait for the clog, fire it at the can.
                    var plant = PaetePlant.OwnedBy(attacker.PlayerSlot);
                    planted |= plant != null;
                    if (plant != null && plant.ShotReady && fireAt < 0f && t > 6.0f) fireAt = t + .3f;
                    if (fireAt > 0f && t >= fireAt - .2f) attacker.Intent.AimPoint = lata.transform.position;
                    bool pressPlant = t > 2.6f && t < 2.8f, pressFire = fireAt > 0f && t > fireAt && t < fireAt + .2f;
                    attacker.Intent.Set(Verb.Skill2, pressPlant || pressFire);
                    // THORN HARVEST, placed beside the holder (owner, 2026-09-26: *"castable and not cast on body"*), off the can.
                    if (t > 9.6f) defender.Intent.AimPoint = can + new Vector3(0.4f, 0f, -2.8f);
                    defender.Intent.Set(Verb.Skill2, t > 9.9f && t < 10.1f);
                    yield return null;
                    reeled |= Vector3.Distance(Flat(attacker.transform.position), Flat(start)) > 3f;
                    // Film s2 found a building-sized dark shell from the plant's landing on: name every big renderer that is not the map.
                    if (f == 120)
                    {
                        var big = new StringBuilder();
                        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        {
                            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r.bounds.size.magnitude < 6f) continue;
                            var top = r.transform.root.name;
                            if (top == "BayanPlaza" || top.StartsWith("Map")) continue;
                            string path = r.name; for (var q = r.transform.parent; q != null; q = q.parent) path = q.name + "/" + path;
                            big.AppendLine(FormattableString.Invariant($"{path}  size {r.bounds.size}  centre {r.bounds.center}  {r.GetType().Name}"));
                        }
                        File.WriteAllText(Path.Combine(root, "big-renderers.txt"), big.ToString());
                    }
                    fired |= Object.FindFirstObjectByType<PaeteWoodenSlipper>() != null;

                    // The cameras, per skill.
                    CharacterMotor acting = t < 9.6f ? attacker : defender;
                    if (t < 2.4f) { wide.transform.position = start + new Vector3(6.5f, 2.6f, 4.5f); wide.transform.LookAt(start + new Vector3(0f, 1f, 4.5f)); }
                    else if (t < 9.6f) { wide.transform.position = can + new Vector3(5.5f, 4.2f, -11f); wide.transform.LookAt(can + new Vector3(-1.2f, .8f, -4.5f)); }
                    else { wide.transform.position = can + new Vector3(-.75f, 3.6f, -9f); wide.transform.LookAt(can + new Vector3(-.75f, .8f, -1.5f)); }
                    if (acting == local && Camera.main != null) Shoot(Camera.main, "owner", f);
                    else
                    {
                        var fwd = acting.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                        shoulder.transform.position = acting.transform.position - fwd * 3.2f + Vector3.up * 2.1f + Vector3.Cross(Vector3.up, fwd) * .9f;
                        shoulder.transform.LookAt(acting.transform.position + fwd * 4f + Vector3.up * 1f);
                        Shoot(shoulder, "owner", f);
                    }
                    Shoot(wide, "wide", f);
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Time.captureFramerate = previousRate;
                Object.Destroy(wide.gameObject); Object.Destroy(shoulder.gameObject);
                hdr.Release(); ldr.Release();
            }
            bool snatched = shoe != null && holder.GetComponent<Carrier>().Held != shoe;
            Note("skills_film_reeled", reeled); Note("skills_film_planted", planted); Note("skills_film_fired", fired); Note("skills_film_snatched", snatched);
            Assert.IsTrue(reeled, "LIANA LEAP did not carry him.");
            Assert.IsTrue(planted, "BAKYA BLOOM planted nothing.");
            Assert.IsTrue(fired, "BAKYA BLOOM never fired its wooden slipper.");
            Assert.IsTrue(snatched, "THORN HARVEST left the slipper in the holder's hand.");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FilmPlayerVineMeeting()
        {
            if(Environment.GetEnvironmentVariable("TUMP_PAETE_FILM")!="1")Assert.Ignore("Opt-in visual capture.");
            var caster=Paete(1,new Vector3(-5,.12f,-6));
            var target=GameServices.Round.PlayerAt(2);target.Teleport(new Vector3(-5,.12f,0));
            caster.Intent.Parked=target.Intent.Parked=false;
            caster.Intent.AimPoint=target.transform.position+Vector3.up*.9f;
            yield return null;
            string root=Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs","player-vine");Directory.CreateDirectory(root);
            var camera=new GameObject("PaetePlayerVineFilm").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);camera.enabled=false;camera.tag="Untagged";camera.fieldOfView=47;
            camera.cullingMask &= ~(1<<5);camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            camera.transform.position=new Vector3(3,4,-7);camera.transform.LookAt(new Vector3(-5,1,-3));
            var hdr=new RenderTexture(1280,720,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var ldr=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            int previous=Time.captureFramerate;Time.captureFramerate=30;bool caught=false;
            try
            {
                for(int frame=0;frame<100;frame++)
                {
                    caster.Intent.Set(Verb.Skill1,frame>=20&&frame<25);
                    yield return null;
                    caught|=Object.FindFirstObjectByType<PaetePlayerPull>()!=null;
                    RenderFilmView(camera,hdr);Graphics.Blit(hdr,ldr);
                    var active=RenderTexture.active;RenderTexture.active=ldr;
                    pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();RenderTexture.active=active;
                    File.WriteAllBytes(Path.Combine(root,$"{frame:D5}.jpg"),pixels.EncodeToJPG(92));
                }
                Assert.IsTrue(caught);Assert.Less(Flat(target.transform.position-caster.transform.position).magnitude,1.1f);
            }
            finally
            {
                Time.captureFramerate=previous;Object.Destroy(camera.gameObject);Object.Destroy(pixels);
                hdr.Release();ldr.Release();Object.Destroy(hdr);Object.Destroy(ldr);
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    }
}
