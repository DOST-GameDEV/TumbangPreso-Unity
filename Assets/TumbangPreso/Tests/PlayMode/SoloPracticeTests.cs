using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The practice lobby set to NONE: one seat, no bots, and everything else still working.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE FIRST BUILD OF THAT OPTION COULD NOT PICK UP A TSINELAS.
    /// 🧑, 2026-08-26: *"weird bug cant pick up any slipper in none bot mode"*. Removing three
    /// seats from a match touches more than the three seats: ownership, the round's player
    /// registry, the penalty clocks and the scoreboard all index by slot, and every one of them
    /// had to be checked by hand. A test that only asserts "the human spawned" would have passed
    /// on the broken build.
    ///
    /// ⚠️ IT DRIVES THE REAL INSTALLER FROM THE REAL SETTING, not a hand-built seat. The whole
    /// question is what `MatchInstaller` does when `AIController.BotsEnabled` is false, so
    /// anything that skips it is measuring something else.
    /// </summary>
    public class SoloPracticeTests
    {
        private int _savedDifficulty;

        /// <summary>
        /// ⚠️ THE SCENE HALF, BESIDE THE SETTINGS HALF BELOW. `docs/TODO.md` § 126.8 names this
        /// suite among the five that went red and then green across two full runs of nearly the
        /// same code. Its `[TearDown]` already put the DIRECTORS back, which is the finding
        /// `PlayModeWorld.Reset` now generalises to every fixture; this adds the scene, which is
        /// the other thing a `LoadSceneMode.Single` load leaves behind.
        /// </summary>
        [UnitySetUp]
        public IEnumerator SetUpWorld() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator TearDownWorld() => PlayModeWorld.Reset();

        [SetUp]
        public void SetUp()
        {
            _savedDifficulty = Settings.SettingsStore.Current.AiDifficulty;
        }

        /// <summary>
        /// ⚠️⚠️ THE SETTING IS RESTORED, AND FORGETTING TO WOULD POISON EVERY LATER TEST.
        /// `AIController.BotsEnabled` is static and `SettingsStore.Current` persists to disk, so
        /// a suite that left NONE selected would run every following arena test with one seat in
        /// it and report the resulting mess as unrelated bugs.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Settings.SettingsStore.Current.AiDifficulty = _savedDifficulty;
            AIController.ApplyDifficulty(_savedDifficulty);

            // ⚠️⚠️ THE MATCH IS ENDED TOO, AND LEAVING IT RUNNING BREAKS THE NEXT TEST RATHER
            // THAN THIS ONE. This test starts a real round through the real runner, and the
            // directors are `DontDestroyOnLoad`: a live round outlives the scene, so the next
            // suite's arena loads under a match that is still ticking and its slipper is
            // teleported home by an intermission it knows nothing about.
            // `LandedHighlightTests` failed exactly that way, twice, and passes alone.
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();
        }

        [UnityTest]
        public IEnumerator SoloPracticeSpawnsOneSeatAndStillLetsItRetrieve()
        {
            Settings.SettingsStore.Current.AiDifficulty = AIController.NoBotsIndex;
            AIController.ApplyDifficulty(AIController.NoBotsIndex);

            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;
            GameLaunch.GuidedTutorial = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 40; i++) yield return null;

            var seats = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None);
            int people = 0;
            foreach (var m in seats) if (m.IsPerson) people++;

            Assert.AreEqual(1, people, $"NONE spawned {people} people rather than one");

            CharacterMotor me = null;
            foreach (var m in seats) if (m.IsPerson) { me = m; break; }

            Assert.IsNotNull(me);

            // The round has to be live or `CanAct` refuses everything, which would make this
            // test pass or fail for a reason that has nothing to do with the seat count.
            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "no SliceRunner in the arena");
            if (!runner.Running) runner.Begin();

            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();

            Assert.True(GameServices.Round.RoundActive, "the round never started");
            Assert.False(me.IsDefender, "the one seat spawned as the taya, which has no ammunition");

            // ---- can this seat retrieve a tsinelas off the road? ----
            var carrier = me.GetComponent<Carrier>();
            Assert.IsNotNull(carrier);

            var slippers = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
            Assert.Greater(slippers.Length, 0, "the arena built no tsinelas at all");

            // ⚠️⚠️ THE HAND IS EMPTIED, AND THIS LINE SAID IT WAS FOR MONTHS WITHOUT DOING IT.
            // The comment here read *"Start empty-handed whatever the equip did, so the pickup is
            // what is measured"* and the code below it only skipped HELD slippers when choosing a
            // target: nothing ever emptied the seat. `SliceRunner.EquipOwnedSlippers` puts this
            // attacker's own tsinelas in their hand at the whistle, and
            // `Slipper.IsGrabbableIgnoringReach` refuses everything to a body that is already
            // holding one, so `CanBeGrabbedBy` answered false and this test failed on a pickup
            // that works perfectly in the player.
            //
            // ⚠️ IT WAS FAILING BEFORE § THE OWNERSHIP LOCK AND IS NOT CAUSED BY IT, which was
            // established by running this fixture alone on the commit before that change: same
            // assertion, same message, `owner=-1`, which is a value the lock's clause cannot
            // refuse. Recorded because a red test next to a new rule reads like the new rule's
            // fault, and this one is not.
            foreach (var s in slippers)
                if (s.Holder == me) s.HostDisarm();

            yield return new WaitForFixedUpdate();

            Slipper target = null;
            foreach (var s in slippers)
            {
                if (s.Holder != null || !s.gameObject.activeInHierarchy) continue;
                target = s;
                break;
            }

            Assert.IsNotNull(target,
                $"every one of the {slippers.Length} tsinelas is held, so there is none to fetch");

            // ⚠️⚠️ THIS FIXTURE IS THE GUARD ON § THE OWNERSHIP LOCK'S ONE CARVE-OUT. With the
            // practice lobby set to NONE, three of the four tsinelas belong to seats that were
            // never built and carry `OwnerSlot = -1`, and `SliceRunner.EquipOwnedSlippers` leaves
            // them in the street deliberately: *"a seat that does not exist has simply left its
            // slippers in the street, which is what an empty practice lobby is."* The lock's test
            // is therefore `owner >= 0 && owner != who` and never `owner != who`. **If this goes
            // red with `owner=-1` after somebody tightens that clause, the clause is the bug.**

            // ⚠️ PUT IT AT HIS FEET RATHER THAN WALKING HIM TO IT. The question is whether the
            // grab CONNECTS, not whether the AI-free seat can pathfind, and `CanBeGrabbedBy`
            // measures from the motor's own transform.
            target.transform.position = me.transform.position;
            yield return new WaitForFixedUpdate();

            Assert.True(target.CanBeGrabbedBy(me),
                $"a loose tsinelas at this seat's own feet reports ungrabbable. " +
                $"state={target.State} defender={me.IsDefender} canAct={me.CanAct()} " +
                $"owner={target.OwnerSlot} roundActive={GameServices.Round.RoundActive}");

            var reader = me.GetComponent<PlayerInputReader>();
            if (reader != null) reader.enabled = false;
            me.Intent.Clear();
            me.Intent.CommitFrame();

            me.Intent.Set(Verb.Grab, true);
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return new WaitForFixedUpdate();
            me.Intent.Set(Verb.Grab, false);

            Assert.IsNotNull(carrier.Held,
                "the pickup key did not put a tsinelas in hand in a NONE-bots match, even with " +
                "one at the seat's own feet and CanBeGrabbedBy returning true.");
        }
    }
}
