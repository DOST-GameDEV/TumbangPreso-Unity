using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SlideInterruptionTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, object> _services = new Dictionary<string, object>();
        private GameObject _root;
        private RoundDirector _round;
        private CharacterMotor _actor;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private Slipper _shoe;
        private INetProvider _provider;
        private float _scale;

        [SetUp]
        public void Before()
        {
            Assert.IsFalse(PresentationClock.Held);
            _scale = PresentationClock.RequestedScale; PresentationClock.RequestScale(1);
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _services.Clear();
            foreach (string name in new[] { "Round", "Audio", "Stats" })
            {
                _services[name] = typeof(GameServices).GetProperty(name).GetValue(null);
                typeof(GameServices).GetProperty(name).SetValue(null, null);
            }
            _root = new GameObject("Controlled interrupted slide");
            _round = _root.AddComponent<RoundDirector>();
            typeof(GameServices).GetProperty("Round").SetValue(null, _round);
            var body = new GameObject("Attacker", typeof(CharacterController));
            body.transform.SetParent(_root.transform);
            _actor = body.AddComponent<CharacterMotor>();
            typeof(CharacterMotor).GetMethod("Awake", Hidden).Invoke(_actor, null);
            _actor.PlayerSlot = 1; _actor.IsDefender = false;
            _carrier = body.AddComponent<Carrier>();
            typeof(Carrier).GetMethod("Awake", Hidden).Invoke(_carrier, null);
            _verbs = body.AddComponent<CombatVerbs>();
            typeof(CombatVerbs).GetMethod("Awake", Hidden).Invoke(_verbs, null);
            _round.Register(_actor); _round.BeginRound();
            var item = new GameObject("Owned loose slipper"); item.transform.SetParent(_root.transform);
            _shoe = item.AddComponent<Slipper>(); _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            _shoe.transform.position = Vector3.forward * (Balance.PickupRadius + Balance.SlideDistance * .25f);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void After()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            foreach (var service in _services) typeof(GameServices).GetProperty(service.Key).SetValue(null, service.Value);
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(_scale);
        }

        private void Tick() => typeof(CombatVerbs).GetMethod("Update", Hidden).Invoke(_verbs, null);

        [TestCase("stun", true)]
        [TestCase("fear", true)]
        [TestCase("round-end", true)]
        [TestCase("pause", false)]
        public void OnlyAnUninterruptedSlideMayRetrieveAfterControlReturns(string interruption, bool cancels)
        {
            Assert.IsTrue(_verbs.HostResolveSlide(_actor.transform.position, Vector3.forward),
                "The actual host predicate must accept a legal slide first.");
            Assert.IsTrue(_verbs.SlideActive); Assert.IsNull(_carrier.Held);
            if (interruption == "stun") _actor.ApplyStagger(1);
            else if (interruption == "fear") _actor.ApplyFeared(Vector3.back, 1);
            else if (interruption == "round-end") _round.EndRound();
            else PresentationClock.RequestScale(0);
            Assert.IsFalse(_actor.CanAct());
            Tick();
            bool windowSurvived = _verbs.SlideActive;

            _actor.ClearStun(); _actor.CleanseStatuses();
            if (interruption == "round-end") _round.BeginRound();
            PresentationClock.RequestScale(1);
            _shoe.transform.position = _actor.transform.position + Vector3.forward * .5f;
            Physics.SyncTransforms();
            Assert.IsTrue(_actor.CanAct());
            Assert.IsTrue(_shoe.CanBeGrabbedBy(_actor), "Recovery must leave a genuinely eligible nearby shoe.");
            Assert.IsFalse(_actor.Intent.Pressed(Verb.Lunge), "There is no new slide input.");
            Tick();
            if (cancels)
            {
                Assert.IsNull(_carrier.Held, "A cancelled slide resumed its old retrieval sweep after recovery.");
                Assert.IsFalse(windowSurvived);
            }
            else
            {
                Assert.IsTrue(windowSurvived, "An ordinary pause must preserve the accepted slide.");
                Assert.AreSame(_shoe, _carrier.Held);
            }
        }
    }
}
