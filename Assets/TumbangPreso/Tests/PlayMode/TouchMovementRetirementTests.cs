using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class TouchMovementRetirementTests
    {
        private sealed class LocalSeatProvider : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }

        private GameObject _body;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private INetProvider _provider;
        private bool _touch;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            PresentationClock.RequestScale(1);
            _body = new GameObject("Touch movement retirement owner");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            // Solo debugging can drive a nonzero seat despite SoloProvider.LocalSlot=0.
            _motor.PlayerSlot = 1; _motor.RoundActive = true; _motor.Mode = GameMode.Classic;
            _reader = _body.AddComponent<PlayerInputReader>();
            yield return null; Assert.IsTrue(_reader.enabled);
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); yield return null;
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private IEnumerator HoldTouchMove()
        {
            TouchInput.Move = Vector2.up; yield return null;
            Assert.AreEqual(Vector2.up, _motor.Intent.MoveAxis, "Actual touch movement did not reach the reader.");
        }

        [UnityTest] public IEnumerator FocusLossRetiresTheCachedTouchAxis()
        {
            yield return HoldTouchMove();
            _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            Assert.AreEqual(Vector2.zero, TouchInput.Move, "Focus loss retained the static touch stick value.");
            yield return null;
            Assert.AreEqual(Vector2.zero, _motor.Intent.MoveAxis, "The next reader frame resurrected retired touch movement.");
        }

        [UnityTest] public IEnumerator ReaderReenableCannotReuseTheCachedTouchAxis()
        {
            yield return HoldTouchMove();
            _reader.enabled = false; _reader.enabled = true;
            Assert.AreEqual(Vector2.zero, TouchInput.Move, "Reader disable retained the static touch stick value.");
            yield return null;
            Assert.AreEqual(Vector2.zero, _motor.Intent.MoveAxis, "Reader re-enable restored the old touch movement.");
        }

        [UnityTest] public IEnumerator OrdinarySustainedTouchMovementStaysHeld()
        {
            yield return HoldTouchMove(); yield return null;
            Assert.AreEqual(Vector2.up, TouchInput.Move); Assert.AreEqual(Vector2.up, _motor.Intent.MoveAxis);
        }

        [UnityTest] public IEnumerator ANewTouchAxisAfterFocusRecoveryStillReachesTheReader()
        {
            yield return HoldTouchMove();
            _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            _body.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
            TouchInput.Move = Vector2.right; yield return null;
            Assert.AreEqual(Vector2.right, _motor.Intent.MoveAxis);
        }

        [UnityTest] public IEnumerator RemoteReaderRetirementPreservesTheLocalTouchAxis()
        {
            yield return HoldTouchMove();
            NetAuthority.Provider = new LocalSeatProvider();
            var remote = new GameObject("Remote reader touch ownership control");
            try
            {
                var remoteMotor = remote.AddComponent<CharacterMotor>(); remoteMotor.enabled = false;
                remoteMotor.PlayerSlot = 2; remoteMotor.RoundActive = true;
                var remoteReader = remote.AddComponent<PlayerInputReader>();
                remoteReader.enabled = false;
                Assert.AreEqual(Vector2.up, TouchInput.Move, "Remote producer disable cleared the local touch axis.");
                remote.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
                Assert.AreEqual(Vector2.up, TouchInput.Move, "Remote producer focus callback cleared local touch movement.");
                yield return null;
                Assert.AreEqual(Vector2.up, _motor.Intent.MoveAxis);
            }
            finally { Object.DestroyImmediate(remote); }
        }
    }
}
