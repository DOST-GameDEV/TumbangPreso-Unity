using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeProducerResetLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _botBody, _localBody, _shoeBody, _canBody, _rangeBody, _floor;
        private CharacterMotor _bot, _local;
        private CombatVerbs _verbs;
        private AIController _brain;
        private PracticeRange _range;
        private INetProvider _provider;
        private Lata _previousCan;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots, _sandbox;
        private float Held => (float)typeof(AIController).GetField("_lungeHeld", Hidden).GetValue(_brain);

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = true; GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata; GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], 1, true);
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube); _floor.name = "Practice producer reset floor";
            _floor.transform.position = Vector3.down * .05f; _floor.transform.localScale = new Vector3(24, .1f, 24);
            _canBody = new GameObject("Practice producer reset can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can;
            _botBody = new GameObject("Practice active producer bot"); _bot = _botBody.AddComponent<CharacterMotor>(); _bot.enabled = false;
            _bot.PlayerSlot = 0; _bot.Mode = GameMode.Classic; _bot.IsDefender = true; _bot.RoundActive = true; _bot.IsBot = true;
            var botCarrier = _botBody.AddComponent<Carrier>(); botCarrier.enabled = false;
            _verbs = _botBody.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            _localBody = new GameObject("Practice local target"); _local = _localBody.AddComponent<CharacterMotor>(); _local.enabled = false;
            _local.PlayerSlot = 1; _local.Mode = GameMode.Classic; _local.IsDefender = false; _local.RoundActive = true;
            var localCarrier = _localBody.AddComponent<Carrier>(); localCarrier.enabled = false;
            _localBody.transform.position = Vector3.back * 8;
            _shoeBody = new GameObject("Practice local owned shoe"); var shoe = _shoeBody.AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = shoe.SeatOfOrigin = 1; Assert.IsTrue(shoe.HostForceEquip(_local));
            GameServices.Round.Register(_bot); GameServices.Round.Register(_local); GameServices.Round.BeginRound();
            _brain = _botBody.AddComponent<AIController>(); _brain.SeatDifficulty = Difficulty.Normal;
            _rangeBody = new GameObject("Prepared public producer reset range"); var runner = _rangeBody.AddComponent<SliceRunner>();
            runner.AutoStart = false; runner.enabled = false; runner.Lata = can;
            var seats = new CharacterMotor[Balance.PlayerCount]; seats[0] = _bot; seats[1] = _local;
            var shoes = new Slipper[Balance.PlayerCount]; shoes[1] = shoe;
            runner.Seats = seats; runner.Slippers = shoes; _range = _rangeBody.AddComponent<PracticeRange>();
            // Supply readiness/seat registration as in PracticeResetCombatLifetimeTests.
            // Real producer hold, public ResetRange and shipping world reset run unchanged.
            typeof(PracticeRange).GetProperty("Instance").SetValue(null, _range);
            typeof(PracticeRange).GetProperty("Local").SetValue(_range, _local);
            Field(_range, "_seats", seats); Field(_range, "_slippers", shoes); Field(_range, "_lata", can);
            Field(_range, "_runner", runner); Field(_range, "_ready", true);
            yield return null;
            Assert.IsTrue(_range.CanEdit); Assert.IsTrue(_brain.enabled); Assert.IsTrue(_bot.CanAct());
            Assert.IsFalse(Panel.AnyOpen); Assert.Greater(PresentationClock.RequestedScale, 0);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in new[] { _rangeBody, _botBody, _localBody, _shoeBody, _canBody, _floor }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private static void Field(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
        private void StepHold(float seconds) => typeof(AIController).GetMethod("StepLungeIntent", Hidden).Invoke(_brain, new object[] { _bot.Intent, _local, seconds });
        private void Target()
        {
            _botBody.transform.rotation = Quaternion.identity;
            _localBody.transform.position = _botBody.transform.position + Vector3.forward;
            Physics.SyncTransforms(); Assert.IsTrue(_local.HoldingSlipper);
            // Direct placement does not update the enabled bot's existing belief.
            // Settle that belief through the shipping observation pass, not hold-state seeding.
            typeof(AIController).GetMethod("Observe", Hidden).Invoke(_brain, new object[] { 2f });
            var predicted = (Vector3?)typeof(AIController).GetMethod("AheadOf", Hidden).Invoke(_brain,
                new object[] { _local, AiTuning.LungeHoldTime });
            Assert.IsTrue(predicted.HasValue); Vector3 reach = predicted.Value - _botBody.transform.position; reach.y = 0;
            Assert.LessOrEqual(reach.magnitude, AiTuning.For(Difficulty.Normal).LungeRange);
        }
        private void Hold()
        {
            _botBody.transform.position = new Vector3(4, 0, 4); Target();
            // A real missed punch spends its normal cooldown, making a lunge preferable.
            Assert.IsTrue(_verbs.HostResolvePunch(_botBody.transform.position, Vector3.back));
            Assert.Greater(_verbs.PunchCooldownLeft, 0); StepHold(.2f);
            Assert.AreEqual(.2f, Held, .0001f); Assert.IsTrue(_bot.Intent.Pressed(Verb.Lunge));
        }
        private void Reset()
        {
            Vector3 previous = _botBody.transform.position; Assert.IsTrue(_range.ResetRange());
            Assert.Greater(Vector3.Distance(previous, _botBody.transform.position), 1);
            Assert.IsTrue(_brain.enabled); Assert.IsTrue(_bot.CanAct()); Assert.IsFalse(_bot.Intent.Parked);
        }
        [Test] public void PublicResetRetiresTheEnabledBotsOldLungeHold()
        {
            Hold(); Reset(); Assert.Less(Held, 0, "Public world reset retained the enabled producer's old lunge clock.");
            Assert.IsFalse(_bot.Intent.Pressed(Verb.Lunge));
        }
        [Test] public void APostResetLungeStartsWithAFreshProducerClock()
        {
            Hold(); Reset(); Target(); StepHold(.1f);
            Assert.AreEqual(.1f, Held, .0001f, "The next lunge reused time from before public reset.");
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Lunge));
        }
        [Test] public void SharedHumanHeroKeysSurviveRetirementOfTheBodyProducer()
        {
            _brain.AbilitiesEnabled = false; Hold();
            _bot.Intent.Set(Verb.Skill1, true); _bot.Intent.Set(Verb.Skill2, true); _bot.Intent.Set(Verb.Ultimate, true);
            Reset(); Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill1)); Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill2));
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Ultimate)); Assert.IsFalse(_brain.AbilitiesEnabled);
            Assert.Less(Held, 0); Assert.IsFalse(_bot.Intent.Pressed(Verb.Lunge));
        }
        [Test] public void AnUninterruptedProducerContinuesItsExistingHold()
        {
            Hold(); StepHold(.1f); Assert.AreEqual(.3f, Held, .0001f);
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Lunge)); Assert.IsTrue(_brain.enabled);
        }
        [Test] public void ARefusedResetDoesNotRetireTheLiveProducer()
        {
            Hold(); Field(_range, "_ready", false); Assert.IsFalse(_range.ResetRange());
            Assert.AreEqual(.2f, Held, .0001f); Assert.IsTrue(_bot.Intent.Pressed(Verb.Lunge)); Assert.IsTrue(_brain.enabled);
        }
        [Test] public void PublicResetPreservesHumanKeysOnADisabledAttachedBrain()
        {
            _brain.enabled = false; _bot.IsBot = false;
            Assert.IsFalse(_brain.enabled); Assert.IsTrue(_brain.AbilitiesEnabled);
            _bot.Intent.Clear(); _bot.Intent.CommitFrame();
            _bot.Intent.Set(Verb.Skill1, true); _bot.Intent.Set(Verb.Skill2, true); _bot.Intent.Set(Verb.Ultimate, true);
            Assert.IsTrue(_bot.Intent.JustPressed(Verb.Skill1)); Assert.IsTrue(_bot.Intent.JustPressed(Verb.Skill2));
            Assert.IsTrue(_bot.Intent.JustPressed(Verb.Ultimate)); Assert.IsTrue(_range.ResetRange());
            Assert.IsFalse(_brain.enabled); Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill1));
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill2)); Assert.IsTrue(_bot.Intent.Pressed(Verb.Ultimate));
            Assert.IsTrue(_bot.Intent.JustPressed(Verb.Skill1)); Assert.IsTrue(_bot.Intent.JustPressed(Verb.Skill2));
            Assert.IsTrue(_bot.Intent.JustPressed(Verb.Ultimate));
        }
    }
}
