using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class EmoteTargetOwnershipTests
    {
        private sealed class Provider : INetProvider
        {
            public bool Networked;
            public int Slot;
            public bool IsHost => !Networked;
            public bool IsNetworked => Networked;
            public int LocalSlot => Slot;
            public int LocalPeerId => Slot;
            public bool IsSeatlessReferee => Slot < 0;
        }
        private INetProvider _previous;
        private bool _tutorial;
        private GameObject _root;
        private readonly CharacterMotor[] _actors = new CharacterMotor[4];
        private readonly PlayerInputReader[] _readers = new PlayerInputReader[4];
        private Provider _provider;
        private static readonly MethodInfo Select = typeof(MatchInstaller).GetMethod(
            "Driven", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp] public void Before()
        {
            Assert.IsEmpty(Object.FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude),
                "Use an empty isolated EditMode scene; unrelated actors must not decide this result.");
            _previous = NetAuthority.Provider;
            _tutorial = UI.GameLaunch.GuidedTutorial;
            NetAuthority.Provider = _provider = new Provider();
            UI.GameLaunch.GuidedTutorial = false;
            _root = new GameObject("Emote ownership seats");
            for (int slot = 0; slot < _actors.Length; slot++)
            {
                var body = new GameObject("Emote seat " + slot);
                body.transform.SetParent(_root.transform);
                _actors[slot] = body.AddComponent<CharacterMotor>();
                _actors[slot].PlayerSlot = slot;
                _actors[slot].enabled = false;
                _readers[slot] = body.AddComponent<PlayerInputReader>();
                _readers[slot].enabled = false;
            }
        }
        [TearDown] public void After()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            UI.GameLaunch.GuidedTutorial = _tutorial;
            NetAuthority.Provider = _previous;
        }
        private CharacterMotor Target(CharacterMotor fallback)
            => (CharacterMotor)Select.Invoke(null, new object[] { fallback });
        private void Drive(int slot)
        {
            _provider.Slot = slot;
            for (int i = 0; i < _readers.Length; i++) _readers[i].enabled = i == slot;
        }

        [TestCase(false)] [TestCase(true)]
        public void AllHumanSeatsFollowTheActualReaderWithoutRelyingOnDiscoveryOrder(bool networked)
        {
            _provider.Networked = networked;
            // The same four actors remain alive throughout; no lookup order is imposed.
            for (int slot = 0; slot < _actors.Length; slot++)
            {
                Drive(slot);
                Assert.AreSame(_actors[slot], Target(_actors[0]), "Emote ownership disagrees at seat " + slot);
            }
        }
        [Test] public void NoActiveReaderCannotEmoteOnTheRetiredFallback()
        {
            _provider.Slot = -1;
            Assert.IsNull(Target(_actors[0]));
        }
        [Test] public void ParkedInputOwnerCannotReceiveAWheelChoice()
        {
            Drive(0);
            _actors[0].Intent.Parked = true;
            Assert.IsNull(Target(_actors[0]));
        }
        [Test] public void EnabledReplacementReaderSurvivesAnOlderDisabledReader()
        {
            for (int slot = 1; slot < _actors.Length; slot++) _actors[slot].gameObject.AddComponent<AIController>();
            var replacement = _actors[0].gameObject.AddComponent<PlayerInputReader>();
            replacement.enabled = true;
            Assert.IsFalse(_readers[0].enabled);
            Assert.AreSame(_actors[0], Target(_actors[0]));
        }
        [Test] public void TemporaryAiWithHumanHeroInputKeepsTheSameFallbackOwner()
        {
            foreach (var actor in _actors) actor.gameObject.AddComponent<AIController>();
            Drive(0);
            _actors[0].GetComponent<AIController>().AbilitiesEnabled = false;
            Assert.AreSame(_actors[0], Target(_actors[0]));
        }
        [Test] public void GuidedTutorialStillTargetsItsStudentFallback()
        {
            UI.GameLaunch.GuidedTutorial = true;
            Drive(0);
            Assert.AreSame(_actors[2], Target(_actors[2]));
        }
    }
}
