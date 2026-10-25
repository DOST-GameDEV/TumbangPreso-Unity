using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorPacingTests
    {
        private GameObject _root;
        private INetProvider _previous;
        private SpectatorDirector _director;
        private CharacterMotor _actor;
        private HeroAbilitySystem _abilities;
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => 1;
          public int LocalPeerId => 1; public bool IsSeatlessReferee => false; }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); _previous = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            GameServices.Ensure(); GameServices.Round.Clear();
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.BeginRound();
            _root = new GameObject("Spectator timing fixture");
            var body = new GameObject("Quiet subject", typeof(CharacterController)); body.transform.SetParent(_root.transform);
            _actor = body.AddComponent<CharacterMotor>(); _actor.enabled = false; _actor.PlayerSlot = 1;
            _actor.Mode = GameMode.HeroStrike; _actor.RoundActive = true; GameServices.Round.Register(_actor);
            _abilities = body.AddComponent<HeroAbilitySystem>(); _abilities.enabled = false; _abilities.BindHero("dante");
            var lens = new GameObject("Automatic spectator"); lens.transform.SetParent(_root.transform);
            var camera = lens.AddComponent<SpectatorCamera>(); camera.enabled = false;
            _director = lens.GetComponent<SpectatorDirector>() ?? lens.AddComponent<SpectatorDirector>(); _director.Engaged = true;
            yield return null;
            Assert.AreEqual(SpectatorBeat.Quiet, _director.Beat);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_root != null) Object.Destroy(_root);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous;
        }
        [UnityTest] public IEnumerator AShortContactPulseDoesNotCutOnTheVeryNextFrame()
        {
            // The interest model's own quiet commitment has elapsed. The director
            // still needs a continuous readable shot across the next event pulse.
            yield return new WaitForSecondsRealtime(2.55f);
            var interest = typeof(SpectatorDirector).GetField("_interest", Hidden).GetValue(_director);
            typeof(SpectatorInterestModel).GetMethod("OnScored", Hidden)
                .Invoke(interest, new object[] { 1, ScoreEvent.Tag });
            yield return null;
            Assert.AreEqual(SpectatorBeat.Quiet, _director.Beat, "A single event pulse immediately cut the established view.");
            float until = Time.unscaledTime + 2.7f;
            while (_director.Beat != SpectatorBeat.Tag && Time.unscaledTime < until) yield return null;
            Assert.AreEqual(SpectatorBeat.Tag, _director.Beat, "The settling hold swallowed the important event altogether.");
        }
    }
}
