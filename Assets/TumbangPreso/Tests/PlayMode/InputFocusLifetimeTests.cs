using System.Collections;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class InputFocusLifetimeTests
    {
        private GameObject _body;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private bool _touch, _toggle;
        private INetProvider _provider;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _touch = TouchInput.Active; _toggle = SettingsStore.Current.ToggleSprint;
            TouchInput.ReleaseAll(); TouchInput.Active = true; SettingsStore.Current.ToggleSprint = true;
            PresentationClock.RequestScale(1);
            _body = new GameObject("Focus lifetime input owner");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.IsDefender = false; _motor.RoundActive = true;
            _reader = _body.AddComponent<PlayerInputReader>();
            yield return null; Assert.IsTrue(_reader.enabled);
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); yield return null;
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            SettingsStore.Current.ToggleSprint = _toggle; NetAuthority.Provider = _provider;
            yield return PlayModeWorld.Reset();
        }

        private IEnumerator LatchSprint()
        {
            TouchInput.Set(Verb.Sprint, true); yield return null;
            TouchInput.Set(Verb.Sprint, false); yield return null;
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Sprint), "Actual press/release did not latch toggle sprint.");
        }

        [UnityTest] public IEnumerator LosingFocusRetiresTheLatchedSprintWithoutParkingTheOwner()
        {
            yield return LatchSprint();
            _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            yield return null;
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Sprint), "The focus boundary retained the old sprint toggle.");
            Assert.IsFalse(_motor.Intent.Parked);
        }

        [UnityTest] public IEnumerator LosingFocusRetiresBufferedRecoveryInput()
        {
            TouchInput.Set(Verb.Jump, true); yield return null;
            TouchInput.Set(Verb.Jump, false); yield return null;
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.Jump), "Actual touch recovery did not leave an unconsumed edge.");
            _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            Assert.IsFalse(_motor.Intent.JustPressed(Verb.Jump), "A recovery tap survived focus retirement.");
        }

        [UnityTest] public IEnumerator OrdinaryReleaseKeepsTheConfiguredSprintToggle()
        {
            yield return LatchSprint();
            yield return null; Assert.IsTrue(_motor.Intent.Pressed(Verb.Sprint));
            Assert.IsFalse(_motor.Intent.Parked);
        }
    }
}
