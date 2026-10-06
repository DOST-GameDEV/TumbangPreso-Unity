using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerPoseFeedbackTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previous;
        private GameObject _body, _router;
        private CharacterMotor _motor;
        private MatchRpc _rpc;

        [UnitySetUp] public IEnumerator Before()
        {
            _previous = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = new Client(); GameServices.Ensure();
            GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            _body = new GameObject("Predicted owner body", typeof(CharacterController));
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.RoundActive = true; GameServices.Round.Register(_motor);
            _router = new GameObject("Dormant actual pose receiver"); _router.SetActive(false);
            _rpc = _router.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(_rpc, 123L);
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body);
            if (_router != null) Object.Destroy(_router);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous;
        }

        private void Deliver(Vector3 oldPosition, bool correction, int epoch = 0, ulong serial = 1)
        {
            using var writer = new FastBufferWriter(320, Allocator.Temp);
            writer.WriteValueSafe(1);
            writer.WriteNetworkSerializable(new GameplayActionScope { Match = 123, Round = 1, Epoch = epoch });
            writer.WriteValueSafe(serial);
            // Protocol150 distinguishes an accepted echo from a host correction.
            // The baseline149 payload has exactly its original schema.
            if (NetSession.ProtocolVersion >= 150) writer.WriteValueSafe(correction);
            writer.WriteValueSafe(oldPosition); writer.WriteValueSafe(0f); writer.WriteValueSafe(Vector3.zero);
            writer.WriteValueSafe(true);
            writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe((int)StunElement.None);
            writer.WriteValueSafe(1); writer.WriteValueSafe(0);
            writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0); writer.WriteValueSafe(0f);
            writer.WriteValueSafe(42f); writer.WriteValueSafe(1f); writer.WriteValueSafe(0f);
            writer.WriteValueSafe(0); writer.WriteValueSafe(0);
            writer.WriteValueSafe((byte)0); writer.WriteValueSafe(Vector3.zero); writer.WriteValueSafe(Vector3.forward);
            writer.WriteValueSafe((byte)0); writer.WriteValueSafe(0f);
            writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0f);
            writer.WriteValueSafe((byte)0); writer.WriteValueSafe((byte)0);
            writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(0f);
            writer.WriteValueSafe(Vector3.zero); writer.WriteValueSafe(0L);
            writer.WriteValueSafe(0f); writer.WriteValueSafe(0f);
            writer.WriteNetworkSerializable(new VoodooBodySnapshot { MarkSource = -1, ReachTarget = -1 });
            writer.WriteNetworkSerializable(default(AbilityAimSnapshot));
            Assert.AreEqual(216 + VoodooBodySnapshot.WireBytes + (NetSession.ProtocolVersion >= 150 ? 1 : 0), writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnSyncUnitMsg", Hidden).Invoke(_rpc, new object[] { NetworkManager.ServerClientId, reader });
            Assert.AreEqual(42f, _motor.Stamina.Current, "The actual packet must be accepted, not rejected by the fixture.");
            Assert.AreEqual(serial, (ulong)typeof(CharacterMotor).GetField("_lastNetworkPoseSerial", Hidden).GetValue(_motor),
                "Every delayed packet must advance the actual receiver serial.");
        }

        private IEnumerator AcceptedEcho(float travelled)
        {
            _motor.transform.position = Vector3.right * travelled;
            typeof(CharacterMotor).GetField("_velocity", Hidden).SetValue(_motor, Vector3.right * 8);
            Deliver(Vector3.zero, correction: false);
            Assert.AreEqual(Vector3.right * travelled, _motor.transform.position,
                "An accepted earlier owner pose pulled valid prediction backward after its round trip.");
            Assert.AreEqual(Vector3.right * 8, _motor.Velocity, "An accepted echo overwrote the owner's current velocity.");
            yield return null;
        }
        [UnityTest] public IEnumerator AcceptedEchoAtAVisibleRoundTripDistanceDoesNotPullTheOwnerBack() => AcceptedEcho(1.6f);
        [UnityTest] public IEnumerator AcceptedEchoBeyondTheOldSnapDistanceDoesNotPullTheOwnerBack() => AcceptedEcho(4.2f);

        [UnityTest] public IEnumerator AnExplicitHostCorrectionStillCorrectsTheOwner()
        {
            _motor.transform.position = Vector3.right * 4.2f;
            Deliver(Vector3.zero, correction: true);
            Assert.AreEqual(Vector3.zero, _motor.transform.position);
            yield return null;
        }
        [UnityTest] public IEnumerator ANewMovementEpochStillAppliesTheAuthoritativeTeleport()
        {
            _motor.transform.position = Vector3.right * .3f;
            Deliver(Vector3.zero, correction: false, epoch: 1);
            Assert.AreEqual(Vector3.zero, _motor.transform.position);
            Assert.AreEqual(1, _motor.MovementEpoch);
            yield return null;
        }
        [UnityTest] public IEnumerator AcceptedOtherSeatPosesStillMoveTheObservedReplica()
        {
            _motor.PlayerSlot = 2; GameServices.Round.Register(_motor);
            // The packet subject remains1; move local authority away from that body.
            _motor.PlayerSlot = 1; NetAuthority.Provider = new Observer();
            _motor.transform.position = Vector3.right * 2;
            Deliver(Vector3.zero, correction: false);
            Assert.AreEqual(Vector3.zero, _motor.transform.position);
            yield return null;
        }
        [UnityTest] public IEnumerator DelayedEchoesPreserveContinuousPredictedRunning()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.down * .5f;
            floor.transform.localScale = new Vector3(60, 1, 60);
            try
            {
                var cc = _body.GetComponent<CharacterController>();
                cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
                _motor.transform.position = new Vector3(0, .1f, -5);
                typeof(CharacterMotor).GetField("_spawnSettle", Hidden).SetValue(_motor, 0);
                _motor.Intent.Parked = false;
                _motor.Intent.Move = Vector2.up;
                _motor.Intent.Set(Verb.Sprint, true);
                Physics.SyncTransforms();
                var step = typeof(CharacterMotor).GetMethod("FixedUpdate", Hidden);
                var poses = new System.Collections.Generic.List<Vector3>();
                float maximumRollback = 0f, totalDistance = 0f;
                int accepted = 0;
                for (int tick = 0; tick < 100; tick++)
                {
                    Vector3 prior = _motor.transform.position;
                    step.Invoke(_motor, null);
                    Vector3 predicted = _motor.transform.position;
                    totalDistance += Vector3.Distance(prior, predicted);
                    poses.Add(predicted);
                    if (tick >= 25)
                    {
                        // Half-second round trip, modeled at the actual receiver.
                        // No socket, host validation or physical device claim.
                        Deliver(poses[tick - 25], false, serial: (ulong)(tick + 1));
                        accepted++;
                        maximumRollback = Mathf.Max(maximumRollback,
                            Vector3.Distance(predicted, _motor.transform.position));
                    }
                }
                TestContext.WriteLine($"physics_steps=100 dt={Time.fixedDeltaTime:F3} delayed_steps=25 accepted={accepted} travel={totalDistance:F4} maximum_rollback={maximumRollback:F4}");
                Assert.That(totalDistance, Is.GreaterThan(6f), "Fixture must really run through the motor and controller.");
                Assert.That(accepted, Is.EqualTo(75));
                Assert.That(maximumRollback, Is.LessThan(.001f), "A delayed accepted echo rewound continuous movement.");
            }
            finally { Object.Destroy(floor); }
            yield return null;
        }

        private sealed class Observer : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 0; public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
    }
}
