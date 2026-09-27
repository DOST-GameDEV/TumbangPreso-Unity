using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SkillReceiptTests
    {
        private sealed class AimProbeAbility : HeroAbility
        {
            public int Activations, PrivatePresents, BodyPresents, BodyEnds;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public AimProbeAbility(string id) : base(id, id, "", 1) => AimByHolding(1, 3);
            protected override void OnActivate(AbilityContext context) => Activations++;
            public override void PresentAim(CharacterMotor caster, Vector3 at, float held) => PrivatePresents++;
            public override void PresentAimBody(CharacterMotor caster, float held) => BodyPresents++;
            public override void EndAimBody() => BodyEnds++;
        }
        private sealed class AimProbeKit : HeroKit
        {
            public AimProbeKit() : base("aim-probe", "aim-probe")
            { Skill1 = new AimProbeAbility("aim-one"); Skill2 = new AimProbeAbility("aim-two"); }
        }

        private INetProvider _provider;
        private sealed class PredictingOwner : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private sealed class ObservingHost : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        [UnitySetUp] public IEnumerator Before()
        { _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider; }

        private static MatchRpc ClockReceiver()
        {
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 3, true);
            var root = new GameObject("Match clock receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 321L);
            return router;
        }

        private static void DeliverClock(MatchRpc router, MatchClockMessage message, ulong sender = 0)
        {
            using var writer = new FastBufferWriter(MatchClockMessage.WireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(message);
            Assert.AreEqual(MatchClockMessage.WireBytes, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnSyncTimeMsg", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(router, new object[] { sender, reader });
        }

        [Test]
        public void MatchClockReceiverRejectsStaleScopesSequencesAndTruncatedValues()
        {
            NetAuthority.Provider = new PredictingOwner();
            var router = ClockReceiver(); float original = PresentationClock.RequestedScale;
            var message = new MatchClockMessage { Match = 321, Round = 3, Sequence = 5, Scale = 0 };
            try
            {
                PresentationClock.RequestScale(1);
                DeliverClock(router, message, 9);
                var bad = message; bad.Match--; DeliverClock(router, bad);
                bad = message; bad.Round--; DeliverClock(router, bad);
                bad = message; bad.Scale = float.NaN; DeliverClock(router, bad);
                bad = message; bad.Sequence = 0; DeliverClock(router, bad);
                using (var writer = new FastBufferWriter(4, Allocator.Temp))
                {
                    writer.WriteValueSafe(0f);
                    using var reader = new FastBufferReader(writer, Allocator.Temp);
                    typeof(MatchRpc).GetMethod("OnSyncTimeMsg", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic).Invoke(router, new object[] { 0UL, reader });
                }
                Assert.AreEqual(1, PresentationClock.RequestedScale);
                DeliverClock(router, message); Assert.AreEqual(0, PresentationClock.RequestedScale);
                message.Sequence = 4; message.Scale = 1; DeliverClock(router, message);
                Assert.AreEqual(0, PresentationClock.RequestedScale);
                message.Sequence = 6; message.Scale = .5f; DeliverClock(router, message);
                Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                message.Scale = 0; DeliverClock(router, message);
                Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 322L);
                message.Match = 322; message.Sequence = 1; message.Scale = 1; DeliverClock(router, message);
                Assert.AreEqual(1, PresentationClock.RequestedScale);
            }
            finally { Hitstop.End(); PresentationClock.RequestScale(original); Object.DestroyImmediate(router.gameObject); }
        }

        [Test]
        public void MatchClockCaptureAndRefreshExcludeLocalHitstopAndPreserveAnActiveHold()
        {
            NetAuthority.Provider = new ObservingHost();
            var router = ClockReceiver(); float original = PresentationClock.RequestedScale;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.NonPublic;
            var hold = typeof(PresentationClock).GetMethod("Hold", hidden | System.Reflection.BindingFlags.Static);
            var release = typeof(PresentationClock).GetMethod("Release", hidden | System.Reflection.BindingFlags.Static);
            try
            {
                Settings.SettingsStore.Current.ReducedEffects = false;
                PresentationClock.RequestScale(1); Hitstop.Trigger(.08f, .05f);
                Assert.AreEqual(.05f, Time.timeScale);
                var message = (MatchClockMessage)typeof(MatchRpc).GetMethod("CaptureMatchClock",
                    hidden | System.Reflection.BindingFlags.Instance).Invoke(router, null);
                Assert.AreEqual(1, message.Scale);
                NetAuthority.Provider = new PredictingOwner(); DeliverClock(router, message);
                Assert.IsTrue(Hitstop.Active); Assert.AreEqual(.05f, Time.timeScale);
                Hitstop.End(); hold.Invoke(null, null);
                message.Sequence++; message.Scale = .5f; DeliverClock(router, message);
                Assert.AreEqual(0, Time.timeScale); Assert.AreEqual(.5f, PresentationClock.RequestedScale);
                release.Invoke(null, null); Assert.AreEqual(.5f, Time.timeScale);
            }
            finally
            {
                release.Invoke(null, null); Hitstop.End(); PresentationClock.RequestScale(original);
                Settings.SettingsStore.Current.ReducedEffects = reduced; Object.DestroyImmediate(router.gameObject);
            }
        }

        [Test]
        public void ClockRequestsKeepIndependentPeerSequencesWithinTheirWorldScope()
        {
            var router = ClockReceiver();
            var accept = typeof(MatchRpc).GetMethod("AcceptClockRequest", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var message = new MatchClockMessage { Match = 321, Round = 3, Sequence = 9, Scale = 0 };
            bool Accept(ulong peer, MatchClockMessage value) => (bool)accept.Invoke(router, new object[] { peer, value });
            try
            {
                Assert.IsTrue(Accept(7, message)); Assert.IsFalse(Accept(7, message)); Assert.IsTrue(Accept(8, message));
                message.Sequence--; Assert.IsFalse(Accept(7, message));
                message.Match--; Assert.IsFalse(Accept(8, message)); message.Match++;
                message.Round--; Assert.IsFalse(Accept(8, message));
                GameServices.Match.ApplySnapshot(new int[4], 4, true);
                message.Round = 4; message.Sequence = 1; Assert.IsTrue(Accept(7, message));
            }
            finally { Object.DestroyImmediate(router.gameObject); }
        }

        [Test]
        public void SentryTargetMaskUsesTheWorldFieldReceiverAndRejectsInvalidSeats()
        {
            NetAuthority.Provider = new PredictingOwner();
            var field = new WorldEffectSnapshot.Field { Type = WorldEffectSnapshot.Kind.Sentry,
                Position = Vector3.zero, Forward = Vector3.forward, Owner = 0,
                Duration = PaeteRules.SentryLifeSeconds + .6f, Remaining = 5,
                Radius = PaeteRules.SentryRadius, TargetMask = 10 };
            Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            var bad = field; bad.TargetMask = 16; Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            bad = field; bad.TargetMask = 1; Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            bad = field; bad.Type = WorldEffectSnapshot.Kind.Plant; Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            var root = new GameObject("Sentry field receiver"); root.SetActive(false);
            var receiver = root.AddComponent<MatchRpc>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var batchField = typeof(MatchRpc).GetField("_worldFieldBatch", flags);
            var receive = typeof(MatchRpc).GetMethod("OnWorldFieldItemMsg", flags);
            void Deliver(byte mask, bool includeMask)
            {
                using var writer = new FastBufferWriter(70, Allocator.Temp);
                writer.WriteValueSafe(7); writer.WriteValueSafe(0); writer.WriteValueSafe((int)field.Type);
                writer.WriteValueSafe(field.Position); writer.WriteValueSafe(field.Forward);
                writer.WriteValueSafe(field.Duration); writer.WriteValueSafe(field.Remaining);
                writer.WriteValueSafe(field.Radius); writer.WriteValueSafe(field.Owner);
                writer.WriteValueSafe(0f); writer.WriteValueSafe(0f); writer.WriteValueSafe(false);
                writer.WriteValueSafe(0L);
                if (includeMask) writer.WriteValueSafe(mask);
                Assert.AreEqual(includeMask ? 70 : 69, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(receiver, new object[] { NetworkManager.ServerClientId, reader });
            }
            try
            {
                foreach (byte mask in new byte[] { 16, 1 })
                {
                    var rejected = new WorldEffectSnapshot.Batch(7, 1); batchField.SetValue(receiver, rejected);
                    Deliver(mask, true); Assert.IsFalse(rejected.Finish(7, out _));
                }
                var truncated = new WorldEffectSnapshot.Batch(7, 1); batchField.SetValue(receiver, truncated);
                Deliver(10, false); Assert.IsFalse(truncated.Finish(7, out _));
                var accepted = new WorldEffectSnapshot.Batch(7, 1); batchField.SetValue(receiver, accepted);
                Deliver(10, true); Assert.IsTrue(accepted.Finish(7, out var fields));
                Assert.AreEqual(10, fields[0].TargetMask);
                Assert.AreEqual(field.Remaining, fields[0].Remaining);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RestoredSentryKeepsCapturedSeatsAndBindsLateBodiesWithoutAnotherCatch()
        {
            var owner = Owner("paete").GetComponent<CharacterMotor>(); owner.PlayerSlot = 0;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(owner);
            CharacterMotor Body(int slot, Vector3 at)
            {
                var body = new GameObject("Sentry target " + slot).AddComponent<CharacterMotor>();
                body.PlayerSlot = slot; body.enabled = false; body.transform.position = at;
                GameServices.Round.Register(body); return body;
            }
            var first = Body(1, Vector3.right);
            var lateHost = Body(3, Vector3.left);
            NetAuthority.Provider = new ObservingHost();
            var original = PaeteSentry.Spawn(Vector3.zero, Vector3.zero, 0, handBack: true);
            original.enabled = false;
            var field = original.Capture();
            Assert.AreEqual(10, field.TargetMask);
            first.transform.position = Vector3.right * 20;
            var bystander = Body(2, Vector3.forward);
            GameServices.Round.Unregister(lateHost); Object.Destroy(lateHost.gameObject);
            NetAuthority.Provider = new PredictingOwner();
            Assert.IsTrue(WorldEffectSnapshot.Apply(new[] { field }, 1f));
            var restored = Object.FindFirstObjectByType<PaeteSentry>(); Assert.IsNotNull(restored);
            restored.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var held = (System.Collections.Generic.List<CharacterMotor>)typeof(PaeteSentry).GetField("_held", flags).GetValue(restored);
            CollectionAssert.AreEqual(new[] { first }, held, "Recovery selected a nearby bystander instead of the host's distant target.");
            Assert.AreEqual(10, restored.Capture().TargetMask, "A not-yet-installed seat was lost from recovery state.");
            var late = Body(3, Vector3.back * 20);
            var update = typeof(PaeteSentry).GetMethod("Update", flags);
            update.Invoke(restored, null); update.Invoke(restored, null);
            CollectionAssert.AreEqual(new[] { first, late }, held, "Late seats must bind once regardless of current distance.");
            var tree = restored.GetComponentInChildren<Visual.PaeteSentryBody>();
            var targets = (System.Collections.IList)typeof(Visual.PaeteSentryBody).GetField("_targets", flags).GetValue(tree);
            Assert.AreEqual(2, targets.Count, "Repeated binding added another authored limb.");
            NetAuthority.Provider = new ObservingHost();
            typeof(PaeteSentry).GetMethod("FixedUpdate", flags).Invoke(restored, null);
            foreach (var body in new[] { first, late, bystander })
            { Assert.IsFalse(body.IsCarried); Assert.IsFalse(body.IsRooted); }
            yield return null;
        }

        [Test]
        public void VoodooBodyPrefixIsBoundedAndLeavesTheExistingAimTailIntact()
        {
            var state = new VoodooBodySnapshot { Drained = 1, Hexed = 2, MarkKind = 2, MarkSource = 0,
                MarkAge = 12, ReachKind = 1, ReachTarget = 2, ReachElapsed = 1 };
            Assert.IsTrue(state.IsValid(1));
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            Assert.AreEqual(VoodooBodySnapshot.WireBytes, writer.Length);
            writer.WriteNetworkSerializable(default(AbilityAimSnapshot));
            using (var reader = new FastBufferReader(writer, Allocator.Temp))
            {
                var input = reader;
                Assert.IsTrue(VoodooBodySnapshot.TryRead(ref input, 1, out var restored));
                Assert.AreEqual(state.MarkAge, restored.MarkAge);
                Assert.AreEqual(state.ReachTarget, restored.ReachTarget);
                Assert.IsTrue(AbilityAimSnapshot.TryRead(ref input, out _));
            }
            using var shortWriter = new FastBufferWriter(VoodooBodySnapshot.WireBytes - 1, Allocator.Temp);
            for (int i = 0; i < VoodooBodySnapshot.WireBytes - 1; i++) shortWriter.WriteValueSafe((byte)0);
            using (var reader = new FastBufferReader(shortWriter, Allocator.Temp))
            { var input = reader; Assert.IsFalse(VoodooBodySnapshot.TryRead(ref input, 1, out _)); }
            var bad = state; bad.Drained = float.NaN; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.Hexed = -1; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.MarkKind = 3; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.MarkSource = 1; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.ReachTarget = 99; Assert.IsFalse(bad.IsValid(1));
            bad = state; bad.ReachSucceeded = true; Assert.IsFalse(bad.IsValid(1));
        }

        private static HeroAbilitySystem Owner(string hero)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -.25f, 0);
            floor.transform.localScale = new Vector3(40, .5f, 40);
            var owner = new GameObject("Receipt owner");
            var motor = owner.AddComponent<CharacterMotor>();
            motor.PlayerSlot = 1; motor.enabled = false;
            var system = owner.AddComponent<HeroAbilitySystem>();
            system.enabled = false; system.BindHero(hero);
            NetAuthority.Provider = new PredictingOwner();
            return system;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PlantRemovalNamesItsLifetimeAndCannotResurrectAfterLateInstallation()
        {
            var system = Owner("paete");
            GameServices.Ensure();
            var motor = system.GetComponent<CharacterMotor>();
            long firstId = System.DateTime.UtcNow.Ticks;
            void Plant(long identity)
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                    Vector3.zero, Vector3.forward, new Vector3(0, 0, 9), 0, false, "paete_skill2", false));
                system.Kit.Skill2.AdoptAcceptedCastEvent(identity, false);
            }
            Plant(firstId);
            var first = PaetePlant.OwnedBy(motor.PlayerSlot);
            Assert.IsNotNull(first); Assert.AreEqual(firstId, first.InstanceId);
            system.Kit.Skill2.AdoptAcceptedCastEvent(firstId + 1, true);
            Assert.AreEqual(firstId, first.InstanceId, "A fire command relabeled the plant's birth.");
            Plant(firstId + 2);
            var replacement = PaetePlant.OwnedBy(motor.PlayerSlot);
            Assert.AreNotSame(first, replacement);
            PaetePlant.ApplyPulled(motor.PlayerSlot, 2, firstId);
            Assert.IsFalse(replacement.IsPulled, "An old removal pulled the current replacement.");
            PaetePlant.ApplyPulled(motor.PlayerSlot, 2, firstId + 2);
            Assert.IsTrue(replacement.IsPulled);
            var late = PaetePlant.Restore(new Vector3(0, 0, 9), motor.PlayerSlot, 16, 3, firstId + 2);
            Assert.IsTrue(late.IsRetiring); Assert.IsFalse(late.gameObject.activeSelf,
                "A queued cast/recovery resurrected an already-retired lifetime.");
            var current = PaetePlant.Restore(new Vector3(0, 0, 9), motor.PlayerSlot, 16, 3, firstId + 3);
            var captured = current.Capture();
            Assert.AreEqual(firstId + 3, captured.InstanceId);
            Assert.IsTrue(WorldEffectSnapshot.Apply(new[] { captured }, .1f));
            var restored = PaetePlant.OwnedBy(motor.PlayerSlot);
            Assert.IsNotNull(restored); Assert.AreEqual(firstId + 3, restored.InstanceId);
            PaetePlant.ApplyPulled(motor.PlayerSlot, 2, firstId + 2);
            Assert.IsFalse(restored.IsPulled);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ScopedUltimateImpactReachesOnlyTheVictimsCameraWithoutGameplayMutation()
        {
            var caster = Owner("cheska").GetComponent<CharacterMotor>(); caster.PlayerSlot = 0;
            var victim = new GameObject("Impact victim").AddComponent<CharacterMotor>();
            victim.PlayerSlot = 1; victim.enabled = false;
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.Clear(); GameServices.Round.Register(caster); GameServices.Round.Register(victim);
            var camera = new GameObject("Impact view", typeof(Camera)).AddComponent<CameraSystem.CameraRig>();
            camera.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(CameraSystem.CameraRig).GetField("_character", flags).SetValue(camera, victim);
            var hold = typeof(CameraSystem.CameraRig).GetField("_holdLeft", flags);
            var root = new GameObject("Impact receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            var receive = typeof(MatchRpc).GetMethod("OnFlairMsg", flags);
            void Deliver(long match, int round, int subject)
            {
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe((byte)Visual.MatchFlair.Kind.UltimateImpact);
                writer.WriteValueSafe(0); writer.WriteValueSafe(subject);
                writer.WriteValueSafe(Vector3.forward); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(match); writer.WriteValueSafe(round);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
            }
            float scale = Time.timeScale;
            Deliver(122, 1, 1); Assert.AreEqual(0f, hold.GetValue(camera));
            Deliver(123, 2, 1); Assert.AreEqual(0f, hold.GetValue(camera));
            Deliver(123, 1, 0); Assert.AreEqual(0f, hold.GetValue(camera), "An impact on another body changed this view.");
            Deliver(123, 1, 1); Assert.AreEqual(.11f, (float)hold.GetValue(camera), .001f);
            Assert.AreEqual(scale, Time.timeScale); Assert.IsFalse(victim.IsStunned); Assert.IsFalse(victim.IsRooted);
            Object.Destroy(camera.gameObject); Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator VoodooSyncPreservesHostPoolAndCarriesReachOutcomeWithoutTheTargetSnapshot()
        {
            var system = Owner("phaister");
            var body = system.GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Register(body);
            var root = new GameObject("Voodoo body receiver"); root.SetActive(false);
            var receiver = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(receiver, 123L);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(MatchRpc).GetMethod("OnSyncUnitMsg", flags);
            var state = new VoodooBodySnapshot { Drained = 2, Hexed = 3, MarkKind = 2, MarkSource = 0,
                MarkAge = 12, ReachKind = 1, ReachTarget = 2, ReachElapsed = 1 };
            void Deliver(ulong serial, VoodooBodySnapshot data, long match = 123, int round = 1, int epoch = 0)
            {
                using var writer = new FastBufferWriter(304, Allocator.Temp);
                writer.WriteValueSafe(1);
                writer.WriteNetworkSerializable(new GameplayActionScope { Match = match, Round = round, Epoch = epoch });
                writer.WriteValueSafe(serial);
                writer.WriteValueSafe(Vector3.zero); writer.WriteValueSafe(0f); writer.WriteValueSafe(Vector3.zero);
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
                writer.WriteNetworkSerializable(data); writer.WriteNetworkSerializable(default(AbilityAimSnapshot));
                Assert.AreEqual(208 + VoodooBodySnapshot.WireBytes, writer.Length);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(receiver, new object[] { NetworkManager.ServerClientId, reader });
            }
            Deliver(100, state, match: 122);
            Deliver(100, state, match: 124);
            Deliver(100, state, round: 0);
            Deliver(100, state, round: 2);
            Deliver(100, state, epoch: -1);
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.IsHexed);
            Assert.IsFalse(body.IsVoodooReaching);
            Assert.AreEqual(0, body.MovementEpoch);
            Deliver(1, state);
            Assert.IsTrue(body.IsDrained); Assert.IsTrue(body.IsHexed);
            Assert.IsTrue(body.Stamina.RecoveryBlocked);
            Assert.AreEqual(42, body.Stamina.Current, "A fresh status edge erased the host's resource correction.");
            Assert.AreEqual(VoodooMarkKind.Hex, body.VoodooMark); Assert.AreEqual(0, body.VoodooMarkSource);
            Assert.IsTrue(body.IsVoodooReaching);
            int ended = 0; bool success = false;
            body.VoodooReachEnded += (_, marked) => { ended++; success = marked; };
            Assert.IsNull(GameServices.Round.PlayerAt(2));
            var finish = new VoodooBodySnapshot { MarkSource = -1, ReachTarget = -1, ReachSucceeded = true };
            Deliver(2, finish);
            Assert.AreEqual(1, ended); Assert.IsTrue(success, "Reach outcome depended on another body's later packet.");
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.Stamina.RecoveryBlocked);
            Deliver(1, state);
            Assert.IsFalse(body.IsDrained, "An older body serial restored expired voodoo state.");
            var invalid = state; invalid.Drained = float.NaN;
            Deliver(3, invalid);
            Assert.IsFalse(body.IsDrained); Assert.AreEqual(1, ended);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReceivedVoodooClocksExpireButOnlyTheHostResolvesWaitingCurses()
        {
            Assert.GreaterOrEqual(NetSession.ProtocolVersion, 75);
            var body = Owner("phaister").GetComponent<CharacterMotor>();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            var step = typeof(CharacterMotor).GetMethod("StepStatuses",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            void Tick(float dt) => step.Invoke(body, new object[] { dt });

            body.ApplyNetworkVoodoo(.3f, .4f, 0, -1, 0, 0, -1, 0);
            Assert.IsTrue(body.Stamina.RecoveryBlocked);
            Tick(.15f);
            Assert.AreEqual(.15f, body.StatusLeft(StatusKind.Drained), .001f);
            Assert.AreEqual(.25f, body.StatusLeft(StatusKind.Hexed), .001f);
            Tick(.3f);
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.IsHexed);
            Assert.IsFalse(body.Stamina.RecoveryBlocked, "Received Drained outlived its timer.");

            int ended = 0;
            body.VoodooReachEnded += (_, __) => ended++;
            body.ApplyNetworkVoodoo(0, 0, (byte)VoodooMarkKind.Drain, 0, VoodooRules.DrainDelaySeconds - .05f,
                (byte)VoodooMarkKind.Drain, 0, VoodooRules.ReachSeconds - .05f);
            Tick(.1f);
            Assert.AreEqual(VoodooMarkKind.Drain, body.VoodooMark, "A replica resolved the host's waiting mark.");
            Assert.IsFalse(body.IsDrained);
            Assert.IsTrue(body.IsVoodooReaching); Assert.AreEqual(0, ended);
            Assert.AreEqual(VoodooRules.PassiveSpeedScale(false), body.StatusSpeedScale, .001f);

            body.BodySpeedScale = VoodooRules.DollSpeedScale;
            body.ClearStatuses();
            Assert.AreEqual(VoodooRules.DollSpeedScale, body.BodySpeedScale, "Round cleanup erased body identity.");
            NetAuthority.Provider = new ObservingHost();
            body.ApplyNetworkVoodoo(0, 0, (byte)VoodooMarkKind.Drain, 0, VoodooRules.DrainDelaySeconds - .05f,
                0, -1, 0);
            Tick(.1f);
            Assert.AreEqual(VoodooMarkKind.None, body.VoodooMark);
            Assert.IsTrue(body.IsDrained); Assert.IsTrue(body.Stamina.RecoveryBlocked);
            body.CleanseStatuses();
            Assert.IsFalse(body.IsDrained); Assert.IsFalse(body.Stamina.RecoveryBlocked);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReceivedRootsOwnTheirRestraintWithoutASentryAndAcceptLateFacingOnce()
        {
            var system = Owner("sean");
            var motor = system.GetComponent<CharacterMotor>();
            motor.ApplyNetworkStatuses(0, 0, 7);
            yield return null;
            var coil = motor.GetComponentInChildren<Visual.PaeteRootCoil>();
            Assert.IsNotNull(coil);
            Assert.IsNull(Object.FindFirstObjectByType<PaeteSentry>());
            Assert.IsEmpty(coil.GetComponentsInChildren<Collider>());
            var marks = motor.GetComponent<Visual.StatusBodyMarks>();
            Assert.AreSame(coil, marks.EnsureRootedRestraint(new Vector3(-10, 0, 0)));
            Assert.Greater(Vector3.Dot(motor.transform.forward, Vector3.right), .99f);
            motor.transform.rotation = Quaternion.identity;
            marks.EnsureRootedRestraint(new Vector3(-10, 0, 0));
            Assert.Greater(Vector3.Dot(motor.transform.forward, Vector3.forward), .99f,
                "Repeated sentry updates must not keep turning a player who can aim freely.");
            motor.ApplyNetworkStatuses(0, 0, 9);
            yield return null;
            Assert.AreSame(coil, motor.GetComponentInChildren<Visual.PaeteRootCoil>());
            motor.ApplyNetworkStatuses(0, 0, 0);
            yield return null; yield return null;
            Assert.IsTrue(coil == null); Assert.IsTrue(motor.CanMove());
            motor.ApplyNetworkStatuses(0, 0, 7);
            yield return null;
            coil = motor.GetComponentInChildren<Visual.PaeteRootCoil>();
            Assert.IsNotNull(coil);
            motor.gameObject.SetActive(false);
            yield return null;
            Assert.IsTrue(coil == null, "A disabled body retained its old restraint.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReceivedFrozenStateOwnsOneRestraintWithoutCastingOrBlockingAfterThaw()
        {
            var system = Owner("sean");
            var motor = system.GetComponent<CharacterMotor>(); motor.PlayerSlot = 2;
            void Snapshot(float left) => motor.ApplyNetworkState(left, left, left > 0 ? StunElement.Ice : StunElement.None,
                9, 0, 0, 0, 0, 0, 100, 0, 0);
            Snapshot(1f);
            yield return null;
            var prison = HeroHazards.SpawnIceCubePrison(motor.transform, 1);
            Assert.IsNotNull(prison);
            Assert.AreSame(prison, HeroHazards.SpawnIceCubePrison(motor.transform, 1), "The host cast duplicated the body-owned status picture.");
            Assert.IsEmpty(prison.GetComponentsInChildren<Collider>());
            Snapshot(2f);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(prison != null, "The initial visual timer overruled a received status refresh.");
            Snapshot(0);
            yield return null; yield return null;
            Assert.IsTrue(prison == null);
            Assert.IsTrue(motor.CanMove(), "Thaw presentation mutated movement state.");
            Snapshot(2f);
            yield return null;
            prison = HeroHazards.SpawnIceCubePrison(motor.transform, 2);
            motor.gameObject.SetActive(false);
            yield return null;
            Assert.IsTrue(prison == null, "Disabling a body left its restraint in the world.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator NetworkVerbRefusalDoesNotAddRefundToAnAuthoritativePool()
        {
            var owner = Owner("dante");
            var motor = owner.GetComponent<CharacterMotor>();
            var verbs = owner.gameObject.AddComponent<CombatVerbs>(); verbs.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            motor.Stamina.ApplyNetworkSnapshot(50, 0, 0);
            typeof(CombatVerbs).GetField("_shoveCooldown", flags).SetValue(verbs, 2f);
            verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Shove, refundResources: false);
            Assert.AreEqual(0, verbs.ShoveCooldownLeft);
            Assert.AreEqual(50, motor.Stamina.Current, "Refusal refunded a cost already absent from the authoritative pool.");
            typeof(CombatVerbs).GetField("_slideCooldown", flags).SetValue(verbs, 2f);
            motor.Commit(1f);
            verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Slide, refundResources: false);
            Assert.AreEqual(0, verbs.SlideCooldownLeft);
            Assert.AreEqual(0, motor.CommitLeft);
            Assert.AreEqual(50, motor.Stamina.Current);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteHostResourceClocksRecoverAndDrainWithoutMovingTheReplica()
        {
            var root = new GameObject("Remote resource clock");
            var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.PlayerSlot = 2; motor.RoundActive = true;
            NetAuthority.Provider = new ObservingHost();
            Assert.IsFalse(motor.IsLocallySimulated());
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var tick = typeof(CharacterMotor).GetMethod("FixedUpdate", flags);
            var position = root.transform.position;
            motor.Stamina.Spend(Balance.StaminaMax);
            Assert.IsTrue(motor.Stamina.IsFatigued);
            for (int i = 0; i < 500; i++) tick.Invoke(motor, null);
            Assert.IsFalse(motor.Stamina.IsFatigued);
            Assert.AreEqual(0, motor.Stamina.SpeedZones.Count);
            Assert.AreEqual(Balance.StaminaMax, motor.Stamina.Current, .01f);

            motor.ApplyNetworkResourceIntent(6);
            for (int i = 0; i < 10; i++) tick.Invoke(motor, null);
            Assert.Less(motor.Stamina.Current, Balance.StaminaMax);
            Assert.AreEqual(position, root.transform.position, "Resource authority must not simulate a second body movement.");
            float spent = motor.Stamina.Current;
            motor.AdoptMovementEpoch(motor.MovementEpoch + 1);
            tick.Invoke(motor, null);
            Assert.AreEqual(spent, motor.Stamina.Current, .01f, "Old movement intent survived the new body epoch.");
            motor.ApplyNetworkResourceIntent(6);
            typeof(CharacterMotor).GetField("_netResourceIntentUntil", flags).SetValue(motor, Time.unscaledTime - 1);
            tick.Invoke(motor, null);
            Assert.AreEqual(spent, motor.Stamina.Current, .01f, "A stale pose kept draining stamina indefinitely.");

            NetAuthority.Provider = new PredictingOwner();
            for (int i = 0; i < 100; i++) tick.Invoke(motor, null);
            Assert.AreEqual(spent, motor.Stamina.Current, .01f, "An observing client simulated host-owned resource clocks.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteInteractionHoldsNeedHostTimeAndExpireWithInputOrEpoch()
        {
            var root = new GameObject("Remote interaction hold");
            var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.PlayerSlot = 2; motor.RoundActive = true;
            NetAuthority.Provider = new ObservingHost();
            GameServices.Ensure(); GameServices.Round.Register(motor);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var step = typeof(CharacterMotor).GetMethod("StepStatuses", flags);
            motor.ApplyRooted(30);
            motor.HostBreakFree(); Assert.IsTrue(motor.IsRooted, "A completion request skipped the host hold clock.");
            motor.ApplyNetworkResourceIntent(8);
            for (int i = 0; i < 10; i++) step.Invoke(motor, new object[] { .1f });
            float earned = motor.BreakFreeProgress; Assert.Greater(earned, 0);
            motor.ApplyNetworkResourceIntent(0);
            step.Invoke(motor, new object[] { 1f });
            Assert.AreEqual(earned, motor.BreakFreeProgress, .001f, "Release must preserve root progress without adding time.");
            motor.ApplyNetworkResourceIntent(8);
            typeof(CharacterMotor).GetField("_netResourceIntentUntil", flags).SetValue(motor, Time.unscaledTime - 1);
            step.Invoke(motor, new object[] { 1f });
            Assert.AreEqual(earned, motor.BreakFreeProgress, .001f, "An expired input lease completed a hold.");
            motor.ApplyNetworkResourceIntent(8);
            motor.AdoptMovementEpoch(motor.MovementEpoch + 1);
            Assert.IsFalse(motor.InteractionHeldForSimulation);
            motor.ApplyNetworkResourceIntent(8);
            for (int i = 0; i < 100 && motor.IsRooted; i++) step.Invoke(motor, new object[] { .1f });
            Assert.IsFalse(motor.IsRooted, "The host did not finish a real remote hold without a second completion request.");

            var plant = PaetePlant.Restore(motor.transform.position, 0, PaeteRules.PlantRootedSeconds + 1, 3);
            plant.enabled = false;
            var pull = typeof(PaetePlant).GetMethod("StepPullers", flags);
            Assert.IsFalse(PaetePlant.HostTryUproot(motor), "A nearby client pulled a plant without holding.");
            motor.ApplyNetworkResourceIntent(8);
            for (int i = 0; i < 14; i++) pull.Invoke(plant, new object[] { .1f });
            Assert.IsTrue(PaetePlant.HostTryUproot(motor));
            motor.ApplyNetworkResourceIntent(0);
            Assert.IsFalse(PaetePlant.HostTryUproot(motor), "A released hold remained actionable until the next Update.");
            pull.Invoke(plant, new object[] { .1f });
            motor.ApplyNetworkResourceIntent(8);
            Assert.IsFalse(PaetePlant.HostTryUproot(motor), "A plant pull must restart after release.");
            Object.Destroy(root); Object.Destroy(plant.gameObject);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RemoteAimPresentsOnlyBodyTellsAndRejectsClosedOrOlderHolds()
        {
            var system = Owner("phaister");
            var motor = system.GetComponent<CharacterMotor>();
            motor.PlayerSlot = 2; motor.RoundActive = true;
            var kit = new AimProbeKit();
            typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, kit);
            system.enabled = true;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var update = typeof(HeroAbilitySystem).GetMethod("Update", flags);
            var one = (AimProbeAbility)kit.Skill1; var two = (AimProbeAbility)kit.Skill2;
            var aim = new AbilityAimSnapshot
            { Slot = 1, AbilityId = new FixedString64Bytes(one.Id), Held = .4f, Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 7) };
            system.ApplyNetworkAim(aim); update.Invoke(system, null);
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            Assert.Greater(one.BodyPresents, 0); Assert.Zero(one.PrivatePresents); Assert.Zero(one.Activations);
            system.CloseNetworkAim(0, aim.Token);
            system.ApplyNetworkAim(aim);
            Assert.IsFalse(system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            aim.Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 8);
            system.ApplyNetworkAim(aim);
            system.CloseNetworkAim(0, AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 7));
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill1), "Old release cancelled a new hold.");
            aim.Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 6);
            system.ApplyNetworkAim(aim);
            Assert.AreEqual(AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 8), system.CaptureAimPresentation().Token);

            aim.Slot = 2; aim.AbilityId = new FixedString64Bytes(two.Id);
            aim.Token = AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 9);
            system.ApplyNetworkAim(aim); update.Invoke(system, null);
            system.CloseNetworkAim(0, AbilityAimSnapshot.MakeToken(motor.MovementEpoch, 10));
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill2), "Another slot's cast ended this hold.");
            typeof(HeroAbilitySystem).GetField("_networkAimUntil", flags).SetValue(system, Time.unscaledTime - 1);
            update.Invoke(system, null);
            Assert.IsFalse(system.IsAiming(HeroAbilitySystem.Slot.Skill2));
            Assert.Greater(two.BodyEnds, 0);
            system.ApplyNetworkAim(aim);
            Assert.IsTrue(system.IsAiming(HeroAbilitySystem.Slot.Skill2), "A fresh packet could not renew an expired lease.");
            system.ResetKit();
            Assert.IsFalse(system.IsAiming(HeroAbilitySystem.Slot.Skill2));
            Assert.Zero(one.Activations + two.Activations, "Visual hold replication cast a gameplay ability.");
            system.enabled = false;
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator EarlierConfirmedEffectsSurviveNewerRequestsWithoutDuplicateReplay()
        {
            var system = Owner("cheska");
            var motor = system.GetComponent<CharacterMotor>();
            var skill = system.Kit.Skill1;
            var first = new AbilityContext(motor, null, null, Vector3.zero, Vector3.forward, Vector3.forward * 5);
            var second = new AbilityContext(motor, null, null, Vector3.zero, Vector3.right, Vector3.right * 5);
            skill.Activate(first); Assert.IsTrue(system.TrackSkillRequest(0, 101));
            skill.Activate(second); Assert.IsTrue(system.TrackSkillRequest(0, 102));
            Assert.IsTrue(system.PendingSkillReceipt(0, 101));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 101,
                first.Position, first.Forward, first.AimPoint, .55f));
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 101,
                first.Position, first.Forward, first.AimPoint, .55f));
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            Assert.IsTrue(system.ResolveSkillReceipt(0, 102, false, 4, 0));
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 102,
                second.Position, second.Forward, second.AimPoint, .55f));

            skill.Activate(first); system.TrackSkillRequest(0, 103);
            skill.Activate(second); system.TrackSkillRequest(0, 104);
            float cooldown = skill.CooldownRemaining;
            Assert.IsTrue(system.ResolveSkillReceipt(0, 103, false, 0, 0));
            Assert.AreEqual(cooldown, skill.CooldownRemaining, "An older refusal rewrote newer resources.");
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 104,
                second.Position, second.Forward, second.AimPoint, .55f));
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            skill.Activate(first); system.TrackSkillRequest(0, 105);
            system.ResetKit();
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 105,
                first.Position, first.Forward, first.AimPoint, .55f));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DeferredPlantWaitsForItsObjectAndCommandsCannotReplaceOrCancelIt()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            motor.transform.position = new Vector3(-10, .12f, -10);
            var pose = new AbilityContext(motor, null, null, motor.transform.position,
                Vector3.forward, new Vector3(-10, .12f, -6));
            var skill = system.Kit.Skill2;
            skill.Activate(pose); Assert.IsTrue(system.TrackSkillRequest(1, 201));
            float remaining = skill.DurationRemaining;
            skill.Tick(pose, 2);
            Assert.AreEqual(remaining, skill.DurationRemaining, "A missing unconfirmed plant ended the active skill.");
            var canPredict = typeof(HeroAbilitySystem).GetMethod("CanPredictSkill",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsFalse((bool)canPredict.Invoke(system, new object[] { HeroAbilitySystem.Slot.Skill2 }));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2, 201,
                pose.Position, pose.Forward, pose.AimPoint, 0));
            yield return null;
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant);
            Assert.IsTrue((bool)canPredict.Invoke(system, new object[] { HeroAbilitySystem.Slot.Skill2 }));

            Assert.IsTrue(system.TrackSkillRequest(1, 202, reactivation: true));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2, 202,
                pose.Position, pose.Forward, pose.AimPoint, 0));
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Assert.AreEqual(1, PaetePlant.Live.Count, "A command confirmation planted a second tree.");
            Assert.IsTrue(system.TrackSkillRequest(1, 203, reactivation: true));
            Assert.IsTrue(system.ResolveSkillReceipt(1, 203, false, 3, 0));
            Assert.IsTrue(skill.IsActive, "A denied command cancelled the earlier accepted plant skill.");
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
        }

        [UnityTest]
        public IEnumerator RefusedFreeRecallDoesNotCreateAChargeAndEligibilityDoesNotMutateHeldTime()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);actor.AbilitySystem.BindHero("nemu");yield return null;
            var system=actor.AbilitySystem;var kit=system.Kit;
            var context=new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>());
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(context));Assert.AreEqual(0,kit.Skill2.ChargesRemaining);
            Assert.IsTrue(kit.Skill2.IsActive);Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(context));
            kit.Skill2.RollBackPredictedCast(context,refundResources:false);
            Assert.AreEqual(0,kit.Skill2.ChargesRemaining,"A free reactivation has no charge to refund");
            float heldBefore=kit.Skill1.HeldSecondsOnCast;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,system.CheckNetworkSkill(HeroAbilitySystem.Slot.Skill1,actor.transform.position,actor.transform.forward,actor.transform.position+Vector3.forward*3,1.1f));
            Assert.AreEqual(heldBefore,kit.Skill1.HeldSecondsOnCast);Assert.AreEqual(0,kit.Skill1.CooldownRemaining);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill1(context));
            float cooldown=kit.Skill1.CooldownRemaining;
            Assert.AreEqual(HeroKit.CastOutcome.Cooling,system.CheckNetworkSkill(HeroAbilitySystem.Slot.Skill1,actor.transform.position,actor.transform.forward,actor.transform.position+Vector3.forward*3,2));
            Assert.AreEqual(heldBefore,kit.Skill1.HeldSecondsOnCast);Assert.AreEqual(cooldown,kit.Skill1.CooldownRemaining);
            system.TrackSkillRequest(0,1);system.TrackSkillRequest(0,2);
            Assert.IsFalse(system.PendingSkillReceipt(0,1));Assert.IsTrue(system.PendingSkillReceipt(0,2));
            system.ResetKit();Assert.IsFalse(system.PendingSkillReceipt(0,2),"Round resets retire outstanding predictions");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ApprovedPlantPlaybackDoesNotRecheckReplicaReadinessOrDeferAnUnpredictedCast()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            var at = new Vector3(-10, .12f, -10);
            var aim = at + Vector3.forward * 4;
            motor.transform.position = at;
            var skill = system.Kit.Skill2;
            skill.Activate(new AbilityContext(motor, null, null, at, Vector3.forward, aim));
            Assert.IsNull(PaetePlant.OwnedBy(1), "Local prediction must still await acceptance.");
            system.ResetKit();

            Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: false));
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant, "An unpredicted approved owner cast must initialize its world effect.");
            Assert.IsTrue(skill.IsActive);
            Assert.IsFalse(plant.ShotReady);
            Assert.IsFalse(plant.Fire(aim), "An ordinary command must still respect reload.");
            Assert.AreEqual(HeroKit.CastOutcome.NotYet, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: true));
            Assert.AreEqual(0, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);

            Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: false));
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length,
                "Replica clock drift must not swallow a host-approved command.");
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Assert.AreEqual("hero-paete-command", skill.CastAction);
            Assert.IsFalse(plant.Fire(aim), "Playback must not unlock further local shots.");
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator JoinedStatusPresentationNeedsNoOriginalCastAndCleansUpPerBody()
        {
            var system = Owner("phaister");
            var first = system.GetComponent<CharacterMotor>();
            var second = new GameObject("Other joined body").AddComponent<CharacterMotor>();
            second.PlayerSlot = 2; second.enabled = false;
            // No original cast and no RoundDirector: these are the received joining timers.
            first.ApplyNetworkReworkStatuses(0, 0, 2, 2, Vector3.zero);
            second.ApplyNetworkReworkStatuses(0, 0, 2, 2, Vector3.zero);
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("HexMark"));

            first.gameObject.SetActive(false);
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length,
                "Despawn must clean only that body's presentation.");
            first.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);

            first.ApplyNetworkReworkStatuses(0, 0, 0, 0, Vector3.zero);
            second.ApplyNetworkReworkStatuses(0, 0, 0, 0, Vector3.zero);
            yield return null;
            yield return null;
            Assert.IsNull(GameObject.Find("HexMark"));
            foreach (var moon in Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None))
                Assert.IsTrue(moon.Ending, "The authored end must follow received status expiry.");
            Object.Destroy(first.gameObject);
            Object.Destroy(second.gameObject);
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator AcceptedCastsWaitForTheirBodyAndKeepExplicitCommandIntentExactlyOnce()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            GameServices.Ensure();
            GameServices.Round.Clear();
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            var root = new GameObject("Delayed body receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router, 123L);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(MatchRpc).GetMethod("OnPlayAbilityMsg", flags);
            var flush = typeof(MatchRpc).GetMethod("FlushPendingSkills", flags);
            int Pending() => ((ICollection)typeof(MatchRpc).GetField("_pendingSkillCasts", flags).GetValue(router)).Count;
            var cast = new SkillCastMessage
            {
                Seat = 1, Slot = 1, AbilityId = new FixedString64Bytes("paete_skill2"), Match = 123, Round = 1,
                Event = 1, Position = new Vector3(-10, .12f, -10), Forward = Vector3.forward,
                AimPoint = new Vector3(-10, .12f, -6)
            };
            void Send()
            {
                using var writer = new FastBufferWriter(SkillCastMessage.MaxWireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(cast);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(router, new object[] { NetworkManager.ServerClientId, reader });
            }
            Send(); Send();
            Assert.AreEqual(1, Pending(), "A duplicate queued the same accepted cast twice.");
            cast.Event = 2; cast.Reactivation = true; Send();
            Assert.AreEqual(2, Pending());
            var staleWorld = new WorldSnapshotHeader { SkillEvent = 2 };
            Assert.IsTrue((bool)typeof(MatchRpc).GetMethod("WorldSnapshotNeedsRefresh", flags)
                .Invoke(router, new object[] { staleWorld }), "A world restore raced ahead of pending casts.");
            GameServices.Round.Register(motor);
            flush.Invoke(router, null);
            Assert.Zero(Pending());
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant);
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);
            Send();
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);

            var skill = system.Kit.Skill2;
            var context = new AbilityContext(motor, null, null);
            skill.Tick(context, PaeteRules.PlantLifeSeconds + 1);
            Assert.IsFalse(skill.IsActive);
            cast.Event = 3; Send();
            Assert.AreSame(plant, PaetePlant.OwnedBy(1), "An accepted command became a new plant after timer drift.");
            Assert.AreEqual(2, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(HeroKit.CastOutcome.Missing, system.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                cast.Position, cast.Forward, cast.AimPoint, 0, false, "paete_skill2d", false));

            GameServices.Round.Clear(); cast.Event = 4; cast.Reactivation = false; Send();
            Assert.AreEqual(1, Pending());
            GameServices.Match.ApplySnapshot(new int[4], 2, true);
            flush.Invoke(router, null);
            Assert.Zero(Pending(), "A queued cast survived its round scope.");
            GameServices.Round.Register(motor);
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator OmenRecoverySeeksAuthoredVisualTimeAndEmptyStateEndsItsGrant()
        {
            var system = Owner("phaister");
            var motor = system.GetComponent<CharacterMotor>();
            var kit = (PhaisterHeroKit)system.Kit;
            Assert.AreSame(kit.Ultimate, system.FindPreparedWorldAbility(kit.Ultimate.Id));
            Assert.IsNull(system.FindPreparedWorldAbility(kit.Skill1.Id));
            var centre = new Vector3(-10, 4, -8);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            kit.RestoreCoven(motor, centre, 0, 2);
            var hole = Object.FindAnyObjectByType<VoodooBlackHole>();
            Assert.IsNotNull(hole);
            var omen = hole.GetComponentInChildren<Visual.PhaisterOmen>();
            float expected = kit.Ultimate.Windup + kit.Ultimate.Duration - 2;
            Assert.AreEqual(expected, (float)typeof(Visual.PhaisterOmen).GetField("_t", flags).GetValue(omen), .001f);
            Assert.AreEqual(2, (float)typeof(VoodooBlackHole).GetField("_life", flags).GetValue(hole), .001f);
            Assert.Greater(omen.LifeSeconds, kit.Ultimate.Windup + kit.Ultimate.Duration,
                "Recovery shortened the authored timeline instead of seeking it.");
            kit.RestoreCoven(motor, centre, 0, 0);
            Assert.IsFalse(kit.Ultimate.IsActive);
            yield return null;
            Assert.IsNull(Object.FindAnyObjectByType<VoodooBlackHole>());

            kit.RestoreCoven(motor, centre, .3f, kit.Ultimate.Duration);
            hole = Object.FindAnyObjectByType<VoodooBlackHole>();
            omen = hole.GetComponentInChildren<Visual.PhaisterOmen>();
            Assert.AreEqual(kit.Ultimate.Windup - .3f,
                (float)typeof(Visual.PhaisterOmen).GetField("_t", flags).GetValue(omen), .001f);
            Assert.IsTrue(kit.Ultimate.IsWindingUp);
            kit.RestoreCoven(motor, centre, 0, 0);
            Assert.IsFalse(kit.Ultimate.IsWindingUp);
            yield return null;
            Assert.IsNull(Object.FindAnyObjectByType<VoodooBlackHole>());
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ExpiredUltimateCohortsRetireOlderPlaybackAndKeepTheirTerminalIdentity()
        {
            Owner("phaister");
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            var previousRouter = MatchRpc.Instance;
            var root = new GameObject("Expired cohort receiver"); root.SetActive(false);
            var router = root.AddComponent<MatchRpc>();
            var phase = root.AddComponent<SharedUltimatePhase>();
            var instance = typeof(MatchRpc).GetProperty("Instance");
            var match = typeof(MatchRpc).GetProperty("PresentationMatchId");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var receive = typeof(SharedUltimatePhase).GetMethod("Receive", flags);
            var commits = new[] { new UltimateCommit(3, 1, Vector3.zero, Vector3.forward, Vector3.up, 0) };
            try
            {
                instance.SetValue(null, router); match.SetValue(router, 123L);
                typeof(SharedUltimatePhase).GetProperty("MatchId").SetValue(phase, 123L);
                typeof(SharedUltimatePhase).GetProperty("Round").SetValue(phase, 1);
                typeof(SharedUltimatePhase).GetProperty("PhaseId").SetValue(phase, 4L);
                typeof(SharedUltimatePhase).GetProperty("Active").SetValue(phase, true);
                void Receive(long id, long sequence, bool expired) => receive.Invoke(phase,
                    new object[] { id, 1, sequence, SharedUltimatePhase.Now - (expired ? 100 : 0), 1f, commits, 50f });
                Receive(123, 5, true);
                Assert.IsFalse(phase.Active); Assert.AreEqual(5, phase.PhaseId);
                Receive(123, 5, false);
                Assert.IsFalse(phase.Active, "A duplicate terminal phase restarted its introduction.");
                match.SetValue(router, 456L);
                Receive(456, 1, true);
                Assert.AreEqual(456, phase.MatchId); Assert.AreEqual(1, phase.PhaseId);
                Receive(456, 1, false);
                Assert.IsFalse(phase.Active, "A late first phase failed to adopt its new match identity.");
            }
            finally { instance.SetValue(null, previousRouter); Object.Destroy(root); }
            yield return null;
        }
    }
}
